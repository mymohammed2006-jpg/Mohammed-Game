using System;

namespace MG;

// The menus, online, account, practice and the rest of the UI are ported in src/Game/UI/*.cs.
public static class Hooks
{
	// lines 7246 typing() (account text fields)
	public static Func<bool> typing = () => G.typing();
}
