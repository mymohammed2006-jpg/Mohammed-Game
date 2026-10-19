using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MG;

// What the old PC app (Electron) did around the game on Windows, kept the same:
// one game at a time (a second start brings the open one to the front), and the same app id
// (com.mohammed.game) so the taskbar pin and the shortcuts the old installer made stay the same app.
public static class WinApp
{
	static System.Threading.Mutex single;

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string id);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string title);
	[DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr w);
	[DllImport("user32.dll")] static extern bool ShowWindow(IntPtr w, int cmd);
	[DllImport("user32.dll")] static extern bool IsIconic(IntPtr w);

	// false: another copy is already open (it was brought to the front), so this one must quit
	public static bool Start()
	{
		if (!OS.HasFeature("windows")) return true;
		try { SetCurrentProcessExplicitAppUserModelID("com.mohammed.game"); } catch (Exception) { }
		try
		{
			single = new System.Threading.Mutex(true, "Local\\MohammedGame.single", out bool first);
			if (!first)
			{
				var w = FindWindow(null, "Mohammed Game");
				if (w != IntPtr.Zero) { if (IsIconic(w)) ShowWindow(w, 9); SetForegroundWindow(w); }
				return false;
			}
		}
		catch (Exception) { }
		CleanOldUpdates();
		return true;
	}

	// leftovers of earlier updates: the old app's update cache, and our own downloaded installer once it has run
	static void CleanOldUpdates()
	{
		try
		{
			var old = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "mohammed-game-updater");
			if (System.IO.Directory.Exists(old)) System.IO.Directory.Delete(old, true);
		}
		catch (Exception) { }
		try
		{
			var mine = ProjectSettings.GlobalizePath("user://update/Mohammed-Game-Setup.exe");
			if (System.IO.File.Exists(mine)) System.IO.File.Delete(mine);
		}
		catch (Exception) { }
	}
}
