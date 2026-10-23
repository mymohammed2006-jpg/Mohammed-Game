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
//          ECHO_T seconds, blinking at its end. A jump off the beat sends no wave.
public class TWave { public double x, y, r, r0, R; public int who; public double born; public bool miss; }

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
	static double tmStaticGain(double m) => 0.42 * Math.Pow(Math.Max(0, m), 1.3);
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
			double d = tmRectDist(w.x, w.y, s); if (d <= Math.Min(w.r, w.R) && (d > w.r0 || w.r0 == 0)) { s.timer = ECHO_T; s.s = 1; }
		}
		s.timer -= dt; s.solid = s.timer > 0;
	}
}
