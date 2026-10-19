using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 4466-5163: level 8 in its AFK shape (the global buildLevel8 and its helpers),
// played only for v37/v38 replays (afkV 2). The option classes are in Level8.cs.
public static partial class G
{
	public static class L8A
	{
		/* ================= level 8 shapes: Hips Don't Lie (the Barranquilla carnival) ================= */
		// the cumbia step: three small steps (beats 1, 2 and 3) and a big kick on the 4th. Returns how far it walked up to beat bt
		static readonly double[] L8ST = { 1, 1, 1, 2.5 }; const double L8BAR = 5.5;
		static double L8step(double bt) { double n = Math.Floor(bt), f = bt - n; int i = (int)(((n % 4) + 4) % 4); double s = Math.Floor(n / 4) * L8BAR; for (int k = 0; k < i; k++) s += L8ST[k]; double e = clamp(f / 0.4, 0, 1); return s + L8ST[i] * (1 - JS.pow(1 - e, 3)); }
		// the hip shake: right and left with every beat
		static double L8hip(double bt) => JS.sin(Math.PI * bt);
		static double L8ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
		static bool L8hitC(Player p, double x, double y, double r) { double dx = p.x - x, dy = p.y - y, rr = r + p.r - 2; return dx * dx + dy * dy < rr * rr; }
		static void L8txt(string s, double x, double y, double size, string col) { g.font = $"{str(size)}px {TFONT}"; g.fillStyle = col; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, x, y); }
		// a coin: a pink circle with a white ring inside
		static void L8coin(double x, double y, double r, double a) { g.globalAlpha = a; g.fillStyle = PINK; circle(x, y, r); g.strokeStyle = "rgba(255,255,255,0.75)"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, r * 0.55, 0, TAU); g.stroke(); g.globalAlpha = 1; }
		static void L8ghost(double x, double y, double r, double a) { g.strokeStyle = $"rgba(255,42,109,{str(a)})"; g.lineWidth = 2; g.beginPath(); g.arc(x, y, r * 0.85, 0, TAU); g.stroke(); }
		// a flying coin (thrown, falls with gravity)
		static Hz L8Coin(double x, double y, double vx, double vy, double gr, double r = 11)
		{
			var h = new Hz { x = x, y = y, vx = vx, vy = vy, r = r }; double? px = null, py = null;
			h.update = (t, dt) => { px = h.x; py = h.y; h.vy += gr * dt; h.x += h.vx * dt; h.y += h.vy * dt; if (h.x < -60 || h.x > W + 60 || h.y > H + 60 || h.y < -700) h.dead = true; };
			h.draw = t => { g.fillStyle = "rgba(255,42,109,0.2)"; circle(h.x, h.y, h.r * 1.6); L8coin(h.x, h.y, h.r * (1 + 0.12 * pulse), 1); };
			// checks the distance to the line of movement between the two frames (a fast coin doesn't jump over the player)
			h.hit = (p, t) => { if (px == null) return L8hitC(p, h.x, h.y, h.r); double dx = h.x - px.Value, dy = h.y - py.Value, l = dx * dx + dy * dy, k = l != 0 ? clamp(((p.x - px.Value) * dx + (p.y - py.Value) * dy) / l, 0, 1) : 1; return L8hitC(p, px.Value + dx * k, py.Value + dy * k, h.r); };
			return h;
		}
		// a path through key points [beat, x, y, angle, size], swaying with the cumbia step
		static Func<double, double[]> L8path(double[][] KF, double bobA = 8) => t =>
		{
			double b = BT(t); int i = 0; while (i < KF.Length - 2 && b >= KF[i + 1][0]) i++; var A = KF[i]; var Bk = KF[i + 1]; double e = L8ease((b - A[0]) / L8or(Bk[0] - A[0], 1));
			double da = Bk[3] - A[3]; da = JS.atan2(JS.sin(da), JS.cos(da)); double bob = bobA * JS.sin(L8step(b) * Math.PI * 2 / L8BAR);
			double x = A[1] + (Bk[1] - A[1]) * e, y = A[2] + (Bk[2] - A[2]) * e, a = A[3] + da * e, s0 = A.Length > 4 ? L8or(A[4], 1) : 1, s1 = Bk.Length > 4 ? L8or(Bk[4], 1) : 1; return new[] { x - JS.cos(a) * bob, y - JS.sin(a) * bob, a, s0 + (s1 - s0) * e };
		};
		// a grey word (harmless)
		static Hz L8Word(string s, double x, double y, double size, double t0, double t1, string col)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t => { if (t < t0) return; double e = t - t0, a = min(1, e / 0.08, (t1 - t) / 0.3), sc = 1 + 0.35 * JS.exp(-e * 10); g.globalAlpha = Math.Max(0, a); g.save(); g.translate(x, y); g.scale(sc, sc); L8txt(s, 0, 0, size, col ?? GREY + "0.55)"); g.restore(); g.globalAlpha = 1; };
			return h;
		}
		// a ring of coins growing from a point (with spaces between the coins). tw: before it, the point pulses faintly
		static Hz L8Ring(double cx, double cy, double t0, double v, int n, double ph = 0, double? tw = null)
		{
			Func<double, double> R = t => 40 + v * (t - t0);
			var h = new Hz();
			h.update = (t, dt) => { if (t > t0 && R(t) > 1500) h.dead = true; };
			h.draw = t =>
			{
				if (tw != null && t < t0) { if (t < tw) return; double k = clamp((t - tw.Value) / (t0 - tw.Value), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.25 * k)})"; circle(cx, cy, 30 + 40 * k + 8 * pulse); return; }
				double r = R(t); for (int i = 0; i < n; i++) { double a = ph + TAU * i / n, x = cx + JS.cos(a) * r, y = cy + JS.sin(a) * r; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) continue; L8coin(x, y, 11 * (1 + 0.1 * pulse), 1); }
			};
			h.hit = (p, t) => { if (t < t0) return false; double r = R(t), d = hypot(p.x - cx, p.y - cy); if (Math.Abs(d - r) > 11 + p.r) return false; for (int i = 0; i < n; i++) { double a = ph + TAU * i / n; if (L8hitC(p, cx + JS.cos(a) * r, cy + JS.sin(a) * r, 11)) return true; } return false; };
			return h;
		}

		/* ---------------- backgrounds (grey, harmless) ---------------- */
		// phase 1: a carnival street, ropes of flags and lights, and two palm trees on the sides. lit: how many lights are on
		static Hz L8BG1(double t0, double t1, Func<double, double> lit)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				double a = min(1, (t - t0) / 0.6, (t1 - t) / 0.6); if (a <= 0) return; double b = BT(t), L = lit(b), kick = JS.exp(-((b % 1) + 1) % 1 * 5); g.globalAlpha = a;
				for (int s = 0; s < 2; s++)
				{
					double y0 = s != 0 ? 14 : 34, sag = s != 0 ? 78 : 52, sw = 6 * JS.sin(L8step(b) * Math.PI / L8BAR + s);
					g.strokeStyle = GREY + "0.25)"; g.lineWidth = 2; g.beginPath(); for (double x = -20; x <= W + 20; x += 40) { double q = (x - W / 2) / (W / 2 + 20), y = y0 + sag * (1 - q * q) + sw; if (x <= -20) g.moveTo(x, y); else g.lineTo(x, y); }
					g.stroke();
					for (double x = (s != 0 ? 0 : 32); x < W; x += 64)
					{
						double q = (x - W / 2) / (W / 2 + 20), y = y0 + sag * (1 - q * q) + sw, fl = JS.sin(b * Math.PI + x * 0.03) * 5;
						g.fillStyle = GREY + ((i32(x / 64)) % 2 != 0 ? "0.16)" : "0.24)"); g.beginPath(); g.moveTo(x - 14, y); g.lineTo(x + 14, y); g.lineTo(x + fl, y + 30); g.closePath(); g.fill();
						g.fillStyle = $"rgba(255,255,255,{str(0.08 + 0.35 * L * (0.5 + 0.5 * kick))})"; circle(x + 32, y + 2, 4 + 2 * L);
					}
				}
				for (int r = 0; r < 6; r++) { double x0 = r < 3 ? 24 + r * 34 : W - 24 - (r - 3) * 34; g.strokeStyle = GREY + (r % 2 != 0 ? "0.14)" : "0.2)"); g.lineWidth = 7; g.beginPath(); for (double y = 40; y <= H; y += 24) { double x = x0 + 12 * JS.sin(y / 40 + b * Math.PI / 2 + r); if (y == 40) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); }
				foreach (int sd in new[] { 0, 1 })
				{
					g.save(); g.translate(sd != 0 ? W - 70 : 70, H); g.scale(sd != 0 ? -1 : 1, 1); g.strokeStyle = GREY + "0.12)"; g.lineWidth = 16; g.beginPath(); g.moveTo(0, 0); g.quadraticCurveTo(20, -140, -10, -270); g.stroke();
					g.fillStyle = GREY + "0.1)"; for (int k = 0; k < 5; k++) { double an = -2.6 + k * 0.55 + 0.08 * JS.sin(b * Math.PI / 2 + k); g.beginPath(); g.ellipse(-10 + JS.cos(an) * 60, -270 + JS.sin(an) * 30, 70, 14, an, 0, TAU); g.fill(); }
					g.restore();
				}
				g.globalAlpha = 1;
			};
			return h;
		}
		// phase 2: every line of the song written big in grey behind. ws: [[start beat, end beat, word]]
		static Hz L8BG2((double b0, double b1, string s)[] ws, double t0, double t1)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				double b = BT(t);
				foreach (var (b0, b1, s) in ws)
				{
					if (b < b0 || b >= b1) continue; double e = t - B(b0), a = min(1, e / 0.15, (B(b1) - t) / 0.3), sc = 1 + 0.2 * JS.exp(-e * 8);
					g.globalAlpha = Math.Max(0, a); g.save(); g.translate(W / 2, H / 2); g.scale(sc, sc); g.font = $"{(s.Length > 9 ? 110 : 150)}px {TFONT}"; g.fillStyle = GREY + "0.1)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, 0, 0);
					g.strokeStyle = GREY + "0.18)"; g.lineWidth = 2; g.strokeText(s, 0, 0); g.restore(); g.globalAlpha = 1;
				}
			};
			return h;
		}
		// phase 3: the band's stage: speakers on the sides pulse with the kick, a light rig on top, and the stage floor
		static Hz L8BG3(double t0, double t1)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				double a = min(1, (t - t0) / 0.8, (t1 - t) / 0.6); if (a <= 0) return; double b = BT(t), ph = ((Math.Floor(b) % 4) + 4) % 4, kp = JS.exp(-(b - Math.Floor(b)) * 6) * (ph == 3 ? 1 : 0.45); g.globalAlpha = a;
				g.strokeStyle = GREY + "0.18)"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, H - 26); g.lineTo(W, H - 26); g.stroke();
				for (double x = 0; x < W; x += 80) { g.fillStyle = GREY + "0.05)"; g.fillRect(x, H - 26, 40, 26); }
				foreach (int sd in new[] { 0, 1 })
				{
					double x = sd != 0 ? W - 62 : 62;
					for (int k = 0; k < 3; k++)
					{
						double y = H - 90 - k * 120; g.fillStyle = GREY + "0.09)"; g.fillRect(x - 48, y - 56, 96, 112); g.strokeStyle = GREY + "0.2)"; g.lineWidth = 3; g.strokeRect(x - 48, y - 56, 96, 112);
						g.beginPath(); g.arc(x, y + 12, 26 + 6 * kp, 0, TAU); g.stroke(); g.beginPath(); g.arc(x, y - 34, 10 + 2 * kp, 0, TAU); g.stroke();
					}
				}
				g.fillStyle = GREY + "0.12)"; g.fillRect(0, 8, W, 10);
				for (double x = 120; x < W; x += 208)
				{
					g.fillStyle = GREY + "0.2)"; g.fillRect(x - 14, 18, 28, 22);
					double an = Math.PI / 2 + 0.35 * JS.sin(b * Math.PI / 4 + x); g.fillStyle = GREY + "0.06)"; g.beginPath(); g.moveTo(x, 40); g.lineTo(x + JS.cos(an - 0.12) * 900, 40 + JS.sin(an - 0.12) * 900); g.lineTo(x + JS.cos(an + 0.12) * 900, 40 + JS.sin(an + 0.12) * 900); g.closePath(); g.fill();
				}
				g.globalAlpha = 1;
			};
			return h;
		}
		// the parade street: the street lines walk towards the band. hor: the street is horizontal, dir the walking direction
		static Hz L8Street(bool hor, double dir, double t0, double t1)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				double a = min(1, (t - t0) / 0.4, (t1 - t) / 0.4); if (a <= 0) return; g.globalAlpha = a; double off = ((dir * 220 * (t - t0)) % 120 + 120) % 120; g.fillStyle = GREY + "0.14)";
				if (hor) { for (double x = -120 + off; x < W + 120; x += 120) { g.fillRect(x, H / 2 - 4, 60, 8); } g.fillStyle = GREY + "0.08)"; g.fillRect(0, 72, W, 6); g.fillRect(0, H - 78, W, 6); }
				else { for (double y = -120 + off; y < H + 120; y += 120) { g.fillRect(W / 2 - 4, y, 8, 60); } g.fillStyle = GREY + "0.08)"; g.fillRect(150, 0, 6, H); g.fillRect(W - 156, 0, 6, H); }
				g.globalAlpha = 1;
			};
			return h;
		}

		/* ---------------- phase 1: the Barranquilla carnival ---------------- */
		// a belt of coins the width of the screen with tassels under it. It unrolls left to right between ta and tu. y(t) its place
		// sh: the shake times, drop: the tassels drop coins
		static Hz L8Belt(L8BeltO o)
		{
			const double sp = 26, cr = 11, fs = 22; int n = (int)Math.Ceiling((W + 40) / sp) + 1;
			Func<double, double> xm = t => t < o.ta ? -60 : -20 + (W + 60) * L8ease((t - o.ta) / (o.tu - o.ta));
			Func<double, double, double> yb = (x, t) => o.y(t) + 16 * JS.sin(x / 140 - Math.PI * BT(t));
			Func<double, bool> inSh = t => { if (o.sh == null) return false; double b = BT(t); return o.sh.Any(s => b >= s[0] && b < s[1]); };
			Func<int, int> fl = i => i % 4 == 0 ? 4 : 2;
			Func<int, double, double, double> fx = (i, t, d) => (-20 + i * sp) + 12 * L8hip(BT(t)) * d / 90;
			bool dropped = false;
			var h = new Hz();
			h.update = (t, dt) =>
			{
				if (o.drop != null && !dropped && t >= B(o.drop.Value)) { dropped = true; for (int i = 0; i < n; i += 2) { double x = -20 + i * sp; if (x < 10 || x > W - 10) continue; double y = yb(x, t); for (int j = 1; j <= fl(i); j++) hazards.Add(L8Coin(fx(i, t, j * fs), y + j * fs, 0, -60 + j * 20, 650, 10)); } }
				if (t > o.te || o.y(t) < -160) h.dead = true;
			};
			h.draw = t =>
			{
				if (t < o.tw) return;
				if (t < o.ta)
				{
					double k = clamp((t - o.tw) / (o.ta - o.tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.4 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); g.beginPath(); for (double x = 0; x <= W; x += 40) { double y = yb(x, o.ta); if (x != 0) g.lineTo(x, y); else g.moveTo(x, y); }
					g.stroke(); g.setLineDash(new double[] { });
					g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.12 * k)})"; g.fillRect(0, o.y(o.ta) - 14, W, 4 * fs + 28); return;
				}
				double m = xm(t); bool sh = inSh(t); Func<int, double> jj = i => sh ? 4 * JS.sin(t * 45 + i * 1.3) : 0;
				g.strokeStyle = "rgba(255,42,109,0.18)"; g.lineWidth = cr * 3; g.lineCap = "round"; g.beginPath(); for (double x = -20; x <= m; x += 40) { double y = yb(x, t); if (x <= -20) g.moveTo(x, y); else g.lineTo(x, y); }
				g.stroke(); g.lineCap = "butt";
				for (int i = 0; i < n; i++)
				{
					double x = -20 + i * sp; if (x > m) break; double y = yb(x, t) + jj(i);
					if (!dropped && i % 2 == 0) { for (int j = 1; j <= fl(i); j++) L8coin(fx(i, t, j * fs) + jj(i + j), y + j * fs, 9, 1); }
					L8coin(x, y, cr, 1);
				}
			};
			h.hit = (p, t) =>
			{
				if (t < o.ta) return false; double m = xm(t); if (p.x > m + cr) return false; double y = yb(p.x, t); if (Math.Abs(p.y - y) < cr + p.r - 2) return true;
				if (dropped) return false; double dy = p.y - y; if (dy < 0 || dy > 4 * fs + cr) return false; int i0 = (int)round((p.x + 20) / sp);
				for (int i = i0 - 2; i <= i0 + 2; i++) { if (i < 0 || i % 2 != 0 || -20 + i * sp > m) continue; double L = fl(i) * fs; if (dy > L + 9 || dy < fs - 9) continue; if (Math.Abs(p.x - fx(i, t, clamp(dy, fs, L))) < 9 + p.r - 2) return true; }
				return false;
			};
			return h;
		}
		// "No fighting": two big coin balls rush at each other, bounce back just before they crash, and each ball lets go a ring of coins
		static Hz L8Balls(double y, double tw, double ta, double tb, double tc, double rv, int rn)
		{
			const double R = 64; double xs = -R - 40, xe = W / 2 - R - 34;
			Func<double, double> xl = t => t < ta ? xs : t < tb ? xs + (xe - xs) * (1 - JS.pow(1 - (t - ta) / (tb - ta), 3)) : xe - (xe + R + 240) * JS.pow(clamp((t - tb) / (tc - tb), 0, 1), 2);
			bool rung = false;
			var h = new Hz();
			h.update = (t, dt) => { if (!rung && t >= tb) { rung = true; double x = xl(t); hazards.Add(L8Ring(x, y, tb, rv, rn, 0)); hazards.Add(L8Ring(W - x, y, tb, rv, rn, Math.PI / rn)); burst(W / 2, y, "#fff", 16, 260); shake = Math.Max(shake, 8); } if (t > tc) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return;
				if (t < ta)
				{
					double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.14 * k)})"; g.fillRect(0, y - R, xe + R, 2 * R); g.fillRect(W - xe - R, y - R, xe + R, 2 * R);
					g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; foreach (int sd in new[] { 0, 1 }) for (int j = 0; j < 3; j++) { double ax = sd != 0 ? W - 60 - j * 46 : 60 + j * 46, d = sd != 0 ? -1 : 1; g.beginPath(); g.moveTo(ax + d * 16, y); g.lineTo(ax - d * 8, y - 16); g.lineTo(ax - d * 8, y + 16); g.closePath(); g.fill(); }
					return;
				}
				double x = xl(t);
				foreach (double xx in new[] { x, W - x })
				{
					g.fillStyle = "rgba(255,42,109,0.2)"; circle(xx, y, R + 12); g.fillStyle = PINK; circle(xx, y, R * (1 + 0.05 * pulse)); g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 4; g.beginPath(); g.arc(xx, y, R * 0.7, 0, TAU); g.stroke();
					g.fillStyle = "rgba(255,255,255,0.7)"; for (int i = 0; i < 8; i++) { double a = i * TAU / 8 + t * 2; circle(xx + JS.cos(a) * R * 0.42, y + JS.sin(a) * R * 0.42, 5); }
				}
			};
			h.hit = (p, t) => { if (t < ta) return false; double x = xl(t); return L8hitC(p, x, y, R) || L8hitC(p, W - x, y, R); };
			return h;
		}
		// a coin curtain: strings of coins hang from the top (side 0) or the bottom (side 1)
		// X(k,t) the string's place, len(k,t) its length, tilt(k,t) its lean, wave(k,t,d) the string vibrating, sh the shake times
		static Hz L8Curtain(L8CurtainO o)
		{
			int n = o.n; const double cr = 11, sp = 26;
			Func<int, double, double> L = (k, t) => Math.Max(0, o.len(k, t));
			Func<double, double> drop = t => t < o.ta ? 0 : t < o.ta + 0.3 ? 1 - JS.pow(1 - (t - o.ta) / 0.3, 3) : t < o.te ? 1 : Math.Max(0, 1 - (t - o.te) / 0.3);
			Func<double, bool> inSh = t => { if (o.sh == null) return false; double b = BT(t); return o.sh.Any(s => b >= s[0] && b < s[1]); };
			Func<int, double, double, bool, double> xd = (k, t, d, sh) => o.X(k, t) + (o.tilt != null ? o.tilt(k, t) * d : 0) + (o.wave != null ? o.wave(k, t, d) : 0) + (sh ? 4 * JS.sin(t * 45 + k * 1.7 + d * 0.05) : 0);
			Func<double, double> Y = d => o.side != 0 ? H - d : d;
			var done = new HashSet<int>();
			var h = new Hz();
			h.update = (t, dt) =>
			{
				if (o.toss != null) for (int j = 0; j < o.toss.Count; j++)
					{
						var q = o.toss[j]; if (done.Contains(j) || t < B(q.b)) continue; done.Add(j); double m = drop(t);
						for (int k = q.ph; k < n; k += q.ev) { double l = L(k, t) * m; if (l < 20) continue; double x = xd(k, t, l, false); if (x < 10 || x > W - 10) continue; hazards.Add(L8Coin(x, Y(l), q.vx * (k % 2 != 0 ? 1 : -1), q.vy, q.gr, 10)); }
					}
				if (t > o.te + 0.35) h.dead = true;
			};
			h.draw = t =>
			{
				if (t < o.tw) return;
				if (t < o.ta)
				{
					double a = 0.12 + 0.35 * clamp((t - o.tw) / (o.ta - o.tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(a)})"; g.lineWidth = 3; g.setLineDash(new double[] { 6, 8 }); g.beginPath();
					for (int k = 0; k < n; k++) { double l = L(k, o.ta); if (l < 4) continue; g.moveTo(xd(k, o.ta, 0, false), Y(0)); g.lineTo(xd(k, o.ta, l, false), Y(l)); }
					g.stroke(); g.setLineDash(new double[] { }); return;
				}
				double m = drop(t); bool sh = inSh(t);
				g.strokeStyle = "rgba(255,42,109,0.17)"; g.lineWidth = cr * 2.6; g.lineCap = "round"; g.beginPath();
				for (int k = 0; k < n; k++) { double l = L(k, t) * m; if (l < 4) continue; for (double d = 0; d <= l + 0.1; d += Math.Max(20, l / 6)) { double x = xd(k, t, d, false); if (d == 0) g.moveTo(x, Y(d)); else g.lineTo(x, Y(d)); } }
				g.stroke(); g.lineCap = "butt";
				for (int k = 0; k < n; k++) { double l = L(k, t) * m; for (double d = cr; d <= l; d += sp) { double x = xd(k, t, d, sh); if (x < -20 || x > W + 20) continue; L8coin(x, Y(d), cr, 1); } }
			};
			h.hit = (p, t) =>
			{
				if (t < o.ta || t >= o.te) return false; double m = drop(t), d = o.side != 0 ? H - p.y : p.y; if (d < -cr) return false;
				for (int k = 0; k < n; k++) { double l = L(k, t) * m; if (l < cr || d > l + cr - 2) continue; if (Math.Abs(p.x - xd(k, t, clamp(d, 0, l), false)) < cr + p.r - 2) return true; }
				return false;
			};
			return h;
		}
		// a big upside-down Spanish mark (¿ or ¡) falls from the top and flips
		static Hz L8Glyph(string ch, double x, double size, double tw, double ta, double tb)
		{
			double hw = size * 0.17, hh = size * 0.4, y0 = -size * 0.5, y1 = H + size * 0.5;
			Func<double, double> y = t => y0 + (y1 - y0) * clamp((t - ta) / (tb - ta), 0, 1), fl = t => JS.cos(Math.PI * 3 * clamp((t - ta) / (tb - ta), 0, 1));
			var h = new Hz();
			h.update = (t, dt) => { if (t > tb) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return; g.save(); g.translate(x, t < ta ? size * 0.45 : y(t)); g.font = $"{str(size)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
				if (t < ta)
				{
					double k = clamp((t - tw) / (ta - tw), 0, 1); g.restore(); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.3 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 8, 10 }); g.beginPath(); g.moveTo(x, 0); g.lineTo(x, H); g.stroke(); g.setLineDash(new double[] { });
					g.save(); g.translate(x, size * 0.45); g.fillStyle = $"rgba(255,42,109,{str(0.1 + 0.25 * k)})"; g.font = $"{str(size)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.fillText(ch, 0, 0); g.restore(); return;
				}
				double f = fl(t); g.scale(1, f); g.fillStyle = "rgba(255,42,109,0.22)"; g.fillRect(-hw - 8, -hh - 8, 2 * hw + 16, 2 * hh + 16); g.fillStyle = PINK; g.fillText(ch, 0, size * 0.04); g.restore();
			};
			h.hit = (p, t) => { if (t < ta) return false; return Math.Abs(p.x - x) < hw + p.r - 2 && Math.Abs(p.y - y(t)) < hh * Math.Max(0.3, Math.Abs(fl(t))) + p.r - 2; };
			return h;
		}
		// a Spanish fan (abanico): ribs of coins from one point, opening and turning. spread(t) how open it is, rot(t) its direction
		static Hz L8Fan(L8FanO o)
		{
			const double cr = 11, sp = 28;
			Func<double, List<double>> ribs = t => { double s = o.spread(t), r = o.rot(t); var a = new List<double>(); for (int i = 0; i < o.n; i++) a.Add(r + ((double)i / (o.n - 1) - 0.5) * s); return a; };
			Func<double, double> fade = t => t < o.te ? 1 : Math.Max(0, 1 - (t - o.te) / 0.3);
			var h = new Hz();
			h.update = (t, dt) => { if (t > o.te + 0.3) h.dead = true; };
			h.draw = t =>
			{
				if (t < o.tw) return; double cx = o.cx, cy = o.cy, R = o.R, r0 = o.r0;
				if (t < o.ta)
				{
					double k = clamp((t - o.tw) / (o.ta - o.tw), 0, 1); var A0 = ribs(o.tf); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, R, A0[0], A0[A0.Count - 1]); g.closePath(); g.fill();
					g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.35 * k)})"; g.lineWidth = 2; g.beginPath(); foreach (var a in A0) { g.moveTo(cx + JS.cos(a) * r0, cy + JS.sin(a) * r0); g.lineTo(cx + JS.cos(a) * R, cy + JS.sin(a) * R); }
					g.stroke(); return;
				}
				var A = ribs(t); double f = fade(t); g.globalAlpha = f;
				for (int i = 0; i < A.Count - 1; i++) { g.fillStyle = GREY + (i % 2 != 0 ? "0.07)" : "0.13)"); g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, R + 10, A[i], A[i + 1]); g.closePath(); g.fill(); }
				g.strokeStyle = GREY + "0.3)"; g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, R + 10, A[0], A[A.Count - 1]); g.stroke();
				foreach (var a in A)
				{
					double c = JS.cos(a), s = JS.sin(a); g.strokeStyle = "rgba(255,42,109,0.18)"; g.lineWidth = cr * 2.6; g.beginPath(); g.moveTo(cx + c * r0, cy + s * r0); g.lineTo(cx + c * R, cy + s * R); g.stroke();
					for (double r = r0; r <= R; r += sp) { double x = cx + c * r, y = cy + s * r; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) continue; L8coin(x, y, cr, f); }
				}
				// the fan's hub is pink and hurts (so the middle is no safe place)
				g.fillStyle = "rgba(255,42,109,0.25)"; circle(cx, cy, 44); g.fillStyle = PINK; circle(cx, cy, 34 * (1 + 0.06 * pulse)); g.fillStyle = GREY + "0.6)"; circle(cx, cy, 12); g.globalAlpha = 1;
			};
			h.hit = (p, t) => { if (t < o.ta || t >= o.te) return false; double dx = p.x - o.cx, dy = p.y - o.cy; if (L8hitC(p, o.cx, o.cy, 34)) return true; foreach (var a in ribs(t)) { double c = JS.cos(a), s = JS.sin(a), pr = dx * c + dy * s; if (pr < o.r0 - cr || pr > o.R + cr) continue; if (Math.Abs(-dx * s + dy * c) < cr + p.r - 2) return true; } return false; };
			return h;
		}
		// a giant grey dancer (harmless) with a belt of pink coins round her waist (it hurts). x(t) her place, spin(t) the belt's turn
		// throws: the belt throws coins [{b,v,ev,gr}], rip: the belt lets go growing oval rings [beats]
		static Hz L8Dancer(L8DancerO o)
		{
			double s = o.s;
			Func<double, double> hipX = t => o.x(t) + o.A * L8hip(BT(t)); double hipY = o.y + 40 * s;
			Func<int, double, double[]> bc = (i, t) => { double a = TAU * i / o.n + o.spin(t); return new[] { hipX(t) + o.rx * JS.cos(a), hipY + o.ry * JS.sin(a), JS.sin(a), a }; };
			Func<double, double> al = t => min(1, (t - o.t0) / 0.5, (o.t1 - t) / 0.5);
			var done = new HashSet<string>();
			var h = new Hz();
			h.update = (t, dt) =>
			{
				if (o.throws != null) for (int j = 0; j < o.throws.Count; j++)
					{
						var q = o.throws[j]; if (done.Contains("" + j) || t < B(q.b)) continue; done.Add("" + j);
						for (int i = q.ph; i < o.n; i += q.ev) { var P = bc(i, t); double x = P[0], y = P[1], a = P[3]; hazards.Add(L8Coin(x, y, JS.cos(a) * q.v, JS.sin(a) * q.v * 0.8 - q.up, q.gr, 10)); }
					}
				if (o.rip != null) for (int j = 0; j < o.rip.Length; j++) { double b = o.rip[j]; if (done.Contains("r" + j) || t < B(b)) continue; done.Add("r" + j); hazards.Add(L8Oval(hipX(t), hipY, o.rx, o.ry, t, o.rv, 28, j * 0.11)); }
				if (t > o.t1) h.dead = true;
			};
			h.draw = t =>
			{
				double a = al(t); if (a <= 0) return; double b = BT(t), x = o.x(t), hx = hipX(t), y = o.y, sw = hx - x, arm = JS.sin(b * Math.PI / 2); g.globalAlpha = Math.Max(0, a);
				for (int i = 0; i < o.n; i++) { var P = bc(i, t); if (P[2] < 0) L8coin(P[0], P[1], 11, a * 0.8); }
				g.lineCap = "round"; g.strokeStyle = GREY + "0.2)"; g.lineWidth = 34 * s; g.beginPath(); g.moveTo(hx - 28 * s, y + 70 * s); g.lineTo(x - 62 * s, y + 290 * s); g.moveTo(hx + 28 * s, y + 70 * s); bool kick = ((Math.Floor(b) % 4) + 4) % 4 == 3 && b % 1 < 0.5; g.lineTo(kick ? x + 130 * s : x + 62 * s, kick ? y + 250 * s : y + 290 * s); g.stroke();
				g.lineWidth = 22 * s; g.beginPath(); g.moveTo(x + sw * 0.2 - 62 * s, y - 180 * s); g.lineTo(x - 130 * s, y - 310 * s + 30 * s * arm); g.moveTo(x + sw * 0.2 + 62 * s, y - 180 * s); g.lineTo(x + 130 * s, y - 310 * s - 30 * s * arm); g.stroke(); g.lineCap = "butt";
				g.fillStyle = GREY + "0.18)"; g.beginPath(); g.moveTo(x + sw * 0.2 - 72 * s, y - 190 * s); g.lineTo(x + sw * 0.2 + 72 * s, y - 190 * s); g.lineTo(x + sw * 0.5 + 42 * s, y - 10 * s); g.lineTo(x + sw * 0.5 - 42 * s, y - 10 * s); g.closePath(); g.fill();
				g.fillStyle = GREY + "0.22)"; g.beginPath(); g.ellipse(hx, y + 40 * s, 88 * s, 58 * s, 0, 0, TAU); g.fill();
				g.fillStyle = GREY + "0.14)"; g.beginPath(); g.ellipse(x + sw * 0.1 - 14 * s, y - 200 * s, 46 * s, 92 * s, 0.15 * JS.sin(b * Math.PI), 0, TAU); g.fill();
				g.fillStyle = GREY + "0.24)"; circle(x + sw * 0.12, y - 250 * s, 40 * s);
				for (int i = 0; i < o.n; i++) { var P = bc(i, t); if (P[2] >= 0) L8coin(P[0], P[1], 11, a); }
				g.globalAlpha = 1;
			};
			h.hit = (p, t) => { if (al(t) < 0.7) return false; for (int i = 0; i < o.n; i++) { var P = bc(i, t); if (L8hitC(p, P[0], P[1], 11)) return true; } return false; };
			return h;
		}
		// an oval ring of coins growing from the dancer's waist
		static Hz L8Oval(double cx, double cy, double rx, double ry, double t0, double v, int n, double ph)
		{
			Func<double, double> k = t => 1 + v * (t - t0) / rx;
			var h = new Hz();
			h.update = (t, dt) => { if (rx * k(t) > 1500) h.dead = true; };
			h.draw = t => { double m = k(t); for (int i = 0; i < n; i++) { double a = ph + TAU * i / n, x = cx + JS.cos(a) * rx * m, y = cy + JS.sin(a) * ry * m; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) continue; L8coin(x, y, 11, 1); } };
			h.hit = (p, t) => { double m = k(t); for (int i = 0; i < n; i++) { double a = ph + TAU * i / n; if (L8hitC(p, cx + JS.cos(a) * rx * m, cy + JS.sin(a) * ry * m, 11)) return true; } return false; };
			return h;
		}
		// a big footprint stamps on the beat (shown faint before). ang the direction of its toes, mir the left foot
		static Hz L8Foot(double x, double y, double ang, double s, double tw, double ta, double te, bool mir)
		{
			double c = JS.cos(ang), sn = JS.sin(ang), m = mir ? -1 : 1;
			var parts = new[] { new[] { 0.12, 0, 0.3, 0.21 }, new[] { -0.3, 0.02 * m, 0.19, 0.16 } };
			var toes = new[] { new[] { 0.5, -0.13 * m, 0.075 }, new[] { 0.52, -0.03 * m, 0.06 }, new[] { 0.5, 0.06 * m, 0.055 }, new[] { 0.46, 0.13 * m, 0.05 }, new[] { 0.4, 0.19 * m, 0.045 } };
			Action shape = () => { g.beginPath(); foreach (var q in parts) { double px = q[0], py = q[1], rx = q[2], ry = q[3]; g.moveTo((px + rx) * s, py * s); g.ellipse(px * s, py * s, rx * s, ry * s, 0, 0, TAU); } foreach (var q in toes) { double px = q[0], py = q[1], r = q[2]; g.moveTo((px + r) * s, py * s); g.arc(px * s, py * s, r * s, 0, TAU); } };
			var h = new Hz();
			h.update = (t, dt) => { if (t > te + 0.35) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return; g.save(); g.translate(x, y); g.rotate(ang);
				if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); shape(); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.15 * k)})"; g.fill(); g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); g.stroke(); g.setLineDash(new double[] { }); }
				else { double e = t - ta, sc = 1 + 0.25 * JS.exp(-e * 20), a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.35); g.globalAlpha = a; g.scale(sc, sc); shape(); g.fillStyle = e < 0.06 ? "#fff" : PINK; g.fill(); g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 3; g.stroke(); g.globalAlpha = 1; }
				g.restore();
			};
			h.hit = (p, t) =>
			{
				if (t < ta || t >= te) return false; double dx = p.x - x, dy = p.y - y, lx = (dx * c + dy * sn) / s, ly = (-dx * sn + dy * c) / s, pr = (p.r - 2) / s;
				foreach (var q in parts) { double px = q[0], py = q[1], rx = q[2], ry = q[3]; double u = (lx - px) / (rx + pr), v = (ly - py) / (ry + pr); if (u * u + v * v < 1) return true; }
				foreach (var q in toes) { double px = q[0], py = q[1], r = q[2]; double u = lx - px, v = ly - py; if (u * u + v * v < (r + pr) * (r + pr)) return true; }
				return false;
			};
			return h;
		}

		/* ---------------- phase 2: the words become real ---------------- */
		// Spanish words walk in a row like a train. Every letter is a box that hurts, and the spaces between words are passages. y(t) the row's place
		static Hz L8Train(L8TrainO o)
		{
			double adv = o.size * 0.72, hw = o.size * 0.3, hh = o.size * 0.37; int N = o.s.Length; double len = N * adv;
			Func<double, double> bx = t => o.x0 + o.dir * o.v * (t - o.ta);
			var h = new Hz();
			h.update = (t, dt) => { double x = bx(t); if (t > o.te + 0.3 || (o.dir > 0 && x > W + 40) || (o.dir < 0 && x + len < -40)) h.dead = true; };
			h.draw = t =>
			{
				if (t < o.tw) return; double y = o.y(t), fade = t < o.te ? 1 : Math.Max(0, 1 - (t - o.te) / 0.3);
				if (t < o.ta + 0.6)
				{
					double k = t < o.ta ? clamp((t - o.tw) / (o.ta - o.tw), 0, 1) : 1 - (t - o.ta) / 0.6; g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.1 * k)})"; g.fillRect(0, y - hh, W, 2 * hh);
					g.fillStyle = $"rgba(255,42,109,{str(0.2 + 0.3 * k)})"; for (int j = 0; j < 4; j++) { double ax = o.dir > 0 ? 40 + j * 40 : W - 40 - j * 40; g.beginPath(); g.moveTo(ax + o.dir * 14, y); g.lineTo(ax - o.dir * 8, y - 14); g.lineTo(ax - o.dir * 8, y + 14); g.closePath(); g.fill(); }
				}
				if (t < o.ta) return; double x0 = bx(t); g.globalAlpha = fade; g.font = $"{str(o.size)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
				for (int i = 0; i < N; i++) { char ch = o.s[i]; if (ch == ' ') continue; double cx = x0 + i * adv + adv / 2; if (cx < -60 || cx > W + 60) continue; g.fillStyle = "rgba(255,42,109,0.2)"; g.fillRect(cx - hw - 5, y - hh - 5, 2 * hw + 10, 2 * hh + 10); g.fillStyle = PINK; g.fillText(ch.ToString(), cx, y + o.size * 0.04); }
				g.globalAlpha = 1;
			};
			h.hit = (p, t) =>
			{
				if (t < o.ta || t >= o.te) return false; double y = o.y(t); if (Math.Abs(p.y - y) >= hh + p.r - 2) return false; double x0 = bx(t), i = Math.Floor((p.x - x0) / adv);
				for (double j = i - 1; j <= i + 1; j++) { if (j < 0 || j >= N || o.s[(int)j] == ' ') continue; if (Math.Abs(p.x - (x0 + j * adv + adv / 2)) < hw + p.r - 2) return true; }
				return false;
			};
			return h;
		}
		// "mi casa, su casa": two houses. During the flood everything outside the safe house hurts. The safe house glows white before the flood
		// fl: [{tw,ta,te,s}] s the number of the safe house
		static Hz L8Houses(L8House[] hs, double t0, double t1, List<L8Flood> fl)
		{
			Action<L8House> shape = (hh) => { double x = hh.x, y = hh.y; g.moveTo(x - 95, y - 40); g.lineTo(x, y - 125); g.lineTo(x + 95, y - 40); g.lineTo(x + 95, y + 110); g.lineTo(x - 95, y + 110); g.closePath(); };
			Func<L8House, Player, bool> inside = (hh, p) => { double x = p.x - hh.x, y = p.y - hh.y, m = p.r - 1; if (Math.Abs(x) > 95 - m || y > 110 - m) return false; if (y >= -40 + m) return true; double roofY = -125 + (Math.Abs(x) / 95) * 85; return y > roofY + m * 1.4; };
			// the running flood first, then the next one's warning, then the one fading out
			Func<double, L8Flood> cur = t => { foreach (var f in fl) if (t >= f.ta && t < f.te) return f; foreach (var f in fl) if (t >= f.tw && t < f.ta) return f; foreach (var f in fl) if (t >= f.te && t < f.te + 0.25) return f; return null; };
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				if (t < t0) return; double ap = min(1, (t - t0) / 0.4, (t1 - t) / 0.4); if (ap <= 0) return; var f = cur(t);
				if (f != null)
				{
					g.beginPath(); g.rect(0, 0, W, H); shape(hs[f.s]);
					if (t < f.ta) { double k = clamp((t - f.tw) / (f.ta - f.tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.2 * k)})"; g.fill("evenodd"); }
					else if (t < f.te)
					{
						g.fillStyle = t - f.ta < 0.06 ? "#fff" : PINK; g.globalAlpha = 0.92; g.fill("evenodd"); g.globalAlpha = 1;
						g.fillStyle = "rgba(255,255,255,0.35)"; double ph = (t - f.ta) * 600; for (double y = -40; y < H + 40; y += 60) for (double x = -40 + ((y / 60) % 2) * 30; x < W + 40; x += 60) { double xx = x + ph % 60; circle(xx, y, 6); }
					}
					else { g.globalAlpha = Math.Max(0, 1 - (t - f.te) / 0.25) * 0.9; g.fillStyle = PINK; g.fill("evenodd"); g.globalAlpha = 1; }
				}
				g.globalAlpha = ap;
				// Easy: during the flood, the next house is marked with a dashed white line
				var nx = easy && f != null && t >= f.ta ? fl.FirstOrDefault(q => q != f && t >= q.tw && t < q.ta) : null;
				for (int i = 0; i < hs.Length; i++)
				{
					var hh = hs[i]; bool safe = f != null && f.s == i; g.beginPath(); shape(hh); g.strokeStyle = safe ? "rgba(255,255,255,0.95)" : GREY + "0.55)"; g.lineWidth = safe ? 5 : 3; g.stroke();
					if (nx != null && nx.s == i && !safe) { g.setLineDash(new double[] { 12, 9 }); g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 4; g.beginPath(); shape(hh); g.stroke(); g.setLineDash(new double[] { }); }
					g.fillStyle = GREY + "0.35)"; g.fillRect(hh.x - 16, hh.y + 62, 32, 48); g.fillStyle = GREY + "0.25)"; g.fillRect(hh.x - 70, hh.y - 20, 30, 26); g.fillRect(hh.x + 40, hh.y - 20, 30, 26);
					L8txt(hh.label, hh.x, hh.y + 26, 22, safe ? "rgba(255,255,255,0.95)" : GREY + "0.6)");
				}
				g.globalAlpha = 1;
			};
			h.hit = (p, t) => { foreach (var f in fl) if (t >= f.ta && t < f.te) return !inside(hs[f.s], p); return false; };
			return h;
		}
		// a letter of "SHAKIRA": shown faint before (and if it has from, it flies faintly from there), then stamps on the beat, and last breaks into coins
		static Hz L8Letter(string ch, double x, double y, double size, double tw, double ta, double te, double[] from)
		{
			double hw = size * 0.3, hh = size * 0.38; bool broke = false;
			var h = new Hz();
			h.update = (t, dt) => { if (!broke && t >= te) { broke = true; for (int i = 0; i < 5; i++) { double a = -Math.PI / 2 + (i - 2) * 0.5; hazards.Add(L8Coin(x + (i - 2) * hw * 0.4, y, JS.cos(a) * 140, JS.sin(a) * 260, 700, 10)); } } if (t > te + 0.3) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return; g.save();
				if (t < ta)
				{
					double k = clamp((t - tw) / (ta - tw), 0, 1), e = L8ease(k * 1.4); double px = x, py = y; if (from != null) { px = from[0] + (x - from[0]) * e; py = from[1] + (y - from[1]) * e; }
					g.translate(px, py); g.font = $"{str(size)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
					g.strokeStyle = $"rgba(255,42,109,{str(0.25 + 0.5 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 }); g.strokeRect(-hw, -hh, 2 * hw, 2 * hh); g.setLineDash(new double[] { }); g.fillStyle = $"rgba(255,42,109,{str(0.08 + 0.18 * k)})"; g.fillText(ch, 0, size * 0.04);
				}
				else { g.translate(x, y); g.font = $"{str(size)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; double e = t - ta, sc = 1 + 0.5 * JS.exp(-e * 22), a = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3); g.globalAlpha = a; g.scale(sc, sc); g.fillStyle = "rgba(255,42,109,0.25)"; g.fillRect(-hw - 6, -hh - 6, 2 * hw + 12, 2 * hh + 12); g.fillStyle = e < 0.06 ? "#fff" : PINK; g.fillText(ch, 0, size * 0.04); g.globalAlpha = 1; }
				g.restore();
			};
			h.hit = (p, t) => t >= ta && t < te && Math.Abs(p.x - x) < hw + p.r - 2 && Math.Abs(p.y - y) < hh + p.r - 2;
			return h;
		}
		// big grey lips (harmless) that open with every word. talk: the beats of the words
		static Hz L8Lips(int sd, double y, double t0, double t1, double[] talk, Func<double, double> spin = null)
		{
			double x = sd != 0 ? W - 58 : 58;
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				double a = min(1, (t - t0) / 0.3, (t1 - t) / 0.3); if (a <= 0) return; double b = BT(t); double op = 0; foreach (var tb in talk) if (b >= tb) op = Math.Max(op, JS.exp(-(b - tb) * 3));
				g.globalAlpha = a; g.save(); g.translate(x, y); g.rotate(spin != null ? spin(t) : 0); double w = 70, hh = 16 + 30 * op;
				g.fillStyle = GREY + "0.4)"; g.beginPath(); g.moveTo(-w, 0); g.quadraticCurveTo(-w * 0.5, -hh - 10, -6, -hh + 2); g.quadraticCurveTo(0, -hh - 4, 6, -hh + 2); g.quadraticCurveTo(w * 0.5, -hh - 10, w, 0); g.quadraticCurveTo(0, -hh * 0.35, -w, 0); g.fill();
				g.beginPath(); g.moveTo(-w, 0); g.quadraticCurveTo(0, hh * 0.35, w, 0); g.quadraticCurveTo(w * 0.4, hh + 16, 0, hh + 14); g.quadraticCurveTo(-w * 0.4, hh + 16, -w, 0); g.fill();
				g.fillStyle = GREY + "0.08)"; g.beginPath(); g.ellipse(0, 0, w * 0.8, hh * 0.35 + 2, 0, 0, TAU); g.fill(); g.restore(); g.globalAlpha = 1;
			};
			return h;
		}
		// the "go mad" whirl: arms of coins turning round the middle, every arm with an empty part. R(t) their length, rot(t) their turn
		static Hz L8Spin(L8SpinO o)
		{
			const double cr = 11, sp = 28;
			Func<int, double, bool> ok = (i, r) => { var gp = o.gaps[i % o.gaps.Length]; return !(r > gp[0] && r < gp[1]); };
			var h = new Hz();
			h.update = (t, dt) => { if (t > o.te + 0.3) h.dead = true; };
			h.draw = t =>
			{
				if (t < o.tw) return; double cx = o.cx, cy = o.cy;
				if (t < o.ta)
				{
					double k = clamp((t - o.tw) / (o.ta - o.tw), 0, 1), R0 = o.R(o.ta + 0.5), rr0 = o.rot(o.ta); g.strokeStyle = $"rgba(255,42,109,{str(0.15 + 0.35 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 }); g.beginPath();
					for (int i = 0; i < o.arms; i++) { double a = rr0 + TAU * i / o.arms; g.moveTo(cx, cy); g.lineTo(cx + JS.cos(a) * R0, cy + JS.sin(a) * R0); }
					g.stroke(); g.setLineDash(new double[] { }); return;
				}
				double R = o.R(t), r0 = o.rot(t), f = t < o.te ? 1 : Math.Max(0, 1 - (t - o.te) / 0.3);
				for (int i = 0; i < o.arms; i++) { double a = r0 + TAU * i / o.arms, c = JS.cos(a), s = JS.sin(a); for (double r = 34; r <= R; r += sp) { if (!ok(i, r)) continue; double x = cx + c * r, y = cy + s * r; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) continue; L8coin(x, y, cr, f); } }
				g.globalAlpha = f; g.fillStyle = PINK; circle(cx, cy, 26); g.strokeStyle = "rgba(255,255,255,0.8)"; g.lineWidth = 4; g.beginPath(); g.arc(cx, cy, 15, r0, r0 + 4.5); g.stroke(); g.globalAlpha = 1;
			};
			h.hit = (p, t) =>
			{
				if (t < o.ta || t >= o.te) return false; double dx = p.x - o.cx, dy = p.y - o.cy, d = hypot(dx, dy); if (d < 26 + p.r - 2) return true; double R = o.R(t), r0 = o.rot(t); if (d > R + cr) return false;
				for (int i = 0; i < o.arms; i++) { double a = r0 + TAU * i / o.arms, c = JS.cos(a), s = JS.sin(a), pr = dx * c + dy * s; if (pr < 20 || pr > R + cr || !ok(i, pr)) continue; if (Math.Abs(-dx * s + dy * c) < cr + p.r - 2) return true; }
				return false;
			};
			return h;
		}
		// "reading the signs": arrow signs come up one after another on top like a sentence, then each one is carried out in order
		// the arrow points at the safe half, and the other half hurts. d: 0 right, 1 down, 2 left, 3 up
		static Hz L8Signs(int[] ds, double[] apps, double[] tas, double dur)
		{
			int n = ds.Length; double wn = easy ? 1.1 : 0.75; Func<int, double[]> reg = d => d == 0 ? new[] { 0, 0, W / 2, H } : d == 2 ? new[] { W / 2, 0, W / 2, H } : d == 1 ? new[] { 0, 0, W, H / 2 } : new[] { 0, H / 2, W, H / 2 };
			var h = new Hz();
			h.update = (t, dt) => { if (t > tas[n - 1] + dur + 0.5) h.dead = true; };
			h.draw = t =>
			{
				if (t < apps[0]) return;
				for (int i = 0; i < n; i++)
				{
					double ta = tas[i]; if (t < Math.Max(apps[i], ta - wn) || t > ta + dur + 0.3) continue; var rg = reg(ds[i]); double rx = rg[0], ry = rg[1], rw = rg[2], rh = rg[3];
					if (t < ta) { double k = clamp((t - Math.Max(apps[i], ta - wn)) / wn, 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.05 + 0.2 * k)})"; g.fillRect(rx, ry, rw, rh); }
					else { double a = t < ta + dur ? 1 : Math.Max(0, 1 - (t - ta - dur) / 0.3); g.globalAlpha = a * 0.92; g.fillStyle = t - ta < 0.06 ? "#fff" : PINK; g.fillRect(rx, ry, rw, rh); g.globalAlpha = 1; }
				}
				int curI = n; for (int i = 0; i < n; i++) if (t < tas[i] + dur) { curI = i; break; }
				for (int i = 0; i < n; i++)
				{
					if (t < apps[i]) continue; double e = t - apps[i], sc = (i == curI ? 1.15 : 0.9) * (1 + 0.4 * JS.exp(-e * 12)), a = i < curI ? 0.3 : 1; double sx = W / 2 + (i - (n - 1) / 2.0) * 136, sy = 74;
					g.globalAlpha = a * Math.Min(1, e / 0.15); g.save(); g.translate(sx, sy); g.scale(sc, sc); g.fillStyle = GREY + "0.2)"; g.strokeStyle = i == curI ? "rgba(255,255,255,0.9)" : GREY + "0.7)"; g.lineWidth = 4; g.fillRect(-52, -36, 104, 72); g.strokeRect(-52, -36, 104, 72);
					g.rotate(new[] { 0, Math.PI / 2, Math.PI, -Math.PI / 2 }[ds[i]]); g.fillStyle = "rgba(255,255,255,0.9)"; g.beginPath(); g.moveTo(34, 0); g.lineTo(6, -24); g.lineTo(6, -9); g.lineTo(-32, -9); g.lineTo(-32, 9); g.lineTo(6, 9); g.lineTo(6, 24); g.closePath(); g.fill(); g.restore(); g.globalAlpha = 1;
				}
			};
			h.hit = (p, t) => { for (int i = 0; i < n; i++) { if (t < tas[i] || t >= tas[i] + dur) continue; var rg = reg(ds[i]); double rx = rg[0], ry = rg[1], rw = rg[2], rh = rg[3]; if (p.x > rx - p.r + 2 && p.x < rx + rw + p.r - 2 && p.y > ry - p.r + 2 && p.y < ry + rh + p.r - 2) return true; } return false; };
			return h;
		}
		// a pink stage light from the top sweeping. ang(t) its direction
		static Hz L8Spot(double x, Func<double, double> ang, double hw, double tw, double ta, double te)
		{
			const double y = -30, L = 1100;
			var h = new Hz();
			h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return; double a = ang(Math.Max(t, ta)), f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3);
				g.beginPath(); g.moveTo(x, y); g.lineTo(x + JS.cos(a - hw) * L, y + JS.sin(a - hw) * L); g.lineTo(x + JS.cos(a + hw) * L, y + JS.sin(a + hw) * L); g.closePath();
				if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.12 * k)})"; g.fill(); }
				else { g.globalAlpha = f * 0.85; g.fillStyle = PINK; g.fill(); g.globalAlpha = f * 0.3; g.fillStyle = "#fff"; g.beginPath(); g.moveTo(x, y); g.lineTo(x + JS.cos(a - hw * 0.3) * L, y + JS.sin(a - hw * 0.3) * L); g.lineTo(x + JS.cos(a + hw * 0.3) * L, y + JS.sin(a + hw * 0.3) * L); g.closePath(); g.fill(); g.globalAlpha = 1; }
				g.fillStyle = GREY + "0.6)"; g.fillRect(x - 22, 0, 44, 24);
			};
			h.hit = (p, t) => { if (t < ta || t >= te) return false; double dx = p.x - x, dy = p.y - y, d = hypot(dx, dy); double da = JS.atan2(dy, dx) - ang(t); da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) < hw - (p.r - 2) / Math.Max(d, 1); };
			return h;
		}
		// the lie detector: grey graph paper, and needles drawing a pink line that walks left. nd: [{y(u), pen(u)}] u = the machine's time
		// wp(t): the needle's time (slows down in "real slow"). rev: the needle is on the left and the paper walks right
		static Hz L8Graph(L8GraphO o)
		{
			double NX = o.NX, v = o.v, th = o.th; Func<double, double> U = o.wp ?? (t => t); double sg = o.rev ? -1 : 1; Func<double, double> age = x => sg * (NX - x) / v; double T0 = o.ta - (o.pre);
			var h = new Hz();
			h.update = (t, dt) => { if (t > o.te + 0.4) h.dead = true; };
			h.draw = t =>
			{
				if (t < o.tw) return; double u = U(t), a = min(1, (t - o.tw) / 0.4, t < o.te ? 1 : 1 - (t - o.te) / 0.4); if (a <= 0) return; g.globalAlpha = a;
				double off = (v * t) % 80; g.strokeStyle = GREY + "0.1)"; g.lineWidth = 1; g.beginPath(); for (double x = (sg > 0 ? -off : off) % 80 + 80; x < W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); } for (double y = 40; y < H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
				g.stroke();
				g.strokeStyle = GREY + "0.2)"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, H / 2); g.lineTo(W, H / 2); g.stroke();
				foreach (var nd in o.nd)
				{
					double yy = nd.y(u); g.strokeStyle = GREY + "0.5)"; g.lineWidth = 6; g.beginPath(); g.moveTo(sg > 0 ? W + 10 : -10, yy - 30); g.lineTo(NX, yy); g.stroke();
					{
						bool wn = t < o.ta; double k = wn ? clamp((t - o.tw) / (o.ta - o.tw), 0, 1) : 1; g.lineCap = "round";
						foreach (var (lw, col) in wn ? new[] { (3.0, $"rgba(255,42,109,{str(0.15 + 0.4 * k)})") } : new[] { (th + 14, "rgba(255,42,109,0.2)"), (th, PINK) })
						{
							g.strokeStyle = col; g.lineWidth = lw; if (wn) g.setLineDash(new double[] { 10, 8 }); g.beginPath(); bool on = false;
							for (double x = NX; sg > 0 ? x >= -6 : x <= W + 6; x -= 6 * sg) { double tt = t - age(x), uu = U(tt); if (tt < T0 || !nd.pen(uu)) { on = false; continue; } double y = nd.y(uu); if (on) g.lineTo(x, y); else g.moveTo(x, y); on = true; }
							g.stroke(); g.setLineDash(new double[] { });
						}
						g.lineCap = "butt";
					}
					g.fillStyle = t < o.ta ? $"rgba(255,42,109,{str(0.3 + 0.5 * pulse)})" : PINK; circle(NX, yy, 9);
				}
				g.globalAlpha = 1;
			};
			h.hit = (p, t) =>
			{
				if (t < o.ta || t >= o.te) return false; double u = U(t);
				foreach (var nd in o.nd)
				{
					if (L8hitC(p, NX, nd.y(u), 9)) return true;
					for (double dx = -10; dx <= 10; dx += 5) { double x = p.x + dx; if (age(x) < 0) continue; double tt = t - age(x), uu = U(tt); if (tt < T0 || !nd.pen(uu)) continue; double yy = nd.y(uu); if (dx * dx + (p.y - yy) * (p.y - yy) < (th / 2 + p.r - 2) * (th / 2 + p.r - 2)) return true; }
				}
				return false;
			};
			return h;
		}
		// "the attraction": two coin walls (with an opening) are pulled to each other and meet in the middle. Two grey magnets on the sides
		static Hz L8Pull(double gyL, double gyR, double gw, double tw, double ta, double tb)
		{
			const double sp = 26, cr = 12; double x0 = 14;
			Func<double, double> xs = t => { if (t < ta) return x0; double k = clamp((t - ta) / (tb - ta), 0, 1); return x0 + (W / 2 - x0) * k * k; };
			var h = new Hz();
			h.update = (t, dt) => { if (t >= tb) { burst(W / 2, H / 2, "#fff", 26, 320); h.dead = true; } };
			h.draw = t =>
			{
				if (t < tw) return; double x = xs(t);
				foreach (int sd in new[] { 0, 1 }) { double mx = sd != 0 ? W - 34 : 34; g.save(); g.translate(mx, H / 2); g.scale(sd != 0 ? -1 : 1, 1); g.strokeStyle = GREY + "0.6)"; g.lineWidth = 16; g.beginPath(); g.arc(0, 0, 44, -Math.PI / 2, Math.PI / 2, true); g.stroke(); g.fillStyle = GREY + "0.75)"; g.fillRect(-4, -52, 26, 16); g.fillRect(-4, 36, 26, 16); g.restore(); }
				double warn = t < ta ? 0.2 + 0.4 * clamp((t - tw) / (ta - tw), 0, 1) : 1;
				foreach (int sd in new[] { 0, 1 }) { double xx = sd != 0 ? W - x : x, gy = sd != 0 ? gyR : gyL; for (double y = -10; y < H + 20; y += sp) { if (Math.Abs(y - gy) < gw / 2) continue; if (warn < 1) L8ghost(xx, y, cr, warn); else L8coin(xx, y, cr, 1); } }
			};
			h.hit = (p, t) => { if (t < ta) return false; double x = xs(t); foreach (int sd in new[] { 0, 1 }) { double xx = sd != 0 ? W - x : x, gy = sd != 0 ? gyR : gyL; if (Math.Abs(p.x - xx) < cr + p.r - 2 && Math.Abs(p.y - gy) > gw / 2 - p.r + 2) return true; } return false; };
			return h;
		}
		// "the tension": a tight rope the width of the screen shakes with the rhythm, more and more, then snaps and drops coins
		static Hz L8Rope(double y0, double tw, double ta, double tb, double amp)
		{
			Func<double, double> A = t => amp * clamp((t - ta) / (tb - ta), 0, 1);
			Func<double, double, double> yy = (x, t) => y0 + A(t) * JS.sin(Math.PI * 3 * x / W) * JS.sin(Math.PI * BT(t));
			var h = new Hz();
			h.update = (t, dt) => { if (t >= tb) { for (double x = 60; x < W; x += 120) { burst(x, yy(x, t), "#fff", 4, 240); hazards.Add(L8Coin(x, yy(x, t), 0, -200, 700, 10)); } shake = Math.Max(shake, 10); h.dead = true; } };
			h.draw = t =>
			{
				if (t < tw) return; if (t < ta) { g.strokeStyle = $"rgba(255,42,109,{str(0.2 + 0.4 * clamp((t - tw) / (ta - tw), 0, 1))})"; g.lineWidth = 3; g.beginPath(); g.moveTo(0, y0); g.lineTo(W, y0); g.stroke(); return; }
				g.lineCap = "round"; g.strokeStyle = "rgba(255,42,109,0.22)"; g.lineWidth = 26; g.beginPath(); for (double x = 0; x <= W; x += 20) { if (x != 0) g.lineTo(x, yy(x, t)); else g.moveTo(x, yy(x, t)); }
				g.stroke();
				g.strokeStyle = PINK; g.lineWidth = 12 * (1 + 0.15 * pulse); g.stroke(); g.lineCap = "butt";
			};
			h.hit = (p, t) => t >= ta && t < tb && Math.Abs(p.y - yy(p.x, t)) < 6 + p.r - 2;
			return h;
		}
		// "this is perfection": a coin mandala: the rings gather (faint until ta), turn against each other, and on tb burst out in straight lines
		// rings: [{r,n,sp,ph}]
		static Hz L8Mandala(double cx, double cy, L8MRing[] rings, double tw, double ta, double tb, double v)
		{
			var C = new List<double[]>(); foreach (var R in rings) { for (int i = 0; i < R.n; i++) C.Add(new[] { R.r, (R.ph) + TAU * i / R.n, R.sp }); }
			double rmin = min(rings.Select(r => r.r).ToArray());
			Func<double[], double, double[]> P = (c, t) => { double r = c[0], a0 = c[1], sp = c[2], ang = a0 + (t > ta ? sp * (Math.Min(t, tb) - ta) : 0), rad = t < ta ? r + (1100 - r) * JS.pow(1 - L8ease((t - tw) / (ta - tw)), 2) : t < tb ? r : r + v * (t - tb); return new[] { cx + JS.cos(ang) * rad, cy + JS.sin(ang) * rad }; };
			var h = new Hz();
			h.update = (t, dt) => { if (t > tb && rmin + v * (t - tb) > 1500) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return;
				if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); foreach (var c in C) { var q = P(c, ta); L8ghost(q[0], q[1], 11, 0.15 + 0.4 * k); var fq = P(c, t); double fx = fq[0], fy = fq[1]; if (fx > -20 && fx < W + 20 && fy > -20 && fy < H + 20) { g.fillStyle = "rgba(255,42,109,0.3)"; circle(fx, fy, 8); } } return; }
				foreach (var c in C) { var q = P(c, t); double x = q[0], y = q[1]; if (x < -20 || x > W + 20 || y < -20 || y > H + 20) continue; L8coin(x, y, 11 * (1 + 0.1 * pulse), 1); }
				if (t < tb) { g.strokeStyle = "rgba(255,255,255,0.12)"; g.lineWidth = 2; foreach (var R in rings) { g.beginPath(); g.arc(cx, cy, R.r, 0, TAU); g.stroke(); } }
			};
			h.hit = (p, t) => { if (t < ta) return false; foreach (var c in C) { var q = P(c, t); if (L8hitC(p, q[0], q[1], 11)) return true; } return false; };
			return h;
		}

		/* ---------------- phase 3: the band ---------------- */
		// a rope of coins hung from one side of the screen to the other (curved like a garland), walking with the 1 2 3 kick step, with a cut place (the opening) to go through
		// vert: the rope is vertical. a0 its place, dir its direction, unit the step length. clip: [x0,x1] shown only in this part. lan: lanterns
		static Hz L8Chain(L8ChainO o)
		{
			double L = o.vert ? H : W, PL = o.vert ? W : H, sp = o.lan ? 36 : 26; const double cr = 12; int n = (int)Math.Ceiling((L + 80) / sp) + 1;
			double tw = o.tw, ta = o.ta, te = o.te, gw = o.gw, gc = o.gc, sag = o.sag ?? 70, b0 = o.b0;
			Func<double, double> pos = t => t < ta ? o.a0 : o.a0 + o.dir * o.unit * (L8step(BT(t)) - L8step(b0));
			Func<double, double, double> sw = (u, t) => o.lan ? 14 * JS.sin(Math.PI * BT(t) + u * 0.01) : 0;
			Func<double, double> off = u => { double q = (u - L / 2) / (L / 2 + 40); return sag * (1 - q * q); };
			Func<int, double, double, double[]> pt = (k, t, a) => { double u = -40 + k * sp, c = a + off(u); return o.vert ? new[] { c + sw(u, t), u } : new[] { u, c + sw(u, t) }; };
			Func<double, bool> inGap = u => Math.Abs(u - gc) < gw / 2;
			Action clipOn = () => { if (o.clip == null && o.clipY == null) return; g.save(); g.beginPath(); if (o.clip != null) g.rect(o.clip[0], 0, o.clip[1] - o.clip[0], H); else g.rect(0, o.clipY[0], W, o.clipY[1] - o.clipY[0]); g.clip(); };
			double? pa = null, pn = null;
			var h = new Hz();
			h.update = (t, dt) => { double a = pos(t); pa = pn == null ? a : pn; pn = a; if (t >= te || (t > ta && (o.dir > 0 ? a > PL + 140 : a < -sag - 140))) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return; double a = pos(t); clipOn();
				if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); for (int i = 0; i < n; i += 2) { double u = -40 + i * sp; if (inGap(u)) continue; var q = pt(i, t, a); L8ghost(q[0], q[1], cr, 0.18 + 0.42 * k); } if (o.clip != null || o.clipY != null) g.restore(); return; }
				g.strokeStyle = "rgba(255,42,109,0.18)"; g.lineWidth = cr * 3; g.lineCap = "round";
				foreach (var seg in new[] { new[] { -40, gc - gw / 2 }, new[] { gc + gw / 2, L + 40 } })
				{
					if (seg[1] <= seg[0]) continue; g.beginPath();
					for (double u = seg[0]; u <= seg[1]; u += 40) { double c = a + off(u); if (o.vert) { if (u == seg[0]) g.moveTo(c, u); else g.lineTo(c, u); } else { if (u == seg[0]) g.moveTo(u, c); else g.lineTo(u, c); } }
					g.stroke();
				}
				g.lineCap = "butt";
				for (int i = 0; i < n; i++)
				{
					double u = -40 + i * sp; if (inGap(u)) continue; var q = pt(i, t, a); double x = q[0], y = q[1];
					if (o.lan) { g.fillStyle = PINK; g.beginPath(); g.ellipse(x, y, cr, cr * 1.25, 0, 0, TAU); g.fill(); g.fillStyle = "rgba(255,255,255,0.6)"; g.fillRect(x - cr * 0.6, y - cr * 1.3, cr * 1.2, 4); g.fillRect(x - cr * 0.6, y + cr * 1.3 - 4, cr * 1.2, 4); g.fillStyle = "rgba(255,255,255,0.35)"; circle(x, y, cr * 0.45); }
					else L8coin(x, y, cr * (1 + 0.08 * pulse), 1);
				}
				foreach (var u in new[] { gc - gw / 2 - sp / 2, gc + gw / 2 + sp / 2 }) { double c = a + off(u); g.fillStyle = "rgba(255,255,255,0.5)"; if (o.vert) circle(c, u, 4); else circle(u, c, 4); }
				if (o.clip != null || o.clipY != null) g.restore();
			};
			h.hit = (p, t) => { if (t < ta) return false; if (o.clip != null && (p.x < o.clip[0] || p.x > o.clip[1])) return false; if (o.clipY != null && (p.y < o.clipY[0] || p.y > o.clipY[1])) return false; double a = pos(t), a0 = pa ?? a, u = o.vert ? p.y : p.x, v = o.vert ? p.x : p.y, e = off(u) + sw(u, t), d1 = v - (a + e), d0 = v - (a0 + e); if (Math.Min(Math.Abs(d1), Math.Abs(d0)) >= cr + p.r - 2 && d0 * d1 > 0) return false; return Math.Abs(u - gc) > gw / 2 - p.r + 2; };
			return h;
		}
		// where valve v (0 1 2) is on the trumpet. pf(t) = [x, y, the bell's angle, size]
		static double[] L8valve(Func<double, double[]> pf, double t, int v) { var P = pf(t); double x = P[0], y = P[1], a = P[2], sc = P.Length > 3 ? P[3] : 0, c = JS.cos(a), s = JS.sin(a), k = 0.75 * L8or(sc, 1), b = (-150 - v * 34) * k; return new[] { x + c * b + s * 22 * k, y + s * b - c * 22 * k }; }
		// the trumpet's body (grey, harmless): the bell is at the place and points at the angle, with the three valves on top
		// quiet: fades out quietly instead of exploding
		static Hz L8Trumpet(Func<double, double[]> pf, double t0, double t1, double chg, bool quiet)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t >= t1 + (quiet ? 0.4 : 0)) { if (!quiet) { var P = pf(t); double x = P[0], y = P[1]; burst(x, y, "#fff", 60, 620); burst(x, y, GREY + "0.9)", 40, 480); flash = Math.Max(flash, 0.7); shake = Math.Max(shake, 20); } h.dead = true; } };
			h.draw = t =>
			{
				if (t < t0) return; var P = pf(t); double x = P[0], y = P[1], a = P[2], sc = P.Length > 3 ? P[3] : 0; double fa = quiet && t > t1 ? Math.Max(0, 1 - (t - t1) / 0.4) : 1; g.globalAlpha = fa; g.save(); g.translate(x, y); g.rotate(a); g.scale(0.75 * L8or(sc, 1), 0.75 * L8or(sc, 1));
				double c = chg != 0 && t > chg ? clamp((t - chg) / (t1 - chg), 0, 1) : 0; string col = c > 0 ? $"rgba({str(215 + 40 * c)},{str(215 + 40 * c)},{str(230 + 25 * c)}," : GREY;
				if (c > 0) { g.shadowColor = "#fff"; g.shadowBlur = 30 * c; }
				g.fillStyle = col + "0.85)"; g.beginPath(); g.moveTo(8, -62); g.quadraticCurveTo(-30, -14, -96, -12); g.lineTo(-96, 12); g.quadraticCurveTo(-30, 14, 8, 62); g.closePath(); g.fill();
				g.fillStyle = col + "0.35)"; g.beginPath(); g.ellipse(6, 0, 10, 60, 0, 0, TAU); g.fill();
				g.fillStyle = col + "0.8)"; g.fillRect(-270, -10, 176, 20); g.fillRect(-300, -6, 32, 12);
				g.strokeStyle = col + "0.7)"; g.lineWidth = 14; g.beginPath(); g.moveTo(-120, 10); g.quadraticCurveTo(-120, 52, -170, 52); g.lineTo(-215, 52); g.quadraticCurveTo(-250, 52, -250, 10); g.stroke();
				for (int v = 0; v < 3; v++) { double b = -150 - v * 34; g.fillStyle = col + "0.9)"; g.fillRect(b - 9, -44, 18, 36); g.fillRect(b - 13, -50, 26, 8); }
				g.shadowBlur = 0; g.restore(); g.globalAlpha = 1;
			};
			return h;
		}
		// a curved sound wave coming out of the trumpet's bell. mr: it fades after this distance (a small puff)
		static Hz L8Wave(double x, double y, double a, double hw, double t0, double sp, double th, double mr = 1500)
		{
			var h = new Hz();
			h.update = (t, dt) => { if ((t - t0) * sp > mr) h.dead = true; };
			h.draw = t => { double r = 30 + (t - t0) * sp, f = mr < 1500 ? clamp((mr - r) / 80, 0, 1) : 1; g.globalAlpha = f; g.strokeStyle = "rgba(255,42,109,0.2)"; g.lineWidth = th + 14; g.beginPath(); g.arc(x, y, r, a - hw, a + hw); g.stroke(); g.strokeStyle = PINK; g.lineWidth = th * (1 + 0.15 * pulse); g.beginPath(); g.arc(x, y, r, a - hw, a + hw); g.stroke(); g.globalAlpha = 1; };
			h.hit = (p, t) => { double r = 30 + (t - t0) * sp; if (r > mr - 30) return false; double dx = p.x - x, dy = p.y - y, d = hypot(dx, dy); if (Math.Abs(d - r) > th / 2 + p.r - 2) return false; double da = JS.atan2(dy, dx) - a; da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) < hw + p.r / Math.Max(d, 1); };
			return h;
		}
		// a trumpet phrase: the pressed valves light up pink, the place of the blow shows faint, then waves on the trumpet's tune
		static Hz L8Riff(Func<double, double[]> pf, int[] vs, double D, double hw, double tw, double[] ts, double sp, double th, double? mr)
		{
			int i = 0;
			Func<double, int, double> ang = (t, v) => pf(t)[2] + (v - 1) * D;
			var h = new Hz();
			h.update = (t, dt) => { while (i < ts.Length && t >= ts[i]) { var P = pf(ts[i]); double x = P[0], y = P[1]; foreach (var v in vs) hazards.Add(L8Wave(x, y, ang(ts[i], v), hw, ts[i], sp, th, mr ?? 1500)); i++; } if (i >= ts.Length) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return;
				foreach (var v in vs)
				{
					var V = L8valve(pf, t, v); double vx = V[0], vy = V[1]; g.fillStyle = PINK; g.globalAlpha = 0.6 + 0.4 * pulse; g.save(); g.translate(vx, vy); g.rotate(pf(t)[2]); g.fillRect(-10, -14, 20, 20); g.restore(); g.globalAlpha = 1;
					if (t < ts[0]) { double k = clamp((t - tw) / (ts[0] - tw), 0, 1); var P = pf(t); double x = P[0], y = P[1], a = ang(t, v); g.fillStyle = $"rgba(255,42,109,{str(0.04 + 0.13 * k)})"; g.beginPath(); g.moveTo(x, y); g.arc(x, y, L8or(mr ?? 0, 1500), a - hw, a + hw); g.closePath(); g.fill(); }
				}
			};
			return h;
		}
		// a blueprint line: drawn faint and dashed, then becomes a straight blow that hurts
		static Hz L8Beam(double x1, double y1, double a, double len, double w, double tw, double ta, double te)
		{
			double c = JS.cos(a), s = JS.sin(a);
			var h = new Hz();
			h.update = (t, dt) => { if (t > te + 0.3) h.dead = true; };
			h.draw = t =>
			{
				if (t < tw) return;
				if (t < ta)
				{
					double k = clamp((t - tw) / ((ta - tw) * 0.5), 0, 1), q = clamp((t - tw) / (ta - tw), 0, 1); g.strokeStyle = $"rgba(255,42,109,{str(0.18 + 0.35 * q)})"; g.lineWidth = 2; g.setLineDash(new double[] { 12, 8 }); g.beginPath(); g.moveTo(x1, y1); g.lineTo(x1 + c * len * k, y1 + s * len * k); g.stroke(); g.setLineDash(new double[] { });
					g.fillStyle = $"rgba(255,42,109,{str(0.03 + 0.08 * q)})"; g.save(); g.translate(x1, y1); g.rotate(a); g.fillRect(0, -w / 2, len * k, w); g.restore(); return;
				}
				double f = t < te ? 1 : Math.Max(0, 1 - (t - te) / 0.3), e = t - ta; g.globalAlpha = f; g.save(); g.translate(x1, y1); g.rotate(a); g.fillStyle = "rgba(255,42,109,0.25)"; g.fillRect(0, -w / 2 - 10, len, w + 20); g.fillStyle = e < 0.06 ? "#fff" : PINK; g.fillRect(0, -w / 2, len, w); g.restore(); g.globalAlpha = 1;
			};
			h.hit = (p, t) => { if (t < ta || t >= te) return false; double dx = p.x - x1, dy = p.y - y1, pr = dx * c + dy * s; if (pr < 0 || pr > len) return false; return Math.Abs(-dx * s + dy * c) < w / 2 + p.r - 2; };
			return h;
		}
		// a row of coins falls from the top (shown faint before). xs the coins' places
		static Hz L8Rain(List<double> xs, double tw, double ta, double gr, double vy)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t >= ta) { foreach (var x in xs) hazards.Add(L8Coin(x, -14, 0, vy, gr, 11)); h.dead = true; } };
			h.draw = t => { if (t < tw) return; double k = clamp((t - tw) / (ta - tw), 0, 1); foreach (var x in xs) L8ghost(x, 22, 11, 0.15 + 0.45 * k); };
			return h;
		}
		// "self-restraint": a tight chain of coins ties the trumpet to the floor and shakes; on t1 it snaps and drops its coins
		static Hz L8Bind(Func<double, double[]> pf, double ax, double ay, double tw, double ta, double t1)
		{
			const double cr = 10, sp = 24;
			Func<double, double[]> ends = t => { var P = pf(t); return new[] { P[0], P[1], ax, ay }; };
			Func<double, List<double[]>> pts = t => { var e = ends(t); double x = e[0], y = e[1], x2 = e[2], y2 = e[3], L = hypot(x2 - x, y2 - y); int n = (int)Math.Floor(L / sp); double nx = -(y2 - y) / L, ny = (x2 - x) / L, amp = 5 * JS.sin(t * 38); var P = new List<double[]>(); for (int i = 1; i < n; i++) { double k = (double)i / n, w = amp * JS.sin(Math.PI * k); P.Add(new[] { x + (x2 - x) * k + nx * w, y + (y2 - y) * k + ny * w }); } return P; };
			var h = new Hz();
			h.update = (t, dt) => { if (t >= t1) { var P = pts(t); for (int i = 0; i < P.Count; i++) { var q = P[i]; if (i % 2 == 0) hazards.Add(L8Coin(q[0], q[1], 0, -80, 700, 10)); } shake = Math.Max(shake, 8); h.dead = true; } };
			h.draw = t =>
			{
				if (t < tw) return; var P = pts(t); if (t < ta) { double k = clamp((t - tw) / (ta - tw), 0, 1); for (int i = 0; i < P.Count; i += 2) L8ghost(P[i][0], P[i][1], cr, 0.15 + 0.45 * k); return; }
				g.strokeStyle = "rgba(255,42,109,0.18)"; g.lineWidth = cr * 2.6; g.lineCap = "round"; g.beginPath(); for (int i = 0; i < P.Count; i++) { var q = P[i]; if (i != 0) g.lineTo(q[0], q[1]); else g.moveTo(q[0], q[1]); }
				g.stroke(); g.lineCap = "butt";
				foreach (var q in P) { g.strokeStyle = PINK; g.lineWidth = 5; g.beginPath(); g.ellipse(q[0], q[1], cr, cr * 0.7, JS.atan2(ay - q[1], ax - q[0]), 0, TAU); g.stroke(); }
			};
			h.hit = (p, t) => { if (t < ta) return false; foreach (var q in pts(t)) if (L8hitC(p, q[0], q[1], cr)) return true; return false; };
			return h;
		}
		// a grey conga line (decoration) walking with the 1 2 3 kick step at the win
		static Hz L8Conga(double t0, double t1, double b0)
		{
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				if (t < t0) return; double d = 40 * (L8step(BT(t)) - L8step(b0)), a = min(1, (t - t0) / 0.4, (t1 - t) / 0.5); g.globalAlpha = Math.Max(0, a); bool kick = ((Math.Floor(BT(t)) % 4) + 4) % 4 == 3;
				for (int i = 0; i < 12; i++)
				{
					double x = -60 - i * 70 + d, y = H - 60; if (x < -40 || x > W + 40) continue; g.fillStyle = GREY + "0.5)"; circle(x, y - 38, 13); g.fillRect(x - 11, y - 24, 22, 34); g.strokeStyle = GREY + "0.5)"; g.lineWidth = 6; g.beginPath(); g.moveTo(x - 5, y + 10); g.lineTo(x - 8, y + 34); g.moveTo(x + 5, y + 10);
					if (kick) g.lineTo(x + 30, y + 20); else g.lineTo(x + 8, y + 34);
					g.moveTo(x + 10, y - 18); g.lineTo(x + 52, y - 26); g.stroke();
				}
				g.globalAlpha = 1;
			};
			return h;
		}
		// confetti (harmless)
		static Hz L8Confetti(double t0, double t1)
		{
			var P = new List<double[]>(); for (int i = 0; i < 90; i++) { double a1 = random() * W, a2 = -20 - random() * 300, a3 = (random() - 0.5) * 120, a4 = 140 + random() * 160, a5 = random() * TAU; P.Add(new[] { a1, a2, a3, a4, a5, i % 3 }); }
			var h = new Hz();
			h.update = (t, dt) => { if (t > t1) h.dead = true; };
			h.draw = t =>
			{
				if (t < t0) return; double e = t - t0, a = Math.Min(1, (t1 - t) / 0.6); g.globalAlpha = Math.Max(0, a);
				foreach (var q in P) { double x = q[0], y = q[1], vx = q[2], vy = q[3], r = q[4], c = q[5]; double xx = x + vx * e + 20 * JS.sin(e * 3 + r), yy = y + vy * e; if (yy > H + 20) continue; g.save(); g.translate(xx, yy); g.rotate(r + e * 4); g.fillStyle = c == 0 ? "rgba(255,255,255,0.85)" : c == 1 ? GREY + "0.7)" : "rgba(255,220,120,0.8)"; g.fillRect(-6, -3, 12, 6); g.restore(); }
				g.globalAlpha = 1;
			};
			return h;
		}

		/* ================================ the level ================================ */
		public static List<Ev> buildLevel8()
		{
			var E = new List<Ev>(); Action<double, Action> ev = (b, f) => E.Add(new Ev { b = b, f = f }); Action<double, Action> evk = (b, f) => E.Add(new Ev { b = b, keep = true, f = f });
			// events that push a small attack, by kind (JS reads this from the event function's source text, see the Easy filter at the end)
			var TAG = new Dictionary<Ev, string>(); Action<double, Action, string> evt = (b, f, k) => { var e = new Ev { b = b, f = f }; E.Add(e); TAG[e] = k; };
			var TI0 = ezT(new[] { new Tier { sp = 300, wn = 1.5, gap = 230 }, new Tier { sp = 310, wn = 1.4, gap = 175 }, new Tier { sp = 330, wn = 1.4, gap = 175 } }); var BAS = new double[] { 300, 310, 330 };
			var TI = TI0.Select((q, i) => new L8Tier { sp = q.sp, wn = q.wn, gap = q.gap, bas = BAS[i] }).ToArray();
			Func<L8Tier, double> kf = T_ => T_.sp / T_.bas;
			Action<Hz> push = h => hazards.Add(h);
			Action<double, double, double> fx = (b, f2, s2) => ev(b, () => { flash = Math.Max(flash, f2); shake = Math.Max(shake, s2); });
			Action<double, string, double, double, double, double, string> word = (b, s, x, y, size, dur, col) => ev(b, () => push(L8Word(s, x, y, size, B(b), B(b + dur), col)));
			// blends sections: secs=[[start beat, f(k,x,b)]...], and the change takes bl beats
			Func<(double b0, Func<double, double, double, double> f)[], double, Func<double, double, double, double>> blend = (secs, bl) => (k, x, b) => { int i = 0; while (i < secs.Length - 1 && b >= secs[i + 1].b0) i++; double v = secs[i].f(k, x, b); if (i == 0) return v; double e = L8ease((b - secs[i].b0) / bl); return e >= 1 ? v : secs[i - 1].f(k, x, b) * (1 - e) + v * e; };
			const int NS = 15; const double DX = W / NS; Func<double, double> X0 = k => DX / 2 + k * DX;
			// the whole curtain walks sideways with the cumbia step and wraps from one side to the other
			Func<double, double, double, double> drift = (k, b, d) => { double x = X0(k) + d * 22 * L8step(b); return ((x % W) + W) % W; };

			/* ================= phase 1: the Barranquilla carnival (0:00 - 0:57.8) ================= */
			var T = TI[0]; double K1 = kf(T), G1 = T.gap + (easy ? 20 : 0), SWA = G1 / 2 + 55, SWP = 0.7, SWA1 = G1 / 2 + 100;
			ev(0, () => push(L8BG1(B(0), B(96.5), b => b < 49.4 ? 0.35 : 1)));
			// 0:00.4 "Ladies up in here tonight": a giant coin belt unrolls left to right
			ev(0, () => push(L8Belt(new L8BeltO { y = t => { double b = BT(t); return b < 13.6 ? H * 0.24 + 40 * JS.sin(L8step(b) * Math.PI / L8BAR) : H * 0.24 - 300 * JS.pow(clamp((b - 13.6) / 1.2, 0, 1), 2); }, tw = B(0), ta = B(1.2), tu = B(3.6), te = B(15.5), sh = new[] { new[] { 12.7, 13.6 } }, drop = 13.6 })));
			// "No fighting" x3: two balls rush at each other and bounce back, and each ball lets go a ring
			Action<double, double, int> balls = (b, y, n) => ev(b - 2, () => push(L8Balls(y, B(b - 2), B(b - 0.9), B(b + 0.2), B(b + 1.6), 230 * K1, n)));
			balls(2.77, 519, 16); balls(6.08, 652, 18); balls(11.08, 388, 20);
			// 0:07.9 "Shakira, Shakira": the curtain falls from the top and shakes, and 0:09.6 with the drums the curtain from top and bottom dances
			fx(15.67, 0.4, 12); word(12.73, "SHAKIRA", W / 2, H * 0.62, 70, 2.6, GREY + "0.3)");
			Func<double, double, double, double, double> wv = (x, b, A, f) => A * JS.sin(TAU * x / W * f + 0.85 * L8step(b));
			var cA = new (double, Func<double, double, double, double>)[] { (12.7, (k, x, b) => H / 2 + 70), (15.67, (k, x, b) => H / 2 + wv(x, b, 70, 1.2)), (19.4, (k, x, b) => H / 2 + wv(x, b, 40, 1)), (23.15, (k, x, b) => H / 2), (28.15, (k, x, b) => H / 2 + wv(x, b, 60, 1.5)), (31.48, (k, x, b) => H / 2) };
			var hA = new (double, Func<double, double, double, double>)[] { (12.7, (k, x, b) => G1 / 2 + 45), (19.4, (k, x, b) => G1 / 2 + 60), (28.15, (k, x, b) => G1 / 2 + 45), (31.48, (k, x, b) => G1 / 2 + 50) };
			Func<double, double> swA = b => b < 14 ? 0 : (1) * SWA1 * Math.Tanh(1.2 * JS.sin(TAU * (b - 14) / (10 * SWP))) / Math.Tanh(1.2) * Math.Min(1, (b - 14) / 1.5) * (1 - L8ease((b - 31.6) / 1.6));
			var cyA0 = blend(cA, 0.6); Func<double, double, double, double> cyA = (k, x, b) => cyA0(k, x, b) + swA(b); var hwA = blend(hA, 0.6);
			// 0:14.1 the four "sí": the curtain gets shorter for the rings from the corners
			Func<double, double> shortA = b => b < 22.6 ? 0 : b < 23.4 ? L8ease((b - 22.6) / 0.8) : b < 27.9 ? 1 : 1 - L8ease((b - 27.9) / 0.6);
			Func<double, double, double, double> lenTopA = (k, x, b) => { double L = cyA(k, x, b) - hwA(k, x, b), s = shortA(b); return L * (1 - s) + (70 + 20 * JS.sin(k + b * Math.PI / 2)) * s; };
			Func<double, double, double, double> lenBotA = (k, x, b) => { if (b < 15.67) return 0; double L = H - cyA(k, x, b) - hwA(k, x, b), s = shortA(b); return Math.Min(1, (b - 15.67) / 0.6) * (L * (1 - s) + (70 + 20 * JS.sin(k + 1 + b * Math.PI / 2)) * s); };
			// 0:19.1 "Oh baby when you talk like that": the curtain opens from the middle like a stage curtain
			Func<double, double> openA = b => b < 31.48 ? 0 : L8ease((b - 31.48) / 2.6);
			Func<double, double, double> XA = (k, t) => { double b = BT(t), x = drift(k, b, 1), o = openA(b); return W / 2 + (x - W / 2) * (1 + 1.6 * o) + (x < W / 2 ? -1 : 1) * o * 90; };
			Action<int, Func<double, double, double, double>, List<L8Toss>> curtainA = (side, len, toss) => ev(12.6, () => push(L8Curtain(new L8CurtainO
			{
				n = NS, side = side, X = XA, len = (k, t) => len(k, XA(k, t), BT(t)), tilt = (k, t) => 0.06 * L8hip(BT(t)), tw = B(12.6), ta = B(14), te = B(35.2),
				sh = new[] { new[] { 14, 14.6 }, new[] { 28.15, 29.1 }, new[] { 29.4, 30.3 } }, toss = toss
			})));
			curtainA(0, lenTopA, new List<L8Toss> { new L8Toss { b = 28.4, ev = 2, ph = 0, vx = 50, vy = -120, gr = 620 }, new L8Toss { b = 29.6, ev = 2, ph = 1, vx = 50, vy = -120, gr = 620 } });
			curtainA(1, lenBotA, new List<L8Toss> { new L8Toss { b = 28.4, ev = 2, ph = 1, vx = 40, vy = -560, gr = 700 }, new L8Toss { b = 29.6, ev = 2, ph = 0, vx = 40, vy = -560, gr = 700 } });
			// 0:11.9 "she make a man want to speak Spanish": big ¿ and ¡ marks fall and flip
			var MK = new (double, string, double)[] { (19.6, "¿", 300), (19.6, "¡", 980), (20.6, "¡", 560), (20.6, "¿", 1240 - 520), (21.6, "¿", 180), (21.6, "¡", 1100), (22.6, "¡", 420), (22.6, "¿", 860) };
			foreach (var (b, ch, x) in MK) evt(b - 1.3, () => push(L8Glyph(ch, x, 200, B(b - 1.3), B(b), B(b + 3.2))), "L8Glyph(");
			// 0:14.1 "Cómo se llama (sí), bonita (sí), mi casa (sí), su casa (sí)": every "sí" a ring from a corner
			var CN = new (double, double, double)[] { (23.9, 0, 0), (25.1, W, 0), (26.3, W, H), (27.5, 0, H) };
			foreach (var (b, x, y) in CN) { double ph = TAU / 46 * new[] { 0, 0.5, 0.25, 0.75 }[Array.FindIndex(CN, q => q.Item1 == b)]; ev(b - 1, () => push(L8Ring(x, y, B(b), 300 * K1, 46, ph, B(b - 1)))); word(b, "¡SÍ!", x != 0 ? W - 150 : 150, y != 0 ? H - 120 : 120, 54, 1.1, GREY + "0.4)"); }
			// 0:21.6 "you make a woman go mad": a giant Spanish fan opens in the middle and turns madly
			Func<double, double> fanRot = b => { double s = B(36.4); double t = B(b); if (b < 36.4) return -Math.PI / 2; double m = B(38.2); return -Math.PI / 2 + (t < m ? 0.75 * (t - s) : 0.75 * (m - s) - 1.0 * (t - m)); };
			Func<double, double> fanSp = b => b < 35.65 ? 0 : b < 36.4 ? 3.2 * L8ease((b - 35.65) / 0.75) : b < 39.82 ? 3.2 : 3.2 * (1 - L8ease((b - 39.82) / 0.9));
			ev(34.3, () => push(L8Fan(new L8FanO { cx = W / 2, cy = H * 0.75, R = 700, r0 = 46, n = easy ? 6 : 8, spread = t => fanSp(BT(t)), rot = t => fanRot(BT(t)), tw = B(34.3), ta = B(35.65), tf = B(36.4), te = B(40.8) })));
			word(35.65, "¡LOCA!", W / 2, H * 0.92, 56, 2, GREY + "0.35)");
			// 0:24.1 "so be wise... reading the signs of my body": the curtain comes back, and the strings lean towards where the passage is going
			var SEQ = new (double, double)[] { (41, 0), (42.6, 1.6), (44.6, 0), (46.4, -1.6), (49.4, 0) };
			Func<double, double> offB = b => { double v = 0, pv = 0, pb = 40; foreach (var (sb, o) in SEQ) { if (b >= sb) { pv = v; v = o; pb = sb; } } return (pv + (v - pv) * L8ease((b - pb) / 0.7)) * 115; };
			Func<double, double> nextB = b => { foreach (var (sb, o) in SEQ) if (sb > b) return o; return 0; };
			Func<double, double> leanB = b => { double nx = nextB(b), c = offB(b) / 115, d = nx - c; double ne = 99; foreach (var (sb, _) in SEQ) if (sb > b) { ne = sb; break; } return ne - b < 1.1 ? 0.32 * sign(d) : 0; };
			var cB = new (double, Func<double, double, double, double>)[] { (40, (k, x, b) => H / 2 + offB(b)), (49.4, (k, x, b) => H / 2), (51.07, (k, x, b) => H / 2 + 40 * JS.sin(TAU * b)), (53.55, (k, x, b) => H / 2 + wv(x, b, 90, 1)), (56.08, (k, x, b) => H / 2) };
			var hB = new (double, Func<double, double, double, double>)[] { (40, (k, x, b) => G1 / 2 + 50), (49.4, (k, x, b) => G1 / 2 + 35), (51.07, (k, x, b) => G1 / 2 + 45), (53.55, (k, x, b) => G1 / 2 + 40), (56.08, (k, x, b) => G1 / 2 + 40) };
			Func<double, double> swB = b => b < 49.4 ? 0 : (SWA1 + 30) * JS.sin(Math.PI * (b - 49.4) / (4 * SWP)) * Math.Min(1, (b - 49.4) / 1) * (1 - L8ease((b - 56.6) / 1));
			var cyB0 = blend(cB, 0.5); Func<double, double, double, double> cyB = (k, x, b) => cyB0(k, x, b) + swB(b); var hwB = blend(hB, 0.6);
			// 0:33.9 "all the attraction, the tension": the strings are pulled to the middle, then go tight from top to bottom and vibrate like strings
			Func<double, double> attB = b => b < 56.08 ? 0 : b < 57.6 ? L8ease((b - 56.08) / 1.2) : 1 - L8ease((b - 57.6) / 1.3);
			Func<double, double> tenB = b => b < 57.6 ? 0 : L8ease((b - 57.6) / 0.8);
			Func<double, double, double> XB = (k, t) => { double b = BT(t), x = drift(k, b, -1), sl = b >= 53.55 && b < 56.6 ? 24 * (L8step(b) - L8step(53.55)) * (1 - L8ease((b - 56) / 0.6)) : 0; return W / 2 + (x + sl - W / 2) * (1 - 0.5 * attB(b)); };
			Func<double, double, double, double> lenTopB = (k, x, b) => { double L = cyB(k, x, b) - hwB(k, x, b), q = tenB(b); return L * (1 - q) + (H / 2 - 110) * q; };
			Func<double, double, double, double> lenBotB = (k, x, b) => { double L = H - cyB(k, x, b) - hwB(k, x, b), q = tenB(b); return L * (1 - q) + (H / 2 - 110) * q; };
			Func<double, double, double, double> pluck = (k, t, d) => { double b = BT(t); if (b < 57.6) return 0; double pk = 58 + k * 0.12; return b < pk ? 0 : 26 * JS.sin(Math.PI * d / H) * JS.sin((t - B(pk)) * 30) * JS.exp(-(b - pk) * 0.8); };
			Action<int, Func<double, double, double, double>, List<L8Toss>> curtainB = (side, len, toss) => ev(40.8 - 1.5, () => push(L8Curtain(new L8CurtainO
			{
				n = NS, side = side, X = XB, len = (k, t) => len(k, XB(k, t), BT(t)), tilt = (k, t) => { double b = BT(t); return b < 49 ? leanB(b) * (side != 0 ? -1 : 1) * -1 : 0.05 * L8hip(b); }, wave = pluck, tw = B(39.3), ta = B(40.8), te = B(59.8),
				sh = new[] { new[] { 51.07, 53.4 } }, toss = toss
			})));
			curtainB(0, lenTopB, new List<L8Toss> { new L8Toss { b = 52, ev = 3, ph = 0, vx = 60, vy = -100, gr = 620 }, new L8Toss { b = 53, ev = 3, ph = 1, vx = 60, vy = -100, gr = 620 } });
			curtainB(1, lenBotB, new List<L8Toss> { new L8Toss { b = 52, ev = 3, ph = 2, vx = 50, vy = -560, gr = 700 }, new L8Toss { b = 53, ev = 3, ph = 0, vx = 50, vy = -560, gr = 700 } });
			word(49.4, "TONIGHT", W / 2, H / 2, 60, 1.6, GREY + "0.3)"); fx(51.07, 0.45, 14); word(51.07, "HIPS DON'T LIE", W / 2, H / 2, 58, 2.4, GREY + "0.35)");
			// 0:36.1 "don't you see baby, this is perfection": the coins line up in full rings round you and burst out in straight lines
			fx(59.83, 0.35, 10);
			ev(58.9, () => { double cx = clamp(player.x, 260, W - 260), cy = clamp(player.y, 260, H - 260); push(L8Mandala(cx, cy, new[] { new L8MRing { r = 0, n = 1, sp = 0 }, new L8MRing { r = 96, n = 12, sp = 0.7 }, new L8MRing { r = 170, n = 20, sp = -0.5, ph = 0.1 }, new L8MRing { r = 244, n = 28, sp = 0.35 } }, B(58.9), B(59.83), B(62.6), 280 * K1)); });
			word(59.83, "PERFECTION", W / 2, H * 0.9, 50, 2.6, GREY + "0.35)");
			// from 0:36 short coin tassels hang from the flag ropes on top
			ev(59.6, () => push(L8Curtain(new L8CurtainO { n = 22, side = 0, X = (k, t) => W / 44 + k * W / 22, len = (k, t) => { double b = BT(t); return 48 + 22 * JS.sin(k * 1.7) + 18 * JS.sin(Math.PI * b + k); }, tilt = (k, t) => 0.12 * L8hip(BT(t)), tw = B(59.6), ta = B(60.6), te = B(93) })));
			// 0:38.7 Wyclef's part: a giant dancer in the background with a pink belt. 0:41.9 the belt spins madly and throws a spiral, 0:43.7 she dances right and left
			Func<double, double> dSpin = t => { double b = BT(t); return b < 69.4 ? 0 : b < 72.35 ? 3.2 * (t - B(69.4)) : 3.2 * (B(72.35) - B(69.4)); };
			Func<double, double> dX = t => { double b = BT(t); return b < 72.35 ? W / 2 : W / 2 + 150 * JS.sin((L8step(b) - L8step(72.35)) * Math.PI / L8BAR); };
			var spiral = new List<L8Throw>(); for (double b = 69.6; b < 72.3; b += 0.5) spiral.Add(new L8Throw { b = b, v = 240 * K1, ev = 4, ph = (int)(round((b - 69.6) * 2) % 4), gr = 0 });
			{
				var thr = new List<L8Throw> { new L8Throw { b = 65, v = 220 * K1, ev = 3, up = 140, gr = 420 }, new L8Throw { b = 67, v = 230 * K1, ev = 2, ph = 1, up = 120, gr = 420 } }; thr.AddRange(spiral); thr.Add(new L8Throw { b = 73, v = 220 * K1, ev = 3, ph = 1, up = 140, gr = 420 }); thr.Add(new L8Throw { b = 75, v = 240 * K1, ev = 2, ph = 0, up = 120, gr = 420 });
				ev(63.2, () => push(L8Dancer(new L8DancerO { x = dX, y = H / 2 - 10, s = 1.12, A = 70, rx = 165, ry = 44, n = 24, spin = dSpin, t0 = B(63.2), t1 = B(77.6), throws = thr })));
			}
			word(64, "HEY GIRL", W / 2, H * 0.9, 46, 1.6, GREY + "0.3)");
			// 0:46.4 "when you walk up on the dance floor": her footprints stamp in a walking line across the screen, then come back
			Action<double, double, double, double, bool> foot = (b, x, y, ang, mir) => evt(b - 1.2, () => push(L8Foot(x, y, ang, 175, B(b - 1.2), B(b), B(b + 1.6), mir)), "L8Foot(");
			for (int i = 0; i < 6; i++) foot(77 + i, 150 + i * 196, i % 2 != 0 ? H * 0.62 : H * 0.36, 0, i % 2 == 0);
			for (int i = 0; i < 5; i++) foot(80.5 + i, W - 170 - i * 230, i % 2 != 0 ? H * 0.86 : H * 0.14, Math.PI, i % 2 == 1);
			for (int i = 0; i < 4; i++) foot(79.5 + i, W / 2 + (i % 2 != 0 ? 75 : -75), 110 + i * 165, Math.PI / 2, i % 2 == 0);
			// 0:52.9 "the way you right and left it": vertical rows of coins sweep right, then left
			Action<bool, double, double, double, double, double, L8Tier, Action<L8ChainO>, bool> chain = (vert, b0, a0, dir, unit, gc, T_, o, k) =>
				(k ? evk : ev)(b0 - T_.wn, () => { var q = new L8ChainO { vert = vert, a0 = a0, dir = dir, unit = unit * kf(T_), b0 = b0, gc = gc * (vert ? H : W), gw = T_.gap, tw = B(b0 - T_.wn), ta = B(b0), te = double.NaN }; o?.Invoke(q); if (double.IsNaN(q.te)) q.te = B(b0 + 40); else if (easy) q.te = B(b0) + (q.te - B(b0)) * 1.25; push(L8Chain(q)); });
			Func<double, Action<L8ChainO>> te1 = b => q => { q.sag = 40; q.te = B(b + 10.5); };
			// every strong hip shake pushes two lines of coins from her waist to the sides, once right and left and once up and down, the opening every time somewhere else
			Action<double, bool, double, double, double, bool> hipPush = (b, vert, g0, g1, u, inw) =>
			{
				double c = vert ? W / 2 : H / 2, E_ = vert ? W : H;
				Action<L8ChainO, double[]> cl = (q, v) => { if (vert) q.clip = v; else q.clipY = v; };
				if (inw) { chain(vert, b, -16, 1, u, g0, T, q => { q.sag = 26; cl(q, new[] { 0, c }); q.te = B(b + 9); }, false); chain(vert, b, E_ - 10, -1, u, g1, T, q => { q.sag = 26; cl(q, new[] { c, E_ }); q.te = B(b + 9); }, false); return; }
				chain(vert, b, c + 20, -1, u, g0, T, q => { q.sag = -26; cl(q, new[] { 0, c }); q.te = B(b + 9); }, false); chain(vert, b, c - 20, 1, u, g1, T, q => { q.sag = 26; cl(q, new[] { c, E_ }); q.te = B(b + 9); }, false);
			};
			// "Hey girl": the first shake the two lines come from the sides to the dancer, after that they go out from her to the sides
			hipPush(64, true, easy ? 0.12 : 0.18, easy ? 0.86 : 0.8, 110, true); hipPush(68, false, 0.75, 0.3, 60, false); hipPush(72.4, true, 0.75, 0.25, 80, false); hipPush(76.4, false, 0.3, 0.7, 60, false);
			chain(true, 84, -30, 1, 80, 0.2, T, te1(84), false); chain(true, 84, -300, 1, 80, easy ? 0.62 : 0.55, T, te1(88), true);
			chain(true, 86, W - 10, -1, easy ? 100 : 80, 0.6, T, te1(easy ? 91 : 88), false); chain(true, 86, W + 200, -1, easy ? 100 : 80, 0.15, T, te1(easy ? 93 : 90), false);
			// 0:56.2 "so you can keep on taking it": an orderly rain of coins, and the opening walks in a zigzag
			var GX = new[] { 3, 6, 9, 12, 9, 6 };
			for (int i = 0; i < GX.Length; i++) { int gx = GX[i]; double b = 88 + i * 0.9; ev(b - 1, () => { var xs = new List<double>(); for (int c = 0; c < 16; c++) if (Math.Abs(c - gx) > 1) xs.Add(40 + c * 80); push(L8Rain(xs, B(b - 1), B(b), 600, 40)); }); }
			// 0:55 the trumpet phrase closes the phase: a belt falls from the top, shakes and its tassels drop
			evk(93.4, () => push(L8Belt(new L8BeltO { y = t => { double b = BT(t); return b < 96.3 ? -60 + (H * 0.3 + 60) * L8ease((b - 93.4) / 1.6) : H * 0.3 - 400 * JS.pow(clamp((b - 96.3) / 1.2, 0, 1), 2); }, tw = B(93.4), ta = B(94), tu = B(95.2), te = B(98), sh = new[] { new[] { 95.2, 96.3 } }, drop = 96.3 })));

			/* ================= phase 2: the words become real (0:57.8 - 1:36.2) ================= */
			T = TI[1]; double K2 = kf(T);
			fx(96, 0.3, 10);
			ev(95, () => push(L8BG2(new[] { (95.65, 99.83, "BAILA"), (99.83, 103.6, "¿ESPAÑOL?"), (111.5, 115.67, "OH BABY"), (115.67, 119.87, "¡LOCA!"), (119.87, 124.4, "BE WISE"), (124.4, 128.98, "SIGNS"), (128.98, 131.08, "TONIGHT"), (136.07, 139.82, "ATTRACTION"), (139.82, 144.42, "PERFECTION"), (151.92, 155.68, "REAL SLOW"), (155.68, 160.0, "PERFECTO") }, B(95), B(160))));
			// 0:57.6 "I never really knew that she could dance like this": the dancer spins, and her belt lets go growing oval rings
			ev(95.2, () => push(L8Dancer(new L8DancerO { x = t => W / 2, y = H / 2 - 10, s = 1.12, A = 60, rx = 150, ry = 40, n = 22, spin = t => 1.6 * (t - B(95.2)), t0 = B(95.2), t1 = B(100.4), rv = 210 * K2, rip = new[] { 96.9, 97.9, 98.9 } })));
			// 1:00.1 "she make a man want to speak Spanish": Spanish words walk in rows like trains
			var LN = new (double, double, string)[] { (118, 1, "¡HOLA!   BONITA   ¡HOLA!   BONITA   ¡HOLA!"), (284, -1, "¿QUÉ TAL?   AMOR   ¿QUÉ TAL?   AMOR"), (450, 1, "BAILA   CASA   BAILA   CASA   BAILA"), (636, -1, "SEÑORITA   ¡OLÉ!   SEÑORITA   ¡OLÉ!") };
			for (int i = 0; i < LN.Length; i++) { var (y, dir, s) = LN[i]; int i2 = i; double sz = 60, adv = sz * 0.72, len = s.Length * adv; evt(99.83 - 1.2 + i * 0.25, () => push(L8Train(new L8TrainO { s = s, y = t => y + (i2 % 2 != 0 ? -1 : 1) * 26 * JS.sin(Math.PI / 4 * (BT(t) - 99.83)), dir = dir, v = 290 * K2, size = sz, x0 = dir > 0 ? -len + 60 : W - 60, tw = B(98.63 + i2 * 0.25), ta = B(99.83 + i2 * 0.25), te = B(102.9) })), "L8Train("); }
			// 1:02.4 "Cómo se llama, bonita, mi casa, su casa": two houses, the flood with every "sí", and "¡SÍ!" stamps
			var HS = new[] { new L8House { x = W / 2 - 130, y = H / 2 + 20, label = "MI CASA" }, new L8House { x = W / 2 + 130, y = H / 2 + 20, label = "SU CASA" } };
			// Easy: the flood's warning is one and a half times longer
			// Easy: the first flood of every group warns 3.5 beats before (time to reach the house from anywhere)
			Func<(double w, double a, int s)[], List<L8Flood>> floods = fl => fl.Select((q, i) => new L8Flood { tw = B(easy ? (i != 0 ? q.a - 1.5 * (q.a - q.w) : q.a - 3.5) : q.w), ta = B(q.a), te = B(q.a) + (easy ? 0.24 : 0.32), s = q.s }).ToList();
			var FL2 = new (double, double, int)[] { (103, 104.1, 0), (104.6, 105.1, 0), (105.6, 107, 1), (107.45, 107.95, 1) };
			ev(easy ? 100.6 : 102.9, () => push(L8Houses(HS, B(easy ? 100.6 : 102.9), B(108.6), floods(FL2))));
			foreach (var (_, a, s) in FL2) word(a, "¡SÍ!", s != 0 ? W - 180 : 180, H * 0.18, 64, 0.9, GREY + "0.5)");
			// 1:05.2 "Shakira, Shakira": the letters stamp left to right, then right to left, and break into coins
			const string NAME = "SHAKIRA";
			Action<double, double, double, bool, double, double, Func<int, double[]>> letters = (b, y, x0, rev, te, tw, org) => evt(b - tw, () => { for (int i = 0; i < 7; i++) { int j = rev ? 6 - i : i; push(L8Letter(NAME[j].ToString(), x0 + j * 147, y, 130, B(b - tw), B(b + i * 0.14), B(te), org != null ? org(j) : null)); } }, "L8Letter(");
			letters(108.2, H * 0.27, 200, false, 110.7, T.wn, null);
			letters(109.4, H * 0.74, 200, true, 111.3, T.wn + 0.8, null);
			// 1:07.1 "Oh baby when you talk like that": lips on the sides talk sound rings with an opening
			var TK = new (double b, int sd, double gc)[] { (112, 0, 0.42), (112.75, 1, -0.4), (113.5, 0, -0.1), (114.5, 1, 0.5), (115, 0, 0.3) };
			Func<double, double> lipSpin = t => { double b = BT(t); return b < 115.67 ? 0 : Math.Min(b - 115.67, 4) * 2.2; };
			ev(111.2, () => { push(L8Lips(0, H / 2, B(111.2), B(120.2), TK.Where(q => q.sd == 0).Select(q => q.b).ToArray(), lipSpin)); push(L8Lips(1, H / 2, B(111.2), B(120.2), TK.Where(q => q.sd == 1).Select(q => q.b).ToArray(), t => -lipSpin(t))); });
			Action<double, int, double, L8Tier, double, double> talk = (b, sd, gc, T_, gh, hw) => evt(b, () =>
			{
				double x = sd != 0 ? W - 70 : 70, a0 = sd != 0 ? Math.PI : 0, t = B(b); double lo = -hw, hi = hw, g0 = gc - gh, g1 = gc + gh;
				push(L8Wave(x, H / 2, a0 + (lo + g0) / 2, (g0 - lo) / 2, t, T_.sp * 1.05, 20)); push(L8Wave(x, H / 2, a0 + (g1 + hi) / 2, (hi - g1) / 2, t, T_.sp * 1.05, 20));
			}, "L8Wave(");
			foreach (var (b, sd, gc) in TK) talk(b, sd, gc, T, 0.22, 1.05);
			// 1:09.6 "you make a woman go mad": a whirl of coin arms turns round the middle, reverses madly, then shrinks
			Func<double, double> spR = b => b < 120 ? 660 : 660 * (1 - L8ease((b - 120) / 3.6));
			Func<double, double> spRot = t => { double b = BT(t), s = B(115.67), m = B(117.8); if (t < s) return 0.3; return 0.3 + (t < m ? 0.7 * (t - s) + 0.1 * (t - s) * (t - s) : 0.7 * (m - s) + 0.1 * (m - s) * (m - s) - 0.85 * (t - m)); };
			ev(114.4, () => push(L8Spin(new L8SpinO { cx = W / 2, cy = H / 2, arms = 4, R = t => spR(BT(t)), rot = spRot, gaps = new[] { new double[] { 180, 280 }, new double[] { 330, 430 } }, tw = B(114.4), ta = B(115.67), te = B(123.6) })));
			// 1:12.2 "So be wise and keep on reading the signs of my body": four signs come up one after another, then are carried out in order
			ev(119.87, () => push(L8Signs(new[] { 0, 3, 2, 1 }, new[] { B(120), B(121), B(122), B(123) }, new[] { B(124.5), B(125.75), B(127), B(128.25) }, 0.34)));
			// 1:17.6 "And I'm on tonight": pink stage lights switch on from the top and sweep
			{
				var SP = new (double, double)[] { (W * 0.2, 0), (W * 0.5, 1.3), (W * 0.8, 2.6) };
				for (int i = 0; i < SP.Length; i++) { var (x, ph) = SP[i]; int i2 = i; ev(128.1 + i * 0.15, () => push(L8Spot(x, t => Math.PI / 2 + 0.62 * JS.sin((t - B(129)) * 2.1 + ph), 0.09, B(128.1 + i2 * 0.15), B(129), B(132.2)))); }
			}
			// 1:18.9 "You know my hips don't lie": the lie detector! The needle draws a line swaying with the hips,
			// "don't lie" the line calms down, and "starting to feel it's right" it climbs stairs with the steps. The line breaks every two beats and you go through
			fx(131.08, 0.4, 12); word(131.08, "HIPS DON'T LIE", W / 2, H * 0.1, 46, 2.2, GREY + "0.45)");
			Func<double, double> n1 = u => { double b = BT(u); if (b < 132.6) return H / 2 + 120 * JS.sin(Math.PI / 2 * (b - 131.08)); if (b < 133.2) return H / 2 + 120 * JS.sin(Math.PI / 2 * (132.6 - 131.08)) * (1 - L8ease((b - 132.6) / 0.5)) + 12 * JS.sin(b * 20); return H / 2 - 54 * (L8step(b) - L8step(133.2)); };
			Func<double, bool> pen1 = u => { double b = BT(u); return ((b - 131.08) % 2 + 2) % 2 < 1.25; };
			ev(129.9, () => push(L8Graph(new L8GraphO { NX = 12, rev = true, v = 430 * K2, th = 14, pre = W / (430 * K2) + 0.3, nd = new List<L8Nd> { new L8Nd { y = n1, pen = pen1 } }, tw = B(129.9), ta = B(131.08), te = B(136.9) })));
			// 1:21.9 "All the attraction, the tension": two coin walls pulled to the middle, then a tight rope shakes and snaps
			fx(136.07, 0.3, 8);
			ev(135.2, () => push(L8Pull(H * (easy ? 0.8 : 0.9), H * 0.6, T.gap, B(135.2), B(136.07), B(138))));
			ev(137.1, () => push(L8Rope(H * (easy ? 0.89 : 0.8), B(137.1), B(138), B(139.8), 110)));
			// 1:24.1 "Don't you see baby, this is perfection": a coin mandala (3 rings turning against each other) gathers and bursts
			fx(139.82, 0.4, 12);
			ev(138.6, () => push(L8Mandala(W / 2, H / 2, new[] { new L8MRing { r = 130, n = 14, sp = 0.6 }, new L8MRing { r = 240, n = 22, sp = -0.45, ph = 0.14 }, new L8MRing { r = 350, n = 30, sp = 0.35 }, new L8MRing { r = 470, n = 40, sp = -0.25, ph = 0.05 } }, B(138.6), B(139.82), B(143.2), 290 * K2)));
			// 1:26.9 "my hips don't lie" again: the lie detector with two needles (top and bottom), and 1:31.4 "real slow": the machine slows to half
			fx(144.42, 0.4, 12);
			double sl0 = B(151.92); Func<double, double> wp2 = t => t < sl0 ? t : sl0 + (t - sl0) * 0.5;
			// the two needles walk together: the whole paper swings up and down (reaching the sides and the middle), and the space between them breathes with the beat
			Func<double, double> m2 = b => H / 2 - 170 * JS.sin(Math.PI / 4 * (b - 144.42)) + 35 * JS.sin(Math.PI / 2 * (b - 144.42));
			Func<double, double> nT = u => { double b = BT(u); return m2(b) - 165 - 20 * JS.sin(Math.PI * b); };
			Func<double, double> nB = u => { double b = BT(u); return m2(b) + 165 + 20 * JS.sin(Math.PI * b); };
			ev(143.2, () => push(L8Graph(new L8GraphO { NX = W - 12, v = 430 * K2, th = 14, pre = W / (430 * K2) + 0.3, wp = wp2, nd = new List<L8Nd> { new L8Nd { y = nT, pen = u => ((BT(u) - 144.42) % 4 + 4) % 4 < 2.6 }, new L8Nd { y = nB, pen = u => ((BT(u) - 146.42) % 4 + 4) % 4 < 2.6 } }, tw = B(143.2), ta = B(144.42), te = B(155.6) })));
			word(151.92, "real slow...", W / 2, H * 0.5, 54, 3.4, GREY + "0.45)");
			// 1:33.6 "así es perfecto / Shakira, Shakira": the letters one last time right to left, and they break into coins to close the phase
			fx(155.68, 0.4, 12);
			letters(155.68, H / 2, 200, true, 158.6, T.wn, null);

			/* ================= phase 3: the band (1:36.2 - 2:24.6) ================= */
			T = TI[2]; double K3 = kf(T);
			evk(158, () => push(L8BG3(B(158), B(250))));
			var RF = new[] { 0, 0.75, 1.5, 2.5, 3.5 }; var BR = new[] { 0, 0.75, 1.5, 2.5 };
			const double DN = Math.PI / 2, UP = -Math.PI / 2;
			Func<int, double, double[]> orbP = (i, b) => { double th = -Math.PI / 2 + TAU * i / 3 + 0.5 * (B(b) - B(168.6)); double x = W / 2 + 300 * JS.cos(th), y = H / 2 + 240 * JS.sin(th); return new[] { x, y, JS.atan2(H / 2 - y, W / 2 - x) + 0.45 }; };
			var TX = new[] { W * 0.25, W * 0.5, W * 0.75 };
			// the trumpets' key points: [beat, x, y, angle, size]
			Func<double, double[], double[]> kp = (b, p) => new[] { b }.Concat(p).ToArray();
			var KFS = new[] { 0, 1, 2 }.Select(i =>
			{
				double x = TX[i]; var top = new[] { x, i == 1 ? 190 : 205, DN }; var o0 = orbP(i, 168.6); var o1 = orbP(i, 173.2);
				var plan = new[] { new[] { 200, 190, 0.55 }, new[] { W / 2, H - 190, UP }, new[] { W - 200, 190, Math.PI - 0.55 } }[i];
				var lc = new[] { 215, H * (0.2 + 0.3 * i), 0 }; var rc = new[] { W - 215, H * (0.2 + 0.3 * i), Math.PI }; var bt = new[] { x, H - 200, UP };
				return new[] { new[] { 158, x, H + 180, UP }, new[] { 159.6, x, H - 200, UP }, new[] { 165.3, x, H - 200, UP }, kp(166.2, top), kp(168.6, o0), kp(173.2, o1), kp(173.9, plan), kp(176.6, plan), kp(177.4, top),
					kp(191.2, top), kp(192, lc), kp(195.8, lc), kp(196.6, rc), kp(199.5, rc), kp(200.3, bt), kp(203.7, bt), kp(204.5, top), kp(236.5, top), new[] { 237.6, W / 2, 250, DN, i == 1 ? 1.5 : 1 }, new[] { 999, W / 2, 250, DN, i == 1 ? 1.5 : 1 } };
			}).ToArray();
			var bas = KFS.Select(k => L8path(k)).ToArray();
			// 2:16.6 "go mad": the trumpets spin round themselves and blow a spiral
			Func<double, double> madA = b => b < 227.33 ? 0 : b < 231.2 ? 2.4 * (B(b) - B(227.33)) : 2.4 * (B(231.2) - B(227.33));
			var PF = new[] { 0, 1, 2 }.Select(i => (Func<double, double[]>)(t => { double b = BT(t); if (b >= 168.6 && b < 173.2) return orbP(i, b).Concat(new double[] { 1 }).ToArray(); var p = bas[i](t); if (b >= 227.33 && b < 232) { double m = madA(b) * (i == 1 ? -1 : 1); double e = b < 231.2 ? 1 : 1 - L8ease((b - 231.2) / 0.8); p[2] += m * e + (1 - e) * 0; } return p; })).ToArray();
			foreach (var i in new[] { 0, 1, 2 }) evk(158, () => push(L8Trumpet(PF[i], B(158), B(i == 1 ? 240 : 237.6), i == 1 ? B(236.5) : 0, i != 1)));
			Action<int, double, int[], L8RiffO> riff = (i, b, vs, o) => { o ??= new L8RiffO(); double lead = L8or(o.lead, 1); var rf = o.rf ?? RF; ev(b - lead, () => push(L8Riff(PF[i], vs, L8or(o.D, 0.55), L8or(o.hw, 0.3) * 0.9, B(b - lead), rf.Select(r => B(b + r)).ToArray(), T.sp * L8or(o.sm, 1.15), L8or(o.th, 22), o.mr))); };
			fx(160, 0.5, 18); word(160, "¡BAILA!", W / 2, H * 0.4, 70, 2.2, GREY + "0.35)");
			// 1:36.1 "Oh boy, I can see your body moving": the band comes in from the bottom and blows the famous phrase in turns
			chain(false, 158.6, H - 10, -1, 96, 0.62, T, q => { q.sag = 30; q.te = B(166); }, true); chain(false, 160.2, -20, 1, 96, 0.3, T, q => { q.sag = 30; q.te = B(168); }, false);
			riff(1, 160, new[] { 1 }, null); riff(0, 161.5, new[] { 1, 2 }, new L8RiffO { rf = BR }); riff(2, 163, new[] { 0, 1 }, new L8RiffO { rf = BR });
			// 1:39.4 "Half animal, half man": the screen in two halves: in one half the ropes go down and in the other they go up, then they swap
			ev(165, () =>
			{
				var hz = new Hz(); hz.update = (t, dt) => { if (t > B(169.2)) hz.dead = true; };
				hz.draw = t => { if (t < B(165)) return; double a = min(1, (t - B(165)) / 0.3, (B(169.2) - t) / 0.3); g.globalAlpha = a; g.strokeStyle = GREY + "0.5)"; g.lineWidth = 4; g.setLineDash(new double[] { 16, 12 }); g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke(); g.setLineDash(new double[] { }); g.globalAlpha = 1; };
				push(hz);
			});
			chain(false, 165.3, -20, 1, 62, 0.3, T, q => { q.clip = new[] { 0, W / 2 }; q.sag = 30; }, false); chain(false, 165.3, H - 10, -1, 62, 0.78, T, q => { q.clip = new[] { W / 2, W }; q.sag = 30; }, false);
			chain(false, 167.3, H - 10, -1, 62, 0.22, T, q => { q.clip = new[] { 0, W / 2 }; q.sag = 30; }, false); chain(false, 167.3, -20, 1, 62, 0.62, T, q => { q.clip = new[] { W / 2, W }; q.sag = 30; }, false);
			// 1:41.4 "I don't, don't really know what I'm doing": the trumpets turn round the middle as if lost and blow in turns
			for (int j = 0; j < 7; j++) riff(j % 3, 169.2 + j * 0.6, new[] { 1 }, new L8RiffO { rf = new double[] { 0 }, lead = 0.8, hw = 0.3 });
			// 1:44.1 "but you seem to have a plan": a faint blueprint is drawn, then the blowing walks on its lines in order
			var PL = new (int, double)[] { (0, 0.25), (0, 0.85), (2, Math.PI - 0.25), (2, Math.PI - 0.85), (1, UP - 0.5), (1, UP + 0.5) };
			for (int j = 0; j < PL.Length; j++) { var (i, a) = PL[j]; int j2 = j; var p = new[] { new double[] { 200, 190 }, new[] { W / 2, H - 190 }, new[] { W - 200, 190 } }[i]; ev(173.6 + j * 0.1, () => push(L8Beam(p[0], p[1], a, 1500, 30, B(173.6 + j2 * 0.1), B(175.2 + j2 * 0.42), B(175.2 + j2 * 0.42) + 0.32))); }
			word(173.17, "PLAN", W / 2, H / 2, 64, 2, GREY + "0.35)");
			// 1:45.6 "My will and self-restraint": chains tie the trumpets. 1:47.9 "fail now, fail now": they snap and a ceiling of coins falls
			ev(176.6, () => { push(L8Bind(PF[0], 0, H * 0.78, B(176.6), B(177.6), B(180))); push(L8Bind(PF[2], W, H * 0.78, B(176.6), B(177.6), B(180))); push(L8Bind(PF[1], W / 2, H + 10, B(176.6), B(177.6), B(181.8))); });
			riff(1, 178, new[] { 0, 2 }, new L8RiffO { rf = new[] { 0, 0.75, 1.5 } });
			Action<double, double> ceil = (b, gx) => ev(b - 1, () => { var xs = new List<double>(); for (double x = 13; x < W; x += 26) if (Math.Abs(x - gx) > (easy ? 110 : 70)) xs.Add(x); push(L8Rain(xs, B(b - 1), B(b), 650, 30)); });
			ceil(180, W * 0.3); ceil(181.8, W * 0.72); ceil(186, W * 0.5); word(186, "I CAN'T", W / 2, H * 0.5, 56, 1.4, GREY + "0.35)"); word(180, "FAIL NOW", W / 2, H * 0.5, 60, 1.6, GREY + "0.35)"); word(181.8, "FAIL NOW", W / 2, H * 0.5, 60, 1.4, GREY + "0.35)");
			// 1:50.4 "See, I am doing what I can, but I can't": the trumpets try and let out small puffs in rows
			var PU = new (int, double, int[])[] { (0, 184, new[] { 0, 1 }), (2, 184.5, new[] { 1, 2 }), (1, 185, new[] { 0, 2 }), (0, 185.75, new[] { 1, 2 }), (2, 186.25, new[] { 0, 1 }), (1, 186.75, new[] { 1 }), (0, 187.25, new[] { 0, 2 }), (2, 187.6, new[] { 0, 2 }) };
			foreach (var (i, b, vs) in PU) riff(i, b, vs, new L8RiffO { rf = new double[] { 0 }, lead = 0.7, hw = 0.4, mr = 300, D = 0.5 });
			// 1:53.1 "that's a bit too hard to explain": the three blow one big blow together
			fx(188.6, 0.45, 14);
			riff(0, 188.6, new[] { 2 }, new L8RiffO { rf = BR, lead = 1.2, hw = 0.26, th = 26 }); riff(1, 188.6, new[] { 1 }, new L8RiffO { rf = BR, lead = 1.2, hw = 0.26, th = 26 }); riff(2, 188.6, new[] { 0 }, new L8RiffO { rf = BR, lead = 1.2, hw = 0.26, th = 26 });
			// 1:55.1 "Baila en la calle de noche / de día" x4: a street parade, every line the band moves to a side and blows along the street, and lantern ropes swing
			word(191.5, "¡BAILA EN LA CALLE!", W / 2, H * 0.5, 54, 2.4, GREY + "0.35)");
			ev(191.2, () => push(L8Street(true, 1, B(191.2), B(199.6)))); ev(199.6, () => push(L8Street(false, -1, B(199.6), B(207.7))));
			riff(0, 192.6, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.26 }); riff(2, 192.6, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.26 }); riff(1, 194.6, new[] { 0, 2 }, new L8RiffO { rf = new[] { 0, 0.75, 1.5 }, hw = 0.24 });
			chain(false, 193, -30, 1, 52, 0.62, T, q => { q.lan = true; q.sag = 40; q.te = B(201); }, false);
			riff(1, 196.9, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.28 }); riff(0, 198.6, new[] { 2 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24 }); riff(2, 198.6, new[] { 0 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24 });
			chain(false, 197, H - 10, -1, 52, 0.45, T, q => { q.lan = true; q.sag = 40; q.te = B(205); }, false);
			riff(0, 200.7, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.26 }); riff(2, 200.7, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.26 }); riff(1, 202.6, new[] { 0, 2 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24 });
			chain(true, 201, -30, 1, 70, 0.6, T, q => { q.lan = true; q.sag = 40; q.te = B(209); }, false);
			riff(1, 204.9, new[] { 1 }, new L8RiffO { rf = BR, hw = 0.28 }); riff(0, 206.6, new[] { 2 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24 }); riff(2, 206.6, new[] { 0 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24 });
			chain(true, 205, W - 10, -1, 70, easy ? 0.5 : 0.35, T, q => { q.lan = true; q.sag = 40; q.te = B(213); }, false);
			// 2:04.9 "I never really knew...": the dancer comes back behind the band, and her belt throws coins
			fx(207.73, 0.4, 12);
			ev(207, () => push(L8Dancer(new L8DancerO { x = t => W / 2, y = H / 2 + 40, s = 1.0, A = 70, rx = 150, ry = 40, n = 22, spin = t => 0, t0 = B(207), t1 = B(211.8), throws = new List<L8Throw> { new L8Throw { b = 209, v = 230 * K3, ev = 2, up = 120, gr = 420 }, new L8Throw { b = 211, v = 230 * K3, ev = 2, ph = 1, up = 120, gr = 420 } } })));
			riff(1, 208.6, new[] { 0, 2 }, new L8RiffO { rf = new[] { 0, 0.75, 1.5, 2.5 } });
			// 2:07.1 "speak Spanish": two rows of words, and the trumpets blow
			{
				var TR = new (double, double, string)[] { (H * 0.42, 1, "¡HOLA!   BONITA   ¡OLÉ!"), (H * 0.8, -1, "¿QUÉ TAL?   AMOR   BAILA") };
				for (int i = 0; i < TR.Length; i++) { var (y, dir, s) = TR[i]; int i2 = i; double sz = 56, len = s.Length * sz * 0.72; evt(210.4 + i * 0.3, () => push(L8Train(new L8TrainO { s = s, y = t => y, dir = dir, v = 300 * K3, size = sz, x0 = dir > 0 ? -len + 60 : W - 60, tw = B(210.4 + i2 * 0.3), ta = B(211.5 + i2 * 0.3), te = B(214.8) })), "L8Train("); }
			}
			riff(0, 212.6, new[] { 2 }, new L8RiffO { rf = new[] { 0, 0.75, 1.5 } });
			// 2:09.6 "mi casa, su casa": the two houses come back
			var FL3 = new (double, double, int)[] { (215.1, 216.8, 1), (217.4, 218.6, 0) };
			ev(easy ? 213.3 : 214.9, () => push(L8Houses(HS, B(easy ? 213.3 : 214.9), B(219.2), floods(FL3))));
			foreach (var (_, a, s) in FL3) word(a, "¡SÍ!", s != 0 ? W - 180 : 180, H * 0.85, 64, 0.9, GREY + "0.5)");
			// 2:11.9 "Shakira, Shakira": the letters come out of the trumpets' bells and stamp
			letters(219.42, H * 0.3, 200, false, 221.6, T.wn + 0.6, j => { var p = PF[j < 3 ? 0 : j < 5 ? 1 : 2](B(218)); return new[] { p[0], p[1] }; });
			letters(220.6, H * 0.74, 200, true, 222.6, T.wn + 0.8, j => { var p = PF[j < 3 ? 0 : j < 5 ? 1 : 2](B(219.4)); return new[] { p[0], p[1] }; });
			// 2:14.1 "Oh baby when you talk like that": the lips come back with the trumpets
			var TK3 = new (double b, int sd, double gc)[] { (223.8, 0, 0.35), (224.6, 1, -0.3), (225.4, 0, -0.45), (226.2, 1, 0.4) };
			ev(223.1, () => { push(L8Lips(0, H / 2, B(223.1), B(227.3), TK3.Where(q => q.sd == 0).Select(q => q.b).ToArray())); push(L8Lips(1, H / 2, B(223.1), B(227.3), TK3.Where(q => q.sd == 1).Select(q => q.b).ToArray())); });
			foreach (var (b, sd, gc) in TK3) talk(b, sd, gc, T, 0.22, 1.05);
			// 2:16.6 "you make a woman go mad": the trumpets spin madly and spray spiral waves
			for (int i = 0; i < 3; i++) { var ts = new List<double>(); for (double b = 227.8 + i * 0.23; b < 231; b += 0.7) ts.Add(b - 227.8 - i * 0.17); riff(i, 227.8 + i * 0.17, new[] { 1 }, new L8RiffO { rf = ts.ToArray(), lead = 0.6, hw = 0.2, sm = 0.95 }); }
			// 2:19.1 "so be wise... reading the signs": the signs one last time
			ev(231.4, () => push(L8Signs(new[] { 2, 1, 0 }, new[] { B(231.5), B(232.1), B(232.7) }, new[] { B(233.6), B(234.85), B(236.1) }, 0.34)));
			// 2:21.9 the trumpets gather into one giant and charge, a last blow, and it explodes 2:24.6 (the explosion is harmless)
			riff(1, 237, new[] { 0, 2 }, new L8RiffO { rf = new[] { 0, 0.75 }, hw = 0.24, lead = 0.9 });
			riff(1, 239, new[] { 1 }, new L8RiffO { rf = new[] { 0, 0.5 }, lead = 2.6, hw = 0.55 });
			word(239.6, "¡SEÑORITA!", W / 2, H * 0.62, 60, 1.6, GREY + "0.4)");
			fx(240, 0.7, 0);
			ev(237, () => push(L8Conga(B(237), B(250), 237))); ev(240, () => push(L8Confetti(B(240), B(250))));
			sort(E, (a, b) => a.b - b.b);
			// Easy: the small attacks (blows, rings, letters, feet, talk waves...) are taken out every other one, and the big attacks that cover the screen all stay
			// (so there is no place in Easy where you can stand AFK)
			if (easy) { var m = new Dictionary<string, int>(); return E.Where(e => { if (e.keep) return true; if (!TAG.TryGetValue(e, out var k)) return true; m[k] = (m.TryGetValue(k, out var c) ? c : 0) + 1; return m[k] % 2 == 1; }).ToList(); }
			return E;
		}
	}
}
