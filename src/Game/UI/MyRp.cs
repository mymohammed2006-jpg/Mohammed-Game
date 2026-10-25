using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static MG.JS;

namespace MG;

// New in v46 (Mohammed 10-22): replays kept on the player's own PC (user://replays, one file each), never sent to the server.
// - Saved from the win screen, the lose screen (the run so far, its death included) and the pause menu (the run up to now).
// - "ريبلاياتي" lists them: in the main menu and in the pause menu. Watch one, or delete it (press twice).
// - Watching from the pause menu leaves the run (asks first).
// With classicUi (the comparison tools) none of this shows.
public static partial class G
{
	const int MYRP = 12;
	public static string MYRP_DIR = "user://replays";   // tests use another folder
	public class MyRp
	{
		public string kind { get; set; } = "dodge";
		public string lvl { get; set; }
		public string m { get; set; }
		public string r { get; set; }       // the letter (a win); null when lost or not finished
		public string res { get; set; }     // "win", "dead" (lost) or "mid" (saved from the pause menu)
		public int v { get; set; }
		public int hz { get; set; }
		public string sk { get; set; }
		public string name { get; set; }
		public long at { get; set; }        // when it was saved (ms since 1970)
		public int hits { get; set; }
		public int deaths { get; set; }
		public double len { get; set; }     // seconds of play
		public List<RpqSeg> segs { get; set; }
		// the platformer (kind "plat"): the pieces taken and saves reached ([second, 0 piece / 1 save, x, y]), the other square online
		public List<double[]> ev { get; set; }
		public List<RpqSeg> gs { get; set; }
		public string gname { get; set; }
		public string gsk { get; set; }
		// practice (v46, his يالله 19:07 10-22): its start second, its speed, its lives (0 = never end)
		public bool pr { get; set; }
		public double prT { get; set; }
		public double spd { get; set; }
		public int prLv { get; set; }
		// the co-op level (kind "team", Plat/TeamRp.cs): whose screen it was, the three names, its messages
		public int tmSeat { get; set; }
		public string[] tmNames { get; set; }
		public string[] tmSkins { get; set; }   // each one's look (10-25); none in older ones: the plain squares
		public List<string> tmStr { get; set; }
		public string file;
	}
	public static RecState lastRec;          // the win's recording (rpWin lets go of rec)
	static List<MyRp> myRps = new();
	static int myRpSel; static double myRpScr; static string myRpBack = "main", myRpMsg = ""; static double myRpMsgT; static int myRpArm = -1; static string myRpArmK = "";
	static double myRpSavedFor = -1;         // the win/lose screen (its winAt/deadAt) whose replay is already saved

	static string myRpPath(string f) => MYRP_DIR + "/" + f;
	// saves a recording; true when it worked
	public static bool myRpSave(RecState r, string res, string letter)
	{
		if (r == null || cur == null) return false;
		var segs = rpSegs(r.segs); if (segs.Count == 0) { showCorner("ما فيه شي ينحفظ للحين", "#ffb35c", 2400); return false; }
		var it = new MyRp { r = res == "win" ? letter : null };
		if (pr.on) { it.pr = true; it.prT = pr.t; it.spd = spd; it.prLv = pr.inf ? 0 : pr.lv; it.r = null; }
		return myRpWrite(it, cur.id, easy ? "e" : "n", res, segs);
	}
	static bool myRpWrite(MyRp it, string lvl, string m, string res, List<RpqSeg> segs, int? hits = null, int? dies = null)
	{
		long at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		it.lvl = lvl; it.m = m; it.res = res; it.v = RP_V; it.hz = it.hz != 0 ? it.hz : RP_HZ; it.sk = classicUi ? "" : skin; it.name = acc?.name ?? ""; it.at = at; it.hits = hits ?? (int)hitsTotal; it.deaths = dies ?? (int)deaths; it.len = segs.Sum(s => (s.n - 1) / (double)it.hz); it.segs = segs;
		try
		{
			Godot.DirAccess.MakeDirRecursiveAbsolute(Godot.ProjectSettings.GlobalizePath(MYRP_DIR));
			string fn = $"{at}_{lvl}.json"; using (var f = Godot.FileAccess.Open(myRpPath(fn), Godot.FileAccess.ModeFlags.Write)) { if (f == null) throw new Exception("open"); f.StoreString(JsonSerializer.Serialize(it, JOPT)); }
			showCorner("انحفظ الريبلاي بجهازك ✓", "#5fe08f", 2600); return true;
		}
		catch (Exception e) { Godot.GD.PushWarning("replay save: " + e.Message); showCorner("ما قدرت أحفظ الريبلاي", "#ffb35c", 3000); return false; }
	}
	static void myRpLoad()
	{
		var o = new List<MyRp>();
		try
		{
			using var d = Godot.DirAccess.Open(MYRP_DIR);
			if (d != null)
				foreach (var fn in d.GetFiles().Where(x => x.EndsWith(".json")))
				{
					try { using var f = Godot.FileAccess.Open(myRpPath(fn), Godot.FileAccess.ModeFlags.Read); var it = JsonSerializer.Deserialize<MyRp>(f.GetAsText()); if (it?.segs != null && it.lvl != null) { it.file = fn; o.Add(it); } }
					catch (Exception e) { Godot.GD.PushWarning("replay file " + fn + ": " + e.Message); }
				}
		}
		catch (Exception e) { Godot.GD.PushWarning("replays: " + e.Message); }
		myRps = o.OrderByDescending(x => x.at).ToList();   // the newest first
	}
	public static void myRpPlayPub(MyRp it) => myRpPlay(it); public static void myRpLoadPub() => myRpLoad(); public static List<MyRp> myRpsPub() => myRps;   // the tests
	public static void openMyRp(string back) { myRpLoad(); myRpBack = back; myRpSel = 0; myRpScr = 0; myRpArm = -1; myRpMsgT = 0; state = "myRp"; }
	static Level myRpLevel(MyRp it) => it.kind is "plat" or "team" ? PLATS.FirstOrDefault(x => x.id == it.lvl)?.lv : ALL_LV().Concat(HCLV).FirstOrDefault(x => x.id == it.lvl);
	static bool myRpInRun() => myRpBack is "paused" or "pltPause";
	// what a replay ended with, when it isn't a letter
	public static string rpResText(string res) => res switch { "dead" => "خسارة", "vsWin" => "فاز", "vsLose" => "خسر", "pdone" => "خلّص التدريب", _ => "ما خلّصها" };
	public static string rpResCol(string res) => res is "dead" or "vsLose" ? PINK : res == "vsWin" ? GOLD : "rgba(255,255,255,0.7)";

