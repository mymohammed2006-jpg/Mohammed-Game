using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MG;
using static MG.JS;

// A test tool only (players never see it: nothing in the menus, and it will not start in the exported game).
// Three bots that play the co-op level like people: they follow the finishing routes (tools/team/*.txt) with a person's
// timing errors, make mistakes on purpose (late or early jumps, freezing, walking into danger), help a fallen one back up
// (touching it for a second), wait for the beat before trying a page again, and check the game's rules while they play.
//   -- --team-bot <levelId> <outDir> <routes.cfg>      (tools/human-bot-plat/run.sh makes it all)
// env SEED (mistakes and timing), JIT (timing error, ms, default 25), REACT (reaction, ms, default 280), MIS (chance of a
// mistake on each try of a room, default 0.7), MISN (tries with mistakes, default 2), ALL3 (chance all three die on purpose),
// FPS (default 60), T (song seconds to stop at), REF=<file> (write the clean run's room starts and places there),
// NODRAW=1, WAV=<file>, FROM (pictures after this time), VIEW (as TeamTest).
// Writes events.log (what happened: deaths and their cause, revives, restarts, rooms, mistakes, the win) and bugs.log
// (anything breaking the level's rules), plus state.log like TeamTest's.
public class TeamBot
{
	public static TeamBot FromArgs(string[] a, int k) => new TeamBot { id = a[k], outDir = a[k + 1], cfg = a[k + 2] };
	string id, outDir, cfg; Main host; int i, fps; double T; bool done, shot, nodraw = Env("NODRAW") == "1";
	public Crew crew; StringBuilder log = new(); BinaryWriter wav = Env("WAV") != null ? new BinaryWriter(new MemoryStream()) : null;
	double from = double.TryParse(Env("FROM"), out var fr) ? fr : 0; string view = Env("VIEW");
	// env CLIPS="a-b,c-d": pictures only for those frames (for a short video of the best moments of a long run)
	readonly (int a, int b)[] clips = Env("CLIPS")?.Split(',').Select(c => (int.Parse(c.Split('-')[0]), int.Parse(c.Split('-')[1]))).ToArray();
	bool Pic(int f) => clips == null || clips.Any(c => f >= c.a && f <= c.b);
	static string Env(string k) => System.Environment.GetEnvironmentVariable(k);

	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		if (OS.HasFeature("template")) { h.GetTree().Quit(); return; }   // never in the game players get
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false;
		fps = int.TryParse(Env("FPS"), out var f) ? f : 60;
		performance.manual = 100000; AudioCtx.manualTime = 0; JS.SeedRandom(7);
		G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0; G.gatePassed = true;
		var L = G.LoadTeam(id);
		crew = new Crew(L, cfg, outDir, -1);
		T = double.TryParse(Env("T"), out var tt) ? tt : 900;
		G.ensureAudio(); G.stopMenu();
		G.tmStart(L, crew.page0); G.state = "team";
		if (Env("SCEN") == "touch") { Touch(L); h.GetTree().Quit(); }
	}
	// his friends' report (02:03 10-20): standing touching, one jumps and the other jumps too without pressing.
	// On every page: two squares side by side (or one on the other's head on stack pages) at several gaps, one jumps.
	void Touch(TeamLevel L)
	{
		var o = new StringBuilder(); int bad = 0;
		for (int p = 0; p < L.pages.Count; p++)
			foreach (double gap in new[] { 0.0, 6, 12, 17, 18, 19, 24 })
				foreach (bool onHead in new[] { false, true })
				{
					G.tmStart(L, p); var tm = G.tm; double dt = 1.0 / 60;
					var a = tm.b[0]; var b = tm.b[1]; var c = tm.b[2];
					a.x = tm.sx[0]; b.x = tm.sx[0] + gap; a.y = tm.sy[0]; b.y = onHead ? tm.sy[0] - 18 : tm.sy[0]; c.x = tm.sx[2] + 200; a.inv = b.inv = c.inv = 99;
					var none = new G.TIn[3];
					for (int k = 0; k < 40; k++) { G.tmStep(dt, none); a.x = tm.sx[0]; b.x = tm.sx[0] + gap; }
					double y0 = b.y, ay0 = a.y, minY = b.y, aMin = a.y; bool bGround0 = b.ground;
					var ins = new G.TIn[3]; ins[0] = new G.TIn { jump = true, jumpHeld = true };
					for (int k = 0; k < 40; k++) { G.tmStep(dt, ins); ins[0].jump = false; minY = Math.Min(minY, b.y); aMin = Math.Min(aMin, a.y); }
					bool rose = y0 - minY > 4, jumped = ay0 - aMin > 20;
					string line = $"page {p} stack={tm.P.stack} gap {gap} {(onHead ? "on head" : "side")}: jumper rose {Math.Round(ay0 - aMin)} px, other (no keys) rose {Math.Round(y0 - minY)} px, other on ground before={bGround0}";
					o.Append(line).Append('\n');
					if (rose && jumped && !(onHead && tm.P.stack)) bad++;
				}
		o.Append($"cases where the other rose without pressing (and not riding on a head on a stack page): {bad}\n");
		File.WriteAllText(Path.Combine(outDir, "touch.txt"), o.ToString());
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{i:D6}.png")); shot = false; }
		if (done) { Finish(); host.GetTree().Quit(); return; }
		while (true)
		{
			i++; double t = (double)i / fps;
			if (t > T || crew.over) { done = true; return; }
			var ins = crew.Step(1.0 / fps);
			Bench(ins);
			AudioCtx.manualTime += 1.0 / fps; performance.manual += 1000.0 / fps;
			bool pic = !nodraw && Pic(i) && t >= from;
			if (pic) G.gfx.BeginFrame();
			G.tmStep(1.0 / fps, ins);
			crew.After(1.0 / fps);
			if (wav != null) { int n = AudioCtx.SR / fps; var l = new float[n]; var r = new float[n]; AudioCtx.RenderAt(l, r, n, AudioCtx.manualTime.Value); for (int j = 0; j < n; j++) { wav.Write((short)(Math.Clamp(l[j], -1, 1) * 32767)); wav.Write((short)(Math.Clamp(r[j], -1, 1) * 32767)); } }
			G.particlesStep(1.0 / fps);
			log.Append(str(Math.Round(t, 3))).Append(" p").Append(G.tm.page).Append(" g").Append(G.tm.gen).Append(" mt=").Append(str(Math.Round(G.tm.t, 2))).Append(" bt=").Append(str(Math.Round(G.tmBeatPub(), 2)));
			foreach (var b in G.tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(b.down ? "d" : "").Append(b.gate ? "g" : "");
			log.Append(" | ").Append(crew.Mode).Append(" ").Append(str(Math.Round(crew.Tau, 3)));
			foreach (var q in G.tm.th) if (q.k == "scale") log.Append(" sc=").Append(str(Math.Round(q.s, 3)));
			if (System.Environment.GetEnvironmentVariable("FALLDBG") == "1") foreach (var q in G.tm.th) if (q.k == "fall") log.Append(" f").Append(str(Math.Round(q.X))).Append("=").Append(q.gone ? "G" : "").Append(str(Math.Round(q.timer, 2))).Append("/").Append(str(Math.Round(q.Y)));
			log.Append(" bt=").Append(str(Math.Round(G.tmBeatPub(), 2))).Append('\n');
			if (pic)
			{
				var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, 1280, 720);
				double sx = (random() * 2 - 1) * G.shake, sy = (random() * 2 - 1) * G.shake; g.setTransform(1, 0, 0, 1, sx, sy);
				G.tmView = view == "cycle" ? (G.tm.P.dark ? (int)(G.tm.pageAt / 3.2) % 3 : -1) : int.TryParse(view, out var vv) ? vv : -1;
				G.drawTeam(performance.manual.Value);
				if (G.flash > 0) { g.fillStyle = $"rgba(255,255,255,{str(Math.Min(0.8, G.flash))})"; g.fillRect(0, 0, 1280, 720); }
				if (tag != null)
				{
					// a fixed line for a before/after clip (env TAG, its colour TAGC): old code or new, and what to look at
					g.setTransform(1, 0, 0, 1, 0, 0); var rows = tag.Split('|'); string tc = Env("TAGC") ?? "#ff5a6e";
					g.font = $"800 27px {G.FONT}"; double wd = rows.Max(r => g.measureText(r).width) + 48, hh = 16 + rows.Length * 36;
					g.fillStyle = "rgba(8,6,20,0.82)"; G.rr(640 - wd / 2, 8, wd, hh, 14); g.fill();
					g.strokeStyle = tc; g.lineWidth = 3; G.rr(640 - wd / 2, 8, wd, hh, 14); g.stroke();
					for (int r = 0; r < rows.Length; r++) G.txt(rows[r], 640, 8 + 26 + r * 36, r == 0 ? 27 : 23, r == 0 ? tc : "#ffffff");
				}
				else if (crew.CapOn)
				{
					// the story's line at the top: one or two rows ("|"), on a dark box as wide as the text
					g.setTransform(1, 0, 0, 1, 0, 0); var rows = crew.CapText.Split('|'); double a = Math.Min(1, Math.Min(crew.frame - crew.CapFrom, crew.CapTo - crew.frame) / 8.0);
					g.font = $"800 27px {G.FONT}"; double wd = rows.Max(r => g.measureText(r).width) + 48, hh = 16 + rows.Length * 36;
					g.globalAlpha = a; g.fillStyle = "rgba(8,6,20,0.78)"; G.rr(640 - wd / 2, 8, wd, hh, 14); g.fill();
					for (int r = 0; r < rows.Length; r++) G.txt(rows[r], 640, 8 + 26 + r * 36, r == 0 ? 27 : 23, r == 0 ? "#ffffff" : "#ffd6f0");
					g.globalAlpha = 1;
				}
				else if (Env("LABELS") == "1" && G.tm.t - crew.LabelAt < 2.6 && crew.Label != "")
				{
					g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "rgba(0,0,0,0.6)"; g.fillRect(340, 652, 600, 50);
					G.txt(crew.Label, 640, 688, 28, "#ffffff");
				}
				G.gfx.EndFrame(); shot = true; return;
			}
			if (!pic && i % 600 == 0) return;
		}
	}
	// before/after clips (env): TP="frame:who:x:y" puts a square there at that frame (y "under" = below the map, x "same"),
	// HOLD="who:from:to" leaves that square without keys in those frames
	readonly string tag = Env("TAG");
	void Bench(G.TIn[] ins)
	{
		if (Env("TP") is string tp) { var v = tp.Split(':'); if (int.Parse(v[0]) == i) { var b = G.tm.b[int.Parse(v[1])]; if (v[2] != "same") b.x = double.Parse(v[2]); b.vx = b.vy = 0; if (v[3] == "under") { b.y = G.tm.h * G.PT + 80; b.vy = -60; } else b.y = double.Parse(v[3]); } }
		if (Env("HOLD") is string hd) { var v = hd.Split(':'); if (i >= int.Parse(v[1]) && i <= int.Parse(v[2])) ins[int.Parse(v[0])] = new G.TIn(); }
	}
	void Finish()
	{
		File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString());
		crew.Save();
		if (wav != null)
		{
			var pcm = ((MemoryStream)wav.BaseStream).ToArray(); using var f = new BinaryWriter(File.Create(Env("WAV")));
			f.Write(Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + pcm.Length); f.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); f.Write(16); f.Write((short)1); f.Write((short)2);
			f.Write(AudioCtx.SR); f.Write(AudioCtx.SR * 4); f.Write((short)4); f.Write((short)16); f.Write(Encoding.ASCII.GetBytes("data")); f.Write(pcm.Length); f.Write(pcm);
		}
	}
}

