using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 131-136 (song preview), 7718-7742 (screen transitions), the menu branches of draw() (7757-7769) and step() (7932-7990),
// and 7827-7892 (practice)
public static partial class G
{
	public static bool gatePassed = false;

	// ---------- level-list song preview ----------
	public static void pvUpdate(double now)
	{
		if (state == "pltList")
		{
			// a platformer level plays its song once it is finished (its name shows on the card)
			var P = plSel < PLATS.Count ? PLATS[plSel] : null; string pk = "plt" + str(plSel);
			if (pk != pv.sel) { pv.sel = pk; pv.at = now; pvStop(); }
			if (pv.src == null && P != null && P.best != null && P.bufs != null && P.bufs.All(b => b != null) && now - pv.at > 350 && actx.state == "running") { pvStartPlat(P); pv.had = true; }
			else if (pv.src == null && P != null && P.best == null && now - pv.at > 350 && actx.state == "running") { pvStartStatic(); pv.had = true; }   // ؟؟؟: static
			return;
		}
		if (state == "hcList")
		{
			// an open Hardcore level plays its song (played or not); a locked one plays the radio static like a ؟؟؟ platformer level
			var H = hcAt(hcSel); bool hu = hcUnlocked(hcSel); string hk = "hc" + str(hcSel) + (hu ? "u" : "");
			if (hk != pv.sel) { pv.sel = hk; pv.at = now; pvStop(); }
			if (pv.src == null && now - pv.at > 350 && actx.state == "running")
			{
				if (hu && H.buf != null && H.ok) { pvStart(H); pv.had = true; } else if (!hu) { pvStartStatic(); pv.had = true; }
			}
			return;
		}
		if (state != "levels" && state != "friends") { pvStop(); pv.sel = ""; pv.had = false; return; }
		var L = state == "levels" ? (lvlSel < LEVELS.Count ? LEVELS[lvlSel] : null) : (friSel < FRIENDS.Count ? FRIENDS[friSel] : null); string k = state + str(lvlSel) + ":" + str(friSel);
		if (k != pv.sel) { pv.sel = k; pv.at = now; pvStop(); }
		if (pv.src == null && L != null && L.buf != null && L.ok != false && now - pv.at > 350 && actx.state == "running") { pvStart(L); pv.had = true; }
	}

