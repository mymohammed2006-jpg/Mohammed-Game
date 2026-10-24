using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L20 "New Game", part 2: playing (Space Invaders and its saucer, the glitch, the rhythm game, the screen closing in). See L20.cs
public static partial class G
{
	// an invader (r = half its width), legs open or shut on the beat
	static void L20InvDraw(double x, double y, double r, string col, bool open)
	{
		g.fillStyle = col; double u = r / 6;
		g.fillRect(x - 4 * u, y - 3 * u, 8 * u, 4 * u); g.fillRect(x - 6 * u, y - u, 12 * u, 2 * u); g.fillRect(x - 2 * u, y - 5 * u, 4 * u, 2 * u);
		g.fillRect(x - 6 * u, y - 4 * u, 2 * u, 3 * u); g.fillRect(x + 4 * u, y - 4 * u, 2 * u, 3 * u);
		if (open) { g.fillRect(x - 6 * u, y + u, 2 * u, 3 * u); g.fillRect(x + 4 * u, y + u, 2 * u, 3 * u); }
		else { g.fillRect(x - 4 * u, y + u, 2 * u, 3 * u); g.fillRect(x + 2 * u, y + u, 2 * u, 3 * u); }
		g.fillStyle = "rgba(40,0,14,0.8)"; g.fillRect(x - 3 * u, y - 2 * u, 2 * u, 2 * u); g.fillRect(x + u, y - 2 * u, 2 * u, 2 * u);
	}

	// its hurt area: the same blocks as the drawing (not its box, so its gaps don't hurt)
	static bool L20InvHit(Player p, double x, double y, double r, bool open)
	{
		double u = r / 6;
		if (L20In(p, x - 4 * u, y - 3 * u, x + 4 * u, y + u) || L20In(p, x - 6 * u, y - u, x + 6 * u, y + u) || L20In(p, x - 2 * u, y - 5 * u, x + 2 * u, y - 3 * u)) return true;
		if (L20In(p, x - 6 * u, y - 4 * u, x - 4 * u, y - u) || L20In(p, x + 4 * u, y - 4 * u, x + 6 * u, y - u)) return true;
		return open ? L20In(p, x - 6 * u, y + u, x - 4 * u, y + 4 * u) || L20In(p, x + 4 * u, y + u, x + 6 * u, y + 4 * u)
			: L20In(p, x - 4 * u, y + u, x - 2 * u, y + 4 * u) || L20In(p, x + 2 * u, y + u, x + 4 * u, y + 4 * u);
	}

