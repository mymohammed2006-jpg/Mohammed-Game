using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// Local play (his ask 08:57 10-30, his يالله 09:08): 2 to 4 players on this PC, each with their own controller (the keyboard counts as
// one), offline, the same modes as online: the boss mode (1-4), the platformer's versus (2-4) and its co-op levels (exactly their players).
// One screen for everyone. The join screen: each one presses ✕ on their controller to take the next seat (○ leaves it); the first one in is
// player 1 (this PC's account when it has one) and starts with Options. Each other seat is a guest (nothing saved) or signs in to their
// own account, or makes a new one here (his ask 09:06): their letter and XP go to their own account, and their sign-in is forgotten as
// soon as they leave local play. The pause stops the game for everyone. No replay is saved.
public static partial class G
{
	public class LcSeat
	{
		public int i, dev; public Acc acc; public PlayerDoc doc; public bool pc; public string sk = ""; public int msel;
		public string saveSt;                  // null: nothing to save, "saving", "ok", "same" (no better letter), "net", "err"
		public int xpGain = -1;
		public Inp inp = new();
		public bool dashQ, jumpQ, jumpHeld; public double inX, inY;
		// the boss: the square, down or not (and where), hits and downs, the letter
		public Player p; public bool down; public double dx, dy; public int hits, deaths; public string rank; public double revAt = -1e9;
		// versus: this one's run of the level, the time through the gate (-1: not yet), dead since
		public PlatRun run; public double fin = -1, deadAt = -1;
		public string name => acc?.name ?? $"اللاعب {ar(i + 1)}";
		public bool guest => acc == null;
	}
	public class LcRun
	{
		public string mode;                    // "boss", "vs", "team"
		public Level BL; public PlatLevel PL; public string back;
		public List<LcSeat> seats = new();
		public bool playing, won, saved; public double endAt, goAt; public int[] res; public double wipeAt = -1;
		public int need => mode == "team" ? PL.team.players : mode == "vs" ? 2 : 1;
		public int max => mode == "team" ? PL.team.players : 4;
		public Level lv => mode == "boss" ? BL : PL.lv;
	}
	public static LcRun lc;
	public static int lcCur = -1;              // the seat being moved / judged in this step (-1: none)
	public static bool lcB => lc?.playing == true && lc.mode == "boss";
	public static bool lcV => lc?.playing == true && lc.mode == "vs";
	public static bool lcT => lc?.playing == true && lc.mode == "team";
	static LcSeat lcSeat(int i) => lc != null && i >= 0 && i < lc.seats.Count ? lc.seats[i] : null;
	public static string lcCol(int i) => lc?.mode == "team" ? tmColOf(lc.PL.team, i) : BN_COL[Math.Clamp(i, 0, 3)];
	static string tmColOf(TeamLevel L, int i) => L?.cols != null && i < L.cols.Length ? TM_COL[L.cols[i]] : TM_COL[Math.Clamp(i, 0, TM_COL.Length - 1)];
	static string[] lcSkins() => lc.seats.Select(s => s.sk ?? "").ToArray();
	static string lcMsg; static double lcMsgT;
	static void lcNote(string s) { lcMsg = s; lcMsgT = 2.6; }
	// the pause stops everything here (nothing goes on for anyone)
	public static bool lcPaused() => lc?.playing == true && state is "paused" or "pauseSet" or "pltPause" or "pltSet" or "tmPause" or "tmSet";

