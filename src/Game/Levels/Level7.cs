using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 2494-2873: the shapes of level 7 (Clownparty, the clown party) and buildLevel7
public static partial class G
{
	// JS `v||d` for an optional number
	static double L7or(double? v, double d) => v is double x && x != 0 && !double.IsNaN(x) ? x : d;

	// a toss arc: from (x0,y0) to (x1,y1) in D seconds, the top is hgt above the higher of them
	public static (double vx, double vy, double gr) L7arc(double x0, double y0, double x1, double y1, double hgt, double D)
	{
		double top = Math.Min(y0, y1) - hgt, a = y0 - top, c = y1 - top, u = (Math.Sqrt(2 * a) + Math.Sqrt(2 * c)) / D, gr = u * u; return ((x1 - x0) / D, -Math.Sqrt(2 * gr * a), gr);
	}
	// a juggling ball: flies on an arc (gravity), bounces on the floor if bn > 0
	public static Hz L7Ball(double x, double y, double vx, double vy, double gr, double r = 12, int bn = 0)
	{
		double rot = rnd() * TAU; var h = new Hz { x = x, y = y, vx = vx, vy = vy, r = r };
		h.update = (t, dt) =>
		{
			h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; rot += dt * 5 * (h.vx < 0 ? -1 : 1);
			if (bn > 0 && h.y > H - h.r && h.vy > 0) { h.y = H - h.r; h.vy = -h.vy * 0.8; bn--; burst(h.x, H - 4, PINK, 4, 140); }
			if (h.x < -90 || h.x > W + 90 || h.y > H + 90 || h.y < -900) h.dead = true;
		};
		h.draw = t =>
		{
			double s = h.r * (1 + 0.12 * pulse); g.fillStyle = "rgba(255,42,109,0.22)"; circle(h.x, h.y, s * 1.6); g.fillStyle = PINK; circle(h.x, h.y, s);
			g.save(); g.translate(h.x, h.y); g.rotate(rot); g.strokeStyle = "rgba(255,255,255,0.75)"; g.lineWidth = Math.Max(2, s * 0.2); g.beginPath(); g.arc(0, 0, s * 0.62, -0.9, 0.9); g.stroke(); g.beginPath(); g.arc(0, 0, s * 0.62, Math.PI - 0.9, Math.PI + 0.9); g.stroke(); g.restore();
		};
		h.hit = (p, t) => { double dx = p.x - h.x, dy = p.y - h.y, rr = h.r + p.r - 2; return dx * dx + dy * dy < rr * rr; };
		return h;
	}
	// a juggling club: spins while it flies on an arc
	public static Hz L7Club(double x, double y, double vx, double vy, double gr, double spin, double? a0)
	{
		double a = a0 == null ? rnd() * TAU : a0.Value; var h = new Hz { x = x, y = y, vx = vx, vy = vy };
		h.update = (t, dt) => { h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; a += spin * dt; if (h.x < -90 || h.x > W + 90 || h.y > H + 90 || h.y < -900) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(h.x, h.y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.ellipse(8, 0, 15, 9, 0, 0, TAU); g.fill(); g.fillRect(-22, -3.5, 22, 7); circle(-22, 0, 5);
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.7)"; g.fillRect(3, -8, 4, 16); g.restore();
		};
		h.hit = (p, t) => obbHit(p.x, p.y, p.r - 1, h.x - JS.cos(a) * 4, h.y - JS.sin(a) * 4, 23, 7, a);
		return h;
	}
	// a big juggling hoop: flies on an arc, its middle is empty (you can pass through it)
	public static Hz L7Hoop(double x, double y, double vx, double vy, double gr, double R = 60, double th = 11)
	{
		double rot = rnd() * TAU; var h = new Hz { x = x, y = y, vx = vx, vy = vy };
		h.update = (t, dt) => { h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; rot += dt * 3; if (h.x < -160 || h.x > W + 160 || h.y > H + 160 || h.y < -900) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(h.x, h.y); g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = th * (1 + 0.15 * pulse); g.beginPath(); g.arc(0, 0, R, 0, TAU); g.stroke(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = th * 0.45; for (int i = 0; i < 8; i++) { double a = rot + i * TAU / 8; g.beginPath(); g.arc(0, 0, R, a, a + 0.25); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) => { double d = hypot(p.x - h.x, p.y - h.y); return Math.Abs(d - R) < th / 2 + p.r - 2; };
		return h;
	}
	// the options of a toss (JS object {twin, staff, hoop, club, r, R, w, bn, ext, dots, L1, L2, spin, a0})
	public class TossO { public bool twin, staff, hoop, club; public double? r, R, w, bn, ext, dots, L1, L2, spin, a0; }
	// a toss: before the throw its path shows as dots, on the beat the ball (or club) comes out
	public static Hz L7Toss(double x0, double y0, double x1, double y1, double hgt, double tw, double ta, double D, TossO o = null)
	{
		o ??= new TossO();
		var q = L7arc(x0, y0, x1, y1, hgt, D); bool club = o.club, hoop = o.hoop; double r = L7or(o.r, 12); int bn = (int)L7or(o.bn, 0); double ext = L7or(o.ext, 1.3); bool done = false;
		Func<double, (double, double)> P = s => (x0 + q.vx * s, y0 + q.vy * s + q.gr * s * s / 2);
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!done && t >= ta)
			{
				done = true;
				hazards.Add(o.twin ? L7Twin(x0, y0, q.vx, q.vy, q.gr, L7or(o.R, 28), L7or(o.w, 9), r) : o.staff ? L7Staff(x0, y0, q.vx, q.vy, q.gr, L7or(o.L1, 46), L7or(o.L2, 280), L7or(o.spin, 4), L7or(o.a0, 0)) : hoop ? L7Hoop(x0, y0, q.vx, q.vy, q.gr, L7or(o.R, 60)) : club ? L7Club(x0, y0, q.vx, q.vy, q.gr, (q.vx > 0 ? 1 : -1) * 10, o.a0) : L7Ball(x0, y0, q.vx, q.vy, q.gr, r, bn));
				h.dead = true;
			}
		};
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1); int n = (int)L7or(o.dots, 24); g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.55 * k)})";
			for (int i = 1; i <= n; i++) { if ((double)i / n > k * 1.2 + 0.12) break; var (px, py) = P(D * ext * i / n); if (py > H + 20 || px < -30 || px > W + 30) break; double s = 2 + 1.5 * k; g.fillRect(px - s, py - s, 2 * s, 2 * s); }
			g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 2; g.beginPath(); g.arc(clamp(x0, 8, W - 8), clamp(y0, 8, H - 8), (o.staff ? L7or(o.L2, 280) : o.twin ? L7or(o.R, 28) + r : hoop ? L7or(o.R, 60) : r) + 10 - 7 * k, 0, TAU); g.stroke();
		};
		return h;
	}
	// a cream pie: thrown high, lands on your spot and bursts into cream that stays a little
	public static Hz L7Pie(double x, double y, double tw, double tl, double te, double PR = 54)
	{
		int st = 0; var h = new Hz();
		h.update = (t, dt) => { if (st == 0 && t >= tl) { st = 1; burst(x, y, PINK, 14, 260); burst(x, y, "#fff", 8, 200); shake = Math.Max(shake, 6); } if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tl)
			{
				double k = clamp((t - tw) / (tl - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.6 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(x, y, PR, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; circle(x, y, PR * k);
				double f = clamp((t - tw) / (tl - tw) * 1.6 - 0.6, 0, 1); if (f > 0) { double s = 2.4 - 1.4 * f, py = y - 260 * (1 - f) * (1 - f); g.globalAlpha = 0.35 + 0.65 * f; g.save(); g.translate(x, py); g.scale(s, s); g.rotate(t * 3); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, 20, 15, 0, 0, TAU); g.fill(); g.shadowBlur = 0; g.fillStyle = "#fff"; g.beginPath(); g.ellipse(0, -3, 14, 8, 0, 0, TAU); g.fill(); g.restore(); g.globalAlpha = 1; }
				return;
			}
			double e = t - tl, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.save(); g.translate(x, y); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = e < 0.06 ? "#fff" : PINK; g.beginPath();
			for (int i = 0; i < 14; i++) { double an = i * TAU / 14, rr = PR * (i % 2 != 0 ? 0.8 : 1.04) * Math.Min(1, 0.6 + e * 6); if (i != 0) g.lineTo(JS.cos(an) * rr, JS.sin(an) * rr); else g.moveTo(rr, 0); }
			g.closePath(); g.fill();
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.8)"; circle(-PR * 0.25, -PR * 0.2, PR * 0.2); circle(PR * 0.3, PR * 0.15, PR * 0.13); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= tl && t < te && hypot(p.x - x, p.y - y) < PR * 0.86 + p.r - 2;
		return h;
	}
	// twin balls: spin fast around each other while they fly on an arc (like a screw)
	public static Hz L7Twin(double x, double y, double vx, double vy, double gr, double R = 28, double w = 9, double r = 15)
	{
		double ph = rnd() * TAU; var h = new Hz { x = x, y = y, vx = vx, vy = vy };
		Func<int, (double, double)> bp = k => { double a = ph + k * Math.PI; return (h.x + JS.cos(a) * R, h.y + JS.sin(a) * R); };
		h.update = (t, dt) => { h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; ph += w * dt * (h.vx < 0 ? -1 : 1); if (h.x < -120 || h.x > W + 120 || h.y > H + 120 || h.y < -900) h.dead = true; };
		h.draw = t =>
		{
			var a = bp(0); var b = bp(1); g.strokeStyle = "rgba(255,42,109,0.35)"; g.lineWidth = 3; g.beginPath(); g.arc(h.x, h.y, R, 0, TAU); g.stroke();
			foreach (var (px, py) in new[] { a, b }) { double s = r * (1 + 0.12 * pulse); g.fillStyle = "rgba(255,42,109,0.22)"; circle(px, py, s * 1.5); g.fillStyle = PINK; circle(px, py, s); g.fillStyle = "rgba(255,255,255,0.7)"; circle(px - s * 0.3, py - s * 0.3, s * 0.28); }
		};
		h.hit = (p, t) => { foreach (var k in new[] { 0, 1 }) { var (px, py) = bp(k); double dx = p.x - px, dy = p.y - py, rr = r + p.r - 2; if (dx * dx + dy * dy < rr * rr) return true; } return false; };
		return h;
	}
	// a long juggling staff: flies on an arc spinning like a fan, its middle is empty (you can pass through the middle)
	public static Hz L7Staff(double x, double y, double vx, double vy, double gr, double L1, double L2, double spin, double a0, double th = 16)
	{
		double a = a0; double hl = (L2 - L1) / 2, mid = (L1 + L2) / 2; var h = new Hz { x = x, y = y, vx = vx, vy = vy };
		h.update = (t, dt) => { h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; a += spin * dt; if (h.x < -L2 - 60 || h.x > W + L2 + 60 || h.y > H + L2 + 60 || h.y < -900) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(h.x, h.y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK;
			foreach (var sd in new double[] { -1, 1 }) { g.fillRect(sd > 0 ? L1 : -L2, -th / 2, L2 - L1, th); circle(sd * L2, 0, th * 0.8); }
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.75)"; foreach (var sd in new double[] { -1, 1 }) for (int k = 1; k < 6; k++) { double d = sd * (L1 + (L2 - L1) * k / 6); g.fillRect(d - 3, -th / 2, 6, th); }
			g.strokeStyle = GREY + "0.5)"; g.lineWidth = 3; g.beginPath(); g.moveTo(-L1, 0); g.lineTo(L1, 0); g.stroke(); g.fillStyle = GREY + "0.9)"; circle(0, 0, 7); g.restore();
		};
		h.hit = (p, t) => { double c = JS.cos(a), s = JS.sin(a); foreach (var sd in new double[] { -1, 1 }) if (obbHit(p.x, p.y, p.r - 1, h.x + c * sd * mid, h.y + s * sd * mid, hl + th * 0.4, th / 2, a)) return true; return false; };
		return h;
	}
	// a juggling circle: balls spin around a point that moves in an 8, the circle grows and shrinks with the beat (its middle is empty)
	public static Hz L7Orbit(Func<double, (double, double)> C, int n, Func<double, double> Rf, Func<double, double> angF, double tw, double t0, double t1, double r = 15)
	{
		Func<double, int, (double, double)> pos = (t, i) => { var (cx, cy) = C(t); double a = angF(t) + i * TAU / n, R = Rf(t); return (cx + JS.cos(a) * R, cy + JS.sin(a) * R); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double al = t < t0 ? clamp((t - tw) / (t0 - tw), 0, 1) : t >= t1 ? clamp(1 - (t - t1) / 0.4, 0, 1) : 1; if (al <= 0) return; g.globalAlpha = al;
			if (t < t0) { var (cx, cy) = C(t); g.strokeStyle = "rgba(255,42,109,0.6)"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); g.arc(cx, cy, Rf(t), 0, TAU); g.stroke(); g.setLineDash(new double[0]); g.globalAlpha = al * 0.45; }
			for (int i = 0; i < n; i++) { var (px, py) = pos(t, i); double s = r * (1 + 0.12 * pulse); g.fillStyle = "rgba(255,42,109,0.22)"; circle(px, py, s * 1.5); g.fillStyle = PINK; circle(px, py, s); g.fillStyle = "rgba(255,255,255,0.7)"; circle(px - s * 0.3, py - s * 0.3, s * 0.28); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; for (int i = 0; i < n; i++) { var (px, py) = pos(t, i); double dx = p.x - px, dy = p.y - py, rr = r + p.r - 2; if (dx * dx + dy * dy < rr * rr) return true; } return false; };
		return h;
	}
	// a piece of confetti that flies and spins
	public static Hz L7Conf(double x, double y, double vx, double vy)
	{
		var b = Bullet(x, y, vx, vy, 8); double a = rnd() * TAU; double sp = (rnd() < 0.5 ? -1 : 1) * (5 + rnd() * 4);
		b.draw = t => { a += sp / 60; g.save(); g.translate(b.x, b.y); g.rotate(a); g.fillStyle = "rgba(255,42,109,0.22)"; g.fillRect(-11, -11, 22, 22); g.fillStyle = PINK; g.fillRect(-9, -4.5, 18, 9); g.restore(); };
		return b;
	}
	// an empty balloon popping (decoration): a circle that grows and fades
	public static Hz L7Puff(double x, double y, double t0)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t0 + 0.3) h.dead = true; };
		h.draw = t => { double k = clamp((t - t0) / 0.3, 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.6 * (1 - k))})"; g.lineWidth = 3; g.beginPath(); g.arc(x, y, 16 + 26 * k, 0, TAU); g.stroke(); };
		return h;
	}
	// a balloon: rises from the bottom and pops into confetti on the beat
	public static Hz L7Balloon(double x, double vy, double tw, double ta, double tp, int n, double sp, double yo = 0, double sa = 0, double sph = 0)
	{
		double y0 = H + 40 + yo, a0 = rnd() * TAU; bool done = false; Func<double, double> Y = t => y0 - vy * Math.Max(0, t - ta), X = t => sa != 0 ? x + JS.sin(t * 2.4 + sph) * sa : x + JS.sin(t * 2 + x) * 10;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!done && t >= tp)
			{
				done = true; double x2 = X(t), y = Y(t); for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; hazards.Add(L7Conf(x2, y, JS.cos(a) * sp, JS.sin(a) * sp)); }
				if (n > 0) { burst(x2, y, PINK, 12, 280); burst(x2, y, "#fff", 6, 220); } else hazards.Add(L7Puff(x2, y, t)); h.dead = true;
			}
		};
		h.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.6 * k)})"; g.beginPath(); g.moveTo(x, H - 8); g.lineTo(x - 11, H - 22 - 10 * k); g.lineTo(x + 11, H - 22 - 10 * k); g.fill(); return; }
			double y = Y(t), x2 = X(t), sw = t > tp - SPB ? 1 + 0.16 * Math.Abs(JS.sin(t * 40)) : 1; g.strokeStyle = GREY + "0.45)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(x2, y + 24); g.quadraticCurveTo(x2 + 8, y + 46, x2 - 4, y + 72); g.stroke();
			g.save(); g.translate(x2, y); g.scale(sw, sw); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, 17, 21, 0, 0, TAU); g.fill(); g.beginPath(); g.moveTo(0, 19); g.lineTo(-5, 26); g.lineTo(5, 26); g.fill();
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.6)"; g.beginPath(); g.ellipse(-6, -8, 4, 7, -0.4, 0, TAU); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; double y = Y(t), x2 = X(t), dx = (p.x - x2) / (17 + p.r - 2), dy = (p.y - y) / (21 + p.r - 2); return dx * dx + dy * dy < 1; };
		return h;
	}
	// the phase 1 background (harmless decoration): a swirl of lines like a tent that turns and flips with the song, and circles on every beat
	public static Hz L7Swirl(double t0, double t1, Func<double, double> angF)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Max(0, Math.Min(1, Math.Min((t - t0) / 1.2, (t1 - t) / 0.8))); if (a <= 0) return; double bt = BT(t), cx = W / 2 + JS.sin(t * 0.5) * 70, cy = H / 2 + JS.cos(t * 0.37) * 45, A = angF(t), tw = 0.0042 * JS.sin(bt * Math.PI / 16) + 0.0016; int n = 12; double hw = TAU / n / 2;
			g.fillStyle = GREY + toFixed(0.05 * a, 3) + ")";
			for (int i = 0; i < n; i++)
			{
				double a0 = A + i * TAU / n; g.beginPath(); g.moveTo(cx, cy); for (double r = 20; r <= 860; r += 40) { double an = a0 + r * tw; g.lineTo(cx + JS.cos(an) * r, cy + JS.sin(an) * r); }
				for (double r = 860; r >= 20; r -= 40) { double an = a0 + hw + r * tw; g.lineTo(cx + JS.cos(an) * r, cy + JS.sin(an) * r); }
				g.closePath(); g.fill();
			}
			double f = bt - Math.Floor(bt); g.strokeStyle = GREY + toFixed(0.12 * a * (1 - f), 3) + ")"; g.lineWidth = 3; for (int k = 0; k < 3; k++) { g.beginPath(); g.arc(cx, cy, (f + k) * 260, 0, TAU); g.stroke(); }
		};
		return h;
	}
	// a juggling clown (harmless grey decoration)
	public static Hz L7Juggler(double x, double y, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; double arm = JS.sin(BT(t) * Math.PI) * 0.6; g.save(); g.globalAlpha = a; g.translate(x, y); g.strokeStyle = GREY + "0.6)"; g.fillStyle = GREY + "0.6)"; g.lineWidth = 4; g.lineCap = "round";
			g.beginPath(); g.moveTo(0, -20); g.lineTo(0, 18); g.lineTo(-12, 44); g.moveTo(0, 18); g.lineTo(12, 44); g.moveTo(0, -12); g.lineTo(-22, -28 - 14 * arm); g.moveTo(0, -12); g.lineTo(22, -28 + 14 * arm); g.stroke();
			circle(0, -34, 13); g.beginPath(); g.moveTo(-11, -43); g.lineTo(0, -68); g.lineTo(11, -43); g.fill(); circle(0, -69, 4); g.fillStyle = "#000"; circle(-4, -36, 2); circle(4, -36, 2); g.fillStyle = GREY + "0.95)"; circle(0, -31, 4);
			g.lineCap = "butt"; g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	// the circus cannon (decoration): kicks back on every shot
	public static Hz L7Cannon(double x, double y, double ang, double t0, double t1, List<double> shots)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; double rc = 0; foreach (var s in shots) { double e = t - s; if (e >= 0 && e < 0.3) rc = Math.Max(rc, (1 - e / 0.3) * 14); }
			g.save(); g.globalAlpha = a; g.translate(x, y); g.fillStyle = GREY + "0.5)"; g.beginPath(); g.arc(0, 8, 24, Math.PI, 0); g.fill(); g.rotate(ang); g.fillStyle = GREY + "0.75)"; g.fillRect(-10 - rc, -15, 70, 30); g.fillStyle = GREY + "0.95)"; g.fillRect(52 - rc, -18, 12, 36);
			g.fillStyle = "rgba(0,0,0,0.5)"; g.fillRect(60 - rc, -11, 4, 22); g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	// the tent curtain (decoration): opens from the middle in the intro
	public static Hz L7Curtain(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double k = clamp((t - t0) / (t1 - t0), 0, 1), o = JS.pow(k, 1.5) * (W / 2 + 60), a = Math.Min(1, (t1 - t) / (2 * SPB)); g.globalAlpha = a;
			foreach (var sd in new double[] { -1, 1 }) for (int i = 0; i < 8; i++) { double x = W / 2 + sd * (o + i * 80); g.fillStyle = GREY + (i % 2 != 0 ? "0.05)" : "0.11)"); g.fillRect(sd > 0 ? x : x - 80, 0, 80, H); }
			g.fillStyle = GREY + "0.14)"; for (int i = 0; i < 17; i++) { g.beginPath(); g.arc(i * 80, 0, 40, 0, Math.PI); g.fill(); }
			g.globalAlpha = 1;
		};
		return h;
	}
	// a decoration word (harmless)
	public static Hz L7Word(string s, double x, double y, double size, double t0, double t1, string col)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double e = t - t0, a = Math.Min(1, Math.Min(e / 0.08, (t1 - t) / 0.3)), sc = 1 + 0.35 * JS.exp(-e * 10); g.globalAlpha = Math.Max(0, a); g.save(); g.translate(x, y); g.scale(sc, sc);
			if (s.Any(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))) { g.font = $"{str(size)}px {TFONT}"; g.fillStyle = col ?? GREY + "0.55)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, 0, 0); }
			else txt(s, 0, 0, size, col ?? GREY + "0.55)", 800);
			g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	/* ---- phase 2: the carousel ---- */
	// a carousel horse
	public static void L7horse(double x, double y, double s, double f, bool lit)
	{
		g.save(); g.translate(x, y); g.strokeStyle = GREY + "0.45)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, -s * 1.8); g.lineTo(0, s * 1.8); g.stroke(); g.scale(f, 1);
		g.fillStyle = lit ? PINK : "rgba(255,42,109,0.35)"; if (lit) { g.shadowColor = PINK; g.shadowBlur = 12; }
		g.beginPath(); g.ellipse(-s * 0.12, s * 0.12, s * 0.8, s * 0.42, 0, 0, TAU); g.fill();
		g.beginPath(); g.moveTo(s * 0.35, -s * 0.05); g.lineTo(s * 0.7, -s * 0.95); g.lineTo(s * 1.12, -s * 0.72); g.lineTo(s * 0.82, -s * 0.5); g.lineTo(s * 0.72, -s * 0.1); g.closePath(); g.fill();
		g.fillRect(-s * 0.72, s * 0.32, s * 0.17, s * 0.62); g.fillRect(s * 0.3, s * 0.32, s * 0.17, s * 0.62); g.beginPath(); g.moveTo(-s * 0.85, -s * 0.05); g.lineTo(-s * 1.2, s * 0.3); g.lineTo(-s * 0.85, s * 0.25); g.fill();
		g.shadowBlur = 0; if (lit) { g.fillStyle = "rgba(0,0,0,0.5)"; circle(s * 0.8, -s * 0.72, s * 0.08); }
		g.restore();
	}
	// a ring of horses turning around a point, going up and down with the beat. The slots in skip are empty
	public static Hz L7Horses(Func<double, (double, double)> C, Func<double, double> Rf, int n, int[] skip, double tw, double t0, double t1, Func<double, double> angF, double bob = 0, double hr = 20)
	{
		Func<double, int, (double, double, double)> pos = (t, i) => { var (cx, cy) = C(t); double a = angF(t) + i * TAU / n, r = Rf(t) + bob * JS.sin(Math.PI * BT(t) + i * Math.PI); return (cx + JS.cos(a) * r, cy + JS.sin(a) * r, a); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; };
		h.draw = t =>
		{
			bool live = t >= t0 && t < t1; double al = t < t0 ? clamp((t - tw) / (t0 - tw), 0, 1) : t >= t1 ? clamp(1 - (t - t1) / 0.4, 0, 1) : 1; if (al <= 0) return; g.globalAlpha = al;
			if (t < t0) { var (cx, cy) = C(t); g.strokeStyle = "rgba(255,42,109,0.55)"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 }); g.beginPath(); g.arc(cx, cy, Rf(t), 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			double dir = angF(t + 0.01) >= angF(t) ? 1 : -1;
			for (int i = 0; i < n; i++) { if (skip.Contains(i)) continue; var (x, y, a) = pos(t, i); L7horse(x, y, hr, -JS.sin(a) * dir >= 0 ? 1 : -1, live); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; for (int i = 0; i < n; i++) { if (skip.Contains(i)) continue; var (x, y, _) = pos(t, i); double dx = p.x - x, dy = p.y - y, rr = hr * 0.82 + p.r - 2; if (dx * dx + dy * dy < rr * rr) return true; } return false; };
		return h;
	}
	public static Hz L7Horses((double, double) ctr, double radF, int n, int[] skip, double tw, double t0, double t1, Func<double, double> angF, double bob = 0, double hr = 20)
		=> L7Horses(t => ctr, t => radF, n, skip, tw, t0, t1, angF, bob, hr);
	public static Hz L7Horses(Func<double, (double, double)> C, double radF, int n, int[] skip, double tw, double t0, double t1, Func<double, double> angF, double bob = 0, double hr = 20)
		=> L7Horses(C, t => radF, n, skip, tw, t0, t1, angF, bob, hr);
	// a flying horse (comes off the carousel)
	public static Hz L7HorseShot(double x, double y, double vx, double vy, double s = 20)
	{
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) => { h.x += vx * dt; h.y += vy * dt; if (h.x < -90 || h.x > W + 90 || h.y < -90 || h.y > H + 90) h.dead = true; };
		h.draw = t => L7horse(h.x, h.y, s, vx >= 0 ? 1 : -1, true);
		h.hit = (p, t) => { double dx = p.x - h.x, dy = p.y - h.y, rr = s * 0.82 + p.r - 2; return dx * dx + dy * dy < rr * rr; };
		return h;
	}
	// striped poles coming out of the middle and turning (the carousel poles)
	public static Hz L7Poles(double cx, double cy, int arms, double r0, double tw, double ta, double te, Func<double, double> angF, double w = 18)
	{
		const double L = 1500; var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double A = angF(t); g.save(); g.translate(cx, cy);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); for (int i = 0; i < arms; i++) { g.save(); g.rotate(A + i * TAU / arms); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; g.fillRect(r0, -w / 2, L, w); g.fillStyle = $"rgba(255,42,109,{str(0.45 + 0.45 * k)})"; g.fillRect(r0, -1.5, L * k, 3); g.restore(); } g.restore(); return; }
			double kk = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = kk;
			for (int i = 0; i < arms; i++)
			{
				g.save(); g.rotate(A + i * TAU / arms); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.07 ? "#fff" : PINK; g.fillRect(r0, -w / 2, L, w); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.35)";
				double o = (t * 120) % 40; for (double q = r0 + o; q < L; q += 40) { g.beginPath(); g.moveTo(q, -w / 2); g.lineTo(q + 12, -w / 2); g.lineTo(q + 2, w / 2); g.lineTo(q - 10, w / 2); g.closePath(); g.fill(); }
				g.restore();
			}
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double A = angF(t); if (hypot(p.x - cx, p.y - cy) < r0 - p.r) return false; for (int i = 0; i < arms; i++) { double a = A + i * TAU / arms; if (obbHit(p.x, p.y, p.r - 1, cx + JS.cos(a) * (r0 + L / 2), cy + JS.sin(a) * (r0 + L / 2), L / 2, w / 2, a)) return true; } return false; };
		return h;
	}
	// the carousel hub: a grey umbrella turning in the middle, its pink core hurts
	public static Hz L7Hub(double cx, double cy, double t0, double t1, Func<double, double> angF)
	{
		Func<double, bool> on = t => t >= t0 + 0.8 && t < t1; var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.translate(cx, cy); g.rotate(angF(t));
			for (int i = 0; i < 12; i++) { g.fillStyle = GREY + (i % 2 != 0 ? "0.08)" : "0.18)"); g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, 92, i * TAU / 12, (i + 1) * TAU / 12); g.closePath(); g.fill(); }
			g.fillStyle = GREY + "0.2)"; for (int i = 0; i < 12; i++) { double an = (i + 0.5) * TAU / 12; circle(JS.cos(an) * 92, JS.sin(an) * 92, 12); }
			bool lv = on(t); g.shadowColor = PINK; g.shadowBlur = lv ? 20 : 0; g.fillStyle = lv ? PINK : "rgba(255,42,109,0.35)"; circle(0, 0, 34 * (1 + 0.1 * pulse)); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.35)"; circle(0, 0, 13); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => on(t) && hypot(p.x - cx, p.y - cy) < 34 + p.r - 2;
		return h;
	}
	// the carousel fence: everything outside the circle hurts. It closes from screen size to half the height, and opens at the end
	public static Hz L7Fence(double cx, double cy, double R1, double tw, double t0, double tc, double to, double t1)
	{
		const double R0 = 800; Func<double, double> rad = t => t < t0 ? R0 : t < tc ? R0 + (R1 - R0) * (1 - JS.pow(1 - (t - t0) / (tc - t0), 2)) : t < to ? R1 : t < t1 ? R1 + (R0 - R1) * JS.pow((t - to) / (t1 - to), 2) : R0;
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.arc(cx, cy, R1, 0, TAU); g.stroke(); g.setLineDash(new double[0]); return; }
			double r = rad(t); if (r >= 780) return; g.save(); g.beginPath(); g.rect(0, 0, W, H); g.arc(cx, cy, r, 0, TAU, true); g.fillStyle = "rgba(255,42,109,0.2)"; g.fill("evenodd"); g.clip("evenodd");
			g.fillStyle = "rgba(255,42,109,0.22)"; double sp = (t * 0.25) % (TAU / 24); g.beginPath(); for (int i = 0; i < 24; i++) { double a = sp + i * TAU / 24; g.moveTo(cx, cy); g.arc(cx, cy, 900, a, a + TAU / 48); g.closePath(); }
			g.fill(); g.restore();
			g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineWidth = 6; g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.stroke(); g.restore();
			g.fillStyle = GREY + "0.5)"; for (int i = 0; i < 36; i++) { double a = i * TAU / 36 + t * 0.3; bool on = (i + (int)Math.Floor(BT(t))) % 3 == 0; g.fillStyle = on ? "rgba(255,255,255,0.85)" : GREY + "0.35)"; circle(cx + JS.cos(a) * (r + 12), cy + JS.sin(a) * (r + 12), 3.5); }
		};
		h.hit = (p, t) => { if (t < t0) return false; double r = rad(t); return hypot(p.x - cx, p.y - cy) > r - 6 - p.r; };
		return h;
	}
	/* ---- phase 3: the big clown ---- */
	// a flying "ha" laugh
	public static Hz L7Ha(double x, double y, double vx, double vy)
	{
		var b = Bullet(x, y, vx, vy, 12);
		b.draw = t => { g.fillStyle = "rgba(255,42,109,0.2)"; circle(b.x, b.y, 19 * (1 + 0.12 * pulse)); g.save(); g.shadowColor = PINK; g.shadowBlur = 10; txt("ها", b.x, b.y + 3, 27, PINK, 800); g.restore(); };
		return b;
	}
	// a boxing glove on a spring: comes out of the screen edge at your spot and goes back
	public static Hz L7Glove(double x0, double y0, double x1, double y1, double tw, double ta, double th, double te)
	{
		double a = JS.atan2(y1 - y0, x1 - x0), Lm = hypot(x1 - x0, y1 - y0) + 70, c = JS.cos(a), s = JS.sin(a);
		Func<double, double> ext = t => t < ta ? 0 : t < ta + 0.13 ? Lm * (t - ta) / 0.13 : t < th ? Lm : t < te ? Lm * (1 - (t - th) / (te - th)) : 0;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.55 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x0 + c * Lm, y0 + s * Lm); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.35 * k)})"; circle(x0 + c * Lm, y0 + s * Lm, 24 * k); g.fillStyle = $"rgba(255,42,109,{str(0.5 + 0.5 * k)})"; circle(clamp(x0, 10, W - 10), clamp(y0, 10, H - 10), 10 + 6 * k); return;
			}
			double e = ext(t), tx = x0 + c * e, ty = y0 + s * e; int nz = (int)Math.Max(4, Math.Floor(e / 22)); g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath();
			for (int i = 0; i <= nz; i++) { double q = Math.Max(0, e - 26) * i / nz, o = i == 0 || i == nz ? 0 : (i % 2 != 0 ? 1 : -1) * 10, px = x0 + c * q - s * o, py = y0 + s * q + c * o; if (i != 0) g.lineTo(px, py); else g.moveTo(px, py); }
			g.stroke();
			g.save(); g.translate(tx, ty); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.ellipse(4, 0, 24, 21, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(-2, -18, 12, 8, 0.5, 0, TAU); g.fill(); g.fillRect(-26, -12, 12, 24);
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.55)"; g.fillRect(-24, -12, 4, 24); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double e = ext(t); return hypot(p.x - (x0 + c * e), p.y - (y0 + s * e)) < 22 + p.r - 2; };
		return h;
	}
	// the big circus mallet: comes down on a strip of the arena (warned first)
	public static Hz L7Mallet(bool vert, double c, double w, double tw, double ta, double te)
	{
		double[] r0 = vert ? new[] { c - w / 2, 0, w, H } : new[] { 0, c - w / 2, W, w }; bool bk = false;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; if (!bk && t >= ta) { bk = true; shake = Math.Max(shake, 10); burst(vert ? c : W / 2, vert ? H / 2 : c, PINK, 16, 320); } };
		h.draw = t =>
		{
			double x = r0[0], y = r0[1], ww = r0[2], hh = r0[3];
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.14 * k)})"; g.fillRect(x, y, ww, hh); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.strokeRect(x + 2, y + 2, ww - 4, hh - 4); g.setLineDash(new double[0]);
				double hx = vert ? c : W - 70 - (1 - k) * 60, hy = vert ? 60 + (1 - k) * -40 : c; g.save(); g.translate(hx, hy); g.rotate(vert ? -0.5 * (1 - k) : Math.PI / 2 - 0.5 * (1 - k)); g.fillStyle = GREY + str(0.35 + 0.5 * k) + ")"; g.fillRect(-6, 0, 12, 90); g.fillStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.fillRect(-46, -30, 92, 44); g.restore(); return;
			}
			double e = t - ta, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = e < 0.07 ? "#fff" : PINK; g.fillRect(x, y, ww, hh); g.restore();
			g.fillStyle = "rgba(255,255,255,0.28)"; for (double q = 0; q < (vert ? hh : ww); q += 46) { if (vert) g.fillRect(x, q, ww, 14); else g.fillRect(q, y, 14, hh); }
			double sq = e < 0.12 ? 1 - e / 0.12 * 0.3 : 0.7; g.save(); g.translate(vert ? c : W - 40, vert ? 40 : c); if (!vert) g.rotate(Math.PI / 2); g.fillStyle = GREY + "0.9)"; g.fillRect(-7, -10, 14, -70); g.fillStyle = "#fff"; g.fillRect(-w * 0.32, -10, w * 0.64, 40 * sq); g.fillStyle = PINK; g.fillRect(-w * 0.32, -10 + 40 * sq, w * 0.64, 8); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double x = r0[0], y = r0[1], ww = r0[2], hh = r0[3]; return p.x > x - p.r + 2 && p.x < x + ww + p.r - 2 && p.y > y - p.r + 2 && p.y < y + hh + p.r - 2; };
		return h;
	}
	// a small clown bouncing in the arena (comes out of the big clown)
	public static Hz L7Mini(double x, double y, double vx, double vy, double r, double t0, double t1)
	{
		double px = x, py = y; var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > t1 + 0.4) { h.dead = true; return; }
			if (t < t0) return; px += vx * dt; py += vy * dt; if (px < r) { px = r; vx = Math.Abs(vx); }
			if (px > W - r) { px = W - r; vx = -Math.Abs(vx); }
			if (py < r) { py = r; vy = Math.Abs(vy); }
			if (py > H - r) { py = H - r; vy = -Math.Abs(vy); }
		};
		h.draw = t =>
		{
			if (t < t0) return; double a = t < t1 ? 1 : Math.Max(0, 1 - (t - t1) / 0.4); g.globalAlpha = a; g.save(); g.translate(px, py); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; circle(0, 0, r * (1 + 0.08 * pulse)); g.shadowBlur = 0;
			g.fillStyle = "#fff"; circle(-r * 0.36, -r * 0.2, r * 0.2); circle(r * 0.36, -r * 0.2, r * 0.2); g.fillStyle = "#000"; circle(-r * 0.36, -r * 0.2, r * 0.09); circle(r * 0.36, -r * 0.2, r * 0.09);
			g.strokeStyle = "rgba(0,0,0,0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(0, r * 0.12, r * 0.48, 0.25, Math.PI - 0.25); g.stroke(); g.fillStyle = "#fff"; circle(0, r * 0.08, r * 0.15);
			g.fillStyle = GREY + "0.8)"; g.beginPath(); g.moveTo(-r * 0.55, -r * 0.75); g.lineTo(0, -r * 1.65); g.lineTo(r * 0.55, -r * 0.75); g.fill(); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= t0 + 0.15 && t < t1 && hypot(p.x - px, p.y - py) < r + p.r - 3;
		return h;
	}
	// the big clown: a grey face (harmless), its pink nose hurts. Opens its mouth before every laugh.
	// in the pause the lights go out except on him, at the end he blows up like a balloon and bursts into confetti
	public static Hz L7Clown(Func<double, (double, double)> cpos, double ts, List<double> laughs, double dk0, double dk1, double ang, double inf0, double pop)
	{
		bool ex = false;
		Func<double, double> sc = t => t < inf0 ? 1 : 1 + 0.85 * JS.pow(clamp((t - inf0) / (pop - inf0), 0, 1), 2);
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= pop && !ex) { ex = true; var (x, y) = cpos(t); for (int i = 0; i < 60; i++) burst(x, y, i % 3 != 0 ? PINK : "#fff", 1, 520); burst(x, y, "#fff", 40, 600); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; }
		};
		h.draw = t =>
		{
			if (t < ts) return;
			if (t >= dk0 && t < dk1)
			{
				double k = Math.Min(1, Math.Min((t - dk0) / 0.25, (dk1 - t) / 0.25)); var (x, y) = cpos(t); g.save(); g.beginPath(); g.rect(0, 0, W, H); g.arc(x, y, 150, 0, TAU, true); g.fillStyle = $"rgba(0,0,0,{str(0.82 * k)})"; g.fill("evenodd"); g.restore();
				var gr = g.createRadialGradient(x, y, 60, x, y, 170); gr.addColorStop(0, $"rgba(255,255,255,{str(0.05 * k)})"); gr.addColorStop(1, "rgba(255,255,255,0)"); g.fillStyle = gr; circle(x, y, 170);
			}
			var (x0, y0) = cpos(t); double s = sc(t); bool ch = t >= pop - 4 * SPB && t < pop; double jx = 0, jy = 0; if (ch) { double k = (t - (pop - 4 * SPB)) / (4 * SPB); jx = JS.sin(t * 80) * 7 * k; jy = JS.cos(t * 67) * 7 * k; }
			const double R = 78; double mo = 0; foreach (var b in laughs) { double e = (t - b) / SPB; mo = e > -1 && e < 0.6 ? Math.Max(mo, e < 0 ? 1 + e : 1 - e / 0.6) : mo; }
			bool angry = t >= ang;
			g.save(); g.translate(x0 + jx, y0 + jy); g.scale(s, s);
			// hair
			g.fillStyle = GREY + "0.5)"; foreach (var sd in new double[] { -1, 1 }) for (int i = 0; i < 3; i++) { circle(sd * (R + 6), -18 + i * 20, 17); }
			// hat
			g.fillStyle = GREY + "0.75)"; g.beginPath(); g.moveTo(-42, -R + 12); g.lineTo(6, -R - 80); g.lineTo(44, -R + 16); g.closePath(); g.fill(); g.fillStyle = "#fff"; circle(6, -R - 82, 10);
			g.fillStyle = GREY + "0.35)"; for (int i = 0; i < 3; i++) { circle(-14 + i * 16, -R - 14 - i * 20, 5); }
			// face
			g.fillStyle = "#d7d7e6"; circle(0, 0, R); g.fillStyle = "rgba(0,0,0,0.08)"; circle(0, 8, R * 0.92);
			// eyes
			g.fillStyle = "#fff"; g.beginPath(); g.ellipse(-28, -20, 17, 22, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(28, -20, 17, 22, 0, 0, TAU); g.fill();
			g.fillStyle = angry ? PINK : "#111"; double lk = t * 1.3; circle(-28 + JS.sin(lk) * 5, -18, 8); circle(28 + JS.sin(lk) * 5, -18, 8);
			if (angry) { g.strokeStyle = "#111"; g.lineWidth = 5; g.beginPath(); g.moveTo(-46, -48); g.lineTo(-12, -36); g.moveTo(46, -48); g.lineTo(12, -36); g.stroke(); }
			else { g.strokeStyle = "#111"; g.lineWidth = 3; g.beginPath(); g.arc(-28, -46, 12, Math.PI * 1.15, Math.PI * 1.85); g.stroke(); g.beginPath(); g.arc(28, -46, 12, Math.PI * 1.15, Math.PI * 1.85); g.stroke(); }
			// the mouth: opens before a laugh
			g.fillStyle = "#111"; g.beginPath(); g.moveTo(-48, 22); g.quadraticCurveTo(0, 46 + 30 * mo, 48, 22); g.quadraticCurveTo(0, 40 + 8 * mo, -48, 22); g.fill();
			if (mo > 0.05) { g.fillStyle = "#fff"; g.fillRect(-14, 30, 28, 6 * mo); }
			g.strokeStyle = "rgba(0,0,0,0.25)"; g.lineWidth = 3; g.beginPath(); g.arc(-50, 20, 8, Math.PI * 0.6, Math.PI * 1.5); g.stroke(); g.beginPath(); g.arc(50, 20, 8, -Math.PI * 0.5, Math.PI * 0.4); g.stroke();
			// the nose (hurts)
			g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; circle(0, 4, 17 * (1 + 0.12 * pulse)); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.6)"; circle(-5, -1, 5);
			g.restore();
		};
		h.hit = (p, t) => { if (t < ts + 0.5 || t >= pop) return false; var (x, y) = cpos(t); double s = sc(t); return hypot(p.x - x, p.y - (y + 4 * s)) < 16 * s + p.r - 2; };
		return h;
	}

	// a difficulty tier of level 7 (ezT keeps base: kf = sp/base)
	class T7 { public double sp, wn, bs; }

	/* level 7: Clownparty. The song starts at 0:16. 144 bpm (a beat is 0.417 s). Beat 0 = 0:16.79
	   phase 1 juggling (0:16 - 1:06): balls and clubs fly on arcs, balloons, pies. The drop is 24 (0:26.8)
	   phase 2 carousel (1:06 - 1:48, checkpoint 120): the fence closes and you are inside a turning carousel: horses and poles
	   phase 3 the clown (1:48 - 2:30, checkpoint 220): the big clown laughs "ha", gloves, pause at 252 (2:01.8) the lights go out,
	   the drop 256 (2:03.5) he gets angry and small clowns come out, blows up and bursts 316 (2:28.5), the end 320 (2:30) */
	public static List<Ev> buildLevel7()
	{
		var E = new List<Ev>(); var R = mulberry32(707);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double[] BS = { 320, 340, 360 };
		var TI = ezT(new[] { new Tier { sp = 320, wn = 1.6 }, new Tier { sp = 340, wn = 1.5 }, new Tier { sp = 360, wn = 1.45 } }).Select((q, i) => new T7 { sp = q.sp, wn = q.wn, bs = BS[i] }).ToArray();
		double kf(T7 T_) => T_.sp / T_.bs;
		double PXp() => player != null ? player.x : W / 2; double PYp() => player != null ? player.y : H / 2;
		void fx(double b, double f2, double s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
		void word(double b, string s, double x, double y, double size, double dur, string col) => ev(b, () => hazards.Add(L7Word(s, x, y, size, B(b), B(b + dur), col)));
		// a toss on an arc: hb the throw beat, fl the flight length in beats
		void toss(double hb, double x0, double y0, double x1, double y1, double hgt, double fl, T7 T_, TossO o) => ev(hb - T_.wn, () => hazards.Add(L7Toss(x0, y0, x1, y1, hgt, B(hb - T_.wn), B(hb), fl * SPB / kf(T_), o)));
		// a toss at your spot: lands where you were at the warning
		void tossP(double hb, double x0, double y0, double hgt, double fl, T7 T_, TossO o, double jx = 0, double jy = 0) => ev(hb - T_.wn, () => { double x1 = clamp(PXp() + jx, 30, W - 30), y1 = clamp(PYp() + jy, 30, H - 30); hazards.Add(L7Toss(x0, y0, x1, y1, hgt, B(hb - T_.wn), B(hb), fl * SPB / kf(T_), o)); });
		// in Easy the warning starts on a whole beat (a bit longer) so the attacks are not all removed
		double wB(double hb, double w) => easy ? Math.Floor(hb - w) : hb - w;
		void pie(double hb, T7 T_, double PR = 54, double jx = 0, double jy = 0, double dur = 1.6) { double wb = wB(hb, T_.wn + 0.5); ev(wb, () => { double x = clamp(PXp() + jx, PR * 0.6, W - PR * 0.6), y = clamp(PYp() + jy, PR * 0.6, H - PR * 0.6); hazards.Add(L7Pie(x, y, B(wb), B(hb), B(hb + dur), PR)); }); }
		void bal(double hb, double x, double pop, int n, T7 T_, double vy = 170, double yo = 0, double sa = 0, double sph = 0) => ev(hb - T_.wn, () => hazards.Add(L7Balloon(x, vy * kf(T_), B(hb - T_.wn), B(hb), B(pop), n, T_.sp * 0.55, yo, sa, sph)));

		/* ---------- phase 1: juggling (0:16 - 1:06) ---------- */
		// everything in order: clear arcs, balloon rows with a gap, pies at your spot on the beat
		var T = TI[0];
		// a balloon row rising from the bottom like a wall, with a gap (gap = the empty slot out of 9)
		void balRow(double hb, int gap, T7 T_, double vy = 175, double pop = 6, int gw = 1) { double ph = hb * 0.9; for (int i = 0; i < 9; i++) { if (i >= gap && i < gap + gw) continue; bal(hb, 70 + i * 142.5, hb + pop, 0, T_, vy, 90 * (0.5 + 0.5 * JS.sin(i * 0.8 + ph)), 55, i * 0.7); } }
		// the swirl's turn speed per beat: flips with the song (at the end every 2 beats)
		double spB(int b) => b < 24 ? 0.12 : b < 56 ? (Math.Floor((b - 24) / 8.0) % 2 != 0 ? -0.3 : 0.3) : b < 88 ? (Math.Floor((b - 56) / 8.0) % 2 != 0 ? -0.38 : 0.38) : b < 112 ? (Math.Floor((b - 88) / 2.0) % 2 != 0 ? -0.5 : 0.5) : 0.75;
		var SW = new List<double> { 0 }; for (int b = 0; b < 130; b++) SW.Add(SW[b] + spB(b));
		Func<double, double> swA = t => { double bt = clamp(BT(t), 0, 129.999); int i = (int)Math.Floor(bt); return SW[i] + (bt - i) * spB(i); };
		ev(0, () => hazards.Add(L7Swirl(B(0), B(120), swA)));
		ev(0, () => hazards.Add(L7Curtain(B(0), B(24))));
		int[] G0 = { 1, 6, 3, 7, 2, 5, 0, 4, 6, 2, 5, 1 };
		for (int k = 0; k < 12; k++) balRow(k * 2, G0[k], T, 175, 7, 2);
		// 8-23: a circle of balls turning and moving in an 8 in the arena, growing and shrinking with the beat, flips its turn at 16
		Func<double, double> oA = t => { double bt = BT(t); return bt < 16 ? Math.PI / 2 * (bt - 8) : Math.PI / 2 * 8 - Math.PI / 2 * (bt - 16); };
		ev(6, () => hazards.Add(L7Orbit(t => { double th = TAU * (BT(t) - 8) / 16; return (W / 2 + 360 * JS.sin(th), H / 2 + 140 * JS.sin(2 * th)); }, 7, t => 120 + 35 * JS.sin(Math.PI * BT(t)), oA, B(6), B(8), B(24), 18)));
		for (int b = 4; b < 24; b += (b < 8 ? 1 : 2)) pie(b, T, b % 2 != 0 ? 66 : 78, 0, 0, 1.6);
		foreach (var b in new double[] { 21, 23 }) pie(b, T, 70, 0, 0, 1.2);
		double[] JL = { 260, H - 52 }, JR = { W - 260, H - 52 }, hL = { JL[0] + 20, JL[1] - 30 }, hR = { JR[0] - 20, JR[1] - 30 };
		var TW = new TossO { twin = true, r = 17, R = 34, w = 6 };
		ev(22, () => { hazards.Add(L7Juggler(JL[0], JL[1], B(22), B(55))); hazards.Add(L7Juggler(JR[0], JR[1], B(22), B(55))); });
		fx(24, 0.6, 24); word(24, "HONK!", W / 2, H * 0.36, 90, 2.5, "rgba(255,255,255,0.75)");
		// 24-51: the clowns toss twin balls spinning around each other (a screw) on crossing arcs, and a big hoop every beat
		for (int b = 24; b < 52; b++)
		{
			if (b >= 40 && b < 48) continue; double h1 = new double[] { 240, 400, 320, 470 }[b % 4], h2 = new double[] { 460, 300, 420, 250 }[b % 4]; toss(b, hL[0], hL[1], hR[0], hR[1], h1, 2, T, TW); toss(b, hR[0], hR[1], hL[0], hL[1], h2, 2, T, TW);
			toss(b, b % 2 != 0 ? hL[0] : hR[0], hL[1], b % 2 != 0 ? hR[0] : hL[0], hL[1], new double[] { 330, 180, 260, 120 }[b % 4], 2.4, T, new TossO { hoop = true, R = 70 });
		}
		// 40-47: a fountain: every hand tosses a high twin
		for (int b = 40; b < 48; b++) foreach (var (x0, dx) in new[] { (hL[0], 180.0), (hR[0], -180.0), (hL[0] + 150, -150.0), (hR[0] - 150, 150.0) }) toss(b, x0, hL[1], x0 + dx, hL[1], new double[] { 520, 400 }[(b + (dx > 0 ? 0 : 1)) % 2], 2, T, b % 2 != 0 ? TW : new TossO { r = 20 });
		for (int b = 24; b < 52; b += 2) pie(b, T, 84, 0, 0, 2.0);
		for (int b = 26; b < 52; b += 4) balRow(b, new[] { 1, 6, 3, 7, 0, 4, 2 }[(b - 26) / 4], T, 190, 6, 2);
		// 52-55: they toss the twins at you
		foreach (var b in new[] { 52, 53, 54, 55 }) { tossP(b, hL[0], hL[1], 240, 2, T, TW); tossP(b, hR[0], hR[1], 240, 2, T, TW); toss(b, b % 2 != 0 ? hL[0] : hR[0], hL[1], W / 2, -60, 120, 2, T, new TossO { hoop = true, R = 70 }); }
		// 56-87: the long juggling staff: thrown from a side and spinning like a fan while it flies (its middle is empty), with twins at your spot between
		for (int b = 56; b < 88; b += 2)
		{
			double dr = (b / 2) % 2 != 0 ? -1 : 1, lx = new[] { 0.55, 0.75, 0.45, 0.65 }[(b >> 2) % 4], x0 = dr > 0 ? -120 : W + 120, x1 = dr > 0 ? W * lx : W * (1 - lx);
			toss(b, x0, H * 0.7, x1, H + 120, new double[] { 380, 460, 330, 420 }[(b >> 1) % 4], 3, T, new TossO { staff = true, L1 = 50, L2 = 250, spin = dr * 2.4, a0 = dr * 0.8, dots = 16 });
		}
		for (int b = 57; b < 88; b += 2) { double dr = ((b - 1) / 2) % 2 != 0 ? 1 : -1; tossP(b, dr > 0 ? -20 : W + 20, H * 0.45, 220, 2, T, TW); }
		for (int b = 56; b < 88; b += 2) balRow(b + 1, new[] { 2, 5, 0, 7, 3, 6, 1, 4 }[((b - 56) / 2) % 8], T, 200, 6, 2);
		for (int b = 58; b < 88; b += 4) pie(b, T, 84, 0, 0, 2.0);
		// 88-115: the cannons fire big balls that bounce on the floor, and the clowns come back
		var CN = new[] { new[] { 40, H - 40, -0.75 }, new[] { W - 40, H - 40, -Math.PI + 0.75 } }; var shots = new[] { new List<double>(), new List<double>() };
		for (int b = 88; b < 116; b++) foreach (var sd in new[] { 0, 1 }) { if ((b + sd) % 2 != 0 && b % 4 != 0) continue; shots[sd].Add(b); int q = (b >> 1) % 3; toss(b, CN[sd][0] + (sd != 0 ? -50 : 50), CN[sd][1] - 46, sd != 0 ? W * (0.36 + 0.1 * q) : W * (0.64 - 0.1 * q), H - 40, new double[] { 330, 250, 400 }[(q + sd) % 3], 2.6, T, new TossO { r = 40, bn = 4 }); }
		ev(86, () => { for (int s = 0; s < 2; s++) hazards.Add(L7Cannon(CN[s][0], CN[s][1], CN[s][2], B(86), B(118), shots[s].Select(b => B(b)).ToList())); });
		ev(88, () => { hazards.Add(L7Juggler(JL[0] + 170, JL[1], B(88), B(115))); hazards.Add(L7Juggler(JR[0] - 170, JR[1], B(88), B(115))); });
		for (int b = 88; b < 112; b++)
		{
			double a = JL[0] + 190, c = JR[0] - 190; toss(b, a, hL[1], c, hL[1], new double[] { 420, 560 }[(b >> 1) % 2], 2.2, T, b % 2 != 0 ? new TossO { r = 20 } : TW); toss(b, c, hL[1], a, hL[1], new double[] { 560, 420 }[(b >> 1) % 2], 2.2, T, b % 2 != 0 ? new TossO { r = 20 } : TW);
			if (b % 2 == 0) toss(b, W / 2 + (b % 4 != 0 ? -200 : 200), H + 40, b % 4 != 0 ? W - 140 : 140, -60, 140, 2.6, T, new TossO { hoop = true, R = 74 });
		}
		for (int b = 89; b < 112; b += 2) pie(b, T, 84, 0, 0, 2.0);
		for (int b = 90; b < 112; b += 4) balRow(b, new[] { 4, 1, 6, 2, 7, 3 }[((b - 90) / 4) % 6], T, 200, 6, 2);
		fx(88, 0.25, 12);
		// 112-119: a curtain of balls coming down from the top in order (left to right and back) with a gap
		for (int b = 112; b < 120; b += 2) { bool lr = (b / 2) % 2 == 0; int gp = new[] { 3, 6, 2, 5 }[(b - 112) / 2]; for (int i = 0; i < 10; i++) { if (i == gp || i == gp + 1) continue; double x = 70 + i * 127; toss(b + (lr ? i : 9 - i) * 0.08, x, -20, x, H + 30, 10, 2.0, T, new TossO { r = 22 }); } }
		foreach (var b in new double[] { 116, 117, 118 }) pie(b, T, 70, 0, 0, 1.0);

		/* ---------- phase 2: the carousel (1:06 - 1:48) ---------- */
		T = TI[1]; double K2 = kf(T); const double CX = W / 2, CY = H / 2, RF = 338;
		Func<double> t120 = () => B(120), t188 = () => B(188);
		// the carousel angle: turns faster from 1:35
		Func<double, double> angC = t => { double a = t120(), b = t188(); return t < b ? 0.36 * K2 * (t - a) : 0.36 * K2 * (b - a) + 0.55 * K2 * (t - b); };
		// a wave: a ring coming out of the carousel's core and growing, with a gap that turns with the carousel
		void wave(double hb, T7 T_, double gs = 0.9, double th = 16, double off = 0) => ev(hb - T_.wn, () => hazards.Add(Ring(CX, CY, B(hb), T_.sp * 0.62, th, angC(B(hb)) + off, gs, T_.wn * SPB)));
		fx(120, 0.45, 16); word(120, "ROUND & ROUND", W / 2, H / 2, 64, 2.2, "rgba(255,255,255,0.6)");
		// keep: if the player dies and comes back from checkpoint 120, the fence comes back with them
		E.Add(new Ev { b = 116, keep = true, f = () => hazards.Add(L7Fence(CX, CY, RF, B(116), B(120), B(124), B(214), B(218))) });
		ev(118, () => hazards.Add(L7Hub(CX, CY, B(118), B(216), angC)));
		// 122-187: the outer ring of horses
		ev(120, () => hazards.Add(L7Horses((CX, CY), 268, easy ? 8 : 10, new int[0], B(120), B(124), B(188), angC, easy ? 8 : 14, 23)));
		// 136-151: turning poles
		{ var T_ = T; ev(134 - T_.wn, () => hazards.Add(L7Poles(CX, CY, easy ? 2 : 3, 40, B(134 - T_.wn), B(136), B(152), t => 0.6 + 0.42 * K2 * (t - B(136))))); }
		for (int b = 124; b < 152; b += 4) wave(b, T, 1.0, 16, b % 8 != 0 ? Math.PI : 0);
		for (int b = 129; b < 152; b += 4) pie(b, T, 64, 0, 0, 1.6);
		for (int b = 152; b < 188; b += 4) wave(b, T, 0.9, 18, (b % 8 != 0 ? Math.PI : 0));
		for (int b = 153; b < 188; b += 4) pie(b, T, 64, 0, 0, 1.6);
		{ var T_ = T; ev(168 - T_.wn, () => hazards.Add(L7Poles(CX, CY, 3, 40, B(168 - T_.wn), B(168), B(186), t => 1.2 - 0.38 * K2 * (t - B(168))))); }
		// 188-215: the carousel speeds up
		fx(188, 0.4, 16);
		ev(186, () => hazards.Add(L7Horses((CX, CY), 255, easy ? 6 : 8, new int[0], B(186), B(188), B(212), angC, 12, 21)));
		{ var T_ = T; ev(188 - T_.wn, () => hazards.Add(L7Poles(CX, CY, 2, 40, B(188 - T_.wn), B(188), B(212), t => 2.0 + 0.5 * K2 * (t - B(188))))); }
		for (int b = 189; b < 212; b += 4) wave(b, T, 0.85, 18, b % 8 == 1 ? 0 : Math.PI);
		for (int b = 191; b < 212; b += 4) pie(b, T, 62, 0, 0, 1.6);
		// 212-219: the carousel breaks: two last waves
		fx(212, 0.35, 14); wave(212, T, 0.9, 20, 0); wave(216, T, 0.9, 20, Math.PI);
		ev(216, () => burst(CX, CY, "#fff", 30, 420));

		/* ---------- phase 3: the clown (1:48 - 2:30) ---------- */
		T = TI[2]; double K3 = kf(T);
		Func<double> tc0 = () => B(220);
		Func<double, (double, double)> cpos = t =>
		{
			double t0 = tc0(); if (t < t0) return (W / 2, -170); double e = t - t0, k = clamp(e / (2 * SPB), 0, 1), en = 1 - JS.pow(1 - k, 3), m = clamp((e - 2 * SPB) / (2 * SPB), 0, 1), d = clamp((t - B(254)) / (2 * SPB), 0, 1);
			return (W / 2 + JS.sin(e * 0.55) * (300 + 90 * d) * m, -170 + (340) * en + JS.sin(e * 1.1) * (36 + 50 * d) * m);
		};
		var laughs = new List<double>();
		Func<double, (double, double)> nose = t => { var (x, y) = cpos(t); return (x, y + 4); };
		void ha(double hb, int n, double sp, double a0) { laughs.Add(hb); ev(hb, () => { var (x, y) = nose(B(hb)); for (int i = 0; i < n; i++) { double a = a0 + i * TAU / n; hazards.Add(L7Ha(x + JS.cos(a) * 34, y + JS.sin(a) * 34, JS.cos(a) * sp, JS.sin(a) * sp)); } burst(x, y, PINK, 10, 260); }); }
		void haFan(double hb, int n, double spr, double sp) { laughs.Add(hb); ev(hb, () => { var (x, y) = nose(B(hb)); double a0 = JS.atan2(PYp() - y, PXp() - x); for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2.0) * spr; hazards.Add(L7Ha(x + JS.cos(a) * 34, y + JS.sin(a) * 34, JS.cos(a) * sp, JS.sin(a) * sp)); } }); }
		// a glove from a side: sd 0 left, 1 right, 2 bottom
		void glove(double hb, int sd, T7 T_, double jit)
		{
			double wb = wB(hb, T_.wn + 0.3);
			ev(wb, () =>
			{
				double px = PXp(), py = PYp(); double x0, y0; if (sd == 0) { x0 = -30; y0 = clamp(py + jit, 40, H - 40); } else if (sd == 1) { x0 = W + 30; y0 = clamp(py + jit, 40, H - 40); } else { x0 = clamp(px + jit, 40, W - 40); y0 = H + 30; }
				hazards.Add(L7Glove(x0, y0, px, py, B(wb), B(hb), B(hb + 0.5), B(hb + 1.2)));
			});
		}
		// the clown juggles: throws 3 balls at you (from above his head)
		void jug(double hb, T7 T_, double spread = 150)
		{
			double wb = wB(hb, T_.wn);
			for (int k = 0; k < 3; k++) { int k2 = k; double jx = (k2 - 1) * spread; ev(wb, () => { var (x, y) = cpos(B(wb)); double x1 = clamp(PXp() + jx, 30, W - 30), y1 = clamp(PYp(), 30, H - 30); hazards.Add(L7Toss(x + (k2 - 1) * 40, y - 90, x1, y1, 120, B(wb), B(hb), 2 * SPB / K3, new TossO { r = 13 })); }); }
		}
		Func<double> DK0 = () => B(252), DK1 = () => B(256);
		ev(218, () => hazards.Add(L7Clown(cpos, B(219), laughs.Select(b => B(b)).ToList(), DK0(), DK1(), B(256), B(312), B(316))));
		fx(220, 0.45, 18);
		// a mallet on a strip with you in it (vertical or horizontal)
		void mal(double hb, bool vert, double w, T7 T_, double jit = 0) { double wb = wB(hb, T_.wn + 0.4); ev(wb, () => { double c = vert ? clamp(PXp() + jit, w / 2, W - w / 2) : clamp(PYp() + jit, w / 2, H - w / 2); hazards.Add(L7Mallet(vert, c, w, B(wb), B(hb), B(hb + 0.85))); }); }
		// 221-251: laughs, gloves, mallet and juggling
		ha(221, 12, T.sp * 0.6, 0.3);
		for (int b = 222; b < 252; b += 2) ha(b, b % 4 == 2 ? 18 : 14, T.sp * 0.66, b * 0.37);
		for (int b = 223; b < 252; b++) if (b % 4 != 1) glove(b, b % 3, T, b % 2 != 0 ? 0 : (R() * 2 - 1) * 90);
		for (int b = 225; b < 252; b += 4) jug(b, T);
		for (int b = 225; b < 252; b += 2) mal(b, (b >> 1) % 2 == 0, 260, T, (R() * 2 - 1) * 80);
		// 228-251: horses turning around the clown
		ev(226, () => hazards.Add(L7Horses(cpos, 150, 7, new int[0], B(226), B(228), B(251), t => 1.1 * K3 * (t - B(228)), 0, 19)));
		// 252-255: the lights go out, and he laughs
		word(252, "هههههههه", W / 2, H * 0.72, 70, 3.6, "rgba(255,255,255,0.7)");
		// 256: he gets angry and 6 small bouncing clowns come out
		fx(256, 0.65, 26);
		{
			var T_ = T;
			ev(256, () => { var (x, y) = cpos(B(256)); double v = T_.sp * 0.62; foreach (var (a, b2) in new[] { (-1.0, 1.0), (1.0, 1.0), (-0.6, 1.4), (0.6, 1.4), (-1.4, 0.5), (1.4, 0.5) }) { double n = hypot(a, b2); hazards.Add(L7Mini(x + a * 40, y + 50, a / n * v, b2 / n * v, 31, B(256), B(287))); } });
		}
		for (int b = 258; b < 288; b += 2) ha(b, b % 4 == 2 ? 18 : 14, T.sp * 0.68, b * 0.41);
		for (int b = 257; b < 288; b++) if (b % 4 != 0) glove(b, b % 3, T, b % 2 != 0 ? 0 : (R() * 2 - 1) * 90);
		for (int b = 258; b < 288; b += 2) mal(b + 1, (b >> 1) % 2 == 1, 260, T, (R() * 2 - 1) * 80);
		for (int b = 258; b < 288; b += 4) bal(b, 140 + ((b * 7) % 9) * 125, b + 4, 8, T, 210);
		for (int b = 259; b < 288; b += 4) pie(b, T, 62, (R() * 2 - 1) * 120, (R() * 2 - 1) * 90, 1.6);
		// 288-311: the carousel comes back around the clown + balloons
		ev(286, () => hazards.Add(L7Horses(cpos, 170, 8, new int[0], B(286), B(288), B(311), t => -1.0 * K3 * (t - B(288)), 16, 19)));
		for (int b = 288; b < 312; b += 2) haFan(b, 7, 0.2, T.sp * 0.82);
		for (int b = 290; b < 312; b += 2) ha(b + 1, 14, T.sp * 0.62, b * 0.3);
		for (int b = 289; b < 312; b++) if (b % 4 != 3) glove(b, b % 3, T, b % 2 != 0 ? 0 : (R() * 2 - 1) * 90);
		for (int b = 292; b < 312; b += 8) jug(b, T, 170);
		for (int b = 290; b < 312; b += 2) mal(b + 1, (b >> 1) % 2 == 0, 260, T, (R() * 2 - 1) * 80);
		for (int b = 288; b < 308; b += 2) bal(b, 140 + ((b * 7) % 9) * 125, b + 4, 6, T, 210);
		// 312-316: he blows up and bursts
		ha(312, 14, T.sp * 0.62, 0.1);
		fx(316, 0.7, 0);
		sort(E, (a, b) => a.b - b.b); return E;
	}

	// AFK maker (v37/v38 replays): a ring or a pie on the spot
	static readonly int _afkL7 = AfkReg("buildLevel7", (x, y, b, v, w) => One(v >= 2 ? AFKRING(x, y, b, v, w) : L7Pie(x, y, B(b - w), B(b), B(b + 1), 80)));
}
