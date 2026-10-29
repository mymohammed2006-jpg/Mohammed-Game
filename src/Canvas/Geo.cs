using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using LibTessDotNet;

namespace MG.Canvas;

// Triangles with a coverage value per vertex (1 = inside, 0 = outside). The 1px soft edge
// around every shape plays the part of the browser's anti-aliasing.
public sealed class CovMesh
{
	public Vector2[] pos = new Vector2[4096];
	public float[] cov = new float[4096];
	public int[] idx = new int[8192];
	public int nv, ni;
	public Rect2 bounds;
	public void Clear() { nv = 0; ni = 0; }
	public int V(Vector2 p, float c)
	{
		if (nv == pos.Length) { Array.Resize(ref pos, nv * 2); Array.Resize(ref cov, nv * 2); }
		pos[nv] = p; cov[nv] = c; return nv++;
	}
	public void T(int a, int b, int c)
	{
		if (ni + 3 > idx.Length) Array.Resize(ref idx, idx.Length * 2);
		idx[ni++] = a; idx[ni++] = b; idx[ni++] = c;
	}
	public void Q(int a, int b, int c, int d) { T(a, b, c); T(a, c, d); }
	public Rect2 Bounds()
	{
		if (nv == 0) return new Rect2();
		float x0 = pos[0].X, y0 = pos[0].Y, x1 = x0, y1 = y0;
		for (int i = 1; i < nv; i++)
		{
			var p = pos[i];
			if (p.X < x0) x0 = p.X; else if (p.X > x1) x1 = p.X;
			if (p.Y < y0) y0 = p.Y; else if (p.Y > y1) y1 = p.Y;
		}
		return new Rect2(x0, y0, x1 - x0, y1 - y0);
	}
}

public static class Geo
{
	const float H = 0.5f;   // half width of the soft edge, in device pixels

	static readonly List<Vector2> tmp = new();

	// remove repeated points (and the closing duplicate) so every edge has a direction
	public static List<Vector2> Clean(List<Vector2> src, bool closed, List<Vector2> dst)
	{
		dst.Clear();
		foreach (var p in src)
		{
			if (dst.Count > 0 && (p - dst[^1]).LengthSquared() < 1e-8f) continue;
			dst.Add(p);
		}
		if (closed) while (dst.Count > 1 && (dst[0] - dst[^1]).LengthSquared() < 1e-8f) dst.RemoveAt(dst.Count - 1);
		return dst;
	}

	// for fills: a vertex whose two neighbours (almost) meet is a zero-area spike, e.g. rect() then arc() joins the arc
	// to the rect's corner and back; the browser gives it no coverage, our soft edges would draw a hairline
	public static List<Vector2> DropSpikes(List<Vector2> p)
	{
		for (int guard = 0; guard < 4 && p.Count > 3; guard++)
		{
			bool any = false;
			for (int i = 0; i < p.Count && p.Count > 3; i++)
			{
				int n = p.Count;
				var a = p[(i - 1 + n) % n]; var c = p[(i + 1) % n];
				if ((a - c).LengthSquared() < 1e-6f && (p[i] - a).LengthSquared() > 1e-6f)
				{
					p.RemoveAt(i); any = true;
					// the two neighbours are now next to each other: keep one
					int j = i % p.Count, k = (j - 1 + p.Count) % p.Count;
					if ((p[j] - p[k]).LengthSquared() < 1e-6f) p.RemoveAt(j);
					i = Math.Max(-1, i - 2);
				}
			}
			if (!any) break;
		}
		return p;
	}

	public static double Area(List<Vector2> p)
	{
		double a = 0;
		for (int i = 0, n = p.Count; i < n; i++) { var q = p[i]; var r = p[(i + 1) % n]; a += (double)q.X * r.Y - (double)r.X * q.Y; }
		return a / 2;
	}

	// convex and not self-crossing (all turns the same way, total turn one round)
	public static bool IsConvex(List<Vector2> p)
	{
		int n = p.Count;
		if (n < 3) return false;
		int sign = 0;
		double turn = 0;
		for (int i = 0; i < n; i++)
		{
			Vector2 a = p[i], b = p[(i + 1) % n], c = p[(i + 2) % n];
			Vector2 d1 = b - a, d2 = c - b;
			float cr = d1.Cross(d2);
			if (Math.Abs(cr) > 1e-6f * (d1.Length() * d2.Length() + 1e-12f))
			{
				int s = cr > 0 ? 1 : -1;
				if (sign == 0) sign = s; else if (s != sign) return false;
			}
			turn += Math.Atan2(cr, d1.Dot(d2));
		}
		return Math.Abs(Math.Abs(turn) - Math.PI * 2) < 0.01;
	}

	// ---------------- fills ----------------