	// ---------- screen transitions: the old picture fades into the new screen's background, then the new screen fades in ----------
	static readonly string[] TR_ST = { "main", "levels", "modes", "bossMenu", "pltList", "pltOn", "pltJoin", "pltRoom", "howto", "settings", "friends", "points", "account", "board", "profile", "rpLoad", "news", "chal", "skins", "hcList", "myRp" };
	const double TR_OUT = 110, TR_IN = 170;
	static ImageTexture trSnap; static string trLast = ""; static bool trOn = false; static double trAt = -1e9;
	public static void trCheck(double now)
	{
		if (state == trLast) return; string from = trLast; trLast = state;
		bool go = TR_ST.Contains(state) || (state == "starting" && TR_ST.Contains(from));
		if (state == "levelIntro" && from == "starting") return;
		if (!go || from == "" || from == "gate" || from == "gateWait") { trOn = false; return; }
		// a copy of the canvas as it is now (the last frame drawn)
		var img = gfx.game.GetTexture().GetImage(); if (img == null) { trOn = false; return; }
		if (trSnap != null && trSnap.GetWidth() == img.GetWidth() && trSnap.GetHeight() == img.GetHeight()) trSnap.Update(img); else trSnap = ImageTexture.CreateFromImage(img);
		trAt = now; trOn = true;
	}
	static void trGrid() { g.strokeStyle = $"rgba({RGB},{str(0.04 + 0.08 * pulse)})"; g.lineWidth = 1; g.beginPath(); for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); } for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); } g.stroke(); }
	public static void trPost(double now)
	{
		if (!trOn) return; double el = now - trAt; if (el >= TR_OUT + TR_IN) { trOn = false; return; }
		bool mn = TR_ST.Contains(state); string bg = mn ? MBG : "#000"; double a;
		if (el < TR_OUT) { g.save(); g.setTransform(1, 0, 0, 1, 0, 0); g.drawTexture(trSnap.GetRid(), 0, 0, trSnap.GetWidth(), trSnap.GetHeight()); g.restore(); double k = el / TR_OUT; a = k * k * (3 - 2 * k); }
		else { double k = (el - TR_OUT) / TR_IN; a = 1 - k * k * (3 - 2 * k); }
		g.globalAlpha = a; g.fillStyle = bg; g.fillRect(-20, -20, W + 40, H + 40); if (mn) trGrid(); g.globalAlpha = 1;
	}

	// ---------- the menu screens of draw() ----------
	public static bool drawMenus(double now, bool inGame)
	{
		if (state is "gate" or "gateWait") drawGate(now);
		else if (state == "splash") drawSplash();
		else if (state == "news") drawNews(now);
		else if (state == "main") drawMain(now);
		else if (state == "levels") drawLevels(now);
		else if (state == "hcList") drawHcList(now);
		else if (state == "howto") drawHowto();
		else if (state == "settings") drawSettings();
		else if (state == "friends") drawFriends(now);
		else if (state == "points") drawPoints(now);
		else if (state == "account") drawAccount(now);
		else if (state == "board") drawBoard(now);
		else if (state == "chal") drawChal(now);
		else if (state == "skins") drawSkins(now);
		else if (state == "profile") drawProfile(now);
		else if (state == "rpLoad") drawRpLoad(now);
		else if (state == "myRp") drawMyRp(now);
		else if (state == "practice") drawPractice(now);
		else if (drawPlatScreens(now)) { }
		else return false;
		return true;
	}

	// ---------- the menu branches of step() ----------
	public static bool stepMenus(Inp inp, double now, double dt)
	{
		if (state == "news") stepNews(inp, now);
		else if (state == "main")
		{
			var MO = new[] { 5 }.Concat(mainItems().Select(m => m.id)).Concat(new[] { 4 }).ToArray(); int mn = MO.Length;
			if (menuSel == HC && !hcOpen()) menuSel = 0;   // the level went below 5 (logged out): the item is gone
			if (menuSel == ACC) { if (inp.down) menuSel = 0; else if (inp.up) menuSel = 4; else if (inp.right) menuSel = 5; }
			else
			{
				int mi = Array.IndexOf(MO, menuSel); if (inp.up) menuSel = MO[(mi + mn - 1) % mn]; else if (inp.down) menuSel = MO[(mi + 1) % mn];
				else if (inp.right && menuSel != 5) menuSel = 5; else if (inp.left) menuSel = ACC;
			}
			if (inp.confirm)
			{
				if (menuSel == 4) setEasy(!easy); else if (menuSel == 5) state = "points"; else if (menuSel == EXIT) quitApp();
				else if (menuSel == ACC) { state = "account"; accSel = 0; authMsg = ""; logoutArm = 0; }
				else if (menuSel == CHAL) { state = "chal"; chalView = 0; }
				else if (menuSel == HC) hcPress();
				else if (menuSel == MYRP) openMyRp("main");
				else if (menuSel == SKINSM) { state = "skins"; skinInfo = false; chalSel = Math.Max(0, Array.IndexOf(SKINS, skin)); }
				else if (menuSel == BOARD) { if (!classicUi) board.idx = 0; state = "board"; board.mode = easy ? "e" : "n"; board.sel = -1; _ = loadBoard(false); }
				else state = new[] { classicUi ? "levels" : "modes", "friends", "howto", "settings" }[menuSel];
			}
		}
		else if (state == "levels")
		{
			if (inp.left && lvlSel < LEVELS.Count - 1) lvlSel++;
			if (inp.right && lvlSel > 0) lvlSel--;
			if ((inp.sq || inp.restart) && !inp.confirm) { var L = LEVELS[lvlSel]; if (L.ok && L.buf != null) { if (inp.sq) openPractice(L, "levels"); else openBot(L, "levels"); } else shakeCard = 0.4; }
			else if (inp.confirm) { var L = LEVELS[lvlSel]; if (L.ok && L.buf != null) startLevel(L, "levels"); else shakeCard = 0.4; }
			else if (inp.back || inp.pz) state = classicUi ? "main" : "modes";
		}
		else if (state == "friends")
		{
			if (inp.left && friSel < FRIENDS.Count - 1) friSel++;
			if (inp.right && friSel > 0) friSel--;
			if ((inp.sq || inp.restart) && !inp.confirm) { var L = FRIENDS[friSel]; if (L.buf != null) { if (inp.sq) openPractice(L, "friends"); else openBot(L, "friends"); } else shakeCard = 0.4; }
			else if (inp.confirm) { var L = FRIENDS[friSel]; if (L.buf != null) startLevel(L, "friends"); else shakeCard = 0.4; }
			else if (inp.back || inp.pz) state = "main";
		}
		else if (state == "chal") stepChal(inp, dt);
		else if (state == "myRp") stepMyRp(inp, dt);
		else if (state == "hcList") stepHcList(inp);
		else if (stepPlat(inp, now, dt)) { }
		else if (state == "skins") stepSkins(inp, dt);
		else if (state == "howto") { if (inp.back || inp.pz || inp.confirm) state = "main"; }
		else if (state == "points") { if (inp.back || inp.pz || inp.confirm) state = "main"; }
		else if (state == "account")
		{
			if (acc != null) { logoutArm = Math.Max(0, logoutArm - dt); if (inp.confirm) { if (logoutArm > 0) { logout(); logoutArm = 0; accSel = 0; authMsg = ""; } else logoutArm = 3; } else if (inp.back || inp.pz) state = "main"; }
			else
			{
				if (K("Tab")) { keys["Tab"] = false; accSel = (accSel + 1) % 4; }
				if (inp.down) accSel = Math.Min(3, accSel + 1); else if (inp.up) accSel = Math.Max(0, accSel - 1);
				else if (inp.confirm && !authBusy) { if (accSel == 0) accSel = 1; else if (accSel == 1 || accSel == 2) _ = doAuth(false, nameVal, passVal); else _ = doAuth(true, nameVal, passVal); }
				else if (inp.pz || (inp.back && accSel > 1)) { state = "main"; authMsg = ""; }
			}
		}
		else if (state == "board")
		{
			int n = ALL_LV().Count + bOff;
			if (inp.left && board.idx < n) { board.idx++; board.sel = -1; } else if (inp.right && board.idx > 0) { board.idx--; board.sel = -1; }
			if (!boardModes().Contains(board.mode)) board.mode = boardXp ? "x" : easy ? "e" : "n";
			var vis = boardVisible(); if (board.sel < 0 || board.sel >= vis.Count) { int mi = vis.FindIndex(x => x.me); board.sel = mi >= 0 ? mi : 0; }
			if (inp.down && board.sel < vis.Count - 1) board.sel++; else if (inp.up && board.sel > 0) board.sel--;
			if (inp.restart) { var ms = boardModes(); board.mode = ms[(Array.IndexOf(ms, board.mode) + 1) % ms.Length]; board.sel = -1; }
			else if (inp.confirm && vis.Count > 0) openProfile(vis[board.sel].p);
			else if (inp.back || inp.pz) state = "main";
			if (!board.busy && performance.now() - board.at > 60000) _ = loadBoard(false);
		}
		else if (state is "settings" or "pauseSet" or "pltSet" or "tmSet")
		{
			var rows = setRows(); if (setSel >= rows.Length) setSel = 0; string r = rows[setSel]; int d = inp.right ? 1 : inp.left ? -1 : 0;
			if (!freeRow(r)) holdD = 0;
			if (numRow != null) { if (inp.padAny) numRow = null; }
			else if (inp.up) setSel = (setSel + rows.Length - 1) % rows.Length; else if (inp.down) setSel = (setSel + 1) % rows.Length;
			else if (freeRow(r)) { if (!freeStep(inp, fdt, r) && (inp.back || inp.pz || inp.confirm)) state = setBack(); }
			else if (d != 0 && r == "vol") setVol(vol + 0.1 * d);
			else if ((d != 0 || inp.confirm) && r == "screen") { int i = Array.IndexOf(SCREEN_MODES, screenMode), sn = SCREEN_MODES.Length; setScreen(SCREEN_MODES[((i < 0 ? 0 : i) + (d != 0 ? d : 1) + sn) % sn]); }
			else if ((d != 0 || inp.confirm) && r == "vsync") setVsync(!vsyncOn);
			else if ((d != 0 || inp.confirm) && r == "counter") setFpsShow(!fpsShow);
			else if (d != 0 && r == "fps") { int i = Array.IndexOf(FPS_LIST, fpsCap); if (i < 0) i = 4; setFps(FPS_LIST[(int)clamp(i + d, 0, FPS_LIST.Length - 1)]); }
			else if (d != 0 && r == "res") { var L = resList(); int i = Array.IndexOf(L, resSet); if (i < 0) i = 0; setRes(L[(i + L.Length + d) % L.Length]); }
			else if (inp.back || inp.pz || inp.confirm) state = setBack();
		}
		else if (state == "profile")
		{
			int n = ALL_LV().Count; prof.msgT -= dt; { double t = profScT(); prof.sc += (t - prof.sc) * Math.Min(1, dt * 14); if (Math.Abs(t - prof.sc) < 0.002) prof.sc = t; }
			if (inp.down && prof.row < n - 1) prof.row++; else if (inp.up && prof.row > 0) prof.row--;
			if (inp.left && prof.col == 0 && !noEz(ALL_LV()[prof.row])) prof.col = 1; else if (inp.right && prof.col == 1) prof.col = 0;
			if (noEz(ALL_LV()[prof.row])) prof.col = 0;
			if (inp.confirm)
			{
				var L = ALL_LV()[prof.row]; string m = prof.col != 0 ? "e" : "n", r = gv(prof.p.F(m), L.id);
				if (hcHidden(L)) { prof.msg = "افتح هذي المرحلة في الهاردكور أول"; prof.msgT = 2.2; }
				else if (r != null && !L.plat && hasRp(prof.p, L, m)) openReplay(prof.p, L, m); else { prof.msg = r == null ? "ما خلّص هذي المرحلة للحين" : prof.busy && !L.plat ? "لحظة، جاري التحميل…" : "ما فيه Replay لهذي المرحلة"; prof.msgT = 2.2; }
			}
			else if (inp.back || inp.pz) state = "board";
		}
		else return false;
		return true;
	}
	// the menu's "خروج" (window.close() in the PC app)

	// ---------- practice ----------
	public class PrState { public bool on, inf = true, ghost = true; public Level L, lastL; public string from = "levels", let = ""; public double t, spd = 1; public int lv = 5, sel; public RpDoc doc; }
	public static PrState pr = new();
	static readonly string[] PR_LET = { "", "S", "A", "B", "C" };
	static readonly Dictionary<string, int> PR_LETLV = new() { [""] = 0, ["S"] = 1, ["A"] = 5, ["B"] = 10, ["C"] = 0 };
	const int PR_LVMAX = 10;
	static readonly double[] PR_SPD = { 1, 0.75, 0.5 };
	// where the settings go back to: the main menu, the pause menu, or the platformer's pause menu
	static string setBack() => state == "pauseSet" ? "paused" : state == "pltSet" ? "pltPause" : state == "tmSet" ? "tmPause" : "main";
	static void openPractice(Level L, string from) { if (pr.lastL != L) { pr.t = 0; pr.lastL = L; } pr.L = L; pr.from = from; pr.doc = botDoc(L, easy ? "e" : "n"); pr.sel = 0; cur = L; fromList = from; levelTiming(L); state = "practice"; }
	static string[] prRows() => pr.doc != null ? new[] { "t", "spd", "let", "inf", "ghost", "go" } : new[] { "t", "spd", "let", "inf", "go" };
	static double prMaxT() => Math.Max(0, Math.Floor((B(pr.L.end) - 4) / 5) * 5);
	public static void prMenuInput(Inp inp)
	{
		var rows = prRows(); if (pr.sel >= rows.Length) pr.sel = 0; string r = rows[pr.sel]; int d = inp.right ? 1 : inp.left ? -1 : 0;
		if (inp.up) pr.sel = (pr.sel + rows.Length - 1) % rows.Length; else if (inp.down) pr.sel = (pr.sel + 1) % rows.Length;
		else if (inp.restart) { double? nx = null; foreach (var c in pr.L.cps) { double bc = B(c); if (bc > pr.t + 0.5) { nx = bc; break; } } pr.t = nx == null ? 0 : round(nx.Value * 10) / 10; pr.sel = 0; }
		else if (d != 0 && r == "t") pr.t = clamp(d > 0 ? Math.Floor(pr.t / 5 + 1e-6) * 5 + 5 : Math.Ceiling(pr.t / 5 - 1e-6) * 5 - 5, 0, prMaxT());
		else if (d != 0 && r == "spd") pr.spd = PR_SPD[(int)clamp(Array.IndexOf(PR_SPD, pr.spd) - d, 0, PR_SPD.Length - 1)];
		else if ((d != 0 || inp.confirm) && r == "let") { int i = Array.IndexOf(PR_LET, pr.let), n = PR_LET.Length; pr.let = PR_LET[(i + (d != 0 ? d : 1) + n) % n]; int v = pr.let == "A" ? maxHp(pr.L) : PR_LETLV[pr.let]; pr.inf = v == 0; if (v != 0) pr.lv = v; }
		else if (d != 0 && r == "inf") { int v = pr.inf ? 0 : pr.lv; v = (v + d + PR_LVMAX + 1) % (PR_LVMAX + 1); pr.inf = v == 0; if (v != 0) pr.lv = v; }
		else if ((d != 0 || inp.confirm) && r == "ghost") pr.ghost = !pr.ghost;
		else if (inp.confirm) prGo();
		else if (inp.back || inp.pz) state = pr.from;
	}
	public static void prGo()
	{
		stopMusic(); pr.on = true; spd = pr.spd; cur = pr.L; levelTiming(pr.L); hitsTotal = 0; deaths = 0; rec = null; checkpoint = 0; startBeat = 0;
		player = null; hazards = new List<Hz>(); particles = new List<Particle>(); state = "prStarting"; audioHint = false; startReqAt = performance.now(); ensureAudio();
	}
	// starts from second pr.t: the level runs from its start, fast and silent, so the attacks are exactly as they are at that second
	public static void prBegin()
	{
		var d = pr.doc; double T = pr.t;
		int seed = d != null ? d.segs[0].seed : 777; noMus = true; beginRun(0, seed); noMus = false;
		var p = player; double end = T > 0 ? T - 0.12 * spd : -1; dry = true;
		// the same fixed steps as real play (v41), so the attacks are exactly where they'd be
		while (sim0 + (simN + 1) * STEP <= end + 1e-9) { simN++; double t = sim0 + simN * STEP; songT = t; var q = botPos(d, t); if (q != null) { p.x = q.x; p.y = q.y; } fireEvents(t); stepHazards(t, STEP); hazards = hazards.Where(h => !h.dead).ToList(); }
		dry = false; particles = new List<Particle>(); shake = 0; flash = 0; gloom = gloomT;
		if (T > 0) { var q = botPos(d, T); if (q != null) { p.x = q.x; p.y = q.y; } }
		p.inv = 1.0; p.trail = new List<TrailPt>(); if (!pr.inf) p.hp = pr.lv; checkpoint = 0; double bt = BT(T); foreach (var c in cur.cps) if (bt >= c) checkpoint = c;
		playMusic(T); songT = sim0 + simN * STEP; state = "play";
		rec = new RecState(); rec.cur = new RecSeg { cp = 0, seed = seed, t0 = null, n = 0, fr = T }; rec.segs.Add(rec.cur); prHitRec = false;   // v46: for "احفظ الريبلاي" (on the PC only)
	}
	public static void prFinish() { stopMusic(); state = "prEnd"; winAt = performance.now(); }
	static void prStop() { stopMusic(); pr.on = false; spd = 1; player = null; hazards = new List<Hz>(); particles = new List<Particle>(); gloom = gloomT = 0; actx.resume(); }
	public static void prToMenu() { prStop(); pr.sel = 0; state = "practice"; }
	public static void prPauseInput(Inp inp)
	{
		int pn = prPauseList().Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
		if (inp.pz || inp.back) resume();
		else if (inp.confirm)
		{
			if (pauseSel == 0) resume(); else if (pauseSel == 1) prGo(); else if (pauseSel == 2) prToMenu(); else if (pauseSel == 3) { state = "pauseSet"; setSel = 0; }
			else if (pn > 5 && pauseSel == 4) myRpSave(rec, "mid", null);   // v46: the practice so far, on the PC
			else if (pn > 5 && pauseSel == 5) openMyRp("paused");
			else { prStop(); state = pr.from; }
		}
	}
	static string mmss(double t) { t = Math.Max(0, t); double m = Math.Floor(t / 60), s = Math.Floor(t % 60); return str(m) + ":" + (s < 10 ? "0" : "") + str(s); }
	static void drawPractice(double now)
	{
		bgShapes(now); var L = pr.L; var K = BTN[kind];
		txt("تدريب", W / 2, 62, 46, "#fff");
		if (isAscii(L.name)) ltxt(L.name, W / 2, 110, 24, RED3); else txt(L.name, W / 2, 110, 26, RED3, 800);
		if (easy) ezBadge(W / 2, 140, 0.9);
		var rows = prRows(); double cx = W / 2 - 150, lx = W / 2 + 350;
		for (int i = 0; i < rows.Length; i++)
		{
			string k = rows[i]; double y = 190 + i * 64; bool sel = i == pr.sel; int ii = i;
			if (k == "go") zone(W / 2 - 150, y - 28, 300, 56, () => pr.sel = ii);
			else
			{
				zone(W / 2 - 390, y - 28, 780, 56, () => pr.sel = ii, k is "let" or "ghost" ? "confirm" : null);
				if (k != "ghost") { zone(cx - 164, y - 24, 42, 48, () => pr.sel = ii, "left"); zone(cx + 122, y - 24, 42, 48, () => pr.sel = ii, "right"); }
			}
			string lc = sel ? "#fff" : "rgba(255,255,255,0.55)", vc = sel ? RED : "rgba(255,255,255,0.7)";
			if (k == "go")
			{
				double w = 300; rr(W / 2 - w / 2, y - 28, w, 56, 28); g.fillStyle = sel ? RED : $"rgba({RGB},0.12)"; if (sel) { g.shadowColor = RED; g.shadowBlur = 16 * (0.6 + 0.6 * pulse); } g.fill(); g.shadowBlur = 0;
				g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); txt("ابدأ التدريب", W / 2, y, 30, sel ? "#fff" : "rgba(255,255,255,0.75)", 800); continue;
			}
			if (sel) { rr(W / 2 - 390, y - 28, 780, 56, 16); g.fillStyle = $"rgba({RGB},{str(0.08 + 0.08 * pulse)})"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			if (k == "t") { txt("من وين تبدأ", lx, y, 30, lc, 800, "right"); optArrows(cx, y, sel); ltxt(pr.t < 0.05 ? "0:00" : mmss(pr.t), cx, y, 30, vc); }
			else if (k == "spd") { txt("السرعة", lx, y, 30, lc, 800, "right"); optArrows(cx, y, sel); txt(ar(pr.spd * 100) + "٪", cx, y, 30, vc, 800); }
			else if (k == "let") { txt("الحرف", lx, y, 30, lc, 800, "right"); optArrows(cx, y, sel); if (pr.let != "") ltxt(pr.let, cx, y + 2, 34, sel ? (easy ? "#7cc8ff" : "#ff2436") : "rgba(255,255,255,0.7)"); else txt("بدون", cx, y, 30, vc, 800); }
			else if (k == "inf") { txt("الأرواح", lx, y, 30, lc, 800, "right"); optArrows(cx, y, sel); if (pr.inf) txt("ما تخلص", cx, y, 30, vc, 800); else ltxt(str(pr.lv), cx, y + 2, 30, vc); }
			else if (k == "ghost") { txt("شبح البوت", lx, y, 30, lc, 800, "right"); checkBox(cx, y, pr.ghost, sel); }
		}
		// the level bar: where you will start, and the checkpoints
		{
			double x0 = 220, x1 = W - 220, y = H - 128, endT = B(L.end), px = x0 + (x1 - x0) * clamp(pr.t / endT, 0, 1);
			g.fillStyle = "rgba(255,255,255,0.12)"; rr(x0, y - 3, x1 - x0, 6, 3); g.fill();
			g.fillStyle = CYAN; rr(x0, y - 3, Math.Max(6, px - x0), 6, 3); g.globalAlpha = 0.35; g.fill(); g.globalAlpha = 1;
			foreach (var c in L.cps.Skip(1)) { double cx2 = x0 + (x1 - x0) * B(c) / endT; g.fillStyle = PINK; g.beginPath(); g.moveTo(cx2, y - 14); g.lineTo(cx2 - 8, y - 5); g.lineTo(cx2 + 8, y - 5); g.closePath(); g.fill(); }
			g.save(); g.translate(px, y); g.rotate(now / 420); g.shadowColor = CYAN; g.shadowBlur = 12; g.fillStyle = CYAN; g.fillRect(-8, -8, 16, 16); g.restore();
			ltxt("0:00", x0 - 14, y, 18, "rgba(255,255,255,0.45)", "right"); ltxt(mmss(endT), x1 + 14, y, 18, "rgba(255,255,255,0.45)", "left");
		}
		string r = rows[pr.sel]; string hint = "";
		if (r == "t") row(new[] { (K.move, "٥ ثواني"), (K.restart, "نقطة الحفظ الجاية") }, W / 2, H - 82, 22, "rgba(255,255,255,0.6)", 17);
		else
		{
			if (r == "spd") hint = "أبطأ = تشوف الهجمات بوضوح، بس الأغنية تصير أثقل";
			else if (r == "let") hint = pr.let switch { "" => "اختار الحرف اللي تتدرب عليه، والأرواح تتعدل معه", "S" => "S: ولا ضربة، أول ضربة ترجعك لنقطة التدريب", "A" => "A: تنضرب بس ما تخسر (5 أرواح)", "B" => "B: تقدر تخسر مرة وحدة بس (10 أرواح)", _ => "C: تكمل المرحلة وبس، الأرواح ما تخلص" };
			else if (r == "inf") hint = pr.inf ? "الضربة تومض بس، وتكمل لين آخر المرحلة" : (pr.lv == 1 ? "روح وحدة" : ar(pr.lv) + " أرواح") + "، ولو خلصت ترجع لنقطة التدريب";
			else if (r == "ghost") hint = "مربع أبيض شفاف يمشي طريق الـ S، ونقاط توريك وين بيروح";
			else hint = "التدريب ما ينحسب له تقييم ولا نقاط";
			txt(hint, W / 2, H - 82, 22, "rgba(255,255,255,0.6)", 500);
		}
		row(new[] { (K.move, "غيّر"), (K.confirm, "ابدأ"), (K.back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
}
