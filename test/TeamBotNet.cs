using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using MG;
using static MG.JS;

// A test tool only: the human-like bots (test/TeamBot.cs) online, three copies of the game on the Firebase emulator (auth 9099,
// firestore 8085), each moving only its own square, through the real menus, room code and connections (like TeamNetTest).
//   -- --team-bot-net <host|g1|g2> <outDir> <routes.cfg>     (tools/human-bot-plat/net.sh starts the three)
// env SCALE (game clock against the wall clock, default 1), SEED/JIT/... as TeamBot, T (game seconds), EVERY (a picture every
// EVERY frames, default never), LEAVE=<song s> (this one leaves from the pause menu then) or KILL=<song s> (its game just closes).
// Writes state.log (each frame: state, page, gen, music step, everyone's place as this PC sees it) and the Crew's events/bugs.
public class TeamBotNet
{
	string role, outDir, cfg; Main host; int i, me = -1; bool shot; double t0, endAt = -1, leftAt = -1; StringBuilder log = new();
	int step; double stepAt; string press; int pressN; Crew crew; bool stepped; string endMsg;
	static readonly System.Net.Http.HttpClient http = new();
	readonly System.Diagnostics.Stopwatch wall = new();
	static string Env(string k) => System.Environment.GetEnvironmentVariable(k);
	static double D(string k, double d) => double.TryParse(Env(k), out var v) ? v : d;
	readonly double SCALE = D("SCALE", 1), T = D("T", 900), LEAVE = D("LEAVE", -1), KILL = D("KILL", -1); readonly int EVERY = (int)D("EVERY", 1e9);
	List<string> pauseQ = new();

