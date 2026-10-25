using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 621-730: mind bloom (Leaf, Bloom, Vine, Checker, Seeder, MindFlower) and buildLevel4
public static partial class G
{
	static readonly int _afkL4 = AfkReg("buildLevel4", (x, y, b, v, w) => One(v >= 2 ? Checker(8, 5, v - 2, B(b - w), B(b), B(b + 0.6)) : Vine(u => v != 0 ? (x, -20 + (H + 40) * u) : (-20 + (W + 40) * u, y), B(b - w), B(b), B(b) + 0.25, B(b + 1))));

	public static Hz Leaf(double x, double y, double vx, double vy, double r = 9)
	{
		var b = Bullet(x, y, vx, vy, r); double a = JS.atan2(vy, vx);
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.rotate(a); double s = 1 + 0.15 * pulse; g.fillStyle = "rgba(255,42,109,0.22)"; g.beginPath(); g.ellipse(0, 0, 20 * s, 11 * s, 0, 0, TAU); g.fill(); g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, 14 * s, 7 * s, 0, 0, TAU); g.fill(); g.restore(); };
		return b;
	}

	// a flower opening: petals grow and spin, then fly away
	public static Hz Bloom(double x, double y, int n, double tw, double ta, double tr, double R, double sp, double rv = 1.2)
	{
		double rot0 = rnd() * TAU, pw = R * 0.32;
		Func<double, double> rot = t => rot0 + rv * Math.Max(0, t - ta), len = t => R * clamp((t - ta) / 0.15, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t >= tr) { double r = rot(tr); for (int i = 0; i < n; i++) { double a = r + i * TAU / n; hazards.Add(Leaf(x + JS.cos(a) * R * 0.5, y + JS.sin(a) * R * 0.5, JS.cos(a) * sp, JS.sin(a) * sp)); } burst(x, y, PINK, 10); h.dead = true; } };
		h.draw = t =>
		{
			g.save(); g.translate(x, y);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.rotate(rot0); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2;
				for (int i = 0; i < n; i++) { g.save(); g.rotate(i * TAU / n); g.beginPath(); g.ellipse(R / 2, 0, R / 2, pw / 2, 0, 0, TAU); g.stroke(); g.restore(); }
				g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; circle(0, 0, 10 * k + 4);
			}
			else
			{
				g.rotate(rot(t)); double l = len(t); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK;
				for (int i = 0; i < n; i++) { g.save(); g.rotate(i * TAU / n); g.beginPath(); g.ellipse(l / 2, 0, l / 2, pw / 2 * (1 + 0.12 * pulse), 0, 0, TAU); g.fill(); g.restore(); }
				g.shadowBlur = 0; g.fillStyle = "#000"; circle(0, 0, 12);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double r = rot(t), l = len(t); if (hypot(p.x - x, p.y - y) < 14 + p.r) return true;
			for (int i = 0; i < n; i++) { double a = r + i * TAU / n, cx = x + JS.cos(a) * l / 2, cy = y + JS.sin(a) * l / 2; double c = JS.cos(a), s = JS.sin(a), dx = p.x - cx, dy = p.y - cy, lx = (dx * c + dy * s) / (l / 2 + p.r), ly = (-dx * s + dy * c) / (pw / 2 + p.r); if (lx * lx + ly * ly < 1) return true; }
			return false;
		};
		return h;
	}

	// a vine growing along a path, staying a moment, then wilting
	public static Hz Vine(Func<double, (double x, double y)> path, double tw, double t0, double tg, double t1, double w = 18)
	{
		const int N = 48; var P = new double[N + 1][]; for (int i = 0; i <= N; i++) { var q = path((double)i / N); P[i] = new[] { q.x, q.y }; }
		Func<double, double> grown = t => clamp((t - t0) / (tg - t0), 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.3) h.dead = true; };
		h.draw = t =>
		{
			g.lineCap = "round"; g.lineJoin = "round";
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; for (int i = 0; i <= N; i += 2) circle(P[i][0], P[i][1], 3); return; }
			double gk = grown(t); int m = (int)Math.Max(1, Math.Floor(gk * N)); double a = t < t1 ? 1 : Math.Max(0, 1 - (t - t1) / 0.3);
			g.globalAlpha = a; g.strokeStyle = PINK; g.lineWidth = w * (t < t1 ? 1 + 0.12 * pulse : a); g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath(); g.moveTo(P[0][0], P[0][1]); for (int i = 1; i <= m; i++) g.lineTo(P[i][0], P[i][1]);
			g.stroke(); g.shadowBlur = 0;
			g.fillStyle = PINK; for (int i = 6; i <= m; i += 8) { double dx = P[i][0] - P[i - 1][0], dy = P[i][1] - P[i - 1][1], an = JS.atan2(dy, dx) + (i % 16 != 0 ? 1 : -1) * 1.1; g.save(); g.translate(P[i][0], P[i][1]); g.rotate(an); g.beginPath(); g.ellipse(14, 0, 14, 6, 0, 0, TAU); g.fill(); g.restore(); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; int m = (int)Math.Max(1, Math.Floor(grown(t) * N)); for (int i = 1; i <= m; i++) if (segDist(p.x, p.y, P[i - 1], P[i]) < w / 2 + p.r - 1) return true; return false; };
		return h;
	}

	// a checker grid: half the cells strike
	public static Hz Checker(int cols, int rows, int par, double tw, double ta, double te)
	{
		double cw = W / cols, ch = H / rows; Func<int, int, bool> bad = (i, j) => ((i + j) & 1) == par;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			double a = t < ta ? 0.07 + 0.2 * clamp((t - tw) / (ta - tw), 0, 1) : t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25);
			for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++)
				{
					if (!bad(i, j)) continue; double x = i * cw, y = j * ch;
					if (t < ta) { g.fillStyle = $"rgba(255,42,109,{str(a)})"; g.fillRect(x + 3, y + 3, cw - 6, ch - 6); g.strokeStyle = "rgba(255,42,109,0.7)"; g.lineWidth = 2; g.strokeRect(x + 10, y + 10, cw - 20, ch - 20); }
					else { g.globalAlpha = a; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; double s = t < te ? 0.92 + 0.08 * pulse : a; g.fillRect(x + cw * (1 - s) / 2, y + ch * (1 - s) / 2, cw * s, ch * s); g.globalAlpha = 1; }
				}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			foreach (var (dx, dy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) { double px = p.x + dx * (p.r - 1), py = p.y + dy * (p.r - 1); double i = Math.Floor(px / cw), j = Math.Floor(py / ch); if (i >= 0 && j >= 0 && i < cols && j < rows && bad((int)i, (int)j)) return true; }
			return false;
		};
		return h;
	}

	// a seed thrower firing leaves along spinning arms
	public static Hz Seeder(double x, double y, int arms, double t0, double t1, double step, double rv, double sp, double tw)
	{
		double last = -1;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= t1) { h.dead = true; return; }
			if (t < t0) return; double k = Math.Floor((t - t0) / step); if (k == last) return; last = k; double bs = rv * (t - t0);
			for (int i = 0; i < arms; i++) { double a = bs + i * TAU / arms; hazards.Add(Leaf(x + JS.cos(a) * 20, y + JS.sin(a) * 20, JS.cos(a) * sp, JS.sin(a) * sp, 8)); }
		};
		h.draw = t => { double k = t < t0 ? clamp((t - tw) / (t0 - tw), 0, 1) : 1; g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.35 * k)})"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, 16 + (t < t0 ? 24 * (1 - k) : 4 * pulse), 0, TAU); g.stroke(); };
		h.hit = (p, t) => false;
		return h;
	}

	// the boss: the mind flower in the middle, its petals spin and pulse with the beat and grow in number
	public static Hz MindFlower(double ts, double te, Tier T)
	{
		const double cx = W / 2, cy = H / 2, Lb = 230, pw = 34; double rot = 0; int lastS = -1; bool ex = false; double lastT = ts;
		Func<double, int> nAt = bar => bar < 60 ? 4 : bar < 68 ? 6 : 8;
		Func<double, double> grow = t => clamp((t - ts) / (2 * SPB), 0, 1);
		Func<double, double> env = t => { double ph = ((BT(t)) % 1 + 1) % 1; return JS.exp(-ph * 4); };
		Func<double, double> plen = t => Lb * grow(t) * (0.55 + 0.45 * env(t));
		void seeds(int n, double bs, double sp) { for (int i = 0; i < n; i++) { double a = bs + i * TAU / n; hazards.Add(Leaf(cx + JS.cos(a) * 60, cy + JS.sin(a) * 60, JS.cos(a) * sp, JS.sin(a) * sp, 8)); } }
		void bloomAt(double t, double px, double py) { px = clamp(px, 110, W - 110); py = clamp(py, 110, H - 110); hazards.Add(Bloom(px, py, 5, t, t + SPB * T.wn, t + SPB * (T.wn + 2), 130, T.sp * 0.8)); }
		void vine(double t)
		{
			if (player == null) return; int s = (int)Math.Floor(rnd() * 4); double u = rnd(); var st = s == 0 ? new[] { u * W, -20 } : s == 1 ? new[] { W + 20, u * H } : s == 2 ? new[] { u * W, H + 20 } : new[] { -20, u * H };
			double tx = player.x, ty = player.y, dx = tx - st[0], dy = ty - st[1], d = hypot(dx, dy); if (d == 0 || double.IsNaN(d)) d = 1; double nx = -dy / d, ny = dx / d, amp = 60 + rnd() * 60, ex2 = st[0] + dx / d * 1700, ey2 = st[1] + dy / d * 1700;
			hazards.Add(Vine(u2 => (st[0] + (ex2 - st[0]) * u2 + nx * JS.sin(u2 * Math.PI * 3) * amp, st[1] + (ey2 - st[1]) * u2 + ny * JS.sin(u2 * Math.PI * 3) * amp), t, t + SPB * T.wn, t + SPB * (T.wn + 1), t + SPB * (T.wn + 2)));
		}
		void fan(int n, double spr, double sp) { if (player == null) return; double a0 = JS.atan2(player.y - cy, player.x - cx); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spr; hazards.Add(Leaf(cx + JS.cos(a) * 60, cy + JS.sin(a) * 60, JS.cos(a) * sp, JS.sin(a) * sp)); } }
		var h = new Hz();
		h.update = (t, dt0) =>
		{
			double dt = t - lastT; lastT = t; double bar = Math.Floor(BT(t) / 4); rot += dt * (Math.Floor(bar / 8) % 2 != 0 ? -0.9 : 0.9);
			if (t >= te && !ex) { ex = true; seeds(32, 0, 230); burst(cx, cy, PINK, 80, 560); burst(cx, cy, "#fff", 40, 460); shake = 28; flash = 0.75; rumble(600, 1, 1); h.dead = true; return; }
			if (t < ts + 2 * SPB) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s; int p = s % 16;
			if (bar < 60) { if (p == 0 && player != null) bloomAt(t, player.x + (rnd() * 2 - 1) * 90, player.y + (rnd() * 2 - 1) * 90); else if (p == 8 && bar % 2 == 0) vine(t); else if (p == 4 || p == 12) seeds(3, s * 0.33, T.sp * 0.75); }
			else if (bar < 68) { if (p == 0 && player != null) bloomAt(t, player.x + (rnd() * 2 - 1) * 90, player.y + (rnd() * 2 - 1) * 90); else if (p == 8) vine(t); else if (p == 4 || p == 12) fan(3, 0.24, T.sp); else if (p == 6 || p == 14) seeds(5, s * 0.3, T.sp * 0.75); }
			else { if ((p == 0 || p == 8) && player != null) bloomAt(t, player.x + (rnd() * 2 - 1) * 110, player.y + (rnd() * 2 - 1) * 110); else if (p % 4 == 2) seeds(6, s * 0.27, T.sp * 0.75); }
		};
		h.draw = t =>
		{
			double gr = grow(t), bar = Math.Floor(BT(t) / 4); int n = nAt(bar); double l = plen(t); g.save(); g.translate(cx, cy);
			g.globalAlpha = t < ts + 2 * SPB ? 0.4 : 1; g.save(); g.rotate(rot); g.shadowColor = PINK; g.shadowBlur = 26; g.fillStyle = PINK;
			for (int i = 0; i < n; i++) { g.save(); g.rotate(i * TAU / n); g.beginPath(); g.ellipse(l / 2 + 20, 0, l / 2, pw / 2, 0, 0, TAU); g.fill(); g.restore(); }
			g.restore();
			g.shadowColor = PINK; g.shadowBlur = 36; g.fillStyle = PINK; circle(0, 0, 55 * gr * (1 + 0.08 * pulse)); g.shadowBlur = 0;
			g.fillStyle = "#000"; g.save(); g.rotate(-t * 0.8); for (int i = 0; i < 6; i++) { g.rotate(TAU / 6); g.beginPath(); g.ellipse(20 * gr, 0, 16 * gr, 7 * gr, 0, 0, TAU); g.fill(); }
			g.restore(); circle(0, 0, 10 * gr);
			g.globalAlpha = 1; g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ts + 2 * SPB) return false; if (hypot(p.x - cx, p.y - cy) < 55 + p.r - 2) return true; double bar = Math.Floor(BT(t) / 4); int n = nAt(bar); double l = plen(t);
			for (int i = 0; i < n; i++) { double a = rot + i * TAU / n, mx = cx + JS.cos(a) * (l / 2 + 20), my = cy + JS.sin(a) * (l / 2 + 20), c = JS.cos(a), s = JS.sin(a), dx = p.x - mx, dy = p.y - my, lx = (dx * c + dy * s) / (l / 2 + p.r), ly = (-dx * s + dy * c) / (pw / 2 + p.r); if (lx * lx + ly * ly < 1) return true; }
			return false;
		};
		return h;
	}

	/* 150 bpm (each bar 1.6 s). part 1 0-111 | part 2 112-207 | part 3 208-287, the boss is beaten at 288 */
	public static List<Ev> buildLevel4()
	{
		var E = new List<Ev>(); var R = mulberry32(404);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 130, double b = W - 130) => a + R() * (b - a);
		double ry(double a = 110, double b = H - 110) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 290, wn = 1.65 }, new Tier { sp = 320, wn = 1.5 }, new Tier { sp = 340, wn = 1.4 } });
		void bloom(double hit, double x, double y, int n, Tier T, double R0 = 130, double life = 2) => ev(hit - T.wn, () => hazards.Add(Bloom(x, y, n, B(hit - T.wn), B(hit), B(hit + life), R0, T.sp * 0.8)));
		void bloomP(double hit, int n, Tier T)
		{
			double jx = (R() * 2 - 1) * 80, jy = (R() * 2 - 1) * 80;
			ev(hit - T.wn, () => { double x = clamp((player != null ? player.x : W / 2) + jx, 110, W - 110), y = clamp((player != null ? player.y : H / 2) + jy, 110, H - 110); hazards.Add(Bloom(x, y, n, B(hit - T.wn), B(hit), B(hit + 2), 130, T.sp * 0.8)); });
		}
		void vine(double hit, Tier T, bool aimP = false)
		{
			int s = (int)Math.Floor(R() * 4); double u = 0.15 + R() * 0.7, amp = 50 + R() * 70, wav = 2 + Math.Floor(R() * 2), tx0 = rx(), ty0 = ry();
			ev(hit - T.wn, () =>
			{
				var st = s == 0 ? new[] { u * W, -20 } : s == 1 ? new[] { W + 20, u * H } : s == 2 ? new[] { u * W, H + 20 } : new[] { -20, u * H }; double tx = aimP && player != null ? player.x : tx0, ty = aimP && player != null ? player.y : ty0;
				double dx = tx - st[0], dy = ty - st[1], d = hypot(dx, dy); if (d == 0 || double.IsNaN(d)) d = 1; double nx = -dy / d, ny = dx / d, ex = st[0] + dx / d * 1700, ey = st[1] + dy / d * 1700;
				hazards.Add(Vine(q => (st[0] + (ex - st[0]) * q + nx * JS.sin(q * Math.PI * wav) * amp, st[1] + (ey - st[1]) * q + ny * JS.sin(q * Math.PI * wav) * amp), B(hit - T.wn), B(hit), B(hit + 1), B(hit + 2)));
			});
		}
		void checker(double hit, int par, Tier T, int cols = 8, int rows = 4, double dur = 0.75) => ev(hit - T.wn, () => hazards.Add(Checker(cols, rows, par, B(hit - T.wn), B(hit), B(hit + dur))));
		void seeder(double b0, double b1, double x, double y, int arms, double stepB, double rv, Tier T, double sp = 0) => ev(b0 - 1, () => hazards.Add(Seeder(x, y, arms, B(b0), B(b1), stepB * SPB, rv, sp != 0 ? sp : T.sp * 0.75, B(b0 - 1))));

		/* ---------- part 1 ---------- */
		var T = TI[0];
		for (int k = 1; k < 8; k++) { bloom(k * 4, rx(), ry(), k % 2 != 0 ? 4 : 5, T); if (k % 2 == 0) vine(k * 4 + 2, T); }
		for (int k = 8; k < 16; k++) { double b = k * 4; vine(b, T); if (k % 2 != 0) bloom(b + 2, rx(), ry(), 4, T); else bloomP(b + 2, 4, T); }
		bloom(60, W / 2, H / 2, 8, T, 170, 2);
		for (int k = 16; k < 28; k++) { double b = k * 4; if (k % 4 == 0) checker(b, k % 8 != 0 ? 1 : 0, T); else if (k % 2 != 0) vine(b, T); else bloomP(b, 5, T); if (k % 4 == 2) vine(b + 2, T, true); }
		// 0:44 the first checkpoint
		/* ---------- part 2 ---------- */
		T = TI[1];
		seeder(112, 136, W / 2, H / 2, 4, 0.5, 0.9, T, T.sp * 0.7);
		for (int k = 28; k < 34; k++) { double b = k * 4; vine(b + 2, T); if (k % 2 == 0) bloomP(b, 4, T); else bloom(b, rx(), ry(), 5, T); }
		foreach (var b in new double[] { 136, 140 }) { ev(b, () => { flash = Math.Max(flash, 0.22); shake = Math.Max(shake, 9); }); bloom(b, W / 2, H / 2, 8, T, 190, 1.5); checker(b + 2, (int)(b / 4) % 2, T); }
		for (int k = 36; k < 44; k++) { double b = k * 4; if (k % 2 == 0) { checker(b, k % 4 != 0 ? 1 : 0, T, 8, 4, 0.6); bloomP(b + 2, 4, T); vine(b + 3, T, true); } else { vine(b, T, true); bloom(b + 2, rx(), ry(), 5, T); } }
		seeder(176, 192, W * 0.3, H / 2, 3, 1, 1.1, T); seeder(176, 192, W * 0.7, H / 2, 3, 1, -1.1, T);
		for (int k = 44; k < 48; k++) { vine(k * 4 + 2, T); bloomP(k * 4, 5, T); }
		for (int k = 48; k < 52; k++) { double b = k * 4; bloomP(b, 5, T); bloomP(b + 2, 4, T); checker(b + 3, k % 2, T, 8, 4, 0.5); }
		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		ev(208, () => hazards.Add(MindFlower(B(208), B(288), TT)));
		for (int k = 54; k < 72; k += 4) checker(k * 4 + 2, k % 8 == 2 ? 0 : 1, T, 8, 4, 0.6);
		for (int k = 52; k < 68; k++) vine(k * 4 + 3, T, k % 2 == 0);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
