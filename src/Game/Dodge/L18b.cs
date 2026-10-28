using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L18 "اعوف الدنيا" (Mohamed Alsalim), the second design (v52, Mohammed 10-28: a sad Iraqi song, so the level is SADNESS and MUSIC, and every time
// the song changes its sound the attacks and the look change too). A lonely night with the oud and a candle:
//  0:50 the tears fall in the dark room | 0:57 the notes of the sad song run along the staff | 1:19 the violin cries (its bow, its sound) |
//  1:37 the old record turns (its grooves, its arm, its notes) | 1:57 the rain on the window | 2:06 the radio's sound bars | 2:25 the tears on the
//  river under the moon | 2:40 the heart beats faster and faster | 3:02 the heart breaks | 3:20 the last song (the staff turned round, the tears)
//  and the candle goes out.
// The first design (v49-v51) stays in L18.cs + data/dodge_L18_v51.json for old replays. Every hazard is a pure function of time once it exists.
// Only pink hurts (decorations are dull and never hurt); every attack shows itself in grey first. The data: data/dodge_L18.json (tools/level18/gen_b.py).
public static partial class G
{
	static double L18bk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L18bG(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double L18bOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k) * (1 - k); }
	static double L18bSeg(double px, double py, double ax, double ay, double bx, double by)
	{
		double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy; double u = l2 > 0 ? clamp(((px - ax) * dx + (py - ay) * dy) / l2, 0, 1) : 0;
		return hypot(px - ax - dx * u, py - ay - dy * u);
	}
	static double L18bWrap(double a) => L18Mod(a + Math.PI, TAU) - Math.PI;
	// an attack's dull, harmless parts (the violin, the record, the staff): drawn before everything that hurts, so they never cover it.
	// The attack keeps it in o["deco"]; its kind adds it first
	static void L18bDeco(Hz h, Action<double> draw, Func<double> end)
	{
		var d = new Hz { kind = h.kind + "Deco", veil = true }; d.update = (t, dt) => { if (t > end() + 0.5) d.dead = true; }; d.draw = draw;
		h.o = new Dictionary<string, object> { ["deco"] = d };
	}
	static void L18bAdd(Hz h) { if (h.o != null && h.o.TryGetValue("deco", out var d)) hazards.Add((Hz)d); hazards.Add(h); }

	// ---------- the heart's shape: its edge's distance from its middle by angle, for a heart s px from the middle to each side ----------
	const int L18bHN = 360;
	static double[] _l18bHeart;
	static double[] L18bHeartTab()
	{
		if (_l18bHeart != null) return _l18bHeart;
		var r = new double[L18bHN]; var has = new bool[L18bHN];
		for (int i = 0; i < 6000; i++)
		{
			double u = i * TAU / 6000, su = JS.sin(u), x = 16 * su * su * su, y = -(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)) - 2;
			int k = (int)(L18Mod(JS.atan2(y, x), TAU) / TAU * L18bHN) % L18bHN; double d = hypot(x, y) / 16; if (!has[k] || d > r[k]) { r[k] = d; has[k] = true; }
		}
		for (int k = 0; k < L18bHN; k++) if (!has[k]) { int a = k, b = k; while (!has[(a + L18bHN) % L18bHN]) a--; while (!has[b % L18bHN]) b++; r[k] = (r[(a + L18bHN) % L18bHN] + r[b % L18bHN]) / 2; }
		return _l18bHeart = r;
	}
	// the heart's edge at angle a (radians, screen y down), for size 1
	static double L18bHR(double a) { var T = L18bHeartTab(); double f = L18Mod(a, TAU) / TAU * L18bHN; int i = (int)f % L18bHN; double q = f - Math.Floor(f); return T[i] + (T[(i + 1) % L18bHN] - T[i]) * q; }
	static void L18bHeartPath(double x, double y, double s, double rot)
	{
		g.beginPath();
		for (int i = 0; i <= 96; i++) { double a = i * TAU / 96, r = L18bHR(a) * s; double c = JS.cos(a + rot), sn = JS.sin(a + rot); if (i == 0) g.moveTo(x + c * r, y + sn * r); else g.lineTo(x + c * r, y + sn * r); }
		g.closePath();
	}
	// a music note: head at (x,y) (radius r), stem up (or down), a flag when fl
	static void L18bNote(double x, double y, double r, bool up, bool fl, string col)
	{
		g.save(); g.fillStyle = col; g.strokeStyle = col;
		g.beginPath(); g.ellipse(x, y, r * 1.2, r * 0.85, -0.4, 0, TAU); g.fill();
		double sx = up ? x + r * 1.05 : x - r * 1.05, sy = up ? y - r * 3.6 : y + r * 3.6;
		g.lineWidth = Math.Max(2, r * 0.28); g.beginPath(); g.moveTo(sx, y); g.lineTo(sx, sy); g.stroke();
		if (fl) { double d = up ? 1 : -1; g.beginPath(); g.moveTo(sx, sy); g.quadraticCurveTo(sx + r * 1.5, sy + d * r * 1.2, sx + r * 1.1, sy + d * r * 2.6); g.lineTo(sx + r * 0.9, sy + d * r * 2.4); g.quadraticCurveTo(sx + r * 1.0, sy + d * r * 1.4, sx, sy + d * r * 0.9); g.closePath(); g.fill(); }
		g.restore();
	}
	static bool L18bNoteHit(Player p, double x, double y, double r, bool up)
	{
		if (hypot((p.x - x) / 1.15, (p.y - y) / 0.85) < r + p.r - 1) return true;
		double sx = up ? x + r * 1.05 : x - r * 1.05, sy = up ? y - r * 3.6 : y + r * 3.6;
		return L18bSeg(p.x, p.y, sx, y, sx, sy) < r * 0.14 + p.r;
	}
	// a tear (round below, pointed above), r = its round part
	static void L18bTear(double x, double y, double r, string col)
	{
		g.fillStyle = col; g.beginPath(); g.moveTo(x, y - r * 2.4);
		g.bezierCurveTo(x + r * 0.35, y - r * 1.5, x + r, y - r * 0.8, x + r, y); g.arc(x, y, r, 0, Math.PI); g.bezierCurveTo(x - r, y - r * 0.8, x - r * 0.35, y - r * 1.5, x, y - r * 2.4); g.fill();
		g.fillStyle = "rgba(255,215,228,0.9)"; g.beginPath(); g.ellipse(x - r * 0.38, y - r * 0.15, r * 0.16, r * 0.34, 0.3, 0, TAU); g.fill();
	}
	static bool L18bTearHit(Player p, double x, double y, double r) => hypot(p.x - x, p.y - y) < r + p.r - 1 || hypot(p.x - x, p.y - (y - r * 1.25)) < r * 0.5 + p.r - 1;

	// ---------- the scenes (harmless, under everything) ----------
	// 1 the dark room (window with the moon, the candle, the oud) | 2 the room with the sheet music | 3 the stage under one light | 4 the old record
	// player's corner | 5 the window in the rain | 6 the radio's glow | 7 the river at night with the palms and the moon | 8 the heart's dark red |
	// 9 the broken night | 10 the last song (the candle burns down and goes out at out)
	public static Hz L18bScene(int sc, double t0, double t1, double tout)
	{
		var h = new Hz { kind = "L18bScene", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.6, (t1 - t) / 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a; double bt = BT(t);
			switch (sc)
			{
				case 1: case 2: case 10: L18bRoom(t, bt, sc, tout); break;
				case 3: L18bSky(t, bt, "rgba(8,6,16,0.9)", "rgba(120,110,160,0.16)"); L18bSpot(W / 2, -60, 720, "rgba(200,190,255,0.07)"); break;
				case 4: L18bSky(t, bt, "rgba(14,8,6,0.9)", "rgba(180,120,70,0.14)"); break;
				case 5: L18bWindow(t, bt); break;
				case 6: L18bSky(t, bt, "rgba(6,10,10,0.9)", "rgba(80,170,150,0.12)"); break;
				case 7: L18bRiver(t, bt); break;
				case 8: { double pu = JS.exp(-L18Mod(bt, 1) * 4); L18bSky(t, bt, "rgba(16,3,6,0.9)", $"rgba(150,20,40,{str(0.10 + 0.08 * pu)})"); break; }
				default: L18bSky(t, bt, "rgba(5,5,10,0.92)", "rgba(110,40,70,0.13)"); L18bStreaks(t); break;
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.65)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}
	static void L18bSky(double t, double bt, string bg, string glow)
	{
		g.fillStyle = bg; g.fillRect(0, 0, W, H);
		var gr = g.createRadialGradient(W / 2, H * 0.45, 30, W / 2, H * 0.45, 600); gr.addColorStop(0, glow); gr.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
	}
	static void L18bSpot(double x, double y, double len, string col)
	{
		g.fillStyle = col; g.beginPath(); g.moveTo(x - 40, y); g.lineTo(x + 40, y); g.lineTo(x + 330, y + len); g.lineTo(x - 330, y + len); g.closePath(); g.fill();
	}
	static void L18bStreaks(double t)
	{
		// slow grey rain far away
		g.strokeStyle = "rgba(150,160,190,0.12)"; g.lineWidth = 1.5; g.beginPath();
		for (int i = 0; i < 70; i++) { double x = L18Mod(i * 173 + t * 40, W + 100) - 50, y = L18Mod(i * 97 + t * 420 * (0.7 + (i % 5) * 0.1), H + 80) - 40; g.moveTo(x, y); g.lineTo(x - 6, y + 26); }
		g.stroke();
	}
	// the room: dark blue wall, the window with the moon (top left), the oud leaning by the wall (right), the candle on the floor (right)
	static void L18bRoom(double t, double bt, int sc, double tout)
	{
		g.fillStyle = "rgba(9,11,22,0.92)"; g.fillRect(0, 0, W, H);
		// the floor
		g.fillStyle = "rgba(20,14,12,0.9)"; g.fillRect(0, 610, W, 110);
		g.strokeStyle = "rgba(90,60,40,0.18)"; g.lineWidth = 1; g.beginPath(); for (int i = 0; i < 9; i++) { g.moveTo(i * 160, 610); g.lineTo(i * 160 - 60, H); } g.stroke();
		// the window, the moon and its light on the floor
		g.fillStyle = "rgba(40,55,90,0.35)"; g.fillRect(110, 90, 230, 300);
		g.fillStyle = "rgba(230,235,255,0.75)"; circle(250, 170, 34); g.fillStyle = "rgba(40,55,90,0.9)"; circle(264, 160, 30);
		g.strokeStyle = "rgba(70,55,45,0.9)"; g.lineWidth = 10; g.strokeRect(110, 90, 230, 300); g.lineWidth = 6; g.beginPath(); g.moveTo(225, 90); g.lineTo(225, 390); g.moveTo(110, 240); g.lineTo(340, 240); g.stroke();
		g.fillStyle = "rgba(120,140,200,0.06)"; g.beginPath(); g.moveTo(110, 390); g.lineTo(340, 390); g.lineTo(520, 720); g.lineTo(200, 720); g.closePath(); g.fill();
		// the oud: its round back, the rose on its face, the neck and the bent pegbox
		g.save(); g.translate(1010, 470); g.rotate(-0.38);
		var og = g.createRadialGradient(-30, -20, 10, 0, 0, 150); og.addColorStop(0, "rgba(120,72,40,0.75)"); og.addColorStop(1, "rgba(50,28,16,0.8)"); g.fillStyle = og;
		g.beginPath(); g.ellipse(0, 0, 120, 150, 0, 0, TAU); g.fill();
		g.strokeStyle = "rgba(170,120,70,0.35)"; g.lineWidth = 2; for (int i = -3; i <= 3; i++) { g.beginPath(); g.ellipse(0, 0, Math.Abs(i) * 36 + 4, 150, 0, -1.4, 1.4); g.stroke(); }
		g.fillStyle = "rgba(20,10,6,0.85)"; circle(0, -40, 26); g.strokeStyle = "rgba(200,150,90,0.4)"; g.lineWidth = 1.5; for (int i = 0; i < 8; i++) { double q = i * TAU / 8; g.beginPath(); g.moveTo(0, -40); g.lineTo(JS.cos(q) * 26, -40 + JS.sin(q) * 26); g.stroke(); }
		g.fillStyle = "rgba(60,34,18,0.85)"; g.fillRect(-18, -300, 36, 160); g.save(); g.translate(0, -300); g.rotate(-1.1); g.fillRect(-14, -80, 28, 84); g.restore();
		g.strokeStyle = "rgba(220,210,190,0.25)"; g.lineWidth = 1; for (int i = -2; i <= 3; i++) { g.beginPath(); g.moveTo(i * 5, -300); g.lineTo(i * 5, 90); g.stroke(); }
		g.fillStyle = "rgba(40,20,10,0.9)"; g.fillRect(-36, 80, 72, 12);
		g.restore();
		// the candle (from 3:39 it burns out)
		double burn = sc == 10 ? L18bk(t, tout - 1.2, tout) : 0, cx = 1180, cy = 600, ch = 90 - 50 * (sc == 10 ? L18bk(t, B(256), tout) : 0);
		g.fillStyle = "rgba(220,205,180,0.8)"; g.fillRect(cx - 14, cy - ch, 28, ch); g.fillStyle = "rgba(240,230,210,0.8)"; g.beginPath(); g.ellipse(cx, cy - ch, 14, 4, 0, 0, TAU); g.fill();
		g.fillStyle = "rgba(70,60,50,0.9)"; g.fillRect(cx - 24, cy, 48, 8);
		if (burn < 1)
		{
			double fl = 1 + 0.12 * JS.sin(t * 13) + 0.06 * JS.sin(t * 31), fa = 1 - burn;
			var lg = g.createRadialGradient(cx, cy - ch - 20, 4, cx, cy - ch - 20, 260 * fl); lg.addColorStop(0, $"rgba(255,190,110,{str(0.22 * fa)})"); lg.addColorStop(1, "rgba(255,190,110,0)"); g.fillStyle = lg; g.fillRect(cx - 300, cy - ch - 300, 600, 600);
			g.fillStyle = $"rgba(255,200,120,{str(0.9 * fa)})"; g.beginPath(); g.ellipse(cx, cy - ch - 14, 6 * fl, 15 * fl, 0, 0, TAU); g.fill();
			g.fillStyle = $"rgba(255,245,220,{str(0.9 * fa)})"; g.beginPath(); g.ellipse(cx, cy - ch - 10, 2.5, 7, 0, 0, TAU); g.fill();
		}
		else { g.strokeStyle = "rgba(160,160,170,0.35)"; g.lineWidth = 2; g.beginPath(); g.moveTo(cx, cy - ch); for (int i = 1; i < 8; i++) g.lineTo(cx + 6 * JS.sin(t * 3 + i), cy - ch - i * 12); g.stroke(); }
		if (sc == 2)
		{
			// the old sheet music behind the staff
			g.fillStyle = "rgba(200,185,150,0.05)"; g.fillRect(60, 60, W - 120, H - 120);
		}
	}
	// the window in the rain: wet glass, the frame in four panes, the curtains
	static void L18bWindow(double t, double bt)
	{
		g.fillStyle = "rgba(10,14,24,0.92)"; g.fillRect(0, 0, W, H);
		var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "rgba(40,55,80,0.25)"); gr.addColorStop(1, "rgba(10,14,24,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
		// the city far away, blurred lights
		for (int i = 0; i < 26; i++) { double x = (i * 211) % W, y = 470 + (i * 37) % 90; g.fillStyle = $"rgba(255,{str(170 + i % 4 * 15)},120,0.10)"; circle(x, y, 10 + i % 3 * 5); }
		// still drops on the glass
		g.fillStyle = "rgba(170,190,220,0.10)"; for (int i = 0; i < 80; i++) circle((i * 389) % W, (i * 241) % H, 2 + i % 3);
		g.strokeStyle = "rgba(60,44,34,0.95)"; g.lineWidth = 22; g.strokeRect(11, 11, W - 22, H - 22); g.lineWidth = 14; g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.moveTo(0, H / 2); g.lineTo(W, H / 2); g.stroke();
		g.fillStyle = "rgba(60,20,30,0.55)"; g.beginPath(); g.moveTo(0, 0); g.lineTo(90, 0); g.quadraticCurveTo(50, 360, 110, H); g.lineTo(0, H); g.closePath(); g.fill();
		g.beginPath(); g.moveTo(W, 0); g.lineTo(W - 90, 0); g.quadraticCurveTo(W - 50, 360, W - 110, H); g.lineTo(W, H); g.closePath(); g.fill();
	}
	// the river at night: the moon and its broken reflection, the palm trees on the far bank, the slow water
	static void L18bRiver(double t, double bt)
	{
		var sky = g.createLinearGradient(0, 0, 0, 300); sky.addColorStop(0, "rgba(6,8,20,0.95)"); sky.addColorStop(1, "rgba(20,26,50,0.95)"); g.fillStyle = sky; g.fillRect(0, 0, W, 300);
		g.fillStyle = "rgba(230,235,250,0.8)"; circle(880, 120, 40); var mg = g.createRadialGradient(880, 120, 40, 880, 120, 160); mg.addColorStop(0, "rgba(200,210,255,0.18)"); mg.addColorStop(1, "rgba(200,210,255,0)"); g.fillStyle = mg; g.fillRect(700, 0, 360, 300);
		var wat = g.createLinearGradient(0, 300, 0, H); wat.addColorStop(0, "rgba(16,22,44,0.95)"); wat.addColorStop(1, "rgba(4,6,14,0.95)"); g.fillStyle = wat; g.fillRect(0, 300, W, H - 300);
		// the far bank and its palms
		g.fillStyle = "rgba(4,5,10,0.95)"; g.fillRect(0, 286, W, 16);
		for (int i = 0; i < 7; i++)
		{
			double x = 70 + i * 190 + (i % 2) * 40, hh = 120 + (i * 37) % 60, sw = 4 * JS.sin(t * 0.8 + i);
			g.strokeStyle = "rgba(4,5,10,0.95)"; g.lineWidth = 7; g.beginPath(); g.moveTo(x, 292); g.quadraticCurveTo(x + 10, 292 - hh / 2, x + sw, 292 - hh); g.stroke();
			g.lineWidth = 4; for (int l = 0; l < 7; l++) { double q = -Math.PI / 2 + (l - 3) * 0.5; g.beginPath(); g.moveTo(x + sw, 292 - hh); g.quadraticCurveTo(x + sw + JS.cos(q) * 40, 292 - hh + JS.sin(q) * 30 - 10, x + sw + JS.cos(q) * 70, 292 - hh + JS.sin(q) * 40 + 25); g.stroke(); }
		}
		// the moon's reflection, broken by the water
		for (int i = 0; i < 14; i++) { double y = 315 + i * 28, w = 70 - i * 3 + 14 * JS.sin(t * 1.3 + i * 0.9); g.fillStyle = $"rgba(220,225,255,{str(0.16 - i * 0.008)})"; g.fillRect(880 - w / 2 + 10 * JS.sin(t * 0.9 + i), y, w, 5); }
		g.strokeStyle = "rgba(120,140,200,0.07)"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 12; i++) { double y = 330 + i * 33; g.moveTo(0, y); for (double x = 0; x <= W; x += 40) g.lineTo(x, y + 4 * JS.sin(x * 0.02 + t * 1.5 + i)); } g.stroke();
	}

	// ---------- 0:50 the tears (also at 3:20): tear k falls from the top at x[k] from t0[k] (s), taking ft s to fall past the bottom, faster and
	// faster. Its line shows in grey wn s before
	public static Hz L18bTears(double[] xs, double[] t0, double ft, double r, double wn, double te)
	{
		var h = new Hz { kind = "L18bTears" }; int n = xs.Length;
		double Y(int k, double t) { double q = (t - t0[k]) / ft; return -40 + (H + 100) * (0.35 * q + 0.65 * q * q); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.lineCap = "round";
			for (int k = 0; k < n; k++)
			{
				if (t < t0[k] - wn || t > t0[k] + ft) continue;
				if (t < t0[k] + 0.2) { double q = L18bk(t, t0[k] - wn, t0[k]); g.strokeStyle = L18bG(0.15 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 9 }); g.beginPath(); g.moveTo(xs[k], 0); g.lineTo(xs[k], H); g.stroke(); g.setLineDash(new double[0]); }
				if (t >= t0[k]) L18bTear(xs[k], Y(k, t), r, PINK);
			}
			g.restore();
		};
		h.hit = (p, t) => { for (int k = 0; k < n; k++) { if (t < t0[k] || t > t0[k] + ft) continue; if (L18bTearHit(p, xs[k], Y(k, t), r)) return true; } return false; };
		return h;
	}

	// ---------- 0:57 the notes on the staff: five staff lines (grey, harmless) with 11 places (lines and spaces, sp px apart round the middle). Note k
	// comes in at the right at t0[k] on place pl[k] and runs left at v px/s; fl = a flag. A grey line shows its way wn s before
	public static Hz L18bStaff(double sp, double[] pl, double[] t0, double[] fl, double v, double r, double wn, double tw, double te)
	{
		var h = new Hz { kind = "L18bStaff" }; int n = pl.Length;
		double Y(double q) => H / 2 + (5 - q) * sp;
		double X(int k, double t) => W + 60 - v * (t - t0[k]);
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L18bk(t, tw, tw + 0.6), 1 - L18bk(t, te, te + 0.4)); if (a <= 0) return;
			g.save(); g.globalAlpha = a;
			for (int k = 0; k < n; k++)
			{
				double x = X(k, t); if (x < -80 || t < t0[k] - wn) continue; double y = Y(pl[k]);
				if (t < t0[k] + 0.3) { double q = L18bk(t, t0[k] - wn, t0[k]); g.strokeStyle = L18bG(0.2 + 0.55 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(Math.Max(0, x - 900), y); g.lineTo(W, y); g.stroke(); g.setLineDash(new double[0]); }
				if (t >= t0[k]) L18bNote(x, y, r, pl[k] < 5, fl[k] > 0, PINK);
			}
			g.restore();
		};
		L18bDeco(h, t =>
		{
			if (t < tw) return; double a = Math.Min(L18bk(t, tw, tw + 0.6), 1 - L18bk(t, te, te + 0.4)); if (a <= 0) return;
			g.save(); g.globalAlpha = a;
			g.strokeStyle = "rgba(200,190,170,0.28)"; g.lineWidth = 2; for (int l = 0; l < 5; l++) { double y = Y(1 + 2 * l); g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
			// the clef, big and dull
			g.strokeStyle = "rgba(200,190,170,0.22)"; g.lineWidth = 7; g.beginPath();
			double cx = 70, cy = Y(3);
			g.moveTo(cx + 10, Y(-1) + 40); g.bezierCurveTo(cx - 30, Y(-1) + 70, cx - 10, Y(0), cx, Y(2)); g.bezierCurveTo(cx + 12, Y(6), cx + 40, Y(10), cx + 22, Y(11) - 10);
			g.bezierCurveTo(cx, Y(11) + 12, cx - 22, Y(9), cx - 4, Y(6)); g.bezierCurveTo(cx + 20, Y(3), cx + 60, Y(3), cx + 50, cy + sp); g.bezierCurveTo(cx + 40, Y(1) + 20, cx - 20, Y(1) + 10, cx - 30, cy); g.stroke();
			g.restore();
		}, () => te);
		h.hit = (p, t) => { if (t < tw || t >= te) return false; for (int k = 0; k < n; k++) { if (t < t0[k]) continue; double x = X(k, t); if (x < -80 || Math.Abs(x - p.x) > r * 5) continue; if (L18bNoteHit(p, x, Y(pl[k]), r, pl[k] < 5)) return true; } return false; };
		return h;
	}

	// ---------- 1:19 the violin cries: the violin stands at (px,py) moving to its next place at mb[k] (s) (mx,my), its bow (pink) turns round it to
	// angle ba[k] (radians) at sb[k] s (half a stroke's time), shown in grey wn s before. On rb[k] s the violin sends a sound wave (a pink ring with a gap
	// at ga[k]) out at rv px/s
	public class L18bVioO { public double[] mb, mx, my, sb, ba, rb, ga; public double bl, bw, sdur, rv, rth, gw, wn, tw, te; }
	public static Hz L18bViolin(L18bVioO o)
	{
		var h = new Hz { kind = "L18bViolin" };
		(double x, double y) P(double t)
		{
			double x = o.mx[0], y = o.my[0];
			for (int k = 1; k < o.mb.Length; k++) { if (t < o.mb[k] - 0.5) break; double q = L18bOut(L18bk(t, o.mb[k] - 0.5, o.mb[k])); x = o.mx[k - 1] + (o.mx[k] - o.mx[k - 1]) * q; y = o.my[k - 1] + (o.my[k] - o.my[k - 1]) * q; }
			return (x, y);
		}
		double A(double t)
		{
			double a = o.ba[0];
			for (int k = 1; k < o.sb.Length; k++) { if (t < o.sb[k]) break; a = o.ba[k - 1] + (o.ba[k] - o.ba[k - 1]) * L18bOut(L18bk(t, o.sb[k], o.sb[k] + o.sdur)); }
			return a;
		}
		(double, double, double, double) bow(double t, double a) { var (x, y) = P(t); double c = JS.cos(a), s = JS.sin(a); return (x - c * 140, y - s * 140, x + c * o.bl, y + s * o.bl); }
		h.update = (t, dt) => { if (t > o.te + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.4)); var (px, py) = P(t);
			g.save(); g.lineCap = "round";
			// the waves
			for (int k = 0; k < o.rb.Length; k++)
			{
				double d = t - o.rb[k]; if (d < -o.wn || d * o.rv > 1600) continue; var (cx, cy) = P(o.rb[k]);
				if (d < 0) { double q = 1 + d / o.wn; g.strokeStyle = L18bG(0.2 + 0.6 * q); g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, 60 + 30 * (1 - q), o.ga[k] + o.gw / 2, o.ga[k] - o.gw / 2 + TAU); g.stroke(); continue; }
				double r = 40 + d * o.rv; g.strokeStyle = PINK; g.lineWidth = o.rth; g.lineCap = "butt"; g.beginPath(); g.arc(cx, cy, r, o.ga[k] + o.gw / 2, o.ga[k] - o.gw / 2 + TAU); g.stroke(); g.lineCap = "round";
			}
			if (al <= 0) { g.restore(); return; }
			g.globalAlpha = al;
			// the next stroke, grey
			for (int k = 1; k < o.sb.Length; k++) { double s = o.sb[k] - t; if (s <= 0 || s > o.wn) continue; var (ax, ay, bx, by) = bow(o.sb[k], o.ba[k]); g.strokeStyle = L18bG(0.2 + 0.5 * (1 - s / o.wn)); g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 }); g.beginPath(); g.moveTo(ax, ay); g.lineTo(bx, by); g.stroke(); g.setLineDash(new double[0]); }
			// the bow: the stick and its hair (pink, it hurts), the frog at the near end
			var (x0, y0, x1, y1) = bow(t, A(t));
			g.strokeStyle = PINK; g.lineWidth = o.bw; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
			g.strokeStyle = "rgba(255,210,225,0.95)"; g.lineWidth = 2; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
			g.fillStyle = PINK; circle(x0, y0, o.bw * 0.9);
			g.restore();
		};
		L18bDeco(h, t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.4)); if (al <= 0) return; var (px, py) = P(t);
			g.save(); g.globalAlpha = al;
			// the violin: its body (two bouts and the waist), f-holes, the neck and scroll, the strings (all dull, harmless)
			double va = A(t) + Math.PI / 2 + 0.25;
			g.save(); g.translate(px, py); g.rotate(va);
			var vg = g.createLinearGradient(-70, 0, 70, 0); vg.addColorStop(0, "rgba(90,40,20,0.85)"); vg.addColorStop(0.5, "rgba(150,75,35,0.85)"); vg.addColorStop(1, "rgba(80,34,16,0.85)"); g.fillStyle = vg;
			g.beginPath(); g.moveTo(0, -120); g.bezierCurveTo(60, -120, 70, -70, 50, -40); g.bezierCurveTo(38, -24, 40, -4, 56, 20); g.bezierCurveTo(84, 60, 70, 130, 0, 130); g.bezierCurveTo(-70, 130, -84, 60, -56, 20); g.bezierCurveTo(-40, -4, -38, -24, -50, -40); g.bezierCurveTo(-70, -70, -60, -120, 0, -120); g.fill();
			g.strokeStyle = "rgba(20,8,4,0.7)"; g.lineWidth = 3; g.beginPath(); g.moveTo(-22, -20); g.bezierCurveTo(-30, 10, -14, 30, -24, 56); g.moveTo(22, -20); g.bezierCurveTo(30, 10, 14, 30, 24, 56); g.stroke();
			g.fillStyle = "rgba(25,12,8,0.9)"; g.fillRect(-10, -260, 20, 170); g.beginPath(); g.arc(0, -272, 14, 0, TAU); g.fill();
			g.strokeStyle = "rgba(230,220,200,0.35)"; g.lineWidth = 1; for (int i = -1; i <= 2; i++) { g.beginPath(); g.moveTo(i * 4 - 2, -262); g.lineTo(i * 6 - 3, 110); g.stroke(); }
			g.fillStyle = "rgba(210,190,150,0.6)"; g.fillRect(-22, 30, 44, 5);
			g.restore();
			g.restore();
		}, () => o.te);
		h.hit = (p, t) =>
		{
			if (t >= o.tw + 0.5 && t < o.te) { var (x0, y0, x1, y1) = bow(t, A(t)); if (L18bSeg(p.x, p.y, x0, y0, x1, y1) < o.bw / 2 + p.r - 1) return true; }
			for (int k = 0; k < o.rb.Length; k++)
			{
				double d = t - o.rb[k]; if (d < 0 || d * o.rv > 1600) continue; var (cx, cy) = P(o.rb[k]); double r = 40 + d * o.rv, dd = hypot(p.x - cx, p.y - cy);
				if (Math.Abs(dd - r) >= o.rth / 2 + p.r - 1) continue;
				if (Math.Abs(L18bWrap(JS.atan2(p.y - cy, p.x - cx) - o.ga[k])) < o.gw / 2 - (p.r + 1) / Math.Max(r, 1)) continue;
				return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 1:37 the old record: it turns (spin) round (cx,cy), radius RR; its grooves burn pink: arc k on radius ar[k], from angle aa[k] (in the
	// record's own turn) aw[k] wide, from on[k] to off[k] (s), grey wn s before. Its arm, from the pivot (qx,qy) arm px long, turns to angle qa[k] at qb[k]
	// (grey first). At nb[k] a note jumps off the rim at the screen angle na[k] and flies out at nv px/s
	public class L18bRecO { public double cx, cy, RR, th, nv, nr, armL, armW, qx, qy, wn, tw, te; public L15Spin sp; public double[] ar, aa, aw, on, off, qb, qa, nb, na, mb, mx; }
	public static Hz L18bRecord(L18bRecO o)
	{
		var h = new Hz { kind = "L18bRecord" };
		double rot(double t) => o.sp.At(BT(t));
		// the record slides to mx[k] at mb[k] (0.6 s), its arm with it
		double X(double t) { if (o.mb == null || o.mb.Length == 0) return o.cx; double x = o.mx[0]; for (int k = 1; k < o.mb.Length; k++) { if (t < o.mb[k]) break; x = o.mx[k - 1] + (o.mx[k] - o.mx[k - 1]) * L18bOut(L18bk(t, o.mb[k], o.mb[k] + 0.6)); } return x; }
		double arm(double t) { double a = o.qa[0]; for (int k = 1; k < o.qb.Length; k++) { if (t < o.qb[k]) break; a = o.qa[k - 1] + (o.qa[k] - o.qa[k - 1]) * L18bOut(L18bk(t, o.qb[k], o.qb[k] + 0.22)); } return a; }
		bool nlive(int k, double d) => d >= 0 && d * o.nv <= 900;
		(double, double) note(int k, double t) { double a = o.na[k], r = o.RR + (t - o.nb[k]) * o.nv; double cx = X(o.nb[k]); return (cx + JS.cos(a) * r, o.cy + JS.sin(a) * r); }
		h.update = (t, dt) => { if (t > o.te + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.6), 1 - L18bk(t, o.te, o.te + 0.4)); double R = rot(t), cx = X(t), qx = o.qx + cx - o.cx;
			g.save();
			if (al > 0)
			{
				g.globalAlpha = al;
				// the grooves that burn
				g.lineCap = "butt";
				for (int k = 0; k < o.ar.Length; k++)
				{
					if (t < o.on[k] - o.wn || t >= o.off[k]) continue; double a0 = o.aa[k] + R;
					if (t < o.on[k]) { double q = L18bk(t, o.on[k] - o.wn, o.on[k]); g.strokeStyle = L18bG(0.2 + 0.6 * q); g.lineWidth = 3; }
					else { g.strokeStyle = PINK; g.lineWidth = o.th; }
					g.beginPath(); g.arc(cx, o.cy, o.ar[k], a0, a0 + o.aw[k]); g.stroke();
				}
				// where the record goes next, in grey
				if (o.mb != null) for (int k = 1; k < o.mb.Length; k++) { double s = o.mb[k] - t; if (s <= 0 || s > 2 * o.wn) continue; g.strokeStyle = L18bG(0.2 + 0.5 * (1 - s / (2 * o.wn))); g.lineWidth = 3; g.setLineDash(new double[] { 14, 10 }); g.beginPath(); g.arc(o.mx[k], o.cy, o.RR, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				// the arm: grey where it is going, pink where it is
				for (int k = 1; k < o.qb.Length; k++) { double s = o.qb[k] - t; if (s <= 0 || s > o.wn) continue; g.strokeStyle = L18bG(0.2 + 0.5 * (1 - s / o.wn)); g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 }); g.beginPath(); g.moveTo(qx, o.qy); g.lineTo(qx + JS.cos(o.qa[k]) * o.armL, o.qy + JS.sin(o.qa[k]) * o.armL); g.stroke(); g.setLineDash(new double[0]); }
				double am = arm(t), ex = qx + JS.cos(am) * o.armL, ey = o.qy + JS.sin(am) * o.armL;
				g.lineCap = "round"; g.strokeStyle = PINK; g.lineWidth = o.armW; g.beginPath(); g.moveTo(qx, o.qy); g.lineTo(ex, ey); g.stroke();
				g.fillStyle = PINK; g.save(); g.translate(ex, ey); g.rotate(am); g.fillRect(-10, -o.armW, 34, o.armW * 2); g.restore();
			}
			g.globalAlpha = 1;
			for (int k = 0; k < o.nb.Length; k++)
			{
				double d = t - o.nb[k]; if (d < -o.wn || (d >= 0 && !nlive(k, d))) continue;
				if (d < 0) { double a = o.na[k]; g.strokeStyle = L18bG(0.2 + 0.6 * (1 + d / o.wn)); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); double nx = X(o.nb[k]); g.moveTo(nx + JS.cos(a) * o.RR, o.cy + JS.sin(a) * o.RR); g.lineTo(nx + JS.cos(a) * 1500, o.cy + JS.sin(a) * 1500); g.stroke(); g.setLineDash(new double[0]); continue; }
				var (x, y) = note(k, t); L18bNote(x, y, o.nr, true, true, PINK);
			}
			g.restore();
		};
		L18bDeco(h, t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.6), 1 - L18bk(t, o.te, o.te + 0.4)); if (al <= 0) return; double R = rot(t), cx = X(t), qx = o.qx + cx - o.cx;
			g.save(); g.globalAlpha = al;
				// the player's box and the black record with its grooves and red label
				g.fillStyle = "rgba(60,36,22,0.9)"; g.fillRect(cx - o.RR - 40, o.cy - o.RR - 40, 2 * o.RR + 80, 2 * o.RR + 80);
				g.strokeStyle = "rgba(120,80,50,0.6)"; g.lineWidth = 4; g.strokeRect(cx - o.RR - 40, o.cy - o.RR - 40, 2 * o.RR + 80, 2 * o.RR + 80);
				g.fillStyle = "rgba(10,10,12,0.97)"; circle(cx, o.cy, o.RR);
				g.strokeStyle = "rgba(90,90,100,0.25)"; g.lineWidth = 1; for (double r = 70; r < o.RR - 4; r += 9) { g.beginPath(); g.arc(cx, o.cy, r, 0, TAU); g.stroke(); }
				g.strokeStyle = "rgba(200,200,220,0.10)"; g.lineWidth = 30; g.beginPath(); g.arc(cx, o.cy, o.RR * 0.62, R + 0.4, R + 0.9); g.stroke(); g.beginPath(); g.arc(cx, o.cy, o.RR * 0.62, R + 0.4 + Math.PI, R + 0.9 + Math.PI); g.stroke();
				g.fillStyle = "rgba(120,24,30,0.95)"; circle(cx, o.cy, 62); g.fillStyle = "rgba(210,180,120,0.5)"; g.beginPath(); g.arc(cx, o.cy, 46, R, R + 2.2); g.arc(cx, o.cy, 38, R + 2.2, R, true); g.closePath(); g.fill();
				g.fillStyle = "rgba(200,200,210,0.9)"; circle(cx, o.cy, 6);
			g.fillStyle = "rgba(160,160,170,0.8)"; circle(qx, o.qy, 34);
			g.restore();
		}, () => o.te);
		h.hit = (p, t) =>
		{
			double R = rot(t), cx = X(t), qx = o.qx + cx - o.cx;
			if (t >= o.tw + 0.6 && t < o.te)
			{
				double am = arm(t), ex = qx + JS.cos(am) * o.armL, ey = o.qy + JS.sin(am) * o.armL;
				if (L18bSeg(p.x, p.y, qx, o.qy, ex, ey) < o.armW / 2 + p.r - 1 || L18bSeg(p.x, p.y, ex, ey, ex + JS.cos(am) * 24, ey + JS.sin(am) * 24) < o.armW + p.r - 1) return true;
				double dd = hypot(p.x - cx, p.y - o.cy), an = JS.atan2(p.y - o.cy, p.x - cx);
				for (int k = 0; k < o.ar.Length; k++)
				{
					if (t < o.on[k] || t >= o.off[k] || Math.Abs(dd - o.ar[k]) >= o.th / 2 + p.r - 1) continue;
					double rel = L18Mod(an - o.aa[k] - R, TAU), m = (p.r - 1) / Math.Max(o.ar[k], 1);
					if (rel < o.aw[k] + m || rel > TAU - m) return true;
				}
			}
			for (int k = 0; k < o.nb.Length; k++) { double d = t - o.nb[k]; if (!nlive(k, d)) continue; var (x, y) = note(k, t); if (L18bNoteHit(p, x, y, o.nr, true)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 1:57 the rain on the window: drop k starts at the top at x0[k] at t0[k] (s) and runs down at vy px/s, swaying wa[k] px (wf turns a
	// second); its trail is pink too. Its way shows in grey wn s before
	public static Hz L18bRain(double[] x0, double[] t0, double[] wa, double vy, double wf, double r, double wn, double te)
	{
		var h = new Hz { kind = "L18bRain" }; int n = x0.Length;
		(double, double) at(int k, double t) { double d = t - t0[k]; return (x0[k] + wa[k] * JS.sin(d * wf * TAU + k), -30 + d * vy); }
		bool live(int k, double t) => t >= t0[k] && t - t0[k] < (H + 120) / vy;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			for (int k = 0; k < n; k++)
			{
				if (t < t0[k] - wn || t - t0[k] > (H + 120) / vy) continue;
				if (t < t0[k] + 0.4)
				{
					double q = L18bk(t, t0[k] - wn, t0[k]); g.strokeStyle = L18bG(0.15 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 5, 9 }); g.beginPath();
					for (double s = 0; s <= (H + 60) / vy; s += 0.05) { double x = x0[k] + wa[k] * JS.sin(s * wf * TAU + k), y = -30 + s * vy; if (s == 0) g.moveTo(x, y); else g.lineTo(x, y); }
					g.stroke(); g.setLineDash(new double[0]);
				}
				if (!live(k, t)) continue;
				g.strokeStyle = PINK; g.lineWidth = r * 0.9; g.beginPath(); var (hx, hy) = at(k, t); g.moveTo(hx, hy);
				for (int j = 1; j <= 4; j++) { var (x, y) = at(k, Math.Max(t0[k], t - j * 0.035)); g.lineTo(x, y); }
				g.stroke(); L18bTear(hx, hy + r * 0.3, r, PINK);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int k = 0; k < n; k++)
			{
				if (!live(k, t)) continue; var (hx, hy) = at(k, t); if (Math.Abs(p.y - hy) > 200) continue;
				if (L18bTearHit(p, hx, hy + r * 0.3, r)) return true;
				double px_ = hx, py_ = hy;
				for (int j = 1; j <= 4; j++) { var (x, y) = at(k, Math.Max(t0[k], t - j * 0.035)); if (L18bSeg(p.x, p.y, px_, py_, x, y) < r * 0.45 + p.r - 1) return true; px_ = x; py_ = y; }
			}
			return false;
		};
		return h;
	}

	// ---------- 2:06 the radio's sound bars: nc bars across the screen, each pink from the top down to its window and from its window down to the
	// bottom (made of lit blocks). Bar i's window is around path(i + shift) (pc[], px, linear in between), gap px tall; the shift moves one bar on at
	// each step beat (sb[] s); on the pulses (pb[] s) the windows close a little (pg px) and open again. The bars rise from ta; grey before
	public static Hz L18bEq(int nc, double[] pc, double[] sb, double[] pb, double gap, double pg, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L18bEq" }; double cw = W / nc;
		double shift(double t) { double s = 0; foreach (var b in sb) { if (t <= b) break; s += L18bOut((t - b) / 0.11); } return s; }
		double path(double u) { u = clamp(u, 0, pc.Length - 1.001); int i = (int)u; double q = u - i; return pc[i] + (pc[i + 1] - pc[i]) * q; }
		double close(double t) { double c = 0; foreach (var b in pb) { if (t < b) break; c = Math.Max(c, JS.exp(-(t - b) * 7)); } return pg * c; }
		double rise(double t) => L18bOut(L18bk(t, ta, ta + 0.5)) * (1 - L18bk(t, te - 0.3, te));
		(double top, double bot) win(int i, double t, double sh) { double c = path(i + sh), gg = gap / 2 - close(t); double k = rise(t); return (c - gg - (1 - k) * 800, c + gg + (1 - k) * 800); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > te) return; double sh = shift(t); bool on = t >= ta; double a = L18bk(t, tw, tw + 0.4);
			g.save(); g.globalAlpha = a;
			// the radio's dial below everything (dull)
			for (int i = 0; i < nc; i++)
			{
				double x = i * cw + 3, w = cw - 6; var (top, bot) = win(i, t, sh);
				if (!on)
				{
					var (t1, b1) = win(i, ta, shift(ta)); g.strokeStyle = L18bG(0.25 + 0.5 * L18bk(t, tw, ta)); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.strokeRect(x, Math.Max(-5, t1), w, Math.Min(H + 5, b1) - Math.Max(-5, t1)); g.setLineDash(new double[0]); continue;
				}
				if (top > 0) { g.fillStyle = PINK; g.fillRect(x - 3, -2, w + 6, top + 2); }
				if (bot < H) { g.fillStyle = PINK; g.fillRect(x - 3, bot, w + 6, H - bot + 2); }
				// the blocks' light lines (light pink on the pink)
				g.fillStyle = "rgba(255,190,212,0.55)";
				for (double y = top - 18; y > -18; y -= 22) g.fillRect(x + 4, y, w - 8, 3);
				for (double y = bot + 15; y < H; y += 22) g.fillRect(x + 4, y, w - 8, 3);
				g.fillStyle = "rgba(255,230,240,0.95)"; if (top > 0) g.fillRect(x, top - 5, w, 5); if (bot < H) g.fillRect(x, bot, w, 5);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double sh = shift(t);
			int c0 = (int)clamp(Math.Floor((p.x - p.r + 1) / cw), 0, nc - 1), c1 = (int)clamp(Math.Floor((p.x + p.r - 1) / cw), 0, nc - 1);
			for (int i = c0; i <= c1; i++) { var (top, bot) = win(i, t, sh); if (p.y - p.r + 1 < top || p.y + p.r - 1 > bot) return true; }
			return false;
		};
		return h;
	}

	// ---------- 2:25 the tears on the river: tear k falls on the water at (x[k], y[k]) at t0[k] (s) (a grey mark shows where, wn s before): a splash,
	// then two rings spread at rv px/s to rmax, each with a gap (gw radians) towards ga[k]
	public static Hz L18bRipple(double[] x, double[] y, double[] t0, double[] ga, double rv, double rmax, double th, double gw, double wn, double te)
	{
		var h = new Hz { kind = "L18bRipple" }; int n = x.Length; const double LAG = 0.28;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.lineCap = "butt";
			for (int k = 0; k < n; k++)
			{
				double d = t - t0[k]; if (d < -wn || d > LAG + rmax / rv) continue;
				if (d < 0)
				{
					double q = 1 + d / wn; g.strokeStyle = L18bG(0.25 + 0.6 * q); g.lineWidth = 2; g.beginPath(); g.ellipse(x[k], y[k], 70 * (1 - q) + 16, (70 * (1 - q) + 16) * 0.5, 0, 0, TAU); g.stroke();
					// the tear coming down to it
					double ty = y[k] - 260 * (-d / wn); L18bTear(x[k], ty, 9, L18bG(0.35 + 0.4 * q)); continue;
				}
				if (d < 0.22) { g.fillStyle = PINK; for (int j = 0; j < 6; j++) { double a = -Math.PI / 2 + (j - 2.5) * 0.4, l = 34 * L18bOut(d / 0.22); g.beginPath(); g.ellipse(x[k] + JS.cos(a) * l, y[k] + JS.sin(a) * l * 1.3, 5, 9, a + Math.PI / 2, 0, TAU); g.fill(); } circle(x[k], y[k], 22); }
				for (int j = 0; j < 2; j++)
				{
					double dj = d - j * LAG; if (dj < 0) continue; double r = 22 + dj * rv; if (r > rmax) continue;
					g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.arc(x[k], y[k], r, ga[k] + gw / 2, ga[k] - gw / 2 + TAU); g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			for (int k = 0; k < n; k++)
			{
				double d = t - t0[k]; if (d < 0 || d > LAG + rmax / rv) continue; double dd = hypot(p.x - x[k], p.y - y[k]);
				if (d < 0.22 && dd < 22 + p.r - 1) return true;
				for (int j = 0; j < 2; j++)
				{
					double dj = d - j * LAG; if (dj < 0) continue; double r = 22 + dj * rv; if (r > rmax || Math.Abs(dd - r) >= th / 2 + p.r - 1) continue;
					if (Math.Abs(L18bWrap(JS.atan2(p.y - y[k], p.x - x[k]) - ga[k])) < gw / 2 - (p.r + 1) / r) continue;
					return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- 2:40 the heart beats faster: the heart (size hs, pink) in the middle beats on hb[] (s); on wb[k] a heart-shaped wave goes out at wv px/s
	// with a gap gw wide towards wg[k]; its arteries (na lines out from it, turning with sp) are dull and burn pink from vb[k] for vd s (grey before)
	public class L18bBeatO { public double hs, wv, wth, gw, vw, vd, wn, tw, ta, te; public double[] hb, wb, wg, vb; public int na; public L15Spin sp; }
	public static Hz L18bBeat(L18bBeatO o)
	{
		var h = new Hz { kind = "L18bBeat" }; double cx = W / 2, cy = H / 2;
		double pulse(double t) { double c = 0; foreach (var b in o.hb) { if (t < b) break; c = JS.exp(-(t - b) * 7); } return c; }
		double size(double t) => o.hs * (1 + 0.16 * pulse(t));
		bool vlit(double t) { foreach (var b in o.vb) { if (t < b) break; if (t < b + o.vd) return true; } return false; }
		double vsoon(double t) { foreach (var b in o.vb) if (b > t) return b - t; return 99; }
		double vang(int i, double r, double t) => o.sp.At(BT(t)) + i * TAU / o.na + 0.35 * JS.sin(r / 70.0 + i);
		h.update = (t, dt) => { if (t > o.te + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.3));
			g.save(); g.lineCap = "round";
			if (al > 0)
			{
				g.globalAlpha = al; bool lit = t >= o.ta && vlit(t) && t < o.te; double sn = vsoon(t), s = size(t), vwn = o.wn * 0.6;
				for (int i = 0; i < o.na && (lit || sn < vwn); i++)
				{
					g.strokeStyle = lit ? PINK : L18bG(0.25 + 0.55 * (1 - sn / vwn)); g.lineWidth = lit ? o.vw : 3; g.beginPath();
					for (double r = s * 0.7; r < 900; r += 18) { double a = vang(i, r, t); if (r == s * 0.7) g.moveTo(cx + JS.cos(a) * r, cy + JS.sin(a) * r); else g.lineTo(cx + JS.cos(a) * r, cy + JS.sin(a) * r); }
					g.stroke();
				}
				// the heart, with its light and its shade
				L18bHeartPath(cx, cy, s, 0); g.fillStyle = t >= o.ta ? PINK : L18bG(0.5); g.fill();
				g.fillStyle = "rgba(255,200,215,0.7)"; g.beginPath(); g.ellipse(cx - s * 0.42, cy - s * 0.38, s * 0.16, s * 0.24, -0.6, 0, TAU); g.fill();
			}
			g.globalAlpha = 1; g.lineCap = "butt";
			for (int k = 0; k < o.wb.Length; k++)
			{
				double d = t - o.wb[k]; if (d < -o.wn || d * o.wv > 1100) continue;
				if (d < 0) { L18bHeartPath(cx, cy, o.hs * 1.25 + 30 * (-d / o.wn), 0); g.strokeStyle = L18bG(0.2 + 0.6 * (1 + d / o.wn)); g.lineWidth = 2; g.stroke(); continue; }
				double sc = o.hs * 1.1 + d * o.wv; g.strokeStyle = PINK; g.lineWidth = o.wth; g.beginPath(); bool pen = false;
				for (int i = 0; i <= 120; i++) { double a = i * TAU / 120; if (Math.Abs(L18bWrap(a - o.wg[k])) < o.gw / 2) { pen = false; continue; } double r = L18bHR(a) * sc; double xx = cx + JS.cos(a) * r, yy = cy + JS.sin(a) * r; if (!pen) { g.moveTo(xx, yy); pen = true; } else g.lineTo(xx, yy); }
				g.stroke();
			}
			g.restore();
		};
		L18bDeco(h, t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.3)); if (al <= 0) return; double s = size(t);
			g.save(); g.globalAlpha = al; g.lineCap = "round"; g.strokeStyle = "rgba(110,30,40,0.5)"; g.lineWidth = 5;
			for (int i = 0; i < o.na; i++) { g.beginPath(); for (double r = s * 0.7; r < 900; r += 18) { double a = vang(i, r, t); if (r == s * 0.7) g.moveTo(cx + JS.cos(a) * r, cy + JS.sin(a) * r); else g.lineTo(cx + JS.cos(a) * r, cy + JS.sin(a) * r); } g.stroke(); }
			g.restore();
		}, () => o.te);
		h.hit = (p, t) =>
		{
			double dx = p.x - cx, dy = p.y - cy, dd = hypot(dx, dy), an = JS.atan2(dy, dx), hr = L18bHR(an);
			if (t >= o.ta && t < o.te)
			{
				if (dd < hr * size(t) + p.r - 1) return true;
				if (vlit(t)) for (int i = 0; i < o.na; i++) { if (dd < size(t) * 0.7) continue; double a = vang(i, dd, t); if (Math.Abs(L18bWrap(an - a)) * dd < o.vw / 2 + p.r - 1) return true; }
			}
			for (int k = 0; k < o.wb.Length; k++)
			{
				double d = t - o.wb[k]; if (d < 0 || d * o.wv > 1100) continue; double sc = o.hs * 1.1 + d * o.wv;
				if (Math.Abs(dd - hr * sc) >= o.wth / 2 + p.r - 1) continue;
				if (Math.Abs(L18bWrap(an - o.wg[k])) < o.gw / 2 - (p.r + 1) / Math.Max(dd, 1)) continue;
				return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 3:02 the heart breaks: two halves (size hs) go round the middle d px out (turning with sp, d breathing with the beats), each turned on
	// its own; on sb[k] (s) shards fly out of the crack in a fan of ns (sa[k] the fan's middle, sw wide) at sv px/s. Grey lines show the fan first
	public class L18bBrokeO { public double hs, d0, d1, sv, sr, fw, wn, tw, ta, te; public int ns; public double[] sb, sa; public L15Spin sp, rs; }
	public static Hz L18bBroke(L18bBrokeO o)
	{
		var h = new Hz { kind = "L18bBroke" }; double cx = W / 2, cy = H / 2;
		double dist(double t) => o.d0 + o.d1 * (0.5 - 0.5 * JS.cos(BT(t) * Math.PI / 2));
		(double x, double y, double rot) half(int k, double t) { double a = o.sp.At(BT(t)) + k * Math.PI, d = dist(t) * L18bOut(L18bk(t, o.ta, o.ta + 0.6)); return (cx + JS.cos(a) * d, cy + JS.sin(a) * d, o.rs.At(BT(t)) * (k == 0 ? 1 : -1)); }
		(double, double) shard(int k, int j, double t) { double a = o.sa[k] + (j - (o.ns - 1) / 2.0) * o.fw / Math.Max(1, o.ns - 1), r = 20 + (t - o.sb[k]) * o.sv; return (cx + JS.cos(a) * r, cy + JS.sin(a) * r); }
		h.update = (t, dt) => { if (t > o.te + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < o.tw) return; double al = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.3));
			g.save();
			for (int k = 0; k < o.sb.Length; k++)
			{
				double d = t - o.sb[k]; if (d < -o.wn || d * o.sv > 900) continue;
				for (int j = 0; j < o.ns; j++)
				{
					if (d < 0) { double a = o.sa[k] + (j - (o.ns - 1) / 2.0) * o.fw / Math.Max(1, o.ns - 1); g.strokeStyle = L18bG(0.15 + 0.5 * (1 + d / o.wn)); g.lineWidth = 2; g.setLineDash(new double[] { 7, 9 }); g.beginPath(); g.moveTo(cx + JS.cos(a) * 60, cy + JS.sin(a) * 60); g.lineTo(cx + JS.cos(a) * 900, cy + JS.sin(a) * 900); g.stroke(); g.setLineDash(new double[0]); continue; }
					var (x, y) = shard(k, j, t); double sa = t * 9 + j + k; g.fillStyle = PINK; g.beginPath();
					for (int v = 0; v < 3; v++) { double q = sa + v * TAU / 3, rr = v == 0 ? o.sr * 1.5 : o.sr; if (v == 0) g.moveTo(x + JS.cos(q) * rr, y + JS.sin(q) * rr); else g.lineTo(x + JS.cos(q) * rr, y + JS.sin(q) * rr); }
					g.closePath(); g.fill();
				}
			}
			if (al > 0)
			{
				g.globalAlpha = al; bool on = t >= o.ta;
				for (int k = 0; k < 2; k++)
				{
					var (x, y, rot) = half(k, t); double s = o.hs;
					g.save(); g.translate(x, y); g.rotate(rot);
					g.beginPath(); for (int i = 0; i <= 48; i++) { double a = (k == 0 ? Math.PI / 2 : -Math.PI / 2) + i * Math.PI / 48, r = L18bHR(a) * s; if (i == 0) g.moveTo(JS.cos(a) * r, JS.sin(a) * r); else g.lineTo(JS.cos(a) * r, JS.sin(a) * r); }
					g.closePath(); g.fillStyle = on ? PINK : L18bG(0.5); g.fill();
					// the crack's edge (light, on the pink)
					g.strokeStyle = "rgba(255,215,228,0.9)"; g.lineWidth = 2; g.beginPath(); double sg = k == 0 ? -1 : 1; g.moveTo(0, -L18bHR(-Math.PI / 2) * s); for (int i = 1; i < 7; i++) g.lineTo(sg * (i % 2 == 0 ? 3 : 12), -L18bHR(-Math.PI / 2) * s + i * (L18bHR(-Math.PI / 2) + L18bHR(Math.PI / 2)) * s / 7); g.lineTo(0, L18bHR(Math.PI / 2) * s); g.stroke();
					g.restore();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t >= o.ta && t < o.te)
				for (int k = 0; k < 2; k++)
				{
					var (x, y, rot) = half(k, t); double dx = p.x - x, dy = p.y - y, c = JS.cos(-rot), s = JS.sin(-rot), lx = dx * c - dy * s, ly = dx * s + dy * c;
					if (k == 0 ? lx > p.r - 1 : lx < -p.r + 1) continue;
					double dd = hypot(lx, ly); if (dd < L18bHR(JS.atan2(ly, lx)) * o.hs + p.r - 1) return true;
				}
			for (int k = 0; k < o.sb.Length; k++)
			{
				double d = t - o.sb[k]; if (d < 0 || d * o.sv > 900) continue;
				for (int j = 0; j < o.ns; j++) { var (x, y) = shard(k, j, t); if (hypot(p.x - x, p.y - y) < o.sr * 1.05 + p.r - 1) return true; }
			}
			return false;
		};
		return h;
	}

	// ---------- 3:20 the last song: the staff bent into rings (radii rr[], dull) round the middle; note k sits on ring ri[k], starting at angle a0[k] at
	// t0[k] (s) and running round at w[k] radians a second until t1[k]; a grey note shows where it will start wn s before
	public static Hz L18bRound(double[] rr, double[] ri, double[] a0, double[] t0, double[] t1, double[] w, double r, double wn, double tw, double te)
	{
		var h = new Hz { kind = "L18bRound" }; int n = ri.Length; double cx = W / 2, cy = H / 2;
		(double, double) at(int k, double t) { double a = a0[k] + w[k] * (t - t0[k]), R = rr[(int)ri[k]]; return (cx + JS.cos(a) * R, cy + JS.sin(a) * R); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double al = Math.Min(L18bk(t, tw, tw + 0.6), 1 - L18bk(t, te, te + 0.4)); if (al <= 0) return;
			g.save(); g.globalAlpha = al;
			for (int k = 0; k < n; k++)
			{
				if (t < t0[k] - wn || t >= t1[k]) continue; var (x, y) = at(k, Math.Max(t, t0[k]));
				if (t < t0[k]) { double q = L18bk(t, t0[k] - wn, t0[k]); L18bNote(x, y, r, true, true, L18bG(0.25 + 0.5 * q)); double R = rr[(int)ri[k]]; g.strokeStyle = L18bG(0.2 + 0.4 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); g.arc(cx, cy, R, a0[k], a0[k] + w[k] * 0.9, w[k] < 0); g.stroke(); g.setLineDash(new double[0]); continue; }
				L18bNote(x, y, r, true, true, PINK);
			}
			g.restore();
		};
		L18bDeco(h, t =>
		{
			if (t < tw) return; double al = Math.Min(L18bk(t, tw, tw + 0.6), 1 - L18bk(t, te, te + 0.4)); if (al <= 0) return;
			g.save(); g.globalAlpha = al; g.strokeStyle = "rgba(200,190,170,0.25)"; g.lineWidth = 2; foreach (var R in rr) { g.beginPath(); g.arc(cx, cy, R, 0, TAU); g.stroke(); } g.restore();
		}, () => te);
		h.hit = (p, t) => { if (t >= te) return false; for (int k = 0; k < n; k++) { if (t < t0[k] || t >= t1[k]) continue; var (x, y) = at(k, t); if (L18bNoteHit(p, x, y, r, true)) return true; } return false; };
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L18.json) ----------
	// times are beats; wn are Normal's warnings (Easy stretches them by ezwn). An event fires before its first warning starts.
	// Easy: lists marked ez drop every third item, sizes a bit smaller, speeds a bit slower
	static double L18bwn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L18bez(double v, double ez) => easy ? v * ez : v;
	static int[] L18bKeep(int n, DodgeEv e) => Enumerable.Range(0, n).Where(k => !easy || e.num("ezk", 3) <= 0 || k % e.i("ezk", 3) != e.i("ezk", 3) - 1).ToArray();
	static double[] L18bSel(double[] a, int[] ks) => ks.Select(k => a[k]).ToArray();
	static readonly int _k18bfx = DodgeKind("L18bfx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L18bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k18bscene = DodgeKind("L18bscene", e => () => hazards.Add(L18bScene(e.i("sc"), B(e.num("fr", e.b)), B(e.num("to")), B(e.num("out", 1e6)))));
	static readonly int _k18btears = DodgeKind("L18btears", e => { double wn = L18bwn(e); return () =>
	{
		var ks = L18bKeep(e.nums("x").Length, e);
		hazards.Add(L18bTears(L18bSel(e.nums("x"), ks), L18Sec(L18bSel(e.nums("tb"), ks)), e.num("ft") * SPB * (easy ? 1.15 : 1), L18bez(e.num("r"), 0.85), wn * SPB, B(e.num("to"))));
	}; });
	static readonly int _k18bstaff = DodgeKind("L18bstaff", e => { double wn = L18bwn(e); return () =>
	{
		var ks = L18bKeep(e.nums("pl").Length, e);
		L18bAdd(L18bStaff(e.num("sp"), L18bSel(e.nums("pl"), ks), L18Sec(L18bSel(e.nums("tb"), ks)), L18bSel(e.nums("fl"), ks), L18bez(e.num("v"), 0.85), L18bez(e.num("r"), 0.85), wn * SPB, B(e.num("tw")), B(e.num("to"))));
	}; });
	static readonly int _k18bviolin = DodgeKind("L18bviolin", e => { double wn = L18bwn(e); return () =>
	{
		var rk = L18bKeep(e.nums("rb").Length, e);
		var o = new L18bVioO { mb = L18Sec(e.nums("mb")), mx = e.nums("mx"), my = e.nums("my"), sb = L18Sec(e.nums("sb")), ba = e.nums("ba").Select(a => a * Math.PI / 180).ToArray(),
			rb = L18Sec(L18bSel(e.nums("rb"), rk)), ga = L18bSel(e.nums("ga"), rk).Select(a => a * Math.PI / 180).ToArray(), bl = e.num("bl"), bw = L18bez(e.num("bw"), 0.8), sdur = e.num("sdur") * SPB * (easy ? 1.35 : 1),
			rv = L18bez(e.num("rv"), 0.85), rth = L18bez(e.num("rth"), 0.85), gw = L18bez(e.num("gw"), 1.3) * Math.PI / 180, wn = wn * SPB, tw = B(e.num("tw")), te = B(e.num("to")) };
		L18bAdd(L18bViolin(o));
	}; });
	static readonly int _k18brecord = DodgeKind("L18brecord", e => { double wn = L18bwn(e); return () =>
	{
		double tw = e.num("tw"), to = e.num("to"); var ak = L18bKeep(e.nums("ar").Length, e); var nk = L18bKeep(e.nums("nb").Length, e);
		var o = new L18bRecO { cx = e.num("cx", W / 2), cy = e.num("cy", H / 2), RR = e.num("RR"), th = L18bez(e.num("th"), 0.85), nv = L18bez(e.num("nv"), 0.85), nr = L18bez(e.num("nr"), 0.85), armL = e.num("armL"), armW = L18bez(e.num("armW"), 0.8),
			qx = e.num("qx"), qy = e.num("qy"), wn = wn * SPB, tw = B(tw), te = B(to),
			sp = new L15Spin(e.num("a0") * Math.PI / 180, tw, to + 4, e.nums("ssb"), e.nums("ssw").Select(v => v * (easy ? 0.85 : 1)).ToArray()),
			ar = L18bSel(e.nums("ar"), ak), aa = L18bSel(e.nums("aa"), ak).Select(a => a * Math.PI / 180).ToArray(), aw = L18bSel(e.nums("aw"), ak).Select(a => a * Math.PI / 180).ToArray(),
			on = L18Sec(L18bSel(e.nums("on"), ak)), off = L18Sec(L18bSel(e.nums("off"), ak)), qb = L18Sec(e.nums("qb")), qa = e.nums("qa").Select(a => a * Math.PI / 180).ToArray(),
			nb = L18Sec(L18bSel(e.nums("nb"), nk)), na = L18bSel(e.nums("na"), nk).Select(a => a * Math.PI / 180).ToArray(), mb = L18Sec(e.nums("mb")), mx = e.nums("mx") };
		L18bAdd(L18bRecord(o));
	}; });
	static readonly int _k18brain = DodgeKind("L18brain", e => { double wn = L18bwn(e); return () =>
	{
		var ks = L18bKeep(e.nums("x").Length, e);
		hazards.Add(L18bRain(L18bSel(e.nums("x"), ks), L18Sec(L18bSel(e.nums("tb"), ks)), L18bSel(e.nums("wa"), ks), L18bez(e.num("vy"), 0.85), e.num("wf"), L18bez(e.num("r"), 0.85), wn * SPB, B(e.num("to"))));
	}; });
	static readonly int _k18beq = DodgeKind("L18beq", e => () =>
	{
		var pb = e.nums("pb"); if (easy) pb = pb.Where((v, i) => i % 2 == 0).ToArray();
		hazards.Add(L18bEq(e.i("nc"), e.nums("pc"), L18Sec(e.nums("sb")), L18Sec(pb), L18bez(e.num("gap"), 1.2), L18bez(e.num("pg"), 0.6), B(e.num("tw")), B(e.num("ta")), B(e.num("to"))));
	});
	static readonly int _k18bripple = DodgeKind("L18bripple", e => { double wn = L18bwn(e); return () =>
	{
		var ks = L18bKeep(e.nums("x").Length, e);
		hazards.Add(L18bRipple(L18bSel(e.nums("x"), ks), L18bSel(e.nums("y"), ks), L18Sec(L18bSel(e.nums("tb"), ks)), L18bSel(e.nums("ga"), ks).Select(a => a * Math.PI / 180).ToArray(),
			L18bez(e.num("rv"), 0.85), e.num("rmax"), L18bez(e.num("th"), 0.85), L18bez(e.num("gw"), 1.3) * Math.PI / 180, wn * SPB, B(e.num("to"))));
	}; });
	static readonly int _k18bbeat = DodgeKind("L18bbeat", e => { double wn = L18bwn(e); return () =>
	{
		double tw = e.num("tw"), to = e.num("to"); var wk = L18bKeep(e.nums("wb").Length, e); var vb = e.nums("vb"); if (easy) vb = vb.Where((v, i) => i % 2 == 0).ToArray();
		var o = new L18bBeatO { hs = L18bez(e.num("hs"), 0.9), wv = L18bez(e.num("wv"), 0.85), wth = L18bez(e.num("wth"), 0.85), gw = L18bez(e.num("gw"), 1.3) * Math.PI / 180, vw = L18bez(e.num("vw"), 0.8), vd = e.num("vd") * SPB,
			na = e.i("na"), wn = wn * SPB, tw = B(tw), ta = B(e.num("ta")), te = B(to), hb = L18Sec(e.nums("hb")), wb = L18Sec(L18bSel(e.nums("wb"), wk)), wg = L18bSel(e.nums("wg"), wk).Select(a => a * Math.PI / 180).ToArray(), vb = L18Sec(vb),
			sp = new L15Spin(e.num("a0") * Math.PI / 180, tw, to + 4, e.nums("ssb"), e.nums("ssw").Select(v => v * (easy ? 0.85 : 1)).ToArray()) };
		L18bAdd(L18bBeat(o));
	}; });
	static readonly int _k18bbroke = DodgeKind("L18bbroke", e => { double wn = L18bwn(e); return () =>
	{
		double tw = e.num("tw"), to = e.num("to"); var sk = L18bKeep(e.nums("sb").Length, e);
		var o = new L18bBrokeO { hs = L18bez(e.num("hs"), 0.9), d0 = e.num("d0"), d1 = e.num("d1"), sv = L18bez(e.num("sv"), 0.85), sr = L18bez(e.num("sr"), 0.85), fw = e.num("fw") * Math.PI / 180, ns = easy ? Math.Max(2, e.i("ns") - 1) : e.i("ns"),
			wn = wn * SPB, tw = B(tw), ta = B(e.num("ta")), te = B(to), sb = L18Sec(L18bSel(e.nums("sb"), sk)), sa = L18bSel(e.nums("sa"), sk).Select(a => a * Math.PI / 180).ToArray(),
			sp = new L15Spin(e.num("a0") * Math.PI / 180, tw, to + 4, e.nums("ssb"), e.nums("ssw").Select(v => v * (easy ? 0.85 : 1)).ToArray()),
			rs = new L15Spin(0, tw, to + 4, e.nums("ssb"), e.nums("rsw").Select(v => v * (easy ? 0.85 : 1)).ToArray()) };
		hazards.Add(L18bBroke(o));
	}; });
	static readonly int _k18bround = DodgeKind("L18bround", e => { double wn = L18bwn(e); return () =>
	{
		var ks = L18bKeep(e.nums("ri").Length, e); double sl = easy ? 0.85 : 1;
		L18bAdd(L18bRound(e.nums("rr"), L18bSel(e.nums("ri"), ks), L18bSel(e.nums("a0"), ks).Select(a => a * Math.PI / 180).ToArray(), L18Sec(L18bSel(e.nums("tb"), ks)), L18Sec(L18bSel(e.nums("te"), ks)),
			L18bSel(e.nums("w"), ks).Select(v => v * sl).ToArray(), L18bez(e.num("r"), 0.85), wn * SPB, B(e.num("tw")), B(e.num("to"))));
	}; });
}
