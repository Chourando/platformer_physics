using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelMap
{
	#region test maps
	//public static readonly string[] TEST_MAP_0 = 
	//{
	//	"                                                    ",
	//	"                                          o         ",
	//	"                                                    ",
	//	"                  ¨                                 ",
	//	"    T       T    ¨                  o     o      www",
	//	"    l     ¨wL¨    ¨       ww                     www",
	//	"    l     ¨wl    ¨¨¨¨¨           p        o   wwwwww",
	//	"          ¨wl     ¨      www                     www",
	//	"          ¨wl    ¨  tt   wwwwwwwwwwwwwwww  w     www",
	//	"wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww^w^wwwwwww",
	//	"wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
	//	"wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww"
	//};
	public static readonly string[] TEST_MAP_0 = 
	{
	    "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
		"w                                     w              w",
		"w           wwwwwwwwwwww     wwwwwwwwww    o         w",
		"w                                                    w",
		"w           o      C        w                        w",
	    "w    T       T    C                  o            wwww",
	    "w    l     DwLF    C        w   b                 wwww",
	    "w    l     Dwl    DEEEF            b        o  wwwwwww",
	    "w          Dwl     C        wp                    wwww",
	    "w          Dwl    C  tt   w  wwwwwwwwwwwww  w     wwww",
	    "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
	    "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
	    "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww"
	};

	public static readonly string[] TEST_MAP_1 = 
	{
	    " p ",
	    "   ",
	    " w "
	};

	public static readonly string[] TEST_MAP_2 = 
	{
	    "   p ",
	    "     ",
	    " w   "
	};

	public static readonly string[] TEST_MAP_3 = 
	{
	    "w p w  ",
	    "w   w  ",
	    " wwww  "
	};
	#endregion

	public int width;
	public int height;

	private MapTile[,] _tiles;
	public SpatialHash spatialHash;

	public float left, right, bottom, top;

	public GameObject mapObj;

	private bool IsWall(string[] definition, int i, int j)
	{
		if (i < 0 || i >= width || j < 0 || j >= height)
		{
			return false;
		}

		char c = definition[j].ToLower()[i];

		return "w".Contains("" + c);
	}

	public LevelMap()
	{
	}

	public Vector3 PositionAt(int i, int j)
	{
		return Vector3.right * (i + 0.5f) + Vector3.up * (-j - 0.5f);
	}

	public void Init(string[] definition)
	{
		width = definition[0].Length;
		height = definition.Length;

		_tiles = new MapTile[width, height];
		left = 0;
		right = left + width;
		top = 0;
		bottom = top - height;

		spatialHash = new SpatialHash(width, height, AABB.FromLRBT(left, right, bottom, top));

		Vector3 currentPosition;

		mapObj = new GameObject("Map");
		mapObj.transform.position = Vector3.zero;
		mapObj.transform.localScale = Vector3.one;
		mapObj.transform.localRotation = Quaternion.identity;

		GameObject bgObj = new GameObject("Background");
		bgObj.transform.parent = mapObj.transform;
		bgObj.transform.position = Vector3.zero;
		bgObj.transform.localScale = Vector3.one;
		bgObj.transform.localRotation = Quaternion.identity;


		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				currentPosition = PositionAt(i, j);
				_tiles[i, j] = null;

				GameObject bgTileObj = (GameObject)GameObject.Instantiate(GameManager.instance.backgroundPrefab, currentPosition, Quaternion.identity);
				bgTileObj.transform.parent = bgObj.transform;
				bgTileObj.isStatic = true;

				switch (definition[j][i])
				{
					case ' ':
						break;

					case 'p': // player
						if (GameManager.instance.player == null)
						{
							GameObject playerObj = (GameObject)GameObject.Instantiate(GameManager.instance.playerPrefab, currentPosition, Quaternion.identity);
							playerObj.name = "Player";

							GameManager.instance.player = playerObj.GetComponent<CharacterController>();

							Camera.main.transform.parent = playerObj.transform;
							Camera.main.transform.localPosition = Vector3.back;

							spatialHash.AddObject(playerObj.GetComponent<PhysicsObject>());
						}
						else
						{
							throw new System.Exception("two players?");
						}
						break;

					case 'o': // rock
						{
							GameObject rockObj = (GameObject)GameObject.Instantiate(GameManager.instance.rockPrefab, currentPosition, Quaternion.identity);
							rockObj.name = "Rock";

							rockObj.transform.position += Vector3.right * Random.Range(-0.2f, 0.2f);

							spatialHash.AddObject(rockObj.GetComponent<PhysicsObject>());
						}
						break;

					case 'b': // boulder
						{
							GameObject boulderObj = (GameObject)GameObject.Instantiate(GameManager.instance.boulderPrefab, currentPosition, Quaternion.identity);
							boulderObj.name = "Boulder";

							spatialHash.AddObject(boulderObj.GetComponent<PhysicsObject>());
						}
						break;

					case 'w':
						{
							// 0 1 2
							// 7 x 3
							// 6 5 4
							bool[] neighbors = new bool[]
							{
								IsWall(definition, i-1, j-1),
								IsWall(definition, i,   j-1),
								IsWall(definition, i+1, j-1),

								IsWall(definition, i+1, j),

								IsWall(definition, i+1, j+1),
								IsWall(definition, i,   j+1),
								IsWall(definition, i-1, j+1),

								IsWall(definition, i-1, j)
							};

							GameObject wallObj = (GameObject)GameObject.Instantiate(GameManager.instance.floorPrefab, currentPosition, Quaternion.identity);

							wallObj.transform.Find("Sides/N").gameObject.SetActive(!neighbors[1]);
							wallObj.transform.Find("Sides/E").gameObject.SetActive(!neighbors[3]);
							wallObj.transform.Find("Sides/S").gameObject.SetActive(!neighbors[5]);
							wallObj.transform.Find("Sides/W").gameObject.SetActive(!neighbors[7]);

							wallObj.transform.Find("Corners/NW").gameObject.SetActive(neighbors[1] && neighbors[7] && !neighbors[0]);
							wallObj.transform.Find("Corners/NE").gameObject.SetActive(neighbors[3] && neighbors[1] && !neighbors[2]);
							wallObj.transform.Find("Corners/SE").gameObject.SetActive(neighbors[5] && neighbors[3] && !neighbors[4]);
							wallObj.transform.Find("Corners/SW").gameObject.SetActive(neighbors[7] && neighbors[5] && !neighbors[6]);

							MapTile mapTile = wallObj.GetComponent<MapTile>();
							_tiles[i, j] = mapTile;

							mapTile.x = i;
							mapTile.y = j;

							wallObj.name = "(" + i + "," + j + ") Wall";

							wallObj.transform.parent = mapObj.transform;

							mapTile.type = MapTile.Type.Wall;

							spatialHash.AddObject(mapTile.physics, true);
						}
						break;

					case 'C':
					case 'D':
					case 'E':
					case 'F':
						{
							GameObject wallObj = (GameObject)GameObject.Instantiate(GameManager.instance.onewayPrefab, currentPosition, Quaternion.identity);

							MapTile mapTile = wallObj.GetComponent<MapTile>();
							_tiles[i, j] = mapTile;

							mapTile.x = i;
							mapTile.y = j;

							wallObj.name = "(" + i + "," + j + ") Wall Oneway";
							wallObj.transform.parent = mapObj.transform;

							mapTile.type = MapTile.Type.OneWayWall;

							spatialHash.AddObject(mapTile.physics, true);
						}
						break;

					case 't':
						{
							GameObject triggerObj = (GameObject)GameObject.Instantiate(GameManager.instance.triggerPrefab, currentPosition, Quaternion.identity);

							MapTile mapTile = triggerObj.GetComponent<MapTile>();
							_tiles[i, j] = mapTile;

							mapTile.x = i;
							mapTile.y = j;

							triggerObj.name = "(" + i + "," + j + ") Trigger";

							triggerObj.transform.parent = mapObj.transform;

							mapTile.type = MapTile.Type.Trigger;
							mapTile.physics.mode = PhysicsMode.Trigger;

							mapTile.physics.priority = PhysicsObject.TRIGGER_PRIORITY;

							mapTile.sprite.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.25f);

							spatialHash.AddObject(mapTile.physics, true);
						}
						break;

					case 'l':
						{
							GameObject triggerObj = (GameObject)GameObject.Instantiate(GameManager.instance.ladderPrefab, currentPosition, Quaternion.identity);

							MapTile mapTile = triggerObj.GetComponent<MapTile>();
							_tiles[i, j] = mapTile;

							mapTile.x = i;
							mapTile.y = j;

							triggerObj.name = "(" + i + "," + j + ") Ladder";

							triggerObj.transform.parent = mapObj.transform;

							mapTile.type = MapTile.Type.Climbable;
							mapTile.physics.mode = PhysicsMode.Trigger;
							mapTile.physics.triggerMode = TriggerMode.WholeArea;

							mapTile.physics.priority = PhysicsObject.TRIGGER_PRIORITY;

							spatialHash.AddObject(mapTile.physics, true);
						}
						break;

					case 'T':
						{
							GameObject triggerObj = (GameObject)GameObject.Instantiate(GameManager.instance.ladderTopPrefab, currentPosition, Quaternion.identity);

							MapTile mapTile = triggerObj.GetComponent<MapTile>();
							_tiles[i, j] = mapTile;

							mapTile.x = i;
							mapTile.y = j;

							triggerObj.name = "(" + i + "," + j + ") Ladder End";

							triggerObj.transform.parent = mapObj.transform;

							mapTile.type = MapTile.Type.Climbable;
							mapTile.physics.mode = PhysicsMode.Trigger;
							mapTile.physics.triggerMode = TriggerMode.WholeArea;

							mapTile.physics.priority = PhysicsObject.TRIGGER_PRIORITY;

							spatialHash.AddObject(mapTile.physics, true);
						}
						break;

					case 'L':
						{
							GameObject triggerObj = (GameObject)GameObject.Instantiate(GameManager.instance.ladderPrefab, currentPosition, Quaternion.identity);
							{
								MapTile mapTile = triggerObj.GetComponent<MapTile>();
								_tiles[i, j] = mapTile;

								triggerObj.name = "(" + i + "," + j + ") Ladder";

								triggerObj.transform.parent = mapObj.transform;

								mapTile.type = MapTile.Type.Climbable;
								mapTile.physics.mode = PhysicsMode.Trigger;
								mapTile.physics.triggerMode = TriggerMode.WholeArea;

								mapTile.physics.priority = PhysicsObject.TRIGGER_PRIORITY;

								spatialHash.AddObject(mapTile.physics, true);
							}

							GameObject platformObj = (GameObject)GameObject.Instantiate(GameManager.instance.onewayPrefab, currentPosition, Quaternion.identity);
							{
								MapTile mapTile = platformObj.GetComponent<MapTile>();
								_tiles[i, j] = mapTile;

								mapTile.x = i;
								mapTile.y = j;

								platformObj.name = "(" + i + "," + j + ") Ladder Top";

								platformObj.transform.parent = mapObj.transform;

								mapTile.type = MapTile.Type.OneWayWall;

								spatialHash.AddObject(mapTile.physics, true);
							}
						}
						break;

					default:
						throw new System.NotImplementedException();
				}
			}
		}

		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				GameManager.instance.UpdateEdgesAt(i, j);
			}
		}
	}

	public bool IsInBounds(int i, int j)
	{
		return !(i < 0 || i >= width || j < 0 || j >= height);
	}

	public MapTile GetTileAt(int i, int j)
	{
		if (!IsInBounds(i, j))
		{
			return null;
		}

		return _tiles[i, j];
	}

	public MapTile[,] GetTiles()
	{
		return _tiles;
	}
}
