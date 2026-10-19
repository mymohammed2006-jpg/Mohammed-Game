using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 731-912: Runaway (PianoKey, KeyStrip, Spark, Firework, glassPath, Toast, Sequin, Ballerina, Feather, Phoenix, DiveMark) and buildLevel5
public static partial class G
{
	static readonly int _afkL5 = AfkReg("buildLevel5", (x, y, b, v, w) => v >= 2 ? new[] { 0, 1, 2, 3, 4, 5, 6, 7 }.Select(i => PianoKey(i * 2 + (v - 2), 16, B(b - w), B(b), B(b + 0.6), false)).ToArray() : One(PianoKey(Math.Floor(x / (W / 16)), 16, B(b - w), B(b), B(b + 0.6), false)));

	// a piano key: comes down from the top over a whole column with the note, then goes back up
	public static Hz PianoKey(double col, int n, double tw, double ta, double te, bool black)
	{
		double cw = W / n, x = col * cw + cw / 2, w = black ? cw * 0.62 : cw - 4, len = black ? H * 0.62 : H;
		Func<double, double> drop = t => clamp((t - ta) / 0.07, 0, 1), lift = t => t < te ? 0 : clamp((t - te) / 0.18, 0, 1);
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.18) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.fillRect(x - w / 2, 0, w, len);
				g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(x - w / 2, len * k - 3, w, 3); return;
			}
			double h = len * drop(t) * (1 - lift(t)); if (h <= 0) return;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.09 ? "#fff" : PINK; rr(x - w / 2, -14, w, h + 14, 8); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(0,0,0,0.35)"; g.fillRect(x - w / 2 + 6, h - 22, w - 12, 6); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta || t >= te) return false; double h = len * drop(t); return Math.Abs(p.x - x) < w / 2 + p.r - 1 && p.y - p.r < h - 1; };
		return hz;
	}

	// decoration: a grey keyboard at the top of the screen (harmless)
	public static Hz KeyStrip(double t0, double t1, int n)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 1.2, (t1 - t) / 1.2)); if (a <= 0) return; double cw = W / n; g.globalAlpha = a; g.strokeStyle = GREY + str(0.1 + 0.08 * pulse) + ")"; g.lineWidth = 1.5;
			for (int i = 0; i < n; i++) g.strokeRect(i * cw + 3, -6, cw - 6, 40);
			g.fillStyle = GREY + "0.12)"; for (int i = 0; i < n; i++) if (i % 7 != 2 && i % 7 != 6) g.fillRect((i + 1) * cw - cw * 0.22, -6, cw * 0.44, 26);
			g.globalAlpha = 1;
		};
		return h;
	}

	// a firework spark: slows down, falls with gravity and fades
	public static Hz Spark(double x, double y, double vx, double vy, double life = 2.3, double r = 8)
	{
		var b = Bullet(x, y, vx, vy, r); double age = 0;
		b.update = (t, dt) => { age += dt; double f = JS.pow(0.5, dt); b.vx *= f; b.vy = b.vy * f + 170 * dt; b.x += b.vx * dt; b.y += b.vy * dt; if (age > life || b.x < -80 || b.x > W + 80 || b.y > H + 80) b.dead = true; };
		b.draw = t =>
		{
			double a = clamp((life - age) / 0.4, 0, 1), s = b.r * (1 + 0.15 * pulse); g.save(); g.globalAlpha = a; g.translate(b.x, b.y); g.fillStyle = "rgba(255,42,109,0.25)"; circle(0, 0, s * 1.8);
			g.fillStyle = PINK; g.beginPath(); for (int i = 0; i < 8; i++) { double rr2 = i % 2 != 0 ? s * 0.45 : s * 1.35, an = i * Math.PI / 4; if (i != 0) g.lineTo(JS.cos(an) * rr2, JS.sin(an) * rr2); else g.moveTo(rr2, 0); }
			g.closePath(); g.fill(); g.restore();
		};
		var h0 = b.hit; b.hit = (p, t) => age < life - 0.3 && h0(p, t);
		return b;
	}

	// a firework rocket: rises from the bottom (grey, harmless) and bursts into pink sparks on the hit
	public static Hz Firework(double x, double y, int n, double tw, double ta, double sp)
	{
		double x0 = x + (x < W / 2 ? -60 : 60); Func<double, double[]> pos = t => { double k = clamp((t - tw) / (ta - tw), 0, 1), e = 1 - JS.pow(1 - k, 2); return new[] { x0 + (x - x0) * e, H + 20 + (y - H - 20) * e }; }; double rot0 = rnd() * TAU;
		var h = new Hz();
		h.update = (t, dt) => { if (t >= ta) { for (int i = 0; i < n; i++) { double a = rot0 + i * TAU / n; hazards.Add(Spark(x, y, JS.cos(a) * sp, JS.sin(a) * sp)); } burst(x, y, PINK, 14, 260); burst(x, y, "#fff", 6, 200); shake = Math.Max(shake, 5); h.dead = true; } };
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1); var q = pos(t); double px = q[0], py = q[1];
			g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < n; i++) { double a = rot0 + i * TAU / n; g.moveTo(x + JS.cos(a) * 14, y + JS.sin(a) * 14); g.lineTo(x + JS.cos(a) * (24 + 30 * k), y + JS.sin(a) * (24 + 30 * k)); }
			g.stroke();
			g.strokeStyle = GREY + "0.35)"; g.beginPath(); g.moveTo(px, py); g.lineTo(px + (x0 - x) * 0.08, py + 70); g.stroke();
			g.fillStyle = GREY + "0.9)"; g.save(); g.translate(px, py); g.rotate(JS.atan2(y - H - 20, x - x0) + Math.PI / 2); g.fillRect(-4, -10, 8, 20); g.beginPath(); g.moveTo(-4, -10); g.lineTo(0, -18); g.lineTo(4, -10); g.fill(); g.restore();
		};
		return h;
	}

	// a glass: the outline of a wine glass
	public static void glassPath(double s) { g.beginPath(); g.moveTo(-s * 0.42, -s * 0.95); g.lineTo(s * 0.42, -s * 0.95); g.quadraticCurveTo(s * 0.46, -s * 0.15, s * 0.06, -s * 0.05); g.lineTo(s * 0.06, s * 0.7); g.lineTo(s * 0.34, s * 0.8); g.lineTo(s * 0.34, s * 0.92); g.lineTo(-s * 0.34, s * 0.92); g.lineTo(-s * 0.34, s * 0.8); g.lineTo(-s * 0.06, s * 0.7); g.lineTo(-s * 0.06, -s * 0.05); g.quadraticCurveTo(-s * 0.46, -s * 0.15, -s * 0.42, -s * 0.95); g.closePath(); }

	// a toast: two glasses come from the sides, clink on the hit and break into shards
	public static Hz Toast(double x, double y, bool vert, double tw, double ta, int n, double sp)
	{
		const double s = 52, D = 560, slide = 0.38; double ts = ta - slide, ang = vert ? Math.PI / 2 : 0, c = JS.cos(ang), sn = JS.sin(ang);
		Func<double, double> off = t => t < ts ? D : t < ta ? 18 + (D - 18) * JS.pow(1 - (t - ts) / slide, 2) : 18;
		var h = new Hz();
		h.update = (t, dt) => { if (t >= ta) { double r0 = rnd() * TAU; for (int i = 0; i < n; i++) { double a = r0 + i * TAU / n, v = sp * (0.85 + 0.3 * (i % 2)); hazards.Add(ShardTri(x, y, JS.cos(a) * v, JS.sin(a) * v, 8)); } burst(x, y, "#fff", 10, 280); burst(x, y, PINK, 14, 300); shake = Math.Max(shake, 7); h.dead = true; } };
		h.draw = t =>
		{
			double o = off(t);
			foreach (var sd in new double[] { -1, 1 })
			{
				double gx = x + c * sd * o, gy = y + sn * sd * o; g.save(); g.translate(gx, gy); g.rotate(sd * 0.25);
				if (t < ts) { double k = clamp((t - tw) / (ts - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.16 * k)})"; glassPath(s); g.fill(); g.strokeStyle = "rgba(255,42,109,0.8)"; g.lineWidth = 2; g.stroke(); }
				else { g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; glassPath(s); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.3)"; g.fillRect(-s * 0.3, -s * 0.8, s * 0.12, s * 0.55); }
				g.restore();
			}
			if (t < ts) { double k = clamp((t - tw) / (ts - tw), 0, 1); g.save(); g.translate(x, y); g.rotate(ang); g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.07 * k)})"; g.fillRect(-D, -s, 2 * D, 2 * s); g.restore(); g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; circle(x, y, 4 + 6 * k); }
		};
		h.hit = (p, t) => { if (t < ts || t >= ta) return false; double o = off(t); foreach (var sd in new double[] { -1, 1 }) { if (obbHit(p.x, p.y, p.r, x + c * sd * o, y + sn * sd * o, s * 0.4, s * 0.9, 0)) return true; } return false; };
		return h;
	}

	// pink sequins flying from the dancer's skirt
	public static Hz Sequin(double x, double y, double vx, double vy)
	{
		var b = Bullet(x, y, vx, vy, 8); double rot = rnd() * TAU;
		b.draw = t => { rot += 0.12; g.save(); g.translate(b.x, b.y); g.rotate(rot); double s = b.r * 1.3 * (1 + 0.15 * pulse); g.fillStyle = "rgba(255,42,109,0.25)"; g.fillRect(-s * 1.2, -s * 1.2, s * 2.4, s * 2.4); g.fillStyle = PINK; g.beginPath(); g.moveTo(0, -s); g.lineTo(s * 0.7, 0); g.lineTo(0, s); g.lineTo(-s * 0.7, 0); g.closePath(); g.fill(); g.restore(); };
		return b;
	}

	// a grey ballerina spinning along a path; her pink skirt opens on every beat (only the skirt hurts)
	public static Hz Ballerina(Func<double, (double x, double y)> path, double tw, double t0, double t1, double R, double shedSp)
	{
		double spin = 0; int lastB = -1;
		Func<double, double> env = t => JS.exp(-(((BT(t)) % 1 + 1) % 1) * 3.5), rad = t => R * (0.6 + 0.4 * env(t)), u = t => clamp((t - t0) / (t1 - t0), 0, 1);
		var h = new Hz();
		h.update = (t, dt) =>
		{
			spin += dt * 8; if (t > t1 + 0.4) { h.dead = true; return; }
			if (shedSp > 0 && t >= t0 && t < t1)
			{
				int b = (int)Math.Floor(BT(t) + 1e-6);
				if (b != lastB) { lastB = b; if (b % 2 == 0) { var q = path(u(t)); for (int i = 0; i < 4; i++) { double a = spin + i * TAU / 4; hazards.Add(Sequin(q.x + JS.cos(a) * R * 0.5, q.y + JS.sin(a) * R * 0.2, JS.cos(a) * shedSp, JS.sin(a) * shedSp)); } } }
			}
		};
		h.draw = t =>
		{
			var q = path(u(t)); bool live = t >= t0 && t < t1; double a = t < t0 ? clamp((t - tw) / (t0 - tw), 0, 1) : t >= t1 ? clamp(1 - (t - t1) / 0.4, 0, 1) : 1;
			if (t < t0) { g.fillStyle = "rgba(255,42,109,0.25)"; for (int i = 0; i <= 30; i++) { var p2 = path(i / 30.0); circle(p2.x, p2.y, 2.5); } }
			g.save(); g.translate(q.x, q.y); g.globalAlpha = a;
			// the grey body
			g.strokeStyle = GREY + "0.85)"; g.fillStyle = GREY + "0.85)"; g.lineWidth = 4; g.lineCap = "round";
			circle(0, -62, 11); g.beginPath(); g.moveTo(0, -50); g.lineTo(0, -6); g.stroke();
			double arm = JS.sin(spin) * 0.5; g.beginPath(); g.moveTo(0, -42); g.quadraticCurveTo(-26, -60 - 12 * arm, -6, -82); g.moveTo(0, -42); g.quadraticCurveTo(26, -60 + 12 * arm, 6, -82); g.stroke();
			g.beginPath(); g.moveTo(0, -4); g.lineTo(0, 46); g.moveTo(0, -4); g.lineTo(JS.cos(spin) * 30, 22); g.stroke(); g.lineCap = "butt";
			// the skirt
			double r = live ? rad(t) : R * 0.75; g.save(); g.scale(1, 0.36);
			if (live) { g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - t0) < 0.08 ? "#fff" : PINK; } else { g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.2 * a)})"; }
			g.beginPath(); for (int i = 0; i <= 24; i++) { double an = spin + i * TAU / 24, k = i % 2 != 0 ? 0.82 : 1; if (i != 0) g.lineTo(JS.cos(an) * r * k, JS.sin(an) * r * k); else g.moveTo(JS.cos(an) * r * k, JS.sin(an) * r * k); }
			g.closePath(); g.fill(); g.restore();
			g.globalAlpha = 1; g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; var q = path(u(t)); double r = rad(t), dx = (p.x - q.x) / (r * 0.92 + p.r - 2), dy = (p.y - q.y) / (r * 0.33 + p.r - 2); return dx * dx + dy * dy < 1; };
		return h;
	}

	// a fire feather
	public static Hz Feather(double x, double y, double vx, double vy, double r = 9)
	{
		var b = Bullet(x, y, vx, vy, r); double a = JS.atan2(vy, vx);
		b.draw = t =>
		{
			g.save(); g.translate(b.x, b.y); g.rotate(a); double s = 1 + 0.15 * pulse; g.fillStyle = "rgba(255,42,109,0.22)"; g.beginPath(); g.ellipse(0, 0, 22 * s, 10 * s, 0, 0, TAU); g.fill();
			g.fillStyle = PINK; g.beginPath(); g.moveTo(18 * s, 0); g.quadraticCurveTo(4, -8 * s, -14 * s, -3 * s); g.lineTo(-20 * s, 0); g.lineTo(-14 * s, 3 * s); g.quadraticCurveTo(4, 8 * s, 18 * s, 0); g.fill(); g.strokeStyle = "rgba(0,0,0,0.45)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(-20 * s, 0); g.lineTo(14 * s, 0); g.stroke(); g.restore();
		};
		return b;
	}

	class Dive { public double tw, ta, tb, te, px, py; }

	// the boss: the phoenix, top middle; its wings flap with the beat and throw feathers, it sweeps with fire and dives at the player.
	// When the music cuts it stops and charges, and explodes at 2:50
	public static Hz Phoenix(double ts, double tc, double tx, Tier T)
	{
		const double HX = W / 2, HY = 150, WL = 190; double x = HX, y = -200; int lastS = -1; bool ex = false; Dive dive = null;
		Func<double, double> flap = t => JS.exp(-(((BT(t)) % 1 + 1) % 1) * 3), grow = t => clamp((t - ts) / (2 * SPB), 0, 1);
		double wingA(double t, double sd) { double a = -0.3 - 0.75 * flap(t); return sd > 0 ? a : Math.PI - a; }
		double[] root(double sd) => new[] { x + sd * 26, y - 4 };
		double[] tip(double t, double sd) { var q = root(sd); double rx = q[0], ry = q[1], a = wingA(t, sd); return new[] { rx + JS.cos(a) * WL, ry + JS.sin(a) * WL }; }
		void fanW(double t, int n, double spr, double sp) { foreach (var sd in new double[] { -1, 1 }) { var q = tip(t, sd); double tx2 = q[0], ty2 = q[1], a0 = sd > 0 ? 0.55 : Math.PI - 0.55; for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spr; hazards.Add(Feather(tx2, ty2, JS.cos(a) * sp, JS.sin(a) * sp)); } } }
		void aim(int n, double spr, double sp) { if (player == null) return; double a0 = JS.atan2(player.y - y, player.x - x); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spr; hazards.Add(Feather(x + JS.cos(a) * 40, y + JS.sin(a) * 40, JS.cos(a) * sp, JS.sin(a) * sp)); } }
		void ring(int n, double bs, double sp) { for (int i = 0; i < n; i++) { double a = bs + i * TAU / n; hazards.Add(Feather(x + JS.cos(a) * 40, y + JS.sin(a) * 40, JS.cos(a) * sp, JS.sin(a) * sp)); } }
		void sweep(double t, double sd) { double a0 = sd > 0 ? 0.12 : Math.PI - 0.12, a1 = sd > 0 ? 1.9 : Math.PI - 1.9; hazards.Add(Sweep(HX, HY, a0, a1, 30, t, t + SPB * T.wn, t + SPB * (T.wn + 2))); }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= tx && !ex) { ex = true; burst(x, y, PINK, 90, 600); burst(x, y, "#fff", 50, 480); burst(x, y, "#ffb36b", 30, 380); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; return; }
			if (t < ts) { return; }
			if (dive != null)
			{
				double tw = dive.tw, ta = dive.ta, tb = dive.tb, te = dive.te, px = dive.px, py = dive.py;
				if (t < ta) { x = HX; y = HY; }
				else if (t < tb) { double k = clamp((t - ta) / (tb - ta), 0, 1), e = k * k; x = HX + (px - HX) * e; y = HY + (py - HY) * e; }
				else if (t < te) { double k = clamp((t - tb) / (te - tb), 0, 1), e = 1 - JS.pow(1 - k, 2); x = px + (HX - px) * e; y = py + (HY - py) * e; }
				else { x = HX; y = HY; dive = null; }
			}
			else { double k = grow(t), e = 1 - JS.pow(1 - k, 3); x = HX; y = -200 + (HY + 200) * e; }
			if (t < ts + 2 * SPB || t >= tc) return;
			int s = (int)Math.Floor(BT(t) * 2 + 1e-6); if (s == lastS) return; lastS = s;
			double b = s / 2.0, rb = Math.Floor((b - BT(ts)) / 4); int p = s % 8; // p: half beat inside the bar
			if (dive != null && t < dive.te) return;
			if (rb < 4) { if (p == 0 || p == 4) fanW(t, 6, 0.26, T.sp * 0.9); else if (p == 2 || p == 6) aim(5, 0.2, T.sp); else if (p == 5) ring(12, s * 0.4, T.sp * 0.7); }
			else if (rb < 8) { if (p == 0) { fanW(t, 6, 0.26, T.sp * 0.9); sweep(t, rb % 2 != 0 ? -1 : 1); } else if (p == 4) fanW(t, 5, 0.28, T.sp * 0.9); else if (p == 2 || p == 6) aim(5, 0.2, T.sp); else if (p == 7) ring(10, s * 0.37, T.sp * 0.7); }
			else
			{
				if (p == 0 && player != null) { double px = clamp(player.x, 120, W - 120), py = clamp(player.y, 200, H - 90), tw = t, ta = t + SPB * T.wn; dive = new Dive { tw = tw, ta = ta, tb = ta + SPB * 0.5, te = ta + SPB * 1.5, px = px, py = py }; hazards.Add(DiveMark(HX, HY, px, py, tw, ta)); }
				else if (p == 5) fanW(t, 5, 0.28, T.sp * 0.9); else if (p == 6) { ring(14, s * 0.33, T.sp * 0.75); } else if (p == 7) aim(5, 0.2, T.sp);
			}
		};
		h.draw = t =>
		{
			double gr = grow(t); bool ch = t >= tc && t < tx; double jx = 0, jy = 0, s = 1; if (ch) { double k = (t - tc) / (tx - tc); s = 1 + 0.3 * k; jx = (random() * 2 - 1) * 7 * k; jy = (random() * 2 - 1) * 7 * k; }
			g.save(); g.translate(jx, jy); g.globalAlpha = t < ts + 2 * SPB ? 0.45 : 1;
			// the tail: three waving flames
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 20; for (int i = -1; i <= 1; i++) { double w2 = JS.sin(t * 6 + i) * 10; g.beginPath(); g.moveTo(x - 12 + i * 10, y + 20); g.quadraticCurveTo(x + i * 30 + w2, y + 80, x + i * 44 + w2, y + 130 + Math.Abs(i) * -20); g.quadraticCurveTo(x + i * 18, y + 70, x + 12 + i * 10, y + 20); g.fill(); }
			// the wings
			foreach (var sd in new double[] { -1, 1 }) { var q = root(sd); double rx = q[0], ry = q[1], a = wingA(t, sd); g.save(); g.translate(rx, ry); g.rotate(a); for (int k = 0; k < 5; k++) { double L = WL * (1 - k * 0.13) * s; g.save(); g.rotate(k * 0.13 * sd); g.beginPath(); g.moveTo(0, -8); g.quadraticCurveTo(L * 0.5, -24 + k * 4, L, 0); g.quadraticCurveTo(L * 0.5, 14, 0, 10); g.fill(); g.restore(); } g.restore(); }
			// body and head
			g.shadowBlur = ch ? 50 : 30; g.fillStyle = ch ? "#fff" : PINK; g.beginPath(); g.ellipse(x, y, 30 * s, 46 * s, 0, 0, TAU); g.fill(); circle(x, y - 52 * s, 20 * s);
			g.beginPath(); g.moveTo(x - 6, y - 48 * s); g.lineTo(x, y - 34 * s); g.lineTo(x + 6, y - 48 * s); g.fill();
			g.shadowBlur = 0; g.fillStyle = "#000"; circle(x - 7, y - 56 * s, 3.5); circle(x + 7, y - 56 * s, 3.5);
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ts + 2 * SPB || t >= tc) return false; if (hypot((p.x - x) / (30 + p.r), (p.y - y) / (46 + p.r)) < 1 || hypot(p.x - x, p.y - y + 52) < 20 + p.r) return true;
			foreach (var sd in new double[] { -1, 1 }) { var q = root(sd); double rx = q[0], ry = q[1], a = wingA(t, sd); if (obbHit(p.x, p.y, p.r - 1, rx + JS.cos(a) * WL * 0.5, ry + JS.sin(a) * WL * 0.5, WL * 0.5, 12, a)) return true; }
			return false;
		};
		return h;
	}

	// the dive mark: a faint line and a circle where it will dive
	public static Hz DiveMark(double x0, double y0, double x1, double y1, double tw, double ta)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > ta + 0.1) h.dead = true; };
		h.draw = t => { double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 10, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.16 * k)})"; circle(x1, y1, 60); g.strokeStyle = "rgba(255,42,109,0.8)"; g.lineWidth = 2; g.beginPath(); g.arc(x1, y1, 60 * (1.5 - 0.5 * k), 0, TAU); g.stroke(); };
		return h;
	}

	/* Runaway. The song starts at 0:32. 87 bpm (a beat is 0.69 s). Beat 8 = the drums come in (0:38.8)
	   part 1 0-51 | part 2 52-103 (1:09) | part 3 104-199 (1:45), the boss from 152 (2:18), the music cuts at 196 (2:48), it explodes at 199 (2:50), win 2:51 */
	public static List<Ev> buildLevel5()
	{
		var E = new List<Ev>(); var R = mulberry32(505);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f }); const int NK = 16; const double CW = W / NK;
		double rx(double a = 140, double b = W - 140) => a + R() * (b - a);
		double ry(double a = 120, double b = H - 170) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 335, wn = 1.1 }, new Tier { sp = 370, wn = 1.0 }, new Tier { sp = 400, wn = 0.95 } });
		void key(double hit, double col, Tier T, double dur = 0.8, bool black = false) => ev(hit - T.wn, () => hazards.Add(PianoKey(col, NK, B(hit - T.wn), B(hit), B(hit + dur), black)));
		void keyP(double hit, Tier T, double dur = 0.8, double d = 0) => ev(hit - T.wn, () => { double c = clamp(Math.Floor((player != null ? player.x : W / 2) / CW) + d, 0, NK - 1); hazards.Add(PianoKey(c, NK, B(hit - T.wn), B(hit), B(hit + dur), false)); });
		// glissando: keys one after another from side to side, with a two-column gap where nothing comes down
		void gliss(double b0, int dir, double gap, double step, Tier T) { for (int i = 0; i < NK; i++) { int c = dir > 0 ? i : NK - 1 - i; if (c == gap || c == gap + 1) continue; key(b0 + i * step, c, T, 0.75); } }
		void fw(double hit, double x, double y, int n, Tier T, double sp = 0) => ev(hit - T.wn, () => hazards.Add(Firework(x, y, n, B(hit - T.wn), B(hit), sp != 0 ? sp : T.sp)));
		void fwP(double hit, int n, Tier T) { double jx = (R() * 2 - 1) * 130, jy = (R() * 2 - 1) * 100; ev(hit - T.wn, () => { double x = clamp((player != null ? player.x : W / 2) + jx, 120, W - 120), y = clamp((player != null ? player.y : H / 2) + jy, 110, H - 170); hazards.Add(Firework(x, y, n, B(hit - T.wn), B(hit), T.sp)); }); }
		void toast(double hit, double x, double y, bool vert, Tier T, int n = 8) => ev(hit - T.wn - 0.6, () => hazards.Add(Toast(x, y, vert, B(hit - T.wn - 0.6), B(hit), n, T.sp * 0.9)));
		void toastP(double hit, bool vert, Tier T, int n = 8) => ev(hit - T.wn - 0.6, () => { double x = clamp(player != null ? player.x : W / 2, 200, W - 200), y = clamp(player != null ? player.y : H / 2, 150, H - 150); hazards.Add(Toast(x, y, vert, B(hit - T.wn - 0.6), B(hit), n, T.sp * 0.9)); });
		void dancer(double b0, double b1, Func<double, (double x, double y)> path, Tier T, double R0 = 80, double shed = 0) => ev(b0 - T.wn, () => hazards.Add(Ballerina(path, B(b0 - T.wn), B(b0), B(b1), R0, shed)));
		Func<double, (double x, double y)> line(double x0, double y0, double x1, double y1, double amp = 0, double wv = 2) => u => (x0 + (x1 - x0) * u - (y1 - y0) / hypot(x1 - x0, y1 - y0) * JS.sin(u * Math.PI * wv) * amp, y0 + (y1 - y0) * u + (x1 - x0) / hypot(x1 - x0, y1 - y0) * JS.sin(u * Math.PI * wv) * amp);
		Func<double, (double x, double y)> orbit(double cx, double cy, double rx2, double ry2, double a0, double turns) => u => (cx + JS.cos(a0 + u * turns * TAU) * rx2, cy + JS.sin(a0 + u * turns * TAU) * ry2);
		E.Add(new Ev { b = 0, keep = true, f = () => hazards.Add(KeyStrip(B(0), B(199.5), NK)) });

		/* ---------- part 1 (0:33 - 1:09) ---------- */
		var T = TI[0];
		// the calm piano: one key with each note
		foreach (var (b, c) in new[] { (1.0, 12.0), (3.0, 9.0), (5.0, 14.0), (7.0, 6.0) }) key(b, c, T, 1.2);
		ev(8, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		fw(8, W * 0.62, H * 0.42, 10, T);
		// keys on the snare, pairs from both sides closing in on the middle
		for (int k = 2; k < 6; k++) { double b = k * 4; int c = k - 1; key(b + 1, c, T); key(b + 1, NK - 1 - c, T); key(b + 3, c + 4, T); key(b + 3, NK - 5 - c, T); keyP(b + 2, T); if (k > 2) fw(b, rx(), ry(), 10, T); if (k > 3) fwP(b + 3, 8, T); }
		// two dancers passing
		dancer(24, 40, line(-80, H * 0.3, W + 80, H * 0.3, 40, 2), T, 80);
		dancer(26, 40, line(W + 80, H * 0.72, -80, H * 0.72, 40, 2), T, 80);
		for (int k = 6; k < 10; k++) { double b = k * 4; keyP(b + 3, T); keyP(b + 1, T, 0.8, k % 2 != 0 ? 2 : -2); fw(b, k % 2 != 0 ? W * 0.3 : W * 0.7, ry(150, H - 200), 9, T); fwP(b + 2, 7, T); }
		// a toast, then a glissando
		foreach (var (b, v) in new[] { (40.0, false), (44.0, true) }) { toastP(b, v, T); keyP(b + 1, T); keyP(b + 3, T); fw(b + 2, rx(), ry(), 7, T); }
		gliss(48, 1, 6 + Math.Floor(R() * 5), 0.25, T);
		// 1:09 the first checkpoint
		/* ---------- part 2 (1:09 - 1:45) ---------- */
		T = TI[1];
		for (int k = 13; k < 17; k++) { double b = k * 4; gliss(b, k % 2 != 0 ? -1 : 1, 2 + Math.Floor(R() * 11), 0.25, T); fwP(b + 3, 10, T); if (k % 2 == 0) fw(b + 1, k % 4 != 0 ? W * 0.25 : W * 0.75, H * 0.35, 10, T); }
		// three dancers circling the middle, throwing sequins
		for (int i = 0; i < 3; i++) dancer(68, 84, orbit(W / 2, H / 2 + 20, 360, 210, i * TAU / 3, 1), T, 95, T.sp * 0.6);
		for (int k = 17; k < 21; k++) { double b = k * 4; keyP(b + 3, T); keyP(b + 1, T, 0.8, k % 2 != 0 ? 2 : -2); if (k % 2 != 0) toast(b + 2, W / 2, H / 2 + 20, k % 4 == 1, T, 12); else { fw(b, W / 2, H / 2 + 20, 12, T); toastP(b + 2, k % 4 == 0, T, 10); } }
		// chords: white and black keys in turn
		for (int k = 21; k < 24; k++) { double b = k * 4; for (int c = k % 3; c < NK; c += 3) { key(b + 1, c, T, 0.8); key(b + 3, c + 1, T, 0.8, true); key(b + 2, c + 2, T, 0.6); } fwP(b, 10, T); fwP(b + 2, 9, T); }
		keyP(97, T); keyP(99, T); fw(96, W * 0.5, H * 0.4, 10, T);
		// the stop (1:42): calm, one big rocket
		fw(102, W / 2, H * 0.45, 16, T, T.sp * 0.8);
		// 1:45 the second checkpoint
		/* ---------- part 3 (1:45 - 2:51) ---------- */
		T = TI[2];
		ev(104, () => { flash = Math.Max(flash, 0.3); shake = Math.Max(shake, 12); });
		dancer(104, 120, line(-80, -60, W + 80, H + 60, 60, 3), T, 80, T.sp * 0.5);
		dancer(108, 120, line(W + 80, -60, -80, H + 60, 60, 3), T, 80);
		for (int k = 26; k < 30; k++) { double b = k * 4; fw(b, rx(), ry(), 12, T); fwP(b + 2, 10, T); double c = Math.Floor(R() * 12) + 2; key(b + 1, c, T); key(b + 1, c + 1, T, 0.8, true); keyP(b + 3, T); keyP(b + 1, T, 0.8, k % 2 != 0 ? 3 : -3); }
		for (int k = 30; k < 34; k++) { double b = k * 4; gliss(b, k % 2 != 0 ? -1 : 1, 2 + Math.Floor(R() * 11), 0.22, T); toastP(b + 2, k % 2 == 0, T, 10); fw(b + 3, k % 2 != 0 ? W * 0.7 : W * 0.3, H * 0.3, 10, T); }
		dancer(136, 152, line(-90, H * 0.5, W + 90, H * 0.5, 150, 2), T, 100, T.sp * 0.5);
		for (int k = 34; k < 38; k++) { double b = k * 4; fwP(b, 10, T); fwP(b + 2, 10, T); keyP(b + 1, T); keyP(b + 3, T, 0.8, k % 2 != 0 ? 2 : -2); }
		// the boss: the phoenix (2:18)
		var TT = T; ev(152, () => hazards.Add(Phoenix(B(152), B(196), B(199), TT)));
		dancer(168, 184, orbit(W / 2, H * 0.62, 420, 150, 0, 1), T, 90);
		for (int k = 38; k < 49; k++) { double b = k * 4; if (k < 48) { keyP(b + 1, T); keyP(b + 1, T, 0.8, 1); keyP(b + 3, T, 0.8, k % 2 != 0 ? 2 : -2); keyP(b + 3, T, 0.8, k % 2 != 0 ? 3 : -3); } if (k < 48) fwP(b + 2, 10, T); }
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
