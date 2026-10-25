using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// one frame of input, like readInput()'s result: x/y are held, the rest are "just pressed"
public class Inp
{
	public double x, y;
	public bool dash, confirm, pz, back, restart, sq, l1, r1, padAny, left, right, up, down, any;
	public bool jump, jumpHeld, pdash;   // the platformer: jump (Space / Z / ✕ / A) and its dash (Shift / J / K / X / □ / L1 / R1)
}

// original index.html lines 6837-6882 (keyboard, gamepads, readInput)
public static partial class G
{
	public static readonly Dictionary<string, bool> keys = new();
	public static bool K(string code) => keys.TryGetValue(code, out var v) && v;
	public static bool anyKeyFlag = false, f11Hit = false;
	public static string kind = "kb";
	public static string toastPad = null, toast = "";
	public static double toastT = 0;

	// Godot key -> the browser's KeyboardEvent.code
	public static string JsCode(InputEventKey e)
	{
		var k = e.PhysicalKeycode != Key.None ? e.PhysicalKeycode : e.Keycode;
		if (k >= Key.A && k <= Key.Z) return "Key" + (char)('A' + (k - Key.A));
		if (k >= Key.Key0 && k <= Key.Key9) return "Digit" + (char)('0' + (k - Key.Key0));
		if (k >= Key.F1 && k <= Key.F12) return "F" + (1 + (k - Key.F1));
		bool right = e.Location == KeyLocation.Right;
		return k switch
		{
			Key.Left => "ArrowLeft", Key.Right => "ArrowRight", Key.Up => "ArrowUp", Key.Down => "ArrowDown",
			Key.Space => "Space", Key.Enter => "Enter", Key.KpEnter => "NumpadEnter", Key.Escape => "Escape",
			Key.Backspace => "Backspace", Key.Tab => "Tab", Key.Delete => "Delete",
			Key.Shift => right ? "ShiftRight" : "ShiftLeft", Key.Ctrl => right ? "ControlRight" : "ControlLeft",
			Key.Alt => right ? "AltRight" : "AltLeft", Key.Home => "Home", Key.End => "End",
			Key.Period => "Period", Key.Comma => "Comma", Key.Minus => "Minus", Key.Equal => "Equal",
			_ => k.ToString(),
		};
	}

	// keydown listener
	public static void OnKeyDown(string code, bool repeat)
	{
		if (numKey(code) || netKey(code)) { ensureAudio(); return; }
		if (Hooks.typing() && !new[] { "Enter", "NumpadEnter", "Escape", "ArrowUp", "ArrowDown", "Tab", "F11" }.Contains(code)) return;
		if (code == "NumpadEnter") keys["Enter"] = true;
		if (code == "F11") { if (!repeat) { if (isFull() && gatePassed) exitFS(); else enterFS(); f11Hit = true; } }
		keys[code] = true; if (!repeat) anyKeyFlag = true;
		ensureAudio(); if (code == "KeyF") toggleFS();
	}
	public static void OnKeyUp(string code) { keys[code] = false; }
	public static void OnPointerDown() { ensureAudio(); anyKeyFlag = true; }
	public static void OnBlur() { foreach (var k in keys.Keys.ToList()) keys[k] = false; if (state == "play") pause(); if (state == "pltPlay") { state = "pltPause"; pauseSel = 0; hostAsk = 0; if (!online) actx.suspend(); } if (state == "tmPlay" && tm?.won != true) { state = "tmPause"; pauseSel = 0; hostAsk = 0; } }

