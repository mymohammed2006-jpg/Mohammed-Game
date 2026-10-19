using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 7724-7810 (draw) and 7926-8029 (step). The menu screens' parts are in drawMenus / stepMenus (UI thread).
public static partial class G
{
	public static double fdt = 0, last = 0;
	public static double gatePressAt = 0, introAt = 0, shakeCard = 0;
	public static bool gateHint = false;
	public static int lvlSel = 0, friSel = 0, lvlFirst = 0, friFirst = 0;
	public static double lvlScr = 0, friScr = 0;
	public static int fpsN = 0; public static double fpsAt = 0, fpsVal = 0;

	public static double ee2(double e) => 1 - JS.pow(1 - e, 3);
	public static double menuPos()
	{
		if (menuSrc == null) return 0; double e = actx.currentTime - menuStart;
		return e < MENU_LOOP ? e : MENU_DROP + ((e - MENU_LOOP) % (MENU_LOOP - MENU_DROP));
	}

	static readonly string[] IN_GAME = { "play", "paused", "pauseSet", "dead", "win", "starting", "replay", "rpGap", "rpEnd", "prEnd" };
	static readonly string[] MENUISH = { "gate", "gateWait", "splash", "main", "levels", "modes", "pltList", "howto", "settings", "friends", "points", "account", "board", "profile", "rpLoad", "chal", "skins" };
	static readonly string[] FS_HINT = { "main", "levels", "modes", "pltList", "pltPause", "pltSet", "friends", "howto", "settings", "points", "account", "board", "profile", "paused", "pauseSet", "chal", "skins" };

