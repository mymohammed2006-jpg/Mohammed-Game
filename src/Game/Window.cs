using Godot;
using System;
using System.Linq;
using MG.Canvas;
using static MG.JS;

namespace MG;

// original index.html lines 85-99 (resolution, V-Sync, Framecap, FPS counter, resize) and 6847-6854 (fullscreen).
// New in the Godot version: three screen modes (window / borderless / real fullscreen).
public static partial class G
{
	public static Gfx gfx;
	public static Action quitApp = () => { };   // window.close(): the exit row in the main menu
	public static double scale = 1, offX = 0, offY = 0, resK = 1;

	public static string resSet = "auto";
	public static bool vsyncOn = true, fpsShow = false;
	public static readonly int[] FPS_LIST = { 60, 120, 144, 165, 240, 360, 500 };
	public static int fpsCap = 240;
	public const int FPS_MIN = 30, FPS_MAX = 500;   // New in the Godot version: any number in between
	public static string screenMode = "borderless", lastFull = "borderless";
	public static readonly string[] SCREEN_MODES = { "window", "borderless", "full" };

	public static void LoadDisplaySettings()
	{
		var r = localStorage.getItem("mg_res"); if (!string.IsNullOrEmpty(r)) resSet = r;
		if (localStorage.getItem("mg_vsync") == "0") vsyncOn = false;
		fpsShow = localStorage.getItem("mg_fpsshow") == "1";
		double f = num(localStorage.getItem("mg_fps")); if (f == (int)f && (FPS_LIST.Contains((int)f) || (f >= FPS_MIN && f <= FPS_MAX))) fpsCap = (int)f;
		var sm = localStorage.getItem("mg_screen"); if (SCREEN_MODES.Contains(sm)) screenMode = sm;
		var lf = localStorage.getItem("mg_screen_full"); if (lf == "full" || lf == "borderless") lastFull = lf;
		if (screenMode != "window") lastFull = screenMode;
		ApplyFrameSettings(); ApplyScreen();
	}

	public static void setVsync(bool on) { vsyncOn = on; localStorage.setItem("mg_vsync", on ? "1" : "0"); ApplyFrameSettings(); }
	public static void setFpsShow(bool on) { fpsShow = on; localStorage.setItem("mg_fpsshow", on ? "1" : "0"); }
	public static void setFps(int f) { fpsCap = f; localStorage.setItem("mg_fps", str(f)); ApplyFrameSettings(); }
	static void ApplyFrameSettings()
	{
		if (DisplayServer.GetName() == "headless") return;
		DisplayServer.WindowSetVsyncMode(vsyncOn ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
		Engine.MaxFps = fpsCap;
	}

	public static double nativeH() => DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen()).Y;
	public static string[] resList() => new[] { "auto" }.Concat(new[] { "1440", "1080", "720" }.Where(r => num(r) < nativeH())).ToArray();
	public static string resLabel(string r) => r == "auto" ? "تلقائي (" + str(round(nativeH())) + "p)" : r + "p";
	public static void setRes(string r) { resSet = r; localStorage.setItem("mg_res", r); resize(); }

	// the canvas backing size follows the window (in real pixels), capped by the resolution setting
	static int lastW, lastH; static string lastRes;
	public static void resize()
	{
		var sz = gfx.GetViewport() is Window win ? win.Size : new Vector2I(1280, 720);
		int w = Math.Max(1, sz.X), h = Math.Max(1, sz.Y);
		double k = 1; if (resSet != "auto" && h * k > num(resSet)) k = num(resSet) / h;
		int cw = Math.Max(1, (int)round(w * k)), ch = Math.Max(1, (int)round(h * k));
		lastW = w; lastH = h; lastRes = resSet; resK = (double)cw / w;
		gfx.Resize(cw, ch);
		scale = Math.Min(cw / W, ch / H); offX = (cw - W * scale) / 2; offY = (ch - H * scale) / 2;
	}
	public static void ResizeIfNeeded()
	{
		var sz = gfx.GetViewport() is Window win ? win.Size : new Vector2I(1280, 720);
		if (sz.X != lastW || sz.Y != lastH || resSet != lastRes) resize();
	}

	// ---------- screen modes ----------
	public static string screenLabel(string m) => m == "window" ? "نافذة" : m == "borderless" ? "ملء الشاشة بدون حواف" : "ملء الشاشة الكامل";
	public static void setScreen(string m)
	{
		screenMode = m; if (m != "window") { lastFull = m; localStorage.setItem("mg_screen_full", m); }
		localStorage.setItem("mg_screen", m); ApplyScreen();
	}
	// New (Mohammed 10-18): the mouse pointer shows in the menus while you use the keyboard and mouse,
	// and hides inside a level and as soon as you use a controller (any screen mode)
	static bool? cursorOn;
	public static void updateCursor()
	{
		if (DisplayServer.GetName() == "headless") return;
		bool on = CUR_ST.Contains(state) && !usingPad;
		if (cursorOn == on) return; cursorOn = on;
		Input.MouseMode = on ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
	}
	static void ApplyScreen()
	{
		if (DisplayServer.GetName() == "headless") return;
		cursorOn = null; updateCursor();
		if (screenMode == "full") DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		else if (screenMode == "borderless") DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			// like the PC app: 90% of the screen, at most 1280x720, centred
			int scr = DisplayServer.WindowGetCurrentScreen(); var wa = DisplayServer.ScreenGetUsableRect(scr);
			int ww = Math.Min(1280, (int)Math.Round(wa.Size.X * 0.9)), wh = Math.Min(720, (int)Math.Round(wa.Size.Y * 0.9));
			DisplayServer.WindowSetSize(new Vector2I(ww, wh));
			DisplayServer.WindowSetPosition(wa.Position + new Vector2I((wa.Size.X - ww) / 2, (wa.Size.Y - wh) / 2));
		}
	}
	public static bool isFull() => screenMode != "window";
	// the PC app never stops the game for leaving fullscreen
	public static bool fsOK() => true;
	public static void enterFS() { if (!isFull()) setScreen(lastFull); }
	public static void exitFS() { if (isFull()) setScreen("window"); }
	public static void toggleFS() { if (isFull()) exitFS(); else enterFS(); }
}
