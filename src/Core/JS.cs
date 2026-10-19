using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MG;

// JavaScript behaviour the game code relies on, so ported lines keep the same results.
public static partial class JS
{
	static readonly CultureInfo IC = CultureInfo.InvariantCulture;

	// ---------- Math ----------
	// Math.round: halves go up (also for negatives: -2.5 -> -2)
	public static double round(double x) => Math.Floor(x + 0.5);
	// Math.hypot exactly as V8 does it (builtins/math.tq FastMathHypot): scaled by the largest value
	public static double hypot(double a, double b)
	{
		a = Math.Abs(a); b = Math.Abs(b);
		if (double.IsPositiveInfinity(a) || double.IsPositiveInfinity(b)) return double.PositiveInfinity;
		double max = Math.Max(a, b);
		if (double.IsNaN(max)) return double.NaN;
		if (max == 0) return 0;
		return Math.Sqrt((a / max) * (a / max) + (b / max) * (b / max)) * max;
	}
	public static double hypot(double a, double b, double c)
	{
		a = Math.Abs(a); b = Math.Abs(b); c = Math.Abs(c);
		if (double.IsPositiveInfinity(a) || double.IsPositiveInfinity(b) || double.IsPositiveInfinity(c)) return double.PositiveInfinity;
		double max = Math.Max(Math.Max(a, b), c);
		if (double.IsNaN(max)) return double.NaN;
		if (max == 0) return 0;
		double pa = (a / max) * (a / max), pb = (b / max) * (b / max);
		double comp = (pa + pb) - pa - pb;
		double pc = (c / max) * (c / max) - comp;
		return Math.Sqrt(pa + pb + pc) * max;
	}
	public static double sign(double x) => double.IsNaN(x) ? double.NaN : x > 0 ? 1 : x < 0 ? -1 : x;
	public static double max(params double[] v) { double m = double.NegativeInfinity; foreach (var x in v) { if (double.IsNaN(x)) return double.NaN; if (x > m) m = x; } return m; }
	public static double min(params double[] v) { double m = double.PositiveInfinity; foreach (var x in v) { if (double.IsNaN(x)) return double.NaN; if (x < m) m = x; } return m; }
	public static double trunc(double x) => Math.Truncate(x);

	// Math.random: the real game uses the system generator; tests replace it with a seeded one
	// so the original and the port draw the same random numbers in the same order.
	static Random sys = new Random();
	static uint seedState;
	static bool seeded;
	public static void SeedRandom(uint seed) { seeded = true; seedState = seed; }
	public static void UnseedRandom() { seeded = false; }
	public static double random()
	{
		if (!seeded) return sys.NextDouble();
		// mulberry32, same as the test harness on the browser side
		unchecked
		{
			seedState += 0x6D2B79F5;
			uint t = seedState;
			t = (t ^ (t >> 15)) * (1 | t);
			t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
			return (t ^ (t >> 14)) / 4294967296.0;
		}
	}

	// ---------- 32-bit integer ops ----------
	// ToInt32 (what x|0, x>>0, ~~x do)
	public static int i32(double x)
	{
		if (double.IsNaN(x) || double.IsInfinity(x)) return 0;
		if (x >= int.MinValue && x <= int.MaxValue) return (int)x;
		double t = Math.Truncate(x) % 4294967296.0;
		if (t < 0) t += 4294967296.0;
		return unchecked((int)(uint)t);
	}
	public static uint u32(double x) => unchecked((uint)i32(x));
	public static int imul(int a, int b) => unchecked(a * b);

