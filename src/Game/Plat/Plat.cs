using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// The platformer mode (part 2 of the big update). Same square, same fixed steps (STEP) as the main mode.
// Rules (his words 10-19): one jump only, one dash that comes back only when the square touches the ground.
// States: modes (pick the main mode or the platformer), pltList, pltPlay, pltPause, pltDead, pltWin.
public static partial class G
{
	public const double PT = 40;                                   // tile size
	const double P_SPEED = 370, P_ACC = 5200, P_AIR = 3600, P_GRAV = 2600, P_JUMP = 880, P_CUT = 0.45, P_FALL = 1100;
	public const int P_HP = 1;   // one heart: every hit is a death (his words 19:55 10-19, was 3)
	const double P_COYOTE = 0.08, P_BUFFER = 0.1, P_DASH = 950, P_DASH_T = 0.18, P_HALF = 9;
	public static int modeSel = 0, plSel = 0;

	public class PlatRun
	{
		public PlatLevel L; public char[][] m; public int w, h;
		public double vx, vy, coyote, jbuf; public bool ground, canDash = true, jumpHeld;
		public double cx, cy;                       // checkpoint (where the square comes back)
		public bool hasCp;                          // a checkpoint was reached (pause "back to the last save" counts a death only then)
		public HashSet<string> got = new(); public bool[] stemOn;
		public Voice[] voices; public double startWhen;
		public double t; public long n;             // music time (seconds since the loop started) on the fixed steps
		public double inX; public bool dashQ, jumpQ;
		public string msg; public double msgT;
		public double winAt, deadAt, introAt; public int introSfx;
		public long beatN = -1;                    // the last beat the tick played on
		public List<(int k, double at)> taken = new();   // the pieces taken, in order (they pass behind the map)
		public bool down, atGate, gateBlock;        // co-op online: out of lives (waiting for the other one), in the gate, just came out of it
	}
	public static PlatRun pl;

	static bool solid(int tx, int ty) => tx < 0 || ty < 0 || tx >= pl.w || ty >= pl.h || pl.m[ty][tx] == '#';
	static double plBeat() => pl.t / pl.L.spb;

