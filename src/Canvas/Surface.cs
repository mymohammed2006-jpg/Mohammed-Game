using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Canvas;

// Turns the Ctx draw calls into Godot canvas items for one frame.
//  - plain shapes go into "batch" items as triangles (one draw call for many shapes)
//  - shadows: the shape is drawn white into a mask atlas, blurred (two passes), then drawn as a tinted quad
//  - gradients get their own item with a shader
//  - clip() makes a clip-only canvas group; everything drawn while it is active becomes its child.
//    Godot can't nest clip groups, so every group sits directly in the picture: a clip inside a clip gets the
//    intersection of both shapes as its mask, and a clip drawn into again after another group was added
//    gets a fresh group with the same mask (so the drawing order stays the canvas order).
// All batch triangles sample the blurred atlas; plain shapes use a white block kept in its corner.
public sealed class Surface
{
	public const int A = 2048;          // shadow atlas size (pixels)
	const int WhiteBlock = 8;           // white square at the atlas corner for plain shapes
	static readonly Vector2 WhiteUV = new Vector2(WhiteBlock * 0.5f / A, WhiteBlock * 0.5f / A);

	readonly Rid gameRoot, maskRoot, maskTextRoot, blurHRoot, blurVRoot, whiteRoot;
	readonly Rid atlasTex;
	readonly Shader gradShader;
	public int W, H;                    // backing size of the game picture

	public Surface(Rid gameRoot, Rid maskRoot, Rid maskTextRoot, Rid blurHRoot, Rid blurVRoot, Rid whiteRoot, Rid atlasTex, Shader gradShader)
	{
		this.gameRoot = gameRoot; this.maskRoot = maskRoot; this.maskTextRoot = maskTextRoot;
		this.blurHRoot = blurHRoot; this.blurVRoot = blurVRoot; this.whiteRoot = whiteRoot;
		this.atlasTex = atlasTex; this.gradShader = gradShader;
		RenderingServer.CanvasItemSetDefaultTextureFilter(gameRoot, RenderingServer.CanvasItemTextureFilter.Linear);
		RenderingServer.CanvasItemClear(whiteRoot);
		RenderingServer.CanvasItemAddRect(whiteRoot, new Rect2(0, 0, WhiteBlock, WhiteBlock), new Color(1, 1, 1, 1));
	}

	// ---------- pooled canvas items ----------
	readonly List<Rid> pool = new();
	int used, usedLast;
	readonly List<ShaderMaterial> gradPool = new();
	int gradUsed;

	// a container: the picture itself (0) or one clip. A clip keeps its mask and its region (for clips inside it).
	struct Cont { public Rid item; public int next; public bool live; public Vector2[] mp; public Color[] mc; public int[] mi; public Clipper2Lib.PathsD region; public Rect2? rect; }
	readonly List<Cont> conts = new();
	int rootLast;          // which container the last item added to the picture belongs to

	Rid Raw()
	{
		if (used == pool.Count) pool.Add(RenderingServer.CanvasItemCreate());
		return pool[used++];
	}

	// the clip's group must be the last thing in the picture, or drawing into it would land under later items
	void Place(int cont)
	{
		if (cont == 0) { rootLast = 0; return; }
		var c = conts[cont];
		if (c.live && rootLast == cont) return;
		var it = Raw();
		var r = conts[0];
		RenderingServer.CanvasItemClear(it);
		RenderingServer.CanvasItemSetParent(it, r.item);
		RenderingServer.CanvasItemSetDrawIndex(it, r.next++);
		conts[0] = r;
		RenderingServer.CanvasItemSetVisible(it, true);
		RenderingServer.CanvasItemSetMaterial(it, default);
		RenderingServer.CanvasItemSetDefaultTextureFilter(it, RenderingServer.CanvasItemTextureFilter.Linear);
		RenderingServer.CanvasItemSetCanvasGroupMode(it, RenderingServer.CanvasGroupMode.ClipOnly, 0, true, 0, false);
		if (c.mi.Length > 0) RenderingServer.CanvasItemAddTriangleArray(it, c.mi, c.mp, c.mc, Enumerable.Repeat(WhiteUV, c.mp.Length).ToArray(), null, null, atlasTex);
		c.item = it; c.next = 0; c.live = true;
		conts[cont] = c;
		rootLast = cont;
	}

