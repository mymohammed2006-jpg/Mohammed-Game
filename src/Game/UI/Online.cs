using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MG.JS;

namespace MG;

// original index.html lines 7130-7247 (points, account, online sync, board, typing field) and 7405-7447, 7482-7485 (replay upload, profile replays, replay download)
// Same Firebase REST calls with the same documents as the original. Async code continues on the main thread (Godot's synchronization context), like JS promises.
public static partial class G
{
	public static readonly Dictionary<string, double> PTS = new() { ["S"] = 100, ["A"] = 70, ["B"] = 40, ["C"] = 20 };
	public static double ptsOf(bool e) { double s = 0; foreach (var L in ALL_LV()) { var r = e ? L.bestE : L.best; if (r != null) s += (e ? PTS[r] / 2 : PTS[r]) * ptsMul(L); } return s; }
	public static double ptsMax(bool e) => ALL_LV().Where(L => !(e && noEz(L))).Sum(L => ptsMul(L)) * (e ? PTS["S"] / 2 : PTS["S"]);   // no Easy in the platformer or Hardcore

	// ---------- the online part ----------
	const string FB_KEY = "AIzaSyCgg3Wd_ZN4o_c-2xg_hGlXq2o0mfH75CU", FB_PROJECT = "mohammed-game-f7563";
	const string FS_DOC = "projects/" + FB_PROJECT + "/databases/(default)/documents", FS_URL = "https://firestore.googleapis.com/v1/" + FS_DOC;
	static readonly Regex NAME_RE = new("^[A-Za-z0-9_ء-غف-ي٠-٩]{3,12}\\z");

	public class Acc { public string uid { get; set; } public string name { get; set; } public bool dirty { get; set; } public string it { get; set; } public string rt { get; set; } public double exp { get; set; } }
	public static Acc acc = null;
	static bool onlineInit = false;
	static readonly JsonSerializerOptions JOPT = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
	// the boot lines of the original (account from the device, the replay queue, and `if(acc)sync()`), on the first frame
	static void onlineBoot()
	{
		if (onlineInit) return; onlineInit = true;
		try { var a = JsonSerializer.Deserialize<Acc>(localStorage.getItem("mg_acc") ?? "null"); if (a != null && !string.IsNullOrEmpty(a.uid) && !string.IsNullOrEmpty(a.rt) && !string.IsNullOrEmpty(a.name)) acc = a; } catch (Exception) { }
		try { rpq = JsonSerializer.Deserialize<Dictionary<string, RpqItem>>(localStorage.getItem("mg_rpq") ?? "{}") ?? new(); } catch (Exception) { rpq = new(); }
		if (acc != null) sync();
	}
	static void saveAcc() { if (acc != null) localStorage.setItem("mg_acc", JsonSerializer.Serialize(acc, JOPT)); else localStorage.removeItem("mg_acc"); }
	static string nameKey(string n) => n.ToLowerInvariant();
	static string nameEmail(string n) => "u" + string.Concat(Encoding.UTF8.GetBytes(nameKey(n)).Select(b => b.ToString("x2"))) + "@mohammed-game.app";
	static readonly (string k, string v)[] ERR_AR =
	{
		("EMAIL_EXISTS", "هذا الاسم مأخوذ، اختر اسم ثاني"), ("INVALID_LOGIN_CREDENTIALS", "الاسم أو كلمة السر غلط"), ("EMAIL_NOT_FOUND", "الاسم أو كلمة السر غلط"), ("INVALID_PASSWORD", "الاسم أو كلمة السر غلط"),
		("WEAK_PASSWORD", "كلمة السر لازم ٦ حروف أو أكثر"), ("TOO_MANY_ATTEMPTS_TRY_LATER", "محاولات كثيرة، جرّب بعد شوي"), ("USER_DISABLED", "هذا الحساب موقّف"), ("NET", "ما فيه نت، تأكد من الاتصال وجرّب مرة ثانية"),
	};
	static string errAr(Exception e) { string m = e?.Message ?? ""; foreach (var (k, v) in ERR_AR) if (m.StartsWith(k, StringComparison.Ordinal)) return v; return "صار خطأ، جرّب مرة ثانية"; }

