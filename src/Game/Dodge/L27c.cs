using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// المفروض (main level 27), part 3 "المفروض والواقع": the dream and the real, the thread, the two halves that go round, the tears that stop, the clock stops.
public static partial class G
{
	// ---------- 9. the dream and the real (2:09.7) ----------
	// The dream (white, harmless) does it first; D beats later the real one (pink) does the very same.
	// kind 0: a bar across falls from the top in dur beats, gap (a = gap x, b = gap width). kind 1: a bar down the screen goes from right to
	// left (a = gap y, b = gap height). kind 2: a ring grows from (a, b) with a gap at angle c (degrees, d wide). kind 3: a line across the
	// screen through (a, b) at angle c (degrees), there for dur beats.
	public static Hz MFEcho(int kind, double a, double b, double c, double d, double bf, double D, double dur)
	{
		var h = new Hz { kind = "MFEcho" }; const double TH = 17;
		h.update = (t, dt) => { if (BT(t) > bf + dur + 0.5) h.dead = true; };
		double pos(double u) => kind == 0 ? -30 + (H + 60) * u / dur : kind == 1 ? W + 30 - (W + 60) * u / dur : 30 + 900 * u / dur;
		void shape(double u, bool real)
		{
			if (kind == 0) { double y = pos(u); g.fillRect(-10, y - TH, a - b / 2 + 10, 2 * TH); g.fillRect(a + b / 2, y - TH, W + 10 - a - b / 2, 2 * TH); }
			else if (kind == 1) { double x = pos(u); g.fillRect(x - TH, -10, 2 * TH, a - b / 2 + 10); g.fillRect(x - TH, a + b / 2, 2 * TH, H + 10 - a - b / 2); }
			else if (kind == 2) { double r = pos(u), g0 = MFRad(c + d / 2), g1 = MFRad(c - d / 2 + 360); g.lineWidth = 2 * TH * 0.8; g.beginPath(); g.arc(a, b, r, g0, g1); g.stroke(); }
			else { double q = MFRad(c), dx = JS.cos(q) * 1600, dy = JS.sin(q) * 1600, k = MFOut(u / 0.12); g.lineWidth = 2 * TH * 0.75 * (real ? k : 1); g.beginPath(); g.moveTo(a - dx, b - dy); g.lineTo(a + dx, b + dy); g.stroke(); }
		}
		h.draw = t =>
		{
			double bt = BT(t); double ug = bt - (bf - D), ur = bt - bf;
			g.save(); g.lineCap = "butt";
			if (ug >= 0 && (kind == 3 ? ur < 0 : ug <= dur))
			{
				double ga = MFk(ug, 0, 0.2) * 0.42; g.fillStyle = MFW(ga); g.strokeStyle = MFW(ga); g.shadowColor = "rgba(214,226,255,0.8)"; g.shadowBlur = 10; shape(kind == 3 ? 0 : ug, false);
			}
			if (ur >= 0 && ur <= dur) { MFGlow(14); shape(ur, true); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double u = BT(t) - bf; if (u < 0 || u > dur) return false; double m = p.r - 2;
			if (kind == 0) { double y = pos(u); return Math.Abs(p.y - y) < TH + m && Math.Abs(p.x - a) > b / 2 - m; }
			if (kind == 1) { double x = pos(u); return Math.Abs(p.x - x) < TH + m && Math.Abs(p.y - a) > b / 2 - m; }
			if (kind == 2)
			{
				double r = pos(u), dd = hypot(p.x - a, p.y - b); if (Math.Abs(dd - r) >= TH * 0.8 + m) return false;
				double an = JS.atan2(p.y - b, p.x - a) * 180 / Math.PI, df = ((an - c) % 360 + 540) % 360 - 180;
				return Math.Abs(df) > d / 2 - m * 57.3 / Math.Max(r, 1);
			}
			{ double q = MFRad(c), dx = JS.cos(q) * 1600, dy = JS.sin(q) * 1600; return MFSeg(p.x, p.y, a - dx, b - dy, a + dx, b + dy) < TH * 0.75 * MFOut(u / 0.12) + m; }
		};
		return h;
	}

	// ---------- 10. the thread (2:27.7) and the two ropes after it snaps (2:36.7) ----------
	// A thread ties the left edge to the right edge (sagging); its ends move on each beat to the next of yl / yr (in 0.35 beat).
	// At bSnap it snaps: two ropes (len px) hang from where the ends were and swing on each beat to the next of al / ar (degrees: 0 =
	// pointing to the middle of the screen, 90 = straight down, -90 = straight up). Shows dashed from b0 - 1; hurts from b0 to b1.
	static double MFStepv(double bt, double b0, double[] v, double mv = 0.35)
	{
		if (bt <= b0) return v[0]; int k = (int)Math.Floor(bt - b0); double f = bt - b0 - k;
		if (k + 1 >= v.Length) return v[v.Length - 1];
		return v[k] + (v[k + 1] - v[k]) * MFEase(f / mv);
	}
	public class MFTh { public double b0, bSnap, b1, sag, len; public double[] yl, yr, al, ar; }
	static void MFThreadPts(MFTh T, double bt, Action<double, double, double, double> seg)
	{
		if (bt < T.bSnap)
		{
			double y0 = MFStepv(bt, T.b0, T.yl), y2 = MFStepv(bt, T.b0, T.yr), cx = W / 2, cy = (y0 + y2) / 2 + T.sag;
			double px = -10, py = y0;
			for (int i = 1; i <= 40; i++) { double u = i / 40.0, x = (1 - u) * (1 - u) * -10 + 2 * u * (1 - u) * cx + u * u * (W + 10), y = (1 - u) * (1 - u) * y0 + 2 * u * (1 - u) * cy + u * u * y2; seg(px, py, x, y); px = x; py = y; }
		}
		else
		{
			double yl = T.yl[T.yl.Length - 1], yr = T.yr[T.yr.Length - 1], qa = MFRad(MFStepv(bt, T.bSnap, T.al)), qb = MFRad(MFStepv(bt, T.bSnap, T.ar));
			seg(-10, yl, -10 + JS.cos(qa) * T.len, yl + JS.sin(qa) * T.len);
			seg(W + 10, yr, W + 10 - JS.cos(qb) * T.len, yr + JS.sin(qb) * T.len);
		}
	}
	// where a point of the thread is at beat bt: f 0-1 along it (before the snap), or a rope's tip (f -1 = the left rope, 2 = the right)
	public static (double x, double y) MFThreadAt(MFTh T, double bt, double f)
	{
		if (bt < T.bSnap)
		{
			double y0 = MFStepv(bt, T.b0, T.yl), y2 = MFStepv(bt, T.b0, T.yr), cx = W / 2, cy = (y0 + y2) / 2 + T.sag, u = f;
			return ((1 - u) * (1 - u) * -10 + 2 * u * (1 - u) * cx + u * u * (W + 10), (1 - u) * (1 - u) * y0 + 2 * u * (1 - u) * cy + u * u * y2);
		}
		if (f < 0) { double q = MFRad(MFStepv(bt, T.bSnap, T.al)); return (-10 + JS.cos(q) * T.len, T.yl[T.yl.Length - 1] + JS.sin(q) * T.len); }
		{ double q = MFRad(MFStepv(bt, T.bSnap, T.ar)); return (W + 10 - JS.cos(q) * T.len, T.yr[T.yr.Length - 1] + JS.sin(q) * T.len); }
	}
	public static Hz MFThread(MFTh T)
	{
		var h = new Hz { kind = "MFThread" }; const double HW = 5;
		h.update = (t, dt) => { if (BT(t) > T.b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < T.b0 - 1 || bt > T.b1 + 0.3) return;
			g.save(); g.lineCap = "round";
			if (bt < T.b0) { double q = MFk(bt, T.b0 - 1, T.b0); MFThreadPts(T, T.b0, (x0, y0, x1, y1) => MFDash(x0, y0, x1, y1, MFW(0.15 + 0.5 * q), 2)); g.restore(); return; }
			double al = 1 - MFk(bt, T.b1, T.b1 + 0.3);
			g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = MFP(al); g.lineWidth = 2 * HW; g.beginPath(); bool first = true;
			MFThreadPts(T, Math.Min(bt, T.b1), (x0, y0, x1, y1) => { if (first || bt >= T.bSnap) { g.moveTo(x0, y0); first = false; } g.lineTo(x1, y1); });
			g.stroke();
			// the anchors (white, harmless)
			g.shadowBlur = 0; g.fillStyle = MFW(0.6 * al);
			if (bt < T.bSnap) { g.beginPath(); g.arc(4, MFStepv(bt, T.b0, T.yl), 9, 0, TAU); g.arc(W - 4, MFStepv(bt, T.b0, T.yr), 9, 0, TAU); g.fill(); }
			else { g.beginPath(); g.arc(4, T.yl[T.yl.Length - 1], 9, 0, TAU); g.fill(); g.beginPath(); g.arc(W - 4, T.yr[T.yr.Length - 1], 9, 0, TAU); g.fill(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < T.b0 || bt >= T.b1) return false; bool hit = false;
			MFThreadPts(T, bt, (x0, y0, x1, y1) => { if (!hit && MFSeg(p.x, p.y, x0, y0, x1, y1) < HW + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 12. the tears that stop (3:06.7) ----------
	// A column of tears hangs at x (rows ys): white outlines from bA, pink (hurts) from bA + 1, drifting down at drift px/s. They shake from
	// bDrop - tl (the column shows dashed) and fall at sp px/s from bDrop.
	public static Hz MFHang(double x, double[] ys, double bA, double bDrop, double tl, double drift, double sp, double r)
	{
		var h = new Hz { kind = "MFHang" }; double tA = B(bA), tD = B(bDrop);
		double dy(double t) => drift * (Math.Min(t, tD) - tA) + (t > tD ? sp * (t - tD) : 0);
		double sx(double t) { double bt = BT(t); return bt >= bDrop - tl && bt < bDrop ? 3 * JS.sin(t * 70) : 0; }
		h.update = (t, dt) => { if (t > tD && ys[0] + dy(t) > H + 60) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bA) return; double oy = dy(t), ox = sx(t);
			g.save();
			if (bt >= bDrop - tl && bt < bDrop + 0.3) { double q = MFk(bt, bDrop - tl, bDrop) * (1 - MFk(bt, bDrop, bDrop + 0.3)); MFDash(x, 0, x, H, MFW(0.12 + 0.4 * q), 2); }
			if (bt < bA + 1) { g.strokeStyle = MFW(0.2 + 0.6 * MFk(bt, bA, bA + 1)); g.lineWidth = 2; foreach (var y in ys) { MFTearPath(x + ox, y + oy, r, Math.PI / 2); g.stroke(); } }
			else { MFGlow(10); foreach (var y in ys) if (y + oy > -40 && y + oy < H + 40) MFTear(x + ox, y + oy, r, Math.PI / 2); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (BT(t) < bA + 1 || Math.Abs(p.x - x) > r + 12) return false; double oy = dy(t), ox = sx(t);
			foreach (var y in ys) if (MFTearHit(p, x + ox, y + oy, r, Math.PI / 2)) return true;
			return false;
		};
		return h;
	}
	public static Hz MFEndText(double b0, double b1)
	{
		var h = new Hz { kind = "MFEnd" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = MFk(bt, b0, b0 + 0.6), a = Math.Min(k, 1 - MFk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(120 - 20 * k)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#d6e2ff"; g.shadowBlur = 26; g.fillStyle = "#eef2ff"; g.fillText("المفروض", W / 2, H / 2); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _mfb = DodgeBuild("L27", () => buildMF());
	static List<Ev> buildMF()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(MFStage(B(284))), true);

		// ===== 1. "المفروض": how it should be (0:00-1:03.7) =====
		// the tears (0:00-0:12.7): one a beat down a column that walks across and back; Normal from 0:06.7 one more on the tak
		{
			double[] cx = { 60, 200, 340, 480, 640, 800, 940, 1080, 1220 }; int[] zz = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 7, 6, 5, 4, 3, 2, 1 };
			for (int j = 0; j < 14; j++)
			{
				double b = 2 + j, x = cx[zz[j % 16]];
				at(b - 1, () => add(MFFall(x, b, 1, ez ? 700 : 900, 13)));
				if (!ez && b >= 4) { double x2 = cx[zz[(j + 8) % 16]]; at(b - 0.25, () => add(MFFall(x2, b + 0.75, 1, 900, 13))); }
			}
		}
		// the clock (0:12.7-0:24.7): the hands tick on each beat; needles come in to the middle from the edge on the taks
		at(14, () => add(ez ? MFClock(16, 32, 0, 180, new[] { 30.0 }, new[] { 0.0 }, 360, 0, false, false, 14, 32)
			: MFClock(16, 32, 0, 180, new[] { 30.0 }, new[] { -15.0 }, 400, 230, false, false, 14, 32)));
		{
			int j = 0;
			for (double bar = 16; bar < 32; bar += 4)
				foreach (var o in new[] { 0.75, 2.75 })
				{
					double b = bar + o, a = (j * 150) % 360; j++;
					if (ez && o > 1) continue;
					at(b - 0.75, () => add(MFNeedle(a, true, b, 0.75, ez ? 420 : 520)));
					if (!ez) at(b - 0.75, () => add(MFNeedle(a + 180, true, b, 0.75, 520)));
				}
		}
		// the mirror (0:24.7-0:36.7): a column of blocks on the left on the beat, the same mirrored on the right on the tak
		at(31, () => add(MFMirrorLine(31, 48)));
		{
			int[] co = { 0, 1, 2, 3, 4, 3, 2, 1 }, gaps = { 1, 3, 0, 2, 3, 1, 2, 0, 2, 0, 3, 1, 0, 2, 1, 3 };
			for (int j = 0; j < 16; j++)
			{
				double b = 32 + j, x = 64 + 128 * co[j % 8]; int gr = gaps[j];
				if (ez && j % 2 == 1) continue;
				at(b - 1, () => add(MFCol(x, gr, b, 1, ez ? 100 : 128, ez ? 0.5 : 0.6)));
				at(b - 0.75, () => add(MFCol(W - x, gr, b + 0.75, 0.75, ez ? 100 : 128, ez ? 0.5 : 0.6)));
			}
		}
		// the calendar (0:36.7-0:51.7): one day crossed out a beat in order, Normal a second one from the month's end; a row struck on the kick bars
		at(46, () => add(MFCalendar(47, 68)));
		for (int j = 0; j < 20; j++)
		{
			double b = 48 + j; int d1 = j, d2 = 27 - j;
			at(b - 1, () => add(MFCross(d1, b, 1, ez ? 0.9 : 1.6)));
			if (!ez && b >= 50) at(b - 1, () => add(MFCross(d2, b + (b >= 58 ? 0.75 : 0), 1, 1.4)));
			else if (ez && b >= 58 && j % 2 == 0) at(b - 1, () => add(MFCross(d2, b + 0.75, 1.25, 0.8)));
		}
		foreach (var (b, row) in ez ? new[] { (60.0, 2) } : new[] { (56.0, 1), (60.0, 2), (64.0, 0) }) at(b - 1, () => add(MFStrike(row, b, 1, 1)));
		// the doors (0:51.7-1:03.7): walls with a door step in from the right on each beat; Normal from 0:57 from the left too
		{
			double[] gys = { 150, 290, 430, 570, 440, 300, 160, 360 };
			double st = 214, gh = ez ? 240 : 175; int w = 0;
			foreach (var bs in ez ? new[] { 68.0, 71, 74, 77 } : new[] { 68.0, 70, 72, 74, 76, 78 })
			{
				double gy = gys[w++ % gys.Length]; var steps = new double[7]; for (int k = 0; k < 7; k++) steps[k] = bs + k;
				at(bs - 1, () => add(MFDoor(-1, gy, gh, bs, 1, steps, st)));
			}
			if (!ez) foreach (var (bs, gy) in new[] { (75.0, 220.0), (77.0, 500.0) })
				{ var steps = new double[7]; for (int k = 0; k < 7; k++) steps[k] = bs + k; at(bs - 1, () => add(MFDoor(1, gy, gh, bs, 1, steps, st))); }
		}
		at(84, () => add(MFFx(84, 0.35, 10, 1.4)));

		// ===== 2. "اللي صار": what happened (1:03.7-2:09.7) =====
		// the heart beats once, breaks on 1:04.4; the halves step apart, come back almost together, and fly off. Tears fan out of the crack.
		{
			const double HS = 8;
			at(82, () => add(MFHeart(W / 2, H / 2, HS, 84, 85)));
			MFPose pose(int side, double bt)
			{
				double d;
				if (bt < 100) { int k = (int)Math.Floor(bt - 85); d = 22 * (k + MFOut((bt - 85 - k) / 0.25)); }
				else if (bt < 104) d = 330 + 20 * JS.sin(TAU * (bt - 100) / 4);
				else if (bt < 110) d = 330 - 270 * MFEase((bt - 104) / 6);
				else if (bt < 111) d = 60;
				else d = 60 + 900 * MFk(bt, 111, 112) * MFk(bt, 111, 112);
				double bob = 26 * JS.sin(Math.PI * (bt - 85) / 4);
				return new MFPose { x = W / 2 + side * Math.Max(0, d), y = H / 2 + side * bob, rot = side * 0.12 * JS.sin(Math.PI * (bt - 85) / 8) };
			}
			at(83, () => { add(MFHalf(-1, bt => pose(-1, bt), HS, 85, 112)); add(MFHalf(1, bt => pose(1, bt), HS, 85, 112)); });
			at(85, () => add(MFFx(85, 0.25, 8, 0.8)));
			for (int j = 0; j < 26; j++)
			{
				double b = 86 + j, a = (j * 47) % 360; int nn = j % 2 == 0 ? 4 : 3;
				at(b - 0.75, () => add(ez ? MFFan(W / 2, H / 2, 2, a, 36, b, 0.75, 320, 12, 30) : MFFan(W / 2, H / 2, nn, a, 18, b, 0.75, 380, 12, 30)));
			}
			// the heart's rain: a row of tears from the top on each bar (Easy every other bar), from below too from 1:15.7 (Normal)
			{
				double[] gxs = { 220, 1060, 420, 860, 640, 300, 980 }; int k = 0;
				for (double bar = ez ? 92 : 88; bar < 111; bar += ez ? 12 : 4)
				{
					double b = bar, gx = gxs[k++ % gxs.Length]; at(b - 1, () => add(MFRain(1, gx, ez ? 260 : 190, b, 1, ez ? 300 : 360, 12)));
					if (!ez && bar >= 100) { double gu = W - gx, bu = bar + 2; at(bu - 1, () => add(MFRain(-1, gu, 190, bu, 1, 360, 12))); }
				}
			}
		}
		// the two roads (1:24.7-1:45.7): they come in as one, part and cross every bar; tears drop off them into the outer lanes
		{
			double v = ez ? 240 : 300, sepA = ez ? 330 : 300, per = ez ? 1300 : 900;
			at(110, () => add(MFRoads(112, 133.5, v, ez ? 9 : 11, sepA, per)));
			// the side rain: a wall of tears from the right with a gap, every other bar (Easy every 4 bars)
			{
				double[] gys = { 600, 130, 470, 250 }; int k = 0;
				for (double bar = 118; bar < 134; bar += ez ? 16 : 8) { double b = bar, gy = gys[k++ % gys.Length]; at(b - 1, () => add(MFRain(-2, gy, ez ? 240 : 180, b, 1, ez ? 300 : 360, 12))); }
			}
			int j = 0;
			for (double b = 116; b < 134; b += ez ? 4 : 1)
			{
				double x = j % 2 == 0 ? 920 : 400; int road = j % 2; j++; double bb = b;
				at(b - 0.75, () => add(MFRoadDrop(112, v, sepA, per, road, x, road == 0 ? -1 : 1, bb, 0.75, ez ? 340 : 420, 12)));
			}
		}
		// the clock backwards (1:45.7-2:09.7): the hands jump back; from 1:57.7 the minute hand reaches both ways and the numbers fly off on the taks
		at(139, () => add(ez ? MFClock(141, 156, 0, 0, new[] { -40.0 }, new[] { 0.0 }, 340, 0, false, true, 139, 156)
			: MFClock(141, 156, 0, 0, new[] { -40.0 }, new[] { 20.0 }, 380, 220, false, true, 139, 156)));
		at(154, () => add(ez ? MFClock(156, 172, -600, 0, new[] { -40.0 }, new[] { 0.0 }, 340, 0, true, true, 156, 171)
			: MFClock(156, 172, -600, 300, new[] { -40.0 }, new[] { 20.0 }, 380, 220, true, true, 156, 171)));
		{
			int j = 0;
			for (double bar = 140; bar < 156; bar += 4)
			{
				double b = bar + 2, a = (j * 90 + 45) % 360; j++; if (!ez || j % 2 == 0) at(b - 0.75, () => add(MFNeedle(a, true, b, 0.75, ez ? 420 : 500)));
				double b2 = bar + 4.75, a2 = (j * 70 + 10) % 360; if (b2 < 156) at(b2 - 0.75, () => { add(MFNeedle(a2, false, b2, 0.75, ez ? 420 : 500)); if (!ez) add(MFNeedle(a2 + 180, false, b2, 0.75, 500)); });
			}
			j = 0;
			for (double bar = 156; bar < 172; bar += 4)
				foreach (var o in new[] { 0.75, 2.75 })
				{
					double b = bar + o, a = (360 - (j * 30) % 360) % 360; j++;
					if (ez && o > 1) continue;
					at(b - 0.75, () => add(MFNeedle(a, false, b, 0.75, ez ? 420 : 520)));
					if (!ez) at(b - 0.75, () => add(MFNeedle(a + 180, false, b, 0.75, 520)));
				}
		}
		at(172, () => add(MFFx(172, 0.3, 8, 1.2)));

		// ===== 3. "المفروض والواقع": the dream and the real (2:09.7-3:30.7) =====
		// the dream shows white, the real one follows a beat later in pink
		{
			double[] gx = { 320, 900, 560, 1040, 240, 760 }, gy = { 200, 520, 360, 150, 580, 300 };
			double[][] ring = { new[] { 0.0, 0, 45, 70 }, new[] { W, H, 225, 70 }, new[] { W, 0, 135, 70 }, new[] { 0, H, 315, 70 } };
			int k = 0;
			for (double bar = 173; bar < 196; bar += 4, k++)
			{
				double gxk = gx[k % 6], gyk = gy[k % 6], br = bar; var rg = ring[k % 4]; double sl = k % 2 == 0 ? 30 : -30;
				at(bar - 1, () => add(MFEcho(0, gxk, ez ? 320 : 200, 0, 0, br, 1, 2.5)));
				if (bar + 1 < 196 && !ez) at(bar, () => add(MFEcho(2, rg[0], rg[1], rg[2], rg[3] + 20, br + 1, 1, 3.5)));
				if (bar + 2 < 196) at(bar + 1, () => add(MFEcho(1, gyk, ez ? 300 : 190, 0, 0, br + 2, 1, 3)));
				if (bar + 3 < 196 && !ez) at(bar + 2, () => add(MFEcho(3, 640, 360, sl, 0, br + 3, 1, 0.5)));
				if (ez && k % 2 == 1 && bar + 3 < 196) at(bar + 2, () => add(MFEcho(2, rg[0], rg[1], rg[2], 120, br + 3, 1, 3.5)));
			}
		}
		// the thread (2:27.7): its ends move on each beat; tears leave it on the taks. It snaps at 2:36.7 and the two ropes swing.
		{
			var T = new MFTh { b0 = 196, bSnap = 208, b1 = 224, sag = 80, len = ez ? 480 : 620 };
			T.yl = new double[] { 360, 200, 260, 460, 540, 380, 220, 300, 520, 440, 260, 180, 340 };
			T.yr = new double[] { 360, 520, 440, 240, 180, 340, 520, 460, 200, 280, 480, 540, 380 };
			T.al = new double[] { 20, 60, 20, 65, 10, 55, 5, 60, -5, 45, 0, 60, -10, 40, 55, 25, 40 };
			T.ar = new double[] { 20, 15, 60, 10, 65, 5, 55, -5, 60, 0, 45, -10, 60, 55, 40, 50, 40 };
			at(195, () => add(MFThread(T)));
			at(208, () => add(MFFx(208, 0.3, 12, 1)));
			int j = 0;
			for (double bar = 196; bar < 208; bar += 4)
				foreach (var o in new[] { 0.75, 2.75 })
				{
					double b = bar + o, f = j % 2 == 0 ? 0.3 : 0.7; int dir = j % 2 == 0 ? -1 : 1; j++;
					if (ez && o > 1) continue;
					var q = MFThreadAt(T, b, f); at(b - 0.75, () => add(MFFan(q.x, q.y, ez ? 1 : 2, dir * 90, 24, b, 0.75, ez ? 320 : 380, 12, 12)));
				}
			for (double bar = 208; bar < 224; bar += 4)
				foreach (var o in new[] { 0.75, 2.75 })
				{
					double b = bar + o; if (ez && o > 1) continue;
					var ql = MFThreadAt(T, b, -1); var qr = MFThreadAt(T, b, 2);
					at(b - 0.75, () => { add(MFFan(ql.x, ql.y, 1, -90, 26, b, 0.75, ez ? 320 : 380, 12, 10)); add(MFFan(qr.x, qr.y, 1, -90, 26, b, 0.75, ez ? 320 : 380, 12, 10)); });
				}
		}
		// the two halves go round and never meet (2:48.7-3:06.7): tears out on each beat, Normal one in on the taks
		{
			const double HS = 7, R = 240;
			MFPose orb(int side, double bt) { double th = MFRad(-90 + (ez ? 30 : 36) * (bt - 224)) + (side > 0 ? Math.PI : 0); return new MFPose { x = W / 2 + JS.cos(th) * R, y = H / 2 + JS.sin(th) * R, rot = th + (side > 0 ? 0 : Math.PI) }; }
			at(222, () => { add(MFHalf(-1, bt => orb(-1, bt), HS, 224, 248)); add(MFHalf(1, bt => orb(1, bt), HS, 224, 248)); });
			for (double b = 225; b < 248; b += 1)
				foreach (var side in new[] { -1, 1 })
				{
					var q = orb(side, b); double a = JS.atan2(q.y - H / 2, q.x - W / 2) * 180 / Math.PI, bb = b;
					at(b - 0.75, () => add(MFFan(q.x, q.y, ez ? 2 : 4, a, ez ? 30 : 15, bb, 0.75, ez ? 300 : 430, 12, 40)));
					if (!ez) { var q2 = orb(side, b + 0.75); double a2 = JS.atan2(H / 2 - q2.y, W / 2 - q2.x) * 180 / Math.PI; at(b, () => add(MFFan(q2.x, q2.y, 1, a2, 0, bb + 0.75, 0.75, 360, 12, 20))); }
				}
		}
		// the heart's rain again: a row of tears on each bar, from the top and the bottom in turn (Easy every other bar, from the top)
		{
			double[] gxs = { 980, 300, 760, 520, 1100, 180 }; int k = 0;
			for (double bar = 226; bar < 247; bar += ez ? 16 : 4) { double b = bar, gx = gxs[k % gxs.Length]; int dir = ez || k % 2 == 0 ? 1 : -1; k++; at(b - 1, () => add(MFRain(dir, gx, ez ? 260 : 180, b, 1, ez ? 300 : 400, 12))); }
		}
		// the tears that stop (3:06.7-3:15.7): they hang, a column falls on each beat (Normal also on the taks from 3:11) and hangs again
		{
			int[] ord = { 4, 3, 5, 2, 6, 1, 7, 0, 8 };
			var drops = new List<double>[9]; for (int c = 0; c < 9; c++) drops[c] = new List<double>();
			int i = 0;
			for (double b = 249; b < 260; b += 1)
			{
				drops[ord[i++ % 9]].Add(b);
				if (!ez && b >= 250 && b + 0.75 < 260) drops[ord[(i + 3) % 9]].Add(b + 0.75);
			}
			for (int c = 0; c < 9; c++)
			{
				double x = 80 + 140 * c; var ys = c % 2 == 0 ? new double[] { 80, 230, 380, 530 } : new double[] { 155, 305, 455 };
				double bA = 247; var dl = drops[c]; dl.Sort(); dl.Add(260);
				foreach (var bd in dl)
				{
					if (bd - bA < 1.5) continue; double a0 = bA, d0 = bd;
					at(a0 - 0.1 < 247 ? 247 : a0 - 0.1, () => add(MFHang(x, ys, a0, d0, 0.75, ez ? 14 : 24, ez ? 600 : 760, 13)));
					bA = bd + 1;
				}
			}
			at(260, () => add(MFFx(260, 0.3, 10, 1)));
		}
		// the clock stops (3:15.7-3:30.7): it ticks again, the needles come in on the taks, its last ticks get shorter and it stops at twelve on 3:29.2
		{
			var stM = new List<double>(); var stH = new List<double>();
			for (int k = 0; k < 15; k++) { stM.Add(30); stH.Add(-15); }
			stM.AddRange(new[] { 30.0, 20, 10, 0 }); stH.AddRange(new[] { -10.0, -6, -4, 0 });
			at(258, () => add(ez ? MFClock(261, 279, 210, 0, stM.ToArray(), new[] { 0.0 }, 360, 0, false, false, 259, 279.5)
				: MFClock(261, 279, 210, 245, stM.ToArray(), stH.ToArray(), 400, 230, false, false, 259, 279.5)));
			int j = 0;
			for (double bar = 260; bar < 276; bar += 4)
				foreach (var o in new[] { 0.75, 2.75 })
				{
					double b = bar + o; if (b < 261.5) continue; double a = (j * 120) % 360; j++;
					if (ez && o > 1) continue;
					at(b - 0.75, () => add(MFNeedle(a, true, b, 0.75, ez ? 420 : 580)));
					if (!ez) { at(b - 0.75, () => add(MFNeedle(a + 120, true, b, 0.75, 580))); at(b - 0.75, () => add(MFNeedle(a + 240, true, b, 0.75, 580))); }
				}
			// Normal: from 3:18.7 two needles out from the middle on the beats between
			if (!ez) for (double b0 = 265; b0 < 276; b0 += 2) { double b = b0, a = (b * 75) % 360; at(b - 0.75, () => { add(MFNeedle(a, false, b, 0.75, 500)); add(MFNeedle(a + 180, false, b, 0.75, 500)); }); }
		}
		at(279, () => add(MFEndText(279, 280.5)));
		at(280, () => add(MFFx(280, 0.4, 0, 1.4)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
