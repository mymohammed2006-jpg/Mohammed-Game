using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// B1 "الزعيم الأخير" (Nitro Fun - Final Boss, 0:00-2:29, 130 BPM): the co-op boss mode's level (his يالله 05:58 10-25, plan doc
// "خطة الزعيم الأخير"). Every pink thing of the old levels was a piece of one creature; here it pulls itself together.
// One level in three parts with no checkpoint:
//   the awakening (b0-124): shards from the core on the notes, the hands slam the quarters (each ring's gap faces the quarter slammed
//     before), the mark (lasers on where the marked one stood), the core's four arms that turn and turn back;
//   the silence (b124-192): on the bass the floor's tiles light in patterns; when the bass stops everything that flies freezes and the
//     circles come out (only who is outside a circle when the bass is back gets hit), then the frozen shards fly back the way they came;
//   the rage (b192-320): the room turns with two spirals and the wedges, the boss splits in four that shoot at the marked one, both of
//     it all together, then the walls close in while the rings come with a gap that sways.
// Beat 0 = the first bar (1.053 s). Every hazard is a pure function of time once it exists, so hit(p, t) is right for any later t.
// Events are made here (DodgeBuild); the ones aimed at the player are rnd (the bot learns them only 0.32-0.40 s after they fire).
public static partial class G
{
	const double B1X = W / 2, B1Y = H / 2;
	public const double B1_END = 320.5;
	// freeze windows (beats): what flies stops at a, the circles judge at b, then it flies back home
	static readonly (double a, double b)[] B1FZ = { (124, 128), (138, 144), (153, 160), (169, 176), (187, 192), (282.5, 288) };
	static (double a, double b)? B1Fz(double t0) { foreach (var w in B1FZ) { double a = B(w.a); if (a > t0 + 1e-6) return (a, B(w.b)); } return null; }
	// how far a thing sent at t0 at speed v has gone at t: on, held in the freeze, then back (< 0 = home again)
	static double B1Fly(double t0, double v, double t)
	{
		var w = B1Fz(t0); if (w == null || t < w.Value.a) return v * (t - t0);
		if (t < w.Value.b) return v * (w.Value.a - t0);
		return v * (w.Value.a - t0) - v * (t - w.Value.b);
	}
	static bool B1Frozen(double t) { foreach (var w in B1FZ) if (t >= B(w.a) && t < B(w.b)) return true; return false; }
	static double B1Ang(double a) { a %= TAU; if (a > Math.PI) a -= TAU; if (a < -Math.PI) a += TAU; return a; }

