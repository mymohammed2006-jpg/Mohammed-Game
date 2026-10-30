using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// المفروض (Aseel Hameem), main level 27: "what should have been". A sad song about a love that was supposed to go one way and went another.
// Part 1 "المفروض" (how it should be): everything in order and in pairs: the tears, the clock that ticks on time, the mirror, the calendar of
// the days, the doors. Part 2 "اللي صار" (what happened): the heart breaks in two, the two roads part and cross, the clock runs backwards and
// its numbers fall off. Part 3 "المفروض والواقع" (the dream and the real): the dream shows first in white and the real one follows in pink,
// the thread that ties the two snaps, the two halves of the heart go round and never meet, the tears stop in the air, the clock stops.
// The level is the song from 0:00 to 3:30.7 (songs/SONG_L27.mp3). Grid: 80 BPM, beat 0 = 0.68 s. The drums hit on each beat and 0.75 of a
// beat after beats 0 and 2 of a bar (the Khaleeji "tak"). Every hazard is a pure function of time once it exists. Only pink hurts.
public static partial class G
{
	static double MFk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string MFP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string MFW(double a) => $"rgba(214,226,255,{str(clamp(a, 0, 1))})";
	static double MFEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double MFOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double MFRad(double d) => d * Math.PI / 180;
	static double MFSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void MFDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// where a ray from (x,y) along (dx,dy) leaves the screen (plus m px)
	static double MFExit(double x, double y, double dx, double dy, double m = 40)
	{
		double best = 1e9;
		if (dx > 1e-6) best = Math.Min(best, (W + m - x) / dx); if (dx < -1e-6) best = Math.Min(best, (-m - x) / dx);
		if (dy > 1e-6) best = Math.Min(best, (H + m - y) / dy); if (dy < -1e-6) best = Math.Min(best, (-m - y) / dy);
		return Math.Max(0, best);
	}
	static void MFGlow(double blur = 12) { g.shadowColor = PINK; g.shadowBlur = blur; g.fillStyle = PINK; g.strokeStyle = PINK; }

	// ---------- a tear: a round head (radius r) going along a, a sharp tail behind it ----------
	static void MFTearPath(double x, double y, double r, double a)
	{
		double c = JS.cos(a), s = JS.sin(a);
		g.beginPath(); g.arc(x, y, r, a - Math.PI / 2, a + Math.PI / 2); g.lineTo(x - c * 2.3 * r, y - s * 2.3 * r); g.closePath();
	}
	static void MFTear(double x, double y, double r, double a) { MFTearPath(x, y, r, a); g.fill(); }
	static bool MFTearHit(Player p, double x, double y, double r, double a)
	{
		if (hypot(p.x - x, p.y - y) < r + p.r - 2) return true;
		double c = JS.cos(a), s = JS.sin(a);
		return MFSeg(p.x, p.y, x, y, x - c * 1.6 * r, y - s * 1.6 * r) < 0.42 * r + p.r - 2;
	}

