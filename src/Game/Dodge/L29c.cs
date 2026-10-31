using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Trouble, main level 29, part 3: "trouble, trouble, trouble" (the alarm, as hard as level 4), the heart, the doors, the door closes
public static partial class G
{
	// ---------- 8. the siren (1:36.8): two pink beams through the middle turn round it ----------
	// speed in degrees a beat, in straight lines between the keys (it turns back at 248)
	static readonly double[] TBSRB = { 232, 240, 247.75, 248.25, 256, 264 }, TBSRW = { 26, 32, 34, -34, -40, -40 };
	public static double TBSirenAng(double bt) => 90 + TBTurn(TBSRB, easy ? TBSRW.Select(v => v * 0.7).ToArray() : TBSRW, Math.Max(232, bt));
	public static Hz TBSiren(double b0, double b1, double tl, double w)
	{
		var h = new Hz { kind = "TBSiren" };
		h.update = (t, dt) => { if (BT(t) > b1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - tl) return; double q = TBRad(TBSirenAng(bt));
			g.save(); g.lineCap = "butt";
			for (int k = 0; k < 2; k++)
			{
				double a = q + k * Math.PI, c = JS.cos(a), s = JS.sin(a);
				if (bt < b0) TBDash(W / 2, H / 2, W / 2 + c * 1500, H / 2 + s * 1500, TBW(0.3 + 0.6 * TBk(bt, b0 - tl, b0)), 3);
				else { g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = w; g.beginPath(); g.moveTo(W / 2, H / 2); g.lineTo(W / 2 + c * 1500, H / 2 + s * 1500); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; double q = TBRad(TBSirenAng(bt));
			for (int k = 0; k < 2; k++) { double a = q + k * Math.PI; if (TBSeg(p.x, p.y, W / 2, H / 2, W / 2 + JS.cos(a) * 1500, H / 2 + JS.sin(a) * 1500) < w / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- 9. the warning tape: a pink striped bar (height hh, length len) shoots in from the left (side -1) or right (1) at y ----------
	// It is in on beat b (0.12 beat to come), stays hold beats, goes back in 0.12. Its whole place shows dashed from b - tl.
	public static Hz TBTape(int side, double y, double hh, double len, double b, double tl, double hold)
	{
		var h = new Hz { kind = "TBTape" };
		double ext(double bt) => bt < b - 0.12 ? 0 : bt < b ? len * TBOut((bt - b + 0.12) / 0.12) : bt < b + hold ? len : len * (1 - TBk(bt, b + hold, b + hold + 0.12));
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.15) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			double x0 = side < 0 ? 0 : W - len;
			if (bt < b) { double q = TBk(bt, b - tl, b); g.strokeStyle = TBW(0.3 + 0.6 * q); g.lineWidth = 2.5; g.setLineDash(new double[] { 12, 9 }); g.strokeRect(x0, y - hh / 2, len, hh); g.setLineDash(new double[0]); g.fillStyle = TBW(0.04 + 0.08 * q); g.fillRect(x0, y - hh / 2, len, hh); }
			double e = ext(bt);
			if (e > 0.5)
			{
				double xa = side < 0 ? 0 : W - e;
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(xa, y - hh / 2, e, hh); g.shadowBlur = 0;
				// light pink stripes on it (still pink: all of it hurts)
				g.save(); g.beginPath(); g.rect(xa, y - hh / 2, e, hh); g.clip(); g.fillStyle = PINKW;
				for (double sx = xa - hh - ((bt * 40) % 56); sx < xa + e + hh; sx += 56) { g.beginPath(); g.moveTo(sx, y + hh / 2); g.lineTo(sx + 22, y + hh / 2); g.lineTo(sx + 22 + hh, y - hh / 2); g.lineTo(sx + hh, y - hh / 2); g.closePath(); g.fill(); }
				g.restore();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), e = ext(bt); if (e <= 0.5) return false;
			double xa = side < 0 ? 0 : W - e, m = p.r - 2;
			return p.x > xa - m && p.x < xa + e + m && Math.Abs(p.y - y) < hh / 2 + m;
		};
		return h;
	}
	// the word TROUBLE (white, harmless) behind the alarm, it flashes on the beats in fb
	public static Hz TBWord(double b0, double b1, double[] fb)
	{
		var h = new Hz { kind = "TBWord", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double a = TBk(bt, b0, b0 + 0.5) * (1 - TBk(bt, b1, b1 + 1)), f = 0;
			foreach (var x in fb) if (bt >= x && bt < x + 0.6) f = Math.Max(f, 1 - (bt - x) / 0.6);
			g.save(); g.globalAlpha = a; g.font = $"{str(150 + 14 * f)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
			g.fillStyle = TBW(0.06 + 0.22 * f); g.fillText("TROUBLE", W / 2, H / 2); g.strokeStyle = TBY(0.25 + 0.5 * f); g.lineWidth = 2; g.strokeText("TROUBLE", W / 2, H / 2);
			g.direction = "rtl"; g.restore();
		};
		return h;
	}

	// ---------- 10. the doors slam (1:58.6): two pink doors close from the sides and leave a gap (centre gx, width gw) ----------
	// They close in 0.2 beat to b, stay hold beats, open in 0.25. From b - tl the gap shows in white and the doors' edges dashed.
	public static Hz TBDoors(double gx, double gw, double b, double tl, double hold)
	{
		var h = new Hz { kind = "TBDoors" };
		double k(double bt) => bt < b - 0.2 ? 0 : bt < b ? TBOut((bt - b + 0.2) / 0.2) : bt < b + hold ? 1 : 1 - TBk(bt, b + hold, b + hold + 0.25);
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			double l = gx - gw / 2, r = gx + gw / 2;
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b);
				TBDash(l, 0, l, H, TBW(0.3 + 0.6 * q), 3); TBDash(r, 0, r, H, TBW(0.3 + 0.6 * q), 3);
				g.fillStyle = TBW(0.04 + 0.1 * q); g.fillRect(l, 0, gw, H);
			}
			double kk = k(bt);
			if (kk > 0.002)
			{
				double lw = l * kk, rw = (W - r) * kk;
				g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(0, 0, lw, H); g.fillRect(W - rw, 0, rw, H); g.shadowBlur = 0;
				// panels on the doors (light pink, still the door)
				g.strokeStyle = PINKW; g.lineWidth = 3;
				if (lw > 60) { g.strokeRect(lw - 50 > 20 ? 20 : lw - 50, 40, Math.Max(10, lw - 70), 280); g.strokeRect(lw - 50 > 20 ? 20 : lw - 50, 380, Math.Max(10, lw - 70), 300); }
				if (rw > 60) { g.strokeRect(W - rw + 50, 40, Math.Max(10, rw - 70), 280); g.strokeRect(W - rw + 50, 380, Math.Max(10, rw - 70), 300); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), kk = k(bt); if (kk <= 0.002) return false; double l = gx - gw / 2, r = gx + gw / 2, m = p.r - 2;
			return p.x < l * kk + m || p.x > W - (W - r) * kk - m;
		};
		return h;
	}

	// ---------- 11. the door closes (2:24.3): everything is pink but the doorway (centre cx,cy, width dw, height dh) ----------
	// The doorway shows from b0 (white frame, warm light inside), the pink comes in from the edges in 0.5 beat to b1, stays to b2
	public static Hz TBDoorway(double cx, double cy, double dw, double dh, double b0, double b1, double b2)
	{
		var h = new Hz { kind = "TBDoorway" };
		double cover(double bt) => TBOut((bt - b1 + 0.5) / 0.5);   // 0-1: how far the pink has come in from the edges (1: to the doorway)
		(double l, double r, double tp, double bo) box(double c) => ((cx - dw / 2) * c, W - (W - cx - dw / 2) * c, (cy - dh / 2) * c, H - (H - cy - dh / 2) * c);
		h.update = (t, dt) => { if (BT(t) > b2 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double c = cover(bt), a = 1 - TBk(bt, b2, b2 + 0.4), q = TBk(bt, b0, b1 - 0.5);
			g.save(); g.globalAlpha = a;
			var lg = g.createLinearGradient(cx, cy - dh / 2, cx, cy + dh / 2); lg.addColorStop(0, TBY(0.10 + 0.25 * q)); lg.addColorStop(1, TBY(0.03)); g.fillStyle = lg; g.fillRect(cx - dw / 2, cy - dh / 2, dw, dh);
			if (c > 0 && bt < b2)
			{
				var (l, r, tp, bo) = box(c); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath();
				g.rect(0, 0, W, H); g.rect(l, tp, r - l, bo - tp); g.fill("evenodd"); g.shadowBlur = 0;
			}
			g.strokeStyle = TBW(0.5 + 0.5 * q); g.lineWidth = 4; g.strokeRect(cx - dw / 2 - 3, cy - dh / 2 - 3, dw + 6, dh + 6);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b1 - 0.5 || bt >= b2) return false; var (l, r, tp, bo) = box(cover(bt)); double m = p.r - 2;
			return p.x < l + m || p.x > r - m || p.y < tp + m || p.y > bo - m;
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _tbb = DodgeBuild("L29", () => buildTB());
	// the groove's beats: 0, 1.5 and 3 of every 4 (the 3-3-2 of the beatbox)
	static double[] TB332(double a, double b) { var l = new List<double>(); for (double x = Math.Floor(a / 4) * 4; x < b; x += 4) foreach (var o in new[] { 0, 1.5, 3 }) if (x + o >= a && x + o < b) l.Add(x + o); return l.ToArray(); }
	static double[] TBThin(double[] a, double gap) { var l = new List<double>(); double last = -1e9; foreach (var x in a) if (x - last >= gap - 1e-9) { l.Add(x); last = x; } return l.ToArray(); }
	static List<Ev> buildTB()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false) => E.Add(new Ev { b = b, f = f, keep = keep });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(TBStage(B(362))), true);

		// ===== 1. he walks in (0:06.4-0:46.9) =====
		// the knocks on the door: rings from the door with a gap at the bottom
		{
			var kb = ez ? new[] { 1.0, 4 } : new[] { 1.0, 3, 4, 5.5 };
			for (int i = 0; i < kb.Length; i++) { double b = kb[i], ga = TBRad(90 + (i % 2 == 0 ? -28 : 28)); at(b - 1.2, () => add(TBRing(W / 2, 110, b, 1.2, ez ? 420 : 520, 8, ga, TBRad(ez ? 90 : 62)))); }
		}
		// the giant's footsteps: out of the door, round the room (21 steps on the 3-3-2), each stamp sends a thin ring
		{
			var st = TB332(8, 36); if (ez) st = TBThin(st, 2.5);
			int n = TB332(8, 36).Length;
			for (int i = 0; i < st.Length; i++)
			{
				double b = st[i]; int j = Array.IndexOf(TB332(8, 36), b);
				double th = TBRad(-90 + j * 360.0 / n * 0.92), px = W / 2 + JS.cos(th) * 420, py = 380 + JS.sin(th) * 215;
				double ta = th + Math.PI / 2; bool left = j % 2 == 0; double off = left ? -46 : 46;
				double x = px + JS.cos(ta + Math.PI / 2) * off, y = py + JS.sin(ta + Math.PI / 2) * off;
				at(b - 1.5, () => add(TBStep(x, y, ta, 1.25, left, b, ez ? 1.5 : 1.0, ez ? 0.55 : 0.7, ez ? 0 : 230)));
			}
		}
		// Normal: the warnings start already under the steps (on the snares from 0:12.2)
		if (!ez) { double[] sb0 = { 15, 16, 17, 20, 21.5, 22.5, 23, 31, 33.5, 35 }, x0 = { 1080, 200, 1080, 640, 200, 1080, 640, 200, 1080, 640 }; for (int i = 0; i < sb0.Length; i++) { double b = sb0[i], x = x0[i]; at(b - 1.2, () => add(TBBang(x, b, 1.0, 0.5, 56))); } }
		at(36, () => add(TBFx(36, 0.25, 8, 1)));
		// the red flags: strings of pennants fall (and rise) on the 3-3-2, the "!" warnings shoot up on the snares
		{
			double[] gxs = { 320, 620, 940, 700, 400, 860, 560, 280 };
			int gi = 0;
			for (double s = 36; s < 64; s += 4)
			{
				double b0 = s, b1 = s + 8; int dir = ((int)(s / 4)) % 2 == 0 ? 1 : -1; var jb = TB332(b0, b1); double gx = gxs[gi++ % gxs.Length];
				at(b0 - 1, () => add(TBBunting(false, dir, b0, b1, jb, new[] { gx }, ez ? 230 : 145, 90)));
			}
			double[] bxs = { 200, 360, 520, 680, 840, 1000, 1160, 1000, 840, 680, 520, 360 };
			var sb = TB_S1.Where(x => x < 62.5).ToArray(); if (ez) sb = TBThin(sb, 2);
			for (int i = 0; i < sb.Length; i++) { double b = sb[i], x = bxs[i % bxs.Length]; at(b - 1.2, () => add(TBBang(x, b, ez ? 1.2 : 1.0, ez ? 0.5 : 0.6, ez ? 56 : 64))); }
			// the strings turn: they sweep across from the sides (1:04)
			double[] gys = { 200, 470, 300, 560, 380, 180 };
			for (double s = 64; s < 80; s += 4)
			{
				double b0 = s, b1 = s + 8; int dir = ((int)(s / 4)) % 2 == 0 ? 1 : -1; var jb = TB332(b0, b1); double gy = gys[gi++ % gys.Length];
				at(b0 - 1, () => add(TBBunting(true, dir, b0, b1, jb, new[] { gy }, ez ? 240 : 170, 70)));
			}
			var sb2 = TB_S1.Where(x => x >= 64 && x < 79).ToArray(); if (ez) sb2 = TBThin(sb2, 3);
			for (int i = 0; i < sb2.Length; i++) { double b = sb2[i], x = bxs[(i * 5) % bxs.Length]; at(b - 1.2, () => add(TBBang(x, b, ez ? 1.2 : 1.0, ez ? 0.5 : 0.6, ez ? 56 : 64))); }
			// both ways at once (0:37.5)
			for (double s = 80; s < 96; s += 4)
			{
				double b0 = s, b1 = s + 7.5; var jb = TB332(b0, b1); double gx = gxs[gi % gxs.Length], gy = gys[gi % gys.Length]; gi++;
				at(b0 - 1, () => add(TBBunting(false, 1, b0, b1, jb, new[] { gx }, ez ? 240 : 200, 90)));
				if (s < 92 && !ez && s % 8 == 0) { double c0 = s + 2, c1 = s + 9.5; var jc = TB332(c0, c1); int dir = s % 16 == 0 ? 1 : -1; at(c0 - 1, () => add(TBBunting(true, dir, c0, c1, jc, new[] { gy }, 225, 70))); }
			}
			// the last warnings before the breakdown
			var sb3 = ez ? new[] { 97.5, 101 } : new[] { 97.5, 100, 101, 101.5 };
			for (int i = 0; i < sb3.Length; i++) { double b = sb3[i], x = new[] { 360.0, 920, 640, 200 }[i]; at(b - 1.2, () => add(TBBang(x, b, ez ? 1.2 : 1.0, ez ? 0.5 : 0.6, ez ? 56 : 64))); }
		}
		at(104, () => add(TBFx(104, 0.35, 10, 1.4)));

		// ===== 2. he flies you away (0:46.9-1:36.8) =====
		// the paper planes on the breakdown's hits: 3 a time (Easy 2) on four kinds of ways
		{
			double[][] ways =
			{
				new double[] { -80, 610, 420, 610, 700, 90, 1360, 190 },
				new double[] { 1360, 250, 1060, 250, 820, 250, 640, 250, 380, 250, 380, 560, 640, 560, 900, 560, 900, 250, 640, 250, 420, 250, 200, 300, -80, 300 },
				new double[] { 300, -80, 300, 320, 980, 400, 980, 800 },
				new double[] { 1360, 520, 900, 700, 380, 0, -80, 140 },
				new double[] { 980, 800, 980, 380, 300, 320, 300, -80 },
				new double[] { -80, 470, 260, 470, 260, 160, 640, 160, 1020, 160, 1020, 470, 1360, 470 },
			};
			double[] durs = { 2.5, 4.5, 2.5, 2.5, 2.5, 3.5 };
			var kb = ez ? TBThin(TB_KB, 3) : TB_KB;
			for (int i = 0; i < kb.Length; i++)
			{
				double b = kb[i]; int k = i % ways.Length; var c = ways[k]; double dur = durs[k] * (ez ? 1.25 : 0.85);
				var offs = ez ? new[] { (0.0, 0.0), (-70.0, -85.0), (-70.0, 85.0) } : new[] { (0.0, 0.0), (-70.0, -85.0), (-70.0, 85.0), (-140.0, 0.0) };
				if (k == 2 || k == 4) offs = ez ? new[] { (0.0, 0.0), (-85.0, -70.0), (85.0, -70.0) } : new[] { (0.0, 0.0), (-85.0, -70.0), (85.0, -70.0), (0.0, -140.0) };
				foreach (var (ox, oy) in offs) { var w = TBMakeWay(c, ox, oy); at(b - 1.5, () => add(TBPlane(w, b, dur, ez ? 1.5 : 1.0, ez ? 34 : 40, ez ? 0.45 : 1.1))); }
			}
		}
		at(136, () => add(TBFx(136, 0.25, 8, 1)));
		// the pins: a journey round a five-pointed star, each route drawn to the next pin on the 3-3-2
		{
			var pb = TB332(136, 150); if (ez) pb = TBThin(pb, 2.5);   // the concert starts on 152 (1:05.6): the last pins cool before it
			double px = W / 2, py = 140;
			for (int i = 0; i < pb.Length; i++)
			{
				double b = pb[i], th = TBRad(-90 + i * 144 + 9 * (i / 5)), nx = W / 2 + JS.cos(th) * 450, ny = 370 + JS.sin(th) * 240;
				double x0 = px, y0 = py, bu = (i % 2 == 0 ? 1 : -1) * 70; px = nx; py = ny;
				double hold = Math.Max(0.75, Math.Min(ez ? 2.5 : 3.5, 151.5 - b));
				at(b - 2.0, () => add(TBPin(x0, y0, nx, ny, bu, b - 0.5, ez ? 1.5 : 1.2, hold, ez ? 150 : 230)));
			}
		}
		// ===== the hype (1:05.6-1:18.1): the concert, every sound on the screen, easy to play (a wide lane always stays free) =====
		{
			at(149, () => add(TBShow(TB_HK, TB_HS, TB_HV)));
			at(152, () => add(TBFx(152, 0.25, 6, 0.8)));
			var kb = TB_HK.Where(x => x < 164 || x >= 168).ToArray(); var sb = TB_HS.Where(x => x < 166 || x >= 168).ToArray();
			if (ez) { kb = TBThin(kb, 2); sb = TBThin(sb, 2); }
			foreach (var b in kb) at(b - 1.3, () => add(TBEq(false, b, ez ? 1.3 : 0.9)));
			foreach (var b in sb) at(b - 1.3, () => add(TBEq(true, b, ez ? 1.3 : 0.9)));
			// the shouts: a wave from one speaker then the other (on 1:11.2 both shout at once)
			var vb = ez ? new[] { 168, 172, 175, 178, 180.75 } : TB_HV; int sd = -1;
			foreach (var b in vb)
			{
				int s = sd; sd = -sd;
				at(b - 1.5, () => add(TBShout(s, b, 1.2, ez ? 520 : 640, ez ? 2 : 3, ez ? 10 : 12)));
				if (!ez && b == 168) at(b - 1.5, () => add(TBShout(-s, b, 1.2, 640, 3, 12)));
			}
			at(168, () => add(TBFx(168, 0.3, 8, 0.8)));
		}
		// the globe turns: lines of longitude sweep across with a gap that rolls up and down
		{
			var mb = TB332(164, 200).Where(x => ez ? x % 4 == 0 || x >= 182 && x % 4 == 1.5 : x >= 182 || x % 4 != 3).ToArray();
			for (int i = 0; i < mb.Length; i++)
			{
				if (mb[i] < 184) continue;   // before 184 is the concert
				double b = mb[i]; int dir = b < 182 ? -1 : 1; double gy = 360 + 210 * JS.sin(i * 0.62);
				at(b - 1.5, () => add(TBMeridian(dir, b, 1.2, ez ? 380 : 460, 150, gy, ez ? 230 : 175)));
			}
		}
		at(200, () => add(TBFx(200, 0.3, 10, 1.2)));
		// put me down: heavy hearts fall on the 3-3-2 and break
		{
			var db = TB332(200, 231); if (ez) db = TBThin(db, 2.5);   // from 224 no pieces (all gone before the save at 232)
			for (int i = 0; i < db.Length; i++)
			{
				double b = db[i]; double x = W / 2 + (i % 2 == 0 ? -1 : 1) * (120 + 130 * (i % 3)), y = 190 + (i * 67) % 360;
				at(b - 1.6, () => add(TBDrop(x, y, b, ez ? 1.6 : 1.4, ez ? 62 : 70, b >= 224 ? 0 : ez ? 4 : 9, 18 * i, ez ? 380 : 640)));
			}
		}
		at(232, () => add(TBFx(232, 0.5, 16, 1.6)));

		// ===== 3. trouble (1:36.8-2:26.6) =====
		// the alarm (as hard as level 4): the siren turns, the tape shoots in on the snares, "TROUBLE"
		{
			at(230, () => add(TBSiren(232, 264, 2, ez ? 20 : 28)));
			at(231.5, () => add(TBWord(232, 263, TB_SX)));
			double[] ys = { 130, 590, 250, 470, 360, 130, 590, 470, 250 };
			var tb = ez ? TBThin(TB_SX, 2) : TB_SX;
			for (int i = 0; i < tb.Length; i++) { double b = tb[i], y = ys[i % ys.Length]; int side = i % 2 == 0 ? -1 : 1; at(b - 1.2, () => add(TBTape(side, y, 74, W * (ez ? 0.36 : 0.46), b, ez ? 0.95 : 0.6, 0.6))); }
			if (!ez)
			{
				// the 3-3-2 hits: a ring from the siren with a wide gap that follows the beams
				foreach (var b in TB332(240, 264)) { double bb = b; at(bb - 1, () => add(TBRing(W / 2, H / 2, bb, 1, 560, 8, TBRad(TBSirenAng(bb) + 90), TBRad(56), 20))); }
			}
		}
		at(264, () => add(TBFx(264, 0.4, 12, 1.4)));
		// the heart beats: rings with a gap that goes round
		{
			var rb = TB332(264, 287).Where(x => x % 4 == 0 || !ez && x % 4 == 1.5).ToArray();
			for (int i = 0; i < rb.Length; i++) { double b = rb[i], ga = TBRad(-90 + 30 * i); at(b - 1.2, () => add(TBRing(W / 2, H / 2 - 10, b, 1.2, ez ? 420 : 470, 12, ga, TBRad(ez ? 95 : 86), 20))); }
		}
		// the doors slam every bar, the footsteps walk along the top and the bottom
		{
			double[] gxs = { 640, 380, 700, 960, 640, 320, 560, 860, 600, 340 };
			int i = 0;
			for (double b = 292; b < 328; b += 4) { double bb = b, gx = gxs[i++ % gxs.Length], tl = bb == 292 ? 3 : ez ? 2.6 : 2.4; at(bb - 3, () => add(TBDoors(gx, ez ? 300 : 220, bb, tl, ez ? 0.6 : 0.75))); }
			var fs = ez ? TBThin(TB_K3, 3) : TBThin(TB_K3, 1.5);
			// he walks across the room and back (stride 160, the feet 90 apart), Normal: each stamp sends a ring
			for (int k = 0; k < fs.Length; k++)
			{
				double b = fs[k]; int lap = k / 7, j = k % 7; bool fwd = lap % 2 == 0, left = k % 2 == 0;
				double x = fwd ? 160 + 160 * j : 1120 - 160 * j, y = (lap % 2 == 0 ? 250 : 470) + (left ? -45 : 45) * (fwd ? 1 : -1);
				at(b - 1.5, () => add(TBStep(x, y, fwd ? 0 : Math.PI, 1.15, left, b, ez ? 1.4 : 1.25, 0.5, ez ? 0 : 150)));
			}
		}
		at(328, () => add(TBFx(328, 0.3, 10, 1.2)));
		// all of it once more: the flags fall and the heart beats, then he walks out
		{
			double[] gxs = { 640, 360, 900, 520, 780, 300 };
			int gi = 0;
			for (double s = 328; s < 348; s += 4) { double b0 = s, b1 = s + 7; var jb = TB332(b0, b1); double gx = gxs[gi++ % gxs.Length]; int dir = gi % 2 == 0 ? -1 : 1; at(b0 - 1, () => add(TBBunting(false, dir, b0, b1, jb, new[] { gx }, ez ? 270 : 200, 80))); }
			var rb = ez ? new double[0] : TB332(330, 350).Where(x => x % 8 == 1.5).ToArray();
			for (int i = 0; i < rb.Length; i++) { double b = rb[i], ga = TBRad(90 + 45 * i); at(b - 1.2, () => add(TBRing(W / 2, H / 2 - 10, b, 1.2, ez ? 420 : 520, 12, ga, TBRad(ez ? 90 : 64), 20))); }
		}
		// the door closes: everything but the doorway
		at(351, () => add(TBDoorway(W / 2, 360, 200, 270, 352, 356.5, 359.5)));
		at(359.5, () => add(TBFx(359.5, 0.5, 12, 1.6)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
