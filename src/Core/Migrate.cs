using Godot;
using System;
using System.Text.Json;

namespace MG;

// First start after the switch from the old PC app (Electron): bring its saves over.
// Both apps use the same folder (%APPDATA%\Mohammed Game). The old app kept the game's localStorage in
// "Local Storage/leveldb" (origin file://) and the fullscreen choice in settings.json.
public static class Migrate
{
	public static void Run()
	{
		try
		{
			string root = ProjectSettings.GlobalizePath("user://");
			string ldb = System.IO.Path.Combine(root, "Local Storage", "leveldb");
			if (!System.IO.Directory.Exists(ldb)) return;                     // a new player
			string mine = ProjectSettings.GlobalizePath(localStorage.FilePath);
			if (System.IO.File.Exists(mine))
			{
				// already on the new app, unless the old app was played after the new one last saved
				// (a test copy run next to the old app): then its saves are the newer ones, so they come over again
				if (NewestWrite(ldb) <= System.IO.File.GetLastWriteTimeUtc(mine)) return;
				localStorage.clear();
			}
			var old = LevelDb.LocalStorage(ldb, "file://");
			foreach (var kv in old) localStorage.setItem(kv.Key, kv.Value);
			string st = System.IO.Path.Combine(root, "settings.json");
			if (System.IO.File.Exists(st))
			{
				var d = JsonDocument.Parse(System.IO.File.ReadAllText(st)).RootElement;
				if (d.TryGetProperty("fullscreen", out var fs) && fs.ValueKind == JsonValueKind.False) localStorage.setItem("mg_screen", "window");
			}
			localStorage.setItem("mg_migrated", "1");
			localStorage.Flush(true);
			GD.Print("migrated ", old.Count, " saved values from the old app");
		}
		catch (Exception e) { GD.PushWarning("migration: " + e.Message); }
	}

	static DateTime NewestWrite(string dir)
	{
		var t = DateTime.MinValue;
		foreach (var f in System.IO.Directory.GetFiles(dir)) { var w = System.IO.File.GetLastWriteTimeUtc(f); if (w > t) t = w; }
		return t;
	}
}
