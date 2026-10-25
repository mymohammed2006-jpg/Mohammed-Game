using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MG;
using static MG.JS;

// The human-limited bot for the new dodge levels: the same limits as the old game's bot (tools/human-bot-v2), made by the L13 thread
// (tools/level13/bot) and shared here so L12, L13 and L14 use one bot. -- --dodge hbot <id> <n|e> <outDir> [seed=1] [maxatt=200]
//   at most 5 input changes in any 1 s, >= 0.185 s between direction changes and between dashes, looks every 0.05 s, each change
//   reaches the game 0-0.04 s late at random, no emergency dashes. Memorized player: knows the FIXED events 1.6 s ahead; events with
//   "rnd": true in data/dodge_<id>.json (aimed at the square, or random each run; a level built by code: Ev.rnd) only 0.32-0.40 s after they fire.
//   Practice like a person: on a hit it goes back 1-10 s and tries a variation from there; stops at the first run with no hit.
//   Writes run.inp (runSeed, then "x y dash" per 60 Hz frame), run.log, rec.txt (RP4=rec.txt --bot-run plays it) and botrun.json (bot.json's entry).
public class DodgeHBot
{
	static readonly CultureInfo IC = CultureInfo.InvariantCulture;
	const double FR = 1.0 / 60;
	static Level Lv(string id) => G.ALL_LV().Concat(G.HCLV).Concat(G.BOSSLV).First(x => x.id == id);
	static void Begin(Level L, bool ez, int seed)
	{
		G.easy = ez; G.applyTheme(ez); G.cur = L; G.levelTiming(L); G.hitsTotal = 0; G.deaths = 0; G.rec = new RecState(); G.state = "play";
		G.beginRun(0, seed); G.songStart = AudioCtx.manualTime.Value + 0.12;
	}
	static void Frame(double x, double y, bool dash)
	{
		AudioCtx.manualTime += FR; performance.manual += 1000 * FR;
		G.updatePlay(FR, new Inp { x = x, y = y, dash = dash });
		foreach (var q in G.particles) { q.x += q.vx * FR; q.y += q.vy * FR; double f = Math.Pow(0.04, FR); q.vx *= f; q.vy *= f; q.life -= FR; }
		G.particles = G.particles.Where(q => q.life > 0).ToList();
		G.shake = Math.Max(0, G.shake - FR * 50); G.flash = Math.Max(0, G.flash - FR * 2.2); G.cpMsg -= FR;
	}
	string[] a;
	public static void Run(string[] args) => new DodgeHBot { a = args }.Bot();

