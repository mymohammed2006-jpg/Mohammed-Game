using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MG;
using static MG.JS;

// Pictures of the new screens (XP, challenges, looks) for Mohammed before they go in: -- --preview <outDir>
public class PvRun
{
	string outDir; int i; Main host; string pending; readonly List<string> rel = new();   // keys let go on the next frame
	readonly Dictionary<int, Action> at = new(); readonly Dictionary<int, string> shots = new();
	BinaryWriter wav = System.Environment.GetEnvironmentVariable("WAV") != null ? new BinaryWriter(new MemoryStream()) : null;
	public static PvRun FromArgs(string[] a, int k) => new PvRun { outDir = a[k] };

	static void key(string k, bool down) { if (down) G.OnKeyDown(k, false); else G.OnKeyUp(k); }
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.netSend = r => System.Threading.Tasks.Task.FromException<System.Net.Http.HttpResponseMessage>(new System.Net.Http.HttpRequestException("offline preview"));
		string[] S = { "L1", "L2", "L3" }, A = { "L4", "L5", "L6", "L7", "L8", "L9", "L10" };
		performance.manual = 100000; AudioCtx.manualTime = 0; AudioCtx.resume(); G.InitSound();
		G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadSongs();
		foreach (var L in G.ALL_LV()) { if (Array.IndexOf(S, L.id) >= 0) L.best = "S"; if (Array.IndexOf(A, L.id) >= 0) L.best = "A"; if (L.id == "F8") L.best = "B"; if (L.id == "F1") L.bestE = "S"; }
		G.LoadXp(); G.setSkin("crystal"); JS.SeedRandom(12345);
		var now = () => performance.manual.Value;
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "mouse") { MouseScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "hc") { HcScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "rp") { RpScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "all46") { All46(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "rpt") { RptScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "radio") { RadioScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "rpp") { RppScene(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "blist")
		{
			// the bosses' list by key presses: modes -> list -> a locked card (shakes) -> back to the boss -> its screen -> back -> back
			void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
			at[1] = () => { G.gatePassed = true; G.state = "modes"; G.modeSel = 2; }; shots[40] = "b1-modes";
			at[41] = () => tap("Enter"); shots[100] = "b2-list";
			at[101] = () => tap("ArrowLeft"); shots[150] = "b3-list-locked"; at[151] = () => tap("Enter"); shots[158] = "b4-locked-shake";
			at[200] = () => tap("ArrowRight"); at[230] = () => tap("Enter"); shots[290] = "b5-boss-screen";
			at[291] = () => tap("Backspace"); shots[340] = "b6-back-to-list"; at[341] = () => tap("Backspace"); shots[390] = "b7-back-to-modes";
			at[391] = () => System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "states.txt"), "end " + G.state);
			endF = 400; G.last = now(); return;
		}
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "v51")
		{
			// v51 check: what's new for a v50 player, then the bosses' list with a PlayStation controller's buttons: left/right do nothing (one card), confirm opens the boss, back, back
			void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
			at[1] = () => { G.gatePassed = true; localStorage.setItem("mg_news", "50"); G.goMain(now()); }; shots[120] = "1-news-v50-player";
			at[121] = () => { G.kindForce = "ps"; G.state = "modes"; G.modeSel = 2; }; at[150] = () => tap("Enter"); shots[200] = "2-boss-list-ps";
			at[201] = () => tap("ArrowLeft"); at[220] = () => tap("ArrowRight"); at[240] = () => tap("ArrowRight"); shots[270] = "3-after-left-right";
			at[271] = () => File.WriteAllText(Path.Combine(outDir, "states.txt"), $"after left/right: state {G.state} bossLv {G.bossLv}\n");
			at[280] = () => tap("Enter"); shots[330] = "4-boss-screen-ps";
			at[331] = () => File.AppendAllText(Path.Combine(outDir, "states.txt"), $"after confirm: state {G.state}\n");
			at[340] = () => tap("Backspace"); at[370] = () => tap("Backspace"); shots[400] = "5-back-to-modes";
			at[401] = () => File.AppendAllText(Path.Combine(outDir, "states.txt"), $"after back x2: state {G.state}\n");
			endF = 410; G.last = now(); return;
		}
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "b2list")
		{
			// boss 2: the bosses' list with a PlayStation controller's buttons: the first boss, left (the list goes right to left) -> رقصة الموت, confirm opens it, back, back
			void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
			at[1] = () => { G.gatePassed = true; G.kindForce = "ps"; G.state = "modes"; G.modeSel = 2; }; at[30] = () => tap("Enter"); shots[90] = "1-boss-list";
			at[91] = () => tap("ArrowLeft"); shots[140] = "2-boss-list-second";
			at[141] = () => File.WriteAllText(Path.Combine(outDir, "states.txt"), $"after left: state {G.state} bossLv {G.bossLv}\n");
			at[150] = () => tap("Enter"); shots[210] = "3-boss2-screen";
			at[211] = () => File.AppendAllText(Path.Combine(outDir, "states.txt"), $"after confirm: state {G.state} boss {G.BOSSLV[G.bossLv].id}\n");
			at[220] = () => tap("Backspace"); at[250] = () => tap("Backspace"); shots[280] = "4-back-to-modes";
			at[281] = () => File.AppendAllText(Path.Combine(outDir, "states.txt"), $"after back x2: state {G.state}\n");
			endF = 290; G.last = now(); return;
		}
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "cpfix") { CpFix(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "v49") { V49Tour(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "hc2") { HcOrder(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "hcsk") { HcSkins(now); G.last = now(); return; }
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "news")
		{
			endF = 200;
			at[1] = () => { G.gatePassed = true; localStorage.setItem("mg_news", System.Environment.GetEnvironmentVariable("MG_NEWS_SEEN") ?? "37"); G.goMain(now()); };
			shots[120] = "n1-news-v39-player";
			at[121] = () => { localStorage.setItem("mg_news", "30"); G.goMain(now()); };
			shots[190] = "n2-news-v30-player";
			G.last = now(); return;
		}
		at[1] = () => { G.gatePassed = true; G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 10; };
		shots[60] = "1-main";
		at[61] = () => { G.state = "skins"; G.chalSel = 1; };
		shots[120] = "2-skins";
		at[121] = () => { G.state = "chal"; };
		shots[150] = "3-chal";
		at[151] = () => { G.easy = false; G.startLevel(G.LEVELS[0], "levels"); };
		// moving and dashing in L1 with each look
		string[] sk = { "crystal", "circuit", "bolt", "eq" };
		for (int s = 0; s < 4; s++)
		{
			int f0 = 700 + s * 90; string ss = sk[s]; string dir = s % 2 == 0 ? "ArrowRight" : "ArrowLeft";
			at[f0] = () => { G.skin = ss; key(dir, true); key("ArrowUp", true); };
			at[f0 + 30] = () => key("Space", true); at[f0 + 33] = () => key("Space", false);
			shots[f0 + 37] = "4-level-" + ss;
			at[f0 + 45] = () => { key(dir, false); key("ArrowUp", false); };
		}
		at[1100] = () =>
		{
			var L11 = G.LEVELS.Find(l => l.id == "L11"); G.skin = "crystal";
			G.cur = L11; L11.best = "A"; G.lastRank = "A"; G.lastEz = false; G.lastGain = 140; G.hitsTotal = 2; G.deaths = 0;
			G.awardXp(); G.state = "win"; G.winAt = now() - 1000; G.winSlam = now() - 900; G.winHit = true;
		};
		shots[1190] = "5-win";
		at[1191] = () => { G.state = "chal"; G.chalView = 2; };
		shots[1230] = "6-chal-after";
		at[1231] = () =>
		{
			G.PlayerDoc P(string uid, string nm, string[] ids, string r, string[] eids) => new G.PlayerDoc { uid = uid, name = nm, n = ids.ToDictionary(k => k, k => r), e = eids.ToDictionary(k => k, k => "S"), nt = new(), et = new() };
			string[] all = G.ALL_LV().Select(l => l.id).ToArray(), main = G.LEVELS.Select(l => l.id).ToArray();
			G.board.list = new List<G.PlayerDoc> { P("a", "Mohammed", all, "S", all), P("b", "Link", all, "A", main.Take(5).ToArray()), P("c", "Nightmare", main, "S", new string[0]), P("d", "AJF", main.Take(7).ToArray(), "A", main.Take(3).ToArray()), P("e", "Twesii", new[] { "L1", "L2", "F5" }, "B", new string[0]), P("f", "ixN1X", new[] { "L1" }, "C", new[] { "L1" }) };
			G.board.at = now(); G.board.idx = 0; G.board.mode = "x"; G.board.sel = 0; G.state = "board";
		};
		shots[1270] = "7-board-xp";
		at[1271] = () => { G.board.idx = 1; G.board.sel = -1; };
		shots[1300] = "8-board-points";
		at[1301] = () => { G.prof.p = G.board.list[1]; G.prof.row = 0; G.prof.col = 0; G.state = "profile"; };
		shots[1330] = "9-profile";
		at[1331] = () => { G.acc = new G.Acc { name = "Mohammed", uid = "zz" }; G.netState = "ok"; G.state = "account"; };
		shots[1360] = "10-account";
		at[1361] = () => { G.acc = null; G.state = "skins"; G.chalSel = 3; G.skinInfo = true; };
		shots[1390] = "11-skin-info-locked";
		at[1391] = () => { G.chalSel = 4; };
		shots[1410] = "12-skin-info-open";
		G.last = now();
	}

	// the mouse and the free frame cap / volume: clicks go through G.mouseQ like real ones; the pictures show the zones in green
	bool showZones; int endF = 1415; Action over;
	static G.Zone find(Func<bool> want) { foreach (var z in G.zonesNow) { z.hover?.Invoke(); if (want()) return z; } return null; }
	static void click(G.Zone z, double fx = 0.5) { double x = z.x + z.w * fx, y = z.y + z.h / 2; G.mouseQ.Add((0, x, y)); G.mouseQ.Add((1, x, y)); G.mouseQ.Add((2, x, y)); }
	static void log(string s) => GD.Print("MOUSE ", s);
	void MouseScene(Func<double> now)
	{
		showZones = System.Environment.GetEnvironmentVariable("MG_ZONES") == "1"; endF = 800; G.fpsCap = 240; G.setVol(0.8);
		at[1] = () => { G.gatePassed = true; G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 0; };
		shots[30] = "m1-main";
		at[31] = () => { var z = find(() => G.menuSel == 10); click(z); };
		at[34] = () => log($"main click looks -> {G.state} (want skins)");
		shots[50] = "m2-skins";
		at[51] = () => { var z = find(() => G.chalSel == 3); click(z); };
		at[54] = () => log($"locked look click -> info {G.skinInfo} (want True)");
		shots[70] = "m3-skin-info";
		at[71] = () => G.mouseQ.Add((3, 600, 300));
		at[74] = () => { log($"right click -> info {G.skinInfo} (want False)"); G.chalSel = 0; G.mouseQ.Add((5, 600, 300)); };
		at[77] = () => { log($"wheel down -> look {G.chalSel} (want 1)"); G.mouseQ.Add((3, 600, 300)); };
		at[80] = () => { log($"right click -> {G.state} (want main)"); G.state = "settings"; G.setSel = 0; };
		shots[100] = "m4-settings";
		at[101] = () => { var z = G.zonesNow.Find(q => q.drag != null); double x0 = z.x + 6, tot = z.w - 12, x = x0 + tot * 0.37; G.mouseQ.Add((1, x, z.y + 28)); G.mouseQ.Add((0, x0 + tot * 0.5, z.y + 28)); G.mouseQ.Add((0, x, z.y + 28)); G.mouseQ.Add((2, x, z.y + 28)); };
		at[104] = () => log($"volume drag -> {G.vol} (want 0.37)");
		shots[110] = "m5-vol37";
		at[111] = () => { var z = G.zonesNow.FindLast(q => q.rep); click(z); };
		at[114] = () => { log($"fps right arrow -> {G.fpsCap} (want 241)"); var z = G.zonesNow.FindLast(q => q.rep); G.mouseQ.Add((1, z.x + 20, z.y + 24)); };
		at[175] = () => { var z = G.zonesNow.FindLast(q => q.rep); G.mouseQ.Add((2, z.x + 20, z.y + 24)); };
		at[177] = () => log($"fps arrow held 1 s -> {G.fpsCap} (want much more than 241)");
		at[180] = () => { var z = find(() => G.setSel == 3); click(z, 0.6); };
		at[183] = () => { log($"fps row click -> typing {G.numRow} (want fps)"); foreach (var k in new[] { "Digit1", "Digit4", "Digit4" }) { G.OnKeyDown(k, false); G.OnKeyUp(k); } };
		shots[200] = "m6-typing";
		at[201] = () => { G.OnKeyDown("Enter", false); G.OnKeyUp("Enter"); };
		at[204] = () => { log($"typed 144 + Enter -> {G.fpsCap} typing {G.numRow ?? "off"} (want 144 off)"); key("ArrowRight", true); };
		at[264] = () => { key("ArrowRight", false); log($"right held 1 s -> {G.fpsCap} (want well above 145)"); G.OnKeyDown("KeyQ", false); };
		void tap(string k) { G.OnKeyDown(k, false); G.OnKeyUp(k); }
		at[267] = () => { G.OnKeyUp("KeyQ"); log($"Q -> {G.fpsCap} (-10)"); G.OnKeyDown("Enter", false); };
		at[268] = () => { G.OnKeyUp("Enter"); tap("Digit9"); tap("Digit9"); tap("Enter"); };
		at[270] = () => { log($"typed 99 -> {G.fpsCap} (want 99); state {G.state}"); G.OnKeyDown("Enter", false); };
		at[271] = () => { G.OnKeyUp("Enter"); tap("Digit5"); tap("Escape"); };
		at[273] = () => { log($"Esc cancels -> {G.fpsCap} (want 99) state {G.state}"); G.setSel = 0; G.OnKeyDown("Enter", false); };
		at[274] = () => { G.OnKeyUp("Enter"); tap("Digit5"); tap("Digit0"); };
		shots[275] = "m7-typing-vol";
		at[276] = () => tap("Enter");
		at[278] = () => log($"typed volume 50 -> {G.vol} (want 0.5)");
		shots[290] = "m7b-settings-after";
		at[300] = () => { G.easy = false; G.startLevel(G.LEVELS[0], "levels"); };
		at[560] = () => { key("Escape", true); }; at[562] = () => key("Escape", false);
		at[566] = () => log($"esc -> {G.state} (want paused)");
		shots[590] = "m8-pause";
		at[591] = () => { var z = find(() => G.pauseSel == 3); click(z); };
		at[594] = () => log($"pause click settings -> {G.state} (want pauseSet)");
		at[600] = () => G.mouseQ.Add((3, 600, 300));
		at[603] = () => log($"right click -> {G.state} (want paused)");
		at[610] = () =>
		{
			var L11 = G.LEVELS.Find(l => l.id == "L11"); G.stopMusic();
			G.cur = L11; G.lastRank = "A"; G.lastEz = false; G.lastGain = 140; G.hitsTotal = 2; G.deaths = 0; G.fromList = "levels";
			G.awardXp(); G.state = "win"; G.winAt = now() - 1000; G.winSlam = now() - 900; G.winHit = true;
		};
		shots[700] = "m9-win";
		at[701] = () => { var z = G.zonesNow.Find(q => q.act == "back"); click(z); };
		at[704] = () => log($"win click levels -> {G.state} (want levels)");
		shots[740] = "m10-levels";
		at[741] = () => { var z = find(() => G.lvlSel == 2); click(z); };
		at[744] = () => log($"level card click -> {G.state} (want starting/levelIntro)");
	}

	// Hardcore (v45): the menu item (open, Easy on, pressed with Easy), the list, 3 hearts, and the unlock card on the win screen
	// v46: replays saved on the PC (win / lose / pause), the "ريبلاياتي" list (main menu and pause), watching and deleting one
	void RpScene(Func<double> now)
	{
		endF = 1900; void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		G.MYRP_DIR = "user://pv_replays"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		foreach (var L in G.LEVELS.Concat(G.FRIENDS)) if (L.name != null) L.best = "S";
		at[1] = () => { G.gatePassed = true; G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 12; };
		shots[40] = "r01-main";
		at[41] = () => { G.easy = false; G.startLevel(G.LEVELS[0], "levels"); };
		at[800] = () => tap("Escape");
		for (int j = 0; j < 4; j++) at[802 + j * 2] = () => tap("ArrowDown");
		shots[811] = "r02-pause";
		at[812] = () => tap("Enter");
		shots[830] = "r03-pause-saved";
		at[831] = () => { tap("ArrowDown"); }; at[833] = () => tap("Enter");
		shots[850] = "r04-list-from-pause";
		at[851] = () => tap("Enter");
		shots[860] = "r05-list-pause-ask";
		at[861] = () => tap("Backspace");
		at[864] = () => GD.Print($"RP back from list -> {G.state} sel {G.pauseSel}");
		for (int j = 0; j < 5; j++) at[865 + j * 2] = () => tap("ArrowUp"); at[879] = () => tap("Enter");   // resume
		at[882] = () => GD.Print($"RP resumed -> {G.state}");
		at[1000] = () => { G.player.hp = 1; G.player.inv = 0; G.damage(); };
		at[1060] = () => tap("KeyT");
		shots[1080] = "r06-dead-saved";
		at[1081] = () => { G.requestStart(G.checkpoint); };
		at[1160] = () =>
		{
			G.lastRec = G.rec; G.rec = null; G.stopMusic(); G.lastRank = "A"; G.lastEz = false; G.lastGain = 0; G.state = "win"; G.winAt = now(); G.winSlam = now() + 300; G.winHit = false;
		};
		shots[1230] = "r07-win";
		at[1231] = () => tap("KeyT");
		shots[1250] = "r08-win-saved";
		at[1251] = () => { G.stopMusic(); G.player = null; G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 12; };
		at[1280] = () => tap("Enter");
		shots[1320] = "r09-list-main";
		at[1321] = () => { tap("ArrowDown"); };   // the lost one
		at[1325] = () => tap("Enter");
		shots[1410] = "r10-watching";
		for (int j = 0; j < 20; j++) at[1411 + j * 2] = () => tap("ArrowRight");
		shots[1460] = "r11-replay-end-lost";
		at[1461] = () => tap("Backspace");
		at[1463] = () => GD.Print($"RP back from replay -> {G.state}");
		shots[1500] = "r12-list-after";
		at[1501] = () => tap("KeyR");
		shots[1510] = "r13-delete-ask";
		at[1511] = () => tap("KeyR");
		shots[1540] = "r14-deleted";
		at[1541] = () => { tap("Backspace"); };
		at[1550] = () => { G.easy = false; G.startLevel(G.HCLV[0], "hcList"); };
		at[1850] = () => tap("Escape");
		shots[1860] = "r15-pause-hardcore";
	}
	// v46: the platformer's replay on the PC
	// v46 pictures for him (20:11 10-22 "وريني كل شي بالصور"): practice, "بدون الموتات", the co-op room and its replay
	void All46(Func<double> now)
	{
		void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		G.MYRP_DIR = "user://pv_replays"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(G.MYRP_DIR));
		var L = G.LEVELS.First(x => x.id == "L1");
		void die() { G.player.hp = 1; G.player.inv = 0; G.damage(); }
		at[1] = () => { G.gatePassed = true; G.pr.L = L; G.pr.lastL = L; G.pr.from = "levels"; G.pr.doc = G.botDoc(L, "n"); G.cur = L; G.fromList = "levels"; G.levelTiming(L); G.pr.t = 40; G.pr.spd = 0.75; G.pr.inf = false; G.pr.lv = 3; G.prGo(); };
		at[90] = () => tap("Escape"); shots[96] = "01-practice-pause"; at[97] = () => tap("Escape");
		at[110] = die; shots[160] = "02-practice-lose"; at[161] = () => tap("KeyT"); shots[170] = "03-practice-lose-saved";
		at[171] = () => tap("Enter"); at[540] = () => G.prFinish(); shots[590] = "04-practice-end"; at[591] = () => tap("KeyT"); shots[600] = "05-practice-end-saved";
		at[601] = () => { G.prToMenu(); G.openMyRp("main"); }; shots[780] = "06-list-practice";
		at[781] = () => tap("Enter"); shots[900] = "07-practice-replay"; at[901] = () => tap("Backspace"); at[904] = () => tap("Backspace");
		// the main mode: a death, then saved from the pause; watched with the box off, then ticked
		at[1615] = () => { G.state = "main"; var c = System.Environment.GetEnvironmentVariable("MG_CUT_FILE"); if (c != null) File.Copy(c, ProjectSettings.GlobalizePath(G.MYRP_DIR + "/1761170000000_L1.json"), true); G.openMyRp("main"); }; at[1700] = () => tap("ArrowDown"); at[1702] = () => tap("ArrowDown"); at[1800] = () => tap("Enter");
		at[1850] = () => G.rpSeekTo(G.rp.tot - 8); shots[1880] = "08-replay-box-off"; at[1890] = () => tap("KeyT"); shots[1896] = "09-replay-box-on"; at[1900] = () => tap("Backspace"); at[1903] = () => tap("Backspace");
		// the co-op level in a room (this PC alone, the maker), on its last page
		var P = G.PLATS.First(p => p.team != null);
		at[1920] = () => { G.tnPage0 = P.team.pages.Count - 1; G.net = new G.NetRun { team = true, host = true, seat = 0, L = P }; G.net.nm[0] = "Mohammed"; G.net.nm[1] = "azm511"; G.net.nm[2] = "Link"; for (int i = 0; i < G.TM_N; i++) G.net.hi[i] = true;
			typeof(G).GetMethod("tnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { now() + 200 }); };
		at[1980] = () => tap("Escape"); shots[1986] = "10-coop-pause"; at[1987] = () => tap("Escape");
		at[1990] = () => { G.tm.won = true; G.tm.wonAt = G.tm.t - 8; }; shots[2000] = "11-coop-win"; at[2001] = () => tap("KeyT"); shots[2010] = "12-coop-win-saved"; at[2011] = () => tap("Backspace");
		at[2020] = () => { G.tnPage0 = 0; File.Copy(System.Environment.GetEnvironmentVariable("MG_RPT_FILE"), ProjectSettings.GlobalizePath(G.MYRP_DIR + "/1761160000000_P2.json"), true); };
		at[2180] = () => G.openMyRp("main"); shots[2195] = "13-list-all";
		for (int j = 0; j < 4; j++) at[2196 + j * 2] = () => tap("ArrowDown"); at[2206] = () => tap("Enter");
		at[2260] = () => tap("KeyT"); at[2263] = () => G.rpSeekTo(double.Parse(System.Environment.GetEnvironmentVariable("MG_TM_AT") ?? "60")); shots[2275] = "14-coop-replay-box-on"; at[2276] = () => tap("KeyT"); shots[2282] = "15-coop-replay-box-off";
		endF = 2290;
	}
	// v49 release check: what's new, the modes, the new levels' cards, boss menu, settings -> extras -> ratings, and a few seconds in each new level
	void V49Tour(Func<double> now)
	{
		void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		int f = 1;
		at[f] = () => { G.gatePassed = true; localStorage.setItem("mg_news", "48"); G.goMain(now()); }; shots[f += 120] = "00-news";
		at[++f] = () => { G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 0; }; shots[f += 60] = "01-main";
		for (int m = 0; m < 3; m++) { int mm = m; at[++f] = () => { G.state = "modes"; G.modeSel = mm; }; shots[f += 40] = "02-modes-" + m; }
		foreach (var id in new[] { "L15", "L16", "L17", "L18", "L19", "L20" }) { string ii = id; at[++f] = () => { G.state = "levels"; G.lvlSel = G.LEVELS.FindIndex(x => x.id == ii); }; shots[f += 50] = "03-levels-" + id; }
		foreach (var id in new[] { "F9", "F10", "F11" }) { string ii = id; at[++f] = () => { G.state = "friends"; G.friSel = G.FRIENDS.FindIndex(x => x.id == ii); }; shots[f += 50] = "04-friends-" + id; }
		at[++f] = () => { G.state = "bossMenu"; G.bossSel = 0; }; shots[f += 50] = "05-boss-menu";
		at[++f] = () => { G.state = "hcList"; }; shots[f += 50] = "06-hardcore-list";
		at[++f] = () => { G.state = "settings"; G.setSel = 0; }; shots[f += 50] = "07-settings";
		at[++f] = () => { G.state = "extras"; }; shots[f += 50] = "08-extras";
		at[++f] = () => G.openRatings(); shots[f += 80] = "09-ratings";
		at[++f] = () => { G.state = "board"; G.board.mode = "n"; }; shots[f += 60] = "10-board";
		at[++f] = () => { G.state = "chal"; }; shots[f += 40] = "11-chal";
		at[++f] = () => { G.state = "points"; }; shots[f += 40] = "12-points";
		foreach (var id in new[] { "L16", "L17", "L18", "L19", "L20", "F10", "F11", "B1" })
		{
			string ii = id;
			at[++f] = () => { G.easy = false; var L = G.ALL_LV().Concat(G.HCLV).First(x => x.id == ii); G.startLevel(L, ii[0] == 'F' ? "friends" : "levels"); };
			shots[f += 90] = "13-" + id + "-intro"; shots[f += 400] = "14-" + id + "-play";
			at[++f] = () => tap("Escape"); shots[f += 10] = "15-" + id + "-pause"; at[++f] = () => { G.state = "main"; G.stopMusic(); };
			f += 20;
		}
		endF = f + 10;
	}
	// the radio level for two: its card on the list, the room, the start, the pause, and its replay (MG_RPT_FILE from TeamTest RPT=1)
	void RadioScene(Func<double> now)
	{
		void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		G.MYRP_DIR = "user://pv_replays"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(G.MYRP_DIR));
		var P = G.PLATS.First(p => p.id == "P3");
		at[1] = () => { G.gatePassed = true; G.state = "pltList"; G.plSel = G.PLATS.IndexOf(P); };
		shots[40] = "r1-list";
		at[41] = () => { G.net = new G.NetRun { team = true, host = true, seat = 0, L = P, phase = "wait", code = "48213" }; G.net.nm[0] = "Mohammed"; G.net.hi[0] = true; G.state = "pltRoom"; };
		shots[60] = "r2-room-wait";
		at[61] = () => { G.net.nm[1] = "azm511"; G.net.hi[1] = true; G.net.phase = "lobby"; };
		shots[80] = "r3-room-ready";
		at[81] = () => { G.tnPage0 = P.team.pages.Count - 1; typeof(G).GetMethod("tnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { now() + 1500 }); };
		shots[100] = "r4-start";
		at[200] = () => tap("Escape"); shots[206] = "r5-pause"; at[207] = () => tap("Escape");
		at[220] = () => { G.netEndPub(); G.tnPage0 = 0; var f = System.Environment.GetEnvironmentVariable("MG_RPT_FILE"); if (f != null) File.Copy(f, ProjectSettings.GlobalizePath(G.MYRP_DIR + "/1761160000000_P3.json"), true); };
		at[240] = () => { G.state = "main"; G.openMyRp("main"); }; shots[260] = "r6-replays";
		at[261] = () => tap("Enter");
		var ticks = (System.Environment.GetEnvironmentVariable("MG_RPT_TICKS") ?? "300,900,1500,1790").Split(',').Select(int.Parse).ToArray();
		int f0 = 330;
		foreach (var k in ticks) { int kk = k; at[f0] = () => G.rpSeekTo(kk / 30.0 - 3 / 60.0); shots[f0 + 4] = $"r7-replay-{k}"; f0 += 12; }
		endF = f0 + 4;
	}
	// v46: the co-op level's replay (a file made by test/TeamBot.cs RPT=1): the list, watching, jumps to MG_RPT_TICKS, the end
	void RptScene(Func<double> now)
	{
		void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		G.MYRP_DIR = "user://pv_replays"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(G.MYRP_DIR));
		File.Copy(System.Environment.GetEnvironmentVariable("MG_RPT_FILE"), ProjectSettings.GlobalizePath(G.MYRP_DIR + "/1761160000000_P2.json"), true);
		var ticks = (System.Environment.GetEnvironmentVariable("MG_RPT_TICKS") ?? "600").Split(',').Select(int.Parse).ToArray();
		at[1] = () => { G.gatePassed = true; G.state = "main"; G.openMyRp("main"); };
		shots[20] = "t1-list";
		at[21] = () => tap("Enter");
		shots[70] = "t2-start";
		int f0 = 80;
		foreach (var k in ticks) { int kk = k; at[f0] = () => G.rpSeekTo(kk / 30.0 - 3 / 60.0); shots[f0 + 3] = $"t3-tick{k}"; f0 += 10; }
		at[f0] = () => G.rpSeekTo(G.rp.tot - 0.5); shots[f0 + 80] = "t4-end"; at[f0 + 81] = () => GD.Print($"RPT end state {G.state}");
		at[f0 + 82] = () => tap("Backspace"); at[f0 + 86] = () => GD.Print($"RPT back {G.state} tm {(G.tm == null ? "null" : "set")} me {G.tmMe} view {G.tmView}");
		endF = f0 + 90;
	}
	void RppScene(Func<double> now)
	{
		endF = 1500; void tap(string k) { G.OnKeyDown(k, false); rel.Add(k); }
		G.MYRP_DIR = "user://pv_replays"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		at[1] = () => { G.gatePassed = true; G.plStart(G.PLATS[0]); };
		for (int f = 2; f < 400; f++) { int ff = f; at[f] = () => { if (G.player != null) G.player.inv = 1e9; if (ff % 120 == 2) { G.OnKeyUp("ArrowLeft"); G.OnKeyDown("ArrowRight", false); } if (ff % 120 == 80) { G.OnKeyUp("ArrowRight"); G.OnKeyDown("ArrowLeft", false); } if (ff % 30 == 5) tap("Space"); }; }
		at[400] = () => { G.OnKeyUp("ArrowRight"); G.OnKeyUp("ArrowLeft"); G.player.inv = 0; typeof(G).GetMethod("plDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null); };
		shots[460] = "p1-dead";
		at[461] = () => tap("Enter");
		for (int f = 470; f < 640; f++) { int ff = f; at[f] = () => { if (G.player != null) G.player.inv = 1e9; if (ff == 470) G.OnKeyDown("ArrowRight", false); if (ff % 30 == 5) tap("Space"); }; }
		at[640] = () => { G.OnKeyUp("ArrowRight"); tap("Escape"); };
		for (int j = 0; j < 4; j++) at[643 + j * 2] = () => tap("ArrowDown");
		shots[652] = "p2-pause";
		at[653] = () => tap("Enter");
		shots[670] = "p3-saved";
		at[671] = () => tap("ArrowDown"); at[673] = () => tap("Enter");
		at[680] = () => tap("Enter"); at[683] = () => tap("Enter");
		shots[760] = "p4-watching";
		shots[900] = "p5-watching2";
		at[901] = () => { for (int j = 0; j < 1; j++) tap("ArrowRight"); };
		shots[1100] = "p6-end";
	}
	void HcScene(Func<double> now)
	{
		endF = 1300; Directory.CreateDirectory(Path.Combine(outDir, "w")); bool fake = System.Environment.GetEnvironmentVariable("MG_HC_FAKE") == "1";
		void tap(string k) { G.OnKeyDown(k, false); G.OnKeyUp(k); }
		if (fake && G.HCLV.Count == 0)
		{
			var L12 = G.LEVELS.Find(l => l.id == "L12"); var F = G.LoadDodge("L12"); F.id = "H1"; F.hc = true; F.name = "اسم المرحلة"; F.song = "Song Name"; F.buf = L12.buf; G.HCLV.Add(F);
		}
		at[1] = () => { G.gatePassed = true; G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 0; GD.Print($"HC xp {G.xp} lv {G.xpProg(G.xp).lv} open {G.hcOpen()}"); };
		shots[40] = "h1-main";
		at[41] = () => G.menuSel = 11;
		shots[70] = "h2-main-selected";
		at[71] = () => { G.setEasy(true); G.mainAt = now() - 3000; };
		shots[110] = "h3-main-easy";
		at[111] = () => key("Enter", true); at[113] = () => key("Enter", false);
		at[114] = () => GD.Print($"HC easy press -> {G.state} (want main)");
		shots[116] = "h4-main-easy-pressed";
		at[160] = () => { G.setEasy(false); G.mainAt = now() - 3000; G.menuSel = 11; };
		at[200] = () => key("Enter", true); at[202] = () => key("Enter", false);
		at[203] = () => GD.Print($"HC press -> {G.state} (want hcList)");
		shots[240] = "h5-list";
		at[241] = () => { if (G.HCLV.Count > 0) { G.hcSel = 0; key("Enter", true); } }; at[243] = () => key("Enter", false);
		at[700] = () => GD.Print($"HC start -> {G.state} hp {G.player?.hp} (want 3)");
		shots[701] = "h6-level-3hearts";
		at[702] = () =>
		{
			G.stopMusic(); G.player = null;
			// below level 5, then a win that crosses it
			var L11 = G.LEVELS.Find(l => l.id == "L11");
			foreach (var L in G.ALL_LV()) { L.best = null; L.bestE = null; }
			foreach (var L in G.LEVELS.Concat(G.FRIENDS)) { if (L == L11 || L.name == null) continue; foreach (var r in new[] { "S", "A", "B", "C" }) { L.best = r; if (G.xp < 1800) break; L.best = null; } }
			// make sure the S on L11 crosses 1800
			GD.Print($"HC before win xp {G.xp} lv {G.xpProg(G.xp).lv}");
			G.cur = L11; G.xpMark(); L11.best = "S"; G.lastRank = "S"; G.lastEz = false; G.lastGain = 140; G.hitsTotal = 0; G.deaths = 0; G.fromList = "levels";
			G.awardXp(); G.state = "win"; G.winAt = now(); G.winSlam = now() + 300; G.winHit = false;
			GD.Print($"HC after win xp {G.xp} lv {G.lastLvFrom}->{G.lastLvTo}");
		};
		for (int f = 703; f < 1200; f++) shots[f] = "w/" + (f - 703).ToString("D4");
		at[1201] = () => key("Escape", true); at[1203] = () => key("Escape", false);
		shots[1240] = "h8-back-main-after";
		at[1241] = () => { G.state = "main"; G.mainAt = now() - 3000; G.menuSel = 0; };
		shots[1280] = "h9-main-now-open";
	}

	// Hardcore levels open in order; a locked one shows nothing and plays the radio static. Three stand-in levels (L12-L14 shapes).
	// env WAV=<file>: the game's sound too (44100 Hz stereo), for the video
	void HcOrder(Func<double> now)
	{
		endF = System.Environment.GetEnvironmentVariable("MG_HC_QUICK") == "1" ? 5 : 900; Directory.CreateDirectory(Path.Combine(outDir, "v"));
		if (G.HCLV.Count == 0)
			foreach (var (src, id, nm) in new[] { ("L12", "H1", "المرحلة الأولى (مثال)"), ("L13", "H2", "المرحلة الثانية (مثال)"), ("L14", "H3", "المرحلة الثالثة (مثال)") })
			{ var O = G.LEVELS.Find(l => l.id == src); var F = G.LoadDodge(src); F.id = id; F.hc = true; F.name = nm; F.buf = O.buf; G.HCLV.Add(F); }
		foreach (var L in G.HCLV) L.best = null;
		at[1] = () =>
		{
			// the top: every letter S (Easy too where there is Easy)
			var keep = G.ALL_LV().ToDictionary(L => L, L => (L.best, L.bestE));
			foreach (var L in G.ALL_LV()) { L.best = "S"; if (!G.noEz(L)) L.bestE = "S"; }
			GD.Print($"HC2 top xp {G.xp} level {G.xpProg(G.xp).lv} (want 15)");
			foreach (var L in G.HCLV) L.best = null;
			GD.Print($"HC2 top without Hardcore xp {G.xp} level {G.xpProg(G.xp).lv}");
			foreach (var kv in keep) { kv.Key.best = kv.Value.best; kv.Key.bestE = kv.Value.bestE; }
			G.gatePassed = true; G.state = "hcList"; G.hcSel = 0;
		};
		at[200] = () => { key("ArrowLeft", true); }; at[202] = () => key("ArrowLeft", false);
		at[380] = () => { key("ArrowLeft", true); }; at[382] = () => key("ArrowLeft", false);
		at[500] = () => { key("Enter", true); }; at[502] = () => key("Enter", false);
		at[506] = () => GD.Print($"HC2 enter on locked -> {G.state} (want hcList)");
		at[559] = () => GD.Print($"HC2 points before {G.ptsOf(false)} max {G.ptsMax(false)} easy max {G.ptsMax(true)}");
		at[561] = () => GD.Print($"HC2 points after H1 B {G.ptsOf(false)} (want +200)");
		at[560] = () => { G.HCLV[0].best = "B"; key("ArrowRight", true); }; at[562] = () => key("ArrowRight", false);   // the first one finished: the second opens
		at[740] = () => GD.Print($"HC2 unlocked {G.hcUnlocked(0)} {G.hcUnlocked(1)} {G.hcUnlocked(2)} (want True True False)");
		for (int f = 2; f < 900; f++) shots[f] = "v/" + f.ToString("D4");
	}

	// the 3 Hardcore looks: the looks screen, the challenges screen, each look moving and dashing in a level, and the win that opens one
	void HcSkins(Func<double> now)
	{
		endF = System.Environment.GetEnvironmentVariable("MG_HC_QUICK") == "1" ? 45 : 1400; Directory.CreateDirectory(Path.Combine(outDir, "v"));
		if (G.HCLV.Count == 0)
			foreach (var (src, id, nm) in new[] { ("L12", "H1", "المرحلة الأولى (مثال)"), ("L13", "H2", "المرحلة الثانية (مثال)"), ("L14", "H3", "المرحلة الثالثة (مثال)") })
			{ var O = G.LEVELS.Find(l => l.id == src); var F = G.LoadDodge(src); F.id = id; F.hc = true; F.name = nm; F.buf = O.buf; G.HCLV.Add(F); }
		foreach (var L in G.HCLV) L.best = "A";
		// first a close look: the 3 looks big, side by side
		at[1] = () => over = () =>
		{
			var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, 1280, 720);
			string[] ids = { "skull", "coffin", "teddy" };
			for (int q = 0; q < 3; q++) { double x = 1280 - 230 - q * 410; g.save(); g.translate(x, 330); g.scale(17, 17); G.skinBody(ids[q], 0.5, performance.now()); g.restore(); G.txt(G.skinName(ids[q]), x, 560, 44, "#fff", 800); }
		};
		shots[40] = "s0-big";
		at[41] = () => { over = null; G.gatePassed = true; G.state = "skins"; G.chalSel = 5; G.skinInfo = false; };
		shots[60] = "s1-skins-skull";
		at[61] = () => G.chalSel = 6; shots[100] = "s2-skins-coffin";
		at[101] = () => G.chalSel = 7; shots[140] = "s3-skins-teddy";
		at[141] = () => { G.HCLV[2].best = null; G.skinInfo = true; }; shots[170] = "s4-skin-info-locked";
		at[171] = () => { G.skinInfo = false; G.HCLV[1].best = null; G.state = "chal"; G.chalView = 4; }; shots[220] = "s5-chal-hardcore";
		at[221] = () => G.chalView = 6; shots[260] = "s6-chal-hardcore-end";
		at[261] = () => { foreach (var L in G.HCLV) L.best = "A"; G.easy = false; G.startLevel(G.LEVELS[0], "levels"); };
		string[] sk = { "skull", "coffin", "teddy" };
		for (int k2 = 0; k2 < 3; k2++)
		{
			int f0 = 700 + k2 * 160; string ss = sk[k2];
			at[f0] = () => { G.skin = ss; key("ArrowRight", true); key("ArrowUp", true); };
			at[f0 + 50] = () => key("ArrowUp", false);
			at[f0 + 60] = () => key("Space", true); at[f0 + 63] = () => key("Space", false);
			at[f0 + 80] = () => { key("ArrowRight", false); key("ArrowLeft", true); key("ArrowDown", true); };
			at[f0 + 120] = () => key("Space", true); at[f0 + 123] = () => key("Space", false);
			at[f0 + 150] = () => { key("ArrowLeft", false); key("ArrowDown", false); };
			for (int f = f0; f < f0 + 155; f++) shots[f] = $"v/{ss}-{f - f0:D3}";
		}
		at[1190] = () =>
		{
			G.stopMusic(); G.player = null; var H1 = G.HCLV[0]; H1.best = null; G.skin = "crystal";
			G.cur = H1; G.xpMark(); H1.best = "A"; G.lastRank = "A"; G.lastEz = false; G.lastGain = 350; G.hitsTotal = 3; G.deaths = 0; G.fromList = "hcList";
			G.awardXp(); G.state = "win"; G.winAt = now() - 1000; G.winSlam = now() - 900; G.winHit = true;
			GD.Print($"HCSK win: +{G.lastXp} XP, challenges {string.Join(",", G.lastChals.Select(c => c.id + " " + c.xp))}");
		};
		shots[1320] = "s7-win-skull";
	}

	static void drawZones()
	{
		var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.strokeStyle = "rgba(60,255,120,0.9)"; g.lineWidth = 2;
		foreach (var z in G.zonesNow) { g.beginPath(); g.rect(z.x, z.y, z.w, z.h); g.stroke(); }
	}
	// v51: after a death at L17's last save and L13's middle save, a few pictures (OLD=1: the way it was, without keepNew)
	Func<string> dyn;
	void CpFix(Func<double> now)
	{
		var todo = new List<(string id, double cp, double[] at)> { ("L17", 120, new[] { 121.0, 125, 130 }), ("L13", 128, new[] { 129.0, 133, 140 }) };
		bool old = System.Environment.GetEnvironmentVariable("OLD") == "1"; G.cpKeepNew = !old; int ti = -1, ai = 0;
		void next() { ti++; ai = 0; if (ti >= todo.Count) { endF = i + 2; return; } var L = G.ALL_LV().First(x => x.id == todo[ti].id); G.easy = false; G.cur = L; G.fromList = "levels"; G.levelTiming(L); if (L.buf == null) L.buf = G.LoadSong(G.SONG_FILE[L.id]); G.gatePassed = true; G.requestStart(todo[ti].cp); }
		at[1] = next; endF = 100000;
		dyn = () =>
		{
			if (ti < 0 || ti >= todo.Count) return null;
			if (G.state == "play" && G.player != null) G.player.inv = 1e9;
			if (G.state == "play" && G.BT(G.songT) >= todo[ti].at[ai]) { var n = $"{todo[ti].id}-{(old ? "old" : "new")}-b{todo[ti].at[ai]}"; if (++ai >= todo[ti].at.Length) next(); return n; }
			return null;
		};
	}
	public void Process()
	{
		if (pending != null) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, pending + ".png")); pending = null; }
		int f = ++i;
		if (f > endF)
		{
			if (wav != null)
			{
				var pcm = ((MemoryStream)wav.BaseStream).ToArray(); using var wf = new BinaryWriter(File.Create(System.Environment.GetEnvironmentVariable("WAV")));
				wf.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); wf.Write(36 + pcm.Length); wf.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); wf.Write(16); wf.Write((short)1); wf.Write((short)2);
				wf.Write(AudioCtx.SR); wf.Write(AudioCtx.SR * 4); wf.Write((short)4); wf.Write((short)16); wf.Write(System.Text.Encoding.ASCII.GetBytes("data")); wf.Write(pcm.Length); wf.Write(pcm);
			}
			GD.Print("preview done"); host.GetTree().Quit(); return;
		}
		if (wav != null) { int n = AudioCtx.SR / 60; var l = new float[n]; var r = new float[n]; AudioCtx.RenderAt(l, r, n, AudioCtx.manualTime.Value); for (int j = 0; j < n; j++) { wav.Write((short)(Math.Clamp(l[j], -1, 1) * 32767)); wav.Write((short)(Math.Clamp(r[j], -1, 1) * 32767)); } }
		foreach (var r in rel.ToList()) { rel.Remove(r); G.OnKeyUp(r); }
		if (at.TryGetValue(f, out var a)) a();
		int k = int.TryParse(System.Environment.GetEnvironmentVariable("MG_PV_HZ"), out var hz) ? hz / 60 : 1;   // 240 Hz like Mohammed's screen: 4 steps per picture frame
		for (int j = 0; j < k; j++) { AudioCtx.manualTime += 1.0 / 60 / k; performance.manual += 1000.0 / 60 / k; G.gfx.BeginFrame(); G.step(performance.manual.Value); if (showZones && j == k - 1) drawZones(); if (over != null && j == k - 1) over(); G.gfx.EndFrame(); }
		{ var dn = dyn?.Invoke(); if (dn != null) pending = dn; }
		if (shots.TryGetValue(f, out var s)) { pending = s; if (s.StartsWith("v/") && G.player != null) File.AppendAllText(Path.Combine(outDir, "pos.txt"), $"{s} {G.player.x:F1} {G.player.y:F1}\n"); }
	}
}
