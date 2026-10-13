using Godot;
using System;
using System.Collections.Generic;

public enum ShapeKind { Unknown, Dot, Line, Circle }

// Struct that stores the final type of shape
public struct ShapeResult
{
	public ShapeKind Kind;
	public Vector2 Centre;
	public float Radius;
	public Vector2 Start;
	public Vector2 End;
	public float Error;
	public Rect2 Bounds;
}

// Detects the shape of the drawing
public static class ShapeDetect
{
	public const float MinStrokeLength = 8f;
	
	// Result of shape
	public static ShapeResult Classify(IList<Vector2> raw)
	{
		var r = new ShapeResult { Kind = ShapeKind.Unknown, Bounds = BoundsOf(raw) };
		if (raw.Count == 0) return r;

		float len = Preprocess.PathLength(raw);
		if (len < MinStrokeLength)
		{
			r.Kind = ShapeKind.Dot;
			r.Centre = Preprocess.Centroid(raw);
			return r;
		}

		if (IsCircle(raw, out var centre, out var radius, out var cErr))
		{
			r.Kind = ShapeKind.Circle;
			r.Centre = centre; r.Radius = radius; r.Error = cErr;
		}
		else if (IsLine(raw, out var lErr))
		{
			r.Kind = ShapeKind.Line;
			r.Start = raw[0]; r.End = raw[^1];
			r.Centre = (raw[0] + raw[^1]) / 2f;
			r.Error = lErr;
		}
		return r;
	}

	// If is a line
	public static bool IsLine(IList<Vector2> pts, out float error, float maxDeviation = 0.08f)
	{
		error = float.MaxValue;
		if (pts.Count < 2) return false;

		Vector2 a = pts[0], b = pts[^1];
		float chord = a.DistanceTo(b);
		if (chord < 1e-3f) return false;

		if (Preprocess.PathLength(pts) / chord > 1.2f) return false;

		Vector2 dir = (b - a) / chord;
		float maxDev = 0f;
		foreach (var p in pts)
		{
			Vector2 v = p - a;
			float dev = Mathf.Abs(v.X * dir.Y - v.Y * dir.X);
			maxDev = Mathf.Max(maxDev, dev);
		}

		error = maxDev / chord;
		return error <= maxDeviation;
	}

	// If is a circle
	public static bool IsCircle(IList<Vector2> pts, out Vector2 centre, out float radius, out float error, float maxError = 0.15f)
	{
		centre = Vector2.Zero; radius = 0f; error = float.MaxValue;
		if (!Preprocess.IsClosed(pts)) return false;

		var rs = Preprocess.Resample(pts, 64);
		centre = Preprocess.Centroid(rs);

		float sum = 0f;
		foreach (var p in rs) sum += p.DistanceTo(centre);
		radius = sum / rs.Count;
		if (radius < 1e-3f) return false;

		float dev = 0f;
		foreach (var p in rs) dev += Mathf.Abs(p.DistanceTo(centre) - radius);
		error = dev / rs.Count / radius;

		float swept = 0f;
		for (int i = 1; i < rs.Count; i++)
		{
			float d = (rs[i] - centre).Angle() - (rs[i - 1] - centre).Angle();
			while (d > Mathf.Pi)  d -= Mathf.Tau;
			while (d < -Mathf.Pi) d += Mathf.Tau;
			swept += d;
		}
		bool wentAround = Mathf.Abs(swept) >= 0.85f * Mathf.Tau;

		return error <= maxError && wentAround;
	}

	// Appiles some leniency to other shapes
	public static Rect2 BoundsOf(IList<Vector2> pts)
	{
		if (pts.Count == 0) return new Rect2();
		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var p in pts)
		{
			minX = Mathf.Min(minX, p.X); maxX = Mathf.Max(maxX, p.X);
			minY = Mathf.Min(minY, p.Y); maxY = Mathf.Max(maxY, p.Y);
		}
		return new Rect2(minX, minY, maxX - minX, maxY - minY);
	}
}