	// ---------- the join screen ----------
	public const string LC_OPT = "على نفس الجهاز";
	public static void lcOpen(string mode, Level BL, PlatLevel PL, string back)
	{
		lc = new LcRun { mode = mode, BL = BL, PL = PL, back = back }; state = "lcJoin"; lcMsgT = 0; devPrime();
	}
	static void lcExit()
	{
		var b = lc?.back ?? "modes"; lc = null; lcCur = -1; state = b; devPrime();
	}
	static readonly string[] LC_ROWS_G = { "ضيف", "سجّل دخول", "حساب جديد" };
	static readonly string[] LC_ROWS_A = { "سجّل خروج" };
	static string[] lcRows(LcSeat s) => s.pc ? Array.Empty<string>() : s.guest ? LC_ROWS_G : LC_ROWS_A;
	static void lcReindex() { for (int k = 0; k < lc.seats.Count; k++) lc.seats[k].i = k; }
	static void lcJoinStep()
	{
		foreach (int dev in lcDevices())
		{
			var d = readDev(dev); var s = lc.seats.FirstOrDefault(q => q.dev == dev);
			if (s == null)
			{
				if (d.confirm)
				{
					if (lc.seats.Count >= lc.max) { lcNote(lc.mode == "team" ? $"هذي المرحلة لـ {ar(lc.max)} لاعبين بالضبط" : "الخانات الأربع كلها مليانة"); continue; }
					var ns = new LcSeat { i = lc.seats.Count, dev = dev };
					if (ns.i == 0 && acc != null) { ns.pc = true; ns.acc = acc; ns.sk = skin; }
					lc.seats.Add(ns); sfx(tt => { sTone(tt, 523, 0.1, "triangle", 0.1); sTone(tt + 0.07, 784, 0.16, "triangle", 0.1); });
					if (dev != KB) { rumbleOnly = dev; rumble(120, 0.5, 0.4); rumbleOnly = -2; }
				}
				else if (d.back || (dev == KB && d.pz && lc.seats.Count == 0)) { lcExit(); return; }
				continue;
			}
			var rows = lcRows(s);
			if (rows.Length > 0) { if (d.up) s.msel = (s.msel + rows.Length - 1) % rows.Length; if (d.down) s.msel = (s.msel + 1) % rows.Length; s.msel = Math.Clamp(s.msel, 0, rows.Length - 1); }
			if ((d.l1 || d.r1) && !s.guest) lcSkinNext(s, d.r1 ? 1 : -1);
			if (d.confirm && rows.Length > 0)
			{
				string r = rows[s.msel];
				if (r == "سجّل دخول" || r == "حساب جديد") { lcAuthOpen(s, r == "حساب جديد"); return; }
				if (r == "سجّل خروج") { s.acc = null; s.doc = null; s.sk = ""; s.msel = 0; }
			}
			else if (d.pz && s.i == 0) { lcTryStart(); if (state != "lcJoin") return; }
			else if (d.back)
			{
				if (s.i == 0) { lcExit(); return; }
				lc.seats.Remove(s); lcReindex();
			}
		}
	}
	// L1 / R1: the next look this account has opened (its letters open the same challenges as on its own PC)
	static void lcSkinNext(LcSeat s, int d)
	{
		if (s.pc) return;
		var open = SKINS.Where(k => k == "" || (s.doc != null && CHALS.Any(c => c.id == k && chalDone(c, id => gv(s.doc.n, id))))).ToList();
		int i = Math.Max(0, open.IndexOf(s.sk ?? "")); s.sk = open[(i + d + open.Count) % open.Count];
	}
	static void lcTryStart()
	{
		int n = lc.seats.Count;
		if (n < lc.need) { lcNote(lc.mode == "team" ? $"هذي المرحلة لـ {ar(lc.need)} لاعبين بالضبط" : $"تحتاجون {ar(lc.need)} لاعبين على الأقل"); return; }
		lcGo();
	}
	// the match starts (again): each mode's own start
	static void lcGo()
	{
		lc.playing = true; lc.won = false; lc.saved = false; lc.res = null; lc.wipeAt = -1; lcMsgT = 0; lcCur = -1;
		foreach (var s in lc.seats) { s.rank = null; s.saveSt = null; s.xpGain = -1; s.hits = s.deaths = 0; s.fin = -1; s.deadAt = -1; s.down = false; s.dashQ = s.jumpQ = s.jumpHeld = false; s.inX = s.inY = 0; }
		devPrime();
		if (lc.mode == "boss") lcBossBegin(); else if (lc.mode == "vs") lcVsBegin(); else lcTeamBegin();
	}
	// back to the join screen (the same seats), from the pause or the end
	static void lcLeave()
	{
		stopMusic(); plStopMusic(); tmStopMusic(); actx.resume();
		player = null; hazards = new List<Hz>(); particles = new List<Particle>(); pl = null; tm = null; tmMe = tmView = -1; lcCur = -1;
		lc.playing = false; state = "lcJoin"; devPrime();
	}
	static void lcLeaveRate()
	{
		var L = lc.lv; bool ask = lc.won && lc.seats.Count > 0 && lc.seats[0].pc;
		lcLeave(); if (ask) rateAsk(L, null);
	}

