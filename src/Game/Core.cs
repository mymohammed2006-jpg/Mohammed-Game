using System;
using System.Collections.Generic;
using MG.Canvas;
using static MG.JS;

namespace MG;

// ---------- the game's objects (JS object literals become these) ----------

// one hazard / decoration. JS: {x,y,...,update(t,dt){},draw(t){},hit(p,t){}}
// Fields the main loop reads are real fields; a hazard's own state usually lives in captured locals.
public class Hz
{
	public double x, y, vx, vy, r;
	public bool dead, ex;
	public int z;   // the drawing layer: < 0 under the others (a floor), > 0 over them; 0 = in the order they came (every level before B1)
	public Action<double, double> update = (t, dt) => { };
	public Action<double> draw = t => { };
	public Func<Player, double, bool> hit = (p, t) => false;
	// set by the main loop (stepHazards / fireEvents / easy mode)
	public string rid;
	public int? rs;
	public int rc;
	public string ezK;
	public int ezN;
	public double ezT;
	public double? ezL;
	// free slots for level code that reads other hazards' properties
	public string kind;
	public Dictionary<string, object> o;
}

public class TrailPt { public double x, y, t; public bool d; }

public class Player
{
	public double x, y, r, hp, inv, hurt, dash, cd, fx, fy, ddx, ddy, ang;
	public double? dt0, sa, sk;
	public List<TrailPt> trail = new();
	public List<TrailPt> skTrail = new();   // the new looks' dash trail (Skins.cs)
	public List<TrailPt> wkTrail = new();   // the walking trail, by time (Skins.cs)
}

public class Particle { public double x, y, vx, vy, life, max, s; public string col; }

// level event: at beat b run f (keep = comes back after a checkpoint)
public class Ev { public double b; public Action f; public bool keep; public bool rnd; }   // rnd: aimed at the player (the test bot learns it only once it fires)

public static partial class G
{
	public static Ctx g;   // the canvas context
	public const double W = 1280, H = 720;
	public static double SPB = 60.0 / 132, OFF = 0.065;
	public static double[] BEATS = null;

	public static double B(double b)
	{
		if (BEATS == null) return OFF + b * SPB;
		int n = BEATS.Length;
		if (b <= 0) return BEATS[0] + b * (BEATS[1] - BEATS[0]);
		if (b >= n - 1) return BEATS[n - 1] + (b - n + 1) * (BEATS[n - 1] - BEATS[n - 2]);
		int i = (int)Math.Floor(b);
		return BEATS[i] + (BEATS[i + 1] - BEATS[i]) * (b - i);
	}
	public static double BT(double t)
	{
		if (BEATS == null) return (t - OFF) / SPB;
		int n = BEATS.Length;
		if (t <= BEATS[0]) return (t - BEATS[0]) / (BEATS[1] - BEATS[0]);
		if (t >= BEATS[n - 1]) return n - 1 + (t - BEATS[n - 1]) / (BEATS[n - 1] - BEATS[n - 2]);
		int lo = 0, hi = n - 1;
		while (hi - lo > 1) { int m = (lo + hi) >> 1; if (BEATS[m] <= t) lo = m; else hi = m; }
		return lo + (t - BEATS[lo]) / (BEATS[lo + 1] - BEATS[lo]);
	}

	public const string PINK = "#ff2a6d", CYAN = "#2de2e6", ARENA = "#000";
	public const string FONT = "\"Tajawal\",\"Segoe UI\",Tahoma,sans-serif";
	public const string TFONT = "\"Audiowide\",\"Segoe UI\",sans-serif";
	public const double TAU = Math.PI * 2;
	public static double clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;

	// seeded randomness for attacks (same numbers in a replay)
	public static int rngS = 1, runSeed = 0;
	public static double rnd()
	{
		unchecked
		{
			rngS = rngS + 0x6D2B79F5;
			int t = imul(rngS ^ (int)((uint)rngS >> 15), 1 | rngS);
			t = (t + imul(t ^ (int)((uint)t >> 7), 61 | t)) ^ t;
			return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
		}
	}
	public static int hstr(string s)
	{
		unchecked
		{
			int h = (int)2166136261;
			for (int i = 0; i < s.Length; i++) { h ^= s[i]; h = imul(h, 16777619); }
			return h;
		}
	}
	public static Func<double> mulberry32(double seed)
	{
		int a = i32(seed);
		return () =>
		{
			unchecked
			{
				a = a + 0x6D2B79F5;
				int t = imul(a ^ (int)((uint)a >> 15), 1 | a);
				t = t + imul(t ^ (int)((uint)t >> 7), 61 | t) ^ t;
				return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
			}
		};
	}

	// ---------- play state ----------
	public static string state = "gate";
	public static List<Hz> hazards = new();
	public static List<Particle> particles = new();
	public static List<Ev> events = new();
	public static int evIdx = 0;
	public static Player player = null;
	public static double songT = 0, checkpoint = 0, shake = 0, flash = 0, pulse = 0, energy = 0.2;
	public static int hitsTotal = 0, deaths = 0;
	public static double gloom = 0, gloomT = 0;
	public static bool dry = false;   // a silent run (easy-mode scan, replay seek): no sounds, no rumble

