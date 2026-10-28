using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L19 "عافني وراح" (Ahmed Jawad): what he left behind. Three things, one in each part:
// part 1 his letter (ink drops, the pen writing row by row with wet ink, the letter folding),
// part 2 his oud (strings that vibrate: safe only at the still points, plucks that run along a string, the plectrum, the strings snapping,
// then the strings turning in their frame), part 3 your photo (it tears, the pieces flip: the front is safe, the back hurts, then they burn).
// Every hazard is a pure function of time once it exists, so hit(p, t) is right for any later t.
// The data is data/dodge_L19.json (made by tools/level19/gen.py).
public static partial class G
{
	static double L19k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L19P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L19G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double L19Ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	const string L19INK = "rgba(34,30,58,0.92)";
	static double L19Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void L19Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static bool L19InPoly(double px, double py, double[] xs, double[] ys)
	{
		bool c = false; int n = xs.Length;
		for (int i = 0, j = n - 1; i < n; j = i++)
			if ((ys[i] > py) != (ys[j] > py) && px < (xs[j] - xs[i]) * (py - ys[i]) / (ys[j] - ys[i]) + xs[i]) c = !c;
		return c;
	}
	static double L19PolyDist(double px, double py, double[] xs, double[] ys)
	{
		double m = 1e9; int n = xs.Length;
		for (int i = 0, j = n - 1; i < n; j = i++) m = Math.Min(m, L19Seg(px, py, xs[j], ys[j], xs[i], ys[i]));
		return m;
	}
	// inside the polygon or within m of its edge
	static bool L19Touch(double px, double py, double[] xs, double[] ys, double m) => L19InPoly(px, py, xs, ys) || L19PolyDist(px, py, xs, ys) < m;
	static void L19PolyPath(double[] xs, double[] ys) { g.beginPath(); for (int i = 0; i < xs.Length; i++) { if (i == 0) g.moveTo(xs[i], ys[i]); else g.lineTo(xs[i], ys[i]); } g.closePath(); }

	// a turning that follows a plan: from beat sb[i] the speed is sw[i] (radians a beat), changing over half a beat
	public class L19Spin
	{
		readonly double b0; readonly double[] tab, sb, sw; const double ST = 1.0 / 32;
		public double W(double b) { double v = sw[0]; for (int i = 1; i < sb.Length; i++) { if (b >= sb[i] + 0.5) v = sw[i]; else { if (b > sb[i]) v = sw[i - 1] + (sw[i] - sw[i - 1]) * (b - sb[i]) / 0.5; break; } } return v; }
		public L19Spin(double a0, double b0, double b1, double[] sb, double[] sw)
		{
			this.b0 = b0; this.sb = sb.Length > 0 ? sb : new[] { b0 }; this.sw = sw.Length > 0 ? sw : new[] { 0.0 };
			int n = (int)Math.Ceiling((b1 - b0) / ST) + 2; tab = new double[n]; tab[0] = a0;
			for (int k = 1; k < n; k++) { double bb = b0 + k * ST; tab[k] = tab[k - 1] + (W(bb - ST) + W(bb)) / 2 * ST; }
		}
		public double At(double bt) { double f = (bt - b0) / ST; if (f <= 0) return tab[0]; int i = (int)f; if (i >= tab.Length - 1) return tab[^1]; return tab[i] + (tab[i + 1] - tab[i]) * (f - i); }
	}

