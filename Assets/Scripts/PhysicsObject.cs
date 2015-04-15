using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsObjectSide
{
	public PhysicsObject obj;
	public AABB.Direction side;

	public PhysicsObjectSide(PhysicsObject obj, AABB.Direction side)
	{
		this.obj = obj;
		this.side = side;
	}

	public override string ToString()
	{
		return "(" + obj.name + " " + side + ")";
	}
}

public enum PhysicsMode
{
	// will be taken into account for collisions
	Solid,
	// instead of collisions, it raises trigger events
	Trigger,
	// does not interact with anything
	Kinematic
}

public enum TriggerMode
{
	// will trigger when any point of the moving box enters the trigger area
	WholeArea,
	// will trigger only when the center enters the trigger area
	Center
}

public class TriggerEventArgs : System.EventArgs
{
	public PhysicsObject trigger { get; internal set; }

	public TriggerEventArgs(PhysicsObject trigger)
	{
		this.trigger = trigger;
	}
}

public class PhysicsObject : MonoBehaviour
{
	#region constants

	public static readonly float REVERSE_DISTANCE = 0.1f;

	public static readonly int DEFAULT_PRIORITY = 100;
	public static readonly int TRIGGER_PRIORITY = 25;

	#endregion constants

	public AABB localHitBox;

	public AABB globalHitBox
	{
		get
		{
			Vector3 center = globalCenter;
			Vector3 corner = transform.TransformPoint(new Vector3(localHitBox.x + localHitBox.hw, localHitBox.y + localHitBox.hh, 1));
			Vector3 size = corner - center;

			return new AABB(center.x, center.y, size.x, size.y);
		}

	}

