using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24, part 3 (4:34-5:25): the bombs, the searchlights and the pillars' shadows, the cave run; and the events.
public static partial class G
{
	// ---------- J. bombs (4:34): a gold bomb is set in a square of the grid, its fuse burns down, then it blasts its whole row and
	// column pink. Where the cross will go shows white dashed first. ----------
	public static Hz QSBomb(int ci, int cj, double bp, double wn, double be, double hold, double hw)
	{
		double x = (ci + 0.5) * QS_C, y = (cj + 0.5) * QS_C;
		// the warning lies under every pink thing (a waiting bomb never covers a live blast)
		var pre = new Hz { kind = "QSBombPre", z = -1 };
		pre.update = (t, dt) => { if (BT(t) >= be) pre.dead = true; };
		pre.draw = t =>
		{
			double bt = BT(t); if (bt < bp || bt >= be) return;
			g.save();
			double q = QSk(bt, bp, be);
			if (bt >= be - wn)
			{
				double q2 = QSk(bt, be - wn, be); g.fillStyle = QSW(0.04 + 0.06 * q2); g.fillRect(0, y - hw, W, 2 * hw); g.fillRect(x - hw, 0, 2 * hw, H);
				QSDash(0, y - hw, W, y - hw, QSW(0.3 + 0.4 * q2)); QSDash(0, y + hw, W, y + hw, QSW(0.3 + 0.4 * q2)); QSDash(x - hw, 0, x - hw, H, QSW(0.3 + 0.4 * q2)); QSDash(x + hw, 0, x + hw, H, QSW(0.3 + 0.4 * q2));
			}
			g.fillStyle = "rgb(30,22,10)"; circle(x, y, 20); g.strokeStyle = QSG(0.95); g.lineWidth = 4; g.beginPath(); g.arc(x, y, 26, -Math.PI / 2, -Math.PI / 2 + TAU * (1 - q)); g.stroke();
			g.fillStyle = QSG(0.6 + 0.4 * JS.sin(bt * TAU * 2)); circle(x, y, 6);
			g.restore();
		};
		hazards.Add(pre);
		var h = new Hz { kind = "QSBomb" };
		h.update = (t, dt) => { if (BT(t) > be + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < be) return;
			g.save();
			{
				double al = 1 - QSk(bt, be + hold, be + hold + 0.3), k = QSOut((bt - be) / 0.08);
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(al); g.fillRect(0, y - hw * k, W, 2 * hw * k); g.fillRect(x - hw * k, 0, 2 * hw * k, H);
				g.shadowBlur = 0; g.fillStyle = $"rgba(255,200,222,{str(0.9 * al)})"; circle(x, y, 24 * k);
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < be || bt >= be + hold) return false; double m = hw * QSOut((bt - be) / 0.08) + p.r - 2; return Math.Abs(p.y - y) < m || Math.Abs(p.x - x) < m; };
		return h;
	}

