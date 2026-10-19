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
	string outDir; int i; Main host; string pending;
	readonly Dictionary<int, Action> at = new(); readonly Dictionary<int, string> shots = new();
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
		if (System.Environment.GetEnvironmentVariable("MG_PV") == "news")
		{
			endF = 200;
			at[1] = () => { G.gatePassed = true; localStorage.setItem("mg_news", "37"); G.goMain(now()); };
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
	bool showZones; int endF = 1415;
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

	static void drawZones()
	{
		var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.strokeStyle = "rgba(60,255,120,0.9)"; g.lineWidth = 2;
		foreach (var z in G.zonesNow) { g.beginPath(); g.rect(z.x, z.y, z.w, z.h); g.stroke(); }
	}
	public void Process()
	{
		if (pending != null) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, pending + ".png")); pending = null; }
		int f = ++i;
		if (f > endF) { GD.Print("preview done"); host.GetTree().Quit(); return; }
		if (at.TryGetValue(f, out var a)) a();
		int k = int.TryParse(System.Environment.GetEnvironmentVariable("MG_PV_HZ"), out var hz) ? hz / 60 : 1;   // 240 Hz like Mohammed's screen: 4 steps per picture frame
		for (int j = 0; j < k; j++) { AudioCtx.manualTime += 1.0 / 60 / k; performance.manual += 1000.0 / 60 / k; G.gfx.BeginFrame(); G.step(performance.manual.Value); if (showZones && j == k - 1) drawZones(); G.gfx.EndFrame(); }
		if (shots.TryGetValue(f, out var s)) pending = s;
	}
}
