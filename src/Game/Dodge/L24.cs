using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24: "the eye and the share" (القسمة). An eye watches over a night of Arabic star patterns and hands out
// your share: part 1 the look (the lines that split the screen open, the shares of the circle, the lashes it throws, the kohl that
// traces its own lids), the build-up (rings of beads leave the eye while the star is drawn), part 2 the division, the hardest part
// (the eight-pointed star that turns and breathes, the darts through its notches, the spiral the eye throws), part 3 the fate
// (the threads of fate with one knot to slip through, the scale and its weights, the eye closing on you).
// The level is the song from 2:34.000 (songs/SONG_L24.mp3 starts there). Grid: 120 BPM, beat 0 = 0.1975 s of the file (2:34.1975 of the song).
// Every hazard is a pure function of time once it exists. Only pink hurts: the gold, the white warnings and the eye never do.
public static partial class G
{
	static double QSk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string QSP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string QSW(double a) => $"rgba(236,232,246,{str(clamp(a, 0, 1))})";
	static string QSG(double a) => $"rgba(232,186,92,{str(clamp(a, 0, 1))})";
	static double QSE(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double QSOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double QSRad(double d) => d * Math.PI / 180;
	static double QSH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
	static void QSDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double QSSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// the integral (degrees) of a speed (degrees a beat) that holds w[i] from kb[i] to kb[i+1] (and w[last] after), from kb[0] to bt
	static double QSTurn(double[] kb, double[] w, double bt)
	{
		double a = 0;
		for (int i = 0; i < kb.Length; i++)
		{
			double b0 = kb[i], b1 = i + 1 < kb.Length ? kb[i + 1] : 1e9; if (bt <= b0) break;
			a += (Math.Min(bt, b1) - b0) * w[i];
		}
		return a;
	}
	// a dart (the eye's thrown lash): a thin sharp needle, length l, pointing along a
	static void QSDart(double x, double y, double l, double a, double wd = 4)
	{
		double c = JS.cos(a), s = JS.sin(a);
		g.beginPath(); g.moveTo(x + c * l * 0.5, y + s * l * 0.5); g.lineTo(x - c * l * 0.5 - s * wd, y - s * l * 0.5 + c * wd);
		g.lineTo(x - c * l * 0.3, y - s * l * 0.3); g.lineTo(x - c * l * 0.5 + s * wd, y - s * l * 0.5 - c * wd); g.closePath(); g.fill();
	}
	static bool QSDartHit(Player p, double x, double y, double l, double a)
	{
		double c = JS.cos(a), s = JS.sin(a);
		return QSSeg(p.x, p.y, x - c * l * 0.45, y - s * l * 0.45, x + c * l * 0.5, y + s * l * 0.5) < 3 + p.r - 2;
	}

	// ---------- the eye (harmless: ivory, a dark iris with a gold ring, kohl, gold lashes) ----------
	// open 0-1 (0 = shut), look: where the iris turns (a point on the screen), spin: the iris pattern turns, glow: a red glow (the hard part)
	public static void QSEyeDraw(double x, double y, double R, double open, double lx, double ly, double spin, double glow, double pupil, double a)
	{
		if (a <= 0.01 || R <= 1) return;
		g.save(); g.globalAlpha = a; g.translate(x, y);
		double hu = Math.Max(0.02, open) * R * 0.62, hl = Math.Max(0.02, open) * R * 0.5;
		void lids() { g.beginPath(); g.moveTo(-R, 0); g.quadraticCurveTo(0, -2 * hu, R, 0); g.quadraticCurveTo(0, 2 * hl, -R, 0); g.closePath(); }
		// glow: the hard part, the white of the eye goes dark (never pink: only pink hurts) with a gold rim
		lids(); var sg = g.createRadialGradient(0, 0, R * 0.1, 0, 0, R);
		sg.addColorStop(0, $"rgba({str(236 - 176 * glow)},{str(228 - 180 * glow)},{str(212 - 150 * glow)},1)"); sg.addColorStop(1, $"rgba({str(150 - 116 * glow)},{str(140 - 112 * glow)},{str(128 - 88 * glow)},1)");
		g.fillStyle = sg; g.fill();
		if (glow > 0) { g.strokeStyle = QSG(0.7 * glow); g.lineWidth = Math.Max(2, R * 0.02); g.stroke(); }
		if (open > 0.05)
		{
			g.save(); lids(); g.clip();
			double dx = lx - x, dy = ly - y, d = hypot(dx, dy), m = Math.Min(R * 0.42, d * 0.3), ix = d > 1 ? dx / d * m : 0, iy = d > 1 ? dy / d * m * 0.5 : 0, ir = R * 0.36;
			var ig = g.createRadialGradient(ix, iy, ir * 0.2, ix, iy, ir); ig.addColorStop(0, "rgba(40,70,92,1)"); ig.addColorStop(0.7, "rgba(18,30,52,1)"); ig.addColorStop(1, "rgba(8,10,20,1)");
			g.fillStyle = ig; circle(ix, iy, ir);
			// the iris's star pattern (gold, turning)
			g.strokeStyle = QSG(0.55); g.lineWidth = Math.Max(1, R * 0.012); g.beginPath();
			for (int k = 0; k < 16; k++) { double q = spin + k * TAU / 16; g.moveTo(ix + JS.cos(q) * ir * 0.45, iy + JS.sin(q) * ir * 0.45); g.lineTo(ix + JS.cos(q + 0.25) * ir * 0.9, iy + JS.sin(q + 0.25) * ir * 0.9); }
			g.stroke();
			g.strokeStyle = QSG(0.8); g.lineWidth = Math.Max(1.5, R * 0.018); g.beginPath(); g.arc(ix, iy, ir * 0.95, 0, TAU); g.stroke();
			g.fillStyle = "rgba(4,4,10,1)"; circle(ix, iy, ir * (0.32 + 0.16 * pupil));
			g.fillStyle = "rgba(255,255,255,0.75)"; circle(ix - ir * 0.3, iy - ir * 0.32, ir * 0.12);
			g.restore();
		}
		// kohl and the gold lashes on the upper lid
		g.strokeStyle = "rgba(10,8,16,0.95)"; g.lineWidth = Math.Max(2, R * 0.04); g.lineJoin = "round"; g.lineCap = "round";
		g.beginPath(); g.moveTo(-R * 1.12, -R * 0.06); g.lineTo(-R, 0); g.quadraticCurveTo(0, -2 * hu, R, 0); g.lineTo(R * 1.18, -R * 0.12); g.stroke();
		g.lineWidth = Math.Max(1.5, R * 0.025); g.beginPath(); g.moveTo(-R, 0); g.quadraticCurveTo(0, 2 * hl, R, 0); g.stroke();
		g.strokeStyle = QSG(0.75); g.lineWidth = Math.Max(1.2, R * 0.014); g.beginPath();
		for (int k = 1; k < 12; k++)
		{
			double u = k / 12.0, px = -R + 2 * R * u, py = -2 * hu * 2 * u * (1 - u), nx = (u - 0.5) * 0.9, ny = -1, nl = hypot(nx, ny), ln = R * (0.1 + 0.08 * Math.Sin(u * Math.PI));
			g.moveTo(px, py); g.lineTo(px + nx / nl * ln, py + ny / nl * ln);
		}
		g.stroke();
		g.restore();
	}
	// where the eye is (and how it looks) at beat bt
	public struct QSEyeSt { public double x, y, R, open, glow, a; }
	public static QSEyeSt QSEyeAt(double bt)
	{
		var e = new QSEyeSt { x = W / 2, y = 112, R = 130, open = 1, a = 1 };
		if (bt < 2) e.open = QSE(bt / 2);
		else if (bt >= 102 && bt < 120)
		{
			// it comes down to the middle and grows (the kohl traces its lids), then goes back up
			double k = QSE(QSk(bt, 102, 104)) * (1 - QSE(QSk(bt, 118, 120)));
			e.y = 112 + (H / 2 - 112) * k; e.R = 130 + (470 - 130) * k; e.a = 1 - 0.55 * k;
		}
		else if (bt >= 138 && bt < 240)
		{
			// the build-up: it comes to the middle, opens wide and glows red; the hard part: it stays there, big and dim, behind the attacks
			double k = QSE(QSk(bt, 138, 142)), f = QSk(bt, 142, 160);
			e.y = 112 + (H / 2 - 112) * k; e.R = 130 + (300 - 130) * k + 60 * f; e.open = 1 + 0.25 * f; e.glow = 0.3 * k + 0.7 * f; e.a = 1 - 0.45 * QSk(bt, 158, 161);
			if (bt >= 236) { double q = QSE(QSk(bt, 236, 240)); e.y += (112 - e.y) * q; e.R += (130 - e.R) * q; e.glow *= 1 - q; e.a += (1 - e.a) * q; e.open += (1 - e.open) * q; }
		}
		else if (bt >= 318)
		{
			// the end: it comes down and fills the screen, and its lids are the pink lids that close on you (QSLids: the same almond)
			QSLidsAt(bt, out var cx, out var hh); double k = QSE(QSk(bt, 318, 321));
			e.x = W / 2 + (cx - W / 2) * k; e.y = 112 + (H / 2 - 112) * k; e.R = 130 + (QS_RX - 130) * k; e.a = 1 - 0.45 * k;
			e.open = 1 + (hh / (0.62 * QS_RX) - 1) * k;
			if (bt > 342) e.open *= 1 - QSE(QSk(bt, 342, 343.2));
		}
		return e;
	}

	// ---------- the stage (harmless, under everything) ----------
	public static Hz QSStage(double t1)
	{
		var h = new Hz { kind = "QSStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p2 = QSk(bt, 140, 160) * (1 - QSk(bt, 238, 242)), p3 = QSk(bt, 238, 244);
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(16 + 22 * p2 + 10 * p3)},{str(12 + 2 * p2 + 8 * p3)},{str(40 - 10 * p2 - 12 * p3)},1)");
			gr.addColorStop(1, $"rgba({str(5 + 8 * p2)},{str(4)},{str(14 - 4 * p2)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the star lattice (faint gold eight-pointed stars, slowly turning round the middle)
			double ang = QSRad(1.5 * bt), ca = JS.cos(ang), sa = JS.sin(ang);
			g.strokeStyle = QSG(0.06 + 0.04 * p3); g.lineWidth = 1.5; g.beginPath();
			for (int ix = -5; ix <= 5; ix++)
				for (int iy = -3; iy <= 3; iy++)
				{
					double ux = ix * 150 + (iy % 2 == 0 ? 0 : 75), uy = iy * 130, cx = W / 2 + ux * ca - uy * sa, cy = H / 2 + ux * sa + uy * ca, r = 42;
					if (cx < -60 || cx > W + 60 || cy < -60 || cy > H + 60) continue;
					for (int s = 0; s < 2; s++) { double q0 = ang + s * Math.PI / 4; for (int k = 0; k <= 4; k++) { double q = q0 + k * Math.PI / 2; double px = cx + JS.cos(q) * r, py = cy + JS.sin(q) * r; if (k == 0) g.moveTo(px, py); else g.lineTo(px, py); } }
				}
			g.stroke();
			// the dunes far away (dark, harmless)
			g.fillStyle = "rgba(10,8,22,0.9)"; g.beginPath(); g.moveTo(0, H);
			for (int k = 0; k <= 16; k++) { double x = W * k / 16.0, y = H - 46 - 22 * JS.sin(k * 0.9 + 0.4) - 14 * JS.sin(k * 2.3); g.lineTo(x, y); }
			g.lineTo(W, H); g.closePath(); g.fill();
			var vg = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
			var e = QSEyeAt(bt);
			double kick = 0; foreach (var kb in QS_KICK) { if (kb > bt) break; kick = Math.Exp(-(bt - kb) * 5); }
			var pl = player; double lx = pl != null ? pl.x : W / 2, ly = pl != null ? pl.y : H / 2;
			QSEyeDraw(e.x, e.y, e.R, e.open, lx, ly, QSRad(20 * bt + (bt > 160 && bt < 240 ? 40 * (bt - 160) : 0)), e.glow, kick, e.a * a);
		};
		return h;
	}
	public static Hz QSFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "QSFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- 1. the split (2:34) ----------
	// A line across the screen through (cx, cy) at ang degrees shows dashed from bw (with the band's width faint at its sides); at b it
	// splits open into a pink band, half width wm, for hold beats, then closes in 0.25 beat and leaves a gold scar (harmless).
	public static Hz QSSplit(double cx, double cy, double ang, double bw, double b, double hold, double wm)
	{
		var h = new Hz { kind = "QSSplit" }; double a = QSRad(ang), c = JS.cos(a), s = JS.sin(a);
		h.update = (t, dt) => { if (BT(t) > b + hold + 2.5) h.dead = true; };
		double w(double bt) => bt < b ? 0 : bt < b + 0.12 ? wm * QSOut((bt - b) / 0.12) : bt < b + hold ? wm : wm * (1 - QSk(bt, b + hold, b + hold + 0.25));
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save(); g.translate(cx, cy); g.rotate(a);
			if (bt < b)
			{
				double q = QSk(bt, bw, b);
				QSDash(-1600, 0, 1600, 0, QSW(0.3 + 0.55 * q), 2);
				g.strokeStyle = QSW(0.08 + 0.12 * q); g.lineWidth = 1; g.beginPath(); g.moveTo(-1600, -wm); g.lineTo(1600, -wm); g.moveTo(-1600, wm); g.lineTo(1600, wm); g.stroke();
			}
			else
			{
				double ww = w(bt);
				if (ww > 0.5)
				{
					g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = QSP(0.6); g.fillRect(-1600, -ww, 3200, 2 * ww);
					g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath(); g.moveTo(-1600, -ww); g.lineTo(1600, -ww); g.moveTo(-1600, ww); g.lineTo(1600, ww); g.stroke();
				}
				double sc = 1 - QSk(bt, b + hold, b + hold + 2.2);
				if (bt > b + hold && sc > 0) { g.strokeStyle = QSG(0.5 * sc); g.lineWidth = 2; g.beginPath(); g.moveTo(-1600, 0); g.lineTo(1600, 0); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) => { double ww = w(BT(t)); if (ww <= 2) return false; return Math.Abs(-(p.x - cx) * s + (p.y - cy) * c) < ww + p.r - 2; };
		return h;
	}

	// ---------- 2. the shares (2:54) ----------
	// The screen is a circle cut in 8 shares round (cx, cy). The shares in mask light pink at b for dur beats (their edges show dashed
	// from bw); the whole circle turns spin degrees a beat from a0 (a0 at beat b).
	public static Hz QSShares(double cx, double cy, double a0, double spin, int mask, double bw, double b, double dur)
	{
		var h = new Hz { kind = "QSShares" };
		h.update = (t, dt) => { if (BT(t) > b + dur + 0.3) h.dead = true; };
		double rot(double bt) => a0 + spin * (bt - b);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + dur + 0.3) return; double r0 = rot(bt);
			g.save();
			for (int k = 0; k < 8; k++)
			{
				if ((mask >> k & 1) == 0) continue; double q0 = QSRad(r0 + k * 45), q1 = QSRad(r0 + (k + 1) * 45);
				if (bt < b)
				{
					double q = QSk(bt, bw, b);
					g.fillStyle = QSW(0.02 + 0.05 * q); g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 1500, q0, q1); g.closePath(); g.fill();
					QSDash(cx, cy, cx + JS.cos(q0) * 1500, cy + JS.sin(q0) * 1500, QSW(0.3 + 0.5 * q)); QSDash(cx, cy, cx + JS.cos(q1) * 1500, cy + JS.sin(q1) * 1500, QSW(0.3 + 0.5 * q));
				}
				else
				{
					double al = 1 - QSk(bt, b + dur, b + dur + 0.3);
					g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = QSP(0.55 * al); g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 1500, q0, q1); g.closePath(); g.fill();
					g.shadowBlur = 0; g.strokeStyle = QSP(al); g.lineWidth = 3; g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b || bt >= b + dur) return false;
			double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy); if (d < 1) return true;
			double ph = (Math.Atan2(dy, dx) * 180 / Math.PI - rot(bt)) % 360; if (ph < 0) ph += 360;
			int k = (int)Math.Floor(ph / 45) % 8; if ((mask >> k & 1) != 0) return true;
			// near the edge of a lit share (the square's own size)
			double e0 = ph - k * 45, e1 = (k + 1) * 45 - ph;
			if ((mask >> ((k + 7) % 8) & 1) != 0 && d * JS.sin(QSRad(Math.Min(e0, 90))) < p.r - 2) return true;
			if ((mask >> ((k + 1) % 8) & 1) != 0 && d * JS.sin(QSRad(Math.Min(e1, 90))) < p.r - 2) return true;
			return false;
		};
		return h;
	}

	// ---------- 3. the lashes (3:14) ----------
	// A fan of n darts leaves (x0, y0) at b, spread over spread degrees round ang, at sp px/s; their ways show dashed from bw.
	public static Hz QSFan(double x0, double y0, int n, double ang, double spread, double bw, double b, double sp, double len = 40)
	{
		var h = new Hz { kind = "QSFan" }; double tf = B(b);
		double aOf(int i) => QSRad(ang + (n == 1 ? 0 : -spread / 2 + spread * i / (n - 1)));
		double dist(double t) => 30 + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && dist(t) - len > 1600) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save();
			if (bt < b) { double q = QSk(bt, bw, b); for (int i = 0; i < n; i++) { double a = aOf(i); QSDash(x0 + JS.cos(a) * 30, y0 + JS.sin(a) * 30, x0 + JS.cos(a) * 1500, y0 + JS.sin(a) * 1500, QSW(0.18 + 0.45 * q)); } }
			else { double d = dist(t); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; for (int i = 0; i < n; i++) { double a = aOf(i); QSDart(x0 + JS.cos(a) * d, y0 + JS.sin(a) * d, len, a); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = dist(t);
			for (int i = 0; i < n; i++) { double a = aOf(i); if (QSDartHit(p, x0 + JS.cos(a) * d, y0 + JS.sin(a) * d, len, a)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 4. the kohl (3:25) ----------
	// The kohl traces the eye's lid: the upper lid (up = true, left to right) or the lower one (right to left), the curve from
	// (cx - rx, cy) through (cx, cy -/+ ry) to (cx + rx, cy), from b0 to b1. The stroke stays pink hot beats after it is drawn, then is
	// a gold line (harmless). Its way shows dashed from b0 - 1.
	public static Hz QSKohl(double cx, double cy, double rx, double ry, bool up, double b0, double b1, double hot, double wd)
	{
		var h = new Hz { kind = "QSKohl" }; const int N = 48;
		h.update = (t, dt) => { if (BT(t) > b1 + hot + 2) h.dead = true; };
		void pt(double u, out double x, out double y) { double s = up ? u : 1 - u; x = cx - rx + 2 * rx * s; y = cy + (up ? -1 : 1) * 4 * ry * s * (1 - s); }
		double head(double bt) => QSE((bt - b0) / (b1 - b0));
		// the part of the curve (u0..u1) that is hot at bt: drawn after bt - hot
		void hotPart(double bt, out double u0, out double u1) { u1 = bt < b0 ? 0 : head(bt); u0 = bt - hot < b0 ? 0 : head(bt - hot); }
		void walk(double u0, double u1, Action<double, double, double, double> f)
		{
			if (u1 <= u0) return; int n = Math.Max(1, (int)Math.Ceiling((u1 - u0) * N));
			pt(u0, out var px, out var py);
			for (int i = 1; i <= n; i++) { pt(u0 + (u1 - u0) * i / n, out var x, out var y); f(px, py, x, y); px = x; py = y; }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < b1) { double q = QSk(bt, b0 - 1, b0); g.strokeStyle = QSW(0.2 + 0.4 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); walk(head(bt), 1, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); g.setLineDash(new double[0]); }
			hotPart(bt, out var u0, out var u1);
			double gl = 1 - QSk(bt, b1 + hot, b1 + hot + 2);
			if (u0 > 0 && gl > 0) { g.strokeStyle = QSG(0.55 * gl); g.lineWidth = 3; g.beginPath(); walk(0, u0, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); }
			if (u1 > u0)
			{
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = wd; g.beginPath(); walk(u0, u1, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke();
				if (bt < b1) { pt(u1, out var hx, out var hy); g.fillStyle = PINK; circle(hx, hy, wd * 1.2); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); hotPart(bt, out var u0, out var u1); if (u1 <= u0) return false; bool hit = false;
			walk(u0, u1, (x0, y0, x1, y1) => { if (!hit && QSSeg(p.x, p.y, x0, y0, x1, y1) < wd / 2 + p.r - 2) hit = true; });
			if (!hit && bt < b1) { pt(u1, out var hx, out var hy); hit = hypot(p.x - hx, p.y - hy) < wd * 1.2 + p.r - 2; }
			return hit;
		};
		return h;
	}

	// ---------- 5. the rings of beads (the build-up, 3:44) ----------
	// A ring of n pink beads leaves the middle at b and grows to 900 px by b + dur; it turns spin degrees a beat; gaps: the beads whose
	// angle (at a0, at b) is within gw/2 of a gap angle are missing. It shows as a dashed ring with its gaps from bw.
	public static Hz QSRing(double cx, double cy, int n, double a0, double spin, double[] gaps, double gw, double bw, double b, double dur, double br = 11)
	{
		var h = new Hz { kind = "QSRing" };
		h.update = (t, dt) => { if (BT(t) > b + dur) h.dead = true; };
		double rad(double bt) => 900 * (bt - b) / dur;
		bool on(int k) { double q = a0 + k * 360.0 / n; foreach (var gq in gaps) { double d = ((q - gq) % 360 + 540) % 360 - 180; if (Math.Abs(d) < gw / 2) return false; } return true; }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw) return;
			g.save();
			if (bt < b)
			{
				double q = QSk(bt, bw, b); g.strokeStyle = QSW(0.25 + 0.5 * q); g.lineWidth = 2;
				for (int k = 0; k < n; k++) { if (!on(k)) continue; double a = QSRad(a0 + k * 360.0 / n); g.beginPath(); g.arc(cx + JS.cos(a) * 30, cy + JS.sin(a) * 30, 4, 0, TAU); g.stroke(); }
				foreach (var gq in gaps) { double a = QSRad(gq); QSDash(cx, cy, cx + JS.cos(a) * 1400, cy + JS.sin(a) * 1400, QSG(0.25 + 0.4 * q)); }
			}
			else
			{
				double r = rad(bt), rot = QSRad(spin * (bt - b)); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				for (int k = 0; k < n; k++) { if (!on(k)) continue; double a = QSRad(a0 + k * 360.0 / n) + rot, x = cx + JS.cos(a) * r, y = cy + JS.sin(a) * r; if (x > -20 && x < W + 20 && y > -20 && y < H + 20) circle(x, y, br); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false; double r = rad(bt), rot = QSRad(spin * (bt - b));
			double d = hypot(p.x - cx, p.y - cy); if (Math.Abs(d - r) > br + p.r) return false;
			for (int k = 0; k < n; k++) { if (!on(k)) continue; double a = QSRad(a0 + k * 360.0 / n) + rot; if (hypot(p.x - cx - JS.cos(a) * r, p.y - cy - JS.sin(a) * r) < br + p.r - 2) return true; }
			return false;
		};
		return h;
	}
	// ---------- the gaze ----------
	// The eye's gaze: n beams (evenly round) from (ex, ey) out to the screen's edge, half width hw, turning from a0 to a1 (degrees)
	// between b0 and b1 at an even speed. Each beam has a blind spot from r1 to r2 px from the eye (ringed in gold): the only way
	// through it. It shows from bw: its first place dashed, the blind spot in gold, and a gold arc for the way it will turn.
	public static Hz QSGaze(double ex, double ey, int n, double a0, double a1, double bw, double b0, double b1, double hw, double r1, double r2)
	{
		var h = new Hz { kind = "QSGaze" };
		h.update = (t, dt) => { if (BT(t) > b1 + 0.3) h.dead = true; };
		double ang(double bt) => a0 + (a1 - a0) * clamp((bt - b0) / (b1 - b0), 0, 1);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b1 + 0.3) return;
			g.save(); g.lineCap = "butt";
			for (int k = 0; k < n; k++)
			{
				double q = QSRad(ang(bt) + k * 360.0 / n), c = JS.cos(q), sn = JS.sin(q);
				if (bt < b0)
				{
					double u = QSk(bt, bw, b0);
					QSDash(ex + c * 20, ey + sn * 20, ex + c * r1, ey + sn * r1, QSW(0.25 + 0.5 * u), 2); QSDash(ex + c * r2, ey + sn * r2, ex + c * 1600, ey + sn * 1600, QSW(0.25 + 0.5 * u), 2);
					g.strokeStyle = QSG(0.2 + 0.4 * u); g.lineWidth = 2; g.beginPath(); g.arc(ex, ey, (r1 + r2) / 2, q, QSRad(a1 + k * 360.0 / n), a1 < a0); g.stroke();
				}
				else
				{
					double al = 1 - QSk(bt, b1, b1 + 0.3);
					g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = QSP(0.85 * al); g.lineWidth = 2 * hw; g.beginPath();
					g.moveTo(ex + c * 10, ey + sn * 10); g.lineTo(ex + c * r1, ey + sn * r1); g.moveTo(ex + c * r2, ey + sn * r2); g.lineTo(ex + c * 1600, ey + sn * 1600); g.stroke(); g.shadowBlur = 0;
				}
				g.fillStyle = QSG(0.9); circle(ex + c * r1, ey + sn * r1, 5); circle(ex + c * r2, ey + sn * r2, 5);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			for (int k = 0; k < n; k++)
			{
				double q = QSRad(ang(bt) + k * 360.0 / n), c = JS.cos(q), sn = JS.sin(q), dx = p.x - ex, dy = p.y - ey, al = dx * c + dy * sn, pe = Math.Abs(-dx * sn + dy * c);
				if (al > 10 - p.r && al < 1600 && pe < hw + p.r - 2 && (al < r1 + p.r - 2 || al > r2 - p.r + 2)) return true;
			}
			return false;
		};
		return h;
	}
}
