using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static MG.JS;

namespace MG;

// New in v46 (his idea 19:07 10-22): the co-op level's replay, kept on the PC only. It is what this PC's screen showed, 30 times
// a second: the three squares, every thing on the page (clouds, scales, buttons, doors, ice, lightning, the camera's steps,
// the photo), the pieces, the falls and the pages. Watching draws those pictures again (nothing is played out again), with the
// song from its place. Saved from the pause menu (inside the room there's no list: watching would leave the others) and the
// win screen; the list is in the main menu (UI/MyRp.cs).
public static partial class G
{
	public const int TM_HZ = 30, TM_RP_MAX = TM_HZ * 60 * 40;   // 40 minutes at most
	public class TRec { public MemoryStream ms = new(); public BinaryWriter w; public int n; public double clock; public List<string> strs = new() { "" }; public int seat; public string[] names; public TRec() { w = new BinaryWriter(ms); } }
	public static TRec tmRec;

	// ---------- recording (after every fixed step, TeamNet.cs tnRun) ----------
	static void tmRecStart() { tmRec = new TRec { seat = tmMe, names = (string[])tm.names.Clone() }; }
	static void tmRecTick(double dt)
	{
		var r = tmRec; if (r == null || tm == null || r.n >= TM_RP_MAX) return;
		r.clock += dt;
		while (r.n / (double)TM_HZ <= r.clock + 1e-9 && r.n < TM_RP_MAX) { tmRecFrame(r); r.n++; }
	}
	static void tmRecFrame(TRec r)
	{
		var w = r.w; void F(double v) => w.Write((float)v);
		w.Write((byte)tm.page);
		w.Write((byte)((tm.won ? 1 : 0) | (tm.fromClouds ? 2 : 0)));
		F(tm.t); F(tm.pageAt); F(tm.doneAt); F(tm.restartAt); F(tm.wonAt); F(tm.reveal); F(tm.chaseX); F(tm.swapAt); F(tm.msgT); F(tm.playT); F(flash); F(shake);
		w.Write((sbyte)tm.swapA); w.Write((sbyte)tm.swapB); w.Write((sbyte)tm.snapAt); w.Write((byte)tm.coverTheme);
		string msg = tm.msg ?? ""; int mi = r.strs.IndexOf(msg); if (mi < 0) { mi = r.strs.Count; r.strs.Add(msg); } w.Write((short)mi);
		int so = 0; for (int i = 0; i < tm.stemOn.Length && i < 8; i++) if (tm.stemOn[i]) so |= 1 << i; w.Write((byte)so);
		w.Write((short)tm.restarts); w.Write((short)tm.hits);
		for (int i = 0; i < TM_N; i++) { w.Write((byte)Math.Min(255, tm.falls[i])); w.Write((byte)(tm.flips[i] & 255)); w.Write((short)tm.xpGot[i]); }
		var got = tm.got.ToList(); w.Write((byte)got.Count); foreach (var k in got) { var p = k.Split(','); w.Write((byte)int.Parse(p[0])); w.Write((byte)int.Parse(p[1])); }
		foreach (var b in tm.b)
		{
			F(b.x); F(b.y); F(b.sa); F(b.sk); F(b.fx); F(b.ddx); F(b.ddy); F(b.dash); F(b.hurt); F(b.revT); F(b.still); F(b.downAt);
			w.Write((byte)Math.Clamp(b.hp, 0, 255)); w.Write((byte)((b.down ? 1 : 0) | (b.gate ? 2 : 0) | (b.armed ? 4 : 0) | (b.carry != null ? 8 : 0) | (b.hang ? 16 : 0)));
		}
		int nb = tnBase(); w.Write((short)nb);
		for (int i = 0; i < nb; i++)
		{
			var s = tm.th[i]; F(s.X); F(s.Y); F(s.s); F(s.timer); F(s.off);
			w.Write((byte)((s.on ? 1 : 0) | (s.melt ? 2 : 0) | (s.gone ? 4 : 0) | (s.solid ? 8 : 0))); w.Write((byte)Math.Clamp(s.n, 0, 255));
		}
		var snaps = tm.th.Skip(nb).Where(s => s.k == "snap").ToList(); w.Write((byte)snaps.Count);
		foreach (var s in snaps) { w.Write((byte)(int)s.d.a); F(s.X); F(s.Y); F(s.timer); F(s.s); }
		w.Write((short)tm.hail.Count); foreach (var q in tm.hail) { F(q.x); F(q.y); }
		w.Write((short)tm.steps.Count); foreach (var q in tm.steps) { F(q.x); F(q.y); F(q.t); w.Write((byte)q.who); }
		if (tmRpDuo(tm.L))   // the radio: how garbled, the static, the cable, the shield and the speakers' waves
		{
			F(tm.murk); F(tm.riseY); w.Write((byte)((tm.ropeLive ? 1 : 0) | (tm.shield << 1)));
			w.Write((byte)Math.Min(255, tm.bars.Count));
			foreach (var q in tm.bars.Take(255)) { F(q.x); w.Write((sbyte)q.dir); w.Write((byte)q.r0); int m = 0; for (int k = 0; k < q.on.Length && k < 8; k++) if (q.on[k]) m |= 1 << k; w.Write((byte)m); w.Write((byte)q.on.Length); }
		}
	}
	// the radio level (its own colours, two players): its frames carry more, and only its bodies
	static bool tmRpDuo(TeamLevel L) => L?.cols != null;
	// saves it on the PC (res: "win" from the win screen, "mid" from the pause)
	static bool tmRpSave(string res)
	{
		var r = tmRec; var P = net?.L ?? PLATS.FirstOrDefault(x => x.id == tm?.L.id); if (r == null || tm == null || P == null) return false;
		if (r.n < 2) { showCorner("ما فيه شي ينحفظ للحين", "#ffb35c", 2400); return false; }
		r.w.Flush(); byte[] gz; using (var o = new MemoryStream()) { using (var z = new GZipStream(o, CompressionLevel.Optimal)) z.Write(r.ms.GetBuffer(), 0, (int)r.ms.Length); gz = o.ToArray(); }
		var seg = new RpqSeg { t0 = 0, n = r.n, d = Convert.ToBase64String(gz) };
		int me = Math.Max(0, r.seat);
		var it = new MyRp { kind = "team", hz = TM_HZ, r = res == "win" ? tmRank(me) : null, tmSeat = r.seat, tmNames = tm.names.ToArray(), tmSkins = tm.skins.ToArray(), tmStr = r.strs.ToList() };
		return myRpWrite(it, P.id, "n", res, new List<RpqSeg> { seg }, tm.hits, tm.falls[me]);
	}
	// the tests (test/TeamBot.cs RPT=1)
	public static void tmRecStartPub() => tmRecStart(); public static void tmRecTickPub(double dt) => tmRecTick(dt); public static bool tmRpSavePub(string res) => tmRpSave(res);
	public static void tmRpApplyPub(double tr) => tmRpApply(tr, 0, false);
	static void tmRpKey(Inp inp) { if (myRpCan() && tm != null && tm.won && inp.sq && !myRpDone(tm.wonAt) && tm.t - tm.wonAt > 1 && tmRpSave("win")) myRpSavedFor = tm.wonAt; }

