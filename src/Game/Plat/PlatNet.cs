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
// An online match gives no letter, points or XP.
// States: pltOn (make a room / type a code), pltJoin (the code), pltRoom (waiting / connecting / together), pltVsEnd (who won).
public static partial class G
{
	public class Snap { public double at, x, y, sa, sk, ddx, ddy, dash, fx; public int hp, fl; }
	public class NetRun
	{
		public Peer peer; public bool host; public string code = "", phase = "";   // phase: make, wait, join, conn, lobby, play, end
		public PlatLevel L; public string other = "", otherSk = ""; public bool hello;
		public double phaseAt, pingAt, rtt = -1, sendAt, endAt;
		public double myFin = -1, otherFin = -1; public bool iWon, otherLeft;
		public List<Snap> snaps = new();
		public Player ghost;
	}
	public static NetRun net;
	public static int onSel = 0; static string joinCode = "";
	static string netMsg; static double netMsgAt = -1e9;   // a line on the level list (why the room ended)
	const double NET_GO = 3000;   // ms from "start" to the music (the intro)

	static bool online => net != null && net.phase is "play" or "end";
	static void netNote(string m) { netMsg = m; netMsgAt = performance.now(); }
	static void netSetPhase(string p) { if (net != null) { net.phase = p; net.phaseAt = performance.now(); } }

	// ---------- making / joining ----------
	static async void netHost(PlatLevel L)
	{
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
	// leave (or the other left): back to the level list with a line saying why
	static void netEnd(string why, bool tell = false)
	{
		var n = net; if (n == null) return; net = null;
		if (tell) n.peer?.Send("{\"k\":\"bye\"}");
		var p = n.peer; _ = Task.Run(async () => { await Task.Delay(300); p?.Close(); });
		if (n.host && n.code != "" && n.phase is "make" or "wait" or "conn") _ = roomDel(n.code);
		if (why != null) netNote(why);
		if (state.StartsWith("plt") && state != "pltList") { plStopMusic(); player = null; particles = new List<Particle>(); actx.resume(); state = "pltList"; }
	}

	// ---------- every frame ----------
	public static void netTick(double now)
	{
		var n = net; if (n == null) return;
		if (n.peer.closed && n.phase != "make" && n.phase != "wait" && n.phase != "join") { netEnd(n.hello ? n.other + " طلع من الغرفة" : "ما قدرنا نتصل، جرب مرة ثانية"); return; }
		if (n.phase == "conn")
		{
			if (n.peer.open) { n.peer.Send(new JsonObject { ["k"] = "hi", ["n"] = acc?.name ?? "", ["s"] = skin, ["v"] = 1 }.ToJsonString()); netSetPhase("lobby"); if (n.host) _ = roomDel(n.code); }
			else if (now - n.phaseAt > 20000) { netEnd("ما قدرنا نتصل، يمكن الشبكة تمنع الاتصال المباشر"); return; }
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
		}
		if (n.phase is "lobby" or "end" && n.peer.open && now - n.pingAt > 1000) { n.pingAt = now; n.peer.Send(new JsonObject { ["k"] = "ping", ["t"] = now }.ToJsonString()); }
		// where my square is, 30 times a second
		if (n.phase == "play" && player != null && pl != null && now - n.sendAt >= 33)
		{
			n.sendAt = now; var p = player; var b = new byte[46]; int o = 0;
			void F(double v) { BitConverter.TryWriteBytes(b.AsSpan(o), (float)v); o += 4; }
			b[o++] = 1; F(pl.t); F(p.x); F(p.y); F(p.sa ?? 0); F(p.sk ?? 0); F(p.ddx); F(p.ddy); F(p.dash); F(p.fx); b[o++] = (byte)Math.Max(0, p.hp);
			b[o++] = (byte)((state == "pltDead" ? 1 : 0) | (state == "pltPause" || state == "pltSet" ? 2 : 0) | (p.hurt > 0 && Math.Floor(p.hurt * 18) % 2 != 0 ? 4 : 0));
			n.peer.Send(false, b.AsSpan(0, o).ToArray());
		}
	}
	static void netSnap(byte[] d, double now)
	{
		if (d.Length < 39 || d[0] != 1 || net == null) return; int o = 1;
		double F() { double v = BitConverter.ToSingle(d, o); o += 4; return v; }
		var s = new Snap { at = now }; F(); s.x = F(); s.y = F(); s.sa = F(); s.sk = F(); s.ddx = F(); s.ddy = F(); s.dash = F(); s.fx = F(); s.hp = d[o++]; s.fl = d[o++];
		net.snaps.Add(s); if (net.snaps.Count > 40) net.snaps.RemoveAt(0);
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
		var n = net; n.myFin = n.otherFin = -1; n.iWon = false; n.snaps.Clear(); n.ghost = null;
		plStart(n.L, true); pl.introAt = goAt - PL_INTRO; netSetPhase("play");
	}
	static void netWin()
	{
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
		var n = net; if (n == null || n.snaps.Count == 0) return null;
		double t = now - 100; Snap a = n.snaps[0], b = a;
		for (int i = 0; i < n.snaps.Count; i++) { if (n.snaps[i].at <= t) a = n.snaps[i]; else { b = n.snaps[i]; break; } if (i == n.snaps.Count - 1) b = a; }
		double f = b.at > a.at ? clamp((t - a.at) / (b.at - a.at), 0, 1) : 1;
		var gp = n.ghost ??= new Player { r = 5 };
		gp.x = a.x + (b.x - a.x) * f; gp.y = a.y + (b.y - a.y) * f; gp.sa = b.sa; gp.sk = b.sk; gp.ddx = b.ddx; gp.ddy = b.ddy; gp.dash = b.dash; gp.fx = b.fx; gp.hp = b.hp;
		gp.hurt = (b.fl & 4) != 0 ? 0.03 : 0;
		return (b.fl & 1) != 0 ? null : gp;
	}

	// ---------- menus ----------
	static readonly string[] ON_OPTS = { "سوّي غرفة", "ادخل بكود" };
	static readonly string[] PL_PAUSE_ON = { "أكمل اللعب", "إعادة من آخر حفظ", "الإعدادات", "اطلع من الغرفة" };
	// the code's digits from the keyboard (true = used)
	static bool netKey(string code)
	{
		if (state != "pltJoin") return false;
		string dg = code.StartsWith("Digit") ? code.Substring(5) : code.StartsWith("Numpad") && code.Length == 7 && char.IsDigit(code[6]) ? code.Substring(6) : code.StartsWith("Kp") && code.Length == 3 && char.IsDigit(code[2]) ? code.Substring(2) : null;
		if (dg != null) { if (joinCode.Length < 5) joinCode += dg; joinDig = Math.Min(4, joinCode.Length); return true; }
		if (code == "Backspace" && joinCode.Length > 0) { joinCode = joinCode.Substring(0, joinCode.Length - 1); joinDig = Math.Min(4, joinCode.Length); return true; }
		return false;
	}
	static bool stepNet(Inp inp, double now)
	{
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
			if (inp.confirm && net?.phase == "lobby" && net.host && net.hello) netGo();
			else if (inp.back || inp.pz) netEnd(null, true);
		}
		else if (state == "pltVsEnd")
		{
			if (inp.confirm && net.host && net.hello && performance.now() - net.endAt > 800) netGo();
			else if (inp.back) netEnd(null, true);
		}
		else return false;
		return true;
	}
	static int joinDig = 0;
}
