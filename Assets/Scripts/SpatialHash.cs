#define DO_SANITY_CHECKS

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/*
 *	Each object is in all the buckets it overlaps.
 */
public class SpatialHash
{
	private Dictionary<int, HashSet<PhysicsObject>> _buckets = new Dictionary<int, HashSet<PhysicsObject>>();
	private Dictionary<PhysicsObject, HashSet<int>> _reverseDict = new Dictionary<PhysicsObject, HashSet<int>>();
	private int _numCols;
	private int _numRows;

	private AABB _boundingBox;

	private int NumBuckets { get { return _numCols * _numRows; } }

	public SpatialHash(int numCols, int numRows, AABB boundingBox)
	{
		_numCols = numCols;
		_numRows = numRows;
		_boundingBox = boundingBox;

		// init buckets
		{
			for (int i = 0; i < NumBuckets; i++)
			{
				_buckets.Add(i, new HashSet<PhysicsObject>());
			}
		}
	}

	// forceJustCenter should only be used when it's certain that the object is exactly one bucket (as in map tiles).
	//   this way we make sure that they don't span more than one bucket (which could happen because of numerical errors).
	public void AddObject(PhysicsObject obj, bool forceJustCenter = false)
	{
#if DO_SANITY_CHECKS
		if (_reverseDict.ContainsKey(obj))
		{
			throw new System.Exception("Object already accounted for");
		}
#endif

		if (forceJustCenter)
		{
			int bucketID = GetCenterBucketID(obj);

			_buckets[bucketID].Add(obj);
			_reverseDict.Add(obj, new HashSet<int>());
			_reverseDict[obj].Add(bucketID);
		}
		else
		{
			HashSet<int> intersectingBuckets = GetIntersectingBuckets(obj.globalHitBox);

			foreach (int id in intersectingBuckets)
			{
				_buckets[id].Add(obj);
			}

			_reverseDict.Add(obj, intersectingBuckets);
		}
	}

	public void UpdateObject(PhysicsObject obj, bool forceJustCenter = false)
	{
		HashSet<int> currentBuckets;

		if (forceJustCenter)
		{
			int bucketID = GetCenterBucketID(obj);

			currentBuckets = new HashSet<int>();
			currentBuckets.Add(bucketID);
		}
		else
		{
			currentBuckets = GetIntersectingBuckets(obj.globalHitBox);
		}

		HashSet<int> bucketsNoLongerIntersecting = new HashSet<int>();
		HashSet<int> bucketsNewlyIntersecting = new HashSet<int>();

		HashSet<int> previousBuckets = _reverseDict[obj];

		// compute which buckets it joined
		foreach (int id in currentBuckets)
		{
			if (!previousBuckets.Contains(id))
			{
				bucketsNewlyIntersecting.Add(id);
			}
		}

		// compute which buckets it left
		foreach (int id in previousBuckets)
		{
			if (!currentBuckets.Contains(id))
			{
				bucketsNoLongerIntersecting.Add(id);
			}
		}

		// leave buckets
		foreach (int id in bucketsNoLongerIntersecting)
		{
			_buckets[id].Remove(obj);
			_reverseDict[obj].Remove(id);
		}

		// join buckets
		foreach (int id in bucketsNewlyIntersecting)
		{
			_buckets[id].Add(obj);
			_reverseDict[obj].Add(id);
		}
	}

	public void RemoveObject(PhysicsObject obj)
	{
#if DO_SANITY_CHECKS
		if (!_reverseDict.ContainsKey(obj))
		{
			throw new System.Exception("Can't remove an obj that's not accounted for.");
		}
#endif
		foreach (int id in _reverseDict[obj])
		{
			_buckets[id].Remove(obj);
		}

		_reverseDict.Remove(obj);
	}

	private int GetCenterBucketID(PhysicsObject obj)
	{
		Vector2 center = obj.globalCenter;

		int i = WorldToBucket_i(center.x);
		int j = WorldToBucket_j(center.y);

		i = Mathf.Max(Mathf.Min(i, _numCols - 1), 0);
		j = Mathf.Max(Mathf.Min(j, _numRows - 1), 0);

		return GetBucketID(i, j);
	}

	public IEnumerable<PhysicsObject> GetPotentialOverlaps(PhysicsObject obj)
	{
		AABB objBox = obj.globalHitBox;

		int minX = WorldToBucket_i(objBox.left);
		int maxX = WorldToBucket_i(objBox.right);
		int minY = WorldToBucket_j(objBox.bottom);
		int maxY = WorldToBucket_j(objBox.top);

		foreach (PhysicsObject potentialObj in GetPotentialOverlaps(minX, maxX, minY, maxY))
		{
			if (potentialObj != obj)
			{
				yield return potentialObj;
			}
		}
	}

