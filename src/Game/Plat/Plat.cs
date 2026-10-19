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
	public const int P_HP = 3;   // lives (his words 14:27 10-19)
	const double P_COYOTE = 0.08, P_BUFFER = 0.1, P_DASH = 950, P_DASH_T = 0.18, P_HALF = 9;
	public static int modeSel = 0, plSel = 0;

	public class PlatRun
	{
		public PlatLevel L; public char[][] m; public int w, h;
		public double vx, vy, coyote, jbuf; public bool ground, canDash = true, jumpHeld;
		public double cx, cy;                       // checkpoint (where the square comes back)
		public HashSet<string> got = new(); public bool[] stemOn;
		public Voice[] voices; public double startWhen;
		public double t; public long n;             // music time (seconds since the loop started) on the fixed steps
		public double inX; public bool dashQ, jumpQ;
		public string msg; public double msgT;
		public double winAt, deadAt;
		public long beatN = -1;                    // the last beat the tick played on
		public List<(int k, double at)> taken = new();   // the pieces taken, in order (they pass behind the map)
	}
	public static PlatRun pl;

	static bool solid(int tx, int ty) => tx < 0 || ty < 0 || tx >= pl.w || ty >= pl.h || pl.m[ty][tx] == '#';
	static double plBeat() => pl.t / pl.L.spb;

	// ---------- start / music ----------
	public static void plStart(PlatLevel L)
	{
		pl = new PlatRun { L = L, m = L.map.Select(r => r.ToCharArray()).ToArray() }; pl.h = pl.m.Length; pl.w = pl.m[0].Length;
		pl.stemOn = (bool[])L.on.Clone();
		hitsTotal = 0; deaths = 0;
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++) if (pl.m[y][x] == 'S') { pl.cx = x * PT + PT / 2; pl.cy = (y + 1) * PT - P_HALF; }
		player = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 1.0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		particles = new List<Particle>(); hazards = new List<Hz>();
		stopMenu(); ensureAudio();
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
		player = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 1.5, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		pl.vx = pl.vy = 0; pl.canDash = true; pl.dashQ = pl.jumpQ = false; state = "pltPlay";
	}

	// ---------- one fixed step ----------
	static void plStep(double dt, double mx, double my, bool now)
	{
		var p = player; pl.t += dt; songT = pl.t;
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
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= pl.w || ty >= pl.h) continue; char c = pl.m[ty][tx]; string k = tx + "," + ty;
			if (c >= '1' && c <= '9' && !pl.got.Contains(k)) plTake(k, c, tx, ty);
			else if (c == 'C' && (pl.cx != tx * PT + PT / 2 || pl.cy != (ty + 1) * PT - P_HALF))
			{
				pl.cx = tx * PT + PT / 2; pl.cy = (ty + 1) * PT - P_HALF; cpMsg = 1.8;
				sfx(tt => { sTone(tt, 659, 0.25, "square", 0.06); sTone(tt + 0.08, 880, 0.3, "square", 0.06); });
			}
			else if (c == 'G' && plOpen() && state == "pltPlay") plWin();
		}
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
	static void plTake(string k, char c, int tx, int ty)
	{
		pl.got.Add(k); double x = tx * PT + PT / 2, y = ty * PT + PT / 2;
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
		if (p.hp <= 0) { state = "pltDead"; deaths++; pl.deadAt = performance.now(); burst(p.x, p.y, CYAN, 50, 520); rumble(500, 1, 1); }
	}
	static void plWin()
	{
		// S no hit, A one hit, B one death (or two hits), C two deaths or more (his words 14:27 10-19)
		lastRank = deaths >= 2 ? "C" : deaths == 1 || hitsTotal >= 2 ? "B" : hitsTotal == 1 ? "A" : "S"; lastEz = false;
		int rb = pl.L.best != null ? Array.IndexOf(RANKS, pl.L.best) : 99;
		if (Array.IndexOf(RANKS, lastRank) < rb) { pl.L.best = lastRank; localStorage.setItem("mg_plbest_" + pl.L.id, lastRank); }
		state = "pltWin"; pl.winAt = performance.now(); winSlam = pl.winAt + 300; winHit = false; lastGain = 0;
		burst(player.x, player.y, CYAN, 40, 450);
	}

	// ---------- step ----------
	public static bool stepPlat(Inp inp, double now, double dt)
	{
		if (state == "modes")
		{
			if (inp.left || inp.right) modeSel = 1 - modeSel;
			if (inp.confirm) { if (modeSel == 0) state = "levels"; else { state = "pltList"; plSel = Math.Min(plSel, Math.Max(0, PLATS.Count - 1)); } }
			else if (inp.back || inp.pz) state = "main";
		}
		else if (state == "pltList")
		{
			int n = PLATS.Count;
			if (inp.left && plSel < n - 1) plSel++; if (inp.right && plSel > 0) plSel--;
			if (inp.confirm && n > 0) plStart(PLATS[plSel]);
			else if (inp.back || inp.pz) state = "modes";
		}
		else if (state == "pltPlay")
		{
			if (inp.pz) { state = "pltPause"; pauseSel = 0; actx.suspend(); return true; }
			if (inp.jump) pl.jumpQ = true; if (inp.pdash) pl.dashQ = true; pl.jumpHeld = inp.jumpHeld;
			double target = actx.currentTime - pl.startWhen;
			if (target - pl.t > SIM_MAX) { pl.n += (long)Math.Floor((target - SIM_MAX - pl.t) / STEP); pl.t = -0.12 + pl.n * STEP; }
			int k = 0;
			while (state == "pltPlay" && -0.12 + (pl.n + 1) * STEP <= target + 1e-9)
			{
				pl.n++; k++; bool nw = -0.12 + (pl.n + 1) * STEP > target + 1e-9;
				pl.t = -0.12 + (pl.n - 1) * STEP; plStep(STEP, nw ? inp.x : pl.inX, nw ? inp.y : 0, nw);
			}
			if (k > 0) pl.inX = inp.x;
			pl.msgT -= dt;
		}
		else if (state == "pltPause")
		{
			int pn = PL_PAUSE.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
			if (inp.pz || inp.back || (inp.confirm && pauseSel == 0)) { state = "pltPlay"; actx.resume(); }
			else if (inp.confirm && pauseSel == 3) { state = "pltSet"; setSel = 0; }
			else if (inp.confirm)
			{
				actx.resume();
				if (pauseSel == 1) plRespawn(); else if (pauseSel == 2) { plStopMusic(); plStart(pl.L); } else plExit();
			}
		}
		else if (state == "pltDead")
		{
			if (now - pl.deadAt > 700 && inp.confirm) plRespawn();
			else if (now - pl.deadAt > 700 && inp.restart) { plStopMusic(); plStart(pl.L); }
			else if (inp.back) plExit();
		}
		else if (state == "pltWin")
		{
			if (now - pl.winAt > 800 && inp.confirm) { plStopMusic(); plStart(pl.L); }
			else if (inp.back) plExit();
		}
		else return false;
		return true;
	}
	static readonly string[] PL_PAUSE = { "أكمل اللعب", "إعادة من آخر حفظ", "إعادة من البداية", "الإعدادات", "الخروج من المرحلة" };
}
