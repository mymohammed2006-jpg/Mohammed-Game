using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24 (v2, his words 10-31: every change in the song plays in a completely new way, the screen full).
// Twelve ways to play, one for each part of the song:
//   part 1 (2:34-3:44): falling blocks that stack up, your own tail, the safe bubble, billiard balls, the laser off the mirrors;
//   the build-up (3:44-3:54): the room closes in step by step faster and faster, then you break out through its wall with a dash;
//   the hardest part (3:54-4:34, rising): the turning grating, the pendulums, the fireworks;
//   part 3 (4:34-5:25): bombs with cross blasts, searchlights (hide in the pillars' shadows), the cave run.
// The level is the song from 2:34.000 (songs/SONG_L24.mp3 starts there). Grid: 120 BPM, beat 0 = 0.1975 s of the file (2:34.1975 of the song).
// Only pink hurts: gold, white warnings and dark shapes never do. Every hazard is a pure function of time once it exists, except
// the tail (it follows where you went).
public static partial class G
{
	static double QSk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string QSP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string QSW(double a) => $"rgba(236,232,246,{str(clamp(a, 0, 1))})";
	static string QSG(double a) => $"rgba(232,186,92,{str(clamp(a, 0, 1))})";
	static double QSE(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double QSOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double QSRad(double d) => d * Math.PI / 180;
	static double QSH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
	static double QSMod(double a, double m) { double r = a % m; return r < 0 ? r + m : r; }
	static void QSDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double QSSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// the integral (degrees or px) of a speed (a beat) that holds w[i] from kb[i] to kb[i+1] (and w[last] after), from kb[0] to bt
	static double QSTurn(double[] kb, double[] w, double bt)
	{
		double a = 0;
		for (int i = 0; i < kb.Length; i++)
		{
			double b0 = kb[i], b1 = i + 1 < kb.Length ? kb[i + 1] : 1e9; if (bt <= b0) break;
			a += (Math.Min(bt, b1) - b0) * w[i];
		}
		return a;
	}
	// a dart: a sharp needle, length l, pointing along a (radians)
	static void QSDart(double x, double y, double l, double a, double wd = 4)
	{
		double c = JS.cos(a), s = JS.sin(a);
		g.beginPath(); g.moveTo(x + c * l * 0.5, y + s * l * 0.5); g.lineTo(x - c * l * 0.5 - s * wd, y - s * l * 0.5 + c * wd);
		g.lineTo(x - c * l * 0.3, y - s * l * 0.3); g.lineTo(x - c * l * 0.5 + s * wd, y - s * l * 0.5 - c * wd); g.closePath(); g.fill();
	}
	static bool QSDartHit(Player p, double x, double y, double l, double a)
	{
		double c = JS.cos(a), s = JS.sin(a);
		return QSSeg(p.x, p.y, x - c * l * 0.4, y - s * l * 0.4, x + c * l * 0.5, y + s * l * 0.5) < 3 + p.r - 2;
	}
	static double[] QSIn(double[] l, double a, double b, double gap = 0)
	{
		var o = new List<double>(); double last = -1e9;
		foreach (var x in l) if (x >= a && x < b && x - last >= gap - 1e-9) { o.Add(x); last = x; }
		return o.ToArray();
	}
	public static Hz QSFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "QSFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the stage (harmless): a dark room whose colour changes with each part, a faint grid that pulses on the kicks ----------
	static readonly double[] QS_SEC = { 0, 40, 80, 104, 120, 140, 160, 180, 220, 240, 280, 320, 342 };
	static readonly int[][] QS_TINT = { new[] { 18, 24, 56 }, new[] { 10, 38, 44 }, new[] { 30, 18, 52 }, new[] { 8, 38, 28 }, new[] { 34, 28, 10 }, new[] { 40, 30, 6 },
		new[] { 8, 26, 54 }, new[] { 30, 16, 44 }, new[] { 6, 8, 26 }, new[] { 36, 24, 10 }, new[] { 10, 12, 22 }, new[] { 26, 14, 36 }, new[] { 10, 10, 20 } };
	static double[] QSTint(double bt)
	{
		int i = 0; while (i + 1 < QS_SEC.Length && bt >= QS_SEC[i + 1]) i++;
		var a = QS_TINT[i]; var b = QS_TINT[Math.Min(QS_TINT.Length - 1, i + 1)]; double k = i + 1 < QS_SEC.Length ? QSE((bt - (QS_SEC[i + 1] - 1)) / 1) : 0;
		return new[] { a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k, a[2] + (b[2] - a[2]) * k };
	}
	public static Hz QSStage(double t1)
	{
		var h = new Hz { kind = "QSStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), kick = 0;
			foreach (var kb in QS_KICK) { double d = bt - kb; if (d >= 0 && d < 0.6) kick = Math.Max(kick, 1 - d / 0.6); if (kb > bt) break; }
			var c = QSTint(bt); g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(c[0] * (1 + 0.25 * kick))},{str(c[1] * (1 + 0.25 * kick))},{str(c[2] * (1 + 0.25 * kick))},1)");
			gr.addColorStop(1, $"rgba({str(c[0] * 0.3)},{str(c[1] * 0.3)},{str(c[2] * 0.3)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.strokeStyle = $"rgba(220,226,255,{str(0.035 + 0.04 * kick)})"; g.lineWidth = 1; g.beginPath();
			for (double x = 80; x < W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
			for (double y = 80; y < H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
			g.stroke();
			var vg = g.createRadialGradient(W / 2, H / 2, 200, W / 2, H / 2, 820); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)");
			g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- A. falling blocks (2:34): pieces of four squares fall on the beat and stack up from the floor; the stack burns away
	// on the big hits. Each piece's column and where it will land show first (white dashed). ----------
	const double QS_C = 80;
	public class QSPiece { public int[][] cells; public int col, top; public double bs, tl, bc, v; public int h; }
	static readonly int[][][] QS_SHAPES =
	{
		new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 0 }, new[] { 3, 0 } },   // I
		new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 } },   // O
		new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 0 }, new[] { 1, 1 } },   // T
		new[] { new[] { 0, 0 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 2 } },   // L
		new[] { new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, 2 }, new[] { 0, 2 } },   // J
		new[] { new[] { 1, 0 }, new[] { 2, 0 }, new[] { 0, 1 }, new[] { 1, 1 } },   // S
		new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 2, 1 } },   // Z
		new[] { new[] { 0, 0 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 } },   // I standing
	};
	// the plan: pieces spawn at the beats in sp (in that order), fall at v px/s; the stack burns at the beats in clr
	static List<QSPiece> QSBlocksPlan(double[] sp, double v, double[] clr)
	{
		var o = new List<QSPiece>(); var colH = new int[16]; int ci = 0; var lastCols = new List<int[]>();
		for (int n = 0; n < sp.Length; n++)
		{
			var sh = QS_SHAPES[(n * 5 + n / 3) % QS_SHAPES.Length]; int w = sh.Max(c => c[0]) + 1, h = sh.Max(c => c[1]) + 1;
			// the column: walks across the well (organized), never on the two pieces before it
			int col = 0;
			for (int tr = 0; tr < 16; tr++)
			{
				col = Math.Min(16 - w, (n * 7 + tr * 5 + (int)(QSH(n + 0.37 * tr) * 2)) % 16);
				bool bad = lastCols.Any(lc => col < lc[1] && col + w > lc[0]);
				if (!bad) break;
			}
			lastCols.Add(new[] { col, col + w }); if (lastCols.Count > 2) lastCols.RemoveAt(0);
			double bs = sp[n], t0 = B(bs);
			// the burn before it lands resets the stack
			while (ci < clr.Length && B(clr[ci]) < t0 + 0.2) { Array.Clear(colH, 0, 16); ci++; }
			int top = 99;
			for (int c = 0; c < w; c++)
			{
				int dyMax = sh.Where(q => q[0] == c).Max(q => q[1]);
				top = Math.Min(top, 9 - colH[col + c] - 1 - dyMax);
			}
			double tl = t0 + (top * QS_C + h * QS_C) / v;
			if (ci < clr.Length && tl > B(clr[ci]))
			{
				Array.Clear(colH, 0, 16); ci++; top = 9 - h; tl = t0 + (top * QS_C + h * QS_C) / v;
			}
			for (int c = 0; c < w; c++) { int dyMin = sh.Where(q => q[0] == c).Min(q => q[1]); colH[col + c] = Math.Max(colH[col + c], 9 - (top + dyMin)); }
			double bc = clr[Math.Min(ci, clr.Length - 1)];   // a piece after the last burn still burns with it (never stays)
			o.Add(new QSPiece { cells = sh, col = col, top = top, bs = bs, tl = tl, bc = bc, v = v, h = h });
		}
		return o;
	}
	public static Hz QSBlock(QSPiece P, double wn)
	{
		var h = new Hz { kind = "QSBlock" }; double t0 = B(P.bs), tc = B(P.bc);
		double yOf(double t) => Math.Min(P.top * QS_C, -P.h * QS_C + (t - t0) * P.v);
		h.update = (t, dt) => { if (t > tc + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < P.bs - wn) return;
			g.save();
			if (t < P.tl)
			{
				double q = QSk(bt, P.bs - wn, P.bs);
				int w = P.cells.Max(c => c[0]) + 1;
				g.fillStyle = QSW(0.04 + 0.05 * q); g.fillRect(P.col * QS_C, 0, w * QS_C, H);
				g.strokeStyle = QSW(0.3 + 0.45 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 });
				foreach (var c in P.cells) g.strokeRect((P.col + c[0]) * QS_C + 5, (P.top + c[1]) * QS_C + 5, QS_C - 10, QS_C - 10);
				g.setLineDash(new double[0]);
			}
			if (t >= t0)
			{
				double y = yOf(t);
				if (t < tc)
				{
					g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK;
					foreach (var c in P.cells) g.fillRect((P.col + c[0]) * QS_C + 4, y + c[1] * QS_C + 4, QS_C - 8, QS_C - 8);
					g.shadowBlur = 0; g.strokeStyle = "rgba(255,190,214,0.8)"; g.lineWidth = 3;
					foreach (var c in P.cells) g.strokeRect((P.col + c[0]) * QS_C + 14, y + c[1] * QS_C + 14, QS_C - 28, QS_C - 28);
				}
				else
				{
					// burnt: gold embers fading (harmless)
					double k = QSk(t, tc, tc + 0.6); g.fillStyle = QSG(0.7 * (1 - k));
					foreach (var c in P.cells) { double s = (QS_C - 8) * (1 - 0.6 * k); g.fillRect((P.col + c[0]) * QS_C + QS_C / 2 - s / 2, y + c[1] * QS_C + QS_C / 2 - s / 2 - 30 * k, s, s); }
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= tc) return false; double y = yOf(t), m = QS_C / 2 - 4 + p.r - 2;
			foreach (var c in P.cells) { double cx = (P.col + c[0] + 0.5) * QS_C, cy = y + (c[1] + 0.5) * QS_C; if (Math.Abs(p.x - cx) < m && Math.Abs(p.y - cy) < m) return true; }
			return false;
		};
		return h;
	}

	// the floor burns (when the stack burns away): a pink band from y0 to y1, shows from bw (white dashed edge, gold sparks), pink at b for hold beats
	public static Hz QSBurn(double y0, double y1, double bw, double b, double hold)
	{
		var h = new Hz { kind = "QSBurn" };
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + hold + 0.4) return;
			g.save();
			if (bt < b) { double q = QSk(bt, bw, b); g.fillStyle = QSW(0.04 + 0.08 * q); g.fillRect(0, y0, W, y1 - y0); QSDash(0, y0, W, y0, QSW(0.4 + 0.5 * q), 3); g.fillStyle = QSG(0.5 * q); for (int i = 0; i < 24; i++) g.fillRect(QSH(i + bt * 0.01) * W, y1 - (y1 - y0) * QSH(i * 3 + Math.Floor(bt * 8)), 5, 5); }
			else { double al = 1 - QSk(bt, b + hold, b + hold + 0.4); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = QSP(0.9 * al); g.fillRect(0, y0, W, y1 - y0); }
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); return bt >= b && bt < b + hold && p.y > y0 - (p.r - 2); };
		return h;
	}

	// ---------- B. your own tail (2:54): where you were turns pink after d0 s and stays so until d1 s; you must never stop and never
	// cross your own fresh path. The part younger than d0 shows white dashed. Stomps on the kicks (a square shows first, then slams). ----------
	public static Hz QSTail(double b0, double b1, double d0, double d1)
	{
		var h = new Hz { kind = "QSTail" }; double t0 = B(b0), t1 = B(b1);
		var ts = new List<double>(); var xs = new List<double>(); var ys = new List<double>();
		h.update = (t, dt) =>
		{
			if (t > t1 + d1 + 0.2) { h.dead = true; return; }
			if (t >= t0 && t <= t1 - d0 && player != null && (ts.Count == 0 || t - ts[ts.Count - 1] >= 1.0 / 60 - 1e-9)) { ts.Add(t); xs.Add(player.x); ys.Add(player.y); }
			while (ts.Count > 0 && ts[0] < t - d1 - 0.1) { ts.RemoveAt(0); xs.RemoveAt(0); ys.RemoveAt(0); }
		};
		h.draw = t =>
		{
			if (t < t0 - 0.5 || t > t1 + 0.3) return;
			double fa = 1 - QSk(t, t1, t1 + 0.3);
			g.save(); g.lineCap = "round"; g.lineJoin = "round"; g.globalAlpha = fa;
			// the young part (harmless yet)
			g.strokeStyle = QSW(0.45); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); bool on = false;
			for (int i = 0; i < ts.Count; i++) { if (t - ts[i] > d0) continue; if (!on) { g.moveTo(xs[i], ys[i]); on = true; } else g.lineTo(xs[i], ys[i]); }
			g.stroke(); g.setLineDash(new double[0]);
			g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 8; g.beginPath(); on = false;
			for (int i = 0; i < ts.Count; i++) { double age = t - ts[i]; if (age < d0 || age > d1) { on = false; continue; } if (!on) { g.moveTo(xs[i], ys[i]); on = true; } else g.lineTo(xs[i], ys[i]); }
			g.stroke();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t >= t1) return false;   // the tail is gone before the flood comes
			for (int i = 0; i + 1 < ts.Count; i++)
			{
				double a0 = t - ts[i], a1 = t - ts[i + 1]; if (a0 > d1 || a1 < d0) continue;
				if (QSSeg(p.x, p.y, xs[i], ys[i], xs[i + 1], ys[i + 1]) < 4 + p.r - 2) return true;
			}
			// standing still: the newest point that is old enough
			for (int i = ts.Count - 1; i >= 0; i--) { double a = t - ts[i]; if (a < d0) continue; if (a > d1) break; if (hypot(p.x - xs[i], p.y - ys[i]) < 4 + p.r - 2) return true; break; }
			return false;
		};
		return h;
	}
	// a stomp: a square (side sz) at (x, y) shows from bw (a gold frame closing in), slams pink at b for hold beats
	public static Hz QSStomp(double x, double y, double sz, double bw, double b, double hold)
	{
		var h = new Hz { kind = "QSStomp" };
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save();
			if (bt < b)
			{
				double q = QSk(bt, bw, b), s = sz * (1.8 - 0.8 * q);
				g.strokeStyle = QSW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(x - sz / 2, y - sz / 2, sz, sz); g.setLineDash(new double[0]);
				g.strokeStyle = QSG(0.3 + 0.6 * q); g.lineWidth = 3; g.strokeRect(x - s / 2, y - s / 2, s, s);
			}
			else
			{
				double al = 1 - QSk(bt, b + hold, b + hold + 0.3), k = QSOut((bt - b) / 0.08);
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(al); double s = sz * (0.7 + 0.3 * k); g.fillRect(x - s / 2, y - s / 2, s, s);
				g.shadowBlur = 0; g.strokeStyle = $"rgba(255,190,214,{str(0.8 * al)})"; g.lineWidth = 3; g.strokeRect(x - s / 2 + 14, y - s / 2 + 14, s - 28, s - 28);
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b || bt >= b + hold) return false; double m = sz / 2 * (0.7 + 0.3 * QSOut((bt - b) / 0.08)) + p.r - 2; return Math.Abs(p.x - x) < m && Math.Abs(p.y - y) < m; };
		return h;
	}

	// ---------- C. the safe bubble (3:14): the whole screen floods pink but for the bubble; the bubble travels, sometimes it splits in
	// two and only the gold one stays (the white dashed one shrinks away). A bar crosses the bubble on the kicks. ----------
	public class QSBub { public double b0, b1, x0, y0, x1, y1, r, die = -1; }   // moves (x0,y0) -> (x1,y1) from b0 to b1; die: shrinks from there
	static bool QSBubAt(QSBub u, double bt, out double x, out double y, out double r)
	{
		x = y = r = 0; if (bt < u.b0 - 1e-9) return false; double k = clamp((bt - u.b0) / (u.b1 - u.b0), 0, 1);
		x = u.x0 + (u.x1 - u.x0) * k; y = u.y0 + (u.y1 - u.y0) * k; r = u.r;
		if (u.die >= 0) { r = u.r * (1 - QSk(bt, u.die, u.die + 1)); if (r <= 0) return false; }
		if (bt > u.b1 && u.die < 0) return false;
		return true;
	}
	public static Hz QSFlood(List<QSBub> bubs, double bw, double b0, double b1, double mid)
	{
		var h = new Hz { kind = "QSFlood" };
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b1 + 0.6) return;
			g.save();
			if (bt < b0)
			{
				double q = QSk(bt, bw, b0); g.fillStyle = QSW(0.05 + 0.07 * q); g.fillRect(0, 0, W, H);
				// white stripes sliding in (the flood is coming)
				g.strokeStyle = QSW(0.15 + 0.25 * q); g.lineWidth = 3; g.beginPath();
				for (double x = -H + QSMod(bt * 80, 60); x < W; x += 60) { g.moveTo(x, H); g.lineTo(x + H, 0); }
				g.stroke();
			}
			else
			{
				double al = bt < b1 ? 0.9 : 0.9 * (1 - QSk(bt, b1, b1 + 0.6)); g.fillStyle = bt < b1 ? QSP(al) : QSG(al * 0.5); g.fillRect(0, 0, W, H);
			}
			foreach (var u in bubs)
			{
				if (!QSBubAt(u, bt, out var x, out var y, out var r)) continue;
				if (bt >= b0) { g.fillStyle = "rgb(12,10,26)"; circle(x, y, r); }   // never cover the tail before the flood
				if (u.die >= 0) { g.strokeStyle = QSW(0.8); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				else { g.strokeStyle = QSG(0.95); g.lineWidth = 4; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); }
			}
			// where the bubble goes next: a small gold ring at its next stop
			foreach (var u in bubs) if (u.die < 0 && bt >= u.b0 - 1 && bt < u.b1) { g.strokeStyle = QSG(0.5); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(u.x1, u.y1, u.r, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 + 0.25 || bt >= b1) return false;
			foreach (var u in bubs) { if (!QSBubAt(u, bt, out var x, out var y, out var r)) continue; if (hypot(p.x - x, p.y - y) + p.r - 2 < r) return false; }
			return true;
		};
		return h;
	}
	// a bar across the bubble u: through its middle at ang degrees, shows from bw, pink at b for hold beats
	public static Hz QSBubBar(QSBub u, double ang, double bw, double b, double hold, double hw)
	{
		var h = new Hz { kind = "QSBubBar" }; double a = QSRad(ang), c = JS.cos(a), s = JS.sin(a);
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + hold + 0.3) return; if (!QSBubAt(u, bt, out var x, out var y, out var r)) return;
			g.save();
			if (bt < b) { double q = QSk(bt, bw, b); QSDash(x - c * r, y - s * r, x + c * r, y + s * r, QSW(0.3 + 0.5 * q), 2); }
			else { double al = 1 - QSk(bt, b + hold, b + hold + 0.3); g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = QSP(al); g.lineWidth = 2 * hw; g.lineCap = "butt"; g.beginPath(); g.moveTo(x - c * r, y - s * r); g.lineTo(x + c * r, y + s * r); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b || bt >= b + hold) return false; if (!QSBubAt(u, bt, out var x, out var y, out var r)) return false;
			return QSSeg(p.x, p.y, x - c * r, y - s * r, x + c * r, y + s * r) < hw + p.r - 2;
		};
		return h;
	}

	// ---------- D. billiards (3:26, the quiet part): pink balls roll in from the corners and bounce off the walls; the way each
	// ball goes next shows white dashed. ----------
	static double QSBounce(double u, double lo, double hi) { double L = hi - lo; double m = QSMod(u - lo, 2 * L); return lo + (m < L ? m : 2 * L - m); }
	public static Hz QSBall(double x0, double y0, double vx, double vy, double r, double bw, double b0, double b1)
	{
		var h = new Hz { kind = "QSBall" }; double t0 = B(b0);
		void pos(double t, out double x, out double y) { double d = Math.Max(0, t - t0); x = QSBounce(x0 + vx * d, r, W - r); y = QSBounce(y0 + vy * d, r, H - r); }
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b1 + 0.6) return;
			g.save();
			// the way ahead (1 s)
			double q = bt < b0 ? QSk(bt, bw, b0) : 1; g.strokeStyle = QSW(0.2 + 0.35 * q * (1 - QSk(bt, b1 - 1, b1))); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath();
			for (int i = 0; i <= 30; i++) { pos(Math.Max(t, t0) + i / 30.0, out var px, out var py); if (i == 0) g.moveTo(px, py); else g.lineTo(px, py); }
			g.stroke(); g.setLineDash(new double[0]);
			pos(t, out var x, out var y);
			if (bt < b0) { g.strokeStyle = QSG(0.4 + 0.5 * q); g.lineWidth = 3; g.beginPath(); g.arc(x, y, r * (1.6 - 0.6 * q), 0, TAU); g.stroke(); }
			else
			{
				double s = bt < b1 ? 1 : 1 - QSk(bt, b1, b1 + 0.5);
				g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; circle(x, y, r * s);
				g.shadowBlur = 0; g.fillStyle = "rgba(255,200,222,0.85)"; circle(x - r * 0.35 * s, y - r * 0.35 * s, r * 0.22 * s);
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1 + 0.4) return false; double s = bt < b1 ? 1 : 1 - QSk(bt, b1, b1 + 0.5); pos(t, out var x, out var y); return hypot(p.x - x, p.y - y) < r * s + p.r - 2; };
		return h;
	}
}