	static void myRpPlay(MyRp it)
	{
		var L = myRpLevel(it); if (L == null) { myRpSay("هذي المرحلة مو موجودة في هذي النسخة"); return; }
		if (it.kind == "team")
		{
			// the co-op level (Plat/TeamRp.cs): read before leaving anything
			if (!tmRpLoad(it, PLATS.FirstOrDefault(x => x.id == it.lvl))) { myRpSay("ملف هذا الريبلاي خربان"); return; }
			rp.p = new RpPerson { name = string.IsNullOrEmpty(it.name) ? "أنت" : it.name, uid = "" }; rp.L = L; rp.m = "n"; rp.err = false; rp.bot = false; rp.local = true; rp.plat = true; rp.team = true; rp.pr = false; rp.rate0 = 1; rp.back = "myRp"; state = "rpLoad"; ensureAudio();
			return;
		}
		RpDoc d;
		try { d = new RpDoc { r = it.r, res = it.res, v = it.v != 0 ? it.v : RP_V, hz = it.hz != 0 ? it.hz : RP_HZ, sk = Array.IndexOf(SKINS, it.sk) > 0 ? it.sk : "", segs = it.segs.Select(s => new RpSeg { cp = s.cp, seed = s.seed, t0 = s.t0, n = s.n, b = unpackRp(s.d), c = s.c != 0, fr = it.pr ? s.t0 : -1 }).ToList() }; }
		catch (Exception) { myRpSay("ملف هذا الريبلاي خربان"); return; }
		bool plat = it.kind == "plat"; PlatLevel P = plat ? PLATS.FirstOrDefault(x => x.id == it.lvl) : null;
		if (plat && (P == null || it.ev == null)) { myRpSay("ملف هذا الريبلاي خربان"); return; }
		if (myRpBack == "paused" && pr.on) { prStop(); myRpBack = pr.from; }   // leaves the practice
		else if (myRpBack == "paused") { stopMusic(); player = null; hazards = new List<Hz>(); particles = new List<Particle>(); actx.resume(); myRpBack = fromList; }   // leaves the run
		else if (myRpBack == "pltPause") { plStopMusic(); player = null; pl = null; particles = new List<Particle>(); actx.resume(); myRpBack = "pltList"; }
		if (plat)
		{
			// the platformer: the parts' times count from 0 on the bar, their real clock is kept apart
			plrp = new PlRpData { L = P, t0 = d.segs.Select(s => s.t0).ToArray(), ev = it.ev.OrderBy(e => e[0]).ToList(), gname = it.gname, gsk = Array.IndexOf(SKINS, it.gsk) > 0 ? it.gsk : "" };
			try { plrp.gs = it.gs?.Select(s => new RpSeg { t0 = 0, n = s.n, b = unpackRp(s.d) }).ToList(); } catch (Exception) { plrp.gs = null; }
			foreach (var s in d.segs) s.t0 = 0;
		}
		rp.p = new RpPerson { name = string.IsNullOrEmpty(it.name) ? "أنت" : it.name, uid = "" }; rp.L = L; rp.m = it.m; rp.doc = d; rp.err = false; rp.bot = false; rp.local = true; rp.plat = plat; rp.back = "myRp"; state = "rpLoad"; ensureAudio();
		rp.pr = it.pr; rp.prLv = it.prLv; rp.rate0 = it.pr && it.spd > 0 ? it.spd : 1; rp.prBot = it.pr ? botDoc(L, it.m) : null;
	}
	static void myRpDelete(MyRp it)
	{
		try { Godot.DirAccess.RemoveAbsolute(Godot.ProjectSettings.GlobalizePath(myRpPath(it.file))); } catch (Exception) { }
		myRps.Remove(it); myRpSel = Math.Min(myRpSel, Math.Max(0, myRps.Count - 1)); myRpSay("انحذف");
	}
	static void myRpSay(string s) { myRpMsg = s; myRpMsgT = 2.4; }

