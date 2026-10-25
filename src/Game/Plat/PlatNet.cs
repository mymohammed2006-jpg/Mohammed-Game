using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// Online for the platformer (part 2 step 3). Two players, each at home: one makes a room (5-digit code), the other types the code.
// Versus (a solo level): both play the same level at the same moment, each sees the other as a see-through square,
// the first one through the gate wins. Each PC runs its own level and music; only where the square is goes across.
// Co-op (a level with "coop": true, exactly two players): the song pieces and the checkpoint count for both; a square that loses its
// lives falls down where it is until the other one touches it (back with full lives); both down = both back at the checkpoint;
// the level ends when both are in the open gate.
// An online match gives no letter, points or XP. Versus is now for 2 to 4 players (Plat/VsNet.cs); what is left here for it is the
// screens before the room, and the old two-player co-op.
// States: pltOn (make a room / type a code), pltJoin (the code), pltRoom (waiting / connecting / together), pltVsEnd (who won).
// The co-op level for three has its own room and play (TeamNet.cs); the screens before it are the same.
public static partial class G
{
	public class Snap { public double at, x, y, sa, sk, ddx, ddy, dash, fx; public int hp, fl; }
	public partial class NetRun
	{
		public Peer peer; public bool host; public string code = "", phase = "";   // phase: make, wait, join, conn, lobby, play, end
		public PlatLevel L; public string other = "", otherSk = ""; public bool hello;
		public double phaseAt, pingAt, rtt = -1, sendAt, endAt;
		public double myFin = -1, otherFin = -1; public bool iWon, otherLeft;
		public bool coop => L?.coop == true; public bool otherGate, coopDone; public double revAt;
		public List<Snap> snaps = new();
		public Player ghost;
	}
	public static NetRun net;
	public static int onSel = 0; static string joinCode = "";
	static string netMsg; static double netMsgAt = -1e9;   // a line on the level list (why the room ended)
	const double NET_GO = 3000;   // ms from "start" to the music (the intro)

	static bool online => net != null && net.phase is "play" or "end";
	static void netNote(string m) { netMsg = m; netMsgAt = performance.now(); }
	static string netDiag;   // a small line under a failed connection (what each side found), to tell us why
	static void netSetPhase(string p) { if (net != null) { net.phase = p; net.phaseAt = performance.now(); } }