	// ---------- start / music ----------
	// intro: entering from the level list shows the level's card and draws the map in first (his words 15:13 10-19: no skip, only on entry)
	public static void plStart(PlatLevel L, bool intro = false)
	{
		plStopMusic();   // a run before this one (a versus rematch started a second copy of the song over the first)
		pl = new PlatRun { L = L, m = L.map.Select(r => r.ToCharArray()).ToArray() }; pl.h = pl.m.Length; pl.w = pl.m[0].Length;
		pl.stemOn = (bool[])L.on.Clone();
		hitsTotal = 0; deaths = 0;
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++) if (pl.m[y][x] == 'S') { pl.cx = x * PT + PT / 2; pl.cy = (y + 1) * PT - P_HALF; }
		player = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 1.0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		particles = new List<Particle>(); hazards = new List<Hz>(); plRecStart();
		stopMenu(); ensureAudio();
		if (intro) { state = "pltIntro"; pl.introAt = performance.now(); pl.t = -0.12; songT = pl.t; return; }
		plGo();
	}
	public const double PL_INTRO = 3000;   // ms
	static void plGo()
	{
		var L = pl.L;
		double when = actx.currentTime + 0.12; pl.startWhen = when; pl.voices = new Voice[L.stems.Length];
		for (int i = 0; i < L.stems.Length; i++)
		{
			if (L.bufs[i] == null || noMus) continue;
			var v = new Voice { song = L.bufs[i], loop = true, loopStart = L.loop[0], loopEnd = L.loop[1] };
			v.gain.value = pl.stemOn[i] ? 1 : 0; v.start(when, L.loop[0]); pl.voices[i] = AudioCtx.Add(v);
		}
		pl.t = -0.12; pl.n = 0; songT = pl.t;
		state = "pltPlay";
	}
	static void plStopMusic() { if (pl?.voices == null) return; foreach (var v in pl.voices) if (v != null) { v.gain.setTargetAtTime(0, actx.currentTime, 0.06); v.stop(actx.currentTime + 0.5); } pl.voices = null; }
	static void plExit() { plStopMusic(); player = null; particles = new List<Particle>(); state = "pltList"; actx.resume(); }
	static void plRespawn()
	{
		pl.down = pl.atGate = false;
		player = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 1.5, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		pl.vx = pl.vy = 0; pl.canDash = true; pl.dashQ = pl.jumpQ = false; state = "pltPlay";
		plRecSeg();   // v46: the replay's next part
	}

	// ---------- one fixed step ----------
	static void plStep(double dt, double mx, double my, bool now)
	{
		var p = player; pl.t += dt; songT = pl.t;
		if (pl.down || pl.atGate) { p.inv -= dt; p.hurt -= dt; return; }   // co-op: waiting for the other one (to bring it back / in the gate)
		// a soft tick on the beat until the first piece of the song is taken
		{ long bi = (long)Math.Floor(plBeat()); if (bi != pl.beatN) { pl.beatN = bi; if (bi >= 0 && !pl.stemOn.Any(o => o)) { bool down = bi % 4 == 0; sfx(tt => sTone(tt, down ? 1500 : 1100, 0.035, "sine", down ? 0.07 : 0.045)); } } }
		if (Math.Abs(mx) < 0.25) mx = 0;
		bool dq = now && pl.dashQ, jq = now && pl.jumpQ; if (now) { pl.dashQ = false; pl.jumpQ = false; }
		if (jq) pl.jbuf = P_BUFFER;
		// dash: any of 8 directions (no direction = where it faces); once, back only on the ground
		if (dq && pl.canDash && p.dash <= 0)
		{
			double m = hypot(mx, my), dx = m > 0.3 ? mx / m : p.fx, dy = m > 0.3 ? my / m : 0;
			p.ddx = dx; p.ddy = dy; p.dash = P_DASH_T; p.inv = Math.Max(p.inv, 0.28); pl.canDash = false; p.dt0 = pl.t; p.sa = JS.atan2(dy, dx);
			burst(p.x, p.y, CYAN, 8, 200); sfx(tt => sNoise(tt, 0.12, "bandpass", 3200, 0.18, 1));
		}
		if (mx != 0) p.fx = Math.Sign(mx);
		if (p.dash > 0)
		{
			pl.vx = p.ddx * P_DASH; pl.vy = p.ddy * P_DASH; p.dash -= dt;
			if (p.dash <= 0) { pl.vx = p.ddx * P_SPEED; pl.vy = p.ddy < 0 ? p.ddy * P_SPEED * 0.6 : 0; }
		}
		else
		{
			double target = mx * P_SPEED, acc = pl.ground ? P_ACC : P_AIR;
			pl.vx = pl.vx < target ? Math.Min(target, pl.vx + acc * dt) : Math.Max(target, pl.vx - acc * dt);
			// one jump: from the ground (or just after leaving it)
			if (pl.jbuf > 0 && pl.coyote > 0) { pl.vy = -P_JUMP; pl.jbuf = 0; pl.coyote = 0; pl.ground = false; sfx(tt => sTone(tt, 520, 0.08, "square", 0.04)); }
			if (!pl.jumpHeld && pl.vy < -300) pl.vy *= P_CUT;   // let go early = a lower jump
			pl.vy = Math.Min(P_FALL, pl.vy + P_GRAV * dt);
		}
		pl.jbuf -= dt; pl.coyote -= dt;
		// move and stop at walls, one axis at a time
		p.x += pl.vx * dt;
		{
			int ty0 = (int)Math.Floor((p.y - P_HALF + 0.01) / PT), ty1 = (int)Math.Floor((p.y + P_HALF - 0.01) / PT);
			if (pl.vx > 0) { int tx = (int)Math.Floor((p.x + P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (solid(tx, ty)) { p.x = tx * PT - P_HALF; pl.vx = 0; break; } }
			else if (pl.vx < 0) { int tx = (int)Math.Floor((p.x - P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (solid(tx, ty)) { p.x = (tx + 1) * PT + P_HALF; pl.vx = 0; break; } }
		}
		bool wasGround = pl.ground; pl.ground = false;
		p.y += pl.vy * dt;
		{
			int tx0 = (int)Math.Floor((p.x - P_HALF + 0.01) / PT), tx1 = (int)Math.Floor((p.x + P_HALF - 0.01) / PT);
			if (pl.vy >= 0) { int ty = (int)Math.Floor((p.y + P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (solid(tx, ty)) { p.y = ty * PT - P_HALF; pl.vy = 0; pl.ground = true; break; } }
			else { int ty = (int)Math.Floor((p.y - P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (solid(tx, ty)) { p.y = (ty + 1) * PT + P_HALF; pl.vy = 0; if (p.dash > 0) p.dash = 0; break; } }
		}
		if (pl.ground) { pl.coyote = P_COYOTE; if (p.dash <= 0) pl.canDash = true; if (!wasGround && p.dash <= 0) burst(p.x, p.y + P_HALF, "rgba(255,255,255,0.6)", 4, 90); }
		// the square leans with its movement like in the main mode
		{
			double sp = hypot(pl.vx, pl.vy), tg = p.dash > 0 ? 1 : Math.Min(1, sp / P_SPEED) * 0.3; if (sp > 1) p.sa = JS.atan2(pl.vy, pl.vx);
			double sk = p.sk ?? 0; p.sk = sk + (tg - sk) * Math.Min(1, dt * (tg > sk ? 30 : 12));
		}
		p.inv -= dt; p.hurt -= dt;
		skinTrailAdd(p);
		plTiles(p);
		if (state == "pltPlay" && p.inv <= 0 && plHit(p)) plDamage();
	}

	// pieces, checkpoints and the goal under the square
	static void plTiles(Player p)
	{
		int tx0 = (int)Math.Floor((p.x - P_HALF) / PT), tx1 = (int)Math.Floor((p.x + P_HALF) / PT), ty0 = (int)Math.Floor((p.y - P_HALF) / PT), ty1 = (int)Math.Floor((p.y + P_HALF) / PT);
		bool inGate = false;
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= pl.w || ty >= pl.h) continue; char c = pl.m[ty][tx]; string k = tx + "," + ty;
			if (c >= '1' && c <= '9' && !pl.got.Contains(k)) plTake(k, c, tx, ty);
			else if (c == 'C' && (pl.cx != tx * PT + PT / 2 || pl.cy != (ty + 1) * PT - P_HALF))
			{
				pl.cx = tx * PT + PT / 2; pl.cy = (ty + 1) * PT - P_HALF; pl.hasCp = true; cpMsg = 1.8; plRecEv(1, tx, ty);
				if (online && net.coop) netTell(new System.Text.Json.Nodes.JsonObject { ["k"] = "cp", ["x"] = tx, ["y"] = ty });
				sfx(tt => { sTone(tt, 659, 0.25, "square", 0.06); sTone(tt + 0.08, 880, 0.3, "square", 0.06); });
			}
			else if (c == 'G' && plOpen() && state == "pltPlay") { if (online && net.coop) inGate = true; else if (online) netWin(); else plWin(); }
		}
		if (online && net.coop && state == "pltPlay") { if (inGate && !pl.atGate && !pl.gateBlock) coopGate(true); if (!inGate) pl.gateBlock = false; }
	}
	static int plPieces() { int n = 0; foreach (var r in pl.m) foreach (var c in r) if (c >= '1' && c <= '9') n++; return n; }
	// no Easy in the platformer: its level list and levels use the normal colours, the Easy colours come back on the mode screen
	public static void plThemeSync()
	{
		bool plt = state.StartsWith("plt"); if (!plt && state != "modes") return;
		bool e = easy && !plt; if (RED != THEMES[e ? "easy" : "normal"].c) applyTheme(e);
		if (menuNormal != (easy && plt)) { menuNormal = easy && plt; applyMenu(); }
	}
	static bool plOpen() => pl.got.Count >= plPieces();
	static void plTake(string k, char c, int tx, int ty, bool fromOther = false)
	{
		pl.got.Add(k); plRecEv(0, tx, ty);
		if (!fromOther && online && net.coop) netTell(new System.Text.Json.Nodes.JsonObject { ["k"] = "pc", ["x"] = tx, ["y"] = ty }); double x = tx * PT + PT / 2, y = ty * PT + PT / 2;
		burst(x, y, GOLD, 30, 380); flash = 0.15; shake = 6; rumble(120, 0.4, 0.5);
		sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.12); sTone(tt + 0.07, 1047, 0.12, "triangle", 0.12); sTone(tt + 0.14, 1568, 0.25, "triangle", 0.1); });
		if (pl.L.pieces != null && pl.L.pieces.TryGetValue(c.ToString(), out int s) && s < pl.stemOn.Length && !pl.stemOn[s])
		{
			pl.stemOn[s] = true; pl.voices?[s]?.gain.setTargetAtTime(1, actx.currentTime, 0.25); pl.taken.Add((s, pl.t));
			pl.msg = (pl.L.stemNames != null && s < pl.L.stemNames.Length ? pl.L.stemNames[s] : "قطعة") + " انضاف للأغنية"; pl.msgT = 2.2;
		}
		if (plOpen()) { pl.msg = "الأغنية كملت! روح للبوابة"; pl.msgT = 2.6; }
	}

	// ---------- pink things ----------
	static double sawPos(PlatSaw s, out double y)
	{
		double b = plBeat() / s.beats + s.ph, f = b - Math.Floor(b), k = f < 0.5 ? f * 2 : 2 - f * 2; k = k * k * (3 - 2 * k);
		y = (s.y0 + (s.y1 - s.y0) * k) * PT + PT / 2; return (s.x0 + (s.x1 - s.x0) * k) * PT + PT / 2;
	}
	// a beat block is solid pink on two beats of every four ('B' beats 0-1, 'b' beats 2-3)
	static bool blockOn(char c) { double b = plBeat(); int q = (int)Math.Floor(((b % 4) + 4) % 4); return c == 'B' ? q < 2 : q >= 2; }
	static bool plHit(Player p)
	{
		const double r = 5;   // the same small hitbox as the main mode
		int tx0 = (int)Math.Floor((p.x - r) / PT), tx1 = (int)Math.Floor((p.x + r) / PT), ty0 = (int)Math.Floor((p.y - r) / PT), ty1 = (int)Math.Floor((p.y + r) / PT);
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= pl.w || ty >= pl.h) continue; char c = pl.m[ty][tx];
			double lx = p.x - tx * PT, ly = p.y - ty * PT;
			if (c == '^' && ly + r > PT * 0.45) return true;
			if (c == 'v' && ly - r < PT * 0.55) return true;
			if ((c == 'B' || c == 'b') && blockOn(c)) return true;
		}
		foreach (var s in pl.L.saws) { double sx = sawPos(s, out double sy); if (hypot(p.x - sx, p.y - sy) < s.r + r) return true; }
		return false;
	}
	static void plDamage()
	{
		var p = player;
		p.hp--; hitsTotal++; p.inv = 2.0; p.hurt = 2.0; shake = 16; flash = 0.3; pl.vy = Math.Min(pl.vy, -520);
		burst(p.x, p.y, CYAN, 22, 380); rumble(220, 0.8, 0.6);
		sfx(t => { sTone(t, 110, 0.3, "square", 0.18); sNoise(t, 0.25, "lowpass", 900, 0.3); });
		if (p.hp <= 0 && online && net.coop) { deaths++; burst(p.x, p.y, CYAN, 50, 520); rumble(500, 1, 1); coopDown(); return; }
		if (p.hp <= 0) { state = "pltDead"; deaths++; pl.deadAt = performance.now(); burst(p.x, p.y, CYAN, 50, 520); rumble(500, 1, 1); }
	}
	static void plWin()
	{
		// one heart, so a hit is a death: S none, A one, B two, C three or more (agreed 19:59 10-19)
		lastRank = deaths >= 3 ? "C" : deaths == 2 ? "B" : deaths == 1 ? "A" : "S"; lastEz = false;
		// the letter counts like the main mode's: points, XP and the online board (a better letter only)
		var lv = pl.L.lv; double p0 = ptsOf(false); xpMark();
		plSaveBest(lv, lastRank, pl.t);
		lastGain = ptsOf(false) - p0; awardXp();
		state = "pltWin"; pl.winAt = performance.now(); winSlam = pl.winAt + 300; winHit = false;
		burst(player.x, player.y, CYAN, 40, 450);
	}

	// the finish time (his ask 20:36 10-22): the board goes by the letter first, then the time. The time kept is the fastest one with
	// the best letter: a better letter takes its own time, the same letter faster takes the new time, a worse letter changes nothing.
	public static double lastT = -1; public static bool lastNewT;   // this finish's time, and whether it is a new best time
	public static double tRound(double t) => Math.Floor(Math.Max(0, t) * 100 + 1e-6) / 100;   // to the hundredth, like it shows
	public static string tFmt(double t) { long cs = (long)Math.Round(tRound(t) * 100); long m = cs / 6000, s = cs / 100 % 60, c = cs % 100; return $"{ar(m)}:{(s < 10 ? "٠" : "")}{ar(s)}٫{(c < 10 ? "٠" : "")}{ar(c)}"; }   // the Arabic decimal mark: a dot looks like the Arabic zero
	public static void setBestT(Level lv, double t) { lv.bestT = t; if (t >= 0) localStorage.setItem("mg_bestt_" + lv.name, t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)); else localStorage.removeItem("mg_bestt_" + lv.name); }
	static void plSaveBest(Level lv, string rank, double t)
	{
		t = tRound(t); lastT = t; lastNewT = false;
		int rb = lv.best != null ? Array.IndexOf(RANKS, lv.best) : 99, r = Array.IndexOf(RANKS, rank);
		if (r < rb) { lv.best = rank; localStorage.setItem("mg_best_" + lv.name, rank); setBestT(lv, t); lastNewT = true; markDirty(); }
		else if (r == rb && (lv.bestT < 0 || t < lv.bestT)) { setBestT(lv, t); lastNewT = true; markDirty(); }
		else if (acc != null && !acc.dirty) sync();
	}

	// ---------- step ----------
	public static bool stepPlat(Inp inp, double now, double dt)
	{
		if (stepNet(inp, now, dt)) return true;
		if (stepBoss(inp, now, dt)) return true;
		if (state == "modes")
		{
			int nm = BOSSLV.Count > 0 ? 3 : 2; if (inp.left) modeSel = Math.Min(nm - 1, modeSel + 1); if (inp.right) modeSel = Math.Max(0, modeSel - 1);   // right to left, like the lists
			if (inp.confirm) { if (modeSel == 0) state = "levels"; else if (modeSel == 2) { state = "bossList"; bossLv = Math.Min(bossLv, Math.Max(0, BOSSLV.Count - 1)); } else { state = "pltList"; plSel = Math.Min(plSel, Math.Max(0, PLATS.Count - 1)); } }
			else if (inp.back || inp.pz) state = "main";
		}
		else if (state == "pltList")
		{
			int n = PLATS.Count;
			if (inp.left && plSel < n - 1) plSel++; if (inp.right && plSel > 0) plSel--;
			if (inp.confirm && n > 0 && PLATS[plSel].coop) { if (acc == null) netNote("سجّل دخول من الحساب بالقائمة عشان تلعب أونلاين"); else { state = "pltOn"; onSel = 0; } }   // co-op: two players only
			else if (inp.confirm && n > 0) plStart(PLATS[plSel], true);
			else if (inp.restart && n > 0 && !PLATS[plSel].coop && PLATS[plSel].lv.best == null) netNote(VS_LOCK);   // versus opens after finishing the level alone (his words 20:40 10-22)
			else if (inp.restart && n > 0) { if (acc == null) netNote("سجّل دخول من الحساب بالقائمة عشان تلعب أونلاين"); else { state = "pltOn"; onSel = 0; } }
			else if (inp.back || inp.pz) state = "modes";
		}
		else if (state == "pltIntro")
		{
			double s = now - pl.introAt;
			if (pl.introSfx == 0 && s > 1300) { pl.introSfx = 1; sfx(tt => sNoise(tt, 1.0, "bandpass", 1400, 0.08, 0.7)); }
			if (pl.introSfx == 1 && s > 2850) { pl.introSfx = 2; burst(pl.cx, pl.cy + P_HALF, CYAN, 18, 260); shake = 5; rumble(90, 0.4, 0.3); sfx(tt => { sTone(tt, 180, 0.12, "square", 0.08); sNoise(tt, 0.1, "lowpass", 700, 0.15); }); }
			if (s >= PL_INTRO) plGo();
		}
		else if (state == "pltPlay")
		{
			if (inp.pz) { state = "pltPause"; pauseSel = 0; hostAsk = 0; if (!online) actx.suspend(); return true; }   // online: the other player keeps going
			if (inp.jump) pl.jumpQ = true; if (inp.pdash) pl.dashQ = true; pl.jumpHeld = inp.jumpHeld;
			double target = actx.currentTime - pl.startWhen;
			if (target - pl.t > SIM_MAX) { pl.n += (long)Math.Floor((target - SIM_MAX - pl.t) / STEP); pl.t = -0.12 + pl.n * STEP; }
			int k = 0;
			while (state == "pltPlay" && -0.12 + (pl.n + 1) * STEP <= target + 1e-9)
			{
				pl.n++; k++; bool nw = -0.12 + (pl.n + 1) * STEP > target + 1e-9;
				pl.t = -0.12 + (pl.n - 1) * STEP; plStep(STEP, nw ? inp.x : pl.inX, nw ? inp.y : 0, nw); plRecSample();
			}
			if (k > 0) pl.inX = inp.x;
			pl.msgT -= dt;
		}
		else if (state == "pltPause" && online)
		{
			if (hostAskStep(inp, a => { if (state == "pltPause") vsRestartAll(); })) return true;
			var PP = plPauseOn(); int pn = PP.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
			string it = PP[Math.Clamp(pauseSel, 0, pn - 1)];
			if (inp.pz || inp.back || (inp.confirm && pauseSel == 0)) state = "pltPlay";
			else if (inp.confirm && pauseSel == 1) { if (!pl.down) deaths++; plRespawn(); }   // a death (his words 10:27 10-21); co-op down already counted it
			else if (inp.confirm && it == RE_ALL) hostAsk = 2;
			else if (inp.confirm && it == RE_START) vsSelfRestart();
			else if (inp.confirm && it == "الإعدادات") { state = "pltSet"; setSel = 0; }
			else if (inp.confirm && it == "احفظ الريبلاي") plRpSave("mid", null);   // v46
			else if (inp.confirm) netEnd(null, true);
		}
		else if (state == "pltPause")
		{
			int pn = PL_PAUSE46.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
			if (inp.pz || inp.back || (inp.confirm && pauseSel == 0)) { state = "pltPlay"; actx.resume(); }
			else if (inp.confirm && pauseSel == 3) { state = "pltSet"; setSel = 0; }
			else if (inp.confirm && pauseSel == 4) plRpSave("mid", null);   // v46: the replay on the PC
			else if (inp.confirm && pauseSel == 5) openMyRp("pltPause");
			else if (inp.confirm)
			{
				actx.resume();
				if (pauseSel == 1 && pl.hasCp) { deaths++; plRespawn(); } else if (pauseSel == 1 || pauseSel == 2) { plStopMusic(); plStart(pl.L); } else plExit();
			}
		}
		else if (state == "pltDead" && online)
		{
			plRpKey(inp, pl.deadAt, "dead", null);
			if (now - pl.deadAt > 700 && inp.confirm) plRespawn();
			else if (inp.back) netEnd(null, true);
		}
		else if (state == "pltDead")
		{
			plRpKey(inp, pl.deadAt, "dead", null);
			if (now - pl.deadAt > 700 && inp.confirm) plRespawn();
			else if (now - pl.deadAt > 700 && inp.restart) { plStopMusic(); plStart(pl.L); }
			else if (inp.back) plExit();
		}
		else if (state == "pltWin")
		{
			plRpKey(inp, pl.winAt, "win", lastRank);
			void again() { plStopMusic(); plStart(pl.L); }
			if (now - pl.winAt > 800 && inp.confirm) { if (!rateAsk(pl.L.lv, again)) again(); }
			else if (inp.back) { if (!rateAsk(pl.L.lv, plExit)) plExit(); }
		}
		else return false;
		return true;
	}
	static readonly string[] PL_PAUSE = { "أكمل اللعب", "إعادة من آخر حفظ", "إعادة من البداية", "الإعدادات", "الخروج من المرحلة" };
	static readonly string[] PL_PAUSE46 = { "أكمل اللعب", "إعادة من آخر حفظ", "إعادة من البداية", "الإعدادات", "احفظ الريبلاي", "ريبلاياتي", "الخروج من المرحلة" };   // v46: the replay items
}
