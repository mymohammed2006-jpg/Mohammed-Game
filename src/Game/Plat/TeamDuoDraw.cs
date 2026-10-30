using System;
using System.Linq;
using static MG.JS;

namespace MG;

// drawing the radio level: a dark studio of steel and speaker grilles seen up close (each one's camera follows their own
// square), the song's noise over the screen too (it clears with the song)
public static partial class G
{
	static double[] duoCx = { double.NaN, double.NaN }, duoCy = { double.NaN, double.NaN }, duoLast = { 0, 0 };
	static double duoZ;   // the camera now: world px x duoZ = screen px
	// where the camera looks (the middle of the screen, in world px): it follows the square softly and stays inside the map
	static (double x, double y, double z) duoCam(double now)
	{
		double mw = tm.w * PT, mh = tm.h * PT;
		if (tmView == -2) return lcDuoCam(now);   // local play: one camera for both (Plat/LocalTeam.cs)
		if (tmView < 0 || tm.P.zoom <= 0) { double z0 = Math.Min(W / mw, H / mh); return (mw / 2, mh / 2, z0); }
		double z = tm.P.zoom, vw = W / z, vh = H / z; var p = tm.b[Math.Min(tmView, tm.b.Length - 1)];
		double tx = p.x + p.fx * 40, ty = p.y - 30;
		int v = Math.Min(tmView, 1); double dt = Math.Clamp((now - duoLast[v]) / 1000, 0, 0.1); duoLast[v] = now;
		if (double.IsNaN(duoCx[v]) || hypot(tx - duoCx[v], ty - duoCy[v]) > 900) { duoCx[v] = tx; duoCy[v] = ty; }
		else { double k = 1 - Math.Exp(-dt * 7); duoCx[v] += (tx - duoCx[v]) * k; duoCy[v] += (ty - duoCy[v]) * k; }
		double cx = mw <= vw ? mw / 2 : Math.Clamp(duoCx[v], vw / 2, mw - vw / 2), cy = mh <= vh ? mh / 2 : Math.Clamp(duoCy[v], vh / 2, mh - vh / 2);
		return (cx, cy, z);
	}
	public static void duoCamReset() { duoCx[0] = duoCx[1] = duoCy[0] = duoCy[1] = double.NaN; }

