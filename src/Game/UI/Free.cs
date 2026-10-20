using System;
using static MG.JS;

namespace MG;

// New in the Godot version: the frame cap (30 to 500) and the volume (0 to 100) take any number.
// Right/left change it by 1 (holding repeats and speeds up), L1/R1 (Q/E) by 10, and on the keyboard Enter lets you type the number.
// With classicUi (the comparison tools) the settings work exactly like v39.
public static partial class G
{
	public static string numRow;          // "fps" / "vol" while a number is being typed for that row
	public static string numBuf = "";
	static int holdD; static double holdT, holdAcc;
	static bool freeRow(string r) => !classicUi && (r == "fps" || r == "vol");
	static int numVal(string r) => r == "fps" ? fpsCap : (int)round(vol * 100);
	static void numSet(string r, int v)
	{
		if (r == "fps") { v = Math.Clamp(v, FPS_MIN, FPS_MAX); if (v != fpsCap) setFps(v); }
		else { v = Math.Clamp(v, 0, 100); if (v != numVal("vol")) setVol(v / 100.0); }
	}
	// how many steps right/left gives this frame: one on the press, then after a moment it repeats faster and faster
	static int holdSteps(Inp inp, double dt, double maxRate)
	{
		int hd = inp.x > 0.5 ? 1 : inp.x < -0.5 ? -1 : 0;
		if (hd == 0 || hd != holdD) { holdD = hd; holdT = 0; holdAcc = 0; return hd; }
		holdT += dt; if (holdT < 0.35) return 0;
		holdAcc += Math.Min(maxRate, 8 + (holdT - 0.35) * maxRate * 0.6) * dt; int n = (int)Math.Floor(holdAcc); holdAcc -= n; return n * hd;
	}
	// true when it used the input
	static bool freeStep(Inp inp, double dt, string r)
	{
		int n = holdSteps(inp, dt, r == "fps" ? 140 : 60) + (inp.r1 ? 10 : 0) - (inp.l1 ? 10 : 0);
		if (n != 0) { numSet(r, numVal(r) + n); return true; }
		if (inp.confirm && !usingPad) { numRow = r; numBuf = ""; return true; }
		return false;
	}
	// the keyboard while typing a number: digits, Backspace, Enter puts it, Esc cancels. true = the key is used here
	public static bool numKey(string code)
	{
		if (numRow == null || !(state is "settings" or "pauseSet" or "pltSet" or "tmSet")) { numRow = null; return false; }
		string dg = code.StartsWith("Digit") ? code.Substring(5) : code.StartsWith("Kp") && code.Length == 3 && char.IsDigit(code[2]) ? code.Substring(2) : null;
		if (dg != null) { if (numBuf.Length < 3) numBuf = (numBuf == "0" ? "" : numBuf) + dg; }
		else if (code == "Backspace") { if (numBuf.Length > 0) numBuf = numBuf.Substring(0, numBuf.Length - 1); }
		else if (code is "Enter" or "NumpadEnter" or "KpEnter") numDone(true);
		else if (code == "Escape") numDone(false);
		else if (code == "F11") return false;
		return true;
	}
	public static void numDone(bool put) { if (put && numBuf != "") numSet(numRow, int.Parse(numBuf)); numRow = null; }
	// the box the number is typed in
	static void numBox(double cx, double y, double w)
	{
		rr(cx - w / 2, y - 24, w, 48, 12); g.fillStyle = CARDS; g.fill(); g.strokeStyle = RED; g.lineWidth = 2.5; g.stroke();
		g.font = $"800 30px {FONT}"; double tw = g.measureText(numBuf).width;
		txt(numBuf, cx, y + 2, 30, "#fff", 800);
		if (Math.Floor(performance.now() / 500) % 2 == 0) { g.fillStyle = RED; g.fillRect(cx + tw / 2 + 4, y - 15, 3, 30); }
	}
	// the volume bars: 10 bars, the last one partly filled (vol is 0..1)
	static void volBars(double x0, double y, bool sel, double bw, double gp)
	{
		double on = vol * 10;
		for (int j = 0; j < 10; j++)
		{
			double x = x0 + j * (bw + gp), h = 12 + j * 3, f = clamp(on - j, 0, 1);
			rr(x, y - h / 2, bw, h, 5); g.fillStyle = "rgba(255,255,255,0.1)"; g.fill();
			if (f <= 0) continue;
			g.save(); g.beginPath(); g.rect(x, y - h / 2 - 2, bw * f, h + 4); g.clip();
			rr(x, y - h / 2, bw, h, 5); g.fillStyle = RED; if (sel) { g.shadowColor = RED; g.shadowBlur = 12 * (0.6 + 0.6 * pulse); } g.fill(); g.shadowBlur = 0;
			g.restore();
		}
	}
}