	// ---------- the frame: true = this screen is local play's ----------
	public static bool lcStep(Inp inp, double now, double dt)
	{
		if (lc == null) return false;
		if (state == "lcJoin") { lcJoinStep(); lcMsgT -= dt; return true; }
		if (state == "lcAuth") { lcAuthStep(inp); return true; }
		if (!lc.playing) return false;
		foreach (var s in lc.seats) s.inp = readDev(s.dev);
		bool anyPz = inp.pz || lc.seats.Any(s => s.inp.pz);
		inp = lcAll(inp); lcMsgT -= dt;
		if (lc.mode == "boss") return lcBossFrame(inp, anyPz, now, dt);
		if (lc.mode == "vs") return lcVsFrame(inp, anyPz, now, dt);
		return lcTeamFrame(inp, anyPz, now, dt);
	}
	// the menus here (pause, win): every player's own presses on top of the usual ones
	static Inp lcAll(Inp inp)
	{
		var o = new Inp { x = inp.x, y = inp.y, confirm = inp.confirm, pz = inp.pz, back = inp.back, up = inp.up, down = inp.down, left = inp.left, right = inp.right, restart = inp.restart, any = inp.any, padAny = inp.padAny };
		foreach (var s in lc.seats) { var q = s.inp; o.confirm |= q.confirm; o.pz |= q.pz; o.back |= q.back; o.up |= q.up; o.down |= q.down; o.left |= q.left; o.right |= q.right; o.any |= q.any; }
		return o;
	}
	// a controller came out: the game stops for everyone (it was one of the players')
	public static void lcPadLost()
	{
		if (lc?.playing != true) return;
		if (state == "play") { state = "paused"; pauseSel = 0; actx.suspend(); }
		else if (state == "pltPlay") { state = "pltPause"; pauseSel = 0; actx.suspend(); }
		else if (state == "tmPlay" && tm?.won != true) { state = "tmPause"; pauseSel = 0; actx.suspend(); }
	}
	// the pause menus (any player's controller moves them)
	static int lcMenu(Inp inp, string[] list)
	{
		int pn = list.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
		pauseSel = Math.Clamp(pauseSel, 0, pn - 1);
		if (inp.pz || inp.back) return 0;
		return inp.confirm ? pauseSel : -1;
	}

