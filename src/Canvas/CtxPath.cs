using Godot;
using System;
using System.Collections.Generic;

namespace MG.Canvas;

// Path building. Points are stored already transformed to device pixels (like the browser does).
public partial class Ctx
{
	public sealed class Sub
	{
		public readonly List<Vector2> pts = new();
		public bool closed;
	}
	public readonly List<Sub> path = new();
	Sub curSub;
	const double Tol = 0.1;   // flattening tolerance in device pixels

	Vector2 last => curSub != null && curSub.pts.Count > 0 ? curSub.pts[^1] : Vector2.Zero;
	bool hasPoint => curSub != null && curSub.pts.Count > 0;

	// v54: subpaths are reused after beginPath() (new ones for every shape were a lot of garbage)
	readonly List<Sub> subPool = new();
	int subUsed;
	Sub TakeSub()
	{
		if (subUsed == subPool.Count) subPool.Add(new Sub());
		var s = subPool[subUsed++];
		s.pts.Clear(); s.closed = false;
		return s;
	}

	public void beginPath() { path.Clear(); curSub = null; subUsed = 0; }

	void NewSub(Vector2 p)
	{
		curSub = TakeSub();
		curSub.pts.Add(p);
		path.Add(curSub);
	}
	void AddPt(Vector2 p)
	{
		if (!hasPoint) { NewSub(p); return; }
		curSub.pts.Add(p);
	}
	static bool Bad(double v) => double.IsNaN(v) || double.IsInfinity(v);

	public void moveTo(double x, double y)
	{
		if (Bad(x) || Bad(y)) return;
		NewSub(S.m.P(x, y));
	}
	public void lineTo(double x, double y)
	{
		if (Bad(x) || Bad(y)) return;
		AddPt(S.m.P(x, y));
	}
	public void closePath()
	{
		if (curSub == null || curSub.pts.Count == 0) return;
		curSub.closed = true;
		var p0 = curSub.pts[0];
		curSub = TakeSub();
		curSub.pts.Add(p0);
		path.Add(curSub);
	}
	public void rect(double x, double y, double w, double h)
	{
		if (Bad(x) || Bad(y) || Bad(w) || Bad(h)) return;
		moveTo(x, y); lineTo(x + w, y); lineTo(x + w, y + h); lineTo(x, y + h); closePath();
	}
	public void roundRect(double x, double y, double w, double h, double r) => roundRect(x, y, w, h, new[] { r });
	public void roundRect(double x, double y, double w, double h, double[] radii)
	{
		if (Bad(x) || Bad(y) || Bad(w) || Bad(h)) return;
		double tl, tr, br, bl;
		switch (radii.Length)
		{
			case 1: tl = tr = br = bl = radii[0]; break;
			case 2: tl = br = radii[0]; tr = bl = radii[1]; break;
			case 3: tl = radii[0]; tr = bl = radii[1]; br = radii[2]; break;
			default: tl = radii[0]; tr = radii[1]; br = radii[2]; bl = radii[3]; break;
		}
		// negative width/height flip the corners (spec)
		if (w < 0) { x += w; w = -w; (tl, tr) = (tr, tl); (bl, br) = (br, bl); }
		if (h < 0) { y += h; h = -h; (tl, bl) = (bl, tl); (tr, br) = (br, tr); }
		double sc = 1;
		double top = tl + tr, bottom = bl + br, left = tl + bl, right = tr + br;
		if (top > w) sc = Math.Min(sc, w / top);
		if (bottom > w) sc = Math.Min(sc, w / bottom);
		if (left > h) sc = Math.Min(sc, h / left);
		if (right > h) sc = Math.Min(sc, h / right);
		tl *= sc; tr *= sc; br *= sc; bl *= sc;
		moveTo(x + tl, y);
		lineTo(x + w - tr, y);
		if (tr > 0) arc(x + w - tr, y + tr, tr, -Math.PI / 2, 0, false);
		lineTo(x + w, y + h - br);
		if (br > 0) arc(x + w - br, y + h - br, br, 0, Math.PI / 2, false);
		lineTo(x + bl, y + h);
		if (bl > 0) arc(x + bl, y + h - bl, bl, Math.PI / 2, Math.PI, false);
		lineTo(x, y + tl);
		if (tl > 0) arc(x + tl, y + tl, tl, Math.PI, Math.PI * 1.5, false);
		closePath();
	}

	public void arc(double x, double y, double r, double a0, double a1, bool ccw = false) => ellipse(x, y, r, r, 0, a0, a1, ccw);

	public void ellipse(double x, double y, double rx, double ry, double rot, double a0, double a1, bool ccw = false)
	{
		if (Bad(x) || Bad(y) || Bad(rx) || Bad(ry) || Bad(rot) || Bad(a0) || Bad(a1)) return;
		if (rx < 0 || ry < 0) { GD.PushWarning("canvas arc with negative radius"); return; }
		// same sweep rules as Chrome (CanvasPath::arc / AdjustEndAngle, in float)
		const float TWO = (float)(Math.PI * 2);
		float s0 = (float)a0, e0 = (float)a1;
		if (rx == 0 && ry == 0 || s0 == e0) { AddPt(S.m.P(x + rx * Math.Cos(a0), y + ry * Math.Sin(a0))); return; }
		float ne = e0;
		if (!ccw && e0 - s0 >= TWO) ne = s0 + TWO;
		else if (ccw && s0 - e0 >= TWO) ne = s0 - TWO;
		else if (!ccw && s0 > e0) ne = s0 + (TWO - (s0 - e0) % TWO);
		else if (ccw && s0 < e0) ne = s0 - (TWO - (e0 - s0) % TWO);
		double sweep = (double)ne - s0;
		double cr = Math.Cos(rot), sr = Math.Sin(rot);
		Vector2 At(double t)
		{
			double ex = rx * Math.Cos(t), ey = ry * Math.Sin(t);
			return S.m.P(x + ex * cr - ey * sr, y + ex * sr + ey * cr);
		}
		AddPt(At(a0));
		if (sweep == 0) return;
		double R = Math.Max(rx, ry) * MaxScale(S.m);
		int n;
		if (R <= Tol) n = 4;
		else
		{
			double step = 2 * Math.Acos(Math.Max(-1, 1 - Tol / R));
			n = (int)Math.Ceiling(Math.Abs(sweep) / Math.Max(step, 1e-3));
		}
		n = Math.Clamp(n, 1, 1024);
		// chords cut inside the curve; push the points out by half the sag so the error is split both ways
		double half = Math.Abs(sweep) / n / 2;
		double grow = 1 + (1 - Math.Cos(half)) / 2;
		rx *= grow; ry *= grow;
		for (int i = 1; i < n; i++) AddPt(At(a0 + sweep * i / n));
		rx /= grow; ry /= grow;
		AddPt(At(a0 + sweep));
	}