	// ---------- the stage (harmless, under everything) ----------
	// night blue; part 1 a faint drawing grid (the plan), part 2 the grid tilts and the rain gets heavy, part 3 the top half is the dream (lighter)
	public static Hz MFStage(double t1)
	{
		var h = new Hz { kind = "MFStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p2 = MFk(bt, 83, 85) * (1 - MFk(bt, 171, 173)), p3 = MFk(bt, 171, 173);
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(12 + 4 * p2 + 10 * p3)},{str(18 + 2 * p2 + 8 * p3)},{str(42 + 6 * p2 + 10 * p3)},1)");
			gr.addColorStop(1, $"rgba({str(3 + 2 * p3)},{str(5 + 2 * p3)},{str(16 + 4 * p2)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the grid of the plan (part 1 straight, part 2 tilted and broken, part 3 straight again only in the dream half)
			{
				double ga = 0.05 * (1 - p3) + 0.035 * p3, tilt = 7 * p2 * JS.sin(TAU * bt / 32);
				g.save(); g.translate(W / 2, H / 2); g.rotate(MFRad(tilt)); g.strokeStyle = MFW(ga); g.lineWidth = 1; g.beginPath();
				for (double x = -800; x <= 800; x += 80) { if (p2 > 0.5 && ((int)(x / 80) & 3) == 1) { g.moveTo(x, -500); g.lineTo(x, -40); continue; } g.moveTo(x, -500); g.lineTo(x, p3 > 0.5 ? 0 : 500); }
				for (double y = -480; y <= 480; y += 80) { if (p3 > 0.5 && y > 0) continue; g.moveTo(-820, y); g.lineTo(820, y); }
				g.stroke(); g.restore();
			}
			// part 3: the dream half (top) is a little lighter, the line between them sways
			if (p3 > 0)
			{
				double sy = H / 2 + 14 * JS.sin(TAU * bt / 16);
				var dg = g.createLinearGradient(0, 0, 0, sy); dg.addColorStop(0, $"rgba(120,140,200,{str(0.10 * p3)})"); dg.addColorStop(1, $"rgba(120,140,200,{str(0.02 * p3)})");
				g.fillStyle = dg; g.fillRect(0, 0, W, sy);
				g.strokeStyle = MFW(0.12 * p3); g.lineWidth = 2; g.beginPath(); g.moveTo(0, sy); g.lineTo(W, sy); g.stroke();
			}
			// the rain: thin pale streaks (looks only), heavier in part 2
			{
				int n = 26 + (int)(34 * p2) + (int)(16 * p3);
				g.strokeStyle = MFW(0.07 + 0.04 * p2); g.lineWidth = 1; g.beginPath();
				for (int i = 0; i < n; i++)
				{
					double sp = 520 + (i * 53) % 260, x = (i * 197.3 + 40) % (W + 200) - 100, y = (i * 131.7 + t * sp) % (H + 120) - 60;
					g.moveTo(x, y); g.lineTo(x - 7, y + 26);
				}
				g.stroke();
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}
	public static Hz MFFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "MFFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- 1. the tears (0:00) ----------
	// A tear falls down the column x from the top at sp px/s from beat bf; its column shows dashed from bf - tl.
	public static Hz MFFall(double x, double bf, double tl, double sp, double r)
	{
		var h = new Hz { kind = "MFFall" }; double tf = B(bf);
		double y(double t) => -40 + sp * (t - tf);
		h.update = (t, dt) => { if (t > tf && y(t) > H + 60) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.3) { double q = MFk(bt, bf - tl, bf) * (1 - MFk(bt, bf, bf + 0.3)); MFDash(x, 0, x, H, MFW(0.15 + 0.45 * q), 2); g.fillStyle = MFW(0.5 * q); g.beginPath(); g.arc(x, 6, 9, 0, TAU); g.fill(); }
			if (bt >= bf) { MFGlow(12); MFTear(x, y(t), r, Math.PI / 2); }
			g.restore();
		};
		h.hit = (p, t) => BT(t) >= bf && MFTearHit(p, x, y(t), r, Math.PI / 2);
		return h;
	}

