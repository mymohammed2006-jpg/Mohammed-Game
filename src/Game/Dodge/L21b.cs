using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Barracuda (main level 21), part 2: the pack and the school. See L21.cs
public static partial class G
{
	// a barracuda (or a small fish) facing +x with its nose at (0,0), drawn at (x,y) turned a
	public static void BRFishShape(double x, double y, double a, double L, double w, string col, bool eye = true)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = col;
		g.beginPath(); g.moveTo(0, 0); g.lineTo(-0.12 * L, -w * 0.38); g.lineTo(-0.4 * L, -w * 0.5); g.lineTo(-0.78 * L, -w * 0.3); g.lineTo(-0.9 * L, -w * 0.12);
		g.lineTo(-L, -w * 0.62); g.lineTo(-0.95 * L, 0); g.lineTo(-L, w * 0.62); g.lineTo(-0.9 * L, w * 0.12); g.lineTo(-0.78 * L, w * 0.3); g.lineTo(-0.4 * L, w * 0.5); g.lineTo(-0.12 * L, w * 0.38); g.closePath(); g.fill();
		if (eye) { g.fillStyle = "rgba(40,0,14,0.7)"; g.beginPath(); g.moveTo(-0.02 * L, w * 0.08); g.lineTo(-0.2 * L, w * 0.2); g.lineTo(-0.2 * L, w * 0.1); g.closePath(); g.fill(); g.fillStyle = "#fff"; circle(-0.1 * L, -w * 0.14, Math.Max(2, w * 0.09)); }
		g.restore();
	}
	// how wide a barracuda is at u (0 the nose .. 1 the tail), for the hit
	static double BRWid(double u, double w) => u < 0.12 ? w * 0.38 * u / 0.12 + 2 : u < 0.4 ? w * (0.38 + 0.12 * (u - 0.12) / 0.28) : u < 0.9 ? w * (0.5 - 0.38 * (u - 0.4) / 0.5) : w * 0.12;
	static bool BRFishHit(Player p, double hx, double hy, double dx, double dy, double L, double w)
	{
		double rx = p.x - hx, ry = p.y - hy, along = -(rx * dx + ry * dy), side = Math.Abs(rx * dy - ry * dx);
		if (along < -p.r || along > L + p.r) return false; double u = clamp(along / L, 0, 1);
		return side < BRWid(u, w) * 0.85 + p.r - 2;
	}

	// the strikes the school gives way to: start (x,y), way (dx,dy), the lunge's time t1 and speed
	public class BRCut { public double x, y, dx, dy, t1, sp; }
	static readonly List<BRCut> _brCuts = new();
	// a point outside the screen on the "clock": k = 0 the top, then clockwise, 8 places
	public static (double x, double y) BRClock(double k)
	{
		double a = -Math.PI / 2 + k * Math.PI / 4, c = JS.cos(a), s = JS.sin(a);
		double t = Math.Min(Math.Abs(c) < 1e-6 ? 1e9 : (W / 2 + 90) / Math.Abs(c), Math.Abs(s) < 1e-6 ? 1e9 : (H / 2 + 90) / Math.Abs(s));
		return (W / 2 + c * t, H / 2 + s * t);
	}

	// ---------- the pack: one barracuda strike ----------
	// From the clock place k it locks onto the square at b0 (its eye glints at the screen's edge, its way shows dashed; turned fa rad, moved
	// po px across: a fan, or two rails on both sides of you), and at b1 it lunges along it at sp px/s. Aimed: rnd
	public static Hz BRStrike(double k, double fa, double po, double b0, double b1, double sp, double L = 300, double w = 36)
	{
		var h = new Hz { kind = "BRStrike" }; var st = BRClock(k); double sx = st.x, sy = st.y, dx = 1, dy = 0, ent = 0, ex = 0; bool lk = false; double t1 = B(b1);
		void aim()
		{
			double tx = player?.x ?? W / 2, ty = player?.y ?? H / 2, l = Math.Max(1, hypot(tx - st.x, ty - st.y)); dx = (tx - st.x) / l; dy = (ty - st.y) / l;
			double c = JS.cos(fa), s = JS.sin(fa), nx = dx * c - dy * s, ny = dx * s + dy * c; dx = nx; dy = ny; sx = st.x - dy * po; sy = st.y + dx * po;
			// where the way comes into the screen and where it leaves
			ent = 0; for (double d = 0; d < 2400; d += 4) { double x = sx + dx * d, y = sy + dy * d; if (x > 0 && x < W && y > 0 && y < H) { ent = d; break; } }
			ex = ent + BRExit(sx + dx * ent, sy + dy * ent, dx, dy, 0);
			_brCuts.Add(new BRCut { x = sx, y = sy, dx = dx, dy = dy, t1 = t1, sp = sp });
		}
		double head(double t) => t < t1 ? ent + 6 : ent + 6 + (t - t1) * sp;
		h.update = (t, dt) => { if (!lk) { aim(); lk = true; } h.z = t < t1 ? -1 : 0; if (head(t) - L > ex + 60) h.dead = true; };
		h.draw = t =>
		{
			if (!lk) return; double bt = BT(t), d = head(t), hx = sx + dx * d, hy = sy + dy * d, a = JS.atan2(dy, dx);
			if (t < t1)
			{
				double k2 = BRk(bt, b0, b1);
				BRDash(sx + dx * ent, sy + dy * ent, sx + dx * ex, sy + dy * ex, BRW(0.35 + 0.5 * k2), 2 + 2 * k2);
				g.save(); g.globalAlpha = 0.55; BRFishShape(hx, hy, a, L, w, "rgba(200,210,230,0.5)", false); g.restore();
				// the glint
				double gx = hx - dx * 0.1 * L, gy = hy - dy * 0.1 * L, r = 7 + 9 * (1 - k2); g.strokeStyle = BRW(0.95); g.lineWidth = 3;
				g.beginPath(); g.moveTo(gx - r, gy); g.lineTo(gx + r, gy); g.moveTo(gx, gy - r); g.lineTo(gx, gy + r); g.stroke(); g.fillStyle = "#fff"; circle(gx, gy, 4);
				return;
			}
			g.save(); g.fillStyle = "rgba(210,235,255,0.22)";
			for (int i = 1; i < 7; i++) { double bd = d - L - i * 45; if (bd < ent) break; circle(sx + dx * bd + JS.sin(i * 2.1 + d * 0.01) * 10, sy + dy * bd + JS.cos(i * 1.7) * 10, 5 - 0.6 * i); }
			g.shadowColor = PINK; g.shadowBlur = 16; BRFishShape(hx, hy, a, L, w, PINK); g.restore();
		};
		h.hit = (p, t) => { if (!lk || t < t1) return false; double d = head(t); return BRFishHit(p, sx + dx * d, sy + dy * d, dx, dy, L, w); };
		return h;
	}

	// a strike that went past moves a fish of the school aside (a corridor behind it that closes a beat later)
	static (double x, double y) BRCutPos(double x, double y, double t)
	{
		foreach (var c in _brCuts)
		{
			double rx = x - c.x, ry = y - c.y, along = rx * c.dx + ry * c.dy, side = rx * c.dy - ry * c.dx; if (Math.Abs(side) > 120) continue;
			double tp = c.t1 + along / c.sp, k = t - tp; if (k < -0.06 || k > 1.0) continue;
			double f = k < 0 ? 1 + k / 0.06 : k < 0.45 ? 1 : 1 - (k - 0.45) / 0.55; f = BREase(f);
			double push = (120 - Math.Abs(side)) * f * (side >= 0 ? 1 : -1); x += c.dy * push; y += -c.dx * push;
		}
		return (x, y);
	}

	// ---------- the school's whirl (0:52-0:59) ----------
	// arms arms of fish (two rows each) curling out from the middle (curl rad per px), turning w rad/s; they curl in from the edge over the
	// first beat and out over the last. Fixed: not aimed
	public static Hz[] BRSwirl(double b0, double b1, int arms, double w, double curl, bool two, double rb = -1)
	{
		var h = new Hz { kind = "BRSwirl" }; var tel = new Hz { kind = "BRSwirlTell", z = -1 }; double t0 = B(b0), tA = B(b1), cx = W / 2, cy = H / 2; const double STEP = 28, FR = 9;
		var buf = new List<(double x, double y, double a)>();
		void fish(double t, List<(double x, double y, double a)> o)
		{
			o.Clear(); double bt = BT(t), rin = 900 * (1 - BREase(BRk(bt, b0, b0 + 1))), rout = 900 * BREase(BRk(bt, b1 - 1, b1)), rot = rb < 0 || t < B(rb) ? w * (t - t0) : w * (2 * B(rb) - t0 - t);
			double sg = rb < 0 || t < B(rb) ? Math.Sign(w) : -Math.Sign(w);
			for (int j = 0; j < arms; j++)
				for (double r = 20; r < 880; r += STEP)
				{
					if (r < rin || r < rout) continue; double q = rot + j * TAU / arms + r * curl;
					double tx = -JS.sin(q) * sg, ty = JS.cos(q) * sg, hd = JS.atan2(ty, tx);
					o.Add((cx + JS.cos(q) * r, cy + JS.sin(q) * r, hd));
					if (two) foreach (var e in new[] { 1.0, 2.0 }) { double q2 = q - e * 18 / Math.Max(r, 40); o.Add((cx + JS.cos(q2) * (r + 14 * e), cy + JS.sin(q2) * (r + 14 * e), hd)); }
				}
		}
		h.update = (t, dt) => { if (t > tA + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0 || t > tA) return; fish(t, buf); g.save(); g.shadowColor = PINK; g.shadowBlur = 10;
			foreach (var f in buf) { var c = BRCutPos(f.x, f.y, t); if (c.x < -40 || c.x > W + 40 || c.y < -40 || c.y > H + 40) continue; BRFishShape(c.x + JS.cos(f.a) * 12, c.y + JS.sin(f.a) * 12, f.a + 0.15 * JS.sin(t * 9 + f.x * 0.05), 26, 13, PINK, false); }
			g.restore();
		};
		tel.update = (t, dt) => { if (t > tA + 0.2) tel.dead = true; };
		tel.draw = t =>
		{
			if (t < t0 || t > tA) return; g.save();
			if (rb > 0 && BT(t) >= rb - 1.5 && BT(t) < rb)
			{
				// it will turn the other way: white arrows round the middle the new way
				double k = BRk(BT(t), rb - 1.5, rb), nw = -Math.Sign(w); g.strokeStyle = BRW(0.35 + 0.55 * k); g.lineWidth = 4;
				for (int i = 0; i < 6; i++) { double q0 = i * TAU / 6 + 0.6 * k * nw, q1 = q0 + 0.5 * nw, R = 250; g.beginPath(); g.arc(W / 2, H / 2, R, Math.Min(q0, q1), Math.Max(q0, q1)); g.stroke(); double ax = W / 2 + JS.cos(q1) * R, ay = H / 2 + JS.sin(q1) * R, tx = -JS.sin(q1) * nw, ty = JS.cos(q1) * nw; g.beginPath(); g.moveTo(ax + tx * 14, ay + ty * 14); g.lineTo(ax - ty * 10, ay + tx * 10); g.lineTo(ax + ty * 10, ay - tx * 10); g.closePath(); g.fillStyle = BRW(0.35 + 0.55 * k); g.fill(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > tA) return false; fish(t, buf);
			foreach (var f in buf) { if (Math.Abs(p.x - f.x) > 150 || Math.Abs(p.y - f.y) > 150) continue; var c = BRCutPos(f.x, f.y, t); if (hypot(p.x - c.x, p.y - c.y) < FR + p.r - 2) return true; }
			return false;
		};
		return new[] { tel, h };
	}

	// ---------- the pack in a row: a rake of barracudas ----------
	// From side d (0 the left, 1 the right, 2 the top, 3 the bottom) a row of them side by side over the whole side, but for the gaps
	// (centres gs, gw px wide). Their eyes glint at the edge and the swept band shows from b0; at b1 they lunge at sp px/s, one after
	// another across the row (stg beats from the first to the last; stg < 0 the other way). Fixed: not aimed
	public static Hz[] BRRake(int d, double b0, double b1, double[] gs, double gw, double sp, double stg = 0, double L = 300, double w = 36)
	{
		var h = new Hz { kind = "BRRake" }; var tel = new Hz { kind = "BRRakeTell", z = -1 }; double t0 = B(b0), t1 = B(b1), ext = d < 2 ? H : W, along = d < 2 ? W : H;
		double dx = d == 0 ? 1 : d == 1 ? -1 : 0, dy = d == 2 ? 1 : d == 3 ? -1 : 0; var cs = new List<(double c, double td)>();
		for (double c = 18; c < ext; c += 36) { bool gap = false; foreach (var q in gs) if (Math.Abs(c - q) < gw / 2 + w * 0.42) gap = true; if (!gap) cs.Add((c, t1 + Math.Abs(stg) * SPB * (stg >= 0 ? c / ext : 1 - c / ext))); }
		double tEnd = cs.Count > 0 ? cs.Max(v => v.td) : t1;
		// the head of fish c at time t
		(double x, double y) head(double c, double td, double t)
		{
			double a = -10 + Math.Max(0, t - td) * sp; return d == 0 ? (a, c) : d == 1 ? (W - a, c) : d == 2 ? (c, a) : (c, H - a);
		}
		h.update = (t, dt) => { if (t > tEnd + (along + L + 40) / sp) h.dead = true; };
		tel.update = (t, dt) => { if (t > tEnd) tel.dead = true; };
		// under everything: the band they will sweep (the gaps left clear and dashed), their eyes glinting at the edge
		tel.draw = t =>
		{
			if (t < t0 || t >= tEnd) return; double k = BRk(BT(t), b0, b1); g.save();
			g.fillStyle = BRW(0.05 + 0.1 * k);
			foreach (var f in cs) { if (t >= f.td) continue; if (d < 2) g.fillRect(0, f.c - 18, W, 36); else g.fillRect(f.c - 18, 0, 36, H); }
			foreach (var q in gs) { if (d < 2) BRDash(0, q, W, q, BRW(0.25 + 0.4 * k), 2); else BRDash(q, 0, q, H, BRW(0.25 + 0.4 * k), 2); }
			foreach (var f in cs) if (t < f.td) { var p = head(f.c, f.td, t); double gx = p.x + dx * 8, gy = p.y + dy * 8, r = 4 + 6 * (1 - k); g.strokeStyle = BRW(0.9); g.lineWidth = 2; g.beginPath(); g.moveTo(gx - r, gy); g.lineTo(gx + r, gy); g.moveTo(gx, gy - r); g.lineTo(gx, gy + r); g.stroke(); }
			g.restore();
		};
		h.draw = t =>
		{
			if (t < t1) return; double ang = JS.atan2(dy, dx); g.save(); g.shadowColor = PINK; g.shadowBlur = 14;
			foreach (var f in cs) if (t >= f.td) { var p = head(f.c, f.td, t); BRFishShape(p.x, p.y, ang, L, w, PINK); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t1) return false; double pc = d < 2 ? p.y : p.x;
			foreach (var f in cs) { if (t < f.td || Math.Abs(pc - f.c) > 40) continue; var q = head(f.c, f.td, t); if (BRFishHit(p, q.x, q.y, dx, dy, L, w)) return true; }
			return false;
		};
		return new[] { tel, h };
	}

	// ---------- the school's currents ----------
	// Bands of the school swimming across (vt: up/down bands that swim sideways... false: rows swimming along x, the bands stacked in y;
	// true: columns), band px thick, gap px between, the first band's centre at o0. They gather over the first beat (their places dashed
	// 1.5 beats before) and go over the last. At every shb[i] the whole pattern slides by shd[i] px in half a beat (where they will be shows
	// dashed a beat before). Fixed: not aimed
	public static Hz[] BRCurrent(bool vt, double b0, double b1, double band, double gap, double o0, double[] shb, double[] shd, double fsp)
	{
		var h = new Hz { kind = "BRCurrent" }; var tel = new Hz { kind = "BRCurrentTell", z = -1 }; double t0 = B(b0), tA = B(b1), P = band + gap, ext = vt ? W : H, len = vt ? H : W;
		double off(double bt) { double o = o0; for (int i = 0; i < shb.Length; i++) { if (bt < shb[i]) break; o += shd[i] * BREase(BRk(bt, shb[i], shb[i] + 0.4)); } return o; }
		double thick(double bt) => band * BREase(Math.Min(BRk(bt, b0, b0 + 1), 1 - BRk(bt, b1 - 1, b1)));
		// the band centres across for offset o
		IEnumerable<double> centres(double o) { double c0 = o - Math.Ceiling((o + P) / P) * P; for (double c = c0; c < ext + P; c += P) yield return c; }
		h.update = (t, dt) => { if (t > tA + 0.2) h.dead = true; };
		tel.update = (t, dt) => { if (t > tA + 0.2) tel.dead = true; };
		tel.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1.5 || bt > b1) return; g.save();
			void outline(double o, double a)
			{
				foreach (var c in centres(o)) foreach (var e in new[] { c - band / 2, c + band / 2 }) { if (vt) BRDash(e, 0, e, H, BRW(a), 2); else BRDash(0, e, W, e, BRW(a), 2); }
			}
			if (bt < b0) outline(off(b0), 0.3 + 0.4 * BRk(bt, b0 - 1.5, b0));
			for (int i = 0; i < shb.Length; i++) if (bt >= shb[i] - 1 && bt < shb[i]) outline(off(shb[i] + 0.4), 0.35 + 0.5 * BRk(bt, shb[i] - 1, shb[i]));
			g.restore();
		};
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 || bt > b1) return; g.save();
			double th = thick(bt);
			if (th > 1)
			{
				double o = off(bt), sc = (t - t0) * fsp; g.shadowColor = PINK; g.shadowBlur = 8;
				foreach (var c in centres(o))
					for (double q = -th / 2 + 9; q <= th / 2 - 9 + 0.1; q += Math.Max(1, (th - 18) / Math.Max(1, Math.Round((th - 18) / 22))))
					{
						int row = (int)Math.Round((q + 1000) / 22); double dirn = row % 2 == 0 ? 1 : -1, sh = ((sc * dirn + row * 11) % 26 + 26) % 26;
						for (double a = -26 + sh; a < len + 26; a += 26) { double x = vt ? c + q : a, y = vt ? a : c + q; if (x < -30 || x > W + 30 || y < -30 || y > H + 30) continue; BRFishShape(vt ? x : x + 11 * dirn, vt ? y + 11 * dirn : y, vt ? (dirn > 0 ? Math.PI / 2 : -Math.PI / 2) : (dirn > 0 ? 0 : Math.PI), 24, 14, PINK, false); }
					}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt > b1) return false; double th = thick(bt); if (th < 4) return false; double pc = vt ? p.x : p.y, o = off(bt);
			foreach (var c in centres(o)) if (Math.Abs(pc - c) < th / 2 + p.r - 3) return true;
			return false;
		};
		return new[] { tel, h };
	}

	// ---------- the school ----------
	// One shape of the school for beats b0 .. b1:
	//   "V": a wall across the screen going from x0 to x1 (hole hy, hole px wide);  "H": a wall going down/up from y0 to y1 (hole hx)
	//   "C": a chevron (apex at x0,y0 heading a deg, sp px/s) that turns by fa[i] deg at fb[i] (its new way shows a beat before)
	// Every fish near a barracuda's strike gives way as it goes past (a corridor behind it that closes a beat later)
	public static Hz BRSchool(string type, double b0, double b1, double p0, double p1, double hp, double hole, double a = 0, double sp = 0, double[] fb = null, double[] fa = null)
	{
		var h = new Hz { kind = "BRSchool" }; const double FR = 9, GAP = 30; fb ??= new double[0]; fa ??= new double[0];
		double t0 = B(b0), tA = B(b1);
		// the chevron's apex and heading at time t
		(double x, double y, double h) apex(double t)
		{
			double x = p0, y = p1, hd = a * Math.PI / 180, last = t0;
			for (int i = 0; i < fb.Length; i++)
			{
				double tf = B(fb[i]); if (t < tf) break;
				x += JS.cos(hd) * sp * (tf - last); y += JS.sin(hd) * sp * (tf - last); last = tf; hd += fa[i] * Math.PI / 180;
			}
			x += JS.cos(hd) * sp * (t - last); y += JS.sin(hd) * sp * (t - last); return (x, y, hd);
		}
		// the base places (no cuts) of the fish and their heading
		void fish(double t, List<(double x, double y, double a)> o)
		{
			o.Clear(); double u = clamp((t - t0) / (tA - t0), 0, 1);
			if (type == "V") { double x = p0 + (p1 - p0) * u, hd = p1 > p0 ? 0 : Math.PI; for (double y = -30; y < H + 40; y += GAP) if (Math.Abs(y - hp) > hole / 2) { o.Add((x, y, hd)); if (Math.Abs(y + GAP / 2 - hp) > hole / 2) o.Add((x - 22 * Math.Cos(hd), y + GAP / 2, hd)); } }
			else if (type == "H") { double y = p0 + (p1 - p0) * u, hd = p1 > p0 ? Math.PI / 2 : -Math.PI / 2; for (double x = -30; x < W + 40; x += GAP) if (Math.Abs(x - hp) > hole / 2) { o.Add((x, y, hd)); if (Math.Abs(x + GAP / 2 - hp) > hole / 2) o.Add((x + GAP / 2, y - 22 * Math.Sin(hd), hd)); } }
			else
			{
				var ap = apex(t); o.Add((ap.x, ap.y, ap.h));
				for (int j = 1; j <= 8; j++) foreach (var sd in new[] { -1.0, 1.0 }) { double q = ap.h + Math.PI + sd * 0.62; o.Add((ap.x + JS.cos(q) * GAP * j, ap.y + JS.sin(q) * GAP * j, ap.h)); }
			}
		}
		(double x, double y) cut(double x, double y, double t) => BRCutPos(x, y, t);
		var buf = new List<(double x, double y, double a)>();
		h.update = (t, dt) => { if (t > tA + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0 - 0.01 || t > tA) return; double bt = BT(t); fish(t, buf);
			g.save(); g.shadowColor = PINK; g.shadowBlur = 10;
			foreach (var f in buf) { var c = cut(f.x, f.y, t); if (c.x < -40 || c.x > W + 40 || c.y < -40 || c.y > H + 40) continue; BRFishShape(c.x + JS.cos(f.a) * 12, c.y + JS.sin(f.a) * 12, f.a + 0.15 * JS.sin(t * 9 + f.x * 0.05), 26, 13, PINK, false); }
			g.shadowBlur = 0;
			// a chevron's next way
			if (type == "C") for (int i = 0; i < fb.Length; i++) if (bt >= fb[i] - 1 && bt < fb[i])
					{ var ap = apex(B(fb[i])); double q = ap.h + fa[i] * Math.PI / 180, k = BRk(bt, fb[i] - 1, fb[i]); BRDash(ap.x, ap.y, ap.x + JS.cos(q) * 260, ap.y + JS.sin(q) * 260, BRW(0.35 + 0.55 * k), 3); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > tA) return false; fish(t, buf);
			foreach (var f in buf) { var c = cut(f.x, f.y, t); if (Math.Abs(p.x - c.x) < 30 && Math.Abs(p.y - c.y) < 30 && hypot(p.x - c.x, p.y - c.y) < FR + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- the bait ball (1:12.8-1:14.6) ----------
	// The school rings the square (locked at bl): n fish (the first gn missing = the way out) at r0 shrinking to r1 from b0 to bc, turning w rad/s.
	// At bb it bursts: every fish flies out along its line at sp px/s (the lines dashed half a beat before)
	public static Hz BRBall(double bl, double b0, double bc, double bb, int n, int gn, double r0, double r1, double w, double sp)
	{
		var h = new Hz { kind = "BRBall" }; double cx = W / 2, cy = H / 2; bool lk = false; double t0 = B(b0), tc = B(bc), tb = B(bb);
		double ang(int i, double t) { double tt = Math.Min(t, tc) - t0 + (t > tc ? 0.35 * (Math.Min(t, tb) - tc) : 0); return i * TAU / n + w * tt; }
		double rad(double t) { if (t < tc) return r0 + (r1 - r0) * BREase((t - t0) / (tc - t0)); if (t < tb) return r1; return r1 + sp * (t - tb); }
		h.update = (t, dt) => { if (!lk && BT(t) >= bl) { lk = true; cx = clamp(player?.x ?? W / 2, 300, W - 300); cy = clamp(player?.y ?? H / 2, 250, H - 250); } if (rad(t) > 1600) h.dead = true; };
		h.draw = t =>
		{
			if (!lk) return; double bt = BT(t), r = rad(t); bool on = t >= t0;
			g.save();
			if (bt >= bb - 0.5 && bt < bb) { double k = BRk(bt, bb - 0.5, bb); for (int i = gn; i < n; i++) { double q = ang(i, tb); BRDash(cx + JS.cos(q) * r1, cy + JS.sin(q) * r1, cx + JS.cos(q) * 1500, cy + JS.sin(q) * 1500, BRW(0.25 + 0.5 * k), 2); } }
			if (on) { g.shadowColor = PINK; g.shadowBlur = 10; }
			for (int i = gn; i < n; i++) { double q = ang(i, t), x = cx + JS.cos(q) * r, y = cy + JS.sin(q) * r, hd = t < tb ? q + Math.PI / 2 * Math.Sign(w) : q; BRFishShape(x + JS.cos(hd) * 12, y + JS.sin(hd) * 12, hd, 26, 13, on ? PINK : "rgba(200,210,230,0.45)", false); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (!lk || t < t0) return false; double r = rad(t);
			for (int i = gn; i < n; i++) { double q = ang(i, t), x = cx + JS.cos(q) * r, y = cy + JS.sin(q) * r; if (hypot(p.x - x, p.y - y) < 9 + p.r - 2) return true; }
			return false;
		};
		return h;
	}
}
