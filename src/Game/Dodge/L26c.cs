using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// "لا دندنت" (main level 26), the events. His asks (10-30): the song from 0:30 to 2:36; as hard as level 2, but level 5's difficulty at
// 1:06-1:14, 1:22-1:30, 1:39-1:47, 1:55-2:03 and 2:28 to the end; 2:03-2:12 the level's start again exactly; 2:20-2:28 earlier parts again.
// Blocks in beats: 13 drums in (0:37.2), 69 (1:06.1), 85 (1:14.4), 101 (1:22.7), 117 (1:31.0), 133 (1:39.2), 149 (1:47.5), 165 (1:55.8),
// 181 (2:04.1), 197 (2:12.3), 213 (2:20.6), 229 (2:28.9), 245 (2:37.2, the last hit). The music at 181 is the music at 13, at 213 it is 149 and 117.
public static partial class G
{
	static readonly int _ddb = DodgeBuild("L26", () => buildDD());
	static List<Ev> buildDD()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		double MX = 640, MY = 347;                 // the big drum's skin
		double D(double d) => DDRad(d);
		at(0, () => add(DDStage(B(250))), true);

		// ===== the intro: the voice alone (0:30.4-0:37.2): a ring from the drum on each word, the gap goes round =====
		void Intro()
		{
			double sp = ez ? 220 : 280, gw = D(ez ? 200 : 140);
			double[] vb = { 0, 2, 4.5, 6.75, 9, 11 };   // the first words of the lines, then two in the hush before the drums
			for (int i = 0; i < vb.Length; i++) { double b = vb[i], ga = D(-90 + 63 * i); at(b - 1, () => add(DDRing(MX, MY, b, 1, sp, 16, ga, gw))); }
		}

		// ===== 1. the sticks (0:37.2-0:45.4; again at 2:04.1): two sticks meet and part on every beat, then the beat's stick and the
		// "dum"'s stick across =====
		void Sticks(double o)
		{
			double hold = ez ? 0.25 : 0.4, tl = ez ? 1.2 : 1;
			for (int k = 0; k < 8; k++)
			{
				double b = 13 + k + o; int l = k < 4 ? k : 7 - k;          // in from the edges to the middle, and back out
				int l2 = 7 - l; if (ez && k % 2 == 1) continue;
				at(b - tl, () => add(DDStick(l * 160 + 14, 0, 132, H, b, tl, hold)));
				if (!ez && k % 2 == 0) at(b - tl, () => add(DDStick(l2 * 160 + 14, 0, 132, H, b, tl, hold)));
			}
			int[] rows = { 0, 5, 1, 4, 2, 3, 1, 4 };
			for (int k = 0; k < 8; k++)
			{
				double b = 21 + k + o; int l = (k * 3) % 8;
				if (!ez || k % 2 == 0) at(b - tl, () => add(DDStick(l * 160 + 14, 0, 132, H, b, tl, hold)));
				if (k % 2 == 0 && (!ez || k % 4 == 0)) { double bd = b + 0.75; int r = rows[k / 2 * 2 % 8]; at(bd - tl, () => add(DDStick(0, r * 120 + 14, W, 92, bd, tl, hold))); }
			}
		}