	// ---------- numbers to text ----------
	// String(n) / template literal ${n}
	public static string str(double v)
	{
		if (double.IsNaN(v)) return "NaN";
		if (double.IsPositiveInfinity(v)) return "Infinity";
		if (double.IsNegativeInfinity(v)) return "-Infinity";
		if (v == 0) return "0";
		if (v == Math.Floor(v) && Math.Abs(v) < 1e21) return v.ToString("0", IC);
		double a = Math.Abs(v);
		if (a >= 1e-6 && a < 1e21) return v.ToString("R", IC);
		// exponent form like JS: 1e-7, 1.5e+21
		string s = v.ToString("R", IC).Replace("E", "e");
		if (s.Contains("e") && !s.Contains("e-") && !s.Contains("e+")) s = s.Replace("e", "e+");
		return s;
	}
	public static string str(int v) => v.ToString(IC);
	public static string str(string s) => s;

	// Number.prototype.toFixed: nearest, ties go to the larger value
	public static string toFixed(double v, int d)
	{
		if (double.IsNaN(v)) return "NaN";
		if (Math.Abs(v) >= 1e21) return str(v);
		decimal m;
		try { m = (decimal)v; } catch { return v.ToString("F" + d, IC); }
		// decimal conversion keeps ~28 digits, enough to see the true binary value for game numbers
		m = Math.Round(m, d, MidpointRounding.AwayFromZero);
		string s = m.ToString("F" + d, IC);
		if (v < 0 && !s.StartsWith("-")) s = "-" + s;
		return s;
	}

	// n.toLocaleString('ar-EG'): Arabic-Indic digits, Arabic thousands separator, up to 3 decimals
	public static string ar(double n)
	{
		if (double.IsNaN(n)) return "ليس رقم";
		bool neg = n < 0;
		double a = Math.Abs(n);
		double r = Math.Round(a * 1000, MidpointRounding.AwayFromZero) / 1000;
		string s = r.ToString("#,##0.###", IC);
		var sb = new StringBuilder();
		if (neg && r != 0) sb.Append('؜').Append('-');
		foreach (char c in s)
		{
			if (c >= '0' && c <= '9') sb.Append((char)('٠' + (c - '0')));
			else if (c == ',') sb.Append('٬');
			else if (c == '.') sb.Append('٫');
			else sb.Append(c);
		}
		return sb.ToString();
	}

	public static double parseFloat(string s)
	{
		if (s == null) return double.NaN;
		s = s.TrimStart();
		int i = 0;
		if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
		if (s.Substring(i).StartsWith("Infinity")) return s[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
		while (i < s.Length && char.IsDigit(s[i])) i++;
		if (i < s.Length && s[i] == '.') { i++; while (i < s.Length && char.IsDigit(s[i])) i++; }
		if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
		{
			int j = i + 1;
			if (j < s.Length && (s[j] == '+' || s[j] == '-')) j++;
			int k = j;
			while (k < s.Length && char.IsDigit(s[k])) k++;
			if (k > j) i = k;
		}
		return double.TryParse(s.Substring(0, i), NumberStyles.Float, IC, out var v) ? v : double.NaN;
	}
	// unary + on a string (Number(s))
	public static double num(string s)
	{
		if (s == null) return 0;
		s = s.Trim();
		if (s.Length == 0) return 0;
		return double.TryParse(s, NumberStyles.Float, IC, out var v) ? v : double.NaN;
	}

	// ---------- arrays ----------
	// Array.prototype.sort with a comparator (stable, like V8)
	public static void sort<T>(List<T> a, Func<T, T, double> cmp)
	{
		var idx = Enumerable.Range(0, a.Count).ToList();
		var copy = a.ToList();
		idx.Sort((i, j) => { double c = cmp(copy[i], copy[j]); return c < 0 ? -1 : c > 0 ? 1 : i.CompareTo(j); });
		for (int k = 0; k < a.Count; k++) a[k] = copy[idx[k]];
	}
	public static List<T> slice<T>(List<T> a, int start, int? end = null)
	{
		int n = a.Count;
		int s = start < 0 ? Math.Max(0, n + start) : Math.Min(start, n);
		int e = end == null ? n : end.Value < 0 ? Math.Max(0, n + end.Value) : Math.Min(end.Value, n);
		return e > s ? a.GetRange(s, e - s) : new List<T>();
	}
}