	public static string padKind(int dev)
	{
		string id = Input.GetJoyName(dev) + " " + Input.GetJoyGuid(dev);
		return System.Text.RegularExpressions.Regex.IsMatch(id, "054c|4c05|sony|dualshock|dualsense|playstation|ps4|ps5", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ? "ps" : "xbox";
	}
	public static void OnPadConnected(int dev, bool on)
	{
		if (on) { toastPad = padKind(dev); toast = toastPad == "ps" ? "تم توصيل يد سوني" : "تم توصيل يد إكس بوكس"; toastT = 2.4; }
		else { toastPad = null; toast = "انفصلت يد التحكم"; toastT = 2.4; if (state == "play") pause(); }
	}

	// the browser "standard" gamepad buttons from Godot's (SDL) layout
	static bool PadBtn(int dev, int i)
	{
		switch (i)
		{
			case 0: return Input.IsJoyButtonPressed(dev, JoyButton.A);
			case 1: return Input.IsJoyButtonPressed(dev, JoyButton.B);
			case 2: return Input.IsJoyButtonPressed(dev, JoyButton.X);
			case 3: return Input.IsJoyButtonPressed(dev, JoyButton.Y);
			case 4: return Input.IsJoyButtonPressed(dev, JoyButton.LeftShoulder);
			case 5: return Input.IsJoyButtonPressed(dev, JoyButton.RightShoulder);
			case 6: return Input.GetJoyAxis(dev, JoyAxis.TriggerLeft) > 30.0 / 255;   // Chrome's "pressed" threshold
			case 7: return Input.GetJoyAxis(dev, JoyAxis.TriggerRight) > 30.0 / 255;
			case 8: return Input.IsJoyButtonPressed(dev, JoyButton.Back);
			case 9: return Input.IsJoyButtonPressed(dev, JoyButton.Start);
			case 10: return Input.IsJoyButtonPressed(dev, JoyButton.LeftStick);
			case 11: return Input.IsJoyButtonPressed(dev, JoyButton.RightStick);
			case 12: return Input.IsJoyButtonPressed(dev, JoyButton.DpadUp);
			case 13: return Input.IsJoyButtonPressed(dev, JoyButton.DpadDown);
			case 14: return Input.IsJoyButtonPressed(dev, JoyButton.DpadLeft);
			case 15: return Input.IsJoyButtonPressed(dev, JoyButton.DpadRight);
			case 16: return Input.IsJoyButtonPressed(dev, JoyButton.Guide);
		}
		return false;
	}

	static Dictionary<string, bool> prev = new();
	public static bool padsDisabled;   // tests
	public static bool usingPad;       // the last thing touched was a controller (the mouse pointer hides)
	public static Inp readInput()
	{
		double x = 0, y = 0; bool dash = false, confirm = false, pz = false, back = false, restart = false, sq = false, l1 = false, r1 = false;
		if (K("ArrowLeft") || K("KeyA")) x -= 1; if (K("ArrowRight") || K("KeyD")) x += 1; if (K("ArrowUp") || K("KeyW")) y -= 1; if (K("ArrowDown") || K("KeyS")) y += 1;
		if (K("Space") || K("ShiftLeft") || K("ShiftRight") || K("KeyJ") || K("KeyK")) dash = true;
		if (K("Enter") || K("Space")) confirm = true;
		if (K("Escape") || K("KeyP")) pz = true;
		if (K("Backspace")) back = true;
		if (K("KeyR")) restart = true; if (K("KeyT")) sq = true;
		if (K("KeyQ")) l1 = true; if (K("KeyE")) r1 = true;
		bool jump = K("Space") || K("KeyZ"), pdash = K("ShiftLeft") || K("ShiftRight") || K("KeyJ") || K("KeyK") || K("KeyX");
		kind = "kb"; bool padAny = false;
		if (!padsDisabled)
			foreach (int dev in Input.GetConnectedJoypads())
			{
				if (kind == "kb") kind = padKind(dev);
				bool b(int i) => PadBtn(dev, i);
				for (int i = 0; i <= 16; i++) if (b(i)) { padAny = true; break; }
				double ax = Input.GetJoyAxis(dev, JoyAxis.LeftX), ay = Input.GetJoyAxis(dev, JoyAxis.LeftY), mag = hypot(ax, ay);
				if (mag > 0.22) usingPad = true;
				if (mag > 0.22) { double k = Math.Min(1, (mag - 0.22) / 0.7) / mag; x += ax * k; y += ay * k; }
				if (b(14)) x -= 1; if (b(15)) x += 1; if (b(12)) y -= 1; if (b(13)) y += 1;
				if (b(0) || b(2) || b(4) || b(5) || b(6) || b(7)) dash = true;
				if (b(0)) jump = true; if (b(2) || b(4) || b(5) || b(6) || b(7)) pdash = true;
				if (b(0)) confirm = true; if (b(9)) pz = true; if (b(1)) back = true; if (b(3) || b(8)) restart = true; if (b(2)) sq = true; if (b(4)) l1 = true; if (b(5)) r1 = true;
			}
		if (padAny) usingPad = true;
		var cur_ = new Dictionary<string, bool>
		{
			["dash"] = dash, ["confirm"] = confirm, ["pz"] = pz, ["back"] = back, ["restart"] = restart, ["sq"] = sq, ["l1"] = l1, ["r1"] = r1,
			["padAny"] = padAny, ["jump"] = jump, ["pdash"] = pdash, ["left"] = x < -0.5, ["right"] = x > 0.5, ["up"] = y < -0.5, ["down"] = y > 0.5,
		};
		bool E(string k) => cur_[k] && !(prev.TryGetValue(k, out var v) && v);
		var o = new Inp
		{
			x = x, y = y, dash = E("dash"), confirm = E("confirm"), pz = E("pz"), back = E("back"), restart = E("restart"), sq = E("sq"),
			l1 = E("l1"), r1 = E("r1"), padAny = E("padAny"), jump = E("jump"), jumpHeld = jump, pdash = E("pdash"), left = E("left"), right = E("right"), up = E("up"), down = E("down"),
		};
		o.any = anyKeyFlag || o.padAny; anyKeyFlag = false;
		prev = cur_; return o;
	}

	// gamepad rumble ('dual-rumble': strong = low-frequency motor, weak = high-frequency motor)
	public static void PadRumble(double ms, double s, double w)
	{
		foreach (int dev in Input.GetConnectedJoypads()) Input.StartJoyVibration(dev, (float)w, (float)s, (float)(ms / 1000));
	}

	public class BtnSet { public string[] move, confirm, dash, pause, back, restart, sq, small; }
	public static readonly Dictionary<string, BtnSet> BTN = new()
	{
		["ps"] = new BtnSet { move = new[] { "stick" }, confirm = new[] { "x" }, dash = new[] { "x", "R1" }, pause = new[] { "Options" }, back = new[] { "o" }, restart = new[] { "tri" }, sq = new[] { "sq" }, small = new[] { "L1  R1" } },
		["xbox"] = new BtnSet { move = new[] { "stick" }, confirm = new[] { "A" }, dash = new[] { "A", "RB" }, pause = new[] { "Menu" }, back = new[] { "B" }, restart = new[] { "Y" }, sq = new[] { "X" }, small = new[] { "LB  RB" } },
		["kb"] = new BtnSet { move = new[] { "الأسهم" }, confirm = new[] { "Enter" }, dash = new[] { "مسافة" }, pause = new[] { "Esc" }, back = new[] { "Backspace" }, restart = new[] { "R" }, sq = new[] { "T" }, small = new[] { "Q  E" } },
	};
}
