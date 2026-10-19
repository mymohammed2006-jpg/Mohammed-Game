using System;
using MG.Canvas;

namespace MG;

// the shining S (sShine) on the win screen and on cards, compared with Electron (scratch stest/)
public static class STest
{
	static Ctx g;
	static double clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
	static void sShine(string ch, double x, double y, double w, double now, double glow)
	{
		double per = 1700, p = -0.3 + ((now % per) / per) * 1.6, a = clamp(p - 0.16, 0, 1), b = clamp(p, 0, 1), c = clamp(p + 0.16, 0, 1);
		var gr = g.createLinearGradient(x - w / 2, y - w / 2, x + w / 2, y + w / 2); gr.addColorStop(0, "#ff2436"); gr.addColorStop(a, "#ff2436"); gr.addColorStop(b, "#ffe0e4"); gr.addColorStop(c, "#ff2436"); gr.addColorStop(1, "#ff2436");
		g.shadowColor = "#ff2436"; g.shadowBlur = glow * (1 + 0.45 * JS.sin(now / 260)); g.fillStyle = gr; g.fillText(ch, x, y); g.shadowBlur = 0;
	}
	public static void Run(Ctx ctx)
	{
		g = ctx;
		g.fillStyle = "#07040d"; g.fillRect(0, 0, 1280, 720);
		string T = "\"Audiowide\",\"Segoe UI\",sans-serif";
		double[] ps = { -0.2, 0.1, 0.35, 0.6, 0.9 };
		for (int i = 0; i < 5; i++)
		{
			double now = 1700 * (ps[i] + 0.3) / 1.6 + 5 * 1700;
			g.save(); g.translate(140 + 250 * i, 200); g.scale(1.1, 1.1); g.font = "150px " + T; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; sShine("S", 0, 0, 110, now, 30); g.restore();
			g.font = "32px " + T; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; sShine("S", 140 + 250 * i, 450, 26, now, 12);
		}
		g.font = "150px " + T; g.fillStyle = "#ff2436"; g.shadowColor = "#ff2436"; g.shadowBlur = 30; g.fillText("A", 640, 600); g.shadowBlur = 0;
	}
}
