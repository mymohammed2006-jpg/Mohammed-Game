using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// pause "back to the last save" counts a death; "from the start" really starts over (S possible), at 240 Hz; also the platformer and the replay count.
// Usage: -- --restart-test <outFile> <dodge ids,...>   (NOEND=1 skips the full clean run)
public class RestartTest
{
	string outFile; string[] ids; Main host; StringBuilder log = new(); int fails;
	public static RestartTest FromArgs(string[] a, int k) => new RestartTest { outFile = a[k], ids = a[k + 1].Split(',') };

	void Ok(bool c, string what) { log.Append(c ? "OK   " : "FAIL ").Append(what).Append('\n'); if (!c) fails++; }
	int frames;
	void Frame(string key = null, bool god = true)
	{
		foreach (var k in new[] { "Escape", "Enter", "ArrowDown", "ArrowUp", "KeyR" }) G.keys[k] = false;
		if (key != null) G.keys[key] = true;
		if (god && G.player != null && G.state == "play") G.player.inv = 1e9;
		AudioCtx.manualTime += 1.0 / 240; performance.manual += 1000.0 / 240; frames++;   // 240 Hz like his PC
		G.gfx.BeginFrame(); G.step(performance.manual.Value);
	}
	void Press(string key) { Frame(key); Frame(); Frame(); }
	bool Until(Func<bool> c, int max) { for (int j = 0; j < max; j++) { if (c()) return true; Frame(); } return c(); }

	public void Start(Main h)
	{
		host = h; localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false;
		performance.manual = 100000; AudioCtx.manualTime = 0; AudioCtx.resume(); JS.SeedRandom(5);
		G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0; G.gatePassed = true;
	}

	public void Process()
	{
		foreach (var id in ids) try { Dodge(id); } catch (Exception e) { Ok(false, id + " threw " + e); }
		try { Plat(); } catch (Exception e) { Ok(false, "plat threw " + e); }
		log.Append(fails == 0 ? "ALL OK\n" : $"{fails} FAILED\n");
		File.WriteAllText(outFile, log.ToString()); host.GetTree().Quit();
	}

	void Pause(int sel) { Press("Escape"); Ok(G.state == "paused", $"  paused (state {G.state})"); for (int j = 0; j < sel; j++) Press("ArrowDown"); Ok(G.pauseSel == sel, $"  pause item {sel}"); Press("Enter"); log.Append($"     after Enter: state {G.state} deaths {G.deaths} hits {G.hitsTotal} cp {G.checkpoint}\n"); }

	void Dodge(string id)
	{
		var L = G.ALL_LV().Concat(G.HCLV).First(x => x.id == id); log.Append($"== {id} {L.name}\n");
		G.easy = false; G.cur = L; G.fromList = "levels"; G.levelTiming(L); L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
		G.requestStart(0); Ok(Until(() => G.state == "play", 2000), "starts");
		// 1) no save yet: back to the last save = a fresh start, no death
		Until(() => G.songT > 3, 4000); G.hitsTotal = 2;
		Pause(1); Ok(G.deaths == 0 && G.hitsTotal == 0 && G.checkpoint == 0, $"no save yet -> fresh start (deaths {G.deaths} hits {G.hitsTotal})");
		Ok(Until(() => G.state == "play", 2000), "  playing again");
		// 2) after a save, hit 4 times then back to the last save: one death, hits kept, full hearts
		Ok(Until(() => G.checkpoint > 0, 240 * 200), $"reached a save (beat {G.checkpoint})"); double cp = G.checkpoint;
		Until(() => G.songT > G.B(cp) + 1, 2000); G.player.hp = 1; G.hitsTotal = 4;
		Pause(1); Ok(G.deaths == 1 && G.hitsTotal == 4 && G.checkpoint == cp, $"back to the save = death (deaths {G.deaths} hits {G.hitsTotal})");
		Ok(Until(() => G.state == "play", 2000) && G.player.hp == G.maxHp(G.cur) && G.songT < G.B(cp), $"  back at the save with 5 hearts (songT {G.songT:F2})");
		Until(() => G.songT > G.B(cp) + 1, 2000); Pause(1); Ok(G.deaths == 2, $"again = 2 deaths ({G.deaths})");
		Until(() => G.state == "play", 2000); Until(() => G.songT > G.B(cp) + 1, 2000);
		// the replay of that run counts them too (new replays); an old (v4) one stays as recorded
		var segs = G.rec.segs.Where(s => s.n > 0).Select(s => new RpSeg { cp = s.cp, seed = s.seed, t0 = s.t0 ?? 0, n = s.n, b = s.b.Select(x => (ushort)x).ToArray() }).ToList();
		Ok(segs.Count == 3, $"recording has 3 parts ({segs.Count})");
		foreach (int v in new[] { G.RP_V, 4 }) Ok(ReplayDeaths(L, segs, v) == (v >= 5 ? 2 : 0), $"replay v{v} deaths = {(v >= 5 ? 2 : 0)} ({rdeaths})");
		// 3) from the start: everything reset, then a clean run = S
		G.cur = L; G.easy = false; G.levelTiming(L); G.requestStart(G.checkpoint); Until(() => G.state == "play", 2000);
		G.deaths = 2; G.hitsTotal = 7;
		Pause(2); Ok(G.deaths == 0 && G.hitsTotal == 0 && G.checkpoint == 0 && G.rec.segs.Count <= 1, $"from the start resets (deaths {G.deaths} hits {G.hitsTotal} cp {G.checkpoint})");
		Ok(Until(() => G.state == "play", 2000) && G.songT < 0.5, $"  starts at the song's start (songT {G.songT:F2})");
		if (System.Environment.GetEnvironmentVariable("NOEND") == null) { Ok(Until(() => G.state == "win", 240 * 400), "  reaches the end");
		Ok(G.lastRank == "S", $"  clean run after from-the-start = {G.lastRank}"); }
		// 4) death screen: continue from save stays one death (unchanged)
		G.requestStart(0); Until(() => G.state == "play", 2000); Until(() => G.checkpoint > 0, 240 * 200); Until(() => G.songT > G.B(G.checkpoint) + 1, 2000);
		G.player.hp = 1; G.player.inv = 0; G.damage(); Ok(G.state == "dead" && G.deaths == 1, "dies");
		for (int j = 0; j < 240; j++) Frame(); Press("Enter"); Ok(G.deaths == 1, $"  continue from save keeps 1 death ({G.deaths})");
		Until(() => G.state == "play", 2000); for (int j = 0; j < 100; j++) Frame();
		Pause(4); G.player = null;
		log.Append($"   ({frames} frames at 240 Hz)\n");
	}