		// ===== 2. the spears (0:45.4-0:53.7): walls of spears from the left on the "dum", from the right on its echo; then from the top and below =====
		void Spears(double o, double from, double to)
		{
			double hold = ez ? 0.45 : 0.6, tl = ez ? 1.2 : 1, reach = ez ? 620 : 700;
			int[] gl = { 0, 2, 4, 3 }, gr = { 4, 2, 0, 1 };
			for (int k = 0; k < 4; k++)
			{
				double b = 29 + 2 * k; if (b < from || b >= to) continue; b += o;
				for (int r = 0; r < 6; r++) { if (r == gl[k] || r == gl[k] + 1 || (ez && r == gl[k] + 2)) continue; double y = 60 + 120 * r; at(b - tl, () => add(DDSpear(-30, y, 0, reach + 30, b, tl, hold))); }
				double b2 = b + 0.75;
				for (int r = 0; r < 6; r++) { if (r == gr[k] || r == gr[k] + 1 || (ez && r == Math.Max(0, gr[k] - 1))) continue; double y = 60 + 120 * r; at(b2 - tl, () => add(DDSpear(W + 30, y, Math.PI, reach + 30, b2, tl, hold))); }
			}
			int[] ct = { 1, 4, 7, 5 }, cb = { 7, 4, 1, 3 };
			for (int k = 0; k < 4; k++)
			{
				double b = 37 + 2 * k; if (b < from || b >= to) continue; b += o; double rv = ez ? 330 : 360;
				for (int c = 0; c < 10; c++) { if (c == ct[k] || c == ct[k] + 1 || (ez && (c == ct[k] + 2 || c == ct[k] - 1))) continue; double x = 64 + 128 * c; at(b - tl, () => add(DDSpear(x, -30, Math.PI / 2, rv + 30, b, tl, hold))); }
				double b2 = b + 0.75;
				for (int c = 0; c < 10; c++) { if (c == cb[k] || c == cb[k] + 1 || (ez && (c == cb[k] - 1 || c == cb[k] + 2))) continue; double x = 64 + 128 * c; at(b2 - tl, () => add(DDSpear(x, H + 30, -Math.PI / 2, rv + 30, b2, tl, hold))); }
			}
		}

		// ===== 3. the drum speaks (0:53.7-1:02.0): two rings on each "dum" (the second's gap a little on), the gap turns; then a ring on
		// every beat, its gap swinging left and right =====
		void DrumTalk()
		{
			double sp = ez ? 240 : 320, gw = D(ez ? 150 : 96);
			for (int k = 0; k < 4; k++)
			{
				double b = 45 + 2 * k, ga = D(-90 + 100 * k);
				if (!ez || k % 2 == 0) at(b - 1, () => add(DDRing(MX, MY, b, 1, sp, 16, ga, gw)));
				if (!ez && k % 2 == 1) at(b - 0.25, () => add(DDRing(MX, MY, b + 0.75, 1, sp, 16, ga + D(50), gw * 1.25)));
			}
			for (int k = 0; k < 4; k++)
			{
				double b = 53 + 2 * k, ga = D(90 + (k % 2 == 0 ? -60 : 60));
				at(b - 1, () => add(DDRing(MX, MY, b, 1, sp, 16, ga, gw)));
				if (!ez || k % 2 == 0) { double bs = b + 1; double y = k % 2 == 0 ? 14 : 614; at(bs - 1, () => add(DDStick(0, y, W, 92, bs, 1, ez ? 0.3 : 0.4))); }
			}
		}
		// ===== 4. the drum roll (1:02.0-1:06.1): a ring every half beat, the gap turning on (a spiral) =====
		void Roll()
		{
			double sp = ez ? 260 : 340, gw = D(ez ? 150 : 100);
			for (int k = 0; k < 5; k++) { if (ez && k % 2 == 1) continue; double b = 61 + 1.5 * k, ga = D(-90 + 72 * k); at(b - 1, () => add(DDRing(MX, MY, b, 1, sp, 16, ga, gw))); }
		}

		// ===== HARD 1 (1:06.1-1:14.4): the war drums, level 5: rings from the two side drums on every beat, the sticks cross on the
		// "dum"; then from the four corner drums =====
		void Hard1()
		{
			double sp = ez ? 380 : 560, gw = D(ez ? 100 : 42), hold = ez ? 0.4 : 0.5, rt = ez ? 1 : 0.8;
			(double x, double y)[] sd = DD_SIDE.Select(d => (d.x, d.y - 5.5)).ToArray();
			double[] la = { 0, 28, -28, 0, 20, -20, 0, 28 };
			for (int k = 0; k < 8; k++)
			{
				double b = 69 + k; var d = sd[k % 2]; double ga = D(k % 2 == 0 ? la[k] : 180 - la[k]);
				at(b - rt, () => add(DDRing(d.x, d.y, b, rt, sp, 18, ga, gw, 40)));
				if (!ez && k % 2 == 1) { double bh = b + 0.5; var e = sd[0]; double gb = D(-la[k]); at(bh - rt, () => add(DDRing(e.x, e.y, bh, rt, sp, 16, gb, gw * 1.2, 40))); }
				if (!ez || k % 2 == 0) { double bd = b + 0.5; double y = k % 2 == 0 ? 8 : 608; at(bd - 1, () => add(DDStick(0, y, W, 104, bd, 1, hold))); }
			}
			(double x, double y)[] cd = DD_CORNER.Select(d => (d.x, d.y - 2)).ToArray();
			int[] ord = { 0, 2, 1, 3, 0, 2, 1, 3 };
			for (int k = 0; k < 8; k++)
			{
				double b = 77 + k; if (ez && k % 2 == 1) continue; var d = cd[ord[k]]; double toC = Math.Atan2(360 - d.y, 640 - d.x), ga = toC + D(k % 2 == 0 ? 18 : -18);
				at(b - rt, () => add(DDRing(d.x, d.y, b, rt, sp * 1.08, 18, ga, gw, 34)));
				if (!ez && k % 2 == 1) { double bh = b + 0.5; var e = cd[(ord[k] + 2) % 4]; double gb = Math.Atan2(360 - e.y, 640 - e.x); at(bh - rt, () => add(DDRing(e.x, e.y, bh, rt, sp * 1.08, 16, gb, gw * 1.15, 34))); }
			}
		}

