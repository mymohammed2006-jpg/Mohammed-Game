using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// a level's settings. JS: {name,song,ok,bpm,off,end,cps,energy,ezg,ezc,ez,build,best}
public class Level
{
	public string id, name, song, by, ver;
	public bool ok = true;
	public double bpm, off, end;
	public double[] beats, cps, energy;
	// easy-mode settings (null = not set on this level); functions because some depend on the replay version
	public Func<double> ezg, ezc, ez, ezlong, ezsp, ezwn, ezgw;
	public Func<List<Ev>> build;
	public string buildName;      // first buildLevelN / buildFriendN name in the JS build function (the AFK tables use it)
	public string best, bestE;
	public double bestT = -1;     // the platformer: the fastest finish with the best letter (seconds, -1 = none)
	public object buf;            // the decoded song (null until loaded)
	public bool err;
	public bool hc;               // a Hardcore level (UI/Hard.cs): 3 hearts, no Easy
	public bool boss;             // a boss level (UI/Boss.cs): 3 hearts, no Easy, no checkpoint
	public bool plat;             // a platformer level (data/plat_<id>.json): only its letter is here, for XP, points, the board and the profile
	public Dictionary<string, double> ezLifeOld, ezLifeNew, ezLife;
	public Level v51; public bool isV51;   // v52: a level with new attacks keeps its v51 design (data/dodge_<id>_v51.json) for the replays recorded before (Replay.cs rpLevel)
	public static Func<double> K(double v) => () => v;
}

// the recording of a run for the replay (sampled at RP_HZ)
public class RecSeg { public double cp; public int seed; public double? t0; public List<int> b = new(); public int n; public double fr = -1; }   // fr: a practice run's start second (v46, saved on the PC only)
public class RecState { public List<RecSeg> segs = new(); public RecSeg cur; }

public static partial class G
{
	public static Level cur = null;
	public static RecState rec = null;
	public static readonly string[] RANKS = { "S", "A", "B", "C" };
	public static string lastRank = "C";
	public static double lastGain = 0, winSlam = 0;
	public static bool winHit = false;
	public const int RP_HZ = 50;
	static bool prHitRec;
	public static void recSample(double t)
	{
		if (rec == null || rec.cur == null) return; var c = rec.cur; var p = player; if (c.t0 == null) c.t0 = t;
		while (c.t0.Value + (double)c.n / RP_HZ <= t && c.n < 60000)
		{
			c.b.Add((int)round(clamp(p.x, 0, 1300) * 50)); c.b.Add((int)round(clamp(p.y, 0, 1300) * 50));
			if (c.fr < 0) c.b.Add((p.dash > 0 ? 1 : 0) | ((int)clamp(p.hp, 0, 7) << 1));
			else { int hp = (int)clamp(p.hp, 0, 15); c.b.Add((p.dash > 0 ? 1 : 0) | ((hp & 7) << 1) | (prHitRec ? 16 : 0) | (hp >= 8 ? 32 : 0)); prHitRec = false; }   // practice: 16 = hit here (lives that never end don't go down), 32 = 8 lives or more
			c.n++;
		}
	}
	public static bool introOn = false, audioHint = false;
	public static double startReqAt = 0, startBeat = 0, deadAt = 0, winAt = 0, cpMsg = 0, deadPct = 0;
	public static string fromList = "levels";

	public static void levelTiming(Level L)
	{
		if (L.beats != null) { BEATS = L.beats; OFF = L.beats[0]; SPB = (L.beats[L.beats.Length - 1] - L.beats[0]) / (L.beats.Length - 1); }
		else { BEATS = null; SPB = 60 / L.bpm; OFF = L.off; }
	}

	// ---------- easy mode ----------
	public static bool easy = false, lastEz = false;
	public static double EZ_GAP = 1, EZ_SP = 0.8, EZ_WN = 1.3, EZ_GW = 1.25, EZ_R = 5;
	public const double EZ_LONG = 12;
	public static byte[] ezKE = null;
	public static double ezV(string k, double d)
	{
		if (cur == null) return d;
		Func<double> f = k switch { "ezg" => cur.ezg, "ezc" => cur.ezc, "ez" => cur.ez, "ezlong" => cur.ezlong, "ezsp" => cur.ezsp, "ezwn" => cur.ezwn, "ezgw" => cur.ezgw, _ => null };
		return f != null ? f() : d;
	}

