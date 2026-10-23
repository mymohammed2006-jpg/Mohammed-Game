using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MG;

// A small copy of the browser's Web Audio: an audio clock (currentTime), song buffers played from a start
// time with loops and playback rate, oscillators, filtered noise, gains with the same automation curves.
// One generator stream is fed from a mixing thread.

// a short decoded buffer (the noise buffer), one channel
public sealed class Pcm
{
	public float[] mono;
	public int frames;
	public int rate = 44100;
	public double duration => (double)frames / rate;
}

// AudioParam with the automation the game uses
public sealed class AParam
{
	struct Evt { public int kind; public double v, t, tau; }   // 0 set, 1 exp ramp, 2 target
	readonly List<Evt> ev = new();
	double baseV;
	public AParam(double v) { baseV = v; }
	public double value
	{
		get => baseV;
		set { lock (AudioCtx.L) { ev.Clear(); baseV = value; } }
	}
	public void setValueAtTime(double v, double t) { lock (AudioCtx.L) Add(new Evt { kind = 0, v = v, t = t }); }
	public void exponentialRampToValueAtTime(double v, double t) { lock (AudioCtx.L) Add(new Evt { kind = 1, v = v, t = t }); }
	public void setTargetAtTime(double v, double t, double tau) { lock (AudioCtx.L) Add(new Evt { kind = 2, v = v, t = t, tau = tau }); }
	void Add(Evt e)
	{
		int i = ev.Count;
		while (i > 0 && ev[i - 1].t > e.t) i--;
		ev.Insert(i, e);
	}
	// value at time t (spec rules for setValue / exponential ramp / setTarget)
	public double At(double t)
	{
		if (ev.Count == 0) return baseV;
		double v = baseV, vt = 0;  // value and time of the last finished event
		for (int i = 0; i < ev.Count; i++)
		{
			var e = ev[i];
			if (e.kind == 1)
			{
				// ramp from the previous event's value/time to e.v at e.t
				if (t < e.t)
				{
					if (v <= 0 || e.v <= 0 || v * e.v < 0) return v;
					double k = (t - vt) / (e.t - vt);
					if (e.t <= vt) return e.v;
					return v * Math.Pow(e.v / v, Math.Max(0, k));
				}
				v = e.v; vt = e.t; continue;
			}
			if (t < e.t) return v;
			if (e.kind == 0) { v = e.v; vt = e.t; continue; }
			// setTarget: runs until the next event starts
			double tEnd = i + 1 < ev.Count ? ev[i + 1].t : double.PositiveInfinity;
			double tt = Math.Min(t, tEnd);
			double v0 = v;
			v = e.v + (v0 - e.v) * Math.Exp(-(tt - e.t) / e.tau);
			vt = tt;
			if (t < tEnd) return v;
		}
		return v;
	}
	// drop events that are long done (keeps evaluation cheap)
	public void Trim(double now)
	{
		while (ev.Count > 1 && ev[1].t < now - 1 && ev[0].kind != 2 && ev[1].kind == 0) { baseV = ev[1].v; ev.RemoveAt(0); }
	}
}

public sealed class Voice
{
	public Pcm buf;                 // noise buffer source
	public AudioStream song;        // song source (decoded on the fly by Godot, same samples as the browser)
	AudioStreamPlayback pb;
	double songLen;
	public string osc;              // oscillator type
	public AParam freq;
	public bool loop;
	public double loopStart, loopEnd;
	public AParam playbackRate = new AParam(1);
	public double startAt = double.PositiveInfinity, offset, stopAt = double.PositiveInfinity;
	public AParam gain = new AParam(1);
	public AParam murk;             // a song heard through a broken radio (the radio level): 0 clear .. 1 all garbled; null = untouched
	double[] mz = new double[16]; double mhL; int mhN; long mFrame;
	// biquad
	public string ftype;
	public double ffreq, fq = 1;
	double b0, b1, b2, a1, a2, x1L, x2L, y1L, y2L;
	bool fReady;
	// state
	internal double pos = double.NaN;   // playhead (seconds into the buffer) / oscillator phase
	internal bool done;

	public void stop(double when = 0) { lock (AudioCtx.L) stopAt = Math.Min(stopAt, when <= 0 ? AudioCtx.Now : when); }
	public void start(double when, double off = 0) { lock (AudioCtx.L) { startAt = when; offset = off; } }

