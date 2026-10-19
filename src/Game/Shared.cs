using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Helpers that one level defines and other levels also use. Ported here first so every level thread can build.
// SqBullet, Sweep, GapWall, Snake: lines 306-341 (level 2) | segDist, Shard, Slash: 472-498 (level 3)
// heartPath, GREY, ShardTri: 914-920 (friends 1) | R2, Squeeze: 1181, 1205 (friends 2) | GC: 1443 (friends 4) | RingSqueeze: 2047 (friends 6)
// a difficulty tier of a level: {sp, wn, gap} (gap 0 = not set)
public class Tier { public double sp, wn, gap; }

public static partial class G
{
	// line 6964: Easy makes each tier slower, with longer warnings and wider gaps
	public static Tier[] ezT(Tier[] a) => easy ? a.Select(t => new Tier { sp = t.sp * ezV("ezsp", EZ_SP), wn = t.wn * ezV("ezwn", EZ_WN), gap = t.gap != 0 ? t.gap * ezV("ezgw", EZ_GW) : t.gap }).ToArray() : a;

	public static Hz SqBullet(double x, double y, double vx, double vy, double s = 11)
	{
		var b = Bullet(x, y, vx, vy, s); double rot = rnd() * TAU;
		b.draw = t =>
		{
			rot += 0.08; g.save(); g.translate(b.x, b.y); g.rotate(rot); double a = b.r * 1.6 * (1 + 0.2 * pulse), c = b.r * (1 + 0.15 * pulse);
			g.fillStyle = "rgba(255,42,109,0.22)"; g.fillRect(-a, -a, 2 * a, 2 * a); g.fillStyle = PINK; g.fillRect(-c, -c, 2 * c, 2 * c); g.restore();
		};
		return b;
	}

