using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L16 "عين بعين" (Hussam Al-Rassam), the third design (v52, his words 10-28: build it again; the part 3 is a fight against the girl).
// The story of the song: she left him, married another and now tells him "forget me, I can replace you".
// Part 1 "جنتي تموت" the garden: a rose drawn by a pen that you live inside, its petals falling one by one, three roses that change
// places ("ابدلك"), thorn vines growing in lanes, two roses face to face ("عين بعين"), and the last rose opening wide.
// Part 2 the mirror they looked in: it cracks where it is hit (the cracks run to the edges), pieces of it fall on you (their shadow shows
// first) and break into splinters, cuts open across the whole mirror, and at the end it breaks.
// Part 3 the bride: she stands in her wedding dress and fights with what a wedding has: the coins thrown over a bride (نثر), the two
// wedding rings spinning like coins, her veil's lace sweeping the room (pass through its holes), her bouquet thrown at you that opens
// into a rose, her dance (the rose of her skirt), and the rings that go and come back ("تالي تجيني").
// Every hazard is a pure function of time once it exists (aimed ones also of where the square was when they locked), so hit(p, t) is right
// for any later t. The data is data/dodge_L16.json (made by tools/level16/gen.py). The second design (v49-v51) is L16.cs + data/dodge_L16_v51.json.
public static partial class G
{
	static double L16bK(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static double L16bS(double k) => k * k * (3 - 2 * k);
	static string L16bG(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string L16bP(double a) => "rgba(255,42,109," + str(clamp(a, 0, 1)) + ")";
	static string L16bAu(double a) => "rgba(255,196,96," + str(clamp(a, 0, 1)) + ")";
	const string L16bLt = "rgba(255,150,185,1)";   // the light pink of the shine on the attacks (still pink)
	static double L16bM(Player p) => p.r - 2;   // how much of the square counts on top of an attack's own size
	static void L16bGlow(double b) { g.shadowColor = PINK; g.shadowBlur = b; }
	static double L16bSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double L16bMod(double a, double n) => a - n * Math.Floor(a / n);
	static double L16bWrap(double a) => L16bMod(a + Math.PI, TAU) - Math.PI;
	// a beat program: the sum of eased steps (beat, amount...) that each take d beats; col picks the amount
	static double L16bSteps(double[] st, int stride, int col, double b, double d)
	{
		double s = 0;
		for (int i = 0; i + stride - 1 < st.Length; i += stride) { double k = (b - st[i]) / d; if (k <= 0) continue; s += st[i + col] * L16bS(Math.Min(1, k)); }
		return s;
	}
	// a rate program (beat, rate...) from b0 to b: each rate holds from its beat to the next one's; col picks the rate
	static double L16bInt(double[] sp, int stride, int col, double b0, double b)
	{
		double a = 0;
		for (int i = 0; i + stride - 1 < sp.Length; i += stride)
		{
			double s0 = Math.Max(sp[i], b0), s1 = i + 2 * stride - 1 < sp.Length ? sp[i + stride] : 1e9; if (b <= s0) break;
			a += sp[i + col] * (Math.Min(b, s1) - s0);
		}
		return a;
	}
	// a pulse 0..1 on the beats in pl: up in 0.1 beat, down in 0.5
	static double L16bPulse(double[] pl, double b)
	{
		double v = 0;
		foreach (var q in pl) { double x = b - q; if (x < 0 || x > 0.6) continue; v = Math.Max(v, x < 0.1 ? x / 0.1 : 1 - (x - 0.1) / 0.5); }
		return v;
	}
	static bool L16bInPoly(double px, double py, IList<(double x, double y)> P, double m)
	{
		bool ins = false; int n = P.Count;
		for (int i = 0, j = n - 1; i < n; j = i++) { var a = P[i]; var c = P[j]; if ((a.y > py) != (c.y > py) && px < (c.x - a.x) * (py - a.y) / (c.y - a.y) + a.x) ins = !ins; }
		if (ins) return true; if (m <= 0) return false;
		for (int i = 0, j = n - 1; i < n; j = i++) if (L16bSeg(px, py, P[j].x, P[j].y, P[i].x, P[i].y) < m) return true;
		return false;
	}
	static void L16bPath(IList<(double x, double y)> P, bool close = true) { g.moveTo(P[0].x, P[0].y); for (int i = 1; i < P.Count; i++) g.lineTo(P[i].x, P[i].y); if (close) g.closePath(); }
	static bool L16bNearLine(double px, double py, IList<(double x, double y)> P, double lim)
	{
		for (int i = 1; i < P.Count; i++) if (L16bSeg(px, py, P[i - 1].x, P[i - 1].y, P[i].x, P[i].y) < lim) return true;
		return false;
	}
	// a small seeded random (the same cracks and pieces every run)
	static double L16bRnd(ref uint s) { s = s * 1664525u + 1013904223u; return (s >> 8) / 16777216.0; }

	// a harmless hazard drawing an attack's warning (draws before every attack, so it never covers one) till t1
	static Hz L16bWarn(string kind, Action<double> draw, double t1)
	{
		var h = new Hz { kind = kind }; h.update = (t, dt) => { if (t >= t1) h.dead = true; }; h.draw = draw; return h;
	}

	// ---------- the rose (rhodonea r = R cos(n u / 2)) ----------
	// petal k of an n-petal rose round (cx, cy) with its axis at ax: from the middle out to R and back, u0..u1 of it (-1..1 = all of it)
	static List<(double x, double y)> L16bPetalPts(double cx, double cy, int n, double R, double ax, int seg, double u0 = -1, double u1 = 1)
	{
		var P = new List<(double x, double y)>(seg + 1); double hw = Math.PI / n;
		for (int i = 0; i <= seg; i++) { double u = hw * (u0 + (u1 - u0) * i / seg), r = R * Math.Cos(n * u / 2), th = ax + u; P.Add((cx + r * JS.cos(th), cy + r * JS.sin(th))); }
		return P;
	}
	// is (px, py) within lim of the rose's line; f (0..1) is how much of it is drawn (petal by petal), gone: petals that fell
	static bool L16bRoseHit(double px, double py, double cx, double cy, int n, double R, double a, double lim, double f = 1, ulong gone = 0)
	{
		double dx = px - cx, dy = py - cy, d = hypot(dx, dy); if (d > R + lim || R < 2) return false;
		double fa = Math.Atan2(dy, dx), fn = f * n;
		for (int k = 0; k < n; k++)
		{
			if ((gone >> k & 1) != 0 || fn <= k) continue;
			double ax = a + k * TAU / n;
			if (d > 3 * lim && Math.Abs(L16bWrap(fa - ax)) > Math.PI / n + lim / d + 0.05) continue;
			if (L16bNearLine(px, py, L16bPetalPts(cx, cy, n, R, ax, 40, -1, Math.Min(1, -1 + 2 * (fn - k))), lim)) return true;
		}
		return false;
	}
	static void L16bRosePath(double cx, double cy, int n, double R, double a, double f = 1, ulong gone = 0)
	{
		double fn = f * n;
		for (int k = 0; k < n; k++)
		{
			if ((gone >> k & 1) != 0 || fn <= k) continue;
			L16bPath(L16bPetalPts(cx, cy, n, R, a + k * TAU / n, 40, -1, Math.Min(1, -1 + 2 * (fn - k))), false);
		}
	}
	// where the pen is when f of the rose is drawn
	static (double x, double y) L16bRosePen(double cx, double cy, int n, double R, double a, double f)
	{
		double fn = Math.Min(f * n, n - 1e-6); int k = (int)Math.Floor(fn); double u = Math.PI / n * (-1 + 2 * (fn - k)), r = R * Math.Cos(n * u / 2), th = a + k * TAU / n + u;
		return (cx + r * JS.cos(th), cy + r * JS.sin(th));
	}

	// ---------- the bride ----------
	// drawn round her face at (x, y); al her alpha, sc her size; she looks at (lx, ly)
	static void L16bBride(double x, double y, double t, double al, double sc, double lx, double ly)
	{
		if (al <= 0.01) return;
		g.save(); g.globalAlpha = al; g.translate(x, y); g.scale(sc, sc);
		double sw = JS.sin(t * 1.6), sw2 = JS.sin(t * 1.1 + 1.3);
		const string SKIN = "rgba(238,203,182,1)", HAIR = "rgba(30,16,22,1)", GOLD = "rgba(232,184,96,1)";
		// the light round her
		var au = g.createRadialGradient(0, 140, 30, 0, 140, 330); au.addColorStop(0, "rgba(255,236,206,0.22)"); au.addColorStop(1, "rgba(255,236,206,0)");
		g.fillStyle = au; g.fillRect(-340, -200, 680, 600);
		// the veil behind her, from the crown down past the hem, its lace edge waving
		{
			var P = new List<(double x, double y)>(); P.Add((-12, -32));
			for (int i = 0; i <= 14; i++) { double k = i / 14.0; P.Add((-16 - 60 * k - 120 * k * k + 10 * sw * k, -32 + 360 * k)); }
			for (int i = 0; i <= 16; i++) { double k = i / 16.0; P.Add((-196 + 10 * sw + 392 * k, 328 + 10 * JS.sin(k * 9 + t * 1.4))); }
			for (int i = 14; i >= 0; i--) { double k = i / 14.0; P.Add((16 + 60 * k + 120 * k * k + 10 * sw * k, -32 + 360 * k)); }
			var vg = g.createLinearGradient(0, -30, 0, 340); vg.addColorStop(0, "rgba(255,252,248,0.42)"); vg.addColorStop(1, "rgba(240,236,255,0.16)");
			g.fillStyle = vg; g.beginPath(); L16bPath(P); g.fill();
			g.strokeStyle = "rgba(255,250,240,0.55)"; g.lineWidth = 1.5; g.stroke();
			// the lace edge: little scallops and dots along the hem of the veil
			g.strokeStyle = "rgba(255,250,240,0.6)"; g.lineWidth = 1.4; g.beginPath();
			for (int i = 0; i < 16; i++) { double k = (i + 0.5) / 16, xx = -196 + 10 * sw + 392 * k, yy = 328 + 10 * JS.sin(k * 9 + t * 1.4); g.moveTo(xx + 11, yy); g.arc(xx, yy, 11, 0, Math.PI); }
			g.stroke();
			g.fillStyle = "rgba(255,252,246,0.5)";
			for (int i = 0; i < 26; i++) { double k = 0.15 + 0.8 * ((i * 0.618) % 1), side = i % 2 == 0 ? -1 : 1, xx = side * (30 + 120 * k * k + 40 * ((i * 0.37) % 1)) + 10 * sw * k, yy = -10 + 330 * k; circle(xx, yy, 1.6); }
		}
		// the long hair down her back
		g.fillStyle = HAIR;
		for (int s = -1; s <= 1; s += 2)
		{
			g.beginPath(); g.moveTo(s * 10, -26); g.bezierCurveTo(s * 40, -20, s * 44, 30, s * (46 + 3 * sw2), 90);
			g.quadraticCurveTo(s * (44 + 4 * sw2), 128, s * (32 + 3 * sw), 132); g.quadraticCurveTo(s * 30, 100, s * 24, 70); g.lineTo(s * 14, 10); g.closePath(); g.fill();
		}
		// the gown: three tiers, the lowest first, each with a scalloped hem, gold embroidery and pearls
		void Tier(double y0, double w0, double y1, double w1, int nsc, double sway, double shade)
		{
			double hgt = y1 - y0;
			g.beginPath(); g.moveTo(-w0, y0);
			g.bezierCurveTo(-w0 - (w1 - w0) * 0.15, y0 + hgt * 0.35, -w1 + sway, y1 - hgt * 0.35, -w1 + sway, y1);
			for (int i = 0; i < nsc; i++)
			{
				double xa = -w1 + 2 * w1 * i / nsc + sway, xb = -w1 + 2 * w1 * (i + 1) / nsc + sway;
				g.quadraticCurveTo((xa + xb) / 2, y1 + 16, xb, y1);
			}
			g.bezierCurveTo(w1 + sway, y1 - hgt * 0.35, w0 + (w1 - w0) * 0.15, y0 + hgt * 0.35, w0, y0); g.closePath();
			var gr = g.createLinearGradient(-w1, 0, w1, 0);
			gr.addColorStop(0, "rgba(" + (int)(212 * shade) + "," + (int)(206 * shade) + "," + (int)(224 * shade) + ",1)");
			gr.addColorStop(0.42, "rgba(255,253,250,1)"); gr.addColorStop(0.6, "rgba(250,247,244,1)");
			gr.addColorStop(1, "rgba(" + (int)(196 * shade) + "," + (int)(190 * shade) + "," + (int)(210 * shade) + ",1)");
			g.fillStyle = gr; g.fillParts(); g.strokeStyle = "rgba(190,180,204,0.7)"; g.lineWidth = 1.2; g.stroke();
			// the folds
			g.strokeStyle = "rgba(180,172,198,0.35)"; g.lineWidth = 1.2; g.beginPath();
			for (int i = 1; i < nsc; i++) { double k = (double)i / nsc, xt = -w0 + 2 * w0 * k, xb = -w1 + 2 * w1 * k + sway; g.moveTo(xt, y0 + 4); g.quadraticCurveTo((xt + xb) / 2 + 6 * sw2, (y0 + y1) / 2, xb, y1 + 6); }
			g.stroke();
			// the gold embroidery over the scallops: a curl in each and a pearl where two meet
			g.strokeStyle = GOLD; g.lineWidth = 1.4; g.beginPath();
			for (int i = 0; i < nsc; i++)
			{
				double xm = -w1 + 2 * w1 * (i + 0.5) / nsc + sway;
				g.moveTo(xm - 7, y1 - 2); g.quadraticCurveTo(xm, y1 - 14, xm + 7, y1 - 2); g.moveTo(xm + 3, y1 - 6); g.arc(xm, y1 - 6, 3, 0, TAU);
			}
			g.stroke();
			g.fillStyle = "rgba(255,255,252,1)";
			for (int i = 0; i <= nsc; i++) circle(-w1 + 2 * w1 * i / nsc + sway, y1 - 1, 2.4);
			// a line of gold leaves round the top of the tier
			g.strokeStyle = "rgba(232,184,96,0.75)"; g.lineWidth = 1; g.beginPath();
			double yl = y1 - hgt * 0.18, wl = w0 + (w1 - w0) * 0.78; for (int i = 0; i < nsc * 2; i++) { double xx = -wl + 2 * wl * (i + 0.5) / (nsc * 2) + sway * 0.8; g.moveTo(xx - 3, yl); g.quadraticCurveTo(xx, yl - 6, xx + 3, yl); }
			g.stroke();
		}
		Tier(106, 26, 300, 152, 11, 9 * sw, 0.9);
		Tier(106, 25, 246, 124, 9, 6 * sw, 0.95);
		Tier(106, 24, 186, 94, 7, 3 * sw, 1.0);
		// shoulders, neck and arms (off the shoulder)
		g.fillStyle = SKIN; g.beginPath(); g.moveTo(-7, 14); g.lineTo(7, 14); g.lineTo(8, 40); g.quadraticCurveTo(30, 44, 44, 54); g.lineTo(30, 62); g.lineTo(-30, 62); g.lineTo(-44, 54); g.quadraticCurveTo(-30, 44, -8, 40); g.closePath(); g.fill();
		g.strokeStyle = SKIN; g.lineCap = "round"; g.lineJoin = "round"; g.lineWidth = 10;
		g.beginPath(); g.moveTo(-42, 58); g.quadraticCurveTo(-48, 86, -36, 104); g.lineTo(-10, 124); g.moveTo(42, 58); g.quadraticCurveTo(48, 86, 36, 104); g.lineTo(10, 124); g.stroke();
		// the lace sleeves over the arms
		g.strokeStyle = "rgba(255,252,248,0.4)"; g.lineWidth = 14;
		g.beginPath(); g.moveTo(-42, 60); g.quadraticCurveTo(-48, 86, -36, 104); g.lineTo(-14, 121); g.moveTo(42, 60); g.quadraticCurveTo(48, 86, 36, 104); g.lineTo(14, 121); g.stroke();
		g.lineCap = "butt"; g.lineJoin = "miter";
		g.fillStyle = "rgba(255,255,255,0.75)";
		for (int i = 0; i < 9; i++) { double k = i / 8.0; foreach (int s in new[] { -1, 1 }) circle(s * (k < 0.55 ? 44 - 4 * JS.sin(k * 5.7) : 36 - 44 * (k - 0.55)), 62 + 60 * k, 1.3); }
		// the bodice: a sweetheart neckline, gold lace lines down to the waist, the belt with its jewel
		g.beginPath(); g.moveTo(-31, 58); g.quadraticCurveTo(-17, 46, 0, 60); g.quadraticCurveTo(17, 46, 31, 58);
		g.bezierCurveTo(34, 76, 26, 94, 24, 110); g.lineTo(-24, 110); g.bezierCurveTo(-26, 94, -34, 76, -31, 58); g.closePath();
		var bg = g.createLinearGradient(-32, 0, 32, 0); bg.addColorStop(0, "rgba(214,208,226,1)"); bg.addColorStop(0.45, "rgba(255,253,250,1)"); bg.addColorStop(1, "rgba(204,198,218,1)");
		g.fillStyle = bg; g.fill(); g.strokeStyle = "rgba(190,180,204,0.8)"; g.lineWidth = 1; g.stroke();
		g.strokeStyle = "rgba(232,184,96,0.85)"; g.lineWidth = 1.1; g.beginPath();
		for (int i = -2; i <= 2; i++) { g.moveTo(i * 11, 58 - Math.Abs(i) * 2); g.quadraticCurveTo(i * 7 + 3 * JS.sin(i), 84, i * 3, 104); }
		g.stroke();
		g.fillStyle = "rgba(255,255,252,0.95)"; for (int i = 0; i < 7; i++) circle(-15 + 5 * i, 66 + 5 * JS.sin(i * 1.3), 1.2);
		var bl = g.createLinearGradient(0, 104, 0, 114); bl.addColorStop(0, "rgba(255,222,150,1)"); bl.addColorStop(1, "rgba(200,140,60,1)");
		g.fillStyle = bl; g.beginPath(); g.roundRect(-26, 104, 52, 9, 4); g.fill();
		g.fillStyle = "rgba(250,250,255,1)"; g.beginPath(); g.moveTo(0, 101); g.lineTo(7, 108.5); g.lineTo(0, 116); g.lineTo(-7, 108.5); g.closePath(); g.fill();
		g.fillStyle = "rgba(60,170,160,1)"; circle(0, 108.5, 2.6);
		// the pearl necklace
		for (int i = 0; i < 11; i++) { double k = (i - 5) / 5.0, px2 = k * 13, py2 = 38 + 8 * (1 - k * k); g.fillStyle = "rgba(250,248,244,1)"; circle(px2, py2, 2.3); g.fillStyle = "rgba(255,255,255,1)"; circle(px2 - 0.7, py2 - 0.7, 0.8); }
		// the bouquet of white roses at her waist, its ribbons hanging
		g.strokeStyle = "rgba(255,248,232,0.85)"; g.lineWidth = 3;
		g.beginPath(); g.moveTo(-3, 132); g.bezierCurveTo(-10 + 5 * sw, 160, 4 + 6 * sw2, 180, -6 + 8 * sw, 206); g.moveTo(3, 132); g.bezierCurveTo(12 + 5 * sw2, 158, 2 + 6 * sw, 182, 12 + 8 * sw2, 200); g.stroke();
		g.fillStyle = "rgba(46,92,60,1)";
		for (int i = 0; i < 7; i++) { double an = -Math.PI / 2 + (i - 3) * 0.5; g.beginPath(); g.ellipse(26 * JS.cos(an) * 0.9, 124 + 18 * JS.sin(an) + 14, 9, 4, an, 0, TAU); g.fill(); }
		var rs = new (double x, double y)[] { (0, 112), (-13, 118), (13, 118), (-7, 128), (7, 128), (-17, 130), (17, 130), (0, 136) };
		foreach (var (rx, ry) in rs)
		{
			g.fillStyle = "rgba(252,248,242,1)"; circle(rx, ry, 8.5);
			g.strokeStyle = "rgba(200,188,186,0.9)"; g.lineWidth = 1; g.beginPath();
			for (int j = 0; j < 3; j++) { double rr = 2 + j * 2.1; g.moveTo(rx + rr, ry); g.arc(rx, ry, rr, 0, Math.PI * 1.5); }
			g.stroke();
		}
		// the face
		var fg = g.createRadialGradient(-5, -6, 4, 0, 0, 28); fg.addColorStop(0, "rgba(248,220,202,1)"); fg.addColorStop(1, "rgba(222,184,162,1)");
		g.fillStyle = fg; g.beginPath(); g.ellipse(0, 0, 19.5, 25, 0, 0, TAU); g.fill();
		g.fillStyle = "rgba(240,160,140,0.18)"; circle(-11, 8, 5); circle(11, 8, 5);
		// the eyes: kohl, long lashes; she blinks, and she looks at you
		double bl0 = L16bMod(t, 3.7), open = bl0 < 0.14 ? 0.15 : 1, ldx = lx - x, ldy = ly - y - 40, ll = Math.Max(1, hypot(ldx, ldy)), ex = 1.3 * ldx / ll, ey = 0.8 * ldy / ll;
		for (int s = -1; s <= 1; s += 2)
		{
			double cx2 = s * 8, cy2 = -2;
			g.fillStyle = "rgba(252,250,248,1)"; g.beginPath(); g.ellipse(cx2, cy2, 5.4, 2.8 * open, 0, 0, TAU); g.fill();
			if (open > 0.5)
			{
				g.fillStyle = "rgba(70,40,26,1)"; circle(cx2 + ex, cy2 + ey, 2.6);
				g.fillStyle = "rgba(10,6,6,1)"; circle(cx2 + ex, cy2 + ey, 1.3);
				g.fillStyle = "rgba(255,255,255,0.95)"; circle(cx2 + ex - 0.8, cy2 + ey - 0.8, 0.7);
			}
			g.strokeStyle = "rgba(14,8,10,1)"; g.lineWidth = 1.7; g.beginPath();
			g.moveTo(cx2 - s * 5.6, cy2 + 0.4); g.quadraticCurveTo(cx2, cy2 - 3.6 * open - 0.4, cx2 + s * 5.6, cy2 - 0.2); g.lineTo(cx2 + s * 8.4, cy2 - 2.4); g.stroke();
			g.lineWidth = 0.8; g.beginPath(); g.moveTo(cx2 - s * 4.6, cy2 + 1.2); g.quadraticCurveTo(cx2, cy2 + 3.4 * open, cx2 + s * 5, cy2 + 0.8); g.stroke();
			g.lineWidth = 0.9; g.beginPath();
			for (int j = 0; j < 5; j++) { double lxx = cx2 + s * (-2 + j * 1.8), lyy = cy2 - 2.7 * open - 0.3; g.moveTo(lxx, lyy); g.lineTo(lxx + s * (1 + j * 0.4), lyy - 2.2 - j * 0.2); }
			g.stroke();
			g.strokeStyle = "rgba(48,26,22,1)"; g.lineWidth = 1.6; g.beginPath(); g.moveTo(cx2 - s * 5, cy2 - 6.5); g.quadraticCurveTo(cx2 + s * 1, cy2 - 9.6, cx2 + s * 7, cy2 - 7); g.stroke();
		}
		g.strokeStyle = "rgba(176,124,110,0.6)"; g.lineWidth = 1; g.beginPath(); g.moveTo(0.5, 0); g.quadraticCurveTo(-1.6, 6, 0, 8.5); g.moveTo(-2.4, 8.6); g.quadraticCurveTo(0, 9.8, 2.4, 8.6); g.stroke();
		// the lips
		g.fillStyle = "rgba(126,26,40,1)"; g.beginPath(); g.moveTo(-6, 14); g.quadraticCurveTo(-3, 11.4, 0, 12.8); g.quadraticCurveTo(3, 11.4, 6, 14); g.quadraticCurveTo(0, 19.4, -6, 14); g.closePath(); g.fill();
		g.strokeStyle = "rgba(80,14,24,0.9)"; g.lineWidth = 0.7; g.beginPath(); g.moveTo(-5.6, 14); g.quadraticCurveTo(0, 15.2, 5.6, 14); g.stroke();
		g.fillStyle = "rgba(255,220,220,0.4)"; g.beginPath(); g.ellipse(1.4, 16.2, 2, 0.8, 0, 0, TAU); g.fill();
		// the hair over the forehead (parted), the locks by her face, the bun under the veil
		g.fillStyle = HAIR; circle(0, -27, 13);
		g.beginPath(); g.moveTo(0, -24); g.bezierCurveTo(-16, -26, -24, -14, -22, 4); g.quadraticCurveTo(-25, 20, -21 + sw2, 36); g.quadraticCurveTo(-18, 22, -18, 4);
		g.bezierCurveTo(-18, -10, -10, -16, 0, -18); g.bezierCurveTo(10, -16, 18, -10, 18, 4); g.quadraticCurveTo(18, 22, 21 + sw2, 36); g.quadraticCurveTo(25, 20, 22, 4); g.bezierCurveTo(24, -14, 16, -26, 0, -24); g.closePath(); g.fill();
		g.strokeStyle = "rgba(90,56,60,0.6)"; g.lineWidth = 0.8; g.beginPath(); g.moveTo(-2, -22); g.quadraticCurveTo(-14, -18, -19, -2); g.moveTo(2, -22); g.quadraticCurveTo(14, -18, 19, -2); g.stroke();
		// the earrings
		for (int s = -1; s <= 1; s += 2)
		{
			double ex2 = s * 19.5, sway = 1.2 * sw2;
			g.strokeStyle = GOLD; g.lineWidth = 1; g.beginPath(); g.moveTo(ex2, 6); g.lineTo(ex2 + sway, 14); g.stroke();
			g.fillStyle = GOLD; circle(ex2 + sway, 15, 2.4); g.fillStyle = "rgba(252,250,246,1)"; circle(ex2 + sway, 19.5, 2.6);
		}
		// the tiara, its stones catching the light
		g.strokeStyle = GOLD; g.lineWidth = 1.6; g.beginPath(); g.moveTo(-18, -20);
		for (int i = 0; i <= 8; i++) { double xx = -18 + 36 * i / 8.0, yy = -22 - (i % 2 == 0 ? 3 : 0) - (i == 4 ? 9 : i == 2 || i == 6 ? 4 : 0) - 2 * (1 - Math.Abs(i - 4) / 4.0); g.lineTo(xx, yy); }
		g.stroke();
		g.fillStyle = "rgba(255,255,255,1)"; for (int i = 0; i <= 8; i += 2) circle(-18 + 36 * i / 8.0, -24 - (i == 4 ? 9 : i == 2 || i == 6 ? 4 : 0) - 2 * (1 - Math.Abs(i - 4) / 4.0), i == 4 ? 2.6 : 1.6);
		g.fillStyle = "rgba(70,190,180,1)"; circle(0, -35.5, 1.4);
		for (int i = 0; i < 3; i++)
		{
			double tw = 0.5 + 0.5 * JS.sin(t * 3 + i * 2.1); if (tw < 0.6) continue;
			double sx = -9 + 9 * i, sy = -30 - (i == 1 ? 8 : 2), sl = 4 * tw; g.strokeStyle = "rgba(255,255,255," + str(tw) + ")"; g.lineWidth = 0.9;
			g.beginPath(); g.moveTo(sx - sl, sy); g.lineTo(sx + sl, sy); g.moveTo(sx, sy - sl); g.lineTo(sx, sy + sl); g.stroke();
		}
		// the veil over her head (sheer), its lace edge down to the shoulders
		g.fillStyle = "rgba(255,252,248,0.2)"; g.beginPath(); g.moveTo(-26, -18); g.quadraticCurveTo(0, -46, 26, -18); g.quadraticCurveTo(34, 20, 46 + 4 * sw, 64); g.lineTo(34, 66); g.quadraticCurveTo(26, 20, 20, -10);
		g.quadraticCurveTo(0, -30, -20, -10); g.quadraticCurveTo(-26, 20, -34, 66); g.lineTo(-46 + 4 * sw, 64); g.quadraticCurveTo(-34, 20, -26, -18); g.closePath(); g.fill();
		g.strokeStyle = "rgba(255,252,246,0.55)"; g.lineWidth = 1; g.stroke();
		// the sparkles on the gown
		for (int i = 0; i < 18; i++)
		{
			double tw = JS.sin(t * 2.3 + i * 1.7); if (tw < 0.55) continue;
			double k = (i * 0.618) % 1, yy = 120 + 170 * k, xx = (((i * 0.383) % 1) * 2 - 1) * (40 + 120 * k), sl = 3.5 * tw;
			g.strokeStyle = "rgba(255,246,220," + str(tw) + ")"; g.lineWidth = 1; g.beginPath(); g.moveTo(xx - sl, yy); g.lineTo(xx + sl, yy); g.moveTo(xx, yy - sl); g.lineTo(xx, yy + sl); g.stroke();
		}
		g.restore();
	}
	// where the bride is at beat b: bp is (beat, x, y, beats to get there)...; she starts at the first one
	static (double x, double y) L16bBrideAt(double[] bp, double b)
	{
		if (bp.Length < 4) return (W / 2, 150);
		double x = bp[1], y = bp[2];
		for (int i = 4; i + 3 < bp.Length; i += 4)
		{
			double k = L16bS(L16bK(b, bp[i], bp[i] + Math.Max(0.01, bp[i + 3]))); if (k <= 0) break;
			x += (bp[i + 1] - x) * k; y += (bp[i + 2] - y) * k;
		}
		return (x, y);
	}

	// ---------- the stage (harmless) ----------
	// One hazard for the whole level, made first (and first again after a checkpoint); veil: every attack is drawn after it.
	// Part k runs from beat pb[k-1]: 1 the garden at night, 2 the room with the mirror (it cracks on the beats in mc), 3 the wedding hall
	// with the bride (where bp says; she comes at pb[2] and goes at the end)
	public static Hz L16bStage(double[] pb, double t1, double[] bp, double[] mc)
	{
		var h = new Hz { kind = "L16bStage", veil = true };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			for (int k = 0; k < pb.Length; k++)
			{
				double s0 = B(pb[k]), s1 = k + 1 < pb.Length ? B(pb[k + 1]) : t1;
				double a = Math.Min(1, Math.Min((t - s0) / 0.8 + (k == 0 ? 0 : 0.5), (s1 - t) / 0.8 + (k + 1 < pb.Length ? 0.5 : 0)));
				if (a > 0) Part(k + 1, a, t);
			}
		};
		void Bush(double x, double y, double s, double t)
		{
			// a rose bush in the dark: leaves and dead dark roses
			g.fillStyle = "rgba(16,34,26,0.9)";
			for (int i = 0; i < 9; i++) { double an = -Math.PI + i * Math.PI / 8, r = s * (0.7 + 0.3 * ((i * 0.53) % 1)); g.beginPath(); g.ellipse(x + r * JS.cos(an), y + r * 0.7 * JS.sin(an), s * 0.42, s * 0.26, an, 0, TAU); g.fill(); }
			g.fillStyle = "rgba(62,14,28,0.95)";
			for (int i = 0; i < 4; i++) { double an = -Math.PI * 0.85 + i * Math.PI * 0.23; circle(x + s * 0.65 * JS.cos(an), y + s * 0.5 * JS.sin(an) - 4 + 2 * JS.sin(t + i), s * 0.16); }
		}
		void Part(int part, double a, double t)
		{
			g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var bgr = g.createLinearGradient(0, 0, 0, H); bgr.addColorStop(0, "rgba(6,8,22,0.86)"); bgr.addColorStop(1, "rgba(10,18,20,0.86)"); g.fillStyle = bgr; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(200,200,235,0.4)";
				for (int i = 0; i < 46; i++) { double x = (i * 331.7) % W, y = (i * 157.3) % 330, tw = 0.5 + 0.5 * JS.sin(t * 1.3 + i); g.globalAlpha = a * (0.25 + 0.5 * tw); g.fillRect(x, y, 2, 2); }
				g.globalAlpha = a;
				var mg = g.createRadialGradient(1080, 110, 10, 1080, 110, 160); mg.addColorStop(0, "rgba(220,220,250,0.25)"); mg.addColorStop(1, "rgba(220,220,250,0)"); g.fillStyle = mg; g.fillRect(900, 0, 380, 300);
				g.fillStyle = "rgba(232,228,250,0.6)"; circle(1080, 110, 36);
				// the garden arch (a trellis) and the bushes round the edges
				g.strokeStyle = "rgba(40,54,48,0.7)"; g.lineWidth = 8; g.beginPath(); g.moveTo(330, H); g.lineTo(330, 300); g.arc(640, 300, 310, Math.PI, 0); g.lineTo(950, H); g.stroke();
				g.lineWidth = 2; g.strokeStyle = "rgba(40,54,48,0.45)"; g.beginPath(); for (int i = 0; i < 9; i++) { double an = Math.PI + i * Math.PI / 8; g.moveTo(640 + 290 * JS.cos(an), 300 + 290 * JS.sin(an)); g.lineTo(640 + 330 * JS.cos(an), 300 + 330 * JS.sin(an)); } g.stroke();
				for (int i = 0; i < 8; i++) Bush(80 + i * 160 + (i % 2) * 20, H - 20 - (i % 3) * 10, 60 + (i % 3) * 10, t);
				Bush(60, 300, 50, t); Bush(W - 60, 330, 54, t);
				// fireflies
				for (int i = 0; i < 16; i++) { double x = (i * 211.3 + 40 * JS.sin(t * 0.5 + i)) % W, y = 200 + (i * 97.1) % 440 + 25 * JS.sin(t * 0.8 + i * 2), tw = 0.5 + 0.5 * JS.sin(t * 2 + i); g.fillStyle = L16bAu(0.15 + 0.4 * tw); circle(x, y, 2.2); }
				// fallen petals on the ground
				g.fillStyle = "rgba(110,76,92,0.4)";
				for (int i = 0; i < 30; i++) { double x = (i * 173.9) % W, y = 520 + (i * 61.7) % 190; g.beginPath(); g.ellipse(x, y, 6, 3, i, 0, TAU); g.fill(); }
			}
			else if (part == 2)
			{
				g.fillStyle = "rgba(10,10,18,0.84)"; g.fillRect(0, 0, W, H);
				// the wallpaper's faint pattern
				g.strokeStyle = "rgba(90,90,120,0.1)"; g.lineWidth = 2; g.beginPath();
				for (double x = 40; x < W; x += 120) for (double y = 40; y < H; y += 120) { g.moveTo(x + 14, y); g.arc(x, y, 14, 0, TAU); }
				g.stroke();
				// the big mirror: its glass shows her (faint, in white), and its cracks grow with every hit
				const double mx = 640, my = 360, rx = 330, ry = 300;
				g.save(); g.beginPath(); g.ellipse(mx, my, rx, ry, 0, 0, TAU); g.clip();
				var gl = g.createLinearGradient(mx - rx, my - ry, mx + rx, my + ry); gl.addColorStop(0, "rgba(120,140,180,0.16)"); gl.addColorStop(0.5, "rgba(200,210,240,0.08)"); gl.addColorStop(1, "rgba(100,110,150,0.16)");
				g.fillStyle = gl; g.fillRect(mx - rx, my - ry, 2 * rx, 2 * ry);
				L16bBride(mx, my - 150, t, 0.1, 0.95, mx, my);
				g.strokeStyle = "rgba(255,255,255,0.08)"; g.lineWidth = 30; g.beginPath(); double sh = L16bMod(t * 60, 1400) - 400; g.moveTo(mx - 500 + sh, my - 400); g.lineTo(mx - 200 + sh, my + 400); g.stroke();
				double b = BT(t); g.strokeStyle = "rgba(170,180,215,0.16)"; g.lineWidth = 1.2; g.beginPath();
				for (int i = 0; i < mc.Length; i++)
				{
					if (b < mc[i]) break;
					uint s = (uint)(i * 7919 + 13); double cx = mx + (L16bRnd(ref s) - 0.5) * rx * 1.2, cy = my + (L16bRnd(ref s) - 0.5) * ry * 1.2;
					for (int r = 0; r < 3; r++)
					{
						double an = r * TAU / 3 + L16bRnd(ref s), x = cx, y = cy; g.moveTo(x, y);
						for (int j = 0; j < 4; j++) { an += (L16bRnd(ref s) - 0.5) * 0.7; x += 36 * JS.cos(an); y += 36 * JS.sin(an); g.lineTo(x, y); }
					}
				}
				g.stroke(); g.restore();
				// its gold frame with leaves
				g.strokeStyle = "rgba(200,150,70,0.55)"; g.lineWidth = 14; g.beginPath(); g.ellipse(mx, my, rx + 8, ry + 8, 0, 0, TAU); g.stroke();
				g.strokeStyle = "rgba(250,210,130,0.35)"; g.lineWidth = 2; g.beginPath(); g.ellipse(mx, my, rx + 16, ry + 16, 0, 0, TAU); g.stroke();
				g.fillStyle = "rgba(210,160,80,0.45)";
				for (int i = 0; i < 24; i++) { double an = i * TAU / 24; g.beginPath(); g.ellipse(mx + (rx + 20) * JS.cos(an), my + (ry + 20) * JS.sin(an), 9, 4, an + Math.PI / 2, 0, TAU); g.fill(); }
				circle(mx, my - ry - 30, 14);
			}
			else
			{
				// the wedding hall: dark velvet, the curtains, the aisle, the strings of lights, the chandelier, gold confetti, and her
				var bgr = g.createLinearGradient(0, 0, 0, H); bgr.addColorStop(0, "rgba(26,8,20,0.86)"); bgr.addColorStop(1, "rgba(14,6,14,0.86)"); g.fillStyle = bgr; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(40,10,24,0.9)";
				for (int s = 0; s < 2; s++)
				{
					double x0 = s == 0 ? 0 : W, d = s == 0 ? 1 : -1; g.beginPath(); g.moveTo(x0, 0); g.lineTo(x0 + d * 170, 0);
					g.quadraticCurveTo(x0 + d * 90, 260, x0 + d * 120 + d * 6 * JS.sin(t * 0.7 + s), H); g.lineTo(x0, H); g.closePath(); g.fill();
				}
				g.strokeStyle = "rgba(200,150,70,0.5)"; g.lineWidth = 4; g.beginPath(); g.moveTo(96, 300); g.lineTo(132, 316); g.moveTo(W - 96, 300); g.lineTo(W - 132, 316); g.stroke();
				g.fillStyle = "rgba(50,14,20,0.6)"; g.beginPath(); g.moveTo(560, 330); g.lineTo(720, 330); g.lineTo(900, H); g.lineTo(380, H); g.closePath(); g.fill();
				g.strokeStyle = "rgba(200,150,70,0.3)"; g.lineWidth = 2; g.beginPath(); g.moveTo(566, 330); g.lineTo(392, H); g.moveTo(714, 330); g.lineTo(888, H); g.stroke();
				for (int r = 0; r < 3; r++)
				{
					double y0 = 26 + r * 30, sag = 50 + r * 10, ph = t * 0.9 + r;
					g.strokeStyle = "rgba(120,90,60,0.35)"; g.lineWidth = 1; g.beginPath(); g.moveTo(0, y0); g.quadraticCurveTo(W / 2, y0 + 2 * sag + 6 * JS.sin(ph), W, y0); g.stroke();
					for (int i = 1; i < 24; i++)
					{
						double k = i / 24.0, x = W * k, y = y0 + 4 * sag * k * (1 - k) + 6 * JS.sin(ph) * 4 * k * (1 - k), tw = 0.6 + 0.4 * JS.sin(t * 2.2 + i * 1.3 + r);
						g.fillStyle = L16bAu(0.25 + 0.45 * tw); circle(x, y + 4, 2.6);
					}
				}
				g.strokeStyle = "rgba(220,170,90,0.4)"; g.lineWidth = 2; g.beginPath(); g.moveTo(640, 0); g.lineTo(640, 30);
				for (int i = 0; i < 3; i++) { double ry2 = 10 + i * 6; g.moveTo(640 + 70 - i * 18, 40 + i * 14); g.ellipse(640, 40 + i * 14, 70 - i * 18, ry2, 0, 0, Math.PI); }
				g.stroke();
				g.fillStyle = "rgba(230,240,255,0.4)"; for (int i = 0; i < 12; i++) { double an = i * Math.PI / 11; circle(640 - 70 * JS.cos(an), 44 + 12 * JS.sin(an) + 6, 2); }
				for (int i = 0; i < 40; i++)
				{
					double x = (i * 211.7) % W + JS.sin(t * 0.7 + i) * 20, y = ((i * 97.3) + t * (24 + i % 5 * 6)) % (H + 20) - 10;
					g.fillStyle = L16bAu(0.2 + 0.15 * JS.sin(t * 3 + i)); g.save(); g.translate(x, y); g.rotate(t * 2 + i); g.fillRect(-3, -1.5, 6, 3); g.restore();
				}
				double b = BT(t), ba = L16bK(b, pb[2], pb[2] + 3) * (1 - L16bK(b, 325, 329.4));
				var bpos = L16bBrideAt(bp, b); L16bBride(bpos.x, bpos.y, t, ba, 1.15, player?.x ?? W / 2, player?.y ?? H / 2);
			}
			var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 780); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.65)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		}
		return h;
	}

	// ---------- a rose ----------
	// n petals, its line th thick, round (cx, cy) (or round it at orad: at oa0, then the steps ost (beat, angle) in od beats: the roses change
	// places). R: from R0 by the steps rst (beat, dR) in 0.5 beat, up by dp on the pulse beats pl; grow > 0: it opens from 0 in grow beats.
	// Turning: a0 and the rate program sp (beat, angle a beat). tr > 0: a pen draws it in tr beats from ta. drop (beat, petal): petals that
	// fall (another event throws them). Its outline shows from tw, its next place 1 beat before a move
	public static Hz L16bRose(double cx, double cy, double orad, double oa0, double[] ost, int n, double R0, double[] rst, double[] pl, double dp,
		double grow, double a0, double[] sp, double th, double tr, double[] drop, double tw, double ta, double te, double od = 1)
	{
		var h = new Hz { kind = "L16bRose" }; double b0 = BT(ta);
		(double x, double y, double R, double a, double f, ulong gone) St(double t)
		{
			double b = BT(t), x = cx, y = cy;
			if (orad > 0) { double oa = oa0 + L16bSteps(ost, 2, 1, b, od); x += orad * JS.cos(oa); y += orad * JS.sin(oa); }
			double R = R0 + L16bSteps(rst, 2, 1, b, 0.5) + dp * L16bPulse(pl, b);
			if (grow > 0) R *= L16bK(b, b0, b0 + grow); else if (tr <= 0) R *= L16bS(L16bK(b, b0, b0 + 0.35));
			ulong gone = 0; for (int i = 0; i + 1 < drop.Length; i += 2) if (b >= drop[i]) gone |= 1UL << (int)drop[i + 1];
			return (x, y, R, a0 + L16bInt(sp, 2, 1, b0, b), tr > 0 ? L16bK(b, b0, b0 + tr) : 1, gone);
		}
		h.update = (t, dt) => { if (t >= te + 0.4 * SPB) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			g.lineJoin = "round"; g.lineCap = "round";
			if (t < ta)
			{
				var q0 = St(ta); double k = L16bK(t, tw, ta), Rw = grow > 0 || tr > 0 ? R0 : q0.R;
				g.setLineDash(new double[] { 12, 10 }); g.strokeStyle = L16bG(0.12 + 0.35 * k); g.lineWidth = 2; g.beginPath(); L16bRosePath(q0.x, q0.y, n, Rw, q0.a); g.stroke(); g.setLineDash(new double[0]);
				g.lineJoin = "miter"; g.lineCap = "butt"; return;
			}
			var q = St(t); double b = BT(t), fade = 1 - L16bK(t, te, te + 0.4 * SPB);
			if (t < te)
			{
				for (int i = 0; i + 1 < ost.Length; i += 2)
				{
					double sb = ost[i]; if (b < sb - 1 || b >= sb) continue;
					var q2 = St(B(sb + od + 0.01)); double k = L16bK(b, sb - 1, sb);
					g.strokeStyle = L16bG(0.15 + 0.4 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); L16bRosePath(q2.x, q2.y, n, q2.R, q2.a, 1, q.gone); g.stroke(); g.setLineDash(new double[0]);
				}
				if (dp > 0) { g.strokeStyle = L16bP(0.14); g.lineWidth = 2; g.beginPath(); L16bRosePath(q.x, q.y, n, q.R + dp * (1 - L16bPulse(pl, b)), q.a, q.f, q.gone); g.stroke(); }
			}
			g.save(); if (t >= te) g.globalAlpha *= fade * 0.6; L16bGlow(14); g.strokeStyle = t < te ? PINK : L16bG(1); g.lineWidth = th; g.beginPath(); L16bRosePath(q.x, q.y, n, q.R, q.a, q.f, q.gone); g.stroke(); g.restore();
			if (t < te) { g.strokeStyle = L16bLt; g.lineWidth = Math.Max(1.5, th * 0.18); g.beginPath(); L16bRosePath(q.x, q.y, n, q.R, q.a, q.f, q.gone); g.stroke(); }
			if (q.f < 1 && t < te) { var pen = L16bRosePen(q.x, q.y, n, q.R, q.a, q.f); g.save(); L16bGlow(20); g.fillStyle = PINK; circle(pen.x, pen.y, th / 2 + 2); g.restore(); g.fillStyle = L16bLt; circle(pen.x, pen.y, th * 0.22); }
			g.lineJoin = "miter"; g.lineCap = "butt";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			var q = St(t); return L16bRoseHit(p.x, p.y, q.x, q.y, n, q.R, q.a, th / 2 + L16bM(p), q.f, q.gone);
		};
		return h;
	}

	// ---------- a thorn vine ----------
	// from (x0, y0) at angle an, L long, waving A across every lam px (phase ph); its tip grows v px/s from ta; at te it dries (harmless) and
	// fades. Its path shows (dashed) from tw
	public static Hz L16bVine(double x0, double y0, double an, double L, double A, double lam, double ph, double v, double th, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bVine" };
		double c = JS.cos(an), s = JS.sin(an); var P = new List<(double x, double y)>();
		for (double d = 0; d <= L + 0.1; d += 10) { double w = A * Math.Sin(TAU * d / lam + ph); P.Add((x0 + d * c - w * s, y0 + d * s + w * c)); }
		int N(double t) => Math.Min(P.Count, 1 + (int)Math.Floor(Math.Max(0, v * (t - ta)) / 10));
		h.update = (t, dt) => { if (t >= te + 0.5 * SPB) h.dead = true; };
		void Body(int n, string col, bool thorns)
		{
			if (n < 2) return;
			g.strokeStyle = col; g.lineWidth = th; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath(); L16bPath(P.Take(n).ToList(), false); g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter";
			if (!thorns) return;
			g.fillStyle = col; g.beginPath();
			for (int i = 3, sd = 1; i < n - 1; i += 4, sd = -sd)
			{
				double tx = P[i + 1].x - P[i - 1].x, ty = P[i + 1].y - P[i - 1].y, tl = Math.Max(1e-6, hypot(tx, ty)); tx /= tl; ty /= tl; double nx = -ty * sd, ny = tx * sd;
				g.moveTo(P[i].x - tx * 5 + nx * th * 0.3, P[i].y - ty * 5 + ny * th * 0.3); g.lineTo(P[i].x + tx * 5 + nx * th * 0.3, P[i].y + ty * 5 + ny * th * 0.3); g.lineTo(P[i].x + tx * 3 + nx * (th / 2 + 7), P[i].y + ty * 3 + ny * (th / 2 + 7)); g.closePath();
			}
			g.fill();
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta) { double k = L16bK(t, tw, ta); g.setLineDash(new double[] { 10, 10 }); g.strokeStyle = L16bG(0.12 + 0.4 * k); g.lineWidth = 2; g.beginPath(); L16bPath(P, false); g.stroke(); g.setLineDash(new double[0]); return; }
			int n = N(t);
			if (t >= te) { g.save(); g.globalAlpha *= 0.6 * (1 - L16bK(t, te, te + 0.5 * SPB)); Body(n, "rgba(90,70,60,1)", true); g.restore(); return; }
			if (n < P.Count) { g.setLineDash(new double[] { 10, 10 }); g.strokeStyle = L16bG(0.3); g.lineWidth = 2; g.beginPath(); L16bPath(P.Skip(n - 1).ToList(), false); g.stroke(); g.setLineDash(new double[0]); }
			g.save(); L16bGlow(12); Body(n, PINK, true); g.restore();
			g.strokeStyle = L16bLt; g.lineWidth = 2; g.beginPath(); L16bPath(P.Take(n).ToList(), false); g.stroke();
			if (n < P.Count) { var hd = P[n - 1]; g.save(); L16bGlow(16); g.fillStyle = PINK; circle(hd.x, hd.y, th / 2 + 3); g.restore(); g.fillStyle = L16bLt; circle(hd.x, hd.y, 3); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double lim = th / 2 + 1 + L16bM(p), u = (p.x - x0) * c + (p.y - y0) * s, w = -(p.x - x0) * s + (p.y - y0) * c;
			if (Math.Abs(w) > A + lim + 2 || u < -lim - 2 || u > L + lim + 2) return false;
			int n = N(t), i0 = Math.Max(1, (int)((u - A - lim) / 10) - 1), i1 = Math.Min(n - 1, (int)((u + A + lim) / 10) + 2);
			for (int i = i0; i <= i1; i++) if (L16bSeg(p.x, p.y, P[i - 1].x, P[i - 1].y, P[i].x, P[i].y) < lim) return true;
			return n == 1 && hypot(p.x - P[0].x, p.y - P[0].y) < lim;
		};
		return h;
	}

	// ---------- petals and splinters thrown out ----------
	// from (x, y) along the angles an (starting d0 out), v px/s from ta. shape 0: a rose petal (aa long, bb wide), 1: a glass splinter (a
	// sharp triangle that spins sp rad/s). Their lines show from tw
	public static Hz L16bPetal(double x, double y, double[] an, double v, double aa, double bb, int shape, double sp, double tw, double ta, double d0 = 0)
	{
		var h = new Hz { kind = "L16bPetal" };
		h.update = (t, dt) => { if (t > ta + (1700 - d0) / v) h.dead = true; };
		List<(double x, double y)> Tri(double px, double py, double a) => new() { (px + aa * JS.cos(a), py + aa * JS.sin(a)), (px + bb * JS.cos(a + 2.3), py + bb * JS.sin(a + 2.3)), (px + bb * 0.8 * JS.cos(a - 2.5), py + bb * 0.8 * JS.sin(a - 2.5)) };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.1 + 0.35 * k); g.lineWidth = 2; g.beginPath();
				foreach (var a in an) { g.moveTo(x + (d0 + 20) * JS.cos(a), y + (d0 + 20) * JS.sin(a)); g.lineTo(x + 1500 * JS.cos(a), y + 1500 * JS.sin(a)); }
				g.stroke(); return;
			}
			double d = d0 + v * (t - ta);
			g.save(); L16bGlow(12); g.fillStyle = PINK;
			foreach (var a in an)
			{
				double px = x + d * JS.cos(a), py = y + d * JS.sin(a);
				if (shape == 0) { g.beginPath(); g.ellipse(px, py, aa, bb, a, 0, TAU); g.fill(); }
				else { g.beginPath(); L16bPath(Tri(px, py, a + sp * (t - ta))); g.fill(); }
			}
			g.restore();
			g.strokeStyle = L16bLt; g.lineWidth = 1.5; g.beginPath();
			foreach (var a in an)
			{
				double px = x + d * JS.cos(a), py = y + d * JS.sin(a);
				if (shape == 0) { g.moveTo(px - aa * 0.7 * JS.cos(a), py - aa * 0.7 * JS.sin(a)); g.lineTo(px + aa * 0.7 * JS.cos(a), py + aa * 0.7 * JS.sin(a)); }
				else { var T = Tri(px, py, a + sp * (t - ta)); g.moveTo((T[0].x + px) / 2, (T[0].y + py) / 2); g.lineTo((T[1].x + px) / 2, (T[1].y + py) / 2); }
			}
			g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double d = d0 + v * (t - ta), m = L16bM(p);
			foreach (var a in an)
			{
				double px = x + d * JS.cos(a), py = y + d * JS.sin(a), dx = p.x - px, dy = p.y - py; if (dx * dx + dy * dy > (aa + m + 2) * (aa + m + 2)) continue;
				if (shape == 0) { double u = dx * JS.cos(a) + dy * JS.sin(a), w = -dx * JS.sin(a) + dy * JS.cos(a); if (u * u / ((aa + m) * (aa + m)) + w * w / ((bb + m) * (bb + m)) < 1) return true; }
				else if (L16bInPoly(p.x, p.y, Tri(px, py, a + sp * (t - ta)), m)) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the mirror cracks ----------
	// hit at (x, y) at ta: n jagged cracks (from angle a0, evenly) run out v px/s to past the edges, th thick, each with a branch; they hold
	// till te, then fall (harmless). The hit point hurts for 0.6 beat. The cracks to come show (thin, grey) from tw
	public static Hz L16bCrack(double x, double y, int n, double a0, double v, double th, uint seed, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bCrack" }; uint s = seed;
		var lines = new List<(List<(double x, double y)> P, List<double> cum, double s0)>();
		for (int i = 0; i < n; i++)
		{
			double an = a0 + i * TAU / n; var P = new List<(double x, double y)> { (x + 24 * JS.cos(an), y + 24 * JS.sin(an)) }; double r = 24, cx2 = 0, cy2 = 0;
			var cum = new List<double> { 0 }; int bi = 2 + (int)(L16bRnd(ref s) * 3);
			while (r < 1600)
			{
				r += 55 + 25 * L16bRnd(ref s); double off = (L16bRnd(ref s) - 0.5) * 36;
				var q = (x: x + r * JS.cos(an) - off * JS.sin(an), y: y + r * JS.sin(an) + off * JS.cos(an));
				cum.Add(cum[^1] + hypot(q.x - P[^1].x, q.y - P[^1].y)); P.Add(q);
				if (P.Count == bi + 1) { cx2 = q.x; cy2 = q.y; }
			}
			lines.Add((P, cum, 0));
			// the branch
			double ba = an + (L16bRnd(ref s) < 0.5 ? -1 : 1) * (0.45 + 0.2 * L16bRnd(ref s)); var Q = new List<(double x, double y)> { (cx2, cy2) }; var cq = new List<double> { 0 };
			for (int j = 0; j < 4; j++) { ba += (L16bRnd(ref s) - 0.5) * 0.5; var q = (x: Q[^1].x + 55 * JS.cos(ba), y: Q[^1].y + 55 * JS.sin(ba)); cq.Add(cq[^1] + 55); Q.Add(q); }
			lines.Add((Q, cq, cum[bi]));
		}
		double Len(double t) => v * (t - ta);
		void Draw(double L, string col, double w)
		{
			g.strokeStyle = col; g.lineWidth = w; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath();
			foreach (var (P, cum, s0) in lines)
			{
				double ll = L - s0; if (ll <= 0) continue; g.moveTo(P[0].x, P[0].y);
				for (int i = 1; i < P.Count; i++)
				{
					if (cum[i] <= ll) { g.lineTo(P[i].x, P[i].y); continue; }
					double k = (ll - cum[i - 1]) / (cum[i] - cum[i - 1]); g.lineTo(P[i - 1].x + (P[i].x - P[i - 1].x) * k, P[i - 1].y + (P[i].y - P[i - 1].y) * k); break;
				}
			}
			g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter";
		}
		h.update = (t, dt) => { if (t >= te + 0.5 * SPB) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); Draw(1e9, L16bG(0.1 + 0.35 * k), 1.5);
				g.strokeStyle = L16bG(0.2 + 0.5 * k); g.lineWidth = 2; g.beginPath(); g.arc(x, y, 30 - 12 * k, 0, TAU); g.stroke(); return;
			}
			if (t >= te) { g.save(); g.globalAlpha *= 0.4 * (1 - L16bK(t, te, te + 0.35 * SPB)); Draw(1e9, "rgba(90,70,90,1)", th * 0.5); g.restore(); return; }
			g.save(); L16bGlow(12); Draw(Len(t), PINK, th); if (t < ta + 0.6 * SPB) { g.fillStyle = PINK; circle(x, y, 32); } g.restore();
			Draw(Len(t), L16bLt, Math.Max(1.5, th * 0.2));
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double m = L16bM(p), lim = th / 2 + m, L = Len(t);
			if (t < ta + 0.6 * SPB && hypot(p.x - x, p.y - y) < 32 + m) return true;
			foreach (var (P, cum, s0) in lines)
			{
				double ll = L - s0; if (ll <= 0) continue;
				for (int i = 1; i < P.Count; i++)
				{
					if (cum[i - 1] >= ll) break;
					double ex = P[i].x, ey = P[i].y;
					if (cum[i] > ll) { double k = (ll - cum[i - 1]) / (cum[i] - cum[i - 1]); ex = P[i - 1].x + (P[i].x - P[i - 1].x) * k; ey = P[i - 1].y + (P[i].y - P[i - 1].y) * k; }
					if (L16bSeg(p.x, p.y, P[i - 1].x, P[i - 1].y, ex, ey) < lim) return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- a falling piece of the mirror ----------
	// a jagged piece S big, turned rot, lands on (x, y) at ta: its shadow grows from tw (and the piece comes down from above). It stays hold
	// beats, then breaks into ns splinters that fly out vs px/s
	public static Hz[] L16bDrop(double x, double y, double S, double rot, uint seed, int ns, double vs, double tw, double ta, double hold)
	{
		var h = new Hz { kind = "L16bDrop" }; uint s = seed; double ts = ta + hold * SPB;
		var shp = new List<(double x, double y)>(); int nv = 6 + (int)(L16bRnd(ref s) * 2);
		for (int i = 0; i < nv; i++) { double an = rot + i * TAU / nv + (L16bRnd(ref s) - 0.5) * 0.5, r = S * (i == 0 ? 1.25 : i % 2 == 0 ? 0.8 + 0.2 * L16bRnd(ref s) : 0.5 + 0.2 * L16bRnd(ref s)); shp.Add((r * JS.cos(an), r * JS.sin(an))); }
		List<(double x, double y)> At(double cx, double cy, double sc) => shp.Select(q => (cx + q.x * sc, cy + q.y * sc)).ToList();
		var sa = Enumerable.Range(0, ns).Select(i => rot + 0.4 + i * TAU / ns).ToArray();
		var spl = L16bPetal(x, y, sa, vs, 20, 11, 1, 5, ts, ts, S * 0.4);
		h.update = (t, dt) => { if (t > ts + 1700 / vs) h.dead = true; };
		var warn = L16bWarn("L16bDropWarn", t =>
		{
			if (t < tw || t >= ta) return;
			{
				double k = L16bK(t, tw, ta);
				g.fillStyle = "rgba(0,0,0," + str(0.2 + 0.3 * k) + ")"; g.beginPath(); L16bPath(At(x, y, 0.4 + 0.6 * k)); g.fill();
				g.strokeStyle = L16bG(0.2 + 0.5 * k); g.lineWidth = 2; g.beginPath(); L16bPath(At(x, y, 1)); g.stroke();
				double up = 380 * (1 - k) * (1 - k); g.fillStyle = "rgba(200,215,245," + str(0.12 + 0.25 * k) + ")"; g.strokeStyle = "rgba(235,240,255," + str(0.25 + 0.4 * k) + ")";
				g.beginPath(); L16bPath(At(x, y - up, 1 + 0.5 * (1 - k))); g.fill(); g.stroke();
			}
		}, ta);
		h.draw = t =>
		{
			if (t < ta) return;
			if (t < ts)
			{
				g.save(); L16bGlow(14); g.fillStyle = PINK; g.beginPath(); L16bPath(At(x, y, 1)); g.fill(); g.restore();
				g.strokeStyle = L16bLt; g.lineWidth = 2; g.beginPath(); var P = At(x, y, 1); for (int i = 0; i < P.Count; i += 2) { g.moveTo(x, y); g.lineTo((P[i].x + x * 2) / 3, (P[i].y + y * 2) / 3); } g.stroke();
				return;
			}
			spl.draw(t);
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			if (t < ts) return hypot(p.x - x, p.y - y) < S * 1.25 + 8 && L16bInPoly(p.x, p.y, At(x, y, 1), L16bM(p));
			return spl.hit(p, t);
		};
		return new[] { warn, h };
	}

	// ---------- a cut across the whole mirror ----------
	// a jagged line through (x, y) at angle an; from ta it opens from 12 to wm wide in op beats, holds hold beats and closes in 0.5 beat
	// (then it is gone). It shows (grey) from tw
	public static Hz L16bCut(double x, double y, double an, double wm, double op, double hold, uint seed, double tw, double ta)
	{
		var h = new Hz { kind = "L16bCut" }; uint s = seed; double c = JS.cos(an), sn = JS.sin(an), t1 = ta + op * SPB, t2 = t1 + hold * SPB, t3 = t2 + 0.5 * SPB;
		var P = new List<(double x, double y)>();
		for (double d = -1500; d <= 1500; d += 60) { double off = (L16bRnd(ref s) - 0.5) * 34; P.Add((x + d * c - off * sn, y + d * sn + off * c)); }
		double Wd(double t) => t < t1 ? 12 + (wm - 12) * L16bS(L16bK(t, ta, t1)) : t < t2 ? wm : wm * (1 - L16bS(L16bK(t, t2, t3)));
		h.update = (t, dt) => { if (t > t3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			g.lineJoin = "round"; g.lineCap = "round";
			if (t < ta) { double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.12 + 0.4 * k); g.lineWidth = 2 + 2 * k; g.beginPath(); L16bPath(P, false); g.stroke(); }
			else
			{
				double w = Wd(t);
				if (t < t1) { g.strokeStyle = L16bP(0.14); g.lineWidth = wm; g.beginPath(); L16bPath(P, false); g.stroke(); }
				g.save(); L16bGlow(14); g.strokeStyle = PINK; g.lineWidth = Math.Max(1, w); g.beginPath(); L16bPath(P, false); g.stroke(); g.restore();
				g.strokeStyle = L16bLt; g.lineWidth = Math.Max(1, w * 0.15); g.beginPath(); L16bPath(P, false); g.stroke();
			}
			g.lineJoin = "miter"; g.lineCap = "butt";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= t3) return false;
			double lim = Wd(t) / 2 + L16bM(p), w = Math.Abs(-(p.x - x) * sn + (p.y - y) * c); if (w > lim + 20 || Wd(t) < 3) return false;
			return L16bNearLine(p.x, p.y, P, lim);
		};
		return h;
	}

	// ---------- a wedding ring ----------
	// a ring R big, its band th thick, a stone on top, spinning like a coin (spin turns a beat). orbit (orad > 0): round (cx, cy) at orad (up
	// by dor on the pulse beats pl) from oa0 at the rate program osp (beat, rad a beat). Else thrown from (cx, cy) to (x1, y1) at v px/s; back > 0:
	// it waits back beats there and comes back. Its way shows from tw; it is gone at te (orbit), back home, or past the edge
	public static Hz L16bRing(double cx, double cy, double R, double th, double spin, double orad, double dor, double[] pl, double oa0, double[] osp,
		double x1, double y1, double v, double back, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L16bRing" }; double b0 = BT(ta), D = hypot(x1 - cx, y1 - cy), ux = D > 0 ? (x1 - cx) / D : 0, uy = D > 0 ? (y1 - cy) / D : 1;
		double tA = ta + D / Math.Max(1, v), tB = tA + back * SPB, tE = orad > 0 ? te : back > 0 ? tB + D / Math.Max(1, v) : ta + 1700 / Math.Max(1, v);
		(double x, double y) Pos(double t)
		{
			if (orad > 0) { double b = BT(t), r = orad + dor * L16bPulse(pl, b), a = oa0 + L16bInt(osp, 2, 1, b0, b); return (cx + r * JS.cos(a), cy + r * JS.sin(a)); }
			double d = back <= 0 || t < tA ? v * (t - ta) : t < tB ? D : D - v * (t - tB);
			return (cx + ux * d, cy + uy * d);
		}
		double Rx(double t) => Math.Max(th / 2, R * Math.Abs(JS.cos(TAU * spin * (t - ta) / SPB)));
		List<(double x, double y)> Ell(double x, double y, double rx)
		{
			var P = new List<(double x, double y)>(41); for (int i = 0; i <= 40; i++) { double a = i * TAU / 40; P.Add((x + rx * JS.cos(a), y + R * JS.sin(a))); }
			return P;
		}
		h.update = (t, dt) => { if (t >= tE) h.dead = true; };
		void Ring(double x, double y, double rx, string col, double w, bool stone)
		{
			g.strokeStyle = col; g.lineWidth = w; g.beginPath(); g.ellipse(x, y, rx, R, 0, 0, TAU); g.stroke();
			if (!stone) return;
			g.fillStyle = col; g.beginPath(); g.moveTo(x, y - R - 20); g.lineTo(x + 9, y - R - 9); g.lineTo(x, y - R + 2); g.lineTo(x - 9, y - R - 9); g.closePath(); g.fill();
		}
		h.draw = t =>
		{
			if (t < tw || t >= tE) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.12 + 0.35 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 10 }); g.beginPath();
				if (orad > 0) g.arc(cx, cy, orad, 0, TAU);
				else { double e = back > 0 ? D : 1600; g.moveTo(cx, cy); g.lineTo(cx + ux * e, cy + uy * e); }
				g.stroke(); g.setLineDash(new double[0]);
				var p0 = Pos(ta); Ring(p0.x, p0.y, R, L16bG(0.2 + 0.4 * k), 2, false);
				if (orad <= 0 && back > 0) { g.strokeStyle = L16bG(0.2 + 0.4 * k); g.beginPath(); g.arc(cx + ux * D, cy + uy * D, R, 0, TAU); g.stroke(); }
				return;
			}
			var p = Pos(t); double rx = Rx(t);
			g.save(); L16bGlow(16); Ring(p.x, p.y, rx, PINK, th, true); g.restore();
			g.strokeStyle = L16bLt; g.lineWidth = Math.Max(1.5, th * 0.25); g.beginPath(); g.ellipse(p.x, p.y, Math.Max(0.5, rx - th * 0.2), R - th * 0.2, 0, Math.PI * 1.1, Math.PI * 1.7); g.stroke();
			g.fillStyle = L16bLt; g.beginPath(); g.moveTo(p.x, p.y - R - 15); g.lineTo(p.x + 4, p.y - R - 9); g.lineTo(p.x, p.y - R - 4); g.lineTo(p.x - 4, p.y - R - 9); g.closePath(); g.fill();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= tE) return false;
			var q = Pos(t); double m = L16bM(p), dx = p.x - q.x, dy = p.y - q.y;
			if (Math.Abs(dx) > R + th + m || dy > R + th + m || dy < -R - 22 - m) return false;
			if (hypot(dx, dy + R + 9) < 7 + m) return true;
			return L16bNearLine(p.x, p.y, Ell(q.x, q.y, Rx(t)), th / 2 + m);
		};
		return h;
	}

	// ---------- her veil's lace ----------
	// a band bw wide sweeping the room (dir 0: standing up, moving across from x = p0 at v px/s; dir 1: lying down, moving down/up from
	// y = p0), with round holes hr big at the places hs along it: you pass through a hole. The holes' lanes show from tw
	public static Hz L16bLace(int dir, double p0, double v, double bw, double[] hs, double hr, double tw, double ta)
	{
		var h = new Hz { kind = "L16bLace" }; double hw = bw / 2, Lmax = dir == 0 ? H : W, Cmax = dir == 0 ? W : H;
		double C(double t) => p0 + v * (t - ta);
		// al, ac: along the band and across it
		(double x, double y) XY(double al, double ac) => dir == 0 ? (ac, al) : (al, ac);
		h.update = (t, dt) => { double c = C(t); if (t > ta && (c < -bw - 40 || c > Cmax + bw + 40)) h.dead = true; };
		void Band(double c)
		{
			g.save(); g.beginPath(); var a = XY(-10, c - hw - 6); var b2 = XY(Lmax + 10, c + hw + 6); g.rect(Math.Min(a.x, b2.x), Math.Min(a.y, b2.y), Math.Abs(b2.x - a.x), Math.Abs(b2.y - a.y)); g.clip();
			g.beginPath(); var r0 = XY(-10, c - hw - 3); var r1 = XY(Lmax + 10, c + hw + 3); g.rect(Math.Min(r0.x, r1.x), Math.Min(r0.y, r1.y), Math.Abs(r1.x - r0.x), Math.Abs(r1.y - r0.y));
			foreach (var hc in hs) { var q = XY(hc, c); g.moveTo(q.x + hr, q.y); g.arc(q.x, q.y, hr, 0, TAU); }
			L16bGlow(12); g.fillStyle = PINK; g.fill("evenodd");
			// the scallops along both edges (not over the holes)
			g.beginPath();
			for (double al = 0; al <= Lmax; al += 12)
			{
				bool skip = false; foreach (var hc in hs) if (Math.Abs(al - hc) < hr + 8) skip = true; if (skip) continue;
				foreach (int sd in new[] { -1, 1 }) { var q = XY(al, c + sd * (hw - 2)); g.moveTo(q.x + 7, q.y); g.arc(q.x, q.y, 7, 0, TAU); }
			}
			g.fillParts();   // v53: each scallop on its own (merging 200+ circles every frame was slow)
			g.restore();
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.strokeStyle = L16bG(0.12 + 0.35 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath();
				foreach (var hc in hs) { var a = XY(hc, -50); var b2 = XY(hc, Cmax + 50); g.moveTo(a.x, a.y); g.lineTo(b2.x, b2.y); }
				g.stroke(); g.setLineDash(new double[0]);
				double ce = clamp(p0, 40, Cmax - 40); g.strokeStyle = L16bAu(0.25 + 0.5 * k);
				foreach (var hc in hs) { var q = XY(hc, ce); g.beginPath(); g.arc(q.x, q.y, hr - 6, 0, TAU); g.stroke(); }
				return;
			}
			double c = C(t); Band(c);
			// the lace's flowers on the band, and gold round the holes
			for (double al = 22; al < Lmax; al += 44)
			{
				bool skip = false; foreach (var hc in hs) if (Math.Abs(al - hc) < hr + 16) skip = true; if (skip) continue;
				var q = XY(al, c); g.fillStyle = "rgba(200,20,84,1)"; for (int i = 0; i < 5; i++) { double an = i * TAU / 5 + 0.3; circle(q.x + 7 * JS.cos(an), q.y + 7 * JS.sin(an), 3.2); }
				g.fillStyle = L16bLt; circle(q.x, q.y, 2.4);
			}
			g.save(); g.beginPath(); var c0 = XY(-10, c - hw); var c1 = XY(Lmax + 10, c + hw); g.rect(Math.Min(c0.x, c1.x), Math.Min(c0.y, c1.y), Math.Abs(c1.x - c0.x), Math.Abs(c1.y - c0.y)); g.clip();
			g.strokeStyle = L16bAu(0.85); g.lineWidth = 2; foreach (var hc in hs) { var q = XY(hc, c); g.beginPath(); g.arc(q.x, q.y, hr - 7, 0, TAU); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			double m = L16bM(p), c = C(t), al = dir == 0 ? p.y : p.x, ac = (dir == 0 ? p.x : p.y) - c;
			if (Math.Abs(ac) > hw + 3 + m) return false;
			foreach (var hc in hs) if (hypot(al - hc, ac) < hr - m) return false;
			return true;
		};
		return h;
	}

	// ---------- the coins thrown over the bride ----------
	// from (x, y) at the angles an with the speeds vs (px/s), falling gr px/s/s, r big. Their arcs show (dotted) from tw
	public static Hz L16bCoin(double x, double y, double[] an, double[] vs, double gr, double r, double tw, double ta)
	{
		var h = new Hz { kind = "L16bCoin" }; int n = an.Length;
		(double x, double y) P(int i, double tau) => (x + vs[i] * JS.cos(an[i]) * tau, y + vs[i] * JS.sin(an[i]) * tau + 0.5 * gr * tau * tau);
		h.update = (t, dt) =>
		{
			if (t < ta) return; double tau = t - ta; bool all = true;
			for (int i = 0; i < n; i++) { var q = P(i, tau); if (q.y < H + 40 && q.x > -40 && q.x < W + 40) { all = false; break; } }
			if (all || tau > 6) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L16bK(t, tw, ta); g.fillStyle = L16bG(0.15 + 0.45 * k);
				for (int i = 0; i < n; i++) for (double tau = 0.06; tau < 3; tau += 0.06) { var q = P(i, tau); if (q.y > H + 20 || q.x < -20 || q.x > W + 20) break; g.fillRect(q.x - 1.5, q.y - 1.5, 3, 3); }
				return;
			}
			double tt = t - ta;
			g.save(); L16bGlow(12); g.fillStyle = PINK; for (int i = 0; i < n; i++) { var q = P(i, tt); circle(q.x, q.y, r); } g.restore();
			g.strokeStyle = L16bLt; g.lineWidth = 1.5; g.beginPath(); for (int i = 0; i < n; i++) { var q = P(i, tt); g.moveTo(q.x + r * 0.6, q.y); g.arc(q.x, q.y, r * 0.6, 0, TAU); } g.stroke();
			g.fillStyle = L16bLt; for (int i = 0; i < n; i++) { var q = P(i, tt); circle(q.x, q.y, r * 0.22); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double tt = t - ta, lim = r + L16bM(p);
			for (int i = 0; i < n; i++) { var q = P(i, tt); if (Math.Abs(q.x - p.x) < lim && Math.Abs(q.y - p.y) < lim && hypot(q.x - p.x, q.y - p.y) < lim) return true; }
			return false;
		};
		return h;
	}

	// ---------- her bouquet ----------
	// thrown from (x0, y0) at tl, it lands on (x1, y1) at ta (harmless in the air) and opens into an n-petal rose Rb big, turning spin rad a
	// beat, for hold beats. Where it lands shows from tl
	public static Hz[] L16bBouquet(double x0, double y0, double x1, double y1, double Rb, int n, double spin, double hold, double tl, double ta)
	{
		var h = new Hz { kind = "L16bBouquet" }; double te = ta + hold * SPB, a0 = Math.Atan2(y1 - y0, x1 - x0);
		const double TH = 20;
		double R(double t) => Rb * L16bS(L16bK(t, ta, ta + 0.3 * SPB));
		double A(double t) => a0 + spin * (t - ta) / SPB;
		h.update = (t, dt) => { if (t > te + 0.4 * SPB) h.dead = true; };
		var air = L16bWarn("L16bBouquetAir", t =>
		{
			if (t < tl || t >= ta && t < te) return;
			g.lineJoin = "round"; g.lineCap = "round";
			if (t >= te)
			{
				// the spent rose fades here, under every attack
				g.save(); g.globalAlpha *= 1 - L16bK(t, te, te + 0.4 * SPB); g.strokeStyle = L16bG(1); g.lineWidth = TH; g.beginPath(); L16bRosePath(x1, y1, n, R(t), A(t)); g.stroke(); g.restore();
			}
			else
			{
				double k = L16bK(t, tl, ta), px = x0 + (x1 - x0) * k, py = y0 + (y1 - y0) * k - 160 * 4 * k * (1 - k);
				g.strokeStyle = L16bG(0.15 + 0.4 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); L16bRosePath(x1, y1, n, Rb, a0); g.stroke(); g.setLineDash(new double[0]);
				g.beginPath(); g.arc(x1, y1, 16, 0, TAU); g.stroke();
				// the bouquet in the air: white roses, green leaves
				g.save(); g.translate(px, py); g.rotate(k * 6);
				g.fillStyle = "rgba(46,92,60,1)"; for (int i = 0; i < 5; i++) { double an = i * TAU / 5; g.beginPath(); g.ellipse(14 * JS.cos(an), 14 * JS.sin(an), 8, 4, an, 0, TAU); g.fill(); }
				g.fillStyle = "rgba(252,248,242,1)"; circle(0, 0, 8); for (int i = 0; i < 5; i++) { double an = i * TAU / 5 + 0.6; circle(9 * JS.cos(an), 9 * JS.sin(an), 6); }
				g.strokeStyle = "rgba(200,188,186,0.9)"; g.lineWidth = 1; g.beginPath(); g.arc(0, 0, 4, 0, 4.7); g.stroke();
				g.restore();
			}
			g.lineJoin = "miter"; g.lineCap = "butt";
		}, te + 0.4 * SPB);
		h.draw = t =>
		{
			if (t < ta || t >= te) return;
			g.lineJoin = "round"; g.lineCap = "round";
			{
				g.save(); L16bGlow(14); g.strokeStyle = PINK; g.lineWidth = TH; g.beginPath(); L16bRosePath(x1, y1, n, R(t), A(t)); g.stroke();
				g.fillStyle = PINK; circle(x1, y1, 18);
				g.restore();
				g.strokeStyle = L16bLt; g.lineWidth = 3; g.beginPath(); L16bRosePath(x1, y1, n, R(t), A(t)); g.stroke(); g.fillStyle = L16bLt; circle(x1, y1, 6);
			}
			g.lineJoin = "miter"; g.lineCap = "butt";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			double m = L16bM(p); if (hypot(p.x - x1, p.y - y1) < 18 + m) return true;
			return L16bRoseHit(p.x, p.y, x1, y1, n, R(t), A(t), TH / 2 + m);
		};
		return new[] { air, h };
	}

	// ---------- the kinds (the events in data/dodge_L16.json) ----------
	// times are beats. Fixed attacks fire before their warning starts (the data's b), aimed ones ("rnd") lock on the square when they fire.
	static double L16bez(double v, double ez) => easy ? v * ez : v;
	static (double x, double y) L16bme() => (player?.x ?? W / 2, player?.y ?? H / 2);
	static double L16bWn(DodgeEv e, double d = 1) => e.num("wn", d) * (easy ? 1.3 : 1);
	static double[] L16bDeg(double[] a, int stride, int col, double ez = 1) => a.Select((v, i) => i % stride == col ? L16bez(v, ez) * Math.PI / 180 : v).ToArray();
	static readonly int _k16bfx = DodgeKind("L16bfx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L16bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k * 0.4); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k16bstage = DodgeKind("L16bstage", e => () => hazards.Add(L16bStage(e.nums("pb"), B(e.num("to")), e.nums("bp"), e.nums("mc"))));
	static readonly int _k16brose = DodgeKind("L16brose", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		// Easy: thinner, a smaller swell, slower turning
		hazards.Add(L16bRose(e.num("x", W / 2), e.num("y", H / 2), e.num("orad"), e.num("oa0") * Math.PI / 180, L16bDeg(e.nums("ost"), 2, 1), e.i("n", 5), e.num("R", 300),
			e.nums("rst"), e.nums("pl"), L16bez(e.num("dp"), 0.5), e.num("grow"), e.num("a0") * Math.PI / 180, L16bDeg(e.nums("sp"), 2, 1, 0.7), L16bez(e.num("th", 22), 0.75),
			e.num("tr"), e.nums("drop"), B(hit - wn), B(hit), B(e.num("to")), L16bez(e.num("od", 1), 1.3)));
	}; });
	static readonly int _k16bvine = DodgeKind("L16bvine", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"), x = e.num("x"), y = e.num("y"), tw = B(hit - wn);
		if (e.flag("rnd"))
		{
			// aimed: it grows along your row (a 0 / 180) or your column (a 90 / -90) from the edge
			var p = L16bme(); tw = B(e.b); hit = e.b + L16bez(e.num("wn", 1), 1.3); double a = e.num("a");
			if (Math.Abs(Math.Cos(a * Math.PI / 180)) > 0.5) y = p.y; else x = p.x;
		}
		hazards.Add(L16bVine(x, y, e.num("a") * Math.PI / 180, e.num("L", 1400), L16bez(e.num("A", 30), 0.7), e.num("lam", 220), e.num("ph"),
			L16bez(e.num("v", 1100), 0.8), L16bez(e.num("th", 18), 0.8), tw, B(hit), B(e.flag("rnd") ? hit + e.num("dur", 2) : e.num("to"))));
	}; });
	static readonly int _k16bpetal = DodgeKind("L16bpetal", e => { double wn = L16bWn(e, 0.75); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"); var an = e.nums("an").Select(v => v * Math.PI / 180).ToArray();
		if (easy && an.Length > 6) an = an.Where((v, i) => i % 3 != 2).ToArray();
		hazards.Add(L16bPetal(e.num("x", W / 2), e.num("y", H / 2), an, L16bez(e.num("v", 400), 0.75), e.num("aa", 30), e.num("bb", 16), e.i("shape"), e.num("sp", 4), B(hit - wn), B(hit), e.num("d0")));
	}; });
	static readonly int _k16bcrack = DodgeKind("L16bcrack", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"), x = e.num("x", W / 2), y = e.num("y", H / 2), a0 = e.num("a0") * Math.PI / 180, tw = B(hit - wn); int n = e.i("n", 6);
		if (e.flag("rnd"))
		{
			// aimed: it hits where you are (or your place in the mirror, "mir", with a crack pointing at you)
			var p = L16bme(); tw = B(e.b); hit = e.b + L16bez(e.num("wn", 1), 1.3);
			if (e.flag("mir")) { x = W - p.x; y = p.y; a0 += Math.Atan2(p.y - y, p.x - x); } else { x = p.x; y = p.y; }
		}
		if (easy) n = Math.Max(3, n * 2 / 3);
		hazards.Add(L16bCrack(x, y, n, a0, e.num("v", 2600), L16bez(e.num("th", 16), 0.75), (uint)e.i("seed", 1), tw, B(hit), B(hit + e.num("hold", 2))));
	}; });
	static readonly int _k16bdrop = DodgeKind("L16bdrop", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"), x = e.num("x"), y = e.num("y"), tw = B(hit - wn);
		if (e.flag("rnd")) { var p = L16bme(); x = p.x; y = p.y; tw = B(e.b); hit = e.b + L16bez(e.num("wn", 1), 1.3); }   // aimed: it falls where you are
		hazards.AddRange(L16bDrop(x, y, L16bez(e.num("S", 80), 0.85), e.num("rot") * Math.PI / 180, (uint)e.i("seed", 1), easy ? Math.Max(2, e.i("ns", 4) - 2) : e.i("ns", 4),
			L16bez(e.num("vs", 420), 0.75), tw, B(hit), e.num("hold", 1)));
	}; });
	static readonly int _k16bcut = DodgeKind("L16bcut", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"), x = e.num("x", W / 2), y = e.num("y", H / 2), tw = B(hit - wn);
		if (e.flag("rnd")) { var p = L16bme(); x = p.x; y = p.y; tw = B(e.b); hit = e.b + L16bez(e.num("wn", 1), 1.3); }
		hazards.Add(L16bCut(x, y, e.num("a") * Math.PI / 180, L16bez(e.num("w", 80), 0.7), e.num("op", 0.5), e.num("hold", 1), (uint)e.i("seed", 1), tw, B(hit)));
	}; });
	static readonly int _k16bring = DodgeKind("L16bring", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"), x1 = e.num("x1"), y1 = e.num("y1"), tw = B(hit - wn), x = e.num("x", W / 2), y = e.num("y", 150);
		if (e.flag("rnd"))
		{
			// aimed: thrown at where you are, D px out (or past the edge)
			var p = L16bme(); tw = B(e.b); hit = e.b + L16bez(e.num("wn", 1), 1.3); double dx = p.x - x, dy = p.y - y, l = Math.Max(1, hypot(dx, dy)), d = e.num("D", 1600);
			x1 = x + dx / l * d; y1 = y + dy / l * d;
		}
		hazards.Add(L16bRing(x, y, e.num("R", 44), L16bez(e.num("th", 12), 0.8), e.num("spin", 0.5), e.num("orad"), L16bez(e.num("dor"), 0.6), e.nums("pl"), e.num("oa0") * Math.PI / 180,
			L16bDeg(e.nums("osp"), 2, 1, 0.75), x1, y1, L16bez(e.num("v", 600), 0.75), e.num("back"), tw, B(hit), B(e.num("to", hit + 4))));
	}; });
	static readonly int _k16blace = DodgeKind("L16blace", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit");
		hazards.Add(L16bLace(e.i("dir"), e.num("p0"), L16bez(e.num("v", 300), 0.75), e.num("bw", 70), e.nums("hs"), L16bez(e.num("hr", 58), 1.15), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16bcoin = DodgeKind("L16bcoin", e => { double wn = L16bWn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		double hit = e.num("hit"); var an = e.nums("an").Select(v => v * Math.PI / 180).ToArray(); var vs = e.nums("vs");
		if (vs.Length < an.Length) vs = an.Select(_ => e.num("v", 600)).ToArray();
		if (easy && an.Length > 5) { var keep = an.Select((v, i) => i % 3 != 1).ToArray(); an = an.Where((v, i) => keep[i]).ToArray(); vs = vs.Where((v, i) => keep[i]).ToArray(); }
		hazards.Add(L16bCoin(e.num("x", W / 2), e.num("y", 300), an, vs, e.num("gr", 900), L16bez(e.num("r", 13), 0.85), B(hit - wn), B(hit)));
	}; });
	static readonly int _k16bbq = DodgeKind("L16bbq", e => () =>
	{
		if (easy && e.flag("ezskip")) return;
		var p = L16bme(); double tl = B(e.b), ta = tl + L16bez(e.num("wn", 1.25), 1.3) * SPB;
		hazards.AddRange(L16bBouquet(e.num("x", W / 2), e.num("y", 300), p.x, p.y, L16bez(e.num("Rb", 170), 0.8), e.i("n", 5), L16bez(e.num("spin", 0.6), 0.6), e.num("hold", 1.2), tl, ta));
	});
}
