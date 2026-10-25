using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 1616-1966: the shapes of Fairytale and buildFriend5; its AFK maker from AFKMK (line 6520)
public static partial class G
{
	// JS a||b for numbers (0, NaN -> b)
	static double or5(double a, double b) => a == 0 || double.IsNaN(a) ? b : a;

	// the four violin strings (their height in the arena)
	static readonly double[] STR_Y = { 195, 315, 435, 555 };

	// Note's options object o={ax,ay,r,sw,sf,ph,heart,life}
	public class NoteO { public double ax, ay, r, sw, sf, ph, life; public bool heart; }

	// a music note: its place is a fixed formula of time (speed, acceleration and sway), so it moves the same every time
	public static Hz Note(double x0, double y0, double vx, double vy, double t0, NoteO o = null)
	{
		o ??= new NoteO();
		double ax = or5(o.ax, 0), ay = or5(o.ay, 0), r = or5(o.r, 10), sw = or5(o.sw, 0), sf = or5(o.sf, 3), ph = or5(o.ph, 0); bool hrt = o.heart; double life = or5(o.life, 9);
		Func<double, (double, double)> pos = t => { double d = t - t0; return (x0 + vx * d + 0.5 * ax * d * d + (sw != 0 ? JS.sin(d * sf + ph) * sw : 0), y0 + vy * d + 0.5 * ay * d * d); };
		var h = new Hz();
		h.update = (t, dt) => { if (t - t0 > life) h.dead = true; else if (t - t0 > 0.6) { var q = pos(t); if (q.Item1 < -100 || q.Item1 > W + 100 || q.Item2 < -130 || q.Item2 > H + 130) h.dead = true; } };
		h.draw = t =>
		{
			if (t < t0) return; var (x, y) = pos(t); double s = r * (1 + 0.15 * pulse); g.save(); g.translate(x, y); g.fillStyle = "rgba(255,42,109,0.22)"; circle(0, 0, s * 1.8); g.fillStyle = PINK;
			if (hrt) { g.translate(0, s * 0.1); heartPath(s * 1.05); g.fill(); }
			else
			{
				g.save(); g.rotate(-0.35); g.beginPath(); g.ellipse(0, 0, s * 1.12, s * 0.84, 0, 0, TAU); g.fill(); g.restore(); g.fillStyle = "rgba(255,42,109,0.6)"; g.fillRect(s * 0.72, -s * 2.3, s * 0.32, s * 2.3);
				g.beginPath(); g.moveTo(s * 1.04, -s * 2.3); g.quadraticCurveTo(s * 1.9, -s * 1.7, s * 1.55, -s * 0.95); g.quadraticCurveTo(s * 1.6, -s * 1.55, s * 1.04, -s * 1.7); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; var (x, y) = pos(t); double dx = p.x - x, dy = p.y - y, rr = r + p.r - 2; return dx * dx + dy * dy < rr * rr; };
		return h;
	}

	// decoration: the four violin strings, grey, across the arena (harmless)
	public static Hz ViolinStrings(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 1.2, (t1 - t) / 1.2); if (a <= 0) return; g.globalAlpha = a; g.strokeStyle = GREY + str(0.13 + 0.1 * pulse) + ")";
			for (int i = 0; i < 4; i++) { g.lineWidth = 1.2 + i * 0.6; g.beginPath(); g.moveTo(0, STR_Y[i]); g.lineTo(W, STR_Y[i]); g.stroke(); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// a string pulls tight: a see-through warning line, then it vibrates pink and hurts (ang is a number or a function of time)
	public static Hz Pluck(double cx, double cy, double ang, double tw, double ta, double te, double hw = 18) => Pluck(cx, cy, t => ang, tw, ta, te, hw);
	public static Hz Pluck(double cx, double cy, Func<double, double> ang, double tw, double ta, double te, double hw = 18)
	{
		Func<double, double> A = ang; const double len = 3000;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(cx, cy); g.rotate(A(t));
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.14 * k)})"; g.fillRect(-len / 2, -hw, len, hw * 2); g.fillStyle = $"rgba(255,42,109,{str(0.35 + 0.55 * k)})"; g.fillRect(-len / 2, -1.5, len, 3); }
			else
			{
				double f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25), amp = (hw - 4) * f, ph = (t - ta) * 48; g.globalAlpha = f; g.fillStyle = t < te ? "rgba(255,42,109,0.45)" : "rgba(255,42,109,0.18)"; g.fillRect(-len / 2, -hw, len, hw * 2);
				g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = (t - ta) < 0.07 ? PINKW : PINK; g.lineWidth = 7; g.beginPath();
				for (int i = 0; i <= 150; i++) { double x = -len / 2 + i * len / 150, y = JS.sin(i * 0.55 + ph) * amp; if (i != 0) g.lineTo(x, y); else g.moveTo(x, y); }
				g.stroke();
			}
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; return obbHit(p.x, p.y, p.r, cx, cy, len / 2, hw, A(t)); };
		return h;
	}

	// the violin bow (or a knight's lance): a long stick that travels along its length on a line from side to side
	public static Hz BowStroke(bool vert, double c, double dir, double tw, double ta, double te, bool lance = false, double len = 620, double th = 13)
	{
		double ext = vert ? H : W, s0 = dir > 0 ? -20 : ext + 20, s1 = dir > 0 ? ext + len + 20 : -len - 20;
		Func<double, double> head = t => s0 + (s1 - s0) * clamp((t - ta) / (te - ta), 0, 1);
		Func<double, double, (double, double)> P = (u, v) => vert ? (c + v, u) : (u, c + v);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			g.save();
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; if (vert) g.fillRect(c - th, 0, th * 2, H); else g.fillRect(0, c - th, W, th * 2);
				g.fillStyle = $"rgba(255,42,109,{str(0.35 + 0.55 * k)})";
				for (int i = 0; i < 3; i++) { double u = (dir > 0 ? 18 : ext - 18) + dir * i * 22; var (x, y) = P(u, 0); g.save(); g.translate(x, y); g.rotate(vert ? (dir > 0 ? Math.PI / 2 : -Math.PI / 2) : (dir > 0 ? 0 : Math.PI)); g.beginPath(); g.moveTo(-8, -th + 2); g.lineTo(6, 0); g.lineTo(-8, th - 2); g.lineTo(-3, 0); g.closePath(); g.fill(); g.restore(); }
				g.restore(); return;
			}
			double hd = head(t), tl = hd - dir * len; g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.strokeStyle = PINK;
			if (lance)
			{
				var (a, b) = P(hd, 0); var (c1, d1) = P(tl, -th); var (c2, d2) = P(tl, th); g.beginPath(); g.moveTo(a, b); g.lineTo(c1, d1); g.lineTo(c2, d2); g.closePath(); g.fill();
				g.shadowBlur = 0; g.fillStyle = GREY + "0.85)"; var (sx, sy) = P(tl - dir * 10, 0); g.save(); g.translate(sx, sy); g.beginPath(); g.moveTo(-26, -34); g.lineTo(26, -34); g.lineTo(26, 6); g.quadraticCurveTo(0, 40, 0, 40); g.quadraticCurveTo(0, 40, -26, 6); g.closePath(); g.fill(); g.restore();
			}
			else
			{
				void r(double u, double v, double l, double w) { var (x, y) = P(Math.Min(u, u + l), v); if (vert) g.fillRect(x - w / 2, y, w, Math.Abs(l)); else g.fillRect(x, y - w / 2, Math.Abs(l), w); }
				g.globalAlpha = 0.5; r(tl, 0, hd - tl, th * 2); g.globalAlpha = 1;   /* 10-25: the inside of the bow hurts too, so it is filled pink */
				r(tl, -th + 4, hd - tl, 6); g.globalAlpha = 0.75; r(tl, th - 4, hd - tl, 3); g.globalAlpha = 1; r(tl + dir * 10, 0, dir * 26, th * 2); r(hd - dir * 8, 0, dir * 8, th * 2 - 4);
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double hd = head(t), a = Math.Min(hd, hd - dir * len), b = Math.Max(hd, hd - dir * len), u = vert ? p.y : p.x, v = vert ? p.x : p.y; return Math.Abs(v - c) < th + p.r - 1 && u > a - p.r && u < b + p.r; };
		return h;
	}

	// a book page turning: its edge sweeps the arena from side to side (only the edge hurts); above and below the page are safe
	public static Hz PageTurn(double dir, double tw, double ta, double te, double y0 = 80, double y1 = H - 80)
	{
		const double PW = W / 2 + 40, ew = 22, LF = 12; Func<double, double> ex = t => W / 2 + dir * PW * JS.cos(Math.PI * clamp((t - ta) / (te - ta), 0, 1)), lf = t => JS.sin(Math.PI * clamp((t - ta) / (te - ta), 0, 1)) * LF;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.1) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.07 * k)})"; g.fillRect(0, y0 - LF, W, y1 - y0 + 2 * LF); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.strokeRect(2, y0 - LF, W - 4, y1 - y0 + 2 * LF); g.setLineDash(new double[0]);
				g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; for (int i = -1; i <= 1; i++) { g.save(); g.translate(W / 2 + i * 60, H / 2); g.scale(-dir, 1); g.beginPath(); g.moveTo(-14, -22); g.lineTo(10, 0); g.lineTo(-14, 22); g.lineTo(-6, 0); g.closePath(); g.fill(); g.restore(); }
				return;
			}
			if (t >= te) return; double x = ex(t), l = lf(t);
			g.fillStyle = "rgba(215,215,230,0.08)"; g.beginPath(); g.moveTo(W / 2, y0); g.lineTo(x, y0 - l); g.lineTo(x, y1 + l); g.lineTo(W / 2, y1); g.closePath(); g.fill();
			g.strokeStyle = GREY + "0.2)"; g.lineWidth = 2; for (int i = 1; i < 9; i++) { double yy = y0 + (y1 - y0) * i / 9, d = (i - 4.5) / 4.5 * l; g.beginPath(); g.moveTo(W / 2 + (x - W / 2) * 0.1, yy + d * 0.1); g.lineTo(W / 2 + (x - W / 2) * 0.88, yy + d * 0.88); g.stroke(); }
			g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK; g.fillRect(x - ew / 2, y0 - l, ew, y1 - y0 + 2 * l); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double x = ex(t), l = lf(t); return Math.Abs(p.x - x) < ew / 2 + p.r - 1 && p.y > y0 - l - p.r && p.y < y1 + l + p.r; };
		return h;
	}

	// decoration: an open grey storybook (harmless)
	public static Hz StoryBook(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 1, (t1 - t) / 0.6); if (a <= 0) return; g.globalAlpha = a; g.strokeStyle = GREY + "0.16)"; g.lineWidth = 2;
			foreach (var sd in new double[] { -1, 1 })
			{
				g.beginPath(); g.moveTo(W / 2, 96); g.quadraticCurveTo(W / 2 + sd * 260, 70, W / 2 + sd * 520, 92); g.lineTo(W / 2 + sd * 520, H - 92); g.quadraticCurveTo(W / 2 + sd * 260, H - 70, W / 2, H - 96); g.stroke();
				for (int i = 1; i < 10; i++) { double y = 100 + (H - 200) * i / 10; g.beginPath(); g.moveTo(W / 2 + sd * 40, y); g.lineTo(W / 2 + sd * (i % 4 == 3 ? 300 : 470), y); g.stroke(); }
			}
			g.beginPath(); g.moveTo(W / 2, 96); g.lineTo(W / 2, H - 96); g.stroke(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// half a book splits off and flies to one side (pink, hurts)
	public static Hz BookHalf(double dir, double ta, double sp, bool vert = false)
	{
		double w = vert ? 520 : 250, hh = vert ? 160 : 300; Func<double, (double, double)> pos = t => { double d = Math.Max(0, t - ta); return vert ? (W / 2, H / 2 + dir * (hh / 2 + 6 + sp * d)) : (W / 2 + dir * (w / 2 + 6 + sp * d), H / 2); };
		var h = new Hz();
		h.update = (t, dt) => { var (x, y) = pos(t); if (x < -w || x > W + w || y < -hh || y > H + hh) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) return; var (x, y) = pos(t); g.save(); g.translate(x, y); g.rotate(dir * (t - ta) * (vert ? 0.15 : 0.25)); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.fillRect(-w / 2, -hh / 2, w, hh); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.3)"; for (int i = 1; i < 7; i++) g.fillRect(-w / 2 + 16, -hh / 2 + i * hh / 7, w - 32, 3); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; var (x, y) = pos(t); return obbHit(p.x, p.y, p.r - 1, x, y, w / 2, hh / 2, dir * (t - ta) * (vert ? 0.15 : 0.25)); };
		return h;
	}

	// sweethearts: two hearts circling each other while they travel along a line
	public static Hz Sweethearts(double x0, double y0, double x1, double y1, double tw, double t0, double t1, double R = 50, double s = 17, double turns = 2.5)
	{
		Func<double, (double, double)> c = t => { double k = clamp((t - t0) / (t1 - t0), 0, 1); return (x0 + (x1 - x0) * k, y0 + (y1 - y0) * k); };
		Func<double, int, (double, double)> hp = (t, i) => { var (cx, cy) = c(t); double a = (t - t0) / (t1 - t0) * turns * TAU + i * Math.PI; return (cx + JS.cos(a) * R, cy + JS.sin(a) * R); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0)
			{
				double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.3 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
				for (int i = 0; i < 2; i++) { var (x, y) = hp(t0, i); g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.3 * k)})"; g.save(); g.translate(clamp(x, 20, W - 20), clamp(y, 20, H - 20)); heartPath(s); g.fill(); g.restore(); }
				return;
			}
			var (a1, b1) = hp(t, 0); var (a2, b2) = hp(t, 1); g.strokeStyle = GREY + "0.35)"; g.lineWidth = 2; g.beginPath(); g.moveTo(a1, b1); g.lineTo(a2, b2); g.stroke();
			for (int i = 0; i < 2; i++) { var (x, y) = hp(t, i); g.save(); g.translate(x, y); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; heartPath(s * (1 + 0.12 * pulse)); g.fill(); g.restore(); }
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; for (int i = 0; i < 2; i++) { var (x, y) = hp(t, i); if (hypot(p.x - x, p.y - y) < s * 0.85 + p.r - 1) return true; } return false; };
		return h;
	}

	// a fairytale castle tower: rises from below (or comes down from above) with a pointed roof; the grey flag is harmless
	public static Hz Turret(double x, double w, double hgt, bool top, double tw, double ta, double te)
	{
		Func<double, double> up = t => t < ta ? 0 : t < ta + 0.12 ? (t - ta) / 0.12 : t < te ? 1 : clamp(1 - (t - te) / 0.22, 0, 1); double rf = w * 0.7;
		Func<double, double> tipY = k => H - (hgt + rf) * k;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.save(); if (top) { g.translate(0, H); g.scale(1, -1); }
			bool pre = t < ta; double k = pre ? 1 : up(t), by = H - hgt * k, ty = by - rf * k;
			if (pre) { double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * q)})"; g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * q)})"; g.lineWidth = 2; }
			else { g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; }
			g.beginPath(); g.moveTo(x - w / 2, H + 10); g.lineTo(x - w / 2, by); g.lineTo(x - w / 2 - 8, by); g.lineTo(x, ty); g.lineTo(x + w / 2 + 8, by); g.lineTo(x + w / 2, by); g.lineTo(x + w / 2, H + 10); g.closePath(); g.fill(); if (pre) g.stroke();
			g.shadowBlur = 0;
			if (!pre && k > 0.5)
			{
				g.fillStyle = "rgba(0,0,0,0.45)"; rr(x - 10, by + 30, 20, 30, 9); g.fill();
				g.strokeStyle = GREY + "0.8)"; g.lineWidth = 3; g.beginPath(); g.moveTo(x, ty); g.lineTo(x, ty - 40); g.stroke(); g.fillStyle = GREY + "0.8)"; g.beginPath(); g.moveTo(x, ty - 40); g.lineTo(x + 26 + JS.sin(t * 9) * 4, ty - 32); g.lineTo(x, ty - 24); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te + 0.1) return false; double k = up(t); if (k <= 0.05) return false; double py = top ? H - p.y : p.y, by = H - hgt * k, ty = by - rf * k;
			if (Math.Abs(p.x - x) < w / 2 + p.r - 1 && py > by - p.r) return true;
			if (py < by && py > ty - p.r) { double hw = (w / 2 + 8) * (py - ty) / (by - ty); return Math.Abs(p.x - x) < hw + p.r - 2; }
			return false;
		};
		return h;
	}

	// a thorn branch: grows from the arena's side to a point and back, with thorns on both sides
	public static Hz Thorn(double side, double y, double len, double tw, double ta, double te, double th = 13)
	{
		Func<double, double> grow = t => t < ta ? 0 : t < ta + 0.16 ? (t - ta) / 0.16 : t < te ? 1 : clamp(1 - (t - te) / 0.25, 0, 1); double x0 = side < 0 ? 0 : W;
		Func<double, double> tip = k => side < 0 ? len * k : W - len * k;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) { double k0 = clamp((t - tw) / (ta - tw), 0, 1), xt0 = tip(1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k0)})"; g.fillRect(Math.Min(x0, xt0), y - th, len, th * 2); g.strokeStyle = $"rgba(255,42,109,{str(0.35 + 0.5 * k0)})"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(x0, y); g.lineTo(xt0, y); g.stroke(); g.setLineDash(new double[0]); return; }
			double k = grow(t); if (k <= 0) return; double xt = tip(k), a = Math.Min(x0, xt), b = Math.Max(x0, xt); g.save(); g.fillStyle = "rgba(255,42,109,0.45)"; g.fillRect(a, y - th, b - a, th * 2); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; g.fillRect(a, y - 4, b - a, 8);
			g.beginPath(); { double x = a + 14; int i = 0; for (; x < b - 6; x += 30, i++) { double s = i % 2 != 0 ? 1 : -1; g.moveTo(x - 7, y); g.lineTo(x + 3, y + s * th); g.lineTo(x + 7, y); } } g.fill();
			g.translate(xt, y); for (int i = 0; i < 5; i++) { g.rotate(TAU / 5); g.beginPath(); g.ellipse(9, 0, 10, 6, 0, 0, TAU); g.fill(); }
			g.fillStyle = "rgba(0,0,0,0.35)"; circle(0, 0, 5); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; double k = grow(t); if (k <= 0.05) return false; double xt = tip(k), a = Math.Min(x0, xt), b = Math.Max(x0, xt); return (Math.Abs(p.y - y) < th + p.r - 3 && p.x > a - p.r && p.x < b + p.r) || hypot(p.x - xt, p.y - y) < 18 + p.r - 2; };
		return h;
	}

	// a swirl: a spinning rose in the middle throwing notes in a spiral
	public static Hz Swirl(double x, double y, double tw, double ts, double te, double step, int arms, double rot, double sp)
	{
		double last = -1;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > te + 0.3) { h.dead = true; return; }
			if (t < ts) return; double s = Math.Floor((t - ts) / step + 1e-6); if (s == last) return; last = s; double t0 = ts + s * step; if (t0 >= te) return;
			for (int i = 0; i < arms; i++) { double a = s * rot + i * TAU / arms; hazards.Add(Note(x + JS.cos(a) * 26, y + JS.sin(a) * 26, JS.cos(a) * sp, JS.sin(a) * sp, t0, new NoteO { r = 9 })); }
		};
		h.draw = t =>
		{
			double k = t < ts ? clamp((t - tw) / (ts - tw), 0, 1) : t > te ? clamp(1 - (t - te) / 0.3, 0, 1) : 1; bool live = t >= ts && t < te; g.save(); g.translate(x, y); g.rotate(t * (live ? 4 : 1)); g.globalAlpha = live ? 1 : 0.3 + 0.4 * k; g.strokeStyle = PINK; g.lineWidth = 4;
			for (int i = 0; i < arms; i++) { g.save(); g.rotate(i * TAU / arms); g.beginPath(); for (int j = 0; j <= 20; j++) { double r = 4 + j * 2.2 * k, a = j * 0.22; if (j != 0) g.lineTo(JS.cos(a) * r, JS.sin(a) * r); else g.moveTo(JS.cos(a) * r, JS.sin(a) * r); } g.stroke(); g.restore(); }
			g.fillStyle = PINK; if (live) { g.shadowColor = PINK; g.shadowBlur = 16; } circle(0, 0, (live ? 20 : 12) * (1 + 0.15 * pulse)); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= ts && t < te && hypot(p.x - x, p.y - y) < 20 + p.r;
		return h;
	}

	// a curse: a magic circle with symbols draws itself and explodes (the circle hurts at the explosion) and throws notes
	public static Hz Curse(double x, double y, double R, double tw, double ta, int n, double sp, double a0 = 0)
	{
		bool done = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!done && t >= ta) { done = true; for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; hazards.Add(Note(x + JS.cos(a) * R * 0.4, y + JS.sin(a) * R * 0.4, JS.cos(a) * sp, JS.sin(a) * sp, ta, new NoteO { r = 9 })); } burst(x, y, PINK, 16, 320); shake = Math.Max(shake, 6); }
			if (t > ta + 0.3) h.dead = true;
		};
		h.draw = t =>
		{
			g.save(); g.translate(x, y);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.14 * k)})"; circle(0, 0, R); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 2.5; g.beginPath(); g.arc(0, 0, R, 0, TAU); g.stroke();
				g.rotate(t * 1.5); g.beginPath(); double m = Math.Ceiling(5 * k); for (int i = 0; i <= m; i++) { double a = -Math.PI / 2 + i * 2 * TAU / 5; if (i != 0) g.lineTo(JS.cos(a) * R * 0.85, JS.sin(a) * R * 0.85); else g.moveTo(JS.cos(a) * R * 0.85, JS.sin(a) * R * 0.85); } g.stroke();
				for (int i = 0; i < 8; i++) { double a = i * TAU / 8; g.fillRect(JS.cos(a) * (R + 7) - 2, JS.sin(a) * (R + 7) - 2, 4, 4); }
			}
			else { double k = clamp((t - ta) / 0.3, 0, 1); g.globalAlpha = 1 - k; g.fillStyle = k < 0.25 ? PINKW : PINK; circle(0, 0, R * (1 + 0.3 * k)); }
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= ta && t < ta + 0.18 && hypot(p.x - x, p.y - y) < R + p.r - 2;
		return h;
	}

	// a lantern searching for her: a cone of light sweeps from one angle to another (the pink light hurts)
	public static Hz Lantern(double px, double py, double a0, double a1, double hw, double tw, double ta, double te)
	{
		Func<double, double> ang = t => a0 + (a1 - a0) * (0.5 - 0.5 * JS.cos(Math.PI * clamp((t - ta) / (te - ta), 0, 1))); const double L = 1700;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(px, py);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, L, a0 - hw, a0 + hw); g.closePath(); g.fill();
				g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 10 }); g.beginPath(); g.arc(0, 0, 150, Math.Min(a0, a1), Math.Max(a0, a1)); g.stroke(); g.setLineDash(new double[0]);
			}
			else if (t < te + 0.25) { double a = ang(Math.Min(t, te)), f = t < te ? 1 : 1 - (t - te) / 0.25; g.globalAlpha = f; g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = "rgba(255,42,109,0.85)"; g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, L, a - hw, a + hw); g.closePath(); g.fill(); g.globalAlpha = 1; }
			g.shadowBlur = 0; g.fillStyle = GREY + "0.9)"; rr(-16, -22, 32, 40, 8); g.fill(); g.fillStyle = "rgba(255,42,109,0.9)"; circle(0, 0, 9); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double dx = p.x - px, dy = p.y - py, d = hypot(dx, dy); if (d < 24) return true; double da = JS.atan2(dy, dx) - ang(t); da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) < hw + (p.r - 1) / d; };
		return h;
	}

	// decoration: the night (the arena gets a bit darker, small grey stars)
	public static Hz NightSky(double t0, double t1)
	{
		var S = new List<double[]>(); for (int i = 0; i < 40; i++) S.Add(new double[] { (i * 397) % W, (i * 211) % H, (i % 3) + 1 });
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 0.8, (t1 - t) / 0.8); if (a <= 0) return; g.fillStyle = $"rgba(10,14,40,{str(0.4 * a)})"; g.fillRect(0, 0, W, H);
			foreach (var q in S) { double x = q[0], y = q[1], s = q[2]; g.fillStyle = GREY + str(a * (0.25 + 0.25 * JS.sin(t * 3 + x))) + ")"; g.fillRect(x, y, s, s); }
		};
		h.hit = (p, t) => false;
		return h;
	}

	// ===== the boss: the enchanted violin, a pink violin floating at the top, its bow plays and throws notes =====
	// when the music stops (2:26) it stops and charges white, and explodes with the big hit (2:29); the explosion is harmless
	public static Hz EnchantedViolin(double ts, double tc, double tx, Tier T)
	{
		double x = W / 2, y = -260, lastS = -1; bool ex = false; const double Y = 150;
		Func<double, (double, double)> pos = t => { double k = clamp((t - ts) / (SPB * 3), 0, 1), e = 1 - JS.pow(1 - k, 3), hold = t >= tc ? tc : t; return (W / 2 + JS.sin((hold - ts) * 0.55) * 230 * k, -260 + (Y + 260) * e); };
		void note(double a, double sp, double t0, NoteO o = null) => hazards.Add(Note(x + JS.cos(a) * 50, y + 40 + JS.sin(a) * 50, JS.cos(a) * sp, JS.sin(a) * sp, t0, o ?? new NoteO { r = 9 }));
		void fan(double t0, double n, double spr, double sp, double a0 = Math.PI / 2) { for (int k = 0; k < n; k++) note(a0 + (k - (n - 1) / 2) * spr, sp, t0); }
		void aim(double t0, double n, double spr, double sp) { if (player == null) return; fan(t0, n, spr, sp, JS.atan2(player.y - (y + 40), player.x - x)); }
		void ring(double t0, double n, double sp, double a0) { for (int k = 0; k < n; k++) note(a0 + k * TAU / n, sp, t0); }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			(x, y) = pos(t);
			if (t >= tx && !ex) { ex = true; burst(x, y, PINK, 90, 600); burst(x, y, "#fff", 50, 480); burst(x, y, "#ffb36b", 20, 380); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; return; }
			if (t < ts + SPB * 3 || t >= tc) return;
			double s = Math.Floor(BT(t) * 2 + 1e-6); if (s == lastS) return; lastS = s; double b = s / 2, t0 = B(b), q = s % 8;
			if (b < 232) { if (q % 2 == 0) fan(t0, 6, 0.24, T.sp * 0.85, Math.PI / 2 + (q % 4 != 0 ? 0.12 : -0.12)); else aim(t0, 3, 0.2, T.sp); }
			else if (b < 248) { if (q == 0) ring(t0, 16, T.sp * 0.6, b * 0.37); else if (q == 4) ring(t0, 14, T.sp * 0.65, b * 0.37 + 0.13); else if (q == 2 || q == 6) aim(t0, 3, 0.2, T.sp * 0.9); }
			else { if (q % 2 == 0) aim(t0, 3, 0.22, T.sp * 1.05); else if (q == 3 || q == 7) fan(t0, 4, 0.5, T.sp * 0.8, Math.PI / 2 + (q == 3 ? -0.6 : 0.6)); }
		};
		h.draw = t =>
		{
			if (t < ts - 0.1) return; bool ch = t >= tc && t < tx, live = t >= ts + SPB * 3 && t < tc; double jx = 0, jy = 0, s = 1;
			if (ch) { double k = (t - tc) / (tx - tc); s = 1 + 0.25 * k; jx = (random() * 2 - 1) * 7 * k; jy = (random() * 2 - 1) * 7 * k; }
			g.save(); g.translate(x + jx, y + jy); g.scale(s, s); g.globalAlpha = live || ch ? 1 : 0.45; string col = ch ? PINKW : PINK;
			// the body: a standing violin (the scroll on top)
			g.shadowColor = col; g.shadowBlur = ch ? 50 : 28; g.fillStyle = col; g.fillRect(-9, -175, 18, 110); circle(0, -182, 15);
			g.beginPath(); g.ellipse(0, -30, 48, 42, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(0, 48, 62, 52, 0, 0, TAU); g.fill(); g.fillRect(-36, -10, 72, 40);
			g.shadowBlur = 0; g.fillStyle = "#000"; foreach (var sd in new double[] { -1, 1 }) { g.beginPath(); g.ellipse(sd * 22, 10, 4, 22, sd * 0.15, 0, TAU); g.fill(); }
			g.fillRect(-16, 52, 32, 7);
			g.strokeStyle = "rgba(0,0,0,0.55)"; g.lineWidth = 1.5; for (int i = 0; i < 4; i++) { g.beginPath(); g.moveTo(-6 + i * 4, -170); g.lineTo(-9 + i * 6, 90); g.stroke(); }
			// the bow plays right and left with the beat
			double bo = live ? JS.sin(BT(t) * Math.PI) * 70 : 0; g.save(); g.rotate(-0.38); g.shadowColor = col; g.shadowBlur = 14; g.fillStyle = col; g.fillRect(-150 + bo, 18, 300, 6); g.fillRect(-160 + bo, 12, 18, 18); g.restore();
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ts + SPB * 3 || t >= tc) return false; double dx = p.x - x, dy = p.y - y;
			if (hypot(dx / (48 + p.r), (dy + 30) / (42 + p.r)) < 1 || hypot(dx / (62 + p.r), (dy - 48) / (52 + p.r)) < 1) return true;
			if (Math.Abs(dx) < 9 + p.r && dy > -197 - p.r && dy < -60) return true;
			double bo = JS.sin(BT(t) * Math.PI) * 70, c = JS.cos(-0.38), sn = JS.sin(-0.38); return obbHit(p.x, p.y, p.r, x + c * bo - sn * 21, y + sn * bo + c * 21, 150, 6, -0.38);
		};
		return h;
	}

	// decoration: a grey girl (the one he loved) standing, fades in and out
	public static Hz Girl(double x, double y, double t0, double t1, double sc = 1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 0.5, (t1 - t) / 0.5); if (a <= 0) return; g.save(); g.translate(x, y); g.scale(sc, sc); g.globalAlpha = a * 0.85; g.fillStyle = GREY + "0.8)"; g.strokeStyle = GREY + "0.8)"; g.lineWidth = 4; g.lineCap = "round";
			circle(0, -58, 13); g.beginPath(); g.moveTo(-15, -58); g.quadraticCurveTo(-22, -30, -16, -18); g.lineTo(-8, -40); g.closePath(); g.fill(); g.beginPath(); g.moveTo(15, -58); g.quadraticCurveTo(22, -30, 16, -18); g.lineTo(8, -40); g.closePath(); g.fill();
			g.beginPath(); g.moveTo(-9, -42); g.lineTo(9, -42); g.lineTo(26, 22); g.lineTo(-26, 22); g.closePath(); g.fill(); g.beginPath(); g.moveTo(-8, 22); g.lineTo(-9, 52); g.moveTo(8, 22); g.lineTo(9, 52); g.moveTo(-9, -36); g.lineTo(-24, -6 + JS.sin(t * 3) * 4); g.moveTo(9, -36); g.lineTo(24, -6 - JS.sin(t * 3) * 4); g.stroke();
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// lovers: two big hearts come from two sides and meet; when they meet they burst into small hearts
	public static Hz HeartMeet(double x0, double y0, double x1, double y1, double mx, double my, double tw, double ta, double tm, int n, double sp, double S = 34)
	{
		Func<double, int, (double, double)> pos = (t, i) => { double k = clamp((t - ta) / (tm - ta), 0, 1), e = k < 0.5 ? 2 * k * k : 1 - JS.pow(-2 * k + 2, 2) / 2; var (ax, ay) = i != 0 ? (x1, y1) : (x0, y0); return (ax + (mx - ax) * e, ay + (my - ay) * e + JS.sin(k * Math.PI * 3) * 30 * (i != 0 ? 1 : -1)); };
		bool done = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!done && t >= tm) { done = true; for (int i = 0; i < n; i++) { double a = i * TAU / n + 0.2; hazards.Add(Note(mx, my, JS.cos(a) * sp, JS.sin(a) * sp, tm, new NoteO { heart = true, r = 12 })); } burst(mx, my, PINK, 24, 360); burst(mx, my, "#fff", 10, 260); shake = Math.Max(shake, 8); }
			if (t > tm + 0.2) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.35 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(mx, my); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]); g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.25 * k)})"; circle(mx, my, 8 + 10 * k); }
			if (t >= tm) { double k = clamp((t - tm) / 0.2, 0, 1); g.globalAlpha = 1 - k; g.fillStyle = "#fff"; g.save(); g.translate(mx, my); heartPath(S * 1.3 * (1 + k)); g.fill(); g.restore(); g.globalAlpha = 1; return; }
			for (int i = 0; i < 2; i++) { var (x, y) = pos(Math.Max(t, ta), i); g.save(); g.translate(x, y); g.globalAlpha = t < ta ? 0.35 : 1; g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; heartPath(S * (1 + 0.1 * pulse)); g.fill(); g.restore(); g.globalAlpha = 1; }
		};
		h.hit = (p, t) => { if (t < ta || t >= tm) return false; for (int i = 0; i < 2; i++) { var (x, y) = pos(t, i); if (hypot(p.x - x, p.y - y) < S * 0.85 + p.r - 2) return true; } return false; };
		return h;
	}

	/* ================= level: Fairytale (Twesii's pick) =================
	   Alexander Rybak, from the start to 2:30. 108 bpm (a beat is 0.56 s), the grid starts at 0.445
	   0 violin intro | 16 drums | 34 first verse | 66 chorus | 98 violin
	   132 save -> second verse | 200 save -> chorus and boss | 232 calm | 248 violin | 262 silence | 268 explodes */
	// the violin melody: beat:string (from the song's own sound)
	static readonly Dictionary<string, string> FT_MEL = new()
	{
		["intro"] = "0.75:3;1.0:3;2.0:3;2.25:2;2.75:3;3.25:3;3.5:3;3.75:3;4.25:2;4.75:3;5.25:1;5.5:2;6.0:3;6.25:2;6.75:3;7.25:2;7.75:1;8.25:3;8.75:3;9.25:3;9.5:3;10.0:0;10.75:0;11.0:0;11.25:0;11.5:0;11.75:3;12.0:1;12.25:1;12.5:1;12.75:2;13.25:1;13.5:1;14.0:3;14.25:3;14.5:1;15.25:1",
		["riff1"] = "17.25:3;17.75:3;23.75:3;24.0:3;24.5:3;25.75:0;26.0:0;26.5:0;26.75:0;27.0:0;27.25:0;27.5:0;27.75:0;28.0:1;28.25:1;28.5:1;29.0:1;29.25:1;29.75:1;31.25:1;32.5:1;33.25:1",
		["riff2"] = "98.5:1;99.0:1;99.25:1;99.5:1;99.75:1;105.75:3;106.0:3;106.5:3;107.5:0;107.75:0;108.0:0;108.75:0;109.0:0;110.0:2;110.5:1;110.75:1;111.0:1;112.5:1;112.75:1;113.25:1;113.75:1;114.75:1;115.0:1;115.25:1;115.75:1;116.0:1;116.25:1;116.75:1;117.25:1;117.5:1;121.75:3;122.0:3;122.25:3;122.5:3;125.5:1;125.75:1;126.25:1;126.75:1;127.25:1;128.75:1;129.25:1;129.5:1;130.25:1;130.75:1;131.0:1;131.25:1;131.5:1",
		["bridge"] = "232.75:3;233.0:3;234.0:3;234.25:2;234.75:3;235.25:3;235.5:2;235.75:3;236.25:2;236.75:3;237.25:1;237.5:2;238.0:3;238.25:2;238.75:3;239.25:2;239.75:1;240.25:3;240.75:3;241.25:3;242.0:0;242.5:0;242.75:0;243.0:0;243.25:0;243.5:3;243.75:3;244.0:2;244.25:1;244.5:1;245.0:1;245.5:1;246.0:3;246.25:3;246.5:1;247.0:1;247.25:1;247.5:1",
		["solo"] = "248.25:1;248.75:1;249.0:1;249.25:1;249.75:1;250.25:1;250.5:1;250.75:1;251.0:1;251.25:1;251.5:1;251.75:1;252.0:1;258.0:0;258.5:0;258.75:0;259.0:0;259.5:0;259.75:0;260.0:1;260.5:1;260.75:1;261.0:1;261.5:1",
	};

	// AFKMK (line 6520)
	static readonly int _afkF5 = AfkReg("buildFriend5", (x, y, b, v, w) => One(v >= 2 ? PageTurn(v % 2 != 0 ? -1 : 1, B(b - w), B(b), B(b + 2), v < 4 ? 170 : 12, v < 4 ? H - 12 : H - 170) : Pluck(x, y, v != 0 ? Math.PI / 2 : 0, B(b - w), B(b), B(b + 0.6))));

	public static List<Ev> buildFriend5()
	{
		var E = new List<Ev>(); Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		var TI = ezT(new[] { new Tier { sp = 290, wn = 1.25 }, new Tier { sp = 305, wn = 1.15 }, new Tier { sp = 320, wn = 1.05 } });
		var LANES = new double[] { 135, 255, 375, 495, 615 };
		var LSH = new double[] { 75, 315, 435, 555, 675 }; var LPH = new double[] { 0, 1, 2, 3 };
		double laneP(int i, int ph) => ph == 1 ? LSH[i] : LANES[i] + new double[] { 0, 0, 30, -30 }[ph];
		double lane(int i, double b) => laneP(i, (int)(Math.Floor(b + 1e-6) % 4));
		void pluck(double hit, int i, Tier T, double dur = 0.6) => ev(hit - T.wn, () => hazards.Add(Pluck(W / 2, STR_Y[i], 0, B(hit - T.wn), B(hit), B(hit + dur))));
		void vpluck(double hit, double x, Tier T, double dur = 0.6) => ev(hit - T.wn, () => hazards.Add(Pluck(x, H / 2, Math.PI / 2, B(hit - T.wn), B(hit), B(hit + dur))));
		int bowN = 0;
		void bow(double hit, double y, double dir, Tier T, double dur = 1.5, bool lance = false) { int i = Array.IndexOf(LANES, y); if (i >= 0 && !lance) y = 75 + 30 * (((bowN++) * 8) % 21); ev(hit - T.wn, () => hazards.Add(BowStroke(false, y, dir, B(hit - T.wn), B(hit), B(hit + dur), lance, 620, lance ? 13 : 19))); }
		int pgN = 0;
		void page(double hit, double dir, Tier T, double dur = 2) { bool top = (pgN++) % 2 == 0; ev(hit - T.wn - 0.5, () => hazards.Add(PageTurn(dir, B(hit - T.wn - 0.5), B(hit), B(hit + dur), top ? 170 : 12, top ? H - 12 : H - 170))); }
		// when the page reaches the middle its letters (notes) fall off toward the turn
		void letters(double hit, double dir, Tier T, double dur = 2, double n = 7) => ev(hit + dur / 2, () => { double t0 = B(hit + dur / 2); for (int i = 0; i < n; i++) { double y = 40 + i * (H - 80) / (n - 1) + (Math.Floor(hit) % 2 != 0 ? (H - 80) / (n - 1) / 2 : 0); hazards.Add(Note(W / 2, y, -dir * T.sp * 0.75, (i - (n - 1) / 2) * 14, t0, new NoteO { r = 10 })); } });
		void turret(double hit, double x, double h, Tier T, double dur = 1.5, double w = 110) => ev(hit - T.wn, () => hazards.Add(Turret(x, w, h, false, B(hit - T.wn), B(hit), B(hit + dur))));
		void spark(double hit, double x, double h, Tier T) => ev(hit, () => { double t0 = B(hit); for (int i = 0; i < 3; i++) { double a = -Math.PI / 2 + (i - 1) * 0.45; hazards.Add(Note(x, H - h - 60, JS.cos(a) * T.sp * 0.7, JS.sin(a) * T.sp * 0.7, t0, new NoteO { r = 9, ay = 300 })); } });
		void thorn(double hit, double side, double y, double len, Tier T, double dur = 1.5) => ev(hit - T.wn, () => hazards.Add(Thorn(side, y, len, B(hit - T.wn), B(hit), B(hit + dur))));
		void curse(double hit, double x, double y, Tier T, double R = 80, int n = 8, double a0 = 0) => ev(hit - T.wn, () => hazards.Add(Curse(x, y, R, B(hit - T.wn), B(hit), n, T.sp * 0.75, a0)));
		void curseP(double hit, Tier T, double R = 80, int n = 8) => ev(hit - T.wn, () => { double x = clamp(player != null ? player.x : W / 2, R + 10, W - R - 10), y = clamp(player != null ? player.y : H / 2, R + 10, H - R - 10); hazards.Add(Curse(x, y, R, B(hit - T.wn), B(hit), n, T.sp * 0.75, hit * 0.7)); });
		void swirl(double b0, double b1, double x, double y, Tier T, int arms = 4, double step = 0.5, double rot = 0.42, double sp = 0) => ev(b0 - T.wn, () => hazards.Add(Swirl(x, y, B(b0 - T.wn), B(b0), B(b1), step * SPB, arms, rot, or5(sp, T.sp * 0.65))));
		void lantern(double hit, bool left, Tier T, double dur, double f0, double f1, double hw = 0.085) => ev(hit - T.wn, () => { double px = left ? 40 : W - 40, a0 = left ? f0 : Math.PI - f0, a1 = left ? f1 : Math.PI - f1; hazards.Add(Lantern(px, 30, a0, a1, hw, B(hit - T.wn), B(hit), B(hit + dur))); });
		void meet(double hit, double x0, double y0, double x1, double y1, Tier T, int n = 14, double glide = 4) => ev(hit - glide - T.wn, () => hazards.Add(HeartMeet(x0, y0, x1, y1, W / 2, H / 2, B(hit - glide - T.wn), B(hit - glide), B(hit), n, T.sp * 0.7)));
		void fanAt(double hit, double x, double y, double n, double a0, double spr, Tier T, double k = 0.8, NoteO o = null) { o ??= new NoteO { r = 9 }; ev(hit, () => { double t0 = B(hit); for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2) * spr; hazards.Add(Note(x, y, JS.cos(a) * T.sp * k, JS.sin(a) * T.sp * k, t0, o)); } }); }
		// melody notes: every note the violin plays comes from the right and travels on a line between the strings (the high string on top)
		void melody(string key, Tier T, double k = 1.25, double from = -1e9, double to = 1e9, double dir = -1, bool echo = true)
		{
			double last = -1; int run = 0, cnt = 0;
			foreach (var it in FT_MEL[key].Split(';'))
			{
				var pr = it.Split(':'); double b = num(pr[0]), sv = num(pr[1]); if (b < from || b >= to) continue; run = sv == last ? run + 1 : 0; last = sv; int ln = (int)sv + (run % 2);
				int ph = new[] { 0, 2, 1, 3 }[(cnt++) % 4]; double y1 = laneP(ln, ph), y2 = laneP(4 - ln, ph);
				ev(b, () => { hazards.Add(Note(dir < 0 ? W + 20 : -20, y1, dir * T.sp * k, 0, B(b), new NoteO { r = 12 })); if (echo) hazards.Add(Note(dir < 0 ? -20 : W + 20, y2, -dir * T.sp * k * 0.8, 0, B(b), new NoteO { r = 11 })); });
			}
		}
		E.Add(new Ev { b = 0, keep = true, f = () => hazards.Add(ViolinStrings(B(0), B(262))) });

		/* ---------- part 1 (0:00 - 1:14) ---------- */
		Tier T = TI[0];
		// 0:00 the violin intro: every note it plays travels on its line, and the strings pull at the end of the phrase
		melody("intro", T, 1.15);
		foreach (var (b, i) in new[] { (4.0, 0), (8.0, 3), (10.0, 1), (12.0, 2), (14.0, 0), (15.0, 3) }) pluck(b, i, T, 0.7);
		// 0:09 the drums: the bow plays on the lines, the melody goes on
		for (int b = 16; b < 32; b += 2) { int k = (b - 16) / 2; bow(b, LANES[new[] { 0, 4, 1, 3, 0, 4, 2, 2 }[k]], k % 2 != 0 ? -1 : 1, T, 1.6); }
		melody("riff1", T, 1.3);
		foreach (var b in new[] { 19, 23, 27, 31 }) pluck(b + 0.5, b % 8 == 3 ? 1 : 2, T, 0.5);
		// 0:19 "Years ago, when I was younger": the storybook opens and its pages turn
		ev(33, () => hazards.Add(StoryBook(B(33), B(66))));
		page(34, -1, T, 2.4); page(38, 1, T, 2.4); letters(34, -1, T, 2.4, 10); letters(38, 1, T, 2.4, 10);
		foreach (var b in new[] { 36, 37, 40, 41 }) pluck(b + 0.5, new[] { 0, 3, 3, 0 }[b % 4], T, 0.5);
		// 0:23 "I kinda liked a girl I knew": the girl in the middle, hearts come out of her in rings with an opening
		ev(41.5, () => hazards.Add(Girl(W / 2, H / 2 + 10, B(41.5), B(50))));
		foreach (var b in new double[] { 42, 44, 46, 48 }) ev(b, () => { double t0 = B(b), gap = (b * 0.9) % TAU; for (int i = 0; i < 16; i++) { double a = i * TAU / 16; double d = JS.atan2(JS.sin(a - gap), JS.cos(a - gap)); if (Math.Abs(d) < 0.42) continue; hazards.Add(Note(W / 2 + JS.cos(a) * 30, H / 2 + JS.sin(a) * 30, JS.cos(a) * T.sp * 0.6, JS.sin(a) * T.sp * 0.6, t0, new NoteO { heart = true, r = 12 })); } });
		foreach (var b in new[] { 43, 45, 47, 49 }) bow(b, b % 4 == 3 ? LANES[0] : LANES[4], b % 4 == 3 ? 1 : -1, T, 1.4);
		// 0:28 "She was mine and we were sweethearts": two hearts meet in the middle and burst into hearts
		meet(54, -80, H * 0.3, W + 80, H * 0.7, T, 20, 4); meet(58, W * 0.25, -80, W * 0.75, H + 80, T, 20, 3.5);
		foreach (var b in new[] { 51, 53, 55, 57 }) pluck(b, new[] { 0, 3, 1, 2 }[(b - 51) / 2], T, 0.5);
		// 0:32 "That was then": a page turns back
		page(60, -1, T, 1.6); letters(60, -1, T, 1.6, 9);
		/* the chorus (0:37) */
		// "I'm in love with a fairytale": the fairytale castle is built tower by tower from the left, then from the right
		for (int k = 0; k < 8; k++) { double h = new double[] { 250, 330, 280, 360, 260, 340, 290, 320 }[k]; turret(66 + k * 0.5, 80 + k * 160, h, T, 2); spark(66 + k * 0.5, 80 + k * 160, h, T); }
		for (int k = 0; k < 8; k++) { double h = new double[] { 330, 260, 350, 280, 330, 250, 360, 290 }[k]; turret(70 + k * 0.5, W - 80 - k * 160, h, T, 2); spark(70 + k * 0.5, W - 80 - k * 160, h, T); }
		foreach (var b in new[] { 67, 69, 71, 73 }) bow(b, b % 4 == 3 ? LANES[0] : LANES[1], b % 4 == 3 ? 1 : -1, T, 1.4);
		// "even though it hurts": thorn and rose branches from both sides
		for (int b = 74; b < 82; b++) { bool L = b % 2 == 0; double o = new double[] { 0, 120, 60, 180 }[b % 4]; for (int i = 0; i < 3; i++) { double y = 60 + o + i * 240; if (y > H - 20) continue; thorn(b, L ? -1 : 1, y, 800, T, 1.3); } }
		// "if I lose my mind": a swirl in the middle and the strings get dizzy
		swirl(82, 90, W / 2, H / 2, T, 5, 0.5, 0.45);
		{ double t0 = B(82), om = 0.6; foreach (var b in new double[] { 82.5, 83, 84, 85, 86, 87, 88, 89 }) { double a0 = (Math.Floor(b) % 2 != 0 ? 0 : Math.PI / 2); ev(b - T.wn, () => hazards.Add(Pluck(W / 2, H / 2, t => a0 + (t - t0) * om, B(b - T.wn), B(b), B(b + 0.6)))); } }
		// "I'm already cursed": curse circles on your spot, and between each a fixed curse star
		foreach (var b in new[] { 90, 92, 94, 96 }) curseP(b, T, 85, 12);
		foreach (var b in new[] { 91, 93, 95, 97 }) curse(b, b % 4 == 3 ? W * 0.25 : W * 0.75, b % 4 == 1 ? H * 0.3 : H * 0.7, T, 70, 6, b * 0.5);
		// 0:55 the violin: the melody travels on the lines, the bow at the start, the strings pull across at the end of every phrase
		melody("riff2", T, 1.3);
		for (int k = 0; k < 8; k++) { double b = 98 + k * 4; bow(b, LANES[new[] { 4, 0, 3, 1, 4, 0, 2, 2 }[k]], k % 2 != 0 ? -1 : 1, T, 1.4); vpluck(b + 2.5, W * (0.2 + 0.6 * ((k * 3) % 5) / 4), T, 0.5); vpluck(b + 0.5, W * (0.8 - 0.6 * ((k * 3) % 5) / 4), T, 0.5); }
		pluck(130, 1, T, 0.6); pluck(130, 2, T, 0.6);
		// 1:14 the first checkpoint
		/* ---------- part 2 (1:14 - 1:51) ---------- */
		T = TI[1];
		ev(132, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		// "Every day we started fighting": knights with lances from both sides on two lines side by side, they clash in the middle
		for (int k = 0; k < 4; k++)
		{
			double b = 132 + k * 2; int i = new[] { 0, 2, 1, 3 }[k]; double y1 = LANES[i], y2 = LANES[i + 1]; bow(b, y1, 1, T, 1.6, true); bow(b, y2, -1, T, 1.6, true);
			double tc = (W / 2 + 20) / ((W + 620 + 40) / 1.6); curse(b + tc, W / 2, (y1 + y2) / 2, T, 40, 6, k);
		}
		// "Every night we fell in love": the night, and heart lanterns rising
		ev(139.5, () => hazards.Add(NightSky(B(139.5), B(148))));
		for (int b_ = 140; b_ < 148; b_++) { int b = b_; ev(b, () => { double t0 = B(b); for (int i = 0; i < 7; i++) { if (i == (b * 2) % 7) continue; hazards.Add(Note((i + 0.5 + (b % 2) * 0.5) * W / 7.5, H + 30, 0, -T.sp * 0.6, t0, new NoteO { heart = true, r = 12, sw = 30, sf = 4, ph = i })); } }); }
		// "No one else could make me sadder": notes falling with gravity
		for (int b_ = 148; b_ < 156; b_++) { int b = b_; ev(b, () => { double t0 = B(b); int gp = (b * 3) % 9; for (int i = 0; i < 11; i++) { if (i == gp || i == gp + 1) continue; hazards.Add(Note((i + 0.5 + (b % 2) * 0.5) * W / 11, -20, 0, 40, t0, new NoteO { ay = 420, r = 10 })); } }); }
		// "But no one else could lift me high above": notes rising from below and speeding up
		for (int b_ = 156; b_ < 164; b_++) { int b = b_; ev(b, () => { double t0 = B(b); int gp = (b * 5) % 9; for (int i = 0; i < 11; i++) { if (i == gp || i == gp + 1) continue; hazards.Add(Note((i + 0.5 + (b % 2) * 0.5) * W / 11, H + 20, 0, -40, t0, new NoteO { ay = -420, r = 10 })); } }); }
		foreach (var b in new[] { 150, 154, 158, 162 }) bow(b + 0.5, LANES[b % 8 == 6 ? 1 : 3], b % 8 == 6 ? 1 : -1, T, 1.3);
		// "I don't know what I was doing": the strings get dizzy and turn around the middle
		{
			double t0 = B(164), om = 0.9; for (int b_ = 164; b_ < 172; b_++) { int b = b_; double a0 = (b % 2 != 0 ? Math.PI / 2 : 0); ev(b - T.wn, () => hazards.Add(Pluck(W / 2, H / 2, t => a0 + (t - t0) * om, B(b - T.wn), B(b), B(b + 0.6)))); }
			foreach (var b in new[] { 165, 167, 169, 171 }) fanAt(b, b % 4 == 1 ? W * 0.12 : W * 0.88, b % 4 == 1 ? H * 0.15 : H * 0.85, 6, b % 4 == 1 ? Math.PI / 4 : -Math.PI * 3 / 4, 0.22, T);
		}
		// "When suddenly we fell apart": the book splits in two halves and they fly
		ev(170, () => hazards.Add(StoryBook(B(170), B(172.2))));
		ev(172 - T.wn, () => hazards.Add(Laser(new LaserO { x = W / 2, y = H / 2, ang = Math.PI / 2, w = 40, len = 3200, tw = B(172 - T.wn), ta = B(172), te = B(172.5) })));
		ev(172, () => { hazards.Add(BookHalf(-1, B(172), T.sp * 1.3)); hazards.Add(BookHalf(1, B(172), T.sp * 1.3)); });
		ev(176 - T.wn, () => hazards.Add(Laser(new LaserO { x = W / 2, y = H / 2, ang = 0, w = 40, len = 3200, tw = B(176 - T.wn), ta = B(176), te = B(176.5) })));
		ev(176, () => { hazards.Add(BookHalf(-1, B(176), T.sp * 0.9, true)); hazards.Add(BookHalf(1, B(176), T.sp * 0.9, true)); });
		foreach (var b in new[] { 173, 175, 177, 179 }) fanAt(b, W / 2, H / 2, 10, b * 0.6, TAU / 10, T, 0.7);
		// "Nowadays I cannot find her": night, the girl shows far away and fades, and lanterns search for her
		ev(179.5, () => hazards.Add(NightSky(B(179.5), B(188))));
		foreach (var (b, x, y) in new[] { (180.0, W * 0.82, H * 0.72), (182.0, W * 0.18, H * 0.72), (184.0, W * 0.8, H * 0.3), (186.0, W * 0.2, H * 0.35) }) ev(b, () => hazards.Add(Girl(x, y, B(b), B(b + 1.8), 0.8)));
		lantern(180, true, T, 2, 0.12, 0.95); lantern(182, false, T, 2, 0.12, 0.95); lantern(184, true, T, 2, 1.45, 0.6); lantern(186, false, T, 2, 1.45, 0.6);
		foreach (var b in new[] { 181, 183, 185, 187 }) pluck(b + 0.5, new[] { 3, 0, 2, 1 }[(b - 181) / 2], T, 0.5);
		// "brand new start": new pages turn, every page throws notes from the side
		page(188, -1, T, 1.8); page(190, 1, T, 1.8); page(192, -1, T, 1.5); page(194, 1, T, 1.5); foreach (var (b, d, u) in new[] { (188.0, -1.0, 1.8), (190.0, 1.0, 1.8), (192.0, -1.0, 1.5), (194.0, 1.0, 1.5) }) letters(b, d, T, u);
		foreach (var b in new[] { 189, 191, 193, 195 }) fanAt(b, b % 4 == 1 ? W : 0, H / 2, 7, b % 4 == 1 ? Math.PI : 0, 0.28, T, 0.8);
		// 1:51 the second checkpoint
		/* ---------- part 3 (1:51 - 2:30) ---------- */
		T = TI[2]; Tier TT = T;
		ev(200, () => { flash = Math.Max(flash, 0.3); shake = Math.Max(shake, 12); hazards.Add(EnchantedViolin(B(200), B(262), B(268), TT)); });
		// the chorus with the boss: the castle, the thorns, the swirl, the curse
		for (int k = 0; k < 8; k++) turret(201 + k * 0.5, 80 + k * 160, new double[] { 240, 300, 260, 320, 250, 310, 270, 300 }[k], T, 1.8);
		for (int k = 0; k < 8; k++) turret(205 + (7 - k) * 0.5, 80 + k * 160, new double[] { 300, 250, 310, 260, 300, 240, 320, 260 }[k], T, 1.8);
		for (int b = 208; b < 216; b++) { bool L = b % 2 == 0; double o = new double[] { 0, 120, 60, 180 }[b % 4]; for (int i = 0; i < 3; i++) { double y = 60 + o + i * 240; if (y > H - 20) continue; thorn(b, L ? -1 : 1, y, 740, T, 1.2); } }
		swirl(216, 224, W / 2, H * 0.62, T, 3, 0.5, 0.5, T.sp * 0.55);
		foreach (var b in new[] { 224, 226, 228, 230 }) curseP(b, T, 75, 10);
		// 2:10 the calm ("She's a fairytale"): the violin plays softly, its melody travels on the lines from the left, and two pages
		melody("bridge", T, 1.0, 0, 1e9, 1);
		page(236, -1, T, 2.4); page(244, 1, T, 2.4); letters(236, -1, T, 2.4, 9); letters(244, 1, T, 2.4, 9);
		// 2:18 the violin: the fast melody and bows on the lines
		melody("solo", T, 1.4);
		for (int b = 248; b < 262; b += 2) bow(b, LANES[new[] { 0, 4, 1, 3, 0, 4, 2 }[(b - 248) / 2]], b % 4 != 0 ? -1 : 1, T, 1.2);
		/* ---------- the rhythm layer: strings and bows on the beats ---------- */
		T = TI[0];
		for (int b = 1; b < 16; b += 2) pluck(b + 0.5, new[] { 3, 1, 2, 0 }[(b >> 1) % 4], T, 0.5);
		foreach (var b in new[] { 2, 6, 10, 14 }) bow(b, LANES[new[] { 0, 4, 1, 3 }[(b - 2) / 4]], b % 8 == 2 ? 1 : -1, T, 1.8);
		for (int b = 17; b < 32; b++) pluck(b + 0.5, new[] { 1, 2, 0, 3 }[b % 4], T, 0.45);
		for (int b = 34; b < 42; b++) if (b % 4 != 0) bow(b + 0.5, LANES[(b * 2) % 5], b % 2 != 0 ? 1 : -1, T, 1.3);
		foreach (var b in new double[] { 42.5, 43.5, 44.5, 45.5, 46.5, 47.5, 48.5, 49.5 }) ev(b, () => { double t0 = B(b), gap = (b * 1.3) % TAU; for (int i = 0; i < 12; i++) { double a = i * TAU / 12; double d = JS.atan2(JS.sin(a - gap), JS.cos(a - gap)); if (Math.Abs(d) < 0.5) continue; hazards.Add(Note(W / 2 + JS.cos(a) * 30, H / 2 + JS.sin(a) * 30, JS.cos(a) * T.sp * 0.5, JS.sin(a) * T.sp * 0.5, t0, new NoteO { heart = true, r = 11 })); } });
		for (int b = 50; b < 62; b++) if (b % 2 == 0) bow(b + 0.5, LANES[(b * 3) % 5], b % 4 != 0 ? 1 : -1, T, 1.3);
		for (int b = 66; b < 74; b++) bow(b + 0.5, LANES[new[] { 0, 1, 0, 2 }[b % 4]], b % 2 != 0 ? 1 : -1, T, 1.2);
		for (int b = 74; b < 82; b++) pluck(b + 0.5, new[] { 1, 2 }[b % 2], T, 0.4);
		for (int b = 82; b < 90; b++) bow(b + 0.5, LANES[new[] { 0, 4 }[b % 2]], b % 2 != 0 ? 1 : -1, T, 1.2);
		for (int b = 90; b < 98; b++) pluck(b + 0.5, new[] { 0, 3, 1, 2 }[b % 4], T, 0.4);
		for (int b = 98; b < 130; b++) if (b % 4 != 0) pluck(b + 0.5, new[] { 2, 0, 3, 1 }[b % 4], T, 0.45);
		for (int b = 99; b < 130; b += 2) bow(b, LANES[(b * 3) % 5], b % 4 == 1 ? -1 : 1, T, 1.3);
		T = TI[1];
		for (int b = 133; b < 140; b += 2) bow(b, LANES[(b * 2) % 5], b % 4 == 1 ? 1 : -1, T, 1.6, true);
		for (int b = 140; b < 148; b++) pluck(b + 0.5, new[] { 0, 3, 1, 2 }[b % 4], T, 0.45);
		for (int b_ = 148; b_ < 164; b_++) { int b = b_; ev(b + 0.5, () => { double t0 = B(b + 0.5); bool up = b >= 156; int gp = (b * 7 + 3) % 9; for (int i = 0; i < 11; i++) { if (i == gp || i == gp + 1) continue; hazards.Add(Note((i + 0.25 + (b % 2) * 0.5) * W / 11, up ? H + 20 : -20, 0, up ? -40 : 40, t0, new NoteO { ay = up ? -420 : 420, r = 10 })); } }); }
		for (int b = 164; b < 172; b++) bow(b + 0.5, LANES[(b * 3) % 5], b % 2 != 0 ? 1 : -1, T, 1.3);
		for (int b = 172; b < 180; b++) if (b != 172 && b != 176) pluck(b + 0.5, new[] { 0, 3, 1, 2 }[b % 4], T, 0.45);
		for (int b = 180; b < 188; b++) bow(b, LANES[(b * 2) % 5], b % 2 != 0 ? 1 : -1, T, 1.4);
		for (int b = 188; b < 196; b++) pluck(b + 0.5, new[] { 1, 2, 0, 3 }[b % 4], T, 0.45);
		T = TI[1];
		for (int b_ = 140; b_ < 148; b_++) { int b = b_; ev(b + 0.5, () => { double t0 = B(b + 0.5); for (int i = 0; i < 6; i++) { if (i == (b * 3 + 1) % 6) continue; hazards.Add(Note((i + 0.7) * W / 6.4, H + 30, 0, -T.sp * 0.6, t0, new NoteO { heart = true, r = 12, sw = 30, sf = 4, ph = i + 2 })); } }); }
		foreach (var b in new[] { 174, 178 }) fanAt(b, W / 2, H / 2, 12, b * 0.4, TAU / 12, T, 0.65);
		foreach (var b in new[] { 181, 183, 185, 187 }) fanAt(b, b % 4 == 1 ? W * 0.82 : W * 0.18, H * 0.5, 8, 0, TAU / 8, T, 0.6, new NoteO { heart = true, r = 11 });
		T = TI[0];
		// the thorn roses drop petals
		for (int b_ = 74; b_ < 82; b_++) { int b = b_; ev(b + 0.6, () => { double t0 = B(b + 0.6); bool L = b % 2 == 0; double o = new double[] { 0, 120, 60, 180 }[b % 4]; for (int i = 0; i < 3; i++) { double y = 60 + o + i * 240; if (y > H - 20) continue; double x = L ? 800 : W - 800; foreach (var a in new[] { Math.PI / 2 - 0.5, Math.PI / 2 + 0.5 }) hazards.Add(Note(x, y, JS.cos(a) * T.sp * 0.5, JS.sin(a) * T.sp * 0.5, t0, new NoteO { heart = true, r = 10, ay = 200 })); } }); }
		T = TI[0];
		for (int b_ = 16; b_ < 32; b_ += 4) { int b = b_; ev(b + 2, () => { double t0 = B(b + 2); for (int i = 0; i < 5; i++) hazards.Add(Note(b % 8 != 0 ? -20 : W + 20, LANES[i], (b % 8 != 0 ? 1 : -1) * T.sp * 0.9, 0, t0, new NoteO { r = 11 })); }); }
		for (int b_ = 50; b_ < 58; b_ += 2) { int b = b_; ev(b + 1, () => { double t0 = B(b + 1); bool L = b % 4 == 0; for (int i = 0; i < 5; i++) { if (i == ((b / 2) % 5)) continue; hazards.Add(Note(L ? -20 : W + 20, LANES[i], (L ? 1 : -1) * T.sp * 0.85, 0, t0, new NoteO { heart = true, r = 11 })); } }); }
		T = TI[1];
		for (int b_ = 132; b_ < 140; b_ += 2) { int b = b_; ev(b + 1, () => { double t0 = B(b + 1); for (int i = 0; i < 5; i++) { if (i == ((b / 2) % 5)) continue; hazards.Add(Note(W / 2, LANES[i], (b % 4 != 0 ? 1 : -1) * T.sp * 0.8, 0, t0, new NoteO { r = 10 })); hazards.Add(Note(W / 2, LANES[i], (b % 4 != 0 ? -1 : 1) * T.sp * 0.8, 0, t0, new NoteO { r = 10 })); } }); }
		T = TI[2];
		for (int b = 200; b < 232; b++) bow(b + 0.5, LANES[(b * 3) % 5], b % 2 != 0 ? 1 : -1, T, 1.2);
		for (int b = 200; b < 232; b += 2) pluck(b + 1.5, new[] { 2, 1, 0, 3 }[(b >> 1) % 4], T, 0.45);
		for (int b = 232; b < 248; b++) pluck(b + 0.5, new[] { 0, 3, 2, 1 }[b % 4], T, 0.45);
		for (int b = 233; b < 248; b += 2) bow(b, LANES[(b * 3) % 5], b % 4 == 1 ? 1 : -1, T, 1.3);
		for (int b = 248; b < 262; b++) pluck(b + 0.5, new[] { 0, 2, 1, 3 }[b % 4], T, 0.4);
		// standing bows: the bow comes down (or up) on a column, so you move right and left too
		{
			int k = 0; void vb(double b, Tier T) { double x = 70 + ((k * 5) % 12) * 104, d = k % 2 != 0 ? -1 : 1; Tier TT = T; k++; ev(b - T.wn, () => hazards.Add(BowStroke(true, x, d, B(b - TT.wn), B(b), B(b + 1.3), false, 420, 19))); }
			for (int b = 35; b < 66; b += 4) if (b != 59 && b != 63) vb(b, TI[0]);
			for (int b = 101; b < 130; b += 2) vb(b, TI[0]);
			for (int b = 189; b < 198; b += 2) vb(b, TI[1]);
		}
		// thorn branches crawl on the floor and the ceiling every phrase, so the edges are never safe
		for (int b_ = 18; b_ < 260; b_ += 4) { int b = b_; if (b >= 170 && b < 178) continue; T = b < 132 ? TI[0] : b < 200 ? TI[1] : TI[2]; bool fl = ((b - 18) / 4) % 2 == 0; Tier TT_ = T; ev(b - T.wn, () => hazards.Add(Thorn(fl ? -1 : 1, fl ? H - 22 : 22, W + 40, B(b - TT_.wn), B(b), B(b + 1.2), 30))); }
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
