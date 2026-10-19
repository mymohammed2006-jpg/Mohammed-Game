using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// Rooms in Firebase (online/firestore-online.rules): rooms/{5 digits} = h (maker's uid), hn (maker's name), lv (level id), o (offer),
// at (when made); the one who joins adds g (uid), gn (name), a (answer). The maker deletes the room once the two PCs are connected.
// A room older than 10 minutes can be made again with the same code.
public static partial class G
{
	static JsonObject fsStr(string s) => new JsonObject { ["stringValue"] = s };
	static string roomPath(string code) => $"{FS_DOC}/rooms/{code}";

	// a new room with a free code (null = no luck after a few tries)
	static async Task<string> roomMake(string lv, string offer)
	{
		for (int i = 0; i < 6; i++)
		{
			string code = ((int)Math.Floor(random() * 90000) + 10000).ToString();
			var w = new JsonObject
			{
				["update"] = new JsonObject { ["name"] = roomPath(code), ["fields"] = new JsonObject { ["h"] = fsStr(acc.uid), ["hn"] = fsStr(acc.name), ["lv"] = fsStr(lv), ["o"] = fsStr(offer) } },
				["updateTransforms"] = new JsonArray(new JsonObject { ["fieldPath"] = "at", ["setToServerValue"] = "REQUEST_TIME" }),
			};
			try { await commit(w); return code; }
			catch (FetchErr e) when (e.code == 403) { }   // that code is taken right now
		}
		return null;
	}
	public class RoomDoc { public string h, hn, lv, o, g, gn, a; }
	// null = no such room
	static async Task<RoomDoc> roomGet(string code)
	{
		JsonNode d;
		try { d = await jfetch($"{FS_URL}/rooms/{code}", tok: await idToken()); }
		catch (FetchErr e) when (e.code == 404) { return null; }
		var f = d["fields"] as JsonObject ?? new JsonObject(); string F(string k) => S(f[k]?["stringValue"]);
		return new RoomDoc { h = F("h"), hn = F("hn"), lv = F("lv"), o = F("o"), g = F("g"), gn = F("gn"), a = F("a") };
	}
	static Task roomAnswer(string code, string answer) => commit(new JsonObject
	{
		["update"] = new JsonObject { ["name"] = roomPath(code), ["fields"] = new JsonObject { ["g"] = fsStr(acc.uid), ["gn"] = fsStr(acc.name), ["a"] = fsStr(answer) } },
		["updateMask"] = new JsonObject { ["fieldPaths"] = new JsonArray("g", "gn", "a") },
		["currentDocument"] = new JsonObject { ["exists"] = true },
	});
	static async Task roomDel(string code) { try { await commit(new JsonObject { ["delete"] = roomPath(code) }); } catch (Exception) { } }
}
