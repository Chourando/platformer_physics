using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsManager : MonoBehaviour
{
	public static PhysicsManager instance = null;

	public float gravity = 30.0f;

	public float airDrag = 5.0f;

	// if the bounciness/friction component after correcting is smaller than this epsilon, it's canceled.
	public float bouncinessEpsilon = 1e-5f;
	public float frictionEpsilon = 1e-5f;

	// if the distance between the original position and the one after colliding is smaller than this number,
	//   it's assumed that it was already touching, and the bounciness component is canceled.
	public float alreadyTouchingEpsilon = 1e-5f;

	public float jumpGravityMultiplier = 0.8f;
	public float smashGravityMultiplier = 2.0f;

	public float jumpDuration_s = 0.3f;

	// only used for debugging
	[HideInInspector]
	public int fixedUpdateIteration = 0;

	// stores, for each trigger, all the objects that are in it.
	private Dictionary<PhysicsObject, HashSet<PhysicsObject>> _triggerToObjectSet = new Dictionary<PhysicsObject, HashSet<PhysicsObject>>();
	// stores, for each object, all the triggers it's in.
	//   if an object is in no triggers, it might not be in this dict at all.
	private Dictionary<PhysicsObject, HashSet<PhysicsObject>> _objectToTriggerSet = new Dictionary<PhysicsObject, HashSet<PhysicsObject>>();

	void Awake()
	{
		instance = this;
	}

	void FixedUpdate()
	{
		fixedUpdateIteration++;
	}

	public void CollideAndCorrect(PhysicsObject obj, Vector3 finalPosition)
	{
		Log("---- INIT ----");

		HashSet<PhysicsObject> previousTriggers = GetTriggerSetCopy(obj);

		List<PhysicsObject> potentialObjects;
		List<PhysicsObjectSide> potentialObjectSides;
		GetPotentialCollidingObjects(obj, finalPosition, out potentialObjectSides, out potentialObjects);

		// debug
		if (GameManager.instance.debugDrawPhysics && GameManager.instance.debugDrawAnything)
		{
			Vector3 beforePos = obj.transform.position;

			Color color = new Color(0, 0, 1, 0.4f);
			obj.transform.position = beforePos;
			AABB globalBox = obj.globalHitBox;
			globalBox.DebugDraw(color);

			color = new Color(1, 0, 0, 0.4f);
			obj.transform.position = finalPosition;
			globalBox = obj.globalHitBox;
			globalBox.DebugDraw(color);

			obj.transform.position = beforePos;
		}

		List<PhysicsObjectSide> alreadyCollidedWith = new List<PhysicsObjectSide>();

		CollideAndCorrect(obj, finalPosition, potentialObjectSides, alreadyCollidedWith);

		// enter/exit triggers
		{
			HashSet<PhysicsObject> currentTriggers = GetOverlappingTriggers(obj, potentialObjects);

			foreach (PhysicsObject trigger in currentTriggers)
			{
				if (previousTriggers.Contains(trigger))
				{
					// already accounted for
				}
				else
				{
					ObjectEntersTrigger(obj, trigger);
				}
			}

			foreach (PhysicsObject trigger in previousTriggers)
			{
				if (currentTriggers.Contains(trigger))
				{
					ObjectStaysTrigger(obj, trigger);
				}
				else
				{
					ObjectExitsTrigger(obj, trigger);
				}
			}

		}

		GameManager.instance.currentMap.spatialHash.UpdateObject(obj);
	}

	private void Log(string str)
	{
		//Debug.Log(str);
	}

	private void CollideAndCorrect(PhysicsObject obj, Vector3 finalPosition, List<PhysicsObjectSide> potentialObjects, List<PhysicsObjectSide> alreadyCollidedWith)
	{
		bool isPlayer = (GameManager.instance.player.physics == obj);
		Vector3 translation = finalPosition - obj.transform.position;

		AABB objBox = obj.globalHitBox;

		float closestTime = float.PositiveInfinity;
		Vector3 closestNormal = Vector3.zero;
		PhysicsObject closestObject = null;
		PhysicsObjectSide closestObjectSide = null;
		bool found = false;
		foreach (PhysicsObjectSide otherObjSide in potentialObjects)
		{
			Log(fixedUpdateIteration + " potential " + otherObjSide);

			PhysicsObject otherObj = otherObjSide.obj;

			if (otherObj.mode != PhysicsMode.Solid)
			{
				continue;
			}

			if (!PhysicsObject.CanCollide(obj, otherObj))
			{
				// layers not compatible for collisions
				continue;
			}

			AABB otherBox = otherObj.globalHitBox;

			if (alreadyCollidedWith.Contains(otherObjSide))
			{
				// sanity check: it should never collide twice with the same object
				Log(fixedUpdateIteration + " sanity check: it should never collide twice with the same object " + otherObjSide);
				continue;
			}

			// TEMP DEBUGGING STUFF
			if (obj == GameManager.instance.player.physics &&
				GameManager.instance.player.movementState == CharacterController.MovementState.Jumping &&
				Mathf.Abs(otherBox.top - objBox.bottom) < 1e-4)
			{
				Debug.Log(fixedUpdateIteration + " CLOSE!! player bottom at " + objBox.bottom.ToString("0.0000000000") + " and other (" + otherObj.name + ") at " + otherBox.top.ToString("0.0000000000"));
			}

			bool isOneway = otherObj.GetComponent<MapTile>() != null && otherObj.GetComponent<MapTile>().type == MapTile.Type.OneWayWall;
			
			if (isOneway && AABB.Overlap(objBox, otherBox, false))
			{
				Log(fixedUpdateIteration + " already overlapping with oneway platform: avoid");
				continue;
			}

			if (GameManager.instance.debugDrawPhysics && GameManager.instance.debugDrawAnything)
			{
				otherBox.DebugDraw(Utils.TranslucidGray);
			}

			bool willOverlap;
			float overlapTime;
			AABB.Axis overlapAxis;
			Vector3 overlapNormal;

			AABB.SweepTest(objBox, otherBox, otherObjSide.side, translation, 1, out willOverlap, out overlapTime, out overlapAxis, out overlapNormal);

			if (willOverlap)
			{
				//if (overlapTime == 0.0f)
				//{
				//	if (Vector3.Dot(overlapNormal, translation) > 0)
				//	{
				//		// ignore this collision, it's already touching and going away from it
				//		Debug.Log(fixedUpdateIteration + " it's already touching and going away from it " + otherObjSide);
				//		continue;
				//	}
				//}

				bool updateClosest = (overlapTime < closestTime) ||
					((overlapTime == closestTime) && (otherObj.priority > closestObject.priority));

				if (updateClosest)
				{
					Log(fixedUpdateIteration + " found!!1 " + otherObjSide + " " + overlapTime);

					if (GameManager.instance.debugDrawPhysics && GameManager.instance.debugDrawAnything)
					{
						otherBox.DebugDraw(Utils.TranslucidGreen);
					}

					found = true;
					closestTime = overlapTime;
					closestNormal = overlapNormal;
					closestObject = otherObj;
					closestObjectSide = otherObjSide;
				}
				else
				{
					Log(fixedUpdateIteration + " found but too late " + otherObjSide);

					if (GameManager.instance.debugDrawPhysics && GameManager.instance.debugDrawAnything)
					{
						otherBox.DebugDraw(Utils.TranslucidRed);
					}
				}
			}
			else
			{
				//Log(fixedUpdateIteration + " not found " + otherObjSide);
			}
		}

		if (found)
		{
			Log(fixedUpdateIteration + " final: " + closestObjectSide + " " + closestTime);

			obj.transform.position += translation * closestTime;

			Vector3 remainingTranslation = finalPosition - obj.transform.position;

			if (GameManager.instance.debugDrawPhysics)
			{
				Debug.DrawRay(closestObject.transform.position, closestNormal);
			}

			Vector3 orthogonal = new Vector3(-closestNormal.y, closestNormal.x);

			// correct the final position and the velocity
			{
				if (false)
				{
					// old way: assumes friction zero, bounciness zero

					// project remainingTranslation onto orthogonal
					finalPosition = obj.transform.position + orthogonal * Vector3.Dot(orthogonal, remainingTranslation);

					// project obj.velocity onto orthogonal
					obj.velocity = orthogonal * Vector3.Dot(orthogonal, obj.velocity);
				}
				else
				{
					float combinedFriction = CombineFriction(obj.friction, closestObject.friction);
					float combinedBounciness = CombineBounciness(obj.bounciness, closestObject.bounciness);

					Utils.AssertTrue(combinedFriction >= 0);
					Utils.AssertTrue(combinedFriction <= 1);
					Utils.AssertTrue(combinedBounciness >= 0);
					Utils.AssertTrue(combinedBounciness <= 1);

					obj.velocity = ReflectAndApplyFrictionBounciness(obj.velocity, closestNormal, orthogonal, combinedFriction, combinedBounciness, true);
					remainingTranslation = ReflectAndApplyFrictionBounciness(remainingTranslation, closestNormal, orthogonal, combinedFriction, combinedBounciness, true);

					finalPosition = obj.transform.position + remainingTranslation;
				}
			}

			if (closestNormal.y > 0)
			{
				// hit ground
				obj.isGrounded = true;
			}

			if (Vector3.Distance(finalPosition, obj.transform.position) == 0.0f)
			{
				return;
			}

			// try again to see if there are more collisions
			// no need to compute potential set again, it's contained in the same bigger box.
			alreadyCollidedWith.Add(closestObjectSide);
			CollideAndCorrect(obj, finalPosition, potentialObjects, alreadyCollidedWith);
		}
		else
		{
			if (Mathf.Abs(finalPosition.y - obj.transform.position.y) > 1e-6)
			{
				obj.isGrounded = false;
			}

			// end recursion
			//Log("FINALLY");
			obj.transform.position = finalPosition;
		}
	}



	public void GetPotentialCollidingObjects(PhysicsObject obj, Vector3 finalPosition,
		out List<PhysicsObjectSide> potentialObjectSides, out List<PhysicsObject> potentialObjects)
	{
		Vector3 translation = finalPosition - obj.transform.position;

		potentialObjects = GameManager.instance.currentMap.spatialHash.GetPotentialOverlapsList(obj, translation);

		if (GameManager.instance.debugDrawAnything)
		{
			foreach (PhysicsObject pObj in potentialObjects)
			{
				pObj.globalHitBox.DebugDraw(Utils.TranslucidRed, false);
			}
		}

		potentialObjectSides = new List<PhysicsObjectSide>();

		foreach (PhysicsObject pObj in potentialObjects)
		{
			if (pObj.canCollideBottom)
			{
				potentialObjectSides.Add(new PhysicsObjectSide(pObj, AABB.Direction.Bottom));
			}
			if (pObj.canCollideLeft)
			{
				potentialObjectSides.Add(new PhysicsObjectSide(pObj, AABB.Direction.Left));
			}
			if (pObj.canCollideRight)
			{
				potentialObjectSides.Add(new PhysicsObjectSide(pObj, AABB.Direction.Right));
			}
			if (pObj.canCollideTop)
			{
				potentialObjectSides.Add(new PhysicsObjectSide(pObj, AABB.Direction.Top));
			}
		}
	}

	private void ObjectEntersTrigger(PhysicsObject obj, PhysicsObject trigger)
	{
		if (!_triggerToObjectSet.ContainsKey(trigger))
		{
			_triggerToObjectSet.Add(trigger, new HashSet<PhysicsObject>());
		}

		if (!_objectToTriggerSet.ContainsKey(obj))
		{
			_objectToTriggerSet.Add(obj, new HashSet<PhysicsObject>());
		}

		_triggerToObjectSet[trigger].Add(obj);
		_objectToTriggerSet[obj].Add(trigger);

		obj.NotifyEnteredTrigger(trigger);
	}

	private void ObjectStaysTrigger(PhysicsObject obj, PhysicsObject trigger)
	{
		obj.NotifyStaysTrigger(trigger);
	}

	private void ObjectExitsTrigger(PhysicsObject obj, PhysicsObject trigger)
	{
		if (
			!_triggerToObjectSet.ContainsKey(trigger) ||
			!_triggerToObjectSet[trigger].Contains(obj) ||
			!_objectToTriggerSet.ContainsKey(obj) ||
			!_objectToTriggerSet[obj].Contains(trigger))
		{
			throw new System.Exception("Exiting trigger but it's not accounted for.");
		}

		_triggerToObjectSet[trigger].Remove(obj);
		_objectToTriggerSet[obj].Remove(trigger);

		obj.NotifyExitedTrigger(trigger);
	}

	private HashSet<PhysicsObject> GetTriggerSetCopy(PhysicsObject obj)
	{
		if (!_objectToTriggerSet.ContainsKey(obj))
		{
			return new HashSet<PhysicsObject>();
		}

		return new HashSet<PhysicsObject>(_objectToTriggerSet[obj]);
	}

	public bool IsObjectInTrigger(PhysicsObject obj, PhysicsObject trigger)
	{
		if (trigger.mode != PhysicsMode.Trigger)
		{
			return false;
		}

		return (_triggerToObjectSet.ContainsKey(trigger) && _triggerToObjectSet[trigger].Contains(obj));
	}

	private HashSet<PhysicsObject> GetOverlappingTriggers(PhysicsObject obj, List<PhysicsObject> potentialObjects)
	{
		HashSet<PhysicsObject> triggers = new HashSet<PhysicsObject>();

		foreach (PhysicsObject pObj in potentialObjects)
		{
			if (pObj.mode == PhysicsMode.Trigger)
			{
				switch (pObj.triggerMode)
				{
					case TriggerMode.WholeArea:
						{
							if (AABB.Overlap(obj.globalHitBox, pObj.globalHitBox, true))
							{
								triggers.Add(pObj);
							}
						}
						break;

					case TriggerMode.Center:
						{
							if (pObj.globalHitBox.Contains(obj.globalCenter, true))
							{
								triggers.Add(pObj);
							}
						}
						break;
				}
			}
		}

		return triggers;
	}

	public static float CombineFriction(float friction1, float friction2)
	{
		return Mathf.Sqrt(friction1 * friction2);
	}

	public static float CombineBounciness(float bounciness1, float bounciness2)
	{
		return Mathf.Sqrt(bounciness1 * bounciness2);
	}

	public static Vector3 ReflectAndApplyFrictionBounciness(Vector3 vec, Vector3 normal, Vector3 orthogonal, float friction, float bounciness, bool applyEpsilons)
	{
		normal.Normalize();
		orthogonal.Normalize();

		vec = Utils.Reflect(vec, normal);

		Vector3 bouncinessComponent = normal * Vector3.Dot(vec, normal);
		Vector3 frictionComponent = orthogonal * Vector3.Dot(vec, orthogonal);

		if (applyEpsilons)
		{
			if (bouncinessComponent.magnitude < PhysicsManager.instance.bouncinessEpsilon)
			{
				bouncinessComponent = Vector3.zero;
			}

			if (frictionComponent.magnitude < PhysicsManager.instance.frictionEpsilon)
			{
				frictionComponent = Vector3.zero;
			}
		}

		bouncinessComponent *= bounciness;
		frictionComponent *= (1 - friction);

		return bouncinessComponent + frictionComponent;
	}
}