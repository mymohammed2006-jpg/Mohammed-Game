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
	int i; double T0; bool done, pendingShot; int shotFrame;
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
		var L = G.ALL_LV().First(x => x.id == id);
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
			G.requestStart(0); G.beginRun(0); G.last = performance.now();
			total = (int)Math.Ceiling((G.B(L.end) + 2) * FPS);
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
		G.gfx.BeginFrame();
		G.step(performance.manual.Value);
		Log();
		return true;
	}

	public void Process()
	{
		if (play && !done)
		{
			if (pendingShot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{shotFrame:D6}.png")); pendingShot = false; }
			while (true)
			{
				if (!PlayFrame()) { done = true; G.gfx.EndFrame(); return; }
				if (i >= from && i % every == 0) { G.gfx.EndFrame(); pendingShot = true; shotFrame = i; return; }
			}
		}
		if (pendingShot)
		{
			G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{shotFrame:D6}.png"));
			pendingShot = false;
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
