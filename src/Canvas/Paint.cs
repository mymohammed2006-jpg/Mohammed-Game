using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MG.Canvas;

// A CSS color or a gradient, the same things canvas fillStyle/strokeStyle accept.
public sealed class Gradient
{
	public bool radial;
	public double x0, y0, r0, x1, y1, r1;
	public readonly List<(double off, Color col)> stops = new();
	public void addColorStop(double off, string col)
	{
		if (!CssColor.TryParse(col, out var c)) return;
		int i = stops.Count;
		while (i > 0 && stops[i - 1].off > off) i--;
		stops.Insert(i, (Math.Clamp(off, 0, 1), c));
	}
	// color at gradient parameter t (0..1), like the canvas spec (clamped ends)
	public Color At(double t)
	{
		if (stops.Count == 0) return new Color(0, 0, 0, 0);
		if (t <= stops[0].off) return stops[0].col;
		for (int i = 1; i < stops.Count; i++)
		{
			if (t <= stops[i].off)
			{
				var a = stops[i - 1]; var b = stops[i];
				double k = b.off - a.off <= 0 ? 1 : (t - a.off) / (b.off - a.off);
				// canvas interpolates in premultiplied space
				return LerpPremul(a.col, b.col, (float)k);
			}
		}
		return stops[^1].col;
	}
	static Color LerpPremul(Color a, Color b, float k)
	{
		float al = a.A + (b.A - a.A) * k;
		if (al <= 0) return new Color(0, 0, 0, 0);
		float r = (a.R * a.A + (b.R * b.A - a.R * a.A) * k) / al;
		float g = (a.G * a.A + (b.G * b.A - a.G * a.A) * k) / al;
		float bl = (a.B * a.A + (b.B * b.A - a.B * a.A) * k) / al;
		return new Color(r, g, bl, al);
	}
	// gradient parameter at a point in gradient space
	public double Param(double px, double py)
	{
		if (!radial)
		{
			double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy;
			return l2 <= 0 ? 0 : ((px - x0) * dx + (py - y0) * dy) / l2;
		}
		// two-point conical gradient (canvas): largest t with r(t) >= 0 where |p - c(t)| = r(t)
		double cdx = x1 - x0, cdy = y1 - y0, dr = r1 - r0;
		double pdx = px - x0, pdy = py - y0;
		double a = cdx * cdx + cdy * cdy - dr * dr;
		double b = pdx * cdx + pdy * cdy + r0 * dr;
		double c = pdx * pdx + pdy * pdy - r0 * r0;
		if (Math.Abs(a) < 1e-9)
		{
			if (Math.Abs(b) < 1e-12) return 0;
			double t0 = c / (2 * b);
			return r0 + t0 * dr >= 0 ? t0 : 0;
		}
		double disc = b * b - a * c;
		if (disc < 0) return 0;
		double s = Math.Sqrt(disc);
		double t1 = (b + s) / a, t2 = (b - s) / a;
		double tt = Math.Max(t1, t2);
		if (r0 + tt * dr >= 0) return tt;
		tt = Math.Min(t1, t2);
		return r0 + tt * dr >= 0 ? tt : 0;
	}
}

public struct Paint
{
	public Color col;
	public Gradient grad;
	public static implicit operator Paint(string s) { CssColor.TryParse(s, out var c); return new Paint { col = c }; }
	public static implicit operator Paint(Gradient gr) => new Paint { grad = gr, col = new Color(1, 1, 1, 1) };
	public static implicit operator Paint(Color c) => new Paint { col = c };
	public float Alpha => grad != null ? 1f : col.A;
}

