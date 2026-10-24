using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F11 "أجمل غرامي" (Abdullah Al Farwan), miracle's level: a night in the desert, two lovers and what stands between them.
// part 1 the eyes (the sadu bands, the kohl eyes that look only at you, the walls of "the circumstances" closing on you),
// part 2 the night and the road (the winding road through the sand, the shooting stars, the coffee pot, the incense),
// part 3 the heart and the hands (the heart's waves, the hands coming together with their fingers between each other, the henna turning).
// The moon, the sand, the tent, the eyes, the coffee pot, the burner and the gold heart are only the look of things; only pink hurts.
// Every hazard is a pure function of time once it exists. The data is data/dodge_F11.json (made by tools/friend11/gen.py).
public static partial class G
{
	static double F11k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double F11e(double u) => u * u * (3 - 2 * u);
	static string F11P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string F11D(double a) => $"rgba(150,10,58,{str(clamp(a, 0, 1))})";   // the dark pink of the patterns inside a pink thing
	static string F11G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string F11Y(double a) => $"rgba(236,190,110,{str(clamp(a, 0, 1))})";   // gold: only a look
	static double F11h(int i) { unchecked { uint x = (uint)(i * 2654435761u) ^ 0x51ed27a3u; x ^= x >> 15; x *= 0x85ebca6bu; x ^= x >> 13; return (x % 10000) / 10000.0; } }
	static double F11Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// the circle of the player touches the box x0..x1, y0..y1
	static bool F11Box(Player p, double x0, double y0, double x1, double y1)
	{
		if (x1 <= x0 || y1 <= y0) return false;
		double cx = clamp(p.x, x0, x1), cy = clamp(p.y, y0, y1); return hypot(p.x - cx, p.y - cy) < p.r - 2 || (p.x > x0 && p.x < x1 && p.y > y0 && p.y < y1);
	}
	static void F11Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static void F11DashBox(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 9, 8 }); g.strokeRect(x0, y0, x1 - x0, y1 - y0); g.setLineDash(new double[0]);
	}
	// a value that is v0, then steps to kv[i] at each time kb[i] (seconds), each step eased over es seconds
	static double F11Step(double[] kb, double[] kv, double t, double es, double v0)
	{
		int n = kb.Length; if (n == 0 || t < kb[0]) return v0;
		int i = 0; while (i < n - 1 && t >= kb[i + 1]) i++;
		double prev = i == 0 ? v0 : kv[i - 1], u = clamp((t - kb[i]) / es, 0, 1); return prev + (kv[i] - prev) * F11e(u);
	}
	static double F11Lerp(double[] kb, double[] kv, double t)
	{
		int n = kb.Length; if (n == 0) return 0; if (t <= kb[0]) return kv[0]; if (t >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && t >= kb[i + 1]) i++;
		double u = (t - kb[i]) / (kb[i + 1] - kb[i]); return kv[i] + (kv[i + 1] - kv[i]) * F11e(u);
	}

	// ---------- the stage (harmless) ----------
	// a line of dunes: their top at y over x
	static double F11Dune(double x, double y0, double a, int s) => y0 - a * (0.55 * JS.sin(x / 210.0 + s) + 0.3 * JS.sin(x / 97.0 + s * 2.1) + 0.15 * JS.sin(x / 43.0 + s * 3.3));
	static void F11Dunes(double y0, double a, int s, string col)
	{
		g.fillStyle = col; g.beginPath(); g.moveTo(-10, H + 10);
		for (double x = -10; x <= W + 10; x += 16) g.lineTo(x, F11Dune(x, y0, a, s));
		g.lineTo(W + 10, H + 10); g.closePath(); g.fill();
	}
	static void F11Stars(int n, double t, double yMax, double a)
	{
		for (int i = 0; i < n; i++)
		{
			double tw = 0.55 + 0.45 * JS.sin(t * (1.2 + F11h(i + 500) * 2) + i);
			g.fillStyle = $"rgba(255,244,222,{str(a * (0.25 + 0.5 * F11h(i + 77)) * tw)})"; double s = F11h(i + 200) > 0.85 ? 3 : 2;
			g.fillRect(F11h(i) * W, F11h(i + 99) * yMax, s, s);
		}
	}
	static void F11Moon(double x, double y, double r, double a)
	{
		var gr = g.createRadialGradient(x, y, r * 0.6, x, y, r * 3.2); gr.addColorStop(0, $"rgba(255,236,196,{str(0.28 * a)})"); gr.addColorStop(1, "rgba(255,236,196,0)"); g.fillStyle = gr; circle(x, y, r * 3.2);
		g.fillStyle = $"rgba(246,234,206,{str(a)})"; circle(x, y, r);
		g.fillStyle = $"rgba(214,198,166,{str(0.45 * a)})"; circle(x - r * 0.3, y - r * 0.2, r * 0.2); circle(x + r * 0.28, y + r * 0.25, r * 0.14); circle(x + r * 0.1, y - r * 0.45, r * 0.09);
	}
	// the tent: a black goat-hair tent (bait al-sha'ar) on the dune
	static void F11Tent(double x, double y, double s)
	{
		g.fillStyle = "#0b0610"; g.beginPath(); g.moveTo(x - 90 * s, y); g.lineTo(x - 70 * s, y - 38 * s); g.lineTo(x - 20 * s, y - 52 * s); g.lineTo(x + 30 * s, y - 50 * s); g.lineTo(x + 80 * s, y - 34 * s); g.lineTo(x + 96 * s, y); g.closePath(); g.fill();
		g.fillStyle = F11Y(0.35); g.fillRect(x - 12 * s, y - 26 * s, 22 * s, 26 * s);   // the warm light inside
		g.strokeStyle = "#0b0610"; g.lineWidth = 2; g.beginPath(); g.moveTo(x - 70 * s, y - 38 * s); g.lineTo(x - 120 * s, y + 4); g.moveTo(x + 80 * s, y - 34 * s); g.lineTo(x + 130 * s, y + 4); g.stroke();
	}
	static void F11Palm(double x, double y, double s)
	{
		g.strokeStyle = "#0b0610"; g.lineWidth = 7 * s; g.beginPath(); g.moveTo(x, y); g.quadraticCurveTo(x + 10 * s, y - 70 * s, x - 6 * s, y - 130 * s); g.stroke();
		g.fillStyle = "#0b0610";
		for (int i = 0; i < 7; i++)
		{
			double a = -Math.PI / 2 + (i - 3) * 0.48, cx = x - 6 * s, cy = y - 130 * s;
			g.beginPath(); g.moveTo(cx, cy); g.quadraticCurveTo(cx + JS.cos(a - 0.3) * 50 * s, cy + JS.sin(a - 0.3) * 50 * s - 10 * s, cx + JS.cos(a) * 70 * s, cy + JS.sin(a) * 70 * s + 22 * s);
			g.quadraticCurveTo(cx + JS.cos(a + 0.2) * 40 * s, cy + JS.sin(a + 0.2) * 40 * s, cx, cy); g.fill();
		}
	}
	// part 1 the desert under the moon, 2 the deep night (the road), 3 the wedding night (henna, gold)
	public static Hz F11Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "F11Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var sk = g.createLinearGradient(0, 0, 0, H); sk.addColorStop(0, "#06051a"); sk.addColorStop(0.6, "#1a1034"); sk.addColorStop(1, "#3a1c38"); g.fillStyle = sk; g.fillRect(0, 0, W, H);
				F11Stars(70, t, 430, 1); F11Moon(1060, 128, 54, 1);
				F11Dunes(560, 40, 1, "#24142e"); F11Tent(250, F11Dune(250, 610, 30, 2) + 4, 1); F11Palm(1150, F11Dune(1150, 610, 30, 2) + 6, 0.9);
				F11Dunes(610, 30, 2, "#190e22"); F11Dunes(668, 22, 3, "#100815");
			}
			else if (part == 2)
			{
				var sk = g.createLinearGradient(0, 0, 0, H); sk.addColorStop(0, "#030310"); sk.addColorStop(1, "#110b24"); g.fillStyle = sk; g.fillRect(0, 0, W, H);
				// the milky way, a soft band across the sky
				g.save(); g.translate(W / 2, H / 2); g.rotate(-0.5); var mw = g.createLinearGradient(0, -90, 0, 90); mw.addColorStop(0, "rgba(160,140,220,0)"); mw.addColorStop(0.5, "rgba(160,140,220,0.1)"); mw.addColorStop(1, "rgba(160,140,220,0)"); g.fillStyle = mw; g.fillRect(-900, -90, 1800, 180); g.restore();
				F11Stars(130, t, H, 0.9); F11Moon(150, 96, 30, 0.8);
			}
			else
			{
				var gr = g.createRadialGradient(W / 2, H / 2, 40, W / 2, H / 2, 760); gr.addColorStop(0, "#35101f"); gr.addColorStop(0.55, "#1c0812"); gr.addColorStop(1, "#080306"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				// faint henna lines in gold
				g.strokeStyle = F11Y(0.09); g.lineWidth = 2;
				for (int r = 1; r <= 5; r++) { g.beginPath(); g.arc(W / 2, H / 2, 70 * r, 0, TAU); g.stroke(); }
				for (int i = 0; i < 16; i++)
				{
					double an = i * TAU / 16 + t * 0.05, c = JS.cos(an), s = JS.sin(an);
					g.beginPath(); g.moveTo(W / 2 + c * 120, H / 2 + s * 120); g.quadraticCurveTo(W / 2 + JS.cos(an + 0.2) * 260, H / 2 + JS.sin(an + 0.2) * 260, W / 2 + c * 360, H / 2 + s * 360); g.stroke();
				}
			}
			g.restore();
		};
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F11Fx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F11fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- part 1: the eyes ----------
	// a sadu band: a woven strip across the screen (vert: a column moving in x, else a row moving in y) from p0, stepping by st at each of ts
	// (each step eased over 0.12 s). Its opening (the diamond of the weave) is at gs[i] after step i, gh half high. It hurts from ts[0] on.
	public static Hz F11Band(bool vert, double p0, double st, double bw, double[] ts, double[] gs, double gh, double tw)
	{
		var h = new Hz { kind = "F11Band" }; int n = ts.Length; double te = ts[n - 1] + 0.4;
		double[] pv = new double[n], gv = new double[n];
		for (int i = 0; i < n; i++) { pv[i] = p0 + st * (i + 1); gv[i] = gs[i]; }
		double pos(double t) => F11Step(ts, pv, t, 0.12, p0);
		double gap(double t) => F11Step(ts, gv, t, 0.12, gs[0]);
		int next(double t) { for (int i = 0; i < n; i++) if (ts[i] > t) return i; return -1; }
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		void band(double c, double gy, string col, string pat, bool line)
		{
			// the strip with its opening, then the woven triangles on it
			double L = vert ? H : W;
			if (line)
			{
				if (vert) { F11DashBox(c - bw / 2, -5, c + bw / 2, gy - gh, col); F11DashBox(c - bw / 2, gy + gh, c + bw / 2, H + 5, col); }
				else { F11DashBox(-5, c - bw / 2, gy - gh, c + bw / 2, col); F11DashBox(gy + gh, c - bw / 2, W + 5, c + bw / 2, col); }
				return;
			}
			g.fillStyle = col;
			if (vert) { g.fillRect(c - bw / 2, -5, bw, gy - gh + 5); g.fillRect(c - bw / 2, gy + gh, bw, H + 5 - gy - gh); }
			else { g.fillRect(-5, c - bw / 2, gy - gh + 5, bw); g.fillRect(gy + gh, c - bw / 2, W + 5 - gy - gh, bw); }
			g.fillStyle = pat; double tri = bw * 0.5;
			for (double u = 0; u < L; u += tri * 1.4)
			{
				if (u + tri > gy - gh - 2 && u < gy + gh + 2) continue;
				g.beginPath();
				if (vert) { g.moveTo(c - bw * 0.32, u); g.lineTo(c + bw * 0.32, u); g.lineTo(c, u + tri); }
				else { g.moveTo(u, c - bw * 0.32); g.lineTo(u, c + bw * 0.32); g.lineTo(u + tri, c); }
				g.closePath(); g.fill();
			}
			// the edges of the opening: a diamond notch, the sadu eye
			g.fillStyle = col;
			foreach (var sd in new[] { -1.0, 1.0 })
			{
				double e = gy + sd * gh; g.beginPath();
				if (vert) { g.moveTo(c - bw / 2, e); g.lineTo(c + bw / 2, e); g.lineTo(c, e + sd * 16); }
				else { g.moveTo(e, c - bw / 2); g.lineTo(e, c + bw / 2); g.lineTo(e + sd * 16, c); }
				g.closePath(); g.fill();
			}
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ts[0])
			{
				double k = F11k(t, tw, ts[0]); band(pv[0], gs[0], F11G(0.3 + 0.5 * k), null, true); return;
			}
			int i = next(t);
			if (i >= 0 && ts[i] - t < 0.45) band(p0 + st * (i + 1), gs[i], F11G(0.35), null, true);   // where it steps next
			double f = 1 - F11k(t, ts[n - 1] + 0.2, te);
			band(pos(t), gap(t), t < ts[0] + 0.05 ? "#fff" : F11P(0.88 * f), F11D(0.55 * f), false);
		};
		h.hit = (p, t) =>
		{
			if (t < ts[0] || t > ts[n - 1] + 0.2) return false;
			double c = pos(t), gy = gap(t);
			double a = vert ? p.x : p.y, b = vert ? p.y : p.x;
			if (Math.Abs(a - c) >= bw / 2 + p.r - 2) return false;
			return Math.Abs(b - gy) > gh - p.r + 2;
		};
		return h;
	}

	// the kohl eyes (harmless, a place): two almond eyes over the dunes; they look where the last gaze went and close when they blink
	static readonly double[] F11LookX = { W / 2, W / 2 }, F11LookY = { H / 2, H / 2 }, F11Blink = { -9, -9 };
	static readonly double[] F11EX = { 430, 850 }; const double F11EY = 92;
	public static Hz F11Eyes(double t0, double te)
	{
		var h = new Hz { kind = "F11Eyes" };
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F11k(t, t0, t0 + 0.8), 1 - F11k(t, te, te + 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			for (int i = 0; i < 2; i++)
			{
				double x = F11EX[i], y = F11EY, bl = t - F11Blink[i], lid = bl >= 0 && bl < 0.3 ? JS.sin(bl / 0.3 * Math.PI) : 0, w = 120, hh = 44 * (1 - 0.92 * lid);
				// the kohl: a thick black line around, with its wing
				double so = i == 0 ? -1 : 1;   // the outer corner, where the kohl's wing goes
				g.fillStyle = "#050307"; g.beginPath(); g.moveTo(x - w - 10, y); g.quadraticCurveTo(x, y - hh * 2 - 14, x + w + 10, y); g.quadraticCurveTo(x, y + hh * 2 + 14, x - w - 10, y); g.closePath(); g.fill();
				g.beginPath(); g.moveTo(x + so * (w - 6), y - 8); g.lineTo(x + so * (w + 52), y - 30); g.lineTo(x + so * (w + 4), y + 8); g.closePath(); g.fill();
				g.fillStyle = "#f2e8d8"; g.beginPath(); g.moveTo(x - w, y); g.quadraticCurveTo(x, y - hh * 2, x + w, y); g.quadraticCurveTo(x, y + hh * 2, x - w, y); g.closePath(); g.fill();
				if (hh > 6)
				{
					g.save(); g.beginPath(); g.moveTo(x - w, y); g.quadraticCurveTo(x, y - hh * 2, x + w, y); g.quadraticCurveTo(x, y + hh * 2, x - w, y); g.closePath(); g.clip();
					double an = JS.atan2(F11LookY[i] - y, F11LookX[i] - x), d = Math.Min(48, hypot(F11LookX[i] - x, F11LookY[i] - y) * 0.1);
					double ix = x + JS.cos(an) * d, iy = y + JS.sin(an) * d * 0.5;
					g.fillStyle = "#3b2414"; circle(ix, iy, 34); g.fillStyle = F11Y(0.6); g.strokeStyle = F11Y(0.6); g.lineWidth = 2; g.beginPath(); g.arc(ix, iy, 30, 0, TAU); g.stroke();
					g.fillStyle = "#050307"; circle(ix, iy, 15); g.fillStyle = "rgba(255,255,255,0.85)"; circle(ix - 8, iy - 9, 5);
					g.restore();
				}
				// lashes on top
				g.strokeStyle = "#050307"; g.lineWidth = 3;
				for (int k = 0; k < 9; k++) { double u = (k + 1) / 10.0, lx = x - w + 2 * w * u, ly = y - (hh * 2) * 4 * u * (1 - u) * 0.5 - 2; g.beginPath(); g.moveTo(lx, ly); g.lineTo(lx + (u - 0.5) * 22, ly - 16); g.stroke(); }
			}
			g.restore();
		};
		return h;
	}
	// a gaze: eye i looks at (tx, ty) (where the player was when it began): a dashed line from tw, then a pink beam bw wide at ta for hold s
	public static Hz F11Gaze(int i, double tx, double ty, double bw, double tw, double ta, double hold)
	{
		var h = new Hz { kind = "F11Gaze" }; const double LEN = 1700; double ex = F11EX[i], ey = F11EY + 10, te = ta + hold;
		double an = JS.atan2(ty - ey, tx - ex), c = JS.cos(an), s = JS.sin(an);
		h.update = (t, dt) => { if (t >= tw) { F11LookX[i] = tx; F11LookY[i] = ty; } if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta); F11Dash(ex, ey, ex + c * LEN, ey + s * LEN, F11G(0.3 + 0.55 * k), 2 + k);
				g.strokeStyle = F11G(0.5 * k); g.lineWidth = 2; g.beginPath(); g.arc(tx, ty, 26 - 12 * k, 0, TAU); g.stroke(); return;
			}
			double f = 1 - F11k(t, te, te + 0.25), nx = -s * bw / 2, ny = c * bw / 2;
			g.fillStyle = t < ta + 0.05 ? "#fff" : F11P(0.9 * f); g.beginPath(); g.moveTo(ex + nx * 0.5, ey + ny * 0.5); g.lineTo(ex + c * LEN + nx, ey + s * LEN + ny); g.lineTo(ex + c * LEN - nx, ey + s * LEN - ny); g.lineTo(ex - nx * 0.5, ey - ny * 0.5); g.closePath(); g.fill();
			g.fillStyle = F11P(f); circle(ex, ey, 12);
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double d = hypot(p.x - ex, p.y - ey), w = bw / 2 * (0.5 + 0.5 * Math.Min(1, d / LEN));
			return F11Seg(p.x, p.y, ex, ey, ex + c * LEN, ey + s * LEN) < w + p.r - 2;
		};
		return h;
	}
	// the lashes: eye i blinks at ta and its lashes fly out, one along each angle (degrees), sp px a second, len long; their paths show from tw
	public static Hz F11Lashes(int i, double[] angs, double sp, double len, double lw, double tw, double ta)
	{
		var h = new Hz { kind = "F11Lashes" }; double ex = F11EX[i], ey = F11EY + 30, te = ta + 1700 / sp;
		h.update = (t, dt) => { if (t >= ta - 0.15 && F11Blink[i] < ta - 0.15) F11Blink[i] = ta - 0.15; if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			foreach (var a in angs)
			{
				double an = a * Math.PI / 180, c = JS.cos(an), s = JS.sin(an);
				if (t < ta) { double k = F11k(t, tw, ta); F11Dash(ex, ey, ex + c * 420, ey + s * 420, F11G(0.25 + 0.5 * k)); continue; }
				double d = (t - ta) * sp + 30;
				g.strokeStyle = t < ta + 0.05 ? "#fff" : PINK; g.lineWidth = lw; g.lineCap = "round"; g.beginPath(); g.moveTo(ex + c * Math.Max(30, d - len), ey + s * Math.Max(30, d - len)); g.lineTo(ex + c * d, ey + s * d); g.stroke(); g.lineCap = "butt";
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double d = (t - ta) * sp + 30;
			foreach (var a in angs)
			{
				double an = a * Math.PI / 180, c = JS.cos(an), s = JS.sin(an), d0 = Math.Max(30, d - len);
				if (F11Seg(p.x, p.y, ex + c * d0, ey + s * d0, ex + c * d, ey + s * d) < lw / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}
	// the circumstances: two walls of sand close on a seam (vert: walls from the left and the right meet at x = seam; else from the top and the bottom at y = seam).
	// Each wall has a notch dep deep at its front, at hc (hh half), so where the two meet there is one hole. The seam and the hole show (dashed) from tw;
	// the walls move from tm, meet at ta, stay till te and go back by tr.
	public static Hz F11Close(bool vert, double seam, double hc, double hh, double dep, double tw, double tm, double ta, double te, double tr)
	{
		var h = new Hz { kind = "F11Close" }; double Lx = vert ? W : H;
		double front(double t, int sd)
		{
			// sd -1: the wall from 0 (its front edge), 1: the wall from the far side
			double off = sd < 0 ? -dep - 12 : Lx + dep + 12, k;
			if (t < tm) k = 0; else if (t < ta) k = F11e(F11k(t, tm, ta)); else if (t < te) k = 1; else k = 1 - F11e(F11k(t, te, tr));
			return off + (seam - off) * k;
		}
		// the boxes of the wall sd, in the wall's own axes (a along the walls' way, b across)
		List<(double a0, double a1, double b0, double b1)> boxes(double t, int sd)
		{
			double f = front(t, sd), L2 = vert ? H : W; var r = new List<(double, double, double, double)>();
			if (sd < 0) { r.Add((-20, f - dep, -20, L2 + 20)); r.Add((f - dep, f, -20, hc - hh)); r.Add((f - dep, f, hc + hh, L2 + 20)); }
			else { r.Add((f + dep, Lx + 20, -20, L2 + 20)); r.Add((f, f + dep, -20, hc - hh)); r.Add((f, f + dep, hc + hh, L2 + 20)); }
			return r;
		}
		void fillBox(double a0, double a1, double b0, double b1) { if (vert) g.fillRect(a0, b0, a1 - a0, b1 - b0); else g.fillRect(b0, a0, b1 - b0, a1 - a0); }
		h.update = (t, dt) => { if (t > tr + 0.1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta), L2 = vert ? H : W;
				if (vert) F11Dash(seam, 0, seam, H, F11G(0.3 + 0.4 * k)); else F11Dash(0, seam, W, seam, F11G(0.3 + 0.4 * k));
				if (vert) F11DashBox(seam - dep, hc - hh, seam + dep, hc + hh, F11Y(0.5 + 0.4 * k), 3); else F11DashBox(hc - hh, seam - dep, hc + hh, seam + dep, F11Y(0.5 + 0.4 * k), 3);
			}
			if (t < tm || t > tr) return;
			bool fl = t >= ta && t < ta + 0.05;
			foreach (var sd in new[] { -1, 1 })
			{
				g.fillStyle = F11P(0.86);
				foreach (var b in boxes(t, sd)) fillBox(b.a0, b.a1, b.b0, b.b1);
				if (fl)
				{
					// the moment they meet: the fronts flash white (only the edges: a white screen would blind)
					g.fillStyle = "#fff"; double f0 = front(t, sd), e = sd < 0 ? f0 - 4 : f0;
					fillBox(e, e + 4, -20, (vert ? H : W) + 20);
					continue;
				}
				// the sand's ripples, dark pink lines along the wall
				g.strokeStyle = F11D(0.5); g.lineWidth = 3; double f = front(t, sd), L2 = vert ? H : W;
				for (int j = 1; j <= 4; j++)
				{
					double a = sd < 0 ? f - dep - 26 * j : f + dep + 26 * j; if ((sd < 0 && a < 0) || (sd > 0 && a > Lx)) continue;
					g.beginPath();
					for (double b = 0; b <= L2; b += 24) { double aa = a + 6 * JS.sin(b / 40.0 + j); if (vert) { if (b == 0) g.moveTo(aa, b); else g.lineTo(aa, b); } else { if (b == 0) g.moveTo(b, aa); else g.lineTo(b, aa); } }
					g.stroke();
				}
			}
		};
		h.hit = (p, t) =>
		{
			if (t < tm || t > tr) return false;
			foreach (var sd in new[] { -1, 1 })
				foreach (var b in boxes(t, sd))
					if (vert ? F11Box(p, b.a0, b.b0, b.a1, b.b1) : F11Box(p, b.b0, b.a0, b.b1, b.a1)) return true;
			return false;
		};
		return h;
	}

	// ---------- part 2: the night and the road ----------
	// the road through the sand: it comes down the screen v px a second; its middle at the distance s is F11RoadX(s). Outside it the sand is pink from ta to te.
	// Its edges show (dashed) from tw.
	static double F11RoadX(double s, double a1, double l1, double p1, double a2, double l2, double p2, double ramp)
	{
		double k = clamp(s / ramp, 0, 1); return W / 2 + k * (a1 * JS.sin(s / l1 + p1) + a2 * JS.sin(s / l2 + p2));
	}
	public static Hz F11Road(double tw, double ta, double te, double v, double a1, double l1, double p1, double a2, double l2, double p2, double ramp, double hw)
	{
		var h = new Hz { kind = "F11Road" };
		double S(double y, double t) => v * (t - ta) + (H - y);
		double X(double y, double t) => F11RoadX(S(y, t), a1, l1, p1, a2, l2, p2, ramp);
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta);
				foreach (var sd in new[] { -1.0, 1.0 })
				{
					g.strokeStyle = F11G(0.3 + 0.5 * k); g.lineWidth = 2 + k; g.setLineDash(new double[] { 10, 9 }); g.beginPath();
					for (double y = 0; y <= H; y += 12) { double x = X(y, t) + sd * hw; if (y == 0) g.moveTo(x, y); else g.lineTo(x, y); }
					g.stroke(); g.setLineDash(new double[0]);
				}
				// the sand that will hurt, faint
				g.fillStyle = F11Y(0.06 + 0.08 * k); F11RoadSides(X, t, hw);
				return;
			}
			double f = 1 - F11k(t, te, te + 0.3); if (f <= 0) return;
			g.fillStyle = F11P(0.7 * f); F11RoadSides(X, t, hw);
			// the wind's ripples on the sand (dark pink), coming down with it
			g.strokeStyle = F11D(0.55 * f); g.lineWidth = 3;
			double sh = (v * (t - ta)) % 60;
			for (double y = -60 + sh; y < H + 60; y += 60)
			{
				double cx = X(y, t);
				g.beginPath(); for (double x = 10; x < cx - hw - 20; x += 20) { double yy = y + 8 * JS.sin(x / 50.0); if (x == 10) g.moveTo(x, yy); else g.lineTo(x, yy); } g.stroke();
				g.beginPath(); bool st = true; for (double x = cx + hw + 20; x < W - 10; x += 20) { double yy = y + 8 * JS.sin(x / 50.0); if (st) { g.moveTo(x, yy); st = false; } else g.lineTo(x, yy); } g.stroke();
			}
			// the road's edges, bright, and its middle line in gold (only a look)
			foreach (var sd in new[] { -1.0, 1.0 })
			{
				g.strokeStyle = t < ta + 0.05 ? "#fff" : F11P(f); g.lineWidth = 4; g.beginPath();
				for (double y = 0; y <= H; y += 8) { double x = X(y, t) + sd * hw; if (y == 0) g.moveTo(x, y); else g.lineTo(x, y); }
				g.stroke();
			}
			g.strokeStyle = F11Y(0.35 * f); g.lineWidth = 3; g.setLineDash(new double[] { 22, 26 }); g.lineDashOffset = -(v * (t - ta)) % 48; g.beginPath();
			for (double y = 0; y <= H; y += 8) { double x = X(y, t); if (y == 0) g.moveTo(x, y); else g.lineTo(x, y); }
			g.stroke(); g.setLineDash(new double[0]); g.lineDashOffset = 0;
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false;
			return Math.Abs(p.x - X(p.y, t)) > hw - p.r + 2;
		};
		return h;
	}
	static void F11RoadSides(Func<double, double, double> X, double t, double hw)
	{
		g.beginPath(); g.moveTo(-5, -5);
		for (double y = 0; y <= H; y += 8) g.lineTo(X(y, t) - hw, y);
		g.lineTo(X(H + 8, t) - hw, H + 5); g.lineTo(-5, H + 5); g.closePath(); g.fill();
		g.beginPath(); g.moveTo(W + 5, -5);
		for (double y = 0; y <= H; y += 8) g.lineTo(X(y, t) + hw, y);
		g.lineTo(X(H + 8, t) + hw, H + 5); g.lineTo(W + 5, H + 5); g.closePath(); g.fill();
	}
	// a shooting star: its head crosses from (x0, y0) to (x1, y1) in dur s from ta (and goes on past them); its path shows from tw
	public static Hz F11Star(double x0, double y0, double x1, double y1, double r, double tw, double ta, double dur)
	{
		var h = new Hz { kind = "F11Star" }; double dx = (x1 - x0) / dur, dy = (y1 - y0) / dur, l = hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / l, uy = (y1 - y0) / l, te = ta + dur * 1.6;
		const double TL = 70;
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta); F11Dash(x0 - ux * 600, y0 - uy * 600, x1 + ux * 600, y1 + uy * 600, F11G(0.25 + 0.55 * k));
				g.fillStyle = F11G(0.3 + 0.5 * k); circle(x0, y0, 5 + 4 * k); return;
			}
			double hx = x0 + dx * (t - ta), hy = y0 + dy * (t - ta);
			g.strokeStyle = t < ta + 0.05 ? "#fff" : F11P(0.75); g.lineWidth = r * 1.2; g.lineCap = "round"; g.beginPath(); g.moveTo(hx - ux * TL, hy - uy * TL); g.lineTo(hx, hy); g.stroke(); g.lineCap = "butt";
			g.fillStyle = t < ta + 0.05 ? "#fff" : PINK; circle(hx, hy, r);
			g.fillStyle = "rgba(255,240,245,0.9)"; circle(hx, hy, r * 0.35);
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double hx = x0 + dx * (t - ta), hy = y0 + dy * (t - ta);
			return F11Seg(p.x, p.y, hx - ux * TL, hy - uy * TL, hx, hy) < r * 0.6 + p.r - 2 || hypot(p.x - hx, p.y - hy) < r + p.r - 2;
		};
		return h;
	}
	// the coffee pot (harmless, a place): a brass dallah at (x, y), its spout turned where the pouring is (F11DalA)
	static double F11DalA = 0;
	public static Hz F11Dallah(double x, double y, double a0, double t0, double te)
	{
		var h = new Hz { kind = "F11Dallah" };
		h.update = (t, dt) => { if (t < t0 + 0.02) F11DalA = a0; if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F11k(t, t0, t0 + 0.6), 1 - F11k(t, te, te + 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			var gl = g.createRadialGradient(x, y, 10, x, y, 120); gl.addColorStop(0, F11Y(0.25)); gl.addColorStop(1, F11Y(0)); g.fillStyle = gl; circle(x, y, 120);
			g.translate(x, y); g.rotate(F11DalA);
			// seen from above: the round body, the lid with its finial, the long beak of the spout (towards +x) and the handle behind
			g.strokeStyle = "#7a5420"; g.lineWidth = 8; g.beginPath(); g.arc(-44, 0, 22, Math.PI * 0.5, Math.PI * 1.5); g.stroke();
			g.fillStyle = "#b8862e"; g.beginPath(); g.moveTo(20, -12); g.quadraticCurveTo(48, -10, 62, -4); g.lineTo(62, 4); g.quadraticCurveTo(48, 10, 20, 12); g.closePath(); g.fill();
			var bd = g.createRadialGradient(-8, -10, 4, 0, 0, 40); bd.addColorStop(0, "#f6d68a"); bd.addColorStop(0.6, "#c99a3c"); bd.addColorStop(1, "#7a5420"); g.fillStyle = bd; circle(0, 0, 38);
			g.strokeStyle = "#7a5420"; g.lineWidth = 2; g.beginPath(); g.arc(0, 0, 26, 0, TAU); g.stroke(); g.beginPath(); g.arc(0, 0, 14, 0, TAU); g.stroke();
			g.fillStyle = "#f6d68a"; circle(0, 0, 7);
			g.restore();
		};
		return h;
	}
	// the pouring: from the dallah's spout a curved stream of coffee (pink) len long, lw wide, turning from a0 to a1 (degrees) between ta and ta + sw.
	// It shows (dashed) from tw at a0 with the arc it will turn through.
	public static Hz F11Pour(double x, double y, double a0, double a1, double len, double lw, double cv, double tw, double ta, double sw)
	{
		var h = new Hz { kind = "F11Pour" }; double te = ta + sw, sg = a1 >= a0 ? 1 : -1; const int N = 22;
		double A(double t) { double k = F11e(F11k(t, ta, te)); return (a0 + (a1 - a0) * k) * Math.PI / 180; }
		(double x, double y)[] pts(double an)
		{
			// the stream bends back, behind the way it turns
			double c = JS.cos(an), s = JS.sin(an), nx = s * sg, ny = -c * sg;
			var r = new (double, double)[N + 1];
			for (int i = 0; i <= N; i++) { double d = 62 + len * i / N, bend = cv * (d - 62) * (d - 62) / len; r[i] = (x + c * d + nx * bend, y + s * d + ny * bend); }
			return r;
		}
		void path((double x, double y)[] P) { g.beginPath(); g.moveTo(P[0].x, P[0].y); for (int i = 1; i <= N; i++) g.lineTo(P[i].x, P[i].y); }
		h.update = (t, dt) => { if (t >= ta && t <= te + 0.05) F11DalA = A(t); else if (t >= tw && t < ta) F11DalA = a0 * Math.PI / 180; if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta); g.strokeStyle = F11G(0.3 + 0.55 * k); g.lineWidth = 2 + k; g.setLineDash(new double[] { 10, 9 }); path(pts(a0 * Math.PI / 180)); g.stroke();
				g.strokeStyle = F11G(0.2 + 0.25 * k); path(pts(a1 * Math.PI / 180)); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = F11G(0.35 * k); g.lineWidth = 2; g.beginPath(); g.arc(x, y, 150, Math.Min(a0, a1) * Math.PI / 180, Math.Max(a0, a1) * Math.PI / 180); g.stroke();
				return;
			}
			double f = 1 - F11k(t, te, te + 0.25); var P = pts(A(t));
			g.strokeStyle = t < ta + 0.05 ? "#fff" : F11P(0.9 * f); g.lineWidth = lw; g.lineCap = "round"; g.lineJoin = "round"; path(P); g.stroke();
			g.strokeStyle = F11D(0.5 * f); g.lineWidth = Math.Max(2, lw * 0.25); path(P); g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; var P = pts(A(t));
			for (int i = 1; i <= N; i++) if (F11Seg(p.x, p.y, P[i - 1].x, P[i - 1].y, P[i].x, P[i].y) < lw / 2 + p.r - 2) return true;
			return false;
		};
		return h;
	}
	// the incense burner (harmless, a place): a mabkhara at (x, y) with grey smoke going up
	public static Hz F11Burner(double x, double y, double t0, double te)
	{
		var h = new Hz { kind = "F11Burner" };
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F11k(t, t0, t0 + 0.6), 1 - F11k(t, te, te + 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			for (int i = 0; i < 6; i++)
			{
				double u = ((t * 0.5 + i / 6.0) % 1), sx = x + 14 * JS.sin(u * 7 + i), sy = y - 30 - u * 110;
				g.fillStyle = $"rgba(200,196,210,{str(0.22 * (1 - u))})"; circle(sx, sy, 10 + 16 * u);
			}
			g.fillStyle = "#5a3418"; g.beginPath(); g.moveTo(x - 30, y + 26); g.lineTo(x + 30, y + 26); g.lineTo(x + 20, y + 4); g.lineTo(x - 20, y + 4); g.closePath(); g.fill();
			g.fillStyle = "#c99a3c"; g.fillRect(x - 34, y - 26, 68, 30); g.fillStyle = "#7a5420"; for (int i = 0; i < 5; i++) g.fillRect(x - 28 + i * 13, y - 20, 6, 18);
			g.fillStyle = "#f6d68a"; g.beginPath(); g.moveTo(x - 38, y - 26); g.lineTo(x + 38, y - 26); g.lineTo(x + 30, y - 34); g.lineTo(x - 30, y - 34); g.closePath(); g.fill();
			g.fillStyle = "rgba(255,150,60,0.8)"; circle(x, y - 36, 6);
			g.restore();
		};
		return h;
	}
	// a ring of incense smoke: grows from the burner's edge (40 px round (x, y)) at ta, sp px a second, th thick, with openings gw wide (degrees) at each of ga, up to rmax; shows from tw
	public static Hz F11Smoke(double x, double y, double[] ga, double gw, double sp, double th, double rmax, double tw, double ta)
	{
		var h = new Hz { kind = "F11Smoke" }; double te = ta + rmax / sp; double[] gr = ga.Select(a => a * Math.PI / 180).ToArray(), gwr = { gw * Math.PI / 180 };
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		bool inGap(double an, double slack) { foreach (var c in gr) { double df = Math.Abs(((an - c) % TAU + TAU + Math.PI) % TAU - Math.PI); if (df < gwr[0] / 2 - slack) return true; } return false; }
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F11k(t, tw, ta); g.strokeStyle = F11G(0.25 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 7, 7 }); g.beginPath(); g.arc(x, y, 56 + 30 * (1 - k), 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				foreach (var c in gr) { g.strokeStyle = F11Y(0.3 + 0.5 * k); g.lineWidth = 6; g.beginPath(); g.arc(x, y, 60, c - gwr[0] / 2, c + gwr[0] / 2); g.stroke(); }
				return;
			}
			double R = 40 + (t - ta) * sp, f = 1 - F11k(t, te, te + 0.15);
			// the ring in pieces between its openings
			var sorted = gr.OrderBy(c => ((c % TAU) + TAU) % TAU).ToArray();
			g.strokeStyle = t < ta + 0.05 ? "#fff" : F11P(0.85 * f); g.lineWidth = th; g.lineCap = "round";
			for (int j = 0; j < sorted.Length; j++)
			{
				double s0 = sorted[j] + gwr[0] / 2, s1 = (j + 1 < sorted.Length ? sorted[j + 1] : sorted[0] + TAU) - gwr[0] / 2; if (s1 <= s0) continue;
				g.beginPath(); int m = Math.Max(6, (int)((s1 - s0) * R / 14));
				for (int q = 0; q <= m; q++) { double an = s0 + (s1 - s0) * q / m, rr = R; if (q == 0) g.moveTo(x + JS.cos(an) * rr, y + JS.sin(an) * rr); else g.lineTo(x + JS.cos(an) * rr, y + JS.sin(an) * rr); }
				g.stroke();
			}
			g.lineCap = "butt";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; double R = 40 + (t - ta) * sp, d = hypot(p.x - x, p.y - y);
			if (Math.Abs(d - R) >= th / 2 + p.r - 2) return false;
			double an = JS.atan2(p.y - y, p.x - x), slack = d > 1 ? (p.r - 2) / d : 0;
			return !inGap(an, slack);
		};
		return h;
	}

	// ---------- part 3: the heart and the hands ----------
	// the gold heart (harmless, a place) at (x, y): it beats with the song
	static readonly (double x, double y)[] F11HeartPts = Enumerable.Range(0, 97).Select(i =>
	{
		double th = TAU * i / 96, s = JS.sin(th); return (16 * s * s * s / 17.0, -(13 * JS.cos(th) - 5 * JS.cos(2 * th) - 2 * JS.cos(3 * th) - JS.cos(4 * th)) / 17.0 - 0.1);
	}).ToArray();
	static void F11HeartPath(double x, double y, double R, int i0 = 0, int i1 = 96)
	{
		g.beginPath(); for (int i = i0; i <= i1; i++) { var q = F11HeartPts[i]; if (i == i0) g.moveTo(x + q.x * R, y + q.y * R); else g.lineTo(x + q.x * R, y + q.y * R); }
	}
	public static Hz F11Heart(double x, double y, double t0, double te)
	{
		var h = new Hz { kind = "F11Heart" };
		h.update = (t, dt) => { if (t > te + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F11k(t, t0, t0 + 0.8), 1 - F11k(t, te + 0.6, te + 1.5)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			double bt = BT(t), u = bt - Math.Floor(bt), beat = Math.Max(0, 1 - u / 0.25) + 0.6 * Math.Max(0, 1 - Math.Abs(u - 0.6) / 0.15), R = 62 * (1 + 0.08 * beat);
			var gl = g.createRadialGradient(x, y, 10, x, y, 190); gl.addColorStop(0, F11Y(0.3 + 0.15 * beat)); gl.addColorStop(1, F11Y(0)); g.fillStyle = gl; circle(x, y, 190);
			var gd = g.createLinearGradient(x, y - R, x, y + R); gd.addColorStop(0, "#f8dc94"); gd.addColorStop(1, "#a8741e"); g.fillStyle = gd; F11HeartPath(x, y, R); g.fill();
			g.strokeStyle = "#6a4210"; g.lineWidth = 2; F11HeartPath(x, y, R * 0.72); g.stroke(); F11HeartPath(x, y, R * 0.45); g.stroke();
			g.restore();
		};
		return h;
	}
	// the heart's wave: a heart-shaped line grows from (x, y) at ta, sp px a second (its size), th thick; the stretches gp (pairs of from, to on 0-1 round the heart) are open
	public static Hz F11HWave(double x, double y, double[] gp, double sp, double th, double rmax, double tw, double ta)
	{
		var h = new Hz { kind = "F11HWave" }; double te = ta + rmax / sp;
		bool open(int i) { double u = i / 96.0; for (int j = 0; j + 1 < gp.Length; j += 2) { double a = gp[j], b = gp[j + 1]; if (a <= b ? (u >= a && u <= b) : (u >= a || u <= b)) return true; } return false; }
		var on = Enumerable.Range(0, 96).Select(i => !open(i) && !open(i + 1)).ToArray();
		void lines(double R)
		{
			int i = 0;
			while (i < 96) { if (!on[i]) { i++; continue; } int j = i; while (j < 96 && on[j]) j++; F11HeartPath(x, y, R, i, j); g.stroke(); i = j; }
		}
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta) { double k = F11k(t, tw, ta); g.strokeStyle = F11G(0.25 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 7, 7 }); lines(90 + 20 * (1 - k)); g.setLineDash(new double[0]); return; }
			double R = 70 + (t - ta) * sp, f = 1 - F11k(t, te, te + 0.15);
			g.strokeStyle = t < ta + 0.05 ? "#fff" : F11P(0.88 * f); g.lineWidth = th; g.lineCap = "round"; lines(R); g.lineCap = "butt";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; double R = 70 + (t - ta) * sp;
			// quick out: the heart's size is about R round its middle
			double d = hypot(p.x - x, p.y - y); if (d > R * 1.2 + th + p.r || d < R * 0.25 - th) return false;
			for (int i = 0; i < 96; i++)
			{
				if (!on[i]) continue; var a = F11HeartPts[i]; var b = F11HeartPts[i + 1];
				if (F11Seg(p.x, p.y, x + a.x * R, y + a.y * R, x + b.x * R, y + b.y * R) < th / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}
	// the henna: n petals round (x, y) from r0 to r1, cr thick, turned by the angle plan ka (degrees) at kb (seconds, each step eased over 0.25 beat).
	// They show (dashed) from tw and hurt from ta to te.
	public static Hz F11Henna(double x, double y, int n, double r0, double r1, double cr, double[] kb, double[] ka, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F11Henna" };
		double A(double t) => F11Step(kb, ka, t, 0.25 * SPB, ka[0]) * Math.PI / 180;
		void petal(double an, string fill, string line)
		{
			double c = JS.cos(an), s = JS.sin(an), nx = -s, ny = c, ax = x + c * r0, ay = y + s * r0, bx = x + c * r1, by = y + s * r1, mx = (ax + bx) / 2, my = (ay + by) / 2;
			g.beginPath(); g.moveTo(ax + nx * cr * 0.6, ay + ny * cr * 0.6);
			g.quadraticCurveTo(mx + nx * cr * 1.7, my + ny * cr * 1.7, bx, by); g.quadraticCurveTo(mx - nx * cr * 1.7, my - ny * cr * 1.7, ax - nx * cr * 0.6, ay - ny * cr * 0.6);
			g.arc(ax, ay, cr * 0.6, an + Math.PI / 2, an + Math.PI * 1.5); g.closePath();
			if (fill != null) { g.fillStyle = fill; g.fill(); } else { g.strokeStyle = line; g.lineWidth = 2; g.setLineDash(new double[] { 7, 6 }); g.stroke(); g.setLineDash(new double[0]); }
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = A(t);
			if (t < ta) { double k = F11k(t, tw, ta); for (int i = 0; i < n; i++) petal(a + i * TAU / n, null, F11G(0.3 + 0.5 * k)); return; }
			double f = 1 - F11k(t, te, te + 0.3);
			for (int i = 0; i < n; i++)
			{
				double an = a + i * TAU / n; petal(an, t < ta + 0.05 ? "#fff" : F11P(0.9 * f), null);
				// the henna's own lines inside the petal (dark pink)
				g.strokeStyle = F11D(0.6 * f); g.lineWidth = 2; double c = JS.cos(an), s = JS.sin(an);
				g.beginPath(); g.moveTo(x + c * (r0 + 10), y + s * (r0 + 10)); g.lineTo(x + c * (r1 - 18), y + s * (r1 - 18)); g.stroke();
				for (int q = 1; q <= 3; q++) { double d = r0 + (r1 - r0) * q / 4.5; g.beginPath(); g.arc(x + c * d, y + s * d, 3, 0, TAU); g.stroke(); }
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; double a = A(t);
			for (int i = 0; i < n; i++)
			{
				// the petal: half wide cr (0.6 (1-u)^2 + 3.4 u (1-u)) at u of the way out (its curve), a round end of 0.6 cr at its base
				double an = a + i * TAU / n, c = JS.cos(an), s = JS.sin(an), px = p.x - x, py = p.y - y, along = px * c + py * s, across = Math.Abs(-px * s + py * c);
				if (along < r0) { if (hypot(along - r0, across) < cr * 0.6 + p.r - 2) return true; continue; }
				if (along > r1) continue;
				double u = (along - r0) / (r1 - r0), half = cr * (0.6 * (1 - u) * (1 - u) + 3.4 * u * (1 - u));
				if (across < half + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}
	// the hands: the left palm's front at kl, the right one's at kr (x, at the beats kb in seconds, each step eased over 0.3 beat);
	// the left fingers (fl long, fh thick) at the rows ly, the right ones at ry; the rows change at the times fb to the next set (eased over 0.3 beat,
	// the next rows show dashed 1 beat before). They hurt from ta to te.
	public static Hz F11Hands(double[] kb, double[] kl, double[] kr, double[] fb, double[][] lys, double[][] rys, double fl, double fh, double ta, double te)
	{
		var h = new Hz { kind = "F11Hands" };
		double Lp(double t) => F11Step(kb, kl, t, 0.3 * SPB, kl[0]);
		double Rp(double t) => F11Step(kb, kr, t, 0.3 * SPB, kr[0]);
		int set(double t) { int i = 0; while (i + 1 < fb.Length && t >= fb[i + 1]) i++; return i; }
		double[] rows(double[][] ys, double t)
		{
			int i = set(t); if (i == 0) return ys[0]; double u = F11e(clamp((t - fb[i]) / (0.3 * SPB), 0, 1));
			return ys[i].Select((v, j) => ys[i - 1][j] + (v - ys[i - 1][j]) * u).ToArray();
		}
		void hand(double p, int sd, double[] ys, string col, bool line)
		{
			// the palm (all the side from its front back to the edge) and the fingers out of it, rounded at their tips
			if (line)
			{
				foreach (var y in ys) { double x0 = sd < 0 ? p : p - fl, x1 = sd < 0 ? p + fl : p; F11DashBox(x0, y - fh / 2, x1, y + fh / 2, col); }
				return;
			}
			g.fillStyle = col;
			if (sd < 0) g.fillRect(-10, -10, p + 10, H + 20); else g.fillRect(p, -10, W + 10 - p, H + 20);
			foreach (var y in ys)
			{
				double tip = sd < 0 ? p + fl : p - fl; g.beginPath();
				if (sd < 0) { g.moveTo(p - 2, y - fh / 2); g.lineTo(tip - fh / 2, y - fh / 2); g.arc(tip - fh / 2, y, fh / 2, -Math.PI / 2, Math.PI / 2); g.lineTo(p - 2, y + fh / 2); }
				else { g.moveTo(p + 2, y - fh / 2); g.lineTo(tip + fh / 2, y - fh / 2); g.arc(tip + fh / 2, y, fh / 2, -Math.PI / 2, -Math.PI * 1.5, true); g.lineTo(p + 2, y + fh / 2); }
				g.closePath(); g.fill();
			}
		}
		void henna(double p, int sd, double[] ys, double f)
		{
			// the henna drawn on the hands: dots and vines in dark pink
			g.strokeStyle = F11D(0.55 * f); g.fillStyle = F11D(0.55 * f); g.lineWidth = 2;
			foreach (var y in ys)
			{
				for (int q = 1; q <= 4; q++) { double x = p - sd * fl * q / 5.0; circle(x, y, 3); }
				g.beginPath(); g.moveTo(p - sd * 20, y); g.quadraticCurveTo(p - sd * fl * 0.5, y - fh * 0.3, p - sd * (fl - fh * 0.6), y); g.stroke();
			}
			double cx = p + sd * 70; if (sd < 0 ? cx > 40 : cx < W - 40) { g.beginPath(); g.arc(cx, H / 2, 46, 0, TAU); g.stroke(); g.beginPath(); g.arc(cx, H / 2, 24, 0, TAU); g.stroke(); circle(cx, H / 2, 6); }
		}
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) return; double f = 1 - F11k(t, te, te + 0.5); if (f <= 0) return;
			double lp = Lp(t), rp = Rp(t); var ly = rows(lys, t); var ry = rows(rys, t);
			hand(lp, -1, ly, F11P(0.86 * f), false); hand(rp, 1, ry, F11P(0.86 * f), false);
			henna(lp, -1, ly, f); henna(rp, 1, ry, f);
			// the rows that come next, dashed (1 beat before they change)
			int i = set(t);
			if (i + 1 < fb.Length && fb[i + 1] - t < SPB) { hand(Lp(fb[i + 1] + SPB), -1, lys[i + 1], F11G(0.75), true); hand(Rp(fb[i + 1] + SPB), 1, rys[i + 1], F11G(0.75), true); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; double lp = Lp(t), rp = Rp(t);
			if (p.x - p.r + 2 < lp - 2 || p.x + p.r - 2 > rp + 2) return true;
			// a finger: its box, and the round tip (as it is drawn)
			foreach (var y in rows(lys, t)) if (F11Box(p, lp - 2, y - fh / 2, lp + fl - fh / 2, y + fh / 2) || hypot(p.x - (lp + fl - fh / 2), p.y - y) < fh / 2 + p.r - 2) return true;
			foreach (var y in rows(rys, t)) if (F11Box(p, rp - fl + fh / 2, y - fh / 2, rp + 2, y + fh / 2) || hypot(p.x - (rp - fl + fh / 2), p.y - y) < fh / 2 + p.r - 2) return true;
			return false;
		};
		return h;
	}
	// the end: the hands meet over the heart in gold light (harmless)
	public static Hz F11Clasp(double t0)
	{
		var h = new Hz { kind = "F11Clasp" };
		h.update = (t, dt) => { if (t > t0 + 4) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = F11k(t, t0, t0 + 1.0), f = 1 - F11k(t, t0 + 2.5, t0 + 4);
			var gr = g.createRadialGradient(W / 2, H / 2, 10, W / 2, H / 2, 200 + 700 * k); gr.addColorStop(0, F11Y(0.85 * f)); gr.addColorStop(0.5, F11Y(0.35 * f)); gr.addColorStop(1, F11Y(0)); g.fillStyle = gr; g.fillRect(0, 0, W, H);
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_F11.json) ----------
	// Only pink hurts, so nothing may hide an attack: the places (the sky, the eyes, the coffee pot, the burner, the heart) are drawn first,
	// in the order they came, under every attack (as in F9: the first thing in the list draws the places before itself).
	static readonly List<(Hz h, Action<double> d)> F11Places = new();
	static void F11Under(Hz h) { F11Places.Add((h, h.draw)); h.draw = t => F11First(h, t); hazards.Add(h); }
	static void F11Add(Hz h) { var d = h.draw; h.draw = t => { F11First(h, t); d(t); }; hazards.Add(h); }
	static void F11First(Hz h, double t)
	{
		if (hazards.Count == 0 || hazards[0] != h) return;
		F11Places.RemoveAll(q => !hazards.Contains(q.h));
		foreach (var q in F11Places) q.d(t);
	}
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F11wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F11ez(double v, double ez) => easy ? v * ez : v;
	static bool F11skip(DodgeEv e) => easy && e.flag("ezskip");

	static readonly int _kf11stage = DodgeKind("F11stage", e => () => F11Under(F11Stage(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _kf11fx = DodgeKind("F11fx", e => () => F11Add(F11Fx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2)))));
	static readonly int _kf11band = DodgeKind("F11band", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; var ts = e.nums("ts").Select(B).ToArray(); double gh = e.num("gh"), gs = F11ez(gh, 1.3);
		F11Add(F11Band(e.flag("vert"), e.num("p0"), e.num("st"), F11ez(e.num("bw"), 0.85), ts, e.nums("gs"), gs, ts[0] - wn * SPB));
	}; });
	static readonly int _kf11eyes = DodgeKind("F11eyes", e => () => F11Under(F11Eyes(B(e.b), B(e.num("to")))));
	static readonly int _kf11gaze = DodgeKind("F11gaze", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; var p = player; double tx = p != null ? p.x : W / 2, ty = p != null ? p.y : H / 2;
		// a fixed aim (ax, ay) when the gaze is part of a pattern, else at the player
		if (e.has("ax")) { tx = e.num("ax"); ty = e.num("ay"); }
		F11Add(F11Gaze(e.i("eye"), tx, ty, F11ez(e.num("bw"), 0.75), B(e.num("hit") - wn), B(e.num("hit")), F11ez(e.num("hold", 0.4), 0.8) * SPB));
	}; });
	static readonly int _kf11lash = DodgeKind("F11lash", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; var an = e.nums("angs"); if (easy) an = an.Where((v, i) => i % 2 == 0).ToArray();
		F11Add(F11Lashes(e.i("eye"), an, F11ez(e.num("sp"), 0.8), e.num("len", 40), e.num("lw", 9), B(e.num("hit") - wn), B(e.num("hit"))));
	}; });
	static readonly int _kf11close = DodgeKind("F11close", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; double hit = e.num("hit"), mv = e.num("mv", 1.5) * (easy ? 1.2 : 1);
		F11Add(F11Close(e.flag("vert"), e.num("seam"), e.num("hc"), F11ez(e.num("hh"), 1.3), e.num("dep", 46), B(hit - mv - wn), B(hit - mv), B(hit), B(hit + F11ez(e.num("hold", 0.5), 0.7)), B(hit + F11ez(e.num("hold", 0.5), 0.7) + 0.7)));
	}; });
	static readonly int _kf11road = DodgeKind("F11road", e => { double wn = F11wn(e); return () =>
		F11Add(F11Road(B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")), e.num("v"), e.num("a1") * (easy ? 0.8 : 1), e.num("l1"), e.num("p1"), e.num("a2") * (easy ? 0.8 : 1), e.num("l2"), e.num("p2"), e.num("ramp"), F11ez(e.num("hw"), 1.25)));
	});
	static readonly int _kf11star = DodgeKind("F11star", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return;
		F11Add(F11Star(e.num("x0"), e.num("y0"), e.num("x1"), e.num("y1"), F11ez(e.num("r", 13), 0.8), B(e.num("hit") - wn), B(e.num("hit")), e.num("dur") * SPB * (easy ? 1.2 : 1)));
	}; });
	static readonly int _kf11dallah = DodgeKind("F11dallah", e => () => F11Under(F11Dallah(e.num("x"), e.num("y"), e.num("a0") * Math.PI / 180, B(e.b), B(e.num("to")))));
	static readonly int _kf11pour = DodgeKind("F11pour", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; double a0 = e.num("a0"), a1 = e.num("a1");
		F11Add(F11Pour(e.num("x"), e.num("y"), a0, a1, e.num("len", 700), F11ez(e.num("lw", 22), 0.75), e.num("cv", 0.18), B(e.num("hit") - wn), B(e.num("hit")), e.num("sw") * SPB * (easy ? 1.15 : 1)));
	}; });
	static readonly int _kf11burner = DodgeKind("F11burner", e => () => F11Under(F11Burner(e.num("x"), e.num("y"), B(e.b), B(e.num("to")))));
	static readonly int _kf11smoke = DodgeKind("F11smoke", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return;
		F11Add(F11Smoke(e.num("x"), e.num("y"), e.nums("ga"), F11ez(e.num("gw"), 1.35), F11ez(e.num("sp"), 0.8) / SPB, F11ez(e.num("th", 16), 0.75), e.num("rmax", 900), B(e.num("hit") - wn), B(e.num("hit"))));
	}; });
	static readonly int _kf11heart = DodgeKind("F11heart", e => () => F11Under(F11Heart(e.num("x"), e.num("y"), B(e.b), B(e.num("to")))));
	static readonly int _kf11hwave = DodgeKind("F11hwave", e => { double wn = F11wn(e); return () =>
	{
		if (F11skip(e)) return; var gp = e.nums("gp"); if (easy) gp = gp.Select((v, i) => i % 2 == 0 ? v - 0.025 : v + 0.025).Select(v => (v + 1) % 1).ToArray();
		F11Add(F11HWave(e.num("x"), e.num("y"), gp, F11ez(e.num("sp"), 0.8) / SPB, F11ez(e.num("th", 14), 0.75), e.num("rmax", 900), B(e.num("hit") - wn), B(e.num("hit"))));
	}; });
	static readonly int _kf11henna = DodgeKind("F11henna", e => { double wn = F11wn(e); return () =>
	{
		var ka = e.nums("ka"); if (easy) { double a0 = ka[0]; ka = ka.Select(a => a0 + (a - a0) * 0.8).ToArray(); }
		F11Add(F11Henna(e.num("x"), e.num("y"), e.i("n", 6), e.num("r0"), e.num("r1"), F11ez(e.num("cr", 16), 0.8), e.nums("kb").Select(B).ToArray(), ka, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _kf11hands = DodgeKind("F11hands", e => () =>
	{
		var ly = e.nums("ly"); var ry = e.nums("ry"); int nf = e.i("nf", 3), ns = ly.Length / nf;
		var lys = Enumerable.Range(0, ns).Select(i => ly.Skip(i * nf).Take(nf).ToArray()).ToArray(); var rys = Enumerable.Range(0, ns).Select(i => ry.Skip(i * nf).Take(nf).ToArray()).ToArray();
		var kl = e.nums("kl"); var kr = e.nums("kr"); if (easy) { kl = kl.Select(x => x - 40).ToArray(); kr = kr.Select(x => x + 40).ToArray(); }
		F11Add(F11Hands(e.nums("kb").Select(B).ToArray(), kl, kr, e.nums("fb").Select(B).ToArray(), lys, rys, e.num("fl"), F11ez(e.num("fh"), 0.8), B(e.b), B(e.num("to"))));
	});
	static readonly int _kf11clasp = DodgeKind("F11clasp", e => () => F11Add(F11Clasp(B(e.b))));
}
