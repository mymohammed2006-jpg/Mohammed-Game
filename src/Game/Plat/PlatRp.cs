using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// New in v46 (Mohammed 10-22): the platformer's replay, kept on the PC only (nothing goes to the server; UI/MyRp.cs lists them).
// The recording: the square RP_HZ times a second in parts (a new part after each death or "back to the last save"), on the
// level's clock (pl.t, which the saws and beat blocks follow), plus the pieces taken and the saves reached. Online (against
// someone) the other square is recorded too, as this PC showed it. The co-op level for three has no replay.
// Watching it uses the main mode's replay bar (Replay.cs); the parts' times there count from 0, the real clock is in plrp.t0.
public static partial class G
{
	public class PlRec { public List<RecSeg> segs = new(), gsegs = new(); public RecSeg cur, gcur; public List<double[]> ev = new(); public string gname, gsk; }
	public static PlRec plRec;
	public class PlRpData { public PlatLevel L; public double[] t0; public List<double[]> ev; public List<RpSeg> gs; public string gname, gsk; public Voice[] voices; public int evDone; public double[] t0Full, t0Cut; public List<RpSeg> gsFull, gsCut; }
	static PlRpData plrp;
	// sample flags: 1 dash, 2-14 hearts (<<1), 16 facing left, 32 hidden (inside the gate / the other one dead), 64 down (co-op)
	const int PF_LEFT = 16, PF_HIDE = 32, PF_DOWN = 64;

	// ---------- recording ----------
	static void plRecStart() { plRec = new PlRec(); if (online && net != null) { plRec.gname = net.other; plRec.gsk = net.otherSk; } plRecSeg(); }
	static void plRecSeg()
	{
		if (plRec == null) return;
		plRec.cur = new RecSeg(); plRec.segs.Add(plRec.cur);
		if (online && net != null) { plRec.gcur = new RecSeg(); plRec.gsegs.Add(plRec.gcur); } else plRec.gcur = null;
	}
	static void plRecEv(int k, int tx, int ty) => plRec?.ev.Add(new double[] { pl.t, k, tx, ty });
	static void plRecSample()
	{
		var r = plRec; if (r?.cur == null || player == null) return; var c = r.cur; var p = player; double t = pl.t; if (c.t0 == null) c.t0 = t;
		Player gp = r.gcur != null ? netGhost(performance.now()) : null;
		while (c.t0.Value + (double)c.n / RP_HZ <= t && c.n < 60000)
		{
			c.b.Add((int)round(clamp(p.x, 0, 1300) * 50)); c.b.Add((int)round(clamp(p.y, 0, 1300) * 50));
			c.b.Add((p.dash > 0 ? 1 : 0) | ((int)clamp(p.hp, 0, 7) << 1) | (p.fx < 0 ? PF_LEFT : 0) | (pl.atGate ? PF_HIDE : 0) | (pl.down ? PF_DOWN : 0)); c.n++;
			if (r.gcur != null)
			{
				var gc = r.gcur; int ln = gc.b.Count;
				if (gp != null) { gc.b.Add((int)round(clamp(gp.x, 0, 1300) * 50)); gc.b.Add((int)round(clamp(gp.y, 0, 1300) * 50)); gc.b.Add(gp.dash == -1 ? PF_DOWN : (gp.dash > 0 ? 1 : 0) | (gp.fx < 0 ? PF_LEFT : 0)); }
				else if (ln >= 3) { gc.b.Add(gc.b[ln - 3]); gc.b.Add(gc.b[ln - 2]); gc.b.Add(PF_HIDE); }
				else { gc.b.Add(0); gc.b.Add(0); gc.b.Add(PF_HIDE); }
				gc.t0 ??= t; gc.n++;
			}
		}
	}
	// saves the platformer run on the PC
	static bool plRpSave(string res, string letter)
	{
		var r = plRec; if (r == null || pl == null) return false;
		var segs = rpSegs(r.segs); if (segs.Count == 0) { showCorner("ما فيه شي ينحفظ للحين", "#ffb35c", 2400); return false; }
		// the other square's parts go with the square's (the same times); a part with too few samples was left out of both
		var gs = r.gsegs.Count == r.segs.Count ? r.segs.Select((s, i) => (s, i)).Where(q => q.s.n > 1).Select(q => r.gsegs[q.i]).Select(s => new RpqSeg { t0 = s.t0 ?? 0, n = s.n, d = packRp(s.b) }).ToList() : null;
		var it = new MyRp { kind = "plat", r = res == "win" ? letter : null, ev = r.ev.ToList(), gs = gs, gname = r.gname, gsk = r.gsk };
		return myRpWrite(it, pl.L.id, "n", res, segs);
	}

