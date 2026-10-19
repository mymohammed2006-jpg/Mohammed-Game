using Godot;
using System;
using System.Collections.Generic;

namespace MG.Canvas;

// 2x3 matrix with the same meaning as the canvas transform (a b c d e f).
public struct Mat
{
	public double a, b, c, d, e, f;
	public static readonly Mat I = new Mat { a = 1, d = 1 };
	public Mat(double a, double b, double c, double d, double e, double f) { this.a = a; this.b = b; this.c = c; this.d = d; this.e = e; this.f = f; }
	public readonly double X(double x, double y) => a * x + c * y + e;
	public readonly double Y(double x, double y) => b * x + d * y + f;
	public readonly Vector2 P(double x, double y) => new Vector2((float)(a * x + c * y + e), (float)(b * x + d * y + f));
	// this * m (m applied first), like canvas transform()
	public readonly Mat Mul(Mat m) => new Mat(a * m.a + c * m.b, b * m.a + d * m.b, a * m.c + c * m.d, b * m.c + d * m.d, a * m.e + c * m.f + e, b * m.e + d * m.f + f);
	public readonly double Det => a * d - b * c;
	public readonly Mat Inv()
	{
		double det = Det;
		if (Math.Abs(det) < 1e-300) return I;
		return new Mat(d / det, -b / det, -c / det, a / det, (c * f - d * e) / det, (b * e - a * f) / det);
	}
	// average scale, used for curve detail and line widths
	public readonly double Scale => Math.Sqrt(Math.Abs(Det));
	public readonly bool Invertible => Math.Abs(Det) > 1e-12 && !double.IsNaN(Det) && !double.IsInfinity(Det);
}

public enum Cap { Butt, Round, Square }
public enum Join { Miter, Round, Bevel }

public struct FontSpec
{
	public int weight;     // 400 normal, 700 bold, 500, 800...
	public double size;    // px
	public string family;  // first family name
}

public struct CtxState
{
	public Mat m;
	public Paint fill, stroke;
	public double lineWidth, miterLimit, globalAlpha, shadowBlur, shadowOffsetX, shadowOffsetY, lineDashOffset;
	public Color shadowColor;
	public Cap cap;
	public Join join;
	public string font;
	public FontSpec fontSpec;
	public string textAlign, textBaseline, direction;
	public double[] dash;
	public int clip;                   // container in the Surface (0 = no clip)

	public static CtxState Default()
	{
		var s = new CtxState
		{
			m = Mat.I,
			fill = new Paint { col = new Color(0, 0, 0, 1) },
			stroke = new Paint { col = new Color(0, 0, 0, 1) },
			lineWidth = 1, miterLimit = 10, globalAlpha = 1,
			shadowColor = new Color(0, 0, 0, 0),
			cap = Cap.Butt, join = Join.Miter,
			textAlign = "start", textBaseline = "alphabetic",
			// the page is dir="rtl", so canvas text direction "inherit" is rtl
			direction = "rtl",
			dash = Array.Empty<double>(),
		};
		s.font = "10px sans-serif";
		s.fontSpec = new FontSpec { weight = 400, size = 10, family = "sans-serif" };
		return s;
	}
}

public class TextMetrics { public double width; }

// The canvas 2D context: same names and behavior as the browser one, so the game code ports line by line.
// Shapes are turned into triangles (with a soft 1px edge like the browser's anti-aliasing) and handed to a Surface.
public partial class Ctx
{
	public CtxState S = CtxState.Default();
	readonly Stack<CtxState> stack = new();
	public Surface surf;
	public int width, height;

	public Ctx(Surface s, int w, int h) { surf = s; width = w; height = h; }

	// setting canvas.width/height resets the context (the game does this in resize())
	public void Reset(int w, int h)
	{
		width = w; height = h;
		S = CtxState.Default();
		stack.Clear();
		path.Clear(); curSub = null;
	}

	// ---------- state ----------
	public void save() => stack.Push(Clone(S));
	public void restore() { if (stack.Count > 0) S = stack.Pop(); }
	static CtxState Clone(CtxState s) => s;   // a struct; the dash array is never changed in place

