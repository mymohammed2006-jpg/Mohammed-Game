using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 304-470: the temple (Stone, Spikes, Guardian) and buildLevel2 (Temple)
// SqBullet, Sweep, GapWall, Snake are in Shared.cs
public static partial class G
{
	public static Hz Stone(double x, double y, double s, double tw, double th, double stay, bool frag, double fsp)
	{
		bool l = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!l && t >= th) { l = true; shake = Math.Max(shake, 6); burst(x, y, PINK, 10, 250); }
			if (t >= th + stay) { if (frag) { for (int i = 0; i < 4; i++) { double a = Math.PI / 4 + i * Math.PI / 2; hazards.Add(SqBullet(x, y, JS.cos(a) * fsp, JS.sin(a) * fsp, 9)); } } h.dead = true; }
		};
		h.draw = t =>
		{
			if (t < th) { double k = clamp((t - tw) / (th - tw), 0, 1), a = s * (1.7 - 0.7 * k); g.fillStyle = $"rgba(255,42,109,{str(0.07 + 0.17 * k)})"; g.fillRect(x - a / 2, y - a / 2, a, a); g.strokeStyle = "rgba(255,42,109,0.85)"; g.lineWidth = 2; g.strokeRect(x - s / 2, y - s / 2, s, s); }
			else
			{
				double k = clamp((t - th) / 0.08, 0, 1); g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = k < 1 ? PINKW : PINK; g.fillRect(x - s / 2, y - s / 2, s, s); g.restore();
				g.strokeStyle = "#000"; g.lineWidth = 3; g.beginPath(); g.moveTo(x - s * 0.3, y - s * 0.2); g.lineTo(x, y + s * 0.05); g.lineTo(x + s * 0.25, y - s * 0.3); g.moveTo(x, y + s * 0.05); g.lineTo(x + s * 0.05, y + s * 0.35); g.stroke();
			}
		};
		h.hit = (p, t) => t >= th && t < th + stay && Math.Abs(p.x - x) < s / 2 + p.r && Math.Abs(p.y - y) < s / 2 + p.r;
		return h;
	}

	public static Hz Spikes(string side, double depth, double tw, double ta, double te)
	{
		bool hz = side == "T" || side == "B"; int n = hz ? 16 : 9; double step = (hz ? W : H) / n;
		Func<double, double> ext = t => t < te ? clamp((t - ta) / 0.06, 0, 1) : clamp(1 - (t - te) / 0.2, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			g.save(); if (side == "B") { g.translate(0, H); g.scale(1, -1); }
			if (side == "R") { g.translate(W, 0); g.scale(-1, 1); }
			double d = t < ta ? depth : depth * ext(t);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.08 + 0.2 * k)})"; } else { g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; }
			g.beginPath(); for (int i = 0; i < n; i++) { double a = i * step, m = a + step / 2, b = a + step; if (hz) { g.moveTo(a, 0); g.lineTo(m, d); g.lineTo(b, 0); } else { g.moveTo(0, a); g.lineTo(d, m); g.lineTo(0, b); } }
			g.fill();
			if (hz) g.fillRect(0, 0, W, 6); else g.fillRect(0, 0, 6, H);
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double d = depth * 0.75; if (side == "T") return p.y - p.r < d; if (side == "B") return p.y + p.r > H - d; if (side == "L") return p.x - p.r < d; return p.x + p.r > W - d; };
		return h;
	}

	// the temple guardian: moves around the arena, fires squares, throws stones and sweeps beams
	public static Hz Guardian(double ts, double te)
	{
		var AN = new[] { new[] { W / 2, H / 2 }, new[] { W * 0.27, H * 0.3 }, new[] { W * 0.73, H * 0.3 }, new[] { W * 0.73, H * 0.7 }, new[] { W * 0.27, H * 0.7 } };
		int lastS = -1; bool ex = false; double[] from = { W / 2, -200 }, to = { W / 2, H / 2 }; double mt0 = ts, mt1 = ts + 2 * SPB; int ai = 0; double x = W / 2, y = -200, eye = 0;
		void sq(int n, double bs, double sp, double s = 11) { for (int i = 0; i < n; i++) { double a = bs + i * TAU / n; hazards.Add(SqBullet(x + JS.cos(a) * 70, y + JS.sin(a) * 70, JS.cos(a) * sp, JS.sin(a) * sp, s)); } }
		void aim(double sp, double spread = 0, int cnt = 1)
		{
			if (player == null) return; double a0 = JS.atan2(player.y - y, player.x - x);
			for (int k = 0; k < cnt; k++) { double a = a0 + (k - (cnt - 1) / 2.0) * spread; hazards.Add(SqBullet(x + JS.cos(a) * 70, y + JS.sin(a) * 70, JS.cos(a) * sp, JS.sin(a) * sp, 11)); }
		}
		void stoneAt(double t, double px, double py, double s) { px = clamp(px, s / 2, W - s / 2); py = clamp(py, s / 2, H - s / 2); hazards.Add(Stone(px, py, s, t, t + SPB * 1.4, SPB * 0.5, true, 240)); }
		void sweep(double t, double a0, double a1) => hazards.Add(Sweep(x, y, a0, a1, 26, t, t + SPB * 1.375, t + SPB * 2.375));
		var h = new Hz();
		h.update = (t, dt) =>
		{
			double k = clamp((t - mt0) / (mt1 - mt0), 0, 1), e = k < 0.5 ? 2 * k * k : 1 - JS.pow(-2 * k + 2, 2) / 2; x = from[0] + (to[0] - from[0]) * e; y = from[1] + (to[1] - from[1]) * e;
			if (player != null) eye = JS.atan2(player.y - y, player.x - x);
			if (t >= te && !ex) { ex = true; sq(24, 0, 230); burst(x, y, PINK, 70, 520); burst(x, y, "#fff", 35, 420); shake = 26; flash = 0.7; rumble(500, 1, 1); h.dead = true; return; }
			if (t < ts + 2 * SPB) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s;
			int bar = (int)Math.Floor(s / 16.0), p = s % 16;
			if (p == 8 && bar % 2 == 1 && bar < 50) { int ni; do { ni = (int)Math.Floor(rnd() * 5); } while (ni == ai); from = new[] { x, y }; ai = ni; to = AN[ni]; mt0 = t; mt1 = t + SPB * 0.75; }
			if (bar >= 50) { if (p % 4 == 0) sq(5, s * 0.4, 210); if (p == 0 || p == 8) aim(275, 0.22, 3); return; }
			if (bar < 28)
			{
				if (p == 0) sq(8, bar * 0.3, 230);
				else if (p == 3 || p == 6) aim(290, 0.22, 2);
				else if (p == 10 && player != null) stoneAt(t, player.x, player.y, 100);
				else if (p == 13) sq(6, bar * 0.3 + 0.5, 250);
			}
			else if (bar < 38)
			{
				if (p == 0) sweep(t, eye - 1.1, eye + 1.1);
				else if (p == 4 || p == 6) aim(300);
				else if (p == 10) sq(6, s * 0.3, 220);
				else if (p == 12 && player != null) stoneAt(t, player.x, player.y, 90);
			}
			else
			{
				if (p == 0) sq(7, bar * 0.37, 215);
				else if (p == 4) aim(280, 0.25, 3);
				else if (p == 10 && bar % 2 == 0) sweep(t, eye + 1.2, eye - 1.2);
				else if (p == 14 && bar % 2 == 1) sq(5, -s * 0.3, 240);
				else if (p == 12 && bar % 4 == 1 && player != null) stoneAt(t, player.x + (rnd() * 2 - 1) * 120, player.y + (rnd() * 2 - 1) * 120, 100);
			}
		};
		h.draw = t =>
		{
			double sc = 1 + 0.1 * pulse; g.save(); g.translate(x, y);
			g.save(); g.rotate(t * 0.6); g.shadowColor = PINK; g.shadowBlur = 34; g.fillStyle = PINK; g.fillRect(-60 * sc, -60 * sc, 120 * sc, 120 * sc); g.shadowBlur = 0; g.fillStyle = "#8f1a43"; g.fillRect(-42 * sc, -42 * sc, 84 * sc, 84 * sc); g.restore();   /* 10-25: the middle hurts too, so dark pink, not black */
			g.save(); g.rotate(Math.PI / 4); g.fillStyle = PINK; g.fillRect(-22, -22, 44, 44); g.restore();
			g.fillStyle = "#000"; circle(JS.cos(eye) * 9, JS.sin(eye) * 9, 9);
			g.strokeStyle = PINK; g.globalAlpha = 0.5; g.lineWidth = 2; g.save(); g.rotate(-t * 0.9); for (int i = 0; i < 4; i++) { g.rotate(TAU / 4); g.strokeRect(84, -8, 16, 16); } g.restore();
			g.restore();
		};
		h.hit = (p, t) => { if (t < ts + 2 * SPB) return false; return hypot(p.x - x, p.y - y) < 58 + p.r; };
		return h;
	}

	/* 120 bpm. song parts in beats:
	   A 0-47 | B 48-79 | C 80-111 (strongest) | D 112-151 | E 152-207 (cuts at 208) */
	public static List<Ev> buildLevel2()
	{
		var E = new List<Ev>(); var R = mulberry32(77);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 120, double b = W - 120) => a + R() * (b - a);
		double ry(double a = 100, double b = H - 100) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 270, wn = 1.5, gap = 220 }, new Tier { sp = 300, wn = 1.375, gap = 195 }, new Tier { sp = 315, wn = 1.375, gap = 190 } });
		var C = new[] { new[] { 0.0, 0.0 }, new[] { W, 0.0 }, new[] { W, H }, new[] { 0.0, H } };
		void stone(double hit, double x, double y, double s, double stay, bool frag, Tier T) => ev(hit - T.wn, () => hazards.Add(Stone(x, y, s, B(hit - T.wn), B(hit), stay * SPB, frag, T.sp * 0.8)));
		void stoneP(double hit, double s, Tier T) => ev(hit - T.wn, () => { double x = clamp(player != null ? player.x : rx(), s / 2, W - s / 2), y = clamp(player != null ? player.y : ry(), s / 2, H - s / 2); hazards.Add(Stone(x, y, s, B(hit - T.wn), B(hit), 0.5 * SPB, true, T.sp * 0.8)); });
		void sqs(double b, double x, double y, double vx, double vy) => ev(b, () => hazards.Add(SqBullet(x, y, vx, vy)));
		void aimSq(double b, int ci, double sp) => ev(b, () => { if (player == null) return; double x = C[ci % 4][0], y = C[ci % 4][1]; double a = JS.atan2(player.y - y, player.x - x); hazards.Add(SqBullet(x, y, JS.cos(a) * sp, JS.sin(a) * sp)); });
		void burstAt(double b, double x, double y, int n, double sp) => ev(b, () => { double o = rnd() * TAU; for (int i = 0; i < n; i++) { double a = o + i * TAU / n; hazards.Add(SqBullet(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } burst(x, y, PINK, 8); });
		void sweep(double hit, double px, double py, double a0, double a1, double dur, Tier T, double w = 28) => ev(hit - T.wn, () => hazards.Add(Sweep(px, py, a0, a1, w, B(hit - T.wn), B(hit), B(hit + dur))));
		void wall(double hit, string dir, Tier T, double sp = 0) { bool vert = dir == "L" || dir == "R"; double gc = vert ? ry(130, H - 130) : rx(170, W - 170); ev(hit - 1, () => hazards.Add(GapWall(dir, gc, T.gap, 40, sp != 0 ? sp : (vert ? 430 : 300), B(hit - 1), B(hit)))); }
		void snake(double b, bool fromLeft, Tier T, double dur = 4, double amp = 170)
		{
			double y0 = ry(200, H - 200), ph = R() * TAU, ln = R() < 0.5 ? 2 : 3;
			Func<double, (double x, double y)> path = u => (fromLeft ? -60 + u * (W + 120) : W + 60 - u * (W + 120), y0 + JS.sin(u * Math.PI * ln + ph) * amp);
			ev(b - 1, () => hazards.Add(Snake(path, B(b), B(b + dur), 10, 0.035, 13, B(b - 1))));
		}
		void spikes(double hit, string side, Tier T, double dur = 0.5, double depth = 90) => ev(hit - T.wn, () => hazards.Add(Spikes(side, depth, B(hit - T.wn), B(hit), B(hit + dur))));
		void col(double hit, double x, Tier T, double dur = 0.5) => ev(hit - T.wn, () => hazards.Add(Laser(new LaserO { x = x, y = H / 2, ang = 0, len = 140, w = H + 10, tile = true, tw = B(hit - T.wn), ta = B(hit), te = B(hit + dur) })));
		var SW = new[] { new[] { 0, Math.PI / 2 }, new[] { Math.PI, Math.PI / 2 }, new[] { Math.PI, 1.5 * Math.PI }, new[] { TAU, 1.5 * Math.PI } };

		// part A: stones, squares, walls and corner beams
		void secA(int d)
		{
			var T = TI[d];
			for (int k = 0; k < 4; k++)
			{
				double b = k * 4; bool fr = k == 0 || k % 2 == 1;
				if (k > 0) stone(b, rx(), ry(), 90, 0.5, d >= 1, T);
				stone(b + 2, rx(), ry(), 90, 0.5, d >= 1, T);
				sqs(b + 0.75, fr ? W + 20 : -20, ry(60, H - 60), (fr ? -1 : 1) * T.sp, 0);
				sqs(b + 1.5, fr ? W + 20 : -20, ry(60, H - 60), (fr ? -1 : 1) * T.sp, 0);
				if (d >= 1) sqs(b + 3.25, rx(), -20, 0, T.sp);
			}
			for (int k = 4; k < 8; k++) { double b = k * 4; wall(b, k % 2 != 0 ? "L" : "R", T); stone(b + 2, rx(), ry(), 90, 0.5, d >= 1, T); stone(b + 3.25, rx(), ry(), 80, 0.5, false, T); if (d >= 1) aimSq(b + 1.5, k, T.sp); }
			for (int k = 8; k < 12; k++)
			{
				double b = k * 4; int c = k % 4;
				if (d >= 1 || k % 2 == 0) sweep(b, C[c][0], C[c][1], SW[c][0], SW[c][1], 2, T);
				var o = C[(c + 2) % 4]; double ag = JS.atan2(H / 2 - o[1], W / 2 - o[0]); for (int i = 0; i < 3; i++) { double a = ag + (i - 1) * 0.2; sqs(b + 1.5, o[0], o[1], JS.cos(a) * T.sp, JS.sin(a) * T.sp); }
				burstAt(b + 3.25, rx(), ry(), 6, T.sp * 0.8);
			}
		}
		// part B: snakes and spikes
		void secB(int d, bool boss)
		{
			var T = TI[d];
			for (int k = 12; k < 20; k++)
			{
				double b = k * 4;
				if (k % 2 == 0 || (d >= 1 && !boss)) snake(b, k % 2 == 0, T, 4, 170);
				spikes(b + 1.5, k % 2 != 0 ? "T" : "B", T);
				if (d >= 1) spikes(b + 3, k % 2 != 0 ? "L" : "R", T, 0.5, 80);
				aimSq(b + 0.75, k, T.sp); if (d >= 1) aimSq(b + 2.5, k + 2, T.sp);
			}
		}
		// part C: the temple collapses (strongest part)
		void secC(int d, bool boss)
		{
			var T = TI[d];
			for (int k = 20; k < 28; k++)
			{
				double b = k * 4;
				if (boss) { if (k % 2 == 0) wall(b, new[] { "L", "R", "D", "U" }[(k / 2) % 4], T); else col(b + 2, rx(100, W - 100), T); stone(b + 3.25, rx(), ry(), 80, 0.4, false, T); continue; }
				stoneP(b, 100, T); stone(b + 0.75, rx(), ry(), 80, 0.4, true, T); stone(b + 1.5, rx(), ry(), 80, 0.4, k % 2 == 1, T);
				if (k % 2 == 0) wall(b + 2, k % 4 == 0 ? "L" : "R", T); else col(b + 2, rx(100, W - 100), T);
				burstAt(b + 3.25, rx(), ry(), k % 2 != 0 ? 6 : 8, T.sp * 0.8);
			}
		}
		// part D: clock hands and spikes
		void secD(int d, bool boss)
		{
			var T = TI[d];
			for (int k = 28; k < 38; k++)
			{
				double b = k * 4;
				if (boss) { spikes(b + 2.5, new[] { "T", "R", "B", "L" }[k % 4], T); if (k % 2 != 0) aimSq(b + 1.5, k, T.sp); continue; }
				if (k % 2 == 0) { double a = R() * TAU; sweep(b, W / 2, H / 2, a, a + Math.PI, 4, T); if (k >= 32) sweep(b, W / 2, H / 2, a + Math.PI, a + TAU, 4, T); }
				aimSq(b + 1, k, T.sp); aimSq(b + 1.5, k + 1, T.sp);
				spikes(b + 2.5, new[] { "T", "R", "B", "L" }[k % 4], T);
				stone(b + 3, rx(), ry(), 80, 0.4, false, T);
			}
		}
		// part E: always with the boss
		void secE(int d)
		{
			var T = TI[d];
			for (int k = 38; k < 50; k++)
			{
				double b = k * 4;
				if (k % 4 == 0) wall(b + 2, new[] { "L", "D", "R", "U" }[(k / 4) % 4], T);
				if (k % 4 == 1) snake(b, k % 8 == 1, T, 4, 150);
				if (k % 4 == 3) spikes(b + 2.5, new[] { "T", "B" }[(int)Math.Floor(k / 4.0) % 2], T);
			}
		}
		void boss(double b) => ev(b, () => hazards.Add(Guardian(B(b), B(208))));
		secA(0); secB(0, false); secC(1, false); secD(1, false); boss(152); secE(2);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
