using System;
using System.Collections.Generic;
using static MG.JS;

namespace MG;

// The 3 Hardcore looks (v45, Mohammed 19:35 and 20:16 10-21): finishing each Hardcore level opens one, with XP (1000, 2000, 3000).
// The skull, the coffin (the dead, for level 2) and the teddy bear (from level 3's own room of bears). Grand: a gold-edged frame, bone and silver shading, glowing eyes.
// Same rules as the other looks: drawn at the square's own size (18 px), cyan and gold (pink is for attacks), a walking trail and a dash trail.
// (the first designs he turned down are kept in /mnt/project-files/tools/hardcore-skins-rejected/)
public static partial class G
{
	const string HG = "#ffd25c", HG2 = "#c48a12", HBG = "#03121a";
	// the frame of the 3: dark, cyan glow, a thin gold line inside and gold studs in the corners
	static void hcFrame(double k)
	{
		g.shadowColor = CYAN; g.shadowBlur = 14 * k; g.fillStyle = HBG; g.fillRect(-9, -9, 18, 18); g.shadowBlur = 0;
		g.strokeStyle = CYAN; g.lineWidth = 1.1; g.strokeRect(-8.4, -8.4, 16.8, 16.8);
		g.strokeStyle = "rgba(255,210,92,0.55)"; g.lineWidth = 0.4; g.strokeRect(-7.4, -7.4, 14.8, 14.8);
		g.fillStyle = HG; foreach (var (x, y) in new[] { (-7.4, -7.4), (7.4, -7.4), (-7.4, 7.4), (7.4, 7.4) }) { g.save(); g.translate(x, y); g.rotate(Math.PI / 4); g.fillRect(-0.75, -0.75, 1.5, 1.5); g.restore(); }
	}
	static double hcPulse(double now, double ms) => 0.6 + 0.4 * JS.sin(now / ms);
	static void glowEye(double x, double y, double r, double k, double p)
	{
		g.shadowColor = CYAN; g.shadowBlur = 6 * k * p; g.fillStyle = CYAN; g.beginPath(); g.arc(x, y, r, 0, TAU); g.fill();
		g.shadowBlur = 0; g.fillStyle = "#e8feff"; g.beginPath(); g.arc(x - r * 0.25, y - r * 0.25, r * 0.4, 0, TAU); g.fill();
	}

	// ---------- the skull (الجمجمة): Hardcore level 1 ----------
	static void skullPath()
	{
		g.beginPath(); g.arc(0, -2.3, 6.1, Math.PI * 0.88, Math.PI * 2.12);
		g.lineTo(5.0, 1.8); g.lineTo(3.5, 2.9); g.lineTo(3.5, 6.0); g.quadraticCurveTo(0, 7.4, -3.5, 6.0); g.lineTo(-3.5, 2.9); g.lineTo(-5.0, 1.8); g.closePath();
	}
	static void skSkull(double k, double now)
	{
		hcFrame(k);
		var gr = g.createLinearGradient(0, -8.4, 0, 7); gr.addColorStop(0, "#ffffff"); gr.addColorStop(0.55, "#d6f2f3"); gr.addColorStop(1, "#7fa6ab");
		g.shadowColor = CYAN; g.shadowBlur = 5 * k; g.fillStyle = gr; skullPath(); g.fill(); g.shadowBlur = 0;
		// the temples and the cheeks in shadow
		g.save(); skullPath(); g.clip(); g.fillStyle = "rgba(3,18,26,0.22)";
		g.beginPath(); g.ellipse(-5.6, -1.2, 1.6, 3, 0, 0, TAU); g.fill(); g.beginPath(); g.ellipse(5.6, -1.2, 1.6, 3, 0, 0, TAU); g.fill();
		g.fillRect(-3.5, 2.9, 7, 0.6); g.restore();
		// a crack on the forehead
		g.strokeStyle = "rgba(3,18,26,0.75)"; g.lineWidth = 0.35; g.beginPath(); g.moveTo(1.6, -8.3); g.lineTo(0.8, -6.6); g.lineTo(1.9, -5.5); g.lineTo(1.1, -4.3); g.stroke();
		// the eyes: deep and angled, a cyan light inside
		g.fillStyle = HBG;
		g.beginPath(); g.moveTo(-4.7, -2.6); g.lineTo(-0.9, -1.7); g.lineTo(-1.2, 1.0); g.quadraticCurveTo(-3.6, 1.6, -4.5, 0.2); g.closePath(); g.fill();
		g.beginPath(); g.moveTo(4.7, -2.6); g.lineTo(0.9, -1.7); g.lineTo(1.2, 1.0); g.quadraticCurveTo(3.6, 1.6, 4.5, 0.2); g.closePath(); g.fill();
		double p = hcPulse(now, 260); glowEye(-2.6, -0.5, 0.85, k, p); glowEye(2.6, -0.5, 0.85, k, p);
		// the nose
		g.fillStyle = HBG; g.beginPath(); g.moveTo(-0.15, 1.5); g.lineTo(-1.1, 3.1); g.lineTo(-0.15, 2.9); g.closePath(); g.fill(); g.beginPath(); g.moveTo(0.15, 1.5); g.lineTo(1.1, 3.1); g.lineTo(0.15, 2.9); g.closePath(); g.fill();
		// the teeth
		g.fillStyle = "rgba(3,18,26,0.85)"; g.fillRect(-3.2, 4.6, 6.4, 0.35);
		for (int i = -2; i <= 2; i++) g.fillRect(i * 1.25 - 0.17, 3.7, 0.34, 2.6);
	}

