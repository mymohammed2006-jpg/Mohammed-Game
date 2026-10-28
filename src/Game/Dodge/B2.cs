using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// B2 "رقصة الموت" (Shirobon - La Danse Macabre, the whole song 0:00-1:56, 128 BPM): the boss mode's second level (his يالله 05:45 10-28,
// plan doc "خطة الزعيم الثاني: رقصة الموت"; 15% harder than B1 by his ask 05:25). The death violinist wakes at midnight and makes
// everyone dance till dawn. One level in three parts with no checkpoint:
//   midnight (b0-128): the clock's hands are beams that tick on the beat, the twelve chimes are rings coming IN with their gap just
//     behind the minute hand (b0-40); then the violin's four strings, plucked on its notes (b40-64); then the bow sweeps the room with
//     one gap every 2 beats while the baton throws notes at the marked one (b64-128);
//   the dance (b128-208): the bass stops and the lights go out (b128-136); three rings of masked dancers turn against each other with two
//     gaps each and a line of dancers sweeps round with its gap in one ring only (a lock to open), pairs waltz along the walls (b136-192);
//     the last waltz: a ring of masks closes in and its one gap follows the marked one, so the team gathers there (b192-208);
//   dawn (b208-248): the music box (a pin on each note in 8 lanes), the clock back at double speed from 6 to 6, the strings and the bow
//     once more; at b248 the dawn comes up and the violinist turns to dust.
// Beat 0 = 0.046 s. Every hazard is a pure function of time once it exists (the last waltz's gap follows the mark as it goes, like B1's
// aimed shots). The aimed events are rnd (the bot learns them only 0.32-0.40 s after they fire). Song notes: B2data.cs.
public static partial class G
{
	const double B2X = W / 2, B2Y = H / 2;
	const double B2K = 1.15;   // the speeds: 15% faster than B1's (his ask 05:25 10-28)
	public const double B2_END = 249;

	// ---------- the clock's hands ----------
	// the minute hand: midnight's ticks (b3-32 one a beat, b32.5-40 two a beat: 46 ticks = two turns, on 12 at b40); dawn's (b209-248 one a
	// beat, two turns from 6 to 6). A tick takes 0.18 beat.
	static readonly double[] B2TI = B2MkTicks();
	static double[] B2MkTicks() { var l = new List<double>(); for (int j = 3; j <= 32; j++) l.Add(j); for (double b = 32.5; b <= 40; b += 0.5) l.Add(b); return l.ToArray(); }
	static double B2Ease(double x) => x <= 0 ? 0 : x < 0.18 ? 1 - JS.pow(1 - x / 0.18, 3) : 1;
	static double B2Min(double bt)
	{
		double n = 0;
		if (bt < 120) { foreach (var T in B2TI) { if (bt <= T) break; n += B2Ease(bt - T); } return -Math.PI / 2 + 4 * Math.PI / 46 * n; }
		for (int j = 209; j <= 248; j++) { if (bt <= j) break; n += B2Ease(bt - j); }
		return Math.PI / 2 + Math.PI / 10 * n;
	}
	// the hour hand: from 11 to 12 by midnight, from 12 to 6 by dawn
	static double B2Hour(double bt) => bt < 120 ? -Math.PI / 2 - Math.PI / 6 * (1 - clamp((bt - 2) / 38, 0, 1)) : -Math.PI / 2 + Math.PI * clamp((bt - 208) / 40, 0, 1);
	// how long a hand is now and whether it is still grey (coming)
	static (double len, bool warn) B2HandLen(double bt, double full)
	{
		if (bt < 0) return (0, false);
		if (bt < 2) return (full, true);
		if (bt < 40) return (full, false);
		if (bt < 41) return (40 + (full - 40) * (41 - bt), false);
		if (bt < 206) return (0, false);
		if (full > 400) full = 520;   // at dawn the minute hand reaches further
		if (bt < 208) return (full, true);
		if (bt < 248) return (full, false);
		return (0, false);
	}
	const double B2ML = 430, B2HL = 250;
	static Hz B2Hands()
	{
		var h = new Hz { kind = "B2Hands", z = -1 };
		h.hit = (p, t) =>
		{
			double bt = BT(t); var m = B2HandLen(bt, B2ML); if (m.warn || m.len <= 40) return false;
			double a = B2Min(bt), c = B2Hour(bt), hl = B2HandLen(bt, B2HL).len;
			if (L15Seg(p.x, p.y, B2X + JS.cos(a) * 40, B2Y + JS.sin(a) * 40, B2X + JS.cos(a) * m.len, B2Y + JS.sin(a) * m.len) < 10 + p.r) return true;
			return hl > 40 && L15Seg(p.x, p.y, B2X + JS.cos(c) * 40, B2Y + JS.sin(c) * 40, B2X + JS.cos(c) * hl, B2Y + JS.sin(c) * hl) < 15 + p.r;
		};
		h.draw = t =>
		{
			double bt = BT(t); var m = B2HandLen(bt, B2ML); if (m.len <= 40) return;
			double a = B2Min(bt), c = B2Hour(bt), hl = B2HandLen(bt, B2HL).len;
			if (m.warn)
			{
				double k = clamp(bt < 100 ? bt / 2 : (bt - 206) / 2, 0, 1);
				L15Dash(B2X + JS.cos(a) * 40, B2Y + JS.sin(a) * 40, B2X + JS.cos(a) * m.len, B2Y + JS.sin(a) * m.len, L15G(0.2 + 0.5 * k), 3);
				L15Dash(B2X + JS.cos(c) * 40, B2Y + JS.sin(c) * 40, B2X + JS.cos(c) * hl, B2Y + JS.sin(c) * hl, L15G(0.2 + 0.5 * k), 5);
				return;
			}
			B2HandDraw(c, hl, 30); B2HandDraw(a, m.len, 20);
		};
		return h;
	}
	// a hand: a pink bar from the middle with a point at its end
	static void B2HandDraw(double a, double len, double w)
	{
		if (len <= 40) return;
		g.save(); g.translate(B2X, B2Y); g.rotate(a); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14;
		g.beginPath(); g.moveTo(40, -w / 2); g.lineTo(len - w, -w / 2); g.lineTo(len, 0); g.lineTo(len - w, w / 2); g.lineTo(40, w / 2); g.closePath(); g.fill(); g.shadowBlur = 0;
		g.fillStyle = "rgba(60,0,20,0.45)"; for (double s = 70; s < len - 2 * w; s += 40) g.fillRect(s, -w / 2, 4, w);
		g.restore();
	}

