using System;
using System.Linq;
using static MG.JS;

namespace MG;

// drawing the platformer and its menus
public static partial class G
{
	static readonly string[] JUMP_KB = { "مسافة" }, JUMP_PS = { "x" }, JUMP_XB = { "A" };
	static readonly string[] PDASH_KB = { "Shift" }, PDASH_PS = { "sq", "R1" }, PDASH_XB = { "X", "RB" };
	static string[] jumpIc() => kind == "ps" ? JUMP_PS : kind == "xbox" ? JUMP_XB : JUMP_KB;
	static string[] pdashIc() => kind == "ps" ? PDASH_PS : kind == "xbox" ? PDASH_XB : PDASH_KB;

	public static bool drawPlatScreens(double now)
	{
		if (state == "modes") drawModes(now);
		else if (state == "pltList") drawPltList(now);
		else if (state == "pltIntro") drawPltIntro(now);
		else if (state is "pltPlay" or "pltPause" or "pltSet" or "pltDead" or "pltWin" or "pltVsEnd") drawPlat(now);
		else if (state == "pltOn") drawPltOn(now);
		else if (state == "pltJoin") drawPltJoin(now);
		else if (state == "pltRoom") drawPltRoom(now);
		else if (state is "tmIntro" or "tmPlay" or "tmPause" or "tmSet") drawTeamNet(now);
		else return false;
		return true;
	}