	// ---------- 2. the clock (0:12.7), 8. the clock backwards (1:45.7), 13. the clock stops (3:15.7) ----------
	// The face (white, harmless) round the middle; the hands are pink. On each beat from b0 each hand jumps on by its step for that beat
	// (stM / stH, degrees, the last one repeats) in 0.15 beat, like a clock's tick. two: the minute hand reaches both ways (a whole line).
	// The hands show dashed from b0 - 1; they hurt from b0 to b1. cracked: the face is cracked (part 2).
	const double MFCX = 640, MFCY = 360;
	static double MFTickAng(double bt, double b0, double a0, double[] st)
	{
		if (bt <= b0) return a0; double u = bt - b0; int n = (int)Math.Floor(u); double f = u - n, a = a0;
		for (int k = 0; k < n; k++) a += st[Math.Min(k, st.Length - 1)];
		return a + st[Math.Min(n, st.Length - 1)] * MFOut(f / 0.15);
	}
	public static Hz MFClock(double b0, double b1, double aM, double aH, double[] stM, double[] stH, double lM, double lH, bool two, bool cracked, double faceIn, double faceOut)
	{
		var h = new Hz { kind = "MFClock" }; const double RF = 335, WM = 7, WH = 11, HUB = 26;
		h.update = (t, dt) => { if (BT(t) > faceOut + 1) h.dead = true; };
		void hands(double bt, Action<double, double, double, double, double> f)
		{
			double am = MFRad(MFTickAng(bt, b0, aM, stM) - 90), ah = MFRad(MFTickAng(bt, b0, aH, stH) - 90);
			f(two ? MFCX - JS.cos(am) * lM : MFCX, two ? MFCY - JS.sin(am) * lM : MFCY, MFCX + JS.cos(am) * lM, MFCY + JS.sin(am) * lM, WM);
			if (lH > 0) f(MFCX, MFCY, MFCX + JS.cos(ah) * lH, MFCY + JS.sin(ah) * lH, WH);
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < faceIn) return; double fa = MFk(bt, faceIn, faceIn + 1) * (1 - MFk(bt, faceOut, faceOut + 1));
			g.save(); g.lineCap = "round";
			// the face
			g.strokeStyle = MFW(0.28 * fa); g.lineWidth = 3; g.beginPath(); g.arc(MFCX, MFCY, RF, 0, TAU); g.stroke();
			g.strokeStyle = MFW(0.1 * fa); g.lineWidth = 1; g.beginPath(); g.arc(MFCX, MFCY, RF - 18, 0, TAU); g.stroke();
			for (int i = 0; i < 12; i++)
			{
				double q = MFRad(i * 30), r0 = i % 3 == 0 ? RF - 36 : RF - 26;
				g.strokeStyle = MFW((i % 3 == 0 ? 0.5 : 0.3) * fa); g.lineWidth = i % 3 == 0 ? 5 : 3; g.beginPath();
				g.moveTo(MFCX + JS.cos(q) * r0, MFCY + JS.sin(q) * r0); g.lineTo(MFCX + JS.cos(q) * (RF - 8), MFCY + JS.sin(q) * (RF - 8)); g.stroke();
			}
			if (cracked)
			{
				g.strokeStyle = MFW(0.22 * fa); g.lineWidth = 2; g.beginPath();
				g.moveTo(MFCX + 40, MFCY - 30); g.lineTo(MFCX + 120, MFCY - 110); g.lineTo(MFCX + 150, MFCY - 210); g.lineTo(MFCX + 230, MFCY - 250);
				g.moveTo(MFCX - 30, MFCY + 40); g.lineTo(MFCX - 140, MFCY + 90); g.lineTo(MFCX - 190, MFCY + 200); g.moveTo(MFCX - 140, MFCY + 90); g.lineTo(MFCX - 260, MFCY + 70);
				g.stroke();
			}
			// the hands: dashed before b0, pink from b0 to b1
			if (bt < b0)
			{
				double q = MFk(bt, b0 - 1, b0);
				if (q > 0) hands(b0, (x0, y0, x1, y1, w) => MFDash(x0, y0, x1, y1, MFW(0.2 + 0.5 * q), 3));
			}
			else if (bt < b1 + 0.3)
			{
				double al = 1 - MFk(bt, b1, b1 + 0.3);
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = MFP(al);
				hands(Math.Min(bt, b1), (x0, y0, x1, y1, w) => { g.lineWidth = 2 * w; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); });
				g.fillStyle = MFP(al); g.beginPath(); g.arc(MFCX, MFCY, HUB, 0, TAU); g.fill();
				g.shadowBlur = 0; g.fillStyle = $"rgba(20,16,40,{str(al)})"; g.beginPath(); g.arc(MFCX, MFCY, 7, 0, TAU); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			if (hypot(p.x - MFCX, p.y - MFCY) < HUB + p.r - 2) return true;
			bool hit = false; hands(bt, (x0, y0, x1, y1, w) => { if (!hit && MFSeg(p.x, p.y, x0, y0, x1, y1) < w + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}
	// A needle along the clock's mark at angle a (degrees from 12 o'clock): inward (from the screen's edge to the hub) or outward (from the mark
	// off the screen), at sp px/s from bf; its way shows dashed from bf - tl.
	public static Hz MFNeedle(double a, bool inward, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "MFNeedle" }; double tf = B(bf), q = MFRad(a - 90), dx = JS.cos(q), dy = JS.sin(q);
		double r0 = inward ? 30 + MFExit(MFCX, MFCY, dx, dy, 0) : 310, r1 = inward ? 30 : 310 + MFExit(MFCX + dx * 310, MFCY + dy * 310, dx, dy, 60), dir = inward ? -1 : 1;
		double rr(double t) => r0 + dir * sp * Math.Max(0, t - tf);
		bool gone(double t) => inward ? rr(t) < r1 : rr(t) > r1;
		h.update = (t, dt) => { if (t > tf && gone(t)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.25) { double k = MFk(bt, bf - tl, bf) * (1 - MFk(bt, bf, bf + 0.25)); MFDash(MFCX + dx * r0, MFCY + dy * r0, MFCX + dx * r1, MFCY + dy * r1, MFW(0.15 + 0.45 * k), 2); }
			if (bt >= bf && !gone(t))
			{
				double r = rr(t), x = MFCX + dx * r, y = MFCY + dy * r, ex = dx * dir, ey = dy * dir;
				MFGlow(12); g.beginPath(); g.moveTo(x + ex * 20, y + ey * 20); g.lineTo(x - ey * 8, y + ex * 8); g.lineTo(x - ex * 20, y - ey * 20); g.lineTo(x + ey * 8, y - ex * 8); g.closePath(); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || gone(t)) return false; double r = rr(t), x = MFCX + dx * r, y = MFCY + dy * r;
			return MFSeg(p.x, p.y, x - dx * 14, y - dy * 14, x + dx * 14, y + dy * 14) < 5 + p.r - 2;
		};
		return h;
	}

	// ---------- 3. the mirror (0:24.7) ----------
	// A column of blocks (s wide, one a row of 180 px, 4 px apart: no way between them) at x with one gap at row gapRow pops pink at bf for
	// stay beats; it shows as white outlines from bf - tl.
	public static Hz MFCol(double x, int gapRow, double bf, double tl, double s, double stay = 0.5)
	{
		var h = new Hz { kind = "MFCol" }; const double BH = 176;
		h.update = (t, dt) => { if (BT(t) > bf + stay + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			for (int r = 0; r < 4; r++)
			{
				if (r == gapRow) continue; double cy = 90 + 180 * r;
				if (bt < bf) { double q = MFk(bt, bf - tl, bf); g.strokeStyle = MFW(0.2 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); g.strokeRect(x - s / 2, cy - BH / 2, s, BH); g.setLineDash(new double[0]); continue; }
				double k = MFOut((bt - bf) / 0.08), al = 1 - MFk(bt, bf + stay, bf + stay + 0.3), sw = s * k, sh = BH * k;
				if (bt < bf + stay) { MFGlow(14); g.fillRect(x - sw / 2, cy - sh / 2, sw, sh); }
				else { g.strokeStyle = MFW(0.3 * al); g.lineWidth = 2; g.strokeRect(x - s / 2, cy - BH / 2, s, BH); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= bf + stay) return false; double k = MFOut((bt - bf) / 0.08), sw = s * k, sh = BH * k;
			for (int r = 0; r < 4; r++) { if (r == gapRow) continue; double cy = 90 + 180 * r; if (Math.Abs(p.x - x) < sw / 2 + p.r - 2 && Math.Abs(p.y - cy) < sh / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}
	public static Hz MFMirrorLine(double b0, double b1)
	{
		var h = new Hz { kind = "MFMirror" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); double a = MFk(bt, b0, b0 + 1) * (1 - MFk(bt, b1, b1 + 1)); if (a <= 0) return;
			g.save(); g.strokeStyle = MFW(0.35 * a); g.lineWidth = 3; g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke();
			var lg = g.createLinearGradient(W / 2 - 60, 0, W / 2 + 60, 0); lg.addColorStop(0, "rgba(214,226,255,0)"); lg.addColorStop(0.5, $"rgba(214,226,255,{str(0.08 * a)})"); lg.addColorStop(1, "rgba(214,226,255,0)");
			g.fillStyle = lg; g.fillRect(W / 2 - 60, 0, 120, H); g.restore();
		};
		return h;
	}

	// ---------- 4. the calendar (0:36.7) ----------
	// 7 x 4 days over the screen (white, harmless). A day is crossed out with a pink X at bf, growing in 0.12 beat; it hurts for hot beats,
	// then stays as a grey scar. It glows white from bf - tl. A row is struck through with a pink line at bf (dashed from bf - tl) for hot beats.
	const double MFCW = W / 7.0, MFCH = H / 4.0;
	public static Hz MFCalendar(double b0, double b1)
	{
		var h = new Hz { kind = "MFCal", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = MFk(bt, b0, b0 + 1) * (1 - MFk(bt, b1, b1 + 1)); if (a <= 0) return;
			g.save(); g.strokeStyle = MFW(0.18 * a); g.lineWidth = 2; g.beginPath();
			for (int i = 1; i < 7; i++) { g.moveTo(i * MFCW, 0); g.lineTo(i * MFCW, H); }
			for (int j = 1; j < 4; j++) { g.moveTo(0, j * MFCH); g.lineTo(W, j * MFCH); }
			g.stroke();
			g.font = $"500 22px {TFONT}"; g.textAlign = "right"; g.textBaseline = "top"; g.fillStyle = MFW(0.3 * a);
			for (int d = 0; d < 28; d++) { int c = 6 - d % 7, r = d / 7; g.fillText(str(d + 1), c * MFCW + MFCW - 12, r * MFCH + 10); }
			g.restore();
		};
		return h;
	}
	static (double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3) MFDayX(int d)
	{
		int c = 6 - d % 7, r = d / 7; double x0 = c * MFCW + 16, y0 = r * MFCH + 16, x1 = x0 + MFCW - 32, y1 = y0 + MFCH - 32;
		return (x0, y0, x1, y1, x1, y0, x0, y1);
	}
	public static Hz MFCross(int d, double bf, double tl, double hot)
	{
		var h = new Hz { kind = "MFCross" }; var q = MFDayX(d);
		h.update = (t, dt) => { if (BT(t) > bf + hot + 12) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round";
			if (bt < bf) { double k = MFk(bt, bf - tl, bf); g.fillStyle = MFW(0.04 + 0.1 * k); g.fillRect(q.x0 - 10, q.y0 - 10, q.x1 - q.x0 + 20, q.y1 - q.y0 + 20); MFDash(q.x0, q.y0, q.x1, q.y1, MFW(0.2 + 0.4 * k)); MFDash(q.x2, q.y2, q.x3, q.y3, MFW(0.2 + 0.4 * k)); }
			else
			{
				double k = MFOut((bt - bf) / 0.12), k2 = MFOut((bt - bf - 0.06) / 0.12);
				if (bt < bf + hot) { g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; }
				else g.strokeStyle = $"rgba(150,150,170,{str(0.16 * (1 - MFk(bt, bf + hot + 8, bf + hot + 12)))})";
				g.lineWidth = bt < bf + hot ? 12 : 4; g.beginPath(); g.moveTo(q.x0, q.y0); g.lineTo(q.x0 + (q.x1 - q.x0) * k, q.y0 + (q.y1 - q.y0) * k);
				if (k2 > 0) { g.moveTo(q.x2, q.y2); g.lineTo(q.x2 + (q.x3 - q.x2) * k2, q.y2 + (q.y3 - q.y2) * k2); }
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= bf + hot) return false; double k = MFOut((bt - bf) / 0.12), k2 = MFOut((bt - bf - 0.06) / 0.12);
			if (MFSeg(p.x, p.y, q.x0, q.y0, q.x0 + (q.x1 - q.x0) * k, q.y0 + (q.y1 - q.y0) * k) < 6 + p.r - 2) return true;
			return k2 > 0 && MFSeg(p.x, p.y, q.x2, q.y2, q.x2 + (q.x3 - q.x2) * k2, q.y2 + (q.y3 - q.y2) * k2) < 6 + p.r - 2;
		};
		return h;
	}
	public static Hz MFStrike(int row, double bf, double tl, double hot)
	{
		var h = new Hz { kind = "MFStrike" }; double y = row * MFCH + MFCH / 2;
		h.update = (t, dt) => { if (BT(t) > bf + hot + 0.5) h.dead = true; };
		double len(double bt) => W * MFOut((bt - bf) / 0.2);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round";
			if (bt < bf) MFDash(W, y, 0, y, MFW(0.2 + 0.5 * MFk(bt, bf - tl, bf)), 3);
			else if (bt < bf + hot) { g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 14; g.beginPath(); g.moveTo(W, y); g.lineTo(W - len(bt), y); g.stroke(); }
			else { g.strokeStyle = MFW(0.25 * (1 - MFk(bt, bf + hot, bf + hot + 0.5))); g.lineWidth = 3; g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); return bt >= bf && bt < bf + hot && p.x > W - len(bt) - 7 && Math.Abs(p.y - y) < 7 + p.r - 2; };
		return h;
	}

	// ---------- 5. the doors (0:51.7) ----------
	// A wall the whole height with one door (gy, gh) comes in from the right (dir -1) or the left (dir 1): on each step beat it jumps st px
	// in 0.2 beat. Its first place shows dashed from bf - tl; it hurts from bf.
	public static Hz MFDoor(int dir, double gy, double gh, double bf, double tl, double[] steps, double st)
	{
		var h = new Hz { kind = "MFDoor" }; const double WW = 26; double x0 = dir < 0 ? W + 10 : -10;
		double xc(double bt) { double d = 0; foreach (var s in steps) { if (bt < s) break; d += st * MFOut((bt - s) / 0.2); } return x0 + dir * d; }
		h.update = (t, dt) => { double x = xc(BT(t)); if (dir < 0 ? x < -60 : x > W + 60) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf) { double q = MFk(bt, bf - tl, bf), xx = x0 + dir * st; MFDash(xx, 0, xx, gy - gh / 2, MFW(0.2 + 0.5 * q), 3); MFDash(xx, gy + gh / 2, xx, H, MFW(0.2 + 0.5 * q), 3); }
			else
			{
				double x = xc(bt); MFGlow(14);
				g.fillRect(x - WW / 2, -10, WW, gy - gh / 2 + 10); g.fillRect(x - WW / 2, gy + gh / 2, WW, H + 10 - gy - gh / 2);
				// the door frame (white, harmless)
				g.shadowBlur = 0; g.strokeStyle = MFW(0.5); g.lineWidth = 2; g.beginPath(); g.moveTo(x - 20, gy - gh / 2 - 2); g.lineTo(x + 20, gy - gh / 2 - 2); g.moveTo(x - 20, gy + gh / 2 + 2); g.lineTo(x + 20, gy + gh / 2 + 2); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf) return false; double x = xc(bt);
			return Math.Abs(p.x - x) < WW / 2 + p.r - 2 && (p.y < gy - gh / 2 + p.r - 2 || p.y > gy + gh / 2 - p.r + 2);
		};
		return h;
	}
}
