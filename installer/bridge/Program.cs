using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace MGBridge;

// Mohammed-Game-Update.exe, the bridge for v40-v42.
// Their updater gives the whole download 30 s, so a big installer never finished on a normal line (it stopped at ~62%
// and the game opened on the old version). latest.yml therefore points them (path/sha512) at this small file, which
// they download in a second and run like the installer ("/S --updated --force-run"). It reads latest.yml again,
// downloads the real installer (fullPath/fullSha512) with no time limit, resuming after a cut, checks its sha512 and
// runs it with the same arguments. A small window shows the progress the way the game's update screen does.
// If it can't finish it opens the game (when asked to with --force-run); the game looks for the update again next start.
static class Program
{
	// MG_UPDATE_TEST_URL: the same test switch as the game's updater
	public static readonly string REPO = Environment.GetEnvironmentVariable("MG_UPDATE_TEST_URL") is string t && t != "" ? t.TrimEnd('/') + "/"
		: "https://github.com/mymohammed2006-jpg/Mohammed-Game/releases/latest/download/";

	[STAThread]
	static void Main(string[] args)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new Win(args));
	}
}

class Win : Form
{
	const int STALL_MS = 30000, TRIES = 6;
	readonly string[] args;
	readonly bool forceRun;
	readonly float s;   // screen scale (DPI)
	readonly FontFamily audiowide;
	readonly PrivateFontCollection fonts = new();
	volatile string msg = "msg_update.png";   // the message, drawn beforehand by make_text.py
	volatile bool done, cancelled;
	double percent, shown;
	readonly System.Windows.Forms.Timer frame = new() { Interval = 16 };

	public Win(string[] args)
	{
		this.args = args;
		forceRun = args.Contains("--force-run");
		audiowide = LoadFont("audiowide.ttf");
		using (var g = CreateGraphics()) s = g.DpiX / 96f;
		Text = "Mohammed Game";
		try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
		FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
		StartPosition = FormStartPosition.CenterScreen;
		AutoScaleMode = AutoScaleMode.None;
		ClientSize = new Size((int)(560 * s), (int)(300 * s));
		BackColor = Color.Black;
		DoubleBuffered = true;
		frame.Tick += (_, _) => { shown += (percent - shown) * 0.25; if (Math.Abs(percent - shown) < 0.05) shown = percent; Invalidate(); };
		frame.Start();
		Shown += (_, _) => new Thread(Work) { IsBackground = true }.Start();
		FormClosing += (_, e) => { if (!done) { cancelled = true; OpenGame(); } };
	}

	FontFamily LoadFont(string name)
	{
		using var st = typeof(Win).Assembly.GetManifestResourceStream(name);
		var data = new byte[st.Length]; st.Read(data, 0, data.Length);
		IntPtr p = Marshal.AllocCoTaskMem(data.Length);   // kept for the whole run, GDI+ reads it while drawing
		Marshal.Copy(data, 0, p, data.Length);
		fonts.AddMemoryFont(p, data.Length);
		return fonts.Families[fonts.Families.Length - 1];
	}

	static string Field(string yml, string key)
	{
		foreach (var line in yml.Split('\n'))
		{
			var l = line.TrimEnd('\r');
			if (l.StartsWith(key + ":")) return l.Substring(key.Length + 1).Trim().Trim('\'', '"');
		}
		return null;
	}

	static HttpWebRequest Req(string url)
	{
		var r = (HttpWebRequest)WebRequest.Create(url);
		r.AllowAutoRedirect = true; r.Timeout = STALL_MS; r.ReadWriteTimeout = STALL_MS;
		r.UserAgent = "Mohammed-Game-Update";
		return r;
	}

	void Work()
	{
		try
		{
			string yml = null;
			for (int i = 0; yml == null; i++)
			{
				try { using var resp = Req(Program.REPO + "latest.yml").GetResponse(); using var rd = new StreamReader(resp.GetResponseStream()); yml = rd.ReadToEnd(); }
				catch (Exception) when (i < TRIES - 1 && !cancelled) { Thread.Sleep(2000); }
			}
			string path = Field(yml, "fullPath"), sha = Field(yml, "fullSha512");
			if (path == null || sha == null) throw new Exception("no fullPath in latest.yml");
			string dir = Path.Combine(Path.GetTempPath(), "mohammed-game-update");
			Directory.CreateDirectory(dir);
			string file = Path.Combine(dir, "Mohammed-Game-Setup.exe");
			Download(Program.REPO + path, file);
			if (Sha(file) != sha) { Download(Program.REPO + path, file); if (Sha(file) != sha) throw new Exception("sha512 mismatch"); }
			if (cancelled) return;
			percent = 100; msg = "msg_done.png";
			Thread.Sleep(1200);
			if (cancelled) return;
			Process.Start(new ProcessStartInfo(file, string.Join(" ", args.Length > 0 ? args : new[] { "/S", "--updated", "--force-run" })) { UseShellExecute = false });
			done = true;
		}
		catch (Exception)
		{
			if (cancelled) return;
			msg = "msg_fail.png";
			Thread.Sleep(3500);
			OpenGame(); done = true;
		}
		try { BeginInvoke(new Action(Close)); } catch (Exception) { }
	}

