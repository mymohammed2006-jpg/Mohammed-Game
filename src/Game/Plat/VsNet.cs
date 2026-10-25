using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// Versus for 2 to 4 players (his words 20:37-20:46 10-22). One makes the room and up to three come in with its code. Like the co-op
// level for three, the maker's PC is in the middle: it has a line to each one and passes on what each one sends to the others.
// Everyone plays the same solo level at the same moment and sees the others as see-through squares with their names. Reaching the
// gate doesn't end it: the ones through watch the others, and the match ends as soon as only ONE player hasn't reached the gate.
// That one lost and all the others won (2, 3 or 4 players); the end screen shows the places and the times. Leaving before the
// gate counts as lost. No letter, points or XP, and nothing goes on the board. Versus opens only once the level was finished alone.
// The room: rooms/{code} with three offers o, o2, o3 (seats g / gn / a, g2 / gn2 / a2, g3 / gn3 / a3). States: pltRoom, pltIntro,
// pltPlay (and its pause), pltVsWait (through the gate, watching), pltVsEnd (the places).
public static partial class G
{
	public const int VS_N = 4;
	const double VS_GRACE = 600;   // ms the maker waits once the end is reached: a gate reached at the same moment on another PC still counts
	public partial class NetRun
	{
		public bool vs, roomOpen; public int vseat; public Peer[] vp = new Peer[VS_N];   // the maker: vp[1..3]; the others: vp[0] (to the maker)
		public string[] vnm = new string[VS_N], vsk = new string[VS_N];
		public bool[] vin = new bool[VS_N], vsaid = new bool[VS_N], vplay = new bool[VS_N], vleft = new bool[VS_N], vwon = new bool[VS_N];
		public double[] vfin = { -1, -1, -1, -1 }, vrt = { -1, -1, -1, -1 }, vconnAt = new double[VS_N], vping = new double[VS_N];
		public List<Snap>[] vsn = { new(), new(), new(), new() }; public Player[] vgh = new Player[VS_N];
		public int[] vres;                 // the end: the seats in place order
		public double vendAt = -1, madeAt;  // the maker: when the end was reached (it is sent VS_GRACE later); when the room was made
	}
	static readonly string VS_LOCK = "خلّص المرحلة لحالك أول عشان يفتح لك وضع الضد";
	static string vsName(int s) => net?.vnm[s] is { Length: > 0 } x ? x : "";
	static int vsGuests(NetRun n) => Enumerable.Range(1, VS_N - 1).Count(s => n.vin[s]);
	static IEnumerable<int> vsOthers(NetRun n) => Enumerable.Range(0, VS_N).Where(s => s != n.vseat && n.vplay[s]);

