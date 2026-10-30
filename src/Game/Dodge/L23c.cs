using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// راضي بحبك (main level 23), part 3: the clock, the hearts gather, the end; and the level's events
public static partial class G
{
	// ---------- the clock (1:45) ----------
	// Two hands (pink) from a gold middle. Each ticks on: from b0 it jumps step degrees every per beats (the jump takes 0.3 of a step);
	// where it will be next shows dashed. The hands grow in the beat before b0 and go in the half beat after b1.
	public class RZHand { public double a0, step, per, len, th; }
	static double RZHandAng(RZHand k, double b0, double bt)
	{
		double x = Math.Max(0, (bt - b0) / k.per); int i = (int)Math.Floor(x); double u = x - i;
		return k.a0 + k.step * (i + RZEase(u / 0.3));
	}
	public static Hz RZClock(RZHand[] hands, double b0, double b1)
	{
		var h = new Hz { kind = "RZClock" }; const double R0 = 30;
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		double gr(double bt) => RZOut(bt - (b0 - 1)) * (1 - RZk(bt, b1, b1 + 0.5));
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2) return; double a = RZk(bt, b0 - 2, b0 - 1) * (1 - RZk(bt, b1 + 0.5, b1 + 1)), k = gr(bt);
			g.save(); g.globalAlpha = a;
			// the face: 12 marks (white, harmless)
			g.strokeStyle = RZW(0.35); g.lineWidth = 3;
			for (int i = 0; i < 12; i++) { double q = i * TAU / 12; g.beginPath(); g.moveTo(W / 2 + JS.cos(q) * 300, H / 2 + JS.sin(q) * 300); g.lineTo(W / 2 + JS.cos(q) * (i % 3 == 0 ? 340 : 322), H / 2 + JS.sin(q) * (i % 3 == 0 ? 340 : 322)); g.stroke(); }
			foreach (var hd in hands)
			{
				double an = RZHandAng(hd, b0, bt), c = JS.cos(RZRad(an)), s = JS.sin(RZRad(an));
				// the next place (dashed)
				if (bt < b1 - 0.2) { double nx = hd.a0 + hd.step * ((bt < b0 ? -1 : Math.Floor((bt - b0) / hd.per)) + 2); RZDash(W / 2 + JS.cos(RZRad(nx)) * R0, H / 2 + JS.sin(RZRad(nx)) * R0, W / 2 + JS.cos(RZRad(nx)) * hd.len, H / 2 + JS.sin(RZRad(nx)) * hd.len, RZW(0.45 * k), 2); }
				if (bt < b0 - 1) { RZDash(W / 2 + c * R0, H / 2 + s * R0, W / 2 + c * hd.len, H / 2 + s * hd.len, RZW(0.6), 3); continue; }
				double l = R0 + (hd.len - R0) * k; if (l < R0 + 2) continue;
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = hd.th; g.lineCap = "round";
				g.beginPath(); g.moveTo(W / 2 + c * R0, H / 2 + s * R0); g.lineTo(W / 2 + c * l, H / 2 + s * l); g.stroke(); g.shadowBlur = 0;
			}
			g.lineCap = "butt"; g.fillStyle = RZG(1); g.shadowColor = "#ecbe5a"; g.shadowBlur = 14; circle(W / 2, H / 2, 24); g.shadowBlur = 0;
			g.fillStyle = "rgba(60,40,20,1)"; circle(W / 2, H / 2, 8);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 - 1) return false; double k = gr(bt);
			foreach (var hd in hands)
			{
				double l = R0 + (hd.len - R0) * k; if (l < R0 + 4) continue; double an = RZRad(RZHandAng(hd, b0, bt));
				if (RZSeg(p.x, p.y, W / 2 + JS.cos(an) * R0, H / 2 + JS.sin(an) * R0, W / 2 + JS.cos(an) * l, H / 2 + JS.sin(an) * l) < hd.th / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the hearts gather (2:00) ----------
	// n small hearts (pink, size sz) stand on a big heart (size S) round the middle: they show dashed from b0, hurt from b0 + 1, turn rot
	// degrees a beat and breathe on the beat. At br they fly at sp px/s: out (mode 0) or in through the middle and out the other side (1).
	// Their ways show dashed from br - tl.
	public static Hz RZGather(int n, double S, double sz, double b0, double br, int mode, double rot0, double rot, double sp, double tl)
	{
		var h = new Hz { kind = "RZGather" }; double tr = B(br);
		double rotAt(double bt) => rot0 + rot * (Math.Min(bt, br) - b0);
		double sc(double bt) => S * (1 + 0.08 * RZPulse(Math.Min(bt, br - 0.01)));
		(double x, double y, double dx, double dy) at(int k, double bt)
		{
			double r = RZRad(rotAt(bt)), c = JS.cos(r), s = JS.sin(r), s0 = sc(bt), lx = RZHX[k * RZN / n] * s0, ly = RZHY[k * RZN / n] * s0;
			double x = W / 2 + lx * c - ly * s, y = H / 2 + lx * s + ly * c, d = Math.Max(1, hypot(x - W / 2, y - H / 2)), ux = (x - W / 2) / d, uy = (y - H / 2) / d;
			if (mode == 1) { ux = -ux; uy = -uy; }
			return (x, y, ux, uy);
		}
		(double x, double y, double ang) pos(int k, double t)
		{
			double bt = BT(t); var q = at(k, bt); double f = Math.Max(0, t - tr) * sp;
			return (q.x + q.dx * f, q.y + q.dy * f, RZRad(rotAt(bt)));
		}
		h.update = (t, dt) => { if (t > tr + 2200 / sp) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; g.save();
			if (bt < b0 + 1)
			{
				double q = RZk(bt, b0, b0 + 1); g.strokeStyle = RZW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 5, 5 });
				for (int k = 0; k < n; k++) { var p = pos(k, t); RZSmallHeart(p.x, p.y, sz, p.ang); g.stroke(); }
				g.setLineDash(new double[0]); g.restore(); return;
			}
			if (bt >= br - tl && bt < br + 0.4)
			{
				double q = RZk(bt, br - tl, br) * (1 - RZk(bt, br, br + 0.4));
				for (int k = 0; k < n; k++) { var a = at(k, bt); RZDash(a.x, a.y, a.x + a.dx * 1500, a.y + a.dy * 1500, RZW(0.15 + 0.4 * q), 2); }
			}
			g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			for (int k = 0; k < n; k++) { var p = pos(k, t); RZSmallHeart(p.x, p.y, sz, p.ang); g.fill(); }
			g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) =>
		{
			if (BT(t) < b0 + 1) return false;
			for (int k = 0; k < n; k++) { var q = pos(k, t); if (RZSmallHit(p, q.x, q.y, sz, q.ang)) return true; }
			return false;
		};
		return h;
	}
	// n small hearts (pink) circle the middle at r px, rot degrees a beat, from b0 (dashed for the first beat) to b1
	public static Hz RZOrbit(int n, double r, double rot, double sz, double b0, double b1)
	{
		var h = new Hz { kind = "RZOrbit" };
		h.update = (t, dt) => { if (BT(t) > b1) h.dead = true; };
		(double x, double y, double a) pos(int k, double bt) { double a = RZRad(rot * (bt - b0) + k * 360.0 / n); return (W / 2 + JS.cos(a) * r, H / 2 + JS.sin(a) * r, a + Math.PI / 2); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; g.save();
			if (bt < b0 + 1) { g.strokeStyle = RZW(0.3 + 0.5 * RZk(bt, b0, b0 + 1)); g.lineWidth = 2; g.setLineDash(new double[] { 5, 5 }); for (int k = 0; k < n; k++) { var q = pos(k, bt); RZSmallHeart(q.x, q.y, sz, q.a); g.stroke(); } g.setLineDash(new double[0]); }
			else { g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; for (int k = 0; k < n; k++) { var q = pos(k, bt); RZSmallHeart(q.x, q.y, sz, q.a); g.fill(); } g.shadowBlur = 0; }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 + 1) return false;
			for (int k = 0; k < n; k++) { var q = pos(k, bt); if (RZSmallHit(p, q.x, q.y, sz, q.a)) return true; }
			return false;
		};
		return h;
	}
	// the last words (gold, harmless)
	public static Hz RZEnd(double b0, double b1)
	{
		var h = new Hz { kind = "RZEnd", z = -1 };   // under the envelope while it folds away
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = RZk(bt, b0, b0 + 0.5), a = Math.Min(k, 1 - RZk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(84 - 10 * k)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "rtl";
			g.shadowColor = "#ecbe5a"; g.shadowBlur = 26; g.fillStyle = "#f6dc96"; g.fillText("راضي بحبك", W / 2, H - 110); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _rzb = DodgeBuild("L23", () => buildRZ());
	static List<Ev> buildRZ()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(RZStage(263)), true);
		double f1 = ez ? 0.85 : 1.0, f2 = f1 * 1.05, f3 = f1 * 1.1;   // speeds: each part 5% faster

		// ===== 1. the letter (0:00-0:52.6) =====
		// the pen writes right to left (0:02.6), the 4th line is a heart (0:12.6)
		{
			double hot = ez ? 1.5 : 2.0, tl = ez ? 2.0 : 1.5;
			var L = new List<RZLine>
			{
				RZWave(1200, 170, 80, 250, 50, 1.5, 0, 4, 3, hot, tl),
				RZWave(1200, 560, 80, 470, 50, 1.5, Math.PI, 8, 3, hot, tl),
				RZWave(1200, 360, 80, 360, 110, 2, 0, 12, 3, hot, tl),
			};
			if (!ez)
			{
				L.Add(RZWave(980, -10, 300, 730, 30, 1, 0, 6, 2.5, hot, tl));
				L.Add(RZWave(980, 730, 300, -10, 30, 1, 0, 10, 2.5, hot, tl));
				L.Add(RZWave(1200, 640, 80, 80, 40, 1.5, 0.5, 14, 2.5, hot, tl));
			}
			L.Add(RZHeartLine(W / 2, H / 2, 230, 16, 4, ez ? 1.2 : 1.5, tl));
			foreach (var l in L) { var ll = l; at(ll.bg - ll.tl, () => hazards.AddRange(RZPen(ll))); }
		}
		// the ink drops (0:12.6): right to left on the upper line, then the lower one; from 0:17.6 a second drop goes left to right
		for (int i = 0; i < 16; i++)
		{
			double b = 20 + i, x = 1130 - 140 * (i % 8), y = i < 8 ? 210 : 510;
			if (ez && i % 2 == 1) continue;
			at(b - 1, () => add(RZDrop(x, y, b, 1, 1.6, 250)));
			if (!ez && i >= 8) { double x2 = 150 + 140 * (i % 8); at(b - 1, () => add(RZDrop(x2, 210, b, 1, 1.6, 250))); }
		}
		// the lines of the letter (0:22.6): one every 2 beats, from 0:32.6 (the bass) one every beat, shorter
		{
			double[] ys = { 150, 560, 290, 430, 110, 610, 250, 500 };
			double[][] W1 = { new[] { 150.0, 90, 210, 120 }, new[] { 200.0, 110, 160 }, new[] { 90.0, 230, 140, 100 }, new[] { 170.0, 150, 120 } };
			double[][] G1 = { new[] { 70.0, 80, 60 }, new[] { 90.0, 70 }, new[] { 60.0, 80, 70 }, new[] { 80.0, 70 } };
			for (int i = 0; i < 8; i++) { double b = 36 + 2 * i, y = ys[i]; var w = W1[i % 4]; var gg = G1[i % 4]; at(b - 1, () => add(RZRow(y, w, gg, b, 1, 330 * f1))); }
			double[] ys2 = { 200, 520, 330, 640, 90, 420, 260, 580 };
			double[][] W2 = { new[] { 160.0, 200 }, new[] { 120.0, 150, 90 }, new[] { 220.0, 110 }, new[] { 140.0, 180 } };
			for (int i = 0; i < 8; i++)
			{
				if (ez && i % 2 == 1) continue;
				double b = 52 + i, y = ys2[i]; var w = W2[i % 4]; var gg = w.Length == 2 ? new[] { 80.0 } : new[] { 70.0, 70 };
				at(b - 1, () => add(RZRow(y, w, gg, b, 1, 400 * f1)));
			}
		}
		// the envelope (0:37.6): one flap a beat round the screen, then two side by side going round (walk round with them), then all but one (the open side goes round)
		{
			double hold = 0.5, back = 0.25, tl = ez ? 1.25 : 1;
			int[] ring = { 0, 1, 2, 3 };
			if (ez)
			{
				for (int i = 0; i < 12; i++) { double b = 60 + i; int s = ring[i % 4]; at(b - tl, () => add(RZFlap(new[] { s }, 1, b, tl, hold, back))); }
				for (int i = 0; i < 5; i++) { double b = 72 + 2 * i; var ss = i % 2 == 0 ? new[] { 0, 2 } : new[] { 1, 3 }; at(b - tl, () => add(RZFlap(ss, 1, b, tl, hold, back))); }
			}
			else
			{
				for (int i = 0; i < 8; i++) { double b = 60 + i; int s = ring[i % 4]; at(b - tl, () => add(RZFlap(new[] { s }, 1, b, tl, hold, back))); }
				for (int i = 0; i < 8; i++) { double b = 68 + i; var ss = new[] { ring[i % 4], ring[(i + 1) % 4] }; at(b - tl, () => add(RZFlap(ss, 1, b, tl, hold, back))); }
				int[] miss = { 3, 0 };
				for (int i = 0; i < 2; i++) { double b = 76 + 2 * i; int m = miss[i]; var ss = ring.Where(s => s != m).ToArray(); at(b - tl, () => add(RZFlap(ss, 1, b, tl, 0.8, back))); }
				// then the last two pairs, opposite (the seal comes after)
				at(80 - tl, () => add(RZFlap(new[] { 0, 2 }, 1, 80, tl, hold, back))); at(81 - tl, () => add(RZFlap(new[] { 1, 3 }, 1, 81, tl, hold, back)));
			}
		}
		// the seal (0:52.6, "راضي بحبك"): the letter is sealed with a heart; it stays as the gold heart of part 2
		at(82, () => add(RZSeal(W / 2, H / 2, 150, 84, 2, 0.75, 84.75)));
		at(84, () => add(RZFx(84, 0.3, 10, 1.0)));

		// ===== 2. the heart (0:52.6-1:45) =====
		at(84, () => add(RZCore(84.75, 136, 64)));
		{
			double[] gaps = { Math.PI, Math.PI / 2, Math.PI * 1.5, 0, Math.PI * 0.75, Math.PI * 1.25 };
			double gw = ez ? 0.62 : 0.44, sp = 250 * f2;
			int gi = 0;
			// out from the heart every 2 beats
			for (double b = 85; b < 96; b += 2) { double ug = gaps[gi++ % gaps.Length], bb = b; at(bb - 1, () => add(RZHeartWave(W / 2, H / 2, bb, 70, sp, ug, gw, 1, 160))); }
			// the beat and its echo: out on the beat, in from outside on the next (the open way opposite)
			for (double b = 96; b < 108; b += 2)
			{
				double ug = gaps[gi++ % gaps.Length], bb = b; at(bb - 1, () => add(RZHeartWave(W / 2, H / 2, bb, 70, sp, ug, gw, 1, 160)));
				if (!ez && (b - 96) % 4 == 0) { double ui = (ug + Math.PI) % TAU, bi = b + 1; at(bi - 1, () => add(RZHeartWave(W / 2, H / 2, bi, 860, -sp, ui, gw, 1, 340))); }
			}
		}
		at(108, () => add(RZFx(108, 0.2, 8, 0.8)));
		// the arrows (1:07.6): a wall every 2 beats, from each side in turn (round like a clock); from 1:17.6 two at once, across each other
		{
			double gw = ez ? 240 : 180, sp = 420 * f2;
			double[] gy = { 360, 200, 520, 300, 460, 160, 560, 380 }, gx = { 640, 340, 940, 500, 800, 260, 1020, 620 };
			int k = 0;
			for (double b = 108; b < 124; b += 2) { int d = k % 4; double gc = d % 2 == 0 ? gy[k % 8] : gx[k % 8], bb = b; k++; at(bb - 1, () => add(RZArrows(d, gc, gw, bb, 1, sp))); }
			for (double b = 124; b < 136; b += 2)
			{
				int d = k % 4; double gc = d % 2 == 0 ? gy[k % 8] : gx[k % 8], bb = b; k++; at(bb - 1, () => add(RZArrows(d, gc, gw, bb, 1, sp)));
				if (!ez && (b - 124) % 4 == 0) { int d2 = (d + 1) % 4; double gc2 = d2 % 2 == 0 ? gy[(k + 3) % 8] : gx[(k + 3) % 8]; at(bb - 1, () => add(RZArrows(d2, gc2, gw + 40, bb, 1, sp))); }
			}
		}
		at(136, () => add(RZFx(136, 0.25, 8, 1.0)));
		// the rose (1:25, the music alone): 4 petals dance round, turning back every 8 beats; from 1:35 five petals and the petals fly
		{
			double r1 = (ez ? 15 : 21) * f2, r2 = (ez ? 18 : 25) * f2;
			double[] k1 = { 137, 144, 152 }, v1 = { r1, -r1, 0 }, k2 = { 153, 160, 168 }, v2 = { r2, -r2, 0 };
			Func<double, double> a1 = bt => RZSpin(k1, v1, 45, bt), a2 = bt => RZSpin(k2, v2, 0, bt);
			at(135, () => hazards.AddRange(RZRose(ez ? 3 : 4, 135, 152, 590, 78, a1)));
			at(151, () => hazards.AddRange(RZRose(ez ? 4 : 5, 151, 165, 610, 78, a2)));
			int n2 = ez ? 4 : 5;
			for (double b = 154.5; b < 164; b += ez ? 4 : 2)
			{
				double bb = b, a0 = a2(b) + 180.0 / n2; int nn = ez ? 8 : 10;
				at(bb - 1, () => add(RZShots(nn, a0, 50, bb, 1, 300 * f2, 11, true)));
			}
		}

		// ===== 3. contentment (1:45-2:43) =====
		at(168, () => add(RZFx(168, 0.3, 10, 1.0)));
		// the clock (1:45, waiting): the long hand ticks 30 degrees a beat, the short one 45 degrees back every 2 beats; from 1:52.6 dots fly out every 4 beats
		{
			var hands = ez ? new[] { new RZHand { a0 = -90, step = 30, per = 1, len = 600, th = 14 } }
				: new[] { new RZHand { a0 = -90, step = 30, per = 1, len = 620, th = 14 }, new RZHand { a0 = 90, step = -45, per = 2, len = 360, th = 22 } };
			at(166, () => add(RZClock(hands, 168, 192)));
			for (double b = 181; b < 190; b += 4) { double bb = b, a0 = 15 + 7 * (b - 181); int nn = ez ? 8 : 10; at(bb - 1, () => add(RZShots(nn, a0, 40, bb, 1, 260 * f3, 9, false))); }
		}
		// the hearts gather (2:00): a heart of small hearts forms round you and flies out every 4 beats; the 3rd one goes in and through;
		// small hearts circle inside; from 2:10 hearts fly in from all round, through the middle
		{
			int n = ez ? 12 : 16; double sp = 320 * f3;
			int[] modes = { 0, 0, 1, 0 }; double[] rots = { 10, -10, 12, -12 };
			for (int i = 0; i < 4; i++) { double b0 = 191 + 4 * i, br = 196 + 4 * i; int md = modes[i]; double rr = rots[i] * (ez ? 0.7 : 1); at(b0, () => add(RZGather(n, 270, 22, b0, br, md, 0, rr, sp, 1))); }
			if (!ez) at(196, () => add(RZOrbit(5, 105, 40, 18, 196, 203)));
			for (double b = 208; b < 216; b += 2)
			{
				double bb = b, a0 = (b - 208) / 2 % 2 == 0 ? 0 : 22.5; int nn = ez ? 4 : 8;
				// hearts start on a ring round the screen and fly through the middle: the same as a gather with mode 1 from far out
				at(bb - 1, () => add(RZRing(nn, a0, 760, bb, 1, sp * 1.05)));
			}
		}
		at(216, () => add(RZFx(216, 0.2, 8, 0.8)));
		// all together (2:15): the heart beats out (every 2 beats); arrows every 4 beats; from 2:25 the pen writes again, fast
		at(215, () => add(RZCore(215.6, 244, 56)));
		{
			double[] gaps = { Math.PI * 1.5, Math.PI / 2, Math.PI, 0, Math.PI * 1.25, Math.PI * 0.75 };
			double gw = ez ? 0.62 : 0.44, sp = 250 * f3; int gi = 0;
			for (double b = 216; b <= 240; b += 2) { double ug = gaps[gi++ % gaps.Length], bb = b; at(bb - 1, () => add(RZHeartWave(W / 2, H / 2, bb, 70, sp, ug, gw, 1, 160))); }
			double[] gy = { 180, 540, 300, 440 }, gx = { 300, 980, 520, 760 }; int k = 0;
			for (double b = 217; b < 232; b += ez ? 8 : 4) { int d = (k + 1) % 4; double gc = d % 2 == 0 ? gy[k % 4] : gx[k % 4], bb = b; k++; at(bb - 1, () => add(RZArrows(d, gc, ez ? 240 : 180, bb, 1, 420 * f3))); }
			double hot = ez ? 1.0 : 1.2;
			var P = new List<RZLine>
			{
				RZWave(1240, 120, 300, 640, 40, 1, 0, 233, 1.5, hot, 1),
				RZWave(1240, 620, 300, 100, 40, 1, Math.PI, 235, 1.5, hot, 1),
				RZWave(980, 40, 40, 560, 40, 1, 0, 237, 1.5, hot, 1),
				RZWave(980, 700, 40, 180, 40, 1, Math.PI, 239, 1.5, hot, 1),
			};
			for (int i = 0; i < P.Count; i++) { if (ez && i % 2 == 1) continue; var ll = P[i]; at(ll.bg - ll.tl, () => hazards.AddRange(RZPen(ll))); }
		}
		// the end (2:32): the envelope closes step by step, only its X stays free; ink drops spread from the middle; the gold seal
		{
			double[] sb = { 245, 249, 253, 257 }, sd = ez ? new[] { 0.25, 0.42, 0.55, 0.66 } : new[] { 0.3, 0.5, 0.66, 0.8 };
			at(243, () => add(RZEnvelope(sb, sd, 2, 259.5)));
			foreach (var b in ez ? new double[0] : new[] { 251.0, 255 }) { double bb = b; at(bb - 1, () => add(RZDrop(W / 2, H / 2, bb, 1, 2.2, 420))); }
			at(259, () => add(RZSeal(W / 2, H / 2, 120, 259.5, 0.5, 0, 262)));
			at(259.5, () => add(RZFx(259.5, 0.35, 8, 1.2)));
			at(259.5, () => add(RZEnd(259.5, 262)));
		}
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
	// n small hearts on a ring of r px round the middle fly in through it and out the other side at sp px/s from b (ways dashed from b - tl)
	public static Hz RZRing(int n, double a0, double r, double b, double tl, double sp)
	{
		var h = new Hz { kind = "RZRing" }; double tb = B(b); const double SZ = 22;
		(double x, double y, double a) pos(int k, double t)
		{
			double a = RZRad(a0 + k * 360.0 / n), d = r - sp * Math.Max(0, t - tb);
			return (W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d, a - Math.PI / 2);
		}
		h.update = (t, dt) => { if (t > tb && r - sp * (t - tb) < -r) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			if (bt < b + 1)
			{
				double q = RZk(bt, b - tl, b) * (1 - RZk(bt, b + 0.5, b + 1));
				for (int k = 0; k < n; k++) { double a = RZRad(a0 + k * 360.0 / n); RZDash(W / 2 + JS.cos(a) * 800, H / 2 + JS.sin(a) * 800, W / 2 - JS.cos(a) * 800, H / 2 - JS.sin(a) * 800, RZW(0.15 + 0.4 * q), 2); }
			}
			if (t >= tb) { g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; for (int k = 0; k < n; k++) { var q = pos(k, t); RZSmallHeart(q.x, q.y, SZ, q.a); g.fill(); } g.shadowBlur = 0; }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tb) return false;
			for (int k = 0; k < n; k++) { var q = pos(k, t); if (RZSmallHit(p, q.x, q.y, SZ, q.a)) return true; }
			return false;
		};
		return h;
	}
}
