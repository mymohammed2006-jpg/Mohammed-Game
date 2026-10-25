using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L20 "New Game", part 2 on the song's sounds: the invaders that play the tune and the glitch that jumps on its notes. See L20.cs
public static partial class G
{
	// the invaders: cols x rows, dx dy apart, the army's corner at (ox[k],oy[k]) from beat sb[k] (it steps on the kicks).
	// Shots: at beat fb[i] the lowest invader of column fc[i] fires its laser straight down out of its own body (lw wide, lh beats; the
	// column is picked by the note's pitch, so the lasers play the tune); its gun glows and its line shows wn beats before.
	// Bombs: at bb[i] the lowest invader of column bc[i] drops a bomb (vb px/s), its line shows wn beats before.
	// Dives: invader (dc[i],dr[i]) leaves at db[i] and dives to x = dx2[i] at the bottom in ddur beats (its curve dashed a beat before).
	// Grey from tw, kills from ta to te (beats)
	public static Hz L20Inv2(int cols, int rows, double dx, double dy, double[] sb, double[] ox, double[] oy, double[] fb, double[] fc, double lw, double lh, double wn,
		double[] bb, double[] bc, double vb, double[] db, double[] dc, double[] dr, double[] dx2, double ddur, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L20Inv2" }; const double IR = 30, U = IR / 6;
		var diveB = new double[cols * rows]; var diveX = new double[cols * rows];
		for (int i = 0; i < diveB.Length; i++) diveB[i] = 1e9;
		for (int i = 0; i < db.Length; i++) { int k = (int)dr[i] * cols + (int)dc[i]; diveB[k] = db[i]; diveX[k] = dx2[i]; }
		(double x, double y) army(double bt)
		{
			int k = 0; for (int i = 0; i < sb.Length; i++) if (bt >= sb[i]) k = i; double u = bt - sb[k];
			if (k == 0 || u >= 0.08) return (ox[k], oy[k]); double e = u / 0.08; return (ox[k - 1] + (ox[k] - ox[k - 1]) * e, oy[k - 1] + (oy[k] - oy[k - 1]) * e);
		}
		// where invader (c,r) is: in the army, or on its dive curve; false when it is gone
		bool inv(int c, int r, double bt, out double x, out double y)
		{
			double d0 = diveB[r * cols + c];
			if (bt < d0) { var o = army(bt); x = o.x + c * dx; y = o.y + r * dy; return true; }
			var s = army(d0); double sx = s.x + c * dx, sy = s.y + r * dy, ex = diveX[r * cols + c], u = (bt - d0) / ddur;
			if (u >= 1) { x = y = 0; return false; }
			double cxp = sx + (sx < ex ? -1 : 1) * 220, cyp = sy + 260, ey = H + 80, m = 1 - u;
			x = m * m * sx + 2 * m * u * cxp + u * u * ex; y = m * m * sy + 2 * m * u * cyp + u * u * ey; return true;
		}
		// the lowest invader of a column still in the army at beat bt (its row), -1 when none
		int low(int c, double bt) { for (int r = rows - 1; r >= 0; r--) if (bt < diveB[r * cols + c]) return r; return -1; }
		bool gun(int c, double b, double bt, out double x, out double y)
		{
			x = y = 0; int r = low(c, b); if (r < 0) return false; var o = army(bt); x = o.x + c * dx; y = o.y + r * dy + U; return true;
		}
		(double x, double y) bomb(int i, double t) { gun((int)bc[i], bb[i], B(bb[i]), out double x, out double y); return (x, y + 3 * U + vb * Math.Max(0, t - B(bb[i]))); }
		h.update = (t, dt) => { if (t > B(te) + 2) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double a = Math.Min(L20k(bt, tw, tw + 1), 1 - L20k(bt, te, te + 0.5)); bool on = bt >= ta;
			g.save(); g.globalAlpha = a;
			// the lines of the shots and bombs to come
			for (int i = 0; i < fb.Length; i++)
			{
				if (bt < fb[i] - wn || bt >= fb[i] || !gun((int)fc[i], fb[i], bt, out double x, out double y)) continue; double k = L20k(bt, fb[i] - wn, fb[i]);
				g.fillStyle = L20P(0.04 + 0.16 * k); g.fillRect(x - lw / 2, y, lw, H - y); L20Dash(x, y + 3 * U, x, H, L20G(0.35 + 0.55 * k), 2 + 2 * k);
			}
			for (int i = 0; i < bb.Length; i++)
			{
				if (bt < bb[i] - wn || bt >= bb[i] || !gun((int)bc[i], bb[i], bt, out double x, out double y)) continue; double k = L20k(bt, bb[i] - wn, bb[i]);
				L20Dash(x, y + 3 * U, x, H, L20G(0.3 + 0.5 * k), 2);
			}
			// the dive curves, a beat before
			g.lineWidth = 2; g.setLineDash(new double[] { 9, 8 });
			for (int i = 0; i < db.Length; i++)
			{
				if (bt < db[i] - 1 || bt >= db[i]) continue; double k = L20k(bt, db[i] - 1, db[i]); g.strokeStyle = $"rgba(255,255,255,{str(0.25 + 0.5 * k)})";
				g.beginPath(); for (int j = 0; j <= 16; j++) { inv((int)dc[i], (int)dr[i], db[i] + ddur * j / 16 * 0.999, out double x, out double y); if (j == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke();
			}
			g.setLineDash(new double[0]);
			// the lasers: out of the invader's body, as wide as it
			for (int i = 0; i < fb.Length; i++)
			{
				if (bt < fb[i] || bt > fb[i] + lh || !gun((int)fc[i], fb[i], bt, out double x, out double y)) continue;
				double w2 = lw * Math.Min(1, (bt - fb[i]) / 0.05 + 0.5); g.shadowColor = PINK; g.shadowBlur = 20; g.fillStyle = bt - fb[i] < 0.06 ? L20HOT : PINK; g.fillRect(x - w2 / 2, y, w2, H - y); g.shadowBlur = 0;
				g.fillStyle = "rgba(255,120,165,0.55)"; g.fillRect(x - w2 / 6, y + 3 * U, w2 / 3, H - y - 3 * U);
			}
			// the bombs
			g.shadowColor = PINK; g.shadowBlur = 14; g.fillStyle = PINK;
			for (int i = 0; i < bb.Length; i++) { if (bt < bb[i]) continue; var c = bomb(i, t); if (c.y > H + 30) continue; g.beginPath(); g.moveTo(c.x, c.y - 18); g.lineTo(c.x + 12, c.y); g.lineTo(c.x, c.y + 18); g.lineTo(c.x - 12, c.y); g.closePath(); g.fill(); }
			g.shadowBlur = 0;
			// the invaders; a gun about to fire glows white
			bool open = ((int)Math.Floor(bt)) % 2 == 0;
			if (on) { g.shadowColor = PINK; g.shadowBlur = 12; }
			for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) if (inv(c, r, bt, out double x, out double y)) L20InvDraw(x, y, IR, on ? PINK : L20P(0.15 + 0.3 * L20k(bt, tw, ta)), open);
			g.shadowBlur = 0;
			for (int i = 0; i < fb.Length; i++)
			{
				if (bt < fb[i] - wn || bt >= fb[i] || !gun((int)fc[i], fb[i], bt, out double x, out double y)) continue; double k = L20k(bt, fb[i] - wn, fb[i]);
				g.fillStyle = $"rgba(255,255,255,{str(0.4 + 0.6 * k * (bt % 0.25 < 0.125 ? 1 : 0.6))})"; g.fillRect(x - 2 * U, y - U, 4 * U, 3 * U);
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te + 2) return false;
			bool open = ((int)Math.Floor(bt)) % 2 == 0;
			if (bt < te) for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) if (inv(c, r, bt, out double x, out double y) && L20InvHit(p, x, y, IR, open)) return true;
			for (int i = 0; i < fb.Length; i++) { if (bt < fb[i] || bt > fb[i] + lh || !gun((int)fc[i], fb[i], bt, out double x, out double y)) continue; double w2 = lw * Math.Min(1, (bt - fb[i]) / 0.05 + 0.5); if (L20In(p, x - w2 / 2, y, x + w2 / 2, H + 50)) return true; }
			for (int i = 0; i < bb.Length; i++) { if (bt < bb[i]) continue; var c = bomb(i, t); if (Math.Abs(p.x - c.x) + Math.Abs(p.y - c.y) * 0.67 < 12 + p.r - 2) return true; }
			return false;
		};
		return h;
	}

