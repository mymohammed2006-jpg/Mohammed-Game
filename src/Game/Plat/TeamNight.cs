using System;
using System.Linq;
using static MG.JS;

namespace MG;

// drawing the co-op level's page three: the photo, on a clear night (stars, the moon, gold frames floating)
public static partial class G
{
	static void tmNightSky(double now, double b, long bi, double bp)
	{
		var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#070a1f"); gr.addColorStop(0.55, "#1a1240"); gr.addColorStop(1, "#3b1f5e");
		g.fillStyle = gr; g.fillRect(0, 0, W, H);
		// stars: a soft twinkle, some of them brighter on the beat
		for (int i = 0; i < 110; i++)
		{
			double x = (i * 373 + 41) % W, y = (i * 211 + 17) % 600, tw = 0.35 + 0.3 * Math.Sin(now / 700 + i * 1.7) + (i % 7 == 0 ? 0.5 * pulse : 0), r = i % 5 == 0 ? 2 : 1.2;
			g.fillStyle = $"rgba(255,248,230,{str(clamp(tw, 0.08, 1))})"; g.fillRect(x - r / 2, y - r / 2, r, r);
		}
		// the moon
		{
			double mx = 1040, my = 118;
			for (int k = 4; k >= 1; k--) { g.fillStyle = $"rgba(255,240,200,{str(0.035 * k)})"; g.beginPath(); g.arc(mx, my, 46 + k * 26, 0, TAU); g.fill(); }
			g.fillStyle = "#fff4d6"; g.beginPath(); g.arc(mx, my, 46, 0, TAU); g.fill();
			g.fillStyle = "rgba(220,200,160,0.35)"; g.beginPath(); g.arc(mx - 14, my - 8, 9, 0, TAU); g.arc(mx + 12, my + 14, 6, 0, TAU); g.arc(mx + 16, my - 16, 4, 0, TAU); g.fill();
		}
		// gold frames floating up slowly, turning a little
		for (int i = 0; i < 7; i++)
		{
			double w = 60 + (i * 37) % 70, h = w * 0.76, x = (i * 191 + 80) % (W - 100) + 50, y = H + 80 - ((now / 1000 * (8 + i * 2) + i * 157) % (H + 200));
			g.save(); g.translate(x, y); g.rotate(Math.Sin(now / 2400 + i) * 0.12);
			g.strokeStyle = $"rgba(255,207,90,{str(0.1 + 0.05 * (i % 3))})"; g.lineWidth = 3; g.strokeRect(-w / 2, -h / 2, w, h);
			g.lineWidth = 1; g.strokeRect(-w / 2 + 6, -h / 2 + 6, w - 12, h - 12); g.restore();
		}
		// low thin clouds lit by the moon
		for (int i = 0; i < 5; i++)
		{
			double sp = 5 + i * 2, x = ((i * 263 + now / 1000 * sp) % (W + 400)) - 200, y = 470 + (i * 61) % 200;
			cloudBlob(x, y, 220 + (i * 53) % 120, 34, "rgba(200,190,255,1)", 0.05 + 0.02 * (i % 2));
		}
	}

