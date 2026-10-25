using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 1442-1615: MEGALOVANIA's shapes (CountTile, Memory, Cannon, DashWall, Seeker, the Nightmare boss) and buildFriend4
// (GC/GR/GW/GH from line 1443 live in Shared.cs)
public static partial class G
{
	// a tile with a counter: its number goes down every beat, and it strikes at zero
	public static Hz CountTile(double i, double j, double tw, double ta, double te)
	{
		double x = i * GW, y = j * GH;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double n = Math.Max(1, Math.Ceiling((ta - t) / SPB - 1e-6)), k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.14 * k)})"; g.fillRect(x + 4, y + 4, GW - 8, GH - 8); g.strokeStyle = "rgba(255,42,109,0.75)"; g.lineWidth = 2; g.strokeRect(x + 10, y + 10, GW - 20, GH - 20);
				g.font = $"800 {(n == 1 ? 70 : 56)}px {FONT}"; g.fillStyle = n == 1 ? "rgba(255,42,109,0.95)" : "rgba(255,42,109,0.6)"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(ar(n), x + GW / 2, y + GH / 2 + 4);
			}
			else { double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25); g.globalAlpha = a; g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; g.fillRect(x + 3, y + 3, GW - 6, GH - 6); g.restore(); g.globalAlpha = 1; }
		};
		h.hit = (p, t) => t >= ta && t < te && Math.Abs(p.x - (x + GW / 2)) < GW / 2 - 3 + p.r && Math.Abs(p.y - (y + GH / 2)) < GH / 2 - 3 + p.r;
		return h;
	}

	// memory: tiles flash in order, then strike in the same order, fast
	public static Hz Memory(double[][] cells, double show0, double showStep, double strike0, double strikeStep)
	{
		double dur = Math.Max(0.12, strikeStep * 0.75);
		var h = new Hz();
		h.update = (t, dt) => { if (t > strike0 + cells.Length * strikeStep + 0.5) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < cells.Length; k++)
			{
				double i = cells[k][0], j = cells[k][1], x = i * GW, y = j * GH, s0 = show0 + k * showStep, a0 = strike0 + k * strikeStep;
				if (t >= s0 && t < s0 + showStep * 0.85) { double q = 1 - (t - s0) / (showStep * 0.85); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.3 * q)})"; g.fillRect(x + 4, y + 4, GW - 8, GH - 8); g.font = $"800 52px {FONT}"; g.fillStyle = "rgba(255,255,255,0.85)"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(ar(k + 1), x + GW / 2, y + GH / 2 + 4); }
				if (t >= strike0 - 0.6 && t < a0) { g.strokeStyle = "rgba(255,42,109,0.18)"; g.lineWidth = 2; g.strokeRect(x + 12, y + 12, GW - 24, GH - 24); }
				if (t >= a0 - 0.1 && t < a0) { g.strokeStyle = "rgba(255,42,109,0.9)"; g.lineWidth = 4; g.strokeRect(x + 8, y + 8, GW - 16, GH - 16); }
				if (t >= a0 && t < a0 + dur + 0.2) { double a = t < a0 + dur ? 1 : 1 - (t - a0 - dur) / 0.2; g.globalAlpha = a; g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - a0) < 0.06 ? PINKW : PINK; g.fillRect(x + 3, y + 3, GW - 6, GH - 6); g.restore(); g.globalAlpha = 1; }
			}
		};
		h.hit = (p, t) =>
		{
			for (int k = 0; k < cells.Length; k++) { double a0 = strike0 + k * strikeStep; if (t >= a0 && t < a0 + dur) { double i = cells[k][0], j = cells[k][1], x = i * GW, y = j * GH; if (Math.Abs(p.x - (x + GW / 2)) < GW / 2 - 3 + p.r && Math.Abs(p.y - (y + GH / 2)) < GH / 2 - 3 + p.r) return true; } }
			return false;
		};
		return h;
	}

	// a cannon: charges and fires a wide beam
	public static Hz Cannon(double x, double y, double ang, double tw, double ta, double te, double bw = 46)
	{
		const double len = 2400;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1); bool act = t >= ta && t < te; double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.save(); g.translate(x, y); g.rotate(ang); g.globalAlpha = a;
			if (t < ta) { g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(30, -bw / 2, len, bw); g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.fillRect(30, -1.5, len, 3); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; g.beginPath(); g.arc(36, 0, 10 + 30 * (1 - k), 0, TAU); g.stroke(); }
			else if (act) { double fl = Math.Max(0, 1 - (t - ta) / 0.12), ww = bw * (1 + 0.12 * pulse); g.shadowColor = PINK; g.shadowBlur = 26; g.fillStyle = PINK; g.fillRect(30, -ww / 2, len, ww); if (fl > 0) { g.globalAlpha = fl; g.fillStyle = PINKW; g.fillRect(30, -ww / 2, len, ww); g.globalAlpha = 1; } }
			g.fillStyle = act ? PINK : $"rgba(255,42,109,{str(0.45 + 0.4 * k)})"; g.beginPath(); g.moveTo(42, 0); g.lineTo(0, -24); g.lineTo(-16, -12); g.lineTo(-16, 12); g.lineTo(0, 24); g.closePath(); g.fill(); g.fillStyle = "#000"; g.fillRect(-6, -5, 14, 10);
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; return obbHit(p.x, p.y, p.r, x + JS.cos(ang) * (30 + len / 2), y + JS.sin(ang) * (30 + len / 2), len / 2, bw / 2, ang) || hypot(p.x - x, p.y - y) < 24 + p.r; };
		return h;
	}

	// a full wall you can only pass by dashing on the beat
	public static Hz DashWall(string dir, double tw, double ta, double sp, double th = 26, bool tag = false)
	{
		bool vert = dir == "L" || dir == "R"; double sg = (dir == "L" || dir == "U") ? -1 : 1, ext = vert ? W : H, st = sg < 0 ? ext + th : -th; Func<double, double> pos = t => st + sg * sp * (t - ta);
		var h = new Hz();
		h.update = (t, dt) => { double p = pos(t); if (t > ta && (p < -th * 2 || p > ext + th * 2)) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; const int n = 8;
				for (int i = 0; i < n; i++)
				{
					double c = (i + 0.5) / n * (vert ? H : W), o = ((t * 3) % 1) * 30; g.save(); if (vert) { g.translate(sg < 0 ? W - 50 - o : 50 + o, c); if (sg < 0) g.scale(-1, 1); } else { g.translate(c, sg < 0 ? H - 50 - o : 50 + o); g.rotate(sg < 0 ? -Math.PI / 2 : Math.PI / 2); }
					g.beginPath(); g.moveTo(-10, -14); g.lineTo(6, 0); g.lineTo(-10, 14); g.lineTo(-4, 0); g.closePath(); g.fill(); g.translate(16, 0); g.beginPath(); g.moveTo(-10, -14); g.lineTo(6, 0); g.lineTo(-10, 14); g.lineTo(-4, 0); g.closePath(); g.fill(); g.restore();
				}
				if (tag) { g.globalAlpha = 0.4 + 0.5 * k; txt("اندفع!", W / 2, H / 2 - 150, 44, PINK); g.globalAlpha = 1; }
				return;
			}
			double pp = pos(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = PINK; if (vert) g.fillRect(pp - th / 2, 0, th, H); else g.fillRect(0, pp - th / 2, W, th); g.restore();
		};
		h.hit = (pl, t) => { if (t < ta) return false; double p = pos(t); return vert ? Math.Abs(pl.x - p) < th / 2 + pl.r : Math.Abs(pl.y - p) < th / 2 + pl.r; };
		return h;
	}

	// a seeker: a square that follows you slowly, then explodes
	public static Hz Seeker(double x, double y, double tw, double t0, double t1, double sp, double turn)
	{
		double vx = 0, vy = 0, rot = 0;
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) =>
		{
			if (t < t0) return; if (t >= t1) { for (int i = 0; i < 4; i++) { double a = i * Math.PI / 2 + Math.PI / 4; hazards.Add(Bullet(h.x, h.y, JS.cos(a) * 240, JS.sin(a) * 240, 8)); } burst(h.x, h.y, PINK, 12); h.dead = true; return; }
			rot += dt * 4; if (player != null) { double a = JS.atan2(player.y - h.y, player.x - h.x), ca = JS.atan2(vy, vx); double da = a - ca; da = JS.atan2(JS.sin(da), JS.cos(da)); double na = (vx != 0 || vy != 0) ? ca + clamp(da, -turn * dt, turn * dt) : a; vx = JS.cos(na) * sp; vy = JS.sin(na) * sp; }
			h.x = clamp(h.x + vx * dt, 0, W); h.y = clamp(h.y + vy * dt, 0, H);
		};
		h.draw = t =>
		{
			g.save(); g.translate(h.x, h.y); g.rotate(rot);
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.6 * k)})"; g.lineWidth = 3; double s = 30 - 14 * k; g.strokeRect(-s, -s, 2 * s, 2 * s); }
			else { bool bl = (t1 - t) < 0.5 && Math.Floor(t * 14) % 2 == 0; g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = bl ? PINKW : PINK; g.fillRect(-16, -16, 32, 32); g.fillStyle = "#000"; g.fillRect(-6, -6, 12, 12); }
			g.restore();
		};
		h.hit = (p, t) => t >= t0 && Math.Abs(p.x - h.x) < 16 + p.r && Math.Abs(p.y - h.y) < 16 + p.r;
		return h;
	}

	// ===== the boss: the Nightmare, a sharp mask floating at the top that runs the attacks =====
	public static Hz Nightmare(double ts, double te, Tier T)
	{
		double x = W / 2, y = -150; int lastS = -1; bool ex = false; const double Y = 110;
		void cannonAt(double t, double cx, double cy, double ang, double bw = 44) => hazards.Add(Cannon(cx, cy, ang, t, t + SPB * T.wn, t + SPB * (T.wn + 0.6), bw));
		var h = new Hz();
		h.update = (t, dt) =>
		{
			double k = clamp((t - ts) / (SPB * 3), 0, 1); x = W / 2 + JS.sin((t - ts) * 0.6) * 260 * k; y = -150 + (Y + 150) * (1 - JS.pow(1 - k, 3));
			if (t >= te && !ex) { ex = true; for (int i = 0; i < 30; i++) { double a = i * TAU / 30; hazards.Add(Bullet(x, y, JS.cos(a) * 230, JS.sin(a) * 230, 8)); } burst(x, y, PINK, 90, 580); burst(x, y, "#fff", 45, 480); shake = 30; flash = 0.8; rumble(700, 1, 1); h.dead = true; return; }
			if (t < ts + SPB * 4) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s; double b = s / 4.0; int p = s % 16; double bar = Math.Floor(s / 16.0); if (t > te - SPB * 2) return;
			if (b < 224)
			{   // edge cannons fire in turn, clockwise
				if (p % 4 == 0) { int q = (int)((s / 4.0) % 4); var C = new[] { new[] { 30, H * 0.5, 0 }, new[] { W * 0.5, H - 30, -Math.PI / 2 }, new[] { W - 30, H * 0.5, Math.PI }, new[] { 30, H * 0.2, 0.25 } }; var c = C[q]; if (q == 3) cannonAt(t, x, y + 40, Math.PI / 2, 50); else cannonAt(t, c[0], c[1], c[2]); }
			}
			else if (b < 256)
			{   // cracks in the mask: slanted lasers from the mask, taking turns
				if (p % 4 == 0) { double a = Math.PI / 2 + (((p / 4.0) % 2) != 0 ? 0.55 : -0.55) * ((bar % 2) != 0 ? 1 : -1); hazards.Add(Cannon(x, y + 30, a, t, t + SPB * T.wn, t + SPB * (T.wn + 0.5), 30)); }
			}
			else
			{   // the end: a beam from the mask at your spot every two beats
				if (p % 8 == 0 && player != null) { double a = JS.atan2(player.y - (y + 30), player.x - x); cannonAt(t, x, y + 30, a, 40); }
			}
		};
		h.draw = t =>
		{
			double k = clamp((t - ts) / (SPB * 3), 0, 1); g.save(); g.translate(x, y); double s = 1 + 0.06 * pulse; g.scale(s, s);
			g.shadowColor = PINK; g.shadowBlur = 34; g.fillStyle = PINK; g.beginPath(); g.moveTo(0, -80); g.lineTo(58, -20); g.lineTo(46, 50); g.lineTo(0, 82); g.lineTo(-46, 50); g.lineTo(-58, -20); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "#000"; g.beginPath(); g.moveTo(-40, -14); g.lineTo(-10, -4); g.lineTo(-36, 8); g.closePath(); g.fill(); g.beginPath(); g.moveTo(40, -14); g.lineTo(10, -4); g.lineTo(36, 8); g.closePath(); g.fill();
			g.beginPath(); g.moveTo(-34, 30); for (int i = 0; i <= 8; i++) g.lineTo(-34 + i * 8.5, i % 2 != 0 ? 42 : 30); g.lineTo(34, 36); for (int i = 8; i >= 0; i--) g.lineTo(-34 + i * 8.5, i % 2 != 0 ? 48 : 40); g.closePath(); g.fill();
			g.restore();
		};
		h.hit = (p, t) => { if (t < ts + SPB * 3) return false; return hypot(p.x - x, p.y - y) < 58 + p.r; };
		return h;
	}

	/* MEGALOVANIA (picked by Nightmare): 120 bpm, the whole song. Everything is planned and ordered, with a few touches that chase you.
	   0 counter + mirrors | 32 the corridor | 48 mirrors + counter | 64 cannons | 80 memory | 94 first dash
	   96 save -> the box | 112 counter waves | 128 fast corridor + cannons | 144 dash walls | 160 long memory | 176 seekers
	   192 save -> the Nightmare | 288 it loses | 288-310 the ending */
	public static List<Ev> buildFriend4()
	{
		var E = new List<Ev>();
		void ev(double b, Action f) => E.Add(new Ev { b = b, f = f });
		var TI = ezT(new[] { new Tier { sp = 320, wn = 1.6, gap = 180 }, new Tier { sp = 330, wn = 1.6, gap = 180 }, new Tier { sp = 350, wn = 1.5, gap = 170 } });
		void tile(double start, double i, double j, double cnt, Tier T, double dur = 0.6) => ev(start, () => hazards.Add(CountTile(i, j, B(start), B(start + cnt), B(start + cnt + dur))));
		void mem(double[][] cells, double s0, double sStep, double k0, double kStep) => ev(s0, () => hazards.Add(Memory(cells, B(s0), sStep * SPB, B(k0), kStep * SPB)));
		void L(double hit, double warn, double dur, LaserO o) => ev(hit - warn, () => { var q = o.Clone(); q.tw = B(hit - warn); q.ta = B(hit); q.te = B(hit + dur); hazards.Add(Laser(q)); });
		void vL(double hit, double x, Tier T, double w = 50, double dur = 0.6) => L(hit, T.wn, dur, new LaserO { x = x, y = H / 2, ang = Math.PI / 2, w = w });
		void hL(double hit, double y, Tier T, double w = 50, double dur = 0.6) => L(hit, T.wn, dur, new LaserO { x = W / 2, y = y, ang = 0, w = w });
		void mirV(double hit, double x, Tier T, double w) { vL(hit, x, T, w); vL(hit + 1, W - x, T, w); }
		void mirH(double hit, double y, Tier T, double w) { hL(hit, y, T, w); hL(hit + 1, H - y, T, w); }
		void wall(double hit, double gc, Tier T, double sp = 420, string dir = "L") => ev(hit - 1, () => hazards.Add(GapWall(dir, gc, T.gap, 36, sp, B(hit - 1), B(hit))));
		void cannon(double hit, double x, double y, double ang, Tier T, double dur = 0.6, double bw = 46) => ev(hit - T.wn, () => hazards.Add(Cannon(x, y, ang, B(hit - T.wn), B(hit), B(hit + dur), bw)));
		void cannonP(double hit, double x, double y, Tier T) => ev(hit - T.wn, () => { double a = player != null ? JS.atan2(player.y - y, player.x - x) : JS.atan2(H / 2 - y, W / 2 - x); hazards.Add(Cannon(x, y, a, B(hit - T.wn), B(hit), B(hit + 0.6), 44)); });
		void dash(double hit, string dir, Tier T, double warn = 3, bool tag = false, double sp = 950) => ev(hit - warn, () => hazards.Add(DashWall(dir, B(hit - warn), B(hit), sp, 26, tag)));
		void box(double hit, double w, double h, Tier T, double dur) => ev(hit - T.wn, () => hazards.Add(Squeeze(W / 2 - w / 2, H / 2 - h / 2, W / 2 + w / 2, H / 2 + h / 2, B(hit - T.wn), B(hit), B(hit + dur))));
		void seeker(double hit, double x, double y, Tier T, double life = 6) => ev(hit - 1, () => hazards.Add(Seeker(x, y, B(hit - 1), B(hit), B(hit + life), T.sp * 0.5, 2.2)));
		var CR = new[] { new[] { 30, 30, Math.PI / 4 }, new[] { W - 30, 30, Math.PI * 3 / 4 }, new[] { W - 30, H - 30, -Math.PI * 3 / 4 }, new[] { 30, H - 30, -Math.PI / 4 } };
		double[][] C(params double[] v) { var r = new double[v.Length / 2][]; for (int q = 0; q < r.Length; q++) r[q] = new[] { v[q * 2], v[q * 2 + 1] }; return r; }

		/* ---------- part 1 ---------- */
		var T = TI[0];
		// 0:00 the intro: teaching the counter and the mirrors
		tile(6, 2, 1, 3, T); tile(12, 5, 2, 3, T); tile(18, 1, 2, 4, T); tile(18, 6, 1, 2, T); tile(24, 3, 0, 3, T); tile(24, 4, 3, 3, T); tile(26, 0, 0, 2, T); tile(26, 7, 3, 2, T);
		mirV(28, W * 0.3, T, 60); mirH(30, H * 0.3, T, 60);
		// 0:16 the corridor: walls with gaps draw a winding path
		for (int k = 0; k < 11; k++) wall(32 + k * 1.5, H / 2 + JS.sin(k * 0.65) * 230, T, 440);
		// 0:24 mirrors with the counter
		var XS = new[] { 0.12, 0.26, 0.4, 0.2, 0.34, 0.08, 0.3, 0.44 }; for (int k = 0; k < XS.Length; k++) mirV(48 + k * 2, W * XS[k], T, 56);
		foreach (var q in new[] { new[] { 49.0, 3, 1, 3 }, new[] { 51.0, 4, 2, 3 }, new[] { 53.0, 0, 3, 3 }, new[] { 55.0, 7, 0, 3 }, new[] { 57.0, 2, 2, 4 }, new[] { 59.0, 5, 1, 4 } }) tile(q[0], q[1], q[2], q[3], T);
		// 0:32 the cannons: from the corners in turn, then aimed at you
		{ var bs = new double[] { 64, 66, 68, 70 }; for (int k = 0; k < 4; k++) cannon(bs[k], CR[k][0], CR[k][1], CR[k][2], T); }
		{ var bs = new double[] { 72, 74, 76, 78 }; for (int k = 0; k < 4; k++) cannonP(bs[k], CR[(k + 2) % 4][0], CR[(k + 2) % 4][1], T); }
		hL(73, H / 2, T, 40); vL(77, W / 2, T, 40);
		// 0:40 memory: watch the order, then it strikes in the same order
		mem(C(1, 1, 2, 2, 5, 1, 6, 2), 80, 1, 85, 0.5);
		mem(C(3, 0, 4, 3, 0, 2, 7, 1, 3, 3), 88, 0.75, 92, 0.5);
		// 0:47 the first dash wall
		dash(95, "L", T, 3, true, 900);
		/* ---------- part 2 ---------- */
		T = TI[1];
		ev(96, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		// 0:48 the box: the arena shrinks, and lasers cut inside it in a pattern
		{
			double bw = 460, bh = 320, x0 = W / 2 - bw / 2, y0 = H / 2 - bh / 2; box(98, bw, bh, T, 13);
			var P = new (double b, string o, double v)[] { (100, "h", y0 + 70), (101, "h", y0 + bh - 70), (102, "v", x0 + 90), (103, "v", x0 + bw - 90), (104, "h", H / 2), (104.5, "v", W / 2), (106, "h", y0 + 60), (106.5, "h", y0 + bh - 60), (107, "v", x0 + bw / 2 - 110), (107.5, "v", x0 + bw / 2 + 110), (108, "h", H / 2 - 50), (108.5, "h", H / 2 + 50), (109, "v", W / 2) };
			foreach (var (b, o, v) in P) { if (o == "h") hL(b, v, T, 40, 0.45); else vL(b, v, T, 40, 0.45); }
		}
		// 0:56 counter waves: the numbers draw a wave moving across the arena
		for (int i = 0; i < GC; i++) for (int j = 0; j < GR; j++) { double c = 2 + ((i + j) % 4); tile(112, i, j, c, T, 0.5); }
		for (int i = 0; i < GC; i++) for (int j = 0; j < GR; j++) { double c = 2 + (((GC - 1 - i) + j) % 4); tile(119, i, j, c, T, 0.5); }
		// 1:04 the fast corridor with cannons aiming at you
		for (int k = 0; k < 12; k++) wall(128 + k * 1.25, H / 2 + JS.sin(k * 0.9 + 1) * 240, T, 560);
		for (double b = 130, k = 0; b < 144; b += 4, k++) cannonP(b, CR[(int)(k % 4)][0], CR[(int)(k % 4)][1], T);
		for (double b = 129; b < 160; b += 4) wall(b, W / 2 + JS.sin(b * 0.5) * 380, T, 320, b % 8 == 1 ? "U" : "D");
		// 1:12 dash walls from every side
		dash(145, "L", T, 2.5); dash(149, "R", T, 2.5); dash(153, "U", T, 2.5); dash(157, "D", T, 2.5);
		mirV(146, W * 0.2, T, 60); mirH(150, H * 0.25, T, 60); mirV(154, W * 0.35, T, 60);
		// 1:20 longer memory
		mem(C(0, 0, 2, 1, 4, 2, 6, 3, 7, 0), 160, 0.75, 165, 0.5);
		mem(C(7, 3, 5, 2, 3, 1, 1, 0, 0, 3, 4, 0), 168, 0.75, 173, 0.5);
		tile(176, 3, 1, 3, T); tile(176, 4, 2, 3, T);
		// 1:28 the seekers with mirrors
		seeker(178, 60, 60, T, 7); seeker(180, W - 60, H - 60, T, 7); seeker(182, W - 60, 60, T, 6);
		mirV(180, W * 0.15, T, 50); mirH(184, H * 0.2, T, 50); mirV(188, W * 0.3, T, 50);
		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		ev(192, () => hazards.Add(Nightmare(B(192), B(288), TT)));
		foreach (var q in new[] { new double[] { 200, 1, 1, 3 }, new double[] { 200, 6, 2, 3 }, new double[] { 204, 2, 3, 3 }, new double[] { 204, 5, 0, 3 }, new double[] { 208, 0, 2, 3 }, new double[] { 208, 7, 1, 3 }, new double[] { 212, 3, 2, 2 }, new double[] { 212, 4, 1, 2 } }) tile(q[0], q[1], q[2], q[3], T, 0.5);
		mem(C(1, 2, 3, 1, 5, 2, 7, 1, 6, 3, 2, 0), 224, 0.5, 228, 0.5);
		mem(C(0, 1, 2, 3, 4, 1, 6, 3, 5, 0, 3, 2, 1, 3), 232, 0.5, 236, 0.4);
		box(242, 500, 330, T, 10);
		for (int i = 0; i < GC; i++) for (int j = 0; j < GR; j++) tile(216, i, j, 2 + ((i + GR - 1 - j) % 4), T, 0.45);
		for (int i = 0; i < GC; i++) for (int j = 0; j < GR; j++) tile(264, i, j, 2 + (((GC - 1 - i) + (GR - 1 - j)) % 4), T, 0.45);
		foreach (var b in new double[] { 256, 264, 272, 280 }) dash(b + 1, new[] { "L", "R", "U", "D" }[(int)((b / 8) % 4)], T, 2.5);
		for (double b = 258; b < 286; b += 4) mirV(b, W * (0.1 + ((b / 4) % 4) * 0.08), T, 50);
		/* ---------- density layers (all planned, no randomness) ---------- */
		double seq(double n, double k) => ((k * 7 + 3) % n);
		// part 1: counters in mirrored pairs + lasers on every beat
		T = TI[0];
		for (double b = 34; b < 92; b += 2) { double i = seq(4, b / 2), j = seq(4, b / 2 + 1); tile(b, i, j, 3, T, 0.5); tile(b, GC - 1 - i, GR - 1 - j, 3, T, 0.5); }
		for (double b = 48; b < 64; b += 4) wall(b + 1, W / 2 + JS.sin(b * 0.5) * 380, T, 340, "U");
		for (double b = 33; b < 64; b += 2) { if (b % 4 == 1) vL(b, W * (0.1 + 0.8 * ((b * 5) % 11) / 10), T, 44, 0.4); else hL(b, H * (0.12 + 0.76 * ((b * 3) % 7) / 6), T, 44, 0.4); }
		for (double b = 80; b < 94; b += 1) cannonP(b + 0.5, CR[(int)(b % 4)][0], CR[(int)(b % 4)][1], T);
		for (int k = 0; k < 20; k++) wall(64 + k * 1.5, H / 2 + JS.sin(k * 0.6 + 2) * 230, T, 480, k % 2 != 0 ? "L" : "R");
		for (double b = 64; b < 80; b++) if (b % 4 == 1) vL(b + 0.5, W * (0.1 + 0.8 * ((b * 5) % 11) / 10), T, 44, 0.4);
		for (double b = 8; b < 30; b += 2) tile(b, (b * 3) % 8, (b / 2) % 4, 3, T, 0.5);
		for (double b = 9; b < 31; b += 2) if (b % 4 == 1) vL(b, W * (0.15 + 0.7 * ((b * 5) % 7) / 6), T, 44, 0.4); else hL(b, H * (0.15 + 0.7 * ((b * 3) % 5) / 4), T, 44, 0.4);
		// part 2
		T = TI[1];
		for (double b = 112; b < 128; b += 4) hL(b + 0.5, H * (0.1 + 0.8 * ((b * 3) % 7) / 6), T, 40, 0.4);
		for (double b = 112; b < 128; b += 4) wall(b, W / 2 + JS.sin(b * 0.4) * 380, T, 300, "U");
		for (double b = 160; b < 192; b += 3) wall(b, H / 2 + JS.sin(b * 0.5) * 230, T, 480);
		for (double b = 128; b < 144; b += 4) vL(b + 0.5, W * (0.1 + 0.8 * ((b * 5) % 11) / 10), T, 44, 0.4);
		for (double b = 146, k = 0; b < 160; b += 4, k++) cannonP(b + 0.5, CR[(int)(k % 4)][0], CR[(int)(k % 4)][1], T);
		for (double b = 160; b < 176; b += 4) { double i = seq(8, b), j = seq(4, b + 1); tile(b, i, j, 3, T, 0.5); tile(b, GC - 1 - i, j, 3, T, 0.5); }
		for (double b = 160; b < 176; b += 1) if (b % 8 == 1) hL(b + 0.5, H * (0.12 + 0.76 * ((b * 5) % 7) / 6), T, 40, 0.4);
		for (double b = 176; b < 192; b += 4) { if (b % 8 != 0) vL(b + 0.5, W * (0.1 + 0.8 * ((b * 5) % 11) / 10), T, 44, 0.4); else hL(b + 0.5, H * (0.12 + 0.76 * ((b * 3) % 7) / 6), T, 44, 0.4); }
		// part 3
		T = TI[2];
		for (double b = 196; b < 286; b += 4) { double i = seq(8, b / 4), j = seq(4, b / 4 + 2); tile(b, i, j, 3, T, 0.5); tile(b + 2, GC - 1 - i, GR - 1 - j, 3, T, 0.5); }
		for (double b = 194; b < 286; b += 4) { if (b % 2 != 0) vL(b + 0.5, W * (0.08 + 0.84 * ((b * 5) % 11) / 10), T, 44, 0.4); else hL(b + 0.5, H * (0.1 + 0.8 * ((b * 3) % 7) / 6), T, 44, 0.4); }
		for (double b = 200; b < 286; b += 4) cannonP(b + 1, CR[(int)((b / 4) % 4)][0], CR[(int)((b / 4) % 4)][1], T);
		ev(198, () => hazards.Add(Spinner(new SpinnerO { x = W / 2, y = H / 2 + 60, arms = 3, L = 760, w = 20, ts = B(198), te = B(286), w1 = 0.6, tsw = B(240), w2 = -0.75 })));
		for (double b = 200; b < 286; b += 4) if (b < 240 || b > 253) wall(b, (b % 8 == 0) ? H / 2 + JS.sin(b * 0.45) * 230 : W / 2 + JS.sin(b * 0.45) * 400, T, (b % 8 == 0) ? 480 : 330, (b % 8 == 0) ? "L" : "U");
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
