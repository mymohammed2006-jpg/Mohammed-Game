using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 5844-6495: the shapes of level 11 and buildLevel11 (I Will Survive)
public static partial class G
{
	static double L11sq(double x) => x * x;   // JS x**2 / Math.pow(x,2) (fdlibm returns x*x for y=2)
	static double L11ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double L11wk(double t, double tw, double ta) => clamp((t - tw) / Math.Max(1e-6, ta - tw), 0, 1);
	static void L11dash(bool on, double a = 12, double b = 9) { g.setLineDash(on ? new[] { a, b } : new double[0]); }
	const double L11G = H - 40;               // the beach ground in phase 1
	static readonly double[] L11SL = { 128, H - 176 };       // where the bird sits on the slingshot
	// "petrified": times when phase 1's attacks stop where they are
	static List<double[]> L11FZ = new();
	static double L11ft(double t) { double s = t; foreach (var q in L11FZ) { double a = q[0], b = q[1]; if (t >= b) s -= b - a; else if (t > a) s -= t - a; } return s; }
	static double L11frozen(double t) { foreach (var q in L11FZ) { double a = q[0], b = q[1]; if (t > a && t < b) return Math.Min(1, Math.Min((t - a) / 0.08, (b - t) / 0.12)); } return 0; }

	/* ---- the path: points, and the place at any distance s ---- */
	public class L11P
	{
		public List<double[]> pts; public List<double> L; public double len;
		public (double x, double y, double a) at(double s)
		{
			int n = pts.Count; if (s <= 0) { var a = pts[0]; var b = pts[1]; return (a[0], a[1], JS.atan2(b[1] - a[1], b[0] - a[0])); }
			if (s >= len) { var a = pts[n - 2]; var b = pts[n - 1]; double an = JS.atan2(b[1] - a[1], b[0] - a[0]); return (b[0] + JS.cos(an) * (s - len), b[1] + JS.sin(an) * (s - len), an); }
			int lo = 0, hi = n - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (L[m] <= s) lo = m; else hi = m; }
			var A = pts[lo]; var Bp = pts[hi]; double dd = L[hi] - L[lo]; if (dd == 0 || double.IsNaN(dd)) dd = 1; double k = (s - L[lo]) / dd;
			return (A[0] + (Bp[0] - A[0]) * k, A[1] + (Bp[1] - A[1]) * k, JS.atan2(Bp[1] - A[1], Bp[0] - A[0]));
		}
		public double near(double x, double y) { int bi = 0; double bd = 1e18; for (int i = 0; i < pts.Count; i++) { double d = L11sq(pts[i][0] - x) + L11sq(pts[i][1] - y); if (d < bd) { bd = d; bi = i; } } return L[bi]; }
		public double near(double[] q) => near(q[0], q[1]);
	}
	static L11P L11Path(List<double[]> pts)
	{
		var L = new List<double> { 0 }; for (int i = 1; i < pts.Count; i++) L.Add(L[i - 1] + hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1])); double len = L[L.Count - 1];
		return new L11P { pts = pts, L = L, len = len };
	}
	// path builder: lines, arcs and functions
	public class L11S
	{
		public List<double[]> P = new();
		double[] last() => P[P.Count - 1];
		public L11S line(double x1, double y1) { var q = last(); double x0 = q[0], y0 = q[1]; int n = (int)Math.Max(1, Math.Ceiling(hypot(x1 - x0, y1 - y0) / 9)); for (int i = 1; i <= n; i++) P.Add(new[] { x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n }); return this; }
		public L11S arc(double cx, double cy, double r, double a0, double a1) { int n = (int)Math.Max(2, Math.Ceiling(Math.Abs(a1 - a0) * r / 9)); for (int i = 1; i <= n; i++) { double a = a0 + (a1 - a0) * i / n; P.Add(new[] { cx + JS.cos(a) * r, cy + JS.sin(a) * r }); } return this; }
		public L11S fn(Func<double, double[]> f, int n) { for (int i = 1; i <= n; i++) P.Add(f((double)i / n)); return this; }
		public L11S add(IEnumerable<double[]> pts) { foreach (var q in pts) P.Add(q); return this; }
		public L11P done() => L11Path(P);
	}
	static L11S L11seg(double x, double y) { var s = new L11S(); s.P.Add(new[] { x, y }); return s; }
	// a toss: an arc through the target (tx,ty), its top `up` above the higher one, going on until it leaves the screen
	static List<double[]> L11toss(double x0, double y0, double tx, double ty, double up, double gv = 900)
	{
		double ya = Math.Min(y0, ty) - up, vy = -Math.Sqrt(2 * gv * (y0 - ya)), dy = ty - y0, tau = (-vy + Math.Sqrt(Math.Max(0, vy * vy + 2 * gv * dy))) / gv, vx = (tx - x0) / tau; var P = new List<double[]>();
		for (double t = 0; t < 14; t += 0.01) { double x = x0 + vx * t, y = y0 + vy * t + 0.5 * gv * t * t; P.Add(new[] { x, y }); if (x < -140 || x > W + 140 || y > H + 140) break; }
		return P;
	}
	// heart shape
	static double[] L11heart(double a) => new[] { 16 * JS.pow(JS.sin(a), 3) / 17, -(13 * JS.cos(a) - 5 * JS.cos(2 * a) - 2 * JS.cos(3 * a) - JS.cos(4 * a)) / 17 };
	static L11P L11heartPath(double cx, double cy, double S, double a0 = Math.PI, double dir = 1) => L11seg(cx + L11heart(a0)[0] * S, cy + L11heart(a0)[1] * S).fn(u => { var h = L11heart(a0 + dir * TAU * u); return new[] { cx + h[0] * S, cy + h[1] * S }; }, 240).done();
	/* ---- drawings ---- */
	static void L11heartShape(double x, double y, double s) { g.beginPath(); for (int i = 0; i <= 24; i++) { var h = L11heart(TAU * i / 24); if (i != 0) g.lineTo(x + h[0] * s, y + h[1] * s); else g.moveTo(x + h[0] * s, y + h[1] * s); } g.closePath(); }
	// a bird (Angry Birds but pink)
	static void L11bird(double x, double y, double an, double r, string kind, double t)
	{
		g.save(); g.translate(x, y); if (kind == "yellow") g.rotate(an); else if (JS.cos(an) < 0) g.scale(-1, 1);
		g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
		if (kind == "yellow") { g.beginPath(); g.moveTo(r * 1.25, 0); g.lineTo(-r * 0.85, -r * 1.05); g.lineTo(-r * 0.85, r * 1.05); g.closePath(); g.fill(); }
		else if (kind == "white") { g.beginPath(); g.ellipse(0, 0, r * 0.95, r * 1.15, 0, 0, TAU); g.fill(); }
		else circle(0, 0, r);
		if (kind == "red" || kind == "big") { g.beginPath(); g.moveTo(-r * 0.2, -r * 0.9); g.lineTo(-r * 0.05, -r * 1.45); g.lineTo(r * 0.15, -r * 0.92); g.lineTo(r * 0.3, -r * 1.3); g.lineTo(r * 0.38, -r * 0.85); g.closePath(); g.fill(); }
		if (kind == "black") { g.strokeStyle = GREY + "0.9)"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, -r); g.quadraticCurveTo(r * 0.3, -r * 1.5, r * 0.1, -r * 1.7); g.stroke(); g.fillStyle = (Math.Floor(t * 12) % 2 != 0) ? "#fff" : "#ffd23a"; circle(r * 0.1, -r * 1.72, 4); }
		g.shadowBlur = 0;
		double ex = kind == "yellow" ? r * 0.1 : r * 0.22;
		g.fillStyle = "rgba(255,255,255,0.32)"; g.beginPath(); g.ellipse(-r * 0.05, r * 0.5, r * 0.55, r * 0.32, 0, 0, TAU); g.fill();
		g.fillStyle = "#fff"; circle(ex, -r * 0.18, r * 0.22); circle(ex + r * 0.32, -r * 0.15, r * 0.18); g.fillStyle = "#000"; circle(ex + r * 0.06, -r * 0.16, r * 0.09); circle(ex + r * 0.36, -r * 0.13, r * 0.08);
		g.strokeStyle = "#000"; g.lineWidth = Math.Max(2, r * (kind == "big" ? 0.18 : 0.12)); g.beginPath(); g.moveTo(ex - r * 0.2, -r * 0.5); g.lineTo(ex + r * 0.18, -r * 0.33); g.lineTo(ex + r * 0.5, -r * 0.45); g.stroke();
		g.fillStyle = "#ffd1de"; g.beginPath(); g.moveTo(ex + r * 0.25, r * 0.02); g.lineTo(ex + r * 0.85, r * 0.15); g.lineTo(ex + r * 0.25, r * 0.32); g.closePath(); g.fill();
		g.fillStyle = "rgba(40,0,15,0.85)"; if (kind != "yellow") { g.fillRect(-r * 1.25, -r * 0.12, r * 0.32, r * 0.1); g.fillRect(-r * 1.2, r * 0.05, r * 0.28, r * 0.1); }
		g.restore();
	}
	static void L11keyShape(double x, double y, double a, double s)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.arc(-s * 0.9, 0, s * 0.55, 0, TAU); g.arc(-s * 0.9, 0, s * 0.25, 0, TAU, true); g.fill("evenodd");
		g.fillRect(-s * 0.4, -s * 0.13, s * 1.5, s * 0.26); g.fillRect(s * 0.65, s * 0.1, s * 0.18, s * 0.35); g.fillRect(s * 0.95, s * 0.1, s * 0.15, s * 0.25); g.shadowBlur = 0; g.restore();
	}
	// a roller coaster car (grey people inside with raised hands)
	static void L11car(double x, double y, double a, bool front, double wht)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.shadowColor = wht > 0.5 ? "#fff" : PINK; g.shadowBlur = 14; g.fillStyle = wht > 0 ? $"rgb(255,{str(42 + 213 * wht)},{str(109 + 146 * wht)})" : PINK;
		g.beginPath(); g.roundRect(-24, -15, 48, 30, 8); g.fill(); if (front) { g.beginPath(); g.moveTo(24, -15); g.lineTo(34, 0); g.lineTo(24, 15); g.fill(); }
		g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.35)"; g.fillRect(-16, -9, 32, 18); g.fillStyle = GREY + "0.85)"; circle(-7, -1, 5); circle(7, 1, 5); g.strokeStyle = GREY + "0.85)"; g.lineWidth = 2.5; g.beginPath(); g.moveTo(-7, -4); g.lineTo(-11, -15); g.moveTo(7, -2); g.lineTo(11, -14); g.stroke();
		if (front) { g.fillStyle = "#fff"; circle(28, 0, 3.5); }
		g.restore();
	}
	// a heart boat (tunnel of love)
	static void L11boat(double x, double y, double a, double r) { g.save(); g.translate(x, y); g.rotate(a + Math.PI / 2); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; L11heartShape(0, 0, r * 1.1); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.35)"; circle(-r * 0.35, -r * 0.3, r * 0.2); g.restore(); }
	static void L11sprite(string kind, double x, double y, double an, double r, double t, int c, L11RO o)
	{
		if (kind == "comet") { g.save(); g.translate(x, y); g.rotate(an); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.beginPath(); g.moveTo(0, -r); g.quadraticCurveTo(-r * 3, -r * 0.7, -r * 6, 0); g.quadraticCurveTo(-r * 3, r * 0.7, 0, r); g.arc(0, 0, r, Math.PI / 2, -Math.PI / 2, true); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.4)"; circle(r * 0.2, -r * 0.2, r * 0.35); g.restore(); return; }
		if (kind == "key") { L11keyShape(x, y, t * 11 + c, r); return; }
		if (kind == "car") { if (o.big) { g.save(); g.translate(x, y); g.rotate(an); g.scale(1.4, 1.55); L11car(0, 0, 0, c == 0, 0); g.restore(); return; } L11car(x, y, an, c == 0, o.wht != null ? o.wht(t) : 0); return; }
		if (kind == "heart") { L11boat(x, y, an, r); return; }
		if (kind == "link") { g.save(); g.translate(x, y); g.rotate(an); g.strokeStyle = PINK; g.lineWidth = 7; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.ellipse(0, 0, r * 1.3, r * 0.7, 0, 0, TAU); g.stroke(); g.restore(); return; }
		L11bird(x, y, an, r, kind, t);
	}
	/* ---- anything moving on a path: before t0 its way shows (dots like the game, or a track), then it moves at speed v ----
	   o: tw t0 v r kind pv('dots'|'track'|'water'|'none') n sp end dashAt v2 frz onEnd gaps */
	public class L11RO
	{
		public double tw, t0, v, v2; public int n = 1; public double sp = 52, r = 20; public string kind = "red", pv = "dots";
		public bool frz, hide, big; public double? end, dashAt; public Action<double> onEnd; public List<double[]> gaps; public Func<double, double> wht;
		public L11RO Clone() => (L11RO)MemberwiseClone();
	}
	static Hz L11Ride(L11P P, L11RO o)
	{
		double tw = o.tw, t0 = o.t0, v = o.v; int n = o.n; double sp = o.sp, r = o.r; string kind = o.kind, pv = o.pv; Func<double, double> TT = o.frz ? L11ft : (t => t); double tail = (n - 1) * sp;
		double end = o.end ?? P.len + 300;
		double vis = P.len; { bool on = false; for (int i = 0; i < P.pts.Count; i++) { double x = P.pts[i][0], y = P.pts[i][1]; bool inS = x > -30 && x < W + 30 && y > -30 && y < H + 30; if (inS) on = true; else if (on) { vis = P.L[i]; break; } } }
		vis = Math.Min(vis, end);
		Func<double, double> S = t => { double e = TT(t) - TT(t0); if (o.dashAt == null || e * v < o.dashAt) return e * v; return o.dashAt.Value + (e - o.dashAt.Value / v) * o.v2; };
		Func<double, bool> inGap = s => { if (o.gaps == null) return false; foreach (var q in o.gaps) if (s > q[0] && s < q[1]) return true; return false; };
		List<double[]> RA = null, RB = null, TIE = null;
		if (pv == "track")
		{
			RA = new(); RB = new(); TIE = new(); var p = P.pts;
			for (int i = 0; i < p.Count; i++) { var a = p[Math.Max(0, i - 1)]; var b = p[Math.Min(p.Count - 1, i + 1)]; double an = JS.atan2(b[1] - a[1], b[0] - a[0]), nx = -JS.sin(an) * 9, ny = JS.cos(an) * 9; RA.Add(new[] { p[i][0] + nx, p[i][1] + ny }); RB.Add(new[] { p[i][0] - nx, p[i][1] - ny }); }
			for (double s = 10; s < vis; s += 26) if (!inGap(s)) { var q = P.at(s); TIE.Add(new[] { s, q.x, q.y, q.a }); }
		}
		bool fired = false;
		double from0 = 0;
		void rail(List<double[]> Rl, double lim) { g.beginPath(); bool pen = false; for (int i = 0; i < Rl.Count && P.L[i] <= lim; i++) { if (P.L[i] < from0 || inGap(P.L[i])) { pen = false; continue; } if (pen) g.lineTo(Rl[i][0], Rl[i][1]); else g.moveTo(Rl[i][0], Rl[i][1]); pen = true; } g.stroke(); }
		var h = new Hz();
		h.update = (t, dt) => { if (t < t0) return; double s = S(t); if (o.onEnd != null && !fired && s >= end) { fired = true; o.onEnd(t); } if (s - tail > Math.Max(end, vis) + 60 && t > t0 + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double k = L11wk(t, tw, t0), sh = t < t0 ? -1 : S(t);
			if (pv == "dots" && t >= tw)
			{
				double lim = t < t0 ? vis * Math.Min(1, 0.12 + k * 1.4) : vis, from = t < t0 ? 0 : sh + r + 8, a = t < t0 ? 0.25 + 0.55 * k : Math.Max(0, 0.8 - (t - t0) * 0.9);
				if (a > 0.02)
				{
					g.fillStyle = $"rgba(255,255,255,{str(a)})"; for (double s = Math.Ceiling(from / 26) * 26; s < lim; s += 26) { if (o.dashAt != null && s >= o.dashAt) break; var q = P.at(s); circle(q.x, q.y, 3.6); }
					if (o.dashAt != null && lim > o.dashAt) { g.fillStyle = $"rgba(255,42,109,{str(a)})"; for (double s = Math.Max(o.dashAt.Value, Math.Ceiling(from / 12) * 12); s < lim; s += 12) { var q = P.at(s); g.save(); g.translate(q.x, q.y); g.rotate(q.a); g.fillRect(-4, -2.5, 8, 5); g.restore(); } }
				}
			}
			if (pv == "water" && t >= tw)
			{
				double lim = t < t0 ? vis * Math.Min(1, 0.15 + k * 1.4) : vis, a = t < t0 ? 0.08 + 0.12 * k : 0.2; g.strokeStyle = $"rgba(150,200,255,{str(a)})"; g.lineWidth = 46; g.lineCap = "round"; g.beginPath(); for (double s = 0; s <= lim; s += 12) { var q = P.at(s); if (s != 0) g.lineTo(q.x, q.y); else g.moveTo(q.x, q.y); } g.stroke(); g.lineCap = "butt";
				g.strokeStyle = $"rgba(255,255,255,{str(a * 1.5)})"; g.lineWidth = 2; L11dash(true, 8, 14); g.beginPath(); for (double s = 0; s <= lim; s += 12) { var q = P.at(s); if (s != 0) g.lineTo(q.x, q.y); else g.moveTo(q.x, q.y); } g.stroke(); L11dash(false);
			}
			if (pv == "track" && t >= tw)
			{
				double lim = t < t0 ? vis * Math.Min(1, 0.1 + k * 1.5) : vis; double a = t < t0 ? 0.25 + 0.5 * k : 0.75; from0 = t < t0 ? 0 : Math.Max(0, sh - tail - 40);
				if (a > 0.02)
				{
					g.strokeStyle = $"rgba(255,42,109,{str(a * 0.35)})"; g.lineWidth = 5; foreach (var q in TIE) { double s = q[0], x = q[1], y = q[2], an = q[3]; if (s > lim) break; if (s < from0) continue; double c = JS.cos(an + Math.PI / 2) * 13, d = JS.sin(an + Math.PI / 2) * 13; g.beginPath(); g.moveTo(x - c, y - d); g.lineTo(x + c, y + d); g.stroke(); }
					g.strokeStyle = $"rgba(225,225,240,{str(a)})"; g.lineWidth = 3; rail(RA, lim); rail(RB, lim);
					if (o.gaps != null) { g.fillStyle = $"rgba(255,255,255,{str(a)})"; foreach (var q in o.gaps) { double a0 = q[0], b0 = q[1]; for (double s = Math.Max(a0, from0); s < Math.Min(b0, lim); s += 24) { var pq = P.at(s); circle(pq.x, pq.y, 3.5); } } }
				}
			}
			if (t < t0 || o.hide) return;
			for (int c = 0; c < n; c++) { double s = sh - c * sp; if (s < 0 || s > end) continue; var q = P.at(s); L11sprite(kind, q.x, q.y, q.a, r, t, c, o); }
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double sh = S(t);
			for (int c = 0; c < n; c++)
			{
				double s = sh - c * sp; if (s < 0 || s > end) continue; var (x, y, an) = P.at(s);
				if (kind == "car") { if (obbHit(p.x, p.y, p.r - 3, x, y, (c == 0 ? 27 : 23) * (o.big ? 1.4 : 1), 14 * (o.big ? 1.55 : 1), an)) return true; }
				else if (kind == "comet") { foreach (var (q, rq) in new[] { (0.0, 0.95), (-1.4, 0.62), (-2.8, 0.4) }) if (hypot(p.x - x - JS.cos(an) * r * q, p.y - y - JS.sin(an) * r * q) < r * rq + p.r - 3) return true; }
				else if (hypot(p.x - x, p.y - y) < r * (kind == "yellow" ? 0.8 : kind == "key" ? 0.9 : 0.95) + p.r - 3) return true;
			}
			return false;
		};
		return h;
	}
	// a growing ring (explosion, bell sound): an optional opening at angle ga, width gw
	public class L11RgO { public bool lin; public double? tw, pr; }
	static Hz L11Ring(double x, double y, double t0, double t1, double r0, double r1, double w, double ga, double gw, L11RgO o = null)
	{
		o ??= new L11RgO();
		Func<double, double> R = t => r0 + (r1 - r0) * (o.lin ? clamp((t - t0) / (t1 - t0), 0, 1) : 1 - L11sq(1 - clamp((t - t0) / (t1 - t0), 0, 1)));
		Func<double, bool> inGap = a => { if (gw <= 0) return false; double d = Math.Abs(((a - ga) % TAU + TAU * 1.5) % TAU - Math.PI); return d < gw / 2; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) { if (o.tw != null && t >= o.tw) { double k = L11wk(t, o.tw.Value, t0); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); g.arc(x, y, (o.pr ?? 0) != 0 ? o.pr.Value : r1 * 0.5, 0, TAU); g.stroke(); L11dash(false); } return; }
			double rr = R(t), al = Math.Min(1, (t1 - t) / 0.25); g.save(); g.globalAlpha = al; g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.lineWidth = w; g.beginPath();
			if (gw > 0) g.arc(x, y, rr, ga + gw / 2, ga - gw / 2 + TAU); else g.arc(x, y, rr, 0, TAU); g.stroke(); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.4)"; g.lineWidth = 2; g.stroke(); g.restore();
		};
		h.hit = (p, t) => { if (t < t0 + 0.02 || t > t1 - 0.2) return false; double d = hypot(p.x - x, p.y - y); if (Math.Abs(d - R(t)) > w / 2 + p.r - 3) return false; return !inGap(JS.atan2(p.y - y, p.x - x)); };
		return h;
	}
	/* ---- phase 1: the island at night (side view) ---- */
	static readonly List<double[]> L11FORT = ((Func<List<double[]>>)(() =>
	{
		var Bl = new List<double[]>(); const double x0 = 1000; void add(double x, double y, double w, double h, double m) => Bl.Add(new[] { x, y, w, h, m });
		add(x0, L11G - 120, 26, 120, 0); add(x0 + 110, L11G - 120, 26, 120, 0); add(x0 + 220, L11G - 120, 26, 120, 0); add(x0 - 10, L11G - 146, 272, 26, 1);
		add(x0 + 30, L11G - 266, 24, 120, 2); add(x0 + 190, L11G - 266, 24, 120, 2); add(x0 + 20, L11G - 290, 214, 24, 1); add(x0 + 95, L11G - 380, 64, 90, 0); add(x0 + 70, L11G - 404, 114, 24, 2); return Bl;
	}))();
	static readonly (double x, double y, double r, int[] nd)[] L11PIGS = { (1068, L11G - 22, 24, new int[0]), (1176, L11G - 22, 24, new int[0]), (1127, L11G - 170, 30, new[] { 3 }), (1127, L11G - 430, 26, new[] { 7, 8 }) };
	// the background: the beach at night, then space
	static Hz L11Night(double t0, double t1, double[] gone)
	{
		var R = mulberry32(1111); var ST = new List<double[]>(); for (int i = 0; i < 70; i++) ST.Add(new[] { R() * W, R() * (H - 260), 0.5 + R() * 1.6, R() * TAU });
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; g.globalAlpha = a; double b = BT(t);
			double sp = clamp((b - 44) / 2, 0, 1) * (1 - clamp((b - 70) / 3, 0, 1)), dawn = clamp((b - 72) / 4, 0, 1);
			g.fillStyle = $"rgba(10,14,40,{str(0.55 - 0.35 * dawn)})"; g.fillRect(0, 0, W, H); if (dawn > 0) { g.fillStyle = $"rgba(255,180,120,{str(0.12 * dawn)})"; g.fillRect(0, 0, W, H); }
			foreach (var q in ST) { double x = q[0], y = q[1], s = q[2], ph = q[3]; g.fillStyle = $"rgba(255,255,255,{str((0.15 + 0.25 * sp) * (0.6 + 0.4 * JS.sin(t * 2 + ph)) * (1 - dawn))})"; g.fillRect(x, y, s * (1 + sp), s * (1 + sp)); }
			g.fillStyle = GREY + str(0.16 * (1 - sp)) + ")"; circle(1040, 96, 40); g.fillStyle = $"rgba(10,14,40,{str(0.9 * (1 - sp))})"; circle(1058, 86, 36);
			// the sea behind, and the sand
			g.globalAlpha = a * (1 - sp * 0.85); g.fillStyle = GREY + "0.06)"; g.fillRect(0, L11G - 150, W, 150); g.strokeStyle = GREY + "0.18)"; g.lineWidth = 2;
			for (int k = 0; k < 3; k++) { g.beginPath(); for (double x = 0; x <= W; x += 20) { double y = L11G - 140 + k * 40 + JS.sin(x / 70 + t * 1.5 + k) * 5; if (x != 0) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); }
			g.fillStyle = GREY + "0.14)"; g.fillRect(0, L11G, W, 40);
			// a palm tree
			g.strokeStyle = GREY + "0.22)"; g.lineWidth = 12; g.beginPath(); g.moveTo(560, L11G); g.quadraticCurveTo(540, L11G - 120, 590, L11G - 230); g.stroke(); g.fillStyle = GREY + "0.2)";
			for (int i = 0; i < 5; i++) { double an = -Math.PI * 0.95 + i * 0.47 + JS.sin(t * 1.2 + i) * 0.05; g.save(); g.translate(590, L11G - 230); g.rotate(an); g.beginPath(); g.ellipse(60, 0, 64, 13, 0.2, 0, TAU); g.fill(); g.restore(); }
			// the slingshot
			g.globalAlpha = a * (1 - sp * 0.6); g.strokeStyle = GREY + "0.75)"; g.lineWidth = 16; g.lineCap = "round"; g.beginPath(); g.moveTo(L11SL[0], L11G); g.lineTo(L11SL[0], L11SL[1] + 70); g.lineTo(L11SL[0] - 34, L11SL[1] - 10); g.moveTo(L11SL[0], L11SL[1] + 70); g.lineTo(L11SL[0] + 34, L11SL[1] - 10); g.stroke(); g.lineCap = "butt";
			// the pigs' fort
			for (int i = 0; i < L11FORT.Count; i++)
			{
				if (t >= gone[i]) continue; double x = L11FORT[i][0], y = L11FORT[i][1], w = L11FORT[i][2], hh = L11FORT[i][3], m = L11FORT[i][4]; g.fillStyle = GREY + (m == 2 ? "0.42)" : m == 1 ? "0.3)" : "0.22)"); g.fillRect(x, y, w, hh); g.strokeStyle = GREY + "0.45)"; g.lineWidth = 2; g.strokeRect(x, y, w, hh);
				if (m == 1) { g.beginPath(); g.moveTo(x + w * 0.3, y + 4); g.lineTo(x + w * 0.3, y + hh - 4); g.moveTo(x + w * 0.7, y + 4); g.lineTo(x + w * 0.7, y + hh - 4); g.stroke(); }
			}
			foreach (var (x, y, r, nd) in L11PIGS)
			{
				if (t >= gone[L11FORT.Count]) break; if (nd.Any(i => t >= gone[i] + 0.15)) continue; g.fillStyle = GREY + "0.5)"; circle(x, y, r); g.fillStyle = GREY + "0.75)"; g.beginPath(); g.ellipse(x, y + 3, r * 0.38, r * 0.28, 0, 0, TAU); g.fill(); g.fillStyle = "rgba(0,0,0,0.6)"; circle(x - r * 0.12, y + 3, 2.5); circle(x + r * 0.12, y + 3, 2.5);
				g.fillStyle = "#000"; circle(x - r * 0.4, y - r * 0.3, 3); circle(x + r * 0.4, y - r * 0.3, 3); g.fillStyle = GREY + "0.5)"; circle(x - r * 0.65, y - r * 0.75, r * 0.25); circle(x + r * 0.65, y - r * 0.75, r * 0.25);
			}
			g.globalAlpha = 1;
			double fz = L11frozen(t); if (fz > 0) { g.fillStyle = $"rgba(200,205,220,{str(0.16 * fz)})"; g.fillRect(0, 0, W, H); g.strokeStyle = $"rgba(255,255,255,{str(0.35 * fz)})"; g.lineWidth = 3; for (int i = 0; i < 6; i++) { double x = 120 + i * 210; g.beginPath(); g.moveTo(x, 0); g.lineTo(x + 30, 90); g.lineTo(x - 10, 180); g.stroke(); } }
		};
		return h;
	}
	// slingshot: the band pulls back before every shot (decoration)
	static Hz L11Sling(double t0, double t1, List<double[]> shots)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double pull = 0; foreach (var q in shots) { double ta = q[0], tb = q[1]; if (t > ta && t < tb + 0.08) pull = t < tb ? L11ease((t - ta) / (tb - ta)) : 1 - (t - tb) / 0.08; }
			double x = L11SL[0], y = L11SL[1], px = x - 60 * pull, py = y + 30 * pull;
			g.strokeStyle = "rgba(120,70,60,0.9)"; g.lineWidth = 6; g.beginPath(); g.moveTo(x - 34, y - 10); g.lineTo(px, py); g.lineTo(x + 34, y - 10); g.stroke(); g.fillStyle = "rgba(120,70,60,0.9)"; circle(px, py, 9);
		};
		return h;
	}
	// a block of the fort falls and rolls
	static Hz L11Block(double x, double y, double w, double hh, double vx, double vy, double va, double t0)
	{
		double px = x + w / 2, py = y + hh / 2, a = 0; int bn = 0; double life = t0 + 2.2;
		var h = new Hz();
		h.update = (t, dt) => { if (t > life) h.dead = true; if (t < t0) return; vy += 1500 * dt; px += vx * dt; py += vy * dt; a += va * dt; if (py > L11G - hh / 2) { py = L11G - hh / 2; if (bn < 1) { vy *= -0.3; bn++; } else vy = 0; vx *= 0.97; va *= 0.9; } };
		h.draw = t => { if (t < t0) return; double al = Math.Min(1, (life - t) / 0.4); g.save(); g.globalAlpha = al; g.translate(px, py); g.rotate(a); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.fillRect(-w / 2, -hh / 2, w, hh); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 2; g.strokeRect(-w / 2 + 4, -hh / 2 + 4, w - 8, hh - 8); g.restore(); };
		h.hit = (p, t) => { if (t < t0 || t > life - 0.4) return false; return obbHit(p.x, p.y, p.r - 3, px, py, w / 2, hh / 2, a); };
		return h;
	}
	// a pink planet that hurts, pulsing with the beat, with its orbit circle (grey)
	static Hz L11Planet(double x, double y, double r, double R, double t0, double t1, double ph)
	{
		double on = t0 + 0.8; Func<double, double> pr = t => r * (1 + 0.07 * Math.Max(0, 1 - ((BT(t) % 1 + 1) % 1) * 3));
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.6, (t1 - t) / 0.6)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			if (t < on) { double k = L11wk(t, t0, on); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); L11dash(false); }
			else { double rr = pr(t); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; circle(x, y, rr); g.shadowBlur = 0; g.fillStyle = "rgba(120,0,40,0.35)"; circle(x - rr * 0.3, y - rr * 0.2, rr * 0.22); circle(x + rr * 0.35, y + rr * 0.3, rr * 0.15); circle(x + rr * 0.1, y - rr * 0.5, rr * 0.1); }
			g.strokeStyle = GREY + "0.45)"; g.lineWidth = 3; g.beginPath(); g.ellipse(x, y, r * 1.5, r * 0.35, -0.3, Math.PI * 1.05, Math.PI * 1.95 + 0.6); g.stroke();
			g.strokeStyle = GREY + "0.18)"; g.lineWidth = 2; L11dash(true, 4, 10); g.beginPath(); g.arc(x, y, R, ph + t * 0.3, ph + t * 0.3 + TAU); g.stroke(); L11dash(false); g.restore();
		};
		h.hit = (p, t) => { if (t < on || t > t1 - 0.4) return false; return hypot(p.x - x, p.y - y) < pr(t) * 0.95 + p.r - 3; };
		return h;
	}
	// a big space pig in the middle (decoration): sad eyes
	static Hz L11SadPig(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 - t) / 0.5)); if (a <= 0) return; double x = W / 2, y = H / 2, r = 150; g.save(); g.globalAlpha = a * 0.9; g.fillStyle = GREY + "0.2)"; circle(x, y, r); g.strokeStyle = GREY + "0.4)"; g.lineWidth = 4; g.beginPath(); g.arc(x, y, r + 26, 0, TAU); g.stroke();
			g.fillStyle = GREY + "0.3)"; g.beginPath(); g.ellipse(x, y + 20, 52, 38, 0, 0, TAU); g.fill(); g.fillStyle = "rgba(0,0,0,0.5)"; circle(x - 16, y + 20, 7); circle(x + 16, y + 20, 7);
			g.fillStyle = "rgba(255,255,255,0.5)"; circle(x - 60, y - 40, 22); circle(x + 60, y - 40, 22); g.fillStyle = "#000"; circle(x - 56, y - 34, 9); circle(x + 56, y - 34, 9); g.strokeStyle = GREY + "0.6)"; g.lineWidth = 5; g.beginPath(); g.moveTo(x - 90, y - 60); g.lineTo(x - 35, y - 80); g.moveTo(x + 90, y - 60); g.lineTo(x + 35, y - 80); g.stroke();
			g.fillStyle = GREY + "0.2)"; circle(x - 110, y - 120, 36); circle(x + 110, y - 120, 36); g.restore();
		};
		return h;
	}
	// the frown: a wide pink arc sweeping the screen (dir 1 down, -1 up), with an opening at gx
	static Hz L11Frown(double dir, double gx, double gw, double tw, double ta, double te)
	{
		const double Rr = 1500, th = 48; Func<double, double> Y = t => { double k = clamp((t - ta) / (te - ta), 0, 1); return dir > 0 ? -120 + (H + 260) * k : H + 120 - (H + 260) * k; };
		Func<double, double, double> yAt = (x, yc) => yc + Rr - Math.Sqrt(Math.Max(0, Rr * Rr - L11sq(x - W / 2)));
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = L11wk(t, tw, ta), yc0 = Y(ta + 0.25 * (te - ta)); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); for (double x = 0; x <= W; x += 20) { double y = yAt(x, yc0); if (x != 0) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); L11dash(false);
				g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; g.beginPath(); g.moveTo(gx, yAt(gx, yc0) + 30); g.lineTo(gx - 16, yAt(gx, yc0) + 56); g.lineTo(gx + 16, yAt(gx, yc0) + 56); g.closePath(); g.fill(); return;
			}
			double yc = Y(t); g.save(); g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 16; g.lineWidth = th; g.lineCap = "round";
			g.beginPath(); for (double x = -20; x <= gx - gw / 2; x += 16) { double y = yAt(x, yc); if (x > -20) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); g.beginPath(); for (double x = gx + gw / 2; x <= W + 20; x += 16) { double y = yAt(x, yc); if (x > gx + gw / 2) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); g.lineCap = "butt"; g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; if (Math.Abs(p.x - gx) < gw / 2 - th / 2 - p.r + 3) return false; return Math.Abs(p.y - yAt(p.x, Y(t))) < th / 2 + p.r - 3; };
		return h;
	}
	// a big lock in the middle: the shackle comes down and closes, the whole lock hurts until te
	static Hz L11Lock(double cx, double cy, double tw, double ta, double tc, double te)
	{
		const double bw = 230, bh = 170, sr = 95;
		Func<double, double> sy = t => t < tc ? -260 + (cy - 110 - (-260)) * L11ease((t - ta) / (tc - ta)) : cy - 110;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) { double k = L11wk(t, tw, ta); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.strokeRect(cx - bw / 2, cy - bh / 2, bw, bh); g.beginPath(); g.arc(cx, cy - bh / 2 - 30, sr, Math.PI, 0); g.stroke(); L11dash(false); return; }
			double a = Math.Min(1, (te - t) / 0.3), y = sy(t); g.save(); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.lineWidth = 36; g.beginPath(); g.moveTo(cx - sr, y + 90); g.lineTo(cx - sr, y); g.arc(cx, y, sr, Math.PI, 0); g.lineTo(cx + sr, y + 90); g.stroke();
			g.fillStyle = PINK; g.fillRect(cx - bw / 2, cy - bh / 2, bw, bh); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.5)"; circle(cx, cy - 12, 20); g.fillRect(cx - 8, cy - 12, 16, 50); if (t > tc && t < tc + 0.2) { g.fillStyle = $"rgba(255,255,255,{str(1 - (t - tc) / 0.2)})"; g.fillRect(cx - bw / 2, cy - bh / 2, bw, bh); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te - 0.3) return false; if (Math.Abs(p.x - cx) < bw / 2 + p.r - 3 && Math.Abs(p.y - cy) < bh / 2 + p.r - 3) return true; double y = sy(t);
			foreach (var sx in new[] { cx - sr, cx + sr }) if (Math.Abs(p.x - sx) < 18 + p.r - 3 && p.y > y - 4 && p.y < y + 90) return true; if (p.y < y) { double d = hypot(p.x - cx, p.y - y); if (Math.Abs(d - sr) < 18 + p.r - 3) return true; }
			return false;
		};
		return h;
	}
	// an egg: Matilda drops it, it falls, and on the ground it breaks in two pieces going left and right
	static Hz L11Egg(double x, double y0, double tw, double td)
	{
		double tl = 0; const double gv = 1500; double yl = L11G - 16; Func<double, double> Y = t => Math.Min(yl, y0 + 0.5 * gv * L11sq(t - td)); { tl = td + Math.Sqrt(2 * (yl - y0) / gv); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > tl + 0.9) h.dead = true; };
		h.draw = t =>
		{
			if (t < td) { double k = L11wk(t, tw, td); g.strokeStyle = $"rgba(255,255,255,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true, 6, 10); g.beginPath(); g.moveTo(x, y0 + 20); g.lineTo(x, yl); g.stroke(); L11dash(false); g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.beginPath(); g.ellipse(x, yl + 12, 26 * k, 6, 0, 0, TAU); g.fill(); return; }
			if (t < tl) { double y = Y(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.ellipse(x, y, 14, 18, 0, 0, TAU); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.45)"; circle(x - 4, y - 6, 4); g.restore(); return; }
			double e = t - tl, a = Math.Max(0, 1 - e / 0.9); g.save(); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; foreach (var s in new double[] { -1, 1 }) { double sx = x + s * (30 + e * 260); g.beginPath(); g.moveTo(sx - 14, yl + 12); g.lineTo(sx, yl - 10); g.lineTo(sx + 14, yl + 12); g.closePath(); g.fill(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < td) return false; if (t < tl) { return hypot(p.x - x, p.y - Y(t)) < 15 + p.r - 3; }
			double e = t - tl; if (e > (easy ? 0.4 : 0.7)) return false; foreach (var s in new double[] { -1, 1 }) { double sx = x + s * (30 + e * 260); if (Math.Abs(p.x - sx) < 14 + p.r - 3 && p.y > yl - 12 - p.r + 3) return true; }
			return false;
		};
		return h;
	}
	/* ---- phase 2: a day at the sea (top view) ---- */
	const double L11SEA = 96;
	static Hz L11Beach(double t0, double t1)
	{
		var R = mulberry32(2222); var SH = new List<double[]>(); for (int i = 0; i < 26; i++) SH.Add(new[] { R() * W, L11SEA + 60 + R() * (H - L11SEA - 80), R() * TAU, 3 + R() * 5 });
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; g.globalAlpha = a; double b = BT(t), dusk = clamp((b - 164) / 8, 0, 1);
			g.fillStyle = $"rgba(255,226,170,{str(0.13 * (1 - dusk))})"; g.fillRect(0, 0, W, H); g.fillStyle = $"rgba(255,240,200,{str(0.08 * (1 - dusk))})"; circle(1180, 40, 70);
			// the sea at the top and its foam going in and out
			g.fillStyle = $"rgba(110,180,240,{str(0.24 - 0.12 * dusk)})"; g.fillRect(0, 0, W, L11SEA); double fo = L11SEA + JS.sin(t * 1.3) * 10; g.strokeStyle = "rgba(255,255,255,0.28)"; g.lineWidth = 3; g.beginPath(); for (double x = 0; x <= W; x += 16) { double y = fo + JS.sin(x / 55 + t * 2) * 6; if (x != 0) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke();
			g.strokeStyle = "rgba(255,255,255,0.12)"; g.lineWidth = 2; for (int k = 0; k < 3; k++) { g.beginPath(); for (double x = 0; x <= W; x += 24) { double y = 16 + k * 24 + JS.sin(x / 90 - t * 1.4 + k) * 4; if (x != 0) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); }
			// sand, shells and starfish (decoration)
			foreach (var q in SH) { double x = q[0], y = q[1], an = q[2], s = q[3]; g.save(); g.translate(x, y); g.rotate(an); g.fillStyle = GREY + "0.16)"; g.beginPath(); for (int i = 0; i < 10; i++) { double rr = i % 2 != 0 ? s : s * 2.2, qq = i * Math.PI / 5; g.lineTo(JS.cos(qq) * rr, JS.sin(qq) * rr); } g.closePath(); g.fill(); g.restore(); }
			// the sun reflecting on the sea
			g.fillStyle = $"rgba(255,255,255,{str(0.14 * (1 - dusk))})"; for (int i = 0; i < 6; i++) g.fillRect(980 + i * 22 - ((t * 40) % 22), 40 + i * 7, 30, 3);
			if (dusk > 0) { g.fillStyle = $"rgba(255,120,80,{str(0.12 * dusk)})"; g.fillRect(0, 0, W, H); g.fillStyle = $"rgba(10,10,35,{str(0.45 * clamp((b - 168) / 4, 0, 1))})"; g.fillRect(0, 0, W, H); }
			g.globalAlpha = 1;
		};
		return h;
	}
	// the beach hut at the top middle: the door opens and sun rays come out
	static Hz L11Hut(double x, double tw, double ta, double te, double[] angs)
	{
		double y = L11SEA + 10;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - tw) / 0.4, (te - t) / 0.4)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.35)"; g.fillRect(x - 90, y - 80, 180, 90); g.beginPath(); g.moveTo(x - 110, y - 80); g.lineTo(x, y - 130); g.lineTo(x + 110, y - 80); g.closePath(); g.fill();
			double op = t < ta ? 0 : L11ease((t - ta) / 0.25); g.fillStyle = "rgba(0,0,0,0.5)"; g.fillRect(x - 30, y - 60, 60, 70); g.fillStyle = GREY + "0.7)"; g.fillRect(x - 30, y - 60, 60 * (1 - op), 70);
			foreach (var an in angs)
			{
				double c = JS.cos(an), s = JS.sin(an); if (t < ta) { double k = L11wk(t, tw, ta); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); g.moveTo(x, y); g.lineTo(x + c * 1500, y + s * 1500); g.stroke(); L11dash(false); continue; }
				double w = t < ta + 0.15 ? (t - ta) / 0.15 * 34 : 34, al = Math.Min(1, (te - 0.1 - t) / 0.3); if (al <= 0) continue; g.globalAlpha = a * al; g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 18; g.lineWidth = w; g.beginPath(); g.moveTo(x, y); g.lineTo(x + c * 1500, y + s * 1500); g.stroke(); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = w * 0.25; g.stroke(); g.globalAlpha = a;
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta + 0.08 || t > te - 0.4) return false; foreach (var an in angs) { double c = JS.cos(an), s = JS.sin(an), dx = p.x - x, dy = p.y - y, u = dx * c + dy * s; if (u < 0) continue; if (Math.Abs(-dx * s + dy * c) < 17 + p.r - 3) return true; } return false; };
		return h;
	}
	// something falling from above: its shadow grows before, it hits when it lands (ball, scoop, buoy, smash)
	// after landing: stay = stays there, roll = rolls with speed (vx,vy)
	public class L11DO { public double? wn; public double stay, vx, vy; public bool roll; }
	static Hz L11Drop(double x, double y, double r, double tw, double ta, string kind, L11DO o = null)
	{
		o ??= new L11DO();
		double stay = o.stay != 0 && !double.IsNaN(o.stay) ? o.stay : 0.12, te = ta + stay + (o.roll ? 6 : 0) + 0.3, vx = o.vx, vy = o.vy;
		Func<double, (double, double)> pos = t => { if (!o.roll || t < ta + stay) return (x, y); double e = t - ta - stay; return (x + vx * e, y + vy * e); };
		bool ring = kind == "buoy";
		var h = new Hz();
		h.update = (t, dt) => { var (px, py) = pos(t); if (t > te || (o.roll && (px < -80 || px > W + 80 || py < -80 || py > H + 80))) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L11wk(t, tw, ta), z = (1 - k) * (1 - k) * 620; g.fillStyle = $"rgba(0,0,0,{str(0.25 + 0.35 * k)})"; g.beginPath(); g.ellipse(x + z * 0.12, y + 6, r * (0.4 + 0.6 * k), r * (0.28 + 0.42 * k), 0, 0, TAU); g.fill();
				g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.55 * k)})"; g.lineWidth = 2; L11dash(true, 7, 7); g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); L11dash(false);
				if (k > 0.35) { g.save(); g.globalAlpha = Math.Min(1, (k - 0.35) / 0.3); L11dropShape(kind, x, y - z, r * (1 + z / 900), t); g.restore(); }
				return;
			}
			var (px, py) = pos(t); double al = Math.Min(1, (te - t) / 0.3); g.save(); g.globalAlpha = al; if (t < ta + 0.25) { g.strokeStyle = $"rgba(255,255,255,{str(1 - (t - ta) / 0.25)})"; g.lineWidth = 4; g.beginPath(); g.arc(x, y, r + (t - ta) * 260, 0, TAU); g.stroke(); }
			L11dropShape(kind, px, py, r, t, o.roll ? (t - ta) * (vx > 0 ? 6 : -6) : 0); g.restore();
		};
		h.hit = (p, t) => { if (t < ta - 0.02 || t > te - 0.3) return false; var (px, py) = pos(t); double d = hypot(p.x - px, p.y - py); if (ring) return d < r + p.r - 3 && d > r * 0.46 - p.r + 3; return d < r * 0.92 + p.r - 3; };
		return h;
	}
	static void L11dropShape(string kind, double x, double y, double r, double t, double spin = 0)
	{
		g.save(); g.translate(x, y); g.rotate(spin); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
		if (kind == "buoy") { g.beginPath(); g.arc(0, 0, r, 0, TAU); g.arc(0, 0, r * 0.46, 0, TAU, true); g.fill("evenodd"); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.7)"; for (int i = 0; i < 4; i++) { g.save(); g.rotate(i * Math.PI / 2 + 0.4); g.fillRect(r * 0.5, -r * 0.14, r * 0.48, r * 0.28); g.restore(); } }
		else if (kind == "scoop") { circle(0, 0, r); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.4)"; circle(-r * 0.3, -r * 0.35, r * 0.25); g.fillStyle = "rgba(255,255,255,0.2)"; for (int i = 0; i < 5; i++) { double q = i * TAU / 5; circle(JS.cos(q) * r * 0.85, JS.sin(q) * r * 0.85, r * 0.22); } }
		else { circle(0, 0, r); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.65)"; g.lineWidth = 3; g.beginPath(); g.arc(0, 0, r * 0.95, 0.4, 2.6); g.stroke(); g.beginPath(); g.arc(0, 0, r * 0.95, 3.5, 5.7); g.stroke(); g.beginPath(); g.moveTo(-r * 0.95, 0); g.lineTo(r * 0.95, 0); g.stroke(); }
		g.restore();
	}
	// a beach ball bouncing on a line: its shadow always on the ground, it hurts only while coming down near the ground
	static Hz L11Ball(List<double[]> pts, List<double> ts, double r, double tw)
	{
		int n = pts.Count; double te = ts[n - 1] + 0.2;
		Func<double, int> seg = t => { int i = 0; while (i < n - 1 && t > ts[i + 1]) i++; return i; };
		Func<double, (double, double, double)> st = t =>
		{
			if (t <= ts[0]) { double x1 = pts[0][0], y1 = pts[0][1], x2 = pts[1][0], y2 = pts[1][1], d = (ts[0] - t) / (ts[1] - ts[0]); return (x1 - (x2 - x1) * d, y1 - (y2 - y1) * d, 0); }
			int i = seg(t); if (i >= n - 1) { return (pts[n - 1][0], pts[n - 1][1], 0); }
			double u = (t - ts[i]) / (ts[i + 1] - ts[i]), xa = pts[i][0], ya = pts[i][1], xb = pts[i + 1][0], yb = pts[i + 1][1]; return (xa + (xb - xa) * u, ya + (yb - ya) * u, 4 * u * (1 - u) * 170);
		};
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			for (int i = 0; i < n; i++) { double e = ts[i] - t; if (e < -0.15 || e > 1.1) continue; double k = 1 - clamp(e / 1.1, 0, 1), x0 = pts[i][0], y0 = pts[i][1]; g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.55 * k)})"; g.lineWidth = 2; L11dash(true, 6, 7); g.beginPath(); g.arc(x0, y0, r, 0, TAU); g.stroke(); L11dash(false); }
			var (x, y, z) = st(t); if (x < -r * 3 || x > W + r * 3) return; g.fillStyle = "rgba(0,0,0,0.35)"; g.beginPath(); g.ellipse(x + z * 0.15, y + 6, r * 0.9, r * 0.6, 0, 0, TAU); g.fill();
			double rr = r * (1 + z / 420); g.save(); g.translate(x, y - z); g.rotate(t * 5); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; circle(0, 0, rr); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.75)"; for (int i = 0; i < 3; i++) { g.beginPath(); g.moveTo(0, 0); g.arc(0, 0, rr, i * TAU / 3, i * TAU / 3 + 0.5); g.closePath(); g.fill(); }
			g.fillStyle = "#fff"; circle(0, 0, rr * 0.18); g.restore();
		};
		h.hit = (p, t) => { if (t < ts[0] - 0.15 || t > te) return false; var (x, y, z) = st(t); if (z > 34) return false; return hypot(p.x - x, p.y - y) < r * 0.92 + p.r - 3; };
		return h;
	}
	// a row of crabs walking sideways (a column at x) with an opening. turns: when they turn back
	static Hz L11Crabs(double x0, double[] ys, double v, double tw, double t0, double[] turns, double te)
	{
		Func<double, double> X = t => { double x = x0, d = v > 0 ? 1 : -1, last = t0; double sp = Math.Abs(v); foreach (var tt in turns) { if (t < tt) break; x += d * sp * (tt - last); last = tt; d = -d; } return x + d * sp * (Math.Max(t, t0) - last); };
		Func<double, double> claw = t => { double f = ((BT(t) % 1) + 1) % 1; return f < 0.18 ? f / 0.18 : f < 0.4 ? 1 - (f - 0.18) / 0.22 : 0; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) { double k = L11wk(t, tw, t0); g.fillStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; foreach (var y in ys) { double ex = x0 < W / 2 ? 10 : W - 10; circle(ex, y, 8 + 4 * k); } return; }
			double x = X(t), c = claw(t), al = Math.Min(1, (te - t) / 0.4); g.save(); g.globalAlpha = al;
			foreach (var y in ys)
			{
				double step = JS.sin(t * 14 + y) * 4; g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.beginPath(); g.ellipse(x, y, 26, 19, 0, 0, TAU); g.fill();
				double cw = 22 + c * 16; foreach (var s in new double[] { -1, 1 }) { g.beginPath(); g.ellipse(x + s * cw, y - 14, 11, 8, s * 0.5, 0, TAU); g.fill(); }
				g.shadowBlur = 0; g.strokeStyle = PINK; g.lineWidth = 3;
				for (int i = -1; i <= 1; i++) foreach (var s in new double[] { -1, 1 }) { g.beginPath(); g.moveTo(x + s * 18, y + i * 7); g.lineTo(x + s * 34, y + i * 9 + step * s); g.stroke(); }
				g.fillStyle = "#fff"; circle(x - 8, y - 14, 5); circle(x + 8, y - 14, 5); g.fillStyle = "#000"; circle(x - 8, y - 14, 2.4); circle(x + 8, y - 14, 2.4);
			}
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t > te - 0.4) return false; double x = X(t), c = claw(t); foreach (var y in ys) { if (Math.Abs(p.y - y) > 24 + p.r - 3) continue; if (Math.Abs(p.x - x) < 26 + c * 16 + p.r - 3) return true; } return false; };
		return h;
	}
	// a boat crosses the sea leaving a V wake behind: the wake is in pieces (with openings)
	static Hz L11Wake(double dir, double v, double tw, double t0, double te, double ang)
	{
		const double by = 46, seg = 96, gap = 74; Func<double, double> X = t => dir > 0 ? -200 + v * (t - t0) : W + 200 - v * (t - t0);
		Func<double, List<double[]>> pts = t => { double x = X(t), c = -dir * JS.cos(ang), s = JS.sin(ang), age = t - t0, L = Math.Min(1700, age * v * 1.1); var o = new List<double[]>(); for (double d = 60; d < L; d += seg + gap) o.Add(new[] { x + c * d, by + s * d, x + c * (d + seg), by + s * (d + seg) }); return o; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) { double k = L11wk(t, tw, t0); g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; double ex = dir > 0 ? 14 : W - 14; g.beginPath(); g.moveTo(ex, by - 20); g.lineTo(ex + dir * 26, by); g.lineTo(ex, by + 20); g.fill(); return; }
			double x = X(t), al = Math.Min(1, (te - t) / 0.5); g.save(); g.globalAlpha = al; g.fillStyle = GREY + "0.75)"; g.beginPath(); g.moveTo(x + dir * 70, by); g.lineTo(x + dir * 30, by - 22); g.lineTo(x - dir * 60, by - 22); g.lineTo(x - dir * 60, by + 22); g.lineTo(x + dir * 30, by + 22); g.closePath(); g.fill();
			g.fillStyle = "rgba(255,255,255,0.8)"; g.fillRect(x - dir * 20 - 6, by - 40, 12, 30); g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.lineWidth = 18; g.lineCap = "round"; foreach (var q in pts(t)) { g.beginPath(); g.moveTo(q[0], q[1]); g.lineTo(q[2], q[3]); g.stroke(); }
			g.lineCap = "butt"; g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t > te - 0.5) return false; foreach (var q in pts(t)) { double x1 = q[0], y1 = q[1], dx = q[2] - x1, dy = q[3] - y1, u = clamp(((p.x - x1) * dx + (p.y - y1) * dy) / (dx * dx + dy * dy), 0, 1); if (hypot(p.x - x1 - dx * u, p.y - y1 - dy * u) < 9 + p.r - 3) return true; } return false; };
		return h;
	}
	// a sand castle in the middle: its towers fall outward one by one (a column comes out of the middle)
	static Hz L11Castle(double cx, double cy, double tw, double t0, double t1, List<double[]> falls, double len, double w)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(1, Math.Min((t - tw) / 0.4, (t1 - t) / 0.4)); g.save(); g.globalAlpha = a;
			foreach (var q in falls)
			{
				double an = q[0], tf = q[1], c = JS.cos(an), s = JS.sin(an);
				if (t < tf) { double k = L11wk(t, tf - 1.2, tf); if (k <= 0) continue; g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.55 * k)})"; g.lineWidth = 2; L11dash(true); g.save(); g.translate(cx, cy); g.rotate(an); g.strokeRect(70, -w / 2, len, w); g.restore(); L11dash(false); }
				else
				{
					double e = t - tf, l = Math.Min(1, e / 0.18) * len, crum = e > 1.2 ? Math.Min(1, (e - 1.2) / 0.5) : 0; if (crum >= 1) continue; g.save(); g.translate(cx, cy); g.rotate(an); g.globalAlpha = a * (1 - crum); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
					for (double qq = 0; qq < l; qq += w * 0.9) { g.fillRect(70 + qq, -w / 2, w * 0.82, w); }
					g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.4)"; for (double qq = 0; qq < l; qq += w * 0.9) g.fillRect(70 + qq + 4, -w / 2 + 4, w * 0.3, w * 0.25); g.restore();
				}
			}
			g.globalAlpha = a; g.fillStyle = GREY + "0.4)"; g.fillRect(cx - 70, cy - 70, 140, 140); g.fillStyle = GREY + "0.55)"; foreach (var (dx, dy) in new[] { (-70.0, -70.0), (70.0, -70.0), (-70.0, 70.0), (70.0, 70.0) }) circle(cx + dx, cy + dy, 26);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); g.moveTo(cx, cy - 40); g.lineTo(cx + 30, cy - 28); g.lineTo(cx, cy - 16); g.fill(); g.fillRect(cx - 2, cy - 40, 4, 50); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; if (Math.Abs(p.x - cx) < 70 + p.r - 3 && Math.Abs(p.y - cy) < 70 + p.r - 3 && t < t1 - 0.3) return true;
			foreach (var q in falls) { double an = q[0], tf = q[1]; if (t < tf) continue; double e = t - tf; if (e > 1.3) continue; double l = Math.Min(1, e / 0.18) * len; if (obbHit(p.x, p.y, p.r - 3, cx + JS.cos(an) * (70 + l / 2), cy + JS.sin(an) * (70 + l / 2), l / 2, w / 2, an)) return true; }
			return false;
		};
		return h;
	}
	// beach umbrellas falling one after another (dominoes): each falls on its side toward dir
	static Hz L11Umbrellas(double[] xs, double y, double dir, double tw, double[] ts, double te, double r = 62)
	{
		int n = xs.Length;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(1, Math.Min((t - tw) / 0.4, (te - t) / 0.4)); g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double x = xs[i], tf = ts[i], k = t < tf ? 0 : L11ease((t - tf) / 0.25), cx = x + dir * k * r * 1.5;
				if (t < tf) { double kk = L11wk(t, tf - 1.0, tf); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * kk)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); g.ellipse(x + dir * r * 1.5, y, r * 1.1, r * 0.7, 0, 0, TAU); g.stroke(); L11dash(false); }
				g.strokeStyle = GREY + "0.7)"; g.lineWidth = 5; g.beginPath(); g.moveTo(x, y); g.lineTo(cx, y); g.stroke(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.beginPath(); g.ellipse(cx, y, r * (1 + 0.1 * k), r * (1 - 0.3 * k), 0, 0, TAU); g.fill(); g.shadowBlur = 0;
				g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; for (int q = 0; q < 8; q++) { double an = q * TAU / 8; g.beginPath(); g.moveTo(cx, y); g.lineTo(cx + JS.cos(an) * r * (1 + 0.1 * k), y + JS.sin(an) * r * (1 - 0.3 * k)); g.stroke(); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ts[0] - 1 || t > te - 0.3) return false;
			for (int i = 0; i < n; i++) { double tf = ts[i]; if (t < tf - 0.6) continue; double k = t < tf ? 0 : L11ease((t - tf) / 0.25), cx = xs[i] + dir * k * r * 1.5, rx = r * (1 + 0.1 * k), ry = r * (1 - 0.3 * k); double dx = (p.x - cx) / (rx + p.r - 3), dy = (p.y - y) / (ry + p.r - 3); if (dx * dx + dy * dy < 1) return true; }
			return false;
		};
		return h;
	}
	// a big wave (surf): a foam band comes down from the sea and sweeps the whole screen, with a tunnel (the opening) moving left or right
	static Hz L11Surf(double tw, double ta, double te, double gx0, double gx1, double gw, double th = 130)
	{
		Func<double, double> Y = t => -th + (H + th * 2) * clamp((t - ta) / (te - ta), 0, 1), GX = t => gx0 + (gx1 - gx0) * L11ease((t - ta) / (te - ta));
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta)
			{
				double k = L11wk(t, tw, ta); g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.12 * k)})"; g.fillRect(0, 0, W, L11SEA); g.strokeStyle = $"rgba(255,255,255,{str(0.35 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); g.moveTo(gx0 - gw / 2, L11SEA + 20); g.lineTo(gx0 - gw / 2, L11SEA + 80); g.moveTo(gx0 + gw / 2, L11SEA + 20); g.lineTo(gx0 + gw / 2, L11SEA + 80); g.stroke(); L11dash(false);
				g.fillStyle = $"rgba(255,255,255,{str(0.35 + 0.5 * k)})"; double ax = gx0 + (gx1 > gx0 ? 1 : -1) * 30; g.beginPath(); g.moveTo(ax, L11SEA + 50); g.lineTo(ax - (gx1 > gx0 ? 1 : -1) * 18, L11SEA + 38); g.lineTo(ax - (gx1 > gx0 ? 1 : -1) * 18, L11SEA + 62); g.fill(); return;
			}
			double y = Y(t), gx = GX(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(-10, y - th / 2, gx - gw / 2 + 10, th); g.fillRect(gx + gw / 2, y - th / 2, W - gx - gw / 2 + 10, th); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.55)"; for (double x = -10; x < W + 10; x += 46) { if (Math.Abs(x - gx) < gw / 2 + 10) continue; circle(x + ((t * 80) % 46), y + th / 2 - 6, 12); }
			g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = 4; g.beginPath(); g.arc(gx, y, gw / 2, 0, TAU); g.stroke(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta) return false; double y = Y(t), gx = GX(t); if (Math.Abs(p.y - y) > th / 2 + p.r - 3) return false; return Math.Abs(p.x - gx) > gw / 2 - p.r + 3; };
		return h;
	}
	// the lifeguard tower (decoration)
	static Hz L11Guard(double x, double y, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 - t) / 0.5)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.45)"; g.fillRect(x - 40, y - 40, 80, 60); g.strokeStyle = GREY + "0.45)"; g.lineWidth = 5; g.beginPath(); g.moveTo(x - 34, y + 20); g.lineTo(x - 44, y + 70); g.moveTo(x + 34, y + 20); g.lineTo(x + 44, y + 70); g.stroke(); g.fillStyle = "rgba(255,255,255,0.35)"; g.fillRect(x - 10, y - 34, 20, 40); g.fillRect(x - 28, y - 16, 56, 8); g.restore(); };
		return h;
	}
	// ice cream: a scoop falls on the cone (Drop), then melts: a pool that grows and shrinks
	static Hz L11Melt(double x, double y, double r1, double tw, double ta, double te)
	{
		Func<double, double> R = t => t < ta ? 0 : t < ta + 0.6 ? 20 + (r1 - 20) * L11ease((t - ta) / 0.6) : t < te - 0.5 ? r1 : r1 * Math.Max(0, (te - t) / 0.5);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) { double k = L11wk(t, tw, ta); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); g.arc(x, y, r1, 0, TAU); g.stroke(); L11dash(false); return; }
			double r = R(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = "rgba(255,42,109,0.85)"; g.beginPath(); for (int i = 0; i <= 20; i++) { double q = i * TAU / 20, rr = r * (1 + 0.08 * JS.sin(q * 3 + t * 3)); if (i != 0) g.lineTo(x + JS.cos(q) * rr, y + JS.sin(q) * rr); else g.moveTo(x + JS.cos(q) * rr, y + JS.sin(q) * rr); }
			g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.3)"; circle(x - r * 0.3, y - r * 0.3, r * 0.18); g.restore();
		};
		h.hit = (p, t) => { if (t < ta + 0.1 || t > te - 0.3) return false; return hypot(p.x - x, p.y - y) < R(t) * 0.9 + p.r - 3; };
		return h;
	}
	static Hz L11Cone(double x, double y, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, Math.Min((t - t0) / 0.4, (t1 - t) / 0.4)); if (a <= 0) return; g.save(); g.globalAlpha = a * 0.8; g.fillStyle = "rgba(215,190,150,0.45)"; g.beginPath(); g.moveTo(x - 22, y - 8); g.lineTo(x + 22, y - 8); g.lineTo(x, y + 44); g.closePath(); g.fill(); g.strokeStyle = "rgba(255,255,255,0.25)"; g.lineWidth = 2; g.beginPath(); g.moveTo(x - 14, y); g.lineTo(x + 6, y + 26); g.moveTo(x + 14, y); g.lineTo(x - 6, y + 26); g.stroke(); g.restore(); };
		return h;
	}
	// the volleyball net (decoration)
	static Hz L11Net(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 - t) / 0.5)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.strokeStyle = GREY + "0.4)"; g.lineWidth = 3; g.beginPath(); g.moveTo(W / 2, L11SEA + 20); g.lineTo(W / 2, H - 10); g.stroke(); g.lineWidth = 1; for (double y = L11SEA + 30; y < H; y += 18) { g.beginPath(); g.moveTo(W / 2 - 10, y); g.lineTo(W / 2 + 10, y); g.stroke(); } g.fillStyle = GREY + "0.6)"; circle(W / 2, L11SEA + 20, 7); circle(W / 2, H - 10, 7); g.restore(); };
		return h;
	}
	// a kite: its place is f(t), and its tail follows it (the same place a little earlier)
	static Hz L11Kite(Func<double, double[]> f, double tw, double t0, double t1, int n = 12)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < t0) { double k = L11wk(t, tw, t0); var q0 = f(t0); double x0 = q0[0], y0 = q0[1]; g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); for (int i = 0; i <= 40; i++) { var q = f(t0 + i * 0.05); if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); } g.stroke(); L11dash(false); g.fillStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; circle(x0, y0, 10); return; }
			double al = Math.Min(1, (t1 - t) / 0.5); g.save(); g.globalAlpha = al; g.strokeStyle = PINK; g.lineWidth = 2.5; g.beginPath(); for (int i = 0; i <= n; i++) { var q = f(Math.Max(t0 - 1, t - i * 0.07)); if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); } g.stroke();
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; for (int i = 2; i <= n; i += 2) { var q = f(Math.Max(t0 - 1, t - i * 0.07)); g.save(); g.translate(q[0], q[1]); g.rotate(i); g.beginPath(); g.moveTo(-9, -6); g.lineTo(9, 6); g.lineTo(9, -6); g.lineTo(-9, 6); g.closePath(); g.fill(); g.restore(); }
			var Q = f(t); var Q2 = f(t - 0.05); double x = Q[0], y = Q[1], an = JS.atan2(y - Q2[1], x - Q2[0]) + Math.PI / 2; g.save(); g.translate(x, y); g.rotate(an * 0.3); g.beginPath(); g.moveTo(0, -34); g.lineTo(24, 0); g.lineTo(0, 44); g.lineTo(-24, 0); g.closePath(); g.fill(); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.6)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, -34); g.lineTo(0, 44); g.moveTo(-24, 0); g.lineTo(24, 0); g.stroke(); g.restore(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t > t1 - 0.4) return false; var Q = f(t); if (hypot(p.x - Q[0], p.y - Q[1]) < 24 + p.r - 3) return true;
			for (int i = 2; i <= n; i += 2) { var q = f(Math.Max(t0 - 1, t - i * 0.07)); if (hypot(p.x - q[0], p.y - q[1]) < 9 + p.r - 3) return true; }
			return false;
		};
		return h;
	}
	/* ---- phase 3: the fair at night ---- */
	static Hz L11Fair(double t0, double t1)
	{
		var R = mulberry32(3333); var RIDES = new List<double[]>(); for (int i = 0; i < 7; i++) { double a1 = 60 + i * 190 + R() * 40, a2 = 120 + R() * 120, a3 = 40 + R() * 60; RIDES.Add(new[] { a1, a2, a3 }); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; g.globalAlpha = a; double bt = BT(t); g.fillStyle = "rgba(10,8,30,0.5)"; g.fillRect(0, 0, W, H);
			// far rides (a coaster arc and tents)
			g.strokeStyle = GREY + "0.08)"; g.lineWidth = 4; g.beginPath(); g.moveTo(0, H - 120); for (double x = 0; x <= W; x += 20) g.lineTo(x, H - 170 - JS.sin(x / 170) * 90 - Math.Max(0, JS.sin(x / 90)) * 30); g.stroke();
			foreach (var q in RIDES) { double x = q[0], hh = q[1], w = q[2]; double y0 = H - hh * 0.5, ww = w * 1.3; g.fillStyle = GREY + "0.05)"; g.fillRect(x - ww, y0, ww * 2, H - y0); g.beginPath(); g.moveTo(x - ww - 10, y0); g.quadraticCurveTo(x, y0 - 70, x, y0 - 90); g.quadraticCurveTo(x, y0 - 70, x + ww + 10, y0); g.closePath(); g.fill(); g.fillRect(x - 1, y0 - 120, 2, 32); g.beginPath(); g.moveTo(x + 1, y0 - 120); g.lineTo(x + 22, y0 - 112 + JS.sin(t * 4 + x) * 3); g.lineTo(x + 1, y0 - 104); g.fill(); }
			// the boardwalk: wooden boards
			g.strokeStyle = GREY + "0.06)"; g.lineWidth = 2; for (double y = 40; y < H; y += 46) { g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); }
			// strings of lights at the top (decoration)
			for (int s = 0; s < 3; s++)
			{
				double xa = -20 + s * 440, xb = xa + 460; Func<double, double> cy2 = u => 14 + 36 * (1 - L11sq(2 * u - 1)); g.strokeStyle = GREY + "0.3)"; g.lineWidth = 2; g.beginPath(); for (int q = 0; q <= 20; q++) { double u = q / 20.0; if (q != 0) g.lineTo(xa + (xb - xa) * u, cy2(u)); else g.moveTo(xa, cy2(0)); } g.stroke();
				for (int q = 1; q < 20; q += 2) { double u = q / 20.0; bool on = ((q >> 1) + s + Math.Floor(bt)) % 3 == 0; string col = new[] { "rgba(255,220,120,0.55)", "rgba(140,200,255,0.5)", "rgba(180,255,180,0.45)" }[(q >> 1) % 3]; g.fillStyle = on ? col : GREY + "0.18)"; circle(xa + (xb - xa) * u, cy2(u) + 7, on ? 6 : 4); }
			}
			g.globalAlpha = 1;
		};
		return h;
	}
	// the strength tester: a pole with a bell on top. The hammer hits below, the puck goes up and rings the bell, and the bell shoots a ring with an opening
	static Hz L11Striker(double x, double t0, double t1, double[] slams)
	{
		const double yb = H - 70, yt = 128;
		Func<double, double?> puck = t => { foreach (var ts in slams) { double e = t - ts; if (e >= 0 && e < 0.45) return yb - (yb - yt) * L11ease(e / 0.45); } return null; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 - t) / 0.5)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.25)"; g.fillRect(x - 14, yt, 28, yb - yt + 20); g.strokeStyle = GREY + "0.4)"; g.lineWidth = 2; for (double y = yt + 30; y < yb; y += 40) { g.beginPath(); g.moveTo(x - 22, y); g.lineTo(x + 22, y); g.stroke(); }
			double rg = 0; foreach (var ts in slams) { double e = t - ts - 0.45; if (e > 0 && e < 0.4) rg = Math.Max(rg, 1 - e / 0.4); }
			g.fillStyle = rg > 0 ? $"rgba(255,255,255,{str(0.4 + 0.6 * rg)})" : GREY + "0.6)"; g.beginPath(); g.arc(x, yt - 14, 30, Math.PI, 0); g.lineTo(x + 34, yt); g.lineTo(x - 34, yt); g.closePath(); g.fill();
			foreach (var ts in slams) { double e = t - ts; if (e > -1.1 && e < 0) { double k = 1 + e / 1.1; g.fillStyle = $"rgba(255,42,109,{str(0.15 + 0.45 * k)})"; g.fillRect(x - 16, yt, 32, yb - yt); } }
			double sw = 0; foreach (var ts in slams) { double e = t - ts; if (e > -0.5 && e < 0.15) sw = e < 0 ? 1 + e / 0.5 : 1 - e / 0.15; }
			g.save(); g.translate(x + 70, yb + 10); g.rotate(-1.2 * (1 - sw)); g.fillStyle = GREY + "0.7)"; g.fillRect(-4, -110, 8, 110); g.fillRect(-28, -130, 56, 28); g.restore();
			var py = puck(t); if (py != null) { g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(x - 26, py.Value - 18, 52, 36); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.5)"; g.fillRect(x - 18, py.Value - 12, 36, 6); }
			g.restore();
		};
		h.hit = (p, t) => { var py = puck(t); if (py == null) return false; return Math.Abs(p.x - x) < 26 + p.r - 3 && Math.Abs(p.y - py.Value) < 18 + p.r - 3; };
		return h;
	}
	// the drop tower: the seat goes up slowly, stops, and falls, and on the ground sparks run left and right
	static Hz L11Tower(double x, double tw, double tu, double tt, double td, double te, double sw = 150)
	{
		const double yb = H - 48, yt = 150, fall = 0.28;
		Func<double, double> Y = t => t < tu ? yb : t < tt ? yb - (yb - yt) * L11ease((t - tu) / (tt - tu)) : t < td ? yt : t < td + fall ? yt + (yb - yt) * L11sq((t - td) / fall) : yb;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(1, Math.Min((t - tw) / 0.5, (te - t) / 0.4)); g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.25)"; g.fillRect(x - 12, yt - 60, 24, yb - yt + 90); g.fillStyle = GREY + "0.45)"; g.fillRect(x - 30, yt - 80, 60, 24);
			if (t < td && t > td - 1.2) { double k = L11wk(t, td - 1.2, td); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.55 * k)})"; g.lineWidth = 2; L11dash(true); g.strokeRect(x - sw / 2, yb - 22, sw, 44); g.beginPath(); g.moveTo(0, H - 14); g.lineTo(W, H - 14); g.stroke(); L11dash(false); }
			double y = Y(t); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fillRect(x - sw / 2, y - 22, sw, 44); g.shadowBlur = 0; g.fillStyle = GREY + "0.85)"; for (int i = 0; i < 4; i++) { double px = x - sw / 2 + 20 + i * (sw - 40) / 3; circle(px, y - 4, 7); g.fillRect(px - 2, y - 30, 4, 20); }
			if (t >= td + fall) { double e = t - td - fall; if (e < 1.4) { g.globalAlpha = a * Math.Min(1, (1.4 - e) / 0.3); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; foreach (var s in new double[] { -1, 1 }) { double sx = x + s * (sw / 2 + e * 640); for (int i = 0; i < 3; i++) { g.beginPath(); g.moveTo(sx - s * i * 26, H); g.lineTo(sx - s * i * 26 + s * 14, H - 34 + i * 8); g.lineTo(sx - s * i * 26 - s * 10, H); g.fill(); } } } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tu - 0.02) return false; double y = Y(t); if (Math.Abs(p.x - x) < sw / 2 + p.r - 3 && Math.Abs(p.y - y) < 22 + p.r - 3) return true;
			if (t >= td + fall) { double e = t - td - fall; if (e < 1.15 && p.y > H - 36 - p.r + 3) { foreach (var s in new double[] { -1, 1 }) { double sx = x + s * (sw / 2 + e * 640); if (Math.Abs(p.x - (sx - s * 26)) < 40 + p.r - 3) return true; } } }
			return false;
		};
		return h;
	}
	// the pirate ship: swings from the top, higher every time. The hull and both arms hurt
	static Hz L11Ship(double tw, double t0, double t1, double[] amps, double per)
	{
		const double px = W / 2, py = -150, L = 640;
		Func<double, double> A = t => { double k = clamp((t - t0) / (t1 - t0), 0, 1) * (amps.Length - 1); int i = (int)Math.Min(amps.Length - 2, Math.Floor(k)); return amps[i] + (amps[i + 1] - amps[i]) * (k - i); };
		Func<double, double> th = t => t < t0 ? 0 : A(t) * JS.sin(TAU * (t - t0) / per) * Math.Min(1, (t - t0) / 0.4) * Math.Min(1, (t1 - t) / 0.6);
		Func<double, (double, double, double)> hull = t => { double a = th(t); return (px + JS.sin(a) * L, py + JS.cos(a) * L, -a); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double al = Math.Min(1, Math.Min((t - tw) / 0.5, (t1 - t) / 0.5)); g.save(); g.globalAlpha = al;
			if (t < t0) { double k = L11wk(t, tw, t0); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); g.beginPath(); g.arc(px, py, L, Math.PI / 2 - amps[amps.Length - 1], Math.PI / 2 + amps[amps.Length - 1]); g.stroke(); L11dash(false); }
			var (hx, hy, ha) = hull(t); g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; g.lineWidth = 10; foreach (var s in new double[] { -1, 1 }) { double ex = hx + JS.cos(ha) * s * 120, ey = hy + JS.sin(ha) * s * 120; g.beginPath(); g.moveTo(px, py); g.lineTo(ex, ey); g.stroke(); }
			g.save(); g.translate(hx, hy); g.rotate(ha); g.fillStyle = PINK; g.beginPath(); g.moveTo(-180, -30); g.lineTo(180, -30); g.lineTo(150, 20); g.quadraticCurveTo(0, 60, -150, 20); g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.3)"; for (int i = -3; i <= 3; i++) circle(i * 42, -6, 8); g.fillStyle = GREY + "0.8)"; for (int i = -4; i <= 4; i++) { circle(i * 36, -40, 7); }
			g.restore(); g.fillStyle = GREY + "0.6)"; circle(px, py, 20); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; var (hx, hy, ha) = hull(t); if (obbHit(p.x, p.y, p.r - 3, hx, hy + JS.cos(ha) * 0, 170, 32, ha)) return true;
			foreach (var s in new double[] { -1, 1 }) { double ex = hx + JS.cos(ha) * s * 120, ey = hy + JS.sin(ha) * s * 120, dx = ex - px, dy = ey - py, u = clamp(((p.x - px) * dx + (p.y - py) * dy) / (dx * dx + dy * dy), 0, 1); if (hypot(p.x - px - dx * u, p.y - py - dy * u) < 5 + p.r - 3) return true; }
			return false;
		};
		return h;
	}
	// teacups: a spinning platform with cups spinning around it, the distance grows and shrinks
	static Hz L11Cups(double cx, double cy, double R0, double R1, int n, double rc, double w, double tw, double t0, double t1)
	{
		Func<double, double> ang = t => w * (t - t0), Rr = t => R0 + (R1 - R0) * (0.5 - 0.5 * JS.cos(TAU * (t - t0) / (4 * SPB)));
		Func<double, List<double[]>> cups = t => { var o = new List<double[]>(); for (int i = 0; i < n; i++) { double a = ang(t) + i * TAU / n; o.Add(new[] { cx + JS.cos(a) * Rr(t), cy + JS.sin(a) * Rr(t), a }); } return o; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double al = Math.Min(1, Math.Min((t - tw) / 0.5, (t1 - t) / 0.5)); g.save(); g.globalAlpha = al; g.fillStyle = GREY + "0.12)"; circle(cx, cy, R1 + rc + 10); g.strokeStyle = GREY + "0.3)"; g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, R1 + rc + 10, 0, TAU); g.stroke();
			if (t < t0) { double k = L11wk(t, tw, t0); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; L11dash(true); foreach (var q in cups(t0)) { g.beginPath(); g.arc(q[0], q[1], rc, 0, TAU); g.stroke(); } L11dash(false); g.restore(); return; }
			foreach (var q in cups(t)) { double x = q[0], y = q[1], a = q[2]; g.save(); g.translate(x, y); g.rotate(-a * 2); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; circle(0, 0, rc); g.shadowBlur = 0; g.fillStyle = "rgba(0,0,0,0.3)"; circle(0, 0, rc * 0.62); g.strokeStyle = PINK; g.lineWidth = 7; g.beginPath(); g.arc(rc + 4, 0, 10, -1.2, 1.2); g.stroke(); g.fillStyle = GREY + "0.85)"; circle(-6, -4, 6); circle(7, 5, 6); g.restore(); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t > t1 - 0.4) return false; foreach (var q in cups(t)) if (hypot(p.x - q[0], p.y - q[1]) < rc + p.r - 3) return true; return false; };
		return h;
	}
	// the coaster's lift chain: a slanted line whose links move up, then it breaks in the middle and both halves fall (turning around their ends)
	static Hz L11Chain(double ax, double ay, double bx, double by, double tw, double t0, double tb, double te, double v)
	{
		double len = hypot(bx - ax, by - ay), an = JS.atan2(by - ay, bx - ax), mx = (ax + bx) / 2, my = (ay + by) / 2;
		Func<double, double> rot = t => t < tb ? 0 : L11ease((t - tb) / (te - 0.4 - tb)) * 1.35;
		Func<double, double[][]> halves = t => { double r = rot(t); return new[] { new[] { ax, ay, an - r, len / 2 }, new[] { bx, by, an + Math.PI + r, len / 2 } }; };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; if (t < t0) { double k = L11wk(t, tw, t0); g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); g.moveTo(ax, ay); g.lineTo(bx, by); g.stroke(); L11dash(false); return; }
			double al = Math.Min(1, (te - t) / 0.4), sh = ((t - t0) * v) % 34; g.save(); g.globalAlpha = al; g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.lineWidth = 6;
			foreach (var q in halves(t)) { double x0 = q[0], y0 = q[1], a = q[2], l = q[3]; double c = JS.cos(a), s = JS.sin(a), dir = x0 == ax ? 1 : -1; for (double d = (dir > 0 ? sh : 34 - sh); d < l; d += 34) { g.save(); g.translate(x0 + c * d, y0 + s * d); g.rotate(a); g.beginPath(); g.ellipse(0, 0, 15, 8, 0, 0, TAU); g.stroke(); g.restore(); } }
			if (t > tb && t < tb + 0.25) { g.fillStyle = $"rgba(255,255,255,{str(1 - (t - tb) / 0.25)})"; circle(mx, my, 40); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0 || t > te - 0.4) return false; foreach (var q in halves(t)) { double x0 = q[0], y0 = q[1], a = q[2], l = q[3]; double c = JS.cos(a), s = JS.sin(a), u = clamp((p.x - x0) * c + (p.y - y0) * s, 0, l); if (hypot(p.x - x0 - c * u, p.y - y0 - s * u) < 9 + p.r - 3) return true; } return false; };
		return h;
	}
	// a breaking track: track pieces standing across the screen crack and fall
	static Hz L11Crumble(double y, int n, double tw, double tb, double te)
	{
		double w = W / n; var P = new List<double[]>(); for (int i = 0; i < n; i++) P.Add(new[] { w * i + w / 2, (i % 2 != 0 ? 1 : -1) * (80 + i * 20), (i % 2 != 0 ? -1 : 1) * (2 + i * 0.4), i * 0.08 });
		Func<int, double, (double, double, double)> st = (i, t) => { double x = P[i][0], vx = P[i][1], va = P[i][2], dl = P[i][3], e = t - tb - dl; if (e < 0) return (x, y, 0); return (x + vx * e, y + 0.5 * 1400 * e * e - 220 * e, va * e); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; g.save();
			for (int i = 0; i < n; i++)
			{
				var (x, yy, a) = st(i, t); bool fall = t >= tb + P[i][3]; g.save(); g.translate(x, yy); g.rotate(a);
				if (!fall)
				{
					double k = L11wk(t, tw, tb); g.strokeStyle = $"rgba(225,225,240,{str(0.4 + 0.4 * k)})"; g.lineWidth = 3; g.beginPath(); g.moveTo(-w / 2, -9); g.lineTo(w / 2, -9); g.moveTo(-w / 2, 9); g.lineTo(w / 2, 9); g.stroke(); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 5; for (double q = -w / 2 + 12; q < w / 2; q += 26) { g.beginPath(); g.moveTo(q, -13); g.lineTo(q, 13); g.stroke(); }
					if (k > 0.6) { g.strokeStyle = $"rgba(255,255,255,{str((k - 0.6) * 2)})"; g.lineWidth = 2; g.beginPath(); g.moveTo(w / 2 - 4, -20); g.lineTo(w / 2 + 6, 0); g.lineTo(w / 2 - 6, 20); g.stroke(); }
				}
				else { g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(-w / 2 + 3, -15, w - 6, 30); g.shadowBlur = 0; g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; g.strokeRect(-w / 2 + 8, -9, w - 16, 18); }
				g.restore();
			}
			g.restore();
		};
		h.hit = (p, t) => { for (int i = 0; i < n; i++) { if (t < tb + P[i][3]) continue; var (x, yy, a) = st(i, t); if (yy > H + 60) continue; if (obbHit(p.x, p.y, p.r - 3, x, yy, w / 2 - 3, 15, a)) return true; } return false; };
		return h;
	}
	// the ride's gate (decoration): two arms go up
	static Hz L11Gate(double x, double y, double t0, double to, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.4, (t1 - t) / 0.4)); if (a <= 0) return; double op = L11ease((t - to) / 0.3) * 1.3; g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.5)"; g.fillRect(x - 8, y - 120, 16, 240);
			foreach (var s in new double[] { -1, 1 }) { g.save(); g.translate(x, y + s * 110); g.rotate(-s * op); g.fillStyle = GREY + "0.65)"; g.fillRect(0, -5, 170, 10); for (double q = 10; q < 170; q += 34) { g.fillStyle = "rgba(255,255,255,0.4)"; g.fillRect(q, -5, 14, 10); } g.restore(); }
			g.restore();
		};
		return h;
	}
	// the tunnel of love (decoration)
	static Hz L11Tunnel(double x, double y, double dir, double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1) h.dead = true; };
		h.draw = t => { double a = Math.Min(1, Math.Min((t - t0) / 0.5, (t1 - t) / 0.5)); if (a <= 0) return; g.save(); g.globalAlpha = a; g.fillStyle = GREY + "0.35)"; g.beginPath(); g.arc(x, y, 80, 0, TAU); g.fill(); g.fillStyle = "rgba(0,0,0,0.7)"; g.beginPath(); g.arc(x, y, 54, 0, TAU); g.fill(); g.fillStyle = "rgba(255,140,170,0.5)"; L11heartShape(x, y - 96, 26); g.fill(); g.restore(); };
		return h;
	}
	/* ---- the sea comes up and goes back: side 1 from below (phase 1), -1 from above (phase 2). Before it a dashed line shows how far it reaches ---- */
	public class L11TO { public double tilt; public bool frz; }
	static Hz L11Tide(double side, double xa, double xb, double hgt, double tw, double ta, double th, double te, L11TO o = null)
	{
		o ??= new L11TO();
		Func<double, double> TT = o.frz ? L11ft : (t => t); double tl = o.tilt;
		Func<double, double> amt = t => { t = TT(t); double A = TT(ta), Hh = TT(th), E = TT(te); return t < A ? 0 : t < A + 0.35 ? L11ease((t - A) / 0.35) : t < Hh ? 1 : Math.Max(0, 1 - L11ease((t - Hh) / (E - Hh))); };
		Func<double, double> ef = x => L11ease(Math.Min(xa <= 0 ? 1 : (x - xa) / 110, xb >= W ? 1 : (xb - x) / 110));
		Func<double, double, double, double> hh = (x, t, m) => ((hgt + tl * ((x - xa) / (xb - xa) - 0.5)) * m + JS.sin(x / 46 + TT(t) * 5) * 8 * m) * (0.12 + 0.88 * ef(x));
		Func<double, double, double, double> Y = (x, t, m) => side > 0 ? H - hh(x, t, m) : hh(x, t, m);
		void poly(double t, double m) { g.beginPath(); g.moveTo(xa, side > 0 ? H : 0); for (double x = xa; x <= xb; x += 16) g.lineTo(x, Y(x, t, m)); g.lineTo(xb, Y(xb, t, m)); g.lineTo(xb, side > 0 ? H : 0); g.closePath(); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta) { double k = L11wk(t, tw, ta); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.1 * k)})"; poly(t, 1); g.fill(); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); for (double x = xa; x <= xb; x += 16) { double y = Y(x, t, 1); if (x > xa) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke(); L11dash(false); return; }
			double m = amt(t); if (m <= 0.01) return; g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; poly(t, m); g.fill(); g.shadowBlur = 0;
			g.strokeStyle = "rgba(255,255,255,0.55)"; g.lineWidth = 4; g.beginPath(); for (double x = xa; x <= xb; x += 16) { double y = Y(x, t, m) + side * 4; if (x > xa) g.lineTo(x, y); else g.moveTo(x, y); } g.stroke();
			g.fillStyle = "rgba(255,255,255,0.35)"; for (double x = xa + 20; x < xb - 10; x += 58) { double y = Y(x, t, m) + side * (16 + JS.sin(x * 0.7) * 6); circle(x, y, 4 + JS.sin(x) * 1.5); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta + 0.05) return false; double m = amt(t); if (m < 0.3) return false; if (p.x < xa - p.r + 3 || p.x > xb + p.r - 3) return false; double y = Y(clamp(p.x, xa, xb), t, m); return side > 0 ? p.y + p.r - 3 > y : p.y - p.r + 3 < y; };
		return h;
	}
	/* ---- cotton candy (phase 3): a pink cloud grows on its stick, then bursts and goes ---- */
	public class L11CO { public double vx, vy, fly; }
	static Hz L11Candy(double x, double y, double r1, double tw, double ta, double tp, L11CO o = null)
	{
		o ??= new L11CO();
		double vx = o.vx, vy = o.vy; Func<double, (double, double)> P = t => t < tp ? (x, y) : (x + vx * (t - tp), y + vy * (t - tp)); double te = tp + o.fly + 0.35;
		Func<double, double> R = t => t < ta ? 0 : t < ta + 0.45 ? r1 * L11ease((t - ta) / 0.45) : t < te - 0.35 ? r1 * (1 + 0.04 * JS.sin((t - ta) * 9)) : r1 * Math.Max(0, (te - t) / 0.35);
		void puff(double cx, double cy, double r) { g.beginPath(); for (int i = 0; i < 9; i++) { double a = i * TAU / 9 + 0.3, q = r * 0.42; g.moveTo(cx + JS.cos(a) * r * 0.62 + q, cy + JS.sin(a) * r * 0.62); g.arc(cx + JS.cos(a) * r * 0.62, cy + JS.sin(a) * r * 0.62, q, 0, TAU); } g.moveTo(cx + r * 0.7, cy); g.arc(cx, cy, r * 0.7, 0, TAU); }
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			var (cx, cy) = P(t); g.save();
			if (o.fly == 0 || t < tp) { double a = Math.Min(1, (t - tw) / 0.3); g.globalAlpha = a * (t > te - 0.35 ? Math.Max(0, (te - t) / 0.35) : 1); g.strokeStyle = GREY + "0.6)"; g.lineWidth = 5; g.beginPath(); g.moveTo(x, y); g.lineTo(x, y + r1 + 50); g.stroke(); g.globalAlpha = 1; }
			if (t < ta) { double k = L11wk(t, tw, ta); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); g.arc(x, y, r1, 0, TAU); g.stroke(); L11dash(false); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.1 * k)})"; g.beginPath(); g.arc(x, y, r1 * k, 0, TAU); g.fill(); g.restore(); return; }
			double r = R(t); if (r < 1) { g.restore(); return; }
			g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; puff(cx, cy, r); g.fill(); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.3)"; puff(cx - r * 0.25, cy - r * 0.3, r * 0.35); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta + 0.1 || t > te - 0.25) return false; var (cx, cy) = P(t); return hypot(p.x - cx, p.y - cy) < R(t) * 0.88 + p.r - 3; };
		return h;
	}
	/* ---- beach towels (phase 2): they unroll on the sand from one end to the other, then roll up ---- */
	static Hz L11Towel(double x, double y, double len, double wd, double an, double tw, double ta, double te)
	{
		double c = JS.cos(an), s = JS.sin(an); Func<double, double> L = t => t < ta ? 0 : t < ta + 0.4 ? len * L11ease((t - ta) / 0.4) : t < te - 0.4 ? len : len * Math.Max(0, (te - t) / 0.4);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(x, y); g.rotate(an);
			if (t < ta) { double k = L11wk(t, tw, ta); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.strokeRect(0, -wd / 2, len, wd); L11dash(false); g.fillStyle = GREY + "0.6)"; g.beginPath(); g.ellipse(0, 0, 10, wd / 2, 0, 0, TAU); g.fill(); g.restore(); return; }
			double l = L(t); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(0, -wd / 2, l, wd); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.35)"; for (double q = 24; q < l - 10; q += 48) g.fillRect(q, -wd / 2, 14, wd);
			g.fillStyle = GREY + "0.75)"; g.beginPath(); g.ellipse(l, 0, 10, wd / 2, 0, 0, TAU); g.fill(); g.restore();
		};
		h.hit = (p, t) => { if (t < ta + 0.05 || t > te - 0.25) return false; double l = L(t); if (l < 8) return false; return obbHit(p.x, p.y, p.r - 3, x + c * l / 2, y + s * l / 2, l / 2, wd / 2, an); };
		return h;
	}
	/* ---- a water splash under the boardwalk (phase 3): a wave running along the ground from one side to the other ---- */
	static Hz L11Splash(double dir, double hgt, double w, double v, double tw, double t0)
	{
		Func<double, double> X = t => dir > 0 ? -w * 2 + v * (t - t0) : W + w * 2 - v * (t - t0); double te = t0 + (W + w * 4) / v;
		Func<double, double, double> hh = (x, t) => { double d = (x - X(t)) / w; return hgt * JS.exp(-d * d) * (1 + 0.08 * JS.sin(x / 30 + t * 9)); };
		var h = new Hz();
		h.update = (t, dt) => { if (t > te) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0)
			{
				double k = L11wk(t, tw, t0); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.5 * k)})"; g.lineWidth = 3; L11dash(true); g.beginPath(); g.moveTo(0, H - hgt); g.lineTo(W, H - hgt); g.stroke(); L11dash(false);
				g.fillStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; double ax = dir > 0 ? 40 : W - 40; g.beginPath(); g.moveTo(ax + dir * 22, H - hgt / 2); g.lineTo(ax - dir * 10, H - hgt / 2 - 18); g.lineTo(ax - dir * 10, H - hgt / 2 + 18); g.fill(); return;
			}
			double xc = X(t); g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.beginPath(); g.moveTo(xc - w * 2.6, H); for (double x = xc - w * 2.6; x <= xc + w * 2.6; x += 12) g.lineTo(x, H - hh(x, t)); g.lineTo(xc + w * 2.6, H); g.closePath(); g.fill(); g.shadowBlur = 0;
			g.fillStyle = "rgba(255,255,255,0.4)"; for (int i = -3; i <= 3; i++) { double x = xc + i * w * 0.45; circle(x, H - hh(x, t) + 14, 5); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < t0) return false; return p.y + p.r - 3 > H - hh(p.x, t); };
		return h;
	}
	static Hz L11Sched(List<(double t, Func<Hz> f)> L)
	{
		int i = -1; var h = new Hz();
		h.update = (t, dt) => { if (i < 0) { i = 0; while (i < L.Count && L[i].t < t - 0.05) i++; } while (i < L.Count && L[i].t <= t) { hazards.Add(L[i].f()); i++; } if (i >= L.Count) h.dead = true; };
		return h;
	}

	// options of a slingshot shot
	class L11ShO { public double? wn, v, r, gv, rr; public bool afk, end, dashAim; public double[] split, dashTo; public double ga, gw; public Action<double> onEnd; }

	/* ================= level 11: I Will Survive (Demi Lovato, the Angry Birds film) ================= */
	public static List<Ev> buildLevel11()
	{
		bool A2 = afkV == 2;
		var E = new List<Ev>(); var R = mulberry32(1111);
		void ev(double b, Action f) => E.Add(new Ev { b = b, f = f });
		void keep(double b, Action f) => E.Add(new Ev { b = b, keep = true, f = f });
		var SW = new List<(double wb, Func<Hz> f)>(); void sweep(double wb, Func<Hz> f) => SW.Add((wb, f));
		double K = easy ? 0.92 : 1, WN = easy ? 1.3 : 1;
		double PXp() => player != null ? player.x : W / 2; double PYp() => player != null ? player.y : H / 2;
		void fx(double b, double f2, double s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
		double aimX(double m = 60) => clamp(PXp(), m, W - m); double aimY(double m = 60) => clamp(PYp(), m, H - m);
		L11FZ = new List<double[]> { new[] { B(16), B(17.5) } };

		/* ---------- phase 1: the island at night, the slingshot (0 - 75) ---------- */
		var FORT_GONE = Enumerable.Repeat(1e9, L11FORT.Count + 1).ToArray();
		keep(0, () => hazards.Add(L11Night(B(0), B(77), FORT_GONE)));
		var SLS = new List<double[]>(); keep(0, () => hazards.Add(L11Sling(B(0), B(76), SLS)));
		double W1 = 2.2 * WN;
		// a shot: the bird leaves the slingshot on an arc through the target. tg null ('aim') = where you are at the warning
		void shot(double b, string kind, double[] tg, double up, L11ShO o = null)
		{
			o ??= new L11ShO();
			double wb = b - (o.wn ?? W1); bool off = o.afk && !A2; if (!off) SLS.Add(new[] { B(wb), B(b) });
			List<Hz> f()
			{
				double tx, ty; if (tg == null) { if (A2 || o.afk) { tx = aimX(10); ty = aimY(10); } else { tx = aimX(); ty = aimY(); } } else { tx = tg[0]; ty = tg[1]; }
				var P = L11Path(L11toss(L11SL[0], L11SL[1], tx, ty, up, o.gv ?? 900)); double v = (o.v ?? (kind == "big" ? 380 : kind == "blue" ? 560 : 600)) * K, r = o.r ?? (kind == "big" ? 56 : kind == "blue" ? 15 : kind == "white" ? 24 : 22);
				var bas = new L11RO { tw = B(wb), t0 = B(b), v = v, r = r, kind = kind, frz = true };
				double sDef() => P.near(tx, ty) * 0.62;
				if (kind == "blue")
				{
					double s0 = o.split != null ? P.near(o.split) : sDef(); var (sx, sy, sa) = P.at(s0); var q0 = bas.Clone(); q0.end = s0; var o2 = new List<Hz> { L11Ride(P, q0) }; double dt = s0 / v;
					foreach (var da in new[] { -0.32, 0, 0.32 }) { double a = sa + da; var P2 = L11seg(sx, sy).line(sx + JS.cos(a) * 1700, sy + JS.sin(a) * 1700).done(); var q = bas.Clone(); q.t0 = B(b) + dt; q.r = 15; o2.Add(L11Ride(P2, q)); }
					return o2;
				}
				if (kind == "yellow")
				{
					double s0 = o.split != null ? P.near(o.split) : sDef(); var (sx, sy, sa) = P.at(s0); double a = sa;
					if (o.dashAim || o.dashTo != null) { double dx, dy; if (o.dashAim) { dx = aimX(); dy = aimY(); } else { dx = o.dashTo[0]; dy = o.dashTo[1]; } a = JS.atan2(dy - sy, dx - sx); }
					int fi = -1; for (int i = 0; i < P.pts.Count; i++) if (P.L[i] >= s0) { fi = i; break; }
					var pts = P.pts.GetRange(0, fi + 1); var P2 = L11seg(pts[0][0], pts[0][1]).add(pts.Skip(1)).line(sx + JS.cos(a) * 1800, sy + JS.sin(a) * 1800).done();
					var q = bas.Clone(); q.dashAt = P2.near(sx, sy); q.v2 = 1150 * K; return new List<Hz> { L11Ride(P2, q) };
				}
				if (kind == "black")
				{
					double s0 = P.near(tx, ty), bt0 = B(b) + s0 / v, rr = o.rr ?? (easy ? 140 : 170); var q = bas.Clone(); q.end = s0; double tx2 = tx, ty2 = ty; q.onEnd = _ => { burst(tx2, ty2, PINK, 14, 320); shake = Math.Max(shake, 8); };
					return new List<Hz> { L11Ride(P, q), L11Ring(tx, ty, bt0, bt0 + 0.75, 20, rr, 30, o.ga, o.gw, new L11RgO { tw = B(wb), pr = rr }) };
				}
				{ var q = bas.Clone(); q.end = o.end ? P.near(tx, ty) : null; q.onEnd = o.onEnd; return new List<Hz> { L11Ride(P, q) }; }
			}
			ev(wb, () => { if (off) return; foreach (var h in f()) hazards.Add(h); });
		}
		// the bird hits the fort: blocks fall
		void knock(double b, int[] idx, string kind = "red")
		{
			var tg = new double[] { 1120, L11G - 200 };
			shot(b, kind, tg, 160, new L11ShO
			{
				end = true,
				onEnd = t => { foreach (var i in idx) { FORT_GONE[i] = t; double x = L11FORT[i][0], y = L11FORT[i][1], w = L11FORT[i][2], hh = L11FORT[i][3]; double vx = -180 - rnd() * 200, vy = -300 - rnd() * 200, va = (rnd() - 0.5) * 6; hazards.Add(L11Block(x, y, w, hh, vx, vy, va, t)); } burst(tg[0], tg[1], PINK, 14, 320); shake = Math.Max(shake, 8); }
			});
		}
		double[] P2_(double x, double y) => new[] { x, y };
		// the intro: 3 shots that teach the rule (top, bottom, middle), then at you
		shot(3, "red", P2_(760, 150), 120); shot(5, "red", P2_(700, 560), 60); shot(7, "red", P2_(640, 360), 140); shot(9, "red", null, 120, new L11ShO { r = 30, afk = true }); if (!easy) shot(10, "red", P2_(900, 200), 60);
		// 12 "At first I was afraid": the bird hits the fort
		knock(12, new[] { 7, 8 }); shot(13, "red", null, 100, new L11ShO { r = 30, afk = true });
		// 14 3 birds in the air, and 16 "petrified": everything freezes a moment, then goes on
		shot(15, "red", P2_(420, 260), 160); shot(15.5, "red", P2_(760, 420), 120); shot(16, "red", P2_(1040, 240), 200);
		shot(18.5, "red", null, 120, new L11ShO { r = 30, afk = true });
		// 20 two birds side by side on two paths, you pass between them
		void pair(double b, double tx, double ty, double up, double d) { shot(b, "red", P2_(tx, ty - d / 2), up); shot(b, "red", P2_(tx, ty + d / 2), up); }
		pair(20, 660, 300, 120, 190); pair(22, 760, 420, 60, 190); pair(24, 560, 240, 160, 200); shot(26, "black", null, 120);
		// 28 Blues splits in 3
		shot(28, "blue", P2_(620, 300), 140, new L11ShO { split = P2_(520, 260) }); if (!easy) shot(30, "blue", null, 120); shot(32, "blue", P2_(700, 460), 80, new L11ShO { split = P2_(560, 380) }); if (!easy) shot(33, "red", null, 100); shot(34, "blue", null, 140);
		// 36 "And I grew strong": big Terence
		shot(36, "big", P2_(640, 380), 90, new L11ShO { wn = 2.8 * WN }); shot(38, "black", null, 140);
		// 40 Chuck speeds up and cuts the screen at you
		shot(40, "yellow", P2_(460, 260), 140, new L11ShO { split = P2_(420, 250), dashAim = true }); if (!easy) shot(41.5, "yellow", P2_(520, 420), 60, new L11ShO { split = P2_(430, 420), dashAim = true }); shot(43, "yellow", P2_(600, 200), 120, new L11ShO { split = P2_(480, 200), dashAim = true });
		// 44 "And so you're back from outer space": space and planets, birds orbit them
		var PL = new[] { new double[] { 430, 250, 52, 118 }, new double[] { 880, 400, 66, 150 } };
		ev(44, () => { double a0 = PL[0][0], b0 = PL[0][1], c0 = PL[0][2], d0 = PL[0][3], a1 = PL[1][0], b1 = PL[1][1], c1 = PL[1][2], d1 = PL[1][3], e = easy ? 0.45 : 1; hazards.Add(L11Planet(a0, b0, c0 * e, d0, B(44), B(70), 0)); hazards.Add(L11Planet(a1, b1, c1 * e, d1, B(44.5), B(70), 1)); });
		void orbit(double b, int pi, double a0, double da, string kind = "red")
		{
			double wb = b - W1; SLS.Add(new[] { B(wb), B(b) });
			ev(wb, () =>
			{
				double cx = PL[pi][0], cy = PL[pi][1], Ro = PL[pi][3], ex = cx + JS.cos(a0) * Ro, ey = cy + JS.sin(a0) * Ro, a1 = a0 + da, dr = da > 0 ? 1 : -1, ox = JS.cos(a1) * Ro + cx, oy = JS.sin(a1) * Ro + cy, tx = -JS.sin(a1) * dr, ty = JS.cos(a1) * dr;
				var P = L11seg(L11SL[0], L11SL[1]).line(ex, ey).arc(cx, cy, Ro, a0, a1).line(ox + tx * 1800, oy + ty * 1800).done(); hazards.Add(L11Ride(P, new L11RO { tw = B(wb), t0 = B(b), v = 560 * K, r = 22, kind = kind, frz = true }));
			});
		}
		orbit(46, 0, Math.PI * 0.75, Math.PI * 1.6); orbit(48, 1, Math.PI * 0.95, Math.PI * 1.4); orbit(50, 0, Math.PI * 0.85, -Math.PI * 1.7); if (!easy) orbit(52, 1, Math.PI * 0.8, -Math.PI * 1.5, "blue"); shot(54, "red", null, 120);
		// 56 "that sad look upon your face": a big pig in the middle, its frown sweeps the screen (down, then up)
		ev(54, () => hazards.Add(L11SadPig(B(54), B(61))));
		sweep(54.5, () => L11Frown(1, 380, easy ? 470 : 190, B(54.5), B(56), B(58))); if (!easy) sweep(56.5, () => L11Frown(-1, 940, easy ? 340 : 190, B(56.5), B(58), B(60)));
		// 60 "changed that stupid lock / leave your key": a lock closes in the middle, keys fly like boomerangs
		ev(58, () => hazards.Add(L11Lock(W / 2, 430, B(58), B(60), B(61), B(67))));
		void key(double b, double y, double ry, double dir)
		{
			double wb = b - W1; SLS.Add(new[] { B(wb), B(b) });
			ev(wb, () => { const double rx = 540; double cx = 70 + rx; var P = L11seg(L11SL[0], L11SL[1]).line(70, y).fn(u => { double q = Math.PI + dir * TAU * u; return new[] { cx + JS.cos(q) * rx, y + JS.sin(q) * ry }; }, 160).done(); hazards.Add(L11Ride(P, new L11RO { tw = B(wb), t0 = B(b), v = 620 * K, r = 26, kind = "key", frz = true, end = P.len })); });
		}
		key(61.5, 250, 150, 1); key(63.5, 560, 150, -1); if (!easy) { key(65, 180, 110, 1); key(66, 620, 110, -1); }
		if (!easy) shot(64.5, "red", null, 120); shot(67, "black", null, 100); shot(70, "red", null, 140); if (!easy) shot(73, "red", null, 100);
		// 68 Matilda flies at the top and drops eggs one after another
		void matilda(double b0, double b1, double dir, double[] xs)
		{
			ev(b0 - W1, () =>
			{
				const double y = 92; double x0 = dir > 0 ? -60 : W + 60, x1 = dir > 0 ? W + 60 : -60; var P = L11seg(x0, y).line(x1, y).done(); double v = (W + 120) / (B(b1) - B(b0)); hazards.Add(L11Ride(P, new L11RO { tw = B(b0 - W1), t0 = B(b0), v = v, r = 24, kind = "white", pv = "none" }));
				foreach (var x in xs) { double td = B(b0) + Math.Abs(x - x0) / v; hazards.Add(L11Egg(x, y, td - 1.1 * WN, td)); }
			});
		}
		var EX1 = easy ? new double[] { 200, 520, 840, 1160 } : new double[] { 150, 330, 510, 690, 870, 1050, 1230 }; var EX2 = easy ? new double[] { 1080, 760, 440, 120 } : new double[] { 1130, 950, 770, 590, 410, 230, 50 };
		matilda(68, 72, 1, EX1); matilda(72, 76, -1, EX2);
		// the sea at night: a wave comes up from below and goes back, each time somewhere else
		void tide(double side, double b, double xa, double xb, double hgt, double hold = 1.2, L11TO o = null) { double wb = b - 2 * WN; ev(wb, () => hazards.Add(L11Tide(side, xa, xb, hgt * (easy ? 0.78 : 1), B(wb), B(b), B(b + hold * (easy ? 0.8 : 1)), B(b + hold * (easy ? 0.8 : 1) + 0.8), o))); }
		(double b, double xa, double xb, double h, L11TO o) TD(double b, double xa, double xb, double h, L11TO o = null) => (b, xa, xb, h, o);
		var TD1 = new[] { TD(1.5,0,640,110),TD(4,640,1280,120),TD(6,0,640,130),TD(8,640,1280,130),TD(10.5,320,960,110),TD(12.5,0,560,140,new L11TO{tilt=-60}),TD(14,720,1280,140,new L11TO{tilt=60}),TD(15.6,0,640,120,new L11TO{frz=true}),TD(17.8,640,1280,120,new L11TO{frz=true}),
			TD(19,0,1280,70),TD(21,640,1280,140),TD(23,0,640,140),TD(25,320,960,120),TD(27,0,1280,75),TD(29,0,600,150,new L11TO{tilt=80}),TD(31,680,1280,150,new L11TO{tilt=-80}),TD(33,0,500,130),TD(33,780,1280,130),TD(35,320,960,140),TD(37,0,1280,80),TD(39,640,1280,150),TD(41,0,640,150),TD(43,240,1040,110),
			TD(70,0,640,120),TD(71.5,640,1280,120),TD(73,0,1280,80),TD(74.5,0,1280,60) };
		for (int i = 0; i < TD1.Length; i++) { var q = TD1[i]; if (!easy || i % 2 == 0) tide(1, q.b, q.xa, q.xb, q.h + 25, 1.2, q.o ?? new L11TO()); }
		// space: comets come back "back from outer space", moving on a dotted line like the birds
		void comet(double b, double x0, double y0, double xm, double ym, double x1, double y1, double r = 30)
		{
			double wb = b - 2.2 * WN;
			ev(wb, () => { var P = L11seg(x0, y0).fn(u => new[] { (1 - u) * (1 - u) * x0 + 2 * u * (1 - u) * xm + u * u * x1, (1 - u) * (1 - u) * y0 + 2 * u * (1 - u) * ym + u * u * y1 }, 80).done(); hazards.Add(L11Ride(P, new L11RO { tw = B(wb), t0 = B(b), v = 400 * K, r = easy ? 24 : r, kind = "comet", frz = true })); });
		}
		comet(45, -80, 130, 640, 40, W + 200, 330); if (!easy) comet(47, W + 80, 600, 640, 700, -200, 420); if (!easy) comet(51, -80, 620, 500, 560, W + 200, 120); comet(53, W + 80, 110, 800, 60, -200, 560);
		comet(56, -80, 360, 640, 300, W + 200, 360, 26); if (!easy) comet(62.5, W + 80, 140, 640, 40, -200, 200); comet(65.5, -80, 620, 640, 680, W + 200, 580); comet(68.5, W + 80, 330, 640, 240, -200, 330);
		// shots at you on a fixed table (every player at the same time), so there is no spot to sit in long
		foreach (var b in new[] { 5.5, 22.5, 31, 35, 39.5 }) shot(b, "red", null, 110, new L11ShO { r = 32, afk = true });
		foreach (var b in new[] { 47, 50.5, 57.5, 61, 63 }) shot(b, "red", null, 110, new L11ShO { r = 32, afk = true });
		// 74 the whole fort falls, and the morning comes
		ev(73.5, () => { double t = B(73.5); for (int i = 0; i < L11FORT.Count; i++) { if (FORT_GONE[i] < t) continue; FORT_GONE[i] = t; double x = L11FORT[i][0], y = L11FORT[i][1], w = L11FORT[i][2], hh = L11FORT[i][3]; double vx = -120 - rnd() * 260, vy = -200 - rnd() * 300, va = (rnd() - 0.5) * 7; hazards.Add(L11Block(x, y, w, hh, vx, vy, va, t)); } FORT_GONE[L11FORT.Count] = t; burst(1120, L11G - 200, PINK, 24, 400); });
		fx(73.5, 0.3, 12);
		fx(76, 0.5, 10);

		/* ---------- phase 2: a day at the sea from above (76 - 171) ---------- */
		keep(74, () => hazards.Add(L11Beach(B(74), B(172))));
		double W2 = 2.2 * WN;
		// x null = 'aim'
		void drop(double b, double? x, double y, double r, string kind, L11DO o = null)
		{
			o ??= new L11DO(); double wb = b - (o.wn ?? W2);
			ev(wb, () => { if (x == null && !A2) return; double px, py; if (x == null) { px = clamp(PXp(), 10, W - 10); py = clamp(PYp(), 10, H - 10); } else { px = x.Value; py = y; } hazards.Add(L11Drop(px, py, r, B(wb), B(b), kind, o)); });
		}
		// 76 "Go on now, go, walk out the door": the hut's door opens and sunlight comes out
		ev(74.5, () => hazards.Add(L11Hut(W / 2, B(74.5), B(77), B(79), new[] { Math.PI * 0.2, Math.PI * 0.4, Math.PI * 0.6, Math.PI * 0.8 })));
		ev(78, () => hazards.Add(L11Hut(W / 2, B(78), B(80), B(82), new[] { Math.PI * 0.12, Math.PI * 0.3, Math.PI * 0.5, Math.PI * 0.7, Math.PI * 0.88 }[(easy ? 1 : 0)..(easy ? 4 : 5)])));
		// 80 beach balls bounce on the beat (each bounce's shadow shows before it)
		void ball(double b0, double y0, double y1, double dir, int nb, double r = 34, double bb = 1)
		{
			ev(b0 - 1.5 * WN, () => { var pts = new List<double[]>(); var ts = new List<double>(); for (int i = 0; i < nb; i++) { double u = (i + 0.5) / nb; pts.Add(new[] { dir > 0 ? u * W : W - u * W, y0 + (y1 - y0) * u }); ts.Add(B(b0 + i * bb)); } hazards.Add(L11Ball(pts, ts, r, B(b0 - 1.5 * WN))); });
		}
		ball(80, 220, 300, 1, 8); ball(81, 520, 440, -1, 8); if (!easy) { ball(82.5, 380, 380, 1, 8); }
		// 84 "just turn around now": crabs walk sideways, turn and come back
		void crab(double b, double x0, double[] ys, double v, double[] turns, double te) => ev(b - 1.5 * WN, () => hazards.Add(L11Crabs(x0, ys, v * K, B(b - 1.5 * WN), B(b), turns.Select(B).ToArray(), B(te))));
		var CY = new[] { L11SEA + 50, L11SEA + 130, L11SEA + 210, L11SEA + 290, L11SEA + 370, L11SEA + 450, L11SEA + 530 };
		crab(84, -40, CY.Where((y, i) => i != 4).ToArray(), 380, new[] { 86.5 }, 90); crab(86, W + 40, CY.Where((y, i) => i != 1).ToArray(), 380, new[] { 88.5 }, 92);
		drop(88, null, 0, 46, "ball");
		crab(90, -40, CY.Where((y, i) => i != 2 && i != 3).ToArray(), 420, new double[0], 94);
		// 92 "tried to hurt me with goodbye": a boat waves goodbye, its V wake opens behind it
		sweep(91, () => L11Wake(1, 330 * K, B(91), B(92), B(99), 0.62)); drop(94, null, 0, 46, "ball"); sweep(95, () => L11Wake(-1, 330 * K, B(95), B(96), B(103), 0.62)); drop(98, null, 0, 46, "ball");
		// 100 "Did you think I'd crumble": a sand castle crumbles, its towers fall outward one after another (like a clock hand)
		ev(98.5, () => { int n = easy ? 6 : 8; var f = new List<double[]>(); for (int i = 0; i < n; i++) f.Add(new[] { -Math.PI / 2 + i * TAU / n, B(100 + i * 0.5) }); hazards.Add(L11Castle(W / 2, 410, B(98.5), B(100), B(105), f, easy ? 230 : 360, 46)); });
		// 104 "lay down and die": beach umbrellas fall one after another (dominoes)
		ev(102.5, () => { hazards.Add(L11Umbrellas(new double[] { 90, 330, 570, 810, 1050 }, 200, 1, B(102.5), new[] { 104, 104.5, 105, 105.5, 106 }.Select(B).ToArray(), B(108))); if (!easy) hazards.Add(L11Umbrellas(new double[] { 1190, 950, 710, 470, 230 }, 620, -1, B(103), new[] { 105, 105.5, 106, 106.5, 107 }.Select(B).ToArray(), B(108.5))); });
		// 108 "Oh no, not I, I will survive": a big wave with a tunnel inside, go in and out with it
		sweep(106, () => L11Surf(B(106), B(108), B(112), 260, 900, easy ? 430 : 210)); if (!easy) sweep(110, () => L11Surf(B(110), B(112), B(116), 1040, 360, easy ? 430 : 210));
		// 116 the lifeguard throws buoys (the inside of a buoy doesn't hurt), they bounce and roll
		ev(114, () => hazards.Add(L11Guard(1180, L11SEA + 50, B(114), B(126))));
		void buoy(double b, double? x, double y, double vx, double vy) => drop(b, x, y, 48, "buoy", new L11DO { stay = 0.6, roll = true, vx = vx * K, vy = vy * K });
		buoy(116, null, 0, -260, 90); buoy(117, 300, 500, 240, -60); buoy(118, null, 0, -260, -90); buoy(119, 900, 300, -200, 120); buoy(120, null, 0, 260, 0); if (!easy) { buoy(121, 600, 600, -240, -120); buoy(122, null, 0, 0, -200); }
		// 124 ice cream falls on the cone, piles up, and melts
		var CN = new[] { new double[] { 250, 300 }, new double[] { 640, 240 }, new double[] { 1030, 300 }, new double[] { 420, 560 }, new double[] { 860, 560 } };
		ev(122, () => { foreach (var q in CN) hazards.Add(L11Cone(q[0], q[1], B(122), B(134))); });
		var ICE = new[] { (124, 0), (124.5, 2), (125, 1), (125.5, 3), (126, 4), (127, 0), (127.5, 4), (128, 2), (128.5, 1), (129, 3) };
		for (int i = 0; i < ICE.Length; i++) { var (b, k) = ICE[i]; if (easy && i % 2 != 0) continue; double x = CN[k][0], y = CN[k][1]; drop(b, x, y - 26, 32, "scoop", new L11DO { stay = (130 - b) * SPB }); }
		drop(126.5, null, 0, 34, "scoop"); drop(129.5, null, 0, 34, "scoop");
		foreach (var q in CN) { double x = q[0], y = q[1]; ev(129, () => hazards.Add(L11Melt(x, y, easy ? 66 : 110, B(129), B(130), B(133.5)))); }
		// 132 volleyball: balls go back and forth over the net, and a smash on "hey hey"
		ev(130, () => hazards.Add(L11Net(B(130), B(140))));
		void lob(double b0, double dir, double y0, double y1, int n) { ev(b0 - 1.5 * WN, () => { var pts = new List<double[]>(); var ts = new List<double>(); for (int i = 0; i < n; i++) { double u = (double)(i + 1) / (n + 1); pts.Add(new[] { dir > 0 ? u * W : W - u * W, y0 + (y1 - y0) * u }); ts.Add(B(b0 + i * 1)); } hazards.Add(L11Ball(pts, ts, 36, B(b0 - 1.5 * WN))); }); }
		lob(132, 1, 250, 550, 4); lob(133, -1, 560, 240, 4);
		void smash(double b) { double wb = b - 2 * WN; ev(wb, () => { double x = aimX(), y = clamp(PYp(), L11SEA + 50, H - 50), t = B(b); hazards.Add(L11Drop(x, y, 44, B(wb), t, "ball", new L11DO { stay = 0.1 })); hazards.Add(L11Ring(x, y, t, t + 0.8, 40, easy ? 220 : 260, 24, 0, 0)); }); }
		smash(136); smash(138);
		// 140 the music without singing: kites dance in the sky with their tails, the shape changes every 8 beats
		void kite(double b0, double b1, Func<double, double[]> f) => ev(b0 - 2, () => hazards.Add(L11Kite(t => f((t - B(b0)) / SPB), B(b0 - 2), B(b0), B(b1))));
		var KS = new (double b0, double b1, Func<double, double, double, Func<double, double[]>> mk)[] {
			(140, 148, (cx, cy, ph) => u => new[] { cx + JS.sin(u * Math.PI / 4 + ph) * 300, cy + JS.sin(u * Math.PI / 2 + ph * 2) * 110 }),
			(148, 156, (cx, cy, ph) => u => new[] { cx + JS.cos(u * Math.PI / 4 + ph) * 200, cy + JS.sin(u * Math.PI / 4 + ph) * 200 }),
			(156, 164, (cx, cy, ph) => u => new[] { cx + JS.sin(u * Math.PI / 2 + ph * 2) * 160, cy + JS.sin(u * Math.PI / 4 + ph) * 230 }) };
		var KC = new[] { new[] { new double[] { 400, 260, 0 }, new double[] { 880, 500, Math.PI } }, new[] { new double[] { 360, 380, 0 }, new double[] { 920, 380, Math.PI } }, new[] { new double[] { 300, 400, 0 }, new double[] { 980, 400, Math.PI } } };
		for (int k = 0; k < KS.Length; k++) { var (b0, b1, mk) = KS[k]; foreach (var q in KC[k].Take(easy ? 1 : 2)) kite(b0, b1 + 0.5, mk(q[0], q[1], q[2])); }
		// between the kites: bouncing balls cross the screen (so there is no empty spot)
		ball(144, 180, 560, 1, 8, 32); ball(152, 560, 180, -1, 8, 32); ball(160, 300, 300, 1, 8, 32);
		foreach (var b in new double[] { 146, 150, 154, 158, 162 }) drop(b, null, 0, easy ? 54 : 46, "ball");
		// 164 the sun sets, the kites fly away, and the last wave
		sweep(164, () => L11Surf(B(164), B(166), B(170), 640, 200, easy ? 430 : 220));
		// the tide: the sea at the top comes up on the sand and goes back, each time on a side
		var TD2 = new[] { TD(83,0,640,240),TD(85,640,1280,240),TD(87,320,960,220),TD(89,0,1280,150,new L11TO{tilt=120}),TD(93.5,0,600,230),TD(96.5,680,1280,230),TD(100.5,0,1280,150),
			TD(117,0,640,240),TD(119,640,1280,240),TD(121,0,1280,160,new L11TO{tilt=-110}),TD(123,0,1280,150),TD(127.5,700,1280,200),TD(131,0,560,230),TD(134.5,720,1280,230),TD(137,280,1000,200),TD(150.5,0,640,220),TD(158.5,640,1280,220),TD(168,0,1280,170) };
		for (int i = 0; i < TD2.Length; i++) { var q = TD2[i]; if (!easy || i % 2 == 0) tide(-1, q.b, q.xa, q.xb, q.h, 1.2, q.o ?? new L11TO()); }
		// between the kites: towels unroll on the sand, the shape changes
		int TI = 0;
		void towel(double b, double[][] L, double hold = 2.5)
		{
			if (easy && (TI++) % 2 != 0) return; if (easy) { var L0 = L; L = L0.Where((q, i) => L0.Length < 2 || i != 1).ToArray(); hold = 1.9; }
			double wb = b - 2 * WN; ev(wb, () => { foreach (var q in L) hazards.Add(L11Towel(q[0], q[1], q[2], easy ? 58 : 72, q[3], B(wb), B(b), B(b + hold))); });
		}
		const double PI = Math.PI;
		double[] T4(double a, double b, double c, double d) => new[] { a, b, c, d };
		towel(141, new[] { T4(80, 620, 300, 0), T4(520, 620, 300, 0), T4(960, 620, 300, 0) }); towel(143, new[] { T4(200, 230, 250, PI / 2), T4(640, 230, 250, PI / 2), T4(1080, 230, 250, PI / 2) });
		towel(147, new[] { T4(90, 190, 380, 0.42), T4(700, 190, 380, 0.42) }); towel(149, new[] { T4(1200, 620, 300, PI), T4(760, 620, 300, PI), T4(320, 620, 300, PI) });
		towel(153, new[] { T4(430, 690, 300, -PI / 2), T4(850, 690, 300, -PI / 2) }); towel(155, new[] { T4(60, 400, 470, 0), T4(1220, 400, 470, PI) });
		towel(161, new[] { T4(1190, 200, 380, PI - 0.42), T4(580, 200, 380, PI - 0.42) }); towel(163, new[] { T4(100, 640, 1080, 0) }, 1.6);
		// balls and ice cream falling on you on a fixed table
		void dropAim(double b, double r, string kind) { double wb = b - W2; ev(wb, () => hazards.Add(L11Drop(clamp(PXp(), 10, W - 10), clamp(PYp(), 10, H - 10), r, B(wb), B(b), kind, new L11DO()))); }
		foreach (var b in new double[] { 80, 84, 102, 106.5, 110.5, 113.5, 124, 132, 142, 166, 169 }.Concat(easy ? new double[] { 92.5, 97 } : new double[0])) if (!easy || !new[] { 106.5, 113.5, 132 }.Contains(b)) dropAim(b, 50, b >= 122 && b < 130 ? "scoop" : "ball");
		fx(172, 0.45, 12);

		/* ---------- phase 3: the fair at night, the coaster boss (172 - 304.6) ---------- */
		keep(168, () => hazards.Add(L11Fair(B(168), B(305))));
		double W3 = 2.6 * WN;
		// a train on a track: the track is drawn first, then the train runs on it
		var ESK = new[] { 184.5, 200, 222, 226, 237, 245, 248.5, 255, 268.5, 273, 277, 290 };
		void train(double b, Func<L11P> mk, int n = 0, double v = 0, bool sw = false, double? wn = null, List<double[]> gaps = null)
		{
			if (easy && ESK.Contains(b)) return; if (easy) n = (int)Math.Max(2, round((n != 0 ? n : 4) * 0.5)); double wb = b - (wn ?? W3);
			void f() { var P = mk(); hazards.Add(L11Ride(P, new L11RO { tw = B(wb), t0 = B(b), v = (v != 0 ? v : 520) * K, r = 20, kind = "car", pv = "track", n = n != 0 ? n : 4, sp = 52, gaps = gaps })); }
			if (sw) sweep(wb, () => { var P = mk(); return L11Ride(P, new L11RO { tw = B(wb), t0 = B(b), v = (v != 0 ? v : 520) * K, r = 20, kind = "car", pv = "track", n = n != 0 ? n : 4, sp = 52, gaps = gaps }); }); else ev(wb, f);
		}
		Func<L11P> straight(double y0, double y1, double dir) => () => L11seg(dir > 0 ? -80 : W + 80, y0).line(dir > 0 ? W + 80 : -80, y1).done();
		// 172 "It took all the strength I had": the strength tester, the bell shoots a ring with an opening
		var SX = new double[] { 320, 640, 960 };
		ev(170, () => { for (int k = 0; k < 3; k++) { var sl = new List<double>(); foreach (var b in new[] { 172 + k * 1.5, 178 + k * 1.5 }) sl.Add(B(b)); hazards.Add(L11Striker(SX[k], B(170), B(184), sl.ToArray())); } });
		for (int k = 0; k < 3; k++) foreach (var b in new[] { 172 + k * 1.5, 178 + k * 1.5 }) { if (easy && k == 1) continue; int k2 = k; double tr = B(b) + 0.45 * 1; ev(b, () => { double ga = Math.PI / 2 + (R() - 0.5) * 1.6; hazards.Add(L11Ring(SX[k2], 100, tr, tr + 3.2 * (1 / K), 30, 1500, 22, ga, easy ? 1.5 : 0.8, new L11RgO { lin = true })); }); }
		// 176 "not to fall apart": the drop tower: the seat goes up and falls, sparks run on the ground
		var TWX = new double[] { 220, 1060, 640 };
		for (int k = 0; k < TWX.Length; k++) { double x = TWX[k]; if (easy && k == 2) continue; double b = 176 + k * 1.2; ev(b - 1, () => hazards.Add(L11Tower(x, B(b - 1), B(b), B(b + 2), B(b + 2.6), B(b + 6), easy ? 110 : 150))); }
		// 180 "mend the pieces of my broken heart": the first ride: the track draws a big heart
		train(181, () => L11heartPath(W / 2, 370, 250, Math.PI, 1), v: 560, n: 5); train(184.5, () => L11heartPath(W / 2, 370, 150, Math.PI, -1), v: 400, n: 3);
		// 188 the pirate ship swings, higher every time
		ev(186, () => hazards.Add(L11Ship(B(186), B(188), B(196.5), easy ? new[] { 0.2, 0.3, 0.38, 0.42 } : new[] { 0.35, 0.55, 0.75, 0.85 }, 4 * SPB)));
		// 196 "now I hold my head up high": the train goes up a big loop
		Func<L11P> loop(double y, double cx, double R2, double dir) => () => { var s = L11seg(dir > 0 ? -80 : W + 80, y); s.line(cx, y); s.arc(cx, y - R2, R2, Math.PI / 2, Math.PI / 2 - dir * TAU); s.line(dir > 0 ? W + 80 : -80, y); return s.done(); };
		train(196, loop(620, 640, 250, 1), v: 600, n: 5, sw: true); train(200, loop(560, 420, 190, -1), v: 600, n: 4);
		// 204 "And you see me, somebody new": the teacups spin
		ev(202, () => { hazards.Add(L11Cups(360, 380, 90, 170, easy ? 2 : 4, 36, 1.3 * K, B(202), B(204), B(212))); hazards.Add(L11Cups(920, 380, 90, 170, easy ? 2 : 4, 36, -1.3 * K, B(202.5), B(204.5), B(212))); });
		train(207, straight(130, 130, -1), v: 700, n: 6, sw: true); train(210, straight(640, 640, 1), v: 700, n: 6);
		// 212 "I'm not that chained up little person": the lift chain, it breaks and falls
		if (!easy) sweep(210.5, () => L11Chain(-40, H + 20, W + 40, -20, B(210.5), B(212), B(216), B(219.5), 160 * K));
		// 220 "felt like dropping in": the train goes down a steep drop
		Func<L11P> dropT(double x0, double dir) => () => L11seg(x0, -80).fn(u => new[] { x0 + dir * u * 460, -80 + (H - 150 + 80) * JS.sin(u * Math.PI / 2) }, 60).line(x0 + dir * 1500, H - 150).done();
		train(220, dropT(140, 1), v: 760, n: 5); train(222, dropT(W - 140, -1), v: 760, n: 5);
		// 224 "expect me to be free": the train flies off the ramp and lands on the other track
		Func<L11P> jump(double y, double dir) => () =>
		{
			var s = L11seg(dir > 0 ? -80 : W + 80, y); double xr = dir > 0 ? 470 : W - 470; s.line(xr, y); s.arc(xr, y - 180, 180, Math.PI / 2, Math.PI / 2 - dir * 0.55); double jx = xr + dir * JS.sin(0.55) * 180, jy = y - 180 + JS.cos(0.55) * 180;
			var fl = new List<double[]>(); for (double t2 = 0.01; t2 < 3; t2 += 0.01) { double x = jx + dir * 480 * t2, yy = jy - 260 * t2 + 0.5 * 900 * t2 * t2; if (yy >= y) { fl.Add(new[] { x, y }); break; } fl.Add(new[] { x, yy }); }
			s.add(fl); s.line(dir > 0 ? W + 80 : -80, y); return s.done();
		};
		List<double[]> JP(double y, double dir) { var P = jump(y, dir)(); double xr = dir > 0 ? 470 : W - 470; double s0 = P.near(xr + dir * JS.sin(0.55) * 180, y - 180 + JS.cos(0.55) * 180); double s1 = s0; for (double s = s0; s < P.len; s += 6) { if (P.at(s).y >= y - 1) { s1 = s; break; } } return new List<double[]> { new[] { s0, s1 } }; }
		train(224, jump(600, 1), v: 620, n: 4, gaps: JP(600, 1)); train(226, jump(320, -1), v: 620, n: 4, gaps: JP(320, -1));
		// 228 "saving all my loving": the tunnel of love: heart boats come out of the tunnel in a row
		ev(226, () => { hazards.Add(L11Tunnel(0, 250, 1, B(226), B(236))); hazards.Add(L11Tunnel(W, 470, -1, B(226.5), B(236))); });
		void boats(double b, double y, double dir, double ph) => ev(b - 2.4 * WN, () => { var P = L11seg(dir > 0 ? -40 : W + 40, y).fn(u => new[] { dir > 0 ? -40 + u * (W + 80) : W + 40 - u * (W + 80), y + JS.sin(u * TAU * 1.5 + ph) * 120 }, 120).done(); hazards.Add(L11Ride(P, new L11RO { tw = B(b - 2.4 * WN), t0 = B(b), v = 300 * K, r = 24, kind = "heart", pv = "water", n = easy ? 4 : 8, sp = 76 })); });
		boats(228, 250, 1, 0); boats(229, 470, -1, Math.PI);
		// 236 the last chorus: the train comes back in a new shape on every line
		// "Go on now, go": the gate opens and the train comes out of it
		ev(234, () => hazards.Add(L11Gate(W / 2, 360, B(234), B(236), B(242))));
		train(236, straight(250, 250, 1), v: 780, n: 6, wn: 2.4); train(237, straight(470, 470, -1), v: 780, n: 6, wn: 2.4);
		// "walk out the door": a zigzag track
		train(240, () => L11seg(-80, 140).fn(u => new[] { -80 + u * (W + 160), 140 + Math.Abs(((u * 4) % 1) * 2 - 1) * 460 }, 200).done(), v: 620, n: 5, sw: true);
		// "just turn around now": the train goes and comes back on the same track
		L11P tbk() { var P = L11seg(-80, 560).fn(u => new[] { -80 + u * 900, 560 - JS.sin(u * Math.PI) * 300 }, 80).done(); var pts = P.pts.ToList(); pts.Reverse(); return L11seg(pts[0][0], pts[0][1]).add(P.pts.ToList()).add(pts.Skip(1)).done(); }
		train(244, tbk, v: 600, n: 4); train(245, () => { var P = tbk(); return L11Path(P.pts.Select(q => new[] { W - q[0], H - q[1] }).ToList()); }, v: 600, n: 4);
		// "you're not welcome anymore": two trains on two tracks, each going its own way
		train(248, straight(180, 560, 1), v: 640, n: 5, sw: true); train(248.5, straight(180, 560, -1), v: 640, n: 5);
		// "tried to hurt me with goodbye": 3 small loops one after another
		train(252, () => { var s = L11seg(-80, 520); foreach (var cx in new double[] { 280, 640, 1000 }) { s.line(cx, 520); s.arc(cx, 400, 120, Math.PI / 2, Math.PI / 2 - TAU); } s.line(W + 80, 520); return s.done(); }, v: 620, n: 4);
		train(255, () => { var s = L11seg(W + 80, 200); foreach (var cx in new double[] { 1000, 640, 280 }) { s.line(cx, 200); s.arc(cx, 320, 120, -Math.PI / 2, -Math.PI / 2 - TAU); } s.line(-80, 200); return s.done(); }, v: 620, n: 4);
		// "Did you think I'd crumble": the track breaks and falls
		ev(258, () => hazards.Add(L11Crumble(300, easy ? 6 : 8, B(258), B(260), B(263)))); ev(259, () => hazards.Add(L11Crumble(140, easy ? 5 : 7, B(259), B(261.5), B(264))));
		// "lay down and die": the strength tester and the drop tower come back together
		ev(262, () => { var sl = new[] { B(264), B(266) }; hazards.Add(L11Striker(640, B(262), B(270), sl)); });
		foreach (var b in (easy ? new double[] { 264 } : new double[] { 264, 266 })) ev(b, () => { double tr = B(b) + 0.45; hazards.Add(L11Ring(640, 100, tr, tr + 3.2 / K, 30, 1500, 22, Math.PI / 2 + (R() - 0.5) * 1.4, easy ? 1.5 : 0.8, new L11RgO { lin = true })); });
		ev(263, () => { hazards.Add(L11Tower(220, B(263), B(264), B(265.5), B(266), B(270), 140)); if (!easy) hazards.Add(L11Tower(1060, B(263.5), B(264.5), B(266), B(266.5), B(270), 140)); });
		// 268 "Oh no, not I, I will survive": two trains on two tracks crossing in an X
		train(268, straight(80, 640, 1), v: 700, n: 6, sw: true); train(268.5, straight(80, 640, -1), v: 700, n: 6);
		train(272, loop(600, 400, 190, 1), v: 640, n: 5); train(273, loop(600, 880, 190, -1), v: 640, n: 5);
		// 276 "as long as I know how to love": two hearts
		train(276, () => L11heartPath(380, 360, 160, Math.PI, 1), v: 520, n: 4); train(277, () => L11heartPath(900, 360, 160, Math.PI, -1), v: 520, n: 4);
		// 284 "all my life to live / love to give": the teacups, and a train passes
		ev(282, () => hazards.Add(L11Cups(W / 2, 380, 110, 200, easy ? 3 : 5, 38, 1.4 * K, B(282), B(284), B(292))));
		train(285, straight(100, 100, 1), v: 700, n: 6, sw: true); train(288, straight(640, 640, -1), v: 700, n: 6); train(290, straight(100, 100, -1), v: 700, n: 6);
		// 292 "and I'll survive": the ship, one big swing
		if (!easy) ev(290.5, () => hazards.Add(L11Ship(B(290.5), B(292), B(296.5), easy ? new[] { 0.5, 0.55 } : new[] { 0.8, 0.85 }, 4 * SPB)));
		// 296 the long "I will survive": the train climbs the big lift slowly, charging white, and at 304 goes down
		L11P lift() => L11seg(-120, H - 60).line(1100 + 108 * JS.cos(-Math.PI * 0.8), 240 + 108 * JS.sin(-Math.PI * 0.8)).arc(1100, 240, 108, -Math.PI * 0.8, -Math.PI * 0.25).fn(u => new[] { 1170 + u * 300, 170 + u * u * 700 }, 40).done();
		ev(294, () =>
		{
			var P = lift(); double tA = B(296), tTop = B(303.6), sTop = P.near(1100, 132), v1 = sTop / (tTop - tA);
			hazards.Add(L11Ride(P, new L11RO { tw = B(294), t0 = tA, v = v1, r = 20, kind = "car", pv = "track", n = 6, sp = 52, dashAt = sTop, v2 = 1500, wht = t => clamp((t - B(300)) / (tTop - B(300)), 0, 1) }));
		});
		// under the lift: bell rings come down (so you don't sit in one spot)
		foreach (var (b, x) in (easy ? new[] { (298.0, 640.0) } : new[] { (296.0, 320.0), (298.0, 960.0), (300.0, 640.0), (302.0, 320.0) })) ev(b, () => hazards.Add(L11Ring(x, 100, B(b), B(b) + 3.2 / K, 30, 1500, 22, Math.PI / 2 + (R() - 0.5) * 1.4, easy ? 1.5 : 0.8, new L11RgO { lin = true })));
		ev(304.4, () => { var cols = new[] { "rgba(255,220,120,0.9)", "rgba(140,200,255,0.9)", "rgba(255,255,255,0.9)" }; for (int i = 0; i < 40; i++) { double a = -Math.PI * 0.95 + rnd() * Math.PI * 0.9; double px = 1150 + (rnd() - 0.5) * 200, py = 200 + rnd() * 200; particles.Add(new Particle { x = px, y = py, vx = JS.cos(a) * 420, vy = JS.sin(a) * 420, life = 1.8, max = 1.8, col = cols[i % 3], s = 8 }); } });
		fx(304.4, 0.7, 0);
		// cotton candy: a pink cloud grows on its stick and bursts (or flies away like a balloon)
		int CI = 0;
		void candy(double b, double[][] L, double pop = 4, int fly = 0) { if (easy && (CI++) % 2 != 0) return; double wb = b - 2 * WN; ev(wb, () => { foreach (var q in L) { double x = q[0], y = q[1]; hazards.Add(L11Candy(x, y, easy ? 68 : 94, B(wb), B(b), B(b + pop), fly != 0 ? new L11CO { vy = -260, vx = (x < W / 2 ? -60 : 60), fly = 2.5 } : new L11CO())); } }); }
		double[][] C2(params double[] v) { var o = new double[v.Length / 2][]; for (int i = 0; i < o.Length; i++) o[i] = new[] { v[2 * i], v[2 * i + 1] }; return o; }
		candy(173, C2(110, 560, 1170, 560)); candy(179, C2(110, 180, 1170, 180), 3.5, 1); candy(189, C2(150, 610, 1130, 610), 4); candy(197, C2(160, 190, 1120, 190), 3.5, 1);
		candy(205, C2(640, 150), 3); candy(208, C2(640, 600), 3.5); candy(214, C2(200, 560, 1080, 160), 3.5); candy(217, C2(1080, 560, 200, 160), 3.5, 1);
		candy(231, C2(640, 110, 640, 620), 3.5); candy(241, C2(320, 600, 960, 140), 3.5); candy(249, C2(160, 360, 1120, 360), 3, 1); candy(258, C2(400, 600, 880, 600), 3.5);
		candy(274, C2(140, 140, 1140, 140), 3); candy(279, C2(640, 130), 3, 1); candy(281.5, C2(180, 600, 1100, 600), 3); candy(293, C2(160, 170, 1120, 170), 3, 1);
		// a water splash under the boardwalk: a wave runs on the ground
		int SI = 0;
		void splash(double b, double dir, double h = 170) { if (easy && (SI++) % 2 != 0) return; double wb = b - 2 * WN; ev(wb, () => hazards.Add(L11Splash(dir, easy ? h * 0.8 : h, 150, 560, B(wb), B(b)))); }
		foreach (var q in new double[][] { new double[] { 175, 1 }, new double[] { 181.5, -1, 140 }, new double[] { 199, -1 }, new double[] { 205.5, 1, 140 }, new double[] { 213, -1 }, new double[] { 218, 1 }, new double[] { 228, 1, 150 }, new double[] { 233, -1 }, new double[] { 242.5, 1 }, new double[] { 250.5, -1, 150 }, new double[] { 256, -1 }, new double[] { 261, 1, 150 }, new double[] { 271, 1 }, new double[] { 278, -1, 150 }, new double[] { 283, -1 }, new double[] { 289, 1, 150 }, new double[] { 299, 1, 130 } })
			if (q.Length > 2) splash(q[0], q[1], q[2]); else splash(q[0], q[1]);
		// at you on a fixed table: cotton candy grows where you are, or a train passes on your row or column
		void aimC(double b, int kind)
		{
			if (kind == 0) { double wb0 = b - 2 * WN; ev(wb0, () => { if (A2) hazards.Add(L11Candy(clamp(PXp(), 10, W - 10), clamp(PYp(), 10, H - 10), easy ? 62 : 76, B(wb0), B(b), B(b + 1.6))); }); return; }
			double wb = b - 2.2 * WN; const double v = 640;
			ev(wb, () =>
			{
				if (!A2) return; double x = clamp(PXp(), 10, W - 10), y = clamp(PYp(), 10, H - 10); bool hz = kind == 1; double dir = hz ? (x < W / 2 ? 1 : -1) : (y < H / 2 ? 1 : -1);
				var P = hz ? L11seg(dir > 0 ? -80 : W + 80, y).line(dir > 0 ? W + 80 : -80, y).done() : L11seg(x, dir > 0 ? -80 : H + 80).line(x, dir > 0 ? H + 80 : -80).done(); double d = hz ? Math.Abs(x - (dir > 0 ? -80 : W + 80)) : Math.Abs(y - (dir > 0 ? -80 : H + 80));
				hazards.Add(L11Ride(P, new L11RO { tw = B(wb), t0 = B(b) - d / v, v = v, r = 20, kind = "car", pv = "track", n = 3, sp = 52, big = true }));
			});
		}
		var AC = new[] { 174.5, 180, 185, 190, 195, 199.5, 205, 209, 214, 219, 223.5, 228, 232.5, 239, 243, 247, 251, 256.5, 260.5, 266, 270, 275, 279, 284, 288, 292.5, 297, 301 };
		for (int i = 0; i < AC.Length; i++) { if (easy && i % 3 == 2) continue; aimC(AC[i], i % 3); }
		// every phase has its table: it starts with the phase (and a checkpoint brings it back from its place)
		sort(SW, (a, b) => a.wb - b.wb);
		foreach (var (b0, b1) in new[] { (0.0, 74.0), (74.0, 170.0), (170.0, 305.0) }) { var L = SW.Where(q => q.wb >= b0 && q.wb < b1).Select(q => (B(q.wb), q.f)).ToList(); if (L.Count > 0) ev(b0, () => hazards.Add(L11Sched(L))); }
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
