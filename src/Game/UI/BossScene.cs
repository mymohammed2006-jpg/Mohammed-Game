using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// The boss's short scene (Mohammed 04:54-04:57 10-25): once each time the level is entered from its list, before the play; no skip,
// not after a down or a restart (the "levelIntro" state, which those don't reach). Written words at the bottom (unlike the story mode).
// Six shots, 2.8 s each, then the title; its music is the song's own quiet end (songs/SONG_B1_SCENE.mp3).
public static partial class G
{
	public const double BOSS_SCENE = 16.5;   // seconds
	static readonly string[] BS_CAP =
	{
		"كل مرحلة لعبتوها... كان أحد يراقبكم",
		"كل شي وردي ضركم... كان قطعة منه",
		"يختار واحد منكم... ويلاحقه، فابعدوا عنه",
		"لا سكتت الموسيقى... كل واحد يدخل دائرة",
		"واللي يطيح... هو أول واحد يبيه، لا تخلونه",
	};
	static readonly string[] BS_COL = { CYAN, "#ffcf5a", "#5dff9a", "#b07cff" };   // the players' squares in the scene (never pink)
	// each boss has its own scene (B2: drawBossScene2 in UI/BossScene2.cs) and its own music (songs/SONG_<id>_SCENE.mp3)
	public static double bossSceneLen => cur?.id == "B2" ? BOSS_SCENE2 : BOSS_SCENE;
	static readonly Dictionary<string, Godot.AudioStream> bsBufs = new();
	public static void bossSceneMusic()
	{
		string id = cur?.id ?? "B1"; if (noMus) return; if (!bsBufs.TryGetValue(id, out var bsBuf)) bsBufs[id] = bsBuf = LoadSong("SONG_" + id + "_SCENE"); if (bsBuf == null) return;
		stopMusic(); var v = new Voice { song = bsBuf }; v.gain.setValueAtTime(0.0001, actx.currentTime); v.gain.exponentialRampToValueAtTime(0.9, actx.currentTime + 0.5);
		v.start(actx.currentTime + 0.05, 0); src = AudioCtx.Add(v);
	}
	static double bsE(double k) { k = clamp(k, 0, 1); return 1 - (1 - k) * (1 - k) * (1 - k); }
	static void bsSquare(double x, double y, string col, double a = 1)
	{
		g.globalAlpha = a; g.shadowColor = col; g.shadowBlur = 14; g.fillStyle = col; g.fillRect(x - 11, y - 11, 22, 22); g.shadowBlur = 0;
		g.fillStyle = "#1b0f26"; g.fillRect(x - 5, y - 5, 10, 10); g.globalAlpha = 1;
	}
	public static void drawBossScene(double now)
	{
		if (cur?.id == "B2") { drawBossScene2(now); return; }
		double s = (now - introAt) / 1000, bt = s * 130 / 60, pu = JS.exp(-(bt % 1) * 5);
		if (s < 0) s = 0; int shot = Math.Min(5, (int)(s / 2.8)); double u = s - shot * 2.8;   // u: seconds into the shot
		double sp = pulse; pulse = pu;
		// the room (as in the level)
		var gr = g.createRadialGradient(W / 2, H / 2, 60, W / 2, H / 2, 780); gr.addColorStop(0, "#1a0718"); gr.addColorStop(1, "#05020a"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
		g.strokeStyle = $"rgba(150,90,210,{str(0.05 + 0.05 * pu)})"; g.lineWidth = 1; g.beginPath();
		for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
		for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
		g.stroke();
		double cx = W / 2, cy = H / 2 - 30;
		if (shot <= 1)
		{
			// 1: the pink things of the old levels, all over, still; a small light in the middle watches. 2: they fly to it and make the core
			double k = shot == 0 ? 0 : bsE((u - 0.2) / 1.9);
			for (int i = 0; i < 18; i++)
			{
				double a = i * 2.39996 + 0.4, d = 260 + (i * 97 % 230), x0 = cx + JS.cos(a) * d * 1.45, y0 = cy + JS.sin(a) * d * 0.78;
				x0 += JS.sin(s * 0.7 + i) * 8; y0 += JS.cos(s * 0.6 + i * 1.3) * 6;
				double x = x0 + (cx - x0) * k, y = y0 + (cy - y0) * k; if (k >= 0.999) continue;
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; B1PieceShape(x, y, 16 * (1 - 0.6 * k), a + s * 0.4 * (1 + 3 * k), i % 3); g.shadowBlur = 0;
			}
			if (shot == 0) { double o = clamp((u - 0.6) / 0.8, 0, 1); g.fillStyle = $"rgba(255,255,255,{str(0.25 + 0.6 * o)})"; circle(cx, cy, 3 + 4 * o * (1 + 0.3 * pu)); }
			else { double r = 6 + 54 * bsE((u - 1.4) / 0.9); B1CoreDraw(cx, cy, r, bt, 1); if (u > 2.1 && u < 2.5) { g.fillStyle = $"rgba(255,220,235,{str((2.5 - u) / 0.4 * 0.3)})"; g.fillRect(0, 0, W, H); } }
		}
		else if (shot == 2)
		{
			// 3: the mark - the white sight on one square, lasers on where it stood; the others keep away from it
			B1CoreDraw(cx, cy, 44, bt, 1);
			double mx = 330 + 520 * bsE(u / 2.6), my = 520 - 60 * JS.sin(u * 1.4);
			bsSquare(mx, my, BS_COL[0]);
			(double x, double y)[] far = { (1080, 140), (1120, 560), (180, 130) };
			for (int i = 0; i < 3; i++) bsSquare(far[i].x - 40 * u, far[i].y + (i == 1 ? -20 * u : 15 * u), BS_COL[i + 1]);
			g.save(); g.translate(mx, my); g.rotate(s * 2.2); g.strokeStyle = "rgba(255,255,255,0.9)"; g.lineWidth = 2.5; g.beginPath();
			for (int i = 0; i < 4; i++) { double q = i * Math.PI / 2, r = 24 + 3 * pu; g.moveTo(JS.cos(q - 0.35) * r, JS.sin(q - 0.35) * r); g.arc(0, 0, r, q - 0.35, q + 0.35); }
			g.stroke(); g.restore();
			foreach (var (t0, at) in new[] { (0.3, 0.35), (1.3, 1.35) })
			{
				double ax = 330 + 520 * bsE(at / 2.6), ay = 520 - 60 * JS.sin(at * 1.4), a = JS.atan2(ay - cy, ax - cx), T = t0 + 0.7;
				if (u >= t0 && u < T) L15Dash(cx, cy, cx + JS.cos(a) * 1500, cy + JS.sin(a) * 1500, L15G(0.3 + 0.5 * (u - t0) / 0.7), 3);
				else if (u >= T && u < T + 0.4) { double q = 1 - (u - T) / 0.4; g.strokeStyle = L15P(q); g.lineWidth = 24; g.shadowColor = PINK; g.shadowBlur = 20; g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + JS.cos(a) * 1500, cy + JS.sin(a) * 1500); g.stroke(); g.shadowBlur = 0; }
			}
		}
		else if (shot == 3)
		{
			// 4: the music stops - everything freezes in the air; a circle for each one; outside them it turns pink
			B1CoreDraw(cx, cy, 44, 0, 0);
			for (int i = 0; i < 10; i++) { double a = i * TAU / 10 + 0.2, d = 150 + 40 * (i % 3); g.save(); g.translate(cx + JS.cos(a) * d, cy + JS.sin(a) * d); g.rotate(a); g.fillStyle = PINK; g.beginPath(); g.moveTo(14, 0); g.lineTo(0, 7); g.lineTo(-11, 0); g.lineTo(0, -7); g.closePath(); g.fill(); g.restore(); }
			g.fillStyle = "rgba(120,170,255,0.08)"; g.fillRect(0, 0, W, H);
			(double x, double y)[] C = { (250, 470), (1030, 160), (220, 150), (1040, 500) }; (double x, double y)[] P0 = { (420, 380), (860, 260), (420, 230), (860, 460) };
			double k = bsE(u / 1.7), hitT = 2.0;
			for (int i = 0; i < 4; i++)
			{
				double a = clamp(u / 0.3, 0, 1); g.fillStyle = $"rgba(255,255,255,{str(0.1 * a)})"; circle(C[i].x, C[i].y, 60);
				g.strokeStyle = $"rgba(255,255,255,{str(0.9 * a)})"; g.lineWidth = 3; g.beginPath(); g.arc(C[i].x, C[i].y, 60, 0, TAU); g.stroke();
				g.lineWidth = 7; g.beginPath(); g.arc(C[i].x, C[i].y, 69, -Math.PI / 2, -Math.PI / 2 + TAU * clamp(u / hitT, 0, 1)); g.stroke();
			}
			if (u >= hitT && u < hitT + 0.6)
			{
				double q = 1 - (u - hitT) / 0.6; g.save(); g.beginPath(); g.rect(0, 0, W, H); foreach (var c in C) { g.moveTo(c.x + 60, c.y); g.arc(c.x, c.y, 60, 0, TAU, true); }
				g.fillStyle = L15P(0.45 * q); g.fill("evenodd"); g.restore();
			}
			for (int i = 0; i < 4; i++) bsSquare(P0[i].x + (C[i].x - P0[i].x) * k, P0[i].y + (C[i].y - P0[i].y) * k, BS_COL[i]);
		}
		else if (shot == 4)
		{
			// 5: one falls (grey, in a ring with a gap that turns); the core goes for it first; a touch from another brings it back
			B1CoreDraw(cx, cy, 44, bt, 1);
			double dx = 900, dy = 470; bool back = u >= 1.9;
			if (!back)
			{
				bsSquare(dx, dy, "#8a8a99", 0.8); g.strokeStyle = "rgba(255,255,255,0.75)"; g.lineWidth = 3; g.beginPath(); g.arc(dx, dy, 30, s * 3 + 0.5, s * 3 + TAU - 0.5); g.stroke();
				double a = JS.atan2(dy - cy, dx - cx);
				for (int j = -1; j <= 1; j++) { double aa = a + j * 0.12; if (u < 0.9) L15Dash(cx, cy, cx + JS.cos(aa) * 600, cy + JS.sin(aa) * 600, L15G(0.25 + 0.4 * u / 0.9), 2); else { double d = 50 + (u - 0.9) * 700; if (d < 560) { g.save(); g.translate(cx + JS.cos(aa) * d, cy + JS.sin(aa) * d); g.rotate(aa); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.moveTo(14, 0); g.lineTo(0, 7); g.lineTo(-11, 0); g.lineTo(0, -7); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore(); } } }
			}
			else { bsSquare(dx, dy, BS_COL[1]); if (u < 2.4) { g.strokeStyle = $"rgba(255,207,90,{str((2.4 - u) / 0.5)})"; g.lineWidth = 4; g.beginPath(); g.arc(dx, dy, 20 + (u - 1.9) * 160, 0, TAU); g.stroke(); } }
			double hk = bsE((u - 0.3) / 1.6); bsSquare(380 + (dx - 26 - 380) * hk, 220 + (dy - 380 + 160) * hk * 0 + (dy - 220) * hk, BS_COL[0]);
			bsSquare(240, 560, BS_COL[2]); bsSquare(1120, 150, BS_COL[3]);
		}
		else
		{
			// 6: the title
			double k = bsE(u / 0.8); B1CoreDraw(cx, cy - 40, 44 + 30 * k, bt, 1);
			g.globalAlpha = k; g.save(); g.shadowColor = PINK; g.shadowBlur = 24 + 12 * pu; txt("الزعيم الأخير", W / 2, H / 2 + 120, 78, "#fff", 800); g.restore(); g.shadowBlur = 0;
			g.font = $"24px {TFONT}"; g.fillStyle = PINK; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText("Nitro Fun - Final Boss", W / 2, H / 2 + 190); g.globalAlpha = 1;
		}
		// the words at the bottom
		if (shot < 5)
		{
			double a = clamp(Math.Min(u / 0.35, (2.8 - u) / 0.3), 0, 1);
			g.globalAlpha = a; g.fillStyle = "rgba(0,0,0,0.55)"; g.fillRect(0, H - 92, W, 64); txt(BS_CAP[shot], W / 2, H - 60, 32, "#fff", 700); g.globalAlpha = 1;
		}
		// a quick dark cut between the shots; the end fades out
		double cut = shot < 5 ? Math.Max(clamp(1 - u / 0.18, 0, 1), clamp((u - 2.62) / 0.18, 0, 1)) : clamp((s - (BOSS_SCENE - 0.7)) / 0.7, 0, 1);
		if (s < 0.3) cut = Math.Max(cut, 1 - s / 0.3);
		if (cut > 0) { g.fillStyle = $"rgba(0,0,0,{str(cut)})"; g.fillRect(0, 0, W, H); }
		pulse = sp;
	}
}
