using Godot;
using System.Collections.Generic;
using System.Linq;

public struct MatchResult
{
	public string Name;
	public float Distance;
	public float RunnerUp;

	public bool Accepted =>
		Name != null &&
		Distance <= GlyphMatcher.MaxDistance &&
		RunnerUp >= Distance * GlyphMatcher.AmbiguityRatio;
}

public class GlyphTemplate
{
	public string Name;
	public int StrokeCount;
	public List<List<Vector2>> RawStrokes;
	public List<Vector2> Cloud;
}

public static class GlyphMatcher
{
	public const int N = 32;
	public const float MaxDistance = 0.12f;
	public const float AmbiguityRatio = 1.1f;

	public static GlyphTemplate MakeTemplate(string name, List<List<Vector2>> strokes) => new()
	{
		Name = name,
		StrokeCount = strokes.Count,
		RawStrokes = strokes,
		Cloud = Normalise(strokes),
	};

	public static List<Vector2> Normalise(List<List<Vector2>> strokes)
	{
		var cloud = ResampleCloud(strokes, N);
		return Preprocess.TranslateToOrigin(Preprocess.ScaleToUnit(cloud));
	}

	private static List<Vector2> ResampleCloud(List<List<Vector2>> strokes, int n)
	{
		var pts = new List<(Vector2 P, int Id)>();
		float total = 0f;
		for (int s = 0; s < strokes.Count; s++)
		{
			total += Preprocess.PathLength(strokes[s]);
			foreach (var p in strokes[s]) pts.Add((p, s));
		}

		var result = new List<Vector2>(n);
		if (pts.Count == 0) return result;
		if (total < 1e-6f)
		{
			for (int i = 0; i < n; i++) result.Add(pts[0].P);
			return result;
		}

		float interval = total / (n - 1);
		float acc = 0f;
		result.Add(pts[0].P);

		for (int i = 1; i < pts.Count; i++)
		{
			if (pts[i].Id != pts[i - 1].Id) continue;
			float d = pts[i - 1].P.DistanceTo(pts[i].P);
			if (d > 0f && acc + d >= interval)
			{
				Vector2 q = pts[i - 1].P.Lerp(pts[i].P, (interval - acc) / d);
				result.Add(q);
				pts.Insert(i, (q, pts[i].Id));
				acc = 0f;
			}
			else acc += d;
		}

		while (result.Count < n) result.Add(pts[^1].P);
		if (result.Count > n) result.RemoveRange(n, result.Count - n);
		return result;
	}

	public static MatchResult Recognise(List<List<Vector2>> strokes,
		IEnumerable<GlyphTemplate> templates, bool requireSameStrokeCount = true)
	{
		if (strokes.Count == 0) return default;
		var cloud = Normalise(strokes);

		var bestByName = new Dictionary<string, float>();
		foreach (var t in templates)
		{
			if (requireSameStrokeCount && t.StrokeCount != strokes.Count) continue;
			float d = GreedyCloudMatch(cloud, t.Cloud) / (N * 0.5f);
			if (!bestByName.TryGetValue(t.Name, out var cur) || d < cur)
				bestByName[t.Name] = d;
		}
		if (bestByName.Count == 0) return default;

		var ranked = bestByName.OrderBy(kv => kv.Value).ToList();
		return new MatchResult
		{
			Name = ranked[0].Key,
			Distance = ranked[0].Value,
			RunnerUp = ranked.Count > 1 ? ranked[1].Value : float.MaxValue,
		};
	}

	private static float GreedyCloudMatch(List<Vector2> a, List<Vector2> b)
	{
		int n = a.Count;
		int step = (int)Mathf.Floor(Mathf.Pow(n, 0.5f));
		float min = float.MaxValue;
		for (int i = 0; i < n; i += step)
			min = Mathf.Min(min, Mathf.Min(CloudDistance(a, b, i), CloudDistance(b, a, i)));
		return min;
	}


	private static float CloudDistance(List<Vector2> a, List<Vector2> b, int start)
	{
		int n = a.Count;
		var used = new bool[n];
		float sum = 0f;
		int i = start;
		do
		{
			int index = -1;
			float min = float.MaxValue;
			for (int j = 0; j < n; j++)
			{
				if (used[j]) continue;
				float d = a[i].DistanceTo(b[j]);
				if (d < min) { min = d; index = j; }
			}
			used[index] = true;
			float weight = 1f - ((i - start + n) % n) / (float)n;
			sum += weight * min;
			i = (i + 1) % n;
		} while (i != start);
		return sum;
	}
}
