using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MG;
using static MG.JS;

// Pictures of the level rating card (v50) over the win screen, nothing goes online.
// Usage: -- --rate-shots <outDir>
public class RateShots
{
	string outDir; Main host; List<(string name, Action set, int frames)> todo = new(); int cur = -1, left; bool shot;
	public static RateShots FromArgs(string[] a, int k) => new RateShots { outDir = a[k] };
	static void SetF(string f, object v) => typeof(G).GetField(f, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).SetValue(null, v);

	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.kindForce = "ps"; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume(); G.noMus = true;
		performance.manual = 100000;
		var L = G.LEVELS.First(l => l.id == "L15");
		void Win(string kd)
		{
			G.kind = kd; G.cur = L; G.player = null; G.hazards = new(); G.state = "win"; G.lastRank = "A"; G.lastEz = false;
			G.winAt = performance.manual.Value - 3000; G.winSlam = G.winAt + 400; G.winHit = true; SetF("hitsTotal", 3); SetF("deaths", 0); G.lastGain = 0;
		}
		void Add(string n, Action a, int f = 40) => todo.Add((n, a, f));
		Add("1_win", () => { Win("ps"); G.rateC = null; });
		Add("2_card_new", () => { Win("ps"); G.rateC = new G.RateCard { L = L, at = performance.manual.Value }; });
		Add("3_card_liked", () => { Win("ps"); G.rateC = new G.RateCard { L = L, at = performance.manual.Value - 1000, like = 1, st = 4, row = 1 }; });
		Add("4_card_disliked", () => { Win("ps"); G.rateC = new G.RateCard { L = L, at = performance.manual.Value - 1000, like = 0, st = 0, row = 1 }; });
		Add("6_ratings", () =>
		{
			G.rateC = null; G.state = "ratings"; var r = new Random(4); G.rateSums = new();
			foreach (var l in G.ALL_LV().Take(14)) { int n = r.Next(0, 9); var s = new G.RateSum { L = l, n = n, likes = n == 0 ? 0 : r.Next(n / 2, n + 1), stars = n == 0 ? 0 : r.Next(n * 2, n * 5 + 1) }; G.rateSums.Add(s); }
			G.rateSums = G.rateSums.OrderByDescending(x => x.avg).ToList(); G.rateSel = 0;
		}, 60);
		Add("5_card_missing", () => { Win("ps"); G.rateC = new G.RateCard { L = L, at = performance.manual.Value - 1000, like = 1, row = 1, msgAt = performance.manual.Value }; });
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, todo[cur].name + ".png")); shot = false; }
		if (left <= 0) { cur++; if (cur >= todo.Count) { host.GetTree().Quit(); return; } todo[cur].set(); left = todo[cur].frames; }
		performance.manual += 1000.0 / 60; AudioCtx.manualTime += 1.0 / 60; G.gfx.BeginFrame(); G.step(performance.manual.Value);
		if (--left == 0) shot = true;
		G.gfx.EndFrame();
	}
}
