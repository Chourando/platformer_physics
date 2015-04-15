using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AABB
{
	public enum Axis
	{
		Horizontal,
		Vertical,
		None
	}

	public enum Direction
	{
		Top,
		Bottom,
		Left, 
		Right
	}

	public float x = 0;
	public float y = 0;
	public float hw = 0.5f;
	public float hh = 0.5f;

	public Interval IntervalX { get { return new Interval(left, right); } }
	public Interval IntervalY { get { return new Interval(bottom, top); } }

	public Vector3 topLeft { get { return new Vector3(left, top); } }
	public Vector3 topRight { get { return new Vector3(right, top); } }
	public Vector3 bottomLeft { get { return new Vector3(left, bottom); } }
	public Vector3 bottomRight { get { return new Vector3(right, bottom); } }

	public float top { get { return y + hh; } }
	public float bottom { get { return y - hh; } }
	public float left { get { return x - hw; } }
	public float right  { get { return x + hw; } }

	public AABB(float x, float y, float hw, float hh)
	{
		this.x = x;
		this.y = y;
		this.hw = hw;
		this.hh = hh;
	}

	public static AABB FromLRBT(float left, float right, float bottom, float top)
	{
		float hw = 0.5f * (right - left);
		float hh = 0.5f * (top - bottom);

		return new AABB(left + hw, bottom + hh, hw, hh);
	}

	public override string ToString()
	{
		return "AABB((" + x + "," + y + ")-(" + hw + "," + hh + "))";
	}

#pragma warning disable 0162
	public static void SweepTest(AABB box1, AABB box2, Direction side, Vector2 velocity, float deltaTime,
		out bool willOverlap, out float overlapTime, out Axis overlapAxis, out Vector3 overlapNormal)
	{
		switch (side)
		{
			case Direction.Top:
				{
					if (velocity.y >= 0 || box1.top <= box2.top)
					{
						// will never collide
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					// box1.bottom + velocity.y * t = box2.top
					float t = (box2.top - box1.bottom) / velocity.y;

					if (t <= 0)
					{
						// already overlapping on Y

						if (Interval.Overlapping(box2.IntervalX, box1.IntervalX, true))
						{
							// also already overlapping on X
							willOverlap = true;
							overlapTime = 0;
							overlapAxis = Axis.Vertical;
							overlapNormal = Vector3.up;
							return;
						}
						else
						{
							// not already overlapping on X: ignore
							willOverlap = false;
							overlapTime = float.NaN;
							overlapAxis = Axis.None;
							overlapNormal = Vector3.zero;
							return;
						}
					}

					if (t > deltaTime)
					{
						// too late
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					if (Interval.Overlapping(box2.IntervalX, box1.IntervalX.Translated(velocity.x * t), true))
					{
						// they will hit!
						willOverlap = true;
						overlapTime = t * deltaTime;
						overlapAxis = Axis.Vertical;
						overlapNormal = Vector3.up;
						return;
					}
					else
					{
						// they don't hit
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}
				}
				break;

			case Direction.Bottom:
				{
					if (velocity.y <= 0 || box1.bottom >= box2.bottom)
					{
						// will never collide
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					// box1.top + velocity.y * t = box2.bottom
					float t = (box2.bottom - box1.top) / velocity.y;

					if (t <= 0)
					{
						// already overlapping on Y

						if (Interval.Overlapping(box2.IntervalX, box1.IntervalX, true))
						{
							// also already overlapping on X
							willOverlap = true;
							overlapTime = 0;
							overlapAxis = Axis.Vertical;
							overlapNormal = Vector3.down;
							return;
						}
						else
						{
							// not already overlapping on X: ignore
							willOverlap = false;
							overlapTime = float.NaN;
							overlapAxis = Axis.None;
							overlapNormal = Vector3.zero;
							return;
						}
					}

					if (t > deltaTime)
					{
						// too late
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					if (Interval.Overlapping(box2.IntervalX, box1.IntervalX.Translated(velocity.x * t), true))
					{
						// they will hit!
						willOverlap = true;
						overlapTime = t * deltaTime;
						overlapAxis = Axis.Vertical;
						overlapNormal = Vector3.down;
						return;
					}
					else
					{
						// they don't hit
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}
				}
				break;

			case Direction.Right:
				{
					//Debug.Log("@@" + velocity.x + " " + box1.right + " " + box2.right);
					if (velocity.x >= 0 || box1.right <= box2.right)
					{
						// will never collide
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					// box1.left + velocity.x * t = box2.right
					float t = (box2.right - box1.left) / velocity.x;

					if (t <= 0)
					{
						// already overlapping on X

						if (Interval.Overlapping(box2.IntervalY, box1.IntervalY, true))
						{
							// also already overlapping on Y
							willOverlap = true;
							overlapTime = 0;
							overlapAxis = Axis.Horizontal;
							overlapNormal = Vector3.right;
							return;
						}
						else
						{
							// not already overlapping on Y: ignore
							willOverlap = false;
							overlapTime = float.NaN;
							overlapAxis = Axis.None;
							overlapNormal = Vector3.zero;
							return;
						}
					}

					if (t > deltaTime)
					{
						// too late
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					if (Interval.Overlapping(box2.IntervalY, box1.IntervalY.Translated(velocity.y * t), true))
					{
						// they will hit!
						willOverlap = true;
						overlapTime = t * deltaTime;
						overlapAxis = Axis.Horizontal;
						overlapNormal = Vector3.right;
						return;
					}
					else
					{
						// they don't hit
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}
				}
				break;

			case Direction.Left:
				{
					if (velocity.x <= 0 || box1.left >= box2.left)
					{
						// will never collide
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					// box1.right + velocity.x * t = box2.left
					float t = (box2.left - box1.right) / velocity.x;

					if (t <= 0)
					{
						// already overlapping on X

						if (Interval.Overlapping(box2.IntervalY, box1.IntervalY, true))
						{
							// also already overlapping on Y
							willOverlap = true;
							overlapTime = 0;
							overlapAxis = Axis.Horizontal;
							overlapNormal = Vector3.left;
							return;
						}
						else
						{
							// not already overlapping on Y: ignore
							willOverlap = false;
							overlapTime = float.NaN;
							overlapAxis = Axis.None;
							overlapNormal = Vector3.zero;
							return;
						}
					}

					if (t > deltaTime)
					{
						// too late
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}

					if (Interval.Overlapping(box2.IntervalY, box1.IntervalY.Translated(velocity.y * t), true))
					{
						// they will hit!
						willOverlap = true;
						overlapTime = t * deltaTime;
						overlapAxis = Axis.Horizontal;
						overlapNormal = Vector3.left;
						return;
					}
					else
					{
						// they don't hit
						willOverlap = false;
						overlapTime = float.NaN;
						overlapAxis = Axis.None;
						overlapNormal = Vector3.zero;
						return;
					}
				}
				break;

			default:
				throw new System.Exception("invalid side?");
		}
	}

	// used only for testing!
	public static void SweepTest(AABB box1, AABB box2, Vector2 velocity, float deltaTime,
		out bool willOverlap, out float overlapTime, out Axis overlapAxis, out Vector3 overlapNormal)
	{
		Interval box1X = box1.IntervalX;
		Interval box1Y = box1.IntervalY;
		Interval box2X = box2.IntervalX;
		Interval box2Y = box2.IntervalY;

		bool willOverlapX, willOverlapY;
		Interval overlapTimeIntervalX, overlapTimeIntervalY;

		Interval.SweepTest(box1X, box2X, velocity.x, out willOverlapX, out overlapTimeIntervalX);

		if (!willOverlapX || overlapTimeIntervalX.min > deltaTime)
		{
			// doesn't overlap on x (or too late)
			willOverlap = false;
			overlapTime = float.NaN;
			overlapAxis = Axis.None;
			overlapNormal = Vector3.zero;
			return;
		}

		Interval.SweepTest(box1Y, box2Y, velocity.y, out willOverlapY, out overlapTimeIntervalY);

		if (!willOverlapY || overlapTimeIntervalY.min > deltaTime)
		{
			// doesn't overlap on y (or too late)
			Debug.Log("no overlap y");
			willOverlap = false;
			overlapTime = float.NaN;
			overlapAxis = Axis.None;
			overlapNormal = Vector3.zero;
			return;
		}

		Interval overlapTimeInterval = Interval.Intersection(overlapTimeIntervalX, overlapTimeIntervalY);

		if (!overlapTimeInterval.isValid)
		{
			// intervals don't intersect
			Debug.Log("no intersect xy");
			willOverlap = false;
			overlapTime = float.NaN;
			overlapAxis = Axis.None;
			overlapNormal = Vector3.zero;
			return;
		}

		willOverlap = true;

		overlapTime = overlapTimeInterval.min;

		if (overlapTimeIntervalX.min < overlapTimeIntervalY.min)
		{
			overlapAxis = Axis.Vertical;

			if (velocity.y > 0)
			{
				overlapNormal = Vector3.down;
			}
			else
			{
				overlapNormal = Vector3.up;
			}
		}
		else
		{
			overlapAxis = Axis.Horizontal;

			if (velocity.x > 0)
			{
				overlapNormal = Vector3.left;
			}
			else
			{
				overlapNormal = Vector3.right;
			}
		}
	}

	public static bool Overlap(AABB box1, AABB box2, bool includeBoundaries)
	{
		if (includeBoundaries)
		{
			return !(
				(box1.right < box2.left) ||
				(box2.right < box1.left) ||
				(box1.top < box2.bottom) ||
				(box2.top < box1.bottom)
			);
		}
		else
		{
			return !(
				(box1.right <= box2.left) ||
				(box2.right <= box1.left) ||
				(box1.top <= box2.bottom) ||
				(box2.top <= box1.bottom)
			);
		}
	}

	public bool Contains(Vector3 point, bool includeBoundaries)
	{
		if (includeBoundaries)
		{
			return !(point.x < left || point.x > right || point.y < bottom || point.y > top);
		}
		else
		{
			return !(point.x <= left || point.x >= right || point.y <= bottom || point.y >= top);
		}
	}

	public AABB TranslatedCopy(Vector2 translation)
	{
		return new AABB(x + translation.x, y + translation.y, hw, hh);
	}

	public void DebugDraw(Color color, bool drawCenter = false)
	{
		Debug.DrawLine(new Vector3(x - hw, y - hh), new Vector3(x + hw, y - hh), color);
		Debug.DrawLine(new Vector3(x + hw, y - hh), new Vector3(x + hw, y + hh), color);
		Debug.DrawLine(new Vector3(x + hw, y + hh), new Vector3(x - hw, y + hh), color);
		Debug.DrawLine(new Vector3(x - hw, y + hh), new Vector3(x - hw, y - hh), color);

		float halfRatio = 0.2f;

		Debug.DrawLine(new Vector3(x - hw * halfRatio, y), new Vector3(x + hw * halfRatio, y), color);
		Debug.DrawLine(new Vector3(x, y - hh * halfRatio), new Vector3(x, y + hh * halfRatio), color);
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(AABB))]
public class AABBDrawer : PropertyDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		SerializedProperty x = prop.FindPropertyRelative("x");
		SerializedProperty y = prop.FindPropertyRelative("y");
		SerializedProperty hw = prop.FindPropertyRelative("hw");
		SerializedProperty hh = prop.FindPropertyRelative("hh");

		label = EditorGUI.BeginProperty(pos, label, prop);

		Rect contentPos = EditorGUI.PrefixLabel(pos, label);

		float totalWidth = contentPos.width;
		float margin = 5.0f;

		float eachWidth = (totalWidth - 4 * margin) / 4;

		contentPos.width = eachWidth;
		EditorGUIUtility.labelWidth = 22.0f;
		EditorGUI.indentLevel = 0;
		EditorGUI.PropertyField(contentPos, x);
		contentPos.x += eachWidth + margin;
		EditorGUI.PropertyField(contentPos, y);
		contentPos.x += eachWidth + margin;
		EditorGUI.PropertyField(contentPos, hw);
		contentPos.x += eachWidth + margin;
		EditorGUI.PropertyField(contentPos, hh);

		EditorGUI.EndProperty();
	}
}
#endif