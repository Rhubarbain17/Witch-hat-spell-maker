using Godot;
using System.Collections.Generic;
//Stores everything to do with a Glyph
public class Glyph
{
	public string Name;
	public float MatchDistance;
	public List<Stroke> Strokes = new();
	public List<Vector2> Points = new();
	public ShapeResult Shape;
	public Rect2 Bounds => Shape.Bounds;
	public Vector2 Centre => Bounds.GetCenter();
	public override string ToString() => $"{Shape.Kind} ({Strokes.Count} stroke(s))";
}
