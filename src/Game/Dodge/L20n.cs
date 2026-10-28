using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// L20 "New Game", the new parts 1 and 2 (10-28): the controller. You pressed NEW GAME and the pad's own buttons come for you, each one on
// one sound of the song: part 1 "press start" (the cross on the kicks, the circle rings on the tune, the square tiles on the bass, the
// spinning triangle on the broken beat, then all four buttons circling and firing on the build), part 2 "playing" (the D-pad turning on the
// drop's stabs, the tiles on the break, the two sticks aiming their beams on the tune, the four corners on the last hits). Part 3 (the boss)
// is the old one (L20.cs, L20c.cs). Kinds "L20b_*" in data/dodge_L20.json (tools/level20/gen.py); the old design is data/dodge_L20_v51.json.
// Every hazard is a pure function of time. Only pink hurts; warnings are white dashed lines with a faint pink fill, like the rest of L20.
public static partial class G
{
	static double L20bWrap(double a) { a %= TAU; if (a < -Math.PI) a += TAU; if (a > Math.PI) a -= TAU; return a; }
	// keyframes: from kb[j] the value goes to kv[j]; ed > 0: it snaps there in ed beats (eased), ed = 0: a straight line between the keys
	static double L20bKey(double[] kb, double[] kv, double bt, double ed)
	{
		if (kb.Length == 0) return 0; if (bt <= kb[0]) return kv[0];
		int j = 0; for (int i = 0; i < kb.Length; i++) if (bt >= kb[i]) j = i;
		if (ed <= 0) { if (j + 1 >= kb.Length) return kv[j]; double u = (bt - kb[j]) / (kb[j + 1] - kb[j]); return kv[j] + (kv[j + 1] - kv[j]) * u; }
		return kv[j - (j > 0 ? 1 : 0)] + (kv[j] - kv[j - (j > 0 ? 1 : 0)]) * L20Ease((bt - kb[j]) / ed);
	}
	static void L20bGlow(bool on) { if (on) { g.shadowColor = PINK; g.shadowBlur = 16; } else g.shadowBlur = 0; }
	// a thick band along the line through (x,y) at angle a, from u0 to u1 along it
	static void L20bBand(double x, double y, double a, double u0, double u1, double w)
	{
		double c = JS.cos(a), s = JS.sin(a), nx = -s * w / 2, ny = c * w / 2;
		g.beginPath(); g.moveTo(x + c * u0 + nx, y + s * u0 + ny); g.lineTo(x + c * u1 + nx, y + s * u1 + ny); g.lineTo(x + c * u1 - nx, y + s * u1 - ny); g.lineTo(x + c * u0 - nx, y + s * u0 - ny); g.closePath();
	}
	static bool L20bInBand(Player p, double x, double y, double a, double u0, double u1, double w)
	{
		double dx = p.x - x, dy = p.y - y, c = JS.cos(a), s = JS.sin(a), u = dx * c + dy * s, v = -dx * s + dy * c;
		return u > u0 - p.r + 2 && u < u1 + p.r - 2 && Math.Abs(v) < w / 2 + p.r - 2;
	}

