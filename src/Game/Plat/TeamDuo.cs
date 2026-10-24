using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// The radio level (his words 19:02 10-23): co-op for exactly two, a close camera so each one finds the way on their own screen,
// four pages with no rooms (each page done is the checkpoint), and the song heard through a broken radio: garbled at the
// start, each sound they take clears it a little (pages 1-3 very slowly, page 4 all the way).
// Page one's thing (a = a player: 0 blue, 1 gold):
//   echo   a cloud no one sees, only player a stands on it: a jump ON the beat sends a sound wave out of the jumper, and the
//          wave lights the OTHER player's clouds it passes, so each one's way is lit by the other; a lit cloud stands for
//          ECHO_T seconds (c seconds when c is set), blinking at its end. A jump off the beat sends no wave.
//   tower  the page's end (his words 20:06 10-23: no gate, something new): a radio mast that the page's sounds light up. With
//          all of them in and both on their pads (`need` = the two pads' ids), it sings a phrase on the beats (`alt` = the
//          phrases: B blue jumps, G gold jumps, X both, . no one), then the two answer it with their jumps on the same beats.
//          A jump that is not theirs, or a missed one, spoils it and the mast sings it again; all phrases answered = page done.
public class TWave { public double x, y, r, r0, R; public int who; public double born; public bool miss; }
// Page three's things:
//   spk    a speaker at tile (x, y) that sends a pink sound wave every b beats (from beat c): a wall h tiles tall going a
//          (1 right, -1 left) at v px a second until x1; rock stops each row of it, and so does the shield
// The shield: one of the two carries it (blue at the start); a wave's rows that reach it stop there, so it keeps safe the
// one carrying it and the one right behind it. The one without it takes it by holding down while touching the other.
public class TBar { public double x, x0; public int dir, r0; public bool[] on; public double v, x1, born; }
public class TTower { public G.TS s; public int ph; public long callAt = -1; public int at; public bool[,] did = new bool[16, 2]; public double failT = -9, okT = -9, doneT = -9; public long lastBeat = -1; }

// Page four's things:
//   rise   the pink static coming up from below (row y): a px a second from c seconds in, and never more than b px under the
//          lower of the two (it catches up fast), so waiting anywhere costs; touching it hurts. More of them: each one's a and
//          b hold while the lower one is between rows y1 and y1 + h (the first one's y and c start it)
//   ropez  a part of the page where the cable ties the two (only while one of them is in it; no dash while it does)
public static partial class G
{
	public const double ECHO_T = 1.0;        // how long a lit cloud stands (about a beat and a half)
	const double ECHO_R = 300, ECHO_V = 1000, ECHO_WIN = 0.2;   // a wave's reach (px), its speed (px a second), the beat window (in beats, either side)
	public static string tmCol(int i) => tm?.L?.cols != null && i < tm.L.cols.Length ? TM_COL[tm.L.cols[i]] : TM_COL[i];
	public static bool tmDuo => tm?.P != null && tm.P.murk >= 0;

