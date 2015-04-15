using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CollisionType
{
	None,
	Overlapping,
	Touching
}

public class CharacterController : MonoBehaviour
{
	public enum MovementState
	{
		Standing, // includes moving (walking and running)
		Jumping,
		Smashing, // used when still has to smash a block
		SmashingFinished, // used when a block has already been smashed
		Crouching, // includes moving (crawling)
		Climbing,
		Falling
	}

	public enum FacingDirection
	{
		Left,
		Right
	}

	public PhysicsObject physics;

	#region Object Holding

	public Transform holderRight;
	public Transform holderLeft;

	public PhysicsObject heldObject = null;

	#endregion

	public float maxAcceleration = 50.0f;
	public float maxAccelerationOpposite = 500.0f;
	public float minHorizAxisValue = 0.3f;
	public float jumpAcceleration = 600;
	public float accelBoost = 2.8f;
	public float maxHorizSpeedBoost = 1.5f;

	public float smashHorizMovementFactor = 0.5f;
	public float smashUpwardVelocity = 20.0f;
	public float smashMaxVertSpeedMultiplier = 2.0f;
	private float _origMaxVertSpeed;

	public Vector3 thrownObjectVelocityRight = new Vector3(10, 10);

	public float climbingSpeed = 10.0f;

	public bool boost = false;

	public bool _isHoldingJump = false;
	private float _jumpStartTimestamp;

	public MovementState movementState = MovementState.Standing;

	private float _jumpGravityFactor = 1.0f;
	private float _smashGravityFactor = 1.0f;

	private FacingDirection _facingDirection = FacingDirection.Left;

	public GameObject sprite = null;

	#region Ladders
	HashSet<PhysicsObject> _currentClimbableAreas = new HashSet<PhysicsObject>();
	#endregion

	void Awake()
	{
		holderRight = transform.Find("Holder Right");
		holderLeft = transform.Find("Holder Left");

		Transform spriteTransform = transform.Find("Sprite");

		if (spriteTransform != null)
		{
			sprite = spriteTransform.gameObject;
		}

		physics = GetComponent<PhysicsObject>();
		_origMaxVertSpeed = physics.maxVertSpeed;

		physics.IsGroundedChanged += IsGroundedChanged_EventHandler;

		physics.EnteredTrigger += physics_EnteredTrigger;
		physics.ExitedTrigger += physics_ExitedTrigger;
	}

	void physics_ExitedTrigger(object sender, TriggerEventArgs e)
	{
		MapTile mapTile = e.trigger.gameObject.GetComponent<MapTile>();

		if (mapTile != null && mapTile.type == MapTile.Type.Climbable)
		{
			ExitedClimbableArea(e.trigger);
		}
	}

	void physics_EnteredTrigger(object sender, TriggerEventArgs e)
	{
		MapTile mapTile = e.trigger.gameObject.GetComponent<MapTile>();

		if (mapTile != null && mapTile.type == MapTile.Type.Climbable)
		{
			EnteredClimbableArea(e.trigger);
		}
	}

	void IsGroundedChanged_EventHandler(object sender, System.EventArgs e)
	{
		if (physics.isGrounded)
		{
			switch (movementState)
			{
				case MovementState.Smashing:
				case MovementState.SmashingFinished:
					{
						physics.maxVertSpeed = _origMaxVertSpeed;
						GameManager.instance.StartScreenShake();
					}
					break;

				case MovementState.Climbing:
					StopClimbing();
					break;
			}

			EnterMovementState(MovementState.Standing);
			_smashGravityFactor = _jumpGravityFactor = 1.0f;
			physics.gravityFactor = _jumpGravityFactor * _smashGravityFactor;
		}
		else
		{
			// now airborne.

			switch (movementState)
			{
				case MovementState.Jumping:
					// do nothing
					break;

				case MovementState.Standing:
				case MovementState.Crouching:
					EnterMovementState(MovementState.Falling);
					break;
			}
		}
	}

	void EnteredClimbableArea(PhysicsObject area)
	{
		_currentClimbableAreas.Add(area);
	}

	void ExitedClimbableArea(PhysicsObject area)
	{
		_currentClimbableAreas.Remove(area);

		if (movementState == MovementState.Climbing && _currentClimbableAreas.Count == 0)
		{
			StopClimbing();
		}
	}

	void FixedUpdate()
	{
		if (_isHoldingJump)
		{
			float elapsed = Time.time - _jumpStartTimestamp;

			if (elapsed >= PhysicsManager.instance.jumpDuration_s)
			{
				EndHoldingJump();
			}
		}

		physics.gravityFactor = _jumpGravityFactor * _smashGravityFactor;
	}

