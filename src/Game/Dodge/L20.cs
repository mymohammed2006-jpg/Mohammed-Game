using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L20 "New Game" (Nitro Fun): you started a new game, and every part of the song is an old arcade game turned against you.
// part 1 switching on (the start menu, Tetris, the loading ring and the loading bars, Pong), part 2 playing (Space Invaders, the glitch,
// a rhythm game whose notes come with the song, the screen closing in), part 3 the boss (a mothership: mirrored sprays, an aimed laser,
// the Tempest tube, the flip, then every game of the level at once while it circles the arena, its end).
// Every hazard is a pure function of time once it exists (the aimed ones also of where the square was), so hit(p, t) is right for any later t.
// Only pink hurts. Warnings are grey/white dashed lines with a faint pink fill. The data is data/dodge_L20.json (made by tools/level20/gen.py).
public static partial class G
{
	static double L20k(double t, double a, double b) => b <= a ? (t >= b ? 1 : 0) : clamp((t - a) / (b - a), 0, 1);
	static string L20P(double a) => $"rgba(255,42,109,{str(clamp(a, 0, 1))})";
	static string L20G(double a) => GREY + str(clamp(a, 0, 1)) + ")";
	const string L20HOT = "#ff6d98";   // the moment a thing turns on (still pink)
	static double L20Ease(double k) { k = clamp(k, 0, 1); return k * k * (3 - 2 * k); }
	static void L20Dash(double x0, double y0, double x1, double y1, string col, double w = 2)
	{
		g.strokeStyle = col; g.lineWidth = w; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); g.setLineDash(new double[0]);
	}
	static double L20Seg(double px, double py, double x0, double y0, double x1, double y1)
	{
		double dx = x1 - x0, dy = y1 - y0, l2 = dx * dx + dy * dy; double u = l2 < 1e-9 ? 0 : clamp(((px - x0) * dx + (py - y0) * dy) / l2, 0, 1);
		return hypot(px - x0 - u * dx, py - y0 - u * dy);
	}
	// the square touches the box x0..x1, y0..y1 (2 px of mercy like the other levels)
	static bool L20In(Player p, double x0, double y0, double x1, double y1) => p.x + p.r - 2 > x0 && p.x - p.r + 2 < x1 && p.y + p.r - 2 > y0 && p.y - p.r + 2 < y1;
	// a pink block with a bevel (Tetris cells, notes, invaders' lasers' ends)
	static void L20Cell(double x, double y, double w, double h, string col)
	{
		g.fillStyle = col; g.fillRect(x, y, w, h);
		g.fillStyle = "rgba(255,190,210,0.35)"; g.fillRect(x, y, w, 4); g.fillRect(x, y, 4, h);
		g.fillStyle = "rgba(70,0,22,0.35)"; g.fillRect(x, y + h - 4, w, 4); g.fillRect(x + w - 4, y, 4, h);
	}
	static double L20Tri(double x, double p) { double m = ((x % (2 * p)) + 2 * p) % (2 * p); return m < p ? m : 2 * p - m; }

	// ---------- the boss's frame ----------
	// The boss sits on an ellipse round the middle at angle th (0 = the top) and "down" points away from it. Before the flip th is 0, so the
	// frame is the screen moved so (0,0) is the boss at (640,80). The angle plan is set by the L20rot event (beats rb, degrees ra, eased).
	static double[] _l20rb = { 0 }, _l20ra = { 0 };
	public static double L20Th(double bt)
	{
		var b = _l20rb; var a = _l20ra; if (bt <= b[0]) return a[0] * Math.PI / 180;
		for (int i = 0; i + 1 < b.Length; i++) if (bt < b[i + 1]) { double k = (bt - b[i]) / (b[i + 1] - b[i]); return (a[i] + (a[i + 1] - a[i]) * (i % 2 == 0 ? L20Ease(k) : k)) * Math.PI / 180; }
		return a[^1] * Math.PI / 180;
	}
	public static (double x, double y) L20BossAt(double th)
	{
		double s = JS.sin(th), c = JS.cos(th), D = 1 / Math.Sqrt(s * s / (560.0 * 560) + c * c / (280.0 * 280));
		return (W / 2 + s * D, H / 2 - c * D);
	}
	// a hazard made in the boss's frame (u across, v away from the boss): drawn turned, and the square is turned back to test it
	public static Hz L20Frame(Hz z)
	{
		var h = new Hz { kind = z.kind };
		h.update = (t, dt) => { z.update(t, dt); h.dead = z.dead; };
		h.draw = t => { double th = L20Th(BT(t)); var b = L20BossAt(th); g.save(); g.translate(b.x, b.y); g.rotate(th); z.draw(t); g.restore(); };
		h.hit = (p, t) =>
		{
			double th = L20Th(BT(t)); var b = L20BossAt(th); double dx = p.x - b.x, dy = p.y - b.y, c = JS.cos(th), s = JS.sin(th);
			return z.hit(new Player { x = dx * c + dy * s, y = -dx * s + dy * c, r = p.r }, t);
		};
		return h;
	}

	// ---------- the arcade itself (harmless) ----------
	// one background for the whole level (so it is always under the attacks): the CRT grid, the stars of part 2, the boss's den in part 3
	public static Hz L20Stage(double t1)
	{
		var h = new Hz { kind = "L20Stage" };
		h.update = (t, dt) => { if (t > t1 + 3) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t), a = Math.Min(1, t / 0.8); if (a <= 0) return;
			double p2 = L20k(bt, 110, 112.5), p3 = L20k(bt, 174, 176.5), th = L20Th(bt);
			g.save(); g.globalAlpha = a;
			g.fillStyle = $"rgba({str(4 + 22 * p3)},{str(6 - 4 * p3)},{str(18 - 8 * p3)},0.85)"; g.fillRect(0, 0, W, H);
			// the grid (turns with the boss in part 3)
			g.save(); g.translate(W / 2, H / 2); g.rotate(th); g.strokeStyle = $"rgba(110,130,200,{str((0.07 + 0.06 * pulse) * (1 - 0.6 * p2 + 0.6 * p3))})"; g.lineWidth = 1; g.beginPath();
			for (double x = -800; x <= 800; x += 80) { g.moveTo(x, -800); g.lineTo(x, 800); }
			for (double y = -800; y <= 800; y += 80) { g.moveTo(-800, y); g.lineTo(800, y); }
			g.stroke(); g.restore();
			// part 2: stars falling past
			if (p2 > 0 && p3 < 1)
			{
				g.fillStyle = $"rgba(220,230,255,{str(0.5 * p2 * (1 - p3))})";
				for (int i = 0; i < 70; i++) { double sx = (i * 383.7) % W, sp = 60 + (i * 37) % 140, sy = ((i * 211.3 + t * sp) % (H + 20)) - 10; g.fillRect(sx, sy, i % 5 == 0 ? 3 : 2, i % 5 == 0 ? 3 : 2); }
			}
			// part 3: the den's speed lines out of the boss
			if (p3 > 0)
			{
				var b = L20BossAt(th); g.strokeStyle = $"rgba(255,170,190,{str(0.05 * p3 + 0.04 * pulse * p3)})"; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 24; i++) { double q = i * TAU / 24 + bt * 0.05, r0 = 120 + (t * 300 + i * 53) % 200; g.moveTo(b.x + JS.cos(q) * r0, b.y + JS.sin(q) * r0); g.lineTo(b.x + JS.cos(q) * (r0 + 160), b.y + JS.sin(q) * (r0 + 160)); }
				g.stroke();
			}
			// CRT scanlines and the dark round the edges
			g.fillStyle = "rgba(0,0,0,0.16)"; for (double y = 0; y < H; y += 4) g.fillRect(0, y, W, 1);
			var gr = g.createRadialGradient(W / 2, H / 2, 300, W / 2, H / 2, 780); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, "rgba(0,0,0,0.6)"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			g.restore();
		};
		return h;
	}

	// ---------- part 1: switching on ----------
	// the start menu: 5 rows. A selector (the pink bar) jumps to row sr[k] at beat sb[k] (the next row is outlined a beat before).
	// A second selector (qb, qr) can join. "Select" scans: from beat cb[k] a vertical line crosses the screen in cd beats (dir cs[k]) and
	// only its gap (row cg[k]) is safe; the gap row and the starting edge show a beat before. Grey from tw (beats), it kills from ta
	static readonly string[] L20MENU = { "NEW GAME", "CONTINUE", "STAGE SELECT", "OPTIONS", "EXIT" };
	public static Hz L20Menu(double[] sb, double[] sr, double[] qb, double[] qr, double[] cb, double[] cs, double[] cg, double cd, double tw, double ta, double te)
	{
		const int N = 5; double rh = H / N; var h = new Hz { kind = "L20Menu" };
		double rowAt(double[] b, double[] r, double bt, out int k)
		{
			// a menu cursor: it jumps, it never slides over the rows between
			k = -1; for (int i = 0; i < b.Length; i++) if (bt >= b[i]) k = i; return k < 0 ? -1 : r[k] * rh;
		}
		double scanX(int i, double bt) { double k = (bt - cb[i]) / cd; return cs[i] > 0 ? -30 + k * (W + 60) : W + 30 - k * (W + 60); }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.6)); bool on = bt >= ta && bt < te;
			g.save(); g.globalAlpha = a;
			// the rows and their names
			g.font = $"800 34px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			for (int i = 0; i < N; i++) { g.strokeStyle = L20G(0.12); g.lineWidth = 2; g.strokeRect(14, i * rh + 6, W - 28, rh - 12); g.fillStyle = L20G(0.16); g.fillText(L20MENU[i], W / 2, i * rh + rh / 2); }
			// the select scans: the gap row to stand in, then the line
			for (int i = 0; i < cb.Length; i++)
			{
				if (bt < cb[i] - 2 || bt > cb[i] + cd) continue; double gy = cg[i] * rh;
				if (bt < cb[i])
				{
					double k = L20k(bt, cb[i] - 2, cb[i]); g.strokeStyle = $"rgba(255,255,255,{str(0.35 + 0.5 * k)})"; g.lineWidth = 3; g.strokeRect(10, gy + 8, W - 20, rh - 16);
					double x0 = cs[i] > 0 ? 12 : W - 12; L20Dash(x0, 0, x0, gy, L20G(0.4 + 0.5 * k), 3); L20Dash(x0, gy + rh, x0, H, L20G(0.4 + 0.5 * k), 3);
					g.fillStyle = L20P(0.05 + 0.18 * k); g.fillRect(x0 - 25, 0, 50, gy); g.fillRect(x0 - 25, gy + rh, 50, H - gy - rh);
					continue;
				}
				double x = scanX(i, bt); g.strokeStyle = "rgba(255,255,255,0.55)"; g.lineWidth = 2; g.strokeRect(10, gy + 8, W - 20, rh - 16);
				g.shadowColor = PINK; g.shadowBlur = 18; g.fillStyle = bt - cb[i] < 0.08 ? L20HOT : PINK; g.fillRect(x - 25, 0, 50, gy + 8); g.fillRect(x - 25, gy + rh - 8, 50, H - gy - rh + 8); g.shadowBlur = 0;
			}
			// the selectors: the next row outlined, then the bar
			void sel(double[] b, double[] r)
			{
				if (b.Length == 0) return; double y = rowAt(b, r, bt, out int k);
				if (k + 1 < b.Length && bt >= b[k + 1] - 1) { double kk = L20k(bt, b[k + 1] - 1, b[k + 1]); g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.55 * kk)})"; g.lineWidth = 3; g.setLineDash(new double[] { 14, 10 }); g.strokeRect(8, r[k + 1] * rh + 4, W - 16, rh - 8); g.setLineDash(new double[0]); }
				if (y < 0) return;
				if (!on) { g.fillStyle = L20P(0.06 + 0.14 * L20k(bt, tw, ta)); g.fillRect(8, y + 4, W - 16, rh - 8); return; }
				// the jump: a white streak from the row it left (harmless)
				if (k > 0 && bt - b[k] < 0.15) { double y0 = r[k - 1] * rh, q = 1 - (bt - b[k]) / 0.15; g.fillStyle = $"rgba(255,255,255,{str(0.25 * q)})"; g.fillRect(W / 2 - 40, Math.Min(y0, y) + rh / 2, 80, Math.Abs(y - y0)); }
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = bt - b[k] < 0.08 ? L20HOT : PINK; g.fillRect(8, y + 4, W - 16, rh - 8); g.shadowBlur = 0;
				g.fillStyle = "rgba(60,0,20,0.5)"; foreach (var sd in new[] { -1.0, 1.0 }) { double ax = W / 2 + sd * 300; g.beginPath(); g.moveTo(ax - sd * 22, y + rh / 2 - 22); g.lineTo(ax, y + rh / 2); g.lineTo(ax - sd * 22, y + rh / 2 + 22); g.closePath(); g.fill(); }
			}
			sel(sb, sr); sel(qb, qr);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false;
			foreach (var (b, r) in new[] { (sb, sr), (qb, qr) }) { if (b.Length == 0) continue; double y = rowAt(b, r, bt, out _); if (y >= 0 && L20In(p, 8, y + 4, W - 8, y + rh - 4)) return true; }
			for (int i = 0; i < cb.Length; i++)
			{
				if (bt < cb[i] || bt > cb[i] + cd) continue; double x = scanX(i, bt), gy = cg[i] * rh;
				if (Math.Abs(p.x - x) < 25 + p.r - 2 && (p.y - p.r + 2 < gy + 8 || p.y + p.r - 2 > gy + rh - 8)) return true;
			}
			return false;
		};
		return h;
	}

	// Tetris: 80 px cells, 16 x 9. The pieces were played out by gen.py: piece i has states (beat, then 4 cells col,row) in pz;
	// its landing (the last state) shows as a dashed ghost. The stack: cell j is at column sc[j], at row kr[..] from beat kb[..], gone at se[j].
	// A full row flashes at cl beats (rows clr) before it goes. Everything is fixed, nothing is random
	public class L20Piece { public double[] b; public int[][] c; }
	public class L20Stk { public int col; public double[] b, r; public double end; }
	public static Hz L20Tetris(List<L20Piece> P, List<L20Stk> S, double[] cl, double[] clr, double tw, double te, double oy = 0)
	{
		const double C = 80; var h = new Hz { kind = "L20Tetris" };
		int stateOf(L20Piece q, double bt) { int k = -1; for (int i = 0; i < q.b.Length; i++) if (bt >= q.b[i]) k = i; return k; }
		double stkRow(L20Stk s, double bt) { double r = double.NaN; for (int i = 0; i < s.b.Length; i++) if (bt >= s.b[i]) r = s.r[i]; return r; }
		h.update = (t, dt) => { if (t > B(te) + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return;
			g.save(); g.translate(0, oy);
			// the well
			g.strokeStyle = L20G(0.1); g.lineWidth = 1; g.beginPath(); for (int c = 1; c < 16; c++) { g.moveTo(c * C, 0); g.lineTo(c * C, H); } g.stroke();
			// the rows about to clear: a laser line across
			for (int i = 0; i < cl.Length; i++) if (bt >= cl[i] - 1 && bt < cl[i]) { double y = clr[i] * C, k = L20k(bt, cl[i] - 1, cl[i]); g.fillStyle = $"rgba(255,255,255,{str(0.15 + 0.6 * k * (bt % 0.25 < 0.125 ? 1 : 0.5))})"; g.fillRect(0, y + C / 2 - 3 - 4 * k, W, 6 + 8 * k); }
			// the stack
			foreach (var s in S) { if (bt >= s.end) continue; double r = stkRow(s, bt); if (double.IsNaN(r)) continue; L20Cell(s.col * C + 2, r * C + 2, C - 4, C - 4, PINK); }
			// the falling pieces and their ghosts
			foreach (var q in P)
			{
				int k = stateOf(q, bt); if (k < 0 || k >= q.b.Length - 1) continue;
				var gh = q.c[^1]; g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 2; g.setLineDash(new double[] { 8, 6 });
				for (int j = 0; j < 4; j++) g.strokeRect(gh[2 * j] * C + 6, gh[2 * j + 1] * C + 6, C - 12, C - 12);
				g.setLineDash(new double[0]);
				var cs = q.c[k]; bool nw = bt - q.b[k] < 0.06;
				g.shadowColor = PINK; g.shadowBlur = 14;
				for (int j = 0; j < 4; j++) L20Cell(cs[2 * j] * C + 2, cs[2 * j + 1] * C + 2, C - 4, C - 4, nw && k == 0 ? L20HOT : PINK);
				g.shadowBlur = 0;
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); var q0 = new Player { x = p.x, y = p.y - oy, r = p.r };
			foreach (var s in S) { if (bt >= s.end) continue; double r = stkRow(s, bt); if (double.IsNaN(r)) continue; if (L20In(q0, s.col * C + 2, r * C + 2, s.col * C + C - 2, r * C + C - 2)) return true; }
			foreach (var q in P)
			{
				int k = stateOf(q, bt); if (k < 0 || k >= q.b.Length - 1) continue; var cs = q.c[k];
				for (int j = 0; j < 4; j++) if (L20In(q0, cs[2 * j] * C + 2, cs[2 * j + 1] * C + 2, cs[2 * j] * C + C - 2, cs[2 * j + 1] * C + C - 2)) return true;
			}
			return false;
		};
		return h;
	}

	// the loading ring round (cx,cy): it breathes from rmin to rmax and back every per beats (rmin at b0), th thick, with one gap (gw rad)
	// that turns w rad a beat and turns back every rv beats (the arrows at the gap show the way, white a beat before it turns back).
	// The loading icon in the middle (coreR) burns too. Grey from tw, kills b0 .. b1 (beats)
	public static Hz L20Ring(double cx, double cy, double rmin, double rmax, double per, double th, double gw, double w, double rv, double a0, double coreR, double tw, double b0, double b1)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20Ring" };
		double R(double bt) => rmin + (rmax - rmin) * (0.5 - 0.5 * JS.cos(TAU * (bt - b0) / per));
		double G(double bt) => a0 + w * L20Tri(Math.Max(0, bt - b0), rv);
		bool inGap(double a, double d, double bt) { double x = (a - G(bt)) % TAU; if (x < -Math.PI) x += TAU; if (x > Math.PI) x -= TAU; return Math.Abs(x) < gw / 2 - 3 / Math.Max(d, 1); }
		h.update = (t, dt) => { if (t > B(b1) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, b1, b1 + 0.6)); bool on = bt >= b0 && bt < b1;
			double r = bt < b0 ? rmin : R(bt), ga = G(bt);
			g.save(); g.globalAlpha = a;
			if (!on) { g.strokeStyle = L20G(0.3 + 0.5 * L20k(bt, tw, b0)); g.lineWidth = 3; g.setLineDash(new double[] { 12, 10 }); g.beginPath(); g.arc(cx, cy, r, ga + gw / 2, ga - gw / 2 + TAU); g.stroke(); g.setLineDash(new double[0]); g.fillStyle = L20P(0.06 + 0.16 * L20k(bt, tw, b0)); circle(cx, cy, coreR); }
			else
			{
				g.shadowColor = PINK; g.shadowBlur = 18; g.strokeStyle = PINK; g.lineWidth = th; g.beginPath(); g.arc(cx, cy, r, ga + gw / 2, ga - gw / 2 + TAU); g.stroke();
				// the loading icon: a pink disc with 8 dark spokes that turn
				g.fillStyle = PINK; circle(cx, cy, coreR); g.shadowBlur = 0;
				g.strokeStyle = "rgba(60,0,20,0.5)"; g.lineWidth = 6; g.beginPath(); for (int i = 0; i < 8; i++) { double q = bt * 2 + i * TAU / 8; g.moveTo(cx + JS.cos(q) * coreR * 0.35, cy + JS.sin(q) * coreR * 0.35); g.lineTo(cx + JS.cos(q) * coreR * 0.85, cy + JS.sin(q) * coreR * 0.85); } g.stroke();
				// the way the gap turns; white a beat before it turns back
				double u = Math.Max(0, bt - b0), m = ((u % (2 * rv)) + 2 * rv) % (2 * rv); int dir = m < rv ? 1 : -1; bool flip = (m < rv ? rv - m : 2 * rv - m) < 1;
				g.strokeStyle = flip ? (bt % 0.5 < 0.25 ? "#fff" : L20G(0.8)) : L20G(0.6); g.lineWidth = 4;
				foreach (var sd in new[] { -1.0, 1.0 })
				{
					double q = ga + sd * (gw / 2 + 0.12), x = cx + JS.cos(q) * (r + th), y = cy + JS.sin(q) * (r + th), d = q + Math.Sign(w) * dir * Math.PI / 2;
					g.beginPath(); g.moveTo(x + JS.cos(d + 2.5) * 12, y + JS.sin(d + 2.5) * 12); g.lineTo(x, y); g.lineTo(x + JS.cos(d - 2.5) * 12, y + JS.sin(d - 2.5) * 12); g.stroke();
				}
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy);
			if (d < coreR + p.r - 2) return true;
			return Math.Abs(d - R(bt)) < th / 2 + p.r - 2 && !inGap(JS.atan2(dy, dx), d, bt);
		};
		return h;
	}

	// a loading bar's front: a pink wall (40 wide, full height) that steps sx px every beat from x0 (dir 1: to the right) at beats b0, b0+1, ...;
	// its hole (hy[k] +- hh) moves with each step. The next place and its hole show a beat before. Behind it the "loaded" part (grey, harmless)
	public static Hz L20Front(double x0, double dir, double sx, double[] hy, double hh, double tw, double b0)
	{
		int K = hy.Length; double b1 = b0 + K; var h = new Hz { kind = "L20Front" };
		double X(int k) => x0 + dir * sx * k;
		void at(double bt, out double x, out double y)
		{
			int k = (int)clamp(Math.Floor(bt - b0), 0, K - 1); double u = bt - b0 - k;
			if (k == 0 || u >= 0.15) { x = X(k); y = hy[k]; return; }
			double e = L20Ease(u / 0.15); x = X(k - 1) + (X(k) - X(k - 1)) * e; y = hy[k - 1] + (hy[k] - hy[k - 1]) * e;
		}
		h.update = (t, dt) => { if (t > B(b1) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, b1, b1 + 0.5));
			g.save(); g.globalAlpha = a;
			if (bt < b0)
			{
				double k = L20k(bt, tw, b0); L20Dash(X(0), 0, X(0), hy[0] - hh, L20G(0.4 + 0.5 * k), 3); L20Dash(X(0), hy[0] + hh, X(0), H, L20G(0.4 + 0.5 * k), 3);
				g.fillStyle = L20P(0.05 + 0.18 * k); g.fillRect(X(0) - 20, 0, 40, hy[0] - hh); g.fillRect(X(0) - 20, hy[0] + hh, 40, H - hy[0] - hh); g.restore(); return;
			}
			at(bt, out double x, out double y); int kk = (int)clamp(Math.Floor(bt - b0), 0, K - 1);
			// the loaded part: grey stripes behind the front (harmless)
			double bx0 = dir > 0 ? 0 : x + 20, bx1 = dir > 0 ? x - 20 : W; g.fillStyle = "rgba(200,210,235,0.06)"; g.fillRect(bx0, 0, bx1 - bx0, H);
			g.strokeStyle = "rgba(200,210,235,0.08)"; g.lineWidth = 10; g.beginPath(); for (double s = -H; s < W; s += 46) { double sa = clamp(s, bx0, bx1), sb2 = clamp(s + H, bx0, bx1); if (sb2 > sa) { g.moveTo(sa, H - (sa - s)); g.lineTo(sb2, H - (sb2 - s)); } } g.stroke();
			// the next step
			if (kk + 1 < K) { double nx = X(kk + 1), ny = hy[kk + 1]; g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 }); g.strokeRect(nx - 20, ny - hh, 40, 2 * hh); g.setLineDash(new double[0]); L20Dash(nx, 0, nx, ny - hh, L20G(0.35), 2); L20Dash(nx, ny + hh, nx, H, L20G(0.35), 2); }
			g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = bt - b0 < 0.08 ? L20HOT : PINK; g.fillRect(x - 20, 0, 40, y - hh); g.fillRect(x - 20, y + hh, 40, H - y - hh); g.shadowBlur = 0;
			g.fillStyle = "rgba(60,0,20,0.45)"; for (double s = 8; s < H; s += 26) if (s < y - hh - 6 || s > y + hh + 6) g.fillRect(x - 12, s, 24, 6);
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < b0 || bt >= b1) return false; at(bt, out double x, out double y);
			return Math.Abs(p.x - x) < 20 + p.r - 2 && (p.y - p.r + 2 < y - hh || p.y + p.r - 2 > y + hh);
		};
		return h;
	}

	// Pong: balls along straight lines between keyframes (ball i: beats kb, places kx ky; nk[i] keyframes each, all in one list).
	// The paddles (when pad) follow their keyframes (lb ly / rb ry). Each ball shows its next two lines dashed. Ball i is shown from
	// its first keyframe to its last (where it stops, frozen, for the shot that follows)
	public static Hz L20Pong(double[] nk, double[] kb, double[] kx, double[] ky, double R, bool pad, double[] lb, double[] ly, double[] rb, double[] ry, double tw, double ta, double te)
	{
		int nb = nk.Length; var st = new int[nb + 1]; for (int i = 0; i < nb; i++) st[i + 1] = st[i] + (int)nk[i];
		var h = new Hz { kind = "L20Pong" };
		bool pos(int i, double bt, out double x, out double y, out int seg)
		{
			x = y = 0; seg = -1; int s = st[i], e = st[i + 1]; if (bt < kb[s] || bt >= kb[e - 1]) return false;
			int k = s; while (k < e - 2 && bt >= kb[k + 1]) k++;
			double u = (bt - kb[k]) / (kb[k + 1] - kb[k]); x = kx[k] + (kx[k + 1] - kx[k]) * u; y = ky[k] + (ky[k + 1] - ky[k]) * u; seg = k; return true;
		}
		double padY(double[] b, double[] y, double bt) { if (b.Length == 0) return H / 2; if (bt <= b[0]) return y[0]; for (int i = 0; i + 1 < b.Length; i++) if (bt < b[i + 1]) return y[i] + (y[i + 1] - y[i]) * L20Ease((bt - b[i]) / (b[i + 1] - b[i])); return y[^1]; }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.5)); bool on = bt >= ta && bt < te;
			g.save(); g.globalAlpha = a;
			if (pad)
			{
				// the court: the middle line (harmless)
				g.strokeStyle = L20G(0.12); g.lineWidth = 4; g.setLineDash(new double[] { 20, 18 }); g.beginPath(); g.moveTo(W / 2, 0); g.lineTo(W / 2, H); g.stroke(); g.setLineDash(new double[0]);
				foreach (var (x, y) in new[] { (32.0, padY(lb, ly, bt)), (W - 32, padY(rb, ry, bt)) })
				{ if (on) { g.shadowColor = PINK; g.shadowBlur = 16; g.fillStyle = PINK; } else g.fillStyle = L20P(0.1 + 0.2 * L20k(bt, tw, ta)); g.fillRect(x - 12, y - 75, 24, 150); g.shadowBlur = 0; }
			}
			for (int i = 0; i < nb; i++)
			{
				if (!pos(i, bt, out double x, out double y, out int k)) continue;
				// the next lines
				g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 9 }); g.beginPath(); g.moveTo(x, y);
				for (int j = k + 1; j <= Math.Min(k + 2, st[i + 1] - 1); j++) g.lineTo(kx[j], ky[j]); g.stroke(); g.setLineDash(new double[0]);
				if (!on) { g.fillStyle = L20P(0.15 + 0.3 * L20k(bt, tw, ta)); circle(x, y, R); continue; }
				for (int j = 1; j <= 5; j++) { pos(i, bt - j * 0.035, out double tx, out double ty, out _); g.fillStyle = L20P(0.32 - 0.05 * j); circle(tx, ty, R * (1 - 0.12 * j)); }
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK; circle(x, y, R); g.shadowBlur = 0; g.fillStyle = "rgba(255,200,215,0.6)"; circle(x - R * 0.3, y - R * 0.3, R * 0.3);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false;
			if (pad && (L20In(p, 20, padY(lb, ly, bt) - 75, 44, padY(lb, ly, bt) + 75) || L20In(p, W - 44, padY(rb, ry, bt) - 75, W - 20, padY(rb, ry, bt) + 75))) return true;
			for (int i = 0; i < nb; i++) if (pos(i, bt, out double x, out double y, out _) && hypot(p.x - x, p.y - y) < R + p.r - 2) return true;
			return false;
		};
		return h;
	}

	// the balls stop (at bx by) from beat f0; at f0 + 0.5 they take aim at the square (a dashed line follows it until then), and at f1
	// they fly there at sp px/s. Aimed: "rnd" in the data
	public static Hz L20Shot(double[] bx, double[] by, double R, double sp, double f0, double f1)
	{
		int n = bx.Length; var h = new Hz { kind = "L20Shot" }; double ax = -1, ay = -1; var dx = new double[n]; var dy = new double[n]; double fa = f0 + 0.5;
		h.update = (t, dt) =>
		{
			double bt = BT(t);
			if (ax < 0 && bt >= fa && player != null) { ax = player.x; ay = player.y; for (int i = 0; i < n; i++) { double l = Math.Max(1, hypot(ax - bx[i], ay - by[i])); dx[i] = (ax - bx[i]) / l; dy[i] = (ay - by[i]) / l; } }
			if (bt > f1 + 4) h.dead = true;
		};
		(double x, double y) at(int i, double t) { double u = Math.Max(0, t - B(f1)); return (bx[i] + dx[i] * sp * u, by[i] + dy[i] * sp * u); }
		h.draw = t =>
		{
			double bt = BT(t); if (bt < f0) return;
			for (int i = 0; i < n; i++)
			{
				var c = at(i, t); if (c.x < -60 || c.x > W + 60 || c.y < -60 || c.y > H + 60) continue;
				if (bt < f1)
				{
					double tx = ax >= 0 ? ax : player?.x ?? W / 2, ty = ax >= 0 ? ay : player?.y ?? H / 2, k = L20k(bt, f0, f1);
					L20Dash(bx[i], by[i], bx[i] + (tx - bx[i]) * 3, by[i] + (ty - by[i]) * 3, ax >= 0 ? $"rgba(255,255,255,{str(0.5 + 0.4 * k)})" : L20G(0.4), ax >= 0 ? 3 : 2);
					g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 3; g.beginPath(); g.arc(bx[i], by[i], R + 6 + 6 * (1 - k), 0, TAU); g.stroke();
				}
				g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = PINK; circle(c.x, c.y, R); g.shadowBlur = 0;
			}
		};
		h.hit = (p, t) => { if (BT(t) < f0) return false; for (int i = 0; i < n; i++) { var c = at(i, t); if (hypot(p.x - c.x, p.y - c.y) < R + p.r - 2) return true; } return false; };
		return h;
	}
}
