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

	void Ok(bool c, string what) { log.Append(c ? "OK   " : "FAIL ").Append(what).Append('\n'); if (!c) fails++; File.WriteAllText(outFile, log.ToString()); }
	int frames;
	void Frame(string key = null, bool god = true)
	{
		foreach (var k in new[] { "Escape", "Enter", "ArrowDown", "ArrowUp", "KeyR", "Backspace", "KeyT" }) G.keys[k] = false;
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
		if (ids[0] == "RP") { try { RpTests(); } catch (Exception e) { Ok(false, "rp threw " + e); } }
		else if (ids[0] == "RPD") { try { DodgeRp(); } catch (Exception e) { Ok(false, "dodge rp threw " + e); } }
		else if (ids[0] == "CUT") { try { CutRp(); } catch (Exception e) { Ok(false, "cut threw " + e); } }
		else if (ids[0] == "TRP") { try { TeamRp(); } catch (Exception e) { Ok(false, "team rp threw " + e); } }
		else if (ids[0] == "PRP") { foreach (var id in ids.Skip(1)) try { PracRp(id); } catch (Exception e) { Ok(false, "practice rp threw " + e); } }
		else if (ids[0] == "RPP") { try { PlatRp(); } catch (Exception e) { Ok(false, "plat rp threw " + e); } }
		else
		{
			foreach (var id in ids) try { Dodge(id); } catch (Exception e) { Ok(false, id + " threw " + e); }
			try { Plat(); } catch (Exception e) { Ok(false, "plat threw " + e); }
		}
		log.Append(fails == 0 ? "ALL OK\n" : $"{fails} FAILED\n");
		File.WriteAllText(outFile, log.ToString()); host.GetTree().Quit();
	}

	void Pause(int sel) { Press("Escape"); Ok(G.state == "paused", $"  paused (state {G.state})"); for (int j = 0; j < sel; j++) Press("ArrowDown"); Ok(G.pauseSel == sel, $"  pause item {sel}"); Press("Enter"); log.Append($"     after Enter: state {G.state} deaths {G.deaths} hits {G.hitsTotal} cp {G.checkpoint}\n"); }

	void Dodge(string id)
	{
		var L = G.ALL_LV().Concat(G.HCLV).Concat(G.BOSSLV).First(x => x.id == id); log.Append($"== {id} {L.name}\n");
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
		Pause(G.pauseList().Length - 1); G.player = null;   // the exit (the last item)
		log.Append($"   ({frames} frames at 240 Hz)\n");
	}

	// ---------- v46: a long Hardcore win keeps only the attempts that won; short ones and the other levels as before ----------
	// Usage: -- --restart-test <outFile> RP
	void Die() { G.player.hp = 1; G.player.inv = 0; G.damage(); G.requestStart(G.checkpoint); Until(() => G.state == "play", 2000); }
	G.RpqItem Queued(string id) { var j = localStorage.getItem("mg_rpq"); if (j == null) return null; var d = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, G.RpqItem>>(j); return d.TryGetValue(id + "_n", out var q) ? q : null; }
	void Run(string id, int[] diesAt)
	{
		var L = G.ALL_LV().Concat(G.HCLV).Concat(G.BOSSLV).First(x => x.id == id); log.Append($"== {id} {L.name} deaths per save {string.Join(",", diesAt)}\n");
		localStorage.removeItem("mg_rpq"); L.best = null; G.easy = false; G.cur = L; G.fromList = "levels"; G.levelTiming(L); if (L.buf == null) L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
		G.requestStart(0); Until(() => G.state == "play", 2000);
		for (int k = 0; k < diesAt.Length; k++)
		{
			if (k > 0) { double c0 = G.checkpoint; Until(() => G.checkpoint > c0, 240 * 300); double c1 = G.checkpoint; Until(() => G.songT > G.B(c1) + 2.5, 240 * 10); Die(); }
			for (int j = 0; j < diesAt[k]; j++) { double t = G.songT; Until(() => G.songT > t + 0.4, 600); Die(); }
		}
		Ok(Until(() => G.state == "win", 240 * 400), $"  wins ({G.lastRank}, deaths {G.deaths}, attempts {G.rec?.segs.Count ?? -1})");
	}
	List<RpSeg> Unpack(G.RpqItem q) => q.segs.Select(s => new RpSeg { cp = s.cp, seed = s.seed, t0 = s.t0, n = s.n, b = G.unpackRp(s.d), c = s.c != 0 }).ToList();
	void RpTests()
	{
		// 1) Hardcore, over the limit (more than 100 attempts): only the run that won, cut before each death
		Run("H1", new[] { 0, 60, 45 }); var q = Queued("H1");   // dying before the first save starts over (no attempt kept)
		Ok(q != null, "  replay queued for the server");
		if (q != null)
		{
			Ok(q.segs.Count == 3 && q.segs.Select(s => s.cp).SequenceEqual(new double[] { 0, 84, 148 }), $"  3 parts from saves 0,84,148 ({string.Join(",", q.segs.Select(s => s.cp))})");
			Ok(q.segs.Select(s => s.c).SequenceEqual(new[] { 1, 1, 0 }), $"  first two cut ({string.Join(",", q.segs.Select(s => s.c))})");
			var segs = Unpack(q); bool alive = segs.Take(2).All(s => ((s.b[(s.n - 1) * 3 + 2] >> 1) & 7) > 0); Ok(alive, "  cut parts end alive (no death shown)");
			for (int k = 0; k < 2; k++) { double end = segs[k].t0 + (segs[k].n - 1) / 50.0, nx = G.B(segs[k + 1].cp - 2); Ok(Math.Abs(end - nx) < 0.03, $"  part {k + 1} ends where part {k + 2} starts ({end:F2} / {nx:F2})"); }
			Ok(q.segs.Sum(s => s.d.Length + 80) < G.RP_MAX_BYTES, $"  size {q.segs.Sum(s => s.d.Length + 80)}");
			var (d, gaps, end2) = Play(G.HCLV[0], segs, q.r);
			Ok(d == 0 && gaps == 0 && end2, $"  replay plays to the end with no deaths or stops (deaths {d}, stops {gaps}, end {end2})");
		}
		// 2) Hardcore under the limit: the whole run with its deaths, as before
		Run("H1", new[] { 0, 3, 2 }); q = Queued("H1");
		Ok(q != null && q.segs.Count == 8 && q.segs.All(s => s.c == 0), $"  short: whole run kept ({q?.segs.Count} parts, none cut)");
		if (q != null) { var (d, gaps, e) = Play(G.HCLV[0], Unpack(q), q.r); Ok(d == 7 && gaps == 7 && e, $"  replay shows the 7 deaths ({d}, stops {gaps})"); }
		// 3) a normal level over the limit: no replay, unchanged
		Run("L1", new[] { 0, 105 }); q = Queued("L1"); Ok(q == null, "  normal level over the limit: not saved (unchanged)");
	}
	(int deaths, int gaps, bool end) Play(Level L, List<RpSeg> segs, string r)
	{
		G.rp.doc = new RpDoc { r = r, hz = G.RP_HZ, v = G.RP_V, segs = segs }; G.rp.bot = false; G.rp.back = "levels"; G.rp.L = L; G.rp.m = "n"; G.rp.hide = true; G.rp.p = new RpPerson { name = "t", uid = "t" };
		G.rp.saveEasy = G.easy; G.cur = L; G.levelTiming(L); G.hitsTotal = 0; G.deaths = 0;
		G.rp.paused = false; G.rp.rate = 1; G.rp.barT = 4; G.rpTimeline(); G.rpGo(0); int gaps = 0;
		for (int j = 0; j < 240 * 600 && G.state != "rpEnd"; j++)
		{
			AudioCtx.manualTime += 1.0 / 240; performance.manual += 1000.0 / 240;
			if (G.state == "rpGap") { gaps++; G.rpSeg(G.rp.seg + 1); } else G.updateReplay(1.0 / 240);
		}
		bool e = G.state == "rpEnd"; int d = G.deaths; G.exitReplay(); return (d, gaps, e);
	}

	// ---------- v46: the main mode's replays on the PC: lose screen, pause, win screen, the list, watching from the pause ----------
	// Usage: -- --restart-test <outFile> RPD
	void DodgeRp()
	{
		G.MYRP_DIR = "user://test_replays"; var dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		int Files() => Godot.DirAccess.Open(G.MYRP_DIR)?.GetFiles().Length ?? 0;
		var L = G.HCLV[0]; log.Append($"== {L.id} {L.name} replays on the PC\n");
		G.easy = false; G.cur = L; G.fromList = "hcList"; G.levelTiming(L); if (L.buf == null) L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
		G.requestStart(0); Until(() => G.state == "play", 2000); Until(() => G.checkpoint > 0, 240 * 120); Until(() => G.songT > G.B(G.checkpoint) + 2, 240 * 5);
		G.player.hp = 1; G.player.inv = 0; G.damage(); Ok(G.state == "dead", "dies");
		Press("KeyT"); Ok(Files() == 0, "  T too soon does nothing"); for (int j = 0; j < 200; j++) Frame(); Press("KeyT"); Ok(Files() == 1, $"  lose screen saves ({Files()})");
		Press("KeyT"); Ok(Files() == 1, "  only once");
		Press("Enter"); Until(() => G.state == "play", 2000); for (int j = 0; j < 480; j++) Frame();
		Press("Escape"); for (int j = 0; j < 4; j++) Press("ArrowDown"); Press("Enter"); Ok(Files() == 2 && G.state == "paused", $"  pause saves ({Files()}, {G.state})");
		Press("Enter"); Ok(Files() == 3, "  pause saves again (a new file)");
		Press("ArrowDown"); Press("Enter"); Ok(G.state == "myRp", $"  list from the pause ({G.state})");
		Press("Backspace"); Ok(G.state == "paused", $"  back to the pause ({G.state})");
		for (int j = 0; j < 5; j++) Press("ArrowUp"); Ok(G.pauseSel == 0, $"  top ({G.pauseSel})"); Press("Enter"); Ok(G.state == "play", $"  playing on ({G.state})");
		Until(() => G.state == "win", 240 * 300); Ok(G.state == "win", $"  wins ({G.lastRank})");
		for (int j = 0; j < 240; j++) Frame(); Press("KeyT"); Ok(Files() == 4, $"  win screen saves ({Files()})");
		// the list from the main menu: newest first, the win; watch it to the end
		G.stopMusic(); G.player = null; G.openMyRp("main"); Ok(G.state == "myRp", "list from the menu");
		Press("Enter"); Until(() => G.state == "replay", 2000); Ok(G.state == "replay" && G.rp.doc.r == G.lastRank, $"  watching the win ({G.state} {G.rp.doc?.r})");
		Ok(G.rp.doc.segs.Count == 2, $"  2 parts: before and after the death ({G.rp.doc.segs.Count})");
		for (int j = 0; j < 240 * 300 && G.state != "rpEnd"; j++) { Frame(); if (G.state == "rpGap") { } }
		Ok(G.state == "rpEnd", $"  to its end ({G.state})");
		Press("Backspace"); Ok(G.state == "myRp", $"  back to the list ({G.state})");
		// delete: twice
		Press("KeyR"); Ok(Files() == 4, "  one R: asks"); Press("KeyR"); Ok(Files() == 3, $"  two: deleted ({Files()})");
		Press("Backspace"); Ok(G.state == "main", $"  back to the menu ({G.state})");
		// watching from the pause leaves the run (asks first)
		G.requestStart(0); Until(() => G.state == "play", 2000); for (int j = 0; j < 240; j++) Frame();
		Press("Escape"); for (int j = 0; j < 5; j++) Press("ArrowDown"); Press("Enter"); Ok(G.state == "myRp", "list from the pause");
		Press("Enter"); Ok(G.state == "myRp", "  first press asks"); Press("Enter"); Ok(G.state == "rpLoad" || G.state == "replay", $"  second watches ({G.state})");
		Until(() => G.state == "replay", 2000); Press("Backspace"); Ok(G.state == "myRp", $"  back to the list ({G.state})"); Press("Backspace"); Ok(G.state == "hcList", $"  then the level list: the run was left ({G.state})");
	}

	// ---------- v46: "بدون الموتات" in a replay on the PC (his words 20:10 10-22): T ticks it, T again back to the whole one ----------
	// Usage: -- --restart-test <outFile> CUT   (env TEAMRP = a co-op replay file from test/TeamBot.cs RPT=1)
	void CutRp()
	{
		G.MYRP_DIR = "user://test_replays"; var dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		int gaps = 0;
		void PlayTo(Func<bool> stop, int max) { for (int j = 0; j < max && !stop(); j++) { bool gap = G.state == "rpGap"; Frame(); if (!gap && G.state == "rpGap") gaps++; } }
		// 1) the main mode: a death before the first save, two after it, then the win
		Run("L1", new[] { 1, 2 }); for (int j = 0; j < 240; j++) Frame(); Press("KeyT");
		var keep = System.Environment.GetEnvironmentVariable("CUT_OUT"); if (keep != null) foreach (var f in Godot.DirAccess.Open(G.MYRP_DIR).GetFiles()) System.IO.File.Copy(Godot.ProjectSettings.GlobalizePath(G.MYRP_DIR + "/" + f), keep, true);
		G.stopMusic(); G.player = null; G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 2000);
		Ok(G.rp.cutOk && G.rp.full.Count == 4 && G.rp.cut.Count == 2 && !G.rp.cutOn, $"L1: whole 4 attempts, without deaths 2 ({G.rp.full?.Count}/{G.rp.cut?.Count}), starts whole");
		double tot = G.rp.tot; G.rpSeekTo(tot - 8); int segF = G.rp.seg; double t0 = G.songT;
		Press("KeyT"); Ok(G.rp.cutOn && G.rp.doc.segs.Count == 2 && G.rp.seg == 1 && Math.Abs(G.songT - t0) < 0.1, $"  ticked at the last attempt: the same moment (seg {segF}->{G.rp.seg}, {t0:F2}->{G.songT:F2})");
		Ok(G.rp.tot < tot, $"  shorter ({G.rp.tot:F0}s of {tot:F0}s)");
		G.rpSeekTo(0); gaps = 0; G.deaths = 0; PlayTo(() => G.state == "rpEnd", 240 * 400);
		Ok(G.state == "rpEnd" && gaps == 0 && G.deaths == 0, $"  plays to the end with no death ({G.state}, stops {gaps}, deaths {G.deaths})");
		Press("KeyT"); Ok(!G.rp.cutOn && G.rp.doc.segs.Count == 4 && G.state == "rpEnd", $"  untick at the end: whole again ({G.rp.doc.segs.Count}, {G.state})");
		G.rpSeekTo(0); gaps = 0; G.deaths = 0; PlayTo(() => G.state == "rpEnd", 240 * 500);
		Ok(G.state == "rpEnd" && gaps == 3 && G.deaths == 3, $"  whole: the 3 deaths ({gaps} stops, deaths {G.deaths})");
		Press("Backspace"); Press("Backspace");
		// a replay with no death: no box
		Run("L1", new[] { 0 }); for (int j = 0; j < 240; j++) Frame(); Press("KeyT");
		G.stopMusic(); G.player = null; G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 2000);
		Ok(!G.rp.cutOk, "  a clean run: no box"); Press("Backspace"); Press("Backspace");
		// the platformer: a run with a death (test PlatRp saves it from the pause), watched again with the box
		PlatRp(); Until(() => G.state is "myRp" or "pltList", 600); G.pl = null;
		G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 4000);
		Ok(G.rp.plat && G.rp.cutOk && G.rp.full.Count == 2 && G.rp.cut.Count == 1, $"platformer: the box, 2 parts / 1 ({G.rp.full?.Count}/{G.rp.cut?.Count})");
		Press("KeyT"); gaps = 0; G.deaths = 0; PlayTo(() => G.state == "rpEnd", 240 * 60);
		Ok(G.rp.cutOn && G.state == "rpEnd" && gaps == 0, $"  plays to the end with no death ({G.state}, stops {gaps})");
		Press("Backspace"); Press("Backspace");
		// 2) the co-op level (a bots' run with restarts)
		string tf = System.Environment.GetEnvironmentVariable("TEAMRP");
		if (tf != null)
		{
			dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
			File.Copy(tf, ProjectSettings.GlobalizePath(G.MYRP_DIR + "/9999999999999_P2.json"), true);
			G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 4000);
			Ok(G.rp.team && G.rp.cutOk, $"co-op: the box ({G.rp.cutOk})"); double ft = G.rp.tot;
			G.rpSeekTo(300); Press("KeyT"); Ok(G.rp.cutOn && G.rp.tot < ft && G.state == "replay", $"  ticked: {G.rp.tot:F0}s of {ft:F0}s ({G.state})");
			for (int j = 0; j < 240 * 5; j++) Frame(); Ok(G.state == "replay" && G.tm != null, $"  plays ({G.state})");
			G.rpSeekTo(G.rp.tot - 3); PlayTo(() => G.state == "rpEnd", 240 * 10); Ok(G.state == "rpEnd" && G.tm.won, $"  ends on the win ({G.state})");
			Press("KeyT"); Ok(!G.rp.cutOn && Math.Abs(G.rp.tot - ft) < 0.01, "  whole again");
			Press("Backspace"); Press("Backspace");
		}
	}

	// ---------- v46: the co-op level in a room (no other PCs: this one is the maker): pause saves, no list there; the win screen saves ----------
	// Usage: -- --restart-test <outFile> TRP
	void TeamRp()
	{
		G.MYRP_DIR = "user://test_replays"; var dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		int Files() => Godot.DirAccess.Open(G.MYRP_DIR)?.GetFiles().Length ?? 0;
		var P = G.PLATS.First(p => p.team != null); log.Append($"== {P.name} (co-op) replays on the PC\n");
		G.net = new G.NetRun { team = true, host = true, seat = 0, L = P }; for (int i = 0; i < G.TM_N; i++) G.net.hi[i] = true;
		typeof(G).GetMethod("tnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { performance.manual.Value + 200 });
		Until(() => G.state == "tmPlay", 2000); Ok(G.state == "tmPlay" && G.tmRec != null, $"playing, recording ({G.state})");
		for (int j = 0; j < 240 * 4; j++) Frame(j % 200 < 100 ? null : "ArrowRight");
		Ok(G.tmRec.n > 100, $"  {G.tmRec.n} pictures so far");
		Press("Escape"); Ok(G.state == "tmPause" && G.tmPauseList().Length == 4 && G.tmPauseList()[2] == "احفظ الريبلاي" && !G.tmPauseList().Contains("ريبلاياتي"), $"  pause: {string.Join(" / ", G.tmPauseList())}");
		Press("ArrowDown"); Press("ArrowDown"); Press("Enter"); Ok(Files() == 1 && G.state == "tmPause", $"  pause saves ({Files()}, {G.state})");
		for (int j = 0; j < 240; j++) Frame(); Ok(G.state == "tmPause" && G.tm != null, "  the level goes on under the pause (online)");
		Press("Escape"); Ok(G.state == "tmPlay", $"  back to play ({G.state})");
		// the win screen (made to win here): T saves, once
		G.tm.won = true; G.tm.wonAt = G.tm.t - 2;
		for (int j = 0; j < 60; j++) Frame(); Press("KeyT"); Ok(Files() == 2, $"  win screen saves ({Files()})"); Press("KeyT"); Ok(Files() == 2, "  only once");
		Press("Backspace"); Ok(G.state == "pltList" && G.net == null, $"  left the room ({G.state})");
		G.myRpLoadPub(); var its = G.myRpsPub(); Ok(its.Count == 2 && its.All(i => i.kind == "team") && its.Any(i => i.res == "win") && its.Any(i => i.res == "mid"), $"  2 co-op files ({string.Join(",", its.Select(i => i.kind + ":" + i.res))})");
		// watched from the menu's list
		G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 2000); Ok(G.state == "replay" && G.rp.team && G.tm != null, $"  watching ({G.state})");
		for (int j = 0; j < 240 * 3; j++) Frame(); Ok(G.state is "replay" or "rpEnd", $"  plays ({G.state}, {G.songT:F1}s)");
		Press("Backspace"); Ok(G.state == "myRp" && G.tm == null && G.tmMe == -1, $"  back to the list ({G.state})");
	}

	// ---------- v46: practice replays on the PC (his يالله 19:07 10-22): the attacks in the replay are the ones he practised against ----------
	// Usage: -- --restart-test <outFile> PRP,L1,L15
	static string Corner() { var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance; var c = typeof(G).GetField("corner", BF)?.GetValue(null); return c == null ? "?" : c.GetType().GetField("t", BF)?.GetValue(c) + ""; }
	void PracRp(string id)
	{
		G.MYRP_DIR = "user://test_replays"; var dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		int Files() => Godot.DirAccess.Open(G.MYRP_DIR)?.GetFiles().Length ?? 0;
		var L = G.ALL_LV().First(x => x.id == id); log.Append($"== {L.id} {L.name} practice replays\n");
		G.easy = false; if (L.buf == null) L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.noMus = false;
		void Open(double t, double spd, bool inf, int lv) { G.pr.L = L; G.pr.lastL = L; G.pr.from = "levels"; G.pr.doc = G.botDoc(L, "n"); G.cur = L; G.fromList = "levels"; G.levelTiming(L); G.pr.t = t; G.pr.spd = spd; G.pr.inf = inf; G.pr.lv = lv; G.prGo(); Until(() => G.state == "play", 4000); }
		// what the attacks were at each whole step number (every 0.5 s), during practice and during its replay
		Dictionary<long, string> Snap(Dictionary<long, string> d) { if (G.simN % 120 == 0 && G.state is "play" or "replay") d[G.simN] = string.Join(",", G.hazards.Where(h => !h.dead).Select(h => $"{h.rid}@{Math.Round(h.x)}:{Math.Round(h.y)}").OrderBy(x => x)); return d; }
		G.levelTiming(L); double endT = G.B(L.end), T0 = Math.Max(10, endT - 25);
		// 1) never-ending lives, 75%, standing still (gets hit), wander a little; save from the pause, then at the end
		double SP = double.Parse(System.Environment.GetEnvironmentVariable("PRSPD") ?? "0.75"); Open(T0, SP, true, 5); Ok(G.state == "play" && G.rec?.cur?.fr == T0, $"practice from {T0:F1} at {SP * 100}% ({G.state})");
		var pa = new Dictionary<long, string>(); int k = 0;
		while (G.state == "play" && k++ < 240 * 60) { string key = (k / 90) % 4 == 0 ? "ArrowUp" : (k / 90) % 4 == 2 ? "ArrowDown" : null; Frame(key, god: false); Snap(pa); }
		Ok(G.state == "prEnd", $"  finished ({G.state}, hits {G.hitsTotal})"); int hits = (int)G.hitsTotal;
		log.Append($"     dbg rec {(G.rec == null ? "null" : string.Join("/", G.rec.segs.Select(s => s.n)))} classic {G.classicUi} corner {Corner()}\n");
		for (int j = 0; j < 200; j++) Frame(); Press("KeyT"); log.Append($"     dbg after T corner {Corner()}\n"); Ok(Files() == 1, $"  end screen saves ({Files()})"); Press("KeyT"); Ok(Files() == 1, "  only once");
		Press("Backspace"); Ok(G.state == "practice", $"  back to practice ({G.state})"); G.state = "levels"; G.pr.on = false;
		// watch it: 75%, the same attacks at the same moments, the same hits
		G.openMyRp("main"); Press("Enter"); Until(() => G.state == "replay", 4000);
		Ok(G.state == "replay" && G.rp.pr && Math.Abs(G.rp.rate - SP) < 1e-9, $"  watching at {SP * 100}% ({G.state}, {G.rp.rate})");
		var pb = new Dictionary<long, string>(); for (int j = 0; j < 240 * 60 && G.state != "rpEnd"; j++) { Frame(); Snap(pb); }
		Ok(G.state == "rpEnd", $"  to its end ({G.state})");
		bool Near(string a, string b) { var A = a.Split(','); var Bs = b.Split(','); if (A.Length != Bs.Length) return false; for (int i = 0; i < A.Length; i++) { var x = A[i].Split('@'); var y = Bs[i].Split('@'); if (x[0] != y[0]) return false; if (x.Length > 1) { var u = x[1].Split(':'); var w = y[1].Split(':'); if (Math.Abs(double.Parse(u[0]) - double.Parse(w[0])) > 2 || Math.Abs(double.Parse(u[1]) - double.Parse(w[1])) > 2) return false; } } return true; }
		double Px(string a, string b) { var A = a.Split(','); var Bs = b.Split(','); if (A.Length != Bs.Length) return 999; double m = 0; for (int i = 0; i < A.Length; i++) { var x = A[i].Split('@'); var y = Bs[i].Split('@'); if (x[0] != y[0]) return 999; if (x.Length > 1) { var u = x[1].Split(':'); var w = y[1].Split(':'); m = Math.Max(m, Math.Max(Math.Abs(double.Parse(u[0]) - double.Parse(w[0])), Math.Abs(double.Parse(u[1]) - double.Parse(w[1])))); } } return m; }
		log.Append("     px by moment: " + string.Join(" ", pa.Keys.OrderBy(x => x).Where(pb.ContainsKey).Select(x => Px(pa[x], pb[x]).ToString("0")) ) + "\n");
		int same = pa.Keys.Count(x => pb.TryGetValue(x, out var v) && Near(v, pa[x])), both = pa.Keys.Count(pb.ContainsKey);
		// attacks that aim at the square can drift a few px (the replay has the square at 50 Hz, like every replay): allowed up to 10 px, one moment off by a frame
		var pxs = pa.Keys.Where(pb.ContainsKey).Select(x => Px(pa[x], pb[x])).ToList();
		Ok(both > 20 && same >= both * 0.7 && pxs.Count(v => v > 10) <= 1, $"  the attacks match ({same} of {both} moments within 2 px, {pa.Keys.Count(x => pb.TryGetValue(x, out var v) && v == pa[x])} exactly, worst {pxs.Where(v => v < 999).Max():0} px, {pxs.Count(v => v >= 999)} off by a frame)");
		if (same != both) { var x = pa.Keys.First(q => pb.ContainsKey(q) && !Near(pb[q], pa[q])); log.Append($"     first diff at step {x}:\n     {pa[x]}\n     {pb[x]}\n"); }
		Ok(G.hitsTotal == hits, $"  the same hits ({G.hitsTotal} / {hits})");
		Press("Backspace"); Ok(G.state == "myRp", $"  back to the list ({G.state})"); Press("Backspace");
		// 2) 3 lives, dies: the lose screen saves; the pause saves and its list leaves the practice
		Open(T0, 1, false, 3); Ok(G.player.hp == 3, $"3 lives ({G.player.hp})");
		for (int j = 0; j < 480; j++) Frame(); G.player.hp = 1; G.player.inv = 0; G.damage();
		Ok(G.state == "dead", $"  dies ({G.state})"); for (int j = 0; j < 200; j++) Frame(); Press("KeyT"); Ok(Files() == 2, $"  lose screen saves ({Files()})");
		Press("Enter"); Until(() => G.state == "play", 4000); for (int j = 0; j < 240; j++) Frame();
		Press("Escape"); for (int j = 0; j < 4; j++) Press("ArrowDown"); Press("Enter"); Ok(Files() == 3 && G.state == "paused", $"  pause saves ({Files()}, {G.state})");
		Press("ArrowDown"); Press("Enter"); Ok(G.state == "myRp", $"  list from the pause ({G.state})");
		Press("Backspace"); Ok(G.state == "paused" && G.pr.on, $"  back to the pause, still practising ({G.state})");
		Ok(G.pauseSel == 5, $"  still on ريبلاياتي ({G.pauseSel})"); Press("Enter"); Press("Enter"); Ok(G.state == "myRp", "  first press asks"); Press("Enter"); Until(() => G.state == "replay", 4000);
		Ok(G.state == "replay" && !G.pr.on && G.spd == 1, $"  watching left the practice ({G.state}, practice {G.pr.on}, speed {G.spd})");
		Press("Backspace"); Press("Backspace"); Ok(G.state == "levels", $"  then the level list ({G.state})");
		G.myRpLoadPub(); var its = G.myRpsPub(); Ok(its.Count == 3 && its.All(i => i.pr) && its.Select(i => i.res).OrderBy(x => x).SequenceEqual(new[] { "dead", "mid", "pdone" }), $"  3 files, all practice ({string.Join(",", its.Select(i => i.res))})");
	}

	// ---------- v46: the platformer's replay on the PC: what it shows is where the square was ----------
	// Usage: -- --restart-test <outFile> RPP
	void PlatRp()
	{
		G.MYRP_DIR = "user://test_replays"; var dd = Godot.DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		var P = G.PLATS[0]; log.Append($"== platformer {P.id} replay\n"); G.noMus = false;
		G.plStart(P); Until(() => G.state == "pltPlay", 2000);
		var seen = new List<(double t, double x, double y)>(); int got0 = 0;
		void Walk(int nf)
		{
			for (int j = 0; j < nf; j++)
			{
				foreach (var k in new[] { "Escape", "Enter", "ArrowDown", "ArrowUp", "KeyR", "Space", "ArrowRight", "ArrowLeft" }) G.keys[k] = false;
				G.keys[(j / 240) % 3 == 2 ? "ArrowLeft" : "ArrowRight"] = true; if (j % 50 < 20) G.keys["Space"] = true;
				if (G.player != null) G.player.inv = 1e9;
				AudioCtx.manualTime += 1.0 / 240; performance.manual += 1000.0 / 240; frames++; G.gfx.BeginFrame(); G.step(performance.manual.Value);
				if (G.state == "pltPlay" && j % 37 == 0 && G.player != null) seen.Add((G.pl.t, G.player.x, G.player.y));
				if (G.state != "pltPlay") break;
			}
			foreach (var k in new[] { "Space", "ArrowRight", "ArrowLeft" }) G.keys[k] = false; Frame(null, false);
		}
		Walk(240 * 6);
		Ok(G.state == "pltPlay", $"  walking ({G.state}, x {G.player?.x:F0})");
		// a death, then on from the last save
		G.player.inv = 0; typeof(G).GetMethod("plDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
		Ok(G.state == "pltDead", $"  dies ({G.state})"); for (int j = 0; j < 240; j++) Frame(null, false); Press("Enter"); Ok(G.state == "pltPlay", $"  on from the save ({G.state})");
		Walk(240 * 4); got0 = G.pl.got.Count;
		double tEnd = G.pl.t;
		// save from the pause menu, then watch it from the list
		Press("Escape"); for (int j = 0; j < 4; j++) Press("ArrowDown"); Ok(G.pauseSel == 4, $"  pause item 4 ({G.pauseSel})"); Press("Enter");
		var files = Godot.DirAccess.Open(G.MYRP_DIR)?.GetFiles() ?? new string[0]; Ok(files.Length == 1, $"  one file saved ({files.Length})");
		Press("ArrowDown"); Press("Enter"); Ok(G.state == "myRp", $"  list from the pause ({G.state})");
		Press("Enter"); Press("Enter"); Ok(G.state == "rpLoad" || G.state == "replay", $"  watching ({G.state})");
		Until(() => G.state == "replay", 2000);
		int cmp = 0; double worst = 0; int k2 = 0;
		for (int j = 0; j < 240 * 30 && G.state is "replay" or "rpGap"; j++)
		{
			AudioCtx.manualTime += 1.0 / 240; performance.manual += 1000.0 / 240; G.gfx.BeginFrame(); G.step(performance.manual.Value);
			if (G.state != "replay" || G.pl == null) continue;
			while (k2 < seen.Count && seen[k2].t < G.pl.t - 0.03) k2++;
			if (k2 < seen.Count && Math.Abs(seen[k2].t - G.pl.t) < 1.0 / 240) { cmp++; worst = Math.Max(worst, Math.Abs(seen[k2].x - G.player.x) + Math.Abs(seen[k2].y - G.player.y)); }
		}
		Ok(G.state == "rpEnd", $"  replay reaches its end ({G.state})");
		Ok(cmp > 20 && worst < 12, $"  the square is where it was ({cmp} points, worst {worst:F1} px)");
		Ok(G.pl?.got.Count == got0, $"  pieces taken the same ({G.pl?.got.Count} / {got0})");
		Press("Backspace"); Ok(G.state == "myRp", $"  back to the list ({G.state})");
		Frame("Backspace"); log.Append($"   after back: {G.state}\n"); Frame(); Frame(); Ok(G.state == "pltList", $"  back out: the run was left ({G.state})");
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
