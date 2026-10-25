using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// The co-op boss mode (طور الزعيم), Mohammed 10-25: one boss level for 1-4 players, in its own list.
// - A boss level is a dodge data file (data/dodge_B1.json) with 3 hearts, no Easy and no checkpoint (a down = from the start).
public static partial class G
{
	public static readonly string[] BOSS_IDS = { "B1" };
	public static readonly List<Level> BOSSLV = new();
	public const int BOSS_HP = 3;
	static void AddBossLevels()
	{
		foreach (var id in BOSS_IDS)
		{
			try { var L = LoadDodge(id); if (L != null) { L.boss = true; BOSSLV.Add(L); } }
			catch (Exception e) { Godot.GD.PushWarning("boss " + id + ": " + e.Message); }
		}
	}
}
