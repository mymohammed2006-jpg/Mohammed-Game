using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Ya Tayeb El Galb (main level 28): the events. Beats from 0 (3:20.345 of the song), saves at 128 (4:18.5) and 184 (4:43.9), the win at 264 (5:20.3).
public static partial class G
{
	// the bite of a heart at (x, y) looks at the screen's middle: the outline's point that points that way (in turns)
	static double TQBiteAt(double x, double y)
	{
		double ax = W / 2 - x, ay = H / 2 - y, l = hypot(ax, ay) + 1e-9, best = -9, bu = 0;
		for (int i = 0; i < TQNH; i++) { double hx = TQHX(i), hy = TQHY(i), hl = hypot(hx, hy) + 1e-9, d = (hx * ax + hy * ay) / (hl * l); if (d > best) { best = d; bu = (i + 0.5) / TQNH; } }
		return bu;
	}
	static double[] TQThin(IEnumerable<double> l, double gap) { var o = new List<double>(); double last = -99; foreach (var x in l) if (x - last >= gap) { o.Add(x); last = x; } return o.ToArray(); }

	static readonly int _tqb = DodgeBuild("L28", () => buildTQ());
	static List<Ev> buildTQ()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(TQStage(B(268))), true);
		at(0, () => add(TQWarnLayer(B(268))), true);

		// ===== 1. it beats (3:20.3-4:18.5) =====
		// the heart monitor: the pen runs left to right, a spike on each kick; from 3:27.6 (Normal) a second pen runs the other way
		{
			double hot = ez ? 1.0 : 1.5, tl = ez ? 1.3 : 1.0;
			at(0, () => add(TQEcg(0, 32, 1, H / 2, TQ_ECG, hot, tl)));
			if (!ez) { var kb = TQ_ECG.Where(x => x >= 16).ToArray(); at(15, () => add(TQEcg(16, 32, -1, H / 2, kb, hot, tl))); }
		}
		at(32, () => add(TQFx(32, 0.15, 6, 0.8)));
		// the heart's waves on the kicks, the gap goes round
		{
			double gw = ez ? 0.18 : 0.14, sp = ez ? 290 : 360, tl = ez ? 1.0 : 0.75;
			var wb = ez ? TQThin(TQ_WAVE, 2.5) : TQ_WAVE;
			for (int i = 0; i < wb.Length; i++) { double b = wb[i], gu = 0.5 + 0.17 * i; at(b - tl, () => add(TQWave(b, gu, gw, sp, 12, tl))); }
			// 3:42.2 "يا طيب القلب": the heart bursts on the claps
			var bb = ez ? TQThin(TQ_BURST, 2) : TQ_BURST;
			for (int i = 0; i < bb.Length; i++) { double b = bb[i], a0 = 18 * i; at(b - 0.6, () => add(TQBurst(b, ez ? 7 : 10, a0, ez ? 270 : 330, 15, 0.6))); }
			// 3:45.8 the waves again
			var w2 = ez ? new[] { 57.5, 61.0 } : TQ_WAVE2.Concat(new[] { 56.0, 59.0, 62.0 }).OrderBy(x => x).ToArray();
			for (int i = 0; i < w2.Length; i++) { double b = w2[i], gu = 0.1 + 0.29 * i; at(b - tl, () => add(TQWave(b, gu, gw, sp, 12, tl))); }
		}
		at(48, () => add(TQFx(48, 0.3, 9, 1.2)));
		at(64, () => add(TQFx(64, 0.2, 7, 1.0)));
		// the pulse walls: from the right on the kicks, from 3:56.7 from the left; the heart sends a wave every 8 beats (Normal)
		{
			double[] gys = { 180, 520, 300, 600, 140, 420, 560, 260, 380 };
			double gh = ez ? 190 : 150, sp = ez ? 420 : 520, tl = ez ? 1.6 : 1.25;
			var wl = ez ? TQThin(TQ_WALL, 3) : TQ_WALL;
			for (int i = 0; i < wl.Length; i++) { double b = wl[i], gy = gys[i % gys.Length]; int dir = b < 80 ? -1 : 1; at(b - tl, () => add(TQWall(b, dir, gy, gh, sp, tl))); }
			if (!ez) foreach (var b in new[] { 70.0, 78, 86, 94 }) { double gu = (b - 70) / 8 * 0.25; at(b - 0.75, () => add(TQWave(b, gu, 0.18, 300, 12, 0.75))); }
		}
		at(96, () => add(TQFx(96, 0.2, 7, 1.0)));
		// the hearts with a bite: they grow in the four corners in turn, the bite looks at the middle; walls from the sides in the last 16 beats
		{
			double[][] pos = { new[] { 420.0, 250 }, new[] { 860.0, 470 }, new[] { 860.0, 250 }, new[] { 420.0, 470 } };
			double sm = ez ? 280 : 340, gw = ez ? 0.26 : 0.17, tl = ez ? 1.3 : 1.0;
			var bt = ez ? TQThin(TQ_BITE, 2) : TQ_BITE;
			for (int i = 0; i < bt.Length; i++) { double b = bt[i], x = pos[i % 4][0], y = pos[i % 4][1], gu = TQBiteAt(x, y); at(b - tl, () => add(TQBite(x, y, sm, gu, gw, b, tl, 1.25, 0.75, 14))); }
			double gh = ez ? 190 : 150, sp = ez ? 420 : 520, wt = ez ? 1.6 : 1.25;
			var wb = ez ? new[] { 106.0, 114, 122 } : new[] { 98.0, 102, 106, 110, 113, 117, 121, 125 };
			double[] gys = { 360, 200, 520, 300, 440, 160, 580, 260 };
			for (int i = 0; i < wb.Length; i++) { double b = wb[i], gy = gys[i % gys.Length]; int dir = i % 2 == 0 ? 1 : -1; at(b - wt, () => add(TQWall(b, dir, gy, gh, sp, wt))); }
		}
		at(128, () => add(TQFx(128, 0.35, 10, 1.4)));

		// ===== 2. the rose (4:18.5-4:43.9) =====
		// the thorny stems turn, pushed on each kick, and turn back every 8 beats; the rose throws petals on the kicks
		{
			var kk = TQ_KICK2.Where(x => x >= 128.5 && x < 160).ToArray();
			at(127, () => add(TQStems(ez ? 3 : 4, 90, ez ? 9 : 15, kk, ez ? 12 : 20, 8, 127, 128.5, 160)), true);
			var pk = ez ? TQThin(TQ_KICK2, 3) : TQ_KICK2;
			for (int i = 0; i < pk.Length; i++) { double b = pk[i], a0 = 22.5 * i + 11; if (b < 129) continue; at(b - 0.75, () => add(TQPetals(b, ez ? 7 : 10, a0, ez ? 280 : 380, 18, 0.75))); }
		}
		at(160, () => add(TQFx(160, 0.2, 6, 1.0)));
		// the petal rain in the wind: rows from the top, the gap zigzags; from 4:40.3 from below
		{
			double[] gxs = { 300, 480, 660, 840, 1000, 820, 640, 460 };
			double gw = ez ? 245 : 210, sp = ez ? 310 : 350, tl = ez ? 1.2 : 0.9;
			var rb = ez ? TQThin(TQ_RAIN, 2) : TQ_RAIN;
			for (int i = 0; i < rb.Length; i++)
			{
				double b = rb[i];
				// from 4:40.3 the rows come from below (the wind turns)
				double gx = gxs[i % gxs.Length], tilt = (Math.Floor((b - 160) / 8) % 2 == 0 ? 1 : -1) * 10; int dir = b >= 176 ? -1 : 1;
				at(b - tl, () => add(TQRain(dir, tilt, gx, gw, b, tl, sp)));
			}
		}
		at(184, () => add(TQFx(184, 0.4, 12, 1.5)));

		// ===== 3. the light (4:43.9-5:20.3) =====
		// the light beams on the kicks, turning; the heart sends a wave every 8 beats (Normal)
		{
			var kb = TQ_BEAM.Where(x => x >= 187).ToArray(); if (ez) kb = TQThin(kb, 2.5);
			at(184, () => add(TQBeams(kb, 15, ez ? 45 : 37.5, ez ? 2 : 3, ez ? 0.55 : 0.75, ez ? 1.3 : 0.85, 26)));
			foreach (var b in ez ? new[] { 192.0, 208 } : new[] { 188.0, 192, 196, 200, 204, 208, 212 }) { double gu = (b - 188) / 4 * 0.29 + 0.25; at(b - (ez ? 1.0 : 0.75), () => add(TQWave(b, gu, ez ? 0.24 : 0.18, ez ? 270 : 320, 12, ez ? 1.0 : 0.75))); }
		}
		at(216, () => add(TQFx(216, 0.25, 8, 1.0)));
		// the spiral of small hearts on the hats, and the heart's waves on the kicks
		{
			var hb = ez ? TQThin(TQ_HAT3, 1) : TQ_HAT3;
			at(215, () => add(TQSpiral(hb, ez ? 2 : 3, 0, 23, ez ? 240 : 290, 13, 0.6)), true);
			var kb = ez ? TQThin(TQ_KICK3, 4) : TQ_KICK3;
			for (int i = 0; i < kb.Length; i++) { double b = kb[i], gu = 0.5 + 0.21 * i; at(b - 0.75, () => add(TQWave(b, gu, ez ? 0.24 : 0.18, ez ? 270 : 320, 12, 0.75))); }
		}
		at(248, () => add(TQFx(248, 0.25, 8, 1.0)));
		// the last bursts, then the last heart closes in round the gold heart
		{
			var bb = ez ? new[] { 248.0, 252 } : new[] { 248.0, 250, 252, 254 };
			for (int i = 0; i < bb.Length; i++) { double b = bb[i], a0 = 15 * i; at(b - 0.6, () => add(TQBurst(b, ez ? 8 : 12, a0, ez ? 270 : 330, 15, 0.6))); }
			at(255, () => add(TQClose(256, 262, 1150, 100, 0.5, ez ? 0.06 : 0.08, ez ? 0.26 : 0.2, 14, 1)));
		}
		at(262, () => add(TQFx(262, 0.15, 6, 1.2)));
		at(262.5, () => add(TQEnd(262.5, 266)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
