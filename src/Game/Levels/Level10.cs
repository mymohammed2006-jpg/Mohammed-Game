using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 3857-4468: the level 10 shapes and buildLevel10 (Je veux, Paris)
// every name starts with L10 so it does not clash with other levels
public static partial class G
{
	static double L10ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	// there and back: 0 off screen, 1 in place. comes in from a to b, stays, goes back from c to d
	static double L10go(double t, double a, double b, double c, double d) => t < b ? L10ease((t - a) / (b - a)) : t < c ? 1 : 1 - L10ease((t - c) / (d - c));
	static void L10dash(bool on) { g.setLineDash(on ? new double[] { 12, 9 } : new double[0]); }
	// a pigeon: body and two flapping wings
	static void L10bird(double x, double y, double a, double fl, double s, string col)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = col; g.beginPath(); g.ellipse(0, 0, s, s * 0.48, 0, 0, TAU); g.fill();
		double w = s * (0.35 + 0.65 * Math.Abs(fl)); g.beginPath(); g.moveTo(-s * 0.2, 0); g.lineTo(-s * 0.75, -w * 1.25); g.lineTo(s * 0.35, -s * 0.1); g.closePath(); g.fill(); g.beginPath(); g.moveTo(-s * 0.2, 0); g.lineTo(-s * 0.75, w * 1.25); g.lineTo(s * 0.35, s * 0.1); g.closePath(); g.fill();
		circle(s * 0.95, 0, s * 0.38); g.restore();
	}
	/* ---- the base: grey Paris (decoration, harmless) ---- */
	// md 0 the Ritz (columns and windows), 1 Montmartre (roofs, chimneys and a dome), 2 the night
	static Hz L10Paris(double t0, double t1, int md)
	{
		var roofs = new List<double[]>(); double x = -20; var R = mulberry32(1010 + md);
		while (x < W + 40) { double w = 70 + R() * 90, h = 50 + R() * 80; roofs.Add(new[] { x, w, h, R() < 0.6 ? (R() * 0.6 + 0.2) : -1 }); x += w; }
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.6) hz.dead = true; };
		hz.draw = t =>
		{
			double a = min(1, (t - t0) / 0.8, (t1 + 0.6 - t) / 0.6); if (a <= 0) return; g.globalAlpha = a;
			if (md == 2) { g.fillStyle = "rgba(8,10,30,0.55)"; g.fillRect(0, 0, W, H); g.fillStyle = GREY + "0.18)"; circle(1080, 110, 46); g.fillStyle = "rgba(8,10,30,0.9)"; circle(1100, 98, 42); }
			if (md == 0)
			{
				g.fillStyle = GREY + "0.05)"; for (int i = 0; i < 9; i++) { double cx = 70 + i * 142; g.fillRect(cx - 16, 60, 32, H - 60); }
				for (int i = 0; i < 8; i++) for (int j = 0; j < 3; j++) { double cx = 141 + i * 142, cy = 150 + j * 170; g.beginPath(); g.arc(cx, cy, 34, Math.PI, 0); g.lineTo(cx + 34, cy + 70); g.lineTo(cx - 34, cy + 70); g.closePath(); g.fill(); }
				g.fillStyle = GREY + "0.08)"; g.fillRect(0, 40, W, 14); g.font = $"44px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = GREY + "0.12)"; g.fillText("HÔTEL RITZ", W / 2, 26); double lit = 1 - (((BT(t) % 1) + 1) % 1); foreach (var cx in L10CH) L10chand(cx, L10CHY, lit);
			}
			else
			{
				double bse = H; g.fillStyle = GREY + (md == 2 ? "0.09)" : "0.07)"); g.beginPath(); g.moveTo(-20, bse); foreach (var rf in roofs) { double rx = rf[0], w = rf[1], h = rf[2]; g.lineTo(rx, bse - h); g.lineTo(rx + w * 0.5, bse - h - 26); g.lineTo(rx + w, bse - h); }
				g.lineTo(W + 40, bse); g.closePath(); g.fill();
				foreach (var rf in roofs) if (rf[3] > 0) { g.fillRect(rf[0] + rf[1] * rf[3], bse - rf[2] - 44, 12, 30); }
				if (md == 1)
				{
					g.fillStyle = GREY + "0.06)"; double cx = W * 0.72, cy = bse - 150; circle(cx, cy, 70); circle(cx - 110, cy + 30, 40); circle(cx + 110, cy + 30, 40); g.fillRect(cx - 150, cy + 20, 300, 140); g.fillRect(cx - 6, cy - 110, 12, 50); double bt = BT(t); g.lineWidth = 2;
					for (int s = 0; s < 3; s++)
					{
						double xa = -20 + s * 440, xb = xa + 460, ya = 24; Func<double, double> cy2 = u => ya + 44 * (1 - JS.pow(2 * u - 1, 2)); g.strokeStyle = GREY + "0.3)"; g.beginPath(); for (int q = 0; q <= 20; q++) { double u = q / 20.0; if (q != 0) g.lineTo(xa + (xb - xa) * u, cy2(u)); else g.moveTo(xa, cy2(0)); }
						g.stroke();
						for (int q = 1; q < 20; q += 2) { double u = q / 20.0; bool on = ((q >> 1) + s + Math.Floor(bt)) % 2 == 0; g.fillStyle = on ? "rgba(255,255,255,0.34)" : GREY + "0.16)"; circle(xa + (xb - xa) * u, cy2(u) + 7, on ? 5.5 : 4); }
					}
					for (int i = 1; i < roofs.Count; i += 3) { double rx = roofs[i][0], w = roofs[i][1], h = roofs[i][2], hop = Math.Max(0, JS.sin(Math.PI * (((bt + i * 0.5) % 2 + 2) % 2))); L10bird(rx + w * 0.5, bse - h - 34 - hop * 16, 0, hop, 9, GREY + "0.35)"); }
				}
				if (md == 2) { g.fillStyle = "rgba(255,255,255,0.10)"; foreach (var rf in roofs) { for (int k = 0; k < 2; k++) g.fillRect(rf[0] + 12 + k * 30, bse - rf[2] + 20, 9, 12); } }
			}
			g.globalAlpha = 1;
		};
		return hz;
	}
	// a far grey Eiffel tower (decoration)
	static Hz L10FarTower(double x, double t0, double t1)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.6) hz.dead = true; };
		hz.draw = t => { double a = min(1, (t - t0) / 0.8, (t1 + 0.6 - t) / 0.6); if (a <= 0) return; g.globalAlpha = a * 0.55; L10towerShape(x, H - 40, 0.55, 0, GREY + "0.35)", null, 0); g.globalAlpha = 1; };
		return hz;
	}
	// the tower shape: base (bx,by), size sc, lean ang, line colour col, lights colour lit (optional)
	static void L10towerShape(double bx, double by, double sc, double ang, string col, string lit, double lk)
	{
		g.save(); g.translate(bx, by); g.rotate(ang); g.scale(sc, sc); g.strokeStyle = col; g.lineWidth = 5; g.lineJoin = "round";
		double[][] P = { new double[] { 260, 0 }, new double[] { 150, -160 }, new double[] { 70, -320 }, new double[] { 26, -470 }, new double[] { 0, -540 } };
		g.beginPath(); g.moveTo(-260, 0); for (int i = 1; i < P.Length; i++) g.lineTo(-P[i][0], P[i][1]); g.moveTo(260, 0); for (int i = 1; i < P.Length; i++) g.lineTo(P[i][0], P[i][1]); g.stroke();
		g.lineWidth = 4; g.beginPath(); g.arc(0, 0, 150, Math.PI, 0); g.stroke();
		foreach (var k in new[] { 1, 2, 3 }) { double x = P[k][0], y = P[k][1]; g.lineWidth = k == 1 ? 8 : 6; g.beginPath(); g.moveTo(-x - 14, y); g.lineTo(x + 14, y); g.stroke(); }
		g.lineWidth = 2;
		for (int k = 0; k < 3; k++)
		{
			double x0 = P[k][0], y0 = P[k][1], x1 = P[k + 1][0], y1 = P[k + 1][1]; int n = k == 0 ? 3 : k == 1 ? 4 : 5;
			for (int i = 0; i < n; i++)
			{
				double u0 = (double)i / n, u1 = (double)(i + 1) / n;
				foreach (var sd in new double[] { -1, 1 }) { double xa = sd * (x0 + (x1 - x0) * u0), ya = y0 + (y1 - y0) * u0, xb = sd * (x0 + (x1 - x0) * u1), yb = y0 + (y1 - y0) * u1, xi = sd * ((x0 + (x1 - x0) * u0) * 0.35), xj = sd * ((x0 + (x1 - x0) * u1) * 0.35); g.beginPath(); g.moveTo(xa, ya); g.lineTo(xj, yb); g.moveTo(xi, ya); g.lineTo(xb, yb); g.stroke(); }
			}
		}
		if (lit != null)
		{
			g.fillStyle = lit;
			for (int k = 0; k < 4; k++)
			{
				double x0 = P[k][0], y0 = P[k][1], x1 = P[k + 1][0], y1 = P[k + 1][1];
				for (int i = 0; i < 6; i++) { double u = i / 6.0; bool on = ((i + k * 2 + Math.Floor(double.IsNaN(lk) ? 0 : lk)) % 3) == 0; if (!on) continue; foreach (var sd in new double[] { -1, 1 }) circle(sd * (x0 + (x1 - x0) * u), y0 + (y1 - y0) * u, 6); }
			}
		}
		g.restore();
	}
	// a decoration word
	static Hz L10Word(string s, double x, double y, double size, double t0, double t1, string col)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < t0) return; double e = t - t0, a = min(1, e / 0.08, (t1 - t) / 0.3), sc = 1 + 0.35 * JS.exp(-e * 10); g.globalAlpha = Math.Max(0, a); g.save(); g.translate(x, y); g.scale(sc, sc); g.font = $"{str(size)}px {TFONT}"; g.fillStyle = col ?? GREY + "0.5)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, 0, 0); g.restore(); g.globalAlpha = 1;
		};
		return hz;
	}
	/* ---- papala papapala: paving stones jump in order ---- */
	static Hz L10Pave(List<double[]> pts, double[] ts, double tw, double r = 30)
	{
		double te = ts[ts.Length - 1] + 0.4;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			for (int i = 0; i < pts.Count; i++)
			{
				double x = pts[i][0], y = pts[i][1], ti = ts[i], e = t - ti;
				if (e < 0)
				{
					double k = clamp((t - tw) / (ti - tw), 0, 1); bool nx = i == 0 || t >= ts[i - 1]; g.strokeStyle = $"rgba(255,42,109,{str((nx ? 0.35 : 0.15) + 0.5 * k * (nx ? 1 : 0.4))})"; g.lineWidth = 3; L10dash(true); g.strokeRect(x - r, y - r, 2 * r, 2 * r); L10dash(false);
					g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(x - r * k, y - r * k, 2 * r * k, 2 * r * k); continue;
				}
				if (e > 0.4) continue; double up = e < 0.08 ? e / 0.08 : e < 0.3 ? 1 : 1 - (e - 0.3) / 0.1, s = r * (0.7 + 0.4 * up); g.save(); g.shadowColor = PINK; g.shadowBlur = 18 * up; g.fillStyle = e < 0.06 ? "#fff" : PINK; g.globalAlpha = 0.4 + 0.6 * up;
				g.beginPath(); g.roundRect(x - s, y - s - 8 * up, 2 * s, 2 * s, 9); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.4)"; g.fillRect(x - s + 6, y - s - 8 * up + 6, 2 * s - 12, 6); g.restore();
			}
		};
		hz.hit = (p, t) => { for (int i = 0; i < pts.Count; i++) { double e = t - ts[i]; if (e < 0.02 || e > 0.32) continue; double x = pts[i][0], y = pts[i][1]; if (Math.Abs(p.x - x) < r + p.r - 3 && Math.Abs(p.y - y) < r + p.r - 3) return true; } return false; };
		return hz;
	}
	/* ---- phase 1: the Ritz (there and back) ---- */
	// a red carpet rolls out across the screen and rolls back. dir 1 from the left
	static Hz L10Carpet(double y, double h, double dir, double tw, double ta, double tf, double tb, double te)
	{
		Func<double, double> X = t => W * L10go(t, ta, tf, tb, te); double rr = h / 2 + 8;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(0, y - h / 2, W, h); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.strokeRect(1, y - h / 2, W - 2, h); L10dash(false);
				g.fillStyle = $"rgba(255,42,109,{str(0.5 + 0.5 * k)})"; circle(dir > 0 ? 14 : W - 14, y, rr * 0.6 * (0.6 + 0.4 * k)); return;
			}
			double x = X(t), x0 = dir > 0 ? 0 : W - x; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(x0, y - h / 2, x, h); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.32)"; g.fillRect(x0, y - h / 2 + 8, x, 4); g.fillRect(x0, y + h / 2 - 12, x, 4); for (double q = 24; q < x; q += 60) { double qx = dir > 0 ? q : W - q; g.fillRect(qx - 3, y - 6, 6, 12); }
			double ex = dir > 0 ? x : W - x; g.fillStyle = "#ffd1de"; circle(ex, y, rr); g.fillStyle = PINK; circle(ex, y, rr * 0.7); g.strokeStyle = "rgba(0,0,0,0.25)"; g.lineWidth = 2; g.beginPath(); g.arc(ex, y, rr * 0.4, 0, TAU); g.stroke(); g.restore();
		};
		hz.hit = (p, t) =>
		{
			if (t < ta) return false; double x = X(t); if (x < 2) return false; double ex = dir > 0 ? x : W - x; if (hypot(p.x - ex, p.y - y) < rr + p.r - 2) return true;
			if (Math.Abs(p.y - y) > h / 2 + p.r - 2) return false; return dir > 0 ? p.x < x + p.r - 2 : p.x > W - x - p.r + 2;
		};
		return hz;
	}
	// a hotel luggage trolley: slides in from the edge to xh (its inner end), stops, and goes back
	static Hz L10Trolley(double y, double dir, double xh, double len, double tw, double ta, double th, double tb, double te, double hh = 150)
	{
		double o = dir > 0 ? -len - 30 : W + len + 30; Func<double, double> X = t => o + (xh - (dir > 0 ? len : -len) - o) * L10go(t, ta, th, tb, te);
		// X = the trolley's outer end
		Func<double, double[]> rect = t => { double xo = X(t); return dir > 0 ? new[] { xo, xo + len } : new[] { xo - len, xo }; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), a_ = dir > 0 ? 0 : xh, b_ = dir > 0 ? xh : W; g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(a_, y - hh / 2, b_ - a_, hh); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.strokeRect(a_ + 1, y - hh / 2, b_ - a_ - 2, hh); L10dash(false);
				g.fillStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; double ex = dir > 0 ? 12 : W - 12; g.beginPath(); g.moveTo(ex, y - 18); g.lineTo(ex + dir * 22, y); g.lineTo(ex, y + 18); g.fill(); return;
			}
			var ab = rect(t); double a = ab[0], b = ab[1]; g.save(); g.strokeStyle = GREY + "0.75)"; g.lineWidth = 6; g.beginPath(); g.moveTo(a, y + hh / 2 - 10); g.lineTo(b, y + hh / 2 - 10); g.stroke(); double hx = dir > 0 ? a + 10 : b - 10; g.beginPath(); g.moveTo(hx, y + hh / 2 - 10); g.lineTo(hx, y - hh / 2 - 12); g.stroke();
			g.fillStyle = GREY + "0.8)"; for (double q = a + 30; q < b - 10; q += 110) circle(q, y + hh / 2 - 2, 9);
			g.shadowColor = PINK; g.shadowBlur = 14; double[][] S = { new[] { 0.04, 0.5, 0.46 }, new[] { 0.52, 0.44, 0.62 }, new[] { 0.08, 0.42, 0.38 }, new[] { 0.55, 0.4, 0.3 } };
			foreach (var sv in S) { double u = sv[0], w = sv[1], hv = sv[2]; double x0 = a + 18 + (b - a - 30) * u, ww = (b - a - 30) * w; g.fillStyle = PINK; double yy = hv > 0.45 ? y + hh / 2 - 16 - hh * 0.55 : y + hh / 2 - 16 - hh * 0.55 - hh * 0.4; double hgt = hv > 0.45 ? hh * 0.55 : hh * 0.4; g.fillRect(x0, yy, ww, hgt - 2); }
			g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.35)"; foreach (var sv in S) { double u = sv[0], w = sv[1]; double x0 = a + 18 + (b - a - 30) * u, ww = (b - a - 30) * w; g.fillRect(x0 + ww * 0.42, y - hh / 2 + 4, ww * 0.16, 6); }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; var ab = rect(t); double a = ab[0], b = ab[1]; return p.x > a - p.r + 3 && p.x < b + p.r - 3 && p.y > y - hh / 2 - p.r + 3 && p.y < y + hh / 2 + p.r - 3; };
		return hz;
	}
	// a perfume bottle on the edge (decoration): puffs with every spray, and after the refusal it is thrown out
	static Hz L10Bottle(double x, double y, double ang, double tw, List<double> puffs, double te, double tout)
	{
		double c = JS.cos(ang), s = JS.sin(ang); Func<double, double[]> bp = t => { double k = t < te ? 0 : L10ease((t - te) / (tout - te)); return new[] { x - c * 240 * k, y - s * 240 * k }; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > tout) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < tw) return; var bxy = bp(t); double bx = bxy[0], by = bxy[1]; double sq = 0; foreach (var q in puffs) { double e = t - q; if (e > -0.15 && e < 0.2) sq = Math.Max(sq, 1 - Math.Abs(e) / 0.2); }
			g.save(); g.globalAlpha = Math.Min(1, (t - tw) / 0.3); g.translate(bx, by); g.rotate(ang); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; g.fillRect(-46, -22, 40, 44); g.shadowBlur = 0; g.fillStyle = GREY + "0.9)"; g.fillRect(-6, -8, 16, 16);
			g.fillStyle = GREY + "0.7)"; circle(-58 - 4 * sq, 0, 12 * (1 - 0.35 * sq)); g.fillStyle = "rgba(255,255,255,0.6)"; g.fillRect(-40, -16, 6, 32);
			g.fillStyle = "rgba(0,0,0,0.5)"; g.font = $"bold 13px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText("N°5", -26, 1); g.restore();
		};
		return hz;
	}
	// a perfume cloud: comes out of the bottle, travels and grows, stays a little and fades
	static Hz L10Mist(double x, double y, double vx, double vy, double r0, double r1, double tw, double ta, double te)
	{
		Func<double, double[]> P = t => { double e = Math.Max(0, t - ta), d = 1 - JS.exp(-e * 1.6); return new[] { x + vx * d / 1.6, y + vy * d / 1.6 }; }; Func<double, double> Rr = t => r0 + (r1 - r0) * L10ease((t - ta) / (0.7));
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.35) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); var exy = P(ta + 3); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.arc(exy[0], exy[1], r1, 0, TAU); g.stroke(); L10dash(false);
				g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; for (int i = 1; i < 10; i++) { var pp = P(ta + 3.0 * i / 10); circle(pp[0], pp[1], 3); }
				return;
			}
			var cxy = P(t); double cx = cxy[0], cy = cxy[1], r = Rr(t), a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.35); g.save(); g.globalAlpha = a * (0.75 + 0.15 * pulse);
			for (int i = 0; i < 6; i++) { double an = i * TAU / 6 + t * 0.8; g.fillStyle = "rgba(255,42,109,0.55)"; circle(cx + JS.cos(an) * r * 0.42, cy + JS.sin(an) * r * 0.42, r * 0.62); }
			g.fillStyle = PINK; circle(cx, cy, r * 0.78); g.fillStyle = "rgba(255,255,255,0.35)"; circle(cx - r * 0.25, cy - r * 0.25, r * 0.22); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta + 0.05 || t >= te) return false; var cxy = P(t); return hypot(p.x - cxy[0], p.y - cxy[1]) < Rr(t) * 0.85 + p.r - 3; };
		return hz;
	}
	// a long limousine: comes in getting longer and longer, stops and opens its doors, and goes back
	static Hz L10Limo(double y, double dir, double tw, double ta, double ts, double td, double tb, double te, double[][] doors, double bh = 70, double dh = 86)
	{
		Func<double, double> F = t => (dir > 0 ? -60 + (W + 140) * L10go(t, ta, ts, tb, te) : W + 60 - (W + 140) * L10go(t, ta, ts, tb, te));
		Func<double, double> dk = t => t < td ? 0 : t < tb ? Math.Min(1, (t - td) / 0.15) : Math.Max(0, 1 - (t - tb) / 0.15);
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(0, y - bh / 2, W, bh); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.strokeRect(1, y - bh / 2, W - 2, bh); L10dash(false);
				g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; double ex = dir > 0 ? 6 : W - 6; circle(ex, y - bh / 4, 6); circle(ex, y + bh / 4, 6); return;
			}
			double f = F(t), a = dir > 0 ? -40 : f, b = dir > 0 ? f : W + 40, dd = dk(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(a, y - bh / 2, b - a, bh);
			foreach (var dr in doors) { double dx = dr[0]; bool up = dr[1] != 0; if (dd <= 0) break; if ((dir > 0 && dx > f - 30) || (dir < 0 && dx < f + 30)) continue; double yy = up ? y - bh / 2 - dh * dd : y + bh / 2; g.fillRect(dx - 30, yy, 60, dh * dd); }
			g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.35)"; for (double q = Math.Max(a, 0) + 30; q < Math.Min(b, W) - 20; q += 95) g.fillRect(q, y - bh / 2 + 8, 64, bh * 0.32);
			g.fillStyle = "rgba(255,255,255,0.9)"; double fx_ = dir > 0 ? f - 8 : f + 8; circle(fx_, y - bh / 4, 7); circle(fx_, y + bh / 4, 7); g.fillStyle = GREY + "0.85)"; for (double q = Math.Max(a, 0) + 50; q < Math.Min(b, W); q += 190) { circle(q, y - bh / 2 - 2, 7); circle(q, y + bh / 2 + 2, 7); }
			g.restore();
		};
		hz.hit = (p, t) =>
		{
			if (t < ta) return false; double f = F(t); Func<double, bool> inX = x => dir > 0 ? x < f + p.r - 3 : x > f - p.r + 3; if (!inX(p.x)) return false;
			if (Math.Abs(p.y - y) < bh / 2 + p.r - 3) return true; double dd = dk(t); if (dd <= 0) return false;
			foreach (var dr in doors) { double dx = dr[0]; bool up = dr[1] != 0; if (Math.Abs(p.x - dx) > 30 + p.r - 3) continue; if (!inX(dx)) continue; if (up ? (p.y < y - bh / 2 + 3 && p.y > y - bh / 2 - dh * dd - p.r + 3) : (p.y > y + bh / 2 - 3 && p.y < y + bh / 2 + dh * dd + p.r - 3)) return true; }
			return false;
		};
		return hz;
	}
	// a grey butler (decoration) who throws a tray
	static Hz L10Butler(double x, double y, double t0, double t1, List<double> throws)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.6) hz.dead = true; };
		hz.draw = t =>
		{
			double a = min(1, (t - t0) / 0.5, (t1 + 0.6 - t) / 0.6); if (a <= 0) return; double arm = 0; foreach (var s in throws) { double e = t - s; if (e > -0.3 && e < 0.3) arm = Math.Max(arm, 1 - Math.Abs(e) / 0.3); }
			g.save(); g.globalAlpha = a; g.translate(x, y); g.fillStyle = GREY + "0.6)"; g.strokeStyle = GREY + "0.6)"; g.lineWidth = 5; g.lineCap = "round";
			g.beginPath(); g.moveTo(0, -26); g.lineTo(0, 14); g.moveTo(0, 14); g.lineTo(-10, 40); g.moveTo(0, 14); g.lineTo(10, 40); g.moveTo(0, -18); g.lineTo(-20, -4); g.moveTo(0, -18); g.lineTo(18, -30 - 26 * arm); g.stroke();
			circle(0, -40, 13); g.fillStyle = GREY + "0.85)"; g.beginPath(); g.moveTo(-16, -14); g.lineTo(0, -4); g.lineTo(16, -14); g.lineTo(16, -26); g.lineTo(-16, -26); g.closePath(); g.fill(); g.fillStyle = "#000"; g.fillRect(-6, -28, 12, 6); g.lineCap = "butt"; g.restore(); g.globalAlpha = 1;
		};
		return hz;
	}
	// a silver tray: flies out in an (elliptic) arc from the butler and comes back to him
	static Hz L10Tray(double bx, double by, double ux, double uy, double ax, double ay, double tw, double t0, double t1, double r = 26)
	{
		double vx = -uy, vy = ux; Func<double, double[]> pos = t => { double s = clamp((t - t0) / (t1 - t0), 0, 1), q = TAU * s, c = -JS.cos(q) * ax + ax, d = JS.sin(q) * ay; return new[] { bx + ux * c + vx * d, by + uy * c + vy * d }; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < t0) { double k = clamp((t - tw) / (t0 - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * k)})"; for (int i = 1; i < 28; i++) { if (i / 28.0 > k * 1.1 + 0.1) break; var pp = pos(t0 + (t1 - t0) * i / 28); g.fillRect(pp[0] - 2.5, pp[1] - 2.5, 5, 5); } return; }
			var xy = pos(t); g.save(); g.translate(xy[0], xy[1]); g.rotate(t * 9); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, r, r * 0.82, 0, 0, TAU); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = 3; g.beginPath(); g.ellipse(0, 0, r * 0.68, r * 0.55, 0, 0, TAU); g.stroke(); g.fillStyle = "rgba(255,255,255,0.5)"; g.beginPath(); g.ellipse(-r * 0.3, -r * 0.3, r * 0.18, r * 0.1, -0.6, 0, TAU); g.fill(); g.restore();
		};
		hz.hit = (p, t) => { if (t < t0 + 0.05 || t > t1 - 0.05) return false; var xy = pos(t); return hypot(p.x - xy[0], p.y - xy[1]) < r * 0.88 + p.r - 3; };
		return hz;
	}
	// the palace gate: two iron halves close from the sides and leave an arched door in the middle (ax,ay the arch centre, ar its radius)
	static Hz L10Gate(double ax, double ay, double ar, double tw, double ta, double tc, double to, double te)
	{
		Func<double, double> L = t => (W / 2) * L10go(t, ta, tc, to, te);
		Func<double, double, double, bool> inDoor = (x, y, m) => Math.Abs(x - ax) < ar - m && y > ay - m ? true : hypot(x - ax, y - ay) < ar - m;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,255,255,{str(0.35 + 0.5 * k)})"; g.lineWidth = 3; L10dash(true); g.beginPath(); g.arc(ax, ay, ar, Math.PI, 0); g.lineTo(ax + ar, H); g.moveTo(ax - ar, H); g.lineTo(ax - ar, ay); g.stroke(); L10dash(false);
				g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.fillRect(0, 0, 10 + 10 * k, H); g.fillRect(W - 10 - 10 * k, 0, 10 + 10 * k, H); return;
			}
			double l = L(t); g.save(); g.beginPath(); g.rect(0, 0, l, H); g.rect(W - l, 0, l, H); g.clip(); g.beginPath(); g.rect(0, 0, W, H); g.arc(ax, ay, ar, 0, Math.PI, true); g.lineTo(ax - ar, H); g.lineTo(ax + ar, H); g.lineTo(ax + ar, ay); g.closePath();
			g.fillStyle = "rgba(255,42,109,0.55)"; g.fill("evenodd"); g.clip("evenodd"); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			for (double q = 0; q < l; q += 46) { g.fillRect(q + 8, 0, 12, H); g.fillRect(W - q - 20, 0, 12, H); }
			g.fillRect(0, 90, W, 12); g.fillRect(0, H - 110, W, 12); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 3; for (double q = 0; q < l; q += 92) { foreach (var sx in new[] { q + 37, W - q - 37 }) { g.beginPath(); g.arc(sx, 96, 14, 0, TAU); g.stroke(); g.beginPath(); g.arc(sx, H - 104, 14, 0, TAU); g.stroke(); } }
			g.restore();
			g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 3; g.beginPath(); g.arc(ax, ay, ar, Math.PI, 0); g.stroke();
		};
		hz.hit = (p, t) => { if (t < ta) return false; double l = L(t); if (p.x > l + p.r - 3 && p.x < W - l - p.r + 3) return false; return !inDoor(p.x, p.y, p.r - 3); };
		return hz;
	}
	// a pink Eiffel tower lying on its side: comes in from the right (its base at X) and is pushed back. the arch between its legs is empty
	static Hz L10TowerSide(double yc, double tipX, double hb, double tw, double ta, double th, double tb, double te)
	{
		double LT = W + 40 - tipX; Func<double, double> X = t => W + LT + 20 + (tipX - W - 20) * L10go(t, ta, th, tb, te);
		Func<double, double, double, double, bool> inTri = (px, py, x, m) => { double u = (x - px) / LT; if (u < 0 || u > 1) return false; return Math.Abs(py - yc) < hb * (1 - u) - m; }; double ar = hb * 0.45;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1), x_ = tipX + LT; g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.1 * k)})"; g.beginPath(); g.moveTo(tipX, yc); g.lineTo(x_, yc - hb); g.lineTo(x_, yc + hb); g.closePath(); g.fill(); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.stroke(); L10dash(false); return; }
			double x = X(t); g.save(); g.translate(x, yc); g.rotate(-Math.PI / 2); g.beginPath(); g.moveTo(-hb, 0); g.lineTo(0, LT); g.lineTo(hb, 0); g.closePath(); g.arc(0, 0, ar, 0, Math.PI, true); g.closePath();
			g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.fill("evenodd"); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.4)"; g.lineWidth = 3;
			foreach (var u in new[] { 0.3, 0.58, 0.85 }) { double w = hb * (1 - u); g.beginPath(); g.moveTo(-w, LT * u); g.lineTo(w, LT * u); g.stroke(); }
			g.lineWidth = 2; for (int i = 0; i < 8; i++) { double u0 = i / 8.0, u1 = (i + 1) / 8.0; g.beginPath(); g.moveTo(-hb * (1 - u0), LT * u0); g.lineTo(hb * (1 - u1) * 0.3, LT * u1); g.moveTo(hb * (1 - u0), LT * u0); g.lineTo(-hb * (1 - u1) * 0.3, LT * u1); g.stroke(); }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; double x = X(t); if (hypot(p.x - x, p.y - yc) < ar - p.r + 3 && p.x < x) return false; return inTri(p.x, p.y, x, -(p.r - 3)); };
		return hz;
	}
	/* ---- phase 2: Montmartre square (the flock) ---- */
	public class L10FO { public Action<double, double> gh; public bool early; }
	// a pigeon flock: pos(i,t) gives pigeon i's place (or null). hurts between t0 and t1. gh(t) draws the warning
	static Hz L10Flock(int n, Func<int, double, double[]> pos, double tw, double t0, double t1, L10FO o = null)
	{
		o ??= new L10FO(); double r = 10, s = 13;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < t0) { if (o.gh != null) o.gh(t, clamp((t - tw) / (t0 - tw), 0, 1)); if (!o.early) return; }
			bool live = t >= t0;
			for (int i = 0; i < n; i++)
			{
				var q = pos(i, t); if (q == null) continue; var q2 = pos(i, t + 0.02) ?? q; double a = JS.atan2(q2[1] - q[1], q2[0] - q[0]); if (hypot(q2[0] - q[0], q2[1] - q[1]) < 0.05) a = q.Length > 2 && !double.IsNaN(q[2]) ? q[2] : 0;
				L10bird(q[0], q[1], a, JS.sin(t * 16 + i * 1.7), s, live ? PINK : "rgba(255,42,109,0.3)");
			}
		};
		hz.hit = (p, t) => { if (t < t0) return false; for (int i = 0; i < n; i++) { var q = pos(i, t); if (q == null) continue; double dx = p.x - q[0], dy = p.y - q[1], rr = r + p.r - 2; if (dx * dx + dy * dy < rr * rr) return true; } return false; };
		return hz;
	}
	// the heart shape (about -1 to 1)
	static double[] L10heart(double th) => new[] { JS.pow(JS.sin(th), 3), -(13 * JS.cos(th) - 5 * JS.cos(2 * th) - 2 * JS.cos(3 * th) - JS.cos(4 * th)) / 16 + 0.15 };
	// a camera flash: a photo frame aims at you, then a "click" hits inside it
	static Hz L10Photo(double cx, double cy, double w, double h, double tw, double ta, double te)
	{
		bool fl = false;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.25) hz.dead = true; if (!fl && t >= ta) { fl = true; flash = Math.Max(flash, 0.35); } };
		hz.draw = t =>
		{
			double x = cx - w / 2, y = cy - h / 2;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1), c = 16 + 18 * k; g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.55 * k)})"; g.lineWidth = 3;
				foreach (var sxy in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 1, 1 } }) { int sx = sxy[0], sy = sxy[1]; double px = x + sx * w, py = y + sy * h, dx = sx != 0 ? -1 : 1, dy = sy != 0 ? -1 : 1; g.beginPath(); g.moveTo(px + dx * c, py); g.lineTo(px, py); g.lineTo(px, py + dy * c); g.stroke(); }
				g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.fillRect(x, y, w, h); g.strokeStyle = $"rgba(255,42,109,{str(0.4 + 0.5 * k)})"; g.beginPath(); g.moveTo(cx - 12, cy); g.lineTo(cx + 12, cy); g.moveTo(cx, cy - 12); g.lineTo(cx, cy + 12); g.stroke();
				g.fillStyle = $"rgba(255,42,109,{str(0.5 + 0.5 * k)})"; circle(x + w - 18, y + 16, 5); return;
			}
			double e = t - ta, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.25); g.globalAlpha = a; g.save(); g.shadowColor = "#fff"; g.shadowBlur = 30; g.fillStyle = e < 0.08 ? "#fff" : PINK; g.fillRect(x, y, w, h); g.restore(); g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 4; g.strokeRect(x, y, w, h); g.globalAlpha = 1;
		};
		hz.hit = (p, t) => t >= ta && t < te && p.x > cx - w / 2 - p.r + 3 && p.x < cx + w / 2 + p.r - 3 && p.y > cy - h / 2 - p.r + 3 && p.y < cy + h / 2 + p.r - 3;
		return hz;
	}
	// a napkin folding: the screen corner (cx,cy) folds over a diagonal line and lands on the other side of it
	static Hz L10Napkin(double cx, double cy, double L, double tw, double ta, double te)
	{
		double sx = cx < W / 2 ? 1 : -1, sy = cy < H / 2 ? 1 : -1; double[] A = { cx + sx * L, cy }, Bp = { cx, cy + sy * L }, M = { cx + sx * L, cy + sy * L };
		// the triangle the fold lands on: A, Bp and M
		Func<double, double, double, bool> inT = (px, py, m) => { double u = (px - cx) * sx / L, v = (py - cy) * sy / L; return u < 1 + m / L && v < 1 + m / L && u + v > 1 - m / L * 1.4; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.3) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.beginPath(); g.moveTo(A[0], A[1]); g.lineTo(M[0], M[1]); g.lineTo(Bp[0], Bp[1]); g.closePath(); g.fill(); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.stroke(); L10dash(false);
				// the corner starts to lift: the original triangle glows
				double f = clamp((t - (ta - 0.35)) / 0.35, 0, 1); if (f > 0) { double[] P = { cx + (M[0] - cx) * f, cy + (M[1] - cy) * f }; g.fillStyle = $"rgba(255,42,109,{str(0.35 + 0.4 * f)})"; g.beginPath(); g.moveTo(A[0], A[1]); g.lineTo(P[0], P[1]); g.lineTo(Bp[0], Bp[1]); g.closePath(); g.fill(); }
				g.fillStyle = GREY + "0.25)"; g.beginPath(); g.moveTo(cx, cy); g.lineTo(A[0], A[1]); g.lineTo(Bp[0], Bp[1]); g.closePath(); g.fill(); return;
			}
			double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = (t - ta) < 0.07 ? "#fff" : PINK; g.beginPath(); g.moveTo(A[0], A[1]); g.lineTo(M[0], M[1]); g.lineTo(Bp[0], Bp[1]); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 3; for (int i = 1; i < 5; i++) { double u = i / 5.0; g.beginPath(); g.moveTo(A[0] + (M[0] - A[0]) * u, A[1] + (M[1] - A[1]) * u); g.lineTo(Bp[0] + (M[0] - Bp[0]) * u, Bp[1] + (M[1] - Bp[1]) * u); g.stroke(); }
			g.restore(); g.globalAlpha = 1;
		};
		hz.hit = (p, t) => t >= ta && t < te && inT(p.x, p.y, p.r - 3);
		return hz;
	}
	// a baguette: a long stick across the screen, heats up and hurts, then breaks at kx and each half flies to a side
	static Hz L10Baguette(bool vert, double c, double kx, double tw, double ta, double tk, double te, double th = 34)
	{
		Func<double, double, double> half = (sd, t) => { double e = Math.Max(0, t - tk), d = e * e * 900 + e * 300; return sd * d; };
		bool bk = false;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; if (!bk && t >= tk) { bk = true; burst(vert ? c : kx, vert ? kx : c, "#ffd1a8", 10, 240); } };
		hz.draw = t =>
		{
			g.save(); if (vert) { g.translate(c, 0); g.rotate(Math.PI / 2); } else g.translate(0, c);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1), Lx_ = vert ? H : W; g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fillRect(0, -th / 2, Lx_, th); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.strokeRect(0, -th / 2, Lx_, th); L10dash(false); g.restore(); return; }
			double Lx = vert ? H : W, a = t < tk ? 1 : Math.Max(0, 1 - (t - tk) / (te - tk)); g.globalAlpha = a;
			foreach (var sd in new double[] { -1, 1 })
			{
				double o = half(sd, t), x0 = sd < 0 ? 0 : kx, x1 = sd < 0 ? kx : Lx; g.save(); g.translate(o, 0); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.roundRect(x0, -th / 2, x1 - x0, th, th / 2); g.fill(); g.shadowBlur = 0;
				g.strokeStyle = "rgba(255,255,255,0.55)"; g.lineWidth = 3; for (double q = x0 + 20; q < x1 - 16; q += 48) { g.beginPath(); g.moveTo(q, -th * 0.3); g.lineTo(q + 20, th * 0.3); g.stroke(); }
				g.restore();
			}
			g.restore(); g.globalAlpha = 1;
		};
		hz.hit = (p, t) => { if (t < ta || t >= tk + 0.15) return false; double u = vert ? p.y : p.x, v = vert ? p.x : p.y; if (Math.Abs(v - c) > th / 2 + p.r - 3) return false; if (t < tk) return true; double o = half(1, t); return u < kx - o + p.r - 3 || u > kx + o - p.r + 3; };
		return hz;
	}
	// cafe parasols: a row of parasols on one edge (top or bottom) that open and close on the beats
	static Hz L10Parasol(double x, bool top, double R, double tw, List<double[]> ops) // ops = [[open time, close time]]
	{
		double st = ops[0][0], en = ops[ops.Count - 1][1] + 0.3; Func<double, double> op = t => { double m = 0; foreach (var ab in ops) { double a = ab[0], b = ab[1]; if (t >= a - 0.05 && t < b + 0.12) m = Math.Max(m, t < a + 0.08 ? clamp((t - a + 0.05) / 0.13, 0, 1) : t < b ? 1 : clamp(1 - (t - b) / 0.12, 0, 1)); } return m; }; double y0 = top ? 0 : H;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > en) hz.dead = true; };
		hz.draw = t =>
		{
			double a = Math.Min(1, (t - tw) / 0.4); if (a <= 0) return; double o = op(t), sg = top ? 1 : -1; g.strokeStyle = GREY + "0.7)"; g.lineWidth = 4; g.beginPath(); g.moveTo(x, y0); g.lineTo(x, y0 + sg * (R * 0.25 + 14)); g.stroke();
			double nx = 1e9; foreach (var ab in ops) if (ab[0] > t) { nx = ab[0]; break; }
			double wk = clamp(1 - (nx - t) / (2 * SPB), 0, 1);
			if (o < 0.02) { g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * wk)})"; g.fillRect(x - 6, y0 + sg * 6 - (top ? 0 : R * 0.6), 12, R * 0.6); if (wk > 0) { g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * wk)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.arc(x, y0, R, top ? 0 : Math.PI, top ? Math.PI : TAU); g.stroke(); L10dash(false); } return; }
			g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.moveTo(x, y0); g.arc(x, y0, R * o, top ? 0 : Math.PI, top ? Math.PI : TAU); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.4)"; for (int i = 0; i < 6; i += 2) { double a0 = (top ? 0 : Math.PI) + i * Math.PI / 6; g.beginPath(); g.moveTo(x, y0); g.arc(x, y0, R * o, a0, a0 + Math.PI / 6); g.closePath(); g.fill(); }
			g.restore();
		};
		hz.hit = (p, t) => { double o = op(t); if (o < 0.6) return false; return hypot(p.x - x, p.y - y0) < R * o + p.r - 3 && (top ? p.y > -10 : p.y < H + 10); };
		return hz;
	}
	// a wooden plank (a tongue of wood) that comes out of an edge and goes back
	static Hz L10Plank(double y, double dir, double len, double th, double tw, double ta, double tf, double tb, double te)
	{
		Func<double, double> X = t => len * L10go(t, ta, tf, tb, te);
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1), x0_ = dir > 0 ? 0 : W - len; g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(x0_, y - th / 2, len, th); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.strokeRect(x0_, y - th / 2, len, th); L10dash(false); return; }
			double x = X(t), x0 = dir > 0 ? 0 : W - x; g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); double tip = dir > 0 ? x0 + x : x0; g.moveTo(dir > 0 ? x0 : x0 + x, y - th / 2); g.lineTo(tip - dir * th * 0.5, y - th / 2); g.quadraticCurveTo(tip + dir * th * 0.4, y, tip - dir * th * 0.5, y + th / 2); g.lineTo(dir > 0 ? x0 : x0 + x, y + th / 2); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(120,30,50,0.55)"; g.lineWidth = 2; for (int k = 0; k < 3; k++) { double yy = y - th / 2 + th * (k + 1) / 4; g.beginPath(); for (double q = 0; q <= x - th * 0.4; q += 20) { double xx = dir > 0 ? q : W - q; if (q != 0) g.lineTo(xx, yy + JS.sin(q * 0.05 + k) * 3); else g.moveTo(xx, yy); } g.stroke(); }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; double x = X(t); if (Math.Abs(p.y - y) > th / 2 + p.r - 3) return false; return dir > 0 ? p.x < x + p.r - 3 : p.x > W - x - p.r + 3; };
		return hz;
	}
	/* ---- phase 3: the boss, the Eiffel tower ---- */
	public class L10EiffelHz : Hz { public Func<double, double[]> top; }
	// the tower: its base at the bottom middle, it leans around its base. its grey body does not hurt, its pink beacon on top hurts
	static L10EiffelHz L10Eiffel(double t0, Func<double, double> lean, double ch0, double boom, double lk)
	{
		bool ex = false; double BX = W / 2, BY = H + 6; Func<double, double> yIn = t => H + 6 + 600 * (1 - L10ease((t - t0) / (4 * SPB)));
		Func<double, double[]> top = t => { double a = lean(t), y0 = yIn(t); return new[] { BX + JS.sin(a) * 540, y0 - JS.cos(a) * 540 }; };
		var hz = new L10EiffelHz { top = top };
		hz.update = (t, dt) =>
		{
			if (t >= boom && !ex) { ex = true; var xy = top(t); for (int i = 0; i < 40; i++) burst(BX + (rnd() - 0.5) * 300, BY - rnd() * 500, i % 2 != 0 ? "#fff" : PINK, 1, 520); burst(xy[0], xy[1], "#fff", 40, 600); shake = 30; flash = 0.8; rumble(650, 1, 1); hz.dead = true; }
		};
		hz.draw = t =>
		{
			if (t < t0) return; double a = lean(t), y0 = yIn(t), c = t >= ch0 ? clamp((t - ch0) / (boom - ch0), 0, 1) : 0; double jx = 0; if (c > 0) jx = JS.sin(t * 80) * 7 * c;
			{ var xy_ = top(t); double x_ = xy_[0], y_ = xy_[1]; if (c < 0.3) { g.save(); g.globalAlpha = 1 - c / 0.3; g.fillStyle = "rgba(210,210,235,0.07)"; foreach (var q in new[] { 0, Math.PI }) { double th = t * 1.1 + q; g.beginPath(); g.moveTo(x_, y_); g.lineTo(x_ + JS.cos(th - 0.07) * 1600, y_ + JS.sin(th - 0.07) * 1600); g.lineTo(x_ + JS.cos(th + 0.07) * 1600, y_ + JS.sin(th + 0.07) * 1600); g.closePath(); g.fill(); } g.restore(); } }
			L10towerShape(BX + jx, y0, 1, a, c > 0 ? $"rgba(255,255,255,{str(0.35 + 0.6 * c)})" : GREY + "0.55)", "rgba(255,42,109,0.55)", BT(t) * 2);
			var xy = top(t); double x = xy[0], y = xy[1]; g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = c > 0.5 ? "#fff" : PINK; g.translate(x + jx, y); g.rotate(a + Math.PI / 4); double s = 22 * (1 + 0.15 * pulse); g.fillRect(-s, -s, 2 * s, 2 * s); g.restore();
			if (c > 0) { var gr = g.createRadialGradient(BX, y0 - 270, 40, BX, y0 - 270, 420); gr.addColorStop(0, $"rgba(255,255,255,{str(0.25 * c)})"); gr.addColorStop(1, "rgba(255,255,255,0)"); g.fillStyle = gr; circle(BX, y0 - 270, 420); }
		};
		hz.hit = (p, t) => { if (t < t0 + 4 * SPB || t >= boom) return false; var xy = top(t); return hypot(p.x - xy[0], p.y - xy[1]) < 26 + p.r - 3; };
		return hz;
	}
	// an iron girder: comes out of the tower across the screen to the edges (with a gap in it), stops, and goes back to the tower
	static Hz L10Girder(double y, double gx, double gw, double tw, double ta, double th, double te, double ox = W / 2, double tk = 26)
	{
		Func<double, double> ext = t => (W * 0.62) * L10go(t, ta, ta + 0.16, th, te);
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(0, y - tk / 2, gx - gw / 2, tk); g.fillRect(gx + gw / 2, y - tk / 2, W - gx - gw / 2, tk); g.strokeStyle = $"rgba(255,42,109,{str(0.35 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.moveTo(0, y); g.lineTo(gx - gw / 2, y); g.moveTo(gx + gw / 2, y); g.lineTo(W, y); g.stroke(); L10dash(false);
				g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; g.beginPath(); g.moveTo(gx - gw / 2, y - tk); g.lineTo(gx - gw / 2, y + tk); g.moveTo(gx + gw / 2, y - tk); g.lineTo(gx + gw / 2, y + tk); g.stroke(); return;
			}
			double e = ext(t), a = Math.Max(0, ox - e), b = Math.Min(W, ox + e); g.save(); g.beginPath(); g.rect(a, y - tk / 2 - 2, b - a, tk + 4); g.clip(); g.beginPath(); g.rect(0, y - tk / 2 - 2, gx - gw / 2, tk + 4); g.rect(gx + gw / 2, y - tk / 2 - 2, W - gx - gw / 2, tk + 4); g.clip();
			g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(0, y - tk / 2, W, 4); g.fillRect(0, y + tk / 2 - 4, W, 4); g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 4; g.beginPath(); for (double q = 0; q < W; q += tk) { g.moveTo(q, y - tk / 2); g.lineTo(q + tk / 2, y + tk / 2); g.lineTo(q + tk, y - tk / 2); }
			g.stroke();
			g.fillStyle = "rgba(255,42,109,0.35)"; g.fillRect(0, y - tk / 2, W, tk); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; double e = ext(t); if (Math.Abs(p.y - y) > tk / 2 + p.r - 3) return false; if (p.x < ox - e - p.r || p.x > ox + e + p.r) return false; return Math.Abs(p.x - gx) > gw / 2 - p.r + 3; };
		return hz;
	}
	// an X shaped iron piece that tumbles
	static Hz L10Cross(double x, double y, double vx, double vy, double spin, double s = 20)
	{
		var b = Bullet(x, y, vx, vy, s); double a = rnd() * TAU;
		b.update = (t, dt) => { b.x += b.vx * dt; b.y += b.vy * dt; a += spin * dt; if (b.x < -80 || b.x > W + 80 || b.y < -80 || b.y > H + 80) b.dead = true; };
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.fillRect(-s, -5, 2 * s, 10); g.fillRect(-5, -s, 10, 2 * s); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.6)"; g.fillRect(-3, -3, 6, 6); g.restore(); };
		b.hit = (p, t) => obbHit(p.x, p.y, p.r - 2, b.x, b.y, s, 5, a) || obbHit(p.x, p.y, p.r - 2, b.x, b.y, 5, s, a);
		return b;
	}
	// the lift: a box that goes up and down in the middle of the tower
	static Hz L10Lift(double tw, double t0, double t1, Func<double, double> yAt)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.4) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < tw) return; double a = min(1, (t - tw) / 0.4, (t1 + 0.4 - t) / 0.4), y = yAt(t); g.globalAlpha = a; g.strokeStyle = GREY + "0.5)"; g.lineWidth = 2; g.beginPath(); g.moveTo(W / 2 - 8, H); g.lineTo(W / 2 - 8, H - 470); g.moveTo(W / 2 + 8, H); g.lineTo(W / 2 + 8, H - 470); g.stroke();
			g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = t >= t0 ? PINK : "rgba(255,42,109,0.4)"; g.fillRect(W / 2 - 24, y - 22, 48, 44); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.6)"; g.fillRect(W / 2 - 16, y - 14, 32, 12); g.restore(); g.globalAlpha = 1;
		};
		hz.hit = (p, t) => { if (t < t0 || t >= t1) return false; double y = yAt(t); return Math.Abs(p.x - W / 2) < 24 + p.r - 3 && Math.Abs(p.y - y) < 22 + p.r - 3; };
		return hz;
	}
	// "Bienvenue": the tower lights light up the whole screen except the safe place (drawSafe draws it, isSafe checks it)
	// after the flash the light pulls away from top to bottom (dd seconds)
	static Hz L10Welcome(Action drawSafe, Func<double, double, double, bool> isSafe, double tw, double ta, double te, double dd = 0)
	{
		Func<double, double> yd = t => t < te ? 0 : H * L10ease((t - te) / dd); bool fl = false;
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + dd + 0.3) hz.dead = true; if (!fl && t >= ta) { fl = true; shake = Math.Max(shake, 12); } };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.beginPath(); g.rect(0, 0, W, H); drawSafe(); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.16 * k)})"; g.fill("evenodd"); g.restore();
				g.save(); drawSafe(); g.strokeStyle = $"rgba(255,255,255,{str(0.45 + 0.5 * k)})"; g.lineWidth = 4; L10dash(true); g.stroke(); L10dash(false); g.restore(); return;
			}
			double a = dd > 0 ? (t < te + dd ? 1 : 0) : (t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3)); if (a <= 0) return; double y0 = dd > 0 ? yd(t) : 0; g.save(); g.globalAlpha = a; g.beginPath(); g.rect(0, y0, W, H - y0); g.clip(); g.beginPath(); g.rect(0, 0, W, H); drawSafe(); g.fillStyle = (t - ta) < 0.07 ? "#fff" : "rgba(255,42,109,0.82)"; g.fill("evenodd"); g.clip("evenodd");
			g.fillStyle = "rgba(255,255,255,0.35)"; for (double y = 20; y < H; y += 60) for (double x = (y / 60 % 2) * 30; x < W; x += 60) circle(x, y, 4);
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; if (t >= te && !(dd > 0 && t < te + dd && p.y > yd(t) + p.r)) return false; return !isSafe(p.x, p.y, p.r - 3); };
		return hz;
	}
	// the tower sparkle: the tower shape (a triangle) grows out from the tower, with a gap in it
	static Hz L10SparkShape(double cx, double cy, double t0, double sp, double gapA, double gapW, double tw)
	{
		double[][] S = { new double[] { 0, -1 }, new[] { 0.62, 0.78 }, new[] { -0.62, 0.78 } }; // the triangle's corners (size 1)
		Func<double, double[][]> pts = sc => S.Select(v => new[] { cx + v[0] * sc, cy + v[1] * sc }).ToArray();
		Func<double, double, bool> angOk = (px, py) => { double da = JS.atan2(py - cy, px - cx) - gapA; da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) > gapW / 2; };
		Func<double, double, double[], double[], double> dSeg = (px, py, a, b) => { double vx = b[0] - a[0], vy = b[1] - a[1], u = clamp(((px - a[0]) * vx + (py - a[1]) * vy) / (vx * vx + vy * vy), 0, 1); return hypot(px - a[0] - vx * u, py - a[1] - vy * u); };
		double FA = 900;
		var hz = new Hz();
		hz.update = (t, dt) => { if ((t - t0) * sp + 40 > FA + 120) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < t0)
			{
				double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; var P_ = pts(30 + 30 * k); g.beginPath(); g.moveTo(P_[0][0], P_[0][1]); g.lineTo(P_[1][0], P_[1][1]); g.lineTo(P_[2][0], P_[2][1]); g.closePath(); g.stroke();
				g.fillStyle = $"rgba(255,255,255,{str(0.4 + 0.5 * k)})"; circle(cx + JS.cos(gapA) * (60 + 60 * k), cy + JS.sin(gapA) * (60 + 60 * k), 6); return;
			}
			double sc = (t - t0) * sp + 40; var P = pts(sc); g.save(); g.globalAlpha = clamp((FA + 120 - sc) / 120, 0, 1); g.beginPath(); g.rect(0, 0, W, H); g.moveTo(cx, cy); g.arc(cx, cy, 2400, gapA - gapW / 2, gapA + gapW / 2); g.closePath(); g.clip("evenodd");
			g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = 12 * (1 + 0.15 * pulse); g.lineJoin = "round"; g.beginPath(); g.moveTo(P[0][0], P[0][1]); g.lineTo(P[1][0], P[1][1]); g.lineTo(P[2][0], P[2][1]); g.closePath(); g.stroke(); g.shadowBlur = 0;
			g.fillStyle = "#fff"; for (int k = 0; k < 3; k++) { var a = P[k]; var b = P[(k + 1) % 3]; for (int i = 0; i < 12; i++) { if ((i + Math.Floor(t * 12)) % 3 != 0) continue; double u = i / 12.0; circle(a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u, 3); } }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < t0) return false; double sc = (t - t0) * sp + 40; if (sc > FA + 40) return false; var P = pts(sc); double d = 1e9; for (int k = 0; k < 3; k++) d = Math.Min(d, dSeg(p.x, p.y, P[k], P[(k + 1) % 3])); return d < 6 + p.r - 2 && angOk(p.x, p.y); };
		return hz;
	}
	// a star of light that comes out of the beacon
	static Hz L10Twinkle(double x, double y, double vx, double vy)
	{
		var b = Bullet(x, y, vx, vy, 9);
		b.draw = t => { double s = 13 * (1 + 0.2 * pulse); g.save(); g.translate(b.x, b.y); g.rotate(Math.PI / 4); g.fillStyle = "rgba(255,42,109,0.25)"; circle(0, 0, s * 1.3); g.fillStyle = PINK; g.beginPath(); g.moveTo(0, -s); g.lineTo(s * 0.28, 0); g.lineTo(0, s); g.lineTo(-s * 0.28, 0); g.closePath(); g.fill(); g.beginPath(); g.moveTo(-s, 0); g.lineTo(0, s * 0.28); g.lineTo(s, 0); g.lineTo(0, -s * 0.28); g.closePath(); g.fill(); g.fillStyle = "#fff"; circle(0, 0, 3); g.restore(); };
		return b;
	}

	/* ---- extras that keep the level full ---- */
	// the Ritz chandeliers (grey decoration on top), the crystal comes down from them
	static readonly double[] L10CH = { 128, 384, 640, 896, 1152 }; const double L10CHY = 62;
	static void L10gem(double x, double y, double s) { g.beginPath(); g.moveTo(x, y - s); g.lineTo(x + s * 0.7, y); g.lineTo(x, y + s * 1.3); g.lineTo(x - s * 0.7, y); g.closePath(); g.fill(); }
	static void L10chand(double x, double y, double lit)
	{
		g.save(); g.translate(x, y); g.strokeStyle = GREY + "0.3)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, -y); g.lineTo(0, -20); g.stroke();
		g.fillStyle = GREY + "0.28)"; g.beginPath(); g.ellipse(0, 0, 50, 12, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(0, -14, 24, 8, 0, 0, TAU); g.fill();
		for (int i = -2; i <= 2; i++) { g.fillStyle = GREY + "0.28)"; g.fillRect(i * 20 - 2, -26, 4, 14); g.fillStyle = $"rgba(255,255,255,{str(0.1 + 0.3 * lit * ((i + 2) % 2))})"; circle(i * 20, -30, 3.5); }
		for (int i = -3; i <= 3; i++) { g.fillStyle = $"rgba(200,200,215,{str(0.18 + 0.22 * lit * ((i + 3) % 2))})"; L10gem(i * 14, 16, 6); }
		g.restore();
	}
	// a crystal strand comes down from the chandelier on a melody note, and goes back up
	static Hz L10Strand(double x, double len, double tw, double ta, double tr, double te)
	{
		double y0 = L10CHY + 26, cr = 14; Func<double, double> E = t => t < ta ? 0 : t < ta + 0.1 ? L10ease((t - ta) / 0.1) : t < tr ? 1 : 1 - L10ease((t - tr) / (te - tr));
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.moveTo(x, y0); g.lineTo(x, y0 + len); g.stroke(); L10dash(false);
				g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.5 * k)})"; L10gem(x, y0 + len, cr * (0.5 + 0.5 * k)); return;
			}
			double y1 = y0 + len * E(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 6; g.beginPath(); g.moveTo(x, y0); g.lineTo(x, y1); g.stroke();
			g.fillStyle = PINK; for (double q = y0 + 30; q < y1 - 20; q += 34) circle(x, q, 6); g.fillStyle = (t - ta) < 0.07 ? "#fff" : PINK; L10gem(x, y1, cr); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.55)"; L10gem(x - 3, y1 - 3, cr * 0.35); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; double y1 = y0 + len * E(t); if (Math.Abs(p.x - x) < 6 + p.r - 3 && p.y > y0 - p.r && p.y < y1 + p.r - 3) return true; return hypot(p.x - x, p.y - y1) < cr + p.r - 3; };
		return hz;
	}
	// a champagne glass tower (decoration) at the bottom middle
	static Hz L10Glasses(double t0, double t1, double[] pops)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.5) hz.dead = true; };
		hz.draw = t =>
		{
			double a = min(1, (t - t0) / 0.4, (t1 + 0.5 - t) / 0.5); if (a <= 0) return; double sq = 0; foreach (var q in pops) { double e = t - q; if (e > -0.1 && e < 0.25) sq = Math.Max(sq, 1 - Math.Abs(e) / 0.25); }
			g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.45)"; double cx = W / 2; for (int r = 0; r < 4; r++) { int n = 4 - r; double y = H - 14 - r * 34; for (int i = 0; i < n; i++) { double x = cx + (i - (n - 1) / 2.0) * 58; g.beginPath(); g.moveTo(x - 24, y - 26); g.lineTo(x + 24, y - 26); g.lineTo(x + 4, y - 8); g.lineTo(x + 4, y); g.lineTo(x - 4, y); g.lineTo(x - 4, y - 8); g.closePath(); g.fill(); } }
			g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.6 * sq)})"; circle(cx, H - 150, 10 + 8 * sq); g.restore();
		};
		return hz;
	}
	// the fountain warning: dots along each drop's path
	static Hz L10Arcs(double ox, double oy, List<double[]> V, double gr, double tw, double ta)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t >= ta) hz.dead = true; };
		hz.draw = t => { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.12 + 0.4 * k)})"; foreach (var v in V) { double vx = v[0], vy = v[1]; for (double s = 0.12; s < 2.6; s += 0.12) { double x = ox + vx * s, y = oy + vy * s + 0.5 * gr * s * s; if (y > H || x < 0 || x > W) break; circle(x, y, 2.5); } } };
		return hz;
	}
	// a champagne drop: flies and falls with gravity
	static Hz L10Drop(double x, double y, double vx, double vy, double gr, double r = 9)
	{
		var b = Bullet(x, y, vx, vy, r);
		b.update = (t, dt) => { b.vy += gr * dt; b.x += b.vx * dt; b.y += b.vy * dt; if (b.y > H + 40 || b.x < -40 || b.x > W + 40) b.dead = true; };
		b.draw = t => { g.save(); g.translate(b.x, b.y); g.rotate(JS.atan2(b.vy, b.vx) - Math.PI / 2); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; g.beginPath(); g.ellipse(0, 0, r * 0.85, r * 1.15, 0, 0, TAU); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.6)"; circle(-r * 0.3, -r * 0.2, r * 0.25); g.restore(); };
		return b;
	}
	// a Montmartre windmill: 4 sails that turn a step (45 degrees) with the beats
	static Hz L10Mill(double hx, double hy, double L, double dir, double a0, double tw, double ta, double te, List<double> steps, double stepB = 0.4)
	{
		double r0 = 28, sw = 19; Func<double, double> ang = t => { double b = BT(t); double s = 0; foreach (var q in steps) { if (b < q) break; s += L10ease((b - q) / stepB); } return a0 + dir * s * Math.PI / 4; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.5) hz.dead = true; };
		hz.draw = t =>
		{
			double al = min(1, (t - tw) / 0.5, (te + 0.5 - t) / 0.5); if (al <= 0) return; bool live = t >= ta && t < te; double a = ang(t), k0 = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.globalAlpha = al;
			g.fillStyle = "rgba(22,22,30,0.95)"; g.strokeStyle = GREY + "0.4)"; g.lineWidth = 3; g.beginPath(); g.moveTo(hx - 40, hy + 10); g.lineTo(hx + 40, hy + 10); g.lineTo(hx + 80, H); g.lineTo(hx - 80, H); g.closePath(); g.fill(); g.stroke(); g.beginPath(); g.moveTo(hx - 56, hy + 16); g.lineTo(hx, hy - 46); g.lineTo(hx + 56, hy + 16); g.closePath(); g.fill();
			g.fillStyle = GREY + "0.35)"; g.fillRect(hx - 14, H - 60, 28, 60); g.fillRect(hx - 30, hy + 70, 18, 22); g.fillRect(hx + 12, hy + 70, 18, 22);
			if (t < ta) { g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k0)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.arc(hx, hy, L, 0, TAU); g.stroke(); L10dash(false); }
			for (int k = 0; k < 4; k++)
			{
				g.save(); g.translate(hx, hy); g.rotate(a + k * Math.PI / 2); if (live) { g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; } else g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.3 * k0)})";
				g.fillRect(r0, -sw, L - r0, 2 * sw); g.shadowBlur = 0; if (live) { g.strokeStyle = "rgba(110,20,45,0.55)"; g.lineWidth = 2; g.beginPath(); for (double q = r0 + 28; q < L; q += 28) { g.moveTo(q, -sw); g.lineTo(q, sw); } g.moveTo(r0, 0); g.lineTo(L, 0); g.stroke(); }
				g.restore();
			}
			g.fillStyle = live ? "#ffd1de" : GREY + "0.5)"; circle(hx, hy, r0 * 0.8); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta || t >= te) return false; double a = ang(t), m = (r0 + L) / 2; if (hypot(p.x - hx, p.y - hy) < r0 * 0.8 + p.r - 3) return true; for (int k = 0; k < 4; k++) { double th = a + k * Math.PI / 2; if (obbHit(p.x, p.y, p.r - 3, hx + JS.cos(th) * m, hy + JS.sin(th) * m, (L - r0) / 2, sw, th)) return true; } return false; };
		return hz;
	}
	// laundry clothes: s shirt, t trousers, d dress, k sock
	static void L10cloth(string k, double hw, double hh)
	{
		double h = 2 * hh; g.beginPath();
		if (k == "s") { g.moveTo(-hw * 0.6, 0); g.lineTo(hw * 0.6, 0); g.lineTo(hw, h * 0.28); g.lineTo(hw * 0.7, h * 0.36); g.lineTo(hw * 0.62, h); g.lineTo(-hw * 0.62, h); g.lineTo(-hw * 0.7, h * 0.36); g.lineTo(-hw, h * 0.28); }
		else if (k == "t") { g.moveTo(-hw, 0); g.lineTo(hw, 0); g.lineTo(hw, h); g.lineTo(hw * 0.12, h); g.lineTo(0, h * 0.3); g.lineTo(-hw * 0.12, h); g.lineTo(-hw, h); }
		else if (k == "d") { g.moveTo(-hw * 0.45, 0); g.lineTo(hw * 0.45, 0); g.lineTo(hw * 0.5, h * 0.35); g.lineTo(hw, h); g.lineTo(-hw, h); g.lineTo(-hw * 0.5, h * 0.35); }
		else { g.moveTo(-hw, 0); g.lineTo(hw, 0); g.lineTo(hw, h * 0.7); g.lineTo(hw * 1.8, h * 0.85); g.lineTo(hw * 1.8, h); g.lineTo(-hw, h); }
		g.closePath(); g.fill();
	}
	// a washing line across the square: the pink clothes swing and drift with the wind back and forth
	static Hz L10Laundry(double yr, (double x0, string k)[] items, double tw, double ta, double te, double ph = 0, double zs = 1)
	{
		double sag = 34; Func<double, double> ry = x => yr + sag * (1 - JS.pow(2 * clamp(x, 0, W) / W - 1, 2));
		var SZ = new Dictionary<string, double[]> { ["s"] = new[] { 42 * zs, 50 * zs }, ["t"] = new[] { 30 * zs, 75 * zs }, ["d"] = new[] { 46 * zs, 70 * zs }, ["k"] = new[] { 16 * zs, 32 * zs } };
		Func<double, double> off = t => 60 * JS.sin(Math.PI * (BT(t) - ph) / 4); Func<double, int, double> sw = (t, i) => 0.22 * JS.sin(Math.PI * (BT(t) - ph) / 2 + i * 0.7);
		Func<double, int, double[]> geo = (t, i) => { double x0 = items[i].x0; var hwh = SZ[items[i].k]; double hw = hwh[0], hh = hwh[1], x = x0 + off(t), y = ry(x), a = sw(t, i); return new[] { x - JS.sin(a) * hh, y + JS.cos(a) * hh, hw, hh, a, x, y }; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te + 0.5) hz.dead = true; };
		hz.draw = t =>
		{
			double al = min(1, (t - tw) / 0.5, (te + 0.5 - t) / 0.5); if (al <= 0) return; bool live = t >= ta && t < te; double k0 = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.globalAlpha = al;
			g.strokeStyle = GREY + "0.6)"; g.lineWidth = 3; g.beginPath(); for (double x = -20; x <= W + 20; x += 20) { double y = ry(x); if (x > -20) g.lineTo(x, y); else g.moveTo(x, y); }
			g.stroke();
			for (int i = 0; i < items.Length; i++)
			{
				var G_ = geo(t, i); double hw = G_[2], hh = G_[3], a = G_[4], px = G_[5], py = G_[6]; g.save(); g.translate(px, py); g.rotate(a);
				if (live) { g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; } else g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.35 * k0)})"; L10cloth(items[i].k, hw, hh); g.shadowBlur = 0;
				if (live) { g.fillStyle = "rgba(255,255,255,0.3)"; g.fillRect(-hw * 0.5, hh * 0.5, hw, 4); }
				g.fillStyle = GREY + "0.85)"; g.fillRect(-hw * 0.6 - 3, -5, 6, 11); g.fillRect(hw * 0.6 - 3, -5, 6, 11); g.restore();
			}
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta || t >= te) return false; for (int i = 0; i < items.Length; i++) { var G_ = geo(t, i); if (obbHit(p.x, p.y, p.r - 3, G_[0], G_[1], G_[2] * 0.9, G_[3] * 0.95, G_[4])) return true; } return false; };
		return hz;
	}
	// the photographers (decoration): grey people at the bottom raising their cameras, a flash on every hit
	static Hz L10Papz(double t0, double t1, double[] hits)
	{
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > t1 + 0.5) hz.dead = true; };
		hz.draw = t =>
		{
			double al = min(1, (t - t0) / 0.4, (t1 + 0.5 - t) / 0.5); if (al <= 0) return; double f = 0; foreach (var q in hits) { double e = t - q; if (e >= 0 && e < 0.35) f = Math.Max(f, 1 - e / 0.35); }
			g.save(); g.globalAlpha = al;
			for (int i = 0; i < 9; i++)
			{
				double x = 70 + i * 142 + (i % 2) * 20, y = H + 10, s = 1 + (i % 3) * 0.12, cx = x + (i % 2 != 0 ? -18 : 18) * s, cy = y - 110 * s; g.fillStyle = GREY + "0.3)"; circle(x, y - 92 * s, 17 * s); g.fillRect(x - 24 * s, y - 74 * s, 48 * s, 80 * s);
				g.fillStyle = GREY + "0.55)"; g.fillRect(cx - 16, cy - 11, 32, 22); g.fillRect(cx - 5, cy - 17, 10, 6); if (f > 0) { g.fillStyle = $"rgba(255,255,255,{str(f)})"; circle(cx, cy - 22, 6 + 18 * f); }
			}
			g.restore();
		};
		return hz;
	}

	// a refused gift thrown out of a hotel window: falls, bounces once on the floor, and leaves. kind 0 bag, 1 perfume bottle, 2 hat box
	static Hz L10Toss(double x0, double y0, double vx, double vy, double gr, double tw, double ta, int kind, double s = 30)
	{
		double fy = H - s * 0.8, A = 0.5 * gr, t1 = (-vy + Math.Sqrt(vy * vy - 4 * A * (y0 - fy))) / (2 * A), v1 = -(vy + gr * t1) * 0.6;
		Func<double, double[]> P = e => { double x = x0 + vx * e; if (e < t1) return new[] { x, y0 + vy * e + A * e * e }; double e2 = e - t1; return new[] { x, fy + v1 * e2 + A * e2 * e2 }; };
		var hz = new Hz();
		hz.update = (t, dt) => { if (t < ta) return; var xy = P(t - ta); if (xy[1] > H + 60 || xy[0] < -80 || xy[0] > W + 80) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; for (double e_ = 0.1; e_ < 4; e_ += 0.09) { var xy_ = P(e_); if (xy_[1] > H || xy_[0] < 0 || xy_[0] > W) break; circle(xy_[0], xy_[1], 2.5); }
				g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; circle(x0, y0, 6 + 6 * k); return;
			}
			double e = t - ta; var xy = P(e); g.save(); g.translate(xy[0], xy[1]); g.rotate(e * vx * 0.012); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
			if (kind == 0) { g.fillRect(-s, -s * 0.7, 2 * s, 1.4 * s); g.shadowBlur = 0; g.strokeStyle = GREY + "0.9)"; g.lineWidth = 4; g.strokeRect(-s * 0.3, -s * 0.95, s * 0.6, s * 0.25); g.fillStyle = "rgba(255,255,255,0.4)"; g.fillRect(-s * 0.55, -s * 0.7, 6, 1.4 * s); g.fillRect(s * 0.45, -s * 0.7, 6, 1.4 * s); }
			else if (kind == 1) { g.fillRect(-s * 0.7, -s * 0.5, 1.4 * s, 1.3 * s); g.shadowBlur = 0; g.fillStyle = GREY + "0.9)"; g.fillRect(-s * 0.25, -s * 0.85, s * 0.5, s * 0.35); g.fillStyle = "rgba(255,255,255,0.45)"; g.fillRect(-s * 0.5, -s * 0.35, 5, s); }
			else { g.beginPath(); g.ellipse(0, 0, s, s * 0.8, 0, 0, TAU); g.fill(); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 3; g.beginPath(); g.moveTo(-s, 0); g.lineTo(s, 0); g.moveTo(0, -s * 0.8); g.lineTo(0, s * 0.8); g.stroke(); }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < ta) return false; var xy = P(t - ta); return hypot(p.x - xy[0], p.y - xy[1]) < s * 0.85 + p.r - 3; };
		return hz;
	}
	// a velvet curtain on a screen edge (the Ritz): opens and closes with the swing. side -1 left, 1 right. segs = [[start, end]] in seconds
	static Hz L10Curtain(double side, List<double[]> segs, double w0, double w1)
	{
		double te = segs[segs.Count - 1][1] + 0.5; Func<double, double> amt = t => { double m = 0; foreach (var ab in segs) m = Math.Max(m, Math.Min(clamp((t - ab[0]) / 0.5, 0, 1), clamp((ab[1] - t) / 0.5, 0, 1))); return m; };
		Func<double, double> wd = t => amt(t) * (w0 + (w1 - w0) * (0.5 - 0.5 * JS.cos(Math.PI * BT(t))));
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			double warn = 0; foreach (var ab in segs) if (t < ab[0] && t > ab[0] - 1.2) warn = Math.Max(warn, 1 - (ab[0] - t) / 1.2); double w = wd(t);
			if (warn > 0 && w < 2) { g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.4 * warn)})"; g.fillRect(side < 0 ? 0 : W - w0, 0, w0, H); }
			if (w < 1) return; double x0 = side < 0 ? 0 : W - w; g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath();
			if (side < 0) { g.moveTo(0, 0); g.lineTo(w, 0); g.quadraticCurveTo(w * 0.7, H * 0.5, w, H); g.lineTo(0, H); } else { g.moveTo(W, 0); g.lineTo(W - w, 0); g.quadraticCurveTo(W - w * 0.7, H * 0.5, W - w, H); g.lineTo(W, H); }
			g.closePath(); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(120,10,45,0.5)"; g.lineWidth = 3; for (double q = 18; q < w - 6; q += 22) { double x = side < 0 ? q : W - q; g.beginPath(); g.moveTo(x, 0); g.quadraticCurveTo(x - side * 6, H * 0.5, x, H); g.stroke(); }
			g.fillStyle = GREY + "0.8)"; double tx = side < 0 ? w * 0.8 : W - w * 0.8; g.fillRect(tx - 5, H * 0.5 - 14, 10, 28); g.restore();
		};
		hz.hit = (p, t) => { double w = wd(t); if (w < 4) return false; double ww = w * (1 - 0.3 * (1 - JS.pow(2 * p.y / H - 1, 2))) + 0; return side < 0 ? p.x < ww + p.r - 3 : p.x > W - ww - p.r + 3; };
		return hz;
	}
	// a striped cafe awning on top or bottom (Montmartre): goes out and in with the rhythm
	static Hz L10Awning(bool top, List<double[]> segs, double d0, double d1)
	{
		double te = segs[segs.Count - 1][1] + 0.5; Func<double, double> amt = t => { double m = 0; foreach (var ab in segs) m = Math.Max(m, Math.Min(clamp((t - ab[0]) / 0.5, 0, 1), clamp((ab[1] - t) / 0.5, 0, 1))); return m; };
		Func<double, double> dp = t => amt(t) * (d0 + (d1 - d0) * Math.Abs(JS.sin(Math.PI * BT(t) / 2)));
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			double warn = 0; foreach (var ab in segs) if (t < ab[0] && t > ab[0] - 1.2) warn = Math.Max(warn, 1 - (ab[0] - t) / 1.2); double d = dp(t);
			if (warn > 0 && d < 2) { g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.4 * warn)})"; g.fillRect(0, top ? 0 : H - d0, W, d0); }
			if (d < 1) return; g.save(); double y0 = top ? 0 : H, sg = top ? 1 : -1; g.shadowColor = PINK; g.shadowBlur = 12;
			for (int i = 0; i < W / 64; i++) { double x = i * 64; g.fillStyle = i % 2 != 0 ? PINK : "rgba(255,42,109,0.72)"; g.beginPath(); g.moveTo(x, y0); g.lineTo(x + 64, y0); g.lineTo(x + 64, y0 + sg * (d - 14)); g.arc(x + 32, y0 + sg * (d - 14), 32, top ? 0 : Math.PI, top ? Math.PI : TAU); g.closePath(); g.fill(); }
			g.restore();
		};
		hz.hit = (p, t) => { double d = dp(t); if (d < 4) return false; return top ? p.y < d + p.r - 3 : p.y > H - d - p.r + 3; };
		return hz;
	}
	// the stage curtain: both halves close from the sides and leave an opening (sx its centre, sw its width), then open again
	static Hz L10Stage(double sx, double sw, double tw, double ta, double th, double tb, double te, double sx2)
	{
		Func<double, double> k = t => L10go(t, ta, th, tb, te), C = t => sx + (sx2 - sx) * L10ease((t - th) / (tb - th)), Lw = t => (C(t) - sw / 2) * k(t), Rw = t => (W - C(t) - sw / 2) * k(t);
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double q = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * q)})"; g.fillRect(0, 0, sx - sw / 2, H); g.fillRect(sx + sw / 2, 0, W - sx - sw / 2, H);
				g.strokeStyle = $"rgba(255,255,255,{str(0.35 + 0.55 * q)})"; g.lineWidth = 3; L10dash(true); g.strokeRect(sx - sw / 2, -4, sw, H + 8); L10dash(false);
				if (sx2 != sx) { g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.5 * q)})"; double d = sign(sx2 - sx); for (int i = 0; i < 3; i++) { double ax = sx + d * (sw / 2 + 16 + i * 22), ay = H / 2; g.beginPath(); g.moveTo(ax, ay - 12); g.lineTo(ax + d * 12, ay); g.lineTo(ax, ay + 12); g.fill(); } }
				return;
			}
			double l = Lw(t), r = Rw(t); if (l < 1 && r < 1) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(0, 0, l, H); g.fillRect(W - r, 0, r, H); g.shadowBlur = 0;
			g.strokeStyle = "rgba(120,10,45,0.5)"; g.lineWidth = 3; for (double q = 20; q < l - 4; q += 26) { g.beginPath(); g.moveTo(l - q, 0); g.quadraticCurveTo(l - q - 5, H * 0.5, l - q, H); g.stroke(); }
			for (double q = 20; q < r - 4; q += 26) { g.beginPath(); g.moveTo(W - r + q, 0); g.quadraticCurveTo(W - r + q + 5, H * 0.5, W - r + q, H); g.stroke(); }
			g.fillStyle = "rgba(255,255,255,0.5)"; g.fillRect(0, 0, W, 10); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta || t >= te) return false; return p.x < Lw(t) + p.r - 3 || p.x > W - Rw(t) - p.r + 3; };
		return hz;
	}
	// a pigeon wave as tall as the screen crossing it, with a gap in it (gy its centre, gh its height) that sways a little
	static Hz L10WaveFlock(double dir, double gy, double gh, double v, double tw, double tA, double sway = 0)
	{
		int n = 30; double x0 = dir > 0 ? -30 : W + 30; Func<int, double> yy = j => 12 + j * 24.3; Func<double, double> gc = t => gy + sway * JS.sin((t - tA) * 2.2);
		Func<int, double, double[]> pos = (i, t) => { int row = i / n, j = i % n; double x = x0 + dir * v * (t - tA) - dir * row * 30, y = yy(j) + (row % 2 != 0 ? 12 : 0); if (x < -50 || x > W + 50) return null; if (Math.Abs(y - gc(t)) < gh / 2) return null; return new[] { x, y }; };
		return L10Flock(3 * n, pos, tw, tA, tA + (W + 170) / v, new L10FO { gh = (t, k) => { g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; double ex = dir > 0 ? 0 : W - 12, c = gc(tA); g.fillRect(ex, 0, 12, c - gh / 2); g.fillRect(ex, c + gh / 2, 12, H - c - gh / 2); g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.5 * k)})"; g.lineWidth = 3; g.strokeRect(ex - 2, c - gh / 2, 16, gh); } });
	}
	// a falling girder (like the lift floor): sweeps the screen from top to bottom, with a gap in it
	static Hz L10Scan(double y0, double y1, double gx, double gw, double tw, double ta, double te, double tk, double gx2)
	{
		Func<double, double> k = t => clamp((t - ta) / (te - ta), 0, 1), Y = t => y0 + (y1 - y0) * k(t), G_ = t => gx + (gx2 - gx) * L10ease(k(t));
		var hz = new Hz();
		hz.update = (t, dt) => { if (t > te) hz.dead = true; };
		hz.draw = t =>
		{
			if (t < ta)
			{
				double q = clamp((t - tw) / (ta - tw), 0, 1), y_ = clamp(y0, 6, H - 6); g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.4 * q)})"; g.fillRect(0, y_ - 3, gx - gw / 2, 6); g.fillRect(gx + gw / 2, y_ - 3, W - gx - gw / 2, 6);
				g.strokeStyle = $"rgba(255,255,255,{str(0.35 + 0.5 * q)})"; g.lineWidth = 3; L10dash(true); g.beginPath(); g.moveTo(gx - gw / 2, y0); g.lineTo(gx2 - gw / 2, y1); g.moveTo(gx + gw / 2, y0); g.lineTo(gx2 + gw / 2, y1); g.stroke(); L10dash(false); return;
			}
			double y = Y(t), c = G_(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; foreach (var ab in new[] { new[] { 0, c - gw / 2 }, new[] { c + gw / 2, W } }) { g.fillRect(ab[0], y - tk / 2, ab[1] - ab[0], tk); }
			g.shadowBlur = 0; g.strokeStyle = "rgba(120,10,45,0.6)"; g.lineWidth = 3; g.beginPath();
			for (double q = 0; q < W; q += tk) { if (q + tk > c - gw / 2 && q < c + gw / 2) continue; g.moveTo(q, y - tk / 2); g.lineTo(q + tk / 2, y + tk / 2); g.lineTo(q + tk, y - tk / 2); }
			g.stroke(); g.restore();
		};
		hz.hit = (p, t) => { if (t < ta || t >= te) return false; double y = Y(t); if (Math.Abs(p.y - y) > tk / 2 + p.r - 3) return false; return Math.Abs(p.x - G_(t)) > gw / 2 - p.r + 3; };
		return hz;
	}
	// the schedule of the big attacks that sweep the whole screen (curtain, pigeon wave, girder): they come out of one schedule in order so Easy does not remove any of them
	// (Easy makes them easier itself: a wider gap and a longer warning). L = [[warning time, a function that makes the attack]]
	static Hz L10Nil()
	{
		var hz = new Hz();
		hz.update = (t, dt) => { hz.dead = true; };
		return hz;
	}
	static Hz L10Sched(List<(double t, Func<Hz> f)> L)
	{
		int i = -1;
		var hz = new Hz();
		hz.update = (t, dt) => { if (i < 0) { i = 0; while (i < L.Count && L[i].t < t - 0.05) i++; } while (i < L.Count && L[i].t <= t) { hazards.Add(L[i].f()); i++; } if (i >= L.Count) hz.dead = true; };
		return hz;
	}
	/* ================= level 10: Je veux (ZAZ) =================
	   the song from the start. 155 bpm (a beat is 0.387 s). beat 0 = 0:00.527
	   intro 0, verse 1 32 (0:12.9), bridge 96, chorus 112 (0:43.9, checkpoint), break 176 (1:08.7),
	   verse 2 184 (1:11.8), bridge 248, chorus 2 264 (1:42.7, checkpoint), solo 328 (2:07.5),
	   the music stops 390 (2:31.5) and the boss explodes, comes back 391 (2:31.9) = the win */
	// a tier of this level also keeps its base speed: {sp, wn, base}
	public class L10Tier { public double sp, wn, bse; }
	// papala options (o.wn, o.sp, o.r, o.ang, o.syl, o.shape)
	public class L10PO { public double? wn, sp, r, ang; public double[] syl; public string shape; }
	public static List<Ev> buildLevel10()
	{
		bool A2 = afkV == 2;
		var E = new List<Ev>(); var R = mulberry32(1010);
		void ev(double b, Action f) => E.Add(new Ev { b = b, f = f });
		void keep(double b, Action f) => E.Add(new Ev { b = b, keep = true, f = f });
		// the attacks that sweep the whole screen (nobody can stay in one place for long): they go in the SW schedule
		var SW = new List<(double t, Func<Hz> f)>(); void sweep(double wb, Func<Hz> f) => SW.Add((wb, f));
		double[] BASE = { 300, 320, 340 };
		var TI = ezT(new[] { new Tier { sp = 300, wn = 3 }, new Tier { sp = 320, wn = 3 }, new Tier { sp = 340, wn = 2.8 } }).Select((t, i) => new L10Tier { sp = t.sp, wn = t.wn, bse = BASE[i] }).ToArray();
		double kf(L10Tier T) => T.sp / T.bse;
		double PXp() => player != null ? player.x : W / 2; double PYp() => player != null ? player.y : H / 2;
		void fx(double b, double f2, double s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
		void word(double b, string s, double x, double y, double size, double dur, string col) => ev(b, () => hazards.Add(L10Word(s, x, y, size, B(b), B(b + dur), col)));
		double wB(double hb, double w) => easy ? Math.Floor(hb - w) : hb - w;
		L10Tier T = TI[0];
		// papala papapala: 7 stones on the syllables. the line starts on b0, the stones are on a line through your place
		double[] PAPA = { 0.35, 1.35, 2.15, 2.9, 3.85, 4.8, 5.8 };
		void papala(double b0, L10Tier T, L10PO o = null)
		{
			o ??= new L10PO(); double w = o.wn ?? T.wn + 1, sp = o.sp ?? 118, r = o.r ?? 30; double? an = o.ang;
			ev(wB(b0 + PAPA[0], w), () =>
			{
				double a = an != null ? an.Value : (R() < 0.5 ? 0 : Math.PI / 2) + (R() - 0.5) * 0.8, cx = clamp(PXp(), 40, W - 40), cy = clamp(PYp(), 40, H - 40);
				double c = JS.cos(a), s = JS.sin(a); var pts = new List<double[]>(); var sy = o.syl ?? PAPA; int n = sy.Length, mid = (int)Math.Floor((n - 1) / 2.0); string shp = o.shape ?? "line";
				for (int i = 0; i < n; i++)
				{
					double d = (i - mid) * sp; double px = cx + c * d, py = cy + s * d;
					if (shp == "arc") { double R2l = sp * 3.2, th = (i - mid) * sp / R2l; px = cx - s * R2l + s * R2l * JS.cos(th) + c * R2l * JS.sin(th); py = cy + c * R2l - c * R2l * JS.cos(th) + s * R2l * JS.sin(th); }
					else if (shp == "zig") { double z = (i - mid) % 2 != 0 ? 60 : 0; px += -s * z; py += c * z; }
					pts.Add(new[] { clamp(px, r, W - r), clamp(py, r, H - r) });
				}
				hazards.Add(L10Pave(pts, sy.Select(q => B(b0 + q)).ToArray(), B(wB(b0 + PAPA[0], w)), r));
			});
		}

		/* ---------- phase 1: the Ritz (0 - 111) ---------- */
		keep(0, () => hazards.Add(L10Paris(B(0), B(111), 0)));
		// the intro: a carpet rolls out with every melody line and rolls back
		void carpet(double b, double y, double h, double dir) => ev(b - 2, () => hazards.Add(L10Carpet(y, h, dir, B(b - 2), B(b), B(b + 2), B(b + 4.5), B(b + 6))));
		carpet(2, 150, 120, 1); carpet(10, 570, 120, -1);
		// the melody notes: each note a crystal strand comes down from a chandelier and goes back up
		double[][] STR = { new double[] { 1, 0 }, new double[] { 3, 2 }, new double[] { 5.75, 3 }, new double[] { 7, 4 }, new double[] { 9.75, 4 }, new double[] { 11, 3 }, new double[] { 13, 1 }, new double[] { 15, 0 }, new double[] { 17, 0 }, new double[] { 18, 4 }, new double[] { 20, 1 }, new double[] { 21.75, 3 }, new double[] { 23, 2 }, new double[] { 25.75, 2 }, new double[] { 28, 1 }, new double[] { 29, 4 }, new double[] { 31, 2 } };
		for (int i = 0; i < STR.Length; i++) { double b = STR[i][0]; int k = (int)STR[i][1]; if (easy && i % 3 != 0) continue; double wb = Math.Max(0, b - (easy ? 2 : 1.5)); ev(wb, () => hazards.Add(L10Strand(L10CH[k], k == 2 ? 400 : 440, B(wb), B(b), B(b + 0.6), B(b + 1)))); }
		if (!easy) { papala(12, T, new L10PO { syl = new[] { 0, 0.5, 1, 2 }, sp = 150 }); papala(20, T, new L10PO { syl = new[] { 0, 0.5, 1, 2 }, sp = 150 }); }
		// 32 "une suite au Ritz": luggage trolleys come in and stop, and with "je n'en veux pas" they go back
		void trol(double b, double y, double dir, double xh, double len, double hold = 4, double back = 2) => ev(b - T.wn, () => hazards.Add(L10Trolley(y, dir, xh, len, B(b - T.wn), B(b), B(b + 2), B(b + 2 + hold), B(b + 2 + hold + back))));
		trol(32, 170, 1, 760, 560); trol(32.5, 550, -1, 520, 560); trol(34, 360, 1, 430, 330, 2);
		// 40 "bijoux de chez Chanel": perfume bottles spray, and with the refusal they are thrown away
		// each bottle puffs clouds that travel inwards and grow
		double[][] PF = { new[] { 30, 110, 0.35 }, new[] { W - 30, 610, Math.PI + 0.35 }, new[] { 30, 610, -0.35 }, new[] { W - 30, 110, Math.PI - 0.35 } }; var puffT = new[] { new List<double>(), new List<double>(), new List<double>(), new List<double>() };
		void puff(double hb, int k, double da, L10Tier T) { puffT[k].Add(B(hb)); double x = PF[k][0], y = PF[k][1], a = PF[k][2], an = a + da, v = T.sp * 1.25; ev(hb - T.wn, () => hazards.Add(L10Mist(x + JS.cos(a) * 40, y + JS.sin(a) * 40, JS.cos(an) * v, JS.sin(an) * v, 30, easy ? 80 : 96, B(hb - T.wn), B(hb), B(hb + 3.2)))); }
		foreach (var pv in new[] { new[] { 40.5, 0, 0 }, new[] { 41.5, 1, 0 }, new[] { 42.5, 2, 0.1 }, new[] { 43.5, 3, -0.1 }, new[] { 44.5, 0, 0.45 }, new[] { 44.5, 1, 0.45 }, new[] { 45.5, 2, -0.4 }, new[] { 45.5, 3, 0.4 } }) { double b = pv[0]; int k = (int)pv[1]; double da = pv[2]; if (!easy || b % 1 == 0.5 && k < 2 || b < 44) puff(b, k, da, T); }
		ev(38, () => { for (int k = 0; k < 4; k++) { hazards.Add(L10Bottle(PF[k][0], PF[k][1], PF[k][2], B(38), puffT[k], B(46.5), B(48))); } });
		// 48 "une limousine, j'en ferais quoi": two limousines come in getting longer, the doors open, then they go back
		ev(46, () => { hazards.Add(L10Limo(230, 1, B(46), B(48), B(50), B(50.5), B(53), B(55), new[] { new double[] { 300, 0 }, new double[] { 690, 0 }, new double[] { 1080, 0 } })); hazards.Add(L10Limo(490, -1, B(46.5), B(48.5), B(50.5), B(51), B(53.5), B(55.5), new[] { new double[] { 500, 1 }, new double[] { 890, 1 } })); });
		papala(56, T);
		// 52 "j'en ferais quoi" and the papala: a champagne fountain from the glass tower, the drops go up and fall back
		double[] FT = easy ? new double[] { 56 } : new double[] { 52, 56, 60 };
		void fount(double hb, int n, double vxm, double v0)
		{
			double gr = 900, ox = W / 2, oy = H - 150; var V = new List<double[]>(); for (int i = 0; i < n; i++) { double u = (double)i / (n - 1) * 2 - 1; V.Add(new[] { u * vxm, -v0 * (1 - 0.18 * Math.Abs(u)) }); }
			double wb = hb - (easy ? 2 : 1.5); ev(wb, () => hazards.Add(L10Arcs(ox, oy, V, gr, B(wb), B(hb)))); ev(hb, () => { foreach (var vv in V) hazards.Add(L10Drop(ox, oy, vv[0], vv[1], gr)); burst(ox, oy, PINK, 10, 260); });
		}
		ev(50, () => hazards.Add(L10Glasses(B(50), B(64), FT.Select(b => B(b)).ToArray()))); for (int i = 0; i < FT.Length; i++) fount(FT[i], easy ? 6 : 8, i % 2 != 0 ? 230 : 420, i % 2 != 0 ? 980 : 860);
		// 64 "du personnel": butlers throw trays that fly and come back
		double[][] BU = { new[] { 260, H - 44 }, new[] { 640, H - 44 }, new[] { 1020, H - 44 } }; var trayT = new List<double[]>();
		void tray(double b, int k, double ang, double ax, double ay, double dur = 4) { trayT.Add(new double[] { k, b }); ev(b - T.wn, () => { double x = BU[k][0], y = BU[k][1]; hazards.Add(L10Tray(x, y - 40, JS.cos(ang), JS.sin(ang), ax, ay * (R() < 0.5 ? 1 : -1), B(b - T.wn), B(b), B(b + dur))); }); }
		foreach (var tv in new[] { new[] { 64, 0, -1.2 }, new[] { 64, 2, -1.94 }, new[] { 66, 1, -1.57 }, new[] { 67, 0, -0.75 }, new[] { 67, 2, -2.39 }, new[] { 69, 1, -1.25 }, new[] { 70, 0, -1.0 }, new[] { 70, 2, -2.14 } }) tray(tv[0], (int)tv[1], tv[2], easy ? 230 : 270, 110);
		ev(62, () => { for (int k = 0; k < 3; k++) hazards.Add(L10Butler(BU[k][0], BU[k][1], B(62), B(75), trayT.Where(q => q[0] == k).Select(q => B(q[1])).ToList())); });
		// 72 "un manoir": the palace gate closes leaving a door at the bottom middle, and "ce n'est pas pour moi" opens it
		sweep(70, () => L10Gate(W / 2, 400, 105, B(70), B(72.5), B(75.5), B(77), B(79)));
		// 80 "la Tour Eiffel, j'en ferais quoi": the Eiffel tower shows far away, and a pink lying tower comes in and is pushed out
		ev(78, () => hazards.Add(L10FarTower(W * 0.8, B(78), B(110))));
		ev(79, () => hazards.Add(L10TowerSide(360, 250, 250, B(79), B(81), B(83.5), B(85.5), B(87.5))));
		// velvet curtains on both sides open and close with the swing (when the square is empty)
		ev(50, () => { var sg = new List<double[]> { new[] { B(51.5), B(72) }, new[] { B(78), B(101) } }; hazards.Add(L10Curtain(-1, sg, easy ? 30 : 45, easy ? 80 : 110)); hazards.Add(L10Curtain(1, sg, easy ? 30 : 45, easy ? 80 : 110)); });
		papala(88, T); carpet(89, 150, 110, -1); if (!easy) foreach (var bk in new[] { new double[] { 91, 4 }, new double[] { 92, 1 }, new double[] { 93, 3 }, new double[] { 94, 0 } }) { double b = bk[0]; int k = (int)bk[1]; ev(b - 1.5, () => hazards.Add(L10Strand(L10CH[k], 440, B(b - 1.5), B(b), B(b + 0.6), B(b + 1)))); }
		// 96 the bridge: the refused gifts are thrown out of the hotel windows, fall, bounce on the floor and leave
		double[][] TS = { new double[] { 97, 200, 1 }, new double[] { 98, 440, 1 }, new double[] { 99, 680, 1 }, new double[] { 100, 920, -1 }, new double[] { 101, 1080, -1 }, new double[] { 102, 840, -1 }, new double[] { 103, 600, -1 }, new double[] { 104, 360, 1 }, new double[] { 105, 160, 1 }, new double[] { 106, 320, 1 }, new double[] { 106, 960, -1 }, new double[] { 107.5, 640, 1 }, new double[] { 107.5, 640, -1 }, new double[] { 108.5, 180, 1 }, new double[] { 108.5, 1100, -1 } };
		for (int i = 0; i < TS.Length; i++) { double b = TS[i][0], x = TS[i][1], d = TS[i][2]; if (easy && i % 2 != 0) continue; double wb = b - (easy ? 2 : 1.5); int i2 = i; ev(wb, () => hazards.Add(L10Toss(x, 96, d * (150 + 70 * (i2 % 3)), -240, 900, B(wb), B(b), i2 % 3))); }
		// the stage curtain closes leaving an opening (each time far from the last one, sometimes the opening moves and you follow it)
		void stage(double b, double sx, double sx2) { double wb = b - (easy ? 3 : 2.5), sw = easy ? 300 : 230, hd = sx2 == sx ? 2.5 : 3.2; sweep(wb, () => A2 ? L10Stage(sx, sw, B(wb), B(b), B(b + 1), B(b + hd), B(b + hd + 1), sx2) : L10Nil()); }
		foreach (var sv in new[] { new double[] { 6.5, 900 }, new double[] { 14.5, 520 }, new double[] { 22.5, 140, 300 }, new double[] { 30.5, 700 }, new double[] { 37.5, 1080 }, new double[] { 44, 700, 540 }, new double[] { 51, 160 }, new double[] { 58, 560 }, new double[] { 66, 940, 1100 }, new double[] { 84, 300 }, new double[] { 91, 680, 840 }, new double[] { 98, 380 }, new double[] { 104, 780 }, new double[] { 109.5, 1100 } }) stage(sv[0], sv[1], sv.Length > 2 ? sv[2] : sv[1]);
		void lanes(double b, int free) { var ys = new double[] { 90, 270, 450, 630 }; for (int i = 0; i < 4; i++) if (i != free) carpet(b, ys[i], easy ? 150 : 170, i % 2 != 0 ? -1 : 1); }
		lanes(18, 1); lanes(26, 2);
		fx(112, 0.45, 16);

		/* ---------- phase 2: Montmartre square, the flock (112 - 263) ---------- */
		T = TI[1]; double K2 = kf(T);
		keep(110, () => hazards.Add(L10Paris(B(110), B(263), 1)));
		// 112 "Je veux d'l'amour": a pigeon heart forms around you with a gap in it, closes on you, and bursts outwards
		void heartTrap(double b, int n, double S0, int gapN, double sh = 0.3)
		{
			sweep(b - 2, () =>
			{
				double cx = A2 ? clamp(PXp(), 40, W - 40) : clamp(PXp(), S0 + 20, W - S0 - 20), cy = A2 ? clamp(PYp(), 40, H - 40) : clamp(PYp(), S0 * 0.9 + 20, H - S0 - 20);
				// the gap is always inside the screen (even when you are at the edge)
				var gOk = Enumerable.Range(0, n).Where(j => { var hh = L10heart(TAU * ((j + gapN / 2.0) % n) / n); double x = cx + hh[0] * S0, y = cy + hh[1] * S0; return x > 60 && x < W - 60 && y > 60 && y < H - 60; }).ToList();
				int g0 = gOk.Count != 0 ? gOk[(int)Math.Floor(R() * gOk.Count)] : (int)Math.Floor(R() * n); double tw = B(b - 2), tA = B(b), tF = B(b + 3), tC0 = B(b + 5), tC1 = B(b + 7), tX = B(b + 7);
				Func<int, bool> skip = i => ((i - g0 + n) % n) < gapN; Func<int, double> ang0 = i => TAU * i / n;
				Func<double, double> sc = t => t < tC0 ? S0 : t < tC1 ? S0 * (1 - (1 - sh) * L10ease((t - tC0) / (tC1 - tC0))) : S0 * sh;
				Func<int, double, double[]> pos = (i, t) =>
				{
					if (skip(i)) return null; var hh = L10heart(ang0(i)); double hx = hh[0], hy = hh[1], s = sc(t), tx = cx + hx * s, ty = cy + hy * s;
					if (t < tF) { double k = L10ease((t - tA) / (tF - tA)), a = JS.atan2(hy, hx), sx = cx + JS.cos(a) * 900, sy = cy + JS.sin(a) * 900; return new[] { sx + (tx - sx) * k, sy + (ty - sy) * k }; }
					if (t < tX) return new[] { tx, ty }; double e = t - tX, a2 = JS.atan2(hy, hx); return new[] { tx + JS.cos(a2) * e * 520 * K2, ty + JS.sin(a2) * e * 520 * K2 };
				};
				Action<double, double> gh = (t, k) =>
				{
					g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; L10dash(true); g.beginPath(); for (int i = 0; i <= n; i++) { var hh = L10heart(ang0(i)); double x = cx + hh[0] * S0, y = cy + hh[1] * S0; if (i != 0) g.lineTo(x, y); else g.moveTo(x, y); }
					g.stroke(); L10dash(false);
					g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.5 * k)})"; g.lineWidth = 5; g.beginPath(); for (int i = 0; i < gapN; i++) { var hh = L10heart(ang0((g0 + i) % n)); double x = cx + hh[0] * S0, y = cy + hh[1] * S0; if (i != 0) g.lineTo(x, y); else g.moveTo(x, y); }
					g.stroke();
				};
				return L10Flock(n, pos, tw, tA, tX + 1.8, new L10FO { gh = gh });
			});
		}
		heartTrap(112, easy ? 44 : 56, 215, easy ? 10 : 8, A2 ? 0.04 : 0.3);
		// 120 "votre argent": the flock forms a € in the middle, then breaks: the arc to the left and the two lines to the right
		ev(118, () =>
		{
			double cx = W / 2 + 40, cy = H / 2, Rr = 165; int n1 = easy ? 26 : 34, n2 = 12; var pts = new List<double[]>(); for (int i = 0; i < n1; i++) { double a = 0.75 + (TAU - 1.5) * i / (n1 - 1); pts.Add(new[] { cx + JS.cos(a) * Rr, cy + JS.sin(a) * Rr, -1 }); }
			foreach (var yy in new double[] { -38, 38 }) for (int i = 0; i < n2; i++) pts.Add(new[] { cx - 230 + i * 24, cy + yy, 1 });
			double tA = B(120), tF = B(122.5), tX = B(125), tw = B(118);
			Func<int, double, double[]> pos = (i, t) => { double x = pts[i][0], y = pts[i][1], d = pts[i][2]; if (t < tF) { double k = L10ease((t - tA) / (tF - tA)), sx = x < cx ? -80 : W + 80, sy = y + (i % 2 != 0 ? -200 : 200); return new[] { sx + (x - sx) * k, sy + (y - sy) * k }; } if (t < tX) return new[] { x, y }; return new[] { x + d * (t - tX) * (d < 0 ? 560 : 620) * K2, y }; };
			hazards.Add(L10Flock(pts.Count, pos, tw, tA, B(132), new L10FO { early = true, gh = (t, k) => { g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; g.lineWidth = 2; L10dash(true); g.beginPath(); g.arc(cx, cy, Rr, 0.75, TAU - 0.75); g.stroke(); g.beginPath(); g.moveTo(cx - 230, cy - 38); g.lineTo(cx + 46, cy - 38); g.moveTo(cx - 230, cy + 38); g.lineTo(cx + 46, cy + 38); g.stroke(); L10dash(false); } }));
		});
		word(121, "€", W / 2 + 40, H / 2, 40, 3, "rgba(255,255,255,0.25)");
		// 128 "la main sur le cœur": a big heart in the middle beats twice and grows, then breaks into pigeons flying out
		ev(126, () =>
		{
			int n = easy ? 36 : 46; double cx = W / 2, cy = H / 2, S = 150, tA = B(126.5), tF = B(128.5), tX = B(133);
			Func<double, double> sc = t => { double s = S; foreach (var pb in new double[] { 129, 131, 132 }) { double e = (t - B(pb)) / SPB; if (e > -0.3 && e < 1.2) s += S * 0.42 * (e < 0 ? 1 + e / 0.3 : Math.Max(0, 1 - e / 1.2)); } return s; };
			Func<int, double, double[]> pos = (i, t) =>
			{
				double th = TAU * i / n; var hh = L10heart(th); double hx = hh[0], hy = hh[1], s = sc(t), tx = cx + hx * s, ty = cy + hy * s; if (t < tF) { double k = L10ease((t - tA) / (tF - tA)), a = JS.atan2(hy, hx); return new[] { cx + JS.cos(a) * 900 + (tx - cx - JS.cos(a) * 900) * k, cy + JS.sin(a) * 900 + (ty - cy - JS.sin(a) * 900) * k }; }
				if (t < tX) return new[] { tx, ty }; double e = t - tX, a2 = JS.atan2(hy, hx); return new[] { tx + JS.cos(a2) * e * 300 * K2, ty + JS.sin(a2) * e * 300 * K2 };
			};
			hazards.Add(L10Flock(n, pos, B(126), tA, B(140)));
		});
		papala(136, T, new L10PO { r = 34, shape = "arc" });
		// Montmartre windmills on the sides, their sails turn a step with every beat
		var MS = new List<double>(); for (double b = 127; b < 141; b += easy ? 4 : 2) MS.Add(b);
		ev(124.5, () => { hazards.Add(L10Mill(190, 250, easy ? 150 : 195, 1, 0.39, B(124.5), B(126.5), B(141), MS)); if (!easy) hazards.Add(L10Mill(W - 190, 250, easy ? 150 : 195, -1, -0.39, B(124.5), B(126.5), B(141), MS)); });
		// 144 "Allons ensemble": two flocks fly together and braid (top, then bottom)
		void braid(double b, double cy, double A, double dir) => ev(b - 2, () =>
		{
			int n = easy ? 18 : 25; double v = 300 * K2, lam = 520, tA = B(b), x0 = dir < 0 ? W + 40 : -40;
			Func<int, double, double[]> pos = (i, t) => { int k = i % 2, j = i >> 1; double x = x0 - dir * j * 24 + dir * v * (t - tA); if (x < -60 || x > W + 60) return null; return new[] { x, cy + A * JS.sin(TAU * (x - x0) / lam + k * Math.PI + (t - tA) * 1.2) }; };
			hazards.Add(L10Flock(2 * n, pos, B(b - 2), tA, tA + (W + n * 24 + 120) / v, new L10FO { gh = (t, k) => { g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; double ex = dir < 0 ? W - 14 : 14; g.fillRect(ex - 6, cy - A, 12, 2 * A); } }));
		});
		braid(144, 230, 140, -1); braid(149, 490, 140, 1);
		// 152 "vos clichés": camera flashes on you
		void photo(double hb, double w, double h, L10Tier T, double jx = 0, double jy = 0) { double wb = wB(hb, T.wn); sweep(wb, () => L10Photo(clamp(PXp() + jx, w / 2, W - w / 2), clamp(PYp() + jy, h / 2, H - h / 2), w, h, B(wb), B(hb), B(hb + 0.6))); }
		foreach (var b in new double[] { 146, 150, 154, 158 }) photo(b, 230, 170, T);
		// 160 "Bienvenue dans ma réalité": a big pigeon cloud crosses the square and stretches and shrinks with the rhythm
		void blob(double b, double x0, double y0, double x1, double y1, double Rb, double dur) => ev(b - 2.5, () =>
		{
			var P = new List<double[]> { new double[] { 0, 0 } }; foreach (var r in new[] { 0.25, 0.5, 0.75, 1 }) { double c = Math.Max(6, round(TAU * r * Rb / 24)); for (int i = 0; i < c; i++) P.Add(new[] { r, i * TAU / c + r * 2 }); }
			double tA = B(b), tE = B(b + dur); Func<double, double[]> ctr = t => { double k = (t - tA) / (tE - tA); return new[] { x0 + (x1 - x0) * k, y0 + (y1 - y0) * k }; };
			Func<int, double, double[]> pos = (i, t) => { double r = P[i][0], a = P[i][1]; var cc = ctr(t); double cx = cc[0], cy = cc[1], ph = Math.PI * BT(t), sx = 1 + 0.28 * JS.sin(ph), sy = 1 - 0.22 * JS.sin(ph), aa = a + (t - tA) * 0.9; return new[] { cx + JS.cos(aa) * r * Rb * sx, cy + JS.sin(aa) * r * Rb * sy }; };
			hazards.Add(L10Flock(P.Count, pos, B(b - 2.5), tA, tE, new L10FO { gh = (t, k) => { var cc = ctr(tA); var dd = ctr(tA + 0.5); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; L10dash(true); g.beginPath(); g.arc(clamp(cc[0], 40, W - 40), clamp(cc[1], 40, H - 40), Rb * 0.6, 0, TAU); g.stroke(); L10dash(false); } }));
		});
		blob(160, -180, 620, W + 180, 140, easy ? 105 : 120, 9); blob(163, W + 180, 560, -180, 200, easy ? 100 : 115, 9);
		// 176 the break: the band stops and hits 4 times, each hit a photographers' flash on you
		foreach (var b in new double[] { 177, 179, 181, 183 }) photo(b, 260, 190, T);
		fx(176, 0.2, 6); ev(174, () => hazards.Add(L10Papz(B(174), B(184), new double[] { 177, 179, 181, 183 }.Select(b => B(b)).ToArray())));
		// 184 "vos bonnes manières": the napkin corners fold on the rhythm
		void nap(double hb, double cx, double cy, double L, L10Tier T) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L10Napkin(cx, cy, L, B(wb), B(hb), B(hb + 1.2)))); }
		nap(186, 0, 0, 560, T); nap(187.5, W, H, 560, T); nap(189, W, 0, 560, T); nap(190.5, 0, H, 560, T);
		// 192 "je mange avec les mains": a baguette across the screen broken by hand
		void bag(double hb, bool vert, double c, double kx, L10Tier T) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L10Baguette(vert, c, kx, B(wb), B(hb), B(hb + 1.5), B(hb + 2.6)))); }
		bag(193, false, 250, 700, T); bag(194, true, 820, 420, T); bag(196, false, 520, 480, T); bag(197, true, 400, 260, T); if (!easy) { bag(198.5, false, 140, 900, T); bag(199, true, 1050, 560, T); }
		// 200 "J'parle fort": pigeons fly in from the four sides to the middle, then turn and go out in a spiral
		ev(198, () =>
		{
			int m = easy ? 10 : 13; double cx = W / 2, cy = H / 2, tA = B(200), tM = B(203); int Arms = 4; double[][] sides = { new double[] { -1, 0 }, new double[] { 1, 0 }, new double[] { 0, -1 }, new double[] { 0, 1 } };
			Func<int, double, double[]> pos = (i, t) =>
			{
				int k = i / m, j = i % m; if (t < tM) { double dx = sides[k][0], dy = sides[k][1], d0 = (dx != 0 ? W / 2 : H / 2) + 60 + j * 26, d = d0 - (d0 - 40) * L10ease((t - tA) / (tM - tA)); if (d < 36) return null; return new[] { cx + dx * d, cy + dy * d }; }
				double tj = tM + j * 0.07; if (t < tj) return null; double e = t - tj, r = 40 + e * 330 * K2, a = k * TAU / Arms + 0.6 + e * 1.25; if (r > 900) return null; return new[] { cx + JS.cos(a) * r, cy + JS.sin(a) * r };
			};
			hazards.Add(L10Flock(4 * m, pos, B(198), tA, B(214), new L10FO { gh = (t, k) => { g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; foreach (var sd in sides) { double dx = sd[0], dy = sd[1]; double x = dx < 0 ? 6 : dx > 0 ? W - 18 : cx - 6, y = dy < 0 ? 6 : dy > 0 ? H - 18 : cy - 6; g.fillRect(x, y, 12, 12); } } }));
		});
		// 208 "l'hypocrisie": the cafe parasols on top and bottom open and close
		ev(206, () =>
		{
			for (int i = 0; i < 5; i++)
			{
				double x = 128 + i * 256; var ops = new List<double[]>(); var ops2 = new List<double[]>(); for (double b = 209; b < 216; b += 2) { if ((i + ((b - 209) / 2)) % 2 == 0) ops.Add(new[] { B(b), B(b + 1.3) }); else ops2.Add(new[] { B(b), B(b + 1.3) }); }
				if (ops.Count != 0) hazards.Add(L10Parasol(x, true, 200, B(206), ops)); if (ops2.Count != 0) hazards.Add(L10Parasol(x, false, 200, B(206), ops2));
			}
		});
		// 216 "langues de bois": wooden planks come out of the sides in turn and make a zigzag path
		double[] PY = { 90, 210, 330, 450, 570, 690 };
		ev(214, () => { for (int k = 0; k < 6; k++) { double dir = k % 2 != 0 ? -1 : 1, ta = 216 + k * 0.5, tb = 221 + (5 - k) * 0.5; hazards.Add(L10Plank(PY[k] - 30, dir, W * 0.74, 56, B(214), B(ta), B(ta + 0.6), B(tb), B(tb + 0.8))); } });
		// 224 "Regardez-moi": pigeon waves cross (top, bottom, middle)
		void front(double b, double cy, double dir, double v = 520) => ev(b - 2, () =>
		{
			int n = 10; double tA = B(b), x0 = dir > 0 ? -40 : W + 40;
			Func<int, double, double[]> pos = (i, t) => { int row = i < n ? 0 : 1, j = i % n; double x = x0 + dir * v * K2 * (t - tA) - dir * row * 70, y = cy - 108 + j * 24 + JS.sin((t - tA) * 7 + j * 0.5) * 12; if (x < -60 || x > W + 60) return null; return new[] { x, y }; };
			hazards.Add(L10Flock(2 * n, pos, B(b - 2), tA, tA + (W + 200) / (v * K2), new L10FO { gh = (t, k) => { g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.fillRect(dir > 0 ? 0 : W - 12, cy - 110, 12, 220); } }));
		});
		front(170, 250, 1, 600); front(171, 470, -1, 600);
		front(224, 170, 1); front(226, 550, -1); front(228, 360, 1); front(230, 170, -1); front(230, 550, -1);
		// 232 "j'suis comme ça (j'suis comme ça)": stones twice
		papala(236.9, T, new L10PO { syl = new[] { 0, 1.05, 2.0 }, sp = 150, r = 34, shape = "zig" });
		papala(249.3, T, new L10PO { syl = new[] { 0, 0.57, 1.03, 1.74 }, sp = 140, r = 34, shape = "arc" });
		photo(251, 240, 180, T);

		papala(256, T, new L10PO { r = 34, shape = "zig" });
		// cafe awnings on top and bottom when the square is empty
		ev(138, () => { var sg = new List<double[]> { new[] { B(140), B(148) }, new[] { B(174), B(186) }, new[] { B(200), B(208) }, new[] { B(256), B(263) } }; hazards.Add(L10Awning(true, sg, easy ? 30 : 40, easy ? 70 : 95)); hazards.Add(L10Awning(false, sg, easy ? 30 : 40, easy ? 70 : 95)); });
		// 232 "j'suis comme ça": washing lines across the square (real life), the clothes swing and drift with the wind
		var LR1 = easy ? new[] { (180.0, "s"), (640.0, "d"), (1100.0, "s") } : new[] { (60.0, "s"), (200.0, "t"), (330.0, "d"), (470.0, "k"), (570.0, "s"), (720.0, "t"), (850.0, "s"), (1000.0, "d"), (1140.0, "k"), (1230.0, "s") };
		var LR2 = easy ? new[] { (420.0, "d"), (860.0, "s") } : new[] { (110.0, "t"), (360.0, "s"), (640.0, "d"), (900.0, "k"), (1150.0, "t") };
		ev(229, () => { hazards.Add(L10Laundry(36, LR1, B(229), B(231), B(257))); hazards.Add(L10Laundry(460, LR2, B(229), B(231), B(257), 2, 0.82)); });
		// 260 the flock gathers and flies to the tower: a pigeon column rises from the bottom middle
		ev(258, () =>
		{
			int n = 40; double tA = B(260), v = 520 * K2; Func<int, double, double[]> pos = (i, t) => { int k = i % 2, j = i >> 1; double y = H + 40 + j * 30 - v * (t - tA), x = W / 2 + (k != 0 ? -1 : 1) * (46 + JS.sin(j * 0.6 + (t - tA) * 4) * 40); if (y < -60 || y > H + 60) return null; return new[] { x, y }; };
			hazards.Add(L10Flock(n, pos, B(258), tA, tA + (H + n * 15 + 120) / v, new L10FO { gh = (t, k) => { g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.fillRect(W / 2 - 100, H - 12, 200, 12); } }));
		});
		// a pigeon wave as tall as the screen crosses, the gap in a new place each time
		void wave(double b, double dir, double gy, double sway = 0) { double wb = b - (easy ? 3.5 : 3); sweep(wb, () => A2 ? L10WaveFlock(dir, gy, easy ? 230 : 180, easy ? 400 : 420 * K2, B(wb), B(b), sway) : L10Nil()); }
		foreach (var wv in new[] { new double[] { 123, 1, 330 }, new double[] { 129, -1, 620 }, new double[] { 139, 1, 140 }, new double[] { 168, 1, 360, 60 }, new double[] { 187, -1, 400 }, new double[] { 196, 1, 110 }, new double[] { 208, -1, 650 }, new double[] { 220, 1, 140 }, new double[] { 228, 1, 590 }, new double[] { 236, -1, 330, 40 }, new double[] { 244, -1, 590 }, new double[] { 252, 1, 140 }, new double[] { 260, -1, 590 } }) wave(wv[0], wv[1], wv[2], wv.Length > 3 ? wv[3] : 0);
		fx(264, 0.45, 16);

		/* ---------- phase 3: the boss, the Eiffel tower at night (264 - 391) ---------- */
		T = TI[2]; double K3 = kf(T);
		keep(260, () => hazards.Add(L10Paris(B(260), B(391), 2)));
		Func<double, double> lean = t => { double b = BT(t); if (b < 296 || b >= 314) return 0; double e = b - 296, k = min(1, e / 2, (314 - b) / 2); return JS.sin(Math.PI * e / 4) * 0.82 * k; };
		L10EiffelHz TOWER = null; keep(261, () => { TOWER = L10Eiffel(B(261), lean, B(386), B(390), 0); hazards.Add(TOWER); });
		Func<double, double[]> tTop = t => TOWER != null ? TOWER.top(t) : new[] { W / 2, 180.0 };
		// 264 "Je veux d'l'amour": the floor girders come out in order 1 2 3 (with a gap in them)
		double[] FY = { H - 160, H - 320, H - 470 };
		void gird(double hb, double y, double gx, double gw, L10Tier T, double hold = 1.6) { double wb = wB(hb, T.wn); ev(wb, () => hazards.Add(L10Girder(y, gx, gw, B(wb), B(hb), B(hb + hold), B(hb + hold + 0.7)))); }
		double[][] GX = { new double[] { 240, 980, 560 }, new double[] { 700, 300, 1000 }, new double[] { 860, 420, 180 }, new double[] { 380, 900, 640 } };
		for (int r = 0; r < 4; r++) { double b0 = 265 + r * 4; bool up = r % 2 == 0; for (int k = 0; k < 3; k++) { int fl = up ? k : 2 - k; if (r == 2 && k == 1) continue; gird(b0 + k, FY[fl], GX[r][k], easy ? 190 : 150, T, 2.6 - k * 0.6); } }
		// the third group: instead of the middle girder, the lift shoots X pieces right and left
		ev(274, () => { foreach (var sd in new double[] { -1, 1 }) for (int i = 0; i < (easy ? 2 : 3); i++) { double a = (sd < 0 ? Math.PI : 0) + (i - 1) * 0.3; hazards.Add(L10Cross(W / 2, FY[1], JS.cos(a) * T.sp * 0.9, JS.sin(a) * T.sp * 0.9, sd * 7)); } burst(W / 2, FY[1], PINK, 10, 260); });
		// 280 "la main sur le cœur": the lift goes up and down and throws X pieces
		double[][] LS = { new[] { 280, H - 60 }, new[] { 281, H - 200 }, new[] { 283, H - 400 }, new[] { 285, H - 120 }, new[] { 287, H - 330 }, new[] { 289, H - 60 } };
		Func<double, double> liftY = t => { double b = BT(t); for (int i = 0; i < LS.Length - 1; i++) { double b0 = LS[i][0], y0 = LS[i][1], b1 = LS[i + 1][0], y1 = LS[i + 1][1]; if (b < b1) { double k = L10ease((b - b0) / (b1 - b0)); return y0 + (y1 - y0) * k; } } return LS[LS.Length - 1][1]; };
		ev(279, () => hazards.Add(L10Lift(B(279), B(280), B(289), liftY)));
		void crossFan(double hb, int n, double sp, double a0, double spr) => ev(hb, () => { double y = liftY(B(hb)); for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2.0) * spr; hazards.Add(L10Cross(W / 2, y, JS.cos(a) * sp, JS.sin(a) * sp, (i % 2 != 0 ? -1 : 1) * 7)); } burst(W / 2, y, PINK, 10, 260); });
		foreach (var bs in new[] { new double[] { 281, -1 }, new double[] { 283, 1 }, new double[] { 285, -1 }, new double[] { 287, 1 } }) { double b = bs[0], sd = bs[1]; crossFan(b, easy ? 4 : 6, T.sp * 0.9, sd < 0 ? Math.PI : 0, 0.32); crossFan(b + 0.5, easy ? 3 : 5, T.sp * 0.8, sd < 0 ? 0 : Math.PI, 0.36); }
		foreach (var b in new double[] { 282, 286 }) gird(b, FY[0], b == 282 ? 260 : 1020, 170, T, 1.4);
		papala(288, T, new L10PO { r = 36, shape = "arc" });
		// 296 "Allons ensemble": the tower leans right and left, its top sweeping arcs, and throws X pieces from its top
		// in turn: a fan at you, and a slower half circle downwards (a new shape each time)
		for (double bi = 297; bi < 312; bi += 2)
		{
			double b = bi;
			ev(b, () =>
			{
				var xy = tTop(B(b)); double x = xy[0], y = xy[1]; bool odd = ((b - 297) / 2) % 2 == 1;
				if (!odd) { double a0 = JS.atan2(PYp() - y, PXp() - x); int n = easy ? 3 : 5; for (int i = 0; i < n; i++) { double a = a0 + (i - (n - 1) / 2.0) * 0.34; hazards.Add(L10Cross(x, y, JS.cos(a) * T.sp * 0.85, JS.sin(a) * T.sp * 0.85, 7)); } }
				else { int n = easy ? 5 : 7; for (int i = 0; i < n; i++) { double a = 0.2 + (Math.PI - 0.4) * i / (n - 1); hazards.Add(L10Cross(x, y, JS.cos(a) * T.sp * 0.6, JS.sin(a) * T.sp * 0.6, -6, 16)); } }
			});
		}
		foreach (var bg in new[] { new[] { 300, FY[0], 330 }, new[] { 304, FY[0], 950 }, new[] { 308, FY[0], 640 } }) gird(bg[0], bg[1], bg[2], 190, T, 1.6);
		// "vos clichés": a flash
		photo(305, 240, 180, T); gird(295, FY[1], 640, easy ? 220 : 190, T, 1.4);
		// 312 "Bienvenue dans ma réalité": the tower lights light up the whole screen except a place inside the tower
		void wel(double hb, Action drawSafe, Func<double, double, double, bool> isSafe, L10Tier T) { double wb = hb - (easy ? 6.5 : 6); sweep(wb, () => L10Welcome(drawSafe, isSafe, B(wb), B(hb), B(hb + 1.4), 2 * SPB)); }
		Action arch = () => { g.moveTo(W / 2 + 150, H); g.arc(W / 2, H, 150, 0, Math.PI, true); g.closePath(); }; Func<double, double, double, bool> archOk = (x, y, m) => hypot(x - W / 2, y - H) < 150 - m;
		Action win1 = () => { g.moveTo(W / 2 - 150, H - 160); g.lineTo(W / 2 + 150, H - 160); g.lineTo(W / 2 + 70, H - 320); g.lineTo(W / 2 - 70, H - 320); g.closePath(); };
		Func<double, double, double, bool> win1Ok = (x, y, m) => { if (y > H - 160 - m || y < H - 320 + m) return false; double u = (H - 160 - y) / 160, hw = 150 - 80 * u; return Math.Abs(x - W / 2) < hw - m; };
		Action win2 = () => { g.moveTo(W / 2 - 70, H - 320); g.lineTo(W / 2 + 70, H - 320); g.lineTo(W / 2 + 26, H - 470); g.lineTo(W / 2 - 26, H - 470); g.closePath(); };
		Func<double, double, double, bool> win2Ok = (x, y, m) => { if (y > H - 320 - m || y < H - 470 + m) return false; double u = (H - 320 - y) / 150, hw = 70 - 44 * u; return Math.Abs(x - W / 2) < hw - m; };
		wel(315, arch, archOk, T); wel(319, win1, win1Ok, T); wel(323, arch, archOk, T); wel(327, easy ? win1 : win2, easy ? win1Ok : win2Ok, T);
		foreach (var b in new[] { 316.5, 320.5, 324.5 }) ev(b, () => { var xy = tTop(B(b)); double x = xy[0], y = xy[1]; for (int i = 0; i < (easy ? 8 : 12); i++) { double a = i * TAU / (easy ? 8 : 12) + b; hazards.Add(L10Twinkle(x, y, JS.cos(a) * T.sp * 0.7, JS.sin(a) * T.sp * 0.7)); } });
		// 328 the solo: the tower sparkles, its shape grows outwards with a gap in it, and light stars come out of its top in a spiral
		void spark(double hb, double gapA, L10Tier T) { double wb = wB(hb, 2); ev(wb, () => hazards.Add(L10SparkShape(W / 2, H - 270, B(hb), T.sp * 0.62, gapA, easy ? 1.1 : 0.8, B(wb)))); }
		double[] SG = { -1.57, -0.6, -2.5, 0.4, -1.2, 2.7, -2.0, -0.9, 1.6, -1.57, 0.8, -2.3, -0.3, -1.9, 2.2 };
		for (int i = 0; i < 10; i++) spark(330 + i * 5.5, SG[i], T);
		// spiral stars: 3 turning arms
		// every 8 beats a new shape: spiral, rings, faster reverse spiral, fans at you, two opposite spirals, alternating rings, an accelerating spiral
		void star(double x, double y, double a, double sp) => hazards.Add(L10Twinkle(x, y, JS.cos(a) * sp, JS.sin(a) * sp));
		for (double bi = 332; bi < 386; bi += 0.5)
		{
			double b = bi, ph = Math.Floor((b - 332) / 8), q = b - 332 - ph * 8; bool on = Math.Floor(b) == b; if (easy && !on) continue;
			if (ph == 0 || ph == 6) ev(b, () => { var xy = tTop(B(b)); int arms = easy ? 3 : 4; double a0 = (b - 332) * (ph != 0 ? 0.55 : 0.42); for (int k = 0; k < arms; k++) star(xy[0], xy[1], a0 + k * TAU / arms, T.sp * 0.62); });
			else if ((ph == 1 || ph == 5) && on && q < 4) ev(b, () => { var xy = tTop(B(b)); int n = easy ? 7 : 10; double o2 = ph == 5 && q % 2 != 0 ? Math.PI / n : 0; for (int k = 0; k < n; k++) star(xy[0], xy[1], o2 + k * TAU / n + (ph == 1 ? b * 0.3 : 0), T.sp * 0.55); });
			else if (ph == 2) ev(b, () => { var xy = tTop(B(b)); int arms = easy ? 2 : 3; double a0 = -(b - 332) * 0.6; for (int k = 0; k < arms; k++) star(xy[0], xy[1], a0 + k * TAU / arms, T.sp * 0.66); });
			else if (ph == 3 && on && q < 4) ev(b, () => { var xy = tTop(B(b)); double x = xy[0], y = xy[1], a0 = JS.atan2(PYp() - y, PXp() - x); int n = easy ? 3 : 5; for (int k = 0; k < n; k++) star(x, y, a0 + (k - (n - 1) / 2.0) * 0.3, T.sp * 0.7); });
			else if (ph == 4) ev(b, () => { var xy = tTop(B(b)); double a = (b - 332) * 0.5; foreach (var sg in easy ? new double[] { 1 } : new double[] { 1, -1 }) for (int k = 0; k < 2; k++) star(xy[0], xy[1], sg * a + k * Math.PI, T.sp * 0.6); });
		}
		// papala in the solo
		papala(344, T, new L10PO { r = 36, shape = "zig" }); papala(360, T, new L10PO { r = 36, shape = "arc" }); papala(376, T, new L10PO { r = 40, sp = 140 });
		// a girder sweeps the screen (sometimes down, sometimes up), the gap in a new place each time, sometimes the gap moves
		void scan(double b, double gx, bool up, double gx2) { double wb = b - (easy ? 3.5 : 3); sweep(wb, () => A2 ? L10Scan(up ? 760 : -40, up ? -40 : 760, gx, easy ? 260 : 200, B(wb), B(b), B(b + 4), 56, gx2) : L10Nil()); }
		foreach (var sv in new[] { new double[] { 267, 1000 }, new double[] { 274, 600, 1 }, new double[] { 284, 1000 }, new double[] { 293, 560, 1 }, new double[] { 300, 1000 }, new double[] { 307, 560, 1 }, new double[] { 331, 900 }, new double[] { 340, 500, 1 }, new double[] { 349, 900, 0, 1060 }, new double[] { 358, 600, 1 }, new double[] { 367, 1020, 0, 900 }, new double[] { 376, 520, 1 }, new double[] { 383, 900 } }) scan(sv[0], sv[1], sv.Length > 2 && sv[2] != 0, sv.Length > 3 ? sv[3] : sv[1]);
		// 386 charges white, 390 explodes (harmless), 391 the win
		ev(390, () => { for (int i = 0; i < 26; i++) { double a = -Math.PI * 0.1 - rnd() * Math.PI * 0.8; particles.Add(new Particle { x = W / 2 + (rnd() - 0.5) * 260, y = H - rnd() * 480, vx = JS.cos(a) * 260, vy = JS.sin(a) * 300, life = 1.6, max = 1.6, col = "rgba(215,215,230,0.8)", s = 7 }); } });
		fx(390, 0.7, 0);
		// each phase has its schedule: it starts with the phase (and a checkpoint brings it back from its place)
		sort(SW, (a, b) => a.t - b.t);
		foreach (var bb in new[] { new double[] { 0, 110 }, new double[] { 110, 262 }, new double[] { 262, 391 } }) { double b0 = bb[0], b1 = bb[1]; var L = SW.Where(q => q.t >= b0 && q.t < b1).Select(q => (t: B(q.t), f: q.f)).ToList(); if (L.Count != 0) ev(b0, () => hazards.Add(L10Sched(L))); }
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
