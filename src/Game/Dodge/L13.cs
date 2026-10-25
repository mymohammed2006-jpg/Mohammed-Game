using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L13: "A beat further" (Neon Beats Music), the whole song. A rocket's journey further and further:
// part 1 the launch (pad lights, engine flames, tower arms, fuel vents, falling stages), part 2 the asteroid belt
// (satellites, splitting asteroids, solar panel walls, comets, the meteor shower that freezes), part 3 light speed
// (warp rays, tunnel frames) and the boss, the black hole (spiral arms, pulled rings, debris, polar jets).
// Every hazard here is a pure function of the song time (no state moved by dt), so its future can be read ahead.
// The data is data/dodge_L13.json (made by tools/level13/gen.py); the kinds are at the end of this file.

public static partial class G
{
	static double L13k(double t, double a, double b) => clamp((t - a) / (b - a), 0, 1);
	static double L13eo(double k) => 1 - JS.pow(1 - k, 3);
	static string L13P(double a) => $"rgba(255,42,109,{str(a)})";

	// a pad light: a ring that fills, then a pink disc on the beat
	public static Hz L13Light(double x, double y, double r, double tw, double ta, double te)
	{
		var h = new Hz { x = x, y = y, r = r };
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta) { double k = L13k(t, tw, ta); g.strokeStyle = L13P(0.35 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); g.fillStyle = L13P(0.05 + 0.18 * k); circle(x, y, r * k); return; }
			double f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.2); g.save(); g.globalAlpha = f; g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK;
			circle(x, y, r * (t < te ? 1 + 0.08 * pulse : f)); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.3)"; circle(x, y, r * 0.45 * f); g.restore();
		};
		h.hit = (p, t) => t >= ta && t < te && hypot(p.x - x, p.y - y) < r + p.r - 2;
		return h;
	}

	// an engine flame: a column of fire from the top (or the bottom) of the screen
	public static Hz L13Flame(double x, double w, bool top, double tw, double ta, double te)
	{
		var h = new Hz { x = x, y = top ? 0 : H };
		double len(double t) => H * L13k(t, ta, ta + 0.09);
		h.update = (t, dt) => { if (t > te + 0.22) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; g.save(); if (!top) { g.translate(0, H); g.scale(1, -1); }
			if (t < ta)
			{
				double k = L13k(t, tw, ta); g.fillStyle = L13P(0.04 + 0.12 * k); g.fillRect(x - w / 2, 0, w, H);
				g.strokeStyle = L13P(0.3 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.moveTo(x - w / 2, 0); g.lineTo(x - w / 2, H * k); g.moveTo(x + w / 2, 0); g.lineTo(x + w / 2, H * k); g.stroke();
			}
			else
			{
				double f = t < te ? 1 : clamp(1 - (t - te) / 0.22, 0, 1), L = len(t), ww = w * f * (1 + 0.1 * pulse);
				g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; g.beginPath(); g.moveTo(x - ww / 2, 0);
				for (int i = 1; i <= 12; i++) { double yy = L * i / 12.0, wob = JS.sin(yy * 0.045 - t * 26) * ww * 0.07; g.lineTo(x - ww / 2 * (1 - 0.3 * i / 12.0) + wob, yy); }
				g.lineTo(x, L + ww * 0.3);
				for (int i = 12; i >= 1; i--) { double yy = L * i / 12.0, wob = JS.sin(yy * 0.045 - t * 26 + 1.7) * ww * 0.07; g.lineTo(x + ww / 2 * (1 - 0.3 * i / 12.0) + wob, yy); }
				g.lineTo(x + ww / 2, 0); g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.3)"; g.fillRect(x - ww * 0.12, 0, ww * 0.24, L * 0.85);
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double yy = top ? p.y : H - p.y; return Math.Abs(p.x - x) < w * 0.4 + p.r - 1 && yy < len(t) + w * 0.15; };
		return h;
	}

	// a spark (with gravity): position from the time, like every hazard here
	public static Hz L13Spark(double x0, double y0, double vx, double vy, double t0, double gy = 0, double r = 8)
	{
		var h = new Hz { x = x0, y = y0, r = r };
		(double x, double y) pos(double t) { double d = Math.Max(0, t - t0); return (x0 + vx * d, y0 + vy * d + 0.5 * gy * d * d); }
		h.update = (t, dt) => { var q = pos(t); h.x = q.x; h.y = q.y; if (q.x < -60 || q.x > W + 60 || q.y > H + 60 || q.y < -500) h.dead = true; };
		h.draw = t =>
		{
			var q = pos(t); double s = r * (1 + 0.15 * pulse); g.save(); g.translate(q.x, q.y); g.rotate(t * 7); g.fillStyle = L13P(0.25); circle(0, 0, s * 1.7);
			g.fillStyle = PINK; g.beginPath(); for (int i = 0; i < 8; i++) { double rr2 = i % 2 != 0 ? s * 0.4 : s * 1.3, an = i * Math.PI / 4; if (i != 0) g.lineTo(JS.cos(an) * rr2, JS.sin(an) * rr2); else g.moveTo(rr2, 0); } g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; var q = pos(t); double dx = p.x - q.x, dy = p.y - q.y, rr2 = r + p.r - 2; return dx * dx + dy * dy < rr2 * rr2; };
		return h;
	}

	// a launch tower arm: lies along the wall, swings out on the beat, holds, then folds back
	public static Hz L13Arm(double px, double py, double len, double w, double a0, double a1, double tw, double ta, double te, double sw)
	{
		var h = new Hz { x = px, y = py };
		double ang(double t) => t < ta ? a0 : t < ta + sw ? a0 + (a1 - a0) * L13eo(L13k(t, ta, ta + sw)) : t < te ? a1 : a1 + (a0 - a1) * JS.pow(L13k(t, te, te + sw), 2);
		h.update = (t, dt) => { if (t > te + sw + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; g.save(); g.translate(px, py);
			if (t < ta)
			{
				double k = L13k(t, tw, ta); g.fillStyle = L13P(0.03 + 0.08 * k); g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, len, Math.Min(a0, a1), Math.Max(a0, a1)); g.closePath(); g.fill();
				g.save(); g.rotate(a1); g.strokeStyle = L13P(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(0, -w / 2, len, w); g.setLineDash(new double[0]); g.restore();
			}
			double f = t < te + sw ? 1 : Math.Max(0, 1 - (t - te - sw) / 0.2);
			g.rotate(ang(t)); g.globalAlpha = t < ta ? 0.4 : f;
			if (t >= ta) { g.shadowColor = PINK; g.shadowBlur = 18; }
			g.fillStyle = t >= ta && t - ta < 0.07 ? "#fff" : t < ta ? GREY + "0.5)" : PINK; g.fillRect(0, -w / 2, len, w); g.shadowBlur = 0;
			g.strokeStyle = "rgba(0,0,0,0.45)"; g.lineWidth = 3; g.beginPath(); for (double u = 0; u + w <= len; u += w) { g.moveTo(u, -w / 2 + 5); g.lineTo(u + w, w / 2 - 5); g.moveTo(u, w / 2 - 5); g.lineTo(u + w, -w / 2 + 5); } g.stroke();
			g.globalAlpha = 1; g.fillStyle = GREY + "0.8)"; circle(0, 0, w * 0.55); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te + sw * 0.8) return false; double a = ang(t); return obbHit(p.x, p.y, p.r - 1, px + JS.cos(a) * len / 2, py + JS.sin(a) * len / 2, len / 2, w / 2, a); };
		return h;
	}

	// a fuel pipe across the screen: every vent blows a cone of steam on both sides, except two (the way through)
	public static Hz L13Vent(double c, bool vert, int gap, double L, double tw, double ta, double te)
	{
		int n = vert ? 9 : 16; double ext = vert ? H : W;
		var h = new Hz { x = vert ? c : W / 2, y = vert ? H / 2 : c };
		double pl(double t) => L * (t < te ? L13eo(L13k(t, ta, ta + 0.12)) : Math.Max(0, 1 - (t - te) / 0.2));
		bool on(int i) => i != gap && i != gap + 1;
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; g.save(); if (vert) { g.translate(c, 0); g.rotate(Math.PI / 2); } else g.translate(0, c);
			double a = Math.Min(1, Math.Min(L13k(t, tw, tw + 0.2), 1 - L13k(t, te + 0.1, te + 0.3)));
			g.globalAlpha = a; g.fillStyle = GREY + "0.35)"; g.fillRect(0, -7, ext, 14); g.globalAlpha = 1;
			for (int i = 0; i < n; i++)
			{
				double u = 40 + 80 * i; g.fillStyle = GREY + str(0.55 * a) + ")"; g.fillRect(u - 9, -11, 18, 22); if (!on(i)) continue;
				if (t < ta) { double k = L13k(t, tw, ta); g.strokeStyle = L13P(0.25 + 0.5 * k); g.lineWidth = 2; foreach (var sd in new double[] { -1, 1 }) { g.beginPath(); g.moveTo(u - 6, sd * 11); g.lineTo(u - 36, sd * L); g.moveTo(u + 6, sd * 11); g.lineTo(u + 36, sd * L); g.stroke(); } continue; }
				double q = pl(t); if (q <= 1) continue; double wb = 36 * q / L * (1 + 0.1 * pulse);
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK;
				foreach (var sd in new double[] { -1, 1 }) { g.beginPath(); g.moveTo(u - 6, sd * 8); g.lineTo(u - wb, sd * q); g.quadraticCurveTo(u, sd * (q + 22), u + wb, sd * q); g.lineTo(u + 6, sd * 8); g.closePath(); g.fill(); }
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double q = pl(t), u = vert ? p.y : p.x, v = Math.Abs(vert ? p.x - c : p.y - c); if (v > q + p.r - 1) return false;
			for (int i = 0; i < n; i++) { if (!on(i)) continue; double cu = 40 + 80 * i; if (Math.Abs(u - cu) < 6 + 30 * Math.Min(v, L) / L + p.r - 1) return true; }
			return false;
		};
		return h;
	}

	// a wall that slides in from one edge with a slow wavy edge (the warp tunnel walls, the burning air of the shower)
	public static Hz L13Wall(string side, double d, double amp, double tw, double ta, double te, double wv)
	{
		bool vert = side == "L" || side == "R"; double ext = vert ? H : W, far = vert ? W : H; bool flip = side == "R" || side == "D";
		var h = new Hz { x = W / 2, y = H / 2 };
		double q(double t) => t < te ? L13eo(L13k(t, ta, ta + 0.35)) : 1 - L13k(t, te, te + 0.35);
		double dep(double u, double t) => q(t) * (d + amp * Math.Sin(u * 0.011 + t * wv));
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		void edge(double t, Func<double, double> f)
		{
			g.save(); if (vert) { g.translate(flip ? W : 0, 0); g.scale(flip ? -1 : 1, 1); g.translate(0, H); g.rotate(-Math.PI / 2); } else { g.translate(0, flip ? H : 0); g.scale(1, flip ? -1 : 1); }
			g.beginPath(); g.moveTo(0, -4); for (double u = 0; u <= ext + 0.1; u += 16) g.lineTo(u, f(vert ? H - u : u)); g.lineTo(ext, -4); g.closePath();
		}
		h.draw = t =>
		{
			if (t < tw || t > te + 0.4) return;
			if (t < ta) { double k = L13k(t, tw, ta); edge(t, u => d + amp * Math.Sin(u * 0.011 + t * wv)); g.strokeStyle = L13P(0.2 + 0.5 * k); g.lineWidth = 2; g.stroke(); g.fillStyle = L13P(0.06 + 0.08 * k); g.fill(); g.restore(); return; }
			edge(t, u => dep(u, t)); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.35)"; g.lineWidth = 3; for (int i = 0; i < 6; i++) { double u = ((i * 230 + t * 900) % (ext + 200)) - 100, dd = q(t) * d * 0.45; g.beginPath(); g.moveTo(u, dd); g.lineTo(u + 120, dd); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te + 0.3) return false; double u = vert ? p.y : p.x, v = vert ? (flip ? W - p.x : p.x) : (flip ? H - p.y : p.y);
			return v < dep(u, t) + p.r - 2;
		};
		return h;
	}

	// a rocket stage falls along a line, crashes on the beat and breaks into n pieces
	public static Hz L13Stage(double x0, double y0, double x1, double y1, double tw, double ta, int n, double sp, double r0)
	{
		var h = new Hz { x = x1, y = y1 }; double ang = JS.atan2(y1 - y0, x1 - x0) - Math.PI / 2; bool boom = false; double life = 1700 / sp;
		(double x, double y) pos(double t) { double e = JS.pow(L13k(t, tw, ta), 2); return (x0 + (x1 - x0) * e, y0 + (y1 - y0) * e); }
		h.update = (t, dt) =>
		{
			if (!boom && t >= ta) { boom = true; burst(x1, y1, PINK, 16, 320); burst(x1, y1, "#fff", 8, 240); shake = Math.Max(shake, 8); }
			if (t > ta + life) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = L13k(t, tw, ta); g.strokeStyle = L13P(0.15 + 0.4 * k); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = L13P(0.05 + 0.15 * k); circle(x1, y1, 64); g.strokeStyle = L13P(0.8); g.lineWidth = 2; g.beginPath(); g.arc(x1, y1, 64 * (1.6 - 0.6 * k), 0, TAU); g.stroke();
				var q = pos(t); g.save(); g.translate(q.x, q.y); g.rotate(ang); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; rr(-30, -85, 60, 170, 14); g.fill(); g.shadowBlur = 0;
				g.beginPath(); g.moveTo(-30, -60); g.lineTo(-52, -92); g.lineTo(-30, -84); g.moveTo(30, -60); g.lineTo(52, -92); g.lineTo(30, -84); g.fill();
				g.fillStyle = "rgba(0,0,0,0.4)"; g.fillRect(-30, -20, 60, 8); g.fillRect(-30, 30, 60, 8); g.restore(); return;
			}
			double d = t - ta;
			if (d < 0.25) { g.globalAlpha = 1 - d / 0.25; g.fillStyle = d < 0.08 ? PINKW : PINK; circle(x1, y1, 64 + 70 * d); g.globalAlpha = 1; }
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
			for (int i = 0; i < n; i++) { double a = r0 + i * TAU / n, fx = x1 + JS.cos(a) * sp * d, fy = y1 + JS.sin(a) * sp * d; g.save(); g.translate(fx, fy); g.rotate(a + d * 9); double s = 20 * (1 + 0.12 * pulse); g.beginPath(); g.moveTo(s, 0); g.lineTo(-s * 0.7, s * 0.7); g.lineTo(-s * 0.5, -s * 0.6); g.closePath(); g.fill(); g.restore(); }
			g.shadowBlur = 0;
		};
		h.hit = (p, t) =>
		{
			if (t < ta) { var q = pos(t); return obbHit(p.x, p.y, p.r - 1, q.x, q.y, 30, 85, ang); }
			double d = t - ta; if (d < 0.12 && hypot(p.x - x1, p.y - y1) < 62 + p.r) return true;
			for (int i = 0; i < n; i++) { double a = r0 + i * TAU / n; if (hypot(p.x - x1 - JS.cos(a) * sp * d, p.y - y1 - JS.sin(a) * sp * d) < 13 + p.r - 1) return true; }
			return false;
		};
		return h;
	}

	// decoration: the launch pad (ground and two grey towers) that sinks away at lift-off, then speed lines
	public static Hz L13Pad(double t0, double tl, double tl2, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(L13k(t, t0, t0 + 1), 1 - L13k(t, t1 - 1, t1)), sink = 700 * JS.pow(L13k(t, tl, tl2), 2); g.save(); g.globalAlpha = a;
			if (sink < 700)
			{
				g.translate(0, sink); g.strokeStyle = GREY + str(0.16 + 0.1 * pulse) + ")"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, H - 40); g.lineTo(W, H - 40); g.stroke();
				foreach (var tx in new double[] { 60, W - 60 })
				{
					g.strokeStyle = GREY + "0.13)"; g.beginPath(); g.moveTo(tx - 22, H - 40); g.lineTo(tx - 22, 40); g.moveTo(tx + 22, H - 40); g.lineTo(tx + 22, 40);
					for (double y = H - 40; y > 60; y -= 44) { g.moveTo(tx - 22, y); g.lineTo(tx + 22, y - 44); g.moveTo(tx + 22, y); g.lineTo(tx - 22, y - 44); }
					g.stroke();
				}
				g.translate(0, -sink);
			}
			if (t > tl)
			{
				double sp = 300 + 1400 * L13k(t, tl, tl2); g.strokeStyle = GREY + "0.1)"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 14; i++) { double x = 45 + i * 92 + (i % 3) * 17, y = ((t - tl) * sp * (0.7 + (i % 4) * 0.12) + i * 211) % (H + 160) - 80; g.moveTo(x, y); g.lineTo(x, y + 40 + sp * 0.04); }
				g.stroke();
			}
			g.restore();
		};
		return h;
	}

	// decoration: the Earth going away at the bottom, then a faint belt of grey rocks
	public static Hz L13Earth(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(L13k(t, t0, t0 + 1.5), 1 - L13k(t, t1 - 1.5, t1)), u = L13k(t, t0, t1); g.save(); g.globalAlpha = a;
			double r = 1100 - 500 * u, cy = H + r - 120 + 260 * u; g.fillStyle = GREY + "0.05)"; circle(W * 0.3, cy, r); g.strokeStyle = GREY + str(0.14 + 0.08 * pulse) + ")"; g.lineWidth = 3; g.beginPath(); g.arc(W * 0.3, cy, r, 0, TAU); g.stroke();
			g.strokeStyle = GREY + "0.07)"; g.lineWidth = 10; g.beginPath(); g.arc(W * 0.3, cy, r + 14, 0, TAU); g.stroke();
			g.fillStyle = GREY + "0.09)"; for (int i = 0; i < 18; i++) { double x = ((i * 157 - (t - t0) * 40 * (1 + i % 3)) % (W + 100) + W + 100) % (W + 100) - 50, y = 70 + (i * 97) % 120; circle(x, y, 3 + i % 4); }
			g.restore();
		};
		return h;
	}

	// a satellite drifting along a path: the grey body is harmless, its two pink solar panels turn and hurt
	public static Hz L13Sat(Func<double, (double x, double y)> path, double tw, double t0, double t1, double a0, double w, double pl, double pw)
	{
		var h = new Hz();
		double u(double t) => L13k(t, t0, t1); double an(double t) => a0 + w * Math.Max(0, t - t0);
		h.update = (t, dt) => { var q = path(u(t)); h.x = q.x; h.y = q.y; if (t > t1 + 0.1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; var q = path(u(t)); bool live = t >= t0; double a = an(t);
			if (!live) { double k = L13k(t, tw, t0); g.fillStyle = L13P(0.12 + 0.2 * k); for (int i = 0; i <= 30; i++) { var p2 = path(i / 30.0); circle(p2.x, p2.y, 2.5); } }
			g.save(); g.translate(q.x, q.y); g.rotate(a); g.globalAlpha = live ? 1 : 0.4;
			foreach (var sd in new double[] { -1, 1 })
			{
				double x0 = sd > 0 ? 26 : -26 - pl; if (live) { g.shadowColor = PINK; g.shadowBlur = 14; } g.fillStyle = live ? PINK : GREY + "0.5)"; g.fillRect(x0, -pw / 2, pl, pw); g.shadowBlur = 0;
				g.strokeStyle = "rgba(0,0,0,0.45)"; g.lineWidth = 2; g.beginPath(); for (double xx = x0 + pl / 5; xx < x0 + pl - 1; xx += pl / 5) { g.moveTo(xx, -pw / 2); g.lineTo(xx, pw / 2); } g.moveTo(x0, 0); g.lineTo(x0 + pl, 0); g.stroke();
			}
			g.fillStyle = GREY + "0.85)"; g.fillRect(-20, -20, 40, 40); g.strokeStyle = GREY + "0.85)"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, -20); g.lineTo(0, -38); g.stroke(); g.beginPath(); g.arc(0, -42, 9, Math.PI, TAU); g.stroke();
			g.globalAlpha = 1; g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= t1) return false; var q = path(u(t)); double a = an(t);
			foreach (var sd in new double[] { -1, 1 }) { double cx = q.x + JS.cos(a) * sd * (26 + pl / 2), cy = q.y + JS.sin(a) * sd * (26 + pl / 2); if (obbHit(p.x, p.y, p.r - 1, cx, cy, pl / 2, pw / 2, a)) return true; }
			return false;
		};
		return h;
	}

	// an asteroid: flies straight, splits in two on one beat, and each half in two again later
	public static Hz L13Rock(double x0, double y0, double vx, double vy, double tw, double t0, double r, double ts1, double ts2, double sep, int seed)
	{
		var h = new Hz { x = x0, y = y0, r = r }; var Rv = mulberry32(seed); var vs = new double[9]; for (int i = 0; i < 9; i++) vs[i] = 0.78 + Rv() * 0.3;
		double v = hypot(vx, vy), ux = vx / v, uy = vy / v, nx = -uy, ny = ux, life = (W + H + 600) / v;
		List<(double x, double y, double r)> parts(double t)
		{
			double d = t - t0, bx = x0 + vx * d, by = y0 + vy * d; var L = new List<(double, double, double)>();
			if (t < ts1) { L.Add((bx, by, r)); return L; }
			double s1 = sep * (t - ts1);
			foreach (var a in new double[] { -1, 1 })
			{
				if (t < ts2) { L.Add((bx + nx * a * s1, by + ny * a * s1, r * 0.72)); continue; }
				double s2 = sep * (t - ts2); foreach (var b in new double[] { -1, 1 }) L.Add((bx + nx * a * s1 + ux * b * s2, by + ny * a * s1 + uy * b * s2, r * 0.5));
			}
			return L;
		}
		h.update = (t, dt) => { if (t > t0 + life) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0)
			{
				double k = L13k(t, tw, t0), ex = clamp(x0, 24, W - 24), ey = clamp(y0, 24, H - 24); g.save(); g.translate(ex, ey); g.rotate(JS.atan2(vy, vx)); g.fillStyle = L13P(0.25 + 0.55 * k);
				g.beginPath(); g.moveTo(22, 0); g.lineTo(-10, -16); g.lineTo(-10, 16); g.closePath(); g.fill(); g.fillStyle = L13P(0.05 + 0.1 * k); g.fillRect(-10, -r, 260 * k, 2 * r); g.restore(); return;
			}
			bool fl = (t >= ts1 && t - ts1 < 0.07) || (t >= ts2 && t - ts2 < 0.07); int j = 0;
			foreach (var q in parts(t))
			{
				g.save(); g.translate(q.x, q.y); g.rotate((t - t0) * (j % 2 == 0 ? 1.3 : -1.1) + j); double s = q.r * (1 + 0.06 * pulse);
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = fl ? PINKW : PINK; g.beginPath(); for (int i = 0; i < 9; i++) { double a = i * TAU / 9; if (i != 0) g.lineTo(JS.cos(a) * s * vs[i], JS.sin(a) * s * vs[i]); else g.moveTo(s * vs[0], 0); } g.closePath(); g.fill();
				g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.35)"; circle(s * 0.25, -s * 0.2, s * 0.22); circle(-s * 0.3, s * 0.25, s * 0.14); g.restore(); j++;
			}
		};
		h.hit = (p, t) => { if (t < t0) return false; foreach (var q in parts(t)) if (hypot(p.x - q.x, p.y - q.y) < q.r * 0.82 + p.r - 1) return true; return false; };
		return h;
	}

	// a wall of solar panels crossing the screen with one panel missing
	public static Hz L13Panel(string dir, int nT, int gap, double th, double sp, double tw, double ta)
	{
		bool vert = dir == "L" || dir == "R"; double sg = (dir == "L" || dir == "U") ? -1 : 1, ext = vert ? W : H, len = vert ? H : W, ts = len / nT;
		double st = sg < 0 ? ext + th / 2 : -th / 2; double pos(double t) => st + sg * sp * (t - ta);
		var h = new Hz();
		h.update = (t, dt) => { double p = pos(t); if (t > ta && (p < -th || p > ext + th)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; g.save();
			if (t < ta)
			{
				double k = L13k(t, tw, ta), e = 16, edge = sg < 0 ? ext - e : 0;
				for (int i = 0; i < nT; i++) { if (i == gap) continue; g.fillStyle = L13P(0.2 + 0.4 * k); if (vert) g.fillRect(edge, i * ts + 3, e, ts - 6); else g.fillRect(i * ts + 3, edge, ts - 6, e); }
				g.restore(); return;
			}
			double p = pos(t); g.shadowColor = PINK; g.shadowBlur = 16;
			for (int i = 0; i < nT; i++)
			{
				if (i == gap) continue; double a = i * ts + 3, b = ts - 6; g.fillStyle = PINK;
				if (vert) g.fillRect(p - th / 2, a, th, b); else g.fillRect(a, p - th / 2, b, th);
			}
			g.shadowBlur = 0; g.strokeStyle = "rgba(0,0,0,0.4)"; g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < nT; i++) { if (i == gap) continue; for (int c = 1; c < 4; c++) { double a = i * ts + c * ts / 4; if (vert) { g.moveTo(p - th / 2, a); g.lineTo(p + th / 2, a); } else { g.moveTo(a, p - th / 2); g.lineTo(a, p + th / 2); } } }
			if (vert) { g.moveTo(p, 0); g.lineTo(p, H); } else { g.moveTo(0, p); g.lineTo(W, p); }
			g.stroke(); g.restore();
		};
		h.hit = (pl, t) =>
		{
			if (t < ta) return false; double p = pos(t), a = gap * ts + 3, b = (gap + 1) * ts - 3;
			if (vert) { if (Math.Abs(pl.x - p) > th / 2 + pl.r - 1) return false; return pl.y < a + pl.r || pl.y > b - pl.r; }
			if (Math.Abs(pl.y - p) > th / 2 + pl.r - 1) return false; return pl.x < a + pl.r || pl.x > b - pl.r;
		};
		return h;
	}

	// a comet: a head with a long tail along a curved path (shown dotted first)
	public static Hz L13Comet(Func<double, (double x, double y)> path, double tw, double t0, double t1, double tail, double r)
	{
		var h = new Hz(); const int NS = 18;
		double u(double t) => (t - t0) / (t1 - t0);
		h.update = (t, dt) => { if (u(t) - tail > 1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = L13k(t, tw, t0); g.fillStyle = L13P(0.12 + 0.3 * k); for (int i = 0; i <= 40; i++) { var q = path(i / 40.0); circle(q.x, q.y, 2.5); } return; }
			double uh = u(t); g.shadowColor = PINK; g.shadowBlur = 14;
			for (int j = NS; j >= 0; j--) { double uj = uh - tail * j / NS; if (uj < 0 || uj > 1) continue; var q = path(uj); double s = r * (1 - 0.85 * j / NS) * (1 + 0.12 * pulse); g.fillStyle = j == 0 ? "#fff" : L13P(1 - 0.6 * j / NS); circle(q.x, q.y, s); }
			g.shadowBlur = 0; if (uh <= 1) { var q = path(uh); g.fillStyle = PINK; circle(q.x, q.y, r * 0.7); }
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double uh = u(t);
			for (int j = 0; j <= NS; j++) { double uj = uh - tail * j / NS; if (uj < 0 || uj > 1) continue; var q = path(uj); if (hypot(p.x - q.x, p.y - q.y) < r * (1 - 0.85 * j / NS) * 0.85 + p.r - 1) return true; }
			return false;
		};
		return h;
	}

	// a meteor streak; at tf every meteor freezes in place (the stop before the drop), at tx it shatters
	public static Hz L13Meteor(double x0, double y0, double dx, double dy, double sp, double tw, double t0, double len, double tf, double tx)
	{
		var h = new Hz { x = x0, y = y0 }; bool gone = false;
		(double x, double y) head(double t) { double d = Math.Max(0, Math.Min(t, tf) - t0) * sp; return (x0 + dx * d, y0 + dy * d); }
		double ln(double t) => Math.Min(len, Math.Max(0, Math.Min(t, tf) - t0) * sp);
		h.update = (t, dt) =>
		{
			var q = head(t); h.x = q.x; h.y = q.y;
			if (t >= tx && !gone) { gone = true; if (q.x > -20 && q.x < W + 20 && q.y > -20 && q.y < H + 20) burst(q.x, q.y, "#fff", 4, 200); h.dead = true; }
			if (t < tf && (q.y - ln(t) * dy > H + 40 || q.x - ln(t) * dx < -60)) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = L13k(t, tw, t0); g.strokeStyle = L13P(0.12 + 0.35 * k); g.lineWidth = 2; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x0 + dx * 240 * k, y0 + dy * 240 * k); g.stroke(); return; }
			var q = head(t); double l = ln(t); bool fz = t >= tf;
			g.lineCap = "round"; g.strokeStyle = fz ? "#ffc4d4" : PINK; g.shadowColor = PINK; g.shadowBlur = fz ? 6 : 12; g.lineWidth = 8 * (1 + 0.15 * pulse);
			g.beginPath(); g.moveTo(q.x, q.y); g.lineTo(q.x - dx * l, q.y - dy * l); g.stroke(); g.shadowBlur = 0;
			if (fz) { g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 2; g.beginPath(); g.moveTo(q.x, q.y); g.lineTo(q.x - dx * l, q.y - dy * l); g.stroke(); }
			g.lineCap = "butt"; g.fillStyle = "#fff"; circle(q.x, q.y, 4);
		};
		h.hit = (p, t) => { if (t < t0 || t >= tx) return false; var q = head(t); double l = ln(t); return segDist(p.x, p.y, new[] { q.x, q.y }, new[] { q.x - dx * l, q.y - dy * l }) < 4 + p.r - 1; };
		return h;
	}

	// decoration: grey lines pulled into the middle (the warp charging up)
	public static Hz L13Charge(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double k = L13k(t, t0, t1); g.strokeStyle = GREY + str(0.08 + 0.25 * k) + ")"; g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < 28; i++) { double a = i * TAU / 28 + 0.1, r1 = 760 - ((t - t0) * (600 + 900 * k) + i * 53) % 700, r2 = r1 + 50 + 120 * k; if (r1 < 30) continue; g.moveTo(W / 2 + JS.cos(a) * r1, H / 2 + JS.sin(a) * r1); g.lineTo(W / 2 + JS.cos(a) * r2, H / 2 + JS.sin(a) * r2); }
			g.stroke(); g.fillStyle = GREY + str(0.2 + 0.5 * k) + ")"; circle(W / 2, H / 2, 6 + 10 * k * (1 + pulse));
		};
		return h;
	}

	// a ring of warp rays leaving the vanishing point faster and faster (some rays missing: the way through)
	public static Hz L13Warp(double cx, double cy, int n, double a0, int g0, int gn, double t0, double v0, double acc, double w = 7, double tw = 1e9)
	{
		var h = new Hz { x = cx, y = cy }; var A = new List<double>();
		for (int i = 0; i < n; i++) { int j = ((i - g0) % n + n) % n; if (j < gn) continue; A.Add(a0 + i * TAU / n); }
		double rad(double t) { double d = Math.Max(0, t - t0); return 20 + v0 * d + 0.5 * acc * d * d; }
		double ln(double t) => 14 + (v0 + acc * Math.Max(0, t - t0)) * 0.09;
		h.update = (t, dt) => { if (rad(t) - ln(t) > 1500) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0)
			{   // the path of each ray, shown before it leaves
				if (t < tw) return; double k = L13k(t, tw, t0); g.save(); g.strokeStyle = L13P(0.12 + 0.38 * k); g.lineWidth = 1.5 + 1.5 * k; g.setLineDash(new double[] { 14, 10 });
				g.beginPath(); foreach (var a in A) { double c = JS.cos(a), s = JS.sin(a); g.moveTo(cx + c * 20, cy + s * 20); g.lineTo(cx + c * 1500, cy + s * 1500); } g.stroke(); g.setLineDash(new double[0]); g.restore(); return;
			}
			double r = rad(t), l = ln(t); g.save(); g.globalAlpha = L13k(t, t0, t0 + 0.12); g.lineCap = "round"; g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; g.lineWidth = w * (1 + 0.15 * pulse);
			g.beginPath(); foreach (var a in A) { double c = JS.cos(a), s = JS.sin(a); g.moveTo(cx + c * Math.Max(0, r - l), cy + s * Math.Max(0, r - l)); g.lineTo(cx + c * r, cy + s * r); } g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double r = rad(t), l = ln(t), dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy); if (d > r + p.r || d < r - l - p.r) return false;
			double pa = JS.atan2(dy, dx);
			foreach (var a in A) { double da = pa - a; da = JS.atan2(JS.sin(da), JS.cos(da)); if (Math.Abs(da) > Math.PI / 2) continue; double along = d * JS.cos(da), perp = Math.Abs(d * JS.sin(da)); if (perp < w / 2 + p.r - 1 && along > r - l - 2 && along < r + 2) return true; }
			return false;
		};
		return h;
	}

	// a tunnel frame: a rectangle growing out of the vanishing point, one side open in the middle
	public static Hz L13Frame(double cx, double cy, double t0, double v0, double acc, int side, double gapF, double tw)
	{
		var h = new Hz { x = cx, y = cy };
		double s(double t) { double d = Math.Max(0, t - t0); return 12 + v0 * d + 0.5 * acc * d * d; }
		h.update = (t, dt) => { if (s(t) > 900) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = L13k(t, tw, t0); g.fillStyle = L13P(0.15 + 0.45 * k); double gx = side == 1 ? 1 : side == 3 ? -1 : 0, gy = side == 2 ? 1 : side == 0 ? -1 : 0; circle(cx + gx * 40, cy + gy * 26, 5 + 4 * k); return; }
			double ss = s(t), hw = ss * 1.78, hh = ss, th = 6 + ss * 0.05; g.save(); g.globalAlpha = L13k(t, t0, t0 + 0.12); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
			for (int sd = 0; sd < 4; sd++)
			{
				bool hor = sd % 2 == 0; double half = hor ? hw : hh, gp = sd == side ? half * gapF : 0;
				foreach (var e in new double[] { -1, 1 })
				{
					double a0 = gp, a1 = half + th / 2; if (a1 <= a0) continue;
					if (hor) { double y = sd == 0 ? cy - hh : cy + hh; double xa = cx + e * a0, xb = cx + e * a1; g.fillRect(Math.Min(xa, xb), y - th / 2, Math.Abs(xb - xa), th); }
					else { double x = sd == 1 ? cx + hw : cx - hw; double ya = cy + e * a0, yb = cy + e * a1; g.fillRect(x - th / 2, Math.Min(ya, yb), th, Math.Abs(yb - ya)); }
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double ss = s(t), hw = ss * 1.78, hh = ss, th = 6 + ss * 0.05, dx = p.x - cx, dy = p.y - cy, m = th / 2 + p.r - 1;
			if (Math.Abs(dy) <= hh + m && Math.Abs(Math.Abs(dx) - hw) < m) { int sd = dx > 0 ? 1 : 3; if (!(sd == side && Math.Abs(dy) < hh * gapF - p.r)) return true; }
			if (Math.Abs(dx) <= hw + m && Math.Abs(Math.Abs(dy) - hh) < m) { int sd = dy > 0 ? 2 : 0; if (!(sd == side && Math.Abs(dx) < hw * gapF - p.r)) return true; }
			return false;
		};
		return h;
	}

	// decoration: the warp tunnel (grey frames flying out of the vanishing point)
	public static Hz L13WarpBG(double t0, double t1, Func<double, (double x, double y)> vp)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(L13k(t, t0, t0 + 0.3), 1 - L13k(t, t1 - 1, t1)); var c = vp(t); g.save(); g.globalAlpha = a; g.strokeStyle = GREY + str(0.07 + 0.06 * pulse) + ")"; g.lineWidth = 2;
			for (int i = 0; i < 6; i++) { double u = (((t - t0) * 0.9 + i / 6.0) % 1), s = 20 + 900 * u * u; g.strokeRect(c.x - s * 1.78, c.y - s, s * 3.56, s * 2); }
			g.beginPath(); foreach (var (x, y) in new[] { (0.0, 0.0), (W, 0.0), (0.0, H), (W, H) }) { g.moveTo(c.x, c.y); g.lineTo(x, y); } g.stroke();
			g.fillStyle = L13P(0.35 + 0.4 * pulse); circle(c.x, c.y, 8 + 5 * pulse); g.restore();
		};
		return h;
	}

	// the boss: the black hole. Two spiral arms turn round it (a turn moves the arms in or out), a ring of matter spins at its edge
	public static Hz L13Hole(double ts, double ta, double tc, double tx, (double b, double w)[] spin, Func<double, (double x, double y)> cpos)
	{
		const double R0 = 120, K = 0.22; double TH = easy ? 13 : 16, SMAX = Math.Log(1050 / R0) / K, CK = 1 / Math.Sqrt(1 + K * K); bool ex = false;
		double phi(double t)
		{
			double a = 0;
			for (int i = 0; i < spin.Length; i++) { double s0 = B(spin[i].b), s1 = i + 1 < spin.Length ? B(spin[i + 1].b) : 1e9; if (t <= s0) break; a += spin[i].w * (Math.Min(t, s1) - s0); }
			return a;
		}
		double smax(int i, double t) { double grow = L13k(t, ta - 3 * SPB, ta); double shr = 1 - L13k(t, tc, tc + 4 * SPB); return SMAX * L13eo(grow) * shr; }
		// the two arms take turns with the beat: arm 0 burns on beats 0-1 of each bar, arm 1 on beats 2-3; the resting arm glows faint (brighter the beat before its turn)
		int turn(double t) => (int)(((Math.Floor(BT(t) + 1e-6) % 4) + 4) % 4) / 2;
		bool armLive(int i, double t) => t >= ta && t < tc + 2 * SPB && turn(t) == i;
		double armWarm(int i, double t) { double bt = BT(t) + 1e-6, f = ((bt % 2) + 2) % 2; return turn(t) == i ? 1 : f > 1 ? 0.3 + 0.35 * (f - 1) : 0.3; }
		double grow0(double t) => L13k(t, ts, ts + 4 * SPB);
		var h = new Hz { x = W / 2, y = H / 2 };
		h.update = (t, dt) =>
		{
			var c = cpos(t); h.x = c.x; h.y = c.y;
			if (t >= tx && !ex) { ex = true; burst(c.x, c.y, PINK, 90, 620); burst(c.x, c.y, "#fff", 50, 480); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; }
		};
		h.draw = t =>
		{
			var c = cpos(t); double ph = phi(t), gr = grow0(t), col = t >= tc ? L13k(t, tc, tx) : 0;
			g.save(); g.translate(c.x, c.y);
			// the arms
			g.lineCap = "round";
			for (int i = 0; i < 2; i++)
			{
				double sm = smax(i, t); if (sm <= 0.05) continue; bool live = armLive(i, t); double b0 = ph + i * Math.PI;
				g.strokeStyle = live ? PINK : L13P(t < ta ? 0.3 : armWarm(i, t)); g.lineWidth = TH * (live ? 1 + 0.12 * pulse : 0.5); if (live) { g.shadowColor = PINK; g.shadowBlur = 18; }
				g.beginPath(); int N = 90; for (int j = 0; j <= N; j++) { double s = sm * j / N, r = R0 * JS.exp(K * s), a = b0 + s; if (j != 0) g.lineTo(JS.cos(a) * r, JS.sin(a) * r); else g.moveTo(JS.cos(a) * r, JS.sin(a) * r); } g.stroke(); g.shadowBlur = 0;
			}
			g.lineCap = "butt";
			// the ring of matter and the core
			double rr2 = 92 * gr * (1 - 0.5 * col); g.strokeStyle = PINK; g.lineWidth = 12; g.shadowColor = PINK; g.shadowBlur = 16;
			for (int k = 0; k < 2; k++) { double a = -ph * 1.6 + k * Math.PI; g.beginPath(); g.arc(0, 0, Math.Max(1, rr2), a + 0.4, a + Math.PI - 0.4); g.stroke(); }
			double cr = 62 * gr * (1 - 0.6 * col) * (1 + 0.06 * pulse); g.fillStyle = "#8f1a43"; circle(0, 0, cr); g.strokeStyle = col > 0 ? PINKW : PINK;   /* 10-25: the hole hurts, so dark pink, not black */ g.lineWidth = 7; g.beginPath(); g.arc(0, 0, Math.Max(1, cr), 0, TAU); g.stroke(); g.shadowBlur = 0;
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ts + 2 * SPB || t >= tx) return false; var c = cpos(t); double dx = p.x - c.x, dy = p.y - c.y, d = hypot(dx, dy), gr = grow0(t), col = t >= tc ? L13k(t, tc, tx) : 0;
			if (d < 62 * gr * (1 - 0.6 * col) + p.r) return true;
			double rr2 = 92 * gr * (1 - 0.5 * col); if (Math.Abs(d - rr2) < 6 + p.r - 1) { double ph0 = -phi(t) * 1.6, pa = JS.atan2(dy, dx); for (int k = 0; k < 2; k++) { double m = ph0 + k * Math.PI + Math.PI / 2, da = pa - m; da = JS.atan2(JS.sin(da), JS.cos(da)); if (Math.Abs(da) < Math.PI / 2 - 0.4) return true; } }
			if (d < R0 - TH) return false; double ph = phi(t), al = JS.atan2(dy, dx);
			for (int i = 0; i < 2; i++)
			{
				if (!armLive(i, t)) continue; double sm = smax(i, t), b0 = ph + i * Math.PI, s0 = ((al - b0) % TAU + TAU) % TAU;
				for (double s = s0; s <= sm + 0.05; s += TAU) { double sc = Math.Min(s, sm); if (Math.Abs(d - R0 * JS.exp(K * sc)) * CK < TH / 2 + p.r - 1) return true; }
			}
			return false;
		};
		return h;
	}

	// a ring pulled into the black hole: it closes from the edge of the screen, with a gap
	public static Hz L13Pull(Func<double, (double x, double y)> cpos, double tw, double t0, double r0, double v, double th, double gap, double gs)
	{
		var h = new Hz();
		double rad(double t) => r0 - v * Math.Max(0, t - t0);
		h.update = (t, dt) => { if (rad(t) < 80) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; var c = cpos(t); g.strokeStyle = PINK;
			if (t < t0) { double k = L13k(t, tw, t0); g.globalAlpha = 0.2 + 0.4 * k; g.lineWidth = 3; g.setLineDash(new double[] { 14, 10 }); g.beginPath(); g.arc(c.x, c.y, r0, gap + gs / 2, gap - gs / 2 + TAU); g.stroke(); g.setLineDash(new double[0]); g.globalAlpha = 1; return; }
			double r = rad(t); g.lineWidth = th * (1 + 0.15 * pulse); g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath(); g.arc(c.x, c.y, Math.Max(1, r), gap + gs / 2, gap - gs / 2 + TAU); g.stroke(); g.shadowBlur = 0;
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; var c = cpos(t); double r = rad(t), dx = p.x - c.x, dy = p.y - c.y, d = hypot(dx, dy); if (Math.Abs(d - r) > th / 2 + p.r - 2) return false;
			double da = JS.atan2(dy, dx) - gap; da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) > gs / 2 - p.r / Math.Max(d, 1);
		};
		return h;
	}

	// a piece of a broken planet spiralling into the black hole (marked on the edge of the screen first)
	public static Hz L13Debris(Func<double, (double x, double y)> cpos, double a0, double tw, double t0, double vr, double w, double r)
	{
		var h = new Hz(); var c0 = cpos(t0); double ca = JS.cos(a0), sa = JS.sin(a0);
		double r0 = Math.Min(Math.Abs(ca) > 1e-6 ? ((ca > 0 ? W - c0.x : c0.x) / Math.Abs(ca)) : 1e9, Math.Abs(sa) > 1e-6 ? ((sa > 0 ? H - c0.y : c0.y) / Math.Abs(sa)) : 1e9) + r + 4;
		(double x, double y) pos(double t) { double d = Math.Max(0, t - t0), rr2 = r0 - vr * d, a = a0 + w * d; var c = cpos(t); return (c.x + JS.cos(a) * rr2, c.y + JS.sin(a) * rr2); }
		h.update = (t, dt) => { var q = pos(t); h.x = q.x; h.y = q.y; if (r0 - vr * Math.Max(0, t - t0) < 75) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = L13k(t, tw, t0), ex = clamp(c0.x + ca * r0, 16, W - 16), ey = clamp(c0.y + sa * r0, 16, H - 16); g.fillStyle = L13P(0.2 + 0.6 * k); g.save(); g.translate(ex, ey); g.rotate(a0 + Math.PI); g.beginPath(); g.moveTo(18, 0); g.lineTo(-8, -12); g.lineTo(-8, 12); g.closePath(); g.fill(); g.restore(); return; }
			var q = pos(t); g.save(); g.translate(q.x, q.y); g.rotate(t * 4); double s = r * (1 + 0.12 * pulse); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.moveTo(s, 0); g.lineTo(s * 0.2, s * 0.9); g.lineTo(-s * 0.9, s * 0.3); g.lineTo(-s * 0.5, -s * 0.8); g.lineTo(s * 0.5, -s * 0.7); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; var q = pos(t); return hypot(p.x - q.x, p.y - q.y) < r * 0.85 + p.r - 1; };
		return h;
	}

	// a polar jet: two beams out of the black hole that turn
	public static Hz L13Jet(Func<double, (double x, double y)> cpos, double a0, double w, double tw, double ta, double te, double wd = 30)
	{
		var h = new Hz(); const double LEN = 1500;
		double ang(double t) => a0 + w * Math.Max(0, t - ta);
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; var c = cpos(t); g.save(); g.translate(c.x, c.y);
			if (t < ta) { double k = L13k(t, tw, ta); g.rotate(a0); g.fillStyle = L13P(0.05 + 0.12 * k); g.fillRect(-LEN, -wd / 2, 2 * LEN, wd); g.fillStyle = L13P(0.85); g.fillRect(-LEN, -1.5, 2 * LEN, 3); g.restore(); return; }
			double f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25); g.rotate(ang(Math.Min(t, te))); g.globalAlpha = f; g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK;
			double ww = wd * f * (1 + 0.12 * pulse); g.fillRect(-LEN, -ww / 2, 2 * LEN, ww); g.fillStyle = "rgba(255,255,255,0.35)"; g.fillRect(-LEN, -ww * 0.15, 2 * LEN, ww * 0.3); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; var c = cpos(t); double a = ang(t); return obbHit(p.x, p.y, p.r - 1, c.x, c.y, LEN, wd / 2, a); };
		return h;
	}

	// ---------- the kinds (data/dodge_L13.json) ----------
	// h = the beat it hits, T = the part's tier (0, 1, 2: speed and warning; Easy makes them slower with longer warnings).
	// An event fires before its warning starts (early enough for Easy's longer warnings); nothing is drawn before the warning.
	static Tier L13T(DodgeEv e) => ezT(new[] { new Tier { sp = 330, wn = 1.8 }, new Tier { sp = 360, wn = 1.7 }, new Tier { sp = 390, wn = 1.6 } })[e.i("T")];
	static double L13px() => player != null ? player.x : W / 2;
	static double L13py() => player != null ? player.y : H / 2;
	// part 3: the vanishing point of the warp (it swings left and right from beat 248 to 256)
	public static (double x, double y) L13vp(double t) { double b = BT(t); double sw = b < 248 ? 0 : b < 256 ? JS.sin((b - 248) / 8 * Math.PI) : 0; return (W / 2 + 330 * sw * JS.sin((b - 248) * Math.PI / 4), H / 2 + 60 * sw * JS.sin((b - 248) * Math.PI / 2)); }
	// the black hole's centre (it circles from beat 320 to 336)
	public static (double x, double y) L13hc(double t) { double b = BT(t), k = b < 320 ? 0 : b < 322 ? (b - 320) / 2 : b < 334 ? 1 : b < 336 ? (336 - b) / 2 : 0; double a = (b - 320) * Math.PI / 8; return (W / 2 + 130 * k * JS.sin(a), H / 2 + 50 * k * JS.sin(2 * a)); }
	static readonly (double b, double w)[] L13SPIN = { (288, 0.45), (296, -0.5), (304, 0.55), (312, -0.55), (320, 0.4), (328, -0.6), (336, 0.7), (344, -1.6) };

	static readonly int _k13a = DodgeKind("L13hit", e => () => { flash = Math.Max(flash, e.num("fl")); shake = Math.Max(shake, e.num("sh")); });
	static readonly int _k13b = DodgeKind("L13light", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Light(e.num("x"), e.num("y"), e.num("r"), B(h - T.wn), B(h), B(h + e.num("dur", 0.7)))); });
	static readonly int _k13c = DodgeKind("L13lightP", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Light(clamp(L13px(), 60, W - 60), clamp(L13py(), 60, H - 60), e.num("r"), B(h - T.wn), B(h), B(h + e.num("dur", 0.7)))); });
	static readonly int _k13d = DodgeKind("L13flame", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Flame(e.num("x"), e.num("w"), !e.flag("bot"), B(h - T.wn), B(h), B(h + e.num("dur", 0.8)))); });
	static readonly int _k13e = DodgeKind("L13flameP", e => () => { var T = L13T(e); double h = e.num("h"), w = e.num("w"); hazards.Add(L13Flame(clamp(L13px(), w / 2, W - w / 2), w, !e.flag("bot"), B(h - T.wn), B(h), B(h + e.num("dur", 0.8)))); });
	static readonly int _k13f = DodgeKind("L13sparks", e => () => { var T = L13T(e); int n = e.i("n", 2); for (int i = 0; i < n; i++) { double sd = i % 2 == 0 ? 1 : -1, k = 1 + i / 2; hazards.Add(L13Spark(e.num("x"), e.num("y"), sd * T.sp * 0.55 * k, -T.sp * 1.25, B(e.b), 900)); } });
	static readonly int _k13g = DodgeKind("L13arm", e => () =>
	{
		var T = L13T(e); double h = e.num("h"), y = e.num("y"), len = e.num("len", 780), a0 = y < H / 2 ? -Math.PI / 2 : Math.PI / 2;
		hazards.Add(e.flag("left") ? L13Arm(0, y, len, 42, a0, 0, B(h - T.wn), B(h), B(h + e.num("hold")), 0.5) : L13Arm(W, y, len, 42, Math.PI - a0, Math.PI, B(h - T.wn), B(h), B(h + e.num("hold")), 0.5));
	});
	static readonly int _k13h = DodgeKind("L13vent", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Vent(e.num("c"), e.flag("vert"), e.i("gap"), e.num("L", 170), B(h - T.wn), B(h), B(h + 1.2))); });
	static readonly int _k13i = DodgeKind("L13stage", e => () => { var T = L13T(e); hazards.Add(L13Stage(e.num("x0"), -200, e.num("x1"), e.num("y1"), B(e.b), B(e.num("h")), e.i("n"), T.sp * 0.9, e.num("r0"))); });
	static readonly int _k13j = DodgeKind("L13pad", e => () => hazards.Add(L13Pad(B(e.b), B(e.num("lift")), B(e.num("lift2")), B(e.num("end")))));
	static readonly int _k13k = DodgeKind("L13earth", e => () => hazards.Add(L13Earth(B(e.b), B(e.num("end")))));
	static readonly int _k13l = DodgeKind("L13sat", e => () =>
	{
		double x0 = e.num("x0"), y0 = e.num("y0"), x1 = e.num("x1"), y1 = e.num("y1"), h = e.num("h");
		hazards.Add(L13Sat(u => (x0 + (x1 - x0) * u, y0 + (y1 - y0) * u), B(h - 3), B(h), B(e.num("end")), 0.4, e.num("w"), 150, 42));
	});
	static void L13rock(DodgeEv e, double y)
	{
		var T = L13T(e); double h = e.num("h"); bool left = e.flag("left");
		hazards.Add(L13Rock(left ? -70 : W + 70, y, (left ? 1 : -1) * T.sp * e.num("vk", 0.8), e.num("vy"), B(h - T.wn), B(h), e.num("r"), B(h + e.num("s1", 3)), B(h + e.num("s2", 6)), e.num("sep", 95), e.i("seed")));
	}
	static readonly int _k13m = DodgeKind("L13rock", e => () => L13rock(e, e.num("y")));
	static readonly int _k13n = DodgeKind("L13rockP", e => () => L13rock(e, clamp(L13py(), 120, H - 120)));
	static readonly int _k13o = DodgeKind("L13panel", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Panel(e.str("dir"), e.i("nT"), e.i("gap"), 46, T.sp * 0.75, B(h - T.wn - 1), B(h))); });
	static readonly int _k13p = DodgeKind("L13comet", e => () =>
	{
		var T = L13T(e); double h = e.num("h"), x0 = e.num("x0"), x1 = e.num("x1"), ya = e.num("ya"), yb = e.num("yb"), xc = e.num("xc"), yc = e.num("yc");
		hazards.Add(L13Comet(u => { double a = (1 - u) * (1 - u), c = u * u, m = 2 * u * (1 - u); return (a * x0 + m * xc + c * x1, a * ya + m * yc + c * yb); }, B(h - T.wn - 1), B(h), B(h + 6), 0.3, 20));
	});
	static readonly int _k13q = DodgeKind("L13meteor", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Meteor(e.num("x0"), e.num("y0"), e.num("dx"), e.num("dy"), T.sp * e.num("spk"), B(h - T.wn), B(h), 120, B(e.num("tf")), B(e.num("tx")))); });
	static readonly int _k13r = DodgeKind("L13charge", e => () => hazards.Add(L13Charge(B(e.b), B(e.num("end")))));
	static readonly int _k13s = DodgeKind("L13warpbg", e => () => hazards.Add(L13WarpBG(B(e.b), B(e.num("end")), L13vp)));
	static readonly int _k13t = DodgeKind("L13warp", e => () => { var T = L13T(e); double h = e.num("h"); var c = L13vp(B(h)); hazards.Add(L13Warp(c.x, c.y, e.i("n"), e.num("a0"), e.i("g0"), e.i("gn"), B(h), e.num("v0", 80), T.sp * 3 * e.num("ak", 1), 7, B(h - T.wn))); });
	static readonly int _k13u = DodgeKind("L13frame", e => () => { var T = L13T(e); double h = e.num("h"); var c = L13vp(B(h)); hazards.Add(L13Frame(c.x, c.y, B(h), 40, T.sp * 1.8, e.i("side"), 0.4, B(h - 1))); });
	static readonly int _k13v = DodgeKind("L13bundle", e => () =>
	{
		var T = L13T(e); double h = e.num("h"); var c = L13vp(B(h)); double a = JS.atan2(L13py() - c.y, L13px() - c.x);
		for (int i = -2; i <= 2; i++) hazards.Add(L13Warp(c.x, c.y, 1, a + i * 0.16, 0, 0, B(h), 160, T.sp * 3 * 1.2, 7, B(h - T.wn)));
	});
	static readonly int _k13w = DodgeKind("L13hole", e => () => hazards.Add(L13Hole(B(e.b), B(e.num("arms")), B(e.num("fall")), B(e.num("end")), L13SPIN, L13hc)));
	static readonly int _k13x = DodgeKind("L13pull", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Pull(L13hc, B(h - T.wn), B(h), 760, T.sp * 1.0, 22, e.num("gap"), e.num("gs", 0.9))); });
	static readonly int _k13y = DodgeKind("L13debris", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Debris(L13hc, e.num("a0"), B(h - T.wn), B(h), T.sp * 0.8, e.num("w"), 16)); });
	static readonly int _k13aa = DodgeKind("L13wall", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Wall(e.str("side"), e.num("d"), e.num("amp", 20), B(h - T.wn), B(h), B(e.num("end")), e.num("wv", 1.6))); });
	static readonly int _k13z = DodgeKind("L13jet", e => () => { var T = L13T(e); double h = e.num("h"); hazards.Add(L13Jet(L13hc, e.num("a0"), e.num("w"), B(h - T.wn), B(h), B(h + 2.5))); });
}
