using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 471-621: the awakened one (lens, Eye, PolyBurst, Orb, Pendulum, Palm, Awakened) and buildLevel3
// segDist, Shard, Slash are in Shared.cs
public static partial class G
{
	static readonly int _afkL3 = AfkReg("buildLevel3", (x, y, b, v, w) => One(v >= 2 ? PolyBurst(x, y, 6, B(b), 230, 16, v - 2, 0, 0.4, w * SPB) : Slash(x, y, v != 0 ? Math.PI / 2 : 0, B(b - w), B(b), B(b + 0.35))));

	public static void lens(double r, double o) { g.beginPath(); g.moveTo(-r * 1.4, 0); g.quadraticCurveTo(0, -r * 1.3 * o - 2, r * 1.4, 0); g.quadraticCurveTo(0, r * 1.3 * o + 2, -r * 1.4, 0); g.closePath(); }

	// an eye that opens and fires a beam where it looks
	public static Hz Eye(double x, double y, double ang, double tw, double ta, double te, double or = 0, double obw = 0, bool hide = false)
	{
		double r = or != 0 ? or : 22, bw = obw != 0 ? obw : 26; const double len = 2200;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.35) h.dead = true; };
		h.draw = t =>
		{
			double op = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : t < te ? 1 : clamp(1 - (t - te) / 0.3, 0, 1);
			g.save(); g.translate(x, y); g.save(); g.rotate(ang);
			if (t < ta) { g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * op)})"; g.fillRect(0, -bw / 2, len, bw); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * op)})"; g.fillRect(0, -1.5, len, 3); }
			else if (t < te) { double fl = Math.Max(0, 1 - (t - ta) / 0.12), ww = bw * (1 + 0.12 * pulse); g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; g.fillRect(0, -ww / 2, len, ww); if (fl > 0) { g.globalAlpha = fl; g.fillStyle = "#fff"; g.fillRect(0, -ww / 2, len, ww); } }
			g.restore();
			if (!hide)
			{
				g.globalAlpha = t < ta ? 0.5 : op; lens(r, Math.Max(op, 0.08)); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.fill(); g.shadowBlur = 0;
				if (op > 0.15) { g.fillStyle = "#000"; circle(JS.cos(ang) * r * 0.4, JS.sin(ang) * r * 0.4 * op, r * 0.45 * op); }
				g.globalAlpha = 1;
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; if (!hide && hypot(p.x - x, p.y - y) < r + p.r) return true; return obbHit(p.x, p.y, p.r, x + JS.cos(ang) * len / 2, y + JS.sin(ang) * len / 2, len / 2, bw / 2, ang); };
		return h;
	}

	// a shape that grows, with one side missing
	public static Hz PolyBurst(double cx, double cy, int n, double t0, double sp, double th, int gapI, double rot0, double rotV, double warn)
	{
		Func<double, double[][]> pts = t => { double r = Math.Max(0, (t - t0) * sp), rot = rot0 + rotV * (t - t0); var P = new double[n][]; for (int i = 0; i < n; i++) { double a = rot + i * TAU / n; P[i] = new[] { cx + JS.cos(a) * r, cy + JS.sin(a) * r }; } return P; };
		var h = new Hz();
		h.update = (t, dt) => { if ((t - t0) * sp > 1700) h.dead = true; };
		h.draw = t =>
		{
			g.strokeStyle = PINK; g.lineCap = "round";
			if (t < t0)
			{
				double k = clamp(1 - (t0 - t) / warn, 0, 1), r = 20 + 45 * k; g.globalAlpha = 0.6; g.lineWidth = 4; g.beginPath();
				for (int i = 0; i < n; i++) { if (i == gapI) continue; double a1 = rot0 + i * TAU / n, a2 = rot0 + (i + 1) * TAU / n; g.moveTo(cx + JS.cos(a1) * r, cy + JS.sin(a1) * r); g.lineTo(cx + JS.cos(a2) * r, cy + JS.sin(a2) * r); }
				g.stroke(); g.globalAlpha = 1; return;
			}
			var P = pts(t); g.lineWidth = th * (1 + 0.15 * pulse); g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath();
			for (int i = 0; i < n; i++) { if (i == gapI) continue; var a = P[i]; var b = P[(i + 1) % n]; g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); }
			g.stroke(); g.shadowBlur = 0;
		};
		h.hit = (p, t) => { if (t < t0) return false; var P = pts(t); for (int i = 0; i < n; i++) { if (i == gapI) continue; if (segDist(p.x, p.y, P[i], P[(i + 1) % n]) < th / 2 + p.r - 2) return true; } return false; };
		return h;
	}

	// a ball that chases you, then explodes
	public static Hz Orb(double x, double y, double tw, double t0, double t1, double sp, double turn, double shsp)
	{
		double vx = 0, vy = 0;
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) =>
		{
			if (t < t0) return;
			if (t >= t1) { for (int i = 0; i < 8; i++) { double a = i * TAU / 8 + 0.2; hazards.Add(Shard(h.x, h.y, JS.cos(a) * shsp, JS.sin(a) * shsp)); } burst(h.x, h.y, PINK, 14); shake = Math.Max(shake, 5); h.dead = true; return; }
			if (player != null) { double a = JS.atan2(player.y - h.y, player.x - h.x), ca = JS.atan2(vy, vx); double da = a - ca; da = JS.atan2(JS.sin(da), JS.cos(da)); double na = (vx != 0 || vy != 0) ? ca + clamp(da, -turn * dt, turn * dt) : a; vx = JS.cos(na) * sp; vy = JS.sin(na) * sp; }
			h.x = clamp(h.x + vx * dt, 0, W); h.y = clamp(h.y + vy * dt, 0, H);
		};
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = PINK; g.globalAlpha = 0.6; g.lineWidth = 3; g.beginPath(); g.arc(h.x, h.y, 40 * (1 - k) + 15, 0, TAU); g.stroke(); g.globalAlpha = 0.3; g.fillStyle = PINK; circle(h.x, h.y, 15 * k); g.globalAlpha = 1; return; }
			double left = t1 - t; bool bl = left < 0.6 && Math.Floor(left * 14) % 2 == 0; g.fillStyle = "rgba(255,42,109,0.25)"; circle(h.x, h.y, 27 * (1 + 0.2 * pulse)); g.fillStyle = bl ? "#fff" : PINK; circle(h.x, h.y, 15 * (1 + 0.15 * pulse));
		};
		h.hit = (p, t) => t >= t0 && hypot(p.x - h.x, p.y - h.y) < 15 + p.r - 1;
		return h;
	}

	// a pendulum swinging from the top
	public static Hz Pendulum(double px, double L, double amp, double per, double ph, double tw, double t0, double t1, double R = 42)
	{
		Func<double, double> ang = t => amp * JS.sin(TAU * (t - t0) / per + ph);
		Func<double, double> len = t => t < t0 ? L * clamp((t - tw) / (t0 - tw), 0, 1) : t < t1 ? L : L * clamp(1 - (t - t1) / (SPB * 1.5), 0, 1);
		Func<double, double[]> bob = t => { double a = ang(Math.Max(t, t0)), l = len(t); return new[] { px + JS.sin(a) * l, -30 + JS.cos(a) * l }; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + SPB * 1.5) h.dead = true; };
		h.draw = t =>
		{
			bool act = t >= t0 && t < t1;
			if (t < t0) { g.strokeStyle = "rgba(255,42,109,0.3)"; g.lineWidth = 2; g.beginPath(); g.arc(px, -30, L, Math.PI / 2 - amp, Math.PI / 2 + amp); g.stroke(); }
			var q = bob(t); double bx = q[0], by = q[1]; g.globalAlpha = act ? 1 : 0.4; g.strokeStyle = PINK; g.lineWidth = 6; g.beginPath(); g.moveTo(px, -30); g.lineTo(bx, by); g.stroke();
			g.fillStyle = PINK; if (act) { g.shadowColor = PINK; g.shadowBlur = 22; }
			circle(bx, by, R * (1 + 0.1 * pulse)); g.shadowBlur = 0; g.fillStyle = "#000"; circle(bx, by, R * 0.42); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; var q = bob(t); double bx = q[0], by = q[1]; if (hypot(p.x - bx, p.y - by) < R + p.r - 2) return true; return segDist(p.x, p.y, new[] { px, -30.0 }, new[] { bx, by }) < 3 + p.r; };
		return h;
	}

	// a palm that comes down and hits the floor
	public static Hz Palm(double x, double y, double tw, double ta, double stay, double shsp)
	{
		const double w = 150, hh = 170; bool l = false;
		void shape(double s) { double fw = w / 4; g.beginPath(); rr(x - w / 2 * s, y - hh * 0.1 * s, w * s, hh * 0.6 * s, 20 * s); g.fill(); for (int i = 0; i < 4; i++) { rr(x - w / 2 * s + i * fw * s + 4 * s, y - hh / 2 * s, fw * s - 8 * s, hh * 0.5 * s, 10 * s); g.fill(); } }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!l && t >= ta) { l = true; shake = Math.Max(shake, 9); burst(x, y, PINK, 14, 300); }
			if (t >= ta + stay) { for (int i = 0; i < 6; i++) { double a = i * TAU / 6 + 0.3; hazards.Add(Shard(x, y, JS.cos(a) * shsp, JS.sin(a) * shsp)); } h.dead = true; }
		};
		h.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.2 * k)})"; shape(1.6 - 0.6 * k); }
			else { g.save(); g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = (t - ta) < 0.08 ? "#fff" : PINK; shape(1); g.restore(); }
		};
		h.hit = (p, t) => t >= ta && t < ta + stay && Math.Abs(p.x - x) < w / 2 + p.r && Math.Abs(p.y - y) < hh / 2 + p.r;
		return h;
	}

	// the boss: the awakened one, a giant eye half outside the screen
	public static Hz Awakened(double ts, double te, Tier T)
	{
		const double cy = H / 2, R = 200, HX = W + 80; double x = W + 420, look = Math.PI; int lastS = -1; bool ex = false, rumbled = false;
		double openT = ts + 4 * SPB, openE = ts + 8 * SPB, live = ts + 8 * SPB;
		Func<double, double> op = t => t >= te ? 0 : clamp((t - openT) / (openE - openT), 0, 1);
		double[] pupil() => new[] { x + JS.cos(look) * R * 0.4, cy + JS.sin(look) * R * 0.4 };
		void beam(double t, double w = 34) { var q = pupil(); double px = q[0], py = q[1]; if (player == null) return; double a = JS.atan2(player.y - py, player.x - px); hazards.Add(Eye(px, py, a, t, t + SPB * T.wn, t + SPB * (T.wn + 0.75), 0, w, true)); }
		void fan(int n, double spread, double sp) { var q = pupil(); double px = q[0], py = q[1]; if (player == null) return; double a0 = JS.atan2(player.y - py, player.x - px); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spread; hazards.Add(Shard(px, py, JS.cos(a) * sp, JS.sin(a) * sp)); } }
		void palm(double t) { if (player == null) return; double px = clamp(player.x + (rnd() * 2 - 1) * 60, 80, W - 300), py = clamp(player.y + (rnd() * 2 - 1) * 60, 90, H - 90); hazards.Add(Palm(px, py, t, t + SPB * (T.wn + 0.2), SPB * 0.5, 230)); }
		void orbs(double t) { foreach (var dy in new double[] { -170, 170 }) hazards.Add(Orb(W - 260, cy + dy, t, t + SPB, t + SPB * 7, 165, 1.6, 210)); }
		void poly_(double t, int n) { var q = pupil(); double px = q[0], py = q[1]; hazards.Add(PolyBurst(px, py, n, t + SPB * T.wn, 230, 16, (int)Math.Floor(rnd() * n), rnd() * TAU, 0.4, SPB * T.wn)); }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			x = t < ts + 2 * SPB ? W + 420 - (W + 420 - HX) * clamp((t - ts) / (2 * SPB), 0, 1) : HX;
			if (player != null) { double px = x, py = cy; double target = JS.atan2(player.y - py, player.x - px); double d = target - look; d = JS.atan2(JS.sin(d), JS.cos(d)); look += d * 0.12; }
			if (t >= te && !ex) { ex = true; for (int i = 0; i < 30; i++) { double a = Math.PI / 2 + i * Math.PI / 29; hazards.Add(Shard(W - 60, cy, JS.cos(a) * 230, JS.sin(a) * 230)); } burst(W - 60, cy, PINK, 80, 560); burst(W - 60, cy, "#fff", 40, 460); shake = 28; flash = 0.75; rumble(600, 1, 1); h.dead = true; return; }
			if (t < openT + 0.01 && t >= ts + 2 * SPB && !rumbled) { rumbled = true; rumble(800, 0.5, 0.4); shake = Math.Max(shake, 6); }
			if (t < live) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s;
			int bar = (int)Math.Floor(s / 16.0), p = s % 16;
			if (bar < 55) { if (p == 0) beam(t); else if (p == 8) palm(t); else if (p == 4 || p == 12) fan(5, 0.18, T.sp); else if (p == 10 && bar % 2 != 0) beam(t, 28); }
			else if (bar < 59) { if (p == 0 && bar % 2 == 1) orbs(t); else if (p == 8) fan(5, 0.2, T.sp * 0.9); }
			else if (bar < 63) { if (p == 0 || p == 8) beam(t); else if (p == 4) palm(t); else if (p == 12) poly_(t, 6); else if (p % 4 == 2) fan(3, 0.2, T.sp); }
			else { if (p == 0) beam(t); else if (p == 8 && bar % 2 != 0) palm(t); else if (p == 8) fan(5, 0.18, T.sp * 0.9); else if (p == 12) fan(3, 0.22, T.sp); }
		};
		h.draw = t =>
		{
			double o = op(t); g.save(); g.translate(x, cy);
			g.save(); g.rotate(t * 0.3); g.strokeStyle = PINK; g.globalAlpha = 0.5; g.lineWidth = 2; for (int i = 0; i < 8; i++) { g.rotate(TAU / 8); g.save(); g.translate(R + 34, 0); g.rotate(Math.PI / 4); g.strokeRect(-8, -8, 16, 16); g.restore(); }
			g.restore(); g.globalAlpha = 1;
			g.shadowColor = PINK; g.shadowBlur = 40; g.fillStyle = PINK; circle(0, 0, R * (1 + 0.04 * pulse)); g.shadowBlur = 0;
			double lr = R * 0.85; void lp() { g.beginPath(); g.moveTo(-lr, 0); g.quadraticCurveTo(0, -R * 0.95 * o, lr, 0); g.quadraticCurveTo(0, R * 0.95 * o, -lr, 0); g.closePath(); }
			if (o > 0.03) { lp(); g.fillStyle = "#000"; g.fill(); g.save(); lp(); g.clip(); g.fillStyle = PINK; circle(JS.cos(look) * R * 0.38, JS.sin(look) * R * 0.38, R * 0.3); g.fillStyle = "#000"; circle(JS.cos(look) * R * 0.43, JS.sin(look) * R * 0.43, R * 0.13 * (1 + 0.3 * pulse)); g.restore(); }
			else { g.strokeStyle = "#000"; g.lineWidth = 7; g.beginPath(); g.moveTo(-lr, 0); g.quadraticCurveTo(0, R * 0.18, lr, 0); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) => hypot(p.x - x, p.y - cy) < R - 6 + p.r;
		return h;
	}

	/* 120 bpm. part 1 0-107 | part 2 108-187 | part 3 188-283, the boss is beaten at 284 */
	public static List<Ev> buildLevel3()
	{
		var E = new List<Ev>(); var R = mulberry32(303);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 120, double b = W - 120) => a + R() * (b - a);
		double ry(double a = 100, double b = H - 100) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 285, wn = 1.375 }, new Tier { sp = 315, wn = 1.25 }, new Tier { sp = 335, wn = 1.2 } });
		double[] edge() { int s = (int)Math.Floor(R() * 4); double u = R(); return s == 0 ? new[] { 60 + u * (W - 120), 30 } : s == 1 ? new[] { W - 30, 60 + u * (H - 120) } : s == 2 ? new[] { 60 + u * (W - 120), H - 30 } : new[] { 30, 60 + u * (H - 120) }; }
		void eye(double hit, Tier T, double[] pt = null, double dur = 0.75)
		{
			var q = pt ?? edge(); double x = q[0], y = q[1];
			ev(hit - T.wn, () => { double a = player != null ? JS.atan2(player.y - y, player.x - x) : JS.atan2(H / 2 - y, W / 2 - x); hazards.Add(Eye(x, y, a, B(hit - T.wn), B(hit), B(hit + dur))); });
		}
		void slash(double hit, Tier T, bool onPlayer = true)
		{
			double a = R() * Math.PI, jx = (R() * 2 - 1) * 80, jy = (R() * 2 - 1) * 80, fx = rx(), fy = ry();
			ev(hit - T.wn, () => { double cx = onPlayer && player != null ? player.x + jx : fx, cy = onPlayer && player != null ? player.y + jy : fy; hazards.Add(Slash(cx, cy, a, B(hit - T.wn), B(hit), B(hit + 0.35))); });
		}
		void poly(double hit, double x, double y, int n, Tier T, double sp = 220, double rv = 0) { int gi = (int)Math.Floor(R() * n); double r0 = R() * TAU; ev(hit - T.wn, () => hazards.Add(PolyBurst(x, y, n, B(hit), sp, 16, gi, r0, rv, T.wn * SPB))); }
		void orb(double hit, double x, double y, Tier T, double life = 5, double sp = 165) => ev(hit - 1, () => hazards.Add(Orb(x, y, B(hit - 1), B(hit), B(hit + life), sp, 1.5, T.sp * 0.7)));
		void pend(double b0, double b1, double px, double L, double amp, double per, double ph) => ev(b0 - 2, () => hazards.Add(Pendulum(px, L, amp, per * SPB, ph, B(b0 - 2), B(b0), B(b1))));
		void fanFrom(double b, double[] pt, int n, double spread, double sp) => ev(b, () => { if (player == null) return; double x = pt[0], y = pt[1]; double a0 = JS.atan2(player.y - y, player.x - x); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spread; hazards.Add(Shard(x, y, JS.cos(a) * sp, JS.sin(a) * sp)); } });
		var C = new[] { new[] { 0.0, 0.0 }, new[] { W, 0.0 }, new[] { W, H }, new[] { 0.0, H } };

		/* ---------- part 1 ---------- */
		var T = TI[0];
		for (int k = 1; k < 8; k += 2) eye(k * 4, T);
		for (int k = 2; k < 8; k += 2) eye(k * 4 + 2, T, new[] { W / 2, 20 });
		orb(12, W * 0.75, H * 0.3, T); orb(28, W * 0.75, H * 0.7, T);
		for (int k = 8; k < 12; k++) { double b = k * 4; slash(b, T); if (k % 2 != 0) slash(b + 1, T, false); if (k % 2 == 0) eye(b + 3, T); }
		for (int k = 12; k < 20; k++) { double b = k * 4; if (k % 2 == 0) poly(b, rx(300, W - 300), ry(200, H - 200), 6, T, 200); else { eye(b, T); slash(b + 2, T, false); } }
		pend(82, 106, W * 0.5, 520, 0.95, 8, 0);
		for (int k = 20; k < 27; k++) { double b = k * 4; if (k % 2 == 0) slash(b, T); else eye(b + 2, T); }

		/* ---------- part 2 ---------- */
		T = TI[1];
		ev(108, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		foreach (var c in C) eye(108, T, new[] { c[0] != 0 ? c[0] - 30 : 30, c[1] != 0 ? c[1] - 30 : 30 }, 1);
		foreach (var b in new double[] { 112, 120, 128 }) { orb(b, rx(700, W - 120), ry(), T, 6, 160); orb(b + 2, rx(120, 560), ry(), T, 6, 160); }
		pend(114, 134, W * 0.5, 560, 1.0, 8, 0);
		foreach (var b in new double[] { 116, 124, 132 }) poly(b, rx(300, W - 300), ry(200, H - 200), 5, T, 230, 0.3);
		for (int k = 34; k < 42; k++) { double b = k * 4; eye(b, T); eye(b + 0.5, T); slash(b + 2, T); fanFrom(b + 3, C[k % 4], 4, 0.2, T.sp); }
		poly(144, W / 2, H / 2, 6, T, 230, 0.4); poly(160, W / 2, H / 2, 6, T, 230, -0.4);
		for (int k = 42; k < 46; k++) { double b = k * 4; slash(b, T); slash(b + 1, T, false); slash(b + 2, T); slash(b + 3, T, false); }
		poly(168, W / 2, H / 2, 3, T, 250, 0.5); poly(176, W / 2, H / 2, 3, T, 250, -0.5);
		pend(168, 184, W * 0.3, 500, 1.0, 4, 0); pend(168, 184, W * 0.7, 500, 1.0, 4, Math.PI);
		// 1:32 the pause, nothing

		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		ev(188, () => hazards.Add(Awakened(B(188), B(284), TT)));
		for (int k = 49; k < 55; k++) { slash(k * 4 + 2, T); if (k % 2 != 0) eye(k * 4 + 3, T); }
		for (int k = 55; k < 59; k++) { double b = k * 4; slash(b, T); slash(b + 2, T, false); eye(b + 1, T); }
		for (int k = 59; k < 63; k++) { double b = k * 4; slash(b + 1, T, false); slash(b + 3, T); }
		pend(254, 282, W * 0.28, 500, 0.9, 8, 0); pend(254, 282, W * 0.58, 500, 0.9, 8, Math.PI);
		for (int k = 63; k < 71; k++) { slash(k * 4 + 2, T); if (k % 2 == 0) slash(k * 4 + 3.5, T, false); }
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