	// a quiet run of the whole level to learn how long every attack (and what it spawns) stays
	public static Dictionary<string, double> ezScan(Level L)
	{
		var svH = hazards; var svP = particles; var svPl = player; double svT = songT, svS = shake, svF = flash, svG = gloom, svGT = gloomT;
		var life = new Dictionary<string, double>(); int rs0 = rngS; dry = true;
		hazards = new List<Hz>(); particles = new List<Particle>();
		player = new Player { x = W / 2, y = H / 2, r = 5, hp = 1e9, inv = 1e9, hurt = 0, dash = 0, cd = 0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		void tag(int k, string key, double t) { for (int j = k; j < hazards.Count; j++) { var h = hazards[j]; h.ezK = key; h.ezN = 0; h.ezT = t; } }
		try
		{
			var ev = L.build(); int n = ev.Count; double dt = 1.0 / 30, end = B(L.end) + 1; double t = -0.12; int i = 0;
			while (t < end)
			{
				songT = t;
				while (i < n && B(ev[i].b) <= t) { int k = hazards.Count; ev[i].f(); tag(k, "e" + i, t); i++; }
				for (int j = 0, m = hazards.Count; j < m; j++) { var h = hazards[j]; int k = hazards.Count; h.update(t, dt); if (hazards.Count > k) tag(k, h.ezK + "." + (h.ezN++), t); }
				foreach (var h in hazards) if (h.ezK != null) life[h.ezK] = Math.Max(life.TryGetValue(h.ezK, out var v) ? v : 0, t - h.ezT);
				hazards = hazards.Where(h => !h.dead).ToList(); particles.Clear(); t += dt;
			}
		}
		catch (Exception e) { Godot.GD.PushWarning("ezScan: " + e.Message); }
		hazards = svH; particles = svP; player = svPl; songT = svT; shake = svS; flash = svF; gloom = svG; gloomT = svGT; rngS = rs0; dry = false; return life;
	}

	public static void ezStart()
	{
		if (!easy) return;
		if (afkOff) { if (cur.ezLifeOld == null) cur.ezLifeOld = ezScan(cur); cur.ezLife = cur.ezLifeOld; }
		else { if (cur.ezLifeNew == null) cur.ezLifeNew = ezScan(cur); cur.ezLife = cur.ezLifeNew; }
		double gp = ezV("ezg", EZ_GAP); ezKE = new byte[events.Count]; double? lb = null; double lk = -1e9; bool kp = true;
		double k = afkEz(ezV("ez", 1)); int m = 0;
		for (int j = 0; j < events.Count; j++)
		{
			var e = events[j];
			if (lb == null || e.b != lb.Value)
			{
				lb = e.b; kp = e.b - lk >= gp - 1e-6;
				if (kp) { lk = e.b; kp = Math.Floor((m + 1) * k + 0.5) > Math.Floor(m * k + 0.5); m++; }
			}
			ezKE[j] = (byte)(kp ? 1 : 0);
		}
	}
	public static void ezCut(int k, string key, bool kp)
	{
		bool keep = kp || (cur.ezLife.TryGetValue(key, out var v) ? v : 0) > ezV("ezlong", EZ_LONG);
		for (int j = k; j < hazards.Count; j++) { var h = hazards[j]; h.ezK = key; h.ezN = 0; if (!keep) h.dead = true; }
	}

	// ---------- a run ----------
	public static void requestStart(double beat)
	{
		if (beat == 0) { hitsTotal = 0; deaths = 0; rec = new RecState(); }
		checkpoint = beat; startBeat = beat; state = "starting"; audioHint = false; startReqAt = performance.now(); introOn = false;
		ensureAudio();
	}

	public static bool cpKeepNew = true;   // false while a replay from before v51 starts an attempt (Replay.cs rpGo)
	public static void beginRun(double cp, int? seed = null)
	{
		runSeed = seed ?? (testSeed != null ? testSeed() : i32(random() * 4294967296)); rngS = hstr(str(runSeed) + ":b");
		hazards = new List<Hz>(); particles = new List<Particle>(); gloom = gloomT = 0; events = cur.build(); ezStart();
		double fromB = cp == 0 ? 0 : cp - 2, fromT = cp == 0 ? 0 : B(fromB);
		evIdx = events.FindIndex(e => e.b >= fromB); if (evIdx < 0) evIdx = events.Count; afkStart(fromB);
		player = new Player { x = 200, y = H / 2, r = easy ? EZ_R : 5, hp = maxHp(cur), inv = 1.0, hurt = 0, dash = 0, cd = 0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		playMusic(fromT); songT = fromT - 0.12; sim0 = songT; simN = 0; dashQ = false; inX = inY = 0;
		// a checkpoint: long-lived things (backgrounds and such, keep) come back with the player. keepNew (v51): an attack that is already out
		// at the restart point and goes on well past the save (L17's star rings, L13's first satellite) comes back too, where it would be by
		// then; replays recorded before v51 (v < 6) play without it, as they were recorded
		if (cp > 0)
		{
			int r0 = rngS;
			for (int j = 0; j < evIdx; j++)
				if (events[j].keep || (events[j].keepNew && cpKeepNew))
				{
					int k = hazards.Count; rngS = hstr(str(runSeed) + ":e" + j); events[j].f();
					for (int i = k; i < hazards.Count; i++) hazards[i].rid = "e" + j + "." + (i - k);
					if (easy) ezCut(k, "e" + j, ezKE[j] != 0);
					// keepNew: one that is already over (a later save) goes at once
					if (!events[j].keep) { for (int i = k; i < hazards.Count; i++) hazards[i].update(songT, 0); for (int i = hazards.Count - 1; i >= k; i--) if (hazards[i].dead) hazards.RemoveAt(i); }
				}
			rngS = r0;
		}
		state = "play";
		if (seed == null && rec != null) { rec.cur = new RecSeg { cp = cp, seed = runSeed, t0 = null, n = 0 }; rec.segs.Add(rec.cur); }
	}

	public static void fireEvents(double t)
	{
		afkFire(t);
		while (evIdx < events.Count && B(events[evIdx].b) <= t)
		{
			int k = hazards.Count; rngS = hstr(str(runSeed) + ":e" + evIdx); events[evIdx].f();
			for (int j = k; j < hazards.Count; j++) hazards[j].rid = "e" + evIdx + "." + (j - k);
			if (easy) ezCut(k, "e" + evIdx, ezKE[evIdx] != 0);
			evIdx++;
		}
	}

	public static void stepHazards(double t, double dt)
	{
		for (int i = 0, n = hazards.Count; i < n; i++)
		{
			var h = hazards[i]; int k = hazards.Count;
			rngS = h.rs ?? hstr(str(runSeed) + ":" + h.rid);
			h.update(t, dt); h.rs = rngS;
			if (hazards.Count > k) { for (int j = k; j < hazards.Count; j++) hazards[j].rid = h.rid + "." + (h.rc = h.rc + 1); }
			if (easy && hazards.Count > k)
			{
				bool kp = t - (h.ezL ?? -1e9) >= ezV("ezc", ezV("ezg", EZ_GAP)) * SPB - 1e-6;
				if (kp) h.ezL = t;
				ezCut(k, h.ezK + "." + h.ezN, kp); h.ezN++;
			}
		}
	}

	public static void damage()
	{
		var p = player; if (pr.on) prHitRec = true;
		if (pr.on && pr.inf) { hitsTotal++; p.inv = 1.0; p.hurt = 1.0; flash = 0.2; burst(p.x, p.y, CYAN, 12, 300); sfx(t => sNoise(t, 0.2, "lowpass", 900, 0.22)); return; }
		p.hp--; hitsTotal++; p.inv = 2.0; p.hurt = 2.0; shake = 16; flash = 0.3;
		burst(p.x, p.y, CYAN, 22, 380); rumble(220, 0.8, 0.6);
		sfx(t => { sTone(t, 110, 0.3, "square", 0.18); sNoise(t, 0.25, "lowpass", 900, 0.3); });
		if (p.hp <= 0 && bnOn) { bnDown(); return; }   // online boss: down, until someone touches it (UI/BossNet.cs)
		if (p.hp <= 0) { state = "dead"; deaths++; deadAt = performance.now(); deadPct = clamp(songT / B(cur.end), 0, 1); stopMusic(); burst(p.x, p.y, CYAN, 50, 520); rumble(500, 1, 1); }
	}

	// v41: the game moves in fixed steps of song time, whatever the frame rate, so every screen (60, 144, 240 Hz...) plays exactly the same.
	// One step is what a 240 Hz frame used to be. sim0 is where the run started; step n is at sim0 + n*STEP.
	public const double STEP = 1.0 / 240, SIM_MAX = 0.5;
	public static double sim0 = 0; public static long simN = 0;
	public static bool dashQ = false;      // a dash press waiting for the next step (a frame can have no step above 240 FPS)
	static double inX = 0, inY = 0;        // the stick/arrows as the previous frame read them
	public static Func<int> testSeed;      // tests only: run seeds that don't depend on the frame rate

	public static void updatePlay(double dt, Inp inp)
	{
		double target = (actx.currentTime - songStart) * spd;
		if (inp.dash) dashQ = true;
		// a long freeze (window dragged, PC busy): jump ahead instead of running half a second of steps at once
		if (target - (sim0 + simN * STEP) > SIM_MAX) simN = (long)Math.Floor((target - SIM_MAX - sim0) / STEP);
		// steps before this frame's moment still use what the previous frame read; the new input (and a dash) starts at the step of this moment.
		// So the same input at the same moment gives the same game at any frame rate.
		int n = 0;
		while (state == "play" && sim0 + (simN + 1) * STEP <= target + 1e-9)
		{
			simN++; n++; bool now = sim0 + (simN + 1) * STEP > target + 1e-9;
			playStep(sim0 + simN * STEP, STEP, now ? inp.x : inX, now ? inp.y : inY, now);
		}
		if (n > 0) { inX = inp.x; inY = inp.y; }
		if (n > 0 && player != null) { var p = player; p.trail.Add(new TrailPt { x = p.x, y = p.y, d = p.dash > 0 }); if (p.trail.Count > 10) p.trail.RemoveAt(0); }
	}

	static void playStep(double t, double dt, double mx, double my, bool now)
	{
		songT = t;
		fireEvents(t);
		var p = player; double m = hypot(mx, my); if (m > 1) { mx /= m; my /= m; }
		if (m > 0.2) { p.fx = mx / m; p.fy = my / m; }
		bool dq = now && dashQ; if (now) dashQ = false;
		if (bnMeDown) { mx = my = 0; m = 0; dq = false; }   // down: it stays where it fell
		if (dq && p.cd <= 0)
		{
			p.dash = 0.18; p.cd = 0.32; p.inv = Math.Max(p.inv, 0.28);
			p.ddx = m > 0.2 ? p.fx : p.fx; p.ddy = m > 0.2 ? p.fy : p.fy;
			p.dt0 = t; { double da = JS.atan2(p.ddy, p.ddx), q = Math.PI / 2; p.ang = da + round((p.ang - da) / q) * q; p.sa = da; }
			burst(p.x, p.y, CYAN, 8, 200); sfx(tt => sNoise(tt, 0.12, "bandpass", 3200, 0.18, 1));
		}
		double vx, vy;
		if (p.dash > 0) { vx = p.ddx * 950; vy = p.ddy * 950; } else { vx = mx * 370; vy = my * 370; }
		p.x = clamp(p.x + vx * dt, 10, W - 10); p.y = clamp(p.y + vy * dt, 10, H - 10);
		if (p.dash <= 0) { double sp = hypot(vx, vy); if (sp > 1) { double q = Math.PI / 2, ma = JS.atan2(vy, vx); double df = ma - p.ang; df -= round(df / q) * q; p.ang += df * Math.Min(1, dt * 16); } }
		{
			double sp = hypot(vx, vy), tg = p.dash > 0 ? 1 : Math.Min(1, sp / 370) * 0.3; if (sp > 1) p.sa = JS.atan2(vy, vx);
			double sk = p.sk ?? 0; p.sk = sk + (tg - sk) * Math.Min(1, dt * (tg > sk ? 30 : 12));
		}
		p.dash -= dt; p.cd -= dt; p.inv -= dt; p.hurt -= dt;
		skinTrailAdd(p);

		stepHazards(t, dt);
		if (p.inv <= 0 && !bnMeDown) { foreach (var h in hazards) { if (!h.dead && h.hit(p, t)) { damage(); break; } } }
		hazards = hazards.Where(h => !h.dead).ToList();
		recSample(t);

		double beat = BT(t);
		foreach (var c in cur.cps)
		{
			if (beat >= c && c > checkpoint) { checkpoint = c; cpMsg = 1.8; sfx(tt => { sTone(tt, 659, 0.25, "square", 0.06); sTone(tt + 0.08, 880, 0.3, "square", 0.06); }); }
		}
		if (pr.on && t >= B(cur.end) + 0.4 && state == "play") prFinish();
		else if (t >= B(cur.end) + 0.4 && state == "play")
		{
			lastRank = (hitsTotal == 0 && deaths == 0) ? "S" : deaths == 0 ? "A" : deaths == 1 ? "B" : "C"; lastEz = easy;
			double p0 = ptsOf(easy); xpMark(); string bk = easy ? "bestE" : "best"; string curBest = easy ? cur.bestE : cur.best;
			int rb = curBest != null ? Array.IndexOf(RANKS, curBest) : 99, rn = Array.IndexOf(RANKS, lastRank);
			if (rn < rb)
			{
				if (easy) cur.bestE = lastRank; else cur.best = lastRank;
				localStorage.setItem((easy ? "mg_beste_" : "mg_best_") + cur.name, lastRank); rpWin(lastRank); markDirty();
			}
			else { rpWin(lastRank); if (acc != null && !acc.dirty) sync(); }
			lastGain = ptsOf(easy) - p0; awardXp(); state = "win"; winAt = performance.now();
			{ double bt0 = BT(t + 0.35), nb = Math.Ceiling(bt0); winSlam = winAt + (B(nb) - t) * 1000; winHit = false; }
			burst(p.x, p.y, CYAN, 40, 450);
			if (bnOn) bnFinish();
		}
	}
}
