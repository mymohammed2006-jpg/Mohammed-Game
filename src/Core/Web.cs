using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace MG;

// performance.now(): milliseconds since start. Tests drive it by hand.
public static class performance
{
	static readonly Stopwatch sw = Stopwatch.StartNew();
	public static double? manual;   // set by the test runner
	public static double now() => manual ?? sw.Elapsed.TotalMilliseconds;
}

// localStorage: string keys and values, kept in a JSON file in the user folder
public static class localStorage
{
	static Dictionary<string, string> data;
	static bool dirty;
	static double lastSave;
	public static string FilePath = "user://storage.json";
	public static bool Memory;   // tests: never touch the disk

	static void Load()
	{
		if (data != null) return;
		data = new Dictionary<string, string>();
		if (Memory) return;
		try
		{
			using var f = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
			if (f != null)
			{
				var d = JsonSerializer.Deserialize<Dictionary<string, string>>(f.GetAsText());
				if (d != null) data = d;
			}
		}
		catch (Exception e) { GD.PushWarning("localStorage load: " + e.Message); }
	}
	public static string getItem(string k) { Load(); return data.TryGetValue(k, out var v) ? v : null; }
	public static void setItem(string k, string v) { Load(); if (data.TryGetValue(k, out var o) && o == v) return; data[k] = v; dirty = true; }
	public static void removeItem(string k) { Load(); if (data.Remove(k)) dirty = true; }
	public static IEnumerable<string> keys() { Load(); return data.Keys; }
	public static void clear() { Load(); if (data.Count > 0) { data.Clear(); dirty = true; } }

	// written at most every 0.5 s, and when the game closes
	public static void Flush(bool force = false)
	{
		if (!dirty || Memory) return;
		double t = performance.now();
		if (!force && t - lastSave < 500) return;
		lastSave = t; dirty = false;
		try
		{
			string tmp = FilePath + ".tmp";
			using (var f = FileAccess.Open(tmp, FileAccess.ModeFlags.Write)) f.StoreString(JsonSerializer.Serialize(data));
			DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(tmp), ProjectSettings.GlobalizePath(FilePath));
		}
		catch (Exception e) { GD.PushWarning("localStorage save: " + e.Message); dirty = true; }
	}
}
