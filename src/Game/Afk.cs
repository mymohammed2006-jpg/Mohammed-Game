using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static MG.JS;

namespace MG;

// original index.html lines 6496-6544: fixed extra attacks that filled the AFK spots (v37/v38 replays, afkV 2) and,
// in the current version, only the knives on F7's middle line (AFKNEW).
// Each level registers its own maker in its level file:  static readonly int _afk = AfkReg("buildLevel3", (x, y, b, v, w) => ...);
public delegate Hz[] AfkMaker(double x, double y, double b, int v, double w);

public static partial class G
{
	public static bool afkOff = true, afkNoFill = false;
	public static int afkV = 3;   // 1 old replay, 2 a v37/v38 replay (plays on that shape), 3 now
	static List<double[]> afkL = new(); static int afkI = 0; static string afkK = "";

	public static readonly Dictionary<string, AfkMaker> AFKMK = new();
	public static int AfkReg(string build, AfkMaker mk) { AFKMK[build] = mk; return 0; }
	public static Hz[] One(Hz h) => new[] { h };

	public static double AFKWN() => Math.Max(2, Math.Ceiling(1.0 / SPB));
	public static Hz AFKLZ(double x, double y, double b, int v, double w) => Laser(new LaserO { x = x, y = y, ang = v != 0 ? Math.PI / 2 : 0, w = 22, len = 3200, tw = B(b - w), ta = B(b), te = B(b + 0.5) });
	// v 0/1 = a line or small attack on the spot, v 2-5 = an attack sweeping the whole screen with an opening
	public static Hz AFKRING(double x, double y, double b, int v, double w) => Ring(x, y, B(b), 300, 16, (v - 2) * Math.PI / 2 + Math.PI / 4, 0.55, w * SPB);
	public static Hz AFKWALL(double x, double y, double b, int v, double w) { string d = "RLDU"[v - 2].ToString(); return GapWall(d, d == "R" || d == "L" ? y : x, 150, 40, d == "R" || d == "L" ? 430 : 300, B(b - w), B(b)); }
	static readonly int _afkL1 = AfkReg("buildLevel1", (x, y, b, v, w) => One(v >= 2 ? AFKRING(x, y, b, v, w) : AFKLZ(x, y, b, v, w)));
	static readonly int _afkL2 = AfkReg("buildLevel2", (x, y, b, v, w) => One(v >= 2 ? AFKWALL(x, y, b, v, w) : AFKLZ(x, y, b, v, w)));
	static readonly int _afkF2 = AfkReg("buildFriend2", (x, y, b, v, w) => One(v >= 2 ? Squeeze(x - 260, y - 160, x + 260, y + 160, B(b - w), B(b), B(b + 0.6)) : AFKLZ(x, y, b, v, w)));
	static readonly int _afkF4 = AfkReg("buildFriend4", (x, y, b, v, w) => One(v >= 2 ? AFKWALL(x, y, b, v, w) : AFKLZ(x, y, b, v, w)));

	// AFKX[build] = {n:[[beat,x,y,v],...], e:[...]}, AFKX._ezk = how much of Easy's own attacks stays; AFKNEW = the v39 F7 knives
	public class AfkSet { public double[][] n { get; set; } public double[][] e { get; set; } }
	static Dictionary<string, AfkSet> AFKX, AFKNEW; static Dictionary<string, double> AFKEZK;
	static void LoadAfk()
	{
		if (AFKX != null) return;
		using var f = Godot.FileAccess.Open("res://data/afk.json", Godot.FileAccess.ModeFlags.Read);
		var root = JsonDocument.Parse(f.GetAsText()).RootElement;
		AFKX = new(); AFKEZK = new(); AFKNEW = new();
		foreach (var p in root.GetProperty("AFKX").EnumerateObject())
		{
			if (p.Name == "_ezk") { foreach (var q in p.Value.EnumerateObject()) AFKEZK[q.Name] = q.Value.GetDouble(); continue; }
			AFKX[p.Name] = p.Value.Deserialize<AfkSet>();
		}
		foreach (var p in root.GetProperty("AFKNEW").EnumerateObject()) AFKNEW[p.Name] = p.Value.Deserialize<AfkSet>();
	}

	public static double afkEz(double k) { LoadAfk(); string key = cur.buildName ?? ""; return easy && !afkOff && AFKEZK.TryGetValue(key, out var z) ? k * z : k; }
	public static void afkStart(double fromB)
	{
		LoadAfk();
		afkK = cur.buildName ?? ""; AFKX.TryGetValue(afkK, out var d);
		if (!afkOff && !afkNoFill && d != null && AFKMK.ContainsKey(afkK))
		{
			var l = ((easy ? d.e : d.n) ?? new double[0][]).ToList(); sort(l, (p, q) => p[0] - afkW(p) - (q[0] - afkW(q))); afkL = l;
		}
		else if (afkV == 3 && AFKNEW.TryGetValue(afkK, out var nw)) afkL = (easy ? nw.e : nw.n).ToList();
		else afkL = new List<double[]>();
		afkI = afkL.FindIndex(e => e[0] >= fromB); if (afkI < 0) afkI = afkL.Count;
	}
	// arena squeeze (friends 2) and page turn (friends 5) get a longer warning, like in their levels;
	// Easy: in these levels the added attacks warn longer (at least 1.5 s instead of 1 s), the hit time stays
	static readonly string[] AFKEZL = { "buildLevel1", "buildLevel2", "buildLevel4", "buildLevel5", "buildFriend1", "buildFriend2", "buildFriend5", "buildFriend6" };
	static double AFKWE() => Math.Max(3, Math.Ceiling(1.5 / SPB));
	static double afkWk(string k, bool ez, int v) => (v >= 2 && k == "buildFriend2" ? 3 : v >= 2 && k == "buildFriend5" ? AFKWN() + 0.5 : AFKWN()) + (ez && AFKEZL.Contains(k) ? AFKWE() - AFKWN() : 0);
	static int afkVof(double[] e) => e.Length > 3 ? (int)e[3] : 0;
	static double afkW(double[] e) => afkWk(afkK, easy, afkVof(e));
	public static void afkFire(double t)
	{
		while (afkI < afkL.Count && B(afkL[afkI][0] - afkW(afkL[afkI])) <= t)
		{
			var e = afkL[afkI]; double w = afkW(e); int k = hazards.Count; rngS = hstr(str(runSeed) + ":x" + afkI);
			hazards.AddRange(AFKMK[afkK](e[1], e[2], e[0], afkVof(e), w));
			for (int j = k; j < hazards.Count; j++) { hazards[j].rid = "x" + afkI + "." + (j - k); hazards[j].ezK = "x" + afkI; hazards[j].ezN = 0; }
			afkI++;
		}
	}
}
