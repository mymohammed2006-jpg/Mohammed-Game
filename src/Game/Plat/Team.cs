using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using static MG.JS;

namespace MG;

// The co-op level for three (his words 18:44 10-19): pages that each carry a part of the song, ONE checkpoint at each page's
// start, and the three must help each other to get through. Each player is a TBody with the platformer's same moves.
// A page is a 32 x 18 map (like plat_*.json) plus "things", in tiles:
//   sink   a cloud that sinks under weight (a = tiles before it melts, b = px a second, c = beats it stays melted)
//   stone  glows while someone stands on it (of = the cloud it sits on, x = its middle)
//   bridge a cloud there while every stone in `need` glows; door: a cloud wall gone while they glow
//   fall   a cloud that drops `a` beats after you land on it, back after `b` beats
//   scale  two pans on one rope: pan L (x, y) goes down `a` tiles when both pans carry someone, pan R (x1, y1) goes up `c`;
//          while its button (need) is pressed pan L goes up `b` tiles instead
//   hatch  a lid that opens while its scale (of) moves up
//   btn    a button on the floor (y = the floor's row); path: a cloud there while its button (need) is pressed
//   ride   a cloud that flies to x1 only with all three on it, `a` px a second (back `b` px a second when not)
//   wind   gusts in a zone on the beat (a = px a second, every b beats, c beats long, the side changes each gust)
//   hail   ice falling in a zone's columns on every beat (a = px a second, the warning half a beat before)
//   bolt   lightning over a ride (of) on every beat: one tile of the ride, two on the bar's first beat (warning one beat before)
// Page two's things (a = a player: 0 red, 1 cyan, 2 gold):
//   cloud  a plain cloud that stays
//   vis    a cloud everyone can stand on but only player a sees (on their own screen; everyone on the bar's lightning)
//   flip   a cloud that comes and goes each time player a does their move: red jumps, cyan dashes, gold stands still a second
//          (b = 0: there at the start, 1: not there at the start)
//   cwall  a curtain only player a walks through;  cbtn: a button only player a presses (bridge/door/path `need` it)
//   chase  a red lightning wall from the left: starts at x, a tiles every beat, after c beats
// A page with "cont" goes on from the one before it (the same part of the song, no checkpoint): a falling team goes back to
// the first page of the run. Flags: dark (only lights around the players), stack (stand on each other's heads),
// swap (on each bar's first beat lightning swaps two players: red-cyan, cyan-gold, gold-red, warned two beats before),
// tether (no one more than this many px from the others, or the chain breaks and all fall).
// Page three's things (the photo, a clear night):
//   cam    a camera only player a works: standing on it, on the next beat it snaps the other two where they are, and each
//          stands on their own picture (a step in the air for 8 beats); step off and on again for the next snap
//   btn    with a = 1: pressed by a reflection in the mirror (the page's `mirror` = the glass's middle, in px; tile I = glass)
//   door / bridge / path also open while every button in `alt` is pressed
//   mover  a gold frame going from (x, y) to (x1, y1) and back on the beat (b = beats one way, c = its phase in beats;
//          with `need` it only moves while those are pressed)
//   pic    player a's piece of the photo (carried: no dash and a lower jump; a fall sends it home)
//   slot   where player a's piece goes in the big frame; all three in, the photo is whole and the song plays out (`outro`)
public class TThing
{
	public string k { get; set; }
	public string id { get; set; }
	public double x { get; set; }
	public double y { get; set; }
	public double w { get; set; } = 1;
	public double h { get; set; } = 0.4;
	public string[] need { get; set; }
	public string of { get; set; }
	public double a { get; set; }
	public double b { get; set; }
	public double c { get; set; }
	public double x1 { get; set; }
	public double y1 { get; set; }
	public string[] alt { get; set; }
}
public class TeamPage
{
	public string name { get; set; }
	public string[] map { get; set; }
	public List<TThing> things { get; set; } = new();
	public List<PlatSaw> saws { get; set; } = new();
	public double[] loop { get; set; }                 // the part of the song this page plays (seconds, from a beat to a beat)
	public double[] beats { get; set; }                // the song's beats in that part (seconds): the song is sung freely, so its beats are not even
	public Dictionary<string, int> pieces { get; set; }
	public bool[] on { get; set; }                     // the stems playing at its start (else the level's)
	public bool cont { get; set; }
	public bool dark { get; set; }
	public bool stack { get; set; }
	public bool swap { get; set; }
	public double tether { get; set; }
	public int theme { get; set; }                     // 0 the sunset, 1 the red storm, 2 the clear night
	public double mirror { get; set; }
	public double outro { get; set; }                  // the last page: once done, the song plays on from here to its end
}
public class TeamLevel
{
	public string id { get; set; }
	public string name { get; set; }
	public string song { get; set; }
	public string[] stems { get; set; }
	public string[] stemNames { get; set; }
	public bool[] on { get; set; }
	public double spb { get; set; }
	public List<TeamPage> pages { get; set; }
	[System.Text.Json.Serialization.JsonIgnore] public AudioStream[] bufs;
}