	public static TeamBotNet FromArgs(string[] a, int k) => new TeamBotNet { role = a[k], outDir = a[k + 1], cfg = a[k + 2] };
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		if (OS.HasFeature("template")) { h.GetTree().Quit(); return; }   // never in the game players get
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume();
		Peer.GatherMs = 1500; Peer.Relay = false;
		G.netSend = r =>
		{
			string u = r.RequestUri.OriginalString.Replace("https://firestore.googleapis.com/", "http://127.0.0.1:8085/")
				.Replace("https://identitytoolkit.googleapis.com/", "http://127.0.0.1:9099/identitytoolkit.googleapis.com/").Replace("https://securetoken.googleapis.com/", "http://127.0.0.1:9099/securetoken.googleapis.com/");
			r.RequestUri = new Uri(u); return http.SendAsync(r);
		};
		string name = role == "host" ? "Ali" : role == "g1" ? "Sara" : "Noor";
		var res = http.PostAsync("http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=k", new StringContent($"{{\"email\":\"{role}{System.Environment.TickCount}@x.app\",\"password\":\"xxxxxx\",\"returnSecureToken\":true}}", Encoding.UTF8, "application/json")).Result;
		var j = JsonNode.Parse(res.Content.ReadAsStringAsync().Result);
		G.acc = new G.Acc { uid = (string)j["localId"], name = name, it = (string)j["idToken"], rt = (string)j["refreshToken"], exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 };
		G.TN_DT = D("DT", 1.0 / 60);
		G.tnBot = Bot;
		G.state = "pltList"; G.plSel = G.PLATS.FindIndex(p => p.team != null); performance.manual = 100000; t0 = 100000; G.last = t0; wall.Start();
	}
	// the bot for this PC's square: the Crew sees the whole level as this PC does and gives the buttons for its own square
	G.TIn Bot()
	{
		if (crew == null) { me = G.tmMe; crew = new Crew(G.tm.L, cfg, outDir, me); }
		if (stepped) crew.After(G.TN_DT); stepped = true;
		return crew.Step(G.TN_DT)[me];
	}
	void Press(string k) { press = k; pressN = 3; G.OnKeyDown(k, false); }
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{i:D6}.png")); shot = false; }
		performance.manual = t0 + wall.Elapsed.TotalMilliseconds * SCALE;
		double now = performance.now(), t = (now - t0) / 1000; i++;
		if (press != null && --pressN <= 0) { G.OnKeyUp(press); press = null; }
		string codeF = Path.Combine(outDir, "..", "code.txt");
		if (press == null && now - stepAt > 150)
		{
			string s = G.state, ph = G.net?.phase;
			if (step == 0 && s == "pltList" && t > 1) { Press("Enter"); step++; stepAt = now; }
			else if (step == 1 && s == "pltOn") { if (role != "host") { Press("ArrowDown"); step = 10; } else { Press("Enter"); step = 2; } stepAt = now; }
			else if (step == 10) { Press("Enter"); step = 11; stepAt = now; }
			else if (step == 2 && ph == "wait" && G.net.code != "") { File.WriteAllText(codeF, G.net.code); step = 3; }
			else if (step == 11 && s == "pltJoin" && File.Exists(codeF) && (role != "g2" || t > 4))
			{
				string c = File.ReadAllText(codeF).Trim(); foreach (char ch in c) G.OnKeyDown("Digit" + ch, false);
				step = 12; stepAt = now;
			}
			else if (step == 12) { Press("Enter"); step = 13; stepAt = now; }
			else if (step == 3 && ph == "lobby" && G.net.hi.All(x => x) && now - G.net.phaseAt > 1500) { Press("Enter"); step = 4; stepAt = now; }
			// leaving from the pause menu: Escape, down twice, Enter (اطلع من الغرفة)
			else if (LEAVE > 0 && leftAt < 0 && G.tm != null && G.state == "tmPlay" && SongClock() >= LEAVE) { leftAt = now; Press("Escape"); stepAt = now; Note($"LEAVE from the pause menu"); }
			else if (leftAt > 0 && G.state == "tmPause") { Press(PauseSel() == 2 ? "Enter" : "ArrowDown"); stepAt = now; }
		}
		if (KILL > 0 && G.tm != null && SongClock() >= KILL) { Note("KILL: this game closes without a word"); Save(); System.Environment.Exit(0); }
		if (G.tm?.won == true && endAt < 0) endAt = now;
		if (typeof(G).GetField("netMsg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) is string m && m.Length > 0) endMsg = m;
		bool done = t > T || (endAt > 0 && now - endAt > 9000) || (t > 30 && G.state == "pltList") || crew?.over == true;
		AudioCtx.manualTime = (now - t0) / 1000;
		bool draw = i % EVERY == 0;
		if (draw) G.gfx.BeginFrame();
		G.step(now, !draw);
		var tm = G.tm;
		log.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(G.net?.phase ?? "-");
		if (tm != null)
		{
			log.Append(" p").Append(tm.page).Append(" g").Append(tm.gen).Append(" n=").Append(tm.n).Append(" r=").Append(tm.restarts);
			foreach (var b in tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(b.down ? "d" : "").Append(b.gate ? "g" : "");
			log.Append(" | ").Append(crew?.Mode).Append(" got=").Append(tm.got.Count).Append(" falls=").Append(string.Join(",", tm.falls));
			if (tm.won) log.Append(" WON");
		}
		log.Append(" songs=").Append(Crew.SongVoices()).Append('\n');
		if (draw) { G.gfx.EndFrame(); shot = true; }
		if (done) { Note($"end: state={G.state} msg={endMsg}"); Save(); host.GetTree().Quit(); }
	}
	static int PauseSel() => (int)(typeof(G).GetField("pauseSel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) ?? 0);
	double SongClock() => G.tm?.t ?? 0;
	readonly StringBuilder notes = new();
	void Note(string s) => notes.Append($"[{str(Math.Round(SongClock(), 2))} p{G.tm?.page}] {s}\n");
	void Save()
	{
		File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString());
		File.WriteAllText(Path.Combine(outDir, "notes.log"), notes.ToString());
		if (crew != null) { crew.After(G.TN_DT); crew.Save(); }
	}
}
