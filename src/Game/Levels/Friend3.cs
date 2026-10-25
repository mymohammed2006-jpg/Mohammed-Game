using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 1300-1441: "انتظرتك سنين وأيام طوال" (MiniClock, CalPage, QMark, Bouncer, Sun, Moon, Star, Hourglass, Envelope,
// NumShot, GreatClock) and buildFriend3
public static partial class G
{
	static readonly int _afkF3 = AfkReg("buildFriend3", (x, y, b, v, w) => One(Star(x, y, B(b - w), B(b), v != 0 ? Math.PI / 2 : 0, 650)));

	// a small clock: grey body, pink hands that tick on every beat
	public static Hz MiniClock(double x, double y, double R, double tw, double ta, double te, double a0, double tickRad = Math.PI / 6)
	{
		double lastB = -1, from = a0, to = a0, tt = 0;
		Func<double, double> hands = t => { double k = clamp((t - tt) / 0.08, 0, 1); return from + (to - from) * k; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) { h.dead = true; return; } if (t < ta) return; double b = Math.Floor(BT(t)); if (b != lastB) { lastB = b; from = hands(t); to = from + tickRad; tt = t; } };
		h.draw = t =>
		{
			double a = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); bool act = t >= ta && t < te; double m = hands(t);
			g.save(); g.translate(x, y); g.globalAlpha = a; g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(0, 0, R, 0, TAU); g.stroke();
			for (int i = 0; i < 12; i++) { double q = i * TAU / 12; g.beginPath(); g.moveTo(JS.cos(q) * R * 0.85, JS.sin(q) * R * 0.85); g.lineTo(JS.cos(q) * R * 0.95, JS.sin(q) * R * 0.95); g.stroke(); }
			g.strokeStyle = act ? PINK : "rgba(255,42,109,0.35)"; g.lineCap = "round"; if (act) { g.shadowColor = PINK; g.shadowBlur = 14; }
			g.lineWidth = 8; g.beginPath(); g.moveTo(0, 0); g.lineTo(JS.cos(m) * R * 1.05, JS.sin(m) * R * 1.05); g.stroke();
			g.lineWidth = 11; g.beginPath(); g.moveTo(0, 0); g.lineTo(JS.cos(m / 12 + a0) * R * 0.55, JS.sin(m / 12 + a0) * R * 0.55); g.stroke(); g.shadowBlur = 0;
			g.fillStyle = act ? PINK : GREY + "0.6)"; circle(0, 0, 9); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double m = hands(t); if (hypot(p.x - x, p.y - y) < 10 + p.r) return true;
			return segDist(p.x, p.y, new[] { x, y }, new[] { x + JS.cos(m) * R * 1.05, y + JS.sin(m) * R * 1.05 }) < 4 + p.r || segDist(p.x, p.y, new[] { x, y }, new[] { x + JS.cos(m / 12 + a0) * R * 0.55, y + JS.sin(m / 12 + a0) * R * 0.55 }) < 5.5 + p.r;
		};
		return h;
	}
	// a calendar page tears off and flies away, swaying
	public static Hz CalPage(double x, double y, double vx, double vy, double w, double hh, string num)
	{
		double rot = 0, ph = rnd() * TAU; double? t0 = null; double rs = (rnd() < 0.5 ? -1 : 1) * (1.5 + rnd());
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) => { if (t0 == null) t0 = t; h.x += (vx + JS.sin((t - t0.Value) * 3 + ph) * 60) * dt; h.y += vy * dt; rot += rs * dt; if (h.y > H + 120 || h.x < -150 || h.x > W + 150 || h.y < -200) h.dead = true; };
		h.draw = t => { g.save(); g.translate(h.x, h.y); g.rotate(rot); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.fillRect(-w / 2, -hh / 2, w, hh); g.shadowBlur = 0; g.fillStyle = "#8f1a43"; g.fillRect(-w / 2, -hh / 2, w, hh * 0.22); g.font = $"800 {str(hh * 0.42)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(num, 0, hh * 0.12); g.restore(); };
		h.hit = (p, t) => obbHit(p.x, p.y, p.r, h.x, h.y, w / 2, hh / 2, rot);
		return h;
	}
	// a question mark "؟" comes down, then its dot bounces around the arena
	public static Hz QMark(double x, double y, double s, double tw, double ta, double td, double sp)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t >= td) { double a = rnd() * TAU; hazards.Add(Bouncer(x, y + s * 0.38, JS.cos(a) * sp, JS.sin(a) * sp, s * 0.13, t + 3.2)); burst(x, y, PINK, 10); h.dead = true; } };
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.translate(x, y); g.font = $"800 {str(s)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "rtl";
			if (t < ta) { g.globalAlpha = 0.2 + 0.4 * k; g.fillStyle = PINK; g.scale(1.4 - 0.4 * k, 1.4 - 0.4 * k); g.fillText("؟", 0, 0); } else { g.fillStyle = "rgba(255,42,109,0.5)"; circle(0, 0, s * 0.3); g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath(); g.arc(0, 0, s * 0.3, 0, TAU); g.stroke(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; g.scale(1 + 0.08 * pulse, 1 + 0.08 * pulse); g.fillText("؟", 0, 0); }   /* 10-25: a pink ring shows the round part that hurts */
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= ta && hypot(p.x - x, p.y - y) < s * 0.3 + p.r;
		return h;
	}
	public static Hz Bouncer(double x, double y, double vx, double vy, double r, double tend)
	{
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) => { h.x += vx * dt; h.y += vy * dt; if (h.x < r || h.x > W - r) { vx = -vx; h.x = clamp(h.x, r, W - r); } if (h.y < r || h.y > H - r) { vy = -vy; h.y = clamp(h.y, r, H - r); } if (t > tend) h.dead = true; };
		h.draw = t => { double a = clamp((tend - t) / 0.4, 0, 1); g.globalAlpha = a; g.fillStyle = "rgba(255,42,109,0.25)"; circle(h.x, h.y, r * 1.6); g.fillStyle = PINK; circle(h.x, h.y, r * (1 + 0.12 * pulse)); g.globalAlpha = 1; };
		h.hit = (p, t) => t < tend - 0.3 && hypot(p.x - h.x, p.y - h.y) < r + p.r - 1;
		return h;
	}
	// the day sun: crosses the arena and its rays grow and shrink with the beat
	public static Hz Sun(double t0, double t1, double y0, double amp)
	{
		Func<double, double[]> pos = t => { double k = clamp((t - t0) / (t1 - t0), 0, 1); return new[] { -80 + (W + 160) * k, y0 - JS.sin(k * Math.PI) * amp }; };
		Func<double, double> env = t => JS.exp(-((BT(t) % 1 + 1) % 1) * 4);
		List<double[][]> rays(double t) { var q = pos(t); double x = q[0], y = q[1], L = 90 + 90 * env(t), rot = (t - t0) * 0.6; var R = new List<double[][]>(); for (int i = 0; i < 8; i++) { double a = rot + i * TAU / 8; R.Add(new[] { new[] { x + JS.cos(a) * 62, y + JS.sin(a) * 62 }, new[] { x + JS.cos(a) * (62 + L), y + JS.sin(a) * (62 + L) } }); } return R; }
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { var q = pos(t); double x = q[0], y = q[1]; g.save(); g.shadowColor = PINK; g.shadowBlur = 30; g.fillStyle = PINK; circle(x, y, 50 * (1 + 0.06 * pulse)); g.strokeStyle = PINK; g.lineWidth = 9; g.lineCap = "round"; foreach (var r in rays(t)) { var a = r[0]; var b = r[1]; g.beginPath(); g.moveTo(a[0], a[1]); g.lineTo(b[0], b[1]); g.stroke(); } g.restore(); };
		h.hit = (p, t) => { var q = pos(t); double x = q[0], y = q[1]; if (hypot(p.x - x, p.y - y) < 50 + p.r) return true; foreach (var r in rays(t)) if (segDist(p.x, p.y, r[0], r[1]) < 4.5 + p.r) return true; return false; };
		return h;
	}
	// the night moon (grey decoration) and stars that twinkle, then fall as shooting stars
	public static Hz Moon(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.5)"; circle(W - 170, 120, 55); g.fillStyle = "#000"; circle(W - 150, 108, 50); g.restore(); g.globalAlpha = 1; };
		return h;
	}
	public static Hz Star(double x, double y, double tw, double ta, double ang, double sp)
	{
		double px = x, py = y; var trail = new List<double[]>();
		var h = new Hz();
		h.update = (t, dt) => { if (t < ta) return; px += JS.cos(ang) * sp * dt; py += JS.sin(ang) * sp * dt; trail.Add(new[] { px, py }); if (trail.Count > 9) trail.RemoveAt(0); if (px < -60 || px > W + 60 || py > H + 60 || py < -60) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), s = 6 + 10 * k * (0.7 + 0.3 * JS.sin(t * 20)); g.save(); g.translate(x, y); g.rotate(t * 2); g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.6 * k)})"; g.beginPath(); for (int i = 0; i < 8; i++) { double r = i % 2 != 0 ? s * 0.4 : s, a = i * Math.PI / 4; g.lineTo(JS.cos(a) * r, JS.sin(a) * r); }
				g.closePath(); g.fill(); g.restore();
				g.strokeStyle = $"rgba(255,42,109,{str(0.12 + 0.25 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 }); g.beginPath(); g.moveTo(x, y); g.lineTo(x + JS.cos(ang) * 1400, y + JS.sin(ang) * 1400); g.stroke(); g.setLineDash(new double[0]); return;
			}
			for (int i = 0; i < trail.Count; i++) { g.globalAlpha = (double)(i + 1) / trail.Count * 0.5; g.fillStyle = PINK; circle(trail[i][0], trail[i][1], 4 + i); }
			g.globalAlpha = 1; g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; circle(px, py, 12 * (1 + 0.1 * pulse)); g.restore();
		};
		h.hit = (p, t) => t >= ta && hypot(p.x - px, p.y - py) < 12 + p.r - 1;
		return h;
	}
	// an hourglass: grey frame, pink sand pours down and scatters, and it flips every so often
	public static Hz Hourglass(double cx, double cy, double ts, double te, double[] flips, Tier T)
	{
		const double HH = 190, HW = 120; int lastS = -1; var done = new HashSet<double>();
		Func<double, double> rot = t => { double r = 0; foreach (var f in flips) { if (t >= f + 0.4) r += Math.PI; else if (t >= f) r += Math.PI * ((t - f) / 0.4); } return r; };
		Func<double, bool> flowing = t => { foreach (var f in flips) if (t >= f - 0.05 && t < f + 0.55) return false; return t >= ts + SPB * 2 && t < te; };
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > te + 0.4) { h.dead = true; return; }
			foreach (var f in flips) if (!done.Contains(f) && t >= f + 0.4) { done.Add(f); for (int i = 0; i < 10; i++) { double a = i * TAU / 10; hazards.Add(Bullet(cx, cy, JS.cos(a) * T.sp * 0.7, JS.sin(a) * T.sp * 0.7, 7)); } shake = Math.Max(shake, 8); }
			if (!flowing(t)) return; int s = (int)Math.Floor(BT(t) * 2); if (s == lastS) return; lastS = s;
			foreach (var d in new double[] { -1, 1 }) { double a = Math.PI / 2 + d * (0.5 + rnd() * 0.6); hazards.Add(Bullet(cx + d * 30, cy + HH * 0.9, JS.cos(a) * T.sp * 0.75, -Math.Abs(JS.sin(a)) * T.sp * 0.3 + 40, 6)); }
		};
		h.draw = t =>
		{
			double a = Math.Min(1, (t - ts) / (SPB * 2)), r = rot(t); g.save(); g.translate(cx, cy); g.rotate(r); g.globalAlpha = a;
			g.strokeStyle = GREY + "0.7)"; g.lineWidth = 5; g.beginPath(); g.moveTo(-HW, -HH); g.lineTo(HW, -HH); g.lineTo(14, 0); g.lineTo(HW, HH); g.lineTo(-HW, HH); g.lineTo(-14, 0); g.closePath(); g.stroke();
			g.fillStyle = GREY + "0.18)"; g.beginPath(); g.moveTo(-HW * 0.7, HH); g.lineTo(HW * 0.7, HH); g.lineTo(0, HH * 0.55); g.closePath(); g.fill();
			g.restore(); g.globalAlpha = 1;
			if (flowing(t)) { g.save(); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.fillRect(cx - 9, cy - HH * 0.2, 18, HH * 1.15 + 60); g.restore(); }
		};
		h.hit = (p, t) => { if (!flowing(t)) return false; double r = rot(t); return obbHit(p.x, p.y, p.r, cx - JS.sin(r) * 0, cy + (HH * 0.375 + 30), 9, (HH * 1.15 + 60) / 2, 0); };
		return h;
	}
	public static Hz Envelope(double x, double y, double vx, double vy)
	{
		var b = Bullet(x, y, vx, vy, 14); double a = JS.atan2(vy, vx);
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; g.fillRect(-18, -12, 36, 24); g.shadowBlur = 0; g.strokeStyle = "#000"; g.lineWidth = 2; g.beginPath(); g.moveTo(-18, -12); g.lineTo(0, 2); g.lineTo(18, -12); g.stroke(); g.restore(); };
		return b;
	}

	// ===== the boss: the great clock =====
	// the minute hand spins and reaches outside the clock; every number it passes flies at you; it ticks; sometimes it turns time back
	public static readonly string[] AR_NUM = { "١٢", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩", "١٠", "١١" };
	public static Hz NumShot(double x, double y, double vx, double vy, string txt)
	{
		var b = Bullet(x, y, vx, vy, 14);
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.fillStyle = "rgba(255,42,109,0.45)"; circle(0, 0, 14); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.font = $"800 34px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(txt, 0, 2); g.restore(); };
		return b;
	}
	public static Hz GreatClock(double ts, double te, Tier T)
	{
		const double cx = W / 2, cy = H / 2, R = 150, L = R + 230; double mA = -Math.PI / 2, lastT = ts; int lastS = -1; bool ex = false; var shotAt = Enumerable.Repeat(-9.0, 12).ToArray();
		Func<double, double> speed = bar => bar < 48 ? 1.7 : bar < 53 ? -3.0 : 2.5;
		var h = new Hz();
		h.update = (t, dt0) =>
		{
			double dt = Math.Max(0, t - lastT); lastT = t; double bar = Math.Floor(BT(t) / 4); bool live = t >= ts + SPB * 4;
			if (t >= te && !ex)
			{
				ex = true; for (int i = 0; i < 12; i++) { double a = -Math.PI / 2 + i * TAU / 12; hazards.Add(NumShot(cx + JS.cos(a) * (R - 25), cy + JS.sin(a) * (R - 25), JS.cos(a) * 230, JS.sin(a) * 230, AR_NUM[i])); }
				for (int i = 0; i < 16; i++) { double a = i * TAU / 16; hazards.Add(Bullet(cx, cy, JS.cos(a) * 280, JS.sin(a) * 280, 8)); }
				burst(cx, cy, PINK, 80, 560); burst(cx, cy, "#fff", 40, 460); shake = 28; flash = 0.75; rumble(600, 1, 1); h.dead = true; return;
			}
			if (!live) return;
			double prev = mA; mA += speed(bar) * dt;
			for (int i = 0; i < 12; i++)
			{
				double na = -Math.PI / 2 + i * TAU / 12; double d0 = JS.atan2(JS.sin(prev - na), JS.cos(prev - na)), d1 = JS.atan2(JS.sin(mA - na), JS.cos(mA - na));
				if (sign(d0) != sign(d1) && Math.Abs(d0) < 1 && t - shotAt[i] > 0.5 && player != null && t < te - SPB) { shotAt[i] = t; double x = cx + JS.cos(na) * (R - 25), y = cy + JS.sin(na) * (R - 25), a = JS.atan2(player.y - y, player.x - x); hazards.Add(NumShot(x, y, JS.cos(a) * T.sp, JS.sin(a) * T.sp, AR_NUM[i])); }
			}
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s; int p = s % 16;
			if (p == 0 && t < te - SPB * 4) { hazards.Add(Ring(cx, cy, t + SPB * T.wn, 250, 14, rnd() * TAU, 1.4, SPB * T.wn)); }
			if ((p == 8 || p == 0) && bar >= 46 && player != null) { double x = clamp(player.x + (rnd() * 2 - 1) * 200, 60, W - 60); hazards.Add(CalPage(x, -60, 0, T.sp * 0.75, 64, 80, str(1 + Math.Floor(rnd() * 30)))); }
		};
		h.draw = t =>
		{
			double a = clamp((t - ts) / (SPB * 3), 0, 1); bool live = t >= ts + SPB * 4; g.save(); g.translate(cx, cy); g.globalAlpha = live ? 1 : 0.4 + 0.4 * a;
			g.fillStyle = live ? "#a3184a" : "#0b0b0f"; circle(0, 0, R); g.strokeStyle = PINK; g.lineWidth = 12;   /* 10-25: the face hurts, so it is pink while it hurts (it was black) */ if (live) { g.shadowColor = PINK; g.shadowBlur = 24; }
			g.beginPath(); g.arc(0, 0, R * (1 + 0.03 * pulse), 0, TAU); g.stroke(); g.shadowBlur = 0;
			g.font = $"800 26px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; for (int i = 0; i < 12; i++) { double na = -Math.PI / 2 + i * TAU / 12; bool rec = t - shotAt[i] < 0.5; g.fillStyle = rec ? "rgba(255,255,255,0.15)" : GREY + "0.8)"; g.fillText(AR_NUM[i], JS.cos(na) * (R - 28), JS.sin(na) * (R - 28) + 2); }
			g.strokeStyle = PINK; g.lineCap = "round"; g.shadowColor = PINK; g.shadowBlur = live ? 18 : 0; g.lineWidth = 12; g.beginPath(); g.moveTo(0, 0); g.lineTo(JS.cos(mA) * L, JS.sin(mA) * L); g.stroke();
			g.lineWidth = 16; g.beginPath(); g.moveTo(0, 0); g.lineTo(JS.cos(mA / 12 - Math.PI * 0.4) * R * 0.55, JS.sin(mA / 12 - Math.PI * 0.4) * R * 0.55); g.stroke(); g.shadowBlur = 0; g.fillStyle = PINK; circle(0, 0, 14);
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < ts + SPB * 4) return false; if (hypot(p.x - cx, p.y - cy) < R + 6 + p.r) return true; return segDist(p.x, p.y, new[] { cx, cy }, new[] { cx + JS.cos(mA) * L, cy + JS.sin(mA) * L }) < 6 + p.r; };
		return h;
	}

	/* "انتظرتك سنين وأيام طوال" (Nice's pick). The beats are tracked (about 157 half beats a minute). The part from 0:48 to 2:19.
	   0 the clocks tick | 16 calendar pages "years and days" | 48 "a question"
	   80 checkpoint -> "day sun" | 96 "night stars" | 112 the hourglass "how strong is patience" + "answer" letters that never arrive
	   160 checkpoint -> the great clock | 228 it breaks */
	public static List<Ev> buildFriend3()
	{
		var E = new List<Ev>(); var R = mulberry32(1503);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 140, double b = W - 140) => a + R() * (b - a);
		double ry(double a = 120, double b = H - 120) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 345, wn = 1.5 }, new Tier { sp = 385, wn = 1.3 }, new Tier { sp = 405, wn = 1.25 } });
		void clock(double hit, double x, double y, double Rr, Tier T, double dur, double a0) => ev(hit - T.wn, () => hazards.Add(MiniClock(x, y, Rr, B(hit - T.wn), B(hit), B(hit + dur), a0)));
		void page(double b, double x, Tier T, double vx = 0) => ev(b, () => hazards.Add(CalPage(x, -60, vx, T.sp * 0.62, 70, 86, str(1 + Math.Floor(R() * 30)))));
		void pageSide(double b, bool fromLeft, double y, Tier T) => ev(b, () => hazards.Add(CalPage(fromLeft ? -60 : W + 60, y, (fromLeft ? 1 : -1) * T.sp * 0.8, 20, 70, 86, str(1 + Math.Floor(R() * 30)))));
		void qmark(double hit, double x, double y, Tier T, double s = 150) => ev(hit - T.wn, () => hazards.Add(QMark(x, y, s, B(hit - T.wn), B(hit), B(hit + 2), T.sp * 0.85)));
		void qmarkP(double hit, Tier T, double s = 140) { double jx = (R() * 2 - 1) * 100, jy = (R() * 2 - 1) * 80; ev(hit - T.wn, () => { double x = clamp((player != null ? player.x : W / 2) + jx, 90, W - 90), y = clamp((player != null ? player.y : H / 2) + jy, 90, H - 90); hazards.Add(QMark(x, y, s, B(hit - T.wn), B(hit), B(hit + 2), T.sp * 0.85)); }); }
		void star(double hit, double x, double y, double ang, Tier T) => ev(hit - T.wn, () => hazards.Add(Star(x, y, B(hit - T.wn), B(hit), ang, T.sp * 1.4)));
		void starP(double hit, double x, double y, Tier T) => ev(hit - T.wn, () => { double a = player != null ? JS.atan2(player.y - y, player.x - x) : Math.PI / 2; hazards.Add(Star(x, y, B(hit - T.wn), B(hit), a, T.sp * 1.35)); });
		void env(double b, Tier T, bool fromLeft) => ev(b, () => { if (player == null) return; double x = fromLeft ? -30 : W + 30, y = clamp(player.y + (R() * 2 - 1) * 120, 40, H - 40), a = JS.atan2(player.y - y, player.x - x); hazards.Add(Envelope(x, y, JS.cos(a) * T.sp, JS.sin(a) * T.sp)); });

		/* ---------- part 1 ---------- */
		var T = TI[0];
		// 0:48 the waiting: clocks appear and tick on every beat
		clock(4, W * 0.25, H * 0.35, 120, T, 24, -Math.PI / 2); clock(8, W * 0.75, H * 0.65, 120, T, 22, Math.PI / 2); clock(14, W * 0.5, H * 0.5, 100, T, 14, 0);
		for (double b = 6; b < 16; b += 2) qmark(b + 1, rx(), ry(), T, 100);
		for (double b = 8; b < 16; b += 2) page(b, rx(80, W - 80), T);
		clock(10, W * 0.5, H * 0.2, 90, T, 10, Math.PI / 2);
		// 0:54 "years and days": calendar pages tear off and fly
		for (double b = 16; b < 48; b++) { page(b, rx(80, W - 80), T, (R() * 2 - 1) * 60); if (b % 4 == 1) pageSide(b + 0.5, b % 8 == 1, ry(), T); }
		clock(28, W * 0.2, H * 0.5, 110, T, 16, 0); clock(36, W * 0.8, H * 0.5, 110, T, 10, Math.PI); for (double b = 18; b < 48; b += 3) qmarkP(b + 1, T, 110);
		// 1:06 "or even a question": question marks come down and their dots bounce
		for (double b = 48; b < 80; b += 2) { qmarkP(b, T); page(b + 1, rx(80, W - 80), T); if (b % 4 == 0) qmark(b + 1, rx(), ry(), T, 120); }
		clock(52, W * 0.2, H * 0.3, 100, T, 12, 0); clock(66, W * 0.8, H * 0.7, 100, T, 12, Math.PI);
		clock(60, W * 0.5, H * 0.5, 130, T, 16, -Math.PI / 2);
		/* ---------- part 2 ---------- */
		T = TI[1];
		ev(80, () => { flash = Math.Max(flash, 0.22); shake = Math.Max(shake, 9); });
		// 1:18 "day sun": a sun crosses the arena
		ev(80, () => hazards.Add(Sun(B(80), B(96), H * 0.55, 300))); clock(82, W * 0.25, H * 0.7, 100, T, 12, 0); clock(86, W * 0.75, H * 0.7, 100, T, 10, Math.PI);
		for (double b = 82; b < 96; b++) { page(b, rx(80, W - 80), T); if (b % 2 == 0) qmarkP(b + 0.5, T, 120); }
		// 1:24 "night stars": a moon and stars falling as shooting stars
		ev(95, () => hazards.Add(Moon(B(95), B(113))));
		for (double b = 96; b < 112; b += 0.5) { double x = rx(60, W - 60); if (b % 1 == 0) starP(b + 1, x, 20 + R() * 120, T); else star(b + 1, x, 20 + R() * 100, Math.PI / 2 + (R() * 2 - 1) * 0.5, T); }
		clock(100, W * 0.3, H * 0.6, 110, T, 10, 0); clock(104, W * 0.7, H * 0.4, 110, T, 8, Math.PI); for (double b = 98; b < 112; b += 2) qmarkP(b, T, 110);
		clock(84, W * 0.5, H * 0.25, 100, T, 10, 0); clock(98, W * 0.2, H * 0.75, 100, T, 12, 0); clock(102, W * 0.8, H * 0.75, 100, T, 10, Math.PI);
		// 1:30 "how strong is patience": an hourglass that flips, and "answer" letters that never arrive
		{ var fl = new double[] { 124, 136, 148 }.Select(B).ToArray(); ev(112, () => hazards.Add(Hourglass(W / 2, H / 2 - 60, B(112), B(158), fl, T))); }   // T here is read when the event runs (the JS `let T`), like the original
		for (double b = 114; b < 158; b++) { env(b, T, b % 2 == 0); if (b % 2 == 1) env(b + 0.5, T, b % 4 == 1); if (b % 4 == 2) page(b + 0.5, rx(80, W - 80), T); starP(b + 0.5, rx(80, W - 80), 30, T); if (b % 2 == 0) qmarkP(b + 0.5, T, 110); }
		clock(126, W * 0.5, H * 0.85, 95, T, 20, 0); clock(116, W * 0.18, H * 0.3, 105, T, 18, 0); clock(120, W * 0.82, H * 0.7, 105, T, 18, Math.PI); clock(136, W * 0.18, H * 0.7, 105, T, 20, 0); clock(140, W * 0.82, H * 0.3, 105, T, 18, Math.PI);
		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		ev(160, () => hazards.Add(GreatClock(B(160), B(228), TT)));
		for (double b = 166; b < 226; b += 1) { env(b, T, b % 2 == 0); if (b % 2 != 0) continue; if (b % 4 == 2) qmarkP(b + 1, T, 110); else page(b + 1, rx(80, W - 80), T); }
		for (double b = 168; b < 224; b += 0.5) starP(b + 0.25, rx(80, W - 80), 20, T);
		clock(172, W * 0.15, H * 0.5, 100, T, 24, 0); clock(180, W * 0.85, H * 0.5, 100, T, 24, Math.PI);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
