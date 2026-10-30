using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// شيلة "حان الوعد يا لابتي", main level 25: "the promise". The tribe's call at night, the raid in the dust, and the promise kept at dawn.
// Part 1 the call (the drum's rings, the spears, the Sadu weave's strips, the sword's slashes in the break), part 2 the raid (the horses'
// lanes, the arrows, the riders' circle, the falcon in the break), part 3 the promise at dawn (the ardah's sword walls, the banners, the
// crossed swords, all of it back once more, and the last X).
// The level is the song from 0:00 to 3:04 (songs/SONG_L25.mp3 ends there). Grid: 134.7 BPM, beat 0 = 0.38 s of the file.
// Every hazard is a pure function of time once it exists. Only pink hurts: the sand, the white marks and the gold never do.
public static partial class G
{
	static double WDk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string WDP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string WDW(double a) => $"rgba(240,234,222,{str(clamp(a, 0, 1))})";
	static string WDG(double a) => $"rgba(240,190,90,{str(clamp(a, 0, 1))})";
	const string WDDK = "#b8164e";   // the darker pink of the patterns drawn on what hurts
	static double WDEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double WDOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double WDRad(double d) => d * Math.PI / 180;
	static double WDH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
	static void WDDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double WDSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// how far a ray from (x,y) along (dx,dy) goes before it leaves the screen (plus m px)
	static double WDExit(double x, double y, double dx, double dy, double m = 40)
	{
		double best = 1e9;
		if (dx > 1e-6) best = Math.Min(best, (W + m - x) / dx); if (dx < -1e-6) best = Math.Min(best, (-m - x) / dx);
		if (dy > 1e-6) best = Math.Min(best, (H + m - y) / dy); if (dy < -1e-6) best = Math.Min(best, (-m - y) / dy);
		return Math.Max(0, best);
	}
	// the integral (degrees) of a speed (degrees a beat) that goes in straight lines between (kb[i], kw[i]), from kb[0] to bt
	static double WDTurn(double[] kb, double[] kw, double bt)
	{
		double a = 0;
		for (int i = 0; i + 1 < kb.Length; i++)
		{
			double b0 = kb[i], b1 = kb[i + 1]; if (bt <= b0) break; double e = Math.Min(bt, b1), w0 = kw[i], w1 = kw[i + 1], u = (e - b0) / (b1 - b0);
			a += (e - b0) * (w0 + (w0 + (w1 - w0) * u)) / 2;
		}
		if (bt > kb[kb.Length - 1]) a += (bt - kb[kb.Length - 1]) * kw[kw.Length - 1];
		return a;
	}
	// is the player inside the ellipse (cx,cy, semi-axes a along ang, b across)?
	static bool WDInEll(Player p, double cx, double cy, double ang, double a, double b)
	{
		double c = JS.cos(ang), s = JS.sin(ang), dx = p.x - cx, dy = p.y - cy, u = dx * c + dy * s, v = -dx * s + dy * c, m = p.r - 2;
		return (u * u) / ((a + m) * (a + m)) + (v * v) / ((b + m) * (b + m)) < 1;
	}

