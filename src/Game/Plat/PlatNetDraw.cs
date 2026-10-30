using System;
using System.Linq;
using static MG.JS;

namespace MG;

// drawing the online screens of the platformer
public static partial class G
{
	const string NGOLD = "#ffd25c";
	static void codeTxt(string s, double x, double y, double size, string col)
	{
		g.font = $"{str(size)}px {TFONT}"; g.fillStyle = col; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillText(s, x, y); g.direction = "rtl";
	}
	static string secs(double t) => ar(Math.Round(Math.Max(0, t) * 10) / 10) + " ث";

	// a line on the level list: why the room ended, or why online is closed
	static void drawNetNote(double now)
	{
		double a = 1 - clamp((now - netMsgAt - (netDiag != null ? 15000 : 3500)) / 500, 0, 1); if (netMsg == null || a <= 0) return;
		g.globalAlpha = a; txt(netMsg, W / 2, H - 80, 26, NGOLD, 700);
		if (netDiag != null) txt(netDiag, W / 2, H - 52, 16, "rgba(255,255,255,0.55)", 500);
		g.globalAlpha = 1;
	}

	static void drawPltOn(double now)
	{
		bgShapes(now); txt("أونلاين", W / 2, 105, 48, "#fff");
		txt(PLATS[plSel].name, W / 2, 165, 28, RED3, 700);
		for (int i = 0; i < ON_OPTS.Length; i++)
		{
			double y = 270 + i * 88; bool sel = i == onSel; int ii = i; zone(W / 2 - 260, y - 38, 520, 76, () => onSel = ii);
			rr(W / 2 - 260, y - 38, 520, 76, 18); g.fillStyle = sel ? CARDS : CARD; g.fill(); g.strokeStyle = sel ? RED : "rgba(255,255,255,0.12)"; g.lineWidth = sel ? 3 : 1.5; g.stroke();
			txt(ON_OPTS[i], W / 2, y, sel ? 36 : 32, sel ? "#fff" : "rgba(255,255,255,0.6)", 800);
		}
		if (PLATS[plSel].team != null)
		{
			txt("تلعبون ٣ مع بعض: واحد يسوي الغرفة، والاثنين يدخلون بالكود", W / 2, 520, 23, "rgba(255,255,255,0.65)", 500);
			txt("ما تعدّون أي صفحة إلا بالتعاون، والثلاثة لازم يوصلون", W / 2, 556, 23, "rgba(255,255,255,0.65)", 500);
		}
		else if (PLATS[plSel].coop)
		{
			txt("تلعبون مع بعض: أي قطعة يجيبها واحد تنحسب للاثنين، واللي يطيح يرجّعه صاحبه إذا لمسه", W / 2, 520, 23, "rgba(255,255,255,0.65)", 500);
			txt("ولازم توصلون البوابة الاثنين عشان تخلصون المرحلة", W / 2, 556, 23, "rgba(255,255,255,0.65)", 500);
		}
		else
		{
			txt("تلعبون من ٢ إلى ٤ نفس المرحلة بنفس الوقت: واحد يسوي الغرفة، والباقين يدخلون بالكود", W / 2, 520, 23, "rgba(255,255,255,0.65)", 500);
			txt("المباراة تخلص لما يبقى واحد بس ما وصل البوابة، وهو الخسران", W / 2, 556, 23, "rgba(255,255,255,0.65)", 500);
		}
		drawNetNote(now);
		row(new[] { (BTN[kind].confirm, "اختيار"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}

	static void drawPltJoin(double now)
	{
		bgShapes(now); txt("ادخل كود الغرفة", W / 2, 130, 48, "#fff");
		double bw = 92, gap = 22, x0 = W / 2 - (5 * bw + 4 * gap) / 2, y = 320;
		for (int i = 0; i < 5; i++)
		{
			double x = x0 + i * (bw + gap); bool cur = i == Math.Min(joinCode.Length, 4) && !usingPad || usingPad && i == joinDig;
			rr(x, y - 60, bw, 120, 16); g.fillStyle = CARD; g.fill(); g.strokeStyle = cur ? RED : "rgba(255,255,255,0.18)"; g.lineWidth = cur ? 3 : 1.5; g.stroke();
			if (i < joinCode.Length) codeTxt(joinCode[i].ToString(), x + bw / 2, y + 4, 64, "#fff");
			else if (cur && Math.Floor(now / 500) % 2 == 0) { g.fillStyle = RED; g.fillRect(x + bw / 2 - 18, y + 36, 36, 4); }
		}
		txt(usingPad ? "فوق وتحت يغيّرون الرقم، يمين ويسار تنقل بين الخانات" : "اكتب الأرقام اللي عطاك إياها صاحبك", W / 2, 460, 24, "rgba(255,255,255,0.65)", 500);
		row(new[] { (BTN[kind].confirm, "ادخل"), (BTN[kind].back, "رجوع") }, W / 2, H - 30, 24, joinCode.Length == 5 ? "rgba(255,255,255,0.85)" : "rgba(255,255,255,0.5)", 18);
	}

	static void drawPltRoom(double now)
	{
		bgShapes(now); var n = net; if (n == null) return;
		if (n.team) { drawTeamRoom(now); return; }
		if (n.vs) { drawVsRoom(now); return; }
		string ph = n.phase; string dots = new string('.', 1 + (int)Math.Floor(now / 400) % 3);
		if (ph is "make" or "wait")
		{
			txt("غرفتك", W / 2, 105, 48, "#fff");
			txt("عط صاحبك هذا الكود يدخله من: البلاتفورم ← أونلاين ← ادخل بكود", W / 2, 175, 24, "rgba(255,255,255,0.7)", 500);
			if (ph == "wait") codeTxt(n.code, W / 2, 320, 130, RED);
			else txt("أسوي الغرفة" + dots, W / 2, 320, 36, "rgba(255,255,255,0.7)", 700);
			if (ph == "wait") txt("أستنى صاحبك يدخل" + dots, W / 2, 470, 30, "rgba(255,255,255,0.75)", 700);
		}
		else if (ph is "join" or "conn")
		{
			txt(n.host ? "صاحبك دخل!" : "أدخل الغرفة", W / 2, 105, 48, "#fff");
			txt("جاري الاتصال" + dots, W / 2, H / 2, 36, "rgba(255,255,255,0.75)", 700);
		}
		else if (ph == "lobby")
		{
			txt("جاهزين؟", W / 2, 105, 48, "#fff");
			txt(n.L?.name ?? "", W / 2, 165, 28, RED3, 700);
			string me = acc?.name ?? "", ot = n.hello ? n.other : n.other + dots;
			string left = n.host ? ot : me, right = n.host ? me : ot, lsk = n.host ? n.otherSk : skin, rsk = n.host ? skin : n.otherSk;
			double dh = (n.otherSk ?? "") == skin ? SEAT_HUE[1] : -1;   // the same look: the one who came in (the guest) in gold
			lobbySq(W / 2 + 200, 330, right, rsk, now, n.host ? -1 : dh); lobbySq(W / 2 - 200, 330, left, lsk, now, n.host ? dh : -1);
			txt(n.coop ? "مع" : "ضد", W / 2, 330, 40, "rgba(255,255,255,0.5)", 800);
			if (n.rtt >= 0) { g.font = $"18px {TFONT}"; g.fillStyle = "rgba(255,255,255,0.4)"; g.textAlign = "center"; g.direction = "ltr"; g.fillText($"{Math.Round(n.rtt)} ms", W / 2, 520); g.direction = "rtl"; }
			if (n.host && n.hello) row(new[] { (BTN[kind].confirm, "ابدأ") }, W / 2, 590, 38, "#fff", 30);
			else if (!n.host) txt($"أستنى {n.other} يبدأ" + dots, W / 2, 590, 28, "rgba(255,255,255,0.75)", 700);
		}
		row(new[] { (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	static void lobbySq(double x, double y, string name, string sk, double now, double hue = -1)
	{
		if (hue >= 0) g.setTint(hue);
		g.save(); g.translate(x, y - 30); g.scale(3, 3); g.rotate(Math.Sin(now / 600) * 0.15);
		if (string.IsNullOrEmpty(sk)) { g.shadowColor = CYAN; g.shadowBlur = 14; g.fillStyle = CYAN; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = ARENA; g.fillRect(-3, -3, 6, 6); }
		else skinBody(sk, 1, now);
		g.restore(); g.clearTint();
		txt(name, x, y + 70, 30, "#fff", 800);
	}

	// the other player's square and name (see-through)
	static void drawNetGhost(double now)
	{
		if (net.vs) { drawVsGhosts(now); return; }
		var gp = netGhost(now); if (gp == null) return;
		if (gp.dash == -1) { drawDown(gp.x, gp.y, now, true); return; }
		tinted(net.host && (net.otherSk ?? "") == skin ? SEAT_HUE[1] : -1, () => drawPlayer(gp, net.otherSk ?? "", 0.55));
		g.globalAlpha = 0.7; txt(net.other, gp.x, gp.y - 30, 18, "#fff", 700); g.globalAlpha = 1;
	}

	// a square out of lives in co-op: an empty frame that beats, until the other one touches it
	static void drawDown(double x, double y, double now, bool other)
	{
		double k = 0.5 + 0.5 * Math.Sin(now / 180);
		g.save(); g.translate(x, y); g.globalAlpha = other ? 0.75 : 0.6; g.strokeStyle = CYAN; g.lineWidth = 2; g.shadowColor = CYAN; g.shadowBlur = 8 + 8 * k;
		g.strokeRect(-9, -9, 18, 18); g.beginPath(); g.arc(0, 0, 18 + 6 * k, 0, TAU); g.stroke(); g.shadowBlur = 0; g.restore(); g.globalAlpha = 1;
		if (other) { g.globalAlpha = 0.85; txt("المسه عشان يرجع", x, y - 34, 18, CYAN, 700); g.globalAlpha = 1; }
	}

	static void drawVsEnd(double now)
	{
		var n = net; if (n == null) return;
		if (n.vs) { drawVsEndN(now); return; }
		overlay(0.72); double ts = now - n.endAt, e = clamp(ts / 220, 0, 1), sc = 1 + 0.6 * JS.pow(1 - e, 3);
		g.save(); g.translate(W / 2, H / 2 - 120); g.scale(sc, sc); g.globalAlpha = e;
		if (n.coopDone) txt("خلّصتوا المرحلة!", 0, 0, 80, NGOLD); else if (n.iWon) txt("فزت!", 0, 0, 100, NGOLD); else txt($"{n.other} فاز", 0, 0, 80, PINK);
		g.restore(); g.globalAlpha = 1;
		if (n.coopDone) txt("وقتكم: " + secs(n.myFin), W / 2, H / 2 + 10, 30, "#e9e6f5", 600);
		else if (n.myFin >= 0) txt("وقتك: " + secs(n.myFin), W / 2, H / 2 + 10, 30, "#e9e6f5", 600);
		if (n.otherFin >= 0 && !n.coopDone) txt($"وقت {n.other}: " + secs(n.otherFin), W / 2, H / 2 + (n.myFin >= 0 ? 55 : 10), 30, "#e9e6f5", 600);
		if (ts > 800)
		{
			if (n.host) row(new[] { (BTN[kind].confirm, "العب مرة ثانية") }, W / 2, H / 2 + 135);
			else txt($"أستنى {n.other} يبدأ مرة ثانية", W / 2, H / 2 + 135, 28, "rgba(255,255,255,0.75)", 700);
			row(n.coopDone || pl == null ? new[] { (BTN[kind].back, "اطلع من الغرفة") } : new[] { myRpHint(n.endAt), (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H / 2 + 190);
		}
	}

	// ---------- the co-op level for three ----------
	// the room: the code, then each one's square in its colour with the name (dots while still coming)
	static void drawTeamRoom(double now)
	{
		var n = net; string ph = n.phase, dots = new string('.', 1 + (int)Math.Floor(now / 400) % 3);
		if (n.host && ph is "make" or "wait")
		{
			txt("غرفتك", W / 2, 95, 48, "#fff");
			txt(tnN > 2 ? "عط صاحبينك هذا الكود يدخلونه من: البلاتفورم ← أونلاين ← ادخل بكود" : "عط صاحبك هذا الكود يدخله من: البلاتفورم ← أونلاين ← ادخل بكود", W / 2, 155, 24, "rgba(255,255,255,0.7)", 500);
			if (ph == "wait") codeTxt(n.code, W / 2, 255, 110, RED); else txt("أسوي الغرفة" + dots, W / 2, 255, 36, "rgba(255,255,255,0.7)", 700);
		}
		else if (!n.host && ph is "join" or "conn") { txt("أدخل الغرفة", W / 2, 105, 48, "#fff"); txt("جاري الاتصال" + dots, W / 2, 255, 36, "rgba(255,255,255,0.75)", 700); }
		else { txt("جاهزين؟", W / 2, 95, 48, "#fff"); txt(n.L?.name ?? "", W / 2, 155, 28, RED3, 700); }
		int N = tnN;
		for (int i = 0; i < N; i++)
		{
			double x = W / 2 + ((N - 1) / 2.0 - i) * 300, y = 430; string col = tnCol(i); bool in_ = n.hi[i] || (!n.host && i == n.seat && n.said[0]);
			string name = n.nm[i] is { Length: > 0 } s ? s : "";
			g.save(); g.translate(x, y - 30); g.scale(3, 3); g.rotate(Math.Sin(now / 600 + i) * 0.15); g.globalAlpha = in_ ? 1 : 0.25;
			coopSquare(col, in_ ? n.sk[i] : "", 1, now, ARENA);
			g.restore(); g.globalAlpha = 1;
			txt(in_ ? name : (name != "" ? name + dots : "أستنى" + dots), x, y + 70, 30, in_ ? "#fff" : "rgba(255,255,255,0.5)", 800);
			if (i == n.seat) txt("أنت", x, y + 108, 22, col, 700);
		}
		string waitOne = N > 2 ? "أستنى الثالث يدخل" : "أستنى صاحبك يدخل";
		if (ph == "lobby" && n.host && tnReady) row(new[] { (BTN[kind].confirm, "ابدأ") }, W / 2, 620, 38, "#fff", 30);
		else if (ph == "lobby" && !n.host) txt((tnReady ? $"أستنى {tnName(0)} يبدأ" : waitOne) + dots, W / 2, 620, 28, "rgba(255,255,255,0.75)", 700);
		else if (n.host && ph == "wait") txt((N == 2 || n.hi[1] || n.hi[2] ? waitOne : "أستنى الاثنين يدخلون") + dots, W / 2, 620, 28, "rgba(255,255,255,0.75)", 700);
		row(new[] { (BTN[kind].back, "اطلع من الغرفة") }, W / 2, H - 30, 24, "rgba(255,255,255,0.7)", 18);
	}
	// the level online: the start (the same moment for the three), the level from this one's own screen, the pause
	static void drawTeamNet(double now)
	{
		var n = net;
		if (state == "tmIntro" || tm == null)
		{
			var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#1c1033"); gr.addColorStop(0.55, "#4a1f4f"); gr.addColorStop(1, "#ff9a5c"); g.fillStyle = gr; g.fillRect(0, 0, W, H);
			if (lcT) { drawLcTeamIntro(now); return; }
			if (n == null) return;
			txt(n.L?.name ?? "", W / 2, 150, 64, "#fff", 800);
			g.font = $"26px {TFONT}"; g.fillStyle = "rgba(255,255,255,0.8)"; g.textAlign = "center"; g.direction = "ltr"; g.textBaseline = "middle"; g.fillText(n.L?.best != null ? n.L.song : "؟؟؟", W / 2, 215); g.direction = "rtl";
			for (int i = 0; i < tnN; i++)
			{
				double x = W / 2 + ((tnN - 1) / 2.0 - i) * 260, y = 380; string col = tnCol(i);
				if (string.IsNullOrEmpty(n.sk[i])) { g.shadowColor = col; g.shadowBlur = 20; g.fillStyle = col; g.fillRect(x - 27, y - 27, 54, 54); g.shadowBlur = 0; g.fillStyle = "#1b0f26"; g.fillRect(x - 9, y - 9, 18, 18); }
				else { g.save(); g.translate(x, y); g.scale(3, 3); coopSquare(col, n.sk[i], 1, now, "#1b0f26"); g.restore(); }
				txt(tnName(i), x, y + 70, 30, "#fff", 800); if (i == n.seat) txt("أنت", x, y + 106, 22, col, 700);
			}
			double left = (n.goAt - now) / 1000; if (left > 0) txt(ar(Math.Ceiling(left)), W / 2, 600, 70, GOLD, 800);
			return;
		}
		drawTeam(now);
		if (state is "tmPause" or "tmSet") overlay(state == "tmSet" ? 0.95 : 0.75);
		if (state == "tmPause")
		{
			if (lcT) txt("اللعبة متوقفة", W / 2, 165, 56, "#fff");
			else { txt("اللعبة متوقفة عندك", W / 2, 165, 56, "#fff"); txt("الباقين يكملون، ومربعك واقف مكانه", W / 2, 222, 22, "rgba(255,255,255,0.55)", 500); }
			var tl = lcT ? LC_PAUSE_T : tmPauseList(); bool six = tl.Length > 4;   // v49: the maker's 6 items sit closer
			if (hostAsk != 0) drawHostAsk();
			else for (int i = 0; i < tl.Length; i++)
			{
				double y = six ? 292 + i * 68 : 310 + i * 80, bh = six ? 60 : 68; bool sel = i == pauseSel; int ii = i; zone(W / 2 - 260, y - bh / 2, 520, bh, () => pauseSel = ii);
				if (sel) { rr(W / 2 - 260, y - bh / 2, 520, bh, 16); g.fillStyle = $"rgba({RGB},0.12)"; g.fill(); g.strokeStyle = RED; g.lineWidth = 2; g.stroke(); }
				txt(tl[i], W / 2, y, sel ? 38 : 32, sel ? "#fff" : "rgba(255,255,255,0.5)", 800);
			}
		}
		if (state == "tmSet") drawSettings();
	}
}
