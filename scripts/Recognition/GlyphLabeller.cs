using Godot;
using System.Collections.Generic;
using System.Linq;

public static class GlyphLabeller
{
	public static List<Glyph> Apply(List<Glyph> glyphs, TemplateLibrary lib)
	{
		var result = glyphs.Where(g => g.Shape.Kind == ShapeKind.Circle).ToList();
		var pool = glyphs.Where(g => g.Shape.Kind != ShapeKind.Circle).ToList();
		var seen = new HashSet<Glyph>();

		foreach (var start in pool)
		{
			if (!seen.Add(start)) continue;

			var cluster = new List<Glyph> { start };
			for (int k = 0; k < cluster.Count; k++)
				foreach (var other in pool)
					if (!seen.Contains(other) && Touching(cluster[k], other))
					{
						seen.Add(other);
						cluster.Add(other);
					}

			result.AddRange(Resolve(cluster, lib));
		}
		return result;
	}

	private static bool Touching(Glyph a, Glyph b, float gap = 10f)
		=> a.Bounds.Grow(gap).Intersects(b.Bounds.Grow(gap));

	private static IEnumerable<Glyph> Resolve(List<Glyph> cluster, TemplateLibrary lib)
	{
		bool worthMatching = cluster.Count > 1 || cluster[0].Shape.Kind == ShapeKind.Unknown;
		if (worthMatching)
		{
			var strokes = cluster.SelectMany(g => g.Strokes).ToList();
			var m = GlyphMatcher.Recognise(strokes.Select(s => s.Points).ToList(), lib.Templates);
			if (m.Accepted)
			{
				var merged = new Glyph { Name = m.Name, MatchDistance = m.Distance, Strokes = strokes };
				foreach (var g in cluster) merged.Points.AddRange(g.Points);
				merged.Shape = new ShapeResult
				{
					Kind = ShapeKind.Unknown,
					Bounds = ShapeDetect.BoundsOf(merged.Points),
				};
				return new[] { merged };
			}
		}
		return cluster;
	}
}
