using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Shiawase VIP (Dion Timmer), main level 22: "the broken mask". Shiawase is "happiness"; kintsugi mends a broken thing with gold so it
// comes back stronger. A smiling porcelain mask: part 1 it cracks (the cracks, the falling tiles, the turning plate, the frozen moment),
// part 2 it breaks and its shards attack (the shard rain, the rows that stop and go, the shards that come back, the eyes, the broken rings),
// part 3 it is mended with gold and is stronger (the gold rails, the corridor of the crack, the seams, the last smile).
// The level is the song from 0:30.000 (songs/SONG_L22.mp3 starts there). Grid: 150 BPM, beat 0 = 0.084 s of the file (0:30.084 of the song).
// Every hazard is a pure function of time once it exists. Only pink hurts: the porcelain (white) and the gold never do.
public static partial class G
{
	static double SWk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string SWP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string SWW(double a) => $"rgba(238,234,226,{str(clamp(a, 0, 1))})";
	static string SWG(double a) => $"rgba(236,186,76,{str(clamp(a, 0, 1))})";
	static double SWEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double SWOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double SWIn(double k) { k = clamp(k, 0, 1); return k * k; }
	static double SWRad(double d) => d * Math.PI / 180;
	static double SWH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }   // a fixed number 0-1 from s
	static void SWDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double SWSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// where a ray from (x,y) along (dx,dy) leaves the screen (plus m px)
	static double SWExit(double x, double y, double dx, double dy, double m = 40)
	{
		double best = 1e9;
		if (dx > 1e-6) best = Math.Min(best, (W + m - x) / dx); if (dx < -1e-6) best = Math.Min(best, (-m - x) / dx);
		if (dy > 1e-6) best = Math.Min(best, (H + m - y) / dy); if (dy < -1e-6) best = Math.Min(best, (-m - y) / dy);
		return Math.Max(0, best);
	}
	// a shard: a sharp three-cornered piece, size s, pointing along a
	static void SWShardPath(double x, double y, double s, double a)
	{
		double c = JS.cos(a), sn = JS.sin(a);
		void P(double u, double v, bool m) { double px = x + c * u - sn * v, py = y + sn * u + c * v; if (m) g.moveTo(px, py); else g.lineTo(px, py); }
		g.beginPath(); P(s, 0, true); P(-0.55 * s, 0.6 * s, false); P(-0.25 * s, 0, false); P(-0.55 * s, -0.6 * s, false); g.closePath();
	}
	static void SWShard(double x, double y, double s, double a) { SWShardPath(x, y, s, a); g.fill(); }
	static bool SWShardHit(Player p, double x, double y, double s) => hypot(p.x - x, p.y - y) < 0.55 * s + p.r - 2;
	// the integral (degrees) of a speed (degrees a beat) that goes in straight lines between (kb[i], kw[i]), from kb[0] to bt
	static double SWTurn(double[] kb, double[] kw, double bt)
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

	// ---------- the mask (harmless: white porcelain, dark eyes, gold seams) ----------
	// half: only the top half (the eyes); crack 0-1: grey cracks; gold 0-1: the gold seams; smile 0-1: the smile turns gold and wider
	static readonly double[][] SWSEAM = {
		new[] { 0.0, -1.0, 0.12, -0.55, -0.05, -0.2, 0.08, 0.15 }, new[] { -0.82, -0.35, -0.45, -0.25, -0.2, -0.2 }, new[] { 0.85, -0.2, 0.5, -0.05, 0.08, 0.15, 0.35, 0.5, 0.3, 0.92 },
		new[] { -0.7, 0.55, -0.35, 0.4, -0.05, 0.5, 0.0, 0.98 }, new[] { -0.95, 0.15, -0.6, 0.12 }, new[] { 0.6, -0.75, 0.42, -0.45 } };
	public static void SWMask(double x, double y, double sc, double rot, double crack, double gold, double smile, bool half = false, double a = 1)
	{
		if (a <= 0.01 || sc <= 0.01) return;
		g.save(); g.globalAlpha = a; g.translate(x, y); g.rotate(rot); g.scale(sc, sc);
		const double RX = 92, RY = 118;
		if (half) { g.beginPath(); g.rect(-RX - 10, -RY - 10, 2 * RX + 20, RY + 30); g.clip(); }
		var gr = g.createRadialGradient(-20, -40, 10, 0, 0, 150); gr.addColorStop(0, "rgba(252,250,245,1)"); gr.addColorStop(1, "rgba(196,190,182,1)");
		g.fillStyle = gr; g.beginPath(); g.ellipse(0, 0, RX, RY, 0, 0, TAU); g.fill();
		g.strokeStyle = "rgba(120,114,110,0.6)"; g.lineWidth = 3; g.stroke();
		// brows, eyes (dark holes), cheeks, the smile
		g.strokeStyle = "rgba(40,36,44,0.9)"; g.lineWidth = 4; g.lineCap = "round";
		g.beginPath(); g.moveTo(-58, -46); g.quadraticCurveTo(-36, -60, -16, -48); g.moveTo(58, -46); g.quadraticCurveTo(36, -60, 16, -48); g.stroke();
		g.fillStyle = "rgba(14,12,22,1)";
		g.beginPath(); g.moveTo(-60, -20); g.quadraticCurveTo(-37, -36, -14, -20); g.quadraticCurveTo(-37, -10, -60, -20); g.fill();
		g.beginPath(); g.moveTo(60, -20); g.quadraticCurveTo(37, -36, 14, -20); g.quadraticCurveTo(37, -10, 60, -20); g.fill();
		g.fillStyle = "rgba(230,150,150,0.25)"; g.beginPath(); g.ellipse(-52, 22, 18, 10, 0, 0, TAU); g.ellipse(52, 22, 18, 10, 0, 0, TAU); g.fill();
		double sw = 30 + 18 * smile;
		g.strokeStyle = smile > 0 ? SWG(0.6 + 0.4 * smile) : "rgba(40,36,44,0.9)"; g.lineWidth = 4 + 2 * smile;
		g.beginPath(); g.moveTo(-sw, 52); g.quadraticCurveTo(0, 74 + 14 * smile, sw, 52); g.stroke();
		// the cracks (grey, then gold)
		if (crack > 0 || gold > 0)
		{
			for (int i = 0; i < SWSEAM.Length; i++)
			{
				double show = clamp(Math.Max(crack, gold) * SWSEAM.Length - i, 0, 1); if (show <= 0) continue;
				var s = SWSEAM[i]; int n = s.Length / 2;
				g.strokeStyle = gold > 0 ? SWG(0.35 + 0.65 * gold) : "rgba(60,56,64,0.85)"; g.lineWidth = gold > 0 ? 3 + 1.5 * gold : 2;
				if (gold > 0) { g.shadowColor = "#ecba4c"; g.shadowBlur = 10 * gold; }
				g.beginPath(); g.moveTo(s[0] * RX, s[1] * RY);
				for (int j = 1; j < n; j++) { double u = clamp(show * (n - 1) - (j - 1), 0, 1); if (u <= 0) break; g.lineTo((s[2 * j - 2] + (s[2 * j] - s[2 * j - 2]) * u) * RX, (s[2 * j - 1] + (s[2 * j + 1] - s[2 * j - 1]) * u) * RY); }
				g.stroke(); g.shadowBlur = 0;
			}
		}
		g.restore();
	}
	// where the mask is (and how it looks) at beat bt, for the stage and for the attacks that come out of it
	public struct SWM { public double x, y, sc, rot, crack, gold, smile, a; public bool half; }
	public static SWM SWMaskAt(double bt)
	{
		var m = new SWM { x = W / 2, y = H / 2, sc = 1, a = 0 };
		if (bt < 17) { m.y = 150; m.sc = 0.85; m.a = 1 - SWk(bt, 16, 17); m.crack = SWk(bt, 1, 16.5); }
		else if (bt < 85) { m.sc = 0.7; m.a = SWk(bt, 17, 18) * (1 - SWk(bt, 84, 85)); m.crack = 1; m.rot = SWRad(SWPlateAng(bt)); }
		else if (bt >= 177 && bt < 213) { m.y = 105; m.sc = 0.9; m.half = true; m.a = SWk(bt, 177, 181); m.crack = 1; }
		else if (bt >= 213 && bt < 245) { double k = SWEase(SWk(bt, 213, 215)); m.y = 105 + (H / 2 - 105) * k; m.sc = 0.9 + 0.1 * k; m.half = k < 0.5; m.a = 1; m.crack = 1; }
		else if (bt >= 245 && bt < 341) { m.a = 1 - SWk(bt, 283, 285); m.crack = 1; m.gold = SWk(bt, 245, 247); }
		else if (bt >= 341) { m.sc = 1.15; m.a = SWk(bt, 339, 341); m.gold = 1; m.rot = SWRad(SWSeamAng(bt)); m.smile = SWk(bt, 373, 373.6); }
		return m;
	}

	// ---------- the stage (harmless, under everything) ----------
	public static Hz SWStage(double t1)
	{
		var h = new Hz { kind = "SWStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p3 = SWk(bt, 243, 247);
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(14 + 16 * p3)},{str(18 + 6 * p3)},{str(40 - 14 * p3)},1)"); gr.addColorStop(1, $"rgba({str(4 + 6 * p3)},{str(5 + 2 * p3)},{str(14 - 6 * p3)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// ink wash: slow soft clouds (looks only)
			for (int i = 0; i < 5; i++)
			{
				double cx = (i * 331.7 + t * (8 + 3 * i)) % (W + 600) - 300, cy = 90 + i * 130, rr = 260 + 40 * i;
				var cg = g.createRadialGradient(cx, cy, 10, cx, cy, rr); cg.addColorStop(0, p3 > 0 ? $"rgba(120,96,40,{str(0.06 * p3)})" : "rgba(70,90,150,0.07)"); cg.addColorStop(1, "rgba(0,0,0,0)");
				g.fillStyle = cg; g.fillRect(cx - rr, cy - rr, 2 * rr, 2 * rr);
			}
			// fine paper grain: thin faint lines (looks only)
			g.strokeStyle = "rgba(255,255,255,0.025)"; g.lineWidth = 1; g.beginPath();
			for (int i = 0; i < 18; i++) { double yy = (i * 41.3 + 13) % H; g.moveTo(0, yy); g.lineTo(W, yy + 6); }
			g.stroke();
			// gold dust in part 3
			if (p3 > 0) { g.fillStyle = SWG(0.25 * p3); for (int i = 0; i < 30; i++) { double dx = (i * 383.7) % W, sp = 20 + (i * 37) % 40, dy = H + 10 - ((i * 211.3 + t * sp) % (H + 20)); circle(dx, dy, i % 5 == 0 ? 2.2 : 1.2); } }
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
			var m = SWMaskAt(bt);
			if (m.a > 0 && !(bt >= 17 && bt < 85)) SWMask(m.x, m.y, m.sc, m.rot, m.crack, m.gold, m.smile, m.half, m.a * a);
		};
		return h;
	}
	public static Hz SWFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "SWFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- 1. the cracks (0:30.5) ----------
	// A crack runs from one edge of the screen to the other in jagged steps. Its whole way shows dashed from bp; on each growth beat gb[i]
	// it runs on to (i+1)/n of its length in 0.12 beat. A piece of it is hot (pink, hurts) for hot beats after it appears, then it cools
	// to a white scar (harmless).
	public class SWLine { public double[] px, py; public double[] gb; public double bp, hot; }
	public static SWLine SWMakeLine(double x0, double y0, double x1, double y1, int segs, double jit, double seed, double[] gb, double bp, double hot)
	{
		var L = new SWLine { px = new double[segs + 1], py = new double[segs + 1], gb = gb, bp = bp, hot = hot };
		double dx = x1 - x0, dy = y1 - y0, l = hypot(dx, dy), nx = -dy / l, ny = dx / l;
		for (int i = 0; i <= segs; i++) { double u = (double)i / segs, j = (i == 0 || i == segs) ? 0 : (SWH(seed + i * 7.1) * 2 - 1) * jit; L.px[i] = x0 + dx * u + nx * j; L.py[i] = y0 + dy * u + ny * j; }
		return L;
	}
	// how far along (0-1) the crack has run at beat bt, and the beat when the point at f was reached
	static double SWLineTip(SWLine L, double bt)
	{
		int n = L.gb.Length; double f = 0;
		for (int i = 0; i < n; i++) { if (bt < L.gb[i]) break; f = (i + SWOut((bt - L.gb[i]) / 0.12)) / n; }
		return clamp(f, 0, 1);
	}
	static double SWLineAt(SWLine L, double f) { int n = L.gb.Length; int i = Math.Min(n - 1, (int)Math.Floor(f * n)); return L.gb[i] + 0.12 * SWk(f * n - i, 0, 1); }
	static double SWLineLen(SWLine L) { double s = 0; for (int i = 1; i < L.px.Length; i++) s += hypot(L.px[i] - L.px[i - 1], L.py[i] - L.py[i - 1]); return s; }
	// walks the crack in small pieces (from f0 to f1 of its length): (x0,y0,x1,y1, f at the piece's middle)
	static void SWLineWalk(SWLine L, double f0, double f1, Action<double, double, double, double, double> fn, double step = 18)
	{
		double len = SWLineLen(L); if (len <= 0 || f1 <= f0) return; int segs = L.px.Length - 1; double acc = 0;
		for (int i = 1; i <= segs; i++)
		{
			double sl = hypot(L.px[i] - L.px[i - 1], L.py[i] - L.py[i - 1]); double a0 = acc / len, a1 = (acc + sl) / len; acc += sl;
			if (a1 <= f0 || a0 >= f1) continue; int pcs = Math.Max(1, (int)Math.Ceiling(sl / step));
			for (int k = 0; k < pcs; k++)
			{
				double u0 = (double)k / pcs, u1 = (double)(k + 1) / pcs, g0 = a0 + (a1 - a0) * u0, g1 = a0 + (a1 - a0) * u1;
				if (g1 <= f0 || g0 >= f1) continue; double c0 = Math.Max(g0, f0), c1 = Math.Min(g1, f1);
				double v0 = (c0 - a0) / (a1 - a0), v1 = (c1 - a0) / (a1 - a0);
				fn(L.px[i - 1] + (L.px[i] - L.px[i - 1]) * v0, L.py[i - 1] + (L.py[i] - L.py[i - 1]) * v0, L.px[i - 1] + (L.px[i] - L.px[i - 1]) * v1, L.py[i - 1] + (L.py[i] - L.py[i - 1]) * v1, (c0 + c1) / 2);
			}
		}
	}
	public static Hz SWCracks(SWLine[] lines, double bEnd)
	{
		var h = new Hz { kind = "SWCracks" };
		h.update = (t, dt) => { if (BT(t) > bEnd + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), fade = 1 - SWk(bt, bEnd, bEnd + 1);
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			foreach (var L in lines)
			{
				if (bt < L.bp) continue; double tip = SWLineTip(L, bt);
				// the way it will run (dashed)
				if (tip < 1) { g.strokeStyle = SWW(0.35 + 0.3 * SWk(bt, L.bp, L.gb[0])); g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(L.px[0], L.py[0]); for (int i = 1; i < L.px.Length; i++) g.lineTo(L.px[i], L.py[i]); g.stroke(); g.setLineDash(new double[0]); }
				// the scar
				g.strokeStyle = SWW(0.45 * fade); g.lineWidth = 3;
				SWLineWalk(L, 0, tip, (x0, y0, x1, y1, f) => { if (bt >= SWLineAt(L, f) + L.hot) { g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); } });
			}
			g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 6;
			foreach (var L in lines)
			{
				if (bt < L.gb[0]) continue; double tip = SWLineTip(L, bt);
				SWLineWalk(L, 0, tip, (x0, y0, x1, y1, f) => { if (bt < SWLineAt(L, f) + L.hot) { g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); } });
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); bool hit = false;
			foreach (var L in lines)
			{
				if (hit || bt < L.gb[0]) continue; double tip = SWLineTip(L, bt);
				SWLineWalk(L, 0, tip, (x0, y0, x1, y1, f) => { if (!hit && bt < SWLineAt(L, f) + L.hot && SWSeg(p.x, p.y, x0, y0, x1, y1) < 3 + p.r - 2) hit = true; });
			}
			return hit;
		};
		return h;
	}

	// ---------- 2. the tiles fall (0:34) ----------
	// The screen is 4 x 3 tiles (along the cracks). On each chop cb[i] the tiles of set i (bit k = tile k, row by row) fall: their edge
	// shows white tl beats before, then the tile is pink and sinks (shrinks to 70%) for dur beats. One Hz per chop.
	public static Hz SWTile(int mask, double cb, double tl, double dur)
	{
		var h = new Hz { kind = "SWTile" }; const double TW = 320, TH = 240;
		h.update = (t, dt) => { if (BT(t) > cb + dur) h.dead = true; };
		double sz(double bt) => 1 - 0.3 * SWIn((bt - cb) / dur);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < cb - tl) return;
			g.save();
			for (int k = 0; k < 12; k++)
			{
				if ((mask >> k & 1) == 0) continue; double x = (k % 4) * TW, y = (k / 4) * TH;
				if (bt < cb)
				{
					double q = SWk(bt, cb - tl, cb); g.strokeStyle = SWW(0.35 + 0.55 * q); g.lineWidth = 2 + 2 * q; g.setLineDash(new double[] { 14, 10 });
					g.strokeRect(x + 8, y + 8, TW - 16, TH - 16); g.setLineDash(new double[0]);
					g.fillStyle = SWW(0.05 + 0.08 * q); g.fillRect(x + 8, y + 8, TW - 16, TH - 16);
				}
				else
				{
					double s = sz(bt), w = TW * s, hh = TH * s, cx = x + TW / 2, cy = y + TH / 2, a = 1 - 0.35 * SWk(bt, cb + dur * 0.7, cb + dur);
					g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = SWP(0.62 * a); g.fillRect(cx - w / 2, cy - hh / 2, w, hh);
					g.shadowBlur = 0; g.strokeStyle = SWP(a); g.lineWidth = 4; g.strokeRect(cx - w / 2, cy - hh / 2, w, hh);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < cb || bt >= cb + dur * 0.85) return false; double s = sz(bt);
			for (int k = 0; k < 12; k++)
			{
				if ((mask >> k & 1) == 0) continue; double cx = (k % 4) * TW + TW / 2, cy = (k / 4) * TH + TH / 2;
				if (Math.Abs(p.x - cx) < TW * s / 2 + p.r - 2 && Math.Abs(p.y - cy) < TH * s / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 3. the plate turns (0:36.9) ----------
	// The screen becomes a porcelain plate (the mask painted in its middle) that turns: its angle in degrees at beat bt
	static readonly double[] SWPLB = { 17, 48.5, 49.5, 53, 84, 85 }, SWPLW = { 13, 13, -13, -18, -18, 0 };
	public static double SWPlateAng(double bt) => SWTurn(SWPLB, SWPLW, Math.Max(17, bt));
	public static Hz SWPlateBg()
	{
		var h = new Hz { kind = "SWPlateBg", z = -1 };
		h.update = (t, dt) => { if (BT(t) > 86) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = SWk(bt, 16.5, 17.5) * (1 - SWk(bt, 84.5, 85.5)); if (a <= 0) return;
			double ang = SWRad(SWPlateAng(bt));
			g.save(); g.globalAlpha = a; g.translate(W / 2, H / 2); g.rotate(ang);
			var gr = g.createRadialGradient(0, 0, 40, 0, 0, 760); gr.addColorStop(0, "rgba(44,46,64,1)"); gr.addColorStop(1, "rgba(18,20,34,1)");
			g.fillStyle = gr; g.beginPath(); g.arc(0, 0, 760, 0, TAU); g.fill();
			// the plate's painted rings and wave pattern (faint white, harmless)
			g.strokeStyle = SWW(0.1); g.lineWidth = 2;
			foreach (var r in new[] { 160.0, 250, 420, 600 }) { g.beginPath(); g.arc(0, 0, r, 0, TAU); g.stroke(); }
			g.strokeStyle = SWW(0.07); g.lineWidth = 2; g.beginPath();
			for (int k = 0; k < 24; k++) { double q = k * TAU / 24; g.moveTo(JS.cos(q) * 260, JS.sin(q) * 260); g.quadraticCurveTo(JS.cos(q + 0.13) * 340, JS.sin(q + 0.13) * 340, JS.cos(q) * 410, JS.sin(q) * 410); }
			g.stroke();
			g.restore();
			SWMask(W / 2, H / 2, 0.7, ang, 1, 0, 0, false, a);
		};
		return h;
	}
	// A group of n cracks from the middle outwards on the turning plate (plate angles a0 + k*360/n). It shows dashed from bs, runs out
	// from r 70 to 820 in 0.3 beat from bg, hot for hot beats, then a scar until bEnd.
	public static Hz SWRadial(double a0, int n, double bs, double bg, double hot, double bEnd)
	{
		var h = new Hz { kind = "SWRadial" }; const double R0 = 70, R1 = 820;
		h.update = (t, dt) => { if (BT(t) > Math.Min(bEnd, bg + hot + 2)) h.dead = true; };
		double tip(double bt) => R0 + (R1 - R0) * SWOut((bt - bg) / 0.3);
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bs) return; double pa = SWPlateAng(bt);
			g.save(); g.lineCap = "round";
			for (int k = 0; k < n; k++)
			{
				double q = SWRad(pa + a0 + k * 360.0 / n), c = JS.cos(q), s = JS.sin(q);
				if (bt < bg) { SWDash(W / 2 + c * R0, H / 2 + s * R0, W / 2 + c * R1, H / 2 + s * R1, SWW(0.3 + 0.5 * SWk(bt, bs, bg)), 2); continue; }
				double r = tip(bt);
				if (bt < bg + hot) { g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = 6; }
				else { g.shadowBlur = 0; g.strokeStyle = SWW(0.4 * (1 - SWk(bt, bg + hot, bg + hot + 2))); g.lineWidth = 3; }
				// a slight zigzag (the same for draw and hit)
				g.beginPath(); g.moveTo(W / 2 + c * R0, H / 2 + s * R0);
				for (int j = 1; j <= 8; j++) { double rr = R0 + (R1 - R0) * j / 8.0; if (rr > r) rr = r; double z = (j % 2 == 0 ? 1 : -1) * 7; g.lineTo(W / 2 + c * rr - s * z, H / 2 + s * rr + c * z); if (rr >= r) break; }
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bg || bt >= bg + hot) return false; double pa = SWPlateAng(bt), r = tip(bt);
			for (int k = 0; k < n; k++)
			{
				double q = SWRad(pa + a0 + k * 360.0 / n), c = JS.cos(q), s = JS.sin(q); double px = W / 2 + c * R0, py = H / 2 + s * R0;
				for (int j = 1; j <= 8; j++)
				{
					double rr = R0 + (R1 - R0) * j / 8.0; if (rr > r) rr = r; double z = (j % 2 == 0 ? 1 : -1) * 7, x = W / 2 + c * rr - s * z, y = H / 2 + s * rr + c * z;
					if (SWSeg(p.x, p.y, px, py, x, y) < 3 + p.r - 2) return true; px = x; py = y; if (rr >= r) break;
				}
			}
			return false;
		};
		return h;
	}
	// A ring of n shards chips off the plate's rim and flies to the middle (in = true) or from the middle out, at sp px/s, from bf.
	// Their ways show dashed from bf - tl.
	public static Hz SWChips(int n, double a0, bool inward, double bf, double tl, double sp, double sz)
	{
		var h = new Hz { kind = "SWChips" }; const double RR = 760, RC = 40; double tf = B(bf);
		double rad(double t) => inward ? RR - (t - tf) * sp : RC + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && (inward ? rad(t) < RC : rad(t) > RR + 40)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			for (int k = 0; k < n; k++)
			{
				double q = SWRad(a0 + k * 360.0 / n), c = JS.cos(q), s = JS.sin(q);
				if (bt < bf) { SWDash(W / 2 + c * RC, H / 2 + s * RC, W / 2 + c * 820, H / 2 + s * 820, SWW(0.18 + 0.45 * SWk(bt, bf - tl, bf)), 2); continue; }
				double r = rad(t); g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; SWShard(W / 2 + c * r, H / 2 + s * r, sz, inward ? q + Math.PI : q);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double r = rad(t);
			for (int k = 0; k < n; k++) { double q = SWRad(a0 + k * 360.0 / n); if (SWShardHit(p, W / 2 + JS.cos(q) * r, H / 2 + JS.sin(q) * r, sz)) return true; }
			return false;
		};
		return h;
	}

	// ---------- 4. the frozen moment (1:04.1) ----------
	// The broken mask's pieces hang in rings round the middle, each ring turning its own way (slow at first, faster and faster), the rings
	// pulled in from 1:12 (beat 105); at beat bx they stand in 8 lines (spokes) and blow outwards along them at 1100 px/s (the spokes show
	// dashed from bx - 2). Pink cracks creep in from the edges as a frame.
	public static Hz SWFrozen(double[] R, int per, double[] wb, double b0, double bx, double frame)
	{
		var h = new Hz { kind = "SWFrozen" }; int n = R.Length; double tx = B(bx);
		double ang(int i, double bt)
		{
			double u = Math.Max(0, bx - bt), dir = i % 2 == 0 ? 1 : -1;
			return SWRad(dir * (wb[i] * 8 * (1 - Math.Exp(-u / 8)) + 3 * u) + 180.0 / per);
		}
		double rs(double bt) => 1 - 0.35 * SWEase((bt - 105) / 11);
		void each(double t, Action<double, double, double, double> f)
		{
			double bt = BT(t);
			for (int i = 0; i < n; i++)
				for (int k = 0; k < per; k++)
				{
					double a, r, sz = 26 + 6 * ((i + k) % 3);
					if (t < tx) { a = ang(i, bt) + k * TAU / per; r = R[i] * rs(bt); }
					else { a = SWRad(180.0 / per) + k * TAU / per; r = R[i] * rs(bx) + (t - tx) * 1100; }
					f(W / 2 + JS.cos(a) * r, H / 2 + JS.sin(a) * r, sz, t < tx ? a + (i % 2 == 0 ? 1 : -1) * Math.PI / 2 : a);   // pointing the way it goes
				}
		}
		double fr(double bt) => frame * SWEase((bt - b0) / 16) * (1 - SWk(bt, bx - 1, bx));
		h.update = (t, dt) => { if (t > tx + 1.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double ap = SWk(bt, b0 - 1, b0);
			g.save();
			// the frame of cracks (jagged pink band from the edges)
			double m = fr(bt);
			if (m > 1)
			{
				g.fillStyle = SWP(0.55); g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath();
				g.moveTo(0, 0); g.lineTo(W, 0); g.lineTo(W, H); g.lineTo(0, H); g.closePath();
				int nx = 32, ny = 18;
				double c = m * 0.9; g.moveTo(c, c);   // the inner edge, corner to corner (points past a corner would fold the edge over)
				for (int j = 1; j < nx; j++) { double x = W * j / nx, d = m * (0.75 + 0.25 * SWH(j * 3.3)); if (x > c + 24 && x < W - c - 24) g.lineTo(x, d); }
				g.lineTo(W - c, c);
				for (int j = 1; j < ny; j++) { double y = H * j / ny, d = m * (0.75 + 0.25 * SWH(j * 5.1 + 40)); if (y > c + 24 && y < H - c - 24) g.lineTo(W - d, y); }
				g.lineTo(W - c, H - c);
				for (int j = nx - 1; j >= 1; j--) { double x = W * j / nx, d = m * (0.75 + 0.25 * SWH(j * 7.7 + 80)); if (x > c + 24 && x < W - c - 24) g.lineTo(x, H - d); }
				g.lineTo(c, H - c);
				for (int j = ny - 1; j >= 1; j--) { double y = H * j / ny, d = m * (0.75 + 0.25 * SWH(j * 9.9 + 120)); if (y > c + 24 && y < H - c - 24) g.lineTo(d, y); }
				g.closePath(); g.fill("evenodd"); g.shadowBlur = 0;
			}
			// the rings they hang on (faint white, harmless)
			if (t < tx) { g.strokeStyle = SWW(0.12 * ap); g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 }); foreach (var r0 in R) { g.beginPath(); g.arc(W / 2, H / 2, r0 * rs(bt), 0, TAU); g.stroke(); } g.setLineDash(new double[0]); }
			if (bt >= bx - 2 && t < tx) for (int k = 0; k < per; k++) { double a = SWRad(180.0 / per) + k * TAU / per; SWDash(W / 2 + JS.cos(a) * 60, H / 2 + JS.sin(a) * 60, W / 2 + JS.cos(a) * 820, H / 2 + JS.sin(a) * 820, SWW(0.3 + 0.5 * SWk(bt, bx - 2, bx)), 2); }
			g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = SWP(ap);
			each(t, (x, y, sz, rot) => { if (x > -40 && x < W + 40 && y > -40 && y < H + 40) SWShard(x, y, sz, rot); });
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; bool hit = false;
			double m = fr(bt); if (m > 1 && (p.x < m * 0.75 + p.r - 2 || p.y < m * 0.75 + p.r - 2 || p.x > W - m * 0.75 - p.r + 2 || p.y > H - m * 0.75 - p.r + 2)) return true;
			each(t, (x, y, sz, rot) => { if (!hit && SWShardHit(p, x, y, sz)) hit = true; });
			return hit;
		};
		return h;
	}
}
