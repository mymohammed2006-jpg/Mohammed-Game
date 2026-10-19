using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// New in the Godot version: the looks screen, the challenges screen, the level badge on the main menu and the XP on the win screen.
// With classicUi (the comparison tools) none of this shows and the menu is exactly v39's.
public static partial class G
{
	const int CHAL = 9, SKINSM = 10;
	static (string s, int id)[] mainItems() => classicUi ? MAIN : MAIN.Take(2).Append(("الأشكال", SKINSM)).Append(("التحديات", CHAL)).Concat(MAIN.Skip(2)).ToArray();
	public static int chalSel = 0;
	public static double chalShake = 0;
	const string GOLD = "#ffd25c";

	static void xpBar(double x, double y, double w, double h, double k, string col)
	{
		rr(x, y - h / 2, w, h, h / 2); g.fillStyle = "rgba(255,255,255,0.12)"; g.fill();
		if (k > 0) { rr(x + w - Math.Max(h, w * k), y - h / 2, Math.Max(h, w * k), h, h / 2); g.fillStyle = col; g.shadowColor = col; g.shadowBlur = 8; g.fill(); g.shadowBlur = 0; }
	}
	// the small level badge under the account box
	static void drawLvBadge()
	{
		if (classicUi) return;
		var (lv, into, need) = xpProg(xp); double x = 40, y = 112;
		txt($"مستوى {ar(lv)}", x + 200, y, 22, "#fff", 800, "right");
		xpBar(x, y + 26, 200, 8, (double)into / need, GOLD);
	}