	int rdeaths;
	int ReplayDeaths(Level L, List<RpSeg> segs, int v)
	{
		G.rp.doc = new RpDoc { r = "B", hz = G.RP_HZ, v = v, segs = segs }; G.rp.bot = false; G.rp.back = "levels"; G.rp.L = L; G.rp.m = "n"; G.rp.hide = true;
		G.rp.saveEasy = G.easy; G.cur = L; G.levelTiming(L); G.hitsTotal = 0; G.deaths = 0;
		G.rp.paused = false; G.rp.rate = 1; G.rp.barT = 4; G.rpTimeline(); G.rpGo(0);
		for (int j = 0; j < 240 * 400 && G.state != "rpEnd"; j++)
		{
			AudioCtx.manualTime += 1.0 / 240; performance.manual += 1000.0 / 240;
			if (G.state == "rpGap") G.rpSeg(G.rp.seg + 1); else G.updateReplay(1.0 / 240);
		}
		int d = G.deaths; G.exitReplay(); return rdeaths = d;
	}

	void Plat()
	{
		var P = G.PLATS[0]; log.Append($"== platformer {P.id}\n");
		G.plStart(P); Until(() => G.state == "pltPlay", 2000);
		for (int j = 0; j < 400; j++) Frame(null, false);
		// no save yet: like from the start
		Press("Escape"); Ok(G.state == "pltPause", $"  paused ({G.state})"); Press("ArrowDown"); Press("Enter");
		Ok(G.deaths == 0 && G.pl.t < 1, $"no save yet -> fresh start (deaths {G.deaths}, t {G.pl.t:F2})");
		// stand on the save, then back to the last save = a death
		int cx = -1, cy = -1; for (int y = 0; y < G.pl.h; y++) for (int x = 0; x < G.pl.w; x++) if (G.pl.m[y][x] == 'C') { cx = x; cy = y; }
		G.player.x = cx * G.PT + G.PT / 2; G.player.y = (cy + 1) * G.PT - 9; G.pl.vx = G.pl.vy = 0;
		for (int j = 0; j < 30 && !G.pl.hasCp; j++) Frame(null, false);
		Ok(G.pl.hasCp, "took the save");
		Press("Escape"); Press("ArrowDown"); Press("Enter");
		Ok(G.deaths == 1 && G.state == "pltPlay" && Math.Abs(G.player.x - G.pl.cx) < 1, $"back to the save = death (deaths {G.deaths})");
		Press("Escape"); Press("ArrowDown"); Press("ArrowDown"); Press("Enter");
		Ok(G.deaths == 0 && !G.pl.hasCp && G.pl.t < 1, $"from the start resets (deaths {G.deaths})");
	}
}
