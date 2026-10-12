using Godot;
using System;
using System.Collections.Generic;

public static class Preprocess
{
	public const int DefaultPointCount = 64;

	public static float PathLength(IList<Vector2> pts)
	{
		float len = 0f;
		for (int i = 1; i < pts.Count; i++)
			len += pts[i - 1].DistanceTo(pts[i]);
		return len;
	}

	public static List<Vector2> Resample(IList<Vector2> pts, int n = DefaultPointCount)
	{
		var result = new List<Vector2>(n);
		if (pts.Count == 0) return result;

		float total = PathLength(pts);

		if (pts.Count == 1 || total < 1e-6f)
		{
			for (int i = 0; i < n; i++) result.Add(pts[0]);
			return result;
		}

		float interval = total / (n - 1);
		float accumulated = 0f;

		var work = new List<Vector2>(pts);
		result.Add(work[0]);

		for (int i = 1; i < work.Count; i++)
		{
			float d = work[i - 1].DistanceTo(work[i]);

			if (d > 0f && accumulated + d >= interval)
			{
				float t = (interval - accumulated) / d;
				Vector2 q = work[i - 1].Lerp(work[i], t);
				result.Add(q);
				work.Insert(i, q);
				accumulated = 0f;
			}
			else
			{
				accumulated += d;
			}
		}

		while (result.Count < n) result.Add(work[^1]);
		if (result.Count > n) result.RemoveRange(n, result.Count - n);

		return result;
	}

	public static Vector2 Centroid(IList<Vector2> pts)
	{
		if (pts.Count == 0) return Vector2.Zero;
		Vector2 sum = Vector2.Zero;
		foreach (var p in pts) sum += p;
		return sum / pts.Count;
	}

	public static List<Vector2> TranslateToOrigin(IList<Vector2> pts)
	{
		var c = Centroid(pts);
		var result = new List<Vector2>(pts.Count);
		foreach (var p in pts) result.Add(p - c);
		return result;
	}

	public static List<Vector2> ScaleToUnit(IList<Vector2> pts)
	{
		var result = new List<Vector2>(pts.Count);
		if (pts.Count == 0) return result;

		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var p in pts)
		{
			minX = Mathf.Min(minX, p.X); maxX = Mathf.Max(maxX, p.X);
			minY = Mathf.Min(minY, p.Y); maxY = Mathf.Max(maxY, p.Y);
		}

		float size = Mathf.Max(maxX - minX, maxY - minY);
		if (size < 1e-6f)
		{
			foreach (var p in pts) result.Add(p);
			return result;
		}

		foreach (var p in pts) result.Add(p / size);
		return result;
	}

	public static bool IsClosed(IList<Vector2> pts, float tolerance = 0.15f)
	{
		if (pts.Count < 3) return false;
		float len = PathLength(pts);
		if (len < 1e-6f) return false;
		return pts[0].DistanceTo(pts[^1]) < tolerance * len;
	}

	public static List<Vector2> Normalize(IList<Vector2> pts, int n = DefaultPointCount)
	{
		var resampled = Resample(pts, n);
		var centred = TranslateToOrigin(resampled);
		return ScaleToUnit(centred);
	}
}
