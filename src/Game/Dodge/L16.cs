using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L16 "عين بعين" (Hussam Al-Rassam): a sad Iraqi night, and the girl behind it.
// Part 1 the night by the river (she is not seen): rain curtains bent by the wind with one way through, her tears falling on you and
// ringing out on the water, the heavy waves of the Tigris, lightning.
// Part 2 the memories (still unseen): palm fronds swinging like slow pendulums, the river rising from below and from above, incense smoke
// twisting up in columns, the storm.
// Part 3 she appears: her hair as turning lasers, rings of light with one gap, X slashes on you, her pink shadow dashing across, the
// spiral of her dance with pillars, and at the end the rings of her dress closing in until she fades away.
// Every hazard is a pure function of time once it exists (aimed ones also of where the square was when they locked), so hit(p, t) is
// right for any later t. The data is data/dodge_L16.json (made by tools/level16/gen.py).
public static partial class G
{
	static double L16k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double L16s(double k) => k * k * (3 - 2 * k);
	static string L16G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double L16ad(double a, double b) => JS.atan2(JS.sin(a - b), JS.cos(a - b));
	static double L16m(Player p) => p.r - 2;   // how much of the square counts on top of an attack's own size
	static double L16Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static bool L16LineHit(Player p, IList<(double x, double y)> pts, double th)
	{
		double m = th / 2 + L16m(p);
		for (int i = 0; i + 1 < pts.Count; i++) if (L16Seg(p.x, p.y, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y) < m) return true;
		return false;
	}
	static void L16Line(IList<(double x, double y)> pts, string col, double w, bool dash = false)
	{
		if (pts.Count < 2) return;
		g.strokeStyle = col; g.lineWidth = w; g.lineJoin = "round"; g.lineCap = "round"; if (dash) g.setLineDash(new double[] { 12, 10 });
		g.beginPath(); g.moveTo(pts[0].x, pts[0].y); for (int i = 1; i < pts.Count; i++) g.lineTo(pts[i].x, pts[i].y); g.stroke();
		if (dash) g.setLineDash(new double[0]); g.lineCap = "butt"; g.lineJoin = "miter";
	}
	static void L16Glow(double b) { g.shadowColor = PINK; g.shadowBlur = b; }
	// a box (cx,cy) w x h turned a; is the square in it (m on top)
	static bool L16InBox(Player p, double cx, double cy, double w, double h, double a)
	{
		double dx = p.x - cx, dy = p.y - cy, c = JS.cos(a), s = JS.sin(a), u = dx * c + dy * s, v = -dx * s + dy * c, m = L16m(p);
		return Math.Abs(u) < w / 2 + m && Math.Abs(v) < h / 2 + m;
	}
	static void L16Box(double cx, double cy, double w, double h, double a)
	{
		g.save(); g.translate(cx, cy); g.rotate(a); g.fillRect(-w / 2, -h / 2, w, h); g.restore();
	}
	static uint L16next(ref uint s) { s = s * 1664525u + 1013904223u; return s; }
	static double L16rf(ref uint s) => (L16next(ref s) >> 8) / 16777216.0;

