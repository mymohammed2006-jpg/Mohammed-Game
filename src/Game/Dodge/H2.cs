using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// H2, Hardcore level 2, "Laughing Corpses" (Black Ops Zombies): a graveyard under siege, played in rounds. Every attack is a fixed
// pattern on the beat that asks for one tactic:
// part 1 the graveyard (the boards and the hands, the electric wires, the horde that follows you and the corpses it leaves, the hellhounds),
// part 2 the rounds (the tally marks, the coffins you have to remember, the dark with lightning on the beat),
// part 3 the last round (your haunted copy, the upgraded horde, all of it at once, the three nukes).
// Fixed attacks are pure functions of time. The horde, the hounds and the copy also follow the square (they are "rnd" in the data file).
// The data is data/dodge_H2.json (made by tools/hardcore2/gen.py).
public static partial class G
{
	static double H2k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string H2P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string H2G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double H2Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void H2Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}

	// ---------- the dark ----------
	// in the dark only lightning (on the beat) shows the attacks; between flashes they are faint shapes. H2Vis is a pure function of time
	static double H2dk0 = -1, H2dk1 = -1; static double[] H2fl = new double[0];
	static double H2va = 1;
	public static double H2Vis(double t)
	{
		if (H2dk0 < 0 || t < H2dk0 || t > H2dk1 + 0.7) return 1;
		double d = Math.Min(H2k(t, H2dk0, H2dk0 + 0.6), 1 - H2k(t, H2dk1, H2dk1 + 0.6)), f = 0;
		foreach (var q in H2fl) if (t >= q) f = Math.Max(f, JS.exp(-(t - q) * 5));
		return 1 - d * (1 - Math.Max(0.1, f));
	}
	static void H2Draw(Hz h, Action<double> body)
	{
		h.draw = t => { H2va = h.hurts ? 1 : H2Vis(t); if (H2va <= 0.01) return;   /* 10-25: what can hurt is never in the dark */ g.save(); g.globalAlpha = H2va; body(t); g.restore(); H2va = 1; };
	}

	// ---------- where the square is, as the run's recording sees it ----------
	// The attacks that follow the square read its position only from the recording's samples (RP_HZ a second, the same rounding as
	// recSample), so a replay, which only has those samples, gives them exactly the same moves. In a replay the samples come from the
	// replay itself; while playing they are made here, step by step, the same way recSample makes them.
	static readonly List<double> H2SX = new(), H2SY = new(); static double H2St0 = double.NaN, H2Sim0 = double.NaN, H2SLast = -1e9;
	static void H2SampUpd(double t)
	{
		if (state == "replay" || player == null) return;
		// a new run (another start, or time went back: a restart) starts the samples again
		if (sim0 != H2Sim0 || t < H2SLast - 1e-9) { H2Sim0 = sim0; H2St0 = sim0 + STEP; H2SX.Clear(); H2SY.Clear(); }
		H2SLast = t;
		while (H2St0 + (double)H2SX.Count / RP_HZ <= t && H2SX.Count < 60000) { H2SX.Add(round(clamp(player.x, 0, 1300) * 50) / 50); H2SY.Add(round(clamp(player.y, 0, 1300) * 50) / 50); }
	}
	// how many samples were made by the step at time t, and sample i (its time and place)
	static int H2SN(double t)
	{
		if (state == "replay" && rp.doc != null)
		{
			var s = rp.doc.segs[rp.seg]; int n = 0; while (n < s.n && s.t0 + (double)n / RP_HZ <= t) n++; return n;
		}
		int m = H2SX.Count; while (m > 0 && H2St0 + (double)(m - 1) / RP_HZ > t) m--; return m;
	}
	static (double t, double x, double y) H2S(int i)
	{
		if (state == "replay" && rp.doc != null) { var s = rp.doc.segs[rp.seg]; return (s.t0 + (double)i / RP_HZ, s.b[i * 3] / 50.0, s.b[i * 3 + 1] / 50.0); }
		return (H2St0 + (double)i / RP_HZ, H2SX[i], H2SY[i]);
	}
	// the latest sample (the square itself before the first one). pre: called when an event fires, before the square moves in this
	// step, so only the samples of the steps before count
	static (double x, double y) H2Now(bool pre = false)
	{
		double tq = pre ? sim0 + (simN - 1) * STEP : songT; if (!pre) H2SampUpd(songT);
		int n = H2SN(tq); if (n == 0) return player != null ? (player.x, player.y) : (W / 2, H / 2);
		var q = H2S(n - 1); return (q.x, q.y);
	}

	// the 16 boarded windows round the walls, clockwise from the top left
	static readonly (double x, double y)[] H2WIN = H2Wins();
	static (double x, double y)[] H2Wins()
	{
		var l = new List<(double x, double y)>();
		foreach (var x in new[] { 160.0, 400, 640, 880, 1120 }) l.Add((x, 0));
		foreach (var y in new[] { 180.0, 360, 540 }) l.Add((W, y));
		foreach (var x in new[] { 1120.0, 880, 640, 400, 160 }) l.Add((x, H));
		foreach (var y in new[] { 540.0, 360, 180 }) l.Add((0, y));
		l.Add((0, 0)); l.Add((W, 0)); l.Add((W, H)); l.Add((0, H));   // 16-19: the corners (cracks in the walls, no boards)
		return l.ToArray();
	}
	static void H2Planks(double x, double y, double k, double sh)
	{
		// a window's boards (k: how broken, sh: how much they shake)
		if ((x <= 0 || x >= W) && (y <= 0 || y >= H)) return;
		bool hor = y <= 0 || y >= H; g.save(); g.translate(x, y); if (!hor) g.rotate(Math.PI / 2);
		g.fillStyle = "rgba(10,6,8,0.9)"; g.fillRect(-58, -16, 116, 32);
		for (int i = 0; i < 3; i++)
		{
			double o = (i - 1) * 9 + JS.sin(i * 7.3 + sh * 41) * sh * 4, a = (i == 1 ? k * 0.5 : 0) + (i - 1) * 0.06;
			g.save(); g.translate(0, o); g.rotate(a); g.fillStyle = i == 1 && k > 0.6 ? "rgba(90,60,40,0.5)" : "rgba(98,70,52,0.85)"; g.fillRect(-54, -3.5, 108, 7); g.restore();
		}
		g.restore();
	}

	// ---------- the boards and the hands ----------
	// arm i comes out of window wi[i] at ta[i] (seconds) towards the middle, lf[i] of the way there, w thick; it stays hold s and goes back.
	// The boards shake and its path shows in grey wn s before
	public static Hz H2Arms(int[] wi, double[] ta, double[] lf, double wn, double hold, double w)
	{
		var h = new Hz { kind = "H2Arms" }; int n = wi.Length; double tEnd = ta.Max() + hold + 0.4;
		(double x0, double y0, double dx, double dy, double L) A(int i) { var p = H2WIN[wi[i] % H2WIN.Length]; double vx = W / 2 - p.x, vy = H / 2 - p.y, d = hypot(vx, vy); return (p.x, p.y, vx / d, vy / d, d * lf[i]); }
		double Ext(int i, double t) { double u = t - ta[i]; if (u < 0) return 0; if (u < 0.07) return u / 0.07; if (u < hold) return 1; if (u < hold + 0.16) return 1 - (u - hold) / 0.16; return 0; }
		h.update = (t, dt) => { if (t > tEnd) h.dead = true; };
		H2Draw(h, t =>
		{
			for (int i = 0; i < n; i++)
			{
				if (t < ta[i] - wn || t > ta[i] + hold + 0.2) continue; var a = A(i); double ang = JS.atan2(a.dy, a.dx);
				if (t < ta[i])
				{
					double k = H2k(t, ta[i] - wn, ta[i]); g.globalAlpha = H2va * (0.3 + 0.6 * k);
					double nx = -a.dy * w / 2, ny = a.dx * w / 2, ex = a.x0 + a.dx * a.L, ey = a.y0 + a.dy * a.L;
					H2Dash(a.x0 + nx, a.y0 + ny, ex + nx, ey + ny, H2G(0.75)); H2Dash(a.x0 - nx, a.y0 - ny, ex - nx, ey - ny, H2G(0.75));
					g.strokeStyle = H2G(0.75); g.lineWidth = 2; g.beginPath(); g.moveTo(ex + nx, ey + ny); g.lineTo(ex - nx, ey - ny); g.stroke();
					g.globalAlpha = H2va; H2Planks(a.x0, a.y0, k * 0.4, k);
					continue;
				}
				double e = Ext(i, t), l = a.L * e; if (l < 2) continue; bool wh = t - ta[i] < 0.05;
				g.globalAlpha = H2va; H2Planks(a.x0, a.y0, 1, 0);
				g.save(); g.translate(a.x0, a.y0); g.rotate(ang);
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = wh ? PINKW : PINK; g.fillRect(0, -w * 0.48, l - w * 0.3, w * 0.96); g.shadowBlur = 0;
				// the hand: a bony palm and four long claws
				g.beginPath(); g.ellipse(l - w * 0.35, 0, w * 0.42, w * 0.5, 0, 0, TAU); g.fill();
				for (int f = 0; f < 4; f++)
				{
					double fa = (f - 1.5) * 0.28, fl = w * (f == 0 || f == 3 ? 0.75 : 1.0) * (0.9 + 0.1 * JS.sin(t * 9 + f));
					g.save(); g.translate(l - w * 0.2, (f - 1.5) * w * 0.22); g.rotate(fa); g.beginPath(); g.moveTo(0, -w * 0.09); g.lineTo(fl * 0.6, -w * 0.07); g.lineTo(fl, w * 0.06); g.lineTo(fl * 0.6, w * 0.07); g.lineTo(0, w * 0.09); g.closePath(); g.fill(); g.restore();
				}
				// rot: dark bruises and the bones showing through
				g.fillStyle = "rgba(40,0,14,0.5)"; for (double s = w * 0.6; s < l - w * 1.0; s += w * 1.1) { g.beginPath(); g.ellipse(s, (JS.sin(s * 0.07) * 0.3) * w, w * 0.22, w * 0.14, 0.4, 0, TAU); g.fill(); }
				g.fillStyle = "rgba(255,225,232,0.35)"; g.fillRect(0, -w * 0.05, l - w * 0.6, w * 0.1);
				g.restore();
			}
		});
		h.hit = (p, t) =>
		{
			for (int i = 0; i < n; i++)
			{
				double e = Ext(i, t); if (e <= 0) continue; var a = A(i); double l = a.L * e;
				if (H2Seg(p.x, p.y, a.x0, a.y0, a.x0 + a.dx * l, a.y0 + a.dy * l) < w / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the electric wires ----------
	// a wire between two poles round (cx,cy), L from the middle to each pole. It turns: from beat sb[i] at sw[i] radians a beat.
	// It is live between on[2k] and on[2k+1] (beats); sparks show 0.75 beat before. The poles hurt from ta to te
	static double H2Ang(double a0, double[] sb, double[] sw, double bt)
	{
		double a = a0;
		for (int i = 0; i < sb.Length; i++) { if (bt <= sb[i]) break; double e = i + 1 < sb.Length ? Math.Min(bt, sb[i + 1]) : bt; a += sw[i] * (e - sb[i]); }
		return a;
	}
	public static Hz H2Wire(double cx, double cy, double L, double a0, double[] sb, double[] sw, double[] on, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H2Wire" }; const double PR = 18, WR = 14;
		bool live(double bt) { for (int i = 0; i + 1 < on.Length; i += 2) if (bt >= on[i] && bt < on[i + 1]) return true; return false; }
		double soon(double bt) { double s = 0; for (int i = 0; i + 1 < on.Length; i += 2) { double u = on[i] - bt; if (u > 0 && u < 0.75) s = Math.Max(s, 1 - u / 0.75); } return s; }
		(double x0, double y0, double x1, double y1) S(double bt) { double a = H2Ang(a0, sb, sw, bt), c = JS.cos(a) * L, s = JS.sin(a) * L; return (cx - c, cy - s, cx + c, cy + s); }
		h.o = new Dictionary<string, object> { ["seg"] = (Func<double, (bool on, double x0, double y0, double x1, double y1)>)(t => { double bt = BT(t); var s = S(bt); return (t >= ta && t < te && live(bt), s.x0, s.y0, s.x1, s.y1); }) };
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		H2Draw(h, t =>
		{
			if (t < tw) return; double bt = BT(t), a = Math.Min(H2k(t, tw, tw + 0.5), 1 - H2k(t, te, te + 0.4)); var s = S(bt); bool act = t >= ta && t < te;
			g.globalAlpha = H2va * a;
			bool lv = act && live(bt); double sn = act ? soon(bt) : 0;
			if (lv || sn > 0)
			{
				// the zigzag of the current
				double len = hypot(s.x1 - s.x0, s.y1 - s.y0), ux = (s.x1 - s.x0) / len, uy = (s.y1 - s.y0) / len; int m = 22;
				g.strokeStyle = lv ? PINK : (bt * 8 % 1 < 0.5 ? "#fff" : H2G(0.8)); g.lineWidth = lv ? 5 : 1.5; if (lv) { g.shadowColor = PINK; g.shadowBlur = 16; }
				if (lv) { g.save(); g.strokeStyle = H2P(0.5); g.lineWidth = WR * 2; g.beginPath(); g.moveTo(s.x0, s.y0); g.lineTo(s.x1, s.y1); g.stroke(); g.restore(); }
				g.beginPath();
				for (int i = 0; i <= m; i++) { double f = (double)i / m, o = (i == 0 || i == m) ? 0 : JS.sin(t * 47 + i * 2.3) * (lv ? 7 : 4 * sn); double x = s.x0 + ux * len * f - uy * o, y = s.y0 + uy * len * f + ux * o; if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
				g.stroke(); g.shadowBlur = 0;
				if (lv) { g.strokeStyle = "rgba(255,240,245,0.9)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(s.x0, s.y0); g.lineTo(s.x1, s.y1); g.stroke(); }
			}
			else { g.strokeStyle = H2G(0.3); g.lineWidth = 2; g.beginPath(); g.moveTo(s.x0, s.y0); g.lineTo(s.x1, s.y1); g.stroke(); }
			foreach (var (px, py) in new[] { (s.x0, s.y0), (s.x1, s.y1) })
			{
				g.fillStyle = act ? "#8f1a43" : "rgba(30,26,34,1)"; circle(px, py, PR + 3);
				g.strokeStyle = act ? PINK : H2G(0.7); g.lineWidth = 3; g.beginPath(); g.arc(px, py, PR, 0, TAU); g.stroke();
				g.fillStyle = act ? H2P(0.5 + 0.4 * pulse) : H2G(0.4); circle(px, py, 5);
			}
		});
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t); var s = S(bt);
			if (hypot(p.x - s.x0, p.y - s.y0) < PR + p.r - 2 || hypot(p.x - s.x1, p.y - s.y1) < PR + p.r - 2) return true;
			return live(bt) && H2Seg(p.x, p.y, s.x0, s.y0, s.x1, s.y1) < WR + p.r - 2;
		};
		return h;
	}

	// ---------- the horde and the corpses ----------
	// zombie i climbs out of its grave (sx[i],sy[i]) at beat sb[i] (the grave cracks wn beats before). On every beat a zombie lurches
	// towards where the square is at that beat, st px at most. Two zombies that bump into each other die and leave a corpse; a zombie
	// that walks into a corpse makes the pile bigger; a live wire kills them too. Corpses stay. With boom > 0 a corpse blows up boom beats
	// after it fell (radius bR); at beat to the zombies sink and every corpse left blows up
	public class H2Z { public double x, y, sx, sy, tx, ty; public int kb; public bool on, dead; public double dt0; }
	public class H2C { public double x, y, r, t0, tb; }
	const double H2ZR = 26;
	static double H2Lurch(double u) { u = clamp(u / 0.6, 0, 1); return 1 - (1 - u) * (1 - u); }
	public static Hz H2Horde(double[] sb, double[] sx, double[] sy, double st, double wn, double to, double boom, double bR)
	{
		var h = new Hz { kind = "H2Horde" }; int n = sb.Length; var Z = new H2Z[n]; var C = new List<H2C>(); double tTo = B(to), BH = 0.4 * SPB;
		for (int i = 0; i < n; i++) Z[i] = new H2Z { x = sx[i], y = sy[i], sx = sx[i], sy = sy[i], tx = sx[i], ty = sy[i], kb = (int)Math.Floor(sb[i]) };
		void Corpse(double x, double y, double r, double t)
		{
			foreach (var c in C) if (t < c.tb && hypot(c.x - x, c.y - y) < c.r * 0.8 + r * 0.5) { c.r = Math.Min(46, c.r + 6); return; }
			C.Add(new H2C { x = x, y = y, r = r, t0 = t, tb = boom > 0 ? Math.Min(t + boom * SPB, tTo) : tTo });
			burst(x, y, PINK, 8, 200);
		}
		h.update = (t, dt) =>
		{
			double bt = BT(t); if (bt > to + 2) { h.dead = true; return; }
			if (bt < to)
			{
				var pn = H2Now(); double px = pn.x, py = pn.y;
				for (int i = 0; i < n; i++)
				{
					var z = Z[i]; if (z.dead || bt < sb[i]) continue;
					if (!z.on) { z.on = true; burst(z.x, z.y, "rgba(150,120,110,1)", 6, 160); }
					int k = (int)Math.Floor(bt);
					if (k > z.kb)
					{
						z.kb = k; z.sx = z.x; z.sy = z.y; double vx = px - z.x, vy = py - z.y, d = hypot(vx, vy), s = Math.Min(st, d);
						if (d > 1e-6) { z.tx = z.x + vx / d * s; z.ty = z.y + vy / d * s; } else { z.tx = z.x; z.ty = z.y; }
					}
					double e = z.kb >= sb[i] ? H2Lurch(bt - z.kb) : 0; z.x = z.sx + (z.tx - z.sx) * e; z.y = z.sy + (z.ty - z.sy) * e;
				}
				// bumps: two zombies, a zombie and a pile, a zombie and a live wire
				for (int i = 0; i < n; i++)
				{
					var a = Z[i]; if (!a.on || a.dead) continue;
					for (int j = i + 1; j < n; j++)
					{
						var b = Z[j]; if (!b.on || b.dead) continue;
						if (hypot(a.x - b.x, a.y - b.y) < H2ZR * 1.8) { a.dead = b.dead = true; a.dt0 = b.dt0 = t; Corpse((a.x + b.x) / 2, (a.y + b.y) / 2, 24, t); break; }
					}
					if (a.dead) continue;
					foreach (var c in C) if (t < c.tb && hypot(a.x - c.x, a.y - c.y) < H2ZR + c.r * 0.8) { a.dead = true; a.dt0 = t; c.r = Math.Min(46, c.r + 5); burst(a.x, a.y, PINK, 5, 160); break; }
					if (a.dead) continue;
					foreach (var w in hazards)
					{
						if (w.kind != "H2Wire" || w.dead || w.o == null) continue; var s = ((Func<double, (bool on, double x0, double y0, double x1, double y1)>)w.o["seg"])(t);
						if (s.on && H2Seg(a.x, a.y, s.x0, s.y0, s.x1, s.y1) < H2ZR + 4) { a.dead = true; a.dt0 = t; Corpse(a.x, a.y, 22, t); burst(a.x, a.y, "#fff", 10, 260); break; }
					}
				}
			}
			foreach (var c in C) if (t >= c.tb && t - dt < c.tb) { burst(c.x, c.y, PINK, 14, 420); shake = Math.Max(shake, 4); }
		};
		// where zombie z is at time tf (later times: it keeps lurching towards the probe, for the bot's look ahead)
		(double x, double y) ZAt(int i, double tf, double qx, double qy)
		{
			var z = Z[i]; double btf = BT(tf); int kf = (int)Math.Floor(btf);
			if (!z.on) { if (btf < sb[i]) return (double.NaN, 0); if (kf <= Math.Floor(sb[i])) return (sx[i], sy[i]); }
			else if (kf <= z.kb) { double e = z.kb >= sb[i] ? H2Lurch(btf - z.kb) : 0; return (z.sx + (z.tx - z.sx) * e, z.sy + (z.ty - z.sy) * e); }
			double bx = z.on ? z.tx : sx[i], by = z.on ? z.ty : sy[i]; int k0 = z.on ? z.kb : (int)Math.Floor(sb[i]);
			double vx = qx - bx, vy = qy - by, d = hypot(vx, vy), m = Math.Min(d, st * (kf - k0 - 1) + st * H2Lurch(btf - kf));
			return d < 1e-6 ? (bx, by) : (bx + vx / d * m, by + vy / d * m);
		}
		H2Draw(h, t =>
		{
			double bt = BT(t), sink = H2k(bt, to, to + 1);
			// graves: they crack wn beats before the zombie comes out
			for (int i = 0; i < n; i++)
			{
				if (bt < sb[i] - wn || bt > to + 1) continue; double k = H2k(bt, sb[i] - wn, sb[i]);
				g.globalAlpha = H2va * (1 - sink) * (Z[i].on ? 0.5 : 1);
				g.fillStyle = "rgba(40,30,30,0.9)"; g.beginPath(); g.ellipse(sx[i], sy[i] + 6, 26, 12, 0, 0, TAU); g.fill();
				g.strokeStyle = H2G(0.4 + 0.5 * k); g.lineWidth = 2; g.beginPath();
				for (int c = 0; c < 5; c++) { double a = c * 1.26 + i, l = 10 + 18 * k; g.moveTo(sx[i], sy[i] + 6); g.lineTo(sx[i] + JS.cos(a) * l, sy[i] + 6 + JS.sin(a) * l * 0.5); }
				g.stroke();
				if (!Z[i].on && k > 0.4) { double u = (k - 0.4) / 0.6; g.fillStyle = H2G(0.8); g.fillRect(sx[i] - 3, sy[i] + 4 - 16 * u, 6, 16 * u); for (int f = 0; f < 3; f++) g.fillRect(sx[i] - 5 + f * 4, sy[i] + 2 - 16 * u - 6, 2, 6); }
			}
			// corpses (and their blast)
			foreach (var c in C)
			{
				double u = t - c.tb;
				if (u >= 0) { if (u < BH + 0.25) { double k = u < BH ? 1 : 1 - (u - BH) / 0.25; g.globalAlpha = H2va * k; g.fillStyle = H2P(0.62); circle(c.x, c.y, bR); g.strokeStyle = u < 0.05 ? PINKW : PINK; g.lineWidth = 4; g.beginPath(); g.arc(c.x, c.y, bR, 0, TAU); g.stroke(); } continue; }
				g.globalAlpha = H2va; double warn = H2k(t, c.tb - 1.5 * SPB, c.tb);
				if (warn > 0) { g.strokeStyle = (bt * 4 % 1 < 0.5) ? "#fff" : H2G(0.8); g.lineWidth = 2; g.beginPath(); g.arc(c.x, c.y, c.r + (bR - c.r) * warn, 0, TAU); g.stroke(); }
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
				for (int q = 0; q < 5; q++) { double a = q * 1.3 + c.x * 0.01; circle(c.x + JS.cos(a) * c.r * 0.35, c.y + JS.sin(a) * c.r * 0.3, c.r * 0.55); }
				g.shadowBlur = 0; g.fillStyle = "rgba(50,0,16,0.55)"; for (int q = 0; q < 4; q++) { double a = q * 1.9 + c.y * 0.01; circle(c.x + JS.cos(a) * c.r * 0.4, c.y + JS.sin(a) * c.r * 0.35, c.r * 0.16); }
			}
			// zombies
			for (int i = 0; i < n; i++)
			{
				var z = Z[i]; if (!z.on) continue; double fade = z.dead ? 1 - (t - z.dt0) / 0.2 : 1 - sink; if (fade <= 0) continue;
				g.globalAlpha = H2va * fade; double rise = Math.Min(1, (bt - sb[i]) * 3), r = H2ZR * (z.dead ? 1 + (t - z.dt0) * 3 : 1) * rise;
				double fx = z.tx - z.x, fy = z.ty - z.y, fd = hypot(fx, fy); if (fd < 1) { fx = (player?.x ?? W / 2) - z.x; fy = (player?.y ?? H / 2) - z.y; fd = Math.Max(1, hypot(fx, fy)); }
				double ux = fx / fd, uy = fy / fd;
				if (!z.dead && bt < to && fd > 4) H2Dash(z.x, z.y, z.tx, z.ty, H2G(0.3), 1.5);
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
				g.save(); g.translate(z.x, z.y); g.rotate(JS.atan2(uy, ux));
				g.fillRect(r * 0.2, -r * 0.75, r * 1.25, r * 0.28); g.fillRect(r * 0.2, r * 0.47, r * 1.15, r * 0.28);   // reaching arms
				circle(0, 0, r); g.shadowBlur = 0;
				g.fillStyle = "rgba(60,0,20,0.6)"; circle(-r * 0.25, 0, r * 0.55);
				g.globalAlpha = fade * Math.Max(H2va, 0.85); g.fillStyle = "#fff6d0"; circle(r * 0.45, -r * 0.3, r * 0.17); circle(r * 0.45, r * 0.3, r * 0.17);   // the eyes glow even in the dark
				g.restore();
			}
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			foreach (var c in C)
			{
				if (t < c.tb) { if (hypot(p.x - c.x, p.y - c.y) < c.r * 0.85 + p.r - 2) return true; }
				else if (t < c.tb + BH && hypot(p.x - c.x, p.y - c.y) < bR + p.r - 2) return true;
			}
			if (bt >= to) return false;
			for (int i = 0; i < n; i++)
			{
				if (Z[i].dead) continue; var q = ZAt(i, t, p.x, p.y); if (double.IsNaN(q.x)) continue;
				if (hypot(p.x - q.x, p.y - q.y) < H2ZR * 0.8 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the hellhounds ----------
	// lightning marks where the hound lands (x0,y0) at t0; it locks on the square (or on the haunted copy) right then, its path shows,
	// and wn s later it runs straight along it at sp px a second
	public static Hz H2Hound(double x0, double y0, bool atCopy, double wn, double sp, double t0)
	{
		var h = new Hz { kind = "H2Hound" }; double tx = W / 2, ty = H / 2;
		var pn = H2Now(true); tx = pn.x; ty = pn.y;
		if (atCopy) { var c = hazards.LastOrDefault(q => q.kind == "H2Copy" && !q.dead); if (c != null) { tx = c.x; ty = c.y; } }
		double dx = tx - x0, dy = ty - y0, d = hypot(dx, dy); if (d < 1) { dx = W / 2 - x0; dy = H / 2 - y0; d = Math.Max(1, hypot(dx, dy)); }
		dx /= d; dy /= d; double ta = t0 + wn, ang = JS.atan2(dy, dx);
		(double x, double y) P(double t) { double u = Math.Max(0, t - ta) * sp; return (x0 + dx * u, y0 + dy * u); }
		h.x = x0; h.y = y0;
		h.update = (t, dt) => { var q = P(t); h.x = q.x; h.y = q.y; if (q.x < -200 || q.x > W + 200 || q.y < -200 || q.y > H + 200) h.dead = true; };
		H2Draw(h, t =>
		{
			double u = t - t0;
			if (u < 0.25)
			{
				// the lightning
				g.globalAlpha = H2va * (1 - u / 0.25); g.strokeStyle = "#fff"; g.lineWidth = 3; g.shadowColor = "#fff"; g.shadowBlur = 14; g.beginPath(); g.moveTo(x0 + 40, -10);
				for (int i = 1; i <= 8; i++) { double f = i / 8.0; g.lineTo(x0 + 40 * (1 - f) + (i % 2 == 0 ? -1 : 1) * 18 * (1 - f), -10 + (y0 + 10) * f); }
				g.stroke(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.5)"; circle(x0, y0, 30 * (1 - u / 0.25));
			}
			g.globalAlpha = H2va;
			if (t < ta + 0.3) { double k = H2k(t, t0, ta); H2Dash(x0, y0, x0 + dx * 1700, y0 + dy * 1700, H2G(0.35 + 0.5 * k), 2 + 2 * k); }
			var q = P(t); bool run = t >= ta;
			g.save(); g.translate(q.x, q.y); g.rotate(ang);
			if (run) { g.fillStyle = H2P(0.25); g.fillRect(-60, -8, 50, 16); }
			g.fillStyle = run ? PINK : H2P(0.6 + 0.4 * H2k(t, t0, ta)); g.shadowColor = PINK; g.shadowBlur = run ? 14 : 6;
			g.beginPath(); g.ellipse(-16, 0, 30, 14, 0, 0, TAU); g.fill(); circle(13, 0, 12);
			g.beginPath(); g.moveTo(6, -6); g.lineTo(2, -16); g.lineTo(12, -8); g.moveTo(6, 6); g.lineTo(2, 16); g.lineTo(12, 8); g.fill();
			g.shadowBlur = 0; g.fillStyle = "#ffd36e"; circle(13, -4, 2.2); circle(13, 4, 2.2);
			g.restore();
		});
		h.hit = (p, t) =>
		{
			if (t < ta) return false; var q = P(t);
			return H2Seg(p.x, p.y, q.x - dx * 40, q.y - dy * 40, q.x + dx * 16, q.y + dy * 16) < 18 + p.r - 2;
		};
		return h;
	}

	// ---------- the coffins ----------
	// coffins at (cx,cy). Coffin ord[i] lights up at sh[i]; then coffin ord[i] bursts at ex[i] (R round it, hold s). Their reach shows as a
	// faint ring from the first light; the order is yours to remember (a burst only shakes its lid 0.2 s before)
	public static Hz H2Coffins(double[] cx, double[] cy, double[] ord, double[] sh, double[] ex, double R, double hold, double tw, double te)
	{
		var h = new Hz { kind = "H2Coffins" }; int m = ord.Length;
		h.update = (t, dt) => { for (int i = 0; i < m; i++) if (t >= ex[i] && t - dt < ex[i]) { int c = (int)ord[i]; burst(cx[c], cy[c], PINK, 16, 420); shake = Math.Max(shake, 5); } if (t > te + 0.5) h.dead = true; };
		H2Draw(h, t =>
		{
			if (t < tw) return; double a = Math.Min(H2k(t, tw, tw + 0.5), 1 - H2k(t, te, te + 0.4)); if (a <= 0) return;
			for (int c = 0; c < cx.Length; c++)
			{
				double lit = 0, shk = 0; bool open = false;
				for (int i = 0; i < m; i++) if ((int)ord[i] != c) continue; else { double u = t - sh[i]; if (u >= 0 && u < 0.45) lit = Math.Max(lit, 1 - u / 0.45); double v = ex[i] - t; if (v > 0 && v < 0.2) shk = 1; if (t >= ex[i] && t < ex[i] + hold + 0.3) open = true; }
				g.globalAlpha = H2va * a * 0.35; g.strokeStyle = H2G(0.6); g.lineWidth = 1.5; g.setLineDash(new double[] { 6, 8 }); g.beginPath(); g.arc(cx[c], cy[c], R, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				g.globalAlpha = H2va * a;
				g.save(); g.translate(cx[c] + (shk > 0 ? JS.sin(t * 90) * 3 : 0), cy[c]);
				g.fillStyle = "rgba(44,26,22,1)"; g.beginPath(); g.moveTo(-18, -52); g.lineTo(18, -52); g.lineTo(30, -24); g.lineTo(22, 52); g.lineTo(-22, 52); g.lineTo(-30, -24); g.closePath(); g.fill();
				g.strokeStyle = lit > 0 ? $"rgba(255,255,255,{str(0.4 + 0.6 * lit)})" : open ? PINK : "rgba(150,110,90,0.8)"; g.lineWidth = 3 + 3 * lit; if (lit > 0) { g.shadowColor = "#fff"; g.shadowBlur = 24 * lit; } g.stroke(); g.shadowBlur = 0;
				g.fillStyle = lit > 0 ? $"rgba(255,255,255,{str(lit)})" : "rgba(150,110,90,0.6)"; g.fillRect(-3, -34, 6, 34); g.fillRect(-12, -24, 24, 6);
				g.restore();
			}
			for (int i = 0; i < m; i++)
			{
				double u = t - ex[i]; if (u < 0 || u > hold + 0.25) continue; int c = (int)ord[i]; double k = u < hold ? 1 : 1 - (u - hold) / 0.25;
				g.globalAlpha = H2va * k; g.fillStyle = H2P(0.62); circle(cx[c], cy[c], R); g.strokeStyle = u < 0.05 ? PINKW : PINK; g.lineWidth = 5; g.shadowColor = PINK; g.shadowBlur = 16; g.beginPath(); g.arc(cx[c], cy[c], R, 0, TAU); g.stroke(); g.shadowBlur = 0;
				g.fillStyle = u < 0.05 ? PINKW : PINK; for (int s = 0; s < 10; s++) { double an = s * TAU / 10 + c, l = R * (0.55 + 0.4 * (s % 2)); g.beginPath(); g.moveTo(cx[c] + JS.cos(an - 0.12) * 30, cy[c] + JS.sin(an - 0.12) * 30); g.lineTo(cx[c] + JS.cos(an) * l, cy[c] + JS.sin(an) * l); g.lineTo(cx[c] + JS.cos(an + 0.12) * 30, cy[c] + JS.sin(an + 0.12) * 30); g.fill(); }
			}
		});
		h.hit = (p, t) => { for (int i = 0; i < m; i++) { if (t < ex[i] || t >= ex[i] + hold) continue; int c = (int)ord[i]; if (hypot(p.x - cx[c], p.y - cy[c]) < R + p.r - 2) return true; } return false; };
		return h;
	}

	// ---------- the tally marks ----------
	// chalk strokes (x0,y0)-(x1,y1): the stroke shows in grey wn[i] s before, then the chalk draws it from sb[i] in dd[i] s, w[i] thick.
	// What is drawn stays a wall until te
	public static Hz H2Tally(double[] x0, double[] y0, double[] x1, double[] y1, double[] sb, double[] dd, double[] w, double[] wn, double te)
	{
		var h = new Hz { kind = "H2Tally" }; int n = x0.Length;
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		H2Draw(h, t =>
		{
			double fade = 1 - H2k(t, te, te + 0.35); if (fade <= 0) return;
			for (int i = 0; i < n; i++)
			{
				if (t < sb[i] - wn[i]) continue;
				if (t < sb[i] + dd[i]) { g.globalAlpha = H2va * fade * (0.4 + 0.5 * H2k(t, sb[i] - wn[i], sb[i])); H2Dash(x0[i], y0[i], x1[i], y1[i], H2G(0.7), 2); g.fillStyle = H2G(0.8); circle(x0[i], y0[i], 5); }
				double k = H2k(t, sb[i], sb[i] + dd[i]); if (k <= 0) continue;
				double ex = x0[i] + (x1[i] - x0[i]) * k, ey = y0[i] + (y1[i] - y0[i]) * k, a = JS.atan2(y1[i] - y0[i], x1[i] - x0[i]), nx = -JS.sin(a), ny = JS.cos(a);
				g.globalAlpha = H2va * fade; g.strokeStyle = PINK; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = 10;
				for (int s = -1; s <= 1; s++) { g.lineWidth = w[i] * (s == 0 ? 0.8 : 0.35); double o = s * w[i] * 0.3; g.beginPath(); g.moveTo(x0[i] + nx * o, y0[i] + ny * o); g.lineTo(ex + nx * o * 0.6, ey + ny * o * 0.6); g.stroke(); }
				g.shadowBlur = 0; g.strokeStyle = "rgba(255,235,240,0.7)"; g.lineWidth = Math.Max(1.5, w[i] * 0.15); g.beginPath(); g.moveTo(x0[i], y0[i]); g.lineTo(ex, ey); g.stroke(); g.lineCap = "butt";
				if (k < 1) { g.fillStyle = "#fff"; circle(ex, ey, w[i] * 0.6 + 3); }
			}
		});
		h.hit = (p, t) =>
		{
			if (t >= te) return false;
			for (int i = 0; i < n; i++)
			{
				double k = H2k(t, sb[i], sb[i] + dd[i]); if (k <= 0) continue;
				if (H2Seg(p.x, p.y, x0[i], y0[i], x0[i] + (x1[i] - x0[i]) * k, y0[i] + (y1[i] - y0[i]) * k) < w[i] / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the dark and the lightning (harmless itself) ----------
	public static Hz H2Dark(double t0, double t1, double[] fl)
	{
		H2dk0 = t0; H2dk1 = t1; H2fl = fl;
		var h = new Hz { kind = "H2Dark", veil = true };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double d = Math.Min(H2k(t, t0, t0 + 0.6), 1 - H2k(t, t1, t1 + 0.6)); if (d <= 0) return; double f = 0; int fi = -1;
			for (int i = 0; i < fl.Length; i++) if (t >= fl[i]) { double v = JS.exp(-(t - fl[i]) * 5); if (v > f) { f = v; fi = i; } }
			g.save(); g.fillStyle = $"rgba(0,0,0,{str(0.86 * d * (1 - f))})"; g.fillRect(0, 0, W, H);
			if (fi >= 0 && f > 0.3)
			{
				double bx = 150 + (fi * 397) % 980; g.globalAlpha = f; g.strokeStyle = "#fff"; g.lineWidth = 3; g.shadowColor = "#fff"; g.shadowBlur = 18; g.beginPath(); g.moveTo(bx, 0);
				for (int i = 1; i <= 10; i++) g.lineTo(bx + ((i * 37 + fi * 13) % 70 - 35), i * 26);
				g.stroke(); g.shadowBlur = 0; g.fillStyle = $"rgba(255,255,255,{str(0.1 * f)})"; g.fillRect(0, 0, W, H);
			}
			g.restore();
		};
		return h;
	}

	// ---------- the haunted copy ----------
	// a corpse in your shape that does what you did dl s ago, mirrored left to right (the middle line is the mirror). Grey from tw, it hurts ta .. te
	public static Hz H2Copy(double tw, double ta, double te, double dl)
	{
		var h = new Hz { kind = "H2Copy" };
		(double x, double y) At(double tq)
		{
			int n = H2SN(songT); if (n == 0) { var p0 = H2Now(); return (W - p0.x, p0.y); }
			var a = H2S(0); if (tq <= a.t) return (W - a.x, a.y);
			int lo = 0, hi = n - 1; if (H2S(hi).t <= tq) { var z = H2S(hi); return (W - z.x, z.y); }
			while (hi - lo > 1) { int m = (lo + hi) / 2; if (H2S(m).t <= tq) lo = m; else hi = m; }
			var A = H2S(lo); var Bq = H2S(hi); double u = (tq - A.t) / Math.Max(1e-9, Bq.t - A.t);
			return (W - (A.x + (Bq.x - A.x) * u), A.y + (Bq.y - A.y) * u);
		}
		h.update = (t, dt) =>
		{
			if (t > te + 0.6) { h.dead = true; return; }
			H2SampUpd(t); var q = At(t - dl); h.x = q.x; h.y = q.y;
		};
		H2Draw(h, t =>
		{
			if (t < tw) return; double a = Math.Min(H2k(t, tw, tw + 0.6), 1 - H2k(t, te, te + 0.5)); if (a <= 0) return; bool act = t >= ta && t < te;
			// the mirror point
			g.globalAlpha = H2va * a * 0.45; g.strokeStyle = act ? H2P(0.5) : H2G(0.5); g.lineWidth = 2; g.setLineDash(new double[] { 4, 14 }); g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke(); g.setLineDash(new double[0]);
			for (int k = 3; k >= 0; k--)
			{
				var q = At(t - dl - k * 0.06); g.globalAlpha = H2va * a * (k == 0 ? 1 : 0.18);
				g.save(); g.translate(q.x, q.y); g.rotate(0.3 * JS.sin(t * 3));
				g.fillStyle = act ? PINK : H2G(0.75); if (k == 0) { g.shadowColor = PINK; g.shadowBlur = act ? 16 : 0; }
				g.fillRect(-22, -22, 44, 44); g.shadowBlur = 0;
				if (k == 0) { g.fillStyle = "rgba(20,0,8,0.9)"; circle(-5, -3, 3.5); circle(6, -3, 3.5); g.fillRect(-7, 6, 14, 2.5); g.strokeStyle = "rgba(20,0,8,0.8)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(-13, 0); g.lineTo(-4, 9); g.moveTo(13, -9); g.lineTo(5, -13); g.stroke(); }
				g.restore();
			}
		});
		h.hit = (p, t) => { if (t < ta || t >= te) return false; var q = At(t - dl); return Math.Abs(p.x - q.x) < 22 + p.r - 2 && Math.Abs(p.y - q.y) < 22 + p.r - 2; };
		return h;
	}

	// ---------- the nuke ----------
	// it counts down on the ticks (seconds); at ta everything but the shelter (x,y,r) burns for hold s
	public static Hz H2Nuke(double x, double y, double r, double tw, double ta, double hold, double[] tick)
	{
		var h = new Hz { kind = "H2Nuke" }; bool b0 = false;
		h.update = (t, dt) => { if (!b0 && t >= ta) { b0 = true; shake = Math.Max(shake, 14); flash = Math.Max(flash, 0.35); } if (t > ta + hold + 0.6) h.dead = true; };
		H2Draw(h, t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = H2k(t, tw, ta), tk = 0; foreach (var q in tick) if (t >= q) tk = Math.Max(tk, JS.exp(-(t - q) * 6));
				var gr = g.createRadialGradient(W / 2, H / 2, 200, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(255,42,109,0)"); gr.addColorStop(1, H2P(0.15 + 0.35 * k + 0.2 * tk)); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(20,14,16,0.85)"; circle(x, y, r);
				g.strokeStyle = tk > 0.5 ? "#fff" : "rgba(240,230,210,0.85)"; g.lineWidth = 3 + 4 * tk; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke();
				g.strokeStyle = "rgba(240,230,210,0.35)"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 4; i++) { double a = i * Math.PI / 4; g.moveTo(x - JS.cos(a) * r, y - JS.sin(a) * r); g.lineTo(x + JS.cos(a) * r, y + JS.sin(a) * r); } g.stroke();
				g.strokeStyle = H2G(0.5 * tk); g.lineWidth = 2; g.beginPath(); g.arc(x, y, r + 30 * (1 - tk) + 6, 0, TAU); g.stroke();
				return;
			}
			double u = t - ta; if (u > hold + 0.5) return; double a2 = u < hold ? 1 : 1 - (u - hold) / 0.5;
			g.globalAlpha = H2va * a2; g.fillStyle = u < 0.06 ? PINKW : "rgba(255,42,109,0.82)"; g.fillRect(0, 0, W, H);
			var gr2 = g.createRadialGradient(x, y, 0, x, y, r); gr2.addColorStop(0, "rgba(30,20,22,1)"); gr2.addColorStop(1, "rgba(14,8,10,1)"); g.fillStyle = gr2; circle(x, y, r);
			g.strokeStyle = "#fff"; g.lineWidth = 3; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke();
		});
		h.hit = (p, t) => t >= ta && t < ta + hold && hypot(p.x - x, p.y - y) > r - p.r + 2;
		return h;
	}

	// ---------- the graveyard itself (harmless) ----------
	// part 1 the graveyard, 2 the rounds, 3 the last round (red moon)
	public static Hz H2Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "H2Stage", veil = true };
		h.update = (t, dt) => { H2SampUpd(t); if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			g.fillStyle = part == 3 ? $"rgba(36,4,10,{str(0.55 + 0.12 * pulse)})" : "rgba(14,16,20,0.65)"; g.fillRect(0, 0, W, H);
			// the moon
			var mg = g.createRadialGradient(1080, 110, 10, 1080, 110, 170); mg.addColorStop(0, part == 3 ? "rgba(200,30,60,0.35)" : "rgba(210,215,230,0.22)"); mg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = mg; circle(1080, 110, 170);
			g.fillStyle = part == 3 ? "rgba(150,20,40,0.5)" : "rgba(200,205,220,0.25)"; circle(1080, 110, 46);
			// tombstones and crosses (fixed)
			g.fillStyle = "rgba(0,0,0,0.35)";
			for (int i = 0; i < 14; i++)
			{
				double x = 70 + i * 88 + (i * 37 % 29), y = 70 + (i * 53 % 4) * 150 + (i % 3) * 40, s = 0.8 + (i % 3) * 0.15;
				if (i % 4 == 0) { g.fillRect(x - 3 * s, y - 26 * s, 6 * s, 40 * s); g.fillRect(x - 13 * s, y - 16 * s, 26 * s, 6 * s); }
				else { g.beginPath(); g.moveTo(x - 14 * s, y + 14 * s); g.lineTo(x - 14 * s, y - 12 * s); g.arc(x, y - 12 * s, 14 * s, Math.PI, TAU); g.lineTo(x + 14 * s, y + 14 * s); g.closePath(); g.fill(); }
			}
			// the round's tally on the wall (part 2 and 3)
			if (part >= 2) { g.strokeStyle = "rgba(220,200,210,0.18)"; g.lineWidth = 4; g.beginPath(); int nn = part == 2 ? 4 : 5; for (int i = 0; i < Math.Min(4, nn); i++) { g.moveTo(40 + i * 14, 640); g.lineTo(44 + i * 14, 690); } if (nn == 5) { g.moveTo(30, 680); g.lineTo(98, 648); } g.stroke(); }
			// the boarded windows
			foreach (var w in H2WIN) H2Planks(w.x, w.y, 0, 0);
			// fog along the ground
			for (int i = 0; i < 6; i++) { double fx = ((i * 260 + t * (14 + i * 3)) % (W + 400)) - 200, fy = 560 + (i % 3) * 50; var fg = g.createRadialGradient(fx, fy, 0, fx, fy, 220); fg.addColorStop(0, "rgba(120,125,140,0.1)"); fg.addColorStop(1, "rgba(120,125,140,0)"); g.fillStyle = fg; circle(fx, fy, 220); }
			var gr = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_H2.json) ----------
	// times are beats; an event fires at its warning's start (hit - wn), the hounds when they lock on
	static double[] H2Sec(double[] bs) => bs.Select(B).ToArray();
	static readonly int _kh2fx = DodgeKind("H2fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "H2fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _kh2gloom = DodgeKind("H2gloom", e => () => { gloomT = e.num("v"); });
	static readonly int _kh2stage = DodgeKind("H2stage", e => () => hazards.Add(H2Stage(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _kh2arms = DodgeKind("H2arms", e => () =>
	{
		var hits = e.nums("hits"); var lf = e.nums("lf");
		hazards.Add(H2Arms(e.nums("win").Select(v => (int)v).ToArray(), H2Sec(hits), hits.Select((v, i) => lf.Length > i ? lf[i] : lf.Length > 0 ? lf[^1] : 1).ToArray(), e.num("wn") * SPB, e.num("hold") * SPB, e.num("w")));
	});
	static readonly int _kh2wire = DodgeKind("H2wire", e => () =>
		hazards.Add(H2Wire(e.num("x", W / 2), e.num("y", H / 2), e.num("L"), e.num("a0") * Math.PI / 180, e.nums("sb"), e.nums("sw").Select(v => v * Math.PI / 180).ToArray(), e.nums("on"), B(e.b), B(e.num("hit")), B(e.num("to")))));
	static readonly int _kh2horde = DodgeKind("H2horde", e => () =>
		hazards.Add(H2Horde(e.nums("sb"), e.nums("sx"), e.nums("sy"), e.num("st"), e.num("wn"), e.num("to"), e.num("boom"), e.num("bR", 80))));
	static readonly int _kh2hound = DodgeKind("H2hound", e => () =>
		hazards.Add(H2Hound(e.num("x"), e.num("y"), e.flag("copy"), e.num("wn") * SPB, e.num("sp", 1400), B(e.b))));
	static readonly int _kh2coffins = DodgeKind("H2coffins", e => () =>
		hazards.Add(H2Coffins(e.nums("cx"), e.nums("cy"), e.nums("ord"), H2Sec(e.nums("sh")), H2Sec(e.nums("ex")), e.num("R"), e.num("hold") * SPB, B(e.b), B(e.num("to")))));
	static readonly int _kh2tally = DodgeKind("H2tally", e => () =>
	{
		var sb = e.nums("sb"); int n = sb.Length;
		double[] Or(string k, double d) { var v = e.nums(k); return Enumerable.Range(0, n).Select(i => v.Length > i ? v[i] : v.Length > 0 ? v[^1] : d).ToArray(); }
		hazards.Add(H2Tally(e.nums("x0"), e.nums("y0"), e.nums("x1"), e.nums("y1"), H2Sec(sb), Or("dd", 0.5).Select(v => v * SPB).ToArray(), Or("w", 14), Or("wn", 1).Select(v => v * SPB).ToArray(), B(e.num("to"))));
	});
	static readonly int _kh2dark = DodgeKind("H2dark", e => () => hazards.Add(H2Dark(B(e.b), B(e.num("to")), H2Sec(e.nums("fl")))));
	static readonly int _kh2copy = DodgeKind("H2copy", e => () => hazards.Add(H2Copy(B(e.b), B(e.num("hit")), B(e.num("to")), e.num("dl", 0.45))));
	static readonly int _kh2nuke = DodgeKind("H2nuke", e => () => hazards.Add(H2Nuke(e.num("x"), e.num("y"), e.num("r"), B(e.b), B(e.num("hit")), e.num("hold") * SPB, H2Sec(e.nums("tick")))));
}