	static void tmDuoBuild()
	{
		tm.waves.Clear(); tm.bars.Clear(); tm.shield = 0; tm.shieldT = 0; tm.ropeLive = false;
		var rise = tm.P.things.FirstOrDefault(d => d.k == "rise"); tm.riseY = rise == null ? double.NaN : rise.y * PT;
		int ns = tm.P.things.Count(d => d.k == "spk"); tm.spkN = ns > 0 ? Enumerable.Repeat(long.MinValue, ns).ToArray() : null;
		if (tm.P.murk < 0) return;
		tm.murk = tm.P.murk; tmMurkTo(0.05);   // back to the page's start (a restart puts its sounds back)
	}
	// the radio's sound: the song's own voices through the broken radio, and his radio static over them
	static void tmDuoMusic(double when)
	{
		if (tm.P.murk < 0 || noMus) return;
		foreach (var v in tm.voices) if (v != null) { v.murk = new AParam(tm.murk); }
		if (!staticTried) { staticTried = true; staticBuf = LoadSong("SFX_STATIC"); }
		if (staticBuf == null) return;
		var s = new Voice { song = staticBuf, loop = true, loopStart = STATIC_LOOP, loopEnd = 2 * STATIC_LOOP };
		s.gain.value = tmStaticGain(tm.murk); s.start(when, STATIC_LOOP); tm.stat = new[] { AudioCtx.Add(s) };
	}
	static double tmStaticGain(double m) => 0.23 * Math.Pow(Math.Max(0, m), 1.5);
	static void tmMurkTo(double tc)
	{
		if (tm.voices != null) foreach (var v in tm.voices) v?.murk?.setTargetAtTime(tm.murk, actx.currentTime, tc);
		if (tm.stat != null) foreach (var v in tm.stat) v?.gain.setTargetAtTime(tmStaticGain(tm.murk), actx.currentTime, tc);
	}
	// a sound taken: the radio clears a step (slowly, over about a second, so it is heard opening up)
	static void tmDuoTake()
	{
		if (tm.P.murk < 0) return;
		tm.murk = Math.Max(0, tm.P.murk - tm.got.Count * tm.P.mstep); if (tm.murk < 0.02) tm.murk = 0;
		tmMurkTo(0.35);
	}
	// how far from the nearest beat (in beats)
	static double tmOffBeat() { double b = tmBeat(); return Math.Abs(b - Math.Round(b)); }
	static void tmDuoJump(TBody b)
	{
		if (!tm.P.things.Any(d => d.k == "echo")) return;
		bool on = tmOffBeat() <= ECHO_WIN;
		tmTowerJump(b.i, on);
		tm.waves.Add(new TWave { x = b.x, y = b.y, R = on ? ECHO_R : 46, who = b.i, born = tm.t, miss = !on });
		if (on) sfx(tt => { sTone(tt, 1320, 0.09, "sine", 0.06); sTone(tt + 0.03, 660, 0.25, "sine", 0.05); });
	}
	// once a step, before the things: the waves spread
	static void tmWaves(double dt)
	{
		foreach (var w in tm.waves) { w.r0 = w.r; w.r = Math.Min(w.R + 60, w.r + ECHO_V * dt); }
		tm.waves.RemoveAll(w => w.r >= w.R + 60);
	}
	static double tmRectDist(double x, double y, TS s) { double dx = Math.Max(Math.Max(s.X - x, 0), x - (s.X + s.W)), dy = Math.Max(Math.Max(s.Y - y, 0), y - (s.Y + s.H)); return hypot(dx, dy); }
	static void tmEchoStep(TS s, double dt)
	{
		foreach (var w in tm.waves)
		{
			if (w.miss || w.who == (int)s.d.a || w.r0 >= w.R) continue;
			double d = tmRectDist(w.x, w.y, s); if (d <= Math.Min(w.r, w.R) && (d > w.r0 || w.r0 == 0)) { s.timer = s.d.c > 0 ? s.d.c : ECHO_T; s.s = 1; }
		}
		s.timer -= dt; s.solid = s.timer > 0;
	}