	const int MYRP_VIS = 8; const double MYRP_RH = 54, MYRP_TOP = 190;
	static void drawMyRp(double now)
	{
		if (myRpInRun()) overlay(0.95); else bgShapes(now);
		txt("ريبلاياتي", W / 2, 80, 52, "#fff");
		var K = BTN[kind];
		if (myRps.Count == 0)
		{
			txt("ما عندك ريبلايات محفوظة للحين", W / 2, H / 2 - 20, 32, "rgba(255,255,255,0.75)", 800);
			txt("تقدر تحفظ الريبلاي لما تفوز أو تخسر، أو من قائمة الإيقاف داخل المرحلة", W / 2, H / 2 + 30, 21, "rgba(255,255,255,0.5)", 500);
			row(new[] { (K.back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
			return;
		}
		{ double t = clamp(Math.Min(myRpSel, Math.Max(myRpScr, myRpSel - MYRP_VIS + 1)), 0, Math.Max(0, myRps.Count - MYRP_VIS)); myRpScr += (t - myRpScr) * Math.Min(1, fdt * 14); if (Math.Abs(t - myRpScr) < 0.002) myRpScr = t; }
		double lx = W / 2 + 380, rx = W / 2 + 10, dx = W / 2 - 190, tx = W / 2 - 340, y0 = MYRP_TOP;
		g.font = $"500 17px {FONT}";
		txt("المرحلة", lx, y0 - 42, 17, "rgba(255,255,255,0.45)", 500, "right"); txt("النتيجة", rx, y0 - 42, 17, "rgba(255,255,255,0.45)", 500);
		txt("التاريخ", dx, y0 - 42, 17, "rgba(255,255,255,0.45)", 500); txt("المدة", tx, y0 - 42, 17, "rgba(255,255,255,0.45)", 500);
		g.save(); g.beginPath(); g.rect(W / 2 - 410, y0 - MYRP_RH / 2, 820, MYRP_VIS * MYRP_RH); g.clip();
		for (int i = 0; i < myRps.Count; i++)
		{
			var it = myRps[i]; double y = y0 + (i - myRpScr) * MYRP_RH; if (y < y0 - MYRP_RH || y > y0 + MYRP_VIS * MYRP_RH) continue;
			bool sel = i == myRpSel, ez = it.m == "e"; int ii = i;
			if (y - MYRP_RH / 2 >= y0 - MYRP_RH / 2 - 1 && y + MYRP_RH / 2 <= y0 - MYRP_RH / 2 + MYRP_VIS * MYRP_RH + 1) zone(W / 2 - 400, y - MYRP_RH / 2 + 3, 800, MYRP_RH - 6, () => { if (myRpSel != ii) myRpArm = -1; myRpSel = ii; });
			rr(W / 2 - 400, y - MYRP_RH / 2 + 3, 800, MYRP_RH - 6, 12); g.fillStyle = sel ? $"rgba({RGB},0.12)" : "rgba(255,255,255,0.035)"; g.fill();
			if (sel) { g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			var L = myRpLevel(it); string nm = L != null ? L.name : it.lvl;
			{ double fs = 23; g.font = $"{(sel ? 800 : 500)} {str(fs)}px {FONT}"; while (fs > 15 && g.measureText(nm).width > 330) { fs--; g.font = $"{(sel ? 800 : 500)} {str(fs)}px {FONT}"; } txt(nm, lx, y + 1, fs, sel ? "#fff" : "#e9e6f5", sel ? 800 : 500, "right"); }
			if (it.res == "win")
			{
				string c = ez ? "#7cc8ff" : "#ff2436"; g.font = $"28px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = c; g.fillText(it.r ?? "", ez ? rx + 36 : rx, y + 2); g.direction = "rtl";
				if (ez) ezBadge(rx - 22, y + 1, 0.7);
			}
			else if (it.pr)
			{
				// practice: what happened, where it started and its speed
				txt(it.res == "pdone" ? "تدريب" : "تدريب · " + rpResText(it.res), rx, y - 9, 19, it.res == "dead" ? PINK : "#fff", 800);
				g.font = $"14px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = "rgba(255,255,255,0.6)";
				g.fillText(mmss(it.prT) + (it.spd > 0 && it.spd != 1 ? $"  ·  {Math.Round(it.spd * 100)}%" : ""), rx, y + 14); g.direction = "rtl";
			}
			else txt(rpResText(it.res), rx, y + 1, 21, rpResCol(it.res), 800);
			var dt = DateTimeOffset.FromUnixTimeMilliseconds(it.at).ToLocalTime();
			g.font = $"18px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = "rgba(255,255,255,0.75)";
			g.fillText($"{dt.Day}/{dt.Month}  {dt.Hour}:{dt.Minute:00}", dx, y + 2); g.fillText(rpClock(it.len), tx, y + 2); g.direction = "rtl";
		}
		g.restore();
		if (myRps.Count > MYRP_VIS)
		{
			double sx = W / 2 + 416, th = MYRP_VIS * MYRP_RH - 6, hh = Math.Max(36, th * MYRP_VIS / myRps.Count), ty = y0 - MYRP_RH / 2 + 3 + (th - hh) * myRpScr / (myRps.Count - MYRP_VIS);
			rr(sx - 2, y0 - MYRP_RH / 2 + 3, 4, th, 2); g.fillStyle = "rgba(255,255,255,0.08)"; g.fill(); rr(sx - 2, ty, 4, hh, 2); g.fillStyle = $"rgba({RGB},0.55)"; g.fill();
		}
		double by = y0 - MYRP_RH / 2 + MYRP_VIS * MYRP_RH + 26;
		if (myRpMsgT > 0) { g.globalAlpha = Math.Min(1, myRpMsgT); txt(myRpMsg, W / 2, by, 22, "#ffb35c", 500); g.globalAlpha = 1; }
		else if (myRpArm == myRpSel && myRpArmK == "del") txt("اضغط مرة ثانية عشان تحذفه", W / 2, by, 22, PINK, 800);
		else if (myRpArm == myRpSel && myRpArmK == "play") txt("اضغط مرة ثانية عشان تشوفه وتطلع من المحاولة اللي كنت فيها", W / 2, by, 22, "#ffb35c", 800);
		row(new[] { (K.confirm, "شاهد"), (K.restart, "احذف"), (K.back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void stepMyRp(Inp inp, double dt)
	{
		myRpMsgT -= dt; int n = myRps.Count;
		if (inp.back || inp.pz) { state = myRpBack; return; }
		if (n == 0) return;
		if (inp.down && myRpSel < n - 1) { myRpSel++; myRpArm = -1; } else if (inp.up && myRpSel > 0) { myRpSel--; myRpArm = -1; }
		var it = myRps[myRpSel];
		if (inp.restart) { if (myRpArm == myRpSel && myRpArmK == "del") { myRpArm = -1; myRpDelete(it); } else { myRpArm = myRpSel; myRpArmK = "del"; myRpMsgT = 0; } }
		else if (inp.confirm)
		{
			if (myRpInRun() && !(myRpArm == myRpSel && myRpArmK == "play")) { myRpArm = myRpSel; myRpArmK = "play"; myRpMsgT = 0; }
			else { myRpArm = -1; myRpPlay(it); }
		}
	}

	// ---------- the save buttons on the win and lose screens ----------
	static bool myRpCan() => !classicUi;
	static bool myRpDone(double at) => myRpSavedFor == at;
	// the win screen: rpWin already let go of the recording, lastRec has it
	static void myRpWinKey(Inp inp) { if (myRpCan() && inp.sq && !myRpDone(winAt) && now0() - winAt > 800 && myRpSave(lastRec, "win", lastRank)) myRpSavedFor = winAt; }
	static void myRpDeadKey(Inp inp) { if (myRpCan() && inp.sq && !myRpDone(deadAt) && now0() - deadAt > 700 && myRpSave(rec, "dead", null)) myRpSavedFor = deadAt; }
	static void myRpPrEndKey(Inp inp) { if (myRpCan() && inp.sq && !myRpDone(winAt) && now0() - winAt > 600 && myRpSave(rec, "pdone", null)) myRpSavedFor = winAt; }
	static double now0() => performance.now();
	// the platformer's win / lose / against-someone end screens
	static void plRpKey(Inp inp, double at, string res, string letter) { if (inp.sq && !myRpDone(at) && now0() - at > 700 && plRpSave(res, letter)) myRpSavedFor = at; }
	static (string[] ic, string l) myRpHint(double at) => (BTN[kind].sq, myRpDone(at) ? "انحفظ الريبلاي ✓" : "احفظ الريبلاي");
}
