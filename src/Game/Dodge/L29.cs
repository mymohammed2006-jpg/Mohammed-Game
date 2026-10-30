using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Trouble (Taylor Swift / Justin Bieber, Mike Tompkins a cappella mashup), main level 29: "trouble walked in".
// Part 1 he walks in (the knocks on the door, the giant's footsteps, the red flags and the warnings),
// part 2 he flies you away (the paper planes, the pins and the routes on the map, the turning globe) and puts you down (the falling weights),
// part 3 trouble (the alarm: the siren and the warning tape, 1:36.8-1:49.2 as hard as level 4), the heart beats, the doors slam, the door closes.
// The level is the song from 0:06.000 (songs/SONG_L29.mp3 starts there). Grid: 154 BPM, beat 0 = 0.371 s of the file (0:06.371 of the song),
// it ends on beat 360 (2:26.6). Every hazard is a pure function of time once it exists. Only pink hurts: white, grey and amber never do.
public static partial class G
{
	static double TBk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string TBP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string TBW(double a) => $"rgba(240,236,232,{str(clamp(a, 0, 1))})";
	static string TBY(double a) => $"rgba(255,196,84,{str(clamp(a, 0, 1))})";
	static double TBOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double TBEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double TBRad(double d) => d * Math.PI / 180;
	static double TBH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
	static double TBSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// how far (px) a point is from a triangle (0 inside)
	static double TBTri(double px, double py, double ax, double ay, double bx, double by, double cx, double cy)
	{
		double s1 = (bx - ax) * (py - ay) - (by - ay) * (px - ax), s2 = (cx - bx) * (py - by) - (cy - by) * (px - bx), s3 = (ax - cx) * (py - cy) - (ay - cy) * (px - cx);
		if ((s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0)) return 0;
		return Math.Min(TBSeg(px, py, ax, ay, bx, by), Math.Min(TBSeg(px, py, bx, by, cx, cy), TBSeg(px, py, cx, cy, ax, ay)));
	}
	static void TBDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	// the integral (degrees) of a speed (degrees a beat) that goes in straight lines between (kb[i], kw[i]), from kb[0] to bt
	static double TBTurn(double[] kb, double[] kw, double bt)
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
	public static Hz TBFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "TBFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- the door, the heart and the siren's body (harmless: white and amber) ----------
	static void TBDoor(double cx, double by, double w, double hh, double open, double a)
	{
		if (a <= 0.01) return;
		g.save(); g.globalAlpha = a;
		// the light behind the open door
		if (open > 0.01) { var lg = g.createLinearGradient(cx, by - hh, cx, by); lg.addColorStop(0, TBY(0.10 + 0.25 * open)); lg.addColorStop(1, TBY(0.03)); g.fillStyle = lg; g.fillRect(cx - w / 2, by - hh, w, hh); }
		g.fillStyle = "rgba(10,10,18,1)"; double dw = w * (1 - 0.82 * open);
		g.fillRect(cx - w / 2, by - hh, dw, hh);
		g.strokeStyle = TBW(0.55); g.lineWidth = 4; g.strokeRect(cx - w / 2 - 6, by - hh - 6, w + 12, hh + 6);
		g.strokeStyle = TBW(0.3); g.lineWidth = 2; g.strokeRect(cx - w / 2 + 12, by - hh + 14, dw - 24 > 4 ? dw - 24 : 4, hh * 0.38); g.strokeRect(cx - w / 2 + 12, by - hh * 0.52, dw - 24 > 4 ? dw - 24 : 4, hh * 0.44);
		g.fillStyle = TBY(0.8); circle(cx - w / 2 + dw - 16, by - hh * 0.5, 5);
		g.restore();
	}
	static void TBHeartPath(double x, double y, double s)
	{
		g.beginPath(); g.moveTo(x, y + 0.9 * s);
		g.bezierCurveTo(x - 1.25 * s, y + 0.05 * s, x - 0.95 * s, y - 0.95 * s, x, y - 0.42 * s);
		g.bezierCurveTo(x + 0.95 * s, y - 0.95 * s, x + 1.25 * s, y + 0.05 * s, x, y + 0.9 * s); g.closePath();
	}

