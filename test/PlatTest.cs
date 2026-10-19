using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// The platformer without a person: -- --plat-run <levelId> <script.txt|-> <outDir> [fps=60] [every=1]
// The script: one line per moment, "<seconds> <keys>" with keys from L R U D (held), J (jump held), X (dash press); later lines replace earlier ones.
// Writes state.log (frame time x y state pieces hits deaths) and a PNG every `every` frames.
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
		var sk = System.Environment.GetEnvironmentVariable("SKIN"); if (sk != null) G.skin = sk;
		if (id == "menu") return;
		lines = script == "-" ? new (double, string)[0] : File.ReadAllLines(script).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0]), f.Length > 1 ? f[1].Trim() : ""); }).ToArray();
		G.plStart(G.PLATS.First(x => x.id == id));
		T = lines.Length > 0 ? lines[^1].t : 10;
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, id == "menu" ? $"menu-{i}.png" : $"f{i:D6}.png")); shot = false; }
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); host.GetTree().Quit(); return; }
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
			G.keys["Space"] = k.Contains('J'); G.keys["ShiftLeft"] = dashNow;
			AudioCtx.manualTime += 1.0 / fps; performance.manual += 1000.0 / fps;
			G.gfx.BeginFrame(); G.step(performance.manual.Value);
			var p = G.player ?? new Player();
			log.Append(i).Append(' ').Append(str(t)).Append(' ').Append(str(Math.Round(p.x, 1))).Append(' ').Append(str(Math.Round(p.y, 1))).Append(' ').Append(G.state)
				.Append(' ').Append(G.pl?.got.Count ?? 0).Append(' ').Append(G.hitsTotal).Append(' ').Append(G.deaths).Append('\n');
			if (i % every == 0) { G.gfx.EndFrame(); shot = true; return; }
		}
	}
}