// The three bots' minds (offline all three; online each PC runs it and uses only its own square: every PC sees the same music
// clock and the same restarts, so they all pick the same mistakes and the same waits).
public class Crew
{
	const double HALF = 9;
	readonly TeamLevel L; readonly string outDir; readonly int me;
	readonly Dictionary<int, (double t, string k)[][]> routes = new();   // room -> each player's lines
	readonly Dictionary<int, int> per = new();                           // room -> the beats its things repeat in (wait for the same place in them)
	public int page0;
	// the clean run's room starts (beat) and places (each 1/60 s): from REF, or loaded
	Dictionary<int, double> refBeat = new(); Dictionary<int, List<double[]>> refPos = new(); Dictionary<int, List<double>> refB = new(); readonly string refOut;
	readonly Random rng0; readonly double JIT, REACT, MIS, ALL3; readonly int MISN;
	public string Label = ""; public double LabelAt = -99;   // a short line for the video (env LABELS=1)
	static readonly string[] NM = { "الأحمر", "الأزرق", "الأصفر" };
	void Say(string s, bool story = false) { if (STORY && !story) return; Label = s; LabelAt = G.tm?.t ?? 0; if (STORY) Cap(s, 3.2); }
	// the story video's lines: each stays its time (a new one waits until the last was up 2.2 s), drawn at the top
	public string CapText = ""; public int CapFrom, CapTo; readonly Queue<(string s, int n)> capQ = new();
	void Cap(string s, double sec) { int n = (int)(sec * 60); if (CapText != "" && frame < CapTo && frame - CapFrom < 132) { capQ.Enqueue((s, n)); return; } CapText = s; CapFrom = frame; CapTo = frame + n; }
	void CapTick() { if (capQ.Count > 0 && (frame >= CapTo || frame - CapFrom >= 132)) { var (s, n) = capQ.Dequeue(); CapText = s; CapFrom = frame; CapTo = frame + n; } }
	public bool CapOn => STORY && CapText != "" && frame < CapTo;
	// env STORY=<file> (tools/human-bot-plat/story_P2.txt): the video where they learn the level. Per room: the first try with a
	// set mistake (and its line), then the try that solves it with lines at route times; a demo while waiting for the beat;
	// one falling on purpose while the others keep playing and one comes to help. Their timing has no error (the mistakes are set).
	class RoomStory { public string intro, failSay, again, done; public Mistake fail; public bool keep; public List<(double t, string s)> says = new(), failSays = new(); public int forceWho = -1, helpWho = -1; public double forceAt, helpAt, firstWait; public string forceSay, demo; public int[] demoWho; }
	readonly Dictionary<int, RoomStory> story = new(); readonly List<string> storyStart = new(); string storyWin; public readonly bool STORY = Env("STORY") != null;
	RoomStory St => story.GetValueOrDefault(room); bool FailTry => St?.fail != null && tries.GetValueOrDefault(room) == 1; bool SolveTry => St != null && !solved.Contains(room) && tries.GetValueOrDefault(room) == (St.fail != null ? 2 : 1); readonly HashSet<int> solved = new();
	bool lastWasFail; int failRoom = -1;
	readonly double[] home = new double[3];
	int sayI; bool failSaid, forced1, helpGo, doneSaid, waitSaid = false, startSaid; int demoState = -1; double demoT; int demoFrom = -1;
	void LoadStory(string f)
	{
		RoomStory r = null;
		foreach (var raw in File.ReadAllLines(f))
		{
			var l = raw.Trim(); if (l.Length == 0 || l.StartsWith("#")) continue;
			var w = l.Split(' ', StringSplitOptions.RemoveEmptyEntries); string rest(int n) => string.Join(' ', w.Skip(n));
			switch (w[0])
			{
				case "start": storyStart.Add(rest(1)); break;
				case "win": storyWin = rest(1); break;
				case "room": r = new RoomStory(); story[int.Parse(w[1])] = r; break;
				case "intro": r.intro = rest(1); break;
				case "again": r.again = rest(1); break;
				case "done": r.done = rest(1); break;
				case "fail": r.fail = new Mistake { kind = w[1], who = int.Parse(w[2]), at = double.Parse(w[3]), len = double.Parse(w[4]), shift = double.Parse(w[4]) }; break;
				case "failsay": r.failSay = rest(1); break;
				case "failat": r.failSays.Add((double.Parse(w[1]), rest(2))); break;
				case "keep": r.keep = true; break;
				case "say": r.says.Add((double.Parse(w[1]), rest(2))); break;
				case "force": r.forceWho = int.Parse(w[1]); r.forceAt = double.Parse(w[2]); r.forceSay = rest(3); break;
				case "help": r.helpWho = int.Parse(w[1]); r.helpAt = double.Parse(w[2]); break;
				case "demo": r.demo = w[1]; r.demoWho = new[] { int.Parse(w[2]), int.Parse(w[3]) }; break;
				case "firstwait": r.firstWait = double.Parse(w[1]); break;
			}
		}
	}
	public bool over; public string Mode = ""; public double Tau => tau;
	readonly StringBuilder ev = new(), bugs = new(); readonly HashSet<string> bugSeen = new(), evSeen = new();
	public int nBugs, nDeaths, nRevives, nRestarts, nMistakes, nForced, nRooms; public bool won;

