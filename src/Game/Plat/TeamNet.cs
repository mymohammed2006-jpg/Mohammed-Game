using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// The co-op level for three, online (his words 18:44 10-19: exactly three, each at home). One makes the room and plays red; the
// first one in with the code plays cyan, the second gold. The maker's PC is in the middle: it has a connection to each of the
// other two and passes on what each one sends to the other (rooms/{code} carries two offers, o and o2).
// Each PC runs the whole level on the same music clock and moves only its own square; the other two come from their messages
// (30 a second, drawn 100 ms in the past so they move smoothly; on a moving thing they ride where that thing is on this PC).
// What everyone must agree on goes as an event: a piece taken, a camera's flash and the picture steps it gives, a photo piece
// placed (whoever did it tells), and the page's restart, the page done and the chain breaking (the maker decides those).
// Each event carries the page's build number (gen), so an event from before a restart is dropped.
// Anyone leaving ends the room for all (the level needs the three). Each one's own letter, points and XP count like P1.
// States: tmIntro (the start, the same moment on the three PCs), tmPlay, tmPause, tmSet (the settings from the pause).
public static partial class G
{
	public class TSnap { public double at, x, y, rx, ry, vx, vy, sa, sk, fx, ddx, ddy, dash, still, revT, hurt; public int gen, fl, hp, falls, flips, tp, on; public uint keys; }
	public partial class NetRun
	{
		public bool team; public int seat; public Peer[] tp = new Peer[TM_N];          // host: tp[1], tp[2]; the others: tp[0] (to the host)
		public string[] nm = new string[TM_N], sk = new string[TM_N]; public bool[] hi = new bool[TM_N], said = new bool[TM_N];
		public double[] rt = { -1, -1, -1 }, connAt = new double[TM_N], pingT = new double[TM_N];
		public List<TSnap>[] ts = { new(), new(), new() }; public List<(double at, double x, double y)> hist = new();
		public List<(string k, JsonNode j)> later = new();
		public byte[] world; public bool worldNew;   // guests: the maker's last word on the things the three move by standing on them (type 3)   // events from a page build this PC hasn't reached yet (it is a little behind)
		public double goAt; public bool tre;   // v49: this start is the maker's restart of the whole level (everyone sees a line)
		public List<Peer> all() => team ? tp.Where(p => p != null).ToList() : vs ? vp.Where(p => p != null).ToList() : peer != null ? new List<Peer> { peer } : new List<Peer>();
	}
	const double TN_DELAY = 100;                // ms: the others are drawn this far in the past
	// the radio (two, the shield and the cable need the other one where they really are): 60 messages a second, drawn 50 ms back
	static double tnDelay => tmDuo ? 50 : TN_DELAY;
	static double tnEvery => tmDuo ? 16 : 33;
	public static int tnPage0 = 0;              // the tests: start on another page
	public static double TN_DT = STEP;          // the tests: the bots' routes were made on 60 steps a second
	public static Func<TIn> tnBot;              // the tests: a bot plays this PC's square
	static string tnName(int i) => net?.nm[i] is { Length: > 0 } s ? s : tm?.names[i] ?? "";
	static int tnN => net?.L?.team?.players ?? TM_N;   // how many play this level (the photo three, the radio two)
	static string tnCol(int i) { var c = net?.L?.team?.cols; return c != null && i < c.Length ? TM_COL[c[i]] : TM_COL[i]; }

