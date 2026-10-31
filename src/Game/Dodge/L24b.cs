using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24: part 2, the division (the hardest part, 3:54-4:34): the eight-pointed star, its rays, the darts
// through its notches, the spiral the eye throws.
public static partial class G
{
	// ---------- 6. the eight-pointed star ----------
	// Two squares make the star round (cx, cy): 8 tips at radius ro, 8 notches at 0.765 ro. It turns (degrees a beat w[i] from kb[i])
	// and breathes between rmin and rmax every per beats (rmin at b0). Each of its 8 blades (notch, tip, notch) is pink; the notches
	// are open (gap px of each edge is missing next to a notch), so the square can slip through there.
	public class QSStarDef
	{
		public double cx = W / 2, cy = H / 2, b0, b1, rmin, rmax, per, gap = 30, a0;
		public double[] kb, w;
		public double Ang(double bt) => a0 + QSTurn(kb, w, Math.Max(b0, bt));
		public double Ro(double bt) => rmin + (rmax - rmin) * (0.5 - 0.5 * JS.cos(TAU * Math.Max(0, bt - b0) / per));
		// the 16 pieces of the blades (x0, y0, x1, y1) at beat bt
		public void Pieces(double bt, Action<double, double, double, double> f)
		{
			double a = QSRad(Ang(bt)), ro = Ro(bt), ri = ro * 0.7654;
			for (int k = 0; k < 8; k++)
			{
				double qt = a + k * Math.PI / 4, tx = cx + JS.cos(qt) * ro, ty = cy + JS.sin(qt) * ro;
				for (int sd = -1; sd <= 1; sd += 2)
				{
					double qn = qt + sd * Math.PI / 8, nx = cx + JS.cos(qn) * ri, ny = cy + JS.sin(qn) * ri, l = hypot(tx - nx, ty - ny), u = Math.Min(0.8, gap / l);
					f(nx + (tx - nx) * u, ny + (ty - ny) * u, tx, ty);
				}
			}
		}
	}
	public static Hz QSStar(QSStarDef S, double bw)
	{
		var h = new Hz { kind = "QSStar" };
		h.update = (t, dt) => { if (BT(t) > S.b1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > S.b1 + 0.5) return;
			g.save(); g.lineCap = "round";
			if (bt < S.b0)
			{
				double q = QSk(bt, bw, S.b0); g.strokeStyle = QSW(0.25 + 0.55 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				S.Pieces(S.b0, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); g.setLineDash(new double[0]);
				// the notches (the ways through) are marked gold
				double a = QSRad(S.Ang(S.b0)), ri = S.Ro(S.b0) * 0.7654; g.fillStyle = QSG(0.3 + 0.5 * q);
				for (int k = 0; k < 8; k++) { double qn = a + Math.PI / 8 + k * Math.PI / 4; circle(S.cx + JS.cos(qn) * ri, S.cy + JS.sin(qn) * ri, 5); }
			}
			else
			{
				double al = 1 - QSk(bt, S.b1, S.b1 + 0.5);
				g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = QSP(al); g.lineWidth = 7; g.beginPath();
				S.Pieces(bt, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke();
				double a = QSRad(S.Ang(bt)), ri = S.Ro(bt) * 0.7654; g.shadowBlur = 0; g.fillStyle = QSG(0.7 * al);
				for (int k = 0; k < 8; k++) { double qn = a + Math.PI / 8 + k * Math.PI / 4; circle(S.cx + JS.cos(qn) * ri, S.cy + JS.sin(qn) * ri, 4); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < S.b0 || bt >= S.b1) return false; bool hit = false;
			S.Pieces(bt, (x0, y0, x1, y1) => { if (!hit && QSSeg(p.x, p.y, x0, y0, x1, y1) < 3.5 + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}
	// A ray from each tip (or each notch: notch = true) of the star outwards to the screen's edge: dashed from bw, pink from b for hot beats.
	// It keeps the direction the tip had at b.
	public static Hz QSStarRay(QSStarDef S, bool notch, int every, double bw, double b, double hot)
	{
		var h = new Hz { kind = "QSStarRay" };
		h.update = (t, dt) => { if (BT(t) > b + hot + 0.3) h.dead = true; };
		double a = QSRad(S.Ang(b) + (notch ? 22.5 : 0)), r0 = S.Ro(b) * (notch ? 0.7654 : 1);
		void each(Action<double, double, double, double> f) { for (int k = 0; k < 8; k += every) { double q = a + k * Math.PI / 4, c = JS.cos(q), s = JS.sin(q); f(S.cx + c * r0, S.cy + s * r0, S.cx + c * 1500, S.cy + s * 1500); } }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + hot + 0.3) return;
			g.save(); g.lineCap = "round";
			if (bt < b) { double q = QSk(bt, bw, b); each((x0, y0, x1, y1) => QSDash(x0, y0, x1, y1, QSW(0.25 + 0.55 * q), 2)); }
			else
			{
				double al = 1 - QSk(bt, b + hot, b + hot + 0.3), wd = 10 * QSOut((bt - b) / 0.1);
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = QSP(al); g.lineWidth = wd; g.beginPath(); each((x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b + 0.05 || bt >= b + hot) return false; bool hit = false;
			each((x0, y0, x1, y1) => { if (!hit && QSSeg(p.x, p.y, x0, y0, x1, y1) < 5 + p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 7. darts from the middle, evenly round ----------
	// n darts leave (cx, cy) at b at angles a0 + k*360/n, at sp px/s; their ways show dashed from bw.
	public static Hz QSBurst(double cx, double cy, int n, double a0, double bw, double b, double sp, double len = 36)
	{
		var h = new Hz { kind = "QSBurst" }; double tf = B(b);
		double dist(double t) => (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && dist(t) - len > 1500) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save();
			if (bt < b) { double q = QSk(bt, bw, b); for (int k = 0; k < n; k++) { double a = QSRad(a0 + k * 360.0 / n); QSDash(cx + JS.cos(a) * 40, cy + JS.sin(a) * 40, cx + JS.cos(a) * 1500, cy + JS.sin(a) * 1500, QSW(0.2 + 0.45 * q)); } }
			else { double d = dist(t); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; for (int k = 0; k < n; k++) { double a = QSRad(a0 + k * 360.0 / n); QSDart(cx + JS.cos(a) * d, cy + JS.sin(a) * d, len, a, 5); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = dist(t);
			for (int k = 0; k < n; k++) { double a = QSRad(a0 + k * 360.0 / n); if (QSDartHit(p, cx + JS.cos(a) * d, cy + JS.sin(a) * d, len, a)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 8. the spiral (4:04) ----------
	// The eye throws darts from (cx, cy): arms arms, rate darts a beat on each, from b0 to b1, at sp px/s. The arms turn: degrees a beat
	// w[i] from kb[i] (a0 at b0). Where the next darts go shows as short dashed rays half a beat ahead (from b0 - 1).
	public static Hz QSSpiral(double cx, double cy, int arms, double rate, double b0, double b1, double sp, double a0, double[] kb, double[] w, double len = 30)
	{
		var h = new Hz { kind = "QSSpiral" }; int n = (int)Math.Floor((b1 - b0) * rate - 1e-9) + 1;
		var tb = new double[n]; var qa = new double[n];
		for (int i = 0; i < n; i++) { double bi = b0 + i / rate; tb[i] = B(bi); qa[i] = a0 + QSTurn(kb, w, bi); }
		double life = 1500 / sp;
		h.update = (t, dt) => { if (t > tb[n - 1] + life + 0.2) h.dead = true; };
		void each(double t, Action<double, double, double> f)
		{
			for (int i = 0; i < n; i++)
			{
				if (tb[i] > t) break; double d = (t - tb[i]) * sp; if (d - len > 1500) continue;
				for (int k = 0; k < arms; k++) { double a = QSRad(qa[i] + k * 360.0 / arms); f(cx + JS.cos(a) * d, cy + JS.sin(a) * d, a); }
			}
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return;
			g.save();
			if (bt < b1)
			{
				double q = bt < b0 ? QSk(bt, b0 - 1, b0) : 1, an = a0 + QSTurn(kb, w, Math.Max(b0, bt + 0.5));
				for (int k = 0; k < arms; k++) { double a = QSRad(an + k * 360.0 / arms); QSDash(cx + JS.cos(a) * 40, cy + JS.sin(a) * 40, cx + JS.cos(a) * 240, cy + JS.sin(a) * 240, QSW(0.25 + 0.4 * q)); }
			}
			g.shadowColor = PINK; g.shadowBlur = 8; g.fillStyle = PINK;
			each(t, (x, y, a) => { if (x > -30 && x < W + 30 && y > -30 && y < H + 30) QSDart(x, y, len, a, 5); });
			g.restore();
		};
		h.hit = (p, t) => { bool hit = false; each(t, (x, y, a) => { if (!hit && Math.Abs(p.x - x) < 40 && Math.Abs(p.y - y) < 40 && QSDartHit(p, x, y, len, a)) hit = true; }); return hit; };
		return h;
	}
}
