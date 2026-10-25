using Godot;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using MG;
using static MG.JS;

// Up to four copies of the game against the boss together (UI/BossNet.cs), on the Firebase emulator (auth 9099, firestore 8085).
// Usage: -- --boss-net <host|g1|g2|g3> <outDir>   (start each one a little after the one before, for its seat)
// The host makes the room from the boss screen and writes its code to <outDir>/../code.txt; the others type it. env NP: the players the
// host waits for; HP: hearts (a test: 1 goes down at the first hit); SCALE: game clock against the wall clock; T: wall-game seconds to
// stop at; STILL=1: this one never moves (it gets hit and waits to be brought back); EVERY: a PNG every that many frames.
// The others wander in loops and go straight to a downed square to touch it. state.log every frame, result.txt at the end.
public class BossNetTest
{
	string role, outDir; Main host; int i; bool shot; double t0, endAt = -1; StringBuilder log = new(); int step; double stepAt; string press; int pressN;
	static readonly System.Net.Http.HttpClient http = new();
	static double Env(string k, double d) => double.TryParse(System.Environment.GetEnvironmentVariable(k), out var v) ? v : d;
	readonly int NP = (int)Env("NP", 2), EVERY = (int)Env("EVERY", 60); readonly double TMAX = Env("T", 400), SCALE = Env("SCALE", 0.2);
	readonly bool STILL = System.Environment.GetEnvironmentVariable("STILL") == "1";
	readonly System.Diagnostics.Stopwatch wall = new();
	string lastEv = ""; bool moved;

	public static BossNetTest FromArgs(string[] a, int k) => new BossNetTest { role = a[k], outDir = a[k + 1] };
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume();
		if (System.Environment.GetEnvironmentVariable("SKIN") is { } sk) G.skin = sk;
		Peer.GatherMs = 1500; Peer.Relay = false; Peer.TestLagMs = Env("NET_LAG", 0) / SCALE;
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		string name = role == "host" ? "Ali" : role == "g2" ? "Noor" : role == "g3" ? "Omar" : "Sara";
		var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"{role}{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
		var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
		G.acc = new G.Acc { uid = (string)j["localId"], name = name, it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
		if (Env("HP", 0) > 0) G.bnTestHp = (int)Env("HP", 0);
		G.bnTestFrom = Env("FROM", 0);   // FROM=<beat>: start there, no scene
		G.state = "bossMenu"; G.bossSel = 0; performance.manual = 100000; t0 = 100000; G.last = t0; wall.Start();
	}
	void Press(string k) { press = k; pressN = 3; G.OnKeyDown(k, false); }
	// where to go: a downed teammate if there is one, else a slow loop of its own (each seat its own loop)
	(double x, double y) Goal(double t)
	{
		var n = G.net; var p = G.player;
		foreach (int s in Enumerable.Range(0, G.VS_N)) if (n != null && s != n.vseat && n.vplay[s] && !n.vleft[s] && n.bdown[s]) return n.bdpos[s];
		int me = n?.vseat ?? 0; double a = t * 0.9 + me * Math.PI / 2;
		return (640 + 420 * Math.Cos(a), 360 + 230 * Math.Sin(a * (1 + 0.1 * me)));
	}
	public void Process()
	{
		performance.manual = t0 + wall.Elapsed.TotalMilliseconds * SCALE;
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{i:D6}.png")); shot = false; }
		double now = performance.now(), t = (now - t0) / 1000; i++;
		if (press != null && --pressN <= 0) { G.OnKeyUp(press); press = null; }
		string codeF = Path.Combine(outDir, "..", "code.txt");
		if (press == null && now - stepAt > 150)
		{
			string s = G.state, ph = G.net?.phase;
			if (step == 0 && s == "bossMenu" && t > 1) { Press("ArrowDown"); step = role == "host" ? 1 : 10; stepAt = now; }
			else if (step == 1) { Press("Enter"); step = 2; stepAt = now; }
			else if (step == 2 && ph == "wait" && G.net.code != "") { File.WriteAllText(codeF, G.net.code); step = 3; }
			else if (step == 10) { Press("ArrowDown"); step = 11; stepAt = now; }
			else if (step == 11) { Press("Enter"); step = 12; stepAt = now; }
			else if (step == 12 && s == "pltJoin" && File.Exists(codeF))
			{
				string c = File.ReadAllText(codeF).Trim(); foreach (char ch in c) G.OnKeyDown("Digit" + ch, false);
				step = 13; stepAt = now;
			}
			else if (step == 13) { Press("Enter"); step = 14; stepAt = now; }
			else if (step == 3 && ph == "lobby" && G.net.vin.Count(b => b) >= NP && now - G.net.phaseAt > 600) { Press("Enter"); step = 4; stepAt = now; }
		}
		// in the level: toward the goal (8 directions)
		if (G.state == "play" && G.player != null)
		{
			var p = G.player; var gl = STILL ? (p.x, p.y) : Goal(G.songT); double dx = gl.Item1 - p.x, dy = gl.Item2 - p.y;
			G.keys["ArrowLeft"] = dx < -8; G.keys["ArrowRight"] = dx > 8; G.keys["ArrowUp"] = dy < -8; G.keys["ArrowDown"] = dy > 8;
		}
		else if (moved) foreach (var kk in new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" }) G.keys[kk] = false;
		moved = G.state == "play" && G.player != null;
		if (G.state == "win" && endAt < 0) endAt = now;
		var n = G.net;
		bool done = t > TMAX || (endAt > 0 && now - endAt > 8000) || (step >= 3 && G.state == "bossMenu" && t > 20);
		AudioCtx.manualTime = (now - t0) / 1000;
		bool draw = i % EVERY == 0 || done || G.state is "win" or "bnWipe";
		G.gfx.BeginFrame(); G.step(now, !draw);
		var q = G.player;
		string ev = n == null ? "" : $"down={string.Join("", n.bdown.Select(b => b ? 1 : 0))} me={(G.bnMeDown ? 1 : 0)} hits={G.hitsTotal} deaths={G.deaths} fin={string.Join(",", n.bfin.Select(x => x ?? "-"))}";
		log.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(n?.phase ?? "-").Append(' ').Append(str(Math.Round(G.songT, 2))).Append(' ')
			.Append(q != null ? str(Math.Round(q.x)) + "," + str(Math.Round(q.y)) + " hp" + q.hp : "-").Append(' ').Append(ev).Append('\n');
		if (ev != lastEv) { lastEv = ev; File.AppendAllText(Path.Combine(outDir, "events.txt"), $"{str(Math.Round(t, 2))} song {str(Math.Round(G.songT, 2))} {G.state} {ev}\n"); }
		if (draw && (i % EVERY == 0 || G.state is "win" && now - endAt > 3000 && now - endAt < 3100)) shot = true;
		G.gfx.EndFrame();
		if (done)
		{
			File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString());
			File.WriteAllText(Path.Combine(outDir, "result.txt"), $"seat {n?.vseat} state {G.state} rank {G.lastRank} hits {G.hitsTotal} deaths {G.deaths} fin {(n != null ? string.Join(",", n.bfin.Select(x => x ?? "-")) : "")}\n");
			host.GetTree().Quit();
		}
	}
}