	// ---------- the coffin (التابوت): Hardcore level 2 ----------
	static void coffinPath(double s = 1)
	{
		g.beginPath(); g.moveTo(-3.1 * s, -8 * s); g.lineTo(3.1 * s, -8 * s); g.lineTo(5.6 * s, -3.6 * s); g.lineTo(3.8 * s, 8 * s); g.lineTo(-3.8 * s, 8 * s); g.lineTo(-5.6 * s, -3.6 * s); g.closePath();
	}
	static void skCoffin(double k, double now)
	{
		hcFrame(k);
		double p = hcPulse(now, 330);
		// light from inside, through the lid that is open a little
		g.save(); g.translate(-0.6, 0.3); g.shadowColor = CYAN; g.shadowBlur = 9 * k * p; g.fillStyle = $"rgba(45,226,230,{str(0.55 + 0.35 * p)})"; coffinPath(); g.fill(); g.restore(); g.shadowBlur = 0;
		var gr = g.createLinearGradient(-5.6, -8, 5.6, 8); gr.addColorStop(0, "#14505c"); gr.addColorStop(0.5, "#0a2f38"); gr.addColorStop(1, "#04161c");
		g.save(); g.translate(0.5, -0.2); g.rotate(-0.04);
		g.fillStyle = gr; coffinPath(); g.fill(); g.strokeStyle = HG; g.lineWidth = 0.75; g.stroke();
		g.strokeStyle = "rgba(255,210,92,0.5)"; g.lineWidth = 0.35; coffinPath(0.72); g.stroke();
		// a gold diamond in the middle, gold nails at the corners
		var dg = g.createLinearGradient(0, -3, 0, 3); dg.addColorStop(0, "#fff2c4"); dg.addColorStop(1, HG2); g.fillStyle = dg;
		g.beginPath(); g.moveTo(0, -3.4); g.lineTo(1.9, 0); g.lineTo(0, 3.4); g.lineTo(-1.9, 0); g.closePath(); g.fill();
		g.fillStyle = CYAN; g.beginPath(); g.arc(0, 0, 0.7, 0, TAU); g.fill();
		g.fillStyle = HG; foreach (var (x, y) in new[] { (-2.6, -6.8), (2.6, -6.8), (-4.3, -3.4), (4.3, -3.4), (-3.1, 6.8), (3.1, 6.8) }) { g.beginPath(); g.arc(x, y, 0.42, 0, TAU); g.fill(); }
		g.restore();
		// a hand of bones over the edge, from inside
		g.strokeStyle = "#e8feff"; g.lineCap = "round"; g.shadowColor = "#e8feff"; g.shadowBlur = 2 * k;
		foreach (var (y, l) in new[] { (-5.2, 2.2), (-3.9, 2.6), (-2.6, 2.4), (-1.4, 1.8) })
		{
			g.lineWidth = 0.55; g.beginPath(); g.moveTo(3.4, y + 0.4); g.lineTo(5.9, y); g.lineTo(5.9 + l * 0.35, y + l * 0.45); g.stroke();
			g.fillStyle = "#e8feff"; g.beginPath(); g.arc(5.9, y, 0.4, 0, TAU); g.fill();
		}
		g.lineWidth = 0.6; g.beginPath(); g.moveTo(4.6, -0.2); g.lineTo(6.6, -1.6); g.stroke();   // the thumb
		g.shadowBlur = 0; g.lineCap = "butt";
	}

