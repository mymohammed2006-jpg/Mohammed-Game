using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L14 "Jump again" (Neon Beats): inside a pinball machine. Everything pink rebounds on the beat.
// Part 1 the top of the machine (bumpers, drop targets, rollover lanes), part 2 the play field (flippers, multiball,
// the lights go out and the dot display runs JUMP AGAIN), part 3 the jackpot (the giant ball, four flippers, the drain, REPLAY).
// Every hazard is a pure function of time once it exists (rings, balls and split balls are part of the hazard that makes them),
// so hit(p, t) is right for any later t. The data is data/dodge_L14.json (made by tools/level14/gen.py).
public static partial class G
{
	// one straight piece of a ball's path: from second t at (x,y) with speed (vx,vy)
	public class L14Seg { public double t, x, y, vx, vy; }

	// a ball's whole path, worked out in advance: it rebounds off the screen edges nb times, then leaves the screen
	public static List<L14Seg> L14Path(double x, double y, double vx, double vy, double r, double t0, int nb, double tEnd = 1e9)
	{
		var s = new List<L14Seg> { new L14Seg { t = t0, x = x, y = y, vx = vx, vy = vy } }; double t = t0;
		for (int k = 0; k < nb; k++)
		{
			double tx = vx > 0 ? (W - r - x) / vx : vx < 0 ? (r - x) / vx : 1e9, ty = vy > 0 ? (H - r - y) / vy : vy < 0 ? (r - y) / vy : 1e9;
			if (tx < 1e-6) tx = 1e9; if (ty < 1e-6) ty = 1e9;
			double d = Math.Min(tx, ty); if (d >= 1e8 || t + d > tEnd) break;
			x += vx * d; y += vy * d; t += d; if (tx <= ty + 1e-9) vx = -vx; if (ty <= tx + 1e-9) vy = -vy;
			s.Add(new L14Seg { t = t, x = x, y = y, vx = vx, vy = vy });
		}
		return s;
	}
	public static (double x, double y) L14At(List<L14Seg> s, double t)
	{
		var q = s[0]; for (int i = s.Count - 1; i >= 0; i--) if (t >= s[i].t) { q = s[i]; break; }
		double d = t - q.t; return (q.x + q.vx * d, q.y + q.vy * d);
	}
	static double L14Ease(double k) => 1 - JS.pow(1 - clamp(k, 0, 1), 3);

	// a flying ball: its path, its size, and when it leaves the screen
	public class L14B
	{
		public List<L14Seg> s; public double r, t0, tOut;
		public L14B(double x, double y, double vx, double vy, double r, double t0, int nb)
		{
			s = L14Path(x, y, vx, vy, r, t0, nb); this.r = r; this.t0 = t0; var e = s[s.Count - 1];
			// after the last rebound it flies straight out
			double tx = e.vx > 0 ? (W + r + 2 - e.x) / e.vx : e.vx < 0 ? (-r - 2 - e.x) / e.vx : 1e9, ty = e.vy > 0 ? (H + r + 2 - e.y) / e.vy : e.vy < 0 ? (-r - 2 - e.y) / e.vy : 1e9;
			tOut = e.t + Math.Max(0, Math.Min(tx, ty));
		}
		public bool live(double t) => t >= t0 && t < tOut;
		public bool hit(Player p, double t) { if (!live(t)) return false; var q = L14At(s, t); return hypot(p.x - q.x, p.y - q.y) < r + p.r - 2; }
		public void draw(double t)
		{
			if (!live(t)) return; var q = L14At(s, t); bool fw = t - t0 < 0.05;
			for (int i = 1; i < s.Count; i++) { double u = t - s[i].t; if (u >= 0 && u < 0.25) { fw |= u < 0.05; g.strokeStyle = $"rgba(255,255,255,{str(0.7 * (1 - u / 0.25))})"; g.lineWidth = 3; g.beginPath(); g.arc(s[i].x, s[i].y, r + 40 * u / 0.25, 0, TAU); g.stroke(); } }
			L14BallDraw(q.x, q.y, r, fw);
		}
	}

	// the steel ball look: pink ball and a white shine (white for a moment when it hits a wall)
	static void L14BallDraw(double x, double y, double r, bool flashW)
	{
		g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = flashW ? "#fff" : PINK; circle(x, y, r * (1 + 0.1 * pulse)); g.shadowBlur = 0;
		g.fillStyle = "rgba(255,255,255,0.55)"; circle(x - r * 0.35, y - r * 0.35, r * 0.28); g.restore();
	}

	// a shock ring: grows from r0 by dr in dur seconds and fades (it only reaches so far)
	public class L14Ring
	{
		public double x, y, t0, r0, dr, dur, th;
		double rad(double t) => r0 + dr * L14Ease((t - t0) / dur);
		public bool hit(Player p, double t) { if (t < t0 || t > t0 + dur * 0.9) return false; double u = (t - t0) / dur; return Math.Abs(hypot(p.x - x, p.y - y) - rad(t)) < th * (1 - 0.4 * u) / 2 + p.r - 2; }
		public void draw(double t)
		{
			if (t < t0 || t > t0 + dur) return; double u = (t - t0) / dur; g.save(); g.globalAlpha = Math.Max(0, 1 - u * u); g.strokeStyle = u < 0.08 ? "#fff" : PINK; g.shadowColor = PINK; g.shadowBlur = 12;
			g.lineWidth = th * (1 - 0.4 * u); g.beginPath(); g.arc(x, y, rad(t), 0, TAU); g.stroke(); g.restore();
		}
	}

