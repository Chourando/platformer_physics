using UnityEngine;
using System.Collections;

public class SweepTesting : MonoBehaviour
{
	public AABB box1 = new AABB(0, 0, 1, 1);
	public AABB box2 = new AABB(2, 2, 1, 1);

	public Vector2 velocity = new Vector2(-1, -1);
	public float deltaTime;

	private static readonly Color COLOR_BOX1 = new Color(0, 1, 1, .8f);
	private static readonly Color COLOR_BOX2 = new Color(1, 0, 0, .8f);
	private static readonly Color COLOR_BOX1_GHOST = new Color(0, 1, 1, .3f);

	void OnDrawGizmos()
	{
		Gizmos.color = new Color(0, 0, 0, 0.6f);
		for (int i = -2; i <= 5; i++)
		{
			for (int j = -2; j >= 5; j++)
			{
				Gizmos.DrawSphere(new Vector3(i, j, 0), 0.6f);
			}
		}

		Gizmos.color = COLOR_BOX1;
		Gizmos.DrawCube(new Vector3(box1.x, box1.y), new Vector3(2 * box1.hw, 2 * box1.hh, 1));

		Gizmos.color = COLOR_BOX2;
		Gizmos.DrawCube(new Vector3(box2.x, box2.y), new Vector3(2 * box2.hw, 2 * box2.hh, 1));

		Gizmos.color = COLOR_BOX1_GHOST;

		Gizmos.DrawLine(new Vector3(box1.x - box1.hw, box1.y - box1.hh), new Vector3(box1.x - box1.hw + velocity.x * deltaTime, box1.y - box1.hh + velocity.y * deltaTime));
		Gizmos.DrawLine(new Vector3(box1.x - box1.hw, box1.y + box1.hh), new Vector3(box1.x - box1.hw + velocity.x * deltaTime, box1.y + box1.hh + velocity.y * deltaTime));
		Gizmos.DrawLine(new Vector3(box1.x + box1.hw, box1.y - box1.hh), new Vector3(box1.x + box1.hw + velocity.x * deltaTime, box1.y - box1.hh + velocity.y * deltaTime));
		Gizmos.DrawLine(new Vector3(box1.x + box1.hw, box1.y + box1.hh), new Vector3(box1.x + box1.hw + velocity.x * deltaTime, box1.y + box1.hh + velocity.y * deltaTime));

		Gizmos.DrawWireCube(new Vector3(box1.x + velocity.x * deltaTime, box1.y + velocity.y * deltaTime), new Vector3(2 * box1.hw, 2 * box1.hh, 1));

		bool willOverlap;
		float overlapTime;
		AABB.Axis overlapAxis;
		Vector3 overlapNormal;
		AABB.SweepTest(box1, box2, velocity, deltaTime, out willOverlap, out overlapTime, out overlapAxis, out overlapNormal);
		{
			Gizmos.color = willOverlap ? Color.green : Color.red;
			Gizmos.DrawCube(new Vector3(5, -5), Vector3.one);

			if (willOverlap)
			{
				Gizmos.color = COLOR_BOX1_GHOST;
				Gizmos.DrawCube(new Vector3(box1.x + velocity.x * overlapTime, box1.y + velocity.y * overlapTime), new Vector3(2 * box1.hw, 2 * box1.hh, 1));

				Vector3 beforePos = new Vector3(box1.x, box1.y, 0);
				Vector3 middlePos = beforePos + new Vector3(velocity.x, velocity.y) * overlapTime;
				Vector3 afterPos = beforePos + new Vector3(velocity.x, velocity.y) * deltaTime;

				Vector3 sweepDistance = afterPos - middlePos;

				Vector3 afterSlidePos = Vector3.zero;
				switch (overlapAxis)
				{
					case AABB.Axis.Horizontal:
						{
							// slide
							afterSlidePos = middlePos + Vector3.up * Vector3.Dot(sweepDistance, Vector3.up);

							Debug.Log(Time.frameCount + "H");
						}
						break;

					case AABB.Axis.Vertical:
						{
							// slide
							afterSlidePos = middlePos + Vector3.right * Vector3.Dot(sweepDistance, Vector3.right);
						}
						break;

					case AABB.Axis.None:
						return;
				}

				Gizmos.DrawCube(new Vector3(afterSlidePos.x, afterSlidePos.y), new Vector3(2 * box1.hw, 2 * box1.hh, 1));


				//Debug.Log((overlapAxis == AABB.Axis.Horizontal) ? "H" : "V");
			}
		}
	}
}
