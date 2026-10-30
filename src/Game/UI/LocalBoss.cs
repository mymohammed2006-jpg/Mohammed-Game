using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Local play, the boss mode (UI/Local.cs): every square on this PC in the same run, the same rules as online (UI/BossNet.cs):
// 3 hearts, down = stays where it fell (the boss still throws at it) until another square touches it (back with one heart), everyone
// down = from the last save (50%) for all, the mark goes round the standing ones, one circle for each (only your own keeps you safe).
// Each one's letter: S no hit, A hit but never down, B down once, C more. The pause stops the song for everyone.
public static partial class G
{
	static readonly string[] LC_PAUSE_B = { "أكمل اللعب", "إعادة من آخر حفظ", "إعادة من البداية", "الإعدادات", "اطلع لشاشة اللاعبين" };
	static void lcBossBegin()
	{
		var L = lc.BL; cur = L; fromList = "bossMenu"; levelTiming(L); requestStart(0); introOn = true; rec = null;
	}
	// beginRun's end (Play.cs): each seat's square, side by side where the one square starts alone
	public static void lcBossRun(double cp)
	{
		rec = null; int n = lc.seats.Count;
		foreach (var s in lc.seats)
		{
			s.p = new Player { x = 200, y = H / 2 + (s.i - (n - 1) / 2.0) * 70, r = 5, hp = maxHp(cur), inv = 1.0, hurt = 0, dash = 0, cd = 0, fx = 1, fy = 0, ddx = 1, ddy = 0, ang = 0 };
			s.down = false; s.dashQ = false; s.inX = s.inY = 0; s.revAt = -1e9;
		}
		player = lc.seats[0].p; lc.wipeAt = -1;
	}
	static void lcBossRestart(double cp)
	{
		stopMusic();
		if (cp > 0) { foreach (var s in lc.seats) if (!s.down) s.deaths++; }   // back to the last save = a death (like alone)
		else foreach (var s in lc.seats) { s.hits = s.deaths = 0; }
		requestStart(cp); rec = null;
	}
	static bool lcBossFrame(Inp inp, bool anyPz, double now, double dt)
	{
		if (state == "play")
		{
			if (anyPz) { state = "paused"; pauseSel = 0; actx.suspend(); return true; }
			lcBossUpdate(); return true;
		}
		if (state == "paused")
		{
			int c = lcMenu(inp, LC_PAUSE_B);
			if (c == 0) { state = "play"; actx.resume(); }
			else if (c == 1) { actx.resume(); lcBossRestart(checkpoint); }
			else if (c == 2) { actx.resume(); lcBossRestart(0); }
			else if (c == 3) { state = "pauseSet"; setSel = 0; }
			else if (c == 4) lcLeave();
			return true;
		}
		if (state == "lcWipe")
		{
			if (now >= lc.wipeAt) { double cp = bnWipeCp; if (cp > 0) beginRun(cp); else { foreach (var s in lc.seats) s.hits = s.deaths = 0; beginRun(0); } }
			return true;
		}
		if (state == "lcBWin")
		{
			if (now - winAt > 900 && inp.confirm) { stopMusic(); lcGo(); }
			else if (inp.back) lcLeaveRate();
			return true;
		}
		return false;   // starting, the boss's scene, the settings: the usual code
	}
	static void lcBossUpdate()
	{
		double target = (actx.currentTime - songStart) * spd;
		foreach (var s in lc.seats) if (s.inp.dash) s.dashQ = true;
		if (target - (sim0 + simN * STEP) > SIM_MAX) simN = (long)Math.Floor((target - SIM_MAX - sim0) / STEP);
		int n = 0;
		while (state == "play" && sim0 + (simN + 1) * STEP <= target + 1e-9)
		{
			simN++; n++; bool now = sim0 + (simN + 1) * STEP > target + 1e-9;
			lcBossStep(sim0 + simN * STEP, STEP, now);
		}
		if (n > 0) foreach (var s in lc.seats)
			{
				s.inX = s.inp.x; s.inY = s.inp.y; var p = s.p;
				p.trail.Add(new TrailPt { x = p.x, y = p.y, d = p.dash > 0 }); if (p.trail.Count > 10) p.trail.RemoveAt(0);
			}
		if (lc.seats.Count > 0) player = lc.seats[0].p;
	}
	static Player lcStandingP() { foreach (var s in lc.seats) if (!s.down) return s.p; return lc.seats[0].p; }
	static void lcBossStep(double t, double dt, bool now)
	{
		songT = t; player = lcStandingP();
		fireEvents(t);
		foreach (var s in lc.seats)
		{
			var p = s.p; double mx = now ? s.inp.x : s.inX, my = now ? s.inp.y : s.inY;
			double m = hypot(mx, my); if (m > 1) { mx /= m; my /= m; }
			if (m > 0.2) { p.fx = mx / m; p.fy = my / m; }
			bool dq = now && s.dashQ; if (now) s.dashQ = false;
			if (s.down) { mx = my = 0; m = 0; dq = false; }
			if (dq && p.cd <= 0)
			{
				p.dash = 0.18; p.cd = 0.32; p.inv = Math.Max(p.inv, 0.28); p.ddx = p.fx; p.ddy = p.fy;
				p.dt0 = t; { double da = JS.atan2(p.ddy, p.ddx), q = Math.PI / 2; p.ang = da + round((p.ang - da) / q) * q; p.sa = da; }
				burst(p.x, p.y, lcCol(s.i), 8, 200); sfx(tt => sNoise(tt, 0.12, "bandpass", 3200, 0.18, 1));
			}
			double vx, vy;
			if (p.dash > 0) { vx = p.ddx * 950; vy = p.ddy * 950; } else { vx = mx * 370; vy = my * 370; }
			p.x = clamp(p.x + vx * dt, 10, W - 10); p.y = clamp(p.y + vy * dt, 10, H - 10);
			if (p.dash <= 0) { double sp = hypot(vx, vy); if (sp > 1) { double q = Math.PI / 2, ma = JS.atan2(vy, vx); double df = ma - p.ang; df -= round(df / q) * q; p.ang += df * Math.Min(1, dt * 16); } }
			{
				double sp = hypot(vx, vy), tg = p.dash > 0 ? 1 : Math.Min(1, sp / 370) * 0.3; if (sp > 1) p.sa = JS.atan2(vy, vx);
				double sk = p.sk ?? 0; p.sk = sk + (tg - sk) * Math.Min(1, dt * (tg > sk ? 30 : 12));
			}
			p.dash -= dt; p.cd -= dt; p.inv -= dt; p.hurt -= dt;
			skinTrailAdd(p);
		}
		player = lcStandingP();
		stepHazards(t, dt);
		foreach (var s in lc.seats)
		{
			if (s.down || s.p.inv > 0) continue;
			player = s.p; lcCur = s.i;
			foreach (var h in hazards) if (!h.dead && h.hit(s.p, t)) { lcBossDamage(s); break; }
		}
		lcCur = -1; player = lc.seats[0].p;
		hazards = hazards.Where(h => !h.dead).ToList();
		// touching a downed square brings it back with one heart
		double wall = performance.now();
		foreach (var r in lc.seats)
		{
			if (r.down || wall - r.revAt < 500) continue;
			foreach (var d in lc.seats)
			{
				if (!d.down || hypot(r.p.x - d.dx, r.p.y - d.dy) >= 26) continue;
				r.revAt = wall; d.down = false; d.p.x = d.dx; d.p.y = d.dy; d.p.hp = 1; d.p.inv = 1.5; d.p.hurt = 0; d.p.dash = 0;
				burst(d.dx, d.dy, lcCol(d.i), 30, 380); sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.2, "triangle", 0.1); });
				lcNote($"{r.name} رجّع {d.name}! عنده قلب واحد"); break;
			}
		}
		double beat = BT(t);
		foreach (var c in cur.cps)
			if (beat >= c && c > checkpoint) { checkpoint = c; cpMsg = 1.8; sfx(tt => { sTone(tt, 659, 0.25, "square", 0.06); sTone(tt + 0.08, 880, 0.3, "square", 0.06); }); }
		if (lc.seats.All(s => s.down) && state == "play")
		{
			bnWipeCp = checkpoint; lc.wipeAt = wall + 2600; stopMusic(); state = "lcWipe"; deadAt = wall; return;
		}
		if (t >= B(cur.end) + 0.4 && state == "play") lcBossWin();
	}
	static void lcBossDamage(LcSeat s)
	{
		var p = s.p; p.hp--; s.hits++; p.inv = 2.0; p.hurt = 2.0; shake = 16; flash = 0.3;
		burst(p.x, p.y, lcCol(s.i), 22, 380); rumbleOnly = s.dev; rumble(220, 0.8, 0.6); rumbleOnly = -2;
		sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); });
		if (p.hp > 0) return;
		s.down = true; s.dx = p.x; s.dy = p.y; p.dash = 0; s.deaths++;
		burst(p.x, p.y, lcCol(s.i), 40, 420); rumbleOnly = s.dev; rumble(400, 1, 0.8); rumbleOnly = -2;
		bnDownAdded(s.i);
		if (lc.seats.Any(q => !q.down)) lcNote($"{s.name} طاح! المسه عشان يرجع");
	}
	static void lcBossWin()
	{
		lc.won = true; state = "lcBWin"; winAt = performance.now(); lc.endAt = winAt;
		foreach (var s in lc.seats)
		{
			string r = (s.hits == 0 && s.deaths == 0) ? "S" : s.deaths == 0 ? "A" : s.deaths == 1 ? "B" : "C";
			lcFinish(s, cur, r);
			if (!s.down) burst(s.p.x, s.p.y, lcCol(s.i), 40, 450);
		}
		lastEz = false;
	}

	// ---------- drawing ----------
	public static void lcBossDraw(double now)
	{
		var sk = lcSkins();
		foreach (var s in lc.seats)
		{
			if (s.down) { bnDownSquare(s.dx, s.dy, s.sk ?? "", s.i, now); txt(s.name, s.dx, s.dy - 34, 18, lcCol(s.i), 800); continue; }
			if (s.p == null) continue;
			tinted(dupHue(sk, s.i), () => drawPlayer(s.p, s.sk ?? "", 1));
			if (lc.seats.Count > 1) { g.globalAlpha = 0.9; txt(s.name, s.p.x, s.p.y - 28, 16, lcCol(s.i), 800); g.globalAlpha = 1; }
		}
	}
	// over the HUD: the others' hearts at the top right (player 1's are the usual ones at the top left), the line in the middle
	public static void lcBossHUD(double now)
	{
		int i = 0;
		foreach (var s in lc.seats)
		{
			if (s.i == 0) continue; double y = 30 + i * 30, x = W - 30; i++;
			int hp = s.down ? 0 : (int)Math.Max(0, s.p?.hp ?? 0); string c = lcCol(s.i);
			for (int k = 0; k < maxHp(cur); k++) { double hx = x - k * 20; if (k < hp) { g.fillStyle = c; g.fillRect(hx - 7, y - 7, 14, 14); } else { g.strokeStyle = c; g.globalAlpha = 0.35; g.lineWidth = 2; g.strokeRect(hx - 6, y - 6, 12, 12); g.globalAlpha = 1; } }
			txt(s.down ? s.name + " (طايح)" : s.name, x - maxHp(cur) * 20 - 8, y + 1, 18, s.down ? "rgba(255,255,255,0.5)" : "#fff", 700, "right");
		}
		if (lc.seats.Count > 1 && lc.seats[0].down) txt(lc.seats[0].name + " (طايح)", 34, 62, 16, "rgba(255,255,255,0.5)", 700, "left");
		if (lcMsgT > 0 && lcMsg != null && state == "play") { g.globalAlpha = clamp(lcMsgT / 0.4, 0, 1); txt(lcMsg, W / 2, 120, 30, "#fff", 800); g.globalAlpha = 1; }
	}
	// the end: every one's card with their letter and what was saved
	public static void drawLcBWin(double now)
	{
		overlay(0.72);
		double e0 = clamp((now - winAt) / 220, 0, 1), sc0 = 1 + 0.6 * JS.pow(1 - e0, 3);
		g.save(); g.translate(W / 2, 120); g.scale(sc0, sc0); g.globalAlpha = e0; txt(lc.seats.Count > 1 ? "خلّصتوا المرحلة!" : "خلّصت المرحلة!", 0, 0, 64, "#fff"); g.restore(); g.globalAlpha = 1;
		lcCards(now, 220, s => s.rank, s => s.deaths == 0 && s.hits == 0 ? "بدون ولا ضربة" : $"انضرب {ar(s.hits)}  •  طاح {ar(s.deaths)}");
		if (now - winAt > 900) row(new[] { (BTN[kind].confirm, "العب مرة ثانية"), (BTN[kind].back, "شاشة اللاعبين") }, W / 2, H - 40, 34, "#fff", 26);
	}
	// a card for each seat: name, the big letter (or the place), a line, what was saved
	static void lcCards(double now, double top, Func<LcSeat, string> big, Func<LcSeat, string> line, Func<LcSeat, string> bigCol = null)
	{
		int n = lc.seats.Count; double cw = n > 3 ? 270 : 300, gap = 22, ch = 330, tot = n * cw + (n - 1) * gap;
		for (int k = 0; k < n; k++)
		{
			var s = lc.seats[k]; double x = W / 2 + tot / 2 - cw / 2 - k * (cw + gap), a = clamp((now - winAt - 250 - k * 150) / 250, 0, 1); if (a <= 0) continue;
			string c = lcCol(s.i); g.globalAlpha = a;
			rr(x - cw / 2, top, cw, ch, 18); g.fillStyle = "rgba(12,10,34,0.9)"; g.fill(); g.strokeStyle = c; g.lineWidth = 3; g.stroke();
			g.save(); g.translate(x, top + 36); g.scale(1.4, 1.4); coopSquare(c, s.sk ?? "", 1, now, "#1b0f26"); g.restore();
			txt(s.name, x, top + 84, 28, "#fff", 800);
			string r = big(s);
			g.font = $"100px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
			if (r == "S") sShine("S", x, top + 168, 74, now, 24);
			else if (r != null) { string rc = bigCol?.Invoke(s) ?? c; g.fillStyle = rc; g.shadowColor = rc; g.shadowBlur = 22; g.fillText(r, x, top + 168); g.shadowBlur = 0; }
			g.direction = "rtl";
			txt(line(s), x, top + 246, 20, "#e9e6f5", 500);
			txt(lcSaveLine(s), x, top + 290, 19, s.guest ? "rgba(255,255,255,0.45)" : lcSaveCol(s), 700);
			g.globalAlpha = 1;
		}
	}
}
