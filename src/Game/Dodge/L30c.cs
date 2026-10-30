using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// C'est la vie (main level 30), part 3: the fireworks and the morning.
public static partial class G
{
	// ---------- 9. the fireworks (2:05.6) ----------
	// A rocket goes up in column bx from bf - 1 and bursts at (bx, by) at bf into sparks that fly out at sp px/s and hurt for life
	// seconds. shape 0 a circle of n, 1 a star (5 arms of 3), 2 a heart of n, 3 two rings. Its way shows dashed from bf - 1 - tl, and
	// where each spark goes as short dashes from bf - 0.6.
	static (double x, double y) CVHeart(double q) { double s = JS.sin(q); return (16 * s * s * s / 16, -(13 * JS.cos(q) - 5 * JS.cos(2 * q) - 2 * JS.cos(3 * q) - JS.cos(4 * q)) / 16); }
	public static Hz CVRocket(double bx, double by, double bf, double tl, int shape, int n, double sp, double life)
	{
		var h = new Hz { kind = "CVRocket" }; double tf = B(bf), tr = B(bf - 1);
		var vx = new List<double>(); var vy = new List<double>();
		if (shape == 0) for (int i = 0; i < n; i++) { double q = TAU * i / n - Math.PI / 2; vx.Add(JS.cos(q)); vy.Add(JS.sin(q)); }
		else if (shape == 1) for (int i = 0; i < 5; i++) for (int k = 0; k < 3; k++) { double q = TAU * i / 5 - Math.PI / 2, m = 0.55 + 0.45 * k; vx.Add(JS.cos(q) * m); vy.Add(JS.sin(q) * m); }
		else if (shape == 2) for (int i = 0; i < n; i++) { var c = CVHeart(TAU * i / n); vx.Add(c.x * 1.05); vy.Add(c.y * 1.05 + 0.15); }
		else for (int i = 0; i < n; i++) { double q = TAU * (i / 2) / (n / 2) + (i % 2) * Math.PI / (n / 2), m = i % 2 == 0 ? 0.62 : 1; vx.Add(JS.cos(q) * m); vy.Add(JS.sin(q) * m); }
		int N = vx.Count; const double RS = 7, RR = 8;
		double ry(double t) => H + 30 + (by - H - 30) * CVOut((t - tr) / (tf - tr));
		h.update = (t, dt) => { if (t > tf + life + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - 1 - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = CVk(bt, bf - 1 - tl, bf - 1); CVDash(bx, H, bx, by, CVW(0.15 + 0.45 * q), 2);
				g.strokeStyle = CVW(0.25 + 0.5 * q); g.lineWidth = 2; g.beginPath(); g.arc(bx, by, 26, 0, TAU); g.stroke();
				if (bt >= bf - 0.6)
				{
					double q2 = CVk(bt, bf - 0.6, bf); g.strokeStyle = CVW(0.2 + 0.5 * q2); g.setLineDash(new double[] { 6, 6 }); g.beginPath();
					for (int i = 0; i < N; i++) { g.moveTo(bx + vx[i] * 30, by + vy[i] * 30); g.lineTo(bx + vx[i] * 120, by + vy[i] * 120); }
					g.stroke(); g.setLineDash(new double[0]);
				}
				if (t >= tr)
				{
					double y = ry(t); g.strokeStyle = CVG(0.7); g.lineWidth = 3; g.beginPath(); g.moveTo(bx, y + 12); g.lineTo(bx, Math.Min(H + 20, y + 70)); g.stroke();
					g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.moveTo(bx, y - RR * 1.6); g.lineTo(bx + RR, y + RR); g.lineTo(bx - RR, y + RR); g.closePath(); g.fill();
				}
			}
			else
			{
				double u = t - tf, al = u < life ? 1 : 1 - (u - life) / 0.3; if (al <= 0) { g.restore(); return; }
				g.globalAlpha = al; g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; double d = sp * u;
				for (int i = 0; i < N; i++) circle(bx + vx[i] * d, by + vy[i] * d + 30 * u * u, RS);
				g.shadowBlur = 0; g.fillStyle = CVL(0.9); for (int i = 0; i < N; i++) circle(bx + vx[i] * d, by + vy[i] * d + 30 * u * u, 2.5);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tr) return false;
			if (t < tf) { double y = ry(t); return hypot(p.x - bx, p.y - y) < RR + p.r - 2; }
			double u = t - tf; if (u >= life) return false; double d = sp * u;
			for (int i = 0; i < N; i++) if (hypot(p.x - bx - vx[i] * d, p.y - by - vy[i] * d - 30 * u * u) < RS + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// ---------- 10. the big wheel (2:21.0) ----------
	// A fair's wheel in the middle (its frame is gold, harmless) with 8 pink cars. It turns rw degrees a beat (backwards from bRev); on
	// each breath beat it pulls in from 240 to 150 and back in 2 beats (the inner circle shows dashed a beat before). On each spoke beat
	// two opposite spokes light up pink for half a beat (white dashes a beat before).
	public static double CVWheelRot(double bt, double b0, double rw, double bRev) => bt < bRev ? rw * (bt - b0) : rw * (bRev - b0) - rw * (bt - bRev);
	public static double CVWheelR(double bt, double[] br)
	{
		foreach (var s in br) if (bt >= s && bt < s + 2) return 240 - 90 * JS.sin(Math.PI * (bt - s) / 2);
		return 240;
	}
	public static (double x, double y) CVCar(double bt, int i, double b0, double rw, double bRev, double[] br)
	{
		double q = CVRad(CVWheelRot(bt, b0, rw, bRev) + 45 * i), R = CVWheelR(bt, br);
		return (640 + R * JS.cos(q), 360 + R * JS.sin(q));
	}
	// the frame, the stand and the hangers (gold, harmless, under everything that hurts)
	public static Hz CVWheelFrame(double b0, double b1, double rw, double bRev, double[] br)
	{
		var h = new Hz { kind = "CVWheelFrame", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2) return; double a = CVk(bt, b0 - 2, b0 - 1) * (1 - CVk(bt, b1, b1 + 1));
			double rot = CVRad(CVWheelRot(bt, b0, rw, bRev)), R = CVWheelR(bt, br);
			g.save(); g.globalAlpha = a;
			g.strokeStyle = CVG(0.55); g.lineWidth = 3; g.beginPath(); g.arc(640, 360, R, 0, TAU); g.stroke();
			g.lineWidth = 2; g.beginPath(); g.arc(640, 360, R * 0.55, 0, TAU);
			for (int i = 0; i < 8; i++) { double q = rot + CVRad(45 * i); g.moveTo(640 + JS.cos(q) * 30, 360 + JS.sin(q) * 30); g.lineTo(640 + JS.cos(q) * R, 360 + JS.sin(q) * R); }
			g.stroke();
			g.strokeStyle = CVG(0.35); g.lineWidth = 8; g.beginPath(); g.moveTo(640, 360); g.lineTo(520, H + 10); g.moveTo(640, 360); g.lineTo(760, H + 10); g.stroke();
			g.fillStyle = CVG(0.9); circle(640, 360, 22);
			// the breath to come: the inner circle dashed
			foreach (var s in br) if (bt >= s - 1 && bt < s + 1) { double q = CVk(bt, s - 1, s); g.strokeStyle = CVW(0.25 + 0.5 * q * (1 - CVk(bt, s + 0.5, s + 1))); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(640, 360, 150, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			g.strokeStyle = CVG(0.8); g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < 8; i++) { var c = CVCar(bt, i, b0, rw, bRev, br); g.moveTo(c.x, c.y - 23); g.lineTo(c.x, c.y - 15); }
			g.stroke(); g.restore();
		};
		return h;
	}
	public static Hz CVWheel(double b0, double b1, double rw, double bRev, double[] br, double[] sb)
	{
		var h = new Hz { kind = "CVWheel" }; const double CW = 19, CH = 15, SW = 6;
		int spokeAt(double bt) { for (int k = 0; k < sb.Length; k++) if (bt >= sb[k] - 1 && bt < sb[k] + 0.5) return k; return -1; }
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2) return; double a = CVk(bt, b0 - 2, b0 - 1) * (1 - CVk(bt, b1, b1 + 1)), on = CVk(bt, b0 - 1, b0);
			double rot = CVRad(CVWheelRot(bt, b0, rw, bRev)), R = CVWheelR(bt, br);
			g.save(); g.globalAlpha = a;
			// the spokes that light up
			int k = spokeAt(bt);
			if (k >= 0)
			{
				int si = (k * 3) % 8; double q0 = rot + CVRad(45 * si);
				if (bt < sb[k])
				{
					double q = CVk(bt, sb[k] - 1, sb[k]);
					for (int m = 0; m < 2; m++) { double q1 = q0 + m * Math.PI; CVDash(640 + JS.cos(q1) * 34, 360 + JS.sin(q1) * 34, 640 + JS.cos(q1) * (R - 22), 360 + JS.sin(q1) * (R - 22), CVW(0.25 + 0.6 * q), 3); }
				}
				else
				{
					g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = 2 * SW; g.lineCap = "round"; g.beginPath();
					for (int m = 0; m < 2; m++) { double q1 = q0 + m * Math.PI; g.moveTo(640 + JS.cos(q1) * 34, 360 + JS.sin(q1) * 34); g.lineTo(640 + JS.cos(q1) * (R - 22), 360 + JS.sin(q1) * (R - 22)); }
					g.stroke(); g.shadowBlur = 0;
				}
			}
			// the cars: they always hang straight
			for (int i = 0; i < 8; i++)
			{
				var c = CVCar(bt, i, b0, rw, bRev, br);
				if (on < 1) { g.strokeStyle = CVW(0.3 + 0.6 * on); g.lineWidth = 2; g.setLineDash(new double[] { 6, 5 }); g.strokeRect(c.x - CW, c.y - CH, 2 * CW, 2 * CH); g.setLineDash(new double[0]); }
				else
				{
					g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.roundRect(c.x - CW, c.y - CH, 2 * CW, 2 * CH, 7); g.fill(); g.shadowBlur = 0;
					g.fillStyle = CVL(0.85); g.fillRect(c.x - CW + 6, c.y - CH + 6, 2 * CW - 12, 7);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			for (int i = 0; i < 8; i++) { var c = CVCar(bt, i, b0, rw, bRev, br); if (obbHit(p.x, p.y, p.r - 2, c.x, c.y, CW, CH, 0)) return true; }
			int k = spokeAt(bt);
			if (k >= 0 && bt >= sb[k])
			{
				double rot = CVRad(CVWheelRot(bt, b0, rw, bRev)), R = CVWheelR(bt, br), q0 = rot + CVRad(45 * ((k * 3) % 8));
				for (int m = 0; m < 2; m++) { double q1 = q0 + m * Math.PI; if (CVSeg(p.x, p.y, 640 + JS.cos(q1) * 34, 360 + JS.sin(q1) * 34, 640 + JS.cos(q1) * (R - 22), 360 + JS.sin(q1) * (R - 22)) < SW + p.r - 2) return true; }
			}
			return false;
		};
		return h;
	}

	// ---------- 11. the words: C'EST LA VIE (2:53.6) ----------
	// A letter on a pink tile falls in column x at bIn (its column dashed from bIn - tl) to yRest, bounces there on every beat
	// ("la la la"), and from bOut falls away down (its way dashed from bOut - tl).
	public static Hz CVLetter(string ch, double x, double bIn, double bOut, double yRest, double tl, double hw = 36, double hh = 42)
	{
		var h = new Hz { kind = "CVLetter" }; double tIn = B(bIn), tOut = B(bOut);
		double yy(double t)
		{
			double bt = BT(t);
			if (bt < bIn + 1) return -hh - 20 + (yRest + hh + 20) * CVIn(CVk(bt, bIn, bIn + 1));
			if (t < tOut) return yRest - 22 * Math.Abs(JS.sin(Math.PI * (bt - bIn - 1)));
			double u = t - tOut; return yRest + 0.5 * 2600 * u * u;
		}
		h.update = (t, dt) => { if (t > tOut && yy(t) > H + 80) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bIn - tl) return;
			g.save();
			if (bt < bIn + 0.6) { double q = CVk(bt, bIn - tl, bIn) * (1 - CVk(bt, bIn, bIn + 0.6)); CVDash(x, 0, x, yRest + hh, CVW(0.15 + 0.45 * q), 2); }
			if (bt >= bOut - tl && bt < bOut + 0.6) { double q = CVk(bt, bOut - tl, bOut) * (1 - CVk(bt, bOut, bOut + 0.6)); CVDash(x, yRest + hh + 6, x, H, CVW(0.2 + 0.55 * q), 3); }
			if (bt >= bIn)
			{
				double y = yy(t); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.roundRect(x - hw, y - hh, 2 * hw, 2 * hh, 12); g.fill(); g.shadowBlur = 0;
				g.font = $"800 {str(hh * 1.35)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = CVL(1); g.fillText(ch, x, y + 3);
			}
			g.restore();
		};
		h.hit = (p, t) => t >= tIn && obbHit(p.x, p.y, p.r - 2, x, yy(t), hw, hh, 0);
		return h;
	}
	// the apostrophe of C'EST (gold, harmless) rides with the C
	public static Hz CVApos(double x, double bIn, double bOut, double yRest)
	{
		var h = new Hz { kind = "CVApos" }; double tOut = B(bOut);
		h.update = (t, dt) => { if (t > tOut + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bIn || t > tOut) return; double a = CVk(bt, bIn, bIn + 1);
			double y = yRest - 60 - 22 * Math.Abs(JS.sin(Math.PI * Math.Max(0, bt - bIn - 1)));
			g.save(); g.globalAlpha = a; g.fillStyle = CVG(1); g.beginPath(); g.moveTo(x - 6, y - 14); g.lineTo(x + 6, y - 14); g.lineTo(x - 2, y + 10); g.closePath(); g.fill(); g.restore();
		};
		return h;
	}

	// ---------- 12. the morning (3:01) ----------
	// The night closes in: everything outside a circle round the middle hurts; the circle shrinks from R0 to R1 between b0 and b1.
	// Where its edge will be one beat later shows dashed (white). At b1 the circle opens and it all ends.
	public static Hz CVClose(double b0, double b1, double R0, double R1)
	{
		var h = new Hz { kind = "CVClose" };
		double R(double bt) => bt < b0 ? R0 : bt < b1 ? R0 + (R1 - R0) * CVEase((bt - b0) / (b1 - b0)) : R1 + 900 * CVOut((bt - b1) / 0.6);
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return;
			g.save();
			if (bt < b1)
			{
				double r1 = R(Math.Min(bt + 1, b1)); g.strokeStyle = CVW(0.55); g.lineWidth = 2; g.setLineDash(new double[] { 12, 9 }); g.beginPath(); g.arc(640, 360, r1, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				if (bt < b0 + 1) { g.strokeStyle = CVG(0.6 * CVk(bt, b0 - 1, b0)); g.lineWidth = 3; g.beginPath(); g.arc(640, 360, R1, 0, TAU); g.stroke(); }
			}
			double r = R(bt);
			if (bt >= b0 && r < 760)
			{
				double a = bt < b1 ? 1 : 1 - CVk(bt, b1, b1 + 0.6);
				g.globalAlpha = a; g.beginPath(); g.rect(-20, -20, W + 40, H + 40); g.arc(640, 360, r, 0, TAU, true); g.fillStyle = CVP(0.6); g.fill("evenodd");
				g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineWidth = 8; g.beginPath(); g.arc(640, 360, r + 4, 0, TAU); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1) return false; return hypot(p.x - 640, p.y - 360) + p.r - 2 > R(bt); };
		return h;
	}
	// the last words in gold (harmless), when everything is gone
	public static Hz CVEnd(double b0, double b1)
	{
		var h = new Hz { kind = "CVEnd", z = 1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = CVk(bt, b0, b0 + 0.3);
			g.save(); g.globalAlpha = k; g.font = $"800 {str(110 - 24 * k)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#f0be50"; g.shadowBlur = 30; g.fillStyle = "#f8dc98"; g.fillText("C'EST LA VIE", W / 2, H / 2); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
}
