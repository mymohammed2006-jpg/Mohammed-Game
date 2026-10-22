using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// H3 (Hardcore level 3) "Raining Teddy Bears" (Black Ops Zombies): Samantha's room, where the toys wake up and hunt you.
// part 1 the room (the music box plays its pins at you, the teddy rain on two grids, the jump rope you can only dash through,
// the needle that sews the room shut), part 2 the box (the mystery box wheel, the bear that tears in two again and again, the scissors
// that close on where you stand, the carousel), part 3 the nightmare (the mirror, the spinning tops, the cat's cradle, all of it at once).
// Every hazard is a pure function of time once it exists (the scissors also of where they locked on), so hit(p, t) is right for any later t.
// The data is data/dodge_H3.json (made by tools/hardcore3/gen.py).
public static partial class G
{
	static double H3k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string H3P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string H3G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string H3F(double a) => $"rgba(196,150,120,{str(clamp(a, 0, 1))})";   // old fur (harmless things)
	static double H3SegD(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void H3Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static int H3Lower(double[] a, double v) { int lo = 0, hi = a.Length; while (lo < hi) { int m = (lo + hi) >> 1; if (a[m] < v) lo = m + 1; else hi = m; } return lo; }

	// ---------- the look of things ----------
	// a torn teddy bear's head (r = size): round ears, one button eye, one eye sewn shut with an X, a stitched mouth
	static void H3Bear(double x, double y, double r, double a, string col, bool lit = true)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = col;
		circle(-r * 0.72, -r * 0.72, r * 0.4); circle(r * 0.72, -r * 0.72, r * 0.4); circle(0, 0, r);
		g.fillStyle = "rgba(20,0,8,0.45)"; circle(-r * 0.72, -r * 0.72, r * 0.2); circle(r * 0.72, -r * 0.72, r * 0.2);
		g.fillStyle = "rgba(255,235,240,0.28)"; g.beginPath(); g.ellipse(0, r * 0.38, r * 0.48, r * 0.34, 0, 0, TAU); g.fill();
		// the button eye (glows) and the sewn eye
		g.fillStyle = lit ? "#ff1744" : "rgba(30,0,10,0.8)"; circle(-r * 0.36, -r * 0.12, r * 0.17);
		g.fillStyle = "rgba(0,0,0,0.75)"; circle(-r * 0.36 - r * 0.05, -r * 0.12, r * 0.04); circle(-r * 0.36 + r * 0.05, -r * 0.12, r * 0.04);
		g.strokeStyle = "rgba(15,0,6,0.9)"; g.lineWidth = Math.Max(1.5, r * 0.07); g.beginPath();
		g.moveTo(r * 0.24, -r * 0.26); g.lineTo(r * 0.5, 0.02 * r); g.moveTo(r * 0.5, -r * 0.26); g.lineTo(r * 0.24, 0.02 * r);
		g.stroke(); g.strokeStyle = "rgba(15,0,6,0.9)";
		// a drip under the button eye
		if (lit) { g.fillStyle = "rgba(120,0,20,0.85)"; g.fillRect(-r * 0.39, r * 0.02, r * 0.06, r * 0.3); circle(-r * 0.36, r * 0.33, r * 0.05); }
		g.beginPath();
		// the mouth: stitches
		g.moveTo(-r * 0.3, r * 0.46); g.lineTo(r * 0.3, r * 0.46);
		for (int i = -2; i <= 2; i++) { g.moveTo(i * r * 0.13, r * 0.38); g.lineTo(i * r * 0.13, r * 0.54); }
		// the seam down the head
		g.moveTo(0, -r * 0.98); g.lineTo(0, -r * 0.55); g.stroke();
		g.restore();
	}
	// a spring-wound spinning top (r = size, spin angle a)
	static void H3TopDraw(double x, double y, double r, double a, string col)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = col; circle(0, 0, r);
		g.fillStyle = "rgba(20,0,8,0.55)"; for (int i = 0; i < 4; i++) { g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, r, i * Math.PI / 2, i * Math.PI / 2 + 0.6); g.closePath(); g.fill(); }
		g.fillStyle = "#f4e6ea"; circle(0, 0, r * 0.22); g.restore();
	}

	// ---------- the room itself (harmless) ----------
	// part 1: the dark nursery, 2: the mystery box's light, 3: the nightmare (red, the wallpaper bleeds)
	public static Hz H3Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "H3Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			g.fillStyle = part == 3 ? $"rgba(44,0,10,{str(0.55 + 0.15 * pulse)})" : "rgba(22,10,18,0.7)"; g.fillRect(0, 0, W, H);
			// the wallpaper: stripes and little bears
			g.fillStyle = part == 3 ? "rgba(120,0,30,0.16)" : "rgba(120,90,110,0.07)";
			for (double x = 0; x < W; x += 64) g.fillRect(x, 0, 22, H);
			g.globalAlpha = a * 0.5; for (int i = 0; i < 40; i++) { double bx = (i % 10) * 128 + 54 + (i / 10 % 2) * 64, by = (i / 10) * 180 + 60; H3Bear(bx, by, 11, 0, part == 3 ? "rgba(140,0,30,0.4)" : "rgba(150,120,130,0.25)", false); } g.globalAlpha = a;
			if (part == 2)
			{
				var gr = g.createRadialGradient(W / 2, H / 2, 20, W / 2, H / 2, 520); gr.addColorStop(0, $"rgba(120,170,255,{str(0.12 + 0.06 * pulse)})"); gr.addColorStop(1, "rgba(120,170,255,0)");
				g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			if (part == 3) { g.globalAlpha = a * (0.22 + 0.1 * pulse); H3Bear(W / 2, H / 2 + 20, 290, 0, "rgba(0,0,0,0.9)", false); g.globalAlpha = a; }
			var dk = g.createRadialGradient(W / 2, H / 2, 250, W / 2, H / 2, 760); dk.addColorStop(0, "rgba(0,0,0,0)"); dk.addColorStop(1, "rgba(0,0,0,0.75)"); g.fillStyle = dk; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- part 1: the room ----------
	// the music box: its comb runs along the top of the room (20 teeth, low notes on the left). Each note of the song drops a pin down its
	// column: the pin's front is at the middle of the room at the note's time tn[k], it moves v px a second, it is len long.
	// The column lights grey wn s before the pin comes in
	const double H3_CW = 64;
	public static Hz H3Pins(double[] tn, double[] col, double v, double len, double wn, double te)
	{
		var h = new Hz { kind = "H3Pins" }; double tin = H / 2 / v, tout = (H / 2 + len) / v;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			int i0 = H3Lower(tn, t - tout), i1 = H3Lower(tn, t + tin + wn);
			for (int i = i0; i < i1; i++)
			{
				double x = col[i] * H3_CW, hy = H / 2 + v * (t - tn[i]);
				if (hy < 0) { double q = H3k(t, tn[i] - tin - wn, tn[i] - tin); g.fillStyle = H3G(0.04 + 0.16 * q); g.fillRect(x + 4, 0, H3_CW - 8, H); H3Dash(x + H3_CW / 2, 0, x + H3_CW / 2, H * q, H3G(0.3 + 0.5 * q), 2); continue; }
				double y0 = hy - len; bool wh = hy < 40;
				g.fillStyle = H3P(0.12); g.fillRect(x + 4, Math.Max(0, y0 - 60), H3_CW - 8, Math.Min(H, hy) - Math.Max(0, y0 - 60));
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = wh ? "#fff" : PINK; g.fillRect(x + 6, y0, H3_CW - 12, len); g.shadowBlur = 0;
				g.fillStyle = "rgba(255,240,245,0.55)"; g.fillRect(x + 12, y0 + 6, 6, len - 12);
				g.fillStyle = "rgba(40,0,12,0.5)"; g.beginPath(); g.moveTo(x + 6, hy); g.lineTo(x + H3_CW / 2, hy + 18); g.lineTo(x + H3_CW - 6, hy); g.closePath(); g.fill();
			}
			// the comb: a tooth shakes when its pin starts
			for (int c = 0; c < 20; c++)
			{
				double sh = 0; for (int i = H3Lower(tn, t + tin - 0.3); i < tn.Length && tn[i] - tin <= t; i++) if ((int)col[i] == c) sh = Math.Max(sh, 1 - (t - tn[i] + tin) / 0.3);
				g.fillStyle = sh > 0 ? $"rgba(255,{str(round(235 - 120 * sh))},{str(round(240 - 100 * sh))},0.9)" : "rgba(200,190,200,0.5)";
				g.fillRect(c * H3_CW + 14, 0, H3_CW - 28, 18 + 10 * sh);
			}
		};
		h.hit = (p, t) =>
		{
			int i0 = H3Lower(tn, t - tout), i1 = H3Lower(tn, t + tin);
			for (int i = i0; i < i1; i++)
			{
				double x = col[i] * H3_CW, hy = H / 2 + v * (t - tn[i]);
				if (p.x + p.r - 2 > x + 6 && p.x - p.r + 2 < x + H3_CW - 6 && p.y + p.r - 2 > hy - len && p.y - p.r + 2 < hy) return true;
			}
			return false;
		};
		return h;
	}

	// the teddy rain: bears fall on a grid (s apart) that drifts with the wind (vx, vy px a second). Landing k (at tl[k]) uses grid par[k]:
	// 0 = the grid, 1 = moved half a step both ways, 2 = half a step across, 3 = half a step down. A landed bear kills R round for hold s.
	// Its shadow grows on the floor wn s before
	static readonly double[][] H3_PAR = { new[] { 0.0, 0.0 }, new[] { 0.5, 0.5 }, new[] { 0.5, 0.0 }, new[] { 0.0, 0.5 } };
	static double H3Wrap(double v, double s) { double m = v % s; if (m < 0) m += s; return m; }
	public static Hz H3Rain(double[] tl, double[] par, double s, double R, double ox, double oy, double vx, double vy, double wn, double hold)
	{
		var h = new Hz { kind = "H3Rain" }; double te = tl.Length > 0 ? tl[^1] + hold : 0;
		(double gx, double gy) org(int k) => (ox + vx * tl[k] + H3_PAR[(int)par[k]][0] * s, oy + vy * tl[k] + H3_PAR[(int)par[k]][1] * s);
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < tl.Length; k++)
			{
				double u = t - tl[k]; if (u < -wn || u > hold + 0.35) continue;
				var (gx, gy) = org(k); double x0 = H3Wrap(gx, s) - s, y0 = H3Wrap(gy, s) - s;
				for (double x = x0; x < W + s; x += s) for (double y = y0; y < H + s; y += s)
				{
					if (x < -R || x > W + R || y < -R || y > H + R) continue;
					if (u < 0)
					{
						double q = 1 + u / wn; g.fillStyle = $"rgba(0,0,0,{str(0.25 + 0.45 * q)})"; g.beginPath(); g.ellipse(x, y, R * (0.25 + 0.75 * q), R * (0.2 + 0.7 * q), 0, 0, TAU); g.fill();
						g.strokeStyle = H3G(0.15 + 0.55 * q); g.lineWidth = 2; g.beginPath(); g.arc(x, y, R * (0.25 + 0.75 * q), 0, TAU); g.stroke();
						// the bear coming down (from above: small and high)
						if (q > 0.55) H3Bear(x, y - (1 - q) * 260, R * 0.25 * q, 0, H3F(0.5 * q), false);
					}
					else if (u < hold) { double sq = u < 0.06 ? 1.25 - u * 4 : 1; g.fillStyle = H3P(0.18); circle(x, y, R); H3Bear(x, y, R * 0.62 * sq, (x * 0.013 + y * 0.007) % 0.6 - 0.3, u < 0.04 ? "#ff8fb0" : PINK); }
					else { double f = 1 - (u - hold) / 0.35; H3Bear(x, y, R * 0.55, 0.4, H3F(0.2 * f), false); }
				}
			}
		};
		h.hit = (p, t) =>
		{
			double rr = R + p.r - 2;
			for (int k = 0; k < tl.Length; k++)
			{
				double u = t - tl[k]; if (u < 0 || u >= hold) continue;
				var (gx, gy) = org(k); double dx = H3Wrap(p.x - gx, s), dy = H3Wrap(p.y - gy, s); dx = Math.Min(dx, s - dx); dy = Math.Min(dy, s - dy);
				if (dx * dx + dy * dy < rr * rr) return true;
			}
			return false;
		};
		return h;
	}

	// the jump rope: two big hands (at the ends) turn a rope that sweeps the whole room. axis 0: the rope is across, it sweeps down (dir 1)
	// or up (dir -1); axis 1: the rope stands up and sweeps right (1) or left (-1). Sweep k starts at ts[k] and takes sd s; th thick.
	// No gap: only a dash gets you through. The rope's way back (over the top) shows as a grey dashed line, wn s before
	public static Hz H3Rope(int axis, double[] ts, double[] dir, double sd, double th, double wn)
	{
		var h = new Hz { kind = "H3Rope" }; double L = axis == 0 ? H : W, Lo = axis == 0 ? W : H, te = ts.Length > 0 ? ts[^1] + sd : 0;
		double pos(int k, double t) { double u = clamp((t - ts[k]) / sd, 0, 1); return dir[k] > 0 ? -20 + u * (L + 40) : L + 20 - u * (L + 40); }
		void line(double v, string col, double w, bool dsh)
		{
			if (dsh) { if (axis == 0) H3Dash(0, v, W, v, col, w); else H3Dash(v, 0, v, H, col, w); return; }
			g.fillStyle = col; if (axis == 0) g.fillRect(0, v - w / 2, W, w); else g.fillRect(v - w / 2, 0, w, H);
		}
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < ts.Length; k++)
			{
				double u = t - ts[k];
				if (u < -wn || u > sd + 0.12) continue;
				if (u < 0)
				{
					// coming round: the rope's way back over the top, the other way
					double q = 1 + u / wn, bk = dir[k] > 0 ? L + 20 - q * (L + 40) : -20 + q * (L + 40);
					line(bk, H3G(0.25 + 0.5 * q), 3, true);
					// where it starts
					g.fillStyle = H3P(0.1 + 0.25 * q); if (axis == 0) g.fillRect(0, dir[k] > 0 ? 0 : H - 10, W, 10); else g.fillRect(dir[k] > 0 ? 0 : W - 10, 0, 10, H);
					continue;
				}
				double v = pos(k, t), f = u > sd ? 1 - (u - sd) / 0.12 : 1;
				g.save(); g.globalAlpha = f; g.shadowColor = PINK; g.shadowBlur = 18; line(v, u < 0.03 ? "#fff" : PINK, th, false); g.shadowBlur = 0;
				// a motion smear behind it
				double back = dir[k] > 0 ? -1 : 1; g.fillStyle = H3P(0.18); if (axis == 0) g.fillRect(0, Math.Min(v, v + back * 70), W, 70); else g.fillRect(Math.Min(v, v + back * 70), 0, 70, H);
				g.restore();
			}
			// the hands at the rope's two ends, turning
			bool on = false; foreach (var t0 in ts) if (t > t0 - wn && t < t0 + sd + 0.3) on = true;
			if (on)
			{
				double an = BT(t) * TAU / 2;
				for (int sd2 = 0; sd2 < 2; sd2++)
				{
					double hx = axis == 0 ? (sd2 == 0 ? 18 : W - 18) : W / 2 + JS.cos(an) * 30, hy = axis == 0 ? H / 2 + JS.sin(an) * 30 : (sd2 == 0 ? 18 : H - 18);
					g.fillStyle = "rgba(244,230,234,0.85)"; circle(hx, hy, 22); g.fillStyle = "rgba(60,0,18,0.7)"; circle(hx, hy, 9);
				}
			}
		};
		h.hit = (p, t) =>
		{
			double c = axis == 0 ? p.y : p.x, hw = th / 2 + p.r - 2;
			for (int k = 0; k < ts.Length; k++)
			{
				if (t < ts[k] || t > ts[k] + sd) continue;
				// it is fast: the smear behind it (what it swept in the last 1/60 s) kills too
				double a = pos(k, t - 1.0 / 60), b = pos(k, t); if (c > Math.Min(a, b) - hw && c < Math.Max(a, b) + hw) return true;
			}
			return false;
		};
		return h;
	}

	// the needle sews the room into rooms: wall i goes from (x0[i],y0[i]) to (x1[i],y1[i]) (edge to edge), the needle runs it from ws[i]
	// for wd s and the thread behind it kills. The button (sx,sy) shows the room that stays: at tf every other room fills with stuffing
	// (it kills until tu); at tu the threads are pulled out. The walls show as grey dashes from tw
	public static Hz H3Needle(double[] x0, double[] y0, double[] x1, double[] y1, double[] ws, double wd, double sx, double sy, double tw, double tf, double tu)
	{
		var h = new Hz { kind = "H3Needle" }; int n = x0.Length;
		double side(int i, double px, double py) => (x1[i] - x0[i]) * (py - y0[i]) - (y1[i] - y0[i]) * (px - x0[i]);
		double sideS(int i) => Math.Sign(side(i, sx, sy));
		double nrm(int i) => hypot(x1[i] - x0[i], y1[i] - y0[i]);
		// the room that stays, as a polygon (the screen cut by every wall)
		var poly = new List<(double x, double y)> { (0, 0), (W, 0), (W, H), (0, H) };
		for (int i = 0; i < n; i++)
		{
			var np = new List<(double x, double y)>(); double ss = sideS(i);
			for (int j = 0; j < poly.Count; j++)
			{
				var a = poly[j]; var b = poly[(j + 1) % poly.Count]; double da = side(i, a.x, a.y) * ss, db = side(i, b.x, b.y) * ss;
				if (da >= 0) np.Add(a);
				if ((da >= 0) != (db >= 0)) { double u = da / (da - db); np.Add((a.x + (b.x - a.x) * u, a.y + (b.y - a.y) * u)); }
			}
			poly = np;
		}
		(double x, double y) tip(int i, double t) { double u = H3k(t, ws[i], ws[i] + wd); return (x0[i] + (x1[i] - x0[i]) * u, y0[i] + (y1[i] - y0[i]) * u); }
		h.update = (t, dt) => { if (t > tu + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double out_ = H3k(t, tu, tu + 0.2);
			// the stuffing
			if (t >= tf - SPB && t < tu + 0.2)
			{
				bool on = t >= tf; double q = H3k(t, tf - SPB, tf);
				g.save(); g.globalAlpha = 1 - out_; g.fillStyle = on ? H3P(t - tf < 0.06 ? 0.95 : 0.8) : H3P(0.05 + 0.25 * q);
				g.beginPath(); g.rect(0, 0, W, H); if (poly.Count > 2) { g.moveTo(poly[0].x, poly[0].y); for (int j = poly.Count - 1; j >= 1; j--) g.lineTo(poly[j].x, poly[j].y); g.closePath(); } g.fill("evenodd");
				if (on) { g.fillStyle = "rgba(255,230,236,0.35)"; for (int i = 0; i < 26; i++) { double fx = (i * 197 + 61) % W, fy = (i * 113 + 37) % H; bool inS = true; for (int j = 0; j < n; j++) if (Math.Sign(side(j, fx, fy)) != sideS(j)) inS = false; if (!inS) circle(fx, fy, 16 + (i % 4) * 6); } }
				g.restore();
			}
			// the button: the room that stays
			if (t < tu) { double q = H3k(t, tw, tw + 0.25); g.fillStyle = $"rgba(255,245,250,{str(0.25 + 0.6 * q)})"; circle(sx, sy, 24); g.fillStyle = "rgba(30,0,10,0.8)"; for (int i = 0; i < 4; i++) circle(sx + (i % 2 == 0 ? -7 : 7), sy + (i < 2 ? -7 : 7), 3.5); g.strokeStyle = $"rgba(255,245,250,{str(0.6 * q)})"; g.lineWidth = 2; g.beginPath(); g.arc(sx, sy, 34 + 6 * pulse, 0, TAU); g.stroke(); }
			for (int i = 0; i < n; i++)
			{
				var tp = tip(i, t);
				if (t < ws[i] + wd) H3Dash(x0[i], y0[i], x1[i], y1[i], H3G(0.25 + 0.5 * H3k(t, tw, ws[i])), 2);
				if (t < ws[i]) continue;
				g.save(); g.globalAlpha = 1 - out_;
				// the thread: a stitched line
				g.strokeStyle = PINK; g.lineWidth = 10; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.moveTo(x0[i], y0[i]); g.lineTo(tp.x, tp.y); g.stroke(); g.shadowBlur = 0;
				double L = nrm(i), ux = (x1[i] - x0[i]) / L, uy = (y1[i] - y0[i]) / L, d = hypot(tp.x - x0[i], tp.y - y0[i]);
				g.strokeStyle = "rgba(255,235,240,0.8)"; g.lineWidth = 2; g.beginPath();
				for (double s = 10; s < d; s += 22) { double cx = x0[i] + ux * s, cy = y0[i] + uy * s; g.moveTo(cx - uy * 9 - ux * 4, cy + ux * 9 - uy * 4); g.lineTo(cx + uy * 9 + ux * 4, cy - ux * 9 + uy * 4); }
				g.stroke();
				// the needle
				if (t < ws[i] + wd + 0.1) { g.save(); g.translate(tp.x, tp.y); g.rotate(JS.atan2(uy, ux)); g.fillStyle = "#e8e4ee"; g.beginPath(); g.moveTo(26, 0); g.lineTo(-60, -5); g.lineTo(-60, 5); g.closePath(); g.fill(); g.fillStyle = "#000"; g.fillRect(-54, -1.5, 10, 3); g.restore(); }
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			if (t < tw || t >= tu) return false; double rr = 5 + p.r - 2;
			for (int i = 0; i < n; i++) { if (t < ws[i]) continue; var tp = tip(i, t); if (H3SegD(p.x, p.y, x0[i], y0[i], tp.x, tp.y) < rr) return true; }
			if (t >= tf) for (int i = 0; i < n; i++) if (side(i, p.x, p.y) * sideS(i) < 0) return true;
			return false;
		};
		return h;
	}

	// ---------- part 2: the box ----------
	// the mystery box wheel: the box in the middle (radius bR, it kills) and n slices round it. The pointer jumps to slice sk[i] at tk[i]
	// (it slows down, so you can see where it will stop); at tb every slice but the last one it points at blasts for hold s
	public static Hz H3Wheel(double cx, double cy, int n, double bR, double[] tk, double[] sk, double tb, double hold, double tw)
	{
		var h = new Hz { kind = "H3Wheel" }; double sw = TAU / n; int safe = (int)sk[^1];
		double ang(int s) => -Math.PI / 2 + (s + 0.5) * sw;
		double cur(double t)
		{
			int i = H3Lower(tk, t + 1e-9) - 1; if (i < 0) return ang((int)sk[0]);
			double a = ang((int)sk[i]); if (i == 0) return a; double a0 = ang((int)sk[i - 1]); double d = a - a0; d -= Math.Floor(d / TAU) * TAU; double u = H3k(t, tk[i], tk[i] + 0.05); return a0 + d * u;
		}
		h.update = (t, dt) => { if (t > tb + hold + 0.1) h.dead = true; };
		h.draw = t =>
		{
			// gone as soon as its blast ends (the next spin is already up)
			if (t < tw || t >= tb + hold) return; double a = H3k(t, tw, tw + 0.15); g.save(); g.globalAlpha = a;
			bool bl = t >= tb && t < tb + hold, stop = t >= tk[^1];
			for (int s = 0; s < n; s++)
			{
				double a0 = -Math.PI / 2 + s * sw;
				if (bl && s != safe) { g.fillStyle = t - tb < 0.05 ? "#fff" : PINK; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 1500, a0, a0 + sw); g.closePath(); g.fill(); continue; }
				if (stop && !bl && t < tb) { double q = H3k(t, tk[^1], tb); if (s != safe) { g.fillStyle = H3P(0.06 + 0.3 * q); g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 1500, a0, a0 + sw); g.closePath(); g.fill(); } }
				g.strokeStyle = H3G(0.35); g.lineWidth = 2; g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + JS.cos(a0) * 1500, cy + JS.sin(a0) * 1500); g.stroke();
			}
			// the pointer
			double pa = cur(t); g.strokeStyle = stop ? "#fff" : "rgba(160,200,255,0.9)"; g.lineWidth = 6; g.beginPath(); g.moveTo(cx + JS.cos(pa) * bR, cy + JS.sin(pa) * bR); g.lineTo(cx + JS.cos(pa) * (bR + 120), cy + JS.sin(pa) * (bR + 120)); g.stroke();
			g.fillStyle = stop ? "#fff" : "rgba(160,200,255,0.9)"; g.beginPath(); g.moveTo(cx + JS.cos(pa) * (bR + 140), cy + JS.sin(pa) * (bR + 140)); g.lineTo(cx + JS.cos(pa + 0.12) * (bR + 110), cy + JS.sin(pa + 0.12) * (bR + 110)); g.lineTo(cx + JS.cos(pa - 0.12) * (bR + 110), cy + JS.sin(pa - 0.12) * (bR + 110)); g.closePath(); g.fill();
			// the box
			g.save(); g.translate(cx, cy); g.rotate(0.05 * JS.sin(t * 9)); g.fillStyle = "#5a3a22"; g.fillRect(-bR * 0.9, -bR * 0.65, bR * 1.8, bR * 1.3); g.strokeStyle = PINK; g.lineWidth = 4; g.strokeRect(-bR * 0.9, -bR * 0.65, bR * 1.8, bR * 1.3);
			g.fillStyle = "rgba(160,200,255,0.9)"; g.fillRect(-bR * 0.9, -bR * 0.12, bR * 1.8, bR * 0.24); g.restore();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw + 0.2 || t >= tb + hold) return false;
			double dx = p.x - cx, dy = p.y - cy; if (Math.Abs(dx) < bR * 0.9 + p.r - 2 && Math.Abs(dy) < bR * 0.65 + p.r - 2) return true;
			if (t < tb) return false;
			// in the safe slice (with the square's size inside it)?
			double a = JS.atan2(dy, dx) + Math.PI / 2; a -= Math.Floor(a / TAU) * TAU; double s0 = safe * sw, d = hypot(dx, dy), m = d > 1 ? Math.Asin(Math.Min(1, (p.r - 2) / d)) : Math.PI;
			return !(a > s0 + m && a < s0 + sw - m);
		};
		return h;
	}

	// the bear that tears: it starts at (x,y) at t0 going at angle a, v px a second; every dt s every piece tears in two (spreading +-sp, the
	// spread halves each time) dep times; pieces get smaller (x0.8) and faster (x1.12). The torn paths show one tear ahead
	public class H3Node { public double t0, t1, x, y, a, v, r; public int d; }
	public static Hz H3Split(double x, double y, double a, double v, double t0, double dt, double sp, int dep, double r0, double tw)
	{
		var h = new Hz { kind = "H3Split" }; var N = new List<H3Node>(); double tEnd = t0 + dt * dep + 4;
		void add(double ts, double nx, double ny, double na, double nv, double nr, int d)
		{
			var nd = new H3Node { t0 = ts, t1 = d < dep ? ts + dt : tEnd, x = nx, y = ny, a = na, v = nv, r = nr, d = d }; N.Add(nd);
			if (d >= dep) return; double ex = nx + JS.cos(na) * nv * dt, ey = ny + JS.sin(na) * nv * dt, s = sp / Math.Pow(2, d);
			add(ts + dt, ex, ey, na - s, nv * 1.12, nr * 0.8, d + 1); add(ts + dt, ex, ey, na + s, nv * 1.12, nr * 0.8, d + 1);
		}
		add(t0, x, y, a, v, r0, 0);
		(double x, double y) at(H3Node nd, double t) { double u = t - nd.t0; return (nd.x + JS.cos(nd.a) * nd.v * u, nd.y + JS.sin(nd.a) * nd.v * u); }
		bool gone(H3Node nd, double t) { var q = at(nd, t); return q.x < -100 || q.x > W + 100 || q.y < -100 || q.y > H + 100; }
		h.update = (t, dt2) => { if (t > t0 + dt * dep + 0.5 && N.Where(nd => nd.d == dep).All(nd => gone(nd, t))) h.dead = true; if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			foreach (var nd in N)
			{
				// its path: from one tear before it starts until it ends
				if (t >= nd.t0 - dt && t < nd.t1) { var e = at(nd, Math.Min(nd.t1, nd.t0 + (nd.d == dep ? 3 : dt))); double q = H3k(t, nd.t0 - dt, nd.t0); H3Dash(nd.x, nd.y, e.x, e.y, H3G(0.2 + 0.5 * q), 2); }
				if (nd.d == 0 && t < t0) { double q = H3k(t, tw, t0); H3Bear(x, y, r0 * (0.6 + 0.4 * q), 0, H3F(0.3 + 0.5 * q), q > 0.8); continue; }
				if (t < nd.t0 || t >= nd.t1) continue;
				var p = at(nd, t); double rot = nd.a + Math.PI / 2 + (nd.d % 2 == 0 ? 0.3 : -0.3);
				g.fillStyle = H3P(0.2); circle(p.x, p.y, nd.r * 1.25);
				H3Bear(p.x, p.y, nd.r, rot, t - nd.t0 < 0.05 ? "#fff" : PINK);
				// the torn edge
				if (nd.d > 0) { g.strokeStyle = "rgba(255,235,240,0.7)"; g.lineWidth = 2; g.beginPath(); g.moveTo(p.x - nd.r * 0.8, p.y); for (int i = 0; i < 6; i++) g.lineTo(p.x - nd.r * 0.8 + i * nd.r * 0.32, p.y + (i % 2 == 0 ? -5 : 5)); g.stroke(); }
			}
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false;
			foreach (var nd in N) { if (t < nd.t0 || t >= nd.t1) continue; var q = at(nd, t); double rr = nd.r * 0.95 + p.r - 2; if ((p.x - q.x) * (p.x - q.x) + (p.y - q.y) * (p.y - q.y) < rr * rr) return true; }
			return false;
		};
		return h;
	}

	// the scissors: from tw they follow the square (grey); at tl they lock on where it is. The pivot is D px from that point at angle ang,
	// the blades (L long, w thick) open +-op round the line to it; from tc they close in cd s and stay shut hold s
	public static Hz H3Snip(double ang, double D, double op, double L, double w, double tw, double tl, double tc, double cd, double hold)
	{
		var h = new Hz { kind = "H3Snip" }; bool lk = false; double lx = W / 2, ly = H / 2;
		(double x, double y) aim() => lk ? (lx, ly) : player != null ? (player.x, player.y) : (W / 2, H / 2);
		double open(double t) => op * (1 - H3k(t, tc, tc + cd));
		h.update = (t, dt) => { if (!lk && t >= tl) { lk = true; var a = player != null ? (player.x, player.y) : (W / 2, H / 2); lx = a.Item1; ly = a.Item2; } if (t > tc + cd + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; var (ax, ay) = aim(); double px = ax + JS.cos(ang) * D, py = ay + JS.sin(ang) * D, ba = ang + Math.PI, o = open(t);
			bool on = t >= tc, shut = t >= tc + cd; double f = 1 - H3k(t, tc + cd + hold, tc + cd + hold + 0.25), q = H3k(t, tw, tl);
			g.save(); g.globalAlpha = f;
			if (!on)
			{
				// the wedge it will close on
				g.fillStyle = lk ? H3P(0.08 + 0.2 * H3k(t, tl, tc)) : H3G(0.05 + 0.06 * q); g.beginPath(); g.moveTo(px, py); g.lineTo(px + JS.cos(ba - o) * L, py + JS.sin(ba - o) * L); g.lineTo(px + JS.cos(ba + o) * L, py + JS.sin(ba + o) * L); g.closePath(); g.fill();
				if (!lk) { g.strokeStyle = H3G(0.4 + 0.4 * q); g.lineWidth = 2; g.beginPath(); g.arc(ax, ay, 26 - 10 * q, 0, TAU); g.stroke(); }
				else { g.strokeStyle = "#fff"; g.lineWidth = 2; g.beginPath(); g.moveTo(ax - 14, ay); g.lineTo(ax + 14, ay); g.moveTo(ax, ay - 14); g.lineTo(ax, ay + 14); g.stroke(); }
			}
			foreach (var sg in new[] { -1.0, 1.0 })
			{
				double bb = ba + sg * o, ex = px + JS.cos(bb) * L, ey = py + JS.sin(bb) * L;
				if (!on) { H3Dash(px, py, ex, ey, lk ? "rgba(255,240,245,0.9)" : H3G(0.3 + 0.4 * q), lk ? 3 : 2); continue; }
				g.save(); g.translate(px, py); g.rotate(bb); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = shut && t - tc - cd < 0.05 ? "#fff" : PINK;
				g.beginPath(); g.moveTo(0, -w / 2); g.lineTo(L, -w * 0.15); g.lineTo(L + 20, 0); g.lineTo(L, w * 0.15); g.lineTo(0, w / 2); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore();
			}
			// the handles and the screw
			g.strokeStyle = on ? PINK : H3G(0.5 + 0.4 * q); g.lineWidth = 6; foreach (var sg in new[] { -1.0, 1.0 }) { double hx = px + JS.cos(ang + sg * (o + 0.35)) * 60, hy = py + JS.sin(ang + sg * (o + 0.35)) * 60; g.beginPath(); g.arc(hx, hy, 26, 0, TAU); g.stroke(); }
			g.fillStyle = "#f4e6ea"; circle(px, py, 9);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tc || t >= tc + cd + hold) return false; var (ax, ay) = aim(); double px = ax + JS.cos(ang) * D, py = ay + JS.sin(ang) * D, ba = ang + Math.PI, rr = w / 2 + p.r - 2;
			// the blades now, and where they were a step ago (they are fast)
			foreach (double tt in new[] { t, t - 1.0 / 240 }) { double o = open(Math.Max(tc, tt)); foreach (var sg in new[] { -1.0, 1.0 }) { double bb = ba + sg * o; if (H3SegD(p.x, p.y, px, py, px + JS.cos(bb) * L, py + JS.sin(bb) * L) < rr) return true; } }
			return false;
		};
		return h;
	}

	// the carousel round (cx,cy): rings at radii rr[i], each with np[i] horse poles, turning by its own plan. On every half beat (from ta)
	// the rings whose turn it is drop their horses (ring i on half beats k with (k + i) even): a dropped horse kills R round for dn s.
	// Its shadow shows from the half beat before. The hub (radius hub, growing by the plan hb/hr) and the fence (rim, by rb/rmr) kill too
	public static Hz H3Carousel(double cx, double cy, double[] rr, double[] np, L15Spin[] sp, double R, double dn, double[] hb, double[] hr, double[] rb, double[] rmr, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H3Carousel" }; double hs = SPB / 2;
		// a size that steps to kv[i] at beat kb[i] (over one beat)
		double plan(double[] kb, double[] kv, double bt) { if (kv.Length == 0) return 0; double v = kv[0]; for (int i = 1; i < kb.Length && bt >= kb[i]; i++) v = kv[i - 1] + (kv[i] - kv[i - 1]) * clamp(bt - kb[i], 0, 1); return v; }
		double hubR(double t) => plan(hb, hr, BT(t));
		double rimR(double t) => plan(rb, rmr, BT(t));
		bool down(int i, double t, out double u) { u = 0; if (t < ta || t >= te) return false; double f = (t - ta) / hs; int k = (int)Math.Floor(f); u = t - ta - k * hs; return (k + i) % 2 == 0 && u < dn; }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(H3k(t, tw, tw + 0.4), 1 - H3k(t, te, te + 0.4)), bt = BT(t); bool live = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			// the fence: outside it is the dark crowd of toys
			double rm = rimR(t); g.fillStyle = live ? H3P(0.55) : H3G(0.12); g.beginPath(); g.rect(0, 0, W, H); g.moveTo(cx + rm, cy); g.arc(cx, cy, rm, TAU, 0, true); g.closePath(); g.fill("evenodd");
			g.strokeStyle = live ? PINK : H3G(0.5); g.lineWidth = 4; g.beginPath(); g.arc(cx, cy, rm, 0, TAU); g.stroke();
			// its next size, dashed
			double rm1 = plan(rb, rmr, bt + 2); if (Math.Abs(rm1 - rm) > 2) { g.strokeStyle = "rgba(255,240,245,0.7)"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(cx, cy, rm1, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			for (int i = 0; i < rr.Length; i++)
			{
				g.strokeStyle = "rgba(200,170,120,0.25)"; g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, rr[i], 0, TAU); g.stroke();
				double th = sp[i].At(bt); bool dn0 = down(i, t, out double u);
				// the next drop of this ring: shadows from the half beat before
				double f = (t - ta) / hs; int k = (int)Math.Floor(f); bool nextMine = (((k + 1 + i) % 2) + 2) % 2 == 0; double q = live || t >= ta - hs ? clamp(f - k, 0, 1) : 0;
				for (int j = 0; j < (int)np[i]; j++)
				{
					double an = th + j * TAU / np[i], x = cx + JS.cos(an) * rr[i], y = cy + JS.sin(an) * rr[i];
					if (dn0) { g.fillStyle = H3P(0.25); circle(x, y, R); g.fillStyle = u < 0.04 ? "#fff" : PINK; circle(x, y, R * 0.62); g.fillStyle = "rgba(255,235,240,0.9)"; g.fillRect(x - 3, y - R, 6, R * 2); continue; }
					if (nextMine && t >= ta - hs && t < te - hs) { g.fillStyle = $"rgba(0,0,0,{str(0.2 + 0.4 * q)})"; circle(x, y, R * (0.4 + 0.6 * q)); g.strokeStyle = H3G(0.2 + 0.6 * q); g.lineWidth = 2; g.beginPath(); g.arc(x, y, R * (0.4 + 0.6 * q), 0, TAU); g.stroke(); }
					// the pole, up
					g.fillStyle = "rgba(220,200,160,0.55)"; circle(x, y, 7); g.fillStyle = "rgba(200,170,120,0.35)"; g.beginPath(); g.ellipse(x, y, R * 0.5, R * 0.3, an, 0, TAU); g.fill();
				}
			}
			// the hub
			double hr0 = hubR(t), hr1 = plan(hb, hr, bt + 2); g.fillStyle = live ? PINK : H3G(0.3); circle(cx, cy, hr0); g.fillStyle = "rgba(40,0,12,0.5)"; circle(cx, cy, hr0 * 0.6);
			if (Math.Abs(hr1 - hr0) > 2) { g.strokeStyle = "rgba(255,240,245,0.7)"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(cx, cy, hr1, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy);
			if (d < hubR(t) + p.r - 2 || d > rimR(t) - p.r + 2) return true;
			double bt = BT(t), rq = R * 0.95 + p.r - 2;
			for (int i = 0; i < rr.Length; i++)
			{
				if (!down(i, t, out _)) continue; if (Math.Abs(d - rr[i]) > rq) continue; double th = sp[i].At(bt);
				for (int j = 0; j < (int)np[i]; j++) { double an = th + j * TAU / np[i], x = cx + JS.cos(an) * rr[i], y = cy + JS.sin(an) * rr[i]; if ((p.x - x) * (p.x - x) + (p.y - y) * (p.y - y) < rq * rq) return true; }
			}
			return false;
		};
		return h;
	}

	// ---------- part 3: the nightmare ----------
	// the mirror: the whole room (not the hearts and the bar) turns over left to right at the beats fb[i] (in 0.25 beat), and back at the
	// next one. A beat before, the mirror cracks and the music box chimes
	public static Hz H3Flip(double[] fb)
	{
		var h = new Hz { kind = "H3Flip" }; double tEnd = fb.Length > 0 ? B(fb[^1]) + 1 : 0; int rung = -1;
		h.o = new Dictionary<string, object> { ["f"] = (Func<double, double>)(t => { double bt = BT(t), v = 0; for (int i = 0; i < fb.Length; i++) { double u = clamp((bt - fb[i]) / 0.25, 0, 1); v = i % 2 == 0 ? u : 1 - u; if (bt < fb[i] + 0.25) break; } return v; }) };
		h.update = (t, dt) =>
		{
			double bt = BT(t); for (int i = 0; i < fb.Length; i++) if (bt >= fb[i] - 1 && bt < fb[i] - 0.9 && rung < i) { rung = i; sfx(tt => { sTone(tt, 1568, 0.5, "sine", 0.16); sTone(tt + 0.08, 2093, 0.45, "sine", 0.12); }); }
			if (t > tEnd) h.dead = true;
		};
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < fb.Length; i++)
			{
				if (bt < fb[i] - 1 || bt > fb[i] + 0.4) continue; double q = clamp(bt - fb[i] + 1, 0, 1), f = bt > fb[i] ? 1 - (bt - fb[i]) / 0.4 : 1;
				g.save(); g.globalAlpha = Math.Max(0, f); g.strokeStyle = "rgba(235,240,255,0.85)"; g.lineWidth = 2.5; g.beginPath();
				for (int c = 0; c < 9; c++) { double a0 = c * 0.71 + 0.3 + i, l = (380 + c * 40) * q; g.moveTo(W / 2, H / 2); g.lineTo(W / 2 + JS.cos(a0) * l * 0.4, H / 2 + JS.sin(a0) * l * 0.4 + 20); g.lineTo(W / 2 + JS.cos(a0 + 0.2) * l, H / 2 + JS.sin(a0 + 0.2) * l); }
				g.stroke(); g.fillStyle = $"rgba(235,240,255,{str(0.1 * q)})"; g.fillRect(0, 0, W, H); g.restore();
			}
		};
		return h;
	}
	// how far the room is turned over now (0 normal, 1 mirrored): Loop.cs draws the room through this
	public static double H3FlipNow()
	{
		if (hazards == null) return 0;
		foreach (var h in hazards) if (h.kind == "H3Flip" && !h.dead && h.o != null && h.o.TryGetValue("f", out var f)) return ((Func<double, double>)f)(songT);
		return 0;
	}

	// a spinning top from (x,y) at angle a, v px a second, from t0: it bounces off the walls and gets faster (x ac) at each bounce, nb bounces,
	// then rolls out. Its next two runs show as grey dashes
	public static Hz H3Top(double x, double y, double a, double v, double t0, int nb, double ac, double R, double tw)
	{
		var h = new Hz { kind = "H3Top" };
		var S = new List<(double t, double x, double y, double vx, double vy)>(); double cx = x, cy = y, vx = JS.cos(a) * v, vy = JS.sin(a) * v, ct = t0;
		for (int b = 0; b <= nb; b++)
		{
			S.Add((ct, cx, cy, vx, vy));
			double tx = vx > 0 ? (W - R - cx) / vx : vx < 0 ? (R - cx) / vx : double.PositiveInfinity, ty = vy > 0 ? (H - R - cy) / vy : vy < 0 ? (R - cy) / vy : double.PositiveInfinity;
			double tt = Math.Max(0, Math.Min(tx, ty)); if (b == nb) { tt = 3; S.Add((ct + tt, cx + vx * tt, cy + vy * tt, vx, vy)); break; }
			cx += vx * tt; cy += vy * tt; ct += tt; if (tx <= ty) vx = -vx; if (ty <= tx) vy = -vy; vx *= ac; vy *= ac;
		}
		(double x, double y) at(double t) { int i = 0; while (i < S.Count - 2 && t >= S[i + 1].t) i++; var s = S[i]; double u = t - s.t; return (s.x + s.vx * u, s.y + s.vy * u); }
		h.update = (t, dt) => { if (t > S[^1].t) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double q = H3k(t, tw, t0);
			int i = 0; while (i < S.Count - 2 && t >= S[i + 1].t) i++;
			var c = t < t0 ? (x, y) : at(t);
			// the path: to the next two bounces
			double px = c.Item1, py = c.Item2;
			for (int j = i; j < Math.Min(S.Count - 1, i + 2); j++) { var e = S[j + 1]; H3Dash(px, py, e.x, e.y, H3G(j == i ? 0.25 + 0.5 * q : 0.25), 2); px = e.x; py = e.y; }
			if (t < t0) { H3TopDraw(x, y, R * (0.5 + 0.5 * q), t * 8, H3F(0.4 + 0.5 * q)); return; }
			g.fillStyle = H3P(0.2); circle(c.Item1, c.Item2, R * 1.3); H3TopDraw(c.Item1, c.Item2, R, t * 22, t - t0 < 0.05 ? "#fff" : PINK);
		};
		h.hit = (p, t) => { if (t < t0) return false; var c = at(t); double rr = R + p.r - 2; return (p.x - c.x) * (p.x - c.x) + (p.y - c.y) * (p.y - c.y) < rr * rr; };
		return h;
	}

	// the cat's cradle: two hands (fingers fy[] on each, d px from the middle) hold strings from left finger i to right finger pm[k][i].
	// At beat cb[k] the strings move to the new fingers pm[k] (in 0.25 beat; the new strings show grey dashed from a beat before).
	// The whole cradle turns round the middle by the plan sp. Strings are th thick; grey from tw, they kill ta .. te
	public static Hz H3Cradle(double[] fy, double d, double[][] pm, double[] cb, L15Spin sp, double th, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H3Cradle" }; int n = fy.Length; const double EXT = 1600;
		double rf(int i, double bt) { int k = H3Lower(cb, bt + 1e-9) - 1; if (k < 0) return fy[(int)pm[0][i]]; double a = fy[(int)pm[k][i]]; if (k == 0) return a; double b0 = fy[(int)pm[k - 1][i]]; return b0 + (a - b0) * clamp((bt - cb[k]) / 0.25, 0, 1); }
		// one string in the room: from the left finger through the right one, long enough to cross the whole room
		(double x0, double y0, double x1, double y1) str_(double ly, double ry, double an)
		{
			double ux = 2 * d, uy = ry - ly, l = hypot(ux, uy); ux /= l; uy /= l;
			double ax = -d - ux * EXT, ay = ly - uy * EXT, bx = d + ux * EXT, by = ry + uy * EXT, c = JS.cos(an), s = JS.sin(an);
			return (W / 2 + ax * c - ay * s, H / 2 + ax * s + ay * c, W / 2 + bx * c - by * s, H / 2 + bx * s + by * c);
		}
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), an = sp.At(bt), a = 1 - H3k(t, te, te + 0.4); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			// the next strings
			int k1 = H3Lower(cb, bt + 1e-9);
			if (k1 < cb.Length && bt >= cb[k1] - 1) for (int i = 0; i < n; i++) { if ((int)pm[k1][i] == (int)pm[Math.Max(0, k1 - 1)][i]) continue; var s = str_(fy[i], fy[(int)pm[k1][i]], sp.At(cb[k1])); H3Dash(s.x0, s.y0, s.x1, s.y1, "rgba(255,240,245,0.65)", 2); }
			for (int i = 0; i < n; i++)
			{
				var s = str_(fy[i], rf(i, bt), an);
				if (!on) { H3Dash(s.x0, s.y0, s.x1, s.y1, H3G(0.3 + 0.5 * H3k(t, tw, ta)), 3); continue; }
				g.strokeStyle = PINK; g.lineWidth = th; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.moveTo(s.x0, s.y0); g.lineTo(s.x1, s.y1); g.stroke(); g.shadowBlur = 0;
			}
			// the hands' fingertips
			double c = JS.cos(an), sn = JS.sin(an);
			for (int sd = -1; sd <= 1; sd += 2) for (int i = 0; i < n; i++) { double x = sd * d, y = fy[i]; g.fillStyle = "rgba(244,230,234,0.85)"; circle(W / 2 + x * c - y * sn, H / 2 + x * sn + y * c, 11); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), an = sp.At(bt), rr = th / 2 + p.r - 2;
			for (int i = 0; i < n; i++) { var s = str_(fy[i], rf(i, bt), an); if (H3SegD(p.x, p.y, s.x0, s.y0, s.x1, s.y1) < rr) return true; }
			// a string that is moving: where it was 1/120 s ago and 1/60 s ago (it can be fast)
			for (int q = 1; q <= 2; q++) { double b0 = BT(t - q / 120.0), a0 = sp.At(b0);
				for (int i = 0; i < n; i++) { var s = str_(fy[i], rf(i, b0), a0); if (H3SegD(p.x, p.y, s.x0, s.y0, s.x1, s.y1) < rr) return true; } }
			return false;
		};
		return h;
	}

	// the end: the big bear waves bye bye (harmless)
	public static Hz H3Bye(double t0, double t1)
	{
		var h = new Hz { kind = "H3Bye" };
		h.update = (t, dt) => { if (t > t1 + 1.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = H3k(t, t0, t0 + 0.6), f = 1 - H3k(t, t1, t1 + 1.2);
			g.save(); g.globalAlpha = f; H3Bear(W / 2, H / 2 - 10, 60 + 120 * k, 0.08 * JS.sin(t * 3), H3F(0.85), true);
			double wv = JS.sin(t * 9) * 0.5; g.save(); g.translate(W / 2 + (60 + 120 * k) * 1.05, H / 2 + 40); g.rotate(-0.9 + wv); g.fillStyle = H3F(0.85); g.fillRect(-14, -90 * k, 28, 90 * k); circle(0, -90 * k, 24 * k); g.restore();
			g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_H3.json) ----------
	// times are beats; an event fires when its warning starts (Hardcore has no Easy)
	static double[] H3Sec(double[] bs) => bs.Select(B).ToArray();
	static L15Spin H3Spin(DodgeEv e, string pre, double b0, double b1) => new L15Spin(e.num(pre + "a0") * Math.PI / 180, b0, b1, e.nums(pre + "sb"), e.nums(pre + "sw"));

	static readonly int _kh3fx = DodgeKind("H3fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 1.5)), t0 = B(e.b); var h = new Hz { kind = "H3fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _kh3stage = DodgeKind("H3stage", e => () => hazards.Add(H3Stage(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _kh3bye = DodgeKind("H3bye", e => () => hazards.Add(H3Bye(B(e.b), B(e.num("to")))));
	static readonly int _kh3pins = DodgeKind("H3pins", e => () => hazards.Add(H3Pins(H3Sec(e.nums("tn")), e.nums("col"), e.num("v"), e.num("len"), e.num("wn") * SPB, B(e.num("to")))));
	static readonly int _kh3rain = DodgeKind("H3rain", e => () =>
		hazards.Add(H3Rain(H3Sec(e.nums("tl")), e.nums("par"), e.num("s"), e.num("R"), e.num("ox"), e.num("oy"), e.num("vx"), e.num("vy"), e.num("wn") * SPB, e.num("hold") * SPB)));
	static readonly int _kh3rope = DodgeKind("H3rope", e => () =>
		hazards.Add(H3Rope(e.i("axis"), H3Sec(e.nums("ts")), e.nums("dir"), e.num("sd") * SPB, e.num("th"), e.num("wn") * SPB)));
	static readonly int _kh3needle = DodgeKind("H3needle", e => () =>
		hazards.Add(H3Needle(e.nums("x0"), e.nums("y0"), e.nums("x1"), e.nums("y1"), H3Sec(e.nums("ws")), e.num("wd") * SPB, e.num("sx"), e.num("sy"), B(e.b), B(e.num("tf")), B(e.num("tu")))));
	static readonly int _kh3wheel = DodgeKind("H3wheel", e => () =>
		hazards.Add(H3Wheel(e.num("x", W / 2), e.num("y", H / 2), e.i("n"), e.num("bR"), H3Sec(e.nums("tk")), e.nums("sk"), B(e.num("tb")), e.num("hold") * SPB, B(e.b))));
	static readonly int _kh3split = DodgeKind("H3split", e => () =>
		hazards.Add(H3Split(e.num("x"), e.num("y"), e.num("a") * Math.PI / 180, e.num("v"), B(e.num("hit")), e.num("dt") * SPB, e.num("sp") * Math.PI / 180, e.i("dep"), e.num("r"), B(e.b))));
	static readonly int _kh3snip = DodgeKind("H3snip", e => () =>
		hazards.Add(H3Snip(e.num("ang") * Math.PI / 180, e.num("D"), e.num("op") * Math.PI / 180, e.num("L"), e.num("w"), B(e.b), B(e.num("tl")), B(e.num("tc")), e.num("cd") * SPB, e.num("hold") * SPB)));
	static readonly int _kh3carousel = DodgeKind("H3carousel", e => () =>
	{
		var rr = e.nums("rr"); var sp = new L15Spin[rr.Length]; double b0 = e.b, b1 = e.num("to") + 1;
		for (int i = 0; i < rr.Length; i++) sp[i] = H3Spin(e, "r" + i, b0, b1);
		hazards.Add(H3Carousel(e.num("x", W / 2), e.num("y", H / 2), rr, e.nums("np"), sp, e.num("R"), e.num("dn") * SPB, e.nums("hb"), e.nums("hr"), e.nums("rb"), e.nums("rm"), B(e.b), B(e.num("hit")), B(e.num("to"))));
	});
	static readonly int _kh3view = ((Func<int>)(() => { viewFlip = H3FlipNow; return 0; }))();
	static readonly int _kh3flip = DodgeKind("H3flip", e => () => hazards.Add(H3Flip(e.nums("fb"))));
	static readonly int _kh3top = DodgeKind("H3top", e => () =>
		hazards.Add(H3Top(e.num("x"), e.num("y"), e.num("a") * Math.PI / 180, e.num("v"), B(e.num("hit")), e.i("nb"), e.num("ac", 1.12), e.num("R"), B(e.b))));
	static readonly int _kh3cradle = DodgeKind("H3cradle", e => () =>
	{
		var raw = e.nums("pm"); int n = e.nums("fy").Length; var pm = new double[raw.Length / n][]; for (int k = 0; k < pm.Length; k++) pm[k] = raw.Skip(k * n).Take(n).ToArray();
		hazards.Add(H3Cradle(e.nums("fy"), e.num("d"), pm, e.nums("cb"), H3Spin(e, "", e.b, e.num("to") + 1), e.num("th"), B(e.b), B(e.num("hit")), B(e.num("to"))));
	});
}