	// ---------- watching ----------
	public class TFrame
	{
		public int page, fl, swapA, swapB, snapAt, cover, msg, stems, restarts, hits; public double[] g = new double[12];
		public int[] falls = new int[TM_N], flips = new int[TM_N], xp = new int[TM_N]; public List<(int x, int y)> got = new();
		public double[][] bd = new double[TM_N][]; public int[] hp = new int[TM_N], bfl = new int[TM_N];
		public double[] tx, ty, ts, tt, toff; public int[] tfl, tn;
		public List<(int who, double x, double y, double timer, double s)> snaps = new(); public List<(double x, double y)> hail = new(); public List<(double x, double y, double t, int who)> steps = new();
		public double murk, riseY = double.NaN; public int duo; public List<(double x, int dir, int r0, int on, int n)> bars = new();
	}
	public class TmRpData { public PlatLevel P; public TeamLevel L; public List<TFrame> fr = new(); public int seat; public string[] names, skins; public List<string> strs; public Voice[] voices; public bool[] von; public string mkey; public TFrame prev; public Voice stat; public double murkNow; public List<TFrame> frFull, frCut; public int[] cutOi; public double lastT = double.NaN, lastTr; }
	static TmRpData tmrp;
	static int tmRpSaveMe = -1, tmRpSaveView = -1;
	public static bool tmRpOn => rp.team && tmrp != null;

