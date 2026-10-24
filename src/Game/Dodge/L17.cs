using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L17 "حالف لا ما انام" (Jafar Al-Ghazal and Mohammed Jamal): the night that won't end. You are the eye that swore not to sleep,
// and the night tries to close it. Every attack is a fixed pattern on the beat that asks for one tactic:
// part 1 the clock (the ticking hands and the wedge they sweep, the numerals' daggers, the gears that bite in turn, the alarm's rings),
// part 2 the eyelids (the blink that leaves one slit, the window blinds, the doze in the dark, the eye's stare),
// part 3 till dawn (the star trails closing in, the messages from both sides, time racing on the clock, the dawn rising).
// Every hazard is a pure function of time once it exists (the stare also of where the square was when it aimed), so hit(p, t) is right
// for any later t. The data is data/dodge_L17.json (made by tools/level17/gen.py).
public static partial class G
{
	const double L17CX = W / 2, L17CY = H / 2;
	static double L17k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L17P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L17G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string L17W(double a) => $"rgba(255,244,236,{str(clamp(a, 0, 1))})";
	static double L17Ease(double k) => 1 - JS.pow(1 - clamp(k, 0, 1), 3);
	// an angle folded to -pi .. pi
	static double L17Ang(double a) { a %= TAU; if (a < -Math.PI) a += TAU; if (a > Math.PI) a -= TAU; return a; }
	static double L17Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void L17Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// the wedge from angle a0 turning by da (signed), from radius r0 to r1 round the clock's middle
	static void L17Wedge(double a0, double da, double r0, double r1)
	{
		double a1 = a0 + da; bool ccw = da < 0;
		g.beginPath(); g.arc(L17CX, L17CY, r1, a0, a1, ccw); g.arc(L17CX, L17CY, r0, a1, a0, !ccw); g.closePath(); g.fill();
	}
	static bool L17InWedge(double px, double py, double a0, double da, double r0, double r1, double pr)
	{
		double dx = px - L17CX, dy = py - L17CY, d = hypot(dx, dy); if (d < r0 - pr + 2 || d > r1 + pr - 2) return false;
		double rel = L17Ang(JS.atan2(dy, dx) - a0) * Math.Sign(da), m = (pr - 2) / Math.Max(d, 1);
		return rel > -m && rel < Math.Abs(da) + m;
	}
	// a clock hand: a blade from tail behind the middle out to len, with an open window (centre hc, half width hw) to slip through, and
	// the pin in the middle. The window's frame is white (it never hurts)
	public class L17HandS { public double tail = 40, len, w, hc, hw; }
	static void L17HandDraw(double a, L17HandS s, string col)
	{
		g.save(); g.translate(L17CX, L17CY); g.rotate(a); g.fillStyle = col; double w = s.w, h0 = s.hc - s.hw, h1 = s.hc + s.hw;
		g.beginPath(); g.moveTo(-s.tail, -w / 2); g.lineTo(h0, -w / 2); g.lineTo(h0, w / 2); g.lineTo(-s.tail, w / 2); g.closePath(); g.fill();
		g.beginPath(); g.moveTo(h1, -w / 2); g.lineTo(s.len - w, -w / 2); g.lineTo(s.len, 0); g.lineTo(s.len - w, w / 2); g.lineTo(h1, w / 2); g.closePath(); g.fill();
		circle(0, 0, 16);
		g.fillStyle = "rgba(60,0,20,0.35)"; g.fillRect(h1 + 10, -w * 0.12, Math.Max(0, s.len - h1 - w * 2), w * 0.24); g.fillRect(-s.tail + 6, -w * 0.12, Math.Max(0, h0 - 20 + s.tail), w * 0.24);
		g.strokeStyle = L17W(0.85); g.lineWidth = 2; g.strokeRect(h0 + 2, -w / 2 - 6, h1 - h0 - 4, w + 12);
		g.restore();
	}
	static bool L17HandHit(double a, L17HandS s, Player p)
	{
		double c = JS.cos(a), sn = JS.sin(a), m = s.w / 2 + p.r - 2;
		double u = (p.x - L17CX) * c + (p.y - L17CY) * sn, v = -(p.x - L17CX) * sn + (p.y - L17CY) * c;
		if (hypot(p.x - L17CX, p.y - L17CY) < 16 + p.r - 2) return true;
		if (Math.Abs(v) >= m || u < -s.tail - p.r || u > s.len + p.r) return false;
		// the point narrows over its last w
		if (u > s.len - s.w && Math.Abs(v) >= s.w / 2 * Math.Max(0, (s.len - u) / s.w) + p.r - 2) return false;
		// in the window (the square has to fit)
		return !(u - p.r + 2 > s.hc - s.hw && u + p.r - 2 < s.hc + s.hw);
	}

