using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// original index.html lines 1140-1300: "وعد مني" (drawPerson, Person, Board, pieceShape, ChessPiece, Tower, lockShape, Lock, Link, BackStab, Traitor)
// and buildFriend2. R2, Squeeze are in Shared.cs; its AFK maker is in Afk.cs
public static partial class G
{
	public static void drawPerson(double x, double y, string col, double a = 1, double sc = 1) { g.save(); g.translate(x, y); g.scale(sc, sc); g.globalAlpha = a; g.strokeStyle = col; g.fillStyle = col; g.lineWidth = 3; g.lineCap = "round"; circle(0, -16, 7); g.beginPath(); g.moveTo(0, -9); g.lineTo(0, 6); g.moveTo(-9, -3); g.lineTo(9, -3); g.moveTo(0, 6); g.lineTo(-7, 17); g.moveTo(0, 6); g.lineTo(7, 17); g.stroke(); g.restore(); g.globalAlpha = 1; }

	// Person options (JS o={}): undefined fields are null
	public class PersonO { public bool follow; public double? r, speed, tx, ty, fade; public PersonO Clone() => (PersonO)MemberwiseClone(); }
	class PDash { public double vx, vy, te; }
	public static Hz Person(double x, double y, double t0, double tb, string mode, Tier T, PersonO o = null)
	{
		o ??= new PersonO();
		double vx = 0, vy = 0, ang = rnd() * TAU; PDash dash = null; double wn = SPB * T.wn, orbR = o.r is double r0 && r0 != 0 ? r0 : 170; double? fade = o.fade;
		var h = new Hz { x = x, y = y };
		h.update = (t, dt) =>
		{
			if (fade != null && t >= fade + 0.6) { h.dead = true; return; }
			if (t < tb)
			{
				if (player == null) return; double tx, ty;
				if (o.follow) { ang += dt * 0.7; tx = player.x + JS.cos(ang) * orbR; ty = player.y + JS.sin(ang) * orbR; } else { tx = o.tx ?? W / 2; ty = o.ty ?? H / 2; }
				double dx = tx - h.x, dy = ty - h.y, d = hypot(dx, dy); double sp = Math.Min(o.speed is double s0 && s0 != 0 ? s0 : 120, d * 2); double dd = d != 0 && !double.IsNaN(d) ? d : 1; h.x += dx / dd * sp * dt; h.y += dy / dd * sp * dt; return;
			}
			if (mode == "pop") { for (int i = 0; i < 6; i++) { double a = i * TAU / 6 + 0.3; hazards.Add(Bullet(h.x, h.y, JS.cos(a) * T.sp * 0.85, JS.sin(a) * T.sp * 0.85, 8)); } burst(h.x, h.y, PINK, 10); h.dead = true; return; }
			if (dash == null) { double a = player != null ? JS.atan2(player.y - h.y, player.x - h.x) : 0; dash = new PDash { vx = JS.cos(a) * T.sp * 1.6, vy = JS.sin(a) * T.sp * 1.6, te = t + 0.5 }; }
			h.x += dash.vx * dt; h.y += dash.vy * dt;
			if (t >= dash.te) { for (int i = 0; i < 4; i++) { double a = i * TAU / 4 + Math.PI / 4; hazards.Add(Bullet(h.x, h.y, JS.cos(a) * T.sp * 0.8, JS.sin(a) * T.sp * 0.8, 7)); } burst(h.x, h.y, PINK, 8); h.dead = true; }
		};
		h.draw = t =>
		{
			double fa = fade != null ? clamp(1 - (t - fade.Value) / 0.6, 0, 1) : 1, ia = clamp((t - t0) / 0.4, 0, 1);
			if (t < tb - wn) drawPerson(h.x, h.y, GREY + "0.75)", fa * ia);
			else if (t < tb) { bool on = Math.Floor((t - (tb - wn)) * 12) % 2 == 0; drawPerson(h.x, h.y, on ? PINK : GREY + "0.75)", fa); if (on) { g.strokeStyle = "rgba(255,42,109,0.35)"; g.lineWidth = 2; g.beginPath(); g.arc(h.x, h.y - 2, 26, 0, TAU); g.stroke(); } }
			else { g.save(); g.fillStyle = "rgba(255,42,109,0.4)"; circle(h.x, h.y, 16); g.shadowColor = PINK; g.shadowBlur = 16; drawPerson(h.x, h.y, PINK, 1, 1.15); g.restore(); }
		};
		h.hit = (p, t) => t >= tb && hypot(p.x - h.x, p.y - h.y) < 16 + p.r;
		return h;
	}