	// ---------- the join screen's drawing ----------
	public static void drawLcJoin(double now)
	{
		bgShapes(now);
		txt("العب مع ربعك هنا", W / 2, 70, 48, "#fff");
		txt(lc.mode == "boss" ? lc.BL.name : lc.PL.name, W / 2, 120, 26, RED3, 700);
		txt(lc.mode == "team" ? $"تعاوني: {ar(lc.max)} لاعبين بالضبط، كل واحد بيده" : lc.mode == "vs" ? "ضد: من ٢ إلى ٤ لاعبين، كل واحد بيده" : "طور الزعيم: من ١ إلى ٤ لاعبين، كل واحد بيده", W / 2, 158, 22, "rgba(255,255,255,0.6)", 500);
		string dots = new string('.', 1 + (int)Math.Floor(now / 400) % 3);
		int slots = lc.max; double cw = 250, gap = 24, tot = slots * cw + (slots - 1) * gap;
		for (int i = 0; i < slots; i++)
		{
			double x = W / 2 + tot / 2 - cw / 2 - i * (cw + gap), top = 196, ch = 370; var s = lcSeat(i); string col = lcCol(i);
			rr(x - cw / 2, top, cw, ch, 20); g.fillStyle = s != null ? CARDS : CARD; g.fill(); g.strokeStyle = s != null ? col : "rgba(255,255,255,0.12)"; g.lineWidth = s != null ? 3 : 1.5; g.stroke();
			txt($"اللاعب {ar(i + 1)}", x, top + 32, 24, s != null ? col : "rgba(255,255,255,0.45)", 800);
			if (s == null)
			{
				g.globalAlpha = 0.25; g.save(); g.translate(x, top + 120); g.scale(2.6, 2.6); coopSquare(col, "", 1, now, "#1b0f26"); g.restore(); g.globalAlpha = 1;
				txt("اضغط ✕ بيدك" + dots, x, top + 210, 22, "rgba(255,255,255,0.5)", 600);
				continue;
			}
			g.save(); g.translate(x, top + 118); g.scale(2.6, 2.6); g.rotate(Math.Sin(now / 600 + i) * 0.12); coopSquare(col, s.sk ?? "", 1, now, "#1b0f26"); g.restore();
			txt(s.name, x, top + 186, 28, "#fff", 800);
			padLabel(devKind(s.dev), s.dev == KB ? "كيبورد" : devKind(s.dev) == "ps" ? "يد سوني" : "يد إكس بوكس", x, top + 222, "rgba(255,255,255,0.55)", 17);
			if (s.pc) { txt("حسابك بهذا الجهاز", x, top + 262, 20, NGOLD, 700); txt("ينحفظ لك كل شي", x, top + 292, 18, "rgba(255,255,255,0.5)", 500); }
			else
			{
				var rows = lcRows(s);
				for (int k = 0; k < rows.Length; k++)
				{
					double y = top + 262 + k * 33; bool sel = k == s.msel;
					if (sel) { rr(x - cw / 2 + 18, y - 16, cw - 36, 32, 10); g.fillStyle = "rgba(255,255,255,0.10)"; g.fill(); g.strokeStyle = col; g.lineWidth = 1.5; g.stroke(); }
					string lab = rows[k] == "ضيف" && s.guest ? "ضيف ✓" : rows[k];
					txt(lab, x, y, sel ? 22 : 20, sel ? "#fff" : "rgba(255,255,255,0.55)", 700);
				}
				if (!s.guest) { txt("ينحفظ بحسابه ✓", x, top + 312, 18, "#5fe08f", 700); txt("L1 R1 يغيّر شكله", x, top + 342, 16, "rgba(255,255,255,0.45)", 500); }
				else txt("الضيف ما ينحفظ له شي", x, top + 353, 15, "rgba(255,255,255,0.45)", 500);
			}
		}
		int n = lc.seats.Count; var K0 = BTN[lc.seats.Count > 0 ? devKind(lc.seats[0].dev) : kind];
		if (n >= lc.need) row(new[] { (K0.pause, "ابدأ (اللاعب ١)") }, W / 2, 610, 34, "#fff", 26);
		else txt(lc.mode == "team" ? $"تحتاجون {ar(lc.need)} لاعبين: كل واحد يضغط ✕ بيده" : "كل واحد يضغط ✕ بيده عشان يدخل", W / 2, 610, 26, "rgba(255,255,255,0.75)", 700);
		if (lcMsgT > 0 && lcMsg != null) { g.globalAlpha = clamp(lcMsgT / 0.4, 0, 1); txt(lcMsg, W / 2, 652, 24, NGOLD, 700); g.globalAlpha = 1; }
		row(new[] { (BTN[kind].confirm, "دخول"), (BTN[kind].back, "طلوع"), (new[] { "الأسهم" }, "اختيار") }, W / 2, H - 26, 22, "rgba(255,255,255,0.65)", 17);
	}