public static partial class G
{
	public const int TM_N = 3;                                      // exactly three players (his words)
	public const double TM_REVIVE = 6;                              // seconds to bring a fallen one back by touching it
	public const double TM_REV_HOLD = 1;                            // one heart, so a revive takes a full second of touching (not a free one in passing)
	public const double TM_SWAP = 1.6;                              // the clouds rise over the page, then part on the next one (no page names, his words 20:00 10-19)
	public static readonly string[] TM_COL = { "#ff4d5e", "#2de2e6", "#ffcf5a" };   // each player's own colour
	public class TS                                                  // a thing while playing
	{
		public TThing d; public string k; public double X, Y, W, H, dx, dy, off, timer, s, fy, fv; public bool solid, one, on, melt, gone; public TS of, L, R; public int n;
	}
	public class TBody
	{
		public int i; public double x, y, vx, vy, coyote, jbuf, dash, ddx, ddy, fx = 1, sa, sk, inv, hurt, ex, downAt, dt0 = -9, prevB;
		public double revT, mdx, mdy, still, gx = double.NaN, gy; public bool armed = true; public TBody onB; public TS carry; public int hp = P_HP; public bool ground, canDash = true, jumpHeld, down, gate; public TS onT;
		public List<(double x, double y, double t)> trail = new();
	}
	public struct TIn { public double x, y; public bool jump, jumpHeld, dash; }
	public class THail { public double x, y; public bool dead; }
	public class TeamRun
	{
		public TeamLevel L; public int page; public TeamPage P; public char[][] m; public int w, h;
		public TBody[] b = new TBody[TM_N]; public List<TS> th = new(); public double t; public HashSet<string> got = new();
		public bool[] stemOn; public Voice[] voices; public List<THail> hail = new(); public long beatN = -1;
		public List<(double x, double y, double t, int who)> steps = new();   // footprints on the clouds
		public double[] sx = new double[TM_N], sy = new double[TM_N];
		public string msg; public double msgT, restartAt = -1, doneAt = -1, pageAt; public int restarts, hits; public bool fromClouds; public int coverTheme;
		public List<(int k, double at)> taken = new();
		public int[] flips = new int[TM_N]; public double reveal, chaseX = -999, beat0, swapAt = -9, swapFx = -9; public int swapA = -1, swapB = -1, snapAt = -1; public double snapT = -9, prevBt, wonAt = -1, playT; public bool won;
		public int[] falls = new int[TM_N]; public string[] names = { "الأحمر", "الأزرق", "الأصفر" };   // the whole level's, for the win screen
		// online (TeamNet): gen = how many times a page was built (events from an older one are dropped); the music clock's start and
		// fixed steps; this player's buttons waiting for the next step; each one's XP from this finish (-1: not known yet)
		public int gen; public double startWhen, inX; public long n; public bool jumpQ, dashQ, jumpHeld, saved; public int[] tp = new int[TM_N]; public int[] xpGot = { -1, -1, -1 };
	}
	public static int tmView = -1;                                  // whose screen this is (-1: everyone's, for the tests)
	public static int tmMe = -1;                                    // online: the square this PC moves (-1: all three here, the tests)
	static bool tmMine(TBody b) => tmMe < 0 || b.i == tmMe;
	static bool tmBoss => tmMe <= 0;                                // the room's maker decides restarts, the page's end and the chain
	static readonly int[,] TM_PAIRS = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
	public static (int a, int b) tmSwapPair(long bar) { int k = (int)(((bar % 3) + 3) % 3); return (TM_PAIRS[k, 0], TM_PAIRS[k, 1]); }
	public static TeamRun tm;
	public static double tmBeatPub() => tmBeat();
	// the beat number at the page's music time (following the song's own beats, again each time the part loops)
	static double tmBeat()
	{
		var B = tm.P.beats; if (B == null || B.Length < 2) return tm.t / tm.L.spb;
		double len = B[^1] - B[0]; int n = B.Length - 1; double k = Math.Floor(tm.t / len), tt = tm.t - k * len + B[0];
		int i = Array.BinarySearch(B, tt); if (i < 0) i = ~i - 1; i = Math.Clamp(i, 0, n - 1);
		return k * n + i + (tt - B[i]) / (B[i + 1] - B[i]);
	}

	// particles, shake and flash fade with time (the game loop does this; the test runs call it themselves)
	public static void particlesStep(double dt)
	{
		foreach (var q in particles) { q.x += q.vx * dt; q.y += q.vy * dt; double f = JS.pow(0.04, dt); q.vx *= f; q.vy *= f; q.life -= dt; }
		particles = particles.Where(q => q.life > 0).ToList(); shake = Math.Max(0, shake - dt * 50); flash = Math.Max(0, flash - dt * 2.2);
	}
	public static TeamLevel LoadTeam(string id)
	{
		using var f = Godot.FileAccess.Open($"res://data/team_{id}.json", Godot.FileAccess.ModeFlags.Read);
		var L = JsonSerializer.Deserialize<TeamLevel>(f.GetAsText());
		L.bufs = L.stems.Select(s => LoadSong(s)).ToArray();
		for (int i = 1; i < L.pages.Count; i++) if (L.pages[i].cont) { var p = L.pages[i]; var q = L.pages[i - 1]; p.loop ??= q.loop; p.beats ??= q.beats; p.on ??= q.on; p.theme = p.theme == 0 ? q.theme : p.theme; }
		return L;
	}

