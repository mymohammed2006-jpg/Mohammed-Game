using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// F9 "I'm Still Here" (Sia), azm511's level: the one the past wants dead is still here.
// part 1 the city at night (street lamps, the tower's windows lit by the shadow, the traffic, the towers' shadows, the towers falling),
// part 2 the sea at sunset (waves from the horizon, tears and their ripples, the lighthouse and the tide, the moon's broken reflection),
// part 3 the coffin (nails, the lid and the dirt, the skull's eyes, the cracks; at 2:30 it opens).
// Yellow light is only the look of things; only pink hurts. Every hazard is a pure function of time once it exists.
// The data is data/dodge_F9.json (made by tools/friend9/gen.py).
public static partial class G
{
	static double F9k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string F9P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string F9G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	static string F9Y(double a) => $"rgba(255,198,96,{str(clamp(a, 0, 1))})";
	static double F9Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	static bool F9Rect(Player p, double x, double y, double w, double h)
	{
		double cx = clamp(p.x, x, x + w), cy = clamp(p.y, y, y + h); return hypot(p.x - cx, p.y - cy) < p.r - 2;
	}
	static void F9Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double F9Lerp(double[] kb, double[] kv, double bt)
	{
		int n = kb.Length; if (n == 0) return 0; if (bt <= kb[0]) return kv[0]; if (bt >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && bt >= kb[i + 1]) i++;
		double u = (bt - kb[i]) / (kb[i + 1] - kb[i]); u = u * u * (3 - 2 * u); return kv[i] + (kv[i + 1] - kv[i]) * u;
	}
	static double F9Lin(double[] kb, double[] kv, double bt)
	{
		int n = kb.Length; if (n == 0) return 0; if (bt <= kb[0]) return kv[0]; if (bt >= kb[n - 1]) return kv[n - 1];
		int i = 0; while (i < n - 2 && bt >= kb[i + 1]) i++;
		return kv[i] + (kv[i + 1] - kv[i]) * (bt - kb[i]) / (kb[i + 1] - kb[i]);
	}
	static double F9h(int i) { unchecked { uint x = (uint)(i * 2654435761u) ^ 0x9e3779b9u; x ^= x >> 15; x *= 0x85ebca6bu; x ^= x >> 13; return (x % 10000) / 10000.0; } }

	// ---------- the look of things ----------
	// a skyline of towers with warm windows (layer 0 far, 1 near), bottom at y0
	static void F9Skyline(int layer, double y0, double a)
	{
		double x = -20; int i = layer * 100;
		while (x < W + 20)
		{
			double w = (layer == 0 ? 70 : 96) + F9h(i) * (layer == 0 ? 60 : 80), ht = (layer == 0 ? 160 : 230) + F9h(i + 7) * (layer == 0 ? 220 : 260);
			g.fillStyle = layer == 0 ? $"rgba(20,22,40,{str(a)})" : $"rgba(6,7,14,{str(a)})"; g.fillRect(x, y0 - ht, w, ht);
			if (F9h(i + 3) > 0.6) { g.fillRect(x + w * 0.42, y0 - ht - 40, w * 0.16, 40); g.fillRect(x + w * 0.48, y0 - ht - 80, w * 0.04, 40); }
			double ww = layer == 0 ? 6 : 9, wh = layer == 0 ? 8 : 12, sx = layer == 0 ? 13 : 19, sy = layer == 0 ? 16 : 22;
			for (double yy = y0 - ht + 14; yy < y0 - 16; yy += sy)
				for (double xx = x + 9; xx < x + w - 12; xx += sx)
				{
					double q = F9h((int)(xx * 7 + yy * 13) + i); if (q < 0.42) continue;
					g.fillStyle = F9Y(a * (layer == 0 ? 0.18 + 0.2 * q : 0.3 + 0.35 * q)); g.fillRect(xx, yy, ww, wh);
				}
			x += w + (layer == 0 ? 4 : 10) + F9h(i + 5) * 14; i++;
		}
	}
	// a street lamp: post from the floor up to (x, y), the head over x
	static void F9LampPost(double x, double y, double glow)
	{
		g.strokeStyle = "rgba(60,62,80,0.9)"; g.lineWidth = 6; g.beginPath(); g.moveTo(x - 30, H); g.lineTo(x - 30, y - 18); g.quadraticCurveTo(x - 30, y - 30, x - 14, y - 30); g.lineTo(x, y - 30); g.stroke();
		g.fillStyle = "rgba(70,72,92,1)"; g.beginPath(); g.moveTo(x - 18, y - 32); g.lineTo(x + 18, y - 32); g.lineTo(x + 12, y - 8); g.lineTo(x - 12, y - 8); g.closePath(); g.fill();
		if (glow > 0) { var gr = g.createRadialGradient(x, y - 6, 2, x, y - 6, 46); gr.addColorStop(0, F9Y(0.9 * glow)); gr.addColorStop(1, F9Y(0)); g.fillStyle = gr; circle(x, y - 6, 46); }
	}
	// a car (cx, cy centre, dir 1 = to the right); col its body
	static void F9CarDraw(double cx, double cy, int dir, double w, double h, string col)
	{
		g.save(); g.translate(cx, cy); g.scale(dir, 1);
		g.fillStyle = col; g.beginPath(); g.moveTo(-w / 2, h * 0.1); g.lineTo(-w / 2, h / 2); g.lineTo(w / 2, h / 2); g.lineTo(w / 2, h * 0.05); g.lineTo(w * 0.28, -h * 0.1); g.lineTo(w * 0.12, -h / 2); g.lineTo(-w * 0.3, -h / 2); g.lineTo(-w * 0.42, -h * 0.05); g.closePath(); g.fill();
		g.fillStyle = "rgba(20,10,30,0.55)"; g.fillRect(-w * 0.26, -h * 0.4, w * 0.16, h * 0.32); g.fillRect(-w * 0.06, -h * 0.4, w * 0.16, h * 0.32);
		g.fillStyle = "#7a0f33"; circle(-w * 0.3, h / 2, h * 0.22); circle(w * 0.3, h / 2, h * 0.22);   // dark pink: the box hurts there too
		g.fillStyle = "#fff3c8"; g.fillRect(w / 2 - 6, h * 0.08, 6, h * 0.2);
		g.restore();
	}
	// a skull of the past: round, cracked, its eyes two dark holes (k = how much it is cracked)
	static void F9SkullDraw(double x, double y, double r, string col, double k)
	{
		g.save(); g.translate(x, y); g.fillStyle = col;
		g.beginPath(); g.arc(0, -r * 0.1, r, Math.PI * 0.95, Math.PI * 2.05); g.lineTo(r * 0.82, r * 0.42); g.quadraticCurveTo(r * 0.6, r * 0.62, r * 0.34, r * 0.66); g.lineTo(r * 0.3, r * 0.9);
		g.lineTo(-r * 0.3, r * 0.9); g.lineTo(-r * 0.34, r * 0.66); g.quadraticCurveTo(-r * 0.6, r * 0.62, -r * 0.82, r * 0.42); g.closePath(); g.fill();
		g.fillStyle = "rgba(6,3,6,0.95)";
		foreach (var sd in new[] { -1.0, 1.0 }) { g.beginPath(); g.ellipse(sd * r * 0.36, r * 0.12, r * 0.24, r * 0.2, sd * 0.15, 0, TAU); g.fill(); }
		g.beginPath(); g.moveTo(0, r * 0.36); g.lineTo(-r * 0.09, r * 0.56); g.lineTo(r * 0.09, r * 0.56); g.closePath(); g.fill();
		for (int i = -2; i <= 2; i++) g.fillRect(i * r * 0.11 - r * 0.02, r * 0.68, r * 0.04, r * 0.2);
		if (k > 0)
		{
			g.strokeStyle = "rgba(6,3,6,0.9)"; g.lineWidth = Math.Max(2, r * 0.03); g.beginPath();
			g.moveTo(-r * 0.1, -r * 1.08); g.lineTo(-r * 0.02, -r * (1.08 - 0.5 * k)); g.lineTo(-r * 0.16, -r * (1.08 - 0.75 * k)); g.lineTo(-r * 0.05, -r * (1.08 - 1.0 * k));
			g.moveTo(r * 0.5, -r * 0.9); g.lineTo(r * (0.5 - 0.2 * k), -r * (0.9 - 0.35 * k)); g.lineTo(r * (0.5 - 0.1 * k), -r * (0.9 - 0.6 * k)); g.stroke();
		}
		g.restore();
	}

