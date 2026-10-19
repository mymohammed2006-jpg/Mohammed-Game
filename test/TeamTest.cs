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
	int hits0; string[] routes; (double t, string k)[][] lines; string[] lastK = new string[G.TM_N]; StringBuilder log = new(); Main host;
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
		lines = routes.Select(r => File.ReadAllLines(r).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).Select(l => { var f = l.Trim().Split(' ', 2); return (double.Parse(f[0]), f.Length > 1 ? f[1].Split('#')[0].Trim() : ""); }).ToArray()).ToArray();
		for (int k = 0; k < G.TM_N; k++) lastK[k] = "";
		T = double.TryParse(System.Environment.GetEnvironmentVariable("T"), out var tt) ? tt : lines.Max(l => l.Length > 0 ? l[^1].t : 0) + 3;
		G.ensureAudio(); G.stopMenu();
		G.tmStart(G.LoadTeam(id), page); G.state = "team";
	}
	public void Process()
	{
		if (shot) { G.gfx.game.GetTexture().GetImage().SavePng(Path.Combine(outDir, $"f{i:D6}.png")); shot = false; }
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
			var ins = new G.TIn[G.TM_N];
			for (int k = 0; k < G.TM_N; k++)
			{
				string kk = ""; foreach (var l in lines[k]) if (l.t <= t + 1e-9) kk = l.k;
				bool dashNow = kk.Contains('X') && !lastK[k].Contains('X'), jumpNow = kk.Contains('J') && !lastK[k].Contains('J'); lastK[k] = kk;
				ins[k] = new G.TIn { x = (kk.Contains('R') ? 1 : 0) - (kk.Contains('L') ? 1 : 0), y = (kk.Contains('D') ? 1 : 0) - (kk.Contains('U') ? 1 : 0), jump = jumpNow, jumpHeld = kk.Contains('J'), dash = dashNow };
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
			log.Append(str(Math.Round(t, 3))).Append(' ').Append(str(Math.Round(G.tm.t / G.tm.L.spb, 2)));
			foreach (var b in G.tm.b) log.Append(" | ").Append(str(Math.Round(b.x))).Append(',').Append(str(Math.Round(b.y))).Append(' ').Append(b.hp).Append(b.down ? "d" : "").Append(b.gate ? "g" : "").Append(b.onT != null ? " " + b.onT.k : "");
			if (G.tm.restarts != n0) log.Append(" RESTART");
			if (G.tm.hits != hits0) { log.Append(" HIT"); hits0 = G.tm.hits; }
			log.Append(" got=").Append(G.tm.got.Count);
			foreach (var s in G.tm.th) if (s.k is "stone" or "btn" && s.on) log.Append(' ').Append(s.d.id);
			foreach (var s in G.tm.th) if (s.k == "scale") log.Append(" s=").Append(str(Math.Round(s.s, 2)));
			foreach (var s in G.tm.th) if (s.k == "ride") log.Append(" ride=").Append(str(Math.Round(s.X)));
			if (G.tm.doneAt >= 0) log.Append(" DONE");
			log.Append('\n');
			if (!nodraw && i % every == 0)
			{
				var g = G.g; g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, 1280, 720);
				double sx = (random() * 2 - 1) * G.shake, sy = (random() * 2 - 1) * G.shake; g.setTransform(1, 0, 0, 1, sx, sy);
				G.drawTeam(performance.manual.Value);
				if (G.flash > 0) { g.fillStyle = $"rgba(255,255,255,{str(Math.Min(0.8, G.flash))})"; g.fillRect(0, 0, 1280, 720); }
				G.gfx.EndFrame(); shot = true; return;
			}
			if (!nodraw) G.gfx.EndFrame();
			if (nodraw && i % 600 == 0) return;   // let Godot breathe
		}
	}
}
