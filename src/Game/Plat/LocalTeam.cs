using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Local play, the co-op levels (UI/Local.cs): every square of the team on this PC (Team.cs with tmMe = -1, like the tests), each moved by
// its own controller. One screen: "الصورة" shows its whole page as always (the clouds only one player sees: everyone sees them here, in
// that player's colour); "صوتك أقوى" has a close camera: here one camera follows both and goes out when they are far apart.
// The pause stops the song for everyone. Each one's letter by their own falls, like online.
public static partial class G
{
	static readonly string[] LC_PAUSE_T = { "أكمل اللعب", "إعادة من آخر حفظ (للكل)", "إعادة المرحلة كاملة (للكل)", "الإعدادات", "اطلع لشاشة اللاعبين" };
	static void lcTeamBegin()
	{
		tmMe = -1; tmView = lc.PL.team.pages.Any(p => p.zoom > 0) ? -2 : -1; duoCamReset(); lcCam = (double.NaN, 0, 0);
		lc.goAt = performance.now() + 3000; state = "tmIntro"; stopMenu(); ensureAudio(); particles = new List<Particle>(); tmStopMusic(); tm = null;
	}
	static bool lcTeamFrame(Inp inp, bool anyPz, double now, double dt)
	{
		if (state == "tmIntro")
		{
			if (now >= lc.goAt)
			{
				var L = lc.PL.team; tmStart(L, Math.Clamp(tnPage0, 0, L.pages.Count - 1));
				foreach (var s in lc.seats) if (s.i < tm.names.Length) { tm.names[s.i] = s.name; tm.skins[s.i] = s.sk ?? ""; }
				state = "tmPlay";
			}
			return true;
		}
		if (tm == null) return true;
		if (state == "tmPlay")
		{
			if (tm.won)
			{
				if (!lc.saved) lcTeamWon();
				if (inp.back) { lcLeaveRate(); return true; }
				if (inp.confirm && tm.t - tm.wonAt > 7.3) { tmStopMusic(); lcGo(); return true; }
			}
			else if (anyPz) { state = "tmPause"; pauseSel = 0; actx.suspend(); return true; }
			else foreach (var s in lc.seats) { if (s.inp.jump) s.jumpQ = true; if (s.inp.pdash) s.dashQ = true; s.jumpHeld = s.inp.jumpHeld; }
			lcTeamRun();
			if (tm != null && tm.won && !lc.saved) lcTeamWon();
			return true;
		}
		if (state == "tmPause")
		{
			int c = lcMenu(inp, LC_PAUSE_T);
			if (c == 0) { state = "tmPlay"; actx.resume(); }
			else if (c == 1)
			{
				state = "tmPlay"; actx.resume();
				if (!tm.won && tm.restartAt < 0 && tm.doneAt < 0) { foreach (var b in tm.b) if (!b.down) tm.falls[b.i]++; tm.restartAt = tm.t + 0.9; tm.hostRs = true; flash = 0.3; }
			}
			else if (c == 2) { actx.resume(); tmStopMusic(); lcGo(); }
			else if (c == 3) { state = "tmSet"; setSel = 0; }
			else if (c == 4) lcLeave();
			return true;
		}
		return false;   // the settings: the usual code
	}
	// the fixed steps up to the song's time (like TeamNet's tnRun), each square's buttons on the last step of this frame
	static void lcTeamRun()
	{
		int k = 0;
		while (tm != null && k < 2000)
		{
			double target = actx.currentTime - tm.startWhen, dt = TN_DT;
			if (target - tm.t > SIM_MAX) { tm.n += (long)Math.Floor((target - SIM_MAX - tm.t) / dt); tm.t = -0.12 + tm.n * dt; }
			if (-0.12 + (tm.n + 1) * dt > target + 1e-9) break;
			tm.n++; k++; bool nw = -0.12 + (tm.n + 1) * dt > target + 1e-9;
			var ins = new TIn[TM_N];
			if (!tm.won) foreach (var s in lc.seats) if (s.i < TM_N) ins[s.i] = new TIn { x = nw ? s.inp.x : s.inX, y = nw ? s.inp.y : 0, jump = nw && s.jumpQ, jumpHeld = s.jumpHeld, dash = nw && s.dashQ };
			if (nw) foreach (var s in lc.seats) s.jumpQ = s.dashQ = false;
			tm.t = -0.12 + (tm.n - 1) * dt;
			tmStep(dt, ins);
		}
		foreach (var s in lc.seats) s.inX = s.inp.x;
	}
	// the photo is whole / the microphone is on: each one's letter (their own falls) and the team's time, each to their own account
	static void lcTeamWon()
	{
		lc.saved = true; lc.won = true; winAt = performance.now();
		foreach (var s in lc.seats) { if (s.i >= tm.b.Length) continue; lcFinish(s, lc.PL.lv, tmRank(s.i), tm.playT); }
	}
	// here every name is drawn, so squares standing together would cover each other's names: each later one goes up a line
	public static double lcNameUp(TBody p) => !lcT ? 0 : 18 * tm.b.Count(q => q.i < p.i && !q.gate && Math.Abs(q.x - p.x) < 80 && Math.Abs(q.y - p.y) < 24);
	public static int lcTeamXp(int i) { var s = lcSeat(i); return s == null || s.guest ? 0 : Math.Max(0, s.xpGain); }

