using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// The co-op boss mode (طور الزعيم), Mohammed 10-25: one boss level for 1-4 players, in its own list.
// - A boss level is a dodge data file (data/dodge_B1.json) with 3 hearts, no Easy and no checkpoint (a down = from the start).
public static partial class G
{
	public static readonly string[] BOSS_IDS = { "B1", "B2" };
	// who made each boss's song (the card's line under the name) and its picture
	static string bossArtist(Level L) => L?.id == "B2" ? "Shirobon" : "Nitro Fun";
	static void bossIcon(Level L, double x, double y, double r, double now) { if (L?.id == "B2") B2Icon(x, y, r, now / 1000 * 128 / 60); else B1CoreDraw(x, y, r, now / 1000 * 130 / 60, 1); }
	public static readonly List<Level> BOSSLV = new();
	public const int BOSS_HP = 3;
	public static int bnTestHp;   // tests only (test/BossNetTest.cs): fewer hearts
	public static double bnTestFrom;   // tests only: online starts at this beat, no scene
	static void AddBossLevels()
	{
		foreach (var id in BOSS_IDS)
		{
			try { var L = LoadDodge(id); if (L != null) { L.boss = true; BOSSLV.Add(L); } }
			catch (Exception e) { Godot.GD.PushWarning("boss " + id + ": " + e.Message); }
		}
	}