	// one convex polygon with an exact soft edge (the inside shrinks by half a pixel, the edge grows by half)
	public static void FillConvex(List<Vector2> p, CovMesh m)
	{
		int n = p.Count;
		bool ccw = Area(p) > 0;   // in y-down screen space "ccw" by this formula means clockwise on screen
		Span<Vector2> inner = n <= 256 ? stackalloc Vector2[n] : new Vector2[n];
		Span<Vector2> outer = n <= 256 ? stackalloc Vector2[n] : new Vector2[n];
		for (int i = 0; i < n; i++)
		{
			Vector2 a = p[(i - 1 + n) % n], b = p[i], c = p[(i + 1) % n];
			Vector2 n1 = Out(b - a, ccw), n2 = Out(c - b, ccw);
			Vector2 mv = Miter(n1, n2, 4f);
			inner[i] = b - mv * H;
			outer[i] = b + mv * H;
		}
		// a polygon thinner than a pixel: the inside collapses; draw it as one faint polygon instead
		var innerList = tmp; innerList.Clear();
		for (int i = 0; i < n; i++) innerList.Add(inner[i]);
		double ai = Area(innerList), ao = Area(p);
		if (ai * ao <= 0 || Math.Abs(ai) < 0.25)
		{
			// coverage ~ real area spread over the grown shape
			var ol = new List<Vector2>(n);
			for (int i = 0; i < n; i++) ol.Add(outer[i]);
			double aOut = Math.Abs(Area(ol));
			float c = aOut > 1e-9 ? (float)Math.Clamp(Math.Abs(ao) / aOut, 0, 1) : 0;
			int b0 = m.nv;
			for (int i = 0; i < n; i++) m.V(outer[i], c);
			for (int i = 1; i < n - 1; i++) m.T(b0, b0 + i, b0 + i + 1);
			return;
		}
		int ib = m.nv;
		for (int i = 0; i < n; i++) m.V(inner[i], 1);
		int ob = m.nv;
		for (int i = 0; i < n; i++) m.V(outer[i], 0);
		for (int i = 1; i < n - 1; i++) m.T(ib, ib + i, ib + i + 1);
		for (int i = 0; i < n; i++)
		{
			int j = (i + 1) % n;
			m.Q(ib + i, ob + i, ob + j, ib + j);
		}
	}

	// outward normal of an edge for a polygon with the given winding
	static Vector2 Out(Vector2 d, bool ccw)
	{
		float l = d.Length();
		if (l < 1e-12f) return Vector2.Zero;
		d /= l;
		return ccw ? new Vector2(d.Y, -d.X) : new Vector2(-d.Y, d.X);
	}

	// vector from a corner to its offset corner for offset 1 (limited so sharp tips don't shoot out)
	static Vector2 Miter(Vector2 n1, Vector2 n2, float limit)
	{
		Vector2 s = n1 + n2;
		float dot = 1 + n1.Dot(n2);
		if (dot < 1e-4f) return n1;
		Vector2 mv = s / dot;
		float l = mv.Length();
		if (l > limit) mv *= limit / l;
		return mv;
	}

	// a simple (not self-crossing) polygon, concave allowed
	static Vector2[] simpleIn = new Vector2[64], simpleOut = new Vector2[64];
	static int[] triOut = new int[192], triV = new int[64];
	// test only (TRICHECK=1): every polygon also goes through Godot's own triangulator and any difference is counted
	public static readonly bool TriCheck = System.Environment.GetEnvironmentVariable("TRICHECK") == "1";
	public static long triChecked, triDiff;
	public static bool FillSimple(List<Vector2> p, CovMesh m)
	{
		int n = p.Count;
		int nt = Triangulate(p, ref triOut);
		if (TriCheck)
		{
			var arr = p.ToArray(); int[] g = Geometry2D.TriangulatePolygon(arr); triChecked++;
			bool same = (g == null || g.Length < 3) ? nt < 3 : g.Length == nt && g.AsSpan().SequenceEqual(triOut.AsSpan(0, nt));
			if (!same) { triDiff++; GD.PrintErr($"TRICHECK differs n={n} godot={g?.Length} ours={nt}"); }
		}
		if (nt < 3) return false;
		var tri = triOut;
		bool ccw = Area(p) > 0;
		int ib = m.nv;
		if (simpleIn.Length < n) { simpleIn = new Vector2[Math.Max(n, simpleIn.Length * 2)]; simpleOut = new Vector2[simpleIn.Length]; }
		var inner = simpleIn;
		var outer = simpleOut;
		for (int i = 0; i < n; i++)
		{
			Vector2 a = p[(i - 1 + n) % n], b = p[i], c = p[(i + 1) % n];
			Vector2 mv = Miter(Out(b - a, ccw), Out(c - b, ccw), 2f);
			inner[i] = b - mv * H; outer[i] = b + mv * H;
		}
		for (int i = 0; i < n; i++) m.V(inner[i], 1);
		int ob = m.nv;
		for (int i = 0; i < n; i++) m.V(outer[i], 0);
		for (int i = 0; i < nt; i += 3) m.T(ib + tri[i], ib + tri[i + 1], ib + tri[i + 2]);
		for (int i = 0; i < n; i++) { int j = (i + 1) % n; m.Q(ib + i, ob + i, ob + j, ib + j); }
		return true;
	}

