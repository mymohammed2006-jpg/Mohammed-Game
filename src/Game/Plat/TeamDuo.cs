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
public class TTower { public G.TS s; public int ph; public long callAt = -1; public int at; public bool[,] did = new bool[16, 2]; public double failT = -9, okT = -9, doneT = -9; public long lastBeat = -1; }

public static partial class G
{
	public const double ECHO_T = 1.0;        // how long a lit cloud stands (about a beat and a half)
	const double ECHO_R = 300, ECHO_V = 1000, ECHO_WIN = 0.2;   // a wave's reach (px), its speed (px a second), the beat window (in beats, either side)
	public static string tmCol(int i) => tm?.L?.cols != null && i < tm.L.cols.Length ? TM_COL[tm.L.cols[i]] : TM_COL[i];
	public static bool tmDuo => tm?.P != null && tm.P.murk >= 0;

	static void tmDuoBuild()
	{
		tm.waves.Clear();
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
}
