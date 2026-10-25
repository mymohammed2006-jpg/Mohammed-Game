using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Rating a level after a win (v50): "عجبتني / ما عجبتني" + 0 to 5 stars. The card opens over the win screen when the player leaves it
// (play again or back to the list), once per level: sent or skipped, it doesn't come back for that level.
public static partial class G
{
	public class RateCard { public Level L; public int like = -1, st = -1, row; public double at, msgAt = -1e9, sentAt = -1e9; public Action then; }
	public static RateCard rateC;
	static readonly string GOLDS = "#ffd54a";

	static void star(double cx, double cy, double r)
	{
		g.beginPath();
		for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5, rr_ = i % 2 == 0 ? r : r * 0.45; double x = cx + rr_ * Math.Cos(a), y = cy + rr_ * Math.Sin(a); if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
		g.closePath();
	}

	public static void drawRate(double now)
	{
		var c = rateC; if (c == null) return;
		double e = clamp((now - c.at) / 220, 0, 1), ee = 1 - JS.pow(1 - e, 3);
		g.fillStyle = $"rgba(0,0,0,{str(0.55 * ee)})"; g.fillRect(0, 0, W, H);
		double bw = 640, bh = 370, bx = W / 2 - bw / 2, by = H / 2 - bh / 2 + 30 * (1 - ee);
		g.save(); g.globalAlpha = ee;
		rr(bx, by, bw, bh, 22); g.fillStyle = "#0c0916"; g.fill(); g.shadowColor = RED; g.shadowBlur = 18; g.strokeStyle = RED; g.lineWidth = 2.5; g.stroke(); g.shadowBlur = 0;
		txt("وش رأيك بالمرحلة؟", W / 2, by + 52, 40, "#fff");
		txt(c.L.name + (c.L.song != null ? "  ·  " + c.L.song : ""), W / 2, by + 94, 24, "rgba(255,255,255,0.6)", 500);
		// row 0: liked / didn't (right = liked, the game reads right to left)
		double ry = by + 160, w1 = 230, h1 = 64;
		for (int k = 0; k < 2; k++)
		{
			int v = k == 0 ? 1 : 0; double x = k == 0 ? W / 2 + 12 : W / 2 - 12 - w1; bool on = c.like == v; int kk = v;
			zone(x, ry - h1 / 2, w1, h1, () => c.row = 0, "confirm", () => { c.like = kk; c.row = 1; });
			rr(x, ry - h1 / 2, w1, h1, 16);
			string col = v == 1 ? GOLDS : "#c9c4dc";
			g.fillStyle = on ? (v == 1 ? "rgba(255,213,74,0.22)" : "rgba(201,196,220,0.18)") : "rgba(255,255,255,0.05)"; g.fill();
			g.strokeStyle = on ? col : "rgba(255,255,255,0.18)"; g.lineWidth = on ? 3 : 1.5; g.stroke();
			txt(v == 1 ? "عجبتني" : "ما عجبتني", x + w1 / 2, ry + 2, 30, on ? col : "rgba(255,255,255,0.55)", 800);
		}
		// row 1: stars, filled left to right (0 to 5)
		double sy = by + 262, sr = 26, sg = 74, sx0 = W / 2 - sg * 2;
		for (int i = 0; i < 5; i++)
		{
			double x = sx0 + i * sg; int n = i + 1; bool on = c.st >= n;
			zone(x - sg / 2, sy - 34, sg, 68, () => c.row = 1, "confirm", () => { c.st = c.st == n && n == 1 ? 0 : n; c.row = 1; });
			star(x, sy, sr);
			if (on) { g.fillStyle = GOLDS; g.shadowColor = GOLDS; g.shadowBlur = 14; g.fill(); g.shadowBlur = 0; }
			else { g.fillStyle = "rgba(255,255,255,0.06)"; g.fill(); g.strokeStyle = "rgba(255,255,255,0.35)"; g.lineWidth = 2; g.stroke(); }
		}
		txt(c.st < 0 ? "كم نجمة تعطيها؟" : c.st == 0 ? "ولا نجمة" : $"{ar(c.st)} من ٥", W / 2, sy + 48, 22, c.st < 0 ? "rgba(255,255,255,0.45)" : "#fff", 500);
		// the selected row
		{
			double hy = c.row == 0 ? ry - h1 / 2 - 10 : sy - 40, hh = c.row == 0 ? h1 + 20 : 104;
			rr(bx + 40, hy, bw - 80, hh, 18); g.strokeStyle = $"rgba({RGB},{str(0.55 + 0.25 * JS.sin(now / 200))})"; g.lineWidth = 2; g.stroke();
		}
		if (now - c.msgAt < 1800) txt(c.like < 0 ? "اختار: عجبتني أو ما عجبتني" : "اختار عدد النجوم", W / 2, by + bh - 24, 20, RED3, 500);
		g.restore(); g.globalAlpha = 1;
		row(new[] { (BTN[kind].move, "اختيار"), (BTN[kind].confirm, "أرسل"), (BTN[kind].back, "تخطّي") }, W / 2, by + bh + 40, 30, "#fff", 22);
	}

