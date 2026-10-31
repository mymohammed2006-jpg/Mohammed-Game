using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24: part 3, the fate (4:34-5:25): the threads of fate, the scale and its weights, the eye closing; and the events.
public static partial class G
{
	// ---------- 9. the threads of fate (4:34) ----------
	// A thread between two gold pegs: from (ax0, ay0)-(bx0, by0) at b to (ax1, ay1)-(bx1, by1) at b + dur (the pegs slide along the
	// screen's edges). It has one knot: a gap of kl px round the point ku (0-1) along it, ringed in gold. It shows dashed (with gold
	// arrows at the pegs: where they will slide) from bw, pink from b to b + dur.
	public static Hz QSThread(double ax0, double ay0, double bx0, double by0, double ax1, double ay1, double bx1, double by1, double ku, double kl, double bw, double b, double dur)
	{
		var h = new Hz { kind = "QSThread" };
		h.update = (t, dt) => { if (BT(t) > b + dur + 0.3) h.dead = true; };
		void at(double bt, out double ax, out double ay, out double bx, out double by)
		{
			double u = QSk(bt, b, b + dur); ax = ax0 + (ax1 - ax0) * u; ay = ay0 + (ay1 - ay0) * u; bx = bx0 + (bx1 - bx0) * u; by = by0 + (by1 - by0) * u;
		}
		// the two pieces either side of the knot
		void pieces(double bt, Action<double, double, double, double> f)
		{
			at(bt, out var ax, out var ay, out var bx, out var by); double l = hypot(bx - ax, by - ay), u0 = ku - kl / 2 / l, u1 = ku + kl / 2 / l;
			f(ax, ay, ax + (bx - ax) * u0, ay + (by - ay) * u0); f(ax + (bx - ax) * u1, ay + (by - ay) * u1, bx, by);
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + dur + 0.3) return;
			g.save(); g.lineCap = "round";
			at(bt, out var ax, out var ay, out var bx, out var by);
			double kx = ax + (bx - ax) * ku, ky = ay + (by - ay) * ku;
			if (bt < b)
			{
				double q = QSk(bt, bw, b); g.strokeStyle = QSW(0.25 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				pieces(bt, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); g.setLineDash(new double[0]);
				// gold arrows: where the pegs go
				g.strokeStyle = QSG(0.4 + 0.5 * q); g.lineWidth = 3;
				void arrow(double x0, double y0, double x1, double y1)
				{
					double dx = x1 - x0, dy = y1 - y0, l = hypot(dx, dy); if (l < 1) return; dx /= l; dy /= l;
					double px = clamp(x0, 12, W - 12) + dx * 40, py = clamp(y0, 12, H - 12) + dy * 40;
					g.beginPath(); g.moveTo(px - dx * 30, py - dy * 30); g.lineTo(px, py); g.moveTo(px - dx * 12 - dy * 9, py - dy * 12 + dx * 9); g.lineTo(px, py); g.lineTo(px - dx * 12 + dy * 9, py - dy * 12 - dx * 9); g.stroke();
				}
				arrow(ax0, ay0, ax1, ay1); arrow(bx0, by0, bx1, by1);
			}
			else
			{
				double al = 1 - QSk(bt, b + dur, b + dur + 0.3);
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = QSP(al); g.lineWidth = 6; g.beginPath();
				pieces(bt, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); g.shadowBlur = 0;
			}
			// the knot's gold ring and the pegs (harmless)
			g.strokeStyle = QSG(0.85); g.lineWidth = 3; g.beginPath(); g.arc(kx, ky, kl / 2 - 6, 0, TAU); g.stroke();
			g.fillStyle = QSG(0.9); circle(clamp(ax, 6, W - 6), clamp(ay, 6, H - 6), 7); circle(clamp(bx, 6, W - 6), clamp(by, 6, H - 6), 7);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b || bt >= b + dur) return false; bool hit = false;
			pieces(bt, (x0, y0, x1, y1) => { if (!hit && QSSeg(p.x, p.y, x0, y0, x1, y1) < 3 + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 10. the scale (4:54) ----------
	// A balance hangs from the top (gold beam, harmless); its two pans are pink bowls. It tips to one side then the other every 2 beats
	// (from b0), so the bowls go up and down. QSScaleAt gives the bowls' middles at beat bt.
	const double QS_PX = W / 2, QS_PY = 96, QS_BL = 400, QS_CH = 200, QS_BR = 88;
	static double QSTilt(double bt, double b0)
	{
		if (bt < b0) return 0; int j = (int)Math.Floor((bt - b0) / 2); double prev = j == 0 ? 0 : (j % 2 == 1 ? 24 : -24), cur = j % 2 == 0 ? 24 : -24;
		return prev + (cur - prev) * QSE(QSk(bt - b0 - 2 * j, 0, 0.6));
	}
	public static void QSScaleAt(double bt, double b0, out double lx, out double ly, out double rx, out double ry)
	{
		double a = QSRad(QSTilt(bt, b0)), c = JS.cos(a), s = JS.sin(a);
		lx = QS_PX - c * QS_BL; ly = QS_PY - s * QS_BL + QS_CH; rx = QS_PX + c * QS_BL; ry = QS_PY + s * QS_BL + QS_CH;
	}
	public static Hz QSScale(double b0, double b1, double bw)
	{
		var h = new Hz { kind = "QSScale" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b1 + 1) return; double al = QSk(bt, bw, bw + 1) * (1 - QSk(bt, b1, b1 + 1));
			double a = QSRad(QSTilt(bt, b0)), c = JS.cos(a), s = JS.sin(a);
			QSScaleAt(bt, b0, out var lx, out var ly, out var rx, out var ry);
			g.save(); g.globalAlpha = al; g.lineCap = "round";
			// the post, the beam, the chains (gold, harmless)
			g.strokeStyle = QSG(0.8); g.lineWidth = 6; g.beginPath(); g.moveTo(QS_PX, -10); g.lineTo(QS_PX, QS_PY); g.stroke();
			g.lineWidth = 8; g.beginPath(); g.moveTo(QS_PX - c * QS_BL, QS_PY - s * QS_BL); g.lineTo(QS_PX + c * QS_BL, QS_PY + s * QS_BL); g.stroke();
			g.fillStyle = QSG(0.95); circle(QS_PX, QS_PY, 12);
			g.lineWidth = 2; g.strokeStyle = QSG(0.6); g.beginPath();
			foreach (var (ex, ey, px, py) in new[] { (QS_PX - c * QS_BL, QS_PY - s * QS_BL, lx, ly), (QS_PX + c * QS_BL, QS_PY + s * QS_BL, rx, ry) })
			{ g.moveTo(ex, ey); g.lineTo(px - QS_BR, py); g.moveTo(ex, ey); g.lineTo(px + QS_BR, py); }
			g.stroke();
			// the bowls
			foreach (var (px, py) in new[] { (lx, ly), (rx, ry) })
			{
				if (bt < b0) { g.strokeStyle = QSW(0.6); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(px, py, QS_BR, 0, Math.PI); g.closePath(); g.stroke(); g.setLineDash(new double[0]); }
				else { g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(0.85); g.beginPath(); g.arc(px, py, QS_BR, 0, Math.PI); g.closePath(); g.fill(); g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 3; g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			QSScaleAt(bt, b0, out var lx, out var ly, out var rx, out var ry);
			foreach (var (px, py) in new[] { (lx, ly), (rx, ry) }) if (p.y > py - (p.r - 2) && hypot(p.x - px, p.y - py) < QS_BR + p.r - 2) return true;
			return false;
		};
		return h;
	}
	// A weight (a pink block, size sz) falls in the column x from the top at b at sp px/s; the column shows dashed from bw.
	public static Hz QSWeight(double x, double sz, double bw, double b, double sp)
	{
		var h = new Hz { kind = "QSWeight" }; double tf = B(b);
		double y(double t) => -sz + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && y(t) - sz > H + 20) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save();
			if (bt < b) { double q = QSk(bt, bw, b); QSDash(x - sz / 2, 0, x - sz / 2, H, QSW(0.2 + 0.45 * q)); QSDash(x + sz / 2, 0, x + sz / 2, H, QSW(0.2 + 0.45 * q)); }
			else
			{
				double yy = y(t); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath();
				g.moveTo(x - sz / 2, yy + sz / 2); g.lineTo(x - sz * 0.32, yy - sz / 2); g.lineTo(x + sz * 0.32, yy - sz / 2); g.lineTo(x + sz / 2, yy + sz / 2); g.closePath(); g.fill();
				g.shadowBlur = 0; g.strokeStyle = QSG(0.9); g.lineWidth = 3; g.beginPath(); g.arc(x, yy - sz / 2 - 6, 7, 0, TAU); g.stroke();
			}
			g.restore();
		};
		// the weight is narrower at its top: the hit follows its drawn shape
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double yy = y(t), dy = p.y - yy; if (Math.Abs(dy) >= sz / 2 + p.r - 2) return false;
			double hw = sz * 0.32 + sz * 0.18 * clamp((dy + sz / 2) / sz, 0, 1);
			return Math.Abs(p.x - x) < hw + p.r - 2;
		};
		return h;
	}

	// ---------- 11. the eye closes (5:14) ----------
	// The eye fills the screen; what is outside its opening is pink (its lids). The opening is the almond round (cx, H/2), half width
	// QS_RX, half height h (the lower lid as high as the eye's own). At each step beat sb[i] it eases (0.75 beat) to the size sh[i] and the middle sx[i];
	// the next almond shows dashed the beat before.
	const double QS_RX = 600, QS_LO = 0.5 / 0.62;   // the lower lid is as high as the eye's own (QSEyeDraw)
	static readonly double[] QS_LSB = { 321, 325, 329, 333, 337, 340 }, QS_LSX = { 640, 520, 760, 560, 720, 640 };
	static readonly double[] QS_LSHN = { 420, 310, 240, 190, 150, 120 }, QS_LSHE = { 470, 400, 340, 290, 250, 220 };
	public static void QSLidsAt(double bt, out double cx, out double hh)
	{
		var sh = easy ? QS_LSHE : QS_LSHN; cx = QS_LSX[0]; hh = sh[0];
		for (int i = 1; i < QS_LSB.Length; i++) { if (bt < QS_LSB[i]) break; double u = QSE(QSk(bt, QS_LSB[i], QS_LSB[i] + 0.75)); cx = QS_LSX[i - 1] + (QS_LSX[i] - QS_LSX[i - 1]) * u; hh = sh[i - 1] + (sh[i] - sh[i - 1]) * u; }
	}
	public static Hz QSLids(double b0, double b1)
	{
		var h = new Hz { kind = "QSLids" }; double[] sb = QS_LSB, sx = QS_LSX, sh = easy ? QS_LSHE : QS_LSHN;
		h.update = (t, dt) => { if (BT(t) > b1 + 3) h.dead = true; };
		void st(double bt, out double cx, out double hh) => QSLidsAt(bt, out cx, out hh);
		double up(double x, double cx, double hh) { double d = (x - cx) / QS_RX; return Math.Abs(d) >= 1 ? H / 2 : H / 2 - hh * (1 - d * d); }
		double lo(double x, double cx, double hh) { double d = (x - cx) / QS_RX; return Math.Abs(d) >= 1 ? H / 2 : H / 2 + QS_LO * hh * (1 - d * d); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double al = QSk(bt, b0 - 1, b0);
			st(bt, out var cx, out var hh);
			g.save();
			if (bt < b0)
			{
				g.strokeStyle = QSW(0.6 * al); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				for (int i = 0; i <= 64; i++) { double x = W * i / 64.0; if (i == 0) g.moveTo(x, up(x, cx, hh)); else g.lineTo(x, up(x, cx, hh)); }
				for (int i = 64; i >= 0; i--) { double x = W * i / 64.0; g.lineTo(x, lo(x, cx, hh)); }
				g.stroke(); g.setLineDash(new double[0]); g.restore(); return;
			}
			// the lids (pink)
			g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(0.62);
			g.beginPath(); g.moveTo(-10, -10); g.lineTo(W + 10, -10); g.lineTo(W + 10, H / 2);
			for (int i = 64; i >= 0; i--) { double x = W * i / 64.0; g.lineTo(x, up(x, cx, hh)); }
			g.lineTo(-10, H / 2); g.closePath(); g.fill();
			g.beginPath(); g.moveTo(-10, H + 10); g.lineTo(W + 10, H + 10); g.lineTo(W + 10, H / 2);
			for (int i = 64; i >= 0; i--) { double x = W * i / 64.0; g.lineTo(x, lo(x, cx, hh)); }
			g.lineTo(-10, H / 2); g.closePath(); g.fill(); g.shadowBlur = 0;
			// the lashes: short strokes out of the upper lid into the pink, and the lids' edges
			g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath();
			for (int i = 0; i <= 64; i++) { double x = W * i / 64.0; if (i == 0) g.moveTo(x, up(x, cx, hh)); else g.lineTo(x, up(x, cx, hh)); }
			for (int i = 0; i <= 64; i++) { double x = W * i / 64.0; if (i == 0) g.moveTo(x, lo(x, cx, hh)); else g.lineTo(x, lo(x, cx, hh)); }
			for (int i = 2; i < 40; i++) { double x = cx - QS_RX + 2 * QS_RX * i / 41.0, y = up(x, cx, hh), d = (x - cx) / QS_RX; g.moveTo(x, y); g.lineTo(x + d * 18, y - 24); }
			g.stroke();
			// the next almond (dashed white) the beat before each step
			for (int i = 1; i < sb.Length; i++)
			{
				if (bt < sb[i] - 1 || bt >= sb[i]) continue; double q = QSk(bt, sb[i] - 1, sb[i]);
				g.strokeStyle = QSW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				for (int k = 0; k <= 64; k++) { double x = W * k / 64.0; if (k == 0) g.moveTo(x, up(x, sx[i], sh[i])); else g.lineTo(x, up(x, sx[i], sh[i])); }
				for (int k = 0; k <= 64; k++) { double x = W * k / 64.0; if (k == 0) g.moveTo(x, lo(x, sx[i], sh[i])); else g.lineTo(x, lo(x, sx[i], sh[i])); }
				g.stroke(); g.setLineDash(new double[0]);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; st(bt, out var cx, out var hh);
			return p.y < up(p.x, cx, hh) + p.r - 2 || p.y > lo(p.x, cx, hh) - p.r + 2;
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _qsb = DodgeBuild("L24", () => buildQS());
	static double[] QSIn(double[] l, double a, double b, double gap = 0)
	{
		var o = new List<double>(); double last = -1e9;
		foreach (var x in l) if (x >= a && x < b && x - last >= gap - 1e-9) { o.Add(x); last = x; }
		return o.ToArray();
	}
	static readonly double QS_DIAG = Math.Atan2(H, W) * 180 / Math.PI;   // the screen's diagonal (corner to corner), degrees
	static List<Ev> buildQS()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(QSStage(B(346))), true);
		double wn = ez ? 1.4 : 1;   // how long the warnings show (beats)

		// ===== 1. the look (2:34-3:44) =====
		// the split: one line through the middle every 2 beats, then a # every bar (two parallel lines, then the two across), turning 22.5 a bar;
		// from 2:42 also a line corner to corner on each bar's first beat
		{
			for (double b = 2; b < 8; b += 2) { double ang = 90 * ((b / 2) % 2) + 45; double bb = b; at(bb - wn, () => add(QSSplit(W / 2, H / 2, ang, bb - wn, bb, 0.6, ez ? 18 : 34))); }
			for (int j = 2; j < 10; j++)
			{
				double th = 22.5 * j, d = new[] { 150.0, 270, 360, 200, 320, 120, 280, 390 }[j % 8];
				for (int k = 0; k < 4; k++)
				{
					if (ez && k % 2 == 1) continue;
					double b = 4 * j + k, ang = th + (k >= 2 ? 90 : 0), sg = k % 2 == 0 ? 1 : -1, n = QSRad(ang + 90), cx = W / 2 + JS.cos(n) * d * sg, cy = H / 2 + JS.sin(n) * d * sg;
					if (ez) { cx = W / 2; cy = H / 2; }
					double w0 = ez ? 1.4 : 0.85;
					at(b - w0, () => add(QSSplit(cx, cy, ang, b - w0, b, 0.7, ez ? 18 : 36)));
					if (!ez && k == 0 && j >= 4) { double a2 = j % 2 == 0 ? QS_DIAG : 180 - QS_DIAG; at(b - w0, () => add(QSSplit(W / 2, H / 2, a2, b - w0, b, 0.7, 36))); }
				}
			}
		}
		// the eye's first looks: its gaze sweeps across three times
		{
			double[] g0 = { 10, 22, 34 }, r1 = { 240, 360, 180 };
			for (int k = 0; k < 3; k++) { double b = g0[k], a0 = k % 2 == 0 ? 195 : -15, rr = r1[k]; at(b - 2, () => add(QSGaze(W / 2, 112, 1, a0, 180 - a0, b - 2, b, b + 4, ez ? 10 : 14, rr, rr + (ez ? 180 : 140)))); }
		}
		at(40, () => add(QSFx(40, 0.3, 8, 1)));
		// the shares: three shares light on each kick (the next ones round each time), every other share on the first kick of a bar;
		// the circle turns, one way then the other
		{
			var kb = QSIn(QS_KICK, 41, 79, ez ? 2 : 1); int s = 0;
			foreach (var b in kb)
			{
				bool half = !ez && Math.Abs(b - 4 * Math.Floor(b / 4)) < 0.3; int m;
				if (ez) m = (1 << (s % 8)) | (1 << ((s + 4) % 8));
				else if (half) m = 0x55 << (s % 2);
				else m = (1 << (s % 8)) | (1 << ((s + 2) % 8)) | (1 << ((s + 4) % 8));
				m &= 0xff; s++;
				double spin = (Math.Floor((b - 40) / 8) % 2 == 0 ? 1 : -1) * (ez ? 8 : 16), a0 = 7 * b, w0 = ez ? 1.4 : 0.9;
				at(b - w0, () => add(QSShares(W / 2, H / 2, a0, spin, m, b - w0, b, ez ? 0.5 : 0.6)));
			}
		}
		at(80, () => add(QSFx(80, 0.3, 8, 1)));
		// the lashes: a fan from the eye on each kick, its gaps moving over by a quarter step each time; from the eye's corners every other bar;
		// from 3:18 a second fan half a beat later
		{
			var kb = QSIn(QS_KICK, 81, 102, ez ? 2 : 1); int i = 0;
			foreach (var b in kb)
			{
				int n = ez ? 7 : 19; double sp = ez ? 340 : 580, spread = 210, off = (i % 4 - 1.5) * 0.25 * spread / (n - 1);
				int bar = (int)Math.Floor((b - 80) / 4); double ex = bar % 2 == 1 ? W / 2 + (i % 2 == 0 ? -130 : 130) : W / 2, ang = 90 + off + (ex - W / 2) * -0.08;
				i++; at(b - wn, () => add(QSFan(ex, 112, n, ang, spread, b - wn, b, sp)));
				if (!ez && b >= 88 && b + 0.5 < 102) { double ex2 = W - ex, ang2 = 90 - off + (ex2 - W / 2) * -0.08; at(b - 0.5, () => add(QSFan(ex2, 112, 9, ang2, 130, b - 0.5, b + 0.5, 420))); }
			}
		}
		// the gaze sweeps across from the eye three times (down, then up over the top, then down again); its blind spot is the way through
		{
			double[] g0 = { 84, 90, 96 }; double[] r1 = { 200, 380, 270 };
			for (int k = 0; k < 3; k++) { double b = g0[k], a0 = k % 2 == 0 ? -15 : 195, a1 = k == 1 ? 345 : 180 - a0, rr = r1[k]; at(b - 1.5, () => add(QSGaze(W / 2, 112, 1, a0, a1, b - 1.5, b, b + 4, ez ? 10 : 14, rr, rr + (ez ? 170 : 125)))); }
		}
		// the kohl: the eye comes to the middle and the kohl traces its lids, the upper one then the lower one each bar; tears of darts on each beat;
		// a line corner to corner at the end of each bar
		{
			for (int j = 0; j < 4; j++)
			{
				double b = 104 + 4 * j, rx = j % 2 == 0 ? 560 : 470, ry = j % 2 == 0 ? 250 : 170;
				at(b - 1, () => add(QSKohl(W / 2, H / 2, rx, ry, true, b, b + 2, ez ? 0.9 : 1.5, 14)));
				at(b + 1, () => add(QSKohl(W / 2, H / 2, rx, ry, false, b + 2, b + 4, ez ? 0.9 : 1.5, 14)));
				{ double bb = b + 3.5, a2 = j % 2 == 0 ? QS_DIAG : 180 - QS_DIAG, w0 = ez ? 1.4 : 1; at(bb - w0, () => add(QSSplit(W / 2, H / 2, a2, bb - w0, bb, 0.5, ez ? 16 : 28))); }
				for (int k = 1; k < 4; k += ez ? 4 : 2) { double bb = b + k, a0 = new[] { 0.0, 10, 5, 15, 2.5, 12.5, 7.5, 17.5 }[2 * j + (k - 1) / 2]; at(bb - wn, () => add(QSBurst(W / 2, H / 2, ez ? 10 : 18, a0, bb - wn, bb, ez ? 240 : 440))); }
			}
			// its gaze: two beams from the middle turning half a turn each 8 beats
			at(103, () => add(QSGaze(W / 2, H / 2, 2, 0, 180, 103, 104, 112, ez ? 10 : 14, 150, ez ? 320 : 280)));
			at(111, () => add(QSGaze(W / 2, H / 2, 2, 180, 360, 111, 112, 119.5, ez ? 10 : 14, 170, ez ? 340 : 300)));
		}
		at(120, () => add(QSFx(120, 0.35, 10, 1.2)));
		// the split and the lashes together: diagonal lines on the claps (through each corner in turn), a fan from the eye every 2 beats
		{
			var sn = QSIn(QS_SNR, 120.5, 137, ez ? 2 : 1); int i = 0;
			foreach (var b in sn)
			{
				double ang = i % 2 == 0 ? 45 : 135, x = new[] { 340.0, 940, 940, 340, 640, 640 }[i % 6], w0 = ez ? 1.4 : 0.85; i++;
				at(b - w0, () => add(QSSplit(x, H / 2, ang, b - w0, b, 0.6, ez ? 18 : 34)));
			}
			at(122.5, () => add(QSGaze(W / 2, 112, 1, 195, 345, 122.5, 124, 128, ez ? 10 : 14, 330, ez ? 500 : 455)));
			// then the eye looks round from the middle: two beams, half a turn
			at(129, () => add(QSGaze(W / 2, H / 2, 2, 90, 270, 129, 130, 136, ez ? 10 : 14, 60, ez ? 210 : 170)));
			for (double b = 122; b < 137; b += ez ? 4 : 2) { double bb = b, off = new[] { -4.4, 1.5, -1.5, 4.4 }[(int)((b - 122) / 2) % 4]; at(bb - wn, () => add(QSFan(W / 2, 112, ez ? 5 : 17, 90 + off * (ez ? 1 : 0.75), 140, bb - wn, bb, ez ? 330 : 500))); }
		}

		// ===== the build-up (3:44-3:54): rings of beads leave the eye, faster and faster =====
		at(140, () => add(QSFx(140, 0.25, 6, 1)));
		{
			for (int i = 0; i < 8; i++)
			{
				double b = 142 + 2 * i, ga = 35 * i + 20, spin = (i % 2 == 0 ? 1 : -1) * (10 + 4 * i); int n = (ez ? 26 : 44) + 2 * i;
				at(b - 1, () => add(QSRing(W / 2, H / 2, n, 0, spin, new[] { ga, ga + 180 }, ez ? 56 : 36, b - 1, b, ez ? 6 : 5)));
			}
			foreach (var b in ez ? new[] { 152.0, 156, 158 } : new[] { 148.0, 150, 152, 154, 155, 156, 157, 158, 158.5, 159, 159.5 })
			{ double ang = new[] { QS_DIAG, 180 - QS_DIAG, 90, 0 }[(int)Math.Round(2 * b) % 4], w0 = ez ? 1.4 : 0.8; at(b - w0, () => add(QSSplit(W / 2, H / 2, ang, b - w0, b, 0.4, ez ? 16 : 24))); }
		}

		// ===== 2. the division (3:54-4:34), the hardest part =====
		at(160, () => add(QSFx(160, 0.45, 14, 1.4)));
		// the star breathes from the middle out past the screen's corners (every 3.5 beats; its notches too): slip through a notch (gold) when a blade comes;
		// darts fly out along the notches on each kick
		var S1 = new QSStarDef { b0 = 160, b1 = 180, rmin = 24, rmax = 1000, per = ez ? 4 : 3.5, gap = ez ? 60 : 40, a0 = 0, kb = new[] { 160.0, 164, 168, 172, 176 },
			w = ez ? new[] { 6.0, -6, 7, -7, 8 } : new[] { 16.0, -16, 18, -18, 20 } };
		at(156, () => add(QSStar(S1, 156)));
		{
			double w0 = ez ? 1 : 0.6;
			foreach (var b in QSIn(QS_KICK, 161, 179.5, ez ? 2 : 1)) { double a0 = S1.Ang(b) + 22.5; at(b - w0, () => add(QSBurst(W / 2, H / 2, ez ? 8 : 16, a0, b - w0, b, ez ? 400 : 640))); }
			if (!ez) foreach (var b in QSIn(QS_SNR, 162, 179.5, 1)) at(b - w0, () => add(QSStarRay(S1, false, 1, b - w0, b, 0.4)));
		}
		at(180, () => add(QSFx(180, 0.4, 12, 1.2)));
		// the spiral: four arms whose turn flips every 2 beats, faster and faster; the gaze turning; rings with one gap; lines on the claps
		{
			var kb2 = new List<double>(); var w2 = new List<double>(); for (int i = 0; i < 20; i++) { kb2.Add(180 + 2 * i); w2.Add((i % 2 == 0 ? 1 : -1) * (ez ? 24 : 44 + 1.2 * i)); }
			at(179, () => add(QSSpiral(W / 2, H / 2, ez ? 3 : 4, ez ? 2 : 5, 180, 220, ez ? 280 : 390, 45, kb2.ToArray(), w2.ToArray())));
			// the gaze: two beams from the eye turning half a turn every 8 beats, the other way each time
			for (int k = 0; k < 5; k++) { double b = 180 + 8 * k, a0 = 90 * k + 20, a1 = a0 + (k % 2 == 0 ? 180 : -180), rr = 160 + 70 * (k % 3); at(b - 1, () => add(QSGaze(W / 2, H / 2, 2, a0, a1, b - 1, b, b + 7.5, ez ? 10 : 13, rr, rr + (ez ? 180 : 130)))); }
			// rings with one gap leave the eye every 2 beats; the gap jumps round
			for (int k = 0; k < 19; k++) { double b = 182 + 2 * k, ga = 135 * k + 30, spn = (k % 2 == 0 ? 1 : -1) * 10; int n = ez ? 30 : 44; if (ez && k % 2 == 1) continue; at(b - 1, () => add(QSRing(W / 2, H / 2, n, 0, spn, new[] { ga }, ez ? 70 : 40, b - 1, b, ez ? 6 : 4.5, 10))); }
			int i2 = 0;
			if (!ez) foreach (var b in QSIn(QS_SNR, 181, 219, 4))
			{
				double ang = 45 * i2 + 22.5, n = QSRad(ang + 90), sg = i2 % 2 == 0 ? 1 : -1, cx = W / 2 + JS.cos(n) * 220 * sg, cy = H / 2 + JS.sin(n) * 220 * sg, w0 = ez ? 1.4 : 0.8; i2++;
				at(b - w0, () => add(QSSplit(cx, cy, ang, b - w0, b, 0.45, ez ? 14 : 24)));
			}
		}
		at(220, () => add(QSFx(220, 0.4, 12, 1.2)));
		// the star again, the other way round; darts through its notches on each kick, rays from its tips
		var S2 = new QSStarDef { b0 = 220, b1 = 237, rmin = 24, rmax = 1000, per = ez ? 4 : 2.5, gap = ez ? 60 : 40, a0 = 11.25, kb = new[] { 220.0, 224, 228, 232 },
			w = ez ? new[] { -6.0, 6, -7, 7 } : new[] { -9.0, 9, -10, 10 } };
		at(217, () => add(QSStar(S2, 217)));
		{
			double w0 = ez ? 1 : 0.6;
			foreach (var b in QSIn(QS_KICK, 221, 236, ez ? 2 : 1)) { double a0 = S2.Ang(b) + 22.5; at(b - w0, () => add(QSBurst(W / 2, H / 2, ez ? 8 : 12, a0, b - w0, b, ez ? 400 : 640))); }
			for (double b = 221.5; b < 236; b += ez ? 4 : 2) { double bb = b; at(bb - w0, () => add(QSStarRay(S2, false, 2, bb - w0, bb, 0.4))); }
			at(236 - wn, () => add(QSBurst(W / 2, H / 2, ez ? 12 : 20, 0, 236 - wn, 236, ez ? 420 : 600)));
			if (!ez) at(236.5 - wn, () => add(QSBurst(W / 2, H / 2, 20, 9, 236.5 - wn, 236.5, 600)));
		}

		// ===== 3. the fate (4:34-5:25) =====
		at(240, () => add(QSFx(240, 0.4, 12, 1.4)));
		// the threads: a thread every 2 beats; turning through the middle (bars 1-4), sliding across (bars 5-8), then two at once (9-10)
		{
			double kl = ez ? 150 : 64;
			for (int i = 0; i < 19; i++)
			{
				double b = 242 + 2 * i, ku = i % 2 == 0 ? 0.28 : 0.72, dur = ez ? 4 : 2.3; int bar = (i * 2) / 4;
				if (ez && i % 2 == 1) continue;
				if (bar < 4)
				{
					if (i % 2 == 0) at(b - wn, () => add(QSThread(-40, -20, W + 40, H + 20, W + 40, -20, -40, H + 20, ku, kl, b - wn, b, dur)));
					else at(b - wn, () => add(QSThread(-20, H + 40, W + 20, -40, -20, -40, W + 20, H + 40, ku, kl, b - wn, b, dur)));
				}
				else if (bar < 8)
				{
					double dir = i % 2 == 0 ? 1 : -1, x0 = dir > 0 ? -60 : W + 60, x1 = x0 + dir * 760;
					double ln = (i / 2) % 2 == 0 ? 260 : -260; at(b - wn, () => add(QSThread(x0 + dir * Math.Max(0, ln), -20, x0 - dir * Math.Min(0, ln), H + 20, x1 + dir * Math.Max(0, ln), -20, x1 - dir * Math.Min(0, ln), H + 20, ku, kl, b - wn, b, dur)));
				}
				else
				{
					double y0 = i % 2 == 0 ? 80 : 640, dir = i % 2 == 0 ? 1 : -1;
					at(b - wn, () => add(QSThread(-20, y0, W + 20, y0 + dir * 120, -20, y0 + dir * 480, W + 20, y0 + dir * 600, 1 - ku, kl, b - wn, b, dur)));
					if (!ez) { double x0 = i % 4 == 0 ? 200 : 1080; at(b - wn, () => add(QSThread(x0, -20, x0, H + 20, W - x0, -20, W - x0, H + 20, ku, kl, b - wn, b, dur))); }
				}
			}
			// beads fall from the knots' gold rings on the claps (a short fan down)
			if (!ez) foreach (var b in QSIn(QS_SNR, 243, 278, 1)) { double x = 240 + 800 * QSH(b); at(b - 0.8, () => add(QSFan(x, -10, 9, 90, 80, b - 0.8, b, 500, 30))); }
		}
		at(280, () => add(QSFx(280, 0.35, 10, 1.2)));
		// the scale: its bowls go up and down; a weight falls on each beat (across the whole width in turn); the lower bowl spills grains on the claps
		{
			at(276, () => add(QSScale(280, 318, 277)));
			double[] cols = { 576, 64, 1088, 320, 832, 192, 704, 1216, 448, 960 };
			int i = 0;
			for (double b = 281; b < 320; b += ez ? 2 : 1) { double x = cols[i++ % cols.Length], bb = b; if (ez) x = cols[(i * 3) % cols.Length]; at(bb - wn, () => add(QSWeight(x, 132, bb - wn, bb, ez ? 520 : 900))); }
			foreach (var b in QSIn(QS_SNR, 282, 317, ez ? 4 : 1))
			{
				QSScaleAt(b, 280, out var lx, out var ly, out var rx, out var ry); bool lft = ly > ry; double px = lft ? lx : rx, py = (lft ? ly : ry) + QS_BR * 0.6;
				at(b - wn, () => add(QSFan(px, py, ez ? 3 : 11, 90 + (lft ? 15 : -15), 120, b - wn, b, ez ? 300 : 540, 30)));
			}
		}
		at(320, () => add(QSFx(320, 0.35, 10, 1.2)));
		// the eye closes: its lids come in a bar at a time and it looks left and right; lines down and across the opening on the claps (somewhere else each time)
		{
			double[] sx = QS_LSX;
			at(319, () => add(QSLids(321, 342)));
			int m = 0;
			foreach (var b in QSIn(QS_SNR, 322, 341, ez ? 4 : 1))
			{
				int i = (int)Math.Floor((b - 321) / 4); double cx = sx[Math.Min(sx.Length - 1, i)], x = cx, y = H / 2.0, an = 90;
				// down through the opening, then across it, each time somewhere else
				if (m % 2 == 0) x = cx + new[] { -240.0, 120, -120, 240, 0, -60, 180, -180, 60 }[m / 2 % 9];
				else { y = H / 2 + new[] { 100.0, -100, 40, -160, 160, -40 }[m / 2 % 6]; an = 0; }
				m++;
				at(b - wn, () => add(QSSplit(x, y, an, b - wn, b, 0.45, ez ? 14 : 22)));
			}
			// tears from the middle of the eye on the kicks, up then down in turn
			int u = 0;
			if (!ez) foreach (var b in QSIn(QS_KICK, 322, 341, 1)) { double cx = QS_LSX[Math.Min(QS_LSX.Length - 1, (int)Math.Floor((b - 321) / 4))]; bool up = u % 2 == 0; double sk = (u++ / 2 % 4) * 3.5; at(b - wn, () => add(QSFan(cx, up ? H / 2 + 60 : H / 2 - 60, 11, (up ? -90 : 90) + sk, 140, b - wn, b, 460, 30))); }
		}
		at(342, () => add(QSFx(342, 0.5, 10, 1.6)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
