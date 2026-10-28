using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// New in v45: the Hardcore mode (طور الهاردكور), Mohammed 10-21.
// - Its levels are dodge data files (data/dodge_H1.json ...) shown in their own list, with 3 hearts instead of 5 and no Easy. Checkpoints stay.
// - The main menu shows it only from level (مستوى) 5 on. With Easy on it is see-through and can't be pressed.
// - Reaching level 5 on a win shows a card sliding in on the right of the win screen.
public static partial class G
{
	const int HC = 11;
	public const int HC_LV = 5, HC_HP = 3;
	public static readonly string[] HC_IDS = { "H1", "H2", "H3" };
	public const int HC_SLOTS = 3;   // the list always shows 3 cards; a level not made yet is a locked card
	public static readonly List<Level> HCLV = new();
	public static int hcSel = 0;
	static double hcShake = 0, hcNoteAt = -1e9;
	const string HC_RED = "#ff1f3d", HC_RGB = "255,31,61";

	static void AddHcLevels()
	{
		foreach (var id in HC_IDS)
		{
			try { var L = LoadDodge(id); if (L != null) { L.hc = true; if (L.v51 != null) L.v51.hc = true; HCLV.Add(L); } }
			catch (Exception e) { Godot.GD.PushWarning("hardcore " + id + ": " + e.Message); }
		}
	}
	public static bool hcOpen() => !classicUi && xpProg(xp).lv >= HC_LV;
	public static bool noEz(Level L) => L != null && (L.plat || L.hc || L.boss);   // levels with no Easy (no Easy letter, column or board page)
	public static int maxHp(Level L) => L != null && L.hc ? HC_HP : L != null && L.boss ? (bnTestHp > 0 ? bnTestHp : BOSS_HP) : 5;
	public static double ptsMul(Level L) => L.hc ? 5 : 1;   // Hardcore letters are worth 5 times the points (Mohammed 19:19 10-21)

	// the main menu: after "مراحل الشباب"
	static (string s, int id)[] withHc((string s, int id)[] items) => hcOpen() ? items.Take(2).Append(("الهاردكور", HC)).Concat(items.Skip(2)).ToArray() : items;
	static void hcPress() { if (easy) { hcShake = 0.4; sfx(tt => sTone(tt, 140, 0.12, "square", 0.08)); } else { state = "hcList"; hcSel = Math.Min(hcSel, HC_SLOTS - 1); } }
	// how the item is drawn: see-through with Easy, shaking when pressed with Easy
	static double hcItemAlpha() => easy ? 0.3 : 1;
	static double hcItemDx() => hcShake > 0 ? JS.sin(hcShake * 60) * 8 * hcShake / 0.4 : 0;