	public static void rateInput(Inp inp)
	{
		var c = rateC; if (c == null || performance.now() - c.at < 250) return;
		if (inp.up) c.row = 0; if (inp.down) c.row = 1;
		if (c.row == 0 && (inp.left || inp.right)) { c.like = inp.right ? 1 : 0; }
		else if (c.row == 1 && inp.right) c.st = Math.Min(5, c.st < 0 ? 1 : c.st + 1);
		else if (c.row == 1 && inp.left) c.st = Math.Max(0, c.st < 0 ? 0 : c.st - 1);
		if (inp.confirm)
		{
			if (c.like < 0) { c.row = 0; c.msgAt = performance.now(); }
			else if (c.st < 0) { c.row = 1; c.msgAt = performance.now(); }
			else rateDone(true);
		}
		else if (inp.back || inp.pz) rateDone(false);
	}
	static void rateDone(bool send)
	{
		var c = rateC; rateC = null;
		c.then?.Invoke();
	}

	// "تقييم المراحل": what the players said, one row per level (only on Mohammed's account)
	public class RateSum { public Level L; public int n, likes, stars; public double avg => n > 0 ? (double)stars / n : -1; }
	public static List<RateSum> rateSums = new(); public static int rateSel, rateFirst; public static bool rateBusy, rateErr;
	public static void drawRatings(double now)
	{
		bgShapes(now); txt("تقييم المراحل", W / 2, 62, 46, "#fff");
		double top = 150, rh = 46, lx = W / 2 - 420, rw = 840;
		txt("المرحلة", lx + rw - 20, top - 30, 20, "rgba(255,255,255,0.5)", 500, "right");
		txt("النجوم", lx + 330, top - 30, 20, "rgba(255,255,255,0.5)", 500);
		txt("عجبتني", lx + 200, top - 30, 20, "rgba(255,255,255,0.5)", 500);
		txt("ما عجبتني", lx + 80, top - 30, 20, "rgba(255,255,255,0.5)", 500);
		if (rateBusy && rateSums.Count == 0) { txt("جاري التحميل…", W / 2, top + 120, 26, "rgba(255,255,255,0.6)", 500); return; }
		if (rateErr && rateSums.Count == 0) { txt("ما فيه نت، ما قدرت أجيب التقييمات", W / 2, top + 120, 26, "#ffb35c", 500); return; }
		for (int i = rateFirst; i < Math.Min(rateSums.Count, rateFirst + 10); i++)
		{
			var x = rateSums[i]; double y = top + (i - rateFirst) * rh; bool sl = i == rateSel;
			rr(lx, y - rh / 2 + 3, rw, rh - 6, 10); g.fillStyle = sl ? $"rgba({RGB},0.16)" : "rgba(255,255,255,0.04)"; g.fill(); if (sl) { g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			txt(lvName(x.L), lx + rw - 20, y, 22, "#fff", 800, "right");
			if (!string.IsNullOrEmpty(x.L.song)) { g.font = $"15px {TFONT}"; g.textAlign = "right"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = "rgba(255,255,255,0.45)"; g.fillText(x.L.song, lx + rw - 250, y + 1); g.direction = "rtl"; }
			if (x.n == 0) { txt("محد قيّمها للحين", lx + 200, y, 18, "rgba(255,255,255,0.35)", 500); continue; }
			star(lx + 380, y, 11); g.fillStyle = GOLDS; g.fill();
			txt($"{ar(Math.Round(x.avg * 10) / 10)} من ٥", lx + 320, y, 21, GOLDS, 800);
			txt(ar(x.likes), lx + 200, y, 22, "#fff", 800);
			txt(ar(x.n - x.likes), lx + 80, y, 22, "rgba(255,255,255,0.7)", 800);
		}
		if (rateSums.Count > 0) { int tot = rateSums.Sum(x => x.n); txt($"كل التقييمات: {ar(tot)}", W / 2, top + 10 * rh + 8, 20, "rgba(255,255,255,0.55)", 500); }
		row(new[] { (BTN[kind].move, "فوق وتحت"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
}
