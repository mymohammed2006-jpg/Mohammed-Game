using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// راضي بحبك (main level 23), part 2: the heart, the arrows, the rose (and the small things part 3 shares)
public static partial class G
{
	// ---------- the heart beats (0:52.6) ----------
	// A heart wave (pink outline, th px thick) grows from size s0 at b by v px/s (v < 0: comes in from outside). One piece of it is open:
	// the heart's points round ug (0 = the dip at the top, pi = the tip at the bottom) within gw. From b - tl its shape shows dashed at size
	// sh, with the open way, so you know where to be.
	public static Hz RZHeartWave(double cx, double cy, double b, double s0, double v, double ug, double gw, double tl, double sh, double th = 10)
	{
		var h = new Hz { kind = "RZHeartWave" }; double tb = B(b);
		var open = new bool[RZN];
		for (int i = 0; i < RZN; i++) { double u = TAU * (i + 0.5) / RZN, d = Math.Abs(u - ug) % TAU; if (d > Math.PI) d = TAU - d; open[i] = d < gw; }
		double sz(double t) => s0 + v * (t - tb);
		h.update = (t, dt) => { if (t > tb && (v > 0 ? sz(t) > 1000 : sz(t) < 12)) h.dead = true; };
		void path(double s)
		{
			var X = RZHX; var Y = RZHY; g.beginPath();
			for (int i = 0; i < RZN; i++) { if (open[i]) continue; g.moveTo(cx + X[i] * s, cy + Y[i] * s); g.lineTo(cx + X[i + 1] * s, cy + Y[i + 1] * s); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save(); g.lineCap = "round";
			if (bt < b)
			{
				double q = RZk(bt, b - tl, b); g.strokeStyle = RZW(0.3 + 0.5 * q); g.lineWidth = 2.5; g.setLineDash(new double[] { 10, 9 }); path(sh); g.stroke(); g.setLineDash(new double[0]);
				// the open way: two gold marks at its ends
				g.fillStyle = RZG(0.5 + 0.5 * q);
				for (int i = 0; i < RZN; i++) if (open[i] != open[(i + 1) % RZN]) circle(cx + RZHX[i + 1] * sh, cy + RZHY[i + 1] * sh, 5);
			}
			else
			{
				double s = sz(t); if (s > 4)
				{
					g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = th; path(s); g.stroke(); g.shadowBlur = 0;
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tb) return false; double s = sz(t); if (s < 4) return false; double m = th / 2 + p.r - 2;
			// far from the curve at this size: no need to look closer (the heart lies within 0.85..1.15 s of its middle near the curve, roughly)
			double lx = (p.x - cx) / s, ly = (p.y - cy) / s, rr = hypot(lx, ly); if (rr > 1.25 + m / s) return false; if (rr < 0.3 - m / s) return false;
			var X = RZHX; var Y = RZHY;
			for (int i = 0; i < RZN; i++)
			{
				if (open[i]) continue;
				if (RZSeg(p.x, p.y, cx + X[i] * s, cy + Y[i] * s, cx + X[i + 1] * s, cy + Y[i + 1] * s) < m) return true;
			}
			return false;
		};
		return h;
	}
	// the heart in the middle (gold, harmless) that beats from b0 to b1
	public static Hz RZCore(double b0, double b1, double s)
	{
		var h = new Hz { kind = "RZCore", z = -1 };   // under the attacks: what hurts is never covered
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double a = RZk(bt, b0, b0 + 0.5) * (1 - RZk(bt, b1, b1 + 1)), pu = RZPulse(bt), pu2 = RZPulse(bt - 0.33) * 0.6;
			double ss = s * (1 + 0.1 * pu + 0.05 * pu2);
			g.save(); g.globalAlpha = a; g.shadowColor = "#ecbe5a"; g.shadowBlur = 18 + 12 * pu;
			var gr = g.createRadialGradient(W / 2 - ss * 0.3, H / 2 - ss * 0.3, 4, W / 2, H / 2, ss * 1.3); gr.addColorStop(0, "rgba(252,232,170,1)"); gr.addColorStop(1, "rgba(170,112,36,1)");
			g.fillStyle = gr; RZHeartPathAt(W / 2, H / 2, ss); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = RZW(0.6); g.lineWidth = 2; RZHeartPathAt(W / 2, H / 2, ss * 0.7); g.stroke();
			g.restore();
		};
		return h;
	}

	// ---------- the arrows (1:07.6) ----------
	// A wall of arrows comes in from one edge (dir 0: from the left going right, 1: from the top going down, 2: from the right, 3: from the bottom)
	// at sp px/s from b. It is open where it crosses gc +- gw/2 (along the edge). From b - tl the edge shows the wall dashed with the open way.
	// What hurts is the pink band behind the arrows (len px deep), drawn exactly.
	public static Hz RZArrows(int dir, double gc, double gw, double b, double tl, double sp)
	{
		var h = new Hz { kind = "RZArrows" }; double tb = B(b); const double LEN = 78, SPC = 34;
		bool hor = dir % 2 == 0; double E = hor ? H : W, A = hor ? W : H, g0 = gc - gw / 2, g1 = gc + gw / 2;
		// along the way: the arrows' heads at lead(t) (from the edge they come from)
		double lead(double t) => -6 + sp * (t - tb);
		h.update = (t, dt) => { if (t > tb && lead(t) - LEN > A + 30) h.dead = true; };
		// (along, across) -> screen
		(double x, double y) P(double al, double ac) => dir switch { 0 => (al, ac), 1 => (ac, al), 2 => (W - al, ac), _ => (ac, H - al) };
		void band(double a0, double a1, double c0, double c1) { var p0 = P(a0, c0); var p1 = P(a1, c1); g.fillRect(Math.Min(p0.x, p1.x), Math.Min(p0.y, p1.y), Math.Abs(p1.x - p0.x), Math.Abs(p1.y - p0.y)); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			if (bt < b + 0.5)
			{
				double q = RZk(bt, b - tl, b) * (1 - RZk(bt, b, b + 0.5));
				var a0 = P(14, 0); var a1 = P(14, g0); var b0 = P(14, g1); var b1 = P(14, E);
				RZDash(a0.x, a0.y, a1.x, a1.y, RZW(0.3 + 0.55 * q), 3); RZDash(b0.x, b0.y, b1.x, b1.y, RZW(0.3 + 0.55 * q), 3);
				g.strokeStyle = RZG(0.5 + 0.5 * q); g.lineWidth = 4;
				foreach (var c in new[] { g0, g1 }) { var u = P(4, c); var w = P(40, c); g.beginPath(); g.moveTo(u.x, u.y); g.lineTo(w.x, w.y); g.stroke(); }
				// chevrons along the way it will go (white)
				g.fillStyle = RZW(0.35 * q);
				for (double c = SPC / 2; c < E; c += SPC * 4) { if (c > g0 - 10 && c < g1 + 10) continue; var u = P(40, c - 8); var w = P(40, c + 8); var z = P(56, c); g.beginPath(); g.moveTo(u.x, u.y); g.lineTo(z.x, z.y); g.lineTo(w.x, w.y); g.closePath(); g.fill(); }
			}
			if (t >= tb)
			{
				double l = lead(t);
				g.fillStyle = RZP(0.6); band(l - LEN, l, 0, g0); band(l - LEN, l, g1, E);
				g.shadowColor = PINK; g.shadowBlur = 8; g.strokeStyle = PINK; g.fillStyle = PINK; g.lineWidth = 4;
				for (double c = SPC / 2; c < E; c += SPC)
				{
					if (c > g0 - 15 && c < g1 + 15) continue;
					var s0 = P(l - LEN + 6, c); var s1 = P(l - 18, c); g.beginPath(); g.moveTo(s0.x, s0.y); g.lineTo(s1.x, s1.y); g.stroke();
					var hd = P(l, c); var hl = P(l - 22, c - 12); var hr = P(l - 22, c + 12); g.beginPath(); g.moveTo(hd.x, hd.y); g.lineTo(hl.x, hl.y); g.lineTo(hr.x, hr.y); g.closePath(); g.fill();
					var f0 = P(l - LEN + 4, c - 8); var f1 = P(l - LEN + 16, c); var f2 = P(l - LEN + 4, c + 8); g.beginPath(); g.moveTo(f0.x, f0.y); g.lineTo(f1.x, f1.y); g.lineTo(f2.x, f2.y); g.stroke();
				}
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tb) return false; double l = lead(t), m = p.r - 2;
			double al = dir switch { 0 => p.x, 1 => p.y, 2 => W - p.x, _ => H - p.y }, ac = hor ? p.y : p.x;
			if (al < l - LEN - m || al > l + m) return false;
			return ac < g0 + m || ac > g1 - m;
		};
		return h;
	}

	// ---------- the rose (1:25) ----------
	// n petals (pink, wd px wide in the middle) out of the middle of the screen, as long as L px; they turn (degrees at beat bt: ang(bt)).
	// From b0 the petals show dashed, they grow from b0 + 1 (in 1 beat), they fold back in the last beat before b1.
	// (two parts: the gold bud in the middle lies under all the attacks, z -1; the petals)
	public static Hz[] RZRose(int n, double b0, double b1, double L, double wd, Func<double, double> ang)
	{
		var h = new Hz { kind = "RZRose" }; var u0 = new Hz { kind = "RZBud", z = -1 }; const double R0 = 44;
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		u0.update = (t, dt) => { if (BT(t) > b1 + 0.5) u0.dead = true; };
		u0.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double a = ang(bt); g.save();
			double pu = RZPulse(bt), ba = RZk(bt, b0, b0 + 1) * (1 - RZk(bt, b1 - 0.5, b1 + 0.5));
			if (ba > 0)
			{
				g.globalAlpha = ba; g.fillStyle = RZG(1); g.shadowColor = "#ecbe5a"; g.shadowBlur = 14 + 8 * pu;
				for (int k = 0; k < 5; k++) { double q = RZRad(-a * 1.5 + k * 72); g.beginPath(); g.ellipse(W / 2 + JS.cos(q) * 14, H / 2 + JS.sin(q) * 14, 18, 10, q, 0, TAU); g.fill(); }
				g.shadowBlur = 0; g.fillStyle = "rgba(255,236,190,1)"; circle(W / 2, H / 2, 9 + 2 * pu); g.globalAlpha = 1;
			}
			g.restore();
		};
		double len(double bt) => bt < b0 + 1 ? 0 : R0 + (L - R0) * RZOut(bt - b0 - 1) * (1 - RZEase(bt - (b1 - 1)));
		double hw(double r, double l) => l <= R0 + 1 ? 0 : wd / 2 * JS.sin(Math.PI * clamp((r - R0) / (l - R0), 0, 1));
		void petal(double q, double l)
		{
			double c = JS.cos(q), s = JS.sin(q); g.beginPath(); g.moveTo(W / 2 + c * R0, H / 2 + s * R0);
			for (int j = 1; j <= 12; j++) { double r = R0 + (l - R0) * j / 12, w = hw(r, l); g.lineTo(W / 2 + c * r - s * w, H / 2 + s * r + c * w); }
			for (int j = 11; j >= 1; j--) { double r = R0 + (l - R0) * j / 12, w = hw(r, l); g.lineTo(W / 2 + c * r + s * w, H / 2 + s * r - c * w); }
			g.closePath();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double a = ang(bt), l = len(bt); g.save();
			if (bt < b0 + 1.4)
			{
				double q = RZk(bt, b0, b0 + 1) * (1 - RZk(bt, b0 + 1, b0 + 1.4));
				g.strokeStyle = RZW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 });
				for (int k = 0; k < n; k++) { petal(RZRad(a + k * 360.0 / n), L); g.stroke(); }
				g.setLineDash(new double[0]);
			}
			if (l > R0 + 2)
			{
				g.shadowColor = PINK; g.shadowBlur = 12;
				for (int k = 0; k < n; k++) { petal(RZRad(a + k * 360.0 / n), l); g.fillStyle = RZP(0.6); g.fill(); g.strokeStyle = PINK; g.lineWidth = 3; g.stroke(); }
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), l = len(bt); if (l < R0 + 4) return false; double a = ang(bt), dx = p.x - W / 2, dy = p.y - H / 2, r = hypot(dx, dy); if (r > l + p.r || r < R0 - p.r) return false;
			double phi = JS.atan2(dy, dx);
			for (int k = 0; k < n; k++)
			{
				double q = RZRad(a + k * 360.0 / n), d = phi - q; d = (d % TAU + TAU) % TAU; if (d > Math.PI) d -= TAU;
				double al = r * JS.cos(d), ac = Math.Abs(r * JS.sin(d)); if (al < R0 - p.r || al > l + p.r) continue;
				if (ac < hw(al, l) + p.r - 2) return true;
			}
			return false;
		};
		return new[] { u0, h };
	}
	// the angle (degrees) of something that turns rate degrees a beat, faster on the beat (dance), changing rate at the beats kb[i] to kr[i]
	public static double RZSpin(double[] kb, double[] kr, double a0, double bt)
	{
		double F(double r, double x) => r * (x + 0.6 * JS.sin(TAU * x) / TAU);
		double a = a0;
		for (int i = 0; i < kb.Length; i++)
		{
			double s = kb[i], e = i + 1 < kb.Length ? kb[i + 1] : 1e9; if (bt <= s) break;
			double x = Math.Min(bt, e); a += F(kr[i], x) - F(kr[i], s);
		}
		return a;
	}

	// ---------- shots from the middle: petals (the rose) or dots (the clock) ----------
	// n of them fly out from r0 at angles a0 + k*360/n (degrees) at sp px/s from b; their ways show dashed from b - tl.
	public static Hz RZShots(int n, double a0, double r0, double b, double tl, double sp, double sz, bool petal)
	{
		var h = new Hz { kind = "RZShots" }; double tb = B(b);
		double rad(double t) => r0 + sp * Math.Max(0, t - tb);
		h.update = (t, dt) => { if (t > tb && rad(t) > 820) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return; g.save();
			if (bt < b + 0.3)
			{
				double q = RZk(bt, b - tl, b) * (1 - RZk(bt, b, b + 0.3));
				for (int k = 0; k < n; k++) { double a = RZRad(a0 + k * 360.0 / n), c = JS.cos(a), s = JS.sin(a); RZDash(W / 2 + c * r0, H / 2 + s * r0, W / 2 + c * 800, H / 2 + s * 800, RZW(0.15 + 0.4 * q), 2); }
			}
			if (t >= tb)
			{
				double r = rad(t); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				for (int k = 0; k < n; k++)
				{
					double a = RZRad(a0 + k * 360.0 / n), x = W / 2 + JS.cos(a) * r, y = H / 2 + JS.sin(a) * r;
					if (petal) { g.beginPath(); g.ellipse(x, y, sz * 1.6, sz, a, 0, TAU); g.fill(); } else circle(x, y, sz);
				}
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tb) return false; double r = rad(t);
			for (int k = 0; k < n; k++) { double a = RZRad(a0 + k * 360.0 / n); if (hypot(p.x - W / 2 - JS.cos(a) * r, p.y - H / 2 - JS.sin(a) * r) < sz + p.r - 2) return true; }
			return false;
		};
		return h;
	}
}
