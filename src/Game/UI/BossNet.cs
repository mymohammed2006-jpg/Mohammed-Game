using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using static MG.JS;

namespace MG;

// The boss mode online (Mohammed 04:49 10-25): 1 to 4 players against the boss, together. The room is the versus room (Plat/VsNet.cs:
// the maker's PC is in the middle, up to three come in with the code); "go" starts the boss's scene on every PC at the same moment,
// then each PC plays the level by its own song time and judges only its own square. What goes across: where each square is (30 a second),
// who went down, who brought whom back, who finished with which letter.
// - Down (no hearts left): the square stays where it is, grey, inside a pink ring with a gap that turns; the boss throws at it.
//   Another square touching it (through the gap) brings it back with ONE heart.
// - Everyone down: the maker says so and the level starts again from its start on every PC (no scene).
// - The mark aims at one standing player at a time (in turn); the circles: one for each standing player, in that player's colour,
//   and only your own circle keeps you safe.
// - At the end the team wins if one is standing; each one gets the normal letter (S no hit, A hit but never down, B down once, C more).
public static partial class G
{
	public static readonly string[] BN_COL = { CYAN, "#ffcf5a", "#5dff9a", "#b07cff" };   // each seat's colour (never pink)
	public partial class NetRun
	{
		public bool boss; public Level BL;
		public bool[] bdown = new bool[VS_N]; public (double x, double y)[] bdpos = new (double, double)[VS_N];
		public string[] bfin = new string[VS_N];   // each one's letter at the end
		public bool bwon;   // a round was won in this room: its rating card when leaving (UI/Rate.cs)
		public double wipeAt = -1, startAt = -1, leaveArm = -1;   // (revAt: the co-op one, PlatNet.cs)
	}
	public static bool bnOn => net?.boss == true && net.phase is "play" or "end";
	public static bool bnMeDown;   // my square is down (online)
	static int bnMe => lcB ? lcCur : net?.vseat ?? 0;   // local play: the seat being judged now (UI/LocalBoss.cs)
	// the seats in this match (and still here)
	static IEnumerable<int> bnSeats() { var n = net; return lcB ? Enumerable.Range(0, lc.seats.Count) : n == null ? new[] { 0 } : Enumerable.Range(0, VS_N).Where(s => n.vplay[s] && !n.vleft[s]); }
	static bool bnStanding(int s) => lcB ? !lc.seats[s].down : net == null ? !bnMeDown : !net.bdown[s];
	// online or local: more than one square (each its own colour, circle and name)
	public static bool bnMulti => net?.boss == true || lcB;
	public static bool bnDownOf(int s) => lcB ? s < lc.seats.Count && lc.seats[s].down : net?.boss == true && net.bdown[s];
	public static (double x, double y) bnDPos(int s) => lcB ? (lc.seats[s].dx, lc.seats[s].dy) : net.bdpos[s];
	public static string bnName(int s) => lcB ? lc.seats[s].name : vsName(s);
	public static List<int> bnStandingSeats() => bnSeats().Where(bnStanding).ToList();
	// where a seat's square is now (mine: the real one; the others: as last heard)
	public static (double x, double y)? bnPos(int s)
	{
		if (lcB) { var q0 = lc.seats[s]; return q0.down ? (q0.dx, q0.dy) : q0.p == null ? null : (q0.p.x, q0.p.y); }
		if (net == null || s == bnMe) return player == null ? null : (player.x, player.y);
		if (net.bdown[s]) return net.bdpos[s];
		var L = net.vsn[s]; if (L.Count == 0) return null; var q = L[^1]; return (q.x, q.y);
	}
	// the marked one at beat b: the standing ones in turn, a new one every 8 beats
	public static int bnMarkSeat(double b)
	{
		var st = bnStandingSeats(); if (st.Count == 0) return lcB ? 0 : bnMe;
		return st[(int)Math.Floor(b / 8) % st.Count];
	}