	// ---------- the stage (harmless, under everything) ----------
	// Night (part 1), dust (part 2), dawn (part 3): the sky, the stars, the dunes and the war drum in the middle while it beats.
	static readonly double[][] WDSKY = {
		new[] { 8.0, 12, 34, 2, 3, 12 },      // night: top, bottom
		new[] { 34.0, 20, 30, 12, 6, 10 },    // the raid's dust
		new[] { 40.0, 24, 52, 120, 52, 30 } };  // dawn
	public static Hz WDStage(double t1)
	{
		var h = new Hz { kind = "WDStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6);
			double k1 = WDk(bt, 100, 104), k2 = WDk(bt, 194, 200);
			double[] c = new double[6]; for (int i = 0; i < 6; i++) c[i] = WDSKY[0][i] + (WDSKY[1][i] - WDSKY[0][i]) * k1 + (WDSKY[2][i] - WDSKY[1][i]) * k2;
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(c[0])},{str(c[1])},{str(c[2])},1)"); gr.addColorStop(1, $"rgba({str(c[3])},{str(c[4])},{str(c[5])},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// stars, a little brighter on each beat (fade out at dawn)
			double pulse = 1 - WDk(bt - Math.Floor(bt), 0, 0.4), st = 1 - k2 * 0.85;
			g.fillStyle = WDW((0.35 + 0.25 * pulse) * st);
			for (int i = 0; i < 46; i++) { double sx = (i * 277.3) % W, sy = (i * 131.7) % (H * 0.62); circle(sx, sy, i % 7 == 0 ? 1.8 : 1.1); }
			// the dawn's sun rising at the bottom
			if (k2 > 0)
			{
				double sy = H + 120 - 150 * WDk(bt, 196, 412);
				var sg = g.createRadialGradient(W / 2, sy, 20, W / 2, sy, 520); sg.addColorStop(0, WDG(0.42 * k2)); sg.addColorStop(1, "rgba(0,0,0,0)");
				g.fillStyle = sg; g.fillRect(0, 0, W, H);
			}
			// dust drifting in the raid (looks only)
			if (k1 > 0 && k2 < 1)
			{
				double da = k1 * (1 - k2);
				for (int i = 0; i < 5; i++)
				{
					double cx = (i * 331.7 + t * (40 + 9 * i)) % (W + 600) - 300, cy = 160 + i * 110, rr = 240 + 30 * i;
					var cg = g.createRadialGradient(cx, cy, 10, cx, cy, rr); cg.addColorStop(0, $"rgba(150,110,70,{str(0.08 * da)})"); cg.addColorStop(1, "rgba(0,0,0,0)");
					g.fillStyle = cg; g.fillRect(cx - rr, cy - rr, 2 * rr, 2 * rr);
				}
			}
			// the dunes (dark, at the bottom)
			g.fillStyle = $"rgba({str(18 + 40 * k2)},{str(14 + 20 * k2)},{str(22 + 4 * k2)},0.85)";
			g.beginPath(); g.moveTo(0, H); for (int x = 0; x <= W; x += 32) g.lineTo(x, H - 70 - 34 * JS.sin(x * 0.006 + 0.8) - 14 * JS.sin(x * 0.017)); g.lineTo(W, H); g.closePath(); g.fill();
			g.fillStyle = $"rgba({str(10 + 26 * k2)},{str(8 + 14 * k2)},{str(14 + 2 * k2)},0.95)";
			g.beginPath(); g.moveTo(0, H); for (int x = 0; x <= W; x += 32) g.lineTo(x, H - 34 - 22 * JS.sin(x * 0.009 + 2.1)); g.lineTo(W, H); g.closePath(); g.fill();
			var vg = g.createRadialGradient(W / 2, H / 2, 320, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.5)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}
	// the war drum (harmless, sand and white) at (x,y), beating on the beat while bt is in [b0, b1)
	public static Hz WDDrum(double x, double y, double b0, double b1)
	{
		var h = new Hz { kind = "WDDrum", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = WDk(bt, b0 - 1, b0) * (1 - WDk(bt, b1, b1 + 1)); if (a <= 0) return;
			double hit = 1 - WDk(bt - Math.Floor(bt), 0, 0.3), s = 1 + 0.06 * hit;
			g.save(); g.globalAlpha = a; g.translate(x, y); g.scale(s, s);
			g.fillStyle = "rgba(120,78,44,1)"; g.beginPath(); g.ellipse(0, 10, 58, 20, 0, 0, TAU); g.fill();
			g.fillRect(-58, -12, 116, 22);
			g.fillStyle = "rgba(236,214,170,1)"; g.beginPath(); g.ellipse(0, -12, 58, 20, 0, 0, TAU); g.fill();
			g.strokeStyle = WDW(0.5 + 0.5 * hit); g.lineWidth = 2; g.beginPath(); g.ellipse(0, -12, 44 + 10 * hit, 14 + 3 * hit, 0, 0, TAU); g.stroke();
			g.strokeStyle = "rgba(240,220,180,0.7)"; g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < 6; i++) { double xx = -50 + i * 20; g.moveTo(xx, -4); g.lineTo(xx + 10, 22); g.moveTo(xx + 10, -4); g.lineTo(xx, 22); }
			g.stroke();
			g.restore();
		};
		return h;
	}
	public static Hz WDFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "WDFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- 1. the drum's rings ----------
	// A ring from (cx,cy) at sp px/s from bf, th thick, open at the angles gaps (degrees) gw degrees wide. From bf - tl the gaps show as two
	// white rays each and the ring's start as a white circle.
	public static Hz WDRing(double cx, double cy, double bf, double tl, double sp, double th, double[] gaps, double gw)
	{
		var h = new Hz { kind = "WDRing" }; const double R0 = 70; double tf = B(bf);
		double rad(double t) => R0 + sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { if (t > tf && rad(t) - th > 1500) h.dead = true; };
		double gwr = WDRad(gw);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.5)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.5));
				foreach (var ga in gaps)
					foreach (var e in new[] { -1.0, 1 })
					{ double an = WDRad(ga) + e * gwr / 2; WDDash(cx + JS.cos(an) * R0, cy + JS.sin(an) * R0, cx + JS.cos(an) * 1500, cy + JS.sin(an) * 1500, WDW(0.25 + 0.55 * q), 2); }
				if (bt < bf) { g.strokeStyle = WDW(0.3 + 0.5 * q); g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, R0, 0, TAU); g.stroke(); }
			}
			if (bt >= bf)
			{
				double r = rad(t); var gs = gaps.Select(x => WDRad(x)).OrderBy(x => ((x % TAU) + TAU) % TAU).Select(x => ((x % TAU) + TAU) % TAU).ToArray();
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = th; g.lineCap = "butt";
				for (int i = 0; i < gs.Length; i++)
				{
					double a0 = gs[i] + gwr / 2, a1 = (i + 1 < gs.Length ? gs[i + 1] : gs[0] + TAU) - gwr / 2;
					if (a1 > a0) { g.beginPath(); g.arc(cx, cy, r, a0, a1); g.stroke(); }
				}
				g.shadowBlur = 0; g.strokeStyle = WDDK; g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 });
				for (int i = 0; i < gs.Length; i++)
				{
					double a0 = gs[i] + gwr / 2, a1 = (i + 1 < gs.Length ? gs[i + 1] : gs[0] + TAU) - gwr / 2;
					if (a1 > a0) { g.beginPath(); g.arc(cx, cy, r, a0, a1); g.stroke(); }
				}
				g.setLineDash(new double[0]);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double r = rad(t), d = hypot(p.x - cx, p.y - cy);
			if (Math.Abs(d - r) >= th / 2 + p.r - 2) return false;
			double an = Math.Atan2(p.y - cy, p.x - cx);
			foreach (var ga in gaps)
			{
				double dd = Math.Abs(((an - WDRad(ga)) % TAU + TAU + Math.PI) % TAU - Math.PI);
				if (dd < gwr / 2 - (p.r - 2) / Math.Max(d, 1)) return false;
			}
			return true;
		};
		return h;
	}

	// ---------- 2. spears and arrows ----------
	// Straight shots along the angle ang (degrees), all the same speed sp px/s from bf. Each goes through its point (px[i], py[i]) and
	// comes in from the edge behind it. Its whole way shows dashed white from bf - tl. len: the shot's length; arrow = small feathered arrows.
	public static Hz WDShots(double[] px, double[] py, double ang, double bf, double tl, double sp, double len, bool arrow = false)
	{
		var h = new Hz { kind = "WDShots" }; double tf = B(bf), c = JS.cos(WDRad(ang)), s = JS.sin(WDRad(ang)); int n = px.Length;
		double[] sx = new double[n], sy = new double[n], run = new double[n];
		for (int i = 0; i < n; i++)
		{
			double back = WDExit(px[i], py[i], -c, -s, 30); sx[i] = px[i] - c * back; sy[i] = py[i] - s * back;
			run[i] = back + WDExit(px[i], py[i], c, s, 30) + len;
		}
		double w = arrow ? 3 : 5;
		h.update = (t, dt) => { if (t > tf) { double d = sp * (t - tf); bool all = true; for (int i = 0; i < n; i++) if (d < run[i]) all = false; if (all) h.dead = true; } };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save(); g.lineCap = "round";
			if (bt < bf + 0.3)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.3));
				for (int i = 0; i < n; i++) WDDash(sx[i], sy[i], sx[i] + c * (run[i] - len), sy[i] + s * (run[i] - len), WDW(0.18 + 0.5 * q), arrow ? 1.5 : 2);
			}
			if (t >= tf)
			{
				double d = sp * (t - tf);
				g.shadowColor = PINK; g.shadowBlur = 10;
				for (int i = 0; i < n; i++)
				{
					if (d - len > run[i]) continue;
					double hx = sx[i] + c * d, hy = sy[i] + s * d, tx = hx - c * len, ty = hy - s * len;
					g.strokeStyle = PINK; g.lineWidth = 2 * w; g.beginPath(); g.moveTo(tx, ty); g.lineTo(hx - c * 8, hy - s * 8); g.stroke();
					// the head
					g.fillStyle = PINK; g.beginPath(); g.moveTo(hx + c * 10, hy + s * 10); g.lineTo(hx - c * 12 - s * (w + 6), hy - s * 12 + c * (w + 6)); g.lineTo(hx - c * 12 + s * (w + 6), hy - s * 12 - c * (w + 6)); g.closePath(); g.fill();
					if (arrow)
					{   // the feathers
						g.strokeStyle = PINK; g.lineWidth = 3; g.beginPath();
						g.moveTo(tx, ty); g.lineTo(tx - c * 8 - s * 8, ty - s * 8 + c * 8); g.moveTo(tx, ty); g.lineTo(tx - c * 8 + s * 8, ty - s * 8 - c * 8); g.stroke();
					}
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double d = sp * (t - tf);
			for (int i = 0; i < n; i++)
			{
				if (d - len > run[i]) continue;
				double hx = sx[i] + c * d, hy = sy[i] + s * d;
				if (WDSeg(p.x, p.y, hx - c * len, hy - s * len, hx + c * 6, hy + s * 6) < w + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 3. the sword's slashes ----------
	// A cut: a band bw wide through (cx,cy) at angle ang (degrees). From bf - tl its edges show dashed white; at bf the blade runs along it
	// in 0.15 beat (pink) and burns for hot beats, then it is a white scar that fades.
	public static Hz WDSlash(double cx, double cy, double ang, double bw, double bf, double tl, double hot)
	{
		var h = new Hz { kind = "WDSlash" }; double c = JS.cos(WDRad(ang)), s = JS.sin(WDRad(ang)), L = 1500;
		h.update = (t, dt) => { if (BT(t) > bf + hot + 1.2) h.dead = true; };
		double grow(double bt) => WDOut((bt - bf) / 0.15);
		void band(double f0, double f1, double hw)
		{
			double ax = cx + c * L * f0, ay = cy + s * L * f0, bx = cx + c * L * f1, by = cy + s * L * f1;
			g.beginPath(); g.moveTo(ax - s * hw, ay + c * hw); g.lineTo(bx - s * hw, by + c * hw); g.lineTo(bx + s * hw, by - c * hw); g.lineTo(ax + s * hw, ay - c * hw); g.closePath();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = WDk(bt, bf - tl, bf);
				g.fillStyle = WDW(0.05 + 0.1 * q); band(-1, 1, bw / 2); g.fill();
				WDDash(cx - c * L - s * bw / 2, cy - s * L + c * bw / 2, cx + c * L - s * bw / 2, cy + s * L + c * bw / 2, WDW(0.3 + 0.55 * q), 2 + 2 * q);
				WDDash(cx - c * L + s * bw / 2, cy - s * L - c * bw / 2, cx + c * L + s * bw / 2, cy + s * L - c * bw / 2, WDW(0.3 + 0.55 * q), 2 + 2 * q);
			}
			else if (bt < bf + hot)
			{
				double f = grow(bt), a = 1 - 0.3 * WDk(bt, bf + hot * 0.6, bf + hot);
				g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = WDP(0.7 * a); band(-1, -1 + 2 * f, bw / 2); g.fill();
				g.shadowBlur = 0; g.fillStyle = WDP(a); band(-1, -1 + 2 * f, 5); g.fill();
				g.strokeStyle = WDP(a); g.lineWidth = 3; band(-1, -1 + 2 * f, bw / 2); g.stroke();
			}
			else { double q = 1 - WDk(bt, bf + hot, bf + hot + 1.2); g.fillStyle = WDW(0.25 * q); band(-1, 1, 4); g.fill(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= bf + hot) return false;
			double dx = p.x - cx, dy = p.y - cy, u = dx * c + dy * s, v = -dx * s + dy * c;
			return Math.Abs(v) < bw / 2 + p.r - 2 && u < -L + 2 * L * grow(bt);
		};
		return h;
	}

	// ---------- 4. the Sadu weave's strips ----------
	// A strip of the Sadu weave (a band th thick with saw teeth on both edges) across the screen, open from g0 to g1 along it. vert = false:
	// it lies across and moves up or down (dir 1 = from the top); vert = true: it stands and moves sideways (dir 1 = from the left).
	// sp px/s from bf. From bf - tl the edge it comes from shows white dashes with the gap left open and gold marks at the gap.
	public static Hz WDStrip(bool vert, int dir, double g0, double g1, double bf, double tl, double sp, double th)
	{
		var h = new Hz { kind = "WDStrip" }; double tf = B(bf), span = vert ? W : H, along = vert ? H : W, s0 = dir > 0 ? -th : span + th;
		double pos(double t) => s0 + dir * sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { double p = pos(t); if (dir > 0 ? p - th > span : p + th < 0) h.dead = true; };
		// draws in "strip space" (u along, v across) mapped to the screen
		void P(double u, double v, bool m) { double x = vert ? v : u, y = vert ? u : v; if (m) g.moveTo(x, y); else g.lineTo(x, y); }
		void piece(double u0, double u1, double c)
		{
			const double TW = 24; double hw = th / 2;
			g.beginPath(); P(u0, c - hw, true);
			for (double u = u0; u < u1; u += TW) { double m = Math.Min(u1, u + TW / 2), e = Math.Min(u1, u + TW); P(m, c - hw - 7, false); P(e, c - hw, false); }
			P(u1, c + hw, false);
			for (double u = u1; u > u0; u -= TW) { double m = Math.Max(u0, u - TW / 2), e = Math.Max(u0, u - TW); P(m, c + hw + 7, false); P(e, c + hw, false); }
			g.closePath();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.5)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.5)), e = dir > 0 ? 8 : span - 8;
				g.strokeStyle = WDW(0.25 + 0.55 * q); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				P(0, e, true); P(Math.Max(0, g0), e, false); P(Math.Min(along, g1), e, true); P(along, e, false); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = WDG(0.5 + 0.5 * q); g.lineWidth = 4; double e2 = dir > 0 ? 34 : span - 34;
				g.beginPath(); P(g0, e, true); P(g0, e2, false); P(g1, e, true); P(g1, e2, false); g.stroke();
			}
			if (t >= tf)
			{
				double c = pos(t);
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				if (g0 > 0) { piece(-10, g0, c); g.fill(); }
				if (g1 < along) { piece(g1, along + 10, c); g.fill(); }
				// the weave's diamonds (darker pink, on the pink)
				g.shadowBlur = 0; g.strokeStyle = WDDK; g.lineWidth = 2; g.beginPath();
				for (double u = 0; u < along; u += 36)
				{
					if (u + 26 > g0 && u - 10 < g1) continue;
					P(u, c, true); P(u + 8, c - th * 0.3, false); P(u + 16, c, false); P(u + 8, c + th * 0.3, false); P(u, c, false);
				}
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double c = pos(t), u = vert ? p.y : p.x, v = vert ? p.x : p.y;
			if (Math.Abs(v - c) >= th / 2 + p.r - 2) return false;
			return !(u > g0 + p.r - 2 && u < g1 - p.r + 2);
		};
		return h;
	}

	// ---------- 5. the horses ----------
	// a galloping horse (pink) at (x,y) facing ang, its legs at phase ph
	static void WDHorseDraw(double x, double y, double ang, double ph)
	{
		g.save(); g.translate(x, y); g.rotate(ang);
		g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10;
		g.beginPath(); g.ellipse(0, -2, 36, 14, 0, 0, TAU); g.fill();                                       // the body
		g.beginPath(); g.moveTo(18, -10); g.lineTo(32, -34); g.lineTo(46, -38); g.lineTo(40, -22); g.lineTo(34, 2); g.closePath(); g.fill();   // the neck
		g.beginPath(); g.ellipse(50, -30, 15, 6.5, 0.75, 0, TAU); g.fill();                               // the head, long, nose down
		g.beginPath(); g.moveTo(38, -38); g.lineTo(42, -48); g.lineTo(45, -38); g.closePath(); g.fill();    // the ear
		g.shadowBlur = 0; g.strokeStyle = PINK; g.lineCap = "round"; g.lineJoin = "round";
		// the legs: thigh and shin, galloping
		double[] lx = { 24, 16, -22, -30 }, lp = { 0, 0.6, 3.4, 4.0 };
		g.lineWidth = 5;
		for (int i = 0; i < 4; i++)
		{
			double q = JS.sin(ph + lp[i]), kx = lx[i] + 8 * q, ky = 22, fx = kx + (i < 2 ? 6 : -6) * (1 - Math.Abs(q)) + 6 * q, fy = 36 - 4 * Math.Max(0, -q);
			g.beginPath(); g.moveTo(lx[i], 6); g.lineTo(kx, ky); g.lineTo(fx, fy); g.stroke();
		}
		g.lineWidth = 6; g.beginPath(); g.moveTo(-34, -8); g.quadraticCurveTo(-52, -14 + 4 * JS.sin(ph), -60, 4); g.stroke();   // the tail
		g.strokeStyle = WDDK; g.lineWidth = 3; g.beginPath(); g.moveTo(30, -36); g.lineTo(20, -14); g.moveTo(36, -38); g.lineTo(26, -16); g.stroke();   // the mane
		g.restore();
	}
	static bool WDHorseHit(Player p, double x, double y, double ang)
	{
		double c = JS.cos(ang), s = JS.sin(ang);
		double X(double u, double v) => x + c * u - s * v; double Y(double u, double v) => y + s * u + c * v;
		if (WDInEll(p, X(0, -2), Y(0, -2), ang, 34, 12)) return true;
		if (WDInEll(p, X(50, -30), Y(50, -30), ang + 0.75, 13, 5)) return true;
		return WDSeg(p.x, p.y, X(26, -12), Y(26, -12), X(40, -30), Y(40, -30)) < 5 + p.r - 2;
	}
	// A herd: horses that gallop along lanes across the screen. vert = false: lanes across (centers lc), dir 1 = from the left; vert = true:
	// lanes down (centers lc), dir 1 = from the top. sp px/s from bf. From bf - tl each lane shows white chevrons at the edge it comes in
	// from and its middle dashed.
	public static Hz WDHerd(bool vert, int dir, double[] lc, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "WDHerd" }; double tf = B(bf), along = vert ? H : W, s0 = dir > 0 ? -70 : along + 70;
		double ang = vert ? (dir > 0 ? Math.PI / 2 : -Math.PI / 2) : (dir > 0 ? 0 : Math.PI);
		double pos(double t) => s0 + dir * sp * Math.Max(0, t - tf);
		h.update = (t, dt) => { double p = pos(t); if (dir > 0 ? p > along + 90 : p < -90) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.3)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.3));
				foreach (var c in lc)
				{
					double e = dir > 0 ? 6 : along - 6;
					if (vert) WDDash(c, 0, c, H, WDW(0.15 + 0.35 * q), 2); else WDDash(0, c, W, c, WDW(0.15 + 0.35 * q), 2);
					g.strokeStyle = WDW(0.4 + 0.6 * q); g.lineWidth = 4; g.beginPath();
					for (int k = 0; k < 2; k++)
					{
						double a = e + dir * (6 + 16 * k), b = a + dir * 14;
						if (vert) { g.moveTo(c - 18, a); g.lineTo(c, b); g.lineTo(c + 18, a); } else { g.moveTo(a, c - 18); g.lineTo(b, c); g.lineTo(a, c + 18); }
					}
					g.stroke();
				}
			}
			if (t >= tf)
			{
				double p = pos(t), ph = t * 22;
				foreach (var c in lc) { if (vert) WDHorseDraw(c, p, ang, ph); else WDHorseDraw(p, c + 3, ang, ph); }
			}
			g.restore();
		};
		h.hit = (pl, t) =>
		{
			if (t < tf) return false; double p = pos(t);
			foreach (var c in lc) { if (vert ? WDHorseHit(pl, c, p, ang) : WDHorseHit(pl, p, c + 3, ang)) return true; }
			return false;
		};
		return h;
	}
	// The riders' circle: n horses gallop round (cx,cy) at radius r(bt) (from rb[] to rr[] in straight lines), their angle the integral of
	// kw (degrees a beat) between the beats kb. They fade in from b0 - 1 (white, harmless) and leave at b1 (white again).
	public static Hz WDCircle(int n, double cx, double cy, double[] rb, double[] rr, double[] kb, double[] kw, double b0, double b1)
	{
		var h = new Hz { kind = "WDCircle" };
		double rad(double bt) { if (bt <= rb[0]) return rr[0]; for (int i = 0; i + 1 < rb.Length; i++) if (bt < rb[i + 1]) return rr[i] + (rr[i + 1] - rr[i]) * WDEase((bt - rb[i]) / (rb[i + 1] - rb[i])); return rr[rr.Length - 1]; }
		double spin(double bt) => WDTurn(kb, kw, Math.Max(kb[0], bt));
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		void each(double bt, Action<double, double, double> f)
		{
			double r = rad(bt), a0 = WDRad(spin(bt)), cw = (kw[0] >= 0 ? 1 : -1);
			for (int k = 0; k < n; k++) { double a = a0 + k * TAU / n; f(cx + JS.cos(a) * r, cy + JS.sin(a) * r, a + cw * Math.PI / 2); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 1) return;
			g.save();
			g.strokeStyle = WDW(0.12); g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 }); g.beginPath(); g.arc(cx, cy, rad(bt), 0, TAU); g.stroke(); g.setLineDash(new double[0]);
			if (bt < b0 || bt >= b1)
			{
				double a = bt < b0 ? WDk(bt, b0 - 1, b0) : 1 - WDk(bt, b1, b1 + 1);
				g.globalAlpha = a; g.strokeStyle = WDW(0.8); g.lineWidth = 2;
				each(bt, (x, y, an) => { g.beginPath(); g.ellipse(x, y, 40, 17, an, 0, TAU); g.stroke(); });
			}
			else each(bt, (x, y, an) => WDHorseDraw(x, y, an, t * 22));
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; bool hit = false;
			each(bt, (x, y, an) => { if (!hit && WDHorseHit(p, x, y, an)) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 6. the falcon ----------
	// The falcon dives along a curve (x0,y0) -> (xc,yc) -> (x1,y1) from bf for dur beats (its wings wide across its way). The curve shows
	// dashed white from bf - tl.
	public static Hz WDFalcon(double x0, double y0, double xc, double yc, double x1, double y1, double bf, double dur, double tl)
	{
		var h = new Hz { kind = "WDFalcon" };
		(double x, double y, double a) at(double u)
		{
			double m = 1 - u, x = m * m * x0 + 2 * m * u * xc + u * u * x1, y = m * m * y0 + 2 * m * u * yc + u * u * y1;
			double dx = 2 * m * (xc - x0) + 2 * u * (x1 - xc), dy = 2 * m * (yc - y0) + 2 * u * (y1 - yc);
			return (x, y, Math.Atan2(dy, dx));
		}
		double uOf(double bt) => (bt - bf) / dur;   // straight along in time (fast)
		h.update = (t, dt) => { if (BT(t) > bf + dur + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.3)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.3));
				g.strokeStyle = WDW(0.2 + 0.6 * q); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 });
				g.beginPath(); g.moveTo(x0, y0); g.quadraticCurveTo(xc, yc, x1, y1); g.stroke(); g.setLineDash(new double[0]);
			}
			double u = uOf(bt);
			if (u >= 0 && u <= 1)
			{
				var (x, y, a) = at(u); double fl = 1 + JS.sin(t * 30) * 0.15;
				g.translate(x, y); g.rotate(a);
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14;
				g.beginPath(); g.ellipse(0, 0, 24, 9, 0, 0, TAU); g.fill();       // the body
				circle(22, 0, 8);                                                   // the head
				foreach (var e in new[] { -1.0, 1 })
				{   // the wings: swept back, feathered at the back edge
					g.beginPath(); g.moveTo(8, e * 4); g.quadraticCurveTo(8, e * 50 * fl, -26, e * 70 * fl);
					g.lineTo(-22, e * 56 * fl); g.lineTo(-30, e * 50 * fl); g.lineTo(-20, e * 38 * fl); g.lineTo(-26, e * 30 * fl); g.lineTo(-16, e * 20 * fl); g.lineTo(-14, e * 4); g.closePath(); g.fill();
				}
				g.beginPath(); g.moveTo(-18, -6); g.lineTo(-44, -14); g.lineTo(-48, 0); g.lineTo(-44, 14); g.lineTo(-18, 6); g.closePath(); g.fill();   // the tail
				g.shadowBlur = 0; g.fillStyle = WDDK; g.beginPath(); g.moveTo(34, 0); g.lineTo(27, -4); g.lineTo(27, 4); g.closePath(); g.fill();   // the beak
				g.strokeStyle = WDDK; g.lineWidth = 2; g.beginPath(); g.moveTo(0, -6); g.lineTo(-14, -40 * fl); g.moveTo(0, 6); g.lineTo(-14, 40 * fl); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double u = uOf(BT(t)); if (u < 0 || u > 1) return false;
			var (x, y, a) = at(u);
			if (WDInEll(p, x, y, a, 22, 8) || hypot(p.x - x - JS.cos(a) * 22, p.y - y - JS.sin(a) * 22) < 7 + p.r - 2) return true;
			double c = JS.cos(a), s = JS.sin(a);   // the wings' hit: their inner part, inside them however they flap
			double wx = x - c * 6, wy = y - s * 6;
			return WDSeg(p.x, p.y, wx + s * 30, wy - c * 30, wx - s * 30, wy + c * 30) < 4 + p.r - 2;
		};
		return h;
	}

	// ---------- 7. the ardah's sword walls ----------
	// Two rows of swords held up (the ardah dance): n blades side by side from both edges (vert = false: from the bottom and the top, the
	// blades stand; vert = true: from the left and the right, they lie). Between them the way through: its middle at blade i is
	// mid + amp * sin(ph(bt) + i * k), gap gw wide, ph steps by dp[j] on the step beat st[j] (in 0.15 beat). From the step before, the
	// next tips show as white marks. Fades in (white, harmless) over b0 - 1.5 .. b0 and away after b1.
	public static Hz WDWalls(bool vert, int n, double mid, double amp, double k, double gw, double[] st, double[] dp, double ph0, double b0, double b1)
	{
		var h = new Hz { kind = "WDWalls" }; double along = vert ? H : W, cw = along / n, deep = vert ? W : H;
		double ph(double bt) { double p = ph0; for (int j = 0; j < st.Length; j++) { if (bt < st[j]) break; p += dp[j] * WDEase((bt - st[j]) / 0.15); } return p; }
		double nextPh(double bt) { double p = ph0; for (int j = 0; j < st.Length; j++) { p += dp[j]; if (bt < st[j]) return p; } return p; }
		double cOf(double p, int i) => mid + amp * JS.sin(p + i * k);
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		void P(double u, double v, bool m) { double x = vert ? v : u, y = vert ? u : v; if (m) g.moveTo(x, y); else g.lineTo(x, y); }
		// a blade in column i from the edge e (0 or deep) to its tip at v (tip 24 long)
		void blade(int i, double e, double v)
		{
			double u0 = i * cw, u1 = u0 + cw, sgn = e > v ? 1 : -1, um = u0 + cw / 2;
			g.beginPath(); P(u0, e, true); P(u0, v + sgn * 24, false); P(um, v, false); P(u1, v + sgn * 24, false); P(u1, e, false); g.closePath();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1.5 || bt > b1 + 1.5) return;
			double p = ph(bt);
			g.save();
			bool live = bt >= b0 && bt < b1;
			double a = live ? 1 : bt < b0 ? WDk(bt, b0 - 1.5, b0) : 1 - WDk(bt, b1, b1 + 1.5);
			for (int i = 0; i < n; i++)
			{
				double c = cOf(p, i);
				if (live) { g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8; }
				else { g.fillStyle = WDW(0.18 * a); g.shadowBlur = 0; }
				blade(i, deep + 10, c + gw / 2); g.fill(); blade(i, -10, c - gw / 2); g.fill();
				if (live)
				{
					g.shadowBlur = 0; g.strokeStyle = WDDK; g.lineWidth = 2; g.beginPath(); double um = i * cw + cw / 2;
					P(um, c + gw / 2 + 30, true); P(um, deep, false); P(um, c - gw / 2 - 30, true); P(um, 0, false);
					// where one blade meets the next, and its guard across
					double u0 = i * cw; P(u0, c + gw / 2 + 24, true); P(u0, deep, false); P(u0, c - gw / 2 - 24, true); P(u0, 0, false);
					P(u0 + 8, c + gw / 2 + 120, true); P(u0 + cw - 8, c + gw / 2 + 120, false); P(u0 + 8, c - gw / 2 - 120, true); P(u0 + cw - 8, c - gw / 2 - 120, false);
					g.stroke();
				}
			}
			// where the tips go next (white marks)
			if (bt >= b0 - 1.5 && bt < b1)
			{
				double np = nextPh(bt), sn = st.FirstOrDefault(x => x > bt, 1e9), q = 1 - WDk(sn - bt, 0, 1);
				if (sn < b1 && Math.Abs(np - p) > 1e-6)
				{
					g.strokeStyle = WDW(0.3 + 0.6 * q); g.lineWidth = 3; g.beginPath();
					for (int i = 0; i < n; i++) { double c = cOf(np, i), u0 = i * cw + 6, u1 = (i + 1) * cw - 6; P(u0, c + gw / 2, true); P(u1, c + gw / 2, false); P(u0, c - gw / 2, true); P(u1, c - gw / 2, false); }
					g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (pl, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			double u = vert ? pl.y : pl.x, v = vert ? pl.x : pl.y, m = pl.r - 2; int i = (int)Math.Floor(u / cw); if (i < 0 || i >= n) return false;
			double p = ph(bt);
			for (int j = Math.Max(0, i - 1); j <= Math.Min(n - 1, i + 1); j++)
			{
				double c = cOf(p, j), um = j * cw + cw / 2, du = Math.Abs(u - um);
				// the lower blade (tip at c + gw/2) and the upper one (tip at c - gw/2): a triangle tip 24 long, then full width
				double d1 = v - (c + gw / 2) + m, d2 = (c - gw / 2) - v + m;
				if (d1 > 0 && du < cw / 2 * Math.Min(1, d1 / 24) + m) return true;
				if (d2 > 0 && du < cw / 2 * Math.Min(1, d2 / 24) + m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 8. the banners ----------
	// A banner's pole slams down from the top at x at bf (0.15 beat), stands for stay beats, then is pulled up (0.3 beat). Its flag (pink,
	// waving) hangs from the pole's top on the side side (1 right, -1 left). From bf - tl the pole's line shows dashed white.
	public static Hz WDBanner(double x, int side, double bf, double tl, double stay)
	{
		var h = new Hz { kind = "WDBanner" }; const double PW = 22, FW = 150, FH = 96, FY = 40;
		h.update = (t, dt) => { if (BT(t) > bf + stay + 0.5) h.dead = true; };
		double bottom(double bt) => bt < bf ? -10 : bt < bf + stay ? H * WDOut((bt - bf) / 0.15) : H * (1 - WDEase((bt - bf - stay) / 0.3));
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf + 0.2)
			{
				double q = WDk(bt, bf - tl, bf) * (1 - WDk(bt, bf, bf + 0.2));
				WDDash(x, 0, x, H, WDW(0.25 + 0.6 * q), 3);
				g.strokeStyle = WDW(0.2 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.strokeRect(side > 0 ? x : x - FW, FY, FW, FH); g.setLineDash(new double[0]);
			}
			double yb = bottom(bt);
			if (bt >= bf && yb > 0)
			{
				double off = yb - H;   // the whole banner moves with the pole
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.fillRect(x - PW / 2, -10, PW, yb + 10);
				// the flag: waving on its far edge only (its hit is the rectangle up to the wave's inner side)
				double fy = FY + off, wv = JS.sin(t * 9) * 8;
				g.beginPath(); g.moveTo(x, fy); g.lineTo(x + side * (FW + wv), fy + 6); g.lineTo(x + side * (FW - 14 - wv), fy + FH / 2); g.lineTo(x + side * (FW + wv), fy + FH - 6); g.lineTo(x, fy + FH); g.closePath(); g.fill();
				g.shadowBlur = 0; g.strokeStyle = WDDK; g.lineWidth = 3; g.beginPath();
				g.moveTo(x + side * 30, fy + 22); g.lineTo(x + side * 100, fy + 22); g.moveTo(x + side * 30, fy + FH - 22); g.lineTo(x + side * 100, fy + FH - 22); g.stroke();
				g.fillStyle = WDW(0.9); circle(x, Math.Max(fy - 14, -20), 9);   // the pole's top (white, harmless)
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf) return false; double yb = bottom(bt); if (yb <= 0) return false; double m = p.r - 2, fy = FY + yb - H;
			if (Math.Abs(p.x - x) < PW / 2 + m && p.y < yb + m) return true;
			double fx0 = side > 0 ? x : x - (FW - 30), fx1 = side > 0 ? x + FW - 30 : x;
			return p.x > fx0 - m && p.x < fx1 + m && p.y > fy + 8 - m && p.y < fy + FH - 8 + m;
		};
		return h;
	}

	// ---------- 9. the crossed swords ----------
	// n blades from the hub (cx,cy) turning together: angle the integral of kw (degrees a beat) between kb. Each blade from 46 to len px,
	// 7 px half wide. Shown white (harmless) over b0 - 2 .. b0, pink from b0 to b1, white again after.
	public static Hz WDCross(double cx, double cy, int n, double len, double[] kb, double[] kw, double a0, double b0, double b1)
	{
		var h = new Hz { kind = "WDCross" }; const double R0 = 46, HW = 7;
		double ang(double bt) => WDRad(a0 + WDTurn(kb, kw, Math.Max(kb[0], bt)));
		h.update = (t, dt) => { if (BT(t) > b1 + 1.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2 || bt > b1 + 1.5) return; bool live = bt >= b0 && bt < b1; double a = ang(bt);
			g.save(); g.translate(cx, cy);
			for (int k = 0; k < n; k++)
			{
				g.save(); g.rotate(a + k * TAU / n);
				if (live) { g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; }
				else { double q = bt < b0 ? WDk(bt, b0 - 2, b0) : 1 - WDk(bt, b1, b1 + 1.5); g.fillStyle = WDW(0.12 + 0.3 * q); }
				g.beginPath(); g.moveTo(R0, -HW); g.lineTo(len - 30, -HW); g.lineTo(len, 0); g.lineTo(len - 30, HW); g.lineTo(R0, HW); g.closePath(); g.fill();
				if (live) { g.shadowBlur = 0; g.strokeStyle = WDDK; g.lineWidth = 2; g.beginPath(); g.moveTo(R0 + 10, 0); g.lineTo(len - 40, 0); g.stroke(); }
				g.restore();
			}
			// the hilts (gold, harmless) at the hub
			g.fillStyle = WDG(0.9); circle(0, 0, 22); g.fillStyle = "rgba(60,40,20,1)"; circle(0, 0, 9);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; double a = ang(bt);
			for (int k = 0; k < n; k++)
			{
				double q = a + k * TAU / n, c = JS.cos(q), s = JS.sin(q);
				if (WDSeg(p.x, p.y, cx + c * R0, cy + s * R0, cx + c * (len - 15), cy + s * (len - 15)) < HW + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 10. the end ----------
	public static Hz WDEnd(double b0, double b1)
	{
		var h = new Hz { kind = "WDEnd", z = 1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = WDk(bt, b0, b0 + 0.3), a = Math.Min(k, 1 - WDk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(110 - 20 * k)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#f0be5a"; g.shadowBlur = 30; g.fillStyle = "#f8dc9a"; g.fillText("حان الوعد", W / 2, H / 2 - 150); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
}
