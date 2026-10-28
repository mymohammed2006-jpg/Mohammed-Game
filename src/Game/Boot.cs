using Godot;
using System;
using System.Linq;

namespace MG;

// original index.html lines 8030-8054: saved best letters and loading the songs
public static partial class G
{
	public static void LoadBests()
	{
		foreach (var L in LEVELS.Concat(FRIENDS).Concat(HCLV).Concat(BOSSLV))
		{
			if (L.name == null) continue;
			var v = localStorage.getItem("mg_best_" + L.name);
			if (RANKS.Contains(v))
			{
				if (localStorage.getItem("mg_bestez_" + L.name) == "1") { localStorage.setItem("mg_beste_" + L.name, v); localStorage.removeItem("mg_best_" + L.name); }
				else L.best = v;
			}
			localStorage.removeItem("mg_bestez_" + L.name);
			var ve = localStorage.getItem("mg_beste_" + L.name); if (RANKS.Contains(ve)) L.bestE = ve;
		}
		redoneSnap();   // v52: the redesigned levels played before (UI/Redone.cs)
	}

	public static AudioStream LoadSong(string file)
	{
		try { return GD.Load<AudioStream>("res://songs/" + file + ".mp3"); }
		catch (Exception e) { GD.PushWarning("song " + file + ": " + e.Message); return null; }
	}
	public static void LoadSongs()
	{
		MENUS["normal"].buf = LoadSong("SONG_MENU"); applyMenu();
		MENUS["easy"].buf = LoadSong("SONG_EZ"); applyMenu();
		foreach (var L in LEVELS.Concat(FRIENDS).Concat(HCLV).Concat(BOSSLV))
		{
			if (!SONG_FILE.TryGetValue(L.id, out var f)) continue;
			var b = LoadSong(f); if (b != null) L.buf = b; else L.err = true;
		}
	}
}