	public Paint fillStyle { get => S.fill; set { if (value.grad != null || value.col.A >= 0) S.fill = value; } }
	public Paint strokeStyle { get => S.stroke; set => S.stroke = value; }
	public double globalAlpha { get => S.globalAlpha; set { if (value >= 0 && value <= 1 && !double.IsNaN(value)) S.globalAlpha = value; } }
	public double lineWidth { get => S.lineWidth; set { if (value > 0 && !double.IsInfinity(value) && !double.IsNaN(value)) S.lineWidth = value; } }
	public double miterLimit { get => S.miterLimit; set { if (value > 0) S.miterLimit = value; } }
	public double shadowBlur { get => S.shadowBlur; set { if (value >= 0 && !double.IsNaN(value)) S.shadowBlur = value; } }
	public double shadowOffsetX { get => S.shadowOffsetX; set => S.shadowOffsetX = value; }
	public double shadowOffsetY { get => S.shadowOffsetY; set => S.shadowOffsetY = value; }
	public string shadowColor { set { if (CssColor.TryParse(value, out var c)) S.shadowColor = c; } }
	public string lineCap { get => S.cap.ToString().ToLowerInvariant(); set { S.cap = value switch { "round" => Cap.Round, "square" => Cap.Square, "butt" => Cap.Butt, _ => S.cap }; } }
	public string lineJoin { get => S.join.ToString().ToLowerInvariant(); set { S.join = value switch { "round" => Join.Round, "bevel" => Join.Bevel, "miter" => Join.Miter, _ => S.join }; } }
	public string textAlign { get => S.textAlign; set { if (value is "start" or "end" or "left" or "right" or "center") S.textAlign = value; } }
	public string textBaseline { get => S.textBaseline; set { if (value is "top" or "hanging" or "middle" or "alphabetic" or "ideographic" or "bottom") S.textBaseline = value; } }
	public string direction { get => S.direction; set { S.direction = value == "inherit" ? "rtl" : value == "ltr" || value == "rtl" ? value : S.direction; } }
	public double lineDashOffset { get => S.lineDashOffset; set => S.lineDashOffset = value; }
	public string font
	{
		get => S.font;
		set { if (Fonts.ParseFont(value, out var fs)) { S.font = value; S.fontSpec = fs; } }
	}
	public void setLineDash(double[] seg)
	{
		foreach (var v in seg) if (v < 0 || double.IsNaN(v) || double.IsInfinity(v)) return;
		if (seg.Length % 2 == 1) { var d = new double[seg.Length * 2]; seg.CopyTo(d, 0); seg.CopyTo(d, seg.Length); seg = d; }
		S.dash = (double[])seg.Clone();
	}
	public double[] getLineDash() => (double[])S.dash.Clone();

	// ---------- transform ----------
	public void setTransform(double a, double b, double c, double d, double e, double f) => S.m = new Mat(a, b, c, d, e, f);
	public void resetTransform() => S.m = Mat.I;
	public void transform(double a, double b, double c, double d, double e, double f) => S.m = S.m.Mul(new Mat(a, b, c, d, e, f));
	public void translate(double x, double y) => S.m = S.m.Mul(new Mat(1, 0, 0, 1, x, y));
	public void rotate(double r) { double c = Math.Cos(r), s = Math.Sin(r); S.m = S.m.Mul(new Mat(c, s, -s, c, 0, 0)); }
	public void scale(double x, double y) => S.m = S.m.Mul(new Mat(x, 0, 0, y, 0, 0));
	public Mat getTransform() => S.m;

	// ---------- gradients ----------
	public Gradient createLinearGradient(double x0, double y0, double x1, double y1) => new Gradient { x0 = x0, y0 = y0, x1 = x1, y1 = y1 };
	public Gradient createRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1) => new Gradient { radial = true, x0 = x0, y0 = y0, r0 = r0, x1 = x1, y1 = y1, r1 = r1 };
}
