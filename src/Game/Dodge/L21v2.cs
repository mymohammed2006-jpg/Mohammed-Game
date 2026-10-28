using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Barracuda (Noisestorm), main level 21, the new design (10-28): "the submarine Barracuda hunts you". You are a small sub in the deep;
// the attack submarine Barracuda finds you, shoots at you, passes over you and comes for you.
// part 1 found (the sonar arm and its contacts, the depth charges on the bass, the mine net on the snare roll, the dive through the hull's frames),
// part 2 the torpedoes (salvos on the bass, the giant propeller passing over you, the crossfire and the pings, the ballast columns),
// part 3 Barracuda itself (it rams through you and fires its tubes, it locks on you, its propeller stutters on the riff, everything at once, the last pings).
// Every hazard is a pure function of time once it exists (the aimed ones also of where the square was when they locked). Only pink hurts;
// warnings are white dashed lines. Song timing: my own onset map of the song (bass, kick, snare bands) on the grid 129.7 BPM, beat 0 = 0.581 s.
// The old design (L21.cs, L21b.cs, L21c.cs, L21data.cs) is kept as it was for the old replays (its builder is "L21_v51").
public static partial class G
{
	static double L21bK(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L21bP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L21bW(double a) => $"rgba(255,255,255,{str(clamp(a, 0, 1))})";
	static double L21bEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static void L21bDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double L21bSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// a value along keys (beats kb, values kv), straight between them
	static double L21bAt(double bt, double[] kb, double[] kv)
	{
		if (bt <= kb[0]) return kv[0];
		for (int i = 0; i + 1 < kb.Length; i++) if (bt < kb[i + 1]) return kv[i] + (kv[i + 1] - kv[i]) * (bt - kb[i]) / (kb[i + 1] - kb[i]);
		return kv[^1];
	}
	// a pink capsule (round ends) from (x0,y0) to (x1,y1), w wide
	static void L21bCap(double x0, double y0, double x1, double y1, double w)
	{
		g.lineCap = "round"; g.lineWidth = w; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.lineCap = "butt";
	}
	// a sea mine: a ball with six spikes (all of it inside r: the spikes are the edge)
	static void L21bMine(double x, double y, double r, double spin)
	{
		g.beginPath();
		for (int i = 0; i < 12; i++) { double a = spin + i * Math.PI / 6, rr = i % 2 == 0 ? r : r * 0.72; if (i == 0) g.moveTo(x + rr * JS.cos(a), y + rr * JS.sin(a)); else g.lineTo(x + rr * JS.cos(a), y + rr * JS.sin(a)); }
		g.closePath(); g.fill();
		g.fillStyle = "rgba(255,170,200,0.7)"; circle(x, y, r * 0.3); g.fillStyle = PINK;
	}

	// ---------- the sea as a sonar screen (harmless, under everything) ----------
	public static Hz L21bStage(double t1)
	{
		var h = new Hz { kind = "L21bStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.8), p2 = L21bK(bt, 95.5, 96.5), p3 = L21bK(bt, 159, 161) * (1 - L21bK(bt, 223.5, 226));
			g.save(); g.globalAlpha = a;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(3 + 22 * p3)},{str(20 + 4 * p2 - 16 * p3)},{str(30 + 6 * p2 - 20 * p3)},1)");
			gr.addColorStop(1, $"rgba({str(1 + 8 * p3)},{str(6 - 3 * p3)},{str(11 - 6 * p3)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// the sonar's rings and lines (the screen of the hunter)
			g.strokeStyle = p3 > 0 ? $"rgba({str(45 + 190 * p3)},{str(226 - 170 * p3)},{str(230 - 160 * p3)},{str(0.07 + 0.03 * pulse)})" : $"rgba(45,226,230,{str(0.07 + 0.03 * pulse)})"; g.lineWidth = 1.5;
			for (int i = 1; i <= 5; i++) { g.beginPath(); g.arc(W / 2, H / 2, i * 140, 0, TAU); g.stroke(); }
			g.beginPath(); g.moveTo(0, H / 2); g.lineTo(W, H / 2); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke();
			for (int i = 0; i < 72; i++) { double an = i * TAU / 72, r0 = i % 6 == 0 ? 318 : 330; g.beginPath(); g.moveTo(W / 2 + r0 * JS.cos(an), H / 2 + r0 * JS.sin(an)); g.lineTo(W / 2 + 340 * JS.cos(an), H / 2 + 340 * JS.sin(an)); g.stroke(); }
			// plankton going up (white, small, faint)
			g.fillStyle = "rgba(200,240,255,0.14)";
			for (int i = 0; i < 40; i++) { double bx = (i * 331.7) % W + 8 * JS.sin(t * 1.1 + i), sp = 25 + (i * 37) % 50, by = H + 20 - ((i * 211.3 + t * sp) % (H + 40)); circle(bx, by, i % 5 == 0 ? 2.6 : 1.4); }
			// part 3: the hunter's red alarm light going round (faint)
			if (p3 > 0)
			{
				double an = t * 2.4; var rg = g.createRadialGradient(W / 2 + 900 * JS.cos(an), H / 2 + 900 * JS.sin(an), 0, W / 2 + 900 * JS.cos(an), H / 2 + 900 * JS.sin(an), 900);
				rg.addColorStop(0, $"rgba(255,40,40,{str(0.07 * p3)})"); rg.addColorStop(1, "rgba(255,40,40,0)"); g.fillStyle = rg; g.fillRect(0, 0, W, H);
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 760); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, $"rgba(0,0,0,{str(0.55 + 0.15 * p3)})"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- the sonar arm ----------
	// arms arms from (cx,cy) turning to the angle plan (beats kb, angles ka in rad). Warning (white dashed) from bIn, pink from bOn,
	// pulls back into the middle over bOff..bEnd. Where it will turn back: white arrows at its ends the beat before (rev = the beats).
	public static Hz L21bSweep(double cx, double cy, double[] kb, double[] ka, int arms, double hubR, double len, double w, double[] rev, double bIn, double bOn, double bOff, double bEnd, double wa = 0)
	{
		var h = new Hz { kind = "L21bSweep" }; double tEnd = B(bEnd);
		double rOut(double bt) => bt < bOff ? len : hubR + (len - hubR) * (1 - L21bEase(L21bK(bt, bOff, bEnd)));
		h.update = (t, dt) => { h.z = BT(t) < bOn ? -1 : 0; if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), an = L21bAt(bt, kb, ka), ro = rOut(bt);
			g.save();
			if (bt < bOn)
			{
				double k = L21bK(bt, bIn, bOn);
				for (int j = 0; j < arms; j++) { double a = an + j * TAU / arms; L21bDash(cx + hubR * JS.cos(a), cy + hubR * JS.sin(a), cx + len * JS.cos(a), cy + len * JS.sin(a), L21bW(0.3 + 0.5 * k), 2 + 2 * k); }
				g.strokeStyle = L21bW(0.5 + 0.4 * k); g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, hubR, 0, TAU); g.stroke();
				g.restore(); return;
			}
			// the turn-back warning
			foreach (var rb in rev)
			{
				if (bt < rb - 1 || bt >= rb) continue; double s = Math.Sign(L21bAt(rb + 0.1, kb, ka) - L21bAt(rb, kb, ka)), q = 1 - (rb - bt);
				g.fillStyle = L21bW(0.5 + 0.45 * q);
				for (int j = 0; j < arms; j++)
				{
					double a = an + j * TAU / arms;
					for (int m = 0; m < 3; m++)
					{
						double rr = ro * (0.45 + 0.22 * m), x = cx + rr * JS.cos(a), y = cy + rr * JS.sin(a), nx = -JS.sin(a) * s, ny = JS.cos(a) * s, ox = JS.cos(a), oy = JS.sin(a), d = 22 + 6 * q;
						g.beginPath(); g.moveTo(x + nx * (d + 18), y + ny * (d + 18)); g.lineTo(x + nx * d + ox * 11, y + ny * d + oy * 11); g.lineTo(x + nx * d - ox * 11, y + ny * d - oy * 11); g.closePath(); g.fill();
					}
				}
			}
			g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = PINK; g.fillStyle = PINK;
			for (int j = 0; j < arms; j++)
			{
				double a = an + j * TAU / arms; L21bCap(cx + hubR * 0.5 * JS.cos(a), cy + hubR * 0.5 * JS.sin(a), cx + ro * JS.cos(a), cy + ro * JS.sin(a), w);
				// the sonar's beam: a wedge wa each side of the arm (solid pink, a lighter middle)
				if (wa > 0) { g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, ro, a - wa, a + wa); g.closePath(); g.fill(); }
			}
			circle(cx, cy, hubR);
			if (wa > 0) { g.shadowBlur = 0; g.fillStyle = "rgba(255,190,210,0.35)"; for (int j = 0; j < arms; j++) { double a = an + j * TAU / arms; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, ro, a - wa * 0.25, a + wa * 0.25); g.closePath(); g.fill(); } g.fillStyle = PINK; }
			g.shadowBlur = 0; g.fillStyle = "rgba(255,200,220,0.5)"; circle(cx, cy, hubR * 0.45);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bOn || t > tEnd) return false;
			double dx = p.x - cx, dy = p.y - cy; if (dx * dx + dy * dy < (hubR + p.r - 2) * (hubR + p.r - 2)) return true;
			double an = L21bAt(bt, kb, ka), ro = rOut(bt);
			for (int j = 0; j < arms; j++)
			{
				double a = an + j * TAU / arms; if (L21bSeg(p.x, p.y, cx, cy, cx + ro * JS.cos(a), cy + ro * JS.sin(a)) < w / 2 + p.r - 2) return true;
				if (wa > 0)
				{
					double d = Math.Sqrt(dx * dx + dy * dy); if (d > ro + p.r - 2) continue;
					double da = Math.Abs(((JS.atan2(dy, dx) - a) % TAU + TAU + Math.PI) % TAU - Math.PI); if (da <= wa) return true;
					// near its straight edges
					foreach (var s in new[] { -1.0, 1 }) if (L21bSeg(p.x, p.y, cx, cy, cx + ro * JS.cos(a + s * wa), cy + ro * JS.sin(a + s * wa)) < p.r - 2) return true;
				}
			}
			return false;
		};
		return h;
	}

	// ---------- the sonar's contacts: mines that turn on where the arm reaches them ----------
	// mine i at (mx,my): a white dashed ring from mb[i]-tell, pink (and hurting) from mb[i] (it grows in over a fifth of a beat) to me[i]
	public static Hz L21bMines(double[] mx, double[] my, double[] mb, double[] me, double r, double tell)
	{
		var h = new Hz { kind = "L21bMines" }; double last = me.Max();
		double size(double bt, int i) => bt < mb[i] || bt >= me[i] ? 0 : r * (0.4 + 0.6 * L21bEase(L21bK(bt, mb[i], mb[i] + 0.2))) * (1 - 0.6 * L21bK(bt, me[i] - 0.2, me[i]));
		h.update = (t, dt) => { if (BT(t) > last + 0.1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			g.lineWidth = 2; g.setLineDash(new double[] { 5, 5 });
			for (int i = 0; i < mx.Length; i++) { if (bt < mb[i] - tell || bt >= mb[i]) continue; double k = L21bK(bt, mb[i] - tell, mb[i]); g.strokeStyle = L21bW(0.3 + 0.6 * k); g.beginPath(); g.arc(mx[i], my[i], r * (1.6 - 0.6 * k), 0, TAU); g.stroke(); }
			g.setLineDash(new double[0]);
			g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK;
			for (int i = 0; i < mx.Length; i++) { double s = size(bt, i); if (s > 0) L21bMine(mx[i], my[i], s, t * 0.8 + i); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			for (int i = 0; i < mx.Length; i++) { double s = size(bt, i); if (s <= 0) continue; double dx = p.x - mx[i], dy = p.y - my[i], q = s + p.r - 2; if (dx * dx + dy * dy < q * q) return true; }
			return false;
		};
		return h;
	}

	// ---------- the depth charge ----------
	// dropped at bDrop, it sinks to (x,y) and goes off at bHit: a blast band across the whole screen (band px high) and a ball (R) for hold beats;
	// a big one (colB > 0) leaves a column of bubbles going up from where it went off for colB beats (colW wide)
	public static Hz L21bCharge(double x, double y, double bDrop, double bHit, double band, double R, double hold, double colB = 0, double colW = 50, double wv = 0, double wth = 14)
	{
		var h = new Hz { kind = "L21bCharge" }; double tH = B(bHit), tE = Math.Max(B(bHit + Math.Max(hold, colB)), wv > 0 ? tH + (Math.Max(y, H - y) + 30) / wv : 0);
		// the blast's two shock waves (thin lines going up and down from it at wv px/s)
		double wUp(double t) => y - band / 2 - wv * (t - tH);
		double wDn(double t) => y + band / 2 + wv * (t - tH);
		double colTop(double t) => Math.Max(-30, y - 1500 * (t - tH));
		h.update = (t, dt) => { h.z = t < tH ? -1 : 0; if (t > tE + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (t < tH)
			{
				double k = L21bK(bt, bDrop, bHit);
				L21bDash(0, y - band / 2, W, y - band / 2, L21bW(0.15 + 0.45 * k), 2); L21bDash(0, y + band / 2, W, y + band / 2, L21bW(0.15 + 0.45 * k), 2);
				g.fillStyle = L21bW(0.04 + 0.08 * k); g.fillRect(0, y - band / 2, W, band);
				double s = 14 + 16 * (1 - k); g.strokeStyle = L21bW(0.6 + 0.4 * k); g.lineWidth = 3; g.beginPath(); g.moveTo(x - s, y - s); g.lineTo(x + s, y + s); g.moveTo(x + s, y - s); g.lineTo(x - s, y + s); g.stroke();
				// the barrel itself (grey, harmless) sinking
				double by = -40 + (y + 40) * k * k; g.fillStyle = "rgba(170,185,195,0.9)"; g.fillRect(x - 13, by - 18, 26, 36); g.fillStyle = "rgba(60,70,80,0.9)"; g.fillRect(x - 13, by - 8, 26, 5); g.fillRect(x - 13, by + 4, 26, 5);
				g.restore(); return;
			}
			double u = bt - bHit;
			if (u < hold)
			{
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = u < 0.08 ? PINKW : PINK; g.fillRect(0, y - band / 2, W, band);
				circle(x, y, R * Math.Min(1, 0.4 + u / 0.08));
				g.shadowBlur = 0; g.fillStyle = "rgba(255,210,225,0.45)"; g.fillRect(0, y - band * 0.18, W, band * 0.36);
			}
			else if (u < hold + 0.2) { g.fillStyle = L21bW(0.25 * (1 - (u - hold) / 0.2)); g.fillRect(0, y - band / 2, W, band); }
			if (wv > 0)
			{
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; double a = wUp(t), b = wDn(t);
				if (a > -wth) g.fillRect(0, a - wth / 2, W, wth); if (b < H + wth) g.fillRect(0, b - wth / 2, W, wth);
			}
			if (colB > 0 && u < colB)
			{
				double top = colTop(t); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = L21bP(0.45); g.fillRect(x - colW / 2, top, colW, y - top);
				g.fillStyle = PINK; g.strokeStyle = PINK; g.lineWidth = 3;
				for (int i = 0; i < 14; i++) { double by = y - ((i * 61 + (t - tH) * 520) % Math.Max(1, y - top + 1)), bx = x + (colW / 2 - 9) * JS.sin(i * 2.3 + t * 3); if (by < top) continue; circle(bx, by, 6 + (i % 3) * 2); }
				g.strokeRect(x - colW / 2, top, colW, y - top);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tH) return false; double u = BT(t) - bHit, m = p.r - 2;
			if (u < hold) { if (Math.Abs(p.y - y) < band / 2 + m) return true; double dx = p.x - x, dy = p.y - y, rr = R * Math.Min(1, 0.4 + u / 0.08) + m; if (dx * dx + dy * dy < rr * rr) return true; }
			if (colB > 0 && u < colB) { double top = colTop(t); if (Math.Abs(p.x - x) < colW / 2 + m && p.y > top - m && p.y < y + m) return true; }
			if (wv > 0 && (Math.Abs(p.y - wUp(t)) < wth / 2 + m || Math.Abs(p.y - wDn(t)) < wth / 2 + m)) return true;
			return false;
		};
		return h;
	}

	// ---------- the mine net ----------
	// cols x rows mines (sx, sy apart) round the middle, turning w rad per beat from bIn. White dashed from bIn, pink from bOn till bEnd.
	// The net's lines: line i joins mines la[i] and lb[i] from on[i] to off[i] (white dashed half a beat before)
	public static Hz L21bNet(int cols, int rows, double sx, double sy, double w, double bIn, double bOn, double bEnd, int[] la, int[] lb, double[] on, double[] off, double r, double lw)
	{
		var h = new Hz { kind = "L21bNet" }; double tEnd = B(bEnd);
		(double x, double y) pt(int i, double bt)
		{
			int c = i % cols, rw = i / cols; double x = (c - (cols - 1) / 2.0) * sx, y = (rw - (rows - 1) / 2.0) * sy, a = w * (bt - bIn), ca = JS.cos(a), sa = JS.sin(a);
			return (W / 2 + x * ca - y * sa, H / 2 + x * sa + y * ca);
		}
		double mr(double bt) => r * (1 - 0.7 * L21bK(bt, bEnd - 0.3, bEnd));
		h.update = (t, dt) => { h.z = BT(t) < bOn ? -1 : 0; if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); int n = cols * rows; g.save();
			for (int i = 0; i < la.Length; i++)
			{
				if (bt < on[i] - 0.5 || bt >= on[i]) continue; var a = pt(la[i], bt); var b = pt(lb[i], bt); L21bDash(a.x, a.y, b.x, b.y, L21bW(0.3 + 0.6 * L21bK(bt, on[i] - 0.5, on[i])), 2);
			}
			if (bt < bOn)
			{
				double k = L21bK(bt, bIn, bOn); g.strokeStyle = L21bW(0.3 + 0.6 * k); g.lineWidth = 2; g.setLineDash(new double[] { 5, 5 });
				for (int i = 0; i < n; i++) { var q = pt(i, bt); g.beginPath(); g.arc(q.x, q.y, r * (1.6 - 0.6 * k), 0, TAU); g.stroke(); }
				g.setLineDash(new double[0]); g.restore(); return;
			}
			g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineCap = "round";
			for (int i = 0; i < la.Length; i++)
			{
				if (bt < on[i] || bt >= off[i]) continue; var a = pt(la[i], bt); var b = pt(lb[i], bt);
				g.lineWidth = bt - on[i] < 0.1 ? lw + 4 : lw; g.strokeStyle = bt - on[i] < 0.1 ? PINKW : PINK; g.beginPath(); g.moveTo(a.x, a.y); g.lineTo(b.x, b.y); g.stroke();
			}
			g.lineCap = "butt"; g.fillStyle = PINK; double rr = mr(bt);
			for (int i = 0; i < n; i++) { var q = pt(i, bt); L21bMine(q.x, q.y, rr, t * 0.6 + i); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bOn || t > tEnd) return false; double m = p.r - 2, rr = mr(bt) + m; int n = cols * rows;
			for (int i = 0; i < n; i++) { var q = pt(i, bt); double dx = p.x - q.x, dy = p.y - q.y; if (dx * dx + dy * dy < rr * rr) return true; }
			for (int i = 0; i < la.Length; i++) { if (bt < on[i] || bt >= off[i]) continue; var a = pt(la[i], bt); var b = pt(lb[i], bt); if (L21bSeg(p.x, p.y, a.x, a.y, b.x, b.y) < lw / 2 + m) return true; }
			return false;
		};
		return h;
	}

	// ---------- the hull's frames (the dive) ----------
	// a rib across the screen coming up from under it at sp px/s from bL, with one way through at gx (gw wide)
	public static Hz L21bRib(double bL, double sp, double gx, double gw, double th = 24)
	{
		var h = new Hz { kind = "L21bRib" }; double t0 = B(bL);
		double Y(double t) => H + th - sp * (t - t0);
		h.update = (t, dt) => { if (Y(t) < -th - 10) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double y = Y(t), a = gx - gw / 2, b = gx + gw / 2;
			g.save(); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
			g.fillRect(-10, y - th / 2, a + 10, th); g.fillRect(b, y - th / 2, W + 10 - b, th);
			g.shadowBlur = 0; g.fillStyle = "rgba(110,0,35,0.5)";
			for (double x = 30; x < W; x += 80) if (x < a - 10 || x > b + 10) circle(x, y, 3.5);
			g.fillStyle = "rgba(255,200,220,0.4)"; g.fillRect(-10, y - th / 2, a + 10, 4); g.fillRect(b, y - th / 2, W + 10 - b, 4);
			// the way through: white corner marks
			g.strokeStyle = L21bW(0.8); g.lineWidth = 3; g.beginPath(); g.moveTo(a - 2, y - th / 2 - 10); g.lineTo(a - 2, y + th / 2 + 10); g.moveTo(b + 2, y - th / 2 - 10); g.lineTo(b + 2, y + th / 2 + 10); g.stroke();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double y = Y(t), m = p.r - 2;
			if (Math.Abs(p.y - y) >= th / 2 + m) return false;
			return p.x - m < gx - gw / 2 || p.x + m > gx + gw / 2;
		};
		return h;
	}

	// ---------- the ping ----------
	// a ring from (cx,cy): radius r0 at bS, growing v px/s (v < 0: closing) till rEnd; th thick; ways through at the angles ga (gw rad wide).
	// From bTell the ways through show as white dashed lines from the middle (and a closing ring shows white dashed where it starts)
	public static Hz L21bPing(double cx, double cy, double bTell, double bS, double r0, double v, double rEnd, double th, double[] ga, double gw)
	{
		var h = new Hz { kind = "L21bPing" }; double tS = B(bS);
		double R(double t) => r0 + v * (t - tS);
		bool inGap(double a, out double gc) { foreach (var c in ga) { double d = Math.Abs(((a - c) % TAU + TAU + Math.PI) % TAU - Math.PI); if (d < gw / 2) { gc = c; return true; } } gc = 0; return false; }
		h.update = (t, dt) => { h.z = t < tS ? -1 : 0; double r = R(t); if (t > tS && (v > 0 ? r > rEnd : r < rEnd)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (t < tS)
			{
				double k = L21bK(bt, bTell, bS);
				foreach (var c in ga)
					foreach (var s in new[] { -1.0, 1 }) { double a = c + s * gw / 2; L21bDash(cx + 40 * JS.cos(a), cy + 40 * JS.sin(a), cx + 900 * JS.cos(a), cy + 900 * JS.sin(a), L21bW(0.25 + 0.55 * k), 2); }
				if (v < 0) { g.strokeStyle = L21bW(0.2 + 0.4 * k); g.lineWidth = 2; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.arc(cx, cy, r0, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				g.restore(); return;
			}
			double r = Math.Max(1, R(t)); g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = t - tS < 0.06 ? PINKW : PINK; g.lineWidth = th;
			var gs = ga.Select(c => ((c - gw / 2) % TAU + TAU) % TAU).OrderBy(x => x).ToArray();
			for (int i = 0; i < gs.Length; i++) { double a0 = gs[i] + gw, a1 = i + 1 < gs.Length ? gs[i + 1] : gs[0] + TAU; if (a1 > a0) { g.beginPath(); g.arc(cx, cy, r, a0, a1); g.stroke(); } }
			// the ways through stay marked (white) while the ring is out
			g.shadowBlur = 0; foreach (var c in ga) L21bDash(cx + Math.Max(40, r - 120) * JS.cos(c), cy + Math.Max(40, r - 120) * JS.sin(c), cx + (r + 120) * JS.cos(c), cy + (r + 120) * JS.sin(c), L21bW(0.35), 2);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tS) return false; double r = R(t), dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), m = p.r - 2;
			if (Math.Abs(d - r) >= th / 2 + m) return false;
			if (!inGap(JS.atan2(dy, dx), out double gc)) return true;
			// in a way through: only its edges can touch
			foreach (var s in new[] { -1.0, 1 }) { double a = gc + s * gw / 2, ex = cx + r * JS.cos(a), ey = cy + r * JS.sin(a); if (hypot(p.x - ex, p.y - ey) < th / 2 + m) return true; }
			return false;
		};
		return h;
	}

	// ---------- the torpedo ----------
	// from (x0,y0) (outside the screen) along angle ang (aim: at where the square is when it appears), its way white dashed from bTell, it goes at bFire, sp px/s
	public static Hz L21bTorp(double x0, double y0, double ang, bool aim, double bTell, double bFire, double sp, double L = 150, double w = 30)
	{
		var h = new Hz { kind = "L21bTorp" }; double tF = B(bFire), dx = JS.cos(ang), dy = JS.sin(ang), ent = 0, ex = 0; bool lk = false;
		void lock_()
		{
			if (aim) { double tx = player?.x ?? W / 2, ty = player?.y ?? H / 2, l = Math.Max(1, hypot(tx - x0, ty - y0)); dx = (tx - x0) / l; dy = (ty - y0) / l; }
			ent = 0; for (double d = 0; d < 3000; d += 4) { double x = x0 + dx * d, y = y0 + dy * d; if (x > 0 && x < W && y > 0 && y < H) { ent = d; break; } }
			ex = ent; for (double d = ent; d < 4000; d += 4) { double x = x0 + dx * d, y = y0 + dy * d; ex = d; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) break; }
		}
		double head(double t) => ent + (t - tF) * sp;
		h.update = (t, dt) => { if (!lk) { lock_(); lk = true; } h.z = t < tF ? -1 : 0; if (head(t) - L > ex + 40) h.dead = true; };
		h.draw = t =>
		{
			if (!lk) return; double bt = BT(t); g.save();
			double sx = x0 + dx * ent, sy = y0 + dy * ent;
			if (t < tF)
			{
				double k = L21bK(bt, bTell, bFire);
				L21bDash(sx, sy, x0 + dx * ex, y0 + dy * ex, L21bW(0.3 + 0.55 * k), 2 + 2 * k);
				// the tube's mark where it comes in
				g.strokeStyle = L21bW(0.9); g.lineWidth = 3; double nx = -dy, ny = dx, s = 16 + 10 * (1 - k);
				g.beginPath(); g.moveTo(sx + nx * s, sy + ny * s); g.lineTo(sx + nx * s + dx * 14, sy + ny * s + dy * 14); g.moveTo(sx - nx * s, sy - ny * s); g.lineTo(sx - nx * s + dx * 14, sy - ny * s + dy * 14); g.stroke();
				g.restore(); return;
			}
			double d = head(t), hx = x0 + dx * d, hy = y0 + dy * d, tx = hx - dx * L, ty = hy - dy * L;
			// the wake (white, harmless)
			g.fillStyle = "rgba(220,240,255,0.25)"; for (int i = 1; i < 8; i++) { double bd = d - L - i * 26; if (bd < ent - 20) break; circle(x0 + dx * bd - dy * JS.sin(i * 1.9) * 7, y0 + dy * bd + dx * JS.sin(i * 1.9) * 7, 5.5 - 0.6 * i); }
			g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = t - tF < 0.06 ? PINKW : PINK; L21bCap(tx, ty, hx, hy, w);
			g.shadowBlur = 0; g.strokeStyle = "rgba(110,0,35,0.55)"; g.lineWidth = 3; double fx = hx - dx * L * 0.3, fy = hy - dy * L * 0.3; g.beginPath(); g.moveTo(fx - dy * w * 0.45, fy + dx * w * 0.45); g.lineTo(fx + dy * w * 0.45, fy - dx * w * 0.45); g.stroke();
			g.restore();
		};
		h.hit = (p, t) => { if (!lk || t < tF) return false; double d = head(t), hx = x0 + dx * d, hy = y0 + dy * d; return L21bSeg(p.x, p.y, hx - dx * L, hy - dy * L, hx, hy) < w / 2 + p.r - 2; };
		return h;
	}

	// ---------- the volley ----------
	// a wall of torpedoes nose to nose (n of them over the screen's height) from the left (side -1) or the right, all but the way through
	// (gy, gh high); the way through shows white from bTell, it goes at bFire at sp px/s
	public static Hz L21bVolley(double side, double gy, double gh, double bTell, double bFire, double sp, double L = 120, double w = 34)
	{
		var h = new Hz { kind = "L21bVolley" }; double tF = B(bFire);
		double X(double t) => side < 0 ? -20 + sp * (t - tF) : W + 20 - sp * (t - tF);   // the noses
		h.update = (t, dt) => { h.z = t < tF ? -1 : 0; double x = X(t); if (side < 0 ? x - L > W + 40 : x + L < -40) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (t < tF)
			{
				double k = L21bK(bt, bTell, bFire);
				L21bDash(0, gy - gh / 2, W, gy - gh / 2, L21bW(0.35 + 0.5 * k), 2 + k); L21bDash(0, gy + gh / 2, W, gy + gh / 2, L21bW(0.35 + 0.5 * k), 2 + k);
				double ex = side < 0 ? 6 : W - 6; g.fillStyle = L21bP(0.15 + 0.35 * k); g.fillRect(side < 0 ? 0 : W - 12, 0, 12, gy - gh / 2); g.fillRect(side < 0 ? 0 : W - 12, gy + gh / 2, 12, H - gy - gh / 2);
				g.fillStyle = L21bW(0.5 + 0.4 * k);
				for (int m = 0; m < 3; m++) { double cx = ex - side * (m * 40 + 40 * k), cy = gy; g.beginPath(); g.moveTo(cx - side * 20, cy); g.lineTo(cx + side * 6, cy - 18); g.lineTo(cx + side * 6, cy + 18); g.closePath(); g.fill(); }
				g.restore(); return;
			}
			double x = X(t), dir = -side, tx = x - dir * L; g.shadowColor = PINK; g.shadowBlur = 16; g.strokeStyle = t - tF < 0.06 ? PINKW : PINK;
			for (double y = w / 2; y < H; y += w)
			{
				if (y + w / 2 > gy - gh / 2 && y - w / 2 < gy + gh / 2) continue;
				L21bCap(tx, y, x, y, w - 4);
			}
			g.shadowBlur = 0; g.fillStyle = "rgba(220,240,255,0.22)"; for (int i = 1; i < 6; i++) { double bx = tx - dir * i * 30; for (double y = w / 2; y < H; y += w * 2) if (!(y + w / 2 > gy - gh / 2 && y - w / 2 < gy + gh / 2)) circle(bx, y + JS.sin(i * 1.7 + y) * 5, 6 - i); }
			g.restore();
		};
		// the wall: the noses' band (x - L .. x) everywhere but the way through (its edges are the round ends of the torpedoes next to it)
		h.hit = (p, t) =>
		{
			if (t < tF) return false; double x = X(t), dir = -side, a = Math.Min(x, x - dir * L), b = Math.Max(x, x - dir * L), m = p.r - 2;
			if (p.x < a - m || p.x > b + m) return false;
			for (double y = w / 2; y < H; y += w)
			{
				if (y + w / 2 > gy - gh / 2 && y - w / 2 < gy + gh / 2) continue;
				if (L21bSeg(p.x, p.y, a + (w - 4) / 2, y, b - (w - 4) / 2, y) < (w - 4) / 2 + m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- Barracuda's hull ramming through ----------
	// dir +1 from the left, -1 from the right; along y, hh high and Ls long (with its tower on top); its lane white dashed from bTell, it comes in at bGo at sp px/s
	public static double L21bHullX(double dir, double Ls, double bGo, double sp, double t) => dir > 0 ? -30 + sp * (t - B(bGo)) : W + 30 - sp * (t - B(bGo));
	public static Hz L21bHull(double dir, double y, double hh, double Ls, double bTell, double bGo, double sp)
	{
		var h = new Hz { kind = "L21bHull" }; double tG = B(bGo);
		double X(double t) => L21bHullX(dir, Ls, bGo, sp, t);   // the nose
		h.update = (t, dt) => { h.z = t < tG ? -1 : 0; double x = X(t); if (dir > 0 ? x - Ls > W + 60 : x + Ls < -60) h.dead = true; };
		(double a, double b, double ta, double tb) parts(double t) { double x = X(t), tail = x - dir * Ls, tw0 = x - dir * Ls * 0.62, tw1 = x - dir * Ls * 0.42; return (Math.Min(x, tail), Math.Max(x, tail), Math.Min(tw0, tw1), Math.Max(tw0, tw1)); }
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (t < tG)
			{
				double k = L21bK(bt, bTell, bGo);
				L21bDash(0, y - hh / 2, W, y - hh / 2, L21bW(0.3 + 0.5 * k), 2 + k); L21bDash(0, y + hh / 2, W, y + hh / 2, L21bW(0.3 + 0.5 * k), 2 + k);
				g.fillStyle = L21bW(0.04 + 0.07 * k); g.fillRect(0, y - hh / 2, W, hh);
				g.fillStyle = L21bW(0.5 + 0.4 * k); double ax = dir > 0 ? 40 : W - 40;
				for (int m = 0; m < 3; m++) { double cx = ax + dir * (m * 46 + 60 * k); g.beginPath(); g.moveTo(cx + dir * 22, y); g.lineTo(cx - dir * 6, y - 26); g.lineTo(cx - dir * 6, y + 26); g.closePath(); g.fill(); }
				g.restore(); return;
			}
			var (a, b, ta, tb) = parts(t); double r = hh / 2;
			g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = PINK;
			g.beginPath(); g.moveTo(a + r, y - r); g.lineTo(b - r, y - r); g.arc(b - r, y, r, -Math.PI / 2, Math.PI / 2); g.lineTo(a + r, y + r); g.arc(a + r, y, r, Math.PI / 2, Math.PI * 1.5); g.closePath(); g.fill();
			g.fillRect(ta, y - r - 46, tb - ta, 48);
			g.shadowBlur = 0; g.fillStyle = "rgba(110,0,35,0.45)"; g.fillRect(a + r, y - 4, b - a - 2 * r, 8);
			for (double x = a + r + 30; x < b - r; x += 70) circle(x, y + r * 0.55, 5);
			g.fillStyle = "rgba(255,200,220,0.4)"; g.fillRect(a + r, y - r + 4, b - a - 2 * r, 6);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tG) return false; var (a, b, ta, tb) = parts(t); double m = p.r - 2, r = hh / 2;
			if (p.x > ta - m && p.x < tb + m && p.y > y - r - 46 - m && p.y < y - r + 2) return true;
			if (Math.Abs(p.y - y) >= r + m) return false;
			if (p.x > a + r && p.x < b - r) return true;
			double cx = p.x <= a + r ? a + r : b - r; return hypot(p.x - cx, p.y - y) < r + m;
		};
		return h;
	}

	// ---------- the lock-on ----------
	// at bTell it locks on where the square is (white cross and a closing square); at bFire two beams (bw wide) along the cross for hold beats
	public static Hz L21bLock(double bTell, double bFire, double hold, double bw)
	{
		var h = new Hz { kind = "L21bLock" }; double tF = B(bFire), tE = B(bFire + hold), x = W / 2, y = H / 2; bool lk = false;
		h.update = (t, dt) => { if (!lk) { x = player?.x ?? W / 2; y = player?.y ?? H / 2; lk = true; } h.z = t < tF ? -1 : 0; if (t > tE + 0.2) h.dead = true; };
		h.draw = t =>
		{
			if (!lk) return; double bt = BT(t); g.save();
			if (t < tF)
			{
				double k = L21bK(bt, bTell, bFire);
				L21bDash(0, y, W, y, L21bW(0.3 + 0.5 * k), 2); L21bDash(x, 0, x, H, L21bW(0.3 + 0.5 * k), 2);
				g.fillStyle = L21bW(0.03 + 0.07 * k); g.fillRect(0, y - bw / 2, W, bw); g.fillRect(x - bw / 2, 0, bw, H);
				double s = 70 - 40 * k; g.strokeStyle = L21bW(0.9); g.lineWidth = 3; g.save(); g.translate(x, y); g.rotate(k * Math.PI / 2);
				for (int i = 0; i < 4; i++) { g.rotate(Math.PI / 2); g.beginPath(); g.moveTo(-s, -s + 16); g.lineTo(-s, -s); g.lineTo(-s + 16, -s); g.stroke(); }
				g.restore(); g.restore(); return;
			}
			if (t < tE)
			{
				g.shadowColor = PINK; g.shadowBlur = 22; g.fillStyle = t - tF < 0.06 ? PINKW : PINK; g.fillRect(0, y - bw / 2, W, bw); g.fillRect(x - bw / 2, 0, bw, H);
				g.shadowBlur = 0; g.fillStyle = "rgba(255,215,230,0.5)"; g.fillRect(0, y - bw * 0.15, W, bw * 0.3); g.fillRect(x - bw * 0.15, 0, bw * 0.3, H);
			}
			else { double q = 1 - (t - tE) / 0.2; g.fillStyle = L21bW(0.25 * q); g.fillRect(0, y - bw / 2, W, bw); g.fillRect(x - bw / 2, 0, bw, H); }
			g.restore();
		};
		h.hit = (p, t) => { if (!lk || t < tF || t >= tE) return false; double m = p.r - 2; return Math.Abs(p.y - y) < bw / 2 + m || Math.Abs(p.x - x) < bw / 2 + m; };
		return h;
	}

	// ---------- the propeller ----------
	// nb blades (round ended, bw wide, out to R) round a hub (hubR), its middle going from (x0,y0) at b0 to (x1,y1) at b1. It turns w0 rad per beat
	// and jumps ja[i] rad over jd beats at the beats jb[i]. White dashed from bIn, pink from bOn; from bOff the blades pull in till bEnd.
	public static Hz L21bProp(double x0, double y0, double x1, double y1, double b0, double b1, int nb, double R, double hubR, double bw, double w0, double[] jb, double[] ja, double jd, double bIn, double bOn, double bOff, double bEnd)
	{
		var h = new Hz { kind = "L21bProp" }; double tEnd = B(bEnd);
		(double x, double y) C(double bt) { double k = L21bK(B(bt), B(b0), B(b1)); return (x0 + (x1 - x0) * k, y0 + (y1 - y0) * k); }
		double A(double bt) { double a = w0 * (bt - b0); for (int i = 0; i < jb.Length; i++) { if (bt < jb[i]) break; a += ja[i] * L21bEase(L21bK(bt, jb[i], jb[i] + jd)); } return a; }
		double Ro(double bt) => bt < bOff ? R : hubR + (R - hubR) * (1 - L21bEase(L21bK(bt, bOff, bEnd)));
		h.update = (t, dt) => { h.z = BT(t) < bOn ? -1 : 0; if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); var c = C(bt); double an = A(bt), ro = Ro(bt); g.save();
			if (bt < bOn)
			{
				double k = L21bK(bt, bIn, bOn); g.strokeStyle = L21bW(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 });
				for (int j = 0; j < nb; j++) { double a = an + j * TAU / nb, ca = JS.cos(a), sa = JS.sin(a), nx = -sa * bw / 2, ny = ca * bw / 2; g.beginPath(); g.moveTo(c.x + nx, c.y + ny); g.lineTo(c.x + ca * ro + nx, c.y + sa * ro + ny); g.moveTo(c.x - nx, c.y - ny); g.lineTo(c.x + ca * ro - nx, c.y + sa * ro - ny); g.stroke(); }
				g.beginPath(); g.arc(c.x, c.y, hubR, 0, TAU); g.stroke(); g.setLineDash(new double[0]); g.restore(); return;
			}
			// the water it throws (harmless, white)
			g.strokeStyle = "rgba(220,240,255,0.12)"; g.lineWidth = 2; g.beginPath(); g.arc(c.x, c.y, ro + 18, an, an + 2.2); g.stroke(); g.beginPath(); g.arc(c.x, c.y, ro + 18, an + Math.PI, an + Math.PI + 2.2); g.stroke();
			g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.fillStyle = PINK;
			for (int j = 0; j < nb; j++) { double a = an + j * TAU / nb; L21bCap(c.x, c.y, c.x + (ro - bw / 2) * JS.cos(a), c.y + (ro - bw / 2) * JS.sin(a), bw); }
			circle(c.x, c.y, hubR);
			g.shadowBlur = 0; g.strokeStyle = "rgba(110,0,35,0.45)"; g.lineWidth = 3;
			for (int j = 0; j < nb; j++) { double a = an + j * TAU / nb + 0.06; g.beginPath(); g.moveTo(c.x + hubR * JS.cos(a), c.y + hubR * JS.sin(a)); g.lineTo(c.x + (ro - bw) * JS.cos(a), c.y + (ro - bw) * JS.sin(a)); g.stroke(); }
			g.fillStyle = "rgba(255,200,220,0.5)"; circle(c.x, c.y, hubR * 0.4);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bOn || t > tEnd) return false; var c = C(bt); double m = p.r - 2, dx = p.x - c.x, dy = p.y - c.y, ro = Ro(bt);
			if (dx * dx + dy * dy > (ro + m) * (ro + m)) return false;
			if (dx * dx + dy * dy < (hubR + m) * (hubR + m)) return true;
			double an = A(bt);
			for (int j = 0; j < nb; j++) { double a = an + j * TAU / nb; if (L21bSeg(p.x, p.y, c.x, c.y, c.x + (ro - bw / 2) * JS.cos(a), c.y + (ro - bw / 2) * JS.sin(a)) < bw / 2 + m) return true; }
			return false;
		};
		return h;
	}
	// the submarine's dark shape in front of its propeller (harmless, on the floor)
	public static Hz L21bShadow(double x0, double y0, double x1, double y1, double b0, double b1, double len, double hh, double bEnd)
	{
		var h = new Hz { kind = "L21bShadow", z = -1 }; double tEnd = B(bEnd);
		h.update = (t, dt) => { if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			double k = L21bK(t, B(b0), B(b1)), cx = x0 + (x1 - x0) * k, cy = y0 + (y1 - y0) * k, a = Math.Min(1, L21bK(BT(t), b0, b0 + 2)) * (1 - L21bK(BT(t), bEnd - 1, bEnd));
			double nose = cx - 60 - len; g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(2,8,14,0.82)"; g.beginPath(); g.ellipse(cx - 60 - len / 2, cy, len / 2, hh / 2, 0, 0, TAU); g.fill();
			g.fillRect(nose + len * 0.3, cy - hh / 2 - 70, len * 0.14, 80);
			g.strokeStyle = "rgba(45,226,230,0.22)"; g.lineWidth = 2; g.beginPath(); g.ellipse(cx - 60 - len / 2, cy, len / 2, hh / 2, 0, 0, TAU); g.stroke();
			g.strokeRect(nose + len * 0.3, cy - hh / 2 - 70, len * 0.14, 72);
			g.fillStyle = "rgba(45,226,230,0.25)"; for (int i = 0; i < 9; i++) circle(nose + len * (0.12 + i * 0.09), cy + hh * 0.18, 4);
			g.restore();
		};
		return h;
	}

	// ---------- the moored mines ----------
	// mines on chains from the top (top[i]) or the floor at x[i], len[i] long; on each hit in kb they all lunge out ext px and pull back.
	// White dashed from bIn, they come in over a beat from bOn, go back over the last beat to bEnd
	public static Hz L21bMoor(double[] xs, bool[] top, double[] len, double[] kb, double ext, double r, double bIn, double bOn, double bEnd)
	{
		var h = new Hz { kind = "L21bMoor" }; double tEnd = B(bEnd);
		double L(int i, double bt)
		{
			double e = 0; foreach (var k in kb) { if (bt < k) break; double u = bt - k; e = Math.Max(e, u < 0.12 ? u / 0.12 : Math.Max(0, 1 - (u - 0.12) / 0.6)); }
			return (len[i] + ext * L21bEase(e)) * L21bEase(L21bK(bt, bOn, bOn + 1)) * (1 - L21bEase(L21bK(bt, bEnd - 1, bEnd)));
		}
		(double x, double y0, double y1) at_(int i, double bt) { double l = L(i, bt); return top[i] ? (xs[i], -10, -10 + l) : (xs[i], H + 10, H + 10 - l); }
		h.update = (t, dt) => { h.z = BT(t) < bOn ? -1 : 0; if (t > tEnd) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (bt < bOn)
			{
				double k = L21bK(bt, bIn, bOn);
				for (int i = 0; i < xs.Length; i++) { double y1 = top[i] ? len[i] : H - len[i]; L21bDash(xs[i], top[i] ? 0 : H, xs[i], y1, L21bW(0.3 + 0.5 * k), 2); g.strokeStyle = L21bW(0.3 + 0.5 * k); g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(xs[i], y1, r, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				g.restore(); return;
			}
			g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.fillStyle = PINK; g.lineWidth = 7;
			for (int i = 0; i < xs.Length; i++) { var q = at_(i, bt); if (Math.Abs(q.y1 - q.y0) < 2) continue; g.beginPath(); g.moveTo(q.x, q.y0); g.lineTo(q.x, q.y1); g.stroke(); L21bMine(q.x, q.y1, r, t * 0.5 + i); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bOn || t > tEnd) return false; double m = p.r - 2;
			for (int i = 0; i < xs.Length; i++)
			{
				if (Math.Abs(p.x - xs[i]) > r + m + 2) continue; var q = at_(i, bt); if (Math.Abs(q.y1 - q.y0) < 2) continue;
				if (hypot(p.x - q.x, p.y - q.y1) < r + m) return true;
				if (Math.Abs(p.x - q.x) < 3.5 + m && p.y > Math.Min(q.y0, q.y1) && p.y < Math.Max(q.y0, q.y1)) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- the ballast column ----------
	// white dashed from bTell; at bUp it shoots up from the floor (over upB beats) and stands till bEnd
	public static Hz L21bPillar(double x, double w, double bTell, double bUp, double upB, double bEnd)
	{
		var h = new Hz { kind = "L21bPillar" }; double tU = B(bUp), tE = B(bEnd);
		double top(double bt) => H + 20 - (H + 40) * L21bEase(L21bK(bt, bUp, bUp + upB));
		h.update = (t, dt) => { h.z = t < tU ? -1 : 0; if (t > tE + 0.25) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); g.save();
			if (t < tU) { double k = L21bK(bt, bTell, bUp); L21bDash(x - w / 2, 0, x - w / 2, H, L21bW(0.3 + 0.5 * k), 2); L21bDash(x + w / 2, 0, x + w / 2, H, L21bW(0.3 + 0.5 * k), 2); g.fillStyle = L21bW(0.04 + 0.07 * k); g.fillRect(x - w / 2, 0, w, H); g.restore(); return; }
			if (t < tE)
			{
				double tp = top(bt); g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = PINK; g.fillRect(x - w / 2, tp, w, H + 20 - tp);
				g.shadowBlur = 0; g.fillStyle = "rgba(255,210,225,0.45)";
				for (int i = 0; i < 10; i++) { double by = H - ((i * 83 + (t - tU) * 700) % (H + 20)); if (by > tp) circle(x + (w / 2 - 10) * JS.sin(i * 2.1 + t * 4), by, 5 + i % 3 * 2); }
			}
			else { g.fillStyle = L21bW(0.3 * (1 - (t - tE) / 0.25)); g.fillRect(x - w / 2, 0, w, H); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < tU || t >= tE) return false; double m = p.r - 2; return Math.Abs(p.x - x) < w / 2 + m && p.y > top(BT(t)) - m; };
		return h;
	}

	public static Hz L21bFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "L21bFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}
}