	// ---------- the room ----------
	static async void bnHost()
	{
		var L = bossL; if (L == null) return;
		var n = net = new NetRun { host = true, vs = true, boss = true, vseat = 0, BL = L }; for (int s = 1; s < VS_N; s++) n.vp[s] = new Peer();
		n.vnm[0] = acc?.name ?? ""; n.vsk[0] = skin; n.vin[0] = true; netSetPhase("make"); state = "pltRoom";
		try
		{
			var os = Enumerable.Range(1, VS_N - 1).Select(s => n.vp[s].Offer()).ToArray(); await System.Threading.Tasks.Task.WhenAll(os); if (net != n) return;
			string code = await roomMake(L.id, os[0].Result, os[1].Result, os[2].Result); if (net != n) { if (code != null) _ = roomDel(code); return; }
			if (code == null) { netEnd("ما قدرت أسوي غرفة، جرب مرة ثانية"); return; }
			n.code = code; n.roomOpen = true; n.madeAt = performance.now(); netSetPhase("wait");
			while (net == n && n.roomOpen)
			{
				await System.Threading.Tasks.Task.Delay(1500); if (net != n || !n.roomOpen) return;
				var d = await roomGet(code); if (net != n || !n.roomOpen) return;
				if (d == null) { n.roomOpen = false; if (vsGuests(n) == 0) netEnd("انتهت الغرفة"); return; }
				for (int s = 1; s < VS_N; s++)
				{
					string a = d.A(s); if (a == null || n.vconnAt[s] != 0) continue;
					n.vnm[s] = d.GN(s) ?? "";
					if (!n.vp[s].Answer(a)) { n.vconnAt[s] = -1; n.vp[s].Close(); continue; }
					n.vconnAt[s] = performance.now();
				}
				if (Enumerable.Range(1, VS_N - 1).All(s => n.vconnAt[s] != 0)) { n.roomOpen = false; _ = roomDel(code); return; }
				if (performance.now() - n.madeAt > 600000) { n.roomOpen = false; _ = roomDel(code); if (vsGuests(n) == 0) netEnd("انتهت الغرفة"); return; }
			}
		}
		catch (Exception) { if (net == n) { if (vsGuests(n) == 0) netEnd("ما فيه إنترنت؟ جرب مرة ثانية"); else n.roomOpen = false; } }
	}
	// a boss room's code (netJoin found its level among the boss levels)
	static async void bnJoin(NetRun n, RoomDoc d)
	{
		try
		{
			n.vs = true; n.boss = true; n.vnm[0] = d.hn ?? ""; n.BL = BOSSLV.FirstOrDefault(b => b.id == d.lv);
			if (d.h == acc.uid) { netEnd("هذي غرفتك أنت"); return; }
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

	// ---------- the match ----------
	// "go" (VsNet.vsStart sends a boss room here): the scene on every PC at the same moment, then the level
	static void bnStart(double goAt)
	{
		var n = net; n.bdown = new bool[VS_N]; n.bfin = new string[VS_N]; n.wipeAt = -1; n.leaveArm = -1; n.startAt = goAt; bnMeDown = false;
		foreach (var l in n.vsn) l.Clear(); n.vgh = new Player[VS_N];
		var L = n.BL; cur = L; fromList = "bossMenu"; levelTiming(L); hitsTotal = 0; deaths = 0; rec = new RecState(); checkpoint = 0; startBeat = 0;
		player = null; hazards = new List<Hz>(); particles = new List<Particle>(); introOn = false;
		state = "levelIntro"; introAt = goAt; bnSceneMusicAt = goAt; netSetPhase("play");
		if (bnTestFrom > 0) { introAt = goAt - bossSceneLen * 1000; bnSceneMusicAt = -1; }
	}
	static double bnSceneMusicAt = -1;
	// everyone down: the same new start on every PC (no scene), from the save the maker had reached (v55: every boss saves at 50%, his rule 10-29)
	static double bnWipeCp;
	static void bnWipe(double at, double cp)
	{
		var n = net; if (n == null) return; n.wipeAt = at; n.bdown = new bool[VS_N]; bnMeDown = false; bnMsgT = 0; bnWipeCp = cp;
		if (bnPlaying) { stopMusic(); state = "bnWipe"; deadAt = performance.now(); }
	}
	static void bnRestart()
	{
		var n = net; n.wipeAt = -1; foreach (var l in n.vsn) l.Clear();
		double cp = bnWipeCp;
		if (cp > 0) { checkpoint = cp; startBeat = cp; beginRun(cp); }   // the hits and downs so far still count (like "كمّل من آخر حفظ")
		else { hitsTotal = 0; deaths = 0; rec = new RecState(); checkpoint = 0; startBeat = 0; beginRun(bnTestFrom); }
	}
	// my hearts ran out
	public static void bnDown()
	{
		var n = net; var p = player; bnMeDown = true; n.bdown[n.vseat] = true; n.bdpos[n.vseat] = (p.x, p.y); p.dash = 0; deaths++;
		burst(p.x, p.y, CYAN, 40, 420); rumble(400, 1, 0.8);
		vsSend(new JsonObject { ["k"] = "dn", ["w"] = n.vseat, ["x"] = p.x, ["y"] = p.y });
		bnDownAdded(n.vseat);
		if (n.host) bnCheckWipe();
	}
	static void bnDownAdded(int s) { hazards.Add(cur?.id == "B2" ? B2DownRing(s, songT) : B1bDownRing(s, songT)); }
	static void bnCheckWipe()
	{
		var n = net; if (n == null || !n.host || n.wipeAt >= 0 || n.phase != "play") return;
		var seats = bnSeats().ToList(); if (seats.Count == 0 || !seats.All(s => n.bdown[s])) return;
		double at = performance.now() + 2600, cp = checkpoint;
		for (int s = 1; s < VS_N; s++) if (n.vplay[s] && !n.vleft[s] && n.vconnAt[s] > 0) n.vp[s].Send(new JsonObject { ["k"] = "wipe", ["d"] = 2600 - Math.Max(0, n.vrt[s]) / 2, ["c"] = cp }.ToJsonString());
		bnWipe(at, cp);
	}
	// a boss message (true = used); the maker passes each one on to the others
	static bool bnMsg(string k, JsonNode j, int from, byte[] raw, double now)
	{
		var n = net; if (n == null || !n.boss) return false;
		int w = j?["w"] is JsonValue wv && wv.TryGetValue<int>(out int wi) ? wi : -1;
		if (k is "dn" or "rv" or "fin" && n.host) vsSendB(true, raw, from);
		if (k == "dn" && w >= 0 && w < VS_N && w != n.vseat)
		{
			n.bdown[w] = true; n.bdpos[w] = (j["x"]?.GetValue<double>() ?? 0, j["y"]?.GetValue<double>() ?? 0);
			if (bnPlaying) { bnDownAdded(w); bnNote($"{vsName(w)} طاح! المسه عشان يرجع"); }
			if (n.host) bnCheckWipe();
		}
		else if (k == "rv" && w >= 0 && w < VS_N)
		{
			int r = j["r"]?.GetValue<int>() ?? -1; n.bdown[w] = false;
			if (w != n.vseat && bnMsgT > 0 && bnMsgS == $"{vsName(w)} طاح! المسه عشان يرجع") bnMsgT = 0;   // he is back: the "touch him" line goes (Mohammed 14:33 10-25)
			if (w == n.vseat && bnMeDown && bnPlaying)
			{
				bnMeDown = false; player.hp = 1; player.inv = 1.5; player.hurt = 0; burst(player.x, player.y, CYAN, 30, 380);
				bnNote((r >= 0 && r < VS_N ? vsName(r) : "") + " رجّعك! عندك قلب واحد");
				sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.2, "triangle", 0.1); });
			}
		}
		else if (k == "wipe" && !n.host) bnWipe(now + (j["d"]?.GetValue<double>() ?? 2600), j["c"]?.GetValue<double>() ?? 0);
		else if (k == "fin" && w >= 0 && w < VS_N) n.bfin[w] = S(j["r"]);
		else return k is "dn" or "rv" or "fin" or "wipe";
		return true;
	}
	static string bnMsgS; static double bnMsgT;
	static void bnNote(string s) { bnMsgS = s; bnMsgT = 2.6; }
	// every frame (from vsTick): my square across, touching a downed one, the start and the new start
	static void bnTick(double now)
	{
		var n = net; if (n.phase != "play") return;
		if (state == "levelIntro" && bnSceneMusicAt > 0 && now >= bnSceneMusicAt) { bnSceneMusicAt = -1; bossSceneMusic(); }
		if (state == "levelIntro" && now - introAt > bossSceneLen * 1000) { beginRun(bnTestFrom); return; }
		if (state == "bnWipe" && n.wipeAt >= 0 && now >= n.wipeAt) { bnRestart(); return; }
		if (!bnPlaying || player == null) return;
		if (now - n.sendAt >= 33)
		{
			n.sendAt = now; var q = player; var b = new byte[47]; int o = 0;
			void F(double v) { BitConverter.TryWriteBytes(b.AsSpan(o), (float)v); o += 4; }
			b[o++] = 1; b[o++] = (byte)n.vseat; F(songT); F(q.x); F(q.y); F(q.sa ?? 0); F(q.sk ?? 0); F(q.ddx); F(q.ddy); F(q.dash); F(q.fx); b[o++] = (byte)Math.Max(0, q.hp);
			b[o++] = (byte)((q.hurt > 0 && Math.Floor(q.hurt * 18) % 2 != 0 ? 4 : 0) | (bnMeDown ? 8 : 0));
			vsSendB(false, b.AsSpan(0, o).ToArray());
		}
		// touching a downed square brings it back (once; the downed one's PC gives it its heart)
		if (!bnMeDown && state == "play" && now - n.revAt > 500)
			foreach (int s in bnSeats())
			{
				if (s == n.vseat || !n.bdown[s]) continue; var d = n.bdpos[s];
				if (hypot(player.x - d.x, player.y - d.y) < 26)
				{
					n.revAt = now; n.bdown[s] = false; vsSend(new JsonObject { ["k"] = "rv", ["w"] = s, ["r"] = n.vseat }); burst(d.x, d.y, BN_COL[s], 30, 320);
					sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.2, "triangle", 0.1); });
					bnNote($"رجّعت {vsName(s)}!"); break;
				}
			}
		bnMsgT -= 1.0 / 60;
	}
	// the end of the song (playStep): tell the others my letter
	public static void bnFinish()
	{
		var n = net; if (n == null) return; n.bfin[n.vseat] = lastRank; n.bwon = true; netSetPhase("end"); n.endAt = performance.now();
		vsSend(new JsonObject { ["k"] = "fin", ["w"] = n.vseat, ["r"] = lastRank });
	}
	// someone left during the match: they don't count any more (all the rest down = the new start)
	static void bnGone(int s) { if (net?.boss == true && net.phase == "play") { net.bdown[s] = false; bnNote(vsName(s) + " طلع من الغرفة"); if (net.host) bnCheckWipe(); } }

	// ---------- the pause online (his ask 10-29): Start / Esc / the window losing focus (the Windows key) open it; the game goes on for
	// everyone, this square stands where it is (it can still be hit), like the co-op platformer's pause. Nothing stops the music here.
	public static bool bnPlaying => state is "play" or "bnPause" or "bnSet";
	public static readonly Inp BN_IDLE = new();
	static readonly string[] BN_PAUSE = { "أكمل اللعب", "الإعدادات", "اطلع من الغرفة" };
	public static void bnPauseOpen() { state = "bnPause"; pauseSel = 0; }
	static void bnPauseStep(Inp inp, double dt)
	{
		int pn = BN_PAUSE.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
		if (inp.pz || inp.back || (inp.confirm && pauseSel == 0)) state = "play";
		else if (inp.confirm && pauseSel == 1) { state = "bnSet"; setSel = 0; }
		else if (inp.confirm) { netEnd(null, true); return; }
		updatePlay(dt, BN_IDLE);
	}
	public static void drawBnPause(double now)
	{
		if (!bnOn || state is not ("bnPause" or "bnSet")) return;
		overlay(state == "bnSet" ? 0.95 : 0.75);
		if (state == "bnSet") { drawSettings(); return; }
		txt("اللعبة متوقفة عندك", W / 2, 165, 56, "#fff");
		txt("الباقين يكملون، ومربعك واقف مكانه", W / 2, 222, 22, "rgba(255,255,255,0.55)", 500);
		for (int i = 0; i < BN_PAUSE.Length; i++)
		{
			double y = 310 + i * 80, bh = 68; bool sel = i == pauseSel; int ii = i; zone(W / 2 - 260, y - bh / 2, 520, bh, () => pauseSel = ii);
			if (sel) { rr(W / 2 - 260, y - bh / 2, 520, bh, 16); g.fillStyle = $"rgba({RGB},0.12)"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			txt(BN_PAUSE[i], W / 2, y, sel ? 38 : 32, sel ? "#fff" : "rgba(255,255,255,0.5)", 800);
		}
	}
	// the win screen's lower part online: everyone's letter, then play again (the maker) or leave
	static void bnWinRows(double now)
	{
		var n = net; drawBnCards(now, H / 2 + 100);
		if (now - Math.Max(winAt + 800, winSlam + 300) <= 0) return;
		var K = BTN[kind];
		if (vsCanGo) row(new[] { (K.confirm, "العب مرة ثانية"), (K.back, "اطلع من الغرفة") }, W / 2, H - 34);
		else { txt(n.host ? "كل اللي معك طلعوا من الغرفة" : $"استنى {vsName(0)} يبدأ مرة ثانية", W / 2, H - 74, 24, "rgba(255,255,255,0.75)", 700); row(new[] { (K.back, "اطلع من الغرفة") }, W / 2, H - 34); }
	}

	// ---------- drawing ----------
	// the others' squares (their looks; a same look later in the room takes its seat's colour) with their names
	public static void bnDrawOthers(double now)
	{
		var n = net; if (n == null || !n.boss) return;
		foreach (int s in bnSeats())
		{
			if (s == n.vseat) continue;
			if (n.bdown[s]) { var d = n.bdpos[s]; bnDownSquare(d.x, d.y, n.vsk[s] ?? "", s, now); txt(vsName(s), d.x, d.y - 46, 18, BN_COL[s], 800); continue; }
			var gp = vsGhost(s, now); if (gp == null) continue;
			tinted(dupHue(n.vsk, s), () => drawPlayer(gp, n.vsk[s] ?? "", 0.85));
			g.globalAlpha = 0.9; txt(vsName(s), gp.x, gp.y - 30, 18, BN_COL[s], 800); g.globalAlpha = 1;
		}
		if (bnMeDown && player != null) { bnDownSquare(player.x, player.y, skin, n.vseat, now); }
	}
	static void bnDownSquare(double x, double y, string sk, int s, double now)
	{
		g.save(); g.globalAlpha = 0.75; g.fillStyle = "#6c6c7a"; g.fillRect(x - 11, y - 11, 22, 22); g.fillStyle = "#1b0f26"; g.fillRect(x - 5, y - 5, 10, 10); g.restore();
		g.strokeStyle = BN_COL[s]; g.lineWidth = 2; g.globalAlpha = 0.5 + 0.4 * JS.sin(now / 160); g.strokeRect(x - 15, y - 15, 30, 30); g.globalAlpha = 1;
	}
	// over the HUD: the others' hearts at the top right, a line when someone goes down or comes back
	public static void bnHUD(double now)
	{
		var n = net; if (n == null || !n.boss) return;
		int i = 0;
		foreach (int s in bnSeats())
		{
			if (s == n.vseat) continue; double y = 30 + i * 30, x = W - 30; i++;
			int hp = n.bdown[s] ? 0 : n.vsn[s].Count > 0 ? n.vsn[s][^1].hp : maxHp(cur);
			for (int k = 0; k < maxHp(cur); k++) { double hx = x - k * 20; if (k < hp) { g.fillStyle = BN_COL[s]; g.fillRect(hx - 7, y - 7, 14, 14); } else { g.strokeStyle = BN_COL[s]; g.globalAlpha = 0.35; g.lineWidth = 2; g.strokeRect(hx - 6, y - 6, 12, 12); g.globalAlpha = 1; } }
			txt(n.bdown[s] ? vsName(s) + " (طايح)" : vsName(s), x - maxHp(cur) * 20 - 8, y + 1, 18, n.bdown[s] ? "rgba(255,255,255,0.5)" : "#fff", 700, "right");
		}
		if (state is "bnPause" or "bnSet") { }   // the pause's own words are there
		else if (bnMeDown) txt("طحت! استنى أحد يلمسك عشان ترجع", W / 2, 120, 32, "#fff", 800);
		else if (bnMsgT > 0 && bnMsgS != null) { g.globalAlpha = clamp(bnMsgT / 0.4, 0, 1); txt(bnMsgS, W / 2, 120, 30, "#fff", 800); g.globalAlpha = 1; }
	}
	public static void drawBnWipe(double now)
	{
		overlay(0.6); double e = clamp((now - deadAt) / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3);
		g.save(); g.translate(W / 2, H / 2 - 40); g.scale(sc, sc); g.globalAlpha = e; txt("طحتوا كلكم!", 0, 0, 84, PINK); g.restore(); g.globalAlpha = 1;
		txt((bnWipeCp > 0 ? "من آخر حفظ" : "من بداية المرحلة") + new string('.', 1 + (int)Math.Floor(now / 400) % 3), W / 2, H / 2 + 50, 32, "#fff", 700);
	}
	// the win screen, online: a card for each one with their letter
	public static void drawBnCards(double now, double top)
	{
		var n = net; if (n == null || !n.boss) return; var seats = bnSeats().ToList(); double cw = 230, gp = 20, tot = seats.Count * cw + (seats.Count - 1) * gp;
		for (int i = 0; i < seats.Count; i++)
		{
			int s = seats[i]; double x = W / 2 + tot / 2 - cw / 2 - i * (cw + gp), a = clamp((now - winAt - 300 - i * 150) / 250, 0, 1); if (a <= 0) continue;
			g.globalAlpha = a; rr(x - cw / 2, top, cw, 120, 16); g.fillStyle = s == n.vseat ? "rgba(255,255,255,0.10)" : "rgba(255,255,255,0.05)"; g.fill(); g.strokeStyle = BN_COL[s]; g.lineWidth = 2; g.stroke();
			txt(vsName(s), x, top + 30, 24, BN_COL[s], 800);
			string r = n.bfin[s]; txt(r ?? "...", x, top + 82, r != null ? 52 : 30, r == null ? "rgba(255,255,255,0.5)" : r == "S" ? "#ffd54a" : "#fff", 800);
		}
		g.globalAlpha = 1;
	}
}
