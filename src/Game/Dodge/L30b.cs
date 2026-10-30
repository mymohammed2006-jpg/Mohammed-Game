using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// C'est la vie (main level 30), part 2: the night dance.
public static partial class G
{
	// ---------- 5. the waves under the moon (1:10.3) ----------
	// The sea rises over [x0, x1] up to yT in up beats from bf, stays hold beats, goes down in down beats. Its top line shows dashed
	// (white, with arrows) from bf - tl.
	// slope: the top line tilts (px per px from the middle): the tide.
	public static Hz CVWave(double x0, double x1, double yT, double bf, double tl, double up = 1, double hold = 1, double down = 1, double slope = 0)
	{
		var h = new Hz { kind = "CVWave" }; double tf = B(bf), y0 = H + 30;
		double e(double bt) => bt < bf ? 0 : bt < bf + up ? CVOut((bt - bf) / up) : bt < bf + up + hold ? 1 : 1 - CVIn((bt - bf - up - hold) / down);
		double yt(double x) => yT + slope * (x - 640);
		double top(double t, double x, double k) => yt(x) + (1 - k) * (y0 - yt(x)) + 9 * JS.sin(x / 55 + t * 4) * k;
		h.update = (t, dt) => { if (BT(t) > bf + up + hold + down + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + up)
			{
				double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf + up * 0.5, bf + up));
				CVDash(x0, yt(x0) - 9, x1, yt(x1) - 9, CVW(0.25 + 0.55 * q), 3);
				g.strokeStyle = CVW(0.2 + 0.5 * q); g.lineWidth = 3; g.beginPath();
				for (double x = x0 + 60; x < x1 - 30; x += 120) { double y = yt(x); g.moveTo(x - 10, y + 30); g.lineTo(x, y + 18); g.lineTo(x + 10, y + 30); }
				g.stroke();
			}
			double k = e(bt);
			if (k > 0)
			{
				g.beginPath(); g.moveTo(x0, H + 10);
				for (double x = x0; x <= x1 + 0.1; x += 16) g.lineTo(x, top(t, Math.Min(x, x1), k));
				g.lineTo(x1, H + 10); g.closePath();
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = CVP(0.8); g.fill(); g.shadowBlur = 0;
				g.strokeStyle = PINK; g.lineWidth = 5; g.beginPath();
				for (double x = x0; x <= x1 + 0.1; x += 16) { double y = top(t, Math.Min(x, x1), k); if (x == x0) g.moveTo(x, y); else g.lineTo(x, y); }
				g.stroke();
				g.strokeStyle = CVL(0.6); g.lineWidth = 2; g.beginPath();
				for (double x = x0 + 20; x < x1 - 20; x += 70) { double y = top(t, x, k) + 26; g.moveTo(x, y); g.quadraticCurveTo(x + 14, y - 8, x + 28, y); }
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double k = e(BT(t)); if (k <= 0 || p.x < x0 - p.r + 2 || p.x > x1 + p.r - 2) return false;
			return p.y + p.r - 2 > top(t, clamp(p.x, x0, x1), k);
		};
		return h;
	}
	// A shooting star: from (x0, y0) along ang degrees at sp px/s from bf. Its way shows dashed from bf - tl.
	public static Hz CVShoot(double x0, double y0, double ang, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "CVShoot" }; double tf = B(bf), c = JS.cos(CVRad(ang)), s = JS.sin(CVRad(ang)); const double R = 12;
		double len = 1700;
		h.update = (t, dt) => { if (t > tf && sp * (t - tf) > len) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.5) { double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 0.5)); CVDash(x0, y0, x0 + c * len, y0 + s * len, CVW(0.15 + 0.45 * q), 2); }
			if (bt >= bf)
			{
				double d = sp * (t - tf), x = x0 + c * d, y = y0 + s * d;
				var tg = g.createLinearGradient(x, y, x - c * 150, y - s * 150); tg.addColorStop(0, CVW(0.55)); tg.addColorStop(1, CVW(0));
				g.strokeStyle = tg; g.lineWidth = 6; g.lineCap = "round"; g.beginPath(); g.moveTo(x - c * 14, y - s * 14); g.lineTo(x - c * 150, y - s * 150); g.stroke();
				g.translate(x, y); g.rotate(t * 5); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath();
				for (int i = 0; i < 10; i++) { double q = Math.PI * i / 5 - Math.PI / 2, r = i % 2 == 0 ? R * 1.35 : R * 0.6; if (i == 0) g.moveTo(JS.cos(q) * r, JS.sin(q) * r); else g.lineTo(JS.cos(q) * r, JS.sin(q) * r); }
				g.closePath(); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < tf) return false; double d = sp * (t - tf); return hypot(p.x - x0 - c * d, p.y - y0 - s * d) < R * 0.85 + p.r - 2; };
		return h;
	}

	// ---------- 6. the zellige dance floor (1:23.2) ----------
	// 8 x 5 eight-pointed star tiles (two squares, one turned 45 degrees). The floor itself is harmless (gold lines).
	public static double CVTx(int i) => 80 + 160 * i;
	public static double CVTy(int j) => 72 + 144 * j;
	const double CV_TS = 50;   // half the side of each of the two squares
	static void CVStarPath(double x, double y, double s)
	{
		g.beginPath();
		for (int i = 0; i < 16; i++)
		{
			// the 8 points and the 8 inner corners of the star
			double q = Math.PI * i / 8, r = i % 2 == 0 ? s * Math.Sqrt(2) : s / JS.cos(Math.PI / 8);
			double px = x + JS.cos(q) * r, py = y + JS.sin(q) * r; if (i == 0) g.moveTo(px, py); else g.lineTo(px, py);
		}
		g.closePath();
	}
	public static Hz CVFloorBg(double b0, double b1)
	{
		var h = new Hz { kind = "CVFloorBg", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = CVk(bt, b0 - 1, b0) * (1 - CVk(bt, b1, b1 + 1));
			g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(10,12,34,0.55)"; g.fillRect(0, 0, W, H);
			for (int i = 0; i < 8; i++) for (int j = 0; j < 5; j++)
			{
				double x = CVTx(i), y = CVTy(j); CVStarPath(x, y, CV_TS);
				g.fillStyle = (i + j) % 2 == 0 ? "rgba(34,52,110,0.55)" : "rgba(60,40,110,0.5)"; g.fill();
				g.strokeStyle = CVG(0.35 + 0.2 * pulse); g.lineWidth = 2; g.stroke();
			}
			// little gold crosses where four tiles meet
			g.fillStyle = CVG(0.5);
			for (int i = 0; i < 9; i++) for (int j = 0; j < 6; j++) { double x = 160 * i, y = 144 * j; g.fillRect(x - 6, y - 1.5, 12, 3); g.fillRect(x - 1.5, y - 6, 3, 12); }
			g.restore();
		};
		return h;
	}
	// The tiles in mask (bit i * 5 + j) light white from bf - tl, are pink (hurt) from bf for dur beats, then fade.
	public static Hz CVFloor(ulong mask, double bf, double tl, double dur)
	{
		var h = new Hz { kind = "CVFloor" }; double tf = B(bf), te = B(bf + dur);
		var cs = new List<(double x, double y)>(); for (int i = 0; i < 8; i++) for (int j = 0; j < 5; j++) if ((mask >> (i * 5 + j) & 1) != 0) cs.Add((CVTx(i), CVTy(j)));
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (t < tf)
			{
				double q = CVk(bt, bf - tl, bf);
				foreach (var c in cs) { CVStarPath(c.x, c.y, CV_TS * (0.55 + 0.45 * q)); g.fillStyle = CVW(0.06 + 0.16 * q); g.fill(); CVStarPath(c.x, c.y, CV_TS); g.strokeStyle = CVW(0.3 + 0.6 * q); g.lineWidth = 3; g.stroke(); }
			}
			else if (t < te)
			{
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK;
				foreach (var c in cs) { CVStarPath(c.x, c.y, CV_TS); g.fill(); }
				g.shadowBlur = 0; g.strokeStyle = CVL(0.8); g.lineWidth = 2;
				foreach (var c in cs) { CVStarPath(c.x, c.y, CV_TS * 0.55); g.stroke(); }
			}
			else { double q = 1 - (t - te) / 0.25; if (q > 0) { g.globalAlpha = q; g.fillStyle = CVW(0.5); foreach (var c in cs) { CVStarPath(c.x, c.y, CV_TS * q); g.fill(); } } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || t >= te) return false;
			foreach (var c in cs) if (obbHit(p.x, p.y, p.r - 2, c.x, c.y, CV_TS, CV_TS, 0) || obbHit(p.x, p.y, p.r - 2, c.x, c.y, CV_TS, CV_TS, Math.PI / 4)) return true;
			return false;
		};
		return h;
	}
	// the patterns of the floor, by name: the bit of tile (i, j) is i * 5 + j
	public static ulong CVPat(Func<int, int, bool> f) { ulong m = 0; for (int i = 0; i < 8; i++) for (int j = 0; j < 5; j++) if (f(i, j)) m |= 1UL << (i * 5 + j); return m; }

	// ---------- 7. the two dancers (1:39.8) ----------
	// A dancer turns round the middle (its way shows as white dots a few beats ahead), spinning a long ribbon. From b0 to b0 + 1 it shows
	// only as an outline (harmless), then hurts until b1, then fades.
	public struct CVD { public double x, y, a; }
	public static CVD CVDancerAt(double ph0, double b0, double orbW, double spinW, double Rm, double Ra, double bt)
	{
		double u = bt - b0, ph = CVRad(ph0 + orbW * u), R = Rm + Ra * JS.cos(TAU * u / 16);
		return new CVD { x = 640 + R * JS.cos(ph), y = 360 + 0.78 * R * JS.sin(ph), a = CVRad(2 * ph0 + spinW * u) };
	}
	public static Hz CVDancer(double ph0, double b0, double b1, double orbW, double spinW, double L, double Rm, double Ra)
	{
		var h = new Hz { kind = "CVDancer" }; const double RW = 8;
		h.update = (t, dt) => { if (BT(t) > b1 + 1.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double on = CVk(bt, b0, b0 + 1), a = 1 - CVk(bt, b1, b1 + 1);
			var d = CVDancerAt(ph0, b0, orbW, spinW, Rm, Ra, bt);
			g.save(); g.globalAlpha = a;
			// the way ahead
			g.fillStyle = CVW(0.35);
			for (int k = 1; k <= 10; k++) { var f = CVDancerAt(ph0, b0, orbW, spinW, Rm, Ra, bt + 0.4 * k); if (bt + 0.4 * k < b1) circle(f.x, f.y, 2.5); }
			g.translate(d.x, d.y); g.rotate(d.a);
			if (on < 1 && bt < b0 + 1)
			{
				g.strokeStyle = CVW(0.3 + 0.6 * on); g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); g.strokeRect(-L, -RW, 2 * L, 2 * RW); g.setLineDash(new double[0]);
				g.beginPath(); g.arc(0, 0, 20, 0, TAU); g.stroke();
			}
			else
			{
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(-L, -RW, 2 * L, 2 * RW); circle(0, 0, 20); g.shadowBlur = 0;
				g.strokeStyle = CVL(0.8); g.lineWidth = 2; g.beginPath();
				for (double x = -L + 10; x < L - 10; x += 22) { if (Math.Abs(x) < 24) continue; g.moveTo(x, -RW + 2); g.lineTo(x + 10, RW - 2); }
				g.stroke();
				g.fillStyle = CVL(0.9); circle(0, 0, 8);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 + 1 || bt >= b1) return false; var d = CVDancerAt(ph0, b0, orbW, spinW, Rm, Ra, bt);
			return obbHit(p.x, p.y, p.r - 2, d.x, d.y, L, RW, d.a) || hypot(p.x - d.x, p.y - d.y) < 20 + p.r - 2;
		};
		return h;
	}

	// ---------- 8. the confetti cannons (1:52.7) ----------
	// The two cannons in the bottom corners (harmless, gold): each turns to where its next shot goes.
	public static Hz CVCannons(double b0, double b1, Func<double, int, double> aim)
	{
		var h = new Hz { kind = "CVCannons", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = CVk(bt, b0 - 1, b0) * (1 - CVk(bt, b1, b1 + 1));
			g.save(); g.globalAlpha = a;
			for (int sd = 0; sd < 2; sd++)
			{
				double cx = sd == 0 ? 60 : W - 60, cy = H - 30, q = aim(bt, sd);
				g.save(); g.translate(cx, cy); g.rotate(q);
				g.fillStyle = CVG(1); g.beginPath(); g.roundRect(-10, -17, 86, 34, 8); g.fill();
				g.fillStyle = "rgba(90,50,140,1)"; g.fillRect(14, -17, 10, 34); g.fillRect(44, -17, 10, 34);
				g.restore();
				g.fillStyle = "rgba(120,62,30,1)"; circle(cx, cy + 6, 26); g.fillStyle = CVG(0.9); circle(cx, cy + 6, 9);
			}
			g.restore();
		};
		return h;
	}
	// A piece of confetti shot from (x0, y0) at (vx, vy) px/s at bf, falling (gravity gr). Its arc shows dotted (white) from bf - tl.
	public static Hz CVConfetti(double x0, double y0, double vx, double vy, double bf, double tl, double gr = 640)
	{
		var h = new Hz { kind = "CVConfetti" }; double tf = B(bf); const double HW = 10, HH = 6;
		(double x, double y) at(double u) => (x0 + vx * u, y0 + vy * u + 0.5 * gr * u * u);
		h.update = (t, dt) => { if (t > tf + 0.3 && at(t - tf).y > H + 40) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.5)
			{
				double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 0.5)); g.fillStyle = CVW(0.2 + 0.55 * q);
				for (int i = 2; i < 34; i++) { var c = at(i * 0.07); if (c.y > H + 10 || c.x < -10 || c.x > W + 10) break; g.fillRect(c.x - 2, c.y - 2, 4, 4); }
			}
			if (t >= tf)
			{
				double u = t - tf; var c = at(u); g.translate(c.x, c.y); g.rotate(u * 7 + x0);
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; g.fillRect(-HW, -HH, 2 * HW, 2 * HH); g.shadowBlur = 0;
				g.fillStyle = CVL(0.8); g.fillRect(-HW + 3, -1.5, 2 * HW - 6, 3);
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < tf) return false; double u = t - tf; var c = at(u); return obbHit(p.x, p.y, p.r - 2, c.x, c.y, HW, HH, u * 7 + x0); };
		return h;
	}
	// A streamer falls from the top in column x at sp px/s from bf; its column shows dashed from bf - tl.
	public static Hz CVStreamer(double x, double bf, double tl, double sp, double len = 170)
	{
		var h = new Hz { kind = "CVStreamer" }; double tf = B(bf); const double HW = 9;
		double y0(double t) => -len - 10 + sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && y0(t) > H + 20) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.6) { double q = CVk(bt, bf - tl, bf) * (1 - CVk(bt, bf, bf + 0.6)); CVDash(x, 0, x, H, CVW(0.12 + 0.4 * q), 2); }
			if (bt >= bf)
			{
				double y = y0(t); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.fillRect(x - HW, y, 2 * HW, len); g.shadowBlur = 0;
				g.strokeStyle = CVL(0.75); g.lineWidth = 3; g.beginPath(); for (double yy = y + 8; yy < y + len - 10; yy += 18) { g.moveTo(x - HW + 2, yy); g.lineTo(x + HW - 2, yy + 9); } g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < tf) return false; double y = y0(t); return Math.Abs(p.x - x) < HW + p.r - 2 && p.y > y - p.r + 2 && p.y < y + len + p.r - 2; };
		return h;
	}
}
