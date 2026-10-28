using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L19 "عافني وراح" (Ahmed Jawad, a sad Iraqi song: "he left me and went"), the second design (10-28): an old black-and-white film of
// the night he left. Part 1 the last night: an old Baghdad street (shanasheel houses, lanterns) in the rain, he walks away, the lamps
// break one by one till it's dark. Part 2 he leaves: the river (Shatt al-Arab) at night, his boat and its wake, the palms in the wind, the doves flying away and circling,
// the big tears, the storm. Part 3 your photo (the first design's photo part, which Mohammed liked: it tears, flips, burns), then your
// heart beats, breaks in two and shatters, and the last photo burns.
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
	// to te, then a grey shadow fades. look(t) adds harmless grey marks while it is out (where a thing goes next), deco(t) draws its harmless
	// look (a man in a boat). The grey parts are their own harmless hazard, so they are drawn under every pink thing.
	static void L19bAdd(Func<double, List<L19bP>> f, double tw, double ta, double te, Func<double, List<L19bP>> pre = null, Func<double, List<(L19bP q, double a)>> look = null, double fade = 0.25, Action<double> deco = null)
	{
		double ct = double.NaN; List<L19bP> cl = null;
		List<L19bP> F(double t) { if (t != ct) { ct = t; cl = f(t); } return cl; }
		var w = new Hz { kind = "L19bW" };
		w.update = (t, dt) => { if (t > te + fade + 0.1) w.dead = true; };
		w.draw = t =>
		{
			if (t < tw) return;
			if (deco != null && t <= te + fade) { g.save(); deco(t); g.restore(); }
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


	// ---------- the looks (harmless) ----------
	static readonly double[] L19bLampX = { 160, 400, 640, 880, 1120 };
	static double[] L19bLampOut = { 24, 28, 32, 36, 48 };   // the beat each lamp breaks (set by the build)
	// a person in black (the old film): standing at (x, foot y), s scale, lean, walk phase (legs apart when walking)
	static void L19bPerson(double x, double y, double s, double lean, double walk, string col)
	{
		g.save(); g.translate(x, y); g.scale(s, s); g.rotate(lean); g.fillStyle = col; g.strokeStyle = col; g.lineCap = "round";
		circle(0, -300, 32);
		g.beginPath(); g.moveTo(-34, -262); g.lineTo(34, -262); g.lineTo(42, -118); g.lineTo(-42, -118); g.closePath(); g.fill();
		double a = 0.32 * walk; g.lineWidth = 26;
		g.beginPath(); g.moveTo(-14, -124); g.lineTo(-14 + JS.sin(a) * 118, -124 + JS.cos(a) * 118); g.stroke();
		g.beginPath(); g.moveTo(14, -124); g.lineTo(14 - JS.sin(a) * 118, -124 + JS.cos(a) * 118); g.stroke();
		g.lineWidth = 18; g.beginPath(); g.moveTo(-30, -250); g.lineTo(-38 - JS.sin(a) * 50, -150); g.stroke(); g.beginPath(); g.moveTo(30, -250); g.lineTo(38 + JS.sin(a) * 50, -150); g.stroke();
		g.restore();
	}
	// a palm (its trunk and its dark crown; the fronds that hurt are their own)
	static void L19bTrunk(double x0, double y0, double x1, double y1, string col)
	{
		g.strokeStyle = col; g.lineWidth = 22; g.lineCap = "round"; g.beginPath(); g.moveTo(x0, y0); g.quadraticCurveTo((x0 + x1) / 2 + 30, (y0 + y1) / 2, x1, y1); g.stroke();
		g.lineWidth = 2; g.strokeStyle = "rgba(0,0,0,0.35)"; for (double u = 0.05; u < 1; u += 0.07) { double x = x0 + (x1 - x0) * u + 30 * 4 * u * (1 - u) * 0.5, y = y0 + (y1 - y0) * u; g.beginPath(); g.moveTo(x - 11, y); g.lineTo(x + 11, y - 4); g.stroke(); }
	}
	static void L19bFilm(double t, double bt)
	{
		// the old film: grain, a few scratches, the light breathing
		int fr = (int)(t * 24);
		uint sd = (uint)(fr * 2654435761u + 12345); double rr() { sd ^= sd << 13; sd ^= sd >> 17; sd ^= sd << 5; return (sd % 10000) / 10000.0; }
		g.fillStyle = "rgba(255,255,255,0.05)"; for (int i = 0; i < 70; i++) g.fillRect(rr() * W, rr() * H, 2, 2);
		g.fillStyle = "rgba(0,0,0,0.10)"; for (int i = 0; i < 70; i++) g.fillRect(rr() * W, rr() * H, 2, 2);
		if (rr() < 0.5) { double x = rr() * W; g.strokeStyle = "rgba(230,226,215,0.10)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(x, 0); g.lineTo(x + rr() * 6 - 3, H); g.stroke(); }
		g.fillStyle = $"rgba(0,0,0,{str(0.05 + 0.05 * rr())})"; g.fillRect(0, 0, W, H);
	}
	// the rain (harmless streaks over the room, under every attack)
	static void L19bRain(double t, double a)
	{
		g.strokeStyle = $"rgba(220,220,230,{str(0.10 * a)})"; g.lineWidth = 1.5; g.beginPath();
		for (int i = 0; i < 70; i++) { double x = (i * 211.7 + t * 120) % (W + 100) - 50, y = (i * 97.3 + t * 900) % (H + 80) - 40; g.moveTo(x, y); g.lineTo(x - 8, y + 30); }
		g.stroke();
	}

	// the old Baghdad street: houses with their wooden bay windows (shanasheel) over the street, lit till the lamp near them breaks
	static readonly (double x0, double x1, double top)[] L19bHouses = { (-20, 190, 250), (190, 370, 300), (370, 540, 228), (540, 720, 286), (720, 900, 240), (900, 1070, 296), (1070, 1300, 254) };
	static void L19bHouse(double x0, double x1, double top, double lit)
	{
		double w = x1 - x0;
		var wg = g.createLinearGradient(0, top, 0, 600); wg.addColorStop(0, "rgba(34,32,36,1)"); wg.addColorStop(1, "rgba(18,17,20,1)"); g.fillStyle = wg; g.fillRect(x0, top, w, 600 - top);
		g.fillStyle = "rgba(11,10,13,1)"; g.fillRect(x0 - 4, top - 10, w + 8, 12);
		for (double x = x0 + 6; x < x1 - 6; x += 18) g.fillRect(x, top - 18, 9, 9);
		// the bricks
		g.strokeStyle = "rgba(0,0,0,0.28)"; g.lineWidth = 1; g.beginPath();
		for (double y = top + 14; y < 600; y += 14) { g.moveTo(x0, y); g.lineTo(x1, y); double o = ((int)((y - top) / 14) % 2) * 15; for (double x = x0 + o; x < x1; x += 30) { g.moveTo(x, y); g.lineTo(x, y + 14); } }
		g.stroke();
		// the shanasheel: a wooden box out over the street on its bracket, a lattice over its window
		double bx = x0 + 24, bw = w - 48, by = top + 34, bh = 108;
		g.fillStyle = "rgba(9,8,10,1)"; g.fillRect(bx - 9, by - 12, bw + 18, bh + 24);
		g.beginPath(); g.moveTo(bx - 9, by + bh + 12); g.lineTo(bx + bw + 9, by + bh + 12); g.lineTo(bx + bw - 22, by + bh + 42); g.lineTo(bx + 22, by + bh + 42); g.closePath(); g.fill();
		if (lit > 0) { var lg = g.createLinearGradient(0, by, 0, by + bh); lg.addColorStop(0, $"rgba(226,206,164,{str(0.62 * lit)})"); lg.addColorStop(1, $"rgba(160,140,108,{str(0.5 * lit)})"); g.fillStyle = lg; }
		else g.fillStyle = "rgba(26,25,29,1)";
		g.fillRect(bx, by, bw, bh);
		g.strokeStyle = "rgba(9,8,10,1)"; g.lineWidth = 2.5; g.beginPath();
		int nc = Math.Max(3, (int)Math.Round(bw / 20)); double cw = bw / nc;
		for (int i = 1; i < nc; i++) { g.moveTo(bx + i * cw, by + 40); g.lineTo(bx + i * cw, by + bh); }
		for (double y = by + 57; y < by + bh; y += 17) { g.moveTo(bx, y); g.lineTo(bx + bw, y); }
		for (int i = 0; i < nc; i++) { double x = bx + i * cw; g.moveTo(x, by + 20); g.lineTo(x + cw / 2, by + 4); g.lineTo(x + cw, by + 20); g.lineTo(x + cw / 2, by + 36); g.closePath(); }
		g.moveTo(bx, by + 40); g.lineTo(bx + bw, by + 40);
		g.stroke();
		// the door: an arch
		double dx = x0 + w / 2;
		g.fillStyle = "rgba(8,8,10,1)"; g.beginPath(); g.moveTo(dx - 24, 600); g.lineTo(dx - 24, 534); g.arc(dx, 534, 24, Math.PI, TAU); g.lineTo(dx + 24, 600); g.closePath(); g.fill();
		g.strokeStyle = "rgba(60,56,58,0.5)"; g.lineWidth = 2; g.beginPath(); g.arc(dx, 534, 30, Math.PI, TAU); g.stroke();
	}
	// a street lamp: an old lantern on its post; lit (0..1), or broken
	static void L19bLantern(double x, double on, bool broken)
	{
		g.fillStyle = "rgba(10,10,12,1)"; g.fillRect(x - 4, 156, 8, 444); g.fillRect(x - 11, 584, 22, 16); g.fillRect(x - 8, 150, 16, 8);
		g.beginPath(); g.moveTo(x - 13, 126); g.lineTo(x + 13, 126); g.lineTo(x + 9, 152); g.lineTo(x - 9, 152); g.closePath();
		g.fillStyle = on > 0 ? $"rgba(246,238,218,{str(0.3 + 0.68 * on)})" : "rgba(42,40,46,1)"; g.fill();
		g.strokeStyle = "rgba(10,10,12,1)"; g.lineWidth = 3; g.stroke(); g.beginPath(); g.moveTo(x, 126); g.lineTo(x, 152); g.stroke();
		if (broken) { g.lineWidth = 1.5; g.strokeStyle = "rgba(120,116,120,0.8)"; g.beginPath(); g.moveTo(x - 9, 130); g.lineTo(x - 2, 140); g.lineTo(x - 7, 148); g.moveTo(x + 9, 132); g.lineTo(x + 3, 139); g.stroke(); }
		g.fillStyle = "rgba(10,10,12,1)"; g.beginPath(); g.moveTo(x - 18, 128); g.lineTo(x, 110); g.lineTo(x + 18, 128); g.closePath(); g.fill(); circle(x, 108, 4);
	}

	// ---------- the room (harmless, under everything): part 1 the street of lamps, 2 the river (Shatt al-Arab) at night, 3 the table ----------
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
				var sky = g.createLinearGradient(0, 0, 0, H); sky.addColorStop(0, "rgba(26,25,30,1)"); sky.addColorStop(1, "rgba(48,46,52,1)"); g.fillStyle = sky; g.fillRect(0, 0, W, H);
				// far roofs and a minaret in the mist, then the street's houses
				g.fillStyle = "rgba(38,36,42,1)"; g.beginPath(); g.moveTo(0, 300);
				double[] fx0 = { 0, 90, 200, 330, 470, 610, 760, 900, 1030, 1160, 1280 }, fh = { 210, 190, 222, 196, 214, 186, 220, 200, 214, 192, 210 };
				for (int i = 0; i < fx0.Length - 1; i++) { g.lineTo(fx0[i], fh[i]); g.lineTo(fx0[i + 1], fh[i]); }
				g.lineTo(W, 300); g.closePath(); g.fill();
				g.fillRect(842, 96, 18, 120); g.beginPath(); g.moveTo(836, 120); g.lineTo(866, 120); g.lineTo(862, 130); g.lineTo(840, 130); g.closePath(); g.fill(); g.beginPath(); g.arc(851, 96, 10, Math.PI, TAU); g.fill(); g.fillRect(850, 76, 2, 12);
				foreach (var (hx0, hx1, top) in L19bHouses)
				{
					double mid = (hx0 + hx1) / 2; int ni = 0; for (int i = 1; i < 5; i++) if (Math.Abs(L19bLampX[i] - mid) < Math.Abs(L19bLampX[ni] - mid)) ni = i;
					L19bHouse(hx0, hx1, top, 1 - L19bE((bt - L19bLampOut[ni] - 1) / 0.6));
				}
				// the wet street: the lamps shine in it, the rain rings the puddles
				var sg = g.createLinearGradient(0, 600, 0, H); sg.addColorStop(0, "rgba(24,23,27,1)"); sg.addColorStop(1, "rgba(10,10,12,1)"); g.fillStyle = sg; g.fillRect(0, 600, W, H - 600);
				g.fillStyle = "rgba(8,8,10,1)"; g.fillRect(0, 598, W, 8);
				for (int i = 0; i < 5; i++)
				{
					double on = 1 - L19bE((bt - L19bLampOut[i]) / 0.4); if (on <= 0) continue; double x = L19bLampX[i];
					g.fillStyle = $"rgba(236,228,206,{str(0.16 * on)})";
					for (int j = 0; j < 6; j++) { double y = 612 + j * 18, ww = (10 + 6 * JS.sin(t * 3 + j * 1.7 + i)) * (1 - j * 0.12); g.fillRect(x - ww, y, ww * 2, 9); }
				}
				g.strokeStyle = "rgba(210,206,198,0.14)"; g.lineWidth = 1.5;
				for (int i = 0; i < 9; i++) { double px = 70 + i * 140 + (i % 3) * 24, py = 640 + (i * 37) % 70, u = ((t * 1.3 + i * 0.37) % 1 + 1) % 1; g.globalAlpha = 1 - u; g.beginPath(); g.ellipse(px, py, 4 + 30 * u, 1.5 + 6 * u, 0, 0, TAU); g.stroke(); }
				g.globalAlpha = 1;
				// the lamps: lit until they break (part 1 ends in the dark)
				for (int i = 0; i < 5; i++)
				{
					double x = L19bLampX[i], on = 1 - L19bE((bt - L19bLampOut[i]) / 0.4);
					if (on > 0) { var lg = g.createRadialGradient(x, 140, 6, x, 300, 380); lg.addColorStop(0, $"rgba(236,230,212,{str(0.22 * on * (0.85 + 0.15 * pulse))})"); lg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = lg; g.fillRect(x - 380, 100, 760, 520); }
					L19bLantern(x, on, bt > L19bLampOut[i]);
				}
				// you, alone under the middle lamp; him, walking away to the right (0:22 to 0:40)
				L19bPerson(560, 600, 0.55, 0, 0, "rgba(8,8,10,1)");
				double wk = clamp((bt - 6) / 30, 0, 1); if (wk < 1) L19bPerson(720 + 700 * wk, 600, 0.52 - 0.12 * wk, 0.04, bt > 6 ? JS.sin(bt * Math.PI) : 0, "rgba(8,8,10,1)");
				L19bRain(t, 0.6 + 0.4 * clamp((bt - 6) / 4, 0, 1));
			}
			else if (part == 2)
			{
				var sky = g.createLinearGradient(0, 0, 0, 300); sky.addColorStop(0, "rgba(22,22,28,1)"); sky.addColorStop(1, "rgba(52,50,58,1)"); g.fillStyle = sky; g.fillRect(0, 0, W, 300);
				// the moon, and the far bank with its palms
				var mg = g.createRadialGradient(1010, 90, 4, 1010, 90, 140); mg.addColorStop(0, "rgba(235,232,222,0.55)"); mg.addColorStop(0.2, "rgba(235,232,222,0.18)"); mg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = mg; g.fillRect(860, 0, 300, 260);
				g.fillStyle = "rgba(230,228,220,0.8)"; circle(1010, 90, 22);
				g.fillStyle = "rgba(14,14,17,1)"; g.fillRect(0, 286, W, 18);
				g.strokeStyle = "rgba(14,14,17,1)"; g.fillStyle = "rgba(14,14,17,1)";
				for (int i = 0; i < 9; i++)
				{
					double x = 60 + i * 150 + (i % 2) * 30, top = 200 + (i % 3) * 18; g.lineWidth = 6; g.beginPath(); g.moveTo(x, 292); g.quadraticCurveTo(x + 8, (292 + top) / 2, x + 4, top); g.stroke();
					g.lineWidth = 4; for (int j = 0; j < 7; j++) { double a = Math.PI + j * Math.PI / 6 + 0.1 * JS.sin(t * 0.8 + i); g.beginPath(); g.moveTo(x + 4, top); g.quadraticCurveTo(x + 4 + JS.cos(a) * 30, top + JS.sin(a) * 30 - 6, x + 4 + JS.cos(a) * 46, top + JS.sin(a) * 30 + 14); g.stroke(); }
				}
				// the water: dark, with the moon's road and slow lines
				var wg = g.createLinearGradient(0, 300, 0, H); wg.addColorStop(0, "rgba(40,40,46,1)"); wg.addColorStop(1, "rgba(18,18,22,1)"); g.fillStyle = wg; g.fillRect(0, 300, W, H - 300);
				g.strokeStyle = "rgba(225,222,212,0.12)"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 26; i++) { double y = 310 + i * 16, x = 1010 + JS.sin(t * 1.3 + i) * 20, wd = 20 + i * 3; g.moveTo(x - wd, y); g.lineTo(x + wd, y); }
				g.stroke();
				g.strokeStyle = "rgba(200,200,210,0.05)"; g.beginPath(); for (int i = 0; i < 14; i++) { double y = 320 + i * 30, x = (i * 173.3 + t * 30) % (W + 200) - 100; g.moveTo(x, y); g.lineTo(x + 140, y); } g.stroke();
				if (bt >= 142) L19bRain(t, clamp((bt - 142) / 2, 0, 1) * (1 - clamp((bt - 164) / 2, 0, 1)));
			}
			else
			{
				// the table the photo lies on
				g.fillStyle = "rgba(22,20,20,1)"; g.fillRect(0, 0, W, H);
				var lt = g.createRadialGradient(W / 2, H / 2 - 60, 40, W / 2, H / 2, 760); lt.addColorStop(0, "rgba(110,104,96,0.28)"); lt.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = lt; g.fillRect(0, 0, W, H);
				g.strokeStyle = "rgba(0,0,0,0.35)"; g.lineWidth = 2; g.beginPath(); for (double x = 0; x < W; x += 160) { g.moveTo(x, 0); g.lineTo(x + 20, H); } g.stroke();
			}
			L19bFilm(t, bt);
			if (fade > 0) { g.fillStyle = $"rgba(0,0,0,{str(fade * 0.85)})"; g.fillRect(0, 0, W, H); }
			var vg = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
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

	// ---------- the shapes ----------
	// a dove at (x, y) flying the way d (+1 right, -1 left), its wings at flap (0 down .. 1 up)
	// a dove (seen from the side, flying the way d): its body, head and beak, its fanned tail, and the two wings (the far one shorter)
	static readonly double[][] L19bDoveP =
	{
		new double[] { 22, -4, 14, -9, 0, -9, -18, -5, -24, 0, -16, 5, 0, 7, 16, 4 },
		new double[] { 31, -14, 39, -10, 31, -7 },
		new double[] { -18, -3, -40, -10, -46, -2, -44, 5, -38, 9, -18, 3 },
	};
	static void L19bDove(List<L19bP> l, double x, double y, double d, double flap, double sc = 1)
	{
		L19bP P(double[] q, double ox, double fy)
		{
			var xs = new double[q.Length / 2]; var ys = new double[q.Length / 2];
			for (int i = 0; i < xs.Length; i++) { xs[i] = x + d * (q[2 * i] + ox) * sc; ys[i] = y + q[2 * i + 1] * sc; }
			return L19bPoly(xs, ys);
		}
		foreach (var q in L19bDoveP) l.Add(P(q, 0, 1));
		l.Add(L19bDisc(x + d * 24 * sc, y - 11 * sc, 7.5 * sc));
		double f = JS.sin((-40 + 115 * flap) * L19bD);
		foreach (var (k, ox) in new[] { (0.8, 7.0), (1.0, 0.0) })
		{
			double v = f * k;
			l.Add(P(new double[] { 10, -6, 2, -6 - 30 * v, -16, -6 - 52 * v, -23, -6 - 42 * v, -16, -6 - 35 * v, -23, -6 - 27 * v, -15, -6 - 20 * v, -19, -6 - 11 * v, -10, -5 }, ox, 1));
		}
	}
	// the wings beat on every beat (up on the beat, down on the half)
	static double L19bFlap(double bt) { double u = ((bt % 1) + 1) % 1; return u < 0.5 ? 1 - L19bE(u / 0.5) : L19bE((u - 0.5) / 0.5); }
	// the heart (k = size): its outline points, from the cleft at the top round the right side, the tip, and the left side
	static (double x, double y) L19bHeartPt(double u, double k) { double s = JS.sin(u); return (16 * s * s * s * k, -(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)) * k); }
	static readonly double[] L19bZig = { 0, 6, -6, 7, -5, 6, -7, 5, 0 };   // the crack down the middle (x offsets, in heart units)
	// half the heart (side +1 right, -1 left), its crack edge zigzag, moved by (dx, dy) and turned by a round its middle
	static L19bP L19bHalf(double cx, double cy, double k, int side, double dx, double dy, double a)
	{
		var xs = new List<double>(); var ys = new List<double>();
		for (int i = 0; i <= 24; i++) { var (x, y) = L19bHeartPt(i * Math.PI / 24, k); xs.Add(side * x); ys.Add(y); }
		double yTop = ys[0], yBot = ys[^1];
		for (int i = L19bZig.Length - 2; i >= 1; i--) { double u = i / (double)(L19bZig.Length - 1); xs.Add(L19bZig[i] * k * 0.6); ys.Add(yTop + (yBot - yTop) * u); }
		double ca = JS.cos(a), sa = JS.sin(a), mx = side * 7 * k;
		var X = new double[xs.Count]; var Y = new double[xs.Count];
		for (int i = 0; i < xs.Count; i++) { double px = xs[i] - mx, py = ys[i] - 3 * k; X[i] = cx + dx + mx + px * ca - py * sa; Y[i] = cy + dy + 3 * k + px * sa + py * ca; }
		return L19bPoly(X, Y);
	}
	static L19bP L19bHeart(double cx, double cy, double k)
	{
		var xs = new double[48]; var ys = new double[48];
		for (int i = 0; i < 48; i++) { var (x, y) = L19bHeartPt(i * TAU / 48, k); xs[i] = cx + x; ys[i] = cy + y; }
		return L19bPoly(xs, ys);
	}
	// a boat (the long Iraqi boat, its ends turned up) at (x, y) going the way d
	static readonly double[] L19bHullX = { -118, -94, 92, 122, 104, 66, -74, -104 }, L19bHullY = { -34, -10, -10, -38, -4, 15, 15, -4 };
	static L19bP L19bHull(double x, double y, double d, double sc = 1) => L19bPoly(L19bHullX.Select(p => x + d * p * sc).ToArray(), L19bHullY.Select(p => y + p * sc).ToArray());
	// a tear: round at the bottom (x, y), r wide, pointed at the top, len long
	static L19bP L19bTear(double x, double y, double r, double len)
	{
		var xs = new double[12]; var ys = new double[12];
		for (int i = 0; i < 11; i++) { double a = (-28 + 236.0 * i / 10) * L19bD; xs[i] = x + JS.cos(a) * r; ys[i] = y - r + JS.sin(a) * r; }
		xs[11] = x; ys[11] = y - len; return L19bPoly(xs, ys);
	}
	// a piece of the lamp's glass (a sharp triangle) at (x, y) turned a
	static L19bP L19bShard(double x, double y, double a, double s = 1) => L19bPoly(new[] { x + JS.cos(a) * 20 * s, x + JS.cos(a + 2.5) * 11 * s, x + JS.cos(a + 3.6) * 14 * s }, new[] { y + JS.sin(a) * 20 * s, y + JS.sin(a + 2.5) * 11 * s, y + JS.sin(a + 3.6) * 14 * s });
	// a palm frond: from (x, y) out the way a for l1, then bending the way a2 for l2; its leaflets make the edge saw-toothed
	static L19bP L19bFrond(double x, double y, double a, double a2, double l1, double l2, double w)
	{
		double cx1 = x + JS.cos(a) * l1, cy1 = y + JS.sin(a) * l1, ex = cx1 + JS.cos(a2) * l2, ey = cy1 + JS.sin(a2) * l2;
		const int n = 16; var lx = new double[n + 1]; var ly = new double[n + 1]; var rx = new double[n]; var ry = new double[n];
		for (int i = 0; i <= n; i++)
		{
			double u = i / (double)n, px = (1 - u) * (1 - u) * x + 2 * u * (1 - u) * cx1 + u * u * ex, py = (1 - u) * (1 - u) * y + 2 * u * (1 - u) * cy1 + u * u * ey;
			double tx = 2 * (1 - u) * (cx1 - x) + 2 * u * (ex - cx1), ty = 2 * (1 - u) * (cy1 - y) + 2 * u * (ey - cy1), tl = Math.Max(1e-6, hypot(tx, ty)), nx = -ty / tl, ny = tx / tl;
			double ww = i == n ? 0 : w * Math.Pow(JS.sin(Math.PI * Math.Min(1, 0.15 + u)), 0.6) * (i % 2 == 0 ? 1 : 0.45);
			lx[i] = px + nx * ww; ly[i] = py + ny * ww; if (i < n) { rx[i] = px - nx * ww; ry[i] = py - ny * ww; }
		}
		return L19bPoly(lx.Concat(rx.Reverse()).ToArray(), ly.Concat(ry.Reverse()).ToArray());
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
		double WN = ez ? 1.25 : 1;           // warnings are longer on Easy
		double SP = ez ? 0.85 : 1;           // and things are slower
		double TH = ez ? 0.9 : 1;            // and thinner
		double cx = W / 2, cy = H / 2;
		const string INK = "rgba(8,8,10,1)";
		at(0, () => hazards.Add(L19bStage(new double[] { 0, 50, 166 }, 239.25)), true);

		// ---- the tears' rain: a row of drops on every lane but a few (the gap), the gap moves one lane a row like a stair ----
		int rowN = 0;
		void rain(double k0, double k1, double step, int gapW, int g0)
		{
			int span = 17 - gapW, gi = g0, dir = 1;
			for (double kh = k0; kh < k1 - 0.01; kh += step)
			{
				int g0r = gi; gi += dir; if (gi > span - 1) { gi = span - 2; dir = -1; } else if (gi < 0) { gi = 1; dir = 1; }
				double ta = B(kh), v = 950 * SP; int n = rowN++;
				List<L19bP> row(double y) { var l = new List<L19bP>(); for (int j = 0; j < 17; j++) if (j < g0r || j >= g0r + gapW) l.Add(L19bTear(j * 80, y, 9 * TH, 64)); return l; }
				atk(kh - 0.8 * WN, () => L19bAdd(t => row(-50 + v * (t - ta)), B(kh - 0.8 * WN), ta, ta + 0.9, pre: t => row(36)));
			}
		}

		// ================= part 1: the last night (0:18-0:48): the street of lamps in the rain, he walks away =================
		// 0:19 the lamp over you swings on the band's hits: its light (pink) flashes one way, then the other
		{
			double[] kb = { 2, 3, 4, 5, 6 }, ang = { 55, 125, 38, 142, 90 };
			for (int i = 0; i < 5; i++)
			{
				if (ez && i == 3) continue;
				double kh = kb[i], a = ang[i] * L19bD, half = (i == 4 ? (ez ? 20 : 24) : (ez ? 13 : 16)) * L19bD;
				atk(kh - 1.5 * WN, () => L19bAdd(t => L19bL(L19bCone(640, 140, a, half, 660)), B(kh - 1.5 * WN), B(kh), B(kh + 0.4)));
			}
			fx(2, 0.2, 6, 0.8);
		}
		// 0:22 the tears start to fall while he walks away: a row on every half beat, the gap moves like a stair
		rain(7, 22, 0.5, ez ? 4 : 3, 1);
		// 0:31 the lamps break one after another as he passes (their light flashes, then the glass falls in a fan); the rain goes on
		{
			void lamp(int i, double kL, int nShards, double wide)
			{
				double x = L19bLampX[i], half = wide * L19bD;
				atk(kL - 1.5 * WN, () => L19bAdd(t => L19bL(L19bCone(x, 140, Math.PI / 2, half, 560)), B(kL - 1.5 * WN), B(kL), B(kL + 0.5)));
				double v0 = 560 * SP, gr = 1400 * SP * SP, t0 = B(kL + 0.5);
				var angs = Enumerable.Range(0, nShards).Select(j => (-155 + 130.0 * j / (nShards - 1)) * L19bD).ToArray();
				(double x, double y, double a) sp(double a, double u) => (x + JS.cos(a) * v0 * u, 140 + JS.sin(a) * v0 * u + 0.5 * gr * u * u, a + u * 9);
				atk(kL - 0.6, () => L19bAdd(t =>
				{
					double u = Math.Max(0, t - t0); var l = new List<L19bP>();
					foreach (var a in angs) { var (px, py, r) = sp(a, u); if (py < H + 40) l.Add(L19bShard(px, py, r, 1.3 * TH)); }
					return l;
				}, B(kL - 0.6), t0, t0 + 1.7, pre: t => { var l = new List<L19bP>(); foreach (var a in angs) for (double u = 0; u < 1.3; u += 0.12) { var (p0x, p0y, _) = sp(a, u); var (p1x, p1y, _) = sp(a, u + 0.12); l.Add(L19bBar(p0x, p0y, p1x, p1y, 3)); } return l; }));
			}
			for (int i = 0; i < 5; i++) L19bLampOut[i] = new[] { 25.0, 29, 33, 37, 48 }[i];
			lamp(0, 25, ez ? 7 : 8, 22); lamp(1, 29, ez ? 7 : 8, 22); lamp(3, 33, ez ? 7 : 9, 22); lamp(4, 37, ez ? 7 : 9, 22);
			rain(23, 40, 1, ez ? 3 : 2, 6);
			// 0:42 the last lamp (over you) swings left and right, then breaks and it's dark
			var swing = new Func<double, double>(bt => Math.PI / 2 + 52 * L19bD * JS.sin(Math.PI * (bt - 40.5) / 2));
			double sh = (ez ? 12 : 15) * L19bD;
			atk(39, () => L19bAdd(t => L19bL(L19bCone(640, 140, swing(BT(t)), sh, 640)), B(39), B(40.5), B(47.6), pre: t => L19bL(L19bCone(640, 140, swing(40.5), sh, 640))));
			rain(41, 47, 1, ez ? 4 : 3, 9);
			lamp(2, 48, ez ? 8 : 10, 28);
			fx(48.4, 0.35, 12, 1.4);
		}

		// ================= part 2: he leaves (0:48-1:58): the river at night, the boat, the palms, the doves =================
		// a boat from one side to the other (its wake opens behind it: two lines with little crests; its oars send ripples to both sides on every beat (Easy every second))
		void boat(double k0, double k1, double y, int d, bool ripples = true)
		{
			double t0 = B(k0), v = (W + 440) / ((k1 - k0) * SPB), xs = d > 0 ? -220 : W + 220, Tw = (ez ? 1.2 : 2.0), tn = Math.Tan(19.5 * L19bD);
			double X(double t) => xs + d * v * (t - t0);
			atk(k0 - 1.5 * WN, () => L19bAdd(t =>
			{
				double x = X(t), tau = Math.Min(Tw, t - t0), xt = X(t - tau) - d * 90, sx = x - d * 90;
				var l = L19bL(L19bHull(x, y, d, 1.2));
				if (tau > 0.02) { l.Add(L19bBar(sx, y, xt, y - tn * v * tau, 14 * TH)); l.Add(L19bBar(sx, y + 8, xt, y + 8 + tn * v * tau, 14 * TH));
					// the wake's little crests along both arms, turned out from them
					double len = hypot(xt - sx, tn * v * tau), c1 = JS.cos(19.5 * L19bD), s1 = JS.sin(19.5 * L19bD), c2 = JS.cos(62 * L19bD), s2 = JS.sin(62 * L19bD);
					for (double q = 70; q < len - 10; q += 78) { double ax = sx - d * c1 * q; l.Add(L19bBar(ax, y - s1 * q, ax - d * c2 * 34, y - s1 * q - s2 * 34, 9 * TH)); l.Add(L19bBar(ax, y + 8 + s1 * q, ax - d * c2 * 34, y + 8 + s1 * q + s2 * 34, 9 * TH)); } }
				return l;
			}, B(k0 - 1.5 * WN), t0, B(k1) + Tw, pre: t => L19bL(L19bBar(-40, y, W + 40, y, 40)),
			deco: t => { if (t < t0) return; double x = X(t); L19bPerson(x - d * 10, y - 12, 0.3, -d * 0.05, 0, INK); g.strokeStyle = INK; g.lineWidth = 5; g.beginPath(); g.moveTo(x - d * 30, y - 90); g.lineTo(x + d * 60, y + 30); g.stroke(); }));
			if (!ripples) return;
			for (double kr = Math.Ceiling(k0 + 1); kr < k1 - 0.5; kr += 1)
			{
				if (ez && ((int)kr) % 2 != 0) continue;
				double ta = B(kr), bx = X(ta); if (bx < -100 || bx > W + 100) continue;
				List<L19bP> rp(double r) => L19bL(L19bArc(bx, y, r, 10 * TH, -140 * L19bD, -40 * L19bD), L19bArc(bx, y, r, 10 * TH, 40 * L19bD, 140 * L19bD));
				atk(kr - 1 * WN, () => L19bAdd(t => rp(40 + 260 * SP * (t - ta) / SPB), B(kr - 1 * WN), ta, ta + SPB, pre: t => rp(160)));
			}
		}
		// 0:49 his boat goes (right to left), then another the other way lower down
		fx(50.1, 0.3, 10, 1);
		boat(51, 59, 330, -1); boat(58, 66, 520, 1);
		// 0:58 the palms in the wind: three crowns whip one way and the other on every beat (the middle one the other way), dates fall
		void palms(double k0, double k1, int[] which, double amp)
		{
			(double x, double y)[] cr = { (220, 240), (640, 200), (1060, 240) };
			double[] fa = { 160, 198, 234, 270, 306, 342, 20 };
			double sway(double bt, int c) { double s = 0; double ph = c == 1 ? -1 : 1; for (double k = k0 + 1; k <= k1; k += 1) { if (bt < k - 0.3) break; double tgt = ((int)k % 2 == 0 ? 1 : -1) * ph; s = s + (tgt - s) * L19bE((bt - k + 0.3) / 0.3); } return s * amp * L19bD; }
			List<L19bP> fronds(double bt, int c)
			{
				var l = new List<L19bP>(); var (x, y) = cr[c]; double sw = sway(bt, c);
				foreach (var a0 in fa)
				{
					double a = a0 * L19bD + sw, dn = Math.PI / 2 - a; dn = Math.Atan2(JS.sin(dn), JS.cos(dn)); double a2 = a + Math.Sign(dn) * 35 * L19bD;
					double x1 = x + JS.cos(a) * 130, y1 = y + JS.sin(a) * 130, x2 = x1 + JS.cos(a2) * 115, y2 = y1 + JS.sin(a2) * 115;
					l.Add(L19bFrond(x, y, a, a2, 130, 115, 13 * TH));
				}
				return l;
			}
			foreach (var c in which)
			{
				int q = c; var (x, y) = cr[c];
				atk(k0 - 1.5 * WN, () => L19bAdd(t => fronds(BT(t), q), B(k0 - 1.5 * WN), B(k0), B(k1), deco: t => { L19bTrunk(x - 20, H + 20, x, y + 10, "rgba(12,12,14,1)"); g.fillStyle = "rgba(12,12,14,1)"; circle(x, y + 4, 18); for (int j = 0; j < 5; j++) circle(x - 24 + j * 12, y + 26 + (j % 2) * 6, 7); }));
			}
			// the dates: on every beat three fall from a crown (Normal: and two from the next one), their way shows a beat before
			int n = 0; double gr = 1500 * SP * SP;
			for (double kh = k0 + 1; kh < k1 - 0.5; kh += 1)
			{
				int c = which[n % which.Length], c2 = which[(n + 1) % which.Length]; n++; var (x, y) = cr[c]; double ta = B(kh);
				var xs3 = !ez && c2 != c ? new[] { x - 60, x, x + 60, cr[c2].x - 60, cr[c2].x + 60 } : new[] { x - 60, x, x + 60 };
				atk(kh - 1 * WN, () => L19bAdd(t => { double u = t - ta, yy = y + 40 + 0.5 * gr * u * u; return yy > H + 30 ? new List<L19bP>() : xs3.Select(xx => L19bDisc(xx, yy, 18 * TH)).ToList(); }, B(kh - 1 * WN), ta, ta + 0.9,
					pre: t => xs3.Select(xx => L19bBar(xx, y + 40, xx, H + 20, 30)).ToList()));
			}
		}
		palms(66, 82, new[] { 0, 1, 2 }, ez ? 18 : 32);
		// 1:08 the doves fly away: V flocks across the river, a row of doves at fixed heights (their heights show first); go between the rows
		void flock(double kh, double y0, int d)
		{
			double ta = B(kh), v = (ez ? 520 : 610) * SP, xs = d > 0 ? -60 : W + 60, dy = ez ? 88 : 82;
			var rows = new List<(double dx, double dy)> { (0, 0) }; for (int j = 1; j <= 3; j++) { rows.Add((-j * 74, -j * dy)); rows.Add((-j * 74, j * dy)); }
			double te = ta + (W + 400) / v;
			atk(kh - 1.5 * WN, () => L19bAdd(t =>
			{
				double x = xs + d * v * (t - ta), fl = L19bFlap(BT(t)); var l = new List<L19bP>();
				foreach (var (ox, oy) in rows) { double bx = x + d * ox; if (bx < -60 || bx > W + 60) continue; L19bDove(l, bx, y0 + oy, d, fl, 0.85); }
				return l;
			}, B(kh - 1.5 * WN), ta, te, pre: t => rows.Select(r => L19bBar(d > 0 ? 0 : W - 260, y0 + r.dy - 10, d > 0 ? 260 : W, y0 + r.dy - 10, 40)).ToList()));
		}
		flock(83, 360, -1); flock(85, 250, 1); flock(87, 470, -1); flock(89, 360, 1);
		if (ez) { flock(92, 300, -1); flock(95, 420, 1); }
		else { flock(91, 220, -1); flock(91, 520, 1); flock(93, 360, -1); flock(95, 260, 1); flock(95, 470, -1); }
		// 1:18 more boats leave, crossing each other (their wakes cross), and the doves again
		boat(99, 107, 300, 1); boat(100, 108, 560, -1); boat(108, 116, 430, 1);
		flock(104, 170, -1); if (!ez) { flock(106, 590, 1); flock(112, 590, 1); flock(114, 170, -1); }
		// 1:30 the doves circle: two rings, each with a gap, turning the other way round; the inner one breathes out for two beats, then (Normal)
		// the outer one breathes in for two
		{
			double k0 = 119, k1 = 134;
			List<L19bP> ring(double bt, int n, int miss, double R0, double amp, double w, double ph = 0)
			{
				var l = new List<L19bP>(); double R = R0 + amp * Math.Max(0, JS.sin(Math.PI * (bt - k0) / 2 + ph)), a0 = w * (bt - k0) * L19bD, fl = L19bFlap(bt);
				for (int i = miss; i < n; i++) { double a = a0 + i * TAU / n; L19bDove(l, cx + JS.cos(a) * R, cy + JS.sin(a) * R * 0.92, w > 0 ? (JS.sin(a) > 0 ? -1 : 1) : (JS.sin(a) > 0 ? 1 : -1), fl, 0.95); }
				return l;
			}
			atk(k0 - 1.5 * WN, () => L19bAdd(t => ring(BT(t), 12, ez ? 3 : 2, 210, ez ? 100 : 115, ez ? 36 : 42), B(k0 - 1.5 * WN), B(k0), B(k1), pre: t => ring(k0, 12, ez ? 3 : 2, 210, ez ? 100 : 115, ez ? 36 : 42)));
			atk(k0 + 1.5 - 1.5 * WN, () => L19bAdd(t => ring(BT(t), ez ? 14 : 18, ez ? 4 : 3, 420, ez ? 0 : -90, ez ? -20 : -28, Math.PI), B(k0 + 1.5 - 1.5 * WN), B(k0 + 1.5), B(k1), pre: t => ring(k0 + 1.5, ez ? 14 : 18, ez ? 4 : 3, 420, ez ? 0 : -90, ez ? -20 : -28, Math.PI)));
		}
		// 1:39 the quiet: big tears fall one after another and ripple out on the water (each ring has a gap towards the middle)
		{
			double[] xs = { 320, 960, 640, 200, 1080, 480, 800 }; double ly = 560;
			for (int i = 0; i < 7; i++)
			{
				if (ez && i % 2 == 1) continue;
				double kl = 135 + i, x = xs[i], tl = B(kl), tf = tl - 1.4, v = (ly + 40) / 1.4, ga = Math.Atan2(cy - ly, cx - x);
				atk(kl - 2.3 - 1.0, () => L19bAdd(t => { double y = -40 + v * (t - tf); return L19bL(L19bTear(x, y + 28, 28 * TH, 96)); }, tf - 1.0 * SPB, tf, tl, pre: t => L19bL(L19bBar(x, -20, x, ly, 44))));
				atk(kl - 1.2 * WN, () => L19bAdd(t => L19bL(L19bGapRing(x, ly, 20 + 330 * (t - tl) / (1.3 * SPB), 10 * TH, ga, (ez ? 34 : 26) * L19bD)), B(kl - 1.2 * WN), tl, tl + 1.3 * SPB, pre: t => L19bL(L19bGapRing(x, ly, 120, 10 * TH, ga, (ez ? 34 : 26) * L19bD))));
			}
		}
		// 1:43 the storm on the river: the rain, the middle palm whipping hard, a boat in it
		fx(142, 0.3, 10, 1);
		rain(143, 163, ez ? 1 : 0.5, ez ? 4 : 2, 7);
		palms(144, 163, new[] { 1 }, ez ? 22 : 32);
		boat(150, 158, 470, -1, false); boat(154, 162, 240, 1, false);
		fx(164, 0.6, 8, 1.2);

		// ================= part 3: your photo (1:58-2:42) =================
		// 1:58 the photo of you two (Mohammed liked it, 10-26): it tears, its pieces drift apart, flip on the beat (the back is pink), come
		// together and part again, then burn (the fire's front is pink, the ash behind it is safe); the tears fall on the ash. These are
		// the first design's events (data/dodge_L19_photo.json, kinds in L19.cs)
		fx(166.15, 0.3, 8, 1.4);
		{
			using var f = Godot.FileAccess.Open("res://data/dodge_L19_photo.json", Godot.FileAccess.ModeFlags.Read);
			using var doc = System.Text.Json.JsonDocument.Parse(f.GetAsText());
			foreach (var el in doc.RootElement.GetProperty("events").EnumerateArray())
			{
				var de = new DodgeEv(el.Clone());
				if (DK.TryGetValue(de.k, out var mk)) E.Add(new Ev { b = de.b, f = mk(de) });
			}
		}
		// 2:22 your heart: it beats, and on every second beat (Normal: every beat from 2:25) a wave in its shape goes out, with a gap where it
		// cracks; at 2:30 it breaks in two: the halves fly apart, slam back together twice, then it shatters
		{
			fx(206, 0.4, 14, 1.6);
			double hk = 8, hy = cy + 20;
			double beat(double bt) { double u = ((bt % 1) + 1) % 1; return JS.exp(-u * 6); }
			atk(205.5, () => L19bAdd(t => L19bL(L19bHeart(cx, hy, hk * (1 + 0.1 * beat(BT(t))))), B(205.5), B(207), B(214.5), pre: t => L19bL(L19bHeart(cx, hy, hk))));
			int wn2 = 0;
			for (double kw = 208; kw < 214.5; kw += 1)
			{
				if (((int)kw % 2 == 1) && (ez || kw < 210)) continue;
				int gi = (wn2++ * 13) % 40; double ta = B(kw);
				List<L19bP> wave(double k)
				{
					var l = new List<L19bP>(); var pts = Enumerable.Range(0, 41).Select(i => L19bHeartPt(i * TAU / 40, k)).ToArray();
					for (int i = 0; i < 40; i++) { int dgi = Math.Abs(i - gi); dgi = Math.Min(dgi, 40 - dgi); if (dgi <= (ez ? 4 : 3)) continue; l.Add(L19bBar(cx + pts[i].x, hy + pts[i].y, cx + pts[i + 1].x, hy + pts[i + 1].y, 12 * TH)); }
					return l;
				}
				atk(kw - 1.2 * WN, () => L19bAdd(t => wave(hk * (1.3 + 5.5 * SP * (t - ta) / (1.6 * SPB))), B(kw - 1.2 * WN), ta, ta + 1.6 * SPB, pre: t => wave(hk * 2.4)));
			}
			// the halves: apart (dx) and turned (a) by beat
			double[] kb = { 214.5, 215.2, 217.7, 218.0, 219.0, 219.7, 221.7, 222.0, 223.0 };
			double[] dv = { 0, 360, 360, 0, 0, 380, 380, 0, 0 };
			double DX(double bt) { if (bt <= kb[0]) return 0; for (int i = 1; i < kb.Length; i++) if (bt < kb[i]) return dv[i - 1] + (dv[i] - dv[i - 1]) * L19bE((bt - kb[i - 1]) / (kb[i] - kb[i - 1])); return dv[^1]; }
			List<L19bP> halves(double bt) { double dx = DX(bt), a = dx / 360 * 0.35; return L19bL(L19bHalf(cx, hy, hk, -1, -dx, 0, -a), L19bHalf(cx, hy, hk, 1, dx, 0, a)); }
			atk(214.4, () => L19bAdd(t => halves(BT(t)), B(214.4), B(214.5), B(223),
				look: t => { double bt = BT(t); return (bt > 216.7 && bt < 217.9) || (bt > 220.7 && bt < 221.9) ? halves(218).Select(q => (q, 0.55)).ToList() : new List<(L19bP, double)>(); }));
			fx(218, 0.25, 12, 0.6); fx(222, 0.25, 12, 0.6);
			// it shatters: eight pieces fly out turning
			double t3 = B(223), vs = 520 * SP;
			atk(222, () => L19bAdd(t =>
			{
				double u = t - t3; var l = new List<L19bP>();
				for (int i = 0; i < 8; i++) { double a = (i + 0.5) * TAU / 8, r = 40 + vs * u, x = cx + JS.cos(a) * r, y = hy + JS.sin(a) * r, s = a + u * 6; if (x < -80 || x > W + 80 || y < -80 || y > H + 80) continue; l.Add(L19bBar(x - JS.cos(s) * 30, y - JS.sin(s) * 30, x + JS.cos(s) * 30, y + JS.sin(s) * 30, 26 * TH)); }
				return l;
			}, B(222), t3, Math.Min(t3 + 1.9, B(227.5)), pre: t => Enumerable.Range(0, 8).Select(i => { double a = (i + 0.5) * TAU / 8; return L19bBar(cx + JS.cos(a) * 60, hy + JS.sin(a) * 60, cx + JS.cos(a) * 900, hy + JS.sin(a) * 900, 30); }).ToList()));
			fx(223, 0.35, 12, 1);
			// then the tears again till the last photo comes (they end before it shows)
			rain(224.5, 226.1, 0.5, ez ? 4 : 3, 5);
			fx(239.2, 0.5, 14, 1.8);
		}
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