	public static void rumble(double ms, double s, double w)
	{
		if (dry) return;
		PadRumble(ms, s, w);
	}

	public static void burst(double x, double y, string col, int n, double sp = 300)
	{
		for (int i = 0; i < n; i++)
		{
			double a = random() * TAU, v = sp * (0.3 + random());
			double l = 0.4 + random() * 0.4;
			particles.Add(new Particle { x = x, y = y, vx = JS.cos(a) * v, vy = JS.sin(a) * v, life = l, max = l, col = col, s = 3 + random() * 5 });
		}
	}

	// ---------- shapes shared by many levels ----------
	public static bool obbHit(double px, double py, double pr, double cx, double cy, double hw, double hh, double a)
	{
		double dx = px - cx, dy = py - cy, c = JS.cos(a), s = JS.sin(a);
		double lx = dx * c + dy * s, ly = -dx * s + dy * c;
		return Math.Abs(lx) < hw + pr && Math.Abs(ly) < hh + pr;
	}
	public static void circle(double x, double y, double r) { g.beginPath(); g.arc(x, y, Math.Max(0, r), 0, TAU); g.fill(); }

	public static Hz Bullet(double x, double y, double vx, double vy, double r = 10)
	{
		var h = new Hz { x = x, y = y, vx = vx, vy = vy, r = r };
		h.update = (t, dt) => { h.x += h.vx * dt; h.y += h.vy * dt; if (h.x < -80 || h.x > W + 80 || h.y < -80 || h.y > H + 80) h.dead = true; };
		h.draw = t => { g.fillStyle = "rgba(255,42,109,0.22)"; circle(h.x, h.y, h.r * 1.7 * (1 + 0.2 * pulse)); g.fillStyle = PINK; circle(h.x, h.y, h.r * (1 + 0.15 * pulse)); };
		h.hit = (p, t) => { double dx = p.x - h.x, dy = p.y - h.y, rr = h.r + p.r - 2; return dx * dx + dy * dy < rr * rr; };
		return h;
	}

	// Laser(o): o = {x,y,ang,w,len,tw,ta,te,tile}
	public class LaserO { public double x, y, ang, w, len = 3200, tw, ta, te; public bool tile; public LaserO Clone() => (LaserO)MemberwiseClone(); }
	public static Hz Laser(LaserO o)
	{
		var h = new Hz();
		h.update = (t, dt) => { if (t > o.te + 0.25) h.dead = true; };
		h.draw = t =>
		{
			g.save(); g.translate(o.x, o.y); g.rotate(o.ang);
			if (t < o.ta)
			{
				double k = clamp((t - o.tw) / (o.ta - o.tw), 0, 1); g.fillStyle = $"rgba(255,42,109,{str(0.07 + 0.2 * k)})"; g.fillRect(-o.len / 2, -o.w / 2, o.len, o.w);
				if (o.tile) { g.strokeStyle = "rgba(255,42,109,0.85)"; g.lineWidth = 3; g.strokeRect(-o.len / 2 + 6, -o.w / 2 + 6, o.len - 12, o.w - 12); }
				else { g.fillStyle = "rgba(255,42,109,0.85)"; g.fillRect(-o.len / 2, -1.5, o.len, 3); }
			}
			else if (t < o.te)
			{
				double k = Math.Min(1, (t - o.ta) / 0.06), fl = Math.Max(0, 1 - (t - o.ta) / 0.15);
				double ww = (o.tile ? o.w : o.w * (0.5 + 0.5 * k) * (1 + 0.12 * pulse)); double ll = o.tile ? o.len * (0.85 + 0.15 * k) : o.len;
				g.shadowColor = PINK; g.shadowBlur = 24; g.fillStyle = PINK; g.fillRect(-ll / 2, -ww / 2, ll, ww);
				if (fl > 0) { g.globalAlpha = fl; g.fillStyle = "#fff"; g.fillRect(-ll / 2, -ww / 2, ll, ww); }
			}
			else { double k = 1 - (t - o.te) / 0.25; g.globalAlpha = Math.Max(0, k); g.fillStyle = PINK; double ww = o.w * Math.Max(0, k); g.fillRect(-o.len / 2, -ww / 2, o.len, ww); }
			g.restore();
		};
		h.hit = (p, t) => t >= o.ta && t < o.te && obbHit(p.x, p.y, p.r, o.x, o.y, o.len / 2, o.w / 2, o.ang);
		return h;
	}

