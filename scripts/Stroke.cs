using Godot;
using System.Collections.Generic;

public partial class Stroke
{
	public List<Vector2> Points = new();
	public List<ulong> Times = new();
	public List<float> Pressures = new();
	public Line2D Line;  
}
