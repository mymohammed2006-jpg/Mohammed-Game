using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 3489-3856: the L9 shapes and buildLevel9 (Right Now, a trip around the world)
public static partial class G
{
	// the world map (grey dots): every # is a dot
	static readonly string[] L9MAP = {
		"................................................",
		"......####......####........#######.............",
		"..##########...###.......###########......##....",
		".############..#.......###############..####....",
		"..###########.........#################.###.....",
		"...##########........##.####################....",
		"....########........######################......",
		".....######.........#####################.......",
		"......####..........######.....##########.......",
		".......###.........########.....#######.........",
		"........#..........#########.....####...........",
		".........###........########......##............",
		"..........####.......######........#.....##.....",
		"..........######.....#####..............####....",
		"...........#####......####.............######...",
		"............####......###...............#####...",
		"............###........#.................##.....",
		"............##..................................",
		"............#...................................",
		"................................................" };
	static double L9mx(double c) => 52 + c * 24.5;
	static double L9my(double r) => 48 + r * 31;
	// the cities (column, row) on the map, in the original's key order
	static readonly string[] L9K = { "LA", "NY", "MIA", "RIO", "LIMA", "PAR", "LON", "DKR", "CAI", "DXB", "MUM", "MOS", "TYO", "SEO", "SYD", "CPT" };
	static readonly Dictionary<string, int[]> L9C = new()
	{
		["LA"] = new[] { 3, 5 }, ["NY"] = new[] { 11, 5 }, ["MIA"] = new[] { 9, 8 }, ["RIO"] = new[] { 15, 14 }, ["LIMA"] = new[] { 11, 13 }, ["PAR"] = new[] { 23, 3 }, ["LON"] = new[] { 21, 2 }, ["DKR"] = new[] { 20, 9 },
		["CAI"] = new[] { 27, 7 }, ["DXB"] = new[] { 32, 8 }, ["MUM"] = new[] { 35, 10 }, ["MOS"] = new[] { 30, 2 }, ["TYO"] = new[] { 43, 4 }, ["SEO"] = new[] { 40, 4 }, ["SYD"] = new[] { 44, 15 }, ["CPT"] = new[] { 24, 15 }
	};
	static double[] L9city(string k) => new[] { L9mx(L9C[k][0]), L9my(L9C[k][1]) };

