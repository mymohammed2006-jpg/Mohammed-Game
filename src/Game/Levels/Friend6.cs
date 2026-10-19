using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 1967-2226: the shapes of "طنش" (Bubble, Flame, Lips, Finger, BurnHeart, Stomp, Tanoura, TalkWall, EdgeHint, Word, Aathool) and buildFriend6
// (RingSqueeze, line 2047, lives in Shared.cs)
public static partial class G
{
	// Bubble/Flame options {ax,ay,r,sw,sf,ph,life} (null = undefined)
	public class F6O
	{
		public double? ax, ay, r, sw, sf, ph, life;
		// Object.assign(this clone, o)
		public F6O Assign(F6O o)
		{
			var c = (F6O)MemberwiseClone();
			if (o.ax != null) c.ax = o.ax; if (o.ay != null) c.ay = o.ay; if (o.r != null) c.r = o.r; if (o.sw != null) c.sw = o.sw;
			if (o.sf != null) c.sf = o.sf; if (o.ph != null) c.ph = o.ph; if (o.life != null) c.life = o.life;
			return c;
		}
	}
	// JS `v||d` on a number that may be undefined
	static double F6or(double? v, double d) => v is double x && x != 0 && !double.IsNaN(x) ? x : d;

	// a speech bubble (people's talk): its place is a fixed formula of time, so it moves the same every time
	public static Hz Bubble(double x0, double y0, double vx, double vy, double t0, F6O o = null)
	{
		o ??= new F6O();
		double ax = F6or(o.ax, 0), ay = F6or(o.ay, 0), r = F6or(o.r, 11), sw = F6or(o.sw, 0), sf = F6or(o.sf, 3), ph = F6or(o.ph, 0), life = F6or(o.life, 10), fl = vx > 0 ? -1 : 1;
		Func<double, (double, double)> pos = t => { double d = t - t0; return (x0 + vx * d + 0.5 * ax * d * d + (sw != 0 ? JS.sin(d * sf + ph) * sw : 0), y0 + vy * d + 0.5 * ay * d * d); };
		var h = new Hz();
		h.update = (t, dt) => { if (t - t0 > life) h.dead = true; else if (t - t0 > 0.6) { var q = pos(t); if (q.Item1 < -120 || q.Item1 > W + 120 || q.Item2 < -130 || q.Item2 > H + 130) h.dead = true; } };
		h.draw = t =>
		{
			if (t < t0) return; var (x, y) = pos(t); double s = r * (1 + 0.15 * pulse); g.save(); g.translate(x, y);
			g.fillStyle = "rgba(255,42,109,0.2)"; g.beginPath(); g.ellipse(0, 0, s * 1.75, s * 1.45, 0, 0, TAU); g.fill();
			g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, s * 1.25, s * 0.95, 0, 0, TAU); g.fill();
			g.beginPath(); g.moveTo(fl * s * 0.25, s * 0.7); g.lineTo(fl * s * 1.0, s * 1.4); g.lineTo(fl * s * 0.75, s * 0.45); g.closePath(); g.fill();
			g.fillStyle = "#000"; for (int i = -1; i <= 1; i++) circle(i * s * 0.42, 0, s * 0.15);
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; var (x, y) = pos(t); double dx = (p.x - x) / (r * 1.2 + p.r - 2), dy = (p.y - y) / (r * 0.92 + p.r - 2); return dx * dx + dy * dy < 1; };
		return h;
	}

	// a flame (from the burning heart): rises slowly
	public static Hz Flame(double x0, double y0, double vx, double vy, double t0, F6O o = null)
	{
		o ??= new F6O();
		double ay = o.ay == null ? -90 : o.ay.Value, r = F6or(o.r, 9), life = F6or(o.life, 9);
		Func<double, (double, double)> pos = t => { double d = t - t0; return (x0 + vx * d, y0 + vy * d + 0.5 * ay * d * d); };
		var h = new Hz();
		h.update = (t, dt) => { if (t - t0 > life) h.dead = true; else if (t - t0 > 0.6) { var q = pos(t); if (q.Item1 < -100 || q.Item1 > W + 100 || q.Item2 < -130 || q.Item2 > H + 130) h.dead = true; } };
		h.draw = t =>
		{
			if (t < t0) return; var (x, y) = pos(t); double s = r * (1 + 0.15 * pulse), fk = 1 + 0.12 * JS.sin(t * 31 + x0); g.save(); g.translate(x, y);
			g.fillStyle = "rgba(255,42,109,0.22)"; circle(0, 0, s * 1.7); g.fillStyle = PINK;
			g.beginPath(); g.moveTo(0, -s * 2.0 * fk); g.quadraticCurveTo(s * 1.25, -s * 0.1, 0, s * 1.05); g.quadraticCurveTo(-s * 1.25, -s * 0.1, 0, -s * 2.0 * fk); g.fill();
			g.fillStyle = "rgba(255,255,255,0.55)"; g.beginPath(); g.ellipse(0, s * 0.25, s * 0.32, s * 0.5, 0, 0, TAU); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; var (x, y) = pos(t); double dx = p.x - x, dy = p.y - y, rr = r + p.r - 2; return dx * dx + dy * dy < rr * rr; };
		return h;
	}

	// a talking mouth (grey, harmless): shows and moves, opens on the beat and the talk comes out
	public static Hz Lips(double x, double y, double tw, double ta, double te, double sc = 1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - tw) / 0.2, (te - t) / 0.3); if (a <= 0) return;
			double o = t < ta ? 0.12 + 0.22 * Math.Abs(JS.sin((t - tw) * 14)) : Math.Max(0.12, 1 - (t - ta) / 0.6);
			g.save(); g.globalAlpha = a; g.translate(x, y); g.scale(sc, sc); g.fillStyle = GREY + str(t < ta ? 0.4 + 0.3 * clamp((t - tw) / (ta - tw), 0, 1) : 0.7) + ")";
			double w = 44, hh = 8 + 26 * o;
			g.beginPath(); g.moveTo(-w, 0); g.quadraticCurveTo(-w * 0.55, -hh - 10, -w * 0.08, -hh * 0.55 - 6); g.quadraticCurveTo(0, -hh * 0.45 - 4, w * 0.08, -hh * 0.55 - 6); g.quadraticCurveTo(w * 0.55, -hh - 10, w, 0); g.quadraticCurveTo(0, -hh * 0.3, -w, 0); g.fill();
			g.beginPath(); g.moveTo(-w, 0); g.quadraticCurveTo(0, hh + 12, w, 0); g.quadraticCurveTo(0, hh * 0.3, -w, 0); g.fill();
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// a pointing finger (the blamers): a warning line, then the hand shoots out fast from the edge and goes back
	public static Hz Finger(double ox, double oy, double ang, double len, double tw, double ta, double te, double hw = 16)
	{
		Func<double, double> ext = t => t < ta ? 0 : t < ta + 0.1 ? (t - ta) / 0.1 : t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.2);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(ox, oy); g.rotate(ang);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; g.fillRect(0, -hw, len, hw * 2); g.fillStyle = $"rgba(255,42,109,{str(0.35 + 0.55 * k)})"; g.fillRect(0, -1.5, len * k, 3); }
			else
			{
				double L = len * ext(t);
				if (L > 4)
				{
					bool wh = (t - ta) < 0.08; g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = wh ? "#fff" : PINK;
					g.fillRect(-10, -hw * 0.7, Math.Max(0, L - hw * 2.2 + 10), hw * 1.4);
					rr(L - hw * 2.6, -hw, hw * 1.8, hw * 2, hw * 0.55); g.fill();
					rr(L - hw * 1.2, -hw, hw * 1.2, hw * 0.7, hw * 0.33); g.fill();
					g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(L - hw * 2.1, hw * 0.05, hw * 1.2, 2); g.fillRect(L - hw * 2.1, hw * 0.5, hw * 1.2, 2);
				}
			}
			g.restore();
		};
		h.hit = (p, t) => { double e = ext(t); if (t < ta || e <= 0.05) return false; double L = len * e; return obbHit(p.x, p.y, p.r, ox + JS.cos(ang) * L / 2, oy + JS.sin(ang) * L / 2, L / 2, hw, ang); };
		return h;
	}

	// a burning heart ("burns your heart"): heats up and shakes, then catches fire and flames come out every way
	public static Hz BurnHeart(double x, double y, double s, double tw, double ta, int n, double sp, double a0 = 0, double fay = -90)
	{
		bool ex = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!ex && t >= ta) { ex = true; for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; hazards.Add(Flame(x + JS.cos(a) * s * 0.4, y + JS.sin(a) * s * 0.4, JS.cos(a) * sp, JS.sin(a) * sp, ta, new F6O { ay = fay })); } burst(x, y, PINK, 16, 320); shake = Math.Max(shake, 7); }
			if (t > ta + 0.3) h.dead = true;
		};
		h.draw = t =>
		{
			g.save(); g.translate(x, y);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), j = k * k * 4; g.translate(JS.sin(t * 67) * j, JS.cos(t * 53) * j);
				g.globalAlpha = 0.12 + 0.28 * k; g.fillStyle = PINK; circle(0, 0, s * (1.1 + 0.3 * k));
				g.globalAlpha = 0.5 + 0.45 * k; g.strokeStyle = PINK; g.lineWidth = 3; heartPath(s * 0.9); g.stroke();
				g.globalAlpha = 0.15 + 0.5 * k; g.fillStyle = PINK; g.save(); g.scale(k, k); heartPath(s * 0.9); g.fill(); g.restore();
				g.globalAlpha = k; for (int i = -1; i <= 1; i++) { double fh = (8 + 14 * k) * (1 + 0.25 * JS.sin(t * 25 + i * 2)); g.beginPath(); g.moveTo(i * s * 0.35 - 6, -s * 0.45); g.quadraticCurveTo(i * s * 0.35, -s * 0.45 - fh * 2, i * s * 0.35 + 6, -s * 0.45); g.fill(); }
			}
			else { double k = (t - ta) / 0.3; g.globalAlpha = Math.Max(0, 1 - k); g.fillStyle = k < 0.3 ? "#fff" : PINK; heartPath(s * (0.9 + k)); g.fill(); }
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= ta && t < ta + 0.12 && hypot(p.x - x, p.y - y) < s * 0.85 + p.r;
		return h;
	}

	// dabke: a foot comes down hard on a column (from below or above), and two waves run along the floor from the hit
	public static Hz Stomp(double x, double w, double hh, bool top, double tw, double ta, double te, double wsp)
	{
		bool wv = false; Func<double, double> ext = t => t < ta ? 0 : t < ta + 0.07 ? (t - ta) / 0.07 : t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.2);
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!wv && wsp != 0 && t >= ta) { wv = true; double y = top ? 16 : H - 16; foreach (var d in new double[] { -1, 1 }) hazards.Add(Bubble(x + d * w * 0.5, y, d * wsp, 0, ta, new F6O { r = 10 })); shake = Math.Max(shake, 5); }
			if (t > te + 0.2) h.dead = true;
		};
		h.draw = t =>
		{
			g.save(); if (top) { g.translate(0, H); g.scale(1, -1); }
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.16 * k)})"; g.fillRect(x - w / 2, H - hh, w, hh); g.fillStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.fillRect(x - w / 2, H - 4, w, 4);
				g.globalAlpha = 0.25 + 0.5 * k; g.strokeStyle = PINK; g.lineWidth = 2; g.strokeRect(x - w * 0.3, H - hh - 60 * (1 - k), w * 0.6, 40); g.globalAlpha = 1;
			}
			else
			{
				double e = ext(t), dy = (1 - e) * hh; bool wh = (t - ta) < 0.08; g.save(); g.translate(0, -dy); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = wh ? "#fff" : PINK;
				g.fillRect(x - w * 0.32, H - hh, w * 0.64, hh - 34); rr(x - w / 2, H - 40, w, 40, 14); g.fill(); g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(x - w * 0.32, H - hh * 0.55, w * 0.64, 4); g.restore();
				if (e >= 1 && t < te + 0.1) { double k = clamp((t - ta) / 0.35, 0, 1); g.globalAlpha = Math.Max(0, 1 - k); g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath(); g.ellipse(x, H - 4, w * 0.6 + k * w, 10 + k * 12, 0, Math.PI, TAU); g.stroke(); g.globalAlpha = 1; }
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || ext(t) < 0.5) return false; double y0 = top ? 0 : H - hh, y1 = top ? hh : H; return Math.Abs(p.x - x) < w / 2 + p.r - 2 && p.y + p.r > y0 + 2 && p.y - p.r < y1 - 2; };
		return h;
	}

	// emit option of Tanoura: {every, sp, lead}
	public class F6Emit { public double every, sp, lead; }

	// a whirling dervish (the tanoura): the skirt spins and its edge hurts, the hat is grey. ang is a function of time, and emit throws bubbles from the skirt's ends every beat
	public static Hz Tanoura(double x, double y, double R, int arms, double w, double ts, double te, Func<double, double> ang, F6Emit emit)
	{
		Func<double, double> grow = t => clamp((t - ts) / (1.5 * SPB), 0, 1); Func<double, bool> live = t => t >= ts + 1.5 * SPB && t < te; int lb = -1;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > te + SPB) h.dead = true;
			if (emit != null && live(t))
			{
				int b = (int)Math.Floor(BT(t) + 1e-6);
				if (b != lb)
				{
					lb = b;
					if (b % emit.every == 0) { double tb = B(b), a = ang(tb); for (int i = 0; i < arms; i++) { double aa = a + i * TAU / arms + emit.lead; hazards.Add(Bubble(x + JS.cos(aa) * R, y + JS.sin(aa) * R, JS.cos(aa) * emit.sp, JS.sin(aa) * emit.sp, tb, new F6O { r = 10 })); } }
				}
			}
		};
		h.draw = t =>
		{
			double gr = t > te ? clamp(1 - (t - te) / SPB, 0, 1) : grow(t); bool act = live(t); double a = ang(t); if (gr <= 0) return; g.save(); g.translate(x, y);
			g.globalAlpha = act ? 1 : 0.3; g.fillStyle = PINK; if (act) { g.shadowColor = PINK; g.shadowBlur = 14; }
			for (int i = 0; i < arms; i++) { g.save(); g.rotate(a + i * TAU / arms); g.beginPath(); g.moveTo(0, -w * 0.22); g.lineTo(R * gr, -w * 0.5); g.quadraticCurveTo(R * gr + w * 0.4, 0, R * gr, w * 0.5); g.lineTo(0, w * 0.22); g.closePath(); g.fill(); g.restore(); }
			g.shadowBlur = 0; circle(0, 0, 24 * (1 + 0.12 * pulse));
			g.globalAlpha = act ? 0.85 : 0.3; g.fillStyle = GREY + "0.9)"; rr(-9, -44, 18, 40, 8); g.fill(); circle(0, -4, 13);
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (!live(t)) return false; if (hypot(p.x - x, p.y - y) < 24 + p.r) return true; double a = ang(t);
			for (int i = 0; i < arms; i++)
			{
				double aa = a + i * TAU / arms, c = JS.cos(aa), s = JS.sin(aa);
				if (obbHit(p.x, p.y, p.r, x + c * R * 0.25, y + s * R * 0.25, R * 0.25, w * 0.26, aa) || obbHit(p.x, p.y, p.r, x + c * R * 0.75, y + s * R * 0.75, R * 0.25, w * 0.42, aa)) return true;
			}
			return false;
		};
		return h;
	}

	// a wall of talk: a row of bubbles moving together with an opening
	public static Hz TalkWall(string dir, double gc, double gh, double sp, double tw, double ta)
	{
		bool vert = dir == "L" || dir == "R"; double sg = (dir == "L" || dir == "U") ? -1 : 1, ext = vert ? W : H, th = 30, len = vert ? H : W;
		double st = sg < 0 ? ext + th : -th; Func<double, double> pos = t => st + sg * sp * (t - ta);
		var h = new Hz();
		h.update = (t, dt) => { double p = pos(t); if (t > ta && (p < -th * 2 || p > ext + th * 2)) h.dead = true; };
		h.draw = t =>
		{
			double a = gc - gh / 2, b = gc + gh / 2;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), e = 14; g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.45 * k)})";
				if (vert) { double x = sg < 0 ? W - e : 0; g.fillRect(x, 0, e, a); g.fillRect(x, b, e, H - b); } else { double y = sg < 0 ? H - e : 0; g.fillRect(0, y, a, e); g.fillRect(b, y, W - b, e); }
				return;
			}
			double p = pos(t), s = 13 * (1 + 0.12 * pulse); g.save(); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12;
			for (double q = s; q < len; q += s * 2.3) { if (q > a - s * 0.6 && q < b + s * 0.6) continue; double x = vert ? p : q, y = vert ? q : p; g.beginPath(); g.ellipse(x, y, s * 1.25, s * 0.95, 0, 0, TAU); g.fill(); }
			g.shadowBlur = 0; g.fillStyle = "#000"; for (double q = s; q < len; q += s * 2.3) { if (q > a - s * 0.6 && q < b + s * 0.6) continue; double x = vert ? p : q, y = vert ? q : p; for (int i = -1; i <= 1; i++) circle(x + i * s * 0.42, y, s * 0.14); }
			g.restore();
		};
		h.hit = (pl, t) =>
		{
			if (t < ta) return false; double p = pos(t), a = gc - gh / 2, b = gc + gh / 2;
			if (vert) { if (Math.Abs(pl.x - p) > th / 2 + pl.r) return false; return pl.y < a + pl.r || pl.y > b - pl.r; }
			if (Math.Abs(pl.y - p) > th / 2 + pl.r) return false; return pl.x < a + pl.r || pl.x > b - pl.r;
		};
		return h;
	}

	// a hint on the edge: pink marks where a row of talk will come from (harmless)
	public static Hz EdgeHint(string side, List<double> ps, double tw, double ta)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > ta + 0.15) h.dead = true; };
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1), a = t < ta ? 0.25 + 0.6 * k : Math.Max(0, 1 - (t - ta) / 0.15); g.fillStyle = $"rgba(255,42,109,{str(a)})";
			foreach (var p in ps) { if (side == "L") g.fillRect(0, p - 11, 8 + 10 * k, 22); else if (side == "R") g.fillRect(W - 8 - 10 * k, p - 11, 8 + 10 * k, 22); else if (side == "T") g.fillRect(p - 11, 0, 22, 8 + 10 * k); else g.fillRect(p - 11, H - 8 - 10 * k, 22, 8 + 10 * k); }
		};
		h.hit = (p, t) => false;
		return h;
	}

	// a big grey word in the background (harmless)
	public static Hz Word(string s, double x, double y, double size, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double a = min(1, (t - t0) / 0.12, (t1 - t) / 0.5); if (a <= 0) return; double k = 1 + 0.25 * Math.Max(0, 1 - (t - t0) / 0.25) + 0.05 * pulse;
			g.save(); g.globalAlpha = a; txt(s, x, y, size * k, GREY + "0.16)"); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// the meddler: a big face that talks and throws talk, its eyes on you. When it gets angry (from ta) its talk spirals, it charges (from tc) and explodes at te
	public static Hz Aathool(double ts, double ta, double tc, double te, Tier T)
	{
		int lastS = -1; bool ex = false; double mo = 0, cT = 0;
		Func<double, (double x, double y)> pos = t =>
		{
			double k = clamp((t - ts) / (2 * SPB), 0, 1), e = 1 - JS.pow(1 - k, 3), d = t - ts; double x = W / 2 + 330 * JS.sin(d * 0.5), y = -170 + (170 + 150) * e + 38 * JS.sin(d * 1.05);
			if (t >= tc) { double c = clamp((t - tc) / (1.5 * SPB), 0, 1); x += (W / 2 - x) * c; y += (H / 2 - 40 - y) * c; }
			return (x, y);
		};
		void fan((double x, double y) P, int n, double @base, double spr, double sp, double r = 10) { for (int i = 0; i < n; i++) { double a = @base + (i - (n - 1) / 2.0) * spr; hazards.Add(Bubble(P.x + JS.cos(a) * 70, P.y + JS.sin(a) * 70, JS.cos(a) * sp, JS.sin(a) * sp, cT, new F6O { r = r })); } mo = 1; }
		void ring((double x, double y) P, int n, double @base, double sp, double r = 10) { for (int i = 0; i < n; i++) { double a = @base + i * TAU / n; hazards.Add(Bubble(P.x + JS.cos(a) * 70, P.y + JS.sin(a) * 70, JS.cos(a) * sp, JS.sin(a) * sp, cT, new F6O { r = r })); } mo = 1; }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			mo = Math.Max(0, mo - F6or(dt, 0) * 3); var P = pos(t);
			if (t >= te && !ex)
			{
				ex = true; cT = te; P = pos(te); ring(P, 22, 0, T.sp * 0.85, 11); for (int i = 0; i < 10; i++) { double a = i * TAU / 10 + 0.3; hazards.Add(Flame(P.x, P.y, JS.cos(a) * T.sp * 0.6, JS.sin(a) * T.sp * 0.6, te, new F6O { ay = -60 })); }
				burst(P.x, P.y, PINK, 70, 520); burst(P.x, P.y, "#fff", 35, 420); shake = 26; flash = 0.7; rumble(500, 1, 1); h.dead = true; return;
			}
			if (t < ts + 2 * SPB || t >= tc) return;
			int s = (int)Math.Floor(BT(t) * 2 + 1e-6); if (s == lastS) return; lastS = s; double bt = s / 2.0; int p = s % 8; cT = B(bt); P = pos(cT); double aim = player != null ? JS.atan2(player.y - P.y, player.x - P.x) : Math.PI / 2;
			if (t < ta)
			{
				if (p == 0) ring(P, 7, bt * 0.37, T.sp * 0.75);
				else if (p == 4 && s % 16 == 4) fan(P, 3, aim, 0.3, T.sp * 0.95);
			}
			else
			{
				if (s % 2 == 0) ring(P, 3, bt * 0.52, T.sp * 0.7, 10);
				if (p == 3) fan(P, 3, aim, 0.32, T.sp);
			}
		};
		h.draw = t =>
		{
			var P = pos(t); bool ch = t >= tc; double s = 1 + 0.1 * pulse, jx = 0, jy = 0;
			if (ch) { double k = clamp((t - tc) / (te - tc), 0, 1); s = 1 + 0.3 * k; jx = JS.sin(t * 71) * 7 * k; jy = JS.cos(t * 59) * 7 * k; }
			bool ang = t >= ta; g.save(); g.translate(P.x + jx, P.y + jy);
			g.fillStyle = GREY + "0.25)"; g.save(); g.rotate(t * 0.4); for (int i = 0; i < 6; i++) { g.rotate(TAU / 6); rr(84 * s, -9, 26, 18, 8); g.fill(); } g.restore();
			g.shadowColor = PINK; g.shadowBlur = ch ? 50 : 30; g.fillStyle = PINK; circle(0, 0, 66 * s); g.shadowBlur = 0;
			double lk = player != null ? JS.atan2(player.y - P.y, player.x - P.x) : Math.PI / 2, ex2 = JS.cos(lk) * 5, ey2 = JS.sin(lk) * 5;
			g.fillStyle = "#000"; foreach (var sx in new double[] { -1, 1 }) { g.beginPath(); g.ellipse(sx * 24 * s, -14 * s, 12 * s, (ang ? 8 : 11) * s, 0, 0, TAU); g.fill(); }
			g.fillStyle = ch ? "#fff" : PINK; foreach (var sx in new double[] { -1, 1 }) circle(sx * 24 * s + ex2, -14 * s + ey2, 4.5 * s);
			g.strokeStyle = "#000"; g.lineWidth = 5 * s; g.lineCap = "round"; foreach (var sx in new double[] { -1, 1 }) { g.beginPath(); g.moveTo(sx * 38 * s, (ang ? -34 : -30) * s); g.lineTo(sx * 12 * s, (ang ? -26 : -32) * s); g.stroke(); }
			double mh = (6 + 22 * Math.Max(mo, ch ? 0.8 : 0)) * s; g.fillStyle = "#000"; g.beginPath(); g.ellipse(0, 26 * s, 24 * s, mh * 0.5 + 2, 0, 0, TAU); g.fill();
			g.restore(); g.lineCap = "butt";
		};
		h.hit = (p, t) => { if (t < ts + 2 * SPB) return false; var P = pos(t); return hypot(p.x - P.x, p.y - P.y) < 60 + p.r; };
		return h;
	}

	/* the level "طنش": 102 bpm (beat 0.59 s), grid starts at 0.137
	   0 intro | 8 calm | 16 big hit | 20 calm mawwal | 40 verse 1 | 56 full band | 72 dabke | 76 checkpoint, chorus
	   108 music (dervishes) | 124 verse 2 | 140 checkpoint, the blamers | 156 dabke | 160 chorus and the meddler (boss) | 184 angry | 194 charge | 197 explodes (1:56) */
	public static List<Ev> buildFriend6()
	{
		var E = new List<Ev>(); Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f }); var R = mulberry32(3030);
		var TI = ezT(new[] { new Tier { sp = 225, wn = 1.6 }, new Tier { sp = 258, wn = 1.45 }, new Tier { sp = 272, wn = 1.35 } });
		void word(double b, string s, double x, double y, double size, double dur) => ev(b, () => hazards.Add(Word(s, x, y, size, B(b), B(b + dur))));
		// a mouth on the edge opens on the beat and talk comes out (a fan of bubbles)
		void talk(double hit, double x, double y, double a, int n, double spr, Tier T, double k = 1, F6O o = null)
		{
			o ??= new F6O();
			ev(hit - T.wn, () => { double t0 = B(hit); hazards.Add(Lips(x, y, B(hit - T.wn), t0, B(hit + 0.9))); for (int i = 0; i < n; i++) { double aa = a + (i - (n - 1) / 2.0) * spr; hazards.Add(Bubble(x + JS.cos(aa) * 34, y + JS.sin(aa) * 34, JS.cos(aa) * T.sp * k, JS.sin(aa) * T.sp * k, t0, o)); } });
		}
		double toC(double x, double y) => JS.atan2(H / 2 - y, W / 2 - x);
		// a row of talk coming from the side with an opening
		void row(double b, bool left, Tier T, double k = 0.8, int n = 8, double gp = -1, F6O o = null)
		{
			o ??= new F6O();
			ev(b - 0.6, () => { double t0 = B(b), G = gp >= 0 ? gp : Math.Floor(rnd() * (n - 1)); var ps = new List<double>(); for (int i = 0; i < n; i++) { if (i == G || i == G + 1) continue; ps.Add((i + 0.5) * H / n); hazards.Add(Bubble(left ? -20 : W + 20, (i + 0.5) * H / n, (left ? 1 : -1) * T.sp * k, 0, t0, new F6O { r = 10 }.Assign(o))); } hazards.Add(EdgeHint(left ? "L" : "R", ps, B(b - 0.6), t0)); });
		}
		void col(double b, bool top, Tier T, double k = 0.8, int n = 11, double gp = -1, F6O o = null)
		{
			o ??= new F6O();
			ev(b - 0.6, () => { double t0 = B(b), G = gp >= 0 ? gp : Math.Floor(rnd() * (n - 1)); var ps = new List<double>(); for (int i = 0; i < n; i++) { if (i == G || i == G + 1) continue; ps.Add((i + 0.5) * W / n); hazards.Add(Bubble((i + 0.5) * W / n, top ? -20 : H + 20, 0, (top ? 1 : -1) * T.sp * k, t0, new F6O { r = 11 }.Assign(o))); } hazards.Add(EdgeHint(top ? "T" : "B", ps, B(b - 0.6), t0)); });
		}
		void finger(double hit, string side, double p, Tier T, double dur = 0.8, double len = 0, double hw = 16)
		{
			double L = len != 0 ? len : (side == "L" || side == "R" ? W * 0.62 : H * 0.62);
			var (ox, oy, a) = side == "L" ? (-6.0, p, 0.0) : side == "R" ? (W + 6, p, Math.PI) : side == "T" ? (p, -6.0, Math.PI / 2) : (p, H + 6, -Math.PI / 2);
			ev(hit - T.wn, () => hazards.Add(Finger(ox, oy, a, L, B(hit - T.wn), B(hit), B(hit + dur), hw)));
		}
		void heart(double hit, double x, double y, Tier T, int n = 10, double s = 46, double a0 = 0) => ev(hit - T.wn - 0.3, () => hazards.Add(BurnHeart(x, y, s, B(hit - T.wn - 0.3), B(hit), n, T.sp * 0.75, a0)));
		void stomp(double hit, double x, Tier T, bool top = false, double h = H * 0.55, double w = 110, double dur = 0.5, bool wave = true) => ev(hit - T.wn, () => hazards.Add(Stomp(x, w, h, top, B(hit - T.wn), B(hit), B(hit + dur), wave ? T.sp * 1.05 : 0)));
		void wall(double hit, string dir, Tier T, double gh = 200, double sp = 0) { bool vert = dir == "L" || dir == "R"; double gc = vert ? 90 + R() * (H - 180) : 150 + R() * (W - 300); ev(hit - 1, () => hazards.Add(TalkWall(dir, gc, gh, sp != 0 ? sp : (vert ? 400 : 290), B(hit - 1), B(hit)))); }
		void squeeze(double hit, double x, double y, double dur, Tier T, double gs = 0.75) { double gap = R() * TAU; ev(hit - T.wn - 0.5, () => hazards.Add(RingSqueeze(x, y, 950, 0, B(hit - T.wn - 0.5), B(hit), B(hit + dur), 18, gap, gs))); }
		void dervish(double b0, double b1, double x, double y, double Rr, int arms, double om, Tier T, double emit = 0) => ev(b0 - 1.5, () => { double t0 = B(b0 - 1.5), ph = R() * TAU; hazards.Add(Tanoura(x, y, Rr, arms, 30, t0, B(b1), t => ph + (t - t0) * om, emit != 0 ? new F6Emit { every = emit, sp = T.sp * 0.7, lead = 0.12 * sign(om) } : null)); });