	public class FetchErr : Exception { public int code; public FetchErr(string m, int c = 0) : base(m) { code = c; } }
	// tests replace this to see the requests (method, url, headers, body) and answer them
	public static Func<HttpRequestMessage, Task<HttpResponseMessage>> netSend = r => http.SendAsync(r);
	static readonly HttpClient http = new();
	static async Task<JsonNode> jfetch(string url, string method = "GET", string body = null, string ctype = null, string tok = null)
	{
		HttpResponseMessage r;
		try
		{
			var q = new HttpRequestMessage(method == "POST" ? HttpMethod.Post : HttpMethod.Get, url);
			if (tok != null) q.Headers.TryAddWithoutValidation("Authorization", "Bearer " + tok);
			if (body != null) { q.Content = new StringContent(body, Encoding.UTF8); q.Content.Headers.Remove("Content-Type"); q.Content.Headers.TryAddWithoutValidation("Content-Type", ctype); }
			r = await netSend(q);
		}
		catch (Exception) { throw new FetchErr("NET"); }
		JsonNode j;
		try { j = JsonNode.Parse(await r.Content.ReadAsStringAsync()) ?? new JsonObject(); } catch (Exception) { j = new JsonObject(); }
		if (!r.IsSuccessStatusCode)
		{
			var er = j is JsonObject o && o["error"] is JsonObject eo ? (S(eo["message"]) ?? S(eo["status"])) : null;
			throw new FetchErr(!string.IsNullOrEmpty(er) ? er : "HTTP_" + (int)r.StatusCode, (int)r.StatusCode);
		}
		return j;
	}
	// a JSON string (or a number written as text), like reading a field in JS
	static string S(JsonNode n) => n is JsonValue v ? (v.TryGetValue<string>(out var s) ? s : v.ToJsonString()) : null;
	static string idUrl(string m) => $"https://identitytoolkit.googleapis.com/v1/accounts:{m}?key={FB_KEY}";
	static Task<JsonNode> jpost(string url, JsonNode body, string tok = null) => jfetch(url, "POST", body.ToJsonString(JOPT), "application/json", tok);
	static double dateNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	static void setTok(JsonNode j)
	{
		acc.it = S(j["idToken"]) is { Length: > 0 } a ? a : S(j["id_token"]); acc.rt = S(j["refreshToken"]) is { Length: > 0 } b ? b : S(j["refresh_token"]);
		double ex = num(S(j["expiresIn"]) is { Length: > 0 } c ? c : S(j["expires_in"])); acc.exp = dateNow() + (ex != 0 && !double.IsNaN(ex) ? ex : 3600) * 1000; saveAcc();
	}
	static readonly Regex TOK_BAD = new("TOKEN_EXPIRED|USER_DISABLED|USER_NOT_FOUND|INVALID_REFRESH_TOKEN");
	static async Task<string> idToken()
	{
		if (acc == null) throw new FetchErr("NO_ACC"); if (!string.IsNullOrEmpty(acc.it) && dateNow() < acc.exp - 60000) return acc.it;
		JsonNode j;
		try { j = await jfetch($"https://securetoken.googleapis.com/v1/token?key={FB_KEY}", "POST", "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(acc.rt), "application/x-www-form-urlencoded"); }
		catch (Exception e) { if (TOK_BAD.IsMatch(e.Message)) logout(); throw; }
		setTok(j); return acc.it;
	}
	// Firestore values
	static JsonObject fsMap(Dictionary<string, string> m, bool ts = false)
	{
		var f = new JsonObject(); foreach (var (k, v) in m) f[k] = ts ? new JsonObject { ["timestampValue"] = v } : new JsonObject { ["stringValue"] = v };
		return new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = f } };
	}
	static Dictionary<string, string> unMap(JsonNode v)
	{
		var o = new Dictionary<string, string>(); if (v?["mapValue"]?["fields"] is JsonObject f) foreach (var (k, x) in f) { var s = S(x?["stringValue"]); o[k] = !string.IsNullOrEmpty(s) ? s : S(x?["timestampValue"]); }
		return o;
	}
	// pt: the platformer's best times (seconds, with the best letter; the board goes by the letter, then the time)
	static Dictionary<string, double> unNum(JsonNode v)
	{
		var o = new Dictionary<string, double>(); if (v?["mapValue"]?["fields"] is JsonObject f) foreach (var (k, x) in f) { if (x?["doubleValue"] is JsonNode d) o[k] = d.GetValue<double>(); else if (double.TryParse(S(x?["integerValue"]), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double iv)) o[k] = iv; }
		return o;
	}
	static JsonObject fsNum(Dictionary<string, double> m) { var f = new JsonObject(); foreach (var (k, v) in m) f[k] = new JsonObject { ["doubleValue"] = v }; return new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = f } }; }
	public class PlayerDoc { public string uid, name; public int xp; public Dictionary<string, double> pt = new(); public Dictionary<string, string> n, e, nt, et; public Dictionary<string, string> F(string f) => f == "n" ? n : f == "e" ? e : f == "nt" ? nt : et; }
	static PlayerDoc parsePlayer(JsonNode d)
	{
		var f = d["fields"] as JsonObject ?? new JsonObject(); string nm = S(f["name"]?["stringValue"]);
		int.TryParse(S(f["xp"]?["integerValue"]), out int xv);
		return new PlayerDoc { uid = S(d["name"]).Split('/').Last(), name = !string.IsNullOrEmpty(nm) ? nm : "?", xp = Math.Max(0, xv), n = unMap(f["n"]), e = unMap(f["e"]), nt = unMap(f["nt"]), et = unMap(f["et"]), pt = unNum(f["pt"]) };
	}
	static string gv(Dictionary<string, string> d, string k) => d.TryGetValue(k, out var v) && !string.IsNullOrEmpty(v) ? v : null;
	static bool better(string a, string b) => a != null && (b == null || Array.IndexOf(RANKS, a) < Array.IndexOf(RANKS, b));
	static (Dictionary<string, string> n, Dictionary<string, string> e) localBests()
	{
		var n = new Dictionary<string, string>(); var e = new Dictionary<string, string>(); foreach (var L in ALL_LV()) { if (L.best != null) n[L.id] = L.best; if (L.bestE != null) e[L.id] = L.bestE; }
		return (n, e);
	}
	static void setLocal(Level L, bool ez, string r, double t = -1)
	{
		if (ez) L.bestE = r; else L.best = r;
		string k = (ez ? "mg_beste_" : "mg_best_") + L.name; if (r != null) localStorage.setItem(k, r); else localStorage.removeItem(k);
		if (L.plat && !ez) setBestT(L, r != null ? t : -1);
	}
	static double ptOf(PlayerDoc p, string id) => p.pt.TryGetValue(id, out var t) && t >= 0 ? t : -1;
	static Dictionary<string, double> localTimes() { var o = new Dictionary<string, double>(); foreach (var L in ALL_LV()) if (L.plat && L.best != null && L.bestT >= 0) o[L.id] = L.bestT; return o; }
	// writing the player file: only the fields that changed, plus the time of each new letter and of the last save (server clock)
	static JsonObject playerWrite(JsonObject fields, List<string> mask, List<string> tsPaths, bool create)
	{
		var w = new JsonObject
		{
			["update"] = new JsonObject { ["name"] = $"{FS_DOC}/players/{acc.uid}", ["fields"] = fields },
			["updateTransforms"] = new JsonArray(new[] { "at" }.Concat(tsPaths).Select(p => (JsonNode)new JsonObject { ["fieldPath"] = p, ["setToServerValue"] = "REQUEST_TIME" }).ToArray()),
			["currentDocument"] = new JsonObject { ["exists"] = !create },
		};
		if (mask != null) w["updateMask"] = new JsonObject { ["fieldPaths"] = new JsonArray(mask.Select(m => (JsonNode)m).ToArray()) };
		return w;
	}
	static async Task<JsonNode> commit(params JsonObject[] writes) { var body = new JsonObject { ["writes"] = new JsonArray(writes) }; return await jpost($"{FS_URL}:commit", body, await idToken()); }
	// the player's XP goes in their file too (not in the comparison runs, which copy v39)
	static JsonObject xpField(JsonObject f) { if (!classicUi) f["xp"] = new JsonObject { ["integerValue"] = str(xp) }; return f; }
	static JsonObject ptField(JsonObject f) { var t = localTimes(); if (!classicUi && t.Count > 0) f["pt"] = fsNum(t); return f; }
	static async Task createPlayer()
	{
		var (n, e) = localBests(); var ts = n.Keys.Select(k => "nt." + k).Concat(e.Keys.Select(k => "et." + k)).ToList();
		await commit(new JsonObject { ["update"] = new JsonObject { ["name"] = $"{FS_DOC}/names/{nameKey(acc.name)}", ["fields"] = new JsonObject { ["uid"] = new JsonObject { ["stringValue"] = acc.uid } } }, ["currentDocument"] = new JsonObject { ["exists"] = false } },
			playerWrite(ptField(xpField(new JsonObject { ["name"] = new JsonObject { ["stringValue"] = acc.name }, ["n"] = fsMap(n), ["e"] = fsMap(e), ["nt"] = fsMap(new(), true), ["et"] = fsMap(new(), true) })), null, ts, true));
		board.at = 0;
	}
	// sync: the better letter of the device and the account becomes both
	public static bool syncing = false; public static string netState = ""; static double lastSyncTry = -1e9; static bool upShow = false, upFailShown = false;
	// the "saved online" message: bottom right, fades in and out
	class Corner { public string t = "", col = "#fff"; public double start, end; }
	static readonly Corner corner = new();
	static void showCorner(string t, string col, double dur) { double now = performance.now(); if (now > corner.end) corner.start = now; corner.t = t; corner.col = col; corner.end = now + dur; }
	public static void drawCorner(double now)
	{
		if (now >= corner.end || string.IsNullOrEmpty(corner.t)) return; double a = Math.Min(1, Math.Min((now - corner.start) / 400, (corner.end - now) / 600)); if (a <= 0) return;
		g.save(); g.globalAlpha = a; g.font = $"800 24px {FONT}"; double w = g.measureText(corner.t).width + 48, h = 50, x = W - 24 - w, y = H - (fpsShow ? 108 : 100);
		rr(x, y, w, h, 14); g.fillStyle = "rgba(8,8,14,0.88)"; g.fill(); g.strokeStyle = corner.col; g.lineWidth = 2; g.stroke();
		txt(corner.t, W - 48, y + h / 2 + 1, 24, corner.col, 800, "right"); g.restore();
	}
	public static void sync() => _ = syncA();
	static async Task syncA()
	{
		if (acc == null || syncing) return; syncing = true; lastSyncTry = performance.now();
		try
		{
			JsonNode doc = null; try { doc = await jfetch($"{FS_URL}/players/{acc.uid}", tok: await idToken()); } catch (FetchErr e) when (e.code == 404) { }
			if (acc == null) return;
			if (doc == null) await createPlayer();
			else
			{
				var on = parsePlayer(doc); var up = new Dictionary<string, Dictionary<string, string>>(); var upT = new Dictionary<string, double>(); var mask = new List<string>(); var ts = new List<string>();
				foreach (var L in ALL_LV()) foreach (bool ez in new[] { false, true })
					{
						string f = ez ? "e" : "n", o = gv(on.F(f), L.id), l = ez ? L.bestE : L.best;
						bool pt = L.plat && !ez && !classicUi; double ot = ptOf(on, L.id), lt = L.bestT;
						if (better(o, l)) setLocal(L, ez, o, ot);
						else if (better(l, o))
						{
							if (!up.ContainsKey(f)) up[f] = new(); up[f][L.id] = l; mask.Add($"{f}.{L.id}"); ts.Add($"{f}t.{L.id}");
							if (pt && (lt >= 0 || ot >= 0)) { if (lt >= 0) upT[L.id] = lt; mask.Add($"pt.{L.id}"); }   // the new letter's own time (none: the old letter's time goes)
						}
						else if (pt && l != null && ot >= 0 && (lt < 0 || ot < lt)) setBestT(L, ot);   // same letter: the faster time
						else if (pt && l != null && lt >= 0 && (ot < 0 || lt < ot)) { upT[L.id] = lt; mask.Add($"pt.{L.id}"); }
					}
				bool xpUp = !classicUi && xp > on.xp; if (xpUp) mask.Add("xp");
				if (mask.Count > 0) { var fields = new JsonObject(); foreach (var (f, m) in up) fields[f] = fsMap(m); if (upT.Count > 0) fields["pt"] = fsNum(upT); if (xpUp) xpField(fields); await commit(playerWrite(fields, mask, ts, false)); board.at = 0; }
			}
			acc.dirty = false; saveAcc(); await flushRpq(); netState = "ok"; if (upShow) { upShow = false; upFailShown = false; showCorner("انحفظ أونلاين ✓", "#5fe08f", 2800); }
		}
		catch (Exception e) { netState = e.Message == "NET" ? "net" : "err"; if (upShow && !upFailShown) { upFailShown = true; showCorner("بينحفظ أونلاين أول ما يرجع النت", "#ffb35c", 3500); } }
		finally { syncing = false; }
	}
	public static void markDirty() { if (acc == null) return; acc.dirty = true; saveAcc(); upShow = true; upFailShown = false; showCorner("جاري الحفظ أونلاين…", "#ffffff", 10000); sync(); }
	static void logout() { acc = null; saveAcc(); rpq = new(); saveRpq(); foreach (var L in ALL_LV()) { setLocal(L, false, null); setLocal(L, true, null); } upShow = false; }
	static bool authBusy = false, authErr = false; static string authMsg = "";
	static async Task doAuth(bool signup, string name, string pass)
	{
		if (authBusy) return; name = name.Trim();
		if (!NAME_RE.IsMatch(name)) { authErr = true; authMsg = "الاسم من ٣ إلى ١٢ حرف أو رقم، بدون مسافات"; return; }
		if (pass.Length < 6) { authErr = true; authMsg = "كلمة السر لازم ٦ حروف أو أكثر"; return; }
		authBusy = true; authErr = false; authMsg = signup ? "جاري إنشاء الحساب…" : "جاري الدخول…";
		try
		{
			var j = await jpost(idUrl(signup ? "signUp" : "signInWithPassword"), new JsonObject { ["email"] = nameEmail(name), ["password"] = pass, ["returnSecureToken"] = true });
			acc = new Acc { uid = S(j["localId"]), name = name, dirty = true }; setTok(j);
			if (signup) { try { await createPlayer(); acc.dirty = false; saveAcc(); } catch (Exception) { } }
			else
			{
				// the name saved in the account (with its own capital and small letters)
				try { var d = await jfetch($"{FS_URL}/players/{acc.uid}", tok: acc.it); acc.name = parsePlayer(d).name; saveAcc(); } catch (Exception) { }
				await syncA();
			}
			authMsg = ""; accSel = 0; tfValue = ""; passVal = ""; nameVal = ""; state = "main"; toast = $"أهلًا {acc.name}!"; toastT = 2.4;
		}
		catch (Exception e) { authErr = true; authMsg = errAr(e); }
		finally { authBusy = false; }
	}
	// the board: everyone's player files (there are few) sorted on the device
	public class BoardRow { public PlayerDoc p; public string r; public double v; public string t; public double ft = -1; public int pos; public bool me; }
	public class BoardState { public double at; public bool busy, err; public List<PlayerDoc> list = new(); public int idx; public string mode = "n"; public int sel = -1; }
	public static readonly BoardState board = new();
	static async Task loadBoard(bool force)
	{
		if (board.busy || (!force && board.at != 0 && performance.now() - board.at < 60000)) return; board.busy = true; board.err = false;
		try
		{
			var o = new List<PlayerDoc>(); string tok = "";
			do
			{
				var j = await jfetch($"{FS_URL}/players?pageSize=300{(tok != "" ? "&pageToken=" + Uri.EscapeDataString(tok) : "")}");
				if (j["documents"] is JsonArray ds) foreach (var d in ds) o.Add(parsePlayer(d)); tok = S(j["nextPageToken"]) ?? "";
			} while (tok != "");
			board.list = o; board.at = performance.now();
		}
		catch (Exception) { board.err = true; }
		finally { board.busy = false; }
	}
	// New in the Godot version: the first board is the players' levels (XP), then the points, then the levels
	static int bOff => classicUi ? 0 : 1;
	static bool boardXp => !classicUi && board.idx == 0;
	static Level boardLv(int i) => i - bOff >= 1 ? ALL_LV()[i - bOff - 1] : null;
	static string[] boardModes() => boardXp ? new[] { "x" } : noEz(boardLv(board.idx)) ? new[] { "n" } : board.idx != bOff ? new[] { "n", "e" } : new[] { "n", "e", "t" };
	public static double ptsOfP(PlayerDoc p, string f) { double s = 0; foreach (var L in ALL_LV()) { var r = gv(p.F(f), L.id); if (r != null) s += (f == "e" ? PTS[r] / 2 : PTS[r]) * ptsMul(L); } return s; }
	static List<BoardRow> boardRows()
	{
		var L = boardLv(board.idx); string m = board.mode; List<BoardRow> rows;
		if (L != null)
		{
			string f = m, t = f + "t";
			rows = board.list.Where(p => gv(p.F(f), L.id) != null).Select(p => new BoardRow { p = p, r = gv(p.F(f), L.id), v = -Array.IndexOf(RANKS, gv(p.F(f), L.id)), t = gv(p.F(t), L.id) ?? "", ft = L.plat && f == "n" ? ptOf(p, L.id) : -1 }).ToList();
			// the platformer: the letter first, then the faster time (no time yet = after the times); then who got there first
			sort(rows, (a, b) => { double d = b.v - a.v; if (d != 0) return d; if (a.ft != b.ft) return a.ft < 0 ? 1 : b.ft < 0 ? -1 : a.ft - b.ft; return string.CompareOrdinal(a.t, b.t) < 0 ? -1 : string.CompareOrdinal(a.t, b.t) > 0 ? 1 : 0; });
		}
		else if (boardXp)
		{
			rows = board.list.Select(p => new BoardRow { p = p, v = xpOfP(p) }).Where(x => x.v > 0).ToList();
			sort(rows, (a, b) => { double d = b.v - a.v; return d != 0 ? d : string.Compare(a.p.name, b.p.name, StringComparison.InvariantCulture); });
		}
		else
		{
			rows = board.list.Select(p => new BoardRow { p = p, v = m == "t" ? ptsOfP(p, "n") + ptsOfP(p, "e") : ptsOfP(p, m) }).Where(x => x.v > 0).ToList();
			sort(rows, (a, b) => { double d = b.v - a.v; return d != 0 ? d : string.Compare(a.p.name, b.p.name, StringComparison.InvariantCulture); });
		}
		for (int i = 0; i < rows.Count; i++) { rows[i].pos = i + 1; rows[i].me = acc != null && rows[i].p.uid == acc.uid; }
		return rows;
	}
	static List<BoardRow> boardVisible() { var rows = boardRows(); var v = rows.Take(10).ToList(); var mine = rows.FirstOrDefault(x => x.me); if (mine != null && mine.pos > 10) v.Add(mine); return v; }

	// the hidden typing field: the keyboard types into it and the game draws what is in it
	static int accSel = 0; static string nameVal = "", passVal = ""; static double logoutArm = 0;
	static string tfValue = "", tfField = ""; static int tfMax = 12; static bool tfFocus = false;
	public static bool typing() => state == "account" && acc == null && accSel < 2;
	public static void syncField()
	{
		onlineBoot(); TextIn.Ensure();
		bool want = typing();
		if (want)
		{
			string v = accSel == 0 ? nameVal : passVal; tfMax = accSel == 0 ? 12 : 32; if (tfField != str(accSel)) { tfField = str(accSel); tfValue = v; }
			tfFocus = true; if (accSel == 0) nameVal = Regex.Replace(tfValue, "\\s", ""); else passVal = tfValue;
		}
		else if (tfFocus) { tfFocus = false; tfField = ""; }
	}
	// what the input box does with a key while it has the focus (letters, Backspace, paste)
	public static void tfKey(string text, bool backspace)
	{
		if (!tfFocus) return;
		if (backspace) { if (tfValue.Length > 0) tfValue = tfValue.Substring(0, tfValue.Length - (tfValue.Length > 1 && char.IsLowSurrogate(tfValue[^1]) ? 2 : 1)); return; }
		text = text.Replace("\r", "").Replace("\n", "");
		int room = tfMax - tfValue.Length; if (room <= 0 || text.Length == 0) return;
		if (text.Length > room) text = text.Substring(0, room);
		tfValue += text;
	}
	public static void syncTick(double now)
	{
		if (acc != null && acc.dirty && !syncing && now - lastSyncTry > 30000 && !(state is "play" or "starting" or "levelIntro")) sync();
	}

	// ---------- replays: saving and uploading ----------
	// v1 = recorded before the AFK fix (plays on the old level shape), v2 = from that update on
	public class RpqSeg { public double cp { get; set; } public int seed { get; set; } public double t0 { get; set; } public int n { get; set; } public string d { get; set; } public int c { get; set; } }   // c 1 = cut before its death (v46)
	public class RpqItem { public string lvl { get; set; } public string m { get; set; } public string r { get; set; } public int v { get; set; } public int hz { get; set; } public List<RpqSeg> segs { get; set; } public string sk { get; set; } }
	static Dictionary<string, RpqItem> rpq = new();
	static void saveRpq() => localStorage.setItem("mg_rpq", JsonSerializer.Serialize(rpq, JOPT));
	static string packRp(List<int> a)
	{
		var u = new byte[a.Count / 3 * 5];
		for (int i = 0, j = 0; i < a.Count; i += 3, j += 5) { u[j] = (byte)(a[i] & 255); u[j + 1] = (byte)(a[i] >> 8); u[j + 2] = (byte)(a[i + 1] & 255); u[j + 3] = (byte)(a[i + 1] >> 8); u[j + 4] = (byte)a[i + 2]; }
		return Convert.ToBase64String(u);
	}
	// after a win: the replay waits in a queue (it goes online with the sync)
	public static void rpWin(string r)
	{
		lastRec = rec;   // the win screen can still save it on the PC (v46)
		if (rec == null || rec.segs.Count == 0) return; var L = cur; string m = easy ? "e" : "n", best = easy ? L.bestE : L.best; rec.cur = null;
		if (best != null && Array.IndexOf(RANKS, r) > Array.IndexOf(RANKS, best)) { rec = null; return; }
		var segs = rpSegs(rec.segs); var all = rec.segs; rec = null;
		// no limit on deaths: the limit is the file size (Firebase takes about 1 MB per file) = about 45 minutes of play
		// v46, Hardcore only (his idea 16:02 10-22): too long = only the run that won, start to end, without the deaths
		if (L.hc && rpTooBig(segs)) segs = rpWinPath(all);
		if (segs.Count == 0 || rpTooBig(segs)) return;
		rpq[L.id + "_" + m] = new RpqItem { lvl = L.id, m = m, r = r, v = RP_V, hz = RP_HZ, segs = segs, sk = classicUi ? "" : skin }; saveRpq();
	}
	static bool rpTooBig(List<RpqSeg> segs) => segs.Count > RP_MAX_SEGS || segs.Sum(s => s.d.Length + 80) > RP_MAX_BYTES;
	public static List<RpqSeg> rpSegs(List<RecSeg> all) => all.Where(s => s.n > 1).Select(s => new RpqSeg { cp = s.cp, seed = s.seed, t0 = s.t0 is double t && t != 0 && !double.IsNaN(t) ? t : 0, n = s.n, d = packRp(s.b) }).ToList();
	// the attempts that made the win: the last one, before it the last one that started from an earlier save (it reached the save the
	// next one starts from), and so on back to the start. Each is cut where the next one starts (2 beats before its save), before its death.
	public static List<RpqSeg> rpWinPath(List<RecSeg> all)
	{
		var ss = all.Where(s => s.n > 1).ToList(); if (ss.Count == 0) return new();
		var path = new List<RecSeg> { ss[^1] };
		for (int i = ss.Count - 2; i >= 0 && path[0].cp > 0; i--) if (ss[i].cp < path[0].cp) path.Insert(0, ss[i]);
		var o = new List<RpqSeg>();
		for (int k = 0; k < path.Count; k++)
		{
			var s = path[k]; double t0 = s.t0 is double t && t != 0 && !double.IsNaN(t) ? t : 0; int n = s.n;
			if (k < path.Count - 1) n = (int)clamp(Math.Floor((B(path[k + 1].cp - 2) - t0) * RP_HZ + 1e-9) + 1, 2, s.n);
			o.Add(new RpqSeg { cp = s.cp, seed = s.seed, t0 = t0, n = n, d = packRp(s.b.Take(n * 3).ToList()), c = k < path.Count - 1 ? 1 : 0 });
		}
		return o;
	}
	static JsonObject IV(double v) => new() { ["integerValue"] = str(v) };
	// the square's look goes with the replay (only when it is not the original square)
	static JsonObject rpFields(RpqItem q) { var f = rpFields0(q); if (!string.IsNullOrEmpty(q.sk)) f["sk"] = new JsonObject { ["stringValue"] = q.sk }; return f; }
	static JsonObject rpFields0(RpqItem q) => new()
	{
		["uid"] = new JsonObject { ["stringValue"] = acc.uid }, ["lvl"] = new JsonObject { ["stringValue"] = q.lvl }, ["m"] = new JsonObject { ["stringValue"] = q.m }, ["r"] = new JsonObject { ["stringValue"] = q.r },
		["v"] = IV(q.v != 0 ? q.v : 1), ["hz"] = IV(q.hz),
		["segs"] = new JsonObject { ["arrayValue"] = new JsonObject { ["values"] = new JsonArray(q.segs.Select(s => (JsonNode)new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = segField(s) } }).ToArray()) } },
	};
	static JsonObject segField(RpqSeg s)
	{
		var f = new JsonObject { ["cp"] = IV(s.cp), ["seed"] = IV(s.seed), ["t0"] = new JsonObject { ["doubleValue"] = s.t0 }, ["n"] = IV(s.n), ["d"] = new JsonObject { ["stringValue"] = s.d } };
		if (s.c != 0) f["c"] = IV(s.c); return f;
	}
	static async Task flushRpq()
	{
		foreach (var k in rpq.Keys.ToList())
		{
			if (!rpq.TryGetValue(k, out var q)) continue;
			var L = ALL_LV().FirstOrDefault(x => x.id == q.lvl); string best = L == null ? null : q.m == "e" ? L.bestE : L.best;
			if (best != q.r) { if (best == null || Array.IndexOf(RANKS, q.r) > Array.IndexOf(RANKS, best)) { rpq.Remove(k); saveRpq(); } continue; }
			try { await commit(new JsonObject { ["update"] = new JsonObject { ["name"] = $"{FS_DOC}/replays/{acc.uid}_{q.lvl}_{q.m}", ["fields"] = rpFields(q) }, ["updateTransforms"] = new JsonArray(new JsonObject { ["fieldPath"] = "at", ["setToServerValue"] = "REQUEST_TIME" }) }); rpq.Remove(k); saveRpq(); }
			catch (Exception e) { if (e.Message == "NET") throw; rpq.Remove(k); saveRpq(); }
		}
	}

	// ---------- the profile's replay marks, and fetching a replay ----------
	public class ProfState { public PlayerDoc p; public int row, col; public Dictionary<string, string> rp = new(); public bool busy, err; public string msg = ""; public double msgT, sc; }
	public static readonly ProfState prof = new();
	const double PROF_RH = 36; const int PROF_VIS = 11;
	static double profScT() => clamp(Math.Min(prof.row - 1, Math.Max(prof.sc, prof.row - PROF_VIS + 2)), 0, Math.Max(0, ALL_LV().Count - PROF_VIS));
	static void openProfile(PlayerDoc p) { prof.p = p; prof.row = 0; prof.sc = 0; prof.col = board.mode == "e" ? 1 : 0; prof.rp = new(); prof.err = false; prof.msgT = 0; state = "profile"; _ = loadProfRp(p.uid); }
	static async Task loadProfRp(string uid)
	{
		prof.busy = true;
		try
		{
			var q = new JsonObject
			{
				["structuredQuery"] = new JsonObject
				{
					["from"] = new JsonArray(new JsonObject { ["collectionId"] = "replays" }),
					["where"] = new JsonObject { ["fieldFilter"] = new JsonObject { ["field"] = new JsonObject { ["fieldPath"] = "uid" }, ["op"] = "EQUAL", ["value"] = new JsonObject { ["stringValue"] = uid } } },
					["select"] = new JsonObject { ["fields"] = new JsonArray(new JsonObject { ["fieldPath"] = "lvl" }, new JsonObject { ["fieldPath"] = "m" }, new JsonObject { ["fieldPath"] = "r" }) },
				}
			};
			var j = await jpost($"{FS_URL}:runQuery", q);
			if (prof.p != null && prof.p.uid == uid)
			{
				var o = new Dictionary<string, string>();
				if (j is JsonArray a) foreach (var x in a) if (x?["document"]?["fields"] is JsonObject f) o[S(f["lvl"]["stringValue"]) + S(f["m"]["stringValue"])] = S(f["r"]["stringValue"]);
				prof.rp = o; prof.err = false;
			}
		}
		catch (Exception) { if (prof.p != null && prof.p.uid == uid) prof.err = true; }
		finally { prof.busy = false; }
	}
	static bool hasRp(PlayerDoc p, Level L, string m) => gv(p.F(m), L.id) != null && prof.rp.TryGetValue(L.id + m, out var r) && r == gv(p.F(m), L.id);
	// the replay player: same level, same song, same attacks, and the square follows the recorded moves
	static void openReplay(PlayerDoc p, Level L, string m)
	{
		rp.p = new RpPerson { name = p.name, uid = p.uid }; rp.L = L; rp.m = m; rp.doc = null; rp.err = false; state = "rpLoad";
		fetchRp(p.uid, L, m);
	}
	static async void fetchRp(string uid, Level L, string m)
	{
		try { var d = await jfetch($"{FS_URL}/replays/{uid}_{L.id}_{m}"); if (state == "rpLoad" && rp.L == L && rp.m == m) rp.doc = parseRp(d); }
		catch (Exception) { if (state == "rpLoad") rp.err = true; }
	}
	static RpDoc parseRp(JsonNode d)
	{
		var f = d["fields"]; double v = num(S(f["v"]?["integerValue"])); double hz = num(S(f["hz"]["integerValue"]));
		return new RpDoc
		{
			r = S(f["r"]["stringValue"]), sk = Array.IndexOf(SKINS, S(f["sk"]?["stringValue"])) > 0 ? S(f["sk"]["stringValue"]) : "", v = v != 0 && !double.IsNaN(v) ? (int)v : 1, hz = hz != 0 && !double.IsNaN(hz) ? (int)hz : RP_HZ,
			segs = (f["segs"]["arrayValue"]["values"] as JsonArray).Select(x =>
			{
				var s = x["mapValue"]["fields"];
				return new RpSeg { cp = num(S(s["cp"]["integerValue"])), seed = i32(num(S(s["seed"]["integerValue"]))), t0 = s["t0"]["doubleValue"] != null ? (double)s["t0"]["doubleValue"] : num(S(s["t0"]["integerValue"])), n = (int)num(S(s["n"]["integerValue"])), b = unpackRp(S(s["d"]["stringValue"])), c = S(s["c"]?["integerValue"]) == "1" };
			}).ToList(),
		};
	}
}