	Rid NewItem(int cont, Rid material, bool linear = true)
	{
		Place(cont);
		var it = Raw();
		var c = conts[cont];
		RenderingServer.CanvasItemClear(it);
		RenderingServer.CanvasItemSetParent(it, c.item);
		RenderingServer.CanvasItemSetDrawIndex(it, c.next++);
		conts[cont] = c;
		RenderingServer.CanvasItemSetVisible(it, true);
		RenderingServer.CanvasItemSetMaterial(it, material);
		RenderingServer.CanvasItemSetCanvasGroupMode(it, RenderingServer.CanvasGroupMode.Disabled);
		RenderingServer.CanvasItemSetDefaultTextureFilter(it, linear ? RenderingServer.CanvasItemTextureFilter.Linear : RenderingServer.CanvasItemTextureFilter.Nearest);
		return it;
	}

	// ---------- growable triangle buffer ----------
	sealed class TriBuf
	{
		public Vector2[] p = new Vector2[1024];
		public Color[] c = new Color[1024];
		public Vector2[] u = new Vector2[1024];
		public int[] i = new int[2048];
		public int nv, ni;
		public void Clear() { nv = 0; ni = 0; }
		public int V(Vector2 pos, Color col, Vector2 uv)
		{
			if (nv == p.Length) { Array.Resize(ref p, nv * 2); Array.Resize(ref c, nv * 2); Array.Resize(ref u, nv * 2); }
			p[nv] = pos; c[nv] = col; u[nv] = uv; return nv++;
		}
		public void Reserve(int moreIdx) { if (ni + moreIdx > i.Length) Array.Resize(ref i, Math.Max(i.Length * 2, ni + moreIdx)); }
		public void T(int a, int b, int d) { Reserve(3); i[ni++] = a; i[ni++] = b; i[ni++] = d; }
		public void Q(int a, int b, int d, int e) { T(a, b, d); T(a, d, e); }
		public void Submit(Rid item, Rid tex, bool uvs = true)
		{
			if (ni == 0) { Clear(); return; }
			RenderingServer.CanvasItemAddTriangleArray(item, i[..ni], p[..nv], c[..nv], uvs ? u[..nv] : null, null, null, tex);
			Clear();
		}
	}

	readonly TriBuf batch = new(), mask = new(), blurH = new(), blurV = new();
	int curCont = -1;
	Rid curItem;
	bool curIsBatch;

	// ---------- frame ----------
	public void BeginFrame(int w, int h)
	{
		W = w; H = h;
		usedLast = Math.Max(usedLast, used);
		used = 0; gradUsed = 0;
		conts.Clear();
		conts.Add(new Cont { item = gameRoot, next = 0, live = true });
		rootLast = 0;
		curCont = -1; curIsBatch = false;
		batch.Clear(); mask.Clear(); blurH.Clear(); blurV.Clear();
		RenderingServer.CanvasItemClear(maskRoot);
		RenderingServer.CanvasItemClear(maskTextRoot);
		RenderingServer.CanvasItemClear(blurHRoot);
		RenderingServer.CanvasItemClear(blurVRoot);
		shelfX = WhiteBlock + 2; shelfY = 0; shelfH = WhiteBlock + 2;
		shadowsSkipped = 0;
	}

	public void EndFrame()
	{
		FlushBatch();
		mask.Submit(maskRoot, default, false);
		blurH.Submit(blurHRoot, default, false);
		blurV.Submit(blurVRoot, default, false);
		for (int i = used; i < usedLast; i++)
		{
			RenderingServer.CanvasItemClear(pool[i]);
			RenderingServer.CanvasItemSetVisible(pool[i], false);
		}
		usedLast = used;
	}

