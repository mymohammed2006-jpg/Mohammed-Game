using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

public class RpSeg { public double cp; public int seed; public double t0; public int n; public ushort[] b; public bool c; public double fr = -1; public int oi; }   // oi: its place in the whole replay (v46 "بدون الموتات")   // fr: a practice run (v46), it starts at t0, the level ran from 0 with the bot before it   // c: cut before its death (v46, a long Hardcore win: only the attempts that won)
public class RpDoc { public string r; public int v; public int hz; public List<RpSeg> segs; public string sk = ""; public string res; }   // res: a replay on the PC (v46) that was lost ("dead") or not finished ("mid")
public class RpPerson { public string name, uid; }
public class RpState
{
	public RpPerson p; public Level L; public string m = "n"; public RpDoc doc; public int seg; public bool err; public double gapAt; public bool gapDead; public double endAt;
	public bool saveEasy, paused; public double rate = 1; public bool hide; public double barT, aT, aCT; public double[] off = Array.Empty<double>(); public double tot;
	public bool bot, local, plat; public string back;
	public bool cutOk, cutOn; public List<RpSeg> full, cut;   // v46 (his words 20:10 10-22): a replay on the PC can be watched without its deaths (a box to tick)
	public bool team;   // the co-op level (Plat/TeamRp.cs; plat is true too)
	public bool pr; public int prLv; public double rate0 = 1; public RpDoc prBot; public int hitI;   // a practice replay (v46): its lives (0 = never end), its speed, the bot it followed before its start   // local: a replay saved on the PC (v46, UI/MyRp.cs)
}

// original index.html lines 7405-7539: the replay player (the recorder is recSample in Play.cs; saving/uploading is the UI thread's)
public static partial class G
{
	public const int RP_MAX_SEGS = 100, RP_MAX_BYTES = 900000;
	public const int RP_V = 7;   // 4 = recorded with fixed steps (v41); 1-3 play back the old way; 5 (v44) = every gap is a death (pause "back to the last save" counts one);
	                             // 6 (v51) = the keepNew attacks come back after a death (Play.cs beginRun); 7 (v52) = the levels with new attacks (older ones play their v51 design, rpLevel)
	public static readonly RpState rp = new();
	public static readonly double[] RP_RATES = { 0.25, 0.5, 0.75, 1, 1.5, 2 };
	public const double RP_JUMP = 5, RP_SMALL = 1, RP_FINE = 0.1, RP_ST = 1.0 / 120;

	public static ushort[] unpackRp(string b64)
	{
		var s = Convert.FromBase64String(b64); int n = s.Length / 5; var a = new ushort[n * 3];
		for (int i = 0, j = 0; i < n; i++, j += 5) { a[i * 3] = (ushort)(s[j] | (s[j + 1] << 8)); a[i * 3 + 1] = (ushort)(s[j + 2] | (s[j + 3] << 8)); a[i * 3 + 2] = s[j + 4]; }
		return a;
	}

