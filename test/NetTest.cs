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
// Versus for 2 to 4 (v46): roles host / guest / g2 / g3 (start each a little later than the one before for its seat); env NP = the
// players the host waits for before it starts; FIN = play seconds at which this one is put through the gate (no route finishes P1);
// LEAVE = play seconds at which this one leaves from the pause; BEST=0 = this one never finished the level (versus stays locked);
// T = wall-game seconds to stop at (default 40). At the end: result.txt (the places, won / lost, the times).
// v49 PZ = "sec:item[:yes]" at play second sec: the pause, down to item, Enter (and Enter again for "متأكد؟" when yes = 1);
// WAITRE = sec: through the gate at FIN, then the maker's restart for everyone (R, then Enter) sec game seconds later.
public class NetTest
{
	string role, outDir, route; Main host; int i; bool shot; double t0, playAt = -1, endAt = -1; StringBuilder log = new();
	(double t, string k)[] lines; string lastK = ""; int step; double stepAt; string press; int pressN;

	public static NetTest FromArgs(string[] a, int k) => new NetTest { role = a[k], outDir = a[k + 1], route = a[k + 2] };
	static readonly System.Net.Http.HttpClient http = new();
#pragma warning disable SIPSORCERY001
	SIPSorcery.Net.TurnServer turn;
#pragma warning restore SIPSORCERY001
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;   // the music clock follows the wall clock (no sound card here)
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume();
		if (System.Environment.GetEnvironmentVariable("SKIN") is { } sk) G.skin = sk;
		Peer.GatherMs = 1500; Peer.Relay = false;   // no STUN answer or relay in the container
		// RELAYTEST=1: only through a TURN relay (SIPSorcery's own small one, started by the host on 127.0.0.1:3478)
		if (System.Environment.GetEnvironmentVariable("RELAYTEST") == "1")
		{
			Peer.ICE = new() { new SIPSorcery.Net.RTCIceServer { urls = "turn:127.0.0.1:3478", username = "mg", credential = "mgpass", credentialType = SIPSorcery.Net.RTCIceCredentialType.password } }; Peer.RelayOnly = true;
#pragma warning disable SIPSORCERY001
			if (role == "host") { turn = new SIPSorcery.Net.TurnServer(new SIPSorcery.Net.TurnServerConfig { ListenAddress = System.Net.IPAddress.Loopback, Port = 3478, EnableUdp = true, EnableTcp = true, RelayAddress = System.Net.IPAddress.Loopback, Username = "mg", Password = "mgpass", Realm = "mg" }); turn.Start(); }
#pragma warning restore SIPSORCERY001
		}
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		string name = role == "host" ? "Ali" : role == "g2" ? "Noor" : role == "g3" ? "Omar" : "Sara";
		if (System.Environment.GetEnvironmentVariable("BEST") != "0") foreach (var P in G.PLATS) if (!P.coop) P.lv.best = "A";
		var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"{role}{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
		var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
		G.acc = new G.Acc { uid = (string)j["localId"], name = name, it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
		lines = File.ReadAllLines(route).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0]), f.Length > 1 ? f[1].Trim() : ""); }).ToArray();
		G.state = "pltList"; G.plSel = int.TryParse(System.Environment.GetEnvironmentVariable("LV"), out var lvi) ? lvi : 0; performance.manual = 100000; t0 = 100000; G.last = t0; wall.Start();
	}
	static double Env(string k, double d) => double.TryParse(System.Environment.GetEnvironmentVariable(k), out var v) ? v : d;
	readonly int NP = (int)Env("NP", 2); readonly double FIN = Env("FIN", -1), LEAVE = Env("LEAVE", -1), TMAX = Env("T", 40); bool finDone; int leaveStep;
	readonly string PZ = System.Environment.GetEnvironmentVariable("PZ") is { Length: > 0 } pz ? pz : null; int pzStep; readonly double WAITRE = Env("WAITRE", -1); int wrStep; double waitAt = -1;
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
			else if (step == 1 && s == "pltOn") { if (role != "host") { Press("ArrowDown"); step = 10; } else { Press("Enter"); step = 2; } stepAt = now; }
			else if (step == 10) { Press("Enter"); step = 11; stepAt = now; }
			else if (step == 2 && ph == "wait") { File.WriteAllText(Path.Combine(outDir, "..", "code.txt"), G.net.code); step = 3; }
			else if (step == 11 && s == "pltJoin" && File.Exists(Path.Combine(outDir, "..", "code.txt")))
			{
				string c = File.ReadAllText(Path.Combine(outDir, "..", "code.txt")).Trim(); foreach (char ch in c) G.OnKeyDown("Digit" + ch, false);
				step = 12; stepAt = now;
			}
			else if (step == 12) { Press("Enter"); step = 13; stepAt = now; }
			else if (step == 3 && ph == "lobby" && (G.net.vs ? G.net.vin.Count(b => b) >= NP : G.net.hello) && now - G.net.phaseAt > 600) { Press("Enter"); step = 4; stepAt = now; }
		}
		if (G.state == "pltPlay" && playAt < 0) playAt = now;
		if (playAt >= 0 && FIN > 0 && !finDone && G.state == "pltPlay" && G.pl.t >= FIN) { finDone = true; typeof(G).GetMethod("netWin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null); }
		if (playAt >= 0 && LEAVE > 0 && leaveStep == 0 && G.state == "pltPlay" && G.pl.t >= LEAVE && press == null) { Press("Escape"); leaveStep = 1; stepAt = now; }
		else if (leaveStep == 1 && press == null && now - stepAt > 150) { Press("ArrowUp"); leaveStep = 2; stepAt = now; }
		else if (leaveStep == 2 && press == null && now - stepAt > 150) { Press("Enter"); leaveStep = 3; }
		if (PZ != null && press == null && now - stepAt > 150)
		{
			var f = PZ.Split(':'); double at = double.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture); int item = int.Parse(f[1]); bool yes = f.Length > 2 && f[2] == "1";
			if (pzStep == 0 && G.state == "pltPlay" && G.pl.t >= at) { Press("Escape"); pzStep = 1; stepAt = now; File.AppendAllText(Path.Combine(outDir, "events.txt"), $"pause at {str(G.pl.t)}\n"); }
			else if (pzStep >= 1 && pzStep <= item) { Press("ArrowDown"); pzStep++; stepAt = now; }
			else if (pzStep == item + 1) { Press("Enter"); pzStep++; stepAt = now; File.AppendAllText(Path.Combine(outDir, "events.txt"), $"picked {item} ask {G.hostAsk} state {G.state}\n"); }
			else if (pzStep == item + 2 && yes) { Press("Enter"); pzStep++; stepAt = now; }
			else if (pzStep == item + 3 && G.state == "pltPause") { Press("Escape"); pzStep++; stepAt = now; }
		}
		if (WAITRE > 0 && press == null && now - stepAt > 150)
		{
			if (G.state == "pltVsWait" && waitAt < 0) waitAt = now;
			if (wrStep == 0 && G.state == "pltVsWait" && now - waitAt >= WAITRE * 1000) { Press("KeyR"); wrStep = 1; stepAt = now; File.AppendAllText(Path.Combine(outDir, "events.txt"), $"wait restart at {str(G.pl.t)}\n"); }
			else if (wrStep == 1) { File.AppendAllText(Path.Combine(outDir, "events.txt"), $"ask {G.hostAsk}\n"); Press("Enter"); wrStep = 2; stepAt = now; }
		}
		if (G.state == "pltVsEnd" && endAt < 0)
		{
			endAt = now; var n = G.net;
			if (n?.vs == true) File.WriteAllText(Path.Combine(outDir, "result.txt"), $"me {n.vseat} won {n.iWon}\norder {string.Join(",", n.vres)}\nwon {string.Join(",", n.vwon)}\nfin {string.Join(",", n.vfin.Select(x => str(x)))}\nleft {string.Join(",", n.vleft)}\nnames {string.Join(",", n.vnm)}\n");
		}
		// the route, held keys like PlatTest
		if (playAt >= 0 && G.state is "pltPlay" or "pltDead")
		{
			double rt = (now - playAt) / 1000; string k = ""; foreach (var l in lines) if (l.t <= rt + 1e-9) k = l.k;
			bool dashNow = k.Contains('X') && !lastK.Contains('X'); lastK = k;
			G.keys["ArrowLeft"] = k.Contains('L'); G.keys["ArrowRight"] = k.Contains('R'); G.keys["ArrowUp"] = k.Contains('U'); G.keys["ArrowDown"] = k.Contains('D');
			G.keys["Space"] = k.Contains('J'); G.keys["ShiftLeft"] = dashNow; G.keys["Enter"] = k.Contains('E');
		}
		else if (G.state is "pltVsEnd" or "pltPause" or "pltVsWait") foreach (var kk in new[] { "ArrowLeft", "ArrowRight", "Space", "ShiftLeft" }) G.keys[kk] = false;   // a held jump (Space) is a held confirm in the menus
		// after the result: the guest leaves, the host sees it on its list
		if (endAt > 0 && role == "guest" && NP <= 2 && now - endAt > 3000 && G.state == "pltVsEnd" && press == null) Press("Backspace");
		bool done = t > TMAX || (endAt > 0 && now - endAt > 6000 && G.state == "pltList") || (endAt > 0 && now - endAt > 9000) || (leaveStep == 3 && G.state == "pltList" && now - stepAt > 3000);
		AudioCtx.manualTime = (now - t0) / 1000; G.gfx.BeginFrame(); G.step(now);
		var p = G.player;
		log.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(G.net?.phase ?? "-").Append(' ')
			.Append(p != null ? str(Math.Round(p.x)) + "," + str(Math.Round(p.y)) : "-").Append(' ').Append(G.net != null ? str(Math.Round(G.net.rtt)) : "-").Append(' ').Append(G.net?.vs == true ? G.net.vsn.Sum(l => l.Count) : G.net?.snaps.Count ?? 0).Append(' ').Append(p?.hp ?? -1).Append(G.pl?.down == true ? " down" : "").Append(G.pl?.atGate == true ? " gate" : "")
			.Append(G.pl != null ? $" t={str(Math.Round(G.pl.t, 2))} ps={G.pauseSel} ask={G.hostAsk} ent={(G.keys.TryGetValue("Enter", out var ek) && ek ? 1 : 0)} got={G.pl.got.Count} d={G.deaths} vm={G.net?.vm} fin={(G.net?.vs == true ? string.Join("/", G.net.vfin.Select(x => str(x))) : "")} msg={(G.pl.msgT > 0 ? G.pl.msg?.Replace(' ', '_') : "")}" : "").Append('\n');
		if (i % 10 == 0) shot = true;
		G.gfx.EndFrame();
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); host.GetTree().Quit(); }
	}
}