	void Coeffs(double sr)
	{
		double w0 = 2 * Math.PI * Math.Clamp(ffreq, 0, sr / 2) / sr, cw = Math.Cos(w0), sw = Math.Sin(w0);
		double A0;
		if (ftype == "bandpass")
		{
			double al = sw / (2 * fq);
			b0 = al; b1 = 0; b2 = -al; A0 = 1 + al; a1 = -2 * cw; a2 = 1 - al;
		}
		else
		{
			// lowpass / highpass: Q is in dB in Web Audio
			double al = sw / (2 * Math.Pow(10, fq / 20));
			if (ftype == "highpass") { b0 = (1 + cw) / 2; b1 = -(1 + cw); b2 = (1 + cw) / 2; }
			else { b0 = (1 - cw) / 2; b1 = 1 - cw; b2 = (1 - cw) / 2; }
			A0 = 1 + al; a1 = -2 * cw; a2 = 1 - al;
		}
		b0 /= A0; b1 /= A0; b2 /= A0; a1 /= A0; a2 /= A0;
		fReady = true;
	}

	// adds this voice into out (stereo) for frames starting at audio time t0
	internal void Render(float[] outL, float[] outR, int n, double t0, double sr)
	{
		for (int i = 0; i < n; i++)
		{
			double t = t0 + i / sr;
			if (t < startAt) continue;
			if (t >= stopAt) { done = true; return; }
			double sL, sR;
			if (song != null)
			{
				// songs are mixed in blocks (see RenderSong); here only one sample at a time is not needed
				RenderSong(outL, outR, i, n, t0, sr);
				return;
			}
			if (buf != null)
			{
				if (double.IsNaN(pos)) pos = offset;
				double rate = playbackRate.At(t);
				if (pos >= buf.duration || pos < 0) { done = true; return; }
				double fp = pos * buf.rate; int fi = (int)fp; double k = fp - fi;
				int j = Math.Min(fi, buf.frames - 1), j2 = Math.Min(fi + 1, buf.frames - 1);
				sL = sR = buf.mono[j] + (buf.mono[j2] - buf.mono[j]) * k;
				pos += rate / sr;
			}
			else
			{
				if (double.IsNaN(pos)) pos = 0;
				double f = freq.At(t), ph = pos - Math.Floor(pos), dp = f / sr;
				double s = osc switch
				{
					"square" => (ph < 0.5 ? 1 : -1) + Blep(ph, dp) - Blep((ph + 0.5) % 1, dp),
					"sawtooth" => 2 * ph - 1 - Blep(ph, dp),
					"triangle" => 1 - 4 * Math.Abs(ph - 0.5),
					_ => Math.Sin(2 * Math.PI * ph),
				};
				sL = sR = s;
				pos += dp;
			}
			if (ftype != null)
			{
				if (!fReady) Coeffs(sr);
				double x = sL;
				double y = b0 * x + b1 * x1L + b2 * x2L - a1 * y1L - a2 * y2L;
				x2L = x1L; x1L = x; y2L = y1L; y1L = y;
				sL = sR = y;
			}
			double gn = gain.At(t);
			outL[i] += (float)(sL * gn);
			outR[i] += (float)(sR * gn);
		}
	}
	// song frames from index i0 of this block on: Godot decodes the MP3 (bit-identical to the browser's decoder)
	void RenderSong(float[] outL, float[] outR, int i0, int n, double t0, double sr)
	{
		if (pb == null)
		{
			pb = song.InstantiatePlayback();
			songLen = song.GetLength();
			pos = offset;
			pb.Start(Math.Max(0, offset));
		}
		int i = i0;
		while (i < n)
		{
			double t = t0 + i / sr;
			if (t >= stopAt) { done = true; return; }
			double rate = playbackRate.At(t);
			double ls = loopStart, le = loopEnd > 0 && loopEnd > loopStart ? Math.Min(loopEnd, songLen) : songLen;
			if (loop && le <= ls) { ls = 0; le = songLen; }
			int k = n - i;
			// stop the block at the stop time
			if (stopAt < t0 + n / sr) k = Math.Min(k, Math.Max(1, (int)Math.Ceiling((stopAt - t) * sr)));
			bool wrap = false;
			if (loop)
			{
				int toEnd = (int)Math.Ceiling((le - pos) * sr / Math.Max(rate, 1e-6));
				if (toEnd <= k) { k = Math.Max(0, toEnd); wrap = true; }
			}
			if (k > 0)
			{
				// Godot mixes the song at the sound card's rate (often 48000), not ours: scale so k frames cover k/sr seconds
				var a = pb.MixAudio((float)(rate * AudioCtx.deviceRate / sr), k);
				if (AudioCtx.check && (int)(t0 * 2) != (int)((t0 - n / sr) * 2)) GD.PrintErr("MG_AUDIO ", pos.ToString("F3", System.Globalization.CultureInfo.InvariantCulture), " ", pb.GetPlaybackPosition().ToString("F3", System.Globalization.CultureInfo.InvariantCulture), " ", AudioCtx.deviceRate);
				double gn = gain.At(t);
				if (murk != null) Murk(a, t, sr);
				for (int j = 0; j < a.Length && i + j < n; j++)
				{
					double g2 = j == 0 ? gn : gain.At(t + j / sr);
					outL[i + j] += (float)(a[j].X * g2);
					outR[i + j] += (float)(a[j].Y * g2);
				}
				pos += rate * k / sr;
				i += k;
			}
			if (wrap)
			{
				pos = ls + Math.Max(0, pos - le);
				pb.Seek(pos);
			}
			else if (!loop && (pos >= songLen || !pb.IsPlaying())) { done = true; return; }
		}
	}

