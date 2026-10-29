using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static MG.JS;

namespace MG;

// Tools for the new dodge levels (no drawing, no sound):
//   -- --dodge diff <id> <n|e>             how hard a level is: hits a minute on 4 fixed wandering paths (with and without the 2 s
//                                           protection after a hit), and how much of the screen hurts (coverage), the same numbers for any level
//   -- --dodge hbot <id> <n|e> <outDir> [seed] [maxatt]   the human-limited bot (old game's limits, test/DodgeHBot.cs): finds an S like a
//                                           person practising; writes rec.txt (RP4=<rec.txt> --bot-run for frames) and bot.json's entry (botrun.json)
//   -- --dodge rpcheck <id>[_v51] <n|e>    plays the level's bot.json run the way "watch the bot" does and counts the steps where it touches an attack (0 = in sync)
public static partial class G
{
	public static void DodgeTool(string[] a, int k)
	{
		localStorage.Memory = true; noMus = true; padsDisabled = true; classicUi = false;
		performance.manual = 100000; AudioCtx.manualTime = 0; InitSound(); JS.SeedRandom(12345);
		string mode = a[k], id = a[k + 1], m = a[k + 2];
		bool old = id.EndsWith("_v51"); if (old) id = id[..^4];   // <id>_v51: the old design of a level with new attacks and its <id><m>_v51 bot run
		var L = ALL_LV().Concat(HCLV).Concat(BOSSLV).First(x => x.id == id); if (old) L = L.v51; easy = m == "e"; cur = L; levelTiming(L);
		if (mode == "diff") DodgeDiff(L);
		else if (mode == "speed") DodgeSpeed(L);
		else if (mode == "hbot") DodgeHBot.Run(a.Skip(k + 1).ToArray());
		else if (mode == "rpcheck") DodgeRpCheck(L, m);
		else if (mode == "afk") DodgeAfk(L);
		else if (mode == "cpcheck") DodgeCpCheck(L);
	}

	// ---------- a restart from each save: what hurts (a 40 px grid) and the kinds of things out, against the same moments of a run from the start ----------
	//   -- --dodge cpcheck <id> n      the square parked in the middle, never hurt; prints the moments where the two differ (none = the save is whole)
	static void DodgeCpCheck(Level L)
	{
		string Snap(double t)
		{
			var pr = new Player { r = 1 }; var sb = new StringBuilder();
			for (int gx = 0; gx < 32; gx++) for (int gy = 0; gy < 18; gy++) { pr.x = 20 + gx * 40; pr.y = 20 + gy * 40; sb.Append(hazards.Any(h => !h.dead && h.hit(pr, t)) ? '#' : '.'); }
			sb.Append(' ').Append(string.Join(",", hazards.Where(h => !h.dead).GroupBy(h => h.kind ?? "?").OrderBy(gq => gq.Key).Select(gq => gq.Key + "x" + gq.Count())));
			return sb.ToString();
		}
		Dictionary<int, string> Run(double cp, double upTo)
		{
			var o = new Dictionary<int, string>(); beginRun(cp, 777); dry = true;
			for (long n = 1; ; n++)
			{
				double t = sim0 + n * STEP; if (t > upTo) break; songT = t;
				player.x = W / 2; player.y = H / 2; player.inv = 1e9;
				fireEvents(t); stepHazards(t, STEP); hazards = hazards.Where(h => !h.dead).ToList(); particles.Clear();
				int k = (int)Math.Floor(BT(t) * 4); if (BT(t) >= cp && !o.ContainsKey(k)) o[k] = Snap(t);
			}
			dry = false; return o;
		}
		foreach (var cp in L.cps.Skip(1))
		{
			double upTo = B(Math.Min(L.end, cp + 48));
			var a = Run(0, upTo); var b = Run(cp, upTo); int bad = 0;
			foreach (var k in a.Keys.OrderBy(x => x)) if (b.TryGetValue(k, out var v) && v != a[k]) { if (bad++ < 400) GD.Print($"beat {toFixed(k / 4.0, 2)}: from start {a[k][577..]} | from save {v[577..]}  cells {a[k][..576].Zip(v[..576]).Count(z => z.First != z.Second)}"); }
			GD.Print($"RESULT {L.id} save {cp}: {bad} of {a.Count} quarter-beats differ");
		}
	}

