using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// "لا دندنت حنا لها" (shilah, performed by Mohammed Al Najm), main level 26: "the war drums". A night in the desert: part 1 the drums call
// (the drum's rings, the sticks, the spears), part 2 the sword dance, al-ardah (the swords, the clashing blades, the arrows, the banner),
// part 3 the raid at dawn (the horses, the sandstorm, the banner of the win).
// The level is the song from 0:30.000 (songs/SONG_L26.mp3 starts there). Grid: 116 BPM, beat 0 = 0.445 s of the file (0:30.445 of the song);
// the drums come in at beat 13 (0:37.2) and the song moves in blocks of 16 beats from there (his times 1:06, 1:14 ... 2:36 are the block edges).
// Every hazard is a pure function of time once it exists. Only pink hurts: the sand-coloured marks, the drums, the gold and the steel never do.
public static partial class G
{
	static double DDk(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string DDP(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string DDS(double a) => $"rgba(238,214,164,{str(clamp(a, 0, 1))})";   // sand: the marks of what comes
	static string DDG(double a) => $"rgba(232,182,82,{str(clamp(a, 0, 1))})";
	static double DDEase(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static double DDOut(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k); }
	static double DDRad(double d) => d * Math.PI / 180;
	static double DDH(double s) { double v = JS.sin(s * 12.9898 + 78.233) * 43758.5453; return v - Math.Floor(v); }
	static void DDDash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double DDSeg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static double DDAngDiff(double a, double b) { double d = (a - b) % TAU; if (d < -Math.PI) d += TAU; if (d > Math.PI) d -= TAU; return Math.Abs(d); }
	public static Hz DDFx(double b, double v, double s, double dur)
	{
		double t0 = B(b); var h = new Hz { kind = "DDFx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- where the drums stand (harmless; the rings come out of their skins) ----------
	// the big drum in the middle (part 1 and its copy at 2:04), the two drums at the sides (1:06), the four small ones in the corners (1:10)
	public static readonly (double x, double y)[] DD_SIDE = { (210, 360), (1070, 360) };
	public static readonly (double x, double y)[] DD_CORNER = { (120, 110), (1160, 110), (1160, 610), (120, 610) };
	static bool DDMidDrum(double bt) => (bt >= -1 && bt < 69) || (bt >= 179 && bt < 197);
	static bool DDSideDrums(double bt) => bt >= 67 && bt < 77.5;
	static bool DDCornerDrums(double bt) => bt >= 75 && bt < 85.5;
	// a drum seen from the front a little from above: body, ropes, the skin (it jumps on hit beats)
	static void DDDrum(double x, double y, double s, double hit, double a)
	{
		if (a <= 0.01) return;
		g.save(); g.globalAlpha = a; g.translate(x, y); g.scale(s, s);
		double rx = 70, ry = 22, h = 80, top = -h / 2 + 10;
		var gr = g.createLinearGradient(-rx, 0, rx, 0); gr.addColorStop(0, "rgba(58,30,18,1)"); gr.addColorStop(0.45, "rgba(120,70,38,1)"); gr.addColorStop(1, "rgba(48,24,14,1)");
		g.fillStyle = gr; g.beginPath(); g.moveTo(-rx, top); g.lineTo(-rx, top + h); g.ellipse(0, top + h, rx, ry, 0, Math.PI, 0, true); g.lineTo(rx, top); g.closePath(); g.fill();
		// the ropes: a zigzag in gold
		g.strokeStyle = DDG(0.85); g.lineWidth = 2.5; g.beginPath();
		for (int i = 0; i <= 8; i++) { double xx = -rx + i * rx / 4, yy = i % 2 == 0 ? top + 8 : top + h + ry * 0.6; if (i == 0) g.moveTo(xx, yy); else g.lineTo(xx, yy); }
		g.stroke();
		g.strokeStyle = "rgba(30,16,10,1)"; g.lineWidth = 4; g.beginPath(); g.ellipse(0, top + h, rx, ry, 0, 0, Math.PI); g.stroke();
		// the skin
		double jump = 1 + 0.08 * hit;
		var sk = g.createRadialGradient(-12, top - 6, 4, 0, top, rx * jump); sk.addColorStop(0, "rgba(250,236,206,1)"); sk.addColorStop(1, "rgba(196,170,128,1)");
		g.fillStyle = sk; g.beginPath(); g.ellipse(0, top, rx * jump, ry * jump, 0, 0, TAU); g.fill();
		g.strokeStyle = "rgba(90,52,28,1)"; g.lineWidth = 4; g.stroke();
		if (hit > 0) { g.strokeStyle = DDS(0.6 * hit); g.lineWidth = 3; g.beginPath(); g.ellipse(0, top, rx * (1.1 + 0.4 * (1 - hit)), ry * (1.1 + 0.4 * (1 - hit)), 0, 0, TAU); g.stroke(); }
		g.restore();
	}
	// how much a drum "jumps" at beat bt (it is hit on every beat once the drums are in, on the vocal hits in the intro)
	static readonly double[] DD_VOX = { 0, 1.25, 2, 3.25, 4.5, 5, 6.75, 7.25 };
	static double DDHitAt(double bt)
	{
		double last = -9;
		if (bt < 13) { foreach (var v in DD_VOX) if (v <= bt) last = v; }
		else last = Math.Floor(bt);
		return 1 - DDk(bt - last, 0, 0.35);
	}

	// ---------- the stage (harmless, under everything) ----------
	// the night (part 1), the torches' purple night (part 2), the red dawn (part 3); stars, the moon, the dunes; and the drums
	public static Hz DDStage(double t1)
	{
		var h = new Hz { kind = "DDStage", z = -1 };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, Math.Max(0, t + 0.5) / 0.6), p2 = DDk(bt, 84, 86) * (1 - DDk(bt, 164, 166)), p3 = DDk(bt, 164, 167);
			g.save(); g.globalAlpha = a;
			double r0 = 6 + 18 * p2 + 50 * p3, g0 = 9 + 2 * p2 + 16 * p3, b0 = 26 + 12 * p2 - 8 * p3;
			double r1 = 14 + 22 * p2 + 120 * p3, g1 = 12 + 4 * p2 + 50 * p3, b1 = 34 + 6 * p2 - 4 * p3;
			var gr = g.createLinearGradient(0, 0, 0, H);
			gr.addColorStop(0, $"rgba({str(r0)},{str(g0)},{str(b0)},1)"); gr.addColorStop(0.72, $"rgba({str(r1)},{str(g1)},{str(b1)},1)"); gr.addColorStop(1, $"rgba({str(r1 * 0.6)},{str(g1 * 0.6)},{str(b1 * 0.6)},1)");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			// stars (fade at dawn)
			double sa = 1 - 0.8 * p3;
			if (sa > 0.05)
			{
				for (int i = 0; i < 60; i++)
				{
					double sx = DDH(i * 3.1) * W, sy = DDH(i * 7.7 + 1) * 430, tw = 0.5 + 0.5 * JS.sin(t * (1.3 + DDH(i) * 2) + i);
					g.fillStyle = $"rgba(255,248,230,{str(sa * (0.25 + 0.5 * tw))})"; g.fillRect(sx, sy, i % 7 == 0 ? 2.4 : 1.4, i % 7 == 0 ? 2.4 : 1.4);
				}
			}
			// the moon (part 1-2), sinking at dawn
			{
				double my = 120 + 520 * p3, mx = 1040;
				var mg = g.createRadialGradient(mx, my, 20, mx, my, 160); mg.addColorStop(0, $"rgba(255,244,214,{str(0.22 * (1 - p3))})"); mg.addColorStop(1, "rgba(0,0,0,0)");
				g.fillStyle = mg; g.fillRect(mx - 160, my - 160, 320, 320);
				g.fillStyle = $"rgba(250,240,214,{str(0.85 * (1 - p3))})"; circle(mx, my, 44);
				g.fillStyle = $"rgba({str(r0)},{str(g0)},{str(b0)},{str(0.95 * (1 - p3))})"; circle(mx + 18, my - 10, 40);
			}
			// the sun's glow at dawn
			if (p3 > 0) { var sg = g.createRadialGradient(W / 2, H + 40, 40, W / 2, H + 40, 640); sg.addColorStop(0, $"rgba(255,150,60,{str(0.35 * p3)})"); sg.addColorStop(1, "rgba(0,0,0,0)"); g.fillStyle = sg; g.fillRect(0, 0, W, H); }
			// the dunes: three dark layers (they slide in part 3: the riders are moving)
			for (int l = 0; l < 3; l++)
			{
				double sh = (p3 > 0 ? (t * (30 + 40 * l)) : 0) % 1600, base0 = 560 + 50 * l, c = 0.3 + 0.25 * l;
				g.fillStyle = $"rgba({str((r1 * 0.5 + 6) * c)},{str((g1 * 0.5 + 4) * c)},{str((b1 * 0.4 + 6) * c)},1)";
				g.beginPath(); g.moveTo(0, H);
				for (double x = 0; x <= W + 20; x += 20) g.lineTo(x, base0 + 34 * JS.sin((x + sh + l * 300) / (180 + 60 * l)) + 18 * JS.sin((x + sh * 0.7) / 77 + l));
				g.lineTo(W, H); g.closePath(); g.fill();
			}
			// torches in part 2 (two at the sides, flickering)
			if (p2 > 0)
			{
				foreach (var tx in new[] { 60.0, W - 60 })
				{
					double fl = 0.75 + 0.25 * JS.sin(t * 17 + tx) * JS.sin(t * 7.3);
					var tg = g.createRadialGradient(tx, 300, 4, tx, 300, 170); tg.addColorStop(0, $"rgba(255,170,70,{str(0.28 * p2 * fl)})"); tg.addColorStop(1, "rgba(0,0,0,0)");
					g.fillStyle = tg; g.fillRect(tx - 170, 130, 340, 340);
					g.fillStyle = $"rgba(70,44,28,{str(p2)})"; g.fillRect(tx - 4, 300, 8, 120);
					g.fillStyle = $"rgba(255,196,96,{str(0.9 * p2)})"; g.beginPath(); g.ellipse(tx, 292, 9 * fl, 18 * fl, 0, 0, TAU); g.fill();
				}
			}
			var vg = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); vg.addColorStop(0, "rgba(0,0,0,0)"); vg.addColorStop(1, "rgba(0,0,0,0.5)"); g.fillStyle = vg; g.fillRect(0, 0, W, H);
			g.restore();
			// the drums
			double hit = DDHitAt(bt);
			if (DDMidDrum(bt)) { double da = bt < 69 ? DDk(bt, -1, 0) * (1 - DDk(bt, 68, 69)) : DDk(bt, 179, 181) * (1 - DDk(bt, 196, 197)); DDDrum(W / 2, H / 2 + 20, 1.1, hit, da * a); }
			if (DDSideDrums(bt)) { double da = DDk(bt, 67, 69) * (1 - DDk(bt, 76.5, 77.5)); foreach (var d in DD_SIDE) DDDrum(d.x, d.y + 20, 0.85, hit, da); }
			if (DDCornerDrums(bt)) { double da = DDk(bt, 75, 77) * (1 - DDk(bt, 84.5, 85.5)); foreach (var d in DD_CORNER) DDDrum(d.x, d.y + 16, 0.6, hit, da); }
		};
		return h;
	}

	// ---------- the drum's rings ----------
	// A ring grows from (cx, cy) from r0 at sp px/s from beat bf, th thick, with a gap of gw radians around angle ga. Where the gap will be
	// shows from bf - tl: two sand dashed lines from the drum along the gap's edges.
	public static Hz DDRing(double cx, double cy, double bf, double tl, double sp, double th, double ga, double gw, double r0 = 50)
	{
		var h = new Hz { kind = "DDRing" }; double tf = B(bf), far = 0;
		foreach (var c in new[] { (0.0, 0.0), (W, 0.0), (0.0, H), (W, H) }) far = Math.Max(far, hypot(c.Item1 - cx, c.Item2 - cy));
		double rad(double t) => r0 + sp * (t - tf);
		h.update = (t, dt) => { if (t > tf && rad(t) - th > far + 20) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = DDk(bt, bf - tl, bf);
				foreach (var s in new[] { -1, 1 }) { double aa = ga + s * gw / 2; DDDash(cx + JS.cos(aa) * r0, cy + JS.sin(aa) * r0, cx + JS.cos(aa) * far, cy + JS.sin(aa) * far, DDS(0.2 + 0.55 * q), 2); }
				g.strokeStyle = DDS(0.15 + 0.35 * q); g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, r0, ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
			}
			else
			{
				double r = rad(t); g.lineCap = "butt";
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK; g.lineWidth = th;
				g.beginPath(); g.arc(cx, cy, Math.Max(1, r), ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double r = rad(t), d = hypot(p.x - cx, p.y - cy);
			if (Math.Abs(d - r) >= th / 2 + p.r - 2) return false;
			return DDAngDiff(JS.atan2(p.y - cy, p.x - cx), ga) > gw / 2;
		};
		return h;
	}

	// ---------- the drumsticks ----------
	// A stick slams down over the rectangle (x, y, w, h) at beat bf and lies there hold beats, then lifts (0.12 beat, it thins and goes).
	// From bf - tl the rectangle shows in sand (dashed edge, faint fill that grows).
	public static Hz DDStick(double x, double y, double w, double hh, double bf, double tl, double hold)
	{
		var h = new Hz { kind = "DDStick" }; bool vert = hh >= w;
		h.update = (t, dt) => { if (BT(t) > bf + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = DDk(bt, bf - tl, bf);
				g.fillStyle = DDS(0.04 + 0.12 * q); g.fillRect(x, y, w, hh);
				g.strokeStyle = DDS(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(x + 1, y + 1, w - 2, hh - 2); g.setLineDash(new double[0]);
			}
			else
			{
				double k = 1 - DDk(bt, bf + hold, bf + hold + 0.12); if (k <= 0) { g.restore(); return; }
				double cx = x + w / 2, cy = y + hh / 2, ww = vert ? w * k : w, hw = vert ? hh : hh * k;
				g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; g.fillRect(cx - ww / 2, cy - hw / 2, ww, hw); g.shadowBlur = 0;
				// the stick's grain (dark lines) and the knob end, inside the pink
				g.strokeStyle = "rgba(120,10,50,0.55)"; g.lineWidth = 3; g.beginPath();
				if (vert) { g.moveTo(cx - ww * 0.18, y + 8); g.lineTo(cx - ww * 0.18, y + hh - 8); g.moveTo(cx + ww * 0.22, y + 8); g.lineTo(cx + ww * 0.22, y + hh - 8); }
				else { g.moveTo(x + 8, cy - hw * 0.18); g.lineTo(x + w - 8, cy - hw * 0.18); g.moveTo(x + 8, cy + hw * 0.22); g.lineTo(x + w - 8, cy + hw * 0.22); }
				g.stroke();
				double fl = 1 - DDk(bt, bf, bf + 0.18); if (fl > 0) { g.globalAlpha = fl; g.fillStyle = PINKW; g.fillRect(cx - ww / 2, cy - hw / 2, ww, hw); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= bf + hold + 0.12) return false;
			double k = 1 - DDk(bt, bf + hold, bf + hold + 0.12), cx = x + w / 2, cy = y + hh / 2, ww = vert ? w * k : w, hw = vert ? hh : hh * k;
			return Math.Abs(p.x - cx) < ww / 2 + p.r - 2 && Math.Abs(p.y - cy) < hw / 2 + p.r - 2;
		};
		return h;
	}

	// ---------- the spears ----------
	// A spear thrust from (x0, y0) (outside the screen) along angle a: its tip runs out to reach px in 0.15 beat from bf, stays hold beats,
	// and pulls back in 0.25 beat. From bf - tl its way shows: a sand dashed line and a mark where the tip will stop.
	public static Hz DDSpear(double x0, double y0, double a, double reach, double bf, double tl, double hold, double w = 12)
	{
		var h = new Hz { kind = "DDSpear" }; double dx = JS.cos(a), dy = JS.sin(a);
		double ext(double bt) => bt < bf ? 0 : reach * (bt < bf + 0.15 ? DDOut((bt - bf) / 0.15) : bt < bf + 0.15 + hold ? 1 : 1 - DDk(bt, bf + 0.15 + hold, bf + 0.4 + hold));
		h.update = (t, dt) => { if (BT(t) > bf + hold + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = DDk(bt, bf - tl, bf), tx = x0 + dx * reach, ty = y0 + dy * reach;
				DDDash(x0, y0, tx, ty, DDS(0.25 + 0.5 * q), 2);
				g.strokeStyle = DDS(0.4 + 0.5 * q); g.lineWidth = 3; g.beginPath(); g.moveTo(tx - dy * 14, ty + dx * 14); g.lineTo(tx + dy * 14, ty - dx * 14); g.stroke();
			}
			else
			{
				double e = ext(bt); if (e <= 1) { g.restore(); return; }
				double tx = x0 + dx * e, ty = y0 + dy * e, hx = tx - dx * 40, hy = ty - dy * 40;
				g.shadowColor = PINK; g.shadowBlur = 10; g.strokeStyle = PINK; g.lineWidth = w; g.lineCap = "butt";
				g.beginPath(); g.moveTo(x0, y0); g.lineTo(hx, hy); g.stroke();
				g.fillStyle = PINK; g.beginPath(); g.moveTo(tx, ty); g.lineTo(hx - dy * 13, hy + dx * 13); g.lineTo(hx + dx * 6, hy + dy * 6); g.lineTo(hx + dy * 13, hy - dx * 13); g.closePath(); g.fill();
				// a tassel ring under the head (gold, harmless, drawn inside the shaft's width)
				g.shadowBlur = 0; g.strokeStyle = DDG(0.9); g.lineWidth = 3; g.beginPath(); g.moveTo(hx - dx * 6 - dy * w / 2, hy - dy * 6 + dx * w / 2); g.lineTo(hx - dx * 6 + dy * w / 2, hy - dy * 6 - dx * w / 2); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf) return false; double e = ext(bt); if (e <= 1) return false;
			double tx = x0 + dx * e, ty = y0 + dy * e, hx = tx - dx * 40, hy = ty - dy * 40;
			if (DDSeg(p.x, p.y, x0, y0, hx, hy) < w / 2 + p.r - 2) return true;
			return DDSeg(p.x, p.y, hx, hy, tx - dx * 14, ty - dy * 14) < 8 + p.r - 2;
		};
		return h;
	}

	// ---------- the swords ----------
	// A sword turns about (cx, cy): its blade from r0 to len. Before bf it rests at a0 in steel (harmless); it swings to a1 in sw beats from
	// bf (pink, hurts), lies there hold beats, and fades (thins out in 0.15 beat). From bf - tl the swept part shows in sand.
	public static Hz DDSword(double cx, double cy, double r0, double len, double a0, double a1, double bf, double sw, double tl, double hold, double w = 22)
	{
		var h = new Hz { kind = "DDSword" };
		double ang(double bt) => bt <= bf ? a0 : a0 + (a1 - a0) * DDEase((bt - bf) / sw);
		double end = bf + sw + hold;
		h.update = (t, dt) => { if (BT(t) > end + 0.3) h.dead = true; };
		void blade(double a, double ww, string col)
		{
			double dx = JS.cos(a), dy = JS.sin(a), bx = cx + dx * r0, by = cy + dy * r0, tx = cx + dx * len, ty = cy + dy * len, nx = -dy * ww / 2, ny = dx * ww / 2;
			double kx = cx + dx * (len - 60), ky = cy + dy * (len - 60);
			g.fillStyle = col; g.beginPath(); g.moveTo(bx + nx, by + ny); g.lineTo(kx + nx, ky + ny); g.lineTo(tx, ty); g.lineTo(kx - nx, ky - ny); g.lineTo(bx - nx, by - ny); g.closePath(); g.fill();
		}
		void hilt(double a, double al)
		{
			double dx = JS.cos(a), dy = JS.sin(a), bx = cx + dx * r0, by = cy + dy * r0;
			g.strokeStyle = DDG(al); g.lineWidth = 7; g.lineCap = "round"; g.beginPath(); g.moveTo(bx - dy * 26, by + dx * 26); g.lineTo(bx + dy * 26, by - dx * 26); g.stroke();
			g.strokeStyle = $"rgba(70,40,24,{str(al)})"; g.lineWidth = 10; g.beginPath(); g.moveTo(bx, by); g.lineTo(cx + dx * Math.Max(0, r0 - 40), cy + dy * Math.Max(0, r0 - 40)); g.stroke();
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl || bt > end + 0.15) return;
			g.save();
			if (bt < bf)
			{
				double q = DDk(bt, bf - tl, bf), lo = Math.Min(a0, a1), hi = Math.Max(a0, a1);
				g.fillStyle = DDS(0.05 + 0.12 * q); g.beginPath(); g.moveTo(cx + JS.cos(lo) * r0, cy + JS.sin(lo) * r0); g.arc(cx, cy, len, lo, hi); g.arc(cx, cy, r0, hi, lo, true); g.closePath(); g.fill();
				g.strokeStyle = DDS(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.arc(cx, cy, len, lo, hi); g.stroke(); g.setLineDash(new double[0]);
				blade(a0, w * 0.8, $"rgba(200,206,214,{str(0.35 + 0.4 * q)})"); hilt(a0, 0.5 + 0.5 * q);
			}
			else
			{
				double a = ang(bt), k = 1 - DDk(bt, end, end + 0.15);
				hilt(a, k);   // under the blade: all of the blade shows pink
				g.shadowColor = PINK; g.shadowBlur = 16; blade(a, w * k, PINK); g.shadowBlur = 0;
				g.strokeStyle = "rgba(255,190,214,0.7)"; g.lineWidth = 2; g.beginPath(); g.moveTo(cx + JS.cos(a) * (r0 + 10), cy + JS.sin(a) * (r0 + 10)); g.lineTo(cx + JS.cos(a) * (len - 70), cy + JS.sin(a) * (len - 70)); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= end + 0.15) return false;
			double a = ang(bt), k = 1 - DDk(bt, end, end + 0.15), dx = JS.cos(a), dy = JS.sin(a);
			if (DDSeg(p.x, p.y, cx + dx * r0, cy + dy * r0, cx + dx * (len - 60), cy + dy * (len - 60)) < w * k / 2 + p.r - 2) return true;
			return DDSeg(p.x, p.y, cx + dx * (len - 60), cy + dy * (len - 60), cx + dx * (len - 22), cy + dy * (len - 22)) < w * k / 4 + p.r - 2;
		};
		return h;
	}
}
