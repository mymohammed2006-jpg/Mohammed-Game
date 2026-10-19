using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html: themes and menu songs (63-75), ensureAudio (137), startLevel (150), pause/resume/toMenu (6997-6999),
// setEasy (6965), drawing helpers (7040-7101), pause menu (7249), bot (7811-7826), drawGhost/drawPrEnd (7893-7908)
public static partial class G
{
	public class Theme { public string c, c2, c3, rgb, bg, card, cardSel; }
	public static readonly Dictionary<string, Theme> THEMES = new()
	{
		["normal"] = new Theme { c = "#ff2436", c2 = "#c4001d", c3 = "#ff6b6b", rgb = "255,36,54", bg = "#000", card = "#0e0607", cardSel = "#1c0a0d" },
		["easy"] = new Theme { c = "#7cc8ff", c2 = "#a8e6cf", c3 = "#bfe3ff", rgb = "124,200,255", bg = "#0b1426", card = "#0f1c33", cardSel = "#16294a" },
	};
	public static string RED, RED2, RED3, RGB, MBG, CARD, CARDS;
	public static void applyTheme(bool e)
	{
		var T = e ? THEMES["easy"] : THEMES["normal"];
		RED = T.c; RED2 = T.c2; RED3 = T.c3; RGB = T.rgb; MBG = T.bg; CARD = T.card; CARDS = T.cardSel;
	}
	static int _themeInit = Init0();
	static int Init0() { applyTheme(false); return 0; }

	public class MenuSong { public double bpm, off, drop, loop; public AudioStream buf; }
	public static readonly Dictionary<string, MenuSong> MENUS = new()
	{
		["normal"] = new MenuSong { bpm = 91.15, off = 0.42, drop = 8, loop = 200 },
		["easy"] = new MenuSong { bpm = 120.19, off = 0.105, drop = 12, loop = 204 },
	};
	public static double MENU_SPB, MENU_OFF, MENU_DROP, MENU_LOOP;
	public static void applyMenu()
	{
		string k = easy && MENUS["easy"].buf != null ? "easy" : "normal"; var M = MENUS[k];
		if (k == menuKey && menuBuf == M.buf) return;
		if (menuSrc != null) stopMenu(); menuKey = k; menuBuf = M.buf;
		MENU_SPB = 60 / M.bpm; MENU_OFF = M.off; MENU_DROP = M.off + M.drop * MENU_SPB; MENU_LOOP = M.off + M.loop * MENU_SPB;
	}
	public static void setEasy(bool v) { easy = v; applyTheme(v); applyMenu(); localStorage.setItem("mg_easy", v ? "1" : "0"); }

	public static void ensureAudio()
	{
		if (actx.state == "suspended" && state != "paused" && state != "pauseSet" && !(rp.paused && (state is "replay" or "rpGap" or "rpEnd"))) actx.resume();
	}

	public static int menuSel = 0, pauseSel = 0;
	public static void startLevel(Level L, string from) { cur = L; fromList = from; levelTiming(L); requestStart(0); introOn = true; }
	public static void pause() { state = "paused"; pauseSel = 0; actx.suspend(); }
	public static void resume() { state = "play"; actx.resume(); }
	public static void toMenu() { stopMusic(); state = "main"; menuSel = 0; player = null; hazards = new List<Hz>(); actx.resume(); }
	public static readonly string[] PAUSE = { "أكمل اللعب", "إعادة من آخر حفظ", "إعادة من البداية", "الإعدادات", "الخروج من المرحلة" };
	public static readonly string[] PR_PAUSE = { "أكمل اللعب", "إعادة من نقطة التدريب", "غيّر نقطة التدريب", "الإعدادات", "الخروج من المرحلة" };