	// ---------- how hard ----------
	static (double x, double y) DPath(int w, double t)
	{
		switch (w)
		{
			case 0: return (W / 2 + JS.cos(t * 0.9) * 330, H / 2 + JS.sin(t * 0.9) * 210);
			case 1: return (W / 2 + JS.sin(t * 0.55) * 470, H / 2 + JS.sin(t * 1.1) * 230);
			case 2: return (W / 2 + JS.sin(t * 0.37) * 520, H / 2 + JS.sin(t * 2.3) * 90 + JS.sin(t * 0.21) * 150);
			default: return (W / 2 + JS.sin(t * 0.71 + 1) * 300 + JS.sin(t * 1.9) * 120, H / 2 + JS.cos(t * 0.63) * 190 + JS.sin(t * 2.7) * 60);
		}
	}
	static void DodgeDiff(Level L)
	{
		double end = B(L.end), mins = end / 60; double sp = 0, su = 0, cov = 0; int covN = 0; var seg = new double[16]; long spawns = 0; var perS = new int[400];
		for (int w = 0; w < 4; w++)
		{
			beginRun(0, 1000 + w); dry = true; int hp = 0, hu = 0; double inv = 1.0, lastU = -9; double nextCov = 0;
			for (long n = 1; ; n++)
			{
				double t = sim0 + n * STEP; if (t > end) break; songT = t;
				var q = DPath(w, t); player.x = q.x; player.y = q.y; player.inv = 1e9;
				int hc0 = hazards.Count; fireEvents(t); stepHazards(t, STEP); if (w == 0) { spawns += Math.Max(0, hazards.Count - hc0); perS[(int)clamp(t, 0, 399)] += Math.Max(0, hazards.Count - hc0); }
				bool hit = hazards.Any(h => !h.dead && h.hit(player, t));
				inv -= STEP; if (hit && inv <= 0) { hp++; inv = 2.0; }
				if (hit && t - lastU > 0.25) { hu++; seg[(int)clamp(Math.Floor(BT(t) / 32), 0, 15)]++; } if (hit) lastU = t;
				if (w == 0 && t >= nextCov)
				{
					nextCov = t + 0.25; var pr = new Player { r = 1 }; int c = 0;
					for (int gx = 0; gx < 32; gx++) for (int gy = 0; gy < 18; gy++) { pr.x = 20 + gx * 40; pr.y = 20 + gy * 40; if (hazards.Any(h => !h.dead && h.hit(pr, t))) c++; }
					cov += c / 576.0; covN++;
				}
				hazards = hazards.Where(h => !h.dead).ToList(); particles.Clear();
			}
			dry = false; sp += hp; su += hu;
			GD.Print($"path {w}: hits {hp} (unprotected {hu})");
		}
		{ int best = 0; for (int i = 0; i + 8 <= 400; i++) best = Math.Max(best, perS.Skip(i).Take(8).Sum()); GD.Print($"busiest 8 s: {toFixed(best / 8.0, 1)} new things/s"); }
		GD.Print("unprotected/min by 32 beats: " + string.Join(" ", seg.Take((int)Math.Ceiling(L.end / 32)).Select(v => toFixed(v / 4 / (32 * SPB / 60), 1))));
		GD.Print($"RESULT {L.id} {(easy ? "e" : "n")}: hits/min {toFixed(sp / 4 / mins, 2)}  unprotected/min {toFixed(su / 4 / mins, 2)}  coverage {toFixed(100 * cov / covN, 1)}%  new things/min {toFixed(spawns / mins, 0)}  events/min {toFixed(events.Count / mins, 0)}");
	}

	// ---------- how fast the pink comes (the L14 thread's measure): the player parked in the middle; every 1/30 s an 80x45 grid,
	// cells deadly now that were free a moment ago, as % of the screen per second; the whole level and the busiest 16 beats ----------
	static void DodgeSpeed(Level L)
	{
		beginRun(0, 1); dry = true; double end = B(L.end); const int CX = 80, CY = 45; var prev = new bool[CX * CY]; double next = 0, tot = 0; int n = 0;
		var blk = new double[64]; var blkN = new int[64]; var pr = new Player { r = 0.5 };
		for (long k = 1; ; k++)
		{
			double t = sim0 + k * STEP; if (t > end) break; songT = t; player.x = W / 2; player.y = H / 2; player.inv = 1e9;
			fireEvents(t); stepHazards(t, STEP); hazards = hazards.Where(h => !h.dead).ToList(); particles.Clear();
			if (t < next) continue; next += 1.0 / 30; int nw = 0;
			for (int j = 0; j < CY; j++) for (int i = 0; i < CX; i++) { pr.x = (i + 0.5) * W / CX; pr.y = (j + 0.5) * H / CY; bool h = hazards.Any(z => z.hit(pr, t)); if (h && !prev[j * CX + i]) nw++; prev[j * CX + i] = h; }
			tot += nw; n++; int b = (int)clamp(Math.Floor(BT(t) / 16), 0, 63); blk[b] += nw; blkN[b]++;
		}
		dry = false; double sc = 30 * 100.0 / (CX * CY); int mb = 0; for (int i = 0; i < 64; i++) if (blkN[i] > 0 && blk[i] / blkN[i] > blk[mb] / Math.Max(1, blkN[mb])) mb = i;
		GD.Print("per 16 beats: " + string.Join(" ", Enumerable.Range(0, 64).Where(i => blkN[i] > 0).Select(i => toFixed(blk[i] / blkN[i] * sc, 0))));
		GD.Print($"SPEED {L.id} {(easy ? "e" : "n")}: new pink {toFixed(tot / n * sc, 1)}%/s, busiest 16 beats (from beat {mb * 16}) {toFixed(blk[mb] / blkN[mb] * sc, 1)}%/s");
	}

