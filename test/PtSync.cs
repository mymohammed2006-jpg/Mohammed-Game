using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using MG;
using static MG.JS;

// The platformer's best times going to the player file and back (v46), on the Firebase emulator (auth 9099, firestore 8085),
// with the v46 rules. Usage: -- --pt-sync <outFile>. Each step waits 21 s of wall time (the rules' gap between two saves).
public class PtSync
{
	string outFile; Main host; StringBuilder log = new(); int step; double at; bool fails;
	readonly System.Diagnostics.Stopwatch wall = new(); static readonly System.Net.Http.HttpClient http = new();
	PlatLevel P1;
	public static PtSync FromArgs(string[] a, int k) => new PtSync { outFile = a[k] };
	static object Call(string m, params object[] a) => typeof(G).GetMethod(m, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, a);
	void Ok(bool c, string m) { log.Append(c ? "OK     " : "FAIL   ").Append(m).Append('\n'); if (!c) fails = true; }
	public void Start(Main h)
	{
		host = h; localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.LoadBests(); G.LoadXp(); G.LoadSongs();
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"pt{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
		var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
		G.acc = new G.Acc { uid = (string)j["localId"], name = "Timer", it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000, dirty = true };
		P1 = G.PLATS.First(p => p.id == "P1"); P1.lv.best = "A"; G.setBestT(P1.lv, 52.4);
		performance.manual = 100000; wall.Start(); G.state = "main"; G.sync(); at = 0;
	}
	(string r, double t) Doc()
	{
		var d = JsonNode.Parse(http.Send(new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:8085/v1/projects/mohammed-game-f7563/databases/(default)/documents/players/{G.acc.uid}") { Headers = { { "Authorization", "Bearer owner" } } }).Content.ReadAsStringAsync().Result);
		string r = (string)d?["fields"]?["n"]?["mapValue"]?["fields"]?["P1"]?["stringValue"];
		var tv = d?["fields"]?["pt"]?["mapValue"]?["fields"]?["P1"]; double t = tv?["doubleValue"] is JsonNode dv ? dv.GetValue<double>() : -1;
		return (r, t);
	}
	public void Process()
	{
		double s = wall.Elapsed.TotalSeconds; performance.manual = 100000 + s * 1000; G.gfx.BeginFrame(); G.step(performance.manual.Value); G.gfx.EndFrame();
		if (s > 600) { log.Append($"FAIL   stuck at step {step} (syncing {G.syncing}, {G.netState})\n"); fails = true; step = 99; }
		if (G.syncing || G.acc.dirty || s - at < 22) return;
		var (r, t) = Doc(); at = s; File.WriteAllText(outFile, log.ToString() + $"(step {step})\n");
		switch (step++)
		{
			case 0: Ok(r == "A" && t == 52.4, $"first save: A with 52.4 ({r} {t})"); Call("plSaveBest", P1.lv, "A", 49.0); break;
			case 1: Ok(r == "A" && t == 49 && G.lastNewT, $"same letter faster: 49 ({r} {t})"); Call("plSaveBest", P1.lv, "A", 55.0); break;
			case 2: Ok(r == "A" && t == 49 && !G.lastNewT && P1.lv.bestT == 49, $"same letter slower: stays 49 ({r} {t}, local {P1.lv.bestT})"); Call("plSaveBest", P1.lv, "S", 60.0); break;
			case 3: Ok(r == "S" && t == 60, $"better letter slower: S with 60 ({r} {t})"); P1.lv.best = "A"; G.setBestT(P1.lv, 40); G.acc.dirty = true; G.sync(); break;
			case 4: Ok(r == "S" && t == 60 && P1.lv.best == "S" && P1.lv.bestT == 60, $"another PC with A 40: takes S 60 from the account ({r} {t}, local {P1.lv.best} {P1.lv.bestT})"); G.setBestT(P1.lv, 58.5); G.acc.dirty = true; G.sync(); break;
			case 5: Ok(r == "S" && t == 58.5, $"same letter faster on this PC goes up ({r} {t})"); G.setBestT(P1.lv, 70); G.acc.dirty = true; G.sync(); break;
			case 6: Ok(P1.lv.bestT == 58.5 && t == 58.5, $"account faster than this PC: this PC takes it ({P1.lv.bestT} {t})"); Call("logout"); Ok(P1.lv.best == null && P1.lv.bestT < 0, "logout clears the time"); goto default;
			default: log.Append(fails ? "SOME FAILED\n" : "ALL OK\n"); File.WriteAllText(outFile, log.ToString()); host.GetTree().Quit(); break;
		}
	}
}