	// ---------- the stage (harmless, under everything) ----------
	// part 1 a dark room with the door at the top, part 2 the night sky over a dotted map, part 3 the alarm (amber), then the heart
	public static Hz TBStage(double t1)
	{
		var h = new Hz { kind = "TBStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6);
			double p2 = TBk(bt, 102, 106) * (1 - TBk(bt, 230, 233)), al = TBk(bt, 231, 232.5) * (1 - TBk(bt, 263, 265)), p3 = TBk(bt, 263, 266);
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			double r0 = 16 + 4 * p2 + 30 * al + 18 * p3, g0 = 12 + 16 * p2 + 12 * al + 4 * p3, b0 = 26 + 30 * p2 - 10 * al + 6 * p3;
			gr.addColorStop(0, $"rgba({str(r0)},{str(g0)},{str(b0)},1)"); gr.addColorStop(1, $"rgba({str(r0 * 0.3)},{str(g0 * 0.3)},{str(b0 * 0.4)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// soft light beams from the top (looks only), they lean with the beat
			double pulse = 0.5 + 0.5 * JS.cos(TAU * (bt % 4) / 4);
			for (int i = 0; i < 4; i++)
			{
				double x = 160 + i * 320 + 40 * JS.sin(bt * 0.11 + i), sw = 90 + 30 * pulse;
				var bg = g.createLinearGradient(x, 0, x, H); bg.addColorStop(0, al > 0 ? TBY(0.07 * al + 0.03) : TBW(0.045)); bg.addColorStop(1, TBW(0));
				g.fillStyle = bg; g.beginPath(); g.moveTo(x - 20, 0); g.lineTo(x + 20, 0); g.lineTo(x + sw, H); g.lineTo(x - sw, H); g.closePath(); g.fill();
			}
			// part 2: the dotted map and the clouds
			if (p2 > 0)
			{
				g.fillStyle = TBW(0.10 * p2);
				for (int i = 0; i < 40; i++) for (int j = 0; j < 22; j++)
					{
						double xx = 16 + i * 32, yy = 16 + j * 32, n = JS.sin(xx * 0.008 + 1.3) + JS.cos(yy * 0.011 - 0.7) + JS.sin((xx + yy) * 0.004);
						if (n > 0.55) circle(xx, yy, 1.6);
					}
				for (int i = 0; i < 4; i++)
				{
					double cx = (i * 377.7 + t * (14 + 4 * i)) % (W + 500) - 250, cy = 100 + i * 160, rr = 210 + 30 * i;
					var cg = g.createRadialGradient(cx, cy, 10, cx, cy, rr); cg.addColorStop(0, TBW(0.06 * p2)); cg.addColorStop(1, TBW(0));
					g.fillStyle = cg; g.fillRect(cx - rr, cy - rr, 2 * rr, 2 * rr);
				}
			}
			// part 3's alarm: amber light turning with the siren
			if (al > 0)
			{
				double q = TBRad(TBSirenAng(bt));
				for (int k = 0; k < 2; k++)
				{
					double qa = q + k * Math.PI + Math.PI / 2;
					var lg = g.createRadialGradient(W / 2, H / 2, 40, W / 2, H / 2, 820); lg.addColorStop(0, TBY(0.16 * al)); lg.addColorStop(1, TBY(0));
					g.fillStyle = lg; g.beginPath(); g.moveTo(W / 2, H / 2); g.arc(W / 2, H / 2, 820, qa - 0.35, qa + 0.35); g.closePath(); g.fill();
				}
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.55)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
			// the door (part 1: knocks then opens), the heart (part 3), the siren body
			double da = TBk(bt, -2, 0) * (1 - TBk(bt, 100, 104));
			if (da > 0) TBDoor(W / 2, 170, 130, 160, TBEase((bt - 7) / 1.2), Math.Min(1, da) * a);
			double ha = TBk(bt, 263, 265) * (1 - TBk(bt, 286, 288)) + TBk(bt, 326, 328) * (1 - TBk(bt, 351, 353));
			if (ha > 0)
			{
				double beat = bt % 4, sc = 1 + 0.08 * Math.Max(0, 1 - beat / 0.5) + 0.06 * Math.Max(0, 1 - Math.Abs(beat - 1.5) / 0.4);
				g.save(); g.globalAlpha = ha * a; TBHeartPath(W / 2, H / 2 - 10, 62 * sc); g.fillStyle = "rgba(30,14,26,1)"; g.fill(); g.strokeStyle = TBW(0.75); g.lineWidth = 4; g.stroke(); g.restore();
			}
			if (al > 0)
			{
				g.save(); g.globalAlpha = al * a;
				g.fillStyle = "rgba(34,26,18,1)"; circle(W / 2, H / 2, 62); g.strokeStyle = TBY(0.9); g.lineWidth = 5; g.beginPath(); g.arc(W / 2, H / 2, 62, 0, TAU); g.stroke();
				double fl = 0.5 + 0.5 * JS.cos(TAU * bt / 2); g.fillStyle = TBY(0.45 + 0.4 * fl); circle(W / 2, H / 2, 30);
				g.restore();
			}
		};
		return h;
	}

	// ---------- a ring that grows from (cx,cy): pink, width w, sp px/s from beat b; gapW (radians) > 0 leaves a gap round gapA ----------
	// Before b: a white ring at the start and, with a gap, two white dashed lines that show where the gap will be
	public static Hz TBRing(double cx, double cy, double b, double tl, double sp, double w, double gapA = 0, double gapW = 0, double r0 = 30, double rMax = 1500)
	{
		var h = new Hz { kind = "TBRing" }; double t0 = B(b);
		double rad(double t) => r0 + (t - t0) * sp;
		h.update = (t, dt) => { if (t > t0 && rad(t) > rMax) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save(); g.lineCap = "butt";
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b);
				g.strokeStyle = TBW(0.3 + 0.6 * q); g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, r0 + 12 * (1 - q), 0, TAU); g.stroke();
				if (gapW > 0) foreach (var s in new[] { -1, 1 }) { double qa = gapA + s * gapW / 2; TBDash(cx + JS.cos(qa) * (r0 + 20), cy + JS.sin(qa) * (r0 + 20), cx + JS.cos(qa) * 900, cy + JS.sin(qa) * 900, TBW(0.18 + 0.5 * q), 2); }
			}
			else
			{
				double r = rad(t); g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = w;
				g.beginPath(); if (gapW > 0) g.arc(cx, cy, r, gapA + gapW / 2, gapA - gapW / 2 + TAU); else g.arc(cx, cy, r, 0, TAU); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double r = rad(t), d = hypot(p.x - cx, p.y - cy);
			if (Math.Abs(d - r) >= w / 2 + p.r - 2) return false;
			if (gapW > 0)
			{
				double da = Math.Abs(((Math.Atan2(p.y - cy, p.x - cx) - gapA) % TAU + 3 * Math.PI) % TAU - Math.PI);
				// the ends of the arc are round-free (butt), so a point is safe when its whole circle is inside the gap
				double m = Math.Asin(clamp((p.r - 2) / Math.Max(r, 1), 0, 1));
				if (da < gapW / 2 - m) return false;
			}
			return true;
		};
		return h;
	}

	// ---------- 1. the footsteps (0:09.5): a giant shoe print stamps at (x,y) facing ang on beat b ----------
	// Its outline shows dashed tl beats before; it is pink for hold beats (it lands 1.25 times bigger and settles in 0.1 beat), then a grey
	// print that fades. ringR > 0: the stamp also sends a thin pink ring out to ringR in 0.5 beat.
	static bool TBFootIn(double px, double py, double x, double y, double ang, double s, double m, bool left)
	{
		double c = JS.cos(ang), sn = JS.sin(ang), dx = px - x, dy = py - y, u = (dx * c + dy * sn) / s, v = (-dx * sn + dy * c) / s * (left ? -1 : 1); m /= s;
		double a = (u - 14) / (46 + m), b = (v - 3) / (22 + m); if (a * a + b * b < 1) return true;
		a = (u + 40) / (20 + m); b = (v + 2) / (18 + m); return a * a + b * b < 1;
	}
	static void TBFootPath(double x, double y, double ang, double s, bool left)
	{
		g.save(); g.translate(x, y); g.rotate(ang); g.scale(s, left ? -s : s);
		g.beginPath(); g.ellipse(14, 3, 46, 22, 0, 0, TAU); g.moveTo(-20, -2); g.ellipse(-40, -2, 20, 18, 0, 0, TAU);
		g.restore();
	}
	public static Hz TBStep(double x, double y, double ang, double s, bool left, double b, double tl, double hold, double ringR)
	{
		var h = new Hz { kind = "TBStep" }; double t0 = B(b);
		double sc(double bt) => s * (1 + 0.25 * (1 - TBOut((bt - b) / 0.1)));
		h.update = (t, dt) => { if (BT(t) > b + hold + 2.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b); TBFootPath(x, y, ang, s, left);
				g.fillStyle = TBW(0.04 + 0.08 * q); g.fill(); g.strokeStyle = TBW(0.35 + 0.55 * q); g.lineWidth = 2.5; g.setLineDash(new double[] { 9, 7 }); g.stroke(); g.setLineDash(new double[0]);
			}
			else if (bt < b + hold)
			{
				TBFootPath(x, y, ang, sc(bt), left); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; g.fill();
			}
			else { TBFootPath(x, y, ang, s, left); g.fillStyle = $"rgba(150,146,170,{str(0.28 * (1 - TBk(bt, b + hold, b + hold + 2.5)))})"; g.fill(); }
			if (ringR > 0 && bt >= b) { double r = 40 + (ringR - 40) * TBOut((bt - b) / 0.5); if (bt < b + 0.5) { g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = 7; g.beginPath(); g.arc(x, y, r, 0, TAU); g.stroke(); } }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false;
			if (bt < b + hold && TBFootIn(p.x, p.y, x, y, ang, sc(bt), p.r - 2, left)) return true;
			if (ringR > 0 && bt < b + 0.5) { double r = 40 + (ringR - 40) * TBOut((bt - b) / 0.5); if (Math.Abs(hypot(p.x - x, p.y - y) - r) < 3.5 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// ---------- 2. the red flags (0:20.4): a string of pennants crosses the screen ----------
	// vertical = false: the string spans the width and moves down (dir 1) or up (-1); true: it spans the height and moves right (1) or left (-1).
	// It sags in the middle by sag px. It goes from off one edge (at b0) to off the other (at b1): 55% steady, 45% in jumps on the beats in jb.
	// Pennants (pink triangles, 50 wide, 54 long, pointing the way it goes) hang every 62 px except in the gaps (centres gx, width gw).
	// The string itself (white) never hurts. It shows dashed at its edge 1 beat before b0.
	public static Hz TBBunting(bool vertical, int dir, double b0, double b1, double[] jb, double[] gx, double gw, double sag)
	{
		var h = new Hz { kind = "TBBunting" }; double L = vertical ? H : W, D = vertical ? W : H; const double PS = 62, PW = 50, PL = 54;
		double prog(double bt)
		{
			double u = clamp((bt - b0) / (b1 - b0), 0, 1), k = 0;
			if (jb.Length > 0) { foreach (var j in jb) k += TBOut((bt - j) / 0.18); k /= jb.Length; } else k = u;
			return 0.55 * u + 0.45 * k;
		}
		double pos(double bt) { double a = dir > 0 ? -90 - sag : D + 90, e = dir > 0 ? D + 90 : -90 - sag; return a + (e - a) * prog(bt); }
		double sg(double s) => sag * 4 * (s / L) * (1 - s / L);
		(double, double) P(double s, double off) => vertical ? (off, s) : (s, off);
		var pen = new List<double>(); for (double s = PS / 2; s < L; s += PS) { bool gap = false; foreach (var c in gx) if (Math.Abs(s - c) < gw / 2 + PW / 2) gap = true; if (!gap) pen.Add(s); }
		void tri(double bt, Action<double, double, double, double, double, double> f)
		{
			double o = pos(bt);
			foreach (var s in pen)
			{
				double o0 = o + sg(s - PW / 2), o1 = o + sg(s + PW / 2), om = o + sg(s);
				var (ax, ay) = P(s - PW / 2, o0); var (bx, by) = P(s + PW / 2, o1); var (cx, cy) = P(s, om + dir * PL);
				f(ax, ay, bx, by, cx, cy);
			}
		}
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return;
			g.save();
			if (bt < b0)
			{
				double q = TBk(bt, b0 - 1, b0), e = dir > 0 ? 6 : D - 6;
				var (x0, y0) = P(0, e); var (x1, y1) = P(L, e); TBDash(x0, y0, x1, y1, TBW(0.3 + 0.6 * q), 3);
				foreach (var c in gx) { var (gx0, gy0) = P(c - gw / 2, e); var (gx1, gy1) = P(c + gw / 2, e); g.strokeStyle = TBW(0.4 + 0.5 * q); g.lineWidth = 6; g.beginPath(); g.moveTo(gx0, gy0); g.lineTo(gx1, gy1); g.stroke(); }
				g.restore(); return;
			}
			double o = pos(bt);
			g.strokeStyle = TBW(0.75); g.lineWidth = 2; g.beginPath();
			for (int i = 0; i <= 32; i++) { double s = L * i / 32; var (x, y) = P(s, o + sg(s)); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
			g.stroke();
			g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
			tri(bt, (ax, ay, bx, by, cx, cy) => { g.beginPath(); g.moveTo(ax, ay); g.lineTo(bx, by); g.lineTo(cx, cy); g.closePath(); g.fill(); });
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0) return false; bool hit = false;
			tri(bt, (ax, ay, bx, by, cx, cy) => { if (!hit && TBTri(p.x, p.y, ax, ay, bx, by, cx, cy) < p.r - 2) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- 3. the warning: a pink "!" shoots up from the bottom at x on beat b (width w), stays hold beats, drops back ----------
	// Its column shows dashed with a small white "!" tl beats before.
	public static Hz TBBang(double x, double b, double tl, double hold, double w)
	{
		var h = new Hz { kind = "TBBang" };
		double top(double bt) => bt < b ? H : bt < b + hold ? H - H * TBOut((bt - b) / 0.2) : H - H * (1 - TBk(bt, b + hold, b + hold + 0.2));
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.25) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b);
				g.strokeStyle = TBW(0.3 + 0.55 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.strokeRect(x - w / 2, 0, w, H); g.setLineDash(new double[0]);
				g.fillStyle = TBW(0.5 + 0.5 * q); g.fillRect(x - 5, H - 70, 10, 34); g.fillRect(x - 5, H - 28, 10, 10);
			}
			else
			{
				double tp = top(bt); if (tp >= H - 1) { g.restore(); return; }
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
				// the bar of the "!" and its dot (a 24 px break between them)
				double dotT = H - 62; if (tp < dotT) { g.fillRect(x - w / 2, tp, w, dotT - 24 - tp > 0 ? dotT - 24 - tp : 0); g.fillRect(x - w / 2, dotT, w, H - dotT); }
				else g.fillRect(x - w / 2, tp, w, H - tp);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false; double tp = top(bt); if (tp >= H - 1) return false;
			if (Math.Abs(p.x - x) >= w / 2 + p.r - 2) return false;
			double dotT = H - 62;
			if (tp < dotT) return p.y > dotT - (p.r - 2) || (p.y > tp - (p.r - 2) && p.y < dotT - 24 + (p.r - 2));
			return p.y > tp - (p.r - 2);
		};
		return h;
	}
}