	public static bool rpFixed() => rp.doc.v >= 4;   // the bot's old runs have no v (old timing); its new ones (v 4) are on fixed steps
	public static double rpFrom(RpSeg s) => s.fr >= 0 ? s.t0 : s.cp == 0 ? 0 : B(s.cp - 2);
	public static double rpLen(RpSeg s) => Math.Max(0, s.t0 - rpFrom(s)) + (s.n - 1) / (double)rp.doc.hz;
	public static void rpTimeline() { double o = 0; rp.off = rp.doc.segs.Select(s => { double a = o; o += rpLen(s); return a; }).ToArray(); rp.tot = o; }
	public static void rpKill() { if (rp.plat) plRpStopMusic(); if (src == null) return; src.stop(); src = null; }
	// starts attempt i from second T of the song (no T = from its start)
	public static void rpGo(int i, double? T = null)
	{
		if (rp.plat) { plRpGo(i, T); return; }   // v46: the platformer's (Plat/PlatRp.cs)
		rp.seg = i; rp.hitI = -1; var s = rp.doc.segs[i]; double T0 = rpFrom(s); checkpoint = s.cp; stopMusic();
		afkV = rp.bot ? 3 : Math.Min(3, rp.doc.v != 0 ? rp.doc.v : 1); afkOff = afkV != 2; cpKeepNew = rp.doc.v >= 6; beginRun(s.cp, s.seed); cpKeepNew = true; afkV = 3; afkOff = true; rpKill(); state = "replay"; player.inv = 1e9;
		double t1 = T == null ? T0 : Math.Max(T0, T.Value);
		if (rpFixed())
		{
			// the same steps the player had, from the run's start, so the attacks are exactly what they saw
			dry = true; while (sim0 + (simN + 1) * STEP <= t1 + 1e-9) { simN++; double t = sim0 + simN * STEP; if (s.fr >= 0 && t < s.t0) rpBotStep(t); else rpApply(t, STEP, false); } dry = false;
			particles = new List<Particle>(); shake = flash = 0; cpMsg = 0; player.trail.Clear();
		}
		else if (t1 > T0) { dry = true; for (double t = T0; t < t1 - 1e-9;) { double d = Math.Min(RP_ST, t1 - t); t += d; rpApply(t, d, false); } dry = false; particles = new List<Particle>(); shake = flash = 0; cpMsg = 0; }
		playMusic(t1); if (src != null) src.playbackRate.value = rp.rate; rp.aT = t1; rp.aCT = songStart + t1; if (!rpFixed()) songT = t1;
	}
	// practice: before its start the level ran fast with the square on the bot's path (Flow.cs prBegin), the same here
	static void rpBotStep(double t)
	{
		songT = t; var q = rp.prBot != null ? botPos(rp.prBot, t) : null; if (q != null) { player.x = q.x; player.y = q.y; }
		fireEvents(t); stepHazards(t, STEP); hazards = hazards.Where(h => !h.dead).ToList();
	}
	public static void rpSeg(int i) => rpGo(i);
	// the level a replay plays on: a player's replay from before v52 (v < 7) on the old design of a level whose attacks changed; the bot's runs are always on the current one
	public static Level rpLevel()
	{
		var L = rp.L; if (rp.bot || rp.plat || rp.doc == null || rp.doc.v >= 7 || L.v51 == null) return L;
		var O = L.v51; O.buf = L.buf; O.hc = L.hc; O.boss = L.boss; O.best = L.best; O.bestE = L.bestE; return O;
	}
	public static void rpStart()
	{
		rp.saveEasy = easy; easy = rp.m == "e"; cur = rpLevel(); levelTiming(cur);
		if (cur.isV51 && rp.prBot != null) rp.prBot = botDoc(cur, rp.m) ?? rp.prBot;   // a practice replay follows the bot run of its time
		hitsTotal = 0; deaths = 0; rp.paused = false; rp.rate = rp.local && rp.rate0 > 0 ? rp.rate0 : 1; rp.barT = 4;
		rpCutPrep(rp.local ? rpCutSegs(rp.doc.segs, rp.doc.hz, s => B(s.cp - 2)) : null);
		rpTimeline(); rpSeg(0);
	}
	// ---------- v46: "بدون الموتات" (his words 20:10 10-22): the attempts that made it, each cut where the next one starts ----------
	// The last attempt, before it the last one that started from an earlier save (it reached the save the next one starts from), and so
	// on back to the start (like the long Hardcore wins on the server, Online.cs rpWinPath). from(s) = when attempt s started on the clock.
	public static List<RpSeg> rpCutSegs(List<RpSeg> all, int hz, Func<RpSeg, double> from, Func<int, double> key = null)
	{
		if (all == null || all.Count < 2) return null; key ??= i => all[i].cp;
		var idx = new List<int> { all.Count - 1 };
		for (int i = all.Count - 2; i >= 0 && key(idx[0]) > 0; i--) if (key(i) < key(idx[0])) idx.Insert(0, i);
		if (idx.Count == all.Count) return null;   // no death to leave out
		var o = new List<RpSeg>();
		for (int k = 0; k < idx.Count; k++)
		{
			var s = all[idx[k]]; var c = new RpSeg { cp = s.cp, seed = s.seed, t0 = s.t0, n = s.n, b = s.b, c = s.c, fr = s.fr, oi = idx[k] };
			if (k < idx.Count - 1) { c.n = (int)clamp(Math.Floor((from(all[idx[k + 1]]) - s.t0) * hz) + 1, 2, s.n); c.c = true; }
			o.Add(c);
		}
		return o;
	}
	static void rpCutPrep(List<RpSeg> cut)
	{
		for (int i = 0; i < rp.doc.segs.Count; i++) rp.doc.segs[i].oi = i;
		rp.full = rp.doc.segs; rp.cut = cut; rp.cutOk = cut != null; rp.cutOn = false;
	}
	// the box ticked or not: the same moment in the other one (in the whole replay a cut part's moment is always there; the other way, a
	// moment left out goes on from the next kept part)
	static void rpCutToggle()
	{
		if (!rp.cutOk) return;
		if (rp.team) { tmRpCutToggle(); return; }
		var a = rp.doc.segs[rp.seg]; double tIn = rp.plat ? songT : songT;   // the platformer's parts count from 0 (songT = the part's second)
		bool end = state == "rpEnd"; int oi = a.oi;
		rp.cutOn = !rp.cutOn; rp.doc.segs = rp.cutOn ? rp.cut : rp.full; if (rp.plat) plRpCutSwap();
		rpTimeline();
		int j; double? T = tIn;
		if (end) { rpKill(); state = "rpEnd"; rp.seg = rp.doc.segs.Count - 1; rp.endAt = performance.now(); return; }
		if (!rp.cutOn) j = oi;
		else
		{
			j = rp.doc.segs.FindIndex(s => s.oi == oi);
			if (j >= 0) { var s = rp.doc.segs[j]; double len = (s.n - 1) / (double)rp.doc.hz; if ((rp.plat ? tIn : tIn - s.t0) > len) j = -1; }
			if (j < 0) { j = rp.doc.segs.FindIndex(s => s.oi > oi); T = null; if (j < 0) { j = rp.doc.segs.Count - 1; T = null; } }
		}
		bool wasPaused = rp.paused; if (wasPaused) rpPause(false);
		rpGo(j, T); if (wasPaused) rpPause(true);
	}
	public static void exitReplay() { stopMusic(); player = null; hazards = new List<Hz>(); particles = new List<Particle>(); gloom = gloomT = 0; easy = rp.saveEasy; if (cur != null && cur.isV51) cur = rp.L; rp.paused = false; state = rp.bot || rp.local ? rp.back : "profile"; if (rp.plat) plRpExit(); rp.bot = rp.local = rp.plat = rp.pr = rp.team = false; rp.rate0 = 1; rp.prBot = null; rp.cutOk = rp.cutOn = false; rp.full = rp.cut = null; actx.resume(); }
	// where the whole replay is (all attempts back to back), in seconds
	public static double rpPos()
	{
		if (state == "rpEnd") return rp.tot; var s = rp.doc.segs[rp.seg]; double len = rpLen(s);
		return rp.off[rp.seg] + (state == "rpGap" ? len : clamp(songT - rpFrom(s), 0, len));
	}
	public static void rpSeekTo(double x)
	{
		x = clamp(x, 0, Math.Max(0, rp.tot - 0.05)); int i = 0; while (i < rp.doc.segs.Count - 1 && x >= rp.off[i + 1]) i++;
		rpGo(i, rpFrom(rp.doc.segs[i]) + x - rp.off[i]);
	}
	public static void rpPause(bool v) { rp.paused = v; if (v) { shake = 0; actx.suspend(); } else actx.resume(); }
	public static void rpSetRate(double r)
	{
		double now = actx.currentTime; if (state == "replay" && now > rp.aCT) { rp.aT = rp.aT + (now - rp.aCT) * rp.rate; rp.aCT = now; }
		rp.rate = r; if (src != null) src.playbackRate.setValueAtTime(r, now); if (rp.plat) plRpRate(r);
	}
	public static void rpInput(Inp inp, double dt)
	{
		rp.barT = Math.Max(0, rp.barT - dt); bool k = false;
		if (inp.confirm) { k = true; if (state == "rpEnd") { rpPause(false); rpSeekTo(0); } else rpPause(!rp.paused); }
		// right/left 5 s, L1/R1 one second (a tenth when paused)
		double sm = rp.paused ? RP_FINE : RP_SMALL, j = inp.left ? -RP_JUMP : inp.right ? RP_JUMP : inp.l1 ? -sm : inp.r1 ? sm : 0;
		if (j < 0) { k = true; rpSeekTo(rpPos() + j); }
		else if (j > 0 && state != "rpEnd") { k = true; double x = rpPos() + j; if (x >= rp.tot - 0.05) { rpKill(); state = "rpEnd"; rp.endAt = performance.now(); } else rpSeekTo(x); }
		if (inp.up || inp.down) { k = true; int i = (int)clamp(Array.IndexOf(RP_RATES, rp.rate) + (inp.up ? 1 : -1), 0, RP_RATES.Length - 1); rpSetRate(RP_RATES[i]); }
		if (inp.restart) rp.hide = !rp.hide;
		if (inp.sq && rp.cutOk) { k = true; rpCutToggle(); }
		if (k) rp.barT = 3;
	}
	// one replay step at second t: the square on the recorded path, the attacks run (fx=false while seeking: no effects)
	public static void rpApply(double t, double dt, bool fx, bool trail = true)
	{
		songT = t; var s = rp.doc.segs[rp.seg]; int hz = rp.doc.hz; var p = player; var b = s.b;
		double fi = clamp((t - s.t0) * hz, 0, Math.Max(0, s.n - 1.0001)); int i = (int)Math.Floor(fi); double k = fi - i; int o = i * 3, o2 = Math.Min(i + 1, s.n - 1) * 3, fl = b[o + 2];
		double nx = (b[o] + (b[o2] - b[o]) * k) / 50, ny = (b[o + 1] + (b[o2 + 1] - b[o + 1]) * k) / 50, dx = nx - p.x, dy = ny - p.y, d = hypot(dx, dy);
		bool wd = p.dash > 0; p.dash = (fl & 1) != 0 ? 0.1 : 0; if (d > 0.5) { p.ddx = dx / d; p.ddy = dy / d; }
		if (p.dash > 0 && !wd) { p.dt0 = t; double da = JS.atan2(p.ddy, p.ddx), q = Math.PI / 2; p.ang = da + round((p.ang - da) / q) * q; p.sa = da; }
		{
			double sp = Math.Min(950, d / Math.Max(dt, 1e-4)), tg = p.dash > 0 ? 1 : Math.Min(1, sp / 370) * 0.3, q = Math.PI / 2;
			if (sp > 1 && d > 0.05) { double ma = JS.atan2(dy, dx); p.sa = ma; if (p.dash <= 0) { double df = ma - p.ang; df -= round(df / q) * q; p.ang += df * Math.Min(1, dt * 16); } }
			double sk = p.sk ?? 0; p.sk = sk + (tg - sk) * Math.Min(1, dt * (tg > sk ? 30 : 12));
		}
		p.x = nx; p.y = ny;
		int hp = (fl >> 1) & 7; bool hit = hp < p.hp;
		if (s.fr >= 0) { hp |= (fl & 32) != 0 ? 8 : 0; hit = false; for (int j = Math.Max(0, rp.hitI + 1); j <= i; j++) if ((b[j * 3 + 2] & 16) != 0) hit = true; rp.hitI = i; }   // practice: its hits are marked (lives that never end don't go down)
		if (hit && fx && s.fr >= 0 && rp.prLv == 0) { hitsTotal++; p.hurt = 1.0; flash = 0.2; burst(p.x, p.y, CYAN, 12, 300); sfx(tt => sNoise(tt, 0.2, "lowpass", 900, 0.22)); }
		else if (hit && fx) { hitsTotal++; p.hurt = 2.0; shake = 16; flash = 0.3; burst(p.x, p.y, CYAN, 22, 380); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); }); }
		p.hp = hp; p.hurt -= dt; if (trail) { p.trail.Add(new TrailPt { x = p.x, y = p.y, d = p.dash > 0 }); if (p.trail.Count > 10) p.trail.RemoveAt(0); } skinTrailAdd(p);
		fireEvents(t); stepHazards(t, dt);
		if (hit) { foreach (var h in hazards) { if (!h.dead && h.hit(p, t)) break; } }
		hazards = hazards.Where(h => !h.dead).ToList();
		double beat = BT(t); foreach (var c in cur.cps) { if (beat >= c && c > checkpoint) { checkpoint = c; if (fx) cpMsg = 1.8; } }
	}
	public static void updateReplay(double dt)
	{
		if (rp.plat) { plRpUpdate(dt); return; }
		if (rp.paused) return;
		double t = rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; var s = rp.doc.segs[rp.seg]; var b = s.b; var p = player;
		if ((t - s.t0) * rp.doc.hz >= s.n - 1)
		{
			int lf = b[(s.n - 1) * 3 + 2];
			if (rp.seg < rp.doc.segs.Count - 1 && s.c) { rpSeg(rp.seg + 1); return; }   // cut: straight on to the next attempt, no death
			if (rp.seg < rp.doc.segs.Count - 1) { rp.gapDead = ((lf >> 1) & 7) == 0; if (rp.gapDead) burst(p.x, p.y, CYAN, 50, 520); if (rp.gapDead || rp.doc.v >= 5) deaths++; stopMusic(); state = "rpGap"; rp.gapAt = performance.now(); }
			else { stopMusic(); state = "rpEnd"; rp.endAt = performance.now(); }
			return;
		}
		if (rpFixed())
		{
			int n = 0; while (sim0 + (simN + 1) * STEP <= t + 1e-9 && n < 240) { simN++; n++; rpApply(sim0 + simN * STEP, STEP, true, false); }
			if (n > 0) { p.trail.Add(new TrailPt { x = p.x, y = p.y, d = p.dash > 0 }); if (p.trail.Count > 10) p.trail.RemoveAt(0); }
			return;
		}
		rpApply(t, dt * rp.rate, true);
	}
}