	// ---------- the camera for both (the radio level's pages are bigger than the screen) ----------
	static (double x, double y, double z) lcCam = (double.NaN, 0, 0); static double lcCamAt;
	public static (double x, double y, double z) lcDuoCam(double now)
	{
		double mw = tm.w * PT, mh = tm.h * PT, z0 = Math.Min(W / mw, H / mh);
		if (tm.P.zoom <= 0) return (mw / 2, mh / 2, z0);
		var bs = tm.b.Where(b => !b.gate).ToList(); if (bs.Count == 0) bs = tm.b.ToList();
		double x0 = bs.Min(b => b.x), x1 = bs.Max(b => b.x), y0 = bs.Min(b => b.y), y1 = bs.Max(b => b.y);
		double tx = (x0 + x1) / 2, ty = (y0 + y1) / 2 - 20;
		double zt = Math.Min(tm.P.zoom, Math.Min(W / (x1 - x0 + 360), H / (y1 - y0 + 300))); zt = Math.Max(z0, zt);
		double dt = Math.Clamp((now - lcCamAt) / 1000, 0, 0.1); lcCamAt = now;
		if (double.IsNaN(lcCam.x) || hypot(tx - lcCam.x, ty - lcCam.y) > 1600) lcCam = (tx, ty, zt);
		else { double k = 1 - Math.Exp(-dt * 6), kz = 1 - Math.Exp(-dt * 4); lcCam = (lcCam.x + (tx - lcCam.x) * k, lcCam.y + (ty - lcCam.y) * k, lcCam.z + (zt - lcCam.z) * kz); }
		double z = lcCam.z, vw = W / z, vh = H / z;
		double cx = mw <= vw ? mw / 2 : Math.Clamp(lcCam.x, vw / 2, mw - vw / 2), cy = mh <= vh ? mh / 2 : Math.Clamp(lcCam.y, vh / 2, mh - vh / 2);
		return (cx, cy, z);
	}

	// ---------- drawing (Plat/PlatNetDraw.cs drawTeamNet) ----------
	public static void drawLcTeamIntro(double now)
	{
		var L = lc.PL; txt(L.name, W / 2, 150, 64, "#fff", 800);
		g.font = $"26px {TFONT}"; g.fillStyle = "rgba(255,255,255,0.8)"; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(L.best != null ? L.song : "؟؟؟", W / 2, 215); g.direction = "rtl";
		int n = lc.seats.Count;
		for (int i = 0; i < n; i++)
		{
			var s = lc.seats[i]; double x = W / 2 + ((n - 1) / 2.0 - i) * 260, y = 380; string col = lcCol(i);
			g.save(); g.translate(x, y); g.scale(3, 3); coopSquare(col, s.sk ?? "", 1, now, "#1b0f26"); g.restore();
			txt(s.name, x, y + 70, 30, "#fff", 800);
		}
		double left = (lc.goAt - now) / 1000; if (left > 0) txt(ar(Math.Ceiling(left)), W / 2, 600, 70, GOLD, 800);
	}
}
