using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Shiawase VIP (main level 22), part 3: the mask is mended with gold, and the events of the whole level.
public static partial class G
{
	// ---------- 8. the gold rails (2:08.1) ----------
	// 8 half-lines (rails) out of the mended mask, gold (harmless). Every bar they turn 22.5 degrees (the next ones show faint a beat before).
	public static double SWRailA(double bt) => 22.5 * Math.Floor((bt - 245) / 2) + 11.25;
	public static Hz SWRails(double b0, double b1)
	{
		var h = new Hz { kind = "SWRails", z = -1 };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return; double a = SWk(bt, b0 - 1, b0) * (1 - SWk(bt, b1, b1 + 1));
			g.save(); g.lineCap = "round";
			void set(double ra, string col, double w, bool dash)
			{
				g.strokeStyle = col; g.lineWidth = w; if (dash) g.setLineDash(new double[] { 8, 10 }); g.beginPath();
				for (int k = 0; k < 8; k++) { double q = SWRad(ra + 45 * k); g.moveTo(W / 2 + JS.cos(q) * 80, H / 2 + JS.sin(q) * 80); g.lineTo(W / 2 + JS.cos(q) * 820, H / 2 + JS.sin(q) * 820); }
				g.stroke(); g.setLineDash(new double[0]);
			}
			double nb = 245 + 2 * Math.Floor((bt - 245) / 2) + 2;
			if (bt >= nb - 1 && nb < b1) set(SWRailA(nb), SWG(0.35 * a * SWk(bt, nb - 1, nb)), 2, true);
			g.shadowColor = "#ecba4c"; g.shadowBlur = 8; set(SWRailA(Math.Max(bt, 245)), SWG(0.55 * a), 3, false);
			g.restore();
		};
		return h;
	}
	// A shard runs along rail k of the bar of bf (inward = from the edge to the mask, else out), at sp px/s from bf. The rail lights up
	// white (dashed) from bf - tl.
	public static Hz SWRailShard(int k, bool inward, double bf, double tl, double sp)
	{
		var h = new Hz { kind = "SWRailShard" }; const double RA = 80, RB = 820; double tf = B(bf), q = SWRad(SWRailA(bf) + 45 * k), c = JS.cos(q), s = JS.sin(q);
		double rad(double t) => inward ? RB - (t - tf) * sp : RA + (t - tf) * sp;
		h.update = (t, dt) => { if (t > tf && (inward ? rad(t) < RA : rad(t) > RB)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf) { SWDash(W / 2 + c * RA, H / 2 + s * RA, W / 2 + c * RB, H / 2 + s * RB, SWW(0.3 + 0.6 * SWk(bt, bf - tl, bf)), 4); g.restore(); return; }
			double r = rad(t), tail = inward ? Math.Min(RB, r + 90) : Math.Max(RA, r - 90);
			g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 6; g.lineCap = "round";
			g.beginPath(); g.moveTo(W / 2 + c * tail, H / 2 + s * tail); g.lineTo(W / 2 + c * r, H / 2 + s * r); g.stroke();
			g.fillStyle = PINK; SWShard(W / 2 + c * r, H / 2 + s * r, 24, inward ? q + Math.PI : q);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double r = rad(t), tail = inward ? Math.Min(RB, r + 90) : Math.Max(RA, r - 90);
			return SWShardHit(p, W / 2 + c * r, H / 2 + s * r, 24) || SWSeg(p.x, p.y, W / 2 + c * tail, H / 2 + s * tail, W / 2 + c * r, H / 2 + s * r) < 3 + p.r - 2;
		};
		return h;
	}

	// ---------- 9. the corridor of the crack (2:24.1) ----------
	// Two pink walls that scroll past at V px/s (horizontal: right to left; vertical: top to bottom), the way between them bends on the
	// wobble (wb: the bend passes the line where you are, at x 420 / y 470, on that beat) and gets narrower (width per beat: wkb, wkw).
	// Spikes stick out of the walls on the snares (sb), half the way across, alternately from each wall. From bs to be (beats).
	public class SWCorrData { public double[] X, Y; }
	public static Hz SWCorr(bool vert, double bs, double be, double[] wb, double[] sb, double[] wkb, double[] wkw, double V, double maxStep, int seed)
	{
		var h = new Hz { kind = "SWCorr" }; double ts = B(bs), REF = vert ? 470 : 420, LO = vert ? 300 : 170, HI = vert ? 980 : 550, MID = vert ? W / 2 : H / 2, EXT = vert ? H : W;
		// world distance D of a thing that reaches the line REF at beat b: D = V * (B(b) - ts); on screen at time t it is at REF + (D - V*(t - ts))
		// (horizontal: x = that; vertical: y = REF - (D - V*(t - ts)))
		var cx = new List<double> { -1e9 }; var cy = new List<double> { MID };
		double c = MID; int i = 0;
		var wbs = wb.Where(b => b >= bs && b < be).ToArray();
		for (int j = 0; j < wbs.Length; j++)
		{
			double D0 = V * (B(wbs[j]) - ts), D1 = j + 1 < wbs.Length ? V * (B(wbs[j + 1]) - ts) : D0 + 300, len = Math.Min(240, D1 - D0) * 0.9;
			double step = Math.Min(maxStep, 0.55 * len), sg = (SWH(seed + j * 3.7) < 0.5 ? -1 : 1); if (c + sg * step > HI || c + sg * step < LO) sg = -sg;
			cx.Add(D0 - len / 2); cy.Add(c); c += sg * step; cx.Add(D0 + len / 2); cy.Add(c); i++;
		}
		cx.Add(1e9); cy.Add(c);
		double Dstart = V * (B(bs - (vert ? 0 : 1.2)) - ts), Dend = V * (B(be) - ts), FUN = 340;
		double centre(double D) { int k = 1; while (k < cx.Count - 1 && cx[k] < D) k++; double a = cx[k - 1], b = cx[k]; double u = b - a < 1e-6 ? 1 : clamp((D - a) / (b - a), 0, 1); return cy[k - 1] + (cy[k] - cy[k - 1]) * u; }
		double width(double bt, double D)
		{
			double w = wkw[0]; for (int k = 1; k < wkb.Length; k++) w += (wkw[k] - wkw[k - 1]) * SWEase((bt - wkb[k] + 0.5) / 1);
			if (D < Dstart + FUN) w += (900 - w) * (1 - SWEase((D - Dstart) / FUN));
			return w;
		}
		// the spikes: world D, which wall (+1 the low/right one), how long
		var sp = sb.Where(b => b >= bs + 0.5 && b < be - 0.5).Select((b, k) => (D: V * (B(b) - ts), side: k % 2 == 0 ? -1.0 : 1.0)).ToArray();
		double scr(double D, double t) => REF + (D - V * (t - ts));   // along the scroll axis (vertical: flipped below)
		double toScreen(double s) => vert ? REF - (s - REF) : s;
		double fromScreen(double v) => vert ? REF - (v - REF) : v;
		h.update = (t, dt) => { if (scr(Dend, t) < -40) h.dead = true; };
		void walls(double t, Action<double, double, double> f)   // (screen along, low edge, high edge) every 8 px
		{
			double bt = BT(t);
			var vs = new List<double>(); for (double v = -16; v <= EXT + 16; v += 8) vs.Add(v);
			foreach (var Db in new[] { Dstart, Dend }) { double v = toScreen(scr(Db, t)); if (v > -16 && v < EXT + 16) vs.Add(v); }   // the ends exactly where they hurt
			vs.Sort();
			foreach (var v in vs)
			{
				double D = fromScreen(v) - REF + V * (t - ts); if (D < Dstart - 0.01 || D > Dend + 0.01) continue;
				double cc = centre(D), w = width(bt, D); f(v, cc - w / 2, cc + w / 2);
			}
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bs - 4) return;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = SWP(0.78);
			var lo = new List<(double, double)>(); var hi = new List<(double, double)>();
			walls(t, (v, a, b) => { lo.Add((v, a)); hi.Add((v, b)); });
			if (lo.Count > 1)
			{
				void poly(List<(double, double)> L, double edge)
				{
					g.beginPath();
					if (vert) { g.moveTo(edge, L[0].Item1); foreach (var q in L) g.lineTo(q.Item2, q.Item1); g.lineTo(edge, L[L.Count - 1].Item1); }
					else { g.moveTo(L[0].Item1, edge); foreach (var q in L) g.lineTo(q.Item1, q.Item2); g.lineTo(L[L.Count - 1].Item1, edge); }
					g.closePath(); g.fill();
				}
				poly(lo, vert ? -20 : -20); poly(hi, vert ? W + 20 : H + 20);
				// a white glaze line on the wall edges (looks only: the wall is the pink)
				g.shadowBlur = 0; g.strokeStyle = SWW(0.35); g.lineWidth = 2;
				foreach (var L in new[] { lo, hi }) { g.beginPath(); for (int k = 0; k < L.Count; k++) { var q = L[k]; if (vert) { if (k == 0) g.moveTo(q.Item2, q.Item1); else g.lineTo(q.Item2, q.Item1); } else { if (k == 0) g.moveTo(q.Item1, q.Item2); else g.lineTo(q.Item1, q.Item2); } } g.stroke(); }
			}
			// spikes
			g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			foreach (var s in sp)
			{
				double v = toScreen(scr(s.D, t)); if (v < -60 || v > EXT + 60) continue; double cc = centre(s.D), w = width(bt, s.D), base0 = s.side < 0 ? cc - w / 2 : cc + w / 2, tip = base0 - s.side * w * 0.45;
				g.beginPath();
				if (vert) { g.moveTo(base0, v - 26); g.lineTo(tip, v); g.lineTo(base0, v + 26); }
				else { g.moveTo(v - 26, base0); g.lineTo(v, tip); g.lineTo(v + 26, base0); }
				g.closePath(); g.fill();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bs - 4) return false;
			double along = vert ? p.y : p.x, across = vert ? p.x : p.y, D = fromScreen(along) - REF + V * (t - ts);
			if (D >= Dstart && D <= Dend)
			{
				double cc = centre(D), w = width(bt, D);
				if (across < cc - w / 2 + p.r - 2 || across > cc + w / 2 - p.r + 2) return true;
			}
			foreach (var s in sp)
			{
				double v = toScreen(scr(s.D, t)); if (Math.Abs(v - along) > 30) continue; double cc = centre(s.D), w = width(bt, s.D), base0 = s.side < 0 ? cc - w / 2 : cc + w / 2, len = w * 0.45;
				// inside the triangle (base 52 on the wall, tip len across), with the player's size
				double dIn = (across - base0) * -s.side; if (dIn < -p.r || dIn > len + p.r - 2) continue;
				double half = 26 * (1 - clamp(dIn / len, 0, 1)); if (Math.Abs(v - along) < half + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- 10. the seams (2:46.5) ----------
	// The mended mask in the middle, turning (its angle in degrees at beat bt). Each fire (fb) lights one of its 6 seams (a line through the
	// middle across the whole screen): gold dashed from fb - tl, pink from fb for hot beats. Shards circle it (orbit).
	static readonly double[] SWSMB = { 341, 357, 365, 369, 369.5 }, SWSMW = { 11, 11, 22, 22, 0 };
	public static double SWSeamAng(double bt) => SWTurn(SWSMB, SWSMW, Math.Max(341, bt));
	public static Hz SWSeam(int k, double fb, double tl, double hot)
	{
		var h = new Hz { kind = "SWSeam" };
		h.update = (t, dt) => { if (BT(t) > fb + hot + 0.3) h.dead = true; };
		void line(double bt, Action<double, double, double, double> f) { double q = SWRad(SWSeamAng(bt) + 30 * k), c = JS.cos(q), s = JS.sin(q); f(W / 2 - c * 900, H / 2 - s * 900, W / 2 + c * 900, H / 2 + s * 900); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < fb - tl) return;
			g.save(); g.lineCap = "round";
			if (bt < fb) { double q = SWk(bt, fb - tl, fb); line(bt, (x0, y0, x1, y1) => SWDash(x0, y0, x1, y1, SWG(0.35 + 0.65 * q), 2 + 2 * q)); }
			else
			{
				double a = 1 - SWk(bt, fb + hot, fb + hot + 0.3);
				line(bt, (x0, y0, x1, y1) =>
				{
					g.shadowColor = PINK; g.shadowBlur = 20; g.strokeStyle = SWP(0.4 * a); g.lineWidth = 22; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
					g.shadowBlur = 0; g.strokeStyle = SWP(a); g.lineWidth = 12; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke();
				});
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < fb || bt >= fb + hot) return false; bool hit = false; line(bt, (x0, y0, x1, y1) => hit = SWSeg(p.x, p.y, x0, y0, x1, y1) < 8 + p.r - 2); return hit; };
		return h;
	}
	public static Hz SWOrbit(int n, double r, double w, double b0, double b1, double sz = 26)
	{
		var h = new Hz { kind = "SWOrbit" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		void each(double bt, Action<double, double, double> f) { for (int k = 0; k < n; k++) { double a = SWRad(-w * (bt - b0) + k * 360.0 / n); f(W / 2 + JS.cos(a) * r, H / 2 + JS.sin(a) * r, a + Math.PI / 2); } }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1 || bt > b1 + 0.5) return; double a = SWk(bt, b0 - 1, b0) * (1 - SWk(bt, b1, b1 + 0.5));
			g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = bt < b0 ? SWW(0.25 + 0.5 * a) : SWP(a);
			each(bt, (x, y, q) => SWShard(x, y, sz, q)); g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); if (bt < b0 || bt >= b1) return false; bool hit = false; each(bt, (x, y, q) => { if (!hit && SWShardHit(p, x, y, sz)) hit = true; }); return hit; };
		return h;
	}

	// ---------- 11. the smile (2:57.7) ----------
	// The last hit: at bf everything is pink for dur beats except a circle (cx, cy, r) - it shows from b0.
	public static Hz SWSmile(double cx, double cy, double r, double b0, double bf, double dur)
	{
		var h = new Hz { kind = "SWSmile" };
		h.update = (t, dt) => { if (BT(t) > bf + dur + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return;
			g.save();
			if (bt < bf)
			{
				double q = SWk(bt, b0, bf), pu = 0.5 + 0.5 * JS.sin(t * 14);
				// white hatching over everything that will hurt
				g.strokeStyle = SWW(0.08 + 0.12 * q); g.lineWidth = 2; g.beginPath();
				for (double x = -H; x < W; x += 34) { g.moveTo(x, H); g.lineTo(x + H, 0); }
				g.stroke();
				g.fillStyle = "rgba(10,12,24,0.85)"; circle(cx, cy, r);
				g.strokeStyle = SWG(0.6 + 0.4 * pu); g.lineWidth = 5; g.shadowColor = "#ecba4c"; g.shadowBlur = 16; g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.stroke();
			}
			else if (bt < bf + dur + 0.3)
			{
				double a = 1 - SWk(bt, bf + dur, bf + dur + 0.3);
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = SWP(0.75 * a); g.beginPath(); g.rect(0, 0, W, H); g.arc(cx, cy, r, 0, TAU, true); g.fill("evenodd");
				g.shadowBlur = 0; g.strokeStyle = SWG(a); g.lineWidth = 4; g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) => { double bt = BT(t); return bt >= bf && bt < bf + dur && hypot(p.x - cx, p.y - cy) > r - p.r + 2; };
		return h;
	}
	public static Hz SWEnd(double b0, double b1)
	{
		var h = new Hz { kind = "SWEnd" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = SWk(bt, b0, b0 + 0.3), a = Math.Min(k, 1 - SWk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(104 - 18 * k)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#ecba4c"; g.shadowBlur = 30; g.fillStyle = "#f6dc96"; g.fillText("SHIAWASE", W / 2, H - 110); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	// ---------- the events ----------
	static readonly int _swb = DodgeBuild("L22", () => buildSW());
	static List<Ev> buildSW()
	{
		var E = new List<Ev>(); bool ez = easy;
		void at(double b, Action f, bool keep = false, bool rnd = false) => E.Add(new Ev { b = b, f = f, keep = keep, rnd = rnd });
		void add(Hz z) => hazards.Add(z);
		at(0, () => add(SWStage(B(380))), true);

		// ===== 1. it cracks (0:30-1:16.9) =====
		{
			double hot = ez ? 1.3 : 2.0;
			double[] gr(double a, double b) { var l = SW_GROW.Where(x => x >= a && x < b).ToArray(); return l.Length >= 3 ? l : new[] { a, a + 1, a + 2, a + 3 }; }
			var lines = new List<SWLine>
			{
				SWMakeLine(640, -10, 640, H + 10, 12, 26, 1, gr(1, 5), 0.25, hot),
				SWMakeLine(-10, 240, W + 10, 240, 16, 22, 2, gr(5, 9), 4.25, hot),
				SWMakeLine(W + 10, 480, -10, 480, 16, 22, 3, gr(5, 9), 4.25, hot),
				SWMakeLine(320, H + 10, 320, -10, 12, 26, 4, gr(9, 13), 8.25, hot),
				SWMakeLine(960, -10, 960, H + 10, 12, 26, 5, gr(9, 13), 8.25, hot),
			};
			lines.Add(SWMakeLine(-10, -10, W + 10, H + 10, 20, 24, 6, gr(13, 17), 12.25, hot));
			if (!ez) { lines.Add(SWMakeLine(W + 10, -10, -10, H + 10, 20, 24, 7, gr(13, 17), 12.25, hot)); lines.Add(SWMakeLine(-10, 120, W + 10, 600, 18, 24, 8, gr(1, 5), 0.25, hot)); }
			var arr = lines.ToArray();
			at(0, () => add(SWCracks(arr, 17)), true);
			// the tiles: checkerboard halves in turn on the chops (Normal from 0:34.1, Easy from 0:35.3 every other chop)
			int A = 0, Bm = 0; for (int k = 0; k < 12; k++) { if (((k % 4) + (k / 4)) % 2 == 0) A |= 1 << k; else Bm |= 1 << k; }
			var ch = new List<double>(); { double last = -9; foreach (var x in SW_CHOP1) if (x >= (ez ? 10 : 9) && x < 17 && x - last >= 1) { ch.Add(x); last = x; } }
			for (int i = 0; i < ch.Count; i++) { double cb = ch[i]; int m = i % 2 == 0 ? A : Bm; at(cb - 1, () => add(SWTile(m, cb, ez ? 1 : 0.8, ez ? 0.5 : 0.7))); }
		}
		at(17, () => add(SWFx(17, 0.3, 10, 1.2)));
		at(16, () => add(SWPlateBg()), true);
		{
			// the plate's cracks (3 at once, on the bass) and the rim's chips (on the snare)
			var rad = SW_RAD.Where(x => x >= 18 && x < 84).ToArray(); if (ez) { var l = new List<double>(); double last = -9; foreach (var x in rad) if (x - last >= 1.5) { l.Add(x); last = x; } rad = l.ToArray(); }
			for (int i = 0; i < rad.Length; i++) { double b = rad[i], a0 = i * 41; at(b - 0.75, () => add(SWRadial(a0, ez ? 3 : (b >= 37 ? 4 : 3), b - 0.75, b, ez ? 1.4 : 2.0, 85))); }
			var sn = SW_SNR1.Where(x => x >= 18 && x < 83).ToArray(); for (int i = 0; i < sn.Length; i++)
			{
				double b = sn[i], a0 = 22.5 * (i % 2) + 7 * i; bool cross = b >= (ez ? 53 : 37) && i % 2 == 1;
				at(b - 0.75, () => add(SWChips(ez ? 8 : 12, a0, !cross, b, 0.75, ez ? 500 : 690, 20)));
			}
		}
		// the frozen moment: the pieces hang, turn, are pulled in and blow out at 1:16.9
		at(84, () => add(ez ? SWFrozen(new[] { 150.0, 255, 360, 465 }, 8, new[] { 20.0, 26, 32, 38 }, 85, 117, 90) : SWFrozen(new[] { 130.0, 215, 300, 385, 470 }, 12, new[] { 30.0, 38, 46, 54, 62 }, 85, 117, 130)));
		at(117, () => add(SWFx(117, 0.45, 16, 1.6)));

		// ===== 2. the shards attack (1:16.9-2:08.1) =====
		{
			double gw = ez ? 190 : 150, sp = ez ? 540 : 660; double[] gxs = { 220, 440, 660, 880, 1060, 840, 620, 400 };
			int gi = 0;
			// bass bars: falling tilted rows, the gap zigzags
			var rain = SW_RAIN.ToArray(); if (ez) { var l = new List<double>(); double last = -9; foreach (var x in rain) if (x - last >= 0.75) { l.Add(x); last = x; } rain = l.ToArray(); }
			foreach (var b in rain)
			{
				double tilt = (Math.Floor((b - 117) / 8) % 2 == 0 ? 1 : -1) * 16, gx = gxs[gi++ % gxs.Length];
				at(b - 0.75, () => add(SWRow(1, tilt, gx, gw, b, 0.75, sp)));
			}
			// chop bars: rows from below that jump up on the chops and stop
			foreach (var bar in new[] { 121.0, 129, 137 })
			{
				var st = SW_CHOP2.Where(x => x >= bar && x < bar + 4).ToArray(); if (st.Length < 3) st = new[] { bar, bar + 1, bar + 2, bar + 3 };
				double[] gx2 = bar == 129 ? new[] { 960.0, 320 } : new[] { 320.0, 960 };
				var s1 = st.Where((v, i) => i % 2 == 0).ToArray(); double step1 = (H + 120) / Math.Max(3, s1.Length);
				at(bar - 1, () => add(SWRow(-1, 0, gx2[0], gw, bar, 1, 0, st, (H + 120) / Math.Max(3, st.Length))));
				{ var s2 = st.Where(x => x >= bar + 1.5).ToArray(); if (s2.Length >= 2) at(bar + 0.5, () => add(SWRow(-1, 0, gx2[1], gw, bar + 1.5, 1, 0, s2, (H + 120) / Math.Max(3, s2.Length)))); }
			}
			// 1:29.7 the shards come back: rows from the top on the beat and from below every other beat (gaps apart)
			int k2 = 0;
			for (double b0 = 149; b0 < 177; b0 += 1)
			{
				double b = b0, gx = gxs[(k2 * 3) % gxs.Length], tilt = (k2 % 2 == 0 ? 12 : -12); k2++;
				at(b - 0.75, () => add(SWRow(1, tilt, gx, gw, b, 0.75, sp)));
				if (!ez && (b - 149) % 2 == 1) { double gu = gxs[(k2 * 3 + 4) % gxs.Length]; at(b - 0.25, () => add(SWRow(-1, -tilt, gu, gw, b + 0.5, 0.75, 560))); }
			}
		}
		at(181, () => add(SWFx(181, 0.45, 16, 1.6)));
		// the eyes (1:42.5-1:55): two beams like scissors, burnt lines on the kicks, the chin's fans on the snares
		{
			var kb = SW_KICK3.Where(x => x >= 182 && x < 213).ToArray();
			at(181, () => add(SWEyes(181, 213, kb, ez ? 1.25 : 1.75, 1)));
			var sn = SW_SNR3.Where(x => x >= 183 && x < 212).ToArray();
			var m = SWMaskAt(181);
			for (int i = 0; i < sn.Length; i++) { double b = sn[i], a0 = (i % 2 == 0 ? -1 : 1) * 9; at(b - 0.6, () => add(SWFan(m.x, m.y + 25, ez ? 5 : 9, a0, ez ? 22 : 13, b, 0.6, ez ? 560 : 700))); }
		}
		// the broken rings (1:55-2:08): a ring every 2 beats, the gap goes round; the hand turns opposite the gap
		{
			double g0 = 90;
			for (int j = 0; j < 14; j++) { double b = 215 + 2 * j, gap = g0 + 50 * j; at(b - 0.75, () => add(SWRing(ez ? 26 : 34, gap, ez ? 64 : 42, b, 0.75, ez ? 360 : 470))); }
			at(214, () => add(SWHand(g0, 25, 216, 214, 244)));
		}
		at(245, () => add(SWFx(245, 0.35, 10, 1.4)));

		// ===== 3. the gold (2:08.1-3:00) =====
		at(244, () => add(SWRails(245, 285)), true);
		{
			int[] ord = { 0, 3, 6, 1, 4, 7, 2, 5 };
			var hb = SW_HAT.Where(x => x >= 245.5 && x < 284.6).ToArray(); if (ez) { var l = new List<double>(); double last = -9; foreach (var x in hb) if (x - last >= 0.75) { l.Add(x); last = x; } hb = l.ToArray(); }
			for (int i = 0; i < hb.Length; i++)
			{
				double b = hb[i]; int k = ord[i % 8];
				double tl = ez ? 0.75 : 0.6; at(b - tl, () => add(SWRailShard(k, true, b, tl, ez ? 1150 : 1600)));
				if (ez ? b >= 265 && i % 2 == 1 : b >= 249) at(b - tl, () => add(SWRailShard((k + 4) % 8, false, b, tl, ez ? 1150 : 1600)));
			}
		}
		at(285, () => add(SWFx(285, 0.25, 8, 1)));
		// the corridor: across (2:24.1), then down (2:36.9)
		at(281, () => add(ez ? SWCorr(false, 285, 317, SW_WOB, SW_SNR4.Where((v, i) => i % 2 == 0).ToArray(), new[] { 285.0, 297, 309 }, new[] { 300.0, 265, 240 }, 580, 120, 11)
			: SWCorr(false, 285, 317, SW_WOB, SW_SNR4, new[] { 285.0, 293, 301, 309 }, new[] { 225.0, 195, 172, 155 }, 700, 150, 11)));
		at(313, () => add(ez ? SWCorr(true, 317, 341, SW_WOB, SW_SNR4.Where((v, i) => i % 2 == 0).ToArray(), new[] { 317.0, 329 }, new[] { 290.0, 255 }, 540, 120, 23)
			: SWCorr(true, 317, 341, SW_WOB, SW_SNR4, new[] { 317.0, 325, 333 }, new[] { 215.0, 188, 165 }, 660, 150, 23)));
		at(341, () => add(SWFx(341, 0.3, 10, 1.2)));
		// the seams (2:46.5): one fires on each kick, from 2:53 (beat 357) on every beat too; shards circle the mask
		{
			var fb = SW_KICK5.Where(x => x >= 342 && x < 369).ToList(); for (double b = 349; b < 369; b += 1) fb.Add(b); if (!ez) for (double b = 353.5; b < 369; b += 1) fb.Add(b);
			fb = fb.Distinct().OrderBy(x => x).ToList(); if (ez) { var l = new List<double>(); double last = -9; foreach (var x in fb) if (x - last >= 1.5) { l.Add(x); last = x; } fb = l; }
			for (int i = 0; i < fb.Count; i++) { double b = fb[i]; int k = (i * 5) % 6; at(b - 0.85, () => add(SWSeam(k, b, 0.85, 0.7))); }
			at(341, () => add(ez ? SWOrbit(6, 250, 12, 342, 369) : SWOrbit(10, 240, 20, 342, 369)));
		}
		// the smile: everything hurts at 2:59.3 except the circle under the mask's smile
		at(369, () => add(SWSmile(W / 2, 600, ez ? 100 : 72, 369, 373, 0.6)));
		at(373, () => add(SWFx(373, 0.4, 12, 1.4)));
		at(373.6, () => add(SWEnd(373.6, 375.5)));
		JS.sort(E, (a, b) => a.b - b.b); return E;
	}
}