	// a pinball: a grey ghost and a short arrow show where it starts and where it goes, then it flies and rebounds nb times.
	// split: at its first rebound a second ball breaks off (multiball)
	public static Hz L14Ball(double x, double y, double vx, double vy, double r, double tw, double t0, int nb, bool split = false, double spin = 0.6)
	{
		var b = new L14B(x, y, vx, vy, r, t0, nb); L14B c = null; double ang = JS.atan2(vy, vx);
		if (split && b.s.Count > 1) { var q = b.s[1]; double a = JS.atan2(q.vy, q.vx) + spin, v = hypot(q.vx, q.vy); c = new L14B(q.x, q.y, JS.cos(a) * v, JS.sin(a) * v, r * 0.85, q.t, 1); }
		var h = new Hz { r = r, kind = "L14Ball" }; bool burstDone = false;
		h.update = (t, dt) =>
		{
			if (c != null && !burstDone && t >= c.t0) { burstDone = true; burst(c.s[0].x, c.s[0].y, "#fff", 8, 220); }
			if (t >= b.tOut && (c == null || t >= c.tOut)) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0)
			{
				double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = GREY + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, r * (1.8 - 0.8 * k), 0, TAU); g.stroke();
				g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.25 * k)})"; circle(x, y, r * k);
				double L = 30 + 60 * k; g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; g.beginPath(); g.moveTo(x + JS.cos(ang) * r * 1.4, y + JS.sin(ang) * r * 1.4); g.lineTo(x + JS.cos(ang) * (r + L), y + JS.sin(ang) * (r + L)); g.stroke();
				return;
			}
			b.draw(t); c?.draw(t);
		};
		h.hit = (p, t) => b.hit(p, t) || (c != null && c.hit(p, t));
		return h;
	}

	// a pop bumper: a pink cap that kicks out a shock ring on every pop
	public static Hz L14Bumper(double x, double y, double R, double tw, double ta, double te, double[] pops, double popR)
	{
		var rings = pops.Select(p => new L14Ring { x = x, y = y, t0 = p, r0 = R, dr = popR, dur = 0.35, th = 10 }).ToArray(); int pi = 0;
		var h = new Hz { x = x, y = y, r = R, kind = "L14Bumper" };
		Func<double, double> bump = t => { double b = 0; foreach (var p in pops) if (t >= p) b = Math.Max(b, JS.exp(-(t - p) * 12)); return 1 + 0.2 * b; };
		h.update = (t, dt) =>
		{
			while (pi < pops.Length && t >= pops[pi]) { burst(x, y, PINK, 6, 220); pi++; }
			if (t > Math.Max(te + 0.25, pops.Length > 0 ? pops.Max() + 0.4 : 0)) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.18 * k)})"; circle(x, y, R * k);
				g.strokeStyle = GREY + str(0.35 + 0.45 * k) + ")"; g.lineWidth = 3; g.beginPath(); g.arc(x, y, R, 0, TAU); g.stroke(); return;
			}
			foreach (var r in rings) r.draw(t);
			double s = t > te ? Math.Max(0, 1 - (t - te) / 0.25) : bump(t), rr2 = R * s; if (rr2 <= 0) return;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = t - ta < 0.07 ? "#fff" : PINK; circle(x, y, rr2); g.shadowBlur = 0;
			g.strokeStyle = "rgba(0,0,0,0.35)"; g.lineWidth = 5; g.beginPath(); g.arc(x, y, rr2 * 0.68, 0, TAU); g.stroke();
			g.fillStyle = "rgba(255,255,255,0.8)"; g.translate(x, y); g.rotate(t * 1.5); poly(5, rr2 * 0.28); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; if (t < te && hypot(p.x - x, p.y - y) < R * bump(t) + p.r - 2) return true; foreach (var r in rings) if (r.hit(p, t)) return true; return false; };
		return h;
	}

	// a bank of drop targets: n tall bars over the whole screen (gw next to each other missing), they stand up, then fall one by one
	public static Hz L14Bank(int n, int gap, double tw, double ta, double[] drops, int gw = 2)
	{
		double cw = W / n; var h = new Hz { kind = "L14Bank" }; bool isGap(int i) => i >= gap && i < gap + gw;
		Func<int, double, double> ht = (i, t) => { if (isGap(i) || t < ta) return 0; double up = clamp((t - ta) / 0.08, 0, 1), dn = clamp((t - drops[i]) / 0.12, 0, 1); return H * up * (1 - dn); };
		var fell = new bool[n];
		h.update = (t, dt) => { for (int i = 0; i < n; i++) if (!fell[i] && !isGap(i) && t >= drops[i]) { fell[i] = true; burst(i * cw + cw / 2, H - 10, PINK, 5, 200); } if (t > drops.Max() + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			for (int i = 0; i < n; i++)
			{
				if (isGap(i)) continue; double x = i * cw + 3, w = cw - 6;
				if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(x, H * (1 - k), w, H * k); g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(x, H * (1 - k), w, 3); continue; }
				double hh = ht(i, t); if (hh <= 0) continue;
				g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = t - ta < 0.07 ? "#fff" : PINK; g.fillRect(x, H - hh, w, hh); g.shadowBlur = 0;
				g.fillStyle = "rgba(0,0,0,0.3)"; g.fillRect(x + w * 0.3, H - hh + 16, w * 0.4, Math.Max(0, hh - 32)); g.restore();
			}
		};
		h.hit = (p, t) => { int i = (int)Math.Floor(p.x / cw); for (int j = i - 1; j <= i + 1; j++) { if (j < 0 || j >= n) continue; double hh = ht(j, t); if (hh > 0 && p.x + p.r - 1 > j * cw + 3 && p.x - p.r + 1 < j * cw + cw - 3 && p.y + p.r - 1 > H - hh) return true; } return false; };
		return h;
	}

	// a rollover lane: a column lights up pink with spinning stripes
	public static Hz L14Lane(double x, double w, double tw, double ta, double te)
	{
		var h = new Hz { x = x, kind = "L14Lane" };
		h.update = (t, dt) => { if (t > te + 0.15) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; g.fillRect(x - w / 2, 0, w, H); g.strokeStyle = GREY + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 2; g.strokeRect(x - w / 2 + 4, 4, w - 8, H - 8); return; }
			double a = t > te ? Math.Max(0, 1 - (t - te) / 0.15) : 1; if (a <= 0) return;
			g.save(); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = t - ta < 0.06 ? "#fff" : PINK; g.fillRect(x - w / 2 + 3, 0, w - 6, H); g.shadowBlur = 0;
			g.fillStyle = "rgba(0,0,0,0.28)"; double o = ((t - ta) * 900) % 60; for (double y = -60 + o; y < H; y += 60) g.fillRect(x - w / 2 + 3, y, w - 6, 18); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < te && Math.Abs(p.x - x) < w / 2 - 3 + p.r - 1;
		return h;
	}

	// a flipper: a pink paddle on a pivot. It flips at each time in flips (a grey wedge shows its swing first) and shoots a ball from its tip
	public static Hz L14Flipper(double px, double py, double len, double a0, double a1, double tw, double ts, double te, double[] flips, double wn, double bsp, int nb, bool split = false)
	{
		var h = new Hz { kind = "L14Flipper" }; double sg = Math.Sign(a1 - a0);
		// the balls it will shoot (their paths are known from the start; a little random angle each)
		var balls = new List<Hz>(); var bt0 = new List<double>();
		foreach (var f in flips)
		{
			double tx = px + JS.cos(a1) * len * 0.85, ty = py + JS.sin(a1) * len * 0.85, a = a1 + sg * Math.PI / 2 + (rnd() - 0.5) * 0.5;
			balls.Add(L14Ball(tx, ty, JS.cos(a) * bsp, JS.sin(a) * bsp, 14, f + 0.07, f + 0.07, nb, split)); bt0.Add(f + 0.07);
		}
		Func<double, double> ang = t =>
		{
			double k = 0; foreach (var f in flips) { if (t < f - 0.001) break; double u = t - f; k = u < 0.07 ? u / 0.07 : u < 0.3 ? 1 : u < 0.45 ? 1 - (u - 0.3) / 0.15 : 0; if (k > 0) break; }
			return a0 + (a1 - a0) * k;
		};
		h.update = (t, dt) =>
		{
			for (int i = 0; i < balls.Count; i++) if (t >= bt0[i] && !balls[i].dead) balls[i].update(t, dt);
			if (t > te + 0.3 && balls.All(b => b.dead || t < bt0[balls.IndexOf(b)])) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ts) { double k = clamp((t - tw) / (ts - tw), 0, 1); g.save(); g.translate(px, py); g.rotate(a0); g.strokeStyle = GREY + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 3; rr(-22, -20, (len + 22) * k, 40, 20); g.stroke(); g.restore(); return; }
			for (int i = 0; i < balls.Count; i++) if (t >= bt0[i] && !balls[i].dead) balls[i].draw(t);
			double al = t > te ? Math.Max(0, 1 - (t - te) / 0.3) : 1; if (al <= 0) return;
			foreach (var f in flips)
			{
				if (t < f - wn || t >= f) continue; double k = clamp((t - f + wn) / wn, 0, 1);
				g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; g.beginPath(); g.moveTo(px, py); g.arc(px, py, len + 16, Math.Min(a0, a1), Math.Max(a0, a1)); g.closePath(); g.fill();
				g.strokeStyle = GREY + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 2; g.beginPath(); g.arc(px, py, len + 16, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke();
			}
			double an = ang(t); g.save(); g.globalAlpha = al; g.translate(px, py); g.rotate(an); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK;
			g.beginPath(); g.arc(0, 0, 24, Math.PI / 2, Math.PI * 1.5); g.lineTo(len, -11); g.arc(len, 0, 11, -Math.PI / 2, Math.PI / 2); g.lineTo(0, 24); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(0,0,0,0.35)"; circle(0, 0, 9); g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int i = 0; i < balls.Count; i++) if (t >= bt0[i] && balls[i].hit(p, t)) return true;
			if (t < ts || t >= te) return false; double an = ang(t), ex = px + JS.cos(an) * len, ey = py + JS.sin(an) * len;
			double dx = ex - px, dy = ey - py, u = clamp(((p.x - px) * dx + (p.y - py) * dy) / (len * len), 0, 1); return segDist(p.x, p.y, new[] { px, py }, new[] { ex, ey }) < 24 - 13 * u + p.r - 2;
		};
		return h;
	}

	// a slingshot: the pink triangle above a flipper. On every kick it swells and its long side punches out
	public static Hz L14Sling(double x, double sd, double tw, double ts, double te, double[] kicks)
	{
		var h = new Hz { kind = "L14Sling" }; double cx = x - sd * 30, cy = 503;
		Func<double, double> sw = t => { double b = 0; foreach (var k in kicks) if (t >= k) b = Math.Max(b, JS.exp(-(t - k) * 9)); return 1 + 0.55 * b; };
		(double x, double y)[] pts(double t) { double s2 = sw(t); return new[] { (x, 380.0), (x, 540.0), (x - sd * 90, 590.0) }.Select(q => (cx + (q.Item1 - cx) * s2, cy + (q.Item2 - cy) * s2)).ToArray(); }
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; var P = pts(t);
			if (t < ts) { double k = clamp((t - tw) / (ts - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.strokeStyle = GREY + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 3; }
			else { double a = t > te ? Math.Max(0, 1 - (t - te) / 0.3) : 1; g.globalAlpha = a; g.fillStyle = sw(t) > 1.4 ? "#fff" : PINK; g.shadowColor = PINK; g.shadowBlur = 18; }
			g.beginPath(); g.moveTo(P[0].x, P[0].y); g.lineTo(P[1].x, P[1].y); g.lineTo(P[2].x, P[2].y); g.closePath(); g.fill(); if (t < ts) g.stroke();
			g.shadowBlur = 0; g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t >= te) return false; var P = pts(t); double m = p.r - 2; bool inside = true; double sgn = 0;
			for (int i = 0; i < 3; i++)
			{
				var a = P[i]; var b = P[(i + 1) % 3]; double cr = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
				if (sgn == 0) sgn = Math.Sign(cr); else if (Math.Sign(cr) != sgn && cr != 0) inside = false;
				if (segDist(p.x, p.y, new[] { a.x, a.y }, new[] { b.x, b.y }) < m) return true;
			}
			return inside;
		};
		return h;
	}

	// the dot display's letters (5 x 7 dots)
	static readonly Dictionary<char, string[]> L14FONT = new()
	{
		['J'] = new[] { "00111", "00010", "00010", "00010", "10010", "10010", "01100" },
		['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
		['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
		['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
		['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
		['G'] = new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01111" },
		['I'] = new[] { "01110", "00100", "00100", "00100", "00100", "00100", "01110" },
		['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
		['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
		['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
		['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
		['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
		['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
		[' '] = new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" },
	};
	public static bool[][] L14Bits(string s)
	{
		var cols = new List<bool[]>();
		foreach (var ch in s) { var f = L14FONT[ch]; for (int c = 0; c < 5; c++) { var col = new bool[7]; for (int r = 0; r < 7; r++) col[r] = f[r][c] == '1'; cols.Add(col); } cols.Add(new bool[7]); }
		return cols.ToArray();
	}

	// the dot display of the machine: a line of giant letters moves across the screen (dir -1 to the left, 1 to the right),
	// rising and sinking with the music (amp px, a wave every 8 beats from beat b0)
	public static Hz L14Matrix(string s, double c, double y0, double dir, double sp, double tw, double ta, double te, double amp, double b0)
	{
		var bits = L14Bits(s); int n = bits.Length; double len = n * c;
		Func<double, double> x0 = t => dir < 0 ? W + 40 - sp * (t - ta) : -len - 40 + sp * (t - ta);
		Func<double, double> yo = t => JS.sin((BT(t) - b0) * Math.PI / 4) * amp;
		var h = new Hz { kind = "L14Matrix" };
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double a = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : t > te ? Math.Max(0, 1 - (t - te) / 0.3) : 1; if (a <= 0) return; double X = x0(t), Y = y0 + yo(t);
			// the dark dots of the display (the band where the letters pass)
			g.fillStyle = GREY + str(0.08 * a) + ")"; for (double x = ((X % c) + c) % c; x < W; x += c) for (int r = 0; r < 7; r++) circle(x + c / 2, Y + r * c + c / 2, 3);
			if (t < ta) return;
			g.save(); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath();
			int j0 = Math.Max(0, (int)Math.Floor(-X / c)), j1 = Math.Min(n - 1, (int)Math.Floor((W - X) / c));
			for (int j = j0; j <= j1; j++) for (int r = 0; r < 7; r++) if (bits[j][r]) g.rect(X + j * c + 3, Y + r * c + 3, c - 6, c - 6);
			g.fill(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double X = x0(t), Y = y0 + yo(t); int j = (int)Math.Floor((p.x - X) / c), r0 = (int)Math.Floor((p.y - Y) / c);
			for (int jj = j - 1; jj <= j + 1; jj++) for (int r = r0 - 1; r <= r0 + 1; r++)
			{
				if (jj < 0 || jj >= n || r < 0 || r > 6 || !bits[jj][r]) continue;
				// lit dots next to each other count as one block (the square can't fit between them anyway)
				bool L = jj > 0 && bits[jj - 1][r], Rt = jj < n - 1 && bits[jj + 1][r], U = r > 0 && bits[jj][r - 1], D = r < 6 && bits[jj][r + 1];
				double lx = X + jj * c + (L ? 0 : 3), rx2 = X + (jj + 1) * c - (Rt ? 0 : 3), ly = Y + r * c + (U ? 0 : 3), ry2 = Y + (r + 1) * c - (D ? 0 : 3);
				double qx = clamp(p.x, lx, rx2), qy = clamp(p.y, ly, ry2); if (hypot(p.x - qx, p.y - qy) < p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// the boss: the jackpot ball. A giant ball that rebounds round the screen; every rebound sends out a ring.
	// From tw2 it splits in two mirror halves (left and right) that join again whenever they meet in the middle. At tDrain it falls into the drain
	public static Hz L14Jackpot(double x, double y, double vx, double vy, double R, double t0, double tw2, double tDrain, double te, double shockR, int ringEvery = 1)
	{
		var s = L14Path(x, y, vx, vy, R, t0, 200, tDrain); int bi = 1;
		bool twin(double t) => t >= tw2 && t < tDrain;
		var rings = new List<L14Ring>();
		for (int i = 1; i < s.Count; i += ringEvery)
		{
			var b = s[i]; double sx = b.x <= R + 1 ? 0 : b.x >= W - R - 1 ? W : b.x, sy = b.y <= R + 1 ? 0 : b.y >= H - R - 1 ? H : b.y;
			rings.Add(new L14Ring { x = sx, y = sy, t0 = b.t, r0 = 10, dr = shockR, dur = 0.5, th = 18 });
			if (twin(b.t)) rings.Add(new L14Ring { x = W - sx, y = sy, t0 = b.t, r0 = 10, dr = shockR, dur = 0.5, th = 18 });
		}
		Func<double, (double x, double y)> pos = t =>
		{
			if (t < tDrain) return L14At(s, t);
			var q = L14At(s, tDrain); double k = clamp((t - tDrain) / (te - tDrain), 0, 1); return (q.x + (W / 2 - q.x) * k, q.y + (H + R * 2 - q.y) * k * k);
		};
		var h = new Hz { r = R, kind = "L14Jackpot" }; bool gone = false;
		h.update = (t, dt) =>
		{
			while (bi < s.Count && t >= s[bi].t) { shake = Math.Max(shake, 6); bi++; }
			if (t > te && !gone) { gone = true; burst(W / 2, H - 20, PINK, 40, 500); burst(W / 2, H - 20, "#fff", 20, 400); shake = Math.Max(shake, 14); }
			if (t > te + 0.6) h.dead = true;
			var q = pos(t); h.x = q.x; h.y = q.y;
		};
		h.draw = t =>
		{
			if (t < t0) return; foreach (var r in rings) r.draw(t); if (t > te) return;
			var q = pos(t); bool tw = twin(t), joined = tw && Math.Abs(q.x - W / 2) < R;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 34; g.fillStyle = PINK;
			foreach (var xx in tw && !joined ? new[] { q.x, W - q.x } : new[] { q.x })
			{
				circle(xx, q.y, R * (1 + 0.08 * pulse)); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.5)"; circle(xx - R * 0.35, q.y - R * 0.35, R * 0.25);
				g.strokeStyle = "rgba(0,0,0,0.3)"; g.lineWidth = 6; g.beginPath(); g.arc(xx, q.y, R * 0.72, 0, TAU); g.stroke();
				g.fillStyle = "rgba(0,0,0,0.45)"; g.font = $"800 {str(R * 0.36)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText("777", xx, q.y + 2);
				g.fillStyle = PINK; g.shadowBlur = 34;
			}
			if (joined) { g.globalAlpha = 0.6; g.fillStyle = "#fff"; circle(q.x, q.y, R * 0.6); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; foreach (var r in rings) if (r.hit(p, t)) return true; if (t > te) return false;
			var q = pos(t); if (hypot(p.x - q.x, p.y - q.y) < R + p.r - 3) return true; return twin(t) && hypot(p.x - (W - q.x), p.y - q.y) < R + p.r - 3;
		};
		return h;
	}

	// a ball that falls into the drain at the bottom middle (it speeds up as it falls). A grey dot at the top shows where it drops
	public static Hz L14Fall(double x0, double tw, double t0, double dur, double r = 15)
	{
		Func<double, (double x, double y)> pos = t => { double k = clamp((t - t0) / dur, 0, 1); return (x0 + (W / 2 - x0) * k * k * 0.85, -r + (H + 3 * r) * k * k); };
		var h = new Hz { r = r, kind = "L14Fall" };
		h.update = (t, dt) => { if (t > t0 + dur) h.dead = true; var q = pos(t); h.x = q.x; h.y = q.y; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * k)})"; circle(x0, 8, 4 + 8 * k); g.strokeStyle = GREY + str(0.2 + 0.3 * k) + ")"; g.lineWidth = 2; g.beginPath(); g.moveTo(x0, 0); g.lineTo(x0, 40 + 80 * k); g.stroke(); return; }
			var q = pos(t); L14BallDraw(q.x, q.y, r, t - t0 < 0.05);
		};
		h.hit = (p, t) => { if (t < t0 || t > t0 + dur) return false; var q = pos(t); return hypot(p.x - q.x, p.y - q.y) < r + p.r - 2; };
		return h;
	}

	// a ramp: a curved rail (an arc around cx,cy with radius rr from angle a0 to a1) that trains of balls race along.
	// The rail shows grey from tw and turns pink before each train; each train is nb balls gap seconds apart, sp px/s
	public static Hz L14Ramp(double cx, double cy, double rr, double a0, double a1, double tw, double[] trains, double wnT, double sp, int nb, double gap, double r)
	{
		double len = Math.Abs(a1 - a0) * rr, dur = len / sp, sg = Math.Sign(a1 - a0), tEnd = trains.Max() + (nb - 1) * gap + dur;
		(double x, double y) at(double u) { double a = a0 + (a1 - a0) * u; return (cx + JS.cos(a) * rr, cy + JS.sin(a) * rr); }
		var h = new Hz { kind = "L14Ramp" };
		h.update = (t, dt) => { if (t > tEnd + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(1, Math.Min((t - tw) / 0.3, (tEnd + 0.3 - t) / 0.3)); if (a <= 0) return;
			// how close the next train is (0..1): the rail glows pink with it
			double warn = 0; foreach (var tr in trains) if (t < tr && t > tr - wnT) warn = Math.Max(warn, 1 - (tr - t) / wnT);
			g.save(); g.globalAlpha = a; g.lineCap = "round";
			g.strokeStyle = GREY + "0.12)"; g.lineWidth = 2 * r + 10; g.beginPath(); g.arc(cx, cy, rr, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke();
			g.strokeStyle = GREY + "0.45)"; g.lineWidth = 2; foreach (var off in new[] { -r - 5, r + 5 }) { g.beginPath(); g.arc(cx, cy, rr + off, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke(); }
			if (warn > 0) { g.strokeStyle = $"rgba(255,42,109,{str(0.08 + 0.3 * warn)})"; g.lineWidth = 2 * r; g.beginPath(); g.arc(cx, cy, rr, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke(); }
			// the holes at both ends
			foreach (var u in new[] { 0.0, 1.0 }) { var q = at(u); g.fillStyle = "rgba(0,0,0,0.5)"; circle(q.x, q.y, r + 6); g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(q.x, q.y, r + 6, 0, TAU); g.stroke(); }
			g.restore();
			foreach (var tr in trains) for (int j = 0; j < nb; j++) { double u = (t - tr - j * gap) / dur; if (u < 0 || u > 1) continue; var q = at(u); L14BallDraw(q.x, q.y, r, u * dur < 0.05); }
		};
		h.hit = (p, t) =>
		{
			foreach (var tr in trains) for (int j = 0; j < nb; j++) { double u = (t - tr - j * gap) / dur; if (u < 0 || u > 1) continue; var q = at(u); if (hypot(p.x - q.x, p.y - q.y) < r + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the magnet in the middle: each cycle (4 beats from cs[i]) n balls come in from far away (r0) to the orbit (ro) in tin,
	// spin around the magnet, and at cs[i] + tout they all shoot straight out (spd). The next cycle comes in between the last one's lines
	public static Hz L14Magnet(double[] cs, double tin, double tout, int n, double r0, double ro, double w, double spd, double R, double tw, double te, double r)
	{
		double cx = W / 2, cy = H / 2; var A0 = new double[cs.Length]; double a = -Math.PI / 2;
		for (int i = 0; i < cs.Length; i++) { A0[i] = a; double dir = i % 2 == 0 ? 1 : -1; a = a + dir * w * (tout - tin) + Math.PI / n; }
		double spin(int i, double t) { double dir = i % 2 == 0 ? 1 : -1; return A0[i] + dir * w * clamp(t - cs[i] - tin, 0, tout - tin); }
		// where ball k of cycle i is at t (null before it comes or after it is gone)
		(double x, double y)? ball(int i, int k, double t)
		{
			double u = t - cs[i]; if (u < 0) return null; double an = spin(i, t) + k * TAU / n, rad;
			if (u < tin) rad = r0 + (ro - r0) * (u / tin); else if (u < tout) rad = ro; else rad = ro + spd * (u - tout);
			if (rad > 900) return null; return (cx + JS.cos(an) * rad, cy + JS.sin(an) * rad);
		}
		var h = new Hz { x = cx, y = cy, kind = "L14Magnet" };
		h.update = (t, dt) => { if (t > te + 2.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			for (int i = 0; i < cs.Length; i++)
			{
				double u = t - cs[i];
				// a ghost where each ball will come from (half a second), and grey lines where they will shoot out
				if (u > -0.5 && u < 0) for (int k = 0; k < n; k++) { double an = A0[i] + k * TAU / n, gx = cx + JS.cos(an) * Math.Min(r0, 600), gy = cy + JS.sin(an) * Math.Min(r0, 600); g.strokeStyle = GREY + str(0.3 + 0.5 * (1 + u / 0.5)) + ")"; g.lineWidth = 2; g.beginPath(); g.arc(gx, gy, r * 1.5, 0, TAU); g.stroke(); }
				if (u > tout - 0.45 && u < tout) { double k2 = 1 - (tout - u) / 0.45; g.strokeStyle = GREY + str(0.15 + 0.4 * k2) + ")"; g.lineWidth = 3; for (int k = 0; k < n; k++) { double an = spin(i, t) + k * TAU / n; g.beginPath(); g.moveTo(cx + JS.cos(an) * (ro + r + 8), cy + JS.sin(an) * (ro + r + 8)); g.lineTo(cx + JS.cos(an) * (ro + 60 + 140 * k2), cy + JS.sin(an) * (ro + 60 + 140 * k2)); g.stroke(); } }
				for (int k = 0; k < n; k++) { var q = ball(i, k, t); if (q != null) L14BallDraw(q.Value.x, q.Value.y, r, Math.Abs(u - tout) < 0.05); }
			}
			// the magnet: grey until it switches on, then a pink core with field lines turning
			double on = clamp((t - cs[0]) / 0.3, 0, 1) * clamp((te - t) / 0.3 + 1, 0, 1), rr2 = R * (1 + 0.12 * pulse);
			g.save(); g.strokeStyle = GREY + "0.3)"; g.lineWidth = 2; for (int k = 0; k < 3; k++) { g.beginPath(); g.arc(cx, cy, ro * (0.45 + 0.27 * k) + 6 * Math.Sin(t * 4 + k), 0, TAU); g.stroke(); }
			if (on > 0) { g.globalAlpha = on; g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; circle(cx, cy, rr2); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.8)"; g.translate(cx, cy); g.rotate(-t * 2); g.fillRect(-rr2 * 0.6, -4, rr2 * 1.2, 8); g.fillRect(-4, -rr2 * 0.6, 8, rr2 * 1.2); }
			else { g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, R, 0, TAU); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t >= cs[0] && t < te && hypot(p.x - cx, p.y - cy) < R + p.r - 2) return true;
			for (int i = 0; i < cs.Length; i++) for (int k = 0; k < n; k++) { var q = ball(i, k, t); if (q != null && hypot(p.x - q.Value.x, p.y - q.Value.y) < r + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the ball locks at the top (harmless): a lock lights when a ball drops in (at lights[i] lock i % 3) and flashes when it lets go (rel)
	public static Hz L14Locks(double t0, double t1, double[] lights, double[] rel)
	{
		var h = new Hz(); double[] X = { W / 4, W / 2, W * 3 / 4 };
		h.update = (t, dt) => { if (t > t1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.4, (t1 + 0.5 - t) / 0.4)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			double lastRel = rel.Where(r => r <= t).DefaultIfEmpty(-1e9).Max();
			for (int i = 0; i < 3; i++)
			{
				bool lit = false; for (int k = i; k < lights.Length; k += 3) if (lights[k] <= t && lights[k] > lastRel) lit = true;
				double fl = Math.Max(0, 1 - (t - lastRel) / 0.25);
				g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(X[i], 46, 26, 0, TAU); g.stroke();
				if (lit || fl > 0) { g.fillStyle = fl > 0 ? $"rgba(255,255,255,{str(fl)})" : "rgba(255,42,109,0.5)"; circle(X[i], 46, 20); }
			}
			g.font = $"800 22px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = GREY + "0.35)"; g.fillText("LOCK", W / 2, 96);
			g.restore();
		};
		return h;
	}

	// the machine around the play (grey, harmless): which part of the machine we are in
	public static Hz L14Deco(int part, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 1.0, (t1 - t) / 1.0)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.strokeStyle = GREY + str(0.1 + 0.08 * pulse) + ")"; g.fillStyle = GREY + "0.08)"; g.lineWidth = 3;
			if (part == 1)
			{
				// the top of the machine: the round top rail and the lanes the ball rolls through
				g.beginPath(); g.arc(W / 2, H + 260, 940, Math.PI * 1.08, Math.PI * 1.92); g.stroke(); g.beginPath(); g.arc(W / 2, H + 300, 900, Math.PI * 1.1, Math.PI * 1.9); g.stroke();
				for (int i = 0; i < 4; i++) { double x = W / 2 - 150 + i * 100; g.beginPath(); g.moveTo(x, 70); g.lineTo(x, 150); g.stroke(); }
			}
			else if (part == 2)
			{
				// the play field: the side lanes going down to the flippers
				g.beginPath(); g.moveTo(90, 0); g.lineTo(90, 520); g.lineTo(280, 640); g.moveTo(W - 90, 0); g.lineTo(W - 90, 520); g.lineTo(W - 280, 640); g.stroke();
			}
			else if (part == 3)
			{
				// the jackpot: arrows pointing to the middle, lit in turn with the beat
				int lit = (int)Math.Floor(BT(t) * 2) % 4;
				for (int k = 0; k < 8; k++)
				{
					double an = k * TAU / 8 + Math.PI / 8, d = 250 + 70 * ((k + lit) % 4), cx = W / 2 + JS.cos(an) * d * 1.5, cy = H / 2 + JS.sin(an) * d * 0.8;
					g.save(); g.translate(cx, cy); g.rotate(an + Math.PI); g.globalAlpha = a * ((k + lit) % 4 == 0 ? 1 : 0.45); g.beginPath(); g.moveTo(22, 0); g.lineTo(-14, -16); g.lineTo(-6, 0); g.lineTo(-14, 16); g.closePath(); g.fill(); g.stroke(); g.restore();
				}
				g.font = $"800 30px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = GREY + str(0.12 + 0.1 * pulse) + ")"; g.fillText("JACKPOT", W / 2, 40);
			}
			else if (part == 4)
			{
				// the drain: the gap at the bottom between two resting flippers
				g.beginPath(); g.moveTo(W / 2 - 330, 560); g.lineTo(W / 2 - 70, 700); g.moveTo(W / 2 + 330, 560); g.lineTo(W / 2 + 70, 700); g.stroke();
				g.beginPath(); g.moveTo(90, 0); g.lineTo(90, 420); g.lineTo(W / 2 - 330, 560); g.moveTo(W - 90, 0); g.lineTo(W - 90, 420); g.lineTo(W / 2 + 330, 560); g.stroke();
			}
			g.restore();
		};
		return h;
	}

	// big writing on the dot display (harmless): grey blinking TILT when the lights go out, cyan REPLAY at the end
	public static Hz L14Word(string s, double t0, double t1, double c, double y, bool cyan)
	{
		var bits = L14Bits(s); int n = bits.Length - 1; var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.15, (t1 - t) / 0.4)); if (a <= 0) return; double X = W / 2 - n * c / 2;
			bool on = cyan || Math.Floor((t - t0) / 0.25) % 2 == 0; g.save(); g.globalAlpha = a * (on ? 1 : 0.35);
			g.fillStyle = cyan ? CYAN : GREY + "0.5)"; if (cyan) { g.shadowColor = CYAN; g.shadowBlur = 18; }
			g.beginPath(); for (int j = 0; j < n; j++) for (int r = 0; r < 7; r++) if (bits[j][r]) g.rect(X + j * c + 2, y + r * c + 2, c - 4, c - 4); g.fill(); g.restore();
		};
		return h;
	}

	// the plunger before the drop: the giant ball sits on the spring at the right, the spring pulls back and the screen shakes more and more
	public static Hz L14Plunger(double t0, double t1, double R)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t >= t0 && t < t1) shake = Math.Max(shake, 2 + 8 * clamp((t - t0) / (t1 - t0), 0, 1)); if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double k = clamp((t - t0) / (t1 - t0), 0, 1), a = Math.Min(1, (t - t0) / 0.3); if (a <= 0) return; double x = W - R - 10, by = H - R - 40 + 40 * k;
			g.save(); g.globalAlpha = a; g.strokeStyle = GREY + "0.35)"; g.lineWidth = 3; g.beginPath(); g.moveTo(W - 2 * R - 24, 0); g.lineTo(W - 2 * R - 24, H); g.stroke();
			g.beginPath(); for (int i = 0; i <= 10; i++) { double yy = by + R + i * (H - by - R) / 10.0; g.lineTo(x + (i % 2 == 0 ? -R * 0.5 : R * 0.5), yy); } g.stroke();
			g.fillStyle = GREY + str(0.35 + 0.3 * k) + ")"; circle(x, by, R); g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L14.json) ----------
	// warnings (wn, beats) and speeds (sp) are Normal's; Easy stretches them here. An event fires at its hit minus Easy's longest warning,
	// and its hazard shows the warning only from hit - wn
	static double L14wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	// Easy keeps every other flip, pop and kick of a long attack
	static double[] L14thin(double[] a) => easy ? a.Where((v, i) => i % 2 == 0).ToArray() : a;
	static double L14sp(DodgeEv e, string k = "sp") => e.num(k) * (easy ? ezV("ezsp", EZ_SP) : 1);
	static double L14PX() => player != null ? player.x : W / 2;
	static double L14PY() => player != null ? player.y : H / 2;

	// white and shake at a part's start, fading out over dur seconds (2)
	static readonly int _k14fx = DodgeKind("L14fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = e.num("dur", 2), t0 = B(e.b); var h = new Hz { kind = "L14fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		h.draw = t => { }; h.hit = (p, t) => false; hazards.Add(h);
	});
	static readonly int _k14gloom = DodgeKind("L14gloom", e => () => { gloomT = e.num("v"); });
	static readonly int _k14deco = DodgeKind("L14deco", e => () => hazards.Add(L14Deco(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _k14word = DodgeKind("L14word", e => () => hazards.Add(L14Word(e.str("s"), B(e.num("t0")), B(e.num("t1")), e.num("c"), e.num("y"), e.flag("cyan"))));
	static readonly int _k14plunger = DodgeKind("L14plunger", e => () => hazards.Add(L14Plunger(B(e.num("t0")), B(e.num("t1")), e.num("R"))));
	// a bumper in a fixed place, popping at the beats in pops
	static readonly int _k14bumper = DodgeKind("L14bumper", e => { double wn = L14wn(e); return () =>
		hazards.Add(L14Bumper(e.num("x"), e.num("y"), e.num("R"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")), L14thin(e.nums("pops")).Select(B).ToArray(), e.num("popR"))); });
	// a bumper beside the player (off px away at angle a), one pop when it lands
	static readonly int _k14bumperP = DodgeKind("L14bumperP", e => { double wn = L14wn(e); return () =>
	{
		double R0 = e.num("R"), a = e.num("a"), off = e.num("off"), hit = e.num("hit");
		double x = clamp(L14PX() + JS.cos(a) * off, R0 + 20, W - R0 - 20), y = clamp(L14PY() + JS.sin(a) * off * 0.7, R0 + 20, H - R0 - 20);
		hazards.Add(L14Bumper(x, y, R0, B(hit - wn), B(hit), B(e.num("to")), new[] { B(hit) }, e.num("popR")));
	}; });
	// a ball from (x,y) at the player (j: a fixed turn off the aim)
	static readonly int _k14ballP = DodgeKind("L14ballP", e => { double wn = L14wn(e), sp = L14sp(e); return () =>
	{
		double x = e.num("x"), y = e.num("y"), hit = e.num("hit"), a = JS.atan2(L14PY() - y, L14PX() - x) + e.num("j");
		hazards.Add(L14Ball(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 14, B(hit - wn), B(hit), e.i("nb"), e.flag("split") && !easy));
	}; });
	// a lane: column i of n, or (aim) the player's column + d
	static readonly int _k14lane = DodgeKind("L14lane", e => { double wn = L14wn(e); return () =>
	{
		int n = e.i("n"); double hit = e.num("hit"); int i = e.flag("aim") ? (int)clamp(Math.Floor(L14PX() / (W / n)) + e.i("d"), 0, n - 1) : e.i("i");
		hazards.Add(L14Lane((i + 0.5) * W / n, W / n, B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	// a bank of drop targets: the gap at column gap (or, aim, beside the player), falling one by one from the side dir (-1 right first)
	static readonly int _k14bank = DodgeKind("L14bank", e => { double wn = L14wn(e); return () =>
	{
		int n = e.i("n"), gw = e.i("gw", 2), gap = e.i("gap"), dir = e.i("dir", 1); double hit = e.num("hit"), step = e.num("step");
		if (e.flag("aim")) { gap = (int)clamp(Math.Floor(L14PX() / (W / n)) + (L14PX() < W / 2 ? 2 : -3), 0, n - gw); dir = L14PX() < W / 2 ? -1 : 1; }
		var d = new double[n]; for (int i = 0; i < n; i++) { int k = dir > 0 ? i : n - 1 - i; d[i] = B(hit + 1 + k * step); }
		hazards.Add(L14Bank(n, gap, B(hit - wn), B(hit), d, gw));
	}; });
	static readonly int _k14flip = DodgeKind("L14flip", e => { double wn = L14wn(e), sp = L14sp(e); return () =>
		hazards.Add(L14Flipper(e.num("px"), e.num("py"), e.num("len"), e.num("a0"), e.num("a1"), B(e.b), B(e.num("b0")), B(e.num("b1")), L14thin(e.nums("flips")).Select(B).ToArray(), wn * SPB, sp, e.i("nb"), e.flag("split") && !easy)); });
	static readonly int _k14sling = DodgeKind("L14sling", e => () =>
		hazards.Add(L14Sling(e.num("x"), e.num("sd"), B(e.b), B(e.num("b0")), B(e.num("b1")), L14thin(e.nums("kicks")).Select(B).ToArray())));
	static readonly int _k14matrix = DodgeKind("L14matrix", e => { double sp = L14sp(e); return () =>
		hazards.Add(L14Matrix(e.str("s"), e.num("c"), e.num("y0"), e.num("dir"), sp, B(e.b), B(e.num("ta")), B(e.num("te")), e.num("amp"), e.num("from"))); });
	static readonly int _k14jackpot = DodgeKind("L14jackpot", e => { double k = easy ? ezV("ezsp", EZ_SP) : 1; return () =>
		hazards.Add(L14Jackpot(e.num("x"), e.num("y"), e.num("vx") * k, e.num("vy") * k, e.num("R"), B(e.b), B(e.num("tw2")), B(e.num("drain")), B(e.num("te")), e.num("shockR"), easy ? 2 : 1)); });
	// a ramp with trains of balls (trains: beats)
	static readonly int _k14ramp = DodgeKind("L14ramp", e => { double wn = L14wn(e), sp = L14sp(e); return () =>
	{
		var tr = L14thin(e.nums("trains")).Select(B).ToArray();
		hazards.Add(L14Ramp(e.num("cx"), e.num("cy"), e.num("rr"), e.num("a0"), e.num("a1"), B(e.nums("trains").Min() - wn), tr, wn * SPB, sp, easy ? Math.Max(1, e.i("nb") - 1) : e.i("nb"), e.num("gap"), e.num("r")));
	}; });
	// the magnet: cycles start at the beats cs, Easy has fewer balls
	static readonly int _k14magnet = DodgeKind("L14magnet", e => { double sp = L14sp(e), k = easy ? ezV("ezsp", EZ_SP) : 1; return () =>
		hazards.Add(L14Magnet(e.nums("cs").Select(B).ToArray(), e.num("tin") * SPB, e.num("tout") * SPB, easy ? e.i("n") - 2 : e.i("n"), e.num("r0"), e.num("ro"), e.num("w") * k, sp, e.num("R"), B(e.nums("cs").Min() - 1), B(e.num("te")), e.num("r"))); });
	static readonly int _k14locks = DodgeKind("L14locks", e => () => hazards.Add(L14Locks(B(e.num("t0")), B(e.num("t1")), e.nums("lights").Select(B).ToArray(), e.nums("rel").Select(B).ToArray())));
	// a ball shot from x,y at angle ang (degrees), speed sp, nb rebounds
	static readonly int _k14shot = DodgeKind("L14shot", e => { double wn = L14wn(e), sp = L14sp(e); return () =>
	{
		double a = e.num("ang") * Math.PI / 180; hazards.Add(L14Ball(e.num("x"), e.num("y"), JS.cos(a) * sp, JS.sin(a) * sp, e.num("r"), B(e.num("hit") - wn), B(e.num("hit")), e.i("nb")));
	}; });
	// a falling ball at x, or (aim) beside the player (off px)
	static readonly int _k14fall = DodgeKind("L14fall", e => { double wn = L14wn(e); return () =>
	{
		double hit = e.num("hit"), x = e.flag("aim") ? clamp(L14PX() + e.num("off"), 60, W - 60) : e.num("x");
		hazards.Add(L14Fall(x, B(hit - wn), B(hit), e.num("dur"), e.num("r")));
	}; });
}
