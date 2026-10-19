using Godot;
using System;
using static MG.JS;

namespace MG;

// the browser's AudioContext as the game code sees it
public static class actx
{
	public static double currentTime => AudioCtx.currentTime;
	public static string state => AudioCtx.state;
	public static double sampleRate => AudioCtx.sampleRate;
	public static void resume() => AudioCtx.resume();
	public static void suspend() => AudioCtx.suspend();
}

// original index.html lines 111-148 (sound)
public static partial class G
{
	public static double vol = 0.8;
	static Pcm noiseBuf;
	public static AudioStream menuBuf = null;
	public static string menuKey = "";
	public static Voice menuSrc = null, src = null;
	public static double menuStart = 0, songStart = 0;
	public static double spd = 1;
	public static bool noMus = false;

	public static void InitSound()
	{
		double v = parseFloat(localStorage.getItem("mg_volume")); if (!double.IsNaN(v)) vol = clamp(v, 0, 1);
		AudioCtx.master.value = 0.9 * vol;
		int n = (int)actx.sampleRate;
		noiseBuf = new Pcm { mono = new float[n], frames = n, rate = n };
		for (int i = 0; i < n; i++) noiseBuf.mono[i] = (float)(random() * 2 - 1);
	}

	public static void setVol(double v)
	{
		double q = classicUi ? 10 : 100; vol = clamp(round(v * q) / q, 0, 1); AudioCtx.master.setTargetAtTime(0.9 * vol, actx.currentTime, 0.03);
		localStorage.setItem("mg_volume", str(vol));
	}

	public static void playMenu(double from = 0)
	{
		if (menuSrc != null || menuBuf == null) return;
		var v = new Voice { song = menuBuf, loop = true, loopStart = MENU_DROP, loopEnd = Math.Min(MENU_LOOP, menuBuf.GetLength()) };
		if (from > 0) { v.gain.setValueAtTime(0.0001, actx.currentTime); v.gain.exponentialRampToValueAtTime(0.85, actx.currentTime + 0.4); }
		else v.gain.value = 0.85;
		double w = actx.currentTime + 0.05; v.start(w, from); menuStart = w - from;
		menuSrc = AudioCtx.Add(v);
	}
	public static void stopMenu()
	{
		if (menuSrc == null) return; var s = menuSrc;
		s.gain.setTargetAtTime(0, actx.currentTime, 0.15); s.stop(actx.currentTime + 1);
		menuSrc = null;
	}

	// level preview in the level list
	public class PvState { public Voice src; public System.Collections.Generic.List<Voice> more = new(); public string sel = ""; public double at; public bool had; }
	public static PvState pv = new();
	public static void pvStop()
	{
		if (pv.src == null) return;
		foreach (var s in System.Linq.Enumerable.Prepend(pv.more, pv.src)) { s.gain.setTargetAtTime(0, actx.currentTime, 0.12); s.stop(actx.currentTime + 0.8); }
		pv.src = null; pv.more.Clear();
	}
	public static void pvStart(Level L)
	{
		var sb = BEATS; double so = OFF, ss = SPB; levelTiming(L); int bi = 0; double mx = -1; var E = L.energy ?? Array.Empty<double>();
		for (int i = 0; i < E.Length; i++) if (E[i] > mx + 0.001) { mx = E[i]; bi = i; }
		var buf = (AudioStream)L.buf; double dur = buf.GetLength();
		double st = Math.Max(0, B(bi * 4)); BEATS = sb; OFF = so; SPB = ss; if (st > dur - 20) st = Math.Max(0, dur * 0.35);
		var v = new Voice { song = buf, loop = true, loopStart = st, loopEnd = Math.Min(dur, st + 30) };
		v.gain.setValueAtTime(0.0001, actx.currentTime); v.gain.exponentialRampToValueAtTime(0.85, actx.currentTime + 0.6);
		v.start(actx.currentTime + 0.05, st);
		pv.src = AudioCtx.Add(v);
	}

	// a platformer level: all its parts together, the whole loop
	public static void pvStartPlat(PlatLevel L)
	{
		for (int i = 0; i < L.bufs.Length; i++)
		{
			var v = new Voice { song = L.bufs[i], loop = true, loopStart = L.loop[0], loopEnd = L.loop[1] };
			v.gain.setValueAtTime(0.0001, actx.currentTime); v.gain.exponentialRampToValueAtTime(0.85, actx.currentTime + 0.6);
			v.start(actx.currentTime + 0.05, L.loop[0]);
			if (i == 0) pv.src = AudioCtx.Add(v); else pv.more.Add(AudioCtx.Add(v));
		}
	}

	public static void playMusic(double fromT)
	{
		stopMusic(); if (noMus) return;
		var v = new Voice { song = (AudioStream)cur.buf };
		v.gain.value = 1;
		double when = actx.currentTime + 0.12; v.playbackRate.value = spd; v.start(when, Math.Max(0, fromT)); songStart = when - fromT / spd;
		src = AudioCtx.Add(v);
	}
	public static void stopMusic()
	{
		if (src == null) return; var s = src;
		s.gain.setTargetAtTime(0, actx.currentTime, 0.06); s.stop(actx.currentTime + 0.5);
		src = null;
	}

	public static void sNoise(double t, double dur, string type, double freq, double vol, double q = 1)
	{
		var v = new Voice { buf = noiseBuf, ftype = type, ffreq = freq, fq = q };
		v.gain.setValueAtTime(vol, t); v.gain.exponentialRampToValueAtTime(0.001, t + dur);
		v.startAt = t; v.offset = 0; v.stopAt = t + dur + 0.05;
		AudioCtx.Add(v);
	}
	public static void sTone(double t, double freq, double dur, string type, double vol)
	{
		var v = new Voice { osc = type, freq = new AParam(freq) };
		v.gain.setValueAtTime(0.0001, t); v.gain.exponentialRampToValueAtTime(vol, t + 0.01); v.gain.exponentialRampToValueAtTime(0.0001, t + dur);
		v.startAt = t; v.stopAt = t + dur + 0.02;
		AudioCtx.Add(v);
	}
	public static void sfx(Action<double> fn) { if (actx.state == "running" && !dry) fn(actx.currentTime); }
}
