using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// شيلة "حان الوعد يا لابتي" (main level 25): the events. Beat b = 0.38 + b * 60/134.7 s of the song. The song's breaks (the kick only on
// the even beats): b88-102 (0:39.6), b184-196 (1:22.3), b280-294 (2:05.1), b374-390 (2:47), the end at b412 (3:03.9). Saves at b102 and b196.
public static partial class G
{
	static readonly int _wdb = DodgeBuild("L25", () => buildWD());
	static List<Ev> buildWD()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		double[] seq(double a, double b, double step) { var l = new List<double>(); for (double x = a; x < b - 1e-9; x += step) l.Add(x); return l.ToArray(); }
		at(0, () => add(WDStage(B(414))), true);
		const double CX = 640, CY = 360;

		// ===== 1. the call (night, 0:00-0:45.8) =====
		at(0, () => add(WDDrum(CX, CY, 0, 48)), true);
		// the drum's rings: one gap, then two, turning (every 2 beats)
		for (int i0 = 0; i0 < 7; i0++)
		{ int i = i0;
			double b = 2 + 2 * i, a = 90 + 135 * i; double[] gaps = i < 3 ? new[] { a } : new[] { a, a + 180 };
			at(b - 1.5, () => add(WDRing(CX, CY, b, 1.5, ez ? 460 : 520, 16, gaps, ez ? 88 : 72)));
		}
		// the spears: across, one lane a beat going down (from the left), then up (from the right); Normal from 0:11 two lanes 300 px apart
		for (int i0 = 0; i0 < 16; i0++)
		{ int i = i0;
			double b = 16 + i; bool second = i >= 8; int k = second ? 7 - (i - 8) : i;
			double y = 80 + 80 * k; var ys = ez || !second ? new[] { y } : new[] { y, y + (y < 360 ? 300 : -300) };
			at(b - 1, () => add(WDShots(ys.Select(_ => second ? W + 50 : -50.0).ToArray(), ys, second ? 180 : 0, b, 1, ez ? 1150 : 1500, 130)));
		}
		foreach (var b in seq(16, 32, 4)) { double a = 45 + 30 * b; at(b - 1.5, () => add(WDRing(CX, CY, b, 1.5, ez ? 460 : 520, 16, new[] { a, a + 180 }, ez ? 88 : 76))); }
		// the spears from above: two columns closing in to the middle, then from below opening out
		for (int i0 = 0; i0 < 16; i0++)
		{ int i = i0;
			double b = 32 + i; if (ez && i % 4 == 3) continue; bool up = i >= 8; int k = up ? 7 - (i - 8) : i;
			double x = 90 + 62 * k; var xs = new[] { x, W - x };
			at(b - 1, () => add(WDShots(xs, xs.Select(_ => CY).ToArray(), up ? -90 : 90, b, 1, ez ? 900 : 1200, 120)));
		}
		at(47, () => add(WDShots(new[] { CX }, new[] { CY }, 90, 47.5, 0.9, 1200, 120)));   // the kick at 0:21.5
		// the Sadu weave: a strip every 2 beats from the top, the gap zigzags like the weave; then from below; then standing strips from
		// the left and from the right; then from the top and the left in turn (a cross weave)
		{
			double[] zig = { 300, 450, 600, 750, 900, 750, 600, 450 }, zy = { 160, 260, 360, 460, 560, 460, 360, 260 };
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 48 + 2 * i; int dir = i < 4 ? 1 : -1; double gx = zig[i], gw = ez ? 265 : 240;
				at(b - 1, () => add(WDStrip(false, dir, gx - gw / 2, gx + gw / 2, b, 1, ez ? 600 : 760, 34)));
			}
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 64 + 2 * i; int dir = i < 4 ? 1 : -1; double gy = zy[i], gw = ez ? 250 : 220;
				at(b - 1, () => add(WDStrip(true, dir, gy - gw / 2, gy + gw / 2, b, 1, ez ? 850 : 1080, 34)));
			}
			// Normal: a spear between the strips (the odd beats), across the way they move
			if (!ez) for (int i0 = 0; i0 < 16; i0++)
			{ int i = i0;
				double b = 49 + 2 * i; double[] lane = { 180, 540, 360, 620, 100, 450 }; double[] col = { 320, 960, 640, 160, 1120, 480 };
				if (i < 8) { double y = lane[i % 6]; at(b - 1, () => add(WDShots(new[] { i % 2 == 0 ? -50.0 : W + 50 }, new[] { y }, i % 2 == 0 ? 0 : 180, b, 1, 1500, 130))); }
				else { double x = col[i % 6]; at(b - 1, () => add(WDShots(new[] { x }, new[] { i % 2 == 0 ? -50.0 : H + 50 }, i % 2 == 0 ? 90 : -90, b, 1, 1200, 120))); }
			}
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 80 + i; bool v = i % 2 == 1; if (ez && v) continue; double gc = v ? zy[(i * 3) % 8] : zig[(i * 3) % 8], gw = ez ? 270 : 240;
				at(b - 1, () => add(WDStrip(v, 1, gc - gw / 2, gc + gw / 2, b, 1, v ? 1080 : (ez ? 600 : 760), 34)));
			}
		}
		// the break (0:39.6): the sword's slashes on the kicks (the even beats), the last ones crossed
		{
			var sl = new (double b, double[] a)[] { (88, new[] { 35.0 }), (90, new[] { -35.0 }), (92, new[] { 90.0 }), (94, new[] { 0.0 }), (96, new[] { 35.0, -35 }), (98, new[] { 90.0, 0 }), (100, new[] { 60.0, -60 }) };
			foreach (var (b, aa) in sl)
			{
				if (ez && (b == 90 || b == 94 || b == 98)) continue;
				var angs = ez ? new[] { aa[0] } : aa;
				foreach (var a in angs) at(b - 1.5, () => add(WDSlash(CX, CY, a, ez ? 110 : 130, b, 1.5, 0.6)));
			}
			at(88, () => add(WDFx(88, 0.15, 6, 0.4)));
		}
		at(102, () => add(WDFx(102, 0.3, 10, 1)));

		// ===== 2. the raid (dust, 0:45.8-1:27.7) =====
		{
			double[] ln = { 70, 186, 302, 418, 534, 650 };
			// the herd across: half the lanes in turn every 2 beats (from the left; a lone horse back between them at 0:48.5), then from
			// 0:49.4 every beat (from the right)
			for (int i0 = 0; i0 < 13; i0++)
			{ int i = i0;
				bool fast = i >= 3; double b = fast ? 110 + (i - 3) : 104 + 2 * i;
				int dir = fast ? -1 : 1; var lc = ln.Where((_, k) => k % 2 == i % 2).ToArray();
				at(b - 1.1, () => add(WDHerd(false, dir, lc, b, 1.1, ez ? 1100 : 1500)));
				if (!ez && !fast && i >= 2) { double b1 = b + 1; var l1 = new[] { ln[(i * 2 + 1) % 6] }; at(b1 - 1.1, () => add(WDHerd(false, -dir, l1, b1, 1.1, 1500))); }
			}
			// the arrows: rows that fall slanted with the wind, the row moved half a gap each beat; the wind turns at 0:56. Normal: two lanes
			// of horses across every 4 beats too
			for (int i0 = 0; i0 < 16; i0++)
			{ int i = i0;
				double b = 120 + i; double ang = i < 8 ? 72 : 108, off = (i % 2) * 80;
				var xs = new List<double>(); for (double x = -480 + off * (ez ? 1 : 0.875); x < W + 480; x += ez ? 160 : 140) xs.Add(x);
				var xa = xs.ToArray(); at(b - 0.9, () => add(WDShots(xa, xa.Select(_ => CY).ToArray(), ang, b, 0.9, ez ? 850 : 1150, 46, true)));
				if (!ez && i % 4 == 2) { var l2 = new[] { ln[(i / 4) % 3], ln[(i / 4) % 3 + 3] }; int d2 = i < 8 ? 1 : -1; at(b - 1.1, () => add(WDHerd(false, d2, l2, b, 1.1, 1500))); }
			}
			// the herd down: half the columns in turn every 2 beats, from the top, then from below; Normal: a lone horse the other way between
			double[] cl = { 80, 240, 400, 560, 720, 880, 1040, 1200 };
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 136 + 2 * i; int dir = i < 4 ? 1 : -1; var lc = cl.Where((_, k) => k % 2 == i % 2).ToArray();
				at(b - 1.1, () => add(WDHerd(true, dir, lc, b, 1.1, ez ? 900 : 1100)));
				if (!ez) { double b1 = b + 1; var l1 = new[] { cl[(i * 3 + 1) % 8] }; at(b1 - 1.1, () => add(WDHerd(true, -dir, l1, b1, 1.1, 1100))); }
			}
			// both: two lanes of horses on the even beats (the pair moves down), straight arrows on the odd beats; Normal: another pair on the odd beats
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 152 + 2 * i; var lc = new[] { ln[i % 6], ln[(i + 3) % 6] };
				at(b - 1.1, () => add(WDHerd(false, i % 4 < 2 ? 1 : -1, lc, b, 1.1, ez ? 1100 : 1500)));
				if (!ez) { double b1 = b + 1; var l1 = new[] { ln[(i + 1) % 6], ln[(i + 4) % 6] }; at(b1 - 1.1, () => add(WDHerd(false, i % 4 < 2 ? -1 : 1, l1, b1, 1.1, 1500))); }
				double ba = b + 1, off = (i % 2) * 100; var xs = new List<double>(); for (double x = 40 + off; x < W; x += 200) xs.Add(x); var xa = xs.ToArray();
				at(ba - 0.9, () => add(WDShots(xa, xa.Select(_ => CY).ToArray(), 90, ba, 0.9, ez ? 850 : 1100, 46, true)));
			}
		}
		// the riders' circle (1:14): eight horses gallop round the middle, closing in; spears thrown through the middle every 2 beats
		// (Normal: from 1:17 two side by side and one across between them)
		at(167, () => add(WDCircle(ez ? 6 : 8, CX, CY, new[] { 168.0, 176, 184 }, new[] { 320.0, 230, 320 }, new[] { 168.0, 175, 176 }, new[] { 30.0, 30, -34 }, 168, 184)));
		for (int i0 = 0; i0 < 8; i0++)
		{ int i = i0;
			double b = 169 + 2 * i, a = new[] { 0.0, 90, 45, 135 }[i % 4];
			double c = JS.cos(WDRad(a)), s = JS.sin(WDRad(a));
			var px = (!ez && b >= 177) ? new[] { CX, CX - s * 160 } : new[] { CX }; var py = (!ez && b >= 177) ? new[] { CY, CY + c * 160 } : new[] { CY };
			at(b - 1, () => add(WDShots(px, py, a + (i % 2 == 0 ? 0 : 180), b, 1, ez ? 1000 : 1300, 150)));
			if (!ez && b >= 173 && b < 183) { double b1 = b + 1, a1 = a + 90; at(b1 - 1, () => add(WDShots(new[] { CX }, new[] { CY }, a1, b1, 1, 1300, 150))); }
		}
		// the break (1:22.3): the falcon dives on the kicks, from 1:25 two at once (mirrored)
		{
			var dv = new (double b, double x0, double y0, double xc, double yc, double x1, double y1)[] {
				(184, -80, 120, 640, 900, 1360, 120), (186, 1360, 600, 640, -180, -80, 600), (188, 200, -80, 1300, 360, 200, 800),
				(190, 1080, -80, -20, 360, 1080, 800), (192, -80, 360, 640, 820, 1360, 360), (194, -80, 80, 900, 360, -80, 640) };
			foreach (var d in dv)
			{
				if (ez && (d.b == 186 || d.b == 190)) continue;
				at(d.b - 1.5, () => add(WDFalcon(d.x0, d.y0, d.xc, d.yc, d.x1, d.y1, d.b, 1.7, 1.5)));
				if (!ez && d.b >= 190) at(d.b - 1.5, () => add(WDFalcon(W - d.x0, H - d.y0, W - d.xc, H - d.yc, W - d.x1, H - d.y1, d.b, 1.7, 1.5)));
			}
		}
		at(196, () => add(WDFx(196, 0.12, 12, 1.2)));

		// ===== 3. the promise (dawn, 1:27.7-3:04) =====
		// the ardah: the sword walls from the bottom and the top, the way through moves with the dance on every beat (the half beats in the
		// roll at 1:31), the other way from 1:31
		{
			var st = new List<double>(); var dp = new List<double>();
			for (double b = 197; b < 211; b += (b >= 204 && b < 208 ? 0.5 : 1)) { st.Add(b); dp.Add(b < 204 ? 0.5 : b < 208 ? -0.22 : -0.4); }
			at(194.5, () => add(WDWalls(false, 16, CY, ez ? 160 : 180, 0.45, ez ? 270 : 225, st.ToArray(), dp.ToArray(), 0, 196, 211)));
			// then lying: from the left and the right
			var st2 = seq(214, 228, 1); var dp2 = st2.Select(b => b < 221 ? 0.45 : -0.45).ToArray();
			at(211.5, () => add(WDWalls(true, 9, CX, ez ? 210 : 240, 0.6, ez ? 290 : 250, st2, dp2, 0, 213, 228)));
		}
		// the banners slam down one a beat, marching to the right, then to the left; Normal: from both sides at once
		for (int i0 = 0; i0 < 8; i0++)
		{ int i = i0;
			double b = 228 + i; double x = 110 + 150 * i;
			at(b - 1, () => add(WDBanner(x, i % 2 == 0 ? 1 : -1, b, 1, 2.2)));
		}
		for (int i0 = 0; i0 < 8; i0++)
		{ int i = i0;
			double b = 236 + i; double x = W - 110 - 150 * i;
			at(b - 1, () => add(WDBanner(x, i % 2 == 0 ? -1 : 1, b, 1, 2.2)));
			if (!ez && i < 4) { double x2 = 110 + 150 * i; at(b - 1, () => add(WDBanner(x2, 1, b, 1, 2.2))); }
		}
		// the crossed swords in the middle, turning (the other way at 1:53); spears across every 2 beats
		at(242, () => add(WDCross(CX, CY, ez ? 2 : 4, 820, new[] { 244.0, 252, 253, 262, 263 }, new[] { 26.0, 26, -30, -30, 34 }, 20, 244, 280)));
		for (int i0 = 0; i0 < 9; i0++)
		{ int i = i0;
			double b = 245 + 2 * i; double y = i % 2 == 0 ? 110 : 610;
			at(b - 1, () => add(WDShots(new[] { i % 4 < 2 ? -50.0 : W + 50 }, new[] { y }, i % 4 < 2 ? 0 : 180, b, 1, ez ? 1100 : 1400, 140)));
		}
		// the arrows rain past the turning swords (2:01)
		for (int i0 = 0; i0 < 8; i0++)
		{ int i = i0;
			double b = 264 + 2 * i; double ang = i < 4 ? 80 : 100, off = (i % 2) * 110;
			var xs = new List<double>(); for (double x = -300 + off; x < W + 300; x += 220) xs.Add(x); var xa = xs.ToArray();
			at(b - 1, () => add(WDShots(xa, xa.Select(_ => CY).ToArray(), ang, b, 1, ez ? 800 : 1000, 46, true)));
		}
		// the break (2:05.1): the drum again, its rings on the kicks; Normal: a slash every 2 beats
		at(278, () => add(WDDrum(CX, CY, 280, 294)));
		for (int i0 = 0; i0 < 7; i0++)
		{ int i = i0;
			double b = 281 + 2 * i, a = 30 + 60 * i;
			at(b - 1.5, () => add(WDRing(CX, CY, b, 1.5, ez ? 520 : 640, 16, new[] { a, a + 180 }, ez ? 85 : 62)));
		}
		if (!ez) foreach (var (b, a) in new[] { (282.0, 0.0), (284, 30), (286, 90), (288, -30), (290, 0), (292, 90) }) at(b - 1.5, () => add(WDSlash(CX, CY, a, 120, b, 1.5, 0.5)));
		// the stampede again: two lanes of three in turn every beat (from the left, then from the right); then down with the arrows
		{
			double[] ln = { 70, 186, 302, 418, 534, 650 };
			for (int i0 = 0; i0 < 16; i0++)
			{ int i = i0;
				double b = 294 + i; int dir = i < 8 ? 1 : -1; var lc = new[] { ln[i % 3], ln[i % 3 + 3] };
				at(b - 1.25, () => add(WDHerd(false, dir, lc, b, 1.25, ez ? 1000 : 1350)));
			}
			double[] cl = { 80, 240, 400, 560, 720, 880, 1040, 1200 };
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 310 + 2 * i; var lc = cl.Where((_, k) => k % 2 == i % 2).ToArray();
				at(b - 1.25, () => add(WDHerd(true, i < 4 ? 1 : -1, lc, b, 1.25, ez ? 700 : 950)));
				if (ez) continue;
				double ba = b + 1, ang = i % 2 == 0 ? 15 : 165; var ys = new List<double>(); for (double y = -200 + (i % 2) * 90; y < H + 200; y += 180) ys.Add(y); var ya = ys.ToArray();
				at(ba - 0.9, () => add(WDShots(ya.Select(_ => CX).ToArray(), ya, ang, ba, 0.9, 1100, 46, true)));
			}
		}
		// the Sadu weave again: from the top and the bottom in turn, then from the left and the right in turn
		{
			double[] zig = { 400, 500, 600, 700, 800, 900, 800, 700, 600, 500 };
			for (int i0 = 0; i0 < 8; i0++)
			{ int i = i0;
				double b = 326 + i; double gx = zig[i % 10], gw = ez ? 300 : 250;
				at(b - 1, () => add(WDStrip(false, i % 2 == 0 ? 1 : -1, gx - gw / 2, gx + gw / 2, b, 1, ez ? 560 : 720, 34)));
			}
			double[] zy = { 200, 280, 360, 440, 520, 440, 360, 280, 200, 280 };
			for (int i0 = 0; i0 < 10; i0++)
			{ int i = i0;
				double b = 334 + i; double gy = zy[i], gw = ez ? 270 : 230;
				at(b - 1, () => add(WDStrip(true, i % 2 == 0 ? 1 : -1, gy - gw / 2, gy + gw / 2, b, 1, ez ? 800 : 1050, 34)));
			}
		}
		// the ardah again, faster: standing walls, then lying ones (2:33.6, 2:40.7)
		{
			var st = seq(345, 359, 1); var dp = st.Select(b => b < 352 ? -0.6 : 0.6).ToArray();
			at(342.5, () => add(WDWalls(false, 16, CY, ez ? 170 : 200, 0.5, ez ? 280 : 220, st, dp, 1, 344, 359)));
			var st2 = seq(362, 376, 1); var dp2 = st2.Select(b => b < 369 ? 0.5 : -0.5).ToArray();
			at(359.5, () => add(WDWalls(true, 9, CX, ez ? 220 : 250, 0.6, ez ? 300 : 250, st2, dp2, 2, 361, 376)));
		}
		// the break (2:47.9): the falcons, two at once (mirrored) on the kicks
		{
			var dv = new (double b, double x0, double y0, double xc, double yc, double x1, double y1)[] {
				(377, -80, 120, 640, 900, 1360, 120), (379, 200, -80, 1300, 360, 200, 800), (381, -80, 360, 640, 820, 1360, 360),
				(383, 1360, 600, 640, -180, -80, 600), (385, 1080, -80, -20, 360, 1080, 800), (387, -80, 80, 900, 360, -80, 640) };
			foreach (var d in dv)
			{
								at(d.b - 1.5, () => add(WDFalcon(d.x0, d.y0, d.xc, d.yc, d.x1, d.y1, d.b, 1.3, 1.25)));
				if (!ez) at(d.b - 1.5, () => add(WDFalcon(W - d.x0, H - d.y0, W - d.xc, H - d.yc, W - d.x1, H - d.y1, d.b, 1.3, 1.25)));
			}
		}
		// the end (2:53): the drum's rings every 2 beats and spears across between them; then the last slashes, crossed
		at(388, () => add(WDDrum(CX, CY, 390, 404)));
		for (int i0 = 0; i0 < 7; i0++)
		{ int i = i0;
			double b = 391 + 2 * i, a = 60 * i;
			at(b - 1.5, () => add(WDRing(CX, CY, b, 1.5, ez ? 520 : 640, 16, new[] { a, a + 180 }, ez ? 85 : 64)));
			if (ez) continue;
			double bs = b + 1, y = i % 2 == 0 ? 120 : 600;
			at(bs - 1, () => add(WDShots(new[] { i % 2 == 0 ? -50.0 : W + 50 }, new[] { y }, i % 2 == 0 ? 0 : 180, bs, 1, 1400, 140)));
		}
		foreach (var (b, aa) in new (double, double[])[] { (405, new[] { 35.0 }), (407, new[] { -35.0 }), (409, new[] { 35.0, -35 }), (411, new[] { 90.0, 0 }) })
		{
			var angs = ez ? new[] { aa[0] } : aa;
			foreach (var a in angs) at(b - 1.5, () => add(WDSlash(CX, CY, a, ez ? 110 : 130, b, 1.5, 0.6)));
		}
		at(412, () => add(WDFx(412, 0.4, 12, 1.4)));
		at(411.6, () => add(WDEnd(411.8, 414)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
