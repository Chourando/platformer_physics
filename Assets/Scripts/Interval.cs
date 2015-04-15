using UnityEngine;
using System.Collections;


public class Interval
{
	public float min
	{
		get
		{
			return _min;
		}
		set
		{
			_min = value;
			CheckValid();
		}
	}
	public float max
	{
		get
		{
			return _max;
		}
		set
		{
			_max = value;
			CheckValid();
		}
	}
	private float _min, _max;

	public bool isValid { get { return _isValid; } }
	private bool _isValid;

	private void CheckValid()
	{
		if (float.IsNaN(_min) || float.IsNaN(_max) || (_min > _max))
		{
			_isValid = false;
		}
	}

	public static Interval Invalid { get { return new Interval(); } }

	public Interval()
	{
		_min = float.NaN;
		_max = float.NaN;
		_isValid = false;
	}

	public Interval(float min, float max)
	{
		if (min <= max)
		{
			_min = min;
			_max = max;
			_isValid = true;
		}
		else
		{
			_min = float.NaN;
			_max = float.NaN;
			_isValid = false;
		}
	}

	public override string ToString()
	{
		if (_isValid)
		{
			return "(" + _min + "," + _max + ")";
		}
		else
		{
			return "(invalid)";
		}
	}

	public void Translate(float incr)
	{
		if (!_isValid)
		{
			return;
		}

		min += incr;
		max += incr;
	}

	public Interval Translated(float incr)
	{
		if (!_isValid)
		{
			return Interval.Invalid;
		}

		return new Interval(min + incr, max + incr);
	}

	public static Interval Intersection(Interval i1, Interval i2, bool strict = false)
	{
		if (!Overlapping(i1, i2, strict))
		{
			return Interval.Invalid;
		}

		return new Interval(Mathf.Max(i1.min, i2.min), Mathf.Min(i1.max, i2.max));
	}

	public static Interval Union(Interval i1, Interval i2, bool strict = false)
	{
		if (!Overlapping(i1, i2, strict))
		{
			return Interval.Invalid;
		}

		return new Interval(Mathf.Min(i1.min, i2.min), Mathf.Max(i1.max, i2.max));
	}

	public static bool Overlapping(Interval i1, Interval i2, bool strict = false)
	{
		if (strict)
		{
			return !(i1.max <= i2.min || i1.min >= i2.max);
		}
		else
		{
			return !(i1.max < i2.min || i1.min > i2.max);
		}
	}

	public static void SweepTest(Interval i1, Interval i2, float speed, out bool willOverlap, out Interval overlapTimeInterval)
	{
		overlapTimeInterval = new Interval();

		if (Interval.Overlapping(i1, i2))
		{
			overlapTimeInterval.min = 0;

			if (speed < 0)
			{
				// going left

				// i1.max + speed * overlapTimeInterval.max = i2.min
				overlapTimeInterval.max = (i2.min - i1.max) / speed;
			}
			else if (speed > 0)
			{
				// going right

				// i1.min + speed * overlapTimeInterval.max = i2.max
				overlapTimeInterval.max = (i2.max - i1.min) / speed;
			}
			else
			{
				overlapTimeInterval.max = float.PositiveInfinity;
			}

			willOverlap = true;
			return;
		}
		else
		{
			if (speed == 0)
			{
				willOverlap = false;
				overlapTimeInterval = Interval.Invalid;
				return;
			}

			bool isRight = (i1.min > i2.max);

			if (isRight)
			{
				if (speed > 0)
				{
					// divergent
					willOverlap = false;
					overlapTimeInterval = Interval.Invalid;
					return;
				}
				else
				{
					overlapTimeInterval.min = (i2.max - i1.min) / speed;
					overlapTimeInterval.max = (i2.min - i1.max) / speed;

					willOverlap = true;
					return;
				}
			}
			else
			{
				if (speed < 0)
				{
					// divergent
					willOverlap = false;
					overlapTimeInterval = Interval.Invalid;
					return;
				}
				else
				{
					overlapTimeInterval.min = (i2.min - i1.max) / speed;
					overlapTimeInterval.max = (i2.max - i1.min) / speed;

					willOverlap = true;
					return;
				}
			}
		}
	}
}

