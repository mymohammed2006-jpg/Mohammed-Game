using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F10 "Walk" (Kwabs), Ghassan's level: a long walk at night, and the walking compass that turns over its own feet.
// part 1 the first steps (the compass, the crossing you walk on the beat), part 2 the long road (the barriers coming down, the compasses
// at the sides, the turning walls), part 3 against the wind (the room turns, the tripod, the rain lanes, your own shadow, the narrow road).
// Only pink hurts. Every hazard is a pure function of time once it exists (the shadow also of where the square has been).
// Drawing: the stage first, then every hazard's look and warnings, then every pink part, so nothing ever covers the pink.
// The data is data/dodge_F10.json (made by tools/friend10/gen.py).
public static partial class G
{
	static double F10k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double F10e(double u) { u = clamp(u, 0, 1); return u * u * (3 - 2 * u); }
	static string F10P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string F10W(double a) => $"rgba(214,224,255,{str(clamp(a, 0, 1))})";   // the cold white of the look and the warnings
	static double F10Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double F10Lin(double[] kb, double[] kv, double bt)
	{
		int n = kb.Length; if (n == 0) return 0; if (bt <= kb[0]) return kv[0]; if (bt >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && bt >= kb[i + 1]) i++;
		return kv[i] + (kv[i + 1] - kv[i]) * (bt - kb[i]) / (kb[i + 1] - kb[i]);
	}
	static double F10h(int i) { unchecked { uint x = (uint)(i * 2654435761u) ^ 0x7f4a7c15u; x ^= x >> 15; x *= 0x85ebca6bu; x ^= x >> 13; return (x % 10000) / 10000.0; } }
	static void F10Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// a sector round (cx, cy), radius r, from angle a0 turning da: a soft fill and a dashed rim (a turn's warning)
	static void F10Sector(double cx, double cy, double r, double a0, double da, double k)
	{
		if (Math.Abs(da) < 1e-4 || r < 2) return; double a1 = a0 + da; bool ccw = da < 0;
		g.fillStyle = F10W(0.03 + 0.09 * k); g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, r, a0, a1, ccw); g.closePath(); g.fill();
		g.strokeStyle = F10W(0.25 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 }); g.beginPath(); g.arc(cx, cy, r, a0, a1, ccw); g.stroke(); g.setLineDash(new double[0]);
	}

	// ---------- the room's turn (part 3): an angle in keyframes; a hazard made with rot = true lives in the turning room ----------
	static double[] F10SpB = new double[0], F10SpA = new double[0];
	static double F10Spin(double t) => F10SpB.Length == 0 ? 0 : F10Lin(F10SpB, F10SpA, BT(t)) * Math.PI / 180;
	// a point on the screen in the room's own frame (turned back by the room's angle a)
	static (double x, double y) F10In(double x, double y, double a)
	{
		if (a == 0) return (x, y); double c = cos(-a), s = sin(-a), dx = x - W / 2, dy = y - H / 2; return (W / 2 + dx * c - dy * s, H / 2 + dx * s + dy * c);
	}
	static void F10Turn(double a) { if (a == 0) return; g.translate(W / 2, H / 2); g.rotate(a); g.translate(-W / 2, -H / 2); }

	// ---------- the layers ----------
	static readonly List<(Hz h, Action<double> d)> F10St = new(), F10Lo = new(), F10Hi = new();
	static void F10Clean(List<(Hz h, Action<double> d)> L) => L.RemoveAll(q => q.h.dead || !hazards.Contains(q.h));
	static readonly HashSet<Hz> F10All = new();
	static void F10Add(Hz h, Action<double> lo, Action<double> hi, bool stage = false)
	{
		if (lo != null) (stage ? F10St : F10Lo).Add((h, lo)); if (hi != null) F10Hi.Add((h, hi));
		F10All.RemoveWhere(q => q.dead); F10All.Add(h);
		// the first of this level's hazards in the list draws the stage and all the looks, the last one all the pink
		h.draw = t =>
		{
			Hz first = null, last = null; foreach (var q in hazards) if (!q.dead && F10All.Contains(q)) { first ??= q; last = q; }
			if (first == h)
			{
				F10Clean(F10St); F10Clean(F10Lo); F10Clean(F10Hi);
				foreach (var q in F10St) q.d(t);
				foreach (var q in F10Lo) q.d(t);
			}
			if (last == h) foreach (var q in F10Hi.ToArray()) q.d(t);
		};
		hazards.Add(h);
	}

	// ---------- the stage (harmless) ----------
	// 1 the street at night, 2 the road (scrolling at v px a beat) then the square, 3 the storm (it turns with the room), 4 the narrow road
	public static Hz F10Stage(int part, double t0, double t1, double v)
	{
		var h = new Hz { kind = "F10Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		F10Add(h, t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; double bt = BT(t); g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var sk = g.createLinearGradient(0, 0, 0, H); sk.addColorStop(0, "#04060f"); sk.addColorStop(1, "#0b1022"); g.fillStyle = sk; g.fillRect(0, 0, W, H);
				// the paving stones and the soft light of lamps far away
				g.strokeStyle = F10W(0.035); g.lineWidth = 1; g.beginPath();
				for (double y = 40; y < H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
				for (int r = 0; r < 9; r++) for (double x = (r % 2) * 60; x < W; x += 120) { g.moveTo(x, 40 + r * 80); g.lineTo(x, 120 + r * 80); }
				g.stroke();
				for (int i = 0; i < 5; i++) { double x = 128 + i * 256; var gr = g.createRadialGradient(x, -30, 4, x, -30, 260); gr.addColorStop(0, "rgba(150,170,255,0.16)"); gr.addColorStop(1, "rgba(150,170,255,0)"); g.fillStyle = gr; g.fillRect(x - 260, 0, 520, 240); }
			}
			else if (part == 2)
			{
				g.fillStyle = "#07090f"; g.fillRect(0, 0, W, H);
				double off = (bt * v) % 120; if (off < 0) off += 120;
				g.fillStyle = F10W(0.07); for (int i = 1; i < 5; i++) for (double y = -120 + off; y < H; y += 120) g.fillRect(i * W / 5 - 3, y, 6, 56);
				g.fillStyle = F10W(0.12); g.fillRect(0, 0, 6, H); g.fillRect(W - 6, 0, 6, H);
			}
			else if (part == 3)
			{
				g.fillStyle = "#05060c"; g.fillRect(0, 0, W, H);
				g.save(); F10Turn(F10Spin(t)); g.strokeStyle = F10W(0.07); g.lineWidth = 1; g.beginPath();
				for (double x = W / 2 - 800; x <= W / 2 + 800; x += 100) { g.moveTo(x, H / 2 - 800); g.lineTo(x, H / 2 + 800); }
				for (double y = H / 2 - 800; y <= H / 2 + 800; y += 100) { g.moveTo(W / 2 - 800, y); g.lineTo(W / 2 + 800, y); }
				g.stroke(); g.strokeStyle = F10W(0.12); g.lineWidth = 2; g.beginPath(); g.arc(W / 2, H / 2, 330, 0, TAU); g.stroke();
				// the compass rose on the floor: it shows the room turning
				g.fillStyle = F10W(0.1); for (int i = 0; i < 8; i++) { double ra = i * Math.PI / 4, r1 = i % 2 == 0 ? 330 : 230; g.beginPath(); g.moveTo(W / 2 + r1 * cos(ra), H / 2 + r1 * sin(ra)); g.lineTo(W / 2 + 26 * cos(ra + 0.5), H / 2 + 26 * sin(ra + 0.5)); g.lineTo(W / 2 + 26 * cos(ra - 0.5), H / 2 + 26 * sin(ra - 0.5)); g.closePath(); g.fill(); }
				g.restore();
				var gr = g.createRadialGradient(W / 2, H / 2, 220, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			else
			{
				g.fillStyle = "#06070d"; g.fillRect(0, 0, W, H);
			}
			g.restore();
		}, null, true);
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F10Fx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F10fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		F10Add(h, null, null); return h;   // through F10Add: the first and the last hazard draw the layers
	}

	// ---------- the walker: the compass (2 legs) and the tripod (3) ----------
	// n legs R long, w thick, from a hub. It stands still; at step k (beat sb[k]) it turns da[k] round its pivot (px, py) in sw beats.
	// (hx, hy, ang) is its pose at each step's start. Grey legs from tIn - 1 s, pink from tIn. Each turn's sweep shows wn beats before.
	public class F10Pose { public double hx, hy, a; }
	public static Hz F10Walker(int n, double R, double w, double[] sb, double[] px, double[] py, double[] hx, double[] hy, double[] ang, double[] da, double sw, double wn, double tIn, double te, bool rot)
	{
		var h = new Hz { kind = "F10Walker" }; int m = sb.Length;
		F10Pose pose(double bt)
		{
			if (m == 0 || bt <= sb[0]) return new F10Pose { hx = hx[0], hy = hy[0], a = ang[0] };
			int k = 0; while (k < m - 1 && bt >= sb[k + 1]) k++;
			double u = F10e((bt - sb[k]) / sw), q = da[k] * u, c = cos(q), s = sin(q), dx = hx[k] - px[k], dy = hy[k] - py[k];
			return new F10Pose { hx = px[k] + dx * c - dy * s, hy = py[k] + dx * s + dy * c, a = ang[k] + q };
		}
		bool moving(double bt) { for (int k = 0; k < m; k++) if (bt >= sb[k] && bt < sb[k] + sw) return true; return false; }
		// the compass: its two legs meet at the hinge, HG out from the middle of its feet; the tripod: three legs from its hub
		double HG = R * 0.55;
		(double x, double y) hinge(F10Pose P) => n == 2 ? (P.hx + HG * cos(P.a - Math.PI / 2), P.hy + HG * sin(P.a - Math.PI / 2)) : (P.hx, P.hy);
		void legs(F10Pose P, string col, double lw)
		{
			var J = hinge(P); g.strokeStyle = col; g.lineWidth = lw; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath();
			for (int j = 0; j < n; j++) { double a = P.a + TAU * j / n; g.moveTo(J.x, J.y); g.lineTo(P.hx + R * cos(a), P.hy + R * sin(a)); }
			g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter"; g.fillStyle = col; circle(J.x, J.y, w * 0.9);
			if (n == 2) { g.lineWidth = w * 0.6; g.lineCap = "round"; g.beginPath(); g.moveTo(J.x, J.y); g.lineTo(J.x + w * 1.6 * cos(P.a - Math.PI / 2), J.y + w * 1.6 * sin(P.a - Math.PI / 2)); g.stroke(); g.lineCap = "butt"; }
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10Add(h, t =>
		{
			if (t < tIn - 1) return; double bt = BT(t), a = 1 - F10k(t, te, te + 0.4); if (a <= 0) return; g.save(); g.globalAlpha = a; F10Turn(rot ? F10Spin(t) : 0);
			if (t < tIn) legs(pose(bt), F10W(0.2 + 0.5 * F10k(t, tIn - 1, tIn)), w);
			// the next turn: each foot's sweep round the pivot, and the pivot ringed
			double wnB = wn;
			for (int k = 0; k < m; k++)
			{
				if (bt < sb[k] - wnB || bt >= sb[k]) continue; double kk = F10k(bt, sb[k] - wnB, sb[k]);
				for (int j = 0; j < n; j++)
				{
					double fa = ang[k] + TAU * j / n, fx = hx[k] + R * cos(fa), fy = hy[k] + R * sin(fa), r = hypot(fx - px[k], fy - py[k]);
					if (r < 4) continue; F10Sector(px[k], py[k], r + w / 2, atan2(fy - py[k], fx - px[k]), da[k], kk);
				}
				g.strokeStyle = F10W(0.4 + 0.5 * kk); g.lineWidth = 2; g.beginPath(); g.arc(px[k], py[k], w * 1.6, 0, TAU); g.stroke();
			}
			// where a foot came down: a white ring
			for (int k = 0; k < m; k++)
			{
				double u = (bt - sb[k] - sw) / 0.7; if (u < 0 || u >= 1) continue; var E = pose(sb[k] + sw);
				double bx = 0, by = 0, bd = -1;
				for (int j = 0; j < n; j++) { double fa = E.a + TAU * j / n, fx = E.hx + R * cos(fa), fy = E.hy + R * sin(fa), d = hypot(fx - px[k], fy - py[k]); if (d > bd) { bd = d; bx = fx; by = fy; } }
				g.strokeStyle = F10W(0.55 * (1 - u)); g.lineWidth = 3; g.beginPath(); g.arc(bx, by, 12 + 46 * u, 0, TAU); g.stroke();
			}
			g.restore();
		}, t =>
		{
			if (t < tIn || t > te) return; double bt = BT(t); g.save(); F10Turn(rot ? F10Spin(t) : 0);
			bool land = false; for (int k = 0; k < m; k++) if (bt >= sb[k] + sw && bt < sb[k] + sw + 0.12) land = true;
			legs(pose(bt), land ? "#fff" : PINK, w);
			g.restore();
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te) return false; var q = F10In(p.x, p.y, rot ? F10Spin(t) : 0); var P = pose(BT(t)); double lim = w / 2 + p.r - 2; var J = hinge(P);
			if (hypot(q.x - J.x, q.y - J.y) < w * 0.9 + p.r - 2) return true;
			if (n == 2 && F10Seg(q.x, q.y, J.x, J.y, J.x + w * 1.6 * cos(P.a - Math.PI / 2), J.y + w * 1.6 * sin(P.a - Math.PI / 2)) < w * 0.3 + p.r - 2) return true;
			for (int j = 0; j < n; j++) { double a = P.a + TAU * j / n; if (F10Seg(q.x, q.y, J.x, J.y, P.hx + R * cos(a), P.hy + R * sin(a)) < lim) return true; }
			return false;
		};
		return h;
	}

	// ---------- part 1: the crossing ----------
	// n stripes over the screen (across x, or across y when horiz). At beat hb[i] the stripes in the bits of mk[i] are pink for on beats;
	// they light up (dashed, then filling) wn beats before. Painted stripes (the look) from tIn to te.
	public static Hz F10Zebra(bool horiz, int n, double[] hb, double[] mk, double on, double wn, double tIn, double te)
	{
		var h = new Hz { kind = "F10Zebra" }; double sw = (horiz ? H : W) / n;
		void rect(int i, double inset) { if (horiz) g.fillRect(0, i * sw + inset, W, sw - 2 * inset); else g.fillRect(i * sw + inset, 0, sw - 2 * inset, H); }
		int now(double bt) { int m = 0; for (int i = 0; i < hb.Length; i++) if (bt >= hb[i] && bt < hb[i] + on) m |= (int)mk[i]; return m; }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		F10Add(h, t =>
		{
			double a = Math.Min(F10k(t, tIn, tIn + 0.5), 1 - F10k(t, te, te + 0.5)); if (a <= 0) return; double bt = BT(t); g.save(); g.globalAlpha = a;
			g.fillStyle = F10W(0.05); for (int i = 1; i < n; i += 2) rect(i, 10);
			for (int k = 0; k < hb.Length; k++)
			{
				if (bt < hb[k] - wn || bt >= hb[k]) continue; double kk = F10k(bt, hb[k] - wn, hb[k]); int b = (int)mk[k];
				for (int i = 0; i < n; i++)
				{
					if ((b >> i & 1) == 0) continue;
					g.fillStyle = F10W(0.04 + 0.14 * kk); rect(i, 0);
					g.strokeStyle = F10W(0.3 + 0.5 * kk); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 });
					if (horiz) g.strokeRect(2, i * sw + 2, W - 4, sw - 4); else g.strokeRect(i * sw + 2, 2, sw - 4, H - 4); g.setLineDash(new double[0]);
				}
			}
			g.restore();
		}, t =>
		{
			double bt = BT(t); int b = now(bt); if (b == 0) return;
			bool fl = false; foreach (var x in hb) if (bt >= x && bt < x + 0.08) fl = true;
			g.fillStyle = fl ? "#fff" : PINK; for (int i = 0; i < n; i++) if ((b >> i & 1) != 0) rect(i, 0);
		});
		h.hit = (p, t) =>
		{
			int b = now(BT(t)); if (b == 0) return false; double v = horiz ? p.y : p.x, e = p.r - 2;
			for (int i = 0; i < n; i++) if ((b >> i & 1) != 0 && v > i * sw - e && v < (i + 1) * sw + e) return true;
			return false;
		};
		return h;
	}

	// ---------- part 2: the road ----------
	// a barrier: comes down from the top at v px a beat from beat b0 (a dashed line with its gaps shows at the top wn beats before),
	// pink across the screen but for its gaps (centres gx, gw wide), th thick
	public static Hz F10Row(double b0, double v, double th, double[] gx, double gw, double wn)
	{
		var h = new Hz { kind = "F10Row" };
		double Y(double bt) => -th + (bt - b0) * v;
		void bars(double y, double hh)
		{
			var xs = gx.OrderBy(x => x).ToArray(); double x0 = 0;
			foreach (var c in xs) { double a = c - gw / 2; if (a > x0) g.fillRect(x0, y, a - x0, hh); x0 = Math.Max(x0, c + gw / 2); }
			if (x0 < W) g.fillRect(x0, y, W - x0, hh);
		}
		h.update = (t, dt) => { if (Y(BT(t)) > H + 10) h.dead = true; };
		F10Add(h, t =>
		{
			double bt = BT(t); if (bt < b0 - wn || bt >= b0 + 0.3) return; double k = F10k(bt, b0 - wn, b0);
			g.fillStyle = F10W(0.08 + 0.2 * k); bars(0, 10);
			g.strokeStyle = F10W(0.4 + 0.5 * k); g.lineWidth = 2; foreach (var c in gx) { g.beginPath(); g.moveTo(c - gw / 2, 0); g.lineTo(c - gw / 2, 26); g.moveTo(c + gw / 2, 0); g.lineTo(c + gw / 2, 26); g.stroke(); }
		}, t =>
		{
			double bt = BT(t); if (bt < b0) return; g.fillStyle = PINK; bars(Y(bt), th);
		});
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; double y = Y(bt), e = p.r - 2; if (p.y < y - e || p.y > y + th + e) return false;
			foreach (var c in gx) if (Math.Abs(p.x - c) < gw / 2 - e) return false;
			return true;
		};
		return h;
	}

	// the turning walls: a bar L long, w thick, on each pivot (x[i], y[i]) at angle a0[i] (degrees); at each step k it turns da[k] degrees
	// in sw[k] beats, from beat sb[k] + dl[i] (the domino). The part it will sweep shows wn[k] beats before. Grey from tIn - 1 s, pink from tIn.
	public static Hz F10Turns(double[] x, double[] y, double[] a0, double[] dl, double L, double w, double[] sb, double[] da, double[] sw, double[] wn, double tIn, double te)
	{
		var h = new Hz { kind = "F10Turns" }; int n = x.Length;
		double ang(int i, double bt)
		{
			double a = a0[i]; for (int k = 0; k < sb.Length; k++) { double s = sb[k] + dl[i]; if (bt <= s) break; a += da[k] * F10e((bt - s) / sw[k]); }
			return a * Math.PI / 180;
		}
		void bars(double bt, string col)
		{
			g.strokeStyle = col; g.lineWidth = w; g.lineCap = "round"; g.beginPath();
			for (int i = 0; i < n; i++) { double a = ang(i, bt); g.moveTo(x[i], y[i]); g.lineTo(x[i] + L * cos(a), y[i] + L * sin(a)); }
			g.stroke(); g.lineCap = "butt"; g.fillStyle = col; for (int i = 0; i < n; i++) circle(x[i], y[i], w * 0.9);
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10Add(h, t =>
		{
			if (t < tIn - 1 || t > te) return; double bt = BT(t);
			if (t < tIn) bars(bt, F10W(0.2 + 0.5 * F10k(t, tIn - 1, tIn)));
			for (int k = 0; k < sb.Length; k++)
				for (int i = 0; i < n; i++)
				{
					double s = sb[k] + dl[i]; if (bt < s - wn[k] || bt >= s) continue;
					F10Sector(x[i], y[i], L + w / 2, ang(i, s), da[k] * Math.PI / 180, F10k(bt, s - wn[k], s));
				}
		}, t =>
		{
			if (t < tIn || t > te) return; double a = 1 - F10k(t, te - 0.2, te); if (a <= 0) return; bars(BT(t), PINK);
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te - 0.2) return false; double bt = BT(t), lim = w / 2 + p.r - 2;
			for (int i = 0; i < n; i++)
			{
				if (Math.Abs(p.x - x[i]) > L + w || Math.Abs(p.y - y[i]) > L + w) continue; double a = ang(i, bt);
				if (F10Seg(p.x, p.y, x[i], y[i], x[i] + L * cos(a), y[i] + L * sin(a)) < lim) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- part 3: against the wind ----------
	// the rain: in the turning room, columns of pink streaks sp apart fall along the wind (angle keyframes kb/ka in degrees), streak sl long
	// every per px, v px a second. The lanes between the columns are dry. The next wind's lanes show (dashed) wn beats before it turns.
	public static Hz F10Rain(double[] kb, double[] ka, double sp, double sl, double per, double v, double sw, double wn, double tIn, double te)
	{
		var h = new Hz { kind = "F10Rain" }; const double RR = 760;
		double wind(double bt) => F10Lin(kb, ka, bt) * Math.PI / 180;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10Add(h, t =>
		{
			if (t < tIn - 1.2 || t > te) return; double bt = BT(t); g.save(); F10Turn(F10Spin(t));
			// the coming wind's lanes, and before the rain starts its first lanes
			for (int i = 0; i < kb.Length - 1; i++)
			{
				bool turn = Math.Abs(ka[i + 1] - ka[i]) > 0.5; double s = turn ? kb[i] : -1; if (i == 0 && bt < BT(tIn)) s = BT(tIn);
				if (s < 0 || bt < s - wn || bt >= s) continue; double k = F10k(bt, s - wn, s), a = (ka[turn ? i + 1 : i]) * Math.PI / 180, c = cos(a), sn = sin(a);
				for (double q = -RR; q <= RR; q += sp) F10Dash(W / 2 - sn * q - c * RR, H / 2 + c * q - sn * RR, W / 2 - sn * q + c * RR, H / 2 + c * q + sn * RR, F10W(0.15 + 0.4 * k), 2);
			}
			g.restore();
		}, t =>
		{
			if (t < tIn || t > te) return; double bt = BT(t), a = wind(bt), c = cos(a), sn = sin(a), u0 = ((t - tIn) * v) % per; g.save(); F10Turn(F10Spin(t));
			g.strokeStyle = PINK; g.lineWidth = sw; g.beginPath();
			for (int ci = -(int)(RR / sp); ci <= (int)(RR / sp); ci++)
				for (double q = ci * sp, u = u0 + (ci & 1) * per / 2 - per * Math.Ceiling(RR / per + 1); u < RR; u += per)   // u = u0 (mod per), as in hit
				{
					double x0 = W / 2 - sn * q + c * u, y0 = H / 2 + c * q + sn * u; g.moveTo(x0, y0); g.lineTo(x0 + c * sl, y0 + sn * sl);
				}
			g.stroke(); g.restore();
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te) return false; double bt = BT(t), a = wind(bt), c = cos(a), sn = sin(a); var P = F10In(p.x, p.y, F10Spin(t));
			double dx = P.x - W / 2, dy = P.y - H / 2, q = -sn * dx + c * dy, u = c * dx + sn * dy, e = p.r - 2;
			int ci = (int)Math.Round(q / sp); if (Math.Abs(q - ci * sp) > sw / 2 + e) return false;
			double u0 = ((t - tIn) * v) % per + (ci & 1) * per / 2, uu = (u - u0) % per; if (uu < 0) uu += per;
			return uu < sl + e || uu > per - e;
		};
		return h;
	}

	// your shadow: from ta it walks your own path dl seconds behind you (dl from keyframes kb/kd), a pink square s wide. From tw its
	// grey outline stands where it will start; the path it will walk (where you have been) shows as a dotted line.
	public class F10Mark { public double t, x, y; }
	public static Hz F10Shadow(double tw, double ta, double te, double[] kb, double[] kd, double s)
	{
		var h = new Hz { kind = "F10Shadow" }; var M = new List<F10Mark>();
		double dl(double t) => F10Lin(kb, kd, BT(t));
		(double x, double y) at(double t)
		{
			if (M.Count == 0) return (player?.x ?? W / 2, player?.y ?? H / 2); double q = t - dl(t);
			if (q <= M[0].t) return (M[0].x, M[0].y); if (q >= M[^1].t) return (M[^1].x, M[^1].y);
			int lo = 0, hi = M.Count - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (M[m].t <= q) lo = m; else hi = m; }
			double u = (q - M[lo].t) / Math.Max(1e-9, M[hi].t - M[lo].t); return (M[lo].x + (M[hi].x - M[lo].x) * u, M[lo].y + (M[hi].y - M[lo].y) * u);
		}
		h.update = (t, dt) =>
		{
			if (t > te + 0.4) { h.dead = true; return; }
			if (t >= tw - 2 && t < te && player != null && state != "dead") M.Add(new F10Mark { t = t, x = player.x, y = player.y });
			while (M.Count > 2 && M[1].t < t - 4) M.RemoveAt(0);
		};
		F10Add(h, t =>
		{
			if (t < tw || t > te) return; var P = at(t); double k = F10k(t, tw, ta);
			// the path it will walk
			double q = t - dl(t); g.fillStyle = F10W(0.35); int c = 0;
			foreach (var m in M) { if (m.t < q) continue; if (c++ % 6 == 0) g.fillRect(m.x - 2, m.y - 2, 4, 4); }
			if (t < ta) { g.strokeStyle = F10W(0.3 + 0.6 * k); g.lineWidth = 2; g.setLineDash(new double[] { 6, 5 }); g.strokeRect(P.x - s / 2, P.y - s / 2, s, s); g.setLineDash(new double[0]); }
		}, t =>
		{
			if (t < ta || t > te) return; var P = at(t); double a = 1 - F10k(t, te - 0.25, te); if (a <= 0) return;
			g.fillStyle = t < ta + 0.08 ? "#fff" : PINK; g.fillRect(P.x - s / 2, P.y - s / 2, s, s);
		});
		h.hit = (p, t) =>
		{
			if (t < ta || t > te - 0.25) return false; var P = at(t); double e = s / 2 + p.r - 2;
			return Math.Abs(p.x - P.x) < e && Math.Abs(p.y - P.y) < e;
		};
		return h;
	}

	// the narrow road: all but the road is pink. Its middle x = W/2 + A sin((y + v bt) / lam turns), its width wd (keyframes kb: kw, ka).
	// Where the edges will be one beat later shows as dashed lines. From tIn to te.
	public static Hz F10Road(double[] kb, double[] kw, double[] ka, double v, double lam, double tIn, double te)
	{
		var h = new Hz { kind = "F10Road" };
		double cx(double y, double bt) => W / 2 + F10Lin(kb, ka, bt) * sin((y + v * bt) / lam * TAU);
		double wd(double bt) => F10Lin(kb, kw, bt);
		void walls(double bt, bool left)
		{
			g.beginPath(); g.moveTo(left ? 0 : W, -4);
			for (double y = -4; y <= H + 4; y += 6) g.lineTo(cx(y, bt) + (left ? -1 : 1) * wd(bt) / 2, y);
			g.lineTo(left ? 0 : W, H + 4); g.closePath(); g.fill();
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		F10Add(h, t =>
		{
			if (t < tIn - 1 || t > te) return; double bt = BT(t) + 1;
			g.strokeStyle = F10W(0.45); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 });
			foreach (var sd in new[] { -1.0, 1.0 }) { g.beginPath(); for (double y = -4; y <= H + 4; y += 8) { double x = cx(y, bt) + sd * wd(bt) / 2; if (y < 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
			g.setLineDash(new double[0]);
			double b0 = BT(t); g.fillStyle = F10W(0.1); for (double y = -((b0 * v) % 60); y < H; y += 60) g.fillRect(cx(y, b0) - 2, y, 4, 26);
		}, t =>
		{
			if (t < tIn || t > te) return; double bt = BT(t); g.fillStyle = PINK; walls(bt, true); walls(bt, false);
		});
		h.hit = (p, t) =>
		{
			if (t < tIn || t > te) return false; double bt = BT(t); return Math.Abs(p.x - cx(p.y, bt)) > wd(bt) / 2 - (p.r - 2);
		};
		return h;
	}

	// ---------- the kinds ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F10wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F10ez(double v, double ez) => easy ? v * ez : v;
	static bool F10skip(DodgeEv e) => easy && e.flag("ezskip");

	static readonly int _kf10stage = DodgeKind("F10stage", e => () => F10Stage(e.i("part"), B(e.b), B(e.num("to")), e.num("v")));
	static readonly int _kf10fx = DodgeKind("F10fx", e => () => F10Fx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2))));
	static readonly int _kf10spin = DodgeKind("F10spin", e => { F10SpB = e.nums("kb"); F10SpA = e.nums("ka"); if (easy) F10SpA = F10SpA.Select(a => a * 0.7).ToArray(); return () => { }; });
	static readonly int _kf10walk = DodgeKind("F10walk", e => { double wn = F10wn(e); return () =>
	{
		if (F10skip(e)) return;
		F10Walker(e.i("n", 2), e.num("R"), F10ez(e.num("w"), 0.8), e.nums("sb"), e.nums("px"), e.nums("py"), e.nums("hx"), e.nums("hy"), e.nums("ang"), e.nums("da"),
			e.num("sw") * (easy ? 1.15 : 1), wn, B(e.num("tin")), B(e.num("to")), e.flag("rot"));
	}; });
	static readonly int _kf10zebra = DodgeKind("F10zebra", e => { double wn = F10wn(e); return () =>
	{
		var hb = e.nums("hb"); var mk = e.nums("mk");
		if (easy) { var ez = e.nums("ezmk"); if (ez.Length == mk.Length) mk = ez; }
		F10Zebra(e.flag("horiz"), e.i("n"), hb, mk, F10ez(e.num("on"), 0.75), wn, B(e.b), B(e.num("to")));
	}; });
	static readonly int _kf10row = DodgeKind("F10row", e => { double wn = F10wn(e); return () =>
	{
		if (F10skip(e)) return; F10Row(e.num("b0"), e.num("v"), F10ez(e.num("th"), 0.8), e.nums("gx"), F10ez(e.num("gw"), 1.35), wn);
	}; });
	static readonly int _kf10turns = DodgeKind("F10turns", e =>
	{
		double ew = easy ? ezV("ezwn", EZ_WN) : 1; var sw = e.nums("sw").Select(v => v * (easy ? 1.15 : 1)).ToArray(); var wn = e.nums("wn").Select(v => v * ew).ToArray();
		return () => F10Turns(e.nums("x"), e.nums("y"), e.nums("a0"), e.nums("dl"), F10ez(e.num("L"), 0.85), F10ez(e.num("w"), 0.8), e.nums("sb"), e.nums("da"), sw, wn, B(e.num("tin")), B(e.num("to")));
	});
	static readonly int _kf10rain = DodgeKind("F10rain", e => { double wn = F10wn(e); return () =>
	{
		F10Rain(e.nums("kb"), e.nums("ka"), F10ez(e.num("sp"), 1.3), e.num("sl"), F10ez(e.num("per"), 1.25), F10ez(e.num("v"), 0.8), F10ez(e.num("sw"), 0.8), wn, B(e.num("tin")), B(e.num("to")));
	}; });
	static readonly int _kf10shadow = DodgeKind("F10shadow", e => () =>
	{
		var kd = e.nums("kd"); if (easy) kd = kd.Select(d => d * 1.35).ToArray();
		F10Shadow(B(e.num("tw")), B(e.num("ta")), B(e.num("to")), e.nums("kb"), kd, F10ez(e.num("s"), 0.8));
	});
	static readonly int _kf10road = DodgeKind("F10road", e => () =>
	{
		var kw = e.nums("kw"); if (easy) kw = kw.Select(x => Math.Min(W * 1.6, x * 1.3)).ToArray();
		var ka = e.nums("ka"); if (easy) ka = ka.Select(x => x * 0.75).ToArray();
		F10Road(e.nums("kb"), kw, ka, F10ez(e.num("v"), 0.8), e.num("lam"), B(e.num("tin")), B(e.num("to")));
	});
}