	// ---------- the room ----------
	static async void vsHost(PlatLevel L)
	{
		var n = net = new NetRun { host = true, vs = true, vseat = 0, L = L }; for (int s = 1; s < VS_N; s++) n.vp[s] = new Peer();
		n.vnm[0] = acc?.name ?? ""; n.vsk[0] = skin; n.vin[0] = true; netSetPhase("make"); state = "pltRoom";
		try
		{
			var os = Enumerable.Range(1, VS_N - 1).Select(s => n.vp[s].Offer()).ToArray(); await Task.WhenAll(os); if (net != n) return;
			string code = await roomMake(L.id, os[0].Result, os[1].Result, os[2].Result); if (net != n) { if (code != null) _ = roomDel(code); return; }
			if (code == null) { netEnd("ما قدرت أسوي غرفة، جرب مرة ثانية"); return; }
			n.code = code; n.roomOpen = true; n.madeAt = performance.now(); netSetPhase("wait");
			// the answers, until the match starts or the three seats are taken (the room stays 10 minutes)
			while (net == n && n.roomOpen)
			{
				await Task.Delay(1500); if (net != n || !n.roomOpen) return;
				var d = await roomGet(code); if (net != n || !n.roomOpen) return;
				if (d == null) { n.roomOpen = false; if (vsGuests(n) == 0) netEnd("انتهت الغرفة"); return; }
				for (int s = 1; s < VS_N; s++)
				{
					string a = d.A(s); if (a == null || n.vconnAt[s] != 0) continue;
					n.vnm[s] = d.GN(s) ?? "";
					if (!n.vp[s].Answer(a)) { n.vconnAt[s] = -1; n.vp[s].Close(); continue; }   // that one sees "couldn't connect"
					n.vconnAt[s] = performance.now();
				}
				if (Enumerable.Range(1, VS_N - 1).All(s => n.vconnAt[s] != 0)) { n.roomOpen = false; _ = roomDel(code); return; }   // full
				if (performance.now() - n.madeAt > 600000) { n.roomOpen = false; _ = roomDel(code); if (vsGuests(n) == 0) netEnd("انتهت الغرفة"); return; }
			}
		}
		catch (Exception) { if (net == n) { if (vsGuests(n) == 0) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); else n.roomOpen = false; } }
	}
	// the first free seat; if someone takes it at the same moment (the rules refuse), the next one
	static async void vsJoin(NetRun n, RoomDoc d)
	{
		try
		{
			n.vs = true; n.vnm[0] = d.hn ?? ""; n.L = PLATS.FirstOrDefault(p => p.id == d.lv && !p.coop);
			if (n.L == null) { netEnd("حدّث اللعبة عشان تلعب هذي المرحلة"); return; }
			if (d.h == acc.uid) { netEnd("هذي غرفتك أنت"); return; }
			if (n.L.lv.best == null) { netEnd(VS_LOCK); return; }
			if (d.o2 == null) { netEnd($"{d.hn} لازم يحدّث اللعبة عشان تلعبون ضد"); return; }   // a room from v45 (two players only)
			for (int tries = 0; tries < 4; tries++)
			{
				if (d.g == acc.uid || d.g2 == acc.uid || d.g3 == acc.uid) { netEnd("أنت داخل هذي الغرفة من قبل"); return; }
				int seat = Enumerable.Range(1, VS_N - 1).FirstOrDefault(s => d.O(s) != null && d.A(s) == null);
				if (seat == 0) { netEnd("الغرفة فيها ٤ لاعبين"); return; }
				var p = new Peer(); string ans = await p.Join(d.O(seat)); if (net != n) { p.Close(); return; }
				if (ans == null) { p.Close(); netEnd("صار خطأ بالاتصال، جرب مرة ثانية"); return; }
				try { await roomAnswer(n.code, ans, seat); }
				catch (FetchErr e) when (e.code == 403) { p.Close(); if (net != n) return; d = await roomGet(n.code); if (net != n) return; if (d == null) { netEnd("انتهت الغرفة"); return; } continue; }
				if (net != n) { p.Close(); return; }
				n.vseat = seat; n.vp[0] = p; n.peer = p; n.vin[0] = true; n.vconnAt[0] = performance.now(); netSetPhase("conn");
				return;
			}
			netEnd("الغرفة فيها ٤ لاعبين");
		}
		catch (Exception) { if (net == n) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); }
	}

	// ---------- sending ----------
	static void vsSendB(bool rel, byte[] d, int skip = -1) { var n = net; if (n == null) return; for (int s = 0; s < VS_N; s++) if (s != skip && n.vconnAt[s] > 0) n.vp[s]?.Send(rel, d); }
	static void vsSend(JsonObject o, int skip = -1) => vsSendB(true, Encoding.UTF8.GetBytes(o.ToJsonString()), skip);
	static JsonArray jArr<T>(IEnumerable<T> xs) => new JsonArray(xs.Select(x => JsonValue.Create(x)).Cast<JsonNode>().ToArray());
	// the maker tells everyone who is in the room
	static void vsRoster() { var n = net; vsSend(new JsonObject { ["k"] = "ro", ["n"] = jArr(n.vnm.Select(x => x ?? "")), ["s"] = jArr(n.vsk.Select(x => x ?? "")), ["i"] = jArr(n.vin) }); }

	// ---------- every frame ----------
	static void vsTick(double now)
	{
		var n = net;
		for (int s = 0; s < VS_N; s++)
		{
			var p = n.vp[s]; if (p == null || n.vconnAt[s] <= 0) continue;
			if (p.closed)
			{
				if (!n.host) { if (!n.vsaid[0]) { netDiag = p.Diag(); netEnd("ما قدرنا نتصل، جرب مرة ثانية"); } else netEnd(vsName(0) + " طلع من الغرفة"); return; }
				vsGone(s); if (net != n) return; continue;
			}
			if (n.vsaid[s]) continue;
			if (p.open)
			{
				n.vsaid[s] = true;
				if (!n.host) { p.Send(new JsonObject { ["k"] = "hi", ["n"] = acc?.name ?? "", ["s"] = skin, ["w"] = n.vseat, ["v"] = 2 }.ToJsonString()); netSetPhase("lobby"); }
			}
			else if (now - n.vconnAt[s] > 25000)
			{
				if (!n.host) { netDiag = p.Diag(); netEnd("ما قدرنا نتصل، يمكن الشبكة تمنع الاتصال"); return; }
				p.Close(); n.vconnAt[s] = -1;   // the maker: only that seat is lost
			}
		}
		for (int s = 0; s < VS_N; s++)
		{
			var p = n.vp[s]; if (p == null) continue;
			while (p.inbox.TryDequeue(out var m))
			{
				if (!m.rel) { if (n.host) vsSendB(false, m.d, s); vsSnapGot(m.d, now); continue; }
				JsonNode j; try { j = JsonNode.Parse(Encoding.UTF8.GetString(m.d)); } catch (Exception) { continue; }
				string k = S(j?["k"]); int w = j?["w"] is JsonValue wv && wv.TryGetValue<int>(out int wi) ? wi : -1;
				if (k == "hi" && n.host)
				{
					n.vnm[s] = S(j["n"]) ?? n.vnm[s]; n.vsk[s] = S(j["s"]) ?? ""; n.vin[s] = true;
					if (n.phase == "wait") netSetPhase("lobby");
					vsRoster();
				}
				else if (k == "ro" && !n.host) for (int i = 0; i < VS_N; i++) { n.vnm[i] = S(j["n"]?[i]) ?? n.vnm[i]; n.vsk[i] = S(j["s"]?[i]) ?? ""; n.vin[i] = j["i"]?[i]?.GetValue<bool>() ?? false; }
				else if (k == "ping") p.Send(new JsonObject { ["k"] = "pong", ["t"] = j["t"]?.GetValue<double>() ?? 0 }.ToJsonString());
				else if (k == "pong") { double r = now - (j["t"]?.GetValue<double>() ?? now); n.vrt[s] = n.vrt[s] < 0 ? r : n.vrt[s] * 0.7 + r * 0.3; }
				else if (k == "go" && !n.host) vsStart(now + (j["d"]?.GetValue<double>() ?? NET_GO), Enumerable.Range(0, VS_N).Select(i => j["p"]?[i]?.GetValue<bool>() ?? false).ToArray());
				else if (k == "fin") { if (n.host) vsFin(s, j["t"]?.GetValue<double>() ?? 0); else if (w >= 0 && w < VS_N) vsFinSeen(w, j["t"]?.GetValue<double>() ?? 0); }
				else if (k == "left" && !n.host && w >= 0 && w < VS_N) { n.vleft[w] = n.vplay[w] && n.phase == "play"; n.vin[w] = false; vsLeftSeen(w); }
				else if (k == "end" && !n.host) vsEndGot(j);
				else if (k == "bye") { if (n.host) { vsGone(s); if (net != n) return; } else { netEnd(vsName(0) + " طلع من الغرفة"); return; } }
			}
		}
		if (net != n) return;
		// the maker measures each line (for the start's timing) and sends the places a moment after the end
		if (n.host && n.phase is "wait" or "lobby" or "end")
			for (int s = 1; s < VS_N; s++) if (n.vsaid[s] && n.vconnAt[s] > 0 && now - n.vping[s] > 1000) { n.vping[s] = now; n.vp[s].Send(new JsonObject { ["k"] = "ping", ["t"] = now }.ToJsonString()); }
		if (n.host && n.phase == "play" && n.vendAt >= 0 && now - n.vendAt >= VS_GRACE) vsFinal();
		// where my square is, 30 times a second (seat in byte 1; the maker passes them on)
		if (n.phase == "play" && player != null && pl != null && now - n.sendAt >= 33)
		{
			n.sendAt = now; var q = player; var b = new byte[47]; int o = 0;
			void F(double v) { BitConverter.TryWriteBytes(b.AsSpan(o), (float)v); o += 4; }
			b[o++] = 1; b[o++] = (byte)n.vseat; F(pl.t); F(q.x); F(q.y); F(q.sa ?? 0); F(q.sk ?? 0); F(q.ddx); F(q.ddy); F(q.dash); F(q.fx); b[o++] = (byte)Math.Max(0, q.hp);
			b[o++] = (byte)((state == "pltDead" ? 1 : 0) | (state == "pltPause" || state == "pltSet" ? 2 : 0) | (q.hurt > 0 && Math.Floor(q.hurt * 18) % 2 != 0 ? 4 : 0) | (state == "pltVsWait" ? 32 : 0));
			vsSendB(false, b.AsSpan(0, o).ToArray());
		}
	}
	static void vsSnapGot(byte[] d, double now)
	{
		if (d.Length < 40 || d[0] != 1 || net == null) return; int seat = d[1]; if (seat >= VS_N || seat == net.vseat) return; int o = 2;
		double F() { double v = BitConverter.ToSingle(d, o); o += 4; return v; }
		var s = new Snap { at = now }; F(); s.x = F(); s.y = F(); s.sa = F(); s.sk = F(); s.ddx = F(); s.ddy = F(); s.dash = F(); s.fx = F(); s.hp = d[o++]; s.fl = d[o++];
		var L = net.vsn[seat]; L.Add(s); if (L.Count > 40) L.RemoveAt(0);
	}
	// someone's line closed or they said bye (the maker)
	static void vsGone(int s)
	{
		var n = net; string who = vsName(s); bool was = n.vin[s];
		n.vp[s]?.Close(); n.vconnAt[s] = -1; n.vin[s] = false;
		if (!was) return;
		bool inMatch = n.phase == "play" && n.vplay[s];
		if (inMatch) n.vleft[s] = true;
		vsSend(new JsonObject { ["k"] = "left", ["w"] = s }); vsRoster(); vsLeftSeen(s);
		if (inMatch) vsCheckEnd();
		else if (vsGuests(n) == 0 && n.phase == "lobby") { if (n.roomOpen) netSetPhase("wait"); else netEnd(who + " طلع من الغرفة"); }
	}
	static void vsLeftSeen(int s)
	{
		if (pl == null || net == null) return;
		if (net.phase == "play") { pl.msg = vsName(s) + " طلع من المباراة"; pl.msgT = 2.5; }
	}

	// ---------- the match ----------
	static bool vsCanGo => net != null && net.host && net.phase is "lobby" or "end" && vsGuests(net) > 0;
	static void vsGo()
	{
		var n = net; if (!vsCanGo) return;
		if (n.roomOpen) { n.roomOpen = false; _ = roomDel(n.code); }   // nobody new comes in once it starts
		for (int s = 1; s < VS_N; s++) if (n.vconnAt[s] >= 0 && !n.vin[s] && n.vp[s] != null && !n.vsaid[s]) { n.vp[s].Close(); n.vconnAt[s] = -1; }
		var play = n.vin.ToArray();
		for (int s = 1; s < VS_N; s++) if (n.vin[s]) n.vp[s].Send(new JsonObject { ["k"] = "go", ["d"] = NET_GO - Math.Max(0, n.vrt[s]) / 2, ["p"] = jArr(play) }.ToJsonString());
		vsStart(performance.now() + NET_GO, play);
	}
	static void vsStart(double goAt, bool[] play)
	{
		var n = net; n.vplay = play; n.vfin = new double[] { -1, -1, -1, -1 }; n.vleft = new bool[VS_N]; n.vwon = new bool[VS_N]; n.vres = null; n.vendAt = -1;
		foreach (var l in n.vsn) l.Clear(); n.vgh = new Player[VS_N]; n.myFin = n.otherFin = -1; n.iWon = false; n.snaps.Clear(); n.ghost = null;
		int o = vsOthers(n).DefaultIfEmpty(-1).First(); n.other = o >= 0 ? vsName(o) : ""; n.otherSk = o >= 0 ? n.vsk[o] ?? "" : "";   // the PC replay keeps one other square (the first)
		plStart(n.L, true); pl.introAt = goAt - PL_INTRO; netSetPhase("play");
	}
	// through the gate: this one watches the others until the end
	static void vsMeFin()
	{
		var n = net; double t = tRound(pl.t); n.myFin = t; pl.atGate = true; pl.msgT = 0; state = "pltVsWait";
		burst(player.x, player.y, CYAN, 40, 450); shake = 8; rumble(140, 0.6, 0.4);
		sfx(tt => { sTone(tt, 784, 0.15, "triangle", 0.12); sTone(tt + 0.1, 1047, 0.2, "triangle", 0.1); });
		if (n.host) vsFin(0, t); else { n.vfin[n.vseat] = t; n.peer.Send(new JsonObject { ["k"] = "fin", ["t"] = t }.ToJsonString()); }
	}
	// the maker: seat s reached the gate at music time t
	static void vsFin(int s, double t)
	{
		var n = net; if (n.phase != "play" || !n.vplay[s] || n.vleft[s] || n.vfin[s] >= 0) return;
		n.vfin[s] = tRound(t); vsSend(new JsonObject { ["k"] = "fin", ["w"] = s, ["t"] = n.vfin[s] });
		if (s != 0) vsFinSeen(s, t);
		vsCheckEnd();
	}
	static void vsFinSeen(int s, double t)
	{
		var n = net; if (n.phase != "play") return; n.vfin[s] = tRound(t);
		if (pl != null && s != n.vseat) { pl.msg = $"{vsName(s)} وصل البوابة!"; pl.msgT = 2.5; }
	}
	// the end: only one left who hasn't reached the gate (or nobody else is still in)
	static void vsCheckEnd()
	{
		var n = net; if (n.vendAt >= 0) return;
		var active = Enumerable.Range(0, VS_N).Where(s => n.vplay[s] && !n.vleft[s]).ToList();
		if (active.Count <= 1 || active.Count(s => n.vfin[s] < 0) <= 1) n.vendAt = performance.now();
	}
	// the maker: the places (through the gate by time, then the one who didn't make it, then the ones who left)
	static void vsFinal()
	{
		var n = net; var play = Enumerable.Range(0, VS_N).Where(s => n.vplay[s]).ToList();
		var active = play.Where(s => !n.vleft[s]).ToList();
		var fin = play.Where(s => n.vfin[s] >= 0).OrderBy(s => n.vfin[s]).ThenBy(s => s).ToList();
		var notIn = active.Where(s => n.vfin[s] < 0).ToList(); var gone = play.Where(s => n.vleft[s] && n.vfin[s] < 0).ToList();
		var won = new bool[VS_N]; foreach (var s in fin) won[s] = true;
		if (active.Count <= 1) foreach (var s in active) won[s] = true;   // the others left: the one still in wins
		else if (notIn.Count == 0 && fin.Count >= 2) won[fin.Last()] = false;   // everyone got there at about the same moment: the slowest is the last
		var order = fin.Concat(notIn.Where(s => won[s])).Concat(notIn.Where(s => !won[s])).Concat(gone).ToArray();
		var j = new JsonObject { ["k"] = "end", ["o"] = jArr(order), ["wn"] = jArr(won), ["f"] = jArr(n.vfin), ["l"] = jArr(n.vleft) };
		vsSend(j); vsEndGot(j);
	}
	static void vsEndGot(JsonNode j)
	{
		var n = net; if (n == null || n.phase != "play") return;
		n.vres = (j["o"] as JsonArray)?.Select(x => x?.GetValue<int>() ?? 0).Where(s => s >= 0 && s < VS_N).ToArray() ?? Array.Empty<int>();
		for (int i = 0; i < VS_N; i++) { n.vwon[i] = j["wn"]?[i]?.GetValue<bool>() ?? false; n.vfin[i] = j["f"]?[i]?.GetValue<double>() ?? -1; n.vleft[i] = j["l"]?[i]?.GetValue<bool>() ?? false; }
		n.iWon = n.vwon[n.vseat]; n.myFin = n.vfin[n.vseat];
		netFinish();
	}

	// the others' squares, a little in the past so they move smoothly between the messages
	static Player vsGhost(int seat, double now)
	{
		var n = net; var L = n?.vsn[seat]; if (L == null || L.Count == 0) return null;
		double t = now - 100; Snap a = L[0], b = a;
		for (int i = 0; i < L.Count; i++) { if (L[i].at <= t) a = L[i]; else { b = L[i]; break; } if (i == L.Count - 1) b = a; }
		double f = b.at > a.at ? clamp((t - a.at) / (b.at - a.at), 0, 1) : 1;
		var gp = n.vgh[seat] ??= new Player { r = 5 };
		gp.x = a.x + (b.x - a.x) * f; gp.y = a.y + (b.y - a.y) * f; gp.sa = b.sa; gp.sk = b.sk; gp.ddx = b.ddx; gp.ddy = b.ddy; gp.dash = b.dash; gp.fx = b.fx; gp.hp = b.hp;
		gp.hurt = (b.fl & 4) != 0 ? 0.03 : 0;
		return (b.fl & 1) != 0 || (b.fl & 32) != 0 ? null : gp;   // dead, or through the gate
	}
	static void drawVsGhosts(double now)
	{
		var n = net; if (n == null) return;
		foreach (int s in vsOthers(n))
		{
			if (n.vleft[s]) continue; var gp = vsGhost(s, now); if (gp == null) continue;
			tinted(dupHue(n.vsk, s), () => drawPlayer(gp, n.vsk[s] ?? "", 0.55));
			g.globalAlpha = 0.7; txt(vsName(s), gp.x, gp.y - 30, 18, "#fff", 700); g.globalAlpha = 1;
		}
	}

	// ---------- screens ----------
	static bool stepVs(Inp inp, double now, double dt)
	{
		var n = net;
		if (state == "pltRoom") { if (inp.confirm && vsCanGo && n.phase == "lobby") vsGo(); else if (inp.back || inp.pz) netEnd(null, true); }
		else if (state == "pltVsWait") { pl.msgT -= dt; if (inp.back) netEnd(null, true); }
		else if (state == "pltVsEnd")
		{
			if (pl != null) plRpKey(inp, n.endAt, n.iWon ? "vsWin" : "vsLose", null);
			if (inp.confirm && vsCanGo && performance.now() - n.endAt > 800) vsGo();
			else if (inp.back) netEnd(null, true);
		}
		else return false;
		return true;
	}
	// the room: the code, then up to four squares with their names
	static void drawVsRoom(double now)
	{
		var n = net; string ph = n.phase, dots = new string('.', 1 + (int)Math.Floor(now / 400) % 3);
		if (n.host && (ph == "make" || n.roomOpen))
		{
			txt("غرفتك", W / 2, 95, 48, "#fff");
			txt("عط أصحابك هذا الكود يدخلونه من: البلاتفورم ← أونلاين ← ادخل بكود", W / 2, 150, 24, "rgba(255,255,255,0.7)", 500);
			if (n.roomOpen) codeTxt(n.code, W / 2, 245, 100, RED); else txt("أسوي الغرفة" + dots, W / 2, 245, 36, "rgba(255,255,255,0.7)", 700);
		}
		else if (!n.host && ph is "join" or "conn") { txt("أدخل الغرفة", W / 2, 105, 48, "#fff"); txt("جاري الاتصال" + dots, W / 2, 245, 36, "rgba(255,255,255,0.75)", 700); }
		else { txt("جاهزين؟", W / 2, 95, 48, "#fff"); txt(n.L?.name ?? "", W / 2, 155, 28, RED3, 700); }
		bool connecting = !n.host && ph is "join" or "conn";
		for (int i = 0; i < VS_N && !connecting; i++)
		{
			double x = W / 2 + (1.5 - i) * 250, y = 430; bool in_ = n.vin[i]; string sk = n.vsk[i] ?? "";
			if (in_ && dupHue(n.vsk, i) >= 0) g.setTint(dupHue(n.vsk, i));
			g.save(); g.translate(x, y - 30); g.scale(3, 3); g.rotate(Math.Sin(now / 600 + i) * 0.15); g.globalAlpha = in_ ? 1 : 0.2;
			if (!in_ || string.IsNullOrEmpty(sk)) { g.shadowColor = CYAN; g.shadowBlur = 14; g.fillStyle = CYAN; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = ARENA; g.fillRect(-3, -3, 6, 6); }
			else skinBody(sk, 1, now);
			g.restore(); g.clearTint(); g.globalAlpha = 1;
			txt(in_ ? vsName(i) : "أستنى" + dots, x, y + 70, 28, in_ ? "#fff" : "rgba(255,255,255,0.4)", 800);
			if (i == n.vseat && in_) txt("أنت", x, y + 106, 22, CYAN, 700);
		}
		if (!connecting) txt("من ٢ إلى ٤ لاعبين", W / 2, 560, 22, "rgba(255,255,255,0.5)", 500);
		if (vsCanGo && ph == "lobby") row(new[] { (BTN[kind].confirm, "ابدأ") }, W / 2, 625, 38, "#fff", 30);
		else if (n.host && !connecting && ph != "make") txt("أستنى أصحابك يدخلون" + dots, W / 2, 625, 28, "rgba(255,255,255,0.75)", 700);
		else if (!n.host && ph == "lobby") txt($"أستنى {vsName(0)} يبدأ" + dots, W / 2, 625, 28, "rgba(255,255,255,0.75)", 700);
		row(new[] { (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// through the gate: the level goes on behind, with who is through so far
	static void drawVsWait(double now)
	{
		var n = net; if (n == null) return;
		int place = Enumerable.Range(0, VS_N).Count(s => n.vplay[s] && n.vfin[s] >= 0 && (n.vfin[s] < n.myFin || (n.vfin[s] == n.myFin && s < n.vseat))) + 1;
		g.fillStyle = "rgba(8,6,16,0.55)"; g.fillRect(0, H - 150, W, 150);
		txt($"وصلت! المركز {ar(place)}   •   وقتك {tFmt(n.myFin)}", W / 2, H - 110, 32, NGOLD, 800);
		var left = vsOthers(n).Where(s => n.vfin[s] < 0 && !n.vleft[s]).Select(vsName).ToList();
		string dots = new string('.', 1 + (int)Math.Floor(now / 400) % 3);
		txt(left.Count > 0 ? "أستنى الباقين: " + string.Join("، ", left) + dots : "أستنى النتيجة" + dots, W / 2, H - 66, 24, "rgba(255,255,255,0.75)", 600);
		row(new[] { (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H - 26, 20, "rgba(255,255,255,0.55)", 16);
	}
	// the end: won or lost, then the places with each one's time
	static void drawVsEndN(double now)
	{
		var n = net; overlay(0.78); double ts = now - n.endAt, e = clamp(ts / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3);
		g.save(); g.translate(W / 2, 120); g.scale(sc, sc); g.globalAlpha = e;
		if (n.iWon) txt("فزت!", 0, 0, 96, NGOLD); else txt("خسرت!", 0, 0, 90, PINK);
		g.restore(); g.globalAlpha = 1;
		var res = n.vres ?? Array.Empty<int>(); double rh = 64, top = 250 - (res.Length - 2) * rh / 4;
		for (int i = 0; i < res.Length; i++)
		{
			int s = res[i]; double y = top + i * rh, a = clamp((ts - 250 - i * 120) / 200, 0, 1); if (a <= 0) continue;
			g.globalAlpha = a; bool me = s == n.vseat;
			rr(W / 2 - 330, y - rh / 2 + 5, 660, rh - 10, 14); g.fillStyle = me ? $"rgba({RGB},0.16)" : "rgba(255,255,255,0.05)"; g.fill();
			if (me) { g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			string pc = !n.vwon[s] ? PINK : i == 0 ? "#ffd54a" : i == 1 ? "#d8dde6" : "#e0a46a";
			txt(ar(i + 1), W / 2 + 295, y + 1, 30, pc, 800);
			txt(vsName(s), W / 2 + 250, y + 1, 28, "#fff", me ? 800 : 600, "right");
			txt(n.vfin[s] >= 0 ? tFmt(n.vfin[s]) : n.vleft[s] ? "طلع" : "ما وصل", W / 2 - 60, y + 1, 26, "#e9e6f5", 600);
			txt(n.vwon[s] ? "فاز" : "خسر", W / 2 - 270, y + 1, 28, n.vwon[s] ? NGOLD : PINK, 800);
		}
		g.globalAlpha = 1;
		if (ts > 800)
		{
			double by = Math.Max(top + res.Length * rh + 30, 560);
			if (vsCanGo) row(new[] { (BTN[kind].confirm, "العب مرة ثانية") }, W / 2, by, 34, "#fff", 26);
			else if (n.host) txt("كل اللي معك طلعوا من الغرفة", W / 2, by, 26, "rgba(255,255,255,0.7)", 700);
			else txt($"أستنى {vsName(0)} يبدأ مرة ثانية", W / 2, by, 26, "rgba(255,255,255,0.75)", 700);
			row(pl == null ? new[] { (BTN[kind].back, "اطلع من الغرفة") } : new[] { myRpHint(n.endAt), (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H - 34);
		}
	}
}
