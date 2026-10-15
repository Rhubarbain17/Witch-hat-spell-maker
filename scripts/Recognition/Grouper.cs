using Godot;
using System.Collections.Generic;

public static class Grouper
{
	public static List<Glyph> Build(IList<Stroke> strokes)
	{
		// Start with one glyph per stroke
		var items = new List<Glyph>();
		foreach (var s in strokes)
		{
			if (s.Points.Count == 0) continue;
			var g = new Glyph();
			g.Strokes.Add(s);
			g.Points.AddRange(s.Points);
			items.Add(g);
		}

		// Keep merging until nothing changes
		bool changed = true;
		while (changed)
		{
			changed = false;
			for (int i = 0; i < items.Count && !changed; i++)
			for (int j = i + 1; j < items.Count && !changed; j++)
			{
				if (TryJoin(items[i], items[j], out var merged))
				{
					items[i] = merged;
					items.RemoveAt(j);
					changed = true;       // indices are stale, so restart the scan
				}
			}
		}

		foreach (var g in items) g.Shape = ShapeDetect.Classify(g.Points);
		return items;
	}

	// Join two open strokes end to end. Accept only if the result is a circle.
	private static bool TryJoin(Glyph a, Glyph b, out Glyph merged)
	{
		merged = null;
		if (Preprocess.IsClosed(a.Points) || Preprocess.IsClosed(b.Points)) return false;

		float lenA = Preprocess.PathLength(a.Points);
		float lenB = Preprocess.PathLength(b.Points);
		float tol = Mathf.Max(10f, 0.15f * (lenA + lenB) / 2f);

		// Try all four ways of orienting the pair; keep the closest joint
		float best = float.MaxValue; bool bestRa = false, bestRb = false;
		foreach (bool ra in new[] { false, true })
		foreach (bool rb in new[] { false, true })
		{
			Vector2 aEnd   = ra ? a.Points[0]  : a.Points[^1];
			Vector2 bStart = rb ? b.Points[^1] : b.Points[0];
			float d = aEnd.DistanceTo(bStart);
			if (d < best) { best = d; bestRa = ra; bestRb = rb; }
		}
		if (best > tol) return false;

		var pts = new List<Vector2>(a.Points);
		if (bestRa) pts.Reverse();
		var second = new List<Vector2>(b.Points);
		if (bestRb) second.Reverse();
		pts.AddRange(second);

		if (ShapeDetect.Classify(pts).Kind != ShapeKind.Circle) return false;

		merged = new Glyph { Points = pts };
		merged.Strokes.AddRange(a.Strokes);
		merged.Strokes.AddRange(b.Strokes);
		return true;
	}
}