	// the looks screen: pick the square's look; the ones not opened yet show locked
	static void drawSkins(double now)
	{
		bgShapes(now); txt("الأشكال", W / 2, 70, 48, "#fff");
		int n = SKINS.Length; double cw = 214, chh = 360, gp = 22, total = n * cw + (n - 1) * gp, cy = 370;
		for (int i = 0; i < n; i++)
		{
			string s = SKINS[i]; bool open = skinOpen(s), sel = i == chalSel, on = s == skin;
			double x = W / 2 + total / 2 - cw / 2 - i * (cw + gp);
			if (sel && chalShake > 0) x += JS.sin(chalShake * 60) * 8 * chalShake / 0.4;
			double lf = lift("K" + i, sel), top = cy - chh / 2 + lf;
			int ii = i; zone(x - cw / 2, cy - chh / 2, cw, chh, () => chalSel = ii);
			rr(x - cw / 2, top, cw, chh, 18); g.fillStyle = sel ? CARDS : CARD; g.fill();
			g.strokeStyle = on ? CYAN : sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel || on ? 3 : 1.5; g.stroke();
			// the square, big
			g.save(); g.translate(x, top + 100); g.scale(4.4, 4.4); g.globalAlpha = open ? 1 : 0.28; skinBody(s, 0.5, now); g.restore(); g.globalAlpha = 1;
			if (!open) { g.save(); g.translate(x, top + 106); g.scale(0.8, 0.8); lockIcon(0, 0); g.restore(); }
			txt(skinName(s), x, top + 196, 30, "#fff", 800);
			if (s == "") txt("الشكل الأصلي", x, top + 240, 19, "rgba(255,255,255,0.55)", 500);
			string st = on ? "مختار" : open ? "مفتوح" : "مقفل";
			txt(st, x, top + chh - 34, 24, on ? CYAN : open ? "#fff" : "rgba(255,255,255,0.35)", 800);
		}
		if (skinInfo) { drawSkinInfo(now); return; }
		row(new[] { (BTN[kind].confirm, "اختار الشكل"), (BTN[kind].restart, "طريقة الفتح"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// how a look opens: shown when you press a locked one, or the info button on any look
	public static bool skinInfo;
	static void drawSkinInfo(double now)
	{
		var c = Array.Find(CHALS, q => q.id == SKINS[chalSel]); if (c == null) return; bool open = chalDone(c);
		overlay(0.7); zone(0, 0, W, H, null, "back"); double pw = 560, ph = 360, px = W / 2 - pw / 2, py = H / 2 - ph / 2 - 10;
		rr(px, py, pw, ph, 20); g.fillStyle = CARDS; g.fill(); g.strokeStyle = open ? CYAN : RED; g.lineWidth = 2.5; g.stroke();
		g.save(); g.translate(W / 2, py + 62); g.scale(2.6, 2.6); g.globalAlpha = open ? 1 : 0.5; skinBody(c.id, 0.5, now); g.restore(); g.globalAlpha = 1;
		txt(skinName(c.id), W / 2, py + 122, 30, "#fff", 800);
		txt("طريقة الفتح", W / 2, py + 160, 18, "rgba(255,255,255,0.45)", 500);
		var ls = c.text.Replace("\n", " ").Replace(" أو ", "\nأو ").Split('\n');
		for (int j = 0; j < ls.Length; j++) txt(ls[j], W / 2, py + 196 + j * 32, 25, "#fff", 800);
		var (h, nd) = chalProg(c); double by = py + 210 + ls.Length * 32;
		xpBar(W / 2 - 160, by, 320, 9, Math.Min(1, (double)h / nd), open ? CYAN : RED);
		txt($"{ar(Math.Min(h, nd))} / {ar(nd)}", W / 2, by + 24, 18, "rgba(255,255,255,0.6)", 500);
		txt(open ? "فتحته ✓" : "مقفل", W / 2, py + ph - 26, 22, open ? CYAN : "rgba(255,255,255,0.45)", 800);
		row(new[] { (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void stepSkins(Inp inp, double dt)
	{
		chalShake = Math.Max(0, chalShake - dt);
		if (skinInfo) { if (inp.back || inp.pz || inp.confirm || inp.restart) skinInfo = false; return; }
		if (inp.left && chalSel < SKINS.Length - 1) chalSel++;
		if (inp.right && chalSel > 0) chalSel--;
		string s = SKINS[chalSel];
		if (inp.confirm) { if (skinOpen(s)) { setSkin(s); sfx(tt => sTone(tt, 880, 0.08, "sine", 0.2)); } else skinInfo = true; }
		else if (inp.restart) { if (s != "") skinInfo = true; else chalShake = 0.4; }
		else if (inp.back || inp.pz) state = "main";
	}

	// the challenges screen: what each challenge asks, how far you are, and what it gives (a look + XP). Only to look at
	static void drawChal(double now)
	{
		bgShapes(now); txt("التحديات", W / 2, 70, 48, "#fff");
		int n = CHALS.Length; double cw = 250, chh = 420, gp = 26, total = n * cw + (n - 1) * gp, cy = 380;
		for (int i = 0; i < n; i++)
		{
			var c = CHALS[i]; bool done = chalDone(c), sel = i == chalView;
			double x = W / 2 + total / 2 - cw / 2 - i * (cw + gp), top = cy - chh / 2 + lift("C" + i, sel);
			int ii = i; zone(x - cw / 2, cy - chh / 2, cw, chh, () => chalView = ii, null);
			rr(x - cw / 2, top, cw, chh, 18); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : done ? CYAN : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : done ? 2.5 : 1.5; g.stroke();
			var ls = c.text.Split('\n');
			for (int j = 0; j < ls.Length; j++) txt(ls[j], x, top + 48 + j * 32 + (3 - ls.Length) * 16, 24, "#fff", 800);
			var (h, nd) = chalProg(c); double py = top + 172;
			xpBar(x - cw / 2 + 28, py, cw - 56, 9, Math.Min(1, (double)h / nd), done ? CYAN : RED);
			txt($"{ar(Math.Min(h, nd))} / {ar(nd)}", x, py + 24, 18, "rgba(255,255,255,0.6)", 500);
			// the reward
			g.strokeStyle = "rgba(255,255,255,0.1)"; g.lineWidth = 1; g.beginPath(); g.moveTo(x - cw / 2 + 24, top + 228); g.lineTo(x + cw / 2 - 24, top + 228); g.stroke();
			txt("الجائزة", x, top + 252, 17, "rgba(255,255,255,0.45)", 500);
			g.save(); g.translate(x, top + 300); g.scale(2.4, 2.4); skinBody(c.id, 0.5, now); g.restore();
			txt($"شكل {skinName(c.id)}", x, top + 346, 20, "#fff", 800);
			g.font = $"800 19px {FONT}"; g.fillStyle = GOLD; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText($"+{ar(c.xp)} XP", x, top + 374);
			txt(done ? "خلّصته ✓" : "", x, cy + chh / 2 + 26, 22, CYAN, 800);
		}
		row(new[] { (BTN[kind].move, "تنقّل"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// moving between the challenges only highlights them (nothing to pick here)
	public static int chalView = 0;
	static void stepChal(Inp inp, double dt)
	{
		if (inp.left && chalView < CHALS.Length - 1) chalView++;
		if (inp.right && chalView > 0) chalView--;
		if (inp.back || inp.pz) state = "main";
	}

	// the win screen: +XP on the right of the letter, the level bar, a new level, and challenges this finish completed
	static void drawWinXp(double now)
	{
		if (classicUi || (lastXp <= 0 && lastChals.Count == 0) || now <= winSlam) return;
		double a = clamp((now - winSlam - 200) / 200, 0, 1), ce = clamp((now - winSlam - 300) / 700, 0, 1), cv = round(lastXp * (1 - JS.pow(1 - ce, 3)));
		g.globalAlpha = a; double x = W / 2 + 175, y = H / 2 - 40;
		g.font = $"800 34px {FONT}"; g.fillStyle = GOLD; g.shadowColor = GOLD; g.shadowBlur = 10; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText($"+{ar(cv)} XP", x, y); g.shadowBlur = 0;
		var (lv, into, need) = xpProg(xp - lastXp + (int)cv);
		txt($"مستوى {ar(lv)}", x + 200, y + 36, 18, "rgba(255,255,255,0.75)", 500, "right");
		xpBar(x, y + 58, 200, 7, (double)into / need, GOLD);
		if (lastLvTo > lastLvFrom && ce >= 1)
		{
			double p = 1 + 0.06 * JS.sin(now / 160);
			g.save(); g.translate(x + 100, y - 46); g.scale(p, p); txt("مستوى جديد!", 0, 0, 24, GOLD, 800); g.restore();
		}
		g.globalAlpha = 1;
		if (lastChals.Count > 0)
		{
			double b = clamp((now - winSlam - 900) / 300, 0, 1); g.globalAlpha = b; double by = 62 - 20 * (1 - b);
			string names = string.Join(" و ", lastChals.Select(c => c.name)); int gx = lastChals.Sum(c => c.xp);
			string msg = $"خلّصت تحدي {names} وفتحت شكل جديد";
			g.font = $"800 24px {FONT}"; double tw = g.measureText(msg).width, bw = tw + 110 + 90 * lastChals.Count;
			rr(W / 2 - bw / 2, by - 30, bw, 60, 16); g.fillStyle = "rgba(10,30,36,0.92)"; g.fill(); g.strokeStyle = CYAN; g.lineWidth = 2; g.stroke();
			double ix = W / 2 + bw / 2 - 40;
			foreach (var c in lastChals) { g.save(); g.translate(ix, by); g.scale(1.6, 1.6); skinBody(c.id, 0.6, now); g.restore(); ix -= 46; }
			txt(msg, ix + 10, by + 2, 24, "#fff", 800, "right");
			g.font = $"800 22px {FONT}"; g.fillStyle = GOLD; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText($"+{ar(gx)} XP", W / 2 - bw / 2 + 20, by + 1);
			g.globalAlpha = 1;
		}
	}
}
