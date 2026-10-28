using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L19 "عافني وراح" (Ahmed Jawad), the second design (10-28): "the time after him".
// Part 1 the clock while you wait (it strikes, its hands tick, its numbers fly back, it breaks),
// part 2 the night he left (his footsteps and the cracks they leave, the doors he slams, the train, the car's lights, the road he took,
// the doors turning round you), part 3 after him (rain on the window, the gears of time going on, then the clock strikes midnight).
// Every attack is fixed on the beat (nothing aimed, nothing random) and every hazard is a pure function of time once it exists.
// Grid: 99.915 BPM, beat 0 = 0.344 s of the song file (it starts at 0:18 of the song); checkpoints 0, 50, 166; win 239.25 (2:42).
// The first design (v49-v51) stays in L19.cs with its data in data/dodge_L19_v51.json, for the replays recorded with it.
public static partial class G
{
	// one piece of an attack: k 0 a bar (x0,y0)-(x1,y1) w wide with round ends, 1 a disc (x0,y0) r, 2 an arc round (x0,y0) of radius r, w wide,
	// from a0 to a1 (a1 > a0), 3 a cone from (x0,y0) towards a0, a1 each side, r long, 4 a polygon (xs, ys)
	public class L19bP { public int k; public double x0, y0, x1, y1, w, r, a0, a1; public double[] xs, ys; }
	static L19bP L19bBar(double x0, double y0, double x1, double y1, double w) => new L19bP { k = 0, x0 = x0, y0 = y0, x1 = x1, y1 = y1, w = w };
	static L19bP L19bDisc(double x, double y, double r) => new L19bP { k = 1, x0 = x, y0 = y, r = r };
	static L19bP L19bArc(double x, double y, double r, double w, double a0, double a1) => new L19bP { k = 2, x0 = x, y0 = y, r = r, w = w, a0 = a0, a1 = a1 };
	static L19bP L19bCone(double x, double y, double dir, double half, double len) => new L19bP { k = 3, x0 = x, y0 = y, a0 = dir, a1 = half, r = len };
	static L19bP L19bPoly(double[] xs, double[] ys) => new L19bP { k = 4, xs = xs, ys = ys };
	// a bar from the middle point (cx,cy) at angle a, from r0 to r1
	static L19bP L19bRay(double cx, double cy, double a, double r0, double r1, double w) => L19bBar(cx + JS.cos(a) * r0, cy + JS.sin(a) * r0, cx + JS.cos(a) * r1, cy + JS.sin(a) * r1, w);
	// a ring with one gap of half-width gh round the angle ga
	static L19bP L19bGapRing(double x, double y, double r, double w, double ga, double gh) => L19bArc(x, y, r, w, ga + gh, ga - gh + TAU);

