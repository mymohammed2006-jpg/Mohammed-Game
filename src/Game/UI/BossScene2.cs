using System;
using static MG.JS;

namespace MG;

// The second boss's short scene ("رقصة الموت", plan doc 10-28, his يالله 05:45): like the first one's - once each time the level is entered
// from its list, no skip, not after a down or a restart, written words at the bottom. Four shots of 2.6 s, then the title; its music is
// the song's own quiet start (songs/SONG_B2_SCENE.mp3, 0:00-0:12.6).
public static partial class G
{
	public const double BOSS_SCENE2 = 12.6;   // seconds
	static readonly string[] BS2_CAP =
	{
		"لا دقت الساعة ١٢... يصحى العازف",
		"أوتاره وقوسه... كلها تضر",
		"يختار واحد منكم يرقص معه... فابعدوا عنه",
		"اصمدوا لين الفجر",
	};
	// the boss list's picture: a clock with pink hands round the violinist's mask
	public static void B2Icon(double x, double y, double r, double bt)
	{
		double s = r / 46; g.save(); g.translate(x, y); g.scale(s, s);
		g.strokeStyle = "rgba(230,215,250,0.45)"; g.lineWidth = 3; g.beginPath(); g.arc(0, 0, 62, 0, TAU); g.stroke();
		for (int i = 0; i < 12; i++) { double q = i * TAU / 12; g.lineWidth = i % 3 == 0 ? 4 : 2; g.beginPath(); g.moveTo(JS.cos(q) * (i % 3 == 0 ? 50 : 54), JS.sin(q) * (i % 3 == 0 ? 50 : 54)); g.lineTo(JS.cos(q) * 60, JS.sin(q) * 60); g.stroke(); }
		double a = -Math.PI / 2 + Math.Floor(bt) * 4 * Math.PI / 46 + 4 * Math.PI / 46 * B2Ease(bt - Math.Floor(bt)), c = -Math.PI / 2 - 0.4;
		foreach (var (an, len, w) in new[] { (c, 34.0, 7.0), (a, 52.0, 4.5) }) { g.save(); g.rotate(an); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8; g.fillRect(0, -w / 2, len, w); g.shadowBlur = 0; g.restore(); }
		g.restore();
		g.save(); g.translate(x, y); g.scale(s * 0.55, s * 0.55); g.translate(0, 8); B2BossDraw(0, 0, 28, bt); g.restore();
	}
	public static void drawBossScene2(double now)
	{
		double s = (now - introAt) / 1000, bt = s * 128 / 60, pu = JS.exp(-(bt % 1) * 5);
		if (s < 0) s = 0; int shot = Math.Min(4, (int)(s / 2.6)); double u = s - shot * 2.6;   // u: seconds into the shot
		double sp = pulse; pulse = pu;
		// the room (as in the level)
		var gr = g.createRadialGradient(W / 2, H / 2, 60, W / 2, H / 2, 780); gr.addColorStop(0, "#160822"); gr.addColorStop(1, "#040108"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
		g.strokeStyle = $"rgba(170,110,220,{str(0.04 + 0.05 * pu)})"; g.lineWidth = 1; g.beginPath();
		for (double q = -H; q <= W + H; q += 120) { g.moveTo(q, 0); g.lineTo(q + H, H); g.moveTo(q, H); g.lineTo(q + H, 0); }
		g.stroke();
		double cx = W / 2, cy = H / 2 - 30;
		if (shot == 0)
		{
			// 1: the clock's face; the hands go to 12 on the last ticks; the violinist rises from the dark
			g.strokeStyle = "rgba(220,200,240,0.16)"; g.lineWidth = 3; g.beginPath(); g.arc(cx, cy, 250, 0, TAU); g.stroke();
			for (int i = 0; i < 12; i++) { double q = i * TAU / 12 - Math.PI / 2; g.strokeStyle = "rgba(230,215,250,0.28)"; g.lineWidth = i % 3 == 0 ? 7 : 3; g.beginPath(); g.moveTo(cx + JS.cos(q) * 222, cy + JS.sin(q) * 222); g.lineTo(cx + JS.cos(q) * 246, cy + JS.sin(q) * 246); g.stroke(); }
			double tk = Math.Min(4, Math.Floor(u / 0.5)), m = -Math.PI / 2 - (4 - tk) * 0.35 + 0.35 * B2Ease((u - tk * 0.5) / 0.5 * 0.5) * (tk < 4 ? 1 : 0);
			double hr = -Math.PI / 2 - 0.25 * (1 - clamp(u / 2, 0, 1));
			foreach (var (an, len, w) in new[] { (hr, 140.0, 16.0), (m, 210.0, 10.0) }) { g.save(); g.translate(cx, cy); g.rotate(an); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12; g.fillRect(30, -w / 2, len - 30, w); g.shadowBlur = 0; g.restore(); }
			double k = bsE((u - 0.9) / 1.4); if (k > 0) B2BossDraw(cx, cy, 28 * k, bt);
			if (u > 2.0 && u < 2.4) { g.fillStyle = $"rgba(255,220,235,{str((2.4 - u) / 0.4 * 0.25)})"; g.fillRect(0, 0, W, H); }
		}
		else if (shot == 1)
		{
			// 2: the four strings, one plucked after another; then the bow sweeps with its one gap and the squares go through it
			double f = 0.35 + 0.05 * u, ux = JS.cos(f), uy = JS.sin(f);
			for (int i = 0; i < 4; i++)
			{
				double d = (i - 1.5) * 150, sx = cx - uy * d, sy = cy + ux * d, pk = u * 2.6 - i * 0.55, am = pk > 0 && pk < 0.6 ? 46 * (1 - pk / 0.6) : 0;
				if (am > 0) { g.strokeStyle = PINK; g.lineWidth = 4; g.shadowColor = PINK; g.shadowBlur = 12; g.beginPath(); for (double q = -900; q <= 900; q += 18) { double o = am * JS.sin(q * 0.035 + s * 40), x = sx + ux * q - uy * o, y = sy + uy * q + ux * o; if (q == -900) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke(); g.shadowBlur = 0; }
				else { g.strokeStyle = L15G(0.3); g.lineWidth = 2; g.beginPath(); g.moveTo(sx - ux * 900, sy - uy * 900); g.lineTo(sx + ux * 900, sy + uy * 900); g.stroke(); }
			}
			B2BossDraw(cx, cy, 28, bt);
			double bk = clamp((u - 1.2) / 1.1, 0, 1), bx = -30 + (W + 60) * bk, gy = 420;
			if (bk > 0 && bk < 1) { g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 16; g.fillRect(bx - 15, -10, 30, gy - 75 + 10); g.fillRect(bx - 15, gy + 75, 30, H - gy - 65); g.shadowBlur = 0; }
			for (int i = 0; i < 4; i++) { double sx2 = 600 + i * 70, sy2 = gy - 45 + i * 30 + 60 * (1 - bsE(u / 1.2)) * (i % 2 == 0 ? -1 : 1); bsSquare(sx2, sy2, BS_COL[i]); }
		}
		else if (shot == 2)
		{
			// 3: the white sight on one square; notes fly at where it stood; the others keep away
			B2BossDraw(cx, cy, 28, bt);
			double mx = 330 + 520 * bsE(u / 2.4), my = 520 - 60 * JS.sin(u * 1.4);
			bsSquare(mx, my, BS_COL[0]);
			(double x, double y)[] far = { (1080, 140), (1120, 560), (180, 130) };
			for (int i = 0; i < 3; i++) bsSquare(far[i].x - 40 * u, far[i].y + (i == 1 ? -20 * u : 15 * u), BS_COL[i + 1]);
			g.save(); g.translate(mx, my); g.rotate(s * 2.2); g.strokeStyle = "rgba(255,255,255,0.9)"; g.lineWidth = 2.5; g.beginPath();
			for (int i = 0; i < 4; i++) { double q = i * Math.PI / 2, r = 24 + 3 * pu; g.moveTo(JS.cos(q - 0.35) * r, JS.sin(q - 0.35) * r); g.arc(0, 0, r, q - 0.35, q + 0.35); }
			g.stroke(); g.restore();
			foreach (double t0 in new[] { 0.2, 1.0, 1.8 })
			{
				double ax = 330 + 520 * bsE(t0 / 2.4), ay = 520 - 60 * JS.sin(t0 * 1.4), a = JS.atan2(ay - cy, ax - cx);
				for (int j = -1; j <= 1; j++)
				{
					double aa = a + j * 0.14;
					if (u >= t0 && u < t0 + 0.45) L15Dash(cx, cy, cx + JS.cos(aa) * 320, cy + JS.sin(aa) * 320, L15G(0.3 + 0.4 * (u - t0) / 0.45), 2);
					else if (u >= t0 + 0.45) { double d = (u - t0 - 0.45) * 700; if (d < 700) B2NoteShape(cx + JS.cos(aa) * d, cy + JS.sin(aa) * d, 1); }
				}
			}
		}
		else if (shot == 3)
		{
			// 4: the rings of dancers turn; the squares go through the gaps; the dawn's light comes up
			double k = clamp(u / 2.6, 0, 1);
			var dg = g.createLinearGradient(0, H, 0, H - 420 * k - 40); dg.addColorStop(0, $"rgba(255,214,150,{str(0.4 * k)})"); dg.addColorStop(1, "rgba(255,214,150,0)"); g.fillStyle = dg; g.fillRect(0, 0, W, H);
			double[] RR = { 120, 200, 280 };
			for (int r = 0; r < 3; r++)
			{
				double g0 = (r % 2 == 0 ? 1 : -1) * s * (0.6 - r * 0.12) + r, gh = 55 / RR[r]; int n = (int)(TAU * RR[r] / 36);
				for (int i = 0; i < n; i++) { double a = g0 + i * TAU / n; if (Math.Abs(B1Ang(a - g0)) < gh || Math.Abs(B1Ang(a - g0 - Math.PI)) < gh) continue; B2MaskSmall(cx + JS.cos(a) * RR[r], cy + JS.sin(a) * RR[r], 12, a + Math.PI / 2, 1); }
			}
			B2BossDraw(cx, cy, 28, bt);
			for (int i = 0; i < 4; i++) { double a = i * Math.PI / 2 + 0.5 + s * 0.3, d = 240 + 100 * bsE(u / 2.2); bsSquare(cx + JS.cos(a) * d * 1.3, cy + JS.sin(a) * d * 0.9, BS_COL[i]); }
		}
		else
		{
			// 5: the title
			double k = bsE(u / 0.8); B2BossDraw(cx, cy - 40, 28 + 14 * k, bt);
			g.globalAlpha = k; g.save(); g.shadowColor = PINK; g.shadowBlur = 24 + 12 * pu; txt("رقصة الموت", W / 2, H / 2 + 120, 78, "#fff", 800); g.restore(); g.shadowBlur = 0;
			g.font = $"24px {TFONT}"; g.fillStyle = PINK; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText("Shirobon - La Danse Macabre", W / 2, H / 2 + 190); g.globalAlpha = 1;
		}
		// the words at the bottom
		if (shot < 4)
		{
			double a = clamp(Math.Min(u / 0.35, (2.6 - u) / 0.3), 0, 1);
			g.globalAlpha = a; g.fillStyle = "rgba(0,0,0,0.55)"; g.fillRect(0, H - 92, W, 64); txt(BS2_CAP[shot], W / 2, H - 60, 32, "#fff", 700); g.globalAlpha = 1;
		}
		// a quick dark cut between the shots; the end fades out
		double cut = shot < 4 ? Math.Max(clamp(1 - u / 0.18, 0, 1), clamp((u - 2.42) / 0.18, 0, 1)) : clamp((s - (BOSS_SCENE2 - 0.7)) / 0.7, 0, 1);
		if (s < 0.3) cut = Math.Max(cut, 1 - s / 0.3);
		if (cut > 0) { g.fillStyle = $"rgba(0,0,0,{str(cut)})"; g.fillRect(0, 0, W, H); }
		pulse = sp;
	}
}