	// the glitch on the notes: the screen in n slices, all pink but for one hole each (hw wide). Slice k's hole stands still and jumps to its
	// next place at the beats of its keyframes (nk[k] each, beats kb, places kx, all in one list); the next place shows dashed before.
	// Grey with the holes outlined from tw, kills ta .. te (beats)
	public static Hz L20Glitch2(int n, double hw, double[] nk, double[] kb, double[] kx, double tw, double ta, double te)
	{
		double sh = H / n; var st = new int[n + 1]; for (int i = 0; i < n; i++) st[i + 1] = st[i] + (int)nk[i];
		var h = new Hz { kind = "L20Glitch2" };
		int key(int k, double bt) { int j = st[k]; for (int i = st[k]; i < st[k + 1]; i++) if (bt >= kb[i]) j = i; return j; }
		double hx(int k, double bt) { int j = key(k, bt); if (j == st[k] || bt - kb[j] >= 0.06) return kx[j]; return kx[j - 1] + (kx[j] - kx[j - 1]) * L20Ease((bt - kb[j]) / 0.06); }
		h.update = (t, dt) => { if (t > B(te) + 0.4) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw || bt > te + 0.3) return; bool on = bt >= ta && bt < te; double a = 1 - L20k(bt, te, te + 0.3);
			g.save(); g.globalAlpha = a;
			for (int k = 0; k < n; k++)
			{
				double y = k * sh, x = hx(k, bt), j = on ? (JS.sin(t * 50 + k * 2) > 0.6 ? 6 : 0) : 0;
				if (!on) { double q = L20k(bt, tw, ta); g.fillStyle = L20P(0.05 + 0.2 * q); g.fillRect(0, y + 2, x - hw / 2, sh - 4); g.fillRect(x + hw / 2, y + 2, W - x - hw / 2, sh - 4); }
				else { g.fillStyle = bt - ta < 0.06 ? L20HOT : PINK; g.fillRect(0, y, x - hw / 2, sh); g.fillRect(x + hw / 2, y, W - x - hw / 2, sh); g.fillStyle = "rgba(60,0,20,0.35)"; for (double s = y + 6 + j; s < y + sh; s += 14) { g.fillRect(0, s, x - hw / 2, 3); g.fillRect(x + hw / 2, s, W - x - hw / 2, 3); } }
				g.strokeStyle = "rgba(255,255,255,0.7)"; g.lineWidth = 2; g.strokeRect(x - hw / 2, y + 2, hw, sh - 4);
				// the next place of this hole
				int nx = key(k, bt) + 1; if (nx < st[k + 1] && kb[nx] - bt < 0.75 && kb[nx] > bt)
				{ double q = L20k(bt, kb[nx] - 0.75, kb[nx]); g.strokeStyle = $"rgba(255,255,255,{str(0.4 + 0.6 * q)})"; g.lineWidth = 3; g.setLineDash(new double[] { 8, 6 }); g.strokeRect(kx[nx] - hw / 2 + 4, y + 6, hw - 8, sh - 12); g.setLineDash(new double[0]); }
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false;
			int k0 = (int)clamp(Math.Floor((p.y - p.r + 2) / sh), 0, n - 1), k1 = (int)clamp(Math.Floor((p.y + p.r - 2) / sh), 0, n - 1);
			for (int k = k0; k <= k1; k++) { double x = hx(k, bt); if (p.x - p.r + 2 < x - hw / 2 || p.x + p.r - 2 > x + hw / 2) return true; }
			return false;
		};
		return h;
	}

	static readonly int _k20inv2 = L20K("L20inv2", e => () =>
		hazards.Add(L20Inv2(e.i("cols"), e.i("rows"), e.num("dx"), e.num("dy"), e.nums("sb"), e.nums("ox"), e.nums("oy"), e.nums("fb"), e.nums("fc"), e.num("lw"), e.num("lh"), e.num("wn"),
			e.nums("bb"), e.nums("bc"), e.num("vb"), e.nums("db"), e.nums("dc"), e.nums("dr"), e.nums("dx2"), e.num("ddur"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20glitch2 = L20K("L20glitch2", e => () => hazards.Add(L20Glitch2(e.i("n"), e.num("hw"), e.nums("nk"), e.nums("kb"), e.nums("kx"), e.num("tw"), e.num("ta"), e.num("te"))));
}
