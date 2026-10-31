using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// يالعين قسامي, main level 24: the laser off the mirrors (3:34), the room that closes in (the build-up, 3:44), and the hardest part
// (3:54-4:34): the turning grating, the pendulums, the fireworks.
public static partial class G
{
	// ---------- E. the laser off the mirrors: six gold mirrors turn into place, then a laser from the side bounces off them ----------
	static readonly double[] QS_MX = { 300, 640, 980, 300, 640, 980 }, QS_MY = { 175, 175, 175, 545, 545, 545 };
	const double QS_ML = 160;
	// the laser's way from (x, y) along a (radians) off the mirrors at angles ang (degrees): the points where it turns
	static List<double[]> QSBeamPath(double x, double y, double a, double[] ang)
	{
		var pts = new List<double[]> { new[] { x, y } }; double dx = JS.cos(a), dy = JS.sin(a); int last = -1;
		for (int n = 0; n < 8; n++)
		{
			double best = 1e9; int bk = -1;
			for (int k = 0; k < QS_MX.Length; k++)
			{
				if (k == last) continue; double ma = QSRad(ang[k]), ux = JS.cos(ma), uy = JS.sin(ma);
				double ax = QS_MX[k] - ux * QS_ML / 2 - x, ay = QS_MY[k] - uy * QS_ML / 2 - y, den = dx * uy - dy * ux; if (Math.Abs(den) < 1e-9) continue;
				double tt = (ax * uy - ay * ux) / den, s = (ax * dy - ay * dx) / den;   // along the ray, along the mirror
				if (tt > 1 && s >= 0 && s <= QS_ML && tt < best) { best = tt; bk = k; }
			}
			if (bk < 0)
			{
				double tx = dx > 1e-9 ? (W + 40 - x) / dx : dx < -1e-9 ? (-40 - x) / dx : 1e9, ty = dy > 1e-9 ? (H + 40 - y) / dy : dy < -1e-9 ? (-40 - y) / dy : 1e9, tt = Math.Min(tx, ty);
				pts.Add(new[] { x + dx * tt, y + dy * tt }); break;
			}
			x += dx * best; y += dy * best; pts.Add(new[] { x, y });
			double na = QSRad(ang[bk]) + Math.PI / 2, nx = JS.cos(na), ny = JS.sin(na), dn = dx * nx + dy * ny; dx -= 2 * dn * nx; dy -= 2 * dn * ny; last = bk;
		}
		return pts;
	}
	public static Hz QSBeam(List<double[]> pts, double bw, double b, double hold, double hw)
	{
		var h = new Hz { kind = "QSBeam" };
		h.update = (t, dt) => { if (BT(t) > b + hold + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bw || bt > b + hold + 0.3) return;
			g.save(); g.lineCap = "round"; g.lineJoin = "round";
			if (bt < b)
			{
				double q = QSk(bt, bw, b); g.strokeStyle = QSW(0.3 + 0.5 * q); g.lineWidth = 2; g.setLineDash(new double[] { 12, 9 }); g.beginPath();
				g.moveTo(pts[0][0], pts[0][1]); for (int i = 1; i < pts.Count; i++) g.lineTo(pts[i][0], pts[i][1]); g.stroke(); g.setLineDash(new double[0]);
				g.fillStyle = QSG(0.5 + 0.5 * q); circle(pts[0][0], pts[0][1], 14 + 6 * q);
			}
			else
			{
				double al = 1 - QSk(bt, b + hold, b + hold + 0.3), wd = 2 * hw * QSOut((bt - b) / 0.08);
				g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = QSP(al); g.lineWidth = wd; g.beginPath();
				g.moveTo(pts[0][0], pts[0][1]); for (int i = 1; i < pts.Count; i++) g.lineTo(pts[i][0], pts[i][1]); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b || bt >= b + hold) return false;
			for (int i = 1; i < pts.Count; i++) if (QSSeg(p.x, p.y, pts[i - 1][0], pts[i - 1][1], pts[i][0], pts[i][1]) < hw + p.r - 2) return true;
			return false;
		};
		return h;
	}
	// the mirrors (harmless gold): before pulse j (beat pb[j]) they turn to ang[j] in the 0.4 beat from pb[j] - wn
	public static Hz QSMirrorSet(List<double> pb, List<double[]> ang, double wn, double b0, double b1)
	{
		var h = new Hz { kind = "QSMirrorSet" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 2 || bt > b1 + 1) return;
			int j = -1; for (int i = 0; i < pb.Count; i++) if (bt >= pb[i] - wn) j = i;
			double al = QSk(bt, b0 - 2, b0 - 1) * (1 - QSk(bt, b1, b1 + 1));
			g.save(); g.lineCap = "round";
			for (int k = 0; k < QS_MX.Length; k++)
			{
				double a = j < 0 ? ang[0][k] : j == 0 ? ang[0][k] : ang[j - 1][k] + (ang[j][k] - ang[j - 1][k]) * QSE((bt - (pb[j] - wn)) / 0.4);
				double ux = JS.cos(QSRad(a)) * QS_ML / 2, uy = JS.sin(QSRad(a)) * QS_ML / 2;
				g.strokeStyle = QSG(0.95 * al); g.lineWidth = 7; g.beginPath(); g.moveTo(QS_MX[k] - ux, QS_MY[k] - uy); g.lineTo(QS_MX[k] + ux, QS_MY[k] + uy); g.stroke();
				g.fillStyle = QSW(0.6 * al); circle(QS_MX[k], QS_MY[k], 6);
			}
			g.restore();
		};
		return h;
	}

	// ---------- F. the build-up: the room closes in. Pink floods all but a room; on each step the room jumps smaller (its next
	// shape shows white dashed), the steps come every beat, then every half, then every quarter beat. At 3:53 the flood beyond
	// the room's wall breaks away; the room then shuts: you dash out through its wall (Easy: a gold door opens in it). ----------
	static readonly double[][] QS_ROOM = QSRoomPlan();
	static double[] QS_RS => QS_ROOM[0];
	static double[] QS_RX => QS_ROOM[1];
	static double[] QS_RY => QS_ROOM[2];
	static double[] QS_RW => QS_ROOM[3];
	static double[] QS_RH => QS_ROOM[4];
	static double[][] QSRoomPlan()
	{
		var sb = new List<double>(); for (double b = 140; b < 148; b += 1) sb.Add(b); for (double b = 148; b < 156; b += 0.5) sb.Add(b); for (double b = 156; b < 158; b += 0.25) sb.Add(b);
		int n = sb.Count; var RS = sb.ToArray(); var RX = new double[n + 1]; var RY = new double[n + 1]; var RW = new double[n + 1]; var RH = new double[n + 1];
		double cx = W / 2, cy = H / 2; RX[0] = cx; RY[0] = cy; RW[0] = W / 2 + 20; RH[0] = H / 2 + 20;
		int[] dx = { 1, 0, -1, 0, -1, 0, 1, 0 }, dy = { 0, 1, 0, -1, 0, -1, 0, 1 };
		for (int i = 1; i <= n; i++)
		{
			double f = (double)i / n, step = i <= 8 ? 1 : i <= 24 ? 0.5 : 0.25, amp = step >= 1 ? 80 : step >= 0.5 ? 46 : 26;
			double hw = 600 - 430 * Math.Pow(f, 0.8), hh = 330 - 210 * Math.Pow(f, 0.8);
			cx = clamp(cx + dx[i % 8] * amp, hw + 30, W - hw - 30); cy = clamp(cy + dy[i % 8] * amp * 0.7, hh + 30, H - hh - 30);
			RX[i] = cx; RY[i] = cy; RW[i] = hw; RH[i] = hh;
		}
		return new[] { RS, RX, RY, RW, RH };
	}
	static void QSRoomAt(double bt, out double cx, out double cy, out double hw, out double hh)
	{
		int n = QS_RS.Length, i = 0; while (i < n && bt >= QS_RS[i]) i++;
		// i = steps begun; step i-1 eases from target i-1 to target i over its length
		if (i == 0) { cx = QS_RX[0]; cy = QS_RY[0]; hw = QS_RW[0]; hh = QS_RH[0]; return; }
		double s0 = QS_RS[i - 1], s1 = i < n ? QS_RS[i] : 158, k = QSE((bt - s0) / ((s1 - s0) * 0.7));
		cx = QS_RX[i - 1] + (QS_RX[i] - QS_RX[i - 1]) * k; cy = QS_RY[i - 1] + (QS_RY[i] - QS_RY[i - 1]) * k; hw = QS_RW[i - 1] + (QS_RW[i] - QS_RW[i - 1]) * k; hh = QS_RH[i - 1] + (QS_RH[i] - QS_RH[i - 1]) * k;
		if (bt > 158) { double s = 1 - QSE((bt - 158) / 1.5); hw *= s; hh *= s; }
	}
	public static Hz QSRoom(bool door)
	{
		var h = new Hz { kind = "QSRoom" }; const double BAND = 100;
		QSRoomAt(158, out var fx, out var fy, out var fw, out var fh);   // the wall's outer edge (fixed from 3:53)
		int side = fx > W / 2 ? -1 : 1;   // the door: on the side that faces the middle
		h.update = (t, dt) => { if (BT(t) > 161) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < 139 || bt > 161) return;
			QSRoomAt(bt, out var cx, out var cy, out var hw, out var hh);
			g.save();
			if (bt < 140) { double q = QSk(bt, 139, 140); g.strokeStyle = QSW(0.3 + 0.5 * q); g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 }); g.strokeRect(14, 14, W - 28, H - 28); g.setLineDash(new double[0]); }
			else if (bt < 159.5)
			{
				g.fillStyle = PINK;
				if (bt < 157.5) g.fillRect(0, 0, W, H);
				else
				{
					g.fillRect(fx - fw - BAND, fy - fh - BAND, 2 * (fw + BAND), 2 * (fh + BAND));
					// the flood beyond the wall breaks away (gold shards)
					double k = QSk(bt, 157.5, 158.5); g.fillStyle = QSG(0.6 * (1 - k));
					for (int i = 0; i < 40; i++) { double ax = QSH(i) * W, ay = QSH(i + 50) * H; if (Math.Abs(ax - fx) < fw + BAND && Math.Abs(ay - fy) < fh + BAND) continue; g.fillRect(ax, ay + 300 * k * k, 18, 18); }
					if (door) { g.fillStyle = "rgb(16,12,6)"; double dx0 = side > 0 ? fx + fw - 4 : fx - fw - BAND - 4; g.fillRect(dx0, fy - 70, BAND + 8, 140); g.strokeStyle = QSG(0.9); g.lineWidth = 3; g.strokeRect(dx0, fy - 70, BAND + 8, 140); }
					// the way out: gold chevrons on the wall
					g.strokeStyle = QSG(0.5 + 0.4 * JS.sin(bt * TAU * 2)); g.lineWidth = 4;
					for (int s = -1; s <= 1; s += 2)
					{
						double xx = fx + s * (fw + BAND / 2); g.beginPath(); g.moveTo(xx - s * 14, fy - 22); g.lineTo(xx + s * 8, fy); g.lineTo(xx - s * 14, fy + 22); g.stroke();
						double yy = fy + s * (fh + BAND / 2); g.beginPath(); g.moveTo(fx - 22, yy - s * 14); g.lineTo(fx, yy + s * 8); g.lineTo(fx + 22, yy - s * 14); g.stroke();
					}
				}
				g.fillStyle = "rgb(16,12,6)"; if (hw > 0 && hh > 0) g.fillRect(cx - hw, cy - hh, 2 * hw, 2 * hh);
				g.strokeStyle = QSG(0.9); g.lineWidth = 3; if (hw > 0 && hh > 0) g.strokeRect(cx - hw, cy - hh, 2 * hw, 2 * hh);
				// the next step's room (white dashed)
				int n = QS_RS.Length, i2 = 0; while (i2 < n && bt >= QS_RS[i2]) i2++;
				if (i2 < n && bt < 157.5) { int nx = i2 + 1 <= n ? i2 + 1 : n; g.strokeStyle = QSW(0.55); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(QS_RX[nx] - QS_RW[nx], QS_RY[nx] - QS_RH[nx], 2 * QS_RW[nx], 2 * QS_RH[nx]); g.setLineDash(new double[0]); }
			}
			else
			{
				// it shatters (gold, harmless)
				double k = QSk(bt, 159.5, 160.5); g.fillStyle = QSG(0.8 * (1 - k));
				for (int i = 0; i < 48; i++) { double a = QSH(i * 3) * TAU, d = (fw + BAND) * (0.6 + 0.5 * QSH(i + 7)) * (1 + 1.6 * k); g.fillRect(fx + JS.cos(a) * d - 9, fy + JS.sin(a) * d * 0.6 - 9, 18, 18); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < 140 || bt >= 159.5) return false;
			QSRoomAt(bt, out var cx, out var cy, out var hw, out var hh); double m = p.r - 2;
			bool inRoom = Math.Abs(p.x - cx) < hw - m && Math.Abs(p.y - cy) < hh - m; if (inRoom) return false;
			if (bt < 157.5) return true;
			bool inWall = Math.Abs(p.x - fx) < fw + BAND + m && Math.Abs(p.y - fy) < fh + BAND + m; if (!inWall) return false;
			if (door && Math.Abs(p.y - fy) < 70 - m && (side > 0 ? p.x > fx : p.x < fx)) return false;
			return true;
		};
		return h;
	}

	// ---------- G. the turning grating (3:54): the whole screen is pink bars with gaps; they turn round the middle and slide; you ride
	// a gap. Later a second grating crosses it and you ride a cell. ----------
	public static Hz QSGrate(double b0, double b1, double a0, double[] kbA, double[] wA, double[] kbS, double[] wS, double sp, double bw, double wn)
	{
		var h = new Hz { kind = "QSGrate" };
		double ang(double bt) => QSRad(a0 + QSTurn(kbA, wA, Math.Max(b0, bt)));
		double off(double bt) => QSTurn(kbS, wS, Math.Max(b0, bt));
		double wid(double bt) => bw * QSOut((bt - b0) / 0.5) * (1 - QSk(bt, b1, b1 + 0.4));
		h.update = (t, dt) => { if (BT(t) > b1 + 0.5) h.dead = true; };
		void lines(double bt, Action<double, double, double, double> f)
		{
			double a = ang(bt), nx = JS.cos(a), ny = JS.sin(a), s = off(bt);
			for (double o = QSMod(s + 760, sp) - 760; o < 760; o += sp) { double cx = W / 2 + nx * o, cy = H / 2 + ny * o; f(cx - ny * 800, cy + nx * 800, cx + ny * 800, cy - nx * 800); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - wn || bt > b1 + 0.4) return;
			g.save(); g.lineCap = "butt";
			if (bt < b0) { double q = QSk(bt, b0 - wn, b0); lines(b0, (x0, y0, x1, y1) => QSDash(x0, y0, x1, y1, QSW(0.25 + 0.5 * q), 2)); }
			else { g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = Math.Max(0.1, wid(bt)); g.beginPath(); lines(bt, (x0, y0, x1, y1) => { g.moveTo(x0, y0); g.lineTo(x1, y1); }); g.stroke(); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1 + 0.3) return false;
			double a = ang(bt), u = (p.x - W / 2) * JS.cos(a) + (p.y - H / 2) * JS.sin(a), m = QSMod(u - off(bt), sp);
			return Math.Min(m, sp - m) < wid(bt) / 2 + p.r - 2;
		};
		return h;
	}

	// ---------- H. the pendulums (4:04): pink pendulums swing from the top (and later from the bottom too); you cross between them
	// on time. The arc each one swings shows white dashed; they get faster. ----------
	public static Hz QSPend(double px, double py, double L, double dir0, double A, double ph0, double[] kb, double[] w, double b0, double b1, double br, double rw)
	{
		var h = new Hz { kind = "QSPend" };
		double th(double bt) => QSRad(dir0 + A * JS.sin(QSRad(ph0 + QSTurn(kb, w, bt))));
		h.update = (t, dt) => { if (BT(t) > b1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - 1.5 || bt > b1 + 0.6) return;
			double a = th(bt), bx = px + JS.cos(a) * L, by = py + JS.sin(a) * L;
			g.save(); g.lineCap = "round";
			double ql = QSk(bt, b0 - 1.5, b0 - 0.5) * (1 - QSk(bt, b1, b1 + 0.6));
			g.strokeStyle = QSW(0.22 * ql); g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath(); g.arc(px, py, L, QSRad(dir0 - A), QSRad(dir0 + A)); g.stroke(); g.setLineDash(new double[0]);
			if (bt < b0) { g.strokeStyle = QSW(0.6 * ql); g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(px, py); g.lineTo(bx, by); g.stroke(); g.setLineDash(new double[0]); g.beginPath(); g.arc(bx, by, br, 0, TAU); g.stroke(); }
			else
			{
				double al = 1 - QSk(bt, b1, b1 + 0.6);
				g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = QSP(al); g.lineWidth = rw; g.beginPath(); g.moveTo(px, py); g.lineTo(bx, by); g.stroke();
				g.fillStyle = QSP(al); circle(bx, by, br);
				g.shadowBlur = 0; g.strokeStyle = $"rgba(255,200,222,{str(0.8 * al)})"; g.lineWidth = 3; g.beginPath(); g.arc(bx, by, br * 0.55, 0, TAU); g.stroke();
			}
			g.fillStyle = QSG(0.9 * ql); circle(px, py, 10);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false;
			double a = th(bt), bx = px + JS.cos(a) * L, by = py + JS.sin(a) * L;
			return hypot(p.x - bx, p.y - by) < br + p.r - 2 || QSSeg(p.x, p.y, px, py, bx, by) < rw / 2 + p.r - 2;
		};
		return h;
	}

	// ---------- I. the fireworks (4:24, the top of the hard part): rockets rise (their way shows white dashed) and burst at the top into
	// pink sparks that fall; more and more of them to the end. ----------
	public static Hz QSRocket(double x0, double xa, double ya, double wn, double bl, double bb, int n, double v0, double gr, double a0, double len = 26)
	{
		var h = new Hz { kind = "QSRocket" }; double tl = B(bl), tb = B(bb);
		void head(double t, out double x, out double y) { double k = QSOut((t - tl) / (tb - tl)); x = x0 + (xa - x0) * k; y = H + 30 + (ya - H - 30) * k; }
		h.update = (t, dt) => { if (t > tb + 3.2) h.dead = true; };
		void sparks(double t, Action<double, double, double> f)
		{
			double tau = t - tb; for (int k = 0; k < n; k++) { double a = QSRad(a0 + k * 360.0 / n), vx = JS.cos(a) * v0, vy = JS.sin(a) * v0 + gr * tau; f(xa + JS.cos(a) * v0 * tau, ya + JS.sin(a) * v0 * tau + 0.5 * gr * tau * tau, JS.atan2(vy, vx)); }
		}
		h.draw = t =>
		{
			double bt = BT(t); if (bt < bl - wn) return;
			g.save();
			if (t < tb)
			{
				double q = QSk(bt, bl - wn, bl); QSDash(x0, H + 30, xa, ya, QSW(0.2 + 0.4 * q), 2);
				g.strokeStyle = QSW(0.25 + 0.45 * q); g.lineWidth = 2; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(xa, ya, 46, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
				if (bt > bb - 0.6) { double q2 = QSk(bt, bb - 0.6, bb); for (int k = 0; k < n; k++) { double a = QSRad(a0 + k * 360.0 / n); QSDash(xa + JS.cos(a) * 50, ya + JS.sin(a) * 50, xa + JS.cos(a) * (60 + 110 * q2), ya + JS.sin(a) * (60 + 110 * q2), QSW(0.5 * q2), 2); } }
				if (t >= tl) { head(t, out var hx, out var hy); g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK; circle(hx, hy, 8); g.shadowBlur = 0; g.fillStyle = QSG(0.7); circle(hx, hy + 14, 4); }
			}
			else
			{
				double fl = 1 - QSk(t, tb, tb + 0.25); if (fl > 0) { g.fillStyle = QSG(0.5 * fl); circle(xa, ya, 30 + 50 * (1 - fl)); }
				g.shadowColor = PINK; g.shadowBlur = 10; g.fillStyle = PINK; sparks(t, (x, y, a) => { if (x > -30 && x < W + 30 && y > -30 && y < H + 30) QSDart(x, y, len, a, 5); });
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (t < tl) return false;
			if (t < tb) { head(t, out var hx, out var hy); return hypot(p.x - hx, p.y - hy) < 8 + p.r - 2; }
			bool hit = false; sparks(t, (x, y, a) => { if (!hit && Math.Abs(p.x - x) < 40 && Math.Abs(p.y - y) < 40 && QSDartHit(p, x, y, len, a)) hit = true; }); return hit;
		};
		return h;
	}
}
