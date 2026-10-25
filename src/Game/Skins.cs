using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// The square's looks (outputs/big-update/skins-src/skins-final.html, approved 10-18): drawn at the square's own size (18 px),
// so the hitbox and everything else stays the same. Each look has a walking trail and a dash trail.
public static partial class G
{
	public static string skin = "";   // "" = the original square
	public static readonly string[] SKINS = { "", "crystal", "circuit", "bolt", "eq", "skull", "coffin", "teddy" };   // the last 3: Hardcore (HardSkins.cs)
	// replays show the look of whoever played them (the bot and old replays: the original square)
	public static string playerSkin() => classicUi ? "" : state is "replay" or "rpGap" or "rpEnd" ? (rp.bot || rp.doc == null ? "" : rp.doc.sk) : skin;
	public static string skinName(string s) => s == "" ? "العادي" : Array.Find(CHALS, c => c.id == s)?.name ?? "";

	// online, two with the same look (his words 04:59 10-25): the first seat keeps its colours, a later one takes its seat's colour (never pink)
	public static readonly double[] SEAT_HUE = { -1, 0.12, 0.38, 0.75 };   // -, gold, green, purple
	public static double dupHue(IList<string> sk, int seat)
	{
		if (sk == null || seat <= 0 || seat >= sk.Count || seat >= SEAT_HUE.Length) return -1;
		for (int i = 0; i < seat; i++) if ((sk[i] ?? "") == (sk[seat] ?? "")) return SEAT_HUE[seat];
		return -1;
	}
	public static void tinted(double h, Action a) { if (h < 0) { a(); return; } g.setTint(h); a(); g.clearTint(); }
	// a co-op player at (0,0): the old square in its colour, or its look inside a frame of its colour (the levels go by the colours)
	public static void coopSquare(string col, string sk, double k, double now, string inner)
	{
		if (string.IsNullOrEmpty(sk)) { g.shadowColor = col; g.shadowBlur = 14 * k; g.fillStyle = col; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = inner; g.fillRect(-3, -3, 6, 6); return; }
		g.shadowColor = col; g.shadowBlur = 16 * k; g.strokeStyle = col; g.lineWidth = 2.4; g.strokeRect(-11.6, -11.6, 23.2, 23.2); g.shadowBlur = 0;
		skinBody(sk, k, now);
	}

