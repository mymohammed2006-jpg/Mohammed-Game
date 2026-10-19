using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MG;

// Reads a Chromium "Local Storage/leveldb" folder (the old PC app's saves), read-only.
// Supports what Chromium writes: the write-ahead .log files and .ldb tables (raw or Snappy blocks).
// The newest value of each key wins (by sequence number); deleted keys are dropped.
public static class LevelDb
{
	public static Dictionary<string, byte[]> ReadAll(string dir)
	{
		var best = new Dictionary<string, (ulong seq, byte[] val)>();
		void Put(byte[] key, ulong seq, byte[] val)
		{
			string k = Convert.ToBase64String(key);
			if (!best.TryGetValue(k, out var cur) || seq >= cur.seq) best[k] = (seq, val);
		}
		foreach (var f in Directory.GetFiles(dir))
		{
			try
			{
				if (f.EndsWith(".ldb") || f.EndsWith(".sst")) ReadTable(File.ReadAllBytes(f), Put);
				else if (f.EndsWith(".log")) ReadLog(File.ReadAllBytes(f), Put);
			}
			catch (Exception) { /* a damaged file: use what the others have */ }
		}
		var res = new Dictionary<string, byte[]>();
		foreach (var kv in best) if (kv.Value.val != null) res[Encoding.Latin1.GetString(Convert.FromBase64String(kv.Key))] = kv.Value.val;
		return res;
	}

	static ulong Varint(byte[] b, ref int p)
	{
		ulong r = 0; int sh = 0;
		while (true) { byte c = b[p++]; r |= (ulong)(c & 0x7f) << sh; if ((c & 0x80) == 0) return r; sh += 7; }
	}

	// .log: 32 KB blocks of records (crc 4, length 2, type 1); each full record is a write batch
	static void ReadLog(byte[] d, Action<byte[], ulong, byte[]> put)
	{
		const int BLOCK = 32768; int p = 0; var rec = new MemoryStream();
		while (p + 7 <= d.Length)
		{
			int left = BLOCK - p % BLOCK; if (left < 7) { p += left; continue; }
			int len = d[p + 4] | (d[p + 5] << 8); byte type = d[p + 6]; p += 7;
			if (p + len > d.Length) break;
			if (type == 0 && len == 0) { p += left - 7; continue; }   // zero padding
			if (type == 1) { rec.SetLength(0); rec.Write(d, p, len); Batch(rec.ToArray(), put); }
			else if (type == 2) { rec.SetLength(0); rec.Write(d, p, len); }
			else if (type == 3) rec.Write(d, p, len);
			else if (type == 4) { rec.Write(d, p, len); Batch(rec.ToArray(), put); rec.SetLength(0); }
			p += len;
		}
	}
	static void Batch(byte[] b, Action<byte[], ulong, byte[]> put)
	{
		if (b.Length < 12) return;
		ulong seq = BitConverter.ToUInt64(b, 0); uint n = BitConverter.ToUInt32(b, 8); int p = 12;
		for (uint i = 0; i < n && p < b.Length; i++)
		{
			byte t = b[p++]; int kl = (int)Varint(b, ref p); var key = b[p..(p + kl)]; p += kl;
			if (t == 1) { int vl = (int)Varint(b, ref p); var v = b[p..(p + vl)]; p += vl; put(key, seq + i, v); }
			else put(key, seq + i, null);
		}
	}

	// .ldb: footer -> index block -> data blocks; keys are internal keys (user key + 8 bytes of seq<<8|type)
	static void ReadTable(byte[] d, Action<byte[], ulong, byte[]> put)
	{
		int q = d.Length - 48; Varint(d, ref q); Varint(d, ref q);   // metaindex handle (not needed)
		int idxOff = (int)Varint(d, ref q), idxSize = (int)Varint(d, ref q);
		var index = Block(d, idxOff, idxSize);
		foreach (var (_, hv) in Entries(index))
		{
			int hp = 0; int off = (int)Varint(hv, ref hp), size = (int)Varint(hv, ref hp);
			foreach (var (k, v) in Entries(Block(d, off, size)))
			{
				if (k.Length < 8) continue;
				ulong tag = BitConverter.ToUInt64(k, k.Length - 8); var uk = k[..^8];
				put(uk, tag >> 8, (tag & 0xff) == 1 ? v : null);
			}
		}
	}
	static byte[] Block(byte[] d, int off, int size)
	{
		byte type = d[off + size];
		var raw = d[off..(off + size)];
		return type == 1 ? Snappy(raw) : raw;
	}
	static IEnumerable<(byte[] k, byte[] v)> Entries(byte[] b)
	{
		int nr = BitConverter.ToInt32(b, b.Length - 4); int end = b.Length - 4 - nr * 4; int p = 0; byte[] last = Array.Empty<byte>();
		while (p < end)
		{
			int shared = (int)Varint(b, ref p), ns = (int)Varint(b, ref p), vl = (int)Varint(b, ref p);
			var k = new byte[shared + ns]; Array.Copy(last, k, shared); Array.Copy(b, p, k, shared, ns); p += ns;
			var v = b[p..(p + vl)]; p += vl; last = k;
			yield return (k, v);
		}
	}
	static byte[] Snappy(byte[] s)
	{
		int p = 0; int n = (int)Varint(s, ref p); var o = new byte[n]; int op = 0;
		while (p < s.Length)
		{
			byte tag = s[p++]; int t = tag & 3;
			if (t == 0)
			{
				int len = tag >> 2;
				if (len >= 60) { int nb = len - 59; len = 0; for (int i = 0; i < nb; i++) len |= s[p++] << (8 * i); }
				len++; Array.Copy(s, p, o, op, len); p += len; op += len;
			}
			else
			{
				int len, off;
				if (t == 1) { len = ((tag >> 2) & 7) + 4; off = ((tag >> 5) << 8) | s[p++]; }
				else if (t == 2) { len = (tag >> 2) + 1; off = s[p] | (s[p + 1] << 8); p += 2; }
				else { len = (tag >> 2) + 1; off = s[p] | (s[p + 1] << 8) | (s[p + 2] << 16) | (s[p + 3] << 24); p += 4; }
				for (int i = 0; i < len; i++) { o[op] = o[op - off]; op++; }
			}
		}
		return o;
	}

	// Chromium's localStorage rows: key "_" + origin + "\0" + (0x01 + Latin-1 name | 0x00 + UTF-16LE name),
	// value 0x01 + Latin-1 text or 0x00 + UTF-16LE text
	static string Str(byte[] b, int start)
	{
		if (b.Length <= start) return null;
		return b[start] == 1 ? Encoding.Latin1.GetString(b, start + 1, b.Length - start - 1) : b[start] == 0 ? Encoding.Unicode.GetString(b, start + 1, b.Length - start - 1) : null;
	}
	public static Dictionary<string, string> LocalStorage(string dir, string origin)
	{
		var res = new Dictionary<string, string>(); var pre = Encoding.Latin1.GetBytes("_" + origin + "\0");
		foreach (var (k, v) in ReadAll(dir))
		{
			var kb = Encoding.Latin1.GetBytes(k);
			if (kb.Length <= pre.Length || !kb.AsSpan(0, pre.Length).SequenceEqual(pre)) continue;
			string name = Str(kb, pre.Length), val = Str(v, 0);
			if (name != null && val != null) res[name] = val;
		}
		return res;
	}
}