	static double MaxScale(Mat m) => Math.Max(Math.Sqrt(m.a * m.a + m.b * m.b), Math.Sqrt(m.c * m.c + m.d * m.d));

	public void quadraticCurveTo(double cx, double cy, double x, double y)
	{
		if (Bad(cx) || Bad(cy) || Bad(x) || Bad(y)) return;
		if (!hasPoint) moveTo(cx, cy);
		Vector2 p0 = last, p1 = S.m.P(cx, cy), p2 = S.m.P(x, y);
		double dev = (p0 - 2 * p1 + p2).Length() / 4;
		int n = Math.Clamp((int)Math.Ceiling(Math.Sqrt(dev / Tol)), 1, 256);
		for (int i = 1; i <= n; i++)
		{
			float t = (float)i / n, u = 1 - t;
			curSub.pts.Add(u * u * p0 + 2 * u * t * p1 + t * t * p2);
		}
	}

	public void bezierCurveTo(double c1x, double c1y, double c2x, double c2y, double x, double y)
	{
		if (Bad(c1x) || Bad(c1y) || Bad(c2x) || Bad(c2y) || Bad(x) || Bad(y)) return;
		if (!hasPoint) moveTo(c1x, c1y);
		Vector2 p0 = last, p1 = S.m.P(c1x, c1y), p2 = S.m.P(c2x, c2y), p3 = S.m.P(x, y);
		double dev = Math.Max((p0 - 2 * p1 + p2).Length(), (p1 - 2 * p2 + p3).Length()) * 0.75;
		int n = Math.Clamp((int)Math.Ceiling(Math.Sqrt(dev / Tol)), 1, 256);
		for (int i = 1; i <= n; i++)
		{
			float t = (float)i / n, u = 1 - t;
			curSub.pts.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
		}
	}

	// arcTo works in user space: the current point is mapped back with the inverse transform
	public void arcTo(double x1, double y1, double x2, double y2, double r)
	{
		if (Bad(x1) || Bad(y1) || Bad(x2) || Bad(y2) || Bad(r)) return;
		if (r < 0) return;
		if (!hasPoint) moveTo(x1, y1);
		var inv = S.m.Inv();
		var lp = last;
		double x0 = inv.X(lp.X, lp.Y), y0 = inv.Y(lp.X, lp.Y);
		double dx0 = x0 - x1, dy0 = y0 - y1, dx2 = x2 - x1, dy2 = y2 - y1;
		double l0 = Math.Sqrt(dx0 * dx0 + dy0 * dy0), l2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
		double cross = dx0 * dy2 - dy0 * dx2;
		if ((Math.Abs(x0 - x1) < 1e-9 && Math.Abs(y0 - y1) < 1e-9) || (Math.Abs(x1 - x2) < 1e-9 && Math.Abs(y1 - y2) < 1e-9) || r == 0 || Math.Abs(cross) < 1e-9 * l0 * l2)
		{
			lineTo(x1, y1);
			return;
		}
		double ux0 = dx0 / l0, uy0 = dy0 / l0, ux2 = dx2 / l2, uy2 = dy2 / l2;
		double cosA = ux0 * ux2 + uy0 * uy2;
		double ang = Math.Acos(Math.Clamp(cosA, -1, 1));
		double tdist = r / Math.Tan(ang / 2);
		double tx0 = x1 + ux0 * tdist, ty0 = y1 + uy0 * tdist;
		double tx2 = x1 + ux2 * tdist, ty2 = y1 + uy2 * tdist;
		// center is along the bisector
		double bx = ux0 + ux2, by = uy0 + uy2, bl = Math.Sqrt(bx * bx + by * by);
		double cd = r / Math.Sin(ang / 2);
		double cx = x1 + bx / bl * cd, cy = y1 + by / bl * cd;
		double a0 = Math.Atan2(ty0 - cy, tx0 - cx), a1 = Math.Atan2(ty2 - cy, tx2 - cx);
		// the corner arc is always the short way round
		double dd = a1 - a0;
		while (dd > Math.PI) dd -= 2 * Math.PI;
		while (dd <= -Math.PI) dd += 2 * Math.PI;
		bool ccwDir = dd < 0;
		lineTo(tx0, ty0);
		arc(cx, cy, r, a0, a1, ccwDir);
	}

	// ---------- the subpaths used for filling (each closed) ----------
	readonly List<List<Vector2>> fillPolys = new();
	List<List<Vector2>> FillPolys()
	{
		var res = fillPolys; res.Clear();
		foreach (var s in path)
		{
			if (s.pts.Count < 3) continue;
			res.Add(s.pts);
		}
		return res;
	}
}
