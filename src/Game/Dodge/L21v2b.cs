using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Barracuda, the new design (10-28): the events. The song's hits are my own onset map (low = the bass, sn = the snare roll), in beats.
public static partial class G
{
	// the bass hits (from 0:15), the snare roll (0:30-0:37) and the riffs
	static readonly double[] L21B_BASS1 = { 33.25, 35.75, 37.25, 38.25, 39, 39.75, 41.25, 43.75, 45.25, 46.25, 47, 47.75, 49.25, 51.75, 53.25, 54.25, 55, 55.75, 57.25, 59.75, 62.5, 63.25 };
	static readonly double[] L21B_SNARE = { 64.75, 65, 65.25, 65.75, 66.25, 66.5, 66.75, 67, 67.25, 67.5, 67.75, 68, 68.25, 68.5, 68.75, 69, 69.25, 69.5, 69.75, 70, 70.25, 70.5, 70.75, 71, 71.25, 71.5, 71.75, 72, 72.25, 72.5, 72.75, 73, 73.25, 73.5, 73.75, 74, 74.75, 75.75, 76, 76.5, 76.75, 77, 77.25, 77.5, 77.75, 78.75, 79.75 };
	static readonly double[] L21B_BASS2 = { 97.25, 98.25, 99.75, 101.25, 102.25, 103, 103.75, 105.25, 106.25, 107.75, 109.25, 110.25, 111.75 };
	static readonly double[] L21B_BASS3 = { 113.25, 114.25, 115.75, 117.25, 118.25, 119, 119.75, 129.25, 130.25, 131.75 };
	static readonly double[] L21B_RIFF1 = { 120.5, 121.25, 122, 123.75, 124.5, 125.25, 126 };
	static readonly double[] L21B_BASS4 = { 133.25, 134, 134.75, 135.5, 137, 138, 139.5, 141, 142, 143.5, 145, 146, 147.5, 149, 150, 150.75, 151.5, 152.25, 153, 153.75, 155.5 };
	static readonly double[] L21B_GROOVE = { 1, 2, 3.5, 5, 6, 6.75, 7.5 };   // part 3's bass in every 8 beats
	static readonly double[] L21B_RIFF2 = { 184.25, 185, 185.75, 187.5, 188.25, 189, 189.75 };