	// ---------- watching ----------
	static bool plRpReady() => rp.team ? tmRpReady() : plrp?.L != null && (noMus || plrp.L.bufs == null || plrp.L.bufs.All(b => b != null)) && actx.state == "running";
	public static void plRpStart()
	{
		if (rp.team) { tmRpStart(); return; }
		rp.saveEasy = easy; easy = false; hitsTotal = 0; deaths = 0; rp.paused = false; rp.rate = 1; rp.barT = 4; plRpCutPrep(); rpTimeline(); plRpGo(0, null);
	}
	// v46 "بدون الموتات": a part starts from the save reached before it (the saves in ev); it is cut where it reached the save the next kept part starts from
	static void plRpCutPrep()
	{
		var segs = rp.doc.segs; var cps = plrp.ev.Where(e => e[1] == 1).Select(e => e[0]).ToList();
		int Key(int i) => cps.Count(t => t <= plrp.t0[i] + 1e-6);
		var real = segs.Select((s, i) => new RpSeg { cp = s.cp, seed = s.seed, t0 = plrp.t0[i], n = s.n, b = s.b, c = s.c }).ToList();
		var cut = rpCutSegs(real, rp.doc.hz, s => { int k = Key(real.IndexOf(s)); return k > 0 && k <= cps.Count ? cps[k - 1] : s.t0; }, i => Key(i));
		rpCutPrep(cut); plrp.t0Full = plrp.t0; plrp.gsFull = plrp.gs;
		if (cut == null) return;
		plrp.t0Cut = cut.Select(s => s.t0).ToArray(); foreach (var s in cut) s.t0 = 0;
		plrp.gsCut = plrp.gs == null ? null : cut.Select(s => s.oi < plrp.gs.Count ? plrp.gs[s.oi] : new RpSeg { n = 0, b = new ushort[3] }).ToList();
	}
	static void plRpCutSwap() { plrp.t0 = rp.cutOn ? plrp.t0Cut : plrp.t0Full; plrp.gs = rp.cutOn ? plrp.gsCut : plrp.gsFull; }
	static void plRpStopMusic() { if (rp.team) { tmRpStopMusic(); return; } if (plrp?.voices == null) return; foreach (var v in plrp.voices) if (v != null) { v.gain.setTargetAtTime(0, actx.currentTime, 0.04); v.stop(actx.currentTime + 0.3); } plrp.voices = null; }
	// the song's stems from the level's second t (the loop like plGo's: it starts at loop[0] when t = 0)
	static void plRpMusic(double t)
	{
		plRpStopMusic(); var L = plrp.L; if (noMus || L.bufs == null) return;
		double a = L.loop[0], len = L.loop[1] - L.loop[0], wait = Math.Max(0, -t) / rp.rate, off = t <= 0 ? a : a + (len > 0 ? t % len : 0);
		double when = actx.currentTime + 0.05 + wait; plrp.voices = new Voice[L.bufs.Length];
		for (int i = 0; i < L.bufs.Length; i++)
		{
			if (L.bufs[i] == null) continue;
			var v = new Voice { song = L.bufs[i], loop = true, loopStart = L.loop[0], loopEnd = L.loop[1] }; v.playbackRate.value = rp.rate;
			v.gain.value = pl.stemOn[i] ? 1 : 0; v.start(when, off); plrp.voices[i] = AudioCtx.Add(v);
		}
	}
	static void plRpRate(double r) { if (rp.team) { tmRpRate(r); return; } if (plrp?.voices != null) foreach (var v in plrp.voices) v?.playbackRate.setValueAtTime(r, actx.currentTime); }
	// part i from its second T (T counts from the part's start)
	static void plRpGo(int i, double? T)
	{
		if (rp.team) { tmRpGo(i, T); return; }
		rp.seg = i; var s = rp.doc.segs[i]; double tr = Math.Max(0, T ?? 0), at = plrp.t0[i] + tr; var L = plrp.L;
		pl = new PlatRun { L = L, m = L.map.Select(r => r.ToCharArray()).ToArray() }; pl.h = pl.m.Length; pl.w = pl.m[0].Length; pl.stemOn = (bool[])L.on.Clone();
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++) if (pl.m[y][x] == 'S') { pl.cx = x * PT + PT / 2; pl.cy = (y + 1) * PT - P_HALF; }
		// what had happened before this second: pieces taken, saves reached
		plrp.evDone = 0;
		while (plrp.evDone < plrp.ev.Count && plrp.ev[plrp.evDone][0] <= at) plRpEv(plrp.ev[plrp.evDone++], false);
		player = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		particles = new List<Particle>(); hazards = new List<Hz>(); shake = flash = 0; cpMsg = 0; pl.msgT = 0;
		pl.t = at; songT = tr; plRpApply(tr, 0, false);
		plRpMusic(at); rp.aT = tr; rp.aCT = actx.currentTime; state = "replay";
	}
	static void plRpEv(double[] e, bool fx)
	{
		int tx = (int)e[2], ty = (int)e[3];
		if (e[1] == 0)
		{
			string k = tx + "," + ty; if (pl.got.Contains(k)) return; pl.got.Add(k); char c = ty < pl.h && tx < pl.w ? pl.m[ty][tx] : '1';
			if (pl.L.pieces != null && pl.L.pieces.TryGetValue(c.ToString(), out int s) && s < pl.stemOn.Length && !pl.stemOn[s])
			{
				pl.stemOn[s] = true; plrp.voices?[s]?.gain.setTargetAtTime(1, actx.currentTime, 0.25);
				if (fx) { pl.msg = (pl.L.stemNames != null && s < pl.L.stemNames.Length ? pl.L.stemNames[s] : "قطعة") + " انضاف للأغنية"; pl.msgT = 2.2; }
			}
			if (fx)
			{
				double x = tx * PT + PT / 2, y = ty * PT + PT / 2; burst(x, y, GOLD, 30, 380); flash = 0.15; shake = 6;
				sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.12); sTone(tt + 0.07, 1047, 0.12, "triangle", 0.12); sTone(tt + 0.14, 1568, 0.25, "triangle", 0.1); });
				if (plOpen()) { pl.msg = "الأغنية كملت! روح للبوابة"; pl.msgT = 2.6; }
			}
		}
		else { pl.cx = tx * PT + PT / 2; pl.cy = (ty + 1) * PT - P_HALF; pl.hasCp = true; if (fx) { cpMsg = 1.8; sfx(tt => { sTone(tt, 659, 0.25, "square", 0.06); sTone(tt + 0.08, 880, 0.3, "square", 0.06); }); } }
	}
	static (double x, double y, int fl) plRpAt(RpSeg s, double tr)
	{
		double fi = clamp(tr * rp.doc.hz, 0, Math.Max(0, s.n - 1.0001)); int i = (int)Math.Floor(fi); double k = fi - i; int o = i * 3, o2 = Math.Min(i + 1, s.n - 1) * 3; var b = s.b;
		return ((b[o] + (b[o2] - b[o]) * k) / 50, (b[o + 1] + (b[o2 + 1] - b[o + 1]) * k) / 50, b[o + 2]);
	}
	// the square at second tr of the part (fx false while seeking: no effects)
	static void plRpApply(double tr, double dt, bool fx)
	{
		var s = rp.doc.segs[rp.seg]; var p = player; var (nx, ny, fl) = plRpAt(s, tr); double dx = nx - p.x, dy = ny - p.y, d = hypot(dx, dy);
		bool wd = p.dash > 0; p.dash = (fl & 1) != 0 ? 0.1 : 0; p.fx = (fl & PF_LEFT) != 0 ? -1 : 1;
		if (p.dash > 0 && !wd) { p.dt0 = pl.t; if (d > 0.5) { p.ddx = dx / d; p.ddy = dy / d; } p.sa = JS.atan2(p.ddy, p.ddx); if (fx) burst(p.x, p.y, CYAN, 8, 200); }
		if (dt > 0) { double sp = Math.Min(950, d / Math.Max(dt, 1e-4)), tg = p.dash > 0 ? 1 : Math.Min(1, sp / P_SPEED) * 0.3; if (sp > 1 && d > 0.05) p.sa = JS.atan2(dy, dx); double sk = p.sk ?? 0; p.sk = sk + (tg - sk) * Math.Min(1, dt * (tg > sk ? 30 : 12)); }
		p.x = nx; p.y = ny; int hp = (fl >> 1) & 7;
		if (hp < p.hp && fx) { hitsTotal++; p.hurt = 2.0; shake = 16; flash = 0.3; burst(p.x, p.y, CYAN, 22, 380); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); }); }
		p.hp = hp; p.hurt -= dt; pl.atGate = (fl & PF_HIDE) != 0; pl.down = (fl & PF_DOWN) != 0;
		if (dt > 0) skinTrailAdd(p);
		pl.t = plrp.t0[rp.seg] + tr; songT = tr;
		while (plrp.evDone < plrp.ev.Count && plrp.ev[plrp.evDone][0] <= pl.t) plRpEv(plrp.ev[plrp.evDone++], fx);
		pl.msgT -= dt;
	}
	public static void plRpUpdate(double dt)
	{
		if (rp.team) { tmRpUpdate(dt); return; }
		if (rp.paused) return;
		double tr = rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; var s = rp.doc.segs[rp.seg];
		if (tr * rp.doc.hz >= s.n - 1)
		{
			plRpApply((s.n - 1) / (double)rp.doc.hz, dt * rp.rate, true); plRpStopMusic();
			if (rp.seg < rp.doc.segs.Count - 1 && s.c) { plRpGo(rp.seg + 1, null); return; }   // cut: on to the next kept part, no death
			if (rp.seg < rp.doc.segs.Count - 1)
			{
				int lf = s.b[(s.n - 1) * 3 + 2]; rp.gapDead = ((lf >> 1) & 7) == 0; deaths++;   // a new part = a death (or "back to the last save", which counts one)
				if (rp.gapDead) { burst(player.x, player.y, CYAN, 50, 520); pl.atGate = true; }
				state = "rpGap"; rp.gapAt = performance.now();
			}
			else { state = "rpEnd"; rp.endAt = performance.now(); }
			return;
		}
		plRpApply(tr, dt * rp.rate, true);
	}
	// the other square (online), as this PC saw it
	static void drawPlRpGhost()
	{
		if (plrp?.gs == null || rp.seg >= plrp.gs.Count || rp.doc == null) return; var s = plrp.gs[rp.seg]; if (s.n < 1) return;
		var (x, y, fl) = plRpAt(s, songT); if ((fl & PF_HIDE) != 0) return;
		if ((fl & PF_DOWN) != 0) { drawDown(x, y, performance.now(), true); return; }
		var gp = new Player { x = x, y = y, r = 5, dash = (fl & 1) != 0 ? 0.1 : 0, fx = (fl & PF_LEFT) != 0 ? -1 : 1, hp = 1, ddx = 1 };
		tinted((plrp.gsk ?? "") == (rp.doc.sk ?? "") ? SEAT_HUE[1] : -1, () => drawPlayer(gp, plrp.gsk ?? "", 0.55));   // the same look: the other one in gold
		g.globalAlpha = 0.7; txt(plrp.gname ?? "", x, y - 30, 18, "#fff", 700); g.globalAlpha = 1;
	}
	// draw(): the platformer's replay instead of the main mode's level
	public static bool plRpDraw(double now)
	{
		if (rp.team) return tmRpDraw(now);
		if (!rp.plat || pl == null || player == null || !(state is "replay" or "rpGap" or "rpEnd")) return false;
		drawPlat(now); drawPlRpGhost(); return true;   // the bar: draw() as for the main mode
	}
	public static void plRpExit() { if (rp.team) { tmRpExit(); return; } plRpStopMusic(); pl = null; plrp = null; }
}