	// ---------- the teddy bear (الدبدوب): Hardcore level 3, from its room of bears ----------
	static void skTeddy(double k, double now)
	{
		hcFrame(k);
		double p = hcPulse(now, 300);
		var fur = g.createRadialGradient(-1.5, -1.5, 0.5, 0, 0.8, 6.8); fur.addColorStop(0, "#e2b265"); fur.addColorStop(0.65, "#a8752e"); fur.addColorStop(1, "#5a3a12");
		// the ears
		foreach (double sx in new[] { -1.0, 1.0 })
		{
			g.fillStyle = fur; g.beginPath(); g.arc(sx * 4.5, -4.3, 2.2, 0, TAU); g.fill();
			g.fillStyle = "#4a2f0e"; g.beginPath(); g.arc(sx * 4.5, -4.2, 1.1, 0, TAU); g.fill();
		}
		// the head
		g.shadowColor = HG; g.shadowBlur = 4 * k; g.fillStyle = fur; g.beginPath(); g.arc(0, 0.9, 5.7, 0, TAU); g.fill(); g.shadowBlur = 0;
		// a seam down the forehead, gold thread
		g.strokeStyle = HG; g.lineWidth = 0.35; g.beginPath(); g.moveTo(0, -4.8); g.lineTo(0, -1.4); g.stroke();
		for (int i = 0; i < 4; i++) { double y = -4.4 + i * 0.9; g.beginPath(); g.moveTo(-0.6, y - 0.3); g.lineTo(0.6, y + 0.3); g.stroke(); }
		// the muzzle and the nose
		g.fillStyle = "#e9c88a"; g.beginPath(); g.ellipse(0, 3.0, 2.7, 2.1, 0, 0, TAU); g.fill();
		g.fillStyle = "#2a1806"; g.beginPath(); g.moveTo(-1.0, 1.9); g.lineTo(1.0, 1.9); g.lineTo(0, 3.0); g.closePath(); g.fill();
		// a stitched smile
		g.strokeStyle = "#2a1806"; g.lineWidth = 0.4; g.beginPath(); g.moveTo(-2.0, 3.6); g.quadraticCurveTo(0, 5.2, 2.0, 3.6); g.stroke();
		for (int i = 0; i < 5; i++) { double x = -1.6 + i * 0.8, y = 3.6 + 0.9 * JS.sin((x + 2) / 4 * Math.PI); g.beginPath(); g.moveTo(x, y - 0.55); g.lineTo(x, y + 0.55); g.stroke(); }
		// the left eye: a gold-rimmed button
		g.fillStyle = HG; g.beginPath(); g.arc(-2.3, -0.4, 1.35, 0, TAU); g.fill();
		g.fillStyle = "#0a0a0a"; g.beginPath(); g.arc(-2.3, -0.4, 1.0, 0, TAU); g.fill();
		g.fillStyle = HG; foreach (var (x, y) in new[] { (-2.65, -0.75), (-1.95, -0.75), (-2.65, -0.05), (-1.95, -0.05) }) { g.beginPath(); g.arc(x, y, 0.16, 0, TAU); g.fill(); }
		// the right eye: torn open, a cyan light inside, stitched across
		g.fillStyle = "#1a0f04"; g.beginPath(); g.ellipse(2.3, -0.4, 1.3, 1.0, -0.3, 0, TAU); g.fill();
		glowEye(2.3, -0.4, 0.65, k, p);
		g.strokeStyle = "#e9c88a"; g.lineWidth = 0.35; g.beginPath(); g.moveTo(1.2, -1.4); g.lineTo(3.4, 0.6); g.moveTo(3.4, -1.4); g.lineTo(1.2, 0.6); g.stroke();
	}

