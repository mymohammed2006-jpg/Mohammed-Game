using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L16 "عين بعين" (Hussam Al-Rassam), the third design (v52, his words 10-28: build it again): the shanasheel of an old Baghdad house at night.
// Everything is the geometry of the house: the wooden lattice of its windows, the star-and-cross tiles of its courtyard, the arches of its
// arcade and the rosette under its dome.
// Part 1 the window: lattice slats rocking with the song, the whole window over the room (you live in its holes; it jumps half a hole on
// "ابدلك"), eight-pointed star rings with a gap, two round windows face to face ("عين بعين") turning around each other, the rosette.
// Part 2 the house: the courtyard floor (stars, then crosses, then only one star is safe), the arcades falling through the room, the
// traitor's dagger aimed at you, and the doors closing on you ("تحرم عليه") with one slit left.
// Part 3 the dome: its ribs turning, the round windows again, the rosette, the floor, the arcades from all sides, the star shots that go out
// and come back to the middle ("تالي تجيني"), and at the end the star rings closing in on the middle.
// Every hazard is a pure function of time once it exists (aimed ones also of where the square was when they locked), so hit(p, t) is right
// for any later t. The data is data/dodge_L16.json (made by tools/level16/gen.py). The second design (v49-v51) is L16.cs + data/dodge_L16_v51.json.
public static partial class G
{
	static double L16bK(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double L16bS(double k) => k * k * (3 - 2 * k);
	static string L16bG(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string L16bP(double a) => "rgba(255,42,109," + str(clamp(a, 0, 1)) + ")";
	static string L16bAu(double a) => "rgba(255,196,96," + str(clamp(a, 0, 1)) + ")";
	static double L16bM(Player p) => p.r - 2;   // how much of the square counts on top of an attack's own size
	static void L16bGlow(double b) { g.shadowColor = PINK; g.shadowBlur = b; }
	static double L16bSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double L16bMod(double a, double n) => a - n * Math.Floor(a / n);
	// a beat program: the sum of eased steps (beat, amount...) that each take d beats; col picks the amount
	static double L16bSteps(double[] st, int stride, int col, double b, double d)
	{
		double s = 0;
		for (int i = 0; i + stride - 1 < st.Length; i += stride) { double k = (b - st[i]) / d; if (k <= 0) continue; s += st[i + col] * L16bS(Math.Min(1, k)); }
		return s;
	}
	// a rate program (beat, rate...) from b0 to b: each rate holds from its beat to the next one's; col picks the rate
	static double L16bInt(double[] sp, int stride, int col, double b0, double b)
	{
		double a = 0;
		for (int i = 0; i + stride - 1 < sp.Length; i += stride)
		{
			double s0 = Math.Max(sp[i], b0), s1 = i + 2 * stride - 1 < sp.Length ? sp[i + stride] : 1e9; if (b <= s0) break;
			a += sp[i + col] * (Math.Min(b, s1) - s0);
		}
		return a;
	}
	// a pulse 0..1 on the beats in pl: up in 0.1 beat, down in 0.5
	static double L16bPulse(double[] pl, double b)
	{
		double v = 0;
		foreach (var q in pl) { double x = b - q; if (x < 0 || x > 0.6) continue; v = Math.Max(v, x < 0.1 ? x / 0.1 : 1 - (x - 0.1) / 0.5); }
		return v;
	}
	static bool L16bInPoly(double px, double py, IList<(double x, double y)> P, double m)
	{
		bool ins = false; int n = P.Count;
		for (int i = 0, j = n - 1; i < n; j = i++) { var a = P[i]; var c = P[j]; if ((a.y > py) != (c.y > py) && px < (c.x - a.x) * (py - a.y) / (c.y - a.y) + a.x) ins = !ins; }
		if (ins) return true; if (m <= 0) return false;
		for (int i = 0, j = n - 1; i < n; j = i++) if (L16bSeg(px, py, P[j].x, P[j].y, P[i].x, P[i].y) < m) return true;
		return false;
	}
	static void L16bPath(IList<(double x, double y)> P) { g.moveTo(P[0].x, P[0].y); for (int i = 1; i < P.Count; i++) g.lineTo(P[i].x, P[i].y); g.closePath(); }
	// the eight-pointed star (two squares, one turned 45 degrees): its 16 corners, tips at R (k even), the corners between at 0.7654 R
	static List<(double x, double y)> L16bStarPts(double x, double y, double R, double rot)
	{
		var P = new List<(double x, double y)>(16);
		for (int k = 0; k < 16; k++) { double a = rot + k * Math.PI / 8, r = k % 2 == 0 ? R : R * 0.76537; P.Add((x + r * JS.cos(a), y + r * JS.sin(a))); }
		return P;
	}
	// is (x, y) (from the star's middle, unturned) in the eight-pointed star whose squares have half-side A
	static bool L16bInStar(double x, double y, double A)
	{
		if (A <= 0) return false;
		if (Math.Abs(x) < A && Math.Abs(y) < A) return true;
		double q = A * 1.41421356; return Math.Abs(x + y) < q && Math.Abs(x - y) < q;
	}

	// ---------- the house (harmless) ----------
	// One hazard for the whole level, made first (and first again after a checkpoint); veil: every attack is drawn after it.
	// Part k runs from beat pb[k-1]: 1 the window at night (the moon, the wooden frames), 2 the courtyard (tiles, the arcade), 3 under the dome
	public static Hz L16bStage(double[] pb, double t1)
	{
		var h = new Hz { kind = "L16bStage", veil = true };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < pb.Length; k++)
			{
				double s0 = B(pb[k]), s1 = k + 1 < pb.Length ? B(pb[k + 1]) : t1;
				double a = Math.Min(1, Math.Min((t - s0) / 0.8 + (k == 0 ? 0 : 0.5), (s1 - t) / 0.8 + (k + 1 < pb.Length ? 0.5 : 0)));
				if (a > 0) Part(k + 1, a, t);
			}
		};
		void Frame(double x, double y, double w, double hh, double t)
		{
			// a shanasheel window: the wooden frame, a pointed top, a faint lattice inside
			g.strokeStyle = "rgba(120,78,46,0.55)"; g.lineWidth = 6; g.strokeRect(x, y, w, hh);
			g.beginPath(); g.moveTo(x, y); g.quadraticCurveTo(x + w * 0.1, y - hh * 0.18, x + w / 2, y - hh * 0.3); g.quadraticCurveTo(x + w * 0.9, y - hh * 0.18, x + w, y); g.stroke();
			g.save(); g.beginPath(); g.rect(x, y, w, hh); g.clip(); g.strokeStyle = L16bAu(0.07); g.lineWidth = 2; g.beginPath();
			for (double d = -hh; d < w + hh; d += 34) { g.moveTo(x + d, y); g.lineTo(x + d + hh, y + hh); g.moveTo(x + d + hh, y); g.lineTo(x + d, y + hh); }
			g.stroke(); g.restore();
		}
		void Part(int part, double a, double t)
		{
			g.save(); g.globalAlpha = a;
			g.fillStyle = part == 1 ? "rgba(10,8,26,0.78)" : part == 2 ? "rgba(8,16,24,0.78)" : "rgba(26,6,16,0.76)"; g.fillRect(0, 0, W, H);
			if (part == 1)
			{
				g.fillStyle = "rgba(200,190,230,0.35)";
				for (int i = 0; i < 40; i++) { double x = (i * 331.7) % W, y = (i * 157.3) % 300, tw = 0.5 + 0.5 * JS.sin(t * 1.3 + i); g.globalAlpha = a * (0.3 + 0.5 * tw); g.fillRect(x, y, 2, 2); }
				g.globalAlpha = a;
				g.fillStyle = "rgba(230,220,250,0.55)"; g.beginPath(); g.arc(640, 96, 40, 0, TAU); g.fill();
				g.fillStyle = "rgba(10,8,26,1)"; g.beginPath(); g.arc(658, 86, 37, 0, TAU); g.fill();
				Frame(40, 200, 220, 460, t); Frame(W - 260, 200, 220, 460, t);
			}
			else if (part == 2)
			{
				// the courtyard: the tiles' lines, the arcade at the top and at the bottom, the fountain
				g.strokeStyle = "rgba(70,110,130,0.16)"; g.lineWidth = 2; g.beginPath();
				for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
				for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
				g.fillStyle = "rgba(40,30,30,0.55)";
				for (int s = 0; s < 2; s++)
					for (double x = 0; x < W; x += 160)
					{
						g.beginPath(); double y0 = s == 0 ? 0 : H, d = s == 0 ? 1 : -1;
						g.moveTo(x, y0); g.lineTo(x + 160, y0); g.lineTo(x + 160, y0 + d * 60); g.lineTo(x + 140, y0 + d * 60);
						g.quadraticCurveTo(x + 136, y0 + d * 22, x + 80, y0 + d * 14); g.quadraticCurveTo(x + 24, y0 + d * 22, x + 20, y0 + d * 60); g.lineTo(x, y0 + d * 60); g.closePath(); g.fill();
					}
				g.strokeStyle = "rgba(90,150,170,0.22)"; g.lineWidth = 3;
				for (int i = 0; i < 3; i++) { double r = 60 + i * 40 + 30 * L16bMod(t * 0.6, 1); g.beginPath(); g.arc(640, 360, r, 0, TAU); g.stroke(); }
			}
			else
			{
				// under the dome: the tiers of muqarnas turning slowly, the gold dust
				g.strokeStyle = L16bAu(0.08); g.lineWidth = 2;
				for (int i = 1; i <= 6; i++)
				{
					double r = i * 120, rot = t * 0.04 * (i % 2 == 0 ? 1 : -1); var P = L16bStarPts(640, 360, r, rot);
					g.beginPath(); L16bPath(P); g.stroke();
				}
				g.beginPath(); for (int k = 0; k < 16; k++) { double an = k * Math.PI / 8 + t * 0.02; g.moveTo(640, 360); g.lineTo(640 + 900 * JS.cos(an), 360 + 900 * JS.sin(an)); } g.stroke();
				g.fillStyle = L16bAu(0.2);
				for (int i = 0; i < 50; i++) { double x = (i * 211.7) % W + JS.sin(t * 0.7 + i) * 20, y = H - (((i * 97.3) + t * (20 + i % 5 * 7)) % (H + 20)) + 10; g.fillRect(x, y, 2, 2); }
			}
			var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 780); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		}
		return h;
	}

	// ---------- the lattice ----------
	// fam 1: parallel slats s apart (th thick, up to th1 on the pulse beats pl), at angle a0 turning spin a beat; fam 2: a second set across
	// them (the window's holes). It drifts (dx, dy) a beat from (cx, cy), or turns around (cx, cy) at orad (oa0, ospin a beat). R > 0: a round
	// window of radius R. sp (beat, turn a beat) and dr (beat, dx, dy a beat), when given, change the turning and the drift from those beats on.
	// st (beat, du, dv): the lattice slides by du along a0 and dv across in 0.22 beat (its next place shows 1 beat before).
	// nw: no warning (it goes on from the one before it)
	public static Hz L16bLat(int fam, double s, double a0, double spin, double th0, double th1, double[] pl, double[] st, double cx, double cy, double dx, double dy,
		double R, double orad, double oa0, double ospin, double tw, double ta, double te, double[] sp = null, double[] dr = null)
	{
		var h = new Hz { kind = "L16bLat" }; double b0 = BT(ta); sp ??= Array.Empty<double>(); dr ??= Array.Empty<double>();
		(double x, double y, double a, double u, double v, double th) St(double t)
		{
			double b = BT(t), db = b - b0, x, y;
			if (orad > 0) { double oa = oa0 + ospin * db; x = cx + orad * JS.cos(oa); y = cy + orad * JS.sin(oa); }
			else { x = cx + (dr.Length > 0 ? L16bInt(dr, 3, 1, b0, b) : dx * db); y = cy + (dr.Length > 0 ? L16bInt(dr, 3, 2, b0, b) : dy * db); }
			return (x, y, a0 + (sp.Length > 0 ? L16bInt(sp, 2, 1, b0, b) : spin * db), L16bSteps(st, 3, 1, b, 0.22), L16bSteps(st, 3, 2, b, 0.22), th0 + (th1 - th0) * L16bPulse(pl, b));
		}
		void Lines((double x, double y, double a, double u, double v, double th) q, double w, string col)
		{
			double c = JS.cos(q.a), sn = JS.sin(q.a);
			g.strokeStyle = col; g.lineWidth = w; g.beginPath();
			for (int f = 0; f < fam; f++)
			{
				double ex = f == 0 ? c : -sn, ey = f == 0 ? sn : c, lx = -ey, ly = ex, o = f == 0 ? q.u : q.v, mn, mx;
				if (R > 0) { mn = -R; mx = R; }
				else
				{
					mn = 1e9; mx = -1e9;
					foreach (var (px, py) in new[] { (0.0, 0.0), (W, 0.0), (0.0, H), (W, H) }) { double pr = (px - q.x) * ex + (py - q.y) * ey; mn = Math.Min(mn, pr); mx = Math.Max(mx, pr); }
				}
				int k0 = (int)Math.Floor((mn - o) / s) - 1, k1 = (int)Math.Ceiling((mx - o) / s) + 1;
				for (int k = k0; k <= k1; k++)
				{
					double d = o + k * s, dn = Math.Max(0, Math.Abs(d) - w / 2), half = R > 0 ? Math.Sqrt(Math.Max(0, R * R - dn * dn)) : 2200; if (half <= 0) continue;
					double px = q.x + ex * d, py = q.y + ey * d; g.moveTo(px - lx * half, py - ly * half); g.lineTo(px + lx * half, py + ly * half);
				}
			}
			g.stroke();
		}
		void Clip((double x, double y, double a, double u, double v, double th) q) { if (R > 0) { g.beginPath(); g.arc(q.x, q.y, R, 0, TAU); g.clip(); } }
		void Rim((double x, double y, double a, double u, double v, double th) q, double al) { if (R <= 0) return; g.strokeStyle = L16bAu(al); g.lineWidth = 3; g.beginPath(); g.arc(q.x, q.y, R + 6, 0, TAU); g.stroke(); }
		h.update = (t, dt) => { if (t >= te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta) { var q0 = St(ta); double k = L16bK(t, tw, ta); g.save(); Clip(q0); Lines(q0, 2 + 2 * k, L16bG(0.12 + 0.3 * k)); g.restore(); Rim(q0, 0.3 * k); return; }
			var q = St(t); double b = BT(t);
			g.save(); Clip(q);
			for (int i = 0; i + 2 < st.Length; i += 3)
			{
				double sb = st[i]; if (b < sb - 1 || b >= sb) continue;
				double k = L16bK(b, sb - 1, sb); var q2 = St(B(sb + 0.24)); Lines(q2, 2 + 1.5 * k, L16bG(0.18 + 0.45 * k));
			}
			if (th1 > th0) Lines(q, th1, L16bP(0.2));
			L16bGlow(12); Lines(q, q.th, PINK);
			g.restore(); Rim(q, 0.55);
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			var q = St(t); double ddx = p.x - q.x, ddy = p.y - q.y, m = L16bM(p);
			if (R > 0 && hypot(ddx, ddy) > R + m) return false;
			double c = JS.cos(q.a), sn = JS.sin(q.a), u = ddx * c + ddy * sn - q.u, v = -ddx * sn + ddy * c - q.v, lim = q.th / 2 + m;
			if (Math.Abs(u - s * Math.Floor(u / s + 0.5)) < lim) return true;
			return fam == 2 && Math.Abs(v - s * Math.Floor(v / s + 0.5)) < lim;
		};
		return h;
	}

	// ---------- an eight-pointed star ring ----------
	// from (x, y), tips at R0 at ta, growing v px/s (v < 0: closing in), th thick, turned rot; gn of its 16 sides are missing around tip gi
	// (the way through). From tw: a gold line shows where the gap is, and the star where it will be
	public static Hz L16bStar(double x, double y, double R0, double v, double th, double rot, int gi, int gn, double tw, double ta)
	{
		var h = new Hz { kind = "L16bStar" };
		double R(double t) => R0 + v * (t - ta);
		var gap = new bool[16]; for (int j = 0; j < gn; j++) gap[(int)L16bMod(2 * gi - gn / 2 + j, 16)] = true;
		double ga = rot + gi * Math.PI / 4;
		h.update = (t, dt) => { double r = R(t); if (t > ta && (r > 1700 || r < 6)) h.dead = true; };
		void Sides(double r, double w, string col)
		{
			var P = L16bStarPts(x, y, r, rot); g.strokeStyle = col; g.lineWidth = w; g.lineCap = "round"; g.beginPath();
			for (int e = 0; e < 16; e++) { if (gap[e]) continue; var a = P[e]; var c = P[(e + 1) % 16]; g.moveTo(a.x, a.y); g.lineTo(c.x, c.y); }
			g.stroke(); g.lineCap = "butt";
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta), rw = v >= 0 ? Math.Max(R0, 90) : Math.Min(R0, 660);
				g.strokeStyle = L16bAu(0.15 + 0.35 * k); g.lineWidth = 3; g.beginPath(); g.moveTo(x + 30 * JS.cos(ga), y + 30 * JS.sin(ga)); g.lineTo(x + 1500 * JS.cos(ga), y + 1500 * JS.sin(ga)); g.stroke();
				g.setLineDash(new double[] { 14, 10 }); Sides(rw, 2, L16bG(0.15 + 0.4 * k)); g.setLineDash(new double[0]);
				return;
			}
			double r = R(t); if (r < 6) return;
			g.save(); L16bGlow(14); Sides(r, th, PINK); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double r = R(t), d = hypot(p.x - x, p.y - y), m = L16bM(p), lim = th / 2 + m; if (r < 6) return false;
			if (d > r + lim + 2 || d < r * 0.7654 * 0.92 - lim - 2) return false;
			var P = L16bStarPts(x, y, r, rot);
			for (int e = 0; e < 16; e++) { if (gap[e]) continue; var a = P[e]; var c = P[(e + 1) % 16]; if (L16bSeg(p.x, p.y, a.x, a.y, c.x, c.y) < lim) return true; }
			return false;
		};
		return h;
	}

	// ---------- the rosette ----------
	// n petals (kites: from the hub r0 out to R, pw wide) around (x, y), at a0 turning by the steps st (beat, angle) in 0.35 beat; R is rl,
	// reaching rh on the pulse beats pl; the hub hurts too. Its outlines show from tw, its next turn 1 beat before
	public static Hz L16bRose(double x, double y, int n, double r0, double rl, double rh, double pw, double a0, double[] st, double[] pl, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bRose" };
		double A(double t) => a0 + L16bSteps(st, 2, 1, BT(t), 0.35);
		double Rr(double t) { double k = L16bS(L16bK(t, ta, ta + 0.4 * SPB)); return r0 + (rl + (rh - rl) * L16bPulse(pl, BT(t)) - r0) * k; }
		List<(double x, double y)> Petal(double a, double R, double w)
		{
			double rm = r0 + (R - r0) * 0.4, c = JS.cos(a), s = JS.sin(a);
			return new List<(double x, double y)> { (x + r0 * c, y + r0 * s), (x + rm * c - w / 2 * s, y + rm * s + w / 2 * c), (x + R * c, y + R * s), (x + rm * c + w / 2 * s, y + rm * s - w / 2 * c) };
		}
		void All(double a, double R, bool fill, string col, double lw)
		{
			g.beginPath(); for (int k = 0; k < n; k++) L16bPath(Petal(a + k * TAU / n, R, pw));
			if (fill) { g.fillStyle = col; g.fill(); } else { g.strokeStyle = col; g.lineWidth = lw; g.stroke(); }
		}
		h.update = (t, dt) => { if (t >= te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta) { double k = L16bK(t, tw, ta); All(a0, rl, false, L16bG(0.15 + 0.45 * k), 2); g.strokeStyle = L16bG(0.15 + 0.45 * k); g.beginPath(); g.arc(x, y, r0 + 6, 0, TAU); g.stroke(); return; }
			double b = BT(t), a = A(t), R = Rr(t);
			for (int i = 0; i + 1 < st.Length; i += 2) { double sb = st[i]; if (b < sb - 1 || b >= sb) continue; double k = L16bK(b, sb - 1, sb); All(A(B(sb + 0.4)), rl, false, L16bG(0.15 + 0.4 * k), 2); }
			if (rh > rl) All(a, rh, true, L16bP(0.16), 0);
			g.save(); L16bGlow(14); All(a, R, true, PINK, 0); g.fillStyle = PINK; circle(x, y, r0 + 6); g.restore();
			g.fillStyle = "rgba(255,150,185,1)"; circle(x, y, r0 * 0.45);
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double m = L16bM(p), d = hypot(p.x - x, p.y - y), R = Rr(t);
			if (d < r0 + 6 + m) return true; if (d > R + m + 2) return false;
			double a = A(t);
			for (int k = 0; k < n; k++) if (L16bInPoly(p.x, p.y, Petal(a + k * TAU / n, R, pw), m)) return true;
			return false;
		};
		return h;
	}

	// ---------- star shots ----------
	// n small stars (r round) from (x, y) at off + k*TAU/n, v px/s from ta. back > 0: they stop at Rs, slide half a step round in back beats,
	// and come back to the middle on the new lines ("تالي تجيني"). Their lines show from tw (the way back from 0.6 beat before they turn)
	public static Hz L16bShot(double x, double y, int n, double off, double v, double r, double Rs, double back, double tw, double ta)
	{
		var h = new Hz { kind = "L16bShot" };
		double t1 = ta + Rs / v, t2 = t1 + back * SPB, te = back > 0 ? t2 + Rs / v : ta + 1700 / v, st = TAU / n;
		(double rr, double an) Pos(int i, double t)
		{
			if (back <= 0 || t < t1) return (v * (t - ta), off + i * st);
			if (t < t2) return (Rs, off + i * st + st / 2 * L16bS(L16bK(t, t1, t2)));
			return (Rs - v * (t - t2), off + (i + 0.5) * st);
		}
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.1 + 0.3 * k); g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < n; i++) { double an = off + i * st, e = back > 0 ? Rs : 1500; g.moveTo(x + 40 * JS.cos(an), y + 40 * JS.sin(an)); g.lineTo(x + e * JS.cos(an), y + e * JS.sin(an)); }
				g.stroke(); g.fillStyle = L16bP(0.3 + 0.5 * k); circle(x, y, 14 * k);
				return;
			}
			if (back > 0 && t > t1 - 0.6 * SPB && t < t2 + 0.3 * SPB)
			{
				double k = L16bK(t, t1 - 0.6 * SPB, t1); g.strokeStyle = L16bG(0.12 + 0.3 * k); g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < n; i++) { double an = off + (i + 0.5) * st; g.moveTo(x + 30 * JS.cos(an), y + 30 * JS.sin(an)); g.lineTo(x + Rs * JS.cos(an), y + Rs * JS.sin(an)); }
				g.stroke();
			}
			g.save(); L16bGlow(10); g.fillStyle = PINK; g.beginPath();
			for (int i = 0; i < n; i++) { var q = Pos(i, t); if (q.rr < 0) continue; double px = x + q.rr * JS.cos(q.an), py = y + q.rr * JS.sin(q.an); L16bPath(L16bStarPts(px, py, r * 1.4, q.an)); }
			g.fill(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double m = L16bM(p);
			for (int i = 0; i < n; i++) { var q = Pos(i, t); if (q.rr < 0) continue; if (hypot(p.x - x - q.rr * JS.cos(q.an), p.y - y - q.rr * JS.sin(q.an)) < r + m) return true; }
			return false;
		};
		return h;
	}

	// ---------- the courtyard floor ----------
	// eight-pointed stars on a grid c apart (0.32 c their half-side), the crosses between them; from (ox, oy) moving (vx, vy) a beat, turning spin.
	// pr (beat, kind, dur, si, sj): from beat for dur beats kind 0 = the stars hurt, 1 = the crosses hurt (all but the stars made
	// 1.7 e smaller), 2 = all but star (si, sj) hurt. Each shows grey wn beats before
	public static Hz L16bFloor(double c, double e, double ox, double oy, double vx, double vy, double spin, double[] pr, double wn, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bFloor", layer = -1 }; double b0 = BT(ta), A = 0.32 * c;
		(double x, double y, double a) O(double t) { double db = BT(t) - b0; return (ox + vx * db, oy + vy * db, spin * db); }
		int Cur(double b, double pre) { for (int i = 0; i + 4 < pr.Length; i += 5) if (b >= pr[i] - pre && b < pr[i] + pr[i + 2]) return i; return -1; }
		void Grid(double t, Action<double, double, int, int> f)
		{
			int K = (int)Math.Ceiling(820 / c) + 1;
			for (int i = -K; i <= K; i++) for (int j = -K; j <= K; j++) f(i * c, j * c, i, j);
		}
		void Region(double t, int kind, int si, int sj, string col)
		{
			var o = O(t); g.save(); g.translate(o.x, o.y); g.rotate(o.a); g.beginPath();
			if (kind == 0) Grid(t, (x, y, i, j) => L16bPath(L16bStarPts(x, y, (A - 2) * 1.41421356, 0)));
			else
			{
				g.rect(-1600, -1600, 3200, 3200);
				if (kind == 1) Grid(t, (x, y, i, j) => L16bPath(L16bStarPts(x, y, (A - 1.7 * e) * 1.41421356, 0)));
				else L16bPath(L16bStarPts(si * c, sj * c, (A - 1.7 * e) * 1.41421356, 0));
			}
			g.fillStyle = col; g.fill(kind == 0 ? "nonzero" : "evenodd"); g.restore();
		}
		h.update = (t, dt) => { if (t >= te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			double b = BT(t), al = Math.Min(L16bK(t, tw, ta), 1 - L16bK(t, te - 0.4 * SPB, te));
			{
				var o = O(t); g.save(); g.translate(o.x, o.y); g.rotate(o.a); g.strokeStyle = L16bAu(0.12 + 0.12 * al); g.lineWidth = 2; g.beginPath();
				Grid(t, (x, y, i, j) => L16bPath(L16bStarPts(x, y, A * 1.41421356, 0))); g.stroke(); g.restore();
			}
			for (int i = 0; i + 4 < pr.Length; i += 5)
			{
				double sb = pr[i]; int kind = (int)pr[i + 1], si = (int)pr[i + 3], sj = (int)pr[i + 4];
				if (b >= sb - wn && b < sb) Region(t, kind, si, sj, L16bG(0.04 + 0.12 * L16bK(b, sb - wn, sb)));
				if (kind == 2 && b >= sb - wn && b < sb + pr[i + 2])
				{
					var o = O(t); g.save(); g.translate(o.x, o.y); g.rotate(o.a); g.strokeStyle = L16bAu(0.9); g.lineWidth = 4; g.beginPath();
					L16bPath(L16bStarPts(si * c, sj * c, (A - 1.7 * e) * 1.41421356, 0)); g.stroke(); g.restore();
				}
			}
			int ci = Cur(b, 0); if (ci < 0 || b < BT(ta)) return;
			Region(t, (int)pr[ci + 1], (int)pr[ci + 3], (int)pr[ci + 4], L16bP(0.88));
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			int ci = Cur(BT(t), 0); if (ci < 0) return false;
			var o = O(t); double dx = p.x - o.x, dy = p.y - o.y, ca = JS.cos(-o.a), sa = JS.sin(-o.a), lx = dx * ca - dy * sa, ly = dx * sa + dy * ca;
			int gi = (int)Math.Floor(lx / c + 0.5), gj = (int)Math.Floor(ly / c + 0.5); double x = lx - gi * c, y = ly - gj * c, m = L16bM(p);
			int kind = (int)pr[ci + 1];
			if (kind == 0) return L16bInStar(x, y, A - 2 + m);
			if (kind == 1) return !L16bInStar(x, y, A - 1.7 * e - m);
			return !(gi == (int)pr[ci + 3] && gj == (int)pr[ci + 4] && L16bInStar(x, y, A - 1.7 * e - m));
		};
		return h;
	}

	// "هيجي جزاته": the whole floor burns but one star, the one nearest the square when it locks (si, sj, found by the kind); same floor as
	// L16bFloor (b0 = its first beat). The star shows gold from tw, the rest grey; from ta to te all else is pink
	public static Hz L16bOne(double c, double e, double ox, double oy, double vx, double vy, double spin, double b0, int si, int sj, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bOne", layer = -1 }; double A = 0.32 * c;
		(double x, double y, double a) O(double t) { double db = BT(t) - b0; return (ox + vx * db, oy + vy * db, spin * db); }
		void Draw(double t, string col)
		{
			var o = O(t); g.save(); g.translate(o.x, o.y); g.rotate(o.a); g.beginPath(); g.rect(-1600, -1600, 3200, 3200);
			L16bPath(L16bStarPts(si * c, sj * c, (A - 1.7 * e) * 1.41421356, 0)); g.fillStyle = col; g.fill("evenodd");
			g.strokeStyle = L16bAu(0.95); g.lineWidth = 4; g.beginPath(); L16bPath(L16bStarPts(si * c, sj * c, (A - 1.7 * e - 4) * 1.41421356, 0)); g.stroke(); g.restore();
		}
		h.update = (t, dt) => { if (t >= te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta) { Draw(t, L16bG(0.04 + 0.14 * L16bK(t, tw, ta))); return; }
			Draw(t, L16bP(0.88));
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			var o = O(t); double dx = p.x - o.x, dy = p.y - o.y, ca = JS.cos(-o.a), sa = JS.sin(-o.a), lx = dx * ca - dy * sa, ly = dx * sa + dy * ca;
			return !L16bInStar(lx - si * c, ly - sj * c, A - 1.7 * e - L16bM(p));
		};
		return h;
	}
	static (int i, int j) L16bNearStar(double c, double ox, double oy, double vx, double vy, double spin, double b0, double t, double px, double py)
	{
		double db = BT(t) - b0, x = ox + vx * db, y = oy + vy * db, a = spin * db, dx = px - x, dy = py - y, ca = JS.cos(-a), sa = JS.sin(-a);
		double lx = dx * ca - dy * sa, ly = dx * sa + dy * ca; return ((int)Math.Floor(lx / c + 0.5), (int)Math.Floor(ly / c + 0.5));
	}

	// ---------- the arcade ----------
	// a wall T thick crossing the room at v px/s from ta (dir 0 down from the top, 1 up, 2 right from the left, 3 left), with pointed arches
	// every P (the first at o): an arch is ow wide at its foot and 0.45 ow at its top. The arches' lanes show from tw
	public static Hz L16bArch(int dir, double T, double P, double o, double ow, double v, double tw, double ta)
	{
		var h = new Hz { kind = "L16bArch" };
		double Lt = dir < 2 ? H : W, Lc = dir < 2 ? W : H, te = ta + (Lt + T + 20) / v;
		double Pos(double t) => -T / 2 - 10 + v * (t - ta);
		(double x, double y) Map(double across, double along) => dir switch { 0 => (across, along), 1 => (across, H - along), 2 => (along, across), _ => (W - along, across) };
		double Hw(double f) => f < 0.55 * T ? ow / 2 : ow / 2 * (1 - 0.55 * L16bS(clamp((f - 0.55 * T) / (0.45 * T), 0, 1)));
		void Shape(double pos, string col)
		{
			double front = pos + T / 2; g.beginPath();
			var R = new List<(double x, double y)> { Map(-20, front), Map(Lc + 20, front), Map(Lc + 20, front - T), Map(-20, front - T) }; L16bPath(R);
			for (int k = -1; o + k * P < Lc + P; k++)
			{
				double cx = o + k * P + P / 2; var A = new List<(double x, double y)>();
				for (int s = 0; s <= 24; s++) { double f = T * s / 24.0; A.Add(Map(cx - Hw(f), front - f)); }
				for (int s = 24; s >= 0; s--) { double f = T * s / 24.0; A.Add(Map(cx + Hw(f), front - f)); }
				L16bPath(A);
			}
			g.fillStyle = col; g.fill("evenodd");
		}
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bAu(0.1 + 0.3 * k); g.lineWidth = 2; g.beginPath();
				for (int j = -1; o + j * P < Lc + P; j++) { double cx = o + j * P + P / 2; foreach (var sg in new[] { -1, 1 }) { var a = Map(cx + sg * ow / 2, 0); var bq = Map(cx + sg * ow / 2, Lt); g.moveTo(a.x, a.y); g.lineTo(bq.x, bq.y); } }
				g.stroke(); Shape(T / 2, L16bG(0.08 + 0.25 * k)); return;
			}
			g.save(); L16bGlow(12); Shape(Pos(t), PINK); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double along = dir == 0 ? p.y : dir == 1 ? H - p.y : dir == 2 ? p.x : W - p.x, across = dir < 2 ? p.x : p.y, m = L16bM(p);
			double f = Pos(t) + T / 2 - along; if (f < -m || f > T + m) return false;
			double rel = Math.Abs(L16bMod(across - o, P) - P / 2), ff = clamp(f, 0, T);
			double hw = Math.Min(Hw(ff), Hw(clamp(f - m, 0, T))); hw = Math.Min(hw, Hw(clamp(f + m, 0, T)));
			return !(rel < hw - m);
		};
		return h;
	}

	// ---------- the traitor's dagger ----------
	// a blade w wide through (px, py) along a (aimed at the square): its line shows from tw, it thrusts across at ta and stays hold beats
	public static Hz L16bDagger(double px, double py, double a, double w, double tw, double ta, double hold)
	{
		var h = new Hz { kind = "L16bDagger" }; double c = JS.cos(a), s = JS.sin(a), te = ta + hold * SPB, ext = 0.12 * SPB;
		double sx = px - c * 1500, sy = py - s * 1500;   // where it comes from (off the room)
		double Tip(double t) => 3000 * L16bS(L16bK(t, ta, ta + ext));
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.2 + 0.5 * k); g.lineWidth = 2 + 2 * k; g.beginPath(); g.moveTo(sx, sy); g.lineTo(px + c * 1500, py + s * 1500); g.stroke();
				g.strokeStyle = L16bG(0.08 + 0.12 * k); g.lineWidth = w * k; g.beginPath(); g.moveTo(sx, sy); g.lineTo(px + c * 1500, py + s * 1500); g.stroke();
				return;
			}
			double al = 1 - L16bK(t, te, te + 0.2); if (al <= 0) return;
			double tip = Tip(t), ex = sx + c * tip, ey = sy + s * tip, nx = -s * w / 2, ny = c * w / 2;
			g.save(); g.globalAlpha = al; L16bGlow(14); g.fillStyle = PINK; g.beginPath();
			g.moveTo(sx + nx, sy + ny); g.lineTo(ex - c * 70 + nx, ey - s * 70 + ny); g.lineTo(ex, ey); g.lineTo(ex - c * 70 - nx, ey - s * 70 - ny); g.lineTo(sx - nx, sy - ny); g.closePath(); g.fill();
			g.strokeStyle = "rgba(255,150,185,1)"; g.lineWidth = 3; g.beginPath(); g.moveTo(sx, sy); g.lineTo(ex - c * 30, ey - s * 30); g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double dx = p.x - sx, dy = p.y - sy, along = dx * c + dy * s, perp = Math.Abs(-dx * s + dy * c), m = L16bM(p), tip = Tip(t);
			if (along > tip + m) return false;
			double hw = along > tip - 70 ? w / 2 * Math.Max(0, (tip - along) / 70) : w / 2;
			return perp < hw + m;
		};
		return h;
	}

	// ---------- the doors ----------
	// two leaves close from the sides in cl beats from ta down to a slit sw wide at gx, stay hold beats and open again in cl beats.
	// The slit shows (gold) and the leaves (grey) from tw
	public static Hz L16bDoor(double gx, double sw, double tw, double ta, double cl, double hold)
	{
		var h = new Hz { kind = "L16bDoor" }; double tc = ta + cl * SPB, to = tc + hold * SPB, te = to + cl * SPB, l = gx - sw / 2, r = gx + sw / 2;
		double K(double t) => t < tc ? L16bS(L16bK(t, ta, tc)) : t < to ? 1 : 1 - L16bS(L16bK(t, to, te));
		void Leaves(double xl, double xr, string col, bool lines)
		{
			g.fillStyle = col; g.fillRect(-10, -10, xl + 10, H + 20); g.fillRect(xr, -10, W + 10 - xr, H + 20);
			if (!lines) return;
			g.strokeStyle = "rgba(255,150,185,1)"; g.lineWidth = 3; g.beginPath();
			foreach (var (x0, x1) in new[] { (0.0, xl), (xr, W) })
			{
				if (x1 - x0 < 30) continue;
				g.rect(x0 + 18, 30, x1 - x0 - 36, H / 2 - 45); g.rect(x0 + 18, H / 2 + 15, x1 - x0 - 36, H / 2 - 45);
			}
			g.stroke();
		}
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); Leaves(l, r, L16bG(0.05 + 0.15 * k), false);
				g.strokeStyle = L16bAu(0.4 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 16, 10 }); g.beginPath(); g.moveTo(l, 0); g.lineTo(l, H); g.moveTo(r, 0); g.lineTo(r, H); g.stroke(); g.setLineDash(new double[0]);
				return;
			}
			if (t >= te) return;
			double k2 = K(t); g.save(); L16bGlow(16); Leaves(l * k2, W - (W - r) * k2, PINK, true); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double k = K(t), m = L16bM(p); return p.x - m < l * k || p.x + m > W - (W - r) * k;
		};
		return h;
	}

	// ---------- the dome's ribs ----------
	// n ribs (th thick) curving out from (x, y) (twist radians a pixel), at a0, turning at the rates sp (beat, radians a beat, from that beat on);
	// they grow out from ta in 0.5 beat; their lines show from tw
	public static Hz L16bRibs(double x, double y, int n, double twist, double th, double a0, double[] sp, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bRibs" }; double b0 = BT(ta);
		double A(double t) => a0 + L16bInt(sp, 2, 1, b0, BT(t));
		double Rmax(double t) => 46 + 1500 * L16bS(L16bK(t, ta, ta + 0.5 * SPB));
		List<(double x, double y)> Rib(double a, double rm)
		{
			var P = new List<(double x, double y)>();
			for (double r = 46; ; r += 40) { double rr = Math.Min(r, rm), an = a + twist * rr; P.Add((x + rr * JS.cos(an), y + rr * JS.sin(an))); if (rr >= rm) break; }
			return P;
		}
		void Draw(double a, double rm, double w, string col)
		{
			g.strokeStyle = col; g.lineWidth = w; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath();
			for (int k = 0; k < n; k++) { var P = Rib(a + k * TAU / n, rm); g.moveTo(P[0].x, P[0].y); for (int i = 1; i < P.Count; i++) g.lineTo(P[i].x, P[i].y); }
			g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter";
		}
		h.update = (t, dt) => { if (t >= te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t >= te) return;
			if (t < ta) { double k = L16bK(t, tw, ta); Draw(a0, 1546, 2 + 2 * k, L16bG(0.12 + 0.4 * k)); return; }
			g.save(); L16bGlow(14); Draw(A(t), Rmax(t), th, PINK); g.fillStyle = PINK; circle(x, y, 50); g.restore();
			g.fillStyle = "rgba(255,150,185,1)"; circle(x, y, 20);
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double m = L16bM(p), d = hypot(p.x - x, p.y - y); if (d < 50 + m) return true;
			double rm = Rmax(t); if (d > rm + th / 2 + m) return false;
			double a = A(t), lim = th / 2 + m;
			for (int k = 0; k < n; k++)
			{
				// only the part of the rib near the square's distance
				double ak = a + k * TAU / n; double r0 = Math.Max(46, d - 80), r1 = Math.Min(rm, d + 80); if (r1 < r0) continue;
				double px0 = 0, py0 = 0; bool first = true;
				for (double r = r0; ; r += 16)
				{
					double rr = Math.Min(r, r1), an = ak + twist * rr, qx = x + rr * JS.cos(an), qy = y + rr * JS.sin(an);
					if (!first && L16bSeg(p.x, p.y, px0, py0, qx, qy) < lim) return true;
					px0 = qx; py0 = qy; first = false; if (rr >= r1) break;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L16.json) ----------
	// times are beats. Fixed attacks fire before their warning starts (the data's b), aimed ones ("rnd") lock on the square when they fire.
	static double L16bez(double v, double ez) => easy ? v * ez : v;
	static (double x, double y) L16bme() => (player?.x ?? W / 2, player?.y ?? H / 2);
	static readonly int _k16bfx = DodgeKind("L16bfx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L16bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k * 0.4); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k16bstage = DodgeKind("L16bstage", e => () => hazards.Add(L16bStage(e.nums("pb"), B(e.num("to")))));
	static double L16bWn(DodgeEv e, double d = 1) => e.num("wn", d) * (easy ? 1.3 : 1);
	static readonly int _k16blat = DodgeKind("L16blat", e => { double wn = L16bWn(e); return () =>
	{
		double hit = e.num("hit"), th0 = L16bez(e.num("th", 28), 0.72), th1 = e.has("th1") && !easy ? th0 + e.num("th1") - e.num("th", 28) : th0;   // Easy: no swelling
		bool nw = e.flag("nw");
		// Easy: the holes 12% bigger (and the jumps with them), the turning and drifting slower
		hazards.Add(L16bLat(e.i("fam", 2), L16bez(e.num("s", 200), 1.12), e.num("a0") * Math.PI / 180, L16bez(e.num("spin"), 0.6) * Math.PI / 180, th0, th1, e.nums("pl"),
			e.nums("st").Select((v, i) => i % 3 == 0 ? v : L16bez(v, 1.12)).ToArray(),
			e.num("cx", W / 2), e.num("cy", H / 2), L16bez(e.num("dx"), 0.6), L16bez(e.num("dy"), 0.6), e.num("R"), e.num("orad"), e.num("oa0") * Math.PI / 180,
			L16bez(e.num("ospin"), 0.6) * Math.PI / 180, nw ? B(hit) : B(hit - wn), B(hit), B(e.num("to")),
			e.nums("sp").Select((v, i) => i % 2 == 1 ? L16bez(v, 0.6) * Math.PI / 180 : v).ToArray(), e.nums("dr").Select((v, i) => i % 3 == 0 ? v : L16bez(v, 0.6)).ToArray()));
	}; });
	static readonly int _k16bstar = DodgeKind("L16bstar", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16bStar(e.num("x", W / 2), e.num("y", H / 2), e.num("R0", 30), L16bez(e.num("v", 430), 0.8), L16bez(e.num("th", 26), 0.8), e.num("rot") * Math.PI / 180,
			e.i("gi"), e.i("gn", 2) + (easy ? 2 : 0), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16brose = DodgeKind("L16brose", e => { double wn = L16bWn(e); return () =>
	{
		double hit = e.num("hit"), rl = e.num("rl", 430), rh = e.num("rh", rl);
		hazards.Add(L16bRose(e.num("x", W / 2), e.num("y", H / 2), e.i("n", 8), e.num("r0", 44), rl, rl + (rh - rl) * (easy ? 0.55 : 1), L16bez(e.num("pw", 120), 0.75),
			e.num("a0") * Math.PI / 180, e.nums("st").Select((v, i) => i % 2 == 1 ? v * Math.PI / 180 : v).ToArray(), e.nums("pl"), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k16bshot = DodgeKind("L16bshot", e => { double wn = L16bWn(e, 0.75); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"); int n = e.i("n", 16); if (easy) n = Math.Max(6, (int)Math.Round(n * 0.6));
		hazards.Add(L16bShot(e.num("x", W / 2), e.num("y", H / 2), n, e.num("off") * Math.PI / 180, L16bez(e.num("v", 380), 0.78), e.num("r", 12), e.num("Rs", 520), e.num("back"), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16bfloor = DodgeKind("L16bfloor", e => { double wn = L16bWn(e); return () =>
	{
		double hit = e.num("hit"); var pr = e.nums("pr");
		if (easy) { var q = new List<double>(); for (int i = 0; i + 4 < pr.Length; i += 5) if (pr[i + 2] >= 1 && pr[i + 1] == 0) q.AddRange(pr.Skip(i).Take(5)); pr = q.ToArray(); }   // Easy: only the stars burn, on the slow steps
		hazards.Add(L16bFloor(e.num("c", 280), L16bez(e.num("e", 26), 0.4), e.num("ox", W / 2), e.num("oy", H / 2), L16bez(e.num("vx"), 0.75), L16bez(e.num("vy"), 0.75),
			L16bez(e.num("spin"), 0.75) * Math.PI / 180, pr, L16bez(e.num("pwn", 1), 1.3), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k16bone = DodgeKind("L16bone", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16bme(); double tl = B(e.b), c = e.num("c", 280), ox = e.num("ox", W / 2), oy = e.num("oy", H / 2), vx = L16bez(e.num("vx"), 0.75), vy = L16bez(e.num("vy"), 0.75);
		double spin = L16bez(e.num("spin"), 0.75) * Math.PI / 180, fb = e.num("fb"), ta = tl + L16bez(e.num("wn", 2), 1.3) * SPB;
		var q = L16bNearStar(c, ox, oy, vx, vy, spin, fb, tl, p.x, p.y);
		hazards.Add(L16bOne(c, L16bez(e.num("e", 26), 0.4), ox, oy, vx, vy, spin, fb, q.i, q.j, tl, ta, ta + e.num("dur", 1.2) * SPB));
	});
	static readonly int _k16barch = DodgeKind("L16barch", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16bArch(e.i("dir"), e.num("T", 80), e.num("P", 320), e.num("o"), L16bez(e.num("ow", 150), 1.4), L16bez(e.num("v", 300), 0.8), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16bdagger = DodgeKind("L16bdagger", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16bme(); double tl = B(e.b);
		hazards.Add(L16bDagger(p.x, p.y, e.num("a") * Math.PI / 180, L16bez(e.num("w", 70), 0.8), tl, tl + L16bez(e.num("wn", 1), 1.45) * SPB, e.num("hold", 0.35)));
	});
	static readonly int _k16bdoor = DodgeKind("L16bdoor", e => { double wn = L16bWn(e, 1.3); return () =>
	{
		double hit = e.num("hit");
		hazards.Add(L16bDoor(e.num("gx"), L16bez(e.num("sw", 120), 1.6), B(hit - wn), B(hit), L16bez(e.num("cl", 0.35), 1.3), e.num("hold", 1)));
	}; });
	static readonly int _k16bribs = DodgeKind("L16bribs", e => { double wn = L16bWn(e); return () =>
	{
		double hit = e.num("hit");
		hazards.Add(L16bRibs(e.num("x", W / 2), e.num("y", H / 2), easy ? Math.Max(3, e.i("n", 6) - 2) : e.i("n", 6), e.num("tw", 0.0016), L16bez(e.num("th", 30), 0.8),
			e.num("a0") * Math.PI / 180, e.nums("sp").Select((v, i) => i % 2 == 1 ? L16bez(v, 0.6) * Math.PI / 180 : v).ToArray(), B(hit - wn), B(hit), B(e.num("to"))));
	}; });
}
