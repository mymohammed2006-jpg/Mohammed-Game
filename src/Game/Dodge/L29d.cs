using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Trouble, main level 29, the hype (1:05.6-1:18.1, beats 152-184): the concert. Every sound plays on the screen:
// the kicks lift the equalizer's bars from the floor, the snares drop them from the ceiling, the shouts are sound waves from the speakers
// (one side then the other), the light background beats make the lights, the small equalizer at the back and the crowd jump (harmless).
// It plays easy: the bars and the waves always leave the same wide lane, which winds slowly over the screen (the stage lights show it).
public static partial class G
{
	// the lane's middle in column k (0-15, 80 px each, a column's middle is at 40 + 80k) at beat bt, and its half height
	static double TBLane(double k, double bt) => H / 2 + (easy ? 100 : 140) * JS.sin(0.55 * k + (easy ? 0.4 : 0.75) * bt);
	static double TBLaneH => easy ? 160 : 105;
	static double TBLaneX(double x) => clamp((x - 40) / 80, 0, 15);

	// ---------- the concert's look (harmless, under everything): beats 150-186.5 ----------
	public static Hz TBShow(double[] kb, double[] sb, double[] vb)
	{
		var h = new Hz { kind = "TBShow", z = -1 };
		h.update = (t, dt) => { if (BT(t) > 187) h.dead = true; };
		// how strong a hit at one of the beats xs still sounds at bt (1 on it, 0 after len beats)
		double near(double[] xs, double bt, double len) { double v = 0; foreach (var x in xs) { double d = bt - x; if (d >= 0 && d < len) v = Math.Max(v, 1 - d / len); } return v; }
		h.draw = t =>
		{
			double bt = BT(t), a = TBk(bt, 150, 152.5) * (1 - TBk(bt, 184, 186.5)); if (a <= 0.01) return;
			double kick = near(kb, bt, 0.6), snare = near(sb, bt, 0.5), shout = near(vb, bt, 1.4), hat = 1 - (bt * 2 % 1);
			double charge = TBk(bt, 164, 168) * (1 - TBk(bt, 168, 168.4));
			g.save(); g.globalAlpha = a;
			// the hall goes dark and purple, it flashes a little on every kick
			g.fillStyle = $"rgba(18,8,30,{str(0.55 + 0.1 * kick)})"; g.fillRect(0, 0, W, H);
			// the small equalizer at the back dances on the light beats (the hi-hats, every half beat)
			int hb = (int)Math.Floor(bt * 2);
			for (int i = 0; i < 40; i++)
			{
				double hh = 30 + 150 * TBH(hb * 41 + i) * (0.55 + 0.45 * hat) + 60 * kick * TBH(i * 7.1);
				g.fillStyle = TBW(0.05 + 0.04 * hat); g.fillRect(i * 32 + 8, H * 0.62 - hh, 16, hh);
				g.fillStyle = TBW(0.03); g.fillRect(i * 32 + 8, H * 0.62 + 4, 16, hh * 0.35);
			}
			// the stage lights from the ceiling: they sweep with the song and light the lane (where the bars and the waves leave room)
			for (int i = 0; i < 6; i++)
			{
				double ox = 110 + i * 212, sw = TBRad(28 * JS.sin(bt * TAU / 8 + i * 1.3)), len = 900, wd = 60 + 40 * kick;
				double ex = ox + JS.sin(sw) * len, ey = JS.cos(sw) * len;
				var lg = g.createLinearGradient(ox, 0, ex, ey); lg.addColorStop(0, i % 2 == 0 ? TBY(0.16 + 0.12 * snare) : TBW(0.12 + 0.1 * snare)); lg.addColorStop(1, TBW(0));
				g.fillStyle = lg; g.beginPath(); g.moveTo(ox - 10, 0); g.lineTo(ox + 10, 0);
				g.lineTo(ex + JS.cos(sw) * wd, ey - JS.sin(sw) * wd); g.lineTo(ex - JS.cos(sw) * wd, ey + JS.sin(sw) * wd); g.closePath(); g.fill();
				g.fillStyle = i % 2 == 0 ? TBY(0.8) : TBW(0.8); circle(ox, 6, 9);
			}
			// the lane, lit softly from above (looks only)
			for (int k = 0; k < 16; k++)
			{
				double cx = 40 + 80 * k, cy = TBLane(k, bt), hh = TBLaneH;
				var cg = g.createLinearGradient(cx, cy - hh, cx, cy + hh); cg.addColorStop(0, TBW(0)); cg.addColorStop(0.5, TBW(0.07 + 0.04 * hat)); cg.addColorStop(1, TBW(0));
				g.fillStyle = cg; g.fillRect(cx - 40, cy - hh, 80, 2 * hh);
			}
			// the speakers at both sides: the cones jump on the kicks, they fill with light before the shouts (1:10.4) and on them
			for (int s = -1; s <= 1; s += 2)
			{
				double ex = s < 0 ? 0 : W, inx = ex - s * 46;
				g.fillStyle = "rgba(14,12,20,1)"; g.fillRect(s < 0 ? -10 : W - 82, 210, 92, 300);
				g.strokeStyle = TBW(0.5); g.lineWidth = 3; g.strokeRect(s < 0 ? -10 : W - 82, 210, 92, 300);
				double glow = Math.Max(charge, shout);
				if (glow > 0.01) { var sg = g.createRadialGradient(inx, 400, 10, inx, 400, 220); sg.addColorStop(0, TBY(0.35 * glow)); sg.addColorStop(1, TBY(0)); g.fillStyle = sg; g.fillRect(inx - 220, 180, 440, 440); }
				foreach (var (cy, cr) in new[] { (275.0, 26.0), (410.0, 40.0) })
				{
					double rr = cr * (1 + 0.16 * kick + 0.1 * shout + 0.05 * charge * JS.sin(bt * TAU * 2));
					g.fillStyle = "rgba(40,36,48,1)"; circle(inx, cy, rr + 8);
					g.strokeStyle = TBW(0.6 + 0.3 * kick); g.lineWidth = 3; g.beginPath(); g.arc(inx, cy, rr, 0, TAU); g.stroke();
					g.fillStyle = TBY(0.25 + 0.6 * glow); circle(inx, cy, rr * 0.35);
				}
			}
			// the crowd at the bottom: they bob on the beat and put their hands up on the shouts
			g.fillStyle = "rgba(4,2,8,0.85)"; g.strokeStyle = "rgba(4,2,8,0.85)"; g.lineCap = "round"; g.lineWidth = 7;
			for (int i = 0; i < 28; i++)
			{
				double x = 24 + i * 46.5 + 8 * TBH(i), bob = 6 * Math.Abs(JS.sin((bt + 0.17 * (i % 3)) * Math.PI)), y = H - 18 - 10 * TBH(i * 3.3) - bob;
				circle(x, y - 26, 13); g.fillRect(x - 17, y - 12, 34, 40);
				double up = Math.Max(shout, 0.35 * kick) * (0.6 + 0.4 * TBH(i * 5.7));
				if (up > 0.05) foreach (var sd in new[] { -1, 1 }) { g.beginPath(); g.moveTo(x + sd * 14, y - 8); g.lineTo(x + sd * (18 + 6 * up), y - 8 - 46 * up); g.stroke(); }
			}
			g.restore();
		};
		return h;
	}