	public static Hz Bomb(double x, double y, double tw, double th, int n, double sp)
	{
		var h = new Hz();
		h.update = (t, dt) =>
		{
			if (!h.ex && t >= th)
			{
				h.ex = true; double off = rnd() * TAU;
				for (int i = 0; i < n; i++) { double a = off + i * TAU / n; hazards.Add(Bullet(x, y, JS.cos(a) * sp, JS.sin(a) * sp, 9)); }
				burst(x, y, PINK, 14); shake = Math.Max(shake, 5);
			}
			if (t > th + 0.25) h.dead = true;
		};
		h.draw = t =>
		{
			if (t < th) { double k = clamp((t - tw) / (th - tw), 0, 1); g.strokeStyle = PINK; g.lineWidth = 3; g.globalAlpha = 0.7; g.beginPath(); g.arc(x, y, 42, 0, TAU); g.stroke(); g.globalAlpha = 0.3; g.fillStyle = PINK; circle(x, y, 42 * k); g.globalAlpha = 1; circle(x, y, 6 + 4 * pulse); }
			else { double k = (t - th) / 0.25; g.globalAlpha = Math.Max(0, 1 - k); g.fillStyle = k < 0.3 ? "#fff" : PINK; circle(x, y, 46 + 40 * k); g.globalAlpha = 1; }
		};
		h.hit = (p, t) => t >= th && t < th + 0.12 && hypot(p.x - x, p.y - y) < 44 + p.r;
		return h;
	}

	public class SpinnerO { public double x, y, ts, te, tsw, w1, w2, w, L; public int arms; }
	public static Hz Spinner(SpinnerO o)
	{
		Func<double, double> ang = t => t < o.tsw ? (t - o.ts) * o.w1 : (o.tsw - o.ts) * o.w1 + (t - o.tsw) * o.w2;
		Func<double, bool> live = t => t >= o.ts + 2 * SPB && t < o.te;
		var h = new Hz();
		h.update = (t, dt) => { if (t > o.te + SPB) h.dead = true; };
		h.draw = t =>
		{
			double gr = t < o.ts + 2 * SPB ? clamp((t - o.ts) / (2 * SPB), 0, 1) : t > o.te ? clamp(1 - (t - o.te) / SPB, 0, 1) : 1; bool act = live(t);
			g.save(); g.translate(o.x, o.y); g.rotate(ang(t)); g.fillStyle = PINK; g.globalAlpha = act ? 1 : 0.35; if (act) { g.shadowColor = PINK; g.shadowBlur = 16; }
			double ww = o.w * (act ? 1 + 0.2 * pulse : 1);
			for (int i = 0; i < o.arms; i++) { g.save(); g.rotate(i * TAU / o.arms); g.fillRect(0, -ww / 2, o.L * gr, ww); g.restore(); }
			circle(0, 0, 26 * (1 + 0.15 * pulse) * Math.Max(gr, 0.3)); g.restore();
		};
		h.hit = (p, t) =>
		{
			if (!live(t)) return false; if (hypot(p.x - o.x, p.y - o.y) < 26 + p.r) return true; double a = ang(t);
			for (int i = 0; i < o.arms; i++) { double aa = a + i * TAU / o.arms; if (obbHit(p.x, p.y, p.r, o.x + JS.cos(aa) * o.L / 2, o.y + JS.sin(aa) * o.L / 2, o.L / 2, o.w / 2, aa)) return true; }
			return false;
		};
		return h;
	}

	public static Hz Ring(double x, double y, double t0, double sp, double th, double gap, double gs, double warn)
	{
		var h = new Hz();
		h.update = (t, dt) => { if ((t - t0) * sp > 1600) h.dead = true; };
		h.draw = t =>
		{
			g.strokeStyle = PINK;
			if (t < t0) { double k = clamp(1 - (t0 - t) / warn, 0, 1); g.globalAlpha = 0.6; g.lineWidth = 4; g.beginPath(); g.arc(x, y, 20 + 50 * k, gap + gs / 2, gap - gs / 2 + TAU); g.stroke(); g.globalAlpha = 1; return; }
			double r = (t - t0) * sp; if (r - th - 30 > hypot(Math.Max(x, W - x), Math.Max(y, H - y))) return;
			g.lineWidth = th * (1 + 0.15 * pulse); g.shadowColor = PINK; g.shadowBlur = 14; g.beginPath(); g.arc(x, y, r, gap + gs / 2, gap - gs / 2 + TAU); g.stroke(); g.shadowBlur = 0;
		};
		h.hit = (p, t) =>
		{
			if (t < t0) return false; double r = (t - t0) * sp, dx = p.x - x, dy = p.y - y, d = hypot(dx, dy); if (Math.Abs(d - r) > th / 2 + p.r - 2) return false;
			double da = JS.atan2(dy, dx) - gap; da = JS.atan2(JS.sin(da), JS.cos(da)); return Math.Abs(da) > gs / 2 - p.r / Math.Max(d, 1);
		};
		return h;
	}

	public static void poly(int n, double r)
	{
		g.beginPath();
		for (int i = 0; i < n; i++) { double a = (double)i * TAU / n - Math.PI / 2; if (i != 0) g.lineTo(JS.cos(a) * r, JS.sin(a) * r); else g.moveTo(JS.cos(a) * r, JS.sin(a) * r); }
		g.closePath(); g.fill();
	}
}
