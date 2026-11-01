using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// Plays a level's bot run frame by frame at exactly 60 fps (no clock, no audio), the same way tools/compare/driver.js
// does in the browser, and writes a state log line per frame plus a PNG every `every` frames.
// Usage: -- --bot-run <levelId> <n|e> <outDir> [every=60] [from=0] [to=all]
// With --play-run instead: real play through step() with a fixed key script (tests movement, dash, hits, deaths, restarts).
public class BotRun
{
	public bool play;
	string id, m, outDir; int every = 60, from = 0, to = int.MaxValue, total;
	int i; double T0; bool done, pendingShot; int shotFrame; int scN = -1; string scSt;
	StringBuilder log = new();
	Main host;
	const uint SEED = 12345;

	public static BotRun FromArgs(string[] a, int k)
	{
		var r = new BotRun { id = a[k], m = a[k + 1], outDir = a[k + 2] };
		if (a.Length > k + 3 && int.TryParse(a[k + 3], out var e)) r.every = e;
		if (a.Length > k + 4 && int.TryParse(a[k + 4], out var f)) r.from = f;
		if (a.Length > k + 5 && int.TryParse(a[k + 5], out var t)) r.to = t;
		return r;
	}

	public void Start(Main h)
	{
		host = h;
		Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.noMus = true; G.padsDisabled = true; G.classicUi = true;
		performance.manual = 100000; AudioCtx.manualTime = 0;
		G.InitSound();
		G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		bool old = id.EndsWith("_v51"); string lid = old ? id[..^4] : id;   // <id>_v51: the old design of a level with new attacks (v52) and its <id><m>_v51 bot run
		var L = G.ALL_LV().Concat(G.HCLV).Concat(G.BOSSLV).First(x => x.id == lid); if (old) L = L.v51;
		if (System.Environment.GetEnvironmentVariable("SKIN") is string sk) { G.classicUi = false; G.skin = sk == "none" ? "" : sk; }   // SKIN=<look|none>: today's game with that look (previews)
		JS.SeedRandom(SEED);
		G.rpOverlayOff = true;
		if (System.Environment.GetEnvironmentVariable("PRT") is string prt)
		{
			// PRT=<second>: how long practice takes to get the level to that second (it runs the level from its start)
			G.easy = m == "e"; G.cur = L; G.levelTiming(L); G.pr.L = L; G.pr.t = double.Parse(prt); G.pr.doc = G.botDoc(L, m); G.pr.on = true; G.spd = 1;
			var sw = System.Diagnostics.Stopwatch.StartNew(); G.prBegin(); GD.Print($"practice to {prt}s: {sw.ElapsedMilliseconds} ms, songT={G.songT}");
			done = true; return;
		}
		if (play)
		{
			G.easy = m == "e"; G.applyTheme(G.easy); G.cur = L; G.fromList = "levels"; G.levelTiming(L); L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
			if (TS) G.testSeed = () => 1000 + G.deaths;   // FPS=<n>: same seeds at every frame rate (draw() uses Math.random)
			// WAV=<file>: the game's own sound to a file (with the song's tail, SongTail.cs; NOTAIL=1 = without it),
			// FROM=<beat> start there, INV=1 nothing hurts (it reaches the win screen), AFTER=<s> seconds of the win screen
			double fb = double.TryParse(System.Environment.GetEnvironmentVariable("FROM"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fbv) ? fbv : 0;
			if (WAV != null) { wav = new BinaryWriter(new MemoryStream()); AudioCtx.deviceRate = AudioServer.GetMixRate();   // the songs mix at the sound card's rate, like the game
				 if (System.Environment.GetEnvironmentVariable("NOTAIL") != "1") G.loadTail(L, G.SONG_FILE[L.id]); }
			G.requestStart(fb); G.beginRun(fb); G.last = performance.now();
			total = (int)Math.Ceiling((G.B(L.end) - (fb > 0 ? G.B(fb) : 0) + 2) * FPS);
			if (double.TryParse(System.Environment.GetEnvironmentVariable("AFTER"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var af)) total = (int)Math.Ceiling((G.B(L.end) - G.B(fb) + 0.6 + af) * FPS);
			Log(); return;
		}
		G.rp.doc = G.botDoc(L, m); G.rp.bot = true; G.rp.back = "levels"; G.rp.L = L; G.rp.m = m; G.rp.hide = true;
		// RPV=1|2: play the bot's run as an old replay of that version (old level shapes)
		if (int.TryParse(System.Environment.GetEnvironmentVariable("RPV"), out var rpv) && rpv > 0)
		{ G.rp.bot = false; G.rp.doc = new RpDoc { r = G.rp.doc.r, hz = G.rp.doc.hz, segs = G.rp.doc.segs, v = rpv }; }
		var rp4 = System.Environment.GetEnvironmentVariable("RP4");
		if (rp4 != null)
		{
			// a v4 replay recorded by --play-run, played back at FPS frames a second through updateReplay
			var segs = File.ReadAllLines(rp4).Where(l => l.Length > 0).Select(l => { var f = l.Split(' '); return new RpSeg { cp = double.Parse(f[0]), seed = int.Parse(f[1]), t0 = double.Parse(f[2]), n = int.Parse(f[3]), b = f[4].Split(',').Select(x => (ushort)int.Parse(x)).ToArray() }; }).ToList();
			G.rp.doc = new RpDoc { r = "C", hz = G.RP_HZ, v = G.RP_V, segs = segs }; G.rp.bot = false; rpLive = true;
			L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
		}
		G.rp.saveEasy = G.easy; G.easy = m == "e"; G.applyTheme(G.easy); G.cur = L; G.levelTiming(L); G.hitsTotal = 0; G.deaths = 0;
		G.rp.paused = false; G.rp.rate = 1; G.rp.barT = 4; G.rpTimeline(); G.rpGo(0);
		T0 = G.songT;
		Log();
	}

	void Log()
	{
		var p = G.player ?? new Player(); double sx = 0, sy = 0; foreach (var h in G.hazards) { sx += h.x; sy += h.y; }
		log.Append(i).Append(' ').Append(str(G.songT)).Append(' ').Append(str(p.x)).Append(' ').Append(str(p.y)).Append(' ').Append(str(p.hp))
			.Append(' ').Append(G.hazards.Count).Append(' ').Append(G.particles.Count).Append(' ').Append(G.rngS).Append(' ').Append(str(G.checkpoint))
			.Append(' ').Append(str(sx)).Append(' ').Append(str(sy)).Append(' ').Append(G.hitsTotal).Append(' ').Append(G.deaths).Append(' ').Append(G.state).Append('\n');
	}

	bool rpLive;
	// one 60 fps frame of the replay, like step() in state 'replay' with dt = 1/60
	bool Sim()
	{
		if (rpLive)
		{
			if (G.state == "rpEnd" || i + 1 > to) return false;
			i++; AudioCtx.manualTime += 1.0 / FPS; performance.manual += 1000.0 / FPS;
			if (G.state == "rpGap") G.rpSeg(G.rp.seg + 1); else G.updateReplay(1.0 / FPS);
			{ double fd = 1.0 / FPS; foreach (var q in G.particles) { q.x += q.vx * fd; q.y += q.vy * fd; double f = Math.Pow(0.04, fd); q.vx *= f; q.vy *= f; q.life -= fd; } G.particles = G.particles.Where(q => q.life > 0).ToList();
			  G.gloom += (G.gloomT - G.gloom) * Math.Min(1, fd * 3); G.shake = Math.Max(0, G.shake - fd * 50); G.flash = Math.Max(0, G.flash - fd * 2.2); G.cpMsg -= fd; }   // what step() does each frame
			Log(); return true;
		}
		const double dt = 1.0 / 60;
		var s = G.rp.doc.segs[G.rp.seg];
		double t = T0 + (i + 1) * dt;
		if ((t - s.t0) * G.rp.doc.hz >= s.n - 1 || i + 1 > to) return false;
		i++;
		G.rpApply(t, dt, true);
		foreach (var q in G.particles) { q.x += q.vx * dt; q.y += q.vy * dt; double f = Math.Pow(0.04, dt); q.vx *= f; q.vy *= f; q.life -= dt; }
		G.particles = G.particles.Where(q => q.life > 0).ToList();
		G.gloom += (G.gloomT - G.gloom) * Math.Min(1, dt * 3); G.shake = Math.Max(0, G.shake - dt * 50); G.flash = Math.Max(0, G.flash - dt * 2.2); G.cpMsg -= dt;
		performance.manual += 1000.0 / 60;
		Log();
		return true;
	}

	static readonly int[][] DIRS = { new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 }, new[] { -1, 1 }, new[] { -1, 0 }, new[] { -1, -1 }, new[] { 0, -1 }, new[] { 1, -1 } };
	// one 60 fps frame of real play: the key script, then step() (which also draws)
	static readonly int FPS = int.TryParse(System.Environment.GetEnvironmentVariable("FPS"), out var f) ? f : 60;
	static readonly bool TS = System.Environment.GetEnvironmentVariable("FPS") != null;
	static readonly int KU = int.TryParse(System.Environment.GetEnvironmentVariable("KU"), out var ku) ? ku : 12;   // key script unit: 1/KU s (must be on every rate's frames)
	int lastU = -1;
	bool PlayFrame()
	{
		if (i + 1 > total || i + 1 > to) return false;
		i++;
		AudioCtx.manualTime += 1.0 / FPS; performance.manual += 1000.0 / FPS;
		if (!TS)
		{
			var d = DIRS[(i / 40 * 5) % 8];
			G.keys["ArrowRight"] = d[0] > 0; G.keys["ArrowLeft"] = d[0] < 0; G.keys["ArrowDown"] = d[1] > 0; G.keys["ArrowUp"] = d[1] < 0;
			G.keys["KeyJ"] = i % 23 == 0; G.keys["Enter"] = i % 50 == 0;
		}
		else
		{
			// the key script in song time (1/KU s units since the run started), so it lands on the same steps at any frame rate
			double tg = G.state == "play" ? (AudioCtx.manualTime.Value - G.songStart) * G.spd - G.sim0 : -1;
			int u = tg >= 0 ? (int)Math.Floor(tg * KU + 1e-6) : -1; bool nu = u != lastU; lastU = u;
			var d = u < 1 ? new[] { 0, 0 } : DIRS[(u / 8 * 5) % 8];   // nothing held in the first 1/12 s: no frame has read the keys yet
			G.keys["ArrowRight"] = d[0] > 0; G.keys["ArrowLeft"] = d[0] < 0; G.keys["ArrowDown"] = d[1] > 0; G.keys["ArrowUp"] = d[1] < 0;
			G.keys["KeyJ"] = nu && u > 0 && u % 5 == 0; G.keys["Enter"] = G.state == "dead" && i % (FPS / 2) == 0;
		}
		if (INV && G.player != null) G.player.inv = 9;
		if (WAV != null && System.Environment.GetEnvironmentVariable("NOSFX") == "1") G.dry = true;   // the songs alone (the effects' noise is not seeded)
		if (winAudio < 0 && G.state is "win" or "lcBWin") winAudio = AudioCtx.manualTime.Value;
		G.gfx.BeginFrame();
		G.step(performance.manual.Value);
		if (wav != null)
		{
			long upto = (long)Math.Round(AudioCtx.manualTime.Value * AudioCtx.SR); int n = (int)(upto - wavN);
			if (n > 0) { var l = new float[n]; var r = new float[n]; AudioCtx.RenderAt(l, r, n, (double)wavN / AudioCtx.SR); wavN = upto; for (int j = 0; j < n; j++) { wav.Write((short)(Math.Clamp(l[j], -1, 1) * 32767)); wav.Write((short)(Math.Clamp(r[j], -1, 1) * 32767)); } }
		}
		Log();
		return true;
	}

	// PERF=<file>: no pictures; every frame is drawn and handed over, one per engine frame, and its cost is written:
	// frame songT simMs drawMs submitMs allocKB gen0 gen1 gen2 items
	static readonly string PERF = System.Environment.GetEnvironmentVariable("PERF");
	static readonly bool PERFSUB = System.Environment.GetEnvironmentVariable("PERFSUB") == "1";
	static readonly bool HASH = System.Environment.GetEnvironmentVariable("HASH") == "1";   // HASH=1: also a hash of all drawing handed to Godot per frame
	StringBuilder perf;
	void PerfFrame()
	{
		perf ??= new StringBuilder();
		long a0 = GC.GetAllocatedBytesForCurrentThread();
		int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
		var sw = System.Diagnostics.Stopwatch.StartNew();
		if (!Sim()) { done = true; File.WriteAllText(PERF, perf.ToString()); if (MG.Canvas.Geo.TriCheck) GD.Print($"tricheck {MG.Canvas.Geo.triChecked} diff {MG.Canvas.Geo.triDiff}"); return; }
		double tSim = sw.Elapsed.TotalMilliseconds;
		if (int.TryParse(System.Environment.GetEnvironmentVariable("PERFGC"), out var pg) && i % pg == 0) GC.Collect(System.Environment.GetEnvironmentVariable("PERFGCF") == "1" ? 2 : 0, GCCollectionMode.Forced, true, true);
		if (int.TryParse(System.Environment.GetEnvironmentVariable("PERFSLEEP"), out var psl) && i == psl) { GD.Print("SLEEPING"); System.Threading.Thread.Sleep(6000); }
		MG.Canvas.Surface.HashOn = HASH; MG.Canvas.Surface.FH = 0; MG.Canvas.Surface.SubN = 0; MG.Canvas.Surface.SubV = 0;
		G.gfx.BeginFrame();
		G.draw(performance.manual.Value);
		double tDraw = sw.Elapsed.TotalMilliseconds;
		G.gfx.EndFrame();
		double tEnd = sw.Elapsed.TotalMilliseconds;
		perf.Append(i).Append(' ').Append(str(G.songT)).Append(' ').Append(tSim.ToString("F3")).Append(' ').Append((tDraw - tSim).ToString("F3")).Append(' ')
			.Append((tEnd - tDraw).ToString("F3")).Append(' ').Append((GC.GetAllocatedBytesForCurrentThread() - a0) / 1024).Append(' ')
			.Append(GC.CollectionCount(0) - g0).Append(' ').Append(GC.CollectionCount(1) - g1).Append(' ').Append(GC.CollectionCount(2) - g2);
		if (HASH) perf.Append(" h ").Append(MG.Canvas.Surface.FH.ToString("x16"));
		if (PERFSUB) perf.Append(" sub ").Append(MG.Canvas.Surface.SubN).Append(" v ").Append(MG.Canvas.Surface.SubV).Append(" items ").Append(MG.Canvas.Surface.ItemN);
		if (GC.CollectionCount(0) != g0)
		{
			var gi = GC.GetGCMemoryInfo(GCKind.Any);
			perf.Append(" gc gen").Append(gi.Generation).Append(" pause ").Append(gi.PauseDurations[0].TotalMilliseconds.ToString("F1")).Append("ms promotedKB ").Append(gi.PromotedBytes / 1024)
				.Append(" heapMB ").Append(gi.HeapSizeBytes >> 20).Append(" concurrent ").Append(gi.Concurrent).Append(" compacted ").Append(gi.Compacted).Append(" pinned ").Append(gi.PinnedObjectsCount).Append(" finPending ").Append(gi.FinalizationPendingCount).Append(" fragKB ").Append(gi.FragmentedBytes / 1024);
		}
		perf.Append('\n');
	}

	static readonly string WAV = System.Environment.GetEnvironmentVariable("WAV");
	static readonly bool INV = System.Environment.GetEnvironmentVariable("INV") == "1";
	BinaryWriter wav; long wavN;
	void WriteWav()
	{
		if (wav == null) return;
		var pcm = ((MemoryStream)wav.BaseStream).ToArray(); using var f = new BinaryWriter(File.Create(WAV));
		f.Write(Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + pcm.Length); f.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); f.Write(16); f.Write((short)1); f.Write((short)2);
		f.Write(AudioCtx.SR); f.Write(AudioCtx.SR * 4); f.Write((short)4); f.Write((short)16); f.Write(Encoding.ASCII.GetBytes("data")); f.Write(pcm.Length); f.Write(pcm);
		File.WriteAllText(WAV + ".txt", $"songStart {G.songStart.ToString(System.Globalization.CultureInfo.InvariantCulture)} end {G.B(G.cur.end).ToString(System.Globalization.CultureInfo.InvariantCulture)} winAtAudio {winAudio.ToString(System.Globalization.CultureInfo.InvariantCulture)} state {G.state}\n");
	}
	double winAudio = -1;
	public void Process()
	{
		if (PERF != null && !play)
		{
			if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); GD.Print("perf done frames=", i); host.GetTree().Quit(); return; }
			PerfFrame(); return;
		}
		if (play && !done)
		{
			if (pendingShot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{shotFrame:D6}.png")); pendingShot = false; }
			while (true)
			{
				if (!PlayFrame()) { done = true; G.gfx.EndFrame(); WriteWav(); return; }
				if (i >= from && i % every == 0) { G.gfx.EndFrame(); pendingShot = true; shotFrame = i; return; }
			}
		}
		if (pendingShot)
		{
			G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"{(scN >= 0 ? "s" : "f")}{shotFrame:D6}.png"));
			pendingShot = false;
		}
		// SCENE=1 (a boss level): its scene first, frames s<n>.png at 60 a second
		if (scN >= 0 || (scN == -1 && System.Environment.GetEnvironmentVariable("SCENE") == "1" && G.cur.boss))
		{
			if (scN < 0) { scSt = G.state; scN = 0; }
			while (scN <= (int)(G.bossSceneLen * 60))
			{
				int n = scN++; var st = G.state; G.state = "levelIntro"; G.introAt = 0; G.gfx.BeginFrame(); G.draw(n * 1000.0 / 60); G.state = st;
				if (n % every == 0) { G.gfx.EndFrame(); pendingShot = true; shotFrame = n; return; }
			}
			scN = -2; G.state = scSt;
		}
		if (done && play && G.rec != null)
		{
			// the run's recording, so RP4=<file> can play it back as a v4 replay
			var sb = new StringBuilder(); foreach (var g in G.rec.segs) sb.Append(str(g.cp)).Append(' ').Append(g.seed).Append(' ').Append(str(g.t0 ?? 0)).Append(' ').Append(g.n).Append(' ').Append(string.Join(",", g.b)).Append('\n');
			File.WriteAllText(Path.Combine(outDir, "rec.txt"), sb.ToString());
		}
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); GD.Print("bot-run done frames=", i); host.GetTree().Quit(); return; }
		// run frames until the next one to capture; every frame is drawn (draw() uses Math.random), only the last is shown
		while (true)
		{
			if (!Sim()) { done = true; break; }
			G.gfx.BeginFrame();
			G.draw(performance.manual.Value);
			if (i >= from && i % every == 0) { G.gfx.EndFrame(); pendingShot = true; shotFrame = i; return; }
		}
		G.gfx.EndFrame();
	}
}