	// ---------- part 1: the letter ----------
	// an ink drop: its shadow shows from tw, it lands at ta (a blot of radius R and 6 splashes round it) and stays wet (pink) hold s,
	// then it dries to dark ink (harmless)
	public static Hz L19Drop(double x, double y, double R, double ang, double tw, double ta, double hold)
	{
		var h = new Hz { kind = "L19Drop", x = x, y = y };
		double sr = R * 0.24, sd = R * 1.22;
		h.update = (t, dt) => { if (t > ta + hold + 1.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L19k(t, tw, ta);
				g.strokeStyle = L19G(0.25 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.beginPath(); g.arc(x, y, R, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = L19G(0.08 + 0.14 * k); circle(x, y, R * (0.25 + 0.75 * k));
				// the drop falling in from above
				double fy = y - 420 * (1 - k) * (1 - k); g.fillStyle = L19G(0.5 + 0.4 * k);
				g.beginPath(); g.moveTo(x, fy - 26); g.quadraticCurveTo(x + 13, fy - 4, x, fy + 9); g.quadraticCurveTo(x - 13, fy - 4, x, fy - 26); g.fill();
				return;
			}
			double u = t - ta, gr = Math.Min(1, u / 0.08);
			if (u < hold)
			{
				g.fillStyle = u < 0.05 ? PINKW : PINK; circle(x, y, R * gr);
				for (int i = 0; i < 6; i++) { double a = ang + i * TAU / 6; circle(x + JS.cos(a) * sd * gr, y + JS.sin(a) * sd * gr, sr * gr); }
				if (u >= 0.05) { g.fillStyle = "rgba(110,0,36,0.35)"; circle(x - R * 0.2, y - R * 0.2, R * 0.45 * gr); }
				return;
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= ta + hold) return false; double gr = Math.Min(1, (t - ta) / 0.08), m = p.r - 2;
			if (hypot(p.x - x, p.y - y) < R * gr + m) return true;
			for (int i = 0; i < 6; i++) { double a = ang + i * TAU / 6; if (hypot(p.x - x - JS.cos(a) * sd * gr, p.y - y - JS.sin(a) * sd * gr) < sr * gr + m) return true; }
			return false;
		};
		return h;
	}

	// the same drop once dry: harmless ink, drawn under every attack (it sits just above the background) so it never hides pink
	public static Hz L19Stain(double x, double y, double R, double ang, double ta, double hold)
	{
		var h = new Hz { kind = "L19Stain", x = x, y = y };
		double sr = R * 0.24, sd = R * 1.22;
		h.update = (t, dt) => { if (t > ta + hold + 1.2) h.dead = true; };
		h.draw = t =>
		{
			double u = t - ta; if (u < hold) return;
			double f = 1 - L19k(u, hold + 0.4, hold + 1.2); g.fillStyle = L19INK.Replace("0.92", str(0.92 * f)); circle(x, y, R);
			for (int i = 0; i < 6; i++) { double a = ang + i * TAU / 6; circle(x + JS.cos(a) * sd, y + JS.sin(a) * sd, sr); }
		};
		h.hit = (p, t) => false;
		return h;
	}
	static void L19Under(Hz h) { int i = hazards.FindLastIndex(z => z.kind == "L19Stage"); hazards.Insert(i + 1, h); }

	// the pen writes a page: row ord[j] (rh high, the first row's top at y0) starts at t0 + j * dr and is written in wr s, from the right
	// to the left (rtl) or the other way. The ink is wet (pink) for wet s after the pen passes, then it is dry ink (harmless).
	// Each row has ng spaces between its words (centres gx, gw wide) where there is no ink. The rows and their spaces show from tw
	public static Hz L19Pen(double y0, double rh, double[] ord, double[] gx, int ng, double gw, bool rtl, double tw, double t0, double dr, double wr, double wet, double te)
	{
		var h = new Hz { kind = "L19Pen" }; int n = ord.Length; bool down = n < 2 || ord[1] > ord[0];
		var rowJ = new Dictionary<int, int>(); for (int j = 0; j < n; j++) rowJ[(int)ord[j]] = j;
		double st(int j) => t0 + j * dr;
		// when the pen passes x on order j's row
		double tx(int j, double x) => st(j) + (rtl ? (W - x) / W : x / W) * wr;
		bool gap(int j, double x) { for (int q = 0; q < ng; q++) if (Math.Abs(x - gx[j * ng + q]) < gw / 2) return true; return false; }
		double penX(int j, double t) { double k = clamp((t - st(j)) / wr, 0, 1); return rtl ? W - k * W : k * W; }
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L19k(t, tw, tw + 0.5), 1 - L19k(t, te, te + 0.6)); if (a <= 0) return;
			g.save(); g.globalAlpha = a;
			for (int j = 0; j < n; j++)
			{
				double y = y0 + ord[j] * rh, s0 = st(j);
				// the spaces (light slots on the row) show from the page's start
				if (t < s0 + wr + wet)
				{
					g.fillStyle = L19G(0.07 + 0.1 * L19k(t, tw, s0)); for (int q = 0; q < ng; q++) g.fillRect(gx[j * ng + q] - gw / 2, y + 4, gw, rh - 8);
					if (t < s0) { g.strokeStyle = L19G(0.15 + 0.35 * L19k(t, s0 - 1.2, s0)); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 }); g.strokeRect(2, y + 2, W - 4, rh - 4); g.setLineDash(new double[0]); }
				}
				if (t < s0) continue;
				double xp = penX(j, t), xd = rtl ? W - clamp((t - s0 - wet) / wr, 0, 1) * W : clamp((t - s0 - wet) / wr, 0, 1) * W;
				// word by word: the ink between the pen and the dry edge is wet
				var cuts = new List<double> { 0, W }; for (int q = 0; q < ng; q++) { cuts.Add(gx[j * ng + q] - gw / 2); cuts.Add(gx[j * ng + q] + gw / 2); }
				cuts.Sort();
				for (int c = 0; c + 1 < cuts.Count; c += 2)
				{
					double w0 = cuts[c], w1 = cuts[c + 1]; if (w1 <= w0) continue;
					double lo = rtl ? Math.Max(w0, xp) : w0, hi = rtl ? w1 : Math.Min(w1, xp); if (hi <= lo) continue;
					// dry part
					double dlo = rtl ? Math.Max(lo, xd) : lo, dhi = rtl ? hi : Math.Min(hi, xd);
					if (dhi > dlo) L19Script(dlo, dhi, y + rh / 2, rh, L19INK, j * 7 + c, false);
					double wlo = rtl ? lo : Math.Max(lo, xd), whi = rtl ? Math.Min(hi, xd) : hi;
					if (whi > wlo)
					{
						g.fillStyle = PINK; g.fillRect(wlo, y, whi - wlo, rh);
						L19Script(wlo, whi, y + rh / 2, rh, "rgba(110,0,36,0.55)", j * 7 + c, true);
					}
				}
				// the pen (its body leans over the rows not written yet, never over wet ink)
				if (t < s0 + wr) L19PenDraw(xp, y + rh / 2, rtl, down);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= te) return false; double m = p.r - 2;
			int r0 = (int)Math.Floor((p.y - m - y0) / rh), r1 = (int)Math.Floor((p.y + m - y0) / rh);
			for (int r = r0; r <= r1; r++)
			{
				if (!rowJ.TryGetValue(r, out int j)) continue; if (t < st(j)) continue;
				foreach (var x in new[] { p.x - m, p.x, p.x + m })
				{
					if (x < 0 || x > W || gap(j, x)) continue; double tp = tx(j, x);
					if (t >= tp && t < tp + wet) return true;
				}
			}
			return false;
		};
		return h;
	}
	// handwriting on a row from x0 to x1 (loops and strokes like a line of Arabic writing)
	static void L19Script(double x0, double x1, double cy, double rh, string col, int seed, bool wet)
	{
		g.strokeStyle = col; g.lineWidth = wet ? 5 : 4; g.lineCap = "round"; g.beginPath();
		double amp = rh * 0.22; bool first = true;
		for (double x = x0; x <= x1 + 0.1; x += 6)
		{
			double y = cy + JS.sin(x * 0.045 + seed) * amp * 0.5 + JS.sin(x * 0.13 + seed * 1.7) * amp * 0.45;
			if (first) { g.moveTo(x, y); first = false; } else g.lineTo(x, y);
		}
		g.stroke();
		// dots and tall letters
		g.fillStyle = col;
		for (double x = x0 + 14 + (seed * 13) % 30; x < x1 - 8; x += 46) { circle(x, cy - amp * 1.25, wet ? 4 : 3); }
		g.beginPath(); for (double x = x0 + 30 + (seed * 7) % 40; x < x1 - 8; x += 74) { g.moveTo(x, cy + amp * 0.3); g.lineTo(x + 3, cy - amp * 1.6); } g.stroke();
		g.lineCap = "butt";
	}
	static void L19PenDraw(double x, double y, bool rtl, bool down)
	{
		// the body leans to the side not written yet (rtl: left) and to the rows not written yet
		double ang = JS.atan2(down ? 1 : -1, rtl ? -0.35 : 0.35);
		g.save(); g.translate(x, y); g.rotate(ang - Math.PI / 2);
		g.fillStyle = "rgba(70,66,84,1)"; g.fillRect(-6, 32, 12, 118);
		g.fillStyle = "rgba(150,146,170,1)"; g.fillRect(-7, 26, 14, 10);
		g.fillStyle = PINK; g.beginPath(); g.moveTo(-7, 26); g.lineTo(7, 26); g.lineTo(0, 0); g.closePath(); g.fill();
		g.restore();
	}

	// the letter folds: the part of the page past the fold line (through (fx,fy), its normal at angle na points into the flap) turns over
	// the line from t1 for fl s, lies on the other side hold s, and turns back. The turning flap shows its back (pink) and hurts.
	// The fold line and the place it lands show from tw
	public static Hz L19Fold(double fx, double fy, double na, double tw, double t1, double fl, double hold)
	{
		var h = new Hz { kind = "L19Fold" }; double nx = JS.cos(na), ny = JS.sin(na), ux = -ny, uy = nx; double t2 = t1 + fl + hold, t3 = t2 + fl;
		// the flap: the screen past the line
		var px = new List<double>(); var py = new List<double>();
		{
			var cx = new[] { 0.0, W, W, 0 }; var cy = new[] { 0.0, 0, H, H };
			for (int i = 0; i < 4; i++)
			{
				int j = (i + 1) % 4; double si = (cx[i] - fx) * nx + (cy[i] - fy) * ny, sj = (cx[j] - fx) * nx + (cy[j] - fy) * ny;
				if (si > 0) { px.Add(cx[i]); py.Add(cy[i]); }
				if ((si > 0) != (sj > 0)) { double u = si / (si - sj); px.Add(cx[i] + (cx[j] - cx[i]) * u); py.Add(cy[i] + (cy[j] - cy[i]) * u); }
			}
		}
		double th(double t) => t < t1 ? 0 : t < t1 + fl ? Math.PI * L19Ease((t - t1) / fl) : t < t2 ? Math.PI : t < t3 ? Math.PI * (1 - L19Ease((t - t2) / fl)) : 0;
		bool cov(double qx, double qy, double c)
		{
			double s = (qx - fx) * nx + (qy - fy) * ny, u = (qx - fx) * ux + (qy - fy) * uy;
			if (Math.Abs(c) < 0.03) return Math.Abs(s) < 4 && L19InPoly(fx + ux * u + nx * 2, fy + uy * u + ny * 2, px.ToArray(), py.ToArray());
			double d = s / c; if (d <= 0) return false; double ox = fx + ux * u + nx * d, oy = fy + uy * u + ny * d;
			return ox >= 0 && ox <= W && oy >= 0 && oy <= H;
		}
		var PX = px.ToArray(); var PY = py.ToArray();
		h.update = (t, dt) => { if (t > t3 + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > t3) return; double a = th(t), c = JS.cos(a);
			if (t < t1)
			{
				double k = L19k(t, tw, t1);
				// the flap and where it lands, in grey; the fold line dashed; arrows across it
				g.strokeStyle = L19G(0.25 + 0.5 * k); g.lineWidth = 3; L19PolyPath(PX, PY); g.stroke();
				var mx = PX.Select((x, i) => x - 2 * nx * ((x - fx) * nx + (PY[i] - fy) * ny)).ToArray(); var my = PY.Select((y, i) => y - 2 * ny * ((PX[i] - fx) * nx + (y - fy) * ny)).ToArray();
				g.strokeStyle = L19G(0.2 + 0.4 * k); g.lineWidth = 2; g.setLineDash(new double[] { 6, 8 }); L19PolyPath(mx, my); g.stroke(); g.setLineDash(new double[0]);
				L19Dash(fx - ux * 1600, fy - uy * 1600, fx + ux * 1600, fy + uy * 1600, L19G(0.4 + 0.5 * k), 3);
				g.strokeStyle = L19G(0.35 + 0.5 * k); g.lineWidth = 3;
				for (int i = -2; i <= 2; i++)
				{
					double bx = fx + ux * i * 150, by = fy + uy * i * 150; if (bx < -50 || bx > W + 50 || by < -50 || by > H + 50) continue;
					double l = 60 + 20 * k; g.beginPath(); g.moveTo(bx + nx * l, by + ny * l); g.quadraticCurveTo(bx + nx * 10 + ux * 18, by + ny * 10 + uy * 18, bx - nx * l * 0.8, by - ny * l * 0.8); g.stroke();
					g.beginPath(); g.moveTo(bx - nx * l * 0.8 + ux * 9 + nx * 9, by - ny * l * 0.8 + uy * 9 + ny * 9); g.lineTo(bx - nx * l * 0.8, by - ny * l * 0.8); g.lineTo(bx - nx * l * 0.8 - ux * 9 + nx * 9, by - ny * l * 0.8 - uy * 9 + ny * 9); g.stroke();
				}
				return;
			}
			var qx = PX.Select((x, i) => { double s = (x - fx) * nx + (PY[i] - fy) * ny; return x - nx * s * (1 - c); }).ToArray();
			var qy = PY.Select((y, i) => { double s = (PX[i] - fx) * nx + (y - fy) * ny; return y - ny * s * (1 - c); }).ToArray();
			if (Math.Abs(c) < 0.03) { g.strokeStyle = PINK; g.lineWidth = 8; g.beginPath(); g.moveTo(fx - ux * 1600, fy - uy * 1600); g.lineTo(fx + ux * 1600, fy + uy * 1600); g.stroke(); return; }
			g.fillStyle = PINK; L19PolyPath(qx, qy); g.fill();
			// the paper's back: a shadow by the fold and faint lines
			g.save(); L19PolyPath(qx, qy); g.clip();
			var gr = g.createLinearGradient(fx, fy, fx - nx * c * 400, fy - ny * c * 400); gr.addColorStop(0, "rgba(90,0,30,0.35)"); gr.addColorStop(1, "rgba(90,0,30,0)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
			g.strokeStyle = "#fff"; g.lineWidth = 2; L19PolyPath(qx, qy); g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < t1 || t >= t3) return false; double c = JS.cos(th(t)), m = p.r - 2;
			return cov(p.x, p.y, c) || cov(p.x + nx * m, p.y + ny * m, c) || cov(p.x - nx * m, p.y - ny * m, c) || cov(p.x + ux * m, p.y + uy * m, c) || cov(p.x - ux * m, p.y - uy * m, c);
		};
		return h;
	}

	// ---------- part 2: the oud ----------
	// A set of strings in a frame (centre cx,cy, turned by spin): string i lies along the frame's x from -lh to lh at frame y ys[i].
	// A string that vibrates covers the shape of its wave (pink): standing waves with n loops (the still points, the nodes, are safe),
	// plucks that run along the string and bounce at its ends, waves the plectrum starts as it crosses, and at the end it can snap.
	// A still string is grey and harmless.
	public class L19StrO
	{
		public double cx = W / 2, cy = H / 2, lh = 700, A = 90, th = 6, md = 0.2, pwn = 0.9, te;
		public L19Spin spin; public double[] ys, on, off;
		public double[] hs; public int[] hn;            // from hs[j] string i has hn[j * ns + i] loops (0 = still); changes over md s; the next shows pwn s before
		public double[] pb, pv, pw, pa, pl, pxs; public int[] pi;   // plucks: at pb on string pi at frame x pxs, two bumps (half width pw, height pa) at pv px/s for pl s
		public double pfw = 0.6;                         // a pluck's finger shows this long before
		public double[] eb, ed; public int[] ei, en;      // waves the plectrum starts
		public double[] kt, kx, ky; public double kr = 30, kwn = 1.2;   // the plectrum's path (times, places), its warning
		public double[] sb, sx, sa, sd, sh; public int[] si; public double swn = 1.0, sp = 600;   // snaps: at sb string si snaps at frame x sx, halves (from the pegs at -sp and sp) swing sa radians in sd s, hold sh s
	}
	const double L19VT = 4;   // a string moving less than this is still (not drawn pink, harmless)
	public static Hz L19Strings(L19StrO o, double tw)
	{
		var h = new Hz { kind = "L19Strings" }; int ns = o.ys.Length;
		double ang(double t) => o.spin == null ? 0 : o.spin.At(BT(t));
		double E(int n, double x) => n <= 0 ? 0 : o.A * Math.Abs(JS.sin(n * Math.PI * (x + o.lh) / (2 * o.lh)));
		double snapT(int i) { if (o.sb != null) for (int q = 0; q < o.sb.Length; q++) if (o.si[q] == i) return o.sb[q]; return double.PositiveInfinity; }
		int hj(double t) { int j = -1; if (o.hs != null) for (int q = 0; q < o.hs.Length; q++) if (o.hs[q] <= t) j = q; return j; }
		double stand(int i, double x, double t)
		{
			int j = hj(t); if (j < 0) return 0; int n1 = o.hn[j * ns + i], n0 = j > 0 ? o.hn[(j - 1) * ns + i] : 0; double u = clamp((t - o.hs[j]) / o.md, 0, 1);
			return (1 - u) * E(n0, x) + u * E(n1, x);
		}
		double bounce(double q) { double L = 2 * o.lh, r = ((q + o.lh) % (2 * L) + 2 * L) % (2 * L); return (r > L ? 2 * L - r : r) - o.lh; }
		double env(int i, double x, double t)
		{
			if (t < o.on[i] || t >= o.off[i] || t >= snapT(i) || x < -o.lh || x > o.lh) return 0;
			double v = stand(i, x, t);
			if (o.eb != null) for (int q = 0; q < o.eb.Length; q++) { if (o.ei[q] != i) continue; double u = t - o.eb[q]; if (u >= 0 && u < o.ed[q]) v = Math.Max(v, E(o.en[q], x) * Math.Min(1, u / 0.06)); }
			if (o.pb != null) for (int q = 0; q < o.pb.Length; q++)
			{
				if (o.pi[q] != i) continue; double u = t - o.pb[q]; if (u < 0 || u >= o.pl[q]) continue;
				for (int sd = -1; sd <= 1; sd += 2) { double xb = bounce(o.pxs[q] + sd * o.pv[q] * u), d = Math.Abs(x - xb) / o.pw[q]; if (d < 1) { double c = JS.cos(d * Math.PI / 2); v = Math.Max(v, o.pa[q] * c * c * Math.Min(1, u / 0.05)); } }
			}
			return v;
		}
		(double x, double y) toF(double x, double y, double a) { double dx = x - o.cx, dy = y - o.cy, c = JS.cos(a), s = JS.sin(a); return (dx * c + dy * s, -dx * s + dy * c); }
		(double x, double y) toS(double fx, double fy, double a) { double c = JS.cos(a), s = JS.sin(a); return (o.cx + fx * c - fy * s, o.cy + fx * s + fy * c); }
		(double x, double y) plec(double t)
		{
			int n = o.kt.Length; if (t <= o.kt[0]) return (o.kx[0], o.ky[0]); if (t >= o.kt[n - 1]) return (o.kx[n - 1], o.ky[n - 1]);
			int i = 0; while (i < n - 2 && t >= o.kt[i + 1]) i++; double u = (t - o.kt[i]) / (o.kt[i + 1] - o.kt[i]);
			return (o.kx[i] + (o.kx[i + 1] - o.kx[i]) * u, o.ky[i] + (o.ky[i + 1] - o.ky[i]) * u);
		}
		// a snapped string's halves: (anchor, angle now)
		double swing(int q, double t) => o.sa[q] * L19Ease((t - o.sb[q]) / o.sd[q]);
		h.update = (t, dt) => { if (t > o.te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = ang(t), al = Math.Min(L19k(t, tw, tw + 0.5), 1 - L19k(t, o.te, o.te + 0.5)); if (al <= 0) return;
			g.save(); g.globalAlpha = al; g.translate(o.cx, o.cy); g.rotate(a);
			for (int i = 0; i < ns; i++)
			{
				double y = o.ys[i]; if (t < o.on[i] - 0.6 || t >= o.off[i] + 0.3) continue;
				double ka = Math.Min(L19k(t, o.on[i] - 0.6, o.on[i]), 1 - L19k(t, o.off[i], o.off[i] + 0.3));
				double tsn = snapT(i);
				if (t >= tsn) continue;
				g.strokeStyle = L19G(0.55 * ka); g.lineWidth = 3; g.beginPath(); g.moveTo(-o.lh, y); g.lineTo(o.lh, y); g.stroke();
				// the vibrating shape
				var xs = new List<double>(); for (double x = -o.lh; x <= o.lh + 0.1; x += 5) xs.Add(x);
				var ev = xs.Select(x => env(i, x, t)).ToArray();
				int k0 = -1;
				for (int k = 0; k <= xs.Count; k++)
				{
					bool on = k < xs.Count && ev[k] > L19VT;
					if (on && k0 < 0) k0 = k;
					if (!on && k0 >= 0)
					{
						g.fillStyle = PINK; g.beginPath();
						for (int q = k0; q < k; q++) { double yy = y - ev[q] - o.th / 2; if (q == k0) g.moveTo(xs[q], y - o.th / 2); g.lineTo(xs[q], yy); }
						g.lineTo(xs[k - 1], y + o.th / 2);
						for (int q = k - 1; q >= k0; q--) g.lineTo(xs[q], y + ev[q] + o.th / 2);
						g.closePath(); g.fill();
						k0 = -1;
					}
				}
				// the string itself (white where it shakes, grey where it is still)
				double ph = JS.sin(t * 38 + i);
				g.strokeStyle = "rgba(255,240,245,0.9)"; g.lineWidth = 3; g.beginPath(); bool pen = false;
				for (int k = 0; k < xs.Count; k++) { if (ev[k] > L19VT) { double yy = y + ev[k] * ph * 0.8; if (!pen) { g.moveTo(xs[k], yy); pen = true; } else g.lineTo(xs[k], yy); } else pen = false; }
				g.stroke();
				// the nodes of the standing wave now (white dots) and of the next one (grey rings)
				int j = hj(t);
				if (j >= 0 && o.hn[j * ns + i] > 0 && ev.Any(v => v > L19VT)) { int n = o.hn[j * ns + i]; g.fillStyle = "rgba(255,255,255,0.95)"; for (int q = 0; q <= n; q++) circle(-o.lh + 2 * o.lh * q / n, y, 5); }
				if (o.hs != null && j + 1 < o.hs.Length && t >= o.hs[j + 1] - o.pwn && o.hs[j + 1] < tsn)
				{
					int n = o.hn[(j + 1) * ns + i]; double k = L19k(t, o.hs[j + 1] - o.pwn, o.hs[j + 1]);
					if (n > 0)
					{
						g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 3; for (int q = 0; q <= n; q++) { g.beginPath(); g.arc(-o.lh + 2 * o.lh * q / n, y, 9 + 6 * (1 - k), 0, TAU); g.stroke(); }
						g.lineWidth = 1.5; g.setLineDash(new double[] { 5, 6 }); g.beginPath();
						for (double x = -o.lh; x <= o.lh; x += 12) { double yy = y - E(n, x) - o.th / 2; if (x == -o.lh) g.moveTo(x, yy); else g.lineTo(x, yy); }
						for (double x = o.lh; x >= -o.lh; x -= 12) g.lineTo(x, y + E(n, x) + o.th / 2);
						g.stroke(); g.setLineDash(new double[0]);
					}
					else { g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 5, 6 }); g.beginPath(); g.moveTo(-o.lh, y - 14); g.lineTo(o.lh, y - 14); g.moveTo(-o.lh, y + 14); g.lineTo(o.lh, y + 14); g.stroke(); g.setLineDash(new double[0]); }
				}
				// a finger before a pluck
				if (o.pb != null) for (int q = 0; q < o.pb.Length; q++)
				{
					if (o.pi[q] != i || t < o.pb[q] - o.pfw || t >= o.pb[q]) continue; double k = L19k(t, o.pb[q] - o.pfw, o.pb[q]);
					g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(o.pxs[q], y, 40 - 26 * k, 0, TAU); g.stroke();
					g.fillStyle = L19G(0.3 + 0.4 * k); circle(o.pxs[q], y, 7);
					// which way the bumps will run
					for (int sd = -1; sd <= 1; sd += 2) { double x0 = o.pxs[q] + sd * 22; g.beginPath(); g.moveTo(x0, y - 8); g.lineTo(x0 + sd * 14, y); g.lineTo(x0, y + 8); g.stroke(); }
				}
			}
			// the snaps: the fraying mark and the areas the halves will sweep before (first, under everything), then the halves whip round their pegs
			if (o.sb != null) for (int pass = 0; pass < 2; pass++) for (int q = 0; q < o.sb.Length; q++)
			{
				int i = o.si[q]; double y = o.ys[i], s0 = o.sb[q], P = o.sp;
				if (t < s0 - o.swn || t > s0 + o.sd[q] + o.sh[q] + 0.6) continue;
				double L1 = o.sx[q] + P, L2 = P - o.sx[q];
				if ((t < s0) != (pass == 0)) continue;
				if (t < s0)
				{
					double k = L19k(t, s0 - o.swn, s0);
					// the areas the halves will sweep
					g.fillStyle = L19G(0.06 + 0.14 * k);
					g.beginPath(); g.moveTo(-P, y); g.arc(-P, y, L1, Math.Min(0, o.sa[q]), Math.Max(0, o.sa[q])); g.closePath(); g.fill();
					g.beginPath(); g.moveTo(P, y); g.arc(P, y, L2, Math.PI + Math.Min(0, -o.sa[q]), Math.PI + Math.Max(0, -o.sa[q])); g.closePath(); g.fill();
					g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 7, 7 });
					g.beginPath(); g.arc(-P, y, L1, Math.Min(0, o.sa[q]), Math.Max(0, o.sa[q])); g.stroke();
					g.beginPath(); g.arc(P, y, L2, Math.PI + Math.Min(0, -o.sa[q]), Math.PI + Math.Max(0, -o.sa[q])); g.stroke(); g.setLineDash(new double[0]);
					g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.6 * k)})"; g.lineWidth = 2; g.beginPath();
					for (int r = 0; r < 6; r++) { double aa = r * 1.05 + t * 9; g.moveTo(o.sx[q], y); g.lineTo(o.sx[q] + JS.cos(aa) * 16 * k, y + JS.sin(aa) * 16 * k); }
					g.stroke(); continue;
				}
				double sw = swing(q, t); bool live = t < s0 + o.sd[q] + o.sh[q];
				double fa = live ? 1 : 1 - L19k(t, s0 + o.sd[q] + o.sh[q], s0 + o.sd[q] + o.sh[q] + 0.6);
				g.strokeStyle = live ? (t - s0 < 0.05 ? "#fff" : PINK) : L19G(0.5 * fa); g.lineWidth = live ? 12 : 3; g.lineCap = "round";
				g.beginPath(); g.moveTo(-P, y); g.lineTo(-P + JS.cos(sw) * L1, y + JS.sin(sw) * L1); g.moveTo(P, y); g.lineTo(P - JS.cos(sw) * L2, y + JS.sin(sw) * L2); g.stroke(); g.lineCap = "butt";
			}
			g.restore();
			// the plectrum and its path (screen space)
			if (o.kt != null && t >= o.kt[0] - o.kwn && t < o.kt[^1])
			{
				g.save(); g.globalAlpha = al;
				g.strokeStyle = L19G(0.55); g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 }); g.beginPath();
				for (int s = 0; s <= 24; s++) { var q = plec(Math.Max(t, o.kt[0]) + s * 2 * SPB / 24); if (s == 0) g.moveTo(q.x, q.y); else g.lineTo(q.x, q.y); }
				g.stroke(); g.setLineDash(new double[0]);
				var pq = plec(t); double dir = 0; { var p2 = plec(t + 0.02); if (hypot(p2.x - pq.x, p2.y - pq.y) > 0.01) dir = JS.atan2(p2.y - pq.y, p2.x - pq.x); }
				if (t >= o.kt[0])
				{
					g.save(); g.translate(pq.x, pq.y); g.rotate(dir + Math.PI / 2); g.fillStyle = PINK;
					g.beginPath(); g.moveTo(0, o.kr * 1.35); g.quadraticCurveTo(o.kr * 1.25, o.kr * 0.2, 0, -o.kr); g.quadraticCurveTo(-o.kr * 1.25, o.kr * 0.2, 0, o.kr * 1.35); g.fill();
					circle(0, 0, o.kr);   // the round body = the whole hit circle, so all of it shows pink
					g.fillStyle = "rgba(110,0,36,0.4)"; circle(0, 0, o.kr * 0.4); g.restore();
				}
				else { double k = L19k(t, o.kt[0] - o.kwn, o.kt[0]); g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(pq.x, pq.y, o.kr + 30 * (1 - k), 0, TAU); g.stroke(); }
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			if (t >= o.te) return false; double m = p.r - 2, a = ang(t); var f = toF(p.x, p.y, a);
			for (int i = 0; i < ns; i++)
			{
				double d = Math.Abs(f.y - o.ys[i]); if (d > o.A + o.th + m + 2) continue;
				double v = Math.Max(env(i, f.x - m, t), Math.Max(env(i, f.x, t), env(i, f.x + m, t)));
				if (v > L19VT + 1 && d < v - 1 + o.th / 2 + m) return true;
			}
			if (o.sb != null) for (int q = 0; q < o.sb.Length; q++)
			{
				double s0 = o.sb[q]; if (t < s0 || t >= s0 + o.sd[q] + o.sh[q]) continue; int i = o.si[q]; double y = o.ys[i], sw = swing(q, t), P = o.sp, L1 = o.sx[q] + P, L2 = P - o.sx[q];
				if (L19Seg(f.x, f.y, -P, y, -P + JS.cos(sw) * L1, y + JS.sin(sw) * L1) < 6 + m) return true;
				if (L19Seg(f.x, f.y, P, y, P - JS.cos(sw) * L2, y + JS.sin(sw) * L2) < 6 + m) return true;
			}
			if (o.kt != null && t >= o.kt[0] && t < o.kt[^1]) { var q = plec(t); if (hypot(p.x - q.x, p.y - q.y) < o.kr + m) return true; }
			return false;
		};
		return h;
	}

	// ---------- part 3: the photo ----------
	// The photo covers the screen and tears along lines into pieces. Piece k is the polygon (vx, vy) of the whole photo; after each tear q
	// it drifts dd[q] along (tnx, tny) times its side psd[k * nt + q] (+1, -1, 0) and turns a little (rot[k]) after the last tear.
	// A tear runs along its line (pink while it tears) from tt for td s. Each piece flips at its times (front: the photo, safe; back: pink),
	// its corner lifts fwn s before. Fires start at (bx, by) at bt and run outward at bv px/s: the pink front (fw wide) burns the pieces;
	// what is behind the front is ash (safe). The table between the pieces never burns
	public class L19PhO
	{
		public double[] vx, vy; public int[] vc;           // pieces (vertex counts, then the vertices one after another)
		public double[] tt, td, tnx, tny, dd, tlx, tly; public int[] tlc, psd;   // tears: time, time to tear, drift direction, drift, the line's points
		public double dr = 1.2;                                   // how long a drift takes
		public double[] gk, gv;                                   // the pieces come together and go apart again: from gk[i] the drift is times gv[i] (changing over gd s)
		public double gd = 0.5;
		public double[] rot;                                      // each piece's turn after the last tear (radians)
		public double[] ft; public int[] fc; public double fd = 0.18, fwn = 0.9;   // flips (counts per piece, then times)
		public double[] bx, by, bt, bv; public double fw = 30, bwn = 1.0;          // fires
		public double t0, te, twn = 1.2;
	}
	public static Hz L19Photo(L19PhO o, double tw)
	{
		var h = new Hz { kind = "L19Photo" }; int np = o.vc.Length, nt = o.tt.Length;
		var px = new double[np][]; var py = new double[np][]; var pcx = new double[np]; var pcy = new double[np];
		{ int s = 0; for (int k = 0; k < np; k++) { px[k] = o.vx.Skip(s).Take(o.vc[k]).ToArray(); py[k] = o.vy.Skip(s).Take(o.vc[k]).ToArray(); pcx[k] = px[k].Average(); pcy[k] = py[k].Average(); s += o.vc[k]; } }
		var tlx = new double[nt][]; var tly = new double[nt][];
		{ int s = 0; for (int q = 0; q < nt; q++) { tlx[q] = o.tlx.Skip(s).Take(o.tlc[q]).ToArray(); tly[q] = o.tly.Skip(s).Take(o.tlc[q]).ToArray(); s += o.tlc[q]; } }
		var fts = new double[np][]; { int s = 0; for (int k = 0; k < np; k++) { fts[k] = o.ft.Skip(s).Take(o.fc[k]).ToArray(); s += o.fc[k]; } }
		double lastTear = nt > 0 ? o.tt[nt - 1] + o.td[nt - 1] : o.t0;
		double gather(double t)
		{
			if (o.gk == null) return 1; double v = 1;
			for (int i = 0; i < o.gk.Length; i++) { if (t < o.gk[i]) break; double u = L19Ease((t - o.gk[i]) / o.gd); v = v + (o.gv[i] - v) * u; if (u < 1) break; }
			return v;
		}
		// a piece's place now: offset and turn
		(double dx, double dy, double r) pose(int k, double t)
		{
			double dx = 0, dy = 0;
			for (int q = 0; q < nt; q++) { int sd = o.psd[k * nt + q]; if (sd == 0) continue; double u = L19Ease((t - o.tt[q] - o.td[q]) / o.dr); dx += sd * o.dd[q] * u * o.tnx[q]; dy += sd * o.dd[q] * u * o.tny[q]; }
			double gf = gather(t);
			return (dx * gf, dy * gf, o.rot[k] * L19Ease((t - lastTear) / (o.dr * 2)) * gf);
		}
		// a piece's flip now: scale of its width (1 .. 0 .. 1) and whether the back shows
		(double sc, bool back) flip(int k, double t)
		{
			bool back = false; double sc = 1;
			foreach (var f in fts[k]) { if (t < f) break; double u = (t - f) / o.fd; if (u < 1) { sc = Math.Abs(JS.cos(u * Math.PI)); back = u >= 0.5 ? !back : back; return (Math.Max(0.02, sc), back); } back = !back; }
			return (1, back);
		}
		// whether a piece has started tearing off (until then the photo shows whole)
		(double[] x, double[] y) world(int k, double t)
		{
			var ps = pose(k, t); var fl = flip(k, t); double c = JS.cos(ps.r), s = JS.sin(ps.r); int n = px[k].Length; var X = new double[n]; var Y = new double[n];
			for (int i = 0; i < n; i++) { double lx = (px[k][i] - pcx[k]) * fl.sc, ly = py[k][i] - pcy[k]; X[i] = pcx[k] + ps.dx + lx * c - ly * s; Y[i] = pcy[k] + ps.dy + lx * s + ly * c; }
			return (X, Y);
		}
		// fire: how far each fire has gone
		double fr(int q, double t) => t < o.bt[q] ? -1 : (t - o.bt[q]) * o.bv[q];
		bool ash(double x, double y, double t) { if (o.bt != null) for (int q = 0; q < o.bt.Length; q++) { double r = fr(q, t); if (r > o.fw && hypot(x - o.bx[q], y - o.by[q]) < r - o.fw) return true; } return false; }
		bool front(double x, double y, double t, double m) { if (o.bt != null) for (int q = 0; q < o.bt.Length; q++) { double r = fr(q, t); if (r < 0) continue; double d = hypot(x - o.bx[q], y - o.by[q]); if (d < r + m && d > r - o.fw - m) return true; } return false; }
		// how much of the tear's line is torn
		double tearK(int q, double t) => L19k(t, o.tt[q], o.tt[q] + o.td[q]);
		h.update = (t, dt) => { if (t > o.te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double al = Math.Min(L19k(t, tw, tw + 0.6), 1 - L19k(t, o.te, o.te + 0.6)); if (al <= 0) return;
			g.save(); g.globalAlpha = al;
			for (int k = 0; k < np; k++)
			{
				var (X, Y) = world(k, t); var fl = flip(k, t);
				g.save(); L19PolyPath(X, Y); g.clip();
				if (fl.back) { g.fillStyle = PINK; g.fillRect(0, 0, W, H); g.strokeStyle = "rgba(110,0,36,0.35)"; g.lineWidth = 3; g.beginPath(); for (double d = -H; d < W; d += 34) { g.moveTo(d, 0); g.lineTo(d + H, H); } g.stroke(); }
				else
				{
					// the photo itself, moved with the piece
					var ps = pose(k, t); g.translate(pcx[k] + ps.dx, pcy[k] + ps.dy); g.rotate(ps.r); g.scale(fl.sc, 1); g.translate(-pcx[k], -pcy[k]);
					L19PhotoArt(t);
				}
				g.restore();
				// the ash where it burnt, and the fire's front (pink), on this piece only
				if (o.bt != null && o.bt.Any(b => t >= b))
				{
					g.save(); L19PolyPath(X, Y); g.clip();
					for (int q = 0; q < o.bt.Length; q++)
					{
						double r = fr(q, t); if (r < 0) continue;
						if (r > o.fw) { g.fillStyle = "rgba(20,18,22,1)"; circle(o.bx[q], o.by[q], r - o.fw); }
					}
					for (int q = 0; q < o.bt.Length; q++)
					{
						double r = fr(q, t); if (r < 0) continue;
						g.strokeStyle = PINK; g.lineWidth = o.fw; g.beginPath(); g.arc(o.bx[q], o.by[q], Math.Max(0.1, r - o.fw / 2), 0, TAU); g.stroke();
						g.strokeStyle = "rgba(255,210,170,0.8)"; g.lineWidth = 3; g.beginPath(); g.arc(o.bx[q], o.by[q], Math.Max(0.1, r - 2), 0, TAU); g.stroke();
					}
					g.restore();
				}
				if (fl.back || fl.sc < 1) { g.strokeStyle = "#fff"; g.lineWidth = 2; L19PolyPath(X, Y); g.stroke(); }
				// the corner lifts before a flip
				foreach (var f in fts[k])
				{
					if (t < f - o.fwn || t >= f) continue; if (fl.back) { double k2 = L19k(t, f - o.fwn, f); g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.6 * k2)})"; g.lineWidth = 2 + 3 * k2; L19PolyPath(X, Y); g.stroke(); continue; }
					double kk = L19k(t, f - o.fwn, f);
					// the corner nearest the top left of the piece
					int ci = 0; for (int i = 1; i < X.Length; i++) if (X[i] + Y[i] < X[ci] + Y[ci]) ci = i;
					double cx = X[ci], cy = Y[ci], mx = X.Average(), my = Y.Average(), dl = hypot(mx - cx, my - cy), ux = (mx - cx) / dl, uy = (my - cy) / dl, l = 30 + 40 * kk;
					g.fillStyle = "rgba(0,0,0,0.55)"; g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + ux * l + uy * l * 0.6, cy + uy * l - ux * l * 0.6); g.lineTo(cx + ux * l - uy * l * 0.6, cy + uy * l + ux * l * 0.6); g.closePath(); g.fill();
					g.fillStyle = "rgba(235,232,240,0.95)"; g.beginPath(); g.moveTo(cx + ux * l * 1.3, cy + uy * l * 1.3); g.lineTo(cx + ux * l + uy * l * 0.6, cy + uy * l - ux * l * 0.6); g.lineTo(cx + ux * l - uy * l * 0.6, cy + uy * l + ux * l * 0.6); g.closePath(); g.fill();
					g.strokeStyle = $"rgba(255,255,255,{str(0.5 + 0.5 * kk)})"; g.lineWidth = 3; L19PolyPath(X, Y); g.stroke();
				}
			}
			// the tears: dashed before, pink while tearing
			for (int q = 0; q < nt; q++)
			{
				double a0 = o.tt[q] - o.twn; if (t < a0 || t >= o.tt[q] + o.td[q] + 0.12) continue; var lx = tlx[q]; var ly = tly[q];
				if (t < o.tt[q])
				{
					double k = L19k(t, a0, o.tt[q]); g.strokeStyle = L19G(0.35 + 0.55 * k); g.lineWidth = 3; g.setLineDash(new double[] { 9, 8 });
					g.beginPath(); for (int i = 0; i < lx.Length; i++) { if (i == 0) g.moveTo(lx[i], ly[i]); else g.lineTo(lx[i], ly[i]); } g.stroke(); g.setLineDash(new double[0]); continue;
				}
				double tk = tearK(q, t) * (lx.Length - 1); g.strokeStyle = PINK; g.lineWidth = 16; g.lineCap = "round"; g.beginPath(); g.moveTo(lx[0], ly[0]);
				for (int i = 1; i < lx.Length; i++) { if (i <= tk) g.lineTo(lx[i], ly[i]); else { double u = tk - (i - 1); if (u > 0) g.lineTo(lx[i - 1] + (lx[i] - lx[i - 1]) * u, ly[i - 1] + (ly[i] - ly[i - 1]) * u); break; } }
				g.stroke(); g.lineCap = "butt";
			}
			// a fire's spark before it starts
			if (o.bt != null) for (int q = 0; q < o.bt.Length; q++)
			{
				if (t < o.bt[q] - o.bwn || t >= o.bt[q]) continue; double k = L19k(t, o.bt[q] - o.bwn, o.bt[q]);
				g.strokeStyle = L19G(0.4 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(o.bx[q], o.by[q], 12 + 50 * (1 - k), 0, TAU); g.stroke();
				g.fillStyle = "rgba(255,220,180,0.9)"; circle(o.bx[q], o.by[q], 4 + 4 * k);
				// which way it will spread
				g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 8; i++) { double aa = i * TAU / 8; g.moveTo(o.bx[q] + JS.cos(aa) * 22, o.by[q] + JS.sin(aa) * 22); g.lineTo(o.bx[q] + JS.cos(aa) * (34 + 20 * k), o.by[q] + JS.sin(aa) * (34 + 20 * k)); } g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < o.t0 || t >= o.te) return false; double m = p.r - 2;
			for (int q = 0; q < nt; q++)
			{
				if (t < o.tt[q] || t >= o.tt[q] + o.td[q] + 0.12) continue; var lx = tlx[q]; var ly = tly[q]; double tk = tearK(q, t) * (lx.Length - 1);
				for (int i = 1; i < lx.Length && i - 1 < tk; i++)
				{
					double u = Math.Min(1, tk - (i - 1)); if (L19Seg(p.x, p.y, lx[i - 1], ly[i - 1], lx[i - 1] + (lx[i] - lx[i - 1]) * u, ly[i - 1] + (ly[i] - ly[i - 1]) * u) < 8 + m) return true;
				}
			}
			bool burn = o.bt != null && o.bt.Any(b => t >= b);
			for (int k = 0; k < np; k++)
			{
				var (X, Y) = world(k, t); var fl = flip(k, t); bool back = fl.back;
				if (!back && !burn) continue;
				if (!L19Touch(p.x, p.y, X, Y, m)) continue;
				if (burn && front(p.x, p.y, t, m) && L19InPoly(p.x, p.y, X, Y)) return true;
				if (back && !ash(p.x, p.y, t)) return true;
			}
			return false;
		};
		return h;
	}
	// the photo: the two of you under a street lamp, he is turning to go (grey tones only)
	static void L19PhotoArt(double t)
	{
		var sky = g.createLinearGradient(0, 0, 0, H); sky.addColorStop(0, "rgba(64,62,74,1)"); sky.addColorStop(1, "rgba(118,112,120,1)"); g.fillStyle = sky; g.fillRect(-60, -60, W + 120, H + 120);
		var lg = g.createRadialGradient(560, 140, 10, 560, 140, 420); lg.addColorStop(0, "rgba(230,226,210,0.55)"); lg.addColorStop(1, "rgba(230,226,210,0)"); g.fillStyle = lg; g.fillRect(-60, -60, W + 120, H + 120);
		g.fillStyle = "rgba(40,38,46,1)"; g.fillRect(-60, 560, W + 120, 220);
		// the lamp
		g.fillStyle = "rgba(30,28,36,1)"; g.fillRect(552, 150, 12, 420); g.fillRect(520, 140, 76, 14);
		g.fillStyle = "rgba(240,236,220,0.9)"; circle(558, 160, 12);
		// you (left) and him (right, walking away)
		void person(double x, double s, double lean)
		{
			g.save(); g.translate(x, 560); g.scale(s, s); g.rotate(lean); g.fillStyle = "rgba(24,22,30,1)";
			circle(0, -300, 34); g.fillRect(-36, -262, 72, 150); g.fillRect(-30, -112, 24, 112); g.fillRect(6, -112, 24, 112);
			g.restore();
		}
		person(440, 1, 0); person(860, 0.92, 0.08);
		// the film's frame
		g.strokeStyle = "rgba(236,230,220,0.9)"; g.lineWidth = 26; g.strokeRect(13, 13, W - 26, H - 26);
	}

	// ---------- the room (harmless): part 1 the paper, 2 the oud's wood, 3 the table ----------
	public static Hz L19Stage(double[] pb)
	{
		var h = new Hz { kind = "L19Stage" };
		h.draw = t =>
		{
			double bt = BT(t); int part = bt < pb[1] ? 1 : bt < pb[2] ? 2 : 3;
			double fade = 0; for (int i = 1; i < pb.Length; i++) fade = Math.Max(fade, 1 - Math.Abs(bt - pb[i]) / 1.0); fade = clamp(fade, 0, 1);
			g.save();
			if (part == 1)
			{
				g.fillStyle = "rgba(30,28,40,1)"; g.fillRect(0, 0, W, H);
				g.strokeStyle = "rgba(120,140,190,0.16)"; g.lineWidth = 2; g.beginPath(); for (double y = 90; y < H; y += 90) { g.moveTo(0, y); g.lineTo(W, y); } g.stroke();
				g.strokeStyle = "rgba(120,140,190,0.22)"; g.beginPath(); g.moveTo(W - 90, 0); g.lineTo(W - 90, H); g.stroke();
			}
			else if (part == 2)
			{
				var wd = g.createLinearGradient(0, 0, 0, H); wd.addColorStop(0, "rgba(34,20,16,1)"); wd.addColorStop(0.5, "rgba(48,28,20,1)"); wd.addColorStop(1, "rgba(30,18,14,1)"); g.fillStyle = wd; g.fillRect(0, 0, W, H);
				g.strokeStyle = "rgba(0,0,0,0.22)"; g.lineWidth = 2; g.beginPath(); for (double y = 20; y < H; y += 26) { g.moveTo(0, y); for (double x = 0; x <= W; x += 80) g.lineTo(x, y + JS.sin(x * 0.01 + y) * 5); } g.stroke();
				// the rose of the oud
				g.strokeStyle = $"rgba(214,180,140,{str(0.09 + 0.06 * pulse)})"; g.lineWidth = 3;
				g.beginPath(); g.arc(W / 2, H / 2, 150, 0, TAU); g.stroke(); g.beginPath(); g.arc(W / 2, H / 2, 120, 0, TAU); g.stroke();
				g.beginPath(); for (int i = 0; i < 12; i++) { double a = i * TAU / 12 + bt * 0.02; g.moveTo(W / 2, H / 2); g.lineTo(W / 2 + JS.cos(a) * 120, H / 2 + JS.sin(a) * 120); } g.stroke();
			}
			else
			{
				g.fillStyle = "rgba(22,18,20,1)"; g.fillRect(0, 0, W, H);
				var lt = g.createRadialGradient(W / 2, H / 2 - 60, 40, W / 2, H / 2, 760); lt.addColorStop(0, "rgba(120,96,70,0.28)"); lt.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = lt; g.fillRect(0, 0, W, H);
				g.strokeStyle = "rgba(0,0,0,0.35)"; g.lineWidth = 2; g.beginPath(); for (double x = 0; x < W; x += 160) { g.moveTo(x, 0); g.lineTo(x + 20, H); } g.stroke();
			}
			if (fade > 0) { g.fillStyle = $"rgba(0,0,0,{str(fade * 0.8)})"; g.fillRect(0, 0, W, H); }
			var gr = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L19.json) ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here. An event fires at its start minus Easy's longest warning
	static double L19wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L19ez(double v, double ez) => easy ? v * ez : v;
	static double[] L19Sec(double[] bs) => bs.Select(B).ToArray();
	static int[] L19I(double[] v) => v.Select(x => (int)x).ToArray();

	static readonly int _k19fx = DodgeKind("L19fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L19fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k19stage = DodgeKind("L19stage", e => () => hazards.Add(L19Stage(e.nums("pb"))));
	static readonly int _k19drop = DodgeKind("L19drop", e => { double wn = L19wn(e); return () =>
	{
		double R = L19ez(e.num("R"), 0.8), hold = L19ez(e.num("hold"), 0.8) * SPB;
		hazards.Add(L19Drop(e.num("x"), e.num("y"), R, e.num("ang"), B(e.num("hit") - wn), B(e.num("hit")), hold));
		L19Under(L19Stain(e.num("x"), e.num("y"), R, e.num("ang"), B(e.num("hit")), hold));
	}; });
	static readonly int _k19pen = DodgeKind("L19pen", e => { double wn = L19wn(e); return () =>
	{
		double hit = e.num("hit");
		hazards.Add(L19Pen(e.num("y0"), e.num("rh"), e.nums("ord"), e.nums("gx"), e.i("ng"), L19ez(e.num("gw"), 1.4), !e.flag("ltr"), B(hit - wn), B(hit),
			e.num("dr") * SPB, L19ez(e.num("wr"), 1.15) * SPB, L19ez(e.num("wet"), 0.75) * SPB, B(e.num("to"))));
	}; });
	static readonly int _k19fold = DodgeKind("L19fold", e => { double wn = L19wn(e); return () =>
		hazards.Add(L19Fold(e.num("fx"), e.num("fy"), e.num("na") * Math.PI / 180, B(e.num("hit") - wn), B(e.num("hit")), L19ez(e.num("fl"), 1.2) * SPB, L19ez(e.num("hold"), 0.8) * SPB)); });
	static readonly int _k19strings = DodgeKind("L19strings", e => { double wn = L19wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.7 : 1;
		var o = new L19StrO
		{
			cx = e.num("cx", W / 2), cy = e.num("cy", H / 2), lh = e.num("lh", 700), A = L19ez(e.num("A", 90), 0.78), th = e.num("th", 6), md = e.num("md", 0.25) * SPB,
			pwn = L19ez(e.num("pwn", 1.5), 1.3) * SPB, te = B(e.num("to")), ys = e.nums("ys"), on = L19Sec(e.nums("on")), off = L19Sec(e.nums("off")),
		};
		if (e.has("sb0")) o.spin = new L19Spin(e.num("a0") * Math.PI / 180, hit - wn - 1, e.num("to") + 1, e.nums("sb0"), e.nums("sw0").Select(v => v * sl).ToArray());
		if (e.has("hs")) { o.hs = L19Sec(e.nums("hs")); o.hn = L19I(e.nums("hn")); }
		if (e.has("pb"))
		{
			o.pb = L19Sec(e.nums("pb")); o.pi = L19I(e.nums("pi")); o.pxs = e.nums("pxs"); int n = o.pb.Length;
			o.pv = Enumerable.Repeat(L19ez(e.num("pv"), 0.8), n).ToArray(); o.pw = Enumerable.Repeat(e.num("pw"), n).ToArray(); o.pa = Enumerable.Repeat(L19ez(e.num("pa"), 0.85), n).ToArray();
			o.pl = Enumerable.Repeat(e.num("pl") * SPB, n).ToArray(); o.pfw = L19ez(e.num("pfw", 1), 1.3) * SPB;
		}
		if (e.has("eb")) { o.eb = L19Sec(e.nums("eb")); o.ei = L19I(e.nums("ei")); o.en = L19I(e.nums("en")); o.ed = e.nums("ed").Select(v => L19ez(v, 0.8) * SPB).ToArray(); }
		if (e.has("kt")) { o.kt = L19Sec(e.nums("kt")); o.kx = e.nums("kx"); o.ky = e.nums("ky"); o.kr = L19ez(e.num("kr", 30), 0.85); o.kwn = L19ez(e.num("kwn", 2), 1.3) * SPB; }
		if (e.has("snb"))
		{
			o.sb = L19Sec(e.nums("snb")); o.si = L19I(e.nums("sni")); o.sx = e.nums("snx"); o.sa = e.nums("sna").Select(v => v * Math.PI / 180).ToArray(); int n = o.sb.Length;
			o.sp = e.num("snp", o.lh); o.sd = Enumerable.Repeat(L19ez(e.num("snd", 0.6), 1.25) * SPB, n).ToArray(); o.sh = Enumerable.Repeat(L19ez(e.num("snh", 0.5), 0.8) * SPB, n).ToArray(); o.swn = L19ez(e.num("snw", 1.5), 1.3) * SPB;
		}
		hazards.Add(L19Strings(o, B(hit - wn)));
	}; });
	static readonly int _k19photo = DodgeKind("L19photo", e => { double wn = L19wn(e); return () =>
	{
		var o = new L19PhO
		{
			vx = e.nums("vx"), vy = e.nums("vy"), vc = L19I(e.nums("vc")), tt = L19Sec(e.nums("tt")), td = e.nums("td").Select(v => v * SPB).ToArray(), tnx = e.nums("tnx"), tny = e.nums("tny"),
			dd = e.nums("dd").Select(v => L19ez(v, 1.3)).ToArray(), tlx = e.nums("tlx"), tly = e.nums("tly"), tlc = L19I(e.nums("tlc")), psd = L19I(e.nums("psd")), rot = e.nums("rot"),
			dr = e.num("dr", 2) * SPB, fc = L19I(e.nums("fc")), ft = L19Sec(e.nums("ft")), fd = e.num("fd", 0.3) * SPB, fwn = L19ez(e.num("fwn", 1.5), 1.3) * SPB,
			fw = e.num("fw", 30), bwn = L19ez(e.num("bwn", 1.5), 1.3) * SPB, twn = L19ez(e.num("twn", 2), 1.3) * SPB, t0 = B(e.num("hit")), te = B(e.num("to")),
		};
		if (e.has("gk")) { o.gk = L19Sec(e.nums("gk")); o.gv = e.nums("gv"); o.gd = e.num("gd", 1) * SPB; }
		if (e.has("bt")) { o.bt = L19Sec(e.nums("bt")); o.bx = e.nums("bx"); o.by = e.nums("by"); o.bv = e.nums("bv").Select(v => L19ez(v, 0.8)).ToArray(); }
		// Easy: the flips marked "ez" off are skipped (each piece keeps its flips in pairs, so it still ends on the same side)
		if (easy && e.has("fez"))
		{
			var fez = e.nums("fez"); var nfc = new List<int>(); var nft = new List<double>(); int s = 0;
			for (int k = 0; k < o.fc.Length; k++) { int c = 0; for (int q = 0; q < o.fc[k]; q++) if (fez[s + q] != 0) { nft.Add(o.ft[s + q]); c++; } nfc.Add(c); s += o.fc[k]; }
			o.fc = nfc.ToArray(); o.ft = nft.ToArray();
		}
		hazards.Add(L19Photo(o, B(e.num("hit") - wn)));
	}; });
}
