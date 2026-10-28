using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L17 "حالف لا ما انام" (Jafar Al-Ghazal and Mohammed Jamal), the second design (10-28, his ask: build it again): "سهرة ما تنام", the party
// that swore not to sleep. Every attack moves on the drum (the song's hits at +0, +0.42 and +0.74 of each beat):
// part 1 the drum and the claps (the frame drums slam, the hands clap shut, the chobi dancers' line steps across, the fireworks, the lights),
// part 2 the record (the record's rings turn and turn back, the equalizer's tunnel, the dance ring stepping in and out),
// part 3 till morning (the fireworks show, the dancers close in from four sides, the lights and the drums, the last fireworks).
// Nothing is aimed or random: every hazard is a pure function of time, so hit(p, t) is right for any later t and a restart rebuilds it.
// The old design (L17.cs, kinds L17*) stays for the replays recorded with it. Data: data/dodge_L17.json (tools/level17/gen.py).
public static partial class G
{
	const double L17bCX = W / 2, L17bCY = H / 2;
	static double L17bK(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L17bP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L17bW(double a) => $"rgba(255,244,236,{str(clamp(a, 0, 1))})";
	static string L17bGold(double a) => $"rgba(255,200,110,{str(clamp(a, 0, 1))})";
	const string L17bDark = "rgba(90,0,32,0.42)";
	static double L17bEase(double k) => 1 - JS.pow(1 - clamp(k, 0, 1), 3);
	static double L17bAng(double a) { a %= TAU; if (a < -Math.PI) a += TAU; if (a > Math.PI) a -= TAU; return a; }
	static double L17bSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void L17bDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static void L17bRing(double x, double y, double r, string col, double w, bool dash = false)
	{
		g.strokeStyle = col; g.lineWidth = w; if (dash) g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(x, y, Math.Max(0, r), 0, TAU); g.stroke(); if (dash) g.setLineDash(new double[0]);
	}
	static void L17bGlow(bool on) { if (on) { g.shadowColor = PINK; g.shadowBlur = 16; } else g.shadowBlur = 0; }
	// a step from a to b that starts at beat s and takes d beats
	static double L17bStep(double bt, double s, double d, double a, double b) => bt <= s ? a : a + (b - a) * L17bEase((bt - s) / d);

	// ---------- the frame drums ----------
	// drum i (x, y, radius r) shows wn beats before its slam on beat sb (a dashed rim and a ring closing in on it), then hurts for hold beats
	public static Hz L17bDrums(double[] x, double[] y, double[] r, double[] sb, double hold, double wn)
	{
		var h = new Hz { kind = "L17bDrums" }; int n = sb.Length; double last = n > 0 ? sb.Max() : 0;
		h.update = (t, dt) => { if (BT(t) > last + hold + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < n; i++)
			{
				double u = bt - sb[i];
				if (u < -wn || u > hold + 0.4) continue;
				if (u < 0)
				{
					double k = L17bK(bt, sb[i] - wn, sb[i]);
					g.fillStyle = L17bP(0.05 + 0.2 * k); circle(x[i], y[i], r[i]);
					L17bRing(x[i], y[i], r[i], L17bW(0.25 + 0.5 * k), 3, true);
					L17bRing(x[i], y[i], r[i] * (1 + 0.7 * (1 - k)), L17bP(0.25 + 0.6 * k), 3);
					continue;
				}
				if (u < hold)
				{
					g.save(); L17bGlow(true); g.fillStyle = u * SPB < 0.05 ? PINKW : PINK; circle(x[i], y[i], r[i]); g.restore();
					// the jingles round the rim
					g.fillStyle = L17bDark; for (int j = 0; j < 8; j++) { double q = j * TAU / 8 + 0.2; circle(x[i] + JS.cos(q) * r[i] * 0.78, y[i] + JS.sin(q) * r[i] * 0.78, r[i] * 0.1); }
					L17bRing(x[i], y[i], r[i] * 0.5, L17bDark, 3);
					continue;
				}
				double k2 = (u - hold) / 0.4; L17bRing(x[i], y[i], r[i] * (1 + 0.35 * k2), L17bW(0.45 * (1 - k2)), 3);
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), m = p.r - 2;
			for (int i = 0; i < n; i++) if (bt >= sb[i] && bt < sb[i] + hold && hypot(p.x - x[i], p.y - y[i]) < r[i] + m) return true;
			return false;
		};
		return h;
	}

	// ---------- the claps ----------
	// two hands (hw half thick) come from both sides along angle a and meet at (x, y) on beat cb: they close in CL beats (faster and faster),
	// stay shut CH beats, then open (no longer hurting). The band they close on shows wn beats before
	const double L17bCL = 0.34, L17bCH = 0.16, L17bFar = 1500;
	public static Hz L17bClap(double[] x, double[] y, double[] a, double[] hw, double[] cb, double wn)
	{
		var h = new Hz { kind = "L17bClap" }; int n = cb.Length; double last = n > 0 ? cb.Max() : 0;
		double gap(double u) { double k = clamp((u + L17bCL) / L17bCL, 0, 1); return L17bFar * (1 - k * k); }
		h.update = (t, dt) => { if (BT(t) > last + L17bCH + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < n; i++)
			{
				double u = bt - cb[i];
				if (u < -wn - L17bCL || u > L17bCH + 0.4) continue;
				g.save(); g.translate(x[i], y[i]); g.rotate(a[i]); double w = hw[i];
				if (u < L17bCH)
				{
					double k = L17bK(bt, cb[i] - wn - L17bCL, cb[i] - L17bCL);
					g.fillStyle = L17bP(0.05 + 0.15 * k); g.fillRect(-L17bFar, -w, 2 * L17bFar, 2 * w);
					L17bDash(-L17bFar, -w, L17bFar, -w, L17bW(0.2 + 0.45 * k), 2); L17bDash(-L17bFar, w, L17bFar, w, L17bW(0.2 + 0.45 * k), 2);
					g.fillStyle = L17bW(0.3 + 0.5 * k); g.fillRect(-3, -w - 14, 6, 2 * w + 28);
				}
				if (u >= -L17bCL && u < L17bCH)
				{
					double gp = gap(u); string col = u >= 0 && u * SPB < 0.05 ? PINKW : PINK;
					L17bGlow(true); g.fillStyle = col; g.fillRect(gp, -w, L17bFar, 2 * w); g.fillRect(-gp - L17bFar, -w, L17bFar, 2 * w); L17bGlow(false);
					// the fingers
					g.fillStyle = L17bDark; for (int s = -1; s <= 1; s += 2) for (int j = 1; j <= 3; j++) g.fillRect(s > 0 ? gp + 14 : -gp - 74, -w + j * w * 0.5 - 2, 60, 4);
				}
				else if (u >= L17bCH)
				{
					double k = (u - L17bCH) / 0.4, gp = 30 + 400 * L17bEase(k); g.strokeStyle = L17bW(0.5 * (1 - k)); g.lineWidth = 2;
					g.strokeRect(gp, -w, L17bFar, 2 * w); g.strokeRect(-gp - L17bFar, -w, L17bFar, 2 * w);
				}
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), m = p.r - 2;
			for (int i = 0; i < n; i++)
			{
				double u = bt - cb[i]; if (u < -L17bCL || u >= L17bCH) continue;
				double c = JS.cos(a[i]), s = JS.sin(a[i]), dx = p.x - x[i], dy = p.y - y[i], uu = dx * c + dy * s, vv = -dx * s + dy * c;
				if (Math.Abs(vv) < hw[i] + m && Math.Abs(uu) > gap(u) - m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the chobi line ----------
	// dancers hand in hand in a line across the screen (vertical when vert) step on beats sb[k] from ps[k] to ps[k + 1] (x for a vertical line,
	// y for a flat one) in ST beats, and the hole where nobody holds hands slides from hc[k] to hc[k + 1] (half size hs). ps[0] / hc[0] are
	// where it waits (off screen). The next place and hole show from wn beats before each step
	const double L17bST = 0.24;
	public static Hz L17bChobi(bool vert, double[] sb, double[] ps, double[] hc, double hs, double dr, double sp, double lw, double wn, double te)
	{
		var h = new Hz { kind = "L17bChobi" }; int n = sb.Length; double L = vert ? H : W;
		int at(double bt) { int k = -1; for (int i = 0; i < n; i++) if (sb[i] <= bt) k = i; return k; }
		double pos(double bt) { int k = at(bt); return k < 0 ? ps[0] : L17bStep(bt, sb[k], L17bST, ps[k], ps[k + 1]); }
		double hole(double bt) { int k = at(bt); return k < 0 ? hc[0] : L17bStep(bt, sb[k], L17bST, hc[k], hc[k + 1]); }
		h.update = (t, dt) => { if (BT(t) > te + 0.5) h.dead = true; };
		void line(double c, double hcv, double q0, double q1, double width)
		{
			// a band across the screen at c, along q0 .. q1
			if (q1 <= q0) return;
			if (vert) g.fillRect(c - width / 2, q0, width, q1 - q0); else g.fillRect(q0, c - width / 2, q1 - q0, width);
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt > te) return; int k = at(bt);
			// the next step: where the line goes and where its hole will be
			if (k + 1 < n && bt > sb[k + 1] - wn)
			{
				double kk = L17bK(bt, sb[k + 1] - wn, sb[k + 1]), c = ps[k + 2], hh = hc[k + 2]; g.setLineDash(new double[] { 12, 10 });
				g.strokeStyle = L17bW(0.2 + 0.5 * kk); g.lineWidth = 3; g.beginPath();
				if (vert) { g.moveTo(c, 0); g.lineTo(c, hh - hs); g.moveTo(c, hh + hs); g.lineTo(c, H); } else { g.moveTo(0, c); g.lineTo(hh - hs, c); g.moveTo(hh + hs, c); g.lineTo(W, c); }
				g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = L17bP(0.05 + 0.12 * kk); double a0 = Math.Min(pos(bt), c), a1 = Math.Max(pos(bt), c);
				if (vert) { g.fillRect(a0, 0, a1 - a0, hh - hs); g.fillRect(a0, hh + hs, a1 - a0, H - hh - hs); } else { g.fillRect(0, a0, hh - hs, a1 - a0); g.fillRect(hh + hs, a0, W - hh - hs, a1 - a0); }
			}
			double cc = pos(bt), hv = hole(bt);
			g.save(); L17bGlow(true); g.fillStyle = k >= 0 && (bt - sb[k]) * SPB < 0.05 ? PINKW : PINK;
			line(cc, hv, -20, hv - hs, lw); line(cc, hv, hv + hs, L + 20, lw);
			for (double q = sp / 2; q < L + dr; q += sp)
			{
				if (Math.Abs(q - hv) < hs + dr) continue;
				double rr = dr * (1 + 0.12 * pulse);
				if (vert) circle(cc, q, rr); else circle(q, cc, rr);
			}
			g.restore();
			// the dancers' faces (darker)
			g.fillStyle = L17bDark;
			for (double q = sp / 2; q < L + dr; q += sp) { if (Math.Abs(q - hv) < hs + dr) continue; if (vert) circle(cc, q, dr * 0.42); else circle(q, cc, dr * 0.42); }
			// the hands that let go: white marks at the hole's ends
			g.fillStyle = L17bW(0.7);
			if (vert) { g.fillRect(cc - lw, hv - hs - 3, lw * 2, 3); g.fillRect(cc - lw, hv + hs, lw * 2, 3); } else { g.fillRect(hv - hs - 3, cc - lw, 3, lw * 2); g.fillRect(hv + hs, cc - lw, 3, lw * 2); }
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt > te) return false; double c = pos(bt), hv = hole(bt), m = p.r - 2;
			double ac = vert ? p.x - c : p.y - c, q = vert ? p.y : p.x;
			if (Math.Abs(ac) < lw / 2 + m && Math.Abs(q - hv) > hs - m) return true;
			if (Math.Abs(ac) > dr + m) return false;
			for (double qq = sp / 2; qq < L + dr; qq += sp)
			{
				if (Math.Abs(qq - hv) < hs + dr) continue;
				if (hypot(ac, q - qq) < dr + m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the fireworks ----------
	// shell i rises from (lx, H + 30) to (fx, fy) in fu beats and bursts on beat fb into n sparks flying out at sp px a second (the first at
	// angle a0, then round the way sg says (1 or -1), one every dl beats when dl > 0: a spiral), each spark sr in radius; a spark burns out as it
	// gets rng px away (it fades over its last 80 px and no longer hurts in its last 50). The rise and the sparks' ways show wn beats before the launch
	public static Hz L17bFire(double[] lx, double[] fx, double[] fy, double[] fb, double[] n, double[] a0, double[] sg, double[] dl, double[] sp, double fu, double sr, double rng, double wn)
	{
		var h = new Hz { kind = "L17bFire" }; int m = fb.Length; double FAR = rng, HURT = rng - 50;
		double lastT = 0; for (int i = 0; i < m; i++) lastT = Math.Max(lastT, B(fb[i] + n[i] * dl[i]) + FAR / sp[i]);
		double ang(int i, int j) => a0[i] + sg[i] * j * TAU / n[i];
		h.update = (t, dt) => { if (t > lastT + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < m; i++)
			{
				double u = bt - fb[i];
				if (u < -fu - wn) continue;
				if (u < 0)
				{
					double k = L17bK(bt, fb[i] - fu - wn, fb[i]);
					L17bDash(lx[i], H + 30, fx[i], fy[i], L17bW(0.12 + 0.3 * k), 2);
					// the sparks' ways
					g.strokeStyle = L17bW(0.12 + 0.4 * k); g.lineWidth = 2; g.beginPath();
					for (int j = 0; j < n[i]; j++) { double q = ang(i, j); g.moveTo(fx[i] + JS.cos(q) * 26, fy[i] + JS.sin(q) * 26); g.lineTo(fx[i] + JS.cos(q) * (40 + 50 * k), fy[i] + JS.sin(q) * (40 + 50 * k)); }
					g.stroke();
					if (u >= -fu)
					{
						double f = L17bEase((u + fu) / fu), sx = lx[i] + (fx[i] - lx[i]) * f, sy = H + 30 + (fy[i] - H - 30) * f;
						g.strokeStyle = L17bW(0.35); g.lineWidth = 3; g.beginPath(); g.moveTo(lx[i] + (fx[i] - lx[i]) * Math.Max(0, f - 0.25), H + 30 + (fy[i] - H - 30) * Math.Max(0, f - 0.25)); g.lineTo(sx, sy); g.stroke();
						g.save(); L17bGlow(true); g.fillStyle = PINK; circle(sx, sy, 11); g.restore();
					}
					continue;
				}
				double tt = t - B(fb[i]);
				if (tt * sp[i] - 60 > FAR + n[i] * dl[i] * SPB * sp[i]) continue;
				if (tt < 0.25) { g.fillStyle = L17bGold(0.5 * (1 - tt / 0.25)); circle(fx[i], fy[i], 30 + 120 * tt); }
				g.save(); L17bGlow(true);
				for (int j = 0; j < n[i]; j++)
				{
					double e = B(fb[i] + j * dl[i]); if (t < e) break; double d = (t - e) * sp[i]; if (d > FAR) continue;
					double q = ang(i, j), c = JS.cos(q), s = JS.sin(q);
					g.globalAlpha = clamp((FAR - d) / 80, 0, 1); g.fillStyle = t - e < 0.05 ? PINKW : PINK; circle(fx[i] + c * d, fy[i] + s * d, sr);
				}
				g.restore();
				g.strokeStyle = L17bW(0.14); g.lineWidth = 2; g.beginPath();
				for (int j = 0; j < n[i]; j++)
				{
					double e = B(fb[i] + j * dl[i]); if (t < e) break; double d = (t - e) * sp[i]; if (d > FAR) continue;
					double q = ang(i, j), c = JS.cos(q), s = JS.sin(q); g.moveTo(fx[i] + c * Math.Max(0, d - sr - 40), fy[i] + s * Math.Max(0, d - sr - 40)); g.lineTo(fx[i] + c * Math.Max(0, d - sr), fy[i] + s * Math.Max(0, d - sr));
				}
				g.stroke();
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), mm = p.r - 2;
			for (int i = 0; i < m; i++)
			{
				double u = bt - fb[i]; if (u < -fu) continue;
				if (u < 0) { double f = L17bEase((u + fu) / fu); if (hypot(p.x - lx[i] - (fx[i] - lx[i]) * f, p.y - H - 30 - (fy[i] - H - 30) * f) < 11 + mm) return true; continue; }
				for (int j = 0; j < n[i]; j++)
				{
					double e = B(fb[i] + j * dl[i]); if (t < e) break; double d = (t - e) * sp[i]; if (d > FAR) continue;
					if (d > HURT) continue; double q = ang(i, j); if (hypot(p.x - fx[i] - JS.cos(q) * d, p.y - fy[i] - JS.sin(q) * d) < sr + mm) return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- the equalizer ----------
	// nc bars from each side (the floor and the ceiling, or the two walls when side) leave a tunnel; at each step sb[k] the tunnel's middle goes to
	// a line from c0[k] (the first bar) to c1[k] (the last), each bar up or down from it by up to jag px, bar after bar (stg beats apart) in ST beats. On the bumps (bb) all the bars
	// jump in by bump px for a moment. The next tunnel shows from wn beats before. The bars grow in from ta and go at te
	public static Hz L17bEq(bool side, int nc, double[] sb, double[] c0, double[] c1, double jag, double gh, double[] bb, double bump, double stg, double wn, double ta, double te)
	{
		var h = new Hz { kind = "L17bEq" }; int n = sb.Length; double A = side ? H : W, Cr = side ? W : H, cw = A / nc, b0 = BT(ta), be = BT(te);
		double cAt(int k, int i) => c0[k] + (c1[k] - c0[k]) * i / Math.Max(1, nc - 1) + jag * JS.sin(i * 2.3 + k * 0.5);
		double cen(int i, double bt)
		{
			int k = 0; for (int j = 1; j < n; j++) if (sb[j] + i * stg <= bt) k = j;
			return k == 0 ? cAt(0, i) : L17bStep(bt, sb[k] + i * stg, L17bST, cAt(k - 1, i), cAt(k, i));
		}
		double bmp(double bt)
		{
			double v = 0;
			foreach (var b in bb) { double u = bt - b; double k = u < -0.06 ? 0 : u < 0 ? (u + 0.06) / 0.06 : u < 0.1 ? 1 : u < 0.22 ? 1 - (u - 0.1) / 0.12 : 0; v = Math.Max(v, k); }
			return v * bump;
		}
		double grow(double bt) => bt < be ? L17bEase(L17bK(bt, b0, b0 + 0.5)) : 1 - L17bEase(L17bK(bt, be, be + 0.4));
		// the two bars of column i: from the edge to lo, and from hi to the far edge
		(double lo, double hi) bars(int i, double bt)
		{
			double c = cen(i, bt), gg = gh - bmp(bt), s = grow(bt);
			return ((c - gg) * s, Cr - (Cr - c - gg) * s);
		}
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		void rect(double a0, double c0_, double aw, double ch) { if (side) g.fillRect(c0_, a0, ch, aw); else g.fillRect(a0, c0_, aw, ch); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt > be + 0.4) return;
			// before it starts: the first tunnel as an outline
			if (bt < b0)
			{
				double k = L17bK(bt, b0 - wn, b0);
				g.fillStyle = L17bP(0.04 + 0.12 * k);
				for (int i = 0; i < nc; i++) { double c = cAt(0, i); rect(i * cw + 2, 0, cw - 4, c - gh); rect(i * cw + 2, c + gh, cw - 4, Cr - c - gh); }
			}
			// the next tunnel
			int kn = -1; for (int j = 1; j < n; j++) if (bt < sb[j] && bt > sb[j] - wn) { kn = j; break; }
			if (kn > 0)
			{
				double k = L17bK(bt, sb[kn] - wn, sb[kn]); g.strokeStyle = L17bW(0.2 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 });
				for (int s = -1; s <= 1; s += 2)
				{
					g.beginPath();
					for (int i = 0; i < nc; i++) { double c = cAt(kn, i) + s * gh, a = i * cw; if (side) { if (i == 0) g.moveTo(c, a); g.lineTo(c, a + cw); } else { if (i == 0) g.moveTo(a, c); g.lineTo(a + cw, c); } }
					g.stroke();
				}
				g.setLineDash(new double[0]);
			}
			if (bt < b0) return;
			g.save(); L17bGlow(true); g.fillStyle = Math.Abs(bt - b0) * SPB < 0.05 ? PINKW : PINK;
			for (int i = 0; i < nc; i++) { var (lo, hi) = bars(i, bt); rect(i * cw + 2, -20, cw - 4, lo + 20); rect(i * cw + 2, hi, cw - 4, Cr - hi + 20); }
			g.restore();
			// the meter's lines in the bars
			g.fillStyle = L17bDark;
			for (int i = 0; i < nc; i++)
			{
				var (lo, hi) = bars(i, bt);
				for (double q = lo - 14; q > 0; q -= 22) rect(i * cw + 6, q, cw - 12, 5);
				for (double q = hi + 9; q < Cr; q += 22) rect(i * cw + 6, q, cw - 12, 5);
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= be) return false; double m = p.r - 2, a = side ? p.y : p.x, c = side ? p.x : p.y;
			int i0 = (int)Math.Floor((a - m) / cw), i1 = (int)Math.Floor((a + m) / cw);
			for (int i = Math.Max(0, i0); i <= Math.Min(nc - 1, i1); i++) { var (lo, hi) = bars(i, bt); if (c - m < lo || c + m > hi) return true; }
			return false;
		};
		return h;
	}

	// ---------- the record ----------
	// rings between the edges re[0] < re[1] < .. round (cx, cy) turn at w[i] radians a beat times the speed (from beat kb[j] it is km[j]; a minus
	// turns them all back), each with no[i] openings hh[i] radians half wide. Inside re[0] (the spindle) hurts too. An outline from tw, hurts
	// ta .. te. Little white arrows in the openings show which way they go, and blink the beat before they turn
	public static Hz L17bRecord(double cx, double cy, double[] re, double[] w, double[] no, double[] hh, double[] a0, double[] kb, double[] km, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17bRecord" }; int nr = w.Length; double b0 = BT(ta), bw = BT(tw), be = BT(te);
		double S(double bt)
		{
			double s = 0, prev = b0, sp = 1;
			for (int j = 0; j < kb.Length; j++) { if (kb[j] > bt) break; if (kb[j] > prev) s += sp * (kb[j] - prev); prev = Math.Max(prev, kb[j]); sp = km[j]; }
			return s + sp * Math.Max(0, bt - prev);
		}
		double spAt(double bt) { double sp = 1; for (int j = 0; j < kb.Length; j++) if (kb[j] <= bt) sp = km[j]; return sp; }
		double ringA(int i, double bt) => a0[i] + w[i] * S(Math.Max(bt, b0));
		void band(int i, double a, double r0, double r1)
		{
			int k = (int)no[i]; double per = TAU / k;
			g.beginPath();
			for (int j = 0; j < k; j++) { double s0 = a + j * per + hh[i], s1 = a + (j + 1) * per - hh[i]; g.moveTo(cx + JS.cos(s0) * r1, cy + JS.sin(s0) * r1); g.arc(cx, cy, r1, s0, s1); g.arc(cx, cy, r0, s1, s0, true); g.closePath(); }
			g.fill();
		}
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > be + 0.5) return; bool on = bt >= b0 && bt < be;
			double al = bt < b0 ? 0.08 + 0.22 * L17bK(bt, bw, b0) : bt < be ? 1 : 1 - L17bK(bt, be, be + 0.5);
			g.save();
			if (on) { L17bGlow(true); g.fillStyle = Math.Abs(bt - b0) * SPB < 0.05 ? PINKW : PINK; }
			else g.fillStyle = bt < b0 ? L17bP(al) : L17bW(0.35 * al);
			circle(cx, cy, re[0]);
			for (int i = 0; i < nr; i++) band(i, ringA(i, bt), re[i], re[i + 1]);
			L17bGlow(false);
			if (bt < b0) for (int i = 0; i <= nr; i++) L17bRing(cx, cy, re[i], L17bW(0.3 + 0.5 * L17bK(bt, bw, b0)), 2, true);
			// the grooves and the edges between the rings
			if (on)
			{
				g.strokeStyle = L17bDark; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < nr; i++) for (double r = re[i] + 14; r < Math.Min(re[i + 1], 760) - 8; r += 16) { g.moveTo(cx + r, cy); g.arc(cx, cy, r, 0, TAU); }
				g.stroke();
				L17bRing(cx, cy, re[0] * 0.45, L17bDark, 4);
				for (int i = 1; i < nr; i++) L17bRing(cx, cy, re[i], "rgba(40,0,14,0.9)", 4);
			}
			g.restore();
			// the arrows in the openings
			if (bt < be)
			{
				double sp = spAt(bt); double nx = -1; foreach (var b in kb) if (b > bt) { nx = b; break; }
				bool blink = nx > 0 && nx - bt < 1 && ((int)Math.Floor((nx - bt) * 8)) % 2 == 0;
				g.strokeStyle = blink ? L17bGold(0.95) : L17bW(0.55); g.lineWidth = 3; g.beginPath();
				for (int i = 0; i < nr; i++)
				{
					double a = ringA(i, bt), r = Math.Min((re[i] + re[i + 1]) / 2, 600), dir = Math.Sign(w[i] * sp); if (dir == 0) continue;
					for (int j = 0; j < no[i]; j++)
					{
						double c = a + j * TAU / no[i], tip = c + dir * 0.6 * hh[i], back = c - dir * 0.2 * hh[i], d = 10;
						g.moveTo(cx + JS.cos(back) * (r - d), cy + JS.sin(back) * (r - d)); g.lineTo(cx + JS.cos(tip) * r, cy + JS.sin(tip) * r); g.lineTo(cx + JS.cos(back) * (r + d), cy + JS.sin(back) * (r + d));
					}
				}
				g.stroke();
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= be) return false;
			double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), m = p.r - 2, pa = JS.atan2(dy, dx);
			if (d < re[0] + m) return true;
			for (int i = 0; i < nr; i++)
			{
				if (d + m <= re[i] || d - m >= re[i + 1]) continue;
				double a = ringA(i, bt), per = TAU / no[i], am = m / Math.Max(d, 1);
				double x = L17bAng(pa - a) % per; if (x < 0) x += per; if (x > per / 2) x -= per;
				if (Math.Abs(x) > hh[i] - am) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the dance ring ----------
	// dancers hand in hand on a circle round (cx, cy): its radius steps from rr[k] to rr[k + 1] on the beats sb[k] (in ST beats), it turns w
	// radians a beat, and nh holes (hh radians half wide) open in it. Each step's circle shows from wn beats before. Grey from tw, hurts ta .. te
	public static Hz L17bDance(double cx, double cy, double[] sb, double[] rr, double w, double a0, int nh, double hh, int nd, double dr, double lw, double wn, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17bDance" }; int n = sb.Length; double b0 = BT(ta), bw = BT(tw), be = BT(te);
		int at(double bt) { int k = -1; for (int i = 0; i < n; i++) if (sb[i] <= bt) k = i; return k; }
		double R(double bt) { int k = at(bt); return k < 0 ? rr[0] : L17bStep(bt, sb[k], L17bST, rr[k], rr[k + 1]); }
		double A(double bt) => a0 + w * (bt - b0);
		bool inHole(double rel, double extra) { double per = TAU / nh, x = L17bAng(rel) % per; if (x < 0) x += per; if (x > per / 2) x -= per; return Math.Abs(x) < hh + extra; }
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > be + 0.4) return; bool on = bt >= b0 && bt < be; double r = R(bt), a = A(bt);
			int k = at(bt);
			if (on && k + 1 < n && bt > sb[k + 1] - wn) { double kk = L17bK(bt, sb[k + 1] - wn, sb[k + 1]); L17bRing(cx, cy, rr[k + 2], L17bW(0.2 + 0.5 * kk), 3, true); }
			double al = bt < b0 ? 0.25 + 0.4 * L17bK(bt, bw, b0) : bt < be ? 1 : 1 - L17bK(bt, be, be + 0.4);
			g.save();
			if (on) { L17bGlow(true); g.fillStyle = k >= 0 && (bt - sb[k]) * SPB < 0.05 ? PINKW : PINK; g.strokeStyle = g.fillStyle; } else { g.fillStyle = L17bW(0.3 * al); g.strokeStyle = g.fillStyle; }
			// the hands: the circle but the holes
			g.lineWidth = lw; double per = TAU / nh; g.beginPath();
			for (int j = 0; j < nh; j++) { double s0 = a + j * per + hh, s1 = a + (j + 1) * per - hh; g.moveTo(cx + JS.cos(s0) * r, cy + JS.sin(s0) * r); g.arc(cx, cy, r, s0, s1); }
			g.stroke();
			double rd = dr * (on ? 1 + 0.12 * pulse : 1);
			for (int j = 0; j < nd; j++) { double q = a + (j + 0.5) * TAU / nd; if (inHole(q - a, dr / Math.Max(r, 1))) continue; circle(cx + JS.cos(q) * r, cy + JS.sin(q) * r, rd); }
			g.restore();
			if (on) { g.fillStyle = L17bDark; for (int j = 0; j < nd; j++) { double q = a + (j + 0.5) * TAU / nd; if (inHole(q - a, dr / Math.Max(r, 1))) continue; circle(cx + JS.cos(q) * r, cy + JS.sin(q) * r, dr * 0.42); } }
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= be) return false;
			double r = R(bt), a = A(bt), dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), m = p.r - 2, pa = JS.atan2(dy, dx);
			if (Math.Abs(d - r) < lw / 2 + m && !inHole(pa - a, -m / Math.Max(d, 1))) return true;
			if (Math.Abs(d - r) > dr + m) return false;
			for (int j = 0; j < nd; j++) { double q = a + (j + 0.5) * TAU / nd; if (inHole(q - a, dr / Math.Max(r, 1))) continue; if (hypot(p.x - cx - JS.cos(q) * r, p.y - cy - JS.sin(q) * r) < dr + m) return true; }
			return false;
		};
		return h;
	}

	// ---------- the party lights ----------
	// a thick beam (bw) from the lamp at (ox, oy): its angle goes from ka[k - 1] to ka[k] on beat kb[k] in mv beats (ka[0] = where it starts).
	// Grey from tw, hurts ta .. te. Where it sweeps next shows as a faint pink fan (and a dashed line where it stops) wn beats before
	public static Hz L17bLaser(double ox, double oy, double[] kb, double[] ka, double mv, double bw, double wn, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17bLaser" }; int n = kb.Length; double b0 = BT(ta), bwt = BT(tw), be = BT(te); const double LEN = 1700;
		double ang(double bt) { double a = ka[0]; for (int k = 1; k < n; k++) { if (kb[k] > bt) break; a = L17bStep(bt, kb[k], mv, ka[k - 1], ka[k]); } return a; }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bwt || bt > be + 0.3) return; double a = ang(bt); bool on = bt >= b0 && bt < be;
			if (on || bt < b0) for (int k = 1; k < n; k++)
			{
				if (bt < kb[k] - wn || bt >= kb[k]) continue;
				double kk = L17bK(bt, kb[k] - wn, kb[k]), s = ka[k - 1], e = ka[k];
				g.fillStyle = L17bP(0.03 + 0.07 * kk); g.beginPath(); g.moveTo(ox, oy); g.arc(ox, oy, LEN, Math.Min(s, e), Math.Max(s, e)); g.closePath(); g.fill();
				L17bDash(ox, oy, ox + JS.cos(e) * LEN, oy + JS.sin(e) * LEN, L17bW(0.25 + 0.5 * kk), 3);
			}
			double c = JS.cos(a), s2 = JS.sin(a);
			if (on)
			{
				g.save(); L17bGlow(true); g.strokeStyle = Math.Abs(bt - b0) * SPB < 0.05 ? PINKW : PINK; g.lineWidth = bw; g.lineCap = "butt";
				g.beginPath(); g.moveTo(ox, oy); g.lineTo(ox + c * LEN, oy + s2 * LEN); g.stroke(); g.restore();
				g.strokeStyle = "rgba(255,190,215,0.55)"; g.lineWidth = Math.Max(2, bw * 0.25); g.beginPath(); g.moveTo(ox, oy); g.lineTo(ox + c * LEN, oy + s2 * LEN); g.stroke();
			}
			else if (bt < b0) L17bDash(ox, oy, ox + c * LEN, oy + s2 * LEN, L17bW(0.2 + 0.5 * L17bK(bt, bwt, b0)), 4);
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= be) return false; double a = ang(bt);
			return L17bSeg(p.x, p.y, ox, oy, ox + JS.cos(a) * LEN, oy + JS.sin(a) * LEN) < bw / 2 + p.r - 2;
		};
		return h;
	}

	// the lamp of a light: drawn under everything (z -1, after the room), so nothing that hurts is ever under it
	public static Hz L17bLamp(double ox, double oy, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17bLamp", z = -1 }; double bwt = BT(tw), b0 = BT(ta), be = BT(te);
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t => { double bt = BT(t); if (bt < bwt || bt > be + 0.3) return; bool on = bt >= b0 && bt < be; g.fillStyle = L17bGold(on ? 0.95 : 0.6); circle(ox, oy, 20); g.fillStyle = "rgba(40,20,10,0.8)"; circle(ox, oy, 9); };
		return h;
	}

	// ---------- the room itself (harmless) ----------
	// beats 0 .. 64 the drum's skin under string lights, 64 .. 120 the record player, 120 .. the night over the roofs until the morning comes
	public static Hz L17bStage(double t0, double t1)
	{
		var h = new Hz { kind = "L17bStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.8 - t) / 0.8)); if (a <= 0) return; double bt = BT(t);
			double p1 = 1 - L17bK(bt, 63, 65), p3 = L17bK(bt, 119, 121), p2 = 1 - p1 - p3;
			g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(16,8,26,0.92)"; g.fillRect(0, 0, W, H);
			if (p1 > 0)
			{
				g.globalAlpha = a * p1;
				// the drum's skin
				g.fillStyle = "rgba(60,34,40,0.55)"; circle(L17bCX, L17bCY, 330 + 6 * pulse);
				L17bRing(L17bCX, L17bCY, 330 + 6 * pulse, L17bGold(0.35), 8);
				for (int i = 0; i < 16; i++) { double q = i * TAU / 16; g.fillStyle = L17bGold(0.3); circle(L17bCX + JS.cos(q) * 344, L17bCY + JS.sin(q) * 344, 7); }
				L17bRing(L17bCX, L17bCY, 120, "rgba(255,200,110,0.08)", 3);
			}
			if (p2 > 0)
			{
				g.globalAlpha = a * p2;
				// the record player: a dark disc and its grooves
				g.fillStyle = "rgba(10,6,14,0.8)"; circle(L17bCX, L17bCY, 345);
				g.strokeStyle = "rgba(200,190,230,0.06)"; g.lineWidth = 2; g.beginPath(); for (double r = 70; r < 340; r += 12) { g.moveTo(L17bCX + r, L17bCY); g.arc(L17bCX, L17bCY, r, 0, TAU); } g.stroke();
				g.fillStyle = L17bGold(0.25); circle(L17bCX, L17bCY, 60);
				// the arm
				g.strokeStyle = "rgba(200,190,230,0.15)"; g.lineWidth = 10; g.beginPath(); g.moveTo(1180, 90); g.lineTo(1090, 420); g.lineTo(980, 470); g.stroke();
			}
			if (p3 > 0)
			{
				g.globalAlpha = a * p3;
				for (int i = 0; i < 60; i++) { double sx = (i * 397.3) % W, sy = (i * 211.7 + i * i * 3.1) % (H * 0.7), tw = 0.25 + 0.25 * JS.sin(t * (1.3 + i % 5 * 0.4) + i); g.fillStyle = L17bW(tw * 0.5); circle(sx, sy, 1.2 + (i % 3) * 0.6); }
				double dawn = L17bK(bt, 160, 177.5);
				if (dawn > 0) { var gr0 = g.createLinearGradient(0, H, 0, 0); gr0.addColorStop(0, $"rgba(255,170,90,{str(0.32 * dawn)})"); gr0.addColorStop(1, "rgba(255,150,90,0)"); g.fillStyle = gr0; g.fillRect(0, 0, W, H); }
				// the roofs and their lit windows
				g.fillStyle = "rgba(6,3,12,0.95)"; g.beginPath(); g.moveTo(0, H);
				for (int i = 0; i <= 16; i++) { double x = i * 80, y = H - 70 - ((i * 37) % 5) * 22; g.lineTo(x, y); g.lineTo(x + 80, y); }
				g.lineTo(W, H); g.closePath(); g.fill();
				g.fillStyle = L17bGold(0.35); for (int i = 0; i < 16; i++) { double x = i * 80 + 20, y = H - 50 - ((i * 37) % 5) * 22; g.fillRect(x, y, 10, 12); if (i % 3 == 0) g.fillRect(x + 30, y + 22, 10, 12); }
			}
			g.globalAlpha = a;
			// the string lights along the top
			g.strokeStyle = "rgba(200,190,230,0.12)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, 40); g.quadraticCurveTo(W / 2, 110, W, 40); g.stroke();
			for (int i = 0; i < 17; i++)
			{
				double u = i / 16.0, x = u * W, y = 40 + 140 * u * (1 - u), on2 = (i + (int)Math.Floor(bt)) % 2 == 0 ? 1 : 0.4;
				g.fillStyle = (i % 3) switch { 0 => L17bGold(0.5 * on2), 1 => $"rgba(170,140,255,{str(0.45 * on2)})", _ => L17bW(0.4 * on2) }; circle(x, y + 10, 6);
			}
			var gv = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); gv.addColorStop(0, "rgba(0,0,0,0)"); gv.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = gv; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L17.json) ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here. An entry with ez 1 is left out on Easy
	static double L17bWn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L17bEz(double v, double ez) => easy ? v * ez : v;
	static int[] L17bKeep(DodgeEv e, int n) { var ez = e.nums("ez"); return Enumerable.Range(0, n).Where(i => !easy || ez.Length <= i || ez[i] == 0).ToArray(); }
	static double[] L17bSel(double[] a, int[] k) => k.Select(i => a[i]).ToArray();

	static readonly int _k17bfx = DodgeKind("L17bfx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L17bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k17bstage = DodgeKind("L17bstage", e => () => hazards.Add(L17bStage(B(e.b), B(e.num("to")))));
	static readonly int _k17bdrums = DodgeKind("L17bdrums", e => { double wn = L17bWn(e); return () =>
	{
		var k = L17bKeep(e, e.nums("sb").Length);
		hazards.Add(L17bDrums(L17bSel(e.nums("x"), k), L17bSel(e.nums("y"), k), L17bSel(e.nums("r"), k).Select(v => L17bEz(v, 0.85)).ToArray(), L17bSel(e.nums("sb"), k), L17bEz(e.num("hold"), 0.8), wn));
	}; });
	static readonly int _k17bclap = DodgeKind("L17bclap", e => { double wn = L17bWn(e); return () =>
	{
		var k = L17bKeep(e, e.nums("cb").Length);
		hazards.Add(L17bClap(L17bSel(e.nums("x"), k), L17bSel(e.nums("y"), k), L17bSel(e.nums("a"), k).Select(v => v * Math.PI / 180).ToArray(), L17bSel(e.nums("hw"), k).Select(v => L17bEz(v, 0.8)).ToArray(), L17bSel(e.nums("cb"), k), wn));
	}; });
	static readonly int _k17bchobi = DodgeKind("L17bchobi", e => { double wn = L17bWn(e); return () =>
		hazards.Add(L17bChobi(e.flag("vert"), e.nums("sb"), e.nums("ps"), e.nums("hc"), L17bEz(e.num("hs"), 1.3), L17bEz(e.num("dr"), 0.85), e.num("sp"), L17bEz(e.num("lw"), 0.8), wn, e.num("to"))); });
	static readonly int _k17bfire = DodgeKind("L17bfire", e => { double wn = L17bWn(e); return () =>
	{
		var k = L17bKeep(e, e.nums("fb").Length);
		// Easy: fewer sparks (a shell keeps 3 in 5), a bit slower
		var n = L17bSel(e.nums("n"), k).Select(v => easy ? Math.Round(v * 0.6) : v).ToArray();
		hazards.Add(L17bFire(L17bSel(e.nums("lx"), k), L17bSel(e.nums("fx"), k), L17bSel(e.nums("fy"), k), L17bSel(e.nums("fb"), k), n, L17bSel(e.nums("a0"), k).Select(v => v * Math.PI / 180).ToArray(), L17bSel(e.nums("sg"), k),
			L17bSel(e.nums("dl"), k).Select((v, i) => easy && n[i] > 0 ? v * e.nums("n")[k[i]] / n[i] : v).ToArray(), L17bSel(e.nums("sp"), k).Select(v => L17bEz(v, ezV("ezsp", EZ_SP))).ToArray(), e.num("fu"), L17bEz(e.num("sr"), 0.85), e.num("rng", 1600), wn));
	}; });
	static readonly int _k17beq = DodgeKind("L17beq", e => { double wn = L17bWn(e); return () =>
		hazards.Add(L17bEq(e.flag("side"), e.i("nc"), e.nums("sb"), e.nums("c0"), e.nums("c1"), e.num("jag"), L17bEz(e.num("gh"), 1.3), easy ? new double[0] : e.nums("bb"), e.num("bump"), e.num("stg"), wn, B(e.num("hit")), B(e.num("to")))); });
	static readonly int _k17brecord = DodgeKind("L17brecord", e => { double wn = L17bWn(e); return () =>
	{
		double sl = easy ? 0.65 : 1;
		hazards.Add(L17bRecord(e.num("cx", L17bCX), e.num("cy", L17bCY), e.nums("re"), e.nums("w").Select(v => v * sl).ToArray(), e.nums("no"), e.nums("hh").Select(v => L17bEz(v, 1.3) * Math.PI / 180).ToArray(),
			e.nums("a0").Select(v => v * Math.PI / 180).ToArray(), e.nums("kb"), e.nums("km"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17bdance = DodgeKind("L17bdance", e => { double wn = L17bWn(e); return () =>
		hazards.Add(L17bDance(e.num("cx", L17bCX), e.num("cy", L17bCY), e.nums("sb"), e.nums("rr"), e.num("w") * (easy ? 0.75 : 1), e.num("a0") * Math.PI / 180, e.i("nh"), L17bEz(e.num("hh"), 1.3) * Math.PI / 180, e.i("nd"),
			L17bEz(e.num("dr"), 0.85), L17bEz(e.num("lw"), 0.8), wn, B(e.num("hit") - Math.Max(wn, 1)), B(e.num("hit")), B(e.num("to")))); });
	// Easy: the lights sweep slower and thinner
	static readonly int _k17blaser = DodgeKind("L17blaser", e => { double wn = L17bWn(e); return () =>
	{
		hazards.Add(L17bLamp(e.num("ox"), e.num("oy"), B(e.num("hit") - Math.Max(wn, 1)), B(e.num("hit")), B(e.num("to"))));
		hazards.Add(L17bLaser(e.num("ox"), e.num("oy"), e.nums("kb"), e.nums("ka").Select(v => v * Math.PI / 180).ToArray(), L17bEz(e.num("mv"), 1.6), L17bEz(e.num("bw"), 0.75), wn, B(e.num("hit") - Math.Max(wn, 1)), B(e.num("hit")), B(e.num("to"))));
	}; });
}
