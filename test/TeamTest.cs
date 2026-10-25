using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// The co-op level with three players and no people: -- --team-run <levelId> <page> <outDir> <route0> <route1> <route2> [fps=60] [every=1]
// Each route is PlatTest's script for one player ("<seconds> <keys>", keys L R U D J X).
// env NODRAW=1: no pictures (fast, for working out routes); env SKINS: the players' looks; env WAV=<file>: also writes the game's sound; env T=<seconds>: how long.
// Writes state.log: time beat, then per player x y hp (d = down, g = in the gate), then events.
public class TeamTest
{
	string id, outDir; int page, fps = 60, every = 1, i; double T; bool done, shot, nodraw = System.Environment.GetEnvironmentVariable("NODRAW") == "1";
	int hits0; string[] routes; (int room, double t, string k)[][] lines; System.Collections.Generic.Dictionary<int, double> entry = new(); G.TeamRun lastTm; string[] lastK = new string[G.TM_N]; StringBuilder log = new(); Main host;
	double from = double.TryParse(System.Environment.GetEnvironmentVariable("FROM"), out var fr) ? fr : 0;   // env FROM: pictures only after this time
	string view; int pass = -1; bool[] beatJ = new bool[G.TM_N]; double[] heldTo = new double[G.TM_N]; bool[] hop = new bool[G.TM_N]; string[] lastK2 = { "", "", "" }; string[] qDone = { "", "", "" }; int[] side = { -1, -1, -1 }; bool[] oWasGround = new bool[3]; double[] waitT = new double[3], waitAt = { -1, -1, -1 }; int[] waitRm = { -1, -1, -1 };   // env VIEW: a player's screen (0-2), or "cycle" (each one's screen in turn in the dark rooms); else everyone's
	BinaryWriter wav = System.Environment.GetEnvironmentVariable("WAV") != null ? new BinaryWriter(new MemoryStream()) : null;
	// env RPT=1: records the replay while the bots play, then checks watching it shows the same at every tick (like TeamBot's)
	bool rpt = System.Environment.GetEnvironmentVariable("RPT") == "1"; System.Collections.Generic.List<string> live = new();
	static string RpStr()
	{
		var tm = G.tm; var o = new StringBuilder(); o.Append('p').Append(tm.page).Append(" t").Append(Math.Round(tm.t, 2)).Append(" got").Append(tm.got.Count).Append(tm.won ? " W" : "");
		foreach (var b in tm.b) o.Append(" |").Append(Math.Round(b.x)).Append(',').Append(Math.Round(b.y)).Append(b.down ? "d" : "").Append(b.gate ? "g" : "").Append(b.hang ? "H" : "").Append(" h").Append(b.hp);
		foreach (var s in tm.th) o.Append(" ").Append(s.k[0]).Append(Math.Round(s.X)).Append(',').Append(Math.Round(s.Y)).Append(s.on ? "+" : "").Append(s.solid ? "" : "~");
		o.Append(" m").Append(Math.Round(tm.murk, 2)).Append(" rise").Append(double.IsNaN(tm.riseY) ? "-" : Math.Round(tm.riseY).ToString()).Append(tm.ropeLive ? " R" : "").Append(" sh").Append(tm.shield);
		foreach (var w in tm.bars) o.Append(" w").Append(w.dir > 0 ? '>' : '<').Append(Math.Round(w.x)).Append('/').Append(w.r0).Append(':').Append(string.Concat(w.on.Select(q => q ? '1' : '0')));
		return o.ToString();
	}
	static bool Near(string a, string b)   // a pixel apart at most (kept as 4-byte numbers)
	{
		var x = System.Text.RegularExpressions.Regex.Split(a, "(-?[0-9.]+)"); var y = System.Text.RegularExpressions.Regex.Split(b, "(-?[0-9.]+)"); if (x.Length != y.Length) return false;
		for (int i = 0; i < x.Length; i++) { if (x[i] == y[i]) continue; if (i % 2 == 1 && double.TryParse(x[i], out var p) && double.TryParse(y[i], out var q) && Math.Abs(p - q) <= 1.01) continue; return false; }
		return true;
	}
	void RpCheck()
	{
		var o = new StringBuilder();
		G.MYRP_DIR = "user://rpt"; var dd = DirAccess.Open(G.MYRP_DIR); if (dd != null) foreach (var f in dd.GetFiles()) dd.Remove(f);
		bool ok = G.tmRpSavePub(G.tm.won ? "win" : "mid"); G.myRpLoadPub(); var its = G.myRpsPub();
		o.Append($"saved {ok}, files {its.Count}, ticks {G.tmRec.n}, live {live.Count}\n");
		if (its.Count == 1)
		{
			var it = its[0]; o.Append($"kind {it.kind} res {it.res} r {it.r} len {it.len:F1}s size {it.segs[0].d.Length / 1024} KB\n");
			G.myRpPlayPub(it); int bad = 0, n = live.Count;
			for (int k = 0; k < n; k++)
			{
				G.tmRpApplyPub(k / (double)G.TM_HZ); string r = RpStr();
				if (!Near(r, live[k])) { if (bad++ < 5) o.Append($"tick {k}:\n  live   {live[k]}\n  replay {r}\n"); }
			}
			o.Append($"ticks that differ: {bad} of {n}\n");
			File.Copy(ProjectSettings.GlobalizePath(G.MYRP_DIR + "/" + it.file), Path.Combine(outDir, "replay.json"), true);
		}
		File.WriteAllText(Path.Combine(outDir, "rpt.log"), o.ToString());
	}