	// v54: Godot's Geometry2D.TriangulatePolygon (core/math/triangulate.cpp, 4.4.1) line for line in C#, same float steps and
	// same triangles, without handing the points over to Godot and back for every shape. Returns how many indices (0 = failed).
	static int Triangulate(List<Vector2> c, ref int[] res)
	{
		int n = c.Count;
		if (n < 3) return 0;
		if (triV.Length < n) triV = new int[Math.Max(n, triV.Length * 2)];
		if (res.Length < 3 * n) res = new int[Math.Max(3 * n, res.Length * 2)];
		var V = triV;
		float A = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++) A += c[p].X * c[q].Y - c[p].Y * c[q].X;
		A *= 0.5f;
		if (0.0f < A) for (int v = 0; v < n; v++) V[v] = v;
		else for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
		bool relaxed = false;
		int nv = n, nr = 0;
		int count = 2 * nv;
		for (int v = nv - 1; nv > 2;)
		{
			if (0 >= (count--))
			{
				if (relaxed) return 0;
				count = 2 * nv;
				relaxed = true;
			}
			int u = v;
			if (nv <= u) u = 0;
			v = u + 1;
			if (nv <= v) v = 0;
			int w = v + 1;
			if (nv <= w) w = 0;
			if (Snip(c, u, v, w, nv, V, relaxed))
			{
				res[nr++] = V[u]; res[nr++] = V[v]; res[nr++] = V[w];
				for (int s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
				nv--;
				count = 2 * nv;
			}
		}
		return nr;
	}
	static bool Snip(List<Vector2> contour, int u, int v, int w, int n, int[] V, bool relaxed)
	{
		float Ax = contour[V[u]].X, Ay = contour[V[u]].Y;
		float Bx = contour[V[v]].X, By = contour[V[v]].Y;
		float Cx = contour[V[w]].X, Cy = contour[V[w]].Y;
		float threshold = relaxed ? (float)-0.00001 : (float)0.00001;   // CMP_EPSILON
		if (threshold > (((Bx - Ax) * (Cy - Ay)) - ((By - Ay) * (Cx - Ax)))) return false;
		for (int p = 0; p < n; p++)
		{
			if ((p == u) || (p == v) || (p == w)) continue;
			float Px = contour[V[p]].X, Py = contour[V[p]].Y;
			if (InsideTri(Ax, Ay, Bx, By, Cx, Cy, Px, Py, relaxed)) return false;
		}
		return true;
	}
	static bool InsideTri(float Ax, float Ay, float Bx, float By, float Cx, float Cy, float Px, float Py, bool includeEdges)
	{
		float ax = Cx - Bx, ay = Cy - By, bx = Ax - Cx, by = Ay - Cy, cx = Bx - Ax, cy = By - Ay;
		float apx = Px - Ax, apy = Py - Ay, bpx = Px - Bx, bpy = Py - By, cpx = Px - Cx, cpy = Py - Cy;
		float aCROSSbp = ax * bpy - ay * bpx;
		float cCROSSap = cx * apy - cy * apx;
		float bCROSScp = bx * cpy - by * cpx;
		if (includeEdges) return (aCROSSbp > 0.0f) && (bCROSScp > 0.0f) && (cCROSSap > 0.0f);
		return (aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f);
	}

	static readonly List<Vector2> cleanTmp = new(), circlePts = new();
	// v54: one clipper and one tessellator for every fill (they keep their own work lists between uses)
	static readonly Clipper2Lib.ClipperD clipper = new(4);
	static readonly Tess tess = new(new FastPool());
	// LibTess's own pool: a lock, a type lookup and a compiled-expression constructor for every mesh piece; this one keeps them per type
	sealed class FastPool : IPool
	{
		static class Store<T> where T : class, Pooled<T>, new() { public static readonly Stack<T> s = new(); }
		public override void Register<T>(ITypePool typePool) { }
		public override T Get<T>()
		{
			var st = Store<T>.s;
			T val = st.Count > 0 ? st.Pop() : new T();
			val.Init(this);
			return val;
		}
		public override void Return<T>(ref T obj)
		{
			if (obj == null) return;
			obj.Reset(this);
			Store<T>.s.Push(obj);
			obj = null;
		}
	}
	static Vector2[] complexIn = new Vector2[64];
	static readonly Dictionary<int, ContourVertex[]> tessIn = new();   // AddContour takes a whole array: one kept per size