	// the walking trails
	static bool hcWalkTrail(string s, List<TrailPt> w, Func<int, double> age)
	{
		int n = w.Count;
		switch (s)
		{
			case "skull":
				// bone dust: small pale specks that scatter as they fade
				for (int i = 0; i < n; i++)
				{
					double a = age(i), sp = (1 - a) * 4; g.globalAlpha = a * 0.5; g.fillStyle = i % 3 == 0 ? HG : "#d6f2f3";
					g.beginPath(); g.arc(w[i].x + JS.sin(i * 2.7) * sp, w[i].y + JS.cos(i * 1.9) * sp, 0.7 + 0.9 * a, 0, TAU); g.fill();
				}
				return true;
			case "coffin":
				// grave mist: dark puffs with a cyan edge that open up as they fade
				for (int i = 0; i < n; i += 2)
				{
					double a = age(i), r = 2 + (1 - a) * 5; g.globalAlpha = a * 0.35; g.fillStyle = "#0a2f38"; g.strokeStyle = CYAN; g.lineWidth = 0.6;
					g.beginPath(); g.arc(w[i].x, w[i].y + (1 - a) * 3, r, 0, TAU); g.fill(); g.stroke();
				}
				return true;
			case "teddy":
				// the music box: small gold notes that float up as they fade
				for (int i = 0; i < n; i += 3)
				{
					double a = age(i), x = w[i].x + JS.sin(i * 1.7) * 3, y = w[i].y - (1 - a) * 9, sz = 0.8 + 0.6 * a; g.globalAlpha = a * 0.6; g.fillStyle = HG; g.strokeStyle = HG; g.lineWidth = 0.7;
					g.beginPath(); g.ellipse(x, y, 1.6 * sz, 1.15 * sz, -0.4, 0, TAU); g.fill();
					g.beginPath(); g.moveTo(x + 1.4 * sz, y - 0.3 * sz); g.lineTo(x + 1.4 * sz, y - 5 * sz); g.lineTo(x + 3 * sz, y - 4 * sz); g.stroke();
				}
				return true;
		}
		return false;
	}
	// the dash trails; pts from oldest to newest
	static bool hcDashTrail(string s, List<TrailPt> pts)
	{
		int n = pts.Count;
		switch (s)
		{
			case "skull":
				// ghost skulls left behind, fading
				for (int i = 0; i < n - 1; i++)
				{
					double a = (i + 1.0) / n, sc = 0.45 + 0.5 * a; g.globalAlpha = a * 0.45;
					g.save(); g.translate(pts[i].x, pts[i].y); g.scale(sc, sc); g.fillStyle = "#d6f2f3"; skullPath(); g.fill();
					g.fillStyle = HBG; g.beginPath(); g.arc(-2.5, -0.6, 1.5, 0, TAU); g.fill(); g.beginPath(); g.arc(2.5, -0.6, 1.5, 0, TAU); g.fill(); g.restore();
				}
				return true;
			case "coffin":
				// a silver chain: links turned one way then the other along the path
				g.strokeStyle = "#bfe9ee"; g.lineWidth = 1.1; g.shadowColor = CYAN; g.shadowBlur = 4;
				for (int i = 1; i < n; i++)
				{
					double dx = pts[i].x - pts[i - 1].x, dy = pts[i].y - pts[i - 1].y; if (dx * dx + dy * dy < 0.25) continue;
					double a = (double)i / n; g.globalAlpha = 0.3 + 0.6 * a;
					g.save(); g.translate((pts[i].x + pts[i - 1].x) / 2, (pts[i].y + pts[i - 1].y) / 2); g.rotate(JS.atan2(dy, dx));
					g.beginPath(); g.ellipse(0, 0, 4.2, i % 2 == 0 ? 2.0 : 0.8, 0, 0, TAU); g.stroke(); g.restore();
				}
				g.shadowBlur = 0;
				return true;
			case "teddy":
				// a seam sewn along the path: gold thread with cross stitches
				g.strokeStyle = HG; g.shadowColor = HG; g.shadowBlur = 4; g.lineWidth = 1;
				for (int i = 1; i < n; i++)
				{
					var A = pts[i - 1]; var Bp = pts[i]; double dx = Bp.x - A.x, dy = Bp.y - A.y, l = Math.Sqrt(dx * dx + dy * dy); if (l < 0.5) continue;
					double nx = -dy / l, ny = dx / l, a = (double)i / n; g.globalAlpha = 0.3 + 0.6 * a;
					g.beginPath(); g.moveTo(A.x, A.y); g.lineTo(Bp.x, Bp.y); g.stroke();
					double mx = (A.x + Bp.x) / 2, my = (A.y + Bp.y) / 2, h = 2.5 + 2 * a;
					g.beginPath(); g.moveTo(mx - nx * h - dx / l * 1.5, my - ny * h - dy / l * 1.5); g.lineTo(mx + nx * h + dx / l * 1.5, my + ny * h + dy / l * 1.5); g.stroke();
				}
				g.shadowBlur = 0;
				return true;
		}
		return false;
	}
}
