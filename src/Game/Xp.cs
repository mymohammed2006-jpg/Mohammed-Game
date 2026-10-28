using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// New in the Godot version (not in the original game): XP and the player's level (مستوى), the 4 challenges, and the square's looks.
// XP is separate from the points: each best letter gives XP times how hard the level is, and each challenge opens a look for the
// square and gives XP. The chosen look is saved as mg_skin.
public static partial class G
{
	// how hard each level is, from the percentages Mohammed asked for when each was built (the first level = 1)
	public static readonly Dictionary<string, double> XP_MULT = new()
	{
		["L1"] = 1.0, ["F6"] = 1.1, ["F1"] = 1.1, ["F2"] = 1.1, ["L2"] = 1.3, ["L11"] = 1.4, ["L3"] = 1.5, ["L9"] = 1.5, ["L4"] = 1.7, ["L10"] = 1.7,
		["L8"] = 1.8, ["L5"] = 1.8, ["F5"] = 2.4, ["F3"] = 2.5, ["F4"] = 2.5, ["L6"] = 2.7, ["L7"] = 3.1, ["F7"] = 3.2, ["F8"] = 3.3, ["F9"] = 1.8, ["F10"] = 1.7, ["F11"] = 1.5,
		["L12"] = 1.8,   // Hard Spike: the same as L5 (Mohammed, 10-20)
		["L13"] = 1.8,   // A beat further: the same difficulty as L5
		["L14"] = 1.8,   // Jump again: the same difficulty as L5 (Mohammed, 10-20)
		["L15"] = 4.0,   // Abra Macabre: the hardest level, On The Floor x3.3 + 20% (Mohammed, 10-21)
		["L16"] = 1.8,   // عين بعين: the same difficulty as L5 (Mohammed, 10-24)
		["L17"] = 1.8,   // حالف لا ما انام: the same difficulty as L5 (Mohammed, 10-24)
		["L18"] = 1.8,   // level 18, Awof Al-Dunya: the same difficulty as L5 (Mohammed, 10-24)
		["L19"] = 1.8,   // عافني وراح: the same difficulty as L5 (Mohammed 20:30 10-24)
		["L20"] = 4.0,   // New Game: as hard as Abra Macabre
		["L21"] = 4.0,   // Barracuda: as hard as Abra Macabre
		["B1"] = 4.0,   // the boss mode's first level (proposed in the plan: like Abra Macabre)
		["B2"] = 4.6,   // the boss mode's second, 15% harder than B1 (his يالله 05:45 10-28 on the plan's x4.6)
		["P1"] = 1.5,   // the platformer's first level
		["P2"] = 2.0,   // the co-op level for three (harder than P1, easier than Link)
		["P3"] = 3.0,   // the radio, co-op for two (harder than P2): Mohammed 00:29 10-24
		// Hardcore (3 hearts, Normal only), by how hard each one is: L15 x4, then +50%, +30%, +30% (Mohammed 19:35 10-21)
		["H1"] = 6.0, ["H2"] = 8.0, ["H3"] = 10.0,
	};
	static readonly Dictionary<string, int> XP_BASE = new() { ["S"] = 100, ["A"] = 60, ["B"] = 40, ["C"] = 20 };
	public static int xpFor(Level L, string r, bool ez) => r == null || !XP_BASE.ContainsKey(r) ? 0 : (int)round(XP_BASE[r] * (XP_MULT.TryGetValue(L.id, out var m) ? m : 1) * (ez ? 0.5 : 1));

	public static bool classicUi;   // the comparison tools run the game exactly as v39 (no XP, no challenges, original square)
	// from level n to n + 1 takes 200 + 100 n XP
	public static int xpNeed(int n) => 200 + 100 * n;
	public static (int lv, int into, int need) xpProg(int x)
	{
		int n = 1; while (x >= xpNeed(n)) { x -= xpNeed(n); n++; }
		return (n, x, xpNeed(n));
	}

