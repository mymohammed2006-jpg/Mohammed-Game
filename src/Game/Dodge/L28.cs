using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Ya Tayeb El Galb (Abdul Majeed Abdullah), main level 28: "the kind heart". A warm gold heart in the night.
// Part 1 it beats (the heart monitor, the heart's waves, it bursts, the pulse walls, the hearts with a bite), part 2 it gives a rose
// (the rose's thorny stems turn, its petals fly, the petal rain), part 3 it shines (the light beams, the spiral of small hearts, the
// last heart that closes round it). The level is the song from 3:20.000 (songs/SONG_L28.mp3 starts there) to 5:20.3.
// Grid: 132.06 BPM, beat 0 = 0.345 s of the file (3:20.345 of the song). Every hazard is a pure function of time once it exists.
// Only pink hurts: the gold heart, the rose, the white lines and the dashed warnings never do.
public static partial class G
{
	static double TQk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string TQP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string TQW(double a) => $"rgba(240,236,228,{str(clamp(a, 0, 1))})";
	static string TQG(double a) => $"rgba(243,201,105,{str(clamp(a, 0, 1))})";
	static double TQEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double TQOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double TQRad(double d) => d * Math.PI / 180;
	static double TQSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void TQDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// where a ray from (x,y) along (dx,dy) leaves the screen (plus m px)
	static double TQExit(double x, double y, double dx, double dy, double m = 40)
	{
		double best = 1e9;
		if (dx > 1e-6) best = Math.Min(best, (W + m - x) / dx); if (dx < -1e-6) best = Math.Min(best, (-m - x) / dx);
		if (dy > 1e-6) best = Math.Min(best, (H + m - y) / dy); if (dy < -1e-6) best = Math.Min(best, (-m - y) / dy);
		return Math.Max(0, best);
	}

