using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// The platformer without a person: -- --plat-run <levelId> <script.txt|-> <outDir> [fps=60] [every=1]
// The script: one line per moment, "<seconds> <keys>" with keys from L R U D (held), J (jump held), X (dash press), P (Esc held), E (Enter held); later lines replace earlier ones.
// Writes state.log (frame time x y state pieces hits deaths) and a PNG every `every` frames.
// With <levelId> = flow: starts on the platformer list (press E to go in).
// With <levelId> = menu: draws the main menu, the mode screen and the platformer list (menu-*.png).
public class PlatTest
{
	string id, script, outDir; int fps = 60, every = 1, i; double T; bool done, shot;
	(double t, string k)[] lines; StringBuilder log = new(); Main host; string lastK = ""; int menuStep;

	public static PlatTest FromArgs(string[] a, int k)
	{
		var r = new PlatTest { id = a[k], script = a[k + 1], outDir = a[k + 2] };
		if (a.Length > k + 3 && int.TryParse(a[k + 3], out var f)) r.fps = f;
		if (a.Length > k + 4 && int.TryParse(a[k + 4], out var e)) r.every = e;
		return r;
	}
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false;
		performance.manual = 100000; AudioCtx.manualTime = 0; JS.SeedRandom(7);
		G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0; G.gatePassed = true;
		var sk = System.Environment.GetEnvironmentVariable("SKIN"); if (sk != null) G.skin = sk; if (System.Environment.GetEnvironmentVariable("EASY") == "1") G.setEasy(true);
		if (id == "menu") return;
		if (id == "board")
		{
			// fake online players: the P1 board page and a profile (rows down to the platformer)
			System.Collections.Generic.Dictionary<string, string> D(params string[] kv) { var d = new System.Collections.Generic.Dictionary<string, string>(); for (int j = 0; j + 1 < kv.Length; j += 2) d[kv[j]] = kv[j + 1]; return d; }
			G.board.list = new() {
				new G.PlayerDoc { uid = "a", name = "Mohammed", xp = 4200, n = D("L1", "S", "P1", "S"), e = D("L1", "A"), nt = D("L1", "2026-10-19T10:00:00Z", "P1", "2026-10-19T16:00:00Z"), et = D("L1", "2026-10-19T10:00:00Z") },
				new G.PlayerDoc { uid = "b", name = "Link", xp = 3000, n = D("L1", "A", "P1", "B"), e = D(), nt = D("L1", "2026-10-19T10:00:00Z", "P1", "2026-10-19T16:05:00Z"), et = D() } };
			G.board.at = 1e18; G.board.idx = G.ALL_LV().Count + 1; G.board.mode = "n"; G.state = "board"; G.PLATS[0].lv.best = "S";
			return;
		}
		lines = script == "-" ? new (double, string)[0] : File.ReadAllLines(script).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0]), f.Length > 1 ? f[1].Trim() : ""); }).ToArray();
		if (id == "flow") { G.state = "pltList"; G.plSel = 0; var _ = G.PLATS; }   // from the level list, as a player goes in
		else	G.plStart(G.PLATS.First(x => x.id == id), System.Environment.GetEnvironmentVariable("INTRO") == "1");
		T = lines.Length > 0 ? lines[^1].t : 10;
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, id is "menu" or "board" ? $"{id}-{i}.png" : $"f{i:D6}.png")); shot = false; }
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); host.GetTree().Quit(); return; }
		if (id == "board")
		{
			if (menuStep >= 3) { done = true; return; }
			if (menuStep == 1) { G.prof.p = G.board.list[0]; G.prof.row = G.ALL_LV().Count - 1; G.prof.sc = G.ALL_LV().Count - 11; G.prof.col = 0; G.state = "profile"; }
			if (menuStep == 2) { G.keys["ArrowLeft"] = true; G.OnKeyDown("ArrowLeft", false); }
			for (int k = 0; k < 40; k++) { performance.manual += 1000.0 / 60; G.gfx.BeginFrame(); G.draw(performance.manual.Value); G.gfx.EndFrame(); }
			G.gfx.BeginFrame(); G.draw(performance.manual.Value);
			i = menuStep++; shot = true; G.gfx.EndFrame();
			return;
		}
		if (id == "menu")
		{
			// main menu, then the mode screen (each mode selected), then the list
			string[] st = { "main", "modes", "modes", "pltList" };
			if (menuStep >= st.Length) { done = true; return; }
			G.state = st[menuStep]; G.modeSel = menuStep == 2 ? 1 : 0; G.menuSel = 0; G.mainAt = -1e6;
			for (int k = 0; k < 40; k++) { performance.manual += 1000.0 / 60; G.gfx.BeginFrame(); G.draw(performance.manual.Value); G.gfx.EndFrame(); }
			G.gfx.BeginFrame(); G.draw(performance.manual.Value);
			i = menuStep++; shot = true; G.gfx.EndFrame();
			return;
		}
		while (true)
		{
			i++; double t = (double)i / fps; if (t > T) { done = true; G.gfx.EndFrame(); return; }
			string k = ""; foreach (var l in lines) if (l.t <= t + 1e-9) k = l.k;
			bool dashNow = k.Contains('X') && !lastK.Contains('X'); lastK = k;
			G.keys["ArrowLeft"] = k.Contains('L'); G.keys["ArrowRight"] = k.Contains('R'); G.keys["ArrowUp"] = k.Contains('U'); G.keys["ArrowDown"] = k.Contains('D');
			G.keys["Space"] = k.Contains('J'); G.keys["ShiftLeft"] = dashNow; G.keys["Escape"] = k.Contains('P'); G.keys["Enter"] = k.Contains('E');
			AudioCtx.manualTime += 1.0 / fps; performance.manual += 1000.0 / fps;
			G.gfx.BeginFrame(); G.step(performance.manual.Value);
			var p = G.player ?? new Player();
			log.Append(i).Append(' ').Append(str(t)).Append(' ').Append(str(Math.Round(p.x, 1))).Append(' ').Append(str(Math.Round(p.y, 1))).Append(' ').Append(G.state)
				.Append(' ').Append(G.pl?.got.Count ?? 0).Append(' ').Append(G.hitsTotal).Append(' ').Append(G.deaths).Append('\n');
			if (i % every == 0) { G.gfx.EndFrame(); shot = true; return; }
		}
	}
}
