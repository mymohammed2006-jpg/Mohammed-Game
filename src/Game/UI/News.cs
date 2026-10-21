using System;
using System.Collections.Generic;
using System.Linq;
using static MG.JS;

namespace MG;

// original index.html lines 6890-6950: "وش الجديد" (what's new). Shown once per update, after the intro and before the menu.
// Each item: title, a button shown next to the title (or null), the points, [from, to): the box shows to a player whose last seen update is from <= it < to
// (a player who saw no update = 0).
public static partial class G
{
	public record NewsItem(string hd, string ic, string[] pts, int from, int to);
	public static string NEWS_V => classicUi ? "37" : "44";   // the comparison tools stay on v39's page
	public static readonly NewsItem[] NEWS_ITEMS =
	{
		new("مرحلة جديدة", null, new[] { "المرحلة ١٥: Abra\u00a0Macabre.", "أصعب مرحلة في اللعبة.", "صارت في البروفايل والترتيب." }, 0, 44),
		new("إصلاح", null, new[] { "إعادة من آخر حفظ صارت تنحسب موتة.", "إعادة من البداية تصفّر كل شي." }, 0, 44),
		new("مراحل جديدة", null, new[] { "المرحلة ١٢: Hard\u00a0Spike.", "المرحلة ١٣: A\u00a0beat\u00a0further.", "المرحلة ١٤: Jump\u00a0again.", "صاروا في البروفايل والترتيب.", "تحدي الدائرة باقي على أول ١١ مرحلة." }, 0, 42),
		new("تعاوني", null, new[] { "صلّحنا مشاكل كثيرة في مرحلة الـ ٣ لاعبين." }, 0, 42),
		new("البلاتفورم", null, new[] { "طور جديد: من المراحل الرئيسية اختار البلاتفورم.", "المرحلة الأولى: سولو، أو ضد صاحبك أونلاين." }, 0, 41),
		new("تعاوني", null, new[] { "مرحلة جديدة لـ ٣ لاعبين أونلاين.", "سوّ غرفة وعط الكود لربعك." }, 0, 41),
		new("الحركة", null, new[] { "حركة المربع صارت نفسها على أي عدد فريمات." }, 0, 41),
		new("ملء الشاشة", null, new[] { "ملء الشاشة الحقيقي.", "من الإعدادات: نافذة، أو بدون حواف، أو ملء الشاشة الكامل." }, 0, 40),
		new("المستوى و XP", null, new[] { "XP من أفضل حرف لك في كل مرحلة.", "المراحل الأصعب تعطي أكثر.", "مستواك تحت حسابك وفي البروفايل.", "أول صفحة في الترتيب صارت للمستوى." }, 0, 40),
		new("التحديات", null, new[] { "٤ تحديات، كل واحد يعطيك XP وشكل جديد للمربع." }, 0, 40),
		new("الأشكال", null, new[] { "اختار شكل المربع من قائمة الأشكال.", "يطلع معك في المراحل." }, 0, 40),
		new("الماوس", null, new[] { "تتحكم في القوائم بالماوس.", "يختفي داخل المرحلة ولما تستخدم الكنترولر." }, 0, 40),
		new("الإعدادات", null, new[] { "الفريمات أي رقم من ٣٠ إلى ٥٠٠.", "الصوت أي رقم من ٠ إلى ١٠٠." }, 0, 40),
		new("مرحلة جديدة", null, new[] { "Link أضاف مرحلة في مراحل الشباب: On\u00a0The\u00a0Floor.", "موثّقة من Nightmare.", "صارت في البروفايل والترتيب." }, 0, 37),
		new("مراحل جديدة", null, new[] { "المرحلة الثامنة: Hips\u00a0Don't\u00a0Lie (Shakira).", "المرحلة التاسعة: Right\u00a0Now (Akon).", "المرحلة العاشرة: Je\u00a0veux (ZAZ).", "المرحلة الحادية عشرة: I\u00a0Will\u00a0Survive (Demi\u00a0Lovato).", "صاروا في البروفايل والترتيب." }, 0, 37),
		new("البروفايل", null, new[] { "قائمة المراحل صارت تنزل وتطلع مع اختيارك." }, 0, 37),
		new("إصلاح", null, new[] { "سكّرنا ثغرات الوقوف بدون حركة (AFK) في المراحل القديمة." }, 0, 37),
		new("مرحلة جديدة", null, new[] { "AJF أضاف مرحلة في مراحل الشباب: Let Me Love You.", "موثّقة من AJF.", "صارت في البروفايل والترتيب." }, 0, 30),
		new("لما تخسر", "restart", new[] { "زر جديد يرجعك لبداية المرحلة على طول.", "اسم الأغنية يطلع بس أول ما تدخل المرحلة." }, 0, 29),
		new("V-Sync", null, new[] { "صار مربع مثل ملء الشاشة.", "لما تطفيه ما يصير تقطيع.", "المرحلة السابعة صارت أخف." }, 0, 28),
		new("التدريب", "sq", new[] { "تختار الحرف اللي تتدرب عليه: S أو A أو B أو C.", "الأرواح تتعدل مع الحرف، وتقدر تغيرها من 1 لين 10." }, 0, 28),
		new("بوت جديد", "restart", new[] { "البوت صار يلعب بحدود الإنسان.", "ما يضغط أكثر من 5 مرات بالثانية.", "يجيب S في كل المراحل." }, 0, 28),
		new("مراحل جديدة", null, new[] { "المرحلة السادسة: H3ЯД.", "المرحلة السابعة: Clownparty.", "البروفايل والترتيب صار فيهم المرحلتين." }, 0, 26),
		new("إصلاح", null, new[] { "لما تموت وترجع من نقطة الحفظ، الخلفية والأشكال الكبيرة ترجع معك." }, 0, 27),
		new("مرحلة جديدة", null, new[] { "A3D أضاف مرحلة في مراحل الشباب: طنش (هبة مسعود).", "موثّقة من Mohammed.", "البروفايل والترتيب صار فيهم طنش." }, 0, 23),
		new("شوف البوت", "restart", new[] { "في كرت أي مرحلة: البوت يلعبها ويجيب S.", "بوت للنورمال وبوت للإيزي.", "تقدر توقفه وتقدمه وترجعه." }, 0, 23),
		new("التدريب", "sq", new[] { "تبدأ من أي ثانية في المرحلة.", "السرعة: ١٠٠٪ أو ٧٥٪ أو ٥٠٪.", "الأرواح: ما تخلص أو عادية.", "شبح البوت يوريك طريقه.", "ما ينحسب له تقييم ولا نقاط." }, 0, 23),
		new("Replay مثل الفيديو", null, new[] { "إيقاف وتشغيل.", "تقديم وترجيع ٥ ثواني، أو ثانية وحدة.", "السرعة من 0.25x لين 2x.", "تقدر تخفي الكلام اللي فوق.", "لما يخلص: تعيده من البداية أو ترجع." }, 0, 23),
		new("الحركة والقوائم", null, new[] { "المربع يتمطط مع اتجاه حركته.", "الاندفاع: تمطط ووميض أبيض سريع.", "القوائم تطلع وتختفي بتلاشي.", "البطاقة المختارة ترتفع بنعومة.", "أغنية المرحلة تشتغل وأنت على كرتها." }, 0, 23),
		new("الفوز والخسارة", null, new[] { "حرف التقييم يطيح مع الضربة.", "النقاط تعدّ من صفر.", "حرف النورمال أحمر وحرف الإيزي أزرق.", "S النورمال يلمع.", "شاشة الخسارة توريك كم بالمية وصلت." }, 0, 23),
		new("طنش", null, new[] { "صارت موثّقة من Mohammed." }, 23, 24),
		new("إصلاح", null, new[] { "رجع الصندوق اللي يحبسك في MEGALOVANIA ووعد مني.", "حركة المربع الجديدة صارت تطلع في Replay والبوت." }, 23, 24),
	};
	static double newsAt = 0, newsPgAt = -1e9, newsDir = 1; static List<NewsItem> newsShow = new(); static int newsPg = 0; const int NEWS_PER = 6;
	public static double mainAt = -1e9;
	static int newsPages() => Math.Max(1, (int)Math.Ceiling((double)newsShow.Count / NEWS_PER));
	static double newsSeen() { double v = parseFloat(localStorage.getItem("mg_news")); return v != 0 && !double.IsNaN(v) ? v : 0; }
	static List<NewsItem> newsList() { double sn = newsSeen(); return NEWS_ITEMS.Where(it => sn >= it.from && sn < it.to && (!classicUi || it.to <= 37)).ToList(); }
	static bool newsDue() => newsSeen() < num(NEWS_V) && newsList().Count > 0;
	static bool newsNew()
	{
		if (localStorage.getItem("mg_news") != null || !string.IsNullOrEmpty(localStorage.getItem("mg_acc"))) return false;
		foreach (var k in localStorage.keys()) if (k.StartsWith("mg_best", StringComparison.Ordinal)) return false;
		return true;
	}
	public static void goMain(double now)
	{
		if (newsDue() && newsNew()) localStorage.setItem("mg_news", NEWS_V);
		if (newsDue()) { newsShow = newsList(); newsPg = 0; newsPgAt = -1e9; state = "news"; newsAt = now; } else { state = "main"; mainAt = now; menuSel = 0; }
	}
	static void newsDone(double now) { localStorage.setItem("mg_news", NEWS_V); state = "main"; mainAt = now; menuSel = 0; }
	static List<string> wrapAr(string t, double maxW, double size, int weight = 500)
	{
		g.font = $"{weight} {str(size)}px {FONT}"; var o = new List<string>(); string ln = "";
		foreach (var w in t.Split(' ')) { string c = ln != "" ? ln + " " + w : w; if (g.measureText(c).width > maxW && ln != "") { o.Add(ln); ln = w; } else ln = c; }
		if (ln != "") o.Add(ln); return o;
	}
	static string[] btnOf(BtnSet K, string n) => n switch { "move" => K.move, "confirm" => K.confirm, "dash" => K.dash, "pause" => K.pause, "back" => K.back, "restart" => K.restart, "sq" => K.sq, _ => K.small };
	static void drawNews(double now)
	{
		bgShapes(now); double el = (now - newsAt) / 1000; var K = BTN[kind];
		double ha = clamp(el / 0.35, 0, 1); g.globalAlpha = ha;
		txt("وش الجديد؟", W / 2, 62 - 10 * (1 - ee2(ha)), 46, "#fff");
		string tag = "التحديث " + ar(num(NEWS_V)); g.font = $"500 19px {FONT}"; double tw = g.measureText(tag).width + 30;
		rr(W / 2 - tw / 2, 100, tw, 30, 15); g.fillStyle = $"rgba({RGB},0.16)"; g.fill(); g.strokeStyle = $"rgba({RGB},0.6)"; g.lineWidth = 1.5; g.stroke();
		txt(tag, W / 2, 116, 19, "rgba(255,255,255,0.85)", 500); g.globalAlpha = 1;
		int np = newsPages(); var L = newsShow.Skip(newsPg * NEWS_PER).Take(NEWS_PER).ToList(); double pe = (now - newsPgAt) / 1000; int n = L.Count; double cw = 380, gx = 16, gy = 16; int cols = Math.Min(3, n), rows = (int)Math.Ceiling((double)n / cols); double x0 = W / 2 + (cols * cw + (cols - 1) * gx) / 2;
		double need = L.Max(it => (double)it.pts.Sum(t => wrapAr(t, cw - 60, 18).Count)), ch = Math.Max(130, Math.Min(236, 72 + need * 29 + 6)), y0 = 162 + (2 * 236 + gy - (rows * ch + (rows - 1) * gy)) / 2;
		for (int i = 0; i < L.Count; i++)
		{
			var (hd, ic, pts) = (L[i].hd, L[i].ic, L[i].pts);
			double k = Math.Min(clamp((el - 0.25 - i * 0.09) / 0.4, 0, 1), clamp((pe - i * 0.04) / 0.3, 0, 1)); bool pk = pe < 1; if (k <= 0) continue; double e = ee2(k);
			int c = i % cols, r = i / cols; double x = x0 - c * (cw + gx) - cw - (pk ? 60 * newsDir * (1 - e) : 0), y = y0 + r * (ch + gy) + (pk ? 0 : 24 * (1 - e));
			g.globalAlpha = e; rr(x, y, cw, ch, 18); g.fillStyle = "rgba(16,12,15,0.94)"; g.fill(); g.strokeStyle = $"rgba({RGB},{str(0.35 + 0.15 * pulse)})"; g.lineWidth = 1.5; g.stroke();
			g.save(); g.translate(x + cw - 28, y + 32); g.rotate(now / 700 + i); g.fillStyle = RED; g.shadowColor = RED; g.shadowBlur = 8; g.fillRect(-6, -6, 12, 12); g.restore();
			txt(hd, x + cw - 46, y + 33, 25, "#fff", 800, "right");
			if (ic != null) { g.font = $"800 25px {FONT}"; double hw = g.measureText(hd).width; string nn = btnOf(K, ic)[0]; double s = 28, w = iconW(nn, s); icon(nn, x + cw - 46 - hw - 12 - w / 2, y + 33, s); }
			double ly = y + 72;
			foreach (var t in pts)
			{
				var ls = wrapAr(t, cw - 60, 18); g.fillStyle = $"rgba({RGB},0.9)"; g.beginPath(); g.arc(x + cw - 30, ly, 3.2, 0, TAU); g.fill();
				foreach (var l in ls) { txt(l, x + cw - 42, ly, 18, "rgba(233,230,245,0.85)", 500, "right"); ly += 29; }
			}
			g.globalAlpha = 1;
		}
		if (np > 1)
		{
			double cy = y0 + (rows * ch + (rows - 1) * gy) / 2; g.globalAlpha = clamp((el - 0.6) / 0.4, 0, 1); g.fillStyle = "rgba(255,255,255,0.5)";
			if (newsPg > 0) { g.beginPath(); g.moveTo(W - 30, cy); g.lineTo(W - 46, cy - 14); g.lineTo(W - 46, cy + 14); g.closePath(); g.fill(); }
			if (newsPg < np - 1) { g.beginPath(); g.moveTo(30, cy); g.lineTo(46, cy - 14); g.lineTo(46, cy + 14); g.closePath(); g.fill(); }
			for (int i = 0; i < np; i++)
			{
				double dx = W / 2 + (np - 1) * 11 - i * 22, dy = H - 57; g.beginPath(); g.arc(dx, dy, 5, 0, TAU);
				if (i == newsPg) { g.fillStyle = RED; g.shadowColor = RED; g.shadowBlur = 8; g.fill(); g.shadowBlur = 0; } else { g.strokeStyle = "rgba(255,255,255,0.45)"; g.lineWidth = 1.5; g.stroke(); }
			}
			g.globalAlpha = 1;
		}
		g.globalAlpha = clamp((el - 0.9) / 0.4, 0, 1);
		row(newsPg < np - 1 ? new[] { (K.confirm, "التالي"), (K.move, "قلّب") } : np > 1 ? new[] { (K.confirm, "يالله نلعب"), (K.move, "قلّب") } : new[] { (K.confirm, "يالله نلعب") }, W / 2, np > 1 ? H - 26 : H - 36, 30, "#fff", 24); g.globalAlpha = 1;
	}
	static void stepNews(Inp inp, double now)
	{
		int np = newsPages(); void pg(int d) { newsPg += d; newsPgAt = now; newsDir = d; }
		if (inp.left && newsPg < np - 1) pg(1); else if (inp.right && newsPg > 0) pg(-1);
		else if (now - newsAt > 700 && inp.confirm && newsPg < np - 1) pg(1); else if (now - newsAt > 700 && (inp.confirm || inp.back || inp.pz)) newsDone(now);
	}
}
