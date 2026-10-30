using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Local play, the platformer's versus (UI/Local.cs): like online (Plat/VsNet.cs), everyone plays the same level at the same moment,
// here on one screen: each square has its own run (its pieces, its checkpoint) and goes through the others. One song for all: a part of
// it comes in when the first one takes its piece. A death: back at your own checkpoint after a second. The match ends as soon as only one
// player hasn't reached the gate: that one lost, the others won (places and times). No letter, points or XP, like online.
public static partial class G
{
	static readonly string[] LC_PAUSE_V = { "أكمل اللعب", "إعادة المرحلة كاملة (للكل)", "الإعدادات", "اطلع لشاشة اللاعبين" };
	const double LC_RESPAWN = 1000;   // ms
	static void lcVsBegin()
	{
		var L = lc.PL; plStart(L, true); hitsTotal = deaths = 0;
		foreach (var s in lc.seats)
		{
			if (s.i == 0) { s.run = pl; s.p = player; continue; }
			s.run = new PlatRun { L = L, m = pl.m, w = pl.w, h = pl.h, stemOn = pl.stemOn, cx = pl.cx, cy = pl.cy, t = pl.t, introAt = pl.introAt };
			s.p = new Player { x = pl.cx, y = pl.cy, r = 5, hp = P_HP, inv = 1.0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		}
	}
	static LcSeat lcCurSeat => lcSeat(lcCur);
	// Plat.cs plDamage: this seat's last heart
	public static void lcVsDown() { var s = lcCurSeat; if (s != null) s.deadAt = performance.now(); }
	// Plat.cs plTiles: this seat is through the gate
	public static void lcVsGate()
	{
		var s = lcCurSeat; if (s == null || s.fin >= 0) return;
		s.fin = tRound(pl.t); int place = lc.seats.Count(q => q.fin >= 0);
		burst(s.p.x, s.p.y, lcCol(s.i), 40, 450); flash = Math.Max(flash, 0.15);
		sfx(tt => { sTone(tt, 659, 0.12, "triangle", 0.12); sTone(tt + 0.08, 988, 0.12, "triangle", 0.12); sTone(tt + 0.16, 1319, 0.25, "triangle", 0.1); });
		lcNote($"{s.name} وصل! المركز {ar(place)}");
		if (lc.seats.Count(q => q.fin < 0) <= 1) lcVsEnd();
	}
	static void lcVsEnd()
	{
		var done = lc.seats.Where(q => q.fin >= 0).OrderBy(q => q.fin).ThenBy(q => q.i).Select(q => q.i);
		lc.res = done.Concat(lc.seats.Where(q => q.fin < 0).Select(q => q.i)).ToArray();
		state = "lcVWin"; winAt = performance.now(); lc.endAt = winAt;
	}
	static void lcVsRespawn(LcSeat s)
	{
		var r = s.run; r.down = r.atGate = false; r.vx = r.vy = 0; r.canDash = true; r.dashQ = r.jumpQ = false; s.deadAt = -1;
		s.p = new Player { x = r.cx, y = r.cy, r = 5, hp = P_HP, inv = 1.5, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
		if (s.i == 0) player = s.p;
	}
	static bool lcVsFrame(Inp inp, bool anyPz, double now, double dt)
	{
		if (state == "pltPlay")
		{
			if (anyPz) { state = "pltPause"; pauseSel = 0; actx.suspend(); return true; }
			lcVsUpdate(now, dt); return true;
		}
		if (state == "pltPause")
		{
			int c = lcMenu(inp, LC_PAUSE_V);
			if (c == 0) { state = "pltPlay"; actx.resume(); }
			else if (c == 1) { actx.resume(); plStopMusic(); lcGo(); }
			else if (c == 2) { state = "pltSet"; setSel = 0; }
			else if (c == 3) lcLeave();
			return true;
		}
		if (state == "lcVWin")
		{
			pl.msgT -= dt;
			if (now - winAt > 900 && inp.confirm) { plStopMusic(); lcGo(); }
			else if (inp.back) lcLeave();
			return true;
		}
		return false;   // the intro, the settings: the usual code (Plat.cs)
	}
	static void lcVsUpdate(double now, double dt)
	{
		var r0 = lc.seats[0].run;
		foreach (var s in lc.seats)
		{
			var r = s.run; r.voices = r0.voices; r.startWhen = r0.startWhen;
			if (s.inp.jump) r.jumpQ = true; if (s.inp.pdash) r.dashQ = true; r.jumpHeld = s.inp.jumpHeld;
		}
		double target = actx.currentTime - r0.startWhen;
		if (target - r0.t > SIM_MAX) { r0.n += (long)Math.Floor((target - SIM_MAX - r0.t) / STEP); r0.t = -0.12 + r0.n * STEP; }
		int k = 0;
		while (state == "pltPlay" && -0.12 + (r0.n + 1) * STEP <= target + 1e-9)
		{
			long nn = r0.n + 1; k++; bool nw = -0.12 + (nn + 1) * STEP > target + 1e-9, ticked = false;
			foreach (var s in lc.seats)
			{
				var r = s.run; r.n = nn;
				if (s.fin >= 0 || s.deadAt >= 0) { r.t = -0.12 + nn * STEP; continue; }
				if (ticked) r.beatN = (long)Math.Floor((-0.12 + nn * STEP) / r.L.spb); ticked = true;   // the soft beat tick once, not once per square
				pl = r; player = s.p; lcCur = s.i; hitsTotal = s.hits; deaths = s.deaths; rumbleOnly = s.dev;
				r.t = -0.12 + (nn - 1) * STEP; plStep(STEP, nw ? s.inp.x : r.inX, nw ? s.inp.y : 0, nw);
				s.hits = hitsTotal; s.deaths = deaths; rumbleOnly = -2;
				if (state != "pltPlay") break;
			}
			lcCur = -1; pl = r0; player = lc.seats[0].p;
		}
		if (k > 0) foreach (var s in lc.seats) s.run.inX = s.inp.x;
		lcCur = -1; pl = r0; player = lc.seats[0].p;
		foreach (var s in lc.seats) if (s.deadAt >= 0 && now - s.deadAt > LC_RESPAWN && state == "pltPlay") lcVsRespawn(s);
		foreach (var s in lc.seats) s.run.msgT -= dt;
	}

	// ---------- drawing (Plat/PlatDraw.cs drawPlat calls these) ----------
	// a piece is drawn while someone still racing hasn't taken it; small dots under it for the ones who did
	public static bool lcVsPieceGone(int x, int y) { string k = x + "," + y; return lc.seats.All(s => s.fin >= 0 || s.run.got.Contains(k)); }
	public static void lcVsPieceDots(int x, int y)
	{
		string k = x + "," + y; int i = 0; var who = lc.seats.Where(s => s.run.got.Contains(k)).ToList(); if (who.Count == 0) return;
		double X = x * PT + PT / 2 - (who.Count - 1) * 5;
		foreach (var s in who) { g.fillStyle = lcCol(s.i); g.fillRect(X + i * 10 - 3, y * PT + PT - 4, 6, 6); i++; }
	}
	public static bool lcVsAnyOpen() => lc.seats.Any(s => s.fin < 0 && s.run.got.Count >= plPieces());
	public static void lcVsDrawSquares(double now)
	{
		var sk = lcSkins();
		for (int k = lc.seats.Count - 1; k >= 0; k--)
		{
			var s = lc.seats[k]; if (s.fin >= 0 || s.p == null) continue;
			if (s.deadAt >= 0) { double e = clamp((now - s.deadAt) / LC_RESPAWN, 0, 1); g.globalAlpha = 0.6 * (1 - e); txt("✕", s.p.x, s.p.y, 26, lcCol(s.i), 800); g.globalAlpha = 1; continue; }
			if (state == "pltIntro")
			{
				double t = (now - pl.introAt) / 1000; if (t < 2.4) continue;
				double f = clamp((t - 2.4) / 0.45, 0, 1), dy = -260 * (1 - f * f); s.p.y += dy; tinted(dupHue(sk, s.i), () => drawPlayer(s.p, s.sk ?? "", k == 0 ? 1 : 0.85)); s.p.y -= dy; continue;
			}
			tinted(dupHue(sk, s.i), () => drawPlayer(s.p, s.sk ?? "", k == 0 ? 1 : 0.85));
			g.globalAlpha = 0.9; txt(s.name, s.p.x, s.p.y - 30, 16, lcCol(s.i), 800); g.globalAlpha = 1;
		}
	}
	// the HUD: a chip for each one (colour, name, pieces, through or not), the song's parts, the line
	public static void lcVsHUD(double now)
	{
		int n = lc.seats.Count, tot = plPieces(); double cw = 230, gap = 14, all = n * cw + (n - 1) * gap;
		for (int k = 0; k < n; k++)
		{
			var s = lc.seats[k]; double x = W / 2 + all / 2 - cw / 2 - k * (cw + gap), y = H - 30; string c = lcCol(s.i);
			rr(x - cw / 2, y - 18, cw, 36, 12); g.fillStyle = "rgba(8,6,16,0.7)"; g.fill(); g.strokeStyle = c; g.lineWidth = 2; g.stroke();
			string st = s.fin >= 0 ? $"وصل {tFmt(s.fin)}" : s.deadAt >= 0 ? "مات" : $"{ar(s.run.got.Count)}/{ar(tot)}";
			txt(s.name, x + cw / 2 - 14, y + 1, 18, c, 800, "right");
			txt(st, x - cw / 2 + 14, y + 1, 18, s.fin >= 0 ? NGOLD : "#fff", 700, "left");
		}
		{
			int ns = pl.L.stems.Length; double x0 = W / 2 + (ns - 1) * 22;
			for (int i = 0; i < ns; i++) plPieceIcon(x0 - i * 44, 24, 0.75, i, pl.stemOn[i] ? 1 : 0.25);
		}
		if (lcMsgT > 0 && lcMsg != null) { g.globalAlpha = clamp(lcMsgT / 0.4, 0, 1); txt(lcMsg, W / 2, 70, 30, NGOLD); g.globalAlpha = 1; }
		else { var mr = lc.seats.Select(s => s.run).OrderByDescending(r => r.msgT).First(); if (mr.msgT > 0) { g.globalAlpha = Math.Min(1, mr.msgT); txt(mr.msg, W / 2, 70, 30, GOLD); g.globalAlpha = 1; } }
	}
	public static void drawLcVWin(double now)
	{
		overlay(0.78); double ts = now - lc.endAt;
		{ double e = clamp(ts / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3); g.save(); g.translate(W / 2, 110); g.scale(sc, sc); g.globalAlpha = e; txt("النتيجة", 0, 0, 80, NGOLD); g.restore(); g.globalAlpha = 1; }
		var res = lc.res ?? Array.Empty<int>(); double rh = 70, top = 250 - (res.Length - 2) * rh / 4;
		for (int i = 0; i < res.Length; i++)
		{
			var s = lc.seats[res[i]]; double y = top + i * rh, a = clamp((ts - 250 - i * 120) / 200, 0, 1); if (a <= 0) continue;
			g.globalAlpha = a; bool won = s.fin >= 0; string c = lcCol(s.i);
			rr(W / 2 - 340, y - rh / 2 + 5, 680, rh - 10, 14); g.fillStyle = "rgba(255,255,255,0.06)"; g.fill(); g.strokeStyle = c; g.lineWidth = 2; g.stroke();
			string pc = !won ? PINK : i == 0 ? "#ffd54a" : i == 1 ? "#d8dde6" : "#e0a46a";
			txt(ar(i + 1), W / 2 + 305, y + 1, 30, pc, 800);
			g.save(); g.translate(W / 2 + 255, y); coopSquare(c, s.sk ?? "", 1, now, "#1b0f26"); g.restore();
			txt(s.name, W / 2 + 225, y + 1, 28, "#fff", 800, "right");
			txt(won ? tFmt(s.fin) : "ما وصل", W / 2 - 60, y + 1, 26, "#e9e6f5", 600);
			txt(won ? "فاز" : "خسر", W / 2 - 275, y + 1, 28, won ? NGOLD : PINK, 800);
		}
		g.globalAlpha = 1;
		if (ts > 900)
		{
			txt("الضد ما فيه حرف ولا نقاط ولا XP", W / 2, Math.Max(top + res.Length * rh + 20, 540), 20, "rgba(255,255,255,0.5)", 500);
			row(new[] { (BTN[kind].confirm, "العب مرة ثانية"), (BTN[kind].back, "شاشة اللاعبين") }, W / 2, H - 40, 34, "#fff", 26);
		}
	}
}