	// the glass down the middle; the other side is the mirror (a little bluer), with the left side's clouds in it
	static void tmDrawGlass(double now)
	{
		double m = tm.P.mirror; int x0 = 99, x1 = -1, y0 = 99, y1 = -1;
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++) if (tm.m[y][x] == 'I') { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
		if (x1 < 0) return;
		double gx = x0 * PT, gw = (x1 - x0 + 1) * PT, gy = y0 * PT, gh = (y1 - y0 + 1) * PT;
		g.fillStyle = "rgba(110,150,255,0.07)"; g.fillRect(gx + gw, 0, W - gx - gw, H);
		foreach (var s in tm.th) if (s.k == "cloud" && s.X + s.W <= m) cloudBlob(2 * m - s.X - s.W, s.Y - 4, s.W, 24, "#cfd8ff", 0.32);
		foreach (var s in tm.th) if (s.k == "door" && s.X + s.W <= m && s.s < 0.95)
		{
			int n = (int)Math.Round(s.H / 26); g.fillStyle = $"rgba(207,216,255,{str(0.3 * (1 - s.s))})";
			for (int i = 0; i < n; i++) { g.beginPath(); g.arc(2 * m - s.X - s.W / 2, s.Y + s.H * (i + 0.5) / n, 16, 0, TAU); g.fill(); }
		}
		var gr = g.createLinearGradient(gx, 0, gx + gw, 0); gr.addColorStop(0, "rgba(180,215,255,0.28)"); gr.addColorStop(0.5, "rgba(220,235,255,0.12)"); gr.addColorStop(1, "rgba(180,215,255,0.28)");
		g.fillStyle = gr; g.fillRect(gx, gy, gw, gh);
		g.save(); g.beginPath(); g.rect(gx, gy, gw, gh); g.clip();
		g.strokeStyle = "rgba(255,255,255,0.22)"; g.lineWidth = 6;
		for (int i = 0; i < 3; i++) { double y = ((now / 9 + i * 260) % (gh + 200)) + gy - 100; g.beginPath(); g.moveTo(gx - 10, y + 40); g.lineTo(gx + gw + 10, y - 40); g.stroke(); }
		g.restore();
		g.strokeStyle = "rgba(220,235,255,0.7)"; g.lineWidth = 2; g.beginPath(); g.moveTo(gx + 1, gy); g.lineTo(gx + 1, gy + gh); g.moveTo(gx + gw - 1, gy); g.lineTo(gx + gw - 1, gy + gh); g.stroke();
	}
	static void tmDrawReflection(TBody p, double now)
	{
		if (p.gate) return; double x = tmRx(p.x); string col = TM_COL[p.i];
		g.save(); g.translate(x, p.y); g.globalAlpha = p.down ? 0.25 : 0.5;
		g.fillStyle = col; g.fillRect(-9, -9, 18, 18); g.strokeStyle = "rgba(220,235,255,0.9)"; g.lineWidth = 1.5; g.strokeRect(-9, -9, 18, 18);
		g.fillStyle = "rgba(255,255,255,0.35)"; g.fillRect(-9, -9, 18, 4); g.restore(); g.globalAlpha = 1;
	}