	// a plane seen from above: the nose points to +x
	static void L9plane(double x, double y, double a, double s, string fill)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.scale(s, s); g.fillStyle = fill; g.beginPath();
		g.moveTo(22, 0); g.quadraticCurveTo(20, -4, 12, -4); g.lineTo(2, -4); g.lineTo(-8, -20); g.lineTo(-13, -20); g.lineTo(-7, -4); g.lineTo(-16, -4); g.lineTo(-21, -10); g.lineTo(-24, -10); g.lineTo(-21, 0);
		g.lineTo(-24, 10); g.lineTo(-21, 10); g.lineTo(-16, 4); g.lineTo(-7, 4); g.lineTo(-13, 20); g.lineTo(-8, 20); g.lineTo(2, 4); g.lineTo(12, 4); g.quadraticCurveTo(20, 4, 22, 0); g.closePath(); g.fill(); g.restore();
	}
	// points of an arc path from A to B, the arc rises h
	static List<double[]> L9arc(double[] A, double[] B, double h, int n = 28)
	{
		double mx = (A[0] + B[0]) / 2, my = (A[1] + B[1]) / 2, dx = B[0] - A[0], dy = B[1] - A[1], d = hypot(dx, dy); if (d == 0 || double.IsNaN(d)) d = 1; double nx = dy / d, ny = -dx / d; if (ny > 0) { nx = -nx; ny = -ny; }
		double cx = mx + nx * h, cy = my + ny * h; var P = new List<double[]>(); for (int i = 0; i <= n; i++) { double u = (double)i / n, v = 1 - u; P.Add(new[] { v * v * A[0] + 2 * u * v * cx + u * u * B[0], v * v * A[1] + 2 * u * v * cy + u * u * B[1] }); }
		return P;
	}
	// phase 1 background: the map and the cities (grey, harmless)
	static Hz L9Map(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Max(0, Math.Min(Math.Min(1, (t - t0) / 1.5), (t1 - t) / 1.0)); if (a <= 0) return; double bt = BT(t);
			g.fillStyle = GREY + toFixed(0.2 * a, 3) + ")"; for (int r = 0; r < L9MAP.Length; r++) { var row = L9MAP[r]; for (int c = 0; c < row.Length; c++) if (row[c] == '#') { g.fillRect(L9mx(c) - 2.5, L9my(r) - 2.5, 5, 5); } }
			g.strokeStyle = GREY + toFixed(0.05 * a, 3) + ")"; g.lineWidth = 1; g.beginPath(); for (int k = 1; k < 6; k++) { double y = L9my(0) + (double)k * L9MAP.Length * 31 / 6; g.moveTo(30, y); g.lineTo(W - 30, y); }
			g.stroke();
			foreach (var k in L9K) { var q = L9city(k); double x = q[0], y = q[1], p = JS.exp(-((bt % 1 + 1) % 1) * 4); g.fillStyle = GREY + toFixed(0.35 * a, 3) + ")"; circle(x, y, 4 + 1.5 * p); }
		};
		return h;
	}
	// a flight: before take-off its path shows as dots, then a pink plane flies it and leaves a trail that stays lin seconds
	static Hz L9Route(List<double[]> P, double tw, double ta, double dur, double lin, double w = 12, int nh = 0)
	{
		var Lc = new List<double> { 0 }; for (int i = 1; i < P.Count; i++) Lc.Add(Lc[i - 1] + hypot(P[i][0] - P[i - 1][0], P[i][1] - P[i - 1][1])); double L = Lc[Lc.Count - 1]; if (L == 0 || double.IsNaN(L)) L = 1;
		Func<double, double[]> at = s =>
		{
			s = clamp(s, 0, L); int i = 1; while (i < Lc.Count - 1 && Lc[i] < s) i++; double dn = Lc[i] - Lc[i - 1]; if (dn == 0 || double.IsNaN(dn)) dn = 1; double u = (s - Lc[i - 1]) / dn;
			return new[] { P[i - 1][0] + (P[i][0] - P[i - 1][0]) * u, P[i - 1][1] + (P[i][1] - P[i - 1][1]) * u, JS.atan2(P[i][1] - P[i - 1][1], P[i][0] - P[i - 1][0]) };
		};
		Func<double, double> sH = t => clamp((t - ta) / dur, 0, 1) * L, sT = t => clamp((t - ta - lin) / dur, 0, 1) * L;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > ta + dur + lin + 0.3) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta + dur)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), s0 = sH(t), n = Math.Max(2, round(L / 26)); g.fillStyle = $"rgba(255,42,109,{str(0.16 + 0.4 * k)})";
				for (int i = 0; i <= n; i++) { double s = L * i / n; if (s < s0) continue; if (t < ta && i / n > k * 1.25 + 0.1) break; var q = at(s); double z = 1.8 + 1.4 * k; g.fillRect(q[0] - z, q[1] - z, 2 * z, 2 * z); }
			}
			if (t >= ta)
			{
				double h = sH(t), tl = sT(t);
				if (h > tl + 0.5)
				{
					double f = t > ta + dur ? Math.Max(0, 1 - (t - ta - dur) / (lin + 0.3)) : 1; g.save(); g.strokeStyle = PINK; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = 12; g.lineWidth = w * (1 + 0.15 * pulse); g.globalAlpha = 0.55 + 0.45 * f;
					g.beginPath(); var st = at(tl); g.moveTo(st[0], st[1]); for (int i = 0; i < P.Count; i++) if (Lc[i] > tl && Lc[i] < h) g.lineTo(P[i][0], P[i][1]); var e = at(h); g.lineTo(e[0], e[1]); g.stroke(); g.restore();
				}
				if (t < ta + dur && nh == 0) { var q = at(h); double x = q[0], y = q[1], a = q[2]; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; L9plane(x, y, a, 1.15 + 0.1 * pulse, PINK); g.restore(); L9plane(x, y, a, 0.5, "rgba(255,255,255,0.8)"); }
			}
			else { double x = P[0][0], y = P[0][1]; g.strokeStyle = $"rgba(255,42,109,{str(0.5 * clamp((t - tw) / (ta - tw), 0, 1))})"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, 16, 0, TAU); g.stroke(); }
		};
		hz.hit = (p, t) =>
		{
			if (t < ta) return false; double h = sH(t), tl = sT(t); if (h <= tl) return false; double rr = w / 2 + p.r - 2;
			if (t < ta + dur && nh == 0) { var q = at(h); if (hypot(p.x - q[0], p.y - q[1]) < 18 + p.r) return true; }
			for (int i = 1; i < P.Count; i++)
			{
				double s0 = Math.Max(Lc[i - 1], tl), s1 = Math.Min(Lc[i], h); if (s1 <= s0) continue; var a = at(s0); var b = at(s1); double dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy; if (l2 == 0 || double.IsNaN(l2)) l2 = 1; double u = clamp(((p.x - a[0]) * dx + (p.y - a[1]) * dy) / l2, 0, 1);
				if (hypot(p.x - a[0] - dx * u, p.y - a[1] - dy * u) < rr) return true;
			}
			return false;
		};
		return hz;
	}
	// a place pin: drops on the city and stays a little (hurts)
	static Hz L9Pin(double x, double y, double tw, double ta, double te, double R = 34)
	{
		bool l = false; var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; if (!l && t >= ta) { l = true; burst(x, y, PINK, 8, 200); } };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(x, y, R, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				double f = clamp(k * 1.4 - 0.4, 0, 1); if (f > 0) { g.globalAlpha = f; L9pinShape(x, y - R - 160 * (1 - f), R * 0.8); g.globalAlpha = 1; }
				return;
			}
			double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.fillStyle = "rgba(255,42,109,0.48)"; circle(x, y, R * (1 + 0.1 * pulse)); L9pinShape(x, y - R * 0.9, R * 0.8); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= ta && t < te && hypot(p.x - x, p.y - y) < R * 0.9 + p.r - 2;
		return h;
	}
	static void L9pinShape(double x, double y, double s) { g.save(); g.translate(x, y); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.arc(0, 0, s * 0.6, Math.PI * 0.85, Math.PI * 2.15); g.lineTo(0, s * 1.1); g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "#000"; circle(0, 0, s * 0.25); g.restore(); }

	// a no-fly zone (circle): dashed warning, then it fills striped pink and hurts until te
	static Hz L9NoFly(double x, double y, double r, double tw, double ta, double te)
	{
		bool l = false; var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; if (!l && t >= ta) { l = true; burst(x, y, PINK, 10, 240); } };
		h.draw = t =>
		{
			g.save();
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; circle(x, y, r); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.lineDashOffset = -t * 30; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				g.lineWidth = 3; g.beginPath(); g.arc(x, y, r * 0.28, 0, TAU); g.moveTo(x - r * 0.2, y + r * 0.2); g.lineTo(x + r * 0.2, y - r * 0.2); g.stroke(); g.restore(); return;
			}
			double a = t < te ? Math.Min(1, (t - ta) / 0.08) : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.fillStyle = "rgba(255,42,109,0.48)"; circle(x, y, r);
			g.beginPath(); g.arc(x, y, r, 0, TAU); g.clip(); g.strokeStyle = "rgba(255,42,109,0.55)"; g.lineWidth = 5; g.beginPath(); double o = (t * 40) % 26; for (double d = -r * 2; d < r * 2; d += 26) { g.moveTo(x + d + o - r, y + r); g.lineTo(x + d + o + r, y - r); }
			g.stroke();
			g.restore(); g.save(); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineWidth = 5 * (1 + 0.2 * pulse); g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < te && hypot(p.x - x, p.y - y) < r - 4 + p.r - 2;
		return h;
	}
	// the control-tower radar: a beam (sector) sweeps from angle a0 to a1, before that its range shows faintly
	static Hz L9Radar(double cx, double cy, double R, double a0, double a1, double wid, double tw, double ta, double te)
	{
		double sg = sign(a1 - a0); if (sg == 0 || double.IsNaN(sg)) sg = 1; Func<double, double> cur = t => a0 + (a1 - a0) * clamp((t - ta) / (te - ta), 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			g.save();
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.07 * k)})"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, R, Math.Min(a0, a1) - wid * (sg > 0 ? 1 : 0), Math.Max(a0, a1) + wid * (sg < 0 ? 1 : 0)); g.closePath(); g.fill();
				g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + JS.cos(a0) * R, cy + JS.sin(a0) * R); g.moveTo(cx, cy); g.lineTo(cx + JS.cos(a0 - sg * wid) * R, cy + JS.sin(a0 - sg * wid) * R); g.stroke(); g.setLineDash(new double[0]);
				for (int i = 1; i <= 3; i++) { g.globalAlpha = 0.25 + 0.35 * k; g.beginPath(); g.arc(cx, cy, R * i / 3.2 * k, 0, TAU); g.stroke(); }
				g.restore(); return;
			}
			double c = cur(t), a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3), b0 = Math.Min(c, c - sg * wid), b1 = Math.Max(c, c - sg * wid); g.globalAlpha = a; g.fillStyle = "rgba(255,42,109,0.48)"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, R, b0, b1); g.closePath(); g.fill();
			g.shadowColor = PINK; g.shadowBlur = 20; g.strokeStyle = PINK; g.lineWidth = 6 * (1 + 0.2 * pulse); g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + JS.cos(c) * R, cy + JS.sin(c) * R); g.stroke(); g.shadowBlur = 0; g.fillStyle = PINK; circle(cx, cy, 14); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double d = hypot(p.x - cx, p.y - cy); if (d > R + p.r - 2) return false; if (d < 16 + p.r) return true; double c = cur(t); double da = (JS.atan2(p.y - cy, p.x - cx) - c) * -sg; da = ((da % TAU) + TAU) % TAU; return da < wid + (p.r - 2) / d;
		};
		return h;
	}
	// the night: half the map is dark (pink) from side `side`, its edge pulls back from x1 to x2 as the sun rises
	static Hz L9Night(double side, double x1, double x2, double tw, double ta, double te)
	{
		Func<double, double> bx = t => x1 + (x2 - x1) * clamp((t - ta) / (te - ta), 0, 1); Func<double, double, bool> inn = (x, b) => side < 0 ? x < b : x > b;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double b = bx(t); bool pre = t < ta; double k = pre ? clamp((t - tw) / (ta - tw), 0, 1) : 1, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3), x0 = side < 0 ? 0 : b, ww = side < 0 ? b : W - b; g.save(); g.globalAlpha = a;
			g.fillStyle = pre ? $"rgba(255,42,109,{str(0.04 + 0.08 * k)})" : "rgba(255,42,109,0.48)"; Func<double, double> wv = y => b + JS.sin(y / 70 + t * 2) * 14; g.beginPath(); g.moveTo(side < 0 ? 0 : W, 0); for (double y = 0; y <= H; y += 20) g.lineTo(wv(y), y); g.lineTo(side < 0 ? 0 : W, H); g.closePath(); g.fill();
			if (!pre) { g.fillStyle = "rgba(255,255,255,0.5)"; for (int i = 0; i < 40; i++) { double sx = (i * 197) % W, sy = (i * 331) % H; if (inn(sx, wv(sy) - side * 20) && (i + Math.Floor(t * 3)) % 5 != 0) g.fillRect(sx - 1.5, sy - 1.5, 3, 3); } }
			g.strokeStyle = pre ? $"rgba(255,42,109,{str(0.3 + 0.5 * k)})" : PINK; g.lineWidth = pre ? 3 : 6; if (pre) g.setLineDash(new double[] { 12, 10 }); else { g.shadowColor = PINK; g.shadowBlur = 20; }
			g.beginPath(); for (double y = 0; y <= H; y += 20) { if (y != 0) g.lineTo(wv(y), y); else g.moveTo(wv(y), y); }
			g.stroke(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; return inn(p.x + side * (p.r - 4), bx(t) + JS.sin(p.y / 70 + t * 2) * 14); };
		return h;
	}

	// a watcher (invisible, harmless): fires an attack at the player at the beats Wb, so there is no spot to stand still in
	static Hz L9Pulse(List<double> Wb, Action<int> mk, double t1)
	{
		int i = -1; var h = new Hz();
		h.update = (t, dt) =>
		{
			if (i < 0) { i = 0; while (i < Wb.Count && B(Wb[i]) < t - 0.05) i++; }
			while (i < Wb.Count && t >= B(Wb[i])) { mk(i); i++; }
			if (t > t1) h.dead = true;
		};
		return h;
	}
	/* ---- phase 2: the storm ---- */
	// grey clouds drifting (decoration)
	static Hz L9Clouds(double t0, double t1)
	{
		var C = new List<double[]>(); for (int i = 0; i < 8; i++) C.Add(new double[] { i * (W + 400) / 8, 70 + ((i * 3) % 8) * 80, 46 + (i * 37) % 30, 26 + (i * 29) % 18 });
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Max(0, Math.Min(Math.Min(1, (t - t0) / 1.5), (t1 - t) / 1.0)); if (a <= 0) return; g.fillStyle = GREY + toFixed(0.06 * a, 3) + ")";
			foreach (var q in C)
			{
				double x0 = q[0], y = q[1], r = q[2], v = q[3]; double x = ((x0 - (t - t0) * v) % (W + 400) + W + 400) % (W + 400) - 200; g.beginPath(); g.ellipse(x, y, r * 2.6, r * 0.7, 0, 0, TAU); g.moveTo(x + r * 1.3, y - r * 0.3); g.arc(x + r * 0.5, y - r * 0.3, r * 0.8, 0, TAU); g.moveTo(x - r * 0.3, y - r * 0.2); g.arc(x - r * 0.9, y - r * 0.2, r * 0.6, 0, TAU); g.fill();
			}
		};
		return h;
	}
	// the storm cloud (grey, harmless): drifts along the top, xf(t) is its place
	static Hz L9StormCloud(Func<double, double> xf, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Max(0, Math.Min(Math.Min(1, (t - t0) / 0.6), (t1 - t) / 0.6)); if (a <= 0) return; double x = xf(t), y = 30, p = pulse; g.fillStyle = GREY + toFixed(0.16 * a, 3) + ")"; g.beginPath(); g.ellipse(x, y + 18, 150, 40, 0, 0, TAU); g.moveTo(x + 30 + 70, y); g.arc(x + 30, y, 70, 0, TAU); g.moveTo(x - 50 + 55, y + 8); g.arc(x - 50, y + 8, 55, 0, TAU); g.moveTo(x + 100 + 45, y + 14); g.arc(x + 100, y + 14, 45, 0, TAU); g.fill();
			g.fillStyle = $"rgba(255,42,109,{str((0.12 + 0.25 * p) * a)})"; g.beginPath(); g.ellipse(x, y + 50, 120, 10, 0, 0, TAU); g.fill();
		};
		return h;
	}
	// lightning: a faint dashed warning, then a zigzag bolt from top to bottom (pts are its points)
	static Hz L9Bolt(List<double[]> pts, double tw, double ta, double te, double w = 22)
	{
		bool s = false; var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; if (!s && t >= ta) { s = true; flash = Math.Max(flash, 0.12); shake = Math.Max(shake, 5); var q = pts[pts.Count - 1]; burst(q[0], Math.Min(q[1], H - 6), "#fff", 8, 260); } };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), fl = 0.6 + 0.4 * JS.sin(t * 40); g.strokeStyle = $"rgba(255,42,109,{str((0.12 + 0.4 * k) * fl)})"; g.lineWidth = 2 + w * 0.25 * k; g.setLineDash(new double[] { 10, 8 });
				g.beginPath(); for (int i = 0; i < pts.Count; i++) { var q = pts[i]; if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); }
				g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = $"rgba(215,215,230,{str(0.06 + 0.12 * k)})"; circle(pts[0][0], 12, 50 + 20 * k); return;
			}
			double e = t - ta, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.save(); g.globalAlpha = a; g.lineJoin = "round"; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = 24; g.strokeStyle = PINK; g.lineWidth = w * (1 + 0.2 * pulse);
			g.beginPath(); for (int i = 0; i < pts.Count; i++) { var q = pts[i]; if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); }
			g.stroke(); g.shadowBlur = 0; g.strokeStyle = e < 0.08 ? "#fff" : "rgba(255,255,255,0.7)"; g.lineWidth = w * 0.3; g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double rr = w / 2 + p.r - 2;
			for (int i = 1; i < pts.Count; i++) { var a = pts[i - 1]; var b = pts[i]; double dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy; if (l2 == 0 || double.IsNaN(l2)) l2 = 1; double u = clamp(((p.x - a[0]) * dx + (p.y - a[1]) * dy) / l2, 0, 1); if (hypot(p.x - a[0] - dx * u, p.y - a[1] - dy * u) < rr) return true; }
			return false;
		};
		return h;
	}
	// a plane formation (V shape): comes in from off screen and passes, every plane has a short harmful trail. Before it, their paths show as dots
	static Hz L9Formation(double x0, double y0, double ang, int n, double gap, double sp, double tw, double ta, double trl = 150, double s = 1.0)
	{
		double dx = JS.cos(ang), dy = JS.sin(ang), nx = -dy, ny = dx; var F = new List<double[]>();
		for (int i = 0; i < n; i++) { double k = Math.Ceiling(i / 2.0), sd = i % 2 != 0 ? 1 : -1; F.Add(new[] { -k * gap * 0.8, i != 0 ? sd * k * gap : 0 }); }
		Func<double[], double, double[]> pos = (f, t) => { double d = (t - ta) * sp + f[0]; return new[] { x0 + dx * d + nx * f[1], y0 + dy * d + ny * f[1], d }; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > ta && (t - ta) * sp - gap * n - trl > 2200) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta + 1.2)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str((0.12 + 0.35 * k) * (t < ta ? 1 : Math.Max(0, 1 - (t - ta) / 1.2)))})";
				foreach (var f in F) { for (int d = 0; d < 2000; d += 34) { double x = x0 + dx * d + nx * f[1], y = y0 + dy * d + ny * f[1]; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) { if (d > 200) break; continue; } g.fillRect(x - 2, y - 2, 4, 4); } }
			}
			if (t < ta) return; g.save(); g.strokeStyle = PINK; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = 10; g.lineWidth = 8;
			foreach (var f in F) { var q = pos(f, t); double x = q[0], y = q[1], d = q[2], l = Math.Min(trl, Math.Max(0, d)); if (l > 0) { g.beginPath(); g.moveTo(x, y); g.lineTo(x - dx * l, y - dy * l); g.stroke(); } }
			g.shadowBlur = 16; foreach (var f in F) { var q = pos(f, t); L9plane(q[0], q[1], ang, s * (1 + 0.1 * pulse), PINK); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			foreach (var f in F)
			{
				var q = pos(f, t); double x = q[0], y = q[1], d = q[2]; if (hypot(p.x - x, p.y - y) < 17 * s + p.r - 2) return true; double l = Math.Min(trl, Math.Max(0, d)); if (l <= 0) continue;
				double u = clamp(((p.x - x) * -dx + (p.y - y) * -dy) / l, 0, 1); if (hypot(p.x - x + dx * l * u, p.y - y + dy * l * u) < 4 + p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// a rain cell: a grey cloud stops at the top and a column of (pink) rain falls from it, harmful from ta to te
	static Hz L9Rain(double x, double w, double tw, double ta, double te)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			bool pre = t < ta; double k = pre ? clamp((t - tw) / (ta - tw), 0, 1) : 1, a = Math.Min(clamp((t - tw) / 0.3, 0, 1), t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3)); g.save(); g.globalAlpha = a;
			g.fillStyle = GREY + "0.18)"; g.beginPath(); g.ellipse(x, 52, w * 0.75, 30, 0, 0, TAU); g.moveTo(x + w * 0.25 + 44, 30); g.arc(x + w * 0.25, 30, 44, 0, TAU); g.moveTo(x - w * 0.3 + 36, 36); g.arc(x - w * 0.3, 36, 36, 0, TAU); g.fill();
			if (pre)
			{
				g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.08 * k)})"; g.fillRect(x - w / 2, 80, w, H - 80); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(x - w / 2, 80); g.lineTo(x - w / 2, H); g.moveTo(x + w / 2, 80); g.lineTo(x + w / 2, H); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; for (int i = 0; i < 6; i++) { double yy = 80 + ((i * 97 + t * 160) % (140 + k * (H - 220))); g.fillRect(x - w / 2 + 8 + (i * 53) % (w - 16), yy, 3, 12); }
				g.restore(); return;
			}
			g.fillStyle = "rgba(255,42,109,0.48)"; g.fillRect(x - w / 2, 80, w, H - 80); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; for (int i = 0; i < 22; i++) { double xx = x - w / 2 + 6 + (i * 37) % (w - 12), yy = 80 + ((i * 131 + t * 900) % (H - 80)); g.fillRect(xx, yy, 4, 26); }
			g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath(); g.moveTo(x - w / 2, 80); g.lineTo(x - w / 2, H); g.moveTo(x + w / 2, 80); g.lineTo(x + w / 2, H); g.stroke(); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < te && p.y > 70 && Math.Abs(p.x - x) < w / 2 - 4 + p.r - 2;
		return h;
	}
	// the heart fills: after the two planes draw it, its inside turns pink and hurts a little (P are its edge points)
	static Hz L9HeartFill(List<double[]> P, double tw, double ta, double te)
	{
		Func<double, double, bool> inside = (x, y) => { bool c = false; for (int i = 0, j = P.Count - 1; i < P.Count; j = i++) { double xi = P[i][0], yi = P[i][1], xj = P[j][0], yj = P[j][1]; if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c; } return c; };
		bool fl = false, fp = false; var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > te + 0.3) h.dead = true; if (!fl && t >= ta) { fl = true; }
			if (!fp && t >= te) { fp = true; double mx = 0, my = 0; foreach (var q in P) { mx += q[0]; my += q[1]; } burst(mx / P.Count, my / P.Count, PINK, 30, 420); }
		};
		h.draw = t =>
		{
			bool pre = t < ta; double k = pre ? clamp((t - tw) / (ta - tw), 0, 1) : 1, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.save(); g.globalAlpha = a; g.fillStyle = pre ? $"rgba(255,42,109,{str(0.03 + 0.1 * k)})" : $"rgba(255,42,109,{str(0.48 + 0.06 * pulse)})";
			g.beginPath(); for (int i = 0; i < P.Count; i++) { var q = P[i]; if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); }
			g.closePath(); g.fill(); if (!pre) { g.shadowColor = PINK; g.shadowBlur = 22; g.strokeStyle = PINK; g.lineWidth = 5; g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) => t >= ta && t < te && inside(p.x, p.y);
		return h;
	}

	/* ---- phase 3: the big plane (the boss) ---- */
	// phase 3 background: the runway lights (grey)
	static Hz L9Runway(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Max(0, Math.Min(Math.Min(1, (t - t0) / 1.5), (t1 - t) / 1.0)); if (a <= 0) return; double bt = BT(t), sc = ((bt % 2) + 2) % 2 / 2;
			g.strokeStyle = GREY + toFixed(0.07 * a, 3) + ")"; g.lineWidth = 3; g.setLineDash(new double[] { 40, 40 }); g.lineDashOffset = -sc * 80; g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke(); g.setLineDash(new double[0]); g.lineDashOffset = 0;
			for (int i = 0; i < 12; i++) { double y = ((i * 64 + sc * 64 * 2) % (H + 64)) - 32; bool on = (i + Math.Floor(bt * 2)) % 6 == 0; g.fillStyle = GREY + toFixed((on ? 0.4 : 0.12) * a, 3) + ")"; foreach (var x in new double[] { W / 2 - 330, W / 2 + 330 }) { g.fillRect(x - 4, y - 4, 8, 8); } }
		};
		return h;
	}
	// the big plane: pos(t) returns [x, y, nose angle]. Three engines: left wing, tail, right wing
	static Hz L9Jet(Func<double, double[]> pos, double t0, double tc, double tx)
	{
		bool ex = false; var h = new Hz();
		h.update = (t, dt) => { if (t >= tx && !ex) { ex = true; var P = pos(t); burst(P[0], P[1], PINK, 70, 520); burst(P[0], P[1], "#fff", 40, 420); shake = 26; flash = 0.7; rumble(500, 1, 1); h.dead = true; } };
		h.draw = t =>
		{
			if (t < t0) return; var q = pos(t); double x = q[0], y = q[1], a = q[2]; if (y < -300 || y > H + 300 || x < -400 || x > W + 400) return; double s = 1;
			if (t >= tc) { double k = clamp((t - tc) / (tx - tc), 0, 1); s = 1 + 0.15 * k; x += (random() * 2 - 1) * 6 * k; y += (random() * 2 - 1) * 6 * k; }
			g.save(); g.translate(x, y); g.rotate(a); g.scale(s, s); g.shadowColor = PINK; g.shadowBlur = 30; g.fillStyle = t >= tc && Math.Floor(t * 12) % 2 != 0 ? PINKW : PINK;
			g.beginPath(); g.moveTo(140, 0); g.quadraticCurveTo(132, -22, 100, -24); g.lineTo(20, -24); g.lineTo(-40, -230); g.lineTo(-74, -230); g.lineTo(-40, -24); g.lineTo(-96, -24); g.lineTo(-124, -80); g.lineTo(-144, -80); g.lineTo(-132, 0);
			g.lineTo(-144, 80); g.lineTo(-124, 80); g.lineTo(-96, 24); g.lineTo(-40, 24); g.lineTo(-74, 230); g.lineTo(-40, 230); g.lineTo(20, 24); g.lineTo(100, 24); g.quadraticCurveTo(132, 22, 140, 0); g.closePath(); g.fill();
			g.shadowBlur = 0; g.fillStyle = "#000"; for (int i = 0; i < 7; i++) circle(92 - i * 20, 0, 4); circle(118, -8, 3); circle(118, 8, 3);
			double bt = BT(t); bool bl = (Math.Floor(bt * 2) % 2) == 0; g.fillStyle = bl ? "#fff" : "rgba(255,255,255,0.3)"; circle(-60, -228, 7); circle(-60, 228, 7);
			g.fillStyle = "rgba(255,255,255,0.75)"; foreach (var e in new[] { new double[] { -6, -120 }, new double[] { -6, 120 }, new double[] { -118, 0 } }) { g.fillRect(e[0] - 14, e[1] - 11, 28, 22); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; var q = pos(t); double x = q[0], y = q[1], a = q[2];
			return obbHit(p.x, p.y, p.r - 2, x + JS.cos(a) * 4, y + JS.sin(a) * 4, 136, 22, a) || obbHit(p.x, p.y, p.r - 2, x - JS.cos(a) * 36, y - JS.sin(a) * 36, 24, 210, a) || obbHit(p.x, p.y, p.r - 2, x - JS.cos(a) * 126, y - JS.sin(a) * 126, 12, 70, a);
		};
		return h;
	}
	// the plane's lane warning: a see-through band as wide as its wings before it passes
	static Hz L9Lane(double y, double hh, double tw, double ta, double te)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t => { if (t > ta) return; double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(0, y - hh, W, 2 * hh); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.45 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 14, 10 }); g.beginPath(); g.moveTo(0, y - hh); g.lineTo(W, y - hh); g.moveTo(0, y + hh); g.lineTo(W, y + hh); g.stroke(); g.setLineDash(new double[0]); };
		return h;
	}
	// the place of engine k (0 left, 1 tail, 2 right) and the angle of its blast (opposite the nose)
	static readonly double[][] L9E = { new double[] { -20, -120 }, new double[] { -140, 0 }, new double[] { -20, 120 } };
	static double[] L9eng(Func<double, double[]> pos, double t, int k) { var q = pos(t); double x = q[0], y = q[1], a = q[2]; var E = L9E[k]; double c = JS.cos(a), s = JS.sin(a); return new[] { x + E[0] * c - E[1] * s, y + E[0] * s + E[1] * c, a + Math.PI }; }
	// an engine blast: a long column out the back of the engine, moving with the plane
	static Hz L9Blast(Func<double, double[]> pos, int k, double tw, double ta, double te, double w = 46)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			var q = L9eng(pos, t, k); double x = q[0], y = q[1], a = q[2]; g.save(); g.translate(x, y); g.rotate(a);
			if (t < ta) { double qq = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.16 * qq)})"; g.fillRect(0, -w / 2, 1700, w); g.fillStyle = $"rgba(255,42,109,{str(0.4 + 0.4 * qq)})"; g.fillRect(0, -1.5, 1700, 3); circle(0, 0, 8 + 10 * qq); }
			else
			{
				double e = t - ta, k2 = t < te ? Math.Min(1, e / 0.06) : Math.Max(0, 1 - (t - te) / 0.3), ww = w * k2 * (1 + 0.12 * pulse); g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; g.fillRect(0, -ww / 2, 1700, ww); g.shadowBlur = 0;
				g.fillStyle = e < 0.1 ? "#fff" : "rgba(255,255,255,0.45)"; g.fillRect(0, -ww * 0.15, 1700, ww * 0.3);
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; var q = L9eng(pos, t, k); double x = q[0], y = q[1], a = q[2]; return obbHit(p.x, p.y, p.r - 2, x + JS.cos(a) * 850, y + JS.sin(a) * 850, 850, w / 2, a); };
		return h;
	}
	// the plane's sound wave: a Λ shape moving down, with a gap at its tip to pass through
	static Hz L9Boom(double x0, double y0, double tw, double t0, double sp, double gap = 150, double al = 1.15, double th = 16)
	{
		double ta = JS.tan(al), sa = JS.sin(al); Func<double, double> yv = t => y0 + (t - t0) * sp;
		var h = new Hz();
		h.update = (t, dt) => { if (t > t0 && yv(t) - W / ta > H + 60) h.dead = true; };
		h.draw = t =>
		{
			bool live = t >= t0; double y = live ? yv(t) : y0, k = live ? 1 : clamp((t - tw) / (t0 - tw), 0, 1); g.save();
			if (live) { g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineWidth = th * (1 + 0.15 * pulse); } else { g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); }
			foreach (var sd in new double[] { -1, 1 }) { double d0 = gap / 2, d1 = 1500; g.beginPath(); g.moveTo(x0 + sd * d0, y - d0 / ta); g.lineTo(x0 + sd * d1, y - d1 / ta); g.stroke(); }
			g.setLineDash(new double[0]); if (!live) { g.fillStyle = $"rgba(255,42,109,{str(0.3 * k)})"; for (int i = 0; i < 3; i++) { double yy = y + 30 + i * 26 + ((t * 90) % 26); g.beginPath(); g.moveTo(x0 - 12, yy); g.lineTo(x0 + 12, yy); g.lineTo(x0, yy + 12); g.closePath(); g.fill(); } }
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; double dx = Math.Abs(p.x - x0); if (dx < gap / 2) return false; double d = (yv(t) - p.y - dx / ta) * sa; return Math.Abs(d) < th / 2 + p.r - 2; };
		return h;
	}

	// a difficulty tier of this level: {sp, wn, base} (ezT keeps base as it is)
	class L9Tier { public double sp, wn, @base; }

	/* ================= level 9: Right Now (Na Na Na), Akon =================
	   138 bpm, cut from 0:57.5. 0 intro | 32 verse 1 | 96 chorus | 160 verse 2 (checkpoint) | 224 chorus
	   288 bridge | 352 chorus (checkpoint) | 416 bridge 2 | 480 last chorus | 512 the end */
	public static List<Ev> buildLevel9()
	{
		bool A2 = afkV == 2;
		var E = new List<Ev>(); var R = mulberry32(909);
		void ev(double b, Action f) => E.Add(new Ev { b = b, f = f });
		var TB = new double[] { 300, 320, 330 };
		var TI = ezT(new[] { new Tier { sp = 300, wn = 1.6 }, new Tier { sp = 320, wn = 1.5 }, new Tier { sp = 330, wn = 1.5 } }).Select((t, i) => new L9Tier { sp = t.sp, wn = t.wn, @base = TB[i] }).ToArray();
		double kf(L9Tier T) => T.sp / T.@base;
		double PXp() => player != null ? player.x : W / 2; double PYp() => player != null ? player.y : H / 2;
		void fx(double b, double f2, double s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
		double wB(double hb, double w) => easy ? Math.Floor(hb - w) : hb - w;
		double[] cityP(object c) => c is string s ? L9city(s) : (double[])c;

		/* ---------- phase 1: the map, trips around the world ---------- */
		var T = TI[0];
		// a flight from city to city: hb take-off, fl flight length in beats, lin how long the trail stays in beats
		void fly(double hb, object A, object Bc, double h, double fl, double lin, L9Tier T, double w = 12) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L9Route(L9arc(cityP(A), cityP(Bc), h), B(wb), B(hb), fl * SPB / kf(T), lin * SPB, w))); }
		// a flight to you: lands where you were at the warning
		void flyP(double hb, string A, double h, double fl, double lin, L9Tier T, double w = 12) { double wb = wB(hb, T.wn); ev(wb, () => { var P = new[] { clamp(PXp(), 10, W - 10), clamp(PYp(), 10, H - 10) }; hazards.Add(L9Route(L9arc(L9city(A), P, h), B(wb), B(hb), fl * SPB / kf(T), lin * SPB, w)); }); }
		void pin(double hb, object c, L9Tier T, double R0 = 34, double dur = 1.2) { double wb = wB(hb, T.wn + 0.5); var q = cityP(c); double x = q[0], y = q[1]; ev(wb, () => hazards.Add(L9Pin(x, y, B(wb), B(hb), B(hb + dur), R0))); }
		E.Add(new Ev { b = 0, keep = true, f = () => hazards.Add(L9Map(B(0), B(160))) });
		void nofly(double hb, object c, double r, L9Tier T, double dur = 3) { double wb = wB(hb, T.wn + 0.5); var q = cityP(c); double x = q[0], y = q[1]; ev(wb, () => hazards.Add(L9NoFly(x, y, r, B(wb), B(hb), B(hb + dur)))); }
		void radar(double hb, object c, double R0, double a0, double a1, L9Tier T, double dur = 4, double wid = 0.42) { double wb = wB(hb, T.wn + 0.8); var q = cityP(c); double x = q[0], y = q[1]; ev(wb, () => hazards.Add(L9Radar(x, y, R0, a0, a1, wid, B(wb), B(hb), B(hb + dur)))); }
		void night(double hb, double side, double x1, double x2, L9Tier T, double dur) { double wb = wB(hb, T.wn + 1); ev(wb, () => hazards.Add(L9Night(side, x1, x2, B(wb), B(hb), B(hb + dur)))); }
		void rain(double hb, double x, double w, L9Tier T, double dur = 4) { double wb = wB(hb, T.wn + 1); ev(wb, () => hazards.Add(L9Rain(x, w, B(wb), B(hb), B(hb + dur)))); }
		const double PI = Math.PI;
		// a free path (points): a plane flies it and leaves a trail
		void flyPath(double hb, List<double[]> P, double fl, double lin, L9Tier T, double w = 10) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L9Route(P, B(wb), B(hb), fl * SPB / kf(T), lin * SPB, w))); }
		List<double[]> ringPts(double cx, double cy, double r, double a0, double turns, int n = 40, double? ry_ = null) { double ry = ry_ ?? r; var P = new List<double[]>(); for (int i = 0; i <= n; i++) { double a = a0 + TAU * turns * i / n; P.Add(new[] { cx + JS.cos(a) * r, cy + JS.sin(a) * ry }); } return P; }
		// 0-15 the intro: the departure hall, from four cities a plane leaves on every beat to different cities
		var DEP = new (string, string[])[] { ("NY", new[] { "PAR", "LON", "RIO", "MIA" }), ("DKR", new[] { "RIO", "PAR", "CAI", "CPT" }), ("DXB", new[] { "MUM", "MOS", "CAI", "SYD" }), ("TYO", new[] { "SEO", "SYD", "MOS", "LA" }) };
		for (int b = 1; b < 16; b++) { var (c, ds) = DEP[b % 4]; var d = ds[(b >> 2) % 4]; fly(b, c, d, new double[] { 70, -50, 110, -80 }[(b >> 1) % 4], 2.4, b < 8 ? 1.1 : 1.4, T, 10); }
		// the control towers: a radar sweeps half a circle
		radar(3, "LA", 460, -PI / 2, PI / 2, T, 4); radar(7, "TYO", 460, PI * 1.5, PI / 2, T, 4); radar(12, "RIO", 460, -PI * 0.05, -PI * 0.95, T, 4);
		// 16-31 the calm: flocks, 5 planes fly together in parallel arcs across the map, with a gap between them
		void flock(double hb, double dir, double y0, int gp, L9Tier T, int n = 5, double dy = 86, double fl = 3.2, double lin = 1.6)
		{
			for (int i = 0; i < n; i++) { if (i == gp) continue; double y = y0 + i * dy; var A = new[] { dir > 0 ? -30 : W + 30, y + (dir > 0 ? 40 : -40) }; var Bp = new[] { dir > 0 ? W + 30 : -30, y - (dir > 0 ? 40 : -40) }; fly(hb + i * 0.25, A, Bp, dir > 0 ? 120 : -120, fl, lin, T, 10); }
		}
		foreach (var (b, d, y, gp) in new (double, double, double, int)[] { (16, 1, 110, 2), (20, -1, 150, 4), (24, 1, 90, 0), (28, -1, 130, 3) })
		{
			flock(b, d, y, gp, T);
			// no-fly zones on rows far from the gap
			int n = 0; for (int i = 0; i < 5 && n < 2; i++) if (Math.Abs(i - gp) >= 2) { nofly(b + 1 + n, new[] { d > 0 ? W * (0.3 + 0.4 * n) : W * (0.7 - 0.4 * n), y + i * 86 }, 92, T, 2.5); n++; }
		}
		// 32-47 verse 1: a tour, the plane hops from city to city every beat, and every city it reaches gets a pin
		var TOURS = new[] { new[] { "LA", "NY", "LON", "PAR", "CAI", "DXB", "MUM", "TYO" }, new[] { "SYD", "TYO", "SEO", "MOS", "PAR", "DKR", "RIO", "LIMA" }, new[] { "CPT", "DKR", "MIA", "NY", "LON", "MOS", "DXB", "SYD" }, new[] { "LIMA", "RIO", "CPT", "CAI", "DXB", "MUM", "SEO", "LA" },
			new[] { "TYO", "MUM", "DXB", "CAI", "PAR", "LON", "NY", "LA" }, new[] { "RIO", "DKR", "PAR", "MOS", "SEO", "TYO", "SYD", "CPT" } };
		void tour(double hb, string[] list, L9Tier T, double step = 1, double lin = 3, double h = 70, double w = 11, int pins = 1)
		{
			for (int i = 0; i + 1 < list.Length; i++) { fly(hb + i * step, list[i], list[i + 1], h, step, lin, T, w); if (pins != 0 && i > 0 && i % 2 == 0) pin(hb + i * step, list[i], T, 24, 0.6); }
			pin(hb + (list.Length - 1) * step, list[list.Length - 1], T, 38, 1.2);
		}
		tour(32, TOURS[0], T, 1, 2, 80); tour(40, TOURS[1], T, 1, 2, -60); nofly(36, "DKR", 130, T, 3); nofly(44, "DXB", 130, T, 3); radar(33, "SYD", 520, -PI * 0.55, -PI * 1.05, T, 3); radar(41, "NY", 520, -PI * 0.4, PI * 0.4, T, 3);
		// 48-55 holding over the airport: planes circle the cities and leave a circle with a gap
		void hold(double hb, string c, L9Tier T, double r = 78, double turns = 1.6, double fl = 6, double lin = 2, double dir = 1) { var q = L9city(c); double x = q[0], y = q[1]; flyPath(hb, ringPts(x, y, r, -Math.PI / 2, dir * turns, 48, r * 0.8), fl, lin, T, 10); }
		hold(48, "NY", T); hold(48, "DXB", T, 78, 1.6, 6, 2.6, -1); hold(50, "PAR", T, 70); hold(50, "SYD", T, 70, 1.6, 6, 2.6, -1); hold(52, "RIO", T, 72); hold(52, "TYO", T, 72, 1.6, 6, 2.6, -1);
		foreach (var (b, c) in new (double, string)[] { (49, "NY"), (49, "DXB"), (51, "PAR"), (51, "SYD"), (53, "RIO"), (53, "TYO") }) nofly(b, c, 52, T, 4);
		// 56-63 they all land: from the screen edges onto one city together
		void conv(double hb, string c, L9Tier T, int n = 8, double a0 = 0, double fl = 1.8, double lin = 1)
		{
			var q = L9city(c); double x = q[0], y = q[1]; pin(hb + fl, c, T, 40, 0.7);
			for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; fly(hb, new[] { x + JS.cos(a) * 820, y + JS.sin(a) * 820 }, new[] { x, y }, 0, fl, lin, T, 9); }
		}
		conv(56, "DKR", T, 8, 0.2); conv(60, "MOS", T, 8, 0.6); radar(58, "CPT", 520, -PI, -PI * 0.1, T, 3.5); radar(62, "LA", 500, PI * 0.5, -PI * 0.4, T, 3.5);
		// 64-79 two tours at the same time, one the reverse of the other
		tour(64, TOURS[2], T, 1, 2, 80); tour(68, TOURS[3].Take(5).Reverse().ToArray(), T, 1, 2.2, -90, 10, 0); tour(72, TOURS[4], T, 1, 2, -60); tour(76, TOURS[5].Take(5).ToArray(), T, 1, 2.2, 90, 10, 0);
		radar(66, "CPT", 520, -PI * 0.1, -PI, T, 4); radar(74, "MOS", 460, PI * 0.1, PI * 0.9, T, 4);
		// 80-95 the morning comes: pins drop on the cities west to east (then back), and flights at you
		var byX = L9K.ToList(); sort(byX, (p, q) => L9C[p][0] - L9C[q][0]);
		for (int i = 0; i < byX.Count; i++) pin(80 + i * 0.5, byX[i], T, 34, 0.8);
		{ var rv = byX.ToList(); rv.Reverse(); for (int i = 0; i < rv.Count; i++) pin(88 + i * 0.5, rv[i], T, 34, 0.8); }
		foreach (var b in new[] { 81, 85, 89, 93 }) flyP(b, new[] { "LA", "TYO", "CPT", "MOS" }[(b - 81) / 4], -100, 1.6, 1.2, T, 11);
		// the night pulls back as the sun rises (from the west, then from the east)
		night(80, -1, W * 0.5, 0, T, 7.5); night(88, 1, W * 0.5, W, T, 7.5);
		// 96-159 chorus 1: "Right now" a star of planes from a city (or they all land on it), "na na na" three cities, each sends three planes
		var HUBS = new[] { "DKR", "NY", "TYO", "PAR", "DXB", "RIO", "LON", "SYD" };
		var DEST = new Dictionary<string, string[]> { ["DKR"] = new[] { "LON", "RIO", "DXB" }, ["NY"] = new[] { "LA", "PAR", "RIO" }, ["TYO"] = new[] { "MOS", "SYD", "DXB" }, ["PAR"] = new[] { "NY", "CAI", "MOS" }, ["DXB"] = new[] { "PAR", "MUM", "CPT" }, ["RIO"] = new[] { "MIA", "DKR", "CPT" }, ["LON"] = new[] { "NY", "MOS", "CAI" }, ["SYD"] = new[] { "TYO", "MUM", "CPT" } };
		void fan(double hb, string hub, L9Tier T, double fl = 1.3, double lin = 0.6, double w = 10, int n = 3) { var ds = DEST[hub].Take(n).ToArray(); for (int i = 0; i < ds.Length; i++) fly(hb, hub, ds[i], new double[] { 90, -40, 140 }[i], fl, lin, T, w); }
		void star(double hb, string hub, L9Tier T, int n = 8, double a0 = 0, double fl = 1.6, double lin = 0.8)
		{
			var q = L9city(hub); double cx = q[0], cy = q[1]; pin(hb, hub, T, 46, 0.8);
			for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; var P = new[] { cx + JS.cos(a) * 900, cy + JS.sin(a) * 900 }; fly(hb + 0.02, new[] { cx, cy }, P, 0, fl * 2.2, lin, T, 9); }
		}
		for (int k = 0; k < 8; k++)
		{
			double L = 96 + k * 8; string h1 = HUBS[k], h2 = HUBS[(k + 3) % 8], h3 = HUBS[(k + 5) % 8]; fx(L, 0.25, 9);
			if (k % 2 == 0) star(L, h1, T, 6, k * 0.2); else conv(L - 1.8, h1, T, 6, k * 0.3, 1.8, 0.8);
			nofly(L + 0.2, h1, 125, T, 3);
			fan(L + 0.5, h2, T); fan(L + 1.5, h3, T); fan(L + 2.5, HUBS[(k + 6) % 8], T);
			if (k % 4 == 0) star(L + 4, h2, T, 6, 0.3 + k * 0.2);
			else if (k % 4 == 1) { double x = L9city(h2)[0]; radar(L + 4, h2, 560, x < W / 2 ? -PI / 2 : PI * 1.5, x < W / 2 ? PI / 2 : PI / 2, T, 3.5); }
			else if (k % 4 == 2) hold(L + 4, h2, T, 84, 1.2, 3, 1.6, k % 2 != 0 ? 1 : -1);
			else { var hs = new[] { h1, h2, h3 }; for (int i = 0; i < hs.Length; i++) nofly(L + 4 + i * 0.5, hs[i], 100, T, 2.5); }
			fan(L + 4.5, h1, T, 1.3, 0.6, 10, 2); fan(L + 5.5, HUBS[(k + 1) % 8], T, 1.3, 0.6, 10, 2); fan(L + 6.5, h3, T, 1.3, 0.6, 10, 2);
		}
		// 156-159 the last line: every city flies at you
		{ var cs = new[] { "LA", "CPT", "SYD", "MOS" }; for (int i = 0; i < cs.Length; i++) flyP(156 + i * 0.5, cs[i], 80, 1.6, 1.2, T, 11); }
		// no spot to stand still in: a flight at your place every 8 beats (v37/v38 replays only)
		void afk(List<double> Hb, Action<double, double> mk, L9Tier T, double wn, double t1)
		{
			var Wb = Hb.Select(hb => wB(hb, wn)).ToList();
			E.Add(new Ev { b = Math.Max(0, Wb[0] - 0.5), keep = true, f = () => { if (A2) hazards.Add(L9Pulse(Wb, i => mk(Hb[i], Wb[i]), B(t1))); } });
		}
		{
			var T1 = T; var Hb = new List<double>(); for (double b = 3; b < 157; b += 6) Hb.Add(b); var CC = new[] { "TYO", "LA", "CPT", "MOS", "SYD", "RIO", "LON", "DXB" };
			afk(Hb, (hb, wb) => { int i = Hb.IndexOf(hb); var P = new[] { clamp(PXp(), 10, W - 10), clamp(PYp(), 10, H - 10) }; hazards.Add(L9Route(L9arc(L9city(CC[i % 8]), P, new double[] { 100, -90 }[i % 2]), B(wb), B(hb), 1.5 * SPB / kf(T1), 1.0 * SPB, 11)); }, T1, T1.wn, 158);
		}

		/* ---------- phase 2: the sky, an air show in the storm ---------- */
		T = TI[1];
		E.Add(new Ev { b = 158, keep = true, f = () => hazards.Add(L9Clouds(B(158), B(352))) });
		// a lightning shape: starts at the top at x and zigzags to the bottom
		List<double[]> boltPts(double x, double amp = 36, double y0 = -10) { var P = new List<double[]> { new[] { x, y0 } }; double cx = x; for (double y = y0 + 60; y < H + 60; y += 60) { cx = clamp(x + (R() * 2 - 1) * amp, 20, W - 20); P.Add(new[] { cx, y }); } return P; }
		void bolt(double hb, double x, L9Tier T, double w = 24, double dur = 0.5, double amp = 36, double y0 = -10) { double wb = wB(hb, T.wn); var pts = boltPts(x, amp, y0); ev(wb, () => hazards.Add(L9Bolt(pts, B(wb), B(hb), B(hb + dur), w))); }
		void front(double hb, double dir, int gp, L9Tier T, int n = 8, double stp = 0.5) { for (int i = 0; i < n; i++) { if (i == gp) continue; double x = 80 + (dir > 0 ? i : n - 1 - i) * (W - 160) / (n - 1); bolt(hb + i * stp, x, T, 20, 0.45, 26); } }
		void form(double hb, int sd, double y, int n, L9Tier T, double gap = 88, double trl = 80) { double wb = wB(hb, T.wn + 0.5), x0 = sd != 0 ? W + 60 : -60, a = sd != 0 ? Math.PI : 0; ev(wb, () => hazards.Add(L9Formation(x0, y, a, n, gap, T.sp * 1.1, B(wb), B(hb), trl))); }
		void formV(double hb, int sd, double x, int n, L9Tier T, double gap = 88, double trl = 80) { double wb = wB(hb, T.wn + 0.5), y0 = sd != 0 ? H + 60 : -60, a = sd != 0 ? -Math.PI / 2 : Math.PI / 2; ev(wb, () => hazards.Add(L9Formation(x, y0, a, n, gap, T.sp * 1.0, B(wb), B(hb), trl))); }
		fx(160, 0.35, 14);
		// 160-191 the storm cloud: a big cloud drifts along the top dropping lightning with the beats, and a lightning front
		void cloudRun(double b0, double b1, double x0, double x1, double every, L9Tier T, double off = 0)
		{
			double cx(double b) => x0 + (x1 - x0) * (b - b0) / (b1 - b0);
			ev(b0 - 1, () => hazards.Add(L9StormCloud(t => cx(BT(t)), B(b0 - 1), B(b1 + 1))));
			for (double b = b0 + 1 + off; b < b1; b += every) bolt(b, cx(b), T, 26, 0.45, 22, 70);
		}
		cloudRun(160, 176, -60, W + 60, 1, T); front(168, -1, 3, T, 8, 0.4);
		// rain cells: a cloud stops and rains
		rain(162, W * 0.8, 130, T, 4); rain(171, W * 0.18, 130, T, 4); rain(178, W * 0.5, 150, T, 4); rain(186, W * 0.5, 150, T, 3);
		cloudRun(176, 192, W + 60, -60, 2, T); cloudRun(176, 192, -60, W + 60, 2, T, 1);
		form(180, 0, 560, 5, T); form(186, 1, 180, 5, T);
		// 192-207 the air show "bomb burst": 6 planes come up together and split to every side with smoke behind them
		void burstShow(double hb, double x, double yA, L9Tier T, int n = 6, double lin = 1.4)
		{
			for (int i = 0; i < n; i++)
			{
				double a = -Math.PI / 2 + (i - (n - 1) / 2.0) * (Math.PI * 1.5 / (n - 1)); var P = new List<double[]> { new[] { x, H + 50 } };
				for (int k = 1; k <= 8; k++) P.Add(new[] { x, H + 50 - (H + 50 - yA) * k / 8 });
				for (int k = 1; k <= 10; k++) { double r = 90 * k; P.Add(new[] { x + JS.cos(a) * r, yA + JS.sin(a) * r * 0.9 + (k * k) * 3 }); }
				flyPath(hb - 1, P, 2.6, lin, T, 10);
			}
		}
		burstShow(194, W * 0.3, 300, T); burstShow(198, W * 0.7, 260, T); burstShow(202, W * 0.5, 320, T); burstShow(206, W * 0.4, 280, T);
		rain(193, W * 0.88, 130, T, 4); rain(197, W * 0.12, 130, T, 4); rain(201, W * 0.9, 120, T, 3); rain(205, W * 0.1, 120, T, 3);
		foreach (var b in new[] { 196, 200, 204 }) formV(b, 0, new double[] { 1100, 180, 900 }[(b - 196) / 4], 4, T);
		// 208-223 the air show: a full loop, 4 planes in a row make a big circle and leave
		void loopShow(double hb, double dir, double y, double cx, L9Tier T, int n = 4, double r = 170)
		{
			for (int i = 0; i < n; i++)
			{
				var P = new List<double[]> { new[] { dir > 0 ? -80 : W + 80, y + r } }; P.Add(new[] { cx, y + r }); foreach (var q in ringPts(cx, y, r, Math.PI / 2, -dir, 32).Skip(1)) P.Add(q); P.Add(new[] { dir > 0 ? W + 80 : -80, y + r });
				flyPath(hb + i * 0.5, P, 4, 1.6, T, 10);
			}
		}
		loopShow(208, 1, 330, W * 0.45, T); loopShow(216, -1, 300, W * 0.55, T);
		rain(209, W * 0.9, 140, T, 5); rain(217, W * 0.1, 140, T, 5);
		foreach (var b in new[] { 212, 220 }) form(b, b == 212 ? 1 : 0, b == 212 ? 130 : 620, 5, T);
		// 224-287 chorus 2: "Right now" a big bolt, "na na na" three planes write NA NA NA in smoke in the sky
		// the letters N and A, one plane per letter
		List<double[]> dens(double[][] P) { var D = new List<double[]>(); for (int i = 1; i < P.Length; i++) for (int k = 0; k < 6; k++) { double u = k / 6.0; D.Add(new[] { P[i - 1][0] + (P[i][0] - P[i - 1][0]) * u, P[i - 1][1] + (P[i][1] - P[i - 1][1]) * u }); } D.Add(P[P.Length - 1]); return D; }
		void write(double hb, double x, double y, double s, L9Tier T, double fl = 2.6, double lin = 3)
		{
			flyPath(hb, dens(new[] { new[] { x, y + s }, new[] { x, y }, new[] { x + s * 0.65, y + s }, new[] { x + s * 0.65, y } }), fl, lin, T, 11); double a = x + s * 0.95;
			flyPath(hb, dens(new[] { new[] { a, y + s }, new[] { a + s * 0.4, y }, new[] { a + s * 0.8, y + s }, new[] { a + s * 0.6, y + s * 0.55 }, new[] { a + s * 0.2, y + s * 0.55 } }), fl, lin, T, 11);
		}
		var ROWS = new double[] { 90, 430, 260, 500, 120, 380, 250, 470 };
		for (int k = 0; k < 8; k++)
		{
			double L = 224 + k * 8, cx = new[] { W / 2, W * 0.3, W * 0.7, W / 2, W * 0.25, W * 0.75, W / 2, W * 0.4 }[k]; fx(L, 0.3, 10); bolt(L, cx, T, 40, 0.6, 50);
			for (int i = 0; i < 3; i++) write(L + 0.5 + i, 90 + i * 400, ROWS[k], 170, T, 2.6, 2.2);
			for (int i = 0; i < 3; i++) write(L + 4.5 + i, 90 + (k % 2 != 0 ? 2 - i : i) * 400, ROWS[(k + 3) % 8], 150, T, 2.4, 1.6);
			rain(L + 0.5, new[] { W * 0.92, W * 0.08 }[k % 2], 120, T, 3); form(L + 6, k % 2, new double[] { 600, 110, 620, 130, 580, 100, 640, 120 }[k], 5, T);
		}
		// 288-351 the bridge (missing her): two planes draw a big heart in smoke, then an arrow (a plane) passes through it, and formations pass
		void heart(double hb, double cx, double cy, double s, L9Tier T)
		{
			var H1 = new List<double[]>(); var H2 = new List<double[]>();
			for (int i = 0; i <= 30; i++) { double u = Math.PI * i / 30, x = 16 * JS.pow(JS.sin(u), 3), y = -(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)); H1.Add(new[] { cx - x * s, cy + y * s }); H2.Add(new[] { cx + x * s, cy + y * s }); }
			H1.Reverse(); H2.Reverse(); flyPath(hb, H1, 3, 3.5, T, 11); flyPath(hb, H2, 3, 3.5, T, 11);
			{ var HP = H1.ToList(); HP.Reverse(); HP.AddRange(H2.Skip(1)); double wb = wB(hb + 3.5, T.wn + 1); ev(wb, () => hazards.Add(L9HeartFill(HP, B(wb), B(hb + 3.5), B(hb + 6.5)))); }
			fly(hb + 4, new[] { -80, cy + 120 }, new[] { W + 80, cy - 140 }, 60, 2.2, 1.4, T, 12);
		}
		heart(288, W * 0.5, 330, 13, T); heart(304, W * 0.3, 320, 11, T); heart(320, W * 0.7, 330, 11, T);
		for (int k = 0; k < 12; k++)
		{
			double b = 290 + k * 4; if ((b >= 296 && b < 300) || (b >= 312 && b < 316)) continue;
			if (k % 4 == 1) formV(b, (k >> 2) % 2, new double[] { 160, 1120, 260, 1020, 180, 1100 }[k % 6], 5, T); else if (k % 4 == 2) form(b, (k >> 2) % 2, new double[] { 620, 110, 600, 130, 640, 100 }[k % 6], 5, T);
		}
		// 336-351 the storm comes back: lightning fronts
		for (int k = 0; k < 4; k++) front(336 + k * 4, k % 2 != 0 ? 1 : -1, new[] { 3, 6, 1, 4 }[k], T, 8, 0.4);
		rain(296, W * 0.15, 140, T, 4); rain(296, W * 0.85, 140, T, 4); rain(312, W * 0.5, 150, T, 4); rain(328, W * 0.12, 140, T, 3); rain(328, W * 0.88, 140, T, 3); rain(337, W * 0.5, 140, T, 3); rain(345, W * 0.15, 140, T, 3);

		// no spot to stand still in: a bolt at your place every 8 beats (v37/v38 replays only)
		{
			var Hb = new List<double>(); for (double b = 161; b < 350; b += 6) Hb.Add(b); Hb.Add(351); var T2 = T;
			afk(Hb, (hb, wb) => { double x = clamp(PXp(), 10, W - 10), py = clamp(PYp(), 0, H); var P = boltPts(x, 30); int i = P.FindIndex(q => q[1] > py); if (i < 1) i = 1; P.Insert(i, new[] { x, py }); hazards.Add(L9Bolt(P, B(wb), B(hb), B(hb + 0.5), 24)); }, T2, T2.wn, 352);
		}

		/* ---------- phase 3: the big plane ---------- */
		T = TI[2]; double K3 = kf(T);
		// the plane's place: top middle, nose up (the blast comes down), moving between the stops ST every half line
		double UP = -Math.PI / 2, YB = 105;
		var ST = new[] { 0.5, 0.3, 0.7, 0.5, 0.25, 0.75, 0.4, 0.6 };
		double hx(double b) { double i = Math.Floor((b - 352) / 4), f = clamp((b - 352) - i * 4 - 3, 0, 1), e = f * f * (3 - 2 * f), a = ST[(int)(((i % 8) + 8) % 8)], c = ST[(int)((((i + 1) % 8) + 8) % 8)]; return W * (a + (c - a) * e); }
		var PASS = new double[] { 200, 520, 360, 600, 140, 450, 280, 560 };
		double[] jpos(double t)
		{
			double b = BT(t);
			if (b < 352) { double k = clamp((b - 348) / 4, 0, 1), e = 1 - JS.pow(1 - k, 3); return new[] { W / 2, -320 + (YB + 320) * e, UP }; }
			if (b < 414) return new[] { hx(b), YB, UP };
			if (b < 416) { double k = (b - 414) / 2; return new[] { hx(414), YB - (YB + 400) * k * k, UP }; }
			if (b < 478) { int i = (int)Math.Floor((b - 416) / 8); double u = (b - 416 - i * 8) / 4; if (u > 1) return new[] { W / 2, -600, UP }; double dr = i % 2 != 0 ? -1 : 1, x = dr > 0 ? -420 + (W + 840) * u : W + 420 - (W + 840) * u; return new[] { x, PASS[i], dr > 0 ? 0 : Math.PI }; }
			if (b < 480) { double k = (b - 478) / 2, e = 1 - JS.pow(1 - k, 3); return new[] { W / 2, -320 + (YB + 320) * e, UP }; }
			return new[] { hx(b - 128), YB, UP };
		}
		E.Add(new Ev { b = 348, keep = true, f = () => hazards.Add(L9Runway(B(348), B(516))) });
		ev(350, () => hazards.Add(L9Jet(jpos, B(348), B(508), B(512))));
		void blast(double hb, int k, L9Tier T, double dur = 0.6, double w = 46) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L9Blast(jpos, k, B(wb), B(hb), B(hb + dur), w))); }
		void boom(double hb, L9Tier T, double gap = 150) { double wb = wB(hb, T.wn + 0.5); ev(wb, () => { var q = jpos(B(hb)); double x = q[0], y = q[1]; hazards.Add(L9Boom(x, y + 130, B(wb), B(hb), T.sp * 0.9, gap)); }); }
		// 352-415 chorus 3: "Right now" a Λ sound wave comes down, "na na na" the engines blast left, tail, right
		fx(352, 0.45, 16);
		for (int k = 0; k < 8; k++)
		{
			double L = 352 + k * 8; var o = k % 2 != 0 ? new[] { 2, 0, 1 } : new[] { 0, 2, 1 }; boom(L, T, 230); for (int i = 0; i < 3; i++) blast(L + 0.5 + i, o[i], T, 0.5, 42);
			for (int i = 0; i < 3; i++) blast(L + 4.5 + i, o[new[] { 1, 0, 2 }[i]], T, 0.5, 42);
		}
		// 416-479 bridge 2: the plane passes from the side at different heights, and its blast (trail) stays behind it
		for (int i = 0; i < 8; i++)
		{
			double L = 416 + i * 8, dr = i % 2 != 0 ? -1 : 1, y = PASS[i];
			// the plane's trail: a horizontal line on its path (dots before) that stays two beats after it passes
			{ double wb = wB(L, T.wn + 1); ev(wb, () => { hazards.Add(L9Lane(y, 232, B(wb), B(L) + 0.35, B(L) + 0.4)); hazards.Add(L9Route(new List<double[]> { new[] { dr > 0 ? -560 : W + 560, y }, new[] { dr > 0 ? W + 280 : -280, y } }, B(wb), B(L), 4 * SPB, 3 * SPB, 50, 1)); }); }
			form(L + 4, dr > 0 ? 1 : 0, PASS[(i + 3) % 8], 5, T, 88, 80); formV(L + 6, 0, new double[] { 980, 300, 200, 640, 460, 820, 180, 1100 }[i], 5, T, 88, 80);
		}
		// 480-511 the last chorus: everything together
		fx(480, 0.5, 18);
		for (int k = 0; k < 4; k++)
		{
			double L = 480 + k * 8; var o = k % 2 != 0 ? new[] { 2, 0, 1 } : new[] { 0, 2, 1 }; boom(L, T, 170); for (int i = 0; i < 3; i++) blast(L + 0.5 + i, o[i], T, 0.5, 42);
			if (k % 2 != 0) boom(L + 4, T, 170);
			for (int i = 0; i < 3; i++) blast(L + 4.5 + i, o[new[] { 1, 0, 2 }[i]], T, 0.55);
			if (k < 3) form(L + 2, k % 2, new double[] { 560, 300, 620 }[k], 5, T, 88, 80);
		}
		// a guard plane comes down on your place every 8 beats (v37/v38 replays only)
		{
			var Hb = new List<double>(); for (double b = 352.5; b < 509; b += 6) Hb.Add(b);
			afk(Hb, (hb, wb) => { double x = clamp(PXp(), 10, W - 10); hazards.Add(L9Formation(x, -60, Math.PI / 2, 3, 88, easy ? 380 : 440, B(wb), B(hb), 80)); }, T, T.wn + 0.5, 510);
		}
		// 508: it charges and shakes, and at 512 it explodes (harmless) with the win
		fx(512, 0.7, 0);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