	public void StartSmash()
	{
		if (movementState != MovementState.Jumping)
		{
			return;
		}

		EndHoldingJump();

		EnterMovementState(MovementState.Smashing);

		physics.velocity = Vector3.up * smashUpwardVelocity;
		_smashGravityFactor = PhysicsManager.instance.smashGravityMultiplier;
		physics.maxVertSpeed = _origMaxVertSpeed * smashMaxVertSpeedMultiplier;
	}

	public bool CanJump()
	{
		switch (movementState)
		{
			case MovementState.Climbing:
			case MovementState.Crouching:
			case MovementState.Standing:
				return true;

			default:
				return false;
		}
	}

	public void StartHoldingJump()
	{
		if (!CanJump())
		{
			return;
		}

		_isHoldingJump = true;
		_jumpStartTimestamp = Time.time;

		if (movementState == MovementState.Climbing)
		{
			StopClimbing();
		}
		else if (movementState == MovementState.Crouching)
		{
			StopCrouching();
		}

		EnterMovementState(MovementState.Jumping);

		physics.acceleration.y += jumpAcceleration;
		_jumpGravityFactor = PhysicsManager.instance.jumpGravityMultiplier;

		physics.isGrounded = false;
	}

	public void EndHoldingJump()
	{
		_isHoldingJump = false;
		_jumpGravityFactor = 1.0f;
	}

	public bool IsHoldingJump()
	{
		return _isHoldingJump;
	}

	public void PressedAction()
	{
		if (movementState == MovementState.Crouching)
		{
			if (heldObject == null)
			{
				// try to pick up object

				var potentialOverlaps = GameManager.instance.currentMap.spatialHash.GetPotentialOverlapsList(this.physics);

				foreach (PhysicsObject obj in potentialOverlaps)
				{
					if (AABB.Overlap(this.physics.globalHitBox, obj.globalHitBox, true))
					{
						if (obj.gameObject.name == "Rock")
						{
							if (obj.velocity.magnitude < Mathf.Epsilon)
							{
								if (Mathf.Abs(obj.globalBottomCenter.y - this.physics.globalBottomCenter.y) < 0.01f)
								{
									Debug.Log("PICK UP");
									obj.mode = PhysicsMode.Kinematic;
									obj.useGravity = false;

									switch (_facingDirection)
									{
										case FacingDirection.Left:
											obj.transform.parent = this.holderLeft;
											break;

										case FacingDirection.Right:
											obj.transform.parent = this.holderRight;
											break;
									}

									obj.transform.localPosition = Vector3.zero;

									heldObject = obj;
									break;
								}
							}
						}
					}
				}
			}
			else
			{
				// drop object
				heldObject.mode = PhysicsMode.Solid;
				heldObject.useGravity = true;
				heldObject.transform.parent = null;
				heldObject = null;
			}
		}
		else if (heldObject != null)
		{
			Vector3 velocity = thrownObjectVelocityRight;

			if (_facingDirection == FacingDirection.Left)
			{
				velocity.x *= -1;
			}

			// throw object
			heldObject.mode = PhysicsMode.Solid;
			heldObject.useGravity = true;
			heldObject.transform.parent = null;
			heldObject.velocity = this.physics.velocity + velocity;
			heldObject.acceleration = Vector3.zero;
			heldObject = null;
		}
	}

	private void SwitchFacingdirection(FacingDirection newDirection)
	{
		_facingDirection = newDirection;

		physics.sprite.transform.localScale = new Vector3(-physics.sprite.transform.localScale.x, physics.sprite.transform.localScale.y, physics.sprite.transform.localScale.z);

		if (heldObject != null)
		{
			switch (_facingDirection)
			{
				case FacingDirection.Right:
					heldObject.transform.parent = holderRight;
					break;

				case FacingDirection.Left:
					heldObject.transform.parent = holderLeft;
					break;
			}

			heldObject.transform.localPosition = Vector3.zero;
		}
	}