	// a camera on a stand over its pad, in its owner's colour; armed, a ring fills toward the beat it snaps on
	static void tmDrawCam(TS s, double now, double bp)
	{
		string c = TM_COL[(int)s.d.a]; double cx = s.X + s.W / 2, by = s.Y;
		g.save(); g.fillStyle = c; g.globalAlpha = s.on ? 1 : 0.6; g.shadowColor = c; g.shadowBlur = s.on ? 18 : 6; rr(s.X + 4, by - 8 + (s.on ? 3 : 0), s.W - 8, 8 - (s.on ? 3 : 0), 3); g.fill(); g.restore();
		g.strokeStyle = "rgba(230,225,255,0.55)"; g.lineWidth = 2; g.beginPath(); g.moveTo(cx, by - 40); g.lineTo(cx - 12, by - 9); g.moveTo(cx, by - 40); g.lineTo(cx + 12, by - 9); g.moveTo(cx, by - 40); g.lineTo(cx, by - 9); g.stroke();
		g.fillStyle = "#1d1a33"; g.strokeStyle = c; g.lineWidth = 2; rr(cx - 16, by - 62, 32, 22, 4); g.fill(); g.stroke();
		g.fillStyle = c; g.fillRect(cx - 12, by - 67, 8, 5);
		g.fillStyle = "#0b0e24"; g.strokeStyle = "#f4f0ff"; g.beginPath(); g.arc(cx + 2, by - 51, 7, 0, TAU); g.fill(); g.stroke();
		if (s.on && s.n == 1) { g.strokeStyle = c; g.lineWidth = 3; g.beginPath(); g.arc(cx + 2, by - 51, 13, -Math.PI / 2, -Math.PI / 2 + TAU * bp); g.stroke(); }
		double f = tm.t - s.timer;
		if (s.timer > 0 && f < 0.5) { var rg = g.createRadialGradient(cx, by - 51, 4, cx, by - 51, 160); rg.addColorStop(0, $"rgba(255,255,255,{str(0.9 * (1 - f / 0.5))})"); rg.addColorStop(1, "rgba(255,255,255,0)"); g.fillStyle = rg; g.fillRect(cx - 160, by - 211, 320, 320); }
	}
	// a snapped picture: a white step with the player's colour, their outline where they were; it blinks in its last two beats
	static void tmDrawSnap(TS s, double b)
	{
		double left = s.timer - b, a = left < 2 && Math.Floor(b * 4) % 2 != 0 ? 0.35 : 1, born = clamp((tm.t - s.s) / 0.4, 0, 1); string c = TM_COL[(int)s.d.a];
		g.save(); g.globalAlpha = a; g.shadowColor = c; g.shadowBlur = 12; g.fillStyle = "#f8f5ff"; rr(s.X, s.Y, s.W, 8, 3); g.fill(); g.shadowBlur = 0;
		g.fillStyle = c; g.fillRect(s.X + 3, s.Y + 8, s.W - 6, 3);
		g.globalAlpha = a * (0.25 + 0.6 * (1 - born)); g.strokeStyle = c; g.lineWidth = 2; g.strokeRect(s.X + s.W / 2 - 9, s.Y - 18, 18, 18);
		g.restore();
	}
	// a gold frame you stand on (its top edge)
	static void tmDrawFrame(TS s, double now)
	{
		g.save(); g.shadowColor = GOLD; g.shadowBlur = 10 + 6 * pulse;
		g.fillStyle = "rgba(255,207,90,0.07)"; g.fillRect(s.X, s.Y, s.W, 34);
		g.strokeStyle = "#ffcf5a"; g.lineWidth = 5; g.strokeRect(s.X + 2.5, s.Y + 2.5, s.W - 5, 32); g.shadowBlur = 0;
		g.strokeStyle = "rgba(255,230,160,0.6)"; g.lineWidth = 1; g.strokeRect(s.X + 8, s.Y + 8, s.W - 16, 20);
		g.fillStyle = "#ffe7a8"; foreach (double x in new[] { s.X + 2.5, s.X + s.W - 2.5 }) { g.beginPath(); g.arc(x, s.Y + 2.5, 4, 0, TAU); g.arc(x, s.Y + 34.5, 4, 0, TAU); g.fill(); }
		g.restore();
	}
	// a torn piece of the photo, in its player's colour
	static void tmDrawPic(TS s, double now)
	{
		string c = TM_COL[(int)s.d.a]; bool home = !s.on && tm.b.All(q => q.carry != s); double bob = home ? 4 * Math.Sin(now / 300 + s.d.a) : 0;
		double x = s.X + 4, y = s.Y + 4 + bob, w = s.W - 8, h = s.H - 8;
		g.save(); g.shadowColor = c; g.shadowBlur = home ? 16 + 10 * pulse : 8; g.fillStyle = "#fbf7ee";
		g.beginPath(); g.moveTo(x, y); g.lineTo(x + w, y);
		for (int i = 1; i <= 6; i++) g.lineTo(x + w - (i % 2 == 0 ? 0 : 4), y + h * i / 6);   // the torn side
		g.lineTo(x, y + h); g.closePath(); g.fill(); g.shadowBlur = 0;
		var gr = g.createLinearGradient(0, y + 3, 0, y + h - 3); gr.addColorStop(0, "#2a1f5c"); gr.addColorStop(1, c); g.fillStyle = gr; g.fillRect(x + 3, y + 3, w - 9, h - 6);
		g.fillStyle = c; g.fillRect(x + w / 2 - 6, y + h - 15, 9, 9);
		g.restore();
	}
	// the big frame with a place for each piece, and the pieces still waiting
	static void tmDrawPhoto(double now, double b)
	{
		var sl = tm.th.Where(s => s.k == "slot").ToList();
		foreach (var s in tm.th) if (s.k == "pic" && !s.on && tm.b.All(q => q.carry != s)) tmDrawPic(s, now);
		if (sl.Count == 0) return;
		var (fx, fy, fw, fh) = tmFrameRect();
		g.fillStyle = "rgba(8,8,26,0.75)"; g.fillRect(fx, fy, fw, fh);
		g.save(); g.shadowColor = GOLD; g.shadowBlur = 16 + 10 * pulse; g.strokeStyle = "#ffcf5a"; g.lineWidth = 8; g.strokeRect(fx - 4, fy - 4, fw + 8, fh + 8); g.shadowBlur = 0;
		g.strokeStyle = "rgba(255,230,160,0.7)"; g.lineWidth = 1.5; g.strokeRect(fx + 5, fy + 5, fw - 10, fh - 10);
		g.fillStyle = "#ffe7a8"; foreach (var (x, y) in new[] { (fx - 4, fy - 4), (fx + fw + 4, fy - 4), (fx - 4, fy + fh + 4), (fx + fw + 4, fy + fh + 4) }) { g.beginPath(); g.arc(x, y, 7, 0, TAU); g.fill(); }
		g.restore();
		foreach (var s in sl)
		{
			string c = TM_COL[(int)s.d.a];
			if (!s.on) { g.save(); g.globalAlpha = 0.18 + 0.1 * pulse; g.fillStyle = c; g.fillRect(s.X + 3, s.Y + 3, s.W - 6, s.H - 6); g.globalAlpha = 0.8; g.strokeStyle = c; g.lineWidth = 2; g.setLineDash(new double[] { 5, 5 }); g.strokeRect(s.X + 3, s.Y + 3, s.W - 6, s.H - 6); g.setLineDash(new double[0]); g.restore(); }
		}
		foreach (var s in tm.th) if (s.k == "pic" && s.on) tmDrawPic(s, now);
	}
	static (double x, double y, double w, double h) tmFrameRect()
	{
		var sl = tm.th.Where(s => s.k == "slot").ToList();
		double x0 = sl.Min(s => s.X), y0 = sl.Min(s => s.Y), x1 = sl.Max(s => s.X + s.W), y1 = sl.Max(s => s.Y + s.H);
		return (x0 - 18, y0 - 18, x1 - x0 + 36, y1 - y0 + 36);
	}
	// the photo of the three, as it comes out: their sunset over the clouds (page one), the three side by side
	static void tmPhotoScene(double x, double y, double w, double h, double a)
	{
		g.save(); g.globalAlpha = a; g.fillStyle = "#fbf7ee"; g.fillRect(x, y, w, h);
		double p = Math.Max(4, w * 0.05), ix = x + p, iy = y + p, iw = w - 2 * p, ih = h - 2 * p;
		g.beginPath(); g.rect(ix, iy, iw, ih); g.clip();
		var gr = g.createLinearGradient(0, iy, 0, iy + ih); gr.addColorStop(0, "#1c1033"); gr.addColorStop(0.5, "#4a1f4f"); gr.addColorStop(0.8, "#c2475a"); gr.addColorStop(1, "#ff9a5c");
		g.fillStyle = gr; g.fillRect(ix, iy, iw, ih);
		g.fillStyle = "#ffd29a"; g.beginPath(); g.arc(ix + iw * 0.62, iy + ih * 0.78, ih * 0.22, 0, TAU); g.fill();
		cloudBlob(ix - iw * 0.05, iy + ih * 0.72, iw * 1.1, ih * 0.3, "#fff6f2", 0.95);
		double sq = ih * 0.16, gy = iy + ih * 0.74 - sq;
		for (int i = 0; i < TM_N; i++) { double cx = ix + iw / 2 + (i - 1) * sq * 1.6; g.shadowColor = TM_COL[i]; g.shadowBlur = sq * 0.6; g.fillStyle = TM_COL[i]; g.fillRect(cx - sq / 2, gy, sq, sq); g.shadowBlur = 0; g.fillStyle = "#1b0f26"; g.fillRect(cx - sq / 6, gy + sq / 3, sq / 3, sq / 3); }
		g.restore();
	}
	// the end: the photo comes out in the big frame, then fills the screen while the song plays out
	static void tmDrawWin(double now)
	{
		if (!tm.th.Any(s => s.k == "slot")) return;
		double f = tm.t - tm.wonAt, a = clamp(f / 1.0, 0, 1), z = ease(clamp((f - 2.2) / 1.8, 0, 1));
		var (fx, fy, fw, fh) = tmFrameRect();
		double tw = 720, th = tw * fh / fw; if (th > 520) { th = 520; tw = th * fw / fh; }
		double x = fx + (W / 2 - tw / 2 - fx) * z, y = fy + (H / 2 - th / 2 - fy) * z, w = fw + (tw - fw) * z, h = fh + (th - fh) * z;
		if (z > 0) { g.fillStyle = $"rgba(6,6,20,{str(0.75 * z)})"; g.fillRect(0, 0, W, H); }
		g.save(); g.shadowColor = GOLD; g.shadowBlur = 24 + 12 * pulse; g.strokeStyle = "#ffcf5a"; g.lineWidth = 8 + 6 * z; g.strokeRect(x - 6, y - 6, w + 12, h + 12); g.restore();
		tmPhotoScene(x, y, w, h, a);
		if (f < 0.6) { g.fillStyle = $"rgba(255,255,255,{str(0.8 * (1 - f / 0.6))})"; g.fillRect(0, 0, W, H); }
		// gold sparkles falling over it
		for (int i = 0; i < 40; i++)
		{
			double sx = (i * 97 + 30) % W, sy = ((f * (60 + i % 5 * 20) + i * 53) % (H + 40)) - 20, r = 2 + i % 3;
			g.fillStyle = $"rgba(255,215,120,{str(clamp(f / 2, 0, 0.8) * (0.5 + 0.5 * Math.Sin(now / 200 + i)))})"; g.fillRect(sx - r / 2, sy - r / 2, r, r);
		}
	}
}
