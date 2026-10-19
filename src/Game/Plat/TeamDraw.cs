using System;
using System.Linq;
using static MG.JS;

namespace MG;

// drawing the co-op level: a sky above the clouds at sunset (it sets further as the page goes on)
public static partial class G
{
	static void cloudBlob(double x, double y, double w, double h, string fill, double a)
	{
		g.save(); g.globalAlpha *= a; g.fillStyle = fill; g.beginPath();
		int n = Math.Max(2, (int)Math.Round(w / 22)); double r = Math.Min(h * 0.9, w / n * 0.75);
		for (int i = 0; i < n; i++) { double cx = x + w * (i + 0.5) / n, cy = y + h * 0.35 - (i % 2 == 0 ? r * 0.25 : 0); g.moveTo(cx + r, cy); g.arc(cx, cy, r, 0, TAU); }
		g.fill(); rr(x, y + h * 0.15, w, h * 0.85, h * 0.4); g.fill(); g.restore();
	}

	public static void drawTeam(double now)
	{
		double b = tmBeat(), bp = ((b % 1) + 1) % 1; pulse = JS.exp(-bp * 5) * 0.8;
		double prog = Math.Clamp((tm.got.Count + (tm.b.Count(q => q.gate) > 0 ? 0.5 : 0)) / Math.Max(1.0, tmPieces() + 0.5), 0, 1);
		long bi = (long)Math.Floor(b);
		bool storm = tm.P.theme == 1;
		if (storm) tmStormSky(now, b, bi, bp);
		else {
		// the sky: deep purple on top to a warm orange low, the sun going down as the pieces come in
		var gr = g.createLinearGradient(0, 0, 0, H);
		gr.addColorStop(0, "#1c1033"); gr.addColorStop(0.45, prog < 0.5 ? "#4a1f4f" : "#3d1846"); gr.addColorStop(0.8, "#c2475a"); gr.addColorStop(1, "#ff9a5c");
		g.fillStyle = gr; g.fillRect(0, 0, W, H);
		{
			double sy = 560 + prog * 120, sr = 110;
			for (int k = 4; k >= 1; k--) { g.fillStyle = $"rgba(255,170,110,{str(0.05 * k)})"; g.beginPath(); g.arc(W * 0.62, sy, sr + k * 40, 0, TAU); g.fill(); }
			g.fillStyle = "#ffd29a"; g.beginPath(); g.arc(W * 0.62, sy, sr, 0, TAU); g.fill();
		}
		// far clouds drifting slowly
		for (int i = 0; i < 7; i++)
		{
			double sp = 6 + i * 2, x = ((i * 233 + now / 1000 * sp) % (W + 400)) - 200, y = 110 + (i * 97) % 520;
			cloudBlob(x, y, 180 + (i * 53) % 140, 40, "rgba(255,200,210,1)", 0.06 + 0.02 * (i % 3));
		}
		}
		// walls: deep plum with a warm edge (near black with a red edge in the storm)
		g.fillStyle = storm ? "#10050a" : "#1b0f26";
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++) if (tm.m[y][x] == '#') g.fillRect(x * PT, y * PT, PT + 0.5, PT + 0.5);
		g.strokeStyle = storm ? $"rgba(255,70,90,{str(0.45 + 0.4 * pulse)})" : $"rgba(255,160,140,{str(0.5 + 0.35 * pulse)})"; g.lineWidth = 2; g.shadowColor = storm ? "#ff3048" : "#ff8a7a"; g.shadowBlur = 8; g.beginPath();
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++)
		{
			if (tm.m[y][x] != '#') continue; double X = x * PT, Y = y * PT;
			if (y > 0 && tm.m[y - 1][x] != '#') { g.moveTo(X, Y + 1); g.lineTo(X + PT, Y + 1); }
			if (y < tm.h - 1 && tm.m[y + 1][x] != '#') { g.moveTo(X, Y + PT - 1); g.lineTo(X + PT, Y + PT - 1); }
			if (x > 0 && tm.m[y][x - 1] != '#') { g.moveTo(X + 1, Y); g.lineTo(X + 1, Y + PT); }
			if (x < tm.w - 1 && tm.m[y][x + 1] != '#') { g.moveTo(X + PT - 1, Y); g.lineTo(X + PT - 1, Y + PT); }
		}
		g.stroke(); g.shadowBlur = 0;
		// the scale's rope over the top, from pan L to pan R
		foreach (var s in tm.th.Where(s => s.k == "scale"))
		{
			double lx = s.L.X + s.L.W / 2, rx = s.R.X + s.R.W / 2;
			g.strokeStyle = "rgba(255,230,210,0.55)"; g.lineWidth = 2; g.beginPath(); g.moveTo(lx, s.L.Y); g.lineTo(lx, 22); g.lineTo(rx, 22); g.lineTo(rx, s.R.Y); g.stroke();
			foreach (double px in new[] { lx, rx }) { g.fillStyle = "#ffe0c8"; g.beginPath(); g.arc(px, 22, 6, 0, TAU); g.fill(); }
		}
		// lightning warnings (the beat before) and strikes
		foreach (var s in tm.th.Where(s => s.k == "bolt" && tmBoltLive(s)))
		{
			foreach (int c in tmBolts(s, bi + 1)) { double a = 0.15 + 0.35 * bp, cx = s.of.X + c * PT; g.fillStyle = $"rgba(220,200,255,{str(a * 0.35)})"; g.fillRect(cx, s.Y, PT, s.H); g.strokeStyle = $"rgba(230,215,255,{str(a)})"; g.lineWidth = 1.5; g.setLineDash(new double[] { 6, 8 }); g.beginPath(); g.moveTo(cx + PT / 2, s.Y); g.lineTo(cx + PT / 2, s.Y + s.H); g.stroke(); g.setLineDash(new double[0]); }
			if (bp < 0.2) foreach (int c in tmBolts(s, bi))
			{
				double a = 1 - bp / 0.2, x = s.of.X + c * PT + PT / 2; g.save(); g.globalAlpha = a; g.strokeStyle = "#f4ecff"; g.shadowColor = "#b48cff"; g.shadowBlur = 24; g.lineWidth = 4; g.beginPath(); g.moveTo(x, s.Y);
				for (double y = s.Y; y < s.Y + s.H; y += 22) g.lineTo(x + ((((long)y * 7 + bi * 13) % 5) - 2) * 6, y + 22);
				g.stroke(); g.shadowBlur = 0; g.fillStyle = "rgba(220,200,255,0.25)"; g.fillRect(x - PT / 2, s.Y, PT, s.H); g.restore();
			}
		}
		// wind: streaks in the zone while a gust blows
		foreach (var s in tm.th.Where(s => s.k == "wind"))
		{
			double ev = s.d.b; long gk = (long)Math.Floor(b / ev); double into = b - gk * ev, dir = gk % 2 == 0 ? 1 : -1;
			double a = into < s.d.c ? 1 : into < s.d.c + 0.4 ? 1 - (into - s.d.c) / 0.4 : 0.12;
			g.strokeStyle = $"rgba(255,240,235,{str(0.28 * a)})"; g.lineWidth = 2; g.beginPath();
			for (int i = 0; i < 14; i++)
			{
				double y = s.Y + 20 + (i * 37) % (s.H - 30), len = 30 + (i * 19) % 40, x = s.X + ((i * 151 + dir * now / 1000 * 520) % s.W + s.W) % s.W;
				g.moveTo(x, y); g.lineTo(x - dir * len, y);
			}
			g.stroke();
			// a small arrow on the ceiling showing the next push
			long nk = into < s.d.c ? gk : gk + 1; double nd = nk % 2 == 0 ? 1 : -1, ax = s.X + s.W / 2, ay = s.Y + 16;
			g.fillStyle = $"rgba(255,240,235,{str(0.25 + 0.4 * (into >= s.d.c ? (into - s.d.c) / (ev - s.d.c) : 1))})";
			g.beginPath(); g.moveTo(ax + nd * 18, ay); g.lineTo(ax - nd * 8, ay - 9); g.lineTo(ax - nd * 8, ay + 9); g.closePath(); g.fill();
		}
		// hail: the warning glint under the ceiling, then the ice
		foreach (var s in tm.th.Where(s => s.k == "hail"))
			if (bp > 0.5) foreach (int c in tmHailCols(s, bi + 1)) { double a = (bp - 0.5) * 2; g.fillStyle = $"rgba(200,235,255,{str(0.7 * a)})"; g.beginPath(); g.moveTo(c * PT + PT / 2 - 7, s.Y + 2); g.lineTo(c * PT + PT / 2 + 7, s.Y + 2); g.lineTo(c * PT + PT / 2, s.Y + 2 + 12 * a); g.closePath(); g.fill(); }
		foreach (var q in tm.hail)
		{
			g.save(); g.translate(q.x, q.y); g.fillStyle = "#d8f1ff"; g.shadowColor = "#9fd8ff"; g.shadowBlur = 10; g.beginPath(); g.moveTo(0, 10); g.lineTo(6, -2); g.lineTo(0, -10); g.lineTo(-6, -2); g.closePath(); g.fill(); g.shadowBlur = 0; g.restore();
		}
		// spikes
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++)
		{
			char c = tm.m[y][x]; double X = x * PT, Y = y * PT;
			if (c == '^' || c == 'v')
			{
				g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8 + 8 * pulse; g.beginPath();
				for (int i = 0; i < 3; i++) { double sx = X + i * PT / 3, h = PT * (0.5 + 0.08 * pulse); if (c == '^') { g.moveTo(sx, Y + PT); g.lineTo(sx + PT / 6, Y + PT - h); g.lineTo(sx + PT / 3, Y + PT); } else { g.moveTo(sx, Y); g.lineTo(sx + PT / 6, Y + h); g.lineTo(sx + PT / 3, Y); } }
				g.fill(); g.shadowBlur = 0;
			}
		}
		// the things
		foreach (var s in tm.th)
		{
			switch (s.k)
			{
				case "sink":
					{
						if (s.melt) { cloudBlob(s.X, s.Y, s.W, 22, "#ffffff", 0.12 + 0.1 * Math.Sin(now / 90)); break; }
						double f = s.off / (s.d.a * PT); cloudBlob(s.X, s.Y - 4, s.W, 26, f > 0.66 ? "#ffd0d6" : "#fff6f2", 0.95 - 0.35 * f);
						break;
					}
				case "stone":
					{
						var c = s.of; if (c == null || c.melt) break; double x = s.d.x * PT, y = c.Y - 12, k = s.on ? 1 : 0.35;
						g.save(); g.shadowColor = GOLD; g.shadowBlur = 10 + 20 * k * (0.7 + 0.3 * pulse); g.fillStyle = s.on ? "#ffe08a" : "#a88a5a";
						g.beginPath(); g.moveTo(x, y - 12); g.lineTo(x + 9, y); g.lineTo(x, y + 10); g.lineTo(x - 9, y); g.closePath(); g.fill(); g.restore();
						break;
					}
				case "bridge": case "path":
					if (s.s > 0.02) cloudBlob(s.X, s.Y - 4, s.W, 18, storm ? "#ffd9dd" : "#fff2e8", s.s * 0.95);
					else { g.strokeStyle = "rgba(255,240,230,0.25)"; g.lineWidth = 2; g.setLineDash(new double[] { 5, 9 }); g.strokeRect(s.X + 2, s.Y, s.W - 4, 12); g.setLineDash(new double[0]); }
					if (s.d.need != null) foreach (var n in tm.th.Where(o => s.d.need.Contains(o.d.id)))   // a thin light from each stone / button
					{
						double sx = n.k == "stone" ? n.d.x * PT : n.X + n.W / 2, sy = n.k == "stone" ? (n.of?.Y ?? n.Y) - 12 : n.Y - 6;
						if (n.k == "cbtn") { g.strokeStyle = n.on ? "rgba(255,90,110,0.45)" : "rgba(255,90,110,0.1)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(sx, sy); g.lineTo(Math.Clamp(sx, s.X, s.X + s.W), s.Y + 4); g.stroke(); continue; }
						g.strokeStyle = n.on ? "rgba(255,224,138,0.45)" : "rgba(255,224,138,0.08)"; g.lineWidth = 1.5; g.beginPath(); g.moveTo(sx, sy); g.lineTo(s.X + (sx < s.X ? 0 : s.W), s.Y + 4); g.stroke();
					}
					break;
				case "door":
					{
						double open = s.s;   // 0 closed, 1 open: the cloud wall parts up and down
						int n = (int)Math.Round(s.H / 26);
						for (int i = 0; i < n; i++)
						{
							double f = (i + 0.5) / n, side = f < 0.5 ? -1 : 1, push = open * (f < 0.5 ? (0.5 - f) : (f - 0.5)) * 2;   // the puffs part up and down
							double y = s.Y + s.H * f + side * open * s.H * 0.5 * (0.4 + push), r = 17 + 3 * Math.Sin(i * 1.7 + now / 400);
							g.fillStyle = $"rgba(255,240,234,{str(0.92 * (1 - open * 0.85))})"; g.beginPath(); g.arc(s.X + s.W / 2 + Math.Sin(i * 2.3) * 4, y, r, 0, TAU); g.fill();
						}
						break;
					}
				case "fall":
					{
						if (s.gone && s.off > 400) break; double sh = s.timer > 0 && !s.gone ? Math.Sin(now / 25) * 3 : 0;
						cloudBlob(s.X + sh, s.Y - 4, s.W, 24, s.timer > 0 && !s.gone ? "#ffd6dc" : "#fff6f2", s.gone ? Math.Max(0, 0.9 - s.off / 300) : 0.95);
						break;
					}
				case "panL": case "panR":
					cloudBlob(s.X - 4, s.Y - 4, s.W + 8, 22, "#ffe9dc", 0.95);
					g.strokeStyle = "rgba(255,230,210,0.55)"; g.lineWidth = 2; g.beginPath(); g.moveTo(s.X + 4, s.Y); g.lineTo(s.X + s.W / 2, s.Y - 30); g.lineTo(s.X + s.W - 4, s.Y); g.stroke();
					break;
				case "hatch":
					if (s.s < 0.98) { g.globalAlpha = 1 - s.s; cloudBlob(s.X, s.Y + 2, s.W, s.H - 4, "#ffe9dc", 0.9); g.globalAlpha = 1; }
					break;
				case "btn":
					{
						double dn = s.on ? 4 : 0; g.fillStyle = s.on ? "#ffe08a" : "#b0906a"; g.shadowColor = GOLD; g.shadowBlur = s.on ? 18 : 4;
						rr(s.X + 6, s.Y - 10 + dn, s.W - 12, 10 - dn, 4); g.fill(); g.shadowBlur = 0; break;
					}
				case "cloud": cloudBlob(s.X, s.Y - 4, s.W, 24, storm ? "#ead0d4" : "#fff6f2", 0.95); break;
				case "flip":
					{
						string c = TM_COL[(int)s.d.a];
						if (s.s > 0.02) { g.save(); g.shadowColor = c; g.shadowBlur = 14; cloudBlob(s.X, s.Y - 4, s.W, 24, c, s.s * 0.95); g.restore(); cloudBlob(s.X + 6, s.Y - 1, s.W - 12, 10, "#ffffff", s.s * 0.35); }
						if (s.s < 0.98) { g.strokeStyle = c; g.globalAlpha = 0.4 * (1 - s.s); g.lineWidth = 2; g.setLineDash(new double[] { 5, 7 }); g.strokeRect(s.X + 3, s.Y - 2, s.W - 6, 14); g.setLineDash(new double[0]); g.globalAlpha = 1; }
						tmMoveIcon((int)s.d.a, s.X + s.W / 2, s.Y + 22, 0.5 + 0.5 * s.s);
						break;
					}
				case "cwall":
					{
						string c = TM_COL[(int)s.d.a]; g.save(); g.globalAlpha = 0.16; g.fillStyle = c; g.fillRect(s.X, s.Y, s.W, s.H); g.globalAlpha = 0.75;
						g.strokeStyle = c; g.shadowColor = c; g.shadowBlur = 10; g.lineWidth = 2; g.beginPath();
						for (int i = 0; i < 4; i++) { double x = s.X + 5 + i * (s.W - 10) / 3; g.moveTo(x, s.Y); for (double y = s.Y; y < s.Y + s.H; y += 10) g.lineTo(x + Math.Sin(y / 9 + now / 150 + i) * 3, y + 10); }
						g.stroke(); g.restore(); break;
					}
				case "cbtn":
					{
						string c = TM_COL[(int)s.d.a]; double dn = s.on ? 4 : 0; g.save(); g.fillStyle = c; g.globalAlpha = s.on ? 1 : 0.55; g.shadowColor = c; g.shadowBlur = s.on ? 20 : 6;
						rr(s.X + 6, s.Y - 10 + dn, s.W - 12, 10 - dn, 4); g.fill(); g.restore(); break;
					}
				case "ride":
					{
						cloudBlob(s.X - 6, s.Y - 8, s.W + 12, 32, "#fff4ec", 0.97);
						for (int i = 0; i < TM_N; i++) { g.fillStyle = i < s.n ? TM_COL[i] : "rgba(120,80,110,0.5)"; g.beginPath(); g.arc(s.X + s.W / 2 + (i - 1) * 16, s.Y + 14, 4.5, 0, TAU); g.fill(); }
						break;
					}
			}
		}
		// footprints on the clouds
		foreach (var q in tm.steps) { g.fillStyle = TM_COL[q.who]; g.globalAlpha = 0.35 * (1 - (tm.t - q.t) / 2.5); g.fillRect(q.x - 4, q.y - 3, 8, 3); }
		g.globalAlpha = 1;
		// pieces and the gate
		int total = tmPieces();
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++)
		{
			char c = tm.m[y][x]; double X = x * PT, Y = y * PT;
			if (c >= '1' && c <= '9' && !tm.got.Contains(x + "," + y))
			{
				int s = tm.P.pieces != null && tm.P.pieces.TryGetValue(c.ToString(), out int v) ? v : 0;
				plPieceIcon(X + PT / 2, Y + PT / 2 + 4 * JS.sin(b * Math.PI / 2), 1 + 0.15 * pulse, s, 1);
			}
			else if (c == 'G')
			{
				bool open = tmOpen(); double cx = X + PT / 2, cy = Y + PT / 2 - 8;
				g.save(); g.translate(cx, cy);
				if (open) { g.rotate(now / 600); g.strokeStyle = CYAN; g.shadowColor = CYAN; g.shadowBlur = 16 + 10 * pulse; g.lineWidth = 4; g.beginPath(); g.arc(0, 0, 26 + 3 * pulse, 0, TAU * 0.8); g.stroke(); g.shadowBlur = 0; }
				else { g.strokeStyle = "rgba(255,255,255,0.3)"; g.lineWidth = 3; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(0, 0, 26, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				g.restore();
				int inG = tm.b.Count(q => q.gate);
				for (int i = 0; i < TM_N; i++) { g.fillStyle = tm.b[i].gate ? TM_COL[i] : "rgba(255,255,255,0.18)"; g.fillRect(cx - 20 + i * 15, cy + 34, 10, 10); }
				if (!open) { g.font = $"800 16px {FONT}"; g.fillStyle = "rgba(255,255,255,0.55)"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "rtl"; g.fillText($"{ar(tm.got.Count)}/{ar(total)}", cx, cy + 2); }
			}
		}
		// saws
		foreach (var s in tm.P.saws)
		{
			double sx = tmSawPos(s, out double sy); g.save(); g.translate(sx, sy); g.rotate(now / 120);
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12 + 8 * pulse; g.beginPath();
			int teeth = 10; for (int i = 0; i < teeth * 2; i++) { double a = i * Math.PI / teeth, r2 = i % 2 == 0 ? s.r : s.r * 0.72; if (i == 0) g.moveTo(JS.cos(a) * r2, JS.sin(a) * r2); else g.lineTo(JS.cos(a) * r2, JS.sin(a) * r2); }
			g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "#000"; g.beginPath(); g.arc(0, 0, s.r * 0.25, 0, TAU); g.fill(); g.restore();
		}
		// the red lightning wall from the left
		if (tm.chaseX > -900) tmDrawChase(now, bp);
		if (tm.P.dark) tmDark(now);
		tmDrawVis(now);
		foreach (var q in particles) { g.globalAlpha = Math.Max(0, q.life / q.max); g.fillStyle = q.col; g.fillRect(q.x - q.s / 2, q.y - q.s / 2, q.s, q.s); }
		g.globalAlpha = 1;
		if (tm.P.tether > 0) tmDrawTether(now);
		if (tm.swapA >= 0) tmDrawSwapWarn(now, b);
		// the three
		foreach (var p in tm.b) tmDrawBody(p, now);
		// HUD: each one's lives in its colour, the song's pieces, messages
		for (int i = 0; i < TM_N; i++)
		{
			var p = tm.b[i]; double y0 = 26 + i * 26;
			for (int k = 0; k < P_HP; k++)
			{
				double x = 34 + k * 24;
				if (!p.down && k < p.hp) { g.fillStyle = TM_COL[i]; g.fillRect(x - 8, y0 - 8, 16, 16); }
				else { g.strokeStyle = TM_COL[i]; g.globalAlpha = 0.35; g.lineWidth = 2; g.strokeRect(x - 7, y0 - 7, 14, 14); g.globalAlpha = 1; }
			}
		}
		{
			int ns = tm.L.stems.Length; double x0 = W / 2 + (ns - 1) * 22;
			for (int i = 0; i < ns; i++) plPieceIcon(x0 - i * 44, 24, 0.75, i, tm.stemOn[i] ? 1 : 0.25);
		}
		if (tm.msgT > 0) { g.globalAlpha = Math.Min(1, tm.msgT); txt(tm.msg, W / 2, 74, 30, GOLD); g.globalAlpha = 1; }
		if (tm.restartAt >= 0) { double a = clamp(1 - (tm.restartAt - tm.t) / 0.9, 0, 1); g.fillStyle = $"rgba(20,8,30,{str(a * 0.85)})"; g.fillRect(0, 0, W, H); }
		// between pages: warm clouds rise over the screen, then keep rising off the top on the next page
		if (tm.doneAt >= 0) tmCloudSwap(1 - clamp((tm.t - tm.doneAt) / (TM_SWAP * 0.85), 0, 1), now, tm.P.theme);
		else if (tm.fromClouds && tm.pageAt < TM_SWAP) tmCloudSwap(-clamp(tm.pageAt / TM_SWAP, 0, 1), now, tm.coverTheme);
	}

	static double ease(double x) => x * x * (3 - 2 * x);
	// p 1..0: the cloud edge comes up from under the screen to its top; p 0..-1: its lower edge goes on up and away
	static void tmCloudSwap(double p, double now, int theme)
	{
		double e = p >= 0 ? -140 + (H + 280) * ease(p) : H + 140 - (H + 280) * ease(-p);
		bool up = p >= 0; double y0 = up ? e : 0, y1 = up ? H : e;
		if (y1 <= y0 - 140) return;
		var gr = g.createLinearGradient(0, 0, 0, H);
		if (theme == 1) { gr.addColorStop(0, "#3a0c16"); gr.addColorStop(0.6, "#5e1222"); gr.addColorStop(1, "#7e1a2a"); }
		else { gr.addColorStop(0, "#ffd9c7"); gr.addColorStop(0.6, "#ffb3b8"); gr.addColorStop(1, "#ff8f8f"); }
		g.save(); g.fillStyle = gr; g.fillRect(0, Math.Max(0, y0), W, Math.Max(0, y1 - Math.Max(0, y0)));
		// soft lighter puffs inside, drifting, so it reads as a thick cloud and not a flat wall
		g.save(); g.beginPath(); g.rect(0, Math.Max(0, y0), W, Math.Max(0, y1 - Math.Max(0, y0))); g.clip(); g.fillStyle = theme == 1 ? "rgba(255,110,130,0.14)" : "rgba(255,240,232,0.35)";
		for (int i = 0; i < 14; i++) { double cx = ((i * 211 + now / 40 * (1 + i % 3)) % (W + 300)) - 150, cy = (i * 149) % H, r = 70 + (i * 29) % 60; g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.arc(cx + r * 0.9, cy + 10, r * 0.75, 0, TAU); g.fill(); }
		g.restore(); g.fillStyle = gr;
		// a row of big puffs along the moving edge, a second softer row behind it
		for (int row = 1; row >= 0; row--)
			for (int i = -1; i < 12; i++)
			{
				double cx = i * 118 + (row * 59) + Math.Sin(now / 900 + i) * 10, r = 80 + ((i * 37 + row * 17) % 40), cy = up ? e + row * 40 + 20 : e - row * 40 - 20;
				g.globalAlpha = row == 0 ? 1 : 0.55; g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.fill();
			}
		g.restore(); g.globalAlpha = 1;
	}

	static void tmDrawBody(TBody p, double now)
	{
		string col = TM_COL[p.i];
		if (p.gate) return;
		if (p.down)
		{
			double k = 0.5 + 0.5 * Math.Sin(now / 180), left = clamp(1 - (tm.t - p.downAt) / TM_REVIVE, 0, 1);
			g.save(); g.translate(p.x, p.y); g.strokeStyle = col; g.lineWidth = 2; g.shadowColor = col; g.shadowBlur = 8 + 8 * k;
			g.strokeRect(-9, -9, 18, 18); g.beginPath(); g.arc(0, 0, 22, -Math.PI / 2, -Math.PI / 2 + TAU * left); g.stroke(); g.shadowBlur = 0;
			if (p.revT > 0) { double f = clamp(p.revT / TM_REV_HOLD, 0, 1); g.fillStyle = col; g.globalAlpha = 0.85; g.fillRect(-9, 9 - 18 * f, 18, 18 * f); g.globalAlpha = 1; }
			g.restore();
			return;
		}
		for (int i = 0; i < p.trail.Count; i++) { var q = p.trail[i]; g.globalAlpha = (double)i / p.trail.Count * 0.3; g.fillStyle = col; g.fillRect(q.x - 6, q.y - 6, 12, 12); }
		g.globalAlpha = (p.hurt > 0 && Math.Floor(p.hurt * 18) % 2 != 0) ? 0.25 : 1;
		g.save(); g.translate(p.x, p.y);
		bool dsh = p.dash > 0; double a = dsh ? JS.atan2(p.ddy, p.ddx) : p.sa, s = dsh ? 1 : p.sk, sx = 1 + 0.75 * s, sy = 1 - 0.38 * s;
		{ double q = Math.PI / 2; double r = a; r -= round(r / q) * q; g.rotate(a - r); g.scale(sx, sy); }
		g.shadowColor = col; g.shadowBlur = 14; g.fillStyle = col; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = "#1b0f26"; g.fillRect(-3, -3, 6, 6);
		g.restore(); g.globalAlpha = 1;
		if (p.i == 2 && p.armed && p.still > 0.08 && tm.th.Any(s => s.k == "flip" && s.d.a == 2))
		{ g.strokeStyle = col; g.lineWidth = 3; g.globalAlpha = 0.8; g.beginPath(); g.arc(p.x, p.y, 17, -Math.PI / 2, -Math.PI / 2 + TAU * Math.Min(1, p.still)); g.stroke(); g.globalAlpha = 1; }
	}

	// ---------- page two: the red storm ----------
	static void tmStormSky(double now, double b, long bi, double bp)
	{
		var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#0c0306"); gr.addColorStop(0.5, "#2a0710"); gr.addColorStop(1, "#4a0d1a");
		g.fillStyle = gr; g.fillRect(0, 0, W, H);
		// far lightning inside the cloud on the bar's first beat (the drum)
		if (bi % 4 == 0 && bp < 0.35) { double a = (1 - bp / 0.35) * 0.35, x = ((bi * 389) % 900 + 900) % 900 + 190; var rg = g.createRadialGradient(x, 120, 20, x, 120, 520); rg.addColorStop(0, $"rgba(255,90,110,{str(a)})"); rg.addColorStop(1, "rgba(255,90,110,0)"); g.fillStyle = rg; g.fillRect(0, 0, W, H); }
		// dark heavy clouds drifting, a red rim underneath
		for (int i = 0; i < 8; i++)
		{
			double sp = 10 + i * 3, x = ((i * 271 + now / 1000 * sp) % (W + 500)) - 250, y = 60 + (i * 89) % 560;
			cloudBlob(x, y, 260 + (i * 67) % 160, 60, "#2a0a12", 0.55);
			g.fillStyle = "rgba(255,60,80,0.06)"; g.fillRect(x + 20, y + 52, 200, 3);
		}
		// rain, slanted
		g.strokeStyle = "rgba(255,170,180,0.12)"; g.lineWidth = 1.5; g.beginPath();
		for (int i = 0; i < 70; i++) { double x = ((i * 97 + now / 1000 * 260) % (W + 200)) - 100, y = ((i * 53 + now / 1000 * 900) % (H + 100)) - 50; g.moveTo(x, y); g.lineTo(x - 7, y + 22); }
		g.stroke();
	}
	// the move that changes a flip cloud: red a jump arrow, cyan a dash, gold an hourglass
	static void tmMoveIcon(int who, double x, double y, double a)
	{
		g.save(); g.globalAlpha = a; g.strokeStyle = TM_COL[who]; g.lineWidth = 2; g.beginPath();
		if (who == 0) { g.moveTo(x, y + 5); g.lineTo(x, y - 5); g.moveTo(x - 4, y - 1); g.lineTo(x, y - 5); g.lineTo(x + 4, y - 1); }
		else if (who == 1) { g.moveTo(x - 7, y); g.lineTo(x + 5, y); g.moveTo(x + 1, y - 4); g.lineTo(x + 5, y); g.lineTo(x + 1, y + 4); g.moveTo(x - 7, y - 3); g.lineTo(x - 3, y - 3); }
		else { g.moveTo(x - 4, y - 5); g.lineTo(x + 4, y - 5); g.lineTo(x - 4, y + 5); g.lineTo(x + 4, y + 5); g.closePath(); }
		g.stroke(); g.restore();
	}
	// the clouds only one player sees: theirs always, everyone's when the bar's lightning lights the room
	static void tmDrawVis(double now)
	{
		foreach (var s in tm.th.Where(s => s.k == "vis"))
		{
			int who = (int)s.d.a; double a = tmView < 0 || tmView == who ? 1 : tm.reveal; if (a <= 0.01) continue;
			string c = TM_COL[who]; g.save(); g.shadowColor = c; g.shadowBlur = 16; cloudBlob(s.X, s.Y - 4, s.W, 24, c, 0.9 * a); g.restore();
			cloudBlob(s.X + 6, s.Y - 1, s.W - 12, 10, "#ffffff", 0.3 * a);
		}
	}
	// the dark: only a light around each player (a fallen one's is small); the gate glows through it
	static void tmDark(double now)
	{
		const double C = 8, R = 115, E = 55; double dark = 0.97 * (1 - 0.88 * tm.reveal);
		var L = tm.b.Where(q => !q.gate).Select(q => (q.x, q.y, r: q.down ? R * 0.45 : R)).ToArray();
		for (double y = 0; y < H; y += C)
		{
			double runX = 0, runA = -1;
			for (double x = 0; x <= W; x += C)
			{
				double a = -1;
				if (x < W)
				{
					double cx = x + C / 2, cy = y + C / 2, d = 1e9;
					foreach (var l in L) d = Math.Min(d, hypot(cx - l.x, cy - l.y) - l.r);
					a = Math.Round(clamp(d / E, 0, 1) * 16) / 16 * dark;
				}
				if (a != runA) { if (runA > 0.01) { g.fillStyle = $"rgba(8,2,5,{str(runA)})"; g.fillRect(runX, y, x - runX + 0.5, C + 0.5); } runX = x; runA = a; }
			}
		}
		for (int y = 0; y < tm.h; y++) for (int x = 0; x < tm.w; x++) if (tm.m[y][x] == 'G')
		{ var rg = g.createRadialGradient(x * PT + PT / 2, y * PT + 12, 4, x * PT + PT / 2, y * PT + 12, 60); rg.addColorStop(0, "rgba(45,226,230,0.55)"); rg.addColorStop(1, "rgba(45,226,230,0)"); g.fillStyle = rg; g.fillRect(x * PT - 40, y * PT - 50, PT + 80, 120); }
	}
	static void tmDrawChase(double now, double bp)
	{
		double x = tm.chaseX; if (x < -40) return;
		g.save(); var gr = g.createLinearGradient(x - 160, 0, x, 0); gr.addColorStop(0, "rgba(120,0,20,0.75)"); gr.addColorStop(1, "rgba(255,40,70,0.45)");
		g.fillStyle = gr; g.fillRect(0, 0, Math.Max(0, x), H);
		g.strokeStyle = "#ffd0d8"; g.shadowColor = "#ff2850"; g.shadowBlur = 22 + 14 * pulse; g.lineWidth = 3; g.beginPath(); g.moveTo(x, 0);
		long seed = (long)(now / 70);
		for (double y = 0; y < H; y += 24) g.lineTo(x + (((long)(y * 13) + seed * 7) % 9 - 4) * 3, y + 24);
		g.stroke(); g.restore();
	}
	// the chain between the three: white while close, red near breaking
	static void tmDrawTether(double now)
	{
		if (tm.snapAt >= 0) return;
		for (int i = 0; i < TM_N; i++) for (int j = i + 1; j < TM_N; j++)
		{
			var p = tm.b[i]; var q = tm.b[j]; if (p.gate || q.gate) continue;
			double f = clamp(hypot(p.x - q.x, p.y - q.y) / tm.P.tether, 0, 1), k = clamp((f - 0.6) / 0.4, 0, 1);
			g.strokeStyle = k > 0 ? $"rgba(255,{str(Math.Round(230 - 190 * k))},{str(Math.Round(240 - 180 * k))},{str(0.25 + 0.6 * k)})" : "rgba(255,230,240,0.22)";
			g.lineWidth = 1.5 + 2.5 * k; g.beginPath(); g.moveTo(p.x, p.y);
			int n = 8; for (int s = 1; s <= n; s++) { double t = (double)s / n, jx = s < n ? Math.Sin(now / 40 + s * 2.1 + i) * (2 + 5 * k) : 0; g.lineTo(p.x + (q.x - p.x) * t + jx, p.y + (q.y - p.y) * t - jx); }
			g.stroke();
		}
	}
	// the two the lightning will swap: a crackling line between them, stronger as the bar's first beat comes
	static void tmDrawSwapWarn(double now, double b)
	{
		var p = tm.b[tm.swapA]; var q = tm.b[tm.swapB]; if (p.down || q.down || p.gate || q.gate) return;
		double left = clamp(tm.swapAt - b, 0, 2), k = 1 - left / 2, fl = (long)(now / 60) % 2 == 0 ? 1 : 0.6;
		g.save(); g.strokeStyle = "#fff0f4"; g.shadowColor = "#ff4060"; g.shadowBlur = 10 + 16 * k; g.lineWidth = 1 + 2.5 * k; g.globalAlpha = (0.25 + 0.7 * k) * fl; g.beginPath(); g.moveTo(p.x, p.y);
		int n = 10; for (int s = 1; s <= n; s++) { double t = (double)s / n, j = s < n ? ((((long)(now / 50) + s * 7) % 5) - 2) * (3 + 5 * k) : 0; g.lineTo(p.x + (q.x - p.x) * t + j, p.y + (q.y - p.y) * t + j); }
		g.stroke();
		foreach (var r in new[] { p, q }) { g.beginPath(); g.arc(r.x, r.y, 16 + 6 * (1 - k), 0, TAU); g.stroke(); }
		g.restore();
	}
}
