using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 913-1140: "ليش ضيعت الوفا" (halfPath, Tear, HalfHeart, HeartBomb, Clap, Chain, BgRain, FadeHeart, IntroHeart,
// TearCurtain, Candle, BigTear, CryEye, Frame, Crack, ECG, BrokenHeart) and buildFriend1. heartPath, GREY, ShardTri are in Shared.cs
public static partial class G
{
	static readonly int _afkF1 = AfkReg("buildFriend1", (x, y, b, v, w) => One(v >= 2 ? Clap(v == 2 ? "V" : "H", v == 2 ? x : y, 200, B(b - w), B(b), B(b + 0.5)) : Chain(x, y, v != 0 ? Math.PI / 2 : 0, B(b - w), B(b), B(b + 0.8), 180)));

	public static void halfPath(double s, double side) { g.beginPath(); g.moveTo(0, 0.8 * s); g.bezierCurveTo(side * 1.2 * s, 0, side * 0.6 * s, -1.0 * s, 0, -0.35 * s); g.lineTo(-side * 0.08 * s, -0.1 * s); g.lineTo(side * 0.07 * s, 0.15 * s); g.lineTo(-side * 0.05 * s, 0.45 * s); g.closePath(); }

	public static Hz Tear(double x, double y, double vx, double vy, double r = 9)
	{
		var b = Bullet(x, y, vx, vy, r); double a = JS.atan2(vy, vx);
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.rotate(a); double s = 1 + 0.15 * pulse; g.fillStyle = "rgba(255,42,109,0.22)"; g.beginPath(); g.arc(0, 0, 13 * s, Math.PI / 2, -Math.PI / 2); g.lineTo(22 * s, 0); g.closePath(); g.fill(); g.fillStyle = PINK; g.beginPath(); g.arc(0, 0, 9 * s, Math.PI / 2, -Math.PI / 2); g.lineTo(16 * s, 0); g.closePath(); g.fill(); g.restore(); };
		return b;
	}

	public static Hz HalfHeart(double x, double y, double vx, double vy, double s, double side)
	{
		double rot = 0;
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) => { h.x += vx * dt; h.y += vy * dt; rot += side * dt * 2; if (h.x < -150 || h.x > W + 150 || h.y < -150 || h.y > H + 150) h.dead = true; };
		h.draw = t => { g.save(); g.translate(h.x, h.y); g.rotate(rot); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; halfPath(s * (1 + 0.1 * pulse), side); g.fill(); g.restore(); };
		h.hit = (p, t) => hypot(p.x - h.x - side * 0.35 * s, p.y - h.y + 0.1 * s) < 0.5 * s + p.r - 2;
		return h;
	}

	public static Hz HeartBomb(double x, double y, double s, double tw, double ta, double tc, double sp, double ang = 0)
	{
		double c = JS.cos(ang), sn = JS.sin(ang);
		var h = new Hz();
		h.update = (t, dt) => { if (t >= tc) { hazards.Add(HalfHeart(x, y, -sp * c, -sp * sn, s, -1)); hazards.Add(HalfHeart(x, y, sp * c, sp * sn, s, 1)); burst(x, y, PINK, 12, 240); shake = Math.Max(shake, 4); h.dead = true; } };
		h.draw = t =>
		{
			g.save(); g.translate(x, y);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.scale(1.5 - 0.5 * k, 1.5 - 0.5 * k); heartPath(s); g.restore(); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.2 * k)})"; g.fill(); g.strokeStyle = "rgba(255,42,109,0.8)"; g.lineWidth = 2; heartPath(s); g.stroke(); }
			else
			{
				double k = 1 + 0.1 * pulse; g.scale(k, k); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = (t - ta) < 0.08 ? "#fff" : PINK; heartPath(s); g.fill(); g.shadowBlur = 0;
				double q = clamp((t - ta) / (tc - ta), 0, 1); if (q > 0.4) { g.strokeStyle = "#000"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, -0.35 * s); g.lineTo(-0.08 * s, -0.1 * s); g.lineTo(0.07 * s, 0.15 * s * (q > 0.75 ? 1 : 0.4)); if (q > 0.75) g.lineTo(-0.05 * s, 0.45 * s); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) => t >= ta && hypot(p.x - x, p.y - (y - 0.05 * s)) < 0.68 * s + p.r - 2;
		return h;
	}

	public static Hz Clap(string axis, double c, double gap, double tw, double ta, double te)
	{
		bool v = axis == "V"; double ext = v ? W : H, a = c - gap / 2, b = c + gap / 2;
		Func<double, double> cl = t => t < ta ? 0 : t < ta + 0.07 ? (t - ta) / 0.07 : t < te ? 1 : clamp(1 - (t - te) / 0.2, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double k = cl(t); void rect(double x0, double x1) { if (v) g.fillRect(x0, 0, x1 - x0, H); else g.fillRect(0, x0, W, x1 - x0); }
			if (t < ta)
			{
				double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * q)})"; rect(0, a); rect(b, ext);
				g.fillStyle = "rgba(255,42,109,0.9)"; if (v) { g.fillRect(a - 2, 0, 4, H); g.fillRect(b - 2, 0, 4, H); } else { g.fillRect(0, a - 2, W, 4); g.fillRect(0, b - 2, W, 4); }
				return;
			}
			g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.1 ? "#fff" : PINK; rect(0, a * k); rect(ext - (ext - b) * k, ext); g.restore();
		};
		h.hit = (p, t) => { if (cl(t) < 0.9 || t >= te) return false; double q = v ? p.x : p.y; return q - p.r < a || q + p.r > b; };
		return h;
	}

	public static Hz Chain(double px, double py, double ang, double tw, double ta, double te, double sp)
	{
		const double L = 1700; double c = JS.cos(ang), s = JS.sin(ang); var A = new[] { px - c * L, py - s * L }; var Bp = new[] { px + c * L, py + s * L };
		var h = new Hz();
		h.update = (t, dt) => { if (t >= te) { for (int i = -3; i <= 3; i++) { if (i == 0) continue; double x = px + c * i * 70, y = py + s * i * 70; foreach (var d in new double[] { -1, 1 }) hazards.Add(Bullet(x, y, -s * d * sp, c * d * sp, 7)); } h.dead = true; } };
		h.draw = t =>
		{
			g.save(); g.translate(px, py); g.rotate(ang);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; for (double d = -L; d < L; d += 34) g.strokeRect(d, -5, 22, 10); }
			else { g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = (t - ta) < 0.08 ? "#fff" : PINK; g.lineWidth = 6; for (double d = -L; d < L; d += 34) { rr(d, -8, 26, 16, 7); g.stroke(); } }
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; return segDist(p.x, p.y, A, Bp) < 9 + p.r; };
		return h;
	}

	// ===== the sad decoration (harmless) =====
	public static Hz BgRain(double t0, double t1, int n = 70)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double a = Math.Min(1, Math.Min((t - t0) / 1.5, (t1 - t) / 1.5)); g.strokeStyle = GREY + str(0.13 * a) + ")"; g.lineWidth = 1.5; g.beginPath();
			for (int i = 0; i < n; i++) { double x = (((i * 97.31) % W) + t * 40) % W, y = ((t * 780 + i * 53.7) % (H + 120)) - 60; g.moveTo(x, y); g.lineTo(x - 7, y + 28); }
			g.stroke();
		};
		return h;
	}
	public static Hz FadeHeart(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, (t - t0) / 0.8) * clamp((t1 - t) / 1.5, 0, 1); g.save(); g.translate(W / 2, H / 2); g.globalAlpha = a; g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; heartPath(60 * (1 + 0.05 * pulse)); g.stroke(); g.restore(); g.globalAlpha = 1; };
		return h;
	}
	// ===== the start: a grey heart beating, then cracking and breaking =====
	public static Hz IntroHeart(double t0, double tb, double sp)
	{
		const double S = 95;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= tb)
			{
				for (int i = 0; i < 10; i++) { double a = i * TAU / 10 + 0.3; hazards.Add(ShardTri(W / 2, H / 2 - 10, JS.cos(a) * sp, JS.sin(a) * sp)); }
				hazards.Add(HalfHeart(W / 2, H / 2, -sp * 0.8, -sp * 0.25, S, -1)); hazards.Add(HalfHeart(W / 2, H / 2, sp * 0.8, sp * 0.25, S, 1)); burst(W / 2, H / 2, "#ffffff", 30, 400); shake = Math.Max(shake, 12); flash = Math.Max(flash, 0.3); h.dead = true;
			}
		};
		h.draw = t =>
		{
			if (t < t0) return; double a = clamp((t - t0) / 2, 0, 1), k = 1 + 0.08 * pulse, c = clamp((t - (tb - 4 * SPB)) / (4 * SPB), 0, 1); g.save(); g.translate(W / 2, H / 2); g.scale(k, k); g.globalAlpha = a;
			heartPath(S); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.16 * c)})"; g.fill(); g.strokeStyle = c > 0 ? $"rgba(255,42,109,{str(0.4 + 0.5 * c)})" : GREY + "0.55)"; g.lineWidth = 3; g.stroke();
			if (c > 0) { g.strokeStyle = "rgba(255,255,255,0.85)"; g.beginPath(); g.moveTo(0, -0.35 * S); g.lineTo(-0.08 * S, -0.1 * S); if (c > 0.33) g.lineTo(0.07 * S, 0.15 * S); if (c > 0.66) g.lineTo(-0.05 * S, 0.45 * S); if (c > 0.9) g.lineTo(0, 0.8 * S); g.stroke(); }
			g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	// ===== a curtain of tears with a swaying path =====
	public static Hz TearCurtain(double t0, double t1, double stepT, Func<double, double> gapF, double gw, double sp, double spacing)
	{
		double last = -1;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= t1) { h.dead = true; return; }
			if (t < t0) return; double k = Math.Floor((t - t0) / stepT); if (k == last) return; last = k; double gx = gapF(t);
			for (double x = spacing / 2; x < W; x += spacing) { if (Math.Abs(x - gx) < gw / 2) continue; hazards.Add(Tear(x, -20, 0, sp)); }
		};
		h.draw = t => { if (t >= t1) return; double gx = gapF(Math.Max(t, t0)); g.fillStyle = GREY + "0.35)"; g.fillRect(gx - gw / 2, 0, gw, 4); };
		return h;
	}
	// ===== a candle: its body is harmless, its flame hurts and throws sparks, then it goes out =====
	class Smoke { public double x, y, vx, vy, l; }
	public static Hz Candle(double x, double y, double tl, double tOut, double tEnd, double[] embers, double sp)
	{
		int ei = 0; var smoke = new List<Smoke>(); bool @out = false;
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t > tEnd) h.dead = true;
			while (ei < embers.Length && t >= embers[ei]) { if (t < tOut && player != null) { double fy = y - 42, a = JS.atan2(player.y - fy, player.x - x); hazards.Add(Bullet(x, fy, JS.cos(a) * sp, JS.sin(a) * sp, 7)); } ei++; }
			if (t >= tOut && !@out) { @out = true; gloomT = Math.Min(0.5, gloomT + 0.06); for (int i = 0; i < 6; i++) smoke.Add(new Smoke { x = x, y = y - 42, vx = (random() * 2 - 1) * 15, vy = -40 - random() * 30, l = 1.4 }); }
			foreach (var s in smoke) { s.x += s.vx * dt; s.y += s.vy * dt; s.l -= dt; }
		};
		h.draw = t =>
		{
			if (t < tl - 0.3) return; double a = Math.Min(1, (t - (tl - 0.3)) / 0.3) * clamp((tEnd - t) / 0.4, 0, 1); g.globalAlpha = a;
			g.fillStyle = GREY + "0.75)"; rr(x - 10, y - 34, 20, 58, 4); g.fill(); g.fillStyle = GREY + "0.9)"; g.fillRect(x - 1, y - 42, 2, 8);
			if (t >= tl && t < tOut) { double f = 1 + 0.15 * JS.sin(t * 28) + 0.15 * pulse; g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK; g.beginPath(); g.ellipse(x, y - 50, 7 * f, 13 * f, 0, 0, TAU); g.fill(); g.shadowBlur = 0; g.fillStyle = "#fff"; g.beginPath(); g.ellipse(x, y - 46, 2.5, 5, 0, 0, TAU); g.fill(); }
			foreach (var s in smoke) if (s.l > 0) { g.fillStyle = GREY + str(0.25 * s.l / 1.4) + ")"; circle(s.x, s.y, 10 + (1.4 - s.l) * 12); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => t >= tl + 0.15 && t < tOut && hypot(p.x - x, p.y - (y - 50)) < 9 + p.r;
		return h;
	}
	// ===== a big tear falls and splashes =====
	public static Hz BigTear(double x, double ty, double tw, double tl, double sp)
	{
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (t >= tl) { for (int i = 0; i < 16; i++) { double a = i * TAU / 16; hazards.Add(Tear(x, ty, JS.cos(a) * sp, JS.sin(a) * sp)); } hazards.Add(Ring(x, ty, tl, 260, 14, rnd() * TAU, 1.5, 0.01)); gloomT = 0; flash = Math.Max(flash, 0.35); shake = Math.Max(shake, 14); rumble(300, 0.9, 0.6); h.dead = true; }
		};
		h.draw = t =>
		{
			double k = clamp((t - tw) / (tl - tw), 0, 1), y = -90 + (ty + 90) * k * k; g.fillStyle = $"rgba(255,42,109,{str(0.08 + 0.2 * k)})"; g.beginPath(); g.ellipse(x, ty, 90 * (1.4 - 0.4 * k), 26 * (1.4 - 0.4 * k), 0, 0, TAU); g.fill();
			g.save(); g.translate(x, y); g.rotate(Math.PI / 2); g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; g.beginPath(); g.arc(0, 0, 38, Math.PI / 2, -Math.PI / 2); g.lineTo(-70, 0); g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) => { double k = clamp((t - tw) / (tl - tw), 0, 1), y = -90 + (ty + 90) * k * k; return hypot(p.x - x, p.y - y) < 36 + p.r; };
		return h;
	}
	// ===== a crying eye at the top =====
	public static Hz CryEye(double x, double t0, double t1, double[] cry, double sp)
	{
		int ci = 0;
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.4) h.dead = true; while (ci < cry.Length && t >= cry[ci]) { if (t < t1) hazards.Add(Tear(x, 62, 0, sp)); ci++; } };
		h.draw = t =>
		{
			if (t < t0 - 0.4) return; double o = clamp((t - (t0 - 0.4)) / 0.4, 0, 1) * clamp((t1 + 0.4 - t) / 0.4, 0, 1); g.save(); g.translate(x, 34); g.strokeStyle = GREY + str(0.7 * o) + ")"; g.lineWidth = 3;
			g.beginPath(); g.moveTo(-34, 0); g.quadraticCurveTo(0, 26 * o, 34, 0); g.stroke(); g.beginPath(); g.moveTo(-34, 0); g.quadraticCurveTo(0, -14 * o, 34, 0); g.stroke();
			for (int i = -2; i <= 2; i++) { g.beginPath(); g.moveTo(i * 12, 10 * o + 2); g.lineTo(i * 15, 18 * o + 4); g.stroke(); }
			double nx = double.PositiveInfinity; for (int i = ci; i < cry.Length; i++) { nx = cry[i] - t; break; }
			if (nx < 0.35 && t < t1) { g.fillStyle = $"rgba(255,42,109,{str(0.8 * (1 - nx / 0.35))})"; circle(0, 26, 5); }
			g.restore();
		};
		return h;
	}
	// ===== a memory frame cracks and breaks into glass =====
	public static Hz Frame(double x, double y, double w, double hh, double tw, double ta, double sp)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t >= ta) { for (int i = 0; i < 12; i++) { double a = i * TAU / 12 + rnd() * 0.3; hazards.Add(ShardTri(x + JS.cos(a) * w * 0.3, y + JS.sin(a) * hh * 0.3, JS.cos(a) * sp, JS.sin(a) * sp, 8)); } burst(x, y, "#ffffff", 16, 300); shake = Math.Max(shake, 6); h.dead = true; } };
		h.draw = t =>
		{
			double k = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.translate(x, y); g.globalAlpha = Math.Min(1, k * 3); g.strokeStyle = GREY + "0.75)"; g.lineWidth = 4; g.strokeRect(-w / 2, -hh / 2, w, hh); g.lineWidth = 2; g.strokeRect(-w / 2 + 10, -hh / 2 + 10, w - 20, hh - 20);
			g.save(); heartPath(Math.Min(w, hh) * 0.22); g.strokeStyle = GREY + "0.5)"; g.stroke(); g.restore();
			g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.fillRect(-w / 2, -hh / 2, w, hh);
			if (k > 0.3) { g.strokeStyle = "rgba(255,255,255,0.85)"; g.lineWidth = 2; int n = (int)Math.Floor(k * 6); for (int i = 0; i < n; i++) { double a = i * 1.1 + 0.4; g.beginPath(); g.moveTo(0, 0); g.lineTo(JS.cos(a) * w * 0.25, JS.sin(a) * hh * 0.2); g.lineTo(JS.cos(a + 0.3) * w * 0.48, JS.sin(a + 0.3) * hh * 0.45); g.stroke(); } }
			g.restore(); g.globalAlpha = 1;
		};
		return h;
	}
	// ===== cracks in the floor =====
	public static Hz Crack(double x, double y, double tw, double ta, double te, int n = 5, double len = 300)
	{
		var P = new List<double[][]>();
		for (int b = 0; b < n; b++) { double a = b * TAU / n + rnd() * 0.6, px = x, py = y; var seg = new List<double[]> { new[] { px, py } }; for (int i = 0; i < 6; i++) { a += (rnd() * 2 - 1) * 0.5; px += JS.cos(a) * len / 6; py += JS.sin(a) * len / 6; seg.Add(new[] { px, py }); } P.Add(seg.ToArray()); }
		Func<double, double> gr = t => clamp((t - ta) / 0.12, 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			bool act = t >= ta && t < te; double k = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : 1; g.lineJoin = "round"; g.lineCap = "round";
			g.strokeStyle = t < ta ? $"rgba(255,42,109,{str(0.2 + 0.45 * k)})" : PINK; g.lineWidth = t < ta ? 2 : (t < te ? 10 * (1 + 0.12 * pulse) : 10 * Math.Max(0, 1 - (t - te) / 0.25)); if (act) { g.shadowColor = PINK; g.shadowBlur = 14; }
			foreach (var s in P) { double m = t < ta ? Math.Ceiling(k * 6) : Math.Ceiling(gr(t) * 6); g.beginPath(); g.moveTo(s[0][0], s[0][1]); for (int i = 1; i <= m; i++) g.lineTo(s[i][0], s[i][1]); g.stroke(); }
			g.shadowBlur = 0;
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double m = Math.Ceiling(gr(t) * 6); foreach (var s in P) for (int i = 1; i <= m; i++) if (segDist(p.x, p.y, s[i - 1], s[i]) < 5 + p.r) return true; return false; };
		return h;
	}
	// ===== a heartbeat line: drawn from left to right with beats on the rhythm =====
	public static Hz ECG(double y, double t0, double t1, double te, double[] spikes, double amp, double tw)
	{
		const int N = 160; var P = new double[N + 1][];
		double off(double x) { double v = 0; foreach (var sx in spikes) { double d = x - sx; if (d > -30 && d < -12) v += amp * 0.15 * JS.sin((d + 30) / 18 * Math.PI); else if (d >= -12 && d < 0) v -= amp * (1 - (-d) / 12); else if (d >= 0 && d < 14) v += amp * 0.45 * (1 - d / 14); } return v; }
		for (int i = 0; i <= N; i++) { double x = i * W / N; P[i] = new[] { x, y + off(x) }; }
		Func<double, double> head = t => clamp((t - t0) / (t1 - t0), 0, 1);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			g.lineJoin = "round"; g.lineCap = "round";
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.4 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 6, 8 }); g.beginPath(); g.moveTo(P[0][0], P[0][1]); foreach (var q in P) g.lineTo(q[0], q[1]); g.stroke(); g.setLineDash(new double[0]); return; }
			int m = (int)Math.Floor(head(t) * N); double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.strokeStyle = PINK; g.lineWidth = 7; g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath(); g.moveTo(P[0][0], P[0][1]); for (int i = 1; i <= m; i++) g.lineTo(P[i][0], P[i][1]);
			g.stroke(); g.shadowBlur = 0;
			if (t < t1) { g.fillStyle = "#fff"; circle(P[m][0], P[m][1], 7); }
			g.globalAlpha = 1;
		};
		h.hit = (p, t) => { if (t < t0 || t >= te) return false; int m = (int)Math.Floor(head(t) * N); for (int i = 1; i <= m; i++) if (segDist(p.x, p.y, P[i - 1], P[i]) < 4 + p.r) return true; return false; };
		return h;
	}

	// ===== the boss: the broken heart =====
	// a whole heart comes in, splits into two halves that spin and clap on you (the floor cracks), a thread ties them and spins with them,
	// they drip tears, and at the end they come back together and the heart breaks for good
	class ClapS { public double x, y, t0, ta, te, tr; public bool cracked; }
	class ThreadS { public double tw, ta, te; }
	public static Hz BrokenHeart(double ts, double te, Tier T)
	{
		const double cx = W / 2, cy = H / 2, S = 85, RX = 300, RY = 190, wv = 0.9; double split = ts + 2 * SPB; int lastS = -1; bool ex = false; ClapS clap = null; ThreadS thread = null; var P = new Dictionary<int, double[]>();
		double[] orb(double t, double side) { double a = wv * (t - ts), k = clamp((t - split) / SPB, 0, 1); return new[] { cx + side * JS.cos(a) * RX * k, cy + side * JS.sin(a) * RY * k }; }
		double[] lerp(double[] p, double[] q, double k) => new[] { p[0] + (q[0] - p[0]) * k, p[1] + (q[1] - p[1]) * k };
		double[] pos(double t, double side)
		{
			var o = orb(t, side);
			if (t > te - SPB) return lerp(o, new[] { cx, cy }, JS.pow(clamp((t - (te - SPB)) / SPB, 0, 1), 2));
			if (clap != null && t >= clap.t0 && t < clap.tr)
			{
				var C = clap; var near = new[] { C.x + side * 120, C.y }; var hit = new[] { C.x + side * 34, C.y };
				if (t < C.ta) return lerp(o, near, 1 - JS.pow(1 - clamp((t - C.t0) / (C.ta - C.t0 - 0.08), 0, 1), 3));
				if (t < C.ta + 0.08) return lerp(near, hit, (t - C.ta) / 0.08);
				if (t < C.te) return hit;
				return lerp(hit, o, clamp((t - C.te) / (C.tr - C.te), 0, 1));
			}
			return o;
		}
		void shoot(int side, int n, double spr, double sp) { if (player == null) return; double x = P[side][0], y = P[side][1]; double a0 = JS.atan2(player.y - y, player.x - x); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * spr; hazards.Add(Tear(x, y, JS.cos(a) * sp, JS.sin(a) * sp)); } }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			P[-1] = pos(t, -1); P[1] = pos(t, 1);
			if (clap != null && !clap.cracked && t >= clap.ta) { clap.cracked = true; hazards.Add(Crack(clap.x, clap.y, t, t + SPB * 0.6, t + SPB * 1.1, 5, 260)); shake = Math.Max(shake, 10); rumble(150, 0.7, 0.5); }
			if (t >= te && !ex)
			{
				ex = true; for (int i = 0; i < 28; i++) { double a = i * TAU / 28; hazards.Add(Tear(cx, cy, JS.cos(a) * 215, JS.sin(a) * 215)); }
				for (int i = 0; i < 10; i++) { double a = i * TAU / 10; hazards.Add(ShardTri(cx, cy, JS.cos(a) * 280, JS.sin(a) * 280, 10)); }
				burst(cx, cy, PINK, 80, 560); burst(cx, cy, "#fff", 40, 460); shake = 28; flash = 0.75; rumble(600, 1, 1); hazards.Add(FadeHeart(t + 0.6, t + 5)); h.dead = true; return;
			}
			if (t < split + SPB) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s; int bar = (int)Math.Floor(s / 16.0), p = s % 16;
			if (t > te - 2 * SPB) return;
			bool busy = clap != null && t < clap.tr, thr = thread != null && t < thread.te;
			if (bar < 75)
			{
				if (p == 8 && bar % 2 == 0 && player != null) clap = new ClapS { x = clamp(player.x, 160, W - 160), y = clamp(player.y, 100, H - 100), t0 = t, ta = t + SPB * T.wn, te = t + SPB * (T.wn + 0.5), tr = t + SPB * (T.wn + 1.5) };
				if (p == 0 && !busy) { shoot(-1, 1, 0, T.sp); shoot(1, 1, 0, T.sp); }
				else if (p == 4 && !busy) shoot(bar % 2 != 0 ? 1 : -1, 3, 0.22, T.sp * 0.95);
			}
			else if (bar < 81)
			{
				if (p == 0 && bar % 2 == 1) thread = new ThreadS { tw = t, ta = t + SPB * T.wn, te = t + SPB * (T.wn + 4) };
				if (p % 4 == 0 && t >= split) { foreach (var side in new[] { -1, 1 }) hazards.Add(Tear(P[side][0], P[side][1] + 30, 0, T.sp * 0.8)); }
				if (p == 8 && bar % 2 == 0 && !thr) shoot(bar % 4 != 0 ? 1 : -1, 3, 0.25, T.sp * 0.9);
			}
			else
			{
				if (p == 0 && player != null && t < te - 5 * SPB) clap = new ClapS { x = clamp(player.x, 160, W - 160), y = clamp(player.y, 100, H - 100), t0 = t, ta = t + SPB * T.wn, te = t + SPB * (T.wn + 0.4), tr = t + SPB * (T.wn + 1.4) };
				if (p == 8) shoot(bar % 2 != 0 ? 1 : -1, 3, 0.22, T.sp);
			}
		};
		h.draw = t =>
		{
			double sc = clamp((t - ts) / (1.5 * SPB), 0, 1); bool act = t >= split;
			if (!act) { g.save(); g.translate(cx, cy); g.scale(sc * (1 + 0.08 * pulse), sc * (1 + 0.08 * pulse)); g.globalAlpha = 0.5; g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 30; heartPath(S); g.fill(); g.restore(); g.globalAlpha = 1; return; }
			var a1 = pos(t, -1); var a2 = pos(t, 1);
			if (thread != null && t >= thread.tw && t < thread.te + 0.2)
			{
				bool on = t >= thread.ta && t < thread.te; g.strokeStyle = on ? PINK : $"rgba(255,42,109,{str(0.25 + 0.4 * clamp((t - thread.tw) / (thread.ta - thread.tw), 0, 1))})"; g.lineWidth = on ? 8 * (1 + 0.15 * pulse) : 2; if (!on) g.setLineDash(new double[] { 8, 8 });
				if (on) { g.shadowColor = PINK; g.shadowBlur = 16; }
				g.beginPath(); g.moveTo(a1[0], a1[1]); g.lineTo(a2[0], a2[1]); g.stroke(); g.setLineDash(new double[0]); g.shadowBlur = 0;
			}
			if (clap != null && t >= clap.t0 && t < clap.ta)
			{
				g.strokeStyle = "rgba(255,42,109,0.5)"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 10 }); foreach (var q in new[] { a1, a2 }) { g.beginPath(); g.moveTo(q[0], q[1]); g.lineTo(clap.x, clap.y); g.stroke(); }
				g.setLineDash(new double[0]);
				g.strokeStyle = "rgba(255,42,109,0.9)"; g.lineWidth = 3; g.beginPath(); g.moveTo(clap.x - 14, clap.y - 14); g.lineTo(clap.x + 14, clap.y + 14); g.moveTo(clap.x + 14, clap.y - 14); g.lineTo(clap.x - 14, clap.y + 14); g.stroke();
			}
			foreach (var side in new double[] { -1, 1 }) { var q = side < 0 ? a1 : a2; double x = q[0], y = q[1]; g.save(); g.translate(x, y); g.scale(1 + 0.08 * pulse, 1 + 0.08 * pulse); g.shadowColor = PINK; g.shadowBlur = 30; g.fillStyle = PINK; halfPath(S, side); g.fill(); g.shadowBlur = 0; g.fillStyle = "#000"; circle(side * 0.4 * S, -0.2 * S, 0.12 * S); g.restore(); }
		};
		h.hit = (p, t) =>
		{
			if (t < split) return false;
			foreach (var side in new double[] { -1, 1 }) { var q = pos(t, side); double x = q[0], y = q[1]; if (hypot(p.x - x - side * 0.35 * S, p.y - y + 0.1 * S) < 0.52 * S + p.r - 2) return true; }
			if (thread != null && t >= thread.ta && t < thread.te && segDist(p.x, p.y, pos(t, -1), pos(t, 1)) < 4 + p.r) return true;
			return false;
		};
		return h;
	}

	/* "ليش ضيعت الوفا" (Gampet's pick). Every beat is tracked on its own (live recording). Scenes:
	   0 the grey heart -> 24 it breaks | 26 scattered hearts | 56 the tear curtain and the claps | 88 chain cages
	   112 the candles (the word candles) | 147 the big tear (the word tears) | 148 checkpoint -> the crying eyes
	   208 broken memories | 240 the heartbeat | 272 the flat line | 276 checkpoint -> the boss | 336 it breaks for good */
	public static List<Ev> buildFriend1()
	{
		var E = new List<Ev>(); var R = mulberry32(902);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 140, double b = W - 140) => a + R() * (b - a);
		double ry(double a = 120, double b = H - 120) => a + R() * (b - a);
		var TI = ezT(new[] { new Tier { sp = 265, wn = 1.6, gap = 200 }, new Tier { sp = 285, wn = 1.5, gap = 175 }, new Tier { sp = 300, wn = 1.45, gap = 160 } });
		void heart(double hit, double x, double y, Tier T, double s = 50, double life = 1.5, double ang = 0) => ev(hit - T.wn, () => hazards.Add(HeartBomb(x, y, s, B(hit - T.wn), B(hit), B(hit + life), T.sp * 0.85, ang)));
		void clap(double hit, string axis, Tier T, double? c = null, double hold = 0.5) { double cc = c ?? (axis == "V" ? rx(220, W - 220) : ry(150, H - 150)); ev(hit - T.wn, () => hazards.Add(Clap(axis, cc, T.gap, B(hit - T.wn), B(hit), B(hit + hold)))); }
		void clapP(double hit, string axis, Tier T, double hold = 0.5) => ev(hit - T.wn, () => { double c = axis == "V" ? clamp((player != null ? player.x : W / 2) + (R() * 2 - 1) * 200, 220, W - 220) : clamp((player != null ? player.y : H / 2) + (R() * 2 - 1) * 150, 150, H - 150); hazards.Add(Clap(axis, c, T.gap, B(hit - T.wn), B(hit), B(hit + hold))); });
		void cage(double hit, Tier T, double rad = 140)
		{
			double a0 = R() * TAU;
			ev(hit - T.wn, () => { double px = clamp(player != null ? player.x : W / 2, 200, W - 200), py = clamp(player != null ? player.y : H / 2, 170, H - 170); for (int k = 0; k < 3; k++) { double n = a0 + k * TAU / 3; hazards.Add(Chain(px + JS.cos(n) * rad, py + JS.sin(n) * rad, n + Math.PI / 2, B(hit - T.wn), B(hit), B(hit + 0.8), T.sp * 0.55)); } });
		}
		void chain(double hit, double x, double y, double ang, Tier T) => ev(hit - T.wn, () => hazards.Add(Chain(x, y, ang, B(hit - T.wn), B(hit), B(hit + 0.6), T.sp * 0.55)));
		void frame(double hit, double x, double y, Tier T, double w = 150, double h = 110) => ev(hit - 2, () => hazards.Add(Frame(x, y, w, h, B(hit - 2), B(hit), T.sp * 0.85)));
		void frameP(double hit, Tier T) { double jx = (R() * 2 - 1) * 120, jy = (R() * 2 - 1) * 90; ev(hit - 2, () => { double x = clamp((player != null ? player.x : W / 2) + jx, 100, W - 100), y = clamp((player != null ? player.y : H / 2) + jy, 80, H - 80); hazards.Add(Frame(x, y, 140, 100, B(hit - 2), B(hit), T.sp * 0.85)); }); }
		void crack(double hit, double x, double y, Tier T, int n = 5) => ev(hit - T.wn, () => hazards.Add(Crack(x, y, B(hit - T.wn), B(hit), B(hit + 0.6), n, 300)));
		void ecg(double b0, double b1, double y, Tier T, double amp = 110) { ev(b0 - T.wn, () => { double t0 = B(b0), t1 = B(b1); var sp = new List<double>(); for (double b = Math.Ceiling(b0); b < b1; b++) sp.Add(W * (B(b) - t0) / (t1 - t0)); hazards.Add(ECG(y, t0, t1, B(b1 + 1), sp.ToArray(), amp, B(b0 - T.wn))); }); }

		/* ---------- part 1 ---------- */
		var T = TI[0];
		E.Add(new Ev { b = 0, keep = true, f = () => hazards.Add(BgRain(B(0), B(340), 70)) });
		ev(2, () => hazards.Add(IntroHeart(B(2), B(24), T.sp * 0.85)));
		// 26-55 scattered hearts: diagonal -> the other diagonal -> a circle
		for (int i = 0; i < 4; i++) heart(28 + i * 2, 180 + i * 300, 120 + i * 160, T, 44, 1.5, Math.PI / 4);
		for (int i = 0; i < 4; i++) heart(37 + i * 2, W - 180 - i * 300, 120 + i * 160, T, 44, 1.5, -Math.PI / 4);
		for (int i = 0; i < 4; i++) { double a = -Math.PI / 2 + i * TAU / 4; heart(46 + i * 2, W / 2 + JS.cos(a) * 300, H / 2 + JS.sin(a) * 210, T, 44, 2, a + Math.PI / 2); }
		// 56-87 the tear curtain and the claps
		double t56 = B(56), t72 = B(72); ev(56, () => hazards.Add(TearCurtain(t56, t72, SPB * 2, t => W / 2 + JS.sin((t - t56) * 0.9) * 380, T.gap + 40, T.sp * 0.7, 85)));
		clap(79, "V", T, W * 0.35); heart(80, W / 2, H / 2, T, 60, 2);
		// 88-111 chain cages
		cage(90, T, 150); chain(94, W / 2, H * 0.3, 0, T); chain(95, W / 2, H * 0.7, 0, T); chain(102, W * 0.3, H / 2, Math.PI / 2, T); chain(103, W * 0.7, H / 2, Math.PI / 2, T); cage(106, T, 150); chain(109, W / 2, H / 2, Math.PI / 4, T); chain(110, W / 2, H / 2, -Math.PI / 4, T);
		// 112 the candles: they light one after another, throw sparks, then go out one by one and it gets darker
		{
			const int N = 8;
			for (int i = 0; i < N; i++)
			{
				double a = -Math.PI / 2 + i * TAU / N, x = W / 2 + JS.cos(a) * 450, y = H / 2 + 30 + JS.sin(a) * 240; var em = new List<double>(); em.Add(120 + i * 2);
				double tl = 112 + i * 0.5, to = 136 + i; ev(tl - 0.5, () => hazards.Add(Candle(x, y, B(tl), B(to), B(147), em.Select(B).ToArray(), T.sp * 0.75)));
			}
		}
		clap(131, "H", T, H / 2); heart(140, W / 2, H / 2 - 20, T, 55, 2, Math.PI / 2);
		// 147 "the tears": a big tear falls and splashes
		ev(145, () => hazards.Add(BigTear(W / 2, H / 2, B(145), B(147), T.sp * 0.85)));
		/* ---------- part 2 ---------- */
		T = TI[1];
		// 148-207 the crying eyes on the drum
		var EX = new[] { W * 0.14, W * 0.38, W * 0.62, W * 0.86 };
		{
			ev(148, () =>
			{
				var cr = new[] { new List<double>(), new List<double>(), new List<double>(), new List<double>() };
				for (int b = 150; b < 166; b++) { cr[(b - 150) % 4].Add(b); if (b % 2 != 0) cr[3 - (b - 150) % 4].Add(b + 0.5); }
				for (int b = 166; b < 182; b++) { int m = (b - 166) % 4; if (m == 0) { cr[0].Add(b); cr[3].Add(b); } else if (m == 2) { cr[1].Add(b); cr[2].Add(b); } else cr[m == 1 ? 2 : 1].Add(b); }
				for (int b = 182; b < 200; b++) { if (b % 4 == 0) for (int i = 0; i < 4; i++) if (i != (b / 4) % 4) cr[i].Add(b); if (b % 4 == 2) cr[(b / 2) % 4].Add(b + 0.5); }
				for (int i = 0; i < 4; i++) hazards.Add(CryEye(EX[i], B(149), B(203), cr[i].Select(B).ToArray(), T.sp * 0.95));
			});
		}
		foreach (var b in new double[] { 155, 163, 167, 171, 179, 183, 187, 195, 199 }) clapP(b, b % 8 == 3 ? "H" : "V", T, 0.45);
		foreach (var b in new double[] { 154, 158, 170, 174, 186, 190 }) heart(b, rx(), ry(220, H - 120), T, 46, 1.5, b % 8 == 2 ? 0 : Math.PI / 2);
		ev(201, () => { });
		// 208 broken memories
		frame(208, W / 2, H / 2, T, 260, 180);
		for (int k = 0; k < 7; k++) { double b = 212 + k * 4; frameP(b, T); crack(b + 2, rx(), ry(), T, 4); if (k % 2 != 0) clapP(b + 3, k % 4 == 1 ? "V" : "H", T, 0.4); else frameP(b + 2.5, T); }
		// 240 the heartbeat: heartbeat lines drawn on the rhythm
		ecg(240, 248, H * 0.3, T); heart(244, W / 2, H * 0.7, T, 50, 1.5); ecg(248, 256, H * 0.7, T); heart(252, W / 2, H * 0.3, T, 50, 1.5); ecg(256, 264, H * 0.5, T, 140); frameP(258, T); frameP(262, T);
		ecg(264, 270, H * 0.3, T, 90); ecg(266, 272, H * 0.7, T, 90); clapP(250, "V", T, 0.4); clapP(261, "H", T, 0.4);
		// 272 the flat line... then the heart comes back
		ev(270, () => hazards.Add(ECG(H / 2, B(272), B(275), B(275.6), new double[0], 0, B(270))));
		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		ev(276, () => hazards.Add(BrokenHeart(B(276), B(336), TT)));
		foreach (var b in new double[] { 284, 290, 296, 300, 306, 312, 316, 322 }) clapP(b, b % 8 == 4 ? "H" : "V", T, 0.4);
		{ double ta = B(302), tb = B(322); ev(302, () => hazards.Add(TearCurtain(ta, tb, SPB * 2, t => W / 2 + JS.sin((t - ta) * 1.1) * 400, T.gap + 70, T.sp * 0.65, 95))); }
		foreach (var b in new double[] { 290, 312, 326, 330 }) crack(b, rx(), ry(), T, 4);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