	// ---------- standing still: the longest time a parked square is never touched, on a 40 px grid ----------
	static void DodgeAfk(Level L)
	{
		beginRun(0, 1); dry = true; double end = double.TryParse(System.Environment.GetEnvironmentVariable("AFKEND"), out var ae) ? B(ae) : B(L.end); const int CX = 32, CY = 18; var since = new double[CX * CY]; var best = new double[CX * CY]; var at = new double[CX * CY];
		// AFKFROM=<beat>: only the stands from that beat on (one part or section of a level)
		double from = double.TryParse(System.Environment.GetEnvironmentVariable("AFKFROM"), out var af) ? B(af) : sim0;
		for (int i = 0; i < since.Length; i++) since[i] = from;
		var pr = new Player { r = 5 }; double next = 0;
		for (long k = 1; ; k++)
		{
			double t = sim0 + k * STEP; if (t > end) break; songT = t; player.x = W / 2; player.y = H / 2; player.inv = 1e9;
			fireEvents(t); stepHazards(t, STEP); hazards = hazards.Where(h => !h.dead).ToList(); particles.Clear();
			int nh = 0;
			if (System.Environment.GetEnvironmentVariable("AFKDBG") != null && ((int)(t * 60)) % 60 == 0) GD.Print($"t {toFixed(t, 1)} hz {hazards.Count} {string.Join(",", hazards.Select(z => z.kind).Distinct())}");
			for (int j = 0; j < CY; j++) for (int i = 0; i < CX; i++)
			{
				int q = j * CX + i; pr.x = 20 + i * 40; pr.y = 20 + j * 40; if (i == 15 && j == 8) continue;   // the middle: the shadow follows the parked square there
				if (System.Environment.GetEnvironmentVariable("AFKPT") is string ap && ap == $"{pr.x},{pr.y}" && hazards.Any(z => z.hit(pr, t))) GD.Print($"hit {toFixed(t, 2)} b {toFixed(BT(t), 2)} {string.Join(",", hazards.Where(z => z.hit(pr, t)).Select(z => z.kind))}");
				if (t < from) continue;
				if (hazards.Any(z => z.hit(pr, t))) { if (t - since[q] > best[q]) { best[q] = t - since[q]; at[q] = since[q]; } since[q] = t; }
			}
		}
		for (int q = 0; q < since.Length; q++) if (end - since[q] > best[q]) { best[q] = end - since[q]; at[q] = since[q]; }
		dry = false;
		var all = Enumerable.Range(0, CX * CY).Where(q => q != 8 * CX + 15).OrderByDescending(q => best[q]).ToList();
		GD.Print($"AFK {L.id} {(easy ? "e" : "n")}: longest untouched stand (s, from s, x y):");
		foreach (var q in all.Take(int.TryParse(System.Environment.GetEnvironmentVariable("AFKN"), out var an) ? an : 25)) GD.Print($"  {toFixed(best[q], 1)} from {toFixed(at[q], 1)} at {20 + q % CX * 40} {20 + q / CX * 40}");
		if (System.Environment.GetEnvironmentVariable("AFKMAP") != null) for (int j = 0; j < CY; j++) GD.Print("MAP " + string.Join("", Enumerable.Range(0, CX).Select(i => { double b = best[j * CX + i]; return b > 10 ? "#" : b > 6 ? "+" : b > 3 ? "." : " "; })));
		GD.Print($"AFK {L.id}: spots over 6 s: {all.Count(q => best[q] > 6)}, over 10 s: {all.Count(q => best[q] > 10)}");
	}

	// ---------- the bot's run as the game shows it ----------
	static void DodgeRpCheck(Level L, string m)
	{
		var d = botDoc(L, m); if (d == null) { GD.Print($"RPCHECK {L.id} {m}: no bot run"); return; }
		rp.p = new RpPerson { name = "البوت", uid = "bot" }; rp.L = L; rp.m = m; rp.doc = d; rp.err = false; rp.bot = true; rp.back = "menu";
		rpStart(); int touch = 0, steps = 0, inDash = 0; long lastN = -1; double lastDash = -9;
		for (int f = 0; f < 60 * 400 && state == "replay"; f++)
		{
			AudioCtx.manualTime += 1.0 / 60; performance.manual += 1000.0 / 60; updateReplay(1.0 / 60);
			if (state != "replay" || simN == lastN) continue; lastN = simN; steps++; double t = sim0 + simN * STEP;
			if (player.dash > 0) lastDash = t;
			if (hazards.Any(h => !h.dead && h.hit(player, t))) { if (t - lastDash < 0.28 - 0.18 + 0.02) inDash++; else { touch++; GD.Print($"touch at {toFixed(t, 3)} s"); } }
		}
		GD.Print($"RPCHECK {L.id} {m}: fixed {rpFixed()} frames-steps {steps} touching {touch} (and {inDash} inside a dash's protection) end {state}");
	}
}
