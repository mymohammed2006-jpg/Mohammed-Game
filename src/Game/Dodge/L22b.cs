using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Shiawase VIP (main level 22), part 2: the shards attack.
public static partial class G
{
	// ---------- 5. rows of shards (1:16.9) ----------
	// A row of shards across the screen (tilted by tilt degrees) with a gap gw wide at gx. It comes in from the top (dir 1) or the bottom
	// (dir -1): smoothly at sp px/s from bf, or, with steps, it jumps st px on each step beat. Where the gap will be shows on the edge it
	// comes from (white dashes, the gap left open, gold marks) from bf - tl.
	public static Hz SWRow(int dir, double tilt, double gx, double gw, double bf, double tl, double sp, double[] steps = null, double st = 0, double sz = 22)
	{
		var h = new Hz { kind = "SWRow" }; const double GAPX = 38; double tf = B(bf), tn = JS.tan(SWRad(tilt)), y0 = dir > 0 ? -40 - Math.Abs(tn) * 640 : H + 40 + Math.Abs(tn) * 640;
		double yc(double t)
		{
			if (steps == null) return y0 + dir * sp * Math.Max(0, t - tf);
			double bt = BT(t), d = 0; foreach (var s in steps) { if (bt < s) break; d += st * SWOut((bt - s) / 0.12); }
			if (steps.Length > 0) d += 700 * Math.Max(0, t - B(steps[steps.Length - 1] + 0.5));   // after its last step it rushes off
			return y0 + dir * d;
		}
		void each(double t, Action<double, double> f)
		{
			double c = yc(t);
			for (double x = -20; x < W + 30; x += GAPX) { if (Math.Abs(x - gx) < gw / 2) continue; f(x, c + tn * (x - 640)); }
		}
		h.update = (t, dt) => { double c = yc(t), span = Math.Abs(tn) * 640 + 60; if (dir > 0 ? c - span > H : c + span < 0) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.6)
			{
				double q = SWk(bt, bf - tl, bf) * (1 - SWk(bt, bf, bf + 0.6)), ye = dir > 0 ? 10 : H - 10;
				g.strokeStyle = SWW(0.25 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				g.moveTo(0, ye); g.lineTo(Math.Max(0, gx - gw / 2), ye); g.moveTo(Math.Min(W, gx + gw / 2), ye); g.lineTo(W, ye); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = SWG(0.5 + 0.5 * q); g.lineWidth = 4; double yy = dir > 0 ? 4 : H - 4, d2 = dir > 0 ? 26 : -26;
				g.beginPath(); g.moveTo(gx - gw / 2, yy); g.lineTo(gx - gw / 2, yy + d2); g.moveTo(gx + gw / 2, yy); g.lineTo(gx + gw / 2, yy + d2); g.stroke();
			}
			if (bt >= bf || (steps != null && bt >= bf - 0.01))
			{
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				each(t, (x, y) => { if (y > -30 && y < H + 30) SWShard(x, y, sz, dir > 0 ? Math.PI / 2 : -Math.PI / 2); });
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (BT(t) < bf) return false; bool hit = false;
			each(t, (x, y) => { if (!hit && Math.Abs(p.y - y) < sz && SWShardHit(p, x, y, sz)) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 6. the eyes (1:42.5) ----------
	// The half mask at the top: a beam from each eye, the two swinging like scissors (mirrored) once a bar. On each kick (kb) the beams leave
	// a burnt line (pink) where they are, for burn beats. The beams show dashed from b0, burn from b0 + 1 to b1.
	public static double SWEyeA(double bt, double b0) => -20 + 58 * JS.sin(TAU * (bt - b0 - 1) / 3);
	public static Hz SWEyes(double b0, double b1, double[] kb, double burn, double amp = 1)
	{
		var h = new Hz { kind = "SWEyes" };
		(double x, double y) eye(int s) { var m = SWMaskAt(181); return (m.x + s * 37 * m.sc, m.y - 22 * m.sc); }
		double ang(int s, double bt) => s * amp * SWEyeA(bt, b0);   // degrees from straight down; s -1 = the left eye
		h.update = (t, dt) => { if (BT(t) > b1 + burn + 0.5) h.dead = true; };
		void beam(int s, double a, Action<double, double, double, double> f) { var e = eye(s); double q = SWRad(a), dx = JS.sin(q), dy = JS.cos(q), l = SWExit(e.x, e.y, dx, dy); f(e.x, e.y, e.x + dx * l, e.y + dy * l); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return;
			g.save(); g.lineCap = "round";
			if (bt < b0 + 1) { foreach (int s in new[] { -1, 1 }) beam(s, ang(s, bt), (x0, y0, x1, y1) => SWDash(x0, y0, x1, y1, SWW(0.3 + 0.5 * SWk(bt, b0, b0 + 1)), 3)); g.restore(); return; }
			// burnt lines
			foreach (var k in kb)
			{
				if (k > bt || k < b0 + 1) continue; if (bt > k + burn + 0.3) continue; double a = 1 - SWk(bt, k + burn, k + burn + 0.3);
				g.shadowColor = PINK; g.shadowBlur = 8; g.strokeStyle = SWP(a); g.lineWidth = 6;
				foreach (int s in new[] { -1, 1 }) beam(s, ang(s, k), (x0, y0, x1, y1) => { g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); });
			}
			if (bt < b1)
			{
				foreach (int s in new[] { -1, 1 })
					beam(s, ang(s, bt), (x0, y0, x1, y1) =>
					{
						g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = SWP(0.45); g.lineWidth = 16; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
						g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 8; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
					});
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 + 1) return false; bool hit = false;
			if (bt < b1) foreach (int s in new[] { -1, 1 }) beam(s, ang(s, bt), (x0, y0, x1, y1) => { if (SWSeg(p.x, p.y, x0, y0, x1, y1) < 6 + p.r - 2) hit = true; });
			foreach (var k in kb)
			{
				if (hit) break; if (k > bt || k < b0 + 1 || bt > k + burn) continue;
				foreach (int s in new[] { -1, 1 }) beam(s, ang(s, k), (x0, y0, x1, y1) => { if (SWSeg(p.x, p.y, x0, y0, x1, y1) < 3 + p.r - 2) hit = true; });
			}
			return hit;
		};
		return h;
	}
	// A fan of n shards from the mask's chin (or from (x, y)) downwards, spread degrees between them, at sp px/s from bf; dashed from bf - tl.
	public static Hz SWFan(double x, double y, int n, double a0, double spread, double bf, double tl, double sp, double sz = 22)
	{
		var h = new Hz { kind = "SWFan" }; double tf = B(bf);
		double ang(int k) => SWRad(a0 + (k - (n - 1) / 2.0) * spread);   // degrees from straight down
		h.update = (t, dt) => { if (t > tf && (t - tf) * sp > 1000) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			for (int k = 0; k < n; k++)
			{
				double q = ang(k), dx = JS.sin(q), dy = JS.cos(q);
				if (bt < bf) { double l = SWExit(x, y, dx, dy); SWDash(x, y, x + dx * l, y + dy * l, SWW(0.2 + 0.5 * SWk(bt, bf - tl, bf)), 2); continue; }
				double d = (t - tf) * sp; g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; SWShard(x + dx * d, y + dy * d, sz, JS.atan2(dy, dx));
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = (t - tf) * sp;
			for (int k = 0; k < n; k++) { double q = ang(k); if (SWShardHit(p, x + JS.sin(q) * d, y + JS.cos(q) * d, sz)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 7. the broken rings (1:55) ----------
	// A ring of n shards grows from the mask (the middle) at sp px/s from bf, with a gap gw degrees wide at gap degrees (0 = right, clockwise).
	// It shows as a white dashed ring round the mask from bf - tl.
	public static Hz SWRing(int n, double gap, double gw, double bf, double tl, double sp, double sz = 22)
	{
		var h = new Hz { kind = "SWRing" }; const double R0 = 70; double tf = B(bf);
		bool open(int k) { double a = k * 360.0 / n - gap; a = ((a % 360) + 540) % 360 - 180; return Math.Abs(a) < gw / 2; }
		double rad(double t) => R0 + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && rad(t) > 800) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = SWk(bt, bf - tl, bf); g.strokeStyle = SWW(0.25 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 });
				g.beginPath(); g.arc(W / 2, H / 2, R0 + 20, SWRad(gap + gw / 2), SWRad(gap - gw / 2 + 360)); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = SWG(0.5 + 0.5 * q); g.lineWidth = 4; g.beginPath();
				foreach (var e in new[] { gap - gw / 2, gap + gw / 2 }) { double a = SWRad(e); g.moveTo(W / 2 + JS.cos(a) * (R0 + 6), H / 2 + JS.sin(a) * (R0 + 6)); g.lineTo(W / 2 + JS.cos(a) * (R0 + 40), H / 2 + JS.sin(a) * (R0 + 40)); }
				g.stroke(); g.restore(); return;
			}
			double r = rad(t); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			for (int k = 0; k < n; k++) { if (open(k)) continue; double a = SWRad(k * 360.0 / n); SWShard(W / 2 + JS.cos(a) * r, H / 2 + JS.sin(a) * r, sz, a); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double r = rad(t), d = hypot(p.x - W / 2, p.y - H / 2); if (Math.Abs(d - r) > sz) return false;
			for (int k = 0; k < n; k++) { if (open(k)) continue; double a = SWRad(k * 360.0 / n); if (SWShardHit(p, W / 2 + JS.cos(a) * r, H / 2 + JS.sin(a) * r, sz)) return true; }
			return false;
		};
		return h;
	}
	// A beam from the middle that turns with the rings' gaps, opposite them (its angle: a0 + w * (bt - bs) degrees + 180). Dashed from b0, on from bs to b1.
	public static Hz SWHand(double a0, double w, double bs, double b0, double b1)
	{
		var h = new Hz { kind = "SWHand" };
		double ang(double bt) => SWRad(a0 + w * (bt - bs) + 180);
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		void ray(double bt, Action<double, double, double, double> f) { double q = ang(bt), dx = JS.cos(q), dy = JS.sin(q); f(W / 2 + dx * 70, H / 2 + dy * 70, W / 2 + dx * 900, H / 2 + dy * 900); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 || bt > b1 + 0.3) return;
			g.save(); g.lineCap = "round";
			if (bt < bs) ray(bt, (x0, y0, x1, y1) => SWDash(x0, y0, x1, y1, SWW(0.3 + 0.5 * SWk(bt, b0, bs)), 3));
			else
			{
				double a = 1 - SWk(bt, b1, b1 + 0.3);
				ray(bt, (x0, y0, x1, y1) =>
				{
					g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = SWP(0.45 * a); g.lineWidth = 16; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
					g.shadowBlur = 0; g.strokeStyle = SWP(a); g.lineWidth = 8; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
				});
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < bs || bt >= b1) return false; bool hit = false; ray(bt, (x0, y0, x1, y1) => hit = SWSeg(p.x, p.y, x0, y0, x1, y1) < 6 + p.r - 2); return hit; };
		return h;
	}
}