	// ---------- the human-limited bot ----------
	static readonly int[][] DIRS = { new[] { 0, 0 }, new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } };
	const double SP = 370, DSP = 950, DDUR = 0.18, DCD = 0.32, DINV = 0.28, GAP = 0.185, RMIN = 0.32, RMAX = 0.40, JIT = 0.04, HOR = 1.6, DEC = 0.05, MARG = 6, PR = 5, COMF = 40; const int CAP = 5;
	static readonly int NF = (int)Math.Round(HOR / FR);

	class BS   // the bot's own state (copied at a frame to restart from it)
	{
		public int hx, hy; public double lastDir = -9, lastDash = -9, nextDec = -1e9, lookAt = -1e9; public List<double> chg = new(); public int planA2 = 0; public double planT0 = -1e9; public bool hasPlan;
		public (int x, int y, bool d, double at)? pend; public int ax, ay; public uint rs;
		public BS Copy() { var c = (BS)MemberwiseClone(); c.chg = new List<double>(chg); return c; }
	}

	bool[] RND = new bool[0];   // events that depend on the run ("rnd" in the data file)
	int seed; bool EZ; Level L; List<Ev> evs; Dictionary<int, List<Hz>> sandbox = new();
	double now; Dictionary<long, bool> cache = new(); List<Hz> known = new(); List<(int k, List<Hz> l)> future = new(); Player probe = new Player { r = PR };
	double vv;   // variation for the current decision

	static double hs(string s) { unchecked { int h = 977; foreach (var ch in s) { h ^= ch; h *= 16777619; } return ((uint)h % 10007) / 10007.0; } }
	double rtOf(string rid) => RMIN + (RMAX - RMIN) * hs(rid + "#" + seed);
	static int evOf(string rid) { if (rid == null || rid.Length < 2 || rid[0] != 'e') return -1; int i = rid.IndexOf('.'); return int.Parse(i < 0 ? rid.Substring(1) : rid.Substring(1, i - 1)); }

	List<Hz> Sand(int j)
	{
		if (sandbox.TryGetValue(j, out var l)) return l;
		var sv = G.hazards; int rs = G.rngS; double fl = G.flash, sh = G.shake; G.hazards = new List<Hz>(); G.rngS = G.hstr(str(G.runSeed) + ":e" + j);
		try { evs[j].f(); } catch (Exception) { }
		l = G.hazards; G.hazards = sv; G.rngS = rs; G.flash = fl; G.shake = sh;
		if (G.easy && G.ezKE != null && G.ezKE[j] == 0 && !((G.cur.ezLife.TryGetValue("e" + j, out var v) ? v : 0) > G.ezV("ezlong", G.EZ_LONG))) l = new List<Hz>();
		return sandbox[j] = l;
	}

	void Observe(double t)
	{
		known.Clear(); future.Clear();
		foreach (var h in G.hazards)
		{
			if (h.dead) continue; int j = evOf(h.rid);
			if (j >= 0 && j < RND.Length && RND[j] && t - G.B(evs[j].b) < rtOf("e" + j) - 1e-9) continue;
			known.Add(h);
		}
		for (int j = G.evIdx; j < evs.Count; j++)
		{
			double tb = G.B(evs[j].b); if (tb > t + HOR) break; if (j < RND.Length && RND[j]) continue;
			future.Add(((int)Math.Ceiling((tb - t) / FR), Sand(j)));
		}
	}

	bool HitAt(double x, double y, double rr, int k)
	{
		long key = ((long)k << 40) | ((long)((int)(x / 2) & 0xFFFFF) << 20) | (long)((int)(y / 2) & 0xFFFF) << 2 | (rr > PR + MARG ? 3L : rr > PR ? 1L : 0L);
		if (cache.TryGetValue(key, out var v)) return v;
		double tf = now + k * FR; probe.x = x; probe.y = y; probe.r = rr; v = false;
		foreach (var h in known) { bool ok; try { ok = h.hit(probe, tf); } catch (Exception) { ok = false; } if (ok) { v = true; break; } }
		if (!v) foreach (var (k0, l) in future) { if (k < k0) continue; foreach (var h in l) { bool ok; try { ok = h.hit(probe, tf); } catch (Exception) { ok = false; } if (ok) { v = true; break; } } if (v) break; }
		cache[key] = v; return v;
	}

	(double cost, double tReal, double tMarg) Eval(Player P, int[] a1, bool dash, int[] a2, int n1, int[] a3, int n2)
	{
		double x = P.x, y = P.y, dsh = P.dash, dx = P.ddx, dy = P.ddy, cd = P.cd, inv = P.inv;
		if (dash && cd <= 0) { dsh = DDUR; cd = DCD; inv = Math.Max(inv, DINV); double m0 = Math.Sqrt(a1[0] * a1[0] + a1[1] * a1[1]); dx = a1[0] / m0; dy = a1[1] / m0; }
		double tReal = double.PositiveInfinity, tMarg = double.PositiveInfinity; int comf = 0;
		for (int k = 1; k <= NF; k++)
		{
			var a = k <= n1 ? a1 : (a3 != null && k > n1 + n2 ? a3 : a2); double vx, vy;
			if (dsh > 0) { vx = dx * DSP; vy = dy * DSP; } else { double m = Math.Sqrt(a[0] * a[0] + a[1] * a[1]); if (m == 0) m = 1; vx = a[0] / m * SP; vy = a[1] / m * SP; }
			x = Math.Clamp(x + vx * FR, 10, G.W - 10); y = Math.Clamp(y + vy * FR, 10, G.H - 10); dsh -= FR; cd -= FR; inv -= FR;
			if (inv <= 0 && (k == 6 || k == 18 || k == 30) && HitAt(x, y, PR + COMF, k)) comf++;
			if (inv <= 0)
			{
				if (double.IsPositiveInfinity(tMarg) && HitAt(x, y, PR + MARG, k)) { tMarg = k * FR; if (HitAt(x, y, PR, k)) tReal = k * FR; }
				else if (!double.IsPositiveInfinity(tMarg) && double.IsPositiveInfinity(tReal) && HitAt(x, y, PR, k)) tReal = k * FR;
				if (!double.IsPositiveInfinity(tReal)) break;
			}
		}
		double cost = 0; if (!double.IsPositiveInfinity(tReal)) cost += 1000 * (HOR + 0.2 - tReal); if (!double.IsPositiveInfinity(tMarg)) cost += 60 * (HOR + 0.2 - tMarg);
		double ex = (x - G.W / 2) / (G.W / 2), ey = (y - G.H / 2) / (G.H / 2); cost += 1.5 * (ex * ex + ey * ey) + 6 * comf;
		return (cost, tReal, tMarg);
	}

	double Nz(string fk, int i, int d, int j) => vv != 0 ? 3 * (1 + Math.Floor(vv / 1000)) * (hs(vv + ":" + fk + ":" + i + ":" + d + ":" + j) - 0.5) : 0;
	static int Di(int x, int y) { for (int i = 0; i < 9; i++) if (DIRS[i][0] == x && DIRS[i][1] == y) return i; return 0; }
	double rnd01(BS s) { unchecked { s.rs += 0x6D2B79F5; uint t = s.rs; t = (uint)((t ^ (t >> 15)) * (1 | t)); t = (t + (uint)((t ^ (t >> 7)) * (61 | t))) ^ t; return (t ^ (t >> 14)) / 4294967296.0; } }

	// one decision (like bot.js decide0 in human mode); returns the input the bot holds now
	(int x, int y, bool dash) Decide(BS s, double t)
	{
		var P = G.player; string fk = t.ToString("0.0000", IC);
		if (t + 1e-9 < s.nextDec)
		{
			if (!s.hasPlan || !(t + 1e-9 >= s.lookAt)) return (s.hx, s.hy, false);
			s.lookAt = t + DEC; now = t; cache.Clear(); Observe(t);
			int rem = Math.Max(0, (int)Math.Round((s.planT0 + GAP - t) / FR)); var r0 = Eval(P, new[] { s.hx, s.hy }, false, DIRS[s.planA2], rem, null, 0);
			if (double.IsPositiveInfinity(r0.tReal) || t - s.lastDir < GAP - 1e-9) return (s.hx, s.hy, false);
		}
		s.nextDec = t + 0.1; s.lookAt = t + DEC; now = t; cache.Clear(); Observe(t);
		while (s.chg.Count > 0 && s.chg[0] <= t - (1 + JIT + FR) + 1e-9) s.chg.RemoveAt(0);
		int budget = CAP - s.chg.Count; bool dirOk = t - s.lastDir >= GAP - 1e-9, dashOk = t - s.lastDash >= GAP - 1e-9;
		(double c, int a1, bool dash, int a2, double tReal, double tMarg)? best = null; var cand = new List<(double c, int a1, bool dash, int a2, double tReal, double tMarg)>();
		int n1 = (int)Math.Round(Math.Max(DEC, GAP) / FR);
		for (int i = 0; i < 9; i++) foreach (var dash in new[] { false, true })
		{
			if (dash && (i == 0 || P.cd > 0 || P.dash > 0)) continue; var a1 = DIRS[i];
			bool dch = a1[0] != s.hx || a1[1] != s.hy; int nc = (dch ? 1 : 0) + (dash ? 1 : 0); if (nc > budget || (dch && !dirOk) || (dash && !dashOk)) continue;
			(double cost, double tReal, double tMarg) bb = (1e18, 0, 0); int ba2 = 0;
			for (int j = 0; j < 9; j++) { var r = Eval(P, a1, dash, DIRS[j], n1, null, 0); if (r.cost < bb.cost) { bb = r; ba2 = j; } }
			double c = bb.cost + (dash ? 4 : 0) + (i != 0 ? 0.15 : 0) + (dch ? 0.25 : 0) + Nz(fk, i, dash ? 1 : 0, -1);
			var e = (c, i, dash, ba2, bb.tReal, bb.tMarg); cand.Add(e); if (best == null || c < best.Value.c) best = e;
		}
		if (best == null) return (s.hx, s.hy, false);
		var B0 = best.Value;
		if (!double.IsPositiveInfinity(B0.tReal) || !double.IsPositiveInfinity(B0.tMarg))
		{
			int n2 = (int)Math.Round(GAP / FR);
			foreach (var cd in cand.OrderBy(q => q.c).Take(6))
				for (int j = 0; j < 9; j++) for (int k = 0; k < 9; k++)
				{
					if (k == j) continue; var r = Eval(P, DIRS[cd.a1], cd.dash, DIRS[j], n1, DIRS[k], n2); bool dch = DIRS[cd.a1][0] != s.hx || DIRS[cd.a1][1] != s.hy;
					double c = r.cost + (cd.dash ? 4 : 0) + (cd.a1 != 0 ? 0.15 : 0) + (dch ? 0.25 : 0) + 0.2 + Nz(fk, cd.a1, cd.dash ? 1 : 0, j * 9 + k);
					if (c < B0.c) B0 = (c, cd.a1, cd.dash, j, r.tReal, r.tMarg);
				}
		}
		var A = DIRS[B0.a1]; bool ch = A[0] != s.hx || A[1] != s.hy;
		int ncc = (ch ? 1 : 0) + (B0.dash ? 1 : 0); for (int q = 0; q < ncc; q++) s.chg.Add(t); if (ch) s.lastDir = t; if (B0.dash) s.lastDash = t;
		if (ch || B0.dash) { s.nextDec = t + GAP; s.planA2 = B0.a2; s.planT0 = t; } else { s.planA2 = B0.a2; s.planT0 = s.hasPlan ? Math.Min(s.planT0, t) : t; }
		s.hasPlan = true; s.hx = A[0]; s.hy = A[1];
		return (s.hx, s.hy, B0.dash);
	}

	// one frame of input (with the human delay): what reaches the game
	(int x, int y, bool dash) Input(BS s, double t)
	{
		double dec0 = s.nextDec; var d = Decide(s, t); bool decided = s.nextDec != dec0;
		if (decided && (d.x != s.ax || d.y != s.ay || d.dash)) { int f = (int)Math.Round(rnd01(s) * JIT / FR); s.pend = (d.x, d.y, d.dash, t + f * FR); }
		if (s.pend != null && t + 1e-9 >= s.pend.Value.at) { s.ax = s.pend.Value.x; s.ay = s.pend.Value.y; bool ds = s.pend.Value.d; s.pend = null; return (s.ax, s.ay, ds); }
		return (s.ax, s.ay, false);
	}

	class Att { public List<(int x, int y, bool d)> inp = new(); public List<BS> st = new(); public List<double> ts = new(); public double hitT = -1; public string hitBy; public string state; public int runSeed; public int hits; public List<string> path = new(); }

	Att Attempt(List<(int x, int y, bool d)> rep, BS st0, int f0, List<(double from, double v)> sched, bool full)
	{
		var at = new Att(); Begin(L, EZ, seed); at.runSeed = G.runSeed; evs = G.events; if (RND.Length == 0) RND = evs.Select(e => e.rnd).ToArray(); sandbox.Clear(); var s = st0 != null ? st0.Copy() : new BS { rs = (uint)seed * 2654435761u };
		double end = G.B(L.end) + 0.5; int f = 0, lastHits = 0;
		while (G.state == "play" && G.songT < end + 1)
		{
			double t = G.songT; (int x, int y, bool d) inp;
			if (f < f0) { at.st.Add(null); inp = rep[f]; }
			else { at.st.Add(s.Copy()); vv = 0; foreach (var q in sched) if (t >= q.from - 1e-9) vv = q.v; inp = Input(s, t); }
			at.inp.Add(inp); at.ts.Add(t); G.player.hp = 1e9;
			Frame(inp.x, inp.y, inp.d); f++;
			var P = G.player; at.path.Add($"{G.songT.ToString("0.0000", IC)} {P.x.ToString("0.00", IC)} {P.y.ToString("0.00", IC)} {(P.dash > 0 ? 1 : 0)} {inp.x} {inp.y} {(inp.d ? 1 : 0)}");
			if (G.hitsTotal > lastHits)
			{
				lastHits = G.hitsTotal; if (at.hitT < 0 && f > f0) { at.hitT = G.songT; at.hitBy = string.Join("+", G.hazards.Where(h => { try { return !h.dead && h.hit(P, G.songT); } catch (Exception) { return false; } }).Select(h => h.rid).Distinct()); }
				if (!full && f > f0) break;
			}
		}
		at.state = G.state; at.hits = G.hitsTotal; return at;
	}

	void Bot()
	{
		L = Lv(a[0]); EZ = a[1] == "e"; string dir = a[2]; Directory.CreateDirectory(dir); string outP = Path.Combine(dir, "run");
		using (var jf = Godot.FileAccess.Open($"res://data/dodge_{a[0]}.json", Godot.FileAccess.ModeFlags.Read)) if (jf != null) { var doc = System.Text.Json.JsonDocument.Parse(jf.GetAsText()); if (doc.RootElement.TryGetProperty("events", out var je)) RND = je.EnumerateArray().Select(e => e.TryGetProperty("rnd", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.True).ToArray(); } seed = a.Length > 3 ? int.Parse(a[3]) : 1; int MAXATT = a.Length > 4 ? int.Parse(a[4]) : 200;
		double[] BACK = { 1, 1.5, 2, 2, 3, 3, 4, 4, 5, 6, 8, 10 };
		var rep = new List<(int, int, bool)>(); BS st0 = null; var snaps = new List<BS>(); int f0 = 0; var sched = new List<(double, double)>(); int vc = 0, att = 0, tries = 0; double spot = -99; Att res = null; var log = new StringBuilder();
		var sw = System.Diagnostics.Stopwatch.StartNew();
		for (; ; )
		{
			att++; res = Attempt(rep, st0, f0, sched, att > MAXATT);
			string line = $"att {att} from {(f0 > 0 ? res.ts[f0] : 0).ToString("0.00", IC)} hit {(res.hitT >= 0 ? res.hitT.ToString("0.00", IC) + " " + res.hitBy : "none")} {sw.ElapsedMilliseconds / 1000}s";
			log.AppendLine(line); GD.Print(line);
			if (res.hitT < 0 || att > MAXATT) break;
			if (Math.Abs(res.hitT - spot) < 3.0) tries++; else { spot = res.hitT; tries = 0; }
			double Bk = res.hitT - BACK[tries % BACK.Length];
			for (int q = f0; q < res.st.Count; q++) { while (snaps.Count <= q) snaps.Add(null); snaps[q] = res.st[q]; }
			int nf = 0; while (nf < res.ts.Count && res.ts[nf] < Bk - 1e-9) nf++;
			// keep the inputs before Bk; the bot's own state there comes from the run that made those inputs
			rep = res.inp.Take(nf).ToList(); st0 = nf < snaps.Count ? snaps[nf] : null; if (st0 == null) { nf = 0; rep.Clear(); }
			f0 = nf; sched = sched.Where(q => q.Item1 < Bk - 1e-9).ToList(); sched.Add((Bk, (++vc % 1000) + 1000 * Math.Min(4, tries >> 1)));
		}
		File.WriteAllText(outP, $"# {a[0]} {a[1]} seed {seed} runSeed {res.runSeed} att {att} hits {res.hits} state {res.state}\n" + string.Join("\n", res.path) + "\n");
		File.WriteAllText(outP + ".inp", $"{res.runSeed}\n" + string.Join("\n", res.inp.Select(q => $"{q.x} {q.y} {(q.d ? 1 : 0)}")) + "\n");
		File.WriteAllText(outP + ".log", log.ToString());
		GD.Print($"BOT {a[0]} {a[1]} att {att} hits {res.hits} state {res.state} {sw.ElapsedMilliseconds / 1000}s");
		Record(res, dir);
	}

	// play the run's inputs once more in the real game, recorded like a real player's run: rec.txt and bot.json's entry
	void Record(Att res, string dir)
	{
		Begin(L, EZ, res.runSeed); var R = new RecState(); G.rec = R; R.cur = new RecSeg { cp = 0, seed = G.runSeed, t0 = null, n = 0 }; R.segs.Add(R.cur);   // the win screen takes G.rec away
		foreach (var q in res.inp) { if (G.state != "play") break; Frame(q.x, q.y, q.d); }
		GD.Print($"recorded run: hits {G.hitsTotal} state {G.state}");
		var sb = new StringBuilder(); foreach (var s in R.segs) sb.Append(str(s.cp)).Append(' ').Append(s.seed).Append(' ').Append(str(s.t0 ?? 0)).Append(' ').Append(s.n).Append(' ').Append(string.Join(",", s.b)).Append('\n');
		File.WriteAllText(Path.Combine(dir, "rec.txt"), sb.ToString());
		// positions packed 5 bytes a sample, base64
		var segs = R.segs.Select(s =>
		{
			var by = new byte[s.n * 5]; for (int i = 0; i < s.n; i++) { int x = s.b[i * 3], y = s.b[i * 3 + 1], f = s.b[i * 3 + 2]; by[i * 5] = (byte)(x & 255); by[i * 5 + 1] = (byte)(x >> 8); by[i * 5 + 2] = (byte)(y & 255); by[i * 5 + 3] = (byte)(y >> 8); by[i * 5 + 4] = (byte)f; }
			return new Dictionary<string, object> { ["cp"] = s.cp, ["seed"] = s.seed, ["t0"] = s.t0 ?? 0, ["n"] = s.n, ["d"] = Convert.ToBase64String(by) };
		}).ToList();
		File.WriteAllText(Path.Combine(dir, "botrun.json"), System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object> { ["r"] = G.hitsTotal == 0 ? "S" : "A", ["hz"] = G.RP_HZ, ["v"] = G.RP_V, ["segs"] = segs }));
	}

}
