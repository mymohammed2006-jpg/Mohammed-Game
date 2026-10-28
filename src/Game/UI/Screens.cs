using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static MG.JS;

namespace MG;

// original index.html lines 7116-7117 (main menu items), 7251-7404 (menu drawing: shapes, title, gate, splash, letters, points, Easy box,
// account, board), 7448-7479 (profile), 7540-7580 (replay loading and the replay bar), 7581-7717 (main, levels, how to play, settings, friends, intro)
// The game is the PC program, so `APP` (window.mgApp) is on: the main menu has "خروج" and the gate shows no F11 text.
public static partial class G
{
	const int EXIT = 6, BOARD = 8, ACC = 7;
	static readonly (string s, int id)[] MAIN = { ("المراحل الرئيسية", 0), ("مراحل الشباب", 1), ("الترتيب", BOARD), ("طريقة اللعب", 2), ("الإعدادات", 3), ("خروج", EXIT) };
	const bool APP = true;
	static readonly Dictionary<string, double> LIFT = new();
	static readonly Regex ASCII = new("^[\\x00-\\x7F]+\\z");
	static bool isAscii(string s) => ASCII.IsMatch(s);

	public static void bgShapes(double now)
	{
		double tt = now / 1000; g.save(); g.globalAlpha = 0.4; g.strokeStyle = RED; g.lineWidth = 3;
		for (int i = 0; i < 6; i++) { double a = tt * 0.25 + i * TAU / 6, r = 300 + 24 * pulse; g.save(); g.translate(W / 2 + JS.cos(a) * r * 1.5, H / 2 + JS.sin(a) * r * 0.85); g.rotate(tt * (i % 2 != 0 ? 1 : -1)); double s = 18 * (1 + 0.25 * pulse); g.strokeRect(-s, -s, 2 * s, 2 * s); g.restore(); }
		g.restore();
	}
	static void drawTitle(double cx, double y, double size, double aM = 1, double aG = 1, double shift = 1)
	{
		double sz = size * (1 + 0.03 * pulse); g.font = $"{str(sz)}px {TFONT}"; g.textBaseline = "middle"; g.textAlign = "left"; g.direction = "ltr";
		double wM = g.measureText("Mohammed").width, wS = g.measureText(" ").width, wG = g.measureText("Game").width;
		double fx = cx - (wM + wS + wG) / 2, startX = cx - wM / 2, mx = startX + (fx - startX) * shift;
		g.globalAlpha = aM; g.fillStyle = RED; g.shadowColor = RED; g.shadowBlur = size * 0.3 * aM * (0.7 + 0.6 * pulse); g.fillText("Mohammed", mx, y);
		g.globalAlpha = aG; g.fillStyle = RED2; g.shadowColor = RED; g.shadowBlur = size * 0.3 * aG * (0.7 + 0.6 * pulse); g.fillText("Game", fx + wM + wS + (1 - shift) * 40, y);
		g.shadowBlur = 0; g.globalAlpha = 1;
	}
	static void drawGate(double now)
	{
		double a = state == "gate" ? 0.55 + 0.25 * JS.sin(now / 400) : 0.4;
		if (!APP) txt("اضغط F11", W / 2, H / 2, 30, $"rgba(255,255,255,{str(a)})", 500);
		if (gateHint) txt("اضغط على الشاشة مرة وحدة عشان يشتغل الصوت", W / 2, H / 2 + 50, 19, "rgba(255,255,255,0.5)", 500);
	}
	static void drawSplash()
	{
		double s = menuPos();
		double k1 = clamp((s - 1.0) / 2.2, 0, 1), k2 = clamp((s - 3.9) / (MENU_DROP - 3.9), 0, 1), e2 = 1 - JS.pow(1 - k2, 3);
		drawTitle(W / 2, H / 2 - 20, 110, k1 * k1, e2, e2);
	}
	// the best letter on a card: normal on the right and Easy on the left. A normal S is red and shines (a flash passes over it + a pulsing glow)
	public static void sShine(string ch, double x, double y, double w, double now, double glow)
	{
		double per = 1700, p = -0.3 + ((now % per) / per) * 1.6, a = clamp(p - 0.16, 0, 1), b = clamp(p, 0, 1), c = clamp(p + 0.16, 0, 1);
		var gr = g.createLinearGradient(x - w / 2, y - w / 2, x + w / 2, y + w / 2); gr.addColorStop(0, "#ff2436"); gr.addColorStop(a, "#ff2436"); gr.addColorStop(b, "#ffe0e4"); gr.addColorStop(c, "#ff2436"); gr.addColorStop(1, "#ff2436");
		g.shadowColor = "#ff2436"; g.shadowBlur = glow * (1 + 0.45 * JS.sin(now / 260)); g.fillStyle = gr; g.fillText(ch, x, y); g.shadowBlur = 0;
	}
	static void drawBests(Level L, double x, double y)
	{
		txt("أفضل تقييم", x, y + 88, 17, "rgba(255,255,255,0.55)", 500);
		if (L.hc)   // Hardcore: Normal only
		{
			g.font = $"32px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
			if (L.best == "S") sShine("S", x, y + 122, 26, performance.now(), 12); else { g.fillStyle = L.best != null ? "#ff2436" : "rgba(255,255,255,0.25)"; g.fillText(L.best ?? "-", x, y + 122); }
			ezBadge(x, y + 150, 0.75, "NORMAL", "#ff2436", "#fff"); return;
		}
		g.strokeStyle = "rgba(255,255,255,0.15)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(x, y + 110); g.lineTo(x, y + 152); g.stroke();
		foreach (var (e, dx) in new[] { (false, 48.0), (true, -48.0) })
		{
			string r = e ? L.bestE : L.best; double cx = x + dx;
			g.font = $"32px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
			if (r == "S" && !e) sShine("S", cx, y + 122, 26, performance.now(), 12); else { g.fillStyle = r != null ? (e ? "#7cc8ff" : "#ff2436") : "rgba(255,255,255,0.25)"; g.fillText(r ?? "-", cx, y + 122); }
			ezBadge(cx, y + 150, 0.75, e ? "EASY" : "NORMAL", e ? "#7cc8ff" : "#ff2436", e ? "#0b1426" : "#fff");
		}
	}
	public static void ezBadge(double x, double y, double k = 1, string lab = "EASY", string bg = "#7cc8ff", string fg = "#0b1426")
	{
		g.save(); g.shadowBlur = 0; g.font = $"{str(13 * k)}px {TFONT}"; double w = g.measureText(lab).width + 20 * k, h = 22 * k; rr(x - w / 2, y - h / 2, w, h, h / 2); g.fillStyle = bg; g.fill();
		g.fillStyle = fg; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(lab, x, y + 1); g.restore();
	}
	// the points button top right of the menu: Easy points when Easy Mode is on, normal points when it is off
	static void drawPtsBox(double now)
	{
		bool sel = menuSel == 5; double n = ptsOf(easy); string nm = ar(n); double rx = W - 40, y = 50;
		g.font = $"800 {(sel ? 34 : 30)}px {FONT}"; double nw = g.measureText(nm).width; g.font = $"500 20px {FONT}"; double lw = g.measureText("نقطة").width;
		double w = nw + lw + 74, x = rx - w;
		zone(x, y - 30, w, 60, () => menuSel = 5);
		rr(x, y - 30, w, 60, 16); g.fillStyle = sel ? $"rgba({RGB},{str(0.1 + 0.08 * pulse)})" : "rgba(255,255,255,0.04)"; g.fill(); g.strokeStyle = sel ? RED : $"rgba({RGB},0.45)"; g.lineWidth = 2; g.stroke();
		g.save(); g.translate(x + 26, y); g.rotate(now / 420); g.fillStyle = RED; g.shadowColor = RED; g.shadowBlur = 10 * (0.6 + 0.6 * pulse); double q = 8 * (1 + 0.25 * pulse); g.fillRect(-q, -q, 2 * q, 2 * q); g.restore();
		txt(nm, rx - 18, y + 1, sel ? 34 : 30, "#fff", 800, "right"); txt("نقطة", rx - 24 - nw, y + 3, 20, sel ? "#fff" : "rgba(255,255,255,0.6)", 500, "right");
	}
	// the points screen: normal, Easy and the total
	static void drawPoints(double now)
	{
		bgShapes(now); txt("نقاطك", W / 2, 110, 48, "#fff");
		double pn = ptsOf(false), pe = ptsOf(true), cw = 300, ch = 250, gp = 40, y = 345;
		var boxes = new (string lab, double v, double mx, string c, string fg)[] { ("NORMAL", pn, ptsMax(false), "#ff2436", "#fff"), ("EASY", pe, ptsMax(true), "#7cc8ff", "#0b1426"), ("المجموع", pn + pe, ptsMax(false) + ptsMax(true), "#fff", "#000") };
		for (int i = 0; i < boxes.Length; i++)
		{
			var (lab, v, mx, c, fg) = boxes[i]; double cx = W / 2 + (1 - i) * (cw + gp), x = cx - cw / 2; bool tot = i == 2;
			rr(x, y - ch / 2, cw, ch, 22); g.fillStyle = tot ? $"rgba({RGB},{str(0.1 + 0.06 * pulse)})" : CARD; g.fill(); g.strokeStyle = tot ? RED : c; g.lineWidth = tot ? 3 : 2; g.stroke();
			if (tot) txt(lab, cx, y - 80, 28, "#fff", 800); else ezBadge(cx, y - 80, 1.35, lab, c, fg);
			g.shadowColor = tot ? RED : c; g.shadowBlur = 18 * (0.6 + 0.6 * pulse); txt(ar(v), cx, y + 8, 76, tot ? "#fff" : c, 800); g.shadowBlur = 0;
			txt($"من {ar(mx)}", cx, y + 82, 22, "rgba(255,255,255,0.55)", 500);
		}
		txt("النقاط من أفضل تقييم لك في كل مرحلة", W / 2, y + 180, 22, "#e9e6f5", 500);
		txt($"العادي: S = {ar(100)}   A = {ar(70)}   B = {ar(40)}   C = {ar(20)}      والـ Easy نصها", W / 2, y + 218, 20, "rgba(255,255,255,0.55)", 500);
		row(new[] { (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void drawEasyBox(double now)
	{
		bool sel = menuSel == 4; double x = 44, y = H - 58, s = 30;
		zone(x - 14, y - s / 2 - 12, 250, s + 24, () => menuSel = 4);
		if (sel) { rr(x - 14, y - s / 2 - 12, 250, s + 24, 14); g.fillStyle = $"rgba({RGB},{str(0.08 + 0.08 * pulse)})"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
		rr(x, y - s / 2, s, s, 7); if (easy) { g.fillStyle = RED; g.shadowColor = RED; g.shadowBlur = 12 * (0.6 + 0.6 * pulse); g.fill(); g.shadowBlur = 0; }
		g.strokeStyle = easy || sel ? RED : "rgba(255,255,255,0.45)"; g.lineWidth = 2.5; g.stroke();
		if (easy) { g.strokeStyle = "#fff"; g.lineWidth = 4; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath(); g.moveTo(x + 7, y + 1); g.lineTo(x + 13, y + 8); g.lineTo(x + 24, y - 7); g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter"; }
		g.font = $"{(sel ? 26 : 24)}px {TFONT}"; g.fillStyle = sel || easy ? "#fff" : "rgba(255,255,255,0.5)"; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText("Easy Mode", x + s + 16, y + 1);
	}
	// the account button top left of the menu: your name when logged in, or "سجّل دخول"
	static void personIcon(double x, double y, string col, double k = 1) { g.fillStyle = col; g.beginPath(); g.arc(x, y - 6 * k, 6 * k, 0, TAU); g.fill(); g.beginPath(); g.ellipse(x, y + 9 * k, 11 * k, 7 * k, 0, Math.PI, TAU); g.fill(); }
	static void drawAccBox(double now)
	{
		bool sel = menuSel == ACC; string lab = acc != null ? acc.name : "سجّل دخول"; double x = 40, y = 50, fs = acc != null ? (sel ? 30 : 27) : (sel ? 26 : 23);
		g.font = $"800 {str(fs)}px {FONT}"; double w = g.measureText(lab).width + 74;
		zone(x, y - 30, Math.Max(w, 210), classicUi ? 60 : 110, () => menuSel = ACC);
		rr(x, y - 30, w, 60, 16); g.fillStyle = sel ? $"rgba({RGB},{str(0.1 + 0.08 * pulse)})" : "rgba(255,255,255,0.04)"; g.fill(); g.strokeStyle = sel ? RED : $"rgba({RGB},0.45)"; g.lineWidth = 2; g.stroke();
		personIcon(x + 28, y + 2, acc != null ? RED : "rgba(255,255,255,0.45)");
		txt(lab, x + w - 18, y + 2, fs, acc != null || sel ? "#fff" : "rgba(255,255,255,0.6)", acc != null ? 800 : 500, "right");
	}
	static void field(string label, string val, double y, bool sel, bool pass)
	{
		double x = W / 2 - 300, w = 600, h = 64;
		txt(label, W / 2 + 300, y - 50, 22, sel ? "#fff" : "rgba(255,255,255,0.55)", 500, "right");
		rr(x, y - h / 2, w, h, 14); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,255,255,0.18)"; g.lineWidth = sel ? 2.5 : 1.5; g.stroke();
		string shown = pass ? new string('•', val.Length) : val;
		g.font = $"800 30px {FONT}"; double tw = g.measureText(shown).width;
		txt(shown, x + w - 24, y + 2, 30, "#fff", 800, "right");
		if (sel && Math.Floor(performance.now() / 500) % 2 == 0) { g.fillStyle = RED; g.fillRect(x + w - 28 - tw, y - 17, 3, 34); }
	}
	static void btnRow(string label, double y, bool sel, double w = 360)
	{
		rr(W / 2 - w / 2, y - 30, w, 60, 16); g.fillStyle = sel ? $"rgba({RGB},{str(0.1 + 0.08 * pulse)})" : "rgba(255,255,255,0.04)"; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,255,255,0.18)"; g.lineWidth = 2; g.stroke();
		txt(label, W / 2, y + 2, sel ? 32 : 28, sel ? "#fff" : "rgba(255,255,255,0.6)", 800);
	}
	static void drawAccount(double now)
	{
		bgShapes(now);
		if (acc != null)
		{
			txt("حسابك", W / 2, 100, 48, "#fff");
			personIcon(W / 2, 205, RED, 2.2); txt(acc.name, W / 2, 280, 46, "#fff", 800);
			var st = syncing ? ("جاري المزامنة…", "rgba(255,255,255,0.55)") : netState == "net" ? ("ما فيه نت، نقاطك بتنحفظ أونلاين أول ما يرجع", "#ffb35c") : netState == "err" ? ("ما قدرت أحفظ أونلاين الحين، بجرب بعدين", "#ffb35c") : ("نقاطك محفوظة أونلاين ✓", "#5fe08f");
			txt(st.Item1, W / 2, 330, 22, st.Item2, 500);
			double pn = ptsOf(false), pe = ptsOf(true);
			var bx = new (string lab, double v, string c, string fg)[] { ("NORMAL", pn, "#ff2436", "#fff"), ("EASY", pe, "#7cc8ff", "#0b1426") };
			double bs = classicUi ? 130 : 250;
			for (int i = 0; i < 2; i++) { var (lab, v, c, fg) = bx[i]; double cx = W / 2 + (i != 0 ? -bs : bs), y = 420; rr(cx - 110, y - 50, 220, 100, 18); g.fillStyle = CARD; g.fill(); g.strokeStyle = c; g.lineWidth = 2; g.stroke(); ezBadge(cx, y - 26, 1, lab, c, fg); txt(ar(v), cx, y + 18, 40, c, 800); }
			if (!classicUi)
			{
				// New in the Godot version: the level and XP in the middle
				var (lv, into, need) = xpProg(xp); double y = 420;
				rr(W / 2 - 110, y - 50, 220, 100, 18); g.fillStyle = CARD; g.fill(); g.strokeStyle = GOLD; g.lineWidth = 2; g.stroke();
				txt($"مستوى {ar(lv)}", W / 2, y - 14, 34, GOLD, 800);
				xpBar(W / 2 - 85, y + 20, 170, 8, (double)into / need, GOLD);
				txt($"{ar(into)} / {ar(need)} XP", W / 2, y + 74, 18, "rgba(255,255,255,0.6)", 500);
			}
			btnRow(logoutArm > 0 ? "اضغط مرة ثانية للتأكيد" : "تسجيل خروج", 540, accSel == 0, logoutArm > 0 ? 420 : 360);
			zone(W / 2 - 210, 510, 420, 60);
			row(new[] { (BTN[kind].confirm, "اختيار"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
			return;
		}
		txt("حسابك", W / 2, 82, 48, "#fff");
		txt("سجّل دخول عشان تنحفظ نقاطك أونلاين ويطلع اسمك في الترتيب", W / 2, 138, 22, "rgba(255,255,255,0.6)", 500);
		field("الاسم", nameVal, 236, accSel == 0, false);
		field("كلمة السر", passVal, 352, accSel == 1, true);
		btnRow("دخول", 446, accSel == 2);
		btnRow("حساب جديد", 522, accSel == 3);
		zone(W / 2 - 300, 204, 600, 64, null, null, () => accSel = 0); zone(W / 2 - 300, 320, 600, 64, null, null, () => accSel = 1);
		zone(W / 2 - 180, 416, 360, 60, null, "confirm", () => accSel = 2); zone(W / 2 - 180, 492, 360, 60, null, "confirm", () => accSel = 3);
		if (authMsg != "") txt(authMsg, W / 2, 585, 22, authErr ? "#ff8a8a" : "rgba(255,255,255,0.7)", 500);
		else txt("أول ما تسجّل، نقاطك اللي في هذا الجهاز تنضاف لحسابك", W / 2, 585, 20, "rgba(255,255,255,0.45)", 500);
		if (kind != "kb") padLabel(kind, "للكتابة تحتاج كيبورد", W / 2, H - 82, RED3, 20);
		row(new[] { (new[] { "الأسهم" }, "تنقّل"), (new[] { "Enter" }, "اختيار"), (new[] { "Esc" }, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void drawBoard(double now)
	{
		bgShapes(now); txt("الترتيب", W / 2, 62, 46, "#fff");
		var L = boardLv(board.idx); double cy = 128;
		// the board's name with arrows right and left
		txt(L != null ? lvName(L) : boardXp ? "اللفل" : "النقاط", W / 2, cy, 34, "#fff", 800);
		if (L != null) { g.font = $"17px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = RED3; g.fillText((L.plat && L.best == null) || hcHidden(L) ? "؟؟؟" : !string.IsNullOrEmpty(L.song) ? L.song : "by " + L.by, W / 2, cy + 32); }   // a platformer song stays hidden until you finish it
		g.fillStyle = "rgba(255,255,255,0.75)";
		if (board.idx < ALL_LV().Count + bOff) zone(W / 2 - 320, cy - 26, 56, 52, null, "left");
		if (board.idx > 0) zone(W / 2 + 264, cy - 26, 56, 52, null, "right");
		if (board.idx < ALL_LV().Count + bOff) { g.beginPath(); g.moveTo(W / 2 - 300, cy); g.lineTo(W / 2 - 284, cy - 14); g.lineTo(W / 2 - 284, cy + 14); g.closePath(); g.fill(); }
		if (board.idx > 0) { g.beginPath(); g.moveTo(W / 2 + 300, cy); g.lineTo(W / 2 + 284, cy - 14); g.lineTo(W / 2 + 284, cy + 14); g.closePath(); g.fill(); }
		// normal / easy / total
		var ms = boardModes(); double mw = 150, my = cy + 78;
		for (int i = 0; i < ms.Length && !boardXp; i++)
		{
			string m = ms[i]; double cx = W / 2 + ((ms.Length - 1) / 2.0 - i) * (mw + 14); bool on = m == board.mode;
			zone(cx - mw / 2, my - 20, mw, 40, null, null, () => { board.mode = m; board.sel = -1; }); string c = m == "n" ? "#ff2436" : m == "e" ? "#7cc8ff" : "#fff";
			rr(cx - mw / 2, my - 20, mw, 40, 20); g.fillStyle = on ? c : "rgba(255,255,255,0.04)"; g.fill(); g.strokeStyle = on ? c : "rgba(255,255,255,0.2)"; g.lineWidth = 2; g.stroke();
			string lab = m == "n" ? "NORMAL" : m == "e" ? "EASY" : "TOTAL"; g.font = $"16px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = on ? (m == "n" ? "#fff" : "#0b1426") : "rgba(255,255,255,0.55)"; g.fillText(lab, cx, my + 1);
		}
		double top = my + 52, rh = 38, lx = W / 2 - 330, rw = 660;
		if (board.busy && board.list.Count == 0) txt("جاري التحميل…", W / 2, top + 120, 26, "rgba(255,255,255,0.6)", 500);
		else if (board.err && board.list.Count == 0) txt("ما فيه نت، ما قدرت أجيب الترتيب", W / 2, top + 120, 26, "#ffb35c", 500);
		else
		{
			var rows = boardRows(); var shown = rows.Take(10).ToList(); var mine = rows.FirstOrDefault(x => x.me); var vis = boardVisible();
			if (rows.Count == 0) txt(L != null ? "محد خلّص هذي المرحلة للحين" : boardXp ? "محد عنده XP للحين" : "محد عنده نقاط للحين", W / 2, top + 120, 26, "rgba(255,255,255,0.55)", 500);
			void drawRow(BoardRow x, double y)
			{
				bool sl = board.sel >= 0 && board.sel < vis.Count && x.p.uid == vis[board.sel].p.uid; rr(lx, y - rh / 2 + 2, rw, rh - 4, 10);
				int vi = vis.FindIndex(v => v.p.uid == x.p.uid); if (vi >= 0) zone(lx, y - rh / 2 + 2, rw, rh - 4, () => board.sel = vi);
				if (sl) { g.fillStyle = x.me ? $"rgba({RGB},{str(0.14 + 0.06 * pulse)})" : "rgba(255,255,255,0.09)"; g.fill(); g.strokeStyle = "#fff"; g.lineWidth = 2.5; g.stroke(); }
				else if (x.me) { g.fillStyle = $"rgba({RGB},{str(0.14 + 0.06 * pulse)})"; g.fill(); g.strokeStyle = RED; g.lineWidth = 1.5; g.stroke(); }
				else { g.fillStyle = "rgba(255,255,255,0.035)"; g.fill(); }
				string pc = x.pos == 1 ? "#ffd54a" : x.pos == 2 ? "#d8dde6" : x.pos == 3 ? "#e0a46a" : "rgba(255,255,255,0.6)";
				txt(ar(x.pos), lx + rw - 30, y + 1, 24, pc, 800);
				txt(x.p.name, lx + rw - 80, y + 1, 24, x.me ? "#fff" : "#e9e6f5", x.me ? 800 : 500, "right");
				if (L != null) { g.font = $"26px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = board.mode == "e" ? "#7cc8ff" : "#ff2436"; g.fillText(x.r, lx + 40, y + 1);
					if (x.ft >= 0) txt(tFmt(x.ft), lx + 76, y + 1, 21, "rgba(255,255,255,0.72)", 600, "left"); }   // the platformer: the time with that letter
				else if (boardXp)
				{
					txt($"مستوى {ar(xpProg((int)x.v).lv)}", lx + 120, y + 1, 22, GOLD, 800, "left");
					g.font = $"500 17px {FONT}"; g.fillStyle = "rgba(255,255,255,0.55)"; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText($"{ar(x.v)} XP", lx + 20, y + 2);
				}
				else txt($"{ar(x.v)} نقطة", lx + 20, y + 1, 22, board.mode == "e" ? "#7cc8ff" : board.mode == "n" ? "#ff6b6b" : "#fff", 800, "left");
			}
			for (int i = 0; i < shown.Count; i++) drawRow(shown[i], top + i * rh);
			if (mine != null && mine.pos > 10) { txt("…", W / 2, top + 10 * rh - 8, 22, "rgba(255,255,255,0.4)", 500); drawRow(mine, top + 10 * rh + 16); }
			if (acc == null) txt("سجّل دخول من القائمة عشان يطلع اسمك هنا", W / 2, H - 82, 20, "rgba(255,255,255,0.5)", 500);
		}
		row(new[] { (BTN[kind].move, "اختيار"), (BTN[kind].confirm, "البروفايل"), (BTN[kind].restart, "NORMAL / EASY"), (BTN[kind].back, "رجوع") }.Where(it => boardModes().Length > 1 || it.Item2 != "NORMAL / EASY").ToArray(), W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// the profile: best letter per level (normal and Easy) and a ▶ mark when there is a replay of it
	static void drawProfile(double now)
	{
		bgShapes(now); var p = prof.p; bool me = acc != null && p.uid == acc.uid;
		txt(p.name, W / 2, 60, 44, "#fff", 800);
		if (!classicUi) { int px = xpOfP(p); txt($"مستوى {ar(xpProg(px).lv)}", W / 2 + 372, 52, 26, GOLD, 800, "right"); g.font = $"500 16px {FONT}"; g.fillStyle = "rgba(255,255,255,0.55)"; g.textAlign = "right"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText($"{ar(px)} XP", W / 2 + 372, 80); }
		double pn = ptsOfP(p, "n"), pe = ptsOfP(p, "e");
		var bx = new (string lab, double v, string c, string fg)[] { ("NORMAL", pn, "#ff2436", "#fff"), ("EASY", pe, "#7cc8ff", "#0b1426"), ("TOTAL", pn + pe, "#fff", "#000") };
		for (int i = 0; i < 3; i++) { var (lab, v, c, fg) = bx[i]; double cx = W / 2 + (1 - i) * 190, yy = 112; ezBadge(cx - 34, yy, 0.9, lab, c, fg); txt(ar(v), cx + 40, yy + 1, 24, c, 800); }
		var LV = ALL_LV(); double nx = W / 2 - 110, ex = W / 2 - 290, lx = W / 2 + 330, top = 176, rh = PROF_RH, y0 = top + 22, gap = LV.Count > LEVELS.Count ? 10 : 0; int vis = Math.Min(LV.Count, PROF_VIS); double boxH = vis * rh + gap;
		ezBadge(nx, top - 20, 0.85, "NORMAL", "#ff2436", "#fff"); ezBadge(ex, top - 20, 0.85, "EASY", "#7cc8ff", "#0b1426");
		g.save(); g.beginPath(); g.rect(W / 2 - 372, y0 - rh / 2, 744, boxH); g.clip();
		for (int i = 0; i < LV.Count; i++)
		{
			var L = LV[i]; double y = y0 + (i - prof.sc) * rh + (i >= LEVELS.Count ? gap : 0); bool selR = i == prof.row; if (y < y0 - rh * 1.5 || y > y0 + boxH + rh) continue;
			rr(W / 2 - 360, y - rh / 2 + 3, 720, rh - 6, 10); g.fillStyle = selR ? $"rgba({RGB},0.10)" : "rgba(255,255,255,0.035)"; g.fill();
			txt(lvName(L), lx, y + 1, 22, selR ? "#fff" : "#e9e6f5", selR ? 800 : 500, "right");
			foreach (var (m, cx, col, c) in new[] { ("n", nx, 0, "#ff2436"), ("e", ex, 1, "#7cc8ff") })
			{
				if (noEz(L) && m == "e") continue;   // no Easy in the platformer or Hardcore
				string r = gv(p.F(m), L.id); bool sel = selR && prof.col == col, hr = hasRp(p, L, m);
				int ri = i, ci = col; if (y - rh / 2 >= y0 - rh / 2 - 1 && y + rh / 2 <= y0 - rh / 2 + boxH + 1) zone(cx - 62, y - rh / 2 + 5, 124, rh - 10, () => { prof.row = ri; prof.col = ci; });
				if (sel) { rr(cx - 62, y - rh / 2 + 5, 124, rh - 10, 9); g.strokeStyle = RED; g.lineWidth = 2.5; g.stroke(); }
				g.font = $"24px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = r != null ? c : "rgba(255,255,255,0.22)"; g.fillText(r ?? "-", hr ? cx - 16 : cx, y + 2);
				if (hr) { g.fillStyle = sel ? "#fff" : "rgba(255,255,255,0.7)"; g.beginPath(); g.moveTo(cx + 10, y - 9); g.lineTo(cx + 10, y + 9); g.lineTo(cx + 26, y); g.closePath(); g.fill(); }
			}
		}
		g.restore();
		// the scroll bar: only when there are more levels than fit on the screen
		if (LV.Count > PROF_VIS)
		{
			double tx = W / 2 + 376, th = boxH - 6, hh = Math.Max(36, th * PROF_VIS / LV.Count), ty = y0 - rh / 2 + 3 + (th - hh) * prof.sc / (LV.Count - PROF_VIS);
			rr(tx - 2, y0 - rh / 2 + 3, 4, th, 2); g.fillStyle = "rgba(255,255,255,0.08)"; g.fill(); rr(tx - 2, ty, 4, hh, 2); g.fillStyle = $"rgba({RGB},0.55)"; g.fill();
		}
		double by = y0 - rh / 2 + boxH + 28;
		if (prof.msgT > 0) { g.globalAlpha = Math.Min(1, prof.msgT); txt(prof.msg, W / 2, by + 8, 22, "#ffb35c", 500); g.globalAlpha = 1; }
		else if (prof.busy) txt("جاري تحميل الـ Replays…", W / 2, by + 8, 20, "rgba(255,255,255,0.5)", 500);
		else if (prof.err) txt("ما قدرت أجيب الـ Replays، تأكد من النت", W / 2, by + 8, 20, "#ffb35c", 500);
		else txt(me ? "▶ يعني فيه Replay لأفضل حرف لك" : "▶ يعني فيه Replay لأفضل حرف له", W / 2, by + 8, 19, "rgba(255,255,255,0.45)", 500);
		row(new[] { (BTN[kind].move, "اختيار"), (BTN[kind].confirm, "شاهد الـ Replay"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void drawRpLoad(double now)
	{
		bgShapes(now);
		if (rp.err) txt("ما قدرت أجيب الـ Replay، تأكد من النت", W / 2, H / 2, 28, "#ffb35c", 500);
		else txt("جاري تحميل الـ Replay…", W / 2, H / 2, 30, "rgba(255,255,255,0.7)", 500);
		row(new[] { (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static string rpClock(double x) { x = Math.Max(0, Math.Floor(x)); return str(Math.Floor(x / 60)) + ":" + str(x % 60).PadLeft(2, '0'); }
	public static bool rpOverlayOff;   // the level check (test/BotRun.cs) leaves the replay bar out, like its browser driver
	// v46: the "بدون الموتات" box (a replay on the PC with deaths in it), top right; □ / T ticks it
	static void rpCutBox()
	{
		double w = 236, h = 46, x = W - w - 16, y = 14, on = rp.cutOn ? 1 : 0;
		rr(x, y, w, h, 23); g.fillStyle = "rgba(8,8,14,0.82)"; g.fill(); g.strokeStyle = rp.cutOn ? "rgba(95,224,143,0.7)" : "rgba(255,255,255,0.22)"; g.lineWidth = 1.5; g.stroke();
		double bx = x + w - 36, by = y + h / 2;
		rr(bx - 11, by - 11, 22, 22, 5); g.fillStyle = rp.cutOn ? "#5fe08f" : "rgba(255,255,255,0.06)"; g.fill(); g.strokeStyle = rp.cutOn ? "#5fe08f" : "rgba(255,255,255,0.7)"; g.lineWidth = 2; g.stroke();
		if (on > 0) { g.strokeStyle = "#0b1a10"; g.lineWidth = 3.5; g.beginPath(); g.moveTo(bx - 6, by); g.lineTo(bx - 1.5, by + 5); g.lineTo(bx + 7, by - 5); g.stroke(); }
		txt("بدون الموتات", bx - 22, by + 1, 19, rp.cutOn ? "#fff" : "rgba(255,255,255,0.8)", 800, "right");
		row(new[] { (BTN[kind].sq, "") }, x + 30, by, 20, "rgba(255,255,255,0.7)", 6);
	}
	public static void drawRpOverlay(double now)
	{
		if (rpOverlayOff) return;
		string c = rp.m == "e" ? "#7cc8ff" : "#ff2436"; var K = BTN[kind];
		if (!rp.hide)
		{
			double w = 600, x = W / 2 - w / 2, y = 14;
			rr(x, y, w, 46, 23); g.fillStyle = "rgba(8,8,14,0.82)"; g.fill(); g.strokeStyle = "rgba(255,255,255,0.22)"; g.lineWidth = 1.5; g.stroke();
			g.fillStyle = c; g.beginPath(); g.moveTo(x + 24, y + 13); g.lineTo(x + 24, y + 33); g.lineTo(x + 40, y + 23); g.closePath(); g.fill();
			g.font = $"16px {TFONT}"; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = "#fff"; g.fillText(rp.bot ? "BOT" : "REPLAY", x + 50, y + 24);
			{ string nm = $"{rp.p.name}  ·  {rp.L.name}"; double fs = 21; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 13 && g.measureText(nm).width > (rp.doc.r == null ? 300 : 360)) { fs--; g.font = $"800 {str(fs)}px {FONT}"; } txt(nm, W / 2 + (rp.doc.r == null ? 10 : 30), y + 24, fs, "#fff", 800); }   // v46: long names (the platformer's) get smaller
			if (rp.doc.r != null) { g.font = $"26px {TFONT}"; g.textAlign = "center"; g.direction = "ltr"; g.fillStyle = c; g.fillText(rp.doc.r, x + w - 30, y + 24); }
			else { g.direction = "rtl"; txt(rp.pr ? (rp.doc.res == "pdone" ? "تدريب" : "تدريب · " + rpResText(rp.doc.res)) : rpResText(rp.doc.res), x + w - 20, y + 24, 15, rpResCol(rp.doc.res), 800, "right"); }   // v46: a lost or unfinished replay on the PC
			if (!rp.plat) ezBadge(x + w - (rp.doc.r == null ? 122 : 82), y + 23, 0.8, rp.m == "e" ? "EASY" : "NORMAL", c, rp.m == "e" ? "#0b1426" : "#fff");
			g.direction = "rtl";
		}
		if (rp.cutOk && !rp.hide) rpCutBox();
		if (state == "rpGap") { overlay(0.45); txt(rp.gapDead ? "خسر! يرجع لآخر حفظ" : "رجع لآخر حفظ", W / 2, H / 2, 48, PINK); }
		if (state == "rpEnd")
		{
			overlay(0.65); txt("خلص الـ Replay", W / 2, H / 2 - 90, 52, "#fff");
			if (rp.doc.r == null) txt(rpResText(rp.doc.res), W / 2, H / 2 + 30, 80, rp.doc.res == "mid" ? "#fff" : rpResCol(rp.doc.res), 800);
			else { g.font = $"130px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = c; g.shadowColor = c; g.shadowBlur = 26; g.fillText(rp.doc.r, W / 2, H / 2 + 30); g.shadowBlur = 0; g.direction = "rtl"; }
			row(new[] { (K.confirm, "شغّله من البداية"), (K.back, "رجوع") }, W / 2, H / 2 + 150, 30, "#fff", 24);
		}
		// the control bar at the bottom (like a video): shows at the start, while paused and after a button, then hides
		double a = rp.paused ? 1 : Math.Min(1, rp.barT / 0.4); if (a <= 0.01 || state == "rpEnd") return;
		g.globalAlpha = a;
		double pw = 1120, px = W / 2 - pw / 2, py = H - 150, ph = 92;
		rr(px, py, pw, ph, 18); g.fillStyle = "rgba(8,8,14,0.82)"; g.fill(); g.strokeStyle = "rgba(255,255,255,0.18)"; g.lineWidth = 1.5; g.stroke();
		double ry = py + 28, pos = rpPos(), tot = Math.Max(rp.tot, 0.01);
		// play / pause
		double bx = px + 34; g.fillStyle = "#fff";
		if (rp.paused) { g.beginPath(); g.moveTo(bx - 8, ry - 11); g.lineTo(bx - 8, ry + 11); g.lineTo(bx + 12, ry); g.closePath(); g.fill(); }
		else { g.fillRect(bx - 9, ry - 10, 7, 20); g.fillRect(bx + 3, ry - 10, 7, 20); }
		g.font = $"17px {TFONT}"; g.textBaseline = "middle"; g.direction = "ltr"; g.textAlign = "left"; g.fillStyle = "#fff";
		g.fillText(rpClock(pos) + " / " + rpClock(tot), px + 62, ry + 1);
		double x0 = px + 215, x1 = px + pw - 200;
		g.fillStyle = "rgba(255,255,255,0.15)"; rr(x0, ry - 3, x1 - x0, 6, 3); g.fill();
		g.fillStyle = c; rr(x0, ry - 3, Math.Max(6, (x1 - x0) * pos / tot), 6, 3); g.fill();
		for (int i = 1; i < rp.doc.segs.Count; i++) { var ps = rp.doc.segs[i - 1]; bool dead = ((ps.b[(ps.n - 1) * 3 + 2] >> 1) & 7) == 0; double cx = x0 + (x1 - x0) * rp.off[i] / tot; g.fillStyle = dead ? PINK : "rgba(255,255,255,0.75)"; g.fillRect(cx - 1.5, ry - 9, 3, 18); }
		g.fillStyle = "#fff"; circle(x0 + (x1 - x0) * pos / tot, ry, 8);
		string rs = str(rp.rate) + "x"; g.font = $"17px {TFONT}"; g.textAlign = "right"; g.direction = "ltr"; g.fillStyle = rp.rate == 1 ? "rgba(255,255,255,0.8)" : "#ffd25c"; g.fillText(rs, px + pw - 24, ry + 1);
		g.direction = "rtl"; if (!rp.team) txt($"المحاولة {ar(rp.seg + 1)} من {ar(rp.doc.segs.Count)}", px + pw - 78, ry + 1, 17, "rgba(255,255,255,0.7)", 500, "right");
		row(new[] { (K.confirm, rp.paused ? "تشغيل" : "إيقاف"), (K.move, "يمين ويسار: ٥ ثواني"), (K.small, rp.paused ? "عُشر ثانية" : "ثانية"), (K.move, "فوق وتحت: السرعة"), (K.restart, rp.hide ? "إظهار الكلام" : "إخفاء الكلام"), (K.back, "رجوع") }, W / 2, py + 68, 23, "rgba(255,255,255,0.75)", 16);
		g.globalAlpha = 1;
	}
	static void drawMain(double now)
	{
		double k = clamp((now - mainAt) / 650, 0, 1), e = k < 0.5 ? 2 * k * k : 1 - JS.pow(-2 * k + 2, 2) / 2, oa = clamp((now - mainAt - 250) / 450, 0, 1);
		g.globalAlpha = oa; bgShapes(now); g.globalAlpha = 1; drawTitle(W / 2, H / 2 - 20 + (180 - (H / 2 - 20)) * e, 110 + (78 - 110) * e);
		g.globalAlpha = oa;
		var items = mainItems(); bool tight = items.Length > MAIN.Length; double y0 = items.Length > 8 ? 262 : items.Length > 7 ? 270 : 288, dy = items.Length > 8 ? 49 : items.Length > 7 ? 52 : 58;   // v46: 9 items with Hardcore
		for (int i = 0; i < items.Length; i++)
		{
			var (s, id) = items[i]; double y = tight ? y0 + i * dy : APP ? 300 + i * 66 : 310 + i * 72, bh = tight ? dy - 4 : 64; bool sel = id == menuSel, hc = id == HC;
			zone(W / 2 - 240, y - bh / 2, 480, bh, () => menuSel = id);
			double hx = hc ? hcItemDx() : 0; if (hc) { g.globalAlpha = oa * hcItemAlpha(); g.save(); g.translate(hx, 0); }
			if (sel)
			{
				rr(W / 2 - 240, y - bh / 2, 480, bh, 16); g.fillStyle = $"rgba({RGB},{str(0.08 + 0.08 * pulse)})"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke();
				g.save(); g.translate(W / 2 + 205, y); g.rotate(now / 420); g.fillStyle = RED; double q = 8 * (1 + 0.3 * pulse); g.fillRect(-q, -q, 2 * q, 2 * q); g.restore();
			}
			if (hc && !easy) { g.save(); g.shadowColor = HC_RED; g.shadowBlur = 14 + 8 * pulse; txt(s, W / 2, y, tight ? (sel ? dy * 0.62 : dy * 0.535) : sel ? 40 : 34, sel ? "#fff" : HC_RED, 800); g.restore(); g.shadowBlur = 0; }
			else txt(s, W / 2, y, tight ? (sel ? dy * 0.62 : dy * 0.535) : sel ? 40 : 34, sel ? "#fff" : "rgba(255,255,255,0.5)", 800);
			if (hc) { g.restore(); g.globalAlpha = oa; }
		}
		drawEasyBox(now);
		drawPtsBox(now); drawAccBox(now); drawLvBadge();
		g.globalAlpha = 1;
		if (kind != "kb") { g.globalAlpha = oa; padLabel(kind, kind == "ps" ? "يد سوني متصلة" : "يد إكس بوكس متصلة", 0, fpsShow ? H - 58 : H - 36, RED3, 20, W - 28); g.globalAlpha = 1; }
	}
	static void padIcon(string type, double cx, double cy, double s, string col = null)
	{
		col ??= RED;
		g.save(); g.translate(cx, cy); g.fillStyle = col; g.shadowColor = col; g.shadowBlur = s * 0.25;
		double w = s * 1.5, h = s * 0.62; rr(-w / 2, -h / 2, w, h, h * 0.45); g.fill();
		g.beginPath(); g.ellipse(-w * 0.36, h * 0.32, s * 0.26, s * 0.36, 0.35, 0, TAU); g.fill();
		g.beginPath(); g.ellipse(w * 0.36, h * 0.32, s * 0.26, s * 0.36, -0.35, 0, TAU); g.fill();
		g.shadowBlur = 0; g.fillStyle = "#000"; double st = s * 0.1, b = s * 0.045;
		void dpad(double x, double y) { g.fillRect(x - st * 0.35, y - st, st * 0.7, st * 2); g.fillRect(x - st, y - st * 0.35, st * 2, st * 0.7); }
		void face(double x, double y) { double d = st * 0.75; circle(x, y - d, b); circle(x, y + d, b); circle(x - d, y, b); circle(x + d, y, b); }
		if (type == "ps") { dpad(-w * 0.33, -h * 0.05); face(w * 0.33, -h * 0.05); circle(-w * 0.13, h * 0.28, st * 0.8); circle(w * 0.13, h * 0.28, st * 0.8); rr(-w * 0.13, -h * 0.38, w * 0.26, h * 0.36, 4); g.fill(); }
		else { circle(-w * 0.33, -h * 0.08, st * 0.8); dpad(-w * 0.13, h * 0.26); circle(w * 0.13, h * 0.26, st * 0.8); face(w * 0.33, -h * 0.08); circle(0, -h * 0.2, st * 0.55); }
		g.restore();
	}
	public static void padLabel(string type, string text, double cx, double y, string col, double fs = 20, double? rightEdge = null)
	{
		double s = fs * 1.5, gap = 12; g.font = $"500 {str(fs)}px {FONT}"; double tw = g.measureText(text).width, tot = s * 1.5 + gap + tw;
		double right = rightEdge ?? cx + tot / 2; padIcon(type, right - s * 0.75, y, s); txt(text, right - s * 1.5 - gap, y, fs, col, 500, "right");
	}
	static void lockIcon(double x, double y) { g.strokeStyle = "rgba(255,255,255,0.3)"; g.lineWidth = 5; g.beginPath(); g.arc(x, y - 10, 14, Math.PI, 0); g.stroke(); rr(x - 22, y - 10, 44, 34, 6); g.fillStyle = "rgba(255,255,255,0.3)"; g.fill(); }
	static double lift(string lk, bool sel) { LIFT.TryGetValue(lk, out var v); LIFT[lk] = v + ((sel ? 1 : 0) - v) * Math.Min(1, fdt * 14); return -10 * LIFT[lk]; }
	static void drawLevels(double now)
	{
		bgShapes(now); txt("المراحل الرئيسية", W / 2, 105, 48, "#fff");
		int n = LEVELS.Count; double cw = 240, chh = 330, gp = 28; int vis = 4, shown = Math.Min(vis, n); double total = shown * cw + (shown - 1) * gp, cy = H / 2 + 20;
		for (int i = 0; i < n; i++)
		{
			var L = LEVELS[i];
			double d = i - lvlScr, al = clamp(1 - Math.Max(-d, d - (shown - 1)), 0, 1); if (al <= 0) continue; g.globalAlpha = al;
			bool sel = i == lvlSel; double x = W / 2 + total / 2 - cw / 2 - d * (cw + gp);
			int ii = i; if (al > 0.99) zone(x - cw / 2, cy - chh / 2, cw, chh, () => lvlSel = ii);
			if (sel && shakeCard > 0) x += JS.sin(shakeCard * 60) * 8 * shakeCard / 0.4;
			double lf = lift("L" + i, sel);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 18); g.fillStyle = sel ? CARDS : CARD; g.fill();
			g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			txt(ar(i + 1), x, cy - 90 + lf, 90, L.ok ? (sel ? RED : "#fff") : "rgba(255,255,255,0.15)", 800);
			if (L.ok)
			{
				{ double fs = 28; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 16 && g.measureText(L.name).width > cw - 24) { fs--; g.font = $"800 {str(fs)}px {FONT}"; } txt(L.name, x, cy + 5 + lf, fs, "#fff", 800); }
				// (no level has a tag in this version)
				g.font = $"20px {TFONT}"; g.fillStyle = RED3; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(L.song, x, cy + 52 + lf);
				if (L.err) txt("الأغنية ما اشتغلت", x, cy + 125 + lf, 20, RED3, 500);
				else if (L.buf == null) txt("جاري التجهيز…", x, cy + 125 + lf, 20, "rgba(255,255,255,0.6)", 500);
				else if (L.best != null || L.bestE != null) drawBests(L, x, cy + lf);
				if (redoneTagOn(L)) redoneTag(x, cy - chh / 2 + lf, now);
			}
			else { lockIcon(x, cy + 20 + lf); txt("قريبًا...", x, cy + 95 + lf, 26, "rgba(255,255,255,0.4)", 500); }
			g.globalAlpha = 1;
		}
		if (lvlScr > 0.01) zone(W - 66, cy - 40, 56, 80, null, null, () => lvlSel = Math.Max(0, (int)round(lvlScr) - 1));
		if (lvlScr + shown < n - 0.01) zone(10, cy - 40, 56, 80, null, null, () => lvlSel = Math.Min(n - 1, (int)round(lvlScr) + shown));
		if (lvlScr > 0.01) { g.fillStyle = "rgba(255,255,255,0.5)"; g.beginPath(); g.moveTo(W - 30, cy); g.lineTo(W - 46, cy - 14); g.lineTo(W - 46, cy + 14); g.closePath(); g.fill(); }
		if (lvlScr + shown < n - 0.01) { g.fillStyle = "rgba(255,255,255,0.5)"; g.beginPath(); g.moveTo(30, cy); g.lineTo(46, cy - 14); g.lineTo(46, cy + 14); g.closePath(); g.fill(); }
		row(new[] { (BTN[kind].confirm, "العب"), (BTN[kind].sq, "تدريب"), (BTN[kind].restart, "شوف البوت"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static readonly string[] HOWTO =
	{
		"الهدف: توصل لآخر المرحلة وأنت عايش، وكل الهجمات ماشية على إيقاع الأغنية.",
		"أي شي لونه وردي يضرّك، مهما كان شكله أو حجمه أو من وين طلع.",
		"قبل ما يضرب أي هجوم يطلع لك باهت أو شفاف كتحذير، هذا وقتك تبتعد.",
		"الاندفاع: وأنت تندفع ما يصيبك شي، فتقدر تعبر من خلال أي هجوم.",
		"بعد كل اندفاعة لازم تنتظر لحظة قصيرة قبل اللي بعدها.",
		"الأرواح: عندك ٥ مربعات فوق على اليسار. كل ضربة تاخذ وحدة، وبعدها تكون محمي ثانيتين.",
		"الشريط اللي تحت يوضح وين وصلت، والمثلثات نقاط حفظ. لو خسرت ترجع لآخر نقطة عديتها.",
		"في آخر كل مرحلة زعيم، وكل مرحلة أصعب من اللي قبلها.",
		"التقييم:  S ما انضربت أبد  •  A انضربت بس ما خسرت  •  B خسرت مرة  •  C خسرت مرتين أو أكثر",
	};
	static void drawHowto()
	{
		txt("طريقة اللعب", W / 2, 58, 46, "#fff");
		var K = BTN[kind];
		row(new[] { (K.move, "تحرّك"), (K.dash, "اندفع"), (K.pause, "وقّف اللعب") }, W / 2, 130, 34, "#fff", 24);
		for (int i = 0; i < HOWTO.Length; i++) { double y = 198 + i * 49; g.fillStyle = RED; g.fillRect(W - 168, y - 5, 10, 10); txt(HOWTO[i], W - 182, y, 23, "#e9e6f5", 500, "right"); }
		row(new[] { (K.back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// settings. New in the Godot version: the fullscreen tick box became the screen mode row (window / borderless / real fullscreen)
	public static int setSel = 0;
	static string[] setRows() => classicUi || state != "settings" ? new[] { "vol", "screen", "vsync", "fps", "counter", "res" } : new[] { "vol", "screen", "vsync", "fps", "counter", "res", "extra" };   // "أشياء إضافية": the menu's settings only (v50)
	static void checkBox(double cx, double y, bool on, bool sel)
	{
		double s = 36, bx = cx - s / 2;
		rr(bx, y - s / 2, s, s, 8); if (on) { g.fillStyle = RED; if (sel) { g.shadowColor = RED; g.shadowBlur = 12 * (0.6 + 0.6 * pulse); } g.fill(); g.shadowBlur = 0; }
		g.strokeStyle = on || sel ? RED : "rgba(255,255,255,0.45)"; g.lineWidth = 2.5; g.stroke();
		if (on) { g.strokeStyle = "#fff"; g.lineWidth = 4; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath(); g.moveTo(bx + 8, y + 1); g.lineTo(bx + 15, y + 9); g.lineTo(bx + 28, y - 8); g.stroke(); g.lineCap = "butt"; g.lineJoin = "miter"; }
	}
	static void optArrows(double cx, double y, bool on)
	{
		g.fillStyle = on ? "rgba(255,255,255,0.85)" : "rgba(255,255,255,0.3)"; g.beginPath(); g.moveTo(cx - 150, y); g.lineTo(cx - 136, y - 11); g.lineTo(cx - 136, y + 11); g.closePath(); g.fill();
		g.beginPath(); g.moveTo(cx + 150, y); g.lineTo(cx + 136, y - 11); g.lineTo(cx + 136, y + 11); g.closePath(); g.fill();
	}
	public static void drawSettings()
	{
		txt("الإعدادات", W / 2, 88, 48, "#fff");
		var rows = setRows(); double cx = W / 2 - 150, lx = W / 2 + 350;
		for (int i = 0; i < rows.Length; i++)
		{
			string k = rows[i]; double y = (rows.Length > 6 ? 160 : 172) + i * (rows.Length > 6 ? 64 : 70); bool sel = i == setSel; int ii = i;
			if (!classicUi)
			{
				zone(W / 2 - 390, y - 30, 780, 60, () => setSel = ii, k is "screen" or "vsync" or "counter" or "extra" ? "confirm" : null, freeRow(k) ? () => { numRow = k; numBuf = ""; } : null);
				if (k == "fps") { zone(cx - 164, y - 24, 42, 48, () => setSel = ii, null, () => numSet("fps", fpsCap - 1), rep: true); zone(cx + 122, y - 24, 42, 48, () => setSel = ii, null, () => numSet("fps", fpsCap + 1), rep: true); }
				else if (k is "screen" or "res") { zone(cx - 164, y - 24, 42, 48, () => setSel = ii, "left"); zone(cx + 122, y - 24, 42, 48, () => setSel = ii, "right"); }
				else if (k == "vol") { double tot = 10 * 20 + 9 * 6, x0 = cx - tot / 2 + 30; zone(x0 - 6, y - 28, tot + 12, 56, () => setSel = ii, null, null, gx => numSet("vol", (int)round(clamp((gx - x0) / tot, 0, 1) * 100))); }
			}
			if (sel) { rr(W / 2 - 390, y - 30, 780, 60, 16); g.fillStyle = $"rgba({RGB},{str(0.08 + 0.08 * pulse)})"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
			string lc = sel ? "#fff" : "rgba(255,255,255,0.55)";
			if (k == "vol")
			{
				txt("مستوى الصوت", lx, y, 32, lc, 800, "right");
				int n = 10; double bw = 20, gp = 6, tot = n * bw + (n - 1) * gp, x0 = cx - tot / 2 + 30, on = round(vol * 10);
				if (!classicUi)
				{
					volBars(x0, y, sel, bw, gp); int pc = (int)round(vol * 100);
					if (numRow == "vol") numBox(x0 - 62, y, 96); else txt(ar(pc) + "٪", x0 - 18, y, 30, pc != 0 ? (sel ? RED : "rgba(255,255,255,0.7)") : "rgba(255,255,255,0.5)", 800, "right");
					continue;
				}
				for (int j = 0; j < n; j++) { double x = x0 + j * (bw + gp), h = 12 + j * 3; rr(x, y - h / 2, bw, h, 5); g.fillStyle = j < on ? RED : "rgba(255,255,255,0.1)"; if (j < on && sel) { g.shadowColor = RED; g.shadowBlur = 12 * (0.6 + 0.6 * pulse); } g.fill(); g.shadowBlur = 0; }
				txt(ar(on * 10) + "٪", x0 - 18, y, 30, on != 0 ? (sel ? RED : "rgba(255,255,255,0.7)") : "rgba(255,255,255,0.5)", 800, "right");
			}
			else if (k == "screen")
			{
				txt("الشاشة", lx, y, 32, lc, 800, "right"); optArrows(cx, y, sel);
				string s = screenLabel(screenMode); double fs = 30; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 18 && g.measureText(s).width > 252) { fs--; g.font = $"800 {str(fs)}px {FONT}"; }
				txt(s, cx, y, fs, sel ? RED : "rgba(255,255,255,0.7)", 800);
			}
			else if (k == "counter") { txt("عداد الفريمات", lx, y, 32, lc, 800, "right"); checkBox(cx, y, fpsShow, sel); }
			else if (k == "vsync") { txt("V-Sync", lx, y, 32, lc, 800, "right"); checkBox(cx, y, vsyncOn, sel); }
			else if (k == "fps") { txt("Framecap", lx, y, 32, lc, 800, "right"); optArrows(cx, y, sel); if (numRow == "fps") numBox(cx, y, 150); else txt("‪" + str(fpsCap) + " FPS‬", cx, y, 30, sel ? RED : "rgba(255,255,255,0.7)", 800); }
			else if (k == "extra") { txt("أشياء إضافية", lx, y, 32, lc, 800, "right"); g.fillStyle = sel ? "rgba(255,255,255,0.85)" : "rgba(255,255,255,0.3)"; g.beginPath(); g.moveTo(cx - 14, y); g.lineTo(cx, y - 11); g.lineTo(cx, y + 11); g.closePath(); g.fill(); }
			else if (k == "res") { txt("الدقة", lx, y, 32, lc, 800, "right"); optArrows(cx, y, sel); txt(resLabel(resSet), cx, y, 30, sel ? RED : "rgba(255,255,255,0.7)", 800); }
		}
		string r = rows[setSel]; string hint = "";
		if (r == "screen") hint = "تقدر كمان تبدّل بين النافذة وملء الشاشة بزر F11";
		else if (r == "vsync") hint = vsyncOn ? "الحركة أنعم وبدون تقطيع في الشاشة" : "اللعبة تمشي لين الـ Framecap، والرسم مع كل تحديث للشاشة";
		else if (r == "fps") hint = vsyncOn ? "أعلى شي يوصل له سرعة شاشتك، لأن V-Sync شغال" : "أعلى عدد فريمات في الثانية";
		else if (r == "counter") hint = "يطلع عدد الفريمات تحت يمين الشاشة";
		else if (r == "extra") hint = "فيها: تقييم المراحل";
		else if (r == "res") hint = "أقل دقة = اللعبة أخف على الجهاز، بس الرسم أقل وضوح";
		if (numRow != null) hint = r == "fps" ? $"اكتب رقم من {ar(FPS_MIN)} إلى {ar(FPS_MAX)}" : "اكتب رقم من ٠ إلى ١٠٠";
		if (hint != "") txt(hint, W / 2, H - 118, 24, "rgba(255,255,255,0.6)", 500);
		if (numRow != null) row(new[] { (new[] { "Enter" }, "تم"), (new[] { "Esc" }, "إلغاء") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
		else if (freeRow(r)) row(new[] { (BTN[kind].move, "غيّر ١"), (BTN[kind].small, "غيّر ١٠") }.Concat(kind == "kb" ? new[] { (BTN[kind].confirm, "اكتب الرقم") } : new (string[], string)[0]).Append((BTN[kind].back, "رجوع")).ToArray(), W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
		else if (r == "extra") row(new[] { (BTN[kind].confirm, "افتح"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
		else row(new[] { (BTN[kind].move, "غيّر"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void drawFriends(double now)
	{
		bgShapes(now); txt("مراحل الشباب", W / 2, 105, 48, "#fff");
		double cw = 240, chh = 330, gp = 28; int vis = 4; double cy = H / 2 + 20; int shown = Math.Min(vis, FRIENDS.Count); double total = shown * cw + (shown - 1) * gp;
		for (int i = 0; i < FRIENDS.Count; i++)
		{
			double d = i - friScr, al = clamp(1 - Math.Max(-d, d - (shown - 1)), 0, 1); if (al <= 0) continue; g.globalAlpha = al; var L = FRIENDS[i]; bool sel = i == friSel; double x = W / 2 + total / 2 - cw / 2 - d * (cw + gp); double lf = lift("F" + i, sel);
			int ii = i; if (al > 0.99) zone(x - cw / 2, cy - chh / 2, cw, chh, () => friSel = ii);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 18); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			if (isAscii(L.name)) { double fs = 24; g.font = $"{str(fs)}px {TFONT}"; while (fs > 14 && g.measureText(L.name).width > cw - 24) { fs--; g.font = $"{str(fs)}px {TFONT}"; } g.fillStyle = "#fff"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(L.name, x, cy - 95 + lf); }
			else { double fs = 26; g.font = $"800 {str(fs)}px {FONT}"; while (fs > 16 && g.measureText(L.name).width > cw - 24) { fs--; g.font = $"800 {str(fs)}px {FONT}"; } txt(L.name, x, cy - 95 + lf, fs, "#fff", 800); }
			txt("اختيار", x, cy - 48 + lf, 18, "rgba(255,255,255,0.55)", 500);
			g.font = $"22px {TFONT}"; g.fillStyle = RED3; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(L.by, x, cy - 18 + lf);
			if (L.err) txt("الأغنية ما اشتغلت", x, cy + 115 + lf, 20, RED3, 500);
			else if (L.buf == null) txt("جاري التجهيز…", x, cy + 115 + lf, 20, "rgba(255,255,255,0.6)", 500);
			else if (L.best != null || L.bestE != null) drawBests(L, x, cy + lf);
			if (!string.IsNullOrEmpty(L.ver))
			{
				double vy = cy + chh / 2 + 28; string lab = "موثّقة من:"; g.font = $"500 17px {FONT}"; double lw = g.measureText(lab).width; g.font = $"17px {TFONT}"; double nw = g.measureText(L.ver).width; double tot = 18 + 8 + lw + 8 + nw, right = x + tot / 2;
				g.strokeStyle = RED; g.lineWidth = 3; g.lineCap = "round"; g.lineJoin = "round"; g.beginPath(); g.moveTo(right - 16, vy); g.lineTo(right - 10, vy + 6); g.lineTo(right - 1, vy - 6); g.stroke();
				txt(lab, right - 26, vy, 17, "rgba(255,255,255,0.6)", 500, "right"); g.font = $"17px {TFONT}"; g.fillStyle = RED3; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(L.ver, x - tot / 2, vy + 1);
			}
			if (redoneTagOn(L)) redoneTag(x, cy - chh / 2 + lf, now);
			g.globalAlpha = 1;
		}
		if (friScr > 0.01) zone(W - 66, cy - 40, 56, 80, null, null, () => friSel = Math.Max(0, (int)round(friScr) - 1));
		if (friScr + shown < FRIENDS.Count - 0.01) zone(10, cy - 40, 56, 80, null, null, () => friSel = Math.Min(FRIENDS.Count - 1, (int)round(friScr) + shown));
		if (friScr > 0.01) { g.fillStyle = "rgba(255,255,255,0.5)"; g.beginPath(); g.moveTo(W - 30, cy); g.lineTo(W - 46, cy - 14); g.lineTo(W - 46, cy + 14); g.closePath(); g.fill(); }
		if (friScr + shown < FRIENDS.Count - 0.01) { g.fillStyle = "rgba(255,255,255,0.5)"; g.beginPath(); g.moveTo(30, cy); g.lineTo(46, cy - 14); g.lineTo(46, cy + 14); g.closePath(); g.fill(); }
		row(new[] { (BTN[kind].confirm, "العب"), (BTN[kind].sq, "تدريب"), (BTN[kind].restart, "شوف البوت"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	public static void drawSpawn() { string sk = playerSkin(); if (sk != "") { g.save(); g.translate(200, H / 2); skinBody(sk, 1, performance.now()); g.restore(); return; } g.save(); g.translate(200, H / 2); g.shadowColor = CYAN; g.shadowBlur = 14; g.fillStyle = CYAN; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(-3, -3, 6, 6); g.restore(); }
	public static void drawLevelInfo(double now)
	{
		double s = (now - introAt) / 1000; var L = cur;
		double a = s < 0.5 ? s / 0.5 : s < 2.4 ? 1 : clamp(1 - (s - 2.4) / 0.7, 0, 1), sl = 1 - clamp(s / 0.5, 0, 1), x0 = 60 - 50 * sl * sl;
		double h = !string.IsNullOrEmpty(L.by) ? 132 : 100, top = H - 52 - h;
		g.globalAlpha = a; g.fillStyle = CYAN; g.fillRect(x0, top, 6, h);
		if (isAscii(L.name)) { g.font = $"40px {TFONT}"; g.fillStyle = "#fff"; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(L.name, x0 + 26, top + 30); } else txt(L.name, x0 + 26, top + 30, 46, "#fff", 800, "left");
		if (!string.IsNullOrEmpty(L.by)) { txt("اختيار", x0 + 26, top + 78, 20, "rgba(255,255,255,0.6)", 500, "left"); g.font = $"26px {TFONT}"; g.fillStyle = PINK; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(L.by, x0 + 26, top + h - 20); }
		else { g.font = $"26px {TFONT}"; g.fillStyle = PINK; g.textAlign = "left"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(L.song, x0 + 26, top + h - 22); }
		g.globalAlpha = 1;
	}
}
