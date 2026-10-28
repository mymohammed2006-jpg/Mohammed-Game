using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F11 "أجمل غرامي" (Abdullah Al Farwan), miracle's level, the new design (v52; the first one stays in F11.cs for the old replays):
// a serenade for the most beautiful love, played on the oud and written in a letter.
// part 1 the strings (the oud's strings across the screen: a plucked string shakes and hurts, its still points (the nodes) are the
//   only way through it; the strum sweeps a lane; the oud turns; the chord: the pick runs over all the strings),
// part 2 the letter (the pen writes giant letters, then the word in mirror writing, then round in a ring that turns: fresh ink hurts,
//   a letter fills with ink when it is done; the letters' dots fall as diamonds),
// part 3 the melody (the strings again, through the middle like the rays of the oud's rose, turning; at the end all of them at once).
// The oud, the paper, the rose, dry ink and strings at rest are gold or wood: only a look. Only pink hurts.
// Every hazard is a pure function of time once it exists. The data is data/dodge_F11.json (made by tools/friend11/gen2.py).
public static partial class G
{
	static double F11bK(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double F11bE(double u) => u * u * (3 - 2 * u);
	static string F11bP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string F11bL(double a) => $"rgba(255,120,165,{str(clamp(a, 0, 1))})";   // the light pink of a string's shaking line
	static string F11bG(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string F11bY(double a) => $"rgba(232,186,104,{str(clamp(a, 0, 1))})";   // gold: only a look
	static string F11bW(double a) => $"rgba(92,52,30,{str(clamp(a, 0, 1))})";   // the oud's wood
	// a value that moves between keyframes (kb seconds, kv values), eased; before the first and after the last it holds
	static double F11bLerp(double[] kb, double[] kv, double t, double v0 = 0)
	{
		int n = kb.Length; if (n == 0) return v0; if (t <= kb[0]) return kv[0]; if (t >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && t >= kb[i + 1]) i++;
		double u = (t - kb[i]) / Math.Max(1e-6, kb[i + 1] - kb[i]); return kv[i] + (kv[i + 1] - kv[i]) * F11bE(clamp(u, 0, 1));
	}
	static double F11bSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void F11bDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double F11bH(int i) { unchecked { uint x = (uint)(i * 2654435761u) ^ 0x2b7e1516u; x ^= x >> 15; x *= 0x85ebca6bu; x ^= x >> 13; return (x % 10000) / 10000.0; } }

	// ---------- the stage (harmless) ----------
	// the oud's rose (the carved sound hole): a gold star of 8 in rings
	static void F11bRose(double x, double y, double r, double rot, double a)
	{
		g.save(); g.translate(x, y); g.rotate(rot);
		g.strokeStyle = F11bY(0.55 * a); g.lineWidth = 2;
		g.beginPath(); g.arc(0, 0, r, 0, TAU); g.stroke(); g.beginPath(); g.arc(0, 0, r * 0.82, 0, TAU); g.stroke();
		for (int k = 0; k < 2; k++)
		{
			g.beginPath();
			for (int i = 0; i <= 8; i++) { double an = i * TAU / 8 + k * Math.PI / 8, rr = i % 2 == 0 ? r * 0.8 : r * 0.8; double x1 = JS.cos(an) * rr, y1 = JS.sin(an) * rr; if (i == 0) g.moveTo(x1, y1); else g.lineTo(x1, y1); }
			g.stroke();
		}
		g.beginPath();
		for (int i = 0; i <= 16; i++) { double an = i * TAU / 16, rr = i % 2 == 0 ? r * 0.78 : r * 0.36; double x1 = JS.cos(an) * rr, y1 = JS.sin(an) * rr; if (i == 0) g.moveTo(x1, y1); else g.lineTo(x1, y1); }
		g.stroke();
		g.fillStyle = F11bY(0.22 * a); g.beginPath(); g.arc(0, 0, r * 0.2, 0, TAU); g.fill();
		for (int i = 0; i < 16; i++) { double an = i * TAU / 16 + Math.PI / 16; g.fillStyle = F11bY(0.4 * a); circle(JS.cos(an) * r * 0.91, JS.sin(an) * r * 0.91, 3); }
		g.restore();
	}
	// an arabesque border on the screen's edge (part 2 and 3)
	static void F11bBorder(double t, double a)
	{
		g.strokeStyle = F11bY(0.22 * a); g.lineWidth = 2; g.strokeRect(14, 14, W - 28, H - 28); g.strokeRect(24, 24, W - 48, H - 48);
		for (int i = 0; i < 40; i++)
		{
			double u = i / 40.0; double x = 24 + u * (W - 48);
			g.beginPath(); g.moveTo(x, 19); g.lineTo(x + 8, 14); g.moveTo(x, H - 19); g.lineTo(x + 8, H - 14); g.stroke();
		}
	}
	// part 1 the oud under the lamp (it turns with the strings: ang over time), 2 the letter (dark paper with gold lines), 3 the rose
	public static Hz F11bStage(int part, double t0, double t1, double[] kb, double[] ka)
	{
		var h = new Hz { kind = "F11bStage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var gr = g.createRadialGradient(W / 2, H / 2, 60, W / 2, H / 2, 820); gr.addColorStop(0, "#2a1420"); gr.addColorStop(0.6, "#140a14"); gr.addColorStop(1, "#06030a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				double an = F11bLerp(kb, ka, t) * Math.PI / 180;
				g.save(); g.translate(W / 2, H / 2); g.rotate(an);
				// the body (a pear of wood), its ribs, the bridge and the neck to the left
				g.fillStyle = F11bW(0.55); g.beginPath(); g.ellipse(90, 0, 470, 300, 0, 0, TAU); g.fill();
				g.strokeStyle = "rgba(40,20,12,0.6)"; g.lineWidth = 2;
				for (int i = -4; i <= 4; i++) { g.beginPath(); g.ellipse(90, 0, 470, Math.Abs(i) * 64 + 8, 0, -Math.PI / 2 - 1.4, -Math.PI / 2 + 1.4 + Math.PI, false); g.stroke(); }
				g.fillStyle = "rgba(30,14,8,0.8)"; g.fillRect(-1100, -46, 760, 92);
				g.fillStyle = "rgba(30,14,8,0.85)"; g.fillRect(400, -120, 26, 240);
				g.restore();
				F11bRose(W / 2 + JS.cos(an) * 90, H / 2 + JS.sin(an) * 90, 92, an, a);
			}
			else if (part == 2)
			{
				var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#0d0a1c"); gr.addColorStop(1, "#1a0f22"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				g.strokeStyle = F11bY(0.07); g.lineWidth = 1;
				for (double y = 60; y < H; y += 48) { g.beginPath(); g.moveTo(40, y); g.lineTo(W - 40, y); g.stroke(); }
				F11bBorder(t, 1);
			}
			else
			{
				var gr = g.createRadialGradient(W / 2, H / 2, 30, W / 2, H / 2, 760); gr.addColorStop(0, "#301024"); gr.addColorStop(0.5, "#170818"); gr.addColorStop(1, "#060208"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				double an = F11bLerp(kb, ka, t) * Math.PI / 180;
				F11bRose(W / 2, H / 2, 150, an, 1); F11bRose(W / 2, H / 2, 300, -an * 0.5, 0.5);
				F11bBorder(t, 1);
			}
			g.restore();
		};
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F11bFx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F11bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the strings ----------
	// A string: through the point (px, py) + d across, at the angle a0 + the turn kb/ka (degrees), from -L to L along it.
	// At rest it is gold and harmless. Plucked (pt[i] seconds, harmonic pn[i], size pa[i]) it shakes: its band is pa * |sin(n pi u)|
	// (u = 0..1 along it), it holds for hold s, then dies out over dec s. Only where |sin| >= thr it hurts (pink); round the still points
	// (the nodes) it stays gold, that is the way through. The next pluck shows its band dashed wn s before.
	public static Hz F11bString(double px, double py, double d, double a0, double L, double[] kb, double[] ka, double[] pt, int[] pn, double[] pa,
		double thr, double hold, double dec, double wn, double t0, double t1)
	{
		var h = new Hz { kind = "F11bString" }; int np = pt.Length;
		double ang(double t) => (a0 + F11bLerp(kb, ka, t)) * Math.PI / 180;
		int cur(double t) { int k = -1; for (int i = 0; i < np; i++) if (pt[i] <= t) k = i; return k; }
		double amp(int k, double t) { if (k < 0) return 0; double s = t - pt[k]; if (s < hold) return pa[k]; return pa[k] * Math.Max(0, 1 - (s - hold) / dec); }
		void frame(double t, out double cx, out double cy, out double ux, out double uy)
		{
			double an = ang(t); ux = JS.cos(an); uy = JS.sin(an); cx = px - uy * d; cy = py + ux * d;
		}
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		const int NS = 160;
		bool ins(int n, int i) => Math.Abs(JS.sin(n * Math.PI * i / NS)) >= thr;
		// the band's outline (one side, then back on the other) for the parts where |sin| >= thr, at size A
		void band(double cx, double cy, double ux, double uy, int n, double A, double ph, bool fill, string col, bool dash)
		{
			int i = 0;
			while (i <= NS)
			{
				if (!ins(n, i)) { i++; continue; }
				int j = i; while (j < NS && ins(n, j + 1)) j++;
				// the run i..j, its exact ends where |sin| = thr
				double ua = i == 0 ? 0 : F11bEdge(n, thr, (i - 1.0) / NS, i / (double)NS), ub = j == NS ? 1 : F11bEdge(n, thr, j / (double)NS, (j + 1.0) / NS);
				g.beginPath();
				for (int q = 0; q <= 14; q++) { double u = ua + (ub - ua) * q / 14, s = JS.sin(n * Math.PI * u), x = -L + 2 * L * u, w = A * Math.Abs(s); double qx = cx + ux * x - uy * w, qy = cy + uy * x + ux * w; if (q == 0) g.moveTo(qx, qy); else g.lineTo(qx, qy); }
				for (int q = 14; q >= 0; q--) { double u = ua + (ub - ua) * q / 14, s = JS.sin(n * Math.PI * u), x = -L + 2 * L * u, w = A * Math.Abs(s); g.lineTo(cx + ux * x + uy * w, cy + uy * x - ux * w); }
				g.closePath();
				if (fill) { g.fillStyle = col; g.fill(); g.strokeStyle = F11bP(0.95); g.lineWidth = 2; g.stroke(); }
				else { g.strokeStyle = col; g.lineWidth = 2; if (dash) g.setLineDash(new double[] { 9, 8 }); g.stroke(); g.setLineDash(new double[0]); }
				// the shaking string inside it
				if (fill)
				{
					g.strokeStyle = F11bL(0.95); g.lineWidth = 3; g.beginPath();
					for (int q = 0; q <= 16; q++) { double u = ua + (ub - ua) * q / 16, s = JS.sin(n * Math.PI * u), x = -L + 2 * L * u, w = A * s * ph; double qx = cx + ux * x - uy * w, qy = cy + uy * x + ux * w; if (q == 0) g.moveTo(qx, qy); else g.lineTo(qx, qy); }
					g.stroke();
				}
				i = j + 1;
			}
		}
		h.draw = t =>
		{
			double a = Math.Min(F11bK(t, t0, t0 + 0.6), 1 - F11bK(t, t1, t1 + 0.6)); if (a <= 0) return;
			frame(t, out var cx, out var cy, out var ux, out var uy);
			int k = cur(t); double A = amp(k, t);
			// the string itself (gold), and its nodes as gold beads while it shakes
			g.strokeStyle = F11bY(0.75 * a); g.lineWidth = 3; g.beginPath(); g.moveTo(cx - ux * L, cy - uy * L); g.lineTo(cx + ux * L, cy + uy * L); g.stroke();
			// the next pluck, dashed
			for (int q = k + 1; q < np; q++)
			{
				if (pt[q] - t > wn) break;
				band(cx, cy, ux, uy, pn[q], pa[q], 0, false, F11bG(0.4 + 0.5 * F11bK(t, pt[q] - wn, pt[q])), true);
				for (int m = 1; m < pn[q]; m++) { double x = -L + 2 * L * m / pn[q]; g.fillStyle = F11bY(0.9); circle(cx + ux * x, cy + uy * x, 6); }
				break;
			}
			if (A >= 3)
			{
				double ph = JS.cos((t - pt[k]) * TAU * 7.5);
				band(cx, cy, ux, uy, pn[k], A, ph, true, F11bP(0.42 * a), false);
				for (int m = 1; m < pn[k]; m++) { double x = -L + 2 * L * m / pn[k]; g.fillStyle = F11bY(0.95); circle(cx + ux * x, cy + uy * x, 5); }
			}
		};
		h.hit = (p, t) =>
		{
			int k = cur(t); if (k < 0) return false; double A = amp(k, t); if (A < 3) return false;
			frame(t, out var cx, out var cy, out var ux, out var uy);
			double rx = p.x - cx, ry = p.y - cy, x = rx * ux + ry * uy, y = -rx * uy + ry * ux;
			// the player's circle against the band: its middle and its two ends along the string
			double m = p.r - 2;
			foreach (var xq in new[] { x, x - m, x + m })
			{
				if (Math.Abs(xq) > L) continue;
				double s = Math.Abs(JS.sin(pn[k] * Math.PI * (xq + L) / (2 * L)));
				if (s >= thr && Math.Abs(y) < A * s + m) return true;
			}
			return false;
		};
		return h;
	}
	// where |sin(n pi u)| crosses thr between u0 and u1 (bisection)
	static double F11bEdge(int n, double thr, double u0, double u1)
	{
		double f0 = Math.Abs(JS.sin(n * Math.PI * u0)) - thr;
		for (int i = 0; i < 24; i++) { double m = (u0 + u1) / 2, fm = Math.Abs(JS.sin(n * Math.PI * m)) - thr; if ((fm < 0) == (f0 < 0)) { u0 = m; f0 = fm; } else u1 = m; }
		return (u0 + u1) / 2;
	}
	// the strum: the pick sweeps one lane of the strings. A block (length bl along, from v0 to v1 across) in the frame of the angle an (deg)
	// round the screen's middle, moving from u0 to u1 along it in dur s. The lane shows dashed wn s before.
	public static Hz F11bSweep(double an, double v0, double v1, double u0, double u1, double bl, double tw, double ts, double dur)
	{
		var h = new Hz { kind = "F11bSweep" }; double a = an * Math.PI / 180, ux = JS.cos(a), uy = JS.sin(a), te = ts + dur;
		double pos(double t) => u0 + (u1 - u0) * clamp((t - ts) / dur, 0, 1);
		void quad(double ua, double ub, double va, double vb)
		{
			double cx = W / 2, cy = H / 2;
			g.beginPath(); g.moveTo(cx + ux * ua - uy * va, cy + uy * ua + ux * va); g.lineTo(cx + ux * ub - uy * va, cy + uy * ub + ux * va);
			g.lineTo(cx + ux * ub - uy * vb, cy + uy * ub + ux * vb); g.lineTo(cx + ux * ua - uy * vb, cy + uy * ua + ux * vb); g.closePath();
		}
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te + 0.3) return;
			double sd = Math.Sign(u1 - u0);
			if (t < ts)
			{
				double k = F11bK(t, tw, ts);
				quad(Math.Min(u0, u1), Math.Max(u0, u1), v0, v1); g.strokeStyle = F11bG(0.35 + 0.45 * k); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 }); g.stroke(); g.setLineDash(new double[0]);
				// arrows along the lane
				double vm = (v0 + v1) / 2;
				for (int i = 0; i < 6; i++)
				{
					double u = Math.Min(u0, u1) + (Math.Abs(u1 - u0)) * (i + 0.5) / 6, c = W / 2 + ux * u - uy * vm, e = H / 2 + uy * u + ux * vm;
					g.strokeStyle = F11bG(0.5 * k); g.lineWidth = 3; g.beginPath(); g.moveTo(c - ux * sd * 14 - uy * 12, e - uy * sd * 14 + ux * 12); g.lineTo(c, e); g.lineTo(c - ux * sd * 14 + uy * 12, e - uy * sd * 14 - ux * 12); g.stroke();
				}
				return;
			}
			double f = 1 - F11bK(t, te, te + 0.3), p = pos(t);
			// the pick: a block with a round point in front, and a pink smear behind it
			quad(p - sd * bl * 2.2, p - sd * bl / 2, v0 + 4, v1 - 4); g.fillStyle = F11bG(0.1 * f); g.fill();   // the air behind it (grey: it does not hurt)
			quad(p - sd * bl / 2, p + sd * bl / 2, v0, v1); g.fillStyle = F11bP(0.9 * f); g.fill();
			g.fillStyle = "rgba(150,10,58," + str(0.6 * f) + ")";
			for (double v = v0 + 18; v < v1 - 10; v += 28) { double c = W / 2 + ux * p - uy * v, e = H / 2 + uy * p + ux * v; circle(c, e, 5); }
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false; double sd = Math.Sign(u1 - u0), q = pos(t);
			double rx = p.x - W / 2, ry = p.y - H / 2, x = rx * ux + ry * uy, y = -rx * uy + ry * ux;
			double xa = Math.Min(q - sd * bl / 2, q + sd * bl / 2), xb = Math.Max(q - sd * bl / 2, q + sd * bl / 2);
			return x > xa - p.r + 2 && x < xb + p.r - 2 && y > v0 - p.r + 2 && y < v1 + p.r - 2;
		};
		return h;
	}

	// the pick (the chord): a pink teardrop running straight from (x0, y0) to (x1, y1) in dur s; its way shows dashed wn s before
	public static Hz F11bPick(double x0, double y0, double x1, double y1, double r, double tw, double ts, double dur)
	{
		var h = new Hz { kind = "F11bPick" }; double te = ts + dur, an = Math.Atan2(y1 - y0, x1 - x0);
		void at(double t, out double x, out double y) { double u = clamp((t - ts) / dur, 0, 1); x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u; }
		void drop(double x, double y, string col, bool line)
		{
			g.save(); g.translate(x, y); g.rotate(an); g.beginPath(); g.moveTo(r * 1.7, 0); g.quadraticCurveTo(r * 0.6, -r * 1.05, -r * 0.4, -r * 0.95);
			g.arc(-r * 0.4, 0, r * 0.95, -Math.PI / 2, Math.PI / 2, true); g.quadraticCurveTo(r * 0.6, r * 1.05, r * 1.7, 0); g.closePath();
			if (line) { g.strokeStyle = col; g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.stroke(); g.setLineDash(new double[0]); } else { g.fillStyle = col; g.fill(); }
			g.restore();
		}
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return;
			if (t < ts) { double k = F11bK(t, tw, ts); F11bDash(x0, y0, x1, y1, F11bG(0.3 + 0.5 * k), 2); drop(x0 + (x1 - x0) * 0.04, y0 + (y1 - y0) * 0.04, F11bG(0.4 + 0.5 * k), true); return; }
			at(t, out var x, out var y); drop(x, y, F11bP(0.95), false);
			g.fillStyle = F11bY(0.5); circle(x - JS.cos(an) * r * 0.3, y - JS.sin(an) * r * 0.3, r * 0.28);
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false; at(t, out var x, out var y);
			return F11bSeg(p.x, p.y, x - JS.cos(an) * r * 0.4, y - JS.sin(an) * r * 0.4, x + JS.cos(an) * r * 0.9, y + JS.sin(an) * r * 0.9) < r * 0.85 + p.r - 2;
		};
		return h;
	}

	// ---------- the letter ----------
	// The pen writes glyphs: glyph j's outlines (contours ci[c]..ci[c+1] of pts, in its own frame: (cx, cy) + turn kb/ka in degrees)
	// from gt[4j] to gt[4j+1] s, at an even speed. Fresh ink hurts for life s, then dries gold. When gt[4j+2] >= 0 the glyph fills
	// with ink at that time for gt[4j+3] s (its inside hurts; it shows grey the beat before). The pen's way shows dashed wn s ahead.
	public static Hz F11bPen(double[] pts, int[] ci, int[] cg, double[] gt, double cx, double cy, double[] kb, double[] ka, double life, double w, double wn, double t1, bool still = false)
	{
		var h = new Hz { kind = "F11bPen" }; int nc = ci.Length - 1, npt = pts.Length / 2, ng = gt.Length / 4;
		// the time each point is written
		var tp = new double[npt];
		for (int j = 0; j < ng; j++)
		{
			double len = 0;
			for (int c = 0; c < nc; c++) if (cg[c] == j) len += F11bClen(pts, ci[c], ci[c + 1]);
			double s = 0, ta = gt[4 * j], tb = gt[4 * j + 1];
			for (int c = 0; c < nc; c++)
			{
				if (cg[c] != j) continue;
				for (int i = ci[c]; i < ci[c + 1]; i++)
				{
					if (i > ci[c]) s += hypot(pts[2 * i] - pts[2 * i - 2], pts[2 * i + 1] - pts[2 * i - 1]);
					tp[i] = ta + (tb - ta) * (len > 0 ? s / len : 0);
				}
				// the closing segment back to the contour's first point
				int i0 = ci[c], i1 = ci[c + 1] - 1; s += hypot(pts[2 * i0] - pts[2 * i1], pts[2 * i0 + 1] - pts[2 * i1 + 1]);
			}
		}
		double tEnd(int c) { int i0 = ci[c], i1 = ci[c + 1] - 1; int j = cg[c]; double ta = gt[4 * j], tb = gt[4 * j + 1]; double nxt = tb; for (int q = c + 1; q < nc; q++) if (cg[q] == j) { nxt = tp[ci[q]]; break; } return nxt; }
		var tc = new double[nc]; for (int c = 0; c < nc; c++) tc[c] = tEnd(c);
		double t0 = gt[0] - wn;
		// still: the outlines are already there (another pen wrote them): only the fills, no pen and no ink
		if (still) { t0 = 1e9; for (int j = 0; j < ng; j++) if (gt[4 * j + 2] >= 0) t0 = Math.Min(t0, gt[4 * j + 2] - wn); }
		double rot(double t) => F11bLerp(kb, ka, t) * Math.PI / 180;
		void toW(double lx, double ly, double ca, double sa, out double x, out double y) { x = cx + lx * ca - ly * sa; y = cy + lx * sa + ly * ca; }
		void path(int c, double ca, double sa, double tcut, bool upTo)
		{
			// contour c as written up to the time tcut (upTo) or all of it
			int i0 = ci[c], i1 = ci[c + 1];
			g.beginPath(); toW(pts[2 * i0], pts[2 * i0 + 1], ca, sa, out var x, out var y); g.moveTo(x, y);
			for (int i = i0 + 1; i <= i1; i++)
			{
				int ii = i == i1 ? i0 : i; double ti = i == i1 ? tc[c] : tp[i];
				if (upTo && ti > tcut)
				{
					double tprev = tp[i - 1], u = ti > tprev ? (tcut - tprev) / (ti - tprev) : 1;
					double lx = pts[2 * (i - 1)] + (pts[2 * ii] - pts[2 * (i - 1)]) * u, ly = pts[2 * (i - 1) + 1] + (pts[2 * ii + 1] - pts[2 * (i - 1) + 1]) * u;
					toW(lx, ly, ca, sa, out x, out y); g.lineTo(x, y); break;
				}
				toW(pts[2 * ii], pts[2 * ii + 1], ca, sa, out x, out y); g.lineTo(x, y);
			}
		}
		void glyphPath(int j, double ca, double sa)
		{
			g.beginPath();
			for (int c = 0; c < nc; c++)
			{
				if (cg[c] != j) continue; int i0 = ci[c], i1 = ci[c + 1];
				for (int i = i0; i < i1; i++) { toW(pts[2 * i], pts[2 * i + 1], ca, sa, out var x, out var y); if (i == i0) g.moveTo(x, y); else g.lineTo(x, y); }
				g.closePath();
			}
		}
		// the pen's point at time t: (contour, x, y) or none
		bool penAt(double t, out double lx, out double ly)
		{
			lx = ly = 0;
			for (int c = 0; c < nc; c++)
			{
				int i0 = ci[c], i1 = ci[c + 1];
				if (t < tp[i0] || t >= tc[c]) continue;
				for (int i = i0 + 1; i <= i1; i++)
				{
					int ii = i == i1 ? i0 : i; double ti = i == i1 ? tc[c] : tp[i];
					if (ti >= t) { double tprev = tp[i - 1], u = ti > tprev ? (t - tprev) / (ti - tprev) : 1; lx = pts[2 * (i - 1)] + (pts[2 * ii] - pts[2 * (i - 1)]) * u; ly = pts[2 * (i - 1) + 1] + (pts[2 * ii + 1] - pts[2 * (i - 1) + 1]) * u; return true; }
				}
			}
			return false;
		}
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double a = 1 - F11bK(t, t1, t1 + 0.6); if (a <= 0) return;
			double r = rot(t), ca = JS.cos(r), sa = JS.sin(r);
			g.lineCap = "round"; g.lineJoin = "round";
			// the fills: a dashed outline the beat before; once dry, gold (a still pen leaves that to the pen that wrote it)
			for (int j = 0; j < ng; j++)
			{
				double fb = gt[4 * j + 2], fd = gt[4 * j + 3]; if (fb < 0 || t < fb - wn || (t >= fb && t < fb + fd)) continue;
				if (t >= fb + fd && still) continue;
				glyphPath(j, ca, sa);
				if (t < fb) { g.strokeStyle = F11bG(0.35 + 0.5 * F11bK(t, fb - wn, fb)); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.stroke(); g.setLineDash(new double[0]); }
				else { g.fillStyle = F11bY(0.16 * a); g.fill(); }
			}
			// the way ahead, dashed
			for (int c = 0; c < nc; c++)
			{
				int i0 = ci[c], i1 = ci[c + 1]; if (tc[c] < t || tp[i0] > t + wn) continue;
				g.beginPath(); bool st = false;
				for (int i = i0; i <= i1; i++)
				{
					int ii = i == i1 ? i0 : i; double ti = i == i1 ? tc[c] : tp[i]; if (ti < t) continue; if (ti > t + wn) break;
					toW(pts[2 * ii], pts[2 * ii + 1], ca, sa, out var x, out var y); if (!st) { g.moveTo(x, y); st = true; } else g.lineTo(x, y);
				}
				if (st) { g.strokeStyle = F11bG(0.6); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.stroke(); g.setLineDash(new double[0]); }
			}
			// the ink: dry gold, fresh pink
			for (int c = 0; c < nc && !still; c++)
			{
				if (t < tp[ci[c]]) continue;
				bool done = t >= tc[c];
				if (done && t - tc[c] > life) { path(c, ca, sa, t, false); g.strokeStyle = F11bY(0.55 * a); g.lineWidth = Math.Max(2, w * 0.45); g.stroke(); continue; }
				// fresh: the part written in the last life s is pink, the older part gold
				path(c, ca, sa, t, true); g.strokeStyle = F11bP(0.95 * a); g.lineWidth = w; g.stroke();
				if (t - tp[ci[c]] > life) { path(c, ca, sa, t - life, true); g.strokeStyle = F11bY(0.7 * a); g.lineWidth = w + 1; g.lineCap = "butt"; g.stroke(); g.lineCap = "round"; }   // butt: its end may not cover the fresh ink
			}
			// the fills while they hurt: pink, with a pink edge
			for (int j = 0; j < ng; j++)
			{
				double fb = gt[4 * j + 2], fd = gt[4 * j + 3]; if (fb < 0 || t < fb || t >= fb + fd) continue;
				glyphPath(j, ca, sa); g.fillStyle = F11bP(0.96 * a); g.fill(); g.strokeStyle = F11bP(0.95 * a); g.lineWidth = 8; g.stroke();
			}
			// the pen
			if (penAt(t, out var px, out var py))
			{
				toW(px, py, ca, sa, out var x, out var y);
				g.fillStyle = F11bP(1); circle(x, y, 11); g.fillStyle = F11bY(0.9); circle(x, y, 4);
			}
			else
			{
				// the pen waits at the next start
				for (int c = 0; c < nc; c++) { double s0 = tp[ci[c]]; if (s0 > t && s0 - t < wn) { toW(pts[2 * ci[c]], pts[2 * ci[c] + 1], ca, sa, out var x, out var y); g.strokeStyle = F11bG(0.75); g.lineWidth = 2; g.beginPath(); g.arc(x, y, 11, 0, TAU); g.stroke(); break; } }
			}
			g.lineCap = "butt"; g.lineJoin = "miter";
		};
		h.hit = (p, t) =>
		{
			if (t < gt[0] || t > t1) return false;
			double r = rot(t), ca = JS.cos(r), sa = JS.sin(r);
			// the player in the glyphs' frame
			double rx = p.x - cx, ry = p.y - cy, x = rx * ca + ry * sa, y = -rx * sa + ry * ca, rr = w / 2 + p.r - 2;
			if (!still && penAt(t, out var px, out var py) && hypot(x - px, y - py) < 11 + p.r - 2) return true;
			for (int c = 0; c < nc && !still; c++)
			{
				int i0 = ci[c], i1 = ci[c + 1]; if (t < tp[i0] || t - tc[c] > life) continue;
				for (int i = i0 + 1; i <= i1; i++)
				{
					int ii = i == i1 ? i0 : i; double ti = i == i1 ? tc[c] : tp[i], tprev = tp[i - 1];
					if (tprev > t) break; if (t - ti > life) continue;
					double ex = pts[2 * ii], ey = pts[2 * ii + 1];
					if (ti > t) { double u = ti > tprev ? (t - tprev) / (ti - tprev) : 1; ex = pts[2 * (i - 1)] + (ex - pts[2 * (i - 1)]) * u; ey = pts[2 * (i - 1) + 1] + (ey - pts[2 * (i - 1) + 1]) * u; }
					// a long segment: only its part written in the last life s (the part drawn pink)
					double sx = pts[2 * (i - 1)], sy = pts[2 * (i - 1) + 1];
					if (t - tprev > life && ti > tprev) { double u0 = (t - life - tprev) / (ti - tprev); sx += (pts[2 * ii] - sx) * u0; sy += (pts[2 * ii + 1] - sy) * u0; }
					if (F11bSeg(x, y, sx, sy, ex, ey) < rr) return true;
				}
			}
			for (int j = 0; j < ng; j++)
			{
				double fb = gt[4 * j + 2], fd = gt[4 * j + 3]; if (fb < 0 || t < fb || t > fb + fd) continue;
				if (F11bInside(pts, ci, cg, j, x, y, p.r - 2)) return true;
			}
			return false;
		};
		return h;
	}
	static double F11bClen(double[] pts, int i0, int i1)
	{
		double s = 0; for (int i = i0 + 1; i < i1; i++) s += hypot(pts[2 * i] - pts[2 * i - 2], pts[2 * i + 1] - pts[2 * i - 1]);
		return s + hypot(pts[2 * i0] - pts[2 * (i1 - 1)], pts[2 * i0 + 1] - pts[2 * (i1 - 1) + 1]);
	}
	// the point is inside glyph j (nonzero winding over its contours: joined letters overlap), or its circle (radius m) touches an edge
	static bool F11bInside(double[] pts, int[] ci, int[] cg, int j, double x, double y, double m)
	{
		int wn = 0; double best = 1e9;
		for (int c = 0; c < cg.Length; c++)
		{
			if (cg[c] != j) continue; int i0 = ci[c], i1 = ci[c + 1];
			for (int i = i0, k = i1 - 1; i < i1; k = i++)
			{
				double xi = pts[2 * i], yi = pts[2 * i + 1], xk = pts[2 * k], yk = pts[2 * k + 1];
				double cr = (xi - xk) * (y - yk) - (x - xk) * (yi - yk);
				if (yk <= y) { if (yi > y && cr > 0) wn++; } else if (yi <= y && cr < 0) wn--;
				best = Math.Min(best, F11bSeg(x, y, xi, yi, xk, yk));
			}
		}
		return wn != 0 || best < m;
	}

	// a diamond (a letter's dot): runs from (x0, y0) to (x1, y1) in dur s, then stays hold s (it hurts all that time); with stay it then
	// sits dry and gold till t1. Its way shows dashed wn s before.
	public static Hz F11bDot(double x0, double y0, double x1, double y1, double r, double tw, double ts, double dur, double hold, bool stay, double t1)
	{
		var h = new Hz { kind = "F11bDot" }; double te = ts + dur + hold, an = Math.Atan2(y1 - y0, x1 - x0);
		void at(double t, out double x, out double y) { double u = dur <= 0 ? 1 : clamp((t - ts) / dur, 0, 1); x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u; }
		void dia(double x, double y, double s, double sp)
		{
			g.save(); g.translate(x, y); g.rotate(sp); g.beginPath(); g.moveTo(0, -s * 1.25); g.lineTo(s, 0); g.lineTo(0, s * 1.25); g.lineTo(-s, 0); g.closePath(); g.restore();
		}
		h.update = (t, dt) => { if (t > (stay ? t1 + 0.6 : te + 0.2)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ts)
			{
				double k = F11bK(t, tw, ts); F11bDash(x0, y0, x1, y1, F11bG(0.25 + 0.45 * k), 2);
				dia(x1, y1, r, 0); g.strokeStyle = F11bG(0.4 + 0.5 * k); g.lineWidth = 2; g.stroke(); return;
			}
			at(t, out var x, out var y);
			if (t <= te) { dia(x, y, r, t < ts + dur ? an + Math.PI / 2 : 0); g.fillStyle = F11bP(0.95); g.fill(); g.strokeStyle = F11bL(0.9); g.lineWidth = 2; g.stroke(); return; }
			if (stay) { double a = 1 - F11bK(t, t1, t1 + 0.6); dia(x1, y1, r, 0); g.fillStyle = F11bY(0.5 * a); g.fill(); }
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false; at(t, out var x, out var y);
			return hypot(p.x - x, p.y - y) < r * 0.95 + p.r - 2;
		};
		return h;
	}

	// a line of dots: a chain of diamonds across the whole screen (vert: a row moving from y p0 to p1, else a column moving in x) in dur s,
	// with one space (the space between two words) gw wide round g. It shows dashed, with its space, wn s before.
	public static Hz F11bRow(bool vert, double p0, double p1, double gc, double gw, double r, double tw, double ts, double dur)
	{
		var h = new Hz { kind = "F11bRow" }; double te = ts + dur, L = vert ? W : H, sp = r * 1.7;
		double pos(double t) => p0 + (p1 - p0) * clamp((t - ts) / dur, 0, 1);
		void chain(double c, bool line, string col)
		{
			if (line)
			{
				if (vert) { F11bDash(-10, c, gc - gw / 2, c, col, 2); F11bDash(gc + gw / 2, c, W + 10, c, col, 2); }
				else { F11bDash(c, -10, c, gc - gw / 2, col, 2); F11bDash(c, gc + gw / 2, c, H + 10, col, 2); }
				return;
			}
			g.fillStyle = col;
			for (double u = sp / 2 - 10; u < L + 10; u += sp)
			{
				if (Math.Abs(u - gc) < gw / 2 + r * 0.5) continue;
				double x = vert ? u : c, y = vert ? c : u; g.beginPath();
				if (vert) { g.moveTo(x - r, y); g.lineTo(x, y - r * 1.25); g.lineTo(x + r, y); g.lineTo(x, y + r * 1.25); }
				else { g.moveTo(x - r * 1.25, y); g.lineTo(x, y - r); g.lineTo(x + r * 1.25, y); g.lineTo(x, y + r); }
				g.closePath(); g.fill();
			}
			// the thread the dots are on
			g.strokeStyle = F11bP(0.9); g.lineWidth = 3; g.beginPath();
			if (vert) { g.moveTo(-10, c); g.lineTo(gc - gw / 2, c); g.moveTo(gc + gw / 2, c); g.lineTo(W + 10, c); }
			else { g.moveTo(c, -10); g.lineTo(c, gc - gw / 2); g.moveTo(c, gc + gw / 2); g.lineTo(c, H + 10); }
			g.stroke();
		}
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return;
			if (t < ts) { double k = F11bK(t, tw, ts); chain(p0 + (p1 - p0) * 0.03, true, F11bG(0.3 + 0.5 * k)); return; }
			chain(pos(t), false, F11bP(0.95));
		};
		h.hit = (p, t) =>
		{
			if (t < ts || t > te) return false; double c = pos(t), a = vert ? p.y : p.x, u = vert ? p.x : p.y;
			if (Math.Abs(a - c) >= r * 1.1 + p.r - 2) return false;
			return Math.Abs(u - gc) > gw / 2 - p.r + 2;
		};
		return h;
	}

	// the end: gold light and the words (harmless)
	public static Hz F11bEnd(double t0)
	{
		var h = new Hz { kind = "F11bEnd" };
		h.update = (t, dt) => { if (t > t0 + 4) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = F11bK(t, t0, t0 + 0.8), f = 1 - F11bK(t, t0 + 2.6, t0 + 4);
			var gr = g.createRadialGradient(W / 2, H / 2, 10, W / 2, H / 2, 200 + 700 * k); gr.addColorStop(0, F11bY(0.7 * f)); gr.addColorStop(0.5, F11bY(0.25 * f)); gr.addColorStop(1, F11bY(0)); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.save(); g.globalAlpha = f * k; g.font = "800 92px " + FONT; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = "#fff4dc"; g.fillText("أجمل غرامي", W / 2, H / 2); g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_F11.json) ----------
	// Only pink hurts, so nothing may hide an attack: the places (the oud, the paper, the rose) are drawn first, under every attack
	// (as in F11.cs: the first thing in the list draws the places before itself).
	static readonly List<(Hz h, Action<double> d)> F11bPlaces = new();
	static void F11bUnder(Hz h) { F11bPlaces.Add((h, h.draw)); h.draw = t => F11bFirst(h, t); hazards.Add(h); }
	static void F11bAdd(Hz h) { var d = h.draw; h.draw = t => { F11bFirst(h, t); d(t); }; hazards.Add(h); }
	static void F11bFirst(Hz h, double t)
	{
		if (hazards.Count == 0 || hazards[0] != h) return;
		F11bPlaces.RemoveAll(q => !hazards.Contains(q.h));
		foreach (var q in F11bPlaces) q.d(t);
	}
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F11bwn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F11bez(double v, double ez) => easy ? v * ez : v;
	static bool F11bskip(DodgeEv e) => easy && e.flag("ezskip");

	static readonly int _kf11bstage = DodgeKind("F11bstage", e => () => F11bUnder(F11bStage(e.i("part"), B(e.b), B(e.num("to")), e.nums("kb").Select(B).ToArray(), e.nums("ka"))));
	static readonly int _kf11bfx = DodgeKind("F11bfx", e => () => F11bAdd(F11bFx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2)))));
	// a string: plk = [beat, harmonic, size, easy skip] for each pluck
	static readonly int _kf11bstr = DodgeKind("F11bstr", e => { double wn = F11bwn(e); return () =>
	{
		var pl = e.nums("plk"); var pt = new List<double>(); var pn = new List<int>(); var pa = new List<double>();
		for (int i = 0; i + 3 < pl.Length; i += 4) { if (easy && pl[i + 3] > 0) continue; pt.Add(B(pl[i])); pn.Add((int)pl[i + 1]); pa.Add(F11bez(pl[i + 2], 0.9)); }
		F11bAdd(F11bString(e.num("px", W / 2), e.num("py", H / 2), e.num("d"), e.num("a0"), e.num("L", 700), e.nums("kb").Select(B).ToArray(), e.nums("ka"), pt.ToArray(), pn.ToArray(), pa.ToArray(),
			F11bez(e.num("thr", 0.25), 1.2), e.num("hold", 0.6) * SPB, e.num("dec", 0.5) * SPB, wn * SPB, B(e.b), B(e.num("to"))));
	}; });
	static readonly int _kf11bsweep = DodgeKind("F11bsweep", e => { double wn = F11bwn(e); return () =>
	{
		if (F11bskip(e)) return; double hit = e.num("hit");
		F11bAdd(F11bSweep(e.num("an"), e.num("v0"), e.num("v1"), e.num("u0"), e.num("u1"), e.num("bl", 60), B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.2 : 1)));
	}; });
	static readonly int _kf11bpick = DodgeKind("F11bpick", e => { double wn = F11bwn(e); return () =>
	{
		if (F11bskip(e)) return; double hit = e.num("hit");
		F11bAdd(F11bPick(e.num("x0"), e.num("y0"), e.num("x1"), e.num("y1"), F11bez(e.num("r", 26), 0.85), B(hit - wn), B(hit), e.num("dur") * SPB));
	}; });
	// the pen: gt = [start, end, fill beat (-1 none), fill length] per glyph, in beats
	static readonly int _kf11bpen = DodgeKind("F11bpen", e => { double wn = F11bwn(e); return () =>
	{
		var gt = e.nums("gt"); var gs = new double[gt.Length];
		for (int j = 0; j < gt.Length / 4; j++)
		{
			gs[4 * j] = B(gt[4 * j]); gs[4 * j + 1] = B(gt[4 * j + 1]);
			bool nofill = gt[4 * j + 2] < 0 || (easy && e.flag("ezfill"));
			gs[4 * j + 2] = nofill ? -1 : B(gt[4 * j + 2]); gs[4 * j + 3] = gt[4 * j + 3] * SPB * (easy ? 0.75 : 1);
		}
		F11bAdd(F11bPen(e.nums("pts"), e.nums("ci").Select(v => (int)v).ToArray(), e.nums("cg").Select(v => (int)v).ToArray(), gs, e.num("cx"), e.num("cy"),
			e.nums("kb").Select(B).ToArray(), e.nums("ka"), F11bez(e.num("life"), 0.7) * SPB, F11bez(e.num("w", 12), 0.8), wn * SPB, B(e.num("to")), e.flag("still")));
	}; });
	static readonly int _kf11bdot = DodgeKind("F11bdot", e => { double wn = F11bwn(e); return () =>
	{
		if (F11bskip(e)) return; double hit = e.num("hit"), x0 = e.num("x0"), y0 = e.num("y0"), x1 = e.num("x1"), y1 = e.num("y1");
		// aim: thrown at where the player is when it shows, as far as x1 says
		if (e.flag("aim") && player != null) { double an = Math.Atan2(player.y - y0, player.x - x0), ln = e.num("len", 1500); x1 = x0 + JS.cos(an) * ln; y1 = y0 + JS.sin(an) * ln; }
		F11bAdd(F11bDot(x0, y0, x1, y1, F11bez(e.num("r", 16), 0.85), B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.15 : 1), F11bez(e.num("hold"), 0.7) * SPB, e.flag("stay"), B(e.num("to", hit + 4))));
	}; });
	static readonly int _kf11brow = DodgeKind("F11brow", e => { double wn = F11bwn(e); return () =>
	{
		if (F11bskip(e)) return; double hit = e.num("hit");
		F11bAdd(F11bRow(e.flag("vert"), e.num("p0"), e.num("p1"), e.num("gc"), F11bez(e.num("gw", 130), 1.2), e.num("r", 14), B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.2 : 1)));
	}; });
	static readonly int _kf11bend = DodgeKind("F11bend", e => () => F11bAdd(F11bEnd(B(e.b))));
}