		// ===== 5. al-ardah, the sword dance (1:14.4-1:22.7): a line of swords sways together from below, spears come down from above on
		// the "dum"; then the swords from above, the spears from below =====
		void Ardah(double o, double from, double to)
		{
			double sway = D(ez ? 14 : 18), len = 300, tl = 1;
			for (int half = 0; half < 2; half++)
			{
				bool top = half == 1; double py = top ? -30 : H + 30, mid = top ? Math.PI / 2 : -Math.PI / 2;
				for (int k = 0; k < 8; k++)
				{
					double b = 85 + 8 * half + k; if (b < from || b >= to) continue; b += o;
					double s0 = (k % 2 == 0 ? -1 : 1), a0 = mid + s0 * sway, a1 = mid - s0 * sway; bool first = k == 0;
					for (int i = 0; i < 7; i++)
					{
						double px = 100 + 180 * i, bi = b + 0.08 * i;
						// each swing is its own sword: it shows in sand only before the first one (1 beat), then the line keeps swaying
						at(bi - (first ? tl : 0.02), () => add(DDSword(px, py, 60, len, a0, a1, bi, 0.45, first ? tl : 0.02, 0.55 - 0.0001, 20)));
					}
				}
				int[] gc = top ? new[] { 6, 3, 0, 4 } : new[] { 1, 4, 7, 3 };
				for (int k = 0; k < 4; k++)
				{
					double b = 85 + 8 * half + 2 * k + 0.75; if (b < from || b >= to) continue; b += o; double rv = ez ? 300 : 340;
					for (int c = 0; c < 10; c++)
					{
						if (c == gc[k] || c == gc[k] + 1 || (ez && (c == gc[k] + 2 || c == gc[k] - 1))) continue; double x = 64 + 128 * c;
						if (top) at(b - 1, () => add(DDSpear(x, H + 30, -Math.PI / 2, rv + 30, b, 1, ez ? 0.45 : 0.6)));
						else at(b - 1, () => add(DDSpear(x, -30, Math.PI / 2, rv + 30, b, 1, ez ? 0.45 : 0.6)));
					}
				}
			}
		}

		// ===== HARD 2 (1:22.7-1:31.0): the blades clash, level 5: a long sword from the middle slashes the top half, then the bottom half,
		// on every beat; the two ways between them (the free wedges) turn on a little every second beat; then two blades at once, crossed,
		// the pair turning on, back and forth =====
		void Hard2()
		{
			double sw = ez ? 0.36 : 0.26, hold = ez ? 0.12 : 0.2, len = 790, span = ez ? 100 : 115;
			for (int k = 0; k < 8; k++)
			{
				double b = 101 + k, rot = 30 * (k / 2), s0 = rot + (k % 2 == 0 ? 180 : 0), a0 = k % 4 < 2 ? s0 : s0 + span, a1 = k % 4 < 2 ? s0 + span : s0;
				if (ez && k % 2 == 1) continue;
				at(b - 1, () => add(DDSword(640, 360, 46, len, D(a0), D(a1), b, sw, 1, hold, 26)));
			}
			for (int k = 0; k < 8; k++)
			{
				double b = 109 + k, s0 = 20 + 32 * k, sp2 = ez ? 56 : 62, dir = k % 2 == 0 ? 1 : -1;
				if (ez && k % 2 == 1) continue;
				for (int m = 0; m < 2; m++)
				{
					double c = s0 + 180 * m, a0 = dir > 0 ? c : c + sp2, a1 = dir > 0 ? c + sp2 : c;
					at(b - 1, () => add(DDSword(640, 360, 46, len, D(a0), D(a1), b, sw, 1, hold, 26)));
				}
			}
		}

