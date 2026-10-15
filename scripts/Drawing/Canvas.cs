using Godot;
using System.Collections.Generic;


public partial class Canvas : Control
{
	private readonly List<Stroke> _strokes = new();
	private Stroke _current; 
	private List<Glyph> _debugGlyphs = new();

	[Export] public Color BrushColor = Colors.Black;
	[Export] public float BrushWidth = 4f;

	// Handles mouse input
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			if (mb.Pressed) BeginStroke(mb.Position);
			else EndStroke();
		}
		else if (@event is InputEventMouseMotion mm && _current != null)
		{
			AddPoint(mm.Position, mm.Pressure);
		}
		else if (@event is InputEventKey key && key.Pressed && key.CtrlPressed && key.Keycode == Key.Z)
		{
			Undo();
		}
		else if (@event is InputEventKey gk && gk.Pressed && gk.Keycode == Key.G)
		{
			_debugGlyphs = Grouper.Build(_strokes);
			foreach (var g in _debugGlyphs) GD.Print(g);

			foreach (var a in _debugGlyphs)
			foreach (var b in _debugGlyphs)
		{
			if (a == b) continue;
			if (Spatial.Inside(a, b))         GD.Print($"{b} is INSIDE {a}");
			if (Spatial.EndTouchesCircle(a, b)) GD.Print($"{b} touches the edge of {a}");
		}
		QueueRedraw();
	}
	}

	// Start of the stroke
	private void BeginStroke(Vector2 pos)
	{
		var line = new Line2D
		{
			Width = BrushWidth,
			DefaultColor = BrushColor,
			Antialiased = true,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round,
			JointMode = Line2D.LineJointMode.Round,
		};
		AddChild(line);

		_current = new Stroke { Line = line };
		_strokes.Add(_current);

		AddPoint(pos, 1f);
	}

	// Adds a point to the line
	private void AddPoint(Vector2 pos, float pressure) 
	{
		if (_current.Points.Count > 0 &&  _current.Points[^1].DistanceTo(pos) < 1f) return;
		_current.Points.Add(pos);
		_current.Times.Add(Time.GetTicksMsec());
		_current.Pressures.Add(pressure);
		_current.Line.AddPoint(pos);
	}

	// End of the stroke
	private void EndStroke() { _current = null; }

	// Undo and clear
	public void Undo()
	{
		if (_strokes.Count == 0) return;

		var last = _strokes[^1];
		last.Line.QueueFree();
		_strokes.RemoveAt(_strokes.Count - 1);
	}

	public void Clear()
	{
		while (_strokes.Count > 0) Undo();
	}
}
