using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// The co-op level with three players and no people: -- --team-run <levelId> <page> <outDir> <route0> <route1> <route2> [fps=60] [every=1]
// Each route is PlatTest's script for one player ("<seconds> <keys>", keys L R U D J X).
// env NODRAW=1: no pictures (fast, for working out routes); env WAV=<file>: also writes the game's sound; env T=<seconds>: how long.
// Writes state.log: time beat, then per player x y hp (d = down, g = in the gate), then events.
public class TeamTest
{
	string id, outDir; int page, fps = 60, every = 1, i; double T; bool done, shot, nodraw = System.Environment.GetEnvironmentVariable("NODRAW") == "1";
	int hits0; string[] routes; (int room, double t, string k)[][] lines; System.Collections.Generic.Dictionary<int, double> entry = new(); G.TeamRun lastTm; string[] lastK = new string[G.TM_N]; StringBuilder log = new(); Main host;
	double from = double.TryParse(System.Environment.GetEnvironmentVariable("FROM"), out var fr) ? fr : 0;   // env FROM: pictures only after this time
	string view; int pass = -1; bool[] beatJ = new bool[G.TM_N]; double[] heldTo = new double[G.TM_N];   // env VIEW: a player's screen (0-2), or "cycle" (each one's screen in turn in the dark rooms); else everyone's
	BinaryWriter wav = System.Environment.GetEnvironmentVariable("WAV") != null ? new BinaryWriter(new MemoryStream()) : null;

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
		view = System.Environment.GetEnvironmentVariable("VIEW");
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
			for (int k = 0; k < G.tm.b.Length; k++)
			{
				string kk = ""; foreach (var l in lines[k]) if (l.room == rm && l.t <= rt + 1e-9) kk = l.k;
				bool dashNow = kk.Contains('X') && !lastK[k].Contains('X'), jumpNow = kk.Contains('J') && !lastK[k].Contains('J'); lastK[k] = kk;
				// "J!": the jump waits for the next beat (the radio's waves need a jump on the beat)
				if (jumpNow && kk.Contains("J!")) { beatJ[k] = true; jumpNow = false; }
				if (beatJ[k]) { double bt = G.tmBeatPub(), nb = bt + 1.0 / fps / G.tm.L.spb; if (Math.Floor(nb) != Math.Floor(bt)) { jumpNow = true; beatJ[k] = false; heldTo[k] = t + 0.3; } }
				ins[k] = new G.TIn { x = (kk.Contains('R') ? 1 : 0) - (kk.Contains('L') ? 1 : 0), y = (kk.Contains('D') ? 1 : 0) - (kk.Contains('U') ? 1 : 0), jump = jumpNow, jumpHeld = kk.Contains('J') || t < heldTo[k], dash = dashNow };
				// "@<x>": walk to x and stop there; "A": on the ride, step to a part the next lightning misses
				var b = G.tm.b[k]; double? goal = null;
				foreach (var tok in kk.Split(' ', StringSplitOptions.RemoveEmptyEntries)) if (tok.StartsWith("@")) goal = double.Parse(tok[1..]);
				if (kk.Contains('A')) goal = dodge(b, k);
				if (goal != null) { double d = goal.Value - b.x; ins[k].x = Math.Abs(d) < 2 ? 0 : Math.Clamp(d / 25, -1, 1); }
			}
			AudioCtx.manualTime += 1.0 / fps; performance.manual += 1000.0 / fps;
			if (!nodraw) G.gfx.BeginFrame();
			int n0 = G.tm.restarts; G.tmStep(1.0 / fps, ins);
			if (wav != null) { int n = AudioCtx.SR / fps; var l = new float[n]; var r = new float[n]; AudioCtx.RenderAt(l, r, n, AudioCtx.manualTime.Value); for (int j = 0; j < n; j++) { wav.Write((short)(Math.Clamp(l[j], -1, 1) * 32767)); wav.Write((short)(Math.Clamp(r[j], -1, 1) * 32767)); } }
			G.particlesStep(1.0 / fps);
			log.Append(str(Math.Round(t, 3))).Append(" r").Append(rm).Append(' ').Append(str(Math.Round(rt, 2))).Append(' ').Append(str(Math.Round(G.tmBeatPub(), 2)));
			foreach (var b in G.tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(' ').Append(b.hp).Append(b.down ? "d" : "").Append(b.gate ? "g" : "").Append(b.onT != null ? " " + b.onT.k : "");
			if (G.tm.restarts != n0) log.Append(" RESTART");
			if (G.tm.hits != hits0) { log.Append(" HIT"); hits0 = G.tm.hits; }
			log.Append(" got=").Append(G.tm.got.Count);
			foreach (var s in G.tm.th) if (s.k is "stone" or "btn" && s.on) log.Append(' ').Append(s.d.id);
			foreach (var s in G.tm.th) if (s.k == "scale") log.Append(" s=").Append(str(Math.Round(s.s, 2)));
			foreach (var s in G.tm.th) if (s.k == "ride") log.Append(" ride=").Append(str(Math.Round(s.X)));
			foreach (var s in G.tm.th) if (s.k == "cbtn" && s.on) log.Append(' ').Append(s.d.id);
			if (G.tm.P.things.Any(d => d.k == "flip")) log.Append(" f=").Append(string.Join(",", G.tm.flips));
			if (G.tm.chaseX > -900) log.Append(" wall=").Append(str(Math.Round(G.tm.chaseX)));
			if (G.tm.swapA >= 0) log.Append($" swap{G.tm.swapA}{G.tm.swapB}@").Append(str(G.tm.swapAt));
			foreach (var b in G.tm.b) if (b.carry != null) log.Append(" carry").Append(b.i);
			if (G.tm.th.Any(s => s.k == "slot")) log.Append(" placed=").Append(G.tm.th.Count(s => s.k == "slot" && s.on));
			foreach (var s in G.tm.th) if (s.k == "mover") log.Append(s.d.need != null ? " lift=" : " mv=").Append(str(Math.Round(s.d.need != null ? s.Y : s.X)));
			log.Append(" songs=").Append(AudioCtx.SongsAt(AudioCtx.manualTime.Value));
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
