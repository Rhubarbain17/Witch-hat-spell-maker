using Godot;
using System.Collections.Generic;

public partial class Canvas : Control
{
	private readonly List<Stroke> _strokes = new();
	private Stroke _current; 

	[Export] public Color BrushColor = Colors.Black;
	[Export] public float BrushWidth = 4f;

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
	}

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

	private void AddPoint(Vector2 pos, float pressure) 
	{
		if (_current.Points.Count > 0 &&  _current.Points[^1].DistanceTo(pos) < 1f) return;
		_current.Points.Add(pos);
		_current.Times.Add(Time.GetTicksMsec());
		_current.Pressures.Add(pressure);
		_current.Line.AddPoint(pos);
	}

	private void EndStroke() { _current = null; }

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
