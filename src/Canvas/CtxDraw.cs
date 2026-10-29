using Godot;
using System;
using System.Collections.Generic;

namespace MG.Canvas;

// fill / stroke / rects / clip / text, same results as the browser canvas
public partial class Ctx
{
	readonly CovMesh mesh = new();
	readonly CovMesh clipTmp = new();
	readonly List<Vector2> tmpPoly = new();

	bool ShadowOn => S.shadowColor.A > 0 && (S.shadowBlur > 0 || S.shadowOffsetX != 0 || S.shadowOffsetY != 0);

	void Emit(CovMesh m, Paint p)
	{
		if (m.ni == 0) return;
		float ga = (float)S.globalAlpha;
		if (ga <= 0) return;
		float pa = p.grad != null ? 1f : p.col.A;
		if (pa <= 0) return;
		if (ShadowOn)
		{
			var b = m.Bounds();
			var sc = S.shadowColor; sc.A *= ga;
			if (surf.Shadow(S.clip, b, S.shadowBlur, S.shadowOffsetX, S.shadowOffsetY, sc, out var map, out var region))
			{
				if (region.Encloses(b)) surf.MaskTris(m, pa, map);
				else { ClipMeshToRect(m, region, clipTmp); surf.MaskTris(clipTmp, pa, map); }
			}
		}
		if (p.grad != null) surf.GradTris(S.clip, m, p.grad, S.m, ga);
		else { var c = p.col; c.A *= ga; surf.Tris(S.clip, m, c); }
	}

	// ---------- fill ----------
	public void fill(string rule = "nonzero")
	{
		mesh.Clear();
		BuildFill(FillPolys(), rule == "evenodd", mesh);
		Emit(mesh, S.fill);
	}

	// not canvas (v53, L16's part 3 was slow): fill() for a path whose subpaths are each a simple outline (they may overlap) in a colour
	// that is not see-through. Each subpath is filled on its own instead of merging them all first (the slow part); it looks the same.
	// One shadow for all of them, like fill().
	public void fillParts()
	{
		mesh.Clear();
		foreach (var s in path)
		{
			if (s.pts.Count < 3) continue;
			var p = Geo.DropSpikes(Geo.Clean(s.pts, true, tmpPoly));
			if (p.Count < 3) continue;
			if (Geo.IsConvex(p)) Geo.FillConvex(p, mesh);
			else if (!Geo.FillSimple(p, mesh)) Geo.FillComplex(new List<List<Vector2>> { s.pts }, false, mesh);
		}
		Emit(mesh, S.fill);
	}

	void BuildFill(List<List<Vector2>> polys, bool evenOdd, CovMesh m)
	{
		if (polys.Count == 0) return;
		if (polys.Count == 1)
		{
			var p = Geo.DropSpikes(Geo.Clean(polys[0], true, tmpPoly));
			if (p.Count < 3) return;
			if (Geo.IsConvex(p)) { Geo.FillConvex(p, m); return; }
			if (p.Count <= 96 && !SelfCrossing(p) && Geo.FillSimple(p, m)) return;
		}
		Geo.FillComplex(polys, evenOdd, m);
	}

