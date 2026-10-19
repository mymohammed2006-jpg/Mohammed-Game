using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// New in the Godot version: the mouse in the menus. Pointing at something selects it, the left click picks it,
// the right click goes back, the wheel moves up and down, and the buttons at the bottom of the screen can be clicked.
// Each screen marks its clickable places (zones) while it draws; the next frame's input uses them. Inside a level the
// mouse does nothing. With classicUi (the comparison tools) no zones are made.
public static partial class G
{
	public class Zone { public double x, y, w, h; public Action hover, click; public string act; public Action<double> drag; public bool rep; }
	static List<Zone> zonesDraw = new(), zones = new();
	public static readonly List<(int k, double x, double y)> mouseQ = new();   // 0 move, 1 left down, 2 left up, 3 right down, 4 wheel up, 5 wheel down
	static Zone dragZ, holdZ; static double holdZT, holdZAcc;
	static string[] curSt;   // made on first use: MENUISH is in another file and may not be set up yet
	static string[] CUR_ST => curSt ??= MENUISH.Concat(new[] { "news", "practice", "paused", "pauseSet", "dead", "win", "prEnd" }).ToArray();
	// lists that go right to left: the wheel moves along them
	static readonly string[] WHEEL_X = { "levels", "friends", "skins", "chal", "news" };

	public static void zone(double x, double y, double w, double h, Action hover = null, string act = "confirm", Action click = null, Action<double> drag = null, bool rep = false)
	{
		if (classicUi) return;
		zonesDraw.Add(new Zone { x = x, y = y, w = w, h = h, hover = hover, act = act, click = click, drag = drag, rep = rep });
	}
	public static List<Zone> zonesNow => zones;   // tests
	static void zonesFlip() { zones = zonesDraw; zonesDraw = new List<Zone>(); }
	static (double x, double y) toGame(double px, double py) => ((px * resK - offX) / scale, (py * resK - offY) / scale);
	static Zone zoneAt(double gx, double gy)
	{
		for (int i = zones.Count - 1; i >= 0; i--) { var z = zones[i]; if (gx >= z.x && gx <= z.x + z.w && gy >= z.y && gy <= z.y + z.h) return z; }
		return null;
	}
	public static bool mouseOnZone(double px, double py) { if (!CUR_ST.Contains(state)) return false; var (gx, gy) = toGame(px, py); return zoneAt(gx, gy) != null; }
	static void inject(Inp inp, string act)
	{
		switch (act)
		{
			case "confirm": inp.confirm = true; break; case "back": inp.back = true; break; case "restart": inp.restart = true; break;
			case "sq": inp.sq = true; break; case "pz": inp.pz = true; break; case "left": inp.left = true; break; case "right": inp.right = true; break;
			case "up": inp.up = true; break; case "down": inp.down = true; break;
		}
	}
	// the mouse's clicks (a zone's click runs, then its act is pressed like a button) and moves since the last frame, as if they were buttons
	public static void mouseApply(Inp inp)
	{
		if (classicUi || !CUR_ST.Contains(state)) { mouseQ.Clear(); dragZ = holdZ = null; return; }
		foreach (var (k, px, py) in mouseQ)
		{
			var (gx, gy) = toGame(px, py);
			if (k == 0)
			{
				if (dragZ != null) { dragZ.drag(gx); continue; }
				if (!usingPad) zoneAt(gx, gy)?.hover?.Invoke();
			}
			else if (k == 1)
			{
				if (numRow != null) numDone(true);
				var z = zoneAt(gx, gy); if (z == null) continue;
				z.hover?.Invoke();
				if (z.drag != null) { dragZ = z; z.drag(gx); }
				else
				{
					z.click?.Invoke(); if (z.act != null) inject(inp, z.act);
					if (z.rep && z.click != null) { holdZ = z; holdZT = 0; holdZAcc = 0; }
				}
			}
			else if (k == 2) { dragZ = null; holdZ = null; }
			else if (k == 3) { if (numRow != null) numDone(false); else inject(inp, "back"); }
			else if (k is 4 or 5) { bool dn = k == 5; inject(inp, WHEEL_X.Contains(state) ? (dn ? "left" : "right") : (dn ? "down" : "up")); }
		}
		mouseQ.Clear();
		// holding the click on an arrow repeats it, faster and faster
		if (holdZ != null)
		{
			holdZT += fdt;
			if (holdZT > 0.35) { holdZAcc += Math.Min(60, 8 + (holdZT - 0.35) * 40) * fdt; while (holdZAcc >= 1) { holdZAcc--; holdZ.click(); } }
		}
	}
	// the action of a button picture at the bottom (the same button in every kind of controller)
	static string iconAct(string[] ic)
	{
		var K = BTN[kind];
		if (ic == K.confirm) return "confirm"; if (ic == K.back) return "back"; if (ic == K.restart) return "restart"; if (ic == K.sq) return "sq"; if (ic == K.pause) return "pz";
		string n = ic.Length > 0 ? ic[0] : "";
		return n == "Enter" ? "confirm" : n == "Esc" ? "pz" : n == "Backspace" ? "back" : null;
	}
}
