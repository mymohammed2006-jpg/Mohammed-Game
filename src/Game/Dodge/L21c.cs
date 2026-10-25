using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Barracuda (main level 21), part 3: the giant barracuda, and the level's events. See L21.cs
public static partial class G
{
	const double BRL = 760, BRWD = 84;   // the giant's length and width
	static double BRBw(double u) => u < 0.08 ? 40 + 20 * u / 0.08 : u < 0.28 ? 60 + 24 * (u - 0.08) / 0.2 : u < 0.92 ? 84 - 64 * (u - 0.28) / 0.64 : 20;

	// the giant's body along the points pts (pts[0] = the head, then back along its path), with its head looking along (hx,hy)
	static void BRGiantDraw(List<(double x, double y)> pts, double glint)
	{
		int n = pts.Count; if (n < 2) return;
		var L = new List<(double x, double y)>(); var R = new List<(double x, double y)>();
		for (int i = 0; i < n; i++)
		{
			int a = Math.Max(0, i - 1), b = Math.Min(n - 1, i + 1); double dx = pts[a].x - pts[b].x, dy = pts[a].y - pts[b].y, l = Math.Max(1e-6, hypot(dx, dy)); dx /= l; dy /= l;
			double w = BRBw(i / (double)(BRPN - 1)) / 2; L.Add((pts[i].x - dy * w, pts[i].y + dx * w)); R.Add((pts[i].x + dy * w, pts[i].y - dx * w));
		}
		double hx = pts[0].x - pts[1].x, hy = pts[0].y - pts[1].y, hl = Math.Max(1e-6, hypot(hx, hy)); hx /= hl; hy /= hl;
		g.save(); g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = PINK;
		g.beginPath(); g.moveTo(pts[0].x + hx * 46, pts[0].y + hy * 46);
		foreach (var p in L) g.lineTo(p.x, p.y);
		for (int i = R.Count - 1; i >= 0; i--) g.lineTo(R[i].x, R[i].y);
		g.closePath(); g.fill();
		// the tail fork and the fins
		if (n == BRPN)
		{
			var e = pts[n - 1]; double tx = pts[n - 1].x - pts[n - 2].x, ty = pts[n - 1].y - pts[n - 2].y, tl = Math.Max(1e-6, hypot(tx, ty)); tx /= tl; ty /= tl;
			g.beginPath(); g.moveTo(e.x, e.y); g.lineTo(e.x + tx * 70 - ty * 46, e.y + ty * 70 + tx * 46); g.lineTo(e.x + tx * 40, e.y + ty * 40); g.lineTo(e.x + tx * 70 + ty * 46, e.y + ty * 70 - tx * 46); g.closePath(); g.fill();
		}
		g.shadowBlur = 0;
		// the mouth line, the teeth, the eye
		double nx = -hy, ny = hx, mx = pts[0].x, my = pts[0].y;
		g.strokeStyle = "rgba(60,0,20,0.6)"; g.lineWidth = 3; g.beginPath(); g.moveTo(mx + hx * 44, my + hy * 44); g.lineTo(mx - hx * 30 + nx * 6, my - hy * 30 + ny * 6); g.stroke();
		g.fillStyle = "rgba(255,190,210,0.95)";
		for (int i = 0; i < 5; i++) { double s = 36 - i * 13, bx = mx + hx * s + nx * 4, by = my + hy * s + ny * 4; g.beginPath(); g.moveTo(bx, by); g.lineTo(bx - hx * 5 + nx * 8, by - hy * 5 + ny * 8); g.lineTo(bx - hx * 10, by - hy * 10); g.closePath(); g.fill(); }
		double ex = mx - hx * 6 - nx * 12, ey = my - hy * 6 - ny * 12; g.fillStyle = "#fff"; circle(ex, ey, 8); g.fillStyle = "#1a0008"; circle(ex + hx * 2, ey + hy * 2, 3.5);
		if (glint > 0) { double r = 10 + 22 * glint; g.strokeStyle = BRW(0.95); g.lineWidth = 3; g.beginPath(); g.moveTo(ex - r, ey); g.lineTo(ex + r, ey); g.moveTo(ex, ey - r); g.lineTo(ex, ey + r); g.stroke(); }
		g.restore();
	}
	static bool BRGiantHit(Player p, List<(double x, double y)> pts)
	{
		int n = pts.Count; if (n < 2) return false;
		double hx = pts[0].x - pts[1].x, hy = pts[0].y - pts[1].y, hl = Math.Max(1e-6, hypot(hx, hy)); hx /= hl; hy /= hl;
		if (BRSeg(p.x, p.y, pts[0].x, pts[0].y, pts[0].x + hx * 34, pts[0].y + hy * 34) < 16 + p.r - 2) return true;
		for (int i = 0; i + 1 < n; i++)
		{
			var a = pts[i]; var b = pts[i + 1]; if (Math.Abs(p.x - a.x) > 80 || Math.Abs(p.y - a.y) > 80) continue;
			if (BRSeg(p.x, p.y, a.x, a.y, b.x, b.y) < Math.Min(BRBw(i / (double)(BRPN - 1)), BRBw((i + 1) / (double)(BRPN - 1))) / 2 * 0.9 + p.r - 2) return true;
		}
		return false;
	}
	const int BRPN = 32;   // body points, BRL / (BRPN - 1) apart