	void FlushBatch()
	{
		if (curIsBatch) batch.Submit(curItem, atlasTex);
		else batch.Clear();
	}

	void EnsureBatch(int cont)
	{
		if (curIsBatch && curCont == cont) return;
		FlushBatch();
		curItem = NewItem(cont, default);
		curCont = cont; curIsBatch = true;
	}

	// ---------- plain shapes ----------
	public void Tris(int cont, CovMesh m, Color col)
	{
		if (m.ni == 0 || col.A <= 0) return;
		EnsureBatch(cont);
		int b0 = batch.nv;
		for (int k = 0; k < m.nv; k++) batch.V(m.pos[k], new Color(col.R, col.G, col.B, col.A * m.cov[k]), WhiteUV);
		batch.Reserve(m.ni);
		for (int k = 0; k < m.ni; k++) batch.i[batch.ni++] = b0 + m.idx[k];
	}

	// ---------- gradients ----------
	public void GradTris(int cont, CovMesh m, Gradient gr, Mat userToDevice, float alpha)
	{
		if (m.ni == 0 || alpha <= 0 || gr.stops.Count == 0) return;
		FlushBatch();
		curIsBatch = false;
		var mat = GradMat(gr, userToDevice.Inv());
		var it = NewItem(cont, mat.GetRid());
		var tb = gradBuf; tb.Clear();
		for (int k = 0; k < m.nv; k++) tb.V(m.pos[k], new Color(1, 1, 1, alpha * m.cov[k]), Vector2.Zero);
		tb.Reserve(m.ni);
		for (int k = 0; k < m.ni; k++) tb.i[tb.ni++] = m.idx[k];
		tb.Submit(it, default, false);
	}
	// text filled with a gradient: the glyphs' own coverage times the gradient colour
	public void GradText(int cont, Font f, string text, int fontSize, Transform2D xf, Gradient gr, Mat glyphToUser, float alpha, TextServer.Direction dir, int outline = 0)
	{
		if (alpha <= 0 || gr.stops.Count == 0) return;
		FlushBatch();
		curIsBatch = false;
		// the shader sees the glyphs' own coordinates (the item's transform comes after), so it maps those to user space
		var mat = GradMat(gr, glyphToUser);
		var it = NewItem(cont, mat.GetRid());
		RenderingServer.CanvasItemAddSetTransform(it, xf);
		var col = new Color(1, 1, 1, alpha);
		if (outline > 0) f.DrawStringOutline(it, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, outline, col, TextServer.JustificationFlag.None, dir);
		else f.DrawString(it, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, col, TextServer.JustificationFlag.None, dir);
	}
	ShaderMaterial GradMat(Gradient gr, Mat inv)
	{
		if (gradUsed == gradPool.Count) gradPool.Add(new ShaderMaterial { Shader = gradShader });
		var mat = gradPool[gradUsed++];
		mat.SetShaderParameter("inv_abcd", new Vector4((float)inv.a, (float)inv.b, (float)inv.c, (float)inv.d));
		mat.SetShaderParameter("inv_ef", new Vector2((float)inv.e, (float)inv.f));
		mat.SetShaderParameter("kind", gr.radial ? 1 : 0);
		mat.SetShaderParameter("p0", new Vector3((float)gr.x0, (float)gr.y0, (float)gr.r0));
		mat.SetShaderParameter("p1", new Vector3((float)gr.x1, (float)gr.y1, (float)gr.r1));
		int n = Math.Min(gr.stops.Count, 8);
		var offs = new float[8];
		var cols = new Vector4[8];
		for (int k = 0; k < 8; k++)
		{
			var s = gr.stops[Math.Min(k, n - 1)];
			offs[k] = (float)s.off;
			// premultiplied, the canvas interpolates gradients that way
			cols[k] = new Vector4(s.col.R * s.col.A, s.col.G * s.col.A, s.col.B * s.col.A, s.col.A);
		}
		mat.SetShaderParameter("n", n);
		mat.SetShaderParameter("offs", offs);
		mat.SetShaderParameter("cols", cols);
		return mat;
	}
	readonly TriBuf gradBuf = new();

