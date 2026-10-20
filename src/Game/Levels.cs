using System;
using System.Collections.Generic;
using System.Linq;

namespace MG;

// original index.html lines 7103-7138: the level tables
public static partial class G
{
	static Func<double> V(double v) => () => v;
	public static readonly List<Level> LEVELS = new()
	{
		new Level { name = "المرحلة الأولى", song = "Harder Things", bpm = 132, off = 0.065, end = 312, cps = new double[] { 0, 100, 196 }, energy = E1, ezg = V(1), ezc = V(1), ez = V(0.5), build = () => buildLevel1(), buildName = "buildLevel1" },
		new Level { name = "المرحلة الثانية", song = "Temple", bpm = 120, off = 0.19, end = 208, cps = new double[] { 0, 80, 152 }, energy = E2, ezg = V(2), ezc = V(1), ez = V(0.55), build = () => buildLevel2(), buildName = "buildLevel2" },
		new Level { name = "المرحلة الثالثة", song = "The Awakened One", bpm = 120, off = 0.14, end = 292, cps = new double[] { 0, 108, 188 }, energy = E3, ezg = V(2), ezc = V(1), ez = V(0.47), ezlong = V(17), build = () => buildLevel3(), buildName = "buildLevel3" },
		new Level { name = "المرحلة الرابعة", song = "Mind Bloom", bpm = 150, off = 0.04, end = 292, cps = new double[] { 0, 112, 208 }, energy = E4, ezg = V(3), ezc = V(1), ez = V(0.5), build = () => buildLevel4(), buildName = "buildLevel4" },
		new Level { name = "المرحلة الخامسة", song = "Runaway", bpm = 87, off = 1.303, end = 199.4, cps = new double[] { 0, 52, 104 }, energy = E5, ezg = V(2), ezc = V(0.5), ez = V(0.6), build = () => buildLevel5(), buildName = "buildLevel5" },
		new Level { name = "المرحلة السادسة", song = "H3ЯД", bpm = 160, off = 0.56, end = 399, cps = new double[] { 0, 132, 268 }, energy = E6, ezg = V(2), ezc = V(2), ez = V(0.4), build = () => buildLevel6(), buildName = "buildLevel6" },
		new Level { name = "المرحلة السابعة", song = "Clownparty", bpm = 144, off = 0.786, end = 320, cps = new double[] { 0, 120, 220 }, energy = E7, ezg = V(1), ezc = V(1), ez = V(0.5), build = () => buildLevel7(), buildName = "buildLevel7" },
		new Level { id = "L8", name = "المرحلة الثامنة", song = "Hips Don't Lie", bpm = 100, off = 0.24, end = 240, cps = new double[] { 0, 96, 160 }, energy = E8, ezg = () => afkV == 2 ? 0 : 1, ezc = V(1), ez = () => afkV == 2 ? 1 : 0.48, build = () => afkV == 2 ? buildLevel8() : buildLevel8pre(), buildName = "buildLevel8" },
		new Level { name = "المرحلة التاسعة", id = "L9", song = "Right Now", bpm = 138, off = 0.237, end = 512, cps = new double[] { 0, 160, 352 }, energy = E9, ezg = V(1), ezc = V(1), ez = V(0.22), build = () => buildLevel9(), buildName = "buildLevel9" },
		new Level { name = "المرحلة العاشرة", id = "L10", song = "Je veux", bpm = 155, off = 0.527, end = 391, cps = new double[] { 0, 112, 264 }, energy = E10, ezg = V(1), ezc = V(1), ez = V(0.5), build = () => buildLevel10(), buildName = "buildLevel10" },
		new Level { name = "المرحلة الحادية عشرة", id = "L11", song = "I Will Survive", bpm = 122.0086, off = 0.1898, end = 304.6, cps = new double[] { 0, 76, 172 }, energy = E11, ezg = V(0), ezc = V(0), ez = V(1), build = () => buildLevel11(), buildName = "buildLevel11" },
	};
	public static readonly List<Level> FRIENDS = new()
	{
		new Level { name = "ليش ضيعت الوفا", by = "Gampet", ver = "Gampet", beats = BF1, end = 340, cps = new double[] { 0, 148, 276 }, energy = EF1, ezg = V(3), ezc = V(1), ez = V(0.5), build = () => buildFriend1(), buildName = "buildFriend1" },
		new Level { name = "وعد مني", by = "ixGha", ver = "Mohammed", bpm = 96, off = 0.005, end = 141, cps = new double[] { 0, 72, 112 }, energy = EF2, ezg = V(2), ezc = V(1), ez = V(0.5), build = () => buildFriend2(), buildName = "buildFriend2" },
		new Level { name = "انتظرتك سنين وأيام طوال", by = "Nice", ver = "Nice", beats = BF3, end = 232, cps = new double[] { 0, 80, 160 }, energy = EF3, ezg = V(1), ezc = V(1), ez = V(0.9), build = () => buildFriend3(), buildName = "buildFriend3" },
		new Level { name = "MEGALOVANIA", by = "Nightmare", ver = "Mohammed", bpm = 120, off = 0.075, end = 310, cps = new double[] { 0, 96, 192 }, energy = EF4, ezg = V(1), ezc = V(1), ez = V(0.72), ezgw = V(1), build = () => buildFriend4(), buildName = "buildFriend4" },
		new Level { name = "Fairytale", by = "Twesii", ver = "Twesii", bpm = 108, off = 0.445, end = 268.9, cps = new double[] { 0, 132, 200 }, energy = EF5, ezg = V(1), ezc = V(1), ez = V(0.7), build = () => buildFriend5(), buildName = "buildFriend5" },
		new Level { name = "طنش", by = "A3D", ver = "Mohammed", bpm = 102, off = 0.137, end = 197, cps = new double[] { 0, 76, 140 }, energy = EF6, ezg = V(1), ezc = V(1.05), ez = V(0.55), build = () => buildFriend6(), buildName = "buildFriend6" },
		new Level { name = "Let Me Love You", by = "AJF", ver = "AJF", bpm = 100, off = 0.225, end = 270, cps = new double[] { 0, 112, 208 }, energy = EF7, ezg = V(1), ezc = V(1), ez = () => afkOff ? 0.37 : 0.29, build = () => afkOff ? buildFriend7v30() : buildFriend7(), buildName = "buildFriend7" },
		new Level { id = "F8", name = "On The Floor", by = "Link", ver = "Nightmare", bpm = 130, off = 0.114, end = 274, cps = new double[] { 0, 122, 186 }, energy = EF8, ezg = V(1), ezc = V(1), ez = V(0.3), build = () => buildFriend8(), buildName = "buildFriend8" },
	};
	public static List<Level> ALL_LV() => LEVELS.Concat(FRIENDS).Where(L => L.name != null).Concat(classicUi ? new List<Level>() : PLAT_LV).ToList();   // the platformer's levels last (not in the v39 comparison runs)
	static G()
	{
		AddDodgeLevels();   // the new levels after L11 are data files (Dodge.cs)
		for (int i = 0; i < LEVELS.Count; i++) LEVELS[i].id ??= "L" + (i + 1);
		for (int i = 0; i < FRIENDS.Count; i++) FRIENDS[i].id ??= "F" + (i + 1);
	}
	// song files (from songs/*.js) for each level id
	public static readonly Dictionary<string, string> SONG_FILE = new()
	{
		["L1"] = "SONG_L1", ["L2"] = "SONG_L2", ["L3"] = "SONG_L3", ["L4"] = "SONG_L4", ["L5"] = "SONG_L5", ["L6"] = "SONG_L6", ["L7"] = "SONG_L7",
		["L8"] = "SONG_L8", ["L9"] = "SONG_L9", ["L10"] = "SONG_L10", ["L11"] = "SONG_L11",
		["F1"] = "SONG_F1", ["F2"] = "SONG_F2", ["F3"] = "SONG_F3", ["F4"] = "SONG_F4", ["F5"] = "SONG_F5", ["F6"] = "SONG_F6", ["F7"] = "SONG_F7", ["F8"] = "SONG_F8",
	};
}