	// ---------- part 1: the clock ----------
	// a ticking hand: angle a0 (radians), and at each tick (beats tk) it jumps st (signed) in TD beats. With burn, the wedge it jumps across
	// burns for BH beats. The next place shows from wn beats before (a dashed hand, and the wedge in faint pink). Grey from tw, kills ta .. te
	const double L17TD = 0.1, L17BH = 0.2;
	public static Hz L17Hand(double a0, double st, double[] tk, L17HandS S, bool burn, double wn, double tw, double ta, double te)
	{
		var h = new Hz { x = L17CX, y = L17CY, kind = "L17Hand" }; double len = S.len;
		double ang(double bt)
		{
			double a = a0;
			foreach (var b in tk) { if (bt >= b + L17TD) a += st; else { if (bt > b) a += st * L17Ease((bt - b) / L17TD); break; } }
			return a;
		}
		int next(double bt) { for (int i = 0; i < tk.Length; i++) if (tk[i] + L17TD > bt) return i; return tk.Length; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = ang(bt), al = Math.Min(L17k(t, tw, tw + 0.4), 1 - L17k(t, te, te + 0.3)); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = al;
			int i = next(bt);
			// the wedge that burns (just after a tick)
			if (burn && on) for (int j = 0; j < tk.Length; j++)
			{
				if (bt < tk[j] || bt >= tk[j] + L17BH || B(tk[j]) < ta) continue;
				double s = a0 + st * j; g.save(); g.globalAlpha = 1; g.fillStyle = L17P(0.85); L17Wedge(s, st, 0, len); g.restore();
			}
			// the next tick: where the hand goes, and the wedge it will burn
			if (i < tk.Length && bt > tk[i] - wn && t < te && B(tk[i]) >= ta)
			{
				double k = L17k(bt, tk[i] - wn, tk[i]), s = a0 + st * i;
				if (burn) { g.fillStyle = L17P(0.05 + 0.2 * k); L17Wedge(s, st, 0, len); }
				g.save(); g.translate(L17CX, L17CY); g.rotate(s + st); L17Dash(-S.tail, 0, len, 0, L17W(0.25 + 0.5 * k), 3); g.restore();
			}
			if (on) { g.shadowColor = PINK; g.shadowBlur = 16; L17HandDraw(a, S, t - ta < 0.05 ? "#fff" : PINK); g.shadowBlur = 0; }
			else if (t < ta) { g.save(); g.translate(L17CX, L17CY); g.rotate(a); L17Dash(-S.tail, 0, len, 0, L17G(0.3 + 0.5 * L17k(t, tw, ta)), 4); g.restore(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), a = ang(bt);
			if (L17HandHit(a, S, p)) return true;
			if (burn) for (int j = 0; j < tk.Length; j++) if (bt >= tk[j] && bt < tk[j] + L17BH && B(tk[j]) >= ta && L17InWedge(p.x, p.y, a0 + st * j, st, 0, len, p.r)) return true;
			return false;
		};
		return h;
	}

	// the numerals' daggers: numeral fk[i] (0 = 12 at the top, clockwise, on radius R) lights up wn s before fb[i] (and its path shows), then
	// throws a dagger (dl long, dw wide) through the middle and across the screen at sp px a second
	public static Hz L17Chime(double R, double[] fb, double[] fk, double sp, double dl, double dw, double wn, double tw, double te)
	{
		var h = new Hz { x = L17CX, y = L17CY, kind = "L17Chime" }; int n = fb.Length; double far = R + 900;
		(double x, double y, double c, double s) at(int i) { double a = -Math.PI / 2 + fk[i] * TAU / 12, c = JS.cos(a), s = JS.sin(a); return (L17CX + c * R, L17CY + s * R, -c, -s); }
		h.update = (t, dt) => { if (t > te + 0.4 && (n == 0 || sp * (t - fb[n - 1]) - dl > far)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			for (int i = 0; i < n; i++)
			{
				var q = at(i); double u = t - fb[i];
				if (u < -wn || sp * u - dl > far) continue;
				if (u < 0)
				{
					double k = L17k(t, fb[i] - wn, fb[i]);
					L17Dash(q.x, q.y, q.x + q.c * far, q.y + q.s * far, L17P(0.12 + 0.35 * k), 2 + 2 * k);
					g.fillStyle = $"rgba(255,226,170,{str(0.15 + 0.5 * k)})"; circle(q.x, q.y, 22 + 8 * k);
					continue;
				}
				double s1 = sp * u, s0 = s1 - dl, x0 = q.x + q.c * s0, y0 = q.y + q.s * s0, x1 = q.x + q.c * s1, y1 = q.y + q.s * s1;
				// a white streak where it has been (harmless)
				g.strokeStyle = L17W(0.18); g.lineWidth = 3; g.beginPath(); if (s0 > 0) { g.moveTo(q.x + q.c * Math.Max(0, s0 - 160), q.y + q.s * Math.Max(0, s0 - 160)); g.lineTo(x0, y0); g.stroke(); }
				g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = u < 0.05 ? "#fff" : PINK;
				double nx = -q.s, ny = q.c, mx = (x0 + x1) / 2 - q.c * dl * 0.15, my = (y0 + y1) / 2 - q.s * dl * 0.15;
				g.beginPath(); g.moveTo(x1, y1); g.lineTo(mx + nx * dw / 2, my + ny * dw / 2); g.lineTo(x0, y0); g.lineTo(mx - nx * dw / 2, my - ny * dw / 2); g.closePath(); g.fill();
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			for (int i = 0; i < n; i++)
			{
				double u = t - fb[i]; if (u < 0) continue; double s1 = sp * u; if (s1 - dl > far) continue;
				// the dagger's own shape: from its back (u = 0) it widens to dw at 0.35 of its length, then narrows to the point
				var q = at(i); double s0 = s1 - dl, dx = p.x - q.x, dy = p.y - q.y, a = dx * q.c + dy * q.s - s0, v = Math.Abs(-dx * q.s + dy * q.c), m = p.r - 2;
				if (a < -m || a > dl + m) continue;
				double f = clamp(a / dl, 0, 1), hw = dw / 2 * (f < 0.35 ? f / 0.35 : (1 - f) / 0.65);
				if (v < hw + m) return true;
			}
			return false;
		};
		return h;
	}

	// the gears: the inner gear (radius ri, ni teeth) and the housing round it (everything past radius ro, no teeth pointing in) turn the
	// opposite ways (wi, wo radians a beat). At rest the teeth are restI / restO long; at the inner bites (ib) the inner teeth shoot out to
	// punI, at the outer bites (ob) the outer teeth shoot in to punO. A bite's reach shows as a dashed line wn beats before
	public static Hz L17Gears(double ri, double ro, int ni, int no, double hwi, double hwo, double wi, double wo, double a0i, double a0o,
		double restI, double restO, double punI, double punO, double[] ib, double[] ob, double wn, double tw, double ta, double te)
	{
		var h = new Hz { x = L17CX, y = L17CY, kind = "L17Gears" }; double b0 = BT(ta), bw = BT(tw), be = BT(te);
		const double UP = 0.07, HOLD = 0.24, DN = 0.14;
		double ext(double bt, double[] bs, double rest, double pun)
		{
			double v = rest;
			foreach (var b in bs)
			{
				double k = bt < b - UP ? 0 : bt < b ? (bt - b + UP) / UP : bt < b + HOLD ? 1 : bt < b + HOLD + DN ? 1 - (bt - b - HOLD) / DN : 0;
				if (k > 0) v = Math.Max(v, rest + (pun - rest) * k);
			}
			return v;
		}
		double nextB(double bt, double[] bs) { foreach (var b in bs) if (b + HOLD > bt) return b; return -1; }
		// the housing slides in from the edges (and the inner gear grows) in the first half beat; before that only their outlines show
		double roNow(double bt) => bt > be ? ro + (900 - ro) * L17k(bt, be, be + 0.6) : 900 - (900 - ro) * L17Ease(L17k(bt, b0, b0 + 0.5));
		double riNow(double bt) => bt > be ? ri * (1 - L17k(bt, be, be + 0.6)) : ri * L17Ease(L17k(bt, b0, b0 + 0.5));
		h.update = (t, dt) => { if (t > te + 0.8) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), ai = a0i + wi * (bt - b0), ao = a0o + wo * (bt - b0); bool on = t >= ta && t < te + 0.5;
			if (t < ta)
			{
				double q = L17k(t, tw, ta); g.fillStyle = L17P(0.04 + 0.12 * q); g.beginPath(); g.rect(-50, -50, W + 100, H + 100); g.arc(L17CX, L17CY, ro, 0, TAU, true); g.fill(); circle(L17CX, L17CY, ri + restI);
				g.strokeStyle = L17W(0.3 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 });
				g.beginPath(); g.arc(L17CX, L17CY, ro, 0, TAU); g.stroke(); g.beginPath(); g.arc(L17CX, L17CY, ri + restI, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				return;
			}
			double rI = riNow(bt), rO = roNow(bt), eI = rI < 1 ? 0 : ext(bt, ib, restI, punI), eO = ext(bt, ob, restO, punO);
			string col = on ? PINK : L17G(0.35 + 0.4 * L17k(t, tw, ta)); bool wh = Math.Abs(t - ta) < 0.05;
			g.save(); if (on) { g.shadowColor = PINK; g.shadowBlur = 14; }
			// the housing and its teeth
			g.fillStyle = wh ? "#fff" : col; g.beginPath(); g.rect(-50, -50, W + 100, H + 100); g.arc(L17CX, L17CY, rO, 0, TAU, true); g.fill();
			g.beginPath();
			for (int i = 0; i < no; i++) { double c = ao + i * TAU / no; g.moveTo(L17CX + JS.cos(c - hwo * 0.85) * (rO + 2), L17CY + JS.sin(c - hwo * 0.85) * (rO + 2)); g.arc(L17CX, L17CY, rO - eO, c - hwo * 0.85, c + hwo * 0.85); g.lineTo(L17CX + JS.cos(c + hwo * 0.85) * (rO + 2), L17CY + JS.sin(c + hwo * 0.85) * (rO + 2)); g.closePath(); }
			g.fill();
			// the inner gear
			if (rI > 1)
			{
				circle(L17CX, L17CY, rI); g.beginPath();
				for (int i = 0; i < ni; i++) { double c = ai + i * TAU / ni; g.moveTo(L17CX + JS.cos(c - hwi * 0.85) * (rI - 2), L17CY + JS.sin(c - hwi * 0.85) * (rI - 2)); g.arc(L17CX, L17CY, rI + eI, c - hwi * 0.85, c + hwi * 0.85); g.lineTo(L17CX + JS.cos(c + hwi * 0.85) * (rI - 2), L17CY + JS.sin(c + hwi * 0.85) * (rI - 2)); g.closePath(); }
				g.fill();
			}
			g.shadowBlur = 0;
			// the metal look: spokes and holes in a darker pink
			g.fillStyle = "rgba(70,0,24,0.4)";
			if (rI > 30) for (int i = 0; i < 5; i++) { double c = ai + i * TAU / 5 + 0.3; circle(L17CX + JS.cos(c) * rI * 0.55, L17CY + JS.sin(c) * rI * 0.55, rI * 0.16); }
			g.strokeStyle = "rgba(70,0,24,0.4)"; g.lineWidth = 3; g.beginPath(); g.arc(L17CX, L17CY, rO + 22, 0, TAU); g.stroke();
			g.beginPath(); for (int i = 0; i < 40; i++) { double c = ao * 0.5 + i * TAU / 40; g.moveTo(L17CX + JS.cos(c) * (rO + 30), L17CY + JS.sin(c) * (rO + 30)); g.lineTo(L17CX + JS.cos(c) * (rO + 60), L17CY + JS.sin(c) * (rO + 60)); } g.stroke();
			g.restore();
			// the next bites: how far the teeth will reach
			if (t >= ta && t < te)
			{
				double nb = nextB(bt, ib);
				if (nb >= 0 && bt > nb - wn && bt < nb) { double k = L17k(bt, nb - wn, nb); g.strokeStyle = L17W(0.3 + 0.6 * k); g.lineWidth = 3; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); double aiN = a0i + wi * (nb - b0); for (int i = 0; i < ni; i++) { double c = aiN + i * TAU / ni; g.moveTo(L17CX + JS.cos(c - hwi * 0.85) * (rI + punI), L17CY + JS.sin(c - hwi * 0.85) * (rI + punI)); g.arc(L17CX, L17CY, rI + punI, c - hwi * 0.85, c + hwi * 0.85); } g.stroke(); g.setLineDash(new double[0]); }
				double no2 = nextB(bt, ob);
				if (no2 >= 0 && bt > no2 - wn && bt < no2) { double k = L17k(bt, no2 - wn, no2); g.strokeStyle = L17W(0.3 + 0.6 * k); g.lineWidth = 3; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); double aoN = a0o + wo * (no2 - b0); for (int i = 0; i < no; i++) { double c = aoN + i * TAU / no; g.moveTo(L17CX + JS.cos(c - hwo * 0.85) * (rO - punO), L17CY + JS.sin(c - hwo * 0.85) * (rO - punO)); g.arc(L17CX, L17CY, rO - punO, c - hwo * 0.85, c + hwo * 0.85); } g.stroke(); g.setLineDash(new double[0]); }
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), dx = p.x - L17CX, dy = p.y - L17CY, d = hypot(dx, dy), a = JS.atan2(dy, dx), m = p.r - 2;
			double rI = riNow(bt), rO = roNow(bt), eI = ext(bt, ib, restI, punI), eO = ext(bt, ob, restO, punO);
			if (d < rI + m || d > rO - m) return true;
			double ai = a0i + wi * (bt - b0), ao = a0o + wo * (bt - b0), am = m / Math.Max(d, 1);
			if (d < rI + eI + m) { double x = L17Ang(a - ai) % (TAU / ni); if (x < 0) x += TAU / ni; if (x > TAU / ni / 2) x -= TAU / ni; if (Math.Abs(x) < hwi * 0.85 + am) return true; }
			if (d > rO - eO - m) { double x = L17Ang(a - ao) % (TAU / no); if (x < 0) x += TAU / no; if (x > TAU / no / 2) x -= TAU / no; if (Math.Abs(x) < hwo * 0.85 + am) return true; }
			return false;
		};
		return h;
	}