	// ---------- clip ----------
	// a new clip with this mask (device pixels) and region; its group is made when something is drawn into it
	public int PushClip(CovMesh m, Clipper2Lib.PathsD region, Rect2? rect)
	{
		FlushBatch();
		curIsBatch = false;
		var mc = new Color[m.nv];
		for (int k = 0; k < m.nv; k++) mc[k] = new Color(1, 1, 1, m.cov[k]);
		conts.Add(new Cont { mp = m.pos[..m.nv], mc = mc, mi = m.idx[..m.ni], region = region, rect = rect });
		return conts.Count - 1;
	}
	public Clipper2Lib.PathsD ClipRegion(int cont) => cont == 0 ? null : conts[cont].region;
	public Rect2? ClipRect(int cont) => cont == 0 ? null : conts[cont].rect;

	// ---------- text ----------
	public void Text(int cont, Font f, string text, int fontSize, Transform2D xf, Color col, TextServer.Direction dir, int outline = 0)
	{
		if (col.A <= 0) return;
		EnsureBatch(cont);
		FlushBatch();
		RenderingServer.CanvasItemAddSetTransform(curItem, xf);
		if (outline > 0) f.DrawStringOutline(curItem, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, outline, col, TextServer.JustificationFlag.None, dir);
		else f.DrawString(curItem, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, col, TextServer.JustificationFlag.None, dir);
		RenderingServer.CanvasItemAddSetTransform(curItem, Transform2D.Identity);
	}

	// ---------- shadows ----------
	int shelfX, shelfY, shelfH;
	public int shadowsSkipped;

	bool Alloc(int w, int h, out int x, out int y)
	{
		x = y = 0;
		if (w > A || h > A) return false;
		if (shelfX + w > A) { shelfY += shelfH; shelfX = 0; shelfH = 0; }
		if (shelfY + h > A) return false;
		x = shelfX; y = shelfY;
		shelfX += w;
		shelfH = Math.Max(shelfH, h);
		return true;
	}

	// where one shadow lives in the atlas: atlas = (device - o) / d + a
	public struct ShadowMap
	{
		public Vector2 o, a;
		public float d;
		public Vector2 Map(Vector2 p) => (p - o) / d + a;
		public Transform2D Xf => new Transform2D(new Vector2(1 / d, 0), new Vector2(0, 1 / d), a - o / d);
	}