		// ===== 6. the arrows (1:31.0-1:39.2): a column of arrows marches right on the beat, another left on the half beat; then the field's
		// two halves in turn (a chessboard) on the "dum" and its echo =====
		void Arrows(double o, double from, double to)
		{
			double r = ez ? 46 : 54, hold = 0.35, tl = 1;
			double[] cy = { 90, 225, 360, 495, 630 };
			for (int k = 0; k < 8; k++)
			{
				double b = 117 + k; if (b < from || b >= to) continue; b += o;
				double xa = 120 + 148 * k, xb = W - 120 - 148 * k;
				var ya = ez ? new[] { cy[0], cy[2], cy[4] } : cy;
				at(b - tl, () => add(DDArrows(ya.Select(_ => xa).ToArray(), ya, b, tl, r, hold, 1)));
				if (!ez || k % 2 == 0) { double bh = b + 0.5; var yb = new[] { cy[1], cy[3] }; at(bh - tl, () => add(DDArrows(yb.Select(_ => xb).ToArray(), yb, bh, tl, r, hold, -1))); }
			}
			for (int k = 0; k < 8; k++)
			{
				double b = 125 + k; if (b < from || b >= to) continue; b += o; int par = k % 2;
				var xs = new List<double>(); var ys = new List<double>();
				for (int i = 0; i < 5; i++) for (int j = 0; j < 3; j++) if ((i + j) % 2 == par) { xs.Add(128 + 256 * i); ys.Add(120 + 240 * j); }
				double rr = ez ? 64 : 108;
				at(b - tl, () => add(DDArrows(xs.ToArray(), ys.ToArray(), b, tl, rr, ez ? 0.3 : 0.4, par == 0 ? 1 : -1)));
			}
		}

		// ===== HARD 3 (1:39.2-1:47.5): the arrow storm, level 5: slanted lines of arrows every half beat, spears from the left on the "dum";
		// then rows of arrows fall down the field and back up while spears come from below =====
		void Hard3()
		{
			double r = ez ? 48 : 62, hold = 0.34, tl = ez ? 1 : 0.8;
			for (int k = 0; k < 16; k++)
			{
				double b = 133 + 0.5 * k; if (ez && k % 2 == 1) continue;
				var xs = new List<double>(); var ys = new List<double>(); double sh = (k % 4) * 110;
				for (int j = 0; j < 6; j++) { double x = 90 + sh + j * 230 - j * 40; double y = 70 + j * 116; if (x > W - 40) x -= W - 80; if (k % 2 == 1) x = W - x; xs.Add(x); ys.Add(y); }
				if (!ez) { int n0 = xs.Count; for (int j = 0; j < n0; j++) { double x2 = xs[j] + 460; if (x2 > W - 40) x2 -= W - 80; xs.Add(x2); ys.Add(ys[j]); } }
				at(b - tl, () => add(DDArrows(xs.ToArray(), ys.ToArray(), b, tl, r, hold, k % 2 == 0 ? 1 : -1)));
			}
			int[] gl = { 4, 0, 2, 3 };
			for (int k = 0; k < 4; k++)
			{
				double b = 133 + 2 * k + 0.75;
				for (int rr = 0; rr < 6; rr++) { if (rr == gl[k] || rr == gl[k] + 1 || (ez && rr == gl[k] + 2)) continue; double y = 60 + 120 * rr; at(b - tl, () => add(DDSpear(-30, y, 0, (ez ? 520 : 640) + 30, b, tl, 0.5))); }
				if (!ez) { double b2 = b + 1; int g2 = (gl[k] + 3) % 5; for (int rr = 0; rr < 6; rr++) { if (rr == g2 || rr == g2 + 1) continue; double y = 60 + 120 * rr; at(b2 - tl, () => add(DDSpear(W + 30, y, Math.PI, 670, b2, tl, 0.5))); } }
			}
			double[] rowY = { 70, 190, 310, 430, 550, 670, 550, 430 };
			for (int k = 0; k < 8; k++)
			{
				double b = 141 + k, y = rowY[k]; var xs = new List<double>(); var ys = new List<double>();
				int gap = new[] { 2, 5, 1, 6, 3, 7, 0, 4 }[k];
				for (int i = 0; i < 8; i++) { if (i == gap || (ez && i == (gap + 1) % 8)) continue; xs.Add(80 + 160 * i); ys.Add(y); }
				at(b - tl, () => add(DDArrows(xs.ToArray(), ys.ToArray(), b, tl, ez ? 64 : 80, ez ? 0.3 : 0.42, 1)));
				if (k % 2 == 1 && !ez) { double b2 = b + 0.5; int c0 = (k * 3) % 10; for (int c = 0; c < 10; c++) { if (c == c0 || c == (c0 + 1) % 10 || c == (c0 + 5) % 10 || c == (c0 + 6) % 10) continue; double x = 64 + 128 * c; at(b2 - tl, () => add(DDSpear(x, H + 30, -Math.PI / 2, 250, b2, tl, 0.45))); } }
			}
		}