	public IEnumerable<PhysicsObject> GetPotentialOverlaps(PhysicsObject obj, Vector2 potentialTranslation)
	{
		AABB objBox = obj.globalHitBox;
		AABB translatedBox = obj.globalHitBox.TranslatedCopy(potentialTranslation);

		int minX = Mathf.Min(WorldToBucket_i(objBox.left), WorldToBucket_i(translatedBox.left));
		int maxX = Mathf.Max(WorldToBucket_i(objBox.right), WorldToBucket_i(translatedBox.right));
		int minY = Mathf.Min(WorldToBucket_j(objBox.bottom), WorldToBucket_j(translatedBox.bottom));
		int maxY = Mathf.Max(WorldToBucket_j(objBox.top), WorldToBucket_j(translatedBox.top));

		foreach (PhysicsObject potentialObj in GetPotentialOverlaps(minX, maxX, minY, maxY))
		{
			if (potentialObj != obj)
			{
				yield return potentialObj;
			}
		}
	}

	public List<PhysicsObject> GetPotentialOverlapsList(PhysicsObject obj)
	{
		List<PhysicsObject> potentialObjs = new List<PhysicsObject>();

		foreach (PhysicsObject potentialObj in GetPotentialOverlaps(obj))
		{
			potentialObjs.Add(potentialObj);
		}

		return potentialObjs;
	}

	public List<PhysicsObject> GetPotentialOverlapsList(PhysicsObject obj, Vector2 potentialTranslation)
	{
		List<PhysicsObject> potentialObjs = new List<PhysicsObject>();

		foreach (PhysicsObject potentialObj in GetPotentialOverlaps(obj, potentialTranslation))
		{
			potentialObjs.Add(potentialObj);
		}

		return potentialObjs;
	}

	private IEnumerable<PhysicsObject> GetPotentialOverlaps(int minX, int maxX, int minY, int maxY)
	{
#if DO_SANITY_CHECKS
		if (minX > maxX)
		{
			throw new System.Exception("minX > maxX");
		}

		if (minY > maxY)
		{
			throw new System.Exception("minY > maxY");
		}
#endif

		minX = Mathf.Max(minX, 0);
		minY = Mathf.Max(minY, 0);
		maxX = Mathf.Min(maxX, _numCols - 1);
		maxY = Mathf.Min(maxY, _numRows - 1);

		for (int i = minX; i <= maxX; i++)
		{
			for (int j = minY; j <= maxY; j++)
			{
				HashSet<PhysicsObject> bucket = _buckets[GetBucketID(i, j)];

				foreach (PhysicsObject potentialObj in bucket)
				{
					yield return potentialObj;
				}
			}
		}
	}

	private HashSet<int> GetIntersectingBuckets(AABB box)
	{
		int minX = WorldToBucket_i(box.left);
		int maxX = WorldToBucket_i(box.right);
		int minY = WorldToBucket_j(box.bottom);
		int maxY = WorldToBucket_j(box.top);
		
#if DO_SANITY_CHECKS
		if (minX > maxX)
		{
			throw new System.Exception("minX > maxX");
		}

		if (minY > maxY)
		{
			throw new System.Exception("minY > maxY");
		}
#endif

		minX = Mathf.Max(minX, 0);
		minY = Mathf.Max(minY, 0);
		maxX = Mathf.Min(maxX, _numCols - 1);
		maxY = Mathf.Min(maxY, _numRows - 1);
		
		HashSet<int> buckets = new HashSet<int>();

		for (int i = minX; i <= maxX; i++)
		{
			for (int j = minY; j <= maxY; j++)
			{
				buckets.Add(GetBucketID(i, j));
			}
		}

		return buckets;
	}

	private int GetBucketID(int i, int j)
	{
		return i + j * _numCols;
	}

	private int WorldToBucket_i(float x)
	{
		// 0 <= normalized <= 1
		float normalized = (x - _boundingBox.left) / (2 * _boundingBox.hw);

		return Mathf.FloorToInt(normalized * _numCols);
	}

	private int WorldToBucket_j(float y)
	{
		// 0 <= normalized <= 1
		float normalized = (y - _boundingBox.bottom) / (2 * _boundingBox.hh);

		return Mathf.FloorToInt(normalized * _numRows);
	}
}