	// ---------- K. searchlights (4:54): pink beams of light sweep from the top (later from the bottom too); the gold pillars stop the
	// light: stand in their shadows. The pillars drift in the second half. ----------
	static readonly double[] QS_PX = { 150, 420, 640, 860, 1130, 290, 990 }, QS_PY = { 300, 410, 290, 410, 300, 570, 570 };
	const double QS_PR = 48;
	static void QSPillar(int k, double bt, out double x, out double y) { double d = QSk(bt, 298, 302); x = QS_PX[k] + d * 70 * JS.sin(QSRad((bt - 300) * 22.5 + 90 * k)); y = QS_PY[k]; }
	public static Hz QSPillars(double b0, double b1)
	{
		var h = new Hz { kind = "QSPillars", z = -1 };   // under every pink thing (the last bombs still blast when they rise)
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2 || bt > b1 + 1) return; double al = QSk(bt, b0 - 2, b0 - 1) * (1 - QSk(bt, b1, b1 + 1));
			g.save();
			for (int k = 0; k < QS_PX.Length; k++) { QSPillar(k, bt, out var x, out var y); g.fillStyle = $"rgba(20,16,8,{str(al)})"; circle(x, y, QS_PR); g.strokeStyle = QSG(0.95 * al); g.lineWidth = 4; g.beginPath(); g.arc(x, y, QS_PR, 0, TAU); g.stroke(); g.strokeStyle = QSG(0.4 * al); g.lineWidth = 2; g.beginPath(); g.arc(x, y, QS_PR * 0.6, 0, TAU); g.stroke(); }
			g.restore();
		};
		return h;
	}
	public static Hz QSLight(double lx, double ly, double dir0, double A, double per, double ph, double ca, double b0, double b1)
	{
		var h = new Hz { kind = "QSLight" };
		double ang(double bt) => QSRad(dir0 + A * JS.sin(Math.PI * (bt - ph) / per));
		// how far the light gets along direction a before a pillar stops it
		double reach(double a, double bt)
		{
			double dx = JS.cos(a), dy = JS.sin(a), best = 1600;
			for (int k = 0; k < QS_PX.Length; k++)
			{
				QSPillar(k, bt, out var cx, out var cy); double ox = cx - lx, oy = cy - ly, pr = ox * dx + oy * dy; if (pr <= 0) continue;
				double pp = ox * ox + oy * oy - pr * pr; if (pp >= QS_PR * QS_PR) continue; double tt = pr - Math.Sqrt(QS_PR * QS_PR - pp); if (tt < best) best = tt;
			}
			return best;
		}
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1.5 || bt > b1 + 0.6) return;
			double a = ang(Math.Max(bt, b0 - 1.5)), cr = QSRad(ca);
			g.save();
			if (bt < b0)
			{
				double q = QSk(bt, b0 - 1.5, b0); QSDash(lx, ly, lx + JS.cos(a - cr) * 1500, ly + JS.sin(a - cr) * 1500, QSW(0.25 + 0.5 * q)); QSDash(lx, ly, lx + JS.cos(a + cr) * 1500, ly + JS.sin(a + cr) * 1500, QSW(0.25 + 0.5 * q));
			}
			else
			{
				double al = 1 - QSk(bt, b1, b1 + 0.6);
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(0.78 * al); g.beginPath(); g.moveTo(lx, ly);
				int n = (int)Math.Ceiling(2 * ca / 0.4); for (int i = 0; i <= n; i++) { double ai = a - cr + 2 * cr * i / n, r = reach(ai, bt); g.lineTo(lx + JS.cos(ai) * r, ly + JS.sin(ai) * r); }
				g.closePath(); g.fill(); g.shadowBlur = 0;
				// where it will point half a beat on
				double a2 = ang(bt + 0.5); QSDash(lx, ly, lx + JS.cos(a2) * 1500, ly + JS.sin(a2) * 1500, QSW(0.3 * al));
			}
			g.fillStyle = QSG(0.95); circle(lx, ly, 16); g.fillStyle = QSW(0.9); circle(lx, ly, 7);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			double dx = p.x - lx, dy = p.y - ly, d = hypot(dx, dy); if (d < 1) return true;
			double da = Math.Abs(QSMod(JS.atan2(dy, dx) - ang(bt) + Math.PI, TAU) - Math.PI); if (da > QSRad(ca) + Math.Asin(Math.Min(1, (p.r - 2) / d))) return false;
			for (int k = 0; k < QS_PX.Length; k++) { QSPillar(k, bt, out var cx, out var cy); if (QSSeg(cx, cy, lx, ly, p.x, p.y) < QS_PR - (p.r - 2)) return false; }
			return true;
		};
		return h;
	}

	// ---------- L. the cave run (5:14): the screen runs to the left through a winding pink cave; spikes from its walls pass the middle
	// on the kicks; at the end it opens out. ----------
	static double QSCaveY(double X) => H / 2 + 140 * JS.sin(X / 520) + 60 * JS.sin(X / 230 + 1);
	public static Hz QSCave(double b0, double b1, double V, double h0, double h1, double[] spX, bool[] spTop, double spH)
	{
		var h = new Hz { kind = "QSCave" }; double T0 = B(b0);
		double hg(double bt) => bt < b0 + 2 ? 420 + (h0 - 420) * QSE((bt - b0) / 2) : bt < b1 - 2 ? h0 + (h1 - h0) * (bt - b0 - 2) / (b1 - b0 - 4) : h1 + (440 - h1) * QSE((bt - b1 + 2) / 1.5);
		double scr(double t) => (t - T0) * V;
		// a spike: world X, top or bottom; its triangle on screen
		void spike(int k, double t, double g0, out double ax, out double ay, out double bx, out double by, out double tx, out double ty)
		{
			double X = spX[k], x = X - scr(t), yc = QSCaveY(X), s = spTop[k] ? -1 : 1, wy = yc + s * g0;
			ax = x - 46; bx = x + 46; ay = by = wy + s * 30; tx = x; ty = wy - s * spH;
		}
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 0.6) return;
			g.save();
			if (bt < b0) { double q = QSk(bt, b0 - 1, b0); for (int s = -1; s <= 1; s += 2) { g.strokeStyle = QSW(0.3 + 0.4 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); for (double x = 0; x <= W; x += 16) { double y = QSCaveY(x) + s * h0; if (x == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); g.setLineDash(new double[0]); } }
			else
			{
				double gg = hg(bt), sc = scr(t), al = 1 - QSk(bt, b1, b1 + 0.6);
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = QSP(0.92 * al);
				for (int s = -1; s <= 1; s += 2)
				{
					g.beginPath(); g.moveTo(-10, s < 0 ? -10 : H + 10);
					for (double x = -10; x <= W + 10; x += 16) g.lineTo(x, QSCaveY(x + sc) + s * gg);
					g.lineTo(W + 10, s < 0 ? -10 : H + 10); g.closePath(); g.fill();
				}
				for (int k = 0; k < spX.Length; k++)
				{
					double x = spX[k] - sc; if (x < -80 || x > W + 80) continue;
					spike(k, t, gg, out var ax, out var ay, out var bx, out var by, out var tx, out var ty);
					g.beginPath(); g.moveTo(ax, ay); g.lineTo(bx, by); g.lineTo(tx, ty); g.closePath(); g.fill();
				}
				g.shadowBlur = 0;
				// the spikes coming in from the right show their place first (white dashed) a second ahead
				for (int k = 0; k < spX.Length; k++)
				{
					double x = spX[k] - sc; if (x <= W + 80 || x > W + 80 + V) continue;
					double xx = W - 20, yc = QSCaveY(spX[k]), s2 = spTop[k] ? -1 : 1; QSDash(xx, yc + s2 * gg, xx, yc + s2 * (gg - spH), QSW(0.6));
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			double gg = hg(bt), sc = scr(t), yc = QSCaveY(p.x + sc), m = p.r - 2;
			if (p.y < yc - gg + m || p.y > yc + gg - m) return true;
			for (int k = 0; k < spX.Length; k++)
			{
				double x = spX[k] - sc; if (Math.Abs(x - p.x) > 80) continue;
				spike(k, t, gg, out var ax, out var ay, out var bx, out var by, out var tx, out var ty);
				double d1 = (p.x - bx) * (ay - by) - (ax - bx) * (p.y - by), d2 = (p.x - tx) * (by - ty) - (bx - tx) * (p.y - ty), d3 = (p.x - ax) * (ty - ay) - (tx - ax) * (p.y - ay);
				bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
				if (!(neg && pos)) return true;
				if (QSSeg(p.x, p.y, ax, ay, tx, ty) < m || QSSeg(p.x, p.y, bx, by, tx, ty) < m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _qsb = DodgeBuild("L24", () => buildQS());
	static List<Ev> buildQS()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(QSStage(B(346))), true);
		double wn = ez ? 1.4 : 1;   // how long the warnings show (beats)
		foreach (var fb in new[] { 40.0, 80, 104, 120, 140, 180, 220, 240, 280, 320 }) { double b = fb; at(b, () => add(QSFx(b, 0.22, 7, 0.9))); }
		at(160, () => add(QSFx(160, 0.4, 14, 1.4))); at(342, () => add(QSFx(342, 0.4, 10, 1.6)));

		// ===== A. falling blocks (2:34-2:54) =====
		{
			var sp = new List<double>(); for (double b = 1; b <= 35; b += ez ? 2 : 1) sp.Add(b);
						if (!ez) for (double b = 3.5; b <= 35; b += 2) if (!sp.Contains(b)) sp.Add(b);
			sp.Sort();
			foreach (var P in QSBlocksPlan(sp.ToArray(), ez ? 260 : 380, new[] { 9.0, 17, 25, 33, 40 })) { var pp = P; at(pp.bs - wn, () => add(QSBlock(pp, wn))); }
			// when the stack burns, the floor burns with it
			foreach (var cb in new[] { 9.0, 17, 25, 33 }) { double b = cb; at(b - 1.5, () => add(QSBurn(H - 2 * QS_C, H, b - 1.5, b, 0.3))); }
		}

		// ===== B. your own tail + stomps (2:54-3:14) =====
		{
			at(39.5, () => add(QSTail(40.5, 79.5, ez ? 0.75 : 0.3, ez ? 1.4 : 2.4)));
			double[] xs = { 200, 480, 800, 1080 }, ys = { 150, 360, 570 }; int n = 0;
			foreach (var b in QSIn(QS_KICK.Concat(QS_SNR).OrderBy(x => x).ToArray(), 42, 79, ez ? 2 : 0.5))
			{
				double x = xs[n % 4], y = ys[(n / 4 + n) % 3], bb = b; n++; at(bb - wn, () => add(QSStomp(x, y, ez ? 160 : 260, bb - wn, bb, 0.5)));
				// and its twin across the middle
				if (!ez) at(bb - wn, () => add(QSStomp(W - x, H - y, 260, bb - wn, bb, 0.5)));
			}
		}

		// ===== C. the safe bubble (3:14-3:26) =====
		{
			double r = ez ? 190 : 175;
			double[][] P = { new[] { 640.0, 360 }, new[] { 480.0, 280 }, new[] { 760.0, 400 }, new[] { 520.0, 450 }, new[] { 780.0, 300 }, new[] { 640.0, 360 }, new[] { 640.0, 380 } };
			var bubs = new List<QSBub> { new QSBub { b0 = 77, b1 = 80, x0 = 640, y0 = 360, x1 = 640, y1 = 360, r = r } };
			var main = new List<QSBub>();
			for (int s = 0; s < 6; s++)
			{
				var u = new QSBub { b0 = 80 + 4 * s, b1 = 84 + 4 * s, x0 = P[s][0], y0 = P[s][1], x1 = P[s + 1][0], y1 = P[s + 1][1], r = r }; bubs.Add(u); main.Add(u);
				if (s == 1 || s == 3) bubs.Add(new QSBub { b0 = 80 + 4 * s, b1 = 84 + 4 * s, x0 = P[s][0], y0 = P[s][1], x1 = W - P[s + 1][0], y1 = P[s + 1][1], r = r, die = 81.5 + 4 * s });
			}
			at(77, () => add(QSFlood(bubs, 77, 80, 104, 0)));
			int n = 0;
			foreach (var b in QSIn(QS_KICK, 81, 103, ez ? 2 : 1)) { var u = main[(int)Math.Min(5, Math.Floor((b - 80) / 4))]; double ang = 60 * n + 15, bb = b; n++; at(bb - wn, () => add(QSBubBar(u, ang, bb - wn, bb, 0.35, ez ? 10 : 12))); }
		}

		// ===== D. billiards (3:26-3:34, the quiet part) =====
		{
			double r = ez ? 44 : 78;
			double[] sb = ez ? new[] { 104.0, 106, 110, 114 } : new[] { 104.0, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114 };
			double[][] start = { new[] { r, r, 1, 0.62 }, new[] { W - r, H - r, -1, -0.7 }, new[] { W - r, r, -0.8, 1 }, new[] { r, H - r, 1, -0.55 }, new[] { W / 2, r, 0.45, 1 }, new[] { W / 2, H - r, -0.5, -1 }, new[] { r, H / 2, 1, 0.3 } };
			for (int i = 0; i < sb.Length; i++)
			{
				double[] s0 = start[i % start.Length]; double sp = (ez ? 280 : 520 + 15 * i), m = hypot(s0[2], s0[3]), b0 = sb[i];
				at(b0 - 1, () => add(QSBall(s0[0], s0[1], s0[2] / m * sp, s0[3] / m * sp, r, b0 - 1, b0, 119.5)));
			}
		}

		// ===== E. the laser off the mirrors (3:34-3:44) =====
		{
			var pb = new List<double>(); var ang = new List<double[]>(); double wE = ez ? 1.0 : 0.6;
			for (double b = 121; b < 140; b += ez ? 2 : 1) pb.Add(b);
			for (int j = 0; j < pb.Count; j++) { var a = new double[6]; for (int k = 0; k < 6; k++) a[k] = 15 * Math.Floor(QSH(j * 11 + k * 3.7) * 12); ang.Add(a); }
			at(118, () => add(QSMirrorSet(pb, ang, wE, 120, 139.5)));
			double[] ye = { 360, 180, 540 };
			for (int j = 0; j < pb.Count; j++)
			{
				int jj = j; double b = pb[j];
				for (int s = 0; s < (ez ? 1 : 3); s++)
				{
					bool left = (j + s) % 2 == 0; double xe = left ? 0 : W, y0 = ye[(j + s) % 3]; int m = (j * 5 + 1 + 3 * s) % 6;
					var pts = QSBeamPath(xe, y0, Math.Atan2(QS_MY[m] - y0, QS_MX[m] - xe), ang[jj]);
					at(b - wE, () => add(QSBeam(pts, b - wE, b, ez ? 0.45 : 1.0, ez ? 8 : 20)));
				}
			}
		}

		// ===== F. the build-up: the room closes in (3:44-3:54) =====
		at(139, () => add(QSRoom(ez)));
		foreach (var sb in QS_RS) { double b = sb; if (b >= 140) at(b, () => add(QSFx(b, 0, b < 148 ? 3 : b < 156 ? 4 : 6, 0.25))); }

		// ===== G. the turning grating (3:54-4:04) =====
		{
			double sp = ez ? 220 : 170, bwg = ez ? 30 : 44, k = ez ? 0.4 : 0.42;
			at(159, () => add(QSGrate(160, 180, 20, new[] { 160.0, 164, 168, 172, 176 }, new[] { 6 * k, -8 * k, 9 * k, -10 * k, 10 * k }, new[] { 160.0, 162, 164, 166, 168, 170, 172, 174, 176, 178 }, new[] { 40 * k, -50 * k, 60 * k, -60 * k, 70 * k, -70 * k, 80 * k, -80 * k, 90 * k, -90 * k }, sp, bwg, 1)));
					}

		// ===== H. the pendulums (4:04-4:24) =====
		{
			double[] kb = { 180, 196, 212 }, w = ez ? new[] { 60.0, 70, 80 } : new[] { 70.0, 90, 110 };
			double[] tx = ez ? new[] { 213.0, 640, 1067 } : new[] { 0.0, 256, 512, 768, 1024, 1280 };
			for (int i = 0; i < tx.Length; i++) { double x = tx[i], ph = 180 * (i % 2); at(178.5, () => add(QSPend(x, -50, 650, 90, ez ? 58 : 62, ph, kb, w, 180, 220, ez ? 36 : 72, 20))); }
			double[] bx = new double[0]; double bs = ez ? 204 : 192;
			for (int i = 0; i < bx.Length; i++) { double x = bx[i], ph = 90 + 180 * (i % 2); at(bs - 1.5, () => add(QSPend(x, H + 50, 500, 270, 58, ph, kb, w, bs, 220, ez ? 36 : 72, 20))); }
			// the floor between the low pivots burns on each bar
			for (double fb = 186; fb < 220; fb += ez ? 8 : 4) { double b = fb; at(b - 1.5, () => add(QSBurn(H - 70, H, b - 1.5, b, 0.4))); }
		}

		// ===== I. the fireworks (4:24-4:34, the top of the hard part) =====
		{
			var L = new List<(double bb, int n, double v)>();
			if (ez) { for (double b = 221; b < 236; b += 2) L.Add((b, 8, 220)); for (double b = 236; b < 239; b += 1) L.Add((b, 10, 240)); }
			else { for (double b = 221; b <= 228; b += 0.5) L.Add((b, 20, 370)); for (double b = 228.5; b < 236; b += 0.5) { L.Add((b, 22, 400)); L.Add((b, 18, 390)); } for (double b = 236; b < 239.5; b += 0.5) { L.Add((b, 24, 430)); L.Add((b, 20, 420)); L.Add((b, 12, 400)); } }
			int n = 0;
			foreach (var (bb, cnt, v) in L)
			{
				int i = n++; double xa = 140 + ((i * 5) % 9) * 125, ya = i % 2 == 0 ? 170 : 300, x0 = xa + (i % 2 == 0 ? -120 : 120), a0 = i * 7.0, b = bb;
				at(b - 1 - wn, () => add(QSRocket(x0, xa, ya, wn, b - 1, b, cnt, v, 260, a0)));
			}
			at(239.5 - 1 - wn, () => add(QSRocket(640, 640, 300, wn, 238.5, 239.5, ez ? 16 : 32, ez ? 260 : 380, 200, 0)));
		}

		// ===== J. bombs (4:34-4:54) =====
		{
			double fuse = ez ? 3 : 2, wJ = ez ? 1.4 : 1; int n = 0;
			// a bomb on every half beat (Easy: every 2 beats); on each bar's first beat a row of three
			for (double b = 242; b < 279.5; b += ez ? 2 : 0.5)
			{
				int q = (int)Math.Floor((b - 240) / 4), k = n++, dir = q % 2 == 0 ? 1 : -1, cnt = !ez && b % 4 == 0 ? 3 : 1;
				for (int c = 0; c < cnt; c++)
				{
					int ci = QSModI(2 + 5 * q + dir * 3 * k + 5 * c, 16), cj = QSModI(1 + 2 * q + 5 * k + 3 * c, 9); double bb = b;
					at(bb - fuse, () => add(QSBomb(ci, cj, bb - fuse, wJ, bb, 0.45, ez ? 28 : 30)));
				}
			}
		}

		// ===== K. searchlights and pillars (4:54-5:14) =====
		{
			at(278, () => add(QSPillars(280, 319.5)));
			double ca = ez ? 8 : 8, per = ez ? 8 : 7;
			double[] lx = ez ? new[] { 330.0, 950 } : new[] { 200.0, 640, 1080 };
			for (int i = 0; i < lx.Length; i++) { double x = lx[i], ph = 280 + 3 * i; at(278.5, () => add(QSLight(x, -12, 90, ez ? 55 : 68, per, ph, ca, 280, 319.5))); }
			if (!ez) foreach (var x in new[] { 420.0, 860 }) { double xx = x, ph = 300 + (x < W / 2 ? 0 : 3); at(298.5, () => add(QSLight(xx, H + 12, 270, 50, per, ph, 11, 300, 319.5))); }
		}

		// ===== L. the cave run (5:14-5:25) =====
		{
			double V = ez ? 360 : 420, T0 = B(320); var kx = QSIn(QS_KICK, 322, 340, ez ? 2 : 1);
			var spX = kx.Select(b => (B(b) - T0) * V + W / 2).ToArray(); var spT = kx.Select((b, i) => i % 2 == 0).ToArray();
			at(319, () => add(QSCave(320, 342, V, ez ? 230 : 190, ez ? 200 : 160, spX, spT, ez ? 110 : 140)));
		}

		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
	static int QSModI(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }
}
