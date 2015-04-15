using UnityEngine;
using System.Collections;

public class MapTile : MonoBehaviour
{
	public enum Type
	{
		Wall,
		OneWayWall,
		TowerBase,
		Trigger,
		Climbable
	}

	public int x, y;
	public Type type;

	public GameObject sprite = null;
	public PhysicsObject physics = null;

	public bool canCollideLeft = true;
	public bool canCollideRight = true;
	public bool canCollideTop = true;
	public bool canCollideBottom = true;

	void Awake()
	{
		Transform spriteTransform = transform.Find("Sprite");

		if (spriteTransform != null)
		{
			sprite = spriteTransform.gameObject;
		}

		physics = this.GetComponent<PhysicsObject>();
	}
}