	// the square at (0,0); k scales the glow (1 = in the level); now = ms, for the moving parts
	public static void skinBody(string s, double k, double now)
	{
		switch (s)
		{
			case "crystal": skCrystal(k); break;
			case "circuit": skCircuit(k); break;
			case "bolt": skBolt(k); break;
			case "eq": skEq(k, now); break;
			case "skull": skSkull(k, now); break;
			case "coffin": skCoffin(k, now); break;
			case "teddy": skTeddy(k, now); break;
			default:
				g.shadowColor = CYAN; g.shadowBlur = 14 * k; g.fillStyle = CYAN; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0; g.fillStyle = ARENA; g.fillRect(-3, -3, 6, 6); break;
		}
	}
	static void skFrame(double k, string fill)
	{
		g.shadowColor = CYAN; g.shadowBlur = 14 * k; g.fillStyle = fill; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0;
		g.strokeStyle = CYAN; g.lineWidth = 1.1; g.strokeRect(-8.4, -8.4, 16.8, 16.8);
	}
	static void skCrystal(double k)
	{
		g.shadowColor = "#5ff4ff"; g.shadowBlur = 16 * k;
		var gr = g.createLinearGradient(-9, -9, 9, 9); gr.addColorStop(0, "#d8fdff"); gr.addColorStop(0.45, "#3fe9f0"); gr.addColorStop(1, "#0a8fa6"); g.fillStyle = gr; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0;
		var f = new (double[] p, string c)[]
		{
			(new double[] { -9, -9, 9, -9, 5, -5, -5, -5 }, "rgba(255,255,255,0.45)"), (new double[] { 9, -9, 9, 9, 5, 5, 5, -5 }, "rgba(0,40,60,0.18)"),
			(new double[] { 9, 9, -9, 9, -5, 5, 5, 5 }, "rgba(0,30,50,0.35)"), (new double[] { -9, 9, -9, -9, -5, -5, -5, 5 }, "rgba(255,255,255,0.22)"),
		};
		foreach (var (p, c) in f) { g.fillStyle = c; g.beginPath(); g.moveTo(p[0], p[1]); g.lineTo(p[2], p[3]); g.lineTo(p[4], p[5]); g.lineTo(p[6], p[7]); g.closePath(); g.fill(); }
		g.strokeStyle = "rgba(255,255,255,0.9)"; g.lineWidth = 0.6; g.strokeRect(-9, -9, 18, 18); g.strokeStyle = "rgba(255,255,255,0.5)"; g.lineWidth = 0.4; g.strokeRect(-5, -5, 10, 10);
		g.save(); g.rotate(Math.PI / 4); g.shadowColor = "#fff"; g.shadowBlur = 6 * k; g.fillStyle = "#ffffff"; g.fillRect(-2.4, -2.4, 4.8, 4.8); g.shadowBlur = 0; g.restore();
	}
	static readonly double[][][] CIRCUIT = { new[] { new[] { -8.4, -4 }, new[] { -5.0, -4 }, new[] { -3.0, -2 } }, new[] { new[] { -8.4, 3 }, new[] { -4.5, 3 }, new[] { -3, 1.5 } }, new[] { new[] { 4, -8.4 }, new[] { 4.0, -5 }, new[] { 2.5, -3 } },
		new[] { new[] { 8.4, 2 }, new[] { 5.0, 2 }, new[] { 3, 0.5 } }, new[] { new[] { -1, 8.4 }, new[] { -1, 4.5 }, new[] { 0.0, 3 } }, new[] { new[] { 8.4, 6 }, new[] { 6.0, 6 } } };
	static void skCircuit(double k)
	{
		skFrame(k, "#062830");
		g.strokeStyle = "rgba(45,226,230,0.85)"; g.lineWidth = 0.55;
		foreach (var l in CIRCUIT)
		{
			g.beginPath(); g.moveTo(l[0][0], l[0][1]); for (int i = 1; i < l.Length; i++) g.lineTo(l[i][0], l[i][1]); g.stroke();
			g.fillStyle = "#bffcff"; g.beginPath(); g.arc(l[^1][0], l[^1][1], 0.6, 0, TAU); g.fill();
		}
		g.fillStyle = "#0d4552"; g.fillRect(-3, -3, 6, 6); g.strokeStyle = "#bffcff"; g.lineWidth = 0.5; g.strokeRect(-3, -3, 6, 6);
		g.fillStyle = "#ffffff"; g.shadowColor = CYAN; g.shadowBlur = 6 * k; g.fillRect(-1.3, -1.3, 2.6, 2.6); g.shadowBlur = 0;
	}
	static void skBolt(double k)
	{
		skFrame(k, "#062830");
		g.save(); g.beginPath(); g.rect(-8.4, -8.4, 16.8, 16.8); g.clip(); g.fillStyle = "rgba(45,226,230,0.12)";
		for (int i = -3; i < 4; i++) { g.beginPath(); g.moveTo(i * 6 - 2, -9); g.lineTo(i * 6 + 1, -9); g.lineTo(i * 6 - 8, 9); g.lineTo(i * 6 - 11, 9); g.closePath(); g.fill(); }
		g.restore();
		g.fillStyle = "#ffffff"; g.shadowColor = CYAN; g.shadowBlur = 8 * k; g.beginPath(); g.moveTo(1.8, -7); g.lineTo(-4.2, 1); g.lineTo(-0.4, 1); g.lineTo(-2, 7); g.lineTo(4.2, -1.4); g.lineTo(0.4, -1.4); g.closePath(); g.fill(); g.shadowBlur = 0;
	}
	static readonly double[] EQ_H = { 5, 9, 13, 13, 9, 5 };   // high in the middle, so the square doesn't lean to one side
	// the sound bars move with the song (the beat pulse) and each bar on its own
	static void skEq(double k, double now)
	{
		skFrame(k, "#062830");
		double x = -7.45;   // 6 bars of 2 with gaps of 0.6, in the middle (a drawn bar spreads a little to the right)
		for (int i = 0; i < 6; i++)
		{
			double h = Math.Min(14.5, EQ_H[i] * (0.55 + 0.3 * (0.5 + 0.5 * JS.sin(now / 170 + i * 1.7)) + 0.35 * pulse));
			var gr = g.createLinearGradient(0, 7, 0, 7 - h); gr.addColorStop(0, "#1fb8c4"); gr.addColorStop(1, "#e8feff"); g.fillStyle = gr;
			g.shadowColor = CYAN; g.shadowBlur = 4 * k; g.fillRect(x, 7 - h, 2, h); g.shadowBlur = 0; g.fillStyle = "#ffffff"; g.fillRect(x, 7 - h - 1.3, 2, 0.6); x += 2.6;
		}
	}

