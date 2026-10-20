using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// Rooms in Firebase (online/firestore-online.rules): rooms/{5 digits} = h (maker's uid), hn (maker's name), lv (level id), o (offer),
// at (when made); the one who joins adds g (uid), gn (name), a (answer). The maker deletes the room once the two PCs are connected.
// A room older than 10 minutes can be made again with the same code.
// A room for three (the co-op level for three) has a second offer o2: the first one in takes g / gn / a, the second g2 / gn2 / a2.
public static partial class G
{
	static JsonObject fsStr(string s) => new JsonObject { ["stringValue"] = s };
	static string roomPath(string code) => $"{FS_DOC}/rooms/{code}";

	// a new room with a free code (null = no luck after a few tries)
	static async Task<string> roomMake(string lv, string offer, string offer2 = null)
	{
		for (int i = 0; i < 6; i++)
		{
			string code = ((int)Math.Floor(random() * 90000) + 10000).ToString();
			var w = new JsonObject
			{
				["update"] = new JsonObject { ["name"] = roomPath(code), ["fields"] = roomFields(lv, offer, offer2) },
				["updateTransforms"] = new JsonArray(new JsonObject { ["fieldPath"] = "at", ["setToServerValue"] = "REQUEST_TIME" }),
			};
			try { await commit(w); return code; }
			catch (FetchErr e) when (e.code == 403) { }   // that code is taken right now
		}
		return null;
	}
	static JsonObject roomFields(string lv, string offer, string offer2)
	{
		var f = new JsonObject { ["h"] = fsStr(acc.uid), ["hn"] = fsStr(acc.name), ["lv"] = fsStr(lv), ["o"] = fsStr(offer) };
		if (offer2 != null) f["o2"] = fsStr(offer2);
		return f;
	}
	public class RoomDoc { public string h, hn, lv, o, g, gn, a, o2, g2, gn2, a2; }
	// null = no such room
	static async Task<RoomDoc> roomGet(string code)
	{
		JsonNode d;
		try { d = await jfetch($"{FS_URL}/rooms/{code}", tok: await idToken()); }
		catch (FetchErr e) when (e.code == 404) { return null; }
		var f = d["fields"] as JsonObject ?? new JsonObject(); string F(string k) => S(f[k]?["stringValue"]);
		return new RoomDoc { h = F("h"), hn = F("hn"), lv = F("lv"), o = F("o"), g = F("g"), gn = F("gn"), a = F("a"), o2 = F("o2"), g2 = F("g2"), gn2 = F("gn2"), a2 = F("a2") };
	}
	// seat 1 = the first one in, seat 2 = the second (rooms for three); the rules refuse a seat already taken (403)
	static Task roomAnswer(string code, string answer, int seat = 1)
	{
		string x = seat == 2 ? "2" : "";
		return commit(new JsonObject
		{
			["update"] = new JsonObject { ["name"] = roomPath(code), ["fields"] = new JsonObject { ["g" + x] = fsStr(acc.uid), ["gn" + x] = fsStr(acc.name), ["a" + x] = fsStr(answer) } },
			["updateMask"] = new JsonObject { ["fieldPaths"] = new JsonArray("g" + x, "gn" + x, "a" + x) },
			["currentDocument"] = new JsonObject { ["exists"] = true },
		});
	}
	static async Task roomDel(string code) { try { await commit(new JsonObject { ["delete"] = roomPath(code) }); } catch (Exception) { } }
}