		// ===== 7. the banner (1:47.5-1:55.8, and its first half again at 2:20.6): it turns an eighth on every beat; then with two arms =====
		void Banner(double o, double from, double to)
		{
			double tl = 1;
			if (from < 157)
			{
				double b0 = 149 + o, b1 = Math.Min(157, to) + o; var turns = Enumerable.Range(0, 8).Select(i => 149.0 + i + o).Where(x => x < b1).ToArray();
				at(b0 - tl, () => add(DDBanner(640, 360, 760, 1, D(-90), D(ez ? 40 : 45), turns, b0, b1, tl)));
			}
			if (to > 157)
			{
				double b0 = 157 + o, b1 = 165 + o;
				var turns = Enumerable.Range(0, 8).Select(i => 157.0 + i + o).ToArray();
				// two arms (the banner and its staff end), an eighth on every beat
				at(b0 - tl, () => add(DDBanner(640, 360, 760, 2, D(0), D(ez ? 25 : 30), turns, b0, b1, tl)));
			}
		}

		// ===== HARD 4 (1:55.8-2:04.1): the horses charge, level 5: on every beat a horse from each side in two lanes; then the herd: four lanes
		// at once, two lanes free, the free lanes move one on each beat =====
		void Hard4()
		{
			// seven lanes that touch (no way between two lanes): the horse is 107 px tall, the lanes 103 apart
			double sp = ez ? 760 : 1120, tl = ez ? 1 : 0.8; int NL = 7;
			double[] ly = Enumerable.Range(0, NL).Select(i => 52.0 + 103 * i).ToArray();
			int[] l1 = { 0, 3, 6, 2, 5, 1, 4, 0 }, l2 = { 4, 1, 2, 6, 3, 0, 2, 5 };
			for (int k = 0; k < 8; k++)
			{
				double b = 165 + k, ya = ly[l1[k]], yb = ly[l2[k]];
				at(b - tl, () => add(DDHorse(ya, 1, b, sp, tl)));
				if (!ez) at(b - tl + 0.5, () => add(DDHorse(yb, -1, b + 0.5, sp, tl)));
				if (!ez) { double yc = ly[(l1[k] + 4) % NL]; at(b - tl + 0.5, () => add(DDHorse(yc, 1, b + 0.5, sp, tl))); }
			}
			for (int k = 0; k < 8; k++)
			{
				double b = 173 + k; int f0 = (k + 1) % NL, f1 = (k + 4) % NL; int dir = k % 2 == 0 ? 1 : -1;
				for (int l = 0; l < NL; l++) { if (l == f0 || l == f1 || (ez && (l == (f0 + 1) % NL || l == (f1 + 1) % NL || l == (f0 + 2) % NL))) continue; double y = ly[l]; at(b - tl, () => add(DDHorse(y, dir, b, sp, tl))); }
				// half a beat later, from the other side: only the lanes free neither now nor on the next beat
				if (!ez && k < 7) { double bh = b + 0.5; for (int l = 0; l < NL; l++) { if (l == f0 || l == f1 || l == (f0 + 1) % NL || l == (f1 + 1) % NL) continue; double y = ly[l]; at(bh - tl, () => add(DDHorse(y, -dir, bh, sp, tl))); } }
			}
		}