	// the alarm clock (it walks along the path P): at each ring (rb) a ring of sound leaves it at sp px a second, th thick, with one gap
	// (gw wide) at angle ga. The gap's way shows wn s before
	public static Hz L17Alarm(L15Path P, double[] rb, double[] ga, double gw, double sp, double th, double wn, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17Alarm" }; int n = rb.Length; var cx = new double[n]; var cy = new double[n];
		for (int i = 0; i < n; i++) { var c = P.At(BT(rb[i])); cx[i] = c.x; cy[i] = c.y; }
		double ringR(int i, double t) => 34 + sp * (t - rb[i]);
		h.update = (t, dt) => { if (t > te + 0.4 && (n == 0 || ringR(n - 1, t) > 1600)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t); var c = P.At(bt); double al = Math.Min(L17k(t, tw, tw + 0.4), 1 - L17k(t, te, te + 0.3)); bool on = t >= ta && t < te;
			for (int i = 0; i < n; i++)
			{
				double u = t - rb[i];
				if (u < 0)
				{
					if (u < -wn) continue; double k = L17k(t, rb[i] - wn, rb[i]), a = ga[i];
					// the way out: a white fan where the gap will be
					g.fillStyle = L17W(0.06 + 0.16 * k); g.beginPath(); g.moveTo(c.x, c.y); g.arc(c.x, c.y, 120 + 160 * k, a - gw / 2, a + gw / 2); g.closePath(); g.fill();
					continue;
				}
				double r = ringR(i, t); if (r > 1600) continue;
				g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = u < 0.05 ? "#fff" : PINK; g.lineWidth = th;
				g.beginPath(); g.arc(cx[i], cy[i], r, ga[i] + gw / 2, ga[i] - gw / 2 + TAU); g.stroke(); g.restore();
				// the edges of the gap
				g.fillStyle = L17W(0.8); foreach (var sd in new[] { -1.0, 1.0 }) circle(cx[i] + JS.cos(ga[i] + sd * gw / 2) * r, cy[i] + JS.sin(ga[i] + sd * gw / 2) * r, 4);
			}
			if (t >= te + 0.3) return;
			// the alarm clock itself (it hurts)
			double ring = 0; for (int i = 0; i < n; i++) { double u = t - rb[i]; if (u >= 0 && u < 0.25) ring = 1 - u / 0.25; }
			double sx = c.x + JS.sin(t * 90) * 4 * ring;
			g.save(); g.globalAlpha = al; g.translate(sx, c.y); g.rotate(JS.sin(t * 70) * 0.12 * ring);
			// before it hurts only its outline shows, so nothing under it is hidden
			if (!on) { g.strokeStyle = L17G(0.6); g.lineWidth = 2; g.beginPath(); g.arc(0, 0, 30, 0, TAU); g.moveTo(-11, -26); g.arc(-24, -26, 13, 0, TAU); g.moveTo(37, -26); g.arc(24, -26, 13, 0, TAU); g.moveTo(0, 0); g.lineTo(0, -15); g.moveTo(0, 0); g.lineTo(11, 4); g.stroke(); g.restore(); return; }
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14;
			circle(0, 0, 30); circle(-24, -26, 13); circle(24, -26, 13); g.fillRect(-26, 22, 8, 14); g.fillRect(18, 22, 8, 14); g.shadowBlur = 0;
			g.fillStyle = on ? "#b8286a" : "rgba(20,8,24,0.85)"; circle(0, 0, 22); g.strokeStyle = on ? "#3a0c24" : L17G(0.5); g.lineWidth = 3;
			g.beginPath(); g.moveTo(0, 0); g.lineTo(0, -15); g.moveTo(0, 0); g.lineTo(11, 4); g.stroke();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			if (t < te) { var c = P.At(BT(t)); if (hypot(p.x - c.x, p.y - c.y) < 30 + p.r - 2) return true; }
			for (int i = 0; i < n; i++)
			{
				double u = t - rb[i]; if (u < 0) continue; double r = ringR(i, t); if (r > 1600) continue;
				double dx = p.x - cx[i], dy = p.y - cy[i], d = hypot(dx, dy);
				if (Math.Abs(d - r) >= th / 2 + p.r - 2) continue;
				if (Math.Abs(L17Ang(JS.atan2(dy, dx) - ga[i])) > gw / 2 - (p.r - 2) / Math.Max(d, 1)) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- part 2: the eyelids ----------
	// a blink: the lids close from the top and the bottom (cl beats) and meet everywhere but an almond slit (centre sx,sy, half width sw,
	// half height sh) that is shut at beat tb, slides dx while it holds (hold beats), then opens (op beats). The slit shows wn beats before
	public static Hz L17Blink(double tb, double sx, double sy, double sw, double sh, double dx, double cl, double hold, double op, double wn)
	{
		var h = new Hz { kind = "L17Blink" };
		double K(double bt) => bt < tb - cl ? 0 : bt < tb ? L17Ease((bt - tb + cl) / cl) : bt < tb + hold ? 1 : bt < tb + hold + op ? 1 - L17Ease((bt - tb - hold) / op) : 0;
		// the slit slides at an even speed while the lids hold (a person can follow it)
		double X(double bt) => sx + dx * L17k(bt, tb, tb + hold);
		(double u, double l) edges(double x, double bt)
		{
			double k = K(bt), c = X(bt), q = (x - c) / sw, o = Math.Abs(q) < 1 ? sh * (1 - q * q) : 0;
			return (-40 + (sy - o + 40) * k, H + 40 + (sy + o - H - 40) * k);
		}
		h.update = (t, dt) => { if (BT(t) > tb + hold + op + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tb - cl - wn) return; double k = K(bt);
			// the slit to be in
			if (bt < tb + hold)
			{
				double q = L17k(bt, tb - cl - wn, tb - cl), c0 = sx, c1 = sx + dx;
				g.strokeStyle = L17W(0.35 + 0.55 * q); g.lineWidth = 3; g.setLineDash(new double[] { 9, 7 }); g.beginPath();
				double cc = bt >= tb ? X(bt) : c0;
				g.moveTo(cc - sw, sy); g.quadraticCurveTo(cc, sy - sh * 2, cc + sw, sy); g.quadraticCurveTo(cc, sy + sh * 2, cc - sw, sy); g.stroke(); g.setLineDash(new double[0]);
				if (Math.Abs(dx) > 1 && bt < tb + hold * 0.7) { double d = Math.Sign(dx); g.strokeStyle = L17W(0.3 + 0.5 * q); g.lineWidth = 4; g.beginPath(); g.moveTo(c1 - d * 18, sy - 14); g.lineTo(c1, sy); g.lineTo(c1 - d * 18, sy + 14); g.stroke(); L17Dash(cc, sy, c1, sy, L17W(0.25 + 0.4 * q), 2); }
			}
			if (k <= 0) return;
			bool wh = bt >= tb && bt < tb + 0.06;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 14;
			for (int sd = 0; sd < 2; sd++)
			{
				g.fillStyle = wh ? "#fff" : PINK; g.beginPath(); g.moveTo(-10, sd == 0 ? -50 : H + 50);
				for (double x = -10; x <= W + 10; x += 10) { var e = edges(x, bt); g.lineTo(x, sd == 0 ? e.u : e.l); }
				g.lineTo(W + 10, sd == 0 ? -50 : H + 50); g.closePath(); g.fill();
			}
			g.shadowBlur = 0;
			// the lashes and the crease, in a darker pink inside the lids
			g.strokeStyle = "rgba(90,0,30,0.55)"; g.lineWidth = 3; g.beginPath();
			for (double x = 0; x <= W; x += 22) { var e = edges(x, bt); g.moveTo(x, e.u - 2); g.lineTo(x - 6, e.u - 18); g.moveTo(x, e.l + 2); g.lineTo(x - 6, e.l + 18); }
			g.stroke();
			g.strokeStyle = "rgba(90,0,30,0.35)"; g.lineWidth = 4; g.beginPath();
			for (double x = -10; x <= W + 10; x += 20) { var e = edges(x, bt); double yu = e.u - 46 * k, yl = e.l + 46 * k; if (x <= -10) { g.moveTo(x, yu); } else g.lineTo(x, yu); }
			for (double x = -10; x <= W + 10; x += 20) { var e = edges(x, bt); double yl = e.l + 46 * k; if (x <= -10) { g.moveTo(x, yl); } else g.lineTo(x, yl); }
			g.stroke(); g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (K(bt) <= 0) return false; var e = edges(p.x, bt);
			return p.y - p.r + 2 < e.u || p.y + p.r - 2 > e.l;
		};
		return h;
	}

	// the window blinds: n slats across the screen (rows, or columns with vert). Slat fs[i] turns shut at beat fb[i] (in fl beats) and stays
	// shut hold beats, then turns open. A shut slat is a pink wall; an open one is a thin grey line. The next ones show wn beats before
	public static Hz L17Blinds(bool vert, int n, double[] fb, double[] fs, double hold, double fl, double wn, double tw, double te)
	{
		var h = new Hz { kind = "L17Blinds" }; double S = (vert ? W : H) / n, L = vert ? H : W;
		var per = new List<double>[n]; for (int i = 0; i < n; i++) per[i] = new List<double>();
		for (int i = 0; i < fb.Length; i++) { int s = (int)fs[i]; if (s >= 0 && s < n) per[s].Add(fb[i]); }
		double Q(int s, double bt)
		{
			double q = 0;
			foreach (var b in per[s]) { double k = bt < b - fl ? 0 : bt < b ? (bt - b + fl) / fl : bt < b + hold ? 1 : bt < b + hold + fl ? 1 - (bt - b - hold) / fl : 0; q = Math.Max(q, k); }
			return q;
		}
		double W8(int s, double bt) { double w = 0; foreach (var b in per[s]) if (bt >= b - fl - wn && bt < b - fl) w = Math.Max(w, L17k(bt, b - fl - wn, b - fl)); return w; }
		void rect(int s, double c, double hh) { double a = s * S + S / 2; if (vert) g.fillRect(a - hh / 2, 0, hh, L); else g.fillRect(0, a - hh / 2, L, hh); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), al = Math.Min(L17k(t, tw, tw + 0.5), 1 - L17k(t, te, te + 0.4));
			g.save(); g.globalAlpha = al;
			for (int s = 0; s < n; s++)
			{
				double q = Q(s, bt), w = W8(s, bt);
				// the cord and the open slat (thin, harmless)
				g.fillStyle = L17G(0.22); rect(s, 0, 4);
				if (w > 0 && q <= 0) { g.fillStyle = L17P(0.05 + 0.22 * w); rect(s, 0, S - 6); g.strokeStyle = L17W(0.2 + 0.4 * w); g.lineWidth = 2; double a = s * S + 3; if (vert) g.strokeRect(a, 2, S - 6, L - 4); else g.strokeRect(2, a, L - 4, S - 6); }
				if (q > 0)
				{
					g.fillStyle = PINK; rect(s, 0, Math.Max(4, (S - 2) * q));
					// the slat's ridges
					g.fillStyle = "rgba(90,0,30,0.3)"; double a0 = s * S + S / 2; if (vert) g.fillRect(a0 - 2, 0, 4, L); else g.fillRect(0, a0 - 2, L, 4);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw || t >= te) return false; double bt = BT(t), c = vert ? p.x : p.y;
			int s0 = (int)clamp(Math.Floor((c - p.r) / S), 0, n - 1), s1 = (int)clamp(Math.Floor((c + p.r) / S), 0, n - 1);
			for (int s = s0; s <= s1; s++) { double q = Q(s, bt); if (q > 0.05 && Math.Abs(c - (s * S + S / 2)) < (S - 2) * q / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the doze: all goes dark pink but a circle of light walking the path P (its next beat shows as a dashed line). At te the eye snaps open:
	// the dark lifts and lashes fly out from the circle's edge
	public static Hz L17Doze(L15Path P, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L17Doze" }; const int NL = 28; double lsp = 1000, lln = 46; bool b0 = false; double ex = 0, ey = 0, er = 0;
		h.update = (t, dt) =>
		{
			if (!b0 && t >= te) { b0 = true; var c = P.At(BT(te)); ex = c.x; ey = c.y; er = c.r; shake = Math.Max(shake, 6); burst(ex, ey, "#fff", 18, 500); }
			if (t > te + 1.4) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t);
			if (t < te + 0.2)
			{
				var c = P.At(bt); bool deadly = t >= ta;
				// first only the circle to be in (so nothing under it is hidden), the dark comes in the last half second
				if (t < ta - 0.5) { double q = L17k(t, tw, ta - 0.5); g.strokeStyle = L17W(0.3 + 0.6 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(c.x, c.y, c.r, 0, TAU); g.stroke(); g.setLineDash(new double[0]); return; }
				double a = Math.Min(L17k(t, ta - 0.5, ta), 1 - L17k(t, te, te + 0.2));
				g.save(); g.globalAlpha = a;
				g.fillStyle = deadly ? "rgba(210,22,82,0.92)" : "rgba(40,30,52,0.85)"; g.beginPath(); g.rect(0, 0, W, H); g.arc(c.x, c.y, c.r, 0, TAU, true); g.fill();
				// heavy lids closing over it: dark bands, slow
				g.fillStyle = deadly ? "rgba(80,0,26,0.35)" : "rgba(10,8,16,0.35)"; for (int i = 0; i < 6; i++) { double y = (i * 130 + t * 24) % (H + 130) - 65; g.fillRect(0, y, W, 26); }
				g.fillStyle = deadly ? "rgba(210,22,82,0.92)" : "rgba(40,30,52,0.85)"; g.beginPath(); g.arc(c.x, c.y, c.r + 1, 0, TAU); g.rect(0, 0, W, H); g.fill("evenodd");
				var gr = g.createRadialGradient(c.x, c.y, 0, c.x, c.y, c.r); gr.addColorStop(0, "rgba(255,236,200,0.22)"); gr.addColorStop(1, "rgba(255,236,200,0.02)"); g.fillStyle = gr; circle(c.x, c.y, c.r);
				g.strokeStyle = deadly ? PINK : L17G(0.6); g.lineWidth = 3; g.beginPath(); g.arc(c.x, c.y, c.r, 0, TAU); g.stroke();
				if (t < te) { g.strokeStyle = L17W(0.45); g.lineWidth = 2; g.setLineDash(new double[] { 8, 9 }); g.beginPath(); for (int i = 0; i <= 8; i++) { var q = P.At(bt + i * 0.125); if (i == 0) g.moveTo(q.x, q.y); else g.lineTo(q.x, q.y); } g.stroke(); g.setLineDash(new double[0]); }
				g.restore();
			}
			if (t >= te)
			{
				double u = t - te; g.save(); g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = u < 0.05 ? "#fff" : PINK; g.lineWidth = 6; g.lineCap = "round"; g.beginPath();
				for (int i = 0; i < NL; i++) { double a = i * TAU / NL, r0 = er + lsp * u; g.moveTo(ex + JS.cos(a) * r0, ey + JS.sin(a) * r0); g.lineTo(ex + JS.cos(a) * (r0 + lln), ey + JS.sin(a) * (r0 + lln)); }
				g.stroke(); g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false;
			if (t < te) { var c = P.At(BT(t)); return hypot(p.x - c.x, p.y - c.y) > c.r - p.r + 2 && !L17InEye(p.x, p.y, t); }
			double u = t - te; if (u > 1.4) return false;
			for (int i = 0; i < NL; i++) { double a = i * TAU / NL, r0 = er + lsp * u; if (L17Seg(p.x, p.y, ex + JS.cos(a) * r0, ey + JS.sin(a) * r0, ex + JS.cos(a) * (r0 + lln), ey + JS.sin(a) * (r0 + lln)) < 3 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the eye's stare: a big eye at (x,y) watches the square. lk beats before each fire (fb) it takes aim at where the square is; at fb a beam
	// (bw wide) goes out that way for hold beats. Before it takes aim a white line follows the square; then the aim line turns pink
	// the eyes on screen (they hide what is under them, so nothing hurts there): x, y, tw, ta, te
	static readonly List<double[]> L17Eyes = new();
	static bool L17InEye(double px, double py, double t)
	{
		foreach (var e in L17Eyes)
		{
			if (t < e[2] || t > e[4] + 0.3) continue; double open = Math.Min(L17k(t, e[2], e[3]), 1 - L17k(t, e[4], e[4] + 0.3)), ex = px - e[0], ey = py - e[1];
			if (Math.Abs(ex) < 86 && Math.Abs(ey) < 42 * open * (1 - ex * ex / (86.0 * 86.0)) - 2) return true;
		}
		return false;
	}
	public static Hz L17Stare(double x, double y, double[] fb, double lk, double bw, double hold, double wn, double tw, double ta, double te)
	{
		if (!L17Eyes.Exists(e => e[0] == x && e[1] == y && e[2] == tw)) L17Eyes.Add(new[] { x, y, tw, ta, te });
		var h = new Hz { x = x, y = y, kind = "L17Stare" }; int n = fb.Length; var ax = new double[n]; var ay = new double[n]; var got = new bool[n];
		double lx = x + 1, ly = y + 200;
		h.update = (t, dt) =>
		{
			double bt = BT(t);
			if (player != null) { lx = player.x; ly = player.y; }
			for (int i = 0; i < n; i++) if (!got[i] && bt >= fb[i] - lk) { got[i] = true; double dx = lx - x, dy = ly - y, d = Math.Max(1, hypot(dx, dy)); ax[i] = dx / d; ay[i] = dy / d; }
			if (t > te + 0.4) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), al = Math.Min(L17k(t, tw, tw + 0.5), 1 - L17k(t, te, te + 0.4));
			// what the eye looks at: the next aim, or the square
			double lookX = lx - x, lookY = ly - y; int nx = -1; for (int i = 0; i < n; i++) if (bt < fb[i] + hold) { nx = i; break; }
			if (nx >= 0 && got[nx]) { lookX = ax[nx]; lookY = ay[nx]; }
			double ld = Math.Max(1, hypot(lookX, lookY)); lookX /= ld; lookY /= ld;
			for (int i = 0; i < n; i++)
			{
				double u = bt - fb[i]; if (u < -wn || u >= hold) continue;
				if (u < 0)
				{
					double k = L17k(bt, fb[i] - wn, fb[i]);
					if (got[i]) L17Dash(x, y, x + ax[i] * 1600, y + ay[i] * 1600, L17P(0.35 + 0.5 * k), 3);
					else L17Dash(x, y, x + lookX * 1600, y + lookY * 1600, L17W(0.2 + 0.3 * k), 2);
					continue;
				}
				g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.strokeStyle = u < 0.06 ? "#fff" : PINK; g.lineWidth = bw * (u < 0.05 ? 0.6 + 8 * u : 1); g.lineCap = "round";
				g.beginPath(); g.moveTo(x, y); g.lineTo(x + ax[i] * 1600, y + ay[i] * 1600); g.stroke(); g.restore();
			}
			// the eye (it never hurts: a cold white eye with a dark iris)
			double open = Math.Min(L17k(t, tw, ta), 1 - L17k(t, te, te + 0.3)); if (open <= 0) return;
			g.save(); g.globalAlpha = al; g.translate(x, y);
			g.fillStyle = "rgba(226,222,236,0.95)"; g.beginPath(); g.moveTo(-86, 0); g.quadraticCurveTo(0, -84 * open, 86, 0); g.quadraticCurveTo(0, 84 * open, -86, 0); g.fill();
			g.save(); g.beginPath(); g.moveTo(-86, 0); g.quadraticCurveTo(0, -84 * open, 86, 0); g.quadraticCurveTo(0, 84 * open, -86, 0); g.clip();
			double ix = lookX * 30, iy = lookY * 16; bool aim = nx >= 0 && got[nx] && bt < fb[nx];
			g.fillStyle = "rgba(52,26,84,1)"; circle(ix, iy, 30); g.fillStyle = aim ? "#fff" : "#06040c"; circle(ix, iy, aim ? 10 + 6 * pulse : 14);
			g.fillStyle = "rgba(255,255,255,0.7)"; circle(ix - 10, iy - 10, 5);
			g.restore();
			g.strokeStyle = "rgba(140,120,170,0.9)"; g.lineWidth = 3; g.beginPath(); g.moveTo(-86, 0); g.quadraticCurveTo(0, -84 * open, 86, 0); g.quadraticCurveTo(0, 84 * open, -86, 0); g.stroke();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te + B(hold) - B(0)) return false; double bt = BT(t);
			// inside an eye the beam does not show, so it does not hurt there
			if (L17InEye(p.x, p.y, t)) return false;
			for (int i = 0; i < n; i++) { double u = bt - fb[i]; if (u >= 0.04 && u < hold && got[i] && L17Seg(p.x, p.y, x, y, x + ax[i] * 1600, y + ay[i] * 1600) < bw / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- part 3: till dawn ----------
	// the star trails: rings of stars come in from radius r0 to the moon at v px a beat (ring i from beat sb[i]), each turning w[i] radians a
	// beat, th thick, with one gap gw wide (at angle ga[i] when it starts)
	public static Hz L17Trails(double[] sb, double[] ga, double[] w, double r0, double v, double gw, double th, double tw, double te)
	{
		var h = new Hz { x = L17CX, y = L17CY, kind = "L17Trails" }; int n = sb.Length;
		double R(int i, double bt) => r0 - v * (bt - sb[i]);
		double A(int i, double bt) => ga[i] + w[i] * (bt - sb[i]);
		h.update = (t, dt) => { if (t > te + 0.4 && (n == 0 || R(n - 1, BT(t)) < 0)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t);
			for (int i = 0; i < n; i++)
			{
				if (bt < sb[i]) continue; double r = R(i, bt); if (r <= 4) continue; double a = A(i, bt), gg = Math.Min(gw, TAU * 0.9);
				g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = th;
				g.beginPath(); g.arc(L17CX, L17CY, r, a + gg / 2, a - gg / 2 + TAU); g.stroke(); g.shadowBlur = 0;
				// the stars along the trail
				g.fillStyle = PINK; int k = Math.Max(6, (int)(r * (TAU - gg) / 34));
				for (int j = 0; j <= k; j++) { double q = a + gg / 2 + (TAU - gg) * j / k, sx = L17CX + JS.cos(q) * r, sy = L17CY + JS.sin(q) * r, s = th * 0.55 * (j % 2 == 0 ? 1 : 0.7); g.beginPath(); g.moveTo(sx, sy - s); g.lineTo(sx + s * 0.35, sy); g.lineTo(sx, sy + s); g.lineTo(sx - s * 0.35, sy); g.closePath(); g.fill(); g.beginPath(); g.moveTo(sx - s, sy); g.lineTo(sx, sy + s * 0.35); g.lineTo(sx + s, sy); g.lineTo(sx, sy - s * 0.35); g.closePath(); g.fill(); }
				g.restore();
				// the gap's edges in white
				g.fillStyle = L17W(0.85); foreach (var sd in new[] { -1.0, 1.0 }) circle(L17CX + JS.cos(a + sd * gg / 2) * r, L17CY + JS.sin(a + sd * gg / 2) * r, 4);
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), dx = p.x - L17CX, dy = p.y - L17CY, d = hypot(dx, dy), pa = JS.atan2(dy, dx);
			for (int i = 0; i < n; i++)
			{
				if (bt < sb[i]) continue; double r = R(i, bt); if (r <= 4 || Math.Abs(d - r) >= th / 2 + p.r - 2) continue;
				if (Math.Abs(L17Ang(pa - A(i, bt))) > Math.Min(gw, TAU * 0.9) / 2 - (p.r - 2) / Math.Max(d, 1)) return true;
			}
			return false;
		};
		return h;
	}

	// the messages: bubbles slide across the screen in lanes (n lanes, bubble bh tall). Bubble i comes in lane fl[i] from the left (fs 0) or
	// the right (fs 1) at beat fb[i], fL[i] long, at sp px a second. "Typing..." dots show where it comes from wn s before
	public static Hz L17Msgs(int n, double bh, double[] fb, double[] fl, double[] fs, double[] fL, double sp, double wn, double tw, double te)
	{
		var h = new Hz { kind = "L17Msgs" }; int m = fb.Length; double LH = H / n;
		(double x0, double x1, double y0) box(int i, double t) { double lead = sp * (t - fb[i]), y0 = fl[i] * LH + (LH - bh) / 2; return fs[i] == 0 ? (lead - fL[i], lead, y0) : (W - lead, W - lead + fL[i], y0); }
		h.update = (t, dt) => { if (t > te + 0.4 && (m == 0 || sp * (t - fb[m - 1]) - fL[m - 1] > W + 40)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			// first what is coming ("typing..." at its edge, its lane in faint pink), so nothing covers a bubble
			for (int i = 0; i < m; i++)
			{
				double u = t - fb[i]; if (u < -wn || u >= 0) continue;
				double k = L17k(t, fb[i] - wn, fb[i]), y = box(i, t).y0;
				g.fillStyle = L17P(0.04 + 0.12 * k); g.fillRect(0, y, W, bh);
				double tx = fs[i] == 0 ? 16 : W - 16 - 96; g.fillStyle = L17G(0.25 + 0.45 * k); g.beginPath(); g.roundRect(tx, y + bh / 2 - 24, 96, 48, 24); g.fill();
				g.fillStyle = "rgba(20,14,30,0.9)"; for (int j = 0; j < 3; j++) { double ph = JS.sin(t * 12 - j * 0.9); circle(tx + 28 + j * 20, y + bh / 2 - 3 * ph, 6); }
			}
			for (int i = 0; i < m; i++)
			{
				double u = t - fb[i]; if (u < 0) continue; var b = box(i, t);
				if (b.x0 > W + 10 || b.x1 < -10) continue;
				g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = u < 0.05 ? "#fff" : PINK;
				g.beginPath(); g.roundRect(b.x0, b.y0, b.x1 - b.x0, bh, bh * 0.38); g.fill(); g.shadowBlur = 0;
				// the lines of text and the two ticks
				g.fillStyle = "rgba(90,0,30,0.45)"; double lw = b.x1 - b.x0 - 70; g.fillRect(b.x0 + 30, b.y0 + bh * 0.3, lw * 0.85, 9); g.fillRect(b.x0 + 30, b.y0 + bh * 0.58, lw * 0.55, 9);
				g.strokeStyle = "rgba(255,255,255,0.85)"; g.lineWidth = 3; double cx2 = b.x1 - 34, cy2 = b.y0 + bh - 18; g.beginPath(); g.moveTo(cx2, cy2); g.lineTo(cx2 + 5, cy2 + 5); g.lineTo(cx2 + 13, cy2 - 4); g.moveTo(cx2 + 8, cy2); g.lineTo(cx2 + 13, cy2 + 5); g.lineTo(cx2 + 21, cy2 - 4); g.stroke();
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			for (int i = 0; i < m; i++)
			{
				if (t < fb[i]) continue; var b = box(i, t); double rr = bh * 0.38;
				// the bubble's rounded box
				double qx = clamp(p.x, b.x0 + rr, b.x1 - rr), qy = clamp(p.y, b.y0 + rr, b.y0 + bh - rr);
				if (hypot(p.x - qx, p.y - qy) < rr + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// time racing: the clock's hands spin by their plans (sp, radians at a beat) and turn back on the strong beats. The arrows round the hub
	// show the way the long hand turns and flip a beat before it does
	public static Hz L17Race(L17HandS[] S, L15Spin[] sp, double tw, double ta, double te)
	{
		var h = new Hz { x = L17CX, y = L17CY, kind = "L17Race" }; int n = S.Length;
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), al = Math.Min(L17k(t, tw, tw + 0.4), 1 - L17k(t, te, te + 0.3)); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = al;
			for (int i = 0; i < n; i++)
			{
				double a = sp[i].At(bt);
				if (on) { g.shadowColor = PINK; g.shadowBlur = 16; L17HandDraw(a, S[i], t - ta < 0.05 ? "#fff" : PINK); g.shadowBlur = 0; }
				else { g.save(); g.translate(L17CX, L17CY); g.rotate(a); L17Dash(-S[i].tail, 0, S[i].len, 0, L17G(0.3 + 0.5 * L17k(t, tw, ta)), 4); g.restore(); }
			}
			// the arrows show the way the long hand turns; they flash a beat before it turns back
			double w = sp[0].W(bt), w1 = sp[0].W(bt + 1); int dir = Math.Sign(Math.Abs(w1) > 0.02 ? w1 : w); bool flip = Math.Sign(w1) != Math.Sign(w) && Math.Abs(w1) > 0.02;
			g.strokeStyle = flip ? (bt % 0.5 < 0.25 ? "#fff" : L17W(0.6)) : L17W(0.4); g.lineWidth = 4; double a0 = sp[0].At(bt);
			for (int i = 0; i < 8; i++) { double an = a0 + i * TAU / 8 + 0.4, x = L17CX + JS.cos(an) * 360, y = L17CY + JS.sin(an) * 330, d = an + dir * Math.PI / 2; g.beginPath(); g.moveTo(x + JS.cos(d + 2.5) * 13, y + JS.sin(d + 2.5) * 13); g.lineTo(x, y); g.lineTo(x + JS.cos(d - 2.5) * 13, y + JS.sin(d - 2.5) * 13); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t);
			for (int i = 0; i < n; i++) if (L17HandHit(sp[i].At(bt), S[i], p)) return true;
			return false;
		};
		return h;
	}

	// the dawn: the horizon rises (its top at hy[i] at beat hb[i], straight between) and everything under it burns; from the sun on it
	// nr rays fan out (rw wide) and sweep: angle = up + spread * (i - middle) + amp sin(2 pi (beat - ph) / per). All of it ends at te (the sun is up)
	public static Hz L17Dawn(double[] hb, double[] hy, int nr, double spread, double amp, double per, double ph, double rw, double tw, double ta, double tr, double te)
	{
		var h = new Hz { kind = "L17Dawn" }; bool b0 = false;
		double Y(double bt) { if (bt <= hb[0]) return hy[0]; for (int i = 0; i < hb.Length - 1; i++) if (bt < hb[i + 1]) return hy[i] + (hy[i + 1] - hy[i]) * (bt - hb[i]) / (hb[i + 1] - hb[i]); return hy[^1]; }
		double RA(int i, double bt) => -Math.PI / 2 + spread * (i - (nr - 1) / 2.0) + amp * JS.sin(TAU * (bt - ph) / per);
		h.update = (t, dt) => { if (!b0 && t >= te) { b0 = true; flash = Math.Max(flash, 0.5); shake = Math.Max(shake, 8); burst(W / 2, Y(BT(te)), "#ffe0b0", 40, 700); } if (t > te + 1.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), y = Y(bt), a = Math.Min(L17k(t, tw, tw + 0.5), 1 - L17k(t, te, te + 0.8)); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			// the rays (from the sun, under the horizon's edge)
			if (t >= tr - 1.2)
			{
				bool ron = t >= tr && t < te;
				for (int i = 0; i < nr; i++)
				{
					double an = RA(i, bt), c = JS.cos(an), s = JS.sin(an);
					if (!ron) { L17Dash(W / 2, y + 40, W / 2 + c * 1500, y + 40 + s * 1500, L17G(0.25 + 0.5 * L17k(t, tr - 1.2, tr)), 3); continue; }
					g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = t - tr < 0.05 ? "#fff" : PINK; g.lineWidth = rw; g.beginPath(); g.moveTo(W / 2, y + 40); g.lineTo(W / 2 + c * 1500, y + 40 + s * 1500); g.stroke(); g.restore();
				}
			}
			// the sun's top behind the edge (the land covers the rest)
			g.fillStyle = "rgba(255,230,190,0.9)"; g.beginPath(); g.arc(W / 2, y + 40, 46, Math.PI, TAU); g.fill();
			// the land under the dawn
			g.fillStyle = on ? PINK : L17G(0.4); if (on) { g.shadowColor = PINK; g.shadowBlur = 18; } g.fillRect(-20, y, W + 40, H - y + 40); g.shadowBlur = 0;
			var gr = g.createLinearGradient(0, y, 0, H); gr.addColorStop(0, "rgba(255,190,120,0.35)"); gr.addColorStop(1, "rgba(90,0,30,0.45)"); g.fillStyle = gr; g.fillRect(-20, y, W + 40, H - y + 40);
			g.restore();
			// where the horizon goes next
			if (t < te) { double ny = Y(bt + 2); if (ny < y - 3) L17Dash(0, ny, W, ny, L17P(0.5 * a), 2); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), y = Y(bt);
			if (p.y + p.r - 2 > y) return true;
			if (t >= tr) for (int i = 0; i < nr; i++) { double an = RA(i, bt); if (L17Seg(p.x, p.y, W / 2, y + 40, W / 2 + JS.cos(an) * 1500, y + 40 + JS.sin(an) * 1500) < rw / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- the room itself (harmless) ----------
	// beats 0 .. 64 the clock face, 64 .. 120 the dark bedroom with a closed eye, 120 .. the night sky and the moon; t0 .. t1 seconds
	public static Hz L17Stage(double t0, double t1)
	{
		var h = new Hz { kind = "L17Stage" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.8 - t) / 0.8)); if (a <= 0) return; double bt = BT(t);
			double p1 = 1 - L17k(bt, 63, 65), p3 = L17k(bt, 119, 121), p2 = 1 - p1 - p3;
			g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(8,10,26,0.9)"; g.fillRect(0, 0, W, H);
			if (p1 > 0)
			{
				g.globalAlpha = a * p1;
				g.fillStyle = "rgba(20,22,44,0.8)"; circle(L17CX, L17CY, 336);
				g.strokeStyle = L17G(0.32); g.lineWidth = 6; g.beginPath(); g.arc(L17CX, L17CY, 336, 0, TAU); g.stroke();
				g.lineWidth = 2; g.beginPath(); g.arc(L17CX, L17CY, 318, 0, TAU); g.stroke();
				g.beginPath(); for (int i = 0; i < 60; i++) { double q = i * TAU / 60, l = i % 5 == 0 ? 22 : 9; g.moveTo(L17CX + JS.cos(q) * 316, L17CY + JS.sin(q) * 316); g.lineTo(L17CX + JS.cos(q) * (316 - l), L17CY + JS.sin(q) * (316 - l)); } g.stroke();
				g.font = $"800 34px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillStyle = L17G(0.3 + 0.1 * pulse);
				for (int i = 0; i < 12; i++) { double q = -Math.PI / 2 + i * TAU / 12; g.fillText(str(i == 0 ? 12 : i), L17CX + JS.cos(q) * 262, L17CY + JS.sin(q) * 262); }
			}
			if (p2 > 0)
			{
				g.globalAlpha = a * p2;
				// the window and a closed eye on the wall
				g.strokeStyle = L17G(0.12); g.lineWidth = 4; g.strokeRect(80, 50, W - 160, H - 100); g.beginPath(); g.moveTo(W / 2, 50); g.lineTo(W / 2, H - 50); g.stroke();
				double br = 1 + 0.03 * JS.sin(bt * 0.8);
				g.strokeStyle = L17G(0.1 + 0.05 * pulse); g.lineWidth = 6; g.beginPath(); g.moveTo(L17CX - 300 * br, L17CY); g.quadraticCurveTo(L17CX, L17CY + 150 * br, L17CX + 300 * br, L17CY); g.stroke();
				g.lineWidth = 4; g.beginPath(); for (int i = 0; i < 9; i++) { double q = (i - 4) / 4.5, x = L17CX + q * 260 * br, y = L17CY + 75 * br * (1 - q * q); g.moveTo(x, y); g.lineTo(x + q * 12, y + 30); } g.stroke();
			}
			if (p3 > 0)
			{
				g.globalAlpha = a * p3;
				// stars that twinkle, and the moon
				for (int i = 0; i < 70; i++) { double sx = (i * 397.3) % W, sy = (i * 211.7 + i * i * 3.1) % H, tw = 0.25 + 0.25 * JS.sin(t * (1.3 + i % 5 * 0.4) + i); g.fillStyle = L17W(tw * 0.6); circle(sx, sy, 1.2 + (i % 3) * 0.6); }
				double dawn = L17k(bt, 164, 177.5);
				if (dawn > 0) { var gr0 = g.createLinearGradient(0, H, 0, 0); gr0.addColorStop(0, $"rgba(255,150,90,{str(0.3 * dawn)})"); gr0.addColorStop(1, "rgba(255,150,90,0)"); g.fillStyle = gr0; g.fillRect(0, 0, W, H); }
				g.fillStyle = L17W(0.5 - 0.3 * dawn); circle(L17CX, L17CY, 40); g.fillStyle = "rgba(8,10,26,0.95)"; circle(L17CX + 16, L17CY - 8, 36);
			}
			g.globalAlpha = a;
			var gv = g.createRadialGradient(W / 2, H / 2, 280, W / 2, H / 2, 760); gv.addColorStop(0, "rgba(0,0,0,0)"); gv.addColorStop(1, "rgba(0,0,0,0.7)"); g.fillStyle = gv; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L17.json) ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here. An event fires at its start minus Easy's longest warning
	static double L17wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L17ez(double v, double ez) => easy ? v * ez : v;
	static double[] L17Sec(double[] bs) => bs.Select(B).ToArray();

	static readonly int _k17fx = DodgeKind("L17fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L17fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k17stage = DodgeKind("L17stage", e => () => hazards.Add(L17Stage(B(e.b), B(e.num("to")))));
	static L17HandS L17HS(double tail, double len, double w, double hc, double hw) => new L17HandS { tail = tail, len = len, w = L17ez(w, 0.8), hc = hc, hw = L17ez(hw, 1.3) };
	static readonly int _k17hand = DodgeKind("L17hand", e => { double wn = L17wn(e); return () =>
	{
		// Easy: the wedge does not burn
		bool burn = e.flag("burn") && !easy;
		hazards.Add(L17Hand(e.num("a0") * Math.PI / 180, e.num("st") * Math.PI / 180, e.nums("tk"), L17HS(e.num("tail", 40), e.num("len"), e.num("w"), e.num("hc"), e.num("hw")), burn, wn, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17chime = DodgeKind("L17chime", e => { double wn = L17wn(e); return () =>
	{
		var fb = e.nums("fb"); var fk = e.nums("fk");
		// Easy: no second dagger on the same beat
		if (easy) { var keep = Enumerable.Range(0, fb.Length).Where(i => i == 0 || Math.Abs(fb[i] - fb[i - 1]) > 1e-6).ToArray(); fb = keep.Select(i => fb[i]).ToArray(); fk = keep.Select(i => fk[i]).ToArray(); }
		hazards.Add(L17Chime(e.num("R"), L17Sec(fb), fk, L17ez(e.num("sp"), ezV("ezsp", EZ_SP)), e.num("dl"), L17ez(e.num("dw"), 0.8), wn * SPB, B(fb.Length > 0 ? fb[0] - wn : e.b), B(e.num("to"))));
	}; });
	static readonly int _k17gears = DodgeKind("L17gears", e => { double wn = L17wn(e); return () =>
	{
		double sl = easy ? 0.75 : 1; var ib = e.nums("ib"); var ob = e.nums("ob");
		// Easy: every other bite
		if (easy) { ib = ib.Where((v, i) => i % 2 == 0).ToArray(); ob = ob.Where((v, i) => i % 2 == 0).ToArray(); }
		hazards.Add(L17Gears(e.num("ri"), e.num("ro"), e.i("ni"), e.i("no"), e.num("hwi") * Math.PI / 180 * (easy ? 0.8 : 1), e.num("hwo") * Math.PI / 180 * (easy ? 0.8 : 1), e.num("wi") * sl, e.num("wo") * sl,
			e.num("a0i") * Math.PI / 180, e.num("a0o") * Math.PI / 180, e.num("restI"), e.num("restO"), e.num("punI"), e.num("punO"), ib, ob, wn, B(e.num("hit") - 1.5), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17alarm = DodgeKind("L17alarm", e => { double wn = L17wn(e); return () =>
	{
		var P = new L15Path { b = e.nums("kb"), x = e.nums("kx"), y = e.nums("ky") }; var rb = e.nums("rb"); var ga = e.nums("ga");
		if (easy) { var keep = Enumerable.Range(0, rb.Length).Where(i => e.nums("ez").Length <= i || e.nums("ez")[i] == 0).ToArray(); rb = keep.Select(i => rb[i]).ToArray(); ga = keep.Select(i => ga[i]).ToArray(); }
		hazards.Add(L17Alarm(P, L17Sec(rb), ga.Select(v => v * Math.PI / 180).ToArray(), L17ez(e.num("gw"), 1.35) * Math.PI / 180, L17ez(e.num("sp"), ezV("ezsp", EZ_SP)), L17ez(e.num("th"), 0.8), wn * SPB, B(e.num("hit") - 1), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17blink = DodgeKind("L17blink", e => { double wn = L17wn(e); return () =>
		hazards.Add(L17Blink(e.num("hit"), e.num("x"), e.num("y"), L17ez(e.num("sw"), 1.25), L17ez(e.num("sh"), 1.25), e.num("dx") * (easy ? 0.7 : 1), L17ez(e.num("cl"), 1.2), e.num("hold"), e.num("op"), wn)); });
	static readonly int _k17blinds = DodgeKind("L17blinds", e => { double wn = L17wn(e); return () =>
	{
		var fb = e.nums("fb"); var fs = e.nums("fs");
		// Easy: no slats marked "ez" (the extra ones)
		var ez = e.nums("ez"); if (easy && ez.Length == fb.Length) { var keep = Enumerable.Range(0, fb.Length).Where(i => ez[i] == 0).ToArray(); fb = keep.Select(i => fb[i]).ToArray(); fs = keep.Select(i => fs[i]).ToArray(); }
		hazards.Add(L17Blinds(e.flag("vert"), e.i("n"), fb, fs, L17ez(e.num("hold"), 0.8), e.num("fl"), wn, B(e.num("hit") - wn - 1), B(e.num("to"))));
	}; });
	static readonly int _k17doze = DodgeKind("L17doze", e => { double wn = L17wn(e); return () =>
	{
		var P = new L15Path { b = e.nums("kb"), x = e.nums("kx"), y = e.nums("ky"), r = e.nums("kr").Select(v => L17ez(v, 1.25)).ToArray() };
		hazards.Add(L17Doze(P, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17stare = DodgeKind("L17stare", e => { double wn = L17wn(e); return () =>
	{
		var fb = e.nums("fb"); var ez = e.nums("ez");
		if (easy && ez.Length == fb.Length) fb = fb.Where((v, i) => ez[i] == 0).ToArray();
		hazards.Add(L17Stare(e.num("x"), e.num("y"), fb, e.num("lk") * (easy ? 1.3 : 1), L17ez(e.num("bw"), 0.8), e.num("hold"), wn, B(e.num("hit") - 1), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k17trails = DodgeKind("L17trails", e => { double wn = L17wn(e); return () =>
	{
		double sl = easy ? 0.75 : 1; var sb = e.nums("sb"); var ga = e.nums("ga"); var w = e.nums("w");
		if (easy) { var keep = Enumerable.Range(0, sb.Length).Where(i => i % 3 != 2).ToArray(); sb = keep.Select(i => sb[i]).ToArray(); ga = keep.Select(i => ga[i]).ToArray(); w = keep.Select(i => w[i]).ToArray(); }
		hazards.Add(L17Trails(sb, ga.Select(v => v * Math.PI / 180).ToArray(), w.Select(v => v * sl).ToArray(), e.num("r0"), e.num("v") * (easy ? 0.85 : 1), L17ez(e.num("gw"), 1.35) * Math.PI / 180, L17ez(e.num("th"), 0.8), B(e.b), B(e.num("to"))));
	}; });
	static readonly int _k17msgs = DodgeKind("L17msgs", e => { double wn = L17wn(e); return () =>
	{
		var fb = e.nums("fb"); var fl = e.nums("fl"); var fs = e.nums("fs"); var fL = e.nums("fL"); var ez = e.nums("ez");
		if (easy && ez.Length == fb.Length) { var keep = Enumerable.Range(0, fb.Length).Where(i => ez[i] == 0).ToArray(); fb = keep.Select(i => fb[i]).ToArray(); fl = keep.Select(i => fl[i]).ToArray(); fs = keep.Select(i => fs[i]).ToArray(); fL = keep.Select(i => fL[i]).ToArray(); }
		hazards.Add(L17Msgs(e.i("n"), L17ez(e.num("bh"), 0.85), L17Sec(fb), fl, fs, fL, L17ez(e.num("sp"), ezV("ezsp", EZ_SP)), wn * SPB, B(e.num("hit") - wn - 0.5), B(e.num("to"))));
	}; });
	static readonly int _k17race = DodgeKind("L17race", e => { double wn = L17wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.7 : 1; var len = e.nums("len"); var wd = e.nums("wd"); var hc = e.nums("hc"); var hw = e.nums("hw"); var a0 = e.nums("a0");
		var S = new L17HandS[len.Length]; var sp = new L15Spin[len.Length];
		for (int i = 0; i < len.Length; i++)
		{
			S[i] = L17HS(40, len[i], wd[i], hc[i], hw[i]);
			sp[i] = new L15Spin(a0[i] * Math.PI / 180, hit - wn - 1, e.num("to") + 1, e.nums("sb"), e.nums("sw" + i).Select(v => v * sl).ToArray());
		}
		hazards.Add(L17Race(S, sp, B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k17dawn = DodgeKind("L17dawn", e => { double wn = L17wn(e); return () =>
		hazards.Add(L17Dawn(e.nums("hb"), e.nums("hy").Select(v => easy ? H - (H - v) * 0.8 : v).ToArray(), e.i("nr"), e.num("spread") * Math.PI / 180, e.num("amp") * Math.PI / 180 * (easy ? 0.75 : 1), e.num("per"), e.num("ph"), L17ez(e.num("rw"), 0.8),
			B(e.num("hit") - wn), B(e.num("hit")), B(e.num("rays")), B(e.num("to")))); });
}
