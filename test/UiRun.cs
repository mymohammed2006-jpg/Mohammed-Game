using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MG;
using static MG.JS;

// The UI check, Godot side (tools/compare/ui/ui-driver.js is the browser side): runs the game from the gate with a fixed key script
// at exactly 60 fps (one frame per Godot frame, so screen transitions copy the real last picture), fake Firebase answers and the
// real song lengths. Writes a state line per frame, the requests sent, the song lengths (for the browser side) and PNGs.
// Usage: -- --ui-run <outDir> <tools/compare/ui folder>
public class UiRun
{
	string outDir, dir; int i; bool pendingShot, done; int shotFrame;
	HashSet<int> caps; List<(int f, string a, string arg)> steps = new(); List<(string k, int at)> ups = new();
	StringBuilder log = new(); List<string> req = new();
	JsonNode net; Main host;

	public static UiRun FromArgs(string[] a, int k) => new UiRun { outDir = a[k], dir = a[k + 1] };

	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = true;
		localStorage.setItem("mg_best_المرحلة الأولى", "A"); localStorage.setItem("mg_beste_المرحلة الأولى", "S"); localStorage.setItem("mg_best_ليش ضيعت الوفا", "S");
		performance.manual = 100000; AudioCtx.manualTime = 0; AudioCtx.resume();
		G.InitSound();
		G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.easy = localStorage.getItem("mg_easy") == "1"; G.applyTheme(G.easy); G.applyMenu();
		G.LoadBests(); G.LoadSongs();
		var dur = new JsonObject { ["menu"] = G.MENUS["normal"].buf.GetLength(), ["ez"] = G.MENUS["easy"].buf.GetLength() };
		foreach (var L in G.ALL_LV()) dur[L.id] = ((AudioStream)L.buf).GetLength();
		File.WriteAllText(Path.Combine(outDir, "durations.json"), dur.ToJsonString());
		JS.SeedRandom(12345);
		var sc = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "script.json")));
		foreach (var s in sc["steps"].AsArray()) steps.Add(((int)s[0], (string)s[1], (string)s[2]));
		caps = sc["caps"].AsArray().Select(x => (int)x).ToHashSet();
		net = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "fakenet.json")));
		G.netSend = Fake;
		G.last = performance.now();
	}

	async Task<HttpResponseMessage> Fake(HttpRequestMessage r)
	{
		string m = r.Method.Method, url = r.RequestUri.OriginalString, body = r.Content != null ? await r.Content.ReadAsStringAsync() : null;
		string auth = r.Headers.TryGetValues("Authorization", out var av) ? av.First() : null;
		req.Add(m + " " + url + (auth != null ? " auth=" + auth : "") + (body != null ? " body=" + body : ""));
		await Task.Yield();
		int st = 404; JsonNode b = JsonNode.Parse("{\"error\":{\"code\":404,\"message\":\"NOT_FOUND\",\"status\":\"NOT_FOUND\"}}");
		foreach (var rule in net["rules"].AsArray()) if ((string)rule[0] == m && url.Contains((string)rule[1])) { st = (int)rule[2]; b = rule[3]; break; }
		if (url.Contains("signInWithPassword")) { bool bad = (string)JsonNode.Parse(body)["password"] == "wrongpw1"; st = bad ? 400 : 200; b = bad ? net["signin_bad"] : net["signin_ok"]; }
		return new HttpResponseMessage((HttpStatusCode)st) { Content = new StringContent(b?.ToJsonString() ?? "null") };
	}

	bool Frame()
	{
		int f = ++i;
		foreach (var (k, at) in ups) if (at == f) G.OnKeyUp(k);
		foreach (var (sf, a, arg) in steps)
		{
			if (sf != f) continue;
			if (a == "key") { G.OnKeyDown(arg, false); ups.Add((arg, f + 3)); }
			else if (a == "type") G.tfKey(arg, false);
			else if (a == "bs") G.tfKey("", true);
			else if (a == "rpwin")
			{
				var b = new List<int>(); for (int j = 0; j < 100; j++) { b.Add(10000 + j * 37); b.Add(18000 + (j * 53) % 9000); b.Add((j % 7 == 0 ? 1 : 0) | (5 << 1)); }
				G.rec = new RecState(); G.rec.segs.Add(new RecSeg { cp = 0, seed = 12345, t0 = -0.1, b = b, n = 100 }); G.rec.segs.Add(new RecSeg { cp = 100, seed = -5, t0 = 43.2, b = b.Take(3).ToList(), n = 1 });
				G.cur = G.LEVELS[1]; G.easy = false; G.cur.best = "S"; localStorage.setItem("mg_best_" + G.cur.name, "S"); G.rpWin("S"); G.markDirty();
			}
			else if (a == "end") return false;
		}
		AudioCtx.manualTime += 1.0 / 60; performance.manual += 1000.0 / 60;
		G.gfx.BeginFrame();
		G.step(performance.manual.Value);
		G.gfx.EndFrame();
		Rec();
		return true;
	}

	static string B(bool v) => v ? "true" : "false";
	void Rec()
	{
		var P = G.prof.p; var pr = G.pr; var rp = G.rp;
		log.Append(string.Join("|", new[] { str(i), G.state, str(G.menuSel), str(G.lvlSel), str(G.friSel), str(G.setSel), str(Get<int>("accSel")), Get<string>("nameVal"), str(Get<string>("passVal").Length), Get<string>("authMsg"),
			str(G.board.idx), G.board.mode, str(G.board.sel), str(G.board.list.Count), str(G.prof.row), str(G.prof.col), P != null ? P.name : "-", str(pr.sel), str(pr.t), str(pr.spd), pr.let, B(pr.inf), str(pr.lv), B(pr.ghost),
			str(Get<int>("newsPg")), B(G.easy), G.acc != null ? G.acc.name : "-", B(G.syncing), G.netState, str(rp.seg), B(rp.paused), str(rp.rate), B(rp.hide),
			str(round(G.songT * 1000)), G.toast, B(G.toastT > 0), str(round(G.pulse * 1e6)), str(G.vol), str(Get<System.Collections.ICollection>("rpq").Count) })).Append('\n');
	}
	static T Get<T>(string name) => (T)typeof(G).GetField(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(null);

	public void Process()
	{
		if (pendingShot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{shotFrame:D6}.png")); pendingShot = false; }
		if (done)
		{
			File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); File.WriteAllText(Path.Combine(outDir, "net.log"), string.Join("\n", req) + "\n");
			GD.Print("ui-run done frames=", i); host.GetTree().Quit(); return;
		}
		if (!Frame()) { done = true; return; }
		if (caps.Contains(i)) { pendingShot = true; shotFrame = i; }
	}
}