	// the area a path covers, as simple outlines (for clips inside clips)
	public static Clipper2Lib.PathsD Region(List<List<Vector2>> polys, bool evenOdd)
	{
		var subj = new Clipper2Lib.PathsD();
		foreach (var p0 in polys)
		{
			if (p0.Count < 3) continue;
			var p = DropSpikes(Clean(p0, true, cleanTmp));
			if (p.Count < 3) continue;
			var path = new Clipper2Lib.PathD(p.Count);
			foreach (var v in p) path.Add(new Clipper2Lib.PointD(v.X, v.Y));
			subj.Add(path);
		}
		var cl = clipper; cl.Clear();
		cl.AddSubject(subj);
		var res = new Clipper2Lib.PathsD();
		cl.Execute(Clipper2Lib.ClipType.Union, evenOdd ? Clipper2Lib.FillRule.EvenOdd : Clipper2Lib.FillRule.NonZero, res);
		return res;
	}
	public static Rect2 PolyBounds(List<List<Vector2>> polys)
	{
		float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
		foreach (var p in polys) foreach (var v in p) { x0 = Math.Min(x0, v.X); y0 = Math.Min(y0, v.Y); x1 = Math.Max(x1, v.X); y1 = Math.Max(y1, v.Y); }
		return x0 > x1 ? new Rect2() : new Rect2(x0, y0, x1 - x0, y1 - y0);
	}
	public static Clipper2Lib.PathsD Intersect(Clipper2Lib.PathsD a, Clipper2Lib.PathsD b)
	{
		var cl = clipper; cl.Clear();
		cl.AddSubject(a); cl.AddClip(b);
		var res = new Clipper2Lib.PathsD();
		cl.Execute(Clipper2Lib.ClipType.Intersection, Clipper2Lib.FillRule.NonZero, res);
		return res;
	}
	// the region when it is one axis-aligned rectangle (the usual clip), else null
	public static Rect2? AxisRect(Clipper2Lib.PathsD r)
	{
		if (r.Count != 1 || r[0].Count != 4) return null;
		var p = r[0];
		double x0 = p.Min(q => q.x), x1 = p.Max(q => q.x), y0 = p.Min(q => q.y), y1 = p.Max(q => q.y);
		foreach (var q in p) if ((Math.Abs(q.x - x0) > 1e-3 && Math.Abs(q.x - x1) > 1e-3) || (Math.Abs(q.y - y0) > 1e-3 && Math.Abs(q.y - y1) > 1e-3)) return null;
		return new Rect2((float)x0, (float)y0, (float)(x1 - x0), (float)(y1 - y0));
	}

