using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// راضي بحبك (عبدالله آل فروان), main level 23: "the love letter". The whole song (songs/SONG_L23.mp3 from 0:00). Grid: 96 BPM, beat 0 = 0.065 s.
// Part 1 the letter: the pen writes (right to left, like Arabic), ink drops, the lines of the letter, the envelope folds and is sealed.
// Part 2 the heart ("راضي بحبك", the chorus): the heart beats out waves with one open way, arrows of love, the rose dances (the music break).
// Part 3 contentment: the clock ticks (waiting), the hearts gather, the heart, the arrows and the pen together, the envelope closes.
// Every hazard is a pure function of time once it exists. Only pink hurts: the ink that dried (gold), the paper (white), the seal (gold) never do.
public static partial class G
{
	static double RZk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string RZP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string RZW(double a) => $"rgba(240,236,228,{str(clamp(a, 0, 1))})";
	static string RZG(double a) => $"rgba(236,190,90,{str(clamp(a, 0, 1))})";
	static double RZEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double RZOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double RZRad(double d) => d * Math.PI / 180;
	static double RZPulse(double bt) { double f = bt - Math.Floor(bt); return Math.Exp(-f / 0.16); }   // 1 on the beat, then fades
	static void RZDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double RZSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// the heart curve, half its width = 1, its middle at (0,0): point i of RZN, i = 0 the dip at the top, going right (clockwise)
	const int RZN = 96;
	static double[] _rzhx, _rzhy;
	static double[] RZHX { get { RZHeartInit(); return _rzhx; } }
	static double[] RZHY { get { RZHeartInit(); return _rzhy; } }
	static void RZHeartInit()
	{
		if (_rzhx != null) return;
		var hx = new double[RZN + 1]; var hy = new double[RZN + 1];
		for (int i = 0; i <= RZN; i++)
		{
			double u = TAU * i / RZN, s = JS.sin(u);
			hx[i] = s * s * s; hy[i] = -(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)) / 16 - 0.15;
		}
		_rzhy = hy; _rzhx = hx;
	}
	static void RZHeartPathAt(double cx, double cy, double s, double rot = 0)
	{
		var X = RZHX; var Y = RZHY; double c = JS.cos(rot), sn = JS.sin(rot);
		g.beginPath(); for (int i = 0; i < RZN; i++) { double x = cx + (X[i] * c - Y[i] * sn) * s, y = cy + (X[i] * sn + Y[i] * c) * s; if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.closePath();
	}
	// is (px,py) inside the heart (middle cx,cy, size s), or within m px of its edge
	static bool RZInHeart(double px, double py, double cx, double cy, double s, double m)
	{
		var X = RZHX; var Y = RZHY; bool inside = false; double best = 1e9;
		for (int i = 0, j = RZN - 1; i < RZN; j = i++)
		{
			double xi = cx + X[i] * s, yi = cy + Y[i] * s, xj = cx + X[j] * s, yj = cy + Y[j] * s;
			if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
			best = Math.Min(best, RZSeg(px, py, xi, yi, xj, yj));
		}
		return inside || best < m;
	}
	// a small heart (pink when it hurts), size s = half its width
	// (its hit: a circle of 0.55 s round (0, 0.08 s), turned with it, all inside the drawn heart)
	static void RZSmallHeart(double x, double y, double s, double rot) => RZHeartPathAt(x, y, s, rot);
	static bool RZSmallHit(Player p, double x, double y, double s, double rot) => hypot(p.x - (x - JS.sin(rot) * 0.08 * s), p.y - (y + JS.cos(rot) * 0.08 * s)) < 0.55 * s + p.r - 2;
	public static Hz RZFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "RZFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the stage (harmless, under everything) ----------
	public static Hz RZStage(double bEnd)
	{
		var h = new Hz { kind = "RZStage", z = -1 };
		h.update = (t, dt) => { if (BT(t) > bEnd + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p2 = RZk(bt, 82, 86) * (1 - RZk(bt, 165, 169)), p3 = RZk(bt, 165, 169), p1 = 1 - p2 - p3, pu = RZPulse(bt);
			g.save(); g.globalAlpha = a;
			// night ink blue (1), deep plum (2), a warm dawn (3)
			double r0 = 12 * p1 + 30 * p2 + 36 * p3, g0 = 16 * p1 + 10 * p2 + 22 * p3, b0 = 36 * p1 + 40 * p2 + 20 * p3;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(Math.Round(r0))},{str(Math.Round(g0))},{str(Math.Round(b0))},1)"); gr.addColorStop(1, $"rgba({str(Math.Round(r0 * 0.3))},{str(Math.Round(g0 * 0.3))},{str(Math.Round(b0 * 0.35))},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// part 1: the ruled lines of the letter paper, the margin line
			if (p1 > 0.01)
			{
				g.strokeStyle = RZW(0.045 * p1); g.lineWidth = 1; g.beginPath();
				for (int i = 1; i < 12; i++) { double yy = i * 60 + 0.5; g.moveTo(0, yy); g.lineTo(W, yy); }
				g.stroke(); g.strokeStyle = RZG(0.08 * p1); g.beginPath(); g.moveTo(W - 90.5, 0); g.lineTo(W - 90.5, H); g.stroke();
			}
			// part 2: soft heart glow that beats
			if (p2 > 0.01)
			{
				double s = 330 + 18 * pu; var cg = g.createRadialGradient(W / 2, H / 2, 20, W / 2, H / 2, s * 1.6);
				cg.addColorStop(0, $"rgba(150,90,170,{str(0.13 * p2)})"); cg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = cg; g.fillRect(0, 0, W, H);
				g.strokeStyle = RZG(0.05 * p2); g.lineWidth = 2; RZHeartPathAt(W / 2, H / 2 + 10, s); g.stroke();
			}
			// part 3: stars and the dawn rising
			if (p3 > 0.01)
			{
				g.fillStyle = RZW(0.5 * p3); for (int i = 0; i < 40; i++) { double sx = (i * 397.3) % W, sy = (i * 151.7) % (H * 0.7), tw = 0.5 + 0.5 * JS.sin(t * (1 + i % 3) + i); g.globalAlpha = a * p3 * (0.25 + 0.5 * tw); circle(sx, sy, i % 6 == 0 ? 1.8 : 1.1); }
				g.globalAlpha = a; double up = RZk(bt, 168, 261);
				var dg = g.createRadialGradient(W / 2, H + 120, 40, W / 2, H + 120, 520 + 260 * up); dg.addColorStop(0, $"rgba(236,170,80,{str(0.22 * p3)})"); dg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = dg; g.fillRect(0, 0, W, H);
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.5)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the pen (0:02.6, again 2:25) ----------
	// The pen writes a line (n pieces) from its first point to its last at an even speed, from bg for dur beats. Its way shows dashed
	// from bg - tl. The fresh ink is pink (hurts) for hot beats after it is written, then it dries to gold (harmless) and fades.
	public class RZLine { public double[] x, y; public double bg, dur, hot, tl; }
	public static RZLine RZWave(double x0, double y0, double x1, double y1, double amp, double cyc, double ph, double bg, double dur, double hot, double tl, int n = 48)
	{
		var L = new RZLine { x = new double[n + 1], y = new double[n + 1], bg = bg, dur = dur, hot = hot, tl = tl };
		double dx = x1 - x0, dy = y1 - y0, l = hypot(dx, dy), nx = -dy / l, ny = dx / l;
		for (int i = 0; i <= n; i++) { double s = (double)i / n, o = amp * JS.sin(TAU * cyc * s + ph); L.x[i] = x0 + dx * s + nx * o; L.y[i] = y0 + dy * s + ny * o; }
		return L;
	}
	public static RZLine RZHeartLine(double cx, double cy, double sc, double bg, double dur, double hot, double tl)
	{
		var L = new RZLine { x = new double[RZN + 1], y = new double[RZN + 1], bg = bg, dur = dur, hot = hot, tl = tl };
		for (int i = 0; i <= RZN; i++) { L.x[i] = cx + RZHX[i] * sc; L.y[i] = cy + RZHY[i] * sc; }
		return L;
	}
	// two parts: what does not hurt (the dashed way, the nib, the dried ink) lies under all the attacks (z -1), the fresh ink is an attack
	public static Hz[] RZPen(RZLine L)
	{
		var h = new Hz { kind = "RZPen" }; var u0 = new Hz { kind = "RZPenUnder", z = -1 }; int n = L.x.Length - 1; double end = L.bg + L.dur + L.hot + 3;
		h.update = (t, dt) => { if (BT(t) > end) h.dead = true; };
		u0.update = (t, dt) => { if (BT(t) > end) u0.dead = true; };
		// the pen's tip at beat bt: piece i (from point i-1 to i) is written at bg + dur * i / n
		double tw(int i) => L.bg + L.dur * i / n;
		u0.draw = t =>
		{
			double bt = BT(t); if (bt < L.bg - L.tl) return; double f = clamp((bt - L.bg) / L.dur, 0, 1) * n;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (f < n)
			{
				g.strokeStyle = RZW(0.3 + 0.4 * RZk(bt, L.bg - L.tl, L.bg)); g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 });
				g.beginPath(); g.moveTo(L.x[0], L.y[0]); for (int i = 1; i <= n; i++) g.lineTo(L.x[i], L.y[i]); g.stroke(); g.setLineDash(new double[0]);
			}
			// the pen (a gold nib) while it writes
			if (bt >= L.bg && bt <= L.bg + L.dur)
			{
				int i = Math.Min(n, (int)Math.Floor(f) + 1); double u = f - (i - 1), px = L.x[i - 1] + (L.x[i] - L.x[i - 1]) * u, py = L.y[i - 1] + (L.y[i] - L.y[i - 1]) * u;
				g.save(); g.translate(px, py); g.rotate(-0.6);
				g.fillStyle = RZG(1); g.beginPath(); g.moveTo(0, -8); g.lineTo(10, -60); g.lineTo(26, -54); g.closePath(); g.fill();
				g.strokeStyle = RZW(0.9); g.lineWidth = 2; g.beginPath(); g.moveTo(14, -48); g.quadraticCurveTo(40, -110, 70, -130); g.stroke(); g.restore();
			}
			// the dried ink (gold)
			double fade = 1 - RZk(bt, L.bg + L.dur + L.hot, end);
			g.strokeStyle = RZG(0.55 * fade); g.lineWidth = 3;
			for (int i = 1; i <= n && i - 1 < f; i++) if (bt >= tw(i) + L.hot) { g.beginPath(); g.moveTo(L.x[i - 1], L.y[i - 1]); g.lineTo(L.x[i], L.y[i]); g.stroke(); }
			g.restore();
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < L.bg) return; double f = clamp((bt - L.bg) / L.dur, 0, 1) * n;
			// the fresh ink (pink)
			g.save(); g.lineCap = "round"; g.lineJoin = "round"; g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 7;
			for (int i = 1; i <= n && i - 1 < f; i++)
			{
				if (bt >= tw(i) + L.hot) continue; double u = Math.Min(1, f - (i - 1));
				g.beginPath(); g.moveTo(L.x[i - 1], L.y[i - 1]); g.lineTo(L.x[i - 1] + (L.x[i] - L.x[i - 1]) * u, L.y[i - 1] + (L.y[i] - L.y[i - 1]) * u); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < L.bg) return false; double f = clamp((bt - L.bg) / L.dur, 0, 1) * n;
			for (int i = 1; i <= n && i - 1 < f; i++)
			{
				if (bt >= tw(i) + L.hot) continue; double u = Math.Min(1, f - (i - 1));
				if (RZSeg(p.x, p.y, L.x[i - 1], L.y[i - 1], L.x[i - 1] + (L.x[i] - L.x[i - 1]) * u, L.y[i - 1] + (L.y[i] - L.y[i - 1]) * u) < 3.5 + p.r - 2) return true;
			}
			return false;
		};
		return new[] { u0, h };
	}

	// ---------- the ink drops (0:12.6) ----------
	// A drop falls (white, harmless) for tl beats onto (x,y); at b an ink ring (pink) spreads from it to R px in grow beats and fades.
	public static Hz RZDrop(double x, double y, double b, double tl, double grow, double R)
	{
		var h = new Hz { kind = "RZDrop" };
		h.update = (t, dt) => { if (BT(t) > b + grow) h.dead = true; };
		double rad(double bt) => 8 + (R - 8) * RZOut((bt - b) / grow);
		double al(double bt) => 1 - RZk(bt, b + 0.7 * grow, b + grow);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			if (bt < b)
			{
				double k = RZk(bt, b - tl, b);
				g.strokeStyle = RZW(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(x, y, 10 + 34 * (1 - k), 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				double dy = y - 260 * (1 - k * k); g.fillStyle = RZW(0.85); g.beginPath(); g.moveTo(x, dy - 14); g.quadraticCurveTo(x + 8, dy, x, dy + 6); g.quadraticCurveTo(x - 8, dy, x, dy - 14); g.fill();
			}
			else
			{
				double r = rad(bt), a = al(bt);
				g.fillStyle = RZG(0.5 * a); circle(x, y, 5);
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = RZP(a); g.lineWidth = 9; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b || al(bt) < 0.6) return false; return Math.Abs(hypot(p.x - x, p.y - y) - rad(bt)) < 4.5 + p.r - 2; };
		return h;
	}

	// ---------- the lines of the letter (0:22.6) ----------
	// A line of words (pink pills hh px high) on y slides in from the right edge to the left at sp px/s from b (Arabic reads right to left).
	// ws = the words' lengths, gs = the spaces between them. Its lane shows dashed (white) from b - tl.
	public static Hz RZRow(double y, double[] ws, double[] gs, double b, double tl, double sp, double hh = 44)
	{
		var h = new Hz { kind = "RZRow" }; double tb = B(b); int n = ws.Length; var off = new double[n]; double tot = 0;
		for (int k = 0; k < n; k++) { off[k] = tot; tot += ws[k] + (k < gs.Length ? gs[k] : 0); }
		double lead(double t) => W + 12 - sp * (t - tb);
		h.update = (t, dt) => { if (t > tb && lead(t) + tot < -40) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			if (bt < b + 1.5)
			{
				double q = RZk(bt, b - tl, b) * (1 - RZk(bt, b, b + 1.5));
				RZDash(0, y, W, y, RZW(0.18 + 0.4 * q), 2);
				g.fillStyle = RZW(0.6 * q); g.beginPath(); g.moveTo(W - 14, y - 14); g.lineTo(W - 34, y); g.lineTo(W - 14, y + 14); g.closePath(); g.fill();
			}
			if (t >= tb)
			{
				double x0 = lead(t), r = hh / 2;
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = RZP(0.85);
				for (int k = 0; k < n; k++)
				{
					double a = x0 + off[k], e = a + ws[k]; if (e < -20 || a > W + 20) continue;
					g.beginPath(); g.arc(a + r, y, r, Math.PI / 2, Math.PI * 1.5); g.lineTo(e - r, y - r); g.arc(e - r, y, r, -Math.PI / 2, Math.PI / 2); g.closePath(); g.fill();
				}
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tb || Math.Abs(p.y - y) > hh / 2 + p.r) return false; double x0 = lead(t), r = hh / 2;
			for (int k = 0; k < n; k++) { double a = x0 + off[k], e = a + ws[k]; if (RZSeg(p.x, p.y, a + r, y, e - r, y) < r + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- the envelope (0:37.6) ----------
	// The four flaps of an envelope: side 0 top, 1 right, 2 bottom, 3 left; a flap is the triangle from its edge to the middle of the screen
	// (dep = how far, 1 = to the middle). Its outline shows dashed from b - tl; at b it folds in (0.15 beat), stays hold beats, folds back in back beats.
	static (double ax, double ay, double bx, double by, double cx, double cy) RZFlapTri(int side, double dep)
	{
		double mx = W / 2, my = H / 2;
		return side switch
		{
			0 => (0, 0, W, 0, mx, my * dep),
			1 => (W, 0, W, H, W - mx * dep, my),
			2 => (W, H, 0, H, mx, H - my * dep),
			_ => (0, H, 0, 0, mx * dep, my),
		};
	}
	static bool RZInTri(double px, double py, (double ax, double ay, double bx, double by, double cx, double cy) T, double m)
	{
		double d1 = (px - T.bx) * (T.ay - T.by) - (T.ax - T.bx) * (py - T.by), d2 = (px - T.cx) * (T.by - T.cy) - (T.bx - T.cx) * (py - T.cy), d3 = (px - T.ax) * (T.cy - T.ay) - (T.cx - T.ax) * (py - T.ay);
		bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
		if (!(neg && pos)) return true;
		return RZSeg(px, py, T.ax, T.ay, T.bx, T.by) < m || RZSeg(px, py, T.bx, T.by, T.cx, T.cy) < m || RZSeg(px, py, T.cx, T.cy, T.ax, T.ay) < m;
	}
	static void RZTriPath((double ax, double ay, double bx, double by, double cx, double cy) T) { g.beginPath(); g.moveTo(T.ax, T.ay); g.lineTo(T.bx, T.by); g.lineTo(T.cx, T.cy); g.closePath(); }
	public static Hz RZFlap(int[] sides, double dep, double b, double tl, double hold, double back)
	{
		var h = new Hz { kind = "RZFlap" };
		h.update = (t, dt) => { if (BT(t) > b + hold + back) h.dead = true; };
		double k(double bt) => bt < b ? 0 : bt < b + hold ? RZOut((bt - b) / 0.15) : 1 - RZk(bt, b + hold, b + hold + back);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			foreach (var s in sides)
			{
				if (bt < b + 0.15)
				{
					double q = RZk(bt, b - tl, b); var T = RZFlapTri(s, dep);
					g.fillStyle = RZW(0.04 + 0.06 * q); RZTriPath(T); g.fill();
					g.strokeStyle = RZW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); RZTriPath(T); g.stroke(); g.setLineDash(new double[0]);
				}
				double kk = k(bt); if (kk <= 0.01) continue; var F = RZFlapTri(s, dep * kk);
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = RZP(0.5); RZTriPath(F); g.fill();
				g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 4; g.lineJoin = "round"; RZTriPath(F); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double kk = k(BT(t)); if (kk < 0.3) return false;
			foreach (var s in sides) if (RZInTri(p.x, p.y, RZFlapTri(s, dep * kk), p.r - 2)) return true;
			return false;
		};
		return h;
	}
	// the end (2:32): all four flaps close a little more on each step beat sb[i] (to depth sd[i]); the new depth shows dashed tl beats before.
	// Only the X between the flaps stays free. They fold away from fb.
	public static Hz RZEnvelope(double[] sb, double[] sd, double tl, double fb)
	{
		var h = new Hz { kind = "RZEnvelope" };
		h.update = (t, dt) => { if (BT(t) > fb + 1.2) h.dead = true; };
		double dep(double bt)
		{
			double d = 0; for (int i = 0; i < sb.Length; i++) { if (bt < sb[i]) break; double d0 = i == 0 ? 0 : sd[i - 1]; d = d0 + (sd[i] - d0) * RZOut((bt - sb[i]) / 0.3); }
			return d * (1 - RZk(bt, fb, fb + 1));
		}
		h.draw = t =>
		{
			double bt = BT(t), d = dep(bt); g.save();
			for (int i = 0; i < sb.Length; i++)
			{
				if (bt < sb[i] - tl || bt >= sb[i] + 0.3) continue; double q = RZk(bt, sb[i] - tl, sb[i]);
				g.strokeStyle = RZW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 });
				for (int s = 0; s < 4; s++) { RZTriPath(RZFlapTri(s, sd[i])); g.stroke(); }
				g.setLineDash(new double[0]);
			}
			if (d > 0.01)
			{
				g.shadowColor = PINK; g.shadowBlur = 14;
				for (int s = 0; s < 4; s++) { var F = RZFlapTri(s, d); g.fillStyle = RZP(0.5); RZTriPath(F); g.fill(); }
				g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 4; g.lineJoin = "round";
				for (int s = 0; s < 4; s++) { RZTriPath(RZFlapTri(s, d)); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double d = dep(BT(t)); if (d < 0.05) return false;
			for (int s = 0; s < 4; s++) if (RZInTri(p.x, p.y, RZFlapTri(s, d), p.r - 2)) return true;
			return false;
		};
		return h;
	}

	// ---------- the wax seal (0:52.6 and the end) ----------
	// A heart stamp (size s) comes down on (x,y) at b: its outline shows dashed from b - tl, it is pink (hurts) for hot beats,
	// then it stays as a gold seal (harmless) until b1. hot = 0: only the gold seal (the end).
	public static Hz RZSeal(double x, double y, double s, double b, double tl, double hot, double b1)
	{
		var h = new Hz { kind = "RZSeal", z = hot > 0 ? 0 : -1 };   // the gold seal of the end lies under everything
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			if (bt < b)
			{
				double q = RZk(bt, b - tl, b);
				g.strokeStyle = RZW(0.3 + 0.55 * q); g.lineWidth = 2.5; g.setLineDash(new double[] { 12, 10 }); RZHeartPathAt(x, y, s); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = RZW(0.05 + 0.07 * q); RZHeartPathAt(x, y, s); g.fill();
				double ss = s * (1.6 - 0.6 * q * q); g.strokeStyle = RZG(0.5 * q); g.lineWidth = 3; RZHeartPathAt(x, y, ss); g.stroke();
			}
			else if (bt < b + hot)
			{
				double sq = 1 + 0.08 * (1 - RZk(bt, b, b + 0.2));
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = RZP(0.7); RZHeartPathAt(x, y, s * sq); g.fill();
				g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 5; RZHeartPathAt(x, y, s * sq); g.stroke();
			}
			else
			{
				double a = 1 - RZk(bt, b1, b1 + 1), pu = RZPulse(bt);
				g.globalAlpha = a; g.shadowColor = "#ecbe5a"; g.shadowBlur = 16 + 10 * pu;
				var gr = g.createRadialGradient(x - s * 0.3, y - s * 0.3, 4, x, y, s * 1.2); gr.addColorStop(0, "rgba(250,224,150,1)"); gr.addColorStop(1, "rgba(176,120,40,1)");
				g.fillStyle = gr; RZHeartPathAt(x, y, s * (0.55 + 0.04 * pu)); g.fill(); g.shadowBlur = 0;
				g.strokeStyle = RZG(0.8); g.lineWidth = 3; RZHeartPathAt(x, y, s * 0.42); g.stroke();
			}
			g.restore();
		};
		if (hot > 0) h.hit = (p, t) => { double bt = BT(t); if (bt < b || bt >= b + hot) return false; return RZInHeart(p.x, p.y, x, y, s * (1 + 0.08 * (1 - RZk(bt, b, b + 0.2))), p.r - 2); };
		return h;
	}
}