	// the trail while dashing, by time and not by frames (the original trail is the last 10 frames: very short at 240 Hz):
	// one point per 1/60 s while dashing, kept 0.17 s
	const double SKT_DT = 1.0 / 60, SKT_KEEP = 0.17;
	public static void skinTrailAdd(Player p)
	{
		var l = p.skTrail; double t = songT;
		if (l.Count > 0 && t < l[^1].t) l.Clear();
		while (l.Count > 0 && l[0].t < t - SKT_KEEP) l.RemoveAt(0);
		if (p.dash > 0 && (l.Count == 0 || t - l[^1].t >= SKT_DT - 1e-6)) l.Add(new TrailPt { x = p.x, y = p.y, t = t, d = true });
		var w = p.wkTrail;
		if (w.Count > 0 && t < w[^1].t) w.Clear();
		while (w.Count > 0 && w[0].t < t - WK_KEEP) w.RemoveAt(0);
		if (w.Count == 0 || t - w[^1].t >= WK_DT - 1e-6) w.Add(new TrailPt { x = p.x, y = p.y, t = t, d = p.dash > 0 });
	}

	// the walking trail (v41): what the original square's trail looked like at 30 FPS (10 points 1/30 s apart), now by time so it looks
	// the same at any frame rate, and for every look. Each point fades with its age.
	const double WK_DT = 1.0 / 30, WK_KEEP = 1.0 / 3;
	public static void walkTrail(string s, Player p)
	{
		var w = p.wkTrail; int n = w.Count; if (n == 0) return; double t = songT;
		double age(int i) => clamp(1 - (t - w[i].t) / WK_KEEP, 0, 1);   // 1 = new, 0 = gone
		g.save();
		if (hcWalkTrail(s, w, age)) { g.restore(); return; }
		switch (s)
		{
			case "":
				for (int i = 0; i < n; i++) { g.globalAlpha = age(i) * (w[i].d ? 0.5 : 0.15); g.fillStyle = CYAN; g.fillRect(w[i].x - 7, w[i].y - 7, 14, 14); }
				break;
			case "crystal":
				// small crystal pieces, turned a little each
				for (int i = 0; i < n; i++)
				{
					double a = age(i), sz = 2 + 3 * a; g.globalAlpha = a * 0.35; g.fillStyle = "rgba(143,246,255,0.5)"; g.strokeStyle = "#bffcff"; g.lineWidth = 0.8;
					g.save(); g.translate(w[i].x, w[i].y); g.rotate(Math.PI / 4 + i * 0.5); g.fillRect(-sz, -sz, 2 * sz, 2 * sz); g.strokeRect(-sz, -sz, 2 * sz, 2 * sz); g.restore();
				}
				break;
			case "circuit":
				// a thin circuit line (right angles) with points of light
				for (int i = 1; i < n; i++)
				{
					var a = w[i - 1]; var b = w[i]; g.globalAlpha = age(i - 1) * 0.4; g.strokeStyle = CYAN; g.lineWidth = 1;
					g.beginPath(); g.moveTo(a.x, a.y); if (i % 2 == 1) g.lineTo(b.x, a.y); else g.lineTo(a.x, b.y); g.lineTo(b.x, b.y); g.stroke();
					if (i % 3 == 0) { g.globalAlpha = age(i) * 0.7; g.fillStyle = "#bffcff"; g.beginPath(); g.arc(b.x, b.y, 1.3, 0, TAU); g.fill(); }
				}
				break;
			case "eq":
				// small sound bars standing across the path, smaller as they fade
				for (int i = 0; i < n; i++)
				{
					double a = age(i), h = (3 + 5 * a) * (0.6 + 0.4 * Math.Abs(JS.sin(i * 1.9 + 0.7)));
					double dx = 1, dy = 0; if (i + 1 < n) { dx = w[i + 1].x - w[i].x; dy = w[i + 1].y - w[i].y; } else if (i > 0) { dx = w[i].x - w[i - 1].x; dy = w[i].y - w[i - 1].y; }
					double l = Math.Sqrt(dx * dx + dy * dy); if (l < 0.5) continue;
					g.globalAlpha = a * 0.4; g.fillStyle = "#8ff6ff"; g.save(); g.translate(w[i].x, w[i].y); g.rotate(JS.atan2(dy, dx)); g.fillRect(-1, -h / 2, 2, h); g.restore();
				}
				break;
			case "bolt":
				// a thin, faint zigzag
				g.strokeStyle = "#bffcff"; g.lineWidth = 1; g.shadowColor = CYAN; g.shadowBlur = 4;
				for (int i = 1; i < n; i++)
				{
					var a = w[i - 1]; var b = w[i]; double dx = b.x - a.x, dy = b.y - a.y, l = Math.Sqrt(dx * dx + dy * dy); if (l < 0.5) continue;
					double nx = -dy / l, ny = dx / l, o = (i % 2 == 0 ? 1 : -1) * 3;
					g.globalAlpha = age(i - 1) * 0.45; g.beginPath(); g.moveTo(a.x, a.y); g.lineTo((a.x + b.x) / 2 + nx * o, (a.y + b.y) / 2 + ny * o); g.lineTo(b.x, b.y); g.stroke();
				}
				g.shadowBlur = 0;
				break;
		}
		g.restore();
	}
	// pts from oldest to newest, in level coordinates
	public static void skinTrail(string s, Player p)
	{
		var pts = new List<TrailPt>(p.skTrail); if (p.dash > 0) pts.Add(new TrailPt { x = p.x, y = p.y, d = true }); int n = pts.Count; if (n < 2) return;
		g.save();
		if (hcDashTrail(s, pts)) { g.restore(); return; }
		switch (s)
		{
			case "crystal":
				for (int i = 0; i < n; i++)
				{
					double a = (i + 1.0) / n, sz = 2.5 + 4.5 * a; g.globalAlpha = a * 0.7; g.strokeStyle = "#8ff6ff"; g.lineWidth = 1.2;
					g.save(); g.translate(pts[i].x, pts[i].y); g.rotate(Math.PI / 4 * (1 - a) + i * 0.6); g.strokeRect(-sz, -sz, 2 * sz, 2 * sz); g.restore();
				}
				break;
			case "circuit":
				g.strokeStyle = CYAN; g.lineWidth = 1.4; g.globalAlpha = 0.55; g.beginPath(); g.moveTo(pts[0].x, pts[0].y);
				for (int i = 1; i < n; i++) { var a = pts[i - 1]; var b = pts[i]; if (i % 2 == 1) g.lineTo(b.x, a.y); g.lineTo(b.x, b.y); }
				g.stroke(); g.globalAlpha = 0.9; g.fillStyle = "#bffcff";
				for (int i = 0; i < n; i += 3) { g.beginPath(); g.arc(pts[i].x, pts[i].y, 1.8, 0, TAU); g.fill(); }
				break;
			case "bolt":
				g.strokeStyle = "#bffcff"; g.lineWidth = 1.6; g.shadowColor = CYAN; g.shadowBlur = 6; g.globalAlpha = 0.75; g.beginPath();
				for (int i = 0; i < n; i++)
				{
					double nx = 0, ny = 0; if (i > 0) { double dx = pts[i].x - pts[i - 1].x, dy = pts[i].y - pts[i - 1].y, l = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy)); nx = -dy / l; ny = dx / l; }
					double o = (i % 2 == 0 ? 1 : -1) * 4 * (i == n - 1 ? 0 : 1);
					if (i == 0) g.moveTo(pts[i].x, pts[i].y); else g.lineTo(pts[i].x + nx * o, pts[i].y + ny * o);
				}
				g.stroke(); g.shadowBlur = 0;
				break;
			case "eq":
				g.strokeStyle = CYAN; g.lineWidth = 1.4; g.globalAlpha = 0.6; g.beginPath();
				for (int i = 0; i < n; i++)
				{
					double nx = 0, ny = 0; if (i > 0) { double dx = pts[i].x - pts[i - 1].x, dy = pts[i].y - pts[i - 1].y, l = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy)); nx = -dy / l; ny = dx / l; }
					double o = JS.sin(i * 1.4) * 5 * (1 - (double)i / n);
					if (i == 0) g.moveTo(pts[i].x, pts[i].y); else g.lineTo(pts[i].x + nx * o, pts[i].y + ny * o);
				}
				g.stroke();
				break;
		}
		g.restore();
	}
}