	static bool SelfCrossing(List<Vector2> p)
	{
		int n = p.Count;
		for (int i = 0; i < n; i++)
		{
			Vector2 a = p[i], b = p[(i + 1) % n];
			for (int j = i + 2; j < n; j++)
			{
				if (i == 0 && j == n - 1) continue;
				Vector2 c = p[j], d = p[(j + 1) % n];
				if (SegHit(a, b, c, d)) return true;
			}
		}
		return false;
	}
	static bool SegHit(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		float d1 = (b - a).Cross(c - a), d2 = (b - a).Cross(d - a), d3 = (d - c).Cross(a - c), d4 = (d - c).Cross(b - c);
		if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
		// touching counts as crossing (the simple triangulator can't take it)
		return (d1 == 0 && OnSeg(a, b, c)) || (d2 == 0 && OnSeg(a, b, d)) || (d3 == 0 && OnSeg(c, d, a)) || (d4 == 0 && OnSeg(c, d, b));
	}
	static bool OnSeg(Vector2 a, Vector2 b, Vector2 p) =>
		p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);

	public void fillRect(double x, double y, double w, double h)
	{
		if (Bad(x) || Bad(y) || Bad(w) || Bad(h) || w == 0 || h == 0) return;
		mesh.Clear();
		tmpPoly.Clear();
		tmpPoly.Add(S.m.P(x, y)); tmpPoly.Add(S.m.P(x + w, y)); tmpPoly.Add(S.m.P(x + w, y + h)); tmpPoly.Add(S.m.P(x, y + h));
		Geo.FillConvex(tmpPoly, mesh);
		Emit(mesh, S.fill);
	}

	// ---------- stroke ----------
	public void stroke()
	{
		mesh.Clear();
		foreach (var s in path) StrokeSub(s.pts, s.closed, mesh);
		Emit(mesh, S.stroke);
	}

	public void strokeRect(double x, double y, double w, double h)
	{
		if (Bad(x) || Bad(y) || Bad(w) || Bad(h)) return;
		if (w == 0 && h == 0) return;
		mesh.Clear();
		var pts = rectPts; pts.Clear();
		if (w == 0 || h == 0)
		{
			pts.Add(S.m.P(x, y)); pts.Add(S.m.P(x + w, y + h));
			StrokeSub(pts, false, mesh);
		}
		else
		{
			pts.Add(S.m.P(x, y)); pts.Add(S.m.P(x + w, y)); pts.Add(S.m.P(x + w, y + h)); pts.Add(S.m.P(x, y + h));
			StrokeSub(pts, true, mesh);
		}
		Emit(mesh, S.stroke);
	}

	bool Similarity(Mat m)
	{
		double s = Math.Max(Math.Abs(m.a) + Math.Abs(m.b), Math.Abs(m.c) + Math.Abs(m.d)) + 1e-12;
		return (Math.Abs(m.a - m.d) < 1e-6 * s && Math.Abs(m.b + m.c) < 1e-6 * s) || (Math.Abs(m.a + m.d) < 1e-6 * s && Math.Abs(m.b - m.c) < 1e-6 * s);
	}

	readonly List<Vector2> strokeSrc = new(), rectPts = new();
	double[] dashBuf = Array.Empty<double>();
	void StrokeSub(List<Vector2> pts, bool closed, CovMesh m)
	{
		if (pts.Count < 2) return;
		var M = S.m;
		if (!M.Invertible) return;
		double k = M.Scale;
		bool sim = Similarity(M);
		List<Vector2> src = pts;
		Mat back = default;
		if (!sim)
		{
			// stretched transform: stroke in user space (scaled to about device size) and map back
			var inv = M.Inv();
			src = strokeSrc; src.Clear();
			foreach (var p in pts) src.Add(new Vector2((float)(k * inv.X(p.X, p.Y)), (float)(k * inv.Y(p.X, p.Y))));
			back = M.Mul(new Mat(1 / k, 0, 0, 1 / k, 0, 0));
		}
		int v0 = m.nv;
		float w = (float)(S.lineWidth * k);
		if (S.dash.Length > 0)
		{
			if (dashBuf.Length != S.dash.Length) dashBuf = new double[S.dash.Length];
			var dash = dashBuf;
			double tot = 0;
			for (int i = 0; i < dash.Length; i++) { dash[i] = S.dash[i] * k; tot += dash[i]; }
			if (tot > 0)
			{
				foreach (var (pp, cl) in Geo.Dash(src, closed, dash, S.lineDashOffset * k))
					Geo.StrokePolyline(pp, cl, w, S.join, S.cap, (float)S.miterLimit, m);
			}
			else Geo.StrokePolyline(src, closed, w, S.join, S.cap, (float)S.miterLimit, m);
		}
		else Geo.StrokePolyline(src, closed, w, S.join, S.cap, (float)S.miterLimit, m);
		if (!sim)
			for (int i = v0; i < m.nv; i++) m.pos[i] = back.P(m.pos[i].X, m.pos[i].Y);
	}

	// ---------- clip ----------
	public void clip(string rule = "nonzero")
	{
		mesh.Clear();
		var polys = FillPolys(); bool eo = rule == "evenodd";
		var region = Geo.Region(polys, eo);
		var pr = surf.ClipRegion(S.clip); var prect = surf.ClipRect(S.clip);
		var bb = Geo.PolyBounds(polys);
		if (pr == null || (prect is Rect2 r0 && r0.Encloses(bb)))
			BuildFill(polys, eo, mesh);                                 // nothing outside it to cut away: same as a single clip
		else
		{
			region = Geo.Intersect(region, pr);
			var cut = new List<List<Vector2>>(region.Count);
			foreach (var q in region) { var c = new List<Vector2>(q.Count); foreach (var v in q) c.Add(new Vector2((float)v.x, (float)v.y)); cut.Add(c); }
			if (cut.Count > 0) Geo.FillComplex(cut, false, mesh);
		}
		S.clip = surf.PushClip(mesh, region, Geo.AxisRect(region));
	}

	// ---------- text ----------
	public void fillText(string text, double x, double y) => DrawText(text, x, y, null, false);
	public void fillText(string text, double x, double y, double maxWidth) => DrawText(text, x, y, maxWidth, false);
	public void strokeText(string text, double x, double y) => DrawText(text, x, y, null, true);
	public void strokeText(string text, double x, double y, double maxWidth) => DrawText(text, x, y, maxWidth, true);

	public TextMetrics measureText(string text) => new TextMetrics { width = Fonts.Measure(S.fontSpec, Spaces(text), S.direction == "rtl") };

	static string Spaces(string t)
	{
		if (t == null) return "";
		if (t.IndexOfAny(new[] { '\n', '\t', '\r', '\f' }) < 0) return t;
		return t.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace('\f', ' ');
	}

	void DrawText(string text, double x, double y, double? maxWidth, bool strokeIt)
	{
		text = Spaces(text);
		if (text.Length == 0 || Bad(x) || Bad(y)) return;
		if (!S.m.Invertible) return;
		var fs = S.fontSpec;
		var font = Fonts.Face(fs);
		bool rtl = S.direction == "rtl";
		double width = Fonts.Measure(fs, text, rtl);
		string al = S.textAlign;
		if (al == "start") al = rtl ? "right" : "left";
		else if (al == "end") al = rtl ? "left" : "right";
		double sx = 1;
		if (maxWidth.HasValue)
		{
			if (maxWidth.Value <= 0 || double.IsNaN(maxWidth.Value)) return;
			if (width > maxWidth.Value) sx = maxWidth.Value / width;
		}
		double w2 = width * sx;
		double dx = al == "left" ? 0 : al == "right" ? -w2 : -w2 / 2;
		double dy = Fonts.BaselineShift(fs, S.textBaseline);
		double ks = S.m.Scale;
		int px = Math.Max(1, (int)Math.Round(fs.size * ks));
		double f = fs.size / px;
		var T = S.m.Mul(new Mat(sx * f, 0, 0, f, x + dx, y + dy));
		var xf = new Transform2D(new Vector2((float)T.a, (float)T.b), new Vector2((float)T.c, (float)T.d), new Vector2((float)T.e, (float)T.f));
		var paint = strokeIt ? S.stroke : S.fill;
		float ga = (float)S.globalAlpha;
		var col = paint.col; col.A *= ga;
		if (col.A <= 0) return;
		if (paint.grad != null && !S.m.Invertible) return;
		var dir = rtl ? TextServer.Direction.Rtl : TextServer.Direction.Ltr;
		// a stroke of lineWidth is centred on the glyph outline
		int outline = strokeIt ? Math.Max(1, (int)Math.Round(S.lineWidth * ks / f)) : 0;
		if (ShadowOn)
		{
			// device box of the text for the shadow atlas
			double asc = fs.size * 1.2, desc = fs.size * 0.6, pad = S.lineWidth;
			var corners = new[] { S.m.P(x + dx - pad, y + dy - asc), S.m.P(x + dx + w2 + pad, y + dy - asc), S.m.P(x + dx + w2 + pad, y + dy + desc), S.m.P(x + dx - pad, y + dy + desc) };
			var b = new Rect2(corners[0], Vector2.Zero);
			foreach (var c in corners) b = b.Expand(c);
			var sc = S.shadowColor; sc.A *= ga;
			if (surf.Shadow(S.clip, b, S.shadowBlur, S.shadowOffsetX, S.shadowOffsetY, sc, out var map, out _))
				surf.MaskText(font, text, px, xf, paint.col.A, dir, map, outline);
		}
		if (paint.grad != null) surf.GradText(S.clip, font, text, px, xf, paint.grad, S.m.Inv().Mul(T), ga, dir, outline);
		else surf.Text(S.clip, font, text, px, xf, col, dir, outline);
	}

	// ---------- picture copies ----------
	// draws a texture (a copy of the game picture) at device pixels (x, y) with size (w, h)
	public void drawTexture(Rid tex, double x, double y, double w, double h)
	{
		var q = new[] { S.m.P(x, y), S.m.P(x + w, y), S.m.P(x + w, y + h), S.m.P(x, y + h) };
		var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
		surf.Image(S.clip, tex, q, uv, (float)S.globalAlpha);
	}

	// ---------- helpers ----------
	// keeps the part of a coverage mesh inside a rectangle (used when a shadowed shape is partly off screen)
	static void ClipMeshToRect(CovMesh src, Rect2 r, CovMesh dst)
	{
		dst.Clear();
		Span<(Vector2 p, float c)> a = stackalloc (Vector2, float)[16];
		Span<(Vector2 p, float c)> b = stackalloc (Vector2, float)[16];
		for (int t = 0; t < src.ni; t += 3)
		{
			int n = 3;
			for (int k = 0; k < 3; k++) { int vi = src.idx[t + k]; a[k] = (src.pos[vi], src.cov[vi]); }
			n = ClipEdge(a, n, b, 0, r.Position.X, false); n = ClipEdge(b, n, a, 0, r.End.X, true);
			n = ClipEdge(a, n, b, 1, r.Position.Y, false); n = ClipEdge(b, n, a, 1, r.End.Y, true);
			if (n < 3) continue;
			int b0 = dst.nv;
			for (int k = 0; k < n; k++) dst.V(a[k].p, a[k].c);
			for (int k = 1; k < n - 1; k++) dst.T(b0, b0 + k, b0 + k + 1);
		}
	}
	static int ClipEdge(Span<(Vector2 p, float c)> inp, int n, Span<(Vector2 p, float c)> outp, int axis, float lim, bool max)
	{
		int m = 0;
		if (n == 0) return 0;
		for (int i = 0; i < n; i++)
		{
			var cur = inp[i]; var nxt = inp[(i + 1) % n];
			float cv = axis == 0 ? cur.p.X : cur.p.Y, nv = axis == 0 ? nxt.p.X : nxt.p.Y;
			bool cin = max ? cv <= lim : cv >= lim, nin = max ? nv <= lim : nv >= lim;
			if (cin) outp[m++] = cur;
			if (cin != nin)
			{
				float t = (lim - cv) / (nv - cv);
				outp[m++] = (cur.p + (nxt.p - cur.p) * t, cur.c + (nxt.c - cur.c) * t);
			}
		}
		return m;
	}
}