	// this try of the room
	int gen = -1, room = -1, restarts0, lastRestarts; readonly Dictionary<int, int> tries = new();
	double tau, startBeat, dB; int frames;   // dB: this try's beats minus the clean run's (a whole number of the room's repeats)
	double[][] jit = new double[3][]; string[] lastK = { "", "", "" };
	Mistake mis; double pausedAt = -1, regroupTill = -1, resumeBeat = -1; int helper = -1, helpOf = -1; double helpFrom = -1, touch;
	double spot, pausedRb, awayTill = -1; readonly Dictionary<int, string> firstSnap = new();
	static string Diff(string a, string b) { var x = a.Split(';'); var y = b.Split(';'); var o = new List<string>(); for (int i = 0; i < Math.Max(x.Length, y.Length); i++) { string p = i < x.Length ? x[i] : "-", q = i < y.Length ? y[i] : "-"; if (p != q) o.Add(p + " -> " + q); } return string.Join("  ", o.Take(4)); } bool said;
	double[] downAt = { -1, -1, -1 }, touchT = new double[3]; string[] cause = new string[3];
	double buildAt; bool[] moved = new bool[3]; double stuckT; int stuckRoom = -1; double lastProg;
	int hits0;
	bool CHAOS = Env("CHAOS") == "1"; readonly List<Mistake> chaos = new(); bool lateHelp, leaveTouch; readonly Random crng = new(5);
	// one silly input for a while: spam jump or dash, run the wrong way, stand still, mash random buttons, jump on a teammate's
	// head, dash into the wall
	G.TIn Chaos(int k, Mistake c, double t)
	{
		var tm = G.tm; var b = tm.b[k]; var inp = new G.TIn(); moved[k] = true; bool tick = (int)(t * 12) % 2 == 0;
		if (c.kind == "gatedie" && !tm.b.Any(q => q.gate)) return Plain(k, t);   // waits for someone in the gate
		if (c.kind == "carrydie" && b.carry == null) return Plain(k, t);           // waits until it carries its photo piece
		if (!c.hit) { c.hit = true; Ev($"CHAOS {c.kind} by p{(c.kind == "pile" ? "all" : k.ToString())}"); Say(c.kind == "pile" ? "تجربة غبية: الثلاثة يتكدسون بمكان واحد" : $"تجربة غبية: {NM[k]} " + c.kind switch { "spamJ" => "ينط بدون توقف", "spamX" => "يندفع بدون توقف", "back" => "يمشي بالعكس", "still" => "يوقف مكانه", "mash" => "يضغط أي شي", "head" => "ينط على راس صاحبه", "downdash" => "يندفع لتحت وبالزوايا", "hug" => "يلصق في الجدار وينط", "headsit" => "يقعد على راس صاحبه", "idle" => "يوقف ١٠ ثواني", "gatedie" => "يموت وصاحبه في البوابة", "carrydie" => "يموت وهو شايل القطعة", _ => "يندفع في الجدار" }); }
		switch (c.kind)
		{
			case "spamJ": inp.jump = tick; inp.jumpHeld = tick; break;
			case "spamX": inp.dash = tick; inp.x = b.fx; break;
			case "back": inp.x = -b.fx; inp.jump = b.ground && Math.Abs(b.vx) < 20 && tick; inp.jumpHeld = !b.ground; break;
			case "still": break;
			case "mash": inp.x = crng.Next(3) - 1; inp.y = crng.Next(3) - 1; inp.jump = crng.Next(4) == 0; inp.jumpHeld = crng.Next(2) == 0; inp.dash = crng.Next(6) == 0; break;
			case "head":
				{
					var o = tm.b.Where(q => q != b && !q.down && !q.gate).OrderBy(q => hypot(q.x - b.x, q.y - b.y)).FirstOrDefault(); if (o == null) break;
					double dx = o.x - b.x; inp.x = Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1); inp.jump = b.ground && Math.Abs(dx) < 50 && tick; inp.jumpHeld = !b.ground; break;
				}
			case "downdash": inp.x = crng.Next(3) - 1; inp.y = 1; inp.dash = tick; inp.jump = b.ground && crng.Next(3) == 0; inp.jumpHeld = true; break;
			case "hug": { int tx = (int)Math.Floor(b.x / G.PT), ty = (int)Math.Floor(b.y / G.PT), dl = 0, dr = 0; while (tx - dl > 0 && tm.m[ty][tx - dl] != '#') dl++; while (tx + dr < tm.w - 1 && tm.m[ty][tx + dr] != '#') dr++; inp.x = dl < dr ? -1 : 1; inp.y = -1; inp.jump = b.ground && tick; inp.jumpHeld = true; break; }
			case "headsit":
				{
					var o = tm.b.Where(q => q != b && !q.down && !q.gate).OrderBy(q => hypot(q.x - b.x, q.y - b.y)).FirstOrDefault(); if (o == null || b.onB != null) break;
					double dx = o.x - b.x; inp.x = Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1); inp.jump = b.ground && Math.Abs(dx) < 40 && tick; inp.jumpHeld = !b.ground; break;
				}
			case "idle": break;
			case "pile": { var a = tm.b[c.who]; double dx = a.x - b.x; inp.x = k == c.who || Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1); inp.jump = b.ground && Math.Abs(dx) < 30 && Math.Abs(a.y - b.y) > 20 && tick; inp.jumpHeld = !b.ground; break; }
			case "gatedie": case "carrydie": { var r = Danger(k, t); if (b.down) c.done = true; return r; }
			default: inp.x = c.at % 2 < 1 ? 1 : -1; inp.dash = tick; inp.y = c.at % 1 < 0.5 ? -1 : 0; break;
		}
		lastK[k] = "?";
		return inp;
	}
	class Mistake { public int who; public double at, len; public string kind; public bool done, hit; public double shift; }

	public Crew(TeamLevel L, string cfg, string outDir, int me)
	{
		this.L = L; this.outDir = outDir; this.me = me;
		int seed = int.TryParse(Env("SEED"), out var s) ? s : 1; rng0 = new Random(seed);
		JIT = D("JIT", 25) / 1000; REACT = D("REACT", 280) / 1000; MIS = D("MIS", 0.7); MISN = (int)D("MISN", 2); ALL3 = D("ALL3", 0.12);
		refOut = Env("REF");
		// routes.cfg: "page <n> <r0> <r1> <r2>" (a route's ROOM blocks start the next rooms), "per <room> <beats>", "start <page>"
		string dir = Path.GetDirectoryName(Path.GetFullPath(cfg));
		foreach (var line in File.ReadAllLines(cfg).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0))
		{
			var f = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (f[0] == "page") { int p = int.Parse(f[1]); for (int k = 0; k < 3; k++) Load(p, k, Path.Combine(dir, f[2 + k])); }
			else if (f[0] == "per") per[int.Parse(f[1])] = int.Parse(f[2]);
			else if (f[0] == "start") page0 = int.Parse(f[1]);
			else if (f[0] == "ref" && refOut == null && File.Exists(Path.Combine(dir, f[1]))) LoadRef(Path.Combine(dir, f[1]));
		}
		if (refOut != null) { JIT = 0; MIS = 0; ALL3 = 0; CHAOS = false; }
		if (STORY) { LoadStory(Env("STORY")); JIT = 0; MIS = 0; ALL3 = 0; CHAOS = false; }
	}
	static string Env(string k) => System.Environment.GetEnvironmentVariable(k);
	static double D(string k, double d) => double.TryParse(Env(k), out var v) ? v : d;
	void Load(int page, int k, string file)
	{
		int r = page; var cur = new List<(double, string)>();
		void put() { if (!routes.ContainsKey(r)) routes[r] = new (double, string)[3][]; routes[r][k] = cur.ToArray(); }
		foreach (var l in File.ReadAllLines(file).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")))
		{
			var f = l.Trim().Split(' ', 2);
			if (f[0] == "ROOM") { put(); r = int.Parse(f[1].Split('#')[0].Trim()); cur = new(); continue; }
			cur.Add((double.Parse(f[0]), f.Length > 1 ? f[1].Split('#')[0].Trim() : ""));
		}
		put();
	}
	void LoadRef(string f)
	{
		var j = JsonDocument.Parse(File.ReadAllText(f)).RootElement;
		foreach (var p in j.GetProperty("beat").EnumerateObject()) refBeat[int.Parse(p.Name)] = p.Value.GetDouble();
		foreach (var p in j.GetProperty("bts").EnumerateObject()) refB[int.Parse(p.Name)] = p.Value.EnumerateArray().Select(x => x.GetDouble()).ToList();
		foreach (var p in j.GetProperty("pos").EnumerateObject()) refPos[int.Parse(p.Name)] = p.Value.EnumerateArray().Select(a => a.EnumerateArray().Select(x => x.GetDouble()).ToArray()).ToList();
	}
	public void Save()
	{
		if (refOut != null)
		{
			var o = new Dictionary<string, object> { ["beat"] = refBeat.ToDictionary(k => k.Key.ToString(), k => (object)Math.Round(k.Value, 4)), ["bts"] = refB.ToDictionary(k => k.Key.ToString(), k => (object)k.Value.Select(x => Math.Round(x, 4)).ToList()), ["pos"] = refPos.ToDictionary(k => k.Key.ToString(), k => (object)k.Value.Select(a => a.Select(x => Math.Round(x)).ToArray()).ToList()) };
			File.WriteAllText(refOut, JsonSerializer.Serialize(o));
		}
		var tm = G.tm;
		Ev($"END won={won} deaths={nDeaths} revives={nRevives} restarts={nRestarts} mistakes={nMistakes} forced={nForced} falls={string.Join(",", tm.falls)} game.restarts={tm.restarts} bugs={nBugs}");
		File.WriteAllText(Path.Combine(outDir, "events.log"), ev.ToString());
		File.WriteAllText(Path.Combine(outDir, "bugs.log"), bugs.ToString());
		File.WriteAllText(Path.Combine(outDir, "summary.txt"), $"won={won} deaths={nDeaths} revives={nRevives} restarts={nRestarts} mistakes={nMistakes} forced={nForced} bugs={nBugs} songT={str(Math.Round(tm.t, 1))}\n");
	}
	public int frame;   // steps since the run began (the video's frame)
	void Ev(string s) => ev.Append('@').Append(frame).Append(' ').Append('[').Append(str(Math.Round(G.tm?.t ?? 0, 2))).Append(" p").Append(G.tm?.page).Append("] ").Append(s).Append('\n');
	void Bug(string key, string s)
	{
		if (evSeen.Add(key + "@" + gen)) Ev("BUG " + s); if (!bugSeen.Add(key)) return; nBugs++;
		var tm = G.tm; bugs.Append($"[song {str(Math.Round(tm.t, 2))}s, page {tm.page}, beat {str(Math.Round(G.tmBeatPub(), 2))}] {s}\n");
		for (int k = 0; k < 3; k++) { var b = tm.b[k]; bugs.Append($"   p{k}: x={str(Math.Round(b.x))} y={str(Math.Round(b.y))} hp={b.hp} down={b.down} gate={b.gate} vx={str(Math.Round(b.vx))} vy={str(Math.Round(b.vy))}\n"); }
	}

	void SayTick() { if (!STORY) return; var sl = FailTry ? St.failSays : SolveTry ? St.says : null; while (sl != null && sayI < sl.Count && sl[sayI].t <= tau) Cap(sl[sayI++].s, 3.6); }
	// story: someone is down and the others keep playing their routes; the set helper goes to them from its time
	G.TIn[] Keep(G.TIn[] ins, List<int> downs, double bt)
	{
		var tm = G.tm; tau = Clock(bt); Mode = "keep"; frames++; SayTick();
		int d = downs[0]; var db = tm.b[d];
		for (int k = 0; k < 3; k++)
		{
			var b = tm.b[k]; if (b.down) continue;
			if (SolveTry && k == St.helpWho && tau >= St.helpAt)
			{
				if (!helpGo) { helpGo = true; spot = Spot(b, db) ?? db.x; Say($"{NM[k]} رايح ينعش {NM[d]}", true); Ev($"p{k} goes to bring p{d} back (story)"); }
				ins[k] = Walk(k, spot, b.y, true); continue;
			}
			ins[k] = Play(k, Math.Max(0, tau));
		}
		return ins;
	}
	// story: while they wait for the beat, two of them show standing on a head: the one under jumps, the one on top stays
	G.TIn[] Demo(G.TIn[] ins, double bt)
	{
		var tm = G.tm; Mode = "demo"; int c = St.demoWho[0], u = St.demoWho[1]; var cb = tm.b[c]; var ub = tm.b[u];
		double left = (startBeat - bt) * L.spb;   // seconds until they go (about)
		if (demoState < 0) { if (left < 12) { demoState = 9; return ins; } demoState = 0; demoT = tm.t; for (int k = 0; k < 3; k++) home[k] = tm.b[k].x; Ev("DEMO start"); Cap($"وهم ينتظرون الإيقاع: {NM[c]} يطلع على راس {NM[u]}", 3.4); }
		double s = tm.t - demoT;
		switch (demoState)
		{
			case 0:   // climbs on the head
				{
					double dx = ub.x - cb.x;
					ins[c] = new G.TIn { x = Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1), jump = cb.ground && cb.onB == null && Math.Abs(dx) < 40 && frame % 20 == 0, jumpHeld = !cb.ground };
					if (cb.onB == ub && cb.ground) { demoState = 1; demoT = tm.t; }
					else if (s > 5) { demoState = 3; demoT = tm.t; }
					break;
				}
			case 1:   // the one under jumps twice
				{
					if (s >= 0.7 && s < 0.7 + 1.0 / 60) Cap($"{NM[u]} ينط، واللي فوق راسه ما يرتفع معه|قبل التصليح كان ينط معه بدون ما يضغط", 4.4);
					bool j = (s >= 0.8 && s < 0.84) || (s >= 2.0 && s < 2.04); ins[u] = new G.TIn { jump = j, jumpHeld = j || (!ub.ground && ub.vy < 0) };
					if (s > 3.4) { demoState = 2; demoT = tm.t; }
					break;
				}
			default:  // all back to the start
				{
					bool there = true;
					for (int k = 0; k < 3; k++) { var b = tm.b[k]; if (Math.Abs(b.x - home[k]) > 8 || !b.ground) { there = false; ins[k] = Walk(k, home[k], tm.sy[k], false); } }
					if ((there && s > 0.5) || left < 1.5) { demoState = 9; Ev("DEMO end" + (there ? "" : " (not all back)")); }
					break;
				}
		}
		return ins;
	}

	// ---------- before each step: everyone's buttons ----------
	public G.TIn[] Step(double dt)
	{
		var tm = G.tm; var ins = new G.TIn[3]; frame++;
		if (tm.gen != gen) NewTry();
		double bt = G.tmBeatPub();
		if (STORY) CapTick();
		if (tm.won || tm.doneAt >= 0 || tm.restartAt >= 0)
		{
			Mode = tm.won ? "won" : tm.doneAt >= 0 ? "next" : "restart";
			if (Mode == "next" && !doneSaid) { doneSaid = true; Ev($"NEXT room {room} done"); if (STORY && FailTry && St.done != null) Cap(St.done, 3.6); if (STORY && SolveTry && St.done != null) Cap(St.done, 3.6); solved.Add(room); }
			return ins;
		}
		var downs = Enumerable.Range(0, 3).Where(k => tm.b[k].down).ToList();
		if (STORY && downs.Count > 0 && ((FailTry && St.keep) || (SolveTry && St.helpWho >= 0))) return Keep(ins, downs, bt);
		if (downs.Count > 0 || regroupTill >= 0 || resumeBeat >= 0 || pausedAt >= 0) return Help(ins, downs, bt);   // (pausedAt: all up again, the regroup is next)
		// the route's clock follows the music: where the clean run was at this beat (waiting at the start for it, counting in)
		tau = Clock(bt);
		if (tau < 0) { if (STORY && St?.demo != null && SolveTry && demoState < 9) return Demo(ins, bt); Mode = "wait"; return ins; }
		if (Mode == "wait" || frames == 0)
		{
			Ev($"go (room {room}, try {tries[room]}) at beat {str(Math.Round(bt, 2))}");
			if (STORY && tries[room] == 1 && St?.intro != null) Cap(St.intro, 4);
			if (STORY && SolveTry && St.fail != null && St.again != null) Cap(St.again, 3.4);
		}
		Mode = "play";
		SayTick();
		frames++; if (refOut != null && refB.TryGetValue(room, out var rbl) && rbl.Count < frames) rbl.Add(bt);
		for (int k = 0; k < 3; k++) ins[k] = Play(k, tau);
		return ins;
	}
	void NewTry()
	{
		var tm = G.tm; bool restart = tm.restarts != lastRestarts && gen >= 0; lastRestarts = tm.restarts;
		gen = tm.gen; room = tm.page; tries[room] = tries.GetValueOrDefault(room) + 1; nRooms++;
		helper = helpOf = -1; pausedAt = regroupTill = resumeBeat = -1; touch = 0; for (int k = 0; k < 3; k++) { downAt[k] = -1; lastK[k] = ""; moved[k] = false; }
		buildAt = tm.t; stuckT = 0; lastProg = tm.t; for (int k = 0; k < 3; k++) dangerFrom[k] = -1;
		{
			var snap = string.Join(";", tm.th.Select(q => $"{q.k}:{Math.Round(q.X)},{Math.Round(q.Y)},{q.solid},{q.on},{q.melt},{q.gone},{Math.Round(q.s, 2)}")) + $"|got={tm.got.Count}|st={string.Join("", tm.stemOn.Select(x => x ? 1 : 0))}|fl={string.Join(",", tm.flips)}";
			if (!firstSnap.TryGetValue(room, out var f0)) firstSnap[room] = snap;
			else if (f0 != snap && !tm.th.Any(q => q.k == "mover")) Bug("reset-" + room, $"room {room} after a restart is not as it was the first time: {Diff(f0, snap)}");
		}
		double bt = G.tmBeatPub();
		if (refOut != null) { if (!refBeat.ContainsKey(room)) { refBeat[room] = bt; refPos[room] = new(); refB[room] = new(); } }
		// the beat to start on: the clean run's place in the room's beats (P beats), now or soon (a little late is fine)
		int P = per.GetValueOrDefault(room, 1); tau = 0;
		// still failing after the clean tries: wait for the very same place in the song as the clean run (a whole loop)
		if (tries[room] > MISN + 3 && tries[room] % 2 == 0 && !tm.P.cont && tm.P.beats != null) P = tm.P.beats.Length - 1;   // (every other try: the same beat to the frame can still fail the same way)
		if (STORY && tries[room] >= 2 && !tm.P.cont && tm.P.beats != null) P = tm.P.beats.Length - 1;   // the story's tries must work: the very same place in the song
		frames = 0; dB = 0; startBeat = bt; Mode = "";
		if (refBeat.TryGetValue(room, out var rb) && refOut == null)
		{
			double k = Math.Floor((bt - rb) / P), s0 = rb + k * P;
			startBeat = bt - s0 <= 0.3 ? s0 : s0 + P; dB = startBeat - rb;
			if (STORY && tries[room] == 1 && St?.firstWait > 0) { startBeat = rb + St.firstWait; dB = St.firstWait; }
		}
		// person-like timing: each of the route's presses a little early or late
		var rr = routes.GetValueOrDefault(room);
		var rng = new Random(Hash(gen, room, tries[room]));
		// (a person learns the room: after the tries with mistakes the timing gets better, then right)
		int tr = tries[room]; double js = tr <= MISN + 1 ? 1 : tr <= MISN + 3 ? 0.4 : 0;
		for (int k = 0; k < 3; k++) { int n = rr?[k]?.Length ?? 0; jit[k] = new double[n]; for (int j = 0; j < n; j++) jit[k][j] = Math.Clamp(Gauss(rng) * JIT * js, -2.5 * JIT, 2.5 * JIT); }
		// a mistake on purpose (the first tries of each room), sometimes all three
		mis = null; chaos.Clear(); lateHelp = rng.NextDouble() < 0.5; leaveTouch = rng.NextDouble() < 0.5;
		sayI = 0; failSaid = forced1 = helpGo = doneSaid = false; demoState = -1;
		if (STORY && FailTry) { var f = St.fail; mis = new Mistake { kind = f.kind, who = f.who, at = f.at, len = f.len, shift = f.shift }; }
		// chaos (env CHAOS=1): silly things a person might do, several times a try, on the tries with mistakes
		if (CHAOS && tries[room] <= MISN + 1 && rr != null)
		{
			string[] ck = { "spamJ", "spamX", "back", "still", "mash", "head", "wall", "downdash", "hug", "headsit", "idle", "pile", "gatedie", "carrydie" };
			int n = 2 + rng.Next(4);
			for (int c = 0; c < n; c++) chaos.Add(new Mistake { who = rng.Next(3), at = rng.NextDouble() * Math.Max(1, refPos.TryGetValue(room, out var rpc) ? rpc.Count / 60.0 : 10), len = 0.4 + rng.NextDouble() * 2, kind = ck[rng.Next(ck.Length)] });
			foreach (var c in chaos) { if (c.kind == "idle") c.len = 10 + rng.NextDouble() * 2; if (c.kind == "pile") c.len = 2.5; if (c.kind is "gatedie" or "carrydie") c.len = 6; }
			Ev("chaos: " + string.Join(", ", chaos.Select(c => $"{c.kind} p{c.who} at {str(Math.Round(c.at, 1))}s")));
		}
		double dur = refPos.TryGetValue(room, out var rp) && rp.Count > 0 ? rp.Count / 60.0 : rr?.Max(x => x?.Length > 0 ? x[^1].t : 0) ?? 10;
		if (tries[room] <= MISN && rng.NextDouble() < MIS && rr != null)
		{
			string[] kinds = { "late", "early", "freeze", "danger", "danger", "freeze" };
			mis = new Mistake { who = rng.Next(3), at = 0.8 + rng.NextDouble() * Math.Max(0.5, dur - 2), kind = kinds[rng.Next(kinds.Length)], len = 0.5 + rng.NextDouble() * 0.8, shift = 0.18 + rng.NextDouble() * 0.2 };
			if (rng.NextDouble() < ALL3) mis.kind = "all3";
		}
		Ev((restart ? "RESTART -> " : "ROOM ") + $"{room} try {tries[room]} (beat {str(Math.Round(bt, 2))}, start on {str(Math.Round(startBeat, 2))})" + (mis != null ? $" plan: {mis.kind} by p{mis.who} at {str(Math.Round(mis.at, 1))}s" : "")
			+ (STORY ? (FailTry ? " STORY fail" : SolveTry ? " STORY solve" : " STORY replay") : ""));
		if (restart) { nRestarts++; Say("رجعوا لبداية الصفحة، ينتظرون الإيقاع"); }
		if (STORY && restart && lastWasFail) Say(room != failRoom ? "الصفحة تعيد: يرجعون لأول غرفة فيها، ويعيدون اللي حلوه" : "الصفحة تعيد من أولها", true);
		if (STORY && !startSaid) { startSaid = true; foreach (var x in storyStart) Cap(x, 3.6); }
		lastWasFail = STORY && FailTry; failRoom = room;
	}
	// room time on the clean run's clock at this beat (-1 before its start)
	double Clock(double bt)
	{
		if (refOut != null || !refB.TryGetValue(room, out var B) || B.Count < 2) return frames / 60.0;
		double rb = bt - dB; if (rb < B[0] - 1e-9) return -1;
		if (rb >= B[^1]) return (B.Count - 1) / 60.0 + (rb - B[^1]) * L.spb;
		int lo = 0, hi = B.Count - 1; while (hi - lo > 1) { int m = (lo + hi) / 2; if (B[m] <= rb) lo = m; else hi = m; }
		double f = B[hi] > B[lo] ? (rb - B[lo]) / (B[hi] - B[lo]) : 0; return (lo + f) / 60.0;
	}
	static int Hash(int a, int b, int c) => (int)(((uint)(int.TryParse(Env("SEED"), out var s) ? s : 1) * 73856093u) ^ ((uint)a * 19349663u) ^ ((uint)b * 83492791u) ^ ((uint)c * 2654435761u));
	static double Gauss(Random r) { double u = 1 - r.NextDouble(), v = r.NextDouble(); return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v); }

	// one player's buttons from the route, at room time t (with its timing errors and this try's mistake)
	G.TIn Play(int k, double t)
	{
		var tm = G.tm; var b = tm.b[k]; var rr = routes.GetValueOrDefault(room)?[k];
		string kk = "";
		if (rr != null)
		{
			double sh = 0;
			if (mis != null && mis.who == k && mis.kind is "late" or "early" && t >= mis.at) sh = mis.kind == "late" ? mis.shift : -mis.shift;
			for (int j = 0; j < rr.Length; j++) { double at = rr[j].t + jit[k][j] + (rr[j].t >= (mis?.at ?? 1e9) ? sh : 0); if (at <= t + 1e-9) kk = rr[j].k; else if (rr[j].t > t + 1) break; }
		}
		foreach (var c in chaos) if ((c.who == k || c.kind == "pile") && t >= c.at && t < c.at + c.len && !c.done) return Chaos(k, c, t);
		if (STORY && SolveTry && St.forceWho == k && t >= St.forceAt && !forced1) { forced1 = true; if (St.forceSay != null) Cap(St.forceSay, 3.6); cause[k] = "story"; Force(k, "story"); }
		if (mis != null && (mis.who == k || mis.kind == "all3") && t >= mis.at && !mis.done)
		{
			if (!mis.hit) { mis.hit = true; nMistakes++; Say(mis.kind switch { "all3" => "غلطة متعمدة: الثلاثة يرمون نفسهم", "freeze" => $"غلطة متعمدة: {NM[mis.who]} يوقف فجأة", "late" => $"غلطة متعمدة: {NM[mis.who]} يتأخر", "early" => $"غلطة متعمدة: {NM[mis.who]} يستعجل", _ => $"غلطة متعمدة: {NM[mis.who]} يمشي للخطر" }); Ev($"MISTAKE {mis.kind} by p{(mis.kind == "all3" ? "all" : mis.who.ToString())}"); }
			if (mis.kind == "freeze") { if (t < mis.at + mis.len) kk = ""; else mis.done = true; }
			if (mis.kind is "danger" or "all3") return Danger(k, t);
		}
		return Keys(k, kk, b);
	}
	G.TIn Plain(int k, double t)
	{
		var rr = routes.GetValueOrDefault(room)?[k]; string kk = "";
		if (rr != null) for (int j = 0; j < rr.Length; j++) { if (rr[j].t + jit[k][j] <= t + 1e-9) kk = rr[j].k; else if (rr[j].t > t + 1) break; }
		return Keys(k, kk, G.tm.b[k]);
	}
	G.TIn Keys(int k, string kk, G.TBody b)
	{
		bool dashNow = kk.Contains('X') && !lastK[k].Contains('X'), jumpNow = kk.Contains('J') && !lastK[k].Contains('J'); lastK[k] = kk;
		var inp = new G.TIn { x = (kk.Contains('R') ? 1 : 0) - (kk.Contains('L') ? 1 : 0), y = (kk.Contains('D') ? 1 : 0) - (kk.Contains('U') ? 1 : 0), jump = jumpNow, jumpHeld = kk.Contains('J'), dash = dashNow };
		double? goal = null;
		foreach (var tok in kk.Split(' ', StringSplitOptions.RemoveEmptyEntries)) if (tok.StartsWith("@")) goal = double.Parse(tok[1..]);
		if (kk.Contains('A')) goal = Dodge(b, k);
		if (goal != null) { double d = goal.Value - b.x; inp.x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
		if (inp.x != 0 || inp.jump || inp.dash) moved[k] = true;
		return inp;
	}
	double? Dodge(G.TBody b, int k)
	{
		var bolt = G.tm.th.FirstOrDefault(s => s.k == "bolt"); var r = bolt?.of; if (r == null) return null;
		double bt = G.tmBeatPub(); long nb = (long)Math.Floor(bt) + (bt - Math.Floor(bt) > 0.25 ? 1 : 0);
		var hit = G.tmBolts(bolt, nb).ToHashSet(); int parts = (int)Math.Round(r.W / G.PT);
		int at = Math.Clamp((int)Math.Floor((b.x - r.X) / G.PT), 0, parts - 1);
		if (!hit.Contains(at)) return r.X + at * G.PT + G.PT / 2 + (k - 1) * 4;
		int best = -1; double bd = 99;
		for (int j = 0; j < parts; j++) if (!hit.Contains(j) && Math.Abs(j - at) < bd) { bd = Math.Abs(j - at); best = j; }
		return best < 0 ? null : r.X + best * G.PT + G.PT / 2;
	}

	// walking into danger on purpose: spikes or a saw it can walk to on its own floor (or the red wall); none near = it freezes
	static bool SawNear(PlatSaw sw, double x, double y, double m)
	{
		double xa = Math.Min(sw.x0, sw.x1) * G.PT + G.PT / 2, xb = Math.Max(sw.x0, sw.x1) * G.PT + G.PT / 2, ya = Math.Min(sw.y0, sw.y1) * G.PT + G.PT / 2, yb = Math.Max(sw.y0, sw.y1) * G.PT + G.PT / 2;
		return x > xa - sw.r - m && x < xb + sw.r + m && y > ya - sw.r - HALF - 4 && y < yb + sw.r + HALF + 4;
	}
	double[] dangerFrom = { -1, -1, -1 }; double?[] tgt = new double?[3]; int[] tgtFor = { -1, -1, -1 };
	G.TIn Danger(int k, double t)
	{
		var tm = G.tm; var b = tm.b[k]; double feet = b.y + HALF;
		if (tgtFor[k] != gen)
		{
			tgtFor[k] = gen; double? target = null; double best = 1e9;
			for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++)
			{
				if (tm.m[y][x] != '^') continue; double top = y * G.PT;
				if (tm.P.mirror > 0 && top < feet && top > feet - G.PT - 5)
				{
					// in the mirror: walk where the reflection meets these spikes
					double rx = 2 * tm.P.mirror - (x * G.PT + G.PT / 2);
					if (Math.Abs(rx - b.x) < best && Math.Abs(rx - b.x) < 500 && Walkable(b.x, rx, feet)) { best = Math.Abs(rx - b.x); target = rx; }
					continue;
				}
				if (top < feet - 5 || top > feet + 45) continue;
				double cx = x * G.PT + G.PT / 2, edge = cx - Math.Sign(cx - b.x) * (G.PT / 2 + 3);
				if (Math.Abs(cx - b.x) < best && Math.Abs(cx - b.x) < 500 && (Math.Abs(edge - b.x) < 6 || Walkable(b.x, edge, feet))) { best = Math.Abs(cx - b.x); target = cx; }
			}
			for (double x = b.x - 500; x <= b.x + 500; x += 10)
				foreach (var sw in tm.P.saws) if (SawNear(sw, x, b.y, 0) && Math.Abs(x - b.x) < best && Walkable(b.x, x - Math.Sign(x - b.x) * 20, feet)) { best = Math.Abs(x - b.x); target = x; }
			if (tm.chaseX > -900 && Math.Abs(b.x - tm.chaseX) < best) target = tm.chaseX - 30;
			tgt[k] = target;
			Ev(target == null ? $"p{k}: no danger it can walk to, it freezes instead" : $"p{k} walks into danger at x={str(Math.Round(target.Value))}");
		}
		var inp = new G.TIn(); moved[k] = true; var tg = tgt[k];
		double since = mis != null && !mis.done ? mis.at : t - 1; if (dangerFrom[k] < 0 || tgtFor[k] != gen) dangerFrom[k] = t;
		if (tg == null || t > dangerFrom[k] + 4) { if (mis != null && t > mis.at + 1.5) mis.done = true; return inp; }
		double dx = tg.Value - b.x; inp.x = Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1);
		return inp;
	}
	bool Mine(int k) => me < 0 || k == me;
	void Force(int k, string why)
	{
		var b = G.tm.b[k]; if (b.down || b.gate || !Mine(k)) return; nForced++; Ev($"FORCED fall of p{k} ({why})"); b.inv = 0; b.hp = 1;
		cause[k] = "forced"; typeof(G).GetMethod("tmDamage", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { b });
	}

	// someone is down: the nearest one goes to touch them (after a person's reaction), the rest wait where they are;
	// back up: the two go back where they were and all three start again on the beat
	G.TIn[] Help(G.TIn[] ins, List<int> downs, double bt)
	{
		var tm = G.tm;
		if (pausedAt < 0) { pausedAt = Math.Max(0, tau); pausedRb = bt - dB; Ev($"pause at room time {str(Math.Round(tau, 2))}"); }
		if (downs.Count > 0)
		{
			Mode = "help"; regroupTill = -1; resumeBeat = -1;
			int d = downs[0]; var db = tm.b[d];
			bool reachable = db.y < tm.h * G.PT - 4;
			if (helpOf != d) { helpOf = d; helper = -1; helpFrom = tm.t + REACT; touch = 0; said = false; }
			if (helper < 0 && reachable)
			{
				double bd = 1e9; for (int k = 0; k < 3; k++) { var o = tm.b[k]; if (o.down || k == d || !o.ground) continue; var sp = Spot(o, db); double dd = Math.Abs(o.x - db.x); if (sp != null && dd < bd && dd < 600) { bd = dd; helper = k; spot = sp.Value; } }
				if (helper >= 0) Say($"{NM[helper]} رايح ينعش {NM[d]}", true);
				if (helper >= 0) Ev($"p{helper} goes to bring p{d} back ({str(Math.Round(bd))} px)");
				else if (!said) { said = true; Say($"ما فيه طريق آمن لـ{NM[d]}: ينتظرون ٦ ثواني", !waitSaid); waitSaid = true; Ev($"no safe way to p{d}: the others wait"); }
			}
			if (CHAOS && lateHelp && helper >= 0 && helpFrom < db.downAt + 3.6) { helpFrom = db.downAt + 3.6; Ev($"chaos: p{helper} waits to bring p{d} back near the end of the 6 s"); }
			if (helper >= 0 && tm.t >= helpFrom && !tm.b[helper].down)
			{
				ins[helper] = Walk(helper, spot, tm.b[helper].y, true);
				// chaos: let go after 0.9 s of touching (not enough), step away, then come back
				if (CHAOS && leaveTouch && touchT[d] > 0.9) { leaveTouch = false; awayTill = tm.t + 0.5; Ev($"chaos: p{helper} lets go of p{d} after 0.9 s"); }
				if (tm.t < awayTill) ins[helper] = new G.TIn { x = tm.b[helper].x < db.x ? -1 : 1 };
			}
			return ins;
		}
		// everyone is up again
		if (regroupTill < 0 && resumeBeat < 0) { regroupTill = tm.t + 3; Mode = "regroup"; }
		if (regroupTill >= 0)
		{
			bool there = true; int f = (int)Math.Round(pausedAt * 60);
			if (refPos.TryGetValue(room, out var rp) && rp.Count > 0)
			{
				var at = rp[Math.Clamp(f, 0, rp.Count - 1)];
				for (int k = 0; k < 3; k++) { var b = tm.b[k]; if (Math.Abs(b.x - at[k * 2]) > 6 && Math.Abs(b.y - at[k * 2 + 1]) < 120) { there = false; ins[k] = Walk(k, at[k * 2], at[k * 2 + 1], false); } }
			}
			if (there || tm.t > regroupTill)
			{
				regroupTill = -1; int P = per.GetValueOrDefault(room, 1);
				// on the beat where the clean run's things are as they were when it stopped (a whole number of repeats later)
				resumeBeat = pausedRb + Math.Ceiling((bt + 0.3 - pausedRb) / P) * P;
				Ev($"back up: all start again on beat {str(Math.Round(resumeBeat, 2))}" + (there ? "" : " (not all back in place)"));
			}
			return ins;
		}
		Mode = "count";
		if (bt + 1e-9 < resumeBeat) return ins;
		dB = resumeBeat - pausedRb; pausedAt = -1; resumeBeat = -1; helper = helpOf = -1; Mode = "play";
		for (int k = 0; k < 3; k++) lastK[k] = "";
		return ins;
	}
	// where on the helper's own floor it can stand to touch the fallen one (within reach, nothing hurting on the way)
	double? Spot(G.TBody o, G.TBody d)
	{
		double feet = o.y + HALF;
		foreach (double off in new[] { 0.0, -8, 8, -16, 16, -22, 22 })
		{
			double x = d.x + off; if (hypot(x - d.x, o.y - d.y) > 27) continue;
			if (Walkable(o.x, x, feet)) return x;
		}
		return null;
	}
	bool Walkable(double x0, double x1, double feet)
	{
		var tm = G.tm; int ty = (int)Math.Floor((feet + 2) / G.PT), tb = (int)Math.Floor((feet - HALF) / G.PT);
		for (double x = Math.Min(x0, x1); x <= Math.Max(x0, x1) + 0.1; x += 6)
		{
			int tx = (int)Math.Floor(x / G.PT); if (tx < 0 || tx >= tm.w || ty < 0 || ty >= tm.h || tb < 0) return false;
			char c = tm.m[tb][tx]; if (c is '#' or 'I' or '^' or 'v') return false;
			if (tm.P.mirror > 0) { double rx = 2 * tm.P.mirror - x; foreach (double e in new[] { -HALF - 7, 0, HALF + 7 }) { int rt = (int)Math.Floor((rx + e) / G.PT); if (rt >= 0 && rt < tm.w && tm.m[tb][rt] is '^' or 'v') return false; } }   // the reflection must not meet spikes either
			bool floor = tm.m[ty][tx] is '#' or 'I' || tm.th.Any(s => s.solid && s.one && Math.Abs(s.Y - feet) < 4 && x > s.X && x < s.X + s.W) || tm.th.Any(s => s.solid && !s.one && Math.Abs(s.Y - feet) < 4 && x > s.X && x < s.X + s.W);
			if (!floor) return false;
			foreach (var sw in tm.P.saws) if (SawNear(sw, x, feet - HALF, 14)) return false;
		}
		return true;
	}
	// walk to a place (and jump up to it, or over what blocks the way); touching = stop on it
	G.TIn Walk(int k, double x, double y, bool touchIt)
	{
		var b = G.tm.b[k]; double dx = x - b.x, dy = y - b.y; var inp = new G.TIn();
		if (touchIt) { inp.x = Math.Abs(dx) < 2 ? 0 : Math.Clamp(dx / 25, -1, 1); return inp; }
		inp.x = Math.Abs(dx) < 3 ? 0 : Math.Clamp(dx / 25, -1, 1);
		bool blocked = Math.Abs(b.vx) < 30 && Math.Abs(dx) > 25 && b.ground;
		if (b.ground && ((dy < -30 && Math.Abs(dx) < 120) || blocked)) { inp.jump = true; inp.jumpHeld = true; }
		else if (!b.ground && b.vy < 0) inp.jumpHeld = true;
		return inp;
	}

	// ---------- after each step: what happened, and the level's rules ----------
	public void After(double dt)
	{
		var tm = G.tm; double bt = G.tmBeatPub();
		if (refOut != null && refPos.TryGetValue(room, out var rp) && tm.gen == gen && Mode == "play") rp.Add(tm.b.SelectMany(b => new[] { b.x, b.y }).ToArray());
		if (tm.won && !won) { won = true; Say("خلصوا المرحلة"); if (STORY && storyWin != null) Cap(storyWin, 6); Ev($"WON after {nRestarts} restarts, {nDeaths} falls; letters {string.Join(" ", Enumerable.Range(0, 3).Select(G.tmRank))}"); Check(); }
		if (tm.won && tm.t - tm.wonAt > (STORY ? 12 : 8)) over = true;
		for (int k = 0; k < 3; k++)
		{
			var b = tm.b[k];
			if (!Mine(k)) { if (b.down && downAt[k] < 0) downAt[k] = tm.t; if (!b.down) downAt[k] = -1; continue; }
			if (b.down && downAt[k] < 0)
			{
				downAt[k] = tm.t; nDeaths++; touchT[k] = 0;
				string c = cause[k] ?? Cause(b); cause[k] = null;
				if (STORY && FailTry && St.failSay != null && !failSaid) { failSaid = true; Cap(St.failSay, 4.4); }
				else if (c != "forced") Say(tm.b.Count(o => o.down) == 3 ? "طاحوا الثلاثة: الصفحة تعيد على طول" : $"{NM[k]} طاح", true);
				Ev($"DOWN p{k} at ({str(Math.Round(b.x))},{str(Math.Round(b.y))}) cause={c}");
				if (c == "nothing") Bug("down-nothing-" + tm.page, $"p{k} fell with nothing hurting it (no spikes, saw, ice, lightning, wall or chain near; on screen at {str(Math.Round(b.x))},{str(Math.Round(b.y))})");
				if (tm.t - buildAt < 1.6 && !moved[k]) Bug("down-spawn-" + tm.page, $"p{k} fell {str(Math.Round(tm.t - buildAt, 2))} s after the page started while still standing at the start (cause={c})");
			}
			if (!b.down && downAt[k] >= 0)
			{
				double held = touchT[k]; downAt[k] = -1;
				if (tm.gen == gen) { nRevives++; Say($"{NM[k]} رجع بعد ثانية لمس", true); Ev($"UP p{k} (touched {str(Math.Round(held, 2))} s)"); if (held < 0.9) Bug("rev-short", $"p{k} came back after only {str(Math.Round(held, 2))} s of touching (needs 1 s)"); }
			}
			if (b.down)
			{
				bool near = tm.b.Any(o => o != b && !o.down && !o.gate && hypot(o.x - b.x, o.y - b.y) < 30);
				touchT[k] = near && tm.restartAt < 0 && tm.doneAt < 0 ? touchT[k] + dt : 0;
				if (touchT[k] > 1.25) Bug("rev-none", $"p{k} touched for {str(Math.Round(touchT[k], 2))} s and still down");
				if (tm.t - downAt[k] > G.TM_REVIVE + (me < 0 ? 0.3 : 1.2) && tm.restartAt < 0 && tm.doneAt < 0) Bug("no-restart", $"p{k} down {str(Math.Round(tm.t - downAt[k], 1))} s and the page did not restart");
			}
			if (double.IsNaN(b.x) || double.IsNaN(b.y)) Bug("nan", $"p{k} position is NaN");
			if (!b.down && !b.gate && (b.x < 0 || b.x > tm.w * G.PT)) Bug("oob-" + k, $"p{k} left the map sideways");
			if (!b.down && !b.gate && InWall(b)) Bug("wall-" + tm.page, $"p{k} is inside a wall tile");
		}
		if (tm.restarts != restarts0)
		{
			restarts0 = tm.restarts;
			for (int k = 0; k < 3; k++) { var b = tm.b[k]; if (!Mine(k)) continue; if (b.down || b.hp != G.P_HP || Math.Abs(b.x - tm.sx[k]) > 1 || Math.Abs(b.y - tm.sy[k]) > 1) Bug("restart-state", $"after a restart p{k} is not at the start healthy"); }
			bool why = true; // the restart's reason was checked when it was asked for
		}
		if (tm.restartAt >= 0 && Mode != "restart" && tm.gen == gen && me <= 0)
		{
			bool ok = tm.b.All(b => b.down) || tm.b.Any(b => b.down && tm.t - b.downAt > G.TM_REVIVE - 0.05) || tm.snapAt >= 0;
			if (!ok && me < 0) Bug("restart-why", "the page restarts with no reason (not all down, no one down 6 s)");
			Ev(tm.b.All(b => b.down) ? "all three down -> restart" : "down too long -> restart");
		}
		if (THINGLOG) ThingLog();
		// songs: never more stems than the level has, playing at once
		int playing = SongVoices(); overT = playing > L.stems.Length ? overT + dt : 0;
		if (overT > 0.35) Bug("song-overlap", $"{playing} song voices playing at once for {str(Math.Round(overT, 2))} s (the level has {L.stems.Length} stems): {VoiceList()}");
		// stuck: the clean route is over but the room is not, and no one is down
		var rr = routes.GetValueOrDefault(room);
		double end = rr == null ? 0 : rr.Max(x => x?.Length > 0 ? x[^1].t : 0);
		if (Mode == "play" && tau > end + 6 && tm.doneAt < 0 && !tm.won)
		{
			Ev($"STUCK in room {room} (route over {str(Math.Round(tau - end, 1))} s ago); falling on purpose to try again");
			for (int k = 0; k < 3; k++) Force(k, "stuck");
		}
	}
	// env THINGLOG=1: every change of the room's things and of what each square does (to write the video's lines)
	readonly bool THINGLOG = Env("THINGLOG") == "1"; readonly Dictionary<string, string> thingWas = new(); int thingGen = -1;
	void ThingLog()
	{
		var tm = G.tm; if (thingGen != tm.gen) { thingGen = tm.gen; thingWas.Clear(); }
		void C(string key, string v) { if (thingWas.TryGetValue(key, out var o) && o == v) return; bool first = !thingWas.ContainsKey(key); thingWas[key] = v; if (!first) Ev($"T {str(Math.Round(tau, 2))} {key}={v}"); }
		for (int i = 0; i < tm.th.Count; i++)
		{
			var q = tm.th[i];
			string id = q.d?.id ?? ""; string v = $"{(q.on ? "on" : "")}{(q.solid ? "" : "-open")}{(q.gone ? "-gone" : "")}{(q.melt ? "-melt" : "")}";
			if (q.k == "scale") v = q.s > 0.98 ? "L-down" : q.s < -0.98 ? "L-up" : "mid"; if (q.k == "ride") v = q.n + "on"; if (q.k == "mover" || q.k == "chase") continue;
			C($"{q.k}{i}{(id != "" ? "(" + id + ")" : "")}@{Math.Round(q.X)},{Math.Round(q.Y)}", v);
		}
		for (int k = 0; k < 3; k++) { var b = tm.b[k]; if (b.onB != null || b.ground) C($"p{k}.on", b.onB != null ? "head of p" + b.onB.i : b.onT?.k ?? "ground"); C($"p{k}.carry", b.carry != null ? "pic" : "-"); C($"p{k}.gate", b.gate ? "in" : "-"); }
		C("got", tm.got.Count.ToString()); C("flips", string.Join(",", tm.flips)); C("chain", tm.snapAt >= 0 ? "snap" : "ok");
	}
	static FieldInfo voicesF =typeof(AudioCtx).GetField("voices", BindingFlags.NonPublic | BindingFlags.Static);
	static FieldInfo doneF = typeof(Voice).GetField("done", BindingFlags.NonPublic | BindingFlags.Instance);
	double overT;
	public static string VoiceList()
	{
		var vs = (List<Voice>)voicesF.GetValue(null); double now = AudioCtx.currentTime; var o = new List<string>();
		lock (AudioCtx.L) foreach (var v in vs) if (v.song != null && !(bool)doneF.GetValue(v) && v.startAt <= now && v.stopAt > now && v.gain.At(now) > 0.02)
			o.Add($"{v.song.ResourcePath.Split('/')[^1]} from {str(Math.Round(v.startAt, 1))} gain {str(Math.Round(v.gain.At(now), 2))}" + (v.loop ? " loop" : ""));
		return string.Join("; ", o);
	}
	public static int SongVoices()
	{
		var vs = (List<Voice>)voicesF.GetValue(null); double now = AudioCtx.currentTime; int n = 0;
		lock (AudioCtx.L) foreach (var v in vs) if (v.song != null && !(bool)doneF.GetValue(v) && v.startAt <= now && v.stopAt > now && v.gain.At(now) > 0.02) n++;
		return n;
	}
	bool InWall(G.TBody b)
	{
		var tm = G.tm; int tx = (int)Math.Floor(b.x / G.PT), ty = (int)Math.Floor(b.y / G.PT);
		if (tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h) return false;
		return tm.m[ty][tx] == '#' && (int)Math.Floor((b.x - HALF + 2) / G.PT) == tx && (int)Math.Floor((b.x + HALF - 2) / G.PT) == tx;
	}
	// why a square fell (the game's own dangers, looked at from here)
	string Cause(G.TBody b)
	{
		var tm = G.tm;
		if (tm.snapAt >= 0) return "chain";
		if (b.y > tm.h * G.PT) return "drop";
		if (tm.chaseX > -900 && b.x - 6 < tm.chaseX) return "wall";
		for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
		{
			int tx = (int)Math.Floor(b.x / G.PT) + dx, ty = (int)Math.Floor(b.y / G.PT) + dy;
			if (tx >= 0 && ty >= 0 && tx < tm.w && ty < tm.h && tm.m[ty][tx] is '^' or 'v') return "spikes";
			if (tm.P.mirror > 0) { int rx = (int)Math.Floor(G.tmRx(b.x) / G.PT) + dx; if (rx >= 0 && ty >= 0 && rx < tm.w && ty < tm.h && tm.m[ty][rx] is '^' or 'v') return "spikes(mirror)"; }
		}
		foreach (var s in tm.P.saws) { double x = Saw(s, out double y); if (hypot(b.x - x, b.y - y) < s.r + 20 || (tm.P.mirror > 0 && hypot(G.tmRx(b.x) - x, b.y - y) < s.r + 20)) return "saw"; }
		if (tm.hail.Any(q => Math.Abs(q.x - b.x) < 24 && Math.Abs(q.y - b.y) < 30)) return "ice";
		foreach (var s in tm.th) if (s.k == "bolt" && G.tmBoltLive(s) && b.y > s.Y - 10 && b.y < s.Y + s.H + 10) return "lightning";
		return "nothing";
	}
	static double Saw(PlatSaw s, out double y)
	{
		double b = G.tmBeatPub() / s.beats + s.ph, f = b - Math.Floor(b), k = f < 0.5 ? f * 2 : 2 - f * 2; k = k * k * (3 - 2 * k);
		y = (s.y0 + (s.y1 - s.y0) * k) * G.PT + G.PT / 2; return (s.x0 + (s.x1 - s.x0) * k) * G.PT + G.PT / 2;
	}
	void Check()
	{
		var tm = G.tm;
		// each one's letter by their own falls
		if (tm.falls.Sum() != nDeaths) Bug("falls", $"falls on the win screen {string.Join(",", tm.falls)} but {nDeaths} falls seen");
		for (int k = 0; k < 3; k++) { string want = tm.falls[k] >= 3 ? "C" : tm.falls[k] == 2 ? "B" : tm.falls[k] == 1 ? "A" : "S"; if (G.tmRank(k) != want) Bug("rank", $"p{k} letter {G.tmRank(k)} with {tm.falls[k]} falls"); }
	}
}
