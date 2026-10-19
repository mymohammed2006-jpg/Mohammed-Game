using Godot;
using System;
using System.Globalization;
using System.Security.Cryptography;
using MG.Canvas;
using static MG.JS;

namespace MG;

// Auto-update, like the old PC app (pc-app/main.js + updater.html): on start it looks for a newer release on GitHub
// (latest.yml), downloads Mohammed-Game-Setup.exe with a progress bar, checks its sha512, and restarts into it.
// No skip. If nothing answers within 7 s the game opens; an update found later installs when the game closes.
public partial class Updater : Node
{
	// MG_UPDATE_TEST_URL: look for the update on a local server instead (tests), like the old app
	static readonly string REPO = System.Environment.GetEnvironmentVariable("MG_UPDATE_TEST_URL") is string t && t != "" ? t.TrimEnd('/') + "/"
		: "https://github.com/mymohammed2006-jpg/Mohammed-Game/releases/latest/download/";
	public static bool Testing => System.Environment.GetEnvironmentVariable("MG_UPDATE_TEST_URL") is string t && t != "";
	const double CHECK_WAIT_MS = 7000;
	public static string Version => (string)ProjectSettings.GetSetting("application/config/version", "0.0.0");

	public string state = "check";   // check | download | restart | done (the game runs)
	public double percent, shown;
	double startAt;
	bool found, installOnQuit, ready;
	string setupPath, sha512, newVersion;
	HttpRequest req;
	public Action onDone;            // opens the game