	// Reserves atlas room for a shadow of something covering 'bounds' (device pixels), queues the blur
	// and puts the tinted shadow quad into the batch. The caller then draws the shape into the mask with Mask*().
	// clipped=true means the shape reaches outside the useful area and its mask must be clipped to 'region'.
	public bool Shadow(int cont, Rect2 bounds, double blur, double offX, double offY, Color col, out ShadowMap map, out Rect2 region)
	{
		map = default; region = default;
		if (col.A <= 0) return false;
		double sigma = blur / 2;
		float d = (float)Math.Max(1, sigma / 3);
		for (int attempt = 0; attempt < 6; attempt++, d *= 2)
		{
			float st = (float)(sigma / d);
			int mpx = sigma < 0.05 ? 1 : (int)Math.Ceiling(3 * st) + 1;
			float ext = mpx * d + 2;
			// only the part whose shadow can land on screen matters
			var lim = new Rect2((float)(-offX - ext), (float)(-offY - ext), W + 2 * ext, H + 2 * ext);
			var b = bounds.Intersection(lim);
			if (b.Size.X <= 0 || b.Size.Y <= 0) return false;
			var o = new Vector2(Mathf.Floor(b.Position.X) - 1, Mathf.Floor(b.Position.Y) - 1);
			int cw = (int)Math.Ceiling((b.End.X - o.X) / d) + 2, ch = (int)Math.Ceiling((b.End.Y - o.Y) / d) + 2;
			if (!Alloc(cw + 4 * mpx, ch + 4 * mpx, out int ax, out int ay)) continue;
			var a = new Vector2(ax + 2 * mpx, ay + 2 * mpx);
			map = new ShadowMap { o = o, a = a, d = d };
			region = new Rect2(o, new Vector2(cw * d, ch * d));
			// blur passes over content +- mpx
			float x0 = a.X - mpx, y0 = a.Y - mpx, x1 = a.X + cw + mpx, y1 = a.Y + ch + mpx;
			var sc = new Color(Math.Min(st / 4f, 1f), 0, 0, 1);
			Quad(blurH, x0, y0, x1, y1, sc);
			Quad(blurV, x0, y0, x1, y1, sc);
			// the tinted quad in the game picture
			EnsureBatch(cont);
			var dev0 = o + (new Vector2(x0, y0) - a) * d + new Vector2((float)offX, (float)offY);
			var dev1 = o + (new Vector2(x1, y1) - a) * d + new Vector2((float)offX, (float)offY);
			int i0 = batch.V(dev0, col, new Vector2(x0, y0) / A);
			int i1 = batch.V(new Vector2(dev1.X, dev0.Y), col, new Vector2(x1, y0) / A);
			int i2 = batch.V(dev1, col, new Vector2(x1, y1) / A);
			int i3 = batch.V(new Vector2(dev0.X, dev1.Y), col, new Vector2(x0, y1) / A);
			batch.Q(i0, i1, i2, i3);
			return true;
		}
		shadowsSkipped++;
		return false;
	}

	static void Quad(TriBuf t, float x0, float y0, float x1, float y1, Color c)
	{
		int i0 = t.V(new Vector2(x0, y0), c, Vector2.Zero), i1 = t.V(new Vector2(x1, y0), c, Vector2.Zero);
		int i2 = t.V(new Vector2(x1, y1), c, Vector2.Zero), i3 = t.V(new Vector2(x0, y1), c, Vector2.Zero);
		t.Q(i0, i1, i2, i3);
	}

	public void MaskTris(CovMesh m, float alpha, ShadowMap map)
	{
		int b0 = mask.nv;
		for (int k = 0; k < m.nv; k++) mask.V(map.Map(m.pos[k]), new Color(1, 1, 1, alpha * m.cov[k]), Vector2.Zero);
		mask.Reserve(m.ni);
		for (int k = 0; k < m.ni; k++) mask.i[mask.ni++] = b0 + m.idx[k];
	}

	public void MaskText(Font f, string text, int fontSize, Transform2D deviceXf, float alpha, TextServer.Direction dir, ShadowMap map, int outline = 0)
	{
		RenderingServer.CanvasItemAddSetTransform(maskTextRoot, map.Xf * deviceXf);
		var c = new Color(1, 1, 1, alpha);
		if (outline > 0) f.DrawStringOutline(maskTextRoot, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, outline, c, TextServer.JustificationFlag.None, dir);
		else f.DrawString(maskTextRoot, Vector2.Zero, text, HorizontalAlignment.Left, -1, fontSize, c, TextServer.JustificationFlag.None, dir);
	}

	// ---------- snapshots (drawImage of a copy of the picture) ----------
	public void Image(int cont, Rid tex, Vector2[] quad, Vector2[] uv, float alpha)
	{
		FlushBatch();
		curIsBatch = false;
		var it = NewItem(cont, default);
		var tb = gradBuf; tb.Clear();
		var c = new Color(1, 1, 1, alpha);
		int i0 = tb.V(quad[0], c, uv[0]), i1 = tb.V(quad[1], c, uv[1]), i2 = tb.V(quad[2], c, uv[2]), i3 = tb.V(quad[3], c, uv[3]);
		tb.Q(i0, i1, i2, i3);
		tb.Submit(it, tex);
	}
}
