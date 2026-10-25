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

// Three copies of the game playing the co-op level together, on the Firebase emulator (auth 9099, firestore 8085), in real time.
// Usage: -- --team-net <host|g1|g2> <outDir> <route.txt>   (the route: TeamTest's, ROOM blocks and all, for this player only)
// The host makes the room and writes its code to <outDir>/../code.txt; g1 and g2 type it (start g2 a little later for seat 2).
// env SCALE: the game clock's speed against the wall clock (default 0.1, this machine is slow); PAGE: the page to start on;
// DT: the step (default 1/60, like the routes were made); T: game seconds to stop at. A PNG every EVERY frames, state.log every frame.
// v49 PZ = "sec:item[:yes]" (more than one with ';'): at music second sec, the pause, down to item, Enter (and Enter again for
// "متأكد؟" when yes = 1).
public class TeamNetTest
{
	string role, outDir, route; Main host; int i, me; bool shot; double t0, endAt = -1; StringBuilder log = new();
	(int room, double t, string k)[] lines; string lastK = ""; int step; double stepAt; string press; int pressN;
	G.TeamRun lastTm; Dictionary<int, double> entry = new(); int page0;
	static readonly string LV = System.Environment.GetEnvironmentVariable("LV"); TeamTest duo;   // env LV=P3: the radio level for two (host and g1), TeamTest's routes and bot
	static readonly System.Net.Http.HttpClient http = new();
	readonly System.Diagnostics.Stopwatch wall = new();
	static double Env(string k, double d) => double.TryParse(System.Environment.GetEnvironmentVariable(k), out var v) ? v : d;
	readonly double SCALE = Env("SCALE", 0.1), T = Env("T", 400); readonly int EVERY = (int)Env("EVERY", 30);