	// anything: several subpaths, holes, crossings. Clipper resolves the fill rule into simple outlines,
	// then every outline gets the same exact half-pixel soft edge as the simple shapes.
	// v55: the same shape again (things standing still, or drawn the same every frame) takes its finished triangles from here
	// instead of working them out again: the very same numbers in give the very same triangles out
	sealed class CxMemo { public ulong h; public bool eo; public int[] cnt = new int[8]; public int np, nin; public ulong[] inp = new ulong[256]; public Vector2[] pos = new Vector2[256]; public float[] cov = new float[256]; public int[] idx = new int[512]; public int nv, ni; public long used; }
	static readonly CxMemo[] cxMemo = new CxMemo[32];
	static long cxTick;
	static readonly ulong[] cxSeen = new ulong[64];
	static int cxSeenAt;
	public static void FillComplex(List<List<Vector2>> polys, bool evenOdd, CovMesh m)
	{
		const ulong P = 0x100000001B3UL;
		ulong h = evenOdd ? 0xCBF29CE484222325UL : 0x84222325CBF29CE4UL; int tot = 0;
		foreach (var p0 in polys)
		{
			var s = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(p0);
			h = (h ^ (ulong)s.Length) * P;
			foreach (var u in System.Runtime.InteropServices.MemoryMarshal.Cast<Vector2, ulong>(s)) h = (h ^ u) * P;
			tot += s.Length;
		}
		cxTick++;
		foreach (var e in cxMemo)
		{
			if (e == null) continue;
			if (e.h == h && e.eo == evenOdd && e.np == polys.Count && e.nin == tot && SameIn(e, polys))
			{
				e.used = cxTick; int b = m.nv;
				for (int i = 0; i < e.nv; i++) m.V(e.pos[i], e.cov[i]);
				for (int i = 0; i < e.ni; i += 3) m.T(b + e.idx[i], b + e.idx[i + 1], b + e.idx[i + 2]);
				return;
			}
		}
		int v0 = m.nv, i0 = m.ni;
		FillComplexWork(polys, evenOdd, m);
		// kept only when it comes a second time (a moving shape is new every frame: keeping it would only cost)
		if (tot > 100000) return;
		if (Array.IndexOf(cxSeen, h) < 0) { cxSeen[cxSeenAt++ & (cxSeen.Length - 1)] = h; return; }
		// keep it, in the empty place or the one unused longest
		int k = 0;
		for (int j = 0; j < cxMemo.Length; j++) { if (cxMemo[j] == null) { k = j; break; } if (cxMemo[j].used < cxMemo[k].used) k = j; }
		var w = cxMemo[k] ??= new CxMemo();
		w.h = h; w.eo = evenOdd; w.np = polys.Count; w.nin = tot; w.used = cxTick;
		if (w.cnt.Length < polys.Count) w.cnt = new int[Math.Max(polys.Count, w.cnt.Length * 2)];
		if (w.inp.Length < tot) w.inp = new ulong[Math.Max(tot, w.inp.Length * 2)];
		int o = 0;
		for (int j = 0; j < polys.Count; j++)
		{
			var s = System.Runtime.InteropServices.MemoryMarshal.Cast<Vector2, ulong>(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(polys[j]));
			w.cnt[j] = s.Length; s.CopyTo(w.inp.AsSpan(o)); o += s.Length;
		}
		w.nv = m.nv - v0; w.ni = m.ni - i0;
		if (w.pos.Length < w.nv) { w.pos = new Vector2[Math.Max(w.nv, w.pos.Length * 2)]; w.cov = new float[w.pos.Length]; }
		if (w.idx.Length < w.ni) w.idx = new int[Math.Max(w.ni, w.idx.Length * 2)];
		Array.Copy(m.pos, v0, w.pos, 0, w.nv); Array.Copy(m.cov, v0, w.cov, 0, w.nv);
		for (int i = 0; i < w.ni; i++) w.idx[i] = m.idx[i0 + i] - v0;
	}
	static bool SameIn(CxMemo e, List<List<Vector2>> polys)
	{
		int o = 0;
		for (int j = 0; j < polys.Count; j++)
		{
			var s = System.Runtime.InteropServices.MemoryMarshal.Cast<Vector2, ulong>(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(polys[j]));
			if (e.cnt[j] != s.Length || !s.SequenceEqual(e.inp.AsSpan(o, s.Length))) return false;
			o += s.Length;
		}
		return true;
	}
	static void FillComplexWork(List<List<Vector2>> polys, bool evenOdd, CovMesh m)
	{
		var subj = new Clipper2Lib.PathsD();
		foreach (var p0 in polys)
		{
			if (p0.Count < 3) continue;
			var p = DropSpikes(Clean(p0, true, cleanTmp));
			if (p.Count < 3) continue;
			var path = new Clipper2Lib.PathD(p.Count);
			foreach (var v in p) path.Add(new Clipper2Lib.PointD(v.X, v.Y));
			subj.Add(path);
		}
		var cl = clipper; cl.Clear();
		cl.AddSubject(subj);
		var res = new Clipper2Lib.PathsD();
		cl.Execute(Clipper2Lib.ClipType.Union, evenOdd ? Clipper2Lib.FillRule.EvenOdd : Clipper2Lib.FillRule.NonZero, res);
		if (res.Count == 0) return;
		// which way round the outer outlines go (holes go the other way, so "outside" is the same side for all)
		var cs = new List<List<Vector2>>(res.Count);
		double bestA = 0; bool ccw = true;
		foreach (var r in res)
		{
			var c = new List<Vector2>(r.Count);
			foreach (var q in r) c.Add(new Vector2((float)q.x, (float)q.y));
			cleanTmp.Clear(); cleanTmp.AddRange(c); Clean(cleanTmp, true, c);
			if (c.Count < 3) continue;
			cs.Add(c);
			double ar = Area(c);
			if (Math.Abs(ar) > bestA) { bestA = Math.Abs(ar); ccw = ar > 0; }
		}
		if (cs.Count == 0) return;   // nothing to tessellate (the kept tessellator would still hold the last result)
		foreach (var c in cs)
		{
			int n = c.Count;
			if (complexIn.Length < n) complexIn = new Vector2[Math.Max(n, complexIn.Length * 2)];
			var inner = complexIn;
			int ob = m.nv;
			for (int i = 0; i < n; i++)
			{
				Vector2 a = c[(i - 1 + n) % n], b = c[i], d = c[(i + 1) % n];
				Vector2 mv = Miter(Out(b - a, ccw), Out(d - b, ccw), 2f);
				inner[i] = b - mv * H;
				m.V(b + mv * H, 0);
			}
			int ib = m.nv;
			for (int i = 0; i < n; i++) m.V(inner[i], 1);
			for (int i = 0; i < n; i++) { int j = (i + 1) % n; m.Q(ib + i, ob + i, ob + j, ib + j); }
			if (!tessIn.TryGetValue(n, out var cv)) tessIn[n] = cv = new ContourVertex[n];
			for (int i = 0; i < n; i++) cv[i].Position = new Vec3(inner[i].X, inner[i].Y, 0);
			tess.AddContour(cv, ContourOrientation.Original);
		}
		tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, null, new Vec3(0, 0, 1));
		int b0 = m.nv;
		for (int i = 0; i < tess.VertexCount; i++) m.V(new Vector2(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y), 1);
		for (int i = 0; i < tess.ElementCount; i++)
		{
			int a = tess.Elements[i * 3], b = tess.Elements[i * 3 + 1], c = tess.Elements[i * 3 + 2];
			if (a < 0 || b < 0 || c < 0) continue;
			m.T(b0 + a, b0 + b, b0 + c);
		}
	}

	// ---------------- strokes ----------------

	// a cross-section of a stroke: 4 vertices from -b to +b (3 for a thin line)
	public struct Stn
	{
		public int n, v0, v1, v2, v3;
		public Stn(int a, int b, int c) { n = 3; v0 = a; v1 = b; v2 = c; v3 = 0; }
		public Stn(int a, int b, int c, int d) { n = 4; v0 = a; v1 = b; v2 = c; v3 = d; }
		public int this[int k]
		{
			readonly get => k == 0 ? v0 : k == 1 ? v1 : k == 2 ? v2 : v3;
			set { if (k == 0) v0 = value; else if (k == 1) v1 = value; else if (k == 2) v2 = value; else v3 = value; }
		}
	}

	// v54: the stroke's work lists are kept between calls (new ones every call were most of the game's garbage)
	static readonly List<Vector2> strokePts = new();
	static Vector2[] sDir = new Vector2[64], sNor = new Vector2[64];
	static float[] sLen = new float[64];
	static Stn[] sStart = new Stn[64], sEnd = new Stn[64];

	// a polyline as a ribbon with soft sides; joins and caps like the canvas.
	public static void StrokePolyline(List<Vector2> src, bool closed, float w, Join join, Cap cap, float miterLimit, CovMesh m)
	{
		var p = Clean(src, closed, strokePts);
		float hw = w / 2;
		if (p.Count == 1 || (p.Count == 2 && closed && (p[0] - p[1]).LengthSquared() < 1e-8f))
		{
			// zero-length subpath: only round and square caps draw something
			if (cap == Cap.Round) FillCircle(p[0], hw, m);
			else if (cap == Cap.Square)
			{
				var sq = new List<Vector2> { p[0] + new Vector2(-hw, -hw), p[0] + new Vector2(hw, -hw), p[0] + new Vector2(hw, hw), p[0] + new Vector2(-hw, hw) };
				FillConvex(sq, m);
			}
			return;
		}
		if (closed && p.Count < 3) closed = false;
		int n = p.Count;
		// cross-section: offsets and coverage
		float a, b, cc;
		bool thin = w < 1;
		if (thin) { a = 0; b = 1; cc = w; }
		else { a = hw - H; b = hw + H; cc = 1; }

		int segs = closed ? n : n - 1;
		if (sDir.Length < segs)
		{
			int sz = Math.Max(segs, sDir.Length * 2);
			sDir = new Vector2[sz]; sNor = new Vector2[sz]; sLen = new float[sz]; sStart = new Stn[sz]; sEnd = new Stn[sz];
		}
		// directions and left normals per segment
		var dir = sDir;
		var nor = sNor;
		var len = sLen;
		for (int i = 0; i < segs; i++)
		{
			Vector2 d = p[(i + 1) % n] - p[i];
			len[i] = d.Length();
			dir[i] = d / len[i];
			nor[i] = new Vector2(-dir[i].Y, dir[i].X);
		}

		// open ends: square caps make the line longer, butt/square get a soft end
		Vector2 startP = p[0], endP = p[n - 1];
		if (!closed && cap == Cap.Square)
		{
			startP -= dir[0] * hw;
			endP += dir[segs - 1] * hw;
		}

		var segStart = sStart;
		var segEnd = sEnd;
		if (!closed)
		{
			segStart[0] = St(startP, nor[0], thin, a, b, cc, m);
			if (cap == Cap.Round) RoundCap(startP, -dir[0], nor[0], a, b, cc, thin, m);
			else SoftEnd(segStart[0], startP, -dir[0], nor[0], m, thin, a, b, cc);
			segEnd[segs - 1] = St(endP, nor[segs - 1], thin, a, b, cc, m);
			if (cap == Cap.Round) RoundCap(endP, dir[segs - 1], nor[segs - 1], a, b, cc, thin, m);
			else SoftEnd(segEnd[segs - 1], endP, dir[segs - 1], nor[segs - 1], m, thin, a, b, cc);
			for (int j = 1; j < segs; j++)
				segStart[j] = JoinStation(p[j], dir[j - 1], dir[j], nor[j - 1], nor[j], len[j - 1], len[j], a, b, cc, thin, join, miterLimit, hw, m, out segEnd[j - 1]);
		}
		else
		{
			for (int j = 0; j < segs; j++)
			{
				int pj = (j - 1 + segs) % segs;
				segStart[j] = JoinStation(p[j], dir[pj], dir[j], nor[pj], nor[j], len[pj], len[j], a, b, cc, thin, join, miterLimit, hw, m, out segEnd[pj]);
			}
		}
		for (int i = 0; i < segs; i++)
		{
			var s0 = segStart[i]; var s1 = segEnd[i];
			for (int k = 0; k < s0.n - 1; k++) m.Q(s0[k], s0[k + 1], s1[k + 1], s1[k]);
		}
	}

	// station = cross-section at a point: 4 (or 3 for thin) vertices from -b to +b
	static Stn St(Vector2 at, Vector2 nn, bool thin, float a, float b, float cc, CovMesh m)
	{
		if (thin) return new Stn(m.V(at - nn * b, 0), m.V(at, cc), m.V(at + nn * b, 0));
		return new Stn(m.V(at - nn * b, 0), m.V(at - nn * a, cc), m.V(at + nn * a, cc), m.V(at + nn * b, 0));
	}

	// a butt end: the last half pixel fades out (corners included)
	static void SoftEnd(Stn st, Vector2 at, Vector2 outDir, Vector2 nn, CovMesh m, bool thin, float a, float b, float cc)
	{
		// move the station half a pixel back inside and add a row half a pixel outside with zero coverage
		int k = st.n;
		for (int j = 0; j < k; j++)
		{
			Vector2 pp = m.pos[st[j]] - outDir * H;
			m.pos[st[j]] = pp;
		}
		var outer = new Stn { n = k };
		for (int j = 0; j < k; j++) outer[j] = m.V(m.pos[st[j]] + outDir * (2 * H), 0);
		for (int j = 0; j < k - 1; j++) m.Q(st[j], st[j + 1], outer[j + 1], outer[j]);
	}

	static void RoundCap(Vector2 c, Vector2 outDir, Vector2 nn, float a, float b, float cc, bool thin, CovMesh m)
	{
		float ang0 = Mathf.Atan2(nn.Y, nn.X);
		// half circle from +normal through outDir to -normal
		float sgn = nn.Cross(outDir) > 0 ? 1 : -1;
		int seg = Math.Clamp((int)Math.Ceiling(Math.PI / (2 * Math.Acos(Math.Max(-1, 1 - 0.2 / Math.Max(b, 0.3))))), 4, 64);
		int center = m.V(c, cc);
		int pa = -1, pb = -1;
		for (int i = 0; i <= seg; i++)
		{
			float t = ang0 + sgn * Mathf.Pi * i / seg;
			var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
			int va = thin ? center : m.V(c + d * a, cc);
			int vb = m.V(c + d * b, 0);
			if (i > 0)
			{
				if (!thin) m.T(center, pa, va);
				m.Q(pa, pb, vb, va);
			}
			pa = va; pb = vb;
		}
	}

	// builds the join at corner p between segment 1 (incoming) and 2 (outgoing).
	// Returns the station where segment 2 starts; endOfPrev is where segment 1 ends.
	static Stn JoinStation(Vector2 p, Vector2 d1, Vector2 d2, Vector2 n1, Vector2 n2, float l1, float l2,
		float a, float b, float cc, bool thin, Join join, float miterLimit, float hw, CovMesh m, out Stn endOfPrev)
	{
		float cross = d1.Cross(d2);
		float dot = d1.Dot(d2);
		Vector2 mv = Vector2.Zero;
		float denom = 1 + n1.Dot(n2);
		bool straight = Math.Abs(cross) < 1e-5f && dot > 0;
		if (straight || denom > 1e-4f) mv = (n1 + n2) / Math.Max(denom, 1e-4f);
		// how far the miter point is, as a multiple of the half width
		float miterRatio = mv.Length();
		bool useMiter = straight || (join == Join.Miter && miterRatio <= miterLimit);
		// the inner side can use the crossing point only if both segments are long enough
		float innerReach = miterRatio * b;
		bool innerOk = !straight && innerReach < Math.Min(l1, l2) + b && denom > 1e-3f;

		if (useMiter && (straight || innerOk))
		{
			Stn s = thin
				? new Stn(m.V(p - mv * b, 0), m.V(p, cc), m.V(p + mv * b, 0))
				: new Stn(m.V(p - mv * b, 0), m.V(p - mv * a, cc), m.V(p + mv * a, cc), m.V(p + mv * b, 0));
			endOfPrev = s;
			return s;
		}
		// the outer side is the side the path turns away from: left normal side when turning right
		float outerSign = cross > 0 ? -1 : 1;   // cross > 0: turning toward +normal side, so outer is -normal
		// stations at the end of segment 1 and start of segment 2
		Stn e1, s2;
		if (thin)
		{
			e1 = new Stn(m.V(p - n1 * b, 0), m.V(p, cc), m.V(p + n1 * b, 0));
			int s2a = m.V(p - n2 * b, 0);
			s2 = new Stn(s2a, e1[1], m.V(p + n2 * b, 0));
		}
		else
		{
			e1 = new Stn(m.V(p - n1 * b, 0), m.V(p - n1 * a, cc), m.V(p + n1 * a, cc), m.V(p + n1 * b, 0));
			s2 = new Stn(m.V(p - n2 * b, 0), m.V(p - n2 * a, cc), m.V(p + n2 * a, cc), m.V(p + n2 * b, 0));
			if (innerOk)
			{
				// share the inner corner (no overlap on the inside of the turn)
				if (outerSign > 0) { s2[0] = e1[0] = m.V(p - mv * b, 0); s2[1] = e1[1] = m.V(p - mv * a, cc); }
				else { s2[3] = e1[3] = m.V(p + mv * b, 0); s2[2] = e1[2] = m.V(p + mv * a, cc); }
			}
		}
		// fill the gap on the outer side
		Vector2 o1 = n1 * outerSign, o2 = n2 * outerSign;
		int c0 = m.V(p, cc);
		if (!thin && innerOk)
		{
			// the segment ends run from the shared inner corner to the outer side; close the wedge up to the centre
			int inn = outerSign > 0 ? 1 : 2, outr = outerSign > 0 ? 2 : 1;
			m.T(c0, e1[inn], e1[outr]);
			m.T(c0, s2[outr], s2[inn]);
		}
		if (join == Join.Round)
		{
			float t0 = Mathf.Atan2(o1.Y, o1.X), t1 = Mathf.Atan2(o2.Y, o2.X);
			float dt = Mathf.Wrap(t1 - t0, -Mathf.Pi, Mathf.Pi);
			int seg = Math.Clamp((int)Math.Ceiling(Math.Abs(dt) / (2 * Math.Acos(Math.Max(-1, 1 - 0.2 / Math.Max(b, 0.3))))), 1, 64);
			int pa = -1, pb = -1;
			for (int i = 0; i <= seg; i++)
			{
				float t = t0 + dt * i / seg;
				var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
				int va = thin ? c0 : m.V(p + d * a, cc), vb = m.V(p + d * b, 0);
				if (i > 0) { if (!thin) m.T(c0, pa, va); m.Q(pa, pb, vb, va); }
				pa = va; pb = vb;
			}
		}
		else
		{
			// bevel (also a miter beyond the limit)
			int a1 = thin ? c0 : m.V(p + o1 * a, cc), a2 = thin ? c0 : m.V(p + o2 * a, cc);
			int b1 = m.V(p + o1 * b, 0), b2 = m.V(p + o2 * b, 0);
			if (!thin) m.T(c0, a1, a2);
			m.Q(a1, b1, b2, a2);
		}
		endOfPrev = e1;
		return s2;
	}

	public static void FillCircle(Vector2 c, float r, CovMesh m)
	{
		int seg = Math.Clamp((int)Math.Ceiling(2 * Math.PI / (2 * Math.Acos(Math.Max(-1, 1 - 0.2 / Math.Max(r, 0.3))))), 8, 256);
		var pts = circlePts; pts.Clear();
		for (int i = 0; i < seg; i++) { float t = Mathf.Tau * i / seg; pts.Add(c + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r); }
		FillConvex(pts, m);
	}

	// ---------------- dashes ----------------
	// v54: the dash lists are kept and reused (the result is only good until the next call)
	static readonly List<(List<Vector2>, bool)> dashRes = new();
	static readonly List<List<Vector2>> dashPool = new();
	static int dashUsed;
	static readonly List<Vector2> dashPts = new();
	static List<Vector2> DashList(Vector2 first)
	{
		if (dashUsed == dashPool.Count) dashPool.Add(new List<Vector2>());
		var l = dashPool[dashUsed++]; l.Clear(); l.Add(first);
		return l;
	}
	public static List<(List<Vector2> pts, bool closed)> Dash(List<Vector2> p, bool closed, double[] dash, double offset)
	{
		var res = dashRes; res.Clear(); dashUsed = 0;
		double total = 0; foreach (var d in dash) total += d;
		if (total <= 0) { res.Add((p, closed)); return res; }
		var pts = dashPts; pts.Clear(); pts.AddRange(p);
		if (closed && pts.Count > 1) pts.Add(pts[0]);
		// position in the pattern
		double pos = offset % total; if (pos < 0) pos += total;
		int di = 0;
		while (pos >= dash[di]) { pos -= dash[di]; di = (di + 1) % dash.Length; }
		double remain = dash[di] - pos;
		bool on = di % 2 == 0;
		List<Vector2> curr = on ? DashList(pts[0]) : null;
		for (int i = 0; i < pts.Count - 1; i++)
		{
			Vector2 a = pts[i], b = pts[i + 1];
			double segLen = (b - a).Length(), t = 0;
			while (segLen - t > remain)
			{
				t += remain;
				var q = a + (b - a) * (float)(t / segLen);
				if (on) { curr.Add(q); res.Add((curr, false)); curr = null; }
				else curr = DashList(q);
				on = !on;
				di = (di + 1) % dash.Length;
				remain = dash[di];
			}
			remain -= segLen - t;
			if (on) curr.Add(b);
		}
		if (on && curr != null && curr.Count > 1) res.Add((curr, false));
		return res;
	}
}
