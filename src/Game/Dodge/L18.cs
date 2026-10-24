using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L18 "اعوف الدنيا" (Mohamed Alsalim): I would leave the world for you. A trip out of the world; every attack is a fixed pattern on the beat
// (the song's three hits in a beat: 0, 0.43, 0.74) that asks for one tactic:
// part 1 the world (the turning meridians, night and day over the time zones, the flight routes, the compass that points at you),
// part 2 leaving it (the map folds over, the map tears into sliding strips, the hourglass that turns the arena over, the world closing in),
// part 3 beyond it (two bodies in orbit with their pebble rings and eclipses, the world breaking apart into spiral arms).
// Every hazard is a pure function of time once it exists (the compass also of where the square was when it locked), so hit(p, t) is right
// for any later t. Only pink hurts; warnings are grey lines, never fills over other attacks. The data is data/dodge_L18.json (tools/level18/gen.py).
public static partial class G
{
	const double L18CX = W / 2, L18CY = H / 2;
	static double L18k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L18G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string L18P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static double L18Ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double L18Mod(double a, double m) => ((a % m) + m) % m;
	// the angle between a and b, -PI..PI
	static double L18Ad(double a, double b) => L18Mod(a - b + Math.PI, TAU) - Math.PI;
	static void L18Line(double x0, double y0, double x1, double y1, string col, double w, bool dash)
	{
		g.strokeStyle = col; g.lineWidth = w; if (dash) g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); if (dash) g.setLineDash(new double[0]);
	}
	// a line through (x,y) at angle a, across the whole screen
	static void L18Through(double x, double y, double a, string col, double w, bool dash)
	{
		double c = JS.cos(a) * 1600, s = JS.sin(a) * 1600; L18Line(x - c, y - s, x + c, y + s, col, w, dash);
	}
	static double L18LineDist(double px, double py, double x, double y, double a) => Math.Abs((px - x) * JS.sin(a) - (py - y) * JS.cos(a));

	// ---------- the stages (harmless) ----------
	// part 1: the night sky with the wire globe, 2: the old map, 3: space (the small world in the middle until it breaks at brk)
	public static Hz L18Stage(int part, double t0, double t1, double brk)
	{
		var h = new Hz { kind = "L18Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a; double bt = BT(t);
			if (part == 1)
			{
				g.fillStyle = "rgba(6,10,26,0.75)"; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(200,210,235,0.35)"; for (int i = 0; i < 60; i++) { double sx = (i * 397) % W, sy = (i * 211) % H; g.fillRect(sx, sy, 2, 2); }
				// the wire globe, turning slowly
				double R = 300, s = bt * 0.06; g.strokeStyle = "rgba(120,150,210,0.16)"; g.lineWidth = 2;
				g.beginPath(); g.arc(L18CX, L18CY, R, 0, TAU); g.stroke();
				for (int i = 1; i < 6; i++) { double y = L18CY - R + i * R / 3, rx = Math.Sqrt(Math.Max(0, R * R - (y - L18CY) * (y - L18CY))); g.beginPath(); g.ellipse(L18CX, y, rx, rx * 0.12, 0, 0, TAU); g.stroke(); }
				for (int i = 0; i < 6; i++) { double u = s + i * Math.PI / 6, rx = Math.Abs(JS.sin(u)) * R; g.beginPath(); g.ellipse(L18CX, L18CY, Math.Max(1, rx), R, 0, 0, TAU); g.stroke(); }
			}
			else if (part == 2)
			{
				g.fillStyle = "rgba(10,20,26,0.78)"; g.fillRect(0, 0, W, H);
				g.strokeStyle = "rgba(150,190,200,0.10)"; g.lineWidth = 1; g.beginPath();
				for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
				for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
				// the land, faint
				g.fillStyle = "rgba(150,180,170,0.08)";
				double[][] land = {
					new double[] { 120, 140, 300, 90, 420, 160, 380, 300, 250, 330, 160, 260 },
					new double[] { 560, 120, 760, 100, 820, 210, 700, 260, 600, 230 },
					new double[] { 900, 160, 1150, 130, 1200, 300, 1040, 380, 930, 300 },
					new double[] { 300, 430, 460, 420, 520, 560, 420, 660, 320, 600 },
					new double[] { 760, 430, 900, 470, 880, 620, 740, 640, 700, 520 },
					new double[] { 1020, 500, 1160, 520, 1180, 640, 1060, 650 } };
				foreach (var pl in land) { g.beginPath(); g.moveTo(pl[0], pl[1]); for (int i = 2; i < pl.Length; i += 2) g.lineTo(pl[i], pl[i + 1]); g.closePath(); g.fill(); }
			}
			else
			{
				g.fillStyle = "rgba(3,4,12,0.82)"; g.fillRect(0, 0, W, H);
				var gr = g.createRadialGradient(L18CX + 260, L18CY - 120, 20, L18CX + 260, L18CY - 120, 520); gr.addColorStop(0, "rgba(40,70,140,0.22)"); gr.addColorStop(1, "rgba(40,70,140,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				for (int i = 0; i < 90; i++) { double sx = L18Mod(i * 389 - bt * (2 + i % 3), W), sy = (i * 157) % H; g.fillStyle = $"rgba(220,228,255,{str(0.25 + 0.3 * ((i * 7) % 5) / 4)})"; g.fillRect(sx, sy, i % 4 == 0 ? 3 : 2, i % 4 == 0 ? 3 : 2); }
				// the world, small, behind everything; it cracks in the last 4 beats before brk
				if (bt < brk)
				{
					double r = 46, cr = L18k(bt, brk - 4, brk);
					var eg = g.createRadialGradient(L18CX - 14, L18CY - 14, 6, L18CX, L18CY, r); eg.addColorStop(0, "rgba(90,150,230,0.95)"); eg.addColorStop(1, "rgba(20,50,110,0.95)"); g.fillStyle = eg; circle(L18CX, L18CY, r);
					g.fillStyle = "rgba(70,150,110,0.85)"; g.beginPath(); g.ellipse(L18CX - 12, L18CY - 8, 16, 11, 0.4, 0, TAU); g.fill(); g.beginPath(); g.ellipse(L18CX + 16, L18CY + 14, 12, 8, -0.3, 0, TAU); g.fill();
					if (cr > 0)
					{
						g.strokeStyle = $"rgba(240,240,255,{str(0.4 + 0.5 * cr)})"; g.lineWidth = 2; g.beginPath();
						for (int i = 0; i < 5; i++) { double q = i * TAU / 5 + 0.3; g.moveTo(L18CX, L18CY); g.lineTo(L18CX + JS.cos(q) * r * cr * 0.6, L18CY + JS.sin(q) * r * cr * 0.6); g.lineTo(L18CX + JS.cos(q + 0.3) * r * cr, L18CY + JS.sin(q + 0.3) * r * cr); }
						g.stroke();
					}
				}
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- part 1: the world ----------
	// the meridians: n lines of longitude on a globe (R px) round the centre, turned by sp (radians). Line i is at longitude i TAU / n, on the front
	// while cos(lon + spin) > 0, at x = CX + R sin(lon + spin) sqrt(1 - (dy / RY)^2): slow and close together at the sides, fast in the middle.
	// th wide in the middle (thinner at the sides). Each line has a gap: centre gy[i], half height gh. Grey from tw, pink from ta, gone at te
	public static Hz L18Merid(int n, double R, double RY, double th, double[] gy, double gh, L15Spin sp, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L18Merid" };
		double X(double u, double y) { double d = (y - L18CY) / RY; return L18CX + R * JS.sin(u) * Math.Sqrt(Math.Max(0, 1 - d * d)); }
		double Th(double u) => th * (0.4 + 0.6 * JS.cos(u));
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double s = sp.At(BT(t)), a = Math.Min(L18k(t, tw, tw + 0.5), 1 - L18k(t, te, te + 0.3)); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double u = i * TAU / n + s; if (JS.cos(u) <= 0.03) continue;
				double gc = gy[i % gy.Length], w = Th(u);
				g.strokeStyle = on ? PINK : L18G(0.5); g.lineWidth = on ? w : 2;
				for (int part = 0; part < 2; part++)
				{
					double y0 = part == 0 ? -10 : gc + gh, y1 = part == 0 ? gc - gh : H + 10; if (y1 <= y0) continue;
					g.beginPath(); g.moveTo(X(u, y0), y0); for (double y = y0 + 20; y < y1; y += 20) g.lineTo(X(u, y), y); g.lineTo(X(u, y1), y1); g.stroke();
				}
				if (on)
				{
					// the gap's lips: small bars across the line's ends
					g.fillStyle = PINK; double xa = X(u, gc - gh), xb = X(u, gc + gh);
					g.fillRect(xa - w / 2 - 5, gc - gh - 4, w + 10, 4); g.fillRect(xb - w / 2 - 5, gc + gh, w + 10, 4);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double s = sp.At(BT(t));
			for (int i = 0; i < n; i++)
			{
				double u = i * TAU / n + s; if (JS.cos(u) <= 0.03) continue;
				double gc = gy[i % gy.Length]; if (p.y - p.r + 2 > gc - gh && p.y + p.r - 2 < gc + gh) continue;
				if (Math.Abs(p.x - X(u, p.y)) < Th(u) / 2 + p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// night over the time zones: cols strips side by side; strip sc[k] is night (pink) from sa[k] to se[k] (seconds). A grey frame warns wn s before a
	// strip falls dark. Lit strips show a soft day glow (harmless)
	public static Hz L18Night(int cols, double[] sc, double[] sa, double[] se, double wn, double tw, double te)
	{
		double cw = W / cols; var h = new Hz { kind = "L18Night" }; int n = sc.Length;
		bool dark(int c, double t) { for (int k = 0; k < n; k++) if ((int)sc[k] == c && t >= sa[k] && t < se[k]) return true; return false; }
		double soon(int c, double t) { double m = 99; for (int k = 0; k < n; k++) if ((int)sc[k] == c && t < sa[k] && sa[k] - t < m) m = sa[k] - t; return m; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L18k(t, tw, tw + 0.5), 1 - L18k(t, te, te + 0.3));
			g.save(); g.globalAlpha = a;
			for (int c = 0; c < cols; c++)
			{
				double x = c * cw;
				if (dark(c, t))
				{
					g.fillStyle = PINK; g.fillRect(x, 0, cw, H);
					// the night's stars and the zone's edge, dark on the pink
					g.fillStyle = "rgba(60,0,22,0.45)"; for (int i = 0; i < 6; i++) g.fillRect(x + 12 + (i * 37) % (cw - 24), 40 + i * 113, 4, 4);
					g.fillStyle = "rgba(60,0,22,0.5)"; g.fillRect(x, 0, 2, H);
				}
				else
				{
					g.fillStyle = "rgba(255,230,170,0.05)"; g.fillRect(x, 0, cw, H);
					double s = soon(c, t);
					if (s < wn) { double k = 1 - s / wn; g.strokeStyle = L18G(0.25 + 0.6 * k); g.lineWidth = 2 + 3 * k; g.setLineDash(new double[] { 12, 8 }); g.strokeRect(x + 4, 4, cw - 8, H - 8); g.setLineDash(new double[0]); }
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw) return false;
			int c0 = (int)clamp(Math.Floor((p.x - p.r + 2) / cw), 0, cols - 1), c1 = (int)clamp(Math.Floor((p.x + p.r - 2) / cw), 0, cols - 1);
			for (int c = c0; c <= c1; c++) if (dark(c, t)) return true;
			return false;
		};
		return h;
	}

	// the polar nights: a band at the top and one at the bottom, each between lo and hi px tall, breathing with a period of per beats (the bottom one
	// opposite the top one). Grey edge from tw, pink from ta, gone at te
	public static Hz L18Polar(double lo, double hi, double per, double b0, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L18Polar" };
		double ht(double t, int sd) { double u = (BT(t) - b0) / per * TAU + (sd == 0 ? 0 : Math.PI); return lo + (hi - lo) * (0.5 - 0.5 * JS.cos(u)); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return; bool on = t >= ta;
			for (int sd = 0; sd < 2; sd++)
			{
				double hh = ht(t, sd), y0 = sd == 0 ? 0 : H - hh;
				if (on)
				{
					g.fillStyle = PINK; g.fillRect(0, y0, W, hh);
					g.strokeStyle = "rgba(60,0,22,0.45)"; g.lineWidth = 3; g.beginPath(); for (double x = 20; x < W; x += 64) { double yy = sd == 0 ? hh - 12 : H - hh + 12; g.moveTo(x, yy); g.lineTo(x + 24, yy); } g.stroke();
				}
				else L18Line(0, sd == 0 ? hh : H - hh, W, sd == 0 ? hh : H - hh, L18G(0.6), 2, true);
			}
		};
		h.hit = (p, t) => t >= ta && t < te && (p.y - p.r + 2 < ht(t, 0) || p.y + p.r - 2 > H - ht(t, 1));
		return h;
	}

	// a flight route: a curve from (x0,y0) over (qx,qy) to (x1,y1). Its dotted path shows from tw; the plane takes off at ta and flies it in fl s,
	// pulling a pink trail (w wide) that is hold s long; the tail follows the plane along the same curve
	public static Hz L18Route(double x0, double y0, double qx, double qy, double x1, double y1, double w, double tw, double ta, double fl, double hold)
	{
		var h = new Hz { kind = "L18Route" }; const int N = 48;
		var px = new double[N + 1]; var py = new double[N + 1];
		for (int i = 0; i <= N; i++) { double u = (double)i / N, v = 1 - u; px[i] = v * v * x0 + 2 * u * v * qx + u * u * x1; py[i] = v * v * y0 + 2 * u * v * qy + u * u * y1; }
		(double x, double y) P(double u) { double v = 1 - u; return (v * v * x0 + 2 * u * v * qx + u * u * x1, v * v * y0 + 2 * u * v * qy + u * u * y1); }
		double uh(double t) => clamp((t - ta) / fl, 0, 1);
		double ut(double t) => clamp((t - ta - hold) / fl, 0, 1);
		h.update = (t, dt) => { if (t > ta + fl + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta + fl)
			{
				double k = L18k(t, tw, ta); int i0 = (int)Math.Floor(uh(t) * N);
				g.strokeStyle = L18G(0.25 + 0.55 * k); g.lineWidth = 2; g.setLineDash(new double[] { 6, 9 }); g.beginPath(); g.moveTo(px[i0], py[i0]); for (int i = i0 + 1; i <= N; i++) g.lineTo(px[i], py[i]); g.stroke(); g.setLineDash(new double[0]);
			}
			if (t < ta) return;
			double a = ut(t), b = uh(t); if (b <= a) return;
			g.strokeStyle = PINK; g.lineWidth = w; g.lineCap = "round"; g.beginPath(); var s0 = P(a); g.moveTo(s0.x, s0.y);
			for (int i = (int)Math.Ceiling(a * N); i < b * N; i++) g.lineTo(px[i], py[i]);
			var s1 = P(b); g.lineTo(s1.x, s1.y); g.stroke(); g.lineCap = "butt";
			if (b < 1)
			{
				// the plane
				var q = P(Math.Min(1, b + 0.01)); double an = JS.atan2(q.y - s1.y, q.x - s1.x);
				g.save(); g.translate(s1.x, s1.y); g.rotate(an); g.fillStyle = PINK;
				g.beginPath(); g.moveTo(w * 1.1, 0); g.lineTo(-w * 0.5, -w * 0.35); g.lineTo(-w * 0.5, w * 0.35); g.closePath(); g.fill();
				g.beginPath(); g.moveTo(w * 0.3, 0); g.lineTo(-w * 0.2, -w * 1.1); g.lineTo(-w * 0.45, -w * 1.1); g.lineTo(-w * 0.15, 0); g.lineTo(-w * 0.45, w * 1.1); g.lineTo(-w * 0.2, w * 1.1); g.closePath(); g.fill();
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double a = ut(t), b = uh(t); if (b <= a) return false;
			var s0 = P(a); double lx = s0.x, ly = s0.y, lim = w / 2 + p.r - 1;
			for (int i = (int)Math.Ceiling(a * N); i <= N; i++)
			{
				double u = Math.Min((double)i / N, b); var q = P(u);
				if (L15Seg(p.x, p.y, lx, ly, q.x, q.y) < lim) return true;
				lx = q.x; ly = q.y; if (u >= b) break;
			}
			return false;
		};
		return h;
	}

	// the compass at (x,y): its needle follows the square. At the times lk it locks where the square is then, shows that line, and fires along the
	// whole line through the centre lag s later, w wide, for hold s. Grey from tw, gone at te
	public static Hz L18Compass(double x, double y, double[] lk, double lag, double w, double hold, double tw, double te)
	{
		var h = new Hz { kind = "L18Compass" }; int n = lk.Length; var la = new double[n]; for (int i = 0; i < n; i++) la[i] = double.NaN;
		double na = 0; bool na0 = false;
		h.update = (t, dt) =>
		{
			if (t > te + 0.5) { h.dead = true; return; }
			double pa = JS.atan2(player.y - y, player.x - x);
			for (int i = 0; i < n; i++) if (double.IsNaN(la[i]) && t >= lk[i]) la[i] = pa;
			if (!na0) { na = pa; na0 = true; } else na += L18Ad(pa, na) * Math.Min(1, dt * 8);
		};
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L18k(t, tw, tw + 0.6), 1 - L18k(t, te, te + 0.3)); g.save(); g.globalAlpha = a;
			// the body
			g.strokeStyle = "rgba(190,200,230,0.45)"; g.lineWidth = 3; g.beginPath(); g.arc(x, y, 74, 0, TAU); g.stroke();
			g.strokeStyle = "rgba(190,200,230,0.25)"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, 62, 0, TAU); g.stroke();
			g.beginPath(); for (int i = 0; i < 16; i++) { double q = i * TAU / 16, r0 = i % 4 == 0 ? 50 : 56; g.moveTo(x + JS.cos(q) * r0, y + JS.sin(q) * r0); g.lineTo(x + JS.cos(q) * 62, y + JS.sin(q) * 62); } g.stroke();
			// the lines that are locked, and the beams
			int live = -1; bool fire = false;
			for (int i = 0; i < n; i++)
			{
				if (double.IsNaN(la[i])) continue; double f = lk[i] + lag;
				if (t < f) { double k = L18k(t, lk[i], f); L18Through(x, y, la[i], $"rgba(235,235,250,{str(0.3 + 0.6 * k)})", 2 + 2 * k, true); live = i; }
				else if (t < f + hold) { L18Through(x, y, la[i], PINK, w, false); L18Through(x, y, la[i], "rgba(255,255,255,0.5)", w * 0.25, false); live = i; fire = true; }
			}
			// the needle: on its lock when one is waiting, else on the square (pink while a beam burns, so nothing on the beam is dark)
			double nd = live >= 0 ? la[live] : na;
			g.save(); g.translate(x, y); g.rotate(nd);
			g.fillStyle = fire ? "rgba(255,150,190,0.95)" : "rgba(235,235,250,0.9)"; g.beginPath(); g.moveTo(52, 0); g.lineTo(0, -9); g.lineTo(-52, 0); g.lineTo(0, 9); g.closePath(); g.fill();
			g.fillStyle = fire ? PINK : "rgba(20,24,40,0.9)"; circle(0, 0, 6); g.restore();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int i = 0; i < n; i++)
			{
				double f = lk[i] + lag; if (t < f || t >= f + hold || double.IsNaN(la[i])) continue;
				if (L18LineDist(p.x, p.y, x, y, la[i]) < w / 2 + p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// the second needle: it turns on its own (angle a0 + wb per beat from b0) round (x,y) and fires along its line at the beats fb, w wide for hold s.
	// Its thin line always shows; it brightens the last pw beats before a shot
	public static Hz L18Needle(double x, double y, double a0, double wb, double b0, double[] fb, double pw, double w, double hold, double tw, double te)
	{
		var h = new Hz { kind = "L18Needle" };
		double A(double t) => a0 + wb * (BT(t) - b0);
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return; double a = A(t), bt = BT(t); bool fire = false; double near = 99;
			foreach (var f in fb) { if (bt >= f && t < B(f) + hold) fire = true; if (f > bt && f - bt < near) near = f - bt; }
			if (fire) { L18Through(x, y, a, PINK, w, false); L18Through(x, y, a, "rgba(255,255,255,0.45)", w * 0.25, false); }
			else { double k = near < pw ? 1 - near / pw : 0; L18Through(x, y, a, L18G(0.2 + 0.6 * k), 2 + 2 * k, true); }
			g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = "rgba(160,200,235,0.85)"; g.fillRect(-40, -3, 80, 6); g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); bool fire = false; foreach (var f in fb) if (bt >= f && t < B(f) + hold) { fire = true; break; }
			return fire && t < te && L18LineDist(p.x, p.y, x, y, A(t)) < w / 2 + p.r - 1;
		};
		return h;
	}

	// ---------- part 2: leaving it ----------
	// the map folds over from side sd (0 right, 1 left, 2 bottom, 3 top) to depth d px: the fold runs in over fi s from ta, stays to te. The folded part
	// is pink, but the cities on it are holes (centres hx hy, radius hr). Grey: its edge and the holes from tw
	public static Hz L18Fold(int sd, double d, double[] hx, double[] hy, double hr, double tw, double ta, double fi, double te)
	{
		var h = new Hz { kind = "L18Fold" };
		double dep(double t) => d * L18Ease(L18k(t, ta, ta + fi));
		bool inReg(double x, double y, double r, double dd) => sd == 0 ? x + r > W - dd : sd == 1 ? x - r < dd : sd == 2 ? y + r > H - dd : y - r < dd;
		void reg(double dd) { if (sd == 0) g.rect(W - dd, 0, dd, H); else if (sd == 1) g.rect(0, 0, dd, H); else if (sd == 2) g.rect(0, H - dd, W, dd); else g.rect(0, 0, W, dd); }
		void edge(double dd, string col, double lw, bool dash) { if (sd == 0) L18Line(W - dd, 0, W - dd, H, col, lw, dash); else if (sd == 1) L18Line(dd, 0, dd, H, col, lw, dash); else if (sd == 2) L18Line(0, H - dd, W, H - dd, col, lw, dash); else L18Line(0, dd, W, dd, col, lw, dash); }
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta)
			{
				double k = L18k(t, tw, ta); edge(d, L18G(0.3 + 0.6 * k), 2 + 2 * k, true);
				g.strokeStyle = L18G(0.3 + 0.5 * k); g.lineWidth = 2; for (int i = 0; i < hx.Length; i++) { g.beginPath(); g.arc(hx[i], hy[i], hr, 0, TAU); g.stroke(); }
				return;
			}
			double dd = dep(t);
			g.fillStyle = PINK; g.beginPath(); reg(dd); for (int i = 0; i < hx.Length; i++) { if (!inReg(hx[i], hy[i], -hr, dd)) continue; g.moveTo(hx[i] + hr, hy[i]); g.arc(hx[i], hy[i], hr, 0, TAU); } g.fill("evenodd");
			// creases of the folded paper
			g.strokeStyle = "rgba(70,0,25,0.35)"; g.lineWidth = 2; g.beginPath();
			for (int i = 1; i < 4; i++) { double v = dd * i / 4; if (sd == 0) { g.moveTo(W - v, 0); g.lineTo(W - v, H); } else if (sd == 1) { g.moveTo(v, 0); g.lineTo(v, H); } else if (sd == 2) { g.moveTo(0, H - v); g.lineTo(W, H - v); } else { g.moveTo(0, v); g.lineTo(W, v); } }
			g.stroke();
			edge(dd, "rgba(255,220,232,0.9)", 3, false);
			g.strokeStyle = "rgba(255,220,232,0.85)"; g.lineWidth = 3; for (int i = 0; i < hx.Length; i++) { if (!inReg(hx[i], hy[i], -hr, dd)) continue; g.beginPath(); g.arc(hx[i], hy[i], hr, 0, TAU); g.stroke(); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double dd = dep(t); if (!inReg(p.x, p.y, p.r - 2, dd)) return false;
			for (int i = 0; i < hx.Length; i++) if (hypot(p.x - hx[i], p.y - hy[i]) < hr - p.r + 2) return false;
			return true;
		};
		return h;
	}

	// the map tears into rows strips. Strip r slides at vs[r] px a second; on it a pink tear pw wide every sp px (the first at x0[r] when the strips
	// start, ta). The seams between strips (seam j under strip j) tear open (a pink zigzag across the screen, sw thick) from the beats sb[k] (seam ss[k])
	// for sd beats; a grey zigzag warns wn s before. Grey tears from tw, gone at te
	public static Hz L18Tear(int rows, double[] vs, double[] x0, double pw, double sp, double sw, double[] sb, double[] ss, double sdur, double wn, double tw, double ta, double te)
	{
		double sh = H / rows; var h = new Hz { kind = "L18Tear" };
		double off(int r, double t) => x0[r] + vs[r] * (t - ta);
		// the first tear's left edge at or before x on strip r
		bool inTear(int r, double x0_, double x1_, double t)
		{
			double o = off(r, t); double k = Math.Floor((x0_ - o) / sp);
			for (double j = k; o + j * sp < x1_; j++) { double a = o + j * sp; if (a < x1_ && a + pw > x0_) return true; }
			return false;
		}
		bool seamOn(int j, double t) { for (int k = 0; k < sb.Length; k++) if ((int)ss[k] == j && t >= B(sb[k]) && t < B(sb[k] + sdur)) return true; return false; }
		double seamSoon(int j, double t) { double m = 99; for (int k = 0; k < sb.Length; k++) if ((int)ss[k] == j) { double d = B(sb[k]) - t; if (d > 0 && d < m) m = d; } return m; }
		void zig(double y, double amp, double ph) { g.beginPath(); g.moveTo(0, y); for (double x = 0; x <= W; x += 16) g.lineTo(x, y + ((((int)Math.Round((x + ph) / 16)) % 2 == 0) ? -amp : amp)); g.stroke(); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return; bool on = t >= ta; double a = L18k(t, tw, tw + 0.5);
			g.save(); g.globalAlpha = a;
			// the strips' edges (harmless)
			g.strokeStyle = "rgba(160,190,200,0.22)"; g.lineWidth = 1; g.beginPath(); for (int r = 1; r < rows; r++) { g.moveTo(0, r * sh); g.lineTo(W, r * sh); } g.stroke();
			for (int r = 0; r < rows; r++)
			{
				double o = off(r, t), y0 = r * sh + 4, y1 = (r + 1) * sh - 4;
				for (double j = Math.Floor((-pw - o) / sp); o + j * sp < W; j++)
				{
					double xa = o + j * sp; if (xa + pw < 0) continue;
					if (!on) { g.strokeStyle = L18G(0.55); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.strokeRect(xa, y0, pw, y1 - y0); g.setLineDash(new double[0]); continue; }
					g.fillStyle = PINK; g.beginPath(); g.moveTo(xa, y0);
					for (double yy = y0; yy <= y1; yy += 12) g.lineTo(xa - (((int)((yy - y0) / 12)) % 2 == 0 ? 0 : 4), yy);
					g.lineTo(xa + pw, y1); for (double yy = y1; yy >= y0; yy -= 12) g.lineTo(xa + pw + (((int)((y1 - yy) / 12)) % 2 == 0 ? 0 : 4), yy);
					g.closePath(); g.fill();
					g.strokeStyle = "rgba(70,0,25,0.4)"; g.lineWidth = 2; g.beginPath(); g.moveTo(xa + pw * 0.3, y0 + 8); g.lineTo(xa + pw * 0.55, (y0 + y1) / 2); g.lineTo(xa + pw * 0.35, y1 - 8); g.stroke();
				}
			}
			for (int j = 1; j < rows; j++)
			{
				double y = j * sh;
				if (seamOn(j, t)) { g.fillStyle = PINK; g.fillRect(0, y - sw / 2 - 5, W, sw + 10); g.strokeStyle = "rgba(90,0,30,0.45)"; g.lineWidth = 2; zig(y, 5, 0); }
				else { double s = seamSoon(j, t); if (s < wn) { double k = 1 - s / wn; g.strokeStyle = L18G(0.3 + 0.6 * k); g.lineWidth = 2 + 2 * k; zig(y, 5, 0); } }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			for (int j = 1; j < rows; j++) if (seamOn(j, t) && Math.Abs(p.y - j * sh) < sw / 2 + 5 + p.r - 1) return true;
			int r0 = (int)clamp(Math.Floor((p.y - p.r + 2 - 4) / sh), 0, rows - 1), r1 = (int)clamp(Math.Floor((p.y + p.r - 2 + 4) / sh), 0, rows - 1);
			for (int r = r0; r <= r1; r++)
			{
				double y0 = r * sh + 4, y1 = (r + 1) * sh - 4; if (p.y + p.r - 2 < y0 || p.y - p.r + 2 > y1) continue;
				if (inTear(r, p.x - p.r + 2, p.x + p.r - 2, t)) return true;
			}
			return false;
		};
		return h;
	}

	// the hourglass: the arena is the glass, everything outside it pink (half width NK at the neck, HW at the ends). Sand runs from the top pile to the
	// bottom pile (pile heights between hmin and hmax) in a pink stream through the neck while it flows (fs..fe beats each cycle); where it lands, grains
	// splash up (splash beats spb, angles spa degrees, speed spv) and drops fall from the top pile (drip beats db at x dx). At the beats fb the glass turns
	// over (half a turn in one beat): stand in the neck. Grey from tw, pink from ta, gone at te
	public class L18GlassO
	{
		public double NK, HW, ex, hmin, hmax, sw, spv, gr, gw;
		public double[] fs, fe, fb, spb, spa, db, dx;
	}
	public static Hz L18Glass(L18GlassO o, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L18Glass" };
		double hw(double y) => o.NK + (o.HW - o.NK) * JS.pow(Math.Min(1, Math.Abs(y) / 360), o.ex);
		// the turn (radians) at beat bt
		// (only while it turns: after a turn the glass is the same again, and the full pile is the top one)
		double th(double bt) { foreach (var f in o.fb) if (bt > f && bt < f + 1) return Math.PI * L18Ease(bt - f); return 0; }
		// the flow cycle at beat bt: progress 0..1 of the sand from the top to the bottom, and whether the stream runs
		double prog(double bt, out bool run)
		{
			run = false; double u = 0;
			for (int i = 0; i < o.fs.Length; i++) { if (bt < o.fs[i]) break; u = clamp((bt - o.fs[i]) / (o.fe[i] - o.fs[i]), 0, 1); run = bt < o.fe[i]; }
			return u;
		}
		double topH(double u) => o.hmax - (o.hmax - o.hmin) * u;
		double botH(double u) => o.hmin + (o.hmax - o.hmin) * u;
		// a grain splashing (beat sb, angle an, in the glass's own frame) at time t: where it is, or false once it is gone
		bool splash(double sb, double an, double t, out double gx, out double gy)
		{
			double t0 = B(sb), u = t - t0; double pu = prog(sb, out _); double y0 = 360 - botH(pu);
			gx = JS.cos(an) * o.spv * u; gy = y0 + JS.sin(an) * o.spv * u + 0.5 * o.gr * u * u;
			if (u < 0 || u > 1.6) return false;
			double bu = prog(BT(t), out _); return gy < 360 - botH(bu) && Math.Abs(gx) < hw(gy);
		}
		bool drip(double sb, double x, double t, out double gx, out double gy)
		{
			double t0 = B(sb), u = t - t0; double pu = prog(sb, out _); double y0 = -360 + topH(pu);
			gx = x * (1 - clamp(u / 1.2, 0, 1) * 0.6); gy = y0 + 0.5 * o.gr * u * u;
			if (u < 0) return false;
			return gy < -6 && Math.Abs(gx) < hw(gy);
		}
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return; double bt = BT(t), a = th(bt); bool on = t >= ta; double u = prog(bt, out bool run);
			g.save(); g.translate(L18CX, L18CY); g.rotate(a);
			// the glass: outside it is pink (or a grey outline before)
			if (on)
			{
				// (in pieces that overlap a little, no hole: a filled hole left a thin dark seam while it turned)
				g.fillStyle = PINK; g.fillRect(-1000, -1000, 2000, 642); g.fillRect(-1000, 358, 2000, 642);
				for (int sd = -1; sd <= 1; sd += 2) { g.beginPath(); g.moveTo(sd * 1000, -362); for (double y = -362; y <= 362; y += 12) g.lineTo(sd * hw(y), y); g.lineTo(sd * hw(362), 362); g.lineTo(sd * 1000, 362); g.closePath(); g.fill(); }
				g.strokeStyle = "rgba(255,225,236,0.9)"; g.lineWidth = 3;
			}
			else { g.strokeStyle = L18G(0.3 + 0.5 * L18k(t, tw, ta)); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); }
			for (int sd = -1; sd <= 1; sd += 2) { g.beginPath(); for (double y = -360; y <= 360; y += 12) { if (y == -360) g.moveTo(sd * hw(y), y); else g.lineTo(sd * hw(y), y); } g.stroke(); }
			g.setLineDash(new double[0]);
			if (on)
			{
				// the glass's shine
				g.strokeStyle = "rgba(90,0,30,0.35)"; g.lineWidth = 6; for (int sd = -1; sd <= 1; sd += 2) { g.beginPath(); for (double y = -330; y <= 330; y += 15) { double x = sd * (hw(y) + 22); if (y == -330) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
				// the piles
				double ht = topH(u), hb = botH(u);
				g.fillStyle = PINK; g.beginPath(); g.moveTo(-hw(-360), -360); for (double y = -360; y <= -360 + ht; y += 8) g.lineTo(-hw(y), y); g.lineTo(-hw(-360 + ht), -360 + ht);
				g.lineTo(hw(-360 + ht), -360 + ht); for (double y = -360 + ht; y >= -360; y -= 8) g.lineTo(hw(y), y); g.lineTo(hw(-360), -360); g.closePath(); g.fill();
				g.beginPath(); g.moveTo(-hw(360), 360); for (double y = 360; y >= 360 - hb; y -= 8) g.lineTo(-hw(y), y); g.lineTo(-hw(360 - hb), 360 - hb);
				g.lineTo(hw(360 - hb), 360 - hb); for (double y = 360 - hb; y <= 360; y += 8) g.lineTo(hw(y), y); g.lineTo(hw(360), 360); g.closePath(); g.fill();
				g.fillStyle = "rgba(90,0,30,0.4)"; for (int i = 0; i < 30; i++) { double gx = ((i * 73) % 900) - 450, gy1 = 360 - ((i * 41) % Math.Max(1, (int)hb)); if (Math.Abs(gx) < hw(gy1) - 6) g.fillRect(gx, gy1, 4, 4); double gy2 = -360 + ((i * 37) % Math.Max(1, (int)ht)); if (Math.Abs(gx) < hw(gy2) - 6) g.fillRect(gx, gy2, 4, 4); }
				// the stream
				if (run) { g.fillStyle = PINK; g.fillRect(-o.sw / 2, -360 + ht, o.sw, 720 - ht - hb); g.fillStyle = "rgba(255,255,255,0.35)"; g.fillRect(-o.sw * 0.12, -360 + ht, o.sw * 0.24, 720 - ht - hb); }
				// grains and drops
				g.fillStyle = PINK;
				foreach (var sb in o.spb) foreach (var an in o.spa) if (splash(sb, an * Math.PI / 180, t, out double gx, out double gy)) circle(gx, gy, o.gw);
				for (int i = 0; i < o.db.Length; i++) if (drip(o.db[i], o.dx[i], t, out double gx, out double gy)) { circle(gx, gy, o.gw); g.fillRect(gx - o.gw * 0.4, gy - o.gw * 2.2, o.gw * 0.8, o.gw * 2.2); }
			}
			g.restore();
			// the neck to stand in while it turns
			foreach (var f in o.fb)
			{
				if (bt > f - 2 && bt < f + 1.2) { double k = bt < f ? L18k(bt, f - 2, f) : 1; g.strokeStyle = $"rgba(235,240,255,{str(0.35 + 0.55 * k)})"; g.lineWidth = 2 + 2 * k; g.setLineDash(new double[] { 8, 6 }); g.beginPath(); g.arc(L18CX, L18CY, o.NK - 12, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), a = -th(bt), dx = p.x - L18CX, dy = p.y - L18CY;
			double x = dx * JS.cos(a) - dy * JS.sin(a), y = dx * JS.sin(a) + dy * JS.cos(a), rr = p.r - 2;
			if (Math.Abs(y) + rr > 360 || Math.Abs(x) + rr > hw(y)) return true;
			double u = prog(bt, out bool run), ht = topH(u), hb = botH(u);
			if (y - rr < -360 + ht || y + rr > 360 - hb) return true;
			if (run && Math.Abs(x) < o.sw / 2 + rr) return true;
			double lim = o.gw + p.r - 2;
			foreach (var sb in o.spb) { if (t < B(sb) || t > B(sb) + 1.7) continue; foreach (var an in o.spa) if (splash(sb, an * Math.PI / 180, t, out double gx, out double gy) && hypot(x - gx, y - gy) < lim) return true; }
			for (int i = 0; i < o.db.Length; i++) { if (t < B(o.db[i]) || t > B(o.db[i]) + 2) continue; if (drip(o.db[i], o.dx[i], t, out double gx, out double gy) && (hypot(x - gx, y - gy) < lim || (Math.Abs(x - gx) < o.gw * 0.4 + rr && y < gy && y > gy - o.gw * 2.2))) return true; }
			return false;
		};
		return h;
	}

	// the world closes in: farther than R from the centre is pink (an ellipse ex times wider than tall), R going from r0 (ta) to r1 (rt) and
	// staying there to te. Grey edge (where it ends) from tw. Part 3 keeps an edge like it: space has an end
	public static Hz L18Close(double r0, double r1, double ex, double tw, double ta, double rt, double te)
	{
		var h = new Hz { kind = "L18Close" };
		double R(double t) => r0 + (r1 - r0) * L18Ease(L18k(t, ta, rt));
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return; double r = R(t);
			if (t < ta) { g.strokeStyle = L18G(0.3 + 0.5 * L18k(t, tw, ta)); g.lineWidth = 2; g.setLineDash(new double[] { 12, 9 }); g.beginPath(); g.ellipse(L18CX, L18CY, r1 * ex, r1, 0, 0, TAU); g.stroke(); g.setLineDash(new double[0]); return; }
			g.fillStyle = PINK; g.beginPath(); g.rect(-50, -50, W + 100, H + 100); g.moveTo(L18CX + r * ex, L18CY); g.ellipse(L18CX, L18CY, r * ex, r, 0, 0, TAU); g.fill("evenodd");
			g.strokeStyle = "rgba(255,225,236,0.9)"; g.lineWidth = 3; g.beginPath(); g.ellipse(L18CX, L18CY, r * ex, r, 0, 0, TAU); g.stroke();
			g.strokeStyle = "rgba(90,0,30,0.35)"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 24; i++) { double q = i * TAU / 24 + BT(t) * 0.02; g.moveTo(L18CX + JS.cos(q) * (r + 10) * ex, L18CY + JS.sin(q) * (r + 10)); g.lineTo(L18CX + JS.cos(q) * (r + 40) * ex, L18CY + JS.sin(q) * (r + 40)); } g.stroke();
		};
		h.hit = (p, t) => t >= ta && t < te && hypot((p.x - L18CX) / ex, p.y - L18CY) > R(t) - p.r + 2;
		return h;
	}

	// a ring spreading from the centre at ta, v px a second, th thick, with a gap at ga (radians) gw wide. A grey mark of it shows from tw
	public static Hz L18Ripple(double ga, double gw, double v, double th, double tw, double ta)
	{
		var h = new Hz { kind = "L18Ripple" };
		h.update = (t, dt) => { if (t > ta + 820 / v) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L18k(t, tw, ta); g.strokeStyle = L18G(0.3 + 0.6 * k); g.lineWidth = 3; g.beginPath(); g.arc(L18CX, L18CY, 30 + 30 * k, ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
				L18Line(L18CX + JS.cos(ga) * 70, L18CY + JS.sin(ga) * 70, L18CX + JS.cos(ga) * 760, L18CY + JS.sin(ga) * 760, L18G(0.2 + 0.4 * k), 2, true);
				return;
			}
			double r = (t - ta) * v; if (r > 780) return;
			g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.arc(L18CX, L18CY, r, ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double r = (t - ta) * v, d = hypot(p.x - L18CX, p.y - L18CY);
			if (Math.Abs(d - r) >= th / 2 + p.r - 1) return false;
			double an = JS.atan2(p.y - L18CY, p.x - L18CX), lim = gw / 2 - Math.Asin(Math.Min(1, (p.r - 2) / Math.Max(1, d)));
			return Math.Abs(L18Ad(an, ga)) > lim;
		};
		return h;
	}

	// ---------- part 3: beyond it ----------
	// two bodies orbit the centre: the inner one at ri on the turning si, the outer one at ro on so (radians), each rb big. Round each a ring of np
	// pebbles (radius pr, turning pwi / pwo radians a beat, pebbles pb big, ring out from pon to poff beats). They line up with the centre (an eclipse)
	// at the beats eb: the line through the centre at the outer one's angle shows from eb - ew and burns bw s, w wide.
	// Grey from tw, pink from ta, gone at te; after te both drift into the centre and meet (harmless)
	public class L18OrbitO
	{
		public double ri, ro, rb, pr, pwi, pwo, pb, pon, poff, ew, bw, w, ex = 1; public int np;
		public double[] eb;
	}
	public static Hz L18Orbit(L18OrbitO o, L15Spin si, L15Spin so, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L18Orbit" };
		(double x, double y) body(int k, double bt) { double a = k == 0 ? si.At(bt) : so.At(bt), r = k == 0 ? o.ri : o.ro; return (L18CX + JS.cos(a) * r * o.ex, L18CY + JS.sin(a) * r); }
		// the eclipse line: through the centre and the outer body
		double eAng(double b) { double a = so.At(b); return JS.atan2(JS.sin(a), JS.cos(a) * o.ex); }
		double pebR(double bt) => o.pr * (1 + 0.12 * JS.exp(-L18Mod(bt, 1) * 5));
		bool pebOn(double bt) => bt >= o.pon && bt < o.poff;
		h.update = (t, dt) => { if (t > te + 2.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), tb = BT(te); bool on = t >= ta && t < te;
			if (t >= te)
			{
				// they meet in the middle
				double k = L18Ease(L18k(bt, tb, tb + 1.5)), al = 1 - L18k(bt, tb + 1.5, tb + 3);
				for (int q = 0; q < 2; q++) { var c = body(q, tb); double x = c.x + (L18CX - c.x) * k, y = c.y + (L18CY - c.y) * k; g.fillStyle = $"rgba(240,240,255,{str(0.8 * al)})"; circle(x, y, o.rb * (1 - 0.4 * k)); }
				return;
			}
			double a = L18k(t, tw, tw + 0.6);
			g.save(); g.globalAlpha = a;
			// the orbits (harmless)
			g.strokeStyle = "rgba(150,170,230,0.18)"; g.lineWidth = 2; g.beginPath(); g.ellipse(L18CX, L18CY, o.ri * o.ex, o.ri, 0, 0, TAU); g.stroke(); g.beginPath(); g.ellipse(L18CX, L18CY, o.ro * o.ex, o.ro, 0, 0, TAU); g.stroke();
			// the eclipses
			foreach (var e in o.eb)
			{
				if (bt < e - o.ew || t >= B(e) + o.bw) continue; double an = eAng(e);
				if (bt < e) { double k = L18k(bt, e - o.ew, e); L18Through(L18CX, L18CY, an, $"rgba(235,235,250,{str(0.25 + 0.65 * k)})", 2 + 3 * k, true); }
				else if (on) { L18Through(L18CX, L18CY, an, PINK, o.w, false); L18Through(L18CX, L18CY, an, "rgba(255,255,255,0.5)", o.w * 0.22, false); }
			}
			for (int q = 0; q < 2; q++)
			{
				var c = body(q, bt);
				if (pebOn(bt))
				{
					double pr = pebR(bt), sp = (q == 0 ? o.pwi : o.pwo) * bt;
					if (!on) { g.strokeStyle = L18G(0.4); g.lineWidth = 2; g.setLineDash(new double[] { 5, 7 }); g.beginPath(); g.arc(c.x, c.y, pr, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
					for (int i = 0; i < o.np; i++) { double an = sp + i * TAU / o.np; g.fillStyle = on ? PINK : L18G(0.5); circle(c.x + JS.cos(an) * pr, c.y + JS.sin(an) * pr, o.pb); }
				}
				g.fillStyle = on ? PINK : L18G(0.5); circle(c.x, c.y, o.rb);
				g.fillStyle = q == 0 ? "rgba(255,240,245,0.55)" : "rgba(80,0,28,0.45)"; circle(c.x - o.rb * 0.3, c.y - o.rb * 0.3, o.rb * 0.35);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t);
			for (int q = 0; q < 2; q++)
			{
				var c = body(q, bt); if (hypot(p.x - c.x, p.y - c.y) < o.rb + p.r - 2) return true;
				if (pebOn(bt)) { double pr = pebR(bt), sp = (q == 0 ? o.pwi : o.pwo) * bt; for (int i = 0; i < o.np; i++) { double an = sp + i * TAU / o.np; if (hypot(p.x - c.x - JS.cos(an) * pr, p.y - c.y - JS.sin(an) * pr) < o.pb + p.r - 2) return true; } }
			}
			foreach (var e in o.eb) if (bt >= e && t < B(e) + o.bw && L18LineDist(p.x, p.y, L18CX, L18CY, eAng(e)) < o.w / 2 + p.r - 1) return true;
			return false;
		};
		return h;
	}

	// the world breaks: from the centre, shards fly out along na spiral arms at the beats sb. Arm k of a burst leaves at angle a0 + k TAU / na +
	// spin * (its beat - b0), v px a second, its path bending kb radians a px. Each shard sr big. The arms' paths show faintly (grey) ahead
	public static Hz L18Shards(double[] sb, int na, double a0, double spin, double b0, double v, double kb, double sr, double tw, double te)
	{
		var h = new Hz { kind = "L18Shards" }; const double FAR = 780;
		double base_(double b) => a0 + spin * (b - b0);
		h.update = (t, dt) => { if (t > te + FAR / v + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t);
			if (t < te)
			{
				// the arms that are coming (the next burst's paths)
				double nb = double.NaN; foreach (var b in sb) if (b >= bt) { nb = b; break; }
				if (!double.IsNaN(nb) && nb - bt < 1.5)
				{
					double k = 1 - (nb - bt) / 1.5; g.strokeStyle = L18G(0.15 + 0.35 * k); g.lineWidth = 2; g.setLineDash(new double[] { 5, 9 });
					for (int a = 0; a < na; a++) { g.beginPath(); for (double r = 50; r < FAR; r += 30) { double an = base_(nb) + a * TAU / na + kb * r; double x = L18CX + JS.cos(an) * r, y = L18CY + JS.sin(an) * r; if (r == 50) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
					g.setLineDash(new double[0]);
				}
			}
			g.fillStyle = PINK;
			foreach (var b in sb)
			{
				double t0 = B(b), u = t - t0; if (u < 0) break; double r = v * u; if (r > FAR) continue;
				for (int a = 0; a < na; a++)
				{
					double an = base_(b) + a * TAU / na + kb * r, x = L18CX + JS.cos(an) * r, y = L18CY + JS.sin(an) * r;
					g.save(); g.translate(x, y); g.rotate(an + u * 4); circle(0, 0, sr * 0.9); g.beginPath(); g.moveTo(sr * 1.3, 0); g.lineTo(-sr * 0.8, -sr); g.lineTo(-sr * 0.5, sr * 0.9); g.closePath(); g.fill(); g.restore();
				}
			}
		};
		h.hit = (p, t) =>
		{
			foreach (var b in sb)
			{
				double t0 = B(b), u = t - t0; if (u < 0) break; double r = v * u; if (r > FAR) continue;
				double d = hypot(p.x - L18CX, p.y - L18CY); if (Math.Abs(d - r) > sr + p.r) continue;
				for (int a = 0; a < na; a++)
				{
					double an = base_(b) + a * TAU / na + kb * r;
					if (hypot(p.x - L18CX - JS.cos(an) * r, p.y - L18CY - JS.sin(an) * r) < sr * 0.9 + p.r - 2) return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L18.json) ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them. An event fires at its start minus Easy's longest warning
	static double[] L18Sec(double[] bs) => bs.Select(B).ToArray();
	static double L18wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L18ez(double v, double ez) => easy ? v * ez : v;
	static readonly int _k18fx = DodgeKind("L18fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L18fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k18stage = DodgeKind("L18stage", e => () => hazards.Add(L18Stage(e.i("part"), B(e.num("fr", e.b)), B(e.num("to")), e.num("brk", 1e9))));
	static readonly int _k18merid = DodgeKind("L18merid", e => { double wn = L18wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.75 : 1;
		var sp = new L15Spin(e.num("a0") * Math.PI / 180, e.num("s0"), e.num("to") + 1, e.nums("sb"), e.nums("sw").Select(v => v * sl).ToArray());
		hazards.Add(L18Merid(e.i("n"), e.num("R"), e.num("RY"), L18ez(e.num("th"), 0.8), e.nums("gy"), L18ez(e.num("gh"), 1.3), sp, B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k18night = DodgeKind("L18night", e => { double wn = L18wn(e); return () =>
	{
		string p = easy ? "e" : "";
		hazards.Add(L18Night(e.i("cols"), e.nums(p + "sc"), L18Sec(e.nums(p + "sa")), L18Sec(e.nums(p + "se")), wn * SPB, B(e.num("tw")), B(e.num("to"))));
	}; });
	static readonly int _k18polar = DodgeKind("L18polar", e => { double wn = L18wn(e); return () =>
		hazards.Add(L18Polar(e.num("lo"), L18ez(e.num("hi"), 0.7), e.num("per"), e.num("hit"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")))); });
	static readonly int _k18route = DodgeKind("L18route", e => { double wn = L18wn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		hazards.Add(L18Route(e.num("x0"), e.num("y0"), e.num("qx"), e.num("qy"), e.num("x1"), e.num("y1"), L18ez(e.num("w"), 0.8), B(e.num("hit") - wn), B(e.num("hit")), e.num("fl") * SPB, L18ez(e.num("hold"), 0.75) * SPB));
	}; });
	static readonly int _k18compass = DodgeKind("L18compass", e => { double wn = L18wn(e); return () =>
		hazards.Add(L18Compass(L18CX, L18CY, e.nums("lk").Select(b => B(b) - (easy ? 0.2 : 0)).ToArray(), easy ? 0.65 : e.num("lag"), L18ez(e.num("w"), 0.75), e.num("hold") * SPB, B(e.num("hit") - wn), B(e.num("to")))); });
	static readonly int _k18needle = DodgeKind("L18needle", e => { double wn = L18wn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		hazards.Add(L18Needle(L18CX, L18CY, e.num("a0") * Math.PI / 180, e.num("wb") * Math.PI / 180, e.num("hit"), e.nums("fb"), e.num("pw", 0.8), L18ez(e.num("w"), 0.75), e.num("hold") * SPB, B(e.num("hit") - wn), B(e.num("to"))));
	}; });
	static readonly int _k18fold = DodgeKind("L18fold", e => { double wn = L18wn(e); return () =>
		hazards.Add(L18Fold(e.i("sd"), e.num("d"), e.nums("hx"), e.nums("hy"), L18ez(e.num("hr"), 1.25), B(e.num("hit") - wn), B(e.num("hit")), e.num("fi") * SPB, B(e.num("to")))); });
	static readonly int _k18tear = DodgeKind("L18tear", e => { double wn = L18wn(e); return () =>
	{
		double sl = easy ? 0.75 : 1; var sb = e.nums("sb"); var ss = e.nums("ss");
		if (easy) { var keep = Enumerable.Range(0, sb.Length).Where(i => i % 2 == 0).ToArray(); sb = keep.Select(i => sb[i]).ToArray(); ss = keep.Select(i => ss[i]).ToArray(); }
		hazards.Add(L18Tear(e.i("rows"), e.nums("vs").Select(v => v * sl).ToArray(), e.nums("x0"), L18ez(e.num("pw"), 0.8), e.num("sp"), e.num("sw"), sb, ss, e.num("sd"), e.num("swn") * SPB * (easy ? 1.3 : 1), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k18glass = DodgeKind("L18glass", e => { double wn = L18wn(e); return () =>
	{
		var spa = e.nums("spa"); var db = e.nums("db"); var dx = e.nums("dx");
		if (easy) { spa = spa.Where((v, i) => i % 2 == 0).ToArray(); db = db.Where((v, i) => i % 2 == 0).ToArray(); dx = dx.Where((v, i) => i % 2 == 0).ToArray(); }
		var o = new L18GlassO { NK = L18ez(e.num("NK"), 1.15), HW = e.num("HW"), ex = e.num("ex"), hmin = e.num("hmin"), hmax = L18ez(e.num("hmax"), 0.8), sw = L18ez(e.num("sw"), 0.8), spv = e.num("spv"), gr = e.num("gr"), gw = L18ez(e.num("gw"), 0.85),
			fs = e.nums("fs"), fe = e.nums("fe"), fb = e.nums("fb"), spb = e.nums("spb"), spa = spa, db = db, dx = dx };
		hazards.Add(L18Glass(o, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k18close = DodgeKind("L18close", e => { double wn = L18wn(e); return () =>
	{
		double r1 = e.num("r0") == e.num("r1") ? e.num("r1") : L18ez(e.num("r1"), 1.2);
		hazards.Add(L18Close(e.num("r0"), r1, e.num("ex", 1), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("rt", e.num("to"))), B(e.num("to"))));
	}; });
	static readonly int _k18ripple = DodgeKind("L18ripple", e => { double wn = L18wn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		hazards.Add(L18Ripple(e.num("ga") * Math.PI / 180, L18ez(e.num("gw"), 1.35) * Math.PI / 180, L18ez(e.num("v"), 0.8), e.num("th"), B(e.num("hit") - wn), B(e.num("hit"))));
	}; });
	static readonly int _k18orbit = DodgeKind("L18orbit", e => { double wn = L18wn(e); return () =>
	{
		double hit = e.num("hit"), to = e.num("to");
		var si = new L15Spin(e.num("ai") * Math.PI / 180, e.num("s0"), to + 4, e.nums("sbi"), e.nums("swi"));
		var so = new L15Spin(e.num("ao") * Math.PI / 180, e.num("s0"), to + 4, e.nums("sbo"), e.nums("swo"));
		var o = new L18OrbitO { ri = e.num("ri"), ro = e.num("ro"), rb = L18ez(e.num("rb"), 0.85), pr = e.num("pr"), pwi = e.num("pwi"), pwo = e.num("pwo"), pb = L18ez(e.num("pb"), 0.85), pon = e.num("pon"), poff = e.num("poff"),
			np = easy ? 6 : e.i("np"), ex = e.num("ex", 1), ew = e.num("ew") * (easy ? 1.3 : 1), bw = e.num("bw") * SPB, w = L18ez(e.num("w"), 0.7), eb = e.nums("eb") };
		hazards.Add(L18Orbit(o, si, so, B(hit - wn), B(hit), B(to)));
	}; });
	static readonly int _k18shards = DodgeKind("L18shards", e => () =>
	{
		var sb = e.nums("sb"); if (easy) sb = sb.Where((v, i) => i % 3 != 2).ToArray();
		hazards.Add(L18Shards(sb, easy ? 4 : e.i("na"), e.num("a0") * Math.PI / 180, e.num("spin") * Math.PI / 180, e.num("b0"), L18ez(e.num("v"), 0.85), e.num("kb"), L18ez(e.num("sr"), 0.85), B(e.num("tw")), B(e.num("to"))));
	});
}
