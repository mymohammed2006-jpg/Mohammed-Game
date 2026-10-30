using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// C'est la vie (main level 30): the events of the whole level. Beat b is 0:07.4905 + b * 60/130 s of the song.
// Normal is about as hard as level 2 (Temple); each part a little harder than the one before. Easy has fewer and slower things.
public static partial class G
{
	static readonly int _cvb = DodgeBuild("L30", () => buildCV());
	static List<Ev> buildCV()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false, bool keepNew = false) => E.Add(new Ev { b = b, f = f, keep = keep, keepNew = keepNew });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(CVStage(B(392))), true);

		// ===== part 1: the sunset (0:07.5 - 1:10.3) =====
		// the lanterns go up (0:07.5): one a beat, the gulls every 2 bars
		{
			double[] xs = { 640, 420, 860, 260, 1020, 540, 740, 340, 940, 180, 1100, 480, 800, 600, 380, 900 };
			double sp = ez ? 190 : 240, tl = ez ? 1.5 : 1.2;
			void L(double x, double bb, double s2 = 0) => at(bb - tl, () => add(CVLantern(x, -1, bb, tl, s2 > 0 ? s2 : sp)));
			// 0:08 one at a time, from side to side
			int n = 0; for (double b = 2; b < 10; b += ez ? 2 : 1) L(xs[n++ % xs.Length], b);
			// 0:11.6 two at once, mirrored, closing in to the middle
			n = 0; for (double b = 10; b < 18; b += 2) { double d = 520 - 110 * n++; L(640 - d, b); if (!ez || n % 2 == 0) L(640 + d, b); }
			// 0:15.3 a staircase that walks across and back on the half beats
			n = 0; for (double b = 18; b < 26; b += ez ? 1 : 0.5) { int k = n++ % 8; double x = k < 4 ? 220 + 280 * k : 1060 - 280 * (k - 4) - 140; L(x, b); }
			// 0:19 faster, one a beat and more between (the drums are coming)
			n = 0; for (double b = 26; b < 36; b += ez ? 2 : 1) L(xs[(n++ * 3) % xs.Length], b, sp + 30);
			if (!ez) for (double b = 28.5; b < 36; b += 2) L(xs[(n++ * 5) % xs.Length] + 60, b, sp + 30);
			double[][] gl = { new[] { 8.0, 170, 1 }, new[] { 16.0, 300, -1 }, new[] { 24.0, 220, 1 }, new[] { 32.0, 130, -1 } };
			foreach (var q in gl) { double b = q[0], y = q[1]; int d = (int)q[2]; at(b - 1.5, () => add(CVGull(y, d, b, 1.5, ez ? 320 : 400))); }
		}
		// the darbuka (0:24.1): a ring on every bar's dum, the notes fly on the tak
		at(34, () => add(CVDrum(640, 360, 35, 72)));
		at(36, () => add(CVFx(36, 0.15, 5, 0.6)));
		{
			double tl = ez ? 2 : 1.5, sp = ez ? 190 : 262, gw = ez ? 150 : 82; int m = 0;
			for (double b = 36; b < 72; b += 4)
			{
				double bb = b, ga = 90 + 50 * m; m++; double rs = bb >= 52 && m % 2 == 0 ? -sp * 1.1 : sp;
				at(bb - tl, () => add(CVRingOut(640, 360, bb, tl, rs, ga, gw)));
				if (ez) { if (m % 2 == 0) { double a0 = 36 * m; at(bb + 1.25, () => add(CVSpray(640, 360, 5, a0, bb + 2, 0.75, 180, 80, 0))); } continue; }
				{ double a0 = 30 * m; at(bb + 0.75, () => add(CVSpray(640, 360, 6, a0, bb + 1.5, 0.75, 242, 80, 0))); }
				{ double a0 = 30 * m + 30; at(bb + 1.25, () => add(CVSpray(640, 360, 6, a0, bb + 2, 0.75, 242, 80, 0))); }
				if (bb >= 52) { double a0 = 30 * m + 15; at(bb + 2.75, () => add(CVSpray(640, 360, 5, a0, bb + 3.5, 0.75, 242, 80, 0))); }
			}
		}
		// the clapping (0:40.7): the hands clap on beats 2 and 4, rows first, then rows and columns, a cross at the end
		{
			double tl = ez ? 2 : 1.5, w = ez ? 100 : 110;
			double[] ys = { 200, 520, 360, 260, 460, 160, 560, 360 }, xs = { 320, 960, 640, 480, 800, 240, 1040, 640 };
			int k = 0;
			for (double b = 73; b < 104; b += ez ? 4 : 2)
			{
				double bb = b; bool vert = bb >= 89 && k % 2 == 1; double pos = vert ? xs[k % xs.Length] : ys[k % ys.Length]; k++;
				at(bb - tl, () => add(CVClap(vert, pos, w, bb, tl)));
				if (!ez && bb >= 101) { double p2 = xs[(k + 3) % xs.Length]; at(bb - tl, () => add(CVClap(true, p2, w, bb, tl))); }
				// the claps throw sparks (Normal, the second half of each 8 bars)
				if (!ez && ((bb >= 81 && bb < 88) || (bb >= 97 && bb < 101)))
				{
					double sx = vert ? pos : 640, sy = vert ? 360 : pos, a0 = 22.5 * k;
					at(bb - 0.5, () => add(CVSpray(sx, sy, 6, a0, bb + 0.25, 0.75, 220, 60, 2, 1.4)));
				}
			}
		}
		// the music staff: "la la la" (0:55.5): a run of 5 notes each bar, bar lines with a gap
		at(102, () => add(CVStaff(103, 136)), false, true);   // keepNew: the last notes still cross the screen after the save at 1:10.3
		at(104, () => add(CVFx(104, 0.15, 4, 0.6)));
		{
			int[][] runs = { new[] { 1, 2, 3, 4, 5 }, new[] { 7, 6, 5, 4, 3 }, new[] { 2, 3, 4, 5, 6 }, new[] { 6, 5, 4, 3, 2 } };
			int[] gk = { 4, 2, 6, 3, 5, 1, 7, 4 };
			double sp = ez ? 240 : 318, tl = 1;
			for (int m = 0; m < 8; m++)
			{
				double b = 104 + 4 * m; var r = runs[m % 4];
				bool kn = b >= 124; for (int i = 0; i < 5; i++) { if (ez && i % 2 == 1) continue; double bb = b + 0.5 * i, k = r[i]; at(bb - tl, () => add(CVSNote(k, bb, tl, sp)), false, kn); }
				if (!ez) { double k1 = 8 - r[4], k2 = 8 - r[2]; at(b + 3 - tl, () => add(CVSNote(k1, b + 3, tl, sp)), false, kn); at(b + 3.5 - tl, () => add(CVSNote(k2, b + 3.5, tl, sp)), false, kn); }
				if (!ez || m % 2 == 0) { double bb = b + 2.5, gy = CVStaffY(gk[m]); at(bb - 1.5, () => add(CVBarLine(gy, ez ? 170 : 130, bb, 1.5, sp)), false, kn); }
			}
		}

		// ===== part 2: the night dance (1:10.3 - 2:05.6) =====
		at(136, () => add(CVFx(136, 0.1, 3, 0.6)));
		// the waves under the moon: the left and the right half in turn, shooting stars above
		{
			double[] yt = ez ? new double[] { 520, 540, 510, 530 } : new double[] { 470, 500, 450, 490, 460, 510, 455, 495 }; double[] sx = { 200, 1080, 420, 860, 640, 300, 980, 540 };
			double tl = ez ? 2 : 1.5; int k = 0;
			for (double b = 136; b < 150; b += ez ? 4 : 2)
			{
				double bb = b, y = yt[k % yt.Length]; bool left = k % 2 == 0; k++;
				at(Math.Max(134.5, bb - tl), () => add(CVWave(left ? 0 : 640, left ? 640 : W, y, bb, tl, 1, ez ? 0.5 : 0.6, 1)));
			}
			for (double b = 150; b < 162; b += ez ? 4 : 2)
			{
				double bb = b, sl = (k % 2 == 0 ? 1 : -1) * (ez ? 0.1 : 0.16), y = ez ? 540 : 500; k++;
				at(bb - tl, () => add(CVWave(0, W, y, bb, tl, 1, ez ? 0.5 : 0.6, 1, sl)));
			}
			k = 0;
			for (double b = 137; b < 164; b += ez ? 4 : 2)
			{
				double bb = b, x0 = sx[k % sx.Length], ang = x0 < 640 ? 58 : 122; k++;
				at(bb - tl, () => add(CVShoot(x0, -20, ang, bb, tl, ez ? 300 : 400)));
			}
		}
		// the zellige dance floor (1:23.2): a new pattern on every beat
		at(162, () => add(CVFloorBg(163, 200)));
		at(164, () => add(CVFx(164, 0.15, 5, 0.6)));
		{
			Func<int, int, bool>[] pats(int n)
			{
				int q = n % 4;
				int ring(int i, int j) => Math.Max((int)Math.Floor(Math.Abs(i - 3.5)), Math.Abs(j - 2));
				switch (n / 4)
				{
					case 0: return new Func<int, int, bool>[] { (i, j) => (i + j) % 4 == q };
					case 1: return new Func<int, int, bool>[] { (i, j) => (i + j) % 2 == q % 2 };
					case 2: return new Func<int, int, bool>[] { (i, j) => i / 2 == q };
					case 3: return new Func<int, int, bool>[] { (i, j) => ring(i, j) == q };
					case 4: return new Func<int, int, bool>[] { (i, j) => (i - j + 8) % 4 == 3 - q };
					case 5: return new Func<int, int, bool>[] { q == 0 ? (i, j) => j == 0 || j == 4 : q == 1 ? (i, j) => j == 1 || j == 3 : q == 2 ? (i, j) => j == 2 : (i, j) => (i + j) % 2 == 0 };
					case 6: return new Func<int, int, bool>[] { (i, j) => (7 - i) / 2 == q };
					case 7: return new Func<int, int, bool>[] { q % 2 == 0 ? (i, j) => (i + j) % 4 == 0 || (i - j + 8) % 4 == 0 : (i, j) => (i + j) % 4 == 2 || (i - j + 8) % 4 == 2 };
					default: return new Func<int, int, bool>[] { (i, j) => ring(i, j) == 3 - q };
				}
			}
			double tl = ez ? 1.5 : 1, dur = ez ? 0.55 : 0.6;
			for (int n = 0; n < 36; n += ez ? 2 : 1) { double bb = 164 + n; ulong m = CVPat(pats(n)[0]); at(bb - tl, () => add(CVFloor(m, bb, tl, dur))); }
		}
		// the two dancers (1:39.8): they turn round the middle with their ribbons; petals fly from them
		{
			double ow = ez ? 16 : 24, sw = ez ? 24 : 50, L = ez ? 95 : 150;
			at(199.5, () => add(CVDancer(0, 200, 228, ow, sw, L, 250, 50)));
			at(199.5, () => add(CVDancer(180, 200, 228, ow, sw, L, 250, 50)));
			for (double b = ez ? 208 : 203; b < 228; b += ez ? 8 : 2)
			{
				double bb = b;
				foreach (var ph in ez ? new[] { 0.0, 180 } : new[] { (bb - 203) % 4 == 0 ? 0.0 : 180 })
				{
					var d = CVDancerAt(ph, 200, ow, sw, 250, 50, bb); double a0 = bb * 13 + ph;
					at(bb - 1, () => add(CVSpray(d.x, d.y, ez ? 5 : 7, a0, bb, 1, ez ? 170 : 200, 26, 1)));
				}
			}
		}
		// the confetti cannons (1:52.7): left and right in turn, the streamers fall between
		{
			double tl = ez ? 1.5 : 1;
			double[] bases = { -58, -66, -50, -62, -54 };
			double shotBase(int m) => bases[m % bases.Length];
			Func<double, int, double> aim = (bt, sd) =>
			{
				int m = (int)Math.Floor((bt - 227) / 2); if (sd == 0 && m % 2 == 1) m++; if (sd == 1 && m % 2 == 0) m++;
				double a = CVRad(shotBase(Math.Max(0, m))); return sd == 0 ? a : Math.PI - a;
			};
			at(226, () => add(CVCannons(227, 256, aim)), false, true);   // keepNew: the last shots still fly after the save at 2:05.6
			at(228, () => add(CVFx(228, 0.15, 5, 0.6)));
			int[] off = ez ? new[] { -12, 0, 12 } : new[] { -20, -12, -4, 4, 12, 20 };
			for (int m = 0; m < 14; m++)
			{
				if (ez && m % 2 == 1) continue;
				double bb = 228 + 2 * m; int sd = m % 2; double a = CVRad(shotBase(m));
				for (int i = 0; i < off.Length; i++)
				{
					double q = a + CVRad(off[i]), v = 740 + 36 * i * (ez ? 2 : 1); if (sd == 1) q = Math.PI - q;
					double x0 = (sd == 0 ? 60 : W - 60) + JS.cos(q) * 76, y0 = H - 30 + JS.sin(q) * 76, vx = JS.cos(q) * v, vy = JS.sin(q) * v;
					at(bb - tl, () => add(CVConfetti(x0, y0, vx, vy, bb, tl)), false, bb >= 248);
				}
			}
			double[] cx = { 400, 880, 640, 240, 1040, 520, 760 }; int k = 0;
			for (double b = ez ? 230 : 229; b < 256; b += ez ? 4 : 2) { double bb = b, x = cx[k++ % cx.Length]; at(bb - tl, () => add(CVStreamer(x, bb, tl, ez ? 240 : 360)), false, bb >= 248); }
		}

		// ===== part 3: the fireworks and the morning (2:05.6 - 3:07) =====
		at(256, () => add(CVFx(256, 0.12, 4, 0.6)));
		// the fireworks: a rocket every 2 beats, circles, stars, hearts, double rings
		{
			double[][] pt = { new[] { 320.0, 220 }, new[] { 960.0, 240 }, new[] { 640.0, 170 }, new[] { 420.0, 430 }, new[] { 860.0, 420 }, new[] { 640.0, 300 }, new[] { 240.0, 360 }, new[] { 1040.0, 330 } };
			double tl = 1; int k = 0;
			for (double b = 258; b < 292; b += ez ? 4 : 2)
			{
				double bb = b; var p = pt[k % pt.Length]; int sh = k % 4; k++;
				int n = sh == 0 ? (ez ? 8 : 12) : sh == 2 ? (ez ? 10 : 14) : (ez ? 10 : 16);
				at(bb - 1 - tl, () => add(CVRocket(p[0], p[1], bb, tl, sh, n, ez ? 160 : 226, ez ? 1.3 : 1.8)));
			}
			if (!ez) { k = 3; for (double b = 265; b < 292; b += 2) { double bb = b; var p = pt[(k += 3) % pt.Length]; at(bb - 1 - tl, () => add(CVRocket(W - p[0], p[1] + 40, bb, tl, 0, 10, 226, 1.8))); } }
		}
		// the big wheel (2:21.0)
		{
			double rw = ez ? 10 : 24; double[] br = ez ? new[] { 304.0, 320 } : new[] { 296.0, 300, 304, 308, 312, 316, 320, 324 };
			var sb = new List<double>(); for (double b = ez ? 299 : 293; b < 328; b += ez ? 8 : 2) sb.Add(b);
			var sba = sb.ToArray();
			at(290, () => add(CVWheelFrame(292, 328, rw, 310, br)));
			at(290, () => add(CVWheel(292, 328, rw, 310, br, sba)));
			at(292, () => add(CVFx(292, 0.12, 4, 0.6)));
			if (!ez) foreach (var b in new[] { 298.0, 302, 306, 310, 314, 318, 322, 326 }) { double bb = b, a0 = CVWheelRot(bb, 292, rw, 310); at(bb - 1, () => add(CVSpray(640, 360, 8, a0, bb, 1, 220, 250, 2))); }
		}
		// the lanterns come down (2:38.9): the night is ending
		{
			double[] xs = { 300, 980, 640, 160, 1120, 460, 820, 560, 720, 240, 1040, 400, 880 };
			double sp = ez ? 180 : 275, tl = ez ? 1.5 : 1.2; int n = 0;
			for (double b = 329; b < 344; b += ez ? 2 : 1) { double x = xs[n++ % xs.Length], bb = b; at(bb - tl, () => add(CVLantern(x, 1, bb, tl, sp))); }
			if (!ez) for (double b = 332.5; b < 344; b += 1) { double x = xs[(n++ * 7) % xs.Length] + 70, bb = b; at(bb - tl, () => add(CVLantern(x, 1, bb, tl, sp))); }
			at(330.5, () => add(CVGull(560, 1, 332, 1.5, ez ? 320 : 400)));
			if (!ez) at(338.5, () => add(CVGull(600, -1, 340, 1.5, 400)));
		}
		// the whole party (2:46.3): the floor again, with the cannons
		at(342, () => add(CVFloorBg(343, 360)));
		at(344, () => add(CVFx(344, 0.15, 5, 0.6)));
		{
			double tl = ez ? 1.5 : 1, dur = ez ? 0.6 : 0.75;
			for (int n = 0; n < 16; n += ez ? 2 : 1)
			{
				double bb = 344 + n; int q = n % 4;
				ulong m = n < 8 ? CVPat((i, j) => (i + j) % 4 == q) : CVPat((i, j) => (i + j) % 2 == q % 2 && (q < 2 ? i < 4 : i >= 4));
				at(bb - tl, () => add(CVFloor(m, bb, tl, dur)));
			}
			Func<double, int, double> aim = (bt, sd) => sd == 0 ? CVRad(-60) : Math.PI - CVRad(-60);
			at(342, () => add(CVCannons(343, 360, aim)));
			for (double b = 346; b < 360; b += ez ? 8 : 4)
				for (int sd = 0; sd < 2; sd++)
					for (int i = 0; i < (ez ? 3 : 4); i++)
					{
						double bb = b, q = CVRad(-60 + (ez ? 14 : 10) * (i - 1.5)), v = 780 + 30 * i; if (sd == 1) q = Math.PI - q;
						double x0 = (sd == 0 ? 60 : W - 60) + JS.cos(q) * 76, y0 = H - 30 + JS.sin(q) * 76, vx = JS.cos(q) * v, vy = JS.sin(q) * v;
						at(bb - tl, () => add(CVConfetti(x0, y0, vx, vy, bb, tl)));
					}
		}
		// the words: C'EST LA VIE (2:53.6): a letter falls on every beat and bounces, "la la la", then they fall away one by one
		{
			string[] ch = { "C", "E", "S", "T", "L", "A", "V", "I", "E" };
			double[] x = { 140, 250, 360, 470, 640, 750, 920, 1030, 1140 }, y = { 250, 250, 250, 250, 360, 360, 470, 470, 470 };
			double hw = ez ? 32 : 36, hh = ez ? 38 : 42;
			for (int i = 0; i < 9; i++) { int i2 = i; double bIn = 360 + i, bOut = 372 + 0.5 * i; at(bIn - 1, () => add(CVLetter(ch[i2], x[i2], bIn, bOut, y[i2], 1, hw, hh))); }
			at(360, () => add(CVApos(195, 360, 372, 250)));
			if (!ez) foreach (var b in new[] { 369.0, 370, 371 }) { double bb = b, a0 = 30 * b; at(bb - 0.75, () => add(CVSpray(695, 360, 6, a0, bb, 0.75, 200, 60, 0))); }
		}
		// the morning (3:01): the circle closes in round the middle while the sun comes up; the sparks of the last fireworks
		at(375, () => add(CVClose(376, 388, 900, ez ? 210 : 170)));
		foreach (var b in ez ? new[] { 382.0, 386 } : new[] { 380.0, 382, 384, 386 }) { double bb = b, a0 = 30 * b; at(bb - 1, () => add(CVSpray(640, 360, ez ? 5 : 6, a0, bb, 1, 200, 20, 2, 0.9))); }
		at(388, () => add(CVFx(388, 0.45, 12, 1.2)));
		at(388, () => add(CVEnd(388, 391)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
