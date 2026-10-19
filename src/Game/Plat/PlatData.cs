using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace MG;

// A platformer level is a data file: data/plat_<id>.json (new levels never need new code).
// map: rows of characters, one per 40 px tile (32 x 18 fills the screen):
//   #  wall / floor        ^  spikes on the floor (pink)   v  spikes under a ceiling (pink)
//   B  beat block: pink on beats 1-2 of every 4, see-through warning before   b  the same on beats 3-4
//   S  start   G  goal (opens when every piece is taken)   C  checkpoint   1-9  a piece of the song
// saws: pink saws going back and forth between two tiles, there and back in `beats` beats
// stems: the song's parts (songs/<name>.mp3, played together and looped); on = which ones play from the start;
// theme: the level's own touch behind the map ("disco"). pieces: map digit -> the stem it adds. loop: the looped part of the files (seconds), a whole number of bars; spb: seconds per beat
public class PlatSaw { public double x0 { get; set; } public double y0 { get; set; } public double x1 { get; set; } public double y1 { get; set; } public double beats { get; set; } = 4; public double r { get; set; } = 16; public double ph { get; set; } }
public class PlatLevel
{
	public string id { get; set; }
	public string name { get; set; }
	public string song { get; set; }
	public string by { get; set; }
	public string[] stems { get; set; }
	public string[] stemNames { get; set; }
	public bool[] on { get; set; }
	public Dictionary<string, int> pieces { get; set; }
	public double[] loop { get; set; }
	public double spb { get; set; }
	public string[] map { get; set; }
	public string theme { get; set; }   // the level's own touch behind the map ("disco")
	public List<PlatSaw> saws { get; set; } = new();
	[System.Text.Json.Serialization.JsonIgnore] public AudioStream[] bufs;
	[System.Text.Json.Serialization.JsonIgnore] public Level lv;   // its row with the other levels (the best letter, Normal only)
	[System.Text.Json.Serialization.JsonIgnore] public string best => lv.best;
}

public static partial class G
{
	public static readonly string[] PLAT_IDS = { "P1" };
	static List<PlatLevel> _plats;
	public static List<Level> PLAT_LV => PLATS.Select(p => p.lv).ToList();
	public static List<PlatLevel> PLATS
	{
		get
		{
			if (_plats != null) return _plats;
			_plats = new();
			foreach (var id in PLAT_IDS)
			{
				try
				{
					using var f = FileAccess.Open($"res://data/plat_{id}.json", FileAccess.ModeFlags.Read);
					var L = JsonSerializer.Deserialize<PlatLevel>(f.GetAsText());
					L.bufs = L.stems.Select(s => LoadSong(s)).ToArray();
					L.lv = new Level { id = L.id, name = "البلاتفورم - " + L.name, song = L.song, plat = true };
					var b = localStorage.getItem("mg_best_" + L.lv.name) ?? localStorage.getItem("mg_plbest_" + L.id);   // mg_plbest_ = the first test builds
					if (RANKS.Contains(b)) { L.lv.best = b; localStorage.setItem("mg_best_" + L.lv.name, b); }
					_plats.Add(L);
				}
				catch (Exception e) { GD.PushWarning("plat " + id + ": " + e.Message); }
			}
			return _plats;
		}
	}
}
