using Godot;

namespace MG;

// The original's hidden <input> for the account screen: while it has the focus, typed letters, Backspace and paste go into it.
// It sits under the canvas node, so it sees each key before Main turns it into a game key (and Main still gets it).
public partial class TextIn : Node
{
	static TextIn node;
	public static void Ensure()
	{
		if (node != null || G.gfx == null || !G.gfx.IsInsideTree()) return;
		node = new TextIn(); G.gfx.CallDeferred(Node.MethodName.AddChild, node);
	}
	public override void _Input(InputEvent e)
	{
		if (e is not InputEventKey k || !k.Pressed || !G.typing()) return;
		if (k.Keycode == Key.Backspace || k.PhysicalKeycode == Key.Backspace) { G.tfKey("", true); return; }
		if ((k.CtrlPressed || k.MetaPressed) && (k.Keycode == Key.V || k.PhysicalKeycode == Key.V)) { G.tfKey(DisplayServer.ClipboardGet(), false); return; }
		if (k.MetaPressed || k.CtrlPressed != k.AltPressed) return;   // Ctrl+Alt together = AltGr, which types letters
		if (k.Unicode >= 32 && k.Unicode != 127) G.tfKey(char.ConvertFromUtf32((int)k.Unicode), false);
	}
}
