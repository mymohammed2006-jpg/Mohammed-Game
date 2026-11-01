using Godot;
using System;
using System.Collections.Generic;

namespace MG;

// v58 (his rule 11-01, all levels now and later): when a level ends the song keeps going to its real end, and when it ends
// while the win screen is still up it starts again from the start of the level's song, until he leaves the screen.
// A song file cut at the level end gets a second file songs/<file>_TAIL.mp3 = the rest of the original song, from 'at'
// (the song time of its first sample, 0.5 s before the level end) to the song's real end. At the level end (B(end)) the
// song switches to it with a 2 ms crossfade, so everything up to the level end is the same file, sample for sample, as before.
// 'gain' = how loud the old file is against the tail where both play (the 0.5 s before the level end), so they match.
public static partial class G
{
	sealed class SongTail { public double at, gain, len; public AudioStream buf; }
	static readonly Dictionary<string, (double at, double gain)> TAILS = new()
	{
		["SONG_L17"] = (139.77, 0.978), ["SONG_L24"] = (170.7, 0.999), ["SONG_L25"] = (183.4, 1.004), ["SONG_L26"] = (127.19, 1.002),
		["SONG_L27"] = (210.18, 1.001), ["SONG_L29"] = (140.13, 0.995), ["SONG_L30"] = (179.53, 1.001),
		["SONG_F11"] = (153.51, 1.0), ["SONG_F12"] = (187.87, 1.031), ["SONG_B1"] = (148.48, 1.0),
	};
	// where each song's sound ends (s, -50 dB): many files end in a few seconds of silence; the win screen starts the song again
	// there, so it never sounds stopped (tools: a 50 ms loudness scan of every file)
	static readonly Dictionary<string, double> SOUND_END = new()
	{
		["SONG_B1"] = 152.0, ["SONG_B1_TAIL"] = 166.25, ["SONG_B2"] = 117.25, ["SONG_F1"] = 159.95, ["SONG_F10"] = 212.65, ["SONG_F11"] = 159.95, 
		["SONG_F11_TAIL"] = 99.85, ["SONG_F12"] = 196.05, ["SONG_F12_TAIL"] = 94.6, ["SONG_F2"] = 89.95, ["SONG_F3"] = 90.9, ["SONG_F4"] = 156.1, 
		["SONG_F5"] = 180.1, ["SONG_F6"] = 195.95, ["SONG_F7"] = 202.6, ["SONG_F8"] = 239.6, ["SONG_F9"] = 237.1, ["SONG_H1"] = 114.65, ["SONG_H2"] 
		= 135.5, ["SONG_H3"] = 115.1, ["SONG_L1"] = 142.95, ["SONG_L10"] = 214.45, ["SONG_L11"] = 241.95, ["SONG_L12"] = 124.4, ["SONG_L13"] = 
		157.45, ["SONG_L14"] = 100.55, ["SONG_L15"] = 183.55, ["SONG_L16"] = 244.35, ["SONG_L17"] = 144.9, ["SONG_L17_TAIL"] = 146.35, ["SONG_L18"] 
		= 219.85, ["SONG_L19"] = 308.35, ["SONG_L2"] = 104.35, ["SONG_L20"] = 257.9, ["SONG_L21"] = 255.55, ["SONG_L22"] = 150.6, ["SONG_L23"] = 
		165.2, ["SONG_L24"] = 173.55, ["SONG_L24_TAIL"] = 76.0, ["SONG_L25"] = 185.6, ["SONG_L25_TAIL"] = 185.25, ["SONG_L26"] = 139.95, 
		["SONG_L26_TAIL"] = 85.45, ["SONG_L27"] = 212.6, ["SONG_L27_TAIL"] = 28.85, ["SONG_L28"] = 153.65, ["SONG_L29"] = 142.75, ["SONG_L29_TAIL"] 
		= 120.55, ["SONG_L3"] = 149.6, ["SONG_L30"] = 181.0, ["SONG_L30_TAIL"] = 47.1, ["SONG_L4"] = 123.0, ["SONG_L5"] = 316.35, ["SONG_L6"] = 
		177.65, ["SONG_L7"] = 136.65, ["SONG_L8"] = 215.7, ["SONG_L9"] = 242.9
	};
	static double soundEnd(string file, double len) => SOUND_END.TryGetValue(file, out var e) ? Math.Min(len, e + 0.25) : len;
	static readonly Dictionary<string, SongTail> tailOf = new();   // level id -> its tail
	static readonly List<Voice> musMore = new();                   // the tail and the win screen's restarts: stopMusic stops them too
	static Voice tailFor, winFor;   // the song voice the tail is set for / the song voice the win screen loops after
	static double musEnd;           // audio time the song (with its tail) ends

	public static void loadTail(Level L, string file)
	{
		if (!TAILS.TryGetValue(file, out var T)) return;
		var b = LoadSong(file + "_TAIL"); if (b == null) return;
		tailOf[L.id] = new SongTail { at = T.at, gain = T.gain, len = soundEnd(file + "_TAIL", b.GetLength()), buf = b };
	}

	// the song voice 'v' (file time 0 at audio time t0) switches to the tail at the level end; returns when the tail ends (or -1)
	static double tailAt(Voice v, double t0)
	{
		if (cur == null || !tailOf.TryGetValue(cur.id, out var T)) return -1;
		double te = B(cur.end), at = t0 + te / spd;
		if (at < actx.currentTime + 0.01) { at = actx.currentTime + 0.01; te = (at - t0) * spd; }
		double off = Math.Max(0, te - T.at);
		v.gain.setTargetAtTime(0, at, 0.002); v.stop(at + 0.03);
		var w = new Voice { song = T.buf }; w.playbackRate.value = spd; w.gain.value = 0; w.gain.setTargetAtTime(T.gain, at, 0.002); w.start(at, off);
		musMore.Add(AudioCtx.Add(w));
		return at + (T.len - off) / spd;
	}
	// in the run (playStep): half a second before the level end, set the switch at its exact moment (not in practice: it stops at the end)
	static void songTailArm(double t)
	{
		if (src == null || src == tailFor || pr.on || cur == null || t < B(cur.end) - 0.5 || !tailOf.ContainsKey(cur.id)) return;
		tailFor = src; musEnd = tailAt(src, songStart);
	}
	// every frame of a win screen: the song plays on, and starts again from the start when it ends
	static void winMusic()
	{
		if (src == null) return;
		if (winFor != src) { winFor = src; if (tailFor != src) musEnd = songStart + curSongEnd() / spd; }
		if (actx.currentTime < musEnd - 0.3) return;
		double at = Math.Max(musEnd, actx.currentTime + 0.02);
		var v = new Voice { song = (AudioStream)cur.buf }; v.gain.value = 1; v.playbackRate.value = spd; v.start(at, 0);
		musMore.Add(AudioCtx.Add(v));
		double e = tailAt(v, at);
		musEnd = e > 0 ? e : at + curSongEnd() / spd;
	}
	static double curSongEnd() => soundEnd(SONG_FILE.TryGetValue(cur.id, out var f) ? f : "", ((AudioStream)cur.buf).GetLength());
	static void stopMore(double fade)
	{
		foreach (var v in musMore) { v.gain.setTargetAtTime(0, actx.currentTime, fade); v.stop(actx.currentTime + fade * 8); }
		musMore.Clear(); tailFor = winFor = null;
	}
}
