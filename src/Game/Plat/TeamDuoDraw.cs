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
		foreach (var p in tm.b) tmDrawBody(p, now);
		if (tmMe >= 0) foreach (var p in tm.b) if (p.i != tmMe && !p.gate) { g.globalAlpha = 0.75; txt(tm.names[p.i], p.x, p.y - 30, 16, "#fff", 700); g.globalAlpha = 1; }
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
}
