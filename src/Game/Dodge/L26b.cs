using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// "لا دندنت" (main level 26), the arrows, the banner, the horses and the sandstorm.
public static partial class G
{
	// ---------- the arrows ----------
	// A volley lands on the points (xs[i], ys[i]) at beat bf. Where each lands shows from bf - tl (a sand circle r wide and a cross);
	// the arrows fly in in sand (harmless) from bf - 0.6; on landing each point is a pink burst of r for hold beats, which then shrinks
	// away in 0.12 beat (it still hurts while it shows).
	public static Hz DDArrows(double[] xs, double[] ys, double bf, double tl, double r, double hold, int from = 1)
	{
		var h = new Hz { kind = "DDArrows" }; int n = xs.Length;
		double rad(double bt) => r * (1 - DDk(bt, bf + hold, bf + hold + 0.12));
		h.update = (t, dt) => { if (BT(t) > bf + hold + 0.2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			if (bt < bf)
			{
				double q = DDk(bt, bf - tl, bf);
				for (int i = 0; i < n; i++)
				{
					g.fillStyle = DDS(0.05 + 0.1 * q); circle(xs[i], ys[i], r);
					g.strokeStyle = DDS(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.beginPath(); g.arc(xs[i], ys[i], r, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
					g.beginPath(); g.moveTo(xs[i] - 10, ys[i] - 10); g.lineTo(xs[i] + 10, ys[i] + 10); g.moveTo(xs[i] + 10, ys[i] - 10); g.lineTo(xs[i] - 10, ys[i] + 10); g.stroke();
				}
				// the flight (sand, harmless): down a curve from high up behind
				double u = DDk(bt, bf - 0.6, bf);
				if (u > 0)
				{
					g.strokeStyle = DDS(0.8); g.lineWidth = 3; g.lineCap = "round";
					for (int i = 0; i < n; i++)
					{
						double sx = xs[i] - from * 260, sy = ys[i] - 520, x = sx + (xs[i] - sx) * u, y = sy + (ys[i] - sy) * u * u, vx = xs[i] - sx, vy = 2 * (ys[i] - sy) * u, l = Math.Max(1, hypot(vx, vy));
						g.beginPath(); g.moveTo(x, y); g.lineTo(x - vx / l * 34, y - vy / l * 34); g.stroke();
					}
				}
			}
			else
			{
				double rr = rad(bt); if (rr <= 0.5) { g.restore(); return; }
				for (int i = 0; i < n; i++)
				{
					g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; circle(xs[i], ys[i], rr); g.shadowBlur = 0;
					// the arrows stuck in the middle (dark lines inside the pink)
					g.strokeStyle = "rgba(110,8,44,0.6)"; g.lineWidth = 3;
					for (int k = -1; k <= 1; k++) { double ox = k * rr * 0.3; g.beginPath(); g.moveTo(xs[i] + ox, ys[i] + rr * 0.2); g.lineTo(xs[i] + ox - from * rr * 0.4, ys[i] - rr * 0.55); g.stroke(); }
					double fl = 1 - DDk(bt, bf, bf + 0.15); if (fl > 0) { g.globalAlpha = fl; g.fillStyle = PINKW; circle(xs[i], ys[i], rr); g.globalAlpha = 1; }
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < bf || bt >= bf + hold + 0.12) return false; double rr = rad(bt);
			for (int i = 0; i < n; i++) if (hypot(p.x - xs[i], p.y - ys[i]) < rr + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// ---------- the banner ----------
	// A banner (a pole with a flag at its end) turns about (cx, cy), with arms poles (1 or 2, opposite). It stands at a0 and turns by step
	// radians in 0.35 beat on each beat of turns. It hurts from b0 to b1 (pole and flag, all pink); from b0 - tl it shows in sand.
	public static Hz DDBanner(double cx, double cy, double len, int arms, double a0, double step, double[] turns, double b0, double b1, double tl)
	{
		var h = new Hz { kind = "DDBanner" };
		double ang(double bt) { double a = a0; foreach (var k in turns) { if (bt <= k) break; a += step * DDEase((bt - k) / 0.35); } return a; }
		const double FL = 150, FW = 74, PW = 14;
		void shape(double a, Action<double, double, double, double, double, double> fn)
		{
			for (int i = 0; i < arms; i++)
			{
				double q = a + i * Math.PI, dx = JS.cos(q), dy = JS.sin(q), side = step >= 0 ? -1 : 1;
				// pole end, and the flag: a rectangle on the side it trails, from len - FL to len along the pole
				fn(dx, dy, side * -dy, side * dx, cx + dx * len, cy + dy * len);
			}
		}
		h.update = (t, dt) => { if (BT(t) > b1 + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - tl || bt > b1 + 0.2) return;
			double a = ang(bt), al = bt < b0 ? 0.25 + 0.5 * DDk(bt, b0 - tl, b0) : 1 - DDk(bt, b1, b1 + 0.2);
			g.save();
			shape(a, (dx, dy, nx, ny, tx, ty) =>
			{
				double fx = tx - dx * FL, fy = ty - dy * FL;
				if (bt < b0)
				{
					DDDash(cx, cy, tx, ty, DDS(al), 3);
					g.strokeStyle = DDS(al); g.lineWidth = 2; g.setLineDash(new double[] { 8, 7 }); g.beginPath(); g.moveTo(fx, fy); g.lineTo(fx + nx * FW, fy + ny * FW); g.lineTo(tx + nx * FW, ty + ny * FW); g.lineTo(tx, ty); g.stroke(); g.setLineDash(new double[0]);
					return;
				}
				g.globalAlpha = al; g.shadowColor = PINK; g.shadowBlur = 14;
				g.strokeStyle = PINK; g.lineWidth = PW; g.lineCap = "butt"; g.beginPath(); g.moveTo(cx, cy); g.lineTo(tx, ty); g.stroke();
				g.fillStyle = PINK; g.beginPath(); g.moveTo(fx, fy); g.lineTo(fx + nx * FW, fy + ny * FW); g.lineTo(tx + nx * FW, ty + ny * FW); g.lineTo(tx, ty); g.closePath(); g.fill();
				g.shadowBlur = 0;
				// the flag's marks: a darker sword and stripe inside the pink (the flag of the raid)
				g.strokeStyle = "rgba(120,10,50,0.55)"; g.lineWidth = 4; double mx = fx + nx * FW * 0.5, my = fy + ny * FW * 0.5;
				g.beginPath(); g.moveTo(mx + dx * 20, my + dy * 20); g.lineTo(mx + dx * (FL - 20), my + dy * (FL - 20)); g.stroke();
				g.lineWidth = 3; g.beginPath(); g.moveTo(mx + dx * 40 - nx * 14, my + dy * 40 - ny * 14); g.lineTo(mx + dx * 40 + nx * 14, my + dy * 40 + ny * 14); g.stroke();
			});
			// the hub (gold, harmless; small, inside the pole's ends)
			if (bt >= b0) { g.globalAlpha = al; g.fillStyle = DDG(1); circle(cx, cy, 9); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1 + 0.1) return false; bool hit = false;
			shape(ang(bt), (dx, dy, nx, ny, tx, ty) =>
			{
				if (hit) return;
				if (DDSeg(p.x, p.y, cx, cy, tx, ty) < PW / 2 + p.r - 2) { hit = true; return; }
				// the flag: in the pole's frame (u along, v across)
				double rx = p.x - (tx - dx * FL), ry = p.y - (ty - dy * FL), u = rx * dx + ry * dy, v = rx * nx + ry * ny, m = p.r - 2;
				if (u > -m && u < FL + m && v > -m && v < FW + m) hit = true;
			});
			return hit;
		};
		return h;
	}

	// ---------- the horses ----------
	// A horse gallops along the lane y, dir 1 = to the right, entering the screen at beat bf at sp px/s. Its lane shows from bf - tl
	// (a sand band with arrows). All of the horse is pink and all of it hurts (body, neck, head, legs, tail).
	static void DDHorseParts(double x, double y, double s, double ph, Action<double, double, double, double, double> cap, Action<double, double, double, double> body)
	{
		body(x, y, 62, 25);
		cap(x + s * 40, y - 8, x + s * 70, y - 40, 12);       // neck
		cap(x + s * 70, y - 42, x + s * 96, y - 36, 11);      // head
		cap(x - s * 56, y - 8, x - s * 88, y + 14, 4);        // tail
		for (int i = 0; i < 4; i++)
		{
			double lx = x + s * (i < 2 ? 40 - i * 14 : -34 - (i - 2) * 14), sw = JS.sin(ph + (i % 2 == 0 ? 0 : Math.PI) + (i < 2 ? 0 : 1.3)) * 0.6;
			cap(lx, y + 14, lx + s * JS.sin(sw) * 36, y + 14 + JS.cos(sw) * 36, 4);
		}
	}
	public static Hz DDHorse(double y, int dir, double bf, double sp, double tl)
	{
		var h = new Hz { kind = "DDHorse" }; double tf = B(bf), x0 = dir > 0 ? -110 : W + 110;
		double xAt(double t) => x0 + dir * sp * (t - tf);
		h.update = (t, dt) => { if (t > tf && (dir > 0 ? xAt(t) > W + 130 : xAt(t) < -130)) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bf - tl) return;
			g.save();
			double lane = (1 - DDk(t, tf + 0.25, tf + 0.6)) * DDk(bt, bf - tl, bf);
			if (lane > 0)
			{
				g.fillStyle = DDS(0.05 + 0.08 * lane); g.fillRect(0, y - 55, W, 110);
				g.strokeStyle = DDS(0.25 + 0.5 * lane); g.lineWidth = 3;
				for (int i = 0; i < 6; i++) { double ax = dir > 0 ? 70 + i * 40 : W - 70 - i * 40; g.beginPath(); g.moveTo(ax - dir * 10, y - 14); g.lineTo(ax + dir * 4, y); g.lineTo(ax - dir * 10, y + 14); g.stroke(); }
			}
			if (t >= tf)
			{
				double x = xAt(t), ph = (t - tf) * sp / 38;
				// dust behind (sand, harmless)
				g.fillStyle = DDS(0.18); for (int i = 0; i < 4; i++) { double dx = x - dir * (90 + i * 26), r = 10 + i * 5; circle(dx, y + 44 - i * 3, r); }
				g.shadowColor = PINK; g.shadowBlur = 12; g.fillStyle = PINK; g.strokeStyle = PINK; g.lineCap = "round";
				DDHorseParts(x, y, dir, ph, (ax, ay, bx, by, r) => { g.lineWidth = 2 * r; g.beginPath(); g.moveTo(ax, ay); g.lineTo(bx, by); g.stroke(); },
					(cx, cy, rx, ry) => { g.beginPath(); g.ellipse(cx, cy, rx, ry, 0, 0, TAU); g.fill(); });
				g.shadowBlur = 0;
				// the mane (darker pink lines inside the neck)
				g.strokeStyle = "rgba(120,10,50,0.5)"; g.lineWidth = 3; g.beginPath(); g.moveTo(x + dir * 44, y - 14); g.lineTo(x + dir * 66, y - 40); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tf) return false; double x = xAt(t); if (Math.Abs(p.x - x) > 140 || Math.Abs(p.y - y) > 80) return false;
			bool hit = false; double m = p.r - 2;
			DDHorseParts(x, y, dir, (t - tf) * sp / 38, (ax, ay, bx, by, r) => { if (!hit && DDSeg(p.x, p.y, ax, ay, bx, by) < r + m) hit = true; },
				(cx, cy, rx, ry) => { double u = (p.x - cx) / (rx + m), v = (p.y - cy) / (ry + m); if (u * u + v * v < 1) hit = true; });
			return hit;
		};
		return h;
	}

	// ---------- the sandstorm ----------
	// Walls of pink sand above and below a way gw wide come in from the right at vs px/s: the sand that comes in at beat ab has its way's
	// middle at yAt(ab) (smooth between the keys kb/ky). Sand comes in from b0 to b1. From b0 - 1 the right edge shimmers in sand.
	public static Hz DDStorm(double b0, double b1, double vs, double gw, double[] kb, double[] ky)
	{
		var h = new Hz { kind = "DDStorm" };
		double yAt(double ab)
		{
			if (ab <= kb[0]) return ky[0]; int n = kb.Length; if (ab >= kb[n - 1]) return ky[n - 1];
			int i = 0; while (i + 1 < n && kb[i + 1] <= ab) i++;
			return ky[i] + (ky[i + 1] - ky[i]) * DDEase((ab - kb[i]) / (kb[i + 1] - kb[i]));
		}
		double abAt(double x, double t) => BT(t - (W - x) / vs);
		h.update = (t, dt) => { if (abAt(0, t) > b1 + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1) return;
			g.save();
			if (bt < b0 + 0.5)
			{
				double q = DDk(bt, b0 - 1, b0) * (1 - DDk(bt, b0, b0 + 0.5)), yc = yAt(b0);
				g.fillStyle = DDS(0.15 * q); g.fillRect(W - 40, 0, 40, yc - gw / 2); g.fillRect(W - 40, yc + gw / 2, 40, H - yc - gw / 2);
				DDDash(W - 2, 0, W - 2, yc - gw / 2, DDS(0.7 * q), 4); DDDash(W - 2, yc + gw / 2, W - 2, H, DDS(0.7 * q), 4);
			}
			// the sand: columns 8 px wide from where the sand starts to where it ends
			double xs = W - vs * (t - B(b0)), xe = W - vs * (t - B(b1));
			double a = Math.Max(0, xe), z = Math.Min(W, xs);
			if (z > a)
			{
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK;
				g.beginPath(); g.moveTo(a, 0);
				for (double x = a; ; x += 8) { double xx = Math.Min(x, z); g.lineTo(xx, yAt(abAt(xx, t)) - gw / 2); if (xx >= z) break; }
				g.lineTo(z, 0); g.closePath(); g.fill();
				g.beginPath(); g.moveTo(a, H);
				for (double x = a; ; x += 8) { double xx = Math.Min(x, z); g.lineTo(xx, yAt(abAt(xx, t)) + gw / 2); if (xx >= z) break; }
				g.lineTo(z, H); g.closePath(); g.fill();
				g.shadowBlur = 0;
				// the wind's streaks inside the sand (darker pink, they run left faster than the sand)
				g.strokeStyle = "rgba(130,12,54,0.45)"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 26; i++)
				{
					double sy = (i * 53.7) % H, sx = W - ((i * 211.3 + t * (vs + 260)) % (W + 200)), yc = sx >= a && sx <= z ? yAt(abAt(sx, t)) : -1;
					if (yc < 0) continue; bool inSand = sy < yc - gw / 2 - 12 || sy > yc + gw / 2 + 12; if (!inSand) continue;
					double ex = Math.Max(a, sx - 60); g.moveTo(sx, sy); g.lineTo(ex, sy);
				}
				g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double ab = abAt(p.x, t); if (ab < b0 || ab > b1) return false; double yc = yAt(ab), m = p.r - 2;
			return p.y - m < yc - gw / 2 || p.y + m > yc + gw / 2;
		};
		return h;
	}

	// ---------- the end: the words of the song in gold (harmless) ----------
	public static Hz DDEnd(double b0, double b1)
	{
		var h = new Hz { kind = "DDEnd" };
		h.update = (t, dt) => { if (BT(t) > b1 + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0) return; double k = DDk(bt, b0, b0 + 0.4), a = Math.Min(k, 1 - DDk(bt, b1, b1 + 1.5));
			g.save(); g.globalAlpha = a; g.font = $"800 {str(92 - 16 * k)}px {FONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "rtl";
			g.shadowColor = "#e8b652"; g.shadowBlur = 28; g.fillStyle = "#f6dc96"; g.fillText("لا دندنت حنا لها", W / 2, H / 2 - 30); g.shadowBlur = 0; g.restore();
		};
		return h;
	}
}