	// ---------- pick the mode ----------
	static void drawModes(double now)
	{
		bgShapes(now); txt("المراحل الرئيسية", W / 2, 105, 48, "#fff");
		double cw = 420, chh = 360, cy = H / 2 + 20;
		for (int i = 0; i < 2; i++)
		{
			bool sel = i == modeSel; double x = W / 2 + (i == 0 ? 1 : -1) * (cw / 2 + 20); int ii = i;
			zone(x - cw / 2, cy - chh / 2, cw, chh, () => modeSel = ii);
			double lf = lift("M" + i, sel);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 22); g.fillStyle = sel ? CARDS : CARD; g.fill();
			g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			g.save(); g.translate(x, cy - 40 + lf); if (i == 0) modeIconDodge(now, sel); else modeIconPlat(now, sel); g.restore();
			txt(i == 0 ? "الطور الرئيسي" : "البلاتفورم", x, cy + 95 + lf, sel ? 40 : 36, "#fff", 800);
		}
		row(new[] { (BTN[kind].confirm, "اختيار"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// the main mode: the square between pink bullets
	static void modeIconDodge(double now, bool sel)
	{
		double t = now / 1000;
		for (int i = 0; i < 5; i++) { double a = i * 1.26 + t * 0.6, r = 70 + 8 * JS.sin(t * 2 + i); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 10; g.beginPath(); g.arc(JS.cos(a) * r, JS.sin(a) * r * 0.6, 7, 0, TAU); g.fill(); }
		g.shadowBlur = 0; g.shadowColor = CYAN; g.shadowBlur = 14; g.fillStyle = CYAN; g.fillRect(-11, -11, 22, 22); g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(-4, -4, 8, 8);
	}
	// the platformer: the square jumping between two platforms, a gold piece on top
	static void modeIconPlat(double now, bool sel)
	{
		double t = now / 1000, ph = t * 1.4 % 2, k = ph < 1 ? ph : 2 - ph, jx = -60 + 120 * k, jy = 30 - 70 * 4 * k * (1 - k);
		g.fillStyle = "#1a1330"; g.strokeStyle = "rgba(170,130,255,0.8)"; g.lineWidth = 2;
		g.fillRect(-110, 40, 80, 16); g.strokeRect(-110, 40, 80, 16); g.fillRect(30, 40, 80, 16); g.strokeRect(30, 40, 80, 16);
		g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8; for (int i = 0; i < 4; i++) { double sx = -28 + i * 14; g.beginPath(); g.moveTo(sx, 58); g.lineTo(sx + 7, 44); g.lineTo(sx + 14, 58); g.closePath(); g.fill(); }
		g.shadowBlur = 0; plPieceIcon(0, -62 + 4 * JS.sin(t * 3), 1, 0, 1);
		g.shadowColor = CYAN; g.shadowBlur = 14; g.fillStyle = CYAN; g.fillRect(jx - 11, jy - 11, 22, 22); g.shadowBlur = 0; g.fillStyle = "#000"; g.fillRect(jx - 4, jy - 4, 8, 8);
	}

	// ---------- the platformer's levels ----------
	static void drawPltList(double now)
	{
		bgShapes(now); txt("البلاتفورم", W / 2, 105, 48, "#fff");
		var list = PLATS; int n = list.Count; double cw = 240, chh = 350, gp = 28, cy = H / 2 + 20, total = n * cw + (n - 1) * gp;
		for (int i = 0; i < n; i++)
		{
			var L = list[i]; bool sel = i == plSel; double x = W / 2 + total / 2 - cw / 2 - i * (cw + gp); int ii = i;
			zone(x - cw / 2, cy - chh / 2, cw, chh, () => plSel = ii);
			double lf = lift("P" + i, sel);
			rr(x - cw / 2, cy - chh / 2 + lf, cw, chh, 18); g.fillStyle = sel ? CARDS : CARD; g.fill();
			g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			txt(ar(i + 1), x, cy - 78 + lf, 90, sel ? RED : "#fff", 800);
			txt(L.name, x, cy + 5 + lf, 28, "#fff", 800);
			modeTag(L, x, cy - 145 + lf, sel);
			g.font = $"20px {TFONT}"; g.fillStyle = RED3; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(L.best != null ? L.song : "؟؟؟", x, cy + 52 + lf);   // the song stays hidden until the level is finished g.direction = "rtl";
			if (L.best != null) { g.font = $"46px {TFONT}"; g.textAlign = "center"; g.direction = "ltr"; if (L.best == "S") sShine("S", x, cy + 115 + lf, 37, now, 12); else { g.fillStyle = "#ff2436"; g.fillText(L.best, x, cy + 115 + lf); } g.direction = "rtl"; }   // S shines like the main list
		}
		row(new[] { (BTN[kind].confirm, "العب"), (BTN[kind].restart, "أونلاين"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
		drawNetNote(now);
	}

	// the card's tag: how the level is played, "ضد أو سولو" (pink) or "تعاوني: N لاعبين" (cyan)
	static void modeTag(PlatLevel L, double x, double y, bool sel)
	{
		string t = L.coop ? $"تعاوني: {ar(L.players)} لاعبين" : "ضد أو سولو", col = L.coop ? CYAN : PINK;
		g.font = $"800 20px {TFONT}"; double w = g.measureText(t).width + 36, h = 34;
		g.globalAlpha = sel ? 1 : 0.7;
		rr(x - w / 2, y - h / 2, w, h, h / 2); g.fillStyle = L.coop ? "rgba(45,226,230,0.13)" : "rgba(255,42,109,0.13)"; g.fill(); g.strokeStyle = col; g.lineWidth = 1.5; g.stroke();
		txt(t, x, y + 1, 20, col, 800);
		g.globalAlpha = 1;
	}

	// ---------- the level ----------
	static void drawPlat(double now)
	{
		double b = plBeat(), bp = ((b % 1) + 1) % 1; pulse = JS.exp(-bp * 5) * 0.8; energy = 0.6;
		plTheme(now, b);
		plPass();
		// background grid like the arena
		g.strokeStyle = $"rgba(140,100,230,{str(0.04 + 0.07 * pulse)})"; g.lineWidth = 1; g.beginPath();
		for (double x = 0; x <= W; x += 80) { g.moveTo(x, 0); g.lineTo(x, H); }
		for (double y = 0; y <= H; y += 80) { g.moveTo(0, y); g.lineTo(W, y); }
		g.stroke();
		// walls: dark inside, a purple neon edge where they meet open space
		g.fillStyle = "#0e0a1c";
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++) if (pl.m[y][x] == '#') g.fillRect(x * PT, y * PT, PT + 0.5, PT + 0.5);
		g.strokeStyle = $"rgba(170,130,255,{str(0.55 + 0.35 * pulse)})"; g.lineWidth = 2; g.shadowColor = "#9a6bff"; g.shadowBlur = 8; g.beginPath();
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++)
		{
			if (pl.m[y][x] != '#') continue; double X = x * PT, Y = y * PT;
			if (y > 0 && pl.m[y - 1][x] != '#') { g.moveTo(X, Y + 1); g.lineTo(X + PT, Y + 1); }
			if (y < pl.h - 1 && pl.m[y + 1][x] != '#') { g.moveTo(X, Y + PT - 1); g.lineTo(X + PT, Y + PT - 1); }
			if (x > 0 && pl.m[y][x - 1] != '#') { g.moveTo(X + 1, Y); g.lineTo(X + 1, Y + PT); }
			if (x < pl.w - 1 && pl.m[y][x + 1] != '#') { g.moveTo(X + PT - 1, Y); g.lineTo(X + PT - 1, Y + PT); }
		}
		g.stroke(); g.shadowBlur = 0;
		// spikes, beat blocks, pieces, checkpoints, the goal
		int total = plPieces();
		for (int y = 0; y < pl.h; y++) for (int x = 0; x < pl.w; x++)
		{
			char c = pl.m[y][x]; double X = x * PT, Y = y * PT;
			if (c == '^' || c == 'v')
			{
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8 + 8 * pulse; g.beginPath();
				for (int i = 0; i < 3; i++) { double sx = X + i * PT / 3, h = PT * (0.5 + 0.08 * pulse); if (c == '^') { g.moveTo(sx, Y + PT); g.lineTo(sx + PT / 6, Y + PT - h); g.lineTo(sx + PT / 3, Y + PT); } else { g.moveTo(sx, Y); g.lineTo(sx + PT / 6, Y + h); g.lineTo(sx + PT / 3, Y); } }
				g.fill(); g.shadowBlur = 0;
			}
			else if (c == 'B' || c == 'b')
			{
				double q = (((b + (c == 'b' ? 2 : 0)) % 4) + 4) % 4;   // 0-2 on, 3-4: the warning grows
				if (q < 2) { g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 14 * (0.6 + 0.4 * pulse); g.fillRect(X + 2, Y + 2, PT - 4, PT - 4); g.shadowBlur = 0; g.fillStyle = "rgba(255,255,255,0.25)"; g.fillRect(X + 8, Y + 8, PT - 16, PT - 16); }
				else { double w = q >= 3 ? (q - 3) : 0; g.strokeStyle = $"rgba(255,42,109,{str(0.3 + 0.4 * w)})"; g.lineWidth = 2; g.strokeRect(X + 3, Y + 3, PT - 6, PT - 6); g.fillStyle = $"rgba(255,42,109,{str(0.12 * w)})"; g.fillRect(X + 3, Y + 3, PT - 6, PT - 6); }
			}
			else if (c >= '1' && c <= '9' && !pl.got.Contains(x + "," + y))
			{
				int s = pl.L.pieces != null && pl.L.pieces.TryGetValue(c.ToString(), out int v) ? v : 0;
				plPieceIcon(X + PT / 2, Y + PT / 2 + 4 * JS.sin(b * Math.PI / 2), 1 + 0.15 * pulse, s, 1);
			}
			else if (c == 'C')
			{
				bool on = pl.cx == X + PT / 2 && pl.cy == Y + PT - P_HALF; string col = on ? CYAN : "rgba(255,255,255,0.35)";
				g.fillStyle = col; g.fillRect(X + PT / 2 - 2, Y + 2, 3, PT - 2);
				g.beginPath(); g.moveTo(X + PT / 2 + 1, Y + 3); g.lineTo(X + PT / 2 + 17, Y + 10); g.lineTo(X + PT / 2 + 1, Y + 17); g.closePath(); if (on) { g.shadowColor = CYAN; g.shadowBlur = 10; } g.fill(); g.shadowBlur = 0;
			}
			else if (c == 'G')
			{
				bool open = plOpen(); double cx = X + PT / 2, cy = Y + PT / 2 - 8;
				g.save(); g.translate(cx, cy);
				if (open)
				{
					g.rotate(now / 600); g.strokeStyle = CYAN; g.shadowColor = CYAN; g.shadowBlur = 16 + 10 * pulse; g.lineWidth = 4;
					g.beginPath(); g.arc(0, 0, 26 + 3 * pulse, 0, TAU * 0.8); g.stroke(); g.lineWidth = 2; g.beginPath(); g.arc(0, 0, 16, Math.PI, Math.PI + TAU * 0.7); g.stroke(); g.shadowBlur = 0;
				}
				else
				{
					g.strokeStyle = "rgba(255,255,255,0.3)"; g.lineWidth = 3; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(0, 0, 26, 0, TAU); g.stroke(); g.setLineDash(new double[0]);
					g.restore(); g.save(); g.translate(cx, cy);
					g.font = $"800 18px {FONT}"; g.fillStyle = "rgba(255,255,255,0.55)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "rtl"; g.fillText($"{ar(pl.got.Count)}/{ar(total)}", 0, 2);
				}
				g.restore();
			}
		}
		// saws
		foreach (var s in pl.L.saws)
		{
			double sx = sawPos(s, out double sy); g.save(); g.translate(sx, sy); g.rotate(now / 120);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12 + 8 * pulse; g.beginPath();
			int teeth = 10; for (int i = 0; i < teeth * 2; i++) { double a = i * Math.PI / teeth, rr2 = i % 2 == 0 ? s.r : s.r * 0.72; if (i == 0) g.moveTo(JS.cos(a) * rr2, JS.sin(a) * rr2); else g.lineTo(JS.cos(a) * rr2, JS.sin(a) * rr2); }
			g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "#000"; g.beginPath(); g.arc(0, 0, s.r * 0.25, 0, TAU); g.fill(); g.restore();
		}
		foreach (var q in particles) { g.globalAlpha = Math.Max(0, q.life / q.max); g.fillStyle = q.col; g.fillRect(q.x - q.s / 2, q.y - q.s / 2, q.s, q.s); }
		g.globalAlpha = 1;
		double ia = 1;   // the intro: the square drops in, then the HUD shows
		if (state == "pltIntro") { double s = (now - pl.introAt) / 1000; ia = clamp((s - 2.85) / 0.15, 0, 1); if (s >= 2.4) { double f = clamp((s - 2.4) / 0.45, 0, 1), dy = -260 * (1 - f * f); player.y += dy; drawPlayer(); player.y -= dy; } }
		else if (state != "pltDead" && pl.down) drawDown(player.x, player.y, now, false);
		else if (pl.atGate) { }   // inside the gate
		else if (state != "pltDead") drawPlayer();
		if (online && state != "pltIntro") drawNetGhost(now);
		g.save(); g.globalAlpha = ia;
		// HUD: lives, the pieces, messages
		for (int i = 0; i < P_HP; i++)
		{
			double x = 34 + i * 28, y0 = 26;
			if (player != null && i < player.hp) { g.fillStyle = CYAN; g.fillRect(x - 9, y0 - 9, 18, 18); }
			else { g.strokeStyle = "rgba(45,226,230,0.35)"; g.lineWidth = 2; g.strokeRect(x - 8, y0 - 8, 16, 16); }
		}
		{
			int ns = pl.L.stems.Length; double x0 = W / 2 + (ns - 1) * 22;
			for (int i = 0; i < ns; i++) plPieceIcon(x0 - i * 44, 24, 0.75, i, pl.stemOn[i] ? 1 : 0.25);
		}
		if (pl.msgT > 0) { g.globalAlpha = Math.Min(1, pl.msgT); txt(pl.msg, W / 2, 70, 30, GOLD); g.globalAlpha = 1; }
		if (cpMsg > 0) { g.globalAlpha = Math.Min(1, cpMsg); txt("✓ تم الحفظ", W / 2, pl.msgT > 0 ? 118 : 70, 24, CYAN); g.globalAlpha = 1; }
		g.restore();
		if (state == "pltPause")
		{
			overlay(0.75); txt("اللعبة متوقفة", W / 2, 150, 56, "#fff");
			var PP = online ? PL_PAUSE_ON : PL_PAUSE;
			for (int i = 0; i < PP.Length; i++)
			{
				double y = 270 + i * 80; bool sel = i == pauseSel; int ii = i; zone(W / 2 - 260, y - 34, 520, 68, () => pauseSel = ii);
				if (sel) { rr(W / 2 - 260, y - 34, 520, 68, 16); g.fillStyle = $"rgba({RGB},0.12)"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
				txt(PP[i], W / 2, y, sel ? 38 : 32, sel ? "#fff" : "rgba(255,255,255,0.5)", 800);
			}
		}
		if (state == "pltSet") { overlay(0.95); drawSettings(); }
		if (state == "pltDead")
		{
			overlay(); double e = clamp((now - pl.deadAt) / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3);
			g.save(); g.translate(W / 2, H / 2 - 80); g.scale(sc, sc); g.globalAlpha = e; txt("خسرت!", 0, 0, 90, PINK); g.restore(); g.globalAlpha = 1;
			if (now - pl.deadAt > 700 && online) { var K = BTN[kind]; row(new[] { (K.confirm, "كمّل من آخر حفظ") }, W / 2, H / 2 + 60, 38, "#fff", 30); row(new[] { (K.back, "اطلع من الغرفة") }, W / 2, H / 2 + 120); }
			else if (now - pl.deadAt > 700)
			{
				var K = BTN[kind];
				row(new[] { (K.confirm, "كمّل من آخر حفظ") }, W / 2, H / 2 + 60, 38, "#fff", 30);
				row(new[] { (K.restart, "من بداية المرحلة"), (K.back, "المراحل") }, W / 2, H / 2 + 120);
			}
		}
		if (state == "pltVsEnd") drawVsEnd(now);
		if (state == "pltWin")
		{
			overlay(0.7); txt("خلّصت المرحلة!", W / 2, H / 2 - 160, 64, "#fff");
			double ts = now - pl.winAt;
			if (ts > 0)
			{
				double e = clamp(ts / 180, 0, 1), sc = e < 1 ? 1 + 2.2 * JS.pow(1 - e, 2) : 1 + 0.18 * JS.exp(-(ts - 180) / 90);
				if (e >= 1 && !winHit) { winHit = true; shake = 14; flash = 0.18; burst(W / 2, H / 2 - 40, "#ff2436", 40, 520); rumble(160, 0.7, 0.5); }
				g.save(); g.translate(W / 2, H / 2 - 40); g.scale(sc, sc); g.globalAlpha = Math.Min(1, e * 1.5);
				g.font = $"150px {TFONT}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr";
				if (lastRank == "S") sShine("S", 0, 0, 110, now, 30); else { g.fillStyle = "#ff2436"; g.shadowColor = "#ff2436"; g.shadowBlur = 30; g.fillText(lastRank, 0, 0); g.shadowBlur = 0; }
				g.restore(); g.globalAlpha = 1;
			}
			// what this finish added: points on the left, XP on the right (like the main mode)
			if (lastGain > 0 && now > winSlam)
			{
				double ce = clamp((now - winSlam - 120) / 700, 0, 1), cv = round(lastGain * (1 - JS.pow(1 - ce, 3)));
				g.globalAlpha = clamp((now - winSlam) / 150, 0, 1); g.save(); g.translate(W / 2 - 170, H / 2 - 40); g.rotate(now / 420); g.fillStyle = "#ff2436"; g.shadowColor = "#ff2436"; g.shadowBlur = 10; g.fillRect(-7, -7, 14, 14); g.restore();
				txt($"+{ar(cv)} نقطة", W / 2 - 190, H / 2 - 40, 34, "#ff2436", 800, "right"); g.globalAlpha = 1;
			}
			drawWinXp(now);
			txt(deaths == 0 ? "بدون ولا موتة" : $"متّ {ar(deaths)} مرة", W / 2, H / 2 + 70, 28, "#e9e6f5", 500);
			if (ts > 800) { row(new[] { (BTN[kind].confirm, "العب مرة ثانية") }, W / 2, H / 2 + 135); row(new[] { (BTN[kind].back, "المراحل") }, W / 2, H / 2 + 190); }
		}
	}

	// the intro: the level's name and song (؟؟؟ until finished) on black, then the map draws in from the bottom up, then the square drops in
	static void drawPltIntro(double now)
	{
		double s = (now - pl.introAt) / 1000;
		if (s >= 1.3)
		{
			double r = clamp((s - 1.3) / 1.1, 0, 1); r = 1 - JS.pow(1 - r, 3); double ly = H - (H + 20) * r;
			g.save(); g.beginPath(); g.rect(0, ly, W, H - ly + 20); g.clip(); drawPlat(now); g.restore();
			if (r < 1) { g.fillStyle = "rgba(170,130,255,0.9)"; g.shadowColor = "#9a6bff"; g.shadowBlur = 18; g.fillRect(0, ly - 1.5, W, 3); g.shadowBlur = 0; }
		}
		double a = s < 0.4 ? s / 0.4 : s < 1.2 ? 1 : clamp(1 - (s - 1.2) / 0.4, 0, 1), sl = 1 - clamp(s / 0.5, 0, 1), up = s > 1.2 ? (s - 1.2) * 60 : 0;
		if (a <= 0) return;
		g.save(); g.globalAlpha = a;
		txt(pl.L.name, W / 2, H / 2 - 30 + 30 * sl * sl - up, 72, "#fff", 800);
		string song = pl.L.best != null ? pl.L.song : "؟؟؟";
		g.font = $"36px {TFONT}"; g.fillStyle = PINK; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(song, W / 2, H / 2 + 40 + 30 * sl * sl - up); g.direction = "rtl";
		if (online) txt((net.coop ? "مع " : "ضد ") + net.other, W / 2, H / 2 + 110 + 30 * sl * sl - up, 30, CYAN, 800);
		g.restore();
	}

	// the pieces taken pass small and faint behind the map, right to left, again every while:
	// the first along the bottom floor, the second the middle, the third the top (his words 14:59 10-19)
	static readonly double[] PASS_Y = { 520, 280, 100 };
	const double PASS_T = 7, PASS_EVERY = 13;
	static void plPass()
	{
		for (int i = 0; i < pl.taken.Count && i < PASS_Y.Length; i++)
		{
			double a0 = pl.t - pl.taken[i].at; if (a0 < 0) continue;
			double f = (a0 % PASS_EVERY) / PASS_T; if (f >= 1) continue;
			double x = W + 60 - (W + 120) * f, a = 0.28 * Math.Min(1, Math.Min(f, 1 - f) * 8);
			g.save(); g.globalAlpha = a; plPieceIcon(x, PASS_Y[i] + 8 * JS.sin(f * Math.PI * 4), 2.2, pl.taken[i].k, 1, false); g.restore();   // no frame: it must not look like a piece to take
		}
	}
	// each level's own touch behind the map (same colours): disco = I Will Survive
	static void plTheme(double now, double b)
	{
		if (pl.L.theme != "disco") return;
		double t = now / 1000, bx = 1120, by = 262, R = 28;   // hangs in the empty right side of the middle floor
		// light spots from the ball drifting over the background
		string[] cols = { "255,42,109", "170,130,255", "45,226,230", "255,200,80" };
		for (int i = 0; i < 9; i++)
		{
			double a = t * 0.18 + i * 0.7, d = 260 + 140 * ((i * 37) % 5), x = bx + JS.cos(a + i) * d * 1.3, y = by + 80 + JS.sin(a * 1.3 + i * 2) * d * 0.55;
			string c = cols[i % 4];
			for (int k = 0; k < 3; k++) { g.fillStyle = $"rgba({c},{str(0.018 + 0.012 * pulse)})"; g.beginPath(); g.arc(x, y, 34 - k * 10, 0, TAU); g.fill(); }
		}
		// the ball on its string, turning slowly, flashing a little on the beat
		g.save(); g.globalAlpha = 0.4;
		g.strokeStyle = "rgba(255,255,255,0.35)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(bx, 200); g.lineTo(bx, by - R); g.stroke();
		g.fillStyle = "#1a1330"; g.beginPath(); g.arc(bx, by, R, 0, TAU); g.fill();
		g.save(); g.beginPath(); g.arc(bx, by, R, 0, TAU); g.clip();
		double sp = 9, rot = (t * 14) % sp;
		for (double yy = -R; yy < R; yy += sp) for (double xx = -R - sp + rot; xx < R; xx += sp)
		{
			double k = 0.5 + 0.5 * JS.sin(xx * 0.4 + yy * 0.7 + t * 3 + b * 2);
			g.fillStyle = $"rgba(220,210,255,{str(0.15 + 0.55 * k * k + 0.25 * pulse)})"; g.fillRect(bx + xx + 1, by + yy + 1, sp - 2, sp - 2);
		}
		g.restore();
		g.strokeStyle = "rgba(170,130,255,0.7)"; g.lineWidth = 1.5; g.beginPath(); g.arc(bx, by, R, 0, TAU); g.stroke();
		{ double gs = 5 + 7 * pulse; g.strokeStyle = "rgba(255,255,255,0.9)"; g.lineWidth = 2; g.beginPath(); g.moveTo(bx - 12 - gs, by - 12); g.lineTo(bx - 12 + gs, by - 12); g.moveTo(bx - 12, by - 12 - gs); g.lineTo(bx - 12, by - 12 + gs); g.stroke(); }   // a glint on the beat
		g.restore();
	}

	// a piece of the song: 0 drums, 1 voice, 2 music (gold)
	static void plPieceIcon(double x, double y, double s, int kind_, double a, bool frame = true)
	{
		g.save(); g.translate(x, y); g.scale(s, s); g.globalAlpha *= a;
		g.shadowColor = GOLD; g.shadowBlur = 14; g.strokeStyle = GOLD; g.fillStyle = GOLD; g.lineWidth = 2.5;
		if (frame) { g.save(); g.rotate(Math.PI / 4); g.strokeRect(-14, -14, 28, 28); g.restore(); } g.shadowBlur = 0;
		if (kind_ == 0) { g.beginPath(); g.ellipse(0, -4, 8, 3.5, 0, 0, TAU); g.stroke(); g.beginPath(); g.moveTo(-8, -4); g.lineTo(-8, 4); g.moveTo(8, -4); g.lineTo(8, 4); g.stroke(); g.beginPath(); g.ellipse(0, 4, 8, 3.5, 0, 0, Math.PI); g.stroke(); }
		else if (kind_ == 1) { rr(-4, -10, 8, 13, 4); g.fill(); g.beginPath(); g.arc(0, 1, 7, 0, Math.PI); g.stroke(); g.fillRect(-1, 8, 2, 4); }
		else { g.beginPath(); g.ellipse(-3, 6, 4.5, 3.5, -0.4, 0, TAU); g.fill(); g.fillRect(0.5, -10, 2, 16); g.beginPath(); g.moveTo(2.5, -10); g.quadraticCurveTo(10, -6, 7, 1); g.lineTo(6, 0); g.quadraticCurveTo(7, -5, 2.5, -6); g.closePath(); g.fill(); }
		g.restore();
	}
}
