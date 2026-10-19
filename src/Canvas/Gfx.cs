using Godot;
using System;

namespace MG.Canvas;

// The game's picture: a SubViewport the size of the browser canvas backing store, the canvas context `g`
// that draws into it, and the three small viewports used for shadows (mask -> blur across -> blur down).
// Child viewports are rendered before their parent, so the shadows are ready when the picture is drawn.
public partial class Gfx : Node
{
	public SubViewport game;
	public Ctx g;
	public Surface surf;
	public TextureRect view;
	SubViewport mask, blurH, blurV;
	Node2D gameRoot;

	public int Width => game.Size.X;
	public int Height => game.Size.Y;

	static SubViewport Vp(Vector2I size, bool transparent)
	{
		return new SubViewport
		{
			Size = size,
			TransparentBg = transparent,
			Disable3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			RenderTargetClearMode = SubViewport.ClearMode.Always,
			CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Linear,
			Snap2DTransformsToPixel = false,
			Snap2DVerticesToPixel = false,
		};
	}

	public override void _Ready()
	{
		Fonts.Init();
		game = Vp(new Vector2I(1280, 720), false);
		AddChild(game);
		gameRoot = new Node2D();
		game.AddChild(gameRoot);

		blurV = Vp(new Vector2I(Surface.A, Surface.A), true);
		blurH = Vp(new Vector2I(Surface.A, Surface.A), true);
		mask = Vp(new Vector2I(Surface.A, Surface.A), true);
		game.AddChild(blurV);
		blurV.AddChild(blurH);
		blurH.AddChild(mask);
		var maskRoot = new Node2D(); mask.AddChild(maskRoot);
		var bhRoot = new Node2D(); blurH.AddChild(bhRoot);
		var bvRoot = new Node2D(); blurV.AddChild(bvRoot);
		var blurShader = GD.Load<Shader>("res://shaders/blur.gdshader");
		bhRoot.Material = new ShaderMaterial { Shader = blurShader };
		((ShaderMaterial)bhRoot.Material).SetShaderParameter("src", mask.GetTexture());
		((ShaderMaterial)bhRoot.Material).SetShaderParameter("dir", new Vector2(1, 0));
		bvRoot.Material = new ShaderMaterial { Shader = blurShader };
		((ShaderMaterial)bvRoot.Material).SetShaderParameter("src", blurH.GetTexture());
		((ShaderMaterial)bvRoot.Material).SetShaderParameter("dir", new Vector2(0, 1));
		// items under the roots inherit nothing; give the blur items the root material explicitly
		var bhItem = RenderingServer.CanvasItemCreate();
		RenderingServer.CanvasItemSetParent(bhItem, bhRoot.GetCanvasItem());
		RenderingServer.CanvasItemSetMaterial(bhItem, bhRoot.Material.GetRid());
		var bvItem = RenderingServer.CanvasItemCreate();
		RenderingServer.CanvasItemSetParent(bvItem, bvRoot.GetCanvasItem());
		RenderingServer.CanvasItemSetMaterial(bvItem, bvRoot.Material.GetRid());
		var whiteItem = RenderingServer.CanvasItemCreate();
		RenderingServer.CanvasItemSetParent(whiteItem, bvRoot.GetCanvasItem());
		RenderingServer.CanvasItemSetDrawIndex(whiteItem, 1);
		var maskTris = RenderingServer.CanvasItemCreate();
		RenderingServer.CanvasItemSetParent(maskTris, maskRoot.GetCanvasItem());
		var maskText = RenderingServer.CanvasItemCreate();
		RenderingServer.CanvasItemSetParent(maskText, maskRoot.GetCanvasItem());
		RenderingServer.CanvasItemSetDrawIndex(maskText, 1);

		surf = new Surface(gameRoot.GetCanvasItem(), maskTris, maskText, bhItem, bvItem, whiteItem,
			blurV.GetTexture().GetRid(), GD.Load<Shader>("res://shaders/grad.gdshader"));
		g = new Ctx(surf, 1280, 720);

		view = new TextureRect
		{
			Texture = game.GetTexture(),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
		};
		view.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var layer = new CanvasLayer();
		AddChild(layer);
		layer.AddChild(view);
	}

	// like setting canvas.width/height: new backing size, context state reset
	public void Resize(int w, int h)
	{
		w = Math.Max(1, w); h = Math.Max(1, h);
		if (game.Size.X != w || game.Size.Y != h) game.Size = new Vector2I(w, h);
		g.Reset(w, h);
	}

	public void BeginFrame()
	{
		g.S.clip = 0;
		surf.BeginFrame(game.Size.X, game.Size.Y);
	}

	public void EndFrame() => surf.EndFrame();
}
