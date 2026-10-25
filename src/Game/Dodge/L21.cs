using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Barracuda (Noisestorm), main level 21: "the hunt in the deep". You are a small fish at night under the sea.
// Part 1 the reef at night (the jellyfish that pulse on the drums, the fishermen's harpoons, the coral that grows on the tune, the net you ride),
// part 2 the pack hunts (barracudas strike in the order of a clock, the school that splits round each strike, the bait ball),
// part 3 the giant barracuda (it circles and bites, it waits in the dark, it chases you, the last bite).
// Every hazard is a pure function of time once it exists (aimed ones also of where the square was when they locked). Only pink hurts.
// Grid: 129.7 BPM, beat 0 = 0.581 s (from the drums stem). Win at beat 224 (1:44.2).
public static partial class G
{
	static double BRk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string BRP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string BRW(double a) => $"rgba(255,255,255,{str(clamp(a, 0, 1))})";
	static double BREase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double BROut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static void BRDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double BRSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// where a ray from (x,y) along (dx,dy) leaves the screen (plus m px)
	static double BRExit(double x, double y, double dx, double dy, double m = 60)
	{
		double best = 1e9;
		if (dx > 1e-6) best = Math.Min(best, (W + m - x) / dx); if (dx < -1e-6) best = Math.Min(best, (-m - x) / dx);
		if (dy > 1e-6) best = Math.Min(best, (H + m - y) / dy); if (dy < -1e-6) best = Math.Min(best, (-m - y) / dy);
		return Math.Max(0, best);
	}
	static double BRRnd(double b) => (Math.Sin(b * 12.9898 + 78.233) * 43758.5453) % 1;   // a fixed number from a beat (looks only)

