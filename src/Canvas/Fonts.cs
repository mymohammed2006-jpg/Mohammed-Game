using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MG.Canvas;

// The game's fonts (same files the PC app ships) plus the system fonts the browser falls back to.
public static class Fonts
{
	static readonly Dictionary<string, Font> faces = new();
	static Font sysFallback, sysBold;

	static FontFile Load(string path)
	{
		// the raw font file (kept as is in the export, see its .import), same as LoadDynamicFont reads it
		var f = new FontFile { Data = Godot.FileAccess.GetFileAsBytes(path) };
		// like the browser on Windows: no hinting, fractional glyph positions
		f.Hinting = TextServer.Hinting.None;
		f.SubpixelPositioning = TextServer.SubpixelPositioning.OneQuarter;
		f.Antialiasing = TextServer.FontAntialiasing.Gray;
		f.GenerateMipmaps = false;
		return f;
	}

	static Font System()
	{
		if (sysFallback != null) return sysFallback;
		var s = new SystemFont();
		s.FontNames = new[] { "Segoe UI", "Tahoma", "Segoe UI Symbol", "Segoe UI Emoji", "Inter", "DejaVu Sans", "Noto Sans", "sans-serif" };
		s.Hinting = TextServer.Hinting.None;
		s.SubpixelPositioning = TextServer.SubpixelPositioning.OneQuarter;
		sysFallback = s;
		return s;
	}

	// the browser asks the fallback font for the same weight, so "✓" in 800 Tajawal comes out bold
	static Font SystemBold()
	{
		if (sysBold != null) return sysBold;
		var s = new SystemFont();
		s.FontNames = new[] { "Segoe UI", "Tahoma", "Segoe UI Symbol", "Segoe UI Emoji", "Inter", "DejaVu Sans", "Noto Sans", "sans-serif" };
		s.FontWeight = 700;
		s.Hinting = TextServer.Hinting.None;
		s.SubpixelPositioning = TextServer.SubpixelPositioning.OneQuarter;
		sysBold = s;
		return s;
	}

	static Font Pair(string a, string b, bool bold = false)
	{
		var f1 = Load(a);
		var f2 = Load(b);
		f1.Fallbacks = new Godot.Collections.Array<Font> { f2, bold ? SystemBold() : System() };
		return f1;
	}

	public static void Init()
	{
		const string D = "res://assets/fonts/";
		faces["tajawal:500"] = Pair(D + "tajawal500_lat.woff2", D + "tajawal500_ar.woff2");
		faces["tajawal:800"] = Pair(D + "tajawal800_lat.woff2", D + "tajawal800_ar.woff2", true);
		faces["audiowide:400"] = Pair(D + "audiowide_lat.woff2", D + "audiowide_latext.woff2");
	}

	// CSS font matching for the weights we have
	public static Font Face(FontSpec fs)
	{
		string fam = fs.family.ToLowerInvariant();
		if (fam == "tajawal")
		{
			// wanted <= 500 picks 500 (nearest available), heavier picks 800
			return faces[fs.weight <= 500 ? "tajawal:500" : "tajawal:800"];
		}
		if (fam == "audiowide") return faces["audiowide:400"];
		return System();
	}

	// "800 30px Tajawal", "bold 13px X", "17px \"Tajawal\",Segoe UI"
	public static bool ParseFont(string s, out FontSpec fs)
	{
		fs = new FontSpec { weight = 400, size = 10, family = "sans-serif" };
		if (string.IsNullOrWhiteSpace(s)) return false;
		int px = s.IndexOf("px", StringComparison.Ordinal);
		if (px < 0) return false;
		int st = px - 1;
		while (st >= 0 && (char.IsDigit(s[st]) || s[st] == '.' || s[st] == '-' || s[st] == 'e' || s[st] == 'E' || s[st] == '+')) st--;
		if (!double.TryParse(s.Substring(st + 1, px - st - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var size)) return false;
		fs.size = size;
		foreach (var tok in s.Substring(0, st + 1).Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			if (tok == "bold") fs.weight = 700;
			else if (tok == "bolder") fs.weight = 700;
			else if (tok == "normal") { }
			else if (int.TryParse(tok, out var w)) fs.weight = w;
		}
		string fam = s.Substring(px + 2).Trim();
		// skip "/line-height"
		if (fam.StartsWith("/")) { int sp = fam.IndexOf(' '); fam = sp > 0 ? fam.Substring(sp + 1) : ""; }
		string first = "sans-serif";
		foreach (var part in fam.Split(','))
		{
			string f = part.Trim().Trim('"', '\'');
			string fl = f.ToLowerInvariant();
			if (fl == "tajawal" || fl == "audiowide") { first = f; break; }
			if (first == "sans-serif" && f.Length > 0) first = f;
		}
		fs.family = first;
		return true;
	}

	// width like measureText(): advances scale linearly, so measure big and scale
	const int REF = 100;
	static readonly Dictionary<(Font, string, bool), float> widthCache = new();
	public static double Measure(FontSpec fs, string text, bool rtl)
	{
		if (string.IsNullOrEmpty(text)) return 0;
		var f = Face(fs);
		var key = (f, text, rtl);
		if (!widthCache.TryGetValue(key, out var w100))
		{
			w100 = f.GetStringSize(text, HorizontalAlignment.Left, -1, REF, TextServer.JustificationFlag.None, rtl ? TextServer.Direction.Rtl : TextServer.Direction.Ltr).X;
			if (widthCache.Count > 20000) widthCache.Clear();
			widthCache[key] = w100;
		}
		return w100 * fs.size / REF;
	}

	// OS/2 typo ascent/descent normalized to 1em, what Chrome uses for textBaseline top/middle/bottom
	static (double asc, double desc) Typo(FontSpec fs)
	{
		string fam = fs.family.ToLowerInvariant();
		if (fam == "tajawal") return (0.643, 0.357);
		if (fam == "audiowide") return (2027.0 / 2611, 584.0 / 2611);
		return (0.8, 0.2);
	}
	// how far below y the alphabetic baseline goes for a textBaseline value
	public static double BaselineShift(FontSpec fs, string baseline)
	{
		var (a, d) = Typo(fs);
		double s = fs.size;
		return baseline switch
		{
			"top" => a * s,
			"hanging" => a * s * 0.8,
			"middle" => (a - d) / 2 * s,
			"bottom" or "ideographic" => -d * s,
			_ => 0,
		};
	}

	public static double Ascent(FontSpec fs) => Face(fs).GetAscent(REF) * fs.size / REF;
	public static double Descent(FontSpec fs) => Face(fs).GetDescent(REF) * fs.size / REF;
}