	// ---------- the mast at the page's end ----------
	static string[] twPh => tm.tw.s.d.alt;
	public static bool twReady => tm.tw != null && tm.got.Count >= tmPieces();
	// the phrase's beats: the call from callAt, the answer right after it
	public static int twLen => twPh[tm.tw.ph].Length;
	static bool twWants(char c, int who) => c == 'X' || (c == 'B' && who == 0) || (c == 'G' && who == 1);
	static bool twOnPads() { var n = tm.tw.s.d.need; return n != null && tm.b.All(b => !b.down && !b.gate && b.ground && b.onT != null && b.onT.d.id == n[b.i]) ; }
	static void tmTowerStep()
	{
		var w = tm.tw; if (!twReady) return;
		double bt = tmBeat(); long bi = (long)Math.Floor(bt);
		// start (again) on the next bar once both stand on their pads
		if (w.callAt < 0) { if (twOnPads()) { w.callAt = (bi / 4 + 1) * 4; w.at = 0; Array.Clear(w.did); } return; }
		long a0 = w.callAt + twLen;
		// the call: a tone on each of its beats
		if (bi != w.lastBeat) { w.lastBeat = bi; long k = bi - w.callAt; if (k >= 0 && k < twLen) { char c = twPh[w.ph][(int)k]; sfx(tt => twTone(tt, c, 0.09)); } }
		// the answer: each beat once its window has passed, the ones it wanted must have jumped
		while (w.at < twLen && bt > a0 + w.at + ECHO_WIN)
		{
			char c = twPh[w.ph][w.at];
			for (int p = 0; p < 2; p++) if (twWants(c, p) && !w.did[w.at, p]) { tmTowerFail(); return; }
			w.at++; w.okT = tm.t;
		}
		if (w.at >= twLen)
		{
			w.ph++; w.at = 0; Array.Clear(w.did);
			if (w.ph >= twPh.Length) { w.doneT = tm.t; tmTowerWin(); return; }
			w.callAt = ((long)Math.Floor(bt) / 4 + 1) * 4;
			sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.1); sTone(tt + 0.08, 1175, 0.2, "triangle", 0.1); });
		}
	}
	// a jump while the mast listens: on its beat and theirs, it counts; anything else spoils the phrase
	static void tmTowerJump(int who, bool onBeat)
	{
		var w = tm.tw; if (w == null || w.callAt < 0 || w.doneT >= 0 || !twReady) return;
		double bt = tmBeat(); long a0 = w.callAt + twLen; long k = (long)Math.Round(bt) - a0;
		if (bt < a0 - ECHO_WIN || k >= twLen) return;   // during the call they jump as they like
		if (!onBeat || k < 0 || !twWants(twPh[w.ph][(int)k], who) || w.did[k, who]) { tmTowerFail(); return; }
		w.did[k, who] = true;
	}
	static void tmTowerFail()
	{
		var w = tm.tw; w.failT = tm.t; w.at = 0; Array.Clear(w.did); w.callAt = ((long)Math.Floor(tmBeat()) / 4 + 1) * 4;
		shake = Math.Max(shake, 6); sfx(tt => { sNoise(tt, 0.35, "bandpass", 900, 0.16); sTone(tt, 180, 0.3, "sawtooth", 0.06); sTone(tt + 0.1, 120, 0.3, "sawtooth", 0.06); });
	}
	static void tmTowerWin()
	{
		var s = tm.tw.s; flash = 0.6; shake = 12; burst(s.X + s.W / 2, s.Y, "#ffffff", 80, 700);
		tm.waves.Add(new TWave { x = s.X + s.W / 2, y = s.Y + 20, R = 2600, who = -1, born = tm.t });
		sfx(tt => { sNoise(tt, 0.5, "highpass", 2500, 0.18); sTone(tt + 0.05, 523, 0.4, "triangle", 0.12); sTone(tt + 0.2, 784, 0.4, "triangle", 0.12); sTone(tt + 0.35, 1047, 0.8, "triangle", 0.12); });
		tm.doneAt = tm.t; tnTell("pd", new() { ["t"] = tm.t });
	}
	static void twTone(double tt, char c, double v)
	{
		if (c == 'B' || c == 'X') sTone(tt, 659, 0.22, "triangle", v);
		if (c == 'G' || c == 'X') sTone(tt, 988, 0.22, "triangle", v);
		if (c == '.') sTone(tt, 1500, 0.03, "sine", v * 0.4);
	}

	// ---------- the cable (the idea after his words 20:44 10-23: no jumping on a beat) ----------
	// the two are tied: never farther apart than the cable. One on the ground holds the other one, who hangs and swings (the
	// stick pushes the swing, a jump lets go of it); the one holding walks away and pulls the other one up. Both off the
	// ground: nothing holds them.
	const double ROPE_PUMP = 2000, ROPE_MAXV = 1100, ROPE_FALL = 1500, ROPE_JUMP = 1.15;   // a jump off the cable goes a little higher (the cable throws you)
	static double tmSwing(TBody b, double mx, double dt) => Math.Clamp((b.vx + mx * ROPE_PUMP * dt) * (1 - 0.15 * dt), -ROPE_MAXV, ROPE_MAXV);
	static void tmRope()
	{
		var a = tm.b[0]; var c = tm.b[1]; a.hang = c.hang = false;
		tm.ropeLive = tmRopeOn(); if (!tm.ropeLive) return;
		if (a.down || c.down || a.gate || c.gate) return;
		double L = tm.P.rope, len = tmRopePath(a, c, out double px, out double py); if (len <= L) return;
		bool piv = !double.IsNaN(px);
		double ax = piv ? px : c.x, ay = piv ? py : c.y, cx = piv ? px : a.x, cy = piv ? py : a.y;   // where each one's part of the cable goes
		if (a.ground && !c.ground) tmHold(a, c, cx, cy, len - L);
		else if (c.ground && !a.ground) tmHold(c, a, ax, ay, len - L);
		else if (!a.ground)
		{
			double e = (len - L) / 2; tmPull(a, ax, ay, e); tmPull(c, cx, cy, e);
			if (!piv)
			{
				double d = Math.Max(1, hypot(c.x - a.x, c.y - a.y)), nx = (c.x - a.x) / d, ny = (c.y - a.y) / d, va = a.vx * nx + a.vy * ny, vc = c.vx * nx + c.vy * ny;
				if (vc > va) { double m = (va + vc) / 2; a.vx += (m - va) * nx; a.vy += (m - va) * ny; c.vx += (m - vc) * nx; c.vy += (m - vc) * ny; }
			}
		}
		else
		{
			bool am = Math.Abs(a.mdx) >= Math.Abs(c.mdx); var m = am ? a : c; var o = am ? c : a;
			double qx = am ? ax : cx, qy = am ? ay : cy; tmHoldX(m, qx, qy, L - (piv ? hypot(o.x - px, o.y - py) : 0));
		}
	}
	// a page with cable zones (the last page): the cable ties them once both are in one, and holds while one of them still is
	// (so the one out first pulls the other one out; one falling back in later is not tied to the other one far away)
	static bool tmRopeOn()
	{
		if (!tm.P.things.Any(z => z.k == "ropez")) return true;
		int n = 0;
		foreach (var b in tm.b) if (!b.down && !b.gate && tm.P.things.Any(z => z.k == "ropez" && b.x > z.x * PT && b.x < (z.x + z.w) * PT && b.y > z.y * PT && b.y < (z.y + z.h) * PT)) n++;
		return tm.ropeLive ? n > 0 : n == tm.b.Length;
	}
	// h hangs from the one on the ground, o (its part of the cable goes to q: o itself, or the edge it bends over)
	static void tmHold(TBody o, TBody h, double qx, double qy, double e)
	{
		tmPull(h, qx, qy, e); h.hang = !h.ground && h.y > qy + 10;   // it holds you only from below if (h.hang) { h.coyote = Math.Max(h.coyote, P_COYOTE); h.ropeC = P_COYOTE; }   // a jump lets go of the cable (a moment after it slackens too)
		double len = tmRopePath(o, h, out double px, out double py);
		if (len > tm.P.rope + 1)   // stuck behind a wall: the one holding can't walk on
		{
			bool piv = !double.IsNaN(px);
			tmHoldX(o, piv ? px : h.x, piv ? py : h.y, tm.P.rope - (piv ? hypot(h.x - px, h.y - py) : 0));
		}
	}
	// pulled e along its part of the cable (toward q), and no speed away from q
	static void tmPull(TBody h, double qx, double qy, double e)
	{
		double d = Math.Max(1, hypot(h.x - qx, h.y - qy)), nx = (h.x - qx) / d, ny = (h.y - qy) / d;
		if (h.y > qy - P_HALF - 3 && Math.Abs(h.x - qx) < 16)   // right under the edge: up it, and over it once nearly there
		{
			if (h.y + P_HALF - qy <= 24) { tmShove(h, 0, qy - P_HALF - 2 - h.y); tmShove(h, Math.Sign(qx - h.x) * 6, 0); }
			else { tmShove(h, 0, -e); tmShove(h, qx - h.x, 0); }
		}
		else
		{
			double x0 = h.x, wx = -nx * e; tmShove(h, wx, -ny * e);
			// dragged against a lip it nearly clears: over it
			if (Math.Abs(wx) > 0.01 && Math.Abs(h.x - x0) < Math.Abs(wx) * 0.5)
			{
				int tx = (int)Math.Floor((h.x + Math.Sign(wx) * (P_HALF + 1)) / PT), ty = (int)Math.Floor((h.y + P_HALF - 0.01) / PT);
				double top = ty * PT; if (tmTile(tx, ty) && !tmTile(tx, ty - 1) && h.y + P_HALF - top <= 22) { tmShove(h, 0, top - P_HALF - 0.5 - h.y); tmShove(h, wx - (h.x - x0), 0); }
			}
		}
		double vr = h.vx * nx + h.vy * ny; if (vr > 0) { h.vx -= vr * nx; h.vy -= vr * ny; }
	}
	// m stays where the cable lets it: no farther than L from q (back toward it, sideways)
	static void tmHoldX(TBody m, double qx, double qy, double L)
	{
		L = Math.Max(0, L);
		double dy = Math.Abs(m.y - qy), side = Math.Sign(m.x - qx), nx = dy >= L ? qx : qx + side * Math.Sqrt(L * L - dy * dy);
		if (Math.Abs(nx - qx) < Math.Abs(m.x - qx)) { tmShove(m, nx - m.x, 0); if (Math.Sign(m.vx) == side) m.vx = 0; }
	}
	// the cable's length between the two: straight, or bent over one edge of rock (px, py; NaN when straight)
	public static double tmRopePath(TBody a, TBody c, out double px, out double py)
	{
		px = py = double.NaN;
		if (tmClear(a.x, a.y, c.x, c.y)) return hypot(c.x - a.x, c.y - a.y);
		double best = double.MaxValue;
		int i0 = (int)Math.Floor(Math.Min(a.x, c.x) / PT) - 1, i1 = (int)Math.Ceiling(Math.Max(a.x, c.x) / PT) + 1, j0 = (int)Math.Floor(Math.Min(a.y, c.y) / PT) - 1, j1 = (int)Math.Ceiling(Math.Max(a.y, c.y) / PT) + 1;
		for (int i = i0; i <= i1; i++) for (int j = j0; j <= j1; j++)
		{
			bool s00 = tmTile(i - 1, j - 1), s10 = tmTile(i, j - 1), s01 = tmTile(i - 1, j), s11 = tmTile(i, j);
			if ((s00 ? 1 : 0) + (s10 ? 1 : 0) + (s01 ? 1 : 0) + (s11 ? 1 : 0) != 1) continue;   // only an outer corner
			double x = i * PT + (s00 || s01 ? 2 : -2), y = j * PT + (s00 || s10 ? 2 : -2);
			double l = hypot(x - a.x, y - a.y) + hypot(c.x - x, c.y - y);
			if (l < best && tmClear(a.x, a.y, x, y) && tmClear(x, y, c.x, c.y)) { best = l; px = x; py = y; }
		}
		return best < double.MaxValue ? best : hypot(c.x - a.x, c.y - a.y);
	}
	static bool tmClear(double x0, double y0, double x1, double y1)
	{
		int n = (int)Math.Ceiling(hypot(x1 - x0, y1 - y0) / 6);
		for (int k = 1; k < n; k++) { double x = x0 + (x1 - x0) * k / n, y = y0 + (y1 - y0) * k / n; if (tmTile((int)Math.Floor(x / PT), (int)Math.Floor(y / PT))) return false; }
		return true;
	}
	// moved by the cable, against the walls (in small steps so it never goes through one)
	static void tmShove(TBody b, double dx, double dy)
	{
		int n = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) / 12); if (n == 0) return;
		for (int k = 0; k < n; k++)
		{
			double sx = dx / n, sy = dy / n;
			b.x += sx;
			int ty0 = (int)Math.Floor((b.y - P_HALF + 0.01) / PT), ty1 = (int)Math.Floor((b.y + P_HALF - 0.01) / PT);
			if (sx > 0) { int tx = (int)Math.Floor((b.x + P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = tx * PT - P_HALF; b.vx = Math.Min(0, b.vx); break; } }
			else if (sx < 0) { int tx = (int)Math.Floor((b.x - P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = (tx + 1) * PT + P_HALF; b.vx = Math.Max(0, b.vx); break; } }
			b.y += sy;
			int tx0 = (int)Math.Floor((b.x - P_HALF + 0.01) / PT), tx1 = (int)Math.Floor((b.x + P_HALF - 0.01) / PT);
			if (sy > 0) { int ty = (int)Math.Floor((b.y + P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (tmTile(tx, ty)) { b.y = ty * PT - P_HALF; b.vy = Math.Min(0, b.vy); b.ground = true; break; } }
			else if (sy < 0) { int ty = (int)Math.Floor((b.y - P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (tmTile(tx, ty)) { b.y = (ty + 1) * PT + P_HALF; b.vy = Math.Max(0, b.vy); break; } }
		}
	}
	// for the bots: where a saw is `ahead` seconds from now
	public static double tmSawPosPub(PlatSaw s, double ahead, out double y)
	{
		double b = tmBeat() + ahead / tm.L.spb, k0 = b / s.beats + s.ph, f = k0 - Math.Floor(k0), k = f < 0.5 ? f * 2 : 2 - f * 2; k = k * k * (3 - 2 * k);
		y = (s.y0 + (s.y1 - s.y0) * k) * PT + PT / 2; return (s.x0 + (s.x1 - s.x0) * k) * PT + PT / 2;
	}

	// ---------- the keys (page two) ----------
	// on the radio level a key let go still holds what it drives for KEY_HOLD seconds (it blinks), so the one out on it
	// can get off; on the other levels it lets go at once
	public const double KEY_HOLD = 1.0;
	static bool tmKeyHeld(TS s, double dt)
	{
		if (tmNeed(s.d)) { s.timer = KEY_HOLD; return true; }
		if (!tmDuo) return false;
		s.timer -= dt; return s.timer > 0;
	}

	// ---------- the shield (page three) ----------
	public const double BAR_W = 14, SHIELD_R = 24;
	static void tmSpeakers(TIn[] ins, double dt)
	{
		double bt = tmBeat(); int i = 0;
		foreach (var s in tm.th)
		{
			if (s.k != "spk") continue;
			var d = s.d; long n = (long)Math.Floor((bt - d.c) / d.b);
			if (tm.spkN[i] != long.MinValue && n > tm.spkN[i] && bt >= d.c)
			{
				int rows = Math.Max(1, (int)Math.Round(d.h)); var w = new TBar { x = (d.x + 0.5 + 0.5 * d.a) * PT, dir = (int)d.a, r0 = (int)d.y, on = Enumerable.Repeat(true, rows).ToArray(), v = d.v > 0 ? d.v : 420, x1 = d.x1 * PT, born = tm.t };
				w.x0 = w.x; tm.bars.Add(w); s.timer = tm.t;
				sfx(tt => { sTone(tt, 70, 0.35, "sawtooth", 0.07); sNoise(tt, 0.3, "lowpass", 400, 0.12); });
			}
			tm.spkN[i] = n; i++;
		}
		var sh = tm.b[tm.shield]; bool shOn = !sh.down && !sh.gate;
		foreach (var w in tm.bars)
		{
			w.x += w.dir * w.v * dt;
			for (int r = 0; r < w.on.Length; r++)
			{
				if (!w.on[r]) continue;
				int row = w.r0 + r, tx = (int)Math.Floor((w.x + w.dir * BAR_W / 2) / PT);
				if (tmTile(tx, row)) { w.on[r] = false; continue; }
				// the shield takes the rows that reach it
				double ry0 = row * PT, ry1 = ry0 + PT;
				if (shOn && sh.y + SHIELD_R > ry0 && sh.y - SHIELD_R < ry1 && Math.Abs(w.x - sh.x) < SHIELD_R + BAR_W / 2)
				{ w.on[r] = false; burst(sh.x + Math.Sign(w.x0 - sh.x) * SHIELD_R, (ry0 + ry1) / 2, "#ffffff", 4, 160); tm.shieldT = Math.Max(tm.shieldT, 0); }
			}
		}
		tm.bars.RemoveAll(w => !w.on.Any(o => o) || (w.dir > 0 ? w.x > w.x1 : w.x < w.x1));
		// passing the shield: the one without it holds down while touching the one with it
		tm.shieldT -= dt;
		var o2 = tm.b[1 - tm.shield];
		if (tm.shieldT <= 0 && shOn && !o2.down && !o2.gate && ins != null && ins.Length > o2.i && ins[o2.i].y > 0.5 && Math.Abs(o2.x - sh.x) < 24 && Math.Abs(o2.y - sh.y) < 24)
		{
			tm.shield = o2.i; tm.shieldT = 0.4; burst(o2.x, o2.y, "#ffffff", 16, 260);
			sfx(tt => { sTone(tt, 880, 0.08, "triangle", 0.08); sTone(tt + 0.06, 1320, 0.12, "triangle", 0.07); });
		}
		// the one carrying it goes down: it stays with them (the other can take it from them once they are back)
	}
	static bool tmBarHit(TBody b)
	{
		if (b.i == tm.shield) return false;
		foreach (var w in tm.bars)
		{
			if (Math.Abs(w.x - b.x) > BAR_W / 2 + 7) continue;
			for (int r = 0; r < w.on.Length; r++) if (w.on[r]) { double ry0 = (w.r0 + r) * PT; if (b.y + 7 > ry0 && b.y - 7 < ry0 + PT) return true; }
		}
		return false;
	}
	// for the drawing: how near a speaker's next wave is (0 far, 1 now)
	public static double tmSpkWarn(TS s) { double bt = tmBeat(), k = (bt - s.d.c) / s.d.b; double f = k - Math.Floor(k); return bt < s.d.c - 1 ? 0 : Math.Max(0, 1 - (1 - f) * s.d.b); }
	static void tmRise(double dt)
	{
		var d = tm.P.things.First(q => q.k == "rise"); if (tm.pageAt < d.c) return;
		double low = double.NaN; foreach (var b in tm.b) if (!b.down && !b.gate) low = double.IsNaN(low) ? b.y : Math.Max(low, b.y);
		if (!double.IsNaN(low)) foreach (var q in tm.P.things) if (q.k == "rise" && low > q.y1 * PT && low < (q.y1 + q.h) * PT) { d = q; break; }   // its speed and reach where the lower one is
		double v = d.a; if (!double.IsNaN(low) && tm.riseY > low + d.b) v = Math.Max(v, (tm.riseY - low - d.b) * 2.5);
		tm.riseY -= v * dt;
	}
	// the last page: both at the microphone. The song is clear now and plays on to its end (no more looping), then the cards
	static void tmMicWin()
	{
		tm.doneAt = tm.t; tm.won = true; tm.wonAt = tm.t; flash = 0.9; shake = 10; burst(W / 2, H / 2, "#ffffff", 90, 640);
		sfx(tt => { sNoise(tt, 0.3, "highpass", 3000, 0.2); sTone(tt + 0.05, 523, 0.3, "triangle", 0.12); sTone(tt + 0.17, 659, 0.3, "triangle", 0.12); sTone(tt + 0.29, 784, 0.6, "triangle", 0.12); });
		tm.murk = 0; tmMurkTo(0.3);
		if (tm.voices != null) foreach (var v in tm.voices) if (v != null) v.loop = false;
		if (tmMe >= 0) tnWon();
	}
}
