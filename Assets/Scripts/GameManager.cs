using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using InControl;


public class GameManager : MonoBehaviour
{
	public static GameManager instance = null;

	public GameObject playerPrefab;
	public GameObject triggerPrefab;
	public GameObject ladderPrefab;
	public GameObject ladderTopPrefab;
	public GameObject invisibleWallPrefab;
	public GameObject rockPrefab;
	public GameObject floorPrefab;
	public GameObject onewayPrefab;
	public GameObject backgroundPrefab;
	public GameObject boulderPrefab;

	public LevelMap currentMap;

	public CharacterController player;
	private bool _isHoldingJump = false;
	private bool _isHoldingAction = false;

	public float screenShakeTime = 0.5f;
	public float screenShakeRadius = 0.5f;
	private float _screenShakeStartTimestamp = -1;
	private bool _screenShakeStarted = false;

	public float cameraDampTime = 0.3f;
	private Vector3 _cameraVelocity = Vector3.zero;

	public bool debugDrawPhysics = false;
	public bool debugDrawAnything = false;

	void Awake()
	{
		instance = this;

		currentMap = new LevelMap();

		currentMap.Init(LevelMap.TEST_MAP_0);

		InputManager.Setup();
	}

	void Update()
	{
		InputManager.Update();

		//if (InputManager.ActiveDevice.LeftStickX.IsPressed)
		{
			player.MoveHoriz(InputManager.ActiveDevice.LeftStickX);
			player.MoveVert(InputManager.ActiveDevice.LeftStickY, InputManager.ActiveDevice.LeftStickX);
		}

		if (InputManager.ActiveDevice.Action1)
		{
			if (!_isHoldingJump)
			{
				player.StartHoldingJump();
			}

			_isHoldingJump = true;
		}
		else
		{
			if (_isHoldingJump)
			{
				player.EndHoldingJump();
			}

			_isHoldingJump = false;
		}

		if (InputManager.ActiveDevice.Action4)
		{
			if (player.movementState == CharacterController.MovementState.Jumping)
			{
				player.StartSmash();
			}
		}

		
		if (InputManager.ActiveDevice.Action3)
		{
			if (!_isHoldingAction)
			{
				player.PressedAction();
			}

			_isHoldingAction = true;
		}
		else
		{
			_isHoldingAction = false;
		}

		player.boost = InputManager.ActiveDevice.RightTrigger || InputManager.ActiveDevice.RightBumper;

		// update screenshake
		{
			if (_screenShakeStarted)
			{
				if (Time.time >= _screenShakeStartTimestamp + screenShakeTime)
				{
					_screenShakeStarted = false;
					RestoreScreenShake();
				}
				else
				{
					UpdateScreenShake();
				}
			}
		}

		if (debugDrawAnything)
		{
			// show player box
			player.physics.globalHitBox.DebugDraw(Utils.TranslucidWhite);
		}
	}

	public void WorldPosToMapTile(Vector3 worldPos, out int i, out int j)
	{
		i = (int)((worldPos.x - currentMap.left) / 1.0f);
		j = currentMap.height - 1 - (int)((worldPos.y - currentMap.bottom) / 1.0f);
	}

	[System.Obsolete]
	public List<MapTile> GetNearbyTiles(Vector3 worldPos, Vector3 direction)
	{
		List<MapTile> tiles = new List<MapTile>();

		int i, j;
		WorldPosToMapTile(worldPos, out i, out j);

		int min_i = i - 1;
		int max_i = i + 1;
		int min_j = j - 1;
		int max_j = j + 1;

		min_i = Mathf.Clamp(min_i, 0, currentMap.width - 1);
		max_i = Mathf.Clamp(max_i, 0, currentMap.width - 1);
		min_j = Mathf.Clamp(min_j, 0, currentMap.height - 1);
		max_j = Mathf.Clamp(max_j, 0, currentMap.height - 1);

		for (i = min_i; i <= max_i; i++)
		{
			for (j = min_j; j <= max_j; j++)
			{
				MapTile tile = GetTileAt(i, j);

				if (tile != null)
				{
					tiles.Add(tile);
				}
			}
		}

		return tiles;
	}