	// ---------- the stage (harmless) ----------
	// part 1 the city at night, 2 the sea at sunset, 3 under the ground (the coffin)
	public static Hz F9Stage(int part, double t0, double t1)
	{
		var h = new Hz { kind = "F9Stage", veil = true };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.8, (t1 - t) / 0.8)); if (a <= 0) return; double k = F9k(t, t0, t1); g.save(); g.globalAlpha = a;
			if (part == 1)
			{
				var sk = g.createLinearGradient(0, 0, 0, H); sk.addColorStop(0, "#05060f"); sk.addColorStop(0.7, "#0e1024"); sk.addColorStop(1, "#1a1426"); g.fillStyle = sk; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(255,240,210,0.5)"; for (int i = 0; i < 40; i++) g.fillRect(F9h(i) * W, F9h(i + 99) * H * 0.45, 2, 2);
				F9Skyline(0, H - 30, 0.9); F9Skyline(1, H - 30, 1);
				g.fillStyle = "#0a0a12"; g.fillRect(0, H - 30, W, 30); g.fillStyle = F9Y(0.35); for (double x = (-(t * 90) % 80); x < W; x += 80) g.fillRect(x, H - 16, 40, 3);
			}
			else if (part == 2)
			{
				const double HZ = 150; double s = k;
				var sk = g.createLinearGradient(0, 0, 0, HZ);
				sk.addColorStop(0, s < 0.5 ? "#2a1b4a" : "#0b0d26"); sk.addColorStop(1, s < 0.35 ? "#ff9a4a" : s < 0.7 ? "#a8507a" : "#2a2350"); g.fillStyle = sk; g.fillRect(0, 0, W, HZ);
				// the sun going down, the moon coming up
				double sy = HZ - 40 + 120 * F9k(t, t0, t0 + (t1 - t0) * 0.6); g.save(); g.beginPath(); g.rect(0, 0, W, HZ); g.clip();
				var sg = g.createRadialGradient(W / 2, sy, 10, W / 2, sy, 160); sg.addColorStop(0, "rgba(255,190,90,0.95)"); sg.addColorStop(0.35, "rgba(255,150,70,0.5)"); sg.addColorStop(1, "rgba(255,150,70,0)"); g.fillStyle = sg; circle(W / 2, sy, 160);
				g.fillStyle = "#ffd27a"; circle(W / 2, sy, 52); g.restore();
				double mk = F9k(t, t0 + (t1 - t0) * 0.35, t0 + (t1 - t0) * 0.75), my = HZ + 30 - 120 * mk;
				if (mk > 0) { g.save(); g.beginPath(); g.rect(0, 0, W, HZ); g.clip(); g.fillStyle = "rgba(244,236,214,0.95)"; circle(W * 0.5, my, 34); g.fillStyle = "rgba(200,190,170,0.4)"; circle(W * 0.5 - 10, my - 6, 8); circle(W * 0.5 + 12, my + 8, 6); g.restore(); }
				var sea = g.createLinearGradient(0, HZ, 0, H); sea.addColorStop(0, s < 0.5 ? "#3a2a52" : "#0c1430"); sea.addColorStop(1, "#05070f"); g.fillStyle = sea; g.fillRect(0, HZ, W, H - HZ);
				for (int i = 0; i < 18; i++)
				{
					double yy = HZ + 8 + i * i * 1.75, ox = (t * (20 + i * 4) + i * 77) % 140;
					g.fillStyle = s < 0.45 ? $"rgba(255,170,90,{str(0.25 - i * 0.01)})" : $"rgba(240,232,210,{str(0.2 - i * 0.008)})";
					for (double x = W / 2 - 40 - i * 10 - ox * 0.2; x < W / 2 + 40 + i * 10; x += 26 + i) g.fillRect(x, yy, 12 + i, 2);
				}
				g.strokeStyle = "rgba(255,220,180,0.3)"; g.lineWidth = 1; g.beginPath(); g.moveTo(0, HZ); g.lineTo(W, HZ); g.stroke();
			}
			else
			{
				g.fillStyle = "#0b0706"; g.fillRect(0, 0, W, H);
				g.fillStyle = "rgba(60,40,28,0.35)"; for (int i = 0; i < 90; i++) { double x = F9h(i) * W, y = F9h(i + 300) * H; g.fillRect(x, y, 3 + F9h(i + 600) * 6, 2 + F9h(i + 900) * 4); }
				var gr = g.createRadialGradient(W / 2, H / 2, 200, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.8)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			g.restore();
		};
		return h;
	}

	// a part's start: white and shake, fading over dur s (2 at most)
	public static Hz F9Fx(double t0, double v, double s, double dur)
	{
		var h = new Hz { kind = "F9fx" };
		h.update = (t, dt) => { double k = 1 - (t - t0) / dur; if (k <= 0) { h.dead = true; return; } flash = Math.Max(flash, v * k); shake = Math.Max(shake, s * k); };
		return h;
	}

	// ---------- part 1: the city ----------
	// a street lamp at x: its light falls pink at each of tas for on s, warm (harmless) wn s before each. The post shows from t0 to te
	const double F9LY = 92;
	static double F9ConeW(double y, double sc) => (14 + 66 * clamp((y - F9LY) / (H - F9LY), 0, 1)) * sc;
	public static Hz F9Lamp(double x, double[] tas, double wn, double on, double t0, double te, double sc)
	{
		var h = new Hz { kind = "F9Lamp" };
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, t0, t0 + 0.5), 1 - F9k(t, te, te + 0.3)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			bool act = false; double wk = 0, fade = 0;
			foreach (var ta in tas)
			{
				if (t >= ta && t < ta + on) act = true;
				else if (t >= ta - wn && t < ta) wk = Math.Max(wk, F9k(t, ta - wn, ta));
				else if (t >= ta + on && t < ta + on + 0.2) fade = Math.Max(fade, 1 - (t - ta - on) / 0.2);
			}
			F9LampPost(x, F9LY, act ? 1 : 0.35 + 0.5 * wk);   // the post first: its light (pink when it hurts) goes over it
			if (act || wk > 0 || fade > 0)
			{
				bool fl = false; foreach (var ta in tas) if (t >= ta && t < ta + 0.05) fl = true;
				g.fillStyle = act ? (fl ? PINKW : F9P(0.78)) : wk > 0 ? F9Y(0.06 + 0.16 * wk) : F9P(0.5 * fade);
				g.beginPath(); g.moveTo(x - F9ConeW(F9LY, sc), F9LY); g.lineTo(x + F9ConeW(F9LY, sc), F9LY); g.lineTo(x + F9ConeW(H, sc), H); g.lineTo(x - F9ConeW(H, sc), H); g.closePath(); g.fill();
				if (!act && wk > 0) { g.strokeStyle = F9G(0.3 + 0.4 * wk); g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 }); g.stroke(); g.setLineDash(new double[0]); }
			}
			if (act) { g.fillStyle = PINK; circle(x, F9LY - 6, 9); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (p.y + p.r < F9LY) return false; bool act = false; foreach (var ta in tas) if (t >= ta && t < ta + on) act = true; if (!act) return false;
			double hw = F9ConeW(clamp(p.y, F9LY, H), sc); return Math.Abs(p.x - x) < hw + p.r - 2;
		};
		return h;
	}

	// the tower: its windows (9 columns) fill the screen and rise with the camera from b0 (v px a beat). Window cells[j] (row * 9 + column)
	// lights pink at tb[j] for on beats, warm from tb - wn (the shadow walking through the floor)
	const double F9FX0 = 25, F9FPX = 140, F9FW = 110, F9FPY = 116, F9FH = 84, F9SP = 18;
	public static Hz F9Facade(double[] cells, double[] tb, double on, double wn, double b0, double v, double tIn, double te)
	{
		var h = new Hz { kind = "F9Facade" }; int n = cells.Length;
		double scr(double bt) => Math.Max(0, bt - b0) * v;
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tIn, tIn + 0.7), 1 - F9k(t, te, te + 0.5)); if (a <= 0) return; double bt = BT(t), s = scr(bt);
			g.save(); g.globalAlpha = a;
			for (int j = 0; j < n; j++)
			{
				double t0 = tb[j]; if (bt < t0 - wn || bt > t0 + on + 0.3) continue;
				int r = (int)cells[j] / 9, c = (int)cells[j] % 9; double x = F9FX0 + c * F9FPX, y = r * F9FPY + 20 - s; if (y > H || y + F9FH < 0) continue;
				if (bt < t0)
				{
					double k = F9k(bt, t0 - wn, t0); g.fillStyle = F9Y(0.25 + 0.5 * k); g.fillRect(x, y, F9FW, F9FH);
					g.strokeStyle = F9G(0.5 + 0.4 * k); g.lineWidth = 2; g.strokeRect(x + 2, y + 2, F9FW - 4, F9FH - 4);
					// the shadow in the window
					g.fillStyle = $"rgba(8,6,12,{str(0.3 + 0.6 * k)})"; circle(x + F9FW / 2, y + 26, 11); g.fillRect(x + F9FW / 2 - 15, y + 38, 30, F9FH - 38);
				}
				else
				{
					double f = bt < t0 + on ? 1 : 1 - (bt - t0 - on) / 0.3; g.fillStyle = F9P(0.7 * f); g.fillRect(x - F9SP, y - F9SP, F9FW + 2 * F9SP, F9FH + 2 * F9SP);   // the light around it hurts too: clearly pink
					g.fillStyle = bt < t0 + 0.07 ? PINKW : F9P(0.95 * f); g.fillRect(x, y, F9FW, F9FH);
					g.fillStyle = $"rgba(70,0,22,{str(0.6 * f)})"; circle(x + F9FW / 2, y + 26, 11); g.fillRect(x + F9FW / 2 - 15, y + 38, 30, F9FH - 38);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t), s = scr(bt);
			for (int j = 0; j < n; j++)
			{
				if (bt < tb[j] || bt >= tb[j] + on) continue;
				int r = (int)cells[j] / 9, c = (int)cells[j] % 9;
				if (F9Rect(p, F9FX0 + c * F9FPX - F9SP, r * F9FPY + 20 - s - F9SP, F9FW + 2 * F9SP, F9FH + 2 * F9SP)) return true;
			}
			return false;
		};
		return h;
	}

	// the tower's wall behind the windows (harmless, a place): rises with the camera like F9Facade
	public static Hz F9FacadeWall(double b0, double v, double tIn, double te)
	{
		var h = new Hz { kind = "F9FacadeWall" };
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tIn, tIn + 0.7), 1 - F9k(t, te, te + 0.5)); if (a <= 0) return; double s = Math.Max(0, BT(t) - b0) * v;
			g.save(); g.globalAlpha = a;
			g.fillStyle = "#0b0c16"; g.fillRect(0, 0, W, H);
			int r0 = (int)Math.Floor((s - 20) / F9FPY) - 1, r1 = r0 + (int)(H / F9FPY) + 3;
			for (int r = r0; r <= r1; r++)
			{
				double y = r * F9FPY + 20 - s; g.fillStyle = "rgba(255,255,255,0.03)"; g.fillRect(0, y + F9FH + 16, W, 4);
				for (int c = 0; c < 9; c++)
				{
					double x = F9FX0 + c * F9FPX, q = F9h(r * 9 + c + 5000);
					g.fillStyle = q > 0.55 ? F9Y(0.07 + 0.08 * q) : "rgba(20,22,38,1)"; g.fillRect(x, y, F9FW, F9FH);
					g.fillStyle = "rgba(0,0,0,0.35)"; g.fillRect(x + F9FW / 2 - 1, y, 2, F9FH);
				}
			}
			g.restore();
		};
		return h;
	}

	// a row of windows across the screen (y centre) lights pink at ta for on s; the walls between them (gaps) are safe
	public static Hz F9Row(double y, double ox, double tw, double ta, double on)
	{
		var h = new Hz { kind = "F9Row" }; const double WW = 100, PX = 140, HH = 54;
		h.update = (t, dt) => { if (t > ta + on + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double k = F9k(t, tw, ta);
			for (double x = ox - PX; x < W; x += PX)
			{
				if (t < ta) { g.fillStyle = F9Y(0.08 + 0.3 * k); g.fillRect(x, y - HH / 2, WW, HH); g.strokeStyle = F9G(0.3 + 0.5 * k); g.lineWidth = 2; g.strokeRect(x + 1, y - HH / 2 + 1, WW - 2, HH - 2); }
				else { double f = t < ta + on ? 1 : 1 - (t - ta - on) / 0.3; g.fillStyle = t < ta + 0.05 ? PINKW : F9P(0.9 * f); g.fillRect(x, y - HH / 2, WW, HH); }
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= ta + on || Math.Abs(p.y - y) > HH / 2 + p.r) return false;
			for (double x = ox - PX; x < W; x += PX) if (F9Rect(p, x, y - HH / 2, WW, HH)) return true;
			return false;
		};
		return h;
	}

	// the roads (harmless): 6 lanes of 120 px
	public static Hz F9Roads(double t0, double t1)
	{
		var h = new Hz { kind = "F9Roads" };
		h.update = (t, dt) => { if (t > t1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, t0, t0 + 0.8), 1 - F9k(t, t1, t1 + 0.8)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			for (int l = 0; l < 6; l++)
			{
				double y = l * 120; g.fillStyle = l % 2 == 0 ? "rgba(22,22,32,0.92)" : "rgba(28,27,38,0.92)"; g.fillRect(0, y + 6, W, 108);
				g.fillStyle = "rgba(90,90,110,0.6)"; g.fillRect(0, y + 4, W, 3); g.fillRect(0, y + 113, W, 3);
				g.fillStyle = F9Y(0.22); double ofs = ((l % 2 == 0 ? 1 : -1) * t * 60) % 90; for (double x = ofs - 90; x < W; x += 90) g.fillRect(x, y + 59, 44, 3);
			}
			g.restore();
		};
		return h;
	}

	// a car on lane ln (centre y = 60 + 120 ln), dir 1 from the left: its front reaches the screen at ta and it goes sp px a second.
	// Its lights go pink (high beam, len px ahead) at each hb for hl s. An arrow shows the lane from tw
	public static Hz F9Car(int ln, int dir, double tw, double ta, double sp, double[] hb, double hl, double len, double cw)
	{
		var h = new Hz { kind = "F9Car" }; double cy = 60 + 120 * ln, ch = 50, x0 = dir > 0 ? -cw / 2 : W + cw / 2;
		double cx(double t) => x0 + dir * sp * (t - ta);
		bool beam(double t) { foreach (var b in hb) if (t >= b && t < b + hl) return true; return false; }
		h.update = (t, dt) => { if (t > ta && (dir > 0 ? cx(t) - cw / 2 > W + 20 : cx(t) + cw / 2 < -20) && t > (hb.Length > 0 ? hb.Max() + hl : 0)) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta + 0.3)
			{
				double k = F9k(t, tw, ta); double ex = dir > 0 ? 18 : W - 18; g.strokeStyle = F9G(0.25 + 0.6 * k); g.lineWidth = 4; g.beginPath();
				for (int i = 0; i < 3; i++) { double ax = ex + dir * i * 22; g.moveTo(ax - dir * 8, cy - 16); g.lineTo(ax + dir * 8, cy); g.lineTo(ax - dir * 8, cy + 16); }
				g.stroke();
			}
			double x = cx(t), fx = x + dir * cw / 2; bool on = beam(t);
			// lights
			double bl = on ? len : 150; var gr = g.createLinearGradient(fx, cy, fx + dir * bl, cy);
			gr.addColorStop(0, on ? F9P(0.85) : F9Y(0.35)); gr.addColorStop(1, on ? F9P(0.55) : F9Y(0));
			g.fillStyle = gr; g.beginPath(); g.moveTo(fx, cy - 6); g.lineTo(fx + dir * bl, cy - (on ? 40 : 30)); g.lineTo(fx + dir * bl, cy + (on ? 40 : 30)); g.lineTo(fx, cy + 12); g.closePath(); g.fill();
			// the high beam coming: a dashed outline a beat before
			foreach (var b in hb) if (t >= b - SPB && t < b) { double k = F9k(t, b - SPB, b); g.strokeStyle = F9G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.beginPath(); g.moveTo(fx, cy - 6); g.lineTo(fx + dir * len, cy - 40); g.lineTo(fx + dir * len, cy + 40); g.lineTo(fx, cy + 12); g.closePath(); g.stroke(); g.setLineDash(new double[0]); }
			if (t >= ta) { g.fillStyle = F9P(0.45); g.fillRect(x - cw / 2, cy - ch / 2, cw, ch); }   // all of the box that hurts is pink, not only the car
			F9CarDraw(x, cy, dir, cw, ch, PINK);
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double x = cx(t);
			if (F9Rect(p, x - cw / 2, cy - ch / 2, cw, ch)) return true;
			if (!beam(t)) return false; double fx = x + dir * cw / 2, d = (p.x - fx) * dir;
			if (d < 0 || d > len + p.r - 2) return false; double hw = 9 + 31 * clamp(d / len, 0, 1); return Math.Abs(p.y - cy) < hw + p.r - 2;
		};
		return h;
	}

	// the towers' shadows: a car's lights pass far below, so each tower (base xs[i] at the floor) throws its shadow up and away, a pink band
	// bw wide, all at the same angle (ka degrees from the right, up, at beats kb), broken where the light comes through its windows
	// (gaps: from, to px along the band, in pairs). Grey from tw, pink from ta to te
	public static Hz F9Shadows(double[] xs, double[] ths, double[] kb, double[] ka, double bw, double[] gaps, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9Shadows" }; int n = xs.Length; const double LEN = 1600, BY = H + 10;
		(double dx, double dy) dirOf(int i, double bt) { double a = F9Lin(kb, ka, bt) * Math.PI / 180; return (JS.cos(a), -JS.sin(a)); }
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw - 0.4, tw), 1 - F9k(t, te, te + 0.4)); if (a <= 0) return; double bt = BT(t); bool act = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				var d = dirOf(i, bt); double ex = xs[i] + d.dx * LEN, ey = BY + d.dy * LEN, nx = -d.dy * bw, ny = d.dx * bw;
				if (act)
				{
					var gr = g.createLinearGradient(xs[i], BY, xs[i] + d.dx * 700, BY + d.dy * 700); gr.addColorStop(0, F9P(0.9)); gr.addColorStop(1, F9P(0.6)); g.fillStyle = gr;
					double u0 = 0;
					for (int k = 0; k <= gaps.Length; k += 2)
					{
						double u1 = k < gaps.Length ? gaps[k] : LEN; if (u1 <= u0) { if (k + 1 < gaps.Length) u0 = gaps[k + 1]; continue; }
						double ax = xs[i] + d.dx * u0, ay = BY + d.dy * u0, bx = xs[i] + d.dx * u1, by = BY + d.dy * u1;
						g.beginPath(); g.moveTo(ax + nx, ay + ny); g.lineTo(bx + nx, by + ny); g.lineTo(bx - nx, by - ny); g.lineTo(ax - nx, ay - ny); g.closePath(); g.fill();
						if (k + 1 < gaps.Length) u0 = gaps[k + 1];
					}
					// the light through the windows
					for (int k = 0; k + 1 < gaps.Length; k += 2) { double cx = xs[i] + d.dx * (gaps[k] + gaps[k + 1]) / 2, cy = BY + d.dy * (gaps[k] + gaps[k + 1]) / 2; g.fillStyle = F9Y(0.35); circle(cx, cy, 10); g.fillStyle = gr; }
				}
				else { F9Dash(xs[i] + nx, BY + ny, ex + nx, ey + ny, F9G(0.5)); F9Dash(xs[i] - nx, BY - ny, ex - nx, ey - ny, F9G(0.5)); }
			}
			// the light far below: a glow on the street
			double la = F9Lin(kb, ka, bt) * Math.PI / 180, lx = W / 2 - JS.cos(la) / Math.Max(0.15, JS.sin(la)) * 260; var lg = g.createRadialGradient(lx, H, 4, lx, H, 140); lg.addColorStop(0, F9Y(0.7)); lg.addColorStop(1, F9Y(0)); g.fillStyle = lg; g.fillRect(lx - 140, H - 140, 280, 140);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double bt = BT(t);
			for (int i = 0; i < n; i++)
			{
				var d = dirOf(i, bt); if (F9Seg(p.x, p.y, xs[i], BY, xs[i] + d.dx * LEN, BY + d.dy * LEN) >= bw + p.r - 2) continue;
				double u = (p.x - xs[i]) * d.dx + (p.y - BY) * d.dy; bool inGap = false;
				for (int k = 0; k + 1 < gaps.Length; k += 2) if (u > gaps[k] + p.r - 2 && u < gaps[k + 1] - p.r + 2) inGap = true;
				if (!inGap) return true;
			}
			return false;
		};
		return h;
	}

	// the towers that throw the shadows (harmless, a place): drawn with F9Shadows' fade
	public static Hz F9ShadowTowers(double[] xs, double[] ths, double tw, double te)
	{
		var h = new Hz { kind = "F9ShadowTowers" }; int n = xs.Length;
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw - 0.4, tw), 1 - F9k(t, te, te + 0.4)); if (a <= 0) return; g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double tw2 = 76, ty = H - ths[i]; g.fillStyle = "#151726"; g.fillRect(xs[i] - tw2 / 2, ty, tw2, ths[i]); g.fillRect(xs[i] - 4, ty - 50, 8, 50);
				g.strokeStyle = "rgba(255,198,96,0.35)"; g.lineWidth = 2; g.strokeRect(xs[i] - tw2 / 2, ty, tw2, ths[i]);
				for (double yy = H - ths[i] + 12; yy < H - 12; yy += 20) for (int c = 0; c < 3; c++) if (F9h((int)yy * 3 + c + i * 31) > 0.35) { g.fillStyle = F9Y(0.55); g.fillRect(xs[i] - 24 + c * 18, yy, 10, 11); }
			}
			g.restore();
		};
		return h;
	}

	// a tower falls: base at (x, floor), it tips over toward dir (on its corner on that side) from tf and lies flat from ta, pink, hold s.
	// len tall, w wide. Grey path from tw
	public static Hz F9Topple(double x, int dir, double len, double w, double tw, double tf, double ta, double hold)
	{
		var h = new Hz { kind = "F9Topple" }; const double BY = H;
		double ang(double t) => -Math.PI / 2 + dir * Math.PI / 2 * JS.pow(F9k(t, tf, ta), 2);   // straight up, then flat toward dir
		h.update = (t, dt) => { if (t > ta + hold + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw - 0.5) return; double a = Math.Min(F9k(t, tw - 0.5, tw), 1 - F9k(t, ta + hold, ta + hold + 0.4)); g.save(); g.globalAlpha = a;
			if (t < tf)
			{
				double k = F9k(t, tw, tf); g.strokeStyle = F9G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath();
				double bx = x + dir * w / 2; g.arc(bx, BY, len, -Math.PI / 2, dir > 0 ? 0 : -Math.PI, dir < 0); g.stroke();
				g.strokeRect(dir > 0 ? bx : bx - len, BY - w, len, w); g.setLineDash(new double[0]);
			}
			double an = ang(t); bool act = t >= tf; if (!act) { g.restore(); return; }   // standing: F9ToppleStand draws it
			g.save(); g.translate(x + dir * w / 2, BY); g.rotate(an + Math.PI / 2);
			// local: the tower goes up from (0,0) to (0,-len), w wide away from dir (the corner it turns on is the origin)
			double lx = dir > 0 ? -w : 0;
			g.fillStyle = act ? (t < tf + 0.05 ? PINKW : PINK) : "#151726"; g.fillRect(lx, -len, w, len);
			if (!act) { g.strokeStyle = "rgba(255,198,96,0.4)"; g.lineWidth = 2; g.strokeRect(lx, -len, w, len); }
			for (double yy = -len + 16; yy < -14; yy += 24) for (int c = 0; c < 3; c++) if (F9h((int)yy * 5 + c + (int)x) > 0.35) { g.fillStyle = act ? "rgba(90,0,30,0.5)" : F9Y(0.6); g.fillRect(lx + 12 + c * (w - 24) / 3, yy, 14, 12); }
			g.restore(); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf || t >= ta + hold) return false; double an = ang(t) + Math.PI / 2, bx = x + dir * w / 2;
			// player in the tower's own frame
			double dx = p.x - bx, dy = p.y - BY, c = JS.cos(-an), s = JS.sin(-an), lx = dx * c - dy * s, ly = dx * s + dy * c, x0 = dir > 0 ? -w : 0;
			double cx = clamp(lx, x0, x0 + w), cy = clamp(ly, -len, 0); return hypot(lx - cx, ly - cy) < p.r - 2;
		};
		return h;
	}

	// the tower before it falls (harmless, a place): F9Topple's tower standing till tf
	public static Hz F9ToppleStand(double x, int dir, double len, double w, double tw, double tf)   // x: as F9Topple's
	{
		var h = new Hz { kind = "F9ToppleStand" }; const double BY = H;
		h.update = (t, dt) => { if (t >= tf) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw - 0.5 || t >= tf) return; g.save(); g.globalAlpha = F9k(t, tw - 0.5, tw);
			double bx = x + dir * w / 2, lx = dir > 0 ? bx - w : bx; g.fillStyle = "#151726"; g.fillRect(lx, BY - len, w, len);
			g.strokeStyle = "rgba(255,198,96,0.4)"; g.lineWidth = 2; g.strokeRect(lx, BY - len, w, len);
			for (double yy = -len + 16; yy < -14; yy += 24) for (int c = 0; c < 3; c++) if (F9h((int)yy * 5 + c + (int)x) > 0.35) { g.fillStyle = F9Y(0.6); g.fillRect(lx + 12 + c * (w - 24) / 3, BY + yy, 14, 12); }
			g.restore();
		};
		return h;
	}

	// ---------- part 2: the sea ----------
	const double F9HZ = 150;
	// a wave: from the horizon it comes down to the bottom of the screen in dur s (reaching it at ta). Its break (gap) is at gx, gw wide
	// at the bottom (narrower and nearer the middle far away). up: the backwash, from the bottom up to the top, straight
	public static Hz F9Wave(double ta, double dur, double gx, double gw, bool up)
	{
		var h = new Hz { kind = "F9Wave" }; double t0 = ta - dur;
		(double y, double th, double cx, double hw) at(double t)
		{
			double u = clamp((t - t0) / dur, 0, 1.15);
			if (up) { double y = H + 20 - (H + 60) * u; return (y, 30, gx, gw / 2); }
			double e = JS.pow(u, 1.6), sc = 0.15 + 0.85 * e; return (F9HZ + (H + 20 - F9HZ) * e, 6 + 26 * e, W / 2 + (gx - W / 2) * sc, gw / 2 * sc);
		}
		h.update = (t, dt) => { if (t > ta + dur * 0.16 + 0.1) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; var w = at(t); double a = up ? 1 : clamp((t - t0) / (dur * 0.25), 0.7, 1);
			g.fillStyle = F9P(0.85 * a);
			g.fillRect(0, w.y - w.th / 2, Math.Max(0, w.cx - w.hw), w.th); g.fillRect(w.cx + w.hw, w.y - w.th / 2, W - w.cx - w.hw, w.th);
			// foam on top
			g.fillStyle = $"rgba(255,220,232,{str(0.7 * a)})"; for (double x = (t * 70) % 46 - 46; x < W; x += 46) if (x < w.cx - w.hw - 10 || x > w.cx + w.hw) g.fillRect(x, w.y - w.th / 2 - 3, 24, 4);
			if (!up) { g.strokeStyle = F9G(0.35); g.lineWidth = 2; g.beginPath(); g.moveTo(w.cx - w.hw, w.y - w.th); g.lineTo(w.cx - w.hw, w.y + w.th); g.moveTo(w.cx + w.hw, w.y - w.th); g.lineTo(w.cx + w.hw, w.y + w.th); g.stroke(); }
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; var w = at(t);
			if (Math.Abs(p.y - w.y) >= w.th / 2 + p.r - 2) return false;
			return Math.Abs(p.x - w.cx) > w.hw - p.r + 2;
		};
		return h;
	}

	// a tear (or the moon's pulse when no drop): falls to (x,y) by ta (seen from tw), then a pink ring grows sp px a second, th thick,
	// with a break gw radians wide at ga, up to rmax
	public static Hz F9Ripple(double x, double y, bool drop, double tw, double ta, double sp, double th, double ga, double gw, double rmax)
	{
		var h = new Hz { kind = "F9Ripple" }; double te = ta + rmax / sp;
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F9k(t, tw, ta); g.strokeStyle = F9G(0.25 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 7, 7 }); g.beginPath(); g.arc(x, y, 30 + 30 * (1 - k), 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				g.strokeStyle = F9G(0.4 * k); g.beginPath(); g.arc(x, y, 34, ga - gw / 2, ga + gw / 2); g.lineWidth = 6; g.stroke();
				if (drop) { double dy = y - (y + 40) * (1 - k * k); g.fillStyle = "rgba(220,236,255,0.9)"; g.beginPath(); g.moveTo(x, dy - 18); g.quadraticCurveTo(x + 9, dy - 2, x, dy + 6); g.quadraticCurveTo(x - 9, dy - 2, x, dy - 18); g.fill(); }
				return;
			}
			double R = Math.Min((t - ta) * sp, rmax), f = 1 - F9k(t, te, te + 0.15); if (f <= 0) return;   // full pink while it hurts (till te)
			g.strokeStyle = t < ta + 0.05 ? PINKW : F9P(0.9 * f); g.lineWidth = th; g.beginPath(); g.arc(x, y, Math.Max(1, R), ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t > te) return false; double R = (t - ta) * sp, d = hypot(p.x - x, p.y - y);
			if (Math.Abs(d - R) >= th / 2 + p.r - 2) return false;
			double an = JS.atan2(p.y - y, p.x - x), df = Math.Abs(((an - ga) % TAU + TAU + Math.PI) % TAU - Math.PI), slack = d > 1 ? (p.r - 2) / d : 0;
			return df > gw / 2 - slack;
		};
		return h;
	}

	// the lighthouse on its rock at (x, y): n beams (hw radians half wide) turning on the angle plan ka (degrees) at beats kb. Grey from tw, pink ta..te
	// the lighthouse (harmless, a place): white and red stripes, a lamp on top (pink while its beams hurt)
	public static Hz F9LightHouse(double x, double y, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9LightHouse" };
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw - 0.6, tw), 1 - F9k(t, te, te + 0.6)); if (a <= 0) return; bool act = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			g.fillStyle = "#1a1418"; g.beginPath(); g.ellipse(x, y + 70, 120, 40, 0, 0, TAU); g.fill();
			g.fillStyle = "#e8e2d6"; g.beginPath(); g.moveTo(x - 30, y + 60); g.lineTo(x - 18, y - 30); g.lineTo(x + 18, y - 30); g.lineTo(x + 30, y + 60); g.closePath(); g.fill();
			g.fillStyle = "#7a3a3a"; g.fillRect(x - 27, y + 20, 54, 14); g.fillRect(x - 22, y - 10, 44, 12);
			g.fillStyle = act ? PINK : F9Y(0.9); circle(x, y - 40, 14); g.fillStyle = "#2a2228"; g.fillRect(x - 20, y - 58, 40, 6);
			g.restore();
		};
		return h;
	}

	public static Hz F9Light(double x, double y, int n, double hw, double[] kb, double[] ka, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9Light" }; const double R0 = 46, LEN = 1700;
		double ang(double bt) => F9Lin(kb, ka, bt) * Math.PI / 180;
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw - 0.6, tw), 1 - F9k(t, te, te + 0.6)); if (a <= 0) return; double bt = BT(t), a0 = ang(bt); bool act = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			for (int i = 0; i < n; i++)
			{
				double an = a0 + i * TAU / n;
				if (act) { g.fillStyle = F9P(0.8); g.beginPath(); g.moveTo(x + JS.cos(an - hw) * R0, y + JS.sin(an - hw) * R0); g.lineTo(x + JS.cos(an - hw) * LEN, y + JS.sin(an - hw) * LEN); g.lineTo(x + JS.cos(an + hw) * LEN, y + JS.sin(an + hw) * LEN); g.lineTo(x + JS.cos(an + hw) * R0, y + JS.sin(an + hw) * R0); g.closePath(); g.fill(); }
				else { F9Dash(x + JS.cos(an - hw) * R0, y + JS.sin(an - hw) * R0, x + JS.cos(an - hw) * LEN, y + JS.sin(an - hw) * LEN, F9G(0.5)); F9Dash(x + JS.cos(an + hw) * R0, y + JS.sin(an + hw) * R0, x + JS.cos(an + hw) * LEN, y + JS.sin(an + hw) * LEN, F9G(0.5)); }
			}
			g.restore();   // the lighthouse itself: F9LightHouse (a place)
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double d = hypot(p.x - x, p.y - y); if (d < R0 - p.r) return false;
			double an = JS.atan2(p.y - y, p.x - x), a0 = ang(BT(t)), sl = d > 1 ? Math.Asin(clamp((p.r - 2) / d, 0, 1)) : 0;
			for (int i = 0; i < n; i++) { double b = a0 + i * TAU / n, df = Math.Abs(((an - b) % TAU + TAU + Math.PI) % TAU - Math.PI); if (df < hw + sl) return true; }
			return false;
		};
		return h;
	}

	// the tide: pink water from the bottom up to the line ky at beats kb. Grey line from tw, pink ta..te
	public static Hz F9Tide(double[] kb, double[] ky, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9Tide" };
		h.update = (t, dt) => { if (t > te + 0.6) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), y = t < ta ? H - (H - F9Lerp(kb, ky, BT(ta))) * F9k(t, tw, ta) : t < te ? F9Lerp(kb, ky, bt) : H - (H - F9Lerp(kb, ky, BT(te))) * (1 - F9k(t, te, te + 0.6));
			if (t < ta) { F9Dash(0, F9Lerp(kb, ky, BT(ta)), W, F9Lerp(kb, ky, BT(ta)), F9G(0.6)); g.fillStyle = "rgba(120,140,200,0.15)"; g.fillRect(0, y, W, H - y); return; }
			g.fillStyle = F9P(t < te ? 0.75 : 0.5); g.beginPath(); g.moveTo(0, H); for (double x = 0; x <= W; x += 40) g.lineTo(x, y + 5 * JS.sin(x * 0.03 + t * 4)); g.lineTo(W, H); g.closePath(); g.fill();
			g.fillStyle = "rgba(255,220,232,0.6)"; for (double x = (t * 50) % 60 - 60; x < W; x += 60) g.fillRect(x, y + 5 * JS.sin(x * 0.03 + t * 4) - 2, 26, 3);
		};
		h.hit = (p, t) => t >= ta && t < te && p.y + p.r - 2 > F9Lerp(kb, ky, BT(t)) + 5;
		return h;
	}

	// the moon's reflection breaking: a row of short bars (centres xs, hw half wide, 9 px half high) comes down from the horizon at ta, sp px a second
	public static Hz F9Glint(double[] xs, double hw, double ta, double sp)
	{
		var h = new Hz { kind = "F9Glint" }; const double HH = 9;
		h.update = (t, dt) => { if (t > ta && F9HZ + (t - ta) * sp > H + 20) h.dead = true; };
		h.draw = t =>
		{
			if (t < ta - 0.6) return; double y = F9HZ + Math.Max(0, t - ta) * sp;
			if (t < ta) { double k = F9k(t, ta - 0.6, ta); g.fillStyle = F9G(0.3 * k); foreach (var x in xs) g.fillRect(x - hw, F9HZ - 2, hw * 2, 4); return; }
			foreach (var x in xs) { g.fillStyle = F9P(0.9); g.fillRect(x - hw, y - HH, hw * 2, HH * 2); g.fillStyle = "rgba(255,230,240,0.7)"; g.fillRect(x - hw, y - HH, hw * 2, 3); }
		};
		h.hit = (p, t) =>
		{
			if (t < ta) return false; double y = F9HZ + (t - ta) * sp; if (Math.Abs(p.y - y) > HH + p.r) return false;
			foreach (var x in xs) if (F9Rect(p, x - hw, y - HH, hw * 2, HH * 2)) return true;
			return false;
		};
		return h;
	}

	// ---------- part 3: the coffin ----------
	// the coffin is the place: outside it is pink. Its turning (degrees at beats) and the polygon (around the middle of the screen)
	static double[] F9CofB = { 0 }, F9CofA = { 0 };
	static readonly double[] F9CX = { -575, -330, 575, 575, -330, -575 }, F9CY = { -150, -300, -215, 215, 300, 150 };
	static double F9CofAng(double bt) => F9Lerp(F9CofB, F9CofA, bt) * Math.PI / 180;
	static (double x, double y) F9ToScreen(double lx, double ly, double an) => (W / 2 + lx * JS.cos(an) - ly * JS.sin(an), H / 2 + lx * JS.sin(an) + ly * JS.cos(an));
	static (double x, double y) F9ToLocal(double x, double y, double an) { double dx = x - W / 2, dy = y - H / 2; return (dx * JS.cos(-an) - dy * JS.sin(-an), dx * JS.sin(-an) + dy * JS.cos(-an)); }
	public static Hz F9Coffin(double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9Coffin" }; int n = F9CX.Length;
		h.update = (t, dt) => { if (t > te + 0.8) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw, tw + 0.8), 1 - F9k(t, te, te + 0.8)); if (a <= 0) return; double an = F9CofAng(BT(t)); bool act = t >= ta && t < te;
			g.save(); g.globalAlpha = a;
			// outside: the earth, pink when it kills
			g.fillStyle = act ? F9P(0.6) : F9G(0.08); g.beginPath(); g.rect(0, 0, W, H);
			var p0 = F9ToScreen(F9CX[0], F9CY[0], an); g.moveTo(p0.x, p0.y); for (int i = n - 1; i >= 1; i--) { var q = F9ToScreen(F9CX[i], F9CY[i], an); g.lineTo(q.x, q.y); } g.closePath(); g.fill();
			// inside: F9CoffinWood (a place, under the skull)
			g.strokeStyle = act ? PINK : F9G(0.6); g.lineWidth = act ? 12 : 3; g.beginPath(); for (int i = 0; i < n; i++) { var q = F9ToScreen(F9CX[i], F9CY[i], an); if (i == 0) g.moveTo(q.x, q.y); else g.lineTo(q.x, q.y); } g.closePath(); g.stroke();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; var l = F9ToLocal(p.x, p.y, F9CofAng(BT(t)));
			bool inside = false; double md = 1e9;
			for (int i = 0, j = n - 1; i < n; j = i++)
			{
				if ((F9CY[i] > l.y) != (F9CY[j] > l.y) && l.x < (F9CX[j] - F9CX[i]) * (l.y - F9CY[i]) / (F9CY[j] - F9CY[i]) + F9CX[i]) inside = !inside;
				md = Math.Min(md, F9Seg(l.x, l.y, F9CX[i], F9CY[i], F9CX[j], F9CY[j]));
			}
			return !inside || md < p.r - 2 + 4;
		};
		return h;
	}

	// the coffin's inside (harmless, a place): dark wood, turning with the coffin
	public static Hz F9CoffinWood(double tw, double te)
	{
		var h = new Hz { kind = "F9CoffinWood" }; int n = F9CX.Length;
		h.update = (t, dt) => { if (t > te + 0.8) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(F9k(t, tw, tw + 0.8), 1 - F9k(t, te, te + 0.8)); if (a <= 0) return; double an = F9CofAng(BT(t));
			g.save(); g.globalAlpha = a;
			g.fillStyle = "rgba(34,22,16,0.92)"; g.beginPath(); for (int i = 0; i < n; i++) { var q = F9ToScreen(F9CX[i], F9CY[i], an); if (i == 0) g.moveTo(q.x, q.y); else g.lineTo(q.x, q.y); } g.closePath(); g.fill();
			g.strokeStyle = "rgba(70,46,30,0.6)"; g.lineWidth = 2; g.beginPath();
			for (double ly = -260; ly <= 260; ly += 40) { var l0 = F9ToScreen(-300, ly, an); var l1 = F9ToScreen(540, ly * 0.72, an); g.moveTo(l0.x, l0.y); g.lineTo(l1.x, l1.y); }
			g.stroke();
			g.restore();
		};
		return h;
	}

	// a nail driven through the coffin's wall: from the wall point (lx, ly, the coffin's own frame) in toward ang (degrees), len long.
	// It goes in a third at each of hb (beats), stays till te. Grey path from tw
	public static Hz F9Nail(double lx, double ly, double ang, double len, double[] hb, double tw, double te, double nw)
	{
		var h = new Hz { kind = "F9Nail" }; double ca = JS.cos(ang * Math.PI / 180), sa = JS.sin(ang * Math.PI / 180); int n = hb.Length;
		double depth(double bt) { double d = 0; for (int i = 0; i < n; i++) { if (bt < hb[i]) break; d = len * (i + Math.Min(1, (bt - hb[i]) / 0.12)) / n; } return d; }
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), an = F9CofAng(bt), d = depth(bt), f = 1 - F9k(t, te, te + 0.4); var w0 = F9ToScreen(lx, ly, an); var w1 = F9ToScreen(lx + ca * len, ly + sa * len, an);
			if (d < len) F9Dash(w0.x, w0.y, w1.x, w1.y, F9G(0.6 * f));
			if (d <= 0) return; var tip = F9ToScreen(lx + ca * d, ly + sa * d, an);
			g.strokeStyle = bt < hb[0] + 0.1 ? PINKW : F9P(f); g.lineWidth = nw; g.beginPath(); g.moveTo(w0.x, w0.y); g.lineTo(tip.x, tip.y); g.stroke();
			g.fillStyle = F9P(f); circle(w0.x, w0.y, nw * 1.3);
		};
		h.hit = (p, t) =>
		{
			if (t >= te) return false; double bt = BT(t), d = depth(bt); if (d <= 0) return false; double an = F9CofAng(bt);
			var w0 = F9ToScreen(lx, ly, an); var tip = F9ToScreen(lx + ca * d, ly + sa * d, an); return F9Seg(p.x, p.y, w0.x, w0.y, tip.x, tip.y) < nw / 2 + p.r - 2;
		};
		return h;
	}

	// the lid comes down: pink wood from the top of the screen down to y (ky at beats kb). Grey line from tw
	public static Hz F9Lid(double[] kb, double[] ky, double tw, double ta, double te)
	{
		var h = new Hz { kind = "F9Lid" };
		h.update = (t, dt) => { if (t > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double bt = BT(t), y = F9Lerp(kb, ky, bt), f = 1 - F9k(t, te, te + 0.5);
			if (t < ta) { F9Dash(0, F9Lerp(kb, ky, BT(ta)), W, F9Lerp(kb, ky, BT(ta)), F9G(0.6)); return; }
			if (t >= te) y = y * f;
			g.fillStyle = F9P(0.55 * f); g.fillRect(0, 0, W, y); g.strokeStyle = $"rgba(120,10,45,{str(0.6 * f)})"; g.lineWidth = 2; g.beginPath(); for (double yy = y - 30; yy > 0; yy -= 30) { g.moveTo(0, yy); g.lineTo(W, yy); } g.stroke();
			g.fillStyle = F9P(0.9 * f); g.fillRect(0, y - 12, W, 12);
		};
		h.hit = (p, t) => t >= ta && t < te && p.y - p.r + 2 < F9Lerp(kb, ky, BT(t));
		return h;
	}

	// a lump of earth: falls from the top at x and lands at y at ta (falling fall s), then lies there till te. R big. Grey from tw
	public static Hz F9Dirt(double x, double y, double R, double tw, double ta, double fall, double te)
	{
		var h = new Hz { kind = "F9Dirt" }; double t0 = ta - fall;
		double cy(double t) => t >= ta ? y : -R + (y + R) * JS.pow(F9k(t, t0, ta), 2);
		h.update = (t, dt) => { if (t > te + 0.4) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return; double f = 1 - F9k(t, te, te + 0.4);
			if (t < ta) { double k = F9k(t, tw, ta); g.strokeStyle = F9G(0.3 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); g.arc(x, y, R, 0, TAU); g.stroke(); g.setLineDash(new double[0]); g.fillStyle = F9G(0.05 + 0.08 * k); g.fillRect(x - R, 0, R * 2, y); }
			if (t < t0) return; double yy = cy(t);
			g.fillStyle = t >= ta && t < ta + 0.05 ? PINKW : F9P(0.92 * f); g.beginPath();
			for (int i = 0; i < 10; i++) { double a = i * TAU / 10, rr = R * (0.86 + 0.14 * F9h(i + (int)x)); if (i == 0) g.moveTo(x + JS.cos(a) * rr, yy + JS.sin(a) * rr); else g.lineTo(x + JS.cos(a) * rr, yy + JS.sin(a) * rr); }
			g.closePath(); g.fill(); g.fillStyle = $"rgba(80,0,26,{str(0.4 * f)})"; circle(x - R * 0.3, yy - R * 0.2, R * 0.18); circle(x + R * 0.25, yy + R * 0.2, R * 0.14);
		};
		h.hit = (p, t) => t >= t0 && t < te && hypot(p.x - x, p.y - cy(t)) < R * 0.86 + p.r - 2;
		return h;
	}

	// the skull over the coffin (harmless): at (x, y) from t0 to t1, cracked from tc to tb, then breaks
	public static Hz F9Skull(double x, double y, double r, double t0, double tc, double tb)
	{
		var h = new Hz { kind = "F9Skull" }; bool br = false;
		h.update = (t, dt) => { if (!br && t >= tb) { br = true; burst(x, y, "#f1e6c8", 40, 700); burst(x, y, F9Y(1), 24, 500); } if (t > tb + 2) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return;
			if (t < tb) { double a = F9k(t, t0, t0 + 0.8); F9SkullDraw(x, y + 60 * (1 - a), r, $"rgba(236,226,200,{str(0.85 * a)})", F9k(t, tc, tb)); return; }
			double u = t - tb; g.fillStyle = $"rgba(236,226,200,{str(Math.Max(0, 0.9 - u))})";
			for (int i = 0; i < 12; i++) { double a = i * TAU / 12 + 0.3, d = 40 + u * 480; g.save(); g.translate(x + JS.cos(a) * d, y + JS.sin(a) * d); g.rotate(u * (2 + i)); g.fillRect(-16, -10, 32, 20); g.restore(); }
		};
		return h;
	}

	// the skull's eyes: two beams from (lx, ly) and (rx, ry) turn from a0 to a1 (degrees, left and right) between ta and ta + sw, pink,
	// and stay hold s. Grey start, end and the turn between from tw
	public static Hz F9Eyes(double lx, double ly, double rx, double ry, double la0, double la1, double ra0, double ra1, double bw, double tw, double ta, double sw, double hold)
	{
		var h = new Hz { kind = "F9Eyes" }; const double LEN = 1500; double te = ta + sw + hold;
		double ang(double a0, double a1, double t) { double k = F9k(t, ta, ta + sw); k = k * k * (3 - 2 * k); return (a0 + (a1 - a0) * k) * Math.PI / 180; }
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			foreach (var e in new[] { (lx, ly, la0, la1), (rx, ry, ra0, ra1) })
			{
				if (t < ta)
				{
					double k = F9k(t, tw, ta), A0 = e.Item3 * Math.PI / 180, A1 = e.Item4 * Math.PI / 180;
					F9Dash(e.Item1, e.Item2, e.Item1 + JS.cos(A0) * LEN, e.Item2 + JS.sin(A0) * LEN, F9G(0.3 + 0.4 * k)); F9Dash(e.Item1, e.Item2, e.Item1 + JS.cos(A1) * LEN, e.Item2 + JS.sin(A1) * LEN, F9G(0.3 + 0.5 * k), 3);
					g.strokeStyle = F9G(0.4 * k); g.lineWidth = 2; g.beginPath(); g.arc(e.Item1, e.Item2, 120, Math.Min(A0, A1), Math.Max(A0, A1)); g.stroke();
					g.fillStyle = F9P(0.3 + 0.6 * k); circle(e.Item1, e.Item2, 10 + 6 * k); continue;
				}
				double an = ang(e.Item3, e.Item4, t), f = 1 - F9k(t, te, te + 0.3), nx = -JS.sin(an) * bw, ny = JS.cos(an) * bw;
				g.fillStyle = t < ta + 0.05 ? PINKW : F9P(0.88 * f); g.beginPath(); g.moveTo(e.Item1 + nx * 0.4, e.Item2 + ny * 0.4); g.lineTo(e.Item1 + JS.cos(an) * LEN + nx, e.Item2 + JS.sin(an) * LEN + ny); g.lineTo(e.Item1 + JS.cos(an) * LEN - nx, e.Item2 + JS.sin(an) * LEN - ny); g.lineTo(e.Item1 - nx * 0.4, e.Item2 - ny * 0.4); g.closePath(); g.fill();
				g.fillStyle = PINK; circle(e.Item1, e.Item2, 16);
			}
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false;
			foreach (var e in new[] { (lx, ly, la0, la1), (rx, ry, ra0, ra1) })
			{
				double an = ang(e.Item3, e.Item4, t), d = hypot(p.x - e.Item1, p.y - e.Item2), w = bw * (0.4 + 0.6 * Math.Min(1, d / LEN));
				if (F9Seg(p.x, p.y, e.Item1, e.Item2, e.Item1 + JS.cos(an) * LEN, e.Item2 + JS.sin(an) * LEN) < w + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// a crack running out of the skull: a broken line through the points (xs, ys), w thick; its path is drawn from tw, it splits open at ta for hold s
	public static Hz F9Crack(double[] xs, double[] ys, double w, double tw, double ta, double hold)
	{
		var h = new Hz { kind = "F9Crack" }; int n = xs.Length;
		h.update = (t, dt) => { if (t > ta + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			if (t < tw) return;
			if (t < ta)
			{
				double k = F9k(t, tw, ta), m = k * (n - 1); g.strokeStyle = F9G(0.35 + 0.5 * k); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.beginPath(); g.moveTo(xs[0], ys[0]);
				for (int i = 1; i < n; i++) { if (i <= m) g.lineTo(xs[i], ys[i]); else { double u = m - (i - 1); if (u > 0) g.lineTo(xs[i - 1] + (xs[i] - xs[i - 1]) * u, ys[i - 1] + (ys[i] - ys[i - 1]) * u); break; } }
				g.stroke(); g.setLineDash(new double[0]); return;
			}
			double f = t < ta + hold ? 1 : 1 - (t - ta - hold) / 0.3;
			g.strokeStyle = t < ta + 0.05 ? PINKW : F9P(0.95 * f); g.lineWidth = w; g.lineJoin = "round"; g.beginPath(); g.moveTo(xs[0], ys[0]); for (int i = 1; i < n; i++) g.lineTo(xs[i], ys[i]); g.stroke();
			g.strokeStyle = $"rgba(40,0,12,{str(0.6 * f)})"; g.lineWidth = Math.Max(1, w * 0.25); g.stroke(); g.lineJoin = "miter";
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= ta + hold) return false;
			for (int i = 1; i < n; i++) if (F9Seg(p.x, p.y, xs[i - 1], ys[i - 1], xs[i], ys[i]) < w / 2 + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// the end: the coffin opens and warm light comes in (harmless)
	public static Hz F9Open(double t0)
	{
		var h = new Hz { kind = "F9Open" };
		h.update = (t, dt) => { if (t > t0 + 4) h.dead = true; };
		h.draw = t =>
		{
			if (t < t0) return; double k = F9k(t, t0, t0 + 1.2), f = 1 - F9k(t, t0 + 2.5, t0 + 4);
			var gr = g.createRadialGradient(W / 2, H / 2, 10, W / 2, H / 2, 200 + 700 * k); gr.addColorStop(0, F9Y(0.85 * f)); gr.addColorStop(0.5, F9Y(0.35 * f)); gr.addColorStop(1, F9Y(0)); g.fillStyle = gr; g.fillRect(0, 0, W, H);
		};
		return h;
	}

	// ---------- the kinds (the events in data/dodge_F9.json) ----------
	// Only pink hurts, so nothing may hide an attack: the places (sky, sea, the tower's wall, the roads, the coffin, the skull) are drawn
	// first, in the order they came, under every attack, whenever they came (the coffin: only its wood inside). The list keeps its order (ids, Easy cuts): the first
	// thing in it draws the places before itself, the places draw nothing on their own turn.
	// The street lamps go over everything (the tower's lit windows come while they still burn): the last thing in the list draws them after itself.
	static readonly List<(Hz h, Action<double> d)> F9Places = new(), F9Tops = new();
	static void F9Under(Hz h) { F9Places.Add((h, h.draw)); h.draw = t => { F9First(h, t); F9Last(h, t); }; hazards.Add(h); }
	static void F9Add(Hz h) { var d = h.draw; h.draw = t => { F9First(h, t); d(t); F9Last(h, t); }; hazards.Add(h); }
	static void F9Top(Hz h) { F9Tops.Add((h, h.draw)); h.draw = t => { F9First(h, t); F9Last(h, t); }; hazards.Add(h); }
	static void F9First(Hz h, double t)
	{
		if (hazards.Count == 0 || hazards[0] != h) return;
		F9Places.RemoveAll(q => !hazards.Contains(q.h));
		foreach (var q in F9Places) q.d(t);
	}
	static void F9Last(Hz h, double t)
	{
		if (hazards.Count == 0 || hazards[hazards.Count - 1] != h) return;
		F9Tops.RemoveAll(q => !hazards.Contains(q.h));
		foreach (var q in F9Tops) q.d(t);
	}
	// times are beats; warnings (wn) are Normal's, Easy stretches them here
	static double F9wn(DodgeEv e) => e.num("wn") * (easy ? ezV("ezwn", EZ_WN) : 1);
	static double F9ez(double v, double ez) => easy ? v * ez : v;
	static bool F9skip(DodgeEv e) => easy && e.flag("ezskip");

	static readonly int _kf9stage = DodgeKind("F9stage", e => () => F9Under(F9Stage(e.i("part"), B(e.b), B(e.num("to")))));
	static readonly int _kf9fx = DodgeKind("F9fx", e => () => F9Add(F9Fx(B(e.b), e.num("v", 0.3), e.num("s", 8), Math.Min(2, e.num("dur", 1.2)))));
	static readonly int _kf9lamp = DodgeKind("F9lamp", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return;
		F9Top(F9Lamp(e.num("x"), e.nums("hb").Select(B).ToArray(), wn * SPB, F9ez(e.num("on"), 0.8) * SPB, B(e.num("t0")), B(e.num("to")), F9ez(1, 0.8)));
	}; });
	static readonly int _kf9facade = DodgeKind("F9facade", e => { double wn = F9wn(e); return () =>
	{
		F9Add(F9Facade(e.nums("cells"), e.nums("tb"), F9ez(e.num("on"), 0.8), wn, e.num("b0"), F9ez(e.num("v"), 0.8), B(e.b), B(e.num("to"))));
		F9Under(F9FacadeWall(e.num("b0"), F9ez(e.num("v"), 0.8), B(e.b), B(e.num("to"))));   // after it: the attack keeps its id
	}; });
	static readonly int _kf9row = DodgeKind("F9row", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return;
		F9Add(F9Row(e.num("y"), e.num("ox"), B(e.num("hit") - wn), B(e.num("hit")), F9ez(e.num("on"), 0.8) * SPB));
	}; });
	static readonly int _kf9roads = DodgeKind("F9roads", e => () => F9Under(F9Roads(B(e.b), B(e.num("to")))));
	static readonly int _kf9car = DodgeKind("F9car", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return; var hb = easy ? new double[0] : e.nums("hb").Select(B).ToArray();
		F9Add(F9Car(e.i("ln"), e.i("dir", 1), B(e.num("hit") - wn), B(e.num("hit")), F9ez(e.num("sp"), 0.8), hb, e.num("hl", 0.5) * SPB, e.num("len", 230), F9ez(e.num("cw", 120), 0.85)));
	}; });
	static readonly int _kf9shadows = DodgeKind("F9shadows", e => { double wn = F9wn(e); return () =>
	{
		var kb = e.nums("kb"); var ka = e.nums("ka");
		var gp = e.nums("gaps"); if (easy) gp = gp.Select((v, i) => i % 2 == 0 ? v - 15 : v + 15).ToArray();
		F9Add(F9Shadows(e.nums("xs"), e.nums("th"), kb, ka, F9ez(e.num("bw"), 0.7), gp, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
		F9Under(F9ShadowTowers(e.nums("xs"), e.nums("th"), B(e.num("hit") - wn), B(e.num("to"))));   // after it: the attack keeps its id
	}; });
	static readonly int _kf9topple = DodgeKind("F9topple", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return; double hit = e.num("hit"), fl = e.num("fall", 0.6) * (easy ? 1.3 : 1);
		F9Add(F9Topple(e.num("x"), e.i("dir", 1), F9ez(e.num("len"), 0.85), F9ez(e.num("w"), 0.8), B(hit - fl - wn), B(hit - fl), B(hit), F9ez(e.num("hold", 0.5), 0.7) * SPB));
		F9Under(F9ToppleStand(e.num("x"), e.i("dir", 1), F9ez(e.num("len"), 0.85), F9ez(e.num("w"), 0.8), B(hit - fl - wn), B(hit - fl)));   // after it: the attack keeps its id
	}; });
	static readonly int _kf9wave = DodgeKind("F9wave", e => () =>
	{
		if (F9skip(e)) return;
		F9Add(F9Wave(B(e.num("hit")), e.num("dur") * SPB * (easy ? 1.2 : 1), e.num("gx"), F9ez(e.num("gw"), 1.35), e.flag("up")));
	});
	static readonly int _kf9ripple = DodgeKind("F9ripple", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return;
		F9Add(F9Ripple(e.num("x"), e.num("y"), !e.flag("nodrop"), B(e.num("hit") - wn), B(e.num("hit")), F9ez(e.num("sp"), 0.8) / SPB, F9ez(e.num("th", 12), 0.75),
			e.num("ga") * Math.PI / 180, F9ez(e.num("gw"), 1.35) * Math.PI / 180, e.num("rmax", 900)));
	}; });
	static readonly int _kf9light = DodgeKind("F9light", e => { double wn = F9wn(e); return () =>
	{
		var kb = e.nums("kb"); var ka = e.nums("ka");
		if (easy) { double a0 = ka[0]; ka = ka.Select(a => a0 + (a - a0) * 0.8).ToArray(); }
		F9Add(F9Light(e.num("x"), e.num("y"), e.i("n", 2), F9ez(e.num("hw"), 0.75) * Math.PI / 180, kb, ka, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
		F9Under(F9LightHouse(e.num("x"), e.num("y"), B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));   // after it: the attack keeps its id
	}; });
	static readonly int _kf9tide = DodgeKind("F9tide", e => { double wn = F9wn(e); return () =>
	{
		var ky = e.nums("ky"); if (easy) ky = ky.Select(y => H - (H - y) * 0.75).ToArray();
		F9Add(F9Tide(e.nums("kb"), ky, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _kf9glint = DodgeKind("F9glint", e => () =>
	{
		if (F9skip(e)) return;
		F9Add(F9Glint(e.nums("xs"), F9ez(e.num("hw"), 0.8), B(e.num("hit")), F9ez(e.num("sp"), 0.8) / SPB));
	});
	static readonly int _kf9coffin = DodgeKind("F9coffin", e =>
	{
		F9CofB = e.nums("kb"); F9CofA = e.nums("ka"); if (easy) F9CofA = F9CofA.Select(a => a * 0.7).ToArray();
		double wn = F9wn(e); return () =>
		{
			F9Add(F9Coffin(B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
			F9Under(F9CoffinWood(B(e.num("hit") - wn), B(e.num("to"))));   // after it: the attack keeps its id
		};
	});
	static readonly int _kf9nail = DodgeKind("F9nail", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return; var hb = e.nums("hb");
		F9Add(F9Nail(e.num("lx"), e.num("ly"), e.num("ang"), F9ez(e.num("len"), 0.8), hb, B(hb[0] - wn), B(e.num("to")), F9ez(e.num("nw", 14), 0.8)));
	}; });
	static readonly int _kf9lid = DodgeKind("F9lid", e => { double wn = F9wn(e); return () =>
	{
		var ky = e.nums("ky"); if (easy) ky = ky.Select(y => y * 0.8).ToArray();
		F9Add(F9Lid(e.nums("kb"), ky, B(e.num("hit") - wn), B(e.num("hit")), B(e.num("to"))));
	}; });
	static readonly int _kf9dirt = DodgeKind("F9dirt", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return; double R = F9ez(e.num("R"), 0.8), y = e.num("y") + (easy ? e.num("R") - R : 0);
		F9Add(F9Dirt(e.num("x"), y, R, B(e.num("hit") - wn), B(e.num("hit")), e.num("fall", 0.75) * SPB, B(e.num("to"))));
	}; });
	static readonly int _kf9skull = DodgeKind("F9skull", e => () => F9Under(F9Skull(e.num("x"), e.num("y"), e.num("r"), B(e.b), B(e.num("tc")), B(e.num("tb")))));
	static readonly int _kf9eyes = DodgeKind("F9eyes", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return; double sl = easy ? 0.8 : 1;
		F9Add(F9Eyes(e.num("lx"), e.num("ly"), e.num("rx"), e.num("ry"), e.num("la0"), e.num("la0") + (e.num("la1") - e.num("la0")) * sl, e.num("ra0"), e.num("ra0") + (e.num("ra1") - e.num("ra0")) * sl,
			F9ez(e.num("bw"), 0.7), B(e.num("hit") - wn), B(e.num("hit")), e.num("sw") * SPB, F9ez(e.num("hold", 0.3), 0.7) * SPB));
	}; });
	static readonly int _kf9crack = DodgeKind("F9crack", e => { double wn = F9wn(e); return () =>
	{
		if (F9skip(e)) return;
		F9Add(F9Crack(e.nums("xs"), e.nums("ys"), F9ez(e.num("w"), 0.75), B(e.num("hit") - wn), B(e.num("hit")), F9ez(e.num("hold", 0.5), 0.7) * SPB));
	}; });
	static readonly int _kf9open = DodgeKind("F9open", e => () => F9Add(F9Open(B(e.b))));
}