	// ---------- the room ----------
	static async void tnHost(PlatLevel L)
	{
		int N = L.team.players; var n = net = new NetRun { host = true, team = true, seat = 0, L = L }; n.tp[1] = new Peer(); if (N > 2) n.tp[2] = new Peer();
		n.nm[0] = acc?.name ?? ""; n.sk[0] = skin; n.hi[0] = true; netSetPhase("make"); state = "pltRoom";
		try
		{
			var o1 = n.tp[1].Offer(); var o2 = N > 2 ? n.tp[2].Offer() : Task.FromResult<string>(null); await Task.WhenAll(o1, o2); if (net != n) return;
			string code = await roomMake(L.id, o1.Result, o2.Result); if (net != n) { if (code != null) _ = roomDel(code); return; }
			if (code == null) { netEnd("ما قدرت أسوي غرفة، جرب مرة ثانية"); return; }
			n.code = code; netSetPhase("wait");
			// the answers (the room stays 10 minutes)
			while (net == n && Enumerable.Range(1, N - 1).Any(s => n.connAt[s] == 0))
			{
				await Task.Delay(1500); if (net != n) return;
				var d = await roomGet(code); if (net != n) return;
				if (d == null) { netEnd("انتهت الغرفة"); return; }
				for (int s = 1; s < N; s++)
				{
					string a = s == 1 ? d.a : d.a2; if (a == null || n.connAt[s] > 0) continue;
					n.nm[s] = (s == 1 ? d.gn : d.gn2) ?? "";
					if (!n.tp[s].Answer(a)) { netEnd("صار خطأ بالاتصال، جرب مرة ثانية"); return; }
					n.connAt[s] = performance.now();
				}
				if (performance.now() - n.phaseAt > 600000) { _ = roomDel(code); netEnd("انتهت الغرفة"); return; }
			}
		}
		catch (Exception) { if (net == n) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); }
	}
	// the first free seat; if someone takes it at the same moment (the rules refuse), the other one
	static async void tnJoin(NetRun n, RoomDoc d)
	{
		try
		{
			n.team = true; n.nm[0] = d.hn ?? ""; n.L = PLATS.FirstOrDefault(p => p.id == d.lv && p.team != null);
			if (n.L == null) { netEnd("حدّث اللعبة عشان تلعب هذي المرحلة"); return; }
			if (d.h == acc.uid) { netEnd("هذي غرفتك أنت"); return; }
			int N = n.L.team.players; string full = N > 2 ? "الغرفة فيها ٣ لاعبين" : "الغرفة فيها لاعبين";
			for (int tries = 0; tries < 3; tries++)
			{
				if (d.g == acc.uid || d.g2 == acc.uid) { netEnd("أنت داخل هذي الغرفة من قبل"); return; }
				int seat = d.a == null ? 1 : N > 2 && d.a2 == null ? 2 : 0;
				if (seat == 0) { netEnd(full); return; }
				var p = new Peer(); string ans = await p.Join(seat == 1 ? d.o : d.o2); if (net != n) { p.Close(); return; }
				if (ans == null) { p.Close(); netEnd("صار خطأ بالاتصال، جرب مرة ثانية"); return; }
				try { await roomAnswer(n.code, ans, seat); }
				catch (FetchErr e) when (e.code == 403) { p.Close(); if (net != n) return; d = await roomGet(n.code); if (net != n) return; if (d == null) { netEnd("انتهت الغرفة"); return; } continue; }
				if (net != n) { p.Close(); return; }
				n.seat = seat; n.tp[0] = p; n.peer = p; n.hi[0] = true; n.connAt[0] = performance.now(); netSetPhase("conn");
				return;
			}
			netEnd(full);
		}
		catch (Exception) { if (net == n) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); }
	}

	// ---------- sending ----------
	static void tnSendB(bool rel, byte[] d, int skip = -1) { var n = net; if (n == null) return; for (int s = 0; s < TM_N; s++) if (s != skip) n.tp[s]?.Send(rel, d); }
	static void tnSend(JsonObject o, int skip = -1) => tnSendB(true, Encoding.UTF8.GetBytes(o.ToJsonString()), skip);
	// an event of the level (only online: the tests have all three here)
	static void tnTell(string k, JsonObject o)
	{
		var n = net; if (tmMe < 0 || n == null || !n.team || tm == null) return;
		o["k"] = k; o["w"] = tmMe; o["g"] = tm.gen; tnSend(o);
	}
	// the host tells the two who is in the room
	static void tnRoster()
	{
		var n = net; var o = new JsonObject { ["k"] = "ro", ["n"] = new JsonArray(n.nm.Select(x => (JsonNode)(x ?? "")).ToArray()), ["s"] = new JsonArray(n.sk.Select(x => (JsonNode)(x ?? "")).ToArray()), ["h"] = new JsonArray(n.hi.Select(x => (JsonNode)x).ToArray()) };
		tnSend(o);
	}
	// the maker, 30 times a second: the things the three drive by standing on them (scale, ride, sinking and falling clouds, stones,
	// buttons), as it sees them. Each PC saw the others 100 ms late and differently, so the pans drifted apart (gold rode into the
	// saw on its own PC only); now the two follow the maker. Type 3: page build, how many, then per thing: flags, a number, a timer.
	// The radio: its lifts (how far along, and whether their key holds them now) and how high the static is.
	static readonly HashSet<string> TN_WORLD = new() { "scale", "ride", "sink", "fall", "stone", "btn", "cbtn", "mover", "rise" };
	static int tnBase() { int k = tm.th.FindIndex(q => q.k == "snap"); return k < 0 ? tm.th.Count : k; }
	static void tnWorldSend()
	{
		int nb = Math.Min(tnBase(), 400); var d = new byte[4 + nb * 9]; int o = 0;
		d[o++] = 3; d[o++] = (byte)(tm.gen & 255); BitConverter.TryWriteBytes(d.AsSpan(o), (short)nb); o += 2;
		for (int i = 0; i < nb; i++)
		{
			var s = tm.th[i]; int fl = 0; double v = 0, t = 0;
			if (TN_WORLD.Contains(s.k))
			{
				int occ = s.k == "scale" ? (tmOnIt(s.L) > 0 && tmOnIt(s.R) > 0 ? 1 : 0) : tmOnIt(s);
				fl = (s.on || (s.lat && s.k is "stone" or "btn" or "cbtn") ? 1 : 0) | (occ > 0 ? 2 : 0) | (s.melt ? 4 : 0) | (s.gone ? 8 : 0) | (Math.Min(3, occ) << 4);
				v = s.k == "scale" ? s.s : s.k == "ride" ? s.X : s.k == "mover" ? s.s : s.k == "rise" ? tm.riseY : s.off; t = s.timer;
				if (s.k == "mover") fl = s.d.need == null || tmNeed(s.d) ? 1 : 0;
				s.lat = false;
			}
			d[o++] = (byte)fl; BitConverter.TryWriteBytes(d.AsSpan(o), (float)v); o += 4; BitConverter.TryWriteBytes(d.AsSpan(o), (float)t); o += 4;
		}
		tnSendB(false, d);
	}
	// a guest: the maker's word on thing s (false: none for this page build yet, or this PC is the maker or a test)
	public static bool tnWorld(TS s, out int fl, out double v, out double t)
	{
		fl = 0; v = t = 0; var n = net; if (tmMe <= 0 || n?.world == null || n.world[1] != (tm.gen & 255)) return false;
		var d = n.world; int i = tm.th.IndexOf(s), nb = BitConverter.ToInt16(d, 2); if (i < 0 || i >= nb || 4 + i * 9 + 9 > d.Length) return false;
		int o = 4 + i * 9; fl = d[o]; v = BitConverter.ToSingle(d, o + 1); t = BitConverter.ToSingle(d, o + 5); return true;
	}
	// my square, 30 times a second (type 2: seat, page build, flags, lives, falls, moves, swaps, what it stands on, then the numbers, then the keys it stood on since the last one)
	static void tnSnapSend()
	{
		var b = tm.b[tmMe]; var d = new byte[74]; int o = 0;
		void F(double v) { BitConverter.TryWriteBytes(d.AsSpan(o), (float)v); o += 4; }
		int on = b.onT != null && b.onT.k != "snap" ? tm.th.IndexOf(b.onT) : -1;
		d[o++] = 2; d[o++] = (byte)tmMe; d[o++] = (byte)(tm.gen & 255);
		d[o++] = (byte)((b.down ? 1 : 0) | (b.gate ? 2 : 0) | (b.ground ? 4 : 0) | (b.carry != null ? 8 : 0) | (b.armed ? 16 : 0));
		d[o++] = (byte)Math.Max(0, b.hp); d[o++] = (byte)Math.Min(255, tm.falls[tmMe]); d[o++] = (byte)(tm.flips[tmMe] & 255); d[o++] = (byte)(tm.tp[tmMe] & 255);
		BitConverter.TryWriteBytes(d.AsSpan(o), (short)on); o += 2;
		F(b.x); F(b.y); F(on >= 0 ? b.x - b.onT.X : 0); F(on >= 0 ? b.y - b.onT.Y : 0); F(b.vx); F(b.vy); F(b.sa); F(b.sk); F(b.fx); F(b.ddx); F(b.ddy); F(b.dash); F(b.still); F(b.revT); F(b.hurt);
		BitConverter.TryWriteBytes(d.AsSpan(o), tmMyKeys); tmMyKeys = 0;
		tnSendB(false, d);
	}
	static void tnSnapGot(byte[] d, double now)
	{
		if (d.Length < 70 || d[0] != 2 || net == null) return; int o = 1;
		double F() { double v = BitConverter.ToSingle(d, o); o += 4; return v; }
		int seat = d[o++]; if (seat >= TM_N) return;
		var s = new TSnap { at = now, gen = d[o++], fl = d[o++], hp = d[o++], falls = d[o++], flips = d[o++], tp = d[o++] };
		s.on = BitConverter.ToInt16(d, o); o += 2;
		s.x = F(); s.y = F(); s.rx = F(); s.ry = F(); s.vx = F(); s.vy = F(); s.sa = F(); s.sk = F(); s.fx = F(); s.ddx = F(); s.ddy = F(); s.dash = F(); s.still = F(); s.revT = F(); s.hurt = F();
		if (d.Length >= 74) s.keys = BitConverter.ToUInt32(d, o);
		var L = net.ts[seat]; L.Add(s); if (L.Count > 40) L.RemoveAt(0);
	}

	// ---------- the others' squares ----------
	static TS tnThing(int i) => i >= 0 && i < tm.th.Count && tm.th[i].k != "snap" ? tm.th[i] : null;
	static double lerp(double a, double b, double f) => a + (b - a) * f;
	static bool tnSameGen(TSnap s) => s.gen == (tm.gen & 255);
	// the maker: whether another one's newest message says they stood on their key (k = the key's place among the page's keys)
	static bool tnKeyBit(int seat, int k)
	{
		var n = net; if (tmMe != 0 || n == null || seat == tmMe || k < 0 || k >= 32) return false;
		var c = n.ts[seat].LastOrDefault(tnSameGen); return c != null && ((c.keys >> k) & 1) != 0;
	}
	// where another one is now on this PC (their messages, 100 ms back; called by tmStep instead of moving it)
	static void tnBody(TBody b)
	{
		var n = net; if (n == null || !n.team) return;
		double t = performance.now() - tnDelay; TSnap a = null, c = null;
		foreach (var s in n.ts[b.i]) { if (!tnSameGen(s)) continue; if (s.at <= t) a = s; else { c = s; break; } }
		if (a == null && c == null) return;   // nothing from this page yet: it stays where the page put it
		a ??= c; c ??= a;
		double f = c.at > a.at ? clamp((t - a.at) / (c.at - a.at), 0, 1) : 1;
		if (a.tp != c.tp) { a = c; f = 1; }   // a jump (lightning swap): no sliding across
		TS onA = tnThing(a.on), onC = tnThing(c.on);
		if (onC != null && onA == onC) { b.x = onC.X + lerp(a.rx, c.rx, f); b.y = onC.Y + lerp(a.ry, c.ry, f); }
		else { b.x = lerp(a.x, c.x, f); b.y = lerp(a.y, c.y, f); }
		b.vx = c.vx; b.vy = c.vy; b.sa = c.sa; b.sk = c.sk; b.fx = c.fx; b.ddx = c.ddx; b.ddy = c.ddy; b.dash = c.dash; b.still = c.still; b.hurt = c.hurt; b.hp = c.hp;
		b.ground = (c.fl & 4) != 0; b.armed = (c.fl & 16) != 0; b.onT = onC != null && onC.solid ? onC : null; b.onB = null;
		bool down = (c.fl & 1) != 0, gate = (c.fl & 2) != 0;
		if (down && !b.down) { b.down = true; b.downAt = tm.t; b.onT = null; burst(b.x, b.y, TM_COL[b.i], 50, 520); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.12); sNoise(tt, 0.25, "lowpass", 900, 0.2); }); }
		else if (!down && b.down) { b.down = false; burst(b.x, b.y, TM_COL[b.i], 30, 320); sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.18, "triangle", 0.1); }); }
		b.revT = down ? c.revT : 0;
		if (gate && !b.gate) { burst(b.x, b.y, TM_COL[b.i], 20, 260); sfx(tt => sTone(tt, 880, 0.15, "triangle", 0.08)); }
		b.gate = gate;
		tm.falls[b.i] = c.falls; tm.flips[b.i] = c.flips;
		// their photo piece goes with them; dropped (a fall), it goes home
		bool car = (c.fl & 8) != 0;
		if (car && b.carry == null) b.carry = tm.th.FirstOrDefault(q => q.k == "pic" && (int)q.d.a == b.i && !q.on);
		else if (!car && b.carry != null) { if (!b.carry.on) { b.carry.X = b.carry.d.x * PT; b.carry.Y = b.carry.d.y * PT; } b.carry = null; }
	}
	// the maker: each one as their newest message has it (for who stands on what; mine as it is)
	static TBody[] tnSeenAll()
	{
		var n = net; var r = new TBody[tm.b.Length];
		for (int i = 0; i < r.Length; i++)
		{
			var b = tm.b[i]; r[i] = b; if (n == null || i == tmMe) continue;
			var c = n.ts[i].LastOrDefault(tnSameGen); if (c == null) continue;
			var on = tnThing(c.on); bool dn = (c.fl & 1) != 0, gt = (c.fl & 2) != 0;
			r[i] = new TBody { i = i, x = on != null ? on.X + c.rx : c.x, y = on != null ? on.Y + c.ry : c.y, ground = (c.fl & 4) != 0, down = dn, gate = gt, onT = on != null && on.solid ? on : null, hp = c.hp };
		}
		return r;
	}
	// where a square is for the chain check: mine as it was 100 ms ago, like the others are seen here
	static (double, double) tnPos(TBody b)
	{
		var n = net; if (tmMe < 0 || n == null || b.i != tmMe || n.hist.Count == 0) return (b.x, b.y);
		double t = performance.now() - tnDelay; var h = n.hist[0]; foreach (var q in n.hist) { if (q.at > t) break; h = q; }
		return (h.x, h.y);
	}
	// where another one is right now (its newest message, moved on by its speed): where my square lands in a lightning swap
	// (ext false: as the newest message has it, not moved on: the radio's cable)
	static (double, double) tnNow(TBody o, bool ext = true)
	{
		var n = net; var s = n?.ts[o.i].LastOrDefault(tnSameGen); if (s == null) return (o.x, o.y);
		var on = tnThing(s.on); if (on != null) return (on.X + s.rx, on.Y + s.ry);
		if (!ext) return (s.x, s.y);
		double age = Math.Min(0.15, (performance.now() - s.at) / 1000 + 0.04);
		return (s.x + s.vx * age, s.y + s.vy * age);
	}

	// ---------- the level's events from the others ----------
	static void tnEvent(string k, JsonNode j)
	{
		if (tm == null || !state.StartsWith("tm") || state == "tmIntro") return;
		int I(string f) => j[f]?.GetValue<int>() ?? -1; double D(string f) => j[f]?.GetValue<double>() ?? 0;
		int w = I("w"); if (w < 0 || w >= TM_N) return;
		if (k == "xp") { tm.xpGot[w] = I("x"); return; }
		int g = I("g");
		if (g > tm.gen) { if (net != null && net.later.Count < 64) net.later.Add((k, j)); return; }   // from the next build: kept until this PC gets there
		if (g != tm.gen) return;   // from before a restart or a page change
		switch (k)
		{
			case "pc": tmTake(I("x"), I("y")); break;
			case "cam":
				{
					var cam = tm.th.FirstOrDefault(s => s.k == "cam" && (int)s.d.a == w);
					if (cam != null) { cam.n = 0; cam.timer = tm.t; tmCamFx(); }
					var me = tm.b[tmMe]; if (me.i != w) tmSnapBody(me, D("b") + 8);
					break;
				}
			case "st": tmSnapStep(w, D("x"), D("y"), D("u")); break;
			case "sl": { var s = tm.th.FirstOrDefault(q => q.k == "slot" && (int)q.d.a == I("i") && !q.on); if (s != null) tmPlace(s); break; }
			case "rs": if (tm.restartAt < 0 && tm.doneAt < 0) { tm.restartAt = D("t"); tm.hostRs = I("h") == 1; flash = 0.3; } break;
			case "pd": if (tm.doneAt < 0 && tm.restartAt < 0) { if (tm.P.mic) tmMicWin(); else tmPageDone(D("t")); } break;
			case "tb": if (tm.snapAt < 0) tmTether(I("p")); break;
			case "sh": if (tm.spkN != null && w < tm.b.Length) tmShieldTo(w); break;   // the radio: the one who took the shield
			case "ab": if (tm.spkN != null) tmBarGone(I("s"), j["n"]?.GetValue<long>() ?? -1, I("r")); break;   // and a wave's row its shield took
		}
	}

	// ---------- every frame ----------
	static readonly HashSet<string> TN_FWD = new() { "pc", "cam", "st", "sl", "xp", "sh", "ab" };   // the host passes these on to the other one
	static void tnTick(double now)
	{
		var n = net; bool over = tm?.won == true;   // after the photo: someone leaving doesn't take the others off the win screen
		// a connection lost: the level can't go on without the three
		for (int s = 0; s < TM_N; s++)
		{
			var p = n.tp[s]; if (p == null || n.connAt[s] == 0 || !p.closed || over) continue;
			string who = n.host ? tnName(s) : tnName(0);
			if (!n.said[s]) { netDiag = p.Diag(); netEnd(n.host ? $"ما قدرنا نتصل بـ{who}، جرب مرة ثانية" : "ما قدرنا نتصل، جرب مرة ثانية", n.host, who); return; }
			netEnd(who + " طلع من الغرفة", n.host, who); return;
		}
		// connecting: hello once the line is open (25 s at most)
		for (int s = 0; s < TM_N; s++)
		{
			var p = n.tp[s]; if (p == null || n.connAt[s] == 0 || n.said[s]) continue;
			if (p.open)
			{
				n.said[s] = true;
				if (n.host) { n.hi[s] = true; tnRoster(); }
				else { n.sk[n.seat] = skin; p.Send(new JsonObject { ["k"] = "hi", ["n"] = acc?.name ?? "", ["s"] = skin, ["w"] = n.seat }.ToJsonString()); netSetPhase("lobby"); }
			}
			else if (now - n.connAt[s] > 25000) { netDiag = p.Diag(); netEnd(n.host ? $"ما قدرنا نتصل بـ{tnName(s)}، يمكن الشبكة تمنع الاتصال" : "ما قدرنا نتصل، يمكن الشبكة تمنع الاتصال", n.host, tnName(s)); return; }
		}
		if (n.host && n.phase == "wait" && Enumerable.Range(1, tnN - 1).All(s => n.said[s])) { netSetPhase("lobby"); _ = roomDel(n.code); }
		for (int s = 0; s < TM_N; s++)
		{
			var p = n.tp[s]; if (p == null) continue;
			while (p.inbox.TryDequeue(out var m))
			{
				if (!m.rel && m.d.Length > 0 && m.d[0] == 3) { if (!n.host) { n.world = m.d; n.worldNew = true; } continue; }
				if (!m.rel) { if (n.host) tnSendB(false, m.d, s); tnSnapGot(m.d, now); continue; }
				JsonNode j; try { j = JsonNode.Parse(Encoding.UTF8.GetString(m.d)); } catch (Exception) { continue; }
				string k = S(j?["k"]);
				if (k == "hi" && n.host) { n.nm[s] = S(j["n"]) ?? n.nm[s]; n.sk[s] = S(j["s"]) ?? ""; tnRoster(); }
				else if (k == "ro" && !n.host)
				{
					for (int i = 0; i < TM_N; i++) { n.nm[i] = S(j["n"]?[i]) ?? n.nm[i]; n.sk[i] = S(j["s"]?[i]) ?? ""; n.hi[i] = j["h"]?[i]?.GetValue<bool>() ?? false; }
				}
				else if (k == "ping") p.Send(new JsonObject { ["k"] = "pong", ["t"] = j["t"]?.GetValue<double>() ?? 0 }.ToJsonString());
				else if (k == "pong") { double r = now - (j["t"]?.GetValue<double>() ?? now); n.rt[s] = n.rt[s] < 0 ? r : n.rt[s] * 0.7 + r * 0.3; }
				else if (k == "go" && !n.host) { n.tre = j["re"]?.GetValue<bool>() ?? false; tnStart(now + (j["d"]?.GetValue<double>() ?? NET_GO)); }
				else if (k == "bye") { if (over) continue; string who = S(j["n"]) ?? tnName(s); netEnd(who + " طلع من الغرفة", n.host, who); return; }
				else { if (n.host && TN_FWD.Contains(k)) tnSendB(true, m.d, s); tnEvent(k, j); }
			}
		}
		if (net != n) return;
		if (tm != null && n.later.Count > 0) { var l = n.later.ToList(); n.later.Clear(); foreach (var (k, j) in l) tnEvent(k, j); }
		// the host measures each line (for the start's timing)
		if (n.host && n.phase is "wait" or "lobby")
			for (int s = 1; s < TM_N; s++) if (n.said[s] && now - n.pingT[s] > 1000) { n.pingT[s] = now; n.tp[s].Send(new JsonObject { ["k"] = "ping", ["t"] = now }.ToJsonString()); }
		if (tm != null && tmMe >= 0 && state is "tmPlay" or "tmPause" or "tmSet" && now - n.sendAt >= tnEvery) { n.sendAt = now; tnSnapSend(); if (n.host) tnWorldSend(); }
	}

	// ---------- the start ----------
	static bool tnReady => net != null && net.hi.Take(tnN).All(h => h);
	static void tnGo(bool re = false)
	{
		var n = net; if (n == null || !n.host || !tnReady) return; n.tre = re;
		for (int s = 1; s < tnN; s++) n.tp[s].Send(new JsonObject { ["k"] = "go", ["d"] = NET_GO - Math.Max(0, n.rt[s]) / 2, ["re"] = re }.ToJsonString());
		tnStart(performance.now() + NET_GO);
	}
	// v49 (his words 00:05 10-25, when they get stuck): the room's maker, from the pause, sends everyone back to the page's start
	// (the last save, like a wipe; a fall for each) or starts the whole level again (everything back to nothing, S possible again)
	static void tnHostRs()
	{
		if (tm == null || tm.won || tm.restartAt >= 0 || tm.doneAt >= 0) return;
		tm.restartAt = tm.t + 0.9; tm.hostRs = true; flash = 0.3; tnTell("rs", new() { ["t"] = tm.restartAt, ["h"] = 1 });
	}
	static void tnStart(double goAt)
	{
		var n = net; n.goAt = goAt; foreach (var l in n.ts) l.Clear(); n.hist.Clear(); n.later.Clear(); n.world = null;
		netSetPhase("play"); state = "tmIntro"; stopMenu(); ensureAudio(); particles = new List<Particle>(); tmStopMusic(); tm = null; hostAsk = 0;
	}
	static void tnBegin()
	{
		var n = net; tmMe = tmView = n.seat; tmStopMusic(); tm = null;
		tmStart(n.L.team, Math.Clamp(tnPage0, 0, n.L.team.pages.Count - 1));
		for (int i = 0; i < TM_N; i++) { tm.names[i] = tnName(i); tm.skins[i] = n.sk[i] ?? ""; }
		foreach (var l in n.ts) l.Clear();   // a restart: the squares from before it (sent during the countdown) are not drawn
		if (n.tre) { tm.msg = n.host ? "عدت المرحلة للكل" : tnName(0) + " عاد المرحلة للكل"; tm.msgT = 2.6; n.tre = false; }
		state = "tmPlay"; tmRecStart();   // v46: what this screen shows, for "احفظ الريبلاي" (Plat/TeamRp.cs)
	}

	// ---------- the level on this PC ----------
	static readonly string[] TM_PAUSE = { "أكمل اللعب", "الإعدادات", "اطلع من الغرفة" };
	static readonly string[] TM_PAUSE46 = { "أكمل اللعب", "الإعدادات", "احفظ الريبلاي", "اطلع من الغرفة" };   // v46: save only (watching would leave the others, his words 19:09 10-22)
	static readonly string[] TM_PAUSE_H = { "أكمل اللعب", RE_SAVE_ALL, RE_ALL, "الإعدادات", "احفظ الريبلاي", "اطلع من الغرفة" };   // v49: the room's maker
	public static string[] tmPauseList() => classicUi ? TM_PAUSE : net?.host == true ? TM_PAUSE_H : TM_PAUSE46;
	static bool tnStep(Inp inp, double now)
	{
		var n = net; if (n == null || !n.team) { tmStopMusic(); tm = null; tmMe = tmView = -1; state = "pltList"; return true; }
		if (state == "tmIntro") { if (now >= n.goAt) tnBegin(); return true; }
		if (tm == null) return true;
		bool play = state == "tmPlay", opened = false;
		if (play && tm.won) { tmRpKey(inp); if (inp.back || (inp.confirm && tm.t - tm.wonAt > 7.3)) { netEnd(null, true); return true; } }
		else if (play && inp.pz) { state = "tmPause"; pauseSel = 0; hostAsk = 0; play = false; }   // online: the others keep going (so does this square, standing)
		else if (play) { if (inp.jump) tm.jumpQ = true; if (inp.pdash) tm.dashQ = true; tm.jumpHeld = inp.jumpHeld; }
		else if (state == "tmPause" && hostAskStep(inp, a => { if (a == 1) { tnHostRs(); state = "tmPlay"; } else tnGo(true); })) { }
		else if (state == "tmPause")
		{
			var tl = tmPauseList(); int pn = tl.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
			string it = tl[Math.Clamp(pauseSel, 0, pn - 1)];
			if (inp.pz || inp.back || (inp.confirm && pauseSel == 0)) state = "tmPlay";
			else if (inp.confirm && it == RE_SAVE_ALL) hostAsk = 1;
			else if (inp.confirm && it == RE_ALL) hostAsk = 2;
			else if (inp.confirm && it == "الإعدادات") { state = "tmSet"; setSel = 0; opened = true; }
			else if (inp.confirm && it == "احفظ الريبلاي") tmRpSave("mid");
			else if (inp.confirm) { netEnd(null, true); return true; }
		}
		if (!play) { tm.jumpQ = tm.dashQ = tm.jumpHeld = false; }
		tnRun(play ? inp : null, now);
		return state != "tmSet" || opened;   // the settings screen reads its own keys, from the next frame (the press that opened it must not press its first row too)
	}
	// the fixed steps up to the music's time (like the platformer's), my buttons on the last step of this frame
	static void tnRun(Inp inp, double now)
	{
		int k = 0;
		while (tm != null && k < 2000)
		{
			double target = actx.currentTime - tm.startWhen;
			double dt = TN_DT;
			if (target - tm.t > SIM_MAX) { tm.n += (long)Math.Floor((target - SIM_MAX - tm.t) / dt); tm.t = -0.12 + tm.n * dt; }
			if (-0.12 + (tm.n + 1) * dt > target + 1e-9) break;
			tm.n++; k++; bool nw = -0.12 + (tm.n + 1) * dt > target + 1e-9;
			var ins = new TIn[TM_N];
			if (inp != null) ins[tmMe] = new TIn { x = nw ? inp.x : tm.inX, y = nw ? inp.y : 0, jump = nw && tm.jumpQ, jumpHeld = tm.jumpHeld, dash = nw && tm.dashQ };
			if (nw) tm.jumpQ = tm.dashQ = false;
			if (tnBot != null) ins[tmMe] = tnBot();
			tm.t = -0.12 + (tm.n - 1) * dt;
			tmStep(dt, ins); tmRecTick(dt);
			if (net != null && tm != null) { var b = tm.b[tmMe]; net.hist.Add((now, b.x, b.y)); if (net.hist.Count > 600) net.hist.RemoveRange(0, 300); }
		}
		if (tm != null) tm.inX = inp?.x ?? 0;
	}

	// the photo is whole: my letter counts like the first level's (by my own falls), and the others see my XP on my card
	static void tnWon()
	{
		var n = net; if (n == null || tm.saved) return; tm.saved = true;
		lastRank = tmRank(tmMe); lastEz = false;
		var lv = n.L.lv; double p0 = ptsOf(false); xpMark();
		plSaveBest(lv, lastRank, tm.playT);   // the team's time
		lastGain = ptsOf(false) - p0; awardXp();
		tm.xpGot[tmMe] = lastXp; tnTell("xp", new() { ["x"] = lastXp });
	}
}