	// XP has an end: it is what the best letters are worth (each level, Normal and Easy on their own) plus the challenges done.
	// A better letter adds only the difference; playing again with the same letter adds nothing. New levels or challenges raise the end.
	// So anyone's XP comes from their letters: n / e = best letter by level id
	public class Chal { public string id, name, text; public int xp; public Func<Func<string, string>, (int have, int need)> prog; }
	static Level lvId(string id) => ALL_LV().FirstOrDefault(L => L.id == id);
	public static readonly Chal[] CHALS =
	{
		new Chal { id = "crystal", name = "الكريستال", text = "جيب S في ٣ مراحل\nنورمال", xp = 300, prog = n => (Math.Min(3, ALL_LV().Count(L => n(L.id) == "S")), 3) },
		new Chal { id = "circuit", name = "الدائرة", text = "خلّص أول ١١ مرحلة\nرئيسية نورمال", xp = 500, prog = n => (LEVELS.Count(L => circuitLv(L) && n(L.id) != null), LEVELS.Count(circuitLv)) },
		new Chal { id = "bolt", name = "البرق", text = "خلّص نورمال\nالمرحلة السابعة\nأو Let Me Love You", xp = 750, prog = n => (n("L7") != null || n("F7") != null ? 1 : 0, 1) },
		new Chal { id = "eq", name = "الإيقاع", text = "خلّص On The Floor\nنورمال", xp = 1000, prog = n => (n("F8") != null ? 1 : 0, 1) },
		// Hardcore (v45, Mohammed 19:35 10-21): finish each Hardcore level for its look and XP (1000, 2000, 3000: Mohammed 20:16 10-21)
		new Chal { id = "skull", name = "الجمجمة", text = "خلّص المرحلة الأولى\nفي الهاردكور", xp = 1000, prog = n => (n("H1") != null ? 1 : 0, 1) },
		new Chal { id = "coffin", name = "التابوت", text = "خلّص المرحلة الثانية\nفي الهاردكور", xp = 2000, prog = n => (n("H2") != null ? 1 : 0, 1) },
		new Chal { id = "teddy", name = "الدبدوب", text = "خلّص المرحلة الثالثة\nفي الهاردكور", xp = 3000, prog = n => (n("H3") != null ? 1 : 0, 1) },
	};
	// the Circuit challenge is the 11 main levels it was made with; the new data-file levels (L12 on) are not in it
	static bool circuitLv(Level L) => L.name != null && Array.IndexOf(DODGE_IDS, L.id) < 0;
	static string myN(string id) => lvId(id)?.best;
	public static (int have, int need) chalProg(Chal c) => c.prog(myN);
	public static bool chalDone(Chal c, Func<string, string> n = null) { var (h, nd) = c.prog(n ?? myN); return h >= nd; }
	public static bool skinOpen(string s) => s == "" || CHALS.Any(c => c.id == s && chalDone(c));
	public static int xpOf(Func<string, string> n, Func<string, string> e)
	{
		int x = 0; foreach (var L in ALL_LV()) x += xpFor(L, n(L.id), false) + xpFor(L, e(L.id), true);
		foreach (var c in CHALS) if (chalDone(c, n)) x += c.xp;
		return x;
	}
	public static int xp => xpOf(myN, id => lvId(id)?.bestE);
	public static int xpOfP(PlayerDoc p) => xpOf(id => gv(p.n, id), id => gv(p.e, id));

	public static void LoadXp() { skin = localStorage.getItem("mg_skin") ?? ""; if (!skinOpen(skin)) skin = ""; }

	// the win screen: what this finish added. xpMark() runs just before the letter is saved
	static int xpM; static HashSet<string> chalM = new();
	public static void xpMark() { xpM = xp; chalM = CHALS.Where(c => chalDone(c)).Select(c => c.id).ToHashSet(); }
	public static int lastXp, lastLvFrom, lastLvTo; public static List<Chal> lastChals = new();
	public static void awardXp()
	{
		lastXp = 0; lastChals = new(); if (classicUi) return;
		int now = xp; lastChals = CHALS.Where(c => chalDone(c) && !chalM.Contains(c.id)).ToList();
		lastXp = now - xpM - lastChals.Sum(c => c.xp);
		lastLvFrom = xpProg(xpM).lv; lastLvTo = xpProg(now).lv; hcUnlockCheck();
	}

	public static void setSkin(string s) { skin = s; localStorage.setItem("mg_skin", s); }
}