	// ---------- the heart shape ----------
	// The classic heart curve, NH points, scaled so it is 2 wide (x -1..1) and about 1.8 high, its middle at 0. Point i is at the
	// curve's angle u = i/NH turns: u 0 is the dip at the top, 0.5 the tip at the bottom.
	const int TQNH = 96;
	static double[] _tqhx, _tqhy;
	static void TQHeartInit()
	{
		if (_tqhx != null) return; _tqhx = new double[TQNH]; _tqhy = new double[TQNH];
		for (int i = 0; i < TQNH; i++)
		{
			double u = TAU * i / TQNH, s = JS.sin(u);
			_tqhx[i] = 16 * s * s * s / 16; _tqhy[i] = (-(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)) - 2.5) / 16;
		}
	}
	static double TQHX(int i) { TQHeartInit(); return _tqhx[((i % TQNH) + TQNH) % TQNH]; }
	static double TQHY(int i) { TQHeartInit(); return _tqhy[((i % TQNH) + TQNH) % TQNH]; }
	// is the outline's piece i (from point i to i+1) in the gap (centre gu turns, width gw turns)?
	static bool TQOpen(int i, double gu, double gw)
	{
		if (gw <= 0) return false; double u = (i + 0.5) / TQNH, d = Math.Abs(((u - gu) % 1 + 1.5) % 1 - 0.5); return d < gw / 2;
	}
	// a filled heart (s = half its width) turned by rot
	static void TQHeartFill(double x, double y, double s, double rot = 0)
	{
		TQHeartInit(); double c = JS.cos(rot), sn = JS.sin(rot);
		g.beginPath(); for (int i = 0; i < TQNH; i += 3) { double hx = _tqhx[i] * s, hy = _tqhy[i] * s, px = x + c * hx - sn * hy, py = y + sn * hx + c * hy; if (i == 0) g.moveTo(px, py); else g.lineTo(px, py); }
		g.closePath(); g.fill();
	}
	// the outline (scale s) with its gap, as straight pieces: the drawing and the hit use the same pieces
	static void TQHeartPieces(double x, double y, double s, double gu, double gw, Action<double, double, double, double> f)
	{
		TQHeartInit();
		for (int i = 0; i < TQNH; i++) { if (TQOpen(i, gu, gw)) continue; int j = (i + 1) % TQNH; f(x + _tqhx[i] * s, y + _tqhy[i] * s, x + _tqhx[j] * s, y + _tqhy[j] * s); }
	}
	static void TQHeartStroke(double x, double y, double s, double gu, double gw)
	{
		g.beginPath(); bool pen = false;
		for (int i = 0; i <= TQNH; i++)
		{
			int k = i % TQNH;
			if (i < TQNH && TQOpen(i, gu, gw)) { if (pen) { g.lineTo(x + TQHX(k) * s, y + TQHY(k) * s); pen = false; } continue; }
			if (!pen) { g.moveTo(x + TQHX(k) * s, y + TQHY(k) * s); pen = true; } else g.lineTo(x + TQHX(k) * s, y + TQHY(k) * s);
		}
		g.stroke();
	}
	static bool TQHeartHit(Player p, double x, double y, double s, double gu, double gw, double th)
	{
		// quick reject: far from the outline's box
		if (p.x < x - s - th - p.r || p.x > x + s + th + p.r || p.y < y - 0.95 * s - th - p.r || p.y > y + 0.95 * s + th + p.r) return false;
		bool hit = false; double lim = th / 2 + p.r - 3;
		TQHeartPieces(x, y, s, gu, gw, (x0, y0, x1, y1) => { if (!hit && TQSeg(p.x, p.y, x0, y0, x1, y1) < lim) hit = true; });
		return hit;
	}
	// a small solid heart (half width s, turned by rot) hits where it is drawn: inside its outline, or within p.r - 3 of it
	static bool TQDotHit(Player p, double x, double y, double s, double rot)
	{
		double dx = p.x - x, dy = p.y - y; if (dx * dx + dy * dy > (s + p.r) * (s + p.r)) return false;
		TQHeartInit(); double c = JS.cos(rot), sn = JS.sin(rot), u = c * dx + sn * dy, v = -sn * dx + c * dy;   // into the heart's own frame
		bool inside = false; double best = 1e9;
		for (int i = 0, j = TQNH - 3; i < TQNH; j = i, i += 3)
		{
			double xi = _tqhx[i] * s, yi = _tqhy[i] * s, xj = _tqhx[j] * s, yj = _tqhy[j] * s;
			if ((yi > v) != (yj > v) && u < (xj - xi) * (v - yi) / (yj - yi) + xi) inside = !inside;
			best = Math.Min(best, TQSeg(u, v, xi, yi, xj, yj));
		}
		return inside || best < p.r - 3;
	}

	// ---------- the gold heart (harmless) and the rose (harmless) ----------
	// how strongly the heart beats at bt (0-1): a quick swell on each kick of the song
	static double TQPulse(double bt)
	{
		double v = 0; foreach (var k in TQ_PULSE) { if (k > bt) break; double d = bt - k; if (d < 0.6) v = Math.Max(v, 1 - d / 0.6); }
		return v;
	}
	public struct TQH { public double x, y, s, a, glow; }
	public static TQH TQHeartAt(double bt)
	{
		var m = new TQH { x = W / 2, y = H / 2, s = 46, a = 1 };
		if (bt < 128) { m.a = TQk(bt, -1, 0.5) * (1 - TQk(bt, 127, 128)); }
		else if (bt < 184) { m.a = 0; }
		else { m.s = 58; m.a = TQk(bt, 183, 184.5); m.glow = TQk(bt, 184, 186) * (bt >= 262 ? 1 + 2 * TQk(bt, 262, 264) : 1); }
		m.s *= 1 + 0.12 * TQPulse(bt);
		return m;
	}
	public static void TQHeart(double bt, double fade)
	{
		var m = TQHeartAt(bt); if (m.a * fade <= 0.01) return;
		g.save(); g.globalAlpha = m.a * fade;
		g.shadowColor = "#f3c969"; g.shadowBlur = 24 + 30 * m.glow;
		var gr = g.createRadialGradient(m.x - m.s * 0.3, m.y - m.s * 0.35, 4, m.x, m.y, m.s * 1.3); gr.addColorStop(0, "rgba(255,244,214,1)"); gr.addColorStop(1, "rgba(214,150,48,1)");
		g.fillStyle = gr; TQHeartFill(m.x, m.y, m.s); g.shadowBlur = 0;
		g.strokeStyle = "rgba(255,250,235,0.7)"; g.lineWidth = 2; TQHeartStroke(m.x, m.y, m.s * 0.78, 0, 0);
		g.restore();
	}
	// the rose: rings of round petals, cream with gold edges, turning slowly; o = how open it is (0-1)
	public static void TQRose(double x, double y, double sc, double rot, double o, double a)
	{
		if (a <= 0.01) return;
		g.save(); g.globalAlpha = a; g.translate(x, y); g.rotate(rot); g.scale(sc, sc);
		g.shadowColor = "#f3c969"; g.shadowBlur = 20;
		for (int ring = 3; ring >= 0; ring--)
		{
			int n = 5 + ring; double rr = 14 + ring * (12 + 6 * o), pw = 16 + ring * 4;
			for (int k = 0; k < n; k++)
			{
				double q = TAU * k / n + ring * 0.6; g.save(); g.rotate(q);
				var gr = g.createRadialGradient(rr, 0, 2, rr, 0, pw); gr.addColorStop(0, "rgba(255,246,226,1)"); gr.addColorStop(1, "rgba(226,178,92,1)");
				g.fillStyle = gr; g.beginPath(); g.ellipse(rr, 0, pw, pw * 0.72, 0, 0, TAU); g.fill();
				g.strokeStyle = "rgba(170,120,50,0.55)"; g.lineWidth = 1.5; g.stroke(); g.restore();
			}
			g.shadowBlur = 0;
		}
		g.fillStyle = "rgba(196,140,60,1)"; circle(0, 0, 9);
		g.restore();
	}

	// ---------- the stage (harmless, under everything) ----------
	public static Hz TQStage(double t1)
	{
		var h = new Hz { kind = "TQStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p2 = TQk(bt, 127, 129) * (1 - TQk(bt, 183, 185)), p3 = TQk(bt, 183, 185);
			g.save(); g.globalAlpha = a;
			// night: deep blue, part 2 a garden green-blue, part 3 a warm dark amber
			double r0 = 10 + 2 * p2 + 22 * p3, g0 = 12 + 14 * p2 + 12 * p3, b0 = 34 - 6 * p2 - 22 * p3;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(r0)},{str(g0)},{str(b0)},1)"); gr.addColorStop(1, $"rgba({str(r0 * 0.35)},{str(g0 * 0.35)},{str(b0 * 0.4)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// stars, they twinkle on the beat (looks only)
			double pu = TQPulse(bt);
			for (int i = 0; i < 46; i++)
			{
				double sx = (i * 277.3 + 31) % W, sy = (i * 151.7 + 17) % (H - 40), tw = 0.35 + 0.35 * JS.sin(t * (0.7 + (i % 5) * 0.3) + i) + (i % 3 == 0 ? 0.3 * pu : 0);
				g.fillStyle = TQW(clamp(tw, 0, 1) * 0.5 * (1 - 0.6 * p3)); circle(sx, sy, i % 7 == 0 ? 1.8 : 1.1);
			}
			// soft warm light round the middle that breathes with the heart
			double lr = 260 + 60 * pu + 140 * p3;
			var lg = g.createRadialGradient(W / 2, H / 2, 10, W / 2, H / 2, lr); lg.addColorStop(0, p2 > 0.5 ? "rgba(150,170,110,0.10)" : $"rgba(243,201,105,{str(0.07 + 0.06 * p3)})"); lg.addColorStop(1, "rgba(0,0,0,0)");
			g.fillStyle = lg; g.fillRect(W / 2 - lr, H / 2 - lr, 2 * lr, 2 * lr);
			// part 3: gold dust rising (looks only)
			if (p3 > 0) { g.fillStyle = TQG(0.22 * p3); for (int i = 0; i < 28; i++) { double dx = (i * 383.7) % W, sp = 18 + (i * 37) % 36, dy = H + 10 - ((i * 211.3 + t * sp) % (H + 20)); circle(dx, dy, i % 5 == 0 ? 2.2 : 1.2); } }
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
			TQHeart(bt, a);
			if (bt >= 127 && bt < 185) TQRose(W / 2, H / 2, 1 + 0.06 * TQPulse(bt), TQRad(6 * (bt - 128)), TQk(bt, 128, 160), TQk(bt, 127, 128.5) * (1 - TQk(bt, 183, 184.5)));
		};
		return h;
	}
	// The warnings (dashed lines, gold marks, white traces) of every attack are drawn first, by one layer under all of them, so a
	// warning never covers the pink of another attack: each attack's draw draws only its warnings while TQWarnPass is on, else only its pink.
	static bool TQWarnPass;
	public static Hz TQWarnLayer(double t1)
	{
		var h = new Hz { kind = "TQWarn", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t => { TQWarnPass = true; try { foreach (var o in hazards) if (o.hurts && o.kind != null && o.kind.StartsWith("TQ")) o.draw(t); } finally { TQWarnPass = false; } };
		return h;
	}
	public static Hz TQFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "TQFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- 1. the heart monitor (3:20.3) ----------
	// A white base line across the screen at y0 and a bright dot (the monitor's pen) that runs along it, one way in 8 beats, then again.
	// On each kick kb the pen draws a spike where it is: a tall zigzag that goes from the top of the screen to the bottom. It shows dashed
	// from kb - tl (where the pen will be), is pink and hurts for hot beats from kb, then cools to a white trace (harmless).
	public static double TQPen(double bt, double b0, int dir) { double f = ((bt - b0) / 8) % 1; if (f < 0) f += 1; return dir > 0 ? 50 + 1180 * f : 1230 - 1180 * f; }
	static void TQSpike(double x, double y0, Action<double, double, double, double> f)
	{
		double[] px = { -46, -24, -12, 0, 12, 26, 46 }, py = { 0, 0, 40, -y0 - 20, H - y0 + 20, -26, 0 };
		for (int i = 0; i + 1 < px.Length; i++) f(x + px[i], y0 + py[i], x + px[i + 1], y0 + py[i + 1]);
	}
	public static Hz TQEcg(double b0, double b1, int dir, double y0, double[] kb, double hot, double tl)
	{
		var h = new Hz { kind = "TQEcg" };
		h.update = (t, dt) => { if (BT(t) > b1 + hot + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = TQk(bt, b0 - 1, b0) * (1 - TQk(bt, b1, b1 + 1));
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			// the base line and the pen's gold trail (harmless)
			if (TQWarnPass && a > 0)
			{
				g.strokeStyle = TQW(0.18 * a); g.lineWidth = 2; g.beginPath(); g.moveTo(0, y0); g.lineTo(W, y0); g.stroke();
				double px = TQPen(bt, b0, dir), tail = 160;
				var lg = g.createLinearGradient(px - dir * tail, 0, px, 0); lg.addColorStop(0, TQG(0)); lg.addColorStop(1, TQG(0.8 * a));
				g.strokeStyle = lg; g.lineWidth = 3; g.beginPath(); g.moveTo(px - dir * tail, y0); g.lineTo(px, y0); g.stroke();
				g.shadowColor = "#f3c969"; g.shadowBlur = 16; g.fillStyle = TQG(a); circle(px, y0, 6); g.shadowBlur = 0;
			}
			foreach (var k in kb)
			{
				if (bt < k - tl || bt > k + hot + 1) continue; double x = TQPen(k, b0, dir);
				if (bt < k) { if (TQWarnPass) { double q = TQk(bt, k - tl, k); TQSpike(x, y0, (x0, y_0, x1, y_1) => TQDash(x0, y_0, x1, y_1, TQW(0.2 + 0.55 * q), 2)); } continue; }
				if (bt < k + hot)
				{
					if (TQWarnPass) continue;
					g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 9; g.beginPath();
					TQSpike(x, y0, (x0, y_0, x1, y_1) => { g.moveTo(x0, y_0); g.lineTo(x1, y_1); }); g.stroke(); g.shadowBlur = 0;
				}
				else if (TQWarnPass)
				{
					double q = 1 - TQk(bt, k + hot, k + hot + 1); g.strokeStyle = TQW(0.45 * q); g.lineWidth = 3; g.beginPath();
					TQSpike(x, y0, (x0, y_0, x1, y_1) => { g.moveTo(x0, y_0); g.lineTo(x1, y_1); }); g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); bool hit = false;
			foreach (var k in kb)
			{
				if (hit) break; if (bt < k || bt >= k + hot) continue; double x = TQPen(k, b0, dir); if (Math.Abs(p.x - x) > 60) continue;
				TQSpike(x, y0, (x0, y_0, x1, y_1) => { if (!hit && TQSeg(p.x, p.y, x0, y_0, x1, y_1) < 4.5 + p.r - 3) hit = true; });
			}
			return hit;
		};
		return h;
	}

	// ---------- 2. the heart's waves (3:34.9) ----------
	// A heart-shaped ring grows out of the gold heart at sp px/s (its half width) from bf, th thick, with a gap gw turns wide at gu
	// (0 = the dip at the top, 0.5 = the tip at the bottom). From bf - tl a small dashed heart shows round the heart with the gap in gold.
	public static Hz TQWave(double bf, double gu, double gw, double sp, double th, double tl, double cx = W / 2, double cy = H / 2, double s0 = 0)
	{
		var h = new Hz { kind = "TQWave" }; double tf = B(bf);
		double sc(double t) => s0 + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && sc(t) > 1150) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < bf)
			{
				if (!TQWarnPass) { g.restore(); return; }
				double q = TQk(bt, bf - tl, bf); g.strokeStyle = TQW(0.25 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 });
				TQHeartStroke(cx, cy, 92, gu, gw); g.setLineDash(new double[0]);
				g.strokeStyle = TQG(0.5 + 0.5 * q); g.lineWidth = 4; g.beginPath();
				foreach (var e in new[] { gu - gw / 2, gu + gw / 2 }) { int i = (int)Math.Round(((e % 1 + 1) % 1) * TQNH); double ex = TQHX(i), ey = TQHY(i), l = hypot(ex, ey) + 1e-9; g.moveTo(cx + ex * 80, cy + ey * 80); g.lineTo(cx + ex * 80 + ex / l * 40, cy + ey * 80 + ey / l * 40); }
				g.stroke(); g.restore(); return;
			}
			if (TQWarnPass) { g.restore(); return; }
			double s = sc(t); g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = th; TQHeartStroke(cx, cy, s, gu, gw);
			g.restore();
		};
		h.hit = (p, t) => t >= tf && TQHeartHit(p, cx, cy, sc(t), gu, gw, th);
		return h;
	}
	// The heart bursts: n small solid hearts fly out from the middle at sp px/s from bf, the first at a0 degrees (0 = right, clockwise).
	// From bf - tl their ways show as short dashed lines.
	public static Hz TQBurst(double bf, int n, double a0, double sp, double sz, double tl, double cx = W / 2, double cy = H / 2)
	{
		var h = new Hz { kind = "TQBurst" }; double tf = B(bf);
		double ang(int k) => TQRad(a0 + k * 360.0 / n);
		h.update = (t, dt) => { if (t > tf && 40 + (t - tf) * sp > 1000) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = TQk(bt, bf - tl, bf);
				if (TQWarnPass) for (int k = 0; k < n; k++) { double a = ang(k), dx = JS.cos(a), dy = JS.sin(a); TQDash(cx + dx * 70, cy + dy * 70, cx + dx * 250, cy + dy * 250, TQW(0.2 + 0.5 * q), 2); }
				g.restore(); return;
			}
			if (TQWarnPass) { g.restore(); return; }
			double d = 40 + (t - tf) * sp; g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK;
			for (int k = 0; k < n; k++) { double a = ang(k); TQHeartFill(cx + JS.cos(a) * d, cy + JS.sin(a) * d, sz, a + Math.PI / 2); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = 40 + (t - tf) * sp; if (Math.Abs(hypot(p.x - cx, p.y - cy) - d) > sz + p.r) return false;
			for (int k = 0; k < n; k++) { double a = ang(k); if (TQDotHit(p, cx + JS.cos(a) * d, cy + JS.sin(a) * d, sz, a + Math.PI / 2)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 3. the pulse walls (3:49.4) ----------
	// A wall made of a heart-monitor zigzag, the whole height of the screen, comes in from the right (dir -1) or the left (dir 1) at sp px/s
	// from bf. It has a gap gh high round gy where the line goes flat (a thin white line, harmless). From bf - tl the side it comes from
	// shows a dashed line with the gap left open and gold marks.
	static void TQWallPieces(double x, double gy, double gh, Action<double, double, double, double> f)
	{
		const double STEPY = 26, AMP = 15; double prevX = 0, prevY = -40; bool have = false; int k = 0;
		for (double y = -40; y <= H + 40; y += STEPY, k++)
		{
			bool inGap = Math.Abs(y - gy) < gh / 2; double xx = x + (inGap ? 0 : (k % 2 == 0 ? -AMP : AMP));
			if (have && !inGap && Math.Abs(prevY - gy) >= gh / 2) f(prevX, prevY, xx, y);
			prevX = xx; prevY = y; have = true;
		}
	}
	public static Hz TQWall(double bf, int dir, double gy, double gh, double sp, double tl)
	{
		var h = new Hz { kind = "TQWall" }; double tf = B(bf);
		double xc(double t) => dir > 0 ? -30 + sp * Math.Max(0, t - tf) : W + 30 - sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { double x = xc(t); if (t > tf && (x < -60 || x > W + 60)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (TQWarnPass && bt < bf + 0.5)
			{
				double q = TQk(bt, bf - tl, bf) * (1 - TQk(bt, bf, bf + 0.5)), xe = dir > 0 ? 10 : W - 10;
				g.strokeStyle = TQW(0.25 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				g.moveTo(xe, 0); g.lineTo(xe, Math.Max(0, gy - gh / 2)); g.moveTo(xe, Math.Min(H, gy + gh / 2)); g.lineTo(xe, H); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = TQG(0.5 + 0.5 * q); g.lineWidth = 4; double xx = dir > 0 ? 4 : W - 4, d2 = dir > 0 ? 28 : -28;
				g.beginPath(); g.moveTo(xx, gy - gh / 2); g.lineTo(xx + d2, gy - gh / 2); g.moveTo(xx, gy + gh / 2); g.lineTo(xx + d2, gy + gh / 2); g.stroke();
			}
			if (bt >= bf)
			{
				double x = xc(t);
				if (TQWarnPass) { g.strokeStyle = TQW(0.5); g.lineWidth = 2; g.beginPath(); g.moveTo(x, gy - gh / 2 + 6); g.lineTo(x, gy + gh / 2 - 6); g.stroke(); g.restore(); return; }
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 8; g.beginPath();
				TQWallPieces(x, gy, gh, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double x = xc(t); if (Math.Abs(p.x - x) > 15 + 4 + p.r) return false; bool hit = false;
			TQWallPieces(x, gy, gh, (x0, y0, x1, y1) => { if (!hit && TQSeg(p.x, p.y, x0, y0, x1, y1) < 4 + p.r - 3) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 4. the hearts with a bite (4:04) ----------
	// A heart outline grows at (cx, cy) from nothing to scale sm in grow beats from bf, stays hold beats, then fades (harmless while it
	// fades). It has a bite: a gap gw turns wide at gu. From bf - tl it shows dashed at its full size with the bite in gold.
	public static Hz TQBite(double cx, double cy, double sm, double gu, double gw, double bf, double tl, double grow, double hold, double th)
	{
		var h = new Hz { kind = "TQBite" };
		double sc(double bt) => sm * TQOut((bt - bf) / grow);
		h.update = (t, dt) => { if (BT(t) > bf + grow + hold + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (TQWarnPass && bt < bf + grow)
			{
				double q = TQk(bt, bf - tl, bf); g.strokeStyle = TQW(0.18 + 0.4 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 });
				TQHeartStroke(cx, cy, sm, gu, gw); g.setLineDash(new double[0]);
				g.strokeStyle = TQG(0.4 + 0.6 * q); g.lineWidth = 4; g.beginPath();
				foreach (var e in new[] { gu - gw / 2, gu + gw / 2 }) { int i = (int)Math.Round(((e % 1 + 1) % 1) * TQNH); double ex = TQHX(i) * sm, ey = TQHY(i) * sm, l = hypot(ex, ey) + 1e-9; g.moveTo(cx + ex - ex / l * 16, cy + ey - ey / l * 16); g.lineTo(cx + ex + ex / l * 16, cy + ey + ey / l * 16); }
				g.stroke();
			}
			if (bt >= bf)
			{
				double end = bf + grow + hold;
				if (bt < end) { if (!TQWarnPass) { g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = th; TQHeartStroke(cx, cy, sc(bt), gu, gw); } }
				else if (TQWarnPass) { g.strokeStyle = TQW(0.5 * (1 - TQk(bt, end, end + 0.5))); g.lineWidth = 3; TQHeartStroke(cx, cy, sm, gu, gw); }
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); return bt >= bf && bt < bf + grow + hold && TQHeartHit(p, cx, cy, sc(bt), gu, gw, th); };
		return h;
	}
}
