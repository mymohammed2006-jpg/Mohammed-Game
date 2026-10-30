using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Trouble, main level 29, part 2: "he flew me to places I'd never been ... till you put me down"
public static partial class G
{
	// ---------- 4. the paper planes (0:46.9) ----------
	// A way made of cubic curves (c = x0,y0, c1x,c1y, c2x,c2y, x1,y1, then 3 more numbers a curve), walked at an even speed in dur beats from b.
	public class TBWay { public double[] x, y, l; public double len; }
	public static TBWay TBMakeWay(double[] c, double ox = 0, double oy = 0)
	{
		var xs = new List<double>(); var ys = new List<double>(); int segs = (c.Length - 2) / 6;
		for (int s = 0; s < segs; s++)
		{
			double x0 = c[6 * s], y0 = c[6 * s + 1], x1 = c[6 * s + 2], y1 = c[6 * s + 3], x2 = c[6 * s + 4], y2 = c[6 * s + 5], x3 = c[6 * s + 6], y3 = c[6 * s + 7];
			for (int i = s == 0 ? 0 : 1; i <= 40; i++)
			{
				double u = i / 40.0, v = 1 - u;
				xs.Add(v * v * v * x0 + 3 * v * v * u * x1 + 3 * v * u * u * x2 + u * u * u * x3 + ox); ys.Add(v * v * v * y0 + 3 * v * v * u * y1 + 3 * v * u * u * y2 + u * u * u * y3 + oy);
			}
		}
		var w = new TBWay { x = xs.ToArray(), y = ys.ToArray(), l = new double[xs.Count] };
		for (int i = 1; i < xs.Count; i++) w.l[i] = w.l[i - 1] + hypot(xs[i] - xs[i - 1], ys[i] - ys[i - 1]);
		w.len = w.l[xs.Count - 1]; return w;
	}
	static (double x, double y, double a) TBWayAt(TBWay w, double f)
	{
		double d = clamp(f, 0, 1) * w.len; int i = 1; while (i < w.l.Length - 1 && w.l[i] < d) i++;
		double u = (d - w.l[i - 1]) / Math.Max(1e-9, w.l[i] - w.l[i - 1]);
		return (w.x[i - 1] + (w.x[i] - w.x[i - 1]) * u, w.y[i - 1] + (w.y[i] - w.y[i - 1]) * u, Math.Atan2(w.y[i] - w.y[i - 1], w.x[i] - w.x[i - 1]));
	}
	// walks the way from fraction f0 to f1 in pieces (x0,y0,x1,y1)
	static void TBWayWalk(TBWay w, double f0, double f1, Action<double, double, double, double> fn)
	{
		f0 = clamp(f0, 0, 1); f1 = clamp(f1, 0, 1); if (f1 <= f0) return;
		var (px, py, _) = TBWayAt(w, f0); double d0 = f0 * w.len, d1 = f1 * w.len;
		for (int i = 1; i < w.l.Length; i++)
		{
			if (w.l[i] <= d0) continue;
			if (w.l[i] >= d1) { var (ex, ey, _) = TBWayAt(w, f1); fn(px, py, ex, ey); break; }
			fn(px, py, w.x[i], w.y[i]); px = w.x[i]; py = w.y[i];
		}
	}
	static void TBPlanePath(double x, double y, double a, double s)
	{
		double c = JS.cos(a), sn = JS.sin(a);
		void P(double u, double v, bool m) { double px = x + c * u - sn * v, py = y + sn * u + c * v; if (m) g.moveTo(px, py); else g.lineTo(px, py); }
		g.beginPath(); P(s, 0, true); P(-0.7 * s, 0.62 * s, false); P(-0.45 * s, 0, false); P(-0.7 * s, -0.62 * s, false); g.closePath();
	}
	// A plane (size s) flies way w from b for dur beats; its last trail beats leave a pink trail (width 6). The way shows dashed from b - tl.
	public static Hz TBPlane(TBWay w, double b, double dur, double tl, double s, double trail)
	{
		var h = new Hz { kind = "TBPlane" };
		double f(double bt) => (bt - b) / dur;
		h.update = (t, dt) => { if (BT(t) > b + dur + trail) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; double fn = f(bt);
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (fn < 1)
			{
				double q = TBk(bt, b - tl, b); g.strokeStyle = TBW(0.2 + 0.45 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 10 }); g.beginPath();
				bool first = true; TBWayWalk(w, Math.Max(0, fn), 1, (x0, y0, x1, y1) => { if (first) { g.moveTo(x0, y0); first = false; } g.lineTo(x1, y1); }); g.stroke(); g.setLineDash(new double[0]);
			}
			if (bt >= b)
			{
				double ft = f(bt - trail); g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = 6;
				g.beginPath(); bool first = true; TBWayWalk(w, ft, Math.Min(1, fn), (x0, y0, x1, y1) => { if (first) { g.moveTo(x0, y0); first = false; } g.lineTo(x1, y1); }); g.stroke();
				if (fn <= 1) { var (x, y, a) = TBWayAt(w, fn); g.fillStyle = PINK; TBPlanePath(x, y, a, s); g.fill(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false; double fn = f(bt);
			if (fn <= 1) { var (x, y, _) = TBWayAt(w, fn); if (hypot(p.x - x, p.y - y) < 0.55 * s + p.r - 2) return true; }
			bool hit = false; TBWayWalk(w, f(bt - trail), Math.Min(1, fn), (x0, y0, x1, y1) => { if (!hit && TBSeg(p.x, p.y, x0, y0, x1, y1) < 3 + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 5. the pins on the map (0:59.4) ----------
	// From beat b a pink route runs from (x0,y0) to (x1,y1) in a curve (bulge px to its side) in 0.5 beat, the pin lands at its end at
	// b + 0.5 with a ring (to ringR in 0.45 beat). The route is hot for 0.75 beat after it arrives, the pin's head for pinHold beats, then
	// both cool to white. The route shows dashed and the landing spot as a white circle from b - tl.
	public static Hz TBPin(double x0, double y0, double x1, double y1, double bulge, double b, double tl, double pinHold, double ringR)
	{
		var h = new Hz { kind = "TBPin" };
		double mx = (x0 + x1) / 2, my = (y0 + y1) / 2, dx = x1 - x0, dy = y1 - y0, l = Math.Max(1, hypot(dx, dy)), cx = mx - dy / l * bulge, cy = my + dx / l * bulge;
		var w = TBMakeWay(new[] { x0, y0, x0 + (cx - x0) * 2 / 3, y0 + (cy - y0) * 2 / 3, x1 + (cx - x1) * 2 / 3, y1 + (cy - y1) * 2 / 3, x1, y1 });
		double bl = b + 0.5;
		h.update = (t, dt) => { if (BT(t) > bl + Math.Max(pinHold, 0.75) + 6) h.dead = true; };
		void pin(double hx, double hy, string fill)
		{
			g.fillStyle = fill; g.beginPath(); g.arc(hx, hy - 34, 24, Math.PI * 0.8, Math.PI * 2.2); g.lineTo(hx, hy); g.closePath(); g.fill();
			g.fillStyle = "rgba(20,10,24,0.9)"; circle(hx, hy - 34, 9);
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < bl)
			{
				double q = TBk(bt, b - tl, b);
				g.strokeStyle = TBW(0.25 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 10 }); g.beginPath(); g.moveTo(w.x[0], w.y[0]); for (int i = 1; i < w.x.Length; i++) g.lineTo(w.x[i], w.y[i]); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = TBW(0.4 + 0.5 * q); g.lineWidth = 3; g.beginPath(); g.arc(x1, y1, 26 + 30 * (1 - q), 0, TAU); g.stroke();
			}
			double fr = TBk(bt, b, bl);
			if (bt >= b)
			{
				bool hot = bt < bl + 0.75;
				if (hot) { g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = 7; }
				else { g.shadowBlur = 0; g.strokeStyle = TBW(0.35 * (1 - TBk(bt, bl + 3, bl + 6))); g.lineWidth = 2; g.setLineDash(new double[] { 6, 8 }); }
				g.beginPath(); bool first = true; TBWayWalk(w, 0, fr, (xa, ya, xb, yb) => { if (first) { g.moveTo(xa, ya); first = false; } g.lineTo(xb, yb); }); g.stroke(); g.setLineDash(new double[0]);
			}
			if (bt >= bl)
			{
				if (bt < bl + 0.45) { double r = 30 + (ringR - 30) * TBOut((bt - bl) / 0.45); g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = 7; g.beginPath(); g.arc(x1, y1, r, 0, TAU); g.stroke(); }
				if (bt < bl + pinHold) { g.shadowColor = PINK; g.shadowBlur = 14; pin(x1, y1 + 34, PINK); }
				else { g.shadowBlur = 0; pin(x1, y1 + 34, TBW(0.3 * (1 - TBk(bt, bl + pinHold + 2, bl + pinHold + 5)))); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false;
			if (bt < bl + 0.75) { bool hit = false; TBWayWalk(w, 0, TBk(bt, b, bl), (xa, ya, xb, yb) => { if (!hit && TBSeg(p.x, p.y, xa, ya, xb, yb) < 3.5 + p.r - 2) hit = true; }); if (hit) return true; }
			if (bt >= bl)
			{
				if (bt < bl + 0.45) { double r = 30 + (ringR - 30) * TBOut((bt - bl) / 0.45); if (Math.Abs(hypot(p.x - x1, p.y - y1) - r) < 3.5 + p.r - 2) return true; }
				// the pin: its round head (centre 34 px up from the point, r 24) and its point
				if (bt < bl + pinHold && (hypot(p.x - x1, p.y - y1) < 24 + p.r - 2 || TBTri(p.x, p.y, x1 - 19.4, y1 + 14.1, x1 + 19.4, y1 + 14.1, x1, y1 + 34) < p.r - 2)) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 6. the globe turns (1:10.3): a curved line of longitude crosses the screen ----------
	// It moves the way dir (1 = to the right) at sp px/s from beat b, bowed bulge px at the middle (towards where it goes), width 14,
	// with a gap (centre gy, height gh). Before b: a dashed line at its edge and the gap marked white.
	public static Hz TBMeridian(int dir, double b, double tl, double sp, double bulge, double gy, double gh)
	{
		var h = new Hz { kind = "TBMeridian" }; double t0 = B(b); const int N = 24; const double LW = 14;
		double x0(double t) => dir > 0 ? -bulge - 20 + (t - t0) * sp : W + bulge + 20 - (t - t0) * sp;
		double xAt(double t, double y) { double u = (y - H / 2) / (H / 2); return x0(t) + dir * bulge * (1 - u * u); }
		h.update = (t, dt) => { if (t > t0 && (dir > 0 ? x0(t) > W + 40 : x0(t) < -40)) h.dead = true; };
		void segs(double t, Action<double, double, double, double> f)
		{
			for (int i = 0; i < N; i++)
			{
				double ya = H * i / N, yb = H * (i + 1) / N;
				// cut at the gap's edges
				double g0 = gy - gh / 2, g1 = gy + gh / 2;
				if (yb <= g0 || ya >= g1) f(xAt(t, ya), ya, xAt(t, yb), yb);
				else { if (ya < g0) f(xAt(t, ya), ya, xAt(t, g0), g0); if (yb > g1) f(xAt(t, g1), g1, xAt(t, yb), yb); }
			}
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save(); g.lineCap = "butt";
			if (t < t0)
			{
				double q = TBk(bt, b - tl, b), ex = dir > 0 ? 6 : W - 6;
				TBDash(ex, 0, ex, gy - gh / 2, TBW(0.3 + 0.6 * q), 3); TBDash(ex, gy + gh / 2, ex, H, TBW(0.3 + 0.6 * q), 3);
				g.strokeStyle = TBW(0.5 + 0.5 * q); g.lineWidth = 3; double ix = dir > 0 ? 0 : W, sx = dir > 0 ? 26 : -26;
				g.beginPath(); g.moveTo(ix + sx, gy - gh / 2); g.lineTo(ix, gy - gh / 2); g.lineTo(ix, gy + gh / 2); g.lineTo(ix + sx, gy + gh / 2); g.stroke();
			}
			else
			{
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = LW;
				g.beginPath(); segs(t, (xa, ya, xb, yb) => { g.moveTo(xa, ya); g.lineTo(xb, yb); }); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; bool hit = false;
			segs(t, (xa, ya, xb, yb) => { if (!hit && TBSeg(p.x, p.y, xa, ya, xb, yb) < LW / 2 + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 7. put me down (1:24.3): a heavy heart falls on (x,y) on beat b ----------
	// Its shadow (white) grows from b - tl, the heart falls white and turns pink as it lands (radius R, pink for 0.5 beat), it breaks in n
	// pieces that fly out at sp px/s (turned a0 degrees). After it lands it is a grey cracked print.
	// the falling heart's shape: two round lobes and a point (the hit uses exactly these)
	static void TBHeartSolid(double x, double y, double R)
	{
		circle(x - 0.45 * R, y - 0.3 * R, 0.5 * R); circle(x + 0.45 * R, y - 0.3 * R, 0.5 * R);
		g.beginPath(); g.moveTo(x - 0.88 * R, y - 0.1 * R); g.lineTo(x + 0.88 * R, y - 0.1 * R); g.lineTo(x, y + 0.82 * R); g.closePath(); g.fill();
	}
	static void TBHeartOutline(double x, double y, double R)
	{
		g.beginPath(); g.moveTo(x, y + 0.82 * R); g.lineTo(x - 0.88 * R, y - 0.1 * R); g.arc(x - 0.45 * R, y - 0.3 * R, 0.5 * R, Math.PI * 1.13, Math.PI * 1.97);
		g.arc(x + 0.45 * R, y - 0.3 * R, 0.5 * R, Math.PI * 1.03, Math.PI * 1.87); g.lineTo(x + 0.88 * R, y - 0.1 * R); g.closePath();
	}
	public static Hz TBDrop(double x, double y, double b, double tl, double R, int n, double a0, double sp)
	{
		var h = new Hz { kind = "TBDrop" }; double t0 = B(b);
		h.update = (t, dt) =>
		{
			if (t < t0 + 0.5) return; double r = 0.6 * R + (t - t0) * sp; bool on = false;
			for (int k = 0; k < n; k++) { double q = TBRad(a0) + k * TAU / n, px = x + JS.cos(q) * r, py = y + JS.sin(q) * r; if (px > -40 && px < W + 40 && py > -40 && py < H + 40) on = true; }
			if (!on && BT(t) > b + 3) h.dead = true;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b);
				g.fillStyle = TBW(0.05 + 0.12 * q); g.beginPath(); g.ellipse(x, y + 0.2 * R, R * (0.4 + 0.6 * q), R * (0.4 + 0.6 * q) * 0.9, 0, 0, TAU); g.fill();
				g.strokeStyle = TBW(0.35 + 0.55 * q); g.lineWidth = 2.5; g.setLineDash(new double[] { 9, 7 }); TBHeartOutline(x, y, R); g.stroke(); g.setLineDash(new double[0]);
				// the falling heart (white, high up, harmless until it lands)
				double fq = TBk(bt, b - 0.6, b); if (fq > 0) { g.fillStyle = TBW(0.75); TBHeartSolid(x, y - 520 * (1 - fq * fq), R * (0.6 + 0.4 * fq)); }
			}
			else
			{
				if (bt < b + 0.5) { g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; TBHeartSolid(x, y, R); g.shadowBlur = 0; }
				else { g.fillStyle = $"rgba(150,146,170,{str(0.3 * (1 - TBk(bt, b + 0.5, b + 3)))})"; TBHeartSolid(x, y, R); g.strokeStyle = "rgba(20,14,26,0.8)"; g.lineWidth = 3; g.beginPath(); g.moveTo(x - 0.1 * R, y - 0.42 * R); g.lineTo(x + 0.12 * R, y); g.lineTo(x - 0.1 * R, y + 0.4 * R); g.stroke(); }
				double r = 0.6 * R + (t - t0) * sp; g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				for (int k = 0; k < n; k++) { double q = TBRad(a0) + k * TAU / n; double px = x + JS.cos(q) * r, py = y + JS.sin(q) * r; if (px > -40 && px < W + 40 && py > -40 && py < H + 40) SWShard(px, py, 24, q); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false;
			if (bt < b + 0.5)
			{
				// the heart: two round lobes and the point (the same curves, kept as circles + a triangle for the hit)
				if (hypot(p.x - (x - 0.45 * R), p.y - (y - 0.3 * R)) < 0.5 * R + p.r - 2 || hypot(p.x - (x + 0.45 * R), p.y - (y - 0.3 * R)) < 0.5 * R + p.r - 2) return true;
				if (TBTri(p.x, p.y, x - 0.88 * R, y - 0.1 * R, x + 0.88 * R, y - 0.1 * R, x, y + 0.82 * R) < p.r - 2) return true;
			}
			double r = 0.6 * R + (t - t0) * sp;
			for (int k = 0; k < n; k++) { double q = TBRad(a0) + k * TAU / n; if (SWShardHit(p, x + JS.cos(q) * r, y + JS.sin(q) * r, 24)) return true; }
			return false;
		};
		return h;
	}
}