	static List<TFrame> tmRpParse(string b64, TeamLevel L)
	{
		int nb0 = L?.players ?? TM_N; bool duo = tmRpDuo(L);
		var o = new List<TFrame>(); byte[] raw;
		using (var i = new MemoryStream(Convert.FromBase64String(b64))) using (var z = new GZipStream(i, CompressionMode.Decompress)) using (var m = new MemoryStream()) { z.CopyTo(m); raw = m.ToArray(); }
		var r = new BinaryReader(new MemoryStream(raw)); double F() => r.ReadSingle();
		while (r.BaseStream.Position < raw.Length)
		{
			var f = new TFrame { page = r.ReadByte(), fl = r.ReadByte() };
			for (int k = 0; k < 12; k++) f.g[k] = F();
			f.swapA = r.ReadSByte(); f.swapB = r.ReadSByte(); f.snapAt = r.ReadSByte(); f.cover = r.ReadByte(); f.msg = r.ReadInt16(); f.stems = r.ReadByte(); f.restarts = r.ReadInt16(); f.hits = r.ReadInt16();
			for (int i = 0; i < TM_N; i++) { f.falls[i] = r.ReadByte(); f.flips[i] = r.ReadByte(); f.xp[i] = r.ReadInt16(); }
			int ng = r.ReadByte(); for (int i = 0; i < ng; i++) f.got.Add((r.ReadByte(), r.ReadByte()));
			for (int i = 0; i < nb0; i++) { var a = new double[12]; for (int k = 0; k < 12; k++) a[k] = F(); f.bd[i] = a; f.hp[i] = r.ReadByte(); f.bfl[i] = r.ReadByte(); }
			int nb = r.ReadInt16(); f.tx = new double[nb]; f.ty = new double[nb]; f.ts = new double[nb]; f.tt = new double[nb]; f.toff = new double[nb]; f.tfl = new int[nb]; f.tn = new int[nb];
			for (int i = 0; i < nb; i++) { f.tx[i] = F(); f.ty[i] = F(); f.ts[i] = F(); f.tt[i] = F(); f.toff[i] = F(); f.tfl[i] = r.ReadByte(); f.tn[i] = r.ReadByte(); }
			int ns = r.ReadByte(); for (int i = 0; i < ns; i++) f.snaps.Add((r.ReadByte(), F(), F(), F(), F()));
			int nh = r.ReadInt16(); for (int i = 0; i < nh; i++) f.hail.Add((F(), F()));
			int nst = r.ReadInt16(); for (int i = 0; i < nst; i++) f.steps.Add((F(), F(), F(), r.ReadByte()));
			if (duo)
			{
				f.murk = F(); f.riseY = F(); f.duo = r.ReadByte();
				int nw = r.ReadByte(); for (int i = 0; i < nw; i++) f.bars.Add((F(), r.ReadSByte(), r.ReadByte(), r.ReadByte(), r.ReadByte()));
			}
			o.Add(f);
		}
		return o;
	}
	// from the list: true when it could be read
	static bool tmRpLoad(MyRp it, PlatLevel P)
	{
		if (P?.team == null || it.segs == null || it.segs.Count == 0) return false;
		try { tmrp = new TmRpData { P = P, L = P.team, fr = tmRpParse(it.segs[0].d, P.team), seat = it.tmSeat, names = it.tmNames ?? new[] { "الأحمر", "الأزرق", "الأصفر" }, skins = it.tmSkins ?? new[] { "", "", "" }, strs = it.tmStr ?? new List<string> { "" } }; }
		catch (Exception) { tmrp = null; return false; }
		if (tmrp.fr.Count < 2 || tmrp.fr.Any(f => f.page >= P.team.pages.Count)) { tmrp = null; return false; }
		rp.doc = new RpDoc { r = it.r, res = it.res, v = RP_V, hz = TM_HZ, segs = new List<RpSeg> { new RpSeg { t0 = 0, n = tmrp.fr.Count, b = new ushort[3] } } };
		return true;
	}
	static bool tmRpReady() => tmrp?.L != null && (noMus || tmrp.L.bufs == null || tmrp.L.bufs.All(b => b != null)) && actx.state == "running";
	static void tmRpStart()
	{
		tmRpSaveMe = tmMe; tmRpSaveView = tmView; tmMe = tmView = tmrp.seat;   // drawn as this PC saw it (its own clouds, the others' names)
		rp.saveEasy = easy; easy = false; hitsTotal = 0; deaths = 0; rp.paused = false; rp.rate = 1; rp.barT = 4; tmRpCutPrep(); rpTimeline(); tmRpGo(0, null);
	}
	// v46 "بدون الموتات": the tries that ended with the team back at the page's start are left out (a fall someone was brought back
	// from stays: the play went on). A try = the pictures between two restarts, or between a restart and a change of song part.
	static void tmRpCutPrep()
	{
		var F = tmrp.fr; tmrp.frFull = F; rp.cutOk = rp.cutOn = false; rp.full = rp.cut = null; tmrp.frCut = null;
		var keep = new bool[F.Count]; int a = 0;
		for (int k = 1; k <= F.Count; k++)
		{
			bool restart = k < F.Count && F[k].restarts > F[k - 1].restarts, part = k < F.Count && tmRpGroup(F[k].page) != tmRpGroup(F[k - 1].page);
			if (k < F.Count && !restart && !part) continue;
			for (int j = a; j < k; j++) keep[j] = !restart; a = k;
		}
		if (keep.All(x => x)) return;
		tmrp.cutOi = Enumerable.Range(0, F.Count).Where(i => keep[i]).ToArray(); tmrp.frCut = tmrp.cutOi.Select(i => F[i]).ToList();
		rp.cutOk = tmrp.frCut.Count >= 2;
	}
	static void tmRpCutToggle()
	{
		double tr = state == "rpEnd" ? (tmrp.fr.Count - 1) / (double)TM_HZ : rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; int i = Math.Clamp((int)Math.Floor(tr * TM_HZ + 1e-6), 0, tmrp.fr.Count - 1);
		bool end = state == "rpEnd"; rp.cutOn = !rp.cutOn;
		int j = rp.cutOn ? Math.Max(0, Array.FindIndex(tmrp.cutOi, o => o >= i)) : tmrp.cutOi[Math.Min(i, tmrp.cutOi.Length - 1)];
		if (rp.cutOn && Array.FindIndex(tmrp.cutOi, o => o >= i) < 0) j = tmrp.cutOi.Length - 1;
		tmrp.fr = rp.cutOn ? tmrp.frCut : tmrp.frFull; rp.doc.segs[0].n = tmrp.fr.Count; rpTimeline();
		if (end) { tmRpApply((tmrp.fr.Count - 1) / (double)TM_HZ, 0, false); return; }
		bool wasPaused = rp.paused; if (wasPaused) rpPause(false);
		tmRpGo(0, j / (double)TM_HZ); if (wasPaused) rpPause(true);
	}
	static void tmRpStopMusic() { if (tmrp?.stat != null) { tmrp.stat.gain.setTargetAtTime(0, actx.currentTime, 0.04); tmrp.stat.stop(actx.currentTime + 0.3); tmrp.stat = null; } if (tmrp?.voices == null) return; foreach (var v in tmrp.voices) if (v != null) { v.gain.setTargetAtTime(0, actx.currentTime, 0.04); v.stop(actx.currentTime + 0.3); } tmrp.voices = null; tmrp.mkey = null; }
	static void tmRpRate(double r) { if (tmrp?.voices != null) foreach (var v in tmrp.voices) v?.playbackRate.setValueAtTime(r, actx.currentTime); }
	// the first page of a run of pages that go on from each other (one song part)
	static int tmRpGroup(int page) { while (page > 0 && tmrp.L.pages[page].cont) page--; return page; }
	// after the photo: the song goes on from its outro on the beat after the win (TeamRun.tmOutro)
	static double tmRpOutroAt(TeamPage P, double wonAt)
	{
		var B = P.beats; if (B == null || B.Length < 2) return wonAt;
		double len = B[^1] - B[0], k = Math.Floor(wonAt / len), tt = wonAt - k * len + B[0];
		int i = Array.BinarySearch(B, tt); if (i < 0) i = ~i - 1; i = Math.Clamp(i, 0, B.Length - 2);
		return wonAt + Math.Max(0.02, B[i + 1] - tt);
	}
	// the song for frame f (started again only when the part changes, after a jump, or when the outro starts)
	static void tmRpMusic(TFrame f, bool force)
	{
		var L = tmrp.L; var P = L.pages[f.page]; double t = f.g[0];
		bool won = (f.fl & 1) != 0 && P.outro > 0; double oAt = won ? tmRpOutroAt(P, f.g[4]) : 0; bool outro = won && t >= oAt;
		bool mic = (f.fl & 1) != 0 && P.mic;   // the radio's end: the song clear, on from where it was to its end (TeamDuo.tmMicWin)
		string key = tmRpGroup(f.page) + (outro ? "o" : "") + (mic ? "m" : "");
		double tr = rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; bool jump = !double.IsNaN(tmrp.lastT) && Math.Abs(t - (tmrp.lastT + (tr - tmrp.lastTr) * 1)) > 0.3 && !outro;
		tmrp.lastT = t; tmrp.lastTr = tr;
		if (!force && !jump && key == tmrp.mkey) { tmRpStems(f); return; }
		tmRpStopMusic(); tmrp.mkey = key; if (noMus || L.bufs == null) return;
		var lp = P.loop; double len = lp[1] - lp[0], wait = Math.Max(0, -t) / rp.rate, off = outro ? P.outro + (t - oAt) : t <= 0 ? lp[0] : lp[0] + (len > 0 ? t % len : 0);
		if (mic) { double wAt = Math.Max(0, f.g[4]); off = lp[0] + (len > 0 ? wAt % len : 0) + (t - wAt); }
		double when = actx.currentTime + 0.03 + wait; tmrp.voices = new Voice[L.bufs.Length];
		for (int i = 0; i < L.bufs.Length; i++)
		{
			if (L.bufs[i] == null) continue;
			var v = outro || mic ? new Voice { song = L.bufs[i], loop = false } : new Voice { song = L.bufs[i], loop = true, loopStart = lp[0], loopEnd = lp[1] };
			if (tmRpDuo(L)) v.murk = new AParam(f.murk);
			v.playbackRate.value = rp.rate; v.gain.value = outro || (f.stems & (1 << i)) != 0 ? 1 : 0; v.start(when, off); tmrp.voices[i] = AudioCtx.Add(v);
		}
		tmrp.von = Enumerable.Range(0, L.bufs.Length).Select(i => outro || (f.stems & (1 << i)) != 0).ToArray();
		tmrp.murkNow = f.murk;
		if (tmRpDuo(L))   // his radio static over it, as loud as the song is garbled
		{
			if (!staticTried) { staticTried = true; staticBuf = LoadSong("SFX_STATIC"); }
			if (staticBuf != null) { var st = new Voice { song = staticBuf, loop = true, loopStart = STATIC_LOOP, loopEnd = 2 * STATIC_LOOP }; st.gain.value = tmStaticGain(f.murk); st.start(when, STATIC_LOOP); tmrp.stat = AudioCtx.Add(st); }
		}
	}
	static void tmRpStems(TFrame f)
	{
		if (tmrp.voices == null || tmrp.mkey?.EndsWith("o") == true) return;
		if (tmRpDuo(tmrp.L) && Math.Abs(f.murk - tmrp.murkNow) > 0.001)
		{
			tmrp.murkNow = f.murk; foreach (var v in tmrp.voices) v?.murk?.setTargetAtTime(f.murk, actx.currentTime, 0.35);
			tmrp.stat?.gain.setTargetAtTime(tmStaticGain(f.murk), actx.currentTime, 0.35);
		}
		for (int i = 0; i < tmrp.voices.Length; i++) { bool on = (f.stems & (1 << i)) != 0; var v = tmrp.voices[i]; if (v != null && tmrp.von[i] != on) { tmrp.von[i] = on; v.gain.setTargetAtTime(on ? 1 : 0, actx.currentTime, 0.25); } }
	}
	static void tmRpGo(int i, double? T)
	{
		rp.seg = 0; double tr = Math.Max(0, T ?? 0); tm = null; tmrp.prev = null;
		particles = new List<Particle>(); shake = flash = 0;
		tmRpApply(tr, 0, false); tmRpMusic(tmRpFrame(tr), true);
		rp.aT = tr; rp.aCT = actx.currentTime; state = "replay";
	}
	static TFrame tmRpFrame(double tr) => tmrp.fr[Math.Clamp((int)Math.Floor(tr * TM_HZ + 1e-6), 0, tmrp.fr.Count - 1)];
	// the picture at second tr (fx false while jumping: no sounds)
	static void tmRpApply(double tr, double dt, bool fx)
	{
		double fi = clamp(tr * TM_HZ + 1e-6, 0, tmrp.fr.Count - 1.0001); int ia = (int)Math.Floor(fi); double k = fi - ia;
		var A = tmrp.fr[ia]; var C = tmrp.fr[Math.Min(ia + 1, tmrp.fr.Count - 1)]; bool same = C.page == A.page && C.g[0] >= A.g[0] && C.g[0] - A.g[0] < 0.5;
		double Lr(double a, double c) => same ? a + (c - a) * k : a;
		var L = tmrp.L;
		if (tm == null || tm.page != A.page || tm.L != L || (tm.th.Count > 0 && tnBase() != A.tx.Length))
		{
			tm = new TeamRun { L = L, page = A.page, P = L.pages[A.page], names = tmrp.names.ToArray(), skins = tmrp.skins.Select(x => x ?? "").Concat(new[] { "", "", "" }).Take(TM_N).ToArray() }; tm.stemOn = new bool[L.stems.Length]; tmBuild();
		}
		var p0 = tmrp.prev; if (p0 != null && Math.Abs(A.g[0] - p0.g[0]) > 0.5) p0 = null;   // after a left-out try: no fall / piece effects from the jump
		tm.t = Lr(A.g[0], C.g[0]); songT = tr; tm.pageAt = Lr(A.g[1], C.g[1]); tm.doneAt = A.g[2]; tm.restartAt = A.g[3]; tm.wonAt = A.g[4]; tm.reveal = A.g[5]; tm.chaseX = Lr(A.g[6], C.g[6]);
		tm.swapAt = A.g[7]; tm.msgT = A.g[8]; tm.playT = A.g[9]; if (fx) { flash = Math.Max(flash, A.g[10]); shake = Math.Max(shake, A.g[11]); }
		tm.won = (A.fl & 1) != 0; tm.fromClouds = (A.fl & 2) != 0; tm.swapA = A.swapA; tm.swapB = A.swapB; tm.snapAt = A.snapAt; tm.coverTheme = A.cover;
		tm.msg = A.msg >= 0 && A.msg < tmrp.strs.Count ? tmrp.strs[A.msg] : ""; tm.restarts = A.restarts; tm.hits = A.hits;
		for (int i = 0; i < tm.stemOn.Length; i++) tm.stemOn[i] = (A.stems & (1 << i)) != 0;
		for (int i = 0; i < TM_N; i++) { tm.falls[i] = A.falls[i]; tm.flips[i] = A.flips[i]; tm.xpGot[i] = A.xp[i]; }
		tm.got.Clear(); foreach (var (x, y) in A.got) tm.got.Add(x + "," + y);
		// the things (the same order as the page builds them)
		int nb = Math.Min(A.tx.Length, tm.th.Count);
		for (int i = 0; i < nb; i++)
		{
			var s = tm.th[i]; bool sc = same && i < C.tx.Length; double ox = s.X, oy = s.Y;
			s.X = sc ? A.tx[i] + (C.tx[i] - A.tx[i]) * k : A.tx[i]; s.Y = sc ? A.ty[i] + (C.ty[i] - A.ty[i]) * k : A.ty[i]; s.s = sc ? A.ts[i] + (C.ts[i] - A.ts[i]) * k : A.ts[i];
			s.timer = A.tt[i]; s.off = sc ? A.toff[i] + (C.toff[i] - A.toff[i]) * k : A.toff[i]; s.dx = s.X - ox; s.dy = s.Y - oy;
			int f = A.tfl[i]; s.on = (f & 1) != 0; s.melt = (f & 2) != 0; s.gone = (f & 4) != 0; s.solid = (f & 8) != 0; s.n = A.tn[i];
		}
		tm.th.RemoveAll(s => s.k == "snap");
		foreach (var q in A.snaps) tm.th.Add(new TS { d = new TThing { k = "snap", a = q.who }, k = "snap", X = q.x, Y = q.y, W = 40, H = 10, one = true, solid = true, timer = q.timer, s = q.s });
		tm.hail.Clear(); foreach (var (x, y) in A.hail) tm.hail.Add(new THail { x = x, y = y });
		tm.steps.Clear(); foreach (var q in A.steps) tm.steps.Add(q);
		if (tmRpDuo(L))
		{
			tm.murk = A.murk; tm.riseY = double.IsNaN(A.riseY) || double.IsNaN(C.riseY) ? A.riseY : Lr(A.riseY, C.riseY); tm.ropeLive = (A.duo & 1) != 0; tm.shield = Math.Clamp(A.duo >> 1, 0, tm.b.Length - 1);
			tm.bars.Clear(); bool sb = same && C.bars.Count == A.bars.Count;
			for (int i = 0; i < A.bars.Count; i++)
			{
				var q = A.bars[i]; double x = sb && C.bars[i].dir == q.dir && C.bars[i].r0 == q.r0 ? Lr(q.x, C.bars[i].x) : q.x;
				tm.bars.Add(new TBar { x = x, x0 = x, dir = q.dir, r0 = q.r0, on = Enumerable.Range(0, q.n).Select(r => (q.on & (1 << r)) != 0).ToArray(), v = 420 });
			}
		}
		// the three
		for (int i = 0; i < tm.b.Length; i++)
		{
			var b = tm.b[i]; var a = A.bd[i]; var c = C.bd[i]; bool wasDown = b.down, wasGate = b.gate; int wasHp = b.hp; bool wasDash = b.dash > 0;
			b.x = Lr(a[0], c[0]); b.y = Lr(a[1], c[1]); b.sa = a[2]; b.sk = Lr(a[3], c[3]); b.fx = a[4]; b.ddx = a[5]; b.ddy = a[6]; b.dash = a[7]; b.hurt = a[8]; b.revT = a[9]; b.still = a[10]; b.downAt = a[11];
			b.hp = A.hp[i]; int f = A.bfl[i]; b.down = (f & 1) != 0; b.gate = (f & 2) != 0; b.armed = (f & 4) != 0; b.hang = (f & 16) != 0;
			b.carry = (f & 8) != 0 ? tm.th.FirstOrDefault(q => q.k == "pic" && (int)q.d.a == i && !q.on) : null;
			if (dt > 0) { b.trail.Add((b.x, b.y, tm.t)); b.trail.RemoveAll(q => tm.t - q.t > 0.18 || q.t > tm.t); } else b.trail.Clear();
			if (!fx || p0 == null) continue;
			string col = tmCol(i);
			if (b.down && !wasDown) { burst(b.x, b.y, col, 50, 520); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); }); }
			else if (!b.down && wasDown) { burst(b.x, b.y, col, 30, 320); sfx(tt => { sTone(tt, 523, 0.12, "triangle", 0.1); sTone(tt + 0.08, 784, 0.18, "triangle", 0.1); }); }
			else if (!b.down && b.hp < wasHp) { burst(b.x, b.y, col, 22, 380); sfx(tt => { sTone(tt, 110, 0.3, "square", 0.18); sNoise(tt, 0.25, "lowpass", 900, 0.3); }); }
			if (b.gate && !wasGate) { burst(b.x, b.y, col, 20, 260); sfx(tt => sTone(tt, 880, 0.15, "triangle", 0.08)); }
			if (b.dash > 0 && !wasDash && !b.down) burst(b.x, b.y, col, 8, 200);
		}
		if (fx && p0 != null && p0.page == A.page)
		{
			foreach (var (x, y) in A.got) if (!p0.got.Contains((x, y)))
			{
				burst(x * PT + PT / 2, y * PT + PT / 2, GOLD, 30, 380);
				sfx(tt => { sTone(tt, 784, 0.12, "triangle", 0.12); sTone(tt + 0.07, 1047, 0.12, "triangle", 0.12); sTone(tt + 0.14, 1568, 0.25, "triangle", 0.1); });
			}
			if (A.g[2] >= 0 && p0.g[2] < 0) { burst(W / 2, H / 2, GOLD, 60, 520); sfx(tt => { sTone(tt, 523, 0.2, "triangle", 0.12); sTone(tt + 0.12, 659, 0.2, "triangle", 0.12); sTone(tt + 0.24, 784, 0.4, "triangle", 0.12); }); }
			if ((A.fl & 1) != 0 && (p0.fl & 1) == 0) { burst(W / 2, H / 2, GOLD, 90, 640); sfx(tt => { sNoise(tt, 0.3, "highpass", 3000, 0.2); sTone(tt + 0.05, 523, 0.3, "triangle", 0.12); sTone(tt + 0.17, 659, 0.3, "triangle", 0.12); sTone(tt + 0.29, 784, 0.6, "triangle", 0.12); }); }
			if (A.g[10] > p0.g[10] + 0.3) sfx(tt => sNoise(tt, 0.08, "highpass", 5000, 0.2));   // the camera's flash
		}
		tmrp.prev = A;
	}
	static void tmRpUpdate(double dt)
	{
		if (rp.paused) return;
		double tr = rp.aT + Math.Max(0, actx.currentTime - rp.aCT) * rp.rate; int n = tmrp.fr.Count;
		if (tr * TM_HZ >= n - 1) { tmRpApply((n - 1) / (double)TM_HZ, dt * rp.rate, true); tmRpStopMusic(); state = "rpEnd"; rp.endAt = performance.now(); return; }
		tmRpApply(tr, dt * rp.rate, true); tmRpMusic(tmRpFrame(tr), false);
	}
	static bool tmRpDraw(double now)
	{
		if (!tmRpOn || tm == null || !(state is "replay" or "rpGap" or "rpEnd")) return false;
		drawTeam(now); return true;
	}
	static void tmRpExit() { tmRpStopMusic(); tm = null; tmrp = null; tmMe = tmRpSaveMe; tmView = tmRpSaveView; }
}
