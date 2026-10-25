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
	// With cs: it closes in steps, one at each beat of cs (the song's last hits), the next edge dashed before
	public static Hz L20Close(double bw, double bh, double tw, double t0, double t1, double t2, double[] cs = null)
	{
		var h = new Hz { kind = "L20Close" }; bool st = cs != null && cs.Length > 0;
		double prog(double bt)
		{
			if (!st) return L20k(bt, t0, t1);
			int n = cs.Length, i = 0; while (i < n && bt >= cs[i]) i++; if (i == 0) return 0; double e = L20Ease(Math.Min(1, (bt - cs[i - 1]) / 0.1)); return (i - 1 + e) / n;
		}
		void box(double bt, out double l, out double r, out double tp, out double bo)
		{
			double k = bt < t2 ? prog(bt) : 1 + 6 * (bt - t2); l = (W / 2 - bw / 2) * Math.Min(k, 1) - (k > 1 ? (k - 1) * 120 : 0); r = W - l;
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
				if (st && bt < t2) { int i = 0; while (i < cs.Length && bt >= cs[i]) i++; if (i < cs.Length && cs[i] - bt < 1) { double k2 = (double)(i + 1) / cs.Length, l2 = (W / 2 - bw / 2) * k2, t2_ = (H / 2 - bh / 2) * k2; g.strokeStyle = "rgba(255,255,255,0.75)"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(l2, t2_, W - 2 * l2, H - 2 * t2_); g.setLineDash(new double[0]); } }
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
