using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// Local play (his ask 10-30): each player on this PC has their own controller, so each one's buttons are read on their own.
// The keyboard counts as one more controller (device -1). Same buttons as readInput, only for one device.
public static partial class G
{
	public const int KB = -1;
	public static int rumbleOnly = -2;   // -2: every controller shakes; else only this one (local play: the one who was hit)
	static readonly Dictionary<int, Dictionary<string, bool>> devPrev = new();
	public static Func<int, Inp> devFake;          // tests: the buttons of a made-up device
	public static Func<List<int>> devListFake;     // tests: the made-up devices plugged in
	// the keyboard and the controllers plugged in now
	public static List<int> lcDevices()
	{
		if (devListFake != null) return devListFake();
		var l = new List<int> { KB }; if (!padsDisabled) l.AddRange(Input.GetConnectedJoypads()); return l;
	}
	public static string devKind(int dev) => dev == KB ? "kb" : kindForce ?? padKind(dev);
	public static Inp readDev(int dev)
	{
		if (devFake != null) { var fk = devFake(dev); if (fk != null) return fk; }   // tests (null: the real one)
		double x = 0, y = 0; bool dash = false, confirm = false, pz = false, back = false, restart = false, sq = false, l1 = false, r1 = false, jump = false, pdash = false, padAny = false;
		if (dev == KB)
		{
			if (Hooks.typing()) return new Inp();
			if (K("ArrowLeft") || K("KeyA")) x -= 1; if (K("ArrowRight") || K("KeyD")) x += 1; if (K("ArrowUp") || K("KeyW")) y -= 1; if (K("ArrowDown") || K("KeyS")) y += 1;
			if (K("Space") || K("ShiftLeft") || K("ShiftRight") || K("KeyJ") || K("KeyK")) dash = true;
			if (K("Enter") || K("Space")) confirm = true;
			if (K("Escape") || K("KeyP")) pz = true;
			if (K("Backspace")) back = true;
			if (K("KeyR")) restart = true; if (K("KeyT")) sq = true;
			if (K("KeyQ")) l1 = true; if (K("KeyE")) r1 = true;
			jump = K("Space") || K("KeyZ"); pdash = K("ShiftLeft") || K("ShiftRight") || K("KeyJ") || K("KeyK") || K("KeyX");
		}
		else if (!padsDisabled)
		{
			bool b(int i) => PadBtn(dev, i);
			for (int i = 0; i <= 16; i++) if (b(i)) { padAny = true; break; }
			double ax = Input.GetJoyAxis(dev, JoyAxis.LeftX), ay = Input.GetJoyAxis(dev, JoyAxis.LeftY), mag = hypot(ax, ay);
			if (mag > 0.22) { double k = Math.Min(1, (mag - 0.22) / 0.7) / mag; x += ax * k; y += ay * k; }
			if (b(14)) x -= 1; if (b(15)) x += 1; if (b(12)) y -= 1; if (b(13)) y += 1;
			if (b(0) || b(2) || b(4) || b(5) || b(6) || b(7)) dash = true;
			if (b(0)) jump = true; if (b(2) || b(4) || b(5) || b(6) || b(7)) pdash = true;
			if (b(0)) confirm = true; if (b(9)) pz = true; if (b(1)) back = true; if (b(3) || b(8)) restart = true; if (b(2)) sq = true; if (b(4)) l1 = true; if (b(5)) r1 = true;
		}
		var cur_ = new Dictionary<string, bool>
		{
			["dash"] = dash, ["confirm"] = confirm, ["pz"] = pz, ["back"] = back, ["restart"] = restart, ["sq"] = sq, ["l1"] = l1, ["r1"] = r1,
			["padAny"] = padAny, ["jump"] = jump, ["pdash"] = pdash, ["left"] = x < -0.5, ["right"] = x > 0.5, ["up"] = y < -0.5, ["down"] = y > 0.5,
		};
		devPrev.TryGetValue(dev, out var prv);
		bool E(string k) => cur_[k] && !(prv != null && prv.TryGetValue(k, out var v) && v);
		var o = new Inp
		{
			x = x, y = y, dash = E("dash"), confirm = E("confirm"), pz = E("pz"), back = E("back"), restart = E("restart"), sq = E("sq"),
			l1 = E("l1"), r1 = E("r1"), padAny = E("padAny"), jump = E("jump"), jumpHeld = jump, pdash = E("pdash"), left = E("left"), right = E("right"), up = E("up"), down = E("down"),
		};
		o.any = o.padAny || o.confirm || o.back || o.pz;
		devPrev[dev] = cur_; return o;
	}
	// a button held down when a screen opens must not count as a new press on it
	public static void devPrime() { foreach (int d in lcDevices()) readDev(d); }
}