	public void Start()
	{
		startAt = performance.now();
		setupPath = ProjectSettings.GlobalizePath("user://update/Mohammed-Game-Setup.exe");
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(setupPath));
		req = new HttpRequest { UseThreads = true, MaxRedirects = 10, Timeout = 30 };
		AddChild(req);
		req.RequestCompleted += OnYml;
		if (req.Request(REPO + "latest.yml") != Error.Ok) Finish();
	}

	void Finish() { if (state is "check" or "download") { state = "done"; onDone?.Invoke(); } }

	static string Field(string yml, string key)
	{
		foreach (var line in yml.Split('\n'))
		{
			var l = line.TrimEnd('\r');
			if (l.StartsWith(key + ":")) return l[(key.Length + 1)..].Trim().Trim('\'', '"');
		}
		return null;
	}
	static int[] Ver(string v)
	{
		var p = (v ?? "0").Split('.'); var r = new int[3];
		for (int i = 0; i < 3 && i < p.Length; i++) int.TryParse(p[i].Split('-')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out r[i]);
		return r;
	}
	public static bool Newer(string a, string b) { var x = Ver(a); var y = Ver(b); for (int i = 0; i < 3; i++) if (x[i] != y[i]) return x[i] > y[i]; return false; }

	void OnYml(long result, long code, string[] headers, byte[] body)
	{
		req.RequestCompleted -= OnYml;
		if (result != (long)HttpRequest.Result.Success || code != 200) { Finish(); return; }
		string yml = System.Text.Encoding.UTF8.GetString(body);
		if (Testing) GD.PrintErr("MG_TEST yml ", code, " ", body.Length);
		newVersion = Field(yml, "version"); string path = Field(yml, "path"); sha512 = Field(yml, "sha512");
		if (newVersion == null || path == null || sha512 == null || !Newer(newVersion, Version) || !(OS.HasFeature("windows") || Testing)) { Finish(); return; }
		found = true;
		if (state == "check") state = "download"; else installOnQuit = true;   // found after the game opened: install on close
		req.DownloadFile = setupPath;
		req.RequestCompleted += OnSetup;
		if (req.Request(REPO + path) != Error.Ok) Finish();
	}

	void OnSetup(long result, long code, string[] headers, byte[] body)
	{
		if (Testing) GD.PrintErr("MG_TEST setup ", result, " ", code, " sha ", ShaOk(), " state ", state);
		if (result != (long)HttpRequest.Result.Success || code != 200 || !ShaOk()) { Finish(); return; }
		ready = true;
		if (state == "download")
		{
			percent = 100; state = "restart";
			GetTree().CreateTimer(1.2).Timeout += () => Install(true);
		}
	}

	bool ShaOk()
	{
		try
		{
			using var f = System.IO.File.OpenRead(setupPath);
			return Convert.ToBase64String(SHA512.HashData(f)) == sha512;
		}
		catch (Exception) { return false; }
	}

	// the installer (NSIS) closes nothing itself: the game quits right after starting it
	void Install(bool run)
	{
		if (!OS.HasFeature("windows")) { GD.Print("MG_TEST would install ", setupPath); state = "done"; onDone?.Invoke(); return; }
		var args = run ? new[] { "/S", "--updated", "--force-run" } : new[] { "/S", "--updated" };
		OS.CreateProcess(setupPath, args);
		localStorage.Flush(true);
		GetTree().Quit();
	}

	// called when the game closes (window X, the exit menu)
	public bool InstallOnQuit()
	{
		if (!installOnQuit || !ready) return false;
		OS.CreateProcess(setupPath, new[] { "/S", "--updated" });
		return true;
	}

	public void Tick()
	{
		if (state == "check" && !found && performance.now() - startAt > CHECK_WAIT_MS) { installOnQuit = true; Finish(); }
		if (state == "download" && req != null)
		{
			long n = req.GetBodySize(), d = req.GetDownloadedBytes();
			if (n > 0) percent = Math.Clamp(100.0 * d / n, 0, 100);
		}
	}

	// the same screen as the old updater.html (1280x720 layout measured in Chrome)
	public void Draw(Ctx g, double now)
	{
		g.setTransform(1, 0, 0, 1, 0, 0); g.fillStyle = "#000"; g.fillRect(0, 0, g.width, g.height);
		g.setTransform(G.scale, 0, 0, G.scale, G.offX, G.offY);
		const string AW = "Audiowide,sans-serif", TJ = "Tajawal,sans-serif"; const double W0 = 1280, H0 = 720;
		// title with two glows
		g.font = $"79.36px {AW}"; g.textAlign = "center"; g.textBaseline = "middle"; g.direction = "ltr"; g.fillStyle = "#ff2436";
		g.shadowColor = "rgba(255,36,54,0.45)"; g.shadowBlur = 47.62; g.fillText("Mohammed Game", 640, 278.3);
		g.shadowColor = "rgba(255,36,54,0.8)"; g.shadowBlur = 19.84; g.fillText("Mohammed Game", 640, 278.3);
		g.shadowBlur = 0;
		// message (with the dots while waiting)
		string msg = state == "download" ? "جاري التحديث" : state == "restart" ? "خلص التحديث، اللعبة تفتح من جديد" : "يدور على تحديث";
		if (state != "download") msg += new string('.', (int)(now / 300) % 4);
		g.font = $"800 33.28px {TJ}"; g.fillStyle = "#fff"; g.direction = "rtl"; g.fillText(msg, 640, 389.92);
		if (state is "download" or "restart")
		{
			double p = state == "restart" ? 100 : percent; shown += (p - shown) * 0.25; if (Math.Abs(p - shown) < 0.05) shown = p;
			int n = (int)Math.Round(Math.Clamp(p, 0, 100));
			// box-shadow: the glow of the whole box, outside it only
			// Chrome snaps the box to whole pixels vertically and draws the .15em border 1 px thick
			const double BX = 406.19, BY = 431, BW = 467.62, BH = 22, BR = 10.24;
			g.save(); G.rr(BX, BY, BW, BH, BR); g.rect(0, 0, W0, H0); g.clip("evenodd");   // rr starts a new path, so the rect goes after it
			g.shadowColor = "rgba(255,36,54,0.4)"; g.shadowBlur = 7.68; g.fillStyle = "#ff2436"; G.rr(BX, BY, BW, BH, BR); g.fill(); g.restore();
			g.save(); G.rr(BX + 1, BY + 1, BW - 2, BH - 2, BR - 1); g.rect(0, 0, W0, H0); g.clip("evenodd");
			g.fillStyle = "#ff2436"; G.rr(BX, BY, BW, BH, BR); g.fill(); g.restore();
			double w = 460.8 * shown / 100;
			if (w > 0.5) { g.fillStyle = "#ff2436"; G.rr(870.41 - w, 434.94, w, 14.08, Math.Min(6.4, w / 2)); g.fill(); }
			g.font = $"23.04px {AW}"; g.fillStyle = "#ff6b6b"; g.direction = "ltr"; g.fillText(str(n) + "%", 640, 485.69);
		}
		g.font = $"17.92px {AW}"; g.fillStyle = "#555"; g.textAlign = "left"; g.direction = "ltr"; g.fillText("v" + Version.Split('.')[0], 43, 673.67);
	}
}
