using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// C'est la vie (Khaled), main level 30: "the night of the party". An easy level made for fun (Mohammed 10-30: about as hard as level 2,
// full, nice shapes and moves). One night by the sea, from the sunset to the morning:
// part 1 the sunset (the lanterns go up, the darbuka, the clapping, the "la la la" on the music staff),
// part 2 the night dance (the waves under the moon, the zellige dance floor, the two dancers, the confetti cannons),
// part 3 the fireworks (the rockets, the big wheel, the lanterns come down, the whole party, the words "C'EST LA VIE", the sunrise).
// The level is the song from 0:07.000 (songs/SONG_L30.mp3 starts there) to 3:07. Grid: 130 BPM (a steady drum machine),
// beat 0 = 0.4905 s of the file (0:07.4905 of the song) = a downbeat.
// Every hazard is a pure function of time once it exists. Only pink hurts: gold, white, the sea and the sky never do.
public static partial class G
{
	static double CVk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string CVP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string CVL(double a) => $"rgba(255,176,206,{str(clamp(a, 0, 1))})";   // light pink: the details drawn on a pink thing
	static string CVW(double a) => $"rgba(242,238,230,{str(clamp(a, 0, 1))})";
	static string CVG(double a) => $"rgba(240,190,80,{str(clamp(a, 0, 1))})";
	static double CVEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double CVOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double CVIn(double k) { k = clamp(k, 0, 1); return k * k; }
	static double CVRad(double d) => d * Math.PI / 180;
	static double CVH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }   // a fixed number 0-1 from s
	static void CVDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double CVSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double CVMix(double a, double b, double k) => a + (b - a) * k;
	public static Hz CVFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "CVFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the stage (harmless, under everything): the sky, the sea, the town; sunset -> night -> fireworks -> sunrise ----------
	const double CV_HOR = 470;   // the horizon
	static readonly double[] CV_SKY = {
		46, 22, 78, 170, 88, 40,     // the sunset (top, horizon)
		8, 12, 34, 22, 36, 78,       // the night
		20, 10, 44, 46, 26, 84,      // the fireworks night
		52, 50, 104, 206, 134, 62 }; // the morning
	public static Hz CVStage(double t1)
	{
		var h = new Hz { kind = "CVStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6);
			double kN = CVk(bt, 132, 138), kF = CVk(bt, 252, 258), kM = CVk(bt, 376, 389);
			double C(int i) { double v = CV_SKY[i]; v = CVMix(v, CV_SKY[6 + i], kN); v = CVMix(v, CV_SKY[12 + i], kF); v = CVMix(v, CV_SKY[18 + i], kM); return v; }
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, CV_HOR);
			gr.addColorStop(0, $"rgba({str(C(0))},{str(C(1))},{str(C(2))},1)"); gr.addColorStop(1, $"rgba({str(C(3))},{str(C(4))},{str(C(5))},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, CV_HOR + 1);
			// the stars (night and fireworks), a slow twinkle
			double ks = Math.Max(kN, 0) * (1 - kM);
			if (ks > 0.01)
			{
				g.fillStyle = CVW(0.7 * ks);
				for (int i = 0; i < 46; i++) { double sx = CVH(i * 3.1) * W, sy = CVH(i * 7.7) * (CV_HOR - 60), tw = 0.5 + 0.5 * JS.sin(t * (1.3 + CVH(i) * 2) + i); g.globalAlpha = a * ks * (0.25 + 0.6 * tw); g.fillRect(sx, sy, 2, 2); }
				g.globalAlpha = a;
			}
			// the sun goes down (part 1) and comes up again (the end)
			double sunA = (1 - kN) + kM, sunY = bt < 200 ? 420 + 80 * CVk(bt, 0, 138) : 560 - 150 * CVOut(CVk(bt, 376, 389));
			if (sunA > 0.01)
			{
				g.save(); g.beginPath(); g.rect(0, 0, W, CV_HOR); g.clip();
				var sg = g.createRadialGradient(640, sunY, 20, 640, sunY, 260); sg.addColorStop(0, $"rgba(255,206,120,{str(0.5 * sunA)})"); sg.addColorStop(1, "rgba(255,150,60,0)");
				g.fillStyle = sg; g.fillRect(380, sunY - 260, 520, 520);
				g.fillStyle = $"rgba(250,178,84,{str(0.8 * sunA)})"; circle(640, sunY, 104);
				g.fillStyle = $"rgba(232,120,46,{str(0.5 * sunA)})"; for (int i = 0; i < 4; i++) g.fillRect(520, sunY + 18 + 20 * i, 240, 5 + i);
				g.restore();
			}
			// the moon (night)
			double mA = kN * (1 - kM);
			if (mA > 0.01) { g.fillStyle = $"rgba(240,232,204,{str(0.85 * mA)})"; circle(1050, 112, 44); g.fillStyle = $"rgba(206,196,170,{str(0.5 * mA)})"; circle(1036, 104, 9); circle(1062, 126, 6); }
			// the sea: dark water with light on it (the sun's road, then the moon's)
			var sea = g.createLinearGradient(0, CV_HOR, 0, H);
			sea.addColorStop(0, $"rgba({str(CVMix(26, 12, kN))},{str(CVMix(34, 26, kN))},{str(CVMix(70, 60, kN))},1)"); sea.addColorStop(1, "rgba(6,10,26,1)");
			g.fillStyle = sea; g.fillRect(0, CV_HOR, W, H - CV_HOR);
			for (int i = 0; i < 26; i++)
			{
				double yy = CV_HOR + 8 + (i * i) * 0.37, ww = 30 + i * 9, cx = 640 + 50 * JS.sin(t * 0.8 + i * 1.7);
				double lx = kN > 0.5 ? 1050 + 30 * JS.sin(t * 0.7 + i) : cx;
				g.fillStyle = kN > 0.5 ? CVW(0.12 * mA) : $"rgba(250,190,100,{str(0.22 * sunA)})"; g.fillRect(lx - ww / 2, yy, ww, 2);
			}
			// the town by the sea: houses, domes, a minaret, palms (dark, looks only)
			g.fillStyle = "rgba(12,10,26,1)"; g.beginPath(); g.moveTo(0, CV_HOR);
			double[] TOWN = { 0, 430, 60, 430, 60, 410, 120, 410, 120, 438, 170, 438, 170, 418, 236, 418, 236, 446, 1040, 446, 1040, 420, 1100, 420, 1100, 404, 1160, 404, 1160, 432, 1220, 432, 1220, 414, 1280, 414 };
			for (int i = 0; i < TOWN.Length; i += 2) g.lineTo(TOWN[i], TOWN[i + 1]);
			g.lineTo(W, CV_HOR); g.closePath(); g.fill();
			circle(203, 418, 26); circle(1130, 404, 24);
			g.fillRect(268, 336, 18, 112); g.beginPath(); g.moveTo(262, 338); g.lineTo(277, 310); g.lineTo(292, 338); g.fill();
			g.fillRect(990, 360, 14, 88); circle(997, 360, 10);
			void palm(double x, double s)
			{
				g.strokeStyle = "rgba(12,10,26,1)"; g.lineWidth = 9 * s; g.lineCap = "round";
				g.beginPath(); g.moveTo(x, CV_HOR); g.quadraticCurveTo(x + 14 * s, CV_HOR - 90 * s, x + 4 * s, CV_HOR - 170 * s); g.stroke();
				g.lineWidth = 7 * s;
				for (int k = 0; k < 6; k++) { double q = CVRad(-160 + 28 * k), sw = 4 * JS.sin(t * 1.2 + k); g.beginPath(); g.moveTo(x + 4 * s, CV_HOR - 170 * s); g.quadraticCurveTo(x + 4 * s + JS.cos(q) * 50 * s, CV_HOR - 170 * s + JS.sin(q) * 50 * s - 16 * s, x + 4 * s + JS.cos(q) * 92 * s, CV_HOR - 170 * s + JS.sin(q) * 92 * s + 26 * s + sw); g.stroke(); }
			}
			palm(40, 1.1); palm(1236, 0.95); palm(1180, 0.7);
			// the windows light up at night (gold)
			if (kN > 0.01) { g.fillStyle = CVG(0.5 * kN); for (int i = 0; i < 9; i++) { double wx = i < 4 ? 70 + 42 * i : 1050 + 46 * (i - 4); g.fillRect(wx, i < 4 ? 424 : 426, 7, 8); } }
			// the beat: a soft gold line on the horizon
			g.fillStyle = CVG(0.12 + 0.25 * pulse); g.fillRect(0, CV_HOR - 1, W, 2);
			g.restore();
		};
		return h;
	}

	// ---------- 1. the lanterns (0:07.5) ----------
	// A paper lantern goes up from the bottom (dir -1) or comes down from the top (dir 1) in column x at sp px/s from bf.
	// Its column shows dashed (white) from bf - tl.
	public static Hz CVLantern(double x, int dir, double bf, double tl, double sp, double s = 1)
	{
		var h = new Hz { kind = "CVLantern" }; double tf = B(bf), y0 = dir < 0 ? H + 40 : -40, hw = 15 * s, hh = 21 * s;
		double yy(double t) => y0 + dir * sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && (dir < 0 ? yy(t) < -70 : yy(t) > H + 70)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.6)
			{
				double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 0.6));
				CVDash(x, dir < 0 ? H : 0, x, dir < 0 ? 0 : H, CVW(0.12 + 0.4 * q), 2);
				g.fillStyle = CVG(0.35 + 0.55 * q); double ye = dir < 0 ? H - 6 : 6, d2 = dir < 0 ? -16 : 16;
				g.beginPath(); g.moveTo(x - 12, ye); g.lineTo(x + 12, ye); g.lineTo(x, ye + d2); g.closePath(); g.fill();
			}
			if (bt >= bf)
			{
				double y = yy(t), sw = 0.08 * JS.sin(t * 3 + x);
				g.translate(x, y); g.rotate(sw);
				g.strokeStyle = CVG(0.85); g.lineWidth = 2; g.beginPath(); g.moveTo(0, -hh - 5 * s); g.lineTo(0, -hh - 18 * s); g.stroke();
				g.fillStyle = CVG(0.95); g.fillRect(-8 * s, -hh - 6 * s, 16 * s, 6 * s); g.fillRect(-8 * s, hh, 16 * s, 6 * s);
				g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.beginPath(); g.roundRect(-hw, -hh, 2 * hw, 2 * hh, 10 * s); g.fill(); g.shadowBlur = 0;
				g.strokeStyle = CVL(0.9); g.lineWidth = 2; g.beginPath();
				g.moveTo(0, -hh + 3); g.lineTo(0, hh - 3); g.moveTo(-hw * 0.5, -hh + 5); g.quadraticCurveTo(-hw * 0.8, 0, -hw * 0.5, hh - 5); g.moveTo(hw * 0.5, -hh + 5); g.quadraticCurveTo(hw * 0.8, 0, hw * 0.5, hh - 5); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) => t >= tf && obbHit(p.x, p.y, p.r - 2, x, yy(t), hw, hh, 0);
		return h;
	}

	// A seagull (a pink V) flies across at height y, from the left (dir 1) or the right (dir -1). Its line shows dashed from bf - tl.
	public static Hz CVGull(double y, int dir, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "CVGull" }; double tf = B(bf);
		double xx(double t) => dir > 0 ? -60 + sp * Math.Max(0, t - tf) : W + 60 - sp * Math.Max(0, t - tf);
		double fl(double t) => 9 * JS.sin((t - tf) * 11);
		h.update = (t, dt) => { if (t > tf && (dir > 0 ? xx(t) > W + 80 : xx(t) < -80)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 1) { double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 1)); CVDash(0, y, W, y, CVW(0.12 + 0.4 * q), 2); }
			if (bt >= bf)
			{
				double x = xx(t), f = fl(t);
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 7; g.lineCap = "round"; g.lineJoin = "round";
				g.beginPath(); g.moveTo(x - 32, y - 12 + f); g.quadraticCurveTo(x - 14, y - 16, x, y); g.quadraticCurveTo(x + 14, y - 16, x + 32, y - 12 + f); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double x = xx(t), f = fl(t);
			return CVSeg(p.x, p.y, x - 32, y - 12 + f, x - 12, y - 10) < 3.5 + p.r - 2 || CVSeg(p.x, p.y, x - 12, y - 10, x, y) < 3.5 + p.r - 2
				|| CVSeg(p.x, p.y, x + 32, y - 12 + f, x + 12, y - 10) < 3.5 + p.r - 2 || CVSeg(p.x, p.y, x + 12, y - 10, x, y) < 3.5 + p.r - 2;
		};
		return h;
	}

	// ---------- 2. the darbuka (0:24.1) ----------
	// The drum seen from above in the middle (harmless): it shakes on the beat.
	public static Hz CVDrum(double cx, double cy, double b0, double b1)
	{
		var h = new Hz { kind = "CVDrum", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = CVk(bt, b0 - 1, b0) * (1 - CVk(bt, b1, b1 + 1.5)), sc = 0.6 + 0.4 * CVOut(CVk(bt, b0 - 1, b0)) + 0.05 * pulse;
			g.save(); g.globalAlpha = a; g.translate(cx, cy); g.scale(sc, sc);
			g.fillStyle = "rgba(120,62,30,1)"; circle(0, 0, 74);
			for (int i = 0; i < 16; i++) { double q = TAU * i / 16 + bt * 0.1; g.fillStyle = i % 2 == 0 ? CVG(1) : "rgba(70,40,120,1)"; g.beginPath(); g.moveTo(JS.cos(q) * 74, JS.sin(q) * 74); g.lineTo(JS.cos(q + TAU / 32) * 62, JS.sin(q + TAU / 32) * 62); g.lineTo(JS.cos(q + TAU / 16) * 74, JS.sin(q + TAU / 16) * 74); g.fill(); }
			var sk = g.createRadialGradient(-12, -12, 4, 0, 0, 60); sk.addColorStop(0, "rgba(246,234,206,1)"); sk.addColorStop(1, "rgba(206,186,150,1)");
			g.fillStyle = sk; circle(0, 0, 58);
			g.strokeStyle = "rgba(140,100,60,0.5)"; g.lineWidth = 2; g.beginPath(); g.arc(0, 0, 30 + 6 * pulse, 0, TAU); g.stroke();
			g.restore();
		};
		return h;
	}
	// The dum: a ring goes out from the drum at sp px/s from bf, open where the gap is (gapA degrees, gapW wide). The gap shows from
	// bf - tl as two gold rays; the ring's start shows dashed (white).
	// With sp < 0 it comes in instead, from 760 (off the screen) to the drum; its start shows dashed at 420.
	public static Hz CVRingOut(double cx, double cy, double bf, double tl, double sp, double gapA, double gapW, double th = 12)
	{
		var h = new Hz { kind = "CVRingOut" }; const double R0 = 78, RI = 760; double tf = B(bf), ga = CVRad(gapA), gw = CVRad(gapW), rs = sp > 0 ? R0 : RI, rd = sp > 0 ? R0 + 4 : 420;
		double rr(double t) => rs + sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && (sp > 0 ? rr(t) > 900 : rr(t) < R0)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 1)
			{
				double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 1));
				g.strokeStyle = CVG(0.25 + 0.6 * q); g.lineWidth = 3; g.beginPath();
				foreach (var e in new[] { ga - gw / 2, ga + gw / 2 }) { g.moveTo(cx + JS.cos(e) * (R0 + 10), cy + JS.sin(e) * (R0 + 10)); g.lineTo(cx + JS.cos(e) * 860, cy + JS.sin(e) * 860); }
				g.stroke();
				if (bt < bf) { g.strokeStyle = CVW(0.2 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(cx, cy, rd, ga + gw / 2, ga - gw / 2 + TAU); g.stroke(); g.setLineDash(new double[0]); }
			}
			if (bt >= bf)
			{
				if (rr(t) < R0) { g.restore(); return; }
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.arc(cx, cy, rr(t), ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
				g.shadowBlur = 0; g.strokeStyle = CVL(0.7); g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, rr(t), ga + gw / 2 + 0.02, ga - gw / 2 + TAU - 0.02); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || rr(t) < R0) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), r = rr(t);
			if (Math.Abs(d - r) >= th / 2 + p.r - 1) return false;
			double da = Math.Abs(((Math.Atan2(dy, dx) - ga) % TAU + 3 * Math.PI) % TAU - Math.PI);
			return da > gw / 2 - (p.r - 1) / Math.Max(d, 1);
		};
		return h;
	}
	// A spray: n things fly out from (cx, cy) at sp px/s from bf, the first at a0 degrees. Their ways show as short white dashes from bf - tl.
	// look 0 a note (the tak), 1 a petal, 2 a spark. They hurt for life seconds, then fade.
	public static Hz CVSpray(double cx, double cy, int n, double a0, double bf, double tl, double sp, double R0, int look, double life = 9, double rr = 0)
	{
		var h = new Hz { kind = "CVSpray" }; double tf = B(bf), r = rr > 0 ? rr : (look == 2 ? 7 : look == 1 ? 8 : 11);
		var dx = new double[n]; var dy = new double[n];
		for (int i = 0; i < n; i++) { double q = CVRad(a0 + 360.0 * i / n); dx[i] = JS.cos(q); dy[i] = JS.sin(q); }
		double dist(double t) => R0 + sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf + life + 0.4 || (t > tf && dist(t) > 1500)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = CVk(bt, bf - tl, bf); g.strokeStyle = CVW(0.2 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 7, 7 }); g.beginPath();
				for (int i = 0; i < n; i++) { g.moveTo(cx + dx[i] * R0, cy + dy[i] * R0); g.lineTo(cx + dx[i] * (R0 + 120), cy + dy[i] * (R0 + 120)); }
				g.stroke(); g.setLineDash(new double[0]); g.restore(); return;
			}
			double d = dist(t), al = t - tf < life ? 1 : 1 - (t - tf - life) / 0.3; if (al <= 0) { g.restore(); return; }
			g.globalAlpha = al; g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; g.strokeStyle = PINK;
			for (int i = 0; i < n; i++)
			{
				double x = cx + dx[i] * d, y = cy + dy[i] * d; if (x < -40 || x > W + 40 || y < -40 || y > H + 40) continue;
				if (look == 0)
				{
					g.beginPath(); g.ellipse(x, y, r * 1.2, r * 0.85, -0.4, 0, TAU); g.fill();
					g.lineWidth = 3; g.beginPath(); g.moveTo(x + r * 1.05, y - 2); g.lineTo(x + r * 1.05, y - r * 2.6); g.quadraticCurveTo(x + r * 1.9, y - r * 2.1, x + r * 2.1, y - r * 1.3); g.stroke();
				}
				else if (look == 1) { g.beginPath(); g.ellipse(x, y, r * 1.25, r * 0.75, Math.Atan2(dy[i], dx[i]) + t * 3, 0, TAU); g.fill(); }
				else { circle(x, y, r); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || t - tf >= life) return false; double d = dist(t);
			for (int i = 0; i < n; i++) if (hypot(p.x - cx - dx[i] * d, p.y - cy - dy[i] * d) < r + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// ---------- 3. the clapping (0:40.7) ----------
	// Two big hands clap on a strip across the screen (vert: a strip down the screen at x = pos). The strip shows from bf - tl (white),
	// the hands slide in during the last half beat and clap at bf, then go back.
	public static Hz CVClap(bool vert, double pos, double w, double bf, double tl)
	{
		var h = new Hz { kind = "CVClap" }; double tf = B(bf), din = 0.5 * SPB, hold = 0.1, dout = 0.45 * SPB, half = (vert ? H : W) / 2; const double PL = 250;
		bool shook = false;
		double e(double t) => t < tf - din ? 0 : t < tf ? CVIn((t - tf + din) / din) : t < tf + hold ? 1 : 1 - CVOut((t - tf - hold) / dout);
		h.update = (t, dt) => { if (!shook && t >= tf) { shook = true; shake = Math.Max(shake, 4); } if (t > tf + hold + dout + 0.3) h.dead = true; };
		void hand(double a1, int sd)
		{
			// a hand along the strip: its finger tips at a1 (along), pointing to the middle (sd 1 = from the low side); drawn across = the strip
			g.save();
			if (vert) { g.translate(pos, a1); g.rotate(sd > 0 ? Math.PI / 2 : -Math.PI / 2); } else { g.translate(a1, pos); if (sd < 0) g.scale(-1, 1); }
			// local: x along (tips at 0, the wrist at -PL), y across (-w/2..w/2)
			g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.roundRect(-PL, -w / 2, PL, w, new[] { 4.0, 26, 26, 4 }); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = CVL(0.85); g.lineWidth = 3; g.lineCap = "round"; g.beginPath();
			for (int i = 1; i < 4; i++) { double yy = -w / 2 + w * i / 4; g.moveTo(-6, yy); g.lineTo(-78, yy); }
			g.moveTo(-170, -w / 2 + 6); g.quadraticCurveTo(-120, -w / 2 + 22, -96, -w / 2 + 8); g.moveTo(-PL + 20, -w / 2 + 10); g.lineTo(-PL + 20, w / 2 - 10);
			g.stroke(); g.restore();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (t < tf + 0.05)
			{
				double q = CVk(bt, bf - tl, bf);
				g.fillStyle = CVW(0.04 + 0.1 * q); if (vert) g.fillRect(pos - w / 2, 0, w, H); else g.fillRect(0, pos - w / 2, W, w);
				g.strokeStyle = CVW(0.25 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				if (vert) { g.moveTo(pos - w / 2, 0); g.lineTo(pos - w / 2, H); g.moveTo(pos + w / 2, 0); g.lineTo(pos + w / 2, H); }
				else { g.moveTo(0, pos - w / 2); g.lineTo(W, pos - w / 2); g.moveTo(0, pos + w / 2); g.lineTo(W, pos + w / 2); }
				g.stroke(); g.setLineDash(new double[0]);
			}
			double k = e(t);
			if (k > 0) { hand(k * half, 1); hand(2 * half - k * half, -1); }
			// the clap: gold sparks at the middle (looks only)
			if (t >= tf && t < tf + 0.35)
			{
				// only out of the strip, so they never cover the hands
				double u = (t - tf) / 0.35; g.strokeStyle = CVG(1 - u); g.lineWidth = 3; g.beginPath();
				double mx = vert ? pos : half, my = vert ? half : pos;
				for (int i = 0; i < 6; i++)
				{
					double q = CVRad(60 + 30 * (i % 3) + 180 * (i / 3)) + (vert ? Math.PI / 2 : 0), sn = Math.Abs(vert ? JS.cos(q) : JS.sin(q)), r1 = (w / 2 + 8 + 50 * u) / sn, r2 = r1 + 26;
					g.moveTo(mx + JS.cos(q) * r1, my + JS.sin(q) * r1); g.lineTo(mx + JS.cos(q) * r2, my + JS.sin(q) * r2);
				}
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double k = e(t); if (k <= 0) return false;
			double al = vert ? p.y : p.x, ac = vert ? p.x : p.y; if (Math.Abs(ac - pos) >= w / 2 + p.r - 2) return false;
			double tip = k * half; if (al > tip - PL - p.r + 2 && al < tip + p.r - 2) return true;
			double tip2 = 2 * half - tip; return al < tip2 + PL + p.r - 2 && al > tip2 - p.r + 2;
		};
		return h;
	}

	// ---------- 4. the music staff: "la la la" (0:55.5) ----------
	// 5 gold lines (harmless) and a treble clef. Notes ride on its lines and spaces: line k (0..8) is y = 200 + 40 k.
	public static double CVStaffY(double k) => 200 + 40 * k;
	public static Hz CVStaff(double b0, double b1)
	{
		var h = new Hz { kind = "CVStaff", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = CVk(bt, b0 - 1, b0) * (1 - CVk(bt, b1, b1 + 1));
			g.save(); g.globalAlpha = a;
			g.strokeStyle = CVG(0.45 + 0.2 * pulse); g.lineWidth = 3; g.beginPath();
			for (int i = 0; i < 5; i++) { double y = CVStaffY(2 * i), x0 = W * (1 - CVOut(CVk(bt, b0 - 1, b0 - 0.2 + 0.1 * i))); g.moveTo(x0, y); g.lineTo(W, y); }
			g.stroke();
			// the treble clef (gold)
			g.strokeStyle = CVG(0.8); g.lineWidth = 6; g.lineCap = "round"; g.beginPath();
			g.moveTo(84, 560); g.quadraticCurveTo(60, 600, 86, 604); g.quadraticCurveTo(110, 600, 100, 500);
			g.lineTo(80, 200); g.quadraticCurveTo(76, 150, 104, 160); g.quadraticCurveTo(122, 200, 70, 280);
			g.quadraticCurveTo(30, 340, 50, 390); g.quadraticCurveTo(80, 440, 130, 410); g.quadraticCurveTo(160, 370, 120, 340); g.quadraticCurveTo(80, 320, 66, 370);
			g.stroke();
			g.restore();
		};
		return h;
	}
	// A note on line k comes from the right at sp px/s from bf. A ghost of it waits at the right edge from bf - tl.
	public static Hz CVSNote(double k, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "CVSNote" }; double tf = B(bf), y = CVStaffY(k); const double RH = 13;
		double xx(double t) => W + 30 - sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && xx(t) < -60) h.dead = true; };
		void shape(double x)
		{
			g.beginPath(); g.ellipse(x, y, RH * 1.25, RH * 0.9, -0.35, 0, TAU); g.fill();
			g.lineWidth = 3; g.beginPath(); if (k >= 4) { g.moveTo(x + RH * 1.1, y - 3); g.lineTo(x + RH * 1.1, y - 52); } else { g.moveTo(x - RH * 1.1, y + 3); g.lineTo(x - RH * 1.1, y + 52); } g.stroke();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = CVk(bt, bf - tl, bf); g.strokeStyle = CVW(0.25 + 0.55 * q); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 });
				g.beginPath(); g.ellipse(W - 26, y, RH * 1.25, RH * 0.9, -0.35, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				CVDash(W - 50, y, W - 240, y, CVW(0.12 + 0.35 * q), 2);
			}
			else { g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.strokeStyle = PINK; shape(xx(t)); }
			g.restore();
		};
		h.hit = (p, t) => t >= tf && hypot(p.x - xx(t), p.y - y) < RH + p.r - 1;
		return h;
	}
	// A bar line of the staff: a pink wall top to bottom with a gap (gapY, gapH), from the right at sp px/s from bf. It shows dashed at
	// the right edge from bf - tl, the gap marked with gold.
	public static Hz CVBarLine(double gapY, double gapH, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "CVBarLine" }; double tf = B(bf), y0 = gapY - gapH / 2, y1 = gapY + gapH / 2;
		double xx(double t) => W + 30 - sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && xx(t) < -60) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = CVk(bt, bf - tl, bf); CVDash(W - 14, 0, W - 14, y0, CVW(0.25 + 0.5 * q), 3); CVDash(W - 14, y1, W - 14, H, CVW(0.25 + 0.5 * q), 3);
				g.strokeStyle = CVG(0.4 + 0.6 * q); g.lineWidth = 4; g.beginPath(); g.moveTo(W - 40, y0); g.lineTo(W, y0); g.moveTo(W - 40, y1); g.lineTo(W, y1); g.stroke();
			}
			else
			{
				double x = xx(t); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
				g.fillRect(x - 7, -10, 14, y0 + 10); g.fillRect(x - 7, y1, 14, H + 10 - y1); g.fillRect(x + 13, -10, 5, y0 + 10); g.fillRect(x + 13, y1, 5, H + 10 - y1);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double x = xx(t); bool inX = Math.Abs(p.x - x) < 7 + p.r - 1 || Math.Abs(p.x - x - 15.5) < 2.5 + p.r - 1;
			return inX && (p.y < y0 + p.r - 2 || p.y > y1 - p.r + 2);
		};
		return h;
	}
}