	// ---------- the screen: the third card of "المراحل الرئيسية" opens it ----------
	static readonly string[] BOSS_OPTS = { "العب لحالك", "سوّ غرفة", "ادخل بكود" };
	public static int bossSel = 0, bossLv = 0; static bool joinBoss;
	static double bossShake;
	// the bosses' list (his ask 13:21 10-25, "اعتمد" + now 13:53): a card per boss, then the boss's own screen; BOSS_LOCKED cards after them
	// show a lock and ؟؟؟ (the bosses still to come); none since v51 (his ask 21:59 10-26: only the boss that is there, no "قريب" cards)
	const int BOSS_LOCKED = 0;
	static int bossSlots => BOSSLV.Count + BOSS_LOCKED;
	static Level bossL => bossLv < BOSSLV.Count ? BOSSLV[bossLv] : BOSSLV.FirstOrDefault();
	public static bool stepBoss(Inp inp, double now, double dt)
	{
		if (state == "bossList")
		{
			if (inp.left && bossLv < bossSlots - 1) bossLv++;
			if (inp.right && bossLv > 0) bossLv--;
			if (inp.confirm) { if (bossLv < BOSSLV.Count) { state = "bossMenu"; bossSel = 0; } else { shakeCard = 0.4; sfx(tt => sTone(tt, 140, 0.12, "square", 0.08)); } }
			else if (inp.back || inp.pz) state = "modes";
			return true;
		}
		if (state != "bossMenu") return false;
		bossShake = Math.Max(0, bossShake - dt);
		int n = BOSS_OPTS.Length; if (inp.up) bossSel = (bossSel + n - 1) % n; if (inp.down) bossSel = (bossSel + 1) % n;
		var L = bossL;
		if (inp.confirm && L != null)
		{
			if (bossSel == 0) { if (L.ok && L.buf != null) { joinBoss = false; startLevel(L, "bossMenu"); } else bossShake = 0.4; }
			else if (acc == null) netNote("سجّل دخول من الحساب بالقائمة عشان تلعب أونلاين");
			else if (bossSel == 1) { joinBoss = true; bnHost(); }
			else { joinBoss = true; joinCode = ""; joinDig = 0; state = "pltJoin"; }
		}
		else if (inp.back || inp.pz) state = "bossList";
		return true;
	}
	public static void drawBossList(double now)
	{
		bgShapes(now); txt("طور الزعيم", W / 2, 95, 52, "#fff");
		const int VIS = 4; double cw = 270, chh = 380, gp = 30, cy = H / 2 + 20; int n = bossSlots, first = Math.Max(0, Math.Min(bossLv - VIS + 1, n - VIS)), vis = Math.Min(VIS, n);
		if (bossLv < first) first = bossLv;
		double total = vis * cw + (vis - 1) * gp;
		for (int k = 0; k < vis; k++)
		{
			int i = first + k; bool sel = i == bossLv, ok = i < BOSSLV.Count; double x = W / 2 + total / 2 - cw / 2 - k * (cw + gp);
			int ii = i; zone(x - cw / 2, cy - chh / 2, cw, chh, () => bossLv = ii);
			if (sel && shakeCard > 0) x += JS.sin(shakeCard * 60) * 8 * shakeCard / 0.4;
			double lf = lift("B" + i, sel);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 20); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,42,109,0.3)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			txt("الزعيم " + ar(i + 1), x, cy - 140 + lf, 24, sel ? PINK : "rgba(255,255,255,0.5)", 700);
			if (ok)
			{
				var L = BOSSLV[i];
				bossIcon(L, x, cy - 50 + lf, 46, now);
				{ double fs = 32; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 18 && g.measureText(L.name).width > cw - 24) { fs--; g.font = $"800 {str(fs)}px {FONT}"; } txt(L.name, x, cy + 45 + lf, fs, "#fff", 800); }
				{ string sl = bossArtist(L) + " - " + L.song; double fs = 20; g.font = $"{str(fs)}px {TFONT}"; while (fs > 12 && g.measureText(sl).width > cw - 24) { fs--; g.font = $"{str(fs)}px {TFONT}"; } g.fillStyle = PINK; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(sl, x, cy + 88 + lf); }
				txt(L.best != null ? "أفضل حرف: " + L.best : "ما خلصتها للحين", x, cy + 135 + lf, 22, L.best != null ? NGOLD : "rgba(255,255,255,0.5)", 700);
			}
			else { lockIcon(x, cy - 30 + lf); txt("؟؟؟", x, cy + 55 + lf, 32, "rgba(255,255,255,0.4)", 800); txt("قريب", x, cy + 105 + lf, 22, "rgba(255,255,255,0.35)", 600); }
		}
		if (first > 0) txt("◀", W / 2 + total / 2 + 40, cy, 28, "rgba(255,255,255,0.5)", 800);
		if (first + vis < n) txt("▶", W / 2 - total / 2 - 40, cy, 28, "rgba(255,255,255,0.5)", 800);
		txt("من ١ إلى ٤ لاعبين", W / 2, H - 80, 22, "rgba(255,255,255,0.6)", 500);
		row(new[] { (BTN[kind].confirm, "اختيار"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	public static void drawBossMenu(double now)
	{
		bgShapes(now); txt("طور الزعيم", W / 2, 95, 52, "#fff");
		var L = bossL; if (L == null) return;
		// the boss's card on the left: its core, name, song, best letter
		double cx = 330, cy = H / 2 + 30, cw = 380, chh = 400, sx = bossShake > 0 ? JS.sin(bossShake * 60) * 8 * bossShake / 0.4 : 0;
		rr(cx - cw / 2 + sx, cy - chh / 2, cw, chh, 22); g.fillStyle = CARD; g.fill(); g.strokeStyle = "rgba(255,42,109,0.5)"; g.lineWidth = 2; g.stroke();
		bossIcon(L, cx + sx, cy - 70, 54, now);
		txt(L.name, cx + sx, cy + 50, 38, "#fff", 800);
		g.font = $"22px {TFONT}"; g.fillStyle = PINK; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(bossArtist(L) + " - " + L.song, cx + sx, cy + 95);
		txt(L.best != null ? "أفضل حرف: " + L.best : "ما خلصتها للحين", cx + sx, cy + 145, 24, L.best != null ? NGOLD : "rgba(255,255,255,0.5)", 700);
		// the choices on the right
		for (int i = 0; i < BOSS_OPTS.Length; i++)
		{
			double x = 850, y = 250 + i * 100; bool sel = i == bossSel; int ii = i; zone(x - 260, y - 38, 520, 76, () => bossSel = ii);
			rr(x - 260, y - 38, 520, 76, 18); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			txt(BOSS_OPTS[i], x, y, sel ? 36 : 32, sel ? "#fff" : "rgba(255,255,255,0.6)", 800);
		}
		txt("من ١ إلى ٤ لاعبين، ٣ قلوب، بدون حفظ", 850, 545, 22, "rgba(255,255,255,0.6)", 500);
		txt("اللي يطيح يرجع إذا لمسه واحد من الفريق", 850, 578, 22, "rgba(255,255,255,0.6)", 500);
		drawNetNote(now);
		row(new[] { (BTN[kind].confirm, "اختيار"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// the mode card's picture: the core with four squares around it
	static void modeIconBoss(double now, bool sel)
	{
		double t = now / 1000; B1CoreDraw(0, 0, 30, t * 130 / 60, 1);
		for (int i = 0; i < 4; i++) { double a = t * 0.8 + i * TAU / 4, x = JS.cos(a) * 92, y = JS.sin(a) * 52; string c = BN_COL[i]; g.shadowColor = c; g.shadowBlur = 12; g.fillStyle = c; g.fillRect(x - 9, y - 9, 18, 18); g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(x - 3, y - 3, 6, 6); }
	}
}
