using UnityEngine;
using System.Collections;

public class Utils
{
	#region Colors

	public static Color TranslucidWhite = new Color(1, 1, 1, 0.5f);
	public static Color TranslucidGray = new Color(0.5f, 0.5f, 0.5f, 0.5f);
	public static Color TranslucidBlack = new Color(0, 0, 0, 0.5f);

	public static Color TranslucidRed = new Color(1, 0, 0, 0.5f);
	public static Color TranslucidGreen = new Color(0, 1, 0, 0.5f);
	public static Color TranslucidBlue = new Color(0, 0, 1, 0.5f);

	public static Color TranslucidCyan = new Color(0, 1, 1, 0.5f);
	public static Color TranslucidMagenta = new Color(1, 0, 1, 0.5f);
	public static Color TranslucidYellow = new Color(1, 1, 0, 0.5f);

	#endregion

	public static int IntSign(float f)
	{
		if (f > 0)
		{
			return 1;
		}
		else if (f < 0)
		{
			return -1;
		}
		else
		{
			return 0;
		}
	}

	public static Vector3 Reflect(Vector3 inwardVector, Vector3 normalVector)
	{
		Vector3 normalized = normalVector.normalized;
		float penetration = Vector3.Dot(inwardVector, normalized);

		return inwardVector + normalized * (-2 * penetration);
	}

	public static void AssertTrue(bool condition, string message = null)
	{
		if (!condition)
		{
			if (message != null)
			{
				Debug.LogError("ASSERT FAILED! Message: " + message);
			}
			else
			{
				Debug.LogError("ASSERT FAILED!");
			}
		}
	}
}