	// ---------- a seat signs in (the keyboard types) ----------
	static LcSeat lcAuthSeat; static bool lcAuthNew;
	public static bool lcTyping() => state == "lcAuth" && accSel < 2;
	static void lcAuthOpen(LcSeat s, bool isNew)
	{
		lcAuthSeat = s; lcAuthNew = isNew; state = "lcAuth"; accSel = 0; nameVal = ""; passVal = ""; tfValue = ""; tfField = ""; authMsg = ""; authErr = false;
	}
	static void lcAuthBack() { state = "lcJoin"; authMsg = ""; nameVal = passVal = tfValue = ""; devPrime(); }
	static void lcAuthStep(Inp inp)
	{
		if (lcAuthSeat == null || !lc.seats.Contains(lcAuthSeat)) { lcAuthBack(); return; }
		if (K("Tab")) { keys["Tab"] = false; accSel = (accSel + 1) % 3; }
		if (inp.down) accSel = Math.Min(2, accSel + 1); else if (inp.up) accSel = Math.Max(0, accSel - 1);
		else if (inp.confirm && !authBusy) { if (accSel == 0) accSel = 1; else _ = lcDoAuth(lcAuthSeat, lcAuthNew, nameVal, passVal); }
		else if (inp.pz || (inp.back && accSel > 1)) lcAuthBack();
	}
	public static void drawLcAuth(double now)
	{
		var s = lcAuthSeat; if (s == null) return;
		txt(lcAuthNew ? $"اللاعب {ar(s.i + 1)}: حساب جديد" : $"اللاعب {ar(s.i + 1)}: سجّل دخول بحسابك", W / 2, 82, 44, lcCol(s.i));
		txt(lcAuthNew ? "يسوي حسابه هنا، ويكمل عليه بجهازه بعدين" : "حرفه والـXP حقه ينحفظون بحسابه هو", W / 2, 138, 22, "rgba(255,255,255,0.6)", 500);
		field("الاسم", nameVal, 236, accSel == 0, false);
		field("كلمة السر", passVal, 352, accSel == 1, true);
		btnRow(lcAuthNew ? "سوّ الحساب" : "دخول", 456, accSel == 2);
		zone(W / 2 - 300, 204, 600, 64, null, null, () => accSel = 0); zone(W / 2 - 300, 320, 600, 64, null, null, () => accSel = 1);
		zone(W / 2 - 180, 426, 360, 60, null, "confirm", () => accSel = 2);
		if (authMsg != "") txt(authMsg, W / 2, 540, 22, authErr ? "#ff8a8a" : "rgba(255,255,255,0.7)", 500);
		else txt("دخوله لهذي الجلسة بس: أول ما يطلع من اللعب المحلي ينشال من جهازك", W / 2, 540, 20, "rgba(255,255,255,0.45)", 500);
		if (kind != "kb") padLabel(kind, "للكتابة تحتاج كيبورد", W / 2, H - 82, RED3, 20);
		row(new[] { (new[] { "الأسهم" }, "تنقّل"), (new[] { "Enter" }, "اختيار"), (new[] { "Esc" }, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// sign in / make the account (Firebase, the same calls as the account screen, but kept on the seat only: never saved on this PC)
	static async Task lcDoAuth(LcSeat s, bool signup, string name, string pass)
	{
		if (authBusy) return; name = name.Trim();
		if (!NAME_RE.IsMatch(name)) { authErr = true; authMsg = "الاسم من ٣ إلى ١٢ حرف أو رقم، بدون مسافات"; return; }
		if (pass.Length < 6) { authErr = true; authMsg = "كلمة السر لازم ٦ حروف أو أكثر"; return; }
		authBusy = true; authErr = false; authMsg = signup ? "جاري إنشاء الحساب…" : "جاري الدخول…";
		try
		{
			var j = await jpost(idUrl(signup ? "signUp" : "signInWithPassword"), new JsonObject { ["email"] = nameEmail(name), ["password"] = pass, ["returnSecureToken"] = true });
			var a = new Acc { uid = S(j["localId"]), name = name }; lcSetTok(a, j);
			if (lc == null || !lc.seats.Contains(s)) return;
			if (acc?.uid == a.uid || lc.seats.Any(q => q != s && q.acc?.uid == a.uid)) { authErr = true; authMsg = "هذا الحساب داخل من قبل"; return; }
			PlayerDoc doc;
			if (signup) { await lcCreatePlayer(a); doc = new PlayerDoc { uid = a.uid, name = a.name, n = new(), e = new(), nt = new(), et = new() }; }
			else
			{
				try { doc = parsePlayer(await jfetch($"{FS_URL}/players/{a.uid}", tok: a.it)); a.name = doc.name; }
				catch (FetchErr e) when (e.code == 404) { await lcCreatePlayer(a); doc = new PlayerDoc { uid = a.uid, name = a.name, n = new(), e = new(), nt = new(), et = new() }; }
			}
			if (lc == null || !lc.seats.Contains(s)) return;
			s.acc = a; s.doc = doc; s.sk = ""; s.msel = 0;
			lcAuthBack(); lcNote($"أهلًا {a.name}!");
		}
		catch (Exception e) { authErr = true; authMsg = errAr(e); }
		finally { authBusy = false; nameVal = passVal = tfValue = ""; }
	}
	static void lcSetTok(Acc a, JsonNode j)
	{
		a.it = S(j["idToken"]) is { Length: > 0 } x ? x : S(j["id_token"]); a.rt = S(j["refreshToken"]) is { Length: > 0 } y ? y : S(j["refresh_token"]);
		double ex = num(S(j["expiresIn"]) is { Length: > 0 } c ? c : S(j["expires_in"])); a.exp = dateNow() + (ex != 0 && !double.IsNaN(ex) ? ex : 3600) * 1000;
	}
	static async Task<string> lcIdToken(Acc a)
	{
		if (!string.IsNullOrEmpty(a.it) && dateNow() < a.exp - 60000) return a.it;
		var j = await jfetch($"https://securetoken.googleapis.com/v1/token?key={FB_KEY}", "POST", "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(a.rt), "application/x-www-form-urlencoded");
		lcSetTok(a, j); return a.it;
	}
	static JsonObject lcWrite(Acc a, JsonObject fields, List<string> mask, List<string> tsPaths, bool create)
	{
		var w = new JsonObject
		{
			["update"] = new JsonObject { ["name"] = $"{FS_DOC}/players/{a.uid}", ["fields"] = fields },
			["updateTransforms"] = new JsonArray(new[] { "at" }.Concat(tsPaths).Select(p => (JsonNode)new JsonObject { ["fieldPath"] = p, ["setToServerValue"] = "REQUEST_TIME" }).ToArray()),
			["currentDocument"] = new JsonObject { ["exists"] = !create },
		};
		if (mask != null) w["updateMask"] = new JsonObject { ["fieldPaths"] = new JsonArray(mask.Select(m => (JsonNode)m).ToArray()) };
		return w;
	}
	static async Task lcCommit(Acc a, params JsonObject[] writes) => await jpost($"{FS_URL}:commit", new JsonObject { ["writes"] = new JsonArray(writes) }, await lcIdToken(a));
	static async Task lcCreatePlayer(Acc a)
	{
		await lcCommit(a, new JsonObject { ["update"] = new JsonObject { ["name"] = $"{FS_DOC}/names/{nameKey(a.name)}", ["fields"] = new JsonObject { ["uid"] = new JsonObject { ["stringValue"] = a.uid } } }, ["currentDocument"] = new JsonObject { ["exists"] = false } },
			lcWrite(a, new JsonObject { ["name"] = new JsonObject { ["stringValue"] = a.name }, ["n"] = fsMap(new()), ["e"] = fsMap(new()), ["nt"] = fsMap(new(), true), ["et"] = fsMap(new(), true), ["xp"] = new JsonObject { ["integerValue"] = "0" } }, null, new List<string>(), true));
		board.at = 0;
	}

	// ---------- saving each one's finish ----------
	// this PC's account (player 1): exactly like playing alone (the device's best letter, points, XP, the online sync)
	static void lcPcWin(LcSeat s, Level lv, string rank, double t)
	{
		double p0 = ptsOf(false); xpMark();
		if (lv.plat) plSaveBest(lv, rank, t);
		else
		{
			int rb = lv.best != null ? Array.IndexOf(RANKS, lv.best) : 99, rn = Array.IndexOf(RANKS, rank);
			if (rn < rb) { lv.best = rank; localStorage.setItem("mg_best_" + lv.name, rank); markDirty(); }
			else if (acc != null && !acc.dirty) sync();
		}
		lastGain = ptsOf(false) - p0; awardXp(); s.xpGain = lastXp; s.saveSt = "ok";
	}
	// a seat signed in here: straight into its own account (a better letter only, the platformer's time, the XP its letters give)
	public static void lcFinish(LcSeat s, Level lv, string rank, double t = -1)
	{
		s.rank = rank;
		if (s.pc) { lcPcWin(s, lv, rank, t); return; }
		if (s.guest) { s.saveSt = null; return; }
		_ = lcSave(s, s.acc, lv, rank, t);
	}
	public static Func<int, int> lcRetryMs = k => k == 0 ? 21000 : 15000;   // tests: shorter
	static async Task lcSave(LcSeat s, Acc a, Level lv, string rank, double t)
	{
		s.saveSt = "saving"; t = lv.plat ? tRound(t) : -1;
		for (int k = 0; k < 40; k++)
		{
			bool gap = false;
			try
			{
				string tok = await lcIdToken(a); PlayerDoc on;
				try { on = parsePlayer(await jfetch($"{FS_URL}/players/{a.uid}", tok: tok)); }
				catch (FetchErr e) when (e.code == 404) { await lcCreatePlayer(a); on = new PlayerDoc { uid = a.uid, name = a.name, n = new(), e = new(), nt = new(), et = new() }; }
				string o = gv(on.n, lv.id); double ot = ptOf(on, lv.id);
				var mask = new List<string>(); var ts = new List<string>(); var fields = new JsonObject(); var nn = new Dictionary<string, string>(on.n);
				if (better(rank, o))
				{
					fields["n"] = fsMap(new() { [lv.id] = rank }); mask.Add($"n.{lv.id}"); ts.Add($"nt.{lv.id}"); nn[lv.id] = rank;
					if (lv.plat && t >= 0) { fields["pt"] = fsNum(new() { [lv.id] = t }); mask.Add($"pt.{lv.id}"); }
				}
				else if (lv.plat && rank == o && t >= 0 && (ot < 0 || t < ot)) { fields["pt"] = fsNum(new() { [lv.id] = t }); mask.Add($"pt.{lv.id}"); }
				int nx = xpOf(id => gv(nn, id), id => gv(on.e, id)), x0 = xpOf(id => gv(on.n, id), id => gv(on.e, id));
				if (nx > on.xp) { fields["xp"] = new JsonObject { ["integerValue"] = str(nx) }; mask.Add("xp"); }
				if (mask.Count == 0) { s.xpGain = 0; s.saveSt = nx > x0 ? "ok" : "same"; s.doc = on; return; }
				var w = lcWrite(a, fields, mask, ts, false);
				try { await lcCommit(a, w); }
				catch (FetchErr e) when (e.code == 403 && hasPt(w))
				{
					var again = (JsonObject)w.DeepClone(); dropPt(again);
					if (again["updateMask"]["fieldPaths"].AsArray().Count == 0) throw;
					await lcCommit(a, again);
				}
				on.n = nn; on.xp = Math.Max(on.xp, nx); s.doc = on; s.xpGain = Math.Max(0, nx - x0);
				s.saveSt = "ok"; board.at = 0; return;
			}
			catch (FetchErr e) { s.saveSt = e.Message == "NET" ? "net" : "err"; gap = e.code == 403; }
			catch (Exception) { s.saveSt = "err"; }
			await Task.Delay(lcRetryMs(gap ? 0 : 1));
			if (lc == null || !lc.seats.Contains(s) || s.acc != a) return;
		}
	}
	static string lcSaveLine(LcSeat s)
	{
		if (s.guest) return "ضيف: ما ينحفظ";
		return s.saveSt switch
		{
			"saving" => "جاري الحفظ…",
			"ok" => s.xpGain > 0 ? $"+{ar(s.xpGain)} XP  •  انحفظ ✓" : "انحفظ ✓",
			"same" => "عنده حرف أحسن من قبل ✓",
			"net" => "ما فيه نت، بيحفظ أول ما يرجع",
			"err" => "بجرب أحفظ بعد شوي…",
			_ => "",
		};
	}
	static string lcSaveCol(LcSeat s) => s.saveSt is "ok" or "same" ? "#5fe08f" : s.saveSt is "net" or "err" ? "#ffb35c" : "rgba(255,255,255,0.6)";
}