	// ---------- making / joining ----------
	static async void netHost(PlatLevel L)
	{
		if (L.team != null) { tnHost(L); return; }
		if (!L.coop) { vsHost(L); return; }   // versus: 2 to 4 players
		var n = net = new NetRun { host = true, L = L, peer = new Peer() }; netSetPhase("make"); state = "pltRoom";
		try
		{
			string offer = await n.peer.Offer(); if (net != n) return;
			string code = await roomMake(L.id, offer); if (net != n) { if (code != null) _ = roomDel(code); return; }
			if (code == null) { netEnd("ما قدرت أسوي غرفة، جرب مرة ثانية"); return; }
			n.code = code; netSetPhase("wait");
			// wait for the answer (the room stays 10 minutes)
			while (net == n && n.phase == "wait")
			{
				await Task.Delay(1500); if (net != n) return;
				var d = await roomGet(code); if (net != n) return;
				if (d == null) { netEnd("انتهت الغرفة"); return; }
				if (d.a != null) { n.other = d.gn ?? ""; if (!n.peer.Answer(d.a)) { netEnd("صار خطأ بالاتصال، جرب مرة ثانية"); return; } netSetPhase("conn"); }
				else if (performance.now() - n.phaseAt > 600000) { _ = roomDel(code); netEnd("انتهت الغرفة"); return; }
			}
		}
		catch (Exception) { if (net == n) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); }
	}
	static async void netJoin(string code)
	{
		var n = net = new NetRun { host = false, code = code, peer = new Peer() }; netSetPhase("join"); state = "pltRoom";
		try
		{
			var d = await roomGet(code); if (net != n) return;
			if (d == null || d.o == null) { netEnd("ما لقيت غرفة بهذا الكود"); return; }
			var RL = PLATS.FirstOrDefault(p => p.id == d.lv);
			if (RL?.team != null || (RL == null && d.o2 != null && d.o3 == null)) { tnJoin(n, d); return; }   // a room for three
			if (RL != null && !RL.coop) { vsJoin(n, d); return; }   // versus: 2 to 4 players
			if (d.a != null) { netEnd("الغرفة فيها لاعبين"); return; }
			if (d.h == acc.uid) { netEnd("هذي غرفتك أنت"); return; }
			n.L = PLATS.FirstOrDefault(p => p.id == d.lv); n.other = d.hn ?? "";
			if (n.L == null) { netEnd("حدّث اللعبة عشان تلعب هذي المرحلة"); return; }
			string ans = await n.peer.Join(d.o); if (net != n) return;
			if (ans == null) { netEnd("صار خطأ بالاتصال، جرب مرة ثانية"); return; }
			await roomAnswer(code, ans); if (net != n) return;
			netSetPhase("conn");
		}
		catch (Exception) { if (net == n) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); }
	}
	public static void netEndPub() => netEnd(null);   // the tests (test/PvRun.cs)
	// leave (or the other left): back to the level list with a line saying why
	static void netEnd(string why, bool tell = false, string byeName = null)
	{
		var n = net; if (n == null) return; net = null;
		var rl = tm != null && tm.won ? ALL_LV().FirstOrDefault(x => x.id == tm.L.id) : null;   // a co-op level won: its rating card on the list (UI/Rate.cs), however the room ended
		var ps = n.all();
		if (tell) foreach (var q in ps) q.Send(new JsonObject { ["k"] = "bye", ["n"] = byeName ?? acc?.name ?? "" }.ToJsonString());
		_ = Task.Run(async () => { await Task.Delay(300); foreach (var q in ps) q.Close(); });
		if (n.host && n.code != "" && (n.phase is "make" or "wait" || n.roomOpen)) _ = roomDel(n.code);   // a failed connection keeps its room (both sides' addresses, to see why; the code is free again after 10 minutes)
		if (why != null) netNote(why);
		if (why == null || !why.StartsWith("ما قدرنا")) netDiag = null;
		if (state.StartsWith("plt") && state != "pltList") { plStopMusic(); player = null; particles = new List<Particle>(); actx.resume(); state = "pltList"; }
		if (state.StartsWith("tm")) { tmStopMusic(); tm = null; tmMe = tmView = -1; particles = new List<Particle>(); actx.resume(); state = "pltList"; }
		if (rl != null && state == "pltList") rateAsk(rl, null);
	}

	// ---------- every frame ----------
	public static void netTick(double now)
	{
		var n = net; if (n == null) return;
		if (n.team) { tnTick(now); return; }
		if (n.vs) { vsTick(now); return; }
		if (n.peer.closed && n.phase != "make" && n.phase != "wait" && n.phase != "join") { if (!n.hello) netDiag = n.peer.Diag(); netEnd(n.hello ? n.other + " طلع من الغرفة" : "ما قدرنا نتصل، جرب مرة ثانية"); return; }
		if (n.phase == "conn")
		{
			if (n.peer.open) { n.peer.Send(new JsonObject { ["k"] = "hi", ["n"] = acc?.name ?? "", ["s"] = skin, ["v"] = 1 }.ToJsonString()); netSetPhase("lobby"); if (n.host) _ = roomDel(n.code); }
			else if (now - n.phaseAt > 25000) { netDiag = n.peer.Diag(); netEnd("ما قدرنا نتصل، يمكن الشبكة تمنع الاتصال"); return; }
		}
		while (n.peer.inbox.TryDequeue(out var m))
		{
			if (!m.rel) { netSnap(m.d, now); continue; }
			JsonNode j; try { j = JsonNode.Parse(Encoding.UTF8.GetString(m.d)); } catch (Exception) { continue; }
			string k = S(j?["k"]);
			if (k == "hi") { n.hello = true; n.other = S(j["n"]) ?? n.other; n.otherSk = S(j["s"]) ?? ""; }
			else if (k == "ping") n.peer.Send(new JsonObject { ["k"] = "pong", ["t"] = j["t"]?.GetValue<double>() ?? 0 }.ToJsonString());
			else if (k == "pong") { double r = now - (j["t"]?.GetValue<double>() ?? now); n.rtt = n.rtt < 0 ? r : n.rtt * 0.7 + r * 0.3; }
			else if (k == "go" && !n.host) netStart(now + (j["d"]?.GetValue<double>() ?? NET_GO));
			else if (k == "win") netOtherWon(j["t"]?.GetValue<double>() ?? 0);
			else if (k == "bye") { netEnd(n.other + " طلع من الغرفة"); return; }
			else if (n.phase == "play" && pl != null) netCoopMsg(k, j);
		}
		if (n.phase is "lobby" or "end" && n.peer.open && now - n.pingAt > 1000) { n.pingAt = now; n.peer.Send(new JsonObject { ["k"] = "ping", ["t"] = now }.ToJsonString()); }
		// where my square is, 30 times a second
		if (n.phase == "play" && player != null && pl != null && now - n.sendAt >= 33)
		{
			n.sendAt = now; var p = player; var b = new byte[46]; int o = 0;
			void F(double v) { BitConverter.TryWriteBytes(b.AsSpan(o), (float)v); o += 4; }
			b[o++] = 1; F(pl.t); F(p.x); F(p.y); F(p.sa ?? 0); F(p.sk ?? 0); F(p.ddx); F(p.ddy); F(p.dash); F(p.fx); b[o++] = (byte)Math.Max(0, p.hp);
			b[o++] = (byte)((state == "pltDead" ? 1 : 0) | (state == "pltPause" || state == "pltSet" ? 2 : 0) | (p.hurt > 0 && Math.Floor(p.hurt * 18) % 2 != 0 ? 4 : 0) | (pl.down ? 8 : 0) | (pl.atGate ? 16 : 0));
			n.peer.Send(false, b.AsSpan(0, o).ToArray());
		}
		// co-op: out of the gate when the other one is down (nobody else can bring it back)
		if (n.phase == "play" && n.coop && pl != null && pl.atGate && n.snaps.LastOrDefault() is { } ds && (ds.fl & 8) != 0) coopGate(false);
		// co-op: touching the other square while it is down brings it back
		if (n.phase == "play" && n.coop && state == "pltPlay" && pl != null && !pl.down && player != null && now - n.revAt > 500)
		{
			var s = n.snaps.LastOrDefault();
			if (s != null && (s.fl & 8) != 0 && hypot(player.x - s.x, player.y - s.y) < 30) { n.revAt = now; n.peer.Send("{\"k\":\"rev\"}"); burst(s.x, s.y, CYAN, 24, 300); }
		}
	}
	static void netSnap(byte[] d, double now)
	{
		if (d.Length < 39 || d[0] != 1 || net == null) return; int o = 1;
		double F() { double v = BitConverter.ToSingle(d, o); o += 4; return v; }
		var s = new Snap { at = now }; F(); s.x = F(); s.y = F(); s.sa = F(); s.sk = F(); s.ddx = F(); s.ddy = F(); s.dash = F(); s.fx = F(); s.hp = d[o++]; s.fl = d[o++];
		net.snaps.Add(s); if (net.snaps.Count > 40) net.snaps.RemoveAt(0);
	}

	// ---------- co-op ----------
	static void netTell(JsonObject o) => net?.peer.Send(o.ToJsonString());
	static void netCoopMsg(string k, JsonNode j)
	{
		var n = net; int I(string f) => j[f]?.GetValue<int>() ?? -1;
		if (k == "pc")
		{
			// the other one took a piece: it counts here too
			int tx = I("x"), ty = I("y"); string key = tx + "," + ty;
			if (ty >= 0 && ty < pl.h && tx >= 0 && tx < pl.w && !pl.got.Contains(key)) { char c = pl.m[ty][tx]; plTake(key, c, tx, ty, true); if (!plOpen()) pl.msg = n.other + " جاب " + pl.msg; }
		}
		else if (k == "cp") { pl.cx = I("x") * PT + PT / 2; pl.cy = (I("y") + 1) * PT - P_HALF; pl.hasCp = true; cpMsg = 1.8; }
		else if (k == "rev" && pl.down)
		{
			pl.down = false; player.hp = P_HP; player.inv = 1.5; player.hurt = 0; pl.vx = pl.vy = 0;
			burst(player.x, player.y, CYAN, 30, 380); pl.msg = n.other + " رجّعك!"; pl.msgT = 2;
			sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.2, "triangle", 0.1); });
		}
		else if (k == "wipe") coopWipe();
		else if (k == "gate") { n.otherGate = true; if (pl.atGate) coopWin(); }
		else if (k == "ungate") n.otherGate = false;
	}
	// my lives ran out in co-op: down where I am (both down = both back at the checkpoint)
	static void coopDown()
	{
		pl.down = true; pl.vx = pl.vy = 0; player.dash = 0;
		var s = net.snaps.LastOrDefault();
		if (s != null && (s.fl & 8) != 0) { netTell(new JsonObject { ["k"] = "wipe" }); coopWipe(); }
		else { pl.msg = $"طحت! {net.other} يقدر يرجّعك إذا لمسك"; pl.msgT = 3; }
	}
	static void coopWipe() { pl.down = false; pl.atGate = false; net.otherGate = false; plRespawn(); pl.msg = "طحتوا الاثنين! رجعتوا لآخر حفظ"; pl.msgT = 2.4; }
	// into the open gate (the square waits inside); out again by itself when the other one falls down, to go and bring it back
	static void coopGate(bool inside)
	{
		if (inside == pl.atGate) return; pl.atGate = inside;
		netTell(new JsonObject { ["k"] = inside ? "gate" : "ungate" });
		if (!inside) { pl.gateBlock = true; pl.msg = $"{net.other} طاح! روح المسه عشان يرجع"; pl.msgT = 3; return; }
		burst(player.x, player.y, CYAN, 24, 260); sfx(tt => sTone(tt, 988, 0.2, "triangle", 0.08));
		if (net.otherGate) coopWin(); else { pl.msg = $"أستنى {net.other} يوصل البوابة"; pl.msgT = 1e9; }
	}
	static void coopWin()
	{
		var n = net; n.coopDone = true; n.myFin = pl.t; n.iWon = true; pl.msgT = 0; netFinish();
	}

	// ---------- the match ----------
	static void netGo()
	{
		var n = net; if (n == null || !n.host || !n.hello) return;
		double d = NET_GO - Math.Max(0, n.rtt) / 2;   // so the other PC starts at the same moment as this one
		n.peer.Send(new JsonObject { ["k"] = "go", ["d"] = d }.ToJsonString());
		netStart(performance.now() + NET_GO);
	}
	static void netStart(double goAt)
	{
		var n = net; n.myFin = n.otherFin = -1; n.iWon = false; n.snaps.Clear(); n.ghost = null; n.otherGate = false; n.coopDone = false;
		plStart(n.L, true); pl.introAt = goAt - PL_INTRO; netSetPhase("play");
	}
	static void netWin()
	{
		if (net.vs) { vsMeFin(); return; }
		var n = net; n.myFin = pl.t; n.peer.Send(new JsonObject { ["k"] = "win", ["t"] = pl.t }.ToJsonString());
		n.iWon = n.otherFin < 0 || n.myFin < n.otherFin || (n.myFin == n.otherFin && n.host);
		netFinish();
	}
	static void netOtherWon(double t)
	{
		var n = net; n.otherFin = t;
		if (n.phase == "play") { n.iWon = false; netFinish(); }
		else if (n.myFin >= 0 && (t < n.myFin || (t == n.myFin && !n.host))) n.iWon = false;   // both got there at about the same time: the faster time wins
	}
	static void netFinish()
	{
		var n = net; netSetPhase("end"); n.endAt = performance.now();
		state = "pltVsEnd"; plStopMusic(); if (player != null) burst(player.x, player.y, CYAN, 40, 450);
		if (n.iWon) { shake = 10; flash = 0.18; rumble(160, 0.7, 0.5); sfx(tt => { sTone(tt, 784, 0.15, "triangle", 0.12); sTone(tt + 0.1, 1047, 0.15, "triangle", 0.12); sTone(tt + 0.2, 1568, 0.35, "triangle", 0.1); }); }
		else sfx(tt => { sTone(tt, 330, 0.2, "square", 0.06); sTone(tt + 0.15, 247, 0.3, "square", 0.06); });
	}

	// the other square, a little in the past so it moves smoothly between the messages
	static Player netGhost(double now)
	{
		var n = net; if (n?.vs == true) { int o = vsOthers(n).DefaultIfEmpty(-1).First(); return o >= 0 && !n.vleft[o] ? vsGhost(o, now) : null; }   // versus: the first other one (the PC replay keeps one)
		if (n == null || n.snaps.Count == 0) return null;
		double t = now - 100; Snap a = n.snaps[0], b = a;
		for (int i = 0; i < n.snaps.Count; i++) { if (n.snaps[i].at <= t) a = n.snaps[i]; else { b = n.snaps[i]; break; } if (i == n.snaps.Count - 1) b = a; }
		double f = b.at > a.at ? clamp((t - a.at) / (b.at - a.at), 0, 1) : 1;
		var gp = n.ghost ??= new Player { r = 5 };
		gp.x = a.x + (b.x - a.x) * f; gp.y = a.y + (b.y - a.y) * f; gp.sa = b.sa; gp.sk = b.sk; gp.ddx = b.ddx; gp.ddy = b.ddy; gp.dash = b.dash; gp.fx = b.fx; gp.hp = b.hp;
		gp.hurt = (b.fl & 4) != 0 ? 0.03 : 0;
		gp.dash = (b.fl & 8) != 0 ? -1 : gp.dash;   // -1 = down (drawn as such)
		return (b.fl & 1) != 0 || (b.fl & 16) != 0 ? null : gp;   // dead, or waiting inside the gate
	}

	// ---------- menus ----------
	static readonly string[] ON_OPTS = { "سوّي غرفة", "ادخل بكود" };
	static readonly string[] PL_PAUSE_ON = { "أكمل اللعب", "إعادة من آخر حفظ", "الإعدادات", "اطلع من الغرفة" };
	static readonly string[] PL_PAUSE_ON46 = { "أكمل اللعب", "إعادة من آخر حفظ", "الإعدادات", "احفظ الريبلاي", "اطلع من الغرفة" };   // v46: save the replay on the PC (watching would leave the room)
	// v49 (his words 00:05-00:16 10-25, when someone is stuck): versus, the room's maker restarts the whole level for everyone,
	// the others restart from the level's start alone
	const string RE_ALL = "إعادة المرحلة كاملة (للكل)", RE_SAVE_ALL = "إعادة من آخر حفظ (للكل)", RE_START = "إعادة من البداية";
	static readonly string[] PL_PAUSE_VSH = { "أكمل اللعب", "إعادة من آخر حفظ", RE_ALL, "الإعدادات", "احفظ الريبلاي", "اطلع من الغرفة" };
	static readonly string[] PL_PAUSE_VSG = { "أكمل اللعب", "إعادة من آخر حفظ", RE_START, "الإعدادات", "احفظ الريبلاي", "اطلع من الغرفة" };
	static string[] plPauseOn() => net?.vs == true ? (net.host ? PL_PAUSE_VSH : PL_PAUSE_VSG) : PL_PAUSE_ON46;
	// a restart for everyone waits for "متأكد؟" (a press by mistake must not throw the others back): 1 = from the last save, 2 = the whole level
	public static int hostAsk;
	static bool hostAskStep(Inp inp, Action<int> yes)
	{
		if (hostAsk == 0) return false;
		if (inp.confirm) { int a = hostAsk; hostAsk = 0; yes(a); }
		else if (inp.back || inp.pz) hostAsk = 0;
		return true;
	}
	static void drawHostAsk()
	{
		overlay(0.6); rr(W / 2 - 330, H / 2 - 130, 660, 260, 22); g.fillStyle = "#1b1428"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke();
		txt("متأكد؟", W / 2, H / 2 - 70, 52, "#fff");
		txt(hostAsk == 1 ? "الكل يرجع لآخر حفظ، وتنحسب موتة لكل واحد" : "الكل يرجع لأول المرحلة، وكل شي يتصفّر", W / 2, H / 2 - 5, 26, "rgba(255,255,255,0.8)", 600);
		var K = BTN[kind]; row(new[] { (K.confirm, "إيه"), (K.back, "لا") }, W / 2, H / 2 + 70, 38, "#fff", 30);
	}
	// the code's digits from the keyboard (true = used)
	static bool netKey(string code)
	{
		if (state != "pltJoin") return false;
		string dg = code.StartsWith("Digit") ? code.Substring(5) : code.StartsWith("Numpad") && code.Length == 7 && char.IsDigit(code[6]) ? code.Substring(6) : code.StartsWith("Kp") && code.Length == 3 && char.IsDigit(code[2]) ? code.Substring(2) : null;
		if (dg != null) { if (joinCode.Length < 5) joinCode += dg; joinDig = Math.Min(4, joinCode.Length); return true; }
		if (code == "Backspace" && joinCode.Length > 0) { joinCode = joinCode.Substring(0, joinCode.Length - 1); joinDig = Math.Min(4, joinCode.Length); return true; }
		return false;
	}
	static bool stepNet(Inp inp, double now, double dt)
	{
		if (state is "tmIntro" or "tmPlay" or "tmPause" or "tmSet") return tnStep(inp, now);
		if (net?.vs == true && stepVs(inp, now, dt)) return true;
		if (state == "pltOn")
		{
			if (inp.up || inp.down) onSel = 1 - onSel;
			if (inp.confirm) { if (onSel == 0) netHost(PLATS[plSel]); else { joinCode = ""; joinDig = 0; state = "pltJoin"; } }
			else if (inp.back || inp.pz) state = "pltList";
		}
		else if (state == "pltJoin")
		{
			// a controller: up / down change the digit under the line, left / right move it
			if (inp.up || inp.down)
			{
				while (joinCode.Length <= joinDig) joinCode += "0";
				int v = joinCode[joinDig] - '0'; v = (v + (inp.up ? 1 : 9)) % 10;
				joinCode = joinCode.Substring(0, joinDig) + (char)('0' + v) + joinCode.Substring(joinDig + 1);
			}
			if (inp.right && joinDig < 4) joinDig++; if (inp.left && joinDig > 0) joinDig--;
			if (inp.confirm && joinCode.Length == 5) netJoin(joinCode);
			else if (inp.pz || inp.back) state = "pltOn";   // Backspace with digits left only deletes one (netKey)
		}
		else if (state == "pltRoom")
		{
			if (inp.confirm && net?.team == true) { if (net.host && net.phase == "lobby") tnGo(); }
			else if (inp.confirm && net?.phase == "lobby" && net.host && net.hello) netGo();
			else if (inp.back || inp.pz) netEnd(null, true);
		}
		else if (state == "pltVsEnd")
		{
			if (pl != null) plRpKey(inp, net.endAt, net.coopDone || net.iWon ? "vsWin" : "vsLose", null);
			if (inp.confirm && net.host && net.hello && performance.now() - net.endAt > 800) netGo();
			else if (inp.back) netEnd(null, true);
		}
		else return false;
		return true;
	}
	static int joinDig = 0;
}
