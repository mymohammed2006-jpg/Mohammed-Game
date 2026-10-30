using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MG;
using static MG.JS;

// Local play (UI/Local.cs) without people: -- --local-test <boss|vs|team> <levelId> <players> <outDir> [fps=60]
// Made-up PlayStation controllers (devices 10..13) join on the join screen, player 1 starts, then each one plays by a little bot:
//   boss: a loop of its own, or straight to a downed teammate;  vs: tools/net/slow.txt, each seat a bit later;  team: walks and jumps.
// A made-up Firebase in memory (accounts, player files, the 20 s gap) answers the sign-ins and the saves.
// env: T = game seconds to stop at; EVERY = a PNG every that many frames (0: none); SHOTS = "s1,s2" seconds for extra PNGs;
//   HP = the boss's hearts (test); STILL = "1,2" seats that never move; PC = 1: this PC has an account (player 1 = "Mohammed");
//   SIGN = "1:Sara,2:Newbie+" seats that sign in before the start (+ = make the account); FIN = "40,45" versus: play seconds at which
//   seat 0, 1.. are put through the gate; WIN = team: the song second the level is won; PZ = song second to pause for 2 s; KB = 1: seat 0
//   is the keyboard; PAGE = team: start page; LOBBY = 1: stop on the join screen (PNG); AUTH = 1: stop on the sign-in screen (PNG).
// Writes state.log (every frame), events.txt (changes), net.log (the requests), result.txt.
public class LocalTest
{
	string mode, id, outDir; int n, fps = 60, i; Main host; bool shot, done; string shotName;
	StringBuilder log = new(); string lastEv = ""; List<string> req = new();
	static double Env(string k, double d) => double.TryParse(System.Environment.GetEnvironmentVariable(k), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : d;
	static string EnvS(string k) => System.Environment.GetEnvironmentVariable(k);
	readonly double TMAX = Env("T", 60), PZ = Env("PZ", -1), WIN = Env("WIN", -1); readonly int EVERY = (int)Env("EVERY", 0);
	bool pzShot; HashSet<int> still = new(); List<double> shots = new(); double[] fin = Array.Empty<double>();
	(double t, string k)[] route; int signK, signAt; double pzAt = -1; bool pzDone, winDone; int step; double t0;
	readonly Dictionary<int, Dictionary<string, bool>> prev = new();
	Dictionary<int, (bool c, bool pz, bool b, bool l1, bool r1, bool up, bool down)> press = new();

	public static LocalTest FromArgs(string[] a, int k)
	{
		var r = new LocalTest { mode = a[k], id = a[k + 1], n = int.Parse(a[k + 2]), outDir = a[k + 3] };
		if (a.Length > k + 4 && int.TryParse(a[k + 4], out var f)) r.fps = f;
		return r;
	}
	int Dev(int s) => s == 0 && EnvS("KB") == "1" ? G.KB : 10 + s;
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false; G.gatePassed = true; G.kindForce = "ps";
		performance.manual = 100000; t0 = 100000; AudioCtx.manualTime = 0; G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0;
		G.applyTheme(false); G.applyMenu(); G.LoadBests(); G.LoadXp(); G.LoadSongs(); actx.resume(); JS.SeedRandom(4242);
		if (EnvS("STILL") is { } st) foreach (var x in st.Split(',', StringSplitOptions.RemoveEmptyEntries)) still.Add(int.Parse(x));
		if (EnvS("SHOTS") is { } sh) shots = sh.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToList();
		if (EnvS("FIN") is { } fi) fin = fi.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
		if (Env("HP", 0) > 0) G.bnTestHp = (int)Env("HP", 0);
		G.tnPage0 = (int)Env("PAGE", 0);
		G.lcRetryMs = k => 3000;
		route = File.ReadAllLines("tools/net/slow.txt").Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture), f.Length > 1 ? f[1].Trim() : ""); }).ToArray();
		G.netSend = Fake;
		if (EnvS("PC") == "1") { var a = SignUp("Mohammed", "pcpass1"); G.acc = new G.Acc { uid = a.uid, name = "Mohammed", it = a.tok, rt = a.tok, exp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000 }; }
		SignUp("Sara", "sarapw1"); docs["uSara"]["fields"]["n"] = JsonNode.Parse("{\"mapValue\":{\"fields\":{\"B1\":{\"stringValue\":\"C\"},\"L1\":{\"stringValue\":\"S\"}}}}");
		G.devListFake = () => Enumerable.Range(0, n).Select(Dev).Prepend(G.KB).Distinct().ToList();
		G.devFake = Fake;
		// the entry: the boss screen's 4th choice / the platformer's "أونلاين" 3rd choice, pressed on the keyboard
		if (mode == "boss") { G.bossLv = Math.Max(0, G.BOSSLV.FindIndex(b => b.id == id)); G.state = "bossMenu"; G.bossSel = 3; }
		else { G.plSel = Math.Max(0, G.PLATS.FindIndex(p => p.id == id)); G.state = "pltOn"; G.onSel = 2; }
		G.last = performance.now();
	}

	// ---------- the made-up controllers ----------
	Inp Fake(int dev)
	{
		if (dev == G.KB && EnvS("KB") != "1") return null;
		int s = dev == G.KB ? 0 : dev - 10;
		double x = 0, y = 0; bool jump = false, dashH = false;
		press.TryGetValue(dev, out var pr);
		var seat = G.lc?.seats.FirstOrDefault(q => q.dev == dev);
		if (G.lc?.playing == true && seat != null && !still.Contains(s) && !G.lcPaused()) (x, y, jump, dashH) = Bot(seat);
		var cur = new Dictionary<string, bool> { ["confirm"] = pr.c, ["pz"] = pr.pz, ["back"] = pr.b, ["l1"] = pr.l1, ["r1"] = pr.r1, ["up"] = pr.up || y < -0.5, ["down"] = pr.down || y > 0.5, ["jump"] = jump, ["dash"] = dashH, ["left"] = x < -0.5, ["right"] = x > 0.5 };
		prev.TryGetValue(dev, out var pv);
		bool E(string k) => cur[k] && !(pv != null && pv.TryGetValue(k, out var v) && v);
		var o = new Inp { x = x, y = y, confirm = E("confirm"), pz = E("pz"), back = E("back"), l1 = E("l1"), r1 = E("r1"), up = E("up"), down = E("down"), left = E("left"), right = E("right"), jump = E("jump"), jumpHeld = jump, dash = E("dash"), pdash = E("dash") };
		prev[dev] = cur; return o;
	}
	(double, double, bool, bool) Bot(G.LcSeat s)
	{
		if (G.lc.mode == "boss" && G.state == "play" && s.p != null && !s.down)
		{
			(double x, double y) gl; var d = G.lc.seats.FirstOrDefault(q => q.down && q != s);
			if (d != null) gl = (d.dx, d.dy);
			else { double a = G.songT * 0.9 + s.i * Math.PI / 2; gl = (640 + 420 * Math.Cos(a), 360 + 230 * Math.Sin(a * (1 + 0.1 * s.i))); }
			double dx = gl.x - s.p.x, dy = gl.y - s.p.y;
			return (dx < -8 ? -1 : dx > 8 ? 1 : 0, dy < -8 ? -1 : dy > 8 ? 1 : 0, false, ((int)(G.songT * 2) + s.i) % 7 == 0);
		}
		if (G.lc.mode == "vs" && G.state == "pltPlay" && s.run != null)
		{
			double t = s.run.t - s.i * 0.5; string k = ""; foreach (var (lt, lk) in route) { if (lt <= t) k = lk; else break; }
			return ((k.Contains('R') ? 1 : 0) - (k.Contains('L') ? 1 : 0), (k.Contains('D') ? 1 : 0) - (k.Contains('U') ? 1 : 0), k.Contains('J'), k.Contains('X'));
		}
		if (G.lc.mode == "team" && G.state == "tmPlay" && G.tm != null)
		{
			double t = G.tm.t; int ph = (int)Math.Floor(t / 1.5 + s.i) % 4;
			if (s.i == 0 && Env("RUN", 0) > 0) return (ph == 3 ? 0 : 1, 0, ((int)Math.Floor(t * 2)) % 2 == 0, false);   // RUN: the first one runs ahead (the camera goes out)
			return (ph == 0 ? 1 : ph == 2 ? -1 : 0, 0, ((int)Math.Floor(t * 2 + s.i)) % 3 == 0, false);
		}
		return (0, 0, false, false);
	}
	void Tap(int dev, string k)
	{
		press.TryGetValue(dev, out var p);
		p = k switch { "c" => p with { c = true }, "pz" => p with { pz = true }, "b" => p with { b = true }, "r1" => p with { r1 = true }, "up" => p with { up = true }, "down" => p with { down = true }, _ => p };
		press[dev] = p; untap.Add((dev, i + 3));
	}
	List<(int dev, int at)> untap = new();

	// ---------- the made-up Firebase ----------
	Dictionary<string, (string pw, string uid)> users = new(); Dictionary<string, JsonObject> docs = new(); Dictionary<string, double> lastAt = new();
	(string uid, string tok) SignUp(string name, string pw)
	{
		string uid = "u" + name; users[G_nameEmail(name)] = (pw, uid);
		docs[uid] = JsonNode.Parse($"{{\"name\":\"x/players/{uid}\",\"fields\":{{\"name\":{{\"stringValue\":\"{name}\"}},\"n\":{{\"mapValue\":{{\"fields\":{{}}}}}},\"e\":{{\"mapValue\":{{\"fields\":{{}}}}}},\"xp\":{{\"integerValue\":\"0\"}}}}}}").AsObject();
		return (uid, "tok" + uid);
	}
	static string G_nameEmail(string n) => "u" + string.Concat(Encoding.UTF8.GetBytes(n.ToLowerInvariant()).Select(b => b.ToString("x2"))) + "@mohammed-game.app";
	async Task<HttpResponseMessage> Fake(HttpRequestMessage r)
	{
		string m = r.Method.Method, url = r.RequestUri.OriginalString, body = r.Content != null ? await r.Content.ReadAsStringAsync() : null;
		string auth = r.Headers.TryGetValues("Authorization", out var av) ? av.First() : null;
		req.Add($"{str(Math.Round(G.songT, 2))} {m} {url.Split('?')[0].Split('/').Last()} {auth} {body}");
		await Task.Yield();
		HttpResponseMessage R(int st, string b) => new((HttpStatusCode)st) { Content = new StringContent(b) };
		if (url.Contains("accounts:signUp"))
		{
			var j = JsonNode.Parse(body); string em = (string)j["email"];
			if (users.ContainsKey(em)) return R(400, "{\"error\":{\"message\":\"EMAIL_EXISTS\"}}");
			string uid = "u" + em.Substring(1, 6); users[em] = ((string)j["password"], uid);
			return R(200, $"{{\"localId\":\"{uid}\",\"idToken\":\"tok{uid}\",\"refreshToken\":\"rt{uid}\",\"expiresIn\":\"3600\"}}");
		}
		if (url.Contains("accounts:signInWithPassword"))
		{
			var j = JsonNode.Parse(body); string em = (string)j["email"];
			if (!users.TryGetValue(em, out var u) || u.pw != (string)j["password"]) return R(400, "{\"error\":{\"message\":\"INVALID_LOGIN_CREDENTIALS\"}}");
			return R(200, $"{{\"localId\":\"{u.uid}\",\"idToken\":\"tok{u.uid}\",\"refreshToken\":\"rt{u.uid}\",\"expiresIn\":\"3600\"}}");
		}
		if (m == "GET" && url.Contains("/players/"))
		{
			string uid = url.Split("/players/")[1].Split('?')[0];
			return docs.TryGetValue(uid, out var d) ? R(200, d.ToJsonString()) : R(404, "{\"error\":{\"code\":404,\"message\":\"NOT_FOUND\"}}");
		}
		if (m == "POST" && url.EndsWith(":commit"))
		{
			var ws = JsonNode.Parse(body)["writes"].AsArray(); double now = performance.now();
			foreach (var w in ws)
			{
				string nm = (string)w["update"]["name"]; if (!nm.Contains("/players/")) continue; string uid = nm.Split("/players/")[1];
				if (lastAt.TryGetValue(uid, out var la) && now - la < 20000) return R(403, "{\"error\":{\"code\":403,\"message\":\"PERMISSION_DENIED\"}}");
			}
			foreach (var w in ws)
			{
				string nm = (string)w["update"]["name"]; if (!nm.Contains("/players/")) continue; string uid = nm.Split("/players/")[1];
				var f = w["update"]["fields"].AsObject(); lastAt[uid] = now;
				if (!docs.TryGetValue(uid, out var d) || w["updateMask"] == null) { docs[uid] = new JsonObject { ["name"] = "x/players/" + uid, ["fields"] = f.DeepClone() }; continue; }
				var df = d["fields"].AsObject();
				foreach (var pth in w["updateMask"]["fieldPaths"].AsArray().Select(x => (string)x))
				{
					var parts = pth.Split('.');
					if (parts.Length == 1) { df[parts[0]] = f[parts[0]].DeepClone(); continue; }
					df[parts[0]] ??= new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = new JsonObject() } };
					df[parts[0]]["mapValue"]["fields"][parts[1]] = f[parts[0]]["mapValue"]["fields"][parts[1]].DeepClone();
				}
			}
			return R(200, "{}");
		}
		return R(404, "{}");
	}

	// ---------- the frames ----------
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, shotName + ".png")); shot = false; }
		if (done) { Finish(); return; }
		i++; foreach (var (dev, at) in untap.ToList()) if (at == i) { press[dev] = default; untap.Remove((dev, at)); if (dev == G.KB) foreach (var k in new[] { "Enter", "Escape", "Backspace" }) G.OnKeyUp(k); }
		double t = i / (double)fps;
		// the script: Enter on the menu, each controller joins, sign-ins, player 1 starts
		if (step == 0 && i == 10) { G.OnKeyDown("Enter", false); untap.Add((G.KB, i + 3)); step = 1; }
		else if (step == 1 && G.state == "lcJoin" && i >= 20) { for (int s = 0; s < n; s++) Tap(Dev(s), "c"); step = 2; }
		else if (step == 2 && i >= 30 && G.lc.seats.Count == n)
		{
			// one sign-in at a time (like people do): the next once the one before is done
			var sg = (EnvS("SIGN") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
			if (signK < sg.Length && !(bool)typeof(G).GetField("authBusy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null))
			{
				var f = sg[signK++].Split(':'); int si = int.Parse(f[0]); bool mk = f[1].EndsWith("+"); string nm = f[1].TrimEnd('+');
				var mi = typeof(G).GetMethod("lcDoAuth", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				mi.Invoke(null, new object[] { G.lc.seats[si], mk, nm, mk ? "newpass1" : nm.ToLowerInvariant() + "pw1" });
			}
			else if (signK >= sg.Length && !(bool)typeof(G).GetField("authBusy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null)) { step = 3; signAt = i; }
		}
		else if (step == 3 && i >= signAt + 20)
		{
			if (EnvS("SIGN") is { } sg2 && sg2.Contains(':')) { var s1 = G.lc.seats.FirstOrDefault(q => !q.guest && !q.pc); if (s1 != null) Tap(s1.dev, "r1"); }
			step = 4;
		}
		else if (step == 4 && i >= signAt + 30)
		{
			if (EnvS("LOBBY") == "1") { Shot("lobby"); done = true; }
			else if (EnvS("AUTH") == "1")
			{
				var s1 = G.lc.seats[Math.Min(1, n - 1)]; Tap(s1.dev, "down"); step = 40;
			}
			else { Tap(Dev(0), "pz"); step = 5; }
		}
		else if (step == 40 && i >= signAt + 36) { Tap(G.lc.seats[Math.Min(1, n - 1)].dev, "c"); step = 41; }
		else if (step == 41 && i >= signAt + 42 && G.state == "lcAuth") { foreach (char ch in "Noor") G.tfKey(ch.ToString(), false); step = 42; }
		else if (step == 42 && i >= signAt + 50) { Shot("auth"); done = true; }
		// the pause
		if (PZ >= 0 && !pzDone && pzAt < 0 && G.lc?.playing == true && G.songT >= PZ && G.state is "play" or "pltPlay" or "tmPlay") { Tap(Dev(Math.Min(1, n - 1)), "pz"); pzAt = t; }
		if (pzAt >= 0 && !pzDone && t - pzAt > 2.5) { Tap(Dev(0), "c"); pzDone = true; }
		else if (pzAt >= 0 && !pzDone && t - pzAt > 2 && !pzShot) { Shot("pause"); pzShot = true; }
		// versus: through the gate at FIN
		if (G.lc?.mode == "vs" && G.state == "pltPlay")
			for (int s = 0; s < Math.Min(fin.Length, G.lc.seats.Count); s++)
			{
				var q = G.lc.seats[s]; if (q.fin >= 0 || q.deadAt >= 0 || q.run.t < fin[s]) continue;
				var r = q.run; for (int y = 0; y < r.h; y++) for (int x = 0; x < r.w; x++) { char c = r.m[y][x]; if (c >= '1' && c <= '9') r.got.Add(x + "," + y); if (c == 'G') { q.p.x = x * G.PT + G.PT / 2; q.p.y = y * G.PT + G.PT / 2; } }
			}
		// team: won at WIN
		if (WIN >= 0 && !winDone && G.lc?.mode == "team" && G.state == "tmPlay" && G.tm != null && G.tm.t >= WIN)
		{
			winDone = true; var tm = G.tm;
			if (tm.L.id == "P3") typeof(G).GetMethod("tmMicWin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
			else if (!tm.th.Any(q => q.k == "slot")) winDone = false;   // the photo's frame is only on its last page: wait for it
			else { var pl = typeof(G).GetMethod("tmPlace", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); foreach (var q in tm.th.Where(q => q.k == "slot" && !q.on).ToList()) pl.Invoke(null, new object[] { q }); }
		}
		foreach (var sh in shots.ToList()) if (G.lc?.playing == true && SongT() >= sh) { Shot($"s{sh:0.0}"); shots.Remove(sh); }
		double now = t0 + i * 1000.0 / fps; performance.manual = now; AudioCtx.manualTime = i / (double)fps;
		bool draw = (EVERY > 0 && i % EVERY == 0) || shot || G.state is "lcJoin" or "lcAuth" or "lcBWin" or "lcVWin" || G.tm?.won == true;
		G.gfx.BeginFrame(); G.step(now, !draw); G.gfx.EndFrame();
		if (EVERY > 0 && i % EVERY == 0 && !shot) Shot($"f{i:D6}");
		Rec(t);
		if (t > TMAX || (G.state is "lcBWin" or "lcVWin" && now - G.winAt > 6000) || (G.tm?.won == true && G.tm.t - G.tm.wonAt > 9)) { Shot("end"); done = true; }
	}
	double SongT() => G.lc?.mode == "team" ? G.tm?.t ?? -1 : G.songT;
	void Shot(string name) { shot = true; shotName = name; }
	void Rec(double t)
	{
		var sb = new StringBuilder(); sb.Append(i).Append(' ').Append(str(Math.Round(t, 3))).Append(' ').Append(G.state).Append(' ').Append(str(Math.Round(SongT(), 3)));
		var ev = new StringBuilder();
		if (G.lc != null) foreach (var s in G.lc.seats)
			{
				string pos = G.lc.mode == "team" && G.tm != null && s.i < G.tm.b.Length ? $"{str(Math.Round(G.tm.b[s.i].x))},{str(Math.Round(G.tm.b[s.i].y))}{(G.tm.b[s.i].down ? "d" : "")}{(G.tm.b[s.i].gate ? "g" : "")}"
					: s.p != null ? $"{str(Math.Round(s.p.x))},{str(Math.Round(s.p.y))}" : "-";
				sb.Append(" |").Append(s.name).Append(' ').Append(pos).Append(" hp").Append(s.p?.hp ?? 0);
				ev.Append($" [{s.name}{(s.pc ? "*" : s.guest ? "" : "@")} hits{s.hits} deaths{s.deaths}{(s.down ? " DOWN" : "")}{(s.fin >= 0 ? " fin" + str(s.fin) : "")}{(s.deadAt >= 0 ? " dead" : "")} rank{s.rank ?? "-"} save{s.saveSt ?? "-"} xp{s.xpGain} sk{s.sk}]");
			}
		if (G.lc?.mode == "team" && G.tm != null) ev.Append($" page{G.tm.page} falls{string.Join(",", G.tm.falls)} won{(G.tm.won ? 1 : 0)} cam{str(Math.Round(G.tm.P.zoom, 2))}");
		string e = G.state + " cp" + str(G.checkpoint) + ev;
		log.Append(sb).Append('\n');
		if (e != lastEv) { lastEv = e; File.AppendAllText(Path.Combine(outDir, "events.txt"), $"{str(Math.Round(t, 2))} song {str(Math.Round(SongT(), 2))} {e}\n"); }
	}
	void Finish()
	{
		File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString()); File.WriteAllText(Path.Combine(outDir, "net.log"), string.Join("\n", req) + "\n");
		var o = new StringBuilder(); o.Append($"state {G.state} best {G.lc?.lv?.best ?? "-"} acc {G.acc?.name ?? "-"}\n");
		if (G.lc != null) foreach (var s in G.lc.seats) o.Append($"seat {s.i} {s.name} pc{(s.pc ? 1 : 0)} guest{(s.guest ? 1 : 0)} rank {s.rank ?? "-"} hits {s.hits} deaths {s.deaths} fin {str(s.fin)} save {s.saveSt ?? "-"} xp {s.xpGain}\n");
		foreach (var (uid, d) in docs) o.Append($"doc {uid}: {d["fields"].ToJsonString()}\n");
		File.WriteAllText(Path.Combine(outDir, "result.txt"), o.ToString());
		GD.Print("local-test done frames=", i); host.GetTree().Quit();
	}
}
