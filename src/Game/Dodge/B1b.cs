using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// B1 "الزعيم الأخير" made again (Mohammed 08:38 10-28: the attacks made new, the old way; the song, its length, how hard it is, the mark,
// the circles, the downed ring and online all stay). The core that pulled the old levels together has eaten the old arcade games too,
// and it plays them against you one by one, each on its part of the song, then it fights like a real final boss with a health bar:
//   pong (b2-30): balls cross from paddle to paddle on the bar, sparks fly back off the paddles;
//   snake (b30-64): a snake (two from b46) comes out of the core and crawls on the floor's grid to its apple, longer with each apple
//     (the apple bursts when eaten), while the core shoots at the marked one;
//   tetris (b64-88): the core drops pieces on the quiet part (their shadow shows where they land), two full rows clear, and it slams
//     the marked one's column;
//   invaders (b88-124): a fleet marches and steps down, shoots down its columns (faster from b104), bombs the marked one, then dives;
//   the road (b124-192): lanes of cars across the room; when the bass comes back only who is inside a circle is safe, and the circles
//     sit on the other side of the road each time;
//   the boss (b192-248): its health bar is up; on the drum it charges at the marked one and bursts where it lands, the bar drips;
//   the bar falls (b248-256): the bar comes down as a wall with a gap and another comes up with a gap on the other side;
//   all the games again, faster (b256-288): pong, snake, tetris, invaders, 8 beats each, with rings on the drum;
//   game over (b288-316): the screen fills cell by cell in a spiral to the middle while it shoots at the marked one; it breaks at b320.
// Beat 0 = the first bar (1.053 s). Every hazard is a pure function of time once it exists, so hit(p, t) is right for any later t.
// Aimed things are made by rnd events (the bot learns them only 0.32-0.40 s after they fire). The old design stays in B1.cs for the
// replays made before v52 (registered there as "B1_v51"); its drawing of the core, the mark, the circles and the break are used here too.
public static partial class G
{
	const double B1bC = 80;   // the floor's grid: 16 x 9 cells of 80 px
	static double B1bCx(int i) => i * B1bC + B1bC / 2;
	static double B1bCy(int j) => j * B1bC + B1bC / 2;
	static double B1bEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }

	// ---------- the core: where it is ----------
	// the fight (b192-250): its charges (made by the mark's events), the rest of the time a fixed path
	static List<(double T, double x0, double y0, double x1, double y1)> B1bCh = new();
	static (double x, double y) B1bCorePos(double bt)
	{
		if (bt < 62) return (640, 360);
		if (bt < 65) return (640, 360 - 290 * B1bEase((bt - 62) / 3));
		if (bt < 124) return (640, 70);
		if (bt < 126) return (640, 70 + 290 * B1bEase((bt - 124) / 2));
		if (bt < 127) return (640, 360);
		if (bt < 129) return (640, 360 - 300 * B1bEase((bt - 127) / 2));
		if (bt < 190) return (640, 60);
		if (bt < 192) return (640, 60 + 300 * B1bEase((bt - 190) / 2));
		if (bt < 250 && B1bCh.Count > 0)
		{
			(double x, double y) p = (640, 360);
			foreach (var c in B1bCh)
			{
				if (bt < c.T) break;
				double k = (bt - c.T) / 0.75; p = k >= 1 ? (c.x1, c.y1) : (c.x0 + (c.x1 - c.x0) * k * k, c.y0 + (c.y1 - c.y0) * k * k);
			}
			if (bt >= 248) { var l = B1bCh[^1]; double k = B1bEase((bt - 248) / 2); p = (l.x1 + (640 - l.x1) * k, l.y1 + (360 - l.y1) * k); }
			return p;
		}
		return (640, 360);
	}
	static double B1bCoreR(double bt) => bt < 0 ? 12 : bt < 8 ? 12 + 32 * bt / 8 : bt < 256 ? 44 : bt < 258 ? 44 + 10 * (bt - 256) / 2 : bt < 320 ? 54 : 0;

	// ---------- the room ----------
	static Hz B1bStage()
	{
		var h = new Hz { kind = "B1bStage", z = -1, veil = true };
		h.draw = t =>
		{
			double bt = BT(t);
			var gr = g.createRadialGradient(640, 360, 60, 640, 360, 780); gr.addColorStop(0, "#160818"); gr.addColorStop(1, "#05020a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the floor's grid (the games' board)
			g.strokeStyle = $"rgba(150,110,220,{str(0.06 + 0.06 * pulse)})"; g.lineWidth = 1; g.beginPath();
			for (int i = 1; i < 16; i++) { g.moveTo(i * B1bC, 0); g.lineTo(i * B1bC, H); }
			for (int j = 1; j < 9; j++) { g.moveTo(0, j * B1bC); g.lineTo(W, j * B1bC); }
			g.stroke();
			// the road: the lanes' lines
			double rd = bt < 126 ? 0 : bt < 128 ? (bt - 126) / 2 : bt < 190 ? 1 : bt < 192 ? 1 - (bt - 190) / 2 : 0;
			if (rd > 0)
			{
				g.fillStyle = $"rgba(40,40,60,{str(0.55 * rd)})"; g.fillRect(0, 120, W, 480);
				g.strokeStyle = $"rgba(230,230,240,{str(0.35 * rd)})"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, 120); g.lineTo(W, 120); g.moveTo(0, 600); g.lineTo(W, 600); g.stroke();
				g.setLineDash(new double[] { 30, 26 }); g.lineWidth = 2; g.beginPath(); for (int j = 1; j < 5; j++) { g.moveTo(0, 120 + 96 * j); g.lineTo(W, 120 + 96 * j); } g.stroke(); g.setLineDash(new double[0]);
			}
			// the boss's health bar (white: it does not hurt): full at b192, half at b248, then it breaks and falls (B1bBarWall)
			if (bt >= 190 && bt < 250.5)
			{
				double a = clamp((bt - 190) / 1.5, 0, 1) * (bt < 249.5 ? 1 : 1 - (bt - 249.5)), f = bt < 192 ? 1 : 1 - 0.5 * clamp((bt - 192) / 56, 0, 1);
				g.fillStyle = $"rgba(0,0,0,{str(0.6 * a)})"; g.fillRect(240, 22, 800, 22);
				g.fillStyle = $"rgba(255,255,255,{str(0.85 * a)})"; g.fillRect(244, 26, 792 * f, 14);
				g.strokeStyle = $"rgba(255,255,255,{str(a)})"; g.lineWidth = 2; g.strokeRect(240, 22, 800, 22);
				if (bt >= 248) { g.strokeStyle = $"rgba(20,0,10,{str(a)})"; g.lineWidth = 4; g.beginPath(); g.moveTo(640, 18); g.lineTo(628, 33); g.lineTo(652, 38); g.lineTo(640, 48); g.stroke(); }
			}
			var vg = g.createRadialGradient(640, 360, 300, 640, 360, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
		};
		return h;
	}

	// ---------- the core (it hurts; in the fight the charges hurt instead, B1bCharge) ----------
	static Hz B1bCore()
	{
		var h = new Hz { kind = "B1bCore" };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt >= 194 && bt < 250) return false;
			double r = B1bCoreR(bt); if (r <= 0) return false; var c = B1bCorePos(bt); return hypot(p.x - c.x, p.y - c.y) < r + p.r;
		};
		h.draw = t =>
		{
			double bt = BT(t), r = B1bCoreR(bt); if (r <= 0) return; var c = B1bCorePos(bt);
			// the pieces of the old levels flying in to it (b0-8, grey: they don't hurt)
			if (bt < 8) for (int i = 0; i < 12; i++)
			{
				double k = clamp((bt - i * 0.6) / 1.4, 0, 1); if (k <= 0 || k >= 1) continue; double a = i * 2.4, d = 700 * (1 - k * k);
				g.fillStyle = L15G(0.5); B1PieceShape(c.x + JS.cos(a) * d, c.y + JS.sin(a) * d * 0.7, 14, a + bt, i % 3);
			}
			B1CoreDraw(c.x, c.y, r, bt, 1);
		};
		return h;
	}

	// ---------- a shard: from (x0,y0) toward a at v px/s from t0; its path shows wn s before ----------
	static Hz B1bShot(double x0, double y0, double a, double v, double t0, double wn, double r = 9)
	{
		var h = new Hz { kind = "B1bShot" }; double ca = JS.cos(a), sa = JS.sin(a);
		(double x, double y) At(double t) { double d = v * (t - t0); return (x0 + ca * d, y0 + sa * d); }
		h.update = (t, dt) => { if (t > t0) { var q = At(t); if (q.x < -60 || q.x > W + 60 || q.y < -60 || q.y > H + 60) h.dead = true; } };
		h.hit = (p, t) => { if (t < t0) return false; var q = At(t); return hypot(p.x - q.x, p.y - q.y) < r + p.r; };
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - (t0 - wn)) / wn, 0, 1); if (k <= 0) return; L15Dash(x0, y0, x0 + ca * 260 * k, y0 + sa * 260 * k, L15G(0.18 + 0.4 * k), 2); return; }
			var q = At(t); if (q.x < -40 || q.x > W + 40 || q.y < -40 || q.y > H + 40) return;
			g.save(); g.translate(q.x, q.y); g.rotate(a); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
			g.beginPath(); g.moveTo(r * 1.6, 0); g.lineTo(0, r * 0.8); g.lineTo(-r * 1.2, 0); g.lineTo(0, -r * 0.8); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
	// a ring from (cx,cy) at T, v px/s, with a gap of half width gh around ga
	static Hz B1bRing(double cx, double cy, double T, double v, double ga, double gh, double th = 16)
	{
		var h = new Hz { kind = "B1bRing" };
		h.update = (t, dt) => { if (t > T && v * (t - T) > 1600) h.dead = true; };
		h.hit = (p, t) =>
		{
			if (t < T) return false; double r = v * (t - T), d = hypot(p.x - cx, p.y - cy); if (Math.Abs(d - r) >= th / 2 + p.r) return false;
			return Math.Abs(B1Ang(JS.atan2(p.y - cy, p.x - cx) - ga)) > gh;
		};
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - SPB)) / SPB, 0, 1); if (k <= 0) return; g.strokeStyle = L15G(0.2 + 0.4 * k); g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, 60 * k, ga + gh, ga + TAU - gh); g.stroke(); return; }
			double r = v * (t - T);
			g.strokeStyle = PINK; g.lineWidth = th; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.arc(cx, cy, r, ga + gh, ga + TAU - gh); g.stroke(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 3; g.beginPath();
			foreach (var e in new[] { ga + gh, ga - gh }) { g.moveTo(cx + JS.cos(e) * (r - th / 2), cy + JS.sin(e) * (r - th / 2)); g.lineTo(cx + JS.cos(e) * (r + th / 2), cy + JS.sin(e) * (r + th / 2)); }
			g.stroke();
		};
		return h;
	}
	// a beam straight down at x (its line shows from T - wn, it burns T .. T + 0.25 s)
	static Hz B1bBeam(double x, double T, double wn, double w = 56)
	{
		var h = new Hz { kind = "B1bBeam" }; const double HOLD = 0.25;
		h.update = (t, dt) => { if (t > T + 0.6) h.dead = true; if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 5); sfx(tt => sNoise(tt, 0.15, "bandpass", 1800, 0.1)); } };
		h.hit = (p, t) => t >= T && t < T + HOLD && Math.Abs(p.x - x) < w / 2 + p.r;
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; g.fillStyle = L15G(0.04 + 0.1 * k); g.fillRect(x - w / 2, 0, w, H); g.strokeStyle = L15G(0.25 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.moveTo(x - w / 2, 0); g.lineTo(x - w / 2, H); g.moveTo(x + w / 2, 0); g.lineTo(x + w / 2, H); g.stroke(); return; }
			double q = t < T + HOLD ? 1 : 1 - (t - T - HOLD) / 0.35; if (q <= 0) return;
			g.fillStyle = L15P(q); g.shadowColor = PINK; g.shadowBlur = 18; g.fillRect(x - w / 2, 0, w, H); g.shadowBlur = 0;
			g.fillStyle = $"rgba(255,230,240,{str(0.7 * q)})"; g.fillRect(x - w * 0.15, 0, w * 0.3, H);
		};
		return h;
	}

	// a row of the board (j) that burns T .. T + 0.25 s, its band shows from T - wn
	static Hz B1bRow(int j, double T, double wn)
	{
		var h = new Hz { kind = "B1bRow" }; const double HOLD = 0.25; double y = j * B1bC;
		h.update = (t, dt) => { if (t > T + 0.6) h.dead = true; if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 5); sfx(tt => sNoise(tt, 0.15, "bandpass", 1800, 0.1)); } };
		h.hit = (p, t) => t >= T && t < T + HOLD && p.y > y + 6 - p.r && p.y < y + B1bC - 6 + p.r;
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; g.fillStyle = L15G(0.04 + 0.1 * k); g.fillRect(0, y + 6, W, B1bC - 12); g.strokeStyle = L15G(0.25 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.moveTo(0, y + 6); g.lineTo(W, y + 6); g.moveTo(0, y + B1bC - 6); g.lineTo(W, y + B1bC - 6); g.stroke(); return; }
			double q = t < T + HOLD ? 1 : 1 - (t - T - HOLD) / 0.35; if (q <= 0) return;
			g.fillStyle = L15P(q); g.shadowColor = PINK; g.shadowBlur = 18; g.fillRect(0, y + 6, W, B1bC - 12); g.shadowBlur = 0;
			g.fillStyle = $"rgba(255,230,240,{str(0.7 * q)})"; g.fillRect(0, y + B1bC / 2 - 6, W, 12);
		};
		return h;
	}

	// ---------- pong: a ball between the paddles (faces at x 72 and 1208); it crosses in cb beats ----------
	const double B1bPL = 72, B1bPR = 1208, B1bBR = 16;
	static double B1bTri(double u, double lo, double hi) { double L = hi - lo, m = ((u % (2 * L)) + 2 * L) % (2 * L); return m < L ? lo + m : hi - (m - L); }
	// where the ball is at beat bt: it starts at the core (b0, y0) going right (dir 1) or left (-1), vy px a beat
	static (double x, double y) B1bBall(double b0, double y0, int dir, double cb, double vy, double bt)
	{
		double lo = B1bPL + B1bBR, hi = B1bPR - B1bBR, u = (640 - lo) + dir * (hi - lo) / cb * (bt - b0);
		return (B1bTri(u, lo, hi), B1bTri(y0 - (B1bBR + 4) + vy * (bt - b0), B1bBR + 4, H - B1bBR - 4));
	}
	// the beats when it reaches a paddle (and which side: 1 right, -1 left)
	static List<(double b, int side)> B1bHits(double b0, int dir, double cb, double b1)
	{
		var L = new List<(double, int)>(); double lo = B1bPL + B1bBR, hi = B1bPR - B1bBR, vx = (hi - lo) / cb;
		double first = b0 + (dir > 0 ? hi - 640 : 640 - lo) / vx;
		int s = dir; for (double b = first; b < b1; b += cb) { L.Add((b, s)); s = -s; }
		return L;
	}
	static Hz B1bPong(double b0, double y0, int dir, double cb, double vy, double b1)
	{
		var h = new Hz { kind = "B1bPong" }; var hits = B1bHits(b0, dir, cb, b1 + cb);
		// a paddle's middle at bt on a side: it glides from one hit's height to the next
		double Pad(int side, double bt)
		{
			(double b, double y)? a = null, c = null;
			foreach (var q in hits) { if (q.side != side) continue; double y = B1bBall(b0, y0, dir, cb, vy, q.b).y; if (q.b <= bt) a = (q.b, y); else { c = (q.b, y); break; } }
			if (a == null && c == null) return 360; if (a == null) return c.Value.y; if (c == null) return a.Value.y;
			return a.Value.y + (c.Value.y - a.Value.y) * B1bEase((bt - a.Value.b) / (c.Value.b - a.Value.b) * 1.6);
		}
		double In(double bt) => clamp((bt - (b0 - 1)) / 1, 0, 1) * (bt < b1 - 0.5 ? 1 : clamp((b1 - bt) / 0.5, 0, 1));
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; var q = B1bBall(b0, y0, dir, cb, vy, bt); if (hypot(p.x - q.x, p.y - q.y) < B1bBR + p.r) return true;
			if (bt < b1 - 0.5) foreach (int s in new[] { -1, 1 }) { double py = Pad(s, bt), px = s > 0 ? B1bPR + 9 : B1bPL - 9; if (Math.Abs(p.x - px) < 9 + p.r && Math.Abs(p.y - py) < 65 + p.r) return true; }
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t), a = In(bt); if (a <= 0) return;
			// the paddles
			foreach (int s in new[] { -1, 1 }) { double py = Pad(s, bt), px = s > 0 ? B1bPR + 9 : B1bPL - 9; g.fillStyle = bt < b0 ? L15G(0.5 * a) : bt < b1 - 0.5 ? PINK : L15G(0.5 * a); if (bt >= b0 && bt < b1 - 0.5) { g.shadowColor = PINK; g.shadowBlur = 10; } g.fillRect(px - 9, py - 65, 18, 130); g.shadowBlur = 0; }
			// the ball's way for the next beat
			var p0 = B1bBall(b0, y0, dir, cb, vy, Math.Max(bt, b0)); g.strokeStyle = L15G(0.35 * a); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); g.moveTo(p0.x, p0.y);
			for (int k = 1; k <= 12; k++) { var q = B1bBall(b0, y0, dir, cb, vy, Math.Max(bt, b0) + k / 12.0); g.lineTo(q.x, q.y); }
			g.stroke(); g.setLineDash(new double[0]);
			if (bt < b0 || bt >= b1) return;
			var b = B1bBall(b0, y0, dir, cb, vy, bt); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.fillRect(b.x - B1bBR, b.y - B1bBR, 2 * B1bBR, 2 * B1bBR); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,220,235,0.8)"; g.fillRect(b.x - 5, b.y - 5, 10, 10);
		};
		return h;
	}

	// ---------- snake: cells of the grid, each one a link from the cell before ----------
	// the snakes' way is worked out when the level is made (the same every run, so it can be learnt): each one goes for its apple
	// (white: it does not hurt) the shortest way around the bodies, grows by 2 for each apple, and the apple bursts when it is eaten
	class B1bSnk { public int i, j, dx, dy, len, ai = 0; public double b0, appear; public (int i, int j)[] apples; public (int i, int j, double l)? last; }
	static void B1bPlan(List<B1bSnk> SS, double st, double end, Action<int, int, int, int, double, double, double> cell, Action<int, int, double, double> apple)
	{
		var occ = new List<(int i, int j, double a, double l)>();
		bool Free(int i, int j, double b)
		{
			if (i < 0 || i > 15 || j < 0 || j > 8) return false;
			if (j == 4 && (i == 7 || i == 8)) return false;   // the core
			foreach (var o in occ) if (o.i == i && o.j == j && b >= o.a - 0.6 && b < o.l + 0.6) return false;
			return true;
		}
		double b0 = SS.Min(x => x.b0);
		for (double T = b0; T < end - 1e-6; T += st)
			foreach (var S in SS)
			{
				if (T < S.b0 - 1e-6) continue;
				var ap = S.apples[S.ai % S.apples.Length];
				// how far every cell is from the apple, going round what is taken now
				var dist = new int[16, 9]; for (int x = 0; x < 16; x++) for (int y = 0; y < 9; y++) dist[x, y] = 999;
				var q = new Queue<(int, int)>(); dist[ap.i, ap.j] = 0; q.Enqueue((ap.i, ap.j));
				while (q.Count > 0) { var (x, y) = q.Dequeue(); foreach (var (ex, ey) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int nx = x + ex, ny = y + ey; if (nx < 0 || nx > 15 || ny < 0 || ny > 8 || dist[nx, ny] <= dist[x, y] + 1 || !Free(nx, ny, T)) continue; dist[nx, ny] = dist[x, y] + 1; q.Enqueue((nx, ny)); } }
				(int x, int y)[] opt = { (S.dx, S.dy), (-S.dy, S.dx), (S.dy, -S.dx) }; (int x, int y)? best = null; int bd = int.MaxValue;
				foreach (var o in opt) { int ni = S.i + o.x, nj = S.j + o.y; if (!Free(ni, nj, T)) continue; int d = dist[ni, nj]; if (d < bd) { bd = d; best = o; } }
				if (best == null) foreach (var o in opt) { int ni = S.i + o.x, nj = S.j + o.y; if (ni >= 0 && ni <= 15 && nj >= 0 && nj <= 8) { best = o; break; } }
				if (best == null) best = (-S.dx, -S.dy);
				int pi = S.i, pj = S.j; S.dx = best.Value.x; S.dy = best.Value.y; S.i += S.dx; S.j += S.dy;
				if (S.i == ap.i && S.j == ap.j) { apple(ap.i, ap.j, S.appear, T); S.ai++; S.len += 2; S.appear = T; }
				double l = Math.Min(T + S.len * st, end), lp = S.last == null ? -1 : S.last.Value.l;
				occ.Add((S.i, S.j, T, l)); cell(pi, pj, S.i, S.j, T, l, lp); S.last = (S.i, S.j, l);
			}
		// the apples not eaten yet show until the end
		foreach (var S in SS) { var ap = S.apples[S.ai % S.apples.Length]; apple(ap.i, ap.j, S.appear, -end); }
	}
	// an apple at (i,j) from beat a until it is eaten at e (e < 0: not eaten, gone at -e)
	static Hz B1bApple(int i, int j, double a, double e)
	{
		var h = new Hz { kind = "B1bApple" }; double x = B1bCx(i), y = B1bCy(j), until = Math.Abs(e);
		h.update = (t, dt) => { if (BT(t) > until + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < a || bt >= until) return; double k = clamp((bt - a) / 0.3, 0, 1) * (1 + 0.08 * pulse);
			g.fillStyle = "rgba(255,255,255,0.9)"; g.fillRect(x - 13 * k, y - 11 * k, 26 * k, 24 * k); g.fillRect(x - 2, y - 19 * k, 4, 8 * k);
			g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; g.strokeRect(x - 30, y - 30, 60, 60);
		};
		return h;
	}
	// a cell (i,j) reached from (pi,pj) at beat a, left at l; the link from the cell before goes when that one is left (lp)
	static Hz B1bCell(int pi, int pj, int i, int j, double a, double l, double lp, double st, double end)
	{
		var h = new Hz { kind = "B1bSnake" }; double x0 = B1bCx(pi), y0 = B1bCy(pj), x1 = B1bCx(i), y1 = B1bCy(j); const double R = 28;
		// the link's two ends at bt (or none)
		(double ax, double ay, double bx, double by)? Seg(double bt)
		{
			if (bt < a - st || bt >= l || bt >= end) return null;
			double k = bt < a ? (bt - (a - st)) / st : 1, ex = x0 + (x1 - x0) * k, ey = y0 + (y1 - y0) * k;
			double s = lp < 0 ? 0 : bt < lp ? 0 : clamp((bt - lp) / st, 0, 1); if (lp < 0 && bt < a) s = 0;
			return (x0 + (x1 - x0) * s, y0 + (y1 - y0) * s, ex, ey);
		}
		h.update = (t, dt) => { if (BT(t) > Math.Min(l, end) + 0.5) h.dead = true; };
		h.hit = (p, t) => { var s = Seg(BT(t)); return s != null && L15Seg(p.x, p.y, s.Value.ax, s.Value.ay, s.Value.bx, s.Value.by) < R + p.r; };
		h.draw = t =>
		{
			double bt = BT(t);
			// where the head goes next: a grey cell from the moment it is decided
			if (bt < a - st) { double k = clamp((bt - (a - st - 0.5)) / 0.5, 0, 1); g.strokeStyle = L15G(0.25 + 0.45 * k); g.lineWidth = 3; g.strokeRect(x1 - 30, y1 - 30, 60, 60); return; }
			if (bt >= end && bt < end + 0.5) { double q = 1 - (bt - end) / 0.5; g.fillStyle = L15G(0.4 * q); circle(x1, y1, R * q); return; }
			var s = Seg(bt); if (s == null) return; var v = s.Value;
			g.strokeStyle = PINK; g.lineWidth = 2 * R; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.moveTo(v.ax, v.ay); g.lineTo(v.bx, v.by); g.stroke(); g.shadowBlur = 0; g.lineCap = "butt";
			// the head's eyes (while this is the head)
			if (bt < a + st * 0.999 && bt >= a - st)
			{
				double ang = JS.atan2(y1 - y0, x1 - x0); g.fillStyle = "rgba(40,0,14,0.85)";
				for (int e = -1; e <= 1; e += 2) circle(v.bx + JS.cos(ang) * 10 + JS.cos(ang + Math.PI / 2) * 12 * e, v.by + JS.sin(ang) * 10 + JS.sin(ang + Math.PI / 2) * 12 * e, 5);
			}
		};
		return h;
	}

	// ---------- tetris: a piece's cells drop at T (the shadow shows from T - wn) and stay until clear ----------
	static Hz B1bPiece((int i, int j)[] cells, double T, double wn, double clear)
	{
		var h = new Hz { kind = "B1bPiece" }; const double DROP = 0.1; int top = cells.Min(c => c.j);
		h.update = (t, dt) =>
		{
			if (t > B(clear) + 0.5) h.dead = true;
			if (!h.ex && t >= T + DROP) { h.ex = true; shake = Math.Max(shake, 7); foreach (var c in cells) burst(B1bCx(c.i), B1bCy(c.j) + 40, PINK, 3, 220); sfx(tt => sNoise(tt, 0.2, "lowpass", 500, 0.18)); }
			if (h.o == null && t >= B(clear)) { h.o = new(); flash = Math.Max(flash, 0.12); foreach (var c in cells) burst(B1bCx(c.i), B1bCy(c.j), "#ffffff", 4, 300); }
		};
		h.hit = (p, t) =>
		{
			if (t < T || t >= B(clear)) return false;
			foreach (var c in cells)
			{
				double x = c.i * B1bC, y = c.j * B1bC; if (p.x <= x - p.r || p.x >= x + B1bC + p.r) continue;
				double y0 = t < T + DROP ? -B1bC : y; if (p.y > y0 - p.r && p.y < y + B1bC + p.r) return true;   // falling: its column down to where it lands
			}
			return false;
		};
		h.draw = t =>
		{
			if (t >= B(clear)) return;
			if (t < T)
			{
				double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return;
				foreach (var c in cells) { g.fillStyle = L15G(0.05 + 0.12 * k); g.fillRect(c.i * B1bC, 0, B1bC, c.j * B1bC); g.strokeStyle = L15G(0.3 + 0.5 * k); g.lineWidth = 3; g.strokeRect(c.i * B1bC + 5, c.j * B1bC + 5, B1bC - 10, B1bC - 10); }
				return;
			}
			double f = t < T + DROP ? (t - T) / DROP : 1, dy = -(top + 1) * B1bC * (1 - f);
			foreach (var c in cells)
			{
				double x = c.i * B1bC, y = c.j * B1bC + dy;
				if (f < 1) { g.fillStyle = PINK; g.fillRect(x, 0, B1bC, c.j * B1bC + B1bC); }
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8; g.fillRect(x + 2, y + 2, B1bC - 4, B1bC - 4); g.shadowBlur = 0;
				g.fillStyle = "rgba(255,200,220,0.55)"; g.fillRect(x + 8, y + 8, B1bC - 16, 8); g.fillStyle = "rgba(60,0,20,0.4)"; g.fillRect(x + 8, y + B1bC - 16, B1bC - 16, 8);
			}
		};
		return h;
	}

	// ---------- invaders: a fleet of 8 x 3 that marches (a step every beat, every half beat from sb2) and steps down at each side ----------
	static (double x, double y) B1bFleetAt(double b0, double sb2, double y0, double bt)
	{
		int n = bt < b0 ? 0 : bt < sb2 ? (int)Math.Floor(bt - b0) : (int)Math.Floor(sb2 - b0) + (int)Math.Floor((bt - sb2) * 2);
		int pass = n / 10, w = n % 10; return (pass % 2 == 0 ? 70 + w * 37 : 440 - w * 37, y0 + pass * 30);
	}
	static (double x, double y) B1bAlien(double b0, double sb2, double y0, int c, int r, double bt) { var f = B1bFleetAt(b0, sb2, y0, bt); return (f.x + c * 110 + 24, f.y + r * 64 + 17); }
	// it comes in at b0 - 2 (from above), shoots from b0, dives at dv (each column falls, 1 beat of warning), gone at b1
	static Hz B1bFleet(double b0, double sb2, double y0, double dv, double b1)
	{
		var h = new Hz { kind = "B1bFleet" };
		double Fall(double bt, int c) { double s = dv + c * 0.125; return bt < s ? 0 : 900 * (B(bt) - B(s)); }
		double In(double bt) => B1bEase((bt - (b0 - 2)) / 2);
		(double x, double y) At(int c, int r, double bt) { var q = B1bAlien(b0, sb2, y0, c, r, Math.Min(bt, dv)); return (q.x, q.y - (1 - In(bt)) * 300 + Fall(bt, c)); }
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 - 0.5 || bt >= b1) return false;
			for (int c = 0; c < 8; c++) for (int r = 0; r < 3; r++) { var q = At(c, r, bt); if (Math.Abs(p.x - q.x) < 24 + p.r && Math.Abs(p.y - q.y) < 17 + p.r) return true; }
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2 || bt >= b1) return; bool live = bt >= b0 - 0.5;
			// the dive's lanes (from a beat before)
			if (bt >= dv - 1 && bt < dv + 1) for (int c = 0; c < 8; c++) { var q = At(c, 2, Math.Min(bt, dv)); double k = clamp(bt - (dv - 1), 0, 1); g.fillStyle = L15G(0.05 + 0.1 * k); g.fillRect(q.x - 24, q.y, 48, H - q.y); }
			for (int c = 0; c < 8; c++) for (int r = 0; r < 3; r++)
			{
				var q = At(c, r, bt); if (q.y > H + 40) continue; bool fr = ((int)Math.Floor(bt)) % 2 == 0;
				g.save(); g.translate(q.x, q.y); g.fillStyle = live ? PINK : L15G(0.5); if (live) { g.shadowColor = PINK; g.shadowBlur = 8; }
				// a little pixel alien: body, eyes cut out, legs that switch on the beat
				g.fillRect(-16, -12, 32, 18); g.fillRect(-24, -4, 48, 8); g.fillRect(-8, -17, 16, 6);
				if (fr) { g.fillRect(-24, 4, 8, 12); g.fillRect(16, 4, 8, 12); } else { g.fillRect(-16, 6, 8, 11); g.fillRect(8, 6, 8, 11); }
				g.shadowBlur = 0; g.fillStyle = "rgba(20,0,10,0.9)"; g.fillRect(-10, -6, 6, 6); g.fillRect(4, -6, 6, 6); g.restore();
			}
		};
		return h;
	}
	// a shot from an alien at T straight down (its column shows from T - wn)
	static Hz B1bBolt(double x, double y0, double T, double wn, double v = 560)
	{
		var h = new Hz { kind = "B1bBolt" };
		h.update = (t, dt) => { if (t > T && y0 + v * (t - T) > H + 40) h.dead = true; };
		h.hit = (p, t) => { if (t < T) return false; double y = y0 + v * (t - T); return Math.Abs(p.x - x) < 5 + p.r && p.y > y - 14 - p.r && p.y < y + 14 + p.r; };
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; L15Dash(x, y0, x, H, L15G(0.12 + 0.35 * k), 2); return; }
			double y = y0 + v * (t - T); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
			g.beginPath(); g.moveTo(x - 5, y - 14); g.lineTo(x + 5, y - 7); g.lineTo(x - 5, y); g.lineTo(x + 5, y + 7); g.lineTo(x, y + 14); g.lineTo(x - 5, y + 7); g.closePath(); g.fill(); g.fillRect(x - 3, y - 14, 6, 28); g.shadowBlur = 0;
		};
		return h;
	}

	// ---------- the road: a lane of cars (centre y), its offset in px from the table S (L15Spin used as px a beat) ----------
	static Hz B1bLane(double y, L15Spin S, double len, double gap, double b0, double b1)
	{
		var h = new Hz { kind = "B1bLane" }; double per = len + gap; const double HH = 32;
		IEnumerable<double> Cars(double bt) { double o = ((S.At(bt) % per) + per) % per; for (double x = o - per; x < W + per; x += per) yield return x; }
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1 || Math.Abs(p.y - y) >= HH + p.r) return false;
			foreach (var x in Cars(bt)) if (p.x > x - p.r && p.x < x + len + p.r) return true; return false;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2 || bt >= b1 + 0.4) return; bool live = bt >= b0 && bt < b1;
			double a = bt < b0 ? clamp((bt - (b0 - 2)) / 2, 0, 1) * 0.5 : bt < b1 ? 1 : 1 - (bt - b1) / 0.4;
			// which way it goes now, and a beat before it turns: white arrows on the lane's edge (they don't hurt)
			double w = S.W(bt + 1); double dir = Math.Sign(Math.Abs(w) < 1e-6 ? S.W(bt) : w);
			g.fillStyle = $"rgba(255,255,255,{str(0.35 * Math.Min(1, a * 2))})";
			for (double x = 40; x < W; x += 160) { g.beginPath(); g.moveTo(x + 14 * dir, y - 44); g.lineTo(x - 10 * dir, y - 38); g.lineTo(x - 10 * dir, y - 50); g.closePath(); g.fill(); }
			foreach (var x in Cars(bt))
			{
				if (x > W || x + len < 0) continue;
				g.fillStyle = live ? L15P(a) : L15G(a); if (live) { g.shadowColor = PINK; g.shadowBlur = 10; }
				rr(x, y - HH, len, 2 * HH, 14); g.fill(); g.shadowBlur = 0;
				g.fillStyle = $"rgba(40,0,14,{str(0.5 * a)})"; g.fillRect(x + 14, y - HH + 8, 26, 2 * HH - 16); g.fillRect(x + len - 40, y - HH + 8, 26, 2 * HH - 16);
				g.fillStyle = $"rgba(255,240,200,{str(0.8 * a)})"; double fx = dir > 0 ? x + len - 6 : x + 2; g.fillRect(fx, y - HH + 6, 4, 12); g.fillRect(fx, y + HH - 18, 4, 12);
			}
		};
		return h;
	}

	// ---------- the fight: a charge of the core at the marked one; it hurts on its way and where it rests until the next one ----------
	static Hz B1bCharge(int k, double T, double x0, double y0, double x1, double y1, double until)
	{
		var h = new Hz { kind = "B1bCharge" }; const double R = 44;
		(double x, double y) At(double bt) { if (bt >= 248) { double q = B1bEase((bt - 248) / 2); return (x1 + (640 - x1) * q, y1 + (360 - y1) * q); } double k2 = clamp((bt - T) / 0.75, 0, 1); return (x0 + (x1 - x0) * k2 * k2, y0 + (y1 - y0) * k2 * k2); }
		h.update = (t, dt) =>
		{
			double bt = BT(t); if (bt > until + 0.2) h.dead = true;
			if (!h.ex && bt >= T + 0.75) { h.ex = true; shake = Math.Max(shake, 10); burst(x1, y1, PINK, 18, 460); sfx(tt => sNoise(tt, 0.2, "lowpass", 500, 0.18)); }
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < T || bt >= until) return false; var q = At(bt); return hypot(p.x - q.x, p.y - q.y) < R + p.r; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt >= T) return;
			double k2 = clamp((bt - (T - 1.5)) / 1.5, 0, 1); if (k2 <= 0) return;
			// its way: a grey band as wide as it is, and the ring where it will land
			double a = JS.atan2(y1 - y0, x1 - x0), L = hypot(x1 - x0, y1 - y0);
			g.save(); g.translate(x0, y0); g.rotate(a); g.fillStyle = L15G(0.05 + 0.12 * k2); g.fillRect(0, -R, L, 2 * R); g.strokeStyle = L15G(0.25 + 0.45 * k2); g.lineWidth = 2; g.strokeRect(0, -R, L, 2 * R); g.restore();
			g.strokeStyle = L15G(0.3 + 0.5 * k2); g.lineWidth = 3; g.beginPath(); g.arc(x1, y1, R, 0, TAU); g.stroke();
		};
		return h;
	}

	// ---------- the bar falls: a wall across the room (top or bottom) with a gap, from b0 to b1 ----------
	static Hz B1bBarWall(bool down, double b0, double b1, double gx, double gw)
	{
		var h = new Hz { kind = "B1bWall" }; const double TH = 26;
		double Y(double bt) { double k = clamp((bt - b0) / (b1 - b0), 0, 1); return down ? 33 + (H + 40 - 33) * k : H - 20 - (H + 20) * k; }
		h.update = (t, dt) => { if (BT(t) > b1 + 0.3) h.dead = true; };
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1) return false; double y = Y(bt); return Math.Abs(p.y - y) < TH / 2 + p.r && Math.Abs(p.x - gx) > gw / 2 - p.r; };
		h.draw = t =>
		{
			double bt = BT(t);
			if (bt < b0) { double k = clamp((bt - (b0 - 1.5)) / 1.5, 0, 1); if (k <= 0) return; double y0 = Y(b0); g.fillStyle = L15G(0.1 + 0.3 * k); g.fillRect(0, y0 - TH / 2, gx - gw / 2, TH); g.fillRect(gx + gw / 2, y0 - TH / 2, W, TH); return; }
			if (bt >= b1) return; double y = Y(bt);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.fillRect(0, y - TH / 2, gx - gw / 2, TH); g.fillRect(gx + gw / 2, y - TH / 2, W - gx - gw / 2, TH); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.8)"; g.fillRect(gx - gw / 2 - 3, y - TH / 2, 3, TH); g.fillRect(gx + gw / 2, y - TH / 2, 3, TH);
		};
		return h;
	}

	// ---------- game over: the cells fill one by one (each shows a beat before) until b2 ----------
	static Hz B1bFill(List<(int i, int j, double b)> cells, double b2)
	{
		var h = new Hz { kind = "B1bFill" };
		h.update = (t, dt) => { if (BT(t) > b2 + 0.6) h.dead = true; if (!h.ex && BT(t) >= b2) { h.ex = true; flash = Math.Max(flash, 0.2); } };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt >= b2) return false; int ci = (int)Math.Floor(p.x / B1bC), cj = (int)Math.Floor(p.y / B1bC);
			foreach (var c in cells) { if (bt < c.b || Math.Abs(c.i - ci) > 1 || Math.Abs(c.j - cj) > 1) continue; double x = c.i * B1bC, y = c.j * B1bC; if (p.x > x - p.r && p.x < x + B1bC + p.r && p.y > y - p.r && p.y < y + B1bC + p.r) return true; }
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t); double fade = bt < b2 ? 1 : 1 - (bt - b2) / 0.6; if (fade <= 0) return;
			foreach (var c in cells)
			{
				double x = c.i * B1bC, y = c.j * B1bC;
				if (bt < c.b) { if (bt < c.b - 1 || bt >= b2) continue; double k = bt - (c.b - 1); g.fillStyle = L15G(0.05 + 0.15 * k); g.fillRect(x + 3, y + 3, B1bC - 6, B1bC - 6); g.strokeStyle = L15G(0.3 + 0.4 * k); g.lineWidth = 2; g.strokeRect(x + 8, y + 8, B1bC - 16, B1bC - 16); continue; }
				if (bt < b2) { g.fillStyle = PINK; g.fillRect(x, y, B1bC, B1bC); g.fillStyle = "rgba(60,0,20,0.35)"; g.fillRect(x + 6, y + 6, B1bC - 12, B1bC - 12); g.fillStyle = PINK; g.fillRect(x + 14, y + 14, B1bC - 28, B1bC - 28); }
				else { g.fillStyle = L15G(0.4 * fade); g.fillRect(x, y, B1bC, B1bC); }
			}
		};
		return h;
	}

	// ---------- online: a downed square sits in a pink ring with a gap that turns; the core throws at it every 2 beats ----------
	public static Hz B1bDownRing(int s, double t0)
	{
		var h = new Hz { kind = "B1bDown" }; double nextB = Math.Ceiling(BT(t0) + 1);
		bool on() => net?.boss == true && net.bdown[s] && state == "play";
		(double x, double y) at() => net.bdpos[s];
		h.update = (t, dt) =>
		{
			if (!on()) { h.dead = true; return; }
			double bt = BT(t);
			if (bt >= nextB) { nextB += 2; var c = at(); var o = B1bCorePos(bt); if (B1bCoreR(bt) > 0) hazards.Add(B1bShot(o.x, o.y, JS.atan2(c.y - o.y, c.x - o.x), 380, B(bt + 1), SPB)); }
		};
		h.hit = (p, t) =>
		{
			if (t < t0 + 0.8 || !on() || s == bnMe || bnMeDown) return false; var c = at(); double d = hypot(p.x - c.x, p.y - c.y);
			if (Math.Abs(d - 44) >= 5 + p.r) return false; return Math.Abs(B1Ang(JS.atan2(p.y - c.y, p.x - c.x) - t * 1.6)) > 0.55;
		};
		h.draw = t =>
		{
			if (!on()) return; var c = at(); double k = clamp((t - t0) / 0.8, 0, 1), ga = t * 1.6;
			g.strokeStyle = k < 1 ? L15G(0.3 + 0.4 * k) : PINK; g.lineWidth = 10; if (k >= 1) { g.shadowColor = PINK; g.shadowBlur = 10; }
			g.beginPath(); g.arc(c.x, c.y, 44, ga + 0.55, ga + TAU - 0.55); g.stroke(); g.shadowBlur = 0;
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _b1b = DodgeBuild("B1", () => buildB1b());
	// the tetris rows: 8 pieces fill the two bottom rows (left to right), then 4 more lie on them
	static readonly (int i, int j)[][] B1bFILL =
	{
		new[] { (0, 7), (1, 7), (0, 8), (1, 8) },            // O
		new[] { (2, 8), (3, 8), (4, 8), (5, 8) },            // I
		new[] { (2, 7), (3, 7), (4, 7), (5, 7) },            // I
		new[] { (6, 7), (6, 8), (7, 8), (8, 8) },            // J
		new[] { (7, 7), (8, 7), (9, 7), (9, 8) },            // L
		new[] { (10, 7), (11, 7), (10, 8), (11, 8) },        // O
		new[] { (12, 8), (13, 8), (14, 8), (15, 8) },        // I
		new[] { (12, 7), (13, 7), (14, 7), (15, 7) },        // I
	};
	static readonly (int i, int j)[][] B1bREST =
	{
		new[] { (3, 8), (4, 8), (4, 7), (5, 7) },            // S
		new[] { (9, 7), (10, 7), (10, 8), (11, 8) },         // Z
		new[] { (13, 8), (14, 8), (15, 8), (14, 7) },        // T
		new[] { (0, 7), (1, 7), (0, 8), (1, 8) },            // O
	};
	// the spiral of game over: cells from the edge in, leaving the middle (columns 5-10, rows 3-5)
	static List<(int i, int j)> B1bSpiral()
	{
		var L = new List<(int, int)>(); int l = 0, r = 15, t = 0, b = 8;
		while (l <= r && t <= b)
		{
			for (int i = l; i <= r; i++) L.Add((i, t)); t++;
			for (int j = t; j <= b; j++) L.Add((r, j)); r--;
			if (t <= b) { for (int i = r; i >= l; i--) L.Add((i, b)); b--; }
			if (l <= r) { for (int j = b; j >= t; j--) L.Add((l, j)); l++; }
		}
		return L.Where(c => !(c.Item1 >= 5 && c.Item1 <= 10 && c.Item2 >= 3 && c.Item2 <= 5)).ToList();
	}
	static List<Ev> buildB1b()
	{
		var E = new List<Ev>();
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz h) => hazards.Add(h);
		double SB = SPB;
		B1bCh = new();

		at(0, () => { B1bCh = new(); add(B1bStage()); }, true);
		at(0, () => add(B1bCore()), true);

		// ===== pong (b2-30): one ball, then two (b10), then three (b18); every crossing is 4 beats, on the bar =====
		void Pong(double b0, double y0, int dir, double cb, double vy, double b1, double sp)
		{
			at(b0 - 1.2, () => add(B1bPong(b0, y0, dir, cb, vy, b1)));
			// sparks back off the paddle where the ball hits it
			foreach (var (hb, side) in B1bHits(b0, dir, cb, b1))
			{
				double hbb = hb; var q = B1bBall(b0, y0, dir, cb, vy, hb); double back = side > 0 ? Math.PI : 0, x = side > 0 ? B1bPR - 10 : B1bPL + 10;
				at(hbb - 0.75, () => { for (int s = -2; s <= 2; s++) add(B1bShot(x, q.y, back + s * 0.3, sp, B(hbb), 0.75 * SB, 8)); });
			}
		}
		Pong(2, 360, 1, 4, 170, 30, 320);
		Pong(8, 200, -1, 4, -210, 30, 340);
		Pong(14, 520, 1, 3, 260, 30, 360);
		Pong(20, 120, -1, 3, 300, 30, 380);
		// the core keeps time on the hats: two shards a beat that turn (b6-30)
		for (double b = 4; b < 30; b += b < 8 ? 1 : 0.5) { double bb = b, a0 = b * 0.61; int n = b < 16 ? 3 : 4; at(bb - 0.75, () => { for (int i = 0; i < n; i++) add(B1bShot(640, 360, a0 + i * TAU / n, 340, B(bb), 0.75 * SB)); }); }

		// ===== snake (b30-64): out of the core to the apples; a second one from b46 =====
		at(29, () => add(B1MarkFx(B(29), B(316))));
		void Snakes(List<B1bSnk> SS, double end, double sp)
		{
			B1bPlan(SS, 0.5, end,
				(pi, pj, i, j, a, l, lp) => at(a - 1.1, () => add(B1bCell(pi, pj, i, j, a, l, lp, 0.5, end))),
				(i, j, a, e) =>
				{
					at(a - 0.05, () => add(B1bApple(i, j, a, e)));
					if (e > 0) at(e - 0.75, () => { for (int k = 0; k < 8; k++) add(B1bShot(B1bCx(i), B1bCy(j), k * TAU / 8 + 0.39, sp, B(e), 0.75 * SB)); });
				});
		}
		Snakes(new List<B1bSnk>
		{
			new B1bSnk { i = 7, j = 4, dx = -1, dy = 0, len = 5, b0 = 30, appear = 30, apples = new[] { (2, 1), (12, 7), (3, 7), (13, 1), (1, 4), (9, 7), (6, 1), (14, 5), (4, 4), (11, 2) } },
			new B1bSnk { i = 8, j = 4, dx = 1, dy = 0, len = 5, b0 = 46, appear = 46, apples = new[] { (13, 7), (3, 1), (12, 1), (2, 7), (14, 4), (6, 7), (9, 1), (1, 3), (11, 4), (4, 6) } },
		}, 64, 300);
		// the marked one: three shards from the core every 2 beats
		for (double T = 34; T < 63; T += 2) { double TT = T; at(TT - 1.5, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - 360, p.x - 640); for (int s = -2; s <= 2; s++) add(B1bShot(640, 360, a + s * 0.18, 400, B(TT), 1.5 * SB)); }, rnd: true); }
		// on the kick the core spits a ring with a gap that turns (b32-62, every 4 beats; every 2 from b48)
		for (double T = 32; T < 63; T += T < 48 ? 4 : 2) { double TT = T, ga = T * 0.83; at(TT, () => add(B1bRing(640, 360, B(TT), 330, ga, 0.6))); }

		// ===== tetris (b64-88): the core (now at the top) drops the pieces on the quiet part =====
		for (int n = 0; n < 8; n++) { double T = 66 + 2 * n; var c = B1bFILL[n]; at(T - 2, () => add(B1bPiece(c, B(T), 2 * SB, 81))); }
		for (int n = 0; n < 4; n++) { double T = 82 + 1.5 * n; var c = B1bREST[n]; at(T - 1.5, () => add(B1bPiece(c, B(T), 1.5 * SB, 88))); }
		// its column on the marked one: every 2 beats, every beat from b80
		for (double T = 67; T < 88; T += T < 72 ? 2 : 1) { double TT = T; at(TT - 1.5, () => { var p = B1Aim(TT); add(B1bBeam(clamp(p.x, 28, W - 28), B(TT), 1.5 * SB)); }, rnd: true); }
		// a row of the board burns every 2 beats (a line clear's flash), its row shows 1.5 beats before
		{ int[] RW = { 2, 5, 3, 6, 1, 4, 2, 5, 3, 6, 1, 4, 2, 5 }; for (int n = 0; n < RW.Length; n++) { double T = n < 4 ? 69 + 2 * n : 77 + (n - 4); int j = RW[n]; at(T - 1.5, () => add(B1bRow(j, B(T), 1.5 * SB))); } }
		// shards on the melody: from the core going down, turning
		for (double T = 66; T < 88; T += 1) { double TT = T, a0 = Math.PI / 2 + JS.sin(T * 0.9) * 0.8; at(TT - 1, () => { for (int s = -2; s <= 2; s++) add(B1bShot(640, 70, a0 + s * 0.26, 340, B(TT), SB)); }); }

		// ===== invaders (b88-124): they march (a step a beat, half a beat from b104), shoot down their columns, dive at b122 =====
		at(85, () => add(B1bFleet(88, 104, 190, 121.5, 124)));
		for (double T = 89; T < 121; T += T < 104 ? 1 : 0.5)
		{
			double TT = T; int k = (int)Math.Round((T - 89) * 2);
			int c1 = (k * 3) % 8, c2 = (k * 3 + 3) % 8, c3 = (k * 3 + 6) % 8, c4 = T < 104 ? -1 : (k * 3 + 5) % 8;
			at(TT - 1, () => { foreach (int c in new[] { c1, c2, c3, c4 }.Where(c => c >= 0)) { var q = B1bAlien(88, 104, 190, c, 2, TT); add(B1bBolt(q.x, q.y + 20, B(TT), SB, 680)); } });
		}
		// the mothership (the core) bombs the marked one every 4 beats (every 2 from b104)
		for (double T = 92; T < 121; T += T < 112 ? 2 : 1) { double TT = T; at(TT - 1.5, () => { var p = B1Aim(TT); add(B1bBeam(clamp(p.x, 40, W - 40), B(TT), 1.5 * SB, 80)); }, rnd: true); }

		// ===== the road (b124-192): five lanes of cars; the circles on the other side when the bass is back =====
		{
			var LY = new[] { 168.0, 264, 360, 456, 552 }; var SP = new[] { 175.0, -220, 200, -240, 210 }; var LEN = new[] { 200.0, 260, 180, 300, 220 }; var GAP = new[] { 260.0, 300, 240, 320, 260 };
			for (int j = 0; j < 5; j++)
			{
				// from b160 they speed up, and turn back every 8 beats (the arrows say so a beat before)
				double s0 = SP[j], s1 = SP[j] * 1.25;
				var S = new L15Spin(j * 97, 126, 194, new double[] { 126, 160, 168, 176, 184 }, new double[] { s0, s1, -s1, s1, -s1 });
				int jj = j; at(126, () => add(B1bLane(LY[jj], S, LEN[jj], GAP[jj], 128, 191)));
			}
			// the circles: bottom, top-left, bottom-right, top-right, bottom-left (online: one for each standing player, side by side)
			var CW = new (double a, double b, double y, double[] xs)[]
			{
				(124, 128, 660, new[] { 640.0, 380, 900, 140 }), (138, 144, 60, new[] { 220.0, 420, 860, 1060 }), (154, 160, 660, new[] { 1060.0, 860, 420, 220 }),
				(170, 176, 60, new[] { 1060.0, 860, 420, 220 }), (186, 192, 660, new[] { 220.0, 420, 860, 1060 }),
			};
			foreach (var w in CW) { var ww = w; at(ww.a - 0.2, () => B1Circles(k => t => (ww.xs[k % 4], ww.y), B(ww.a), B(ww.b), 60)); }
			// the core (above the road) throws at the marked one every 2 beats
			for (double T = 130; T < 190; T += T < 160 ? 2 : 1) { if (T % 16 == 14 || T % 16 == 15 || T % 16 == 0) continue; double TT = T; at(TT - 1.5, () => { var o = B1bCorePos(TT); var p = B1Aim(TT); double a = JS.atan2(p.y - o.y, p.x - o.x); for (int s = -2; s <= 2; s++) add(B1bShot(o.x, o.y, a + s * 0.15, 420, B(TT), 1.5 * SB)); }, rnd: true); }
		}

		// ===== the boss (b192-248): its bar is up; every 2 beats it charges at the marked one and bursts where it lands =====
		for (int k = 0; k < 27; k++)
		{
			double T = 194 + 2 * k; int kk = k;
			at(T - 1.5, () =>
			{
				var from = B1bCh.Count > 0 ? (B1bCh[^1].x1, B1bCh[^1].y1) : (640.0, 360.0); var p = B1Aim(T);
				double tx = clamp(p.x, 90, W - 90), ty = clamp(p.y, 110, H - 70), d = hypot(tx - from.Item1, ty - from.Item2);
				if (d < 260) { double a = d < 1 ? kk * 2.1 : JS.atan2(ty - from.Item2, tx - from.Item1); tx = clamp(from.Item1 + JS.cos(a) * 300, 90, W - 90); ty = clamp(from.Item2 + JS.sin(a) * 300, 110, H - 70); if (hypot(tx - from.Item1, ty - from.Item2) < 200) { tx = 1280 - from.Item1; ty = 720 - from.Item2; } }
				B1bCh.Add((T, from.Item1, from.Item2, tx, ty));
				add(B1bCharge(kk, T, from.Item1, from.Item2, tx, ty, kk == 26 ? 250 : T + 2));
				int n = T < 224 ? 14 : 16; double a0 = kk * 0.37;
				for (int i = 0; i < n; i++) add(B1bShot(tx, ty, a0 + i * TAU / n, 380, B(T + 0.75), 0.75 * SB));
				add(B1bRing(tx, ty, B(T + 1.5), 420, JS.atan2(360 - ty, 640 - tx), 0.55));
			}, rnd: true);
		}
		for (double T = 195; T < 248; T += 2)
		{
			double TT = T;
			at(TT - 1.5, () => { var o = B1bCorePos(TT); var p = B1Aim(TT); double a = JS.atan2(p.y - o.y, p.x - o.x); for (int s = -1; s <= 1; s++) add(B1bShot(o.x, o.y, a + s * 0.2, 420, B(TT), 1.5 * SB)); }, rnd: true);
		}
		// the bar drips on the hats from b224: two drops a half beat that sweep across (its line shows a beat before)
		for (double T = 208; T < 248; T += 0.5)
		{
			double TT = T, u = (T - 208) * 2, x = 120 + 1040 * B1bTri(u / 12, 0, 1);
			at(TT - 1, () => { add(B1bBolt(x, 48, B(TT), SB, 480)); add(B1bBolt(W - x, 48, B(TT), SB, 480)); });
		}

		// ===== the bar falls (b248-256): a wall down with a gap on the right, one up with a gap on the left =====
		at(248, () => add(B1bBarWall(true, 249.5, 254.5, 960, 210)));
		at(250, () => add(B1bBarWall(false, 251.5, 256, 320, 210)));

		// ===== all the games again, faster (b256-288), with rings on the drum =====
		Pong(256, 240, 1, 4, 260, 264, 380); Pong(256, 480, -1, 4, -260, 264, 380); Pong(257, 120, -1, 3, 320, 264, 380); Pong(258, 600, 1, 3, -300, 264, 380);
		Snakes(new List<B1bSnk> { new B1bSnk { i = 7, j = 4, dx = 0, dy = -1, len = 10, b0 = 264, appear = 264, apples = new[] { (2, 1), (13, 7), (13, 1), (2, 7) } } }, 272, 340);
		for (int n = 0; n < 8; n++) { double T = 272.5 + n; var c = B1bFILL[n]; at(T - 1.5, () => add(B1bPiece(c, B(T), 1.5 * SB, 280.5))); }
		{ int[] RW = { 1, 4, 2, 5, 3, 6, 1, 4 }; for (int n = 0; n < 8; n++) { double T = 272 + n; int j = RW[n]; at(T - 1.5, () => add(B1bRow(j, B(T), 1.5 * SB))); } }
		at(277, () => add(B1bFleet(280, 280, 110, 999, 288)));
		for (double T = 280.5; T < 288; T += 0.5)
		{
			double TT = T; int k = (int)Math.Round((T - 280.5) * 2), c1 = (k * 3) % 8, c2 = (k * 3 + 3) % 8, c3 = (k * 3 + 6) % 8;
			at(TT - 1, () => { foreach (int c in new[] { c1, c2, c3 }) { var q = B1bAlien(280, 280, 110, c, 2, TT); add(B1bBolt(q.x, q.y + 20, B(TT), SB, 600)); } });
		}
		for (double T = 281; T < 288; T += 2) { double TT = T; at(TT - 1.5, () => { var p = B1Aim(TT); add(B1bBeam(clamp(p.x, 40, W - 40), B(TT), 1.5 * SB, 80)); }, rnd: true); }
		for (double T = 258; T < 288; T += 2) { double TT = T, ga = T * 1.13; at(TT - 1, () => add(B1bRing(640, 360, B(TT), 400, ga, 0.5))); }

		// ===== game over (b288-316): the spiral fills, the core shoots three at the marked one every beat, rings every 4 =====
		{
			var sp = B1bSpiral(); var cells = new List<(int i, int j, double b)>();
			for (int n = 0; n < sp.Count; n++) cells.Add((sp[n].i, sp[n].j, 289 + 23.0 * n / sp.Count));
			at(287, () => add(B1bFill(cells, 316)));
		}
		for (double T = 290; T < 315; T += 1)
		{
			double TT = T; at(TT - 1.5, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - 360, p.x - 640); for (int s = -1; s <= 1; s++) add(B1bShot(640, 360, a + s * 0.2, 400, B(TT), 1.5 * SB)); }, rnd: true);
		}
		for (double T = 290.5; T < 315; T += 2) { double TT = T, ga = T * 0.7; at(TT - 1, () => add(B1bRing(640, 360, B(TT), 300, ga, 0.6))); }
		at(319, () => add(B1Shatter(B(320))));

		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