	// ---------- the girl (harmless; only her pink attacks hurt) ----------
	// where she stands (and how much she is seen) at beat b: hidden till 188, gone while her shadow dashes, fades at the end
	static readonly double[][] L16GK =
	{
		new[] { 188.0, 640, 250, 1 }, new[] { 204.0, 640, 250, 1 }, new[] { 205.0, 640, 230, 1 }, new[] { 220.0, 640, 230, 1 }, new[] { 221.0, 640, 190, 1 },
		new[] { 236.0, 640, 190, 1 }, new[] { 237.0, 640, 320, 1 }, new[] { 251.5, 640, 320, 1 }, new[] { 252.2, 640, 320, 0 }, new[] { 259.3, 640, 320, 0 },
		new[] { 260.0, 640, 320, 1 }, new[] { 287.5, 640, 320, 1 }, new[] { 288.2, 640, 320, 0 }, new[] { 311.3, 640, 240, 0 }, new[] { 312.0, 640, 240, 1 },
		new[] { 319.5, 640, 240, 1 }, new[] { 320.5, 640, 330, 1 }, new[] { 328.6, 640, 330, 1 }, new[] { 329.4, 640, 330, 0 },
	};
	public static (double x, double y, double a) L16GirlAt(double t)
	{
		double b = BT(t); if (b < 188) return (640, 250, 0);
		var K = L16GK; int i = 0; while (i + 1 < K.Length && K[i + 1][0] <= b) i++;
		double x, y, a;
		if (i + 1 >= K.Length) { x = K[i][1]; y = K[i][2]; a = K[i][3]; }
		else { double k = L16s(clamp((b - K[i][0]) / (K[i + 1][0] - K[i][0]), 0, 1)); x = K[i][1] + (K[i + 1][1] - K[i][1]) * k; y = K[i][2] + (K[i + 1][2] - K[i][2]) * k; a = K[i][3] + (K[i + 1][3] - K[i][3]) * k; }
		if (b < 189) a *= clamp(b - 188, 0, 1);
		// she drifts slowly, and from 2:36 she walks from side to side
		double sway = b >= 205 && b < 220 ? 300 * JS.sin((b - 205) * Math.PI / 8) : 36 * JS.sin(b * 0.4);
		return (x + sway, y + 10 * JS.sin(b * 0.9), a);
	}
	// her head (the hair comes from it) and her hands (rings and fans come from them)
	static (double x, double y) L16Head(double t) { var q = L16GirlAt(t); return (q.x, q.y - 86); }
	static void L16GirlDraw(double x, double y, double s, double t, double a, string body = null)
	{
		if (a <= 0.01) return;
		g.save(); g.globalAlpha = a;
		double sw = JS.sin(t * 2.0) * 0.12; bool pink = body != null;
		string bodyC = body ?? "rgba(56,30,50,1)", line = pink ? "rgba(255,238,246,1)" : "rgba(240,205,222,0.9)", hairC = pink ? "rgba(255,110,160,1)" : "rgba(36,16,30,1)";
		// hair flowing down her back
		g.fillStyle = hairC; g.beginPath(); g.moveTo(x - s * 0.22, y - s * 0.95);
		for (int i = 0; i <= 8; i++) { double k = i / 8.0; g.lineTo(x - s * (0.3 + 0.35 * k) + JS.sin(t * 3 + k * 4) * s * 0.1, y - s * 0.8 + k * s * 1.4); }
		for (int i = 8; i >= 0; i--) { double k = i / 8.0; g.lineTo(x + s * (0.3 + 0.35 * k) + JS.sin(t * 3 + k * 4 + 1) * s * 0.1, y - s * 0.8 + k * s * 1.4); }
		g.closePath(); g.fill(); if (!pink) { g.strokeStyle = "rgba(240,205,222,0.35)"; g.lineWidth = 1.5; g.stroke(); }
		// the dress
		g.fillStyle = bodyC; g.beginPath(); g.moveTo(x - s * 0.16, y - s * 0.45); g.lineTo(x + s * 0.16, y - s * 0.45); g.lineTo(x + s * (0.8 + sw), y + s * 1.3); g.lineTo(x - s * (0.8 - sw), y + s * 1.3); g.closePath(); g.fill();
		g.strokeStyle = line; g.lineWidth = 2; g.stroke();
		if (!pink) { g.strokeStyle = "rgba(110,70,96,0.8)"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 5; i++) { g.moveTo(x, y - s * 0.3); g.lineTo(x + (i - 2) * s * 0.33 + sw * s, y + s * 1.25); } g.stroke(); }
		// arms
		g.strokeStyle = line; g.lineWidth = Math.Max(3, s * 0.06); g.lineCap = "round"; g.beginPath();
		for (int sg = -1; sg <= 1; sg += 2) { g.moveTo(x + sg * s * 0.15, y - s * 0.4); g.lineTo(x + sg * s * 0.62, y - s * 0.05 + JS.sin(t * 2 + sg) * s * 0.12); }
		g.stroke(); g.lineCap = "butt";
		// the head, the crown and the earrings
		double r = s * 0.2; g.fillStyle = bodyC; g.beginPath(); g.arc(x, y - s * 0.78, r, 0, TAU); g.fill(); g.strokeStyle = line; g.lineWidth = 2; g.stroke();
		if (!pink)
		{
			g.fillStyle = "rgba(255,200,80,1)"; g.beginPath(); g.moveTo(x - r * 0.9, y - s * 0.93); g.lineTo(x - r * 0.6, y - s * 1.14); g.lineTo(x - r * 0.2, y - s * 0.98); g.lineTo(x, y - s * 1.22);
			g.lineTo(x + r * 0.2, y - s * 0.98); g.lineTo(x + r * 0.6, y - s * 1.14); g.lineTo(x + r * 0.9, y - s * 0.93); g.closePath(); g.fill();
			g.strokeStyle = "rgba(255,200,80,1)"; g.lineWidth = 2; g.beginPath(); g.arc(x - r, y - s * 0.7, 5, 0, TAU); g.stroke(); g.beginPath(); g.arc(x + r, y - s * 0.7, 5, 0, TAU); g.stroke();
		}
		g.restore();
	}

	// ---------- the room (harmless) ----------
	// One hazard for the whole level, made first (and first again after a checkpoint), so it is always drawn under every attack.
	// Part k runs from beat pb[k-1]: 1 the night by the river (moon, palms, the water), 2 the same night, mistier, 3-4 her hall, and her
	public static Hz L16Stage(double[] pb, double t1)
	{
		var h = new Hz { kind = "L16Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < pb.Length; k++)
			{
				double s0 = B(pb[k]), s1 = k + 1 < pb.Length ? B(pb[k + 1]) : t1;
				double a = Math.Min(1, Math.Min((t - s0) / 0.8 + (k == 0 ? 0 : 0.5), (s1 - t) / 0.8 + (k + 1 < pb.Length ? 0.5 : 0)));
				if (a > 0) Part(k + 1, a, t);
			}
			var q = L16GirlAt(t); L16GirlDraw(q.x, q.y, 110, t, q.a);
		};
		void Palm(double px, double hgt, double t)
		{
			g.strokeStyle = "rgba(44,22,40,1)"; g.lineWidth = 10; g.beginPath(); g.moveTo(px, H); g.quadraticCurveTo(px - 10, H - hgt / 2, px + 18, H - hgt); g.stroke();
			g.lineWidth = 6; g.lineCap = "round";
			for (int i = 0; i < 7; i++)
			{
				double a = -Math.PI / 2 + (i - 3) * 0.45 + JS.sin(t * 0.7 + i) * 0.04; g.beginPath(); g.moveTo(px + 18, H - hgt);
				for (int k = 1; k <= 6; k++) g.lineTo(px + 18 + JS.cos(a + 0.04 * k * (i > 3 ? 1 : -1)) * 22 * k, H - hgt + JS.sin(a) * 22 * k + 3 * k * k);
				g.stroke();
			}
			g.lineCap = "butt";
		}
		void Part(int part, double a, double t)
		{
			g.save(); g.globalAlpha = a;
			g.fillStyle = part == 1 ? "rgba(14,8,22,0.75)" : part == 2 ? "rgba(8,10,26,0.78)" : part == 3 ? "rgba(24,6,14,0.72)" : "rgba(32,4,12,0.74)"; g.fillRect(0, 0, W, H);
			if (part <= 2)
			{
				// the moon, the palms and the river
				g.fillStyle = "rgba(64,52,74,0.9)"; g.beginPath(); g.arc(1060, 100, 50, 0, TAU); g.fill();
				g.fillStyle = part == 1 ? "rgba(14,8,22,1)" : "rgba(8,10,26,1)"; g.beginPath(); g.arc(1080, 90, 46, 0, TAU); g.fill();
				Palm(90, 300, t); Palm(210, 230, t); Palm(1180, 270, t);
				g.strokeStyle = "rgba(40,30,62,0.9)"; g.lineWidth = 2;
				for (int i = 0; i < 4; i++)
				{
					double y = 600 + i * 28, ph = t * 1.4 + i; g.beginPath();
					for (int x = 0; x <= W + 40; x += 40) { double yy = y + JS.sin(x / 90.0 + ph) * 4; if (x == 0) g.moveTo(x, yy); else g.lineTo(x, yy); }
					g.stroke();
				}
				if (part == 2)
				{
					for (int i = 0; i < 6; i++)
					{
						double mx = ((i * 260 + t * (14 + i * 3)) % (W + 400)) - 200, my = 160 + i * 80;
						var mg = g.createRadialGradient(mx, my, 0, mx, my, 220); mg.addColorStop(0, "rgba(90,90,130,0.07)"); mg.addColorStop(1, "rgba(90,90,130,0)"); g.fillStyle = mg; circle(mx, my, 220);
					}
				}
			}
			else
			{
				// her hall: gold dust drifting and a soft light where she stands
				var q = L16GirlAt(t);
				if (q.a > 0.01) { var lg = g.createRadialGradient(q.x, q.y, 0, q.x, q.y, 420); lg.addColorStop(0, "rgba(120,40,70," + str(0.22 * q.a) + ")"); lg.addColorStop(1, "rgba(120,40,70,0)"); g.fillStyle = lg; circle(q.x, q.y, 420); }
				g.fillStyle = "rgba(255,200,80,0.18)";
				for (int i = 0; i < 50; i++) { double x = (i * 211.7) % W + JS.sin(t * 0.7 + i) * 20, y = H - (((i * 97.3) + t * (20 + i % 5 * 7)) % (H + 20)) + 10; g.fillRect(x, y, 2, 2); }
			}
			var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 780); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		}
		return h;
	}

	// ---------- part 1: the night by the river ----------
	// a rain curtain: one row of long drops (len x w) in columns sp apart (the first at x0), leaning (dx per dy), falls through the room at
	// v px/s from ta; the columns whose top lies within gw of gx are missing (the way through). Their paths show from tw
	public static Hz L16Rain(double x0, double sp, double lean, double gx, double gw, double len, double w, double v, double tw, double ta)
	{
		var h = new Hz { kind = "L16Rain" };
		var xs = new List<double>();
		for (double x = x0 - 4 * sp; x < W + 4 * sp; x += sp) { double xm = x + lean * H / 2; if (Math.Abs(xm - gx) >= gw && xm > -lean * H / 2 - 100 && xm < W - lean * H / 2 + 100 + Math.Abs(lean) * H) xs.Add(x); }
		double yc(double t) => -len / 2 - 20 + v * (t - ta);
		double te = ta + (H + len + 60) / v;
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); g.strokeStyle = L16G(0.06 + 0.16 * k); g.lineWidth = 1.5; g.beginPath();
				foreach (var x in xs) { g.moveTo(x, 0); g.lineTo(x + lean * H, H); }
				g.stroke(); return;
			}
			double y = yc(t); g.save(); L16Glow(10); g.strokeStyle = PINK; g.lineWidth = w; g.lineCap = "round"; g.beginPath();
			foreach (var x in xs) { g.moveTo(x + lean * (y - len / 2), y - len / 2); g.lineTo(x + lean * (y + len / 2), y + len / 2); }
			g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double y = yc(t), m = w / 2 + L16m(p); if (p.y < y - len / 2 - m - 2 || p.y > y + len / 2 + m + 2) return false;
			foreach (var x in xs) if (L16Seg(p.x, p.y, x + lean * (y - len / 2), y - len / 2, x + lean * (y + len / 2), y + len / 2) < m) return true;
			return false;
		};
		return h;
	}

	// a tear: falls from above onto (x,y) from tw to ta (a ring marks the splash, sr round), splashes there for 0.2 s, then one ring rings out
	// on the water at v px/s up to R (th thick) with two gaps (gw wide, at ga and across from it); the gaps show from tw
	public static Hz L16Tear(double x, double y, double sr, double ga, double gw, double v, double R, double th, double tw, double ta)
	{
		var h = new Hz { x = x, y = y, kind = "L16Tear" };
		double te = ta + (R - sr) / v + 0.1;
		(double x, double y) drop(double t) { double k = L16k(t, tw, ta); return (x, -60 + (y + 60) * k * k); }
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); var d = drop(t);
				g.fillStyle = L16G(0.05 + 0.1 * k); circle(x, y, sr);
				g.strokeStyle = L16G(0.3 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.arc(x, y, sr, 0, TAU); g.stroke();
				g.setLineDash(new double[] { 10, 8 }); g.lineWidth = 2;
				for (int q = 0; q < 2; q++) { double a0 = ga + q * Math.PI + gw / 2, a1 = ga + (q + 1) * Math.PI - gw / 2; g.beginPath(); g.arc(x, y, sr + 70, a0, a1); g.stroke(); }
				g.setLineDash(new double[0]);
				g.save(); L16Glow(12); g.fillStyle = PINK; g.beginPath(); g.moveTo(d.x, d.y - 46); g.quadraticCurveTo(d.x + 20, d.y - 4, d.x, d.y + 16); g.quadraticCurveTo(d.x - 20, d.y - 4, d.x, d.y - 46); g.fill(); g.restore();
				return;
			}
			g.save(); L16Glow(12);
			if (t < ta + 0.2) { g.globalAlpha = 1 - (t - ta) / 0.2 * 0.3; g.fillStyle = PINK; circle(x, y, sr); g.globalAlpha = 1; }
			double r = sr + (t - ta) * v;
			if (r < R) { g.globalAlpha = Math.Min(1, (R - r) / 80); g.strokeStyle = PINK; g.lineWidth = th; for (int q = 0; q < 2; q++) { g.beginPath(); g.arc(x, y, r, ga + q * Math.PI + gw / 2, ga + (q + 1) * Math.PI - gw / 2); g.stroke(); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw) return false;
			if (t < ta) { var d = drop(t); return hypot(p.x - d.x, p.y - (d.y - 10)) < 18 + L16m(p); }
			double dd = hypot(p.x - x, p.y - y), m = L16m(p);
			if (t < ta + 0.2 && dd < sr + m) return true;
			double r = sr + (t - ta) * v; if (r > R - 40 || Math.Abs(dd - r) >= th / 2 + m) return false;
			double da = Math.Abs(L16ad(JS.atan2(p.y - y, p.x - x), ga)); if (da > Math.PI / 2) da = Math.PI - da;
			return !(da < gw / 2 && (gw / 2 - da) * Math.Max(dd, 1) > m + 2);
		};
		return h;
	}

	// the river: n heavy waves (th thick) span the room, y = y0 + k*dy + lift sin(wl t) + A sin(x / L + ws t + k * 1.3); they show as dashed
	// lines from tw, swell in from ta, and sink away from te
	public static Hz L16Waves(int n, double y0, double dy, double lift, double wl, double A, double L, double ws, double th, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16Waves" };
		double Y(int k, double x, double t) { double u = t - ta; return y0 + k * dy + lift * JS.sin(wl * u) + A * JS.sin(x / L + ws * u + k * 1.3); }
		double thick(double t) => th * Math.Min(L16k(t, ta, ta + 0.3), 1 - L16k(t, te, te + 0.3));
		h.update = (t, dt) => { if (t > te + 0.35) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta);
				for (int j = 0; j < n; j++) { var pts = new List<(double, double)>(); for (int x = -20; x <= W + 20; x += 20) pts.Add((x, Y(j, x, ta))); L16Line(pts, L16G(0.2 + 0.4 * k), 2, true); }
				return;
			}
			double tk = thick(t); if (tk < 0.5) return;
			g.save(); L16Glow(14);
			for (int j = 0; j < n; j++) { var pts = new List<(double, double)>(); for (int x = -20; x <= W + 20; x += 16) pts.Add((x, Y(j, x, t))); L16Line(pts, PINK, tk); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te + 0.3) return false;
			double tk = thick(t); if (tk < 4) return false;
			for (int j = 0; j < n; j++)
			{
				double y = Y(j, p.x, t), sl = A / L * JS.cos(p.x / L + ws * (t - ta) + j * 1.3);
				if (Math.Abs(p.y - y) / Math.Sqrt(1 + sl * sl) < tk / 2 + L16m(p)) return true;
			}
			return false;
		};
		return h;
	}

	// lightning: a jagged bolt from the top edge down through x, with a fork; its path shows (dashed) from tw, it strikes at ta for hold s
	public static Hz L16Bolt(double x, uint seed, double th, double tw, double ta, double hold)
	{
		var h = new Hz { x = x, kind = "L16Bolt" };
		var main = new List<(double x, double y)>(); var fork = new List<(double x, double y)>(); uint s = seed * 2654435761u + 11; L16next(ref s);
		double cx = x; for (int i = 0; i <= 10; i++) { main.Add((cx, i * H / 10.0)); cx = x + (L16rf(ref s) - 0.5) * 120; }
		int fi = 3 + (int)(L16rf(ref s) * 4); double fx = main[fi].x, fy = main[fi].y, dir = L16rf(ref s) < 0.5 ? -1 : 1;
		for (int i = 0; i <= 5; i++) { fork.Add((fx, fy)); fx += dir * (50 + L16rf(ref s) * 40); fy += 60 + L16rf(ref s) * 30; }
		h.update = (t, dt) => { if (t > ta + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > ta + hold + 0.15) return;
			if (t < ta) { double k = L16k(t, tw, ta); L16Line(main, L16G(0.2 + 0.55 * k), 2, true); L16Line(fork, L16G(0.15 + 0.45 * k), 2, true); return; }
			if (t >= ta + hold) return;
			g.save(); L16Glow(20); L16Line(main, PINK, th); L16Line(fork, PINK, th * 0.7); L16Line(main, "rgba(255,190,215,0.8)", th * 0.3); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < ta + hold && (L16LineHit(p, main, th) || L16LineHit(p, fork, th * 0.7));
		return h;
	}

	// ---------- part 2: the memories ----------
	// a palm frond on a pivot (px,py) off the edge: it swings from angle a0 to a1 between ta and te (eased), L long; its arc shows from tw
	public static Hz L16Frond(double px, double py, double a0, double a1, double L, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16Frond" };
		double side = JS.cos((a0 + a1) / 2) >= 0 ? 1 : -1;
		List<(double x, double y)> spine(double a)
		{
			var o = new List<(double, double)>();
			for (int i = 0; i <= 12; i++) { double k = i / 12.0; o.Add((px + JS.cos(a) * L * k - JS.sin(a) * 70 * k * k * side, py + JS.sin(a) * L * k + JS.cos(a) * 70 * k * k * side)); }
			return o;
		}
		List<((double x, double y) a, (double x, double y) b)> leaves(List<(double x, double y)> sp)
		{
			var o = new List<((double, double), (double, double))>();
			for (int i = 2; i < 12; i++)
			{
				var q0 = sp[i]; var q1 = sp[i + 1]; double ang = JS.atan2(q1.y - q0.y, q1.x - q0.x), ll = 120 * (1 - 0.55 * i / 12.0);
				for (int sg = -1; sg <= 1; sg += 2) { double la = ang + sg * 0.7; o.Add((q0, (q0.x + JS.cos(la) * ll, q0.y + JS.sin(la) * ll))); }
			}
			return o;
		}
		double ang(double t) => a0 + (a1 - a0) * L16s(L16k(t, ta, te));
		h.update = (t, dt) => { if (t > te + 0.05) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); g.strokeStyle = L16G(0.2 + 0.45 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				double lo = Math.Min(a0, a1), hi = Math.Max(a0, a1); g.arc(px, py, L * 0.85, lo, hi); g.stroke(); g.setLineDash(new double[0]);
				L16Line(spine(a0), L16G(0.2 + 0.45 * k), 2, true); return;
			}
			var sp = spine(ang(t)); g.save(); L16Glow(10); L16Line(sp, PINK, 18);
			g.strokeStyle = PINK; g.lineWidth = 11; g.lineCap = "round"; g.beginPath(); foreach (var lf in leaves(sp)) { g.moveTo(lf.a.x, lf.a.y); g.lineTo(lf.b.x, lf.b.y); } g.stroke(); g.lineCap = "butt"; g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false;
			var sp = spine(ang(t)); if (L16LineHit(p, sp, 18)) return true;
			double m = 5.5 + L16m(p); foreach (var lf in leaves(sp)) if (L16Seg(p.x, p.y, lf.a.x, lf.a.y, lf.b.x, lf.b.y) < m) return true;
			return false;
		};
		return h;
	}

	// the tide: the river rises from the bottom (top: from the top) to lvl px with a wavy edge and sinks back, ta to te; its high mark shows from tw
	public static Hz L16Tide(bool top, double lvl, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16Tide" };
		double level(double t) => lvl * JS.sin(Math.PI * L16k(t, ta, te)) + (t >= ta && t < te ? 0 : -100);
		double edge(double x, double t, double lv) { double yy = lv + 24 * JS.sin(x / 120 + t * 3.5); return top ? yy : H - yy; }
		h.update = (t, dt) => { if (t > te + 0.05) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); var pts = new List<(double, double)>(); for (int x = 0; x <= W; x += 20) pts.Add((x, top ? lvl : H - lvl)); L16Line(pts, L16G(0.2 + 0.5 * k), 2, true);
				g.fillStyle = L16G(0.03 + 0.05 * k); g.fillRect(0, top ? 0 : H - lvl, W, lvl); return;
			}
			double lv = level(t); if (lv <= 0) return;
			g.save(); L16Glow(14); g.fillStyle = PINK; g.beginPath(); g.moveTo(0, top ? -10 : H + 10);
			for (int x = 0; x <= W; x += 20) g.lineTo(x, edge(x, t, lv));
			g.lineTo(W, top ? -10 : H + 10); g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double lv = level(t); if (lv <= 0) return false; double y = edge(p.x, t, lv), m = L16m(p);
			return top ? p.y - m < y : p.y + m > y;
		};
		return h;
	}

	// incense smoke: n columns (at cx[k]) of rising puffs (r) that sway A px; column k sways the other way to k+1. From ta to te (grow and
	// fade 0.4 s), shown as dashed lines from tw. The whole set drifts dr px to the sides and back
	public static Hz L16Smoke(double[] cx, double A, double r, double dr, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16Smoke" };
		double X(int k, double y, double t) { double u = t - ta, x = cx[k] + dr * JS.sin(u * 0.55); return x + A * JS.sin(y / 130 - u * 1.25 + k * 2.1) * (k % 2 == 0 ? 1 : -1); }
		double rad(double t) => r * Math.Min(L16k(t, ta, ta + 0.4), 1 - L16k(t, te - 0.4, te));
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta);
				for (int j = 0; j < cx.Length; j++) { var pts = new List<(double, double)>(); for (int y = -20; y <= H + 20; y += 20) pts.Add((X(j, y, ta), y)); L16Line(pts, L16G(0.18 + 0.4 * k), 2, true); }
				return;
			}
			double rr = rad(t); if (rr < 1) return; double u = t - ta, sh = (u * 120) % 46;
			g.save(); L16Glow(12); g.fillStyle = PINK;
			for (int j = 0; j < cx.Length; j++)
			{
				// one band per column (drawn in pieces where it wraps round the room), puffs on it
				// (drawn again a room's width each side, so a column on the edge shows on both sides like it hits)
				for (int c = 0; c <= 0; c++)
				{
					var pts = new List<(double, double)>(); double px0 = double.NaN;
					for (int y = -30; y <= H + 30; y += 12)
					{
						double x = X(j, y, t); if (!double.IsNaN(px0) && Math.Abs(x - px0) > W / 2) { L16Line(pts, PINK, 2 * rr * 0.9); pts.Clear(); }
						pts.Add((x + c * W, y)); px0 = x;
					}
					L16Line(pts, PINK, 2 * rr * 0.9);
					for (int i = -1; i < 18; i++) { double y = H + 20 - i * 46 - sh; circle(X(j, y, t) + c * W, y, rr * (0.92 + 0.14 * JS.sin(i * 1.7 + j))); }
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double rr = rad(t) * 0.86; if (rr < 4) return false;
			for (int j = 0; j < cx.Length; j++)
			{
				double x = X(j, p.y, t), dx = Math.Abs(p.x - x);
				double sl = A / 130 * JS.cos(p.y / 130 - (t - ta) * 1.25 + j * 2.1); if (dx / Math.Sqrt(1 + sl * sl) < rr + L16m(p)) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- part 3: she appears ----------
	// her hair: n thick wavy strands (th) from her head, turning; the turn speed changes every 4 beats (ws, rad/beat, from beat b0). The
	// strands show as thin lines from tw, they are on from ta to te
	public static Hz L16Hair(int n, double b0, double[] ws, double len, double th, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16Hair" };
		double ang(double t)
		{
			double b = BT(t) - b0, a = 0; int i = 0;
			while (b > 4 && i < ws.Length - 1) { a += ws[i] * 4; b -= 4; i++; }
			return a + ws[Math.Min(i, ws.Length - 1)] * Math.Max(0, b);
		}
		List<(double x, double y)> strand(int j, double t, double tk)
		{
			var hd = L16Head(t); double a = ang(t) + j * TAU / n; var o = new List<(double, double)>();
			for (int i = 0; i <= 16; i++) { double k = i / 16.0, r = 70 + (len - 70) * k, off = JS.sin(t * 3 + j + k * 4) * 30 * k; o.Add((hd.x + JS.cos(a) * r - JS.sin(a) * off, hd.y + JS.sin(a) * r + JS.cos(a) * off)); }
			return o;
		}
		double thick(double t) => th * Math.Min(L16k(t, ta, ta + 0.25), 1 - L16k(t, te - 0.3, te));
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta) { double k = L16k(t, tw, ta); for (int j = 0; j < n; j++) L16Line(strand(j, t, 0), L16G(0.25 + 0.4 * k), 2, true); return; }
			double tk = thick(t); g.save(); L16Glow(16); for (int j = 0; j < n; j++) L16Line(strand(j, t, tk), PINK, tk); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double tk = thick(t); if (tk < 6) return false;
			for (int j = 0; j < n; j++) if (L16LineHit(p, strand(j, t, tk), tk)) return true;
			return false;
		};
		return h;
	}

	// a ring of n lights from (x,y) at ta with one gap (gw radians at ga), growing at v px/s; it shows as a dashed ring with an arrow at the
	// gap from tw
	public static Hz L16Fan(double x, double y, int n, double ga, double gw, double v, double r, double tw, double ta)
	{
		var h = new Hz { x = x, y = y, kind = "L16Fan" };
		double te = ta + 1700 / v;
		double ad(int i) => ga + gw / 2 + i * (TAU - gw) / (n - 1);
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); g.strokeStyle = L16G(0.25 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(x, y, 60, ga + gw / 2, ga - gw / 2 + TAU); g.stroke(); g.setLineDash(new double[0]);
				g.beginPath(); g.moveTo(x + JS.cos(ga) * 70, y + JS.sin(ga) * 70); g.lineTo(x + JS.cos(ga) * 150, y + JS.sin(ga) * 150); g.stroke(); return;
			}
			double R = 50 + (t - ta) * v; g.save(); L16Glow(10); g.fillStyle = PINK;
			for (int i = 0; i < n; i++) { double a = ad(i); circle(x + JS.cos(a) * R, y + JS.sin(a) * R, r); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double R = 50 + (t - ta) * v, d = hypot(p.x - x, p.y - y), m = r + L16m(p);
			if (Math.Abs(d - R) >= m) return false;
			for (int i = 0; i < n; i++) { double a = ad(i); if (hypot(p.x - x - JS.cos(a) * R, p.y - y - JS.sin(a) * R) < m) return true; }
			return false;
		};
		return h;
	}

	// an X slash on (x,y): two bars (len x w) crossed, turned a; shown from tw, cut from ta for hold s
	public static Hz L16Slash(double x, double y, double a, double w, double tw, double ta, double hold)
	{
		var h = new Hz { x = x, y = y, kind = "L16Slash" };
		h.update = (t, dt) => { if (t > ta + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > ta + hold + 0.15) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); g.fillStyle = L16G(0.04 + 0.08 * k); L16Box(x, y, 2400, w, a); L16Box(x, y, 2400, w, a + Math.PI / 2);
				g.strokeStyle = L16G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 });
				for (int q = 0; q < 2; q++) { double aa = a + q * Math.PI / 2; for (int sg = -1; sg <= 1; sg += 2) { double ox = -JS.sin(aa) * w / 2 * sg, oy = JS.cos(aa) * w / 2 * sg; g.beginPath(); g.moveTo(x + ox - JS.cos(aa) * 1300, y + oy - JS.sin(aa) * 1300); g.lineTo(x + ox + JS.cos(aa) * 1300, y + oy + JS.sin(aa) * 1300); g.stroke(); } }
				g.setLineDash(new double[0]); return;
			}
			if (t >= ta + hold) return;
			g.save(); L16Glow(18); g.fillStyle = PINK; L16Box(x, y, 2400, w, a); L16Box(x, y, 2400, w, a + Math.PI / 2); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < ta + hold && (L16InBox(p, x, y, 2400, w, a) || L16InBox(p, x, y, 2400, w, a + Math.PI / 2));
		return h;
	}

	// her pink shadow dashes across the room along a lane (vertical: at x = c, down if dir 1; else at y = c, right if dir 1), from ta in
	// dur s; the lane shows from tw. It is a pink girl, 300 tall; it hits as a box round her
	public static Hz L16Dash(bool vert, double c, int dir, double tw, double ta, double dur)
	{
		var h = new Hz { kind = "L16Dash" };
		(double x, double y) at(double t)
		{
			double k = L16k(t, ta, ta + dur), u = -260 + k * ((vert ? H : W) + 520); if (dir < 0) u = (vert ? H : W) - u;
			return vert ? (c, u) : (u, c);
		}
		h.update = (t, dt) => { if (t > ta + dur) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= ta + dur) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta); g.fillStyle = L16G(0.03 + 0.08 * k); g.strokeStyle = L16G(0.3 + 0.55 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 });
				if (vert) { g.fillRect(c - 80, 0, 160, H); g.strokeRect(c - 80, 0, 160, H); } else { g.fillRect(0, c - 140, W, 280); g.strokeRect(0, c - 140, W, 280); }
				g.setLineDash(new double[0]); return;
			}
			var q = at(t); g.save(); L16Glow(22);
			for (int i = 4; i >= 1; i--) { var q2 = at(t - i * 0.03); g.globalAlpha = 0.08 * (5 - i) / 4; g.fillStyle = PINK; g.beginPath(); g.roundRect(q2.x - 75, q2.y + 5 - 130, 150, 260, 22); g.fill(); }
			g.globalAlpha = 1; g.fillStyle = PINK; g.beginPath(); g.roundRect(q.x - 75, q.y + 5 - 130, 150, 260, 22); g.fill(); g.shadowBlur = 0;
			L16GirlDraw(q.x, q.y - 20, 82, t, 1, PINK); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= ta + dur) return false; var q = at(t); return L16InBox(p, q.x, q.y + 5, 146, 256, 0); };
		return h;
	}

	// the spiral of her dance: from (x,y) arms lights leave every pt s from t0 to t1, at v px/s, turning by sp rad/s (arm k at k*TAU/arms)
	public static Hz L16Spiral(double x, double y, int arms, double pt, double v, double sp, double r, double t0, double t1)
	{
		var h = new Hz { x = x, y = y, kind = "L16Spiral" };
		double te = t1 + 900 / v;
		IEnumerable<(double x, double y)> lights(double t)
		{
			int i0 = (int)Math.Max(0, Math.Floor((t - t0 - 900 / v) / pt)), i1 = (int)Math.Floor((Math.Min(t, t1) - t0) / pt);
			for (int i = i0; i <= i1; i++)
			{
				double tb = t0 + i * pt, R = 40 + (t - tb) * v; if (R > 900 || tb > t) continue;
				for (int k = 0; k < arms; k++) { double a = k * TAU / arms + (tb - t0) * sp; yield return (x + JS.cos(a) * R, y + JS.sin(a) * R); }
			}
		}
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t => { if (t < t0) return; g.save(); L16Glow(10); g.fillStyle = PINK; foreach (var q in lights(t)) circle(q.x, q.y, r); g.restore(); };
		h.hit = (p, t) => { if (t < t0) return false; double m = r + L16m(p); foreach (var q in lights(t)) if (Math.Abs(q.x - p.x) < m && hypot(q.x - p.x, q.y - p.y) < m) return true; return false; };
		return h;
	}

	// a pillar of light down the room at x (w wide): shown from tw, on from ta for hold s
	public static Hz L16Pillar(double x, double w, double tw, double ta, double hold)
	{
		var h = new Hz { x = x, kind = "L16Pillar" };
		h.update = (t, dt) => { if (t > ta + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > ta + hold + 0.15) return;
			if (t < ta) { double k = L16k(t, tw, ta); g.fillStyle = L16G(0.04 + 0.1 * k); g.fillRect(x - w / 2, 0, w, H); g.strokeStyle = L16G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.strokeRect(x - w / 2, 0, w, H); g.setLineDash(new double[0]); return; }
			if (t >= ta + hold) return;
			g.save(); L16Glow(20); g.fillStyle = PINK; g.fillRect(x - w / 2, 0, w, H); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < ta + hold && Math.Abs(p.x - x) < w / 2 + L16m(p);
		return h;
	}

	// the rings of her dress: a ring (th) round (x,y) starts at R0 at ta and closes in at v px/s, gone at 70; one gap gw wide that turns
	// (ga + gs * (t - ta)). It shows as a dashed ring from tw
	public static Hz L16Close(double x, double y, double R0, double v, double th, double ga, double gs, double gw, double tw, double ta)
	{
		var h = new Hz { x = x, y = y, kind = "L16Close" };
		double te = ta + (R0 - 70) / v;
		double R(double t) => R0 - (t - ta) * v;
		double gap(double t) => ga + gs * (t - ta);
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta)
			{
				double k = L16k(t, tw, ta), a = gap(ta); g.strokeStyle = L16G(0.2 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 14, 10 }); g.beginPath(); g.arc(x, y, R0, a + gw / 2, a - gw / 2 + TAU); g.stroke(); g.setLineDash(new double[0]);
				return;
			}
			double r = R(t), ga2 = gap(t); g.save(); L16Glow(14); g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.arc(x, y, r, ga2 + gw / 2, ga2 - gw / 2 + TAU); g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double r = R(t), d = hypot(p.x - x, p.y - y), m = L16m(p); if (Math.Abs(d - r) >= th / 2 + m) return false;
			double da = Math.Abs(L16ad(JS.atan2(p.y - y, p.x - x), gap(t)));
			return !(da < gw / 2 && (gw / 2 - da) * Math.Max(d, 1) > m + 2);
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L16.json) ----------
	// times are beats. Fixed attacks fire before their warning starts (the data's b), aimed ones ("rnd") lock on the square when they fire.
	static double L16ez(double v, double ez) => easy ? v * ez : v;
	static (double x, double y) L16me() => (player?.x ?? W / 2, player?.y ?? H / 2);
	static readonly int _k16fx = DodgeKind("L16fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L16fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k * 0.4); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k16stage = DodgeKind("L16stage", e => () => hazards.Add(L16Stage(e.nums("pb"), B(e.num("to")))));
	static readonly int _k16rain = DodgeKind("L16rain", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16Rain(e.num("x0"), e.num("sp", 105), e.num("lean"), e.num("gx"), L16ez(e.num("gw", 150), 1.65), e.num("len", 190), e.num("w", 15), L16ez(e.num("v", 1000), 0.7), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16tear = DodgeKind("L16tear", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16me(); double tl = B(e.b), wn = L16ez(e.num("wn", 1.4), 1.3);
		hazards.Add(L16Tear(p.x, p.y, e.num("sr", 80), e.num("ga") * Math.PI / 180, L16ez(e.num("gw", 50), 1.4) * Math.PI / 180, L16ez(e.num("v", 420), 0.8), e.num("R", 520), e.num("th", 18), tl, tl + wn * SPB));
	});
	static readonly int _k16waves = DodgeKind("L16waves", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		double hit = e.num("hit"), ez = easy ? 0.75 : 1;
		hazards.Add(L16Waves(e.i("n", 3), e.num("y0"), L16ez(e.num("dy", 190), 1.0), e.num("lift", 70), e.num("wl", 1.6) * ez, e.num("A", 60), e.num("L", 170), e.num("ws", 2.4) * ez, L16ez(e.num("th", 36), 0.8), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k16bolt = DodgeKind("L16bolt", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		double tl = B(e.b), x = e.flag("rnd") ? L16me().x : e.num("x");
		hazards.Add(L16Bolt(x, (uint)e.i("seed"), e.num("th", 26), tl, tl + L16ez(e.num("wn", 1), 1.5) * SPB, e.num("hold", 0.35) * SPB));
	});
	static readonly int _k16frond = DodgeKind("L16frond", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16Frond(e.num("px"), e.num("py"), e.num("a0") * Math.PI / 180, e.num("a1") * Math.PI / 180, e.num("L", 760), B(hit - wn), B(hit), B(hit + L16ez(e.num("dur", 1.6), 1.25))));
	}; });
	static readonly int _k16tide = DodgeKind("L16tide", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16Tide(e.flag("top"), L16ez(e.num("lvl", 300), 0.8), B(hit - wn), B(hit), B(hit + e.num("dur", 2))));
	}; });
	static readonly int _k16smoke = DodgeKind("L16smoke", e => { double wn = e.num("wn", 1.5) * (easy ? 1.3 : 1); return () =>
	{
		double hit = e.num("hit"); var cx = e.nums("cx"); if (easy) cx = cx.Where((_, i) => i % 2 == 0).ToArray();
		hazards.Add(L16Smoke(cx, e.num("A", 95), e.num("r", 30), e.num("dr", 0), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k16hair = DodgeKind("L16hair", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		double hit = e.num("hit");
		hazards.Add(L16Hair(easy ? Math.Max(3, e.i("n", 5) - 2) : e.i("n", 5), hit, e.nums("ws").Select(v => L16ez(v, 0.75)).ToArray(), e.num("len", 1500), e.num("th", 38), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k16fan = DodgeKind("L16fan", e => { double wn = e.num("wn", 0.75) * (easy ? 1.3 : 1); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"); var q = L16GirlAt(B(hit)); double x = q.x, y = q.y - 40;
		hazards.Add(L16Fan(x, y, easy ? 20 : e.i("n", 30), e.num("ga") * Math.PI / 180, L16ez(e.num("gw", 40), 1.5) * Math.PI / 180, L16ez(e.num("v", 520), 0.72), e.num("r", 13), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16slash = DodgeKind("L16slash", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16me(); double tl = B(e.b);
		hazards.Add(L16Slash(p.x, p.y, e.num("a") * Math.PI / 180, e.num("w", 110), tl, tl + L16ez(e.num("wn", 1), 1.45) * SPB, e.num("hold", 0.4) * SPB));
	});
	static readonly int _k16dash = DodgeKind("L16dash", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16me(); double tl = B(e.b); bool v = e.flag("vert");
		hazards.Add(L16Dash(v, v ? clamp(p.x, 80, W - 80) : clamp(p.y, 120, H - 120), e.i("dir", 1), tl, tl + L16ez(e.num("wn", 1), 1.5) * SPB, L16ez(e.num("dur", 0.5), 1.4) * SPB));
	});
	static readonly int _k16spiral = DodgeKind("L16spiral", e => () =>
	{
		var q = L16GirlAt(B(e.num("t0")));
		hazards.Add(L16Spiral(q.x, q.y - 40, e.i("arms", 6), L16ez(e.num("pt", 0.125), 1.8) * SPB, L16ez(e.num("v", 360), 0.75), e.num("sp", 0.9), e.num("r", 13), B(e.num("t0")), B(e.num("t1"))));
	});
	static readonly int _k16pillar = DodgeKind("L16pillar", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16me(); double tl = B(e.b);
		hazards.Add(L16Pillar(p.x, e.num("w", 170), tl, tl + L16ez(e.num("wn", 1), 1.3) * SPB, e.num("hold", 0.45) * SPB));
	});
	static readonly int _k16close = DodgeKind("L16close", e => { double wn = e.num("wn", 1) * (easy ? 1.3 : 1); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"); var q = L16GirlAt(B(hit));
		hazards.Add(L16Close(q.x, q.y, e.num("R0", 820), L16ez(e.num("v", 300), 0.8), e.num("th", 30), e.num("ga") * Math.PI / 180, e.num("gs", 0.6), L16ez(e.num("gw", 50), 1.5) * Math.PI / 180, B(hit - wn), B(hit)));
	}; });
}
