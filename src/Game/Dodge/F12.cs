using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F12 "Can't Stop The Feeling" (Justin Timberlake), Coulexrl's level: a feeling that starts as a spark inside the bones and can't be stopped.
// part 1 the electricity in the bones (cables on the guitar, the x-ray and its wavy corridor, the city's arcs and the broken ceiling,
//   the soles of the feet stamping, the hot drop, the stage's searchlights, "just imagine"),
// part 2 the sunshine in the pocket (the giant neon dancer whose dance is the attack, the light creeping in from the edges, the magic
//   rivers with their gold bubbles, the sun rising with its turning rays, the ring of spotlights closing in),
// part 3 can't stop the feeling (the dancer again, bigger, on a turning floor, the endless figure-8 line, the x-ray skeleton dancing
//   and throwing its bones like boomerangs, the hush and the gold light).
// Only pink hurts: the places, the sun's disc, the bubbles, the dancer's body and head are only a look.
// Every hazard is a pure function of time once it exists. The data is data/dodge_F12.json (made by tools/friend12/gen.py).
public static partial class G
{
	static double F12K(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double F12E(double u) => u * u * (3 - 2 * u);
	static string F12P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string F12L(double a) => $"rgba(255,150,190,{str(clamp(a, 0, 1))})";   // light pink: the hot core of a pink thing
	static string F12G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string F12Y(double a) => $"rgba(232,186,104,{str(clamp(a, 0, 1))})";   // gold: only a look
	static string F12X(double a) => $"rgba(120,190,255,{str(clamp(a, 0, 1))})";   // the x-ray's blue: only a look
	static readonly double[] F12DA = { 10, 9 }, F12DN = Array.Empty<double>();
	static void F12Dashed(string col, double w) { g.strokeStyle = col; g.lineWidth = w; g.setLineDash(F12DA); g.stroke(); g.setLineDash(F12DN); }
	// a value between keyframes (kb seconds, kv values), eased; before the first and after the last it holds
	static double F12Lerp(double[] kb, double[] kv, double t)
	{
		int n = kb.Length; if (n == 0) return 0; if (t <= kb[0]) return kv[0]; if (t >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && t >= kb[i + 1]) i++;
		double u = (t - kb[i]) / Math.Max(1e-6, kb[i + 1] - kb[i]); return kv[i] + (kv[i + 1] - kv[i]) * F12E(clamp(u, 0, 1));
	}
	// the next keyframe (index) whose value changes and that starts moving within wn s from t; -1 none
	static int F12Next(double[] kb, double[] kv, double t, double wn)
	{
		for (int i = 1; i < kb.Length; i++)
		{
			if (kb[i - 1] + 1e-6 < t) continue;   // this move has started already
			if (Math.Abs(kv[i] - kv[i - 1]) < 1e-6) continue;
			return kb[i - 1] - t <= wn ? i : -1;
		}
		return -1;
	}
	static double F12Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double F12H(int i) { unchecked { uint x = (uint)(i * 2654435761u) ^ 0x7f4a7c15u; x ^= x >> 15; x *= 0x85ebca6bu; x ^= x >> 13; return (x % 10000) / 10000.0; } }
	static double F12AngD(double a, double b) { double d = (a - b) % TAU; if (d > Math.PI) d -= TAU; if (d < -Math.PI) d += TAU; return Math.Abs(d); }
	// the angle room a circle of radius pr at distance d takes (so a cone's edge hits the player's edge, not its middle)
	static double F12Room(double d, double pr) => d <= pr ? Math.PI : Math.Asin(Math.Min(1, pr / d));

	// ---------- the places (harmless) ----------
	// sc: 1 the dark room, 2 the x-ray, 3 the city at night (bx = buildings: x, width, height), 4 the dance floor, 5 the stage,
	// 6 the neon hall, 7 the magic night, 8 the sunrise, 9 the turning dance floor (kb/ka: its turn, degrees), 10 the x-ray again (stronger)
	public static Hz F12Stage(int sc, double t0, double t1, double[] kb, double[] ka, double[] bx)
	{
		var h = new Hz { kind = "F12Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.7, (t1 - t) / 0.7)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			switch (sc)
			{
				case 1:
				{
					var gr = g.createRadialGradient(W / 2, 90, 20, W / 2, H / 2, 900); gr.addColorStop(0, "#2a1a14"); gr.addColorStop(0.35, "#120a12"); gr.addColorStop(1, "#05030a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					// the wall's panels and the bulb on its wire
					g.strokeStyle = "rgba(90,60,50,0.18)"; g.lineWidth = 2;
					for (double x = 80; x < W; x += 160) { g.strokeRect(x - 66, 70, 132, 560); }
					g.strokeStyle = F12Y(0.35); g.lineWidth = 2; g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, 64); g.stroke();
					double fl = 0.75 + 0.25 * JS.sin(t * 9.3) * JS.sin(t * 3.1);
					var gb = g.createRadialGradient(W / 2, 76, 2, W / 2, 76, 140); gb.addColorStop(0, F12Y(0.5 * fl)); gb.addColorStop(1, F12Y(0)); g.fillStyle = gb; g.fillRect(W / 2 - 140, 0, 280, 220);
					g.fillStyle = F12Y(0.85); circle(W / 2, 76, 11);
					break;
				}
				case 2: case 10:
				{
					var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, sc == 2 ? "#03101c" : "#041628"); gr.addColorStop(1, "#02060e"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					g.strokeStyle = F12X(sc == 2 ? 0.05 : 0.07); g.lineWidth = 1;
					for (double x = 0; x <= W; x += 40) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x, H); g.stroke(); }
					for (double y = 0; y <= H; y += 40) { g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
					// a faint spine and ribs behind everything (the film of the x-ray; in part 3 the skeleton is there itself)
					g.strokeStyle = F12X(0.07); g.lineWidth = 6;
					for (int i = 0; i < (sc == 2 ? 6 : 0); i++)
					{
						double y = 170 + i * 62;
						g.beginPath(); g.ellipse(W / 2, y, 250 - i * 18, 40, 0, Math.PI * 1.08, Math.PI * 1.92, false); g.stroke();
						g.fillStyle = F12X(0.06); g.fillRect(W / 2 - 12, y - 30, 24, 46);
					}
					// the scan line
					double sy = (t * 140) % (H + 200) - 100; var gs = g.createLinearGradient(0, sy - 60, 0, sy + 4); gs.addColorStop(0, F12X(0)); gs.addColorStop(1, F12X(sc == 2 ? 0.08 : 0.12)); g.fillStyle = gs; g.fillRect(0, sy - 60, W, 64);
					break;
				}
				case 3:
				{
					var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#07061a"); gr.addColorStop(0.7, "#1a0f2c"); gr.addColorStop(1, "#24122e"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					for (int i = 0; i < 46; i++) { double x = F12H(i) * W, y = F12H(i + 99) * 380, tw2 = 0.5 + 0.5 * JS.sin(t * (1.5 + F12H(i + 7) * 2) + i); g.fillStyle = F12Y(0.15 + 0.35 * tw2); circle(x, y, 1.2 + F12H(i + 3) * 1.4); }
					g.fillStyle = "rgba(255,240,215,0.12)"; circle(1080, 110, 44); g.fillStyle = "rgba(255,240,215,0.22)"; circle(1080, 110, 30);
					for (int i = 0; i + 2 < bx.Length; i += 3)
					{
						double x = bx[i], w = bx[i + 1], hh = bx[i + 2], y = H - hh;
						g.fillStyle = "#100a1c"; g.fillRect(x, y, w, hh); g.strokeStyle = F12X(0.14); g.lineWidth = 2; g.strokeRect(x, y, w, hh);
						for (double wy = y + 16; wy < H - 14; wy += 22)
							for (double wx = x + 10; wx < x + w - 12; wx += 18)
							{ int q = (int)(wx * 7 + wy * 13); if (F12H(q) < 0.42) { g.fillStyle = F12Y(0.18 + 0.2 * F12H(q + 1)); g.fillRect(wx, wy, 8, 10); } }
					}
					break;
				}
				case 4:
				{
					g.fillStyle = "#120b08"; g.fillRect(0, 0, W, H);
					for (int r = 0; r * 30 < H; r++)
					{
						double y = r * 30; g.fillStyle = r % 2 == 0 ? "rgba(70,40,22,0.35)" : "rgba(58,32,18,0.35)"; g.fillRect(0, y, W, 30);
						g.strokeStyle = "rgba(20,10,6,0.8)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke();
						double x = -F12H(r * 3) * 300; while (x < W) { g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + 30); g.stroke(); x += 260 + F12H(r * 3 + (int)x) * 220; }
						g.strokeStyle = "rgba(110,70,40,0.18)"; g.lineWidth = 1; g.beginPath(); g.moveTo(0, y + 10 + 6 * F12H(r)); g.lineTo(W, y + 12 + 6 * F12H(r + 50)); g.stroke();
					}
					var gr = g.createRadialGradient(W / 2, H / 2, 40, W / 2, H / 2, 760); gr.addColorStop(0, F12Y(0.14)); gr.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					break;
				}
				case 5:
				{
					var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#0b0616"); gr.addColorStop(1, "#140a1a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					// the curtains (violet velvet) and the truss the lamps hang from
					for (int s = 0; s < 2; s++)
					{
						double x0 = s == 0 ? 0 : W - 76;
						g.fillStyle = "#1c0e34"; g.fillRect(x0, 0, 76, H);
						g.strokeStyle = "rgba(90,60,160,0.35)"; g.lineWidth = 3;
						for (double x = x0 + 10; x < x0 + 76; x += 16) { g.beginPath(); g.moveTo(x, 0); g.quadraticCurveTo(x + 6 * JS.sin(t + x), H / 2, x, H); g.stroke(); }
					}
					g.fillStyle = "#1a1824"; g.fillRect(0, 0, W, 26); g.strokeStyle = "rgba(170,170,190,0.25)"; g.lineWidth = 2;
					for (double x = 0; x < W; x += 26) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x + 13, 26); g.lineTo(x + 26, 0); g.stroke(); }
					g.strokeStyle = F12Y(0.12); g.lineWidth = 2; g.beginPath(); g.moveTo(76, H - 40); g.lineTo(W - 76, H - 40); g.stroke();
					break;
				}
				case 6:
				{
					g.fillStyle = "#07040f"; g.fillRect(0, 0, W, H);
					g.strokeStyle = "rgba(120,80,200,0.12)"; g.lineWidth = 2;
					for (int i = -8; i <= 8; i++) { g.beginPath(); g.moveTo(W / 2 + i * 40, 430); g.lineTo(W / 2 + i * 190, H); g.stroke(); }
					for (double y = 450; y < H; y += 30 + (y - 430) * 0.3) { g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
					double pu = 0.7 + 0.3 * JS.sin(t * TAU * 113 / 60 / 2);
					g.strokeStyle = F12Y(0.28 * pu); g.lineWidth = 3; g.strokeRect(18, 18, W - 36, H - 36);
					g.strokeStyle = "rgba(140,100,255," + str(0.22 * pu) + ")"; g.lineWidth = 2; g.strokeRect(30, 30, W - 60, H - 60);
					break;
				}
				case 7:
				{
					var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#030f1a"); gr.addColorStop(1, "#0a0a26"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					for (int i = 0; i < 50; i++)
					{
						double x = F12H(i + 300) * W + 20 * JS.sin(t * 0.7 + i), y = (F12H(i + 400) * (H + 80) - t * (18 + 30 * F12H(i))) % (H + 80); if (y < 0) y += H + 80;
						g.fillStyle = F12Y(0.12 + 0.3 * F12H(i + 9)); circle(x, y - 40, 1.4 + 1.6 * F12H(i + 5));
					}
					break;
				}
				case 8:
				{
					var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#0e0718"); gr.addColorStop(0.55, "#24102a"); gr.addColorStop(1, "#4a2412"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
					g.fillStyle = "#130a14"; g.beginPath(); g.moveTo(0, H);
					for (double x = 0; x <= W; x += 40) g.lineTo(x, H - 70 - 40 * JS.sin(x / 210) - 20 * JS.sin(x / 77));
					g.lineTo(W, H); g.closePath(); g.fill();
					break;
				}
				case 9:
				{
					g.fillStyle = "#08040e"; g.fillRect(0, 0, W, H);
					double an = F12Lerp(kb, ka, t) * Math.PI / 180;
					g.save(); g.translate(W / 2, H / 2); g.rotate(an);
					for (int i = 1; i <= 9; i++) { g.strokeStyle = F12Y(i % 3 == 0 ? 0.13 : 0.06); g.lineWidth = i % 3 == 0 ? 3 : 1.5; g.beginPath(); g.arc(0, 0, i * 82, 0, TAU); g.stroke(); }
					for (int i = 0; i < 24; i++) { double q = i * TAU / 24; g.strokeStyle = F12Y(i % 2 == 0 ? 0.1 : 0.05); g.lineWidth = 2; g.beginPath(); g.moveTo(JS.cos(q) * 82, JS.sin(q) * 82); g.lineTo(JS.cos(q) * 760, JS.sin(q) * 760); g.stroke(); }
					g.restore();
					break;
				}
			}
			g.restore();
		};
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F12Fx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F12fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- electricity ----------
	// a bolt (a live cable, an arc between two roofs, a power line): along the points pts (x, y pairs); it hurts w wide from ts to te;
	// its way shows dashed from tw
	public static Hz F12Bolt(double[] pts, double w, double tw, double ts, double te, int sd)
	{
		var h = new Hz { kind = "F12Bolt" };
		// cut it into pieces of 40 px at most (the zigzag of the spark is on their joints)
		var lx = new List<double>(); var ly = new List<double>(); int np = pts.Length / 2;
		for (int i = 0; i + 1 < np; i++)
		{
			double ax = pts[2 * i], ay = pts[2 * i + 1], bx = pts[2 * i + 2], by = pts[2 * i + 3]; int m = Math.Max(1, (int)Math.Ceiling(hypot(bx - ax, by - ay) / 40));
			for (int q = 0; q < m; q++) { lx.Add(ax + (bx - ax) * q / m); ly.Add(ay + (by - ay) * q / m); }
		}
		lx.Add(pts[2 * np - 2]); ly.Add(pts[2 * np - 1]);
		var px = lx.ToArray(); var py = ly.ToArray(); int N = px.Length - 1;
		double x0 = px[0], y0 = py[0], x1 = px[N], y1 = py[N];
		void path(double jit, int fr)
		{
			g.beginPath();
			for (int i = 0; i <= N; i++)
			{
				double ox = 0, oy = 0;
				if (jit > 0 && i > 0 && i < N)
				{
					double dx = px[i + 1] - px[i - 1], dy = py[i + 1] - py[i - 1], l = Math.Max(1e-6, hypot(dx, dy)), s = (F12H(fr * 31 + i * 7 + sd) * 2 - 1) * jit;
					ox = -dy / l * s; oy = dx / l * s;
				}
				if (i == 0) g.moveTo(px[i] + ox, py[i] + oy); else g.lineTo(px[i] + ox, py[i] + oy);
			}
		}
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te + 0.15) return;
			if (t < ts)
			{
				double k = F12K(t, tw, ts); path(0, 0); F12Dashed(F12G(0.3 + 0.55 * k), 2);
				g.strokeStyle = F12G(0.4 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.arc(x0, y0, 7, 0, TAU); g.stroke(); g.beginPath(); g.arc(x1, y1, 7, 0, TAU); g.stroke();
				return;
			}
			double f = t > te ? 1 - F12K(t, te, te + 0.15) : 1; int fr = (int)Math.Floor(t * 24);
			g.lineCap = "round"; g.lineJoin = "round";
			path(w * 0.16, fr); g.strokeStyle = F12P(0.5 * f); g.lineWidth = w; g.stroke();
			g.strokeStyle = F12P(0.95 * f); g.lineWidth = Math.Max(3, w * 0.5); g.stroke();
			path(w * 0.22, fr + 5); g.strokeStyle = F12L(0.95 * f); g.lineWidth = Math.Max(2, w * 0.18); g.stroke();
			g.lineCap = "butt"; g.lineJoin = "miter";
			g.fillStyle = F12P(0.95 * f); circle(x0, y0, w * 0.5); circle(x1, y1, w * 0.5);
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false;
			for (int i = 0; i < N; i++) if (F12Seg(p.x, p.y, px[i], py[i], px[i + 1], py[i + 1]) < w * 0.5 + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// the x-ray's wavy corridor: two live bands from the top and the bottom close in to a wave (middle cy + A sin, A from the keyframes
	// kb/ka, wavelength lam, running left at v px/s) leaving a corridor gap tall; sparks run along it from the right (st start times,
	// so their place across: -1..1 of the corridor, sv speed, sr size, shown on the right edge swn s before)
	public static Hz F12Wave(double cy, double[] kb, double[] ka, double lam, double v, double gap, double tw, double t0, double t1, double[] st, double[] so, double sv, double sr, double swn)
	{
		var h = new Hz { kind = "F12Wave" };
		double c(double x, double t) => cy + F12Lerp(kb, ka, t) * JS.sin(TAU * (x + v * (t - t0)) / lam);
		double k(double t) => F12E(F12K(t, t0, t0 + 0.6)) * (1 - F12E(F12K(t, t1, t1 + 0.6)));
		double top(double x, double t) => -20 + (c(x, t) - gap / 2 + 20) * k(t);
		double bot(double x, double t) => H + 20 - (H + 20 - c(x, t) - gap / 2) * k(t);
		double sx(int i, double t) => W + 40 - sv * (t - st[i]);
		h.update = (t, dt) => { if (t > t1 + 0.8 && (st.Length == 0 || sx(st.Length - 1, t) < -60)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0)
			{
				double q = F12K(t, tw, t0);
				for (int s = -1; s <= 1; s += 2) { g.beginPath(); for (double x = -10; x <= W + 10; x += 20) { double y = c(x, t) + s * gap / 2; if (x < -9) g.moveTo(x, y); else g.lineTo(x, y); } F12Dashed(F12G(0.3 + 0.55 * q), 2); }
			}
			else if (t < t1 + 0.6)
			{
				int fr = (int)Math.Floor(t * 20);
				for (int s = -1; s <= 1; s += 2)
				{
					g.beginPath(); g.moveTo(-10, s < 0 ? -30 : H + 30);
					for (double x = -10; x <= W + 10; x += 20) g.lineTo(x, s < 0 ? top(x, t) : bot(x, t));
					g.lineTo(W + 10, s < 0 ? -30 : H + 30); g.closePath(); g.fillStyle = F12P(0.5); g.fill();
					g.beginPath();
					for (double x = -10; x <= W + 10; x += 20) { double y = (s < 0 ? top(x, t) : bot(x, t)) + ((((int)(x / 20) + fr) % 2 == 0) ? 2.5 : -2.5); if (x < -9) g.moveTo(x, y); else g.lineTo(x, y); }
					g.strokeStyle = F12P(0.95); g.lineWidth = 5; g.stroke(); g.strokeStyle = F12L(0.8); g.lineWidth = 1.5; g.stroke();
				}
			}
			for (int i = 0; i < st.Length; i++)
			{
				if (t < st[i] - swn) break;
				double y0 = c(W - 30, Math.Max(t, st[i])) + so[i] * gap / 2 * 0.62;
				if (t < st[i])
				{
					double q = F12K(t, st[i] - swn, st[i]); g.strokeStyle = F12G(0.4 + 0.5 * q); g.lineWidth = 3;
					g.beginPath(); g.moveTo(W - 14, y0); g.lineTo(W - 60, y0); g.moveTo(W - 46, y0 - 12); g.lineTo(W - 60, y0); g.lineTo(W - 46, y0 + 12); g.stroke(); continue;
				}
				double x = sx(i, t); if (x < -60) continue; double y = c(x, t) + so[i] * gap / 2 * 0.62;
				g.fillStyle = F12P(0.4); circle(x, y, sr * 1.5); g.fillStyle = F12P(0.95); circle(x, y, sr); g.fillStyle = F12L(0.95); circle(x, y, sr * 0.45);
				g.strokeStyle = F12P(0.9); g.lineWidth = 2; g.beginPath();
				for (int j = 0; j < 6; j++) { double an = j * TAU / 6 + t * 9; g.moveTo(x + JS.cos(an) * sr, y + JS.sin(an) * sr); g.lineTo(x + JS.cos(an) * sr * 1.6, y + JS.sin(an) * sr * 1.6); }
				g.stroke();
			}
		};
		h.hit = (p, t) =>
		{
			if (t >= t0 && t < t1 + 0.6 && k(t) > 0.01)
			{
				if (p.y - p.r + 2 < top(p.x, t) || p.y + p.r - 2 > bot(p.x, t)) return true;
			}
			for (int i = 0; i < st.Length; i++)
			{
				if (t < st[i]) break; double x = sx(i, t); if (x < -60) continue; double y = c(x, t) + so[i] * gap / 2 * 0.62;
				if (hypot(p.x - x, p.y - y) < sr + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- things that fly ----------
	// shape 0 a shard of the ceiling, 1 a bone, 2 a hot drop: from (x0, y0) to (x1, y1) (through (qx, qy) when cur) in dur s; back: it
	// slows down there and comes back the same way (a boomerang). spin: turns a second (0: it points where it goes). Its way shows dashed from tw.
	public static Hz F12Proj(int shape, double x0, double y0, double x1, double y1, double qx, double qy, bool cur, bool back, double r, double spin, double tw, double ts, double dur, int sd)
	{
		var h = new Hz { kind = "F12Proj" }; double te = ts + dur * (back ? 2 : 1);
		var sh = new double[10]; for (int i = 0; i < 5; i++) { double an = i * TAU / 5 + F12H(sd * 5 + i) * 0.7, rr = r * (0.75 + 0.45 * F12H(sd * 7 + i)); sh[2 * i] = JS.cos(an) * rr; sh[2 * i + 1] = JS.sin(an) * rr; }
		void at(double u, out double x, out double y)
		{
			if (cur) { double a = (1 - u) * (1 - u), b = 2 * u * (1 - u), c = u * u; x = a * x0 + b * qx + c * x1; y = a * y0 + b * qy + c * y1; }
			else { x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u; }
		}
		double U(double t) { double s = clamp((t - ts) / dur, 0, back ? 2 : 1); return back ? 1 - (1 - s) * (1 - s) : s; }
		double ang(double t)
		{
			if (spin != 0) return spin * TAU * (t - ts);
			double u = U(t), u2 = Math.Min(1, u + 0.01); at(u, out var ax, out var ay); at(u2, out var bx, out var by);
			if (u2 - u < 0.005) { at(u - 0.01, out bx, out by); return Math.Atan2(ay - by, ax - bx); }
			return Math.Atan2(by - ay, bx - ax);
		}
		void shapePath(double x, double y, double an)
		{
			g.save(); g.translate(x, y); g.rotate(an); g.beginPath();
			if (shape == 0) { for (int i = 0; i < 5; i++) { if (i == 0) g.moveTo(sh[0], sh[1]); else g.lineTo(sh[2 * i], sh[2 * i + 1]); } g.closePath(); }
			else if (shape == 1)
			{
				double L = r * 1.15, k = r * 0.3;
				g.rect(-L, -k * 0.75, 2 * L, k * 1.5);
				g.moveTo(-L + k, -k * 0.6); g.arc(-L, -k * 0.6, k, 0, TAU); g.moveTo(-L + k, k * 0.6); g.arc(-L, k * 0.6, k, 0, TAU);
				g.moveTo(L + k, -k * 0.6); g.arc(L, -k * 0.6, k, 0, TAU); g.moveTo(L + k, k * 0.6); g.arc(L, k * 0.6, k, 0, TAU);
			}
			else { g.moveTo(r * 1.6, 0); g.quadraticCurveTo(r * 0.5, -r, -r * 0.3, -r * 0.92); g.arc(-r * 0.3, 0, r * 0.92, -Math.PI / 2, Math.PI / 2, true); g.quadraticCurveTo(r * 0.5, r, r * 1.6, 0); g.closePath(); }
			g.restore();
		}
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return;
			if (t < ts)
			{
				double k = F12K(t, tw, ts); g.beginPath();
				for (int i = 0; i <= 20; i++) { at(i / 20.0, out var x, out var y); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
				F12Dashed(F12G(0.25 + 0.5 * k), 2);
				shapePath(x0, y0, ang(ts)); g.strokeStyle = F12G(0.4 + 0.5 * k); g.lineWidth = 2; g.stroke();
				return;
			}
			at(U(t), out var px, out var py); double an = ang(t);
			if (back && t < ts + dur)
			{   // the way back, dashed
				g.beginPath(); for (int i = 0; i <= 12; i++) { at(i / 12.0, out var x, out var y); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
				F12Dashed(F12G(0.25), 2);
			}
			shapePath(px, py, an); g.fillStyle = F12P(0.95); g.fill(); g.strokeStyle = F12L(0.9); g.lineWidth = 2; g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false; at(U(t), out var x, out var y);
			if (shape == 1)
			{
				double an = ang(t), L = r * 1.15, c = JS.cos(an), s = JS.sin(an);
				return F12Seg(p.x, p.y, x - c * L, y - s * L, x + c * L, y + s * L) < r * 0.42 + p.r - 2;
			}
			if (shape == 2) { double an = ang(t); return F12Seg(p.x, p.y, x - JS.cos(an) * r * 0.3, y - JS.sin(an) * r * 0.3, x + JS.cos(an) * r * 0.45, y + JS.sin(an) * r * 0.45) < r * 0.75 + p.r - 2; }
			return hypot(p.x - x, p.y - y) < r * 0.78 + p.r - 2;
		};
		return h;
	}

	// a ring running out from (x, y): from radius r0 to r1 in dur s, w thick, with gaps (centres in degrees, gw degrees each side);
	// it shows dashed with its gaps from tw
	public static Hz F12Ring(double x, double y, double r0, double r1, double w, double[] gaps, double gw, double tw, double ts, double dur)
	{
		var h = new Hz { kind = "F12Ring" }; double te = ts + dur, gwr = gw * Math.PI / 180;
		var ga = gaps.Select(q => q * Math.PI / 180).ToArray();
		double R(double t) => r0 + (r1 - r0) * clamp((t - ts) / dur, 0, 1);
		void arcs(double rr)
		{
			if (ga.Length == 0) { g.beginPath(); g.arc(x, y, rr, 0, TAU); return; }
			var s = ga.OrderBy(q => ((q % TAU) + TAU) % TAU).Select(q => ((q % TAU) + TAU) % TAU).ToArray(); g.beginPath();
			for (int i = 0; i < s.Length; i++) { double a0 = s[i] + gwr, a1 = (i + 1 < s.Length ? s[i + 1] : s[0] + TAU) - gwr; if (a1 > a0) { g.moveTo(x + JS.cos(a0) * rr, y + JS.sin(a0) * rr); g.arc(x, y, rr, a0, a1); } }
		}
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return;
			if (t < ts) { double k = F12K(t, tw, ts); arcs(r0 + 30); F12Dashed(F12G(0.3 + 0.5 * k), 2); arcs(r0 + 30 + w); F12Dashed(F12G(0.2 + 0.3 * k), 2); return; }
			double rr = R(t), f = 1 - F12K(t, te - 0.15, te);
			arcs(rr); g.strokeStyle = F12P(0.9 * f); g.lineWidth = w; g.stroke(); g.strokeStyle = F12L(0.8 * f); g.lineWidth = Math.Max(2, w * 0.2); g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te - 0.15) return false; double d = hypot(p.x - x, p.y - y), rr = R(t);
			if (Math.Abs(d - rr) >= w / 2 + p.r - 2) return false;
			double an = Math.Atan2(p.y - y, p.x - x), room = F12Room(d, p.r - 2);
			foreach (var q in ga) if (F12AngD(an, q) < gwr - room) return false;
			return true;
		};
		return h;
	}

	// ---------- the feet ----------
	// a giant sole stamps at (x, y), its toe pointing at ang (deg), L long (right: the right foot). Its print shows dashed from tw;
	// it hurts from ts for hold s, then lifts
	public static Hz F12Sole(double x, double y, double ang, double L, bool right, double tw, double ts, double hold)
	{
		var h = new Hz { kind = "F12Sole" }; double te = ts + hold, an = ang * Math.PI / 180, ca = JS.cos(an), sa = JS.sin(an), mir = right ? 1 : -1;
		void sole(double sc)
		{
			g.save(); g.translate(x, y); g.rotate(an); g.scale(sc, sc * mir); g.beginPath();
			g.moveTo(0.5 * L, 0);
			g.bezierCurveTo(0.5 * L, -0.2 * L, 0.2 * L, -0.24 * L, 0.02 * L, -0.19 * L);
			g.bezierCurveTo(-0.12 * L, -0.15 * L, -0.2 * L, -0.15 * L, -0.32 * L, -0.15 * L);
			g.bezierCurveTo(-0.52 * L, -0.15 * L, -0.52 * L, 0.15 * L, -0.32 * L, 0.15 * L);
			g.bezierCurveTo(-0.2 * L, 0.15 * L, -0.12 * L, 0.08 * L, 0.02 * L, 0.12 * L);
			g.bezierCurveTo(0.2 * L, 0.2 * L, 0.5 * L, 0.18 * L, 0.5 * L, 0);
			g.closePath(); g.restore();
		}
		void tread(double f)
		{
			g.save(); g.translate(x, y); g.rotate(an); g.scale(1, mir); g.strokeStyle = "rgba(140,10,52," + str(0.7 * f) + ")"; g.lineWidth = 4;
			for (int i = 0; i < 5; i++) { double u = 0.08 * L + i * 0.075 * L; g.beginPath(); g.moveTo(u, -0.14 * L); g.lineTo(u + 0.04 * L, 0); g.lineTo(u, 0.13 * L); g.stroke(); }
			for (int i = 0; i < 3; i++) { double u = -0.42 * L + i * 0.07 * L; g.beginPath(); g.moveTo(u, -0.1 * L); g.lineTo(u + 0.03 * L, 0); g.lineTo(u, 0.1 * L); g.stroke(); }
			g.restore();
		}
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te + 0.2) return;
			if (t < ts)
			{
				double k = F12K(t, tw, ts); sole(1); g.fillStyle = F12G(0.06 + 0.12 * k); g.fill(); F12Dashed(F12G(0.35 + 0.55 * k), 2); return;
			}
			if (t > te) { double f = 1 - F12K(t, te, te + 0.2); sole(1 + 0.15 * (1 - f)); g.strokeStyle = F12G(0.5 * f); g.lineWidth = 2; g.stroke(); return; }
			double sc = 1 + 0.14 * (1 - F12K(t, ts, ts + 0.07));
			sole(sc); g.fillStyle = F12P(0.9); g.fill(); g.strokeStyle = F12L(0.95); g.lineWidth = 3; g.stroke(); tread(1);
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false;
			double rx = p.x - x, ry = p.y - y, u = (rx * ca + ry * sa) / L, v = mir * (-rx * sa + ry * ca) / L, pr = (p.r - 2) / L;
			// the forefoot, the heel and the arch between them (in L units)
			double e1 = Math.Pow((u - 0.24) / (0.27 + pr), 2) + Math.Pow((v - 0.0) / (0.2 + pr), 2);
			double e2 = Math.Pow((u + 0.33) / (0.17 + pr), 2) + Math.Pow(v / (0.15 + pr), 2);
			bool arch = u > -0.34 && u < 0.05 && v > -0.15 - pr && v < 0.1 + pr;
			return e1 < 1 || e2 < 1 || arch;
		};
		return h;
	}

	// ---------- the lights ----------
	// a searchlight: a cone from the lamp (ox, oy), its direction moving between keyframes (kb seconds, ka degrees), ha degrees each side,
	// Lb long. It hurts from t0 to t1; before t0 it shows dashed (from tw), and while it burns the place it goes next shows dashed wn s before
	public static Hz F12Beam(double ox, double oy, double[] kb, double[] ka, double ha, double Lb, double wn, double tw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Beam" }; double har = ha * Math.PI / 180;
		void cone(double an)
		{
			g.beginPath(); g.moveTo(ox, oy); g.lineTo(ox + JS.cos(an - har) * Lb, oy + JS.sin(an - har) * Lb);
			g.arc(ox, oy, Lb, an - har, an + har); g.closePath();
		}
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > t1 + 0.3) return;
			double an = F12Lerp(kb, ka, t) * Math.PI / 180;
			if (t < t0) { double k = F12K(t, tw, t0); cone(an); F12Dashed(F12G(0.3 + 0.55 * k), 2); }
			else
			{
				double f = t > t1 ? 1 - F12K(t, t1, t1 + 0.3) : 1;
				int j = F12Next(kb, ka, t, wn); if (j >= 0 && t < t1) { cone(ka[j] * Math.PI / 180); F12Dashed(F12G(0.5), 2); }
				var gr = g.createRadialGradient(ox, oy, 10, ox, oy, Lb); gr.addColorStop(0, F12P(0.72 * f)); gr.addColorStop(1, F12P(0.5 * f));
				cone(an); g.fillStyle = gr; g.fill(); g.strokeStyle = F12P(0.95 * f); g.lineWidth = 3; g.stroke();
			}
			// the lamp (harmless)
			g.save(); g.translate(ox, oy); g.rotate(an); g.fillStyle = "#2a2632"; g.fillRect(-22, -15, 30, 30); g.fillStyle = F12Y(0.8); g.fillRect(6, -13, 6, 26); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false; double d = hypot(p.x - ox, p.y - oy); if (d > Lb + p.r || d < 18) return false;
			double an = F12Lerp(kb, ka, t) * Math.PI / 180;
			return F12AngD(Math.Atan2(p.y - oy, p.x - ox), an) < har + F12Room(d, p.r - 2);
		};
		return h;
	}

	// "just imagine": a shape (pts round (cx, cy), turned rot degrees) draws itself dashed from tw, then fills pink at ts for hold s
	public static Hz F12Shape(double cx, double cy, double[] pts, double rot, double tw, double ts, double hold)
	{
		var h = new Hz { kind = "F12Shape" }; int n = pts.Length / 2; double te = ts + hold, ra = rot * Math.PI / 180, c = JS.cos(ra), s = JS.sin(ra);
		var X = new double[n]; var Y = new double[n]; double per = 0;
		for (int i = 0; i < n; i++) { X[i] = cx + pts[2 * i] * c - pts[2 * i + 1] * s; Y[i] = cy + pts[2 * i] * s + pts[2 * i + 1] * c; }
		for (int i = 0; i < n; i++) per += hypot(X[(i + 1) % n] - X[i], Y[(i + 1) % n] - Y[i]);
		void poly() { g.beginPath(); for (int i = 0; i < n; i++) { if (i == 0) g.moveTo(X[i], Y[i]); else g.lineTo(X[i], Y[i]); } g.closePath(); }
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te + 0.25) return;
			if (t < ts)
			{
				// the outline draws itself (a pencil going round), then waits dashed
				double k = F12E(F12K(t, tw, tw + (ts - tw) * 0.7)), want = per * k, done = 0;
				g.beginPath(); g.moveTo(X[0], Y[0]);
				for (int i = 0; i < n && done < want; i++)
				{
					double x1 = X[(i + 1) % n], y1 = Y[(i + 1) % n], l = hypot(x1 - X[i], y1 - Y[i]);
					if (done + l <= want) g.lineTo(x1, y1); else { double u = (want - done) / l; g.lineTo(X[i] + (x1 - X[i]) * u, Y[i] + (y1 - Y[i]) * u); }
					done += l;
				}
				F12Dashed(F12G(0.45 + 0.45 * F12K(t, tw, ts)), 3); return;
			}
			double f = t > te ? 1 - F12K(t, te, te + 0.25) : 1, pop = 1 - F12K(t, ts, ts + 0.1);
			poly(); g.fillStyle = F12P((0.55 + 0.3 * pop) * f); g.fill(); g.strokeStyle = F12L(0.95 * f); g.lineWidth = 4; g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false;
			bool ins = false;
			for (int i = 0, j = n - 1; i < n; j = i++) if ((Y[i] > p.y) != (Y[j] > p.y) && p.x < (X[j] - X[i]) * (p.y - Y[i]) / (Y[j] - Y[i]) + X[i]) ins = !ins;
			if (ins) return true;
			for (int i = 0; i < n; i++) if (F12Seg(p.x, p.y, X[i], Y[i], X[(i + 1) % n], Y[(i + 1) % n]) < p.r - 2) return true;
			return false;
		};
		return h;
	}

	// ---------- the dancer ----------
	// A giant dancer whose dance is the attack: its arms and legs are pink and hurt, its body and head are only a look.
	// A pose key: x, y, rot (deg), s (size), then the limbs' directions in degrees (0 = right, 90 = down, in the body's frame):
	//   left upper arm, left forearm, right upper arm, right forearm, left thigh, left shin, right thigh, right shin (12 numbers a key).
	// kt[i] = when pose i is reached; the move into it takes mv s (it starts at kt[i] - mv) and shows dashed wn s before it starts.
	// style 0 the neon dancer, 1 the x-ray skeleton (pink bones).
	const int F12NP = 12;
	static readonly int[] F12HF = { 4, 7, 10, 13 };
	public static Hz F12Dancer(int style, double[] kt, double[] kp, double mv, double wn, double lw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Dancer" }; int nk = kt.Length;
		var cur = new double[F12NP]; var nxt = new double[F12NP]; var J = new double[36]; var JN = new double[36];
		void pose(double t, double[] o)
		{
			// b: the last pose whose move has started; inside that move it is between pose b-1 and pose b
			int b = 0; while (b + 1 < nk && t >= kt[b + 1] - mv) b++;
			if (b >= 1 && t < kt[b])
			{
				double u = F12E(clamp((t - (kt[b] - mv)) / mv, 0, 1));
				for (int q = 0; q < F12NP; q++) { double v0 = kp[(b - 1) * F12NP + q]; o[q] = v0 + (kp[b * F12NP + q] - v0) * u; }
			}
			else for (int q = 0; q < F12NP; q++) o[q] = kp[b * F12NP + q];
		}
		// the joints: 0 neck, 1 hip, 2 l shoulder, 3 l elbow, 4 l hand, 5 r shoulder, 6 r elbow, 7 r hand, 8 l hip, 9 l knee, 10 l foot,
		// 11 r hip, 12 r knee, 13 r foot, 14 head (x, y pairs)
		void joints(double[] o, double[] j)
		{
			double x = o[0], y = o[1], ra = o[2] * Math.PI / 180, s = o[3], c = JS.cos(ra), sn = JS.sin(ra);
			void P(int k, double lx, double ly) { j[2 * k] = x + (lx * c - ly * sn) * s; j[2 * k + 1] = y + (lx * sn + ly * c) * s; }
			void Limb(int k0, int k1, double a, double len) { double an = (a + o[2]) * Math.PI / 180; j[2 * k1] = j[2 * k0] + JS.cos(an) * len * s; j[2 * k1 + 1] = j[2 * k0 + 1] + JS.sin(an) * len * s; }
			P(0, 0, -150); P(1, 0, 0); P(2, -46, -146); P(5, 46, -146); P(8, -26, 0); P(11, 26, 0); P(14, 0, -196);
			Limb(2, 3, o[4], 108); Limb(3, 4, o[5], 102); Limb(5, 6, o[6], 108); Limb(6, 7, o[7], 102);
			Limb(8, 9, o[8], 124); Limb(9, 10, o[9], 118); Limb(11, 12, o[10], 124); Limb(12, 13, o[11], 118);
		}
		int[] LS = { 2, 3, 3, 4, 5, 6, 6, 7, 8, 9, 9, 10, 11, 12, 12, 13 };
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		// the body and the head (harmless; drawn with the places, under every attack, so they never cover one)
		var JB = new double[36]; var cb = new double[F12NP];
		void body(double t)
		{
			if (t < t0 - wn || t > t1 + 0.5) return;
			pose(t, cb); joints(cb, JB); double s = cb[3], a = t < t0 ? 0.35 + 0.5 * F12K(t, t0 - wn, t0) : 1 - F12K(t, t1, t1 + 0.5), ra = cb[2] * Math.PI / 180;
			g.lineCap = "round"; g.lineJoin = "round";
			if (style == 0)
			{
				g.beginPath(); g.moveTo(JB[4], JB[5]); g.lineTo(JB[10], JB[11]); g.lineTo(JB[22], JB[23]); g.lineTo(JB[16], JB[17]); g.closePath();
				g.fillStyle = "rgba(40,22,70," + str(0.85 * a) + ")"; g.fill(); g.strokeStyle = F12Y(0.85 * a); g.lineWidth = 4; g.stroke();
				g.strokeStyle = "rgba(150,110,255," + str(0.55 * a) + ")"; g.lineWidth = 3; g.beginPath(); g.moveTo(JB[0], JB[1]); g.lineTo(JB[2], JB[3]); g.stroke();
				g.fillStyle = "rgba(40,22,70," + str(0.9 * a) + ")"; circle(JB[28], JB[29], 36 * s); g.strokeStyle = F12Y(0.9 * a); g.lineWidth = 4; g.beginPath(); g.arc(JB[28], JB[29], 36 * s, 0, TAU); g.stroke();
				// the hat (a dancer's look)
				g.save(); g.translate(JB[28], JB[29]); g.rotate(ra - 0.25); g.fillStyle = F12Y(0.75 * a); g.fillRect(-48 * s, -40 * s, 96 * s, 9 * s); g.fillRect(-28 * s, -70 * s, 56 * s, 32 * s); g.restore();
			}
			else
			{
				g.strokeStyle = F12X(0.6 * a); g.lineWidth = 7 * s; g.beginPath(); g.moveTo(JB[0], JB[1]); g.lineTo(JB[2], JB[3]); g.stroke();
				g.save(); g.translate(JB[2], JB[3]); g.rotate(ra); g.strokeStyle = F12X(0.5 * a); g.lineWidth = 5 * s;
				for (int i = 0; i < 5; i++) { double yy = (-132 + i * 24) * s; g.beginPath(); g.ellipse(0, yy, (54 - i * 4) * s, 14 * s, 0, Math.PI * 0.04, Math.PI * 0.96, false); g.stroke(); }
				g.strokeStyle = F12X(0.55 * a); g.lineWidth = 6 * s; g.beginPath(); g.moveTo(-50 * s, -146 * s); g.lineTo(50 * s, -146 * s); g.moveTo(-30 * s, 0); g.lineTo(30 * s, 0); g.stroke();
				g.restore();
				g.fillStyle = F12X(0.3 * a); circle(JB[28], JB[29], 34 * s); g.strokeStyle = F12X(0.8 * a); g.lineWidth = 3; g.beginPath(); g.arc(JB[28], JB[29], 34 * s, 0, TAU); g.stroke();
				g.fillStyle = "rgba(2,8,18," + str(0.9 * a) + ")"; circle(JB[28] - 12 * s, JB[29] - 4 * s, 8 * s); circle(JB[28] + 12 * s, JB[29] - 4 * s, 8 * s);
			}
			g.lineCap = "butt"; g.lineJoin = "miter";
		}
		h.o = new Dictionary<string, object> { ["body"] = (Action<double>)body };
		h.draw = t =>
		{
			if (t < t0 - wn || t > t1 + 0.5) return;
			pose(t, cur); joints(cur, J); double s = cur[3], rr = lw * s / 2;
			double a = t < t0 ? 0.35 + 0.5 * F12K(t, t0 - wn, t0) : 1 - F12K(t, t1, t1 + 0.5);
			bool live = t >= t0 && t <= t1;
			// the next pose, dashed
			if (live)
			{
				int i = 0; while (i + 1 < nk && t >= kt[i + 1] - mv) i++;
				if (i + 1 < nk && kt[i + 1] - mv - t <= wn)
				{
					for (int q = 0; q < F12NP; q++) nxt[q] = kp[(i + 1) * F12NP + q];
					joints(nxt, JN); g.beginPath();
					for (int k = 0; k < 16; k += 2) { g.moveTo(JN[2 * LS[k]], JN[2 * LS[k] + 1]); g.lineTo(JN[2 * LS[k + 1]], JN[2 * LS[k + 1] + 1]); }
					g.lineCap = "round"; F12Dashed(F12G(0.55), 3);
					g.beginPath(); for (int k = 0; k < 16; k += 2) { double ex = JN[2 * LS[k + 1]], ey = JN[2 * LS[k + 1] + 1]; g.moveTo(ex + rr, ey); g.arc(ex, ey, rr, 0, TAU); } F12Dashed(F12G(0.4), 2);
				}
			}
			// the limbs
			g.lineCap = "round"; g.lineJoin = "round";
			if (!live)
			{
				g.beginPath(); for (int k = 0; k < 16; k += 2) { g.moveTo(J[2 * LS[k]], J[2 * LS[k] + 1]); g.lineTo(J[2 * LS[k + 1]], J[2 * LS[k + 1] + 1]); }
				g.strokeStyle = F12G(0.5 * a); g.lineWidth = lw * s; g.stroke();
			}
			else
			{
				g.beginPath(); for (int k = 0; k < 16; k += 2) { g.moveTo(J[2 * LS[k]], J[2 * LS[k] + 1]); g.lineTo(J[2 * LS[k + 1]], J[2 * LS[k + 1] + 1]); }
				g.strokeStyle = F12P(0.92); g.lineWidth = lw * s; g.stroke();
				g.strokeStyle = F12L(0.85); g.lineWidth = Math.Max(2, lw * s * (style == 0 ? 0.22 : 0.3)); g.stroke();
				g.fillStyle = F12P(0.95);
				if (style == 0)
				{
					// the joints as rings, the hands as gloves (a star), the feet as shoes
					for (int k = 0; k < 16; k += 4) { int e = LS[k + 1]; circle(J[2 * e], J[2 * e + 1], rr * 1.15); }
					for (int q = 0; q < 2; q++)
					{
						int e = q == 0 ? 4 : 7, f2 = q == 0 ? 3 : 6; double ax = J[2 * e] - J[2 * f2], ay = J[2 * e + 1] - J[2 * f2 + 1], an = Math.Atan2(ay, ax);
						g.beginPath(); for (int i = 0; i < 10; i++) { double rq = i % 2 == 0 ? rr * 1.7 : rr * 0.9, aq = an + i * Math.PI / 5; if (i == 0) g.moveTo(J[2 * e] + JS.cos(aq) * rq, J[2 * e + 1] + JS.sin(aq) * rq); else g.lineTo(J[2 * e] + JS.cos(aq) * rq, J[2 * e + 1] + JS.sin(aq) * rq); }
						g.closePath(); g.fillStyle = F12P(0.95); g.fill(); g.fillStyle = F12L(0.9); circle(J[2 * e], J[2 * e + 1], rr * 0.45);
						e = q == 0 ? 10 : 13; f2 = q == 0 ? 9 : 12; ax = J[2 * e] - J[2 * f2]; ay = J[2 * e + 1] - J[2 * f2 + 1]; an = Math.Atan2(ay, ax);
						g.save(); g.translate(J[2 * e], J[2 * e + 1]); g.rotate(an + (q == 0 ? 1 : -1) * Math.PI / 2 * 0); g.fillStyle = F12P(0.95);
						g.beginPath(); g.ellipse(rr * 0.2, 0, rr * 1.2, rr * 1.0, 0, 0, TAU); g.fill(); g.strokeStyle = F12L(0.9); g.lineWidth = 2; g.stroke(); g.restore();
					}
				}
				else
				{
					for (int k = 0; k < 16; k += 2) { int e = LS[k + 1]; circle(J[2 * e], J[2 * e + 1], rr * 1.25); }
					for (int k = 0; k < 16; k += 2) { int e = LS[k]; circle(J[2 * e], J[2 * e + 1], rr * 1.2); }
				}
			}
			g.lineCap = "butt"; g.lineJoin = "miter";
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false;
			pose(t, cur); joints(cur, J); double rr = lw * cur[3] / 2 * (style == 1 ? 1.0 : 1.0);
			for (int k = 0; k < 16; k += 2)
			{
				int a = LS[k], b = LS[k + 1];
				if (F12Seg(p.x, p.y, J[2 * a], J[2 * a + 1], J[2 * b], J[2 * b + 1]) < rr + p.r - 2) return true;
			}
			// the gloves and the shoes reach a little further
			if (style == 0) foreach (int e in F12HF) if (hypot(p.x - J[2 * e], p.y - J[2 * e + 1]) < rr * 1.35 + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// the light creeping in from the edges: everything within d of the screen's edge hurts (d between keyframes kb seconds / kd px);
	// where it goes next shows dashed wn s before
	public static Hz F12Creep(double[] kb, double[] kd, double wn, double tw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Creep" };
		double D(double t) => F12Lerp(kb, kd, t) * (1 - F12K(t, t1, t1 + 0.5));
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > t1 + 0.5) return;
			if (t < t0) { double k = F12K(t, tw, t0), d0 = kd[0]; g.beginPath(); g.rect(d0, d0, W - 2 * d0, H - 2 * d0); F12Dashed(F12G(0.3 + 0.5 * k), 2); return; }
			double d = D(t); int j = F12Next(kb, kd, t, wn);
			if (j >= 0 && kd[j] > d + 4 && t < t1) { g.beginPath(); g.rect(kd[j], kd[j], W - 2 * kd[j], H - 2 * kd[j]); F12Dashed(F12G(0.55), 2); }
			if (d < 1) return;
			g.fillStyle = F12P(0.5); g.fillRect(0, 0, W, d); g.fillRect(0, H - d, W, d); g.fillRect(0, d, d, H - 2 * d); g.fillRect(W - d, d, d, H - 2 * d);
			// the edge: little curls of light creeping along
			g.strokeStyle = F12P(0.95); g.lineWidth = 4; g.strokeRect(d, d, W - 2 * d, H - 2 * d);
			g.fillStyle = F12L(0.9);
			double per = 2 * (W + H - 4 * d), off = (t * 140) % 60;
			for (double q = off; q < per; q += 60)
			{
				double x, y, iw = W - 2 * d, ih = H - 2 * d;
				if (q < iw) { x = d + q; y = d; } else if (q < iw + ih) { x = W - d; y = d + q - iw; } else if (q < 2 * iw + ih) { x = W - d - (q - iw - ih); y = H - d; } else { x = d; y = H - d - (q - 2 * iw - ih); }
				circle(x, y, 4);
			}
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false; double d = D(t); if (d < 1) return false;
			return p.x - p.r + 2 < d || p.x + p.r - 2 > W - d || p.y - p.r + 2 < d || p.y + p.r - 2 > H - d;
		};
		return h;
	}

	// ---------- the magic ----------
	// a river of light rushing up the screen: its middle at y0 at ts, rising at vy px/s (negative), a wave A tall over lam px (ph phase),
	// th thick. A gold bubble (radius br) floats along it, from bx0 at bv px/s, bouncing off the sides: inside the bubble it is safe.
	// It shows as dashed arrows on the bottom edge from tw.
	public static Hz F12River(double y0, double vy, double A, double lam, double ph, double th, double bx0, double bv, double br, double tw, double ts, double fl)
	{
		var h = new Hz { kind = "F12River" };
		double yc(double x, double t) => y0 + vy * (t - ts) + A * JS.sin(TAU * x / lam + ph);
		double bx(double t)
		{
			double lo = 80, hi = W - 80, span = hi - lo, q = (bx0 - lo + bv * (t - ts)) % (2 * span); if (q < 0) q += 2 * span;
			return lo + (q < span ? q : 2 * span - q);
		}
		bool gone(double t) => y0 + vy * (t - ts) < -A - th - 10;
		h.update = (t, dt) => { if (t > ts && gone(t)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ts)
			{
				double k = F12K(t, tw, ts); g.strokeStyle = F12G(0.35 + 0.5 * k); g.lineWidth = 3;
				for (double x = 100; x < W; x += 180) { g.beginPath(); g.moveTo(x - 14, H - 12); g.lineTo(x, H - 30); g.lineTo(x + 14, H - 12); g.stroke(); }
				double b0 = bx(ts); g.beginPath(); g.arc(b0, H - 30, 18, 0, TAU); g.strokeStyle = F12Y(0.4 + 0.5 * k); g.lineWidth = 2; g.stroke();
				return;
			}
			if (gone(t)) return;
			g.beginPath();
			for (double x = -10; x <= W + 10; x += 16) { double y = yc(x, t) - th / 2; if (x < -9) g.moveTo(x, y); else g.lineTo(x, y); }
			for (double x = W + 10; x >= -10; x -= 16) g.lineTo(x, yc(x, t) + th / 2);
			g.closePath(); g.fillStyle = F12P(0.5); g.fill(); g.strokeStyle = F12P(0.95); g.lineWidth = 3; g.stroke();
			// the rush inside it
			g.beginPath();
			for (double x = -10; x <= W + 10; x += 16) { double y = yc(x, t); if (x < -9) g.moveTo(x, y); else g.lineTo(x, y); }
			g.strokeStyle = F12L(0.8); g.lineWidth = 3; g.setLineDash(F12DA); g.lineDashOffset = -fl * (t - ts); g.stroke(); g.setLineDash(F12DN); g.lineDashOffset = 0;
			// the bubble (safe)
			double x0 = bx(t), y1 = yc(x0, t);
			g.fillStyle = "#071626"; circle(x0, y1, br); g.strokeStyle = F12Y(0.95); g.lineWidth = 3; g.beginPath(); g.arc(x0, y1, br, 0, TAU); g.stroke();
			g.strokeStyle = F12Y(0.5); g.lineWidth = 2; g.beginPath(); g.arc(x0, y1, br * 0.72, -2.6, -1.4); g.stroke();
			g.fillStyle = F12Y(0.8); circle(x0 + br * 0.35, y1 - br * 0.4, 4);
		};
		h.hit = (p, t) =>
		{
			if (t < ts || gone(t)) return false;
			if (Math.Abs(p.y - yc(p.x, t)) >= th / 2 + p.r - 2) return false;
			double x0 = bx(t); return hypot(p.x - x0, p.y - yc(x0, t)) > br - p.r;
		};
		return h;
	}

	// ---------- the sun ----------
	// the sun (its middle moving between keyframes kb seconds / kx, ky) with n pink rays turning (keyframes rb seconds / ra degrees),
	// each ray hw degrees each side, from the disc (rd, it burns too) to Lr. It hurts from t0 to t1; before that it shows dashed, and the place
	// the rays turn to next shows dashed wn s before
	public static Hz F12Sun(double[] kb, double[] kx, double[] ky, double[] rb, double[] ra, int n, double hw, double rd, double Lr, double wn, double tw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Sun" }; double hwr = hw * Math.PI / 180;
		void rays(double cx, double cy, double an)
		{
			g.beginPath();
			for (int i = 0; i < n; i++)
			{
				double q = an + i * TAU / n;
				g.moveTo(cx + JS.cos(q - hwr) * rd, cy + JS.sin(q - hwr) * rd); g.lineTo(cx + JS.cos(q - hwr) * Lr, cy + JS.sin(q - hwr) * Lr);
				g.arc(cx, cy, Lr, q - hwr, q + hwr); g.lineTo(cx + JS.cos(q + hwr) * rd, cy + JS.sin(q + hwr) * rd); g.arc(cx, cy, rd, q + hwr, q - hwr, true);
			}
		}
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > t1 + 0.5) return;
			double cx = F12Lerp(kb, kx, t), cy = F12Lerp(kb, ky, t), an = F12Lerp(rb, ra, t) * Math.PI / 180, f = 1 - F12K(t, t1, t1 + 0.5);
			// the light round it
			var gr = g.createRadialGradient(cx, cy, rd * 0.5, cx, cy, rd * 4); gr.addColorStop(0, F12Y(0.4 * f)); gr.addColorStop(1, F12Y(0)); g.fillStyle = gr; g.fillRect(cx - rd * 4, cy - rd * 4, rd * 8, rd * 8);
			if (t < t0) { double k = F12K(t, tw, t0); rays(cx, cy, an); F12Dashed(F12G(0.3 + 0.55 * k), 2); }
			else
			{
				int j = F12Next(rb, ra, t, wn); if (j >= 0 && t < t1) { rays(cx, cy, ra[j] * Math.PI / 180); F12Dashed(F12G(0.5), 2); }
				rays(cx, cy, an); g.fillStyle = F12P(0.45 * f); g.fill(); g.strokeStyle = F12P(0.95 * f); g.lineWidth = 3; g.stroke();
			}
			// the disc: white hot while it burns (it hurts too: nobody hides in the sun), gold before; engraved rings and points turning
			bool hot = t >= t0 && t <= t1;
			g.fillStyle = hot ? F12P(0.95 * f) : F12Y(0.9 * f); circle(cx, cy, rd);
			g.strokeStyle = hot ? "rgba(140,8,50," + str(0.8 * f) + ")" : "rgba(150,90,30," + str(0.7 * f) + ")"; g.lineWidth = 2;
			g.beginPath(); g.arc(cx, cy, rd * 0.78, 0, TAU); g.stroke(); g.beginPath(); g.arc(cx, cy, rd * 0.5, 0, TAU); g.stroke();
			g.beginPath(); for (int i = 0; i < 16; i++) { double q = -an * 0.5 + i * TAU / 16; g.moveTo(cx + JS.cos(q) * rd * 0.52, cy + JS.sin(q) * rd * 0.52); g.lineTo(cx + JS.cos(q) * rd * 0.76, cy + JS.sin(q) * rd * 0.76); } g.stroke();
			g.fillStyle = hot ? F12L(0.95 * f) : "rgba(255,244,214," + str(0.8 * f) + ")"; circle(cx, cy, rd * 0.22);
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false;
			double cx = F12Lerp(kb, kx, t), cy = F12Lerp(kb, ky, t), an = F12Lerp(rb, ra, t) * Math.PI / 180, d = hypot(p.x - cx, p.y - cy);
			if (d < rd + p.r - 2) return true;
			if (d - p.r > Lr) return false;
			double pa = Math.Atan2(p.y - cy, p.x - cx), room = F12Room(d, p.r - 2);
			for (int i = 0; i < n; i++) if (F12AngD(pa, an + i * TAU / n) < hwr + room) return true;
			return false;
		};
		return h;
	}

	// the ring of spotlights closing in: outside the circle (middle and radius between keyframes kb seconds / kx, ky, kr) it hurts;
	// before t0 its circle shows dashed, and where it shrinks to next shows dashed wn s before
	public static Hz F12Zone(double[] kb, double[] kx, double[] ky, double[] kr, double wn, double tw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Zone" };
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		double R(double t) => F12Lerp(kb, kr, t) + 900 * F12E(F12K(t, t1, t1 + 0.5));
		h.draw = t =>
		{
			if (t < tw || t > t1 + 0.5) return;
			double cx = F12Lerp(kb, kx, t), cy = F12Lerp(kb, ky, t), r = R(t);
			if (t < t0) { double k = F12K(t, tw, t0); g.beginPath(); g.arc(cx, cy, r, 0, TAU); F12Dashed(F12G(0.3 + 0.55 * k), 3); return; }
			int j = F12Next(kb, kr, t, wn);
			if (j >= 0 && t < t1) { g.beginPath(); g.arc(kx[j], ky[j], kr[j], 0, TAU); F12Dashed(F12G(0.6), 3); }
			g.beginPath(); g.arc(cx, cy, r + 800, 0, TAU); g.closePath(); g.strokeStyle = F12P(0.5); g.lineWidth = 1600; g.stroke();
			g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.strokeStyle = F12P(0.95); g.lineWidth = 4; g.stroke();
			// the lamps on its edge, shining in
			for (int i = 0; i < 20; i++)
			{
				double q = i * TAU / 20 + t * 0.4, x = cx + JS.cos(q) * (r + 12), y = cy + JS.sin(q) * (r + 12);
				g.fillStyle = F12L(0.95); circle(x, y, 6); g.fillStyle = F12P(0.95); circle(x, y, 3);
			}
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false;
			double cx = F12Lerp(kb, kx, t), cy = F12Lerp(kb, ky, t);
			return hypot(p.x - cx, p.y - cy) > R(t) - p.r + 2;
		};
		return h;
	}

	// ---------- can't stop ----------
	// the endless line: a line of light running round a figure 8 (middle cx, cy, ax wide, ay tall, the 8 turned by keyframes rb / ra degrees)
	// without stopping: its head at s0 + om (t - t0) along the 8, len long (in the 8's own measure), in nseg pieces with gaps (gapf of each
	// piece) to cross through, w thick. The 8's track is drawn gold from tw (harmless), the line hurts from t0 to t1.
	public static Hz F12Snake(double cx, double cy, double ax, double ay, double[] rb, double[] ra, double s0, double om, double len, int nseg, double gapf, double w, double tw, double t0, double t1)
	{
		var h = new Hz { kind = "F12Snake" };
		void pt(double s, double c, double sn, out double x, out double y)
		{
			double ss = JS.sin(s), cs = JS.cos(s), dd = 1 + ss * ss, lx = ax * cs / dd, ly = ay * 2.83 * ss * cs / dd;
			x = cx + lx * c - ly * sn; y = cy + lx * sn + ly * c;
		}
		double L(double t) => Math.Min(len, Math.Max(0, om * (t - t0)));
		h.update = (t, dt) => { if (t > t1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > t1 + 0.4) return;
			double an = F12Lerp(rb, ra, t) * Math.PI / 180, c = JS.cos(an), sn = JS.sin(an), f = 1 - F12K(t, t1, t1 + 0.4);
			g.beginPath(); for (int i = 0; i <= 96; i++) { pt(i * TAU / 96, c, sn, out var x, out var y); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
			g.strokeStyle = F12Y(0.22 * f); g.lineWidth = 2; g.stroke();
			if (t < t0)
			{
				double k = F12K(t, tw, t0); pt(s0, c, sn, out var hx, out var hy); g.strokeStyle = F12G(0.4 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(hx, hy, w, 0, TAU); g.stroke();
				g.beginPath(); for (int i = 0; i <= 20; i++) { pt(s0 + i * 0.6 / 20, c, sn, out var x, out var y); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); } F12Dashed(F12G(0.4 + 0.5 * k), 3);
				return;
			}
			double hs = s0 + om * (t - t0), l = L(t), pl = len / nseg;
			g.lineCap = "round";
			for (int q = 0; q < nseg; q++)
			{
				double a1 = hs - q * pl, a0 = a1 - pl * (1 - gapf); if (a1 < hs - l) break; a0 = Math.Max(a0, hs - l);
				g.beginPath(); int m = Math.Max(2, (int)((a1 - a0) / 0.04));
				for (int i = 0; i <= m; i++) { pt(a0 + (a1 - a0) * i / m, c, sn, out var x, out var y); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
				g.strokeStyle = F12P(0.95 * f); g.lineWidth = w; g.stroke(); g.strokeStyle = F12L(0.85 * f); g.lineWidth = Math.Max(2, w * 0.3); g.stroke();
			}
			g.lineCap = "butt";
			pt(hs, c, sn, out var px, out var py); g.fillStyle = F12L(0.95 * f); circle(px, py, w * 0.75);
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1) return false;
			double an = F12Lerp(rb, ra, t) * Math.PI / 180, c = JS.cos(an), sn = JS.sin(an), hs = s0 + om * (t - t0), l = L(t), pl = len / nseg, lim = w / 2 + p.r - 2;
			for (int q = 0; q < nseg; q++)
			{
				double a1 = hs - q * pl, a0 = a1 - pl * (1 - gapf); if (a1 < hs - l) break; a0 = Math.Max(a0, hs - l);
				int m = Math.Max(2, (int)((a1 - a0) / 0.03)); double lx = 0, ly = 0;
				for (int i = 0; i <= m; i++)
				{
					pt(a0 + (a1 - a0) * i / m, c, sn, out var x, out var y);
					if (i > 0 && F12Seg(p.x, p.y, lx, ly, x, y) < lim) return true;
					lx = x; ly = y;
				}
			}
			return false;
		};
		return h;
	}

	// the end: gold light from the middle and the song's words (harmless)
	public static Hz F12End(double t0)
	{
		var h = new Hz { kind = "F12End" };
		h.update = (t, dt) => { if (t > t0 + 4) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = F12K(t, t0, t0 + 0.7), f = 1 - F12K(t, t0 + 2.6, t0 + 4);
			var gr = g.createRadialGradient(W / 2, H / 2, 10, W / 2, H / 2, 200 + 760 * k); gr.addColorStop(0, F12Y(0.75 * f)); gr.addColorStop(0.5, F12Y(0.28 * f)); gr.addColorStop(1, F12Y(0)); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			for (int i = 0; i < 24; i++) { double q = i * TAU / 24, r0 = 60 + 500 * k, r1 = r0 + 60; g.strokeStyle = F12Y(0.5 * f); g.lineWidth = 3; g.beginPath(); g.moveTo(W / 2 + JS.cos(q) * r0, H / 2 + JS.sin(q) * r0); g.lineTo(W / 2 + JS.cos(q) * r1, H / 2 + JS.sin(q) * r1); g.stroke(); }
			g.save(); g.globalAlpha = f * k; g.font = "400 58px " + TFONT; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = "#fff4dc"; g.fillText("CAN'T STOP THE FEELING", W / 2, H / 2); g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_F12.json) ----------
	// Only pink hurts, so nothing may hide an attack: the places are drawn first, under every attack (the first thing in the list draws them)
	// (the places first, then the looks that stand on them: the dancer's body)
	static readonly List<(Hz h, Action<double> d)> F12Places = new(), F12Looks = new();
	static void F12Under(Hz h, bool look = false) { (look ? F12Looks : F12Places).Add((h, h.draw)); h.draw = t => F12First(h, t); hazards.Add(h); }
	static void F12Add(Hz h) { var d = h.draw; h.draw = t => { F12First(h, t); d(t); }; hazards.Add(h); }
	static void F12First(Hz h, double t)
	{
		if (hazards.Count == 0 || hazards[0] != h) return;
		F12Places.RemoveAll(q => !hazards.Contains(q.h)); F12Looks.RemoveAll(q => !hazards.Contains(q.h));
		foreach (var q in F12Places) q.d(t);
		foreach (var q in F12Looks) q.d(t);
	}
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F12wn(DodgeEv e, string k = "wn") => e.num(k) * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F12ez(double v, double ez) => easy ? v * ez : v;
	static bool F12skip(DodgeEv e) => easy && e.flag("ezskip");
	static double[] F12Bs(DodgeEv e, string k) => e.nums(k).Select(B).ToArray();

	static readonly int _kf12stage = DodgeKind("F12stage", e => () => F12Under(F12Stage(e.i("sc"), B(e.b), B(e.num("to")), F12Bs(e, "kb"), e.nums("ka"), e.nums("bx"))));
	static readonly int _kf12fx = DodgeKind("F12fx", e => () => F12Add(F12Fx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2)))));
	static readonly int _kf12bolt = DodgeKind("F12bolt", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit");
		F12Add(F12Bolt(e.nums("pts"), F12ez(e.num("w", 22), 0.8), B(hit - wn), B(hit), B(hit + e.num("dur", 1)), e.i("sd")));
	}; });
	static readonly int _kf12wave = DodgeKind("F12wave", e => { double wn = F12wn(e); return () =>
	{
		var sp = e.nums("sp"); var st = new List<double>(); var so = new List<double>();
		for (int i = 0; i + 2 < sp.Length; i += 3) { if (easy && sp[i + 2] > 0) continue; st.Add(B(sp[i])); so.Add(sp[i + 1]); }
		double hit = e.num("hit");
		F12Add(F12Wave(e.num("cy", H / 2), F12Bs(e, "kb"), e.nums("ka").Select(v => F12ez(v, 0.8)).ToArray(), e.num("lam"), e.num("v"), F12ez(e.num("gap"), 1.2), B(hit - wn), B(hit), B(e.num("to")),
			st.ToArray(), so.ToArray(), F12ez(e.num("sv"), 0.85), e.num("sr", 16), F12wn(e, "swn") * SPB));
	}; });
	static readonly int _kf12proj = DodgeKind("F12proj", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit"), x0 = e.num("x0"), y0 = e.num("y0"), x1 = e.num("x1"), y1 = e.num("y1");
		// aim: thrown at where the player is when its way shows (as far as len)
		if (e.flag("aim") && player != null) { double an = Math.Atan2(player.y - y0, player.x - x0), ln = e.num("len", 900); x1 = x0 + JS.cos(an) * ln; y1 = y0 + JS.sin(an) * ln; }
		F12Add(F12Proj(e.i("sh"), x0, y0, x1, y1, e.num("qx"), e.num("qy"), e.has("qx"), e.flag("back"), F12ez(e.num("r", 20), 0.85), e.num("spin"), B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.15 : 1), e.i("sd")));
	}; });
	static readonly int _kf12ring = DodgeKind("F12ring", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit");
		F12Add(F12Ring(e.num("x"), e.num("y"), e.num("r0"), e.num("r1"), F12ez(e.num("w", 30), 0.8), e.nums("gaps"), F12ez(e.num("gw", 14), 1.25), B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.15 : 1)));
	}; });
	static readonly int _kf12sole = DodgeKind("F12sole", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit");
		F12Add(F12Sole(e.num("x"), e.num("y"), e.num("a"), F12ez(e.num("L", 240), 0.88), e.flag("right"), B(hit - wn), B(hit), F12ez(e.num("hold", 1), 0.8) * SPB));
	}; });
	static readonly int _kf12beam = DodgeKind("F12beam", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; var kb = F12Bs(e, "kb"); var ka = e.nums("ka"); double t0 = B(e.num("hit"));
		// aim: its last place is where the player is when it shows
		// aim: it points where the player is when it shows, and stays there
		if (e.flag("aim") && player != null && ka.Length > 0) { double an = Math.Atan2(player.y - e.num("oy"), player.x - e.num("ox")) * 180 / Math.PI; ka = ka.Select(_ => an).ToArray(); }
		F12Add(F12Beam(e.num("ox"), e.num("oy"), kb, ka, F12ez(e.num("ha", 8), 0.85), e.num("len", 1500), wn * SPB, t0 - wn * SPB, t0, B(e.num("to"))));
	}; });
	static readonly int _kf12shape = DodgeKind("F12shape", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit");
		F12Add(F12Shape(e.num("x"), e.num("y"), e.nums("pts"), e.num("rot"), B(hit - wn), B(hit), F12ez(e.num("hold", 1), 0.8) * SPB));
	}; });
	static readonly int _kf12dancer = DodgeKind("F12dancer", e => { double wn = F12wn(e); return () =>
	{
		var h = F12Dancer(e.i("style"), F12Bs(e, "kt"), e.nums("kp"), e.num("mv", 0.4) * SPB * (easy ? 1.25 : 1), wn * SPB, F12ez(e.num("lw", 34), 0.85), B(e.num("hit")), B(e.num("to")));
		var bd = new Hz { kind = "F12DancerBody", draw = (Action<double>)h.o["body"] }; bd.update = (t, dt) => { if (h.dead) bd.dead = true; };
		F12Under(bd, true); F12Add(h);
	}; });
	static readonly int _kf12creep = DodgeKind("F12creep", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double t0 = B(e.num("hit"));
		F12Add(F12Creep(F12Bs(e, "kb"), e.nums("kd").Select(v => F12ez(v, 0.75)).ToArray(), wn * SPB, t0 - wn * SPB, t0, B(e.num("to"))));
	}; });
	static readonly int _kf12river = DodgeKind("F12river", e => { double wn = F12wn(e); return () =>
	{
		if (F12skip(e)) return; double hit = e.num("hit");
		F12Add(F12River(e.num("y0", H + 50), F12ez(e.num("vy"), 0.85), e.num("A"), e.num("lam", 900), e.num("ph"), F12ez(e.num("th", 60), 0.85), e.num("bx"), F12ez(e.num("bv"), 0.8), F12ez(e.num("br", 64), 1.15),
			B(hit - wn), B(hit), e.num("fl", 260)));
	}; });
	static readonly int _kf12sun = DodgeKind("F12sun", e => { double wn = F12wn(e); return () =>
	{
		double t0 = B(e.num("hit"));
		F12Add(F12Sun(F12Bs(e, "kb"), e.nums("kx"), e.nums("ky"), F12Bs(e, "rb"), e.nums("ra"), e.i("n", 8), F12ez(e.num("hw", 7), 0.8), e.num("rd", 70), e.num("len", 1500), wn * SPB, t0 - wn * SPB, t0, B(e.num("to"))));
	}; });
	static readonly int _kf12zone = DodgeKind("F12zone", e => { double wn = F12wn(e); return () =>
	{
		double t0 = B(e.num("hit"));
		F12Add(F12Zone(F12Bs(e, "kb"), e.nums("kx"), e.nums("ky"), e.nums("kr").Select(v => F12ez(v, 1.2)).ToArray(), wn * SPB, t0 - wn * SPB, t0, B(e.num("to"))));
	}; });
	static readonly int _kf12snake = DodgeKind("F12snake", e => { double wn = F12wn(e); return () =>
	{
		double t0 = B(e.num("hit"));
		F12Add(F12Snake(e.num("cx", W / 2), e.num("cy", H / 2), e.num("ax"), e.num("ay"), F12Bs(e, "rb"), e.nums("ra"), e.num("s0"), F12ez(e.num("om"), 0.85), e.num("len"), e.i("nseg", 3),
			F12ez(e.num("gap", 0.3), 1.3), F12ez(e.num("w", 26), 0.85), t0 - wn * SPB, t0, B(e.num("to"))));
	}; });
	static readonly int _kf12end = DodgeKind("F12end", e => () => F12Add(F12End(B(e.b))));
}