	static Level hcAt(int i) => i < HCLV.Count ? HCLV[i] : null;
	// the levels open in order: the first one with the mode, each next one when the one before it is finished (any letter, his words 18:35 10-21)
	public static bool hcUnlocked(int i) => hcOpen() && i < HCLV.Count && (i == 0 || HCLV[i - 1].best != null);
	// a Hardcore level the player hasn't opened shows nothing of itself anywhere (list, board, profile)
	public static bool hcHidden(Level L) => L != null && L.hc && !hcUnlocked(HCLV.IndexOf(L));
	public static string lvName(Level L) => hcHidden(L) ? $"هاردكور {ar(HCLV.IndexOf(L) + 1)}: ؟؟؟" : L.name;
	static void drawHcList(double now)
	{
		bgShapes(now);
		// a slow red glow from the edges
		{ var gr = g.createRadialGradient(W / 2, H / 2, 260, W / 2, H / 2, 760); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, $"rgba({HC_RGB},{str(0.10 + 0.06 * pulse)})"); g.fillStyle = gr; g.fillRect(0, 0, W, H); }
		g.save(); g.shadowColor = HC_RED; g.shadowBlur = 18 + 10 * pulse; txt("الهاردكور", W / 2, 92, 52, "#fff"); g.restore(); g.shadowBlur = 0;
		double cw = 240, chh = 330, gp = 28; int n = HC_SLOTS; double total = n * cw + (n - 1) * gp, cy = H / 2 + 30;
		for (int i = 0; i < n; i++)
		{
			var L = hcAt(i); bool sel = i == hcSel, ok = L != null && L.ok && hcUnlocked(i); double x = W / 2 + total / 2 - cw / 2 - i * (cw + gp);
			int ii = i; zone(x - cw / 2, cy - chh / 2, cw, chh, () => hcSel = ii);
			if (sel && shakeCard > 0) x += JS.sin(shakeCard * 60) * 8 * shakeCard / 0.4;
			double lf = lift("H" + i, sel);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 18); g.fillStyle = sel ? "#220709" : "#0e0405"; g.fill();
			if (sel) { g.shadowColor = HC_RED; g.shadowBlur = 16 + 10 * pulse; }
			g.strokeStyle = sel ? HC_RED : $"rgba({HC_RGB},0.25)"; g.lineWidth = sel ? 3 : 1.5; g.stroke(); g.shadowBlur = 0;
			txt(ar(i + 1), x, cy - 90 + lf, 90, ok ? (sel ? HC_RED : "#fff") : "rgba(255,255,255,0.15)", 800);
			if (ok)
			{
				{ double fs = 28; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 16 && g.measureText(L.name).width > cw - 24) { fs--; g.font = $"800 {str(fs)}px {FONT}"; } txt(L.name, x, cy + 5 + lf, fs, "#fff", 800); }
				g.font = $"20px {TFONT}"; g.fillStyle = RED3; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(L.song ?? "", x, cy + 52 + lf);
				if (L.err) txt("الأغنية ما اشتغلت", x, cy + 125 + lf, 20, RED3, 500);
				else if (L.buf == null) txt("جاري التجهيز…", x, cy + 125 + lf, 20, "rgba(255,255,255,0.6)", 500);
				else if (L.best != null) drawBests(L, x, cy + lf);
			}
			else { lockIcon(x, cy + 20 + lf); txt("؟؟؟", x, cy + 95 + lf, 30, "rgba(255,255,255,0.4)", 800); }
		}
		row(new[] { (BTN[kind].confirm, "العب"), (BTN[kind].sq, "تدريب"), (BTN[kind].restart, "شوف البوت"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void stepHcList(Inp inp)
	{
		if (inp.left && hcSel < HC_SLOTS - 1) hcSel++;
		if (inp.right && hcSel > 0) hcSel--;
		var L = hcAt(hcSel); bool ok = L != null && L.ok && L.buf != null && !easy && hcUnlocked(hcSel);
		if ((inp.sq || inp.restart) && !inp.confirm) { if (ok) { if (inp.sq) openPractice(L, "hcList"); else openBot(L, "hcList"); } else shakeCard = 0.4; }
		else if (inp.confirm) { if (ok) startLevel(L, "hcList"); else shakeCard = 0.4; }
		else if (inp.back || inp.pz) state = "main";
	}

	// ---------- the unlock card on the win screen ----------
	// awardXp() calls this: the win that took the player from below level 5 to 5 or more
	static void hcUnlockCheck()
	{
		if (classicUi || !(lastLvFrom < HC_LV && lastLvTo >= HC_LV)) return;
		hcNoteAt = performance.now() + 1100;   // after the XP has counted up
	}
	const double HC_IN = 550, HC_HOLD = 5200, HC_OUT = 450;
	static bool hcNoteSfx;
	public static void drawHcNote(double now)
	{
		double el = now - hcNoteAt; if (el < 0 || el > HC_IN + HC_HOLD + HC_OUT) { if (el < 0) hcNoteSfx = false; return; }
		if (state != "win") { hcNoteAt = -1e9; return; }
		if (!hcNoteSfx) { hcNoteSfx = true; sfx(tt => { sTone(tt, 220, 0.16, "square", 0.10); sTone(tt + 0.10, 330, 0.16, "square", 0.10); sTone(tt + 0.20, 440, 0.32, "square", 0.12); sNoise(tt, 0.3, "lowpass", 900, 0.18); }); rumble(220, 0.8, 0.6); }
		double k = el < HC_IN ? el / HC_IN : el > HC_IN + HC_HOLD ? 1 - (el - HC_IN - HC_HOLD) / HC_OUT : 1;
		double e = el < HC_IN ? 1 - JS.pow(1 - k, 3) : k * k;
		double bw = 380, bh = 104, x = W - 24 - bw + (bw + 40) * (1 - e), y = lastChals.Count > 0 ? 120 : 24;   // under the challenge banner when there is one
		g.save();
		rr(x, y, bw, bh, 18); g.fillStyle = "rgba(16,3,5,0.94)"; g.fill();
		double gl = 0.6 + 0.4 * JS.sin(now / 180); g.shadowColor = HC_RED; g.shadowBlur = 22 * gl; g.strokeStyle = HC_RED; g.lineWidth = 2.5; g.stroke(); g.shadowBlur = 0;
		// the open lock, spinning square behind it
		double ix = x + bw - 58, iy = y + bh / 2;
		g.save(); g.translate(ix, iy); g.rotate(now / 600); g.fillStyle = $"rgba({HC_RGB},0.22)"; g.fillRect(-26, -26, 52, 52); g.restore();
		g.strokeStyle = "#fff"; g.lineWidth = 4; g.lineCap = "round"; g.beginPath(); g.arc(ix + 8, iy - 8, 10, Math.PI, 0); g.lineTo(ix + 18, iy - 4); g.stroke(); g.lineCap = "butt";
		rr(ix - 15, iy - 6, 30, 24, 5); g.fillStyle = "#fff"; g.fill(); g.fillStyle = HC_RED; g.fillRect(ix - 2, iy + 1, 4, 9);
		txt("فتحت طور الهاردكور!", x + bw - 100, y + 38, 27, "#fff", 800, "right");
		txt("تلقاه في القائمة الرئيسية", x + bw - 100, y + 74, 19, "rgba(255,255,255,0.65)", 500, "right");
		g.restore(); g.globalAlpha = 1;
	}
	public static void hcTick(double dt) { hcShake = Math.Max(0, hcShake - dt); }
}