	// ---------- the room ----------
	static Hz B1Stage()
	{
		var h = new Hz { kind = "B1Stage", z = -1 };
		h.draw = t =>
		{
			double bt = BT(t);
			var gr = g.createRadialGradient(B1X, B1Y, 60, B1X, B1Y, 780); gr.addColorStop(0, "#1a0718"); gr.addColorStop(1, "#05020a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the grid: still, then it turns with the rage (b192-252) and shakes on the bass
			double rot = bt < 192 ? 0 : bt < 252 ? (bt - 192) * 0.035 * (Math.Floor((bt - 192) / 8) % 2 == 0 ? 1 : -1) + Math.Floor((bt - 192) / 8) * 0.1 : 0;
			g.save(); g.translate(B1X, B1Y); g.rotate(rot); g.strokeStyle = $"rgba(150,90,210,{str(0.05 + 0.07 * pulse)})"; g.lineWidth = 1; g.beginPath();
			for (double x = -900; x <= 900; x += 80) { g.moveTo(x, -900); g.lineTo(x, 900); }
			for (double y = -900; y <= 900; y += 80) { g.moveTo(-900, y); g.lineTo(900, y); }
			g.stroke(); g.restore();
			// the silence: the floor's tile lines
			if (bt >= 124 && bt < 192) { g.strokeStyle = "rgba(200,190,230,0.10)"; g.lineWidth = 2; g.beginPath(); for (int i = 1; i < 10; i++) { g.moveTo(i * 128, 0); g.lineTo(i * 128, H); } for (int j = 1; j < 6; j++) { g.moveTo(0, j * 120); g.lineTo(W, j * 120); } g.stroke(); }
			// frozen: a cold blue veil
			if (B1Frozen(t)) { g.fillStyle = "rgba(120,170,255,0.07)"; g.fillRect(0, 0, W, H); }
			var vg = g.createRadialGradient(B1X, B1Y, 300, B1X, B1Y, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
		};
		return h;
	}

	// ---------- the core ----------
	// where the core is and how big (r = 0: not there). It grows as it pulls itself together, splits at b220, comes back at b252, breaks at b320
	static double B1CoreR(double bt) => bt < 0 ? 12 : bt < 8 ? 12 + 32 * bt / 8 : bt < 220 ? 44 : bt < 252 ? 0 : bt < 256 ? 44 * (bt - 252) / 4 : bt < 320 ? 44 : 0;
	static Hz B1Core()
	{
		var h = new Hz { kind = "B1Core" };
		h.hit = (p, t) =>
		{
			double bt = BT(t), r = B1CoreR(bt); if (r > 0 && hypot(p.x - B1X, p.y - B1Y) < r + p.r) return true;
			if (bt >= 8.5 && bt < 84) for (int i = 0; i < 6; i++) { var q = B1Plate(bt, i); if (hypot(p.x - q.x, p.y - q.y) < 13 + p.r) return true; }
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t), r = B1CoreR(bt), fz = B1Frozen(t) ? 0 : 1;
			// the pieces of the old levels flying in to it (b0-8, grey: they don't hurt)
			if (bt < 8) for (int i = 0; i < 12; i++)
			{
				double k = clamp((bt - i * 0.6) / 1.4, 0, 1); if (k <= 0 || k >= 1) continue; double a = i * 2.4, d = 700 * (1 - k * k);
				double x = B1X + JS.cos(a) * d, y = B1Y + JS.sin(a) * d * 0.7; g.fillStyle = L15G(0.5); B1PieceShape(x, y, 14, a + bt, i % 3);
			}
			// the plates (b6-84): grey while coming, pink once they hurt
			if (bt >= 6 && bt < 85)
				for (int i = 0; i < 6; i++)
				{
					var q = B1Plate(bt, i); double a = bt < 8.5 ? (bt - 6) / 2.5 : bt > 84 ? 85 - bt : 1;
					g.save(); g.translate(q.x, q.y); g.rotate(q.a + Math.PI / 2); g.fillStyle = bt < 8.5 ? L15G(0.5 * a) : L15P(a); g.shadowColor = PINK; g.shadowBlur = bt < 8.5 ? 0 : 10;
					g.fillRect(-15, -7, 30, 14); g.shadowBlur = 0; g.fillStyle = "rgba(40,0,14,0.6)"; g.fillRect(-9, -2, 18, 4); g.restore();
				}
			if (r > 0) B1CoreDraw(B1X, B1Y, r, bt, fz);
		};
		return h;
	}
	static (double x, double y, double a) B1Plate(double bt, int i) { double a = bt * 0.26 + i * TAU / 6, d = 140 + 10 * JS.sin(bt * Math.PI / 2); return (B1X + JS.cos(a) * d, B1Y + JS.sin(a) * d, a); }
	// a piece of an old level: 0 a spike, 1 a gear, 2 a ring
	static void B1PieceShape(double x, double y, double s, double a, int kind)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.beginPath();
		if (kind == 0) { g.moveTo(0, -s); g.lineTo(s * 0.8, s * 0.7); g.lineTo(-s * 0.8, s * 0.7); g.closePath(); g.fill(); }
		else if (kind == 1) { for (int i = 0; i < 16; i++) { double q = i * TAU / 16, rr2 = i % 2 == 0 ? s : s * 0.7; if (i == 0) g.moveTo(JS.cos(q) * rr2, JS.sin(q) * rr2); else g.lineTo(JS.cos(q) * rr2, JS.sin(q) * rr2); } g.closePath(); g.fill(); }
		else { g.lineWidth = s * 0.35; g.strokeStyle = g.fillStyle; g.arc(0, 0, s * 0.75, 0, TAU); g.stroke(); }
		g.restore();
	}
	// the core: a pink heart of shards with dark cracks that turn
	static void B1CoreDraw(double x, double y, double r, double bt, double live)
	{
		g.save(); g.translate(x, y);
		double pu = 1 + 0.06 * pulse * live;
		g.shadowColor = PINK; g.shadowBlur = 26 + 14 * pulse;
		var gr = g.createRadialGradient(0, 0, r * 0.15, 0, 0, r * pu); gr.addColorStop(0, "#ffd0e0"); gr.addColorStop(0.35, PINK); gr.addColorStop(1, "#b0123f"); g.fillStyle = gr;
		g.beginPath(); for (int i = 0; i < 10; i++) { double q = i * TAU / 10 + bt * 0.05, rr2 = r * pu * (i % 2 == 0 ? 1.08 : 0.9); if (i == 0) g.moveTo(JS.cos(q) * rr2, JS.sin(q) * rr2); else g.lineTo(JS.cos(q) * rr2, JS.sin(q) * rr2); } g.closePath(); g.fill(); g.shadowBlur = 0;
		g.rotate(-bt * 0.12); g.strokeStyle = "rgba(40,0,14,0.75)"; g.lineWidth = 3; g.beginPath();
		for (int i = 0; i < 6; i++) { double q = i * TAU / 6; g.moveTo(JS.cos(q) * r * 0.25, JS.sin(q) * r * 0.25); g.lineTo(JS.cos(q + 0.3) * r * 0.85, JS.sin(q + 0.3) * r * 0.85); }
		g.stroke(); g.fillStyle = "rgba(40,0,14,0.85)"; circle(0, 0, r * 0.22); g.fillStyle = "#fff"; circle(0, 0, r * 0.08 * (1 + pulse * live));
		g.restore();
	}