	// ---------- a page ----------
	public static void tmStart(TeamLevel L, int page, bool fromClouds = false)
	{
		var old = tm; var P = L.pages[page];
		tm = new TeamRun { L = L, page = page, P = P, fromClouds = fromClouds, coverTheme = old?.P.theme ?? 0 };
		if (old != null && old.L == L) { tm.falls = old.falls; tm.playT = old.playT; tm.names = old.names; tm.gen = old.gen; tm.tp = old.tp; tm.restarts = old.restarts; tm.hits = old.hits; }   // the whole level's, for the win screen
		if (P.cont && old != null && old.L == L)
		{
			tm.stemOn = old.stemOn; tm.voices = old.voices; tm.t = old.t; tm.beatN = old.beatN; tm.restarts = old.restarts; tm.hits = old.hits; tm.startWhen = old.startWhen; tm.n = old.n;
			tmBuild(); return;
		}
		if (old != null) { tmStopVoices(old.voices); old.voices = null; }   // the page before's song stops (it kept looping under the new one)
		tm.stemOn = (bool[])(P.on ?? L.on).Clone();
		tmBuild(); tmMusic();
	}
	static void tmBuild()
	{
		var P = tm.P; tm.m = P.map.Select(r => r.ToCharArray()).ToArray(); tm.h = tm.m.Length; tm.w = tm.m[0].Length; tm.gen++;
		tm.th.Clear(); tm.hail.Clear(); tm.got.Clear(); tm.taken.Clear(); tm.steps.Clear();
		tm.flips = new int[TM_N]; tm.chaseX = -999; tm.beat0 = tm.L != null && tm.P.beats != null ? tmBeat() : 0; tm.swapA = tm.swapB = -1; tm.snapAt = -1; tm.pageAt = 0;
		tmSeen = null;
		if (tmMe >= 0 && net != null) net.hist.Clear();   // where mine was on the page before: the chain must not measure from there (it broke right at the start)
		var byId = new Dictionary<string, TS>();
		foreach (var d in P.things)
		{
			var s = new TS { d = d, k = d.k, X = d.x * PT, Y = d.y * PT, W = d.w * PT, H = d.h * PT };
			s.one = d.k is "sink" or "fall" or "bridge" or "path" or "ride" or "cloud" or "vis" or "flip" or "mover";
			s.solid = d.k is "sink" or "fall" or "door" or "hatch" or "ride" or "cloud" or "vis" or "cwall" or "mover";
			if (d.k == "cam") s.n = 1;
			if (d.k is "pic" or "slot") s.H = PT;
			if (d.k == "flip") { s.solid = d.b == 0; s.s = s.solid ? 1 : 0; }
			if (d.k == "chase") tm.chaseX = d.x * PT;
			if (d.k == "door") s.H = d.h * PT;
			tm.th.Add(s); if (d.id != null) byId[d.id] = s;
			if (d.k == "scale")
			{
				s.L = new TS { d = d, k = "panL", X = d.x * PT, Y = d.y * PT, W = d.w * PT, H = 0.4 * PT, one = true, solid = true, of = s };
				s.R = new TS { d = d, k = "panR", X = d.x1 * PT, Y = d.y1 * PT, W = d.w * PT, H = 0.4 * PT, one = true, solid = true, of = s };
				tm.th.Add(s.L); tm.th.Add(s.R);
			}
		}
		foreach (var s in tm.th) if (s.d.of != null && s.of == null && byId.TryGetValue(s.d.of, out var o)) s.of = o;
		foreach (var s in tm.th) s.fy = s.Y;
		tm.prevBt = tm.L != null && tm.P.beats != null ? tmBeat() : 0;
		foreach (var s in tm.th) if (s.k == "mover") tmMover(s, 0);
		// the start: the S tile, the three side by side
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++) if (tm.m[y][x] == 'S')
			for (int i = 0; i < TM_N; i++) { tm.sx[i] = x * PT + PT / 2 + i * 30; tm.sy[i] = (y + 1) * PT - P_HALF; }
		for (int i = 0; i < TM_N; i++) tm.b[i] = new TBody { i = i, x = tm.sx[i], y = tm.sy[i], inv = 1.0, fx = 1 };
	}
	static void tmMusic()
	{
		tmStopMusic(); var L = tm.L; var lp = tm.P.loop;
		double when = actx.currentTime + 0.12; tm.voices = new Voice[L.stems.Length]; tm.startWhen = when; tm.n = 0;
		for (int i = 0; i < L.stems.Length; i++)
		{
			if (L.bufs[i] == null || noMus) continue;
			var v = new Voice { song = L.bufs[i], loop = true, loopStart = lp[0], loopEnd = lp[1] };
			v.gain.value = tm.stemOn[i] ? 1 : 0; v.start(when, lp[0]); tm.voices[i] = AudioCtx.Add(v);
		}
		tm.t = -0.12;
	}
	public static void tmStopMusic() { if (tm == null) return; tmStopVoices(tm.voices); tm.voices = null; }
	static void tmStopVoices(Voice[] vs) { if (vs != null) foreach (var v in vs) if (v != null) { v.gain.setTargetAtTime(0, actx.currentTime, 0.06); v.stop(actx.currentTime + 0.5); } }
	// back to the page's start, all three (the music keeps going; the page's pieces go back too)
	static void tmRestart()
	{
		tm.restarts++; tm.restartAt = -1;
		int g0 = tm.page; while (g0 > 0 && tm.L.pages[g0].cont) g0--;
		if (g0 != tm.page) { tm.page = g0; tm.P = tm.L.pages[g0]; }
		var want = tm.P.on ?? tm.L.on;
		for (int i = 0; i < tm.stemOn.Length; i++) if (tm.stemOn[i] != want[i]) { tm.stemOn[i] = want[i]; tm.voices?[i]?.gain.setTargetAtTime(want[i] ? 1 : 0, actx.currentTime, 0.2); }
		tmBuild(); tm.msg = "ترجعون لبداية الصفحة"; tm.msgT = 2;
		sfx(tt => { sTone(tt, 330, 0.25, "square", 0.06); sTone(tt + 0.12, 220, 0.35, "square", 0.06); });
	}

	// ---------- where things are ----------
	static bool tmTile(int tx, int ty) => tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h || tm.m[ty][tx] is '#' or 'I';
	static bool tmPressed(string id) { var s = tm.th.FirstOrDefault(q => q.d.id == id && q.k is "stone" or "btn" or "cbtn"); return s != null && s.on; }
	static bool tmAll(string[] ids) => ids != null && ids.All(tmPressed);
	static bool tmNeed(TThing d) => tmAll(d.need) || tmAll(d.alt);
	public static double tmRx(double x) => 2 * tm.P.mirror - x;   // where a reflection is
	static bool tmStands(TBody b, double x, TS s) => !b.down && !b.gate && b.ground && Math.Abs(b.y + P_HALF - s.Y) < 3 && x > s.X - 4 && x < s.X + s.W + 4;
	// who stands where, for what the three drive by standing (the maker online: the others as their newest message says, not the
	// picture drawn 100 ms back, or their presses counted late on every PC and gold met the saw)
	static TBody[] tmSeen;
	static TBody[] tmDec => tmSeen ?? tm.b;
	static int tmOnIt(TS s) { int n = 0; foreach (var b in tmDec) if (!b.down && !b.gate && b.onT == s) n++; return n; }
	static double tmSawPos(PlatSaw s, out double y)
	{
		double b = tmBeat() / s.beats + s.ph, f = b - Math.Floor(b), k = f < 0.5 ? f * 2 : 2 - f * 2; k = k * k * (3 - 2 * k);
		y = (s.y0 + (s.y1 - s.y0) * k) * PT + PT / 2; return (s.x0 + (s.x1 - s.x0) * k) * PT + PT / 2;
	}
	// lightning strikes one tile-wide part of the ride (it moves with the cloud) on beat k, two on the bar's first beat;
	// the warning shows during the beat before
	static readonly int[] TM_BOLT = { 0, 2, 1, 3, 0, 3, 1, 2 };
	public static IEnumerable<int> tmBolts(TS bolt, long k)
	{
		if (bolt.of == null || k < 0) yield break;
		yield return TM_BOLT[k % 8];
		if (k % 4 == 0) yield return TM_BOLT[(k + 3) % 8];
	}
	public static bool tmBoltLive(TS bolt) => bolt.of != null && (tmOnIt(bolt.of) > 0 || bolt.of.X > bolt.of.d.x * PT + 4);
	static IEnumerable<int> tmHailCols(TS z, long k)
	{
		int n = (int)z.d.w; if (k < 0 || n <= 0) yield break;
		yield return (int)z.d.x + (int)((k * 7) % n); yield return (int)z.d.x + (int)((k * 5 + 3) % n);
	}

	// ---------- one fixed step for everyone ----------
	public static void tmStep(double dt, TIn[] ins)
	{
		tm.t += dt; tm.pageAt += dt; if (!tm.won) tm.playT += dt; tm.msgT -= dt; songT = tm.t;
		double bt = tmBeat(), spb = tm.L.spb, dB = bt - tm.prevBt; long bi = (long)Math.Floor(bt); tm.prevBt = bt;
		if (tm.restartAt >= 0) { if (tm.t >= tm.restartAt) tmRestart(); return; }
		if (tm.doneAt >= 0) { if (tm.t - tm.doneAt >= TM_SWAP && tm.page + 1 < tm.L.pages.Count) tmStart(tm.L, tm.page + 1, true); return; }
		// buttons and stones first (from where everyone stood last step)
		tmSeen = tmMe == 0 ? tnSeenAll() : null;
		foreach (var s in tm.th)
		{
			if (s.k is "stone" or "btn" or "cbtn" && tnWorld(s, out int wf, out _, out _)) { s.on = (wf & 1) != 0; continue; }   // online: as the maker sees it
			if (s.k == "stone") { double sx0 = s.d.x * PT; s.on = s.of != null && !s.of.melt && tmDec.Any(b => !b.down && b.onT == s.of && Math.Abs(b.x - sx0) < 26); }
			else if (s.k == "btn" && s.d.a == 1) s.on = tmDec.Any(b => tmStands(b, tmRx(b.x), s));
			else if (s.k == "btn") s.on = tmDec.Any(b => !b.down && b.ground && Math.Abs(b.y + P_HALF - s.Y) < 3 && b.x > s.X - 4 && b.x < s.X + s.W + 4);
			else if (s.k == "cam") { var who = tm.b[(int)s.d.a]; s.on = tmStands(who, who.x, s); if (!s.on) s.n = 1; }
			else if (s.k == "cbtn") { var who = tmDec[(int)s.d.a]; s.on = !who.down && !who.gate && who.ground && Math.Abs(who.y + P_HALF - s.Y) < 3 && who.x > s.X - 4 && who.x < s.X + s.W + 4; }
		}
		// then the things move
		foreach (var s in tm.th)
		{
			double y0 = s.Y, x0 = s.X; var d = s.d;
			bool wk = tnWorld(s, out int wfl, out double wv, out double wt), fresh = wk && net.worldNew;   // online guests: the maker's word
			switch (s.k)
			{
				case "sink":
					if (fresh && s.melt != ((wfl & 4) != 0)) { s.melt = (wfl & 4) != 0; s.solid = !s.melt; s.timer = wt; s.off = s.melt ? wv : 0; s.Y = s.fy + s.off; }
					if (s.melt) { s.timer -= dt; if (s.timer <= 0 && !wk) { s.melt = false; s.solid = true; s.off = 0; s.Y = s.fy; } break; }
					if (wk ? (wfl & 2) != 0 : tmOnIt(s) > 0) s.off += d.b * dt; else s.off = Math.Max(0, s.off - 40 * dt);
					if (fresh) s.off += (wv - s.off) * 0.5;
					if (s.off >= d.a * PT) { s.melt = true; s.solid = false; s.timer = d.c * spb; burst(s.X + s.W / 2, s.Y, "rgba(255,255,255,0.8)", 26, 220); sfx(tt => sNoise(tt, 0.4, "lowpass", 900, 0.12)); }
					s.Y = s.fy + s.off; break;
				case "fall":
					if (fresh && s.gone != ((wfl & 8) != 0)) { s.gone = (wfl & 8) != 0; s.solid = !s.gone; s.timer = s.gone ? wt : 0; s.off = s.gone ? wv : 0; s.fv = 0; s.Y = s.fy + s.off; }
					if (s.gone && wk && s.timer - dt <= 0) { s.fv += 1800 * dt; s.off += s.fv * dt; s.Y = s.fy + s.off; break; }   // back up only on the maker's word
					if (s.gone) { s.timer -= dt; s.fv += 1800 * dt; s.off += s.fv * dt; if (s.timer <= 0) { s.gone = false; s.solid = true; s.off = 0; s.fv = 0; s.timer = 0; s.Y = s.fy; } else s.Y = s.fy + s.off; break; }
					if (s.timer > 0) { s.timer -= dt; if (s.timer <= 0) { s.gone = true; s.solid = false; s.timer = d.b * spb; s.fv = 0; sfx(tt => sNoise(tt, 0.25, "bandpass", 700, 0.1)); } }
					else if (wk ? (wfl & 2) != 0 : tmOnIt(s) > 0) s.timer = d.a * spb;
					break;
				case "bridge": case "path": s.solid = tmNeed(d); s.s = Math.Clamp(s.s + (s.solid ? 6 : -4) * dt, 0, 1); break;
				case "door": s.solid = !tmNeed(d); s.s = Math.Clamp(s.s + (s.solid ? -3 : 5) * dt, 0, 1); break;
				case "scale":
					{
						bool btn = tmAll(d.need), both = wk ? (wfl & 2) != 0 : tmOnIt(s.L) > 0 && tmOnIt(s.R) > 0;
						double s0 = s.s;
						if (btn) s.s = Math.Max(-1, s.s - 0.6 * dt); else if (both) s.s = Math.Min(1, s.s + 0.45 * dt);
						if (fresh) s.s += (wv - s.s) * 0.5;
						if (s.s != s0 && (long)(s.s * 40) != (long)(s0 * 40)) sfx(tt => sTone(tt, 160 + 60 * s.s, 0.05, "triangle", 0.03));
						double ly = d.y * PT + (s.s >= 0 ? s.s * d.a : s.s * d.b) * PT, ry = d.y1 * PT - Math.Max(0, s.s) * d.c * PT;
						s.L.dy = ly - s.L.Y; s.L.Y = ly; s.R.dy = ry - s.R.Y; s.R.Y = ry;
						break;
					}
				case "flip": s.solid = (tm.flips[(int)d.a] + (int)d.b) % 2 == 0; s.s = Math.Clamp(s.s + (s.solid ? 8 : -8) * dt, 0, 1); break;
				case "chase":
					{
						double k = bt - tm.beat0 - d.c; if (k < 0) break;
						double f = k - Math.Floor(k); f = 1 - Math.Pow(1 - Math.Min(1, f * 3), 3);   // a lurch on each beat
						tm.chaseX = d.x * PT + (Math.Floor(k) + f) * d.a * PT; break;
					}
				case "mover": tmMover(s, d.need == null || tmNeed(d) ? dB : 0); break;
				case "snap": if (bt >= s.timer) { s.solid = false; s.gone = true; } break;
				case "hatch": s.solid = !(s.of != null && s.of.s > 0.02); s.s = Math.Clamp(s.s + (s.solid ? -4 : 4) * dt, 0, 1); break;
				case "ride":
					{
						int n = wk ? (wfl >> 4) & 3 : tmOnIt(s); double ex = d.x1 * PT;
						if (n >= TM_N) s.X = Math.Min(ex, s.X + d.a * dt); else if (s.X < ex) s.X = Math.Max(d.x * PT, s.X - d.b * dt);
						if (fresh) s.X += (wv - s.X) * 0.5;
						s.n = n; break;
					}
			}
			if (s.k is not ("panL" or "panR" or "scale")) { s.dx = s.X - x0; s.dy = s.Y - y0; }
		}
		if (net != null) net.worldNew = false;
		// the beat: hail columns and lightning
		if (bi != tm.beatN)
		{
			tm.beatN = bi;
			if (tm.got.Count == 0 && bi >= 0) { bool dn = bi % 4 == 0; sfx(tt => sTone(tt, dn ? 1500 : 1100, 0.035, "sine", dn ? 0.07 : 0.045)); }   // a soft tick until the first piece
			foreach (var s in tm.th)
			{
				if (s.k == "hail") foreach (int c in tmHailCols(s, bi)) tm.hail.Add(new THail { x = c * PT + PT / 2, y = s.Y + 6 });
				if (s.k == "bolt" && tmBoltLive(s) && bi >= 0) { shake = Math.Max(shake, 5); flash = Math.Max(flash, 0.12); sfx(tt => { sNoise(tt, 0.35, "highpass", 2400, 0.16); sTone(tt, 70, 0.3, "sawtooth", 0.08); }); }
			}
			if (tm.P.dark && bi % 4 == 0) { tm.reveal = 1; sfx(tt => { sNoise(tt, 0.6, "lowpass", 500, 0.14); sNoise(tt, 0.15, "highpass", 3000, 0.06); }); }
			if (tm.P.swap && bi % 4 == 2 && bi + 2 >= tm.beat0 + 3) { var (pa, pb) = tmSwapPair((bi + 2) / 4); tm.swapA = pa; tm.swapB = pb; tm.swapAt = (bi + 2); sfx(tt => sNoise(tt, 0.25, "highpass", 4000, 0.05)); }
			if (tm.P.swap && bi % 4 == 0 && tm.swapA >= 0 && bi == (long)tm.swapAt) tmDoSwap();
			foreach (var s in tm.th.ToList()) if (s.k == "cam" && s.on && s.n == 1 && tmMine(tm.b[(int)s.d.a])) tmSnap(s, bi);
		}
		tm.th.RemoveAll(s => s.k == "snap" && s.gone);
		foreach (var q in tm.hail)
		{
			q.y += tm.th.First(s => s.k == "hail").d.a * dt;
			int tx = (int)Math.Floor(q.x / PT), ty = (int)Math.Floor((q.y + 6) / PT);
			if (tmTile(tx, ty) || tm.th.Any(s => s.solid && s.k != "door" && q.x > s.X && q.x < s.X + s.W && q.y + 6 > s.Y && q.y < s.Y + s.H)) { q.dead = true; burst(q.x, q.y, "rgba(200,235,255,0.9)", 6, 140); }
		}
		tm.hail.RemoveAll(q => q.dead);
		tm.reveal = Math.Max(0, tm.reveal - dt * 1.8);
		// everyone moves
		for (int i = 0; i < TM_N; i++) { var b = tm.b[i]; double px = b.x, py = b.y; if (tmMine(b)) tmBody(b, ins[i], dt); else tnBody(b); b.mdx = b.x - px; b.mdy = b.y - py; }
		// the chain: no one too far from the others
		if (tm.P.tether > 0 && tm.snapAt < 0 && tmBoss)
			for (int i = 0; i < TM_N; i++) for (int j = i + 1; j < TM_N; j++)
			{
				var p = tm.b[i]; var q = tm.b[j]; if (p.gate || q.gate || p.down || q.down) continue;   // the chain holds only the living (one lying behind broke it on the others)
				var (px, py) = tnPos(p); var (qx, qy) = tnPos(q);
				if (hypot(px - qx, py - qy) > tm.P.tether && tm.snapAt < 0) { tmTether(i * 3 + j); tnTell("tb", new() { ["p"] = i * 3 + j }); }
			}
		// hurt, fall, bring back
		foreach (var b in tm.b)
		{
			if (b.down || b.gate) continue;
			b.inv -= dt; b.hurt -= dt;
			if (tmMine(b) && b.inv <= 0 && tmHit(b)) tmDamage(b);
			else if (!tmMine(b)) foreach (var q in tm.hail) if (Math.Abs(q.x - b.x) < 10 && Math.Abs(q.y - b.y) < 14) q.dead = true;   // the others' ice breaks on them here too
		}
		foreach (var b in tm.b)
		{
			if (!b.down || !tmMine(b)) continue;   // the others: their own PC brings them back (revT comes with their square)
			var h = tm.b.FirstOrDefault(o => o != b && !o.down && !o.gate && hypot(o.x - b.x, o.y - b.y) < 30);
			b.revT = h != null ? b.revT + dt : 0;
			if (b.revT >= TM_REV_HOLD) { b.revT = 0; b.down = false; b.hp = P_HP; b.inv = 1.5; b.vx = b.vy = 0; burst(b.x, b.y, TM_COL[b.i], 30, 320); sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.18, "triangle", 0.1); }); }
		}
		if (tm.b.Any(b => b.down) && tm.b.Any(b => b.gate)) foreach (var b in tm.b) if (b.gate && tmMine(b)) b.gate = false;   // out of the gate to help
		if (tmBoss && (tm.b.All(b => b.down) || tm.b.Any(b => b.down && tm.t - b.downAt > TM_REVIVE + (tmMine(b) ? 0 : 0.4)))) { tm.restartAt = tm.t + 0.9; flash = 0.3; tnTell("rs", new() { ["t"] = tm.restartAt }); }
		// the photo's pieces: each one takes their own, carries it up and puts it in its place in the big frame
		foreach (var s in tm.th) if (s.k == "pic" && !s.on)
		{
			var who = tm.b[(int)s.d.a];
			if (who.carry == s) { s.X = who.x - s.W / 2; s.Y = who.y - P_HALF - 8 - s.H; }
			else if (tmMine(who) && !who.down && !who.gate && hypot(who.x - (s.d.x + 0.5) * PT, who.y - (s.d.y + 0.5) * PT) < 30)
			{ who.carry = s; burst(who.x, who.y, TM_COL[who.i], 18, 240); sfx(tt => { sTone(tt, 660, 0.08, "triangle", 0.08); sTone(tt + 0.06, 990, 0.12, "triangle", 0.07); }); }
		}
		foreach (var s in tm.th) if (s.k == "slot" && !s.on)
		{
			var who = tm.b[(int)s.d.a]; if (who.carry == null || who.down || !tmMine(who)) continue;
			if (who.x + P_HALF > s.X && who.x - P_HALF < s.X + s.W && who.y + P_HALF > s.Y && who.y - P_HALF < s.Y + s.H) { tmPlace(s); tnTell("sl", new() { ["i"] = who.i }); }
		}
		if (tm.th.Any(s => s.k == "slot") && tm.th.Where(s => s.k == "slot").All(s => s.on) && tm.b.All(b => !b.down))   // all three alive to win
		{
			tm.doneAt = tm.t; tm.won = true; tm.wonAt = tm.t; flash = 0.9; shake = 10; burst(W / 2, H / 2, GOLD, 90, 640);
			sfx(tt => { sNoise(tt, 0.3, "highpass", 3000, 0.2); sTone(tt + 0.05, 523, 0.3, "triangle", 0.12); sTone(tt + 0.17, 659, 0.3, "triangle", 0.12); sTone(tt + 0.29, 784, 0.6, "triangle", 0.12); });
			tmOutro(); if (tmMe >= 0) tnWon(); return;
		}
		// pieces and the gate (a piece in the mirror is taken by a reflection)
		foreach (var b in tm.b) if (!b.down && !b.gate && tmMine(b)) { tmTiles(b, b.x, false); if (tm.P.mirror > 0) tmTiles(b, tmRx(b.x), true); }
		if (tmBoss && tm.b.All(b => b.gate)) { tmPageDone(tm.t); tnTell("pd", new() { ["t"] = tm.t }); }
		foreach (var b in tm.b) { b.trail.Add((b.x, b.y, tm.t)); b.trail.RemoveAll(q => tm.t - q.t > 0.18); }
		tm.steps.RemoveAll(q => tm.t - q.t > 2.5);
	}

	// lightning swaps the two (both must be up and out of the gate)
	static void tmDoSwap()
	{
		var p = tm.b[tm.swapA]; var q = tm.b[tm.swapB]; tm.swapA = tm.swapB = -1;
		if (p.down || q.down || p.gate || q.gate) return;
		if (tmMe < 0) { (p.x, q.x) = (q.x, p.x); (p.y, q.y) = (q.y, p.y); }
		else if (tmMine(p) || tmMine(q)) { var me = tmMine(p) ? p : q; var o = me == p ? q : p; (me.x, me.y) = tnNow(o); tm.tp[me.i]++; }   // online: mine goes where the other one is now (theirs comes with its message)
		foreach (var b in new[] { p, q }) { b.vy = 0; b.onT = null; b.onB = null; b.ground = false; b.dash = 0; b.trail.Clear(); burst(b.x, b.y, TM_COL[b.i], 24, 300); }
		tm.swapFx = tm.t; shake = Math.Max(shake, 8); flash = Math.Max(flash, 0.2);
		sfx(tt => { sNoise(tt, 0.3, "highpass", 2600, 0.18); sTone(tt, 1200, 0.1, "square", 0.05); sTone(tt + 0.05, 300, 0.2, "square", 0.05); });
	}
	public static bool tmSwapLive() => tm.swapA >= 0;

	// a gold frame on its way, on the beat: it lurches at each beat (its own beats while it waits for a button)
	static void tmMover(TS s, double dB)
	{
		var d = s.d; s.s += dB; double k = s.s + d.c, f = Math.Floor(k) + ease(Math.Min(1, (k - Math.Floor(k)) * 2.5));
		double u = f / Math.Max(0.5, d.b); u -= 2 * Math.Floor(u / 2); double p = u < 1 ? u : 2 - u;
		s.X = (d.x + (d.x1 - d.x) * p) * PT; s.Y = (d.y + (d.y1 - d.y) * p) * PT;
	}
	// the camera's flash: the other two each get a step under their feet where they are, for 8 beats
	static void tmSnap(TS cam, long bi)
	{
		cam.n = 0; cam.timer = tm.t; tmCamFx();
		foreach (var b in tm.b) if (b.i != (int)cam.d.a && tmMine(b)) tmSnapBody(b, bi + 8);
		tnTell("cam", new() { ["b"] = bi });
	}
	static void tmCamFx() { flash = Math.Max(flash, 0.45); sfx(tt => { sNoise(tt, 0.08, "highpass", 5000, 0.2); sTone(tt, 1800, 0.05, "square", 0.04); sNoise(tt + 0.09, 0.12, "bandpass", 1500, 0.08); }); }
	// a picture step under this square for 8 beats (until beat `until`); online its owner tells the others where it is
	static void tmSnapBody(TBody b, double until)
	{
		if (b.down || b.gate) return;
		var s = tmSnapStep(b.i, b.x - 20, b.y + P_HALF, until);
		b.vx = b.vy = 0; b.dash = 0; b.onT = s; b.onB = null; b.ground = true; b.coyote = P_COYOTE; b.canDash = true;
		tnTell("st", new() { ["x"] = s.X, ["y"] = s.Y, ["u"] = until });
	}
	static TS tmSnapStep(int who, double x, double y, double until)
	{
		var s = new TS { d = new TThing { k = "snap", a = who }, k = "snap", X = x, Y = y, W = 40, H = 10, one = true, solid = true, timer = until, s = tm.t };
		tm.th.Add(s); burst(x + 20, y - P_HALF, "#ffffff", 20, 260); return s;
	}
	// the chain breaks: everyone falls (the room's maker sees it and tells the others)
	static void tmTether(int pair)
	{
		tm.snapAt = pair; tm.snapT = tm.t; shake = 18; flash = 0.4;
		sfx(tt => { sNoise(tt, 0.5, "highpass", 2000, 0.2); sTone(tt, 60, 0.5, "sawtooth", 0.12); });
		foreach (var r in tm.b) if (!r.down && !r.gate && tmMine(r)) { r.inv = 0; r.hp = 1; tmDamage(r); }
	}
	// all three in the gate: the page is done (at music time t)
	static void tmPageDone(double t)
	{
		tm.doneAt = t; burst(W / 2, H / 2, GOLD, 60, 520);
		sfx(tt => { sTone(tt, 523, 0.2, "triangle", 0.12); sTone(tt + 0.12, 659, 0.2, "triangle", 0.12); sTone(tt + 0.24, 784, 0.4, "triangle", 0.12); });
	}
	// a piece of the photo goes into its place in the big frame
	static void tmPlace(TS slot)
	{
		var who = tm.b[(int)slot.d.a]; var pic = who.carry ?? tm.th.FirstOrDefault(q => q.k == "pic" && q.d.a == slot.d.a && !q.on); if (pic == null) return;
		slot.on = true; pic.on = true; pic.X = slot.X; pic.Y = slot.Y; who.carry = null; burst(slot.X + slot.W / 2, slot.Y + slot.H / 2, TM_COL[who.i], 36, 360); flash = 0.2; shake = 6;
		sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.12); sTone(tt + 0.07, 1047, 0.12, "triangle", 0.12); sTone(tt + 0.14, 1568, 0.25, "triangle", 0.1); });
	}
	// the photo is whole: the song goes on from the part's end to its own end, from the next beat
	static void tmOutro()
	{
		var L = tm.L; if (tm.P.outro <= 0 || tm.voices == null) return;
		var B = tm.P.beats; double len = B[^1] - B[0], k = Math.Floor(tm.t / len), tt = tm.t - k * len + B[0];
		int i = Array.BinarySearch(B, tt); if (i < 0) i = ~i - 1; i = Math.Clamp(i, 0, B.Length - 2);
		double when = actx.currentTime + Math.Max(0.02, B[i + 1] - tt); var old = tm.voices; tm.voices = new Voice[old.Length];
		for (int j = 0; j < old.Length; j++)
		{
			if (old[j] != null) { old[j].gain.setTargetAtTime(0, when - 0.02, 0.05); old[j].stop(when + 0.6); }
			if (L.bufs[j] == null || noMus) continue;
			var v = new Voice { song = L.bufs[j], loop = false }; v.gain.value = 1; v.start(when, tm.P.outro); tm.voices[j] = AudioCtx.Add(v); tm.stemOn[j] = true;
		}
	}

	// each one's letter by their own falls over the whole level (like the first level: S none, A one, B two, C three or more)
	public static string tmRank(int i) { int d = tm.falls[i]; return d >= 3 ? "C" : d == 2 ? "B" : d == 1 ? "A" : "S"; }

	static int tmPieces() { int n = 0; foreach (var r in tm.m) foreach (var c in r) if (c >= '1' && c <= '9') n++; return n; }
	static bool tmOpen() => tm.got.Count >= tmPieces();
	static void tmTiles(TBody b, double bx, bool refl)
	{
		int tx0 = (int)Math.Floor((bx - P_HALF) / PT), tx1 = (int)Math.Floor((bx + P_HALF) / PT), ty0 = (int)Math.Floor((b.y - P_HALF) / PT), ty1 = (int)Math.Floor((b.y + P_HALF) / PT);
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h) continue; char c = tm.m[ty][tx]; string k = tx + "," + ty;
			if (c >= '1' && c <= '9' && !tm.got.Contains(k)) { tmTake(tx, ty); tnTell("pc", new() { ["x"] = tx, ["y"] = ty }); }
			else if (c == 'G' && tmOpen() && !refl) { b.gate = true; b.vx = b.vy = 0; burst(b.x, b.y, TM_COL[b.i], 20, 260); sfx(tt => sTone(tt, 880, 0.15, "triangle", 0.08)); }
		}
	}
	// a song piece taken (by anyone): it plays from now on
	static void tmTake(int tx, int ty)
	{
		if (ty < 0 || ty >= tm.h || tx < 0 || tx >= tm.w) return; char c = tm.m[ty][tx]; string k = tx + "," + ty;
		if (c < '1' || c > '9' || tm.got.Contains(k)) return;
		tm.got.Add(k); double x = tx * PT + PT / 2, y = ty * PT + PT / 2;
		burst(x, y, GOLD, 30, 380); flash = 0.15; shake = 6;
		sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.12); sTone(tt + 0.07, 1047, 0.12, "triangle", 0.12); sTone(tt + 0.14, 1568, 0.25, "triangle", 0.1); });
		if (tm.P.pieces != null && tm.P.pieces.TryGetValue(c.ToString(), out int s) && s < tm.stemOn.Length && !tm.stemOn[s])
		{
			tm.stemOn[s] = true; tm.voices?[s]?.gain.setTargetAtTime(1, actx.currentTime, 0.25); tm.taken.Add((s, tm.t));
			tm.msg = tm.L.stemNames[s] + " انضاف للأغنية"; tm.msgT = 2.2;
		}
		if (tmOpen()) { tm.msg = "كملت! ادخلوا البوابة الثلاثة"; tm.msgT = 2.6; }
	}
	// spikes and saws at a point (a body, or its reflection in the mirror)
	static bool tmHitAt(double x, double y)
	{
		const double r = 5;
		int tx0 = (int)Math.Floor((x - r) / PT), tx1 = (int)Math.Floor((x + r) / PT), ty0 = (int)Math.Floor((y - r) / PT), ty1 = (int)Math.Floor((y + r) / PT);
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h) continue; char c = tm.m[ty][tx]; double ly = y - ty * PT;
			if (c == '^' && ly + r > PT * 0.45) return true;
			if (c == 'v' && ly - r < PT * 0.55) return true;
		}
		foreach (var s in tm.P.saws) { double sx = tmSawPos(s, out double sy); if (hypot(x - sx, y - sy) < s.r + r) return true; }
		return false;
	}
	static bool tmHit(TBody b)
	{
		const double r = 5;
		if (tmHitAt(b.x, b.y) || (tm.P.mirror > 0 && tmHitAt(tmRx(b.x), b.y))) return true;
		foreach (var q in tm.hail) if (Math.Abs(q.x - b.x) < 5 + r && Math.Abs(q.y - b.y) < 9 + r) { q.dead = true; return true; }
		if (tm.chaseX > -900 && b.x - r < tm.chaseX) return true;
		double bf = tmBeat() - Math.Floor(tmBeat());
		if (bf < 0.2) foreach (var s in tm.th) if (s.k == "bolt" && tmBoltLive(s))
			foreach (int c in tmBolts(s, tm.beatN)) { double cx = s.of.X + c * PT; if (b.x + r > cx && b.x - r < cx + PT && b.y > s.Y && b.y < s.Y + s.H) return true; }
		return false;
	}
	static void tmDamage(TBody b)
	{
		b.hp--; tm.hits++; b.inv = 2.0; b.hurt = 2.0; shake = 14; flash = 0.25; b.vy = Math.Min(b.vy, -520);
		burst(b.x, b.y, TM_COL[b.i], 22, 380); sfx(t => { sTone(t, 110, 0.3, "square", 0.18); sNoise(t, 0.25, "lowpass", 900, 0.3); });
		if (b.hp <= 0 && b.carry != null) { b.carry.X = b.carry.d.x * PT; b.carry.Y = b.carry.d.y * PT; b.carry = null; }
		if (b.hp <= 0) tm.falls[b.i]++;
		if (b.hp <= 0) { b.down = true; b.revT = 0; b.downAt = tm.t; b.vx = b.vy = 0; b.onT = null; burst(b.x, b.y, TM_COL[b.i], 50, 520); }
	}

	static void tmFlip(int who)
	{
		tm.flips[who]++;
		if (tm.th.Any(s => s.k == "flip" && (int)s.d.a == who)) sfx(tt => { sTone(tt, 660 + who * 110, 0.06, "triangle", 0.05); sTone(tt + 0.05, 990 + who * 110, 0.08, "triangle", 0.04); });
	}

	// one player: the platformer's moves (one jump, one dash back on the ground), on tiles and on the clouds
	static void tmBody(TBody b, TIn inp, double dt)
	{
		if (b.down || b.gate) return;
		// carried by what it stands on
		if (b.onT != null) { if (b.onT.solid) { b.x += b.onT.dx; b.y += b.onT.dy; } else { b.onT = null; b.ground = false; } }
		// on someone's head: it goes along sideways and down with them, but their jump doesn't throw it up (it jumped with them without pressing anything)
		if (b.onB != null) { if (!b.onB.down && !b.onB.gate && b.onB.mdy > -0.5) { b.x += b.onB.mdx; b.y += b.onB.mdy; } else { b.onB = null; b.ground = false; } }
		double mx = Math.Abs(inp.x) < 0.25 ? 0 : inp.x, my = inp.y;
		b.jumpHeld = inp.jumpHeld; if (inp.jump) b.jbuf = P_BUFFER;
		if (inp.dash && b.canDash && b.dash <= 0 && b.carry == null)
		{
			double m = hypot(mx, my), dx = m > 0.3 ? mx / m : b.fx, dy = m > 0.3 ? my / m : 0;
			b.ddx = dx; b.ddy = dy; b.dash = P_DASH_T; b.inv = Math.Max(b.inv, 0.28); b.canDash = false; b.dt0 = tm.t; b.sa = JS.atan2(dy, dx);
			burst(b.x, b.y, TM_COL[b.i], 8, 200); sfx(tt => sNoise(tt, 0.12, "bandpass", 3200, 0.14, 1));
			if (b.i == 1) tmFlip(1);
		}
		if (mx != 0) b.fx = Math.Sign(mx);
		if (b.dash > 0)
		{
			b.vx = b.ddx * P_DASH; b.vy = b.ddy * P_DASH; b.dash -= dt;
			if (b.dash <= 0) { b.vx = b.ddx * P_SPEED; b.vy = b.ddy < 0 ? b.ddy * P_SPEED * 0.6 : 0; }
		}
		else
		{
			double target = mx * P_SPEED, acc = b.ground ? P_ACC : P_AIR;
			b.vx = b.vx < target ? Math.Min(target, b.vx + acc * dt) : Math.Max(target, b.vx - acc * dt);
			if (b.jbuf > 0 && b.coyote > 0) { b.vy = -P_JUMP * (b.carry != null ? 0.82 : 1); b.jbuf = 0; b.coyote = 0; b.ground = false; b.onT = null; b.onB = null; sfx(tt => sTone(tt, 520, 0.08, "square", 0.03)); if (b.i == 0) tmFlip(0); }
			if (!b.jumpHeld && b.vy < -300) b.vy *= P_CUT;
			b.vy = Math.Min(P_FALL, b.vy + P_GRAV * dt);
		}
		b.jbuf -= dt; b.coyote -= dt;
		// the wind pushes (not part of its own speed)
		b.ex = 0;
		foreach (var s in tm.th) if (s.k == "wind" && b.x > s.X && b.x < s.X + s.W && b.y > s.Y && b.y < s.Y + s.H)
		{
			double bt = tmBeat(), ev = s.d.b; long gk = (long)Math.Floor(bt / ev); if (bt - gk * ev < s.d.c) b.ex = (gk % 2 == 0 ? 1 : -1) * s.d.a;
		}
		// x, then y: tiles, then the solid things
		double oy0 = b.y; double ox = b.x; b.x += (b.vx + b.ex) * dt; double mvx = b.x - ox;
		{
			int ty0 = (int)Math.Floor((b.y - P_HALF + 0.01) / PT), ty1 = (int)Math.Floor((b.y + P_HALF - 0.01) / PT);
			if (mvx > 0) { int tx = (int)Math.Floor((b.x + P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = tx * PT - P_HALF; b.vx = Math.Min(0, b.vx); break; } }
			else if (mvx < 0) { int tx = (int)Math.Floor((b.x - P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = (tx + 1) * PT + P_HALF; b.vx = Math.Max(0, b.vx); break; } }
			foreach (var s in tm.th) if (s.solid && !s.one && !(s.k == "cwall" && (int)s.d.a == b.i) && b.y + P_HALF > s.Y + 0.5 && b.y - P_HALF < s.Y + s.H - 0.5 && b.x + P_HALF > s.X && b.x - P_HALF < s.X + s.W)
			{
				if (mvx >= 0 && ox + P_HALF <= s.X + 1) { b.x = s.X - P_HALF; b.vx = Math.Min(0, b.vx); } else if (mvx <= 0 && ox - P_HALF >= s.X + s.W - 1) { b.x = s.X + s.W + P_HALF; b.vx = Math.Max(0, b.vx); }
				else if (oy0 - P_HALF < s.Y + s.H - 2) { if (b.x - s.X < s.X + s.W - b.x) b.x = s.X - P_HALF; else b.x = s.X + s.W + P_HALF; b.vx = 0; }   // a door closed on it: out to the nearer side (it went up into the floor)
			}
		}
		bool was = b.ground; b.ground = false; var oldOn = b.onT; b.onT = null; b.onB = null;
		double oy = b.y, ob = b.y + P_HALF; b.y += b.vy * dt;
		{
			int tx0 = (int)Math.Floor((b.x - P_HALF + 0.01) / PT), tx1 = (int)Math.Floor((b.x + P_HALF - 0.01) / PT);
			if (b.vy >= 0) { int ty = (int)Math.Floor((b.y + P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (tmTile(tx, ty)) { b.y = ty * PT - P_HALF; b.vy = 0; b.ground = true; break; } }
			else { int ty = (int)Math.Floor((b.y - P_HALF) / PT); for (int tx = tx0; tx <= tx1; tx++) if (tmTile(tx, ty)) { b.y = (ty + 1) * PT + P_HALF; b.vy = 0; if (b.dash > 0) b.dash = 0; break; } }
			foreach (var s in tm.th)
			{
				if (!s.solid || b.x + P_HALF <= s.X || b.x - P_HALF >= s.X + s.W) continue;
				if (s.one)
				{
					if (b.vy >= 0 && ob <= s.Y + Math.Max(0, s.dy) + 1 && b.y + P_HALF >= s.Y) { b.y = s.Y - P_HALF; b.vy = 0; b.ground = true; b.onT = s; }
				}
				else if (s.k == "cwall" && (int)s.d.a == b.i) { }
				else if (b.y + P_HALF > s.Y && b.y - P_HALF < s.Y + s.H)
				{
					if (b.vy >= 0 && ob <= s.Y + 2) { b.y = s.Y - P_HALF; b.vy = 0; b.ground = true; b.onT = s; }
					else if (b.vy < 0) { b.y = s.Y + s.H + P_HALF; b.vy = 0; if (b.dash > 0) b.dash = 0; }
				}
			}
		}
		// standing on someone's head (page two)
		if (tm.P.stack && b.vy >= 0 && !b.ground)
			foreach (var o in tm.b)
			{
				if (o == b || o.down || o.gate || Math.Abs(b.x - o.x) >= P_HALF * 2 - 2) continue;
				double top = o.y - P_HALF;
				if (ob <= top + Math.Max(0, o.mdy) + 1 && b.y + P_HALF >= top) { b.y = top - P_HALF; b.vy = 0; b.ground = true; b.onB = o; break; }
			}
		// gold's move: standing still a second (then it must move before the next)
		if (b.i == 2)
		{
			if (b.ground && Math.Abs(b.vx) < 5 && b.dash <= 0) { b.still += dt; if (b.still >= 1 && b.armed) { b.armed = false; tmFlip(2); } }
			else { b.still = 0; if (Math.Abs(b.vx) > 40 || !b.ground) b.armed = true; }
		}
		if (b.ground)
		{
			b.coyote = P_COYOTE; if (b.dash <= 0) b.canDash = true;
			if (!was && b.dash <= 0) burst(b.x, b.y + P_HALF, "rgba(255,255,255,0.6)", 4, 90);
			if (b.onT != null && b.onT.k is not "door" and not "hatch" and not "snap" and not "mover" && (tm.steps.Count == 0 || tm.steps.Where(q => q.who == b.i).Select(q => Math.Abs(q.x - b.x)).DefaultIfEmpty(99).Last() > 14))
				tm.steps.Add((b.x - (b.onT?.X ?? 0), b.y + P_HALF, tm.t, b.i));
		}
		{ double sp = hypot(b.vx, b.vy), tg = b.dash > 0 ? 1 : Math.Min(1, sp / P_SPEED) * 0.3; if (sp > 1) b.sa = JS.atan2(b.vy, b.vx); b.sk = b.sk + (tg - b.sk) * Math.Min(1, dt * (tg > b.sk ? 30 : 12)); }
		if (b.ground && (b.onT == null || b.onT.k is "sink" or "cloud" or "vis" or "bridge" or "path")) { b.gx = b.x; b.gy = b.y; }   // the last firm ground
		// fallen out: it lies where it last stood, so the others can still bring it back (under the screen no one could reach it)
		if (b.y > tm.h * PT + 40) { if (!double.IsNaN(b.gx)) { b.x = b.gx; b.y = b.gy; b.onT = null; b.onB = null; } b.hp = 1; tmDamage(b); b.vx = b.vy = 0; }
	}
}