	static readonly int _l21b = DodgeBuild("L21", () => buildL21b());
	static List<Ev> buildL21b()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz z) => hazards.Add(z);
		const double CX = W / 2, CY = H / 2;
		at(0, () => add(L21bStage(B(230))), true);

		// ===== 1. found =====
		// the sonar arm (0:00-0:15): it turns back on the beats in rev (white arrows a beat before); one arm, two from 0:08 (Normal)
		{
			double[] rev = ez ? new[] { 8.0, 16, 22, 28 } : new[] { 8.0, 14, 18, 22, 25, 28, 30 };
			var kb = new List<double> { 2 }; var ka = new List<double> { -Math.PI / 2 }; double dir = 1, b = 2;
			foreach (var r in rev.Concat(new[] { 32.0 }))
			{
				double sp = ez ? (b < 16 ? 0.6 : 0.72) : (b < 16 ? 0.7 : 0.88); ka.Add(ka[^1] + dir * sp * (r - b)); kb.Add(r); dir = -dir; b = r;
			}
			var KB = kb.ToArray(); var KA = ka.ToArray(); var KA2 = KA.Select(a => a + Math.PI).ToArray();
			at(0, () => add(L21bSweep(CX, CY, KB, KA, 1, 48, 900, ez ? 16 : 20, rev, 0, 2, 30.5, 32, ez ? 0.2 : 0.26)));
			at(14, () => add(L21bSweep(CX, CY, KB, KA2, 1, 48, 900, ez ? 16 : 20, rev.Where(r => r > 16).ToArray(), 14, 16, 30.5, 32, ez ? 0.2 : 0.26)));
			// the contacts: on each beat a row of mines along the arm turns on as it gets there (white rings a beat before) and stays a while;
			// the rows take turns (in and out by half a step) so the way between them zigzags
			double[][] rr = ez ? new[] { new[] { 150.0, 310, 470 }, new[] { 230.0, 390, 550 } } : new[] { new[] { 120.0, 220, 320, 420, 520 }, new[] { 170.0, 270, 370, 470, 570 } };
			var mx = new List<double>(); var my = new List<double>(); var mb = new List<double>(); var me = new List<double>(); int ri = 0;
			for (double q = 4; q < 30; q += ez ? 1.5 : 1)
			{
				for (int arm = 0; arm < (q < 16 ? 1 : 2); arm++)
				{
					double a = L21bAt(q, KB, arm == 0 ? KA : KA2); var row = rr[ri++ % 2];
					foreach (var r in row) { mx.Add(CX + r * JS.cos(a)); my.Add(CY + r * JS.sin(a)); mb.Add(q); me.Add(Math.Min(31, q + (ez ? 3 : 4))); }
				}
			}
			at(2, () => add(L21bMines(mx.ToArray(), my.ToArray(), mb.ToArray(), me.ToArray(), ez ? 14 : 17, 1)));
		}
		// the depth charges on the bass (0:15-0:30): two big ones, then three in a row that step down or up; from 0:22 in pairs with bubble columns
		{
			var hits = L21B_BASS1; double tell = ez ? 1.75 : 1.25, band = ez ? 54 : 70, R = ez ? 105 : 125, wv = ez ? 480 : 650;
			(double y, double y2, double x)[] plan =
			{
				(240, -1, 420), (480, -1, 860), (170, -1, 640), (360, -1, 400), (550, -1, 880),
				(480, -1, 860), (240, -1, 420), (550, -1, 640), (360, -1, 880), (170, -1, 400),
				(200, 520, 640), (120, 600, 400), (170, 550, 860), (260, 460, 400), (360, -1, 640), (360, -1, 880),
				(520, 200, 640), (600, 120, 880), (550, 170, 400), (460, 260, 880), (360, -1, 640), (180, 540, 400),
			};
			for (int i = 0; i < hits.Length; i++)
			{
				double hb = hits[i]; var (y, y2, x) = plan[i]; int inBar = i < 20 ? (i < 10 ? i % 5 : (i - 10) % 6) : 9;
				bool big = i < 10 ? inBar < 2 : (i < 16 ? inBar < 2 : i < 18 || i == 21);
				
				double colB = big && (!ez || i >= 10) ? 2.0 : 0;
				at(hb - tell, () => add(L21bCharge(x, y, hb - tell, hb, band, R, ez ? 0.35 : 0.45, colB, ez ? 50 : 60, wv)));
				if (y2 > 0 && !ez) at(hb - tell, () => add(L21bCharge(W - x, y2, hb - tell, hb, band, R * 0.8, 0.35, 0, 50)));
			}
		}
		// the mine net on the snare roll (0:30-0:37): each snare hit lights the next line of the net (a snake through it), it turns slowly
		{
			int cols = 7, rows = ez ? 4 : 5; var path = new List<int>();
			for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) path.Add(r * cols + (r % 2 == 0 ? c : cols - 1 - c));
			var back = Enumerable.Range(0, cols).SelectMany(c => Enumerable.Range(0, rows).Select(r => (c % 2 == 0 ? rows - 1 - r : r) * cols + c)).ToList();
			var order = path.Concat(back.Skip(1)).ToList();
			var hits = ez ? L21B_SNARE.Where((v, i) => i % 2 == 0).ToArray() : L21B_SNARE;
			var la = new List<int>(); var lb = new List<int>(); var on = new List<double>(); var off = new List<double>();
			int n = cols * rows;
			for (int i = 0; i < hits.Length && i + 1 < order.Count; i++)
			{
				la.Add(order[i]); lb.Add(order[i + 1]); on.Add(hits[i]); off.Add(Math.Min(80, hits[i] + (ez ? 3.0 : 4.0)));
				// Normal: a second snake the other way round (from the far corner)
				la.Add(n - 1 - order[i]); lb.Add(n - 1 - order[i + 1]); on.Add(hits[i]); off.Add(Math.Min(80, hits[i] + (ez ? 3.0 : 4.0)));
			}
			foreach (var (cb, cy, cx) in ez ? new[] { (66.0, 180.0, 900.0), (70.0, 540.0, 380.0), (74.0, 360.0, 640.0), (78.0, 200.0, 380.0) } : new[] { (66.0, 180.0, 900.0), (68.0, 470.0, 300.0), (70.0, 540.0, 380.0), (72.0, 250.0, 980.0), (74.0, 360.0, 640.0), (76.0, 560.0, 900.0), (78.0, 200.0, 380.0) })
				at(cb - 1.5, () => add(L21bCharge(cx, cy, cb - 1.5, cb, ez ? 54 : 70, ez ? 105 : 125, ez ? 0.35 : 0.45, 0, 50, ez ? 480 : 650)));
			at(62, () => add(L21bNet(cols, rows, ez ? 190 : 180, ez ? 170 : 135, ez ? 0.035 : 0.05, 62, 64, 80, la.ToArray(), lb.ToArray(), on.ToArray(), off.ToArray(), ez ? 15 : 19, ez ? 10 : 14)));
		}
		// the dive (0:37-0:45): the hull's frames come up on the bass, then on the roll a winding way through, then a ring closes on the middle
		{
			double sp = ez ? 540 : 560, gw = ez ? 170 : 150; double[] gx = { 340, 640, 940, 640, 340, 640, 940, 640 };
			for (int i = 0; i < 8; i++) { double b = 80.75 + i - 1.5, x = gx[i]; at(b, () => add(L21bRib(b, sp, x, gw, ez ? 30 : 38))); }
			for (int i = 0; i < 9; i++) { if (ez && i % 2 == 1) continue; double b = 88.25 + 0.5 * i - 1.5, x = 640 - 380 * JS.sin(0.34 * i); at(b, () => add(L21bRib(b, sp, x, gw, ez ? 30 : 38))); }
			at(92.75, () => add(L21bPing(CX, CY, 92.75, 93.75, 760, -760 / (2 * SPB), 0, ez ? 16 : 20, new[] { 0.3, 0.3 + Math.PI }, ez ? 1.0 : 0.7)));
		}
		at(96, () => add(L21bFx(96, 0.35, 12, 1.4)));

		// ===== 2. the torpedoes =====
		// salvos on the bass (0:45-0:52): two from one side, the next from the other; on the bars' crashes three come down from above
		{
			double[][] lanes = { new[] { 150.0, 420, 640, 270 }, new[] { 300.0, 570, 80, 440 }, new[] { 90.0, 360, 560, 220 }, new[] { 240.0, 510, 660, 80 }, new[] { 180.0, 630, 400, 520 }, new[] { 360.0, 620, 110, 230 }, new[] { 120.0, 450, 280, 620 } };
			double tell = ez ? 1.4 : 1.0, sp = ez ? 1100 : 1800;
			for (int i = 0; i < L21B_BASS2.Length; i++)
			{
				double hb = L21B_BASS2[i], side = i % 2 == 0 ? -1 : 1; var ln = lanes[i % lanes.Length];
				// a volley: a wall of torpedoes with one way through (it goes round the lanes)
				double gy = ln[0] < 120 ? 120 : ln[0] > 600 ? 600 : ln[0];
				at(hb - tell, () => add(L21bVolley(side, gy, ez ? 160 : 120, hb - tell, hb, ez ? 850 : 1000)));
			}
			foreach (var cb in ez ? new[] { 100.0, 104, 108 } : new[] { 100.0, 102.25, 104, 106.25, 108, 110.25 })
			{
				double off = cb == 104 || cb == 106.25 ? 160 : -160;
				foreach (var x in ez ? new[] { 400.0, 880 } : new[] { 320.0, 640, 960 }) { double xx = x + off; at(cb - tell - 0.25, () => add(L21bTorp(xx, -100, Math.PI / 2, false, cb - tell - 0.25, cb, ez ? 900 : 1300))); }
			}
		}
		// with them, one aimed at you from a top corner on each bar (Normal: every two beats)
		for (double b = 98; b < 112; b += ez ? 4 : 2) { double bb = b, x = (b / 2) % 2 == 0 ? -100 : W + 100, tl = ez ? 1.8 : 1.3; at(bb - tl, () => add(L21bTorp(x, -100, 0, true, bb - tl, bb, ez ? 1000 : 1500)), false, true); }
		for (double b = 114; b < 132; b += ez ? 2 : 1) { double bb = b, x = (b / 2) % 2 == 0 ? -100 : W + 100, y = (b / 4) % 2 == 0 ? 160 : H - 160, tl = ez ? 1.6 : 1.2; at(bb - tl, () => add(L21bTorp(x, y, 0, true, bb - tl, bb, ez ? 1200 : 1800)), false, true); }
		// the moored mines (0:45-0:52): chains from the top and the floor; on the bass they all lunge out together
		{
			var xs = new List<double>(); var tp = new List<bool>(); var ln = new List<double>();
			for (int i = 0; i < (ez ? 6 : 9); i++) { double x = ez ? 120 + i * 210 : 70 + i * 142; xs.Add(x); tp.Add(true); ln.Add(ez ? 120 : 150 + 40 * (i % 2)); xs.Add(x + (ez ? 105 : 71)); tp.Add(false); ln.Add(ez ? 120 : 150 + 40 * ((i + 1) % 2)); }
			at(94.5, () => add(L21bMoor(xs.ToArray(), tp.ToArray(), ln.ToArray(), L21B_BASS2, ez ? 90 : 150, ez ? 32 : 40, 94.5, 96, 112.5)));
		}
		// the giant propeller (0:52-1:01): Barracuda passes right over you, its propeller crosses the screen; on the riff it jumps forward;
		// torpedoes keep the top and the bottom edges
		{
			double R = ez ? 300 : 345, j = ez ? 0.45 : 0.6;
			at(109.5, () => add(L21bShadow(W + 320, CY, -320, CY, 110.5, 136, 1500, 300, 136.5)));
			at(109.5, () => add(L21bProp(W + 360, CY, -360, CY, 110.5, 136, ez ? 3 : 4, R, 56, ez ? 54 : 62, ez ? 0.8 : 1.25, L21B_RIFF1, L21B_RIFF1.Select(_ => j).ToArray(), 0.15, 109.5, 111, 140, 141)));
			double tell = ez ? 1.4 : 1.0;
			for (int i = 0; i < L21B_BASS3.Length; i++)
			{
				double hb = L21B_BASS3[i], y = i % 2 == 0 ? 42 : H - 42;
				at(hb - tell, () => add(L21bTorp(-100, y, 0, false, hb - tell, hb, ez ? 1000 : 1400)));
			}
		}
		// the crossfire (1:01-1:12): from the edges, going round the clock, some aimed at you; from 1:09 the pings from the middle
		{
			(double x, double y)[] st = { (-100, 80), (640, -100), (W + 100, 80), (W + 100, 640), (640, H + 100), (-100, 640), (-100, 360), (W + 100, 360) };
			double[] offs = { -220, 0, 220 }; var aimed = ez ? new HashSet<double> { 137, 141, 145, 149, 153 } : new HashSet<double> { 135.5, 137, 139.5, 141, 143.5, 145, 147.5, 149, 151.5, 153, 155.5 };
			for (int i = 0; i < L21B_BASS4.Length; i++)
			{
				double hb = L21B_BASS4[i]; var s = st[i % st.Length]; bool am = aimed.Contains(hb);
								double tl = am ? (ez ? 1.8 : 1.3) : (ez ? 1.4 : 1.0), tx = CX + offs[i % 3], ty = CY + offs[(i + 1) % 3] * 0.6, ang = JS.atan2(ty - s.y, tx - s.x);
				if (am) at(hb - tl, () => add(L21bTorp(s.x, s.y, ang, true, hb - tl, hb, ez ? 1000 : 1500)), false, true);
				else foreach (var da in ez ? new[] { -0.2, 0, 0.2 } : new[] { -0.26, 0, 0.26 }) at(hb - tl, () => add(L21bTorp(s.x, s.y, ang + da, false, hb - tl, hb, ez ? 1000 : 1500)));
			}
			for (int i = 0; i < 20; i++)
			{
				if (ez && i < 4) continue; double b = 136 + i, ga = -Math.PI / 2 + i * Math.PI / 2;
				at(b - 0.75, () => add(L21bPing(CX, CY, b - 0.75, b, 30, ez ? 420 : 560, 900, ez ? 16 : 22, new[] { ga }, ez ? 1.4 : 1.0)));
			}
		}
		// the ballast columns (1:12-1:14): one, two, four
		{
			var cs = new (double b, double[] xs)[] { (156, new[] { 640.0 }), (157, new[] { 300.0, 980 }), (158, ez ? new[] { 140.0, 1140 } : new[] { 140.0, 470, 810, 1140 }) };
			foreach (var (b, xs) in cs) foreach (var x in xs) at(b - 1, () => add(L21bPillar(x, ez ? 56 : 70, b - 1, b, 0.5, 159.5)));
		}
		at(160, () => add(L21bFx(160, 0.4, 14, 1.5)));

		// ===== 3. Barracuda =====
		// it rams through on a lane every 4 beats (from the sides in turn) and fires its tubes up and down on the bass as it goes
		void rams(double[] goes, double[] ys, double[] dirs, double b0, double b1, bool bothWays)
		{
			double hh = ez ? 110 : 165, Ls = 900, sp = ez ? 1300 : 1500, tell = ez ? 2 : 1.5;
			for (int i = 0; i < goes.Length; i++)
			{
				double bg = goes[i], y = ys[i], d = dirs[i]; at(bg - tell, () => add(L21bHull(d, y, hh, Ls, bg - tell, bg, sp)));
				// Normal: a second one at the same time on the mirrored lane, from the other side
				if (!ez && Math.Abs(H - 2 * y) > 220) at(bg - tell, () => add(L21bHull(-d, H - y, hh, Ls, bg - tell, bg, sp)));
			}
			var hits = new List<double>(); for (double b = b0; b < b1; b += 8) foreach (var o in L21B_GROOVE) if (b + o < b1) hits.Add(b + o);
			int k = 0;
			foreach (var hb in hits)
			{
				// the hull on the screen at that moment
				for (int i = 0; i < goes.Length; i++)
				{
					double th = B(hb); if (th < B(goes[i])) continue; double nose = L21bHullX(dirs[i], Ls, goes[i], sp, th), tube = nose - dirs[i] * Ls * 0.3;
					if (tube < 70 || tube > W - 70) continue;
					double y = ys[i], tl = ez ? 1.0 : 0.75, ts = ez ? 900 : 1500; int kk = k++;
					foreach (var da in ez ? new[] { 0.0 } : new[] { -0.35, -0.12, 0.12, 0.35 })
					{
						if (bothWays || kk % 2 == 0) at(hb - tl, () => add(L21bTorp(tube, y - hh / 2 + 10, -Math.PI / 2 + da, false, hb - tl, hb, ts, 90, 22)));
						if (bothWays || kk % 2 == 1) at(hb - tl, () => add(L21bTorp(tube, y + hh / 2 - 10, Math.PI / 2 - da, false, hb - tl, hb, ts, 90, 22)));
					}
					if (ez) break;
				}
			}
		}
		if (ez) rams(new[] { 161.5, 163.5, 165.5, 167.5, 169.5, 171.5, 173.5 }, new[] { 200.0, 520, 360, 160, 560, 280, 440 }, new[] { 1.0, -1, 1, -1, 1, -1, 1 }, 160, 176, true);
		else rams(new[] { 161.5, 163.5, 165.5, 167.5, 169.5, 171.5, 173.5 }, new[] { 200.0, 520, 360, 160, 560, 280, 440 }, new[] { 1.0, -1, 1, -1, 1, -1, 1 }, 160, 176, true);
		foreach (var hb in ez ? new[] { 169.0 } : new[] { 165.0, 169, 173 }) { double tl = ez ? 1.5 : 1.1; at(hb - tl, () => add(L21bLock(hb - tl, hb, 0.2, ez ? 36 : 50)), false, true); }
		// it locks on you on the bass (1:22-1:25), while the sonar arms turn
		{
			var hits = ez ? new[] { 177.0, 178, 179.5, 181, 182, 183.5 } : new[] { 177.0, 178, 179.5, 181, 182, 182.75, 183.5 }; double tl = ez ? 1.35 : 1.0;
			foreach (var hb in hits) at(hb - tl, () => add(L21bLock(hb - tl, hb, 0.2, ez ? 36 : 50)), false, true);
			double sp = ez ? 0.4 : 0.55; double[] kb = { 176, 180, 184 }, ka = { 0, sp * 4, 0 };
			at(174.5, () => add(L21bSweep(CX, CY, kb, ka, ez ? 1 : 2, 40, 900, 12, new[] { 180.0 }, 174.5, 176, 183, 184, ez ? 0.08 : 0.14)));
		}
		// its propeller in the middle stutters on the riff (1:25-1:29): it jumps on each hit (forward three times, then back four)
		{
			double j = ez ? 0.35 : 0.55; var ja = L21B_RIFF2.Select((v, i) => i < 3 ? j : -j).ToArray();
			at(182.5, () => add(L21bProp(CX, CY, CX, CY, 184, 192, 4, ez ? 300 : 330, 50, ez ? 44 : 50, ez ? 0.15 : 0.25, L21B_RIFF2, ja, 0.15, 182.5, 184, 191.5, 192.25)));
			if (!ez) foreach (var (hb, x) in new[] { (186.0, -100.0), (186.5, W + 100), (187.0, -100.0), (190.5, W + 100), (191.0, -100.0) }) at(hb - 1.2, () => add(L21bTorp(x, x < 0 ? 80 : H - 80, 0, true, hb - 1.2, hb, 1400)), false, true);
		}
		// everything at once (1:29-1:40): the charges with the arm, then salvos with the lock-on, then it rams again
		{
			double tell = ez ? 2 : 1.5; double[] ys = { 200, 520, 360, 140, 580, 260, 460 }; double[] xs = { 400, 880, 640, 300, 980, 520, 760 };
			for (int i = 0; i < L21B_GROOVE.Length; i++)
			{
				double hb = 192 + L21B_GROOVE[i], y = ys[i], x = xs[i]; if (ez && i % 2 == 1) continue;
				at(hb - tell, () => add(L21bCharge(x, y, hb - tell, hb, ez ? 44 : 70, ez ? 95 : 125, ez ? 0.35 : 0.45, i % 3 == 0 ? 1.6 : 0, ez ? 50 : 60, ez ? 420 : 650)));
				if (!ez && i % 2 == 0) at(hb - tell, () => add(L21bCharge(W - x, H - y, hb - tell, hb, 70, 100, 0.45, 0, 60)));
			}
			double sp = ez ? 0.5 : 0.75; double[] kb = { 192, 196, 200 }, ka = { Math.PI / 4, Math.PI / 4 + sp * 4, Math.PI / 4 };
			at(190.5, () => add(L21bSweep(CX, CY, kb, ka, ez ? 1 : 2, 40, 900, 12, new[] { 196.0 }, 190.5, 192, 199.5, 200.5, ez ? 0.1 : 0.17)));
			double[][] lanes = { new[] { 160.0, 560, 360 }, new[] { 300.0, 640, 100 }, new[] { 80.0, 420, 600 }, new[] { 240.0, 600, 420 }, new[] { 120.0, 480, 300 }, new[] { 360.0, 660, 180 } };
			for (int i = 0; i < 6; i++)
			{
				double hb = 200 + L21B_GROOVE[i]; var ln = lanes[i]; double tl = ez ? 1.4 : 1.0;
				at(hb - tl, () => add(L21bVolley(i % 2 == 0 ? -1 : 1, clamp(ln[0], 120, 600), ez ? 160 : 110, hb - tl, hb, ez ? 850 : 1100)));
				if (!ez) at(hb - tl, () => add(L21bTorp(i % 2 == 0 ? W + 100 : -100, ln[1], i % 2 == 0 ? Math.PI : 0, false, hb - tl, hb, 1800)));
			}
			foreach (var hb in ez ? new[] { 205.0 } : new[] { 203.5, 207.5 }) { double tl = ez ? 1.5 : 1.1; at(hb - tl, () => add(L21bLock(hb - tl, hb, 0.2, ez ? 34 : 40)), false, true); }
			if (ez) rams(new[] { 208.5, 212.5 }, new[] { 240.0, 480 }, new[] { 1.0, -1 }, 208, 216, false);
			else rams(new[] { 208.5, 210.5, 212.5, 214.5 }, new[] { 240.0, 520, 160, 440 }, new[] { 1.0, -1, 1, -1 }, 208, 216, true);
		}
		// the end (1:40-1:44): rings close on you on the riff (the way through goes round), then the last pings all leave by the top
		{
			double[] arr = ez ? new[] { 216.25, 217.75, 219.5 } : new[] { 216.25, 217, 217.75, 219.5 }; double cl = 1.75;
			for (int i = 0; i < arr.Length; i++)
			{
				double hb = arr[i], ga = -Math.PI / 2 + i * TAU / 3;
				at(hb - cl - 0.75, () => add(L21bPing(CX, CY, hb - cl - 0.75, hb - cl, 760, -760 / (cl * SPB), 0, ez ? 18 : 22, new[] { ga }, ez ? 0.95 : 0.5)));
			}
			// between them the sonar's beams turn fast one last time (it has found you)
			{ double sp = ez ? 0.9 : 1.3; double[] kb = { 219.75, 222.25 }, ka = { Math.PI / 2, Math.PI / 2 + sp * 2.5 }; at(218.5, () => add(L21bSweep(CX, CY, kb, ka, 2, 40, 900, 12, new double[0], 218.5, 219.75, 222, 222.4, ez ? 0.14 : 0.2))); }
			foreach (var hb in ez ? new[] { 222.5, 223.25 } : new[] { 222.5, 223, 223.25, 223.5, 223.75 })
				at(hb - 1, () => add(L21bPing(CX, CY, hb - 1, hb, 30, 700, 900, 16, new[] { -Math.PI / 2 }, ez ? 1.1 : 0.8)));
		}
		at(224, () => add(L21bFx(224, 0.4, 12, 1.4)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
