using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L18 "اعوف الدنيا" (Mohamed Alsalim), the second design (v52, Mohammed 10-28: made again the old way). The strings that hold me to the world:
// part 1 they sound (plucked strings, the loom, the string art that morphs, the twisted cradle), part 2 I cut them (the strings snap and whip,
// the needle sews the world shut round you, the harp spokes tighten until they snap), part 3 only one thread is left: the red thread to you
// (two hearts on their orbits, the thread between them sounds on the song's hits; the heart drawn in strings).
// The first design stays in L18.cs + data/dodge_L18_v51.json for old replays. Every hazard is a pure function of time once it exists.
// Only pink hurts; a pluck, a whip, a stitch and a pattern show their path in grey first. The data is data/dodge_L18.json (tools/level18/gen_b.py).
public static partial class G
{
	static double L18bk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L18bG(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string L18bP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static double L18bEase(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k) * (1 - k); }
	// distance from p to the segment a-b
	static double L18bSeg(double px, double py, double ax, double ay, double bx, double by)
	{
		double dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy; double u = l2 > 0 ? clamp(((px - ax) * dx + (py - ay) * dy) / l2, 0, 1) : 0;
		return hypot(px - ax - dx * u, py - ay - dy * u);
	}

	// ---------- the stages (harmless, under everything) ----------
	public static Hz L18bStage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "L18bStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a; double bt = BT(t);
			if (part == 1)
			{
				g.fillStyle = "rgba(14,8,6,0.78)"; g.fillRect(0, 0, W, H);
				// the oud's wood, dark grain
				g.strokeStyle = "rgba(120,80,50,0.10)"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 14; i++) { double y = 30 + i * 52; g.moveTo(0, y); for (double x = 0; x <= W; x += 64) g.lineTo(x, y + 9 * JS.sin(x * 0.006 + i * 1.7)); }
				g.stroke();
				var gr = g.createRadialGradient(W / 2, H / 2, 40, W / 2, H / 2, 420); gr.addColorStop(0, "rgba(200,140,80,0.10)"); gr.addColorStop(1, "rgba(200,140,80,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			else if (part == 2)
			{
				g.fillStyle = "rgba(10,12,18,0.80)"; g.fillRect(0, 0, W, H);
				// the cloth: a fine weave
				g.strokeStyle = "rgba(160,170,200,0.06)"; g.lineWidth = 1; g.beginPath();
				for (double x = 0; x <= W; x += 16) { g.moveTo(x, 0); g.lineTo(x, H); }
				for (double y = 0; y <= H; y += 16) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
			}
			else
			{
				g.fillStyle = "rgba(6,3,8,0.85)"; g.fillRect(0, 0, W, H);
				double pu = 0.5 + 0.5 * JS.cos(L18Mod(bt, 1) * TAU);
				var gr = g.createRadialGradient(W / 2, H / 2, 30, W / 2, H / 2, 560); gr.addColorStop(0, $"rgba(120,20,50,{str(0.14 + 0.06 * pu)})"); gr.addColorStop(1, "rgba(120,20,50,0)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the strings that sound ----------
	// n strings; geo(i, t) gives string i's two ends. A pluck (string pi[k] at pt[k] s, pa[k] px) makes it sound: it shakes inside a lens
	// pa sin(PI u) wide (u = 0..1 along it) that shrinks to nothing over pd s; the whole lens hurts (it is filled pink). A grey dashed lens
	// warns wn s before. A silent string is a grey line and harmless, unless core > 0 (then it is a pink thread core px thick that always hurts).
	// hub > 0: a pink disc that radius at (hx,hy) (the harp's middle)
	public class L18bStr
	{
		public int n; public Func<int, double, (double ax, double ay, double bx, double by)> geo;
		public double[] pi, pt, pa; public double pd, wn, tw, ta, te, core, hub, hx, hy;
		public Action<double> deco;
	}
	public static Hz L18bStrings(L18bStr o)
	{
		var h = new Hz { kind = "L18bStr" }; int m = o.pi.Length;
		var by = new List<int>[o.n]; for (int i = 0; i < o.n; i++) by[i] = new List<int>();
		for (int k = 0; k < m; k++) { int i = (int)o.pi[k]; if (i >= 0 && i < o.n) by[i].Add(k); }
		double amp(int i, double t)
		{
			double A = 0;
			foreach (int k in by[i]) { double d = t - o.pt[k]; if (d < 0 || d >= o.pd) continue; double q = 1 - d / o.pd; A = Math.Max(A, o.pa[k] * q * Math.Sqrt(q)); }
			return A;
		}
		h.update = (t, dt) => { if (t > o.te + 0.4) h.dead = true; };
		void lens(double ax, double ay, double bx, double by_, double A, double wob)
		{
			double dx = bx - ax, dy = by_ - ay, L = hypot(dx, dy); if (L < 1) return; double nx = -dy / L, ny = dx / L; const int N = 24;
			g.beginPath();
			for (int j = 0; j <= N; j++) { double u = (double)j / N, e = A * JS.sin(Math.PI * u); g.lineTo(ax + dx * u + nx * e, ay + dy * u + ny * e); }
			for (int j = N; j >= 0; j--) { double u = (double)j / N, e = A * JS.sin(Math.PI * u); g.lineTo(ax + dx * u - nx * e, ay + dy * u - ny * e); }
			g.closePath();
			if (wob > -2) { g.fill(); g.stroke(); }
			else g.stroke();
			if (wob > -2)
			{
				// the string itself, shaking inside its lens (light, on the pink)
				g.strokeStyle = "rgba(255,190,210,0.95)"; g.lineWidth = 2; g.beginPath();
				for (int j = 0; j <= N; j++) { double u = (double)j / N, e = A * 0.85 * JS.sin(Math.PI * u) * wob; g.lineTo(ax + dx * u + nx * e, ay + dy * u + ny * e); }
				g.stroke();
			}
		}
		h.draw = t =>
		{
			if (t < o.tw) return; double a = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.3)); if (a <= 0) return;
			g.save(); g.globalAlpha = a; g.lineJoin = "round";
			o.deco?.Invoke(t);
			for (int i = 0; i < o.n; i++)
			{
				var (ax, ay, bx, by_) = o.geo(i, t);
				// the warnings: the lens it will shake in
				foreach (int k in by[i])
				{
					double s = o.pt[k] - t; if (s <= 0 || s > o.wn) continue; double q = 1 - s / o.wn;
					g.strokeStyle = L18bG(0.25 + 0.6 * q); g.lineWidth = 2; g.setLineDash(new double[] { 9, 7 }); lens(ax, ay, bx, by_, o.pa[k], -9); g.setLineDash(new double[0]);
				}
				double A = t >= o.ta && t < o.te ? amp(i, t) : 0;
				if (A > 2)
				{
					g.fillStyle = PINK; g.strokeStyle = PINK; g.lineWidth = 3; lens(ax, ay, bx, by_, A + 2, JS.sin(t * 46 + i));
				}
				else if (o.core > 0 && t >= o.ta && t < o.te) { g.lineCap = "round"; L18Line(ax, ay, bx, by_, PINK, o.core, false); g.lineCap = "butt"; }
				else L18Line(ax, ay, bx, by_, L18bG(0.4), 2, false);
			}
			if (o.hub > 0 && t >= o.ta && t < o.te) { g.fillStyle = PINK; circle(o.hx, o.hy, o.hub); g.fillStyle = "rgba(255,190,210,0.9)"; circle(o.hx, o.hy, o.hub * 0.25); }
			else if (o.hub > 0) { g.strokeStyle = L18bG(0.6); g.lineWidth = 2; g.beginPath(); g.arc(o.hx, o.hy, o.hub, 0, TAU); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < o.ta || t >= o.te) return false;
			if (o.hub > 0 && hypot(p.x - o.hx, p.y - o.hy) < o.hub + p.r - 1) return true;
			for (int i = 0; i < o.n; i++)
			{
				double A = amp(i, t); if (A <= 2 && o.core <= 0) continue;
				var (ax, ay, bx, by_) = o.geo(i, t); double dx = bx - ax, dy = by_ - ay, L = hypot(dx, dy); if (L < 1) continue;
				double u = ((p.x - ax) * dx + (p.y - ay) * dy) / (L * L), d = Math.Abs((p.x - ax) * dy - (p.y - ay) * dx) / L;
				if (A > 2)
				{
					if (u < 0 || u > 1) continue;
					if (d < (A + 2) * JS.sin(Math.PI * u) + 1.5 + p.r - 1) return true;
				}
				else if (L18bSeg(p.x, p.y, ax, ay, bx, by_) < o.core / 2 + p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the string art ----------
	// M pegs on ring A (radius R round cx,cy, turned by spA) and M on ring B (turned by spB). Pattern k from pb[k] s: peg i of A is tied to peg
	// (mul[k] i + off[k]) mod M of B; the threads are pink, w thick. The next pattern shows in grey wn s before it comes; the first one from tw.
	public static Hz L18bArt(int M, double R, double cx, double cy, L15Spin spA, L15Spin spB, double[] pb, double[] mul, double[] off, double w, double wn, double tw, double te)
	{
		var h = new Hz { kind = "L18bArt" };
		int cur(double t) { int c = -1; for (int k = 0; k < pb.Length; k++) if (t >= pb[k]) c = k; return c; }
		(double, double, double, double) chord(int k, int i, double t)
		{
			double bt = BT(t), aA = spA.At(bt) + i * TAU / M; int j = (int)L18Mod(mul[k] * i + off[k], M); double aB = spB.At(bt) + j * TAU / M;
			return (cx + JS.cos(aA) * R, cy + JS.sin(aA) * R, cx + JS.cos(aB) * R, cy + JS.sin(aB) * R);
		}
		bool skip(int k, int i, double t) { var (ax, ay, bx, by) = chord(k, i, t); return hypot(bx - ax, by - ay) < 4; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L18bk(t, tw, tw + 0.4), 1 - L18bk(t, te, te + 0.3)); if (a <= 0) return;
			g.save(); g.globalAlpha = a; int c = cur(t);
			if (c + 1 < pb.Length && pb[c + 1] - t < wn)
			{
				double q = 1 - (pb[c + 1] - t) / wn; g.setLineDash(new double[] { 10, 8 });
				for (int i = 0; i < M; i++) { if (skip(c + 1, i, t)) continue; var (ax, ay, bx, by) = chord(c + 1, i, t); L18Line(ax, ay, bx, by, L18bG(0.2 + 0.55 * q), 2, false); }
				g.setLineDash(new double[0]);
			}
			if (c >= 0 && t < te)
			{
				double pop = 1 + 0.6 * (1 - L18bk(t, pb[c], pb[c] + 0.15));
				for (int i = 0; i < M; i++) { if (skip(c, i, t)) continue; var (ax, ay, bx, by) = chord(c, i, t); L18Line(ax, ay, bx, by, PINK, w * pop, false); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			int c = cur(t); if (c < 0 || t >= te) return false;
			for (int i = 0; i < M; i++)
			{
				if (skip(c, i, t)) continue; var (ax, ay, bx, by) = chord(c, i, t);
				if (L18bSeg(p.x, p.y, ax, ay, bx, by) < w / 2 + p.r - 1) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- a string snaps and whips ----------
	// tied at (ax,ay), L long, taut at angle a0 (grey, harmless) from tw; at tp it snaps: it turns pink and whips da round in dur s, the far end
	// lagging (curl), then lies still for hold s and is gone. A grey sector shows where it will sweep from tw
	public static Hz L18bWhip(double ax, double ay, double L, double a0, double da, double w, double curl, double tw, double tp, double dur, double hold)
	{
		var h = new Hz { kind = "L18bWhip" }; const int N = 14; double tend = tp + dur * (1 + curl) + hold;
		var xs = new double[N + 1]; var ys = new double[N + 1];
		void pts(double t)
		{
			xs[0] = ax; ys[0] = ay;
			for (int j = 1; j <= N; j++) { double s = (j - 0.5) / N, q = L18bEase((t - tp - curl * s * dur) / dur), an = a0 + da * q; xs[j] = xs[j - 1] + JS.cos(an) * L / N; ys[j] = ys[j - 1] + JS.sin(an) * L / N; }
		}
		h.update = (t, dt) => { if (t > tend + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw || t > tend + 0.2) return; g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (t < tp)
			{
				double q = L18bk(t, tw, tp);
				g.strokeStyle = L18bG(0.15 + 0.45 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(ax, ay);
				for (int j = 0; j <= 20; j++) { double an = a0 + da * j / 20.0; g.lineTo(ax + JS.cos(an) * L, ay + JS.sin(an) * L); }
				g.closePath(); g.stroke(); g.setLineDash(new double[0]);
				L18Line(ax, ay, ax + JS.cos(a0) * L, ay + JS.sin(a0) * L, L18bG(0.5 + 0.4 * q), 3, false);
			}
			else
			{
				double fa = t > tend ? 1 - (t - tend) / 0.2 : 1; pts(Math.Min(t, tend)); g.globalAlpha = clamp(fa, 0, 1);
				g.strokeStyle = PINK; g.lineWidth = w; g.beginPath(); g.moveTo(xs[0], ys[0]); for (int j = 1; j <= N; j++) g.lineTo(xs[j], ys[j]); g.stroke();
				g.fillStyle = PINK; circle(ax, ay, w * 0.9);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tp || t >= tend) return false; pts(t);
			for (int j = 1; j <= N; j++) if (L18bSeg(p.x, p.y, xs[j - 1], ys[j - 1], xs[j], ys[j]) < w / 2 + p.r - 1) return true;
			return false;
		};
		return h;
	}

	// ---------- the needle sews ----------
	// along the path (px,py); on every step beat sb[k] (s) it lunges st px on in lunge s. Behind its eye the thread is stitched: so px over the
	// cloth (pink), su px under it (a gap to slip through), counted from the path's start. The needle is nl px long. The path ahead is a faint
	// grey dashed line from tw; all of it is pulled out at te
	public static Hz L18bNeedle(double[] px, double[] py, double[] sb, double st, double lunge, double so, double su, double w, double nl, double tw, double te)
	{
		var h = new Hz { kind = "L18bNeedle" }; int n = px.Length; var cl = new double[n]; for (int j = 1; j < n; j++) cl[j] = cl[j - 1] + hypot(px[j] - px[j - 1], py[j] - py[j - 1]);
		double per = so + su;
		double S(double t) { double s = 0; foreach (var b in sb) { if (t <= b) break; s += st * L18bEase((t - b) / lunge); } return s; }
		(double x, double y, double a) at(double s)
		{
			s = clamp(s, 0, cl[n - 1]); int j = 1; while (j < n - 1 && cl[j] < s) j++;
			double l = cl[j] - cl[j - 1], u = l > 0 ? (s - cl[j - 1]) / l : 0;
			return (px[j - 1] + (px[j] - px[j - 1]) * u, py[j - 1] + (py[j] - py[j - 1]) * u, JS.atan2(py[j] - py[j - 1], px[j] - px[j - 1]));
		}
		void stroke(double s0, double s1)
		{
			if (s1 <= s0) return; var a = at(s0); g.beginPath(); g.moveTo(a.x, a.y);
			for (int j = 1; j < n; j++) if (cl[j] > s0 && cl[j] < s1) g.lineTo(px[j], py[j]);
			var b = at(s1); g.lineTo(b.x, b.y); g.stroke();
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = 1 - L18bk(t, te, te + 0.3); if (a <= 0) return; double s = S(t), tail = s - nl;
			g.save(); g.globalAlpha = a * L18bk(t, tw, tw + 0.4);
			// the way ahead
			g.strokeStyle = L18bG(0.28); g.lineWidth = 2; g.setLineDash(new double[] { 6, 10 }); stroke(Math.Max(0, s), cl[n - 1]); g.setLineDash(new double[0]);
			if (t < te)
			{
				g.strokeStyle = PINK; g.lineWidth = w; g.lineCap = "butt";
				for (double s0 = 0; s0 < tail; s0 += per) stroke(s0, Math.Min(s0 + so, tail));
				// the needle: pink, pointed, a light eye
				var e = at(Math.Max(0, tail)); var tp = at(s); double ang = JS.atan2(tp.y - e.y, tp.x - e.x), L = hypot(tp.x - e.x, tp.y - e.y);
				if (s > 0 && L > 1)
				{
					g.save(); g.translate(e.x, e.y); g.rotate(ang); g.fillStyle = PINK; g.beginPath(); g.moveTo(-4, -5); g.lineTo(L - 16, -5); g.lineTo(L + 6, 0); g.lineTo(L - 16, 5); g.lineTo(-4, 5); g.closePath(); g.fill();
					g.strokeStyle = "rgba(255,200,220,0.95)"; g.lineWidth = 1.5; g.beginPath(); g.ellipse(6, 0, 6, 2, 0, 0, TAU); g.stroke(); g.restore();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tw || t >= te) return false; double s = S(t), tail = s - nl; if (s <= 0) return false;
			var e = at(Math.Max(0, tail)); var tp = at(s);
			if (L18bSeg(p.x, p.y, e.x, e.y, tp.x, tp.y) < 5 + p.r - 1) return true;
			for (int j = 1; j < n; j++)
			{
				if (cl[j - 1] >= tail) break;
				double dx = px[j] - px[j - 1], dy = py[j] - py[j - 1], l = cl[j] - cl[j - 1]; if (l <= 0) continue;
				double u = clamp(((p.x - px[j - 1]) * dx + (p.y - py[j - 1]) * dy) / (l * l), 0, 1), d = hypot(p.x - px[j - 1] - dx * u, p.y - py[j - 1] - dy * u);
				if (d >= w / 2 + p.r - 1) continue;
				double sp = cl[j - 1] + u * l; if (sp > tail + p.r) continue;
				double q = L18Mod(sp, per);
				if (q < so + p.r - 1 || q > per - p.r + 1) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L18.json) ----------
	// times are beats; wn are Normal's warnings (Easy stretches them by ezwn). An event fires before its warning starts
	static double L18bwn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L18bez(double v, double ez) => easy ? v * ez : v;
	// the plucks of a string event, Easy without every third one and a little smaller
	static (double[] pi, double[] pt, double[] pa) L18bPlucks(DodgeEv e)
	{
		var pi = e.nums("pi"); var pb = e.nums("pb"); var pa = e.nums("pa"); var keep = Enumerable.Range(0, pi.Length).Where(k => !easy || k % 3 != 2).ToArray();
		return (keep.Select(k => pi[k]).ToArray(), keep.Select(k => B(pb[k])).ToArray(), keep.Select(k => L18bez(pa[k], 0.75)).ToArray());
	}
	static readonly int _k18bfx = DodgeKind("L18bfx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L18bfx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k18bstage = DodgeKind("L18bstage", e => () => hazards.Add(L18bStage(e.i("part"), B(e.num("fr", e.b)), B(e.num("to")))));
	// parallel strings: offsets v (px, across), centre (cx,cy), half length lh; the frame turns by the spin (a0 degrees, sb/sw)
	// or radial strings (rad): n spokes da degrees apart from r0 to r1 round (cx,cy) (r0 < 0: through the middle), turned by the spin, with a hub
	static readonly int _k18bstr = DodgeKind("L18bstr", e => { double wn = L18bwn(e); return () =>
	{
		var (pi, pt, pa) = L18bPlucks(e); double cx = e.num("cx", W / 2), cy = e.num("cy", H / 2), tw = e.num("tw"), to = e.num("to");
		var sp = new L15Spin(e.num("a0") * Math.PI / 180, tw, to + 4, e.nums("sb"), e.nums("sw").Select(v => v * (easy ? 0.85 : 1)).ToArray());
		var o = new L18bStr { pi = pi, pt = pt, pa = pa, pd = e.num("pd") * SPB * (easy ? 0.85 : 1), wn = wn * SPB, tw = B(tw), ta = B(e.num("ta", tw)), te = B(to) };
		if (e.str("mode") == "rad")
		{
			int n = e.i("n"); double r0 = e.num("r0"), r1 = e.num("r1"), da = e.num("da", 360.0 / n) * Math.PI / 180; o.n = n; o.hub = L18bez(e.num("hub"), 0.85); o.hx = cx; o.hy = cy;
			o.geo = (i, t) => { double an = sp.At(BT(t)) + i * da, c = JS.cos(an), s = JS.sin(an); return (cx + c * r0, cy + s * r0, cx + c * r1, cy + s * r1); };
		}
		else
		{
			var v = e.nums("v"); double lh = e.num("lh"); o.n = v.Length;
			o.geo = (i, t) => { double an = sp.At(BT(t)), c = JS.cos(an), s = JS.sin(an), mx = cx - s * v[i], my = cy + c * v[i]; return (mx - c * lh, my - s * lh, mx + c * lh, my + s * lh); };
		}
		hazards.Add(L18bStrings(o));
	}; });
	// the red thread: pairs of hearts, heart 2k and 2k+1 tied by thread k. Heart j goes round (cx,cy) on an ellipse rx[j] x ry[j], at angle
	// a0[j] + its spin (sw[j] radians a beat, changing at sb). The thread is pink (core px) and sounds when plucked; the hearts hurt too
	static readonly int _k18bthread = DodgeKind("L18bthread", e => { double wn = L18bwn(e); return () =>
	{
		var (pi, pt, pa) = L18bPlucks(e); double tw = e.num("tw"), to = e.num("to"), hr = e.num("hr");
		var rx = e.nums("rx"); var ry = e.nums("ry"); var a0 = e.nums("ha0"); var sw = e.nums("hsw"); var sb = e.nums("sb"); int nh = rx.Length;
		var sp = new L15Spin[nh];
		for (int j = 0; j < nh; j++) { var w = new double[sb.Length]; for (int q = 0; q < sb.Length; q++) w[q] = sw[q * nh + j] * (easy ? 0.85 : 1); sp[j] = new L15Spin(a0[j] * Math.PI / 180, tw, to + 4, sb, w); }
		var on = e.nums("on"); var off = e.nums("off");
		(double, double) hp(int j, double t) { double an = sp[j].At(BT(t)); return (W / 2 + JS.cos(an) * rx[j], H / 2 + JS.sin(an) * ry[j]); }
		bool live(int k, double t) => t >= B(on[k]) && t < B(off[k]);
		var o = new L18bStr { n = nh / 2, pi = pi, pt = pt, pa = pa, pd = e.num("pd") * SPB * (easy ? 0.85 : 1), wn = wn * SPB, tw = B(tw), ta = B(e.num("ta", tw)), te = B(to), core = L18bez(e.num("core"), 0.8) };
		// a thread that is not on yet (or gone) lies off the screen
		o.geo = (i, t) => { if (!live(i, t)) return (-900, -900, -899, -900); var (ax, ay) = hp(2 * i, t); var (bx, by) = hp(2 * i + 1, t); return (ax, ay, bx, by); };
		var h = L18bStrings(o); var draw = h.draw; var hit = h.hit; double hR = L18bez(hr, 0.85);
		void heart(double x, double y, double r)
		{
			g.beginPath(); g.moveTo(x, y + r);
			for (int q = 1; q <= 24; q++) { double u = q / 24.0 * TAU, su3 = JS.sin(u), hx = 16 * su3 * su3 * su3, hy = -(13 * JS.cos(u) - 5 * JS.cos(2 * u) - 2 * JS.cos(3 * u) - JS.cos(4 * u)); g.lineTo(x + hx * r / 16, y + hy * r / 16 + r * 0.1); }
			g.closePath(); g.fill();
		}
		h.draw = t =>
		{
			draw(t); if (t < o.tw || t >= o.te + 0.3) return;
			g.save(); g.globalAlpha = Math.Min(L18bk(t, o.tw, o.tw + 0.5), 1 - L18bk(t, o.te, o.te + 0.3));
			double beat = 1 + 0.15 * JS.exp(-L18Mod(BT(t), 1) * 6);
			for (int j = 0; j < nh; j++)
			{
				if (!live(j / 2, t) && !(t < B(on[j / 2]) && t > B(on[j / 2]) - o.wn * 2)) continue; var (x, y) = hp(j, t);
				g.fillStyle = live(j / 2, t) && t >= o.ta ? PINK : L18bG(0.5); heart(x, y, hR * beat);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < o.ta || t >= o.te) return false;
			for (int j = 0; j < nh; j++) { if (!live(j / 2, t)) continue; var (x, y) = hp(j, t); if (hypot(p.x - x, p.y - y) < hR * 0.85 + p.r - 1) return true; }
			return hit(p, t);
		};
		hazards.Add(h);
	}; });
	// the string art: M pegs on a ring R round (cx,cy), turned (a0 + sb/sw); ring B turns with spin sbB/swB (or with ring A when there is none)
	static readonly int _k18bart = DodgeKind("L18bart", e => { double wn = L18bwn(e); return () =>
	{
		double tw = e.num("tw"), to = e.num("to"), ez = easy ? 0.85 : 1;
		var spA = new L15Spin(e.num("a0") * Math.PI / 180, tw, to + 4, e.nums("sb"), e.nums("sw").Select(v => v * ez).ToArray());
		var spB = e.has("swB") ? new L15Spin(e.num("a0B") * Math.PI / 180, tw, to + 4, e.nums("sbB"), e.nums("swB").Select(v => v * ez).ToArray()) : spA;
		int M = easy ? e.i("ezM", e.i("M")) : e.i("M");
		// Easy ties the threads again half as often
		double[] pb = e.nums("pb"), mul = e.nums("mul"), off = e.nums("off"); var ks = Enumerable.Range(0, pb.Length).Where(k => !easy || k % 2 == 0).ToArray();
		hazards.Add(L18bArt(M, e.num("R"), e.num("cx", W / 2), e.num("cy", H / 2), spA, spB, L18Sec(ks.Select(k => pb[k]).ToArray()), ks.Select(k => mul[k]).ToArray(), ks.Select(k => off[k]).ToArray(), L18bez(e.num("w"), 0.65), wn * SPB, B(tw), B(to)));
	}; });
	static readonly int _k18bwhip = DodgeKind("L18bwhip", e => { double wn = L18bwn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return; double hit = e.num("hit");
		hazards.Add(L18bWhip(e.num("x"), e.num("y"), L18bez(e.num("L"), 0.9), e.num("a0") * Math.PI / 180, e.num("da") * Math.PI / 180, L18bez(e.num("w"), 0.8), e.num("curl"),
			B(hit - wn), B(hit), e.num("dur") * SPB * (easy ? 1.3 : 1), e.num("hold") * SPB));
	}; });
	static readonly int _k18bneedle = DodgeKind("L18bneedle", e => () =>
	{
		var sb = e.nums("sb"); if (easy) sb = sb.Where((v, i) => i % 3 != 2).ToArray();
		hazards.Add(L18bNeedle(e.nums("px"), e.nums("py"), L18Sec(sb), e.num("st"), e.num("lunge") * SPB, e.num("so"), L18bez(e.num("su"), 1.3), L18bez(e.num("w"), 0.8), e.num("nl"), B(e.num("tw")), B(e.num("to"))));
	});
}