	// the ride's part (0-3) this player should be on for the next strike: stay if it is safe, else the nearest safe part
	double? dodge(G.TBody b, int k)
	{
		var bolt = G.tm.th.FirstOrDefault(s => s.k == "bolt"); var r = bolt?.of; if (r == null) return null;
		double bt = G.tmBeatPub(); long nb = (long)Math.Floor(bt) + (bt - Math.Floor(bt) > 0.25 ? 1 : 0);
		var hit = G.tmBolts(bolt, nb).ToHashSet(); int parts = (int)Math.Round(r.W / G.PT);
		int me = Math.Clamp((int)Math.Floor((b.x - r.X) / G.PT), 0, parts - 1);
		if (!hit.Contains(me)) return r.X + me * G.PT + G.PT / 2 + (k - 1) * 4;
		int best = -1; double bd = 99;
		for (int j = 0; j < parts; j++) if (!hit.Contains(j) && Math.Abs(j - me) < bd) { bd = Math.Abs(j - me); best = j; }
		return best < 0 ? null : r.X + best * G.PT + G.PT / 2;
	}
	public static TeamTest FromArgs(string[] a, int k)
	{
		var r = new TeamTest { id = a[k], page = int.Parse(a[k + 1]), outDir = a[k + 2], routes = new[] { a[k + 3], a[k + 4], a[k + 5] } };
		if (a.Length > k + 6 && int.TryParse(a[k + 6], out var f)) r.fps = f;
		if (a.Length > k + 7 && int.TryParse(a[k + 7], out var e)) r.every = e;
		return r;
	}
	public void Start(Main h)
	{
		host = h; Directory.CreateDirectory(outDir);
		localStorage.Memory = true; G.padsDisabled = true; G.classicUi = false;
		performance.manual = 100000; AudioCtx.manualTime = 0; JS.SeedRandom(7);
		G.InitSound(); G.gfx.Resize(1280, 720); G.scale = 1; G.offX = 0; G.offY = 0; G.gatePassed = true;
		// "ROOM <page>" starts a part whose times count from when that page (room) begins
		lines = routes.Select(r => { int room = page; var o = new System.Collections.Generic.List<(int, double, string)>();
			foreach (var l in File.ReadAllLines(r).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")))
			{ var f = l.Trim().Split(' ', 2); if (f[0] == "ROOM") { room = int.Parse(f[1].Split('#')[0].Trim()); continue; } o.Add((room, double.Parse(f[0]), f.Length > 1 ? f[1].Split('#')[0].Trim() : "")); }
			return o.ToArray(); }).ToArray();
		for (int k = 0; k < G.TM_N; k++) lastK[k] = "";
		T = double.TryParse(System.Environment.GetEnvironmentVariable("T"), out var tt) ? tt : lines.Max(l => l.Length > 0 ? l[^1].t : 0) + 3;
		G.ensureAudio(); G.stopMenu();
		G.tmStart(G.LoadTeam(id), page); G.state = "team";
		// env SKINS: each player's look, a comma list (e.g. "crystal,skull,") for pictures of the looks in their colours
		if (System.Environment.GetEnvironmentVariable("SKINS") is string sks) { var a = sks.Split(','); for (int k = 0; k < G.TM_N; k++) G.tm.skins[k] = k < a.Length ? a[k] : ""; }
		if (rpt) G.tmRecStartPub();
		view = System.Environment.GetEnvironmentVariable("VIEW");
	}
	// one player's buttons at time t (rt: since page rm began), from its route; online (test/TeamNetTest.cs DUO) one of these per PC
	public G.TIn Input(int k, double t, int rm, double rt)
	{
		var inp = new G.TIn();
		// "W<id>": the route's clock stops on this line till key <id> is down as this PC shows it (a person waits for the partner's path)
		if (waitRm[k] != rm) { waitRm[k] = rm; waitT[k] = 0; waitAt[k] = -1; }
		string kk = ""; foreach (var l in lines[k]) if (l.room == rm && l.t <= rt - waitT[k] + 1e-9) kk = l.k;
		var wTok = kk.Split(' ').FirstOrDefault(q => q.Length > 1 && q[0] == 'W');
		if (wTok != null && !G.tmPressedPub(wTok[1..]) && waitAt[k] >= 0) waitT[k] += t - waitAt[k];
		waitAt[k] = t;
		bool dashNow = kk.Contains('X') && !lastK[k].Contains('X'), jumpNow = kk.Contains('J') && !lastK[k].Contains('J'); lastK[k] = kk;
		// "J!": the jump waits for the next beat (the radio's waves need a jump on the beat)
		// "j!": the same, a short hop (the jump let go early, so it lands before the next beat)
		if (kk.Contains("j!") && !lastK2[k].Contains("j!")) { beatJ[k] = true; hop[k] = true; }
		if (jumpNow && kk.Contains("J!")) { beatJ[k] = true; hop[k] = false; jumpNow = false; }
		if (beatJ[k]) { double bt = G.tmBeatPub(), nb = bt + 1.0 / fps / G.tm.L.spb; if (Math.Floor(nb) != Math.Floor(bt)) { jumpNow = true; beatJ[k] = false; heldTo[k] = t + (hop[k] ? 0.18 : 0.3); } }
		lastK2[k] = kk;
		inp = new G.TIn { x = (kk.Contains('R') ? 1 : 0) - (kk.Contains('L') ? 1 : 0), y = (kk.Contains('D') ? 1 : 0) - (kk.Contains('U') ? 1 : 0), jump = jumpNow, jumpHeld = kk.Contains('J') || t < heldTo[k], dash = dashNow };
		// "@<x>": walk to x and stop there; "A": on the ride, step to a part the next lightning misses
		var b = G.tm.b[k]; double? goal = null;
		foreach (var tok in kk.Split(' ', StringSplitOptions.RemoveEmptyEntries)) if (tok.StartsWith("@")) goal = double.Parse(tok[1..]);
		if (kk.Contains('A')) goal = dodge(b, k);
		if (goal != null) { double d = goal.Value - b.x; inp.x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
		// the shield page: "F<px>" keeps that far from the other one; "B" keeps behind the shield (away from the nearest wave
		// coming); "C" stops while a wave comes close (the one carrying the shield waits for the other); "K" jumps spikes and
		// low rock ahead; "M" jumps when the other one jumps; "D" holds down (takes the shield when touching)
		{
			var o = G.tm.b[k == 0 ? 1 : 0]; var tks = kk.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (var tok in tks) if (tok.Length > 1 && tok[0] == 'F' && double.TryParse(tok[1..], out var off)) goal = o.x + off;
			if (tks.Contains("B"))
			{
				MG.TBar best = null; double bt = 99;
				foreach (var w in G.tm.bars) { double d = (b.x - w.x) * w.dir; if (d > -10 && d / w.v < bt && w.on.Where((on, r) => on && Math.Abs((w.r0 + r) * G.PT + 20 - b.y) < 50).Any()) { bt = d / w.v; best = w; } }
				if (best != null) side[k] = best.dir;
				goal = o.x + side[k] * 15;
			}
			if (goal != null) { double d = goal.Value - b.x; inp.x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
			// "H": no shield, waves from ahead: run from cover to cover (low rock blocks), going on only when the next one is reached first
			if (tks.Contains("H") && goal != null)
			{
				int row = (int)Math.Floor(b.y / G.PT), dir = Math.Sign(goal.Value - b.x);
				var spots = new System.Collections.Generic.List<double>();
				for (int tx = 0; tx < G.tm.w; tx++) if (G.tm.m[row][tx] == '#' && row >= 2 && G.tm.m[row - 2][tx] != '#') spots.Add(dir > 0 ? tx * G.PT - 11 : (tx + 1) * G.PT + 11);
				MG.TBar w0 = null; double eta = 99;
				foreach (var w in G.tm.bars) { double d = (w.x - b.x) * dir; if (w.dir == -dir && d > -5 && d / w.v < eta && w.on.Where((on, r) => on && Math.Abs((w.r0 + r) * G.PT + 20 - b.y) < 30).Any()) { eta = d / w.v; w0 = w; } }
				double? tgt = null;
				if (w0 != null)
				{
					double best = double.NaN;
					foreach (var sp in spots) { double reach = Math.Abs(sp - b.x) / 370 + 0.25, arrive = (w0.x - sp) * dir / w0.v; if (arrive > reach && (sp - b.x) * dir >= -2 && (double.IsNaN(best) || (sp - best) * dir > 0) && (goal.Value - sp) * dir > -5) best = sp; }
					if (double.IsNaN(best)) foreach (var sp in spots) if ((sp - b.x) * dir < 0 && (double.IsNaN(best) || (sp - best) * dir > 0)) best = sp;
					if (!double.IsNaN(best) && (eta < 1.6 || Math.Abs(best - b.x) < 3)) tgt = best;
				}
				if (tgt != null) { double d = tgt.Value - b.x; inp.x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
			}
			if (tks.Contains("C"))
				foreach (var w in G.tm.bars) { double d = (b.x - w.x) * w.dir; if (d > 0 && d < 170 && Math.Abs(w.r0 * G.PT - b.y) < 200) inp.x = 0; }
			if (tks.Contains("K") && b.ground && Math.Abs(inp.x) > 0.2)
			{
				int dir = Math.Sign(inp.x), fx = (int)Math.Floor((b.x + dir * 26) / G.PT), fy = (int)Math.Floor((b.y + 20) / G.PT), my = (int)Math.Floor(b.y / G.PT);
				char cf = G.tm.m[fy][fx], cm = G.tm.m[my][fx];
				if (cf == '^' || cm == '#') { inp.jump = true; inp.jumpHeld = true; heldTo[k] = t + 0.3; }
			}
			if (tks.Contains("M") && o.vy < -700 && oWasGround[k] && !o.ground && b.ground) { inp.jump = true; inp.jumpHeld = true; heldTo[k] = t + 0.3; }
			oWasGround[k] = o.ground;
			if (tks.Contains("D")) inp.y = 1;
		}
		// "Z": wait while a saw is near the way to the goal and not going away from it
		if (kk.Split(' ').Contains("Z") && goal != null)
			foreach (var sw in G.tm.P.saws)
			{
				double sx = G.tmSawPosPub(sw, 0, out double sy), sx2 = G.tmSawPosPub(sw, 0.1, out _), dir = Math.Sign(goal.Value - b.x);
				bool ahead = (sx - b.x) * dir > -40 && (sx - b.x) * dir < 160, away = (sx2 - sx) * dir > 0;
				if (Math.Abs(sy - b.y) < 70 && ahead && !(away && (sx - b.x) * dir > 60)) inp.x = 0;
			}
		// "S": jump a saw coming at you (standing on a key under one)
		if (kk.Split(' ').Contains("S") && b.ground)
			foreach (var sw in G.tm.P.saws)
			{
				double sx = G.tmSawPosPub(sw, 0, out double sy), sx2 = G.tmSawPosPub(sw, 0.12, out _);
				if (Math.Abs(sy - b.y) < 40 && Math.Abs(sx2 - b.x) < sw.r + 26 && Math.Abs(sx2 - b.x) <= Math.Abs(sx - b.x) + 1) { inp.jump = true; inp.jumpHeld = true; heldTo[k] = t + 0.25; }
			}
		// the cable: "P" pumps the swing (pushes the way it goes), "Q<deg>" lets go with a jump once the swing is that far up (moving right; "q" moving left)
		if (kk.Contains('P') && b.hang) inp.x = Math.Abs(b.vx) < 30 ? 1 : Math.Sign(b.vx);
		foreach (var tok in kk.Split(' ', StringSplitOptions.RemoveEmptyEntries)) if ((tok[0] == 'Q' || tok[0] == 'q') && tok.Length > 1 && (b.hang || b.ropeC > 0))
		{
			var o = G.tm.b[1 - k]; double th = Math.Atan2(b.x - o.x, b.y - o.y) * 180 / Math.PI; bool right = tok[0] == 'Q';
			if ((right ? b.vx > 0 && th >= double.Parse(tok[1..]) : b.vx < 0 && -th >= double.Parse(tok[1..])) && t >= heldTo[k] && qDone[k] != kk) { qDone[k] = kk; inp.jump = true; inp.jumpHeld = true; heldTo[k] = t + 0.35; inp.x = right ? 1 : -1; }
		}
		return inp;
	}
	public static TeamTest ForOne(string route, int me, int page)
	{
		var r = new TeamTest { page = page }; var o = new System.Collections.Generic.List<(int, double, string)>(); int room = page;
		foreach (var l in File.ReadAllLines(route).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")))
		{ var f = l.Trim().Split(' ', 2); if (f[0] == "ROOM") { room = int.Parse(f[1].Split('#')[0].Trim()); continue; } o.Add((room, double.Parse(f[0]), f.Length > 1 ? f[1].Split('#')[0].Trim() : "")); }
		r.lines = new (int, double, string)[G.TM_N][]; for (int k = 0; k < G.TM_N; k++) { r.lines[k] = k == me ? o.ToArray() : new (int, double, string)[0]; r.lastK[k] = ""; }
		return r;
	}
	void duoShot()
	{
		var g = G.g; if (pass > 0) G.gfx.BeginFrame(); g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, 1280, 720);
		double sx = (random() * 2 - 1) * G.shake, sy = (random() * 2 - 1) * G.shake; g.setTransform(1, 0, 0, 1, sx, sy);
		G.tmView = pass - 1; G.drawTeam(performance.manual.Value);
		if (G.flash > 0) { g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = $"rgba(255,255,255,{str(Math.Min(0.8, G.flash))})"; g.fillRect(0, 0, 1280, 720); }
		g.setTransform(1, 0, 0, 1, 0, 0);
		if (pass == 0) G.txt("الخريطة كاملة (للفيديو بس، ما تطلع باللعبة)", 1250, 700, 20, "rgba(255,255,255,0.7)", 700, "right");
		else G.txt(pass == 1 ? "شاشة الأزرق" : "شاشة الأصفر", 1250, 700, 34, G.tmCol(pass - 1), 800, "right");
		G.gfx.EndFrame(); shot = true;
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, pass >= 0 ? $"f{i:D6}_{pass}.png" : $"f{i:D6}.png")); shot = false; }
		// VIEW=duo: three pictures of each frame (the whole map, then each player's own screen)
		if (pass >= 0 && pass < 2) { pass++; duoShot(); return; }
		pass = -1;
		if (done)
		{
			if (rpt) RpCheck();
			File.WriteAllText(Path.Combine(outDir, "state.log"), log.ToString());
			if (wav != null)
			{
				var pcm = ((MemoryStream)wav.BaseStream).ToArray(); using var f = new BinaryWriter(File.Create(System.Environment.GetEnvironmentVariable("WAV")));
				f.Write(Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + pcm.Length); f.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); f.Write(16); f.Write((short)1); f.Write((short)2);
				f.Write(AudioCtx.SR); f.Write(AudioCtx.SR * 4); f.Write((short)4); f.Write((short)16); f.Write(Encoding.ASCII.GetBytes("data")); f.Write(pcm.Length); f.Write(pcm);
			}
			host.GetTree().Quit(); return;
		}
		while (true)
		{
			i++; double t = (double)i / fps; if (t > T) { done = true; if (!nodraw) G.gfx.EndFrame(); return; }
			var ins = new G.TIn[G.tm.b.Length];
			if (G.tm != lastTm) { lastTm = G.tm; if (!entry.ContainsKey(G.tm.page) || G.tm.page == page) entry[G.tm.page] = t; }
			int rm = G.tm.page; double rt = t - entry[rm];
			for (int k = 0; k < G.tm.b.Length; k++) ins[k] = Input(k, t, rm, rt);
			AudioCtx.manualTime += 1.0 / fps; performance.manual += 1000.0 / fps;
			if (!nodraw) G.gfx.BeginFrame();
			int n0 = G.tm.restarts; G.tmStep(1.0 / fps, ins);
			if (rpt) { int r0 = G.tmRec.n; G.tmRecTickPub(1.0 / fps); for (int k = r0; k < G.tmRec.n; k++) live.Add(RpStr()); }
			if (wav != null) { int n = AudioCtx.SR / fps; var l = new float[n]; var r = new float[n]; AudioCtx.RenderAt(l, r, n, AudioCtx.manualTime.Value); for (int j = 0; j < n; j++) { wav.Write((short)(Math.Clamp(l[j], -1, 1) * 32767)); wav.Write((short)(Math.Clamp(r[j], -1, 1) * 32767)); } }
			G.particlesStep(1.0 / fps);
			log.Append(str(Math.Round(t, 3))).Append(" r").Append(rm).Append(' ').Append(str(Math.Round(rt, 2))).Append(' ').Append(str(Math.Round(G.tmBeatPub(), 2)));
			foreach (var b in G.tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(' ').Append(b.hp).Append(b.down ? "d" : "").Append(b.gate ? "g" : "").Append(b.onT != null ? " " + b.onT.k : "").Append(b.hang ? " H" : "");
			if (G.tm.restarts != n0) log.Append(" RESTART");
			if (G.tm.hits != hits0) { log.Append(" HIT"); hits0 = G.tm.hits; }
			log.Append(" got=").Append(G.tm.got.Count);
			foreach (var s in G.tm.th) if (s.k is "stone" or "btn" && s.on) log.Append(' ').Append(s.d.id);
			foreach (var s in G.tm.th) if (s.k == "scale") log.Append(" s=").Append(str(Math.Round(s.s, 2)));
			foreach (var s in G.tm.th) if (s.k == "ride") log.Append(" ride=").Append(str(Math.Round(s.X)));
			foreach (var s in G.tm.th) if (s.k == "cbtn" && s.on) log.Append(' ').Append(s.d.id);
			if (G.tm.P.things.Any(d => d.k == "flip")) log.Append(" f=").Append(string.Join(",", G.tm.flips));
			if (G.tm.chaseX > -900) log.Append(" wall=").Append(str(Math.Round(G.tm.chaseX)));
			if (!double.IsNaN(G.tm.riseY)) log.Append(" rise=").Append(str(Math.Round(G.tm.riseY))).Append(G.tm.ropeLive ? " R" : "");
			if (G.tm.swapA >= 0) log.Append($" swap{G.tm.swapA}{G.tm.swapB}@").Append(str(G.tm.swapAt));
			foreach (var b in G.tm.b) if (b.carry != null) log.Append(" carry").Append(b.i);
			if (G.tm.th.Any(s => s.k == "slot")) log.Append(" placed=").Append(G.tm.th.Count(s => s.k == "slot" && s.on));
			foreach (var s in G.tm.th) if (s.k == "mover") log.Append(s.d.need != null ? " lift=" : " mv=").Append(str(Math.Round(s.d.need != null ? s.Y : s.X)));
			log.Append(" songs=").Append(AudioCtx.SongsAt(AudioCtx.manualTime.Value));
			if (G.tm.spkN != null) { log.Append(" sh=").Append(G.tm.shield); foreach (var w in G.tm.bars) log.Append(" w").Append(w.dir > 0 ? '>' : '<').Append(str(Math.Round(w.x))).Append('/').Append(w.r0); }
			if (G.tm.tw != null) log.Append($" tw={G.tm.tw.ph},{G.tm.tw.at},{G.tm.tw.callAt}");
			if (G.tm.won) log.Append(" WON");
			if (G.tm.doneAt >= 0) log.Append(" DONE");
			log.Append('\n');
			if (G.tm.won && System.Environment.GetEnvironmentVariable("TEAM_FALLS") is string tf) { var fs = tf.Split(','); for (int k = 0; k < G.TM_N; k++) G.tm.falls[k] = int.Parse(fs[k]); G.tm.restarts = int.Parse(System.Environment.GetEnvironmentVariable("TEAM_RESTARTS") ?? "0"); }
			if (System.Environment.GetEnvironmentVariable("TEAM_NAMES") is string tn) G.tm.names = tn.Split(',');
			if (!nodraw && i % every == 0 && t >= from && view == "duo") { pass = 0; duoShot(); return; }
			if (!nodraw && i % every == 0 && t >= from)
			{
				var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, 1280, 720);
				double sx = (random() * 2 - 1) * G.shake, sy = (random() * 2 - 1) * G.shake; g.setTransform(1, 0, 0, 1, sx, sy);
				G.tmView = view == "cycle" ? (G.tm.P.dark ? (int)(G.tm.pageAt / 3.2) % 3 : -1) : int.TryParse(view, out var vv) ? vv : -1;
				G.drawTeam(performance.manual.Value);
				if (G.tmView >= 0) { string[] nm = { "شاشة الأحمر", "شاشة الأزرق", "شاشة الأصفر" }; G.g.setTransform(1, 0, 0, 1, 0, 0); G.txt(nm[G.tmView], 1150, 30, 22, G.TM_COL[G.tmView]); }
				if (G.flash > 0) { g.fillStyle = $"rgba(255,255,255,{str(Math.Min(0.8, G.flash))})"; g.fillRect(0, 0, 1280, 720); }
				G.gfx.EndFrame(); shot = true; return;
			}
			if (!nodraw) G.gfx.EndFrame();
			if (nodraw && i % 600 == 0) return;   // let Godot breathe
		}
	}
}