	public static void drawDuo(double now)
	{
		double b = tmBeat(), bp = ((b % 1) + 1) % 1; pulse = JS.exp(-bp * 5) * 0.8; long bi = (long)Math.Floor(b);
		var (cx, cy, z) = duoCam(now); duoZ = z;
		// the sky behind: deep navy, a slow glow that breathes on the beat
		{
			var gr = g.createLinearGradient(0, 0, 0, H); gr.addColorStop(0, "#05070f"); gr.addColorStop(0.6, "#0b1022"); gr.addColorStop(1, "#141a33");
			g.fillStyle = gr; g.fillRect(0, 0, W, H);
			var rg = g.createRadialGradient(W / 2, H * 0.55, 40, W / 2, H * 0.55, 700); rg.addColorStop(0, $"rgba(90,120,255,{str(0.07 + 0.05 * pulse)})"); rg.addColorStop(1, "rgba(90,120,255,0)");
			g.fillStyle = rg; g.fillRect(0, 0, W, H);
			// far equaliser bars, moving slowly with the camera (depth)
			g.fillStyle = "rgba(120,150,255,0.05)";
			for (int i = 0; i < 26; i++)
			{
				double bx = ((i * 61 - cx * 0.25 * z) % (W + 80) + W + 80) % (W + 80) - 40, hh = 60 + 140 * Math.Abs(Math.Sin(i * 1.7 + bi * 0.9 + (i % 3) * pulse));
				g.fillRect(bx, H - hh, 26, hh);
			}
		}
		g.save(); g.translate(W / 2, H / 2); g.scale(z, z); g.translate(-cx, -cy);
		double vx0 = cx - W / 2 / z - PT, vx1 = cx + W / 2 / z + PT, vy0 = cy - H / 2 / z - PT, vy1 = cy + H / 2 / z + PT;
		int tx0 = Math.Max(0, (int)(vx0 / PT)), tx1 = Math.Min(tm.w - 1, (int)(vx1 / PT)), ty0 = Math.Max(0, (int)(vy0 / PT)), ty1 = Math.Min(tm.h - 1, (int)(vy1 / PT));
		// walls: dark steel with a cold edge
		g.fillStyle = "#1a2346";
		for (int y = ty0; y <= ty1; y++) for (int x = tx0; x <= tx1; x++) if (tm.m[y][x] == '#') g.fillRect(x * PT, y * PT, PT + 0.5, PT + 0.5);
		g.fillStyle = "rgba(160,185,255,0.09)";
		for (int y = ty0; y <= ty1; y++) for (int x = tx0; x <= tx1; x++) if (tm.m[y][x] == '#' && (x + y) % 2 == 0) { g.fillRect(x * PT + 8, y * PT + 8, 4, 4); g.fillRect(x * PT + 26, y * PT + 24, 4, 4); }
		g.strokeStyle = $"rgba(170,195,255,{str(0.35 + 0.3 * pulse)})"; g.lineWidth = 2; g.shadowColor = "#7f9cff"; g.shadowBlur = 8; g.beginPath();
		for (int y = ty0; y <= ty1; y++) for (int x = tx0; x <= tx1; x++)
		{
			if (tm.m[y][x] != '#') continue; double X = x * PT, Y = y * PT;
			if (y > 0 && tm.m[y - 1][x] != '#') { g.moveTo(X, Y + 1); g.lineTo(X + PT, Y + 1); }
			if (y < tm.h - 1 && tm.m[y + 1][x] != '#') { g.moveTo(X, Y + PT - 1); g.lineTo(X + PT, Y + PT - 1); }
			if (x > 0 && tm.m[y][x - 1] != '#') { g.moveTo(X + 1, Y); g.lineTo(X + 1, Y + PT); }
			if (x < tm.w - 1 && tm.m[y][x + 1] != '#') { g.moveTo(X + PT - 1, Y); g.lineTo(X + PT - 1, Y + PT); }
		}
		g.stroke(); g.shadowBlur = 0;
		// the things under the players
		foreach (var s in tm.th)
		{
			if (s.X > vx1 || s.X + s.W < vx0 || s.Y > vy1 || s.Y + s.H + 20 < vy0) continue;
			if (s.k == "tower") duoTower(s, b, now);
			else if (s.k == "cloud") duoGrille(s.X, s.Y, s.W, "#c9d4ff", 1);
			else if (s.k == "spk") duoSpeaker(s, now);
			else if (s.k == "cwall")
			{
				// a curtain only its own one walks through: their colour, waving
				string c = tmCol((int)s.d.a); g.save(); g.globalAlpha = 0.14; g.fillStyle = c; g.fillRect(s.X, s.Y, s.W, s.H); g.globalAlpha = 0.7;
				g.strokeStyle = c; g.shadowColor = c; g.shadowBlur = 10; g.lineWidth = 2; g.beginPath();
				for (int i = 0; i < 3; i++) { double x = s.X + 3 + i * (s.W - 6) / 2; g.moveTo(x, s.Y); for (double y = s.Y; y < s.Y + s.H; y += 10) g.lineTo(x + Math.Sin(y / 9 + now / 150 + i) * 3, y + 10); }
				g.stroke(); g.restore();
			}
			else if (s.k == "cbtn")
			{
				// a key: only its own one presses it (their colour), and it drives the other one's things (lit while pressed)
				string c = tmCol((int)s.d.a); double dn = s.on ? 4 : 0; g.save(); g.fillStyle = c; g.globalAlpha = s.on ? 1 : 0.6; g.shadowColor = c; g.shadowBlur = s.on ? 22 + 8 * pulse : 6;
				g.fillRect(s.X + 4, s.Y - 9 + dn, s.W - 8, 9 - dn); g.shadowBlur = 0; g.globalAlpha = s.on ? 0.9 : 0.4; g.fillStyle = "#05070f";
				for (int i = 0; i < 3; i++) g.fillRect(s.X + s.W / 2 - 7 + i * 5, s.Y - 7 + dn + (2 - i) * 1.5, 3, 5 - (2 - i) * 1.5);   // the signal bars
				g.restore();
			}
			else if (s.k == "path" || s.k == "mover")
			{
				// a step the other one's key holds: in the key's colour; while it is not held, only a dotted outline
				string c = duoKeyCol(s.d); double k = s.k == "mover" ? (s.d.need == null || tmNeed(s.d) ? 1 : 0.6) : s.s;
				if (s.k == "path" && s.solid && !tmNeed(s.d) && Math.Floor(s.timer * 10) % 2 == 0) k *= 0.3;   // let go: it blinks out
				if (k > 0.02) { g.save(); g.globalAlpha = k; g.shadowColor = c; g.shadowBlur = 14; duoGrille(s.X, s.Y, s.W, c, 1); g.restore(); }
				if (k < 0.98 && s.k == "path") { g.save(); g.strokeStyle = c; g.globalAlpha = 0.45 * (1 - k); g.lineWidth = 2; g.setLineDash(new double[] { 5, 7 }); g.strokeRect(s.X + 2, s.Y, s.W - 4, 10); g.setLineDash(new double[0]); g.restore(); }
				if (s.k == "mover") { g.save(); g.strokeStyle = c; g.globalAlpha = 0.25; g.lineWidth = 2; g.setLineDash(new double[] { 3, 8 }); double rx = s.X + s.W / 2; g.beginPath(); g.moveTo(rx, s.d.y * PT); g.lineTo(rx, s.d.y1 * PT); g.stroke(); g.setLineDash(new double[0]); g.restore(); }
			}
			else if (s.k == "door")
			{
				// a door the other one's key opens: bars in the key's colour that slide away
				string c = duoKeyCol(s.d); double open = s.s;
				if (!s.solid && !tmNeed(s.d) && s.timer > 0 && Math.Floor(s.timer * 10) % 2 == 0) { g.save(); g.strokeStyle = c; g.globalAlpha = 0.7; g.lineWidth = 2; g.setLineDash(new double[] { 5, 6 }); g.strokeRect(s.X, s.Y, s.W, s.H); g.setLineDash(new double[0]); g.restore(); }   // let go: about to shut
				if (open > 0.98) continue;
				g.save(); g.globalAlpha = 1 - open; g.fillStyle = "#101634"; g.fillRect(s.X, s.Y, s.W, s.H * (1 - open));
				g.strokeStyle = c; g.shadowColor = c; g.shadowBlur = 10; g.lineWidth = 3; g.strokeRect(s.X, s.Y, s.W, s.H * (1 - open));
				g.fillStyle = c; for (double y = s.Y + 12; y < s.Y + s.H * (1 - open) - 6; y += 22) g.fillRect(s.X + 3, y, s.W - 6, 4);
				g.restore();
			}
			else if (s.k == "echo" && s.timer > 0)
			{
				double k = Math.Min(1, s.timer / 0.3), blink = s.timer < 0.3 && Math.Floor(s.timer * 20) % 2 == 0 ? 0.35 : 1;
				string c = tmCol((int)s.d.a); g.save(); g.globalAlpha = k * blink; g.shadowColor = c; g.shadowBlur = 18; duoGrille(s.X, s.Y, s.W, c, 1); g.restore();
				// equaliser ticks over it, jumping with the lit time
				g.save(); g.globalAlpha = 0.6 * k * blink; g.fillStyle = c;
				int n = Math.Max(2, (int)(s.W / 8)); for (int i = 0; i < n; i++) { double hh = 3 + 7 * Math.Abs(Math.Sin(i * 2.3 + now / 90)); g.fillRect(s.X + 3 + i * (s.W - 6) / n, s.Y - hh - 1, 3, hh); }
				g.restore();
			}
		}
		// spikes (the only thing that hurts here: pink)
		for (int y = ty0; y <= ty1; y++) for (int x = tx0; x <= tx1; x++)
		{
			char c = tm.m[y][x]; double X = x * PT, Y = y * PT;
			if (c != '^' && c != 'v') continue;
			g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 8 + 8 * pulse; g.beginPath();
			for (int i = 0; i < 3; i++) { double sx = X + i * PT / 3, h = PT * (0.5 + 0.08 * pulse); if (c == '^') { g.moveTo(sx, Y + PT); g.lineTo(sx + PT / 6, Y + PT - h); g.lineTo(sx + PT / 3, Y + PT); } else { g.moveTo(sx, Y); g.lineTo(sx + PT / 6, Y + h); g.lineTo(sx + PT / 3, Y); } }
			g.fill(); g.shadowBlur = 0;
		}
		// the sounds to take and the gate
		int total = tmPieces();
		for (int y = ty0; y <= ty1; y++) for (int x = tx0; x <= tx1; x++)
		{
			char c = tm.m[y][x]; double X = x * PT + PT / 2, Y = y * PT + PT / 2;
			if (c >= '1' && c <= '9' && !tm.got.Contains(x + "," + y)) duoSound(X, Y + 4 * JS.sin(b * Math.PI / 2), now);
			else if (c == 'G')
			{
				bool open = tmOpen(); double gy = Y - 8;
				if (tm.P.mic) { duoMic(X, Y, open, now); continue; }
				g.save(); g.translate(X, gy);
				if (open) { g.rotate(now / 600); g.strokeStyle = "#ffffff"; g.shadowColor = "#9fb4ff"; g.shadowBlur = 16 + 10 * pulse; g.lineWidth = 4; g.beginPath(); g.arc(0, 0, 26 + 3 * pulse, 0, TAU * 0.8); g.stroke(); g.shadowBlur = 0; }
				else { g.strokeStyle = "rgba(255,255,255,0.3)"; g.lineWidth = 3; g.setLineDash(new double[] { 6, 6 }); g.beginPath(); g.arc(0, 0, 26, 0, TAU); g.stroke(); g.setLineDash(new double[0]); }
				g.restore();
				for (int i = 0; i < tm.b.Length; i++) { g.fillStyle = tm.b[i].gate ? tmCol(i) : "rgba(255,255,255,0.18)"; g.fillRect(X - 13 + i * 16, gy + 34, 10, 10); }
				if (!open) txt($"{ar(tm.got.Count)}/{ar(total)}", X, gy + 2, 16, "rgba(255,255,255,0.55)");
			}
		}
		// the waves out of the jumps
		foreach (var w in tm.waves)
		{
			double f = Math.Min(1, w.r / Math.Max(1, w.R)), a = w.miss ? 0.35 * (1 - f) : 0.9 * (1 - f * f);
			if (a <= 0.01) continue; string c = w.miss ? "#8a90a8" : w.who < 0 ? "#ffffff" : tmCol(w.who);
			g.save(); g.globalAlpha = a; g.strokeStyle = c; g.shadowColor = c; g.shadowBlur = w.miss ? 0 : 14; g.lineWidth = w.miss ? 2 : 3;
			if (w.miss) g.setLineDash(new double[] { 4, 6 });
			g.beginPath(); g.arc(w.x, w.y, Math.Min(w.r, w.R), 0, TAU); g.stroke();
			if (!w.miss && w.r > 30) { g.globalAlpha = a * 0.4; g.lineWidth = 1.5; g.beginPath(); g.arc(w.x, w.y, Math.Min(w.r, w.R) - 22, 0, TAU); g.stroke(); }
			g.restore();
		}
		// the speakers' waves (pink: they hurt), row by row (rock and the shield stop rows)
		foreach (var w in tm.bars)
		{
			g.save(); g.fillStyle = PINK; g.strokeStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 16 + 8 * pulse;
			for (int r = 0; r < w.on.Length; r++)
			{
				if (!w.on[r]) continue; double y0 = (w.r0 + r) * PT;
				g.globalAlpha = 0.95; g.fillRect(w.x - BAR_W / 2, y0 + 1, BAR_W, PT - 2);
				g.globalAlpha = 0.5; g.lineWidth = 3;   // the sound's rings behind it
				for (int k = 1; k <= 2; k++) { double bx = w.x - w.dir * (BAR_W / 2 + 9 * k); g.beginPath(); g.moveTo(bx, y0 + 6); g.quadraticCurveTo(bx + w.dir * 7, y0 + PT / 2, bx, y0 + PT - 6); g.stroke(); }
			}
			g.restore();
		}
		// the saws (pink: they hurt)
		foreach (var sw in tm.P.saws)
		{
			double sx = tmSawPos(sw, out double sy); if (sx < vx0 - 40 || sx > vx1 + 40 || sy < vy0 - 40 || sy > vy1 + 40) continue;
			g.save(); g.translate(sx, sy); g.rotate(now / 120); g.fillStyle = PINK; g.shadowColor = PINK; g.shadowBlur = 12 + 8 * pulse; g.beginPath();
			int teeth = 10; for (int i = 0; i < teeth * 2; i++) { double a = i * Math.PI / teeth, r2 = i % 2 == 0 ? sw.r : sw.r * 0.72; if (i == 0) g.moveTo(JS.cos(a) * r2, JS.sin(a) * r2); else g.lineTo(JS.cos(a) * r2, JS.sin(a) * r2); }
			g.closePath(); g.fill(); g.shadowBlur = 0; g.fillStyle = "#05070f"; g.beginPath(); g.arc(0, 0, sw.r * 0.25, 0, TAU); g.fill(); g.restore();
		}
		foreach (var q in particles) { g.globalAlpha = Math.Max(0, q.life / q.max); g.fillStyle = q.col; g.fillRect(q.x - q.s / 2, q.y - q.s / 2, q.s, q.s); }
		g.globalAlpha = 1;
		// the beat: a ring closing in on each square, on the beat it touches it (the jump that sends a wave)
		if (tm.P.things.Any(d => d.k == "echo"))
			foreach (var p in tm.b)
			{
				if (p.down || p.gate || (tmView >= 0 && p.i != tmView)) continue;
				double f = bp, r = 12 + 30 * (1 - f), a = 0.12 + 0.55 * f * f;
				g.strokeStyle = tmCol(p.i); g.globalAlpha = a; g.lineWidth = 2; g.beginPath(); g.arc(p.x, p.y, r, 0, TAU); g.stroke(); g.globalAlpha = 1;
			}
		if (!double.IsNaN(tm.riseY)) duoRise(now, vx0, vx1, vy1);
		if (tm.P.rope > 0 && tm.ropeLive) duoCable(now);
		foreach (var p in tm.b) tmDrawBody(p, now);
		if (tm.spkN != null) duoShield(now);
		if (tmMe >= 0 || lcT) foreach (var p in tm.b) if (p.i != tmMe && !p.gate) { g.globalAlpha = 0.75; txt(tm.names[p.i], p.x, p.y - 30 - lcNameUp(p), 16, tm.skins[p.i] != "" ? tmCol(p.i) : "#fff", 700); g.globalAlpha = 1; }
		g.restore();
		// the song's noise on the screen too, as much as the song is garbled
		duoStatic(now, tm.murk);
		// where the other one is, when they are off the screen
		if (tmView >= 0 && tm.P.zoom > 0) foreach (var p in tm.b) if (p.i != tmView && !p.gate) duoArrow(p, cx, cy, z, now);
		// HUD: each one's heart, the radio's signal
		for (int i = 0; i < tm.b.Length; i++)
		{
			var p = tm.b[i]; double y0 = 26 + i * 26, x = 34;
			if (!p.down && p.hp > 0) { g.fillStyle = tmCol(i); g.fillRect(x - 8, y0 - 8, 16, 16); }
			else { g.strokeStyle = tmCol(i); g.globalAlpha = 0.35; g.lineWidth = 2; g.strokeRect(x - 7, y0 - 7, 14, 14); g.globalAlpha = 1; }
		}
		duoSignal(W / 2, 30, 1 - tm.murk, now);
		if (tm.msgT > 0) { g.globalAlpha = Math.Min(1, tm.msgT); txt(tm.msg, W / 2, 74, 30, "#ffffff"); g.globalAlpha = 1; }
		if (tm.restartAt >= 0) { double a = clamp(1 - (tm.restartAt - tm.t) / 0.9, 0, 1); g.fillStyle = $"rgba(4,6,14,{str(a * 0.85)})"; g.fillRect(0, 0, W, H); }
		if (tm.won) tmDrawWin(now);
		else if (tm.doneAt >= 0) tmCloudSwap(1 - clamp((tm.t - tm.doneAt) / (TM_SWAP * 0.85), 0, 1), now, 3);
		else if (tm.fromClouds && tm.pageAt < TM_SWAP) tmCloudSwap(-clamp(tm.pageAt / TM_SWAP, 0, 1), now, 3);
	}

