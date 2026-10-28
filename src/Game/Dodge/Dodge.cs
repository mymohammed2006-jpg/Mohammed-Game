using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace MG;

// A new dodge level (after L11, and the friends' levels after F8) is a data file: data/dodge_<id>.json. The 19 old levels stay as code.
//   id, name, song (shown on the card), by and ver (a friends' level: who picked it, who verified it), file (songs/<file>.mp3), bpm + off (or beats), end (beat of the win), cps (checkpoint beats),
//   ez: the easy settings by name (ezg, ezc, ez, ezlong, ezsp, ezwn, ezgw), energy: one number per bar (0-1, the pulse),
//   events: [{ "b": beat, "k": kind, "keep": true|false, ...the kind's own fields }], sorted by b.
//   (keepNew: true = also comes back after a death when it is already out at the restart point, v51; see Play.cs beginRun)
//   (or no events: the level's own code makes them, registered with DodgeBuild(id, ...))
// A kind is code: DodgeKind("name", e => () => { ...add hazards... }) in the level's own file (src/Game/Dodge/<id>.cs).
// The handler gets the event's fields (e.num("x"), e.str("dir"), e.nums("cols")) and returns what runs at beat b.
public class DodgeEv
{
	readonly JsonElement j;
	public DodgeEv(JsonElement j) { this.j = j; }
	public bool has(string k) => j.TryGetProperty(k, out _);
	public double num(string k, double d = 0) => j.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : d;
	public int i(string k, int d = 0) => (int)num(k, d);
	public bool flag(string k, bool d = false) => j.TryGetProperty(k, out var v) ? v.ValueKind == JsonValueKind.True : d;
	public string str(string k, string d = null) => j.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : d;
	public double[] nums(string k) => j.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Select(x => x.GetDouble()).ToArray() : Array.Empty<double>();
	public double b => num("b");
	public string k => str("k");
}

public static partial class G
{
	// the new dodge levels, in the order they come after L11
	public static readonly string[] DODGE_IDS = { "L12", "L13", "L14", "L15", "L16", "L17", "L18", "L19", "L20", "L21", "L22" };
	// the new friends' levels, in the order they come after F8
	public static readonly string[] DODGE_FRIENDS = { "F9", "F10", "F11" };

	static Dictionary<string, Func<DodgeEv, Action>> _dk;
	static Dictionary<string, Func<DodgeEv, Action>> DK => _dk ??= new();
	public static int DodgeKind(string name, Func<DodgeEv, Action> f) { DK[name] = f; return 0; }
	// a level whose events are made by code instead (its data file has no "events"): DodgeBuild("L14", () => buildLevel14())
	static Dictionary<string, Func<List<Ev>>> _db;
	static Dictionary<string, Func<List<Ev>>> DB => _db ??= new();
	public static int DodgeBuild(string id, Func<List<Ev>> f) { DB[id] = f; return 0; }

	public class DodgeFile
	{
		public string id { get; set; }
		public string name { get; set; }
		public string song { get; set; }
		public string by { get; set; }
		public string ver { get; set; }
		public string file { get; set; }
		public double bpm { get; set; }
		public double off { get; set; }
		public double[] beats { get; set; }
		public double end { get; set; }
		public double[] cps { get; set; }
		public Dictionary<string, double> ez { get; set; } = new();
		public double[] energy { get; set; }
		public JsonElement events { get; set; }
	}

	// v52: a level whose attacks changed keeps the old one as data/dodge_<id>_v51.json (a code-built one: DodgeBuild("<id>_v51", ...)),
	// replays recorded before (v < 7) play on it, as they were recorded
	public static Level LoadDodge(string id)
	{
		var L = LoadDodge(id, "");
		if (L != null && FileAccess.FileExists($"res://data/dodge_{id}_v51.json")) { L.v51 = LoadDodge(id, "_v51"); if (L.v51 != null) L.v51.isV51 = true; }
		return L;
	}
	static Level LoadDodge(string id, string sfx)
	{
		using var f = FileAccess.Open($"res://data/dodge_{id}{sfx}.json", FileAccess.ModeFlags.Read);
		if (f == null) return null;
		var D = JsonSerializer.Deserialize<DodgeFile>(f.GetAsText());
		var evs = D.events.ValueKind == JsonValueKind.Array ? D.events.EnumerateArray().Select(e => new DodgeEv(e.Clone())).ToList() : null;
		Func<double> ez(string k) => D.ez.TryGetValue(k, out var v) ? () => v : null;
		if (sfx == "") SONG_FILE[D.id] = D.file;
		return new Level
		{
			id = D.id, name = D.name, song = D.song, by = D.by, ver = D.ver, bpm = D.bpm, off = D.off, beats = D.beats, end = D.end, cps = D.cps, energy = D.energy,
			ezg = ez("ezg"), ezc = ez("ezc"), ez = ez("ez"), ezlong = ez("ezlong"), ezsp = ez("ezsp"), ezwn = ez("ezwn"), ezgw = ez("ezgw"),
			buildName = "dodge" + D.id,
			build = () =>
			{
				if (evs == null) return DB.TryGetValue(D.id + sfx, out var bf) ? bf() : new List<Ev>();
				var E = new List<Ev>();
				foreach (var e in evs)
				{
					if (!DK.TryGetValue(e.k ?? "", out var mk)) { GD.PushWarning($"dodge {D.id}: no kind '{e.k}'"); continue; }
					E.Add(new Ev { b = e.b, keep = e.flag("keep"), keepNew = e.flag("keepNew"), f = mk(e) });
				}
				JS.sort(E, (a, b) => a.b - b.b); return E;
			},
		};
	}
	static void AddDodgeLevels()
	{
		foreach (var id in DODGE_IDS)
		{
			try { var L = LoadDodge(id); if (L != null) LEVELS.Add(L); }
			catch (Exception e) { GD.PushWarning("dodge " + id + ": " + e.Message); }
		}
		foreach (var id in DODGE_FRIENDS)
		{
			try { var L = LoadDodge(id); if (L != null) FRIENDS.Add(L); }
			catch (Exception e) { GD.PushWarning("dodge " + id + ": " + e.Message); }
		}
	}
}
