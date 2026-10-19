using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 2874-3191 (F7 shapes, F7Sched, buildFriend7 = the AFK version for v37/v38 replays)
// and 3194-3486 (buildFriend7v30 = the old shape, the game now). The shapes inside buildFriend7v30 (3198-3304) are the same text as 2874-2983, so they are ported once.
// Friends level 7: Let Me Love You (AJF)
public static partial class G
{
	// the road of phase 2: {c(u), hw(u), u(t,y)}
	public class F7RoadO { public Func<double, double, double> u; public Func<double, double> c, hw; }
	// a difficulty tier with its base speed (JS keeps `base` through ezT)
	public class F7T { public double sp, wn, bs; }
	static F7T[] F7ezT(F7T[] a)
	{
		var q = ezT(a.Select(t => new Tier { sp = t.sp, wn = t.wn }).ToArray());
		return a.Select((t, i) => new F7T { sp = q[i].sp, wn = q[i].wn, bs = t.bs }).ToArray();
	}

	static readonly int _afkF7 = AfkReg("buildFriend7", (x, y, b, v, w) => One(F7Knife(true, 1, x, y, 180, B(b - w), B(b), B(b + 1.5))));

	// a beam from point to point (ext = goes on past the second point)
	public static Hz F7Beam(double x0, double y0, double x1, double y1, double w, double tw, double ta, double te, double ext = 0)
	{
		double a = JS.atan2(y1 - y0, x1 - x0), L = hypot(x1 - x0, y1 - y0) + ext, c = JS.cos(a), s = JS.sin(a);
		return Laser(new LaserO { x = x0 + c * L / 2, y = y0 + s * L / 2, ang = a, len = L, w = w, tw = tw, ta = ta, te = te });
	}
	// a decoration word (harmless)
	public static Hz F7Word(string s, double x, double y, double size, double t0, double t1, string col)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double e = t - t0, a = min(1, e / 0.08, (t1 - t) / 0.3), sc = 1 + 0.3 * JS.exp(-e * 10); g.globalAlpha = Math.Max(0, a); g.save(); g.translate(x, y); g.scale(sc, sc); g.font = $"{str(size)}px {TFONT}";
			g.fillStyle = string.IsNullOrEmpty(col) ? GREY + "0.5)" : col; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, 0, 0); g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	/* ---- phase 1: the mirrors ---- */
	// the mirror in the middle (decoration): shines with the beat, cracks from tc and breaks at tx
	public static Hz F7Mirror(double t0, double tc, double tx)
	{
		bool bk = false; var h = new Hz();
		h.update = (t, dt) => { if (t >= tx) { if (!bk) { bk = true; for (int i = 0; i < 9; i++) burst(W / 2, 40 + i * 80, "#fff", 4, 300); shake = Math.Max(shake, 14); } h.dead = true; } };
		h.draw = t =>
		{
			double a = Math.Min(1, (t - t0) / 1.5); if (a <= 0) return; double x = W / 2, bt = BT(t); g.globalAlpha = a;
			g.fillStyle = GREY + "0.05)"; g.fillRect(x - 16, 0, 32, H); g.fillStyle = GREY + toFixed(0.3 + 0.3 * pulse, 3) + ")"; g.fillRect(x - 1.5, 0, 3, H);
			for (int i = 0; i < 4; i++) { double y = ((bt * 0.125 + i / 4.0) % 1) * H; g.save(); g.translate(x, y); g.rotate(Math.PI / 4); g.fillStyle = GREY + "0.55)"; g.fillRect(-5, -5, 10, 10); g.restore(); }
			if (t > tc)
			{
				double k = clamp((t - tc) / (tx - tc), 0, 1); g.strokeStyle = $"rgba(255,255,255,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2;
				for (int c = 0; c < 5; c++) { double y0 = 72 + c * 144, n = Math.Floor(2 + 6 * k); g.beginPath(); g.moveTo(x, y0); for (int j = 1; j <= n; j++) { double sd = (j + c) % 2 != 0 ? 1 : -1; g.lineTo(x + sd * j * 9, y0 + (c % 2 != 0 ? 1 : -1) * j * 11); } g.stroke(); }
			}
			g.globalAlpha = 1;
		};
		return h;
	}
	// a long glass panel: shows, hurts, then breaks into shards that fly away from the mirror (dir)
	public static Hz F7Panel(double x, double w, double tw, double ta, double te, double n, double sp, double dir, double inw = 0)
	{
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= te)
			{
				for (int i = 0; i < n; i++) { double y = (i + 0.5) * H / n; hazards.Add(Shard(x, y, dir * sp, (i - (n - 1) / 2) * sp * 0.07, 9)); }
				for (int i = 0; i < inw; i++) { double y = (i + (dir < 0 ? 0.25 : 0.75)) * H / inw; hazards.Add(Shard(x, y, -dir * sp * 0.8, 0, 9)); }
				burst(x, H / 2, "#fff", 10, 260); h.dead = true;
			}
		};
		h.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(x - w / 2, 0, w, H); g.strokeStyle = $"rgba(255,42,109,{str(0.35 + 0.5 * k)})"; g.lineWidth = 2; g.strokeRect(x - w / 2 + 1, -2, w - 2, H + 4); return; }
			g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = (t - ta) < 0.07 ? "#fff" : PINK; g.fillRect(x - w / 2, 0, w, H); g.restore();
			g.fillStyle = "rgba(255,255,255,0.32)"; double o = (BT(t) * 40) % 120; for (double y = -120 + o; y < H; y += 120) { g.beginPath(); g.moveTo(x - w / 2, y + w * 0.9); g.lineTo(x + w / 2, y); g.lineTo(x + w / 2, y + 18); g.lineTo(x - w / 2, y + w * 0.9 + 18); g.closePath(); g.fill(); }
			if (te - t < 0.25) { g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 2; g.beginPath(); for (int i = 0; i < 5; i++) { g.moveTo(x, H * (i + 0.5) / 5); g.lineTo(x + (i % 2 != 0 ? w / 2 : -w / 2), H * (i + 0.5) / 5 + 30); } g.stroke(); }
		};
		h.hit = (p, t) => t >= ta && t < te && Math.Abs(p.x - x) < w / 2 + p.r - 2;
		return h;
	}
	// a knife: comes out of the screen edge (sd -1 left/top, 1 right/bottom) up to reach, and goes back. vert = from top or bottom
	public static Hz F7Knife(bool vert, double sd, double c, double reach, double h_, double tw, double ta, double te)
	{
		double h = h_, hk = Math.Min(h, 90); Func<double, double> ext = t => t < ta ? 0 : t < ta + 0.1 ? reach * (t - ta) / 0.1 : t < te ? reach : t < te + 0.15 ? reach * (1 - (t - te) / 0.15) : 0;
		void tr() { g.translate(vert ? c : (sd < 0 ? 0 : W), vert ? (sd < 0 ? 0 : H) : c); if (vert) g.rotate(sd < 0 ? Math.PI / 2 : -Math.PI / 2); else if (sd > 0) g.scale(-1, 1); }
		var o = new Hz();
		o.update = (t, dt) => { if (t > te + 0.15) o.dead = true; };
		o.draw = t =>
		{
			g.save(); tr();
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(0, -h / 2, reach, h); g.fillStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; for (double d = 0; d < reach * k; d += 26) g.fillRect(d, -1.5, 14, 3); g.restore(); return; }
			double e = ext(t); if (e < 1) { g.restore(); return; }
			g.fillStyle = GREY + "0.8)"; g.fillRect(-20, -h * 0.3, Math.Min(e, 64) + 20, h * 0.6);
			if (e > 64)
			{
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.12 ? "#fff" : PINK; g.beginPath(); g.moveTo(64, -h / 2); g.lineTo(Math.Max(64, e - hk * 0.8), -h / 2); g.lineTo(e, Math.Min(h / 2, hk * 0.18)); g.lineTo(Math.Max(64, e - hk * 0.5), h / 2); g.lineTo(64, h / 2); g.closePath(); g.fill(); g.shadowBlur = 0;
				g.fillStyle = "rgba(255,255,255,0.45)"; g.fillRect(64, -h / 2 + 5, Math.Max(0, e - 64 - hk), 5);
			}
			g.restore();
		};
		o.hit = (p, t) => { if (t < ta || t >= te + 0.05) return false; double e = ext(t), q = vert ? (sd < 0 ? p.y : H - p.y) : (sd < 0 ? p.x : W - p.x), oo = vert ? p.x - c : p.y - c; return q - p.r + 2 < e - hk * 0.25 && Math.Abs(oo) < h / 2 + p.r - 2; };
		return o;
	}
	// the hug: two arms close from both sides and leave a gap in the middle (vert: from top and bottom)
	public static Hz F7Hug(bool vert, double c, double gap, double tw, double ta, double te)
	{
		double ext = vert ? H : W, a = c - gap / 2, b = c + gap / 2; Func<double, double> cl = t => t < ta ? 0 : t < ta + 0.15 ? (t - ta) / 0.15 : t < te ? 1 : clamp(1 - (t - te) / 0.25, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			void R(double x0, double x1) { if (vert) g.fillRect(0, x0, W, x1 - x0); else g.fillRect(x0, 0, x1 - x0, H); }
			if (t < ta) { double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * q)})"; R(0, a); R(b, ext); g.fillStyle = "rgba(255,42,109,0.85)"; if (vert) { g.fillRect(0, a - 2, W, 4); g.fillRect(0, b - 2, W, 4); } else { g.fillRect(a - 2, 0, 4, H); g.fillRect(b - 2, 0, 4, H); } return; }
			double k = cl(t), ea = a * k, eb = ext - (ext - b) * k, span = vert ? W : H; g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.12 ? "#fff" : PINK; R(0, ea); R(eb, ext);
			// fingers on the arm's edge
			foreach (var (e0, sg) in new[] { (ea, 1.0), (eb, -1.0) }) for (int i = 0; i < 4; i++) { double m = span * (0.32 + i * 0.12), fr = 18; if (vert) circle(m, e0 + sg * fr * 0.2, fr); else circle(e0 + sg * fr * 0.2, m, fr); }
			g.restore(); g.fillStyle = "rgba(255,255,255,0.25)"; for (int i = 0; i < 3; i++) { double m = span * (0.2 + i * 0.3); if (vert) { g.fillRect(m, 0, 8, ea); g.fillRect(m, eb, 8, ext - eb); } else { g.fillRect(0, m, ea, 8); g.fillRect(eb, m, ext - eb, 8); } }
		};
		h.hit = (p, t) => { if (cl(t) < 0.9 || t >= te) return false; double q = vert ? p.y : p.x, k = cl(t); return q - p.r + 2 < a * k || q + p.r - 2 > ext - (ext - b) * k; };
		return h;
	}
	/* ---- phase 2: the road ---- */
	// the road: outside the road hurts. The lane lines move down
	public static Hz F7Road(F7RoadO R, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = min(1, (t - t0) / 0.5, (t1 - t) / 0.5); if (a <= 0) return; g.globalAlpha = a; const double st = 12; var L = new List<(double x, double y)>(); var Rr = new List<(double x, double y)>();
			for (double y = -st; y <= H + st; y += st) { double u = R.u(t, y), c = R.c(u), w = R.hw(u); L.Add((c - w, y)); Rr.Add((c + w, y)); }
			g.fillStyle = "rgba(255,42,109,0.2)"; g.beginPath(); g.moveTo(-10, -st); foreach (var (x, y) in L) g.lineTo(x, y); g.lineTo(-10, H + st); g.closePath(); g.fill();
			g.beginPath(); g.moveTo(W + 10, -st); foreach (var (x, y) in Rr) g.lineTo(x, y); g.lineTo(W + 10, H + st); g.closePath(); g.fill();
			g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 5; foreach (var P in new[] { L, Rr }) { g.beginPath(); for (int i = 0; i < P.Count; i++) { if (i != 0) g.lineTo(P[i].x, P[i].y); else g.moveTo(P[i].x, P[i].y); } g.stroke(); }
			g.restore();
			// lane lines (grey, harmless)
			g.fillStyle = GREY + "0.32)"; for (double y = -st; y <= H + st; y += 8) { double u = R.u(t, y); if (((u % 96) + 96) % 96 > 48) continue; double c = R.c(u), w = R.hw(u); if (w > 600) continue; foreach (var f in new[] { -0.5, 0, 0.5 }) g.fillRect(c + f * w - 2, y, 4, 8); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 + 0.3 || t >= t1) return false; double u = R.u(t, p.y), c = R.c(u), w = R.hw(u); return Math.Abs(p.x - c) > w - p.r + 2; };
		return h;
	}
	// a car coming at you in lane f (-1 to 1 of the road width). Before it, its lights light up the lane
	public static Hz F7Car(F7RoadO R, double f, double tw, double ta, double V, double cw = 60, double cl = 104)
	{
		Func<double, double> y = t => -70 + V * (t - ta), x = t => { double yy = y(t), u = R.u(t, yy); return R.c(u) + f * R.hw(u); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > ta && y(t) > H + 90) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * k)})"; for (double yy = 0; yy < H; yy += 20) { double u = R.u(t, yy); g.fillRect(R.c(u) + f * R.hw(u) - cw / 2, yy, cw, 20); }
				double u0 = R.u(t, 0), x0 = R.c(u0) + f * R.hw(u0); g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; circle(x0 - cw * 0.3, 8, 5 + 3 * k); circle(x0 + cw * 0.3, 8, 5 + 3 * k); return;
			}
			double yy2 = y(t), xx = x(t); g.save(); g.translate(xx, yy2); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.roundRect(-cw / 2, -cl / 2, cw, cl, 12); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(0,0,0,0.45)"; g.fillRect(-cw / 2 + 8, cl * 0.08, cw - 16, cl * 0.2); g.fillRect(-cw / 2 + 8, -cl * 0.36, cw - 16, cl * 0.14);
			g.fillStyle = "#fff"; circle(-cw * 0.3, cl / 2 - 6, 6); circle(cw * 0.3, cl / 2 - 6, 6); g.fillStyle = "rgba(255,255,255,0.12)"; g.beginPath(); g.moveTo(-cw * 0.3, cl / 2); g.lineTo(-cw * 0.9, cl / 2 + 120); g.lineTo(cw * 0.9, cl / 2 + 120); g.lineTo(cw * 0.3, cl / 2); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; double yy = y(t); return Math.Abs(p.y - yy) < cl / 2 + p.r - 3 && Math.Abs(p.x - x(t)) < cw / 2 + p.r - 3; };
		return h;
	}
	// the eyelids: close from top and bottom and leave a gap around cy(t) (don't fall asleep while driving)
	public static Hz F7Lids(Func<double, double> cy, double gap, double tw, double ta, double te)
	{
		Func<double, double> cl = t => t < ta ? 0 : t < ta + 0.18 ? (t - ta) / 0.18 : t < te ? 1 : clamp(1 - (t - te) / 0.3, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double c = cy(t), a = c - gap / 2, b = c + gap / 2;
			if (t < ta) { double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * q)})"; g.fillRect(0, 0, W, a); g.fillRect(0, b, W, H - b); g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(0, a - 2, W, 4); g.fillRect(0, b - 2, W, 4); return; }
			double k = cl(t), ea = a * k, eb = H - (H - b) * k; g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.12 ? "#fff" : PINK; g.fillRect(0, 0, W, ea); g.fillRect(0, eb, W, H - eb); g.restore();
			g.strokeStyle = GREY + "0.75)"; g.lineWidth = 3; g.beginPath(); for (double x = 20; x < W; x += 44) { g.moveTo(x, ea); g.lineTo(x + (x - W / 2) * 0.02, ea + 16); g.moveTo(x, eb); g.lineTo(x + (x - W / 2) * 0.02, eb - 16); }
			g.stroke();
		};
		h.hit = (p, t) => { double k = cl(t); if (k < 0.9 || t >= te) return false; double c = cy(t); return p.y - p.r + 2 < (c - gap / 2) * k || p.y + p.r - 2 > H - (H - c - gap / 2) * k; };
		return h;
	}
	/* ---- phase 3: the ring ---- */
	// a diamond ring: the band is grey (harmless) and the pink diamond hurts. Spins from spin, cracks from tc and breaks at tx
	public static Hz F7Gem(Func<double, (double x, double y)> pos, double ts, double spin, double tc, double tx)
	{
		const double S = 50; bool ex = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= tx && !ex) { ex = true; var (x, y) = pos(t); for (int i = 0; i < 50; i++) burst(x, y, i % 3 != 0 ? PINK : "#fff", 1, 560); burst(x, y, "#fff", 40, 620); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; }
		};
		h.draw = t =>
		{
			if (t < ts) return; var (x, y) = pos(t); if (t > tc) { double k = (t - tc) / (tx - tc); x += JS.sin(t * 83) * 6 * k; y += JS.cos(t * 71) * 6 * k; }
			g.save(); g.translate(x, y);
			g.strokeStyle = GREY + "0.7)"; g.lineWidth = 15; g.beginPath(); g.arc(0, S + 62, 66, 0, TAU); g.stroke(); g.strokeStyle = "rgba(255,255,255,0.35)"; g.lineWidth = 4; g.beginPath(); g.arc(0, S + 62, 66, Math.PI * 1.1, Math.PI * 1.6); g.stroke();
			g.fillStyle = GREY + "0.85)"; g.fillRect(-26, S * 0.15, 10, 30); g.fillRect(16, S * 0.15, 10, 30);
			double sx = t < spin ? 1 : JS.cos((t - spin) * 4); g.scale(Math.Max(0.15, Math.Abs(sx)), 1 + 0.08 * pulse);
			g.shadowColor = PINK; g.shadowBlur = 26; g.fillStyle = PINK; g.beginPath(); g.moveTo(-0.55 * S, -0.55 * S); g.lineTo(0.55 * S, -0.55 * S); g.lineTo(S, -0.12 * S); g.lineTo(0, S); g.lineTo(-S, -0.12 * S); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = 2; g.beginPath(); g.moveTo(-S, -0.12 * S); g.lineTo(S, -0.12 * S); g.moveTo(-0.55 * S, -0.55 * S); g.lineTo(-0.25 * S, -0.12 * S); g.lineTo(0, -0.55 * S); g.lineTo(0.25 * S, -0.12 * S); g.lineTo(0.55 * S, -0.55 * S);
			g.moveTo(-0.25 * S, -0.12 * S); g.lineTo(0, S); g.lineTo(0.25 * S, -0.12 * S); g.stroke();
			if (t > tc) { double k = clamp((t - tc) / (tx - tc), 0, 1); g.strokeStyle = "#fff"; g.lineWidth = 2.5; g.beginPath(); for (int c = 0; c < 3; c++) { g.moveTo(0, -0.3 * S); double px = 0, py = -0.3 * S; for (int j = 0; j < Math.Floor(1 + 5 * k); j++) { px += (c - 1) * 9 + (j % 2 != 0 ? 6 : -6); py += 11; g.lineTo(px, py); } } g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < ts + 0.8 || t >= tx) return false; var (x, y) = pos(t); return hypot(p.x - x, p.y - (y + 0.1 * S)) < S * 0.72 + p.r - 2; };
		return h;
	}
	// beams spinning around the diamond (pos moves)
	public static Hz F7Spin(Func<double, (double x, double y)> pos, int arms, Func<double, double> angF, double tw, double t0, double t1, double w = 20, double r0 = 70)
	{
		const double L = 1700;
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.3) h.dead = true; };
		h.draw = t =>
		{
			var (cx, cy) = pos(t); double A = angF(t); g.save(); g.translate(cx, cy);
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); for (int i = 0; i < arms; i++) { g.save(); g.rotate(A + (double)i * TAU / arms); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; g.fillRect(r0, -w / 2, L, w); g.fillStyle = $"rgba(255,42,109,{str(0.45 + 0.45 * k)})"; g.fillRect(r0, -1.5, L * k, 3); g.restore(); } g.restore(); return; }
			double kk = t < t1 ? 1 : Math.Max(0, 1 - (t - t1) / 0.3); g.globalAlpha = kk; g.shadowColor = PINK; g.shadowBlur = 18;
			for (int i = 0; i < arms; i++) { g.save(); g.rotate(A + (double)i * TAU / arms); g.fillStyle = (t - t0) < 0.07 ? "#fff" : PINK; g.fillRect(r0, -w / 2 * (1 + 0.12 * pulse), L, w * (1 + 0.12 * pulse)); g.fillStyle = "rgba(255,255,255,0.4)"; g.fillRect(r0, -2, L, 4); g.restore(); }
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= t1) return false; var (cx, cy) = pos(t); double A = angF(t); if (hypot(p.x - cx, p.y - cy) < r0 - p.r) return false;
			for (int i = 0; i < arms; i++) { double a = A + (double)i * TAU / arms; if (obbHit(p.x, p.y, p.r - 1, cx + JS.cos(a) * (r0 + L / 2), cy + JS.sin(a) * (r0 + L / 2), L / 2, w / 2, a)) return true; }
			return false;
		};
		return h;
	}
	// a fixed attack table (not on the player): fires each attack on its beat. Lives the whole level so Easy does not drop the fill attacks
	public static Hz F7Sched(List<Ev> L)
	{
		sort(L, (a, b) => a.b - b.b); int i = -1; var h = new Hz();
		h.update = (t, dt) =>
		{
			if (i < 0) { i = 0; while (i < L.Count && B(L[i].b) < t - 0.02) i++; }
			while (i < L.Count && B(L[i].b) <= t) { L[i].f(); i++; }
			if (i >= L.Count && t > B(L[L.Count - 1].b) + 1) h.dead = true;
		};
		return h;
	}

	/* friends level 7: Let Me Love You (DJ Snake ft. Justin Bieber), picked by AJF. 100 bpm (a beat = 0.6 s)
	   phase 1 the mirrors (0:00-1:07) | phase 2 the road (1:07-2:05, checkpoint 112) | phase 3 the ring (2:05-2:42, checkpoint 208) */
	public static List<Ev> buildFriend7() => bF7(true);      // line 2984: the AFK version (v37/v38 replays)
	public static List<Ev> buildFriend7v30() => bF7(false);  // line 3305: the old shape (the game now)

	// A = the AFK version (line 2984); otherwise the v30 one (line 3305). Every difference is marked with A.
	static List<Ev> bF7(bool A)
	{
		var E = new List<Ev>(); var R = mulberry32(7007);
		void evk(double b, Action f) => E.Add(new Ev { b = b, keep = true, f = f });
		// KEEP (A only): in Easy these attacks run on a fixed table so they are not dropped. Normal is unchanged
		string KEEP = null;
		var SQ = new Dictionary<string, List<Ev>>(); var NE = new[] { "sh", "ep", "mk", "cc" };
		Action<double, Action> sq(string k) => (b, f) =>
		{
			if (easy && NE.Contains(k)) return;
			if (!SQ.ContainsKey(k)) { SQ[k] = new List<Ev>(); evk(0, () => hazards.Add(F7Sched(SQ[k]))); }
			SQ[k].Add(new Ev { b = b, f = f });
		};
		void ev(double b, Action f) { if (A && KEEP != null && easy) sq(KEEP)(b, f); else E.Add(new Ev { b = b, f = f }); }
		void kp(string k, Action fn) { KEEP = k; fn(); KEEP = null; }
		var DF = new { sp = 1.1, wn = 1.0, gp = 1.0, hd = 1.5, wd = 1.45, pn = 1.8, d1 = true, d2 = true, d3 = true };
		var TI = F7ezT(new[] { new F7T { sp = 330 * DF.sp, wn = 1.3 * DF.wn, bs = 330 }, new F7T { sp = 350 * DF.sp, wn = 1.25 * DF.wn, bs = 350 }, new F7T { sp = 370 * DF.sp, wn = 1.2 * DF.wn, bs = 370 } });
		double kf(F7T T) => T.sp / T.bs; const double CX = W / 2, CY = H / 2;
		double PXp() => player != null ? player.x : CX; double PYp() => player != null ? player.y : CY;
		void fx(double b, double f2, double s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
		void word(double b, string s, double x, double y, double size, double dur, string col) => ev(b, () => hazards.Add(F7Word(s, x, y, size, B(b), B(b + dur), col)));
		// in Easy the warning starts on a whole beat so the attacks are not all dropped
		double wB(double hb, double w) => easy ? Math.Floor(hb - w) : hb - w;
		void beam(double hb, double x0, double y0, double x1, double y1, double w, F7T T, double dur = 0.5, double ext = 0) { w *= DF.wd; dur *= DF.hd; double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(F7Beam(x0, y0, x1, y1, w, B(wb), B(hb), B(hb + dur), ext))); }
		// V: a beam from the edge hits the mirror at (CX,ym) and reflects to the other edge
		void V(double hb, double yE, double ym, double w, F7T T, double dur = 0.5) { beam(hb, -40, yE, CX + 8, ym, w, T, dur); beam(hb, W + 40, yE, CX - 8, ym, w, T, dur); }
		void panel(double hb, double d, double w, F7T T, double hold, double n, double spk, double inw = 0) { w *= DF.wd; n = round(n * DF.pn); inw = round(inw * DF.pn); double wb = wB(hb, T.wn); ev(wb, () => { foreach (var sd in new[] { -1.0, 1 }) hazards.Add(F7Panel(CX + sd * d, w, B(wb), B(hb), B(hb + hold), n, T.sp * spk, sd, inw)); }); }
		void knife(double hb, bool vert, double c, double reach, double h, F7T T, double hold = 1.4, double[] sides = null) { sides ??= new[] { -1.0, 1 }; h *= DF.wd; hold *= DF.hd; double wb = wB(hb, T.wn); ev(wb, () => { foreach (var sd in sides) hazards.Add(F7Knife(vert, sd, c, reach, h, B(wb), B(hb), B(hb + hold))); }); }
		// a knife in the mirror: right and left on the same row
		void knifeRow(double hb, double y, double reach, F7T T, double hold) => knife(hb, false, y, reach, 96, T, hold);
		// knives from top and bottom on two mirrored columns
		void knifeCol(double hb, double d, double sd, double reach, F7T T, double hold) { hold *= DF.hd; double wb = wB(hb, T.wn); ev(wb, () => { foreach (var m in new[] { -1.0, 1 }) hazards.Add(F7Knife(true, sd, CX + m * d, reach, 84 * DF.wd, B(wb), B(hb), B(hb + hold))); }); }
		void slash(double hb, double x, double y, double a, F7T T, double dur = 0.35, double w = 26) { w *= DF.wd; dur *= DF.hd; double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(Slash(x, y, a, B(wb), B(hb), B(hb + dur), w))); }
		void hug(double hb, bool vert, double gap, F7T T, double hold) { gap *= DF.gp; double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(F7Hug(vert, vert ? CY : CX, gap, B(wb), B(hb), B(hb + hold)))); }
		// A: filling the empty spots (Mohammed 2026-10-15): normal attacks on fixed times, each kind on its own table
		// the mirror shines: a beam on the mirror line itself
		void shine(double hb, F7T T, double w = 230, double dur = 0.45) { double wb = wB(hb, T.wn); sq("sh")(wb, () => hazards.Add(F7Beam(CX, -40, CX, H + 40, w, B(wb), B(hb), B(hb + dur * DF.hd)))); }
		// glass panels on the screen edges
		void edgeP(double hb, F7T T) { double w = 120, wb = wB(hb, T.wn); sq("ep")(wb, () => { foreach (var sd in new[] { -1.0, 1 }) hazards.Add(F7Panel(CX + sd * (CX - 60), w, B(wb), B(hb), B(hb + 1.2), round(6 * DF.pn), T.sp * 0.5, sd)); }); }
		// a knife in the mirror: from top or bottom on the mirror line
		void midK(double hb, double sd, F7T T, double hold = 1.0, double rch = H - 150) { hold *= DF.hd; double wb = wB(hb, T.wn); sq("mk")(wb, () => hazards.Add(F7Knife(true, sd, CX, rch, 180, B(wb), B(hb), B(hb + hold)))); }

		/* ---------- phase 1: the mirrors (0:00 - 1:07) ---------- */
		var T = TI[0];
		evk(0, () => hazards.Add(F7Mirror(B(0), B(104), B(111.5))));
		// 0-15 the intro: glass panels show in the mirror and break softly
		double pw = A ? 80 : 64;
		panel(4, 470, pw, T, 2, 6, 0.5); panel(8, 300, pw, T, 2, 6, 0.5); panel(12, 130, pw, T, 2, 6, 0.55);
		V(6, -40, CY, 22, T); V(10, H + 40, CY, 22, T); V(14, -40, H * 0.4, 24, T);
		if (DF.d1) { panel(2, 560, pw, T, 2, 6, 0.5); panel(6, 380, pw, T, 2, 6, 0.5); panel(10, 210, pw, T, 2, 6, 0.55); panel(14, 470, pw, T, 2, 6, 0.55); V(4, H + 40, H * 0.6, 22, T); V(8, -40, H * 0.35, 22, T); V(12, H + 40, H * 0.65, 24, T); V(15, H + 40, CY, 24, T); }
		// 16-47 V beams and panels in the mirror
		var YM = new[] { 0.5, 0.32, 0.68, 0.42, 0.58, 0.28, 0.72, 0.5 };
		for (int b = 16; b < 48; b += 2) { bool up = (b / 2) % 2 == 0; V(b, up ? -40 : H + 40, H * YM[(b / 2) % 8], 40, T, 0.8); }
		var PD = A ? new double[] { 413, 92, 306, 520, 199 } : new double[] { 520, 380, 240, 100, 240, 380 };
		for (int b = DF.d1 ? 17 : 25; b < 48; b += 2) V(b, (b >> 1) % 2 != 0 ? H + 40 : -40, H * YM[((b + 3) >> 1) % 8], 30, T, 0.4);
		for (int b = 17; b < 48; b += 2) panel(b, PD[((b - 17) / 2) % PD.Length], A ? 100 : 80, T, 1.5, DF.d1 ? 10 : 8, 0.62, b >= (DF.d1 ? 18 : 24) ? 4 : 0);
		// 32-39 "smoke and mirrors": V and Λ together (an X in the mirror)
		foreach (var b in new[] { 33, 37 }) V(b, H + 40, H * YM[((b - 1) / 2) % 8], 26, T);
		// a star of shards from the middle of the mirror
		void star(double hb, int n, double a0, F7T T, double y = CY) => ev(hb, () => { for (int i = 0; i < n; i++) { double a = a0 + (double)i * TAU / n; hazards.Add(Shard(CX + JS.cos(a) * 30, y + JS.sin(a) * 30, JS.cos(a) * T.sp * 0.6, JS.sin(a) * T.sp * 0.6, 9)); } burst(CX, y, "#fff", 12, 300); });
		star(26, DF.d1 ? 20 : 14, 0, T); star(36, DF.d1 ? 20 : 14, 0.22, T, H * 0.4); star(43, DF.d1 ? 22 : 16, 0.1, T, H * 0.6);
		// 48-77 "go through the darkest of days ... driving the edge of a knife": the knives
		double LY(double i) => 60 + i * 120;
		var L1 = new[] { 0, 3, 1, 4, 2, 5, 3, 0, 4, 1, 5, 2, 0, 3, 1, 4 };
		for (int b = 48; b < 56; b++) { knifeRow(b, LY(L1[b - 48]), CX - 75, T, 1.4); if (b % 2 != 0 || DF.d1) knifeRow(b + 0.5, LY((L1[b - 48] + 3) % 6), CX - 75, T, 1.0); }
		// "heaven's a heartbreak away": full knives (no gap in the middle)
		foreach (var (b, i) in new[] { (52, 2), (54, 4) }) knifeRow(b + 0.5, LY(i), CX + 10, T, 1.0);
		// 56-63 knives from top then bottom on mirrored columns
		var CD = new double[] { 540, 400, 260, 120 };
		for (int b = 56; b < 60; b++) { knifeCol(b, CD[b - 56], -1, H - 140, T, 1.3); knifeRow(b + 0.5, LY(new[] { 5, 0, 4, 1 }[b - 56]), 260, T, 1.0); }
		for (int b = 60; b < 64; b++) { knifeCol(b, CD[63 - b], 1, H - 140, T, 1.3); knifeRow(b + 0.5, LY(new[] { 0, 5, 1, 4 }[b - 60]), 260, T, 1.0); }
		foreach (var b in new[] { 56, 58, 60, 62 }) V(b + 0.5, b % 4 != 0 ? H + 40 : -40, CY, 24, T, 0.4);
		// 64-67 "hell of a ride": a wall of knives with one empty row, the empty row moves
		var G1 = new[] { 2, 3, 4, 3, 2, 1, 0, 1 };
		for (int b = 64; b < 68; b++) for (int i = 0; i < 6; i++) { if (i == G1[b - 64]) continue; knifeRow(b, LY(i), CX + 10, T, 0.55 / DF.hd); }
		// 68-71 two big knives from both sides, a thin line stays in the middle
		ev(wB(68, T.wn), () => { double wb = wB(68, T.wn); foreach (var sd in new[] { -1.0, 1 }) hazards.Add(F7Knife(false, sd, CY, CX - 55, H * 2, B(wb), B(68), B(71))); });
		for (double b = 68; b < 71; b += 0.5) { double b2 = b; ev(b, () => hazards.Add(Shard(CX + ((b2 * 2) % 2 != 0 ? 24 : -24), -20, 0, T.sp * 1.0, 8))); }
		// 72-77 rows and beams
		for (int b = 72; b < 77; b++) { knifeRow(b, LY(L1[b - 68]), CX - 75, T, 1.3); if (DF.d1 && b < 76) knifeRow(b + 0.5, LY((L1[b - 68] + 3) % 6), CX - 75, T, 1.0); }
		foreach (var b in new[] { 72, 74, 76 }) V(b + 0.5, b % 4 != 0 ? H + 40 : -40, H * YM[b % 8], 28, T, 0.4);
		// 78-111 the chorus: "Don't you give up" a big X, "nah nah nah" 3 hits, "Let me love you" the hug
		void gu(double hb, F7T T) { slash(hb, CX, CY, 0.55, T, 0.4, 30); slash(hb, CX, CY, Math.PI - 0.55, T, 0.4, 30); }
		// nah: mirrored columns that come closer to the mirror (in) or move away
		void nah(double hb, double dir, F7T T) { var D = dir > 0 ? new double[] { 520, 340, 160 } : new double[] { 160, 340, 520 }; var O = new[] { 0, 0.5, 1 }; for (int k = 0; k < O.Length; k++) { foreach (var sd in new[] { -1.0, 1 }) slash(hb + O[k], CX + sd * D[k], CY, Math.PI / 2, T, 0.3, 32); } }
		// horizontal nah: rows in the mirror
		int nahN = 0;
		void nahH(double hb, F7T T)
		{
			if (!A) { var Y0 = new[] { 0.2, 0.8, 0.5 }; for (int k = 0; k < Y0.Length; k++) slash(hb + k * 0.5, CX, H * Y0[k], 0, T, 0.3, 32); return; }
			double a = 0.05 + 0.0625 * new[] { 0, 2, 1, 3 }[(nahN++) % 4]; var Y = new[] { a, a + 0.5, a + 0.25, a + 0.75 }; double w = 32 * DF.wd, dur = 0.3 * DF.hd, w0 = wB(hb, T.wn);
			ev(w0, () => { for (int k = 0; k < Y.Length; k++) { double h = hb + k * 0.5; hazards.Add(Slash(CX, H * Y[k], 0, B(Math.Max(w0, wB(h, T.wn))), B(h), B(h + dur), w)); } });
		}
		void rain(double b0, double b1, double step, double[] xs, F7T T) { int k = 0; for (double b = b0; b < b1; b += step) { double x = xs[k++ % xs.Length]; ev(b, () => hazards.Add(Shard(x, -20, 0, T.sp * 1.05, 9))); } }
		void side(double b0, double b1, double step, double[] ys, F7T T) { int k = 0; for (double b = b0; b < b1; b += step) { double y = ys[k++ % ys.Length]; ev(b, () => { hazards.Add(Shard(-20, y, T.sp * 1.05, 0, 9)); hazards.Add(Shard(W + 20, y, -T.sp * 1.05, 0, 9)); }); } }
		foreach (var c in new double[] { 78, 94 })
		{
			gu(c, T); word(c, "DON'T GIVE UP", CX, H * 0.18, 60, 2, "rgba(255,255,255,0.55)"); nah(c + 1, 1, T);
			gu(c + 4, T); nah(c + 5, -1, T);
			hug(c + 8, false, 200, T, 2); rain(c + 8.5, c + 10, DF.d1 ? 0.25 : 0.5, new[] { CX - 58, CX + 58 }, T); word(c + 8, "LET ME LOVE YOU", CX, H * 0.5, 58, 2, "rgba(255,255,255,0.4)");
			V(c + 2.5, H + 40, H * 0.6, 24, T, 0.4); V(c + 6.5, -40, H * 0.4, 24, T, 0.4); V(c + 10.5, -40, CY, 26, T, 0.4);
			hug(c + 12, true, 210, T, 2); side(c + 12.5, c + 14, DF.d1 ? 0.25 : 0.5, new[] { CY - 52, CY + 52 }, T);
			V(c + 14.5, H + 40, CY, 26, T, 0.4);
			if (DF.d1) foreach (var (o, yE, ym) in new[] { (1.5, -40.0, 0.3), (3.5, H + 40, 0.7), (5.5, -40.0, 0.65), (9.5, H + 40, 0.35), (13.5, -40.0, 0.5) }) V(c + o, yE, H * ym, 24, T, 0.4);
		}
		foreach (var b in (DF.d1 ? new double[] { 80, 82, 84, 86, 88, 90, 92, 96, 98, 100, 102, 104, 106 } : new double[] { 80, 84, 88, 92, 96, 100, 104 })) nahH(b + 0.5, T);
		// 108-111 "baby": the mirror cracks, panels on the edges, and at 111.5 it breaks and shards fly
		kp("pn", () => { panel(108, 560, 60, T, 1.5, 6, 0.6); panel(109.5, 420, 60, T, 1.5, 6, 0.6); }); if (DF.d1) { panel(108.5, 280, 60, T, 1.5, 6, 0.6); panel(110, 140, 60, T, 1.2, 6, 0.6); }
		ev(111.5, () => { for (int i = 0; i < 8; i++) { double y = 45 + i * 90; foreach (var sd in new[] { -1.0, 1 }) hazards.Add(Shard(CX + sd * 20, y, sd * T.sp * 0.85, (i % 2 != 0 ? 1 : -1) * 30, 10)); } });
		if (A)
		{
			// filling the empty spots in phase 1
			foreach (var b in new double[] { 4, 8, 12, 18, 22, 26, 30, 34, 38, 42, 46, 80, 84, 91, 96, 100, 106 }) shine(b, T);
			foreach (var b in new double[] { 3, 7, 11, 15, 20, 28, 36, 44, 81, 89, 97, 105 }) edgeP(b, T);
			knifeRow(77, LY(1), CX - 75, T, 1.0); shine(110, T); kp("pn", () => panel(110.5, 230, 60, T, 1.0, 6, 0.6));
			foreach (var (b, sd) in new[] { (50.0, -1.0), (54, -1), (58, -1), (61, 1), (62.5, -1), (73, -1), (75, 1) }) midK(b, sd, T);
			midK(52, 1, T, 1.0, 260); // short: covers only below the white line
		}
		fx(112, 0.6, 24);

		/* ---------- phase 2: the road (1:07 - 2:05) ---------- */
		T = TI[1]; double K2 = kf(T);
		// the road moves down at speed RS. Each part of it has an "arrival beat" (when it reaches mid-screen), and its shape depends on it
		const double RS = 480; double tR0 = B(110), LAM = 8 * SPB * RS; double AB(double u) => BT(tR0 + (u + CY) / RS);
		double lerpK(double[][] K, double b)
		{
			if (b <= K[0][0]) return K[0][1];
			for (int i = 1; i < K.Length; i++) if (b < K[i][0]) { double a0 = K[i - 1][0], v0 = K[i - 1][1], a1 = K[i][0], v1 = K[i][1], q = (b - a0) / (a1 - a0), s = q * q * (3 - 2 * q); return v0 + (v1 - v0) * s; }
			return K[K.Length - 1][1];
		}
		var AK = new double[][] { new double[] { 113, 0 }, new double[] { 116, 120 }, new double[] { 128, 120 }, new double[] { 130, 170 }, new double[] { 144, 150 }, new double[] { 176, 170 }, new double[] { 190, 150 }, new double[] { 196, 130 }, new double[] { 201, 110 }, new double[] { 203, 0 } }.Select(p => new[] { p[0], p[1] * (easy ? 0.75 : 1) }).ToArray();
		var HK = new double[][] { new double[] { 111, 780 }, new double[] { 113.5, 300 * DF.gp }, new double[] { 176, 300 * DF.gp }, new double[] { 188, 215 * DF.gp }, new double[] { 195, 110 }, new double[] { 201, 110 }, new double[] { 203.5, 300 }, new double[] { 205, 300 }, new double[] { 206.5, 780 } };
		var ROAD = new F7RoadO { u = (t, y) => RS * (t - tR0) - y, c = u => { double b = AB(u); return CX + lerpK(AK, b) * JS.sin(TAU * u / LAM); }, hw = u => lerpK(HK, AB(u)) };
		E.Add(new Ev { b = 110, keep = true, f = () => hazards.Add(F7Road(ROAD, B(110), B(207))) });
		word(112, "LET ME LOVE YOU", CX, H * 0.3, 76, 2.5, "rgba(255,255,255,0.6)");
		double VC = RS + 430 * K2;
		// a car passes mid-screen on beat hb
		void car(double hb, double f, F7T T, double cw = 60) { cw *= DF.wd; double ta = B(hb) - (CY + 70) / VC, wb = wB(BT(ta), T.wn); ev(wb, () => hazards.Add(F7Car(ROAD, f, B(wb), ta, VC, cw))); }
		var LN = new[] { -0.72, -0.24, 0.24, 0.72 };
		void row(double hb, int gap, F7T T, double cw = 60) { for (int i = 0; i < 4; i++) if (i != gap) car(hb, LN[i], T, cw); }
		// 116-143 the drop: rows of cars with an empty lane
		var Z1 = new[] { 1, 2, 1, 0, 1, 2, 3, 2, 1, 2, 3, 2 };
		for (int b = 116; b < 128; b++) { row(b, Z1[b - 116], T); if (b >= (DF.d2 ? 116 : 120) && b < 127 && (Z1[b - 116] + 2) % 4 != Z1[b - 115]) car(b + 0.5, LN[(Z1[b - 116] + 2) % 4], T); }
		var Z = new[] { 0, 1, 2, 3, 2, 1 };
		for (int b = 128; b < 144; b++) { row(b, Z[(b - 128) % 6], T); if (b < 143) car(b + 0.5, LN[(Z[(b - 128) % 6] + 2) % 4], T); }
		fx(128, 0.25, 10);
		// 144-175 "Don't fall asleep at the wheel": the eyelids close, the cars go on
		void lids(double hb, Func<double, double> cyF, double gap, F7T T, double hold) { gap *= DF.gp; double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(F7Lids(cyF, gap, B(wb), B(hb), B(hb + hold)))); }
		lids(145, t => CY, 190, T, 2.5); word(144, "DON'T FALL ASLEEP", CX, H * 0.5, 54, 2, "rgba(255,255,255,0.5)");
		foreach (var (b, gp) in new[] { (146, 2), (147, 1) }) row(b, gp, T);
		for (int b = 149; b < 160; b++) { row(b, Z[(b - 149 + 3) % 6], T); if (DF.d2 && b < 159) car(b + 0.5, LN[(Z[(b - 149 + 3) % 6] + 2) % 4], T); }
		// "all that we need": the eyelids close and the gap moves from top to bottom
		lids(160, t => CY + 150 * JS.sin(Math.PI * (BT(t) - 160) / 3.5 - Math.PI / 2), 200, T, 3.5);
		foreach (var (b, gp) in new[] { (161, 1), (162, 2), (163, 1) }) row(b, gp, T);
		// "rude awakening": a horn, high beams cut the screen
		fx(164, 0.5, 18); word(164, "WAKE UP!", CX, H * 0.5, 90, 1.6, "rgba(255,255,255,0.7)");
		beam(164, -40, H * 0.2, W + 40, H * 0.2, 46, T, 0.6); beam(164, -40, H * 0.8, W + 40, H * 0.8, 46, T, 0.6);
		for (int b = 165; b < 172; b++) { row(b, Z[(b - 165 + 2) % 6], T); if (b < 171) car(b + 0.5, LN[(Z[(b - 165 + 2) % 6] + 2) % 4], T); }
		// "know we're good enough"
		lids(172, t => CY, 220, T, 3); for (int b = 173; b < 176; b++) { row(b, new[] { 2, 1, 2 }[b - 173], T); if (DF.d2 && b < 175) car(b + 0.5, LN[new[] { 0, 3, 0 }[b - 173]], T); }
		// 176-195 "go through the darkest of days": the road narrows
		for (int b = 176; b < 188; b++) { row(b, Z[(b - 176) % 6], T); if (b < 187) car(b + 0.5, LN[(Z[(b - 176) % 6] + 2) % 4], T); }
		for (int b = 188; b < 196; b++) { kp("ec", () => car(b, b % 2 != 0 ? 0.5 : -0.5, T, 46)); if (DF.d2) car(b + 0.5, b % 2 != 0 ? -0.5 : 0.5, T, 46); }
		// 196-200 "driving the edge of a knife": the narrowest road, cars in the middle
		word(196, "EDGE OF A KNIFE", CX, H * 0.18, 50, 2.5, "rgba(255,255,255,0.5)");
		for (int b = 197; b < 201; b++) kp("ec", () => car(b, 0, T, 38));
		// 201-206 the road widens again
		foreach (var (b, gp) in new[] { (202, 1), (203, 2), (204, 0), (205, 3) }) kp("ec", () => row(b, gp, T));
		if (A)
		{
			// a car in the middle of the road every 6 beats (fills the gap between the two middle lanes)
			for (int b = 118; b < 192; b += 6) kp("cc", () => car(b + 0.5, 0, T));
			// Easy: many attacks are dropped, so every 8 beats (fixed times) an attack on your spot:
			// phase 1 a beam reflected from the mirror at your height, phase 2 a car in your lane
			if (easy)
			{
				void aimB(double hb, F7T T) { double wb = wB(hb, T.wn); sq("aim")(wb, () => { double y = PYp(); foreach (var sd in new[] { -1.0, 1 }) hazards.Add(F7Beam(CX, y, sd < 0 ? -40 : W + 40, y, 30 * DF.wd, B(wb), B(hb), B(hb + 0.4 * DF.hd))); }); }
				void aimC(double hb, F7T T) { double wb = wB(hb, T.wn); sq("aim")(wb, () => { double x = PXp(), y = PYp(), ta = B(hb), u = ROAD.u(ta, y), f = clamp((x - ROAD.c(u)) / ROAD.hw(u), -0.9, 0.9), V2 = Math.Max(VC, (y + 70) / (ta - B(wb) - 0.05)); hazards.Add(F7Car(ROAD, f, B(wb), ta - (y + 70) / V2, V2, 60 * DF.wd)); }); }
				foreach (var b in new double[] { 6, 14, 22, 30, 38, 46, 54, 60, 72, 78, 86, 94, 102, 108 }) aimB(b, TI[0]);
				for (int b = 116; b < 188; b += 8) aimC(b, TI[1]);
			}
		}
		fx(206, 0.3, 10);

		/* ---------- phase 3: the ring (2:05 - 2:42) ---------- */
		T = TI[2]; double K3 = kf(T);
		double tG() => B(205);
		(double, double) gpos(double t)
		{
			double t0 = tG(); if (t < t0) return (CX, -220); double k = clamp((t - t0) / (3 * SPB), 0, 1), en = 1 - JS.pow(1 - k, 3), bt = BT(t), m = clamp((bt - 240) / 2, 0, 1) * clamp((268 - bt) / 2, 0, 1);
			return (CX + JS.sin(TAU * (bt - 240) / 16) * 320 * m, -220 + 370 * en + JS.sin(TAU * (bt - 240) / 8) * 50 * m);
		}
		evk(204, () => hazards.Add(F7Gem(t => gpos(t), B(205), B(240), B(266), B(270))));
		(double x, double y) gem(double t) { var (x, y) = gpos(t); return (x, y + 5); }
		// a fan of beams from the diamond down
		void fan(double hb, int n, double spr, double off, F7T T, double dur = 0.5) { double wb = wB(hb, T.wn); ev(wb, () => { var (x, y) = gem(B(hb)); for (int i = 0; i < n; i++) { double a = Math.PI / 2 + off + (i - (n - 1) / 2.0) * spr; hazards.Add(F7Beam(x, y, x + JS.cos(a) * 1600, y + JS.sin(a) * 1600, 22 * DF.wd, B(wb), B(hb), B(hb + dur * DF.hd))); } }); }
		// where the beam meets the screen edge
		(double, double) hitEdge(double x, double y, double a)
		{
			double c = JS.cos(a), s = JS.sin(a); double d = 1e9; if (c > 1e-6) d = Math.Min(d, (W - x) / c); if (c < -1e-6) d = Math.Min(d, -x / c); if (s > 1e-6) d = Math.Min(d, (H - y) / s); if (s < -1e-6) d = Math.Min(d, -y / s);
			return (x + c * d, y + s * d);
		}
		// a broken beam: from the diamond at you, hits the screen edge and splits into two beams
		void prism(double hb, F7T T, double jit = 0)
		{
			double wb = wB(hb, T.wn);
			ev(wb, () =>
			{
				var (x, y) = gem(B(hb)); double a = JS.atan2(PYp() - y, PXp() - x) + jit; var (ex, ey) = hitEdge(x, y, a);
				hazards.Add(F7Beam(x, y, ex, ey, 24 * DF.wd, B(wb), B(hb), B(hb + 0.45), 20));
				bool vertWall = Math.Abs(ex) < 2 || Math.Abs(ex - W) < 2; double ra = vertWall ? Math.PI - a : -a;
				foreach (var d in new[] { -0.42, 0.42 }) { double b2 = ra + d; var (fx2, fy2) = hitEdge(ex + JS.cos(b2) * 2, ey + JS.sin(b2) * 2, b2); hazards.Add(F7Beam(ex, ey, fx2, fy2, 20 * DF.wd, B(wb), B(hb + 0.3), B(hb + 0.7), 20)); }
			});
		}
		// the ring closes on you: a ring around your spot shrinks, with a gap
		void ringIn(double hb, double gapA, F7T T, double r0 = 290, double dur = 1.6) { double wb = wB(hb, T.wn + 0.4); ev(wb, () => { double x = clamp(PXp(), 120, W - 120), y = clamp(PYp(), 120, H - 120); hazards.Add(RingSqueeze(x, y, r0, 26, B(wb), B(hb), B(hb + dur), 16, gapA, 0.95)); }); }
		// a ring of shards from the diamond with a gap
		void gring(double hb, double gapA, F7T T, double gs = 0.9) { ev(hb - T.wn, () => { var (x, y) = gem(B(hb)); hazards.Add(Ring(x, y, B(hb), T.sp * 0.7, 16, gapA, gs, T.wn * SPB)); }); }
		// 208-237 the second chorus
		fx(208, 0.45, 18);
		var RG = new[] { 0.6, 2.4, 4.0, 5.5, 1.4, 3.3, 4.8, 0.2 };
		foreach (var c in new double[] { 208, 224 })
		{
			int ci = (int)((c - 208) / 16);
			kp("fn", () => fan(c, DF.d3 ? 9 : 7, DF.d3 ? 0.24 : 0.3, 0, T)); word(c, "DON'T GIVE UP", CX, H * 0.85, 56, 2, "rgba(255,255,255,0.5)");
			kp("pr", () => prism(c + 1, T)); prism(c + 1.5, T, 0.3); prism(c + 2, T, -0.3);
			fan(c + 4, DF.d3 ? 8 : 6, DF.d3 ? 0.24 : 0.3, 0, T); kp("gr", () => gring(c + 3, RG[ci * 2], T));
			kp("pr", () => prism(c + 5, T)); prism(c + 5.5, T, -0.3); prism(c + 6, T, 0.3);
			ringIn(c + 8, RG[ci * 4 % 8], T); word(c + 8, "LET ME LOVE YOU", CX, H * 0.85, 56, 2, "rgba(255,255,255,0.45)");
			gring(c + 9, RG[3], T); fan(c + 10.5, 5, 0.38, 0, T, 0.4);
			ringIn(c + 12, RG[ci * 4 % 8 + 2], T); kp("gr", () => gring(c + 13, RG[6], T)); if (DF.d3) { prism(c + 3, T, 0.2); prism(c + 7, T, -0.2); kp("pr", () => { prism(c + 11, T); prism(c + 15, T); }); }
			fan(c + 14.5, 5, 0.38, 0.19, T, 0.4);
		}
		// mirrored like phase 1: V beams to the bottom corners
		foreach (var b in new[] { 218, 234 }) V(b + 0.5, H + 40, H * 0.55, 26, T, 0.4);
		foreach (var b in new[] { 236, 237, 238, 239 }) kp("gr", () => gring(b, RG[b % 8], T, 0.8));
		ev(238, () => burst(CX, 150, "#fff", 24, 380));
		// 240-265 the second drop: the diamond spins and fires beams around it, and rings
		fx(240, 0.6, 24); word(240, "LET ME LOVE YOU", CX, H * 0.5, 76, 2.5, "rgba(255,255,255,0.6)");
		double spA(double t)
		{
			double bt = BT(t); double seg = Math.Floor((bt - 240) / 8), q = bt - 240 - seg * 8; double a = 0.4;
			for (int s = 0; s < seg; s++) a += (s % 2 != 0 ? -1 : 1) * 8 * SPB * 1.0 * K3;
			return a + (seg % 2 != 0 ? -1 : 1) * q * SPB * 1.0 * K3;
		}
		ev(239 - T.wn, () => hazards.Add(F7Spin(t => gem(t), easy ? 2 : 3, spA, B(239 - T.wn), B(240), B(263), 20 * DF.wd)));
		for (int b = 241; b < 260; b += 2) gring(b, RG[(b >> 1) % 8], T, 1.25);
		for (int b = 244; b < 264; b += DF.d3 ? 2 : 4) kp("pr", () => prism(b, T));
		// 256-263 like the road: rows of shards falling from the top with a gap
		void shRow(double hb, int gc, F7T T) => ev(hb, () => { for (int i = 0; i < 12; i++) { if (i == gc || i == gc + 1) continue; hazards.Add(Shard(53 + i * 107, -20, 0, T.sp * 0.9, 10)); } });
		for (int b = DF.d3 ? 248 : 256; b < 264; b += 2) shRow(b, new[] { 2, 7, 4, 9 }[((b - 248) / 2) % 4], T);
		// 264-269 the last hug, and the diamond cracks and breaks at 270 (2:42)
		hug(264, false, 260, T, 2); rain(264.5, 266, 0.5, new[] { CX - 62, CX + 62 }, T);
		fan(267, 9, 0.24, 0, T, 0.5); fan(268.5, 8, 0.24, 0.12, T, 0.5);
		fx(270, 0.7, 0);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
