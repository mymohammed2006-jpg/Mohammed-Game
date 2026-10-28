using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F10 "Walk" (Kwabs), Ghassan's level, the second design (10-28): one long walk through the city at night, everything walks.
// part 1 the empty street (footprints that stamp on the beat and send out a ring, then the giant whose steps crack the street),
// part 2 the crowd (people walking in lanes, a step on every beat, and the buses that make you cross them; then the marching squares:
// you walk inside their holes, a step with them on every beat), part 3 the way home (the roundabout of people with the waves from the
// middle, the footsteps walking round you into the middle, and at the end the street turns pink: only your own steps are safe).
// Only pink hurts. Every hazard is a pure function of time once it exists.
// Drawing: the stage first, then every hazard's look and warnings, then every pink part, so nothing ever covers the pink.
// The data is data/dodge_F10.json (made by tools/friend10b/gen.py). The first design (F10.cs + data/dodge_F10_v51.json) stays for old replays.
public static partial class G
{
	static double F10bk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double F10be(double u) { u = clamp(u, 0, 1); return u * u * (3 - 2 * u); }
	static string F10bW(double a) => $"rgba(214,226,255,{str(clamp(a, 0, 1))})";   // the cold white of the look and the warnings
	static string F10bD(double a) => $"rgba(150,10,58,{str(clamp(a, 0, 1))})";     // the dark pink of the details inside a pink thing
	static double F10bmod(double a, double m) { double r = a % m; return r < 0 ? r + m : r; }
	static void F10bDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}

	// ---------- the layers ----------
	static readonly List<(Hz h, Action<double> d)> F10bSt = new(), F10bLo = new(), F10bHi = new();
	static void F10bClean(List<(Hz h, Action<double> d)> L) => L.RemoveAll(q => q.h.dead || !hazards.Contains(q.h));
	static readonly HashSet<Hz> F10bAll = new();
	static void F10bAdd(Hz h, Action<double> lo, Action<double> hi, bool stage = false)
	{
		if (lo != null) (stage ? F10bSt : F10bLo).Add((h, lo)); if (hi != null) F10bHi.Add((h, hi));
		F10bAll.RemoveWhere(q => q.dead); F10bAll.Add(h);
		// the first of this level's hazards in the list draws the stage and all the looks, the last one all the pink
		h.draw = t =>
		{
			Hz first = null, last = null; foreach (var q in hazards) if (!q.dead && F10bAll.Contains(q)) { first ??= q; last = q; }
			if (first == h)
			{
				F10bClean(F10bSt); F10bClean(F10bLo); F10bClean(F10bHi);
				foreach (var q in F10bSt) q.d(t);
				foreach (var q in F10bLo) q.d(t);
			}
			if (last == h) foreach (var q in F10bHi.ToArray()) q.d(t);
		};
		hazards.Add(h);
	}

	// ---------- the footprint: a sole and a heel, S long, pointing at angle a; sd = -1 the left foot, 1 the right ----------
	// (u, v) = a point in the print's own frame (u forward). The two parts are ellipses.
	static readonly double[] F10bPU = { 0.13, -0.29 }, F10bPA = { 0.27, 0.19 }, F10bPB = { 0.19, 0.155 };
	// the toes: (u, v for the right foot, radius), all times S; the big toe inside
	static readonly double[] F10bTU = { 0.47, 0.475, 0.455, 0.425, 0.385 }, F10bTV = { -0.115, -0.03, 0.04, 0.10, 0.15 }, F10bTR = { 0.075, 0.052, 0.047, 0.042, 0.037 };
	static (double u, double v) F10bLoc(double px, double py, double x, double y, double a, double sd)
	{
		double q = a + sd * 0.12, c = cos(q), s = sin(q), dx = px - x, dy = py - y; return (dx * c + dy * s, -dx * s + dy * c);
	}
	// the point is within e of the print (e < 0: at least -e inside it)
	static bool F10bIn(double px, double py, double x, double y, double a, double sd, double S, double e)
	{
		var (u, v) = F10bLoc(px, py, x, y, a, sd);
		for (int i = 0; i < 2; i++)
		{
			double ra = F10bPA[i] * S + e, rb = F10bPB[i] * S + e; if (ra <= 0 || rb <= 0) continue; double du = (u - F10bPU[i] * S) / ra, dv = v / rb;
			if (du * du + dv * dv < 1) return true;
		}
		for (int i = 0; i < 5; i++) { double r = F10bTR[i] * S + e; if (r > 0 && hypot(u - F10bTU[i] * S, v - sd * F10bTV[i] * S) < r) return true; }
		return false;
	}
	static void F10bPrintPath(double x, double y, double a, double sd, double S, double grow = 0)
	{
		double q = a + sd * 0.12, c = cos(q), s = sin(q);
		for (int i = 0; i < 2; i++)
		{
			double cx = x + F10bPU[i] * S * c, cy = y + F10bPU[i] * S * s;
			g.moveTo(cx + (F10bPA[i] * S + grow) * c, cy + (F10bPA[i] * S + grow) * s);
			g.ellipse(cx, cy, F10bPA[i] * S + grow, F10bPB[i] * S + grow, q, 0, TAU);
		}
		for (int i = 0; i < 5; i++)
		{
			double u = F10bTU[i] * S, v = sd * F10bTV[i] * S, tx = x + u * c - v * s, ty = y + u * s + v * c, r = F10bTR[i] * S + grow;
			g.moveTo(tx + r, ty); g.arc(tx, ty, r, 0, TAU);
		}
	}
	static void F10bPrintFill(double x, double y, double a, double sd, double S, string col)
	{
		g.fillStyle = col; g.beginPath(); F10bPrintPath(x, y, a, sd, S); g.fill();
	}
	static void F10bPrintLine(double x, double y, double a, double sd, double S, string col, double w = 2, bool dash = true)
	{
		g.strokeStyle = col; g.lineWidth = w; if (dash) g.setLineDash(new double[] { 8, 7 });
		double q = a + sd * 0.12, c = cos(q), s = sin(q);
		for (int i = 0; i < 2; i++) { double cx = x + F10bPU[i] * S * c, cy = y + F10bPU[i] * S * s; g.beginPath(); g.ellipse(cx, cy, F10bPA[i] * S, F10bPB[i] * S, q, 0, TAU); g.stroke(); }
		for (int i = 0; i < 5; i++) { double u = F10bTU[i] * S, v = sd * F10bTV[i] * S; g.beginPath(); g.arc(x + u * c - v * s, y + u * s + v * c, F10bTR[i] * S, 0, TAU); g.stroke(); }
		if (dash) g.setLineDash(new double[0]);
	}
	// the dark pink lines of the sole (only inside the pink print)
	static void F10bTread(double x, double y, double a, double sd, double S)
	{
		double q = a + sd * 0.12, c = cos(q), s = sin(q); g.strokeStyle = F10bD(0.55); g.lineWidth = Math.Max(2, S * 0.025); g.beginPath();
		for (int k = -2; k <= 3; k++)
		{
			double u = 0.13 * S + k * 0.07 * S, hw = 0.12 * S * Math.Sqrt(Math.Max(0, 1 - Math.Pow((u - 0.13 * S) / (0.27 * S), 2)));
			g.moveTo(x + u * c + hw * s, y + u * s - hw * c); g.lineTo(x + u * c - hw * s, y + u * s + hw * c);
		}
		g.stroke();
	}

	// ---------- the stage (harmless) ----------
	// 1 the empty street at night, 2 the busy street, 3 the square with the marching tiles, 4 the roundabout, 5 the way home
	public static Hz F10bStage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "F10bStage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		F10bAdd(h, t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; double bt = BT(t); g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var sk = g.createLinearGradient(0, 0, 0, H); sk.addColorStop(0, "#05070f"); sk.addColorStop(1, "#090d1b"); g.fillStyle = sk; g.fillRect(0, 0, W, H);
				// the big paving slabs, and the pools of the street lamps along the curbs
				g.strokeStyle = F10bW(0.04); g.lineWidth = 1; g.beginPath();
				for (double x = 0; x <= W; x += 160) { g.moveTo(x, 0); g.lineTo(x, H); }
				for (double y = 0; y <= H; y += 160) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
				for (int i = 0; i < 4; i++) foreach (var y in new[] { 0.0, H })
					{
						double x = 160 + i * 320 + (y > 0 ? 160 : 0); var gr = g.createRadialGradient(x, y, 6, x, y, 230); gr.addColorStop(0, "rgba(255,214,150,0.13)"); gr.addColorStop(1, "rgba(255,214,150,0)");
						g.fillStyle = gr; g.fillRect(x - 230, y - 230, 460, 460);
					}
			}
			else if (part == 2)
			{
				g.fillStyle = "#07080e"; g.fillRect(0, 0, W, H);
				// the pavements top and bottom, the road in the middle with its lane lines
				g.fillStyle = "rgba(120,130,170,0.07)"; g.fillRect(0, 0, W, 150); g.fillRect(0, H - 150, W, 150);
				g.fillStyle = F10bW(0.14); g.fillRect(0, 148, W, 3); g.fillRect(0, H - 151, W, 3);
				g.fillStyle = F10bW(0.05); for (double y = 210; y < H - 150; y += 60) for (double x = -((bt * 30) % 80); x < W; x += 80) g.fillRect(x, y - 1, 40, 2);
			}
			else if (part == 3)
			{
				g.fillStyle = "#06070c"; g.fillRect(0, 0, W, H);
				g.strokeStyle = F10bW(0.06); g.lineWidth = 1; g.beginPath();
				for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
				for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
				// the drum of the march: the floor pulses on each beat
				double p = 1 - F10bmod(bt, 1); g.fillStyle = F10bW(0.025 * p * p); g.fillRect(0, 0, W, H);
			}
			else if (part == 4)
			{
				g.fillStyle = "#05060b"; g.fillRect(0, 0, W, H);
				g.strokeStyle = F10bW(0.06); g.lineWidth = 2;
				for (double r = 90; r <= 750; r += 60) { g.beginPath(); g.arc(W / 2, H / 2, r, 0, TAU); g.stroke(); }
				g.fillStyle = F10bW(0.05); g.beginPath(); g.arc(W / 2, H / 2, 40, 0, TAU); g.fill();
				var gr = g.createRadialGradient(W / 2, H / 2, 240, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			else
			{
				g.fillStyle = "#05060a"; g.fillRect(0, 0, W, H);
				var gr = g.createRadialGradient(W / 2, -200, 20, W / 2, -200, 900); gr.addColorStop(0, "rgba(255,220,170,0.10)"); gr.addColorStop(1, "rgba(255,220,170,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			g.restore();
		}, null, true);
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F10bFx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F10bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		F10bAdd(h, null, null); return h;   // through F10bAdd: the first and the last hazard draw the layers
	}

	// ---------- the footsteps: a walk of prints ----------
	// print i lands at beat bl[i] (its dashed outline shows wn beats before) and stays pink st beats. With rr > 0 it sends a ring rw thick
	// out to rr px in rd beats (its reach shows as a dashed circle). With ck[i] = 1 the street cracks across the walk through the print
	// cko beats after it lands: a line cw thick to the screen's edges, pink cd beats (shown dashed cwn beats before).
	// With rg > 0 the ring has two holes rg wide (radians), in front of the foot and behind it (shown as dashed lines with its reach).
	public static Hz F10bTrail(double[] x, double[] y, double[] a, double[] sd, double[] bl, double[] ck, double S, double wn, double st,
		double rr, double rw, double rd, double rg, double cko, double cw, double cd, double cwn)
	{
		var h = new Hz { kind = "F10bTrail" }; int n = x.Length; double last = bl.Length > 0 ? bl.Max() : 0;
		double endB = last + Math.Max(st + 0.4, Math.Max(rr > 0 ? rd : 0, ck.Any(v => v > 0) ? cko + cd : 0)) + 0.2;
		const double GROW = 0.12, RL = 1600;
		double rad(double u) => rr * (1 - (1 - u) * (1 - u));
		void ring(int i, double r)
		{
			if (rg <= 0) { g.beginPath(); g.arc(x[i], y[i], r, 0, TAU); g.stroke(); return; }
			for (int s = 0; s < 2; s++) { double a0 = a[i] + s * Math.PI + rg / 2; g.beginPath(); g.arc(x[i], y[i], r, a0, a0 + Math.PI - rg); g.stroke(); }
		}
		bool hole(int i, double px, double py, double d, double e)
		{
			if (rg <= 0 || d < 1) return false; double an = atan2(py - y[i], px - x[i]), ae = e / d;
			for (int s = 0; s < 2; s++) { double dd = F10bmod(an - a[i] - s * Math.PI + Math.PI, TAU) - Math.PI; if (Math.Abs(dd) < rg / 2 - ae) return true; }
			return false;
		}
		void crackLine(int i, double bt, out double ux, out double uy, out double len)
		{
			double q = a[i] + Math.PI / 2; ux = cos(q); uy = sin(q); len = RL * F10bk(bt, bl[i] + cko, bl[i] + cko + GROW);
		}
		h.update = (t, dt) => { if (BT(t) > endB) h.dead = true; };
		F10bAdd(h, t =>
		{
			double bt = BT(t);
			for (int i = 0; i < n; i++)
			{
				if (bt >= bl[i] - wn && bt < bl[i])
				{
					double k = F10bk(bt, bl[i] - wn, bl[i]);
					g.fillStyle = F10bW(0.05 + 0.12 * k); g.beginPath(); F10bPrintPath(x[i], y[i], a[i], sd[i], S); g.fill();
					F10bPrintLine(x[i], y[i], a[i], sd[i], S, F10bW(0.3 + 0.6 * k));
					if (rr > 0)
					{
						g.strokeStyle = F10bW(0.1 + 0.3 * k); g.lineWidth = 2; g.setLineDash(new double[] { 6, 9 }); ring(i, rr);
						if (rg > 0)
						{
							g.strokeStyle = F10bW(0.15 + 0.45 * k); g.beginPath();
							for (int s = 0; s < 4; s++) { double an = a[i] + (s / 2) * Math.PI + (s % 2 == 0 ? -1 : 1) * rg / 2; g.moveTo(x[i] + S * 0.6 * cos(an), y[i] + S * 0.6 * sin(an)); g.lineTo(x[i] + rr * cos(an), y[i] + rr * sin(an)); }
							g.stroke();
						}
						g.setLineDash(new double[0]);
					}
				}
				// the dust where it lands (only a look)
				double u = (bt - bl[i]) / 0.6;
				if (u >= 0 && u < 1) { g.strokeStyle = F10bW(0.35 * (1 - u)); g.lineWidth = 3; g.beginPath(); g.arc(x[i], y[i], S * 0.45 + 50 * u, 0, TAU); g.stroke(); }
				// a print that lifts: its outline fades
				double lu = (bt - bl[i] - st) / 0.4;
				if (lu >= 0 && lu < 1) F10bPrintLine(x[i], y[i], a[i], sd[i], S, F10bW(0.4 * (1 - lu)));
				if (ck[i] > 0)
				{
					double c0 = bl[i] + cko; if (bt >= c0 - cwn && bt < c0)
					{
						double k = F10bk(bt, c0 - cwn, c0), q = a[i] + Math.PI / 2, cx = cos(q), cy = sin(q);
						F10bDash(x[i] - cx * RL, y[i] - cy * RL, x[i] + cx * RL, y[i] + cy * RL, F10bW(0.2 + 0.6 * k), 2 + 2 * k);
					}
				}
			}
		}, t =>
		{
			double bt = BT(t);
			// the cracks and rings under the prints
			for (int i = 0; i < n; i++)
			{
				if (ck[i] > 0 && bt >= bl[i] + cko && bt < bl[i] + cko + cd)
				{
					crackLine(i, bt, out var ux, out var uy, out var len);
					g.strokeStyle = PINK; g.lineWidth = cw; g.beginPath(); g.moveTo(x[i] - ux * len, y[i] - uy * len); g.lineTo(x[i] + ux * len, y[i] + uy * len); g.stroke();
				}
				if (rr > 0 && bt >= bl[i] && bt < bl[i] + rd)
				{
					double r = rad((bt - bl[i]) / rd); if (r > rw / 2) { g.strokeStyle = PINK; g.lineWidth = rw; ring(i, r); }
				}
			}
			for (int i = 0; i < n; i++)
			{
				if (bt < bl[i] || bt >= bl[i] + st) continue;
				F10bPrintFill(x[i], y[i], a[i], sd[i], S, PINK); F10bTread(x[i], y[i], a[i], sd[i], S);
			}
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t), e = p.r - 2;
			for (int i = 0; i < n; i++)
			{
				if (bt >= bl[i] && bt < bl[i] + st && F10bIn(p.x, p.y, x[i], y[i], a[i], sd[i], S, e)) return true;
				if (rr > 0 && bt >= bl[i] && bt < bl[i] + rd)
				{
					double r = rad((bt - bl[i]) / rd), d = hypot(p.x - x[i], p.y - y[i]); if (r > rw / 2 && Math.Abs(d - r) < rw / 2 + e && !hole(i, p.x, p.y, d, e)) return true;
				}
				if (ck[i] > 0 && bt >= bl[i] + cko && bt < bl[i] + cko + cd)
				{
					crackLine(i, bt, out var ux, out var uy, out var len); double dx = p.x - x[i], dy = p.y - y[i], along = dx * ux + dy * uy, off = -dx * uy + dy * ux;
					if (Math.Abs(off) < cw / 2 + e && Math.Abs(along) < len + e) return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- the crowd: people walking in lanes, a step on every beat ----------
	// lanes: lc[k] = the lane's middle (y; ring = true: its radius round the middle of the screen), ld[k] = its way (+1 / -1),
	// lp[k] = how long its pattern is (px along the lane; a ring: its whole round), the people of lane k are ofs[] from lo[k] (ln[k] of them).
	// Steps at sb0 + i * ev (i < sn), each st px in md beats; at each beat of fb all the ways turn round (arrows show it fw beats before).
	// A person is a pink disc rb wide. Grey from tIn - 1 s, pink from tIn to te.
	public static Hz F10bCrowd(bool ring, double[] lc, double[] ld, double[] lp, double[] lo, double[] ln, double[] ofs, double sb0, double ev, int sn, double st, double md,
		double[] fb, double fw, double rb, double tIn, double te)
	{
		var h = new Hz { kind = "F10bCrowd" }; int L = lc.Length;
		// the way of step i (+1, or -1 after an odd number of turns) and the steps done before step i
		var sg = new double[sn]; var cum = new double[sn + 1];
		for (int i = 0; i < sn; i++) { double b = sb0 + i * ev; int f = fb.Count(x => x <= b + 1e-6); sg[i] = f % 2 == 0 ? 1 : -1; cum[i + 1] = cum[i] + sg[i]; }
		double steps(double bt)
		{
			if (sn == 0 || bt < sb0) return 0; int i = Math.Min(sn - 1, (int)Math.Floor((bt - sb0) / ev));
			return cum[i] + sg[i] * F10be((bt - sb0 - i * ev) / md);
		}
		double cxm = W / 2, cym = H / 2;
		// each person's place: (x, y), calls f for every one on (or near) the screen
		void each(double bt, Action<double, double> f)
		{
			double s0 = steps(bt) * st;
			for (int k = 0; k < L; k++)
			{
				double s = s0 * ld[k], P = lp[k]; int a = (int)lo[k], m = (int)ln[k];
				if (ring)
				{
					double R = lc[k];
					for (int j = a; j < a + m; j++) { double q = (ofs[j] + s) / R; f(cxm + R * cos(q), cym + R * sin(q)); }
				}
				else
					for (int j = a; j < a + m; j++)
					{
						double x0 = F10bmod(ofs[j] + s, P);
						for (double x = x0 - P; x < W + rb + P; x += P) if (x > -rb - 4 && x < W + rb + 4) f(x, lc[k]);
					}
			}
		}
		// the head shows the way they walk (only a look, inside the pink)
		double way(double bt, int k) { double s = steps(bt + 0.05) - steps(bt - 0.05); return s >= 0 ? ld[k] : -ld[k]; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10bAdd(h, t =>
		{
			if (t < tIn - 1 || t > te) return; double bt = BT(t);
			if (t < tIn) { g.fillStyle = F10bW(0.15 + 0.45 * F10bk(t, tIn - 1, tIn)); g.beginPath(); each(bt, (x, y) => { g.moveTo(x + rb, y); g.arc(x, y, rb, 0, TAU); }); g.fill(); }
			// where the next turn round is coming: arrows on every lane, pointing the new way
			foreach (var f in fb)
			{
				if (bt < f - fw || bt >= f) continue; double k = F10bk(bt, f - fw, f); int sgn = fb.Count(x => x <= f + 1e-6) % 2 == 0 ? 1 : -1;
				g.strokeStyle = F10bW(0.35 + 0.55 * k); g.lineWidth = 4; g.beginPath();
				for (int l = 0; l < L; l++)
				{
					double d = ld[l] * sgn;
					if (ring) { double R = lc[l]; for (int q = 0; q < 4; q++) { double an = q * Math.PI / 2 + Math.PI / 4, ax = cxm + R * cos(an), ay = cym + R * sin(an), tx = -sin(an) * d, ty = cos(an) * d; g.moveTo(ax - tx * 14 + ty * 10, ay - ty * 14 - tx * 10); g.lineTo(ax + tx * 4, ay + ty * 4); g.lineTo(ax - tx * 14 - ty * 10, ay - ty * 14 + tx * 10); } }
					else foreach (var ax in new[] { 40.0, W - 40 }) { double ay = lc[l]; g.moveTo(ax - d * 14, ay - 10); g.lineTo(ax + d * 4, ay); g.lineTo(ax - d * 14, ay + 10); }
				}
				g.stroke();
			}
		}, t =>
		{
			if (t < tIn || t > te + 0.3) return; double bt = BT(t), a = 1 - F10bk(t, te, te + 0.3); if (a <= 0) return;
			g.save(); g.globalAlpha = a; g.fillStyle = PINK; g.beginPath(); each(bt, (x, y) => { g.moveTo(x + rb, y); g.arc(x, y, rb, 0, TAU); }); g.fill();
			// the heads, a little ahead in the way they walk
			double s0 = steps(bt) * st; g.fillStyle = F10bD(0.6); g.beginPath();
			for (int k = 0; k < L; k++)
			{
				double s = s0 * ld[k], P = lp[k], d = way(bt, k) * rb * 0.25; int a0 = (int)lo[k], m = (int)ln[k];
				if (ring) { double R = lc[k]; for (int j = a0; j < a0 + m; j++) { double q = (ofs[j] + s) / R, x = cxm + R * cos(q) - sin(q) * d, y = cym + R * sin(q) + cos(q) * d; g.moveTo(x + rb * 0.42, y); g.arc(x, y, rb * 0.42, 0, TAU); } }
				else for (int j = a0; j < a0 + m; j++) { double x0 = F10bmod(ofs[j] + s, P); for (double x = x0 - P; x < W + rb + P; x += P) if (x > -rb - 4 && x < W + rb + 4) { g.moveTo(x + d + rb * 0.42, lc[k]); g.arc(x + d, lc[k], rb * 0.42, 0, TAU); } }
			}
			g.fill(); g.restore();
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te) return false; double bt = BT(t), lim = rb + p.r - 2, l2 = lim * lim; bool hit = false;
			each(bt, (x, y) => { if (!hit) { double dx = p.x - x, dy = p.y - y; if (dx * dx + dy * dy < l2) hit = true; } });
			return hit;
		};
		return h;
	}

	// ---------- the bus: a long pink block that drives along a band (y0..y1) from xs to xe between b0 and b1 (front at the head) ----------
	// its headlights and the band's dashed edges show wn beats before
	public static Hz F10bBus(double y0, double y1, double xs, double xe, double len, double b0, double b1, double wn)
	{
		var h = new Hz { kind = "F10bBus" }; double dir = Math.Sign(xe - xs);
		double front(double bt) => xs + (xe - xs) * clamp((bt - b0) / (b1 - b0), 0, 1);
		h.update = (t, dt) => { if (BT(t) > b1 + 0.1) h.dead = true; };
		F10bAdd(h, t =>
		{
			double bt = BT(t); if (bt < b0 - wn || bt >= b1) return;
			double k = F10bk(bt, b0 - wn, b0);
			if (bt < b0) { g.fillStyle = F10bW(0.04 + 0.1 * k); g.fillRect(0, y0, W, y1 - y0); }
			F10bDash(0, y0 + 2, W, y0 + 2, F10bW(0.3 + 0.5 * k), 3); F10bDash(0, y1 - 2, W, y1 - 2, F10bW(0.3 + 0.5 * k), 3);
			// the headlights' beams ahead of it
			double fx = front(bt), sx = dir > 0 ? Math.Max(fx, 0) : Math.Min(fx, W), ex = dir > 0 ? W : 0;
			var gr = g.createLinearGradient(sx, 0, sx + dir * 700, 0); gr.addColorStop(0, "rgba(255,240,200,0.22)"); gr.addColorStop(1, "rgba(255,240,200,0)");
			g.fillStyle = gr; g.fillRect(Math.Min(sx, ex), y0 + 8, Math.Abs(ex - sx), y1 - y0 - 16);
		}, t =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return; double fx = front(bt), bx = fx - dir * len;
			double x0 = Math.Min(fx, bx), x1 = Math.Max(fx, bx); g.fillStyle = PINK; g.beginPath(); g.roundRect(x0, y0, x1 - x0, y1 - y0, 14); g.fill();
			g.fillStyle = F10bD(0.55); for (double x = x0 + 30; x < x1 - 40; x += 60) g.fillRect(x, y0 + 12, 34, y1 - y0 - 24);
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; double fx = front(bt), bx = fx - dir * len, e = p.r - 2;
			return p.y > y0 - e && p.y < y1 + e && p.x > Math.Min(fx, bx) - e && p.x < Math.Max(fx, bx) + e;
		};
		return h;
	}

	// ---------- the march: squares in rows and columns (mask rows, '#' = one, '.' = a hole) c px apart, top-left at (x0, y0) ----------
	// At each mb[k] all of them step (mx[k], my[k]) cells in md beats. The way of the next step shows as arrows on the front row wn beats before
	// (and the whole block's next place, dashed, when the way changes). Grey from tIn - 1 s, pink from tIn to te.
	public static Hz F10bMarch(string mask, double c, double x0, double y0, double[] mb, double[] mx, double[] my, double md, double wn, double sz, double tIn, double te)
	{
		var h = new Hz { kind = "F10bMarch" }; var rows = mask.Split('/'); int R = rows.Length, C = rows.Max(r => r.Length);
		var cells = new List<(int i, int j)>(); for (int j = 0; j < R; j++) for (int i = 0; i < rows[j].Length; i++) if (rows[j][i] == '#') cells.Add((i, j));
		int m = mb.Length;
		(double x, double y) off(double bt)
		{
			double ox = 0, oy = 0;
			for (int k = 0; k < m; k++) { if (bt <= mb[k]) break; double u = F10be((bt - mb[k]) / md); ox += mx[k] * u; oy += my[k] * u; }
			return (x0 + ox * c, y0 + oy * c);
		}
		double hs = sz / 2;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10bAdd(h, t =>
		{
			if (t < tIn - 1 || t > te) return; double bt = BT(t); var o = off(bt);
			if (t < tIn) { g.fillStyle = F10bW(0.15 + 0.45 * F10bk(t, tIn - 1, tIn)); foreach (var q in cells) g.fillRect(o.x + q.i * c + c / 2 - hs, o.y + q.j * c + c / 2 - hs, sz, sz); }
			for (int k = 0; k < m; k++)
			{
				if (bt < mb[k] - wn || bt >= mb[k]) continue; double kk = F10bk(bt, mb[k] - wn, mb[k]), dx = Math.Sign(mx[k]), dy = Math.Sign(my[k]);
				bool turn = k == 0 || Math.Sign(mx[k - 1]) != dx || Math.Sign(my[k - 1]) != dy;
				if (turn)
				{
					g.strokeStyle = F10bW(0.25 + 0.5 * kk); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 });
					foreach (var q in cells) g.strokeRect(o.x + (q.i + mx[k]) * c + c / 2 - hs, o.y + (q.j + my[k]) * c + c / 2 - hs, sz, sz);
					g.setLineDash(new double[0]);
				}
				// an arrow on each square of the front, the way of the step
				g.strokeStyle = F10bW(0.4 + 0.5 * kk); g.lineWidth = 3; g.beginPath();
				foreach (var q in cells)
				{
					int ni = q.i + (int)dx, nj = q.j + (int)dy; bool front = !(nj >= 0 && nj < R && ni >= 0 && ni < rows[nj].Length && rows[nj][ni] == '#'); if (!front) continue;
					double ax = o.x + q.i * c + c / 2 + dx * (hs + 12), ay = o.y + q.j * c + c / 2 + dy * (hs + 12);
					g.moveTo(ax - dx * 10 - dy * 9, ay - dy * 10 - dx * 9); g.lineTo(ax, ay); g.lineTo(ax - dx * 10 + dy * 9, ay - dy * 10 + dx * 9);
				}
				g.stroke();
			}
		}, t =>
		{
			if (t < tIn || t > te + 0.3) return; double a = 1 - F10bk(t, te, te + 0.3); if (a <= 0) return; var o = off(BT(t));
			g.save(); g.globalAlpha = a; g.fillStyle = PINK; g.beginPath();
			foreach (var q in cells) { double x = o.x + q.i * c + c / 2 - hs, y = o.y + q.j * c + c / 2 - hs; if (x > W || y > H || x + sz < 0 || y + sz < 0) continue; g.roundRect(x, y, sz, sz, sz * 0.18); }
			g.fill(); g.fillStyle = F10bD(0.6); g.beginPath();
			foreach (var q in cells) { double x = o.x + q.i * c + c / 2, y = o.y + q.j * c + c / 2; if (x > W + c || y > H + c || x < -c || y < -c) continue; g.moveTo(x + sz * 0.2, y); g.arc(x, y, sz * 0.2, 0, TAU); }
			g.fill(); g.restore();
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te) return false; var o = off(BT(t)); double e = p.r - 2;
			double fi = (p.x - o.x) / c, fj = (p.y - o.y) / c; int i0 = (int)Math.Floor(fi), j0 = (int)Math.Floor(fj);
			for (int j = j0 - 1; j <= j0 + 1; j++)
				for (int i = i0 - 1; i <= i0 + 1; i++)
				{
					if (j < 0 || j >= R || i < 0 || i >= rows[j].Length || rows[j][i] != '#') continue;
					double cx = o.x + i * c + c / 2, cy = o.y + j * c + c / 2;
					if (Math.Abs(p.x - cx) < hs + e && Math.Abs(p.y - cy) < hs + e) return true;
				}
			return false;
		};
		return h;
	}

	// ---------- the wave: a pink ring out of (cx, cy) from beat b0, v px a beat, w thick, with holes: cones ga[k] (radians) gw wide ----------
	// the holes show as dashed lines and the middle beats wn beats before
	public static Hz F10bWave(double cx, double cy, double b0, double v, double w, double[] ga, double gw, double wn)
	{
		var h = new Hz { kind = "F10bWave" }; const double RM = 800;
		double rad(double bt) => (bt - b0) * v;
		h.update = (t, dt) => { if (rad(BT(t)) > RM) h.dead = true; };
		F10bAdd(h, t =>
		{
			double bt = BT(t); if (bt < b0 - wn) return; double k = F10bk(bt, b0 - wn, b0);
			if (bt < b0) { double p = 1 - F10bmod(bt, 1); g.strokeStyle = F10bW(0.3 + 0.5 * k); g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, 30 + 30 * p, 0, TAU); g.stroke(); }
			double r0 = Math.Max(0, rad(bt)); string col = F10bW(bt < b0 ? 0.25 + 0.5 * k : 0.45);
			foreach (var q in ga)
				foreach (var s in new[] { -1.0, 1.0 })
				{
					double an = q + s * gw / 2; F10bDash(cx + r0 * cos(an), cy + r0 * sin(an), cx + RM * cos(an), cy + RM * sin(an), col, 2);
				}
		}, t =>
		{
			double bt = BT(t); if (bt < b0) return; double r = rad(bt); if (r < w / 2) return;
			g.strokeStyle = PINK; g.lineWidth = w;
			// the ring in pieces between the holes
			var cuts = new List<(double a0, double a1)>(); foreach (var q in ga) cuts.Add((q - gw / 2, q + gw / 2));
			cuts.Sort((p1, p2) => p1.a0.CompareTo(p2.a0));
			if (cuts.Count == 0) { g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.stroke(); return; }
			for (int i = 0; i < cuts.Count; i++)
			{
				double a0 = cuts[i].a1, a1 = i + 1 < cuts.Count ? cuts[i + 1].a0 : cuts[0].a0 + TAU; if (a1 <= a0) continue;
				g.beginPath(); g.arc(cx, cy, r, a0, a1); g.stroke();
			}
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; double r = rad(bt), d = hypot(p.x - cx, p.y - cy), e = p.r - 2; if (r < w / 2 || Math.Abs(d - r) > w / 2 + e) return false;
			// in a hole only when the whole player is inside the cone
			double an = atan2(p.y - cy, p.x - cx), ae = d > 1 ? e / d : Math.PI;
			foreach (var q in ga) { double dd = F10bmod(an - q + Math.PI, TAU) - Math.PI; if (Math.Abs(dd) < gw / 2 - ae) return false; }
			return true;
		};
		return h;
	}

	// ---------- the way home: the street turns pink, only your steps are safe ----------
	// the floor greys in from fon - fw and is pink from fon to te. Print i (x, y, a, sd, S) is safe from bs[i] to be[i]; it shows dashed
	// wn beats before, and its outline shrinks the last beat before it goes. At first the middle of the street is still dark: a circle
	// cr0 wide at fon that closes to nothing at cb1 (where it will be a beat later shows dashed).
	public static Hz F10bFloor(double[] x, double[] y, double[] a, double[] sd, double[] bs, double[] be, double S, double wn, double fon, double fw, double cr0, double cb1, double te)
	{
		var h = new Hz { kind = "F10bFloor" }; int n = x.Length;
		double rad(double bt) => bt >= cb1 ? 0 : cr0 * (1 - F10be((bt - fon) / (cb1 - fon)));
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10bAdd(h, t =>
		{
			double bt = BT(t); if (bt >= fon) return;
			if (bt >= fon - fw)
			{
				// the street greys over (it will be pink), the steps stay dark
				double k = F10bk(bt, fon - fw, fon); g.fillStyle = F10bW(0.06 + 0.16 * k); g.fillRect(0, 0, W, H);
				g.strokeStyle = F10bW(0.4 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.strokeRect(4, 4, W - 8, H - 8);
				g.beginPath(); g.arc(W / 2, H / 2, cr0, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
			}
			for (int i = 0; i < n; i++)
			{
				if (bt < bs[i] - wn || bt >= be[i]) continue; double k = F10bk(bt, bs[i] - wn, bs[i]);
				F10bPrintFill(x[i], y[i], a[i], sd[i], S, "#0b0d16"); F10bPrintLine(x[i], y[i], a[i], sd[i], S, F10bW(0.4 + 0.6 * k), 3, bt < bs[i]);
			}
		}, t =>
		{
			double bt = BT(t); if (bt < fon || t > te) return; double r = rad(bt);
			g.fillStyle = PINK; g.beginPath(); g.rect(-20, -20, W + 40, H + 40); if (r > 0) { g.moveTo(W / 2 + r, H / 2); g.arc(W / 2, H / 2, r, 0, TAU); } g.fill("evenodd");
			if (r > 0) { double r1 = rad(bt + 1); g.strokeStyle = F10bW(0.6); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.arc(W / 2, H / 2, Math.Max(1, r1), 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			// the safe steps: the street shows through (one dark shape); a new one lights its edge, one about to go shows it dashed
			g.fillStyle = "#0b0d16"; g.beginPath(); for (int i = 0; i < n; i++) if (bt >= bs[i] && bt < be[i]) F10bPrintPath(x[i], y[i], a[i], sd[i], S); g.fill();
			for (int i = 0; i < n; i++)
			{
				if (bt < bs[i] || bt >= be[i]) continue;
				double kn = 1 - F10bk(bt, bs[i], bs[i] + 0.6), k = F10bk(bt, be[i] - 1, be[i]);
				if (kn > 0) F10bPrintLine(x[i], y[i], a[i], sd[i], S, F10bW(0.8 * kn), 3, false);
				if (k > 0) F10bPrintLine(x[i], y[i], a[i], sd[i], S * (1 - 0.1 * k), F10bW(0.5 + 0.5 * k), 3, true);
			}
			// the next steps: only dashed lines on the pink (the pink stays bright pink until they are safe)
			for (int i = 0; i < n; i++)
			{
				if (bt < bs[i] - wn || bt >= bs[i]) continue; double k = F10bk(bt, bs[i] - wn, bs[i]);
				F10bPrintLine(x[i], y[i], a[i], sd[i], S, F10bW(0.5 + 0.5 * k), 2 + 2 * k);
			}
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < fon || t > te) return false; double e = p.r - 2;
			if (hypot(p.x - W / 2, p.y - H / 2) < rad(bt) - e) return false;
			for (int i = 0; i < n; i++) if (bt >= bs[i] && bt < be[i] && F10bIn(p.x, p.y, x[i], y[i], a[i], sd[i], S, -e)) return false;
			return true;
		};
		return h;
	}

	// ---------- the kinds ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F10bwn(DodgeEv e, string k = "wn") => e.num(k) * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F10bez(double v, double ez) => easy ? v * ez : v;
	static bool F10bskip(DodgeEv e) => easy && e.flag("ezskip");

	static readonly int _kf10bstage = DodgeKind("F10bstage", e => () => F10bStage(e.i("part"), B(e.b), B(e.num("to"))));
	static readonly int _kf10bfx = DodgeKind("F10bfx", e => () => F10bFx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2))));
	static readonly int _kf10btrail = DodgeKind("F10btrail", e => { double wn = F10bwn(e), cwn = F10bwn(e, "cwn"); return () =>
	{
		if (F10bskip(e)) return;
		var ck = e.nums("ck"); if (easy && e.flag("ezck")) ck = new double[ck.Length];
		F10bTrail(e.nums("x"), e.nums("y"), e.nums("a"), e.nums("sd"), e.nums("bl"), ck, F10bez(e.num("S"), 0.85), wn, e.num("st"),
			F10bez(e.num("rr"), 0.8), F10bez(e.num("rw"), 0.75), e.num("rd"), F10bez(e.num("rg"), 1.3), e.num("cko"), F10bez(e.num("cw"), 0.75), e.num("cd"), cwn);
	}; });
	static readonly int _kf10bcrowd = DodgeKind("F10bcrowd", e => { double fw = F10bwn(e, "fw"); return () =>
	{
		F10bCrowd(e.flag("ring"), e.nums("lc"), e.nums("ld"), e.nums("lp"), e.nums("lo"), e.nums("ln"), e.nums("ofs"), e.num("sb0"), e.num("ev", 1), e.i("sn"),
			F10bez(e.num("st"), 0.85), e.num("md") * (easy ? 1.2 : 1), e.nums("fb"), fw, F10bez(e.num("rb"), 0.8), B(e.num("tin")), B(e.num("to")));
	}; });
	static readonly int _kf10bbus = DodgeKind("F10bbus", e => { double wn = F10bwn(e); return () =>
	{
		if (F10bskip(e)) return;
		double y0 = e.num("y0"), y1 = e.num("y1"); if (easy) { double m = (y0 + y1) / 2, hh = (y1 - y0) / 2 * 0.85; y0 = m - hh; y1 = m + hh; }
		double b0 = e.num("b0"), b1 = e.num("b1"); if (easy) b1 = b0 + (b1 - b0) / ezV("ezsp", EZ_SP);
		F10bBus(y0, y1, e.num("xs"), e.num("xe"), e.num("len"), b0, b1, wn);
	}; });
	static readonly int _kf10bmarch = DodgeKind("F10bmarch", e => { double wn = F10bwn(e); return () =>
	{
		F10bMarch(e.str("mask"), e.num("c"), e.num("x0"), e.num("y0"), e.nums("mb"), e.nums("mx"), e.nums("my"), e.num("md") * (easy ? 1.15 : 1), wn,
			F10bez(e.num("sz"), 0.82), B(e.num("tin")), B(e.num("to")));
	}; });
	static readonly int _kf10bwave = DodgeKind("F10bwave", e => { double wn = F10bwn(e); return () =>
	{
		if (F10bskip(e)) return;
		F10bWave(e.num("cx", W / 2), e.num("cy", H / 2), e.num("b0"), F10bez(e.num("v"), ezV("ezsp", EZ_SP)), F10bez(e.num("w"), 0.75), e.nums("ga"), F10bez(e.num("gw"), 1.3), wn);
	}; });
	static readonly int _kf10bfloor = DodgeKind("F10bfloor", e => { double wn = F10bwn(e); return () =>
	{
		var bs = e.nums("bs"); var be = e.nums("be");
		if (easy) { bs = bs.Select(v => v - 0.5).ToArray(); be = be.Select(v => v + 0.5).ToArray(); }
		F10bFloor(e.nums("x"), e.nums("y"), e.nums("a"), e.nums("sd"), bs, be, F10bez(e.num("S"), 1.15), wn, e.num("fon"), e.num("fw"), e.num("cr0"), e.num("cb1") + (easy ? 1 : 0), B(e.num("to")));
	}; });
}
