using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L12 "Hard Spike": the neon spike-trap tower. Every spike shows a grey tip first, then stabs pink on the beat.
// Phase 1 the ground floor (beds of spikes on the edges, spiked crushers, lances), phase 2 the crystal mine (falling crystals that shatter,
// rolling spiked gears, crystals growing in the dark), phase 3 the summit and the neon hedgehog (quills, rolls along the edges).
// Every hazard here is a pure function of time once it exists (hit(p, t) is right for any later t), so the S bot can look ahead.
// The data is data/dodge_L12.json (made by tools/level12/gen.py).
public static partial class G
{
	// edges: b = floor, t = ceiling, l = left wall, r = right wall. u = along the edge, v = in from it
	static double L12len(string e) => e == "l" || e == "r" ? H : W;
	static (double x, double y) L12pt(string e, double u, double v) => e switch { "t" => (u, v), "l" => (v, u), "r" => (W - v, u), _ => (u, H - v) };
	static (double u, double v) L12uv(string e, double x, double y) => e switch { "t" => (x, y), "l" => (y, x), "r" => (y, W - x), _ => (x, H - y) };
	const string L12G = "rgba(215,215,230,";

	// a row of teeth along an edge from u0 to u1, height hgt, drawn in the current fill
	static void L12teeth(string e, double u0, double u1, double hgt, double tw = 40)
	{
		int n = Math.Max(1, (int)round((u1 - u0) / tw)); double w = (u1 - u0) / n;
		g.beginPath();
		for (int i = 0; i < n; i++)
		{
			var a = L12pt(e, u0 + i * w, 0); var m = L12pt(e, u0 + i * w + w / 2, hgt); var c = L12pt(e, u0 + (i + 1) * w, 0);
			g.moveTo(a.x, a.y); g.lineTo(m.x, m.y); g.lineTo(c.x, c.y); g.closePath();
		}
		g.fill();
	}
	static void L12teethS(string e, double u0, double u1, double hgt, double tw = 40)
	{
		int n = Math.Max(1, (int)round((u1 - u0) / tw)); double w = (u1 - u0) / n;
		g.beginPath();
		for (int i = 0; i < n; i++)
		{
			var a = L12pt(e, u0 + i * w, 0); var m = L12pt(e, u0 + i * w + w / 2, hgt); var c = L12pt(e, u0 + (i + 1) * w, 0);
			g.moveTo(a.x, a.y); g.lineTo(m.x, m.y); g.lineTo(c.x, c.y);
		}
		g.stroke();
	}
	// stab: 0 before ta, out in 0.06 s, held to te, back in 0.16 s
	static double L12stab(double t, double ta, double te) => t < ta ? 0 : t < ta + 0.06 ? (t - ta) / 0.06 : t < te ? 1 : clamp(1 - (t - te) / 0.16, 0, 1);