	// downloads from the start, then after a cut carries on from where it stopped (Range), TRIES times at most
	void Download(string url, string file)
	{
		long have = 0, total = -1; percent = 0;
		File.Delete(file);
		for (int i = 0; ; i++)
		{
			try
			{
				var r = Req(url);
				if (have > 0) r.AddRange(have);
				using var resp = (HttpWebResponse)r.GetResponse();
				if (have > 0 && resp.StatusCode != HttpStatusCode.PartialContent) have = 0;   // the server sent the whole file again
				if (resp.ContentLength > 0) total = have + resp.ContentLength;
				using var src = resp.GetResponseStream();
				using var fs = new FileStream(file, have > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write);
				var buf = new byte[1 << 16];
				int n;
				while ((n = src.Read(buf, 0, buf.Length)) > 0)
				{
					if (cancelled) throw new OperationCanceledException();
					fs.Write(buf, 0, n); have += n;
					if (total > 0) percent = Math.Min(100.0, 100.0 * have / total);
				}
				if (total > 0 && have < total) throw new IOException("cut");
				return;
			}
			catch (Exception) when (i < TRIES - 1 && !cancelled) { Thread.Sleep(2000); }
		}
	}

	static string Sha(string file)
	{
		using var f = File.OpenRead(file);
		using var h = SHA512.Create();
		return Convert.ToBase64String(h.ComputeHash(f));
	}

	void OpenGame()
	{
		if (!forceRun) return;
		try
		{
			string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "mohammed-game", "Mohammed Game.exe");
			if (File.Exists(exe)) Process.Start(new ProcessStartInfo(exe, "--no-update") { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false });
		}
		catch (Exception) { }
	}

	// the game's update screen, smaller: red title with a glow, the message, the bar filling from the right, the percent
	protected override void OnPaint(PaintEventArgs e)
	{
		var g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		g.ScaleTransform(s, s);
		var red = Color.FromArgb(255, 36, 54);
		var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		using (var title = new GraphicsPath())
		{
			title.AddString("Mohammed Game", audiowide, 0, 40, new RectangleF(0, 55, 560, 70), center);
			for (int i = 0; i < 3; i++)   // the glow: wide faint outlines under the letters
				using (var p = new Pen(Color.FromArgb(new[] { 22, 34, 60 }[i], red), new[] { 14f, 9f, 5f }[i]) { LineJoin = LineJoin.Round }) g.DrawPath(p, title);
			using var b = new SolidBrush(red); g.FillPath(b, title);
		}
		var img = Msg(msg);
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.DrawImage(img, 280 - img.Width / 4f, 160 - img.Height / 4f, img.Width / 2f, img.Height / 2f);
		const float BX = 130, BY = 192, BW = 300, BH = 16;
		using (var box = Round(BX, BY, BW, BH, 7)) using (var p = new Pen(red, 1.2f)) g.DrawPath(p, box);
		float w = (float)((BW - 6) * shown / 100);
		if (w > 0.5f) using (var bar = Round(BX + BW - 3 - w, BY + 3, w, BH - 6, Math.Min(4, w / 2))) using (var b = new SolidBrush(red)) g.FillPath(b, bar);
		using (var f = new Font(audiowide, 15, GraphicsUnit.Pixel)) using (var b = new SolidBrush(Color.FromArgb(255, 107, 107)))
			g.DrawString((int)Math.Round(shown) + "%", f, b, new RectangleF(0, 216, 560, 30), center);
	}

	readonly System.Collections.Generic.Dictionary<string, Image> msgs = new();
	Image Msg(string name)
	{
		if (!msgs.TryGetValue(name, out var img)) msgs[name] = img = Image.FromStream(typeof(Win).Assembly.GetManifestResourceStream(name));
		return img;
	}

	static GraphicsPath Round(float x, float y, float w, float h, float r)
	{
		var p = new GraphicsPath(); float d = r * 2;
		p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
		p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90);
		p.CloseFigure(); return p;
	}
}
