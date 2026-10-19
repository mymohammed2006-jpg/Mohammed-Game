using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

public class RpSeg { public double cp; public int seed; public double t0; public int n; public ushort[] b; }
public class RpDoc { public string r; public int v; public int hz; public List<RpSeg> segs; public string sk = ""; }
public class RpPerson { public string name, uid; }
public class RpState
{
	public RpPerson p; public Level L; public string m = "n"; public RpDoc doc; public int seg; public bool err; public double gapAt; public bool gapDead; public double endAt;
	public bool saveEasy, paused; public double rate = 1; public bool hide; public double barT, aT, aCT; public double[] off = Array.Empty<double>(); public double tot;
	public bool bot; public string back;
}

// original index.html lines 7405-7539: the replay player (the recorder is recSample in Play.cs; saving/uploading is the UI thread's)
public static partial class G
{
	public const int RP_MAX_SEGS = 100, RP_MAX_BYTES = 900000;
	public const int RP_V = 3;
	public static readonly RpState rp = new();
	public static readonly double[] RP_RATES = { 0.25, 0.5, 0.75, 1, 1.5, 2 };
	public const double RP_JUMP = 5, RP_SMALL = 1, RP_FINE = 0.1, RP_ST = 1.0 / 120;

	public static ushort[] unpackRp(string b64)
	{
		var s = Convert.FromBase64String(b64); int n = s.Length / 5; var a = new ushort[n * 3];
		for (int i = 0, j = 0; i < n; i++, j += 5) { a[i * 3] = (ushort)(s[j] | (s[j + 1] << 8)); a[i * 3 + 1] = (ushort)(s[j + 2] | (s[j + 3] << 8)); a[i * 3 + 2] = s[j + 4]; }
		return a;
	}

	public static double rpFrom(RpSeg s) => s.cp == 0 ? 0 : B(s.cp - 2);
	public static double rpLen(RpSeg s) => Math.Max(0, s.t0 - rpFrom(s)) + (s.n - 1) / (double)rp.doc.hz;
	public static void rpTimeline() { double o = 0; rp.off = rp.doc.segs.Select(s => { double a = o; o += rpLen(s); return a; }).ToArray(); rp.tot = o; }
	public static void rpKill() { if (src == null) return; src.stop(); src = null; }
	// starts attempt i from second T of the song (no T = from its start)
	public static void rpGo(int i, double? T = null)
	{
		rp.seg = i; var s = rp.doc.segs[i]; double T0 = rpFrom(s); checkpoint = s.cp; stopMusic();
		afkV = rp.bot ? 3 : Math.Min(3, rp.doc.v != 0 ? rp.doc.v : 1); afkOff = afkV != 2; beginRun(s.cp, s.seed); afkV = 3; afkOff = true; rpKill(); state = "replay"; player.inv = 1e9;
		double t1 = T == null ? T0 : Math.Max(T0, T.Value);
		if (t1 > T0) { dry = true; for (double t = T0; t < t1 - 1e-9;) { double d = Math.Min(RP_ST, t1 - t); t += d; rpApply(t, d, false); } dry = false; particles = new List<Particle>(); shake = flash = 0; cpMsg = 0; }
		playMusic(t1); if (src != null) src.playbackRate.value = rp.rate; rp.aT = t1; rp.aCT = songStart + t1; songT = t1;
	}
	public static void rpSeg(int i) => rpGo(i);
	public static void rpStart() { rp.saveEasy = easy; easy = rp.m == "e"; cur = rp.L; levelTiming(rp.L); hitsTotal = 0; deaths = 0; rp.paused = false; rp.rate = 1; rp.barT = 4; rpTimeline(); rpSeg(0); }
	public static void exitReplay() { stopMusic(); player = null; hazards = new List<Hz>(); particles = new List<Particle>(); gloom = gloomT = 0; easy = rp.saveEasy; rp.paused = false; state = rp.bot ? rp.back : "profile"; rp.bot = false; actx.resume(); }
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
		rp.rate = r; if (src != null) src.playbackRate.setValueAtTime(r, now);
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
		if (k) rp.barT = 3;
	}
	// one replay step at second t: the square on the recorded path, the attacks run (fx=false while seeking: no effects)
	public static void rpApply(double t, double dt, bool fx)
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
		int hp = (fl >> 1) & 7; double prevHp = p.hp;
		if (hp < p.hp && fx) { hitsTotal++; p.hurt = 2.0; shake = 16; flash = 0.3; burst(p.x, p.y, CYAN, 22, 380); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); }); }
		p.hp = hp; p.hurt -= dt; p.trail.Add(new TrailPt { x = p.x, y = p.y, d = p.dash > 0 }); if (p.trail.Count > 10) p.trail.RemoveAt(0); skinTrailAdd(p);
		fireEvents(t); stepHazards(t, dt);
		if (hp < prevHp) { foreach (var h in hazards) { if (!h.dead && h.hit(p, t)) break; } }
		hazards = hazards.Where(h => !h.dead).ToList();
		double beat = BT(t); foreach (var c in cur.cps) { if (beat >= c && c > checkpoint) { checkpoint = c; if (fx) cpMsg = 1.8; } }
	}
	public static void updateReplay(double dt)
	{
		if (rp.paused) return;
		double t = rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; var s = rp.doc.segs[rp.seg]; var b = s.b; var p = player;
		if ((t - s.t0) * rp.doc.hz >= s.n - 1)
		{
			int lf = b[(s.n - 1) * 3 + 2];
			if (rp.seg < rp.doc.segs.Count - 1) { rp.gapDead = ((lf >> 1) & 7) == 0; if (rp.gapDead) { burst(p.x, p.y, CYAN, 50, 520); deaths++; } stopMusic(); state = "rpGap"; rp.gapAt = performance.now(); }
			else { stopMusic(); state = "rpEnd"; rp.endAt = performance.now(); }
			return;
		}
		rpApply(t, dt * rp.rate, true);
	}
}
