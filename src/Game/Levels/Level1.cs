using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 205-303: the boss and buildLevel1 (Harder Things)
public static partial class G
{
	// the boss: comes down with the bass, charges before the second drop, explodes when the music cuts
	public static Hz Boss(double cx, double cy, double ts, double te)
	{
		int lastB = -1; bool ex = false;
		Func<double, (double x, double y)> pos = t => { double k = clamp((t - ts) / (2 * SPB), 0, 1), e = 1 - JS.pow(1 - k, 3); return (cx, -160 + (cy + 160) * e); };
		Action<int, double, double, double> ring = (n, bs, sp, r) =>
		{
			for (int i = 0; i < n; i++) { double a = bs + i * TAU / n; hazards.Add(Bullet(cx + JS.cos(a) * 60, cy + JS.sin(a) * 60, JS.cos(a) * sp, JS.sin(a) * sp, r)); }
		};
		Func<double, bool> charging = t => t >= B(258) && t < B(262);
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= te && !ex) { ex = true; ring(28, 0, 210, 10); burst(cx, cy, PINK, 70, 520); burst(cx, cy, "#fff", 35, 420); shake = 26; flash = 0.7; rumble(500, 1, 1); h.dead = true; return; }
			if (t < ts + 2 * SPB) return;
			int beat = (int)Math.Floor(BT(t) + 1e-6); if (beat == lastB) return; lastB = beat;
			if (beat < 258) ring(6, beat * 0.45, 185, 9);
			else if (beat < 262) { }
			else if (beat == 262) { ring(16, 0, 230, 10); flash = Math.Max(flash, 0.35); shake = Math.Max(shake, 14); rumble(300, 0.9, 0.7); }
			else if (beat == 263) { }
			else if (beat % 2 == 0) ring(8, beat * 0.37, 205, 9);
			else if (player != null)
			{
				double a0 = JS.atan2(player.y - cy, player.x - cx);
				for (int k = -1; k <= 1; k++) { double a = a0 + k * 0.25; hazards.Add(Bullet(cx + JS.cos(a) * 60, cy + JS.sin(a) * 60, JS.cos(a) * 265, JS.sin(a) * 265, 11)); }
			}
		};
		h.draw = t =>
		{
			var P = pos(t); bool ch = charging(t); double s = 1 + 0.12 * pulse; double jx = 0, jy = 0;
			if (ch) { double k = (t - B(258)) / (B(262) - B(258)); s = 1 + 0.25 * k; jx = (random() * 2 - 1) * 6 * k; jy = (random() * 2 - 1) * 6 * k; }
			g.save(); g.translate(P.x + jx, P.y + jy);
			g.shadowColor = PINK; g.shadowBlur = ch ? 50 : 34; g.fillStyle = PINK; g.save(); g.rotate(t * (ch ? 3 : 0.8)); poly(6, 70 * s); g.restore();
			g.shadowBlur = 0; g.fillStyle = ARENA; g.save(); g.rotate(-t * 1.6); poly(3, 36 * s); g.restore();
			g.fillStyle = ch ? "#fff" : PINK; circle(0, 0, 9 + 5 * pulse);
			g.globalAlpha = 0.5; g.strokeStyle = PINK; g.lineWidth = 2; g.save(); g.rotate(-t * 0.5); for (int i = 0; i < 4; i++) { g.rotate(TAU / 4); g.strokeRect(92, -7, 14, 14); } g.restore();
			g.restore();
		};
		h.hit = (p, t) => { if (t < ts + 2 * SPB) return false; var P = pos(t); return hypot(p.x - P.x, p.y - P.y) < 58 + p.r; };
		return h;
	}

	/* the song is 132 bpm, every 4 beats = one bar.
	   part 1: beats 0-99 | part 2: 100-195 | part 3: 196-end */
	public static List<Ev> buildLevel1()
	{
		var E = new List<Ev>(); var R = mulberry32(2026);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double EW = easy ? ezV("ezwn", EZ_WN) : 1, ES = easy ? ezV("ezsp", EZ_SP) : 1;
		Func<double, double, double> rx_ = (a, b) => a + R() * (b - a);
		Func<double, double, double> ry_ = (a, b) => a + R() * (b - a);
		double rx(double a = 120, double b = W - 120) => rx_(a, b);
		double ry(double a = 100, double b = H - 100) => ry_(a, b);
		void L(double hit, double warn, double dur, LaserO o)
		{
			warn *= EW;
			ev(hit - warn, () => { var q = o.Clone(); q.tw = B(hit - warn); q.ta = B(hit); q.te = B(hit + dur); hazards.Add(Laser(q)); });
		}
		void hL(double hit, double y, double w, double dur = 1, double warn = 1.5) => L(hit, warn, dur, new LaserO { x = W / 2, y = y, ang = 0, w = w });
		void vL(double hit, double x, double w, double dur = 1, double warn = 1.5) => L(hit, warn, dur, new LaserO { x = x, y = H / 2, ang = Math.PI / 2, w = w });
		void tile(double hit, double c, double r, double dur = 0.5, double warn = 1.5) => L(hit, warn, dur, new LaserO { x = c * 320 + 160, y = r * 240 + 120, ang = 0, w = 240, len = 320, tile = true });
		void bomb(double hit, double x, double y, int n = 6, double sp = 200, double warn = 2)
		{
			warn *= EW; sp *= ES;
			ev(hit - warn, () => hazards.Add(Bomb(x, y, B(hit - warn), B(hit), n, sp)));
		}
		void shot(double b, double x, double y, double vx, double vy, double r = 10) => ev(b, () => hazards.Add(Bullet(x, y, vx * ES, vy * ES, r)));
		void ring(double hit, double x, double y, double sp, double gs = 1.5, double warn = 1.5)
		{
			warn *= EW; sp *= ES; double gap = R() * TAU;
			ev(hit - warn, () => hazards.Add(Ring(x, y, B(hit), sp, 16, gap, gs, warn * SPB)));
		}
		var C = new[] { (0.0, 0.0), (W, 0.0), (W, H), (0.0, H) };
		void aimed(double b, int ci, double sp = 270)
		{
			ev(b, () =>
			{
				if (player == null) return; sp *= ES; var (x, y) = C[ci % 4];
				double a = JS.atan2(player.y - y, player.x - x); hazards.Add(Bullet(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 11));
			});
		}
		int lastTile = -1;
		(int, int) rtile() { int k; do { k = (int)Math.Floor(R() * 12); } while (k == lastTile); lastTile = k; return (k % 4, (int)Math.Floor(k / 4.0)); }

		/* ---------- part 1 (a bit easier) ---------- */
		// 0:00 the quiet intro
		bomb(8, rx(400), ry(), 5, 160); bomb(12, rx(400), ry(), 5, 160);
		for (int b = 16; b < 32; b += 4) bomb(b, rx(380), ry(), 5, 170);
		for (int b = 18; b < 32; b += 2) shot(b, W + 20, ry(50, H - 50), -240, 0);
		// 0:15 the bass comes in
		for (int b = 32; b < 64; b += 2) shot(b, W + 20, ry(50, H - 50), -(260 + R() * 40), 0);
		foreach (var b in new double[] { 36, 44, 52, 60 }) for (int i = 0; i < 3; i++) shot(b, rx(), -20, 0, 240);
		foreach (var b in new double[] { 34, 40, 46 }) bomb(b, rx(380), ry(), 6, 190);
		foreach (var b in new double[] { 48, 52, 56, 60 }) vL(b, rx(), 80, 0.75, 2);
		hL(62, ry(), 80, 0.75, 2);
		// 0:29 the strong beat
		for (int k = 0; k < 6; k++) { double b = 64 + k * 4; hL(b, ry(), 60, 0.75); vL(b + 2, rx(), 60, 0.75); shot(b + 1, W + 20, ry(50, H - 50), -300, 0); shot(b + 3, W + 20, ry(50, H - 50), -300, 0); }
		ring(76, W / 2, H / 2, 230, 1.6, 2); ring(84, W / 2, H / 2, 230, 1.6, 2);
		// 0:40 the build: floor tiles
		for (int b = 88; b < 92; b++) { var (c, r) = rtile(); tile(b, c, r); }
		for (int b = 92; b < 96; b++) { var (c, r) = rtile(); tile(b, c, r); (c, r) = rtile(); tile(b, c, r); }
		// 0:44 the break, nothing

		/* ---------- part 2 (same difficulty) ---------- */
		// 0:45 the first drop
		ev(100, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		for (int k = 25; k <= 32; k++)
		{
			double b = k * 4;
			if (k % 2 != 0) { vL(b, rx(), 70, 0.6); vL(b + 2, rx(), 70, 0.6); for (int i = 0; i < 4; i++) shot(b + i, W + 20, ry(50, H - 50), -320, 0); }
			else { hL(b, ry(), 55, 0.5); hL(b + 1, ry(), 55, 0.5); hL(b + 1.5, ry(), 55, 0.5); hL(b + 3, ry(), 55, 0.5); }
		}
		foreach (var b in new double[] { 100, 108, 116, 124 }) ring(b, W / 2, H / 2, 260);
		// 1:00 the choppy part: a spinner in the middle
		ev(132, () => hazards.Add(Spinner(new SpinnerO { x = W / 2, y = H / 2, arms = 3, L = 220, w = 20, ts = B(132), te = B(162), w1 = 0.6, tsw = B(148), w2 = -0.8 })));
		var SP = new[] { (200.0, 150.0), (W - 200, 150.0), (W - 200, H - 150), (200.0, H - 150) };
		for (int k = 33; k <= 40; k++)
		{
			double b = k * 4; var (x, y) = SP[k % 4]; bomb(b, x, y, 6, 190); aimed(b + 1.5, k + 2);
			if (k % 2 == 0) { tile(b + 2, R() < 0.5 ? 0 : 3, Math.Floor(R() * 3)); tile(b + 3.5, R() < 0.5 ? 0 : 3, Math.Floor(R() * 3)); }
		}
		// 1:15 the calm
		for (int k = 41; k <= 48; k++) { double b = k * 4; bomb(b, rx(300), ry(), 6, 150); if (k == 44 || k == 48) vL(b + 2, rx(), 80, 0.75); }
		for (int b = 165; b < 194; b += 2) shot(b, W + 20, ry(50, H - 50), -220, 0);
		for (int b = 180; b < 192; b++) shot(b, rx(), -20, 0, 200);

		/* ---------- part 3 (a bit harder) ---------- */
		// 1:29 the long build, the arena gets smaller
		for (int k = 49; k <= 56; k++)
		{
			double b = k * 4; hL(b, ry(), 60, 0.6); vL(b + 2, rx(), 60, 0.6); aimed(b + 1, k, 280);
			if (k == 52 || k == 56) for (int i = 0; i < 3; i++) shot(b + 3.5, W + 20, ry(80, H - 80), -330, 0);
		}
		L(214, 2, 14, new LaserO { x = 35, y = H / 2, ang = Math.PI / 2, w = 70 }); L(214, 2, 14, new LaserO { x = W - 35, y = H / 2, ang = Math.PI / 2, w = 70 });
		L(222, 2, 6, new LaserO { x = W / 2, y = 35, ang = 0, w = 70 }); L(222, 2, 6, new LaserO { x = W / 2, y = H - 35, ang = 0, w = 70 });
		// 1:44 the boss comes down
		ev(228, () => hazards.Add(Boss(W / 2, H / 2, B(228), B(294))));
		foreach (var b in new double[] { 232, 240, 248, 256 }) ring(b, W / 2, H / 2, 250);
		hL(236, 110, 200, 1, 2); hL(244, H - 110, 200, 1, 2); vL(252, 190, 340, 1, 2); vL(256, W - 190, 340, 1, 2);
		// 1:58 the second drop
		foreach (var b in new double[] { 264, 272, 280, 288 }) ring(b, W / 2, H / 2, 260);
		for (int k = 66; k <= 72; k++) { vL(k * 4 + 2, rx(100, W - 100), 70, 0.5); if (k % 2 != 0) hL(k * 4, ry(), 50, 0.5); }
		// 2:13 the ending
		bomb(300, rx(300), ry(), 5, 140); bomb(304, rx(300), ry(), 5, 140);
		foreach (var b in new double[] { 298, 302, 306 }) shot(b, W + 20, ry(60, H - 60), -220, 0);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
