using Godot;
using System.Linq;
using MG;

// -- --leveldb-test <dir> <origin>: prints the localStorage found in a Chromium leveldb folder
public static class LevelDbTest
{
	public static void Run(string dir, string origin)
	{
		var d = LevelDb.LocalStorage(dir, origin);
		foreach (var kv in d.OrderBy(k => k.Key, System.StringComparer.Ordinal)) GD.Print(kv.Key, " = ", kv.Value.Length > 60 ? kv.Value[..60] + "...(" + kv.Value.Length + ")" : kv.Value);
		GD.Print("count ", d.Count);
		System.IO.File.WriteAllText(dir.TrimEnd('/') + ".json", System.Text.Json.JsonSerializer.Serialize(d));
	}
}