#pragma warning disable CS8321 // fanAt is defined but never called in the original too
		void fanAt(double hit, double x, double y, int n, double a0, double spr, Tier T, double k = 0.8, F6O o = null) { o ??= new F6O { r = 10 }; ev(hit, () => { double t0 = B(hit); for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2.0) * spr; hazards.Add(Bubble(x, y, JS.cos(a) * T.sp * k, JS.sin(a) * T.sp * k, t0, o)); } }); }
#pragma warning restore CS8321

		/* ---------- part 1 (0:00 - 0:44) ---------- */
		var T = TI[0];
		// 0:00 intro hits: the word "طنش" shows, and people start talking from the corners on every beat
		word(0, "طنّش", W / 2, H / 2, 190, 7.5);
		talk(2, W - 60, 60, toC(W - 60, 60), 3, 0.3, T, 0.8); talk(3, W - 60, H - 60, toC(W - 60, H - 60), 3, 0.3, T, 0.8); talk(4, 60, H - 60, toC(60, H - 60), 3, 0.3, T, 0.8);
		talk(5.5, W / 2, 40, Math.PI / 2, 5, 0.34, T, 0.75); talk(7, W / 2, H - 40, -Math.PI / 2, 5, 0.34, T, 0.75);
		// 0:04 calm: rows of talk from the right on the off-beats, and pointing fingers
		for (double b = 8.5; b < 16; b += 2) row(b, Math.Floor(b) % 4 == 2, T, 0.7, 7, -1, new F6O { sw = 14, sf = 4 });
		finger(11.5, "T", W * 0.3, T, 0.7); finger(13.5, "B", W * 0.7, T, 0.7); finger(15.5, "T", W * 0.55, T, 0.6);
		// 0:09 the big hit: dabke, and a burning heart in the middle
		stomp(16, W * 0.2, T); stomp(16, W * 0.8, T, true);
		heart(18.5, W / 2, H / 2, T, 14, 54); heart(19.5, W * 0.22, H * 0.3, T, 6, 40, 0.4); heart(19.5, W * 0.78, H * 0.7, T, 6, 40, 0.4);
		// 0:12 the calm mawwal ("ريّح بالك"): a dervish spins slowly in the middle, talk comes from the edges
		dervish(21, 31, W / 2, H / 2, 200, 3, 0.6, T);
		foreach (var (b, x, y) in new[] { (22.0, 40.0, H * 0.2), (24, W - 40, H * 0.8), (26, 40, H * 0.8), (28, W - 40, H * 0.2), (30, W / 2, 30) }) talk(b, x, y, toC(x, y), 3, 0.32, T, 0.75);
		foreach (var b in new double[] { 25, 29 }) finger(b, b % 4 == 3 ? "L" : "R", b % 4 == 3 ? H * 0.12 : H * 0.88, T, 0.7);
		// 0:18 the circle closes on you: get out through the gap
		squeeze(32, W / 2, H / 2, 5, T, 0.85);
		row(39, true, T, 0.65, 9, -1);
		// 0:23 verse 1 (people's talk): every sung line, a mouth talks and throws talk
		foreach (var (b, x, y, n) in new[] { (40.0, 40.0, H * 0.3, 4), (43, W - 40, H * 0.7, 4), (47, W * 0.3, 30, 4), (48, W * 0.7, H - 30, 4), (51, 40, H / 2, 5), (52, W - 40, H / 2, 5), (53, W / 2, 30, 4) }) talk(b, x, y, toC(x, y), n, 0.22, T, 0.8);
		foreach (var b in new double[] { 42, 46, 50, 54 }) finger(b, b % 8 == 2 ? "B" : "T", b % 8 == 2 ? W * 0.35 : W * 0.65, T, 0.8);
		foreach (var b in new double[] { 45, 55 }) wall(b, b == 45 ? "L" : "R", T, 210);
		// 0:33 full band ("يحرق قلبك"): hearts catch fire, fingers point, and dabke on the floor
		foreach (var (b, x, y) in new[] { (56.0, W * 0.3, H * 0.35), (59, W * 0.72, H * 0.3), (63, W / 2, H * 0.32), (68, W * 0.25, H / 2), (69, W * 0.75, H / 2) }) heart(b, x, y, T, 5, 42, b * 0.5);
		foreach (var b in new double[] { 58, 62, 66, 70 }) finger(b, b % 8 == 2 ? "L" : "R", H * (b % 8 == 2 ? 0.25 : 0.75), T, 0.7);
		foreach (var b in new double[] { 56, 64 }) stomp(b + 2, b % 8 == 0 ? W * 0.85 : W * 0.15, T, b % 8 != 0, H * 0.45, 100, 0.45);
		// 0:42 dabke: the feet hit the floor left to right on the drum
		foreach (var (b, i) in new[] { (72.0, 0), (73, 1), (74, 2), (75, 3) }) stomp(b, 160 + i * 320, T, i % 2 == 1, H * 0.62, 130, 0.55);

		/* ---------- part 2 (0:44 - 1:22) ---------- */
		T = TI[1];
		ev(76, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		// 0:44 chorus "طنش": the word comes out big, walls of talk with an opening, and dabke on the strong beat
		foreach (var b in new double[] { 76, 84, 92, 100 }) word(b, "طنّش", W / 2, H / 2, 210, 3.5);
		foreach (var (b, d) in new[] { (78.0, "L"), (82, "R") }) wall(b, d, T, 210);
		foreach (var b in new double[] { 76, 80 }) stomp(b, b == 76 ? W * 0.15 : W * 0.85, T, b == 80, H * 0.5, 110, 0.5);
		foreach (var b in new double[] { 77, 81 }) talk(b, b % 4 == 1 ? 40 : W - 40, b % 4 == 1 ? H * 0.25 : H * 0.75, b % 4 == 1 ? 0 : Math.PI, 4, 0.27, T, 0.85);
		// 0:49 two dervishes spinning against each other
		dervish(85, 91, W * 0.27, H / 2, 145, 3, 1.0, T); dervish(85, 91, W * 0.73, H / 2, 145, 3, -1.0, T);
		foreach (var b in new double[] { 86, 90 }) col(b + 0.5, b % 4 == 2, T, 0.75, 13, -1, new F6O { r = 10 });
		foreach (var b in new double[] { 87, 89, 91 }) finger(b, b % 4 == 3 ? "L" : "R", b % 4 == 3 ? H * 0.12 : H * 0.88, T, 0.6);
		// 0:54 hearts on fire and walls
		foreach (var (b, x, y) in new[] { (92.0, W * 0.25, H * 0.3), (93, W * 0.75, H * 0.7), (96, W * 0.75, H * 0.3), (97, W * 0.25, H * 0.7) }) heart(b, x, y, T, 8, 42, b * 0.3);
		foreach (var (b, d) in new[] { (94.0, "L"), (98, "R") }) wall(b, d, T, 190);
		foreach (var b in new double[] { 95, 99 }) stomp(b, b == 95 ? W * 0.5 : W * 0.5, T, b == 99, H * 0.45, 140, 0.5);
		// 0:59 "ريّح بالك": the circle closes, and fingers from the side
		squeeze(101, W / 2, H / 2, 5, T, 0.8);
		foreach (var b in new double[] { 100, 102, 104, 106 }) finger(b, b % 4 == 0 ? "T" : "B", b % 4 == 0 ? W * 0.15 : W * 0.85, T, 0.6, H * 0.75);
		row(107, false, T, 0.75, 9, -1);
		// 1:03 the music: a big dervish in the middle throws talk from its skirt's ends, and dabke above and below
		dervish(109, 123, W / 2, H / 2, 190, 3, 0.7, T, 3);
		for (double b = 110; b < 124; b += 4) stomp(b, b % 4 == 2 ? W * 0.1 : W * 0.9, T, b % 4 == 2, H * 0.4, 100, 0.45);
		foreach (var b in new double[] { 111, 115, 119 }) talk(b, b % 8 == 7 ? 40 : W - 40, b % 8 == 7 ? H * 0.15 : H * 0.85, b % 8 == 7 ? 0.35 : Math.PI + 0.35, 4, 0.22, T, 0.8);
		foreach (var b in new double[] { 113, 117, 121 }) talk(b, b % 8 == 1 ? W - 40 : 40, b % 8 == 1 ? H * 0.15 : H * 0.85, b % 8 == 1 ? Math.PI - 0.35 : -0.35, 4, 0.22, T, 0.8);
		// 1:13 verse 2 ("خلّك بعيد"): walls from both sides come at you, and a mouth talks with every line
		foreach (var (b, d) in new[] { (124.0, "L"), (130, "R"), (136, "L") }) wall(b, d, T, 230, 330);
		foreach (var (b, x, y) in new[] { (125.0, W / 2, 30.0), (127, W / 2, H - 30), (129, W * 0.25, 30), (131, W * 0.75, H - 30), (133, W * 0.75, 30), (135, W * 0.25, H - 30), (137, W / 2, 30), (139, W / 2, H - 30) }) talk(b, x, y, toC(x, y), 4, 0.27, T, 0.8);

		/* ---------- part 3 (1:22 - 1:56) ---------- */
		T = TI[2];
		ev(140, () => { flash = Math.Max(flash, 0.25); shake = Math.Max(shake, 10); });
		// 1:22 the blamers: fingers from every side, and burning hearts
		for (int k = 0; k < 8; k++) { double b = 140 + k * 2; string s = new[] { "L", "T", "R", "B" }[k % 4]; double p = (s == "L" || s == "R") ? H * (0.2 + 0.6 * ((k * 3) % 5) / 4) : W * (0.15 + 0.7 * ((k * 2) % 5) / 4); finger(b, s, p, T, 0.7); if (k % 4 == 0) finger(b + 1, new[] { "R", "B", "L", "T" }[k % 4], (s == "L" || s == "R") ? H - p : W - p, T, 0.6); }
		foreach (var (b, x, y) in new[] { (141.5, W * 0.3, H * 0.3), (145.5, W * 0.7, H * 0.7), (149.5, W * 0.7, H * 0.3), (153.5, W * 0.3, H * 0.7) }) heart(b, x, y, T, 6, 42, b * 0.4);
		foreach (var b in new double[] { 143, 147, 151, 155 }) row(b, b % 8 == 3, T, 0.75, 8, -1);
		// 1:31 dabke
		foreach (var (b, i) in new[] { (156.0, 3), (157, 2), (158, 1), (159, 0) }) stomp(b, 160 + i * 320, T, i % 2 == 0, H * 0.62, 130, 0.55);
		// 1:34 chorus and the meddler: comes down from above and talks, and you ignore it
		var TT = T; ev(160, () => { flash = Math.Max(flash, 0.3); shake = Math.Max(shake, 12); hazards.Add(Aathool(B(160), B(184), B(194), B(197), TT)); });
		foreach (var b in new double[] { 160, 168, 176 }) word(b, "طنّش", W / 2, H * 0.62, 200, 3.5);
		foreach (var (b, d) in new[] { (164.0, "L"), (180, "U") }) wall(b, d, T, 220);
		foreach (var b in new double[] { 162, 170, 178 }) stomp(b, b % 8 == 2 ? W * 0.12 : W * 0.88, T, false, H * 0.42, 100, 0.5);
		foreach (var b in new double[] { 163, 167, 175, 183 }) finger(b, b % 4 == 3 ? "L" : "R", H * (0.55 + 0.3 * ((i32(b) >> 2) % 2)), T, 0.6);
		// 1:48 the meddler gets angry and spirals its talk, dabke on the edges
		for (double b = 185; b < 194; b += 4) stomp(b, b % 8 == 1 ? W * 0.08 : W * 0.92, T, b % 8 == 5, H * 0.36, 90, 0.45);
		squeeze(188, W / 2, H / 2, 4, T, 0.85);
		// 1:54 it charges, the last dabke, and it explodes at 1:56
		foreach (var (b, i) in new[] { (194.0, 0), (195, 3), (196, 1), (196.5, 2) }) stomp(b, 160 + i * 320, T, i % 2 == 1, H * 0.5, 120, 0.4);

		/* ---------- fingers in the middle: no place in the middle stays safe for long ---------- */
		foreach (var (b, sd, x) in new[] { (17.0, "T", 90.0), (22, "B", W - 90), (27, "T", W - 140), (31, "B", 140), (36, "T", W * 0.5) }) finger(b, sd, x, TI[0], 0.7, H * 0.85);
		foreach (var (b, sd, y) in new[] { (18.0, "L", 110.0), (23, "L", 190), (29, "R", 100), (20, "R", 610), (26, "R", 190), (30, "L", 610), (34, "R", 110) }) finger(b, sd, y, TI[0], 0.7, W * 0.42); finger(24, "T", 500, TI[0], 0.7, H * 0.4); finger(28, "T", 580, TI[0], 0.7, H * 0.4);
		foreach (var (b, sd, p) in new[] { (46.0, "L", 0.5), (52, "R", 0.35), (66, "L", 0.58), (120, "L", 0.7), (126, "R", 0.8), (132, "L", 0.66), (138, "R", 0.74), (145, "L", 0.7), (96.5, "R", 0.5), (116, "L", 0.78), (122, "R", 0.62), (128, "R", 0.72), (134, "L", 0.55), (150, "R", 0.6) }) { T = b < 76 ? TI[0] : b < 140 ? TI[1] : TI[2]; finger(b, sd, H * p, T, 0.7, W * 0.7); }
		foreach (var (b, sd, p) in new[] { (44.0, "L", 0.36), (56.5, "R", 0.42), (88, "R", 0.46), (61, "L", 0.3), (137, "R", 0.85), (147, "L", 0.62), (153, "R", 0.86), (128, "L", 0.76), (134.5, "R", 0.68), (141, "R", 0.7), (150.5, "L", 0.78), (169, "R", 0.72), (174, "L", 0.66) }) { T = b < 76 ? TI[0] : b < 140 ? TI[1] : TI[2]; finger(b, sd, H * p, T, 0.7, W * 0.96); }
		finger(50, "T", W * 0.12, TI[0], 0.7, H * 0.7); finger(54, "B", W * 0.88, TI[0], 0.7, H * 0.7);
		/* ---------- the edge layer: fingers walking on the floor and ceiling, so the edges are not safe ---------- */
		for (double b = 13, k = 0; b < 41; b += 4, k++) finger(b, k % 2 != 0 ? "R" : "L", k % 2 != 0 ? 20 : H - 20, TI[0], 0.9, W * 0.8, 18);
		for (double b = 11, k = 0; b < 190; b += 8, k++) { if ((b >= 72 && b < 77) || (b >= 155 && b < 161)) continue; T = b < 76 ? TI[0] : b < 140 ? TI[1] : TI[2]; bool lf = k % 2 == 0; finger(b, lf ? "T" : "B", lf ? 20 : W - 20, T, 0.9, H * 0.8, 18); if (k % 3 == 2) finger(b + 4, lf ? "B" : "T", lf ? 20 : W - 20, T, 0.9, H * 0.8, 18); }
		for (double b = 41, k = 0; b < 196; b += 4, k++) { if ((b >= 72 && b < 77) || (b >= 155 && b < 161)) continue; T = b < 76 ? TI[0] : b < 140 ? TI[1] : TI[2]; bool fl = k % 2 == 0, lf = (i32(k) >> 1) % 2 == 0; finger(b, lf ? "L" : "R", fl ? H - 20 : 20, T, 0.9, W * 0.8, 18); }
		sort(E, (a, b) => a.b - b.b); return E;
	}

	// AFK maker (line 6521)
	static readonly int _afkF6 = AfkReg("buildFriend6", (x, y, b, v, w) =>
	{
		if (v >= 2) { string d = "RLDU"[v - 2].ToString(); return One(TalkWall(d, d == "R" || d == "L" ? y : x, 150, d == "R" || d == "L" ? 400 : 290, B(b - w), B(b))); }
		return One(Stomp(x, 110, H * 0.6, y < H / 2, B(b - w), B(b), B(b + 0.6), 0));
	});
}
