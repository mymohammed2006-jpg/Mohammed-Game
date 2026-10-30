using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Ya Tayeb El Galb (main level 28), parts 2 and 3: the rose and the light.
public static partial class G
{
	// ---------- 5. the rose's thorny stems (4:18.5) ----------
	// n stems from the rose (from r0 px out of the middle to past the screen) turn together: w degrees a beat, and on each kick in kk a
	// push of surge degrees more (eased over half a beat). Every fl beats from bs they turn the other way. They show dashed from b0,
	// are pink and hurt from bs to b1, then wither white.
	static double TQStemAng(double bt, double a0, double w, double bs, double[] kk, double surge, double fl)
	{
		double TurnDir(double b) => Math.Floor((b - bs) / fl) % 2 == 0 ? 1 : -1;
		double a = a0, u = bs;
		if (bt <= bs) a += w * (bt - bs);
		else while (u < bt) { double e = Math.Min(bt, bs + (Math.Floor((u - bs) / fl + 1e-9) + 1) * fl); a += TurnDir(u + 1e-6) * w * (e - u); u = e; }
		foreach (var k in kk) { if (k > bt) break; a += TurnDir(k + 1e-6) * surge * TQEase((bt - k) / 0.5); }
		return a;
	}
	public static Hz TQStems(int n, double a0, double w, double[] kk, double surge, double fl, double b0, double bs, double b1, double r0 = 70)
	{
		var h = new Hz { kind = "TQStems" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		void each(double bt, Action<double, double, double, double, double> f)
		{
			double a = TQStemAng(bt, a0, w, bs, kk, surge, fl);
			for (int k = 0; k < n; k++) { double q = TQRad(a + k * 360.0 / n), dx = JS.cos(q), dy = JS.sin(q); f(W / 2 + dx * r0, H / 2 + dy * r0, W / 2 + dx * 900, H / 2 + dy * 900, q); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 || bt > b1 + 1) return;
			g.save(); g.lineCap = "round";
			if (bt < bs) { if (TQWarnPass) each(bt, (x0, y0, x1, y1, q) => TQDash(x0, y0, x1, y1, TQW(0.25 + 0.5 * TQk(bt, b0, bs)), 3)); g.restore(); return; }
			bool on = bt < b1; double fa = on ? 1 : 1 - TQk(bt, b1, b1 + 1);
			if (on == TQWarnPass) { g.restore(); return; }
			each(on ? bt : b1, (x0, y0, x1, y1, q) =>
			{
				double dx = JS.cos(q), dy = JS.sin(q), nx = -dy, ny = dx;
				g.strokeStyle = on ? PINK : TQW(0.5 * fa); g.fillStyle = g.strokeStyle; if (on) { g.shadowColor = PINK; g.shadowBlur = 12; }
				g.lineWidth = 8; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.shadowBlur = 0;
				// the thorns: small triangles on both sides in turn
				g.beginPath();
				for (int i = 0; i < 18; i++)
				{
					double d = 40 + i * 46, s = i % 2 == 0 ? 1 : -1, bx = x0 + dx * d, by = y0 + dy * d;
					g.moveTo(bx - dx * 6, by - dy * 6); g.lineTo(bx + nx * s * 9, by + ny * s * 9); g.lineTo(bx + dx * 6, by + dy * 6); g.closePath();
				}
				g.fill();
			});
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bs || bt >= b1) return false; bool hit = false;
			each(bt, (x0, y0, x1, y1, q) => { if (!hit && TQSeg(p.x, p.y, x0, y0, x1, y1) < 4 + p.r - 3) hit = true; });
			return hit;
		};
		return h;
	}
	// a petal: an oval sz long, 0.55 sz wide, its length along q
	static void TQPetal(double x, double y, double sz, double q) { g.beginPath(); g.ellipse(x, y, sz, sz * 0.55, q, 0, TAU); g.fill(); }
	static bool TQPetalHit(Player p, double x, double y, double sz, double q)
	{
		double dx = p.x - x, dy = p.y - y; if (dx * dx + dy * dy > (sz + p.r) * (sz + p.r)) return false;
		double c = JS.cos(q), s = JS.sin(q), u = dx * c + dy * s, v = -dx * s + dy * c, ra = sz + p.r - 3, rb = 0.55 * sz + p.r - 3;
		return (u * u) / (ra * ra) + (v * v) / (rb * rb) < 1;
	}
	// The rose throws n petals out round it at sp px/s from bf, the first at a0 degrees; their ways show dashed from bf - tl.
	public static Hz TQPetals(double bf, int n, double a0, double sp, double sz, double tl)
	{
		var h = new Hz { kind = "TQPetals" }; double tf = B(bf);
		double ang(int k) => TQRad(a0 + k * 360.0 / n);
		h.update = (t, dt) => { if (t > tf && 60 + (t - tf) * sp > 1000) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = TQk(bt, bf - tl, bf);
				if (TQWarnPass) for (int k = 0; k < n; k++) { double a = ang(k), dx = JS.cos(a), dy = JS.sin(a); TQDash(W / 2 + dx * 80, H / 2 + dy * 80, W / 2 + dx * 280, H / 2 + dy * 280, TQW(0.2 + 0.5 * q), 2); }
				g.restore(); return;
			}
			if (TQWarnPass) { g.restore(); return; }
			double d = 60 + (t - tf) * sp; g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			for (int k = 0; k < n; k++) { double a = ang(k); TQPetal(W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d, sz, a); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = 60 + (t - tf) * sp; if (Math.Abs(hypot(p.x - W / 2, p.y - H / 2) - d) > sz + p.r) return false;
			for (int k = 0; k < n; k++) { double a = ang(k); if (TQPetalHit(p, W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d, sz, a)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 6. the petal rain (4:33) ----------
	// A row of petals across the screen, tilted by tilt degrees (the wind), falls from the top (dir 1) or rises from the bottom (dir -1)
	// at sp px/s from bf, with a gap gw wide at gx. Where the gap will be shows on its edge (white dashes, gold marks) from bf - tl.
	public static Hz TQRain(int dir, double tilt, double gx, double gw, double bf, double tl, double sp, double sz = 20)
	{
		var h = new Hz { kind = "TQRain" }; const double GAPX = 40; double tf = B(bf), tn = JS.tan(TQRad(tilt)), y0 = dir > 0 ? -30 - Math.Abs(tn) * 640 : H + 30 + Math.Abs(tn) * 640;
		double q = dir > 0 ? Math.PI / 2 - TQRad(tilt) * 0.8 : -Math.PI / 2 - TQRad(tilt) * 0.8;
		double yc(double t) => y0 + dir * sp * Math.Max(0, t - tf);
		void each(double t, Action<double, double> f) { double c = yc(t); for (double x = -20; x < W + 30; x += GAPX) { if (Math.Abs(x - gx) < gw / 2) continue; f(x, c + tn * (x - 640)); } }
		h.update = (t, dt) => { double c = yc(t), span = Math.Abs(tn) * 640 + 60; if (dir > 0 ? c - span > H : c + span < 0) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (TQWarnPass && bt < bf + 0.5)
			{
				double k = TQk(bt, bf - tl, bf) * (1 - TQk(bt, bf, bf + 0.5)), ye = dir > 0 ? 10 : H - 10;
				g.strokeStyle = TQW(0.25 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				g.moveTo(0, ye); g.lineTo(Math.Max(0, gx - gw / 2), ye); g.moveTo(Math.Min(W, gx + gw / 2), ye); g.lineTo(W, ye); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = TQG(0.5 + 0.5 * k); g.lineWidth = 4; double yy = dir > 0 ? 4 : H - 4, d2 = dir > 0 ? 26 : -26;
				g.beginPath(); g.moveTo(gx - gw / 2, yy); g.lineTo(gx - gw / 2, yy + d2); g.moveTo(gx + gw / 2, yy); g.lineTo(gx + gw / 2, yy + d2); g.stroke();
			}
			if (!TQWarnPass && bt >= bf) { g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; each(t, (x, y) => { if (y > -30 && y < H + 30) TQPetal(x, y, sz, q); }); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; bool hit = false;
			each(t, (x, y) => { if (!hit && Math.Abs(p.y - y) < sz + p.r && TQPetalHit(p, x, y, sz, q)) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 7. the light beams (4:43.9) ----------
	// On each kick kb[i] the heart shines lines straight lines through itself (lines 2 = a cross), at a0 + i * step degrees. Each shows
	// dashed from kb - tl, is pink and hurts for hot beats, then fades white. th thick.
	public static Hz TQBeams(double[] kb, double a0, double step, int lines, double hot, double tl, double th)
	{
		var h = new Hz { kind = "TQBeams" }; double last = kb.Length > 0 ? kb[kb.Length - 1] : 0;
		h.update = (t, dt) => { if (BT(t) > last + hot + 1.5) h.dead = true; };
		void each(int i, Action<double, double, double, double> f)
		{
			for (int l = 0; l < lines; l++) { double q = TQRad(a0 + i * step + l * 180.0 / lines), dx = JS.cos(q), dy = JS.sin(q); f(W / 2 - dx * 900, H / 2 - dy * 900, W / 2 + dx * 900, H / 2 + dy * 900); }
		}
		h.draw = t =>
		{
			double bt = BT(t); g.save(); g.lineCap = "butt";
			for (int i = 0; i < kb.Length; i++)
			{
				double k = kb[i]; if (bt < k - tl || bt > k + hot + 1) continue;
				if (bt < k) { if (TQWarnPass) { double q = TQk(bt, k - tl, k); each(i, (x0, y0, x1, y1) => TQDash(x0, y0, x1, y1, TQW(0.25 + 0.5 * q), 3)); } continue; }
				if (bt < k + hot)
				{
					if (TQWarnPass) continue;
					each(i, (x0, y0, x1, y1) =>
					{
						g.shadowColor = PINK; g.shadowBlur = 20; g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
						g.shadowBlur = 0; g.strokeStyle = "rgba(255,104,150,1)"; g.lineWidth = th * 0.3; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
					});
				}
				else if (TQWarnPass) { double q = 1 - TQk(bt, k + hot, k + hot + 1); each(i, (x0, y0, x1, y1) => { g.strokeStyle = TQW(0.4 * q); g.lineWidth = 3; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); }); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); bool hit = false;
			for (int i = 0; i < kb.Length && !hit; i++) { double k = kb[i]; if (bt < k || bt >= k + hot) continue; each(i, (x0, y0, x1, y1) => { if (!hit && TQSeg(p.x, p.y, x0, y0, x1, y1) < th / 2 + p.r - 3) hit = true; }); }
			return hit;
		};
		return h;
	}

	// ---------- 8. the spiral of small hearts (4:58.5) ----------
	// On each hat hb[i] the heart lets go arms small hearts (sz) at a0 + rot * i + k * 360 / arms degrees; they fly out at sp px/s.
	// The next ones' ways show as short dashed lines from the heart from hb - tl.
	public static Hz TQSpiral(double[] hb, int arms, double a0, double rot, double sp, double sz, double tl)
	{
		var h = new Hz { kind = "TQSpiral" }; double last = hb.Length > 0 ? hb[hb.Length - 1] : 0; const double R0 = 70;
		double ang(int i, int k) => TQRad(a0 + rot * i + k * 360.0 / arms);
		h.update = (t, dt) => { if (BT(t) > last + 0.1 && R0 + (t - B(last)) * sp > 1000) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			for (int i = 0; i < hb.Length; i++)
			{
				double b = hb[i]; if (bt < b - tl) break; double tf = B(b);
				if (bt < b)
				{
					double q = TQk(bt, b - tl, b); if (!TQWarnPass) continue;
					for (int k = 0; k < arms; k++) { double a = ang(i, k), dx = JS.cos(a), dy = JS.sin(a); TQDash(W / 2 + dx * 76, H / 2 + dy * 76, W / 2 + dx * 210, H / 2 + dy * 210, TQW(0.2 + 0.45 * q), 2); }
					continue;
				}
				double d = R0 + (t - tf) * sp; if (d > 1000 || TQWarnPass) continue;
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				for (int k = 0; k < arms; k++) { double a = ang(i, k); TQHeartFill(W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d, sz, a + Math.PI / 2); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), pd = hypot(p.x - W / 2, p.y - H / 2);
			for (int i = 0; i < hb.Length; i++)
			{
				double b = hb[i]; if (bt < b) break; double d = R0 + (t - B(b)) * sp; if (Math.Abs(pd - d) > sz + p.r) continue;
				for (int k = 0; k < arms; k++) { double a = ang(i, k); if (TQDotHit(p, W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d, sz, a + Math.PI / 2)) return true; }
			}
			return false;
		};
		return h;
	}

	// ---------- 9. the last heart (5:16.7) ----------
	// A big heart outline closes in round the gold heart: its scale goes from s0 at b0 to s1 at b1 (eased), its gap (gw turns) turns
	// spin turns a beat from gu0. Dashed at s0 from b0 - tl. It is pink and hurts from b0 to b1, then fades.
	public static Hz TQClose(double b0, double b1, double s0, double s1, double gu0, double spin, double gw, double th, double tl)
	{
		var h = new Hz { kind = "TQClose" };
		double sc(double bt) => s0 + (s1 - s0) * TQEase((bt - b0) / (b1 - b0));
		double gu(double bt) => gu0 + spin * (bt - b0);
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - tl || bt > b1 + 0.6) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < b0) { if (TQWarnPass) { g.strokeStyle = TQW(0.25 + 0.5 * TQk(bt, b0 - tl, b0)); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); TQHeartStroke(W / 2, H / 2, Math.Min(s0, 600), gu(b0), gw); g.setLineDash(new double[0]); } }
			else if (bt < b1) { if (!TQWarnPass) { g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = th; TQHeartStroke(W / 2, H / 2, sc(bt), gu(bt), gw); } }
			else if (TQWarnPass) { g.strokeStyle = TQG(0.8 * (1 - TQk(bt, b1, b1 + 0.6))); g.lineWidth = 4; TQHeartStroke(W / 2, H / 2, s1, gu(b1), gw); }
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); return bt >= b0 && bt < b1 && TQHeartHit(p, W / 2, H / 2, sc(bt), gu(bt), gw, th); };
		return h;
	}
	// the end: the song's name in gold under the heart
	public static Hz TQEnd(double b0, double b1)
	{
		var h = new Hz { kind = "TQEnd" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = TQk(bt, b0, b0 + 0.6), a = Math.Min(k, 1 - TQk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(84 - 12 * k)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#f3c969"; g.shadowBlur = 30; g.fillStyle = "#f8e2a6"; g.fillText("يا طيب القلب", W / 2, H - 120); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
}
