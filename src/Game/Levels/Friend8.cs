using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 6540-6835: Friend 8 (Link: On The Floor), the F8* helpers and buildFriend8
// a tier of this level: {sp, wn, base} (ezT keeps base as it is)
public class F8Tier { public double sp, wn, @base; }
// options of row(): {sp, r, n, m, v, wn, ph, life, same} (null = not set)
public class F8RowO { public double? sp, r, n, m, v, wn, ph, life; public bool same; }
// one attack in the F8Gen queue: {b, f, c}
public class F8Q { public double b; public Action f; public bool c; }

public static partial class G
{
	public const int F8NC = 16; public const double F8CW = W / 16;
	// JS `o.k||d`
	static double F8or(double? a, double d) => a is double v && v != 0 && !double.IsNaN(v) ? v : d;

	// the song's real sound: 16 digits (0-9) per half beat
	public static double F8eq(double hb, int i) { string s = F8EQ; double k = max(0, round(hb * 2)) * 16 + i; return k < s.Length ? (s[(int)k] - 48) / 9.0 : 0.5; }

	// a word (decor)
	public static Hz F8Word(string s, double x, double y, double size, double t0, double t1, string col)
	{
		var z = new Hz();
		z.update = (t, dt) => { if (t > t1) z.dead = true; };
		z.draw = t =>
		{
			if (t < t0) return; double e = t - t0, a = min(1, e / 0.08, (t1 - t) / 0.3), sc = 1 + 0.3 * JS.exp(-e * 10); g.globalAlpha = max(0, a); g.save(); g.translate(x, y); g.scale(sc, sc); g.font = $"{str(size)}px {TFONT}"; g.fillStyle = !string.IsNullOrEmpty(col) ? col : GREY + "0.5)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, 0, 0); g.restore(); g.globalAlpha = 1;
		};
		z.hit = (p, t) => false;
		return z;
	}
	// a column across: from x0 to x1 inside column i
	public static (double, double) F8cx(int i) => (i * F8CW + 5, (i + 1) * F8CW - 5);