	public Vector3 globalCenter
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x, localHitBox.y));
		}
	}

	public Vector3 globalTopCenter
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x, localHitBox.y + localHitBox.hh));
		}
	}

	public Vector3 globalBottomCenter
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x, localHitBox.y - localHitBox.hh));
		}
	}

	public Vector3 globalRightCenter
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x + localHitBox.hw, localHitBox.y));
		}
	}

	public Vector3 globalLeftCenter
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x - localHitBox.hw, localHitBox.y));
		}
	}

	public Vector3 globalTopLeft
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x - localHitBox.hw, localHitBox.y + localHitBox.hh));
		}
	}

	public Vector3 globalTopRight
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x + localHitBox.hw, localHitBox.y + localHitBox.hh));
		}
	}

	public Vector3 globalBottomLeft
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x - localHitBox.hw, localHitBox.y - localHitBox.hh));
		}
	}

	public Vector3 globalBottomRight
	{
		get
		{
			return transform.TransformPoint(new Vector3(localHitBox.x + localHitBox.hw, localHitBox.y - localHitBox.hh));
		}
	}

	public bool canCollideLeft = true;
	public bool canCollideRight = true;
	public bool canCollideTop = true;
	public bool canCollideBottom = true;

	public bool useGravity = true;

	public float maxHorizSpeed = 5.0f;
	public float maxVertSpeed = 20.0f;
	// 0..friction..1
	public float friction = 0.8f;
	// 0..bounciness..1
	public float bounciness = 0.5f;
	public Vector3 velocity = Vector3.zero;
	public Vector3 acceleration = Vector3.zero;

	public Vector3 additionalDrag = Vector3.zero;

	public float gravityFactor = 1.0f;
	public float maxHorizSpeedFactor = 1.0f;
	public float maxVertSpeedFactor = 1.0f;

	public PhysicsMode mode = PhysicsMode.Solid;
	public bool isStatic = true;

	// only used when mode == PhysicsMode.Trigger
	public TriggerMode triggerMode = TriggerMode.WholeArea;

	public bool performSpriteStretch = false;
	public float verticalStretchMultiplier = 1.0f;
	private float currentVertStretch = 1.0f;

	// higher means it will be processed earlier, other circumstances being equal
	public int priority = DEFAULT_PRIORITY;

	public GameObject sprite = null;


	public bool isGrounded
	{
		get
		{
			return _isGrounded;
		}
		set
		{
			if (_isGrounded != value)
			{
				_isGrounded = value;

				if (IsGroundedChanged != null)
				{
					IsGroundedChanged(this, null);
				}
			}
		}
	}
	private bool _isGrounded = false;

	public event System.EventHandler IsGroundedChanged;
	// for when THIS object enters/exits a trigger
	public event System.EventHandler<TriggerEventArgs> EnteredTrigger;
	public event System.EventHandler<TriggerEventArgs> StayedTrigger;
	public event System.EventHandler<TriggerEventArgs> ExitedTrigger;

	void Awake()
	{
		Transform spriteTransform = transform.Find("Sprite");

		if (spriteTransform != null)
		{
			sprite = spriteTransform.gameObject;
		}
	}

	void Update()
	{
		if (GameManager.instance.debugDrawAnything)
		{
			if (mode == PhysicsMode.Trigger)
			{
				globalHitBox.DebugDraw(Utils.TranslucidYellow);
			}
			else
			{
				globalHitBox.DebugDraw(new Color(1, 1, 1, 0.2f));
			}
		}
	}

	public void FixedUpdate()
	{
		if (isStatic)
		{
			return;
		}

		if (useGravity)
		{
			acceleration.y += -PhysicsManager.instance.gravity * gravityFactor;
		}

		Vector3 finalPosition = transform.position;
		Vector3 previousVelocity = velocity;

		float velocityDelta = (ComputeVelocity(velocity.x, acceleration.x, PhysicsManager.instance.airDrag + additionalDrag.x, maxHorizSpeed * maxHorizSpeedFactor) - velocity.x) / 2;
		velocity.x += velocityDelta;
		finalPosition += Vector3.right * velocity.x * Time.fixedDeltaTime;
		velocity.x += velocityDelta;

		velocityDelta = (ComputeVelocity(velocity.y, acceleration.y, PhysicsManager.instance.airDrag + additionalDrag.y, maxVertSpeed * maxVertSpeedFactor) - velocity.y) / 2;
		velocity.y += velocityDelta;
		finalPosition += Vector3.up * velocity.y * Time.fixedDeltaTime;
		velocity.y += velocityDelta;

		PhysicsManager.instance.CollideAndCorrect(this, finalPosition);

		if (performSpriteStretch)
		{
			UpdateSpriteStretch(previousVelocity);
		}

		acceleration = Vector3.zero;
	}

	#region SpriteStretch
	public float GetSpriteArea()
	{
		if (sprite == null)
		{
			return float.NaN;
		}
		else
		{

			return sprite.transform.lossyScale.x * sprite.transform.lossyScale.y;
		}
	}


	float[] stretchAccum = { 0, 0, 0, 0, 0, 0 };
	int nextStretchValue = 0;

	private void UpdateSpriteStretch(Vector3 previousVelocity)
	{
		stretchAccum[nextStretchValue] = Mathf.Abs(velocity.y) - Mathf.Abs(previousVelocity.y);

		float average = 0.0f;
		for (int i = 0; i < stretchAccum.Length; i++)
		{
			average += stretchAccum[i] / (float)stretchAccum.Length;
		}

		nextStretchValue = (nextStretchValue + 1) % stretchAccum.Length;

		SetSpriteStretch(Mathf.Pow(1.1f, average));
	}

	private void SetSpriteStretch(float vertStretch)
	{
		if (sprite == null)
		{
			return;
		}

		float previousVertStretch = currentVertStretch;
		float previousHorizStretch = 1.0f / currentVertStretch;

		vertStretch *= verticalStretchMultiplier;

		float horizStretch = 1.0f / vertStretch;

		sprite.transform.localScale = new Vector3(
			horizStretch * sprite.transform.localScale.x / previousHorizStretch,
			vertStretch * sprite.transform.localScale.y / previousVertStretch,
			sprite.transform.localScale.z);
		sprite.transform.localPosition = Vector3.down * (0.5f * (1 - vertStretch));

		currentVertStretch = vertStretch;
	}
	#endregion

	// partially taken from flixel
	public static float ComputeVelocity(float velocity, float acceleration, float drag)
	{
		if (acceleration != 0)
		{
			velocity += acceleration * Time.fixedDeltaTime;
		}
		else if (drag != 0)
		{
			float drag_velocity = drag * Time.fixedDeltaTime;

			if (velocity - drag_velocity > 0)
			{
				velocity -= drag_velocity;
			}
			else if (velocity + drag_velocity < 0)
			{
				velocity += drag_velocity;
			}
			else
			{
				velocity = 0;
			}
		}

		return velocity;
	}

	public static float ComputeVelocity(float velocity, float acceleration, float drag, float max)
	{
		velocity = ComputeVelocity(velocity, acceleration, drag);
		
		if (velocity > max)
		{
			velocity = max;
		}
		else if (velocity < -max)
		{
			velocity = -max;
		}

		return velocity;
	}

	public static bool CanCollide(PhysicsObject obj1, PhysicsObject obj2)
	{
		return !Physics2D.GetIgnoreLayerCollision(obj1.gameObject.layer, obj2.gameObject.layer);
	}

	public void NotifyEnteredTrigger(PhysicsObject trigger)
	{
		if (EnteredTrigger != null)
		{
			EnteredTrigger(this, new TriggerEventArgs(trigger));
		}
	}

	public void NotifyExitedTrigger(PhysicsObject trigger)
	{
		if (ExitedTrigger != null)
		{
			ExitedTrigger(this, new TriggerEventArgs(trigger));
		}
	}

	public void NotifyStaysTrigger(PhysicsObject trigger)
	{
		if (StayedTrigger != null)
		{
			StayedTrigger(this, new TriggerEventArgs(trigger));
		}
	}
}
