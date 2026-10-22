using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// H1 "Clockwork Squares" (Black Ops Zombies), Hardcore level 1: the cursed clock made of iron squares. Everything moves on the tick.
// part 1 the machine starts (squares that jump a cell each tick, the sliding grid, countdown bombs with cross blasts, the press, the square wave),
// part 2 the clock strikes (the clock face turning a quarter at once, the domino wave, the knights, the conveyor lanes),
// part 3 midnight (12 chimes close the box while its lines slide, the falling stack, then the whole machine at once).
// Every hazard is a pure function of time once it exists (aimed lines pick the square's row or column when they fire), so hit(p, t) is
// right for any later t. The data is data/dodge_H1.json (made by tools/hardcore1/gen.py).
public static partial class G
{
	static double H1k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string H1P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string H1G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double H1Ease(double k) { k = clamp(k, 0, 1); return k < 0.5 ? 4 * k * k * k : 1 - JS.pow(-2 * k + 2, 3) / 2; }
	static bool H1In(Player p, double x0, double y0, double x1, double y1) => p.x + p.r - 2 > x0 && p.x - p.r + 2 < x1 && p.y + p.r - 2 > y0 && p.y - p.r + 2 < y1;

	// a grid: cols x rows cells of cw x ch from (x0, y0)
	public struct H1Grid
	{
		public double x0, y0, cw, ch; public int cols, rows;
		public double X(double c) => x0 + c * cw;
		public double Y(double r) => y0 + r * ch;
		public double W => cols * cw;
		public double H => rows * ch;
		public int Col(double x) => (int)Math.Floor((x - x0) / cw);
		public int Row(double y) => (int)Math.Floor((y - y0) / ch);
	}
	static H1Grid H1G8 => new H1Grid { x0 = 0, y0 = 0, cw = 160, ch = 144, cols = 8, rows = 5 };
	static H1Grid H1Gof(DodgeEv e) => !e.has("gx") ? H1G8 : new H1Grid { x0 = e.num("gx"), y0 = e.num("gy"), cw = e.num("gw"), ch = e.num("gh"), cols = e.i("gc"), rows = e.i("gr") };

	// ---------- the look of things ----------
	// an iron square (x, y = its top left): k 0 = grey steel, 1 = deadly pink; rivets in the corners and a cross-cut in the middle
	static void H1Sq(double x, double y, double w, double h, double k, bool white = false, double alpha = 1)
	{
		if (w < 2 || h < 2) return;
		g.save(); g.globalAlpha = alpha;
		g.fillStyle = white ? "#fff" : k >= 1 ? PINK : $"rgba({str(70 + 185 * k)},{str(64 - 22 * k)},{str(78 + 31 * k)},1)";
		g.fillRect(x, y, w, h);
		if (!white)
		{
			double m = Math.Min(w, h) * 0.16;
			g.strokeStyle = k >= 1 ? "rgba(70,0,20,0.55)" : "rgba(0,0,0,0.45)"; g.lineWidth = 3; g.strokeRect(x + m, y + m, w - 2 * m, h - 2 * m);
			g.fillStyle = k >= 1 ? "rgba(70,0,20,0.6)" : "rgba(0,0,0,0.5)";
			double rr = Math.Max(2, Math.Min(w, h) * 0.05);
			circle(x + m * 0.5, y + m * 0.5, rr); circle(x + w - m * 0.5, y + m * 0.5, rr); circle(x + m * 0.5, y + h - m * 0.5, rr); circle(x + w - m * 0.5, y + h - m * 0.5, rr);
		}
		g.restore();
	}
	// a grey dashed outline: where something will be
	static void H1Ghost(double x, double y, double w, double h, double a, double lw = 3)
	{
		g.strokeStyle = H1G(a); g.lineWidth = lw; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(x + 3, y + 3, w - 6, h - 6); g.setLineDash(new double[0]);
		g.fillStyle = H1G(a * 0.18); g.fillRect(x + 3, y + 3, w - 6, h - 6);
	}
	static void H1Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// chevrons along a band, pointing dir (axis 0: a row pointing right for dir 1; axis 1: a column pointing down for dir 1)
	static void H1Chevrons(double x0, double y0, double w, double h, int axis, int dir, double a, double ph)
	{
		g.strokeStyle = H1G(a); g.lineWidth = 4; g.beginPath();
		if (axis == 0)
		{
			double s = h * 0.28, step = 70;
			for (double x = x0 + ((ph * step) % step) - step; x < x0 + w + step; x += step)
			{
				if (x < x0 + 10 || x > x0 + w - 10) continue; double cy = y0 + h / 2;
				g.moveTo(x - dir * s * 0.6, cy - s); g.lineTo(x + dir * s * 0.4, cy); g.lineTo(x - dir * s * 0.6, cy + s);
			}
		}
		else
		{
			double s = w * 0.28, step = 70;
			for (double y = y0 + ((ph * step) % step) - step; y < y0 + h + step; y += step)
			{
				if (y < y0 + 10 || y > y0 + h - 10) continue; double cx = x0 + w / 2;
				g.moveTo(cx - s, y - dir * s * 0.6); g.lineTo(cx, y + dir * s * 0.4); g.lineTo(cx + s, y - dir * s * 0.6);
			}
		}
		g.stroke();
	}
	// a gear (decor): n teeth, radius R, turned a
	static void H1Gear(double x, double y, double R, int n, double a, string col)
	{
		g.fillStyle = col; g.beginPath();
		for (int i = 0; i < n * 2; i++)
		{
			double a0 = a + i * Math.PI / n, a1 = a + (i + 1) * Math.PI / n, r = i % 2 == 0 ? R : R * 0.84;
			if (i == 0) g.moveTo(x + JS.cos(a0) * r, y + JS.sin(a0) * r); else g.lineTo(x + JS.cos(a0) * r, y + JS.sin(a0) * r);
			g.lineTo(x + JS.cos(a1) * r, y + JS.sin(a1) * r);
		}
		g.closePath(); g.fill();
		g.fillStyle = "rgba(8,6,10,0.9)"; circle(x, y, R * 0.3);
	}

	// ---------- part 1: the machine starts ----------
	// a square that jumps one place every st beats: at beat b0 + k st it is at (xs[k], ys[k]) (the jump takes the last 0.12 of the step
	// before). Its next place is a dashed ghost with a line to it. Grey from tw, deadly from ta to te
	public static Hz H1Jump(double[] xs, double[] ys, double b0, double st, double sz, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H1Jump" }; int n = xs.Length;
		(double x, double y) at(double bt)
		{
			double f = (bt - b0) / st; int k = (int)Math.Floor(f); if (k < 0) return (xs[0], ys[0]); if (k >= n - 1) return (xs[n - 1], ys[n - 1]);
			double u = f - k; if (u < 0.88) return (xs[k], ys[k]); double q = H1Ease((u - 0.88) / 0.12);
			return (xs[k] + (xs[k + 1] - xs[k]) * q, ys[k] + (ys[k + 1] - ys[k]) * q);
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = Math.Min(H1k(t, tw, tw + 0.4), 1 - H1k(t, te, te + 0.3)); var c = at(bt);
			int k = (int)clamp(Math.Floor((bt - b0) / st) + 1, 0, n - 1);
			if (t < te && k < n && (xs[k] != c.x || ys[k] != c.y))
			{
				g.save(); g.globalAlpha = a; H1Ghost(xs[k] - sz / 2, ys[k] - sz / 2, sz, sz, 0.75); H1Dash(c.x, c.y, xs[k], ys[k], H1G(0.5), 3); g.restore();
			}
			bool on = t >= ta && t < te; double fl = (bt - b0) / st - Math.Floor((bt - b0) / st);
			H1Sq(c.x - sz / 2, c.y - sz / 2, sz, sz, on ? 1 : 0.15 + 0.5 * H1k(t, tw, ta), on && fl < 0.06 && t - ta > 0.05, a);
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; var c = at(BT(t)); return H1In(p, c.x - sz / 2, c.y - sz / 2, c.x + sz / 2, c.y + sz / 2); };
		return h;
	}

