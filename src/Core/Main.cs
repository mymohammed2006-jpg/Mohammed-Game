using Godot;
using System;
using System.Globalization;
using MG.Canvas;

namespace MG;

// The Godot node that runs everything: like the browser page around index.html (canvas, events, requestAnimationFrame).
public partial class Main : Node
{
	Gfx gfx;
	string canvasTestOut;
	string shotOut, shotState; double shotPct;   // --updater-shot <out.png> <check|download|restart> <percent>
	BotRun botRun;
	PvRun pvRun;
	UiRun uiRun;
	VsShots vsShots;
	RateShots rateShots;
	RateSync rateSync;        // --rate-sync: the level rating end to end on the Firebase emulator (test/RateSync.cs)      // --rate-shots: pictures of the level rating card (test/RateShots.cs)
	PtSync ptSync;            // --pt-sync: the platformer's best times to the account and back, on the emulator (test/PtSync.cs)          // --vs-shots: pictures of the v46 time and versus screens (test/VsShots.cs)
	BossNetTest bossNet;      // --boss-net: up to four copies against the boss online (test/BossNetTest.cs)
	NetTest netTest;          // --net-test: two copies of the game online on the Firebase emulator (test/NetTest.cs)
	TeamNetTest teamNet;      // --team-net: three copies of the game on the co-op level (test/TeamNetTest.cs)
	TeamBotNet teamBotNet;    // --team-bot-net: the same bots online, three copies of the game (test/TeamBotNet.cs, a test tool)
	TeamBot teamBot;          // --team-bot: the human-like bots on the co-op level (test/TeamBot.cs, a test tool)
	LocalTest localTest;      // --local-test: local play with made-up controllers (test/LocalTest.cs)
	TeamTest teamTest;        // --team-run: the co-op level with three players and no people (test/TeamTest.cs)
	RestartTest restartTest;
	PlatTest platTest;        // --plat-run: the platformer without a person (test/PlatTest.cs)              // --ui-run: the menus check (tools/compare/ui)
	int frame;
	Updater upd;
	string devLevel;          // --dev <id>: start that level right away (developer shortcut)
	bool devLog;              // --dev-log: print timing once a second
	double devAt, devWorst; int devFrames; double devCost;