	public void MoveHoriz(float value)
	{
		if (value > Mathf.Epsilon && _facingDirection == FacingDirection.Left)
		{
			SwitchFacingdirection(FacingDirection.Right);
		}
		else if (value < -Mathf.Epsilon && _facingDirection == FacingDirection.Right)
		{
			SwitchFacingdirection(FacingDirection.Left);
		}

		if (Mathf.Abs(value) < minHorizAxisValue)
		{
			physics.acceleration.x = 0;
			return;
		}

		if (movementState == MovementState.Climbing)
		{
			return;
		}

		float maxAccel;

		if (Mathf.Sign(value) != Mathf.Sign(physics.velocity.x))
		{
			maxAccel = maxAccelerationOpposite;
		}
		else
		{
			maxAccel = maxAcceleration;
		}

		if (boost)
		{
			maxAccel *= accelBoost;
			physics.maxHorizSpeedFactor = maxHorizSpeedBoost;
		}
		else
		{
			physics.maxHorizSpeedFactor = 1.0f;
		}

		physics.acceleration.x = maxAccel * Utils.IntSign(value);

		if (movementState == MovementState.Smashing || movementState == MovementState.SmashingFinished)
		{
			physics.acceleration.x *= smashHorizMovementFactor;
		}
	}

	public void MoveVert(float value, float horizValue)
	{
		bool tooSmall = (Mathf.Abs(value) < minHorizAxisValue);
		bool horizTooSmall = (Mathf.Abs(horizValue) < 2 * minHorizAxisValue);

		// try to start climbing
		if (movementState != MovementState.Climbing)
		{
			PhysicsObject climbableArea;
			bool canClimb = CanStartClimbing(out climbableArea);

			if (canClimb)
			{
				if (value > 0 && !tooSmall && horizTooSmall)
				{
					StartClimbing(climbableArea);
				}
			}
		}

		if (movementState == MovementState.Climbing)
		{
			if (!tooSmall && CanClimb(Utils.IntSign(value)))
			{
				//physics.acceleration.y = maxAcceleration * Utils.IntSign(value);
				physics.velocity.y = climbingSpeed * Utils.IntSign(value);
			}
			else
			{
				//physics.acceleration.y = physics.velocity.y = 0;
				physics.velocity.y = 0;
			}
		}

		if (physics.isGrounded)
		{
			if (
				(movementState != MovementState.Crouching) &&
				(value < 0 && !tooSmall)
				)
			{
				StartCrouching();
			}
			else if (
				(movementState == MovementState.Crouching) &&
				(tooSmall || value >= 0)
				)
			{
				StopCrouching();
			}
		}
	}

	private void StartCrouching()
	{
		EnterMovementState(MovementState.Crouching);

		physics.verticalStretchMultiplier = 0.7f;
	}

	private void StopCrouching()
	{
		EnterMovementState(MovementState.Standing);
	}

	private void StartClimbing(PhysicsObject climbableArea)
	{
		// start climbing!
		physics.velocity = physics.acceleration = Vector3.zero;
		physics.useGravity = false;
		//physics.maxVertSpeedFactor = climbingSpeedFactor;

		// center in the climbable object
		{
			Vector3 pos = transform.position;
			pos.x = climbableArea.transform.position.x;

			transform.position = pos;
		}

		EnterMovementState(MovementState.Climbing);
	}

	private void StopClimbing()
	{
		EnterMovementState(MovementState.Falling);

		physics.useGravity = true;
		physics.maxVertSpeedFactor = 1.0f;
		physics.velocity = Vector3.zero;
	}

	private PhysicsObject FindClimbableAreaContaining(Vector2 point)
	{
		foreach (PhysicsObject area in _currentClimbableAreas)
		{
			if (area.globalHitBox.Contains(point, true))
			{
				return area;
			}
		}

		return null;
	}

	// if it can, returns the area that overlaps the center of the box.
	private bool CanStartClimbing(out PhysicsObject climbableArea)
	{
		if (movementState == MovementState.Climbing || _currentClimbableAreas.Count == 0)
		{
			climbableArea = null;
			return false;
		}

		climbableArea = FindClimbableAreaContaining(physics.globalCenter);
		return (climbableArea != null);
	}

	// vertDirection < 0: down, otherwise up
	private bool CanClimb(int vertDirection)
	{
		PhysicsObject climbableArea = FindClimbableAreaContaining(physics.globalCenter);

		if (climbableArea != null)
		{
			// center overlaps an area: we can move
			return true;
		}
		else
		{
			if (vertDirection < 0) // going down
			{
				//return (FindClimbableAreaContaining(physics.globalBottomCenter) != null);

				// always allow descending
				return true;
			}
			else // going up
			{
				return (FindClimbableAreaContaining(physics.globalTopCenter) != null);
			}
		}
	}

	public void EnterMovementState(MovementState state)
	{
		//Debug.Log(Time.frameCount + " enter state " + state);

		// exit current state
		switch (movementState)
		{
			case MovementState.Crouching:
				physics.verticalStretchMultiplier = 1.0f;
				break;

			default:
				break;
		}

		movementState = state;

		// enter new state
		switch (movementState)
		{
			default:
				break;
		}
	}
}
