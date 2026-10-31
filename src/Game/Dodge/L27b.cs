using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// المفروض (main level 27), part 2 "اللي صار": the heart breaks in two, the two roads, the clock runs backwards.
public static partial class G
{
	// ---------- the heart and its two halves ----------
	// The heart in units (about 32 wide, 29 high), the crack down its middle. Half -1 = the left half, 1 = the right; both end on the crack,
	// so the two fit together. A half is drawn and hit from the same points.
	static readonly double[] MFCRX = { 0, 2.5, -2, 2, -1.5, 0 }, MFCRY = { -5, -1, 3, 7, 11, 17 };
	static double[] _mfHL, _mfHR;
	static double[] MFHalfPts(int side)
	{
		if (side < 0 && _mfHL != null) return _mfHL; if (side > 0 && _mfHR != null) return _mfHR;
		var l = new List<double>(); const int N = 40;
		double hx(double t) => 16 * Math.Pow(JS.sin(t), 3);
		double hy(double t) => -(13 * JS.cos(t) - 5 * JS.cos(2 * t) - 2 * JS.cos(3 * t) - JS.cos(4 * t));
		if (side < 0)
		{
			for (int i = 0; i <= N; i++) { double t = Math.PI + Math.PI * i / N; l.Add(hx(t)); l.Add(hy(t)); }   // the tip, round the left, to the top
			for (int i = 1; i < MFCRX.Length - 1; i++) { l.Add(MFCRX[i]); l.Add(MFCRY[i]); }                      // down the crack
		}
		else
		{
			for (int i = 0; i <= N; i++) { double t = Math.PI * i / N; l.Add(hx(t)); l.Add(hy(t)); }              // the top, round the right, to the tip
			for (int i = MFCRX.Length - 2; i >= 1; i--) { l.Add(MFCRX[i]); l.Add(MFCRY[i]); }                     // up the crack
		}
		var a = l.ToArray(); if (side < 0) _mfHL = a; else _mfHR = a; return a;
	}
	static void MFHalfPath(int side, double x, double y, double s, double rot)
	{
		var P = MFHalfPts(side); double c = JS.cos(rot), sn = JS.sin(rot);
		g.beginPath();
		for (int i = 0; i < P.Length; i += 2) { double px = x + (c * P[i] - sn * P[i + 1]) * s, py = y + (sn * P[i] + c * P[i + 1]) * s; if (i == 0) g.moveTo(px, py); else g.lineTo(px, py); }
		g.closePath();
	}
	static bool MFHalfHit(int side, Player p, double x, double y, double s, double rot)
	{
		var P = MFHalfPts(side); double c = JS.cos(rot), sn = JS.sin(rot);
		double lx = (c * (p.x - x) + sn * (p.y - y)) / s, ly = (-sn * (p.x - x) + c * (p.y - y)) / s, m = (p.r - 2) / s;
		if (Math.Abs(lx) > 18 + m || ly < -14 - m || ly > 18 + m) return false;
		bool inside = false; int n = P.Length / 2;
		for (int i = 0, j = n - 1; i < n; j = i++)
		{
			double xi = P[2 * i], yi = P[2 * i + 1], xj = P[2 * j], yj = P[2 * j + 1];
			if ((yi > ly) != (yj > ly) && lx < (xj - xi) * (ly - yi) / (yj - yi) + xi) inside = !inside;
			if (m > 0 && MFSeg(lx, ly, xi, yi, xj, yj) < m) return true;
		}
		return inside;
	}
	public struct MFPose { public double x, y, rot; }
	// A half of the heart that goes where pose says; it shows as a white outline from b0 - 1 and hurts (pink) from b0 to b1.
	public static Hz MFHalf(int side, Func<double, MFPose> pose, double s, double b0, double b1)
	{
		var h = new Hz { kind = "MFHalf" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 0.4) return; var q = pose(Math.Min(bt, b1));
			g.save();
			if (bt < b0) { g.strokeStyle = MFW(0.2 + 0.6 * MFk(bt, b0 - 1, b0)); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); MFHalfPath(side, q.x, q.y, s, q.rot); g.stroke(); g.setLineDash(new double[0]); }
			else
			{
				double al = 1 - MFk(bt, b1, b1 + 0.4);
				g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = MFP(al); MFHalfPath(side, q.x, q.y, s, q.rot); g.fill();
				g.shadowBlur = 0; g.strokeStyle = $"rgba(255,190,210,{str(0.5 * al)})"; g.lineWidth = 2; g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1) return false; var q = pose(bt); return MFHalfHit(side, p, q.x, q.y, s, q.rot); };
		return h;
	}
	// The whole heart before it breaks: it beats in the middle (pink) from b0 to b1 and shows as an outline from b0 - 1.
	public static Hz MFHeart(double x, double y, double s, double b0, double b1)
	{
		var h = new Hz { kind = "MFHeart" };
		double sc(double bt) => s * (1 + 0.08 * (1 - MFk(bt - Math.Floor(bt), 0, 0.35)));
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt >= b1) return; double ss = sc(bt);
			g.save();
			if (bt < b0) { g.strokeStyle = MFW(0.2 + 0.6 * MFk(bt, b0 - 1, b0)); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); MFHalfPath(-1, x, y, s, 0); g.stroke(); MFHalfPath(1, x, y, s, 0); g.stroke(); g.setLineDash(new double[0]); }
			else { g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; MFHalfPath(-1, x, y, ss, 0); g.fill(); MFHalfPath(1, x, y, ss, 0); g.fill(); }
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1) return false; double ss = sc(bt); return MFHalfHit(-1, p, x, y, ss, 0) || MFHalfHit(1, p, x, y, ss, 0); };
		return h;
	}

	// ---------- tears shot in a straight line ----------
	// n tears from (x, y), spread degrees apart round the angle a (degrees, 0 = right, 90 = down), at sp px/s from bf; dashed from bf - tl.
	public static Hz MFFan(double x, double y, int n, double a, double spread, double bf, double tl, double sp, double r, double from = 0)
	{
		var h = new Hz { kind = "MFFan" }; double tf = B(bf);
		double ang(int k) => MFRad(a + (k - (n - 1) / 2.0) * spread);
		h.update = (t, dt) => { if (t > tf && from + (t - tf) * sp > 1550) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			for (int k = 0; k < n; k++)
			{
				double q = ang(k), dx = JS.cos(q), dy = JS.sin(q);
				if (bt < bf + 0.2) { double l = MFExit(x + dx * from, y + dy * from, dx, dy); double kk = MFk(bt, bf - tl, bf) * (1 - MFk(bt, bf, bf + 0.2)); MFDash(x + dx * from, y + dy * from, x + dx * (from + l), y + dy * (from + l), MFW(0.12 + 0.4 * kk), 2); }
				if (bt >= bf) { double d = from + (t - tf) * sp; MFGlow(10); MFTear(x + dx * d, y + dy * d, r, q); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = from + (t - tf) * sp;
			for (int k = 0; k < n; k++) { double q = ang(k), tx = x + JS.cos(q) * d, ty = y + JS.sin(q) * d; if (Math.Abs(p.x - tx) < 40 && Math.Abs(p.y - ty) < 40 && MFTearHit(p, tx, ty, r, q)) return true; }
			return false;
		};
		return h;
	}

	// ---------- a row of tears (the heart's rain, 1:09) ----------
	// A row of tears, one every 28 px (no way between them), with a gap gw wide at g. dir 1: across, from the top, falling; -1: across, from
	// the bottom, rising; 2: down the screen, from the left, going right; -2: from the right, going left. At sp px/s from bf. The edge it
	// comes from shows dashed, the gap left open, from bf - tl.
	public static Hz MFRain(int dir, double gp, double gw, double bf, double tl, double sp, double r)
	{
		var h = new Hz { kind = "MFRain" }; double tf = B(bf); bool vert = Math.Abs(dir) == 2; int sg = Math.Sign(dir);
		double len = vert ? H : W, far = vert ? W : H, p0 = sg > 0 ? -30 : far + 30, ang = vert ? (sg > 0 ? 0 : Math.PI) : (sg > 0 ? Math.PI / 2 : -Math.PI / 2);
		double pc(double t) => p0 + sg * sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { double c = pc(t); if (sg > 0 ? c > far + 60 : c < -60) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.3)
			{
				double q = MFk(bt, bf - tl, bf) * (1 - MFk(bt, bf, bf + 0.3)), e = sg > 0 ? 8 : far - 8, a0 = Math.Max(0, gp - gw / 2), a1 = Math.Min(len, gp + gw / 2);
				if (vert) { MFDash(e, 0, e, a0, MFW(0.2 + 0.5 * q), 3); MFDash(e, a1, e, len, MFW(0.2 + 0.5 * q), 3); }
				else { MFDash(0, e, a0, e, MFW(0.2 + 0.5 * q), 3); MFDash(a1, e, len, e, MFW(0.2 + 0.5 * q), 3); }
			}
			if (bt >= bf) { double c = pc(t); MFGlow(10); for (double u = 14; u < len; u += 28) { if (Math.Abs(u - gp) < gw / 2) continue; if (vert) MFTear(c, u, r, ang); else MFTear(u, c, r, ang); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double c = pc(t), pa = vert ? p.y : p.x, pb = vert ? p.x : p.y; if (Math.Abs(pb - c) > 3 * r + 10) return false;
			for (double u = 14; u < len; u += 28) { if (Math.Abs(u - gp) < gw / 2) continue; if (Math.Abs(pa - u) < 2 * r + 10 && (vert ? MFTearHit(p, c, u, r, ang) : MFTearHit(p, u, c, r, ang))) return true; }
			return false;
		};
		return h;
	}

	// ---------- 7. the two roads (1:24.7) ----------
	// Two pink roads come in from the right and run to the left at v px/s. Along the road (s = 0 at the right edge at b0) they start as one,
	// part, and cross every 4 beats; the lanes between and outside them are where the square lives. They pulse wider on each beat.
	// The roads' last part enters at bEnd. Hits use the same points the drawing uses (every 6 px).
	public static double MFRoadC(double s) => 360 + 165 * JS.sin(TAU * s / 1900 + 0.6) - 30;
	public static double MFRoadSep(double s, double sepA, double per = 900) => sepA * JS.sin(Math.PI * s / per) * Math.Min(1, s / 300 + 0.25);
	public static Hz MFRoads(double b0, double bEnd, double v, double hw, double sepA, double per = 900)
	{
		var h = new Hz { kind = "MFRoads" }; double t0 = B(b0), sEnd = v * (B(bEnd) - t0);
		double S(double t) => v * (t - t0);
		double yy(int i, double s) { double c = MFRoadC(s), d = MFRoadSep(s, sepA, per) / 2; return i == 0 ? c - d : c + d; }
		double w(double bt) => hw + 4 * (1 - MFk(bt - Math.Floor(bt), 0, 0.4));
		h.update = (t, dt) => { if (S(t) - sEnd > W + 60) h.dead = true; };
		// the screen x of the road point s at time t: x = W - (S - s)
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1.5) return; double St = S(t);
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < b0)
			{
				// where they come in: two white dashes at the right edge
				double q = MFk(bt, b0 - 1.5, b0); MFDash(W - 4, MFRoadC(0) - 50, W - 4, MFRoadC(0) + 50, MFW(0.2 + 0.6 * q), 4);
				g.restore(); return;
			}
			double xr = W - (St - Math.Min(St, sEnd)), xl = Math.Max(-20, W - St);   // the road's part on the screen: from xl (its head) to xr (its tail)
			double ww = w(bt);
			for (int i = 0; i < 2; i++)
			{
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 2 * ww; g.beginPath(); bool first = true;
				for (double x = xl; ; x += 6) { double xx = Math.Min(x, xr); double s = St - (W - xx); double y = yy(i, s); if (first) { g.moveTo(xx, y); first = false; } else g.lineTo(xx, y); if (xx >= xr) break; }
				g.stroke();
				g.shadowBlur = 0; g.strokeStyle = "rgba(255,200,215,0.55)"; g.lineWidth = 2; g.stroke();
			}
			// where the roads cross next: a white ring on the right edge 1 beat before a crossing comes in (looks only)
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; double St = S(t), xr = W - (St - Math.Min(St, sEnd)), xl = Math.Max(-20, W - St);
			if (p.x < xl - 30 || p.x > xr + 30) return false; double ww = w(bt), lim = ww + p.r - 2;
			double a0 = Math.Max(xl, xl + Math.Floor((p.x - 40 - xl) / 6) * 6), a1 = Math.Min(xr, p.x + 40);
			for (int i = 0; i < 2; i++)
			{
				double px0 = a0, py0 = yy(i, St - (W - px0));
				for (double x = a0 + 6; ; x += 6)
				{
					double xx = Math.Min(x, a1), py1 = yy(i, St - (W - xx));
					if (MFSeg(p.x, p.y, px0, py0, xx, py1) < lim) return true;
					px0 = xx; py0 = py1; if (xx >= a1) break;
				}
				if (a1 <= a0 && hypot(p.x - a0, p.y - py0) < lim) return true;
			}
			return false;
		};
		return h;
	}
	// A tear drops off a road at the screen x (straight down or up, dir) at bf; the road point under x at bf is where it starts. Dashed from bf - tl.
	public static Hz MFRoadDrop(double b0, double v, double sepA, double per, int road, double x, int dir, double bf, double tl, double sp, double r)
	{
		double s = v * (B(bf) - B(b0)) - (W - x), c = MFRoadC(s), d = MFRoadSep(s, sepA, per) / 2, y = road == 0 ? c - d : c + d;
		return MFFan(x, y, 1, dir > 0 ? 90 : -90, 0, bf, tl, sp, r, 16);
	}
}