	// the broken radio: the song squeezed into a narrow band (the lows and the highs cut away, a band that opens as it clears,
	// made up in loudness so it stays as loud), the samples held in steps (a crunchy, digital sound), the signal fading in and out
	// and cutting off for moments. At 0 the song is left as it is.
	void Murk(Vector2[] a, double t, double sr)
	{
		double m = Math.Clamp(murk.At(t), 0, 1); if (m <= 0.001) { mFrame += a.Length; return; }
		var lo = Biquad(false, 20000 * Math.Pow(1100.0 / 20000, m), sr); var hi = Biquad(true, 20 * Math.Pow(500.0 / 20, m), sr);
		double make = Math.Pow(10, 9.6 * Math.Pow(m, 5) / 20); int hold = 1 + (int)Math.Floor(m * 5.5);
		for (int j = 0; j < a.Length; j++, mFrame++)
		{
			double x = (a[j].X + a[j].Y) * 0.5;   // a radio is mono
			for (int k = 0; k < 4; k++)   // two low passes, then two high passes (12 dB an octave each)
			{
				var c = k < 2 ? lo : hi; int o = k * 4;
				double y = c[0] * x + c[1] * mz[o] + c[2] * mz[o + 1] - c[3] * mz[o + 2] - c[4] * mz[o + 3]; mz[o + 1] = mz[o]; mz[o] = x; mz[o + 3] = mz[o + 2]; mz[o + 2] = y; x = y;
			}
			if (mhN <= 0) { mhL = x; mhN = hold; } mhN--; x = mhL;   // held after the band, so the steps crunch
			double ts = mFrame / sr, h = Hash(Math.Floor(ts / 0.11)), fade = make * (1 - 0.45 * m * (0.5 + 0.5 * Math.Sin(ts * 2 * Math.PI * 0.37 + Math.Sin(ts * 1.3))));
			if (h < 0.3 * m) fade *= 1 - 0.92 * m;   // a dropout
			a[j] = new Vector2((float)(x * fade), (float)(x * fade));
		}
	}
	static double[] Biquad(bool high, double fc, double sr)
	{
		double w0 = 2 * Math.PI * Math.Min(fc, sr * 0.45) / sr, cw = Math.Cos(w0), al = Math.Sin(w0) / (2 * 0.7071), A0 = 1 + al;
		double b0 = (high ? (1 + cw) / 2 : (1 - cw) / 2) / A0, b1 = (high ? -(1 + cw) : 1 - cw) / A0;
		return new[] { b0, b1, b0, -2 * cw / A0, (1 - al) / A0 };
	}
	static double Hash(double x) { double s = Math.Sin(x * 12.9898 + 78.233) * 43758.5453; return s - Math.Floor(s); }

	static double Blep(double t, double dt)
	{
		if (dt <= 0) return 0;
		if (t < dt) { t /= dt; return t + t - t * t - 1; }
		if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
		return 0;
	}
}