public static class CssColor
{
	static readonly Dictionary<string, Color> cache = new();
	public static bool TryParse(string s, out Color c)
	{
		c = new Color(0, 0, 0, 1);
		if (s == null) return false;
		if (cache.TryGetValue(s, out c)) return true;
		bool ok = Parse(s.Trim(), out c);
		// strings with a computed alpha are endless; keep the cache for fixed ones only
		if (ok && cache.Count < 4096 && s.Length < 12) cache[s] = c;
		return ok;
	}
	static int Hex(char ch) => ch >= '0' && ch <= '9' ? ch - '0' : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10 : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10 : -1;
	static bool Parse(string s, out Color c)
	{
		c = new Color(0, 0, 0, 1);
		if (s.Length > 0 && s[0] == '#')
		{
			int n = s.Length - 1;
			int[] h = new int[n];
			for (int i = 0; i < n; i++) { h[i] = Hex(s[i + 1]); if (h[i] < 0) return false; }
			if (n == 3 || n == 4)
			{
				c = new Color(h[0] * 17 / 255f, h[1] * 17 / 255f, h[2] * 17 / 255f, n == 4 ? h[3] * 17 / 255f : 1f);
				return true;
			}
			if (n == 6 || n == 8)
			{
				c = new Color((h[0] * 16 + h[1]) / 255f, (h[2] * 16 + h[3]) / 255f, (h[4] * 16 + h[5]) / 255f, n == 8 ? (h[6] * 16 + h[7]) / 255f : 1f);
				return true;
			}
			return false;
		}
		int p = s.IndexOf('(');
		if (p > 0 && s.EndsWith(")"))
		{
			string fn = s.Substring(0, p).Trim().ToLowerInvariant();
			var parts = s.Substring(p + 1, s.Length - p - 2).Split(new[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (fn == "rgb" || fn == "rgba")
			{
				if (parts.Length < 3) return false;
				double r = Comp(parts[0], 255), g = Comp(parts[1], 255), b = Comp(parts[2], 255);
				double a = parts.Length >= 4 ? Alpha(parts[3]) : 1;
				if (double.IsNaN(r) || double.IsNaN(g) || double.IsNaN(b) || double.IsNaN(a)) return false;
				// browsers round rgb components to whole numbers
				c = new Color((float)(Math.Round(Math.Clamp(r, 0, 255)) / 255), (float)(Math.Round(Math.Clamp(g, 0, 255)) / 255), (float)(Math.Round(Math.Clamp(b, 0, 255)) / 255), (float)Math.Clamp(a, 0, 1));
				return true;
			}
			if (fn == "hsl" || fn == "hsla")
			{
				if (parts.Length < 3) return false;
				double hh = Num(parts[0].Replace("deg", "")), ss = Num(parts[1].TrimEnd('%')) / 100, ll = Num(parts[2].TrimEnd('%')) / 100;
				double a = parts.Length >= 4 ? Alpha(parts[3]) : 1;
				hh = ((hh % 360) + 360) % 360 / 360;
				c = Color.FromHsv(0, 0, 0);
				var rgb = Hsl(hh, Math.Clamp(ss, 0, 1), Math.Clamp(ll, 0, 1));
				c = new Color((float)(Math.Round(rgb.r * 255) / 255), (float)(Math.Round(rgb.g * 255) / 255), (float)(Math.Round(rgb.b * 255) / 255), (float)Math.Clamp(a, 0, 1));
				return true;
			}
			return false;
		}
		switch (s.ToLowerInvariant())
		{
			case "white": c = new Color(1, 1, 1); return true;
			case "black": c = new Color(0, 0, 0); return true;
			case "red": c = new Color(1, 0, 0); return true;
			case "transparent": c = new Color(0, 0, 0, 0); return true;
		}
		return false;
	}
	static (double r, double g, double b) Hsl(double h, double s, double l)
	{
		double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
		double f(double t) { if (t < 0) t += 1; if (t > 1) t -= 1; if (t < 1.0 / 6) return p + (q - p) * 6 * t; if (t < 0.5) return q; if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6; return p; }
		return (f(h + 1.0 / 3), f(h), f(h - 1.0 / 3));
	}
	static double Num(string t) => double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;
	static double Comp(string t, double max) => t.EndsWith("%") ? Num(t.TrimEnd('%')) / 100 * max : Num(t);
	static double Alpha(string t) => t.EndsWith("%") ? Num(t.TrimEnd('%')) / 100 : Num(t);
}