	// ✕ the cross: at beat xb[i] two beams cross at (xx,xy), at xa and xa+90 degrees, w wide, for lh beats; dashed wn beats before
	public static Hz L20bCross(double[] xb, double[] xx, double[] xy, double[] xa, double w, double wn, double lh)
	{
		var h = new Hz { kind = "L20bCross" }; double last = xb.Length > 0 ? xb.Max() : 0;
		h.update = (t, dt) => { if (BT(t) > last + lh + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < xb.Length; i++)
			{
				double u = bt - xb[i]; if (u < -wn || u > lh + 0.25) continue; double a = xa[i] * Math.PI / 180;
				if (u < 0)
				{
					double k = 1 + u / wn; g.fillStyle = L20P(0.03 + 0.1 * k);
					for (int j = 0; j < 2; j++) { L20bBand(xx[i], xy[i], a + j * Math.PI / 2, -1600, 1600, w); g.fill(); }
					for (int j = 0; j < 2; j++) { double b = a + j * Math.PI / 2; L20Dash(xx[i] - JS.cos(b) * 1600, xy[i] - JS.sin(b) * 1600, xx[i] + JS.cos(b) * 1600, xy[i] + JS.sin(b) * 1600, L20G(0.3 + 0.6 * k), 2 + k); }
					continue;
				}
				double f = u <= lh ? 1 : 1 - (u - lh) / 0.25, w2 = w * (u <= lh ? Math.Min(1, 0.5 + u / 0.06) : f);
				g.save(); g.globalAlpha = Math.Max(0, f); L20bGlow(true); g.fillStyle = u < 0.06 ? L20HOT : PINK;
				for (int j = 0; j < 2; j++) { L20bBand(xx[i], xy[i], a + j * Math.PI / 2, -1600, 1600, w2); g.fill(); }
				L20bGlow(false); g.fillStyle = "rgba(255,150,185,0.6)"; for (int j = 0; j < 2; j++) { L20bBand(xx[i], xy[i], a + j * Math.PI / 2, -1600, 1600, w2 / 4); g.fill(); }
				g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			for (int i = 0; i < xb.Length; i++)
			{
				double u = bt - xb[i]; if (u < 0 || u > lh) continue; double a = xa[i] * Math.PI / 180, w2 = w * Math.Min(1, 0.5 + u / 0.06);
				if (L20bInBand(p, xx[i], xy[i], a, -1600, 1600, w2) || L20bInBand(p, xx[i], xy[i], a + Math.PI / 2, -1600, 1600, w2)) return true;
			}
			return false;
		};
		return h;
	}

	// ○ □ △ the button rings: from (x,y) at beat b a ring grows at v px/s from r0, th thick, shaped like the button: sides 0 = a circle, 3 = a
	// triangle, 4 = a square (turned rot degrees, r is its corners' distance); open where the angle is within gw/2 of ga. Shows wn beats before
	// as a dashed outline and its open way lit
	public static Hz L20bRing(double cx, double cy, double b0, int sides, double rot, double ga, double gw, double v, double th, double r0, double wn)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20bRing" }; rot *= Math.PI / 180; ga *= Math.PI / 180; gw *= Math.PI / 180;
		double R(double t) => r0 + v * (t - B(b0));
		// the outline's distance from the middle at angle q
		double edge(double r, double q) { if (sides < 3) return r; double s = TAU / sides, m = ((q - rot) % s + s) % s; return r * JS.cos(Math.PI / sides) / JS.cos(m - Math.PI / sides); }
		void path(double r)
		{
			// from the gap's end round to its start, through every corner
			double a0 = ga + gw / 2, a1 = ga - gw / 2 + TAU; g.beginPath();
			var qs = new List<double> { a0 };
			if (sides < 3) for (int i = 1; i < 72; i++) qs.Add(a0 + (a1 - a0) * i / 72);
			else { double s = TAU / sides; double c0 = rot + Math.Ceiling((a0 - rot) / s) * s; for (double c = c0; c < a1 - 1e-9; c += s) qs.Add(c); }
			qs.Add(a1);
			for (int i = 0; i < qs.Count; i++) { double d = edge(r, qs[i]), x = cx + JS.cos(qs[i]) * d, y = cy + JS.sin(qs[i]) * d; if (i == 0) g.moveTo(x, y); else g.lineTo(x, y); }
		}
		h.update = (t, dt) => { if (R(t) > 1700) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 - wn) return;
			g.save(); g.lineJoin = "miter";
			if (bt < b0)
			{
				double k = L20k(bt, b0 - wn, b0); g.fillStyle = $"rgba(255,255,255,{str(0.03 + 0.07 * k)})"; g.beginPath(); g.moveTo(cx, cy); g.arc(cx, cy, 420, ga - gw / 2, ga + gw / 2); g.closePath(); g.fill();
				L20Dash(cx, cy, cx + JS.cos(ga) * 420, cy + JS.sin(ga) * 420, $"rgba(255,255,255,{str(0.25 + 0.4 * k)})", 2);
				g.strokeStyle = L20G(0.4 + 0.5 * k); g.lineWidth = 3; g.setLineDash(new double[] { 10, 8 }); path(r0 + 30 * k); g.stroke(); g.setLineDash(new double[0]);
			}
			else { L20bGlow(true); g.strokeStyle = bt - b0 < 0.06 ? L20HOT : PINK; g.lineWidth = th; path(R(t)); g.stroke(); L20bGlow(false); }
			g.restore();
		};
		h.hit = (p, t) =>
		{
			if (BT(t) < b0) return false; double dx = p.x - cx, dy = p.y - cy, d = hypot(dx, dy), q = JS.atan2(dy, dx);
			if (Math.Abs(L20bWrap(q - ga)) < gw / 2 - 3 / Math.Max(d, 1)) return false;
			double r = R(t); if (sides < 3) return Math.Abs(d - r) < th / 2 + p.r - 2;
			// a polygon: the square's distance to the outline's sides
			double s = TAU / sides;
			for (int i = 0; i < sides; i++)
			{
				double qa = rot + i * s, qb = qa + s;
				if (L20Seg(p.x, p.y, cx + JS.cos(qa) * r, cy + JS.sin(qa) * r, cx + JS.cos(qb) * r, cy + JS.sin(qb) * r) < th / 2 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// □ the tiles: a grid of cw x ch cells; at beat tb[i] the cells of hit i (tn[i] of them, in tc as col + row * cols) slam pink for lh
	// beats; they show dashed wn beats before
	public static Hz L20bTiles(int cols, double cw, double ch, double[] tb, double[] tn, double[] tc, double wn, double lh)
	{
		var h = new Hz { kind = "L20bTiles" }; var st = new int[tb.Length + 1]; for (int i = 0; i < tb.Length; i++) st[i + 1] = st[i] + (int)tn[i];
		double last = tb.Length > 0 ? tb.Max() : 0;
		h.update = (t, dt) => { if (BT(t) > last + lh + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < tb.Length; i++)
			{
				double u = bt - tb[i]; if (u < -wn || u > lh + 0.2) continue;
				for (int j = st[i]; j < st[i + 1]; j++)
				{
					double x = ((int)tc[j] % cols) * cw, y = ((int)tc[j] / cols) * ch;
					if (u < 0)
					{
						double k = 1 + u / wn; g.fillStyle = L20P(0.04 + 0.12 * k); g.fillRect(x + 4, y + 4, cw - 8, ch - 8);
						g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 9, 7 }); g.strokeRect(x + 8, y + 8, cw - 16, ch - 16); g.setLineDash(new double[0]);
						continue;
					}
					double f = u <= lh ? 1 : 1 - (u - lh) / 0.2, s = u <= lh ? Math.Min(1, 0.7 + u / 0.05 * 0.3) : 1;
					g.save(); g.globalAlpha = Math.Max(0, f); L20bGlow(true);
					L20Cell(x + cw / 2 - (cw - 4) * s / 2, y + ch / 2 - (ch - 4) * s / 2, (cw - 4) * s, (ch - 4) * s, u < 0.06 ? L20HOT : PINK); L20bGlow(false);
					g.strokeStyle = "rgba(255,210,225,0.55)"; g.lineWidth = 4; g.strokeRect(x + cw / 2 - cw * 0.17, y + ch / 2 - ch * 0.17, cw * 0.34, ch * 0.34);
					g.restore();
				}
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			for (int i = 0; i < tb.Length; i++)
			{
				double u = bt - tb[i]; if (u < 0 || u > lh) continue; double s = Math.Min(1, 0.7 + u / 0.05 * 0.3);
				for (int j = st[i]; j < st[i + 1]; j++)
				{
					double x = ((int)tc[j] % cols) * cw + cw / 2, y = ((int)tc[j] / cols) * ch + ch / 2, hw = (cw - 4) * s / 2, hh = (ch - 4) * s / 2;
					if (L20In(p, x - hw, y - hh, x + hw, y + hh)) return true;
				}
			}
			return false;
		};
		return h;
	}

	// △ the spinning triangle round (cx,cy): its corners' distance and its turn are keyframes (kb beats, kr px, ka degrees, snapping in ed
	// beats); each side is th thick and open in its middle (hw px). A small solid triangle turns the other way in the middle. Its next pose
	// shows dashed 0.6 beat before. Grey from tw, kills from ta to te
	public static Hz L20bTri(double cx, double cy, double[] kb, double[] kr, double[] ka, double ed, double hw, double th, double core, double tw, double ta, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20bTri" };
		double rad(double bt) => L20bKey(kb, kr, bt, ed);
		double ang(double bt) => L20bKey(kb, ka, bt, ed) * Math.PI / 180;
		// the 2 pieces of side i (from corner A to the hole, from the hole to corner B)
		void side(double r, double a, int i, out double ax, out double ay, out double bx, out double by)
		{
			double qa = a - Math.PI / 2 + i * TAU / 3, qb = qa + TAU / 3; ax = cx + JS.cos(qa) * r; ay = cy + JS.sin(qa) * r; bx = cx + JS.cos(qb) * r; by = cy + JS.sin(qb) * r;
		}
		void draw(double r, double a, bool dash)
		{
			for (int i = 0; i < 3; i++)
			{
				side(r, a, i, out double ax, out double ay, out double bx, out double by); double L = hypot(bx - ax, by - ay), u0 = Math.Max(0, 0.5 - hw / 2 / L), u1 = 1 - u0;
				g.beginPath(); g.moveTo(ax, ay); g.lineTo(ax + (bx - ax) * u0, ay + (by - ay) * u0); g.moveTo(ax + (bx - ax) * u1, ay + (by - ay) * u1); g.lineTo(bx, by); g.stroke();
				if (!dash) { g.fillStyle = "rgba(255,210,225,0.8)"; g.beginPath(); g.arc(ax + (bx - ax) * u0, ay + (by - ay) * u0, th / 4, 0, TAU); g.arc(ax + (bx - ax) * u1, ay + (by - ay) * u1, th / 4, 0, TAU); g.fill(); }
			}
		}
		double cang(double bt) => -2 * ang(bt) + bt * 0.6;
		h.update = (t, dt) => { if (BT(t) > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double al = 1 - L20k(bt, te, te + 0.4), r = rad(bt), a = ang(bt); bool on = bt >= ta && bt < te;
			g.save(); g.globalAlpha = al; g.lineCap = "round";
			// the next pose
			int nx = -1; for (int i = 0; i < kb.Length; i++) if (kb[i] > bt) { nx = i; break; }
			if (nx >= 0 && kb[nx] - bt < 0.6 && kb[nx] < te)
			{
				double k = L20k(bt, kb[nx] - 0.6, kb[nx]); g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.55 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 12, 9 }); draw(kr[nx], ka[nx] * Math.PI / 180, true); g.setLineDash(new double[0]);
			}
			if (on) { L20bGlow(true); g.strokeStyle = bt - ta < 0.06 ? L20HOT : PINK; } else g.strokeStyle = L20P(0.15 + 0.3 * L20k(bt, tw, ta));
			g.lineWidth = th; draw(r, a, false);
			// the core
			double c = cang(bt); g.fillStyle = on ? PINK : L20P(0.2); g.beginPath(); for (int i = 0; i < 3; i++) { double q = c - Math.PI / 2 + i * TAU / 3; if (i == 0) g.moveTo(cx + JS.cos(q) * core, cy + JS.sin(q) * core); else g.lineTo(cx + JS.cos(q) * core, cy + JS.sin(q) * core); } g.closePath(); g.fill();
			L20bGlow(false); g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false; double r = rad(bt), a = ang(bt);
			for (int i = 0; i < 3; i++)
			{
				side(r, a, i, out double ax, out double ay, out double bx, out double by); double L = hypot(bx - ax, by - ay), u0 = Math.Max(0, 0.5 - hw / 2 / L), u1 = 1 - u0;
				if (L20Seg(p.x, p.y, ax, ay, ax + (bx - ax) * u0, ay + (by - ay) * u0) < th / 2 + p.r - 2) return true;
				if (L20Seg(p.x, p.y, ax + (bx - ax) * u1, ay + (by - ay) * u1, bx, by) < th / 2 + p.r - 2) return true;
			}
			// the core: inside it, or within reach of its sides
			double c = cang(bt); var P = new (double x, double y)[3];
			for (int i = 0; i < 3; i++) { double q = c - Math.PI / 2 + i * TAU / 3; P[i] = (cx + JS.cos(q) * core, cy + JS.sin(q) * core); }
			bool inside = true; for (int i = 0; i < 3; i++) { var A = P[i]; var Bq = P[(i + 1) % 3]; if ((Bq.x - A.x) * (p.y - A.y) - (Bq.y - A.y) * (p.x - A.x) < 0) inside = false; }
			if (inside) return true;
			for (int i = 0; i < 3; i++) if (L20Seg(p.x, p.y, P[i].x, P[i].y, P[(i + 1) % 3].x, P[(i + 1) % 3].y) < p.r - 2) return true;
			return false;
		};
		return h;
	}

	// the controller's four buttons (0 ✕, 1 ○, 2 □, 3 △) going round the ellipse rx x ry about (cx,cy): button i comes at eb[i], at its own
	// place ph[i] degrees; the ring's turn ka (degrees) and size ks (0-1) are keyframes at kb (straight lines between). R is a button's size.
	// Each one is a solid pink round button with its sign on it; it shows dashed a beat before it comes. Gone at te (they meet in the middle)
	public static (double x, double y) L20bOrbitAt(double cx, double cy, double rx, double ry, double[] kb, double[] ka, double[] ks, double ph, double bt)
	{
		double a = (L20bKey(kb, ka, bt, 0) + ph) * Math.PI / 180, s = L20bKey(kb, ks, bt, 0);
		return (cx + JS.cos(a) * rx * s, cy + JS.sin(a) * ry * s);
	}
	static void L20bSign(int type, double x, double y, double R, double rot, string col, double lw)
	{
		g.save(); g.translate(x, y); g.rotate(rot); g.strokeStyle = col; g.lineWidth = lw; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath(); double s = R * 0.5;
		if (type == 0) { g.moveTo(-s, -s); g.lineTo(s, s); g.moveTo(s, -s); g.lineTo(-s, s); }
		else if (type == 1) g.arc(0, 0, s, 0, TAU);
		else if (type == 2) g.rect(-s * 0.85, -s * 0.85, s * 1.7, s * 1.7);
		else { for (int i = 0; i < 3; i++) { double q = -Math.PI / 2 + i * TAU / 3; if (i == 0) g.moveTo(JS.cos(q) * s * 1.1, JS.sin(q) * s * 1.1 + s * 0.15); else g.lineTo(JS.cos(q) * s * 1.1, JS.sin(q) * s * 1.1 + s * 0.15); } g.closePath(); }
		g.stroke(); g.restore();
	}
	public static Hz L20bOrbit(double cx, double cy, double rx, double ry, double[] kb, double[] ka, double[] ks, double[] eb, double[] ph, double R, double te)
	{
		var h = new Hz { kind = "L20bOrbit" };
		h.update = (t, dt) => { if (BT(t) > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt > te + 0.3) return;
			for (int i = 0; i < eb.Length; i++)
			{
				if (bt < eb[i] - 1) continue; var o = L20bOrbitAt(cx, cy, rx, ry, kb, ka, ks, ph[i], bt); double rot = bt * 0.9 * (i % 2 == 0 ? 1 : -1);
				if (bt < eb[i])
				{
					double k = L20k(bt, eb[i] - 1, eb[i]); g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.6 * k)})"; g.lineWidth = 3; g.setLineDash(new double[] { 9, 7 }); g.beginPath(); g.arc(o.x, o.y, R, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
					L20bSign(i, o.x, o.y, R, rot, $"rgba(255,255,255,{str(0.3 + 0.5 * k)})", 4); continue;
				}
				double f = bt <= te ? Math.Min(1, (bt - eb[i]) / 0.15) : 1 - (bt - te) / 0.3;
				g.save(); g.globalAlpha = Math.Max(0, f); L20bGlow(true); g.fillStyle = bt - eb[i] < 0.08 ? L20HOT : PINK; g.beginPath(); g.arc(o.x, o.y, R, 0, TAU); g.fill(); L20bGlow(false);
				g.fillStyle = "rgba(120,0,40,0.35)"; g.beginPath(); g.arc(o.x, o.y, R * 0.78, 0, TAU); g.fill();
				L20bSign(i, o.x, o.y, R, rot, "rgba(255,215,228,0.95)", 6); g.restore();
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt > te) return false;
			for (int i = 0; i < eb.Length; i++) { if (bt < eb[i]) continue; var o = L20bOrbitAt(cx, cy, rx, ry, kb, ka, ks, ph[i], bt); if (hypot(p.x - o.x, p.y - o.y) < R + p.r - 4) return true; }
			return false;
		};
		return h;
	}

	// the D-pad in the middle: a hub (hb) and four arms, aw wide and al long, turned by the keyframes kb/ka (degrees, snapping in ed beats);
	// on a press (pb[i]) the arms reach the screen's edges (pl long, pw wide) for pd beats, shown dashed a beat before; the next turn shows
	// dashed 0.6 beat before. Grey from tw, kills ta .. te
	public static Hz L20bPad(double cx, double cy, double hb, double aw, double al, double[] kb, double[] ka, double ed, double[] pb, double pl, double pw, double pd, double tw, double ta, double te)
	{
		var h = new Hz { x = cx, y = cy, kind = "L20bPad" };
		double ang(double bt) => L20bKey(kb, ka, bt, ed) * Math.PI / 180;
		int press(double bt) { for (int i = 0; i < pb.Length; i++) if (bt >= pb[i] && bt < pb[i] + pd) return i; return -1; }
		void arms(double a, double len, double w) { for (int j = 0; j < 4; j++) { L20bBand(cx, cy, a + j * Math.PI / 2, 0, len, w); g.fill(); } }
		h.update = (t, dt) => { if (BT(t) > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double al2 = 1 - L20k(bt, te, te + 0.4), a = ang(bt); bool on = bt >= ta && bt < te;
			g.save(); g.globalAlpha = al2;
			// the press to come, and the next turn
			for (int i = 0; i < pb.Length; i++) if (bt >= pb[i] - 1 && bt < pb[i])
				{
					double k = L20k(bt, pb[i] - 1, pb[i]), a2 = ang(pb[i]); g.fillStyle = L20P(0.03 + 0.1 * k); arms(a2, pl, pw);
					for (int j = 0; j < 4; j++) { double q = a2 + j * Math.PI / 2, c = JS.cos(q), s = JS.sin(q); for (int sg = -1; sg <= 1; sg += 2) L20Dash(cx + c * hb - s * pw / 2 * sg, cy + s * hb + c * pw / 2 * sg, cx + c * pl - s * pw / 2 * sg, cy + s * pl + c * pw / 2 * sg, L20G(0.3 + 0.6 * k), 2); }
				}
			int nx = -1; for (int i = 0; i < kb.Length; i++) if (kb[i] > bt) { nx = i; break; }
			if (nx >= 0 && kb[nx] - bt < 0.6 && kb[nx] < te)
			{
				double k = L20k(bt, kb[nx] - 0.6, kb[nx]), a2 = ka[nx] * Math.PI / 180; g.strokeStyle = $"rgba(255,255,255,{str(0.3 + 0.55 * k)})"; g.lineWidth = 2; g.setLineDash(new double[] { 10, 8 });
				for (int j = 0; j < 4; j++) { L20bBand(cx, cy, a2 + j * Math.PI / 2, hb * 0.6, al, aw); g.stroke(); }
				g.setLineDash(new double[0]);
			}
			int pi = on ? press(bt) : -1; double len = al, w = aw;
			if (pi >= 0) { double u = (bt - pb[pi]) / pd; double e = u < 0.15 ? u / 0.15 : u > 0.8 ? (1 - u) / 0.2 : 1; len = al + (pl - al) * e; w = aw + (pw - aw) * e; }
			if (on) { L20bGlow(true); g.fillStyle = (pi >= 0 && bt - pb[pi] < 0.06) || bt - ta < 0.06 ? L20HOT : PINK; } else g.fillStyle = L20P(0.12 + 0.3 * L20k(bt, tw, ta));
			arms(a, len, w); g.beginPath(); g.arc(cx, cy, hb, 0, TAU); g.fill(); L20bGlow(false);
			// the D-pad's arrows on its arms, and the dent in the hub
			g.fillStyle = "rgba(255,215,228,0.75)";
			for (int j = 0; j < 4; j++)
			{
				double q = a + j * Math.PI / 2, c = JS.cos(q), s = JS.sin(q), d = Math.Min(al, len) - 40;
				g.beginPath(); g.moveTo(cx + c * (d + 18), cy + s * (d + 18)); g.lineTo(cx + c * d - s * 14, cy + s * d + c * 14); g.lineTo(cx + c * d + s * 14, cy + s * d - c * 14); g.closePath(); g.fill();
			}
			g.fillStyle = "rgba(120,0,40,0.4)"; g.beginPath(); g.arc(cx, cy, hb * 0.45, 0, TAU); g.fill();
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false; double a = ang(bt); int pi = press(bt); double len = al, w = aw;
			if (pi >= 0) { double u = (bt - pb[pi]) / pd; double e = u < 0.15 ? u / 0.15 : u > 0.8 ? (1 - u) / 0.2 : 1; len = al + (pl - al) * e; w = aw + (pw - aw) * e; }
			if (hypot(p.x - cx, p.y - cy) < hb + p.r - 2) return true;
			for (int j = 0; j < 4; j++) if (L20bInBand(p, cx, cy, a + j * Math.PI / 2, 0, len, w)) return true;
			return false;
		};
		return h;
	}

	// the two sticks: stick 0 at the keyframed place (kb beats, kx ky, straight lines between), stick 1 opposite it through the middle of the
	// screen, aiming the opposite way. Beam i fires at fb[i] at angle fa0[i] (degrees, stick 0) and turns to fa1[i] in fl[i] beats; it is a
	// ray bw wide out of the stick; the stick turns to it and its line shows wn beats before. The stick's head (a pink knob) hurts too
	public static (double x, double y) L20bStickAt(double[] kb, double[] kx, double[] ky, double bt) => (L20bKey(kb, kx, bt, 0), L20bKey(kb, ky, bt, 0));
	public static Hz L20bSticks(double[] kb, double[] kx, double[] ky, double[] fb, double[] fa0, double[] fa1, double[] fl, double bw, double wn, double tw, double ta, double te)
	{
		var h = new Hz { kind = "L20bSticks" }; const double BR = 92, KR = 30, KO = 46;
		// where stick 0 points at beat bt, and whether a beam is out
		double aim(double bt, out int live)
		{
			live = -1; int last = -1;
			for (int i = 0; i < fb.Length; i++) { if (bt >= fb[i] && bt <= fb[i] + fl[i]) live = i; if (bt >= fb[i]) last = i; }
			if (live >= 0) { double u = fl[live] <= 0 ? 1 : (bt - fb[live]) / fl[live]; return (fa0[live] + (fa1[live] - fa0[live]) * L20Ease(u)) * Math.PI / 180; }
			double from = last >= 0 ? fa1[last] : (fa0.Length > 0 ? fa0[0] : 0);
			int nx = last + 1; if (nx < fb.Length && bt >= fb[nx] - wn) return (from + (fa0[nx] - from) * L20Ease((bt - (fb[nx] - wn)) / Math.Min(0.2, wn))) * Math.PI / 180;
			return from * Math.PI / 180;
		}
		h.update = (t, dt) => { if (BT(t) > te + 0.5) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < tw) return; double al = 1 - L20k(bt, te, te + 0.4); bool on = bt >= ta && bt < te; var c0 = L20bStickAt(kb, kx, ky, bt);
			double a = aim(bt, out int live);
			g.save(); g.globalAlpha = al;
			for (int s = 0; s < 2; s++)
			{
				double x = s == 0 ? c0.x : W - c0.x, y = s == 0 ? c0.y : H - c0.y, q = a + s * Math.PI;
				// the next beam's line
				for (int i = 0; i < fb.Length; i++)
				{
					if (bt < fb[i] - wn || bt >= fb[i]) continue; double k = L20k(bt, fb[i] - wn, fb[i]), q2 = fa0[i] * Math.PI / 180 + s * Math.PI;
					g.fillStyle = L20P(0.03 + 0.1 * k); L20bBand(x, y, q2, BR * 0.5, 1700, bw); g.fill();
					L20Dash(x + JS.cos(q2) * BR, y + JS.sin(q2) * BR, x + JS.cos(q2) * 1700, y + JS.sin(q2) * 1700, L20G(0.35 + 0.55 * k), 2 + k);
					if (Math.Abs(fa1[i] - fa0[i]) > 1) { double q3 = fa1[i] * Math.PI / 180 + s * Math.PI; L20Dash(x + JS.cos(q3) * BR, y + JS.sin(q3) * BR, x + JS.cos(q3) * 700, y + JS.sin(q3) * 700, L20G(0.2 + 0.4 * k), 2); g.strokeStyle = L20G(0.2 + 0.4 * k); g.lineWidth = 2; g.beginPath(); g.arc(x, y, 300, Math.Min(q2, q3), Math.Max(q2, q3)); g.stroke(); }
				}
				// the stick's base (harmless)
				g.strokeStyle = "rgba(200,210,240,0.45)"; g.lineWidth = 4; g.beginPath(); g.arc(x, y, BR, 0, TAU); g.stroke(); g.fillStyle = "rgba(30,34,60,0.5)"; g.fill();
				// the beam
				if (on && live >= 0)
				{
					double u = bt - fb[live], w2 = bw * Math.Min(1, 0.5 + u / 0.06); L20bGlow(true); g.fillStyle = u < 0.06 ? L20HOT : PINK; L20bBand(x, y, q, 0, 1700, w2); g.fill(); L20bGlow(false);
					g.fillStyle = "rgba(255,150,185,0.6)"; L20bBand(x, y, q, 0, 1700, w2 / 4); g.fill();
				}
				// the knob
				double kx2 = x + JS.cos(q) * KO, ky2 = y + JS.sin(q) * KO;
				if (on) L20bGlow(true); g.fillStyle = on ? PINK : L20P(0.2 + 0.3 * L20k(bt, tw, ta)); g.beginPath(); g.arc(kx2, ky2, KR, 0, TAU); g.fill(); L20bGlow(false);
				g.strokeStyle = "rgba(255,215,228,0.6)"; g.lineWidth = 3; g.beginPath(); g.arc(kx2, ky2, KR * 0.6, 0, TAU); g.stroke();
			}
			g.restore();
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t); if (bt < ta || bt >= te) return false; var c0 = L20bStickAt(kb, kx, ky, bt); double a = aim(bt, out int live);
			for (int s = 0; s < 2; s++)
			{
				double x = s == 0 ? c0.x : W - c0.x, y = s == 0 ? c0.y : H - c0.y, q = a + s * Math.PI;
				if (hypot(p.x - (x + JS.cos(q) * KO), p.y - (y + JS.sin(q) * KO)) < KR + p.r - 3) return true;
				if (live >= 0 && L20bInBand(p, x, y, q, 0, 1700, bw * Math.Min(1, 0.5 + (bt - fb[live]) / 0.06))) return true;
			}
			return false;
		};
		return h;
	}

	// the triangle's shards: from (sx,sy) at beat sb[i] a small pink triangle flies at sa degrees, v px/s; its path shows wn beats before
	public static Hz L20bShards(double[] sb, double[] sx, double[] sy, double[] sa, double v, double R, double wn)
	{
		var h = new Hz { kind = "L20bShards" }; double last = sb.Length > 0 ? sb.Max() : 0;
		h.update = (t, dt) => { if (BT(t) > last + 8) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t);
			for (int i = 0; i < sb.Length; i++)
			{
				double u = bt - sb[i], a = sa[i] * Math.PI / 180, c = JS.cos(a), s = JS.sin(a); if (u < -wn) continue;
				if (u < 0) { double k = 1 + u / wn; L20Dash(sx[i], sy[i], sx[i] + c * 1800, sy[i] + s * 1800, L20G(0.3 + 0.6 * k), 2); continue; }
				double d = v * (t - B(sb[i])), x = sx[i] + c * d, y = sy[i] + s * d; if (d > 2200) continue;
				L20bGlow(true); g.fillStyle = PINK; g.beginPath(); g.moveTo(x + c * R, y + s * R); g.lineTo(x - c * R * 0.6 - s * R * 0.8, y - s * R * 0.6 + c * R * 0.8); g.lineTo(x - c * R * 0.6 + s * R * 0.8, y - s * R * 0.6 - c * R * 0.8); g.closePath(); g.fill(); L20bGlow(false);
			}
		};
		h.hit = (p, t) =>
		{
			double bt = BT(t);
			for (int i = 0; i < sb.Length; i++)
			{
				if (bt < sb[i]) continue; double a = sa[i] * Math.PI / 180, d = v * (t - B(sb[i])); if (d > 2200) continue;
				if (hypot(p.x - (sx[i] + JS.cos(a) * d), p.y - (sy[i] + JS.sin(a) * d)) < R * 0.5 + p.r - 2) return true;
			}
			return false;
		};
		return h;
	}

	// "PRESS START" (harmless): blinks from b0, pressed at b1 (it flashes and goes)
	public static Hz L20bTitle(double b0, double b1)
	{
		var h = new Hz { kind = "L20bTitle" };
		h.update = (t, dt) => { if (BT(t) > b1 + 1) h.dead = true; };
		h.draw = t =>
		{
			double bt = BT(t); if (bt < b0 || bt > b1 + 0.8) return; double a = bt < b1 ? (Math.Floor(bt * 2) % 2 == 0 ? 0.9 : 0.35) : 1 - (bt - b1) / 0.8, s = bt < b1 ? 1 : 1 + (bt - b1) * 0.5;
			g.save(); g.globalAlpha = Math.Max(0, a); g.font = $"800 {str(64 * s)}px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle";
			g.shadowColor = "#ffffff"; g.shadowBlur = 24; g.fillStyle = "#ffffff"; g.fillText("PRESS START", W / 2, H / 2); g.shadowBlur = 0; g.restore();
		};
		return h;
	}

	static readonly int _k20bcross = L20K("L20b_cross", e => () => hazards.Add(L20bCross(e.nums("xb"), e.nums("xx"), e.nums("xy"), e.nums("xa"), e.num("w"), e.num("wn"), e.num("lh"))));
	static readonly int _k20bring = L20K("L20b_ring", e => () =>
		hazards.Add(L20bRing(e.num("x"), e.num("y"), e.num("ta"), e.i("sides"), e.num("rot"), e.num("ga"), e.num("gw"), e.num("v"), e.num("th"), e.num("r0"), e.num("wn"))));
	static readonly int _k20btiles = L20K("L20b_tiles", e => () => hazards.Add(L20bTiles(e.i("cols"), e.num("cw"), e.num("ch"), e.nums("tb"), e.nums("tn"), e.nums("tc"), e.num("wn"), e.num("lh"))));
	static readonly int _k20btri = L20K("L20b_tri", e => () =>
		hazards.Add(L20bTri(e.num("x", W / 2), e.num("y", H / 2), e.nums("kb"), e.nums("kr"), e.nums("ka"), e.num("ed"), e.num("hw"), e.num("th"), e.num("core"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20borbit = L20K("L20b_orbit", e => () =>
		hazards.Add(L20bOrbit(e.num("x", W / 2), e.num("y", H / 2), e.num("rx"), e.num("ry"), e.nums("kb"), e.nums("ka"), e.nums("ks"), e.nums("eb"), e.nums("ph"), e.num("R"), e.num("te"))));
	static readonly int _k20bpad = L20K("L20b_pad", e => () =>
		hazards.Add(L20bPad(e.num("x", W / 2), e.num("y", H / 2), e.num("hb"), e.num("aw"), e.num("al"), e.nums("kb"), e.nums("ka"), e.num("ed"), e.nums("pb"), e.num("pl"), e.num("pw"), e.num("pd"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20bsticks = L20K("L20b_sticks", e => () =>
		hazards.Add(L20bSticks(e.nums("kb"), e.nums("kx"), e.nums("ky"), e.nums("fb"), e.nums("fa0"), e.nums("fa1"), e.nums("fl"), e.num("bw"), e.num("wn"), e.num("tw"), e.num("ta"), e.num("te"))));
	static readonly int _k20bshards = L20K("L20b_shards", e => () => hazards.Add(L20bShards(e.nums("sb"), e.nums("sx"), e.nums("sy"), e.nums("sa"), e.num("v"), e.num("R"), e.num("wn"))));
	static readonly int _k20btitle = L20K("L20b_title", e => () => hazards.Add(L20bTitle(e.num("b0"), e.num("b1"))));
}