	// ---------- shards ----------
	// a shard from (x0,y0) toward angle a at v px/s, sent at t0; its path shows wn s before. Frozen in a freeze, then back home
	static Hz B1Shot(double x0, double y0, double a, double v, double t0, double wn, double r = 9, double life = 4)
	{
		var h = new Hz { kind = "B1Shot" }; double ca = JS.cos(a), sa = JS.sin(a);
		(double x, double y, bool on) At(double t) { if (t < t0) return (x0, y0, false); double d = B1Fly(t0, v, t); if (d < 0 || (d > 1700 && t > t0 + life)) return (0, 0, false); return (x0 + ca * d, y0 + sa * d, true); }
		h.update = (t, dt) => { if (t > t0 && B1Fly(t0, v, t) < 0) h.dead = true; if (t > t0 + 30) h.dead = true; var w = B1Fz(t0); if (w == null && t > t0 + life) h.dead = true; if (w != null && t > t0 + life && t < w.Value.a && v * (t - t0) > 1800) h.dead = true; };
		h.hit = (p, t) => { var q = At(t); return q.on && hypot(p.x - q.x, p.y - q.y) < r + p.r; };
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - (t0 - wn)) / wn, 0, 1); if (k <= 0) return; L15Dash(x0, y0, x0 + ca * 300 * k, y0 + sa * 300 * k, L15G(0.18 + 0.4 * k), 2); return; }
			var q = At(t); if (!q.on) return;
			// frozen and coming back: the way home shows
			var w = B1Fz(t0); if (w != null && t >= w.Value.a && t < w.Value.b) { double k = clamp((t - w.Value.a) / 0.4, 0, 1); L15Dash(q.x, q.y, q.x - ca * Math.Min(360, B1Fly(t0, v, t)) * k, q.y - sa * Math.Min(360, B1Fly(t0, v, t)) * k, L15G(0.35), 2); }
			if (q.x < -40 || q.x > W + 40 || q.y < -40 || q.y > H + 40) return;
			g.save(); g.translate(q.x, q.y); g.rotate(a); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
			g.beginPath(); g.moveTo(r * 1.6, 0); g.lineTo(0, r * 0.8); g.lineTo(-r * 1.2, 0); g.lineTo(0, -r * 0.8); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	// ---------- the hands: a quarter (q 0 top-left, 1 top-right, 2 bottom-right, 3 bottom-left) or a wedge from the core ----------
	static (double x, double y, double w, double h) B1Quad(int q) => (q == 0 || q == 3 ? 0 : B1X, q < 2 ? 0 : B1Y, W / 2, H / 2);
	static Hz B1Slam(int q, double T, double wn)
	{
		var h = new Hz { kind = "B1Slam", z = -1 }; var R = B1Quad(q); const double HOLD = 0.22;
		h.update = (t, dt) => { if (t > T + 0.6) h.dead = true; if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 9); burst(R.x + R.w / 2, R.y + R.h / 2, PINK, 16, 420); sfx(tt => sNoise(tt, 0.2, "lowpass", 500, 0.18)); } };
		h.hit = (p, t) => t >= T && t < T + HOLD && p.x > R.x - p.r && p.x < R.x + R.w + p.r && p.y > R.y - p.r && p.y < R.y + R.h + p.r;
		h.draw = t =>
		{
			if (t < T)
			{
				double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return;
				g.fillStyle = L15G(0.05 + 0.13 * k); g.fillRect(R.x, R.y, R.w, R.h); g.strokeStyle = L15G(0.3 + 0.5 * k); g.lineWidth = 3; g.strokeRect(R.x + 4, R.y + 4, R.w - 8, R.h - 8);
				B1Fist(R.x + R.w / 2, R.y + R.h / 2, 0.55 + 0.45 * k, L15G(0.25 + 0.4 * k)); return;
			}
			double a = t < T + HOLD ? 0.85 : 0.85 * (1 - (t - T - HOLD) / (0.6 - HOLD)); if (a <= 0) return;
			g.fillStyle = L15P(a * 0.75); g.fillRect(R.x, R.y, R.w, R.h); B1Fist(R.x + R.w / 2, R.y + R.h / 2, 1.05, L15P(a));
		};
		return h;
	}
	// a fist seen from above (s = size)
	static void B1Fist(double x, double y, double s, string col)
	{
		g.save(); g.translate(x, y); g.scale(s, s); g.fillStyle = col;
		rr(-70, -40, 140, 95, 26); g.fill(); for (int i = 0; i < 4; i++) { rr(-68 + i * 35, -78, 32, 50, 14); g.fill(); } rr(-104, -10, 44, 30, 14); g.fill();
		g.fillStyle = "rgba(0,0,0,0.25)"; for (int i = 0; i < 4; i++) g.fillRect(-60 + i * 35, -44, 16, 4);
		g.restore();
	}
	// a wedge of the room from the core (centre angle a, half width hw), slams at T
	static Hz B1Wedge(double a, double hw, double T, double wn)
	{
		var h = new Hz { kind = "B1Wedge", z = -1 }; const double HOLD = 0.22;
		h.update = (t, dt) => { if (t > T + 0.6) h.dead = true; if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 6); } };
		h.hit = (p, t) => t >= T && t < T + HOLD && hypot(p.x - B1X, p.y - B1Y) > 30 && Math.Abs(B1Ang(JS.atan2(p.y - B1Y, p.x - B1X) - a)) < hw + p.r / Math.Max(30, hypot(p.x - B1X, p.y - B1Y));
		h.draw = t =>
		{
			double fill, line; string c;
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; fill = 0.05 + 0.13 * k; line = 0.3 + 0.5 * k; c = "g"; }
			else { double q = t < T + HOLD ? 1 : 1 - (t - T - HOLD) / (0.6 - HOLD); if (q <= 0) return; fill = 0.7 * q; line = q; c = "p"; }
			g.beginPath(); g.moveTo(B1X, B1Y); g.arc(B1X, B1Y, 1000, a - hw, a + hw); g.closePath();
			g.fillStyle = c == "g" ? L15G(fill) : L15P(fill); g.fill(); g.strokeStyle = c == "g" ? L15G(line) : L15P(line); g.lineWidth = 3; g.stroke();
		};
		return h;
	}

	// ---------- rings: from (cx,cy) at T, v px/s, a gap of half width gh around angle ga (frozen and back like the shards) ----------
	static Hz B1Ring(double cx, double cy, double T, double v, double ga, double gh, double th = 18)
	{
		var h = new Hz { kind = "B1Ring" };
		double Rr(double t) => t < T ? -1 : B1Fly(T, v, t);
		h.update = (t, dt) => { double r = Rr(t); if (t > T && (r < 0 || (r > 1600 && B1Fz(T) is var w && (w == null || t < w.Value.a)))) h.dead = true; };
		h.hit = (p, t) =>
		{
			double r = Rr(t); if (r < 0) return false; double d = hypot(p.x - cx, p.y - cy); if (Math.Abs(d - r) >= th / 2 + p.r) return false;
			return Math.Abs(B1Ang(JS.atan2(p.y - cy, p.x - cx) - ga)) > gh;
		};
		h.draw = t =>
		{
			double r = Rr(t); if (r < 0) return;
			g.strokeStyle = PINK; g.lineWidth = th; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.arc(cx, cy, r, ga + gh, ga + TAU - gh); g.stroke(); g.shadowBlur = 0;
			// the gap's edges in white so it reads at a glance
			g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 3; g.beginPath();
			foreach (var e in new[] { ga + gh, ga - gh }) { g.moveTo(cx + JS.cos(e) * (r - th / 2), cy + JS.sin(e) * (r - th / 2)); g.lineTo(cx + JS.cos(e) * (r + th / 2), cy + JS.sin(e) * (r + th / 2)); }
			g.stroke();
		};
		return h;
	}

	// ---------- the mark ----------
	// the white sight on the marked square (it does not hurt)
	static Hz B1MarkFx(double t0, double t1)
	{
		var h = new Hz { kind = "B1Mark" };
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; };
		h.draw = t =>
		{
			var p = player; if (p == null || t < t0) return; double a = Math.Min(1, Math.Min((t - t0) / 0.3, (t1 - t) / 0.3)); if (a <= 0) return;
			double r = 22 + 3 * pulse, sp = t * 2.2; g.save(); g.translate(p.x, p.y); g.rotate(sp); g.strokeStyle = $"rgba(255,255,255,{str(0.85 * a)})"; g.lineWidth = 2.5; g.beginPath();
			for (int i = 0; i < 4; i++) { double q = i * Math.PI / 2; g.moveTo(JS.cos(q - 0.35) * r, JS.sin(q - 0.35) * r); g.arc(0, 0, r, q - 0.35, q + 0.35); g.moveTo(JS.cos(q) * (r + 4), JS.sin(q) * (r + 4)); g.lineTo(JS.cos(q) * (r + 11), JS.sin(q) * (r + 11)); }
			g.stroke(); g.restore();
		};
		return h;
	}
	// a laser from (x0,y0) toward angle a: its line shows from T - wn, it burns T .. T + 0.25 s
	static Hz B1Laser(double x0, double y0, double a, double T, double wn, double w = 24)
	{
		var h = new Hz { kind = "B1Laser", z = -1 }; double ca = JS.cos(a), sa = JS.sin(a); const double HOLD = 0.25;
		h.update = (t, dt) => { if (t > T + 0.55) h.dead = true; if (!h.ex && t >= T) { h.ex = true; sfx(tt => sNoise(tt, 0.15, "bandpass", 1800, 0.1)); } };
		h.hit = (p, t) => { if (t < T || t >= T + HOLD) return false; double u = (p.x - x0) * ca + (p.y - y0) * sa; if (u < 0) return false; return Math.Abs(-(p.x - x0) * sa + (p.y - y0) * ca) < w / 2 + p.r; };
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; g.strokeStyle = L15G(0.2 + 0.5 * k); g.lineWidth = 1 + 3 * k; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x0 + ca * 1600, y0 + sa * 1600); g.stroke(); return; }
			double q = t < T + HOLD ? 1 : 1 - (t - T - HOLD) / 0.3; if (q <= 0) return;
			g.strokeStyle = L15P(q); g.lineWidth = w; g.shadowColor = PINK; g.shadowBlur = 20; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x0 + ca * 1600, y0 + sa * 1600); g.stroke(); g.shadowBlur = 0;
			g.strokeStyle = $"rgba(255,230,240,{str(0.8 * q)})"; g.lineWidth = w * 0.3; g.stroke();
		};
		return h;
	}

	// ---------- the arms (b84-122): four beams from the core that turn and turn back ----------
	static Hz B1Arms(L15Spin S, double bOn, double bOff)
	{
		var h = new Hz { kind = "B1Arms", z = -1 };
		double Len(double bt) => bt < bOn ? 0 : bt < bOn + 1 ? 760 * (bt - bOn) : bt < bOff ? 760 : bt < bOff + 2 ? 760 * (1 - (bt - bOff) / 2) : 0;
		h.update = (t, dt) => { if (BT(t) > bOff + 2.2) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t), L = Len(bt); if (L <= 0) return false; double a0 = S.At(bt);
			for (int i = 0; i < 4; i++) { double a = a0 + i * Math.PI / 2; if (L15Seg(p.x, p.y, B1X + JS.cos(a) * 40, B1Y + JS.sin(a) * 40, B1X + JS.cos(a) * L, B1Y + JS.sin(a) * L) < 11 + p.r) return true; }
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t), a0 = S.At(bt);
			if (bt < bOn) { double k = clamp((bt - (bOn - 2)) / 2, 0, 1); if (k <= 0) return; for (int i = 0; i < 4; i++) { double a = a0 + i * Math.PI / 2; L15Dash(B1X, B1Y, B1X + JS.cos(a) * 760, B1Y + JS.sin(a) * 760, L15G(0.2 + 0.4 * k), 3); } return; }
			double L = Len(bt); if (L <= 0) return;
			for (int i = 0; i < 4; i++)
			{
				double a = a0 + i * Math.PI / 2; g.save(); g.translate(B1X, B1Y); g.rotate(a); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14;
				g.fillRect(40, -11, L - 40, 22); g.shadowBlur = 0; g.fillStyle = "rgba(60,0,20,0.5)"; for (double s = 70; s < L - 20; s += 46) g.fillRect(s, -11, 6, 22); g.restore();
			}
			// which way they will turn next: small white arrows at the tips a beat before a turn back
		};
		return h;
	}

	// ---------- the floor's tiles (the silence): cells (i 0-9, j 0-5) light from T - wn, burn T .. T + dur ----------
	static Hz B1Tiles(List<(int i, int j)> cells, double T, double wn, double dur)
	{
		var h = new Hz { kind = "B1Tiles", z = -1 };
		h.update = (t, dt) => { if (t > T + dur + 0.3) h.dead = true; };
		h.hit = (p, t) => { if (t < T || t >= T + dur) return false; foreach (var c in cells) { double x = c.i * 128, y = c.j * 120, m = p.r * 0.6; if (p.x > x - m && p.x < x + 128 + m && p.y > y - m && p.y < y + 120 + m) return true; } return false; };
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; foreach (var c in cells) { g.fillStyle = L15G(0.04 + 0.12 * k); g.fillRect(c.i * 128 + 3, c.j * 120 + 3, 122, 114); g.strokeStyle = L15G(0.25 + 0.45 * k); g.lineWidth = 2; g.strokeRect(c.i * 128 + 8, c.j * 120 + 8, 112, 104); } return; }
			double q = t < T + dur ? 1 : 1 - (t - T - dur) / 0.3; if (q <= 0) return;
			foreach (var c in cells) { g.fillStyle = L15P(0.8 * q); g.fillRect(c.i * 128 + 3, c.j * 120 + 3, 122, 114); }
		};
		return h;
	}

	// ---------- the circles: who is outside one at T1 is hit (the outside turns pink for that moment) ----------
	static Hz B1Circle(Func<double, (double x, double y)> at, double T0, double T1, double R)
	{
		var h = new Hz { kind = "B1Circle", z = -1 }; const double HOLD = 0.15;
		h.update = (t, dt) => { if (t > T1 + 0.5) h.dead = true; if (!h.ex && t >= T1) { h.ex = true; flash = Math.Max(flash, 0.15); sfx(tt => { sTone(tt, 196, 0.25, "square", 0.08); sNoise(tt, 0.2, "lowpass", 700, 0.15); }); } };
		h.hit = (p, t) => { if (t < T1 || t >= T1 + HOLD) return false; var c = at(T1); return hypot(p.x - c.x, p.y - c.y) > R - p.r; };
		h.draw = t =>
		{
			if (t < T0 - 0.2) return; var c = at(Math.Min(t, T1)); double k = clamp((t - T0) / (T1 - T0), 0, 1), a = clamp((t - (T0 - 0.2)) / 0.2, 0, 1);
			if (t >= T1)
			{
				double q = t < T1 + HOLD ? 1 : 1 - (t - T1 - HOLD) / 0.35; if (q <= 0) return;
				g.save(); g.beginPath(); g.rect(0, 0, W, H); g.arc(c.x, c.y, R, 0, TAU, true); g.fillStyle = L15P(0.45 * q); g.fill("evenodd"); g.restore();
				g.strokeStyle = $"rgba(255,255,255,{str(q)})"; g.lineWidth = 4; g.beginPath(); g.arc(c.x, c.y, R, 0, TAU); g.stroke(); return;
			}
			g.fillStyle = $"rgba(255,255,255,{str(0.10 * a)})"; circle(c.x, c.y, R);
			g.strokeStyle = $"rgba(255,255,255,{str(0.9 * a)})"; g.lineWidth = 3; g.beginPath(); g.arc(c.x, c.y, R, 0, TAU); g.stroke();
			g.lineWidth = 7; g.beginPath(); g.arc(c.x, c.y, R + 9, -Math.PI / 2, -Math.PI / 2 + TAU * k); g.stroke();
		};
		return h;
	}

	// ---------- the split (b220-252): four pieces going round, each shoots at the marked one in turn ----------
	static (double x, double y) B1PieceAt(int i, double bt)
	{
		double k = clamp((bt - 220) / 2, 0, 1), back = clamp((bt - 250) / 2, 0, 1), e = (1 - JS.pow(1 - k, 3)) * (1 - back * back);
		double a = -Math.PI / 4 + i * Math.PI / 2 + (bt - 220) * 0.09 * (bt < 236 ? 1 : 1) - Math.Max(0, bt - 236) * 0.18;
		double rx = 450 + 40 * JS.sin((bt - 220) * Math.PI / 8), ry = 250;
		return (B1X + JS.cos(a) * rx * e, B1Y + JS.sin(a) * ry * e);
	}
	static Hz B1Pieces()
	{
		var h = new Hz { kind = "B1Pieces" };
		h.update = (t, dt) => { if (BT(t) > 253) h.dead = true; };
		h.hit = (p, t) => { double bt = BT(t); if (bt < 220 || bt >= 252) return false; for (int i = 0; i < 4; i++) { var q = B1PieceAt(i, bt); if (hypot(p.x - q.x, p.y - q.y) < 26 + p.r) return true; } return false; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < 220 || bt >= 252) return;
			for (int i = 0; i < 4; i++) { var q = B1PieceAt(i, bt); B1CoreDraw(q.x, q.y, 26, bt + i * 3, 1); }
		};
		return h;
	}

	// ---------- the walls (b288-318): pink from the four sides closing in to ix, iy ----------
	static Hz B1Walls(double b0, double b1, double b2, double b3, double ix, double iy)
	{
		var h = new Hz { kind = "B1Walls", z = 1 };
		(double x, double y) In(double bt) { double k = bt < b0 ? 0 : bt < b1 ? (bt - b0) / (b1 - b0) : bt < b2 ? 1 : bt < b3 ? 1 - (bt - b2) / (b3 - b2) : 0; k = k * k * (3 - 2 * k); return (ix * k, iy * k); }
		h.update = (t, dt) => { if (BT(t) > b3 + 0.5) h.dead = true; };
		h.hit = (p, t) => { var q = In(BT(t)); if (q.x < 2) return false; return p.x < q.x + p.r || p.x > W - q.x - p.r || p.y < q.y + p.r || p.y > H - q.y - p.r; };
		h.draw = t =>
		{
			double bt = BT(t); var q = In(bt);
			if (bt < b0 && bt > b0 - 2) { double k = (bt - (b0 - 2)) / 2; g.strokeStyle = L15G(0.2 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 14, 10 }); g.strokeRect(ix, iy, W - 2 * ix, H - 2 * iy); g.setLineDash(new double[0]); return; }
			if (q.x < 1) return;
			g.fillStyle = L15P(0.9); g.fillRect(0, 0, W, q.y); g.fillRect(0, H - q.y, W, q.y); g.fillRect(0, q.y, q.x, H - 2 * q.y); g.fillRect(W - q.x, q.y, q.x, H - 2 * q.y);
			g.strokeStyle = "rgba(60,0,20,0.45)"; g.lineWidth = 10; g.beginPath();
			for (double s = -H; s < W + H; s += 46) { g.moveTo(s + bt * 8 % 46, 0); g.lineTo(s + bt * 8 % 46 + H, H); }
			g.save(); g.beginPath(); g.rect(0, 0, W, H); g.rect(q.x, q.y, W - 2 * q.x, H - 2 * q.y); g.clip("evenodd"); g.stroke(); g.restore();
			g.strokeStyle = "#fff"; g.lineWidth = 2; g.strokeRect(q.x, q.y, W - 2 * q.x, H - 2 * q.y);
		};
		return h;
	}

	// ---------- the end: the core breaks (it does not hurt) ----------
	static Hz B1Shatter(double T)
	{
		var h = new Hz { kind = "B1Shatter" };
		h.update = (t, dt) => { if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 14); flash = Math.Max(flash, 0.35); burst(B1X, B1Y, "#ffd0e0", 60, 800); burst(B1X, B1Y, "#ffffff", 30, 500); sfx(tt => { sNoise(tt, 0.6, "lowpass", 400, 0.3); sTone(tt, 82, 0.6, "square", 0.12); }); } if (t > T + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < T - 0.6) return; double bt = BT(t);
			if (t < T) { double k = (t - (T - 0.6)) / 0.6; g.strokeStyle = $"rgba(255,255,255,{str(k)})"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 7; i++) { double a = i * 0.9; g.moveTo(B1X, B1Y); g.lineTo(B1X + JS.cos(a) * 60 * k, B1Y + JS.sin(a) * 60 * k); } g.stroke(); return; }
			double e = t - T; for (int i = 0; i < 14; i++) { double a = i * TAU / 14 + 0.3, d = 60 + e * 520; g.fillStyle = $"rgba(255,208,224,{str(clamp(1 - e / 1.6, 0, 1))})"; B1PieceShape(B1X + JS.cos(a) * d, B1Y + JS.sin(a) * d, 12, a + e * 6, i % 3); }
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _b1 = DodgeBuild("B1", () => buildB1());
	// a shard's speed through the awakening: 260 -> 380 px/s by the first slam
	static double B1Sp(double b) => 260 + 120 * clamp(b / 28, 0, 1);
	// the tile patterns of the silence: k = the beat's number inside its run
	static List<(int i, int j)> B1Pat(int pat, int k)
	{
		var c = new List<(int i, int j)>();
		for (int i = 0; i < 10; i++) for (int j = 0; j < 6; j++)
		{
			bool on = pat switch
			{
				0 => (i + j) % 2 == k % 2,                                                        // checker, swapping each beat
				1 => j != (k % 10 < 5 ? k % 5 : 4 - k % 5) + (k % 10 < 5 ? 0 : 1) && i % 3 != k % 3,   // a safe row that walks + a safe column every third
				2 => ((int)Math.Max(Math.Abs(i - 4.5), Math.Abs(j - 2.5)) + k) % 3 == 0,           // rings going out from the core
				_ => (i == 9 - k % 10 || j == k % 6) == false && (i + 2 * j + k) % 4 == 0,         // a lattice that slides
			};
			if (on) c.Add((i, j));
		}
		return c;
	}
	static List<Ev> buildB1()
	{
		var E = new List<Ev>();
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz h) => hazards.Add(h);
		double SB = SPB;

		at(0, () => add(B1Stage()), true);
		at(0, () => add(B1Core()), true);

		// ===== 1. the awakening (b0-124) =====
		// shards on the notes: one a half beat turning clockwise 30 degrees, then two opposite (b6), then three (b14)
		for (double b = 2; b < 28; b += 0.5)
		{
			double bb = b, a0 = -Math.PI / 2 + (b - 2) * 2 * (Math.PI / 6);
			at(bb - 0.75, () =>
			{
				int n = bb < 6 ? 1 : bb < 14 ? 2 : 3;
				for (int i = 0; i < n; i++) add(B1Shot(B1X, B1Y, a0 + i * TAU / n, B1Sp(bb), B(bb), 0.75 * SB));
			});
		}
		// the hands: a quarter slams every 2 beats going clockwise; a ring out of it whose gap faces the quarter slammed before
		for (int n = 0; n < 8; n++)
		{
			double T = 28 + 2 * n; int q = n % 4, pq = (q + 3) % 4; var R = B1Quad(q); var P = B1Quad(pq);
			double cx = R.x + R.w / 2, cy = R.y + R.h / 2, ga = JS.atan2(P.y + P.h / 2 - cy, P.x + P.w / 2 - cx);
			at(T - 2, () => add(B1Slam(q, B(T), 2 * SB)));
			if (n > 0) at(T, () => add(B1Ring(cx, cy, B(T), 430, ga, 0.5)));
		}
		// still from the core: two shards a beat across the slams
		for (double b = 28; b < 64; b += 1)
		{
			double bb = b, a0 = b * 0.55;
			at(bb - 0.75, () => { add(B1Shot(B1X, B1Y, a0, 380, B(bb), 0.75 * SB)); add(B1Shot(B1X, B1Y, a0 + Math.PI, 380, B(bb), 0.75 * SB)); });
		}
		// two hands at once on a diagonal, the diagonals swap: every 2 beats, then every beat (b56-64)
		for (double T = 44; T < 64; T += T < 56 ? 2 : 1)
		{
			double TT = T, wn = T < 56 ? 2 : 1; int d = (int)Math.Round(T < 56 ? (T - 44) / 2 : T - 56 + 6) % 2;
			at(TT - wn, () => { add(B1Slam(d, B(TT), wn * SB)); add(B1Slam(d + 2, B(TT), wn * SB)); });
		}
		// rings from the core every 4 beats with a gap that turns
		for (double T = 46; T < 64; T += 4) { double TT = T, ga = T * 0.7; at(TT, () => add(B1Ring(B1X, B1Y, B(TT), 380, ga, 0.55))); }

		// the mark (b62-84): it picks you; a laser from the core on where you stand, every beat (three at once every fourth)
		at(62, () => add(B1MarkFx(B(62), B(120))));
		for (double T = 65.5; T < 84; T += 1)
		{
			double TT = T; bool tri = (int)(T - 65.5) % 4 == 3;
			at(TT - 1.5, () =>
			{
				var p = player; double a = JS.atan2(p.y - B1Y, p.x - B1X);
				add(B1Laser(B1X, B1Y, a, B(TT), 1.5 * SB));
				if (tri) { add(B1Laser(B1X, B1Y, a + 0.35, B(TT), 1.5 * SB)); add(B1Laser(B1X, B1Y, a - 0.35, B(TT), 1.5 * SB)); }
			}, rnd: true);
		}
		// the plates throw a shard out every 2 beats, half of them at a time
		for (double T = 68; T < 84; T += 2)
		{
			double TT = T; int h0 = (int)(T / 2) % 2;
			at(TT - 1, () => { for (int i = h0; i < 6; i += 2) { var q = B1Plate(TT, i); add(B1Shot(q.x, q.y, q.a, 340, B(TT), SB)); } });
		}
		for (double T = 68; T < 84; T += 4) { double TT = T, ga = T * 0.9 + 1; at(TT, () => add(B1Ring(B1X, B1Y, B(TT), 360, ga, 0.5))); }

		// the arms (b84-122): four beams that turn, turn back every 8 beats, faster after b100; the lasers keep coming
		var S = new L15Spin(Math.PI / 4, 82, 124, new double[] { 84, 92, 100, 108, 116 }, new double[] { 0.26, -0.26, 0.34, -0.34, 0.34 });
		at(82, () => add(B1Arms(S, 84, 120)));
		for (double T = 87; T < 120; T += T < 104 ? 2 : 1)
		{
			double TT = T;
			at(TT - 1.5, () => { var p = player; add(B1Laser(B1X, B1Y, JS.atan2(p.y - B1Y, p.x - B1X), B(TT), 1.5 * SB)); }, rnd: true);
		}
		// between the arms: four shards a beat out along the middle of each quarter
		for (double T = 88; T < 120; T += 1)
		{
			double TT = T, a0 = S.At(T) + Math.PI / 4;
			at(TT - 1, () => { for (int i = 0; i < 4; i++) add(B1Shot(B1X, B1Y, a0 + i * Math.PI / 2, 300, B(TT), SB)); });
		}
		// b122: it throws everything out (24 shards) - and then the bass stops
		at(121.25, () => { for (int i = 0; i < 24; i++) add(B1Shot(B1X, B1Y, i * TAU / 24 + 0.13, 330, B(122), 0.75 * SB)); });

		// ===== 2. the silence (b124-192) =====
		// a circle each time the bass stops (solo: one); outside it when the bass is back = hit
		var CIR = new (double x, double y)[] { (320, 540), (1050, 170), (230, 170), (1000, 560) };
		for (int w = 0; w < 5; w++)
		{
			var (a0, b0) = B1FZ[w]; int ww = w;
			Func<double, (double, double)> path = ww switch
			{
				3 => t => { double k = clamp((t - B(a0)) / (B(b0) - B(a0)), 0, 1); return (1000 - 640 * k, 560 - 300 * k * (1 - k) * 2); },        // it moves
				4 => t => { double k = clamp((t - B(a0)) / (B(b0) - B(a0)), 0, 1); return (640 + 420 * JS.cos(k * Math.PI * 1.2), 360 + 240 * JS.sin(k * Math.PI * 1.2)); },
				_ => t => CIR[ww],
			};
			at(a0 - 0.2, () => add(B1Circle(path, B(a0), B(b0), ww >= 3 ? 70 : 60)));
		}
		// on the bass: the floor's tiles, a beat of warning; the core's fans of 5 every 2 beats, turning 72 degrees
		(double a, double b, int pat)[] ON = { (128, 138, 0), (144, 153, 1), (160, 169, 2) };
		foreach (var (a0, b0, pat) in ON)
			for (double T = a0 + 1; T < b0; T += 1)
			{
				double TT = T; int k = (int)(T - a0), pp = pat;
				at(TT - 1, () => add(B1Tiles(B1Pat(pp, k), B(TT), SB, 0.35 * SB)));
			}
		foreach (var (a0, b0) in new[] { (128.0, 138.0), (144.0, 153.0), (160.0, 169.0), (176.0, 187.0) })
			for (double T = a0 + 1; T < b0 - 0.5; T += 2)
			{
				double TT = T, base0 = T * 72 * Math.PI / 180;
				at(TT - 1, () => { for (int i = 0; i < 5; i++) add(B1Shot(B1X, B1Y, base0 + (i - 2) * 0.22, 300, B(TT), SB)); });
			}
		// b176-187: the hands again, half the room each beat going round (top, right, bottom, left)
		for (double T = 177; T < 187; T += 1)
		{
			double TT = T; int h0 = (int)(T - 177) % 4, q0 = h0, q1 = (h0 + 1) % 4;
			at(TT - 1, () => { add(B1Slam(q0, B(TT), SB)); add(B1Slam(q1, B(TT), SB)); });
		}

		// ===== 3. the rage (b192-320) =====
		// two (three) spirals that turn and flip every 4 beats, the wedges every 2 beats
		{
			double th = 0;
			for (double b = 192; b < 220; b += 0.5)
			{
				double bb = b; int dir = (int)((b - 192) / 4) % 2 == 0 ? 1 : -1; th += 0.35 * dir; double t0 = th;
				at(bb - 0.5, () => { for (int i = 0; i < 3; i++) add(B1Shot(B1X, B1Y, t0 + i * TAU / 3, 320, B(bb), 0.5 * SB)); });
			}
		}
		for (double T = 194; T < 220; T += 2)
		{
			double TT = T, a = T * 0.45;
			at(TT - 2, () => { add(B1Wedge(a, 0.38, B(TT), 2 * SB)); add(B1Wedge(a + Math.PI, 0.38, B(TT), 2 * SB)); });
		}
		// the split (b220-252): four pieces go round; in turn one of them shoots three at you; crosses of light every 4 beats
		at(219, () => add(B1Pieces()));
		for (double T = 223.5; T < 251; T += 1)
		{
			double TT = T; int i = (int)(T - 223.5) % 4;
			at(TT - 1.5, () =>
			{
				var q = B1PieceAt(i, TT); var p = player; double a = JS.atan2(p.y - q.y, p.x - q.x);
				for (int s = -1; s <= 1; s++) add(B1Shot(q.x, q.y, a + s * 0.13, 560, B(TT), 1.5 * SB, 9, 3));
			}, rnd: true);
		}
		// and each piece throws two shards out every beat, crossing the others
		for (double T = 222; T < 251; T += 1)
		{
			double TT = T;
			at(TT - 1, () => { for (int i = 0; i < 4; i++) { var q = B1PieceAt(i, TT); double a = JS.atan2(q.y - B1Y, q.x - B1X); add(B1Shot(q.x, q.y, a + 1.1, 330, B(TT), SB)); add(B1Shot(q.x, q.y, a - 1.1, 330, B(TT), SB)); } });
		}
		for (double T = 224; T < 252; T += 2)
		{
			double TT = T, x = 640 + 380 * JS.sin(T * 0.7), y = 360 + 200 * JS.cos(T * 0.9);
			at(TT - 2, () => { add(B1Laser(x, -20, Math.PI / 2, B(TT), 2 * SB, 30)); add(B1Laser(-20, y, 0, B(TT), 2 * SB, 30)); });
		}
		// back together (b252-282): all of it at once - the hands with their rings, the mark's lasers, one spiral
		for (int n = 0; n < 13; n++)
		{
			double T = 258 + 2 * n; if (T > 281) break; int q = n % 4, pq = (q + 3) % 4; var R = B1Quad(q); var P = B1Quad(pq);
			double cx = R.x + R.w / 2, cy = R.y + R.h / 2, ga = JS.atan2(P.y + P.h / 2 - cy, P.x + P.w / 2 - cx), TT = T;
			at(TT - 2, () => add(B1Slam(q, B(TT), 2 * SB)));
			at(TT, () => add(B1Ring(cx, cy, B(TT), 480, ga, 0.5)));
		}
		for (double T = 257; T < 282; T += 2)
		{
			double TT = T;
			at(TT - 1.5, () => { var p = player; add(B1Laser(B1X, B1Y, JS.atan2(p.y - B1Y, p.x - B1X), B(TT), 1.5 * SB)); }, rnd: true);
		}
		{
			double th = 0;
			for (double b = 256; b < 282.5; b += 0.5) { double bb = b; th += 0.42; double t0 = th; at(bb - 0.5, () => add(B1Shot(B1X, B1Y, t0, 300, B(bb), 0.5 * SB))); }
		}
		// the dip: everything stops, one circle low in the middle
		at(282.3, () => add(B1Circle(t => (640, 610), B(282.5), B(288), 60)));
		// the end (b288-316): the walls close in, a ring every beat with a gap that sways around a turning line
		at(286, () => add(B1Walls(288, 300, 316, 318, 290, 150)));
		for (double T = 289; T < 316; T += 1)
		{
			double TT = T, ga = -Math.PI / 2 + (T - 288) / 4 * (Math.PI / 6) + ((int)(T - 289) % 2 == 0 ? 1 : -1) * 22 * Math.PI / 180;
			at(TT, () => add(B1Ring(B1X, B1Y, B(TT), 360, ga, 0.45)));
		}
		at(319, () => add(B1Shatter(B(320))));

		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