	// the invaders: cols x rows, dx dy apart, the army's corner at (ox[k],oy[k]) from beat sb[k] (a quick slide). Lasers: at beat lb[i] the
	// lowest invader of column lc[i] fires down (lw wide, lh beats); its column shows wn beats before. Dive: the lower two rows leave at
	// dv[0], the upper two at dv[1], along curves out and down (shown dashed a beat before). Grey from tw, kills from ta (beats)
	public static Hz L20Invaders(int cols, int rows, double dx, double dy, double[] sb, double[] ox, double[] oy, double[] lb, double[] lc, double lw, double lh, double wn, double[] dv, double ddur, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L20Invaders" }; const double IR = 30;
		(double x, double y) army(double bt)
		{
			int k = 0; for (int i = 0; i < sb.Length; i++) if (bt >= sb[i]) k = i; double u = bt - sb[k];
			if (k == 0 || u >= 0.08) return (ox[k], oy[k]); double e = u / 0.08; return (ox[k - 1] + (ox[k] - ox[k - 1]) * e, oy[k - 1] + (oy[k] - oy[k - 1]) * e);
		}
		double diveAt(int r) => dv.Length < 2 ? 1e9 : r >= rows / 2 ? dv[0] : dv[1];
		// where invader (c,r) is: in the army, or on its dive curve; false when it is gone
		bool inv(int c, int r, double bt, out double x, out double y)
		{
			double d0 = diveAt(r);
			if (bt < d0) { var o = army(bt); x = o.x + c * dx; y = o.y + r * dy; return true; }
			var s = army(d0); double sx = s.x + c * dx, sy = s.y + r * dy, side = c < cols / 2.0 ? -1 : 1, u = (bt - d0) / ddur;
			if (u >= 1) { x = y = 0; return false; }
			double cxp = sx + side * 260, cyp = sy + 420, ex = sx + side * 140 - side * 380, ey = H + 120; double m = 1 - u;
			x = m * m * sx + 2 * m * u * cxp + u * u * ex; y = m * m * sy + 2 * m * u * cyp + u * u * ey; return true;
		}
		(double x, double y) shooter(int c, double bt) { var o = army(bt); return (o.x + c * dx, o.y + (rows - 1) * dy); }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.5)); bool on = bt >= ta;
			g.save(); g.globalAlpha = a;
			// the lasers' columns before they fire, then the lasers
			for (int i = 0; i < lb.Length; i++)
			{
				if (bt < lb[i] - wn || bt > lb[i] + lh) continue; var s = shooter((int)lc[i], lb[i]); double y0 = s.y + IR * 0.7;
				if (bt < lb[i]) { double k = L20k(bt, lb[i] - wn, lb[i]); g.fillStyle = L20P(0.05 + 0.2 * k); g.fillRect(s.x - lw / 2, y0, lw, H - y0); L20Dash(s.x, y0, s.x, H, L20G(0.35 + 0.55 * k), 2 + 2 * k); continue; }
				double w2 = lw * Math.Min(1, (bt - lb[i]) / 0.06 + 0.4); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = bt - lb[i] < 0.06 ? L20HOT : PINK; g.fillRect(s.x - w2 / 2, y0, w2, H - y0); g.shadowBlur = 0;
				g.fillStyle = "rgba(255,200,220,0.55)"; g.fillRect(s.x - w2 / 6, y0, w2 / 3, H - y0);
			}
			// the dive curves, a beat before
			for (int r = 0; r < rows; r++)
			{
				double d0 = diveAt(r); if (bt < d0 - 1 || bt >= d0) continue; double k = L20k(bt, d0 - 1, d0);
				g.strokeStyle = $"rgba(255,255,255,{str(0.2 + 0.4 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 });
				for (int c = 0; c < cols; c++) { g.beginPath(); for (int j = 0; j <= 16; j++) { inv(c, r, d0 + ddur * j / 16 * 0.999, out double x, out double y); if (j == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
				g.setLineDash(new double[0]);
			}
			bool open = ((int)Math.Floor(bt)) % 2 == 0;
			if (on) { g.shadowColor = PINK; g.shadowBlur = 12; }
			for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) if (inv(c, r, bt, out double x, out double y)) L20InvDraw(x, y, IR, on ? PINK : L20P(0.15 + 0.3 * L20k(bt, tw, ta)), open);
			g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false;
			bool open = ((int)Math.Floor(bt)) % 2 == 0;
			for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) if (inv(c, r, bt, out double x, out double y) && L20InvHit(p, x, y, IR, open)) return true;
			for (int i = 0; i < lb.Length; i++) { if (bt < lb[i] || bt > lb[i] + lh) continue; var s = shooter((int)lc[i], lb[i]); double w2 = lw * Math.Min(1, (bt - lb[i]) / 0.06 + 0.4); if (L20In(p, s.x - w2 / 2, s.y + IR * 0.7, s.x + w2 / 2, H + 50)) return true; }
			return false;
		};
		return h;
	}

	// the saucer crosses the top (y) from b0 to b1 (dir 1: from the left); at the beats bd it drops a bomb that falls at vb px/s
	// (its line shows wn beats before)
	public static Hz L20Ufo(double y, double dir, double[] bd, double vb, double wn, double b0, double b1)
	{
		var h = new Hz { kind = "L20Ufo" };
		double ux(double bt) { double k = (bt - b0) / (b1 - b0); return dir > 0 ? -90 + k * (W + 180) : W + 90 - k * (W + 180); }
		h.update = (t, dt) => { if (BT(t) > b1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - wn) return;
			for (int i = 0; i < bd.Length; i++)
			{
				double x = ux(bd[i]);
				if (bt >= bd[i] - wn && bt < bd[i]) { double k = L20k(bt, bd[i] - wn, bd[i]); L20Dash(x, y + 20, x, H, L20G(0.3 + 0.55 * k), 2); g.fillStyle = L20P(0.06 + 0.16 * k); g.fillRect(x - 14, y + 20, 28, H - y - 20); }
				else if (bt >= bd[i]) { double by = y + 20 + vb * (t - B(bd[i])); if (by > H + 30) continue; g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.moveTo(x, by - 20); g.lineTo(x + 14, by); g.lineTo(x, by + 20); g.lineTo(x - 14, by); g.closePath(); g.fill(); g.shadowBlur = 0; }
			}
			if (bt >= b0 && bt < b1)
			{
				double x = ux(bt); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK;
				g.beginPath(); g.ellipse(x, y, 70, 18, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(x, y - 14, 30, 16, 0, Math.PI, TAU); g.fill(); g.shadowBlur = 0;
				g.fillStyle = "rgba(60,0,20,0.6)"; for (int i = -2; i <= 2; i++) circle(x + i * 24, y + 2, 5);
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			if (bt >= b0 && bt < b1) { double x = ux(bt), ex = (p.x - x) / (70 + p.r - 2), ey = (p.y - y) / (18 + p.r - 2); if (ex * ex + ey * ey < 1) return true; }
			for (int i = 0; i < bd.Length; i++) { if (bt < bd[i]) continue; double x = ux(bd[i]), by = y + 20 + vb * (t - B(bd[i])); if (Math.Abs(p.x - x) + Math.Abs(p.y - by) * 0.7 < 14 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the glitch: the screen in n slices; every slice is pink but for its hole (hw wide), which slides at v[k] px/s from x0[k] and bounces
	// at the edges. Grey with the holes outlined from tw, kills ta .. te (beats)
	public static Hz L20Glitch(int n, double hw, double[] x0, double[] v, double tw, double ta, double te)
	{
		double sh = H / n; var h = new Hz { kind = "L20Glitch" };
		double hx(int k, double t) { double lo = hw / 2 + 10, hi = W - hw / 2 - 10, span = hi - lo, u = x0[k] - lo + v[k] * (t - B(ta)); double m = ((u % (2 * span)) + 2 * span) % (2 * span); return lo + (m < span ? m : 2 * span - m); }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw || bt > te + 0.3) return; bool on = bt >= ta && bt < te; double a = 1 - L20k(bt, te, te + 0.3), tt = Math.Max(t, B(ta));
			g.save(); g.globalAlpha = a;
			for (int k = 0; k < n; k++)
			{
				double y = k * sh, x = hx(k, tt), j = on ? (JS.sin(t * 50 + k * 2) > 0.6 ? 6 : 0) : 0;
				if (!on) { double q = L20k(bt, tw, ta); g.fillStyle = L20P(0.05 + 0.2 * q); g.fillRect(0, y + 2, x - hw / 2, sh - 4); g.fillRect(x + hw / 2, y + 2, W - x - hw / 2, sh - 4); }
				else { g.fillStyle = bt - ta < 0.06 ? L20HOT : PINK; g.fillRect(0, y, x - hw / 2, sh); g.fillRect(x + hw / 2, y, W - x - hw / 2, sh); g.fillStyle = "rgba(60,0,20,0.35)"; for (double s = y + 6 + j; s < y + sh; s += 14) { g.fillRect(0, s, x - hw / 2, 3); g.fillRect(x + hw / 2, s, W - x - hw / 2, 3); } }
				g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 2; g.strokeRect(x - hw / 2, y + 2, hw, sh - 4);
				double ar = v[k] > 0 ? 1 : -1; g.beginPath(); g.moveTo(x + ar * 18, y + sh / 2); g.lineTo(x - ar * 4, y + sh / 2 - 12); g.lineTo(x - ar * 4, y + sh / 2 + 12); g.closePath(); g.fillStyle = "rgba(255,255,255,0.35)"; g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false;
			int k0 = (int)clamp(Math.Floor((p.y - p.r + 2) / sh), 0, n - 1), k1 = (int)clamp(Math.Floor((p.y + p.r - 2) / sh), 0, n - 1);
			for (int k = k0; k <= k1; k++) { double x = hx(k, t); if (p.x - p.r + 2 < x - hw / 2 || p.x + p.r - 2 > x + hw / 2) return true; }
			return false;
		};
		return h;
	}

	// the rhythm game: n lanes lw wide (lane 0's middle at cx0), notes fall vpb px a beat; note i reaches the hit line (hitY) at beat nb[i]
	// in lane nl[i], and a long note is nlen[i] beats long. Notes show from clipY down and fall on past the line. The highway leans
	// (tilt degrees, from tb0 to tb1, swinging every 4 beats) round (rx,ry), and lanes swap places: lanes sa[i] and sb[i] cross from beat sw[i]
	// over swd beats. Kills ta .. te (beats)
	public static Hz L20Lanes(int n, double lw, double cx0, double hitY, double vpb, double clipY, double[] nb, double[] nl, double[] nlen, double tilt, double tb0, double tb1, double rx, double ry, double[] sw, double[] sa, double[] sbb, double swd, double tw, double ta, double te, bool line)
	{
		var h = new Hz { kind = "L20Lanes" };
		double laneX(int i, double bt)
		{
			var x = new double[n]; for (int j = 0; j < n; j++) x[j] = cx0 + j * lw;
			for (int s = 0; s < sw.Length; s++)
			{
				if (bt < sw[s]) break; int A = (int)sa[s], Bq = (int)sbb[s]; double k = L20Ease((bt - sw[s]) / swd), xa = x[A], xb = x[Bq];
				x[A] = xa + (xb - xa) * k; x[Bq] = xb + (xa - xb) * k;
			}
			return x[i];
		}
		double ang(double bt) => tilt == 0 || bt < tb0 || bt > tb1 ? 0 : tilt * Math.PI / 180 * JS.sin(Math.PI * (bt - tb0) / 2) * Math.Min(1, Math.Min(bt - tb0, tb1 - bt));
		// note i's top and bottom
		void span(int i, double bt, out double y0, out double y1) { double head = hitY - (nb[i] - bt) * vpb; y1 = head + 20; y0 = head - 20 - nlen[i] * vpb; }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.5));
			g.save(); g.globalAlpha = a; double an = ang(bt); g.translate(rx, ry); g.rotate(an); g.translate(-rx, -ry);
			// the highway: lane edges, the hit line and its keys (harmless)
			g.strokeStyle = L20G(0.16); g.lineWidth = 2; g.beginPath();
			for (int j = 0; j < n; j++) { double x = laneX(j, bt); g.moveTo(x - lw / 2, clipY); g.lineTo(x - lw / 2, 1200); g.moveTo(x + lw / 2, clipY); g.lineTo(x + lw / 2, 1200); }
			g.stroke();
			if (line)
			{
				g.fillStyle = "rgba(255,255,255,0.25)"; g.fillRect(-300, hitY - 2, W + 600, 4);
				for (int j = 0; j < n; j++) { double x = laneX(j, bt); g.strokeStyle = "rgba(255,255,255,0.3)"; g.lineWidth = 3; g.beginPath(); g.ellipse(x, hitY, lw * 0.32, 16, 0, 0, TAU); g.stroke(); }
			}
			for (int i = 0; i < nb.Length; i++)
			{
				span(i, bt, out double y0, out double y1); if (y1 < clipY || y0 > 1250) continue; y0 = Math.Max(y0, clipY);
				double x = laneX((int)nl[i], bt); bool on = bt >= ta;
				if (!on) { g.fillStyle = L20P(0.2); g.fillRect(x - lw / 2 + 1, y0, lw - 2, y1 - y0); continue; }
				g.shadowColor = PINK; g.shadowBlur = 14; L20Cell(x - lw / 2 + 1, y0, lw - 2, y1 - y0, PINK); g.shadowBlur = 0;
				if (nlen[i] > 0) { g.fillStyle = "rgba(60,0,20,0.35)"; g.fillRect(x - 10, y0 + 6, 20, y1 - y0 - 12); }
				// the note passing the line: a white ring (harmless)
				double u = bt - nb[i]; if (line && u >= 0 && u < 0.4) { g.strokeStyle = $"rgba(255,255,255,{str(0.7 * (1 - u / 0.4))})"; g.lineWidth = 4; g.beginPath(); g.ellipse(x, hitY, lw * (0.32 + u), 16 + 40 * u, 0, 0, TAU); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false; double an = -ang(bt), c = JS.cos(an), s = JS.sin(an), dx = p.x - rx, dy = p.y - ry;
			var q = new Player { x = rx + dx * c - dy * s, y = ry + dx * s + dy * c, r = p.r };
			for (int i = 0; i < nb.Length; i++)
			{
				span(i, bt, out double y0, out double y1); if (y1 < clipY || y0 > 1250) continue; y0 = Math.Max(y0, clipY);
				double x = laneX((int)nl[i], bt); if (L20In(q, x - lw / 2 + 1, y0, x + lw / 2 - 1, y1)) return true;
			}
			return false;
		};
		return h;
	}

	// the screen closes in: pink walls from the 4 edges close (t0 .. t1, beats) to a box bw x bh in the middle (shown dashed from tw), and
	// at t2 they burst away (harmless from then)
	public static Hz L20Close(double bw, double bh, double tw, double t0, double t1, double t2)
	{
		var h = new Hz { kind = "L20Close" };
		void box(double bt, out double l, out double r, out double tp, out double bo)
		{
			double k = bt < t2 ? L20k(bt, t0, t1) : 1 + 6 * (bt - t2); l = (W / 2 - bw / 2) * Math.Min(k, 1) - (k > 1 ? (k - 1) * 120 : 0); r = W - l;
			tp = (H / 2 - bh / 2) * Math.Min(k, 1) - (k > 1 ? (k - 1) * 70 : 0); bo = H - tp;
		}
		h.update = (t, dt) => { if (BT(t) > t2 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = bt < t2 ? 1 : Math.Max(0, 1 - (bt - t2) * 2);
			g.save(); g.globalAlpha = a;
			if (bt < t2) { double k = L20k(bt, tw, t0); g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 }); g.strokeRect(W / 2 - bw / 2, H / 2 - bh / 2, bw, bh); g.setLineDash(new double[0]); }
			if (bt >= t0)
			{
				box(bt, out double l, out double r, out double tp, out double bo); g.fillStyle = bt < t2 ? PINK : L20G(0.5); if (bt < t2) { g.shadowColor = PINK; g.shadowBlur = 16; }
				g.fillRect(0, 0, Math.Max(0, l), H); g.fillRect(r, 0, Math.Max(0, W - r), H); g.fillRect(l, 0, r - l, Math.Max(0, tp)); g.fillRect(l, bo, r - l, Math.Max(0, H - bo)); g.shadowBlur = 0;
			}
			else if (bt >= tw) { double k = L20k(bt, tw, t0); g.fillStyle = L20P(0.08 + 0.2 * k); g.fillRect(0, 0, W, 10); g.fillRect(0, H - 10, W, 10); g.fillRect(0, 0, 10, H); g.fillRect(W - 10, 0, 10, H); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < t0 || bt >= t2) return false; box(bt, out double l, out double r, out double tp, out double bo);
			return p.x - p.r + 2 < l || p.x + p.r - 2 > r || p.y - p.r + 2 < tp || p.y + p.r - 2 > bo;
		};
		return h;
	}
}