	// ---------- a bed of spikes: segments along an edge stab one after another ----------
	static Hz L12Bed(string e, double u0, double sw, int n, double d, double[] ta, double[] te, double tw0)
	{
		var h = new Hz { kind = "bed" }; double last = te.Max();
		h.update = (t, dt) => { if (t > last + 0.2) h.dead = true; };
		h.draw = t =>
		{
			for (int i = 0; i < n; i++)
			{
				if (double.IsNaN(ta[i])) continue; double a = u0 + i * sw, b = a + sw, tw = ta[i] - tw0;
				if (t < tw) continue;
				if (t < ta[i])
				{
					double k = clamp((t - tw) / (ta[i] - tw), 0, 1);
					var p0 = L12pt(e, a, 0); var p1 = L12pt(e, b, d);
					g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.13 * k)})"; g.fillRect(Math.Min(p0.x, p1.x), Math.Min(p0.y, p1.y), Math.Abs(p1.x - p0.x), Math.Abs(p1.y - p0.y));
					g.fillStyle = L12G + str(0.35 + 0.5 * k) + ")"; L12teeth(e, a, b, 10 + 14 * k);
					continue;
				}
				double s = L12stab(t, ta[i], te[i]); if (s <= 0) continue;
				g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = t - ta[i] < 0.07 ? PINKW : PINK; L12teeth(e, a, b, d * s * (1 + 0.06 * pulse)); g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			var q = L12uv(e, p.x, p.y);
			for (int i = 0; i < n; i++)
			{
				if (double.IsNaN(ta[i]) || t < ta[i] || t >= te[i]) continue; double a = u0 + i * sw;
				if (q.u > a - p.r + 4 && q.u < a + sw + p.r - 4 && q.v < d * 0.78 * L12stab(t, ta[i], te[i]) + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}
	static readonly int _k12bed = DodgeKind("bed", e => () =>
	{
		// e: edge, u0/u1: the part of the edge, sw: segment width, d: depth, hit: first stab beat, step: beats between segments,
		// dir: 1 from u0, -1 from u1, gaps: segments that stay down, dur: beats held, wn: warning beats
		string ed = e.str("e", "b"); double u0 = e.num("u0", 0), u1 = e.num("u1", L12len(ed)), sw = e.num("sw", 80), d = e.num("d", 140);
		int n = Math.Max(1, (int)round((u1 - u0) / sw)); sw = (u1 - u0) / n;
		double hit = e.num("hit"), step = e.num("step", 0), dur = e.num("dur", 0.75), wn = e.num("wn", 1.5); int dir = e.i("dir", 1);
		var gaps = e.nums("gaps").Select(x => (int)x).ToHashSet(); var ta = new double[n]; var te = new double[n];
		for (int i = 0; i < n; i++) { int j = dir > 0 ? i : n - 1 - i; double b = hit + i * step; ta[j] = gaps.Contains(j) ? double.NaN : B(b); te[j] = B(b + dur); }
		hazards.Add(L12Bed(ed, u0, sw, n, d, ta, te, B(hit) - B(hit - wn)));
	});

	// ---------- a spiked crusher: a block slides in from an edge on the beat and goes back ----------
	static Hz L12Crusher(string e, double a, double b, double d, double tw, double ta, double te, double slide = 0)
	{
		var h = new Hz { kind = "crusher" }; double sl = Math.Max(0.08, slide);
		Func<double, double> dep = t => t < ta ? 0 : t < ta + sl ? d * (slide > 0 ? (t - ta) / sl : (t - ta) / sl) : t < te ? d : d * clamp(1 - (t - te) / 0.22, 0, 1);
		h.update = (t, dt) => { if (t > te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); var p0 = L12pt(e, a, 0); var p1 = L12pt(e, b, d + 22);
				g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.14 * k)})"; g.fillRect(Math.Min(p0.x, p1.x), Math.Min(p0.y, p1.y), Math.Abs(p1.x - p0.x), Math.Abs(p1.y - p0.y));
				g.strokeStyle = L12G + str(0.3 + 0.5 * k) + ")"; g.lineWidth = 2; var q0 = L12pt(e, a, 0); var q1 = L12pt(e, b, d * k);
				g.strokeRect(Math.Min(q0.x, q1.x), Math.Min(q0.y, q1.y), Math.Abs(q1.x - q0.x), Math.Abs(q1.y - q0.y));
				return;
			}
			double dd = dep(t); if (dd <= 0) return; bool fl = t - ta < 0.05;
			var r0 = L12pt(e, a, 0); var r1 = L12pt(e, b, dd);
			g.save(); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = fl ? PINKW : PINK;
			g.fillRect(Math.Min(r0.x, r1.x), Math.Min(r0.y, r1.y), Math.Abs(r1.x - r0.x), Math.Abs(r1.y - r0.y));
			// the teeth on its face
			g.translate(0, 0); g.beginPath(); int n = Math.Max(1, (int)round((b - a) / 44)); double w = (b - a) / n;
			for (int i = 0; i < n; i++) { var p0 = L12pt(e, a + i * w, dd - 1); var m = L12pt(e, a + i * w + w / 2, dd + 22); var p1 = L12pt(e, a + (i + 1) * w, dd - 1); g.moveTo(p0.x, p0.y); g.lineTo(m.x, m.y); g.lineTo(p1.x, p1.y); g.closePath(); }
			g.fill(); g.shadowBlur = 0;
			// dark bolts on the block
			g.fillStyle = "rgba(0,0,0,0.35)";
			for (double u = a + 26; u < b - 14; u += 52) { var c = L12pt(e, u, dd * 0.5); if (dd > 30) { g.beginPath(); g.arc(c.x, c.y, 5, 0, TAU); g.fill(); } }
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te + 0.1) return false; var q = L12uv(e, p.x, p.y); return q.u > a - p.r + 2 && q.u < b + p.r - 2 && q.v < dep(t) + 16 + p.r - 2; };
		return h;
	}
	static readonly int _k12crush = DodgeKind("crush", e => () =>
	{
		// e: edge, u0/u1: the part of the edge it covers, d: how far it comes in, hit, dur, wn (beats)
		string ed = e.str("e", "t"); double hit = e.num("hit"), wn = e.num("wn", 1.5);
		hazards.Add(L12Crusher(ed, e.num("u0"), e.num("u1", L12len(ed)), e.num("d", 200), B(hit - wn), B(hit), B(hit + e.num("dur", 0.75)), e.num("slide", 0) * SPB));
		if (e.flag("boom")) L12Shake(B(hit), 9);
	});
	// a harmless helper that shakes the screen at time ta
	static void L12Shake(double ta, double s)
	{
		var h = new Hz { kind = "fx" }; bool done = false;
		h.update = (t, dt) => { if (!done && t >= ta) { done = true; shake = Math.Max(shake, s); h.dead = true; } };
		hazards.Add(h);
	}

	// ---------- a lance: one long spike from an edge ----------
	static Hz L12Lance(string e, double c, double w, double len, double tw, double ta, double te)
	{
		var h = new Hz { kind = "lance" }; const double TIP = 70;
		Func<double, double> L = t => t < ta ? 0 : t < ta + 0.07 ? len * (t - ta) / 0.07 : t < te ? len : len * clamp(1 - (t - te) / 0.2, 0, 1);
		h.update = (t, dt) => { if (t > te + 0.22) h.dead = true; };
		void shape(double l, double ww)
		{
			var a = L12pt(e, c - ww / 2, 0); var b = L12pt(e, c - ww / 2, Math.Max(0, l - TIP)); var m = L12pt(e, c, l); var d = L12pt(e, c + ww / 2, Math.Max(0, l - TIP)); var f = L12pt(e, c + ww / 2, 0);
			g.beginPath(); g.moveTo(a.x, a.y); g.lineTo(b.x, b.y); g.lineTo(m.x, m.y); g.lineTo(d.x, d.y); g.lineTo(f.x, f.y); g.closePath(); g.fill();
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; shape(len, w);
				g.fillStyle = L12G + str(0.4 + 0.5 * k) + ")"; shape(16 + 40 * k, w * 0.7); return;
			}
			double l = L(t); if (l <= 0) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = t - ta < 0.08 ? PINKW : PINK; shape(l, w * (1 + 0.1 * pulse)); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; var q = L12uv(e, p.x, p.y); double l = L(t); if (q.v > l + p.r - 3) return false;
			double hw = w / 2 * clamp((l - q.v) / TIP, 0.1, 1); return Math.Abs(q.u - c) < hw + p.r - 3;
		};
		return h;
	}
	static readonly int _k12lance = DodgeKind("lance", e => () =>
	{
		// e, c (place along the edge; aim = the square's place), w, len, hit, dur, wn, off (added to the aimed place)
		string ed = e.str("e", "b"); double c = e.num("c", L12len(ed) / 2);
		if (e.flag("aim") && player != null) c = clamp(L12uv(ed, player.x, player.y).u + e.num("off"), 40, L12len(ed) - 40);
		double hit = e.num("hit"), wn = e.num("wn", 1.5);
		hazards.Add(L12Lance(ed, c, e.num("w", 56), e.num("len", 420), B(hit - wn), B(hit), B(hit + e.num("dur", 0.6))));
	});

	// ---------- a crystal shard: flies (with gravity) from t0, harmless before ----------
	static Hz L12Shard(double x0, double y0, double vx, double vy, double t0, double gr, double r = 8, double life = 2.6)
	{
		var h = new Hz { kind = "shard", r = r }; double rot0 = rnd() * TAU, spin = (rnd() < 0.5 ? -1 : 1) * (5 + rnd() * 4);
		(double x, double y) at(double t) { double s = t - t0; return (x0 + vx * s, y0 + vy * s + 0.5 * gr * s * s); }
		h.update = (t, dt) => { if (t < t0) return; var q = at(t); h.x = q.x; h.y = q.y; if (t - t0 > life || q.x < -60 || q.x > W + 60 || q.y > H + 60 || q.y < -400) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; var q = at(t); double a = clamp((life - (t - t0)) / 0.3, 0, 1); g.save(); g.globalAlpha = a; g.translate(q.x, q.y); g.rotate(rot0 + spin * (t - t0)); double s = r * 1.5 * (1 + 0.12 * pulse);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.moveTo(0, -s * 1.2); g.lineTo(s * 0.55, 0); g.lineTo(0, s * 1.2); g.lineTo(-s * 0.55, 0); g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t - t0 > life - 0.3) return false; var q = at(t); double dx = p.x - q.x, dy = p.y - q.y, rr2 = r + p.r - 2; return dx * dx + dy * dy < rr2 * rr2; };
		return h;
	}

	// ---------- a falling crystal: hangs grey, falls on the beat and shatters on the floor ----------
	static Hz L12Stal(double x, double w, double len, double tw, double tf, double ta)
	{
		var h = new Hz { kind = "stal" };
		Func<double, double> tipY = t => t < tf ? len : len + (H - len) * JS.pow(clamp((t - tf) / (ta - tf), 0, 1), 2);
		h.update = (t, dt) => { if (t > ta + 0.05) { h.dead = true; } };
		void crystal(double ty) { g.beginPath(); g.moveTo(x - w / 2, ty - len); g.lineTo(x + w / 2, ty - len); g.lineTo(x + w * 0.22, ty - len * 0.3); g.lineTo(x, ty); g.lineTo(x - w * 0.22, ty - len * 0.3); g.closePath(); g.fill(); }
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < tf)
			{
				double k = clamp((t - tw) / (tf - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.1 * k)})"; g.fillRect(x - w / 2, 0, w, H);
				g.fillStyle = L12G + str(0.35 + 0.5 * k) + ")"; crystal(len * (0.4 + 0.6 * k)); return;
			}
			g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; crystal(tipY(t)); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || t >= ta) return false; double ty = tipY(t), top = ty - len; if (p.y < top - p.r || p.y > ty + p.r - 2) return false;
			double k = clamp((ty - p.y) / (len * 0.7), 0.15, 1); return Math.Abs(p.x - x) < w / 2 * k + p.r - 3;
		};
		return h;
	}
	static readonly int _k12stal = DodgeKind("stal", e => () =>
	{
		// x (aim = above the square, plus off), w, len, hit = reaches the floor, fall (beats), n shards, sp, wn
		double x = e.num("x", W / 2); if (e.flag("aim") && player != null) x = clamp(player.x + e.num("off"), 40, W - 40);
		double hit = e.num("hit"), fall = e.num("fall", 0.5), wn = e.num("wn", 1.5), ta = B(hit); int n = e.i("n", 6); double sp = e.num("sp", 330);
		hazards.Add(L12Stal(x, e.num("w", 54), e.num("len", 150), B(hit - fall - wn), B(hit - fall), ta));
		double a0 = rnd() * 0.3;
		for (int i = 0; i < n; i++) { double a = Math.PI + 0.35 + (Math.PI - 0.7) * (n == 1 ? 0.5 : (double)i / (n - 1)) + (a0 - 0.15); double v = sp * (0.8 + 0.4 * ((i * 7) % 5) / 4.0); hazards.Add(L12Shard(x, H - 8, JS.cos(a) * v, JS.sin(a) * v, ta, 420, 8, e.num("life", 1.4))); }
		L12Shake(ta, 5);
	});

	// ---------- a spiked gear: rolls along a line, hopping on the beats ----------
	static Hz L12Gear(double x0, double y0, double x1, double y1, double tw, double t0, double t1, double R, double hop, double hb)
	{
		var h = new Hz { kind = "gear" }; double len = hypot(x1 - x0, y1 - y0), nx = len > 0 ? (y1 - y0) / len : 0, ny = len > 0 ? -(x1 - x0) / len : -1;
		if (ny > 0) { nx = -nx; ny = -ny; }   // hops go up
		(double x, double y, double a) at(double t)
		{
			double k = clamp((t - t0) / (t1 - t0), 0, 1); double hp = hop > 0 ? hop * Math.Abs(JS.sin(Math.PI * (BT(t) - hb))) : 0;
			return (x0 + (x1 - x0) * k + nx * hp, y0 + (y1 - y0) * k + ny * hp, len * k / R * (x1 >= x0 ? 1 : -1));
		}
		void gear(double x, double y, double a, double r)
		{
			g.beginPath(); int n = 12;
			for (int i = 0; i < n * 2; i++) { double an = a + i * Math.PI / n, rr2 = i % 2 == 0 ? r * 1.22 : r * 0.9; if (i == 0) g.moveTo(x + JS.cos(an) * rr2, y + JS.sin(an) * rr2); else g.lineTo(x + JS.cos(an) * rr2, y + JS.sin(an) * rr2); }
			g.closePath(); g.fill();
		}
		h.update = (t, dt) => { var q = at(t); h.x = q.x; h.y = q.y; if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0)
			{
				double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = L12G + str(0.15 + 0.3 * k) + ")"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 12 });
				g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = L12G + str(0.3 + 0.5 * k) + ")"; gear(clamp(x0, R, W - R), clamp(y0, R, H - R), 0, R * (0.5 + 0.5 * k)); return;
			}
			var q = at(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; gear(q.x, q.y, q.a, R * (1 + 0.05 * pulse)); g.shadowBlur = 0;
			g.fillStyle = "rgba(0,0,0,0.45)"; g.beginPath(); g.arc(q.x, q.y, R * 0.35, 0, TAU); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; var q = at(t); return hypot(p.x - q.x, p.y - q.y) < R * 1.02 + p.r - 3; };
		return h;
	}
	static readonly int _k12gear = DodgeKind("gear", e => () =>
	{
		// x0 y0 x1 y1, t0 t1 (beats it rolls), R, hop (px), wn
		double t0 = e.num("t0"), wn = e.num("wn", 1.5);
		hazards.Add(L12Gear(e.num("x0"), e.num("y0"), e.num("x1"), e.num("y1"), B(t0 - wn), B(t0), B(e.num("t1")), e.num("R", 46), e.num("hop", 0), e.num("hb", 0)));
	});

	// ---------- a crystal cluster: grows out of a point, holds, and goes back (or bursts into shards) ----------
	static Hz L12Grow(double x, double y, double R, double tw, double t0, double t1, double t2, double t3, int sp0)
	{
		var h = new Hz { kind = "grow", x = x, y = y }; double rot = rnd() * TAU;
		Func<double, double> rad = t => t < t0 ? 0 : t < t1 ? R * (1 - JS.pow(1 - (t - t0) / (t1 - t0), 2)) : t < t2 ? R : t < t3 ? R * (1 - (t - t2) / (t3 - t2)) : 0;
		h.update = (t, dt) => { if (t > t3) h.dead = true; };
		void star(double r)
		{
			int n = sp0; g.beginPath();
			for (int i = 0; i < n * 2; i++) { double an = rot + i * Math.PI / n, rr2 = i % 2 == 0 ? r : r * 0.42; if (i == 0) g.moveTo(x + JS.cos(an) * rr2, y + JS.sin(an) * rr2); else g.lineTo(x + JS.cos(an) * rr2, y + JS.sin(an) * rr2); }
			g.closePath(); g.fill();
		}
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0)
			{
				double k = clamp((t - tw) / (t0 - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.1 + 0.3 * k)})"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, R * 0.72, 0, TAU); g.stroke();
				g.fillStyle = L12G + str(0.4 + 0.5 * k) + ")"; star(10 + 14 * k); return;
			}
			double r = rad(t); if (r <= 1) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = t - t0 < 0.06 ? PINKW : PINK; star(r * (1 + 0.05 * pulse)); g.restore();
		};
		h.hit = (p, t) => { double r = rad(t); return r > 4 && hypot(p.x - x, p.y - y) < r * 0.62 + p.r - 3; };
		return h;
	}
	static readonly int _k12grow = DodgeKind("grow", e => () =>
	{
		// x y (aim: near the square, off), R, t0 = starts growing, g (beats to grow), hold, shrink, n spikes, burst (shards), sp, wn
		double x = e.num("x"), y = e.num("y");
		if (e.flag("aim") && player != null) { x = clamp(player.x + e.num("ox"), 60, W - 60); y = clamp(player.y + e.num("oy"), 60, H - 60); }
		double t0 = e.num("t0"), gb = e.num("g", 1), hold = e.num("hold", 1), sh = e.num("shrink", 0.5), wn = e.num("wn", 1.5); int nb = e.i("burst");
		double T0 = B(t0), T1 = B(t0 + gb), T2 = B(t0 + gb + hold), T3 = B(t0 + gb + hold + (nb > 0 ? 0.05 : sh));
		hazards.Add(L12Grow(x, y, e.num("R", 90), B(t0 - wn), T0, T1, T2, T3, e.i("n", 9)));
		if (nb > 0) { double a0 = rnd() * TAU, sp = e.num("sp", 300); for (int i = 0; i < nb; i++) { double a = a0 + i * TAU / nb; hazards.Add(L12Shard(x, y, JS.cos(a) * sp, JS.sin(a) * sp, T2, 0, 8, e.num("life", 1.0))); } L12Shake(T2, 6); }
	});

	// ---------- the dark: the light closes around the square ----------
	static readonly int _k12dark = DodgeKind("dark", e => () => { gloomT = e.num("v", 0.85); });
	static readonly int _k12flash = DodgeKind("flash", e => () => { flash = Math.Max(flash, e.num("v", 0.3)); shake = Math.Max(shake, e.num("s", 8)); });

	// ---------- the tower around the level (grey, harmless) ----------
	static Hz L12Bg(int ph, double t0, double t1, double rise)
	{
		var h = new Hz { kind = "bg" }; double[] cx = { 180, 470, 810, 1100 };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 1.0, (t1 + 1 - t) / 1.0)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			// rising: the walls run down fast for a moment, as if going up a floor
			double sc = rise > 0 && t > rise ? Math.Min(1, (t - rise) / 0.9) : 0, off = (1 - JS.pow(1 - sc, 3)) * 720 + (t - t0) * 6;
			g.strokeStyle = L12G + str(0.07 + 0.06 * pulse) + ")"; g.lineWidth = 1.5;
			if (ph == 1)
			{
				// bricks on both walls and the floor line
				g.beginPath();
				for (int s = 0; s < 2; s++)
				{
					double x0 = s == 0 ? 0 : W - 70;
					for (double y = -(off % 36) - 36; y < H + 36; y += 36) { g.moveTo(x0, y); g.lineTo(x0 + 70, y); double o = (Math.Floor((y + off) / 36) % 2 + 2) % 2 * 35; g.moveTo(x0 + o + 17, y); g.lineTo(x0 + o + 17, y + 36); }
				}
				g.stroke();
			}
			else if (ph == 2)
			{
				// the mine: wooden posts and beams, and dim crystals in the rock
				g.beginPath();
				foreach (var x in cx) { g.moveTo(x, 0); g.lineTo(x, H); g.moveTo(x - 50, 40); g.lineTo(x + 50, 40); g.moveTo(x - 30, 40); g.lineTo(x, 80); g.moveTo(x + 30, 40); g.lineTo(x, 80); }
				g.stroke();
				g.fillStyle = L12G + str(0.05 + 0.05 * pulse) + ")";
				for (int i = 0; i < 9; i++) { double x = 70 + i * 140, y = (i * 233) % 600 + 60; g.beginPath(); g.moveTo(x, y - 22); g.lineTo(x + 9, y); g.lineTo(x, y + 22); g.lineTo(x - 9, y); g.closePath(); g.fill(); }
			}
			else
			{
				// the summit: stars and the peaks below
				g.fillStyle = L12G + str(0.12 + 0.08 * pulse) + ")";
				for (int i = 0; i < 40; i++) { double x = (i * 317) % W, y = (i * 191) % (H - 200) + 10; g.fillRect(x, y, 2, 2); }
				g.beginPath(); g.moveTo(0, H); double[] pk = { 0, 640, 140, 560, 290, 610, 420, 520, 560, 600, 700, 540, 860, 620, 990, 530, 1130, 600, 1280, 560 };
				for (int i = 0; i < pk.Length; i += 2) g.lineTo(pk[i], pk[i + 1]); g.lineTo(W, H); g.closePath(); g.stroke();
			}
			g.restore();
		};
		return h;
	}
	static readonly int _k12bg = DodgeKind("bg", e => () => hazards.Add(L12Bg(e.i("ph", 1), B(e.num("b")), B(e.num("to")), e.has("rise") ? B(e.num("rise")) : 0)));

	// ---------- the neon hedgehog (phase 3) ----------
	class L12Roll { public double tw, t0, t1; public double[] pts; public double len; }
	static Hz L12HogH;
	static double L12HogT0, L12HogTX;
	static List<L12Roll> L12Rolls = new();
	const double HOGX = W / 2, HOGY = 150, HOGR = 58;
	static (double x, double y, bool ball) L12HogAt(double t)
	{
		foreach (var r in L12Rolls)
		{
			if (t < r.t0 || t >= r.t1) continue; double d = r.len * (t - r.t0) / (r.t1 - r.t0);
			for (int i = 0; i + 3 < r.pts.Length; i += 2)
			{
				double sl = hypot(r.pts[i + 2] - r.pts[i], r.pts[i + 3] - r.pts[i + 1]);
				if (d <= sl || i + 4 >= r.pts.Length) { double k = sl > 0 ? clamp(d / sl, 0, 1) : 0; return (r.pts[i] + (r.pts[i + 2] - r.pts[i]) * k, r.pts[i + 1] + (r.pts[i + 3] - r.pts[i + 1]) * k, true); }
				d -= sl;
			}
		}
		double e = clamp((t - L12HogT0) / (2 * SPB), 0, 1); return (HOGX, -150 + (HOGY + 150) * (1 - JS.pow(1 - e, 3)), false);
	}
	static Hz L12Hog(double t0, double tx)
	{
		L12HogT0 = t0; L12HogTX = tx; L12Rolls = new List<L12Roll>(); bool ex = false;
		var h = new Hz { kind = "hog" };
		h.update = (t, dt) =>
		{
			var q = L12HogAt(t); h.x = q.x; h.y = q.y;
			if (!ex && t >= tx) { ex = true; burst(q.x, q.y, PINK, 90, 600); burst(q.x, q.y, "#fff", 50, 480); shake = 30; flash = 0.8; rumble(650, 1, 1); h.dead = true; }
		};
		h.draw = t =>
		{
			var q = L12HogAt(t); bool live = t >= t0 + 2 * SPB; double ch = clamp((t - (tx - 2 * SPB)) / (2 * SPB), 0, 1), jx = ch > 0 ? (random() * 2 - 1) * 6 * ch : 0, s = 1 + 0.25 * ch;
			// the dotted paths of the rolls coming
			foreach (var r in L12Rolls)
			{
				if (t < r.tw || t >= r.t0) continue; double k = clamp((t - r.tw) / (r.t0 - r.tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 10, 12 });
				g.beginPath(); g.moveTo(r.pts[0], r.pts[1]); for (int i = 2; i + 1 < r.pts.Length; i += 2) g.lineTo(r.pts[i], r.pts[i + 1]); g.stroke(); g.setLineDash(new double[0]);
			}
			g.save(); g.translate(q.x + jx, q.y); g.globalAlpha = live ? 1 : 0.45; g.shadowColor = PINK; g.shadowBlur = ch > 0 ? 40 : 22; g.fillStyle = ch > 0.5 ? PINKW : PINK;
			if (q.ball)
			{
				// curled up: a spiked ball spinning
				double a = (t - L12HogT0) * 9; g.beginPath(); int n = 16;
				for (int i = 0; i < n * 2; i++) { double an = a + i * Math.PI / n, rr2 = i % 2 == 0 ? HOGR * 1.3 : HOGR * 0.92; if (i == 0) g.moveTo(JS.cos(an) * rr2, JS.sin(an) * rr2); else g.lineTo(JS.cos(an) * rr2, JS.sin(an) * rr2); }
				g.closePath(); g.fill();
			}
			else
			{
				// the quills on its back, opening on each beat
				double op = JS.exp(-(((BT(t)) % 1 + 1) % 1) * 3.5); g.beginPath();
				for (int i = 0; i <= 12; i++) { double an = Math.PI + i * Math.PI / 12, l = HOGR * s * (1.35 + 0.35 * op) * (i % 2 == 0 ? 1 : 0.86), w2 = 0.12; g.moveTo(JS.cos(an - w2) * HOGR * 0.8 * s, JS.sin(an - w2) * HOGR * 0.8 * s); g.lineTo(JS.cos(an) * l, JS.sin(an) * l); g.lineTo(JS.cos(an + w2) * HOGR * 0.8 * s, JS.sin(an + w2) * HOGR * 0.8 * s); }
				g.fill();
				g.beginPath(); g.ellipse(0, 6, HOGR * 1.05 * s, HOGR * 0.82 * s, 0, 0, TAU); g.fill();
				g.shadowBlur = 0; g.fillStyle = "#000"; circle(-20 * s, 4, 6); circle(20 * s, 4, 6); g.fillStyle = "rgba(0,0,0,0.5)"; circle(0, 24 * s, 7);
			}
			g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < t0 + 2 * SPB || t >= tx - 2 * SPB) return false; var q = L12HogAt(t);
			return q.ball ? hypot(p.x - q.x, p.y - q.y) < HOGR * 1.08 + p.r - 3 : hypot((p.x - q.x) / (HOGR * 1.05 + p.r - 3), (p.y - q.y - 6) / (HOGR * 0.82 + p.r - 3)) < 1;
		};
		return h;
	}
	static readonly int _k12hog = DodgeKind("hog", e => () => { L12HogH = L12Hog(B(e.num("b")), B(e.num("x"))); hazards.Add(L12HogH); });

	// a quill: a thin needle flying straight from t0
	static Hz L12Quill(double x0, double y0, double ang, double sp, double t0)
	{
		var h = new Hz { kind = "quill", r = 7 }; double c = JS.cos(ang), s = JS.sin(ang);
		h.update = (t, dt) => { if (t < t0) return; h.x = x0 + c * sp * (t - t0); h.y = y0 + s * sp * (t - t0); if (h.x < -60 || h.x > W + 60 || h.y < -60 || h.y > H + 60) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double x = x0 + c * sp * (t - t0), y = y0 + s * sp * (t - t0); g.save(); g.translate(x, y); g.rotate(ang); double k = 1 + 0.15 * pulse;
			g.fillStyle = "rgba(255,42,109,0.25)"; g.beginPath(); g.ellipse(0, 0, 24 * k, 9 * k, 0, 0, TAU); g.fill();
			g.fillStyle = PINK; g.beginPath(); g.moveTo(22 * k, 0); g.lineTo(-16 * k, -5 * k); g.lineTo(-20 * k, 0); g.lineTo(-16 * k, 5 * k); g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double x = x0 + c * sp * (t - t0), y = y0 + s * sp * (t - t0), dx = p.x - x, dy = p.y - y, lx = dx * c + dy * s, ly = -dx * s + dy * c;
			return Math.Abs(lx) < 18 + p.r - 3 && Math.Abs(ly) < 5 + p.r - 3;
		};
		return h;
	}
	// a quill stuck in the ground where the ball rolled: stands for a while
	static Hz L12Stuck(double x, double y, double ang, double t0, double t1)
	{
		var h = new Hz { kind = "stuck", x = x, y = y }; double c = JS.cos(ang), s = JS.sin(ang);
		h.update = (t, dt) => { if (t > t1 + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = t < t1 ? Math.Min(1, (t - t0) / 0.1) : Math.Max(0, 1 - (t - t1) / 0.2); g.save(); g.translate(x, y); g.rotate(ang); g.globalAlpha = k;
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.moveTo(-6, -10); g.lineTo(34, 0); g.lineTo(-6, 10); g.closePath(); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t >= t1) return false; double dx = p.x - x, dy = p.y - y, lx = dx * c + dy * s, ly = -dx * s + dy * c; return lx > -8 - p.r + 3 && lx < 30 + p.r - 3 && Math.Abs(ly) < 6 + p.r - 3; };
		return h;
	}
	static readonly int _k12hogq = DodgeKind("hogq", e => () =>
	{
		// the hedgehog throws quills from where it sits: m = fan | aim | ring | spiral
		// fan: n, a (centre angle, degrees; 90 = down), spr (degrees between), sp. aim: n, spr, sp (at the square). ring: n, sp, rot (degrees)
		// spiral: arms, t0, t1 (beats), every (beats between throws), turn (degrees per beat), sp
		var q = L12HogAt(B(e.b)); double x = q.x, y = q.y + 6, sp = e.num("sp", 300); string m = e.str("m", "fan"); double T = B(e.b);
		void one(double a, double tt) => hazards.Add(L12Quill(x + JS.cos(a) * HOGR, y + JS.sin(a) * HOGR, a, sp, tt));
		if (m == "fan" || m == "aim")
		{
			int n = e.i("n", 7); double spr = e.num("spr", 14) * Math.PI / 180, a0 = e.num("a", 90) * Math.PI / 180;
			if (m == "aim" && player != null) a0 = JS.atan2(player.y - y, player.x - x) + e.num("off") * Math.PI / 180;
			for (int k = 0; k < n; k++) one(a0 + (k - (n - 1) / 2.0) * spr, T);
		}
		else if (m == "ring") { int n = e.i("n", 16); double r0 = e.num("rot") * Math.PI / 180; for (int k = 0; k < n; k++) one(r0 + k * TAU / n, T); }
		else if (m == "spiral")
		{
			int arms = e.i("arms", 3); double b0 = e.num("t0", e.b), b1 = e.num("t1", e.b + 4), ev = e.num("every", 0.25), turn = e.num("turn", 40) * Math.PI / 180, r0 = e.num("rot") * Math.PI / 180;
			for (double b = b0; b < b1 - 1e-6; b += ev) for (int k = 0; k < arms; k++) one(r0 + turn * (b - b0) + k * TAU / arms, B(b));
		}
	});
	static readonly int _k12roll = DodgeKind("roll", e => () =>
	{
		// the hedgehog curls up and rolls along pts [x0,y0,x1,y1,...] from t0 to t1 (beats), shows the path wn beats before; stuck = quills left behind every so many px
		var pts = e.nums("pts"); double len = 0; for (int i = 0; i + 3 < pts.Length; i += 2) len += hypot(pts[i + 2] - pts[i], pts[i + 3] - pts[i + 1]);
		double t0 = e.num("t0"), t1 = e.num("t1"), wn = e.num("wn", 1.5); var r = new L12Roll { tw = B(t0 - wn), t0 = B(t0), t1 = B(t1), pts = pts, len = len }; L12Rolls.Add(r);
		double st = e.num("stuck", 0), stay = e.num("stay", 1.5);
		if (st > 0)
			for (double d = st; d < len - st * 0.5; d += st)
			{
				double tt = r.t0 + (r.t1 - r.t0) * d / len; var q = L12HogAt(tt); double ang = rnd() * TAU;
				hazards.Add(L12Stuck(q.x + JS.cos(ang) * 30, q.y + JS.sin(ang) * 30, ang, tt, tt + stay * SPB));
			}
	});
}