		// ===== 8. the sandstorm (2:12.3-2:20.6): the way through the sand bends on every second beat =====
		void Storm()
		{
			double vs = ez ? 360 : 430, gw = ez ? 300 : 230;
			double[] kb = { 197, 199, 201, 203, 205, 207, 209, 211, 213 }, ky = { 360, 200, 470, 260, 520, 330, 170, 430, 300 };
			double b1 = BT(B(213) - W / vs);   // the sand's tail leaves the screen at 2:20.6
			at(196, () => add(DDStorm(197, b1, vs, gw, kb, ky)));
		}

		// ===== HARD 5 (2:28.9-2:37.2): the banner of the win, level 5: the banner (two arms) turns a sixth of a round every second beat while horses run along the top and the bottom
		// on the beat; then the crossed blades from the middle, the last ring at 2:37.2 =====
		void Hard5()
		{
			double sp = ez ? 760 : 940, tl = 1;
			var turns = new[] { 230.0, 232, 234, 236 };
			at(229 - tl, () => add(DDBanner(640, 360, 760, 2, D(-90), D(ez ? 50 : 60), turns, 229, 237, tl)));
			double[] ly = { 60, 660, 660, 60 };
			for (int k = 0; k < 8; k++) { double b = 229 + k + 0.5; if (ez && k % 2 == 1) continue; double y = ly[k % 4]; int dir = k % 2 == 0 ? 1 : -1; at(b - tl, () => add(DDHorse(y, dir, b, sp, tl))); }
			// 2:33: the crossed blades every second beat, a horse along the top or the bottom edge between them
			double sw = ez ? 0.4 : 0.28, hold = ez ? 0.15 : 0.22;
			for (int k = 0; k < 4; k++)
			{
				double b = 237 + 2 * k, s0 = 45 + 70 * k, sp2 = ez ? 66 : 80, dir = k % 2 == 0 ? 1 : -1;
				for (int m = 0; m < 2; m++) { double c = s0 + 180 * m, a0 = dir > 0 ? c : c + sp2, a1 = dir > 0 ? c + sp2 : c; at(b - 1, () => add(DDSword(640, 360, 46, 790, D(a0), D(a1), b, sw, 1, hold, 26))); }
				double b2 = b + 1, y = k % 2 == 0 ? 60 : 660; int d2 = k % 2 == 0 ? 1 : -1;
				at(b2 - tl, () => add(DDHorse(y, d2, b2, sp, tl)));
				if (!ez) { double b3 = b2 + 0.5, y3 = y == 60 ? 660 : 60; at(b3 - tl, () => add(DDHorse(y3, -d2, b3, sp, tl))); }
			}
			at(244, () => add(DDRing(640, 360, 245, 1, ez ? 420 : 520, 22, D(90), D(ez ? 70 : 50), 30)));
		}

		Intro();
		Sticks(0);
		at(13, () => add(DDFx(13, 0.3, 10, 1.0)));
		Spears(0, 29, 45);
		DrumTalk(); Roll();
		at(69, () => add(DDFx(69, 0.25, 8, 0.8)));
		Hard1();
		at(85, () => add(DDFx(85, 0.3, 10, 1.0)));
		Ardah(0, 85, 101);
		Hard2();
		Arrows(0, 117, 133);
		Hard3();
		Banner(0, 149, 165);
		at(165, () => add(DDFx(165, 0.3, 10, 1.0)));
		Hard4();
		// 2:04.1: the start again, exactly (the drum comes back)
		Sticks(168);
		at(181, () => add(DDFx(181, 0.25, 8, 0.8)));
		Storm();
		// 2:20.6: earlier parts again: the banner's first half (1:47.5), the arrows' first half (1:31.0)
		Banner(64, 149, 157);
		Arrows(104, 117, 125);
		at(229, () => add(DDFx(229, 0.3, 10, 1.0)));
		Hard5();
		at(245, () => add(DDFx(245, 0.45, 14, 1.4)));
		at(245.3, () => add(DDEnd(245.3, 247.5)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
