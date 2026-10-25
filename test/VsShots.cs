using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MG;
using static MG.JS;

// Pictures of the v46 platformer-time and versus screens, with made-up players (nothing goes online).
// Usage: -- --vs-shots <outDir>   (env SHOTS = a comma list of: win, board, room, wait, end, endlose, lock; default all; env VSK, SEAT: the looks, this PC's seat)
public class VsShots
{
	string outDir; Main host; int i; List<(string name, Action set, int frames)> todo = new(); int cur = -1, left; bool shot;
	public static VsShots FromArgs(string[] a, int k) => new VsShots { outDir = a[k] };
	static object Call(string m, params object[] a) => typeof(G).GetMethod(m, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, a);
	static void SetF(string f, object v) => typeof(G).GetField(f, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).SetValue(null, v);
	static G.PlayerDoc P(string name, string r, double t, int xp = 500) => new G.PlayerDoc { uid = name, name = name, xp = xp, n = new() { ["P1"] = r }, e = new(), nt = new() { ["P1"] = "2026-10-22T10:00:00Z" }, et = new(), pt = t >= 0 ? new() { ["P1"] = t } : new() };

	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume(); G.noMus = true;
		G.acc = new G.Acc { uid = "Mohammed", name = "Mohammed", it = "x", rt = "x", exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
		performance.manual = 100000;
		var P1 = G.PLATS.First(p => !p.coop); P1.lv.best = "A"; P1.lv.bestT = 52.4;
		var want = (System.Environment.GetEnvironmentVariable("SHOTS") ?? "win,board,room,wait,end,endlose,lock").Split(',');
		void Add(string n, Action a, int f = 90) { if (want.Contains(n)) todo.Add((n, a, f)); }
		Add("win", () =>
		{
			G.acc = null; G.plStart(P1); for (int k = 0; k < 600; k++) Tick();   // ten seconds in
			SetF("deaths", 1); Call("plWin");
		});
		Add("board", () =>
		{
			G.acc = new G.Acc { uid = "Mohammed", name = "Mohammed", it = "x", rt = "x", exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
			G.board.list = new() { P("Nightmare", "S", 48.31), P("Mohammed", "S", 51.07), P("Link", "S", -1), P("Sara", "A", 39.9), P("Noor", "A", 44.02), P("Omar", "B", 61.5) };
			G.board.at = performance.now() + 1e9; G.board.busy = false; G.board.err = false; G.board.mode = "n"; G.board.sel = -1;
			G.board.idx = 2 + G.ALL_LV().FindIndex(L => L.id == "P1"); G.state = "board";
		});
		G.NetRun Fake(bool play)
		{
			var n = new G.NetRun { host = true, vs = true, vseat = 0, L = P1, code = "48213", phase = play ? "play" : "lobby" };
			string[] nm = { "Mohammed", "Nightmare", "Sara", "Noor" }; for (int s = 0; s < 4; s++) { n.vnm[s] = nm[s]; n.vin[s] = true; n.vsk[s] = ""; n.vplay[s] = true; }
			if (System.Environment.GetEnvironmentVariable("VSK") is string vk) { var a = vk.Split(','); for (int s = 0; s < 4; s++) n.vsk[s] = s < a.Length ? a[s] : ""; }   // env VSK: the four looks
			if (int.TryParse(System.Environment.GetEnvironmentVariable("SEAT"), out var me)) { n.vseat = me; G.skin = n.vsk[me]; }   // env SEAT: which one is this PC
			return n;
		}
		Add("room", () => { G.net = Fake(false); G.net.roomOpen = true; G.state = "pltRoom"; });
		Add("wait", () =>
		{
			G.net = null; G.plStart(P1); for (int k = 0; k < 600; k++) Tick();
			var n = Fake(true); G.net = n; n.vfin[1] = 9.12; n.vfin[0] = 10.05; n.myFin = 10.05; G.pl.atGate = true; G.state = "pltVsWait";
		});
		void End(bool lose)
		{
			G.net = null; G.plStart(P1); for (int k = 0; k < 300; k++) Tick();
			var n = Fake(true); G.net = n; n.vseat = lose ? 3 : 0; n.host = !lose;
			n.vfin = new[] { 31.48, 29.95, 35.2, -1 }; n.vres = new[] { 1, 0, 2, 3 }; n.vwon = new[] { true, true, true, false };
			Call("vsEndGot", System.Text.Json.Nodes.JsonNode.Parse("{\"o\":[1,0,2,3],\"wn\":[true,true,true,false],\"f\":[31.48,29.95,35.2,-1],\"l\":[false,false,false,false]}"));
		}
		Add("end", () => End(false), 150); Add("endlose", () => End(true), 150);
		Add("lock", () => { G.net = null; P1.lv.best = null; G.state = "pltList"; Call("netNote", "خلّص المرحلة لحالك أول عشان يفتح لك وضع الضد"); });
	}
	void Tick() { performance.manual += 1000.0 / 60; AudioCtx.manualTime += 1.0 / 60; G.gfx.BeginFrame(); G.step(performance.manual.Value); G.gfx.EndFrame(); }
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, todo[cur].name + ".png")); shot = false; }
		if (left <= 0) { cur++; if (cur >= todo.Count) { host.GetTree().Quit(); return; } todo[cur].set(); left = todo[cur].frames; }
		performance.manual += 1000.0 / 60; AudioCtx.manualTime += 1.0 / 60; G.gfx.BeginFrame(); G.step(performance.manual.Value);
		if (--left == 0) shot = true;
		G.gfx.EndFrame(); i++;
	}
}