	// the grid's tiles (harmless): the board the lines slide on
	public static Hz H1Board(H1Grid G0, double t0, double t1)
	{
		var h = new Hz { kind = "H1Board" };
		h.update = (t, dt) => { if (t > t1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(H1k(t, t0, t0 + 0.5), 1 - H1k(t, t1, t1 + 0.4)); if (a <= 0) return;
			g.save(); g.globalAlpha = a * 0.55;
			for (int r = 0; r < G0.rows; r++) for (int c = 0; c < G0.cols; c++)
			{
				double x = G0.X(c), y = G0.Y(r); g.fillStyle = (r + c) % 2 == 0 ? "rgba(46,40,52,0.9)" : "rgba(38,33,44,0.9)"; g.fillRect(x + 3, y + 3, G0.cw - 6, G0.ch - 6);
				g.strokeStyle = "rgba(0,0,0,0.6)"; g.lineWidth = 2; g.strokeRect(x + 3, y + 3, G0.cw - 6, G0.ch - 6);
			}
			g.restore();
		};
		return h;
	}

	// a line of the grid (axis 0: row idx, 1: column idx) slides one cell in dir over dur s from ta; all of it kills while it slides.
	// Grey chevrons from tw. aim: the line is the square's row or column when the event fires
	public static Hz H1Slide(H1Grid G0, int axis, int idx, int dir, double tw, double ta, double dur)
	{
		var h = new Hz { kind = "H1Slide" };
		double bx0, by0, bw, bh;
		if (axis == 0) { bx0 = G0.x0; by0 = G0.Y(idx); bw = G0.W; bh = G0.ch; } else { bx0 = G0.X(idx); by0 = G0.y0; bw = G0.cw; bh = G0.H; }
		h.update = (t, dt) => { if (t > ta + dur + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = H1k(t, tw, ta); g.fillStyle = H1P(0.04 + 0.16 * k); g.fillRect(bx0, by0, bw, bh);
				g.strokeStyle = H1G(0.4 + 0.5 * k); g.lineWidth = 2; g.strokeRect(bx0 + 2, by0 + 2, bw - 4, bh - 4);
				H1Chevrons(bx0, by0, bw, bh, axis, dir, 0.35 + 0.55 * k, (t - tw) * 2 * dir); return;
			}
			double u = t - ta; if (u > dur + 0.25) return; double q = H1Ease(u / dur), al = u < dur ? 1 : 1 - (u - dur) / 0.25;
			g.save(); g.globalAlpha = al; g.beginPath(); g.rect(bx0, by0, bw, bh); g.clip();
			int n = axis == 0 ? G0.cols : G0.rows; double cs = axis == 0 ? G0.cw : G0.ch;
			for (int i = -1; i <= n; i++)
			{
				double o = (i + dir * q) * cs;
				if (axis == 0) H1Sq(bx0 + o + 4, by0 + 4, G0.cw - 8, G0.ch - 8, 1, u < 0.05); else H1Sq(bx0 + 4, by0 + o + 4, G0.cw - 8, G0.ch - 8, 1, u < 0.05);
			}
			g.restore();
		};
		h.hit = (p, t) => t >= ta && t < ta + dur && H1In(p, bx0, by0, bx0 + bw, by0 + bh);
		return h;
	}

	// a countdown bomb lands on cell (c, r) at tl (its shadow grows from tl - 0.5 beat) and shows the beats left as pips; at ta its row and
	// its column blast for hold s. The bomb itself kills from tl
	public static Hz H1Bomb(H1Grid G0, int c, int r, double tl, double ta, double hold)
	{
		var h = new Hz { kind = "H1Bomb" }; double x = G0.X(c), y = G0.Y(r), tw = tl - SPB * 0.5; bool b0 = false;
		h.update = (t, dt) => { if (!b0 && t >= ta) { b0 = true; shake = Math.Max(shake, 4); burst(x + G0.cw / 2, y + G0.ch / 2, PINK, 8, 300); } if (t > ta + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = H1k(t, tw, ta);
				// the cross it will blast
				g.fillStyle = H1P(0.03 + 0.14 * k); g.fillRect(G0.x0, y, G0.W, G0.ch); g.fillRect(x, G0.y0, G0.cw, G0.H);
				g.strokeStyle = H1G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 });
				g.strokeRect(G0.x0 + 2, y + 2, G0.W - 4, G0.ch - 4); g.strokeRect(x + 2, G0.y0 + 2, G0.cw - 4, G0.H - 4); g.setLineDash(new double[0]);
				if (t < tl) { double s = H1k(t, tw, tl); H1Ghost(x + G0.cw * (0.5 - 0.4 * s), y + G0.ch * (0.5 - 0.4 * s), G0.cw * 0.8 * s, G0.ch * 0.8 * s, 0.8); return; }
				double s2 = 1 + 0.06 * JS.sin((t - tl) * 30) * k;
				double bw = G0.cw * 0.8 * s2, bh = G0.ch * 0.8 * s2; H1Sq(x + (G0.cw - bw) / 2, y + (G0.ch - bh) / 2, bw, bh, 1, t - tl < 0.05);
				// pips: the beats left
				int left = (int)Math.Ceiling((ta - t) / SPB - 1e-6); g.fillStyle = "#fff";
				for (int i = 0; i < left; i++) circle(x + G0.cw / 2 + (i - (left - 1) / 2.0) * 22, y + G0.ch / 2, 7);
				return;
			}
			double u = t - ta, al = u < hold ? 1 : Math.Max(0, 1 - (u - hold) / 0.3); bool wh = u < 0.05;
			g.save(); g.globalAlpha = al; g.fillStyle = wh ? "#fff" : PINK; g.fillRect(G0.x0, y + 4, G0.W, G0.ch - 8); g.fillRect(x + 4, G0.y0, G0.cw - 8, G0.H);
			g.fillStyle = "rgba(70,0,20,0.45)"; g.fillRect(G0.x0, y + G0.ch / 2 - 6, G0.W, 12); g.fillRect(x + G0.cw / 2 - 6, G0.y0, 12, G0.H); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t >= tl && t < ta) return H1In(p, x + G0.cw * 0.1, y + G0.ch * 0.1, x + G0.cw * 0.9, y + G0.ch * 0.9);
			if (t < ta || t >= ta + hold) return false;
			return H1In(p, G0.x0, y, G0.x0 + G0.W, y + G0.ch) || H1In(p, x, G0.y0, x + G0.cw, G0.y0 + G0.H);
		};
		return h;
	}

	// the press: at each slam hs[k] the walls crush everything but the gap (centre gx, gy; mode 0: the walls come from the left and the
	// right and leave a column gw wide, 1: from all four sides and leave a gw x gh hole). The walls close over cl s, hold hold s, snap back (0.12 s).
	// The next gap is a dashed outline from the end of the slam before
	public static Hz H1Press(double[] hs, double[] gx, double[] gy, double[] md, double gw, double gh, double cl, double hold, double tw)
	{
		var h = new Hz { kind = "H1Press" }; int n = hs.Length; double te = hs[n - 1] + hold + 0.5; var done = new bool[n];
		h.update = (t, dt) =>
		{
			for (int k = 0; k < n; k++) if (!done[k] && t >= hs[k]) { done[k] = true; shake = Math.Max(shake, 6); }
			if (t > te) h.dead = true;
		};
		// how far the walls are in (0 out .. 1 shut) at slam k
		double shut(int k, double t) => t < hs[k] - cl ? 0 : t < hs[k] ? JS.pow((t - (hs[k] - cl)) / cl, 2) : t < hs[k] + hold ? 1 : Math.Max(0, 1 - (t - hs[k] - hold) / 0.12);
		h.draw = t =>
		{
			if (t < tw) return; double a = H1k(t, tw, tw + 0.4);
			int k = 0; while (k < n - 1 && t >= hs[k] + hold) k++;
			g.save(); g.globalAlpha = a;
			// the next gap
			double from = k > 0 ? hs[k - 1] + hold : tw;
			if (t >= from && t < hs[k] + hold)
			{
				double q = H1k(t, from, hs[k]); double x0 = gx[k] - gw / 2, y0 = md[k] > 0 ? gy[k] - gh / 2 : 0, ww = gw, hh = md[k] > 0 ? gh : H;
				g.strokeStyle = $"rgba(255,240,245,{str(0.45 + 0.5 * q)})"; g.lineWidth = 3 + 3 * q; g.setLineDash(new double[] { 14, 8 }); g.strokeRect(x0 + 4, y0 + 4, ww - 8, hh - 8); g.setLineDash(new double[0]);
				if (k > 0) H1Dash(gx[k - 1], md[k - 1] > 0 ? gy[k - 1] : H / 2, gx[k], md[k] > 0 ? gy[k] : H / 2, H1G(0.45), 3);
			}
			// the walls (the slam before, still going back, and this one)
			for (int j = Math.Max(0, k - 1); j <= k; j++)
			{
				double s = shut(j, t); bool wh = t >= hs[j] && t < hs[j] + 0.05;
				double l = gx[j] - gw / 2, r = gx[j] + gw / 2; double rest = 26;
				double lw = rest + (l - rest) * s, rw = rest + (W - r - rest) * s;
				string col = s >= 1 ? PINK : $"rgba(120,104,120,{str(0.6 + 0.4 * s)})";   // (a slam every beat: no white walls, only a white edge)
				g.fillStyle = col; g.fillRect(0, 0, lw, H); g.fillRect(W - rw, 0, rw, H);
				// teeth on the crushing edge
				g.fillStyle = wh ? "#fff" : col; g.beginPath(); for (double y = 0; y < H; y += 48) { g.moveTo(lw, y); g.lineTo(lw + 16, y + 24); g.lineTo(lw, y + 48); g.moveTo(W - rw, y); g.lineTo(W - rw - 16, y + 24); g.lineTo(W - rw, y + 48); } g.fill();
				if (md[j] > 0)
				{
					double tp = gy[j] - gh / 2, bt = gy[j] + gh / 2; double th = rest + (tp - rest) * s, bh = rest + (H - bt - rest) * s;
					g.fillStyle = col; g.fillRect(0, 0, W, th); g.fillRect(0, H - bh, W, bh);
				}
				g.strokeStyle = "rgba(60,0,18,0.5)"; g.lineWidth = 3; g.beginPath(); for (double y = 36; y < H; y += 72) { g.moveTo(8, y); g.lineTo(lw - 8, y); g.moveTo(W - rw + 8, y); g.lineTo(W - 8, y); } g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int k = 0; k < n; k++)
			{
				if (t < hs[k] - 0.03 || t >= hs[k] + hold) continue;
				double l = gx[k] - gw / 2, r = gx[k] + gw / 2;
				if (p.x - p.r + 2 < l || p.x + p.r - 2 > r) return true;
				if (md[k] > 0 && (p.y - p.r + 2 < gy[k] - gh / 2 || p.y + p.r - 2 > gy[k] + gh / 2)) return true;
			}
			return false;
		};
		return h;
	}

	// the square wave: a channel between two stepped walls (the floor fills under bot[i], the ceiling over top[i]) that runs sideways.
	// Step i is segw px wide; the steps move with the belt (its speed can turn round). Grey from tw, deadly ta .. te
	public static Hz H1Wave(double[] top, double[] bot, double segw, H1Belt belt, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H1Wave" }; int m = top.Length;
		int seg(double x, double t) { int i = (int)Math.Floor((x - belt.X(t)) / segw); return ((i % m) + m) % m; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; bool on = t >= ta && t < te; double a = Math.Min(H1k(t, tw, tw + 0.4), 1 - H1k(t, te, te + 0.3));
			g.save(); g.globalAlpha = a; double o = belt.X(t), x0 = o + Math.Floor(-o / segw) * segw;
			string fill = on ? PINK : H1P(0.08 + 0.2 * H1k(t, tw, ta));
			for (double x = x0; x < W; x += segw)
			{
				int i = seg(x + segw / 2, t); double xa = Math.Max(0, x), xb = Math.Min(W, x + segw); if (xb <= xa) continue;
				g.fillStyle = fill; g.fillRect(xa, 0, xb - xa, top[i]); g.fillRect(xa, bot[i], xb - xa, H - bot[i]);
				if (on) { g.fillStyle = "rgba(70,0,20,0.35)"; for (double q = top[i] - 30; q > 0; q -= 46) g.fillRect(xa, q - 4, xb - xa, 4); for (double q = bot[i] + 30; q < H; q += 46) g.fillRect(xa, q, xb - xa, 4); }
			}
			// the edges
			g.strokeStyle = on ? "#fff" : H1G(0.65); g.lineWidth = on ? 3 : 2; if (!on) g.setLineDash(new double[] { 10, 8 });
			for (int sd = 0; sd < 2; sd++)
			{
				g.beginPath(); bool first = true;
				for (double x = x0; x < W; x += segw) { int i = seg(x + segw / 2, t); double y = sd == 0 ? top[i] : bot[i]; double xa = Math.Max(0, x), xb = Math.Min(W, x + segw); if (first) { g.moveTo(xa, y); first = false; } else g.lineTo(xa, y); g.lineTo(xb, y); }
				g.stroke();
			}
			g.setLineDash(new double[0]); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			foreach (var x in new[] { p.x - p.r + 2, p.x, p.x + p.r - 2 }) { int i = seg(x, t); if (p.y - p.r + 2 < top[i] || p.y + p.r - 2 > bot[i]) return true; }
			return false;
		};
		return h;
	}

	// ---------- part 2: the clock strikes ----------
	// the clock face: a 5 x 5 board in the middle of the screen (the sides are walls). The cells in lay (as the board stands at the start)
	// are deadly blocks; at each turn tt[k] the whole board turns at once to qs[k] quarter turns (clockwise). The board as it will
	// stand after the next turn shows as dashed ghosts wn s before it
	public static Hz H1Face(H1Grid G0, double[] lay, double[] tt, double[] qs, double q0, double wn, double tw, double ta, double te, bool walls)
	{
		var h = new Hz { kind = "H1Face" }; int N = G0.cols; var on = new bool[N * N]; foreach (var v in lay) on[(int)v] = true;
		double cx = G0.x0 + G0.W / 2, cy = G0.y0 + G0.H / 2;
		int qAt(double t) { double q = q0; for (int k = 0; k < tt.Length; k++) if (t >= tt[k]) q = qs[k]; return (((int)q % 4) + 4) % 4; }
		// is cell (c, r) deadly with the board turned q quarters (clockwise)? turn back and look
		bool cell(int c, int r, int q) { for (int i = 0; i < q; i++) { int nc = r, nr = N - 1 - c; c = nc; r = nr; } return on[r * N + c]; }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(H1k(t, tw, tw + 0.5), 1 - H1k(t, te, te + 0.4)); bool act = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			if (walls)
			{
				double s = H1k(t, tw, ta);
				g.fillStyle = act ? PINK : $"rgba(120,104,120,{str(0.5 + 0.4 * s)})";
				g.fillRect(0, 0, G0.x0 * s, H); g.fillRect(W - (W - G0.x0 - G0.W) * s, 0, (W - G0.x0 - G0.W) * s, H);
				for (int i = 0; i < 2; i++) { double gxp = i == 0 ? G0.x0 * s - 140 : W - (W - G0.x0 - G0.W) * s + 140; H1Gear(gxp, H / 2, 120, 12, BT(t) * 0.25 * (i == 0 ? 1 : -1), "rgba(60,0,18,0.45)"); }
			}
			// the clock ring
			g.strokeStyle = "rgba(255,230,240,0.25)"; g.lineWidth = 3; g.strokeRect(G0.x0 + 2, G0.y0 + 2, G0.W - 4, G0.H - 4);
			for (int i = 0; i < 12; i++) { double an = i * TAU / 12; g.beginPath(); g.moveTo(cx + JS.cos(an) * 340, cy + JS.sin(an) * 340); g.lineTo(cx + JS.cos(an) * 356, cy + JS.sin(an) * 356); g.stroke(); }
			int q = qAt(t);
			// the turn itself: for 0.12 s the old board spins into the new one (only the look; it kills as the new one from tt[k])
			int kk = -1; for (int k = 0; k < tt.Length; k++) if (t >= tt[k] && t < tt[k] + 0.12) kk = k;
			if (kk >= 0 && act)
			{
				int qp = qAt(tt[kk] - 1e-4); double d = ((qs[kk] - (kk > 0 ? qs[kk - 1] : q0))), u = H1Ease((t - tt[kk]) / 0.12);
				g.save(); g.translate(cx, cy); g.rotate(d * Math.PI / 2 * u);
				for (int r = 0; r < N; r++) for (int c = 0; c < N; c++) if (cell(c, r, qp)) H1Sq(G0.X(c) - cx + 4, G0.Y(r) - cy + 4, G0.cw - 8, G0.ch - 8, 1, true);
				g.restore();
			}
			else
				for (int r = 0; r < N; r++) for (int c = 0; c < N; c++) if (cell(c, r, q)) H1Sq(G0.X(c) + 4, G0.Y(r) + 4, G0.cw - 8, G0.ch - 8, act ? 1 : 0.2 + 0.5 * H1k(t, tw, ta));
			// the next turn's board
			for (int k = 0; k < tt.Length; k++)
			{
				if (t < tt[k] - wn || t >= tt[k]) continue; double w = H1k(t, tt[k] - wn, tt[k]); int qn = ((((int)qs[k]) % 4) + 4) % 4;
				for (int r = 0; r < N; r++) for (int c = 0; c < N; c++) if (cell(c, r, qn)) H1Ghost(G0.X(c) + 4, G0.Y(r) + 4, G0.cw - 8, G0.ch - 8, 0.45 + 0.5 * w, 3);
				// the way it turns: an arc arrow round the face
				double d = qs[k] - (k > 0 ? qs[k - 1] : q0); if (d == 0) continue; double a0 = -Math.PI / 2, a1 = a0 + d * Math.PI / 2 * w;
				g.strokeStyle = $"rgba(255,240,245,{str(0.4 + 0.5 * w)})"; g.lineWidth = 6; g.beginPath(); g.arc(cx, cy, 372, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke();
				double ax = cx + JS.cos(a1) * 372, ay = cy + JS.sin(a1) * 372, tg = a1 + Math.Sign(d) * Math.PI / 2;
				g.fillStyle = $"rgba(255,240,245,{str(0.4 + 0.5 * w)})"; g.beginPath(); g.moveTo(ax + JS.cos(tg) * 18, ay + JS.sin(tg) * 18); g.lineTo(ax + JS.cos(tg + 2.4) * 16, ay + JS.sin(tg + 2.4) * 16); g.lineTo(ax + JS.cos(tg - 2.4) * 16, ay + JS.sin(tg - 2.4) * 16); g.closePath(); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			if (walls && (p.x - p.r + 2 < G0.x0 || p.x + p.r - 2 > G0.x0 + G0.W)) return true;
			int q = qAt(t);
			int c0 = (int)clamp(Math.Floor((p.x - p.r + 2 - G0.x0) / G0.cw), 0, N - 1), c1 = (int)clamp(Math.Floor((p.x + p.r - 2 - G0.x0) / G0.cw), 0, N - 1);
			int r0 = (int)clamp(Math.Floor((p.y - p.r + 2 - G0.y0) / G0.ch), 0, N - 1), r1 = (int)clamp(Math.Floor((p.y + p.r - 2 - G0.y0) / G0.ch), 0, N - 1);
			for (int r = r0; r <= r1; r++) for (int c = c0; c <= c1; c++)
				if (cell(c, r, q)) return true;
			return false;
		};
		return h;
	}

	// the dominoes: every cell of the grid stands up; cell i falls at each of its times (ft: per cell, nk times each, -1 = none) and
	// kills while it falls (hold s). It wobbles grey wn s before. A fallen one is safe (dark) until it stands up for the next wave
	public static Hz H1Domino(H1Grid G0, double[] ft, int nk, double hold, double wn, double tw, double te)
	{
		var h = new Hz { kind = "H1Domino" }; int n = G0.cols * G0.rows;
		double next(int i, double t) { for (int k = 0; k < nk; k++) { double f = ft[i * nk + k]; if (f >= 0 && t < f + hold) return f; } return double.PositiveInfinity; }
		double last(int i, double t) { double l = double.NegativeInfinity; for (int k = 0; k < nk; k++) { double f = ft[i * nk + k]; if (f >= 0 && t >= f + hold) l = f; } return l; }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(H1k(t, tw, tw + 0.5), 1 - H1k(t, te, te + 0.4));
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double x = G0.X(i % G0.cols), y = G0.Y(i / G0.cols), f = next(i, t), l = last(i, t);
				bool fallen = l > double.NegativeInfinity && (double.IsPositiveInfinity(f) || t < f - wn - SPB);
				if (t >= f)
				{
					// falling: the tile tips over (squashes toward its bottom edge) and kills
					double u = (t - f) / hold; double hh = (G0.ch - 10) * (1 - 0.75 * u);
					H1Sq(x + 5, y + G0.ch - 5 - hh, G0.cw - 10, hh, 1, t - f < 0.05);
					continue;
				}
				if (fallen)
				{
					g.fillStyle = "rgba(20,14,22,0.85)"; g.fillRect(x + 5, y + G0.ch - 30, G0.cw - 10, 25); g.strokeStyle = "rgba(255,42,109,0.25)"; g.lineWidth = 2; g.strokeRect(x + 5, y + G0.ch - 30, G0.cw - 10, 25);
					continue;
				}
				double w = t >= f - wn ? H1k(t, f - wn, f) : 0, wob = w > 0 ? JS.sin(t * 40 + i) * 3 * w : 0;
				H1Sq(x + 6 + wob, y + 6, G0.cw - 12, G0.ch - 12, 0.08 + 0.25 * w);
				if (w > 0) { g.strokeStyle = H1G(0.5 + 0.5 * w); g.lineWidth = 3; g.strokeRect(x + 4, y + 4, G0.cw - 8, G0.ch - 8); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw) return false;
			int c0 = (int)clamp(G0.Col(p.x - p.r + 2), 0, G0.cols - 1), c1 = (int)clamp(G0.Col(p.x + p.r - 2), 0, G0.cols - 1);
			int r0 = (int)clamp(G0.Row(p.y - p.r + 2), 0, G0.rows - 1), r1 = (int)clamp(G0.Row(p.y + p.r - 2), 0, G0.rows - 1);
			for (int r = r0; r <= r1; r++) for (int c = c0; c <= c1; c++)
			{
				int i = r * G0.cols + c;
				for (int k = 0; k < nk; k++) { double f = ft[i * nk + k]; if (f >= 0 && t >= f && t < f + hold) return true; }
			}
			return false;
		};
		return h;
	}

	// a knight: it lands on cell (cs[k], rs[k]) at tj[k] and the 3 x 3 cells round it blast for hold s. Between landings it stands on its cell
	// (it kills there) and then leaps in a high arc (the last jd s). The next landing shows as a dashed 3 x 3 and the arc to it
	public static Hz H1Knight(H1Grid G0, double[] cs, double[] rs, double[] tj, double hold, double jd, double tw)
	{
		var h = new Hz { kind = "H1Knight" }; int n = tj.Length; double te = tj[n - 1] + hold + 0.3; var done = new bool[n];
		double px(int k) => G0.X(cs[k]) + G0.cw / 2; double py(int k) => G0.Y(rs[k]) + G0.ch / 2;
		(double x, double y, bool air) at(double t)
		{
			if (t < tj[0] - jd) return (px(0), py(0) - 900, true);
			for (int k = 0; k < n; k++)
			{
				if (t < tj[k] - jd) return (px(k - 1), py(k - 1), false);
				if (t < tj[k])
				{
					double u = (t - (tj[k] - jd)) / jd; double x0 = k > 0 ? px(k - 1) : px(0), y0 = k > 0 ? py(k - 1) : py(0) - 600;
					return (x0 + (px(k) - x0) * u, y0 + (py(k) - y0) * u - JS.sin(Math.PI * u) * 160, true);
				}
			}
			return (px(n - 1), py(n - 1), false);
		}
		h.update = (t, dt) =>
		{
			for (int k = 0; k < n; k++) if (!done[k] && t >= tj[k]) { done[k] = true; shake = Math.Max(shake, 3); burst(px(k), py(k), PINK, 5, 240); }
			if (t > te) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(H1k(t, tw, tw + 0.4), 1 - H1k(t, te - 0.3, te));
			g.save(); g.globalAlpha = a;
			int k = 0; while (k < n && t >= tj[k] + hold) k++;
			// the blast now
			for (int j = Math.Max(0, k - 1); j < n && j <= k; j++)
			{
				if (t < tj[j] || t >= tj[j] + hold + 0.2) continue; double u = t - tj[j], al = u < hold ? 1 : 1 - (u - hold) / 0.2;
				double x0 = Math.Max(G0.x0, G0.X(cs[j] - 1)), y0 = Math.Max(G0.y0, G0.Y(rs[j] - 1)), x1 = Math.Min(G0.x0 + G0.W, G0.X(cs[j] + 2)), y1 = Math.Min(G0.y0 + G0.H, G0.Y(rs[j] + 2));
				g.globalAlpha = a * al; g.fillStyle = u < 0.05 ? "#fff" : PINK; g.fillRect(x0 + 3, y0 + 3, x1 - x0 - 6, y1 - y0 - 6);
				g.strokeStyle = "rgba(70,0,20,0.5)"; g.lineWidth = 4; g.strokeRect(G0.X(cs[j]) + 8, G0.Y(rs[j]) + 8, G0.cw - 16, G0.ch - 16); g.globalAlpha = a;
			}
			// the next landing
			if (k < n && t < tj[k])
			{
				double from = k > 0 ? tj[k - 1] : tw, w = H1k(t, from, tj[k]);
				double x0 = Math.Max(G0.x0, G0.X(cs[k] - 1)), y0 = Math.Max(G0.y0, G0.Y(rs[k] - 1)), x1 = Math.Min(G0.x0 + G0.W, G0.X(cs[k] + 2)), y1 = Math.Min(G0.y0 + G0.H, G0.Y(rs[k] + 2));
				H1Ghost(x0, y0, x1 - x0, y1 - y0, 0.4 + 0.55 * w, 2 + 2 * w); g.fillStyle = H1P(0.04 + 0.16 * w); g.fillRect(x0 + 3, y0 + 3, x1 - x0 - 6, y1 - y0 - 6);
				if (k > 0)
				{
					g.strokeStyle = H1G(0.35 + 0.4 * w); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath();
					for (int i = 0; i <= 16; i++) { double u = i / 16.0, x = px(k - 1) + (px(k) - px(k - 1)) * u, y = py(k - 1) + (py(k) - py(k - 1)) * u - JS.sin(Math.PI * u) * 160; if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
					g.stroke(); g.setLineDash(new double[0]);
				}
			}
			// the knight: an iron block with a notch (the horse's head cut)
			var c = at(t);
			if (c.y > -200)
			{
				double s = c.air ? 74 : 64; H1Sq(c.x - s / 2, c.y - s / 2, s, s, 1);
				g.fillStyle = "rgba(20,0,6,0.85)"; g.beginPath(); g.moveTo(c.x - s * 0.2, c.y - s * 0.35); g.lineTo(c.x + s * 0.3, c.y - s * 0.1); g.lineTo(c.x + s * 0.05, c.y + s * 0.05); g.lineTo(c.x - s * 0.1, c.y + s * 0.35); g.lineTo(c.x - s * 0.3, c.y + s * 0.35); g.closePath(); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int k = 0; k < n; k++)
			{
				if (t < tj[k] || t >= tj[k] + hold) continue;
				double x0 = Math.Max(G0.x0, G0.X(cs[k] - 1)), y0 = Math.Max(G0.y0, G0.Y(rs[k] - 1)), x1 = Math.Min(G0.x0 + G0.W, G0.X(cs[k] + 2)), y1 = Math.Min(G0.y0 + G0.H, G0.Y(rs[k] + 2));
				if (H1In(p, x0, y0, x1, y1)) return true;
			}
			if (t < tj[0]) return false; var c = at(t);
			return !c.air && H1In(p, c.x - 32, c.y - 32, c.x + 32, c.y + 32);
		};
		return h;
	}

	// a conveyor lane: blocks bw wide, one every per px, in the band y0 .. y0 + lh. Its speed is sv[i] px a second from beat sb[i]
	// (the change takes a quarter beat). The arrows of its next speed show a beat before. Grey from tw, deadly ta .. te
	public class H1Belt
	{
		readonly double t0; readonly double[] tab; const double ST = 1.0 / 240; readonly double[] st, sv;
		public double V(double t) { double v = sv[0]; for (int i = 1; i < st.Length; i++) { double r = SPB * 0.25; if (t >= st[i] + r) v = sv[i]; else { if (t > st[i]) v = sv[i - 1] + (sv[i] - sv[i - 1]) * (t - st[i]) / r; break; } } return v; }
		public H1Belt(double t0, double t1, double[] st, double[] sv, double x0 = 0)
		{
			this.t0 = t0; this.st = st; this.sv = sv; int n = (int)Math.Ceiling((t1 - t0) / ST) + 2; tab = new double[n]; tab[0] = x0;
			for (int k = 1; k < n; k++) { double tt = t0 + k * ST; tab[k] = tab[k - 1] + (V(tt - ST) + V(tt)) / 2 * ST; }
		}
		public double X(double t) { double f = (t - t0) / ST; if (f <= 0) return 0; int i = (int)f; if (i >= tab.Length - 1) return tab[^1]; return tab[i] + (tab[i + 1] - tab[i]) * (f - i); }
		public double NextV(double t, double ahead, out double at) { at = -1; for (int i = 1; i < st.Length; i++) if (st[i] > t && st[i] <= t + ahead) { at = st[i]; return sv[i]; } return double.NaN; }
	}
	public static Hz H1Lane(double y0, double lh, double bw, double per, double ph, H1Belt belt, double tw, double ta, double te)
	{
		var h = new Hz { kind = "H1Lane" }; double span = Math.Ceiling((W + bw) / per) * per;
		IEnumerable<double> xs(double t) { double o = ph + belt.X(t); o = ((o % span) + span) % span; for (double x = o - span; x < W + per; x += per) if (x + bw > 0 && x < W) yield return x; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(H1k(t, tw, tw + 0.4), 1 - H1k(t, te, te + 0.3)); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(24,20,28,0.8)"; g.fillRect(0, y0, W, lh);
			// the belt's rollers moving with it
			g.strokeStyle = "rgba(255,255,255,0.08)"; g.lineWidth = 2; g.beginPath(); double o = ((belt.X(t) % 40) + 40) % 40; for (double x = o - 40; x < W; x += 40) { g.moveTo(x, y0 + 4); g.lineTo(x, y0 + lh - 4); } g.stroke();
			foreach (var x in xs(t)) H1Sq(x, y0 + 8, bw, lh - 16, on ? 1 : 0.15 + 0.5 * H1k(t, tw, ta));
			// the arrows of the next change
			double nv = belt.NextV(t, SPB * 1.25, out double at);
			if (!double.IsNaN(nv) && at > 0) { double w = H1k(t, at - SPB * 1.25, at); H1Chevrons(0, y0, W, lh, 0, Math.Sign(nv) >= 0 ? 1 : -1, 0.3 + 0.6 * w, t * 3 * Math.Sign(nv)); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te || p.y + p.r - 2 < y0 + 8 || p.y - p.r + 2 > y0 + lh - 8) return false;
			foreach (var x in xs(t)) if (p.x + p.r - 2 > x && p.x - p.r + 2 < x + bw) return true;
			return false;
		};
		return h;
	}

	// ---------- part 3: midnight ----------
	// the 12 chimes: at each chime ct[k] one side of the box closes in by one cell of the grid (side 0 top, 1 right, 2 bottom, 3 left)
	// and stays shut. The strip that closes next glows wn s before. At tx it all bursts open
	public static Hz H1Shrink(H1Grid G0, double[] ct, double[] cs, double wn, double tw, double tx)
	{
		var h = new Hz { kind = "H1Shrink" }; int n = ct.Length; var done = new bool[n]; bool bx = false;
		void sides(double t, out int T, out int R, out int Bm, out int L) { T = R = Bm = L = 0; for (int k = 0; k < n; k++) if (t >= ct[k]) { int s = (int)cs[k]; if (s == 0) T++; else if (s == 1) R++; else if (s == 2) Bm++; else L++; } }
		h.update = (t, dt) =>
		{
			for (int k = 0; k < n; k++) if (!done[k] && t >= ct[k]) { done[k] = true; shake = Math.Max(shake, 4); }
			if (!bx && t >= tx) { bx = true; shake = Math.Max(shake, 12); burst(W / 2, H / 2, PINK, 40, 700); burst(W / 2, H / 2, "#fff", 20, 500); }
			if (t > tx + 1) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return; double a = 1 - H1k(t, tx, tx + 0.25), o = t >= tx ? H1Ease((t - tx) / 0.25) * 400 : 0;
			sides(Math.Min(t, tx - 1e-4), out int T, out int R, out int Bm, out int L);
			g.save(); g.globalAlpha = a;
			double l = G0.cw * L - o, r = G0.cw * R - o, tp = G0.ch * T - o, bt = G0.ch * Bm - o;
			g.fillStyle = PINK;
			if (l > 0) g.fillRect(0, 0, l, H); if (r > 0) g.fillRect(W - r, 0, r, H); if (tp > 0) g.fillRect(0, 0, W, tp); if (bt > 0) g.fillRect(0, H - bt, W, bt);
			// the bricks of the closed strips
			g.strokeStyle = "rgba(70,0,20,0.5)"; g.lineWidth = 2; g.beginPath();
			for (int i = 1; i <= L; i++) { g.moveTo(G0.cw * i - o, 0); g.lineTo(G0.cw * i - o, H); }
			for (int i = 1; i <= R; i++) { g.moveTo(W - G0.cw * i + o, 0); g.lineTo(W - G0.cw * i + o, H); }
			for (int i = 1; i <= T; i++) { g.moveTo(0, G0.ch * i - o); g.lineTo(W, G0.ch * i - o); }
			for (int i = 1; i <= Bm; i++) { g.moveTo(0, H - G0.ch * i + o); g.lineTo(W, H - G0.ch * i + o); }
			g.stroke();
			// the next strip
			for (int k = 0; k < n; k++)
			{
				if (t < ct[k] - wn || t >= ct[k]) continue; double w = H1k(t, ct[k] - wn, ct[k]); int s = (int)cs[k];
				double x0 = s == 3 ? l : s == 1 ? W - r - G0.cw : l, x1 = s == 3 ? l + G0.cw : s == 1 ? W - r : W - r;
				double y0 = s == 0 ? tp : s == 2 ? H - bt - G0.ch : tp, y1 = s == 0 ? tp + G0.ch : s == 2 ? H - bt : H - bt;
				g.fillStyle = H1P(0.08 + 0.3 * w); g.fillRect(x0, y0, x1 - x0, y1 - y0); H1Ghost(x0, y0, x1 - x0, y1 - y0, 0.5 + 0.5 * w, 3);
			}
			// the clock hand that strikes: from the middle of the box to the strip
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ct[0] || t >= tx) return false; sides(t, out int T, out int R, out int Bm, out int L);
			return p.x - p.r + 2 < G0.cw * L || p.x + p.r - 2 > W - G0.cw * R || p.y - p.r + 2 < G0.ch * T || p.y + p.r - 2 > H - G0.ch * Bm;
		};
		return h;
	}

	// the stack: iron blocks fall into the columns of the grid and pile up; a full row blasts across and the blocks over it drop a row.
	// Block j: column bc[j], it falls from bs[j] and lands at kt[first] on row kr[first]; it is on row kr[i] from kt[i] (its keys are
	// ko[j] .. ko[j+1]-1); it is gone from bd[j]. Row clears: row cr[i] and the row over it blast at ctm[i] for hold s. A block's column glows from bs - wn
	public static Hz H1Stack(H1Grid G0, double[] bc, double[] bs, double[] ko, double[] kt, double[] kr, double[] bd, double[] cr, double[] ctm, double hold, double wn, double te)
	{
		var h = new Hz { kind = "H1Stack" }; int n = bc.Length; var done = new bool[cr.Length];
		// where block j is at t (top of the block, y), and if it is there
		bool pos(int j, double t, out double y)
		{
			y = 0; if (t < bs[j] || t >= bd[j]) return false; int k0 = (int)ko[j], k1 = (int)ko[j + 1];
			if (t < kt[k0]) { double u = (t - bs[j]) / (kt[k0] - bs[j]); y = -G0.ch + (G0.Y(kr[k0]) + G0.ch) * u * u; return true; }
			int k = k0; while (k + 1 < k1 && t >= kt[k + 1]) k++;
			y = G0.Y(kr[k]);
			// it drops to the row below over 0.1 s
			if (k > k0 && t < kt[k] + 0.1) { double u = (t - kt[k]) / 0.1; y = G0.Y(kr[k - 1]) + (G0.Y(kr[k]) - G0.Y(kr[k - 1])) * u; }
			return true;
		}
		h.update = (t, dt) =>
		{
			for (int i = 0; i < cr.Length; i++) if (!done[i] && t >= ctm[i]) { done[i] = true; shake = Math.Max(shake, 7); burst(W / 2, G0.Y(cr[i]) + G0.ch / 2, PINK, 16, 600); }
			if (t > te + 0.4) h.dead = true;
		};
		h.draw = t =>
		{
			double a = 1 - H1k(t, te, te + 0.3); if (a <= 0) return; g.save(); g.globalAlpha = a;
			for (int j = 0; j < n; j++)
			{
				double x = G0.X(bc[j]);
				if (t >= bs[j] - wn && t < kt[(int)ko[j]])
				{
					double w = H1k(t, bs[j] - wn, kt[(int)ko[j]]); double ly = G0.Y(kr[(int)ko[j]]);
					g.fillStyle = H1P(0.03 + 0.1 * w); g.fillRect(x + 4, 0, G0.cw - 8, ly); H1Ghost(x, ly, G0.cw, G0.ch, 0.45 + 0.5 * w, 3);
				}
				if (pos(j, t, out double y)) H1Sq(x + 5, y + 5, G0.cw - 10, G0.ch - 10, 1, t >= kt[(int)ko[j]] && t < kt[(int)ko[j]] + 0.05);
			}
			for (int i = 0; i < cr.Length; i++)
			{
				if (t >= ctm[i] - SPB && t < ctm[i]) { double w = H1k(t, ctm[i] - SPB, ctm[i]); g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * w)})"; g.lineWidth = 3 + 3 * w; g.strokeRect(G0.x0 + 2, Math.Max(G0.y0, G0.Y(cr[i] - 1)) + 2, G0.W - 4, G0.Y(cr[i]) + G0.ch - Math.Max(G0.y0, G0.Y(cr[i] - 1)) - 4); }
				if (t >= ctm[i] && t < ctm[i] + hold + 0.2) { double u = t - ctm[i]; g.globalAlpha = a * (u < hold ? 1 : 1 - (u - hold) / 0.2); g.fillStyle = u < 0.05 ? "#fff" : PINK; g.fillRect(G0.x0, Math.Max(G0.y0, G0.Y(cr[i] - 1)), G0.W, G0.Y(cr[i]) + G0.ch - Math.Max(G0.y0, G0.Y(cr[i] - 1))); g.globalAlpha = a; }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int i = 0; i < cr.Length; i++) if (t >= ctm[i] && t < ctm[i] + hold && H1In(p, G0.x0, Math.Max(G0.y0, G0.Y(cr[i] - 1)), G0.x0 + G0.W, G0.Y(cr[i]) + G0.ch)) return true;
			for (int j = 0; j < n; j++) if (pos(j, t, out double y) && H1In(p, G0.X(bc[j]), y, G0.X(bc[j]) + G0.cw, y + G0.ch)) return true;
			return false;
		};
		return h;
	}

	// ---------- the clockwork itself (harmless) ----------
	// part 1: big gears turning behind, 2: the clock face ring and its ticks, 3: the hands closing on midnight
	public static Hz H1Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "H1Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			double bt = BT(t), tick = Math.Floor(bt) + H1Ease(Math.Min(1, (bt - Math.Floor(bt)) / 0.15));
			g.fillStyle = part == 3 ? "rgba(30,4,12,0.6)" : "rgba(16,12,20,0.6)"; g.fillRect(0, 0, W, H);
			if (part == 1)
			{
				H1Gear(180, 140, 210, 14, tick * TAU / 28, "rgba(255,255,255,0.035)"); H1Gear(1110, 590, 250, 16, -tick * TAU / 32, "rgba(255,255,255,0.035)"); H1Gear(760, 120, 120, 10, -tick * TAU / 20, "rgba(255,255,255,0.03)");
			}
			if (part == 2)
			{
				g.strokeStyle = "rgba(255,255,255,0.05)"; g.lineWidth = 3;
				for (int i = 0; i < 60; i++) { double an = i * TAU / 60, l = i % 5 == 0 ? 40 : 16; g.beginPath(); g.moveTo(W / 2 + JS.cos(an) * 600, H / 2 + JS.sin(an) * 600); g.lineTo(W / 2 + JS.cos(an) * (600 - l), H / 2 + JS.sin(an) * (600 - l)); g.stroke(); }
			}
			if (part == 3)
			{
				// the hands: the big one ticks to 12 by the end, the small one is already there
				double k = H1k(t, t0, t1), am = -Math.PI / 2 - (1 - k) * Math.PI * 0.9 + 0.02 * JS.sin(bt * Math.PI);
				g.strokeStyle = $"rgba(255,42,109,{str(0.1 + 0.05 * pulse)})"; g.lineWidth = 18; g.beginPath(); g.moveTo(W / 2, H / 2); g.lineTo(W / 2 + JS.cos(am) * 420, H / 2 + JS.sin(am) * 420); g.stroke();
				g.lineWidth = 28; g.beginPath(); g.moveTo(W / 2, H / 2); g.lineTo(W / 2, H / 2 - 260); g.stroke();
				g.fillStyle = "rgba(255,42,109,0.15)"; circle(W / 2, H / 2, 30);
			}
			var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}
	// the end: the last chime, the clock bursts into its squares
	public static Hz H1Burst(double t1)
	{
		var h = new Hz { kind = "H1Burst" }; bool b0 = false;
		h.update = (t, dt) => { if (!b0 && t >= t1) { b0 = true; burst(W / 2, H / 2, "#fff", 40, 800); burst(W / 2, H / 2, PINK, 40, 600); } if (t > t1 + 2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t1) return; double u = t - t1; g.fillStyle = $"rgba(255,42,109,{str(Math.Max(0, 0.9 - u))})";
			for (int i = 0; i < 24; i++) { double a = i * TAU / 24 + 0.1, d = 40 + u * 640; g.save(); g.translate(W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d); g.rotate(u * (2 + i % 5)); g.fillRect(-20, -20, 40, 40); g.restore(); }
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_H1.json) ----------
	// times are beats. Hardcore has no Easy, so the warnings are as they are
	static double[] H1Sec(double[] bs) => bs.Select(B).ToArray();

	static readonly int _kh1fx = DodgeKind("H1fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 1.5)), t0 = B(e.b); var h = new Hz { kind = "H1fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _kh1gloom = DodgeKind("H1gloom", e => () => { gloomT = e.num("v"); });
	static readonly int _kh1stage = DodgeKind("H1stage", e => () => hazards.Add(H1Stage(e.i("part"), B(e.num("at", e.b)), B(e.num("to")))));
	static readonly int _kh1burst = DodgeKind("H1burst", e => () => hazards.Add(H1Burst(B(e.num("t1")))));
	static readonly int _kh1board = DodgeKind("H1board", e => () => hazards.Add(H1Board(H1Gof(e), B(e.b), B(e.num("to")))));
	static readonly int _kh1jump = DodgeKind("H1jump", e => () =>
		hazards.Add(H1Jump(e.nums("xs"), e.nums("ys"), e.num("b0"), e.num("st", 1), e.num("sz"), B(e.num("hit") - e.num("wn")), B(e.num("hit")), B(e.num("to")))));
	static readonly int _kh1slide = DodgeKind("H1slide", e => () =>
	{
		var G0 = H1Gof(e); int axis = e.i("axis"), idx = e.i("idx");
		// aimed: the square's own row or column right now
		if (e.flag("aim") && player != null) idx = axis == 0 ? (int)clamp(G0.Row(player.y), 0, G0.rows - 1) : (int)clamp(G0.Col(player.x), 0, G0.cols - 1);
		hazards.Add(H1Slide(G0, axis, idx, e.i("dir", 1), B(e.num("hit") - e.num("wn")), B(e.num("hit")), e.num("dur") * SPB));
	});
	static readonly int _kh1bomb = DodgeKind("H1bomb", e => () =>
		hazards.Add(H1Bomb(H1Gof(e), e.i("c"), e.i("r"), B(e.num("land")), B(e.num("hit")), e.num("hold") * SPB)));
	static readonly int _kh1press = DodgeKind("H1press", e => () =>
		hazards.Add(H1Press(H1Sec(e.nums("hs")), e.nums("gx"), e.nums("gy"), e.nums("md"), e.num("gw"), e.num("gh"), e.num("cl") * SPB, e.num("hold") * SPB, B(e.num("tw")))));
	static readonly int _kh1wave = DodgeKind("H1wave", e => () =>
	{
		double ta = B(e.num("hit")), tw = B(e.num("hit") - e.num("wn")), te = B(e.num("to"));
		var belt = new H1Belt(tw - 1, te + 1, H1Sec(e.nums("sb")), e.nums("sv"), e.num("x0"));
		hazards.Add(H1Wave(e.nums("top"), e.nums("bot"), e.num("segw"), belt, tw, ta, te));
	});
	static readonly int _kh1face = DodgeKind("H1face", e => () =>
		hazards.Add(H1Face(H1Gof(e), e.nums("lay"), H1Sec(e.nums("tt")), e.nums("qs"), e.num("q0"), e.num("wn") * SPB, B(e.num("hit") - e.num("wi")), B(e.num("hit")), B(e.num("to")), e.flag("walls"))));
	static readonly int _kh1domino = DodgeKind("H1domino", e => () =>
	{
		var ft = e.nums("ft").Select(v => v < 0 ? -1 : B(v)).ToArray();
		hazards.Add(H1Domino(H1Gof(e), ft, e.i("nk"), e.num("hold") * SPB, e.num("wn") * SPB, B(e.b + 0.01), B(e.num("to"))));
	});
	static readonly int _kh1knight = DodgeKind("H1knight", e => () =>
		hazards.Add(H1Knight(H1Gof(e), e.nums("cs"), e.nums("rs"), H1Sec(e.nums("tj")), e.num("hold") * SPB, e.num("jd") * SPB, B(e.num("tw")))));
	static readonly int _kh1lane = DodgeKind("H1lane", e => () =>
	{
		double ta = B(e.num("hit")), tw = B(e.num("hit") - e.num("wn")), te = B(e.num("to"));
		var belt = new H1Belt(tw - 1, te + 1, H1Sec(e.nums("sb")), e.nums("sv"));
		hazards.Add(H1Lane(e.num("y0"), e.num("lh"), e.num("bw"), e.num("per"), e.num("ph"), belt, tw, ta, te));
	});
	static readonly int _kh1shrink = DodgeKind("H1shrink", e => () =>
		hazards.Add(H1Shrink(H1Gof(e), H1Sec(e.nums("ct")), e.nums("cs"), e.num("wn") * SPB, B(e.b + 0.01), B(e.num("tx")))));
	static readonly int _kh1stack = DodgeKind("H1stack", e => () =>
		hazards.Add(H1Stack(H1Gof(e), e.nums("bc"), H1Sec(e.nums("bs")), e.nums("ko"), H1Sec(e.nums("kt")), e.nums("kr"), H1Sec(e.nums("bd")), e.nums("cr"), H1Sec(e.nums("ctm")), e.num("hold") * SPB, e.num("wn") * SPB, B(e.num("to")))));
}