	// ---------- the room: a dark ballroom; the clock's face at midnight and at dawn (grey, it does not hurt); the dawn's light ----------
	static Hz B2Stage()
	{
		var h = new Hz { kind = "B2Stage", z = -1 };
		h.draw = t =>
		{
			double bt = BT(t);
			var gr = g.createRadialGradient(B2X, B2Y, 60, B2X, B2Y, 780); gr.addColorStop(0, "#160822"); gr.addColorStop(1, "#040108"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the dance floor: big diamonds that breathe with the beat
			g.strokeStyle = $"rgba(170,110,220,{str(0.04 + 0.05 * pulse)})"; g.lineWidth = 1; g.beginPath();
			for (double s = -H; s <= W + H; s += 120) { g.moveTo(s, 0); g.lineTo(s + H, H); g.moveTo(s, H); g.lineTo(s + H, 0); }
			g.stroke();
			// the clock's face
			double cf = bt < 126 ? clamp(1 - (bt - 124) / 2, 0, 1) : clamp((bt - 204) / 2, 0, 1);
			if (bt >= 128 && bt < 204) cf = 0;
			if (cf > 0)
			{
				g.strokeStyle = $"rgba(220,200,240,{str(0.14 * cf)})"; g.lineWidth = 3; g.beginPath(); g.arc(B2X, B2Y, 330, 0, TAU); g.stroke();
				g.lineWidth = 2; g.beginPath(); g.arc(B2X, B2Y, 345, 0, TAU); g.stroke();
				for (int i = 0; i < 12; i++)
				{
					double q = i * TAU / 12 - Math.PI / 2, r0 = i % 3 == 0 ? 290 : 308;
					g.strokeStyle = $"rgba(230,215,250,{str((i % 3 == 0 ? 0.3 : 0.18) * cf)})"; g.lineWidth = i % 3 == 0 ? 8 : 4; g.beginPath();
					g.moveTo(B2X + JS.cos(q) * r0, B2Y + JS.sin(q) * r0); g.lineTo(B2X + JS.cos(q) * 326, B2Y + JS.sin(q) * 326); g.stroke();
				}
			}
			// the dawn (b238-249): a warm light from the bottom
			if (bt > 238)
			{
				double k = clamp((bt - 238) / 10, 0, 1); var dg = g.createLinearGradient(0, H, 0, H - 520 * k - 40);
				dg.addColorStop(0, $"rgba(255,214,150,{str(0.38 * k)})"); dg.addColorStop(1, "rgba(255,214,150,0)"); g.fillStyle = dg; g.fillRect(0, 0, W, H);
			}
			var vg = g.createRadialGradient(B2X, B2Y, 300, B2X, B2Y, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
		};
		return h;
	}
	// the dark (b128-137): the lights go out when the bass stops; it is a veil, so all that hurts is drawn over it
	static Hz B2Dark()
	{
		var h = new Hz { kind = "B2Dark", z = 1, veil = true };
		h.update = (t, dt) => { if (BT(t) > 137.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = bt < 128 ? 0 : bt < 128.3 ? (bt - 128) / 0.3 : bt < 135.5 ? 1 : clamp(1 - (bt - 135.5) / 1.5, 0, 1); if (a <= 0) return;
			g.fillStyle = $"rgba(0,0,0,{str(0.8 * a)})"; g.fillRect(0, 0, W, H);
		};
		return h;
	}

	// ---------- the violinist: a dark robe with a violin, a pink mask (the mask hurts) ----------
	static double B2MaskR(double bt) => bt < 0 ? 0 : bt < 2 ? 28 * bt / 2 : bt < 248 ? 28 : 0;
	static Hz B2Boss()
	{
		var h = new Hz { kind = "B2Boss" };
		h.hit = (p, t) => { double r = B2MaskR(BT(t)); return r > 0 && hypot(p.x - B2X, p.y - B2Y) < r + p.r; };
		h.draw = t => { double bt = BT(t), r = B2MaskR(bt); if (r > 0) B2BossDraw(B2X, B2Y, r, bt); };
		return h;
	}
	public static void B2BossDraw(double x, double y, double r, double bt)
	{
		double s = r / 28, pu = 1 + 0.05 * pulse;
		g.save(); g.translate(x, y); g.scale(s, s);
		// the robe and the violin (dark, they don't hurt)
		g.fillStyle = "rgba(34,12,48,0.92)"; g.beginPath(); g.moveTo(-26, 14); g.lineTo(26, 14); g.lineTo(52, 104); g.lineTo(-52, 104); g.closePath(); g.fill();
		g.save(); g.translate(30, 30); g.rotate(-0.6 + 0.08 * JS.sin(bt * Math.PI)); g.fillStyle = "rgba(70,30,40,0.95)";
		g.beginPath(); g.ellipse(0, 0, 14, 26, 0, 0, TAU); g.fill(); g.fillRect(-3, -58, 6, 34); g.strokeStyle = "rgba(255,200,220,0.35)"; g.lineWidth = 1; g.beginPath(); g.moveTo(-2, -56); g.lineTo(-2, 20); g.moveTo(2, -56); g.lineTo(2, 20); g.stroke(); g.restore();
		g.strokeStyle = "rgba(90,50,70,0.9)"; g.lineWidth = 3; g.beginPath(); double ba = 0.25 + 0.35 * JS.sin(bt * Math.PI / 2); g.moveTo(-40 + 20 * JS.cos(ba), 54); g.lineTo(60 * JS.cos(ba) + 10, 54 - 60 * JS.sin(ba)); g.stroke();
		// the mask
		g.shadowColor = PINK; g.shadowBlur = 22 + 12 * pulse;
		var gr = g.createRadialGradient(0, -6, 4, 0, 0, 28 * pu); gr.addColorStop(0, "#ffd0e0"); gr.addColorStop(0.45, PINK); gr.addColorStop(1, "#b0123f"); g.fillStyle = gr;
		g.beginPath(); g.moveTo(0, -30 * pu); g.bezierCurveTo(24 * pu, -30 * pu, 30 * pu, -6 * pu, 22 * pu, 12 * pu); g.bezierCurveTo(14 * pu, 30 * pu, -14 * pu, 30 * pu, -22 * pu, 12 * pu); g.bezierCurveTo(-30 * pu, -6 * pu, -24 * pu, -30 * pu, 0, -30 * pu); g.closePath(); g.fill(); g.shadowBlur = 0;
		g.fillStyle = "rgba(30,0,12,0.9)";
		foreach (double ex in new[] { -10.0, 10.0 }) { g.beginPath(); g.ellipse(ex, -6, 7, 4.5, ex < 0 ? 0.35 : -0.35, 0, TAU); g.fill(); }
		g.beginPath(); g.moveTo(-9, 12); g.quadraticCurveTo(0, 18 + 3 * pulse, 9, 12); g.lineWidth = 3; g.strokeStyle = "rgba(30,0,12,0.9)"; g.stroke();
		g.fillStyle = "#fff"; foreach (double ex in new[] { -10.0, 10.0 }) circle(ex, -6, 1.6 + 1.2 * pulse);
		g.restore();
	}

	// ---------- a note: from (x0,y0) toward a at v px/s from t0; its path shows wn s before. The head and the stem hurt ----------
	static Hz B2Note(double x0, double y0, double a, double v, double t0, double wn, double life = 5)
	{
		var h = new Hz { kind = "B2Note" }; double ca = JS.cos(a), sa = JS.sin(a); const double R = 9;
		h.update = (t, dt) => { if (t > t0 + life) h.dead = true; };
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double d = v * (t - t0), x = x0 + ca * d, y = y0 + sa * d;
			if (hypot(p.x - x, p.y - y) < R + p.r) return true;
			return L15Seg(p.x, p.y, x + 7, y, x + 7, y - 24) < 2 + p.r || L15Seg(p.x, p.y, x + 7, y - 24, x + 16, y - 10) < 2 + p.r;
		};
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - (t0 - wn)) / wn, 0, 1); if (k <= 0) return; L15Dash(x0, y0, x0 + ca * 320 * k, y0 + sa * 320 * k, L15G(0.18 + 0.4 * k), 2); return; }
			double d = v * (t - t0), x = x0 + ca * d, y = y0 + sa * d; if (x < -40 || x > W + 40 || y < -40 || y > H + 50) return;
			B2NoteShape(x, y, 1);
		};
		return h;
	}
	static void B2NoteShape(double x, double y, double a)
	{
		g.fillStyle = L15P(a); g.strokeStyle = L15P(a); g.shadowColor = PINK; g.shadowBlur = 10;
		g.beginPath(); g.ellipse(x, y, 9.5, 7.5, -0.4, 0, TAU); g.fill();
		g.lineWidth = 3.5; g.beginPath(); g.moveTo(x + 7, y); g.lineTo(x + 7, y - 24); g.quadraticCurveTo(x + 14, y - 18, x + 16, y - 10); g.stroke(); g.shadowBlur = 0;
	}

	// ---------- the chimes: a ring coming in from the edge (740 px) at v px/s, a gap of half width gh around ga ----------
	static Hz B2Chime(double T, double v, double ga, double gh, double th = 26)
	{
		var h = new Hz { kind = "B2Chime" }; const double R0 = 740;
		double Rr(double t) => R0 - v * (t - T);
		h.update = (t, dt) => { if (t > T && Rr(t) < 20) h.dead = true; if (!h.ex && t >= T) { h.ex = true; sfx(tt => sTone(tt, 196, 0.25, "square", 0.05)); } };
		h.hit = (p, t) =>
		{
			if (t < T) return false; double r = Rr(t); if (r < 20) return false; double d = hypot(p.x - B2X, p.y - B2Y); if (Math.Abs(d - r) >= th / 2 + p.r) return false;
			return Math.Abs(B1Ang(JS.atan2(p.y - B2Y, p.x - B2X) - ga)) > gh;
		};
		h.draw = t =>
		{
			if (t < T)
			{
				double k = clamp((t - (T - SPB)) / SPB, 0, 1); if (k <= 0) return;
				g.strokeStyle = L15G(0.15 + 0.45 * k); g.lineWidth = 3; g.beginPath(); g.arc(B2X, B2Y, R0 - 20 * k, ga + gh, ga + TAU - gh); g.stroke();
				L15Dash(B2X + JS.cos(ga) * 60, B2Y + JS.sin(ga) * 60, B2X + JS.cos(ga) * 700, B2Y + JS.sin(ga) * 700, L15G(0.12 + 0.25 * k), 2); return;
			}
			double r = Rr(t); if (r < 20) return;
			g.strokeStyle = PINK; g.lineWidth = th; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.arc(B2X, B2Y, r, ga + gh, ga + TAU - gh); g.stroke(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 3; g.beginPath();
			foreach (var e in new[] { ga + gh, ga - gh }) { g.moveTo(B2X + JS.cos(e) * (r - th / 2), B2Y + JS.sin(e) * (r - th / 2)); g.lineTo(B2X + JS.cos(e) * (r + th / 2), B2Y + JS.sin(e) * (r + th / 2)); }
			g.stroke();
		};
		return h;
	}

	// ---------- the strings: four lines across the room (165 px apart) that turn slowly; grey while still, a plucked one shakes pink ----------
	static readonly L15Spin B2S1 = new(0.35, 36, 130, new double[] { 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120 }, new double[] { 0.035, -0.035, 0.045, -0.045, 0.04, -0.04, 0.05, -0.05, 0.05, -0.05, 0.05 });
	static readonly L15Spin B2S2 = new(-0.3, 222, 250, new double[] { 226, 230, 234, 238, 242, 246 }, new double[] { 0.05, -0.05, 0.06, -0.06, 0.06, -0.06 });
	static double B2StrA(double bt) => bt < 170 ? B2S1.At(bt) : B2S2.At(bt);
	static double B2StrD(int i) => (i - 1.5) * 165;
	static bool B2StrOn(double bt) => (bt >= 38 && bt < 128) || (bt >= 224 && bt < 248);
	static Hz B2Strings()
	{
		var h = new Hz { kind = "B2Strings", z = -1 };
		h.draw = t =>
		{
			double bt = BT(t); if (!B2StrOn(bt)) return; double a = bt < 170 ? clamp(Math.Min((bt - 38) / 2, 128 - bt), 0, 1) : clamp(Math.Min((bt - 224) / 2, 248 - bt), 0, 1);
			double f = B2StrA(bt), ux = JS.cos(f), uy = JS.sin(f);
			g.strokeStyle = L15G(0.32 * a); g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < 4; i++) { double d = B2StrD(i), cx = B2X - uy * d, cy = B2Y + ux * d; g.moveTo(cx - ux * 900, cy - uy * 900); g.lineTo(cx + ux * 900, cy + uy * 900); }
			g.stroke();
		};
		return h;
	}
	// string i plucked at T (beats): its band (A px each side) shows grey a beat before, then shakes pink for half a beat
	static Hz B2Pluck(int i, double Tb, double A = 56)
	{
		var h = new Hz { kind = "B2Pluck", z = -1 }; double T = B(Tb), T1 = B(Tb + 0.5), d = B2StrD(i);
		double Amp(double t) => t < T ? 0 : A * (1 - (t - T) / (T1 - T));
		h.update = (t, dt) => { if (t > T1 + 0.2) h.dead = true; };
		h.hit = (p, t) =>
		{
			double am = Amp(t); if (am < 10) return false; double f = B2StrA(BT(t));
			double perp = -(p.x - B2X) * JS.sin(f) + (p.y - B2Y) * JS.cos(f); return Math.Abs(perp - d) < am + p.r;
		};
		h.draw = t =>
		{
			double f = B2StrA(BT(t)), ux = JS.cos(f), uy = JS.sin(f), nx = -uy, ny = ux, cx = B2X + nx * d, cy = B2Y + ny * d;
			if (t < T)
			{
				double k = clamp((t - (T - SPB)) / SPB, 0, 1); if (k <= 0) return;
				foreach (int s in new[] { -1, 1 }) L15Dash(cx + nx * A * s - ux * 900, cy + ny * A * s - uy * 900, cx + nx * A * s + ux * 900, cy + ny * A * s + uy * 900, L15G(0.12 + 0.35 * k), 2);
				g.strokeStyle = L15G(0.3 + 0.4 * k); g.lineWidth = 3; g.beginPath(); g.moveTo(cx - ux * 900, cy - uy * 900); g.lineTo(cx + ux * 900, cy + uy * 900); g.stroke(); return;
			}
			double am = Amp(t); if (am < 10) return; double q = am / A;
			g.save(); g.beginPath(); g.moveTo(cx + nx * am - ux * 900, cy + ny * am - uy * 900); g.lineTo(cx + nx * am + ux * 900, cy + ny * am + uy * 900);
			g.lineTo(cx - nx * am + ux * 900, cy - ny * am + uy * 900); g.lineTo(cx - nx * am - ux * 900, cy - ny * am - uy * 900); g.closePath(); g.fillStyle = L15P(0.28 + 0.2 * q); g.fill(); g.restore();
			g.strokeStyle = PINK; g.lineWidth = 4; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); double ph = t * 40;
			for (double s = -900; s <= 900; s += 18) { double o = am * JS.sin(s * 0.035 + ph); double x = cx + ux * s + nx * o, y = cy + uy * s + ny * o; if (s == -900) g.moveTo(x, y); else g.lineTo(x, y); }
			g.stroke(); g.shadowBlur = 0;
		};
		return h;
	}

	// ---------- the bow: a bar across the whole room that sweeps it in a beat with one gap ----------
	// (it sweeps the room in 1 beat) dir 0: left to right (the gap is a height gc), 1: top to bottom (the gap is an x), 2: right to left, 3: bottom to top
	static Hz B2Bow(int dir, double Tb, double gc, double gh = 58, double dur = 1.0)
	{
		var h = new Hz { kind = "B2Bow", z = -1 }; const double TH = 40, TRAIL = 110; bool vert = dir % 2 == 0; double len = vert ? W : H;
		double Pos(double bt) { double k = clamp((bt - Tb) / dur, 0, 1); double s = -TH + (len + 2 * TH) * k; return dir < 2 ? s : len - s; }
		h.update = (t, dt) => { double bt = BT(t); if (bt > Tb + dur + 0.2) h.dead = true; if (!h.ex && bt >= Tb) { h.ex = true; sfx(tt => sNoise(tt, 0.3, "bandpass", 1200, 0.08)); } };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < Tb || bt >= Tb + dur) return false; double s = Pos(bt), a = vert ? p.x : p.y, o = vert ? p.y : p.x, back = (dir < 2 ? 1 : -1) * (s - a);
			if (back <= -(TH / 2 + p.r) || back >= TH / 2 + TRAIL + p.r) return false; return Math.Abs(o - gc) > gh - p.r;
		};
		h.draw = t =>
		{
			double bt = BT(t);
			void bar(double s, string col, bool live)
			{
				g.fillStyle = col; if (live) { g.shadowColor = PINK; g.shadowBlur = 16; }
				if (vert) { g.fillRect(s - TH / 2, -10, TH, gc - gh + 10); g.fillRect(s - TH / 2, gc + gh, TH, H + 10 - gc - gh); }
				else { g.fillRect(-10, s - TH / 2, gc - gh + 10, TH); g.fillRect(gc + gh, s - TH / 2, W + 10 - gc - gh, TH); }
				g.shadowBlur = 0;
				if (live)
				{
					g.strokeStyle = "rgba(255,235,245,0.75)"; g.lineWidth = 2; g.beginPath();
					if (vert) { g.moveTo(s, -10); g.lineTo(s, gc - gh); g.moveTo(s, gc + gh); g.lineTo(s, H + 10); g.moveTo(s - TH / 2, gc - gh); g.lineTo(s + TH / 2, gc - gh); g.moveTo(s - TH / 2, gc + gh); g.lineTo(s + TH / 2, gc + gh); }
					else { g.moveTo(-10, s); g.lineTo(gc - gh, s); g.moveTo(gc + gh, s); g.lineTo(W + 10, s); g.moveTo(gc - gh, s - TH / 2); g.lineTo(gc - gh, s + TH / 2); g.moveTo(gc + gh, s - TH / 2); g.lineTo(gc + gh, s + TH / 2); }
					g.stroke();
				}
			}
			if (bt < Tb)
			{
				double k = clamp((bt - (Tb - 2)) / 2, 0, 1); if (k <= 0) return;
				// the gap's two edges along the whole way, and the bow waiting at its edge
				if (vert) { L15Dash(0, gc - gh, W, gc - gh, L15G(0.15 + 0.4 * k), 2); L15Dash(0, gc + gh, W, gc + gh, L15G(0.15 + 0.4 * k), 2); }
				else { L15Dash(gc - gh, 0, gc - gh, H, L15G(0.15 + 0.4 * k), 2); L15Dash(gc + gh, 0, gc + gh, H, L15G(0.15 + 0.4 * k), 2); }
				bar(dir < 2 ? 14 : len - 14, L15G(0.2 + 0.4 * k), false); return;
			}
			if (bt >= Tb + dur) return;
			// the shaking hair behind the bow (it hurts too)
			{
				double s = Pos(bt), sg = dir < 2 ? -1 : 1, e = s + sg * (TH / 2 + TRAIL);
				var lg = vert ? g.createLinearGradient(s, 0, e, 0) : g.createLinearGradient(0, s, 0, e); lg.addColorStop(0, L15P(0.55)); lg.addColorStop(1, L15P(0.22)); g.fillStyle = lg;
				double lo = Math.Min(s, e), hi = Math.Max(s, e);
				if (vert) { g.fillRect(lo, -10, hi - lo, gc - gh + 10); g.fillRect(lo, gc + gh, hi - lo, H + 10 - gc - gh); }
				else { g.fillRect(-10, lo, gc - gh + 10, hi - lo); g.fillRect(gc + gh, lo, W + 10 - gc - gh, hi - lo); }
			}
			bar(Pos(bt), PINK, true);
		};
		return h;
	}

	// ---------- the dancers' rings (b132-192): three rings of masks around the violinist, two gaps each, turning against each other ----------
	static readonly double[] B2RR = { 150, 245, 340 };
	static readonly L15Spin[] B2RS = B2MkRingSpins();
	static L15Spin[] B2MkRingSpins()
	{
		double[] mag = { 0.46, 0.35, 0.27 }; var sb = new List<double>(); for (double b = 136; b < 192; b += 4) sb.Add(b);
		var r = new L15Spin[3];
		for (int k = 0; k < 3; k++) { var sw = new List<double>(); for (int i = 0; i < sb.Count; i++) sw.Add(mag[k] * (k % 2 == 0 ? 1 : -1) * (i % 2 == 0 ? 1 : -1)); r[k] = new L15Spin(k * 0.9, 132, 194, sb.ToArray(), sw.ToArray()); }
		return r;
	}
	static double B2RingR(int k, double bt) => B2RR[k] + 8 * JS.exp(-(bt - Math.Floor(bt)) * 5);
	static double B2RingGh(int k) => 46 / B2RR[k];
	static bool B2InGap(int k, double bt, double a) { double g0 = B2RS[k].At(bt); return Math.Abs(B1Ang(a - g0)) < B2RingGh(k) || Math.Abs(B1Ang(a - g0 - Math.PI)) < B2RingGh(k); }
	static Hz B2Rings()
	{
		var h = new Hz { kind = "B2Rings" };
		h.update = (t, dt) => { if (BT(t) > 192.2) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < 136 || bt >= 190) return false; double d = hypot(p.x - B2X, p.y - B2Y), a = JS.atan2(p.y - B2Y, p.x - B2X);
			for (int k = 0; k < 3; k++) if (Math.Abs(d - B2RingR(k, bt)) < 15 + p.r && !B2InGap(k, bt, a)) return true;
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < 132 || bt >= 192) return; bool live = bt >= 136 && bt < 190;
			double al = bt < 136 ? clamp((bt - 132) / 2, 0, 1) : bt < 190 ? 1 : clamp(1 - (bt - 190) / 2, 0, 1);
			for (int k = 0; k < 3; k++)
			{
				double R = B2RingR(k, bt), gh = B2RingGh(k), g0 = B2RS[k].At(bt); int n = (int)Math.Floor(TAU * R / 38);
				if (live) { g.strokeStyle = L15P(0.5); g.lineWidth = 30; foreach (double c in new[] { g0, g0 + Math.PI }) { g.beginPath(); g.arc(B2X, B2Y, R, c + gh, c + Math.PI - gh); g.stroke(); } }
				for (int i = 0; i < n; i++)
				{
					double a = g0 + i * TAU / n; if (B2InGap(k, bt, a)) continue;
					double x = B2X + JS.cos(a) * R, y = B2Y + JS.sin(a) * R;
					if (live) B2MaskSmall(x, y, 13, a + Math.PI / 2, 1); else { g.fillStyle = L15G(0.45 * al); circle(x, y, 11); }
				}
				if (live)
				{
					g.strokeStyle = "rgba(255,255,255,0.75)"; g.lineWidth = 3; g.beginPath();
					foreach (double c in new[] { g0, g0 + Math.PI }) foreach (double e in new[] { c + gh, c - gh }) { g.moveTo(B2X + JS.cos(e) * (R - 15), B2Y + JS.sin(e) * (R - 15)); g.lineTo(B2X + JS.cos(e) * (R + 15), B2Y + JS.sin(e) * (R + 15)); }
					g.stroke();
				}
				else if (bt >= 190) { g.globalAlpha = al; for (int i = 0; i < n; i += 2) { double a = g0 + i * TAU / n; B2MaskSmall(B2X + JS.cos(a) * R, B2Y + JS.sin(a) * R, 13, a, 1); } g.globalAlpha = 1; }
			}
		};
		return h;
	}
	// a small dancer's mask (pink, it hurts where it is used)
	static void B2MaskSmall(double x, double y, double r, double a, double al)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = L15P(al); g.shadowColor = PINK; g.shadowBlur = 8;
		g.beginPath(); g.ellipse(0, 0, r, r * 1.12, 0, 0, TAU); g.fill(); g.shadowBlur = 0;
		g.fillStyle = "rgba(40,0,16,0.85)"; g.beginPath(); g.ellipse(-r * 0.38, -r * 0.15, r * 0.26, r * 0.16, 0.3, 0, TAU); g.fill(); g.beginPath(); g.ellipse(r * 0.38, -r * 0.15, r * 0.26, r * 0.16, -0.3, 0, TAU); g.fill();
		g.restore();
	}
	// the free room between the rings (zone 0 inside the first ring .. 3 outside the last)
	static (double a, double b) B2Zone(int z) => z switch { 0 => (60, 120), 1 => (180, 216), 2 => (275, 311), _ => (370, 900) };
	// a line of dancers from the violinist to the wall that turns round (w rad/beat from a0 at b0) till b1; it can pass only where its gap
	// is (one zone); twin: also the opposite line. It shows grey a beat before.
	static Hz B2Spoke(double b0, double b1, double a0, double w, int zone, bool twin)
	{
		var h = new Hz { kind = "B2Spoke" }; var Z = B2Zone(zone);
		double Ang(double bt) => a0 + w * (Math.Max(bt, b0) - b0);
		h.update = (t, dt) => { if (BT(t) > b1 + 0.1) h.dead = true; };
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			for (int s = 0; s < (twin ? 2 : 1); s++)
			{
				double a = Ang(bt) + s * Math.PI, ux = JS.cos(a), uy = JS.sin(a), u = (p.x - B2X) * ux + (p.y - B2Y) * uy; if (u < 40 || u > 800) continue;
				if (Math.Abs(-(p.x - B2X) * uy + (p.y - B2Y) * ux) >= 13 + p.r) continue;
				if (u - p.r > Z.a && u + p.r < Z.b) continue;   // in the gap
				return true;
			}
			return false;
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt >= b1) return;
			for (int s = 0; s < (twin ? 2 : 1); s++)
			{
				double a = Ang(bt) + s * Math.PI, ux = JS.cos(a), uy = JS.sin(a);
				if (bt < b0)
				{
					double k = clamp(bt - (b0 - 1), 0, 1);
					L15Dash(B2X + ux * 40, B2Y + uy * 40, B2X + ux * Z.a, B2Y + uy * Z.a, L15G(0.2 + 0.5 * k), 5);
					if (Z.b < 800) L15Dash(B2X + ux * Z.b, B2Y + uy * Z.b, B2X + ux * 800, B2Y + uy * 800, L15G(0.2 + 0.5 * k), 5);
					continue;
				}
				g.strokeStyle = L15P(0.5); g.lineWidth = 26; g.beginPath(); g.moveTo(B2X + ux * 40, B2Y + uy * 40); g.lineTo(B2X + ux * Z.a, B2Y + uy * Z.a); if (Z.b < 800) { g.moveTo(B2X + ux * Z.b, B2Y + uy * Z.b); g.lineTo(B2X + ux * 800, B2Y + uy * 800); } g.stroke();
				for (double r = 52; r < 800; r += 30) { if (r > Z.a - 12 && r < Z.b + 12) continue; B2MaskSmall(B2X + ux * r, B2Y + uy * r, 12, a + Math.PI / 2, 1); }
				g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 3; g.beginPath();
				foreach (double r in new[] { Z.a, Z.b }) { if (r > 800) continue; g.moveTo(B2X + ux * r - uy * 14, B2Y + uy * r + ux * 14); g.lineTo(B2X + ux * r + uy * 14, B2Y + uy * r - ux * 14); }
				g.stroke();
			}
		};
		return h;
	}
	// a pair of dancers turning round each other while their middle goes along a wall, from (x0,y0) to (x1,y1) from T at v px/s
	static Hz B2Pair(double x0, double y0, double x1, double y1, double T, double v)
	{
		var h = new Hz { kind = "B2Pair" }; double L = hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / L, uy = (y1 - y0) / L, wn = 1.5 * SPB;
		(double ax, double ay, double bx, double by) At(double t)
		{
			double d = v * (t - T), cx = x0 + ux * d, cy = y0 + uy * d, q = (t - T) / SPB * 2.4;
			return (cx + JS.cos(q) * 34, cy + JS.sin(q) * 34, cx - JS.cos(q) * 34, cy - JS.sin(q) * 34);
		}
		h.update = (t, dt) => { if (t > T + L / v + 0.3) h.dead = true; };
		h.hit = (p, t) => { if (t < T) return false; var q = At(t); return hypot(p.x - q.ax, p.y - q.ay) < 15 + p.r || hypot(p.x - q.bx, p.y - q.by) < 15 + p.r; };
		h.draw = t =>
		{
			if (t < T) { double k = clamp((t - (T - wn)) / wn, 0, 1); if (k <= 0) return; L15Dash(x0, y0, x1, y1, L15G(0.12 + 0.35 * k), 3); g.strokeStyle = L15G(0.2 + 0.4 * k); g.lineWidth = 2; g.beginPath(); g.arc(x0 + ux * 60, y0 + uy * 60, 34, 0, TAU); g.stroke(); return; }
			var q = At(t); g.strokeStyle = "rgba(230,215,250,0.5)"; g.lineWidth = 3; g.beginPath(); g.moveTo(q.ax, q.ay); g.lineTo(q.bx, q.by); g.stroke();
			B2MaskSmall(q.ax, q.ay, 14, t * 3, 1); B2MaskSmall(q.bx, q.by, 14, t * 3 + Math.PI, 1);
		};
		return h;
	}
	// the last waltz (b0-b1): a ring of masks closing in from 760 px to 40; its one gap goes after the marked one (1.1 rad/s at most) and sways round it
	static Hz B2Waltz(double b0, double b1)
	{
		var h = new Hz { kind = "B2Waltz" }; double gf = double.NaN, ga = double.NaN; const double TH = 56, GA = 34;
		double Rr(double bt) => 760 - 720 * clamp((bt - b0) / (b1 - b0), 0, 1);
		double Gh(double bt) => Math.Min(1.2, GA / Math.Max(40, Rr(bt)));
		h.update = (t, dt) =>
		{
			double bt = BT(t); if (bt > b1 + 0.1) { h.dead = true; return; }
			var m = B1Aim(bt); double want = JS.atan2(m.y - B2Y, m.x - B2X);
			if (double.IsNaN(gf)) gf = want; else { double dd = B1Ang(want - gf), mx = 1.1 * dt; gf += Math.Max(-mx, Math.Min(mx, dd)); }
			ga = gf + 0.6 * JS.sin((bt - b0) * Math.PI);   // the sway: one side and back every 2 beats
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1 || double.IsNaN(ga)) return false; double r = Rr(bt), d = hypot(p.x - B2X, p.y - B2Y); if (Math.Abs(d - r) >= TH / 2 + p.r) return false;
			return Math.Abs(B1Ang(JS.atan2(p.y - B2Y, p.x - B2X) - ga)) > Gh(bt);
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt >= b1 || double.IsNaN(ga)) return; double r = Rr(bt), gh = Gh(bt);
			if (bt < b0) { double k = clamp((bt - (b0 - 1)) / 1, 0, 1); g.strokeStyle = L15G(0.15 + 0.45 * k); g.lineWidth = 4; g.beginPath(); g.arc(B2X, B2Y, 700, ga + gh, ga + TAU - gh); g.stroke(); return; }
			g.strokeStyle = L15P(0.45); g.lineWidth = TH; g.beginPath(); g.arc(B2X, B2Y, r, ga + gh, ga + TAU - gh); g.stroke();
			// two rows of masks
			foreach (double rr2 in new[] { r - 14, r + 14 })
			{
				if (rr2 < 12) continue; int n = (int)Math.Floor(TAU * rr2 / 30);
				for (int i = 0; i < n; i++) { double a = ga + Math.PI + (i - n / 2.0) * TAU / n; if (Math.Abs(B1Ang(a - ga)) < gh + 0.02) continue; B2MaskSmall(B2X + JS.cos(a) * rr2, B2Y + JS.sin(a) * rr2, 12, a + Math.PI / 2, 1); }
			}
			g.strokeStyle = "rgba(255,255,255,0.85)"; g.lineWidth = 3; g.beginPath();
			foreach (var e in new[] { ga + gh, ga - gh }) { g.moveTo(B2X + JS.cos(e) * (r - TH / 2), B2Y + JS.sin(e) * (r - TH / 2)); g.lineTo(B2X + JS.cos(e) * (r + TH / 2), B2Y + JS.sin(e) * (r + TH / 2)); }
			g.stroke();
		};
		return h;
	}

	// ---------- the music box (b208-248): pins from the right in 8 lanes, each passes the comb (x 900) on its note ----------
	const double B2CX = 900, B2PV = 840;
	static double B2LaneY(int l, double b = 0) => 45 + 90 * l + (b >= 208 && Math.Floor((b - 208) / 8) % 2 == 1 ? 45 : 0);
	static Hz B2Comb()
	{
		var h = new Hz { kind = "B2Comb", z = -1 };
		h.update = (t, dt) => { if (BT(t) > 249) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < 205 || bt >= 249) return; double a = clamp(Math.Min((bt - 205) / 2, 249 - bt), 0, 1);
			g.fillStyle = L15G(0.28 * a); g.fillRect(B2CX + 30, 0, 10, H);
			for (int l = 0; l < 8; l++) { double y = B2LaneY(l, bt); g.fillRect(B2CX - 6, y - 4, 40, 8); }
		};
		return h;
	}
	static Hz B2Pin(int lane, double Tb)
	{
		var h = new Hz { kind = "B2Pin" }; double T = B(Tb), y = B2LaneY(lane, Tb); const double PW = 110, PH = 50;
		double X(double t) => B2CX - B2PV * (t - T);
		double t0 = T - (W + PW - B2CX) / B2PV;   // when it comes in at the right
		h.update = (t, dt) => { if (X(t) < -PW) h.dead = true; if (!h.ex && t >= T) { h.ex = true; burst(B2CX, y, "#ffffff", 4, 160); } };
		h.hit = (p, t) => { if (t < t0) return false; double x = X(t); return Math.Abs(p.x - x) < PW / 2 + p.r && Math.Abs(p.y - y) < PH / 2 + p.r; };
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - (t0 - 0.75 * SPB)) / (0.75 * SPB), 0, 1); if (k <= 0) return; L15Dash(W, y, W - 420 * k, y, L15G(0.15 + 0.4 * k), 3); return; }
			double x = X(t); if (x < -PW || x > W + PW) return;
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; rr(x - PW / 2, y - PH / 2, PW, PH, 10); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(60,0,20,0.4)"; g.fillRect(x - PW / 2 + 10, y - 2, PW - 20, 4);
		};
		return h;
	}

	// ---------- online: a downed square (seat s) sits in a pink ring with a gap that turns; the violinist throws notes at it every 2 beats ----------
	public static Hz B2DownRing(int s, double t0)
	{
		var h = new Hz { kind = "B2Down" }; double nextB = Math.Ceiling(BT(t0) + 1);
		bool on() => net?.boss == true && net.bdown[s] && state == "play";
		(double x, double y) at() => net.bdpos[s];
		h.update = (t, dt) =>
		{
			if (!on()) { h.dead = true; return; }
			double bt = BT(t);
			if (bt >= nextB) { nextB += 2; var c = at(); if (B2MaskR(bt) > 0) hazards.Add(B2Note(B2X, B2Y, JS.atan2(c.y - B2Y, c.x - B2X), 380 * B2K, B(bt + 1), SPB)); }
		};
		h.hit = (p, t) =>
		{
			if (t < t0 + 0.8 || !on() || s == bnMe || bnMeDown) return false; var c = at(); double d = hypot(p.x - c.x, p.y - c.y);
			if (Math.Abs(d - 44) >= 5 + p.r) return false; return Math.Abs(B1Ang(JS.atan2(p.y - c.y, p.x - c.x) - t * 1.6)) > 0.55;
		};
		h.draw = t =>
		{
			if (!on()) return; var c = at(); double k = clamp((t - t0) / 0.8, 0, 1), ga = t * 1.6;
			g.strokeStyle = k < 1 ? L15G(0.3 + 0.4 * k) : PINK; g.lineWidth = 10; if (k >= 1) { g.shadowColor = PINK; g.shadowBlur = 10; }
			g.beginPath(); g.arc(c.x, c.y, 44, ga + 0.55, ga + TAU - 0.55); g.stroke(); g.shadowBlur = 0;
		};
		return h;
	}

	// ---------- the end: the dawn breaks the violinist into dust (it does not hurt) ----------
	static Hz B2Shatter(double T)
	{
		var h = new Hz { kind = "B2Shatter" };
		h.update = (t, dt) => { if (!h.ex && t >= T) { h.ex = true; shake = Math.Max(shake, 14); flash = Math.Max(flash, 0.35); burst(B2X, B2Y, "#ffe2b0", 60, 800); burst(B2X, B2Y, "#ffffff", 30, 500); sfx(tt => { sNoise(tt, 0.6, "lowpass", 400, 0.3); sTone(tt, 82, 0.6, "square", 0.12); }); } if (t > T + 3) h.dead = true; };
		h.draw = t =>
		{
			if (t < T - 0.6) return;
			if (t < T) { double k = (t - (T - 0.6)) / 0.6; g.strokeStyle = $"rgba(255,240,220,{str(k)})"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 7; i++) { double a = i * 0.9; g.moveTo(B2X, B2Y); g.lineTo(B2X + JS.cos(a) * 50 * k, B2Y + JS.sin(a) * 50 * k); } g.stroke(); return; }
			double e = t - T; for (int i = 0; i < 16; i++) { double a = i * TAU / 16 + 0.3, d = 50 + e * 480; g.fillStyle = $"rgba(255,226,190,{str(clamp(1 - e / 1.6, 0, 1))})"; circle(B2X + JS.cos(a) * d, B2Y + JS.sin(a) * d - e * 60, 5 + (i % 3) * 2); }
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _b2 = DodgeBuild("B2", () => buildB2());
	// how far a point is from the minute hand at beat bt (for placing the bow's gap away from it)
	static double B2HandDist(double x, double y, double bt) { var m = B2HandLen(bt, B2ML); if (m.warn || m.len <= 40) return 999; double a = B2Min(bt); return L15Seg(x, y, B2X + JS.cos(a) * 40, B2Y + JS.sin(a) * 40, B2X + JS.cos(a) * m.len, B2Y + JS.sin(a) * m.len); }
	static List<Ev> buildB2()
	{
		var E = new List<Ev>();
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz h) => hazards.Add(h);
		double SB = SPB;

		at(0, () => add(B2Stage()), true);
		at(0, () => add(B2Strings()), true);
		at(0, () => add(B2Hands()), true);
		at(0, () => add(B2Boss()), true);

		// ===== 1. midnight (b0-128) =====
		// the twelve chimes (b4-28 every 4 beats, b32-40 every 2): rings coming in, the gap just behind the minute hand when the ring is
		// halfway in (300 px)
		{
			var CH = new List<double>(); for (double b = 4; b <= 28; b += 4) CH.Add(b); for (double b = 32; b <= 40; b += 2) CH.Add(b);
			foreach (var T in CH)
			{
				double TT = T, v = (T < 32 ? 420 : 470) * B2K, bc = BT(B(T) + (740 - 300) / v), ga = B2Min(bc) - 0.42;
				at(TT - 1, () => add(B2Chime(B(TT), v, ga, 0.3)));
			}
		}
		// a note on each note of the tune, from the violinist away from the minute hand (two from b32)
		foreach (var q in B2_INTRO)
		{
			double qq = q, a = B2Min(q) + Math.PI, v = (300 + 80 * clamp((q - 2) / 30, 0, 1)) * B2K;
			at(qq - 0.75, () => { if (qq < 32) add(B2Note(B2X, B2Y, a, v, B(qq), 0.75 * SB)); else { add(B2Note(B2X, B2Y, a - 0.35, v, B(qq), 0.75 * SB)); add(B2Note(B2X, B2Y, a + 0.35, v, B(qq), 0.75 * SB)); } });
		}
		// the violin (b40-64): each of its notes plucks a string (by the pitch); on every other beat two strings at once, and a ring of notes out every 2 beats
		for (int i = 0; i < B2_PLK.Length; i++)
		{
			double T = B2_PLK[i]; int s = B2_PLKS[i]; if (T < 40 || T >= 64) continue;
			bool bar = Math.Abs(T / 2 - Math.Round(T / 2)) < 1e-6; double TT = T;
			at(TT - 1, () => { add(B2Pluck(s, TT)); if (bar) add(B2Pluck((s + 2) % 4, TT)); });
		}
		for (double T = 40; T < 64; T += 2)
		{
			double TT = T, a0 = T * 0.31;
			at(TT - 1, () => { for (int i = 0; i < 10; i++) add(B2Note(B2X, B2Y, a0 + i * TAU / 10, 340 * B2K, B(TT), SB)); });
		}
		// the baton wakes up (b44-64): the marked one's three every 2 beats
		at(42, () => add(B1MarkFx(B(42), B(128))));
		for (double T = 44; T < 64; T += 2)
		{
			double TT = T;
			at(TT - 1, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.14, 520 * B2K, B(TT), SB, 3)); }, rnd: true);
		}
		// the bow (b64-128): a sweep every 2 beats going round the four ways; a string plucked in each rest
		for (int n = 0; n < 32; n++)
		{
			double T = 64 + 2 * n; int dir = n % 4;
			double gc = dir % 2 == 0 ? clamp(360 + 230 * JS.sin(n * 1.1 + 0.4), 110, 610) : clamp(640 + 460 * JS.sin(n * 0.9 + 1.2), 120, 1160);
			double TT = T; at(TT - 2, () => add(B2Bow(dir, TT, gc)));
			int s = new[] { 0, 2, 1, 3 }[n % 4]; double P = T + 1.5;
			at(P - 1, () => add(B2Pluck(s, P)));
		}
		// the mark (b72-128): the baton points at the marked one, three notes at where it stands on every beat, every half beat from b104 (0:49)
		for (double T = 72; T < 127.9; T += T < 104 ? 1 : 0.5)
		{
			double TT = T;
			at(TT - 1, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.14, 560 * B2K, B(TT), SB, 3)); }, rnd: true);
		}

		// ===== 2. the dance (b128-208) =====
		at(127.5, () => add(B2Dark()));
		// in the dark: a ring of 18 notes on every second beat, three at the marked one between them
		for (double T = 128; T < 136; T += 2)
		{
			double TT = T, a0 = T * 0.17;
			at(TT - 1, () => { for (int i = 0; i < 18; i++) add(B2Note(B2X, B2Y, a0 + i * TAU / 18, 330 * B2K, B(TT), SB)); });
			double T2 = T + 1; at(T2 - 1, () => { var p = B1Aim(T2); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.14, 560 * B2K, B(T2), SB, 3)); }, rnd: true);
		}
		// the rings of dancers (masks grey from b132, b136-190) and the line of dancers that sweeps round, its gap in one zone a turn
		at(131, () => add(B2Rings()), true);
		{
			(double b0, double b1, int zone, bool twin)[] SP = { (136, 144, 3, false), (144, 152, 2, false), (152, 160, 1, true), (160, 168, 2, true), (168, 176, 0, true), (176, 184, 1, true), (184, 190, 2, true) };
			double w = TAU / 8;
			foreach (var (b0, b1, zone, twin) in SP) { double bb0 = b0, bb1 = b1, a0 = -Math.PI / 2 + w * (b0 - 136); int zz = zone; bool tw = twin; at(bb0 - 1.5, () => add(B2Spoke(bb0, bb1, a0, w, zz, tw))); }
		}
		// the pairs along the walls, every 4 beats going round the room
		{
			(double x0, double y0, double x1, double y1)[] PW = { (-60, 52, W + 60, 52), (W - 52, -60, W - 52, H + 60), (W + 60, H - 52, -60, H - 52), (52, H + 60, 52, -60) };
			int n = 0; for (double T = 140; T < 188; T += 2, n++) { var pw = PW[n % 4]; double TT = T; at(TT - 1.5, () => add(B2Pair(pw.x0, pw.y0, pw.x1, pw.y1, B(TT), 520 * B2K))); }
		}
		// two notes out every beat in a turning line; the mark's three every 2 beats, every beat from b160 (1:15)
		for (double T = 137; T < 190; T += 1) { double TT = T, a = T * 0.6; at(TT - 1, () => { add(B2Note(B2X, B2Y, a, 300 * B2K, B(TT), SB)); add(B2Note(B2X, B2Y, a + Math.PI, 300 * B2K, B(TT), SB)); }); }
		at(141, () => add(B1MarkFx(B(141), B(208))));
		for (double T = 143; T < 190; T += T < 160 ? 2 : 1)
		{
			double TT = T;
			at(TT - 1, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.14, 560 * B2K, B(TT), SB, 3)); }, rnd: true);
		}
		// the last waltz (b192-208): a closing ring every 4 beats whose gap goes after the marked one; notes in a slow spiral, the mark's three
		foreach (var (w0, w1) in new[] { (192.0, 197.0), (196.0, 201.0), (200.0, 205.0), (204.0, 208.0) }) { double a0 = w0, a1 = w1; at(a0 - 1, () => add(B2Waltz(a0, a1))); }
		for (double T = 192; T < 207.5; T += 0.5) { double TT = T, a = T * 1.3; at(TT - 1, () => { for (int k = 0; k < 3; k++) add(B2Note(B2X, B2Y, a + k * TAU / 3, 360 * B2K, B(TT), SB)); }); }
		for (double T = 193; T < 208; T += 1)
		{
			double TT = T;
			at(TT - 1, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.16, 520 * B2K, B(TT), SB, 3)); }, rnd: true);
		}

		// ===== 3. dawn (b208-248) =====
		at(204, () => add(B2Comb()), true);
		// the bow twice (b232, b240): its gap where the minute hand and the pins are not
		var bows = new List<(int dir, double T, double gc)>();
		foreach (var (dir, T) in new[] { (0, 232.0), (1, 240.0) })
		{
			double best = -1, bg = 0;
			for (double c = dir == 0 ? 110 : 140; c <= (dir == 0 ? 610 : 1140); c += 20)
			{
				double sc = 999;
				for (double k = 0; k <= 1.0001; k += 0.05)
				{
					double bt = T + 1.0 * k, s = -15 + ((dir == 0 ? W : H) + 30) * k, x = dir == 0 ? s : c, y = dir == 0 ? c : s; sc = Math.Min(sc, B2HandDist(x, y, bt));
					for (int i = 0; i < B2_PIN.Length; i++) { double pt = B(B2_PIN[i]), px = B2CX - B2PV * (B(bt) - pt), py = B2LaneY(B2_PINL[i], B2_PIN[i]); if (Math.Abs(px - x) < 60 && Math.Abs(py - y) < 60) sc = Math.Min(sc, Math.Abs(py - y)); }
				}
				if (sc > best) { best = sc; bg = c; }
			}
			bows.Add((dir, T, bg));
		}
		foreach (var bw in bows) { var b2 = bw; at(b2.T - 2, () => add(B2Bow(b2.dir, b2.T, b2.gc))); }
		// the pins on the tune's notes; none in the bow's gap while it goes by
		for (int i = 0; i < B2_PIN.Length; i++)
		{
			double T = B2_PIN[i]; int l = B2_PINL[i]; double y = B2LaneY(l, T), pt = B(T), t0 = pt - (W + 110 - B2CX) / B2PV, TT = T; bool skip = false;
			foreach (var bw in bows)
				for (double k = 0; k <= 1.0001; k += 0.05)
				{
					double bt = bw.T + 1.0 * k, s = -15 + ((bw.dir == 0 ? W : H) + 30) * k, px = B2CX - B2PV * (B(bt) - pt);
					double x = bw.dir == 0 ? s : bw.gc, yy = bw.dir == 0 ? bw.gc : s;
					if (B(bt) > t0 && Math.Abs(px - x) < 70 && Math.Abs(y - yy) < 90) skip = true;
				}
			if (skip) continue;
			bool chord = Math.Abs(T / 4 - Math.Round(T / 4)) < 1e-6;
			at(BT(t0) - 0.75, () => { add(B2Pin(l, TT)); if (chord) add(B2Pin((l + 4) % 8, TT)); });
		}
		// the clock strikes six (b224-244, every 4 beats): rings coming in like at midnight, the gap just behind the minute hand
		for (double T = 224; T <= 244; T += 4)
		{
			double TT = T, v = 470 * B2K, bc = BT(B(T) + (740 - 300) / v), ga = B2Min(bc) - 0.42;
			at(TT - 1, () => add(B2Chime(B(TT), v, ga, 0.3)));
		}
		// the strings again (b228-248): two plucked on every beat (the kick), the pairs swap
		for (double T = 228; T < 248; T += 1) { double TT = T; int s0 = (int)(T - 228) % 2; at(TT - 1, () => { add(B2Pluck(s0, TT, 66)); add(B2Pluck(s0 + 2, TT, 66)); }); }
		// the mark's three every beat through the music box, every half beat from b232
		at(209, () => add(B1MarkFx(B(209), B(247))));
		for (double T = 211; T < 247; T += T < 232 ? 1 : 0.5)
		{
			double TT = T;
			at(TT - 1, () => { var p = B1Aim(TT); double a = JS.atan2(p.y - B2Y, p.x - B2X); for (int s = -1; s <= 1; s++) add(B2Note(B2X, B2Y, a + s * 0.14, 560 * B2K, B(TT), SB, 3)); }, rnd: true);
		}
		at(247.5, () => add(B2Shatter(B(248))));

		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
