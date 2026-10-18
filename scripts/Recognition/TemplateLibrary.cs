using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class TemplateDto
{
	public string Name { get; set; }
	public List<List<float[]>> Strokes { get; set; }
}

public class TemplateLibrary
{
	public List<GlyphTemplate> Templates = new();

	private const string DefaultPath = "res://data/glyph_templates/default.json";
	private const string UserPath = "user://glyph_templates.json";

	public void Add(string name, List<List<Vector2>> strokes)
		=> Templates.Add(GlyphMatcher.MakeTemplate(name, strokes));

	public void Save()
	{
		var dtos = Templates.Select(t => new TemplateDto
		{
			Name = t.Name,
			Strokes = t.RawStrokes
				.Select(s => s.Select(p => new[] { p.X, p.Y }).ToList())
				.ToList(),
		}).ToList();

		using var f = Godot.FileAccess.Open(UserPath, Godot.FileAccess.ModeFlags.Write);
		f.StoreString(JsonSerializer.Serialize(dtos));
	}

	public void Load()
	{
		Templates.Clear();
		foreach (var path in new[] { DefaultPath, UserPath })
		{
			if (!Godot.FileAccess.FileExists(path)) continue;
			using var f = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
			var dtos = JsonSerializer.Deserialize<List<TemplateDto>>(f.GetAsText());
			if (dtos == null) continue;

			foreach (var d in dtos)
			{
				var strokes = d.Strokes
					.Select(s => s.Select(p => new Vector2(p[0], p[1])).ToList())
					.ToList();
				Add(d.Name, strokes);
			}
		}
	}
}