	public static Hz Sweep(double px, double py, double a0, double a1, double w, double tw, double ta, double te)
	{
		const double len = 1700; Func<double, double> ang = t => a0 + (a1 - a0) * clamp((t - ta) / (te - ta), 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(px, py);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.08 * k)})"; g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, len, Math.Min(a0, a1), Math.Max(a0, a1)); g.closePath(); g.fill();
				g.save(); g.rotate(a0); g.fillStyle = "rgba(255,42,109,0.9)"; g.fillRect(0, -1.5, len, 3); g.restore();
			}
			else if (t < te) { g.save(); g.rotate(ang(t)); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK; double ww = w * (1 + 0.12 * pulse); g.fillRect(0, -ww / 2, len, ww); g.restore(); }
			else { double k = Math.Max(0, 1 - (t - te) / 0.2); g.save(); g.globalAlpha = k; g.rotate(a1); g.fillStyle = PINK; g.fillRect(0, -w * k / 2, len, w * k); g.restore(); }
			g.fillStyle = PINK; circle(0, 0, 14 + 4 * pulse); g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double a = ang(t); return obbHit(p.x, p.y, p.r, px + JS.cos(a) * len / 2, py + JS.sin(a) * len / 2, len / 2, w / 2, a); };
		return h;
	}

	public static Hz GapWall(string dir, double gc, double gh, double th, double sp, double tw, double ta)
	{
		bool vert = dir == "L" || dir == "R"; double sg = (dir == "L" || dir == "U") ? -1 : 1, ext = vert ? W : H;
		double st = sg < 0 ? ext + th / 2 : -th / 2; Func<double, double> pos = t => st + sg * sp * (t - ta);
		var h = new Hz();
		h.update = (t, dt) => { double p = pos(t); if (t > ta && (p < -th || p > ext + th)) h.dead = true; };
		h.draw = t =>
		{
			double a = gc - gh / 2, b = gc + gh / 2;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), e = 16; g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})";
				if (vert) { double x = sg < 0 ? W - e : 0; g.fillRect(x, 0, e, a); g.fillRect(x, b, e, H - b); } else { double y = sg < 0 ? H - e : 0; g.fillRect(0, y, a, e); g.fillRect(b, y, W - b, e); }
				return;
			}
			double p = pos(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK;
			if (vert) { g.fillRect(p - th / 2, 0, th, a); g.fillRect(p - th / 2, b, th, H - b); } else { g.fillRect(0, p - th / 2, a, th); g.fillRect(b, p - th / 2, W - b, th); }
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

	public static Hz Snake(Func<double, (double x, double y)> path, double t0, double t1, int n, double lag, double r, double tw)
	{
		Func<double, double> u = t => (t - t0) / (t1 - t0); Func<int, double> sz = i => r * (i == 0 ? 1.3 : 1 - i / (n * 1.6));
		var h = new Hz();
		h.update = (t, dt) => { if (u(t) - n * lag > 1.02) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.3 * k)})"; for (int i = 0; i <= 44; i++) { var q = path(i / 44.0); circle(q.x, q.y, 3); } return; }
			double uh = u(t);
			for (int i = n - 1; i >= 0; i--)
			{
				double v = uh - i * lag; if (v < 0 || v > 1) continue; var q = path(v); double s = sz(i) * (1 + 0.12 * pulse);
				g.save(); g.translate(q.x, q.y); g.rotate(v * 14); g.fillStyle = PINK; if (i == 0) { g.shadowColor = PINK; g.shadowBlur = 18; } g.fillRect(-s, -s, 2 * s, 2 * s); g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double uh = u(t);
			for (int i = 0; i < n; i++) { double v = uh - i * lag; if (v < 0 || v > 1) continue; var q = path(v); double s = sz(i); if (Math.Abs(p.x - q.x) < s + p.r - 1 && Math.Abs(p.y - q.y) < s + p.r - 1) return true; }
			return false;
		};
		return h;
	}

	public static double segDist(double px, double py, double[] a, double[] b)
	{
		double dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy; if (l2 == 0 || double.IsNaN(l2)) l2 = 1;
		double u = clamp(((px - a[0]) * dx + (py - a[1]) * dy) / l2, 0, 1); return hypot(px - a[0] - u * dx, py - a[1] - u * dy);
	}

	public static Hz Shard(double x, double y, double vx, double vy, double r = 8)
	{
		var b = Bullet(x, y, vx, vy, r); double a = JS.atan2(vy, vx);
		b.draw = t =>
		{
			g.save(); g.translate(b.x, b.y); g.rotate(a); double s = 1 + 0.15 * pulse;
			g.fillStyle = "rgba(255,42,109,0.22)"; g.beginPath(); g.moveTo(-19 * s, 0); g.lineTo(0, -9 * s); g.lineTo(19 * s, 0); g.lineTo(0, 9 * s); g.closePath(); g.fill();
			g.fillStyle = PINK; g.beginPath(); g.moveTo(-13 * s, 0); g.lineTo(0, -6 * s); g.lineTo(13 * s, 0); g.lineTo(0, 6 * s); g.closePath(); g.fill(); g.restore();
		};
		return b;
	}

	// a sword slash across the screen
	public static Hz Slash(double cx, double cy, double ang, double tw, double ta, double te, double w = 24)
	{
		const double L = 3400, cut = 0.09; double ax = cx - JS.cos(ang) * L / 2, ay = cy - JS.sin(ang) * L / 2; Func<double, double> pr_ = t => clamp((t - ta) / cut, 0, 1);
		void blade(double l, double ww) { g.beginPath(); g.moveTo(0, 0); g.lineTo(l * 0.5, -ww / 2); g.lineTo(l, 0); g.lineTo(l * 0.5, ww / 2); g.closePath(); g.fill(); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(ax, ay); g.rotate(ang);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * k)})"; for (double d = 0; d < L; d += 28) g.fillRect(d, -1.5, 15, 3); }
			else if (t < te) { double q = pr_(t); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = q < 1 ? "#fff" : PINK; blade(L * q, w * (1 + 0.15 * pulse)); }
			else { double k = Math.Max(0, 1 - (t - te) / 0.25); g.globalAlpha = k; g.fillStyle = PINK; blade(L, w * k); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double c = JS.cos(ang), s = JS.sin(ang), dx = p.x - ax, dy = p.y - ay, lx = dx * c + dy * s, ly = -dx * s + dy * c;
			if (lx < 0 || lx > L * pr_(t)) return false; double half = (w / 2) * (1 - Math.Abs(lx - L / 2) / (L / 2)); return Math.Abs(ly) < Math.Max(3, half) + p.r;
		};
		return h;
	}

	public static void heartPath(double s) { g.beginPath(); g.moveTo(0, 0.8 * s); g.bezierCurveTo(-1.2 * s, 0, -0.6 * s, -1.0 * s, 0, -0.35 * s); g.bezierCurveTo(0.6 * s, -1.0 * s, 1.2 * s, 0, 0, 0.8 * s); g.closePath(); }
	public const string GREY = "rgba(215,215,230,";
	public static Hz ShardTri(double x, double y, double vx, double vy, double r = 9)
	{
		var b = Bullet(x, y, vx, vy, r); double rot = rnd() * TAU; double sp = (rnd() < 0.5 ? -1 : 1) * (4 + rnd() * 4);
		b.draw = t =>
		{
			rot += sp * 0.016; g.save(); g.translate(b.x, b.y); g.rotate(rot); double s = b.r * 1.5 * (1 + 0.12 * pulse);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.moveTo(0, -s); g.lineTo(s * 0.8, s * 0.7); g.lineTo(-s * 0.6, s * 0.5); g.closePath(); g.fill(); g.restore();
		};
		return b;
	}

	public static double R2() => rnd();
	public static Hz Squeeze(double x0, double y0, double x1, double y1, double tw, double ta, double te)
	{
		Func<double, double> cl = t => t < ta ? 0 : t < ta + 0.12 ? (t - ta) / 0.12 : t < te ? 1 : clamp(1 - (t - te) / 0.25, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(0, 0, W, y0); g.fillRect(0, y1, W, H - y1); g.fillRect(0, y0, x0, y1 - y0); g.fillRect(x1, y0, W - x1, y1 - y0);
				g.strokeStyle = "rgba(255,42,109,0.9)"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 8 }); g.strokeRect(x0, y0, x1 - x0, y1 - y0); g.setLineDash(new double[0]); return;
			}
			double kk = cl(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.1 ? "#fff" : PINK;
			g.fillRect(0, 0, W, y0 * kk); g.fillRect(0, H - (H - y1) * kk, W, (H - y1) * kk); g.fillRect(0, 0, x0 * kk, H); g.fillRect(W - (W - x1) * kk, 0, (W - x1) * kk, H); g.restore();
		};
		h.hit = (p, t) => { if (cl(t) < 0.9 || t >= te) return false; return p.x - p.r < x0 || p.x + p.r > x1 || p.y - p.r < y0 || p.y + p.r > y1; };
		return h;
	}

	public const double GC = 8, GR = 4, GW = W / GC, GH = H / GR;

	public static Hz RingSqueeze(double x, double y, double r0, double r1, double tw, double ta, double te, double th, double gap, double gs)
	{
		Func<double, double> rad = t => { double k = clamp((t - ta) / (te - ta), 0, 1); return r0 + (r1 - r0) * (k * (2 - k)); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.strokeStyle = PINK;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.globalAlpha = 0.25 + 0.45 * k; g.lineWidth = 3; g.beginPath(); g.arc(x, y, r0, gap + gs / 2, gap - gs / 2 + TAU); g.stroke();
				g.globalAlpha = 0.5 * k; g.fillStyle = GREY + "0.5)"; foreach (var s in new double[] { -1, 1 }) { double a = gap + s * gs / 2; circle(x + JS.cos(a) * Math.Min(r0, 700), y + JS.sin(a) * Math.Min(r0, 700), 6); }
				g.globalAlpha = 1; return;
			}
			double f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25), r = rad(t); g.globalAlpha = f; g.lineWidth = th * (1 + 0.15 * pulse); g.shadowColor = PINK; g.shadowBlur = 14;
			g.beginPath(); g.arc(x, y, Math.Max(1, r), gap + gs / 2, gap - gs / 2 + TAU); g.stroke(); g.shadowBlur = 0; g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double r = rad(t), dx = p.x - x, dy = p.y - y, d = hypot(dx, dy); if (Math.Abs(d - r) > th / 2 + p.r - 2) return false;
			double da = JS.atan2(dy, dx) - gap; da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) > gs / 2 - p.r / Math.Max(d, 1);
		};
		return h;
	}
}
