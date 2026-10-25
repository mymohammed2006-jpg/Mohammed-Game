using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L20 "New Game", part 3: the boss (the mothership, its sprays and laser, the Tempest tube, the shock rings, the games it throws at you
// while it circles the arena, its end), and the kinds (the events in data/dodge_L20.json). See L20.cs
public static partial class G
{
	// the mothership, drawn in its own frame at (0,0): it comes down from the top (in0 .. on0, beats), hurts from on0, cracks at ex0 and
	// bursts a beat later. Its health bar (white, harmless) runs out from on0 to ex0
	// the hull (the drawing's corners) and a point within m of a convex outline
	static readonly double[] L20HULL = { -290, -18, -200, -58, 200, -58, 290, -18, 250, 34, -250, 34 };
	static bool L20Poly(double x, double y, double m, double[] q)
	{
		int n = q.Length / 2; double cx = 0, cy = 0; for (int i = 0; i < n; i++) { cx += q[2 * i] / n; cy += q[2 * i + 1] / n; }
		for (int i = 0; i < n; i++)
		{
			double ax = q[2 * i], ay = q[2 * i + 1], bx = q[(2 * i + 2) % (2 * n)], by = q[(2 * i + 3) % (2 * n)], nx = by - ay, ny = ax - bx, l = Math.Sqrt(nx * nx + ny * ny);
			nx /= l; ny /= l; if (nx * (cx - ax) + ny * (cy - ay) > 0) { nx = -nx; ny = -ny; }
			if (nx * (x - ax) + ny * (y - ay) >= m) return false;
		}
		return true;
	}

	public static Hz L20Boss(double in0, double on0, double ex0)
	{
		var h = new Hz { kind = "L20Boss" }; bool boom = false;
		double drop(double bt) => -260 * (1 - L20Ease(L20k(bt, in0, on0)));
		h.update = (t, dt) =>
		{
			double bt = BT(t);
			if (!boom && bt >= ex0 + 1) { boom = true; var b = L20BossAt(L20Th(bt)); burst(b.x, b.y, PINK, 40, 700); burst(b.x, b.y, "#ffffff", 30, 500); shake = Math.Max(shake, 14); }
			if (bt > ex0 + 3) h.dead = true;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < in0 || bt >= ex0 + 1) return; double th = L20Th(bt); var b = L20BossAt(th); bool on = bt >= on0;
			// the health bar (screen, top)
			if (bt >= on0 - 1) { double k = 1 - L20k(bt, on0, ex0); g.fillStyle = "rgba(255,255,255,0.12)"; g.fillRect(W / 2 - 300, 10, 600, 8); g.fillStyle = $"rgba(255,255,255,{str(0.55 + 0.3 * pulse)})"; g.fillRect(W / 2 - 300, 10, 600 * k, 8); }
			g.save(); g.translate(b.x, b.y); g.rotate(th); g.translate(0, drop(bt));
			double cr = bt >= ex0 ? L20k(bt, ex0, ex0 + 1) : 0, sh = cr > 0 ? JS.sin(t * 70) * 6 * cr : 0; g.translate(sh, 0);
			if (on) { g.shadowColor = PINK; g.shadowBlur = 24; }
			g.fillStyle = on ? PINK : L20P(0.3);
			g.beginPath(); g.moveTo(-290, -18); g.lineTo(-200, -58); g.lineTo(200, -58); g.lineTo(290, -18); g.lineTo(250, 34); g.lineTo(-250, 34); g.closePath(); g.fill(); g.shadowBlur = 0;
			// the hull's lines, windows and the core (dark details on the pink)
			g.strokeStyle = "rgba(60,0,20,0.55)"; g.lineWidth = 3; g.beginPath(); g.moveTo(-260, 0); g.lineTo(260, 0); g.moveTo(-120, -58); g.lineTo(-150, 34); g.moveTo(120, -58); g.lineTo(150, 34); g.stroke();
			g.fillStyle = "rgba(60,0,20,0.6)"; for (int i = -5; i <= 5; i++) if (Math.Abs(i) > 1) g.fillRect(i * 40 - 8, -36, 16, 10);
			g.fillStyle = "rgba(255,230,240,0.85)"; circle(0, 6, 22 + 6 * pulse); g.fillStyle = on ? PINK : L20P(0.5); circle(0, 6, 12 + 4 * pulse);
			foreach (var sd in new[] { -1.0, 1.0 }) { g.fillStyle = "rgba(60,0,20,0.6)"; circle(sd * 200, 22, 14); g.fillStyle = "rgba(255,230,240,0.7)"; circle(sd * 200, 22, 6); }
			if (cr > 0) { g.strokeStyle = $"rgba(255,255,255,{str(0.5 + 0.5 * cr)})"; g.lineWidth = 3; g.beginPath(); for (int i = 0; i < 7; i++) { double a = i * 0.9 + 0.2, l = 200 * cr; g.moveTo(0, 0); g.lineTo(JS.cos(a) * l * 0.5, JS.sin(a) * l * 0.3); g.lineTo(JS.cos(a + 0.3) * l, JS.sin(a + 0.3) * l * 0.25); } g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < on0 || bt >= ex0 + 1) return false; double th = L20Th(bt); var b = L20BossAt(th);
			double dx = p.x - b.x, dy = p.y - b.y, c = JS.cos(th), s = JS.sin(th), u = dx * c + dy * s, v = -dx * s + dy * c;
			return L20Poly(u, v, p.r - 2, L20HULL);
		};
		return h;
	}

	// the sprays: emitters (ex,ey) fire a fan of k bullets (spread rad apart) every ev beats from b0 to b1; the fan swings round the
	// straight-down line by amp (rad) every per beats; the right-hand emitters mirror the left. Bullets fly sp px/s, radius R
	public class L20Bul { public double t0, x, y, dx, dy; }
	public static Hz L20Spray(double[] ex, double[] ey, double b0, double b1, double ev, int k, double spread, double amp, double per, double sp, double R)
	{
		var L = new List<L20Bul>(); var h = new Hz { kind = "L20Spray" };
		for (double b = b0; b < b1 - 1e-9; b += ev)
			for (int e = 0; e < ex.Length; e++)
			{
				double sw = amp * JS.sin(TAU * (b - b0) / per), a = Math.PI / 2 + (ex[e] < W / 2 ? sw : -sw);
				for (int j = 0; j < k; j++) { double q = a + (j - (k - 1) / 2.0) * spread; L.Add(new L20Bul { t0 = B(b), x = ex[e], y = ey[e], dx = JS.cos(q), dy = JS.sin(q) }); }
			}
		double life = 1500 / sp;
		h.update = (t, dt) => { if (t > B(b1) + life) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			// the emitters glow a beat before they start
			if (bt >= b0 - 1 && bt < b1) for (int e = 0; e < ex.Length; e++) { double kk = L20k(bt, b0 - 1, b0); g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * kk)})"; g.lineWidth = 3; g.beginPath(); g.arc(ex[e], ey[e], 20 + 8 * (1 - kk), 0, TAU); g.stroke(); }
			g.shadowColor = PINK; g.shadowBlur = 10;
			foreach (var q in L) { double u = t - q.t0; if (u < 0 || u > life) continue; double x = q.x + q.dx * sp * u, y = q.y + q.dy * sp * u; if (x < -30 || x > W + 30 || y < -30 || y > H + 30) continue; g.fillStyle = PINK; circle(x, y, R); g.fillStyle = "rgba(255,210,225,0.7)"; circle(x, y, R * 0.4); }
			g.shadowBlur = 0;
		};
		h.hit = (p, t) =>
		{
			foreach (var q in L) { double u = t - q.t0; if (u < 0 || u > life) continue; double x = q.x + q.dx * sp * u, y = q.y + q.dy * sp * u; if (hypot(p.x - x, p.y - y) < R + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the boss's laser from (ox,oy): at beat ba it takes aim at the square (a dashed line follows it until then), at bf it fires w wide and
	// sweeps swp rad (dir: the sign) until be. Aimed: "rnd" in the data
	public static Hz L20Beam(double ox, double oy, double ba, double bf, double be, double w, double swp)
	{
		var h = new Hz { kind = "L20Beam" }; double a0 = double.NaN;
		h.update = (t, dt) =>
		{
			double bt = BT(t); if (double.IsNaN(a0) && bt >= ba && player != null) a0 = JS.atan2(player.y - oy, player.x - ox);
			if (bt > be + 1) h.dead = true;
		};
		double ang(double bt) => a0 + swp * L20Ease(L20k(bt, bf, be));
		h.draw = t =>
		{
			double bt = BT(t); if (bt < ba - 1 || bt > be + 0.3) return;
			if (bt < bf)
			{
				double a = double.IsNaN(a0) ? JS.atan2((player?.y ?? H / 2) - oy, (player?.x ?? W / 2) - ox) : a0, k = L20k(bt, ba - 1, bf);
				g.fillStyle = L20P(0.05 + 0.2 * k); g.save(); g.translate(ox, oy); g.rotate(a); g.fillRect(0, -w / 2, 1600, w); g.restore();
				L20Dash(ox, oy, ox + JS.cos(a) * 1600, oy + JS.sin(a) * 1600, double.IsNaN(a0) ? L20G(0.45) : $"rgba(255,255,255,{str(0.6 + 0.35 * k)})", double.IsNaN(a0) ? 2 : 3);
				// which way it will sweep
				if (!double.IsNaN(a0)) { double q = a0 + Math.Sign(swp) * 0.12; g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(ox, oy, 140, a0, q, swp < 0); g.stroke(); }
				g.fillStyle = $"rgba(255,230,240,{str(0.4 + 0.5 * k)})"; circle(ox, oy, 10 + 18 * k);
				return;
			}
			double an = ang(bt), al = bt < be ? 1 : 1 - (bt - be) / 0.3, ww = w * Math.Min(1, (bt - bf) / 0.06 + 0.3);
			g.save(); g.globalAlpha = al; g.translate(ox, oy); g.rotate(an); g.shadowColor = PINK; g.shadowBlur = 26; g.fillStyle = bt - bf < 0.06 ? L20HOT : PINK; g.fillRect(0, -ww / 2, 1600, ww); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,210,225,0.6)"; g.fillRect(0, -ww / 6, 1600, ww / 3); g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= be || double.IsNaN(a0)) return false; double an = ang(bt), ww = w * Math.Min(1, (bt - bf) / 0.06 + 0.3);
			return L20Seg(p.x, p.y, ox, oy, ox + JS.cos(an) * 1600, oy + JS.sin(an) * 1600) < ww / 2 + p.r - 2;
		};
		return h;
	}

	// the Tempest tube round (cx,cy): n lanes that turn w rad a beat (from a0 at tb0). Spike i comes out of the middle (radius r0) in lane
	// sl[i] at beat sb[i] and climbs out at v px/s, len long. The eye in the middle (eR) burns. Grey from tw, kills ta .. te (beats)
	public static Hz L20Tube(double cx, double cy, int n, double w, double a0, double[] sb, double[] sl, double v, double len, double r0, double eR, double tw, double ta, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20Tube" }; double lw = TAU / n;
		double rot(double bt) => a0 + w * (bt - ta);
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.5)), ro = rot(Math.Max(bt, tw)); bool on = bt >= ta && bt < te;
			g.save(); g.globalAlpha = a;
			// the web
			g.strokeStyle = L20G(0.22); g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < n; i++) { double q = ro + i * lw; g.moveTo(cx + JS.cos(q) * eR, cy + JS.sin(q) * eR); g.lineTo(cx + JS.cos(q) * 900, cy + JS.sin(q) * 900); }
			g.stroke(); g.strokeStyle = L20G(0.08); for (double rr = 120; rr < 900; rr += 120) { g.beginPath(); g.arc(cx, cy, rr, 0, TAU); g.stroke(); }
			// the spikes
			g.shadowColor = PINK; g.shadowBlur = 12;
			for (int i = 0; i < sb.Length; i++)
			{
				double u = t - B(sb[i]); if (u < -0.5) continue; double rf = r0 + v * Math.Max(0, u); if (rf - len > 900) continue;
				double q0 = ro + sl[i] * lw + 0.03, q1 = ro + (sl[i] + 1) * lw - 0.03, ri = Math.Max(eR, rf - len);
				if (u < 0) { g.fillStyle = L20P(0.25 + 0.4 * (1 + u * 2)); g.beginPath(); g.arc(cx, cy, eR + 14, q0, q1); g.arc(cx, cy, eR, q1, q0, true); g.closePath(); g.fill(); continue; }
				g.fillStyle = on ? (u < 0.05 ? L20HOT : PINK) : L20P(0.3); g.beginPath(); g.arc(cx, cy, rf, q0, q1); g.arc(cx, cy, ri, q1, q0, true); g.closePath(); g.fill();
			}
			g.shadowBlur = 0;
			g.fillStyle = on ? PINK : L20P(0.15 + 0.3 * L20k(bt, tw, ta)); circle(cx, cy, eR); g.fillStyle = "rgba(60,0,20,0.6)"; circle(cx, cy, eR * 0.45); g.fillStyle = "rgba(255,230,240,0.8)"; circle(cx + JS.cos(ro) * eR * 0.15, cy + JS.sin(ro) * eR * 0.15, eR * 0.15);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy); if (d < eR + p.r - 2) return true;
			double pa = JS.atan2(dy, dx), ro = rot(bt), m = (p.r - 2) / Math.Max(d, 1);
			for (int i = 0; i < sb.Length; i++)
			{
				double u = t - B(sb[i]); if (u < 0) continue; double rf = r0 + v * u, ri = Math.Max(eR, rf - len); if (d > rf + p.r - 2 || d < ri - p.r + 2) continue;
				double mid = ro + (sl[i] + 0.5) * lw, x = (pa - mid) % TAU; if (x < -Math.PI) x += TAU; if (x > Math.PI) x -= TAU;
				if (Math.Abs(x) < lw / 2 - 0.03 + m) return true;
			}
			return false;
		};
		return h;
	}

	// a shock ring out of (cx,cy) from beat b0 (radius r0) at v px/s, th thick, with a gap (gw rad) at angle ga. The gap's way shows wn beats before
	public static Hz L20Shock(double cx, double cy, double b0, double v, double th, double ga, double gw, double r0, double wn)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20Shock" };
		double R(double t) => r0 + v * (t - B(b0));
		bool gap(double a, double d) { double x = (a - ga) % TAU; if (x < -Math.PI) x += TAU; if (x > Math.PI) x -= TAU; return Math.Abs(x) < gw / 2 - 3 / Math.Max(d, 1); }
		h.update = (t, dt) => { if (R(t) > 1600) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - wn) return;
			if (bt < b0) { double k = L20k(bt, b0 - wn, b0); g.fillStyle = $"rgba(255,255,255,{str(0.08 + 0.2 * k)})"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 900, ga - gw / 2, ga + gw / 2); g.closePath(); g.fill(); g.strokeStyle = L20G(0.4 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.arc(cx, cy, r0, ga + gw / 2, ga - gw / 2 + TAU); g.stroke(); g.setLineDash(new double[0]); return; }
			double r = R(t); g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = bt - b0 < 0.06 ? L20HOT : PINK; g.lineWidth = th; g.beginPath(); g.arc(cx, cy, r, ga + gw / 2, ga - gw / 2 + TAU); g.stroke(); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.08)"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, r, ga - gw / 2, ga + gw / 2); g.closePath(); g.fill();
		};
		h.hit = (p, t) => { if (BT(t) < b0) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy); return Math.Abs(d - R(t)) < th / 2 + p.r - 2 && !gap(JS.atan2(dy, dx), d); };
		return h;
	}

	// ---------- the games the boss throws (in its frame: u across, v away from it) ----------
	// the 7 Tetris pieces in a 4x4 box, turned r quarters round their middle (c)
	static readonly int[][] L20SH = { new[] { 0, 1, 1, 1, 2, 1, 3, 1 }, new[] { 1, 1, 2, 1, 1, 2, 2, 2 }, new[] { 0, 1, 1, 1, 2, 1, 1, 0 }, new[] { 1, 0, 2, 0, 0, 1, 1, 1 }, new[] { 0, 0, 1, 0, 1, 1, 2, 1 }, new[] { 0, 0, 0, 1, 1, 1, 2, 1 }, new[] { 2, 0, 0, 1, 1, 1, 2, 1 } };
	static readonly double[] L20SC = { 1.5, 1.5, 1, 1, 1, 1, 1 };
	static void L20Turn(int s, int r, int[] o)
	{
		var b = L20SH[s]; double c = L20SC[s];
		for (int j = 0; j < 4; j++) { double x = b[2 * j], y = b[2 * j + 1]; for (int k = 0; k < ((r % 4) + 4) % 4; k++) { double nx = 2 * c - y, ny = x; x = nx; y = ny; } o[2 * j] = (int)Math.Round(x); o[2 * j + 1] = (int)Math.Round(y); }
	}
	// a rain of pieces out of the boss: piece i (shape ps[i]) at column pc[i] (u = pc * 80) from beat pb[i], a row (80 px) every pst beats,
	// a quarter turn every beat; it shows as an outline at the boss pw beats before
	public static Hz L20Rain(double[] pb, double[] pc, double[] ps, double[] pr, double pst, double pw, double v0)
	{
		var h = new Hz { kind = "L20Rain" }; const double C = 80; var tmp = new int[8]; double last = pb.Length > 0 ? pb.Max() : 0;
		double row(int i, double bt) => v0 + C * Math.Floor((bt - pb[i]) / pst);
		h.update = (t, dt) => { if (BT(t) > last + 1600 / C * pst + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < pb.Length; i++)
			{
				double u = bt - pb[i]; if (u < -pw) continue; double y0 = u < 0 ? v0 : row(i, bt); if (y0 > 1700) continue;
				L20Turn((int)ps[i], (int)pr[i] + (int)Math.Floor(Math.Max(0, u)), tmp); double x0 = pc[i] * C - 2 * C;
				if (u < 0) { g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * (1 + u / pw))})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); for (int j = 0; j < 4; j++) g.strokeRect(x0 + tmp[2 * j] * C + 6, y0 + tmp[2 * j + 1] * C + 6, C - 12, C - 12); g.setLineDash(new double[0]); continue; }
				g.shadowColor = PINK; g.shadowBlur = 14; for (int j = 0; j < 4; j++) L20Cell(x0 + tmp[2 * j] * C + 2, y0 + tmp[2 * j + 1] * C + 2, C - 4, C - 4, u < 0.06 ? L20HOT : PINK); g.shadowBlur = 0;
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); var o = new int[8];
			for (int i = 0; i < pb.Length; i++)
			{
				double u = bt - pb[i]; if (u < 0) continue; double y0 = row(i, bt); if (y0 > 1700) continue; L20Turn((int)ps[i], (int)pr[i] + (int)Math.Floor(u), o); double x0 = pc[i] * C - 2 * C;
				for (int j = 0; j < 4; j++) if (L20In(p, x0 + o[2 * j] * C + 2, y0 + o[2 * j + 1] * C + 2, x0 + o[2 * j] * C + C - 2, y0 + o[2 * j + 1] * C + C - 2)) return true;
			}
			return false;
		};
		return h;
	}

	// a row of n invaders under the boss (v = y0), swaying sw px; at lb[i] invader lc[i] fires a laser away from the boss (shown wn before)
	public static Hz L20Row(int n, double sp, double y0, double sw, double[] lb, double[] lc, double lw, double lh, double wn, double b0, double b1)
	{
		var h = new Hz { kind = "L20Row" }; const double IR = 30;
		double ux(int c, double bt) => (c - (n - 1) / 2.0) * sp + sw * JS.sin(Math.PI * (bt - b0) / 2);
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 0.5) return; bool on = bt >= b0 && bt < b1; double a = Math.Min(1, 1 + (bt - b0)) * (bt > b1 ? Math.Max(0, 1 - (bt - b1) * 2) : 1);
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < lb.Length; i++)
			{
				if (bt < lb[i] - wn || bt > lb[i] + lh) continue; double x = ux((int)lc[i], lb[i]);
				if (bt < lb[i]) { double k = L20k(bt, lb[i] - wn, lb[i]); g.fillStyle = L20P(0.05 + 0.2 * k); g.fillRect(x - lw / 2, y0 + 20, lw, 1700); L20Dash(x, y0 + 20, x, 1700, L20G(0.35 + 0.55 * k), 2 + 2 * k); continue; }
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = bt - lb[i] < 0.06 ? L20HOT : PINK; g.fillRect(x - lw / 2, y0 + 20, lw, 1700); g.shadowBlur = 0;
			}
			if (on) { g.shadowColor = PINK; g.shadowBlur = 12; }
			for (int c = 0; c < n; c++) L20InvDraw(ux(c, bt), y0, IR, on ? PINK : L20P(0.3), ((int)Math.Floor(bt)) % 2 == 0);
			g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1 + lh) return false;
			if (bt < b1) for (int c = 0; c < n; c++) { double x = ux(c, bt); if (L20InvHit(p, x, y0, IR, ((int)Math.Floor(bt)) % 2 == 0)) return true; }
			for (int i = 0; i < lb.Length; i++) { if (bt < lb[i] || bt > lb[i] + lh) continue; double x = ux((int)lc[i], lb[i]); if (L20In(p, x - lw / 2, y0 + 20, x + lw / 2, 1800)) return true; }
			return false;
		};
		return h;
	}

	// the last hit: NEW GAME across the screen (harmless)
	public static Hz L20End(double b0, double b1)
	{
		var h = new Hz { kind = "L20End" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = L20k(bt, b0, b0 + 0.3), a = Math.Min(k, 1 - L20k(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(120 - 20 * k)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#ffffff"; g.shadowBlur = 30; g.fillStyle = "#ffffff"; g.fillText("NEW GAME", W / 2, H / 2); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L20.json) ----------
	// times are beats. "nz": Normal only, "ez": Easy only (the two have their own patterns where they differ)
	static int L20K(string name, Func<DodgeEv, Action> f) => DodgeKind(name, e => { var a = f(e); return () => { if (easy ? e.flag("nz") : e.flag("ez")) return; a(); }; });
	static bool L20F(DodgeEv e) => e.flag("frame");
	static void L20Add(DodgeEv e, Hz z) => hazards.Add(L20F(e) ? L20Frame(z) : z);

	static readonly int _k20fx = L20K("L20fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L20fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k20rot = L20K("L20rot", e => () => { _l20rb = e.nums("rb"); _l20ra = e.nums("ra"); });
	static readonly int _k20stage = L20K("L20stage", e => () => hazards.Add(L20Stage(B(e.num("to")))));
	static readonly int _k20tetris = L20K("L20tetris", e => () =>
	{
		var pz = e.nums("pz"); var P = new List<L20Piece>(); int i = 0;
		while (i < pz.Length) { int ns = (int)pz[i++]; var q = new L20Piece { b = new double[ns], c = new int[ns][] }; for (int s = 0; s < ns; s++) { q.b[s] = pz[i++]; q.c[s] = new int[8]; for (int j = 0; j < 8; j++) q.c[s][j] = (int)pz[i++]; } P.Add(q); }
		var sk = e.nums("sk"); var S = new List<L20Stk>(); i = 0;
		while (i < sk.Length) { var s = new L20Stk { col = (int)sk[i++] }; int nk = (int)sk[i++]; s.b = new double[nk]; s.r = new double[nk]; for (int j = 0; j < nk; j++) { s.b[j] = sk[i++]; s.r[j] = sk[i++]; } s.end = sk[i++]; S.Add(s); }
		hazards.Add(L20Tetris(P, S, e.nums("cl"), e.nums("clr"), e.num("tw"), e.num("te")));
	});
	static readonly int _k20ring = L20K("L20ring", e => () =>
		hazards.Add(L20Ring(e.num("x", W / 2), e.num("y", H / 2), e.num("rmin"), e.num("rmax"), e.num("per"), e.num("th"), e.num("gw") * Math.PI / 180, e.num("w"), e.num("rv"), e.num("a0") * Math.PI / 180, e.num("core"), e.num("tw"), e.num("ta"), e.num("te"), e.nums("jb"), e.nums("ja").Select(v => v * Math.PI / 180).ToArray())));
	static readonly int _k20front = L20K("L20front", e => () => hazards.Add(L20Front(e.num("x0"), e.num("dir"), e.num("sx"), e.nums("hy"), e.num("hh"), e.num("tw"), e.num("ta"), e.nums("sb"))));
	static readonly int _k20pong = L20K("L20pong", e => () =>
		L20Add(e, L20Pong(e.nums("nk"), e.nums("kb"), e.nums("kx"), e.nums("ky"), e.num("R"), e.flag("pad"), e.nums("lb"), e.nums("ly"), e.nums("rb"), e.nums("ry"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20shot = L20K("L20shot", e => () => hazards.Add(L20Shot(e.nums("bx"), e.nums("by"), e.num("R"), e.num("sp"), e.num("f0"), e.num("f1"), e.num("fa", -1))));
	static readonly int _k20ufo = L20K("L20ufo", e => () => hazards.Add(L20Ufo(e.num("y"), e.num("dir"), e.nums("bd"), e.num("vb"), e.num("wn"), e.num("ta"), e.num("te"))));
	static readonly int _k20lanes = L20K("L20lanes", e => () =>
		L20Add(e, L20Lanes(e.i("n"), e.num("lw"), e.num("cx0"), e.num("hitY"), e.num("vpb"), e.num("clipY", -400), e.nums("nb"), e.nums("nl"), e.nums("nlen"), e.num("tilt"), e.num("tb0"), e.num("tb1"), e.num("rx", W / 2), e.num("ry", H / 2),
			e.nums("sw"), e.nums("sa"), e.nums("sbb"), e.num("swd", 1), e.num("tw"), e.num("ta"), e.num("te"), e.flag("line"))));
	static readonly int _k20close = L20K("L20close", e => () => hazards.Add(L20Close(e.num("bw"), e.num("bh"), e.num("tw"), e.num("t0"), e.num("t1"), e.num("t2"), e.nums("cs"))));
	static readonly int _k20boss = L20K("L20boss", e => () => hazards.Add(L20Boss(e.num("in"), e.num("on"), e.num("ex"))));
	static readonly int _k20spray = L20K("L20spray", e => () =>
		hazards.Add(L20Spray(e.nums("ex"), e.nums("ey"), e.num("ta"), e.num("te"), e.num("ev"), e.i("fan"), e.num("spread") * Math.PI / 180, e.num("amp") * Math.PI / 180, e.num("per"), e.num("sp"), e.num("R"))));
	static readonly int _k20beam = L20K("L20beam", e => () => hazards.Add(L20Beam(e.num("x"), e.num("y"), e.num("ba"), e.num("bf"), e.num("be"), e.num("w"), e.num("swp") * Math.PI / 180)));
	static readonly int _k20tube = L20K("L20tube", e => () =>
		hazards.Add(L20Tube(e.num("x", W / 2), e.num("y", H / 2), e.i("n"), e.num("w"), e.num("a0") * Math.PI / 180, e.nums("sb"), e.nums("sl"), e.num("v"), e.num("len"), e.num("r0"), e.num("eR"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20shock = L20K("L20shock", e => () =>
		hazards.Add(L20Shock(e.num("x", W / 2), e.num("y", H / 2), e.num("ta"), e.num("v"), e.num("th"), e.num("ga") * Math.PI / 180, e.num("gw") * Math.PI / 180, e.num("r0"), e.num("wn"))));
	static readonly int _k20rain = L20K("L20rain", e => () => L20Add(e, L20Rain(e.nums("pb"), e.nums("pc"), e.nums("ps"), e.nums("pr"), e.num("pst"), e.num("pw"), e.num("v0"))));
	static readonly int _k20row = L20K("L20row", e => () =>
		L20Add(e, L20Row(e.i("n"), e.num("sp"), e.num("y0"), e.num("sw"), e.nums("lb"), e.nums("lc"), e.num("lw"), e.num("lh"), e.num("wn"), e.num("ta"), e.num("te"))));
	static readonly int _k20end = L20K("L20end", e => () => hazards.Add(L20End(e.num("ta"), e.num("te"))));
}