	public static TeamNetTest FromArgs(string[] a, int k) => new TeamNetTest { role = a[k], outDir = a[k + 1], route = a[k + 2] };
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true;
		AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume();
		Peer.GatherMs = 1500; Peer.Relay = false; Peer.TestLagMs = Env("NET_LAG", 0) / SCALE;   // env NET_LAG: one way, in game ms
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
		page0 = (int)Env("PAGE", 0); G.tnPage0 = page0; G.TN_DT = Env("DT", 1.0 / 60);
		int room = page0; var o = new List<(int, double, string)>();
		foreach (var l in File.ReadAllLines(route).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")))
		{ var f = l.Trim().Split(' ', 2); if (f[0] == "ROOM") { room = int.Parse(f[1].Split('#')[0].Trim()); continue; } o.Add((room, double.Parse(f[0]), f.Length > 1 ? f[1].Split('#')[0].Trim() : "")); }
		lines = o.ToArray();
		G.tnBot = Bot;
		if (LV != null) duo = TeamTest.ForOne(route, role == "host" ? 0 : 1, page0);
		G.state = "pltList"; G.plSel = G.PLATS.FindIndex(p => p.team != null && (LV == null || p.id == LV)); performance.manual = 100000; t0 = 100000; G.last = t0; wall.Start();
	}
	// TeamTest's driver for one player: the keys at this time since the page began, "@x" walks to x, "A" dodges the lightning
	G.TIn Bot()
	{
		var tm = G.tm; me = G.tmMe;
		if (tm != lastTm) { lastTm = tm; if (!entry.ContainsKey(tm.page) || tm.page == page0) entry[tm.page] = tm.t; }
		double rt = tm.t - entry[tm.page]; string kk = "";
		if (duo != null) return duo.Input(me, tm.t, tm.page, rt);
		foreach (var l in lines) if (l.room == tm.page && l.t <= rt + 1e-9) kk = l.k;
		bool dashNow = kk.Contains('X') && !lastK.Contains('X'), jumpNow = kk.Contains('J') && !lastK.Contains('J'); lastK = kk;
		var inp = new G.TIn { x = (kk.Contains('R') ? 1 : 0) - (kk.Contains('L') ? 1 : 0), y = (kk.Contains('D') ? 1 : 0) - (kk.Contains('U') ? 1 : 0), jump = jumpNow, jumpHeld = kk.Contains('J'), dash = dashNow };
		var b = tm.b[me]; double? goal = null;
		foreach (var tok in kk.Split(' ', StringSplitOptions.RemoveEmptyEntries)) if (tok.StartsWith("@")) goal = double.Parse(tok[1..]);
		if (kk.Contains('A')) goal = Dodge(b);
		if (goal != null) { double d = goal.Value - b.x; inp.x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
		return inp;
	}
	double? Dodge(G.TBody b)
	{
		var bolt = G.tm.th.FirstOrDefault(s => s.k == "bolt"); var r = bolt?.of; if (r == null) return null;
		double bt = G.tmBeatPub(); long nb = (long)Math.Floor(bt) + (bt - Math.Floor(bt) > 0.25 ? 1 : 0);
		var hit = G.tmBolts(bolt, nb).ToHashSet(); int parts = (int)Math.Round(r.W / G.PT);
		int at = Math.Clamp((int)Math.Floor((b.x - r.X) / G.PT), 0, parts - 1);
		if (!hit.Contains(at)) return r.X + at * G.PT + G.PT / 2 + (b.i - 1) * 4;
		int best = -1; double bd = 99;
		for (int j = 0; j < parts; j++) if (!hit.Contains(j) && Math.Abs(j - at) < bd) { bd = Math.Abs(j - at); best = j; }
		return best < 0 ? null : r.X + best * G.PT + G.PT / 2;
	}
	void Press(string k) { press = k; pressN = 3; G.OnKeyDown(k, false); }
	readonly string[] PZ = System.Environment.GetEnvironmentVariable("PZ") is { Length: > 0 } pz ? pz.Split(';') : null; int pzI, pzStep;
	void Ev(string m) => File.AppendAllText(Path.Combine(outDir, "events.txt"), m + "\n");
	void PzStep(double now)
	{
		if (PZ == null || pzI >= PZ.Length || press != null || now - stepAt <= 150) return;
		var f = PZ[pzI].Split(':'); double at = double.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture); int item = int.Parse(f[1]); bool yes = f.Length > 2 && f[2] == "1";
		if (pzStep == 0 && G.state == "tmPlay" && G.tm != null && G.tm.t >= at) { Press("Escape"); pzStep = 1; stepAt = now; Ev($"pause at {str(G.tm.t)} items {string.Join("|", G.tmPauseList())}"); }
		else if (pzStep >= 1 && pzStep <= item) { Press("ArrowDown"); pzStep++; stepAt = now; }
		else if (pzStep == item + 1) { Press("Enter"); pzStep++; stepAt = now; Ev($"picked {item} ask {G.hostAsk} state {G.state}"); }
		else if (pzStep == item + 2) { if (yes) Press("Enter"); else if (G.state == "tmPause") Press("Escape"); pzStep++; stepAt = now; }
		else if (pzStep == item + 3) { Ev($"after: state {G.state} ask {G.hostAsk}"); if (G.state == "tmPause") Press("Escape"); pzI++; pzStep = 0; stepAt = now; }
	}
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
			else if (step == 3 && ph == "lobby" && G.net.hi.Take(G.net.L.team.players).All(x => x) && now - G.net.phaseAt > 1500) { Press("Enter"); step = 4; stepAt = now; }
		}
		PzStep(now);
		if (G.tm?.won == true && endAt < 0) endAt = now;
		bool done = t > T || (endAt > 0 && now - endAt > 9000) || (t > 30 && G.state == "pltList");
		AudioCtx.manualTime = (now - t0) / 1000;
		bool draw = i % EVERY == 0 || done;
		if (draw) G.gfx.BeginFrame();
		G.step(now, !draw);
		var tm = G.tm;
		log.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(G.net?.phase ?? "-");
		if (tm != null)
		{
			log.Append(" p").Append(tm.page).Append(" g").Append(tm.gen).Append(" mt=").Append(str(Math.Round(tm.t, 3)));
			foreach (var b in tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(b.down ? "d" : "").Append(b.gate ? "g" : "").Append(b.carry != null ? "c" : "");
			log.Append(" got=").Append(tm.got.Count).Append(" falls=").Append(string.Join(",", tm.falls)).Append(" r=").Append(tm.restarts);
			if (tm.th.Any(s => s.k == "slot")) log.Append(" placed=").Append(tm.th.Count(s => s.k == "slot" && s.on));
			if (tm.th.Any(s => s.k == "snap")) log.Append(" snaps=").Append(tm.th.Count(s => s.k == "snap"));
			if (tm.won) log.Append(" WON xp=").Append(string.Join(",", tm.xpGot)).Append(" best=").Append(G.PLATS.First(p => p.team == tm.L).best);
			if (!double.IsNaN(tm.riseY)) log.Append(" rise=").Append(str(Math.Round(tm.riseY))).Append(tm.ropeLive ? " R" : "");
			if (tm.spkN != null) { log.Append(" sh=").Append(tm.shield).Append(" bars=").Append(tm.bars.Count); foreach (var w in tm.bars) log.Append(" w").Append(w.dir > 0 ? '>' : '<').Append(str(Math.Round(w.x))).Append('/').Append(w.r0).Append(':').Append(string.Concat(w.on.Select(o => o ? '1' : '0'))); }
			foreach (var s in tm.th) if (s.k == "mover" && s.d.need != null) log.Append(" lift=").Append(str(Math.Round(s.Y)));
			foreach (var s in tm.th) if (s.k == "cbtn" && s.on) log.Append(' ').Append(s.d.id);
			if (tm.th.Any(s => s.k is "path" or "door")) log.Append(" paths=").Append(string.Concat(tm.th.Where(s => s.k is "path" or "door").Select(s => s.solid ? '1' : '0')));
			if (tm.doneAt >= 0) log.Append(" DONE");
			if (tm.restartAt >= 0) log.Append(" RESTART");
			log.Append(" playT=").Append(str(Math.Round(tm.playT, 2))); if (tm.msgT > 0) log.Append(" msg=").Append(tm.msg?.Replace(' ', '_'));
		}
		log.Append('\n');
		if (draw) { G.gfx.EndFrame(); shot = true; }
		if (done) { File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); host.GetTree().Quit(); }
	}
}
