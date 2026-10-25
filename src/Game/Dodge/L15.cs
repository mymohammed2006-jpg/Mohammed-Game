using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L15 "Abra Macabre" (Black Ops Zombies): the summoning in the crypt. Every attack is a fixed pattern on the beat that asks for one tactic:
// part 1 the crypt (the lantern, the ribcage waves, the falling floor, the chained cells and their echo, the light closing in),
// part 2 the ritual (the bone arms that turn and turn back, echo blasts, the curse on your own path, the door rings, the star),
// part 3 the beast (pendulums and the creeping fog, the rotating jaws, the spine whips, the false light, then all of it at once).
// Every hazard is a pure function of time once it exists (the curse also of where the square has been), so hit(p, t) is right for any later t.
// The data is data/dodge_L15.json (made by tools/level15/gen.py).
public static partial class G
{
	static double L15k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L15P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L15G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static double L15Ease(double k) => 1 - JS.pow(1 - clamp(k, 0, 1), 3);
	static double L15Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static void L15Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}

	// a smooth path through keyframes (beats b, places x y, and a radius r that changes in straight lines)
	public class L15Path
	{
		public double[] b, x, y, r;
		static double M(double[] v, double[] b, int i) => b.Length < 2 ? 0 : i <= 0 ? (v[1] - v[0]) / (b[1] - b[0]) : i >= v.Length - 1 ? (v[i] - v[i - 1]) / (b[i] - b[i - 1]) : (v[i + 1] - v[i - 1]) / (b[i + 1] - b[i - 1]);
		static double Hm(double[] v, double[] b, int i, double u, double d) { double u2 = u * u, u3 = u2 * u; return (2 * u3 - 3 * u2 + 1) * v[i] + (u3 - 2 * u2 + u) * d * M(v, b, i) + (-2 * u3 + 3 * u2) * v[i + 1] + (u3 - u2) * d * M(v, b, i + 1); }
		public (double x, double y, double r) At(double bt)
		{
			int n = b.Length; double rr(int i) => r == null || r.Length == 0 ? 0 : r[i];
			if (n == 1 || bt <= b[0]) return (x[0], y[0], rr(0));
			if (bt >= b[n - 1]) return (x[n - 1], y[n - 1], rr(n - 1));
			int i = 0; while (i < n - 2 && bt >= b[i + 1]) i++;
			double d = b[i + 1] - b[i], u = (bt - b[i]) / d;
			return (Hm(x, b, i, u, d), Hm(y, b, i, u, d), rr(i) + (rr(i + 1) - rr(i)) * u);
		}
	}

	// a turning that follows a plan: from beat sb[i] the speed is sw[i] (radians a beat), changing over half a beat
	public class L15Spin
	{
		readonly double b0; readonly double[] tab, sb, sw; const double ST = 1.0 / 32;
		public double W(double b) { double v = sw[0]; for (int i = 1; i < sb.Length; i++) { if (b >= sb[i] + 0.5) v = sw[i]; else { if (b > sb[i]) v = sw[i - 1] + (sw[i] - sw[i - 1]) * (b - sb[i]) / 0.5; break; } } return v; }
		public L15Spin(double a0, double b0, double b1, double[] sb, double[] sw)
		{
			this.b0 = b0; this.sb = sb.Length > 0 ? sb : new[] { b0 }; this.sw = sw.Length > 0 ? sw : new[] { 0.0 };
			int n = (int)Math.Ceiling((b1 - b0) / ST) + 2; tab = new double[n]; tab[0] = a0;
			for (int k = 1; k < n; k++) { double bb = b0 + k * ST; tab[k] = tab[k - 1] + (W(bb - ST) + W(bb)) / 2 * ST; }
		}
		public double At(double bt) { double f = (bt - b0) / ST; if (f <= 0) return tab[0]; int i = (int)f; if (i >= tab.Length - 1) return tab[^1]; return tab[i] + (tab[i + 1] - tab[i]) * (f - i); }
	}

	// ---------- the look of things ----------
	// a bone from (x0,y0) to (x1,y1), w thick, knuckles at both ends
	static void L15Bone(double x0, double y0, double x1, double y1, double w, string col)
	{
		double a = JS.atan2(y1 - y0, x1 - x0), L = hypot(x1 - x0, y1 - y0); if (L < 1) return;
		g.save(); g.translate(x0, y0); g.rotate(a); g.fillStyle = col;
		g.fillRect(w * 0.35, -w * 0.4, Math.Max(0, L - w * 0.7), w * 0.8);
		circle(w * 0.3, -w * 0.28, w * 0.36); circle(w * 0.3, w * 0.28, w * 0.36); circle(L - w * 0.3, -w * 0.28, w * 0.36); circle(L - w * 0.3, w * 0.28, w * 0.36);
		g.fillStyle = "rgba(20,0,6,0.35)"; for (double s = w * 1.4; s < L - w * 1.2; s += w * 1.6) g.fillRect(s, -w * 0.4, w * 0.12, w * 0.8);
		g.restore();
	}
	// a skull (r = size; jaw 0..1 open)
	static void L15SkullDraw(double x, double y, double r, double a, string col, double jaw = 0)
	{
		g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = col;
		g.beginPath(); g.ellipse(0, -r * 0.15, r, r * 0.9, 0, 0, TAU); g.fill();
		g.beginPath(); g.moveTo(-r * 0.72, r * 0.25); g.lineTo(r * 0.72, r * 0.25); g.lineTo(r * 0.5, r * 0.85); g.lineTo(-r * 0.5, r * 0.85); g.closePath(); g.fill();
		g.fillRect(-r * 0.46, r * 0.88 + jaw * r * 0.45, r * 0.92, r * 0.3);
		g.fillStyle = "rgba(8,0,3,0.93)";
		foreach (var sd in new[] { -1.0, 1.0 }) { g.beginPath(); g.moveTo(sd * r * 0.1, -r * 0.02); g.lineTo(sd * r * 0.64, -r * 0.22); g.lineTo(sd * r * 0.56, r * 0.22); g.lineTo(sd * r * 0.16, r * 0.24); g.closePath(); g.fill(); }
		g.beginPath(); g.moveTo(0, r * 0.3); g.lineTo(-r * 0.12, r * 0.56); g.lineTo(r * 0.12, r * 0.56); g.closePath(); g.fill();
		for (int i = -3; i <= 3; i++) g.fillRect(i * r * 0.12 - r * 0.018, r * 0.66, r * 0.036, r * 0.22);
		for (int i = -3; i <= 3; i++) g.fillRect(i * r * 0.12 - r * 0.018, r * 0.88 + jaw * r * 0.45, r * 0.036, r * 0.16);
		g.restore();
	}
	// bone spikes bursting out of the floor at (x,y) (k = how far they are out)
	static void L15Spikes(double x, double y, double R, double k, string col, double rot = 0)
	{
		if (k <= 0) return; g.fillStyle = col; g.beginPath();
		for (int i = 0; i < 6; i++) { double a = rot + i * TAU / 6, l = R * k * (i % 2 == 0 ? 1.15 : 0.8); g.moveTo(x + JS.cos(a - 0.45) * R * 0.3, y + JS.sin(a - 0.45) * R * 0.3); g.lineTo(x + JS.cos(a) * l, y + JS.sin(a) * l); g.lineTo(x + JS.cos(a + 0.45) * R * 0.3, y + JS.sin(a + 0.45) * R * 0.3); }
		g.closePath(); g.fill(); circle(x, y, R * 0.42 * k);
	}
	// the deadly fog over the whole screen with round holes of light. Before it kills it is grey (deadly = false); a = how thick
	static void L15FogDraw(double t, double a, bool deadly, IList<(double x, double y, double r)> holes)
	{
		if (a <= 0.01) return;
		g.save(); g.globalAlpha = a;
		g.fillStyle = deadly ? "rgba(214,24,84,0.9)" : "rgba(64,56,68,0.8)"; g.fillRect(0, 0, W, H);
		for (int i = 0; i < 10; i++)
		{
			double sx = ((i * 211 + t * (22 + i * 4)) % (W + 360)) - 180, sy = (i * 67 % H) + JS.sin(t * 0.6 + i * 1.7) * 70, sr = 150 + 50 * JS.sin(t * 0.9 + i);
			var sg = g.createRadialGradient(sx, sy, 0, sx, sy, sr); sg.addColorStop(0, deadly ? "rgba(90,0,28,0.25)" : "rgba(20,16,22,0.4)"); sg.addColorStop(1, deadly ? "rgba(90,0,28,0)" : "rgba(20,16,22,0)"); g.fillStyle = sg; circle(sx, sy, sr);
		}
		foreach (var hl in holes)
		{
			if (hl.r <= 0) continue;
			var gr = g.createRadialGradient(hl.x, hl.y, 0, hl.x, hl.y, hl.r); gr.addColorStop(0, "rgba(120,82,58,1)"); gr.addColorStop(0.55, "rgba(52,28,22,1)"); gr.addColorStop(1, "rgba(14,5,7,1)");
			g.fillStyle = gr; circle(hl.x, hl.y, hl.r);
			g.strokeStyle = deadly ? PINK : L15G(0.6); g.lineWidth = 3; g.beginPath(); g.arc(hl.x, hl.y, hl.r, 0, TAU); g.stroke();
		}
		g.restore();
	}

	// ---------- part 1: the crypt ----------
	// the lantern: all but a circle of light is the deadly fog. The light runs along the path; a dashed line shows its next 1.5 beats and a
	// ring where it will be one beat on. Grey fog from tw, it kills from ta, it lifts at te (fading 0.3 s)
	public static Hz L15Lantern(L15Path P, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L15Lantern" };
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = Math.Min(L15k(t, tw, tw + 0.6), 1 - L15k(t, te, te + 0.3)); var c = P.At(bt);
			L15FogDraw(t, a, t >= ta, new[] { c });
			if (t < te)
			{
				g.save(); g.globalAlpha = a; g.strokeStyle = "rgba(255,225,205,0.55)"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 9 }); g.beginPath();
				for (int i = 0; i <= 12; i++) { var q = P.At(bt + i * 0.125); if (i == 0) g.moveTo(q.x, q.y); else g.lineTo(q.x, q.y); }
				g.stroke(); g.setLineDash(new double[0]); var n = P.At(bt + 1); g.strokeStyle = "rgba(255,225,205,0.3)"; g.beginPath(); g.arc(n.x, n.y, n.r, 0, TAU); g.stroke(); g.restore();
			}
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; var c = P.At(BT(t)); return hypot(p.x - c.x, p.y - c.y) > c.r - p.r + 2; };
		return h;
	}

	// a ribcage wave: n columns of ribs snap shut from the floor and the ceiling one after the other (dir 1: from the left), column k at
	// ta + dur * k / (n - 1), and stay shut hold seconds. Each column leaves one gap (centre gy[k], half height gh). Grey outline from tw
	public static Hz L15Ribs(int dir, double[] gy, double gh, double tw, double ta, double dur, double hold)
	{
		int n = gy.Length; double cw = W / n; var h = new Hz { kind = "L15Ribs" };
		double tc(int col) { int k = dir > 0 ? col : n - 1 - col; return ta + dur * k / Math.Max(1, n - 1); }
		h.update = (t, dt) => { if (t > ta + dur + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			// the gap: a grey channel to stand in
			double kw = L15k(t, tw, ta);
			g.strokeStyle = L15G(0.25 + 0.5 * kw); g.lineWidth = 2; g.setLineDash(new double[] { 9, 7 });
			for (int sd = -1; sd <= 1; sd += 2) { g.beginPath(); for (int c = 0; c < n; c++) { double cx = (c + 0.5) * cw, y = gy[c] + sd * gh; if (c == 0) g.moveTo(0, y); g.lineTo(cx, y); if (c == n - 1) g.lineTo(W, y); } g.stroke(); }
			g.setLineDash(new double[0]);
			for (int c = 0; c < n; c++)
			{
				double t0 = tc(c), x0 = c * cw; double q = t < t0 - 0.09 ? 0 : t < t0 ? (t - t0 + 0.09) / 0.09 : t < t0 + hold ? 1 : Math.Max(0, 1 - (t - t0 - hold) / 0.12);
				if (t < t0 && t > t0 - 0.6) { g.fillStyle = L15G(0.06 + 0.16 * L15k(t, t0 - 0.6, t0)); g.fillRect(x0 + 2, 0, cw - 4, gy[c] - gh); g.fillRect(x0 + 2, gy[c] + gh, cw - 4, H - gy[c] - gh); }
				if (q <= 0) continue;
				double top = (gy[c] - gh) * q, bot = H - (H - gy[c] - gh) * q; bool wh = t >= t0 && t < t0 + 0.05;
				g.fillStyle = wh ? PINKW : PINK; g.fillRect(x0 + 1, 0, cw - 2, top); g.fillRect(x0 + 1, bot, cw - 2, H - bot);
				// the ribs: curved bones across the column
				g.strokeStyle = "rgba(40,0,12,0.55)"; g.lineWidth = 5; g.beginPath();
				for (double y = 18; y < top - 6; y += 34) { g.moveTo(x0 + 4, y); g.quadraticCurveTo(x0 + cw / 2, y + 14, x0 + cw - 4, y); }
				for (double y = H - 18; y > bot + 6; y -= 34) { g.moveTo(x0 + 4, y); g.quadraticCurveTo(x0 + cw / 2, y - 14, x0 + cw - 4, y); }
				g.stroke();
				g.fillStyle = "#f4e6ea"; g.beginPath(); for (int i = 0; i < 3; i++) { double fx = x0 + cw * (0.2 + 0.3 * i); g.moveTo(fx - 7, top); g.lineTo(fx, top + 16); g.lineTo(fx + 7, top); g.moveTo(fx - 7, bot); g.lineTo(fx, bot - 16); g.lineTo(fx + 7, bot); } g.fill();
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > ta + dur + hold) return false;
			int c0 = (int)clamp(Math.Floor((p.x - p.r + 2) / cw), 0, n - 1), c1 = (int)clamp(Math.Floor((p.x + p.r - 2) / cw), 0, n - 1);
			for (int c = c0; c <= c1; c++) { double t0 = tc(c); if (t >= t0 && t < t0 + hold && (p.y - p.r + 2 < gy[c] - gh || p.y + p.r - 2 > gy[c] + gh)) return true; }
			return false;
		};
		return h;
	}

	// the floor of the crypt in slabs (cols x rows): slab ord[j] cracks (wn s) and falls at t0 + j * rate, and comes back up dn s later.
	// A fallen slab is the pink pit
	public static Hz L15Floor(int cols, int rows, double[] ord, double tw, double t0, double rate, double dn, double wn, double te)
	{
		double sw = W / cols, sh = H / rows; int n = cols * rows; var fall = new double[n]; for (int i = 0; i < n; i++) fall[i] = double.PositiveInfinity;
		var fall2 = new double[n]; for (int i = 0; i < n; i++) fall2[i] = double.PositiveInfinity;
		for (int j = 0; j < ord.Length; j++) { int i = (int)ord[j]; if (i < 0 || i >= n) continue; if (double.IsPositiveInfinity(fall[i])) fall[i] = t0 + j * rate; else fall2[i] = t0 + j * rate; }
		var h = new Hz { kind = "L15Floor" };
		double fallOf(int i, double t) => t >= fall2[i] - wn ? fall2[i] : fall[i];
		bool down(int i, double t) { double f = fallOf(i, t); return t >= f && t < f + dn && t < te; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L15k(t, tw, tw + 0.5), 1 - L15k(t, te, te + 0.4));
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double x = (i % cols) * sw, y = (i / cols) * sh, f = fallOf(i, t);
				if (down(i, t))
				{
					double u = t - f, k = Math.Min(1, u / 0.12);
					g.fillStyle = u < 0.05 ? PINKW : PINK; g.fillRect(x + 3, y + 3, sw - 6, sh - 6);
					var gr = g.createRadialGradient(x + sw / 2, y + sh / 2, 4, x + sw / 2, y + sh / 2, sw * 0.6); gr.addColorStop(0, "rgba(40,0,10,0.7)"); gr.addColorStop(1, "rgba(40,0,10,0)");
					g.fillStyle = gr; g.fillRect(x + 3, y + 3, sw - 6, sh - 6);
					// the slab dropping away
					if (k < 1) { double s = 1 - k; g.fillStyle = $"rgba(60,48,56,{str(s)})"; g.fillRect(x + sw / 2 - sw / 2 * s, y + sh / 2 - sh / 2 * s, sw * s, sh * s); }
					continue;
				}
				double rise = t >= f + dn ? L15k(t, f + dn, f + dn + 0.15) : 1;
				g.fillStyle = $"rgba(52,40,48,{str(0.55 * rise)})"; g.fillRect(x + 3, y + 3, sw - 6, sh - 6);
				g.strokeStyle = "rgba(0,0,0,0.5)"; g.lineWidth = 2; g.strokeRect(x + 3, y + 3, sw - 6, sh - 6);
				if (t >= f - wn && t < f)
				{
					// cracks that grow until it falls (the order shows)
					double k = L15k(t, f - wn, f), cx = x + sw / 2 + JS.sin(t * 60 + i) * 2 * k, cy = y + sh / 2;
					g.fillStyle = L15P(0.08 + 0.3 * k); g.fillRect(x + 3, y + 3, sw - 6, sh - 6);
					g.strokeStyle = L15G(0.4 + 0.6 * k); g.lineWidth = 2 + k; g.beginPath();
					for (int r = 0; r < 5; r++) { double ang = r * 1.31 + i, l = (sw * 0.5) * k; g.moveTo(cx, cy); g.lineTo(cx + JS.cos(ang) * l * 0.5, cy + JS.sin(ang) * l * 0.4); g.lineTo(cx + JS.cos(ang + 0.3) * l, cy + JS.sin(ang + 0.3) * l * 0.8); }
					g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0 || t >= te) return false;
			int c0 = (int)clamp(Math.Floor((p.x - p.r + 2) / sw), 0, cols - 1), c1 = (int)clamp(Math.Floor((p.x + p.r - 2) / sw), 0, cols - 1);
			int r0 = (int)clamp(Math.Floor((p.y - p.r + 2) / sh), 0, rows - 1), r1 = (int)clamp(Math.Floor((p.y + p.r - 2) / sh), 0, rows - 1);
			for (int r = r0; r <= r1; r++) for (int c = c0; c <= c1; c++) if (down(r * cols + c, t)) return true;
			return false;
		};
		return h;
	}

	// the chains split the crypt into cells (cols x rows). At each pulse (pt) every cell but the safe one (ps) burns for hold s.
	// The next safe cell shows from the end of the pulse before (at most wn s before). Echo pulses (pe) are the mirror of the one before
	public static Hz L15Cells(int cols, int rows, double[] pt, double[] ps, double[] pe, double hold, double wn, double tw, double te)
	{
		double cw = W / cols, ch = H / rows; var h = new Hz { kind = "L15Cells" };
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		(double x, double y) cc(int s) => ((s % cols + 0.5) * cw, (s / cols + 0.5) * ch);
		h.draw = t =>
		{
			if (t < tw) return; double a = Math.Min(L15k(t, tw, tw + 0.5), 1 - L15k(t, te, te + 0.4));
			g.save(); g.globalAlpha = a;
			int k = 0; while (k < pt.Length && t >= pt[k] + hold) k++;
			// the burning pulse
			if (k < pt.Length && t >= pt[k])
			{
				int s = (int)ps[k]; bool wh = t - pt[k] < 0.05;
				for (int i = 0; i < cols * rows; i++) if (i != s) { g.fillStyle = wh ? PINKW : PINK; g.fillRect((i % cols) * cw + 2, (i / cols) * ch + 2, cw - 4, ch - 4); }
				if (!wh) for (int i = 0; i < cols * rows; i++) if (i != s) L15SkullDraw((i % cols + 0.5) * cw, (i / cols + 0.5) * ch, 26, 0, "rgba(60,0,18,0.35)");
			}
			// the next one: its safe cell outlined, the others turning
			else if (k < pt.Length)
			{
				double from = Math.Max(pt[k] - wn, k > 0 ? pt[k - 1] + hold : tw), q = L15k(t, from, pt[k]);
				if (t >= from)
				{
					int s = (int)ps[k]; bool echo = pe.Length > k && pe[k] > 0;
					for (int i = 0; i < cols * rows; i++) if (i != s) { g.fillStyle = echo ? $"rgba(190,120,220,{str(0.05 + 0.2 * q)})" : L15P(0.04 + 0.2 * q); g.fillRect((i % cols) * cw + 2, (i / cols) * ch + 2, cw - 4, ch - 4); }
					g.strokeStyle = $"rgba(255,240,245,{str(0.5 + 0.5 * q)})"; g.lineWidth = 3 + 3 * q; g.strokeRect((s % cols) * cw + 6, (s / cols) * ch + 6, cw - 12, ch - 12);
					// an echo: a line through the middle to the mirrored cell
					if (echo && k > 0) { var p0 = cc((int)ps[k - 1]); var p1 = cc(s); L15Dash(p0.x, p0.y, p1.x, p1.y, "rgba(200,150,230,0.7)", 3); }
				}
			}
			// the chains
			g.strokeStyle = "rgba(150,140,150,0.75)"; g.lineWidth = 3;
			for (int c = 1; c < cols; c++) { double x = c * cw; for (double y = 0; y < H; y += 22) { g.beginPath(); g.ellipse(x, y + 11, 5, 10, 0, 0, TAU); g.stroke(); } }
			for (int r = 1; r < rows; r++) { double y = r * ch; for (double x = 0; x < W; x += 22) { g.beginPath(); g.ellipse(x + 11, y, 10, 5, 0, 0, TAU); g.stroke(); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			int k = 0; while (k < pt.Length && t >= pt[k] + hold) k++;
			if (k >= pt.Length || t < pt[k]) return false; int s = (int)ps[k]; double x0 = (s % cols) * cw, y0 = (s / cols) * ch;
			return p.x - p.r + 2 < x0 || p.x + p.r - 2 > x0 + cw || p.y - p.r + 2 < y0 || p.y + p.r - 2 > y0 + ch;
		};
		return h;
	}

	// ---------- part 2: the ritual ----------
	// bone arms from (cx,cy): n of them from radius r0 to len, th thick, turning by the plan (sp); grey ghosts from tw, they kill ta .. te.
	// The arrows round the skull in the middle show the way they turn, and flip a beat before they do
	public static Hz L15Arms(double cx, double cy, int n, double r0, double len, double th, L15Spin sp, double tw, double ta, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L15Arms" };
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), th0 = sp.At(bt), a = 1 - L15k(t, te, te + 0.4); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double an = th0 + i * TAU / n, c = JS.cos(an), s = JS.sin(an);
				if (!on) { L15Dash(cx + c * r0, cy + s * r0, cx + c * len, cy + s * len, L15G(0.3 + 0.5 * L15k(t, tw, ta)), 3); continue; }
				g.shadowColor = PINK; g.shadowBlur = 14; L15Bone(cx + c * r0, cy + s * r0, cx + c * len, cy + s * len, th, t - ta < 0.05 ? PINKW : PINK); g.shadowBlur = 0;
			}
			if (r0 < 60)
			{
				double w = sp.W(bt), w1 = sp.W(bt + 1); int dir = Math.Sign(Math.Abs(w1) > 0.02 ? w1 : w); bool flip = Math.Sign(w1) != Math.Sign(w) && Math.Abs(w1) > 0.02;
				g.strokeStyle = flip ? (bt % 0.5 < 0.25 ? "#fff" : L15G(0.9)) : L15G(0.55); g.lineWidth = 4;
				for (int i = 0; i < 6; i++) { double an = th0 + i * TAU / 6 + 0.4, x = cx + JS.cos(an) * 78, y = cy + JS.sin(an) * 78, d = an + dir * Math.PI / 2; g.beginPath(); g.moveTo(x + JS.cos(d + 2.5) * 12, y + JS.sin(d + 2.5) * 12); g.lineTo(x, y); g.lineTo(x + JS.cos(d - 2.5) * 12, y + JS.sin(d - 2.5) * 12); g.stroke(); }
				L15SkullDraw(cx, cy - 6, 42, 0, on ? PINK : L15G(0.6), 0.3 + 0.5 * pulse);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double th0 = sp.At(BT(t));
			for (int i = 0; i < n; i++) { double an = th0 + i * TAU / n, c = JS.cos(an), s = JS.sin(an); if (L15Seg(p.x, p.y, cx + c * r0, cy + s * r0, cx + c * len, cy + s * len) < th / 2 + p.r - 2) return true; }
			if (r0 < 60 && hypot(p.x - cx, p.y - cy) < 44 + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// a blast of bone spikes at (x,y), radius R: a grey ring (tw), it bursts at ta for hold s. An echo's ring is dashed and violet
	public static Hz L15Blast(double x, double y, double R, double tw, double ta, double hold, bool echo)
	{
		var h = new Hz { x = x, y = y, kind = "L15Blast" }; bool b0 = false;
		h.update = (t, dt) => { if (!b0 && t >= ta) { b0 = true; shake = Math.Max(shake, 5); burst(x, y, PINK, 6, 260); } if (t > ta + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = L15k(t, tw, ta); g.strokeStyle = echo ? $"rgba(200,150,230,{str(0.35 + 0.6 * k)})" : L15G(0.3 + 0.6 * k); g.lineWidth = 3; if (echo) g.setLineDash(new double[] { 10, 8 });
				g.beginPath(); g.arc(x, y, R, 0, TAU); g.stroke(); g.setLineDash(new double[0]); g.fillStyle = echo ? $"rgba(200,150,230,{str(0.05 + 0.18 * k)})" : L15P(0.04 + 0.18 * k); circle(x, y, R * k); return;
			}
			double u = t - ta, al = u < hold ? 1 : Math.Max(0, 1 - (u - hold) / 0.3);
			g.save(); g.globalAlpha = al; g.fillStyle = L15P(0.6); circle(x, y, R); g.shadowColor = PINK; g.shadowBlur = 16;
			L15Spikes(x, y, R, Math.Min(1, u / 0.07), u < 0.05 ? PINKW : PINK, x * 0.01); g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) => t >= ta && t < ta + hold && hypot(p.x - x, p.y - y) < R + p.r - 2;
		return h;
	}

	// the curse: wherever the square goes, bone spikes come up dl s later and stay lf s (the marks show before they come up)
	public class L15Mark { public double t, x, y; }
	public static Hz L15Curse(double ta, double te, double dl, double lf, double R)
	{
		var h = new Hz { kind = "L15Curse" }; var M = new List<L15Mark>();
		h.update = (t, dt) =>
		{
			if (t > te + dl + lf + 0.3) { h.dead = true; return; }
			if (t >= ta && t < te && player != null && state != "dead")
			{
				var l = M.Count > 0 ? M[^1] : null;
				if (l == null || t - l.t >= 0.04 || hypot(player.x - l.x, player.y - l.y) > R * 0.6) M.Add(new L15Mark { t = t, x = player.x, y = player.y });
			}
			while (M.Count > 0 && M[0].t < t - dl - lf - 0.5) M.RemoveAt(0);
		};
		h.draw = t =>
		{
			if (t < ta - 0.6) return;
			if (t < te) { double k = L15k(t, ta - 0.6, ta); g.strokeStyle = $"rgba(200,150,230,{str(0.35 * k)})"; g.lineWidth = 8; g.strokeRect(4, 4, W - 8, H - 8); }
			foreach (var m in M)
			{
				double u = t - m.t; if (u < 0) continue;
				if (u < dl) { double k = u / dl; g.fillStyle = $"rgba(30,10,20,{str(0.25 + 0.45 * k)})"; circle(m.x, m.y, R * (0.4 + 0.5 * k)); g.strokeStyle = $"rgba(200,150,230,{str(0.2 + 0.5 * k)})"; g.lineWidth = 1.5; g.beginPath(); g.arc(m.x, m.y, R * (0.4 + 0.5 * k), 0, TAU); g.stroke(); }
				else if (u < dl + lf + 0.25) { double v = u - dl, k = v < 0.06 ? v / 0.06 : v > lf ? Math.Max(0, 1 - (v - lf) / 0.25) : 1; L15Spikes(m.x, m.y, R, k, v < 0.04 ? PINKW : PINK, m.x * 0.03 + m.y * 0.02); }
			}
		};
		h.hit = (p, t) =>
		{
			foreach (var m in M) { double u = t - m.t; if (u >= dl && u < dl + lf && hypot(p.x - m.x, p.y - m.y) < R * 0.85 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the door rings: rings (radii rr, rw thick) round (cx,cy), each with one door (dw wide) and turning at its own speed (ws, radians a beat).
	// The fog closes in to radius fr[i] from beat fb[i] (its next edge shows 2 beats before as a dashed ring); the core (radius cR) blasts
	// at beats cb for ch beats. Grey from tw, kills ta .. te
	public static Hz L15Rings(double cx, double cy, double[] rr, double rw, double dw, double[] ws, double[] a0, double[] fb, double[] fr, double[] cb, double cR, double ch, double tw, double ta, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L15Rings" }; double b0 = BT(ta);
		double ang(int i, double bt) => a0[i] + ws[i] * (bt - b0);
		double fogR(double bt) { double r = 9999; for (int i = 0; i < fb.Length; i++) if (bt >= fb[i]) r = fr[i]; return r; }
		double nextFog(double bt, out double at) { at = 0; for (int i = 0; i < fb.Length; i++) if (fb[i] > bt) { at = fb[i]; return fr[i]; } return -1; }
		bool core(double bt) { foreach (var c in cb) if (bt >= c && bt < c + ch) return true; return false; }
		bool inDoor(int i, double a, double d, double bt) { double x = (a - ang(i, bt)) % TAU; if (x < -Math.PI) x += TAU; if (x > Math.PI) x -= TAU; return Math.Abs(x) < dw / 2 - 3 / Math.Max(d, 1); }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = Math.Min(L15k(t, tw, tw + 0.5), 1 - L15k(t, te, te + 0.4)); bool on = t >= ta && t < te;
			double fR = on ? fogR(bt) : 9999;
			if (fR < 2000) L15FogDraw(t, a, true, new[] { (cx, cy, fR) });
			g.save(); g.globalAlpha = a;
			double nf = nextFog(bt, out var nat);
			if (on && nf > 0 && bt > nat - 2) { double k = L15k(bt, nat - 2, nat); g.strokeStyle = nf > 2000 ? "rgba(255,240,245,0.5)" : L15P(0.4 + 0.6 * k); g.lineWidth = 3; g.setLineDash(new double[] { 14, 10 }); g.beginPath(); g.arc(cx, cy, Math.Min(nf, 760), 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			// the core
			double nc = -1; foreach (var c in cb) if (c + ch > bt) { nc = c; break; }
			if (on && nc >= 0)
			{
				if (bt >= nc) { g.fillStyle = bt - nc < 0.1 ? PINKW : PINK; g.shadowColor = PINK; g.shadowBlur = 24; circle(cx, cy, cR); g.shadowBlur = 0; L15Spikes(cx, cy, cR * 0.8, 1, "rgba(60,0,18,0.5)", bt); }
				else if (bt > nc - 3) { double k = L15k(bt, nc - 3, nc); g.fillStyle = L15P(0.05 + 0.25 * k); circle(cx, cy, cR); g.strokeStyle = L15P(0.4 + 0.6 * k); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(cx, cy, cR, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
			}
			// the rings and their doors (a skull on each side of a door)
			for (int i = 0; i < rr.Length; i++)
			{
				double an = ang(i, bt); g.strokeStyle = on ? PINK : L15G(0.3 + 0.5 * L15k(t, tw, ta)); g.lineWidth = rw; if (on) { g.shadowColor = PINK; g.shadowBlur = 12; }
				g.beginPath(); g.arc(cx, cy, rr[i], an + dw / 2, an - dw / 2 + TAU); g.stroke(); g.shadowBlur = 0;
				g.strokeStyle = "rgba(40,0,12,0.5)"; g.lineWidth = 2; g.beginPath(); for (int s = 0; s < 36; s++) { double q = an + dw / 2 + (s + 0.5) * (TAU - dw) / 36; g.moveTo(cx + JS.cos(q) * (rr[i] - rw / 2), cy + JS.sin(q) * (rr[i] - rw / 2)); g.lineTo(cx + JS.cos(q) * (rr[i] + rw / 2), cy + JS.sin(q) * (rr[i] + rw / 2)); } g.stroke();
				foreach (var sd in new[] { -1.0, 1.0 }) { double q = an + sd * (dw / 2 + 0.06); L15SkullDraw(cx + JS.cos(q) * rr[i], cy + JS.sin(q) * rr[i], 9, q + Math.PI / 2, on ? "#f4e6ea" : L15G(0.6)); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t), dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), a = JS.atan2(dy, dx);
			if (d > fogR(bt) - p.r + 2) return true;
			if (core(bt) && d < cR + p.r - 2) return true;
			for (int i = 0; i < rr.Length; i++) if (Math.Abs(d - rr[i]) < rw / 2 + p.r - 2 && !inDoor(i, a, d, bt)) return true;
			return false;
		};
		return h;
	}

	// the star: drawn line by line round (cx,cy) (circle R) while it turns (sp); line k fires at lt[k] (traced in grey wn s before).
	// At dt0 everything but the marked point of the star (sv) burns for dh s
	public static Hz L15Star(double cx, double cy, double R, L15Spin sp, double[] lt, double wn, int sv, double dt0, double dh, double tw, double th)
	{
		var h = new Hz { x = cx, y = cy, kind = "L15Star" }; int[] ord = { 0, 2, 4, 1, 3, 0 }; double ri = R * 0.381966;
		(double x, double y) V(int i, double bt) { double a = sp.At(bt) + i * TAU / 5 - Math.PI / 2; return (cx + JS.cos(a) * R, cy + JS.sin(a) * R); }
		(double x, double y) I(int i, double bt) { double a = sp.At(bt) + i * TAU / 5 - Math.PI / 2 + Math.PI / 5; return (cx + JS.cos(a) * ri, cy + JS.sin(a) * ri); }
		// the safe point: the tip at vertex sv and the two inner corners next to it
		(double x, double y)[] Tri(double bt) => new[] { V(sv, bt), I(sv, bt), I((sv + 4) % 5, bt) };
		static double side(double px, double py, (double x, double y) a, (double x, double y) b) { double ex = b.x - a.x, ey = b.y - a.y, l = Math.Sqrt(ex * ex + ey * ey); return ((px - a.x) * ey - (py - a.y) * ex) / l; }
		bool inTri(double px, double py, double m, double bt)
		{
			var T = Tri(bt); double s0 = side(px, py, T[0], T[1]), s1 = side(px, py, T[1], T[2]), s2 = side(px, py, T[2], T[0]);
			return (s0 >= m && s1 >= m && s2 >= m) || (s0 <= -m && s1 <= -m && s2 <= -m);
		}
		h.update = (t, dt) => { if (t > dt0 + dh + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = 1 - L15k(t, dt0 + dh, dt0 + dh + 0.4);
			g.save(); g.globalAlpha = a;
			g.strokeStyle = L15G(0.25); g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, R, 0, TAU); g.stroke();
			var T = Tri(bt);
			if (t >= dt0 && t < dt0 + dh)
			{
				g.fillStyle = t - dt0 < 0.06 ? PINKW : PINK; g.beginPath(); g.rect(0, 0, W, H); g.moveTo(T[0].x, T[0].y); g.lineTo(T[1].x, T[1].y); g.lineTo(T[2].x, T[2].y); g.closePath(); g.fill("evenodd");
			}
			else
			{
				g.fillStyle = "rgba(255,240,245,0.10)"; g.beginPath(); g.moveTo(T[0].x, T[0].y); g.lineTo(T[1].x, T[1].y); g.lineTo(T[2].x, T[2].y); g.closePath(); g.fill();
				double mx = (T[0].x * 2 + T[1].x + T[2].x) / 4, my = (T[0].y * 2 + T[1].y + T[2].y) / 4; L15SkullDraw(mx, my, 13, sp.At(bt) + sv * TAU / 5, "rgba(255,240,245,0.75)");
				if (t > dt0 - 1.0) { double k = L15k(t, dt0 - 1.0, dt0); g.fillStyle = L15P(0.15 * k); g.beginPath(); g.rect(0, 0, W, H); g.moveTo(T[0].x, T[0].y); g.lineTo(T[1].x, T[1].y); g.lineTo(T[2].x, T[2].y); g.closePath(); g.fill("evenodd"); }
			}
			for (int k = 0; k < 5; k++)
			{
				if (t < lt[k] - wn) continue; var p0 = V(ord[k], bt); var p1 = V(ord[k + 1], bt);
				if (t < lt[k]) { double q = L15k(t, lt[k] - wn, lt[k]); L15Dash(p0.x, p0.y, p0.x + (p1.x - p0.x) * q, p0.y + (p1.y - p0.y) * q, L15G(0.4 + 0.6 * q), 3); continue; }
				if (t >= dt0 + dh) continue;
				g.save(); g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = t - lt[k] < 0.05 ? PINKW : PINK; g.lineWidth = th; g.lineCap = "round"; g.beginPath(); g.moveTo(p0.x, p0.y); g.lineTo(p1.x, p1.y); g.stroke(); g.restore();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			if (t >= dt0 && t < dt0 + dh) return !inTri(p.x, p.y, p.r - 2, bt);
			if (t >= dt0 + dh) return false;
			for (int k = 0; k < 5; k++) { if (t < lt[k]) continue; var p0 = V(ord[k], bt); var p1 = V(ord[k + 1], bt); if (L15Seg(p.x, p.y, p0.x, p0.y, p1.x, p1.y) < th / 2 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- part 3: the beast ----------
	// a pendulum blade hanging at (px,py), len long: angle = down + amp sin(2 pi (beat - ph) / per). Its swing shows in grey (tw), it kills ta .. te
	public static Hz L15Blade(double px, double py, double len, double amp, double per, double ph, double R, double tw, double ta, double te)
	{
		var h = new Hz { x = px, y = py, kind = "L15Blade" };
		double ang(double t) => Math.PI / 2 + amp * JS.sin(TAU * (BT(t) - ph) / per);
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double a = ang(t), x = px + JS.cos(a) * len, y = py + JS.sin(a) * len, al = 1 - L15k(t, te, te + 0.4); bool on = t >= ta && t < te;
			g.save(); g.globalAlpha = al;
			g.strokeStyle = L15G(0.18); g.lineWidth = 2; g.setLineDash(new double[] { 8, 10 }); g.beginPath(); g.arc(px, py, len, Math.PI / 2 - amp, Math.PI / 2 + amp); g.stroke(); g.setLineDash(new double[0]);
			// the rope kills too, so it is pink while the blade is
			g.strokeStyle = on ? PINK : L15G(0.4); g.lineWidth = 6; if (on) { g.shadowColor = PINK; g.shadowBlur = 10; } g.beginPath(); g.moveTo(px, py); g.lineTo(x, y); g.stroke(); g.shadowBlur = 0;
			// the blade: a crescent across the swing
			g.save(); g.translate(x, y); g.rotate(a); g.fillStyle = on ? PINK : L15G(0.5); if (on) { g.shadowColor = PINK; g.shadowBlur = 16; }
			g.beginPath(); g.arc(-R * 0.6, 0, R * 1.4, -1.15, 1.15); g.arc(-R * 1.3, 0, R * 1.25, 0.95, -0.95, true); g.closePath(); g.fill(); g.restore();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double a = ang(t), x = px + JS.cos(a) * len, y = py + JS.sin(a) * len;
			return hypot(p.x - x, p.y - y) < R * 0.75 + p.r - 2 || L15Seg(p.x, p.y, px, py, x, y) < 3 + p.r - 2;
		};
		return h;
	}

	// the fog creeping in from a side (0 left, 1 right): its edge at kx[i] at beat kb[i] (straight between). Kills ta .. te
	public static Hz L15Creep(int side, double[] kb, double[] kx, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L15Creep" };
		double X(double bt) { if (bt <= kb[0]) return kx[0]; for (int i = 0; i < kb.Length - 1; i++) if (bt < kb[i + 1]) return kx[i] + (kx[i + 1] - kx[i]) * (bt - kb[i]) / (kb[i + 1] - kb[i]); return kx[^1]; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), x = X(bt), a = Math.Min(L15k(t, tw, tw + 0.5), 1 - L15k(t, te, te + 0.4)); bool on = t >= ta;
			double w = side == 0 ? x : W - x, x0 = side == 0 ? 0 : x; if (w <= 0) return;
			g.save(); g.globalAlpha = a; g.fillStyle = on ? "rgba(214,24,84,0.9)" : "rgba(64,56,68,0.8)"; g.fillRect(x0, 0, w, H);
			g.fillStyle = on ? "rgba(70,0,22,0.3)" : "rgba(20,16,22,0.3)"; for (int i = 0; i < 8; i++) circle(side == 0 ? x - 40 - i * 70 : x + 40 + i * 70, (i * 97 + t * 30) % H, 60);
			// clawing hands at the edge
			g.fillStyle = on ? PINK : L15G(0.5); double d = side == 0 ? 1 : -1;
			for (double y = (t * 40) % 90 - 90; y < H; y += 90) { g.beginPath(); g.moveTo(x, y); g.lineTo(x + d * 26, y + 12); g.lineTo(x + d * 8, y + 22); g.lineTo(x + d * 30, y + 34); g.lineTo(x, y + 44); g.closePath(); g.fill(); }
			g.restore();
			double nx = X(bt + 2); if (Math.Abs(nx - x) > 4) L15Dash(nx, 0, nx, H, L15P(0.45 * a), 2);
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double x = X(BT(t)); return side == 0 ? p.x - p.r + 2 < x : p.x + p.r - 2 > x; };
		return h;
	}

	// the beast's jaws round a turning crypt (sp): at each bite (bb) everything outside the strip |u - gx| < dw (u along the turned x) is
	// bitten for hold s. The strip shows from the end of the bite before (at most wn s)
	public static Hz L15Maw(double cx, double cy, L15Spin sp, double[] bb, double[] gx, double dw, double hold, double wn, double tw, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L15Maw" };
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), f = sp.At(bt), a = Math.Min(L15k(t, tw, tw + 0.5), 1 - L15k(t, te, te + 0.4));
			g.save(); g.globalAlpha = a; g.translate(cx, cy); g.rotate(f);
			// the turning floor
			g.strokeStyle = "rgba(255,255,255,0.07)"; g.lineWidth = 2; g.beginPath(); for (double u = -800; u <= 800; u += 80) { g.moveTo(u, -800); g.lineTo(u, 800); g.moveTo(-800, u); g.lineTo(800, u); } g.stroke();
			int k = 0; while (k < bb.Length && t >= bb[k] + hold) k++;
			if (k < bb.Length)
			{
				double gxk = gx[k], from = Math.Max(bb[k] - wn, k > 0 ? bb[k - 1] + hold : tw);
				if (t >= bb[k])
				{
					// shut: the two jaws meet the strip, fangs on their edges
					double u = t - bb[k]; g.fillStyle = u < 0.05 ? PINKW : PINK; g.shadowColor = PINK; g.shadowBlur = 18;
					g.fillRect(-900, -900, 900 + gxk - dw, 1800); g.fillRect(gxk + dw, -900, 900, 1800); g.shadowBlur = 0;
					g.fillStyle = "#f4e6ea"; g.beginPath(); for (double v = -800; v < 800; v += 46) { g.moveTo(gxk - dw, v); g.lineTo(gxk - dw + 16, v + 23); g.lineTo(gxk - dw, v + 46); g.moveTo(gxk + dw, v + 23); g.lineTo(gxk + dw - 16, v + 46); g.lineTo(gxk + dw, v + 69); } g.fill();
				}
				else if (t >= from)
				{
					double q = L15k(t, from, bb[k]);
					// the jaws coming in from the edges, the strip in white
					double l = -760 + (gxk - dw + 760) * q, r = 760 + (gxk + dw - 760) * q;
					g.fillStyle = L15P(0.18 + 0.3 * q); g.fillRect(-900, -900, l + 900, 1800); g.fillRect(r, -900, 900, 1800);
					g.fillStyle = $"rgba(244,230,234,{str(0.5 + 0.5 * q)})"; g.beginPath(); for (double v = -800; v < 800; v += 46) { g.moveTo(l, v); g.lineTo(l + 16, v + 23); g.lineTo(l, v + 46); g.moveTo(r, v + 23); g.lineTo(r - 16, v + 46); g.lineTo(r, v + 69); } g.fill();
					g.strokeStyle = $"rgba(255,240,245,{str(0.4 + 0.6 * q)})"; g.lineWidth = 3; g.beginPath(); g.moveTo(gxk - dw, -800); g.lineTo(gxk - dw, 800); g.moveTo(gxk + dw, -800); g.lineTo(gxk + dw, 800); g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			int k = 0; while (k < bb.Length && t >= bb[k] + hold) k++;
			if (k >= bb.Length || t < bb[k]) return false; double f = sp.At(BT(t)), u = (p.x - cx) * JS.cos(f) + (p.y - cy) * JS.sin(f);
			return Math.Abs(u - gx[k]) > dw - p.r + 2;
		};
		return h;
	}

	// a spine whip from (x,y) along angle ang, len long, w thick: dashed in grey (tw), it lashes out at ta (0.1 s) and stays hold s
	public static Hz L15Whip(double x, double y, double ang, double len, double w, double tw, double ta, double hold)
	{
		var h = new Hz { x = x, y = y, kind = "L15Whip" }; double c = JS.cos(ang), s = JS.sin(ang); bool b0 = false;
		double L(double t) => t < ta ? 0 : t < ta + 0.1 ? len * (t - ta) / 0.1 : t < ta + hold ? len : Math.Max(0, len * (1 - (t - ta - hold) / 0.12));
		h.update = (t, dt) => { if (!b0 && t >= ta + 0.1) { b0 = true; shake = Math.Max(shake, 4); } if (t > ta + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta) { double k = L15k(t, tw, ta); g.save(); g.translate(x, y); g.rotate(ang); g.fillStyle = L15P(0.06 + 0.22 * k); g.fillRect(0, -w / 2, len, w); g.restore(); L15Dash(x, y, x + c * len, y + s * len, L15G(0.3 + 0.6 * k), 2 + 2 * k); return; }
			double l = L(t); if (l <= 1) return; bool wh = t - ta < 0.12;
			g.save(); g.translate(x, y); g.rotate(ang); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = wh ? PINKW : PINK;
			for (double u = 0; u < l; u += w * 0.9) { double wv = JS.sin(u * 0.02 - t * 30) * w * 0.1 * (1 - u / len); g.fillRect(u, -w * 0.4 + wv, w * 0.75, w * 0.8); g.beginPath(); g.moveTo(u + w * 0.1, -w * 0.4 + wv); g.lineTo(u + w * 0.35, -w * 0.62 + wv); g.lineTo(u + w * 0.6, -w * 0.4 + wv); g.fill(); }
			g.shadowBlur = 0; g.restore();
		};
		h.hit = (p, t) => { double l = L(t); return t >= ta && l > 4 && L15Seg(p.x, p.y, x, y, x + c * l, y + s * l) < w / 2 + p.r - 2; };
		return h;
	}

	// the false light: two lights (paths A and B, 5 keyframes a round) in the deadly fog, in rounds of rl beats from r0. In each round they
	// split from one place and cross, and rv beats into it the false one (real[k] says which is true: 0 = A) turns to fog. The true one beats
	// on the beat, the false one between the beats
	public static Hz L15Lights(double[] kb, double[] ax, double[] ay, double[] bx, double[] by, double[] real, double r, double rv, double rl, double r0, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L15Lights" }; const int kn = 5; int rounds = real.Length;
		var PA = new L15Path[rounds]; var PB = new L15Path[rounds];
		for (int k = 0; k < rounds; k++)
		{
			var b = kb.Skip(k * kn).Take(kn).ToArray();
			PA[k] = new L15Path { b = b, x = ax.Skip(k * kn).Take(kn).ToArray(), y = ay.Skip(k * kn).Take(kn).ToArray() };
			PB[k] = new L15Path { b = b, x = bx.Skip(k * kn).Take(kn).ToArray(), y = by.Skip(k * kn).Take(kn).ToArray() };
		}
		List<(double x, double y, double r)> holes(double bt, out int k, out bool revealed)
		{
			k = (int)clamp(Math.Floor((bt - r0) / rl), 0, rounds - 1); double u = bt - r0 - k * rl; revealed = u >= rv;
			var a = PA[k].At(bt); var b = PB[k].At(bt); var l = new List<(double, double, double)>();
			if (!revealed || real[k] == 0) l.Add((a.x, a.y, r)); if (!revealed || real[k] == 1) l.Add((b.x, b.y, r)); return l;
		}
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), a = Math.Min(L15k(t, tw, tw + 0.6), 1 - L15k(t, te, te + 0.3)); var l = holes(bt, out int k, out bool rev);
			// the beat each light keeps: the true one on the beat, the false one between
			double on = JS.exp(-(((bt % 1) + 1) % 1) * 6), off = JS.exp(-((((bt + 0.5) % 1) + 1) % 1) * 6);
			L15FogDraw(t, a, t >= ta, l);
			if (t >= te) return;
			g.save(); g.globalAlpha = a;
			var pa = PA[k].At(bt); var pb = PB[k].At(bt);
			for (int i = 0; i < 2; i++)
			{
				bool tr = real[k] == i; var c = i == 0 ? pa : pb;
				if (rev && !tr) { double v = bt - r0 - k * rl - rv; if (v < 0.5) { g.fillStyle = v < 0.08 ? PINKW : PINK; circle(c.x, c.y, r * (1 - v * 1.6)); } continue; }
				double pz = tr ? on : off; g.strokeStyle = $"rgba(255,230,210,{str(0.35 + 0.6 * pz)})"; g.lineWidth = 2 + 6 * pz; g.beginPath(); g.arc(c.x, c.y, Math.Max(4, r - 6 - 10 * pz), 0, TAU); g.stroke();
				g.fillStyle = $"rgba(255,225,200,{str(0.06 + 0.12 * pz)})"; circle(c.x, c.y, r * 0.5 + 8 * pz);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; var l = holes(BT(t), out _, out _);
			foreach (var c in l) if (hypot(p.x - c.x, p.y - c.y) <= c.r - p.r + 2) return false;
			return true;
		};
		return h;
	}

	// ---------- the crypt itself (harmless) ----------
	// part 0: dark, 1: the crypt, 2: the ritual circle, 3: the beast's den
	public static Hz L15Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "L15Stage", veil = true };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			g.fillStyle = part == 3 ? $"rgba(48,0,8,{str(0.5 + 0.15 * pulse)})" : "rgba(26,6,10,0.6)"; g.fillRect(0, 0, W, H);
			// stone slabs
			g.strokeStyle = "rgba(0,0,0,0.45)"; g.lineWidth = 3; g.beginPath(); for (double y = 0; y <= H; y += 90) { g.moveTo(0, y); g.lineTo(W, y); } for (double x = 0; x <= W; x += 160) for (double y = 0; y < H; y += 90) { double o = (y / 90) % 2 * 80; g.moveTo(x + o, y); g.lineTo(x + o, y + 90); } g.stroke();
			if (part == 2)
			{
				double r = BT(t) * 0.05; g.strokeStyle = $"rgba(255,42,109,{str(0.08 + 0.08 * pulse)})"; g.lineWidth = 3;
				g.beginPath(); g.arc(W / 2, H / 2, 330, 0, TAU); g.stroke(); g.beginPath(); g.arc(W / 2, H / 2, 300, 0, TAU); g.stroke();
				g.beginPath(); for (int i = 0; i < 24; i++) { double q = r + i * TAU / 24; g.moveTo(W / 2 + JS.cos(q) * 300, H / 2 + JS.sin(q) * 300); g.lineTo(W / 2 + JS.cos(q + 0.1) * 330, H / 2 + JS.sin(q + 0.1) * 330); } g.stroke();
			}
			if (part == 3) L15SkullDraw(W / 2, H / 2 - 20, 300, 0, $"rgba(0,0,0,{str(0.35 + 0.1 * pulse)})", 0.15 + 0.2 * pulse);
			// the dark round the edges
			var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.75)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// the end: the beast's skull cracks (t0 .. t1) and breaks apart
	public static Hz L15Shatter(double t0, double t1)
	{
		var h = new Hz { kind = "L15Shatter" }; bool b0 = false;
		h.update = (t, dt) => { if (!b0 && t >= t1) { b0 = true; burst(W / 2, H / 2, "#f4e6ea", 40, 700); burst(W / 2, H / 2, PINK, 30, 500); } if (t > t1 + 2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = L15k(t, t0, t1);
			if (t < t1)
			{
				L15SkullDraw(W / 2, H / 2 - 10, 120 + 30 * k, 0, $"rgba(244,230,234,{str(0.3 + 0.5 * k)})", k);
				g.strokeStyle = "rgba(20,0,6,0.9)"; g.lineWidth = 3; g.beginPath();
				for (int i = 0; i < 7; i++) { double a = i * 0.9 + 0.3, l = 150 * k; g.moveTo(W / 2, H / 2 - 40); g.lineTo(W / 2 + JS.cos(a) * l * 0.5, H / 2 - 40 + JS.sin(a) * l * 0.5); g.lineTo(W / 2 + JS.cos(a + 0.25) * l, H / 2 - 40 + JS.sin(a + 0.25) * l); }
				g.stroke(); return;
			}
			double u = t - t1; g.fillStyle = $"rgba(244,230,234,{str(Math.Max(0, 0.9 - u))})";
			for (int i = 0; i < 14; i++) { double a = i * TAU / 14 + 0.2, d = 60 + u * 520; g.save(); g.translate(W / 2 + JS.cos(a) * d, H / 2 + JS.sin(a) * d); g.rotate(u * (3 + i)); g.fillRect(-18, -12, 36, 24); g.restore(); }
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_L15.json) ----------
	// times are beats; warnings (wn) are Normal's, Easy stretches them here. An event fires at its start minus Easy's longest warning
	static double L15wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double L15ez(double v, double ez) => easy ? v * ez : v;
	static double[] L15Sec(double[] bs) => bs.Select(B).ToArray();

	// white and shake at a part's start, fading out over dur seconds (2 at most)
	static readonly int _k15fx = DodgeKind("L15fx", e => () =>
	{
		double v = e.num("v", 0.3), s = e.num("s", 10), dur = Math.Min(2, e.num("dur", 2)), t0 = B(e.b); var h = new Hz { kind = "L15fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		hazards.Add(h);
	});
	static readonly int _k15gloom = DodgeKind("L15gloom", e => () => { gloomT = e.num("v"); });
	static readonly int _k15stage = DodgeKind("L15stage", e => () => hazards.Add(L15Stage(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _k15shatter = DodgeKind("L15shatter", e => () => hazards.Add(L15Shatter(B(e.num("t0")), B(e.num("t1")))));
	static readonly int _k15lantern = DodgeKind("L15lantern", e => { double wn = L15wn(e); return () =>
	{
		var P = new L15Path { b = e.nums("kb"), x = e.nums("kx"), y = e.nums("ky"), r = e.nums("kr").Select(v => L15ez(v, 1.25)).ToArray() };
		hazards.Add(L15Lantern(P, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k15ribs = DodgeKind("L15ribs", e => { double wn = L15wn(e); return () =>
		hazards.Add(L15Ribs(e.i("dir", 1), e.nums("gy"), L15ez(e.num("gh"), 1.35), B(e.num("hit") - wn), B(e.num("hit")), e.num("dur") * SPB, L15ez(e.num("hold"), 0.8) * SPB)); });
	static readonly int _k15floor = DodgeKind("L15floor", e => { double wn = L15wn(e); return () =>
		hazards.Add(L15Floor(e.i("cols"), e.i("rows"), e.nums("ord"), B(e.num("hit") - 2), B(e.num("hit")), e.num("rate") * SPB, L15ez(e.num("dn"), 0.6) * SPB, wn * SPB, B(e.num("to")))); });
	static readonly int _k15cells = DodgeKind("L15cells", e => { double wn = L15wn(e); return () =>
	{
		var pt = e.nums("pt"); var ps = e.nums("ps"); var pe = e.nums("pe");
		// Easy: no echoes
		if (easy) { var keep = Enumerable.Range(0, pt.Length).Where(i => pe.Length <= i || pe[i] == 0).ToArray(); pt = keep.Select(i => pt[i]).ToArray(); ps = keep.Select(i => ps[i]).ToArray(); pe = keep.Select(i => 0.0).ToArray(); }
		hazards.Add(L15Cells(e.i("cols"), e.i("rows"), L15Sec(pt), ps, pe, L15ez(e.num("hold"), 0.8) * SPB, wn * SPB, B(e.num("hit") - 2), B(e.num("to"))));
	}; });
	static readonly int _k15arms = DodgeKind("L15arms", e => { double wn = L15wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.75 : 1;
		var sp = new L15Spin(e.num("a0") * Math.PI / 180, hit - wn - 1, e.num("to") + 1, e.nums("sb"), e.nums("sw").Select(v => v * sl).ToArray());
		hazards.Add(L15Arms(e.num("x", W / 2), e.num("y", H / 2), e.i("n"), e.num("r0"), e.num("len"), L15ez(e.num("th"), 0.8), sp, B(hit - wn), B(hit), B(e.num("to"))));
	}; });
	static readonly int _k15blast = DodgeKind("L15blast", e => { double wn = L15wn(e); return () =>
	{
		if (easy && e.flag("echo")) return;
		hazards.Add(L15Blast(e.num("x"), e.num("y"), L15ez(e.num("R"), 0.8), B(e.num("hit") - wn), B(e.num("hit")), e.num("hold", 0.3) * SPB, e.flag("echo")));
	}; });
	static readonly int _k15curse = DodgeKind("L15curse", e => () =>
		hazards.Add(L15Curse(B(e.num("hit")), B(e.num("to")), easy ? 1.1 : e.num("dl"), easy ? 1.2 : e.num("lf"), L15ez(e.num("R"), 0.8))));
	static readonly int _k15rings = DodgeKind("L15rings", e => { double wn = L15wn(e); return () =>
	{
		double sl = easy ? 0.75 : 1;
		hazards.Add(L15Rings(e.num("x", W / 2), e.num("y", H / 2), e.nums("rr"), e.num("rw"), L15ez(e.num("dw"), 1.4) * Math.PI / 180, e.nums("ws").Select(v => v * sl).ToArray(), e.nums("a0").Select(v => v * Math.PI / 180).ToArray(),
			e.nums("fb"), e.nums("fr"), e.nums("cb"), e.num("cR"), e.num("ch"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _k15star = DodgeKind("L15star", e => { double wn = L15wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.7 : 1, d0 = e.num("det");
		var sp = new L15Spin(e.num("a0") * Math.PI / 180, hit - 4, d0 + 2, e.nums("sb"), e.nums("sw").Select(v => v * sl).ToArray());
		hazards.Add(L15Star(e.num("x", W / 2), e.num("y", H / 2), e.num("R"), sp, L15Sec(e.nums("lt")), wn * SPB, e.i("sv"), B(d0), e.num("dh") * SPB, B(hit - wn), L15ez(e.num("th"), 0.8)));
	}; });
	static readonly int _k15blade = DodgeKind("L15blade", e => { double wn = L15wn(e); return () =>
		hazards.Add(L15Blade(e.num("x"), e.num("y"), e.num("len"), e.num("amp") * Math.PI / 180, e.num("per"), e.num("ph"), L15ez(e.num("R"), 0.8), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")))); });
	static readonly int _k15creep = DodgeKind("L15creep", e => { double wn = L15wn(e); return () =>
		hazards.Add(L15Creep(e.i("side"), e.nums("kb"), e.nums("kx"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")))); });
	static readonly int _k15maw = DodgeKind("L15maw", e => { double wn = L15wn(e); return () =>
	{
		double hit = e.num("hit"), sl = easy ? 0.7 : 1;
		var sp = new L15Spin(e.num("a0") * Math.PI / 180, hit - 4, e.num("to") + 1, e.nums("sb"), e.nums("sw").Select(v => v * sl).ToArray());
		hazards.Add(L15Maw(e.num("x", W / 2), e.num("y", H / 2), sp, L15Sec(e.nums("bb")), e.nums("gx"), L15ez(e.num("dw"), 1.4), L15ez(e.num("hold"), 0.8) * SPB, wn * SPB, B(hit - 2), B(e.num("to"))));
	}; });
	static readonly int _k15whip = DodgeKind("L15whip", e => { double wn = L15wn(e); return () =>
	{
		if (easy && e.flag("ezskip")) return;
		hazards.Add(L15Whip(e.num("x"), e.num("y"), e.num("ang") * Math.PI / 180, e.num("len"), L15ez(e.num("w"), 0.75), B(e.num("hit") - wn), B(e.num("hit")), e.num("hold", 0.4) * SPB));
	}; });
	static readonly int _k15lights = DodgeKind("L15lights", e => { double wn = L15wn(e); return () =>
		hazards.Add(L15Lights(e.nums("kb"), e.nums("ax"), e.nums("ay"), e.nums("bx"), e.nums("by"), e.nums("real"), L15ez(e.num("r"), 1.25), e.num("rv"), e.num("rl"), e.num("r0"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to")))); });
}
