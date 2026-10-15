using Godot;

public enum Direction { Above, Below, Left, Right }

public static class Spatial
{
	// Every point of 'inner' lies within the circle (with a little margin)
	public static bool Inside(Glyph circle, Glyph inner, float margin = 0.9f)
	{
		if (circle.Shape.Kind != ShapeKind.Circle || circle == inner) return false;
		float limit = circle.Shape.Radius * margin;
		foreach (var p in inner.Points)
			if (p.DistanceTo(circle.Shape.Centre) > limit) return false;
		return true;
	}

	// Every point of 'other' lies clearly outside the circle
	public static bool Outside(Glyph circle, Glyph other, float margin = 1.1f)
	{
		if (circle.Shape.Kind != ShapeKind.Circle || circle == other) return false;
		float limit = circle.Shape.Radius * margin;
		foreach (var p in other.Points)
			if (p.DistanceTo(circle.Shape.Centre) < limit) return false;
		return true;
	}

	// Does either end of 'stroke' land on the circle's edge? (tolerance scales with radius)
	public static bool EndTouchesCircle(Glyph circle, Glyph stroke, float tolerance = 0.2f)
	{
		if (circle.Shape.Kind != ShapeKind.Circle || stroke.Points.Count == 0) return false;
		float tol = circle.Shape.Radius * tolerance;
		foreach (var p in new[] { stroke.Points[0], stroke.Points[^1] })
			if (Mathf.Abs(p.DistanceTo(circle.Shape.Centre) - circle.Shape.Radius) <= tol)
				return true;
		return false;
	}

	// Do the two glyphs' endpoints meet each other?
	public static bool EndpointsMeet(Glyph a, Glyph b, float tolerance = 12f)
	{
		foreach (var pa in new[] { a.Points[0], a.Points[^1] })
		foreach (var pb in new[] { b.Points[0], b.Points[^1] })
			if (pa.DistanceTo(pb) <= tolerance) return true;
		return false;
	}

	// Where is 'b' relative to 'a'? (screen Y points down)
	public static Direction DirectionOf(Glyph a, Glyph b)
	{
		Vector2 d = b.Centre - a.Centre;
		if (Mathf.Abs(d.X) > Mathf.Abs(d.Y)) return d.X > 0 ? Direction.Right : Direction.Left;
		return d.Y > 0 ? Direction.Below : Direction.Above;
	}
}