	public bool IsSolid(int i, int j, bool includeOneway)
	{
		MapTile tile = GetTileAt(i, j);

		if (tile != null)
		{
			if (includeOneway)
			{
				return (tile.physics.mode == PhysicsMode.Solid);
			}
			else
			{
				return (tile.type != MapTile.Type.OneWayWall) && (tile.physics.mode == PhysicsMode.Solid);
			}
		}
		else
		{
			return false;
		}
	}

	public bool IsTrigger(int i, int j)
	{
		MapTile tile = GetTileAt(i, j);

		if (tile != null)
		{
			return (tile.physics.mode == PhysicsMode.Trigger);
		}
		else
		{
			return false;
		}
	}

	public bool IsKinematic(int i, int j)
	{
		MapTile tile = GetTileAt(i, j);

		if (tile != null)
		{
			return (tile.physics.mode == PhysicsMode.Kinematic);
		}
		else
		{
			return false;
		}
	}

	public MapTile GetTileAt(int i, int j)
	{
		return currentMap.GetTileAt(i, j);
	}

	public void UpdateEdgesAt(int i, int j)
	{
		MapTile tile = GetTileAt(i, j);

		if (tile == null || tile.physics.mode == PhysicsMode.Kinematic)
		{
			return;
		}

		tile.physics.canCollideBottom = (tile.canCollideBottom && !IsSolid(i, j + 1, false));
		tile.physics.canCollideTop = (tile.canCollideTop && !IsSolid(i, j - 1, false));
		tile.physics.canCollideLeft = (tile.canCollideLeft && !IsSolid(i - 1, j, false));
		tile.physics.canCollideRight = (tile.canCollideRight && !IsSolid(i + 1, j, false));
	}

	public void UpdateEdgesAround(int i, int j)
	{
		for (int x = i - 1; x <= i + 1; x++)
		{
			for (int y = j - 1; y <= j + 1; y++)
			{
				UpdateEdgesAt(x, y);
			}
		}
	}

	public void StartScreenShake()
	{
		_screenShakeStartTimestamp = Time.time;
		_screenShakeStarted = true;
	}

	private void UpdateScreenShake()
	{
		float z = Camera.main.transform.localPosition.z;
		Camera.main.transform.localPosition = Random.insideUnitCircle * screenShakeRadius;
		Camera.main.transform.localPosition += Vector3.forward * z;
	}

	private void RestoreScreenShake()
	{
		Camera.main.transform.localPosition = Vector3.forward * Camera.main.transform.localPosition.z;
	}

	void OnDrawGizmos()
	{
		if (currentMap != null && debugDrawAnything)
		{
			Gizmos.color = Color.white;

			Gizmos.DrawWireCube(
				new Vector3((currentMap.left + currentMap.right) * 0.5f, (currentMap.top + currentMap.bottom) * 0.5f, 0),
				new Vector3((currentMap.right - currentMap.left), (currentMap.top - currentMap.bottom), 1)
				);

			// draw edges
			{
				Gizmos.color = Color.magenta;

				foreach (MapTile tile in currentMap.GetTiles())
				{
					if ((tile != null) && (tile.physics.mode != PhysicsMode.Kinematic))
					{
						AABB box = tile.physics.globalHitBox;

						if (tile.physics.canCollideTop)
						{
							Gizmos.DrawLine(box.topLeft, box.topRight);
						}
						if (tile.physics.canCollideBottom)
						{
							Gizmos.DrawLine(box.bottomLeft, box.bottomRight);
						}
						if (tile.physics.canCollideLeft)
						{
							Gizmos.DrawLine(box.topLeft, box.bottomLeft);
						}
						if (tile.physics.canCollideRight)
						{
							Gizmos.DrawLine(box.bottomRight, box.topRight);
						}
					}
				}
			}
		}
	}
}
