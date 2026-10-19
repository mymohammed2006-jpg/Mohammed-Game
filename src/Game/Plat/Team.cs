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
	public static readonly string[] TM_COL = { "#ff4d5e", "#2de2e6", "#ffcf5a" };   // each player's own colour
	public class TS                                                  // a thing while playing
	{
		public TThing d; public string k; public double X, Y, W, H, dx, dy, off, timer, s, fy, fv; public bool solid, one, on, melt, gone; public TS of, L, R; public int n;
	}
	public class TBody
	{
		public int i; public double x, y, vx, vy, coyote, jbuf, dash, ddx, ddy, fx = 1, sa, sk, inv, hurt, ex, downAt, dt0 = -9, prevB;
		public int hp = P_HP; public bool ground, canDash = true, jumpHeld, down, gate; public TS onT;
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
		public string msg; public double msgT, restartAt = -1, doneAt = -1, pageAt; public int restarts, hits;
		public List<(int k, double at)> taken = new();
	}
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
		return L;
	}

	// ---------- a page ----------
	public static void tmStart(TeamLevel L, int page)
	{
		tm = new TeamRun { L = L, page = page, P = L.pages[page] }; tm.stemOn = (bool[])L.on.Clone();
		tmBuild(); tmMusic();
		tm.msg = tm.P.name; tm.msgT = 3; tm.pageAt = 0;
	}
	static void tmBuild()
	{
		var P = tm.P; tm.m = P.map.Select(r => r.ToCharArray()).ToArray(); tm.h = tm.m.Length; tm.w = tm.m[0].Length;
		tm.th.Clear(); tm.hail.Clear(); tm.got.Clear(); tm.taken.Clear(); tm.steps.Clear();
		var byId = new Dictionary<string, TS>();
		foreach (var d in P.things)
		{
			var s = new TS { d = d, k = d.k, X = d.x * PT, Y = d.y * PT, W = d.w * PT, H = d.h * PT };
			s.one = d.k is "sink" or "fall" or "bridge" or "path" or "ride";
			s.solid = d.k is "sink" or "fall" or "door" or "hatch" or "ride";
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
		// the start: the S tile, the three side by side
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++) if (tm.m[y][x] == 'S')
			for (int i = 0; i < TM_N; i++) { tm.sx[i] = x * PT + PT / 2 + i * 30; tm.sy[i] = (y + 1) * PT - P_HALF; }
		for (int i = 0; i < TM_N; i++) tm.b[i] = new TBody { i = i, x = tm.sx[i], y = tm.sy[i], inv = 1.0, fx = 1 };
	}
	static void tmMusic()
	{
		tmStopMusic(); var L = tm.L; var lp = tm.P.loop;
		double when = actx.currentTime + 0.12; tm.voices = new Voice[L.stems.Length];
		for (int i = 0; i < L.stems.Length; i++)
		{
			if (L.bufs[i] == null || noMus) continue;
			var v = new Voice { song = L.bufs[i], loop = true, loopStart = lp[0], loopEnd = lp[1] };
			v.gain.value = tm.stemOn[i] ? 1 : 0; v.start(when, lp[0]); tm.voices[i] = AudioCtx.Add(v);
		}
		tm.t = -0.12;
	}
	public static void tmStopMusic() { if (tm?.voices == null) return; foreach (var v in tm.voices) if (v != null) { v.gain.setTargetAtTime(0, actx.currentTime, 0.06); v.stop(actx.currentTime + 0.5); } tm.voices = null; }
	// back to the page's start, all three (the music keeps going; the page's pieces go back too)
	static void tmRestart()
	{
		tm.restarts++; tm.restartAt = -1;
		for (int i = 0; i < tm.stemOn.Length; i++) if (tm.stemOn[i] && !tm.L.on[i]) { tm.stemOn[i] = false; tm.voices?[i]?.gain.setTargetAtTime(0, actx.currentTime, 0.2); }
		tmBuild(); tm.msg = "ترجعون لبداية الصفحة"; tm.msgT = 2;
		sfx(tt => { sTone(tt, 330, 0.25, "square", 0.06); sTone(tt + 0.12, 220, 0.35, "square", 0.06); });
	}

	// ---------- where things are ----------
	static bool tmTile(int tx, int ty) => tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h || tm.m[ty][tx] == '#';
	static bool tmPressed(string id) { var s = tm.th.FirstOrDefault(q => q.d.id == id && q.k is "stone" or "btn"); return s != null && s.on; }
	static bool tmAll(string[] ids) => ids != null && ids.All(tmPressed);
	static int tmOnIt(TS s) { int n = 0; foreach (var b in tm.b) if (!b.down && !b.gate && b.onT == s) n++; return n; }
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
		tm.t += dt; tm.pageAt += dt; tm.msgT -= dt; songT = tm.t;
		double bt = tmBeat(), spb = tm.L.spb; long bi = (long)Math.Floor(bt);
		if (tm.restartAt >= 0) { if (tm.t >= tm.restartAt) tmRestart(); return; }
		if (tm.doneAt >= 0) return;
		// buttons and stones first (from where everyone stood last step)
		foreach (var s in tm.th)
		{
			if (s.k == "stone") { double sx0 = s.d.x * PT; s.on = s.of != null && !s.of.melt && tm.b.Any(b => !b.down && b.onT == s.of && Math.Abs(b.x - sx0) < 26); }
			else if (s.k == "btn") s.on = tm.b.Any(b => !b.down && b.ground && Math.Abs(b.y + P_HALF - s.Y) < 3 && b.x > s.X - 4 && b.x < s.X + s.W + 4);
		}
		// then the things move
		foreach (var s in tm.th)
		{
			double y0 = s.Y, x0 = s.X; var d = s.d;
			switch (s.k)
			{
				case "sink":
					if (s.melt) { s.timer -= dt; if (s.timer <= 0) { s.melt = false; s.solid = true; s.off = 0; s.Y = s.fy; } break; }
					if (tmOnIt(s) > 0) s.off += d.b * dt; else s.off = Math.Max(0, s.off - 40 * dt);
					if (s.off >= d.a * PT) { s.melt = true; s.solid = false; s.timer = d.c * spb; burst(s.X + s.W / 2, s.Y, "rgba(255,255,255,0.8)", 26, 220); sfx(tt => sNoise(tt, 0.4, "lowpass", 900, 0.12)); }
					s.Y = s.fy + s.off; break;
				case "fall":
					if (s.gone) { s.timer -= dt; s.fv += 1800 * dt; s.off += s.fv * dt; if (s.timer <= 0) { s.gone = false; s.solid = true; s.off = 0; s.fv = 0; s.timer = 0; s.Y = s.fy; } else s.Y = s.fy + s.off; break; }
					if (s.timer > 0) { s.timer -= dt; if (s.timer <= 0) { s.gone = true; s.solid = false; s.timer = d.b * spb; s.fv = 0; sfx(tt => sNoise(tt, 0.25, "bandpass", 700, 0.1)); } }
					else if (tmOnIt(s) > 0) s.timer = d.a * spb;
					break;
				case "bridge": case "path": s.solid = tmAll(d.need); s.s = Math.Clamp(s.s + (s.solid ? 6 : -4) * dt, 0, 1); break;
				case "door": s.solid = !tmAll(d.need); s.s = Math.Clamp(s.s + (s.solid ? -3 : 5) * dt, 0, 1); break;
				case "scale":
					{
						bool btn = tmAll(d.need), both = tmOnIt(s.L) > 0 && tmOnIt(s.R) > 0;
						double s0 = s.s;
						if (btn) s.s = Math.Max(-1, s.s - 0.6 * dt); else if (both) s.s = Math.Min(1, s.s + 0.45 * dt);
						if (s.s != s0 && (long)(s.s * 40) != (long)(s0 * 40)) sfx(tt => sTone(tt, 160 + 60 * s.s, 0.05, "triangle", 0.03));
						double ly = d.y * PT + (s.s >= 0 ? s.s * d.a : s.s * d.b) * PT, ry = d.y1 * PT - Math.Max(0, s.s) * d.c * PT;
						s.L.dy = ly - s.L.Y; s.L.Y = ly; s.R.dy = ry - s.R.Y; s.R.Y = ry;
						break;
					}
				case "hatch": s.solid = !(s.of != null && s.of.s > 0.02); s.s = Math.Clamp(s.s + (s.solid ? -4 : 4) * dt, 0, 1); break;
				case "ride":
					{
						int n = tmOnIt(s); double ex = d.x1 * PT;
						if (n >= TM_N) s.X = Math.Min(ex, s.X + d.a * dt); else if (s.X < ex) s.X = Math.Max(d.x * PT, s.X - d.b * dt);
						s.n = n; break;
					}
			}
			if (s.k is not ("panL" or "panR" or "scale")) { s.dx = s.X - x0; s.dy = s.Y - y0; }
		}
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
		}
		foreach (var q in tm.hail)
		{
			q.y += tm.th.First(s => s.k == "hail").d.a * dt;
			int tx = (int)Math.Floor(q.x / PT), ty = (int)Math.Floor((q.y + 6) / PT);
			if (tmTile(tx, ty) || tm.th.Any(s => s.solid && s.k != "door" && q.x > s.X && q.x < s.X + s.W && q.y + 6 > s.Y && q.y < s.Y + s.H)) { q.dead = true; burst(q.x, q.y, "rgba(200,235,255,0.9)", 6, 140); }
		}
		tm.hail.RemoveAll(q => q.dead);
		// everyone moves
		for (int i = 0; i < TM_N; i++) tmBody(tm.b[i], ins[i], dt);
		// hurt, fall, bring back
		foreach (var b in tm.b)
		{
			if (b.down || b.gate) continue;
			b.inv -= dt; b.hurt -= dt;
			if (b.inv <= 0 && tmHit(b)) tmDamage(b);
		}
		foreach (var b in tm.b)
		{
			if (!b.down) continue;
			var h = tm.b.FirstOrDefault(o => o != b && !o.down && !o.gate && hypot(o.x - b.x, o.y - b.y) < 30);
			if (h != null) { b.down = false; b.hp = P_HP; b.inv = 1.5; b.vx = b.vy = 0; burst(b.x, b.y, TM_COL[b.i], 30, 320); sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.18, "triangle", 0.1); }); }
		}
		if (tm.b.Any(b => b.down) && tm.b.Any(b => b.gate)) foreach (var b in tm.b) if (b.gate) b.gate = false;   // out of the gate to help
		if (tm.b.All(b => b.down) || tm.b.Any(b => b.down && tm.t - b.downAt > TM_REVIVE)) { tm.restartAt = tm.t + 0.9; flash = 0.3; }
		// pieces and the gate
		foreach (var b in tm.b) if (!b.down && !b.gate) tmTiles(b);
		if (tm.b.All(b => b.gate)) { tm.doneAt = tm.t; burst(W / 2, H / 2, GOLD, 60, 520); sfx(tt => { sTone(tt, 523, 0.2, "triangle", 0.12); sTone(tt + 0.12, 659, 0.2, "triangle", 0.12); sTone(tt + 0.24, 784, 0.4, "triangle", 0.12); }); }
		foreach (var b in tm.b) { b.trail.Add((b.x, b.y, tm.t)); b.trail.RemoveAll(q => tm.t - q.t > 0.18); }
		tm.steps.RemoveAll(q => tm.t - q.t > 2.5);
	}

	static int tmPieces() { int n = 0; foreach (var r in tm.m) foreach (var c in r) if (c >= '1' && c <= '9') n++; return n; }
	static bool tmOpen() => tm.got.Count >= tmPieces();
	static void tmTiles(TBody b)
	{
		int tx0 = (int)Math.Floor((b.x - P_HALF) / PT), tx1 = (int)Math.Floor((b.x + P_HALF) / PT), ty0 = (int)Math.Floor((b.y - P_HALF) / PT), ty1 = (int)Math.Floor((b.y + P_HALF) / PT);
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h) continue; char c = tm.m[ty][tx]; string k = tx + "," + ty;
			if (c >= '1' && c <= '9' && !tm.got.Contains(k))
			{
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
			else if (c == 'G' && tmOpen()) { b.gate = true; b.vx = b.vy = 0; burst(b.x, b.y, TM_COL[b.i], 20, 260); sfx(tt => sTone(tt, 880, 0.15, "triangle", 0.08)); }
		}
	}
	static bool tmHit(TBody b)
	{
		const double r = 5;
		int tx0 = (int)Math.Floor((b.x - r) / PT), tx1 = (int)Math.Floor((b.x + r) / PT), ty0 = (int)Math.Floor((b.y - r) / PT), ty1 = (int)Math.Floor((b.y + r) / PT);
		for (int ty = ty0; ty <= ty1; ty++) for (int tx = tx0; tx <= tx1; tx++)
		{
			if (tx < 0 || ty < 0 || tx >= tm.w || ty >= tm.h) continue; char c = tm.m[ty][tx]; double ly = b.y - ty * PT;
			if (c == '^' && ly + r > PT * 0.45) return true;
			if (c == 'v' && ly - r < PT * 0.55) return true;
		}
		foreach (var s in tm.P.saws) { double sx = tmSawPos(s, out double sy); if (hypot(b.x - sx, b.y - sy) < s.r + r) return true; }
		foreach (var q in tm.hail) if (Math.Abs(q.x - b.x) < 5 + r && Math.Abs(q.y - b.y) < 9 + r) { q.dead = true; return true; }
		double bf = tmBeat() - Math.Floor(tmBeat());
		if (bf < 0.2) foreach (var s in tm.th) if (s.k == "bolt" && tmBoltLive(s))
			foreach (int c in tmBolts(s, tm.beatN)) { double cx = s.of.X + c * PT; if (b.x + r > cx && b.x - r < cx + PT && b.y > s.Y && b.y < s.Y + s.H) return true; }
		return false;
	}
	static void tmDamage(TBody b)
	{
		b.hp--; tm.hits++; b.inv = 2.0; b.hurt = 2.0; shake = 14; flash = 0.25; b.vy = Math.Min(b.vy, -520);
		burst(b.x, b.y, TM_COL[b.i], 22, 380); sfx(t => { sTone(t, 110, 0.3, "square", 0.18); sNoise(t, 0.25, "lowpass", 900, 0.3); });
		if (b.hp <= 0) { b.down = true; b.downAt = tm.t; b.vx = b.vy = 0; b.onT = null; burst(b.x, b.y, TM_COL[b.i], 50, 520); }
	}

	// one player: the platformer's moves (one jump, one dash back on the ground), on tiles and on the clouds
	static void tmBody(TBody b, TIn inp, double dt)
	{
		if (b.down || b.gate) return;
		// carried by what it stands on
		if (b.onT != null) { if (b.onT.solid) { b.x += b.onT.dx; b.y += b.onT.dy; } else { b.onT = null; b.ground = false; } }
		double mx = Math.Abs(inp.x) < 0.25 ? 0 : inp.x, my = inp.y;
		b.jumpHeld = inp.jumpHeld; if (inp.jump) b.jbuf = P_BUFFER;
		if (inp.dash && b.canDash && b.dash <= 0)
		{
			double m = hypot(mx, my), dx = m > 0.3 ? mx / m : b.fx, dy = m > 0.3 ? my / m : 0;
			b.ddx = dx; b.ddy = dy; b.dash = P_DASH_T; b.inv = Math.Max(b.inv, 0.28); b.canDash = false; b.dt0 = tm.t; b.sa = JS.atan2(dy, dx);
			burst(b.x, b.y, TM_COL[b.i], 8, 200); sfx(tt => sNoise(tt, 0.12, "bandpass", 3200, 0.14, 1));
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
			if (b.jbuf > 0 && b.coyote > 0) { b.vy = -P_JUMP; b.jbuf = 0; b.coyote = 0; b.ground = false; b.onT = null; sfx(tt => sTone(tt, 520, 0.08, "square", 0.03)); }
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
		double ox = b.x; b.x += (b.vx + b.ex) * dt; double mvx = b.x - ox;
		{
			int ty0 = (int)Math.Floor((b.y - P_HALF + 0.01) / PT), ty1 = (int)Math.Floor((b.y + P_HALF - 0.01) / PT);
			if (mvx > 0) { int tx = (int)Math.Floor((b.x + P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = tx * PT - P_HALF; b.vx = Math.Min(0, b.vx); break; } }
			else if (mvx < 0) { int tx = (int)Math.Floor((b.x - P_HALF) / PT); for (int ty = ty0; ty <= ty1; ty++) if (tmTile(tx, ty)) { b.x = (tx + 1) * PT + P_HALF; b.vx = Math.Max(0, b.vx); break; } }
			foreach (var s in tm.th) if (s.solid && !s.one && b.y + P_HALF > s.Y + 0.5 && b.y - P_HALF < s.Y + s.H - 0.5 && b.x + P_HALF > s.X && b.x - P_HALF < s.X + s.W)
			{ if (mvx >= 0 && ox + P_HALF <= s.X + 1) { b.x = s.X - P_HALF; b.vx = Math.Min(0, b.vx); } else if (mvx <= 0 && ox - P_HALF >= s.X + s.W - 1) { b.x = s.X + s.W + P_HALF; b.vx = Math.Max(0, b.vx); } }
		}
		bool was = b.ground; b.ground = false; var oldOn = b.onT; b.onT = null;
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
				else if (b.y + P_HALF > s.Y && b.y - P_HALF < s.Y + s.H)
				{
					if (b.vy >= 0 && ob <= s.Y + 2) { b.y = s.Y - P_HALF; b.vy = 0; b.ground = true; b.onT = s; }
					else if (b.vy < 0) { b.y = s.Y + s.H + P_HALF; b.vy = 0; if (b.dash > 0) b.dash = 0; }
				}
			}
		}
		if (b.ground)
		{
			b.coyote = P_COYOTE; if (b.dash <= 0) b.canDash = true;
			if (!was && b.dash <= 0) burst(b.x, b.y + P_HALF, "rgba(255,255,255,0.6)", 4, 90);
			if (b.onT != null && b.onT.k is not "door" and not "hatch" && (tm.steps.Count == 0 || tm.steps.Where(q => q.who == b.i).Select(q => Math.Abs(q.x - b.x)).DefaultIfEmpty(99).Last() > 14))
				tm.steps.Add((b.x - (b.onT?.X ?? 0), b.y + P_HALF, tm.t, b.i));
		}
		{ double sp = hypot(b.vx, b.vy), tg = b.dash > 0 ? 1 : Math.Min(1, sp / P_SPEED) * 0.3; if (sp > 1) b.sa = JS.atan2(b.vy, b.vx); b.sk = b.sk + (tg - b.sk) * Math.Min(1, dt * (tg > b.sk ? 30 : 12)); }
		if (b.y > tm.h * PT + 40) { b.hp = 1; tmDamage(b); }
	}
}