	public static void draw(double now)
	{
		trCheck(now);
		plThemeSync();
		g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, g.width, g.height);
		double sx = (random() * 2 - 1) * shake, sy = (random() * 2 - 1) * shake;
		g.setTransform(scale, 0, 0, scale, offX + sx * scale, offY + sy * scale);
		g.save(); g.beginPath(); g.rect(0, 0, W, H); g.clip();
		bool inGame = player != null && IN_GAME.Contains(state);
		bool menuish = MENUISH.Contains(state);
		if (inGame)
		{
			double bt = BT(songT), bar = Math.Floor(bt / 4); double ev = cur.energy[(int)clamp(bar, 0, cur.energy.Length - 1)];
			energy = ev != 0 && !double.IsNaN(ev) ? ev : 0.12; pulse = JS.exp(-(((bt % 1) + 1) % 1) * 5) * (0.35 + 0.65 * energy);
		}
		else if (menuish && menuSrc != null) { double mp = menuPos(), bt = (mp - MENU_OFF) / MENU_SPB; energy = 0.6; pulse = mp >= MENU_DROP - 0.02 ? JS.exp(-(((bt % 1) + 1) % 1) * 5) : 0; }
		else { energy = 0.3; pulse = 0; }
		g.fillStyle = menuish && state != "gate" && state != "gateWait" ? MBG : "#000"; g.fillRect(0, 0, W, H);
		if (menuish && state != "gate" && state != "gateWait" && state != "splash")
		{
			g.strokeStyle = $"rgba({RGB},{str(0.04 + 0.08 * pulse)})"; g.lineWidth = 1; g.beginPath();
			for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
			for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
			g.stroke();
		}
		var K = BTN[kind];
		if (drawMenus(now, inGame)) { }
		else if (!inGame) { drawSpawn(); if (state == "levelIntro") drawLevelInfo(now); }
		else
		{
			foreach (var h in hazards) h.draw(songT);
			if (gloom > 0.01 && player != null)
			{
				var gr = g.createRadialGradient(player.x, player.y, 170, player.x, player.y, 520); gr.addColorStop(0, "rgba(0,0,0,0)"); gr.addColorStop(1, $"rgba(0,0,0,{str(gloom)})");
				g.fillStyle = gr; g.fillRect(0, 0, W, H);
			}
			foreach (var q in particles) { g.globalAlpha = Math.Max(0, q.life / q.max); g.fillStyle = q.col; g.fillRect(q.x - q.s / 2, q.y - q.s / 2, q.s, q.s); }
			g.globalAlpha = 1;
			drawGhost(); drawPlayer(); drawHUD(); prHUD();
		}
		if (state == "paused")
		{
			overlay(0.75); txt("اللعبة متوقفة", W / 2, 150, 56, "#fff");
			var list = pr.on ? PR_PAUSE : PAUSE;
			for (int i = 0; i < list.Length; i++)
			{
				string s = list[i]; double y = 262 + i * 80; bool sel = i == pauseSel; int ii = i;
				zone(W / 2 - 260, y - 34, 520, 68, () => pauseSel = ii);
				if (sel) { rr(W / 2 - 260, y - 34, 520, 68, 16); g.fillStyle = $"rgba({RGB},0.12)"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); g.save(); g.translate(W / 2 + 225, y); g.rotate(now / 420); g.fillStyle = RED; g.fillRect(-8, -8, 16, 16); g.restore(); }
				txt(s, W / 2, y, sel ? 38 : 32, sel ? "#fff" : "rgba(255,255,255,0.5)", 800);
			}
		}
		if (state == "pauseSet") { overlay(0.95); drawSettings(); }
		if (state == "dead")
		{
			overlay();
			{ double e = clamp((now - deadAt) / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3); g.save(); g.translate(W / 2, H / 2 - 110); g.scale(sc, sc); g.globalAlpha = e; txt("خسرت!", 0, 0, 90, PINK); g.restore(); g.globalAlpha = 1; }
			{
				double e = clamp((now - deadAt - 250) / 600, 0, 1), ee = 1 - JS.pow(1 - e, 3), bw = 420, bx = W / 2 - bw / 2, by = H / 2 - 20, pc = deadPct * ee;
				g.globalAlpha = clamp((now - deadAt - 200) / 200, 0, 1); rr(bx, by - 5, bw, 10, 5); g.fillStyle = "rgba(255,255,255,0.12)"; g.fill(); if (pc > 0.005) { rr(bx, by - 5, bw * pc, 10, 5); g.fillStyle = PINK; g.fill(); }
				txt($"وصلت {ar(round(pc * 100))}٪", W / 2, by + 34, 30, "#fff", 800); g.globalAlpha = 1;
			}
			if (now - deadAt > 700)
			{
				row(new[] { (K.confirm, pr.on ? "من نقطة التدريب" : "كمّل من آخر حفظ") }, W / 2, H / 2 + 110, 38, "#fff", 30);
				row(pr.on ? new[] { (K.back, "التدريب") } : new[] { (K.restart, "من بداية المرحلة"), (K.back, "القائمة") }, W / 2, H / 2 + 170);
			}
		}
		if (state == "win")
		{
			overlay(0.7);
			txt("خلّصت المرحلة!", W / 2, H / 2 - 160, 64, "#fff");
			{
				double fall = 180, ts = now - (winSlam - fall);
				if (ts > 0)
				{
					double e = clamp(ts / fall, 0, 1), sc = e < 1 ? 1 + 2.2 * JS.pow(1 - e, 2) : 1 + 0.18 * JS.exp(-(ts - fall) / 90);
					if (e >= 1 && !winHit) { winHit = true; shake = 14; flash = 0.18; burst(W / 2, H / 2 - 40, lastEz ? "#7cc8ff" : "#ff2436", 40, 520); rumble(160, 0.7, 0.5); sfx(tt => { sTone(tt, 90, 0.25, "sine", 0.35); sNoise(tt, 0.18, "lowpass", 700, 0.35); }); }
					g.save(); g.translate(W / 2, H / 2 - 40); g.scale(sc, sc); g.globalAlpha = Math.Min(1, e * 1.5);
					g.font = $"150px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
					if (lastRank == "S" && !lastEz) sShine("S", 0, 0, 110, now, 30);
					else { string c = lastEz ? "#7cc8ff" : "#ff2436"; g.fillStyle = c; g.shadowColor = c; g.shadowBlur = 30; g.fillText(lastRank, 0, 0); g.shadowBlur = 0; }
					g.restore(); g.globalAlpha = 1;
					if (lastEz && e >= 1) ezBadge(W / 2 + 100, H / 2 - 95, 1.3);
				}
			}
			{ double a = clamp((now - winSlam - 150) / 250, 0, 1); g.globalAlpha = a; txt($"انضربت {ar(hitsTotal)} مرة    خسرت {ar(deaths)} مرة", W / 2, H / 2 + 70 + 10 * (1 - a), 28, "#e9e6f5", 500); g.globalAlpha = 1; }
			if (lastGain > 0 && now > winSlam)
			{
				string gc = lastEz ? "#7cc8ff" : "#ff2436"; double ce = clamp((now - winSlam - 120) / 700, 0, 1), cv = round(lastGain * (1 - JS.pow(1 - ce, 3)));
				g.globalAlpha = clamp((now - winSlam) / 150, 0, 1); g.save(); g.translate(W / 2 - 170, H / 2 - 40); g.rotate(now / 420); g.fillStyle = gc; g.shadowColor = gc; g.shadowBlur = 10; g.fillRect(-7, -7, 14, 14); g.restore();
				txt($"+{ar(cv)} نقطة", W / 2 - 190, H / 2 - 40, 34, gc, 800, "right"); g.globalAlpha = 1;
			}
			drawWinXp(now);
			if (now - Math.Max(winAt + 800, winSlam + 300) > 0) { row(new[] { (K.confirm, "العب مرة ثانية") }, W / 2, H / 2 + 135); row(new[] { (K.back, "المراحل") }, W / 2, H / 2 + 190); }
		}
		if (state == "prEnd") drawPrEnd(now);
		if ((state is "replay" or "rpGap" or "rpEnd") && rp.doc != null) drawRpOverlay(now);
		if (state == "starting" && audioHint) { overlay(0.4); txt("اضغط على الشاشة مرة وحدة عشان يشتغل الصوت 🔊", W / 2, H / 2, 32, "#fff", 500); }
		if (gatePassed && !fsOK() && FS_HINT.Contains(state)) txt("اضغط F11 عشان ترجع ملء الشاشة", W / 2, state == "paused" ? H - 40 : 22, 18, "rgba(255,255,255,0.5)", 500);
		if (toastT > 0 && state != "main")
		{
			g.globalAlpha = Math.Min(1, toastT);
			if (toastPad != null) padLabel(toastPad, toast, W / 2, H - 78, menuish ? RED3 : CYAN, 22); else txt(toast, W / 2, H - 78, 22, menuish ? RED3 : CYAN, 500);
			g.globalAlpha = 1;
		}
		if (flash > 0) { g.fillStyle = $"rgba(255,255,255,{str(Math.Min(0.8, flash))})"; g.fillRect(0, 0, W, H); }
		trPost(now);
		drawCorner(now);
		g.restore();
		zonesFlip();
	}

	static readonly string[] MENU_MUSIC = { "main", "levels", "modes", "pltList", "howto", "settings", "friends", "points", "account", "board", "profile", "rpLoad", "practice", "news", "chal", "skins" };
	public static void step(double now, bool nd = false)
	{
		double dt = Math.Min(0.05, Math.Max(0, now - last) / 1000); last = now; fdt = dt;
		var inp = readInput(); mouseApply(inp); syncField();
		syncTick(now);
		if (state == "gate") { if ((f11Hit || fsOK()) && menuBuf != null) { ensureAudio(); gatePressAt = now; gatePassed = true; state = "gateWait"; } f11Hit = false; }
		else if (state == "gateWait") { if (actx.state == "running") { playMenu(0); state = "splash"; } else if (now - gatePressAt > 600) { gateHint = true; if (inp.any) ensureAudio(); } }
		else if (state == "splash") { if (menuPos() >= MENU_DROP) { goMain(now); } else if (inp.any && menuPos() > 1.2) { stopMenu(); playMenu(MENU_DROP); goMain(now); } }
		else if (stepMenus(inp, now, dt)) { }
		else if (state == "rpLoad") { if (inp.back || inp.pz) state = rp.bot ? rp.back : "profile"; else if (rp.doc != null && rp.L.buf != null && actx.state == "running") rpStart(); else ensureAudio(); }
		else if (state is "replay" or "rpGap" or "rpEnd")
		{
			if (inp.back || inp.pz) exitReplay();
			else
			{
				rpInput(inp, dt);
				if (state == "replay") updateReplay(dt);
				else if (state == "rpGap") { if (rp.paused) rp.gapAt += dt * 1000; else if (performance.now() - rp.gapAt > 1300 / Math.Max(1, rp.rate)) rpSeg(rp.seg + 1); }
			}
		}
		else if (state == "practice") prMenuInput(inp);
		else if (state == "prStarting") { if (actx.state == "running" && cur.buf != null) prBegin(); else ensureAudio(); }
		else if (state == "prEnd") { if (performance.now() - winAt > 600 && inp.confirm) prGo(); else if (inp.back || inp.pz) prToMenu(); }
		else if (state == "starting")
		{
			if (actx.state == "running" && cur.buf != null) { if (startBeat == 0 && introOn) { introOn = false; state = "levelIntro"; introAt = now; player = null; hazards = new List<Hz>(); particles = new List<Particle>(); } else beginRun(startBeat); }
			else if (now - startReqAt > 500) audioHint = true;
		}
		else if (state == "levelIntro") { if (now - introAt > 3200) beginRun(0); }
		else if (state == "play") { if (inp.pz) pause(); else { updatePlay(pr.on ? dt * spd : dt, inp); if (pr.on) cpMsg = 0; } }
		else if (state == "paused" && pr.on) prPauseInput(inp);
		else if (state == "paused")
		{
			int pn = PAUSE.Length; if (inp.up) pauseSel = (pauseSel + pn - 1) % pn; if (inp.down) pauseSel = (pauseSel + 1) % pn;
			if (inp.pz || inp.back) resume();
			else if (inp.confirm)
			{
				if (pauseSel == 0) resume(); else if (pauseSel == 1) { stopMusic(); requestStart(checkpoint); } else if (pauseSel == 2) { stopMusic(); requestStart(0); }
				else if (pauseSel == 3) { state = "pauseSet"; setSel = 0; } else { stopMusic(); player = null; hazards = new List<Hz>(); state = fromList; actx.resume(); }
			}
		}
		else if (state == "dead" && pr.on) { if (now - deadAt > 700 && inp.confirm) prGo(); else if (inp.back) prToMenu(); }
		else if (state == "dead") { if (now - deadAt > 700 && inp.confirm) requestStart(checkpoint); else if (now - deadAt > 700 && inp.restart) requestStart(0); else if (inp.back) toMenu(); }
		else if (state == "win") { if (now - winAt > 800 && inp.confirm) { stopMusic(); requestStart(0); } else if (inp.back) { stopMusic(); player = null; hazards = new List<Hz>(); state = fromList; } }
		pvUpdate(now);
		if (pv.src != null || pv.had) { if (menuSrc != null) stopMenu(); }
		else if (MENU_MUSIC.Contains(state)) { if (menuSrc == null && menuBuf != null && actx.state == "running") playMenu(MENU_DROP); }
		else if (menuSrc != null && state != "splash") stopMenu();
		double edt = (state is "replay" or "rpGap" or "rpEnd") ? (rp.paused ? 0 : dt * rp.rate) : dt;
		foreach (var q in particles) { q.x += q.vx * edt; q.y += q.vy * edt; double f = JS.pow(0.04, edt); q.vx *= f; q.vy *= f; q.life -= edt; }
		particles = particles.Where(q => q.life > 0).ToList();
		gloom += (gloomT - gloom) * Math.Min(1, edt * 3); shake = Math.Max(0, shake - edt * 50); flash = Math.Max(0, flash - edt * 2.2); cpMsg -= edt; toastT -= dt; shakeCard = Math.Max(0, shakeCard - dt);
		{
			double k = Math.Min(1, dt * 12);
			if (lvlSel < lvlFirst) lvlFirst = lvlSel; else if (lvlSel > lvlFirst + 3) lvlFirst = lvlSel - 3;
			if (friSel < friFirst) friFirst = friSel; else if (friSel > friFirst + 3) friFirst = friSel - 3;
			double lt = clamp(lvlFirst, 0, Math.Max(0, LEVELS.Count - 4)), ft = clamp(friFirst, 0, Math.Max(0, FRIENDS.Count - 4));
			lvlScr += (lt - lvlScr) * k; friScr += (ft - friScr) * k; if (Math.Abs(lt - lvlScr) < 0.001) lvlScr = lt; if (Math.Abs(ft - friScr) < 0.001) friScr = ft;
		}
		if (!nd) draw(now);
		fpsN++; if (now - fpsAt >= 500) { fpsVal = fpsAt != 0 ? round(fpsN * 1000 / (now - fpsAt)) : 0; fpsN = 0; fpsAt = now; }
		if (fpsShow && fpsVal != 0)
		{
			g.setTransform(scale, 0, 0, scale, offX, offY); g.font = $"800 22px {FONT}"; g.textAlign = "right"; g.textBaseline = "bottom"; g.direction = "ltr";
			g.lineWidth = 4; g.strokeStyle = "rgba(0,0,0,0.7)"; g.strokeText(str(fpsVal) + " FPS", W - 14, H - 10); g.fillStyle = "rgba(255,255,255,0.85)"; g.fillText(str(fpsVal) + " FPS", W - 14, H - 10); g.direction = "rtl";
		}
	}
}