	// the radio mast: a lattice with four lamps (the page's sounds), and over the pads the phrase it sings and how it is answered
	static void duoTower(TS s, double b, double now)
	{
		var w = tm.tw; double mx = s.X + s.W / 2, top = s.Y, bot = s.Y + s.H; int got = tm.got.Count, need = Math.Max(1, tmPieces());
		bool ready = twReady;
		g.save(); g.strokeStyle = "#8fa6e8"; g.lineWidth = 2; g.globalAlpha = 0.85; g.beginPath();
		g.moveTo(mx - 22, bot); g.lineTo(mx - 4, top + 20); g.moveTo(mx + 22, bot); g.lineTo(mx + 4, top + 20);
		for (double y = bot; y > top + 30; y -= 34) { double k = (y - top) / (bot - top), hw = 4 + 18 * k, hw2 = 4 + 18 * ((y - 34 - top) / (bot - top)); g.moveTo(mx - hw, y); g.lineTo(mx + hw2, y - 34); g.moveTo(mx + hw, y); g.lineTo(mx - hw2, y - 34); }
		g.moveTo(mx, top + 20); g.lineTo(mx, top - 10); g.stroke(); g.restore();
		for (int i = 0; i < need; i++)
		{
			double ly = bot - 40 - i * (s.H - 70) / need; bool on = i < got;
			g.fillStyle = on ? "#ffffff" : "rgba(255,255,255,0.15)"; g.shadowColor = "#bcd0ff"; g.shadowBlur = on ? 14 : 0; g.beginPath(); g.arc(mx, ly, 6, 0, TAU); g.fill(); g.shadowBlur = 0;
		}
		// the top: dark until all the sounds are in, then it beats
		double tp = ready ? 0.6 + 0.4 * pulse : 0.15; g.fillStyle = $"rgba(255,255,255,{str(tp)})"; g.shadowColor = "#9fb4ff"; g.shadowBlur = ready ? 24 : 0; g.beginPath(); g.arc(mx, top - 12, 7, 0, TAU); g.fill(); g.shadowBlur = 0;
		if (!ready || w == null) return;
		if (ready && w.doneT < 0) for (int k = 1; k <= 2; k++) { double r = 20 + ((now / 900 + k * 0.5) % 1) * 60; g.strokeStyle = $"rgba(220,230,255,{str(0.4 * (1 - (r - 20) / 60))})"; g.lineWidth = 2; g.beginPath(); g.arc(mx, top - 12, r, -2.4, -0.7); g.stroke(); }
		if (w.doneT >= 0 || w.ph >= twPh.Length) return;
		// the phrase: slots in a row over the pads
		string ph = twPh[w.ph]; int n = ph.Length; double sw = 30, gap = 8, x0 = s.X + s.W + 80 - (n * sw + (n - 1) * gap) / 2, y0 = top - 44;
		long a0 = w.callAt + n; bool call = w.callAt >= 0 && b >= w.callAt && b < a0 + 0 - 0.0, ans = w.callAt >= 0 && b >= a0 - ECHO_WIN;
		double fail = clamp(1 - (tm.t - w.failT) / 0.8, 0, 1);
		for (int i = 0; i < n; i++)
		{
			double x = x0 + i * (sw + gap); char c = ph[i]; bool shown = ans || (call && b >= w.callAt + i), cur = (call && Math.Floor(b) == w.callAt + i) || (ans && i == w.at && b < a0 + n);
			double a = shown ? (cur ? 1 : 0.75) : 0.2, grow = cur ? 4 * pulse : 0;
			g.save(); g.globalAlpha = a * (1 - 0.6 * fail);
			if (!shown) { g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 2; g.strokeRect(x, y0, sw, sw); }
			else if (c == '.') { g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 2; g.setLineDash(new double[] { 3, 4 }); g.strokeRect(x, y0, sw, sw); g.setLineDash(new double[0]); }
			else
			{
				string c0 = c == 'G' ? tmCol(1) : tmCol(0), c1 = c == 'B' ? tmCol(0) : tmCol(1);
				g.shadowColor = c0; g.shadowBlur = cur ? 18 : 6; g.fillStyle = c0; g.fillRect(x - grow, y0 - grow, c == 'X' ? sw / 2 + grow : sw + 2 * grow, sw + 2 * grow);
				if (c == 'X') { g.fillStyle = c1; g.fillRect(x + sw / 2, y0 - grow, sw / 2 + grow, sw + 2 * grow); }
				g.shadowBlur = 0;
			}
			// answered: a white tick on it
			if (ans && i < w.at) { g.globalAlpha = 1; g.strokeStyle = "#ffffff"; g.lineWidth = 3; g.beginPath(); g.moveTo(x + 7, y0 + 16); g.lineTo(x + 13, y0 + 22); g.lineTo(x + 24, y0 + 8); g.stroke(); }
			if (cur && ans) { g.globalAlpha = 0.9; g.strokeStyle = "#ffffff"; g.lineWidth = 2; g.strokeRect(x - 6, y0 - 6, sw + 12, sw + 12); }
			g.restore();
		}
		// which phrase of how many, and listen / answer
		for (int i = 0; i < twPh.Length; i++) { g.fillStyle = i < w.ph ? "#ffffff" : "rgba(255,255,255,0.2)"; g.beginPath(); g.arc(x0 + (n * sw + (n - 1) * gap) / 2 + (i - (twPh.Length - 1) / 2.0) * 16, y0 + sw + 16, 4, 0, TAU); g.fill(); }
		if (fail > 0) { g.save(); g.globalAlpha = fail; g.strokeStyle = "#c8cede"; g.lineWidth = 4; double cx = x0 + (n * sw + (n - 1) * gap) / 2, cy = y0 + sw / 2; g.beginPath(); g.moveTo(cx - 14, cy - 14); g.lineTo(cx + 14, cy + 14); g.moveTo(cx + 14, cy - 14); g.lineTo(cx - 14, cy + 14); g.stroke(); g.restore(); }
	}
	// a speaker grille to stand on
	static void duoGrille(double x, double y, double w, string col, double a)
	{
		g.save(); g.globalAlpha *= a; g.fillStyle = "#1a2240"; rr(x, y, w, 12, 4); g.fill();
		g.fillStyle = col; g.fillRect(x + 2, y, w - 4, 3);
		g.globalAlpha *= 0.55; for (double px = x + 6; px < x + w - 4; px += 7) { g.fillRect(px, y + 6, 3, 3); }
		g.restore();
	}
	// a sound to take: a small speaker with its waves, white
	// the last page's static coming up from below (pink: it hurts): a torn bright edge, the noise under it
	static void duoRise(double now, double vx0, double vx1, double vy1)
	{
		double y = tm.riseY; if (y > vy1) return; long fr = (long)(now / 60);
		g.save(); g.fillStyle = PINK; g.globalAlpha = 0.5; g.fillRect(vx0, y, vx1 - vx0, vy1 - y);
		g.globalAlpha = 0.85; g.fillRect(vx0, y + 26, vx1 - vx0, vy1 - y);
		// the edge: jagged, jumping each frame like a bad signal
		g.globalAlpha = 1; g.shadowColor = PINK; g.shadowBlur = 18; g.beginPath(); g.moveTo(vx0, y + 30);
		for (double x = Math.Floor(vx0 / 16) * 16; x <= vx1 + 16; x += 16) g.lineTo(x, y + rnd(fr, (long)(x / 16)) * 14 - 4);
		g.lineTo(vx1, y + 30); g.closePath(); g.fill(); g.shadowBlur = 0;
		g.fillStyle = "#ffd1e6";
		for (int i = 0; i < 90; i++) { double sx = vx0 + rnd(fr, i * 3) * (vx1 - vx0), sy = y + 4 + rnd(fr, i * 3 + 1) * Math.Min(220, vy1 - y); g.globalAlpha = 0.35 + 0.5 * rnd(fr, i * 3 + 2); g.fillRect(sx, sy, 3 + 9 * rnd(fr, i), 2); }
		g.restore();
	}
	static double rnd(long a, long b) { ulong h = (ulong)(a * 73856093 ^ b * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h % 10007) / 10007.0; }
	// the last page's end: a microphone on a stand; lit once all four sounds are in, and the two squares under it
	static void duoMic(double X, double Y, bool open, double now)
	{
		double by = Y + PT / 2, k = 0.5 + 0.5 * Math.Sin(now / 200);
		g.save(); g.strokeStyle = open ? "#ffffff" : "rgba(255,255,255,0.35)"; g.lineWidth = 3;
		g.beginPath(); g.moveTo(X - 14, by); g.lineTo(X + 14, by); g.moveTo(X, by); g.lineTo(X, by - 34); g.stroke();
		if (open) { g.shadowColor = "#9fb4ff"; g.shadowBlur = 18 + 12 * k; }
		g.fillStyle = open ? "#ffffff" : "rgba(255,255,255,0.25)"; rr(X - 9, by - 62, 18, 30, 9); g.fill(); g.shadowBlur = 0;
		g.strokeStyle = open ? "#5a6ab0" : "rgba(10,14,30,0.6)"; g.lineWidth = 1.5;
		for (int i = 0; i < 3; i++) { g.beginPath(); g.moveTo(X - 7, by - 54 + i * 7); g.lineTo(X + 7, by - 54 + i * 7); g.stroke(); }
		if (open) { g.strokeStyle = "#ffffff"; g.lineWidth = 2.5; for (int i = 1; i <= 2; i++) { g.globalAlpha = 0.4 + 0.4 * ((k + i * 0.4) % 1); g.beginPath(); g.arc(X, by - 47, 14 + i * 9, -0.7, 0.7); g.stroke(); g.beginPath(); g.arc(X, by - 47, 14 + i * 9, Math.PI - 0.7, Math.PI + 0.7); g.stroke(); } }
		g.restore();
		for (int i = 0; i < tm.b.Length; i++) { g.fillStyle = tm.b[i].gate ? tmCol(i) : "rgba(255,255,255,0.18)"; g.fillRect(X - 13 + i * 16, by + 8, 10, 10); }
		if (!open) txt($"{ar(tm.got.Count)}/{ar(tmPieces())}", X, by - 80, 16, "rgba(255,255,255,0.55)");
	}
	// the win: the microphone big in the middle, singing, then each one's card under it
	static void duoDrawWin(double now)
	{
		double f = tm.t - tm.wonAt, a = clamp(f / 0.8, 0, 1), z = ease(clamp(f / 1.2, 0, 1));
		g.fillStyle = $"rgba(6,6,20,{str(0.82 * a)})"; g.fillRect(0, 0, W, H);
		// sound rings out of it, on the song's beat
		double b = tmBeat(), bf = b - Math.Floor(b);
		g.save(); g.strokeStyle = "#ffffff"; g.lineWidth = 3;
		for (int k = 0; k < 3; k++) { double r = 60 + ((bf + k / 3.0) % 1) * 220; g.globalAlpha = a * 0.35 * (1 - ((bf + k / 3.0) % 1)); g.beginPath(); g.arc(W / 2, 150, r, 0, TAU); g.stroke(); }
		g.restore();
		g.save(); g.globalAlpha = a; g.translate(W / 2, 150 + 40 * (1 - z)); g.scale(2.6, 2.6); duoMic(0, -PT / 2 + 34, true, now); g.restore();
		if (f < 0.6) { g.fillStyle = $"rgba(255,255,255,{str(0.8 * (1 - f / 0.6))})"; g.fillRect(0, 0, W, H); }
		tmDrawResults(now, f - 1.6, 314);
		for (int i = 0; i < 40; i++)
		{
			double sx = (i * 97 + 30) % W, sy = ((f * (60 + i % 5 * 20) + i * 53) % (H + 40)) - 20, r = 2 + i % 3;
			g.fillStyle = $"rgba(255,255,255,{str(clamp(f / 2, 0, 0.6) * (0.5 + 0.5 * Math.Sin(now / 200 + i)))})"; g.fillRect(sx - r / 2, sy - r / 2, r, r);
		}
	}
	static void duoSound(double x, double y, double now)
	{
		g.save(); g.translate(x, y); double k = 0.5 + 0.5 * Math.Sin(now / 160);
		g.shadowColor = "#bcd0ff"; g.shadowBlur = 16 + 8 * k; g.fillStyle = "#ffffff"; g.strokeStyle = "#ffffff"; g.lineWidth = 2.5;
		g.beginPath(); g.moveTo(-10, -5); g.lineTo(-4, -5); g.lineTo(3, -11); g.lineTo(3, 11); g.lineTo(-4, 5); g.lineTo(-10, 5); g.closePath(); g.fill();
		for (int i = 1; i <= 2; i++) { g.globalAlpha = 0.5 + 0.5 * ((k + i * 0.3) % 1); g.beginPath(); g.arc(4, 0, 6 + i * 6, -0.8, 0.8); g.stroke(); }
		g.restore();
	}
	// the noise over the screen: specks and a torn line now and then; none when the song is clear
	static void duoStatic(double now, double m)
	{
		if (m <= 0.01) return; long fr = (long)(now / 50);
		int n = (int)(260 * m);
		for (int i = 0; i < n; i++)
		{
			double h1 = duoHash(fr * 977 + i * 13), h2 = duoHash(fr * 131 + i * 7 + 3), h3 = duoHash(fr * 17 + i);
			g.fillStyle = h3 < 0.5 ? $"rgba(220,228,255,{str(0.10 + 0.12 * m)})" : $"rgba(0,0,0,{str(0.25 * m)})"; g.fillRect(h1 * W, h2 * H, 2 + h3 * 3, 2);
		}
		if (duoHash(fr * 3 + 1) < 0.18 * m) { double y = duoHash(fr) * H; g.fillStyle = $"rgba(200,210,255,{str(0.06 + 0.08 * m)})"; g.fillRect(0, y, W, 3 + 10 * duoHash(fr + 9)); }
		g.fillStyle = $"rgba(0,0,0,{str(0.05 * m)})"; for (double y = (now / 30) % 4; y < H; y += 4) g.fillRect(0, y, W, 1);
	}
	static double duoHash(double x) { double s = Math.Sin(x * 12.9898 + 78.233) * 43758.5453; return s - Math.Floor(s); }
	// the radio's signal: five bars, lit as much as the song is clear
	static void duoSignal(double x, double y, double clear, double now)
	{
		for (int i = 0; i < 5; i++)
		{
			double f = clamp(clear * 5 - i, 0, 1), h = 8 + i * 5, bx = x - 40 + i * 18;
			g.fillStyle = "rgba(255,255,255,0.15)"; g.fillRect(bx, y + 12 - h, 10, h);
			if (f > 0) { g.fillStyle = "#ffffff"; g.shadowColor = "#9fb4ff"; g.shadowBlur = 10; g.fillRect(bx, y + 12 - h * f, 10, h * f); g.shadowBlur = 0; }
		}
	}
	// an arrow on the screen's edge pointing at the other one (in their colour)
	static void duoArrow(TBody p, double cx, double cy, double z, double now)
	{
		double sx = W / 2 + (p.x - cx) * z, sy = H / 2 + (p.y - cy) * z;
		if (sx > 20 && sx < W - 20 && sy > 20 && sy < H - 20) return;
		double dx = sx - W / 2, dy = sy - H / 2, k = Math.Min((W / 2 - 34) / Math.Max(1e-6, Math.Abs(dx)), (H / 2 - 34) / Math.Max(1e-6, Math.Abs(dy)));
		double ax = W / 2 + dx * k, ay = H / 2 + dy * k, a = JS.atan2(dy, dx); string c = tmCol(p.i);
		g.save(); g.translate(ax, ay); g.rotate(a); g.fillStyle = c; g.shadowColor = c; g.shadowBlur = 12; g.globalAlpha = p.down ? 0.5 + 0.5 * Math.Sin(now / 120) : 0.9;
		g.beginPath(); g.moveTo(16, 0); g.lineTo(-8, -11); g.lineTo(-3, 0); g.lineTo(-8, 11); g.closePath(); g.fill(); g.restore();
	}
	// the cable between the two: it sags when slack, pulls straight when tight (a light runs along it then), half each one's colour
	static void duoCable(double now)
	{
		var a = tm.b[0]; var c = tm.b[1]; if (a.down || c.down || a.gate || c.gate) return;
		double L = tm.P.rope, len = tmRopePath(a, c, out double px, out double py), tight = Math.Clamp((len - (L - 30)) / 30, 0, 1);
		bool piv = !double.IsNaN(px);
		// a bend over the edge: two straight parts; else one curve hanging as much as the cable is slack
		double sag = piv ? 0 : Math.Sqrt(Math.Max(0, L * L - len * len)) * 0.45, mx = piv ? px : (a.x + c.x) / 2, my = piv ? py : (a.y + c.y) / 2 + sag;
		(double, double) at(double t) => piv ? (t < 0.5 ? (a.x + (px - a.x) * t * 2, a.y + (py - a.y) * t * 2) : (px + (c.x - px) * (t * 2 - 1), py + (c.y - py) * (t * 2 - 1)))
			: ((1 - t) * (1 - t) * a.x + 2 * t * (1 - t) * mx + t * t * c.x, (1 - t) * (1 - t) * a.y + 2 * t * (1 - t) * my + t * t * c.y);
		g.save();
		g.strokeStyle = "#05070f"; g.lineWidth = 6; g.beginPath(); for (int j = 0; j <= 24; j++) { var (x, y) = at(j / 24.0); if (j == 0) g.moveTo(x, y); else g.lineTo(x, y); } g.stroke();
		for (int k = 0; k < 2; k++)
		{
			g.strokeStyle = tmCol(k); g.globalAlpha = 0.75 + 0.25 * tight; g.lineWidth = 2.5; g.beginPath();
			for (int j = 0; j <= 12; j++) { var (x, y) = at(k * 0.5 + j / 24.0); if (j == 0) g.moveTo(x, y); else g.lineTo(x, y); }
			g.stroke();
		}
		if (tight > 0)
			for (int j = 0; j < 3; j++)
			{
				var (x, y) = at((now / 900.0 + j / 3.0) % 1); g.globalAlpha = tight; g.fillStyle = "#ffffff"; g.shadowColor = "#ffffff"; g.shadowBlur = 8;
				g.beginPath(); g.arc(x, y, 2.5, 0, TAU); g.fill();
			}
		g.restore();
	}
	// the colour of the key that drives a thing (its first button's owner)
	static string duoKeyCol(TThing d)
	{
		var id = d.need?.FirstOrDefault(); var k = id == null ? null : tm.th.FirstOrDefault(q => q.k == "cbtn" && q.d.id == id);
		return k == null ? "#c9d4ff" : tmCol((int)k.d.a);
	}
	// a speaker: a dark box with a cone; a beat before it sends its wave the cone throbs and the wave's way shows (dashed pink)
	static void duoSpeaker(TS s, double now)
	{
		double cx = s.X + PT / 2, cy = (s.d.y + s.d.h / 2) * PT, warn = tmSpkWarn(s), kick = Math.Max(0, 1 - (tm.t - s.timer) * 4);
		double hh = Math.Max(PT, s.d.h * PT);
		g.save();
		g.fillStyle = "#0d1230"; g.strokeStyle = "#c9d4ff"; g.lineWidth = 2; g.shadowColor = "#9fb4ff"; g.shadowBlur = 8;
		g.fillRect(s.X + 2, cy - hh / 2, PT - 4, hh); g.strokeRect(s.X + 2, cy - hh / 2, PT - 4, hh); g.shadowBlur = 0;
		int n = Math.Max(1, (int)Math.Round(s.d.h));
		for (int i = 0; i < n; i++)
		{
			double y = cy - hh / 2 + PT * (i + 0.5), r = 11 + 3 * kick + 2 * warn * Math.Abs(Math.Sin(now / 40));
			g.strokeStyle = "#c9d4ff"; g.lineWidth = 2; g.beginPath(); g.arc(cx, y, r, 0, TAU); g.stroke();
			g.fillStyle = "#c9d4ff"; g.beginPath(); g.arc(cx, y, 4 + 2 * kick, 0, TAU); g.fill();
		}
		if (warn > 0)
		{
			g.globalAlpha = 0.25 + 0.45 * warn; g.strokeStyle = PINK; g.lineWidth = 2; g.setLineDash(new double[] { 8, 8 });
			double x0 = s.d.a > 0 ? s.X + PT : s.d.x1 * PT, x1 = s.d.a > 0 ? s.d.x1 * PT : s.X;
			g.strokeRect(x0, s.d.y * PT + 2, x1 - x0, n * PT - 4); g.setLineDash(new double[0]);
		}
		g.restore();
	}
	// the shield: a white ring round the one carrying it
	static void duoShield(double now)
	{
		var p = tm.b[tm.shield]; if (p.down || p.gate) return;
		g.save(); g.strokeStyle = "#ffffff"; g.shadowColor = "#ffffff"; g.shadowBlur = 14 + 6 * pulse; g.lineWidth = 3; g.globalAlpha = 0.85;
		g.beginPath(); g.arc(p.x, p.y, SHIELD_R, 0, TAU); g.stroke();
		g.globalAlpha = 0.12; g.fillStyle = "#ffffff"; g.beginPath(); g.arc(p.x, p.y, SHIELD_R, 0, TAU); g.fill();
		g.restore();
	}
}