	// v54: the memory clean-up sets itself up for a game-sized heap once, here at start; before, the first clean-up while playing
	// did that and stopped the game for a moment (tens of ms)
	static void WarmGc()
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		object keep = null;
		for (int q = 0; q < 3_000_000; q++) keep = new byte[48];
		GC.KeepAlive(keep);
		GC.Collect();
		GD.Print($"gc warm-up {sw.ElapsedMilliseconds} ms");
	}

	public override void _Ready()
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		var args = OS.GetCmdlineUserArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == "--canvas-test" && i + 1 < args.Length) canvasTestOut = args[i + 1];
			if (args[i] == "--updater-shot") { shotOut = args[i + 1]; shotState = args[i + 2]; shotPct = double.Parse(args[i + 3], CultureInfo.InvariantCulture); }
			if (args[i] == "--bot-run") botRun = BotRun.FromArgs(args, i + 1);
			if (args[i] == "--play-run") { botRun = BotRun.FromArgs(args, i + 1); botRun.play = true; }
			if (args[i] == "--restart-test") restartTest = RestartTest.FromArgs(args, i + 1);
			if (args[i] == "--plat-run") platTest = PlatTest.FromArgs(args, i + 1);
			if (args[i] == "--team-bot-net") teamBotNet = TeamBotNet.FromArgs(args, i + 1);
			if (args[i] == "--team-bot") teamBot = TeamBot.FromArgs(args, i + 1);
			if (args[i] == "--dodge") { G.DodgeTool(args, i + 1); GetTree().Quit(); return; }   // test/DodgeTool.cs
			if (args[i] == "--team-run") teamTest = TeamTest.FromArgs(args, i + 1);
			if (args[i] == "--local-test") localTest = LocalTest.FromArgs(args, i + 1);
			if (args[i] == "--net-test") netTest = NetTest.FromArgs(args, i + 1);
			if (args[i] == "--boss-net") bossNet = BossNetTest.FromArgs(args, i + 1);
			if (args[i] == "--vs-shots") vsShots = VsShots.FromArgs(args, i + 1);
			if (args[i] == "--rate-sync") rateSync = RateSync.FromArgs(args, i + 1);
			if (args[i] == "--rate-shots") rateShots = RateShots.FromArgs(args, i + 1);
			if (args[i] == "--pt-sync") ptSync = PtSync.FromArgs(args, i + 1);
			if (args[i] == "--team-net") teamNet = TeamNetTest.FromArgs(args, i + 1);
			if (args[i] == "--ui-run") uiRun = UiRun.FromArgs(args, i + 1);
			if (args[i] == "--preview") pvRun = PvRun.FromArgs(args, i + 1);
			if (args[i] == "--dev" && i + 1 < args.Length) devLevel = args[i + 1];
			if (args[i] == "--dev-log") devLog = true;
			if (args[i] == "--leveldb-test") { LevelDbTest.Run(args[i + 1], args[i + 2]); GetTree().Quit(); return; }
		}
		gfx = new Gfx();
		AddChild(gfx);
		G.gfx = gfx; G.g = gfx.g;
		WarmGc();
		if (canvasTestOut != null) return;
		if (shotOut != null) { upd = new Updater { state = shotState, percent = shotPct, shown = shotPct }; G.gfx.Resize(1280, 720); return; }
		if (botRun != null) { botRun.Start(this); return; }
		if (uiRun != null) { uiRun.Start(this); return; }
		if (restartTest != null) { restartTest.Start(this); return; }
		if (platTest != null) { platTest.Start(this); return; }
		if (teamBot != null) { teamBot.Start(this); return; }
		if (teamBotNet != null) { teamBotNet.Start(this); return; }
		if (teamTest != null) { teamTest.Start(this); return; }
		if (localTest != null) { localTest.Start(this); return; }
		if (netTest != null) { netTest.Start(this); return; }
		if (bossNet != null) { bossNet.Start(this); return; }
		if (vsShots != null) { vsShots.Start(this); return; }
		if (rateShots != null) { rateShots.Start(this); return; }
		if (rateSync != null) { rateSync.Start(this); return; }
		if (ptSync != null) { ptSync.Start(this); return; }
		if (teamNet != null) { teamNet.Start(this); return; }
		if (pvRun != null) { pvRun.Start(this); return; }

		if (!WinApp.Start()) { GetTree().Quit(); return; }
		Migrate.Run();
		AudioCtx.Init(this);
		G.InitSound();
		G.LoadDisplaySettings();
		G.easy = localStorage.getItem("mg_easy") == "1";
		G.applyTheme(G.easy); G.applyMenu();
		G.LoadBests(); G.LoadXp();
		G.LoadSongs();
		Input.JoyConnectionChanged += (dev, on) => G.OnPadConnected((int)dev, on);
		G.resize();
		G.last = performance.now();
		G.quitApp = Quit;
		// the exported game looks for an update first (not in the editor, not with --dev or --no-update)
		bool noUpdate = Array.IndexOf(args, "--no-update") >= 0 || devLevel != null;
		if (!noUpdate && (OS.HasFeature("template") || Updater.Testing))
		{
			upd = new Updater(); AddChild(upd);
			upd.onDone = () => { G.last = performance.now(); };
			upd.Start();
		}
		if (devLevel != null)
		{
			var L = G.ALL_LV().Find(x => x.id == devLevel);
			G.gatePassed = true; actx.resume(); G.startLevel(L, "levels");
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut) G.OnBlur();
		if (what == NotificationWMCloseRequest) upd?.InstallOnQuit();
		if (what == NotificationWMCloseRequest || what == NotificationPredelete) { localStorage.Flush(true); AudioCtx.Shutdown(); }
	}

	// the exit button in the main menu (window.close() in the original)
	void Quit()
	{
		upd?.InstallOnQuit();
		localStorage.Flush(true); AudioCtx.Shutdown();
		GetTree().Quit();
	}

	public override void _Input(InputEvent e)
	{
		if (restartTest != null || botRun != null || uiRun != null || pvRun != null || platTest != null || teamTest != null || teamBot != null || teamBotNet != null || netTest != null || bossNet != null || vsShots != null || rateShots != null || rateSync != null || ptSync != null || teamNet != null || localTest != null || canvasTestOut != null) return;
		if (upd != null && upd.state != "done") return;
		if (e is InputEventKey k)
		{
			string code = G.JsCode(k);
			if (k.Pressed) { G.OnKeyDown(code, k.Echo); G.usingPad = false; } else G.OnKeyUp(code);
			GetViewport().SetInputAsHandled();
		}
		else if (e is InputEventMouseMotion mm) { if (mm.Relative.LengthSquared() > 4) G.usingPad = false; G.mouseQ.Add((0, mm.Position.X, mm.Position.Y)); }
		else if (e is InputEventMouseButton mb)
		{
			int mk = mb.ButtonIndex switch { MouseButton.Left => mb.Pressed ? 1 : 2, MouseButton.Right => mb.Pressed ? 3 : -1, MouseButton.WheelUp => mb.Pressed ? 4 : -1, MouseButton.WheelDown => mb.Pressed ? 5 : -1, _ => -1 };
			if (mk >= 0) G.mouseQ.Add((mk, mb.Position.X, mb.Position.Y));
			if (!mb.Pressed) return;
			G.usingPad = false;
			G.OnPointerDown();
			// a double click toggles fullscreen, but not on something clickable
			if (mb.DoubleClick && mb.ButtonIndex == MouseButton.Left && !G.mouseOnZone(mb.Position.X, mb.Position.Y)) G.toggleFS();
		}
	}

	public override void _Process(double delta)
	{
		frame++;
		if (canvasTestOut != null)
		{
			gfx.BeginFrame(); var ct = System.Environment.GetEnvironmentVariable("MG_CTEST"); if (ct == "clip") ClipTest.Run(gfx.g); else if (ct == "s") STest.Run(gfx.g); else CanvasTest.Run(gfx.g); gfx.EndFrame();
			if (frame == 4)
			{
				gfx.game.GetTexture().GetImage().SavePng(canvasTestOut);
				GD.Print("saved ", canvasTestOut, " shadowsSkipped=", gfx.surf.shadowsSkipped);
				GetTree().Quit();
			}
			return;
		}
		if (botRun != null) { botRun.Process(); return; }
		if (shotOut != null)
		{
			gfx.BeginFrame(); upd.Draw(gfx.g, 0); gfx.EndFrame();
			if (frame == 4) { gfx.game.GetTexture().GetImage().SavePng(shotOut); GetTree().Quit(); }
			return;
		}

		if (uiRun != null) { uiRun.Process(); return; }
		if (restartTest != null) { restartTest.Process(); return; }
		if (platTest != null) { platTest.Process(); return; }
		if (teamBot != null) { teamBot.Process(); return; }
		if (teamBotNet != null) { teamBotNet.Process(); return; }
		if (teamTest != null) { teamTest.Process(); return; }
		if (localTest != null) { localTest.Process(); return; }
		if (netTest != null) { netTest.Process(); return; }
		if (bossNet != null) { bossNet.Process(); return; }
		if (vsShots != null) { vsShots.Process(); return; }
		if (rateShots != null) { rateShots.Process(); return; }
		if (rateSync != null) { rateSync.Process(); return; }
		if (ptSync != null) { ptSync.Process(); return; }
		if (teamNet != null) { teamNet.Process(); return; }
		if (pvRun != null) { pvRun.Process(); return; }
		double t0 = performance.now();
		if (upd != null)
		{
			upd.Tick();
			if (upd.state != "done")
			{
				G.ResizeIfNeeded(); gfx.BeginFrame(); upd.Draw(gfx.g, t0); gfx.EndFrame();
				return;
			}
		}
		AudioCtx.Tick();
		G.ResizeIfNeeded();
		gfx.BeginFrame();
		G.step(performance.now()); G.updateCursor();
		gfx.EndFrame();
		localStorage.Flush();
		if (devLog)
		{
			double c = performance.now() - t0; devCost += c; devWorst = Math.Max(devWorst, c); devFrames++;
			if (t0 - devAt >= 1000)
			{
				GD.Print($"[dev] {G.state} wall={t0 / 1000:F2} audio={actx.currentTime:F3} songT={G.songT:F3} fps={devFrames * 1000 / (t0 - devAt):F0} cpu avg={devCost / devFrames:F2}ms worst={devWorst:F2}ms hz={G.hazards.Count}");
				devAt = t0; devFrames = 0; devCost = 0; devWorst = 0;
			}
		}
	}
}