	// ===== the chess board: "لعبت بحالي لعبة" =====
	public const double CELL = 120, BX = 40;
	public static double[] cellC(double i, double j) => new[] { BX + i * CELL + CELL / 2, j * CELL + CELL / 2 };
	public static Hz Board(double t0, double t1)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > t1 + 0.6) h.dead = true; };
		h.draw = t =>
		{
			double a = Math.Min(1, Math.Min((t - t0) / 0.6, (t1 + 0.6 - t) / 0.6)); if (a <= 0) return; g.fillStyle = GREY + str(0.05 * a) + ")";
			for (int i = 0; i < 10; i++) for (int j = 0; j < 6; j++) if ((i + j) % 2 == 0) g.fillRect(BX + i * CELL, j * CELL, CELL, CELL);
			g.strokeStyle = GREY + str(0.12 * a) + ")"; g.lineWidth = 1; g.strokeRect(BX, 0, CELL * 10, CELL * 6);
		};
		return h;
	}
	public static void pieceShape(string type, double s)
	{
		g.beginPath();
		if (type == "R") { g.moveTo(-s * 0.45, s * 0.5); g.lineTo(s * 0.45, s * 0.5); g.lineTo(s * 0.35, -s * 0.25); g.lineTo(s * 0.45, -s * 0.25); g.lineTo(s * 0.45, -s * 0.5); g.lineTo(s * 0.25, -s * 0.5); g.lineTo(s * 0.25, -s * 0.38); g.lineTo(s * 0.08, -s * 0.38); g.lineTo(s * 0.08, -s * 0.5); g.lineTo(-s * 0.08, -s * 0.5); g.lineTo(-s * 0.08, -s * 0.38); g.lineTo(-s * 0.25, -s * 0.38); g.lineTo(-s * 0.25, -s * 0.5); g.lineTo(-s * 0.45, -s * 0.5); g.lineTo(-s * 0.45, -s * 0.25); g.lineTo(-s * 0.35, -s * 0.25); g.closePath(); }
		else if (type == "B") { g.ellipse(0, -s * 0.1, s * 0.24, s * 0.36, 0, 0, TAU); g.moveTo(-s * 0.4, s * 0.5); g.lineTo(s * 0.4, s * 0.5); g.lineTo(s * 0.25, s * 0.25); g.lineTo(-s * 0.25, s * 0.25); g.closePath(); g.moveTo(s * 0.06, -s * 0.6); g.arc(0, -s * 0.56, s * 0.07, 0, TAU); }
		else if (type == "N") { g.moveTo(-s * 0.4, s * 0.5); g.lineTo(s * 0.4, s * 0.5); g.lineTo(s * 0.3, -s * 0.05); g.lineTo(s * 0.05, -s * 0.15); g.lineTo(s * 0.35, 0.02 * s); g.lineTo(s * 0.4, -s * 0.2); g.lineTo(s * 0.1, -s * 0.55); g.lineTo(-s * 0.15, -s * 0.5); g.lineTo(-s * 0.35, -s * 0.1); g.lineTo(-s * 0.2, s * 0.15); g.closePath(); }
		else { g.moveTo(-s * 0.42, s * 0.5); g.lineTo(s * 0.42, s * 0.5); g.lineTo(s * 0.45, -s * 0.35); g.lineTo(s * 0.2, -s * 0.05); g.lineTo(0, -s * 0.5); g.lineTo(-s * 0.2, -s * 0.05); g.lineTo(-s * 0.45, -s * 0.35); g.closePath(); }
	}
	public static Hz ChessPiece(string type, double i, double j, double tw, double ta, double te, double w = 58)
	{
		var cc = cellC(i, j); double cx = cc[0], cy = cc[1];
		var KN = new List<double[]>(); foreach (var (a, b) in new[] { (1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1) }) { double ka = i + a, kb = j + b; if (ka >= 0 && ka < 10 && kb >= 0 && kb < 6) KN.Add(new[] { ka, kb }); }
		bool rook = type == "R" || type == "Q", bish = type == "B" || type == "Q";
		void lines()
		{
			if (rook) { g.fillRect(-2000, cy - w / 2, 4000, w); g.fillRect(cx - w / 2, -2000, w, 4000); }
			if (bish) foreach (var sg in new double[] { 1, -1 }) { g.save(); g.translate(cx, cy); g.rotate(sg * Math.PI / 4); g.fillRect(-2000, -w / 2, 4000, w); g.restore(); }
			if (type == "N") foreach (var k in KN) { var q = cellC(k[0], k[1]); double x = q[0], y = q[1]; g.fillRect(x - CELL / 2 + 6, y - CELL / 2 + 6, CELL - 12, CELL - 12); }
		}
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			double a = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3);
			if (t < ta) { g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.13 * a)})"; lines(); }
			else if (t < te) { g.save(); g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = (t - ta) < 0.08 ? PINKW : PINK; lines(); g.restore(); }
			g.save(); g.translate(cx, cy + (t < ta ? (1 - a) * -30 : 0)); g.globalAlpha = t < ta ? 0.4 + 0.5 * a : a; pieceShape(type, 78 * (1 + 0.08 * pulse)); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14; g.fill(); g.restore(); g.globalAlpha = 1;
		};
		h.hit = (p, t) =>
		{
			if (t < ta || t >= te) return false; double dx = p.x - cx, dy = p.y - cy, r = w / 2 + p.r - 2;
			if (rook && (Math.Abs(dy) < r || Math.Abs(dx) < r)) return true;
			if (bish && (Math.Abs(dx - dy) / Math.Sqrt(2) < r || Math.Abs(dx + dy) / Math.Sqrt(2) < r)) return true;
			if (type == "N") { foreach (var k in KN) { var q = cellC(k[0], k[1]); double x = q[0], y = q[1]; if (Math.Abs(p.x - x) < CELL / 2 - 6 + p.r && Math.Abs(p.y - y) < CELL / 2 - 6 + p.r) return true; } }
			return hypot(dx, dy) < 30 + p.r;
		};
		return h;
	}
	// ===== the building: a tower builds up, then falls and its stones scatter =====
	public static Hz Tower(double x, int n, double tb, double step, double tf, double dir, Tier T)
	{
		const double bw = 86, bh = 84; double h = n * bh, wn = SPB * T.wn, fallD = 0.35, lie = SPB * 0.6; var piv = new[] { x + dir * bw / 2, H };
		Func<double, double> ang = t => t < tf ? 0 : dir * Math.PI / 2 * Math.Min(1, JS.pow((t - tf) / fallD, 2));
		Func<double, double> blocks = t => Math.Min(n, Math.Floor((t - tb) / step) + 1);
		var hz = new Hz();
		hz.update = (t, dt) =>
		{
			if (t >= tf + fallD + lie)
			{
				double a = ang(t), c = JS.cos(a), s = JS.sin(a);
				for (int k = 0; k < n; k++) { double lx = -dir * bw / 2, ly = -(k + 0.5) * bh, cx = piv[0] + lx * c - ly * s, cy = piv[1] + lx * s + ly * c; double @out = -Math.PI / 2 + dir * (0.3 + R2() * 0.9); hazards.Add(SqBullet(cx, cy, JS.cos(@out) * T.sp * 0.75, JS.sin(@out) * T.sp * 0.75, 13)); }
				burst(piv[0] + dir * h * 0.5, H - 40, PINK, 20, 300); shake = Math.Max(shake, 12); hz.dead = true;
			}
		};
		hz.draw = t =>
		{
			if (t < tb) return; double a = ang(t); bool warn = t >= tf - wn && t < tf, act = t >= tf;
			if (warn) { double k = (t - (tf - wn)) / wn; g.fillStyle = $"rgba(255,42,109,{str(0.06 + 0.16 * k)})"; g.beginPath(); g.moveTo(piv[0], piv[1]); g.arc(piv[0], piv[1], h, dir > 0 ? -Math.PI / 2 : Math.PI, dir > 0 ? 0 : -Math.PI / 2); g.closePath(); g.fill(); }
			g.save(); g.translate(piv[0], piv[1]); g.rotate(a); double m = blocks(t);
			for (int k = 0; k < m; k++) { bool flick = warn && Math.Floor(t * 12) % 2 == 0; g.fillStyle = act ? PINK : flick ? "rgba(255,42,109,0.85)" : GREY + "0.55)"; if (act) { g.shadowColor = PINK; g.shadowBlur = 14; } g.fillRect(dir > 0 ? -bw : 0, -(k + 1) * bh + 3, bw, bh - 6); g.shadowBlur = 0; }
			g.restore();
		};
		hz.hit = (p, t) => { if (t < tf || t >= tf + fallD + lie) return false; double a = ang(t), c = JS.cos(a), s = JS.sin(a); double lx0 = -dir * bw / 2, ly0 = -h / 2; double cx = piv[0] + lx0 * c - ly0 * s, cy = piv[1] + lx0 * s + ly0 * c; return obbHit(p.x, p.y, p.r, cx, cy, bw / 2, h / 2, a); };
		return hz;
	}
	// ===== the lock and chain: "وعد مني وعد" =====
	public static void lockShape(double x, double y, double s) { g.save(); g.translate(x, y); g.lineWidth = s * 0.16; g.beginPath(); g.arc(0, -s * 0.2, s * 0.32, Math.PI, 0); g.stroke(); rr(-s * 0.48, -s * 0.2, s * 0.96, s * 0.75, s * 0.12); g.fill(); g.fillStyle = "#000"; circle(0, s * 0.12, s * 0.09); g.fillRect(-s * 0.04, s * 0.12, s * 0.08, s * 0.2); g.restore(); }
	public static Hz Lock(double x, double y, double tw, double ta, double te, double s = 56)
	{
		bool l = false;
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; if (!l && t >= ta) { l = true; shake = Math.Max(shake, 7); burst(x, y, PINK, 10, 220); } };
		h.draw = t => { double k = t < ta ? clamp((t - tw) / (ta - tw), 0, 1) : 1, a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = t < ta ? 0.25 + 0.45 * k : a; g.strokeStyle = PINK; g.fillStyle = PINK; if (t >= ta) { g.shadowColor = PINK; g.shadowBlur = 16; } lockShape(x, y - (t < ta ? (1 - k) * 80 : 0), s * (t < ta ? 1.3 - 0.3 * k : 1 + 0.08 * pulse)); g.shadowBlur = 0; g.globalAlpha = 1; };
		h.hit = (p, t) => t >= ta && t < te && hypot(p.x - x, p.y - y) < s * 0.5 + p.r;
		return h;
	}
	public static Hz Link(double ax, double ay, double bx, double by, double tw, double ta, double te)
	{
		double L = hypot(bx - ax, by - ay), ang = JS.atan2(by - ay, bx - ax);
		var h = new Hz();
		h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(ax, ay); g.rotate(ang);
			if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.5 * k)})"; g.lineWidth = 2; for (double d = 0; d < L; d += 34) g.strokeRect(d, -5, 22, 10); }
			else { double a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3), q = clamp((t - ta) / 0.12, 0, 1); g.globalAlpha = a; g.shadowColor = PINK; g.shadowBlur = 14; g.strokeStyle = PINK; g.lineWidth = 6; for (double d = 0; d < L * q; d += 34) { rr(d, -8, 26, 16, 7); g.stroke(); } g.globalAlpha = 1; }
			g.restore();
		};
		h.hit = (p, t) => { if (t < ta || t >= te) return false; double q = clamp((t - ta) / 0.12, 0, 1); return segDist(p.x, p.y, new[] { ax, ay }, new[] { ax + (bx - ax) * q, ay + (by - ay) * q }) < 9 + p.r; };   // 10-25: only as far as the chain has grown
		return h;
	}
	// ===== a stab in the back =====
	public static Hz BackStab(double x, double y, double tw, double ta, double sp, int n = 3)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t >= ta) { if (player != null) { double a0 = JS.atan2(player.y - y, player.x - x); for (int k = 0; k < n; k++) { double a = a0 + (k - (n - 1) / 2.0) * 0.2; hazards.Add(SqBullet(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 10)); } } burst(x, y, PINK, 8); h.dead = true; } };
		h.draw = t => { double k = clamp((t - tw) / (ta - tw), 0, 1); g.save(); g.translate(x, y); g.rotate(Math.PI / 4); g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.6 * k)})"; g.lineWidth = 3; double s = 26 - 12 * k; g.strokeRect(-s, -s, 2 * s, 2 * s); g.restore(); };
		return h;
	}
	// ===== the boss: the traitor friend =====
	class TDash { public double x0, y0, x1, y1, ta, te; }
	class TPt { public double x, y, t; }
	public static Hz Traitor(double ts, double turn, double te, Tier T)
	{
		double x = W / 2, y = H / 2; int lastS = -1; bool ex = false; double fx = 1, fy = 0; TDash dash = null; var trail = new List<TPt>(); const double S = 30;
		double[] behind() { if (player == null) return new[] { W / 2, H / 2 }; return new[] { clamp(player.x - fx * 330, 40, W - 40), clamp(player.y - fy * 330, 40, H - 40) }; }
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (player != null && hypot(player.fx, player.fy) > 0.1) { fx = player.fx; fy = player.fy; }
			if (dash != null && t >= dash.ta && t < dash.te) { double k = (t - dash.ta) / (dash.te - dash.ta); x = dash.x0 + (dash.x1 - dash.x0) * k; y = dash.y0 + (dash.y1 - dash.y0) * k; trail.Add(new TPt { x = x, y = y, t = t }); }
			else if (!(dash != null && t < dash.ta) && player != null) { var q = t < turn ? new[] { player.x - fx * 150, player.y - fy * 150 } : behind(); double tx = q[0], ty = q[1]; double sp = t < turn ? 2.2 : 1.4; x += (clamp(tx, 40, W - 40) - x) * Math.Min(1, dt * sp); y += (clamp(ty, 40, H - 40) - y) * Math.Min(1, dt * sp); }
			trail = trail.FindAll(q => t - q.t < 0.55);
			if (t >= te && !ex) { ex = true; for (int i = 0; i < 24; i++) { double a = i * TAU / 24; hazards.Add(SqBullet(x, y, JS.cos(a) * 220, JS.sin(a) * 220, 10)); } burst(x, y, PINK, 70, 520); burst(x, y, "#fff", 35, 420); shake = 26; flash = 0.7; rumble(500, 1, 1); h.dead = true; return; }
			if (t < turn + SPB) return;
			int s = (int)Math.Floor(BT(t) * 4 + 1e-6); if (s == lastS) return; lastS = s; int bar = (int)Math.Floor(s / 16.0), p = s % 16; if (t > te - SPB) return;
			if (p == 0) { for (int i = 0; i < 8; i++) { double a = bar * 0.4 + i * TAU / 8; hazards.Add(SqBullet(x, y, JS.cos(a) * T.sp * 0.8, JS.sin(a) * T.sp * 0.8, 9)); } }
			else if (p == 4 || p == 12) { var q = behind(); hazards.Add(BackStab(q[0], q[1], t, t + SPB * T.wn, T.sp, 3)); }
			if ((p == 8 || (p == 0 && bar % 2 == 1)) && player != null && !(dash != null && t < dash.te + 0.2)) { double a = JS.atan2(player.y - y, player.x - x), d = hypot(player.x - x, player.y - y) + 220; dash = new TDash { x0 = x, y0 = y, x1 = clamp(x + JS.cos(a) * d, 30, W - 30), y1 = clamp(y + JS.sin(a) * d, 30, H - 30), ta = t + SPB * T.wn, te = t + SPB * T.wn + 0.32 }; }
		};
		h.draw = t =>
		{
			bool pre = t < turn; double g0 = clamp((t - ts) / (2 * SPB), 0, 1); bool fl = t >= turn - SPB * 1.5 && t < turn && Math.Floor(t * 12) % 2 == 0;
			foreach (var q in trail) { double u = t - q.t, a = u < 0.4 ? 1 : 1 - (u - 0.4) / 0.15; g.globalAlpha = a * 0.75; g.fillStyle = PINK; g.fillRect(q.x - S * 0.6, q.y - S * 0.6, S * 1.2, S * 1.2); }
			g.globalAlpha = 1;
			if (dash != null && t < dash.ta) { g.strokeStyle = "rgba(255,42,109,0.6)"; g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); g.moveTo(dash.x0, dash.y0); g.lineTo(dash.x1, dash.y1); g.stroke(); g.setLineDash(new double[0]); }
			g.save(); g.translate(x, y); double s = S * (0.4 + 0.6 * g0) * (1 + 0.08 * pulse); g.rotate(pre ? JS.sin(t * 2) * 0.1 : t * 1.5);
			string col = pre && !fl ? GREY + "0.8)" : PINK; g.fillStyle = col; if (!pre || fl) { g.shadowColor = PINK; g.shadowBlur = 24; }
			g.fillRect(-s, -s, 2 * s, 2 * s); g.shadowBlur = 0;
			g.fillStyle = "#000"; if (pre && !fl) { circle(-s * 0.35, -s * 0.15, s * 0.13); circle(s * 0.35, -s * 0.15, s * 0.13); } else { g.beginPath(); g.moveTo(-s * 0.55, -s * 0.35); g.lineTo(-s * 0.15, -s * 0.1); g.lineTo(-s * 0.55, -s * 0.05); g.fill(); g.beginPath(); g.moveTo(s * 0.55, -s * 0.35); g.lineTo(s * 0.15, -s * 0.1); g.lineTo(s * 0.55, -s * 0.05); g.fill(); }
			g.restore();
		};
		h.hit = (p, t) => { if (t < turn) return false; if (hypot(p.x - x, p.y - y) < S + p.r - 2) return true; foreach (var q in trail) if (t - q.t < 0.4 && hypot(p.x - q.x, p.y - q.y) < S * 0.6 + p.r) return true; return false; };
		return h;
	}

	/* "وعد مني" (ixGha's pick). 96 bpm, the part from 0:30 to 2:00 of the song.
	   0 the people | 16 "game" | 32 "the loyal one betrayed" | 44 "building" | 56 the part repeats
	   72 checkpoint -> "وعد مني وعد" | 80 "I never trust" | 88 "I give no room" | 96 "everything has a limit"
	   112 checkpoint -> "not one" -> the traitor friend | 138 it is beaten */
	public static List<Ev> buildFriend2()
	{
		var E = new List<Ev>(); var R = mulberry32(1203);
		Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f });
		double rx(double a = 140, double b = W - 140) => a + R() * (b - a);
		double ry(double a = 120, double b = H - 120) => a + R() * (b - a);
		double ri(double n) => Math.Floor(R() * n);
		var TI = ezT(new[] { new Tier { sp = 265, wn = 1.5 }, new Tier { sp = 285, wn = 1.4 }, new Tier { sp = 300, wn = 1.35 } });
		void piece(double hit, string type, double i, double j, Tier T, double dur = 0.6) => ev(hit - T.wn, () => hazards.Add(ChessPiece(type, i, j, B(hit - T.wn), B(hit), B(hit + dur))));
		void pieceP(double hit, string type, Tier T, double dur = 0.6)
		{
			double oi = ri(5) - 2, oj = ri(3) - 1;
			ev(hit - T.wn, () => { double i = clamp(Math.Floor(((player != null ? player.x : W / 2) - BX) / CELL) + oi, 0, 9), j = clamp(Math.Floor((player != null ? player.y : H / 2) / CELL) + oj, 0, 5); hazards.Add(ChessPiece(type, i, j, B(hit - T.wn), B(hit), B(hit + dur))); });
		}
		void crowd(double b, int n, double tb0, double gap, string mode, Tier T, PersonO o = null)
		{
			double ang0 = R() * TAU;
			ev(b, () =>
			{
				for (int k = 0; k < n; k++)
				{
					double a = ang0 + k * TAU / n, px = clamp((player != null ? player.x : W / 2) + JS.cos(a) * 420, 30, W - 30), py = clamp((player != null ? player.y : H / 2) + JS.sin(a) * 300, 30, H - 30);
					var oo = new PersonO { follow = true, r = 150 + (k % 2) * 60 }; if (o != null) { if (o.r != null) oo.r = o.r; }   // Object.assign({follow,r}, o): o only ever sets r here
					hazards.Add(Person(px, py, B(b), B(tb0 + k * gap), mode, T, oo));
				}
			});
		}
		void tower(double x, int n, double b0, double stepB, double fallB, double dir, Tier T) => ev(b0, () => hazards.Add(Tower(x, n, B(b0), stepB * SPB, B(fallB), dir, T)));
		void @lock(double hit, double x, double y, Tier T, double dur = 6) => ev(hit - T.wn, () => hazards.Add(Lock(x, y, B(hit - T.wn), B(hit), B(hit + dur))));
		void link(double hit, double[] a, double[] b, Tier T, double dur) => ev(hit - T.wn, () => hazards.Add(Link(a[0], a[1], b[0], b[1], B(hit - T.wn), B(hit), B(hit + dur))));
		void squeeze(double hit, Tier T, double w, double h, double dur = 1.5, bool onP = true) => ev(hit - T.wn, () => { double cx = onP && player != null ? clamp(player.x, w / 2 + 10, W - w / 2 - 10) : W / 2, cy = onP && player != null ? clamp(player.y, h / 2 + 10, H - h / 2 - 10) : H / 2; hazards.Add(Squeeze(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, B(hit - T.wn), B(hit), B(hit + dur))); });
		void frame(double hit, Tier T, double w, double h) => ev(hit - T.wn, () =>
		{
			double cx = clamp(player != null ? player.x : W / 2, w / 2 + 20, W - w / 2 - 20), cy = clamp(player != null ? player.y : H / 2, h / 2 + 20, H - h / 2 - 20), tw = B(hit - T.wn), ta = B(hit), te = B(hit + 0.75);
			foreach (var y in new[] { cy - h / 2, cy + h / 2 }) hazards.Add(Laser(new LaserO { x = W / 2, y = y, ang = 0, w = 20, len = 3200, tw = tw, ta = ta, te = te }));
			foreach (var x in new[] { cx - w / 2, cx + w / 2 }) hazards.Add(Laser(new LaserO { x = x, y = H / 2, ang = Math.PI / 2, w = 20, len = 3200, tw = tw, ta = ta, te = te }));
		});
		void backstab(double hit, Tier T, int n = 3) => ev(hit - T.wn, () => { if (player == null) return; double fx = player.fx, fy = player.fy, x = clamp(player.x - fx * 320, 40, W - 40), y = clamp(player.y - fy * 320, 40, H - 40); hazards.Add(BackStab(x, y, B(hit - T.wn), B(hit), T.sp, n)); });

		/* ---------- part 1 ---------- */
		var T = TI[0];
		// 0:30 "طيبة قلبي": grey people walking with you in peace... one betrays
		crowd(0, 6, 99999, 0, "dash", T); E[E.Count - 1].keep = true;
		crowd(4, 3, 9, 2, "dash", T, new PersonO { r = 240 });
		// 0:40 "لعبت بحالي لعبة": the arena becomes a chess board
		ev(15, () => hazards.Add(Board(B(15), B(31))));
		piece(18, "R", 1, 1, T); piece(20, "B", 8, 4, T); piece(22, "N", 4, 2, T); piece(24, "R", 7, 1, T); pieceP(26, "B", T); piece(28, "Q", 5, 3, T, 0.5); pieceP(30, "N", T);
		// "صاحبت الوفي وخان": the people with you turn one after another
		crowd(32, 4, 36, 1.5, "dash", T); crowd(38, 3, 41, 1, "pop", T, new PersonO { r = 260 });
		// "شلون تعلي بنيان": towers build up and fall
		tower(W * 0.27, 5, 44, 0.5, 48, 1, T); tower(W * 0.73, 5, 48, 0.5, 52, -1, T); tower(W * 0.5, 6, 52, 0.5, 56, R() < 0.5 ? 1 : -1, T);
		// 0:58 the part repeats: game, betrayal and building together
		ev(56, () => hazards.Add(Board(B(56), B(66))));
		pieceP(58, "R", T); pieceP(60, "B", T); crowd(60, 3, 64, 1, "dash", T); pieceP(63, "N", T);
		tower(W * 0.15, 5, 64, 0.5, 68, 1, T); tower(W * 0.85, 5, 65, 0.5, 70, -1, T);
		/* ---------- part 2 (the chorus) ---------- */
		T = TI[1];
		ev(72, () => { flash = Math.Max(flash, 0.22); shake = Math.Max(shake, 9); });
		// "وعد مني وعد": locks lock and get tied with chains
		{ var P = new[] { new[] { 200.0, 170 }, new[] { W - 200, 170 }, new[] { W / 2, H - 130 } }; @lock(72, P[0][0], P[0][1], T, 8); @lock(73, P[1][0], P[1][1], T, 7); @lock(74, P[2][0], P[2][1], T, 6); link(75, P[0], P[1], T, 4); link(76, P[1], P[2], T, 3); link(77, P[2], P[0], T, 2); }
		// "بعد ما أثق أبد": everyone who comes close betrays right away
		crowd(80, 5, 82, 0.5, "dash", T, new PersonO { r = 200 }); crowd(84, 4, 86, 0.5, "pop", T, new PersonO { r = 280 });
		// "ولا أنطي مجال": the arena closes in
		squeeze(88, T, 520, 320, 1.5, false); squeeze(92, T, 420, 280, 1.5, true); @lock(90, W * 0.2, H * 0.5, T, 4); @lock(94, W * 0.8, H * 0.5, T, 4);
		// "كل شي وله حد": borders close around you
		frame(96, T, 360, 260); pieceP(98, "Q", T, 0.5); frame(100, T, 300, 220); crowd(100, 3, 103, 1, "dash", T);
		// the chorus again
		{ var P = new[] { new[] { W * 0.25, H * 0.3 }, new[] { W * 0.75, H * 0.7 } }; @lock(104, P[0][0], P[0][1], T, 6); @lock(105, P[1][0], P[1][1], T, 5); link(106, P[0], P[1], T, 3); }
		squeeze(108, T, 460, 300, 1.25, true); backstab(110, T);
		/* ---------- part 3 (the boss) ---------- */
		T = TI[2]; var TT = T;
		// 1:40 "ولا واحد": the people vanish one after another... one friend stays
		ev(110, () => { for (int k = 0; k < 8; k++) { double a = k * TAU / 8; hazards.Add(Person(W / 2 + JS.cos(a) * 330, H / 2 + JS.sin(a) * 230, B(110), 99999, "dash", TT, new PersonO { tx = W / 2 + JS.cos(a) * 300, ty = H / 2 + JS.sin(a) * 210, fade = B(112 + k * 0.75) })); } });
		ev(112, () => hazards.Add(Traitor(B(112), B(118), B(138), TT)));
		pieceP(114, "Q", T, 0.5); pieceP(116.5, "N", T, 0.5);
		foreach (var b in new double[] { 122, 124, 126, 130, 132, 134 }) pieceP(b, b % 4 == 2 ? "R" : "B", T, 0.5);
		squeeze(120, T, 600, 380, 1, true); squeeze(128, T, 520, 340, 1.25, true); crowd(118, 3, 121, 2, "dash", T, new PersonO { r = 260 }); backstab(126, T); backstab(131, T); backstab(136, T);
		sort(E, (a, b) => a.b - b.b); return E;
	}
}
