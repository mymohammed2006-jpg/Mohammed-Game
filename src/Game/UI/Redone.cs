using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static MG.JS;

namespace MG;

// v52 (his words 11:29 UTC 10-28): the levels with a new design (they keep their v51 design for old replays, Level.v51) get a yellow
// "تم إعادة تصميمها" tag on their card for the players who had won them before (a letter on this PC when v52 first started) until
// they go into the level,
// and their ratings start again: asked again on the next win, the old design's ratings left out and deleted (Rate.cs)
public static partial class G
{
	public static HashSet<string> _redoneSeen;   // public for test/PvRun.cs
	// once, the first time v52 starts: the redesigned levels this PC already has a letter on
	public static void redoneSnap()
	{
		if (localStorage.getItem("mg_redone52") is string s) { try { _redoneSeen = JsonSerializer.Deserialize<HashSet<string>>(s); } catch (Exception) { } }
		if (_redoneSeen != null) return;
		_redoneSeen = LEVELS.Concat(FRIENDS).Concat(BOSSLV).Where(L => L.v51 != null && (L.best != null || L.bestE != null)).Select(L => L.id).ToHashSet();
		localStorage.setItem("mg_redone52", JsonSerializer.Serialize(_redoneSeen));
	}
	// his words 11:58 UTC 10-28: the tag goes when the player goes into the level (a run or the practice), finished or not
	public static void redoneClear(Level L)
	{
		if (L == null || _redoneSeen == null || !_redoneSeen.Remove(L.id)) return;
		localStorage.setItem("mg_redone52", JsonSerializer.Serialize(_redoneSeen));
	}
	public static bool redoneTagOn(Level L) => L?.v51 != null && _redoneSeen != null && _redoneSeen.Contains(L.id);
	public static bool redoneLv(string id) => LEVELS.Concat(FRIENDS).Concat(BOSSLV).Any(L => L.id == id && L.v51 != null);
	// the tag: a yellow pill on the card's top edge
	public static void redoneTag(double x, double top, double now)
	{
		const string T = "تم إعادة تصميمها"; g.font = $"800 19px {FONT}"; double w = g.measureText(T).width + 40, h = 34, y = top - h / 2;
		double p = 0.5 + 0.5 * JS.sin(now / 420);
		rr(x - w / 2, y, w, h, h / 2); g.fillStyle = "#ffd54a"; g.shadowColor = "#ffd54a"; g.shadowBlur = 10 + 8 * p; g.fill(); g.shadowBlur = 0;
		g.strokeStyle = "rgba(255,255,255,0.85)"; g.lineWidth = 1.5; g.stroke();
		txt(T, x, y + h / 2 + 1, 19, "#2a1a00", 800);
	}
}