	// ---------- drawing helpers ----------
	public static void txt(string s, double x, double y, double size, string col, int weight = 800, string align = "center")
	{
		g.font = $"{weight} {str(size)}px {FONT}"; g.fillStyle = col; g.textAlign = align; g.textBaseline = "middle"; g.direction = "rtl"; g.fillText(s, x, y);
	}
	public static void rr(double x, double y, double w, double h, double r)
	{
		g.beginPath(); g.moveTo(x + r, y); g.arcTo(x + w, y, x + w, y + h, r); g.arcTo(x + w, y + h, x, y + h, r); g.arcTo(x, y + h, x, y, r); g.arcTo(x, y, x + w, y, r); g.closePath();
	}
	static readonly string[] ROUND = { "x", "o", "sq", "tri", "A", "B", "X", "Y", "stick" };
	public static double iconW(string n, double s)
	{
		if (ROUND.Contains(n)) return s;
		g.font = $"800 {str(s * 0.46)}px {FONT}"; return Math.Max(s, g.measureText(n).width + s * 0.7);
	}
	static readonly System.Text.RegularExpressions.Regex ArabicRx = new("[؀-ۿ]");
	public static void icon(string n, double cx, double y, double s)
	{
		double r = s / 2; g.lineCap = "round";
		if (n == "stick") { g.fillStyle = "#2a2440"; circle(cx, y, r); g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; g.beginPath(); g.arc(cx, y, r - 3, 0, TAU); g.stroke(); g.fillStyle = "#e9e6f5"; circle(cx, y, r * 0.38); return; }
		var PS = new Dictionary<string, string> { ["x"] = "#8fb4ff", ["o"] = "#ff6b7a", ["sq"] = "#ff8ad8", ["tri"] = "#4fe0a8" };
		if (PS.TryGetValue(n, out var pc))
		{
			g.fillStyle = "#1d1830"; circle(cx, y, r); g.strokeStyle = "rgba(255,255,255,0.25)"; g.lineWidth = 1.5; g.beginPath(); g.arc(cx, y, r - 1, 0, TAU); g.stroke();
			g.strokeStyle = pc; g.lineWidth = Math.Max(2.5, s * 0.09); double k = r * 0.42; g.beginPath();
			if (n == "x") { g.moveTo(cx - k, y - k); g.lineTo(cx + k, y + k); g.moveTo(cx + k, y - k); g.lineTo(cx - k, y + k); }
			else if (n == "o") g.arc(cx, y, k, 0, TAU);
			else if (n == "sq") g.rect(cx - k * 0.85, y - k * 0.85, k * 1.7, k * 1.7);
			else { g.moveTo(cx, y - k); g.lineTo(cx + k, y + k * 0.75); g.lineTo(cx - k, y + k * 0.75); g.closePath(); }
			g.stroke(); return;
		}
		var XB = new Dictionary<string, string> { ["A"] = "#5cc85c", ["B"] = "#ef5350", ["X"] = "#42a5f5", ["Y"] = "#ffca28" };
		if (XB.TryGetValue(n, out var xc)) { g.fillStyle = xc; circle(cx, y, r); g.font = $"800 {str(s * 0.55)}px {FONT}"; g.fillStyle = "#111"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(n, cx, y + 2); return; }
		double w = iconW(n, s); rr(cx - w / 2, y - r, w, s, Math.Min(10, r * 0.6)); g.fillStyle = "#2a2440"; g.fill(); g.strokeStyle = "rgba(255,255,255,0.35)"; g.lineWidth = 1.5; g.stroke();
		g.font = $"800 {str(s * 0.46)}px {FONT}"; g.fillStyle = "#fff"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = ArabicRx.IsMatch(n) ? "rtl" : "ltr"; g.fillText(n, cx, y + 2);
	}
	// a row of instructions: [buttons, text] pairs, laid out right to left
	public static void row((string[] ic, string l)[] items, double cx, double y, double s = 34, string col = "#fff", double fs = 26)
	{
		const double gap = 14, sep = 50, orW = 30;
		double lw(string l) { g.font = $"500 {str(fs)}px {FONT}"; return g.measureText(l).width; }
		var ws = items.Select(it => { double w = 0; for (int i = 0; i < it.ic.Length; i++) w += iconW(it.ic[i], s) + (i > 0 ? orW : 0); return w + gap + lw(it.l); }).ToList();
		double x = cx + (ws.Sum() + sep * (items.Length - 1)) / 2;
		foreach (var (ic, l) in items)
		{
			double xr = x; string za = classicUi ? null : iconAct(ic);
			for (int i = 0; i < ic.Length; i++) { var n = ic[i]; if (i > 0) { txt("أو", x - orW / 2, y, fs * 0.65, "rgba(255,255,255,0.55)", 500); x -= orW; } double w = iconW(n, s); icon(n, x - w / 2, y, s); x -= w; }
			x -= gap; double tw = lw(l); txt(l, x, y, fs, col, 500, "right"); x -= tw;
			if (za != null) zone(x - 8, y - s / 2 - 8, xr - x + 16, s + 16, null, za);
			x -= sep;
		}
	}
	public static void ltxt(string s, double x, double y, double size, string col, string align = "center")
	{
		g.font = $"{str(size)}px {TFONT}"; g.fillStyle = col; g.textAlign = align; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, x, y); g.direction = "rtl";
	}

	public static void drawArena()
	{
		g.fillStyle = ARENA; g.fillRect(0, 0, W, H);
		g.strokeStyle = $"rgba(140,100,230,{str(0.04 + 0.09 * pulse * energy + 0.03 * energy)})"; g.lineWidth = 1; g.beginPath();
		for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
		for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
		g.stroke();
	}
	public static void drawPlayer()
	{
		var p = player; if (p == null || state == "dead") return;
		string sk = playerSkin();
		if (classicUi) for (int i = 0; i < p.trail.Count; i++) { var q = p.trail[i]; g.globalAlpha = ((double)i / p.trail.Count) * (q.d ? 0.5 : 0.15); g.fillStyle = CYAN; g.fillRect(q.x - 7, q.y - 7, 14, 14); }
		else { walkTrail(sk, p); if (sk != "") skinTrail(sk, p); }
		g.globalAlpha = (p.hurt > 0 && Math.Floor(p.hurt * 18) % 2 != 0) ? 0.25 : 1;
		g.save(); g.translate(p.x, p.y);
		{
			bool dsh = p.dash > 0; double a = dsh ? JS.atan2(p.ddy, p.ddx) : (p.sa ?? 0), s = p.sk ?? (dsh ? 1 : 0), sx = 1 + 0.75 * s, sy = 1 - 0.38 * s;
			double fl = (dsh && p.dt0 != null) ? clamp(1 - (songT - p.dt0.Value) / 0.06, 0, 1) : 0;
			{ double sp0 = dsh ? a : p.ang, q = Math.PI / 2; double r = a - sp0; r -= round(r / q) * q; g.rotate(a - r); g.scale(sx, sy); }
			if (sk == "")
			{
				string col = fl > 0 ? $"rgb({str(round(45 + 210 * fl))},{str(round(226 + 29 * fl))},{str(round(230 + 25 * fl))})" : CYAN;
				g.shadowColor = fl > 0 ? "#fff" : CYAN; g.shadowBlur = 14 + 10 * fl; g.fillStyle = col; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = ARENA; g.fillRect(-3, -3, 6, 6);
			}
			else
			{
				skinBody(sk, 1, performance.now());
				// the same short white flash when a dash starts
				if (fl > 0) { g.shadowColor = "#fff"; g.shadowBlur = 10 * fl; g.fillStyle = $"rgba(255,255,255,{str(0.85 * fl)})"; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; }
			}
		}
		g.restore(); g.globalAlpha = 1;
	}
	public static void drawHUD()
	{
		var p = player; if (p == null) return;
		for (int i = 0, hn = pr.on && !pr.inf ? pr.lv : 5; i < hn; i++)
		{
			double x = 34 + i * 28, y0 = 34;
			if (i < p.hp) { g.fillStyle = CYAN; g.fillRect(x - 9, y0 - 9, 18, 18); }
			else { g.strokeStyle = "rgba(45,226,230,0.35)"; g.lineWidth = 2; g.strokeRect(x - 8, y0 - 8, 16, 16); }
		}
		double x0 = 220, x1 = W - 220, y = H - 26, endT = B(cur.end), prog = clamp(songT / endT, 0, 1);
		g.fillStyle = "rgba(255,255,255,0.12)"; g.fillRect(x0, y - 2, x1 - x0, 4);
		g.fillStyle = CYAN; g.fillRect(x0, y - 2, (x1 - x0) * prog, 4);
		foreach (var c in cur.cps.Skip(1)) { double cx = x0 + (x1 - x0) * B(c) / endT; g.fillStyle = c <= checkpoint ? CYAN : PINK; g.beginPath(); g.moveTo(cx, y - 12); g.lineTo(cx - 7, y - 4); g.lineTo(cx + 7, y - 4); g.closePath(); g.fill(); }
		g.fillStyle = "#fff"; g.fillRect(x0 + (x1 - x0) * prog - 5, y - 5, 10, 10);
		g.fillStyle = "rgba(255,255,255,0.7)"; g.beginPath(); g.moveTo(x1 + 14, y - 9); g.lineTo(x1 + 14, y + 9); g.lineTo(x1 + 28, y); g.closePath(); g.fill();
		if (cpMsg > 0) { g.globalAlpha = Math.Min(1, cpMsg); txt("✓ تم الحفظ", W / 2, 70, 30, CYAN); g.globalAlpha = 1; }
	}
	public static void overlay(double a = 0.62) { g.fillStyle = $"rgba(5,3,10,{str(a)})"; g.fillRect(0, 0, W, H); }

	// ---------- the bot's runs (songs/bot.js -> data/bot.json) ----------
	public class BotRunJ { public string r { get; set; } public int hz { get; set; } public List<SegJ> segs { get; set; } }
	public class SegJ { public double cp { get; set; } public double seed { get; set; } public double t0 { get; set; } public int n { get; set; } public string d { get; set; } }
	static Dictionary<string, BotRunJ> BOT_RUNS;
	static readonly Dictionary<string, RpDoc> botCache = new();
	static void LoadBotRuns()
	{
		if (BOT_RUNS != null) return;
		BOT_RUNS = new();
		using var f = Godot.FileAccess.Open("res://data/bot.json", Godot.FileAccess.ModeFlags.Read);
		if (f == null) return;
		BOT_RUNS = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, BotRunJ>>(f.GetAsText()) ?? new();
	}
	public static RpDoc botDoc(Level L, string m)
	{
		string k = L.id + m; if (botCache.TryGetValue(k, out var c)) return c;
		LoadBotRuns(); if (!BOT_RUNS.TryGetValue(k, out var R)) return null;
		return botCache[k] = new RpDoc { r = R.r, hz = R.hz, segs = R.segs.Select(s => new RpSeg { cp = s.cp, seed = i32(s.seed), t0 = s.t0, n = s.n, b = unpackRp(s.d) }).ToList() };
	}
	public class BotPt { public double x, y; public int d; }
	public static BotPt botPos(RpDoc d, double t)
	{
		if (d == null) return null; var s = d.segs[0]; double fi = (t - s.t0) * d.hz; if (fi < 0 || fi >= s.n - 1) return null;
		int i = (int)Math.Floor(fi); double k = fi - i; int o = i * 3; var b = s.b;
		return new BotPt { x = (b[o] + (b[o + 3] - b[o]) * k) / 50, y = (b[o + 1] + (b[o + 4] - b[o + 1]) * k) / 50, d = b[o + 2] & 1 };
	}
	public static void noBot() { toastPad = null; toast = "البوت ما عنده جولة لهذي المرحلة للحين"; toastT = 2.4; }
	public static void openBot(Level L, string from)
	{
		string m = easy ? "e" : "n"; var d = botDoc(L, m); if (d == null) { noBot(); return; }
		fromList = from; rp.p = new RpPerson { name = "البوت", uid = "bot" }; rp.L = L; rp.m = m; rp.doc = d; rp.err = false; rp.bot = true; rp.back = from; state = "rpLoad"; ensureAudio();
	}

	public static void drawGhost()
	{
		if (!pr.on || !pr.ghost || pr.doc == null || player == null) return; var q = botPos(pr.doc, songT); if (q == null) return;
		for (int k = 1; k <= 8; k++) { var f = botPos(pr.doc, songT + k * 0.1); if (f == null) break; g.globalAlpha = 0.5 * (1 - k / 9.0); g.fillStyle = "#fff"; g.fillRect(f.x - 2.5, f.y - 2.5, 5, 5); }
		g.save(); g.translate(q.x, q.y); g.globalAlpha = 0.16; g.fillStyle = "#fff"; g.fillRect(-9, -9, 18, 18); g.globalAlpha = 0.7; g.strokeStyle = "#fff"; g.lineWidth = 2; g.strokeRect(-9, -9, 18, 18); g.restore(); g.globalAlpha = 1;
	}
	public static void prHUD()
	{
		if (!pr.on || player == null) return;
		double x0 = 220, x1 = W - 220, y = H - 26, endT = B(cur.end), sx = x0 + (x1 - x0) * clamp(pr.t / endT, 0, 1); g.fillStyle = "#fff"; g.fillRect(sx - 1.5, y - 10, 3, 20);
		if (pr.inf) ltxt("∞", 34 + 5 * 28 + 6, 34, 26, CYAN, "left");
		var parts = new List<string> { "تدريب" }; if (spd != 1) parts.Add(ar(spd * 100) + "٪"); parts.Add($"انضربت {ar(hitsTotal)}"); string s = string.Join("  ·  ", parts);
		g.font = $"800 19px {FONT}"; double w = g.measureText(s).width + 40; rr(W / 2 - w / 2, 14, w, 36, 18); g.fillStyle = "rgba(8,8,14,0.7)"; g.fill(); g.strokeStyle = "rgba(255,255,255,0.18)"; g.lineWidth = 1.5; g.stroke();
		txt(s, W / 2, 32, 19, "rgba(255,255,255,0.85)", 800);
	}
	public static void drawPrEnd(double now)
	{
		overlay(0.7); txt("خلّصت التدريب!", W / 2, H / 2 - 120, 60, "#fff");
		txt(deaths != 0 ? $"انضربت {ar(hitsTotal)} مرة    خسرت {ar(deaths)} مرة" : $"انضربت {ar(hitsTotal)} مرة", W / 2, H / 2 - 30, 32, hitsTotal != 0 ? "#e9e6f5" : CYAN, 800);
		if (hitsTotal == 0) txt("ولا ضربة! جاهز للـ S", W / 2, H / 2 + 20, 26, CYAN, 500);
		txt("التدريب ما ينحسب له تقييم ولا نقاط", W / 2, H / 2 + 62, 20, "rgba(255,255,255,0.5)", 500);
		if (now - winAt > 600) { var K = BTN[kind]; row(new[] { (K.confirm, "مرة ثانية من نفس النقطة") }, W / 2, H / 2 + 130); row(new[] { (K.back, "التدريب") }, W / 2, H / 2 + 185); }
	}
}