public static class AudioCtx
{
	public static readonly object L = new();
	public const int SR = 44100;
	static readonly List<Voice> voices = new();
	public static AParam master = new AParam(0.9 * 0.8);
	static long rendered;              // frames the mixer has made (audio clock)
	static bool running;
	static Thread thread;
	static AudioStreamGeneratorPlayback playback;
	static AudioStreamPlayer player;
	static volatile bool quit;
	public static double? manualTime;  // tests: the clock is set by hand
	public static volatile float deviceRate = 44100;   // the rate Godot's own mixer runs at (the sound card's)
	public static readonly bool check = System.Environment.GetEnvironmentVariable("MG_AUDIO_CHECK") == "1";   // tools/compare/audio.sh

	// estimate of the audio clock seen by the game (smooth, follows the mixer)
	static double est;
	static double lastWall = -1;

	public static double Now => (double)Interlocked.Read(ref rendered) / SR;
	public static double currentTime => manualTime ?? est;
	public static string state => running ? "running" : "suspended";
	public static double sampleRate => SR;

	public static void Init(Node host)
	{
		deviceRate = AudioServer.GetMixRate();
		var gen = new AudioStreamGenerator { MixRate = SR, BufferLength = 0.1f };
		player = new AudioStreamPlayer { Stream = gen, Bus = "Master" };
		host.AddChild(player);
		player.Play();
		playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback();
		thread = new Thread(Mix) { IsBackground = true, Name = "mixer", Priority = ThreadPriority.AboveNormal };
		thread.Start();
	}
	public static void Shutdown() { quit = true; thread?.Join(200); }

	public static void resume() { running = true; }
	public static void suspend() { running = false; }

	public static Voice Add(Voice v) { lock (L) voices.Add(v); return v; }
	// tests: how many songs (not effects) are playing at time t (the checks for a song left playing under another)
	public static int SongsAt(double t) { lock (L) { int n = 0; foreach (var v in voices) if (v.song != null && v.startAt <= t && v.stopAt > t) n++; return n; } }

	// tests: mix every voice for [t0, t0 + n/SR) into l/r (for videos with the game's own sound)
	public static void RenderAt(float[] l, float[] r, int n, double t0)
	{
		lock (L)
		{
			foreach (var v in voices) v.Render(l, r, n, t0, SR);
			for (int i = 0; i < n; i++) { double m = master.At(t0 + (double)i / SR); l[i] = (float)(l[i] * m); r[i] = (float)(r[i] * m); }
		}
	}

	// called once per game frame: moves the smooth clock toward the mixer clock
	public static void Tick()
	{
		deviceRate = AudioServer.GetMixRate();
		double wall = performance.now() / 1000;
		if (lastWall < 0) lastWall = wall;
		double dw = wall - lastWall; lastWall = wall;
		if (!running) return;
		// what has actually reached Godot's mixer: made minus still waiting in the generator buffer
		int queued = playback != null ? Math.Max(0, (int)(SR * 0.1) - playback.GetFramesAvailable()) : 0;
		double target = (double)(Interlocked.Read(ref rendered) - queued) / SR;
		est += dw;
		double err = target - est;
		if (Math.Abs(err) > 0.06) est = target;
		else est += err * Math.Min(1, dw * 4);
	}

	static void Mix()
	{
		const int Q = 128;
		var l = new float[Q]; var r = new float[Q]; var outBuf = new Vector2[Q];
		const int target = SR * 30 / 1000;   // keep about 30 ms ready
		while (!quit)
		{
			int avail = playback?.GetFramesAvailable() ?? 0;
			int queued = (int)(SR * 0.1) - avail;
			if (queued >= target || avail < Q) { Thread.Sleep(1); continue; }
			Array.Clear(l); Array.Clear(r);
			if (running)
			{
				lock (L)
				{
					double t0 = (double)rendered / SR;
					for (int i = voices.Count - 1; i >= 0; i--)
					{
						var v = voices[i];
						v.Render(l, r, Q, t0, SR);
						if (v.done) voices.RemoveAt(i);
					}
					for (int i = 0; i < Q; i++)
					{
						double m = master.At(t0 + (double)i / SR);
						l[i] = (float)(l[i] * m); r[i] = (float)(r[i] * m);
					}
					master.Trim(t0);
					rendered += Q;
				}
			}
			for (int i = 0; i < Q; i++) outBuf[i] = new Vector2(Math.Clamp(l[i], -1, 1), Math.Clamp(r[i], -1, 1));
			playback.PushBuffer(outBuf);
		}
	}
}
