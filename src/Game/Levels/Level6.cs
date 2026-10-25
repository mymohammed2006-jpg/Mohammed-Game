using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 2227-2493: the shapes of level 6 (H3ЯД, a virus breaking into the game) and buildLevel6
// all names start with Hx so they do not clash with other levels
public static partial class G
{
	// line 6514: the AFK maker
	static readonly int _afkL6 = AfkReg("buildLevel6", (x, y, b, v, w) =>
	{
		if (v >= 2) { bool vt = v < 4, fw = v % 2 == 0; return One(HxScan(vt, fw ? 0 : (vt ? W : H), fw ? (vt ? W : H) : 0, B(b - w), B(b), B(b) + 2.4, vt ? y : x, 150)); }
		return One(HxTiles(new List<(int i, int j)> { ((int)Math.Floor(x / HX_CW), (int)Math.Floor(y / HX_CH)) }, B(b - w), B(b), B(b + 0.6)));
	});

	// a flying pink pixel (a small spinning square), hurts
	public static Hz HxPix(double x, double y, double vx, double vy, double r = 10)
	{
		var b = Bullet(x, y, vx, vy, r); double rot = rnd() * TAU; double rs = (rnd() < 0.5 ? -1 : 1) * (3 + rnd() * 3);
		b.draw = t =>
		{
			rot += rs / 60; double s = b.r * (1 + 0.15 * pulse); g.save(); g.translate(b.x, b.y); g.fillStyle = "rgba(255,42,109,0.22)"; g.fillRect(-s * 1.6, -s * 1.6, s * 3.2, s * 3.2); g.rotate(rot);
			g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; g.fillRect(-s, -s, 2 * s, 2 * s); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.35)"; g.fillRect(-s * 0.35, -s * 0.35, s * 0.7, s * 0.7); g.restore();
		};
		return b;
	}

	public static Hz HxBlock(double x, double y, double w, double h, double tw, double ta, double te)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.2) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), j = k > 0.55 ? JS.sin(t * 70 + x) * 4 * k : 0; g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.fillRect(x - w / 2, y - h / 2, w, h);
				g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); g.strokeRect(x - w / 2 + j, y - h / 2, w, h); g.setLineDash(new double[0]);
				double c = 12 * k; g.fillStyle = "rgba(255,42,109,0.9)"; foreach (var (sx, sy) in new[] { (-1.0, -1.0), (1.0, -1.0), (-1.0, 1.0), (1.0, 1.0) }) g.fillRect(x + sx * w / 2 - (sx > 0 ? c : 0), y + sy * h / 2 - (sy > 0 ? 3 : 0), c, 3); return;
			}
			if (t < te)
			{
				double e = t - ta; g.save(); g.shadowColor = PINK; g.shadowBlur = 20;
				if (e < 0.16) { int n = 4; double sh = h / n; for (int i = 0; i < n; i++) { double o = JS.sin(i * 2.7 + x) * 14 * (1 - e / 0.16); g.fillStyle = e < 0.07 ? PINKW : PINK; g.fillRect(x - w / 2 + o, y - h / 2 + i * sh, w, sh + 0.5); } }
				else { g.fillStyle = PINK; g.fillRect(x - w / 2, y - h / 2, w, h); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.28)"; double q = Math.Max(8, Math.Min(w, h) / 5); for (double i = 0; i < w / q; i++) for (double k2 = 0; k2 < h / q; k2++) if ((i * 7 + k2 * 3 + Math.Floor(x)) % 5 == 0) g.fillRect(x - w / 2 + i * q, y - h / 2 + k2 * q, q, q); }
				g.restore(); return;
			}
			{
				double k = 1 - (t - te) / 0.2; if (k <= 0) return; g.globalAlpha = k; g.fillStyle = PINK; double q = Math.Max(8, Math.Min(w, h) / 5); for (double i = 0; i < w / q; i++) for (double k2 = 0; k2 < h / q; k2++) if ((i + k2) % 2 == 0) g.fillRect(x - w / 2 + i * q + q * (1 - k) / 2, y - h / 2 + k2 * q + q * (1 - k) / 2, q * k, q * k); g.globalAlpha = 1;
			}
		};
		hz.hit = (p, t) => t >= ta && t < te && Math.Abs(p.x - x) < w / 2 + p.r - 1 && Math.Abs(p.y - y) < h / 2 + p.r - 1;
		return hz;
	}

	// a scan line: moves across the screen from one place to another, with an opening to pass through (gs=0 no opening)
	public static Hz HxScan(bool vert, double p0, double p1, double tw, double ta, double te, double gc, double gs, double th = 16)
	{
		Func<double, double> pos = t => p0 + (p1 - p0) * clamp((t - ta) / (te - ta), 0, 1); double ext = vert ? H : W;
		void seg(Action<double, double> f) { if (gs <= 0) { f(0, ext); return; } f(0, gc - gs / 2); f(gc + gs / 2, ext); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double P = pos(t), dir = sign(p1 - p0); if (dir == 0 || double.IsNaN(dir)) dir = 1;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.6 * k)})"; seg((a, b) => { if (vert) g.fillRect(P - 1.5, a, 3, b - a); else g.fillRect(a, P - 1.5, b - a, 3); });
				for (int i = 0; i < 5; i++) { double c = (i + 0.5) * ext / 5; if (gs > 0 && Math.Abs(c - gc) < gs / 2 + 20) continue; double o = dir * (10 + 12 * k); g.beginPath(); if (vert) { g.moveTo(P + o + dir * 8, c); g.lineTo(P + o - dir * 4, c - 9); g.lineTo(P + o - dir * 4, c + 9); } else { g.moveTo(c, P + o + dir * 8); g.lineTo(c - 9, P + o - dir * 4); g.lineTo(c + 9, P + o - dir * 4); } g.fill(); }
				if (gs > 0) { g.strokeStyle = $"rgba(255,255,255,{str(0.25 + 0.4 * k)})"; g.lineWidth = 2; if (vert) { g.strokeRect(P - 14, gc - gs / 2, 28, gs); } else { g.strokeRect(gc - gs / 2, P - 14, gs, 28); } }
				return;
			}
			if (t > te) { double k = 1 - (t - te) / 0.2; g.globalAlpha = Math.Max(0, k); }
			for (int i = 1; i <= 6; i++) { g.fillStyle = $"rgba(255,42,109,{str(0.09 * (1 - i / 7.0))})"; double q = P - dir * i * 14; seg((a, b) => { if (vert) g.fillRect(q - 7, a, 14, b - a); else g.fillRect(a, q - 7, b - a, 14); }); }
			g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; double ww = th * (1 + 0.12 * pulse); seg((a, b) => { if (vert) g.fillRect(P - ww / 2, a, ww, b - a); else g.fillRect(a, P - ww / 2, b - a, ww); }); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double P = pos(t), d = vert ? p.x - P : p.y - P, c = vert ? p.y : p.x; if (Math.Abs(d) > th / 2 + p.r - 1) return false; return !(gs > 0 && Math.Abs(c - gc) < gs / 2 - p.r + 1);
		};
		return h;
	}

	// an error window: pops up, hits, and explodes into code characters
	public static Hz HxErr(double x, double y, double w, double h, double tw, double ta, int n, double sp, string lab = "")
	{
		const double hold = 0.22; bool done = false;
		var hz = new Hz();
		hz.update = (t, dt) =>
		{
			if (!done && t >= ta) { done = true; double a0 = rnd() * TAU; for (int i = 0; i < n; i++) { double a = a0 + (double)i * TAU / n; hazards.Add(HxPix(x + JS.cos(a) * Math.Min(w, h) * 0.3, y + JS.sin(a) * Math.Min(w, h) * 0.3, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } burst(x, y, PINK, 16, 320); burst(x, y, "#fff", 8, 240); shake = Math.Max(shake, 8); }
			if (t > ta + hold + 0.25) hz.dead = true;
		};
		hz.draw = t =>
		{
			bool hasLab = lab != "";
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), s = 0.55 + 0.45 * Math.Min(1, k * 1.6), ww = w * s, hh = h * s; g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; g.fillRect(x - ww / 2, y - hh / 2, ww, hh);
				g.strokeStyle = $"rgba(255,42,109,{str(0.45 + 0.5 * k)})"; g.lineWidth = 2; g.strokeRect(x - ww / 2, y - hh / 2, ww, hh); g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.4 * k)})"; g.fillRect(x - ww / 2, y - hh / 2, ww, 22 * s);
				txt(hasLab ? lab : "ERROR", x, y + 8 * s, round((hasLab ? Math.Min(hh * 0.62, 110) : 24) * s), $"rgba(255,42,109,{str(0.3 + 0.6 * k)})", 800);
				g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * k)})"; g.font = $"800 {str(round(15 * s))}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText("x", x + ww / 2 - 12 * s, y - hh / 2 + 11 * s); return;
			}
			double e = t - ta; if (e > hold + 0.25) return; double al = e < hold ? 1 : 1 - (e - hold) / 0.25; g.globalAlpha = al; g.save(); g.shadowColor = PINK; g.shadowBlur = 26; g.fillStyle = e < 0.07 ? PINKW : PINK; double j = e < 0.15 ? JS.sin(e * 90) * 6 : 0; g.fillRect(x - w / 2 + j, y - h / 2, w, h); g.restore();
			g.fillStyle = "rgba(0,0,0,0.35)"; g.fillRect(x - w / 2, y - h / 2, w, 22); txt(hasLab ? lab : "ERROR", x, y + 8, hasLab ? Math.Min(h * 0.62, 110) : 24, "rgba(0,0,0,0.55)", 800); g.globalAlpha = 1;
		};
		hz.hit = (p, t) => t >= ta && t < ta + hold && Math.Abs(p.x - x) < w / 2 + p.r - 1 && Math.Abs(p.y - y) < h / 2 + p.r - 1;
		return hz;
	}

	// a bass pulse: a square that spins and shrinks, then explodes into a ring of characters
	public static Hz HxPulse(double x, double y, double tw, double ta, int n, double sp)
	{
		bool done = false;
		var h = new Hz();
		h.update = (t, dt) => { if (!done && t >= ta) { done = true; double a0 = rnd() * TAU; for (int i = 0; i < n; i++) { double a = a0 + (double)i * TAU / n; hazards.Add(HxPix(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } burst(x, y, PINK, 14, 300); shake = Math.Max(shake, 9); } if (t > ta + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1), s = 70 - 36 * k; g.save(); g.translate(x, y); g.rotate(k * Math.PI); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; g.strokeRect(-s, -s, 2 * s, 2 * s); g.restore(); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.25 * k)})"; g.fillRect(x - 30, y - 30, 60, 60); return; }
			{ double k = (t - ta) / 0.25; g.globalAlpha = Math.Max(0, 1 - k); g.fillStyle = k < 0.3 ? PINKW : PINK; double s = 34 + 40 * k; g.fillRect(x - s, y - s, 2 * s, 2 * s); g.globalAlpha = 1; }
		};
		h.hit = (p, t) => t >= ta && t < ta + 0.12 && Math.Abs(p.x - x) < 36 + p.r && Math.Abs(p.y - y) < 36 + p.r;
		return h;
	}

	// the floor grid: 8 columns x 5 rows. Broken tiles warn and then hit
	public const int HX_C = 8, HX_R = 5;
	public const double HX_CW = W / HX_C, HX_CH = H / HX_R;
	public static Hz HxTiles(List<(int i, int j)> cells, double tw, double ta, double te)
	{
		const double pad = 4;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			foreach (var (i, j) in cells)
			{
				double x = i * HX_CW + pad, y = j * HX_CH + pad, w = HX_CW - 2 * pad, hh = HX_CH - 2 * pad;
				if (t < ta)
				{
					double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.17 * k)})"; g.fillRect(x, y, w, hh); g.fillStyle = $"rgba(255,42,109,{str(0.45 + 0.5 * k)})"; double c = 10 + 16 * k;
					g.fillRect(x, y, c, 3); g.fillRect(x, y, 3, c); g.fillRect(x + w - c, y, c, 3); g.fillRect(x + w - 3, y, 3, c); g.fillRect(x, y + hh - 3, c, 3); g.fillRect(x, y + hh - c, 3, c); g.fillRect(x + w - c, y + hh - 3, c, 3); g.fillRect(x + w - 3, y + hh - c, 3, c); continue;
				}
				if (t < te)
				{
					double e = t - ta; g.save(); g.shadowColor = PINK; g.shadowBlur = 18;
					if (e < 0.14) { int n = 4; double sh = hh / n; for (int q = 0; q < n; q++) { double o = JS.sin(q * 2.3 + i * 1.7 + j) * 12 * (1 - e / 0.14); g.fillStyle = e < 0.06 ? PINKW : PINK; g.fillRect(x + o, y + q * sh, w, sh + 0.5); } }
					else { g.fillStyle = PINK; g.fillRect(x, y, w, hh); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.25)"; double q = w / 8; for (int a = 0; a < 8; a++) for (int b2 = 0; b2 < 7; b2++) if ((a * 5 + b2 * 3 + i + j * 2) % 7 == 0) g.fillRect(x + a * q, y + b2 * q, q, q); }
					g.restore(); continue;
				}
				{
					double k = 1 - (t - te) / 0.2; if (k <= 0) continue; g.globalAlpha = k; g.fillStyle = PINK; double q = w / 6; for (int a = 0; a < 6; a++) for (int b2 = 0; b2 < 5; b2++) if ((a + b2) % 2 == 0) g.fillRect(x + a * q + q * (1 - k) / 2, y + b2 * q * 1.1 + q * (1 - k) / 2, q * k, q * k); g.globalAlpha = 1;
				}
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			foreach (var (i, j) in cells) { double x = i * HX_CW + pad, y = j * HX_CH + pad; if (p.x > x - p.r + 2 && p.x < x + HX_CW - 2 * pad + p.r - 2 && p.y > y - p.r + 2 && p.y < y + HX_CH - 2 * pad + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// decoration: the grid lines (do not hurt)
	public static Hz HxFloor(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 1, (t1 + 1 - t) / 1); if (a <= 0) return; g.globalAlpha = a; g.strokeStyle = GREY + str(0.1 + 0.1 * pulse) + ")"; g.lineWidth = 2; g.beginPath();
			for (int i = 1; i < HX_C; i++) { g.moveTo(i * HX_CW, 0); g.lineTo(i * HX_CW, H); }
			for (int j = 1; j < HX_R; j++) { g.moveTo(0, j * HX_CH); g.lineTo(W, j * HX_CH); }
			g.stroke(); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// a virus worm: pixel squares that follow the player (slower than them)
	public static Hz HxWorm(double x, double y, double tw, double t0, double t1, double sp, double turn, int n)
	{
		double a = JS.atan2(H / 2 - y, W / 2 - x), hx = x, hy = y; var hist = new List<(double, double)>(); const double S = 20; const int lag = 4;
		List<(double, double)> segs() { var o = new List<(double, double)>(); for (int i = 0; i < n; i++) { int ix = Math.Min(hist.Count - 1, i * lag); if (ix >= 0) o.Add(hist[ix]); } return o; }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > t1 + 0.5) { h.dead = true; return; }
			if (t < t0 || t >= t1) return;
			if (player != null) { double want = JS.atan2(player.y - hy, player.x - hx); double d = want - a; d = JS.atan2(JS.sin(d), JS.cos(d)); a += clamp(d, -turn * dt, turn * dt); }
			hx += JS.cos(a) * sp * dt; hy += JS.sin(a) * sp * dt; hist.Insert(0, (hx, hy)); if (hist.Count > n * lag + 2) hist.RemoveAt(hist.Count - 1);
		};
		h.draw = t =>
		{
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1), cx = clamp(x, 24, W - 24), cy = clamp(y, 24, H - 24); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; double s = 16 + 10 * JS.sin(t * 14); g.strokeRect(cx - s, cy - s, 2 * s, 2 * s); txt("!", cx, cy + 2, 26, $"rgba(255,42,109,{str(0.5 + 0.5 * k)})", 800); return; }
			double fade = t >= t1 ? 1 - (t - t1) / 0.5 : 1; if (fade <= 0) return; var S2 = segs(); g.globalAlpha = fade; g.save(); g.shadowColor = PINK; g.shadowBlur = 14;
			for (int i = S2.Count - 1; i >= 0; i--) { var (sx, sy) = S2[i]; double s = S * (1 - i / (n * 1.7)); g.fillStyle = i == 0 ? PINK : $"rgba(255,42,109,{str(0.95 - i * 0.04)})"; g.fillRect(sx - s / 2, sy - s / 2, s, s); }
			g.restore(); if (S2.Count != 0) { var (sx, sy) = S2[0]; g.fillStyle = "#000"; g.fillRect(sx - 6 + JS.cos(a) * 4, sy - 6 + JS.sin(a) * 4, 4, 4); g.fillRect(sx + 2 + JS.cos(a) * 4, sy - 6 + JS.sin(a) * 4, 4, 4); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < t0 + 0.05 || t >= t1) return false; var S2 = segs();
			for (int i = 0; i < S2.Count; i++) { double s = S * (1 - i / (n * 1.7)) / 2; if (Math.Abs(p.x - S2[i].Item1) < s + p.r - 2 && Math.Abs(p.y - S2[i].Item2) < s + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// a firewall: a brick wall moving from one side to the other, with an open gate in it
	public static Hz HxGate(bool vert, double gc, double gs, double tw, double ta, double sp, double dir, double th = 50)
	{
		double ext = vert ? W : H, st = dir > 0 ? -th : ext + th; Func<double, double> pos = t => st + dir * sp * (t - ta); double len = vert ? H : W;
		void seg(Action<double, double> f) { f(0, gc - gs / 2); f(gc + gs / 2, len); }
		var h = new Hz();
		h.update = (t, dt) => { double P = pos(t); if (t > ta && (P < -th * 2 || P > ext + th * 2)) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), e = dir > 0 ? 6 : ext - 6; g.fillStyle = $"rgba(255,42,109,{str(0.35 + 0.55 * k)})"; seg((a, b) => { if (vert) g.fillRect(e - 4, a, 8, b - a); else g.fillRect(a, e - 4, b - a, 8); });
				g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; double o = dir > 0 ? 20 : ext - 20; if (vert) g.strokeRect(o - 12, gc - gs / 2, 24, gs); else g.strokeRect(gc - gs / 2, o - 12, gs, 24); return;
			}
			double P = pos(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; seg((a, b) => { if (b <= a) return; if (vert) g.fillRect(P - th / 2, a, th, b - a); else g.fillRect(a, P - th / 2, b - a, th); }); g.restore();
			g.fillStyle = "rgba(0,0,0,0.32)"; const double bh = 26;
			seg((a, b) =>
			{
				int i = 0;
				for (double q = a; q < b; q += bh, i++)
				{
					double m = Math.Min(bh - 4, b - q); if (m <= 0) continue;
					if (vert) { g.fillRect(P - th / 2, q, th, 2); g.fillRect(P + (i % 2 != 0 ? -2 : th / 2 - 10) - th / 4, q, 2, m); } else { g.fillRect(q, P - th / 2, 2, th); g.fillRect(q, P + (i % 2 != 0 ? -2 : th / 2 - 10) - th / 4, m, 2); }
				}
			});
			g.strokeStyle = "rgba(255,255,255,0.55)"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); if (vert) { g.beginPath(); g.moveTo(P, gc - gs / 2); g.lineTo(P, gc + gs / 2); g.stroke(); } else { g.beginPath(); g.moveTo(gc - gs / 2, P); g.lineTo(gc + gs / 2, P); g.stroke(); }
			g.setLineDash(new double[0]);
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double P = pos(t), d = vert ? p.x - P : p.y - P, c = vert ? p.y : p.x; if (Math.Abs(d) > th / 2 + p.r - 1) return false; return Math.Abs(c - gc) > gs / 2 - p.r + 1;
		};
		return h;
	}

	// a loading circle: pieces turning around a point, the lit ones hurt
	public static Hz HxSpin(double x, double y, double R, double tw, double t0, double t1, int segs, int on, double w, double rot, double a0 = 0)
	{
		Func<double, double> ang = t => a0 + rot * (t - t0); const double gw = 0.07;
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; };
		h.draw = t =>
		{
			bool live = t >= t0 && t < t1; double a = t < t0 ? clamp((t - tw) / (t0 - tw), 0, 1) : t >= t1 ? clamp(1 - (t - t1) / 0.4, 0, 1) : 1, A = ang(Math.Max(t, t0)), st = TAU / segs;
			g.save(); g.globalAlpha = a; g.lineWidth = w; g.lineCap = "butt";
			for (int i = 0; i < segs; i++)
			{
				bool lit = i < on; double s0 = A + i * st + gw, s1 = A + (i + 1) * st - gw; g.beginPath(); g.arc(x, y, R, s0, s1);
				if (lit && live) { g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 16; } else if (lit) { g.strokeStyle = "rgba(255,42,109,0.35)"; g.shadowBlur = 0; } else { g.strokeStyle = GREY + "0.12)"; g.shadowBlur = 0; }
				g.stroke();
			}
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= t1) return false; double d = hypot(p.x - x, p.y - y); if (Math.Abs(d - R) > w / 2 + p.r - 1) return false; double q = JS.atan2(p.y - y, p.x - x) - ang(t); q = ((q % TAU) + TAU) % TAU; double st = TAU / segs; return q < on * st - gw + p.r / Math.Max(d, 1);
		};
		return h;
	}

	// beams turning from a point (for the boss)
	public static Hz HxBeams(double x, double y, int arms, double tw, double ta, double te, double a0, double rot, double w = 22)
	{
		Func<double, double> ang = t => a0 + rot * Math.Max(0, t - ta); const double L = 1600;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double A = ang(t); g.save(); g.translate(x, y);
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.12 * k)})";
				for (int i = 0; i < arms; i++) { g.save(); g.rotate(A + (double)i * TAU / arms); g.fillRect(0, -w / 2, L, w); g.fillStyle = $"rgba(255,42,109,{str(0.5 + 0.4 * k)})"; g.fillRect(0, -1.5, L, 3); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.12 * k)})"; g.restore(); }
				g.restore(); return;
			}
			{
				double k = t < te ? 1 : 1 - (t - te) / 0.3; g.globalAlpha = Math.Max(0, k); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = (t - ta) < 0.07 ? PINKW : PINK; double ww = w * (1 + 0.12 * pulse);
				for (int i = 0; i < arms; i++) { g.save(); g.rotate(A + (double)i * TAU / arms); g.fillRect(0, -ww / 2, L, ww); g.restore(); }
				g.restore(); g.globalAlpha = 1;
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double A = ang(t);
			for (int i = 0; i < arms; i++) { double a = A + (double)i * TAU / arms; if (obbHit(p.x, p.y, p.r - 1, x + JS.cos(a) * L / 2, y + JS.sin(a) * L / 2, L / 2, w / 2, a)) return true; }
			return false;
		};
		return h;
	}

	// decoration: a grey grid coming down slowly + screen lines (does not hurt)
	public static Hz HxGrid(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 1.5, (t1 - t) / 1.5); if (a <= 0) return; g.globalAlpha = a; g.strokeStyle = GREY + str(0.05 + 0.05 * pulse) + ")"; g.lineWidth = 1; const double s = 64; double o = (t * 40) % s; g.beginPath();
			for (double x = 0; x <= W; x += s) { g.moveTo(x, 0); g.lineTo(x, H); }
			for (double y = o - s; y <= H; y += s) { g.moveTo(0, y); g.lineTo(W, y); }
			g.stroke();
			g.fillStyle = GREY + "0.025)"; for (double y = 0; y < H; y += 4) g.fillRect(0, y, W, 1); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// decoration: a loading bar at the top (does not hurt)
	public static Hz HxLoader(double t0, double t1, string lab)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double k = clamp((t - t0) / (t1 - t0), 0, 1), a = min(1, (t - t0) / 0.8, (t1 + 0.6 - t) / 0.6); if (a <= 0) return; const double bw = 420; double bx = W / 2 - bw / 2, by = 34; g.globalAlpha = a * 0.85;
			g.strokeStyle = GREY + "0.5)"; g.lineWidth = 2; g.strokeRect(bx, by, bw, 14); g.fillStyle = GREY + "0.55)"; double n = Math.Floor(k * 30); for (int i = 0; i < n; i++) g.fillRect(bx + 3 + i * (bw - 6) / 30, by + 3, (bw - 6) / 30 - 3, 8);
			g.font = $"400 15px {TFONT}"; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = GREY + "0.6)"; g.fillText(lab, bx, by + 28); g.textAlign = "right"; g.fillText(str(Math.Floor(k * 100)) + "%", bx + bw, by + 28); g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// decoration: the screen tears with the drops (does not hurt)
	public static Hz HxTear(double t0, double dur)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t0 + dur) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = 1 - (t - t0) / dur; g.globalAlpha = k * 0.5;
			for (int i = 0; i < 9; i++) { double y = (i * 97 + Math.Floor(t * 30) * 53) % H, hh = 8 + (i * 13) % 30, o = JS.sin(i * 3.1 + t * 40) * 60 * k; g.fillStyle = i % 3 != 0 ? GREY + "0.25)" : "rgba(255,42,109,0.25)"; g.fillRect(o, y, W, hh); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => false;
		return h;
	}

	// the boss: the virus (a pixel skull). It vanishes and comes back somewhere else, throws code, laser eyes, and spins beams at the peak.
	// it charges before the music cuts, and explodes into pixels at 2:39
	static readonly string[] HX_SKULL = { "....#####....", "..#########..", ".###########.", ".###########.", "###..###..###", "###..###..###", "#############", ".#####.#####.", "..#########..", "..#.#.#.#.#..", "..#########..", "...#.#.#.#..." };
	class HxMv { public double tw, ta, tb, x, y; }
	public static Hz HxSkull(double ts, double tc, double tx, Tier T, double[][] moves)
	{
		const double PX = 11, SW = 13 * PX, SH = 12 * PX; double x = W / 2, y = -200, lastB = -1, eyeT = -9; bool ex = false;
		// moves: [[beat,x,y],...] each move: the ghost shows 2 beats before, it vanishes on the beat and comes back a quarter beat later
		var M = moves.Select(mv => new HxMv { tw = B(mv[0] - 2), ta = B(mv[0]), tb = B(mv[0] + 0.25), x = mv[1], y = mv[2] }).ToList();
		(double px, double py, bool vis, HxMv mv) at(double t)
		{
			double px = W / 2, py = -200; bool vis = true; HxMv mv = null; if (t < ts) { return (px, py, false, null); }
			double k = clamp((t - ts) / (2 * SPB), 0, 1); px = W / 2; py = -200 + (150 + 200) * (1 - JS.pow(1 - k, 3));
			foreach (var m in M) { if (t >= m.tb) { px = m.x; py = m.y; } else { if (t >= m.ta) vis = false; if (t >= m.tw) mv = m; break; } }
			return (px, py, vis, mv);
		}
		(double, double) mouth() => (x, y + SH * 0.3);
		void fan(int n, double spr, double sp) { if (player == null) return; var (mx, my) = mouth(); double a0 = JS.atan2(player.y - my, player.x - mx); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spr; hazards.Add(HxPix(mx + JS.cos(a) * 30, my + JS.sin(a) * 30, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } }
		void ring(int n, double sp, double a0) { for (int i = 0; i < n; i++) { double a = a0 + (double)i * TAU / n; hazards.Add(HxPix(x + JS.cos(a) * 60, y + JS.sin(a) * 60, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } }
		void eyes(double t)
		{
			if (player == null) return; eyeT = t; double ex0 = x, ey0 = y - SH * 0.1, a = JS.atan2(player.y - ey0, player.x - ex0); const double L = 2600; double c = JS.cos(a), s = JS.sin(a);
			foreach (var sd in new double[] { -1, 1 }) { double ox = ex0 - s * sd * 30, oy = ey0 + c * sd * 30; hazards.Add(Laser(new LaserO { x = ox + c * L / 2, y = oy + s * L / 2, ang = a, len = L, w = 20, tw = t, ta = t + SPB * T.wn, te = t + SPB * (T.wn + 0.6) })); }
		}
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= tx && !ex) { ex = true; for (int i = 0; i < 13; i++) for (int j = 0; j < 12; j++) if (HX_SKULL[j][i] == '#') burst(x - SW / 2 + i * PX, y - SH / 2 + j * PX, PINK, 2, 420); burst(x, y, "#fff", 50, 500); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; return; }
			if (t < ts) return; var S = at(t); x = S.px; y = S.py;
			if (t < ts + 2 * SPB || t >= tc) return;
			double b = Math.Floor(BT(t) + 1e-6); if (b == lastB) return; lastB = b; if (!S.vis) return;
			if (S.mv != null && t >= S.mv.ta - SPB * 0.6) return; // stops a bit before the move
			if (b < 316) { double o = (b - 268) % 16; if (o < 12) { if (o % 4 == 2) ring(12, T.sp * 0.75, b * 0.5); else if (o % 2 == 0) fan(5, 0.22, T.sp); else fan(3, 0.3, T.sp * 0.9); } else if (o == 14) ring(14, T.sp * 0.75, b * 0.4); }
			else if (b < 368) { double o = (b - 316) % 8; if (o == 0 || o == 4) eyes(t); else if (o == 2) ring(12, T.sp * 0.75, b * 0.4); else if (o == 6) fan(5, 0.2, T.sp); else if (o == 3 || o == 7) fan(3, 0.3, T.sp * 0.9); }
			else if (b < 380) { if (b % 2 == 0) ring(12, T.sp * 0.7, b * 0.37); }
			else { double o = (b - 380) % 8; if (o == 0 || o == 4) eyes(t); else if (o == 2 || o == 6) fan(7, 0.18, T.sp); else if (o == 7 || o == 3) ring(14, T.sp * 0.75, b * 0.4); }
		};
		h.draw = t =>
		{
			if (t < ts) return; var S = at(t); bool ch = t >= tc && t < tx; double jx = 0, jy = 0, s = 1; if (ch) { double k = (t - tc) / (tx - tc); s = 1 + 0.25 * k; jx = JS.sin(t * 90) * 6 * k; jy = JS.cos(t * 77) * 6 * k; }
			if (S.mv != null && t < S.mv.tb) { double k = clamp((t - S.mv.tw) / (S.mv.ta - S.mv.tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.strokeRect(S.mv.x - SW / 2, S.mv.y - SH / 2, SW, SH); g.setLineDash(new double[0]); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; g.fillRect(S.mv.x - SW / 2, S.mv.y - SH / 2, SW, SH); }
			if (!S.vis) return; bool solid = t >= ts + 2 * SPB; g.save(); g.translate(x + jx, y + jy); g.scale(s, s); g.globalAlpha = solid ? 1 : 0.45;
			bool gl = JS.sin(t * 23) > 0.93; g.shadowColor = PINK; g.shadowBlur = ch ? 44 : 26;
			for (int j = 0; j < 12; j++) { double o = gl && j % 3 == 1 ? 8 : 0; for (int i = 0; i < 13; i++) if (HX_SKULL[j][i] == '#') { g.fillStyle = ch && (i + j) % 2 != 0 ? PINKW : PINK; g.fillRect(-SW / 2 + i * PX + o, -SH / 2 + j * PX, PX - 1, PX - 1); } }
			g.shadowBlur = 0; double eb = t - eyeT < SPB * T.wn ? 0.5 + 0.5 * JS.sin(t * 30) : 0; if (eb > 0) { g.fillStyle = $"rgba(255,255,255,{str(eb)})"; g.fillRect(-SW / 2 + 3 * PX, -SH / 2 + 4 * PX, 2 * PX, 2 * PX); g.fillRect(-SW / 2 + 8 * PX, -SH / 2 + 4 * PX, 2 * PX, 2 * PX); }
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ts + 2 * SPB || t >= tc) return false; var S = at(t); if (!S.vis || (S.mv != null && t >= S.mv.ta)) return false; if (M.Any(m => t >= m.tb && t < m.tb + SPB * 0.6)) return false;
			return Math.Abs(p.x - x) < SW / 2 - 8 + p.r && Math.Abs(p.y - y) < SH / 2 - 8 + p.r;
		};
		return h;
	}

	/* level 6: H3ЯД
	   the song starts at 0:10. 160 bpm (a beat is 0.375 s). beat 0 = 0:10.56
	   phase 1 the grid (0:10 - 1:00): floor tiles break in shapes on the beat, the drop at 36 (0:24)
	   phase 2 the firewall (1:00 - 1:51, checkpoint 132): walls with gates + a worm, the pause at 196 (1:24), the maze at 204 (1:27)
	   phase 3 the boss (1:51 - 2:40, checkpoint 268): the skull throws pixels, lasers 316 (2:09), the peak 368 (2:28), explodes 396 (2:39), the end 399 */
	public static List<Ev> buildLevel6()
	{
		var E = new List<Ev>(); var R = mulberry32(606); Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		var TI = ezT(new[] { new Tier { sp = 300, wn = 1.7 }, new Tier { sp = 320, wn = 1.6 }, new Tier { sp = 340, wn = 1.5 } });
		double PXp() => player != null ? player.x : W / 2; double PYp() => player != null ? player.y : H / 2;
		double rx(double a = 110, double b = W - 110) => a + R() * (b - a); double ry(double a = 100, double b = H - 100) => a + R() * (b - a);
		(int, int) cellP() => ((int)clamp(Math.Floor(PXp() / HX_CW), 0, HX_C - 1), (int)clamp(Math.Floor(PYp() / HX_CH), 0, HX_R - 1));
		// the grid shapes
		var ALL = new List<(int i, int j)>(); for (int i = 0; i < HX_C; i++) for (int j = 0; j < HX_R; j++) ALL.Add((i, j));
		List<(int i, int j)> Prow(int[] js) => ALL.Where(c => js.Contains(c.j)).ToList();
		List<(int i, int j)> Pcol(int[] iss) => ALL.Where(c => iss.Contains(c.i)).ToList();
		List<(int i, int j)> Pchecker(int par) => ALL.Where(c => (c.i + c.j) % 2 == par).ToList();
		List<(int i, int j)> Pdiag(int k, int m) => ALL.Where(c => (c.i + c.j) % m == k).ToList();
		List<(int i, int j)> Padiag(int k, int m) => ALL.Where(c => ((c.i - c.j) % m + m) % m == k).ToList();
		List<(int i, int j)> Pring(int d) => ALL.Where(c => Math.Min(Math.Min(c.i, c.j), Math.Min(HX_C - 1 - c.i, HX_R - 1 - c.j)) == d).ToList();
		List<(int i, int j)> Prand(int n) { var a = ALL.ToList(); for (int q = a.Count - 1; q > 0; q--) { int r = (int)Math.Floor(R() * (q + 1)); (a[q], a[r]) = (a[r], a[q]); } return slice(a, 0, n); }
		void tiles(double hit, List<(int i, int j)> cells, Tier T, double dur = 0.6) => ev(hit - T.wn, () => hazards.Add(HxTiles(cells, B(hit - T.wn), B(hit), B(hit + dur))));
		// around the player: their tile and the ones next to it (they must get out)
		void tilesP(double hit, Tier T, int rad = 1, double dur = 0.6, int par = -1) => ev(hit - T.wn, () => { var (pi, pj) = cellP(); if (par >= 0 && (pi + pj) % 2 != par) return; hazards.Add(HxTiles(ALL.Where(c => Math.Abs(c.i - pi) <= rad && Math.Abs(c.j - pj) <= rad).ToList(), B(hit - T.wn), B(hit), B(hit + dur))); });
		// the whole floor breaks except the tiles around you
		void tilesX(double hit, Tier T, int rad = 1, double dur = 0.8) => ev(hit - T.wn, () => { var (pi, pj) = cellP(); hazards.Add(HxTiles(ALL.Where(c => Math.Abs(c.i - pi) > rad || Math.Abs(c.j - pj) > rad).ToList(), B(hit - T.wn), B(hit), B(hit + dur))); });
		void blockP(double hit, double w, double h, Tier T, double dur = 1) { double jx = (R() * 2 - 1) * 90, jy = (R() * 2 - 1) * 70; ev(hit - T.wn, () => { double x = clamp(PXp() + jx, w / 2, W - w / 2), y = clamp(PYp() + jy, h / 2, H - h / 2); hazards.Add(HxBlock(x, y, w, h, B(hit - T.wn), B(hit), B(hit + dur))); }); }
		void scan(double hit, bool vert, double p0, double p1, double dur, Tier T, double gc = -1, double gs = 0) => ev(hit - T.wn, () => hazards.Add(HxScan(vert, p0, p1, B(hit - T.wn), B(hit), B(hit + dur), gc, gs)));
		void err(double hit, double x, double y, double w, double h, Tier T, int n = 8, string lab = "") => ev(hit - T.wn - 0.6, () => hazards.Add(HxErr(x, y, w, h, B(hit - T.wn - 0.6), B(hit), n, T.sp * 0.8, lab)));
		void errP(double hit, double w, double h, Tier T, int n = 8) { double jx = (R() * 2 - 1) * 120, jy = (R() * 2 - 1) * 80; ev(hit - T.wn - 0.6, () => { double x = clamp(PXp() + jx, w / 2 + 10, W - w / 2 - 10), y = clamp(PYp() + jy, h / 2 + 10, H - h / 2 - 10); hazards.Add(HxErr(x, y, w, h, B(hit - T.wn - 0.6), B(hit), n, T.sp * 0.8)); }); }
		void worm(double b0, double b1, double x, double y, Tier T, int n = 9) => ev(b0 - 2 * T.wn, () => hazards.Add(HxWorm(x, y, B(b0 - 2 * T.wn), B(b0), B(b1), T.sp * 0.66, 2.0, n)));
		void gate(double hit, bool vert, double gc, double gs, Tier T, double dir, double spd = 0.85) => ev(hit - T.wn, () => hazards.Add(HxGate(vert, gc, gs, B(hit - T.wn), B(hit), T.sp * spd, dir)));
		void spin(double b0, double b1, double x, double y, double Rr, Tier T, int segs, int on, double rot, double a0 = 0, double w = 22) => ev(b0 - 2 * T.wn, () => hazards.Add(HxSpin(x, y, Rr, B(b0 - 2 * T.wn), B(b0), B(b1), segs, on, w, rot, a0)));
		void fx(double b, double f2, double s2, double tear = 0) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); if (tear != 0 && !double.IsNaN(tear)) hazards.Add(HxTear(B(b), tear)); });

		E.Add(new Ev { b = 0, keep = true, f = () => hazards.Add(HxGrid(B(0), B(399.5))) });
		ev(0, () => hazards.Add(HxLoader(B(0), B(36), "LOADING  H3RD.EXE")));
		ev(0, () => hazards.Add(HxFloor(B(0), B(131))));
		/* ---------- phase 1: the grid (0:10 - 1:00) ---------- */
		var T = TI[0];
		scan(3, false, -10, H + 10, 7, T, W * 0.55, 240);
		for (int k = 0; k < 4; k++) tiles(8 + 2 * k, Prand(3 + k), T, 0.8);
		tiles(16, Pcol(new[] { 1, 2 }), T, 0.8); tiles(18, Pcol(new[] { 5, 6 }), T, 0.8); tiles(20, Prow(new[] { 0, 1 }), T, 0.8); tiles(22, Prow(new[] { 3, 4 }), T, 0.8);
		// diagonal lines moving every beat: you have to move diagonally with them
		for (int b = 24; b < 32; b++) tiles(b, Pdiag(b % 4, 4), T, 0.5);
		err(33, W * 0.25, H * 0.5, 200, 170, T, 6, "3"); err(34, W * 0.75, H * 0.5, 200, 170, T, 6, "2"); err(35, W * 0.5, H * 0.28, 200, 170, T, 8, "1");
		fx(36, 0.55, 22, 1.0);
		err(36, W / 2, H / 2, 380, 150, T, 12, "H3RD");
		// every phrase (16 beats) a different shape, on 12 half the screen, on 14 around you, on 15 an ERROR window
		void phrase(double s, string kind, Tier T)
		{
			for (int i = 0; i < 12; i++)
			{
				double b = s + i;
				if (kind == "checker") { if (i % 2 == 0) tiles(b, Pchecker((i / 2) % 2), T, 0.8); else if (i > 2) tilesP(b, T, 0, 0.5, ((i + 1) / 2) % 2); }
				else if (kind == "rows") { tiles(b, i < 5 ? Prow(new[] { i % 5, (i + 3) % 5 }) : Pcol(new[] { (i - 5) % 8, (i - 1) % 8 }), T, 0.7); }
				else if (kind == "diag") { tiles(b, i < 6 ? Pdiag(i % 3, 3) : Padiag(i % 3, 3), T, 0.7); }
				else if (kind == "ring") { if (i % 2 == 0) tiles(b, new[] { Pring(0), Pring(0), Pring(1), Pring(1), Pring(0).Concat(Pring(1)).ToList(), Pring(2) }[i / 2], T, 0.8); }
				else if (kind == "rand") { tiles(b, Prand(i < 6 ? 14 : 17), T, 0.7); }
			}
			tilesX(s + 12, T, 1, 0.9); fx(s + 12, 0.15, 12);
			tilesP(s + 14, T, 1, 0.6); tiles(s + 15, Pring(0), T, 0.6);
			errP(s + 15, 230, 150, T, 8);
		}
		phrase(36, "checker", T); phrase(52, "rows", T); phrase(68, "diag", T); phrase(84, "ring", T); phrase(100, "rand", T);
		scan(52, false, -10, H + 10, 8, T, W * 0.3, 220); scan(84, true, W + 10, -10, 8, T, H * 0.6, 200); scan(100, false, H + 10, -10, 8, T, W * 0.7, 220);
		for (int b = 116; b < 124; b++) tiles(b, b % 2 != 0 ? Pchecker(1) : Pdiag(b % 4, 4), T, 0.5);
		// 124-131 the connection gets weak
		spin(124, 132, W / 2, H / 2, 150, T, 8, 3, 1.6, 0, 24); tiles(126, Pring(0), T, 0.8); tiles(130, Pring(0), T, 0.8);
		/* ---------- phase 2: the firewall (1:00 - 1:51) ---------- */
		T = TI[1];
		fx(132, 0.35, 14, 0.6);
		ev(132, () => hazards.Add(HxLoader(B(132), B(196), "FIREWALL  ACTIVE")));
		// on the broken beat: two walls coming from two sides and crossing, with blocks on you between them
		for (int gI = 0; gI < 8; gI++)
		{
			double s = 132 + gI * 8; bool vert = gI % 2 == 0; double len = vert ? H : W, g1 = 110 + R() * (len - 220), g2 = 110 + R() * (len - 220);
			gate(s, vert, g1, 150, T, 1); gate(s + 4, vert, g2, 150, T, -1);
			blockP(s + 2, 110, 110, T, 1.1); blockP(s + 6, 110, 110, T, 1.1);
			gate(s + 5, !vert, 110 + R() * ((vert ? W : H) - 220), 165, T, gI % 4 < 2 ? 1 : -1);
		}
		worm(142, 160, W + 30, H * 0.2, T, 10); worm(174, 192, -30, H * 0.8, T, 10);
		errP(147, 240, 160, T, 8); errP(163, 240, 160, T, 8); errP(179, 240, 160, T, 8);
		fx(196, 0.4, 16, 0.8); err(196, W / 2, H / 2, 300, 190, T, 12, "404");
		// 1:27 the maze: walls one after another and the gate winds, you walk in it like a corridor
		ev(204, () => hazards.Add(HxLoader(B(204), B(268), "REBOOTING  SYSTEM")));
		for (int k = 0; k < 12; k++) { double b = 204 + k * 2; gate(b, true, H / 2 + 190 * JS.sin(k * 0.55), 165, T, -1, 0.95); }
		worm(214, 228, -30, H * 0.5, T, 9);
		for (int b = 207; b < 252; b += 4) blockP(b, 100, 100, T, 1);
		for (int k = 0; k < 12; k++) { double b = 228 + k * 2; gate(b, false, W / 2 + 380 * JS.sin(k * 0.5 + 1), 170, T, 1, 0.95); }
		spin(236, 252, W * 0.5, H * 0.5, 200, T, 10, 3, -1.2, 0, 22);
		// 1:45 it comes out: walls from all four sides
		for (int k = 0; k < 6; k++) { double b = 252 + k * 2; bool vert = k % 2 == 0; double len = vert ? H : W; gate(b, vert, 110 + R() * (len - 220), 170, T, k % 4 < 2 ? 1 : -1, 1.05); blockP(b + 1, 100, 100, T, 0.9); }
		err(265, W * 0.25, H * 0.5, 200, 170, T, 6, "3"); err(266, W * 0.75, H * 0.5, 200, 170, T, 6, "2"); err(267, W * 0.5, H * 0.72, 200, 170, T, 8, "1");
		/* ---------- phase 3: the boss (1:51 - 2:40) ---------- */
		T = TI[2];
		fx(268, 0.6, 24, 1.0);
		var MV = new[] { new[] { 276, W * 0.25, H * 0.3 }, new[] { 284, W * 0.75, H * 0.32 }, new[] { 292, W * 0.5, H * 0.25 }, new[] { 300, W * 0.22, H * 0.6 }, new[] { 308, W * 0.78, H * 0.6 }, new[] { 321, W * 0.2, H * 0.35 }, new[] { 329, W * 0.8, H * 0.62 }, new[] { 337, W * 0.5, H * 0.22 }, new[] { 345, W * 0.25, H * 0.7 }, new[] { 353, W * 0.75, H * 0.3 }, new[] { 361, W * 0.3, H * 0.3 }, new[] { 368, W * 0.5, H * 0.5 }, new[] { 385, W * 0.78, H * 0.3 }, new[] { 393, W * 0.5, H * 0.25 } };
		var TT = T; ev(266, () => hazards.Add(HxSkull(B(268), B(392), B(396), TT, MV)));
		// the skull calls things from phases 1 and 2
		foreach (var s in new double[] { 268, 284, 300 }) { tiles(s + 12, Pchecker((int)(Math.Floor(s / 16) % 2)), T, 0.7); fx(s + 12, 0.15, 12); bool vert = R() < 0.5; gate(s + 15, vert, 110 + R() * ((vert ? H : W) - 220), 180, T, R() < 0.5 ? 1 : -1); blockP(s + 5, 110, 110, T, 1); blockP(s + 9, 110, 110, T, 1); }
		foreach (var b in new double[] { 324, 332, 348, 356, 364 }) tilesP(b + 1, T, 1, 0.6);
		foreach (var b in new double[] { 320, 328, 336, 344, 352, 360 }) tiles(b + 3, Prand(14), T, 0.6);
		worm(338, 352, -30, H * 0.15, T, 8);
		// the peak (2:28): beams spinning in the middle
		ev(368 - 2 * T.wn, () => hazards.Add(HxBeams(W / 2, H / 2, 4, B(368 - 2 * T.wn), B(370), B(374), 0.3, 0.9)));
		ev(374 - T.wn, () => hazards.Add(HxBeams(W / 2, H / 2, 4, B(374 - T.wn), B(374.5), B(380), 0.3 + 0.9 * (B(374) - B(370)), -1.0)));
		tiles(372, Pring(0), T, 0.8); tiles(377, Pring(0), T, 0.8);
		for (int gI = 0; gI < 2; gI++) { double s = 380 + gI * 8; tilesP(s + 5, T, 1, 0.6); blockP(s + 1, 110, 110, T, 1.1); gate(s + 3, gI == 0, gI == 0 ? H * 0.6 : W * 0.4, 180, T, gI != 0 ? 1 : -1); }
		fx(396, 0.5, 0, 0.8);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