	// ---------- the equalizer: on beat b its 16 bars jump from the floor (or drop from the ceiling) up to the lane's edge ----------
	// They grow in 0.1 beat, stay 0.3 beat, go back in 0.3 beat. From b - tl a white dashed line shows how far they will come.
	public static Hz TBEq(bool fromTop, double b, double tl)
	{
		var h = new Hz { kind = "TBEq" }; const double BW = 60;
		var len = new double[16];
		for (int k = 0; k < 16; k++) len[k] = fromTop ? TBLane(k, b) - TBLaneH : H - (TBLane(k, b) + TBLaneH);
		double f(double bt) => bt < b ? 0 : bt < b + 0.1 ? TBOut((bt - b) / 0.1) : bt < b + 0.4 ? 1 : 1 - TBEase((bt - b - 0.4) / 0.3);
		h.update = (t, dt) => { if (BT(t) > b + 0.75) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save();
			if (bt < b)
			{
				double q = TBk(bt, b - tl, b);
				for (int k = 0; k < 16; k++)
				{
					double x = 40 + 80 * k - BW / 2, e = fromTop ? len[k] : H - len[k];
					g.fillStyle = TBW(0.04 + 0.06 * q); g.fillRect(x, fromTop ? 0 : e, BW, len[k]);
					TBDash(x, e, x + BW, e, TBW(0.35 + 0.55 * q), 3);
				}
			}
			else
			{
				double fr = f(bt); g.shadowColor = PINK; g.shadowBlur = 10;
				for (int k = 0; k < 16; k++)
				{
					double l = len[k] * fr; if (l < 1) continue; double x = 40 + 80 * k - BW / 2, y = fromTop ? 0 : H - l;
					g.fillStyle = PINK; g.fillRect(x, y, BW, l);
				}
				// the bar's blocks (lighter pink lines) and its bright end
				g.shadowBlur = 0; g.fillStyle = PINKW;
				for (int k = 0; k < 16; k++)
				{
					double l = len[k] * fr; if (l < 1) continue; double x = 40 + 80 * k - BW / 2;
					for (double d = 26; d < l - 6; d += 26) g.fillRect(x, fromTop ? d : H - d, BW, 2);
					g.fillRect(x, fromTop ? l - 5 : H - l, BW, 5);
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b) return false; double fr = f(bt), m = p.r - 2;
			int k = (int)Math.Floor(p.x / 80);
			for (int kk = Math.Max(0, k - 1); kk <= Math.Min(15, k + 1); kk++)
			{
				double l = len[kk] * fr; if (l < 1) continue; double x0 = 40 + 80 * kk - BW / 2, y0 = fromTop ? 0 : H - l;
				double dx = Math.Max(0, Math.Max(x0 - p.x, p.x - (x0 + BW))), dy = Math.Max(0, Math.Max(y0 - p.y, p.y - (y0 + l)));
				if (dx * dx + dy * dy < m * m) return true;
			}
			return false;
		};
		return h;
	}

	// ---------- a shout: n sound waves (0.25 beat apart) from the speaker at side (-1 left, 1 right), from beat b, sp px/s ----------
	// Each wave is a pink arc round a point 300 px outside the screen, with a gap that is the lane where it is now.
	// From b - tl the speaker's edge shows a white dashed arc with the gap's ends.
	public static Hz TBShout(int side, double b, double tl, double sp, int n, double w)
	{
		var h = new Hz { kind = "TBShout" }; const double R0 = 300, CY = 360;
		double cx = side < 0 ? -R0 : W + R0, t0 = B(b), gh = TBLaneH + (easy ? 25 : 12);
		double rad(int j, double t) => R0 + (t - B(b + 0.25 * j)) * sp;
		double gapY(double r, double bt) => TBLane(TBLaneX(side < 0 ? cx + r : cx - r), bt);
		h.update = (t, dt) => { if (t > t0 && rad(n - 1, t) > W + 2 * R0 + 40) h.dead = true; };
		// the two arcs (above and below the gap) of a wave of radius r, as angles on the side that faces the screen
		void arcs(double r, double gy, Action<double, double> f)
		{
			double s1 = clamp((gy - gh - CY) / r, -1, 1), s2 = clamp((gy + gh - CY) / r, -1, 1), a1 = Math.Asin(s1), a2 = Math.Asin(s2);
			if (side < 0) { if (a1 > -Math.PI / 2) f(-Math.PI / 2, a1); if (a2 < Math.PI / 2) f(a2, Math.PI / 2); }
			else { if (a1 > -Math.PI / 2) f(Math.PI - a1, 1.5 * Math.PI); if (a2 < Math.PI / 2) f(Math.PI / 2, Math.PI - a2); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b - tl) return;
			g.save(); g.lineCap = "butt";
			if (t < t0)
			{
				double q = TBk(bt, b - tl, b), r = R0 + 22 + 30 * (1 - q), gy = gapY(r, bt);
				g.strokeStyle = TBW(0.3 + 0.6 * q); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 });
				arcs(r, gy, (s, e) => { g.beginPath(); g.arc(cx, CY, r, s, e); g.stroke(); }); g.setLineDash(new double[0]);
				double ex = side < 0 ? 0 : W, ix = ex - side * 34; g.lineWidth = 4;
				foreach (var yy in new[] { gy - gh, gy + gh }) { g.beginPath(); g.moveTo(ex, yy); g.lineTo(ix, yy); g.stroke(); }
			}
			else
			{
				g.shadowColor = PINK; g.shadowBlur = 12; g.strokeStyle = PINK;
				for (int j = 0; j < n; j++)
				{
					double r = rad(j, t); if (r < R0) continue; g.lineWidth = w * (1 - 0.18 * j);
					arcs(r, gapY(r, bt), (s, e) => { g.beginPath(); g.arc(cx, CY, r, s, e); g.stroke(); });
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double bt = BT(t), d = hypot(p.x - cx, p.y - CY), m = p.r - 2;
			for (int j = 0; j < n; j++)
			{
				double r = rad(j, t); if (r < R0 || Math.Abs(d - r) >= w * (1 - 0.18 * j) / 2 + m) continue;
				double gy = gapY(r, bt); if (p.y < gy - gh + m || p.y > gy + gh - m) return true;
			}
			return false;
		};
		return h;
	}
}