	// ---------- the giant on its fixed path (the circling parts) ----------
	// Its head: comes in from the left (b 159-160), circles the middle (rx, ry shrinking, w rad/s) and bites the points bp at the beats bb
	// (its way shows dashed with the eye's glint 1.2 beats before), leaves along its way at 175.5; out of sight 176-207.3; comes in again
	// at 207.3 for the second circle with its bites, and leaves at 219.5. Not aimed: its whole path is fixed.
	public static Hz BRGiant(double w1, double w2, double[] bb, double[] bpx, double[] bpy)
	{
		var h = new Hz { kind = "BRGiant" }; var cache = new Dictionary<long, List<(double, double)>>();
		double T(double b) => B(b);
		(double x, double y) E1(double t) { double u = BRk(BT(t), 160, 176), rx = 560 - 230 * u, ry = 290 - 110 * u, f = Math.PI + w1 * (t - T(160)); return (W / 2 + JS.cos(f) * rx, H / 2 + JS.sin(f) * ry); }
		(double x, double y) E2(double t) { double u = BRk(BT(t), 208.3, 219.5), rx = 520 - 220 * u, ry = 280 - 110 * u, f = Math.PI + w2 * (t - T(208.3)); return (W / 2 + JS.cos(f) * rx, H / 2 + JS.sin(f) * ry); }
		(double x, double y) Lerp((double x, double y) a, (double x, double y) b, double k) => (a.x + (b.x - a.x) * k, a.y + (b.y - a.y) * k);
		(double x, double y) circ(double t) => BT(t) < 190 ? E1(t) : E2(t);
		// a bite from beat b: out along to the point (and 120 px past) at 2400 px/s, back to the circle in 0.38 s
		(double x, double y)? bite(double t)
		{
			for (int i = 0; i < bb.Length; i++)
			{
				double t0 = T(bb[i]); if (t < t0) continue; var p0 = circ(t0); double dx = bpx[i] - p0.x, dy = bpy[i] - p0.y, l = Math.Max(1, hypot(dx, dy)); dx /= l; dy /= l;
				double L = l + 120, da = L / 2400; if (t < t0 + da) { double s = (t - t0) * 2400; return (p0.x + dx * s, p0.y + dy * s); }
				if (t < t0 + da + 0.38) { var q = (p0.x + dx * L, p0.y + dy * L); return Lerp(q, circ(t), BREase((t - t0 - da) / 0.38)); }
			}
			return null;
		}
		(double x, double y) head(double t)
		{
			double bt = BT(t);
			if (bt < 159) return (-3000, 360);
			if (bt < 160) return Lerp((-460, 360), E1(T(160)), BREase((t - T(159)) / (T(160) - T(159))));
			if (bt < 175.5) return bite(t) ?? E1(t);
			if (bt < 177) { var p = E1(T(175.5)); var q = E1(T(175.5) + 0.01); double dx = q.x - p.x, dy = q.y - p.y, l = Math.Max(1e-6, hypot(dx, dy)); double s = (t - T(175.5)) * 1500; return (p.x + dx / l * s, p.y + dy / l * s); }
			if (bt < 207.3) return (-3000, -3000);
			if (bt < 208.3) return Lerp((-460, 360), E2(T(208.3)), BREase((t - T(207.3)) / (T(208.3) - T(207.3))));
			if (bt < 219.5) return bite(t) ?? E2(t);
			if (bt < 221) { var p = E2(T(219.5)); var q = E2(T(219.5) + 0.01); double dx = q.x - p.x, dy = q.y - p.y, l = Math.Max(1e-6, hypot(dx, dy)); double s = (t - T(219.5)) * 1500; return (p.x + dx / l * s, p.y + dy / l * s); }
			return (-3000, -3000);
		}
		// the body: back along the head's path, BRPN points BRL / (BRPN - 1) apart; cut where the head jumped
		List<(double x, double y)> body(double t)
		{
			long key = (long)Math.Round(t * 2400); if (cache.TryGetValue(key, out var c)) return c;
			if (cache.Count > 400) cache.Clear();
			var pts = new List<(double, double)>(); var h0 = head(t); pts.Add(h0); double step = BRL / (BRPN - 1), acc = 0, need = step; var prev = h0;
			for (int j = 1; j < 700 && pts.Count < BRPN; j++)
			{
				var q = head(t - j / 360.0); double d = hypot(q.x - prev.x, q.y - prev.y); if (d > 150) break;
				while (acc + d >= need && pts.Count < BRPN) { double k = (need - acc) / Math.Max(1e-9, d); pts.Add((prev.x + (q.x - prev.x) * k, prev.y + (q.y - prev.y) * k)); need += step; }
				acc += d; prev = q;
			}
			cache[key] = pts; return pts;
		}
		bool onScreen(List<(double x, double y)> pts) { foreach (var p in pts) if (p.x > -120 && p.x < W + 120 && p.y > -120 && p.y < H + 120) return true; return false; }
		h.update = (t, dt) => { if (BT(t) > 222) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			// the bites to come: the way dashed, the eye glints
			double gl = 0;
			for (int i = 0; i < bb.Length; i++)
			{
				if (bt < bb[i] - 1.2 || bt >= bb[i]) continue; double k = BRk(bt, bb[i] - 1.2, bb[i]); var p0 = circ(T(bb[i])); double dx = bpx[i] - p0.x, dy = bpy[i] - p0.y, l = Math.Max(1, hypot(dx, dy));
				BRDash(p0.x, p0.y, bpx[i] + dx / l * 120, bpy[i] + dy / l * 120, BRW(0.3 + 0.6 * k), 2 + 2 * k); g.strokeStyle = BRW(0.5 + 0.4 * k); g.lineWidth = 2; g.beginPath(); g.arc(bpx[i], bpy[i], 18 + 16 * (1 - k), 0, TAU); g.stroke();
				gl = Math.Max(gl, k);
			}
			var pts = body(t); if (!onScreen(pts)) return; BRGiantDraw(pts, gl);
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < 159.5 || bt > 221) return false; var pts = body(t); return BRGiantHit(p, pts); };
		return h;
	}

	// ---------- the chase (1:29.4-1:36.8): the giant's path in straight pieces, one an event ----------
	// Its head runs at the fixed speed BRChV (faster on the high sound); every piece k (from beat bk, dt beats) heads for where the square
	// is when it starts (aimed: rnd). The body lies on the last BRL px of the path. The last piece (fixed way: up) takes it out
	public class BRPiece { public double x, y, dx, dy, s0, s1, t0, t1; }
	static readonly List<BRPiece> _brCh = new();
	static double[] _brChVox = new double[0];
	static double BRChV(double bt, bool ez)
	{
		if (bt >= 206.5) return 900; double v = ez ? (bt < 200 ? 380 : 420) : (bt < 200 ? 470 : 530);
		foreach (var b in _brChVox) if (bt >= b && bt < b + 0.5) v *= ez ? 1.15 : 1.3; return v;
	}
	// the head's distance along the path at time t (from 192)
	static double BRChS(double t, bool ez)
	{
		double t0 = B(192); if (t <= t0) return 0; double s = 0, dt = 1 / 240.0;
		for (double u = t0; u < t; u += dt) s += BRChV(BT(u), ez) * Math.Min(dt, t - u);
		return s;
	}
	static readonly Dictionary<long, double> _brChSc = new();
	static double BRChSc(double t, bool ez) { long k = (long)Math.Round(t * 2400); if (_brChSc.TryGetValue(k, out var v)) return v; if (_brChSc.Count > 3000) _brChSc.Clear(); return _brChSc[k] = BRChS(t, ez); }
	public static Hz BRChase(double b0, double b1, bool up)
	{
		var h = new Hz { kind = "BRChase" }; bool ez = easy; double t0 = B(b0), t1 = B(b1);
		var prev = _brCh.Count > 0 ? _brCh[^1] : null;
		double x = prev != null ? prev.x + prev.dx * (prev.s1 - prev.s0) : W / 2, y = prev != null ? prev.y + prev.dy * (prev.s1 - prev.s0) : -160;
		double tx = up ? x : player?.x ?? W / 2, ty = up ? -600 : player?.y ?? H / 2, l = Math.Max(1, hypot(tx - x, ty - y));
		var pc = new BRPiece { x = x, y = y, dx = (tx - x) / l, dy = (ty - y) / l, s0 = BRChSc(t0, ez), s1 = BRChSc(t1, ez), t0 = t0, t1 = t1 };
		if (up) pc.s1 = pc.s0 + 2400;
		_brCh.Add(pc);
		// the part of the body on this piece at time t: from arc s_a (tail side) to s_b (head side)
		bool span(double t, out double sa, out double sb, out double S)
		{
			S = BRChSc(t, ez); sa = Math.Max(pc.s0, S - BRL); sb = Math.Min(pc.s1, S); return sb > sa;
		}
		(double x, double y) at(double s) => (pc.x + pc.dx * (s - pc.s0), pc.y + pc.dy * (s - pc.s0));
		h.update = (t, dt) => { if (BRChSc(t, ez) - BRL > pc.s1 + 10 || BT(t) > 210) h.dead = true; };
		h.draw = t =>
		{
			// its way before the head gets here (it locked on where you were)
			if (!up) { double S0 = BRChSc(t, ez); if (S0 < pc.s0) { var a0 = at(pc.s0); var a1 = at(pc.s1); BRDash(a0.x, a0.y, a1.x + pc.dx * 60, a1.y + pc.dy * 60, BRW(0.75), 3); g.strokeStyle = BRW(0.8); g.lineWidth = 2; g.beginPath(); g.arc(a1.x, a1.y, 10, 0, TAU); g.stroke(); } }
			if (!span(t, out double sa, out double sb, out double S)) return;
			var pts = new List<(double x, double y)>(); bool hd = S <= pc.s1 + 1e-6;
			// points every BRL / (BRPN - 1) on the body, those on this piece
			for (int i = 0; i < BRPN; i++) { double s = S - i * BRL / (BRPN - 1); if (s > pc.s1 + 1e-6 || s < pc.s0 - 1e-6) continue; pts.Add(at(s)); }
			if (hd && pts.Count >= 2) { BRGiantDraw(pts.Count == BRPN ? pts : pts, 0); }
			else
			{
				g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineCap = "round";
				for (int i = 0; i + 1 < BRPN; i++)
				{
					double s1 = S - i * BRL / (BRPN - 1), s2 = S - (i + 1) * BRL / (BRPN - 1); double a = Math.Min(s1, pc.s1), b = Math.Max(s2, pc.s0); if (a <= b) continue;
					var p1 = at(a); var p2 = at(b); g.lineWidth = BRBw((i + 0.5) / (BRPN - 1)); g.beginPath(); g.moveTo(p1.x, p1.y); g.lineTo(p2.x, p2.y); g.stroke();
				}
				g.restore();
			}
			// its way while its head is on this piece
			if (hd && !up) { var hp = at(S); BRDash(hp.x, hp.y, hp.x + pc.dx * 220, hp.y + pc.dy * 220, BRW(0.6), 3); }
		};
		h.hit = (p, t) =>
		{
			if (!span(t, out double sa, out double sb, out double S)) return false;
			if (S <= pc.s1) { var hp = at(S); if (hypot(p.x - hp.x - pc.dx * 20, p.y - hp.y - pc.dy * 20) < 22 + p.r - 2) return true; }
			for (int i = 0; i + 1 < BRPN; i++)
			{
				double s1 = S - i * BRL / (BRPN - 1), s2 = S - (i + 1) * BRL / (BRPN - 1); double a = Math.Min(s1, pc.s1), b = Math.Max(s2, pc.s0); if (a <= b) continue;
				var p1 = at(a); var p2 = at(b); if (BRSeg(p.x, p.y, p1.x, p1.y, p2.x, p2.y) < BRBw((i + 0.5) / (BRPN - 1)) / 2 * 0.9 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the last bite (1:42-1:44): its jaws sweep across with one gap (at where the square is at bl) ----------
	public static Hz BRJaw(double bl, double b0, double b1, double dir, double gap)
	{
		var h = new Hz { kind = "BRJaw" }; double gy = H / 2; bool lk = false; const double TH = 80;
		double X(double bt) { double k = BRk(bt, b0, b1); return dir > 0 ? -TH + (W + 2 * TH) * k : W + TH - (W + 2 * TH) * k; }
		h.update = (t, dt) => { double bt = BT(t); if (!lk) { gy = clamp(player?.y ?? H / 2, gap / 2 + 20, H - gap / 2 - 20); if (bt >= bl) lk = true; } h.z = bt < b0 ? -1 : 0; if (bt > b1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), x = X(bt);
			g.save();
			if (bt < b0)
			{
				double k = BRk(bt, bl - 1, b0), ex = dir > 0 ? 6 : W - 6; BRDash(0, gy - gap / 2, W, gy - gap / 2, BRW(lk ? 0.4 + 0.5 * k : 0.3), 2); BRDash(0, gy + gap / 2, W, gy + gap / 2, BRW(lk ? 0.4 + 0.5 * k : 0.3), 2);
				g.fillStyle = BRW(0.05 + 0.1 * k); g.fillRect(dir > 0 ? 0 : W - 40, 0, 40, gy - gap / 2); g.fillRect(dir > 0 ? 0 : W - 40, gy + gap / 2, 40, H - gy - gap / 2);
				g.restore(); return;
			}
			if (bt > b1) { g.restore(); return; }
			g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK;
			g.fillRect(x - TH / 2, 0, TH, gy - gap / 2 - 26); g.fillRect(x - TH / 2, gy + gap / 2 + 26, TH, H - gy - gap / 2 - 26);
			// the teeth round the gap
			g.beginPath();
			for (double q = -TH / 2; q < TH / 2 - 1; q += TH / 4) { g.moveTo(x + q, gy - gap / 2 - 26); g.lineTo(x + q + TH / 8, gy - gap / 2); g.lineTo(x + q + TH / 4, gy - gap / 2 - 26); g.moveTo(x + q, gy + gap / 2 + 26); g.lineTo(x + q + TH / 8, gy + gap / 2); g.lineTo(x + q + TH / 4, gy + gap / 2 + 26); }
			g.fill(); g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt > b1) return false; double x = X(bt); return Math.Abs(p.x - x) < TH / 2 + p.r - 2 && (p.y - p.r + 2 < gy - gap / 2 || p.y + p.r - 2 > gy + gap / 2); };
		return h;
	}
	public static Hz BREnd(double b0, double b1)
	{
		var h = new Hz { kind = "BREnd" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = BRk(bt, b0, b0 + 0.3), a = Math.Min(k, 1 - BRk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(110 - 20 * k)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#ffffff"; g.shadowBlur = 30; g.fillStyle = "#ffffff"; g.fillText("BARRACUDA", W / 2, H / 2); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
	public static Hz BRFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "BRFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the events ----------
	static readonly int _brb = DodgeBuild("L21", () => buildBR());
	static List<Ev> buildBR()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz z) => hazards.Add(z);
		void adds(Hz[] zs) { foreach (var z in zs) hazards.Add(z); }
		double TB(double sec) => sec / SPB;   // seconds as beats
		at(0, () => add(BRStage(B(230))), true);

		// ===== 1. the reef at night =====
		var pul = ez ? BR_PUL.Where((v, i) => i % 2 == 0).ToArray() : BR_PUL;
		at(0, () => add(ez ? BRJelly(200, 200, pul, 30, 30, BR_VOXB, BR_VOXD, 20, 0, 32.5) : BRJelly(150, 150, pul, 34, 34, BR_VOXB, BR_VOXD, 34, 0, 32.5)));
		double[] boats = { 160, 480, 800, 1120 }; int[] bo = { 0, 3, 1, 2 };
		at(30, () => add(BRBoats(boats, 30, 64)));
		at(30.5, () => add(ez ? BRReef(BR_REEF, 32, 63, 60, 60) : BRReef(BR_REEF, 32, 63, 80, 95)));
		for (int i = 0; ; i++)
		{
			double f = 32 + i * (ez ? 2 : 1); if (f > 63.01) break; double bx = boats[bo[i % 4]];
			if (ez) { at(f - 2, () => add(BRHarpoon(bx, f - 2, f - 0.9, f, 1.5, 2000)), false, true); if (f >= 48) { double hx2 = boats[bo[(i + 2) % 4]], ox = (i % 2 == 0 ? 1 : -1) * 170; at(f - 2, () => add(BRHarpoon(hx2, f - 2, f - 0.9, f, 1.5, 2000, ox)), false, true); } }
			else
			{
				at(f - 1.5, () => add(BRHarpoon(bx, f - 1.5, f - 0.55, f, 2.5, 2600)), false, true);
				// from 0:22 a second one from another boat closes the side you would run to
				if (f >= 48) { double hx2 = boats[bo[(i + 2) % 4]], ox = (i % 2 == 0 ? 1 : -1) * 150; at(f - 1.5, () => add(BRHarpoon(hx2, f - 1.5, f - 0.55, f, 2.5, 2600, ox)), false, true); }
				// from 0:28 a third from the last boat closes the other side: you stay in the lane between them
				if (f >= 56) { double hx3 = boats[bo[(i + 1) % 4]], ox = (i % 2 == 0 ? -1 : 1) * 150; at(f - 1.5, () => add(BRHarpoon(hx3, f - 1.5, f - 0.55, f, 2.5, 2600, ox)), false, true); }
			}
		}
		{
			var nb = BR_ARPB.Where(b => b < 78).ToArray(); var np = BR_ARPP.Take(nb.Length).ToArray();
			if (ez) { nb = nb.Where((v, i) => i % 2 == 0).ToArray(); np = np.Where((v, i) => i % 2 == 0).ToArray(); }
			if (ez) at(62.5, () => adds(BRCoral(new double[] { 260, 1020, 640, 0 }, new double[] { H, H, 0, 360 }, new double[] { -90, -90, 90, 0 }, nb, np, 120, 0.82, 18, 64, 78.5)));
			else at(62.5, () => adds(BRCoral(new double[] { 220, 640, 1060, 430, 850, 0, W }, new double[] { H, H, H, 0, 0, 300, 420 }, new double[] { -90, -90, -90, 90, 90, 0, 180 }, nb, np, 150, 0.82, 22, 64, 78.5)));
		}
		{
			var kb = BR_NET;
			at(78.5, () => add(ez ? BRNet(160, 7, 16, kb, 32, 0.18, 78.5, 80, 92, 95.5, 96) : BRNet(150, 8, 20, kb, 36, 0.22, 78.5, 80, 92, 95.5, 96)));
		}
		at(96, () => add(BRFx(96, 0.35, 12, 1.4)));

		// ===== 2. the pack hunts =====
		int ck = 0; double tellN = TB(0.62), tellE = TB(0.85);
		void strike(double b, double fa = 0, double po = 0, int n = 1, double spread = 0)
		{
			double k = ck++ % 8, tl = ez ? tellE : tellN, sp = ez ? 2000 : 2800, fw = ez ? 36 : 40;
			at(b - tl, () => { for (int j = 0; j < n; j++) add(BRStrike(k, fa + (j - (n - 1) / 2.0) * spread, po, b - tl, b, sp, 300, fw)); }, false, true);
		}
		// two at once from places a quarter of the clock apart, both at you: they cross where you are
		void cross(double b) { double k = ck++ % 8, tl = tellN; at(b - tl, () => { add(BRStrike(k, 0, 0, b - tl, b, 2800)); add(BRStrike((k + 2) % 8, 0, 0, b - tl, b, 2800)); }, false, true); }
		// the pack in a row from one side, gaps left (their places go round so you always have to move)
		int rk = 0; int[] rd = { 0, 2, 1, 3 };
		double[][] gH = { new[] { 170.0, 530 }, new[] { 90.0, 360, 630 }, new[] { 250.0, 600 }, new[] { 120.0, 470 } };
		double[][] gV = { new[] { 200.0, 640, 1080 }, new[] { 420.0, 860 }, new[] { 130.0, 530, 950 }, new[] { 310.0, 740, 1160 } };
		void rake(double b, double stg = 0)
		{
			int d = rd[rk % 4], j = rk / 4 % 4; rk++; double tl = ez ? 2 : 1.5; var gs = d < 2 ? gH[(j + rk) % 4] : gV[(j + rk) % 4];
			at(b - tl, () => { foreach (var z in BRRake(d, b - tl, b, gs, ez ? 135 : 110, ez ? 850 : 1000, stg)) add(z); });
		}
		void rails(double b, double off) { double k = ck++ % 8, tl = tellN; at(b - tl, () => { add(BRStrike(k, 0, -off, b - tl, b, 2800)); add(BRStrike(k, 0, off, b - tl, b, 2800)); }, false, true); }
		for (int m = 0; m < 8; m++)
		{
			double b = 96 + 4 * m;
			if (ez)
			{
				if (m < 4) { strike(b + 1); strike(b + 2); strike(b + 3); }
				else { strike(b + 1); strike(b + 2); strike(b + 3); }
			}
			else if (m < 4) { if (m > 0) strike(b, 0, 0, 3, 14 * Math.PI / 180); strike(b + 1); cross(b + 1.5); strike(b + 2.5); cross(b + 3.5); }
			else { strike(b + 0.5); cross(b + 1.5); strike(b + 2.5); cross(b + 3.5); }
		}
		// the first half: the school in currents across the screen, sliding up and down on the beat (where to shows dashed a beat before)
		{
			var shb = new List<double>(); var shd = new List<double>(); double[] pat = { 80, 80, -80, 80, -80, -80, 80, -80 };
			for (double b = 97; b < 111; b += 1) { shb.Add(b); shd.Add(pat[shb.Count % 8] * (ez ? 1.0 : 1.2)); }
			at(94.5, () => adds(ez ? BRCurrent(false, 96, 111.5, 95, 140, 120, shb.ToArray(), shd.ToArray(), 60) : BRCurrent(false, 96, 111.5, 100, 120, 100, shb.ToArray(), shd.ToArray(), 80)));
		}
		// the second half: the school whirls round the middle, the pack strikes through it
		at(111.5, () => adds(ez ? BRSwirl(112, 128, 4, 0.65, 0.007, true, 120) : BRSwirl(112, 128, 5, 0.9, 0.008, true, 120)));
		// the school, a shape every 4 beats; strikes go on through it
		double hole = ez ? 115 : 95;
		at(127.5, () => { add(BRSchool("V", 128, 132, W + 40, -40, 200, hole)); if (!ez) add(BRSchool("V", 128, 132, -40, W + 40, 520, hole)); });
		at(131.5, () => { foreach (var y in ez ? new[] { 140.0, 580 } : new[] { 140.0, 360, 580 }) add(BRSchool("C", 132, 136, -60, y, hole, 0, 0, 560, new[] { 134.0 }, new[] { 90.0 })); });
		at(135.5, () => { add(BRSchool("H", 136, 140, -40, H + 40, 320, hole)); if (!ez) add(BRSchool("H", 136, 140, H + 40, -40, 960, hole)); });
		at(139.5, () => { foreach (var y in ez ? new[] { 180.0, 540 } : new[] { 100.0, 280, 460, 640 }) add(BRSchool("C", 140, 144, W + 60, y, hole, 0, 180, 560, new[] { 142.0, 143 }, new[] { -90.0, 90 })); });
		at(143.5, () => { add(BRSchool("V", 144, 148, -40, W + 40, 360, hole)); if (!ez) add(BRSchool("H", 144, 148, -40, H + 40, 640, hole)); });
		at(147.5, () => { foreach (var x in ez ? new[] { 360.0, 920 } : new[] { 200.0, 520, 840, 1160 }) add(BRSchool("C", 148, 152, x, -60, hole, 0, 90, 520, new[] { 150.0, 151 }, new[] { 90.0, -90 })); });
		at(151.5, () => { add(BRSchool("V", 152, 155.5, W + 40, -40, 160, hole)); if (!ez) add(BRSchool("V", 153, 156, W + 40, -40, 560, hole)); });
		for (double b = 130; b < 155; b += ez ? 4 : 2) rake(b, (b / 2) % 2 == 0 ? 0.5 : -0.5);
		for (double b = 128; b < 156; b += 4) { if (ez) { strike(b + 1); strike(b + 2); strike(b + 3); } else { strike(b); strike(b + 0.5); cross(b + 1); strike(b + 1.5); strike(b + 2); cross(b + 2.5); strike(b + 3); strike(b + 3.5); } }
		// the bait ball
		at(155, () => add(ez ? BRBall(155.5, 156, 159.5, 160, 16, 3, 360, 50, 1.8, 750) : BRBall(155.5, 156, 159.5, 160, 24, 3, 330, 40, 2.4, 950)), false, true);
		at(160, () => add(BRFx(160, 0.4, 14, 1.5)));

		// ===== 3. the giant barracuda =====
		var bb = new List<double>(); var bx2 = new List<double>(); var by2 = new List<double>();
		double[][] pts1 = { new[] { 640.0, 360 }, new[] { 440.0, 260 }, new[] { 840.0, 460 }, new[] { 640.0, 360 }, new[] { 840.0, 260 }, new[] { 440.0, 460 }, new[] { 640.0, 360 } };
		for (int i = 0; i < 7; i++) { double b = 162 + 2 * i; if (ez && i % 2 == 1) continue; bb.Add(b); bx2.Add(pts1[i][0]); by2.Add(pts1[i][1]); }
		foreach (var (b, x, y) in ez ? new[] { (214.5, 640.0, 360.0) } : new[] { (210.5, 640.0, 360.0), (214.5, 480.0, 300.0), (218.0, 800.0, 420.0) }) { bb.Add(b); bx2.Add(x); by2.Add(y); }
		at(158, () => add(BRGiant(ez ? 1.3 : 1.8, ez ? 1.4 : 2.0, bb.ToArray(), bx2.ToArray(), by2.ToArray())), true);
		for (double b = 161; b < 175.6; b += 1) { if (ez) { if ((b - 161) % 2 == 0) strike(b); } else if ((b - 161) % 2 == 0) { strike(b); strike(b + 0.5); } else cross(b); }
		// two rows at once from two sides (Normal): only where their gaps cross is safe
		foreach (var b in new[] { 163.0, 167, 171 }) { rake(b, ez ? 0 : 0.5); rake(b, -0.5); }
		// the ambush: it strikes out of the dark along where you are, with two small ones a beat after from the second half
		int[] ak = { 0, 3, 6, 1, 4, 7, 2, 5 };
		for (int i = 0; i < 8; i++)
		{
			double b = 176.5 + 2 * i, tl = ez ? TB(0.9) : TB(0.65); int k = ak[i];
			at(b - tl, () => add(BRStrike(k, 0, 0, b - tl, b, ez ? 2100 : 2600, BRL, BRWD)), false, true);
			if (b + 1 < 191.5) { if (ez) strike(b + 1); else { cross(b + 1); if (b + 1.5 < 191.5) strike(b + 1.5); } }
		}

		// the school sweeps through the dark while it waits
		// the school runs in currents through the dark while it waits: they slide on the beat (where to shows dashed a beat before)
		{
			var shb = new List<double>(); var shd = new List<double>(); double[] pat = { 80, 80, -80, 80, -80, -80, 80, -80 };
			for (double b = 177; b < 191; b += 1) { shb.Add(b); shd.Add(pat[shb.Count % 8] * (ez ? 1.0 : 1.2)); }
			at(174.5, () => adds(ez ? BRCurrent(true, 176, 191.5, 85, 155, 200, shb.ToArray(), shd.ToArray(), 60) : BRCurrent(true, 176, 191.5, 90, 150, 160, shb.ToArray(), shd.ToArray(), 80)));
		}



		// the chase
		at(190.5, () => { _brCh.Clear(); _brChSc.Clear(); _brChVox = BR_VOX2.Where(v => v >= 192 && v < 206.5).ToArray(); });
		{
			// each piece locks on where you are a little before the head runs it (its way shows dashed till then)
			double st = ez ? 1 : 0.5, lead = ez ? 1 : 0.75;
			for (double b = 192; b < 206.49; b += st) { double b0 = b, b1 = Math.Min(206.5, b + st); at(b0 - lead, () => add(BRChase(b0, b1, false)), false, true); }
			at(206.5 - lead, () => add(BRChase(206.5, 212, true)), false, true);
			for (double b = 196; b < 206.5; b += 1) if (!ez || b % 2 == 0) strike(b);
		}
		// the second circle: the pack and the school with it
		for (double b = 208; b < 219; b += 4) { if (ez) { strike(b + 1); strike(b + 2); strike(b + 3); } else { strike(b); strike(b + 0.5); cross(b + 1); strike(b + 1.5); cross(b + 2); if (b + 2.5 < 219) strike(b + 2.5); if (b + 3 < 219) cross(b + 3); if (b + 3.5 < 219) strike(b + 3.5); } }
		foreach (var b in ez ? new[] { 211.0, 215 } : new[] { 209.5, 211.5, 213.5, 215.5, 217.5 }) { rake(b, -0.5); if (!ez && (b == 209.5 || b == 213.5)) rake(b, 0.5); }
		at(209.5, () => add(BRSchool("V", 210, 213, -40, W + 40, 240, hole)));
		if (!ez) at(213.5, () => add(BRSchool("H", 214, 217, -40, H + 40, 900, hole)));
		// the last bite: its jaws twice, then it is gone
		double gap = ez ? 190 : 130;
		at(219.5, () => add(BRJaw(220.5, 221, 222, 1, gap)), false, true);
		at(221, () => add(BRJaw(222, 222.25, 223.25, -1, gap)), false, true);
		at(223.5, () => add(BRFx(223.5, 0.35, 10, 1.2)));
		at(223.6, () => add(BREnd(223.6, 226)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
