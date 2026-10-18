using Godot;
using System.Collections.Generic;
using System.Linq;


public partial class Canvas : Control
{
	private readonly List<Stroke> _strokes = new();
	private Stroke _current; 
	private List<Glyph> _debugGlyphs = new();

	[Export] public Color BrushColor = Colors.Black;
	[Export] public float BrushWidth = 4f;
	[Export] public string RecordName = "star";
	private TemplateLibrary _library = new();

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
		else if (@event is InputEventKey tk && tk.Pressed && tk.Keycode == Key.T && _strokes.Count > 0)
		{
		// Records EVERYTHING on the canvas as one template, so draw one symbol at a time
		var strokes = _strokes.Select(s => new List<Vector2>(s.Points)).ToList();
		_library.Add(RecordName, strokes);
		_library.Save();
		GD.Print($"Recorded '{RecordName}' ({strokes.Count} stroke(s)). Total: {_library.Templates.Count}");
		}
		else if (@event is InputEventKey ck && ck.Pressed && ck.Keycode == Key.C)
		{
		Clear();
		_debugGlyphs.Clear();
		QueueRedraw();
		}
		else if (@event is InputEventKey mk && mk.Pressed && mk.Keycode == Key.M && _strokes.Count > 0)
		{		
		var all = GlyphMatcher.Recognise(_strokes.Select(s => s.Points).ToList(), _library.Templates);
		GD.Print($"whole canvas: best={all.Name} dist={all.Distance:F3} runnerUp={all.RunnerUp:F3}");

		_debugGlyphs = GlyphLabeller.Apply(Grouper.Build(_strokes), _library);
		foreach (var g in _debugGlyphs)
			GD.Print($"{g}  name={g.Name ?? "-"}  dist={g.MatchDistance:F3}");
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
