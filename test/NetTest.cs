using Godot;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using MG;
using static MG.JS;

// Two copies of the game playing online against each other, on the Firebase emulator (auth 9099, firestore 8085), in real time.
// Usage: -- --net-test <host|guest> <outDir> <route.txt>   (the route: PlatTest's script, from the moment the level starts)
// The game clock runs at SCALE (env, default 0.1) of the wall clock, so a slow test machine still plays at 60 game frames a second.
// The host writes its room code to <outDir>/../code.txt; the guest reads it there. PNG every 10 frames, state.log every frame.
public class NetTest
{
	string role, outDir, route; Main host; int i; bool shot; double t0, playAt = -1, endAt = -1; StringBuilder log = new();
	(double t, string k)[] lines; string lastK = ""; int step; double stepAt; string press; int pressN;

	public static NetTest FromArgs(string[] a, int k) => new NetTest { role = a[k], outDir = a[k + 1], route = a[k + 2] };
	static readonly System.Net.Http.HttpClient http = new();
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;   // the music clock follows the wall clock (no sound card here)
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume();
		if (System.Environment.GetEnvironmentVariable("SKIN") is { } sk) G.skin = sk;
		Peer.GatherMs = 1500; Peer.Relay = false;   // no STUN answer or relay in the container
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		string name = role == "host" ? "Ali" : "Sara";
		var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"{role}{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
		var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
		G.acc = new G.Acc { uid = (string)j["localId"], name = name, it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
		lines = File.ReadAllLines(route).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0]), f.Length > 1 ? f[1].Trim() : ""); }).ToArray();
		G.state = "pltList"; G.plSel = int.TryParse(System.Environment.GetEnvironmentVariable("LV"), out var lvi) ? lvi : 0; performance.manual = 100000; t0 = 100000; G.last = t0; wall.Start();
	}
	void Press(string k) { press = k; pressN = 3; G.OnKeyDown(k, false); }
	readonly System.Diagnostics.Stopwatch wall = new(); static readonly double SCALE = double.TryParse(System.Environment.GetEnvironmentVariable("SCALE"), out var sc) ? sc : 0.1;
	public void Process()
	{
		performance.manual = t0 + wall.Elapsed.TotalMilliseconds * SCALE;
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{i:D5}.png")); shot = false; }
		double now = performance.now(), t = (now - t0) / 1000; i++;
		if (press != null && --pressN <= 0) { G.OnKeyUp(press); press = null; }
		// the menus, one step at a time
		if (press == null && now - stepAt > 150)
		{
			string s = G.state, ph = G.net?.phase;
			if (step == 0 && s == "pltList" && t > 1) { Press("KeyR"); step++; stepAt = now; }
			else if (step == 1 && s == "pltOn") { if (role == "guest") { Press("ArrowDown"); step = 10; } else { Press("Enter"); step = 2; } stepAt = now; }
			else if (step == 10) { Press("Enter"); step = 11; stepAt = now; }
			else if (step == 2 && ph == "wait") { File.WriteAllText(Path.Combine(outDir, "..", "code.txt"), G.net.code); step = 3; }
			else if (step == 11 && s == "pltJoin" && File.Exists(Path.Combine(outDir, "..", "code.txt")))
			{
				string c = File.ReadAllText(Path.Combine(outDir, "..", "code.txt")).Trim(); foreach (char ch in c) G.OnKeyDown("Digit" + ch, false);
				step = 12; stepAt = now;
			}
			else if (step == 12) { Press("Enter"); step = 13; stepAt = now; }
			else if (step == 3 && ph == "lobby" && G.net.hello && now - G.net.phaseAt > 600) { Press("Enter"); step = 4; stepAt = now; }
		}
		if (G.state == "pltPlay" && playAt < 0) playAt = now;
		if (G.state == "pltVsEnd" && endAt < 0) endAt = now;
		// the route, held keys like PlatTest
		if (playAt >= 0 && G.state is "pltPlay" or "pltDead")
		{
			double rt = (now - playAt) / 1000; string k = ""; foreach (var l in lines) if (l.t <= rt + 1e-9) k = l.k;
			bool dashNow = k.Contains('X') && !lastK.Contains('X'); lastK = k;
			G.keys["ArrowLeft"] = k.Contains('L'); G.keys["ArrowRight"] = k.Contains('R'); G.keys["ArrowUp"] = k.Contains('U'); G.keys["ArrowDown"] = k.Contains('D');
			G.keys["Space"] = k.Contains('J'); G.keys["ShiftLeft"] = dashNow; G.keys["Enter"] = k.Contains('E');
		}
		else if (G.state == "pltVsEnd") foreach (var kk in new[] { "ArrowLeft", "ArrowRight", "Space", "ShiftLeft" }) G.keys[kk] = false;
		// after the result: the guest leaves, the host sees it on its list
		if (endAt > 0 && role == "guest" && now - endAt > 3000 && G.state == "pltVsEnd" && press == null) Press("Backspace");
		bool done = t > 40 || (endAt > 0 && now - endAt > 6000 && G.state == "pltList");
		AudioCtx.manualTime = (now - t0) / 1000; G.gfx.BeginFrame(); G.step(now);
		var p = G.player;
		log.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(G.net?.phase ?? "-").Append(' ')
			.Append(p != null ? str(Math.Round(p.x)) + "," + str(Math.Round(p.y)) : "-").Append(' ').Append(G.net != null ? str(Math.Round(G.net.rtt)) : "-").Append(' ').Append(G.net?.snaps.Count ?? 0).Append(' ').Append(p?.hp ?? -1).Append(G.pl?.down == true ? " down" : "").Append(G.pl?.atGate == true ? " gate" : "").Append('\n');
		if (i % 10 == 0) shot = true;
		G.gfx.EndFrame();
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); host.GetTree().Quit(); }
	}
}