	static double L19bE(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double L19bAng(double a) { a %= TAU; return a < 0 ? a + TAU : a; }
	const double L19bD = Math.PI / 180;

	static bool L19bTouch(L19bP q, double px, double py, double m)
	{
		switch (q.k)
		{
			case 0: return L19Seg(px, py, q.x0, q.y0, q.x1, q.y1) < q.w / 2 + m;
			case 1: return hypot(px - q.x0, py - q.y0) < q.r + m;
			case 2:
			{
				double d = hypot(px - q.x0, py - q.y0);
				if (Math.Abs(d - q.r) >= q.w / 2 + m) return false;
				if (L19bAng(atan2(py - q.y0, px - q.x0) - q.a0) <= q.a1 - q.a0) return true;
				foreach (var a in new[] { q.a0, q.a1 }) if (hypot(px - q.x0 - JS.cos(a) * q.r, py - q.y0 - JS.sin(a) * q.r) < q.w / 2 + m) return true;
				return false;
			}
			case 3:
			{
				double d = hypot(px - q.x0, py - q.y0); if (d > q.r + m) return false; if (d < m + 1) return true;
				double da = Math.Abs(L19bAng(atan2(py - q.y0, px - q.x0) - q.a0 + Math.PI) - Math.PI);
				return da <= q.a1 + Math.Asin(Math.Min(1, m / d));
			}
			default: return L19Touch(px, py, q.xs, q.ys, m);
		}
	}
	static void L19bShapePath(L19bP q)
	{
		g.beginPath();
		switch (q.k)
		{
			case 0: g.moveTo(q.x0, q.y0); g.lineTo(q.x1, q.y1); break;
			case 1: g.arc(q.x0, q.y0, Math.Max(0, q.r), 0, TAU); break;
			case 2: g.arc(q.x0, q.y0, Math.Max(0, q.r), q.a0, q.a1); break;
			case 3: g.moveTo(q.x0, q.y0); g.arc(q.x0, q.y0, q.r, q.a0 - q.a1, q.a0 + q.a1); g.closePath(); break;
			default: for (int i = 0; i < q.xs.Length; i++) { if (i == 0) g.moveTo(q.xs[i], q.ys[i]); else g.lineTo(q.xs[i], q.ys[i]); } g.closePath(); break;
		}
	}
	// pink: bars and arcs are strokes, the rest fills
	static void L19bPink(L19bP q)
	{
		L19bShapePath(q);
		if (q.k == 0 || q.k == 2) { g.lineWidth = q.w; g.stroke(); } else g.fill();
	}
	// where it will be: a faint grey body and a dashed grey line (harmless)
	static void L19bGhost(L19bP q, double a)
	{
		string cl = GREY + str(clamp(a, 0, 1)) + ")", cf = GREY + str(clamp(a * 0.2, 0, 1)) + ")";
		L19bShapePath(q);
		if (q.k == 0 || q.k == 2) { g.strokeStyle = cf; g.lineWidth = q.w; g.stroke(); } else { g.fillStyle = cf; g.fill(); }
		g.strokeStyle = cl; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 });
		if (q.k == 1) { g.beginPath(); g.arc(q.x0, q.y0, Math.Max(0, q.r), 0, TAU); g.stroke(); }
		else { L19bShapePath(q); g.stroke(); }
		g.setLineDash(new double[0]);
	}

	// An attack: f(t) gives its pieces. From tw it shows grey where it will be at ta (or pre(t), a moving preview), it hurts (pink) from ta
	// to te, then a grey shadow fades. look(t) adds harmless grey marks while it is out (where a hand goes next). The grey parts are their own
	// harmless hazard, so they are drawn under every pink thing.
	static void L19bAdd(Func<double, List<L19bP>> f, double tw, double ta, double te, Func<double, List<L19bP>> pre = null, Func<double, List<(L19bP q, double a)>> look = null, double fade = 0.25)
	{
		double ct = double.NaN; List<L19bP> cl = null;
		List<L19bP> F(double t) { if (t != ct) { ct = t; cl = f(t); } return cl; }
		var w = new Hz { kind = "L19bW" };
		w.update = (t, dt) => { if (t > te + fade + 0.1) w.dead = true; };
		w.draw = t =>
		{
			if (t < tw) return;
			g.save(); g.lineCap = "round";
			if (t < ta) { double k = clamp((t - tw) / Math.Max(0.01, ta - tw), 0, 1); foreach (var q in pre != null ? pre(t) : f(ta)) L19bGhost(q, 0.3 + 0.55 * k); }
			else if (t > te) { if (t < te + fade) foreach (var q in f(te)) L19bGhost(q, 0.55 * (1 - (t - te) / fade)); }
			if (look != null && t <= te) foreach (var (q, a) in look(t)) L19bGhost(q, a);
			g.restore();
		};
		var h = new Hz { kind = "L19bH" };
		h.update = (t, dt) => { if (t > te + 0.1) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta || t > te) return;
			g.save(); g.fillStyle = PINK; g.strokeStyle = PINK; g.lineCap = "round"; g.lineJoin = "round"; g.shadowColor = PINK; g.shadowBlur = 12;
			foreach (var q in F(t)) L19bPink(q);
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t > te) return false; double m = p.r - 2; foreach (var q in F(t)) if (L19bTouch(q, p.x, p.y, m)) return true; return false; };
		hazards.Add(w); hazards.Add(h);
	}
	static List<L19bP> L19bL(params L19bP[] a) => a.ToList();

	// ---------- the room (harmless, under everything): part 1 the clock's face, 2 the street at night, 3 his empty room ----------
	public static Hz L19bStage(double[] pb, double end)
	{
		var h = new Hz { kind = "L19bStage", z = -1, veil = true };
		h.update = (t, dt) => { if (BT(t) > end + 8) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); int part = bt < pb[1] ? 1 : bt < pb[2] ? 2 : 3;
			double fade = 0; for (int i = 1; i < pb.Length; i++) fade = Math.Max(fade, 1 - Math.Abs(bt - pb[i]) / 1.0); fade = clamp(fade, 0, 1);
			g.save();
			if (part == 1)
			{
				var gr = g.createRadialGradient(W / 2, H / 2, 40, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(28,30,58,1)"); gr.addColorStop(1, "rgba(10,10,24,1)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				// the face: its ring and the twelve marks (the marks are gone once they fell, from 0:40)
				g.strokeStyle = $"rgba(150,160,220,{str(0.16 + 0.08 * pulse)})"; g.lineWidth = 6; g.beginPath(); g.arc(W / 2, H / 2, 330, 0, TAU); g.stroke();
				g.lineWidth = 2; g.beginPath(); g.arc(W / 2, H / 2, 300, 0, TAU); g.stroke();
				g.strokeStyle = "rgba(150,160,220,0.28)"; g.lineWidth = 8; g.lineCap = "round"; g.beginPath();
				for (int i = 0; i < 12; i++) { if (bt >= 37 + 0.5 * i - 1 && bt < 46) continue; if (bt >= 46) continue; double a = -Math.PI / 2 + i * TAU / 12; g.moveTo(W / 2 + JS.cos(a) * 300, H / 2 + JS.sin(a) * 300); g.lineTo(W / 2 + JS.cos(a) * 330, H / 2 + JS.sin(a) * 330); }
				g.stroke();
			}
			else if (part == 2)
			{
				var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "rgba(14,14,30,1)"); gr.addColorStop(1, "rgba(30,22,38,1)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
				// the street lamps' light, far away
				for (int i = 0; i < 5; i++) { double x = 128 + i * 256; var lg = g.createRadialGradient(x, -20, 10, x, -20, 260); lg.addColorStop(0, $"rgba(255,214,150,{str(0.10 + 0.04 * pulse)})"); lg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = lg; g.fillRect(x - 260, 0, 520, 280); }
				g.strokeStyle = "rgba(160,150,200,0.07)"; g.lineWidth = 2; g.beginPath(); for (double y = 80; y < H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); } g.stroke();
			}
			else
			{
				g.fillStyle = "rgba(20,16,24,1)"; g.fillRect(0, 0, W, H);
				var lt = g.createRadialGradient(W / 2, H / 2, 60, W / 2, H / 2, 700); lt.addColorStop(0, "rgba(80,90,130,0.22)"); lt.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = lt; g.fillRect(0, 0, W, H);
				// the window's frame
				g.strokeStyle = "rgba(170,170,210,0.12)"; g.lineWidth = 10; g.strokeRect(70, 50, W - 140, H - 100);
			}
			if (fade > 0) { g.fillStyle = $"rgba(0,0,0,{str(fade * 0.8)})"; g.fillRect(0, 0, W, H); }
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}
	public static Hz L19bFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "L19bFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } if (t >= t0) { flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); } };
		return h;
	}

	// ---------- the events ----------
	static readonly int _l19b = DodgeBuild("L19", () => buildL19b());
	static List<Ev> buildL19b()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void fx(double b, double v = 0.3, double s = 10, double dur = 1.2) => at(b - 0.05, () => hazards.Add(L19bFx(b, v, s, dur)));
		// an attack whose warning starts at beat bw: it fires a little before so the grey shows from bw
		void atk(double bw, Action f) => at(bw - 0.1, f);
		double WN = ez ? 1.3 : 1;            // warnings are longer on Easy
		double SP = ez ? 0.78 : 1;           // and things are slower
		double TH = ez ? 0.85 : 1;           // and thinner
		double cx = W / 2, cy = H / 2;
		at(0, () => hazards.Add(L19bStage(new double[] { 0, 50, 166 }, 239.25)), true);

		// ================= part 1: the clock (0:18-0:48) =================
		// 0:19 it strikes: a ring from the middle on each strike with one gap; the gap goes round the clock (from where you start)
		{
			double[] kb = { 2, 3, 4, 5, 6 }; double v = 600 * SP, gh = (ez ? 36 : 27) * L19bD;
			for (int i = 0; i < kb.Length; i++)
			{
				if (ez && i == 3) continue;
				double kh = kb[i], ga = Math.PI + i * 60 * L19bD, ta = B(kh);
				atk(kh - 1.5 * WN, () => L19bAdd(t => L19bL(L19bGapRing(cx, cy, 40 + v * Math.Max(0, t - ta), 24 * TH, ga, gh)), B(kh - 1.5 * WN), ta, ta + 1.4,
					pre: t => L19bL(L19bGapRing(cx, cy, 300, 24 * TH, ga, gh))));
			}
			fx(2, 0.25, 8, 0.8);
		}
		// 0:22 the hands tick: the second hand jumps on every beat, the minute hand the other way every 2 beats (where each goes next shows
		// grey). From 0:27 the second hand goes right through the clock (both ways)
		{
			double k0 = 6.5, k1 = 22, sw = 0.2;
			var sB = new List<double>(); for (double k = 7; k < k1 - 0.5; k += 1) sB.Add(k);
			var mB = new List<double>(); for (double k = 8; k < k1 - 0.5; k += 2) mB.Add(k);
			double sStep = 30 * L19bD, mStep = -45 * L19bD, s0 = -90 * L19bD, m0 = 90 * L19bD, len = 820, wd = 16 * TH;
			double ang(double bt, double a0, List<double> kb, double st) { double a = a0; foreach (var b in kb) { if (bt < b - sw) break; a += st * L19bE((bt - (b - sw)) / sw); } return a; }
			double next(double bt, double a0, List<double> kb, double st) { double a = a0; foreach (var b in kb) { a += st; if (bt < b - sw) return a; } return a; }
			bool dbl(double bt) => !ez && bt >= 14 - 0.2;
			List<L19bP> hands(double t)
			{
				double bt = BT(t), a = ang(bt, s0, sB, sStep), m = ang(bt, m0, mB, mStep);
				var l = L19bL(L19bRay(cx, cy, a, 0, len, wd), L19bRay(cx, cy, m, 0, len, wd), L19bDisc(cx, cy, 30));
				if (dbl(bt)) l.Add(L19bRay(cx, cy, a + Math.PI, 0, len, wd));
				return l;
			}
			List<(L19bP, double)> look(double t)
			{
				double bt = BT(t), a = ang(bt, s0, sB, sStep), an = next(bt, s0, sB, sStep), m = ang(bt, m0, mB, mStep), mn = next(bt, m0, mB, mStep);
				var l = new List<(L19bP, double)>();
				// the next jump's sweep shows from 0.8 beat before it
				double ns = sB.FirstOrDefault(b => b - sw > bt, -1), nm = mB.FirstOrDefault(b => b - sw > bt, -1);
				if (ns > 0 && ns - sw - bt < 1.0 * WN) { double ka = 1 - (ns - sw - bt) / (1.0 * WN); l.Add((L19bCone(cx, cy, (a + an) / 2, Math.Abs(an - a) / 2, len), 0.25 + 0.4 * ka)); if (dbl(ns)) l.Add((L19bCone(cx, cy, (a + an) / 2 + Math.PI, Math.Abs(an - a) / 2, len), 0.25 + 0.4 * ka)); }
				if (nm > 0 && nm - sw - bt < 1.2 * WN) { double ka = 1 - (nm - sw - bt) / (1.2 * WN); l.Add((L19bCone(cx, cy, (m + mn) / 2, Math.Abs(mn - m) / 2, len), 0.25 + 0.4 * ka)); }
				return l;
			}
			atk(k0 - 1.5, () => L19bAdd(hands, B(k0 - 1.5), B(k0), B(k1), look: look));
			// the second arm's own warning (0:26)
			if (!ez) atk(12, () => L19bAdd(t => new List<L19bP>(), B(12), B(13.8), B(13.8), pre: t => { double a = ang(14, s0, sB, sStep); return L19bL(L19bRay(cx, cy, a + Math.PI, 0, len, wd)); }));
		}
		// 0:31 time goes back: the numbers leave the clock one by one (twelve, eleven, ten...), each flies through the middle and out the
		// other side (its line shows a beat before). A long hand turns slowly the whole time. From 0:35 two numbers at once
		{
			double v = 1050 * SP, r = 26 * TH;
			for (int i = 0; i < 14; i++)
			{
				double kh = 22.5 + i;
				void num(int hr, double kh2)
				{
					double a = -Math.PI / 2 + hr * TAU / 12, ta = B(kh2), dx = JS.cos(a), dy = JS.sin(a);
					atk(kh2 - 1.1 * WN, () => L19bAdd(t => { double d = 320 - v * (t - ta); return L19bL(L19bDisc(cx + dx * d, cy + dy * d, r)); }, B(kh2 - 1.1 * WN), ta, ta + 1.2,
						pre: t => L19bL(L19bBar(cx + dx * 320, cy + dy * 320, cx - dx * 900, cy - dy * 900, 2 * r))));
				}
				int h = ((12 - i) % 12 + 12) % 12;
				if (ez && i % 2 == 1) continue;
				num(h, kh);
				if (!ez && i >= 6) num((h + 4) % 12, kh + 0.5);
			}
			var sp = new L19Spin(0, 21, 37, new double[] { 21, 29 }, new[] { 14 * L19bD * SP, -14 * L19bD * SP });
			atk(21, () => L19bAdd(t => L19bL(L19bRay(cx, cy, sp.At(BT(t)), 60, 820, 18 * TH)), B(21), B(22.5), B(36), pre: t => L19bL(L19bRay(cx, cy, sp.At(22.5), 60, 820, 18 * TH))));
		}
		// 0:40 the clock breaks: one long hand right through it turns, and turns back every 4 beats; the twelve marks fall off one after
		// another (each shows its fall a beat before). 0:44 the rim closes in twice, its gap going round
		{
			var sp = new L19Spin(0, 36, 47, new double[] { 36, 40, 44 }, new[] { 24 * L19bD * SP, -28 * L19bD * SP, 30 * L19bD * SP });
			atk(35.5, () => L19bAdd(t => { double a = sp.At(BT(t)); return L19bL(L19bRay(cx, cy, a, -900, 900, 18 * TH)); }, B(35.5), B(37), B(45), pre: t => L19bL(L19bRay(cx, cy, sp.At(37), -900, 900, 18 * TH))));
			double gy = 1700 * SP;
			for (int i = 0; i < 12; i++)
			{
				if (ez && i % 2 == 1) continue;
				double kh = 37 + 0.5 * i, a = -Math.PI / 2 + i * TAU / 12, x0 = cx + JS.cos(a) * 315, y0 = cy + JS.sin(a) * 315, ta = B(kh), ca = JS.cos(a), sa = JS.sin(a);
				atk(kh - 1.2 * WN, () => L19bAdd(t =>
				{
					double u = Math.Max(0, t - ta), y = y0 + 0.5 * gy * u * u, sp2 = a + u * 5;
					return L19bL(L19bBar(x0 - JS.cos(sp2) * 26, y - JS.sin(sp2) * 26, x0 + JS.cos(sp2) * 26, y + JS.sin(sp2) * 26, 20 * TH));
				}, B(kh - 1.2 * WN), ta, ta + 1.4, pre: t => L19bL(L19bBar(x0 - ca * 26, y0 - sa * 26, x0 + ca * 26, y0 + sa * 26, 20 * TH), L19bBar(x0, y0, x0, H + 40, 52))));
			}
			// the rim closes: a ring from the edge to the middle with one gap that turns (the second ring turns the other way)
			void close(double k0, double k1, double ga0, double gw)
			{
				double ta = B(k0), te = B(k1), gh = (ez ? 34 : 25) * L19bD;
				Func<double, L19bP> rg = t => { double k = clamp((t - ta) / (te - ta), 0, 1), r = 760 * (1 - k); return L19bGapRing(cx, cy, Math.Max(1, r), 26 * TH, ga0 + gw * (BT(t) - k0), gh); };
				atk(k0 - 1.5 * WN, () => L19bAdd(t => L19bL(rg(t)), B(k0 - 1.5 * WN), ta, te, pre: t => L19bL(L19bGapRing(cx, cy, 340, 26 * TH, ga0, gh))));
			}
			close(45, 48.5, -90 * L19bD, 50 * L19bD * SP);
			if (!ez) close(46.5, 49.8, 90 * L19bD, -60 * L19bD);
			fx(49.8, 0.35, 12, 1.4);
		}

		// ================= part 2: the night he left (0:48-1:58) =================
		// 0:49 his footsteps: a step on every beat (its print shows grey first), and on every second step the floor cracks from the print
		// to the far side. One walks across the top to the left, then one across the bottom to the right and one across the top again
		{
			double fl = 1.25 * WN, wet = ez ? 1.0 : 1.25, cw = 14 * TH;
			void walk(double k0, int n, double xa, double xb, double y, int crackDir, int every)
			{
				double dx = (xb - xa) / (n - 1), dir = Math.Sign(xb - xa);
				for (int i = 0; i < n; i++)
				{
					double kh = k0 + i, x = xa + dx * i, side = (i % 2 == 0 ? -1 : 1) * 34, fy = y + side, ta = B(kh);
					// the print: the sole (a short bar along the way) and the heel
					var prt = L19bL(L19bBar(x - dir * 4, fy, x + dir * 44, fy, 58 * TH), L19bDisc(x - dir * 40, fy, 26 * TH));
					atk(kh - fl, () => L19bAdd(t => prt, B(kh - fl), ta, ta + wet * SPB));
					if (i % every == every - 1 || every == 1)
					{
						// the crack: a broken line from the print to the edge (up or down)
						var pts = new List<(double x, double y)> { (x, fy + crackDir * 40) }; double yy = fy + crackDir * 40; int j = 0;
						while (yy > -40 && yy < H + 40) { yy += crackDir * 90; pts.Add((x + ((j++ % 2 == 0) ? 22 : -22), yy)); }
						var cr = new List<L19bP>(); for (int q = 1; q < pts.Count; q++) cr.Add(L19bBar(pts[q - 1].x, pts[q - 1].y, pts[q].x, pts[q].y, cw));
						atk(kh - fl, () => L19bAdd(t => cr, B(kh - fl), ta, ta + 0.55 * SPB));
					}
				}
			}
			walk(51.5, 8, 1200, 80, 200, 1, 2);
			walk(59.5, 7, 80, 1200, 520, -1, 2);
			if (!ez) walk(59.5, 7, 1200, 80, 200, 1, 2);
			fx(51.5, 0.2, 6, 0.6);
		}
		// 0:58 the doors he slams behind him: doors on the top and the bottom walls slam shut one after another along the hall (each shows
		// grey where it swings a beat before), stay shut a beat and open again. From 1:03 two at once (one up, one down)
		{
			double L = ez ? 280 : 300, dw = 20 * TH, swp = 0.22, shut = 1.0, back = 0.5;
			void door(double hx, bool top, double kh)
			{
				double hy = top ? 0 : H, aOpen = top ? 0 : Math.PI, aShut = top ? Math.PI / 2 : -Math.PI / 2, ta = B(kh - swp);
				double ang(double bt) { double u = bt - (kh - swp); if (u < swp) return aOpen + (aShut - aOpen) * L19bE(u / swp); u -= swp; if (u < shut) return aShut; u -= shut; return aShut + (aOpen - aShut) * L19bE(u / back); }
				double mid = (aOpen + aShut) / 2, half = Math.Abs(aShut - aOpen) / 2;
				atk(kh - swp - 1.25 * WN, () => L19bAdd(t => L19bL(L19bRay(hx, hy, ang(BT(t)), 0, L, dw), L19bDisc(hx, hy, 18)), B(kh - swp - 1.25 * WN), ta, B(kh + shut + back - 0.05),
					pre: t => L19bL(L19bCone(hx, hy, mid, half, L))));
			}
			double[] tx = { 160, 480, 800, 1120 }, bx = { 320, 640, 960 };
			// along the hall to the right and back
			var seq = new (double x, bool top)[] { (160, true), (320, false), (480, true), (640, false), (800, true), (960, false), (1120, true), (960, false), (800, true), (640, false), (480, true), (320, false), (160, true), (320, false), (480, true), (640, false) };
			for (int i = 0; i < 8; i++) { if (ez && i % 2 == 1) continue; door(seq[i].x, seq[i].top, 67 + i); }
			for (int i = 8; i < 15; i++)
			{
				double kh = 67 + i; door(seq[i].x, seq[i].top, kh);
				if (!ez) { var o = seq[(i + 4) % 15]; door(o.x, o.top, kh); }
			}
		}
		// 1:08 the train: two tracks, the cars go left on the top one and right on the bottom one, a car on every beat. The steam fills
		// the middle, then the top, then the bottom (it shows first): cross the tracks between the cars
		{
			double y1 = 250, y2 = 470, car = ez ? 200 : 250, gap = ez ? 300 : 230, per = car + gap, v = per / SPB * (ez ? 0.8 : 1), cwid = 84 * TH;
			List<L19bP> cars(double t, double y, int d, double t0)
			{
				var l = new List<L19bP>(); double off = ((v * (t - t0)) % per + per) % per;
				for (double s = -per * 2; s < W + per * 2; s += per)
				{
					double x = d > 0 ? s + off - per : W - (s + off - per);
					double xa = x, xb = x + d * car; if (Math.Max(xa, xb) < -60 || Math.Min(xa, xb) > W + 60) continue;
					l.Add(L19bBar(xa, y, xb, y, cwid));
				}
				return l;
			}
			double t1 = B(83), t2 = B(84.5), te = B(97.6);
			atk(81.5, () => L19bAdd(t => cars(t, y1, -1, t1), B(81.5), t1, te, pre: t => L19bL(L19bBar(-20, y1, W + 20, y1, cwid))));
			atk(83, () => L19bAdd(t => cars(t, y2, 1, t2), B(83), t2, te, pre: t => L19bL(L19bBar(-20, y2, W + 20, y2, cwid))));
			// steam: (beat, y of its middle, height)
			var st = ez ? new[] { (87.0, 360.0, 130.0), (91.0, 95.0, 150.0), (95.0, 625.0, 150.0) }
				: new[] { (86.0, 360.0, 130.0), (88.5, 95.0, 150.0), (91.0, 625.0, 150.0), (93.5, 360.0, 130.0), (95.5, 95.0, 150.0) };
			foreach (var (kh, y, hh) in st)
			{
				double ta = B(kh), te2 = B(kh + (ez ? 1.5 : 1.75));
				atk(kh - 1.75 * WN, () => L19bAdd(t => L19bL(L19bBar(-60, y, W + 60, y, hh)), B(kh - 1.75 * WN), ta, te2));
			}
		}
		// 1:18 the car's lights: from the bottom its lights swing to a new way on every second beat (the next way shows grey). From 1:21 a
		// second car's lights from the top, from 1:25 a third from the left
		{
			double half = (ez ? 9 : 11) * L19bD, len = 1150;
			double sw(double bt, double[] kb, double[] av) { double a = av[0]; for (int i = 1; i < kb.Length; i++) { if (bt < kb[i] - 0.35) break; a = av[i - 1] + (av[i] - av[i - 1]) * L19bE((bt - kb[i] + 0.35) / 0.35); } return a; }
			double nx(double bt, double[] kb, double[] av) { for (int i = 1; i < kb.Length; i++) if (bt < kb[i] - 0.35) return av[i]; return av[^1]; }
			void car(double ox, double oy, double k0, double k1, double[] degs)
			{
				var kb = new double[degs.Length]; for (int i = 0; i < kb.Length; i++) kb[i] = k0 + 2 * i;
				var av = degs.Select(d => d * L19bD).ToArray();
				atk(k0 - 1.5 * WN, () => L19bAdd(t => L19bL(L19bCone(ox, oy, sw(BT(t), kb, av), half, len)), B(k0 - 1.5 * WN), B(k0), B(k1),
					pre: t => L19bL(L19bCone(ox, oy, av[0], half, len)),
					look: t => { double bt = BT(t), a = sw(bt, kb, av), n = nx(bt, kb, av); return Math.Abs(n - a) > 0.01 && bt > k0 - 0.1 ? new List<(L19bP, double)> { (L19bCone(ox, oy, n, half, len), 0.45) } : new List<(L19bP, double)>(); }));
			}
			car(640, H + 40, 99, 118, new double[] { -90, -60, -118, -72, -105, -52, -128, -80, -100, -64 });
			if (!ez) car(640, -40, 105, 118, new double[] { 70, 112, 58, 100, 125, 80, 96 });
			car(-40, 360, 109, 118, new double[] { 20, -15, 30, -25, 10 });
		}
		// 1:30 the road he took: only the road is safe; its far end turns left and right on every second beat (where it goes shows grey),
		// and the lines of the road come at you down its middle
		{
			double k0 = 119, k1 = 134, bw = ez ? 440 : 380, tw = 34;
			double[] vx = { 640, 430, 860, 520, 780, 360, 920, 600, 640 };
			double vpx(double bt) { double x = vx[0]; for (int i = 1; i < vx.Length; i++) { double kb = k0 + 1 + 2 * (i - 1); if (bt < kb - 0.5) break; x = vx[i - 1] + (vx[i] - vx[i - 1]) * L19bE((bt - kb + 0.5) / 0.5); } return x; }
			// o pushes both walls out (they slide in at the start and out at the end)
			List<L19bP> walls(double x, double o = 0) => L19bL(L19bPoly(new[] { -40 - o, x - tw - o, cx - bw - o, -40 - o }, new[] { -40.0, -40, H + 40, H + 40 }), L19bPoly(new[] { x + tw + o, W + 40 + o, W + 40 + o, cx + bw + o }, new[] { -40.0, -40, H + 40, H + 40 }));
			double tin = B(k0), tout = B(k1);
			atk(k0 - 2, () => L19bAdd(t =>
			{
				double bt = BT(t), o = 300 * (1 - L19bE((bt - k0) / 0.6)) + 900 * L19bE((bt - k1 + 0.8) / 0.8);
				return walls(vpx(bt), o);
			}, B(k0 - 2), tin, tout, pre: t => walls(vx[0], 300),
			look: t => { double bt = BT(t); for (int i = 1; i < vx.Length; i++) { double kb = k0 + 1 + 2 * (i - 1); if (bt < kb - 0.5 && kb - 0.5 - bt < 1.2) return walls(vx[i]).Select(q => (q, 0.35)).ToList(); } return new List<(L19bP, double)>(); }));
			// the lines down the middle of the road: one on every beat, they speed up as they come
			for (double kh = k0 + 1; kh < k1 - 1.5; kh += ez ? 2 : 1)
			{
				double ta = B(kh);
				Func<double, (double x, double y, double s)> at2 = t => { double u = Math.Max(0, t - ta), y = 20 + 260 * u + 380 * u * u, x = vpx(BT(t)) + (cx - vpx(BT(t))) * (y / H); return (x, y, 0.4 + 0.6 * y / H); };
				atk(kh - 0.8, () => L19bAdd(t => { var (x, y, s) = at2(t); return y > H + 80 ? new List<L19bP>() : L19bL(L19bBar(x, y - 50 * s, x, y + 50 * s, 30 * s * TH)); }, B(kh - 0.8), ta, ta + 2.2));
			}
		}
		// 1:39 the quiet before: you see him go far away. Rings close in on the middle, one on every beat, each with one gap
		{
			double[] ga = { 0, 150, 300, 80, 230, 20, 190 };
			for (int i = 0; i < 7; i++)
			{
				if (ez && i % 2 == 1) continue;
				double k0 = 134.5 + i, ta = B(k0), te = B(k0 + 2.2), a = ga[i] * L19bD, gh = (ez ? 32 : 24) * L19bD;
				atk(k0 - 1.2 * WN, () => L19bAdd(t => L19bL(L19bGapRing(cx, cy, Math.Max(1, 780 * (1 - (t - ta) / (te - ta))), 22 * TH, a, gh)), B(k0 - 1.2 * WN), ta, te,
					pre: t => L19bL(L19bGapRing(cx, cy, 360, 22 * TH, a, gh))));
			}
		}
		// 1:43 all of it round you: six doors on a ring turn round the middle and slam (out from the ring) in turn; his footsteps walk
		// round inside the ring and crack the floor out to the edge on every second step
		{
			fx(142, 0.3, 10, 1);
			double R = 300, L = 250, dw = 18 * TH, swp = 0.22, shut = 1.0, back = 0.5;
			var rot = new L19Spin(0, 141, 167, new double[] { 141, 154 }, new[] { 9 * L19bD * SP, -9 * L19bD * SP });
			void door(int i, double kh)
			{
				double ta = B(kh - swp);
				double ang(double bt) { double u = bt - (kh - swp); double k = u < swp ? L19bE(u / swp) : u < swp + shut ? 1 : 1 - L19bE((u - swp - shut) / back); return k; }
				Func<double, (double hx, double hy, double a0, double a1)> geo = bt => { double th = rot.At(bt) + i * TAU / 6; return (cx + JS.cos(th) * R, cy + JS.sin(th) * R, th + Math.PI / 2, th); };
				atk(kh - swp - 1.25 * WN, () => L19bAdd(t => { double bt = BT(t); var (hx, hy, a0, a1) = geo(bt); double k = ang(bt); return L19bL(L19bRay(hx, hy, a0 + (a1 - a0) * k, 0, L, dw), L19bDisc(hx, hy, 16)); },
					B(kh - swp - 1.25 * WN), ta, B(kh + shut + back - 0.05),
					pre: t => { var (hx, hy, a0, a1) = geo(BT(t)); return L19bL(L19bCone(hx, hy, (a0 + a1) / 2, Math.PI / 4, L)); }));
			}
			for (int j = 0; j < 20; j++)
			{
				double kh = 143 + j; if (ez && j % 2 == 1) continue;
				door(j % 6, kh); if (!ez && j >= 8) door((j + 3) % 6, kh);
			}
			// the steps go round inside the ring
			double fl = 1.25 * WN, wet = ez ? 1.0 : 1.25;
			for (int i = 0; i < 16; i++)
			{
				if (ez && i % 2 == 1) continue;
				double kh = 148 + i, th = -Math.PI / 2 + i * 26 * L19bD, rr = 150 + (i % 2 == 0 ? -26 : 26), x = cx + JS.cos(th) * rr, y = cy + JS.sin(th) * rr, ta = B(kh);
				double tx = -JS.sin(th), ty = JS.cos(th);
				var prt = L19bL(L19bBar(x - tx * 4, y - ty * 4, x + tx * 44, y + ty * 44, 56 * TH), L19bDisc(x - tx * 40, y - ty * 40, 25 * TH));
				atk(kh - fl, () => L19bAdd(t => prt, B(kh - fl), ta, ta + wet * SPB));
				if (i % 2 == 1)
				{
					var cr = new List<L19bP>(); double px = x, py = y; for (int q = 1; q <= 9; q++) { double d = 30 + q * 90, o = (q % 2 == 0 ? 20 : -20); double nx = cx + JS.cos(th) * (rr + d) - JS.sin(th) * o, ny = cy + JS.sin(th) * (rr + d) + JS.cos(th) * o; cr.Add(L19bBar(px, py, nx, ny, 14 * TH)); px = nx; py = ny; }
					atk(kh - fl, () => L19bAdd(t => cr, B(kh - fl), ta, ta + 0.55 * SPB));
				}
			}
			// they all slam at once
			for (int i = 0; i < 6; i++) { if (ez && i % 2 == 1) continue; door(i, 164); }
			fx(164, 0.3, 12, 1.2);
		}

		// ================= part 3: after him (1:58-2:42) =================
		// 1:58 the rain on his window: the window's cross turns slowly (and back), drops run down the glass in a stair, a beat after their
		// line shows; from 2:04 two at a time
		{
			fx(166, 0.3, 8, 1.4);
			var rot = new L19Spin(0, 166, 187, new double[] { 166, 176 }, new[] { 6 * L19bD * SP, -7 * L19bD * SP });
			double ww = 22 * TH;
			List<L19bP> cross(double a) => L19bL(L19bRay(cx, cy, a, -900, 900, ww), L19bRay(cx, cy, a + Math.PI / 2, -900, 900, ww));
			atk(166, () => L19bAdd(t => cross(rot.At(BT(t))), B(166), B(168), B(186), pre: t => cross(0)));
			double v = 820 * SP;
			int n = 0;
			for (double kh = 168.75; kh < 185.5; kh += 0.5)
			{
				if (ez && n % 2 == 1) { n++; continue; }
				int j = n % 28; int lane = j < 14 ? j : 27 - j; n++;
				void drop(int ln, double k)
				{
					double x = 120 + ln * 80, ta = B(k);
					atk(k - 1.1 * WN, () => L19bAdd(t => { double y = -80 + v * (t - ta); return y > H + 100 ? new List<L19bP>() : L19bL(L19bBar(x, y - 70, x, y, 18 * TH)); }, B(k - 1.1 * WN), ta, ta + 1.25,
						pre: t => L19bL(L19bBar(x, -20, x, H + 20, 22))));
				}
				drop(lane, kh);
				if (!ez && kh >= 176) drop((lane + 7) % 14, kh);
			}
		}
		// 2:10 the gears of the time that goes on: two gears tick (a tooth on every beat, the other way round each other), their middles
		// fill up in turn (shown first), and the walls close in from the top and the bottom
		{
			double k0 = 187, k1 = 206, R = 200, nt = 10, tw = 26 * TH;
			double[] gx = { 400, 880 };
			double stepA(double bt) { double a = 0; for (double k = k0 + 1; k <= bt + 0.2 && k < k1; k += 1) a += L19bE((bt - (k - 0.2)) / 0.2); return a * TAU / nt / 2; }
			List<L19bP> gear(double t, int gi)
			{
				double a = stepA(BT(t)) * (gi == 0 ? 1 : -1) + (gi == 0 ? 0 : TAU / nt / 2); var l = new List<L19bP> { L19bDisc(gx[gi], cy, 38) };
				for (int i = 0; i < nt; i++) l.Add(L19bRay(gx[gi], cy, a + i * TAU / nt, R - 34, R + 36, tw));
				return l;
			}
			for (int gi = 0; gi < 2; gi++) { int q = gi; atk(k0 - 1.5, () => L19bAdd(t => gear(t, q), B(k0 - 1.5), B(k0), B(k1), pre: t => gear(B(k0), q))); }
			// the middles fill (the gear you are in: get out between its teeth)
			double[] fb = ez ? new[] { 192.0, 200 } : new[] { 191.0, 194, 197, 200, 203 };
			for (int i = 0; i < fb.Length; i++)
			{
				double kh = fb[i], x = gx[i % 2], ta = B(kh);
				atk(kh - 1.75 * WN, () => L19bAdd(t => L19bL(L19bDisc(x, cy, R - 44)), B(kh - 1.75 * WN), ta, ta + 1.0 * SPB));
			}
			// the walls
			double wall(double bt) => 60 + 70 * L19bE((bt - 192) / 6);
			atk(189, () => L19bAdd(t => { double y = wall(BT(t)); return L19bL(L19bBar(-60, y - 60, W + 60, y - 60, 120), L19bBar(-60, H - y + 60, W + 60, H - y + 60, 120)); }, B(189), B(191), B(206),
				pre: t => L19bL(L19bBar(-60, 0, W + 60, 0, 120), L19bBar(-60, H, W + 60, H, 120))));
		}
		// 2:22 the last part: the clock comes back. Its hands tick again with the numbers flying (2:23), then the doors with the train
		// (2:28), then the car lights from the bottom and the top with the steps (2:33), and at 2:37 it strikes midnight: twelve rings, two
		// a beat, the gap going round; the two hands close on twelve and it's over
		{
			fx(206, 0.4, 14, 1.6);
			double sw = 0.2, len = 820, wd = 16 * TH;
			var sB = new List<double>(); for (double k = 209; k < 216; k += 1) sB.Add(k);
			double ang(double bt, double a0, List<double> kb, double st) { double a = a0; foreach (var b in kb) { if (bt < b - sw) break; a += st * L19bE((bt - (b - sw)) / sw); } return a; }
			atk(206.5, () => L19bAdd(t => { double bt = BT(t), a = ang(bt, -90 * L19bD, sB, 30 * L19bD); var l = L19bL(L19bRay(cx, cy, a, 0, len, wd), L19bDisc(cx, cy, 30)); return l; }, B(206.5), B(208), B(216.2),
				look: t => { double bt = BT(t), a = ang(bt, -90 * L19bD, sB, 30 * L19bD); double ns = sB.FirstOrDefault(b => b - sw > bt, -1); if (ns < 0 || ns - sw - bt > 0.8 * WN) return new List<(L19bP, double)>(); return new List<(L19bP, double)> { (L19bCone(cx, cy, a + 15 * L19bD, 15 * L19bD, len), 0.5) }; }));
			double v = 1050 * SP, r = 26 * TH;
			for (int i = 0; i < 8; i++)
			{
				if (ez && i % 2 == 1) continue;
				double kh = 208.5 + i; int hr = (i * 5) % 12; double a = -Math.PI / 2 + hr * TAU / 12, ta = B(kh), dx = JS.cos(a), dy = JS.sin(a);
				atk(kh - 1.1 * WN, () => L19bAdd(t => { double d = 320 - v * (t - ta); return L19bL(L19bDisc(cx + dx * d, cy + dy * d, r)); }, B(kh - 1.1 * WN), ta, ta + 1.2,
					pre: t => L19bL(L19bBar(cx + dx * 320, cy + dy * 320, cx - dx * 900, cy - dy * 900, 2 * r))));
			}
			// the doors and the train
			{
				double L = ez ? 270 : 300, dw = 20 * TH, swp = 0.22;
				void door(double hx, bool top, double kh)
				{
					double hy = top ? 0 : H, aOpen = top ? 0 : Math.PI, aShut = top ? Math.PI / 2 : -Math.PI / 2, ta = B(kh - swp);
					double ang2(double bt) { double u = bt - (kh - swp); if (u < swp) return aOpen + (aShut - aOpen) * L19bE(u / swp); u -= swp; if (u < 1) return aShut; u -= 1; return aShut + (aOpen - aShut) * L19bE(u / 0.5); }
					atk(kh - swp - 1.25 * WN, () => L19bAdd(t => L19bL(L19bRay(hx, hy, ang2(BT(t)), 0, L, dw), L19bDisc(hx, hy, 18)), B(kh - swp - 1.25 * WN), ta, B(kh + 1.45),
						pre: t => L19bL(L19bCone(hx, hy, (aOpen + aShut) / 2, Math.PI / 4, L))));
				}
				double[] xs = { 200, 520, 840, 1080 };
				for (int i = 0; i < 8; i++) { if (ez && i % 2 == 1) continue; double kh = 216.5 + i; door(xs[i % 4], true, kh); if (!ez && i % 2 == 1) door(xs[(i + 2) % 4], false, kh); }
				double car = ez ? 200 : 260, gap = ez ? 300 : 240, per = car + gap, vt = per / SPB * (ez ? 0.8 : 1), t0 = B(217);
				atk(215.5, () => L19bAdd(t =>
				{
					var l = new List<L19bP>(); double off = ((vt * (t - t0)) % per + per) % per;
					for (double s = -per * 2; s < W + per * 2; s += per) { double x = s + off - per; if (x + car < -60 || x > W + 60) continue; l.Add(L19bBar(x, cy, x + car, cy, 80 * TH)); }
					return l;
				}, B(215.5), t0, B(224.5), pre: t => L19bL(L19bBar(-20, cy, W + 20, cy, 80 * TH))));
			}
			// the car lights from the bottom and the top, a new way every one and a half beats, and the steps across the middle
			{
				double half = (ez ? 8 : 10) * L19bD, len2 = 1150;
				(double x, double y, double[] a)[] cs = { (640, H + 40, new double[] { -90, -125, -58, -110, -70, -100 }), (640, -40, new double[] { 60, 95, 120, 75, 108, 66 }) };
				for (int c = 0; c < 2; c++)
				{
					if (ez && c == 1) continue;
					var (ox, oy, degs) = cs[c]; var av = degs.Select(d => d * L19bD).ToArray(); double k0 = 225;
					double sw2(double bt) { double a = av[0]; for (int i = 1; i < av.Length; i++) { double kb = k0 + 1.5 * i; if (bt < kb - 0.3) break; a = av[i - 1] + (av[i] - av[i - 1]) * L19bE((bt - kb + 0.3) / 0.3); } return a; }
					double nx(double bt) { for (int i = 1; i < av.Length; i++) if (bt < k0 + 1.5 * i - 0.3) return av[i]; return av[^1]; }
					atk(k0 - 1.5 * WN, () => L19bAdd(t => L19bL(L19bCone(ox, oy, sw2(BT(t)), half, len2)), B(k0 - 1.5 * WN), B(k0), B(232), pre: t => L19bL(L19bCone(ox, oy, av[0], half, len2)),
						look: t => { double bt = BT(t), a = sw2(bt), n = nx(bt); return Math.Abs(n - a) > 0.01 && bt > k0 - 0.1 ? new List<(L19bP, double)> { (L19bCone(ox, oy, n, half, len2), 0.45) } : new List<(L19bP, double)>(); }));
				}
				double fl = 1.25 * WN;
				for (int i = 0; i < 7; i++)
				{
					if (ez && i % 2 == 1) continue;
					double kh = 225.5 + i, x = 1180 - i * 180, fy = cy + (i % 2 == 0 ? -34 : 34), ta = B(kh);
					var prt = L19bL(L19bBar(x + 4, fy, x - 44, fy, 58 * TH), L19bDisc(x + 40, fy, 26 * TH));
					atk(kh - fl, () => L19bAdd(t => prt, B(kh - fl), ta, ta + 1.2 * SPB));
				}
			}
			// midnight: twelve strikes, two a beat (Easy one), each a ring from the middle with its gap one hour on
			{
				double vr = 560 * SP, gh = (ez ? 36 : 30) * L19bD; int cnt = 0;
				for (double kh = 232.5; kh < 238.4; kh += 0.5)
				{
					int i = cnt++; if (ez && i % 2 == 1) continue;
					double ga = -Math.PI / 2 + ((i + 10) % 12) * TAU / 12, ta = B(kh);
					atk(kh - 1.2 * WN, () => L19bAdd(t => L19bL(L19bGapRing(cx, cy, 40 + vr * Math.Max(0, t - ta), 22 * TH, ga, gh)), B(kh - 1.2 * WN), ta, ta + 1.3,
						pre: t => L19bL(L19bGapRing(cx, cy, 280, 22 * TH, ga, gh))));
				}
				// the hands close on twelve like scissors
				double k0 = 236.5, k1 = 239.2;
				atk(k0 - 1.5, () => L19bAdd(t =>
				{
					double k = L19bE((BT(t) - k0) / (k1 - k0)), a = (60 - 58 * k) * L19bD;
					return L19bL(L19bRay(cx, cy, -Math.PI / 2 + a, 0, 900, 20 * TH), L19bRay(cx, cy, -Math.PI / 2 - a, 0, 900, 20 * TH), L19bRay(cx, cy, Math.PI / 2 + a, 0, 900, 20 * TH), L19bRay(cx, cy, Math.PI / 2 - a, 0, 900, 20 * TH));
				}, B(k0 - 1.5), B(k0), B(k1), pre: t => { double a = 60 * L19bD; return L19bL(L19bRay(cx, cy, -Math.PI / 2 + a, 0, 900, 20 * TH), L19bRay(cx, cy, -Math.PI / 2 - a, 0, 900, 20 * TH), L19bRay(cx, cy, Math.PI / 2 + a, 0, 900, 20 * TH), L19bRay(cx, cy, Math.PI / 2 - a, 0, 900, 20 * TH)); }));
			}
			fx(239.2, 0.5, 14, 1.8);
		}
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