	// ---------- the sea itself (harmless, under everything) ----------
	public static Hz BRStage(double t1)
	{
		var h = new Hz { kind = "BRStage", z = -1 };
		_brCuts.Clear();
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.8), p2 = BRk(bt, 95.5, 96.5) * (1 - BRk(bt, 159, 160.5)), p3 = BRk(bt, 159, 161);
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(8 + 10 * p3)},{str(26 - 14 * p2 - 12 * p3)},{str(52 - 18 * p2 - 30 * p3)},1)");
			gr.addColorStop(1, $"rgba({str(2 + 6 * p3)},{str(6 - 3 * p2 - 3 * p3)},{str(16 - 6 * p2 - 8 * p3)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the moon's light from the surface: three slow rays
			for (int i = 0; i < 3; i++)
			{
				double cx = 260 + i * 380 + 60 * JS.sin(t * 0.3 + i * 2), wd = 70 + 30 * JS.sin(t * 0.5 + i);
				g.fillStyle = $"rgba(140,190,255,{str((0.035 + 0.015 * pulse) * (1 - 0.7 * p3))})";
				g.beginPath(); g.moveTo(cx - wd * 0.4, 0); g.lineTo(cx + wd * 0.4, 0); g.lineTo(cx + wd * 2.2 - 160, H); g.lineTo(cx - wd * 2.2 - 160, H); g.closePath(); g.fill();
			}
			// part 2: the current, fast streaks going left
			if (p2 > 0)
			{
				g.strokeStyle = $"rgba(150,200,255,{str(0.07 * p2)})"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 26; i++) { double sy = (i * 97.3) % H, sp = 500 + (i * 53) % 400, sx = W + 200 - ((i * 311.7 + t * sp) % (W + 400)); g.moveTo(sx, sy); g.lineTo(sx + 90 + (i % 3) * 40, sy); }
				g.stroke();
			}
			// bubbles going up (white, small, faint)
			g.fillStyle = "rgba(210,235,255,0.16)";
			for (int i = 0; i < 34; i++) { double bx = (i * 383.7) % W + 10 * JS.sin(t * 1.3 + i), sp = 40 + (i * 37) % 70, by = H + 20 - ((i * 211.3 + t * sp) % (H + 40)); circle(bx, by, i % 4 == 0 ? 3 : 1.6); }
			// the dark round the edges (stronger near the boss)
			var vg = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, $"rgba(0,0,0,{str(0.55 + 0.2 * p3)})"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- 1. the jellyfish (0:00-0:15) ----------
	// A lattice of jellyfish (sx apart, rows sy apart, every other row half a column across) going up. It surges sp px on every hit of the
	// drums (pb, beats) and drifts v px/s between. From the high sound (vb, the step +1/-1 in vd) even rows slide sh px one way, odd rows the other.
	public static Hz BRJelly(double sx, double sy, double[] pb, double sp, double v, double[] vb, double[] vd, double sh, double b0, double bOut)
	{
		var h = new Hz { kind = "BRJelly" }; const double BW = 24, TL = 64; double t0 = B(b0);
		double D(double t)
		{
			double bt = BT(t), d = v * Math.Max(0, t - t0) + 420 * Math.Max(0, t - B(bOut - 4.5)) * Math.Min(1, Math.Max(0, t - B(bOut - 4.5)) / 0.6);
			foreach (var p in pb) { if (bt < p) break; d += sp * BROut((bt - p) / 0.45); }
			return d;
		}
		double Sh(double bt) { double s = 0; for (int i = 0; i < vb.Length; i++) { if (bt < vb[i]) break; s += vd[i] * sh * BREase((bt - vb[i]) / 0.12); } return s; }
		double squeeze(double bt) { double q = 0; foreach (var p in pb) { if (bt < p) break; double u = bt - p; if (u < 0.3) q = 1 - u / 0.3; } return q; }
		double enter = D(B(bOut - 4.5)); int rows = 0; while (H + 30 + rows * sy - enter < H + 20) rows++;
		h.update = (t, dt) => { if (BT(t) > bOut + 1) h.dead = true; };
		void each(double t, Action<double, double> f)
		{
			double bt = BT(t), d = D(t), s = Sh(bt);
			for (int k = 0; k < rows; k++)
			{
				double y = H + 30 + k * sy - d; if (y < -TL - 40 || y > H + 40) continue;
				double off = (k % 2) * sx / 2 + (k % 2 == 0 ? s : -s); off = ((off % sx) + sx) % sx;
				for (double x = off - sx; x < W + sx; x += sx) f(x, y);
			}
		}
		h.draw = t =>
		{
			double bt = BT(t), q = squeeze(bt), sw = JS.sin(t * 3);
			g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.strokeStyle = PINK; g.lineWidth = 4; g.lineCap = "round";
			each(t, (x, y) =>
			{
				double bw = BW * (1 - 0.18 * q), bh = 22 * (1 + 0.2 * q);
				g.beginPath(); g.ellipse(x, y, bw, bh, 0, Math.PI, TAU); g.closePath(); g.fill();
				g.beginPath();
				for (int j = -1; j <= 1; j++) { double bx = x + j * 12, tip = sw * 10 * (j == 0 ? -1 : 1); g.moveTo(bx, y + 1); g.quadraticCurveTo(bx + tip * 0.3, y + TL * 0.5, bx + tip, y + TL); }
				g.stroke();
			});
			g.shadowBlur = 0; g.fillStyle = "rgba(255,200,220,0.45)";
			each(t, (x, y) => { g.beginPath(); g.ellipse(x - 6, y - 12, 7, 4, -0.4, 0, TAU); g.fill(); });
			g.restore();
		};
		h.hit = (p, t) =>
		{
			bool hit = false; double sw = JS.sin(t * 3);
			each(t, (x, y) =>
			{
				if (hit || Math.Abs(p.x - x) > 60 || p.y < y - 40 || p.y > y + TL + 20) return;
				double ex = (p.x - x) / (BW + p.r - 2), ey = (p.y - (y - 10)) / (14 + p.r - 2); if (ex * ex + ey * ey < 1 && p.y < y + 2 + p.r) { hit = true; return; }
				for (int j = -1; j <= 1; j++) { double bx = x + j * 12, tip = sw * 10 * (j == 0 ? -1 : 1); if (BRSeg(p.x, p.y, bx, y + 1, bx + tip, y + TL) < 2 + p.r - 2) { hit = true; return; } }
			});
			return hit;
		};
		return h;
	}

	// ---------- 2. the fishermen's harpoons (0:15-0:30) ----------
	// The boats on the surface (harmless)
	public static Hz BRBoats(double[] xs, double b0, double b1)
	{
		var h = new Hz { kind = "BRBoats" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(BRk(bt, b0, b0 + 1), 1 - BRk(bt, b1, b1 + 1)); if (a <= 0) return;
			g.save(); g.globalAlpha = a; g.fillStyle = "rgba(10,14,24,0.95)"; g.strokeStyle = "rgba(170,190,220,0.35)"; g.lineWidth = 2;
			g.beginPath(); g.moveTo(0, 6); g.lineTo(W, 6); g.stroke();
			foreach (var x in xs) { double bob = 3 * JS.sin(t * 2 + x); g.beginPath(); g.moveTo(x - 46, -4 + bob); g.lineTo(x + 46, -4 + bob); g.lineTo(x + 30, 16 + bob); g.lineTo(x - 30, 16 + bob); g.closePath(); g.fill(); g.stroke(); }
			g.restore();
		};
		return h;
	}
	// one harpoon from the boat at bx: its line follows the square from b0 and locks at bl, it flies at sp px/s at bf, its rope stays (bowing on
	// the bass) until bf + life beats, then it is reeled back at 3000 px/s. Aimed: rnd
	public static Hz BRHarpoon(double bx, double b0, double bl, double bf, double life, double sp, double ox = 0)
	{
		var h = new Hz { kind = "BRHarpoon" }; const double BY = 14; double dx = 0, dy = 1, L = 0; bool lk = false; double tf = B(bf), tr = B(bf + life);
		void aim() { double tx = clamp((player?.x ?? W / 2) + ox, 20, W - 20), ty = player?.y ?? H / 2; double l = Math.Max(1, hypot(tx - bx, ty - BY)); dx = (tx - bx) / l; dy = (ty - BY) / l; L = BRExit(bx, BY, dx, dy, 20); }
		double tip(double t) { if (t < tf) return 0; double d = Math.Min(L, (t - tf) * sp); if (t > tr) d = Math.Max(0, Math.Min(L, (tr - tf) * sp) - (t - tr) * 3000); return d; }
		double bow(double t, double u) => 14 * JS.sin(Math.PI * u) * JS.sin(TAU * BT(t) / 2);
		h.update = (t, dt) => { double bt = BT(t); if (!lk) { aim(); if (bt >= bl) lk = true; } h.z = t < tf ? -1 : 0; if (t > tr && tip(t) <= 0) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return;
			if (bt < bf)
			{
				double k = BRk(bt, b0, bf); BRDash(bx, BY, bx + dx * L, BY + dy * L, lk ? BRW(0.55 + 0.4 * k) : "rgba(215,215,230,0.4)", lk ? 3 : 2);
				g.strokeStyle = BRW(0.8); g.lineWidth = 3; g.beginPath(); g.arc(bx, BY + 6, 10 + 8 * (1 - k), 0, TAU); g.stroke(); return;
			}
			double d = tip(t); if (d <= 0) return; double nx = -dy, ny = dx;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = 5; g.beginPath();
			for (int i = 0; i <= 12; i++) { double u = i / 12.0, s = d * u, b = bow(t, s / Math.Max(1, L)); double x = bx + dx * s + nx * b, y = BY + dy * s + ny * b; if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
			g.stroke();
			double hx = bx + dx * d, hy = BY + dy * d; g.fillStyle = PINK; g.beginPath(); g.moveTo(hx + dx * 16, hy + dy * 16); g.lineTo(hx + nx * 9 - dx * 6, hy + ny * 9 - dy * 6); g.lineTo(hx - nx * 9 - dx * 6, hy - ny * 9 - dy * 6); g.closePath(); g.fill();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = tip(t); if (d <= 0) return false; double nx = -dy, ny = dx, px = bx, py = BY;
			for (int i = 1; i <= 12; i++) { double u = i / 12.0, s = d * u, b = bow(t, s / Math.Max(1, L)); double x = bx + dx * s + nx * b, y = BY + dy * s + ny * b; if (BRSeg(p.x, p.y, px, py, x, y) < 2.5 + p.r - 2) return true; px = x; py = y; }
			return hypot(p.x - (bx + dx * (d + 4)), p.y - (BY + dy * (d + 4))) < 9 + p.r - 2;
		};
		return h;
	}

	// ---------- 3. the coral (0:30-0:37) ----------
	// Trees from roots (rx, ry, ra): note j (beat nb[j], pitch class np[j]) grows the next branch of root j % n (each branch gives two tips,
	// grown in order); a higher note leans it more to the right of its way. The next branch of each root shows dashed 0.75 beat before.
	// On every beat from b0 the coral breathes (thicker for a moment). At bb it breaks and its pieces fall.
	public class BRBranch { public double x0, y0, x1, y1, w, b; public int root; }
	public static Hz[] BRCoral(double[] rx, double[] ry, double[] ra, double[] nb, double[] np, double L0, double sc, double w0, double b0, double bb)
	{
		var h = new Hz { kind = "BRCoral" }; int R = rx.Length; var br = new List<BRBranch>();
		var q = new List<Queue<(double x, double y, double a, double l, double w)>>();
		for (int i = 0; i < R; i++) { var Q = new Queue<(double, double, double, double, double)>(); Q.Enqueue((rx[i], ry[i], ra[i] * Math.PI / 180, L0, w0)); q.Add(Q); }
		for (int j = 0; j < nb.Length; j++)
		{
			int r = j % R; if (q[r].Count == 0) continue; var tp = q[r].Dequeue();
			double a = tp.a + (np[j] - 5.5) / 5.5 * 0.22, x1 = tp.x + JS.cos(a) * tp.l, y1 = tp.y + JS.sin(a) * tp.l;
			br.Add(new BRBranch { x0 = tp.x, y0 = tp.y, x1 = x1, y1 = y1, w = tp.w, b = nb[j], root = r });
			q[r].Enqueue((x1, y1, a - 0.5, tp.l * sc, tp.w * 0.78)); q[r].Enqueue((x1, y1, a + 0.5, tp.l * sc, tp.w * 0.78));
		}
		const double GROW = 0.12;
		double breath(double bt) { if (bt < b0) return 0; double u = (bt - b0) % 1; return u < 0.25 ? 5 * (1 - u / 0.25) : 0; }
		// where branch c is at beat bt (falling after bb): its two ends, its width; false when not there
		bool at(BRBranch c, double bt, out double x0, out double y0, out double x1, out double y1)
		{
			x0 = c.x0; y0 = c.y0; x1 = c.x1; y1 = c.y1; if (bt < c.b) return false;
			double k = Math.Min(1, (bt - c.b) / GROW); x1 = c.x0 + (c.x1 - c.x0) * k; y1 = c.y0 + (c.y1 - c.y0) * k;
			if (bt < bb) return true;
			double u = (B(bt) - B(bb)) - (c.root % 3) * 0.02, f = u > 0 ? 0.5 * 3400 * u * u : 0, rot = u > 0 ? u * (c.root % 2 == 0 ? 2.2 : -2.2) : 0;
			double mx = (x0 + x1) / 2, my = (y0 + y1) / 2 + f, hx = (x1 - x0) / 2, hy = (y1 - y0) / 2, c2 = JS.cos(rot), s2 = JS.sin(rot);
			x0 = mx - (hx * c2 - hy * s2); y0 = my - (hx * s2 + hy * c2); x1 = mx + (hx * c2 - hy * s2); y1 = my + (hx * s2 + hy * c2);
			return Math.Min(y0, y1) < H + 40;
		}
		double tEnd = B(bb) + 0.9;
		h.update = (t, dt) => { if (t > tEnd) h.dead = true; };
		// the next branch of each root (under everything)
		var tel = new Hz { kind = "BRCoralTell", z = -1 };
		tel.update = (t, dt) => { if (t > tEnd) tel.dead = true; };
		tel.draw = t =>
		{
			double bt = BT(t); if (bt >= bb) return; g.save(); g.lineCap = "round"; var seen = new bool[R];
			foreach (var c in br) { if (c.b <= bt || seen[c.root]) continue; seen[c.root] = true; if (c.b - bt > 0.75) continue; double k = BRk(bt, c.b - 0.75, c.b); BRDash(c.x0, c.y0, c.x1, c.y1, BRW(0.3 + 0.55 * k), 2 + 2 * k); }
			g.restore();
		};
		h.draw = t =>
		{
			double bt = BT(t), w2 = breath(bt);
			g.save(); g.lineCap = "round";
			g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK;
			foreach (var c in br) { if (!at(c, bt, out double x0, out double y0, out double x1, out double y1)) continue; g.lineWidth = c.w + w2; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); }
			g.shadowBlur = 0; g.strokeStyle = "rgba(255,190,210,0.45)";
			foreach (var c in br) { if (!at(c, bt, out double x0, out double y0, out double x1, out double y1)) continue; g.lineWidth = Math.Max(1.5, c.w * 0.25); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), w2 = breath(bt);
			foreach (var c in br) { if (!at(c, bt, out double x0, out double y0, out double x1, out double y1)) continue; if (BRSeg(p.x, p.y, x0, y0, x1, y1) < (c.w + w2) / 2 + p.r - 2) return true; }
			return false;
		};
		return new[] { tel, h };
	}

	// ---------- 4. the net (0:37-0:45) ----------
	// A diamond net (lines s px apart, th0 -> th1 thick) round the middle. It steps st px on every kick (kb) the way of its bar (down, right,
	// up, left) and turns w rad/s, the way flipping every bar. From torn one cell (2 x 2) is torn open (white edge); at cl the net closes
	// (the lines grow over every other cell) and at lift it goes. Grey dashed from tw, kills ta .. lift (beats)
	public static Hz BRNet(double s, double th0, double th1, double[] kb, double st, double w, double tw, double ta, double torn, double cl, double lift)
	{
		var h = new Hz { kind = "BRNet" }; double[] DX = { 0, 1, 0, -1 }, DY = { 1, 0, -1, 0 }; double tA = B(ta);
		(double x, double y) O(double bt)
		{
			double x = 0, y = 0; foreach (var k in kb) { if (bt < k) break; int bar = (int)Math.Floor((k - ta) / 4) & 3; double e = BREase((bt - k) / 0.18); x += DX[bar] * st * e; y += DY[bar] * st * e; }
			if (bt > lift) y -= 2400 * Math.Pow(B(bt) - B(lift), 2);
			return (x, y);
		}
		double Th(double t) { double bt = BT(t), th = 0; for (int i = 0; ; i++) { double a = tA + i * 4 * SPB, b = a + 4 * SPB; if (t <= a) break; th += (i % 2 == 0 ? 1 : -1) * w * (Math.Min(t, b) - a); if (t < b) break; } return th; }
		double Wd(double bt) => bt < cl ? th0 + (th1 - th0) * BRk(bt, ta, torn) : th1 + (s - th1) * BREase((bt - cl) / 0.35);
		// a screen point in the net's own frame (a, b: along the two line families)
		(double a, double b) loc(double x, double y, double t)
		{
			var o = O(BT(t)); double th = Th(t), lx = x - W / 2 - o.x, ly = y - H / 2 - o.y, c = JS.cos(-th), sn = JS.sin(-th);
			double rx = lx * c - ly * sn, ry = lx * sn + ly * c; return ((rx + ry) / Math.Sqrt(2), (ry - rx) / Math.Sqrt(2));
		}
		var tc = (a: 0.0, b: 0.0); bool tset = false;
		void setTorn() { if (tset) return; tset = true; var l = loc(W / 2 + 0.01, H / 2 + 0.01, B(torn)); tc = (Math.Round(l.a / s) * s, Math.Round(l.b / s) * s); }
		bool inTorn(double a, double b) => a > tc.a - s && a < tc.a + s && b > tc.b - s && b < tc.b + s;
		h.update = (t, dt) => { double bt = BT(t); if (bt >= torn) setTorn(); if (bt > lift + 1.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw || bt >= lift) return; bool on = bt >= ta && bt < lift; double th = Th(t), wd = Wd(bt); var o = O(bt);
			g.save(); g.translate(W / 2 + o.x, H / 2 + o.y); g.rotate(th); g.rotate(Math.PI / 4);
			double ext = Math.Ceiling(1100 / s) * s;
			if (!on && bt < ta)
			{
				g.strokeStyle = BRW(0.25 + 0.4 * BRk(bt, tw, ta)); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				for (double v = -ext; v <= ext; v += s) { g.moveTo(v, -ext); g.lineTo(v, ext); g.moveTo(-ext, v); g.lineTo(ext, v); }
				g.stroke(); g.setLineDash(new double[0]); g.restore(); return;
			}
			if (bt >= lift) g.globalAlpha = 1 - BRk(bt, lift, lift + 1.2);
			g.shadowColor = PINK; g.shadowBlur = on ? 10 : 0; g.strokeStyle = on ? PINK : BRW(0.3); g.lineWidth = Math.Min(wd, s);
			g.beginPath();
			for (double v = -ext; v <= ext; v += s)
			{
				// the lines inside the torn cell are gone
				if (tset && Math.Abs(v - tc.a) < 1) { g.moveTo(v, -ext); g.lineTo(v, tc.b - s); g.moveTo(v, tc.b + s); g.lineTo(v, ext); }
				else { g.moveTo(v, -ext); g.lineTo(v, ext); }
				if (tset && Math.Abs(v - tc.b) < 1) { g.moveTo(-ext, v); g.lineTo(tc.a - s, v); g.moveTo(tc.a + s, v); g.lineTo(ext, v); }
				else { g.moveTo(-ext, v); g.lineTo(ext, v); }
			}
			g.stroke(); g.shadowBlur = 0;
			if (tset && bt < lift)
			{
				double f = (bt * 4) % 1 < 0.5 ? 1 : 0.55; g.strokeStyle = BRW(0.85 * f); g.lineWidth = 3; double ww = Math.Min(wd, s); g.strokeRect(tc.a - s + ww / 2 + 3, tc.b - s + ww / 2 + 3, 2 * s - ww - 6, 2 * s - ww - 6);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= lift) return false; var l = loc(p.x, p.y, t); double wd = Wd(bt), m = Math.Min(wd, s) / 2 + p.r - 2;
			if (tset && inTorn(l.a, l.b)) { double ia = Math.Min(l.a - (tc.a - s), tc.a + s - l.a), ib = Math.Min(l.b - (tc.b - s), tc.b + s - l.b); return ia < m || ib < m; }
			double da = Math.Abs(l.a - s * Math.Round(l.a / s)), db = Math.Abs(l.b - s * Math.Round(l.b / s));
			return da < m || db < m;
		};
		return h;
	}

	// the sea floor (0:15-0:30): a rocky pink band at the bottom, base px high; on every bass hit (kb) it heaves up amp px and sinks back
	// over a beat. Where the next heave reaches shows dashed 0.75 beat before. It rises in from b0 and sinks away at b1
	public static Hz BRReef(double[] kb, double b0, double b1, double bas, double amp)
	{
		var h = new Hz { kind = "BRReef" };
		double heave(double bt) { double v = 0; foreach (var k in kb) { if (bt < k) break; double u = bt - k; double e = u < 0.12 ? u / 0.12 : Math.Max(0, 1 - (u - 0.12) / 1.0); v = Math.Max(v, BREase(e)); } return v; }
		double top(double x, double bt) { double inn = BREase(BRk(bt, b0 - 1, b0)) * (1 - BREase(BRk(bt, b1, b1 + 1))); return H - inn * (bas + amp * heave(bt) + 26 * JS.sin(x * 0.021 + 1.3) + 14 * JS.sin(x * 0.057)); }
		h.update = (t, dt) => { if (BT(t) > b1 + 1.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 1) return;
			// the next heave
			foreach (var k in kb) { if (k <= bt) continue; if (k - bt < 0.75 && bt >= b0) { double q = BRk(bt, k - 0.75, k); g.strokeStyle = BRW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); for (double x = 0; x <= W; x += 32) { double y = H - (bas + amp + 26 * JS.sin(x * 0.021 + 1.3) + 14 * JS.sin(x * 0.057)); if (x == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); g.setLineDash(new double[0]); } break; }
			g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.moveTo(0, H + 10);
			for (double x = 0; x <= W; x += 16) g.lineTo(x, top(x, bt)); g.lineTo(W, H + 10); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(70,0,22,0.35)"; for (double x = 30; x < W; x += 90) { double y = top(x, bt); circle(x, y + 40, 12); circle(x + 40, y + 80, 8); }
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 - 0.5 || bt > b1 + 0.6) return false; return p.y + p.r - 2 > Math.Min(top(p.x - p.r, bt), Math.Min(top(p.x, bt), top(p.x + p.r, bt))); };
		return h;
	}
}
