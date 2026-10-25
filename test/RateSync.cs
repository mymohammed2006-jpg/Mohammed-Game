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

// The level rating (v49) end to end on the Firebase emulator (auth 9099, firestore 8085) with firestore-v49c.rules, pressing the
// real keys: the card after a win (main level, Hardcore, the platformer), once per level, skip, the missing-choice line, kept on the PC
// without an account, sent after logging in, and the page in the menu's settings (أشياء إضافية, تقييم المراحل).
// Usage: -- --rate-sync <outFile>
public class RateSync
{
	double t20; string outFile; Main host; StringBuilder log = new(); int step; double at; bool fails; readonly System.Diagnostics.Stopwatch wall = new();
	static readonly System.Net.Http.HttpClient http = new(); Queue<string> keysQ = new(); string held;
	public static RateSync FromArgs(string[] a, int k) => new RateSync { outFile = a[k] };
	static object Call(string m, params object[] a) => typeof(G).GetMethod(m, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, a);
	static object GetF(string f) => typeof(G).GetField(f, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null);
	void Ok(bool c, string m) { log.Append(c ? "OK     " : "FAIL   ").Append(m).Append('\n'); if (!c) fails = true; }
	Level Lv(string id) => G.ALL_LV().First(x => x.id == id);
	void Win(string id) { var L = Lv(id); G.cur = L; G.fromList = "levels"; G.state = "win"; G.lastRank = "A"; G.winAt = performance.manual.Value - 2000; G.winSlam = G.winAt + 300; G.winHit = true; }
	void Press(params string[] k) { foreach (var x in k) keysQ.Enqueue(x); }
	bool unsent() => Rated().Contains("\"skip\":false,\"sent\":false");
	string Rated() => localStorage.getItem("mg_rated") ?? "";
	JsonNode Doc(string path) { var r = http.Send(new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:8085/v1/projects/mohammed-game-f7563/databases/(default)/documents/{path}")); return r.IsSuccessStatusCode ? JsonNode.Parse(r.Content.ReadAsStringAsync().Result) : null; }
	public void Start(Main h)
	{
		host = h; localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume(); G.noMus = true;
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		foreach (var (id, r) in new[] { ("L1", "A"), ("L2", "B"), ("H1", "C"), ("P1", "S") }) { Lv(id).best = r; localStorage.setItem("mg_best_" + Lv(id).name, r); }
		performance.manual = 100000; wall.Start(); G.state = "main";
	}
	public void Process()
	{
		double s = wall.Elapsed.TotalSeconds; performance.manual = 100000 + s * 1000;
		// one key per two frames: down, then up
		if (held != null) { G.keys[held] = false; held = null; } else if (keysQ.Count > 0) { held = keysQ.Dequeue(); G.keys[held] = true; }
		G.gfx.BeginFrame(); G.step(performance.manual.Value); G.gfx.EndFrame();
		if (s > 300) { log.Append($"FAIL   stuck at step {step}\n"); fails = true; step = 99; }
		if (keysQ.Count > 0 || held != null || s - at < 1.2) return;
		if (step == 10 && (G.syncing || G.acc?.dirty == true || unsent()) && s - at < 40) return;
		at = s; File.WriteAllText(outFile, log.ToString() + $"(step {step})\n");
		switch (step++)
		{
			case 0: Win("L1"); Press("Enter"); break;
			case 1: Ok(G.rateC != null && G.rateC.L.id == "L1" && G.state == "win", "main level: Enter on the win screen opens the card"); Press("Enter"); break;
			case 2: Ok(G.rateC != null && G.rateC.row == 0 && performance.now() - G.rateC.msgAt < 3000, "Enter with nothing picked: stays, line shown, on the first row"); Press("ArrowRight", "ArrowDown", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "Enter"); break;
			case 3: Ok(G.rateC == null && G.state == "starting" && Rated().Contains("\"L1\":{\"lk\":true,\"st\":4,\"skip\":false,\"sent\":false}"), $"liked + 4 stars sent: kept on the PC (no account), then play again ({G.state} {Rated()})"); Win("L1"); Press("Enter"); break;
			case 4: Ok(G.rateC == null && G.state == "starting", "second win on the same level: no card"); Win("L2"); Press("Backspace"); break;
			case 5: Ok(G.rateC != null && G.rateC.L.id == "L2", "Backspace on the win screen opens the card too"); Press("Backspace"); break;
			case 6: Ok(G.rateC == null && G.state == "levels" && Rated().Contains("\"L2\":{\"lk\":false,\"st\":0,\"skip\":true"), $"skip: back to the list, never again for L2 ({G.state})"); Win("L2"); Press("Backspace"); break;
			case 7: Ok(G.rateC == null && G.state == "levels", "L2 after skip: no card"); Win("H1"); Press("Enter"); break;
			case 8: Press("ArrowLeft", "ArrowDown", "ArrowRight", "ArrowLeft", "Enter"); break;
			case 9:
				Ok(G.rateC == null && Rated().Contains("\"H1\":{\"lk\":false,\"st\":0,"), $"Hardcore: didn't like + 0 stars ({Rated()})");
				{
					var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"rt{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
					var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
					G.acc = new G.Acc { uid = (string)j["localId"], name = "Rater" + (System.Environment.TickCount % 1000), it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000, dirty = true };
					G.state = "main"; G.sync();
				}
				break;
			case 10:
				{
					var a = Doc($"ratings/{G.acc.uid}_L1")?["fields"]; var h = Doc($"ratings/{G.acc.uid}_H1")?["fields"]; var b = Doc($"ratings/{G.acc.uid}_L2");
					Ok(a != null && (bool)a["lk"]["booleanValue"] && (string)a["st"]["integerValue"] == "4" && h != null && !(bool)h["lk"]["booleanValue"] && (string)h["st"]["integerValue"] == "0" && b == null && !unsent(),
						$"after logging in: L1 (liked, 4) and H1 (no, 0) online, skipped L2 not sent ({Rated()})");
				}
				G.state = "settings"; G.setSel = 0; Press("ArrowUp", "Enter"); break;
			case 11: Ok(G.state == "extras", $"settings: the last row (أشياء إضافية) opens its page ({G.state})"); Press("Enter"); break;
			case 12: Ok(G.state == "ratings" && G.rateSums.Count == G.ALL_LV().Count, $"تقييم المراحل opens with every level ({G.rateSums.Count})"); break;
			case 13:
				{
					var top = G.rateSums.Take(2).Select(x => $"{x.L.id}:{x.n}/{x.likes}/{x.stars}").ToArray();
					var l1 = G.rateSums.First(x => x.L.id == "L1"); var h1 = G.rateSums.First(x => x.L.id == "H1");
					Ok(l1.n >= 1 && G.rateSums[0].avg >= l1.avg && G.rateSums.TakeWhile(x => x.n > 0).Count() == G.rateSums.Count(x => x.n > 0), $"the page counts them, best first, no ratings at the end ({string.Join(" ", top)})");
				}
				Press("ArrowDown", "ArrowDown", "Backspace"); break;
			case 14: Ok(G.state == "extras", "back to أشياء إضافية"); Press("Backspace"); break;
			case 15: Ok(G.state == "settings" && G.setSel == 6, $"back to the settings on its row ({G.state} {G.setSel})"); Press("Backspace"); break;
			case 16:
				Ok(G.state == "main", "back to the menu");
				{ var P1 = G.PLATS.First(p => p.id == "P1"); G.plStart(P1); }
				break;
			case 17: G.pl.t = 30.5; Call("plWin"); break;   // a real finish takes longer than the rules' 2 s minimum
			case 18: Press("Enter"); break;
			case 19: Ok(G.rateC != null && G.rateC.L.id == "P1" && G.state == "pltWin", $"platformer: the card on its win screen ({G.state})"); Press("ArrowRight", "ArrowDown", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "ArrowRight", "Enter"); break;
			case 20: t20 = s; Ok(G.rateC == null && G.state is "pltPlay" or "pltIntro" or "pltStart" && Rated().Contains("\"P1\":{\"lk\":true,\"st\":5"), $"platformer: 5 stars (no more than 5), then play again ({G.state})"); break;
			case 21: { var p = Doc($"ratings/{G.acc.uid}_P1")?["fields"]; if (p == null && s - t20 < 80) { step--; break; } Ok(p != null && (string)p["st"]["integerValue"] == "5", $"platformer rating online right away (account, letters saved) ({Rated()} dirty {G.acc.dirty} syncing {G.syncing} {G.netState})"); } break;
			case 22:
				// co-op online: the room ends after the win (left, or the others left): the card on the level list
				Call("plStopMusic"); G.net = new G.NetRun { team = true, host = false, code = "" }; G.tm = new G.TeamRun { L = new TeamLevel { id = "P2" }, won = true }; G.state = "tmPlay";
				Call("netEnd", "", false, null);
				Ok(G.state == "pltList" && G.rateC != null && G.rateC.L.id == "P2", $"co-op: after the room ends, the card on the list ({G.state} {G.rateC?.L.id})"); break;
			case 23: Press("Backspace"); break;
			case 24:
				Ok(G.rateC == null && G.state == "pltList", "co-op: skipped, stays on the list");
				G.net = new G.NetRun { team = true, host = false, code = "" }; G.tm = new G.TeamRun { L = new TeamLevel { id = "P3" }, won = false }; G.state = "tmPlay"; Call("netEnd", "", false, null);
				Ok(G.rateC == null, "co-op: left before winning, no card"); goto default;
			default: log.Append(fails ? "SOME FAILED\n" : "ALL OK\n"); File.WriteAllText(outFile, log.ToString()); host.GetTree().Quit(); break;
		}
	}
}