	/* ---- phase 1: the equalizer ---- */
	public static Hz F8EqDecor(double t0, double t1)
	{
		var z = new Hz();
		z.update = (t, dt) => { if (t > t1 + 1) z.dead = true; };
		z.draw = t =>
		{
			double a = min(1, (t - t0) / 1, (t1 + 1 - t) / 1); if (a <= 0) return; double hb = BT(t); g.globalAlpha = a * 0.5;
			for (int i = 0; i < F8NC; i++) { double v = F8eq(hb, i), h = 40 + v * 170; var (x0, x1) = F8cx(i); g.fillStyle = GREY + "0.10)"; g.fillRect(x0, H - h, x1 - x0, h); g.fillStyle = GREY + "0.35)"; g.fillRect(x0, H - h - 6, x1 - x0, 3); }
			g.globalAlpha = 1;
		};
		z.hit = (p, t) => false;
		return z;
	}
	public static Hz F8Bars(double[] fl, double[] cl, double tw, double ta, double hold, double dec)
	{
		double te = ta + hold + dec; Func<double, double> env = t => t < ta ? 0 : t < ta + 0.05 ? (t - ta) / 0.05 : t < ta + hold ? 1 : t < te ? JS.pow(1 - (t - ta - hold) / dec, 2) : 0;
		var z = new Hz();
		z.update = (t, dt) => { if (t > te) z.dead = true; };
		z.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); for (int i = 0; i < F8NC; i++)
				{
					var (x0, x1) = F8cx(i);
					double fa = k * k * 0.13;
					if (fl[i] > 0) { g.fillStyle = $"rgba(255,42,109,{str(fa)})"; g.fillRect(x0, H - fl[i], x1 - x0, fl[i]); g.fillStyle = k > 0.6 ? $"rgba(255,42,109,{toFixed(0.5 + 0.5 * k, 3)})" : GREY + toFixed(0.25 + 0.6 * k, 3) + ")"; g.fillRect(x0, H - fl[i] - 2, x1 - x0, 4); }
					if (cl[i] > 0) { g.fillStyle = $"rgba(255,42,109,{str(fa)})"; g.fillRect(x0, 0, x1 - x0, cl[i]); g.fillStyle = k > 0.6 ? $"rgba(255,42,109,{toFixed(0.5 + 0.5 * k, 3)})" : GREY + toFixed(0.25 + 0.6 * k, 3) + ")"; g.fillRect(x0, cl[i] - 2, x1 - x0, 4); }
				}
				return;
			}
			double e = env(t); if (e <= 0) return; bool fl0 = (t - ta) < 0.08; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = fl0 ? PINKW : "rgba(255,42,109,0.78)";
			for (int i = 0; i < F8NC; i++) { var (x0, x1) = F8cx(i); if (fl[i] > 0) { double h = fl[i] * e; g.fillRect(x0, H - h, x1 - x0, h); } if (cl[i] > 0) { double h = cl[i] * e; g.fillRect(x0, 0, x1 - x0, h); } }
			if (!fl0) { g.fillStyle = PINK; for (int i = 0; i < F8NC; i++) { var (x0, x1) = F8cx(i); if (fl[i] > 0) { double h = fl[i] * e; g.fillRect(x0, H - h, x1 - x0, min(h, 12)); } if (cl[i] > 0) { double h = cl[i] * e; g.fillRect(x0, max(0, h - 12), x1 - x0, min(h, 12)); } } }
			g.restore(); g.fillStyle = "rgba(255,255,255,0.35)"; for (int i = 0; i < F8NC; i++) { var (x0, x1) = F8cx(i); if (fl[i] > 0) { double h = fl[i] * e; for (double y = H - 14; y > H - h + 6; y -= 22) g.fillRect(x0 + 4, y, x1 - x0 - 8, 6); } if (cl[i] > 0) { double h = cl[i] * e; for (double y = 8; y < h - 12; y += 22) g.fillRect(x0 + 4, y, x1 - x0 - 8, 6); } }
		};
		z.hit = (p, t) =>
		{
			double e = env(t); if (t < ta || e < 0.2) return false; Func<int, bool> ch = i => i >= 0 && i < F8NC && ((fl[i] > 0 && p.y + p.r - 2 > H - fl[i] * e) || (cl[i] > 0 && p.y - p.r + 2 < cl[i] * e));
			for (int i = 0; i < F8NC; i++) { if (p.x + p.r - 2 < i * F8CW || p.x - p.r + 2 > (i + 1) * F8CW) continue; if (ch(i)) return true; }
			return false;
		};
		return z;
	}
	// the floor comes up: a pink slab from below up to hm and back
	public static Hz F8Slab(double hm, double tw, double ta, double te)
	{
		Func<double, double> h = t => t < ta ? 0 : t < ta + 0.3 ? hm * (t - ta) / 0.3 : t < te ? hm : t < te + 0.3 ? hm * (1 - (t - te) / 0.3) : 0;
		var z = new Hz();
		z.update = (t, dt) => { if (t > te + 0.3) z.dead = true; };
		z.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(0, H - hm, W, hm); g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(0, H - hm - 2, W, 4); return; }
			double v = h(t); if (v <= 0) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.1 ? PINKW : PINK; g.fillRect(0, H - v, W, v); g.restore();
			g.fillStyle = "rgba(255,255,255,0.3)"; for (int i = 0; i < F8NC; i++) g.fillRect(i * F8CW + 4, H - v + 8, F8CW - 8, 5);
		};
		z.hit = (p, t) => { double v = h(t); return t >= ta && v > 20 && p.y + p.r - 2 > H - v; };
		return z;
	}
	// the equalizer wave (chorus): yc(i,t) = middle of the gap in column i
	public static Hz F8Wave(Func<double, double, double> yc, Func<double, double> gp, double tw, double ta, double te)
	{
		var z = new Hz();
		z.update = (t, dt) => { if (t > te + 0.4) z.dead = true; };
		z.draw = t =>
		{
			bool live = t >= ta && t < te; double a = t < ta ? 0.12 + 0.25 * clamp((t - tw) / (ta - tw), 0, 1) : t < te ? 1 : clamp(1 - (t - te) / 0.4, 0, 1); if (a <= 0) return;
			double pu = 1 + 0.06 * pulse; g.save(); if (live) { g.shadowColor = PINK; g.shadowBlur = 14; }
			g.globalAlpha = live ? 1 : a; g.fillStyle = PINK;
			for (int i = 0; i < F8NC; i++) { var (x0, x1) = F8cx(i); double c = yc(i, t), hg = gp(t) / 2 * (live ? 1 : pu); g.fillRect(x0, 0, x1 - x0, max(0, c - hg)); g.fillRect(x0, c + hg, x1 - x0, H - c - hg); }
			g.restore(); g.globalAlpha = live ? 0.35 : a * 0.5; g.fillStyle = "#fff"; for (int i = 0; i < F8NC; i++) { var (x0, x1) = F8cx(i); double c = yc(i, t), hg = gp(t) / 2; g.fillRect(x0, c - hg - 5, x1 - x0, 4); g.fillRect(x0, c + hg + 1, x1 - x0, 4); }
			g.globalAlpha = 1;
		};
		z.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; Func<int, bool> ch = i => { if (i < 0 || i >= F8NC) return false; double c = yc(i, t), hg = gp(t) / 2; return p.y - p.r + 2 < c - hg || p.y + p.r - 2 > c + hg; };
			for (int i = 0; i < F8NC; i++) { if (p.x + p.r - 2 < i * F8CW || p.x - p.r + 2 > (i + 1) * F8CW) continue; if (ch(i)) return true; }
			return false;
		};
		return z;
	}

	/* ---- phase 2: the crowd ---- */
	public static Hz F8CrowdDecor(double t0, double t1)
	{
		var z = new Hz();
		z.update = (t, dt) => { if (t > t1 + 1) z.dead = true; };
		z.draw = t =>
		{
			double a = min(1, (t - t0) / 0.8, (t1 + 1 - t) / 1); if (a <= 0) return; double bt = BT(t); g.globalAlpha = a; g.fillStyle = GREY + "0.22)";
			for (int i = 0; i < 34; i++) { double x = i * 39 + 12, bob = Math.Abs(JS.sin(Math.PI * (bt + i * 0.13))) * 6; circle(x, H - 14 - bob, 13); g.fillRect(x - 15, H - 6 - bob, 30, 20); circle(x + 19, 24 + bob * 0.6, 11); g.fillRect(x + 6, 0, 26, 14 + bob * 0.6); }
			g.globalAlpha = 1;
		};
		z.hit = (p, t) => false;
		return z;
	}
	// a raised hand (drawing): from the floor (sd 1) or from above (sd -1)
	public static void F8arm(double x, double h, double sd, double w) { if (h < 4) return; double y0 = sd > 0 ? H : 0, y1 = sd > 0 ? H - h : h; g.fillRect(x - w / 2, min(y0, y1), w, Math.Abs(y1 - y0)); circle(x, y1, w * 0.85); }
	// the crowd wave (La Ola)
	public static Hz F8Ola(double side, double dir, double h, double w, double v, double tw, double ta)
	{
		Func<double, double> cx = t => dir > 0 ? -w / 2 + v * (t - ta) : W + w / 2 - v * (t - ta);
		Func<double, double, double> prof = (x, c) => { double q = (x - c) / (w / 2); return Math.Abs(q) >= 1 ? 0 : h * JS.pow(JS.cos(q * Math.PI / 2), 1.4); };
		var z = new Hz();
		z.update = (t, dt) => { if (t > ta && (t - ta) * v > W + w + 40) z.dead = true; };
		z.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), ex = dir > 0 ? 0 : W; g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * k)})"; double y = side > 0 ? H - h : h;
				g.beginPath(); g.moveTo(ex, y - 16); g.lineTo(ex + dir * 34 * k + dir * 8, y); g.lineTo(ex, y + 16); g.closePath(); g.fill(); g.fillRect(dir > 0 ? 0 : W - 6, side > 0 ? H - h : 0, 6, h); return;
			}
			double c = cx(t); g.save(); g.fillStyle = "rgba(255,42,109,0.16)"; g.beginPath(); g.moveTo(c - w / 2, side > 0 ? H : 0); for (double x = c - w / 2; x <= c + w / 2; x += 8) { double hh = prof(x, c); g.lineTo(x, side > 0 ? H - hh : hh); }
			g.lineTo(c + w / 2, side > 0 ? H : 0); g.closePath(); g.fill();
			g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; for (double x = Math.Ceiling((c - w / 2) / 22) * 22; x < c + w / 2; x += 22) F8arm(x, prof(x, c), side, 9); g.restore();
		};
		z.hit = (p, t) => { if (t < ta) return false; double c = cx(t); if (Math.Abs(p.x - c) > w / 2) return false; double hh = prof(p.x, c); if (hh < 8) return false; return side > 0 ? p.y + p.r - 2 > H - hh : p.y - p.r + 2 < hh; };
		return z;
	}
	// the whole crowd jumps: hands from below and above reach the middle, except one gap (gx)
	public static Hz F8Jump(double gx, double gw, double tw, double ta, double te, double sd = 0, double rh = H / 2 + 8)
	{
		Func<double, double> k = t => t < ta ? 0 : t < ta + 0.12 ? (t - ta) / 0.12 : t < te ? 1 : clamp(1 - (t - te) / 0.25, 0, 1);
		double y0 = sd < 0 ? 0 : sd > 0 ? H - rh : 0, hh0 = sd == 0 ? H : rh;
		var z = new Hz();
		z.update = (t, dt) => { if (t > te + 0.25) z.dead = true; };
		z.draw = t =>
		{
			if (t < ta)
			{
				double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * q)})"; g.fillRect(0, y0, gx - gw / 2, hh0); g.fillRect(gx + gw / 2, y0, W - gx - gw / 2, hh0); g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(gx - gw / 2 - 2, y0, 4, hh0); g.fillRect(gx + gw / 2 - 2, y0, 4, hh0);
				if (sd != 0) { double yl = sd > 0 ? H - rh : rh; g.fillRect(0, yl - 2, max(0, gx - gw / 2), 4); g.fillRect(gx + gw / 2, yl - 2, W - gx - gw / 2, 4); }
				return;
			}
			double e = k(t); if (e <= 0) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.1 ? PINKW : PINK;
			for (double x = 11; x < W; x += 22) { if (Math.Abs(x - gx) < gw / 2 + 6) continue; double hh = rh * e * (0.92 + 0.08 * JS.sin(x * 0.7)); if (sd >= 0) F8arm(x, hh, 1, 11); if (sd <= 0) F8arm(x + 6, hh, -1, 11); }
			g.restore();
		};
		z.hit = (p, t) => { if (t < ta || k(t) <= 0.7 || Math.Abs(p.x - gx) <= gw / 2 - p.r + 2) return false; if (sd == 0) return true; double r = rh * 0.84 * k(t); return sd > 0 ? p.y + p.r - 2 > H - r : p.y - p.r + 2 < r; };
		return z;
	}
	// a clap: two big hands close on (x,y)
	public static Hz F8Clap(double x, double y, double hw, double hh, double tw, double ta, double te)
	{
		double D = hw * 3.2; Func<double, double> pos = t => t < ta ? D : t < ta + 0.08 ? D * (1 - (t - ta) / 0.08) : t < te ? 0 : min(D, D * (t - te) / 0.3);
		var z = new Hz();
		z.update = (t, dt) => { if (t > te + 0.3) z.dead = true; };
		z.draw = t =>
		{
			double d = pos(t); if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; g.fillRect(x - D - hw, y - hh / 2, 2 * (D + hw), hh); g.strokeStyle = "rgba(255,42,109,0.8)"; g.lineWidth = 2;
				foreach (var s in new double[] { -1, 1 }) { double cx = x + s * (D + hw / 2); g.strokeRect(cx - hw / 2, y - hh / 2, hw, hh); }
				return;
			}
			g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.12 ? PINKW : PINK; foreach (var s in new double[] { -1, 1 }) { double cx = x + s * (d + hw / 2); g.fillRect(cx - hw / 2, y - hh / 2 + 18, hw, hh - 18); for (int f = 0; f < 4; f++) g.fillRect(cx - hw / 2 + f * hw / 4 + 2, y - hh / 2, hw / 4 - 4, 26); g.fillRect(s < 0 ? cx + hw / 2 - 8 : cx - hw / 2 - 16, y - 6, 24, 40); }
			g.restore(); if (t >= ta && t < ta + 0.25) { g.globalAlpha = 1 - (t - ta) / 0.25; g.strokeStyle = "#fff"; g.lineWidth = 3; for (int i = 0; i < 6; i++) { double a = i * TAU / 6; g.beginPath(); g.moveTo(x + JS.cos(a) * 30, y + JS.sin(a) * 30); g.lineTo(x + JS.cos(a) * 70, y + JS.sin(a) * 70); g.stroke(); } g.globalAlpha = 1; }
		};
		z.hit = (p, t) => { if (t < ta || t > te) return false; double d = pos(t); if (Math.Abs(p.y - y) > hh / 2 + p.r - 2) return false; return Math.Abs(p.x - x) < d + hw + p.r - 2 && (Math.Abs(p.x - x) > d - p.r + 2 || d < 4); };
		return z;
	}
	// a glow stick thrown up from the crowd, spinning
	public static Hz F8Stick(double x, double vy, double w0, double L, double tw)
	{
		double y = H + 30, a = 0; var z = new Hz();
		z.update = (t, dt) => { if (t < tw) return; y += vy * dt; a += w0 * dt; if (y < -80 || y > H + 90) z.dead = true; };
		z.draw = t => { if (t < tw) return; g.save(); g.translate(x, y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(-L / 2, -6, L, 12); g.fillStyle = "rgba(255,255,255,0.6)"; g.fillRect(-L / 2 + 6, -2, L - 12, 4); g.restore(); };
		z.hit = (p, t) => t >= tw && obbHit(p.x, p.y, p.r - 2, x, y, L / 2, 6, a);
		return z;
	}

	// stage lights (decor): 4 grey spots swaying with the beat
	public static Hz F8Spots(double t0, double t1)
	{
		var z = new Hz();
		z.update = (t, dt) => { if (t > t1 + 1) z.dead = true; };
		z.draw = t =>
		{
			double a = min(1, (t - t0) / 1, (t1 + 1 - t) / 1); if (a <= 0) return; double bt = BT(t); g.save(); g.globalAlpha = a;
			for (int i = 0; i < 4; i++)
			{
				double x = W * (0.14 + i * 0.24), an = Math.PI / 2 + 0.45 * JS.sin(TAU * (bt / 8 + i * 0.27)), L = 900, sp = 0.13;
				var gr = g.createLinearGradient(x, 0, x + JS.cos(an) * L, JS.sin(an) * L); gr.addColorStop(0, GREY + toFixed(0.16 + 0.08 * pulse, 3) + ")"); gr.addColorStop(1, GREY + "0)"); g.fillStyle = gr;
				g.beginPath(); g.moveTo(x, 0); g.lineTo(x + JS.cos(an - sp) * L, JS.sin(an - sp) * L); g.lineTo(x + JS.cos(an + sp) * L, JS.sin(an + sp) * L); g.closePath(); g.fill();
			}
			g.restore();
		};
		z.hit = (p, t) => false;
		return z;
	}
	/* ---- phase 3: the disco ball ---- */
	// grey light dots on the wall behind (decor): they move with the ball's spin
	public static Hz F8DiscoBg(Func<double, double> ang, double t0, double t1)
	{
		var P = new List<(double, double, double)>(); for (int i = 0; i < 70; i++) P.Add(((i * 137.5) % 1, ((i * 0.618) % 1), 2 + (i % 3)));
		var z = new Hz();
		z.update = (t, dt) => { if (t > t1) z.dead = true; };
		z.draw = t =>
		{
			double a = min(1, (t - t0) / 1.5, (t1 - t) / 0.3); if (a <= 0) return; double an = ang(t); g.globalAlpha = a; g.fillStyle = GREY + "0.22)";
			foreach (var (u, v, r) in P) { double x = (((u + an * 0.08) % 1 + 1) % 1) * W, y = v * H; circle(x, y, r); }
			g.globalAlpha = 1;
		};
		z.hit = (p, t) => false;
		return z;
	}
	// pos(t) where the ball is, ang(t) its spin. It charges from tc and explodes at tx (harmless)
	public static Hz F8Ball(Func<double, (double, double)> pos, Func<double, double> ang, double t0, double tc, double tx)
	{
		bool bk = false; var z = new Hz();
		z.update = (t, dt) =>
		{
			if (t >= tx) { if (!bk) { bk = true; var (x, y) = pos(t); for (int i = 0; i < 5; i++) burst(x, y, i % 2 != 0 ? PINKW : PINK, 24, 620); shake = max(shake, 26); flash = max(flash, 0.9); } z.dead = true; }
		};
		z.draw = t =>
		{
			if (t < t0) return; var (x, y) = pos(t); double r = 74, an = ang(t), ch = t > tc ? clamp((t - tc) / (tx - tc), 0, 1) : 0;
			g.strokeStyle = GREY + "0.4)"; g.lineWidth = 3; g.beginPath(); g.moveTo(x, y - r); g.lineTo(x, -10); g.stroke();
			g.save(); g.shadowColor = ch > 0 ? PINKW : PINK; g.shadowBlur = 26 + 40 * ch; g.fillStyle = PINK; circle(x, y, r * (1 + 0.04 * pulse + 0.08 * ch)); g.restore();
			g.save(); g.beginPath(); g.arc(x, y, r, 0, TAU); g.clip();
			for (int la = -3; la <= 3; la++)
			{
				double yy = y + la * r / 3.6, hw = Math.Sqrt(max(0, r * r - (yy - y) * (yy - y))); for (int k = 0; k < 12; k++)
				{
					double u = ((k / 12.0 + an / TAU) % 1 + 1) % 1, xx = x + hw * JS.cos(u * TAU + Math.PI), sh = JS.sin(u * TAU + Math.PI);
					if (sh < 0) continue; g.fillStyle = $"rgba(255,255,255,{toFixed(0.15 + 0.55 * sh * (0.5 + 0.5 * JS.sin(k * 1.7 + la + an * 3)), 3)})"; g.fillRect(xx - 5, yy - 4, 10, 8);
				}
			}
			g.restore(); g.fillStyle = $"rgba(255,255,255,{toFixed(0.6 * ch, 3)})"; circle(x, y, r * 0.9);
		};
		z.hit = (p, t) => { if (t < t0 + 0.4) return false; var (x, y) = pos(t); return hypot(p.x - x, p.y - y) < 66 + p.r - 2; };
		return z;
	}
	// a row of light dots (the ball's reflection on the wall)
	public static Hz F8Row(double y0, double vy, double sp, double r, double n, double m, double ph, double v, double tw, double ta, double te, Func<double, double> mt, Func<double, double> sw)
	{
		Func<double, double> Y = t => y0 + vy * (mt(t) - mt(ta)), X0 = t => ph + v * (mt(t) - mt(tw)); Func<double, bool> miss = k => (((k % n) + n) % n) < m;
		var z = new Hz();
		z.update = (t, dt) => { double y = Y(t); if (t > te + 0.3 || (t > ta && (y < -60 || y > H + 60))) z.dead = true; };
		z.draw = t =>
		{
			double y = Y(t), x0 = X0(t), k0 = Math.Floor((-40 - x0) / sp), k1 = Math.Ceiling((W + 40 - x0) / sp);
			if (t < ta) { double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.45 * q)})"; for (double k = k0; k <= k1; k++) if (!miss(k)) { circle(x0 + k * sp, y, r * 0.45); if (!miss(k + 1)) g.fillRect(x0 + k * sp, y - 1, sp, 2); } return; }
			double a = t < te ? 1 : clamp(1 - (t - te) / 0.3, 0, 1), rr = r * sw(t); g.globalAlpha = a * 0.85; g.fillStyle = PINK; for (double k = k0; k <= k1; k++) if (!miss(k) && !miss(k + 1)) g.fillRect(x0 + k * sp, y - 3, sp, 6); g.globalAlpha = a; g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; for (double k = k0; k <= k1; k++) if (!miss(k)) circle(x0 + k * sp, y, rr); g.restore();
			g.fillStyle = "rgba(255,255,255,0.7)"; for (double k = k0; k <= k1; k++) if (!miss(k)) circle(x0 + k * sp - rr * 0.25, y - rr * 0.25, rr * 0.3); g.globalAlpha = 1;
		};
		z.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double y = Y(t), rr = r * sw(t); if (Math.Abs(p.y - y) > rr + p.r - 2) return false; double x0 = X0(t), k = round((p.x - x0) / sp), kf = Math.Floor((p.x - x0) / sp); if (Math.Abs(p.y - y) < 3 + p.r - 2 && !miss(kf) && !miss(kf + 1)) return true; if (miss(k)) return false; return hypot(p.x - (x0 + k * sp), p.y - y) < rr + p.r - 2;
		};
		return z;
	}

	// fires the phase's attacks on time (behind the scenes, not drawn, harmless)
	public static Hz F8Gen(List<F8Q> list)
	{
		int i = 0; var z = new Hz();
		z.update = (t, dt) => { z.ezL = null; while (i < list.Count && B(list[i].b) <= t) { list[i].f(); i++; } if (i >= list.Count) z.dead = true; };
		z.draw = t => { };
		z.hit = (p, t) => false;
		return z;
	}
	public static List<Ev> buildFriend8()
	{
		var E = new List<Ev>(); var R = mulberry32(8008); void evk(double b, Action f) => E.Add(new Ev { b = b, keep = true, f = f });
		int CV = 0; var GQ = new[] { new List<F8Q>(), new List<F8Q>(), new List<F8Q>() }; void ev(double b, Action f) => GQ[b < 120 ? 0 : b < 184 ? 1 : 2].Add(new F8Q { b = b, f = f, c = CV > 0 });
		void C(Action f, bool on = true) { if (on) CV++; f(); if (on) CV--; }
		var DF = (sp: 1.0, hd: 1.0, gp: 1.0, d1: 1.0, d2: 1.0, d3: 1.0);
		var TB = new double[] { 520, 540, 560 };
		var TI = ezT(new[] { new Tier { sp = 520 * DF.sp, wn = 1.5 }, new Tier { sp = 540 * DF.sp, wn = 1.5 }, new Tier { sp = 560 * DF.sp, wn = 1.4 } }).Select((q, k) => new F8Tier { sp = q.sp, wn = q.wn, @base = TB[k] }).ToArray();
		const double CX = W / 2, CY = H / 2;
		double PXp() => player != null ? player.x : CX; double PYp() => player != null ? player.y : CY;
		void fx(double b, double f2, double s2) => C(() => ev(b, () => { flash = max(flash, f2); shake = max(shake, s2); }));
		void word(double b, string s, double x, double y, double size, double dur, string col) => C(() => ev(b, () => hazards.Add(F8Word(s, x, y, size, B(b), B(b + dur), col))));
		double wB(double hb, double w) => easy ? Math.Floor(hb - w) : hb - w;
		const int NC = F8NC; double[] Z() => new double[NC];
		void bars(double hb, double[] fl, double[] cl, F8Tier T, double hold = 0.12, double dec = 0.34) { double wb = wB(hb, T.wn); hold *= DF.hd; ev(wb, () => hazards.Add(F8Bars(fl, cl, B(wb), B(hb), hold, dec))); }
		double tall(double hb, int i, double lo = 0.7) { double mn = 1, mx = 0; for (int j = 0; j < NC; j++) { double v0 = F8eq(hb, j); mn = min(mn, v0); mx = max(mx, v0); } double v = mx - mn > 0.05 ? (F8eq(hb, i) - mn) / (mx - mn) : 0.5; return H * (lo + (1 - lo) * v); }
		void safeHit(double hb, double s, double n, F8Tier T, double low = H * 0.2, double lo = 0.45, bool full = false) { n = max(1, round(n * DF.gp)); var fl = Z(); var cl = Z(); for (int i = 0; i < NC; i++) { bool sf = i >= s && i < s + n; fl[i] = sf ? low : tall(hb, i, lo); if (full && !sf) cl[i] = H - fl[i] + 24; } bars(hb, fl, cl, T); }

		/* ---------- phase 1: the equalizer ---------- */
		var T = TI[0];
		evk(0, () => hazards.Add(F8EqDecor(B(0), B(121))));
		word(2, "ON THE FLOOR", CX, H * 0.2, 64, 2, "rgba(255,255,255,0.5)");
		C(() => safeHit(3, 7, 2, T, H * 0.15, 0.8, true)); fx(3, 0.3, 12);
		var SAFE1 = new int[] { 7, 7, 6, 5, 5, 4, 4, 6, 6, 7, 8, 9, 10, 10, 9, 8, 8, 7, 6, 5, 4, 3, 3, 4, 5, 6, 7, 8, 9, 10 };
		for (int b = 4; b < 12; b++)
		{
			C(() => safeHit(b, SAFE1[b - 4], 2, T, H * 0.2, 0.45, b % 2 == 0), b % 2 == 0);
			if (DF.d1 != 0 && b % 2 == 1) { var cl = Z(); int s = SAFE1[b - 4]; for (int i = s - 1; i < s + 3; i++) if (i >= 0 && i < NC) cl[i] = H * 0.3; bars(b + 0.5, Z(), cl, T, 0.08, 0.25); }
		}
		for (int b = 12; b < 20; b += easy ? 2 : 1) { var fl = Z(); double c0 = Math.Floor(b / 2.0) % 2; for (int i = 0; i < NC; i++) fl[i] = i % 2 == c0 ? H : H * 0.16; var cl = Z(); if (DF.d1 != 0 && b % 2 != 0) for (int i = 0; i < NC; i++) cl[i] = i % 4 == 1 ? H * 0.32 : 0; C(() => bars(b, fl, cl, T, 0.14, 0.3)); }
		var VAL = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12, 11, 10, 9, 8, 7 };
		for (int k = 0; k < 16; k++) { double b = 20 + k * 0.5; int c = VAL[k]; var fl = Z(); var cl = Z(); for (int i = 0; i < NC; i++) { fl[i] = min(H, H * 0.14 + Math.Abs(i - c) * H * (easy ? 0.1 : 0.16)); if (Math.Abs(i - c) > (easy ? 2 : 3)) cl[i] = H * (easy ? 0.42 : 0.34); } C(() => bars(b, fl, cl, T, 0.1, 0.22), k % 2 == 0); }
		for (int b = 28; b < 34; b++) { C(() => safeHit(b, SAFE1[b - 4], 2, T, H * 0.2, 0.45, b % 2 == 0), b % 2 == 0); if (DF.d1 != 0 && b < 33) { var cl = Z(); int s = SAFE1[b - 4]; for (int i = 0; i < NC; i++) cl[i] = (i >= s - 1 && i < s + 3) ? H * 0.3 : 0; bars(b + 0.5, Z(), cl, T, 0.08, 0.25); } }
		for (int b = 34; b < 42; b++) { var fl = Z(); var w = b % 2 != 0 ? new[] { 6, 7, 8, 9 } : new[] { 5, 10, 4, 11 }; foreach (var i in w) fl[i] = H * (0.55 + 0.3 * F8eq(b, 0)); int gq = (b - 34) % 4; for (int i = gq * 4; i < gq * 4 + 4; i++) fl[i] = H; C(() => bars(b, fl, Z(), T, 0.1, 0.3)); }
		if (DF.d1 != 0) foreach (var b in new double[] { 36.5, 38.5, 40.5 }) { var fl = Z(); foreach (var i in new[] { 0, 1, 14, 15 }) fl[i] = H * 0.8; bars(b, fl, Z(), T, 0.1, 0.3); }
		(double, double) GZ(int i, double k, double g2) { double q = (((i + k) % 8) + 8) % 8, tri = q < 4 ? q / 4 : (8 - q) / 4; double c = H * (0.22 + 0.56 * tri); return (max(0, H - c - g2 / 2), max(0, c - g2 / 2)); }
		void zig(double hb, double k, F8Tier T, double g2 = 175) { g2 *= DF.gp; var fl = Z(); var cl = Z(); for (int i = 0; i < NC; i++) { var (a, c) = GZ(i, k, g2); fl[i] = a; cl[i] = c; } bars(hb, fl, cl, T, 0.14, 0.3); }
		for (int b = 42; b < 51; b++) C(() => zig(b, b - 42, T));
		for (double b = 51; b < 59; b += 0.5) C(() => zig(b, (b - 42) * 2 - 9, T, 185), b % 1 == 0);
		word(51, "RHYTHM", CX, H * 0.5, 70, 1.6, "rgba(255,255,255,0.45)");
		C(() => ev(wB(59, T.wn), () => { double wb = wB(59, T.wn); hazards.Add(F8Slab(H * 0.42, B(wb), B(59), B(65.5))); }));
		var CL2 = new int[][] { new[] { 2, 3, 12, 13 }, new[] { 5, 6, 9, 10 }, new[] { 0, 1, 7, 8, 14, 15 }, new[] { 3, 4, 11, 12 }, new[] { 6, 7, 8, 9 }, new[] { 1, 2, 13, 14 } };
		for (int b = 60; b < 66; b++) { var cl = Z(); foreach (var i in CL2[b - 60]) cl[i] = H * 0.58 + 10; for (int i = 0; i < NC; i++) if (cl[i] == 0 || double.IsNaN(cl[i])) cl[i] = DF.d1 != 0 ? H * 0.22 : 0; C(() => bars(b, Z(), cl, T, 0.14, 0.3)); }
		var CITY = new int[] { 2, 6, 10, 13 };
		for (int k = 0; k < CITY.Length; k++) { int s = CITY[k]; C(() => safeHit(66 + k * 2, s, 2, T, H * 0.15, 0.85, true)); if (DF.d1 != 0) safeHit(67 + k * 2, k % 2 != 0 ? s - 2 : s + 1, 3, T, H * 0.2, 0.6); }
		Func<double, double> wph = b => b < 90 ? (b - 74) / 8 : 2 - (b - 90) / 8;
		Func<double, double, double> yc = (i, t) => { double b = BT(t); return CY + (easy ? 140 : 190) * JS.sin(TAU * (i / NC * 0.75 - wph(b))); };
		Func<double, double> gw = t => { double b = BT(t); return (210 - (b > 106 ? 40 * (b - 106) / 4 : 0)) * DF.gp * (easy ? 1.2 : 1); };
		C(() => ev(72, () => hazards.Add(F8Wave(yc, gw, B(72), B(74), B(110)))));
		void slam(double hb, int s, F8Tier T) { var fl = Z(); var cl = Z(); double t = B(hb); for (int i = 0; i < NC; i++) { if (i >= s && i < s + 3) continue; double c = yc(i, t), hg = gw(t) / 2; fl[i] = H - c + hg + 4; cl[i] = 0; } bars(hb, fl, cl, new F8Tier { sp = T.sp, wn = T.wn + 1.5, @base = T.@base }, 0.16, 0.3); }
		foreach (var (b, s) in new[] { (78, 7), (82, 4), (86, 7), (90, 10), (94, 13), (98, 10), (102, 7), (106, 4) }) C(() => slam(b, s, T));
		word(82, "DANCE THE NIGHT AWAY", CX, H * 0.12, 46, 3, "rgba(255,255,255,0.45)"); word(98, "DANCE THE NIGHT AWAY", CX, H * 0.88, 46, 3, "rgba(255,255,255,0.45)");
		if (DF.d1 != 0) for (int b = 76; b < 108; b += 2) if (!new[] { 78, 82, 86, 90, 94, 98, 102, 106 }.Contains(b)) { var fl = Z(); int i0 = (b * 5) % NC; foreach (var i in new[] { i0, (i0 + 1) % NC }) fl[i] = H; bars(b, fl, Z(), T, 0.1, 0.3); }
		Func<double, double> sqg = t => { double b = BT(t); return clamp(300 - 170 * (b - 110) / 11, 130, 300) * DF.gp * (easy ? 1.2 : 1); };
		C(() => ev(109, () => hazards.Add(F8Wave((i, t) => CY, sqg, B(109), B(110), B(122)))));
		for (int b = 110; b < 122; b++) { var fl = Z(); for (int j = 0; j < 4; j++) fl[((b - 110) * 4 + j) % NC] = H; C(() => bars(b, fl, Z(), T, 0.12, 0.3)); }
		if (DF.d1 != 0) for (double b = 116; b < 121.5; b += 0.5) if (b % 1 != 0) { var fl = Z(); int i = i32(b * 7) % NC; fl[i] = H; bars(b, fl, Z(), T, 0.08, 0.25); }
		C(() => ev(122, () => { for (int i = 0; i < NC; i++) { double x = (i + 0.5) * F8CW; hazards.Add(Shard(x, CY - 90, (i - 7.5) * 12, -T.sp * 0.9, 9)); hazards.Add(Shard(x, CY + 90, (i - 7.5) * 12, T.sp * 0.9, 9)); } burst(CX, CY, "#fff", 30, 500); }));
		fx(122, 0.7, 26);

		/* ---------- phase 2: the crowd ---------- */
		T = TI[1];
		evk(120, () => hazards.Add(F8CrowdDecor(B(120), B(188)))); evk(121, () => hazards.Add(F8Spots(B(121), B(187))));
		word(122, "LA LA LA", CX, H * 0.5, 90, 2, "rgba(255,255,255,0.55)");
		var SJ = new (double b, double sd, double gx)[] { (124, -1, 0.75), (128, -1, 0.1), (135, 1, 0.25), (137, -1, 0.85), (139, 1, 0.42), (144, -1, 0.08), (146, 1, 0.45), (152, -1, 0.8), (155, 1, 0.45) };
		void ola(double hb, double side, double dir, double h, F8Tier T, double w = 170) { if (!easy && SJ.Any(q => q.sd == -side && Math.Abs(hb - q.b) <= 1.01)) return; double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(F8Ola(side, dir, h, w, T.sp, B(wb), B(hb)))); }
		var LAMB = new double[] { 0.55, 0.62, 0.7, 0.62, 0.48, 0.4, 0.48, 0.58 };
		for (int b = 122; b < 130; b++) C(() => ola(b, 1, 1, H * LAMB[(b - 122) % 8], T), b % (easy ? 4 : 2) == 0);
		for (int b = 122; b < 130; b += 2) if (DF.d2 != 0) ola(b + 1, -1, -1, H * 0.24, T, 140);
		for (int b = 130; b < 138; b++) if (b <= 130 || b >= 134) C(() => ola(b, -1, -1, H * LAMB[(b - 130 + 3) % 8], T), b % (easy ? 4 : 2) == 0);
		for (int b = 130; b < 138; b += 2) if (DF.d2 != 0 && b != 132) ola(b + 0.5, 1, 1, H * 0.24, T, 140);
		void jump(double hb, double gx, F8Tier T, double hold = 0.7, double sd = 0, double rh = H / 2 + 8) { double wb = wB(hb, T.wn + 0.5); ev(wb, () => hazards.Add(F8Jump(gx, 170 * DF.gp * (easy && sd != 0 ? 1.25 : 1), B(wb), B(hb), B(hb + hold), sd, rh))); }
		foreach (var (b, sd, gx) in SJ) C(() => jump(b, W * gx, T, 0.5, sd, H * 0.62));
		C(() => jump(132, W * 0.58, T)); word(132, "TONIGHT", CX, H * 0.18, 60, 1.6, "rgba(255,255,255,0.5)");
		for (int b = 138; b < 146; b++) { C(() => ola(b, 1, 1, H * 0.5 * LAMB[(b - 138) % 8] / 0.7, T), b % (easy ? 4 : 2) == 0); ola(b + 0.5, -1, -1, H * 0.5 * LAMB[(b - 138 + 4) % 8] / 0.7, T); }
		for (int b = 146; b < 154; b++) if (b != 148 && b != 149) { C(() => ola(b, 1, -1, H * LAMB[(b - 146 + 2) % 8], T), b % (easy ? 4 : 2) == 0); if (DF.d2 != 0 && b % 2 != 0) ola(b + 0.5, -1, 1, H * 0.26, T, 140); }
		C(() => jump(148, W * 0.3, T)); word(148, "TONIGHT", CX, H * 0.82, 60, 1.6, "rgba(255,255,255,0.5)");
		void stick(double hb, double[] xs, F8Tier T) => ev(hb, () => { foreach (var x in xs) hazards.Add(F8Stick(x, -T.sp * 0.8, 5, 84, B(hb))); });
		foreach (var (b, xs) in new (double, double[])[] { (124, new double[] { 200, 1080 }), (128, new double[] { 640 }), (136, new double[] { 380, 900 }), (152, new double[] { 260, 1020 }) }) stick(b, xs, T);
		for (int b = 154; b < 162; b++) { C(() => ola(b, 1, b % 2 != 0 ? -1 : 1, H * (0.3 + 0.28 * ((b * 3) % 5) / 4.0), T, 150), b % (easy ? 4 : 2) == 0); if (DF.d2 != 0 && b % 4 == 2) ola(b + 0.5, -1, b % 2 != 0 ? 1 : -1, H * 0.22, T, 140); }
		for (int b = 162; b < 170; b++) { var xs = new List<double>(); for (int j = 0; j < 3; j++) xs.Add(b % 2 != 0 ? W - 110 - ((b - 162) * 90 + j * 380) % (W - 220) : 110 + ((b - 162) * 90 + j * 380) % (W - 220)); stick(b, xs.ToArray(), T); if (b % 2 == 0) ola(b + 0.5, -1, b % 4 != 0 ? 1 : -1, H * 0.24, T, 140); }
		for (int b = 170; b < 178; b += 2) { C(() => { ola(b, 1, 1, H * 0.5, T, 170); ola(b, 1, -1, H * 0.5, T, 170); }); if (DF.d2 != 0) ola(b + 1, -1, b % 4 != 0 ? 1 : -1, H * 0.3, T, 150); }
		for (int b = 178; b < 184; b++) { C(() => ola(b, 1, b % 2 != 0 ? -1 : 1, H * (0.35 + 0.25 * ((b * 7) % 5) / 4.0), T, 150), b % (easy ? 4 : 2) == 0); if (DF.d2 != 0 && b % 2 != 0) ola(b + 0.5, -1, b % 4 == 1 ? 1 : -1, H * 0.24, T, 140); }
		void clap(double hb, F8Tier T, double hw = 56, double hh = 220) { double wb = wB(hb, T.wn); ev(wb, () => { double x = clamp(PXp(), hw + 10, W - hw - 10), y = clamp(PYp(), hh / 2 + 10, H - hh / 2 - 10); hazards.Add(F8Clap(x, y, hw, hh * DF.gp, B(wb), B(hb), B(hb + 0.2))); }); }
		foreach (var b in new double[] { 153, 157, 161, 165, 169, 173, 177, 181 }) C(() => clap(b, T));
		if (DF.d2 != 0) foreach (var b in new double[] { 159, 167, 175 }) clap(b, T);
		C(() => clap(183, T));
		foreach (var (b, xs) in new (double, double[])[] { (156, new double[] { 160, 640, 1120 }), (172, new double[] { 400, 880 }), (180, new double[] { 160, 640, 1120 }) }) stick(b, xs, T);
		word(155, "ON THE FLOOR", CX, H * 0.15, 54, 2, "rgba(255,255,255,0.4)");
		C(() => jump(184, W * 0.62, T, 0.6));
		C(() => jump(186, W * 0.5, new F8Tier { sp = T.sp, wn = T.wn + 0.05, @base = T.@base }, 0.5)); fx(186, 0.4, 14);

		/* ---------- phase 3: the disco ball ---------- */
		T = TI[2];
		var FRZ = new[] { (234.0, 235.5), (250.0, 251.5) }.Select(q => (B(q.Item1), B(q.Item2))).ToArray();
		Func<double, double> mt = t => { double s = t; foreach (var (a, b) in FRZ) { if (t > b) s -= b - a; else if (t > a) s -= t - a; } return s; };
		Func<double, double> sw = t => { foreach (var (a, b) in FRZ) if (t > a && t < b + 0.25) return 1 + 0.7 * min(1, (t - a) / 0.15) * min(1, (b + 0.25 - t) / 0.25); return 1; };
		var SPIN = new (double, double)[] { (186, 1.2), (187, 0), (190, 1.4), (195, -1.6), (218, -2.2), (234, 1.8), (262, 3), (268, 6) };
		Func<double, double> ang = t => { double a = 0; for (int i = 0; i < SPIN.Length; i++) { double s = B(SPIN[i].Item1), e = i + 1 < SPIN.Length ? B(SPIN[i + 1].Item1) : 1e9; if (t <= s) break; a += SPIN[i].Item2 * (min(t, e) - s); } return a; };
		Func<double, double> sdir = b => { double d = 1; foreach (var (s, r) in SPIN) if (b >= s && r != 0) d = sign(r); return d; };
		Func<double, (double, double)> bpos = t => { double b = BT(t), k = clamp((b - 186) / 2, 0, 1), e = 1 - JS.pow(1 - k, 3), d = clamp((b - 262) / 6, 0, 1), dd = d * d * (3 - 2 * d); return (CX + JS.sin(TAU * (b - 190) / 16) * 140 * clamp((b - 190) / 2, 0, 1) * (1 - dd), -120 + (250) * e + (H - 130 - 130) * dd); };
		evk(185, () => hazards.Add(F8DiscoBg(ang, B(185.5), B(274)))); evk(185, () => hazards.Add(F8Ball(bpos, ang, B(185.5), B(270), B(274))));
		var GC = new Dictionary<double, double>();
		void row(double hb, double y0, double vy, F8Tier T, F8RowO o = null)
		{
			o ??= new F8RowO();
			double sp = F8or(o.sp, 58), r = F8or(o.r, 11), n = F8or(o.n, 13), m = max(1, round((F8or(o.m, 3) + (easy ? 1 : 0)) * DF.gp)), v = F8or(o.v, 150) * sdir(hb) * (T.sp / T.@base), wb = wB(hb, F8or(o.wn, T.wn)), P = sp * n;
			double ph;
			if (o.ph == null) { double kk = sign(vy), c = GC[kk] = (GC.TryGetValue(kk, out var gv) ? F8or(gv, 0) : 0) + (o.same ? 0 : 1), tm = vy != 0 ? B(hb) + (CY - y0) / (vy * (T.sp / T.@base)) : B(hb); ph = ((c * (easy ? 0.618034 : m / n + 0.07)) % 1) * P - v * (tm - B(wb)); }
			else ph = o.ph.Value;
			ev(wb, () => hazards.Add(F8Row(y0, vy * (T.sp / T.@base), sp, r, n, m, ph, v, B(wb), B(hb), B(hb + F8or(o.life, 16)), mt, sw)));
		}
		void fan(double hb, double a0, int n, double spr, F8Tier T) { ev(hb, () => { var (x, y) = bpos(B(hb)); for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2.0) * spr; hazards.Add(Bullet(x + JS.cos(a) * 70, y + JS.sin(a) * 70, JS.cos(a) * T.sp * 0.75, JS.sin(a) * T.sp * 0.75, 10)); } flash = max(flash, 0.25); }); }
		C(() => { row(187, H + 20, -260, T, new F8RowO { v = 160 }); fan(187, Math.PI / 2, 9, 0.3, T); row(188, -20, 230, T, new F8RowO { v = 150 }); row(189, H + 20, -260, T, new F8RowO { v = 160 }); }); row(188, H * 0.8, 0, T, new F8RowO { life = 2, v = 1 });
		for (int b = 190; b < 202; b += 2) C(() => row(b, -20, 230, T));
		if (DF.d3 != 0) for (int b = easy ? 191 : 193; b < 202; b += easy ? 2 : 4) C(() => row(b, H + 20, -230, T, new F8RowO { n = 11, m = 4, v = 100 }));
		for (int b = 202; b < 210; b += 2) C(() => { row(b, -20, 230, T, new F8RowO { v = 160 }); row(b, H + 20, -230, T, new F8RowO { v = 160 }); });
		var SR = new double[][] { new[] { 0.25, 0.75 }, new[] { 0.5 }, new[] { 0.15, 0.62 }, new[] { 0.38, 0.85 } };
		for (int k = 0; k < SR.Length; k++) { foreach (var y in SR[k]) row(210 + k * 2, H * y, 0, T, new F8RowO { v = k % 2 != 0 ? -260 : 260, life = 2.6, n = 12, m = 4 }); C(() => row(211 + k * 2, k % 2 != 0 ? H + 20 : -20, k % 2 != 0 ? -200 : 200, T, new F8RowO { v = 140, m = 3 })); }
		foreach (var (b, a0) in new[] { (218.0, Math.PI * 0.75), (219.0, Math.PI * 0.25), (220.0, Math.PI * 0.6), (221.0, Math.PI * 0.4) }) C(() => fan(b, a0, DF.d3 != 0 ? 11 : 9, 0.16, T), b % 2 == 0);
		C(() => { row(219, -20, 230, T, new F8RowO { v = 150 }); row(221, H + 20, -230, T, new F8RowO { v = 150 }); });
		for (int b = 222; b < 234; b++) C(() => row(b, b % 2 != 0 ? H + 20 : -20, b % 2 != 0 ? -250 : 250, T, new F8RowO { v = 180 }), b % 2 == 0);
		foreach (var b in new double[] { 234, 250 }) { fx(b, 0.35, 10); word(b, "DANCE THE NIGHT AWAY", CX, H * 0.5, 50, 2, "rgba(255,255,255,0.45)"); }
		for (int b = 236; b < 250; b += 2) C(() => row(b, b % 4 != 0 ? H + 20 : -20, b % 4 != 0 ? -220 : 220, T, new F8RowO { v = 170 }));
		if (DF.d3 != 0) for (int b = 237; b < 250; b += 2) C(() => row(b, b % 4 == 1 ? -20 : H + 20, b % 4 == 1 ? 220 : -220, T, new F8RowO { v = 120, n = 11 }), b % 4 == 1);
		for (int b = 252; b < 262; b++) C(() => row(b, b % 2 != 0 ? H + 20 : -20, b % 2 != 0 ? -260 : 260, T, new F8RowO { v = 190 }), b % 2 == 0);
		foreach (var (b, a0) in new[] { (256.0, Math.PI * 0.5), (260.0, Math.PI * 0.5) }) C(() => fan(b, a0, DF.d3 != 0 ? 13 : 11, 0.2, T));
		for (int b = 260; b < 271; b++) C(() => row(b, H + 20, -360, T, new F8RowO { v = 220, n = 11, m = 3, wn = 1.2, same = !easy && b % 2 == 1 }));
		fx(270, 0.2, 8);
		fx(274, 0.9, 26);
		// in Easy: only the covering attacks stay, the extra ones go
		double eg = 99;
		for (int k = 0; k < GQ.Length; k++)
		{
			var q = GQ[k]; sort(q, (a, b) => a.b - b.b);
			if (easy) { double lk = -1e9; var q1 = new List<F8Q>(); foreach (var o in q) { if (o.c) { q1.Add(o); continue; } if (o.b == lk || o.b - lk >= eg - 1e-6) { lk = o.b; q1.Add(o); } } q = q1; }
			if (q.Count > 0) { var qq = q; E.Add(new Ev { b = new double[] { 0, 120, 184 }[k], f = () => hazards.Add(F8Gen(qq)) }); }
		}
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
