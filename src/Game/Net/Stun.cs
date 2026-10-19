using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SIPSorcery.Net;

namespace MG;

// Our own quick check of every STUN / TURN server, all at once, before the connection is made.
// SIPSorcery uses one server at a time (a relay first), about 1.3 s for each dead one, and a relay that answers
// but never gives a relay address keeps it waiting for good: so it is only given servers that really work.
// STUN: a "binding" request must come back. TURN: a full "allocate" (with the password) must give a relay address.
public static class Stun
{
	const uint COOKIE = 0x2112A442;
	const ushort BIND = 0x0001, BIND_OK = 0x0101, ALLOC = 0x0003, ALLOC_OK = 0x0103, ALLOC_ERR = 0x0113, REFRESH = 0x0004;
	const ushort A_USER = 0x0006, A_MI = 0x0008, A_ERR = 0x0009, A_LIFE = 0x000D, A_REALM = 0x0014, A_NONCE = 0x0015, A_RELAYED = 0x0016, A_TRANSPORT = 0x0019;

	static byte[] build(ushort type, byte[] tx, List<(ushort t, byte[] v)> attrs, byte[] key = null)
	{
		var b = new List<byte>(); void u16(int v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }
		u16(type); u16(0); b.AddRange(new byte[] { 0x21, 0x12, 0xA4, 0x42 }); b.AddRange(tx);
		foreach (var (t, v) in attrs) { u16(t); u16(v.Length); b.AddRange(v); while (b.Count % 4 != 0) b.Add(0); }
		var m = b.ToArray(); int len = m.Length - 20 + (key != null ? 24 : 0); m[2] = (byte)(len >> 8); m[3] = (byte)len;
		if (key == null) return m;
		using var h = new HMACSHA1(key); var mi = h.ComputeHash(m);
		var r = new byte[m.Length + 24]; m.CopyTo(r, 0); r[m.Length] = 0; r[m.Length + 1] = (byte)A_MI; r[m.Length + 3] = 20; mi.CopyTo(r, m.Length + 4);
		return r;
	}
	static ushort type(byte[] m) => (ushort)(m[0] << 8 | m[1]);
	static byte[] attr(byte[] m, ushort want)
	{
		for (int i = 20; i + 4 <= m.Length;)
		{
			int t = m[i] << 8 | m[i + 1], n = m[i + 2] << 8 | m[i + 3];
			if (i + 4 + n > m.Length) break;
			if (t == want) return m[(i + 4)..(i + 4 + n)];
			i += 4 + n + (4 - n % 4) % 4;
		}
		return null;
	}

	// one server's socket: UDP sends again every 250 ms; TCP sends once (no extra framing: the header has the length)
	sealed class Link : IDisposable
	{
		UdpClient u; TcpClient t; NetworkStream s; IPEndPoint ep;
		public static async Task<Link> Open(IPEndPoint ep, bool tcp, CancellationToken ct)
		{
			var l = new Link { ep = ep };
			if (tcp) { l.t = new TcpClient(ep.AddressFamily); await l.t.ConnectAsync(ep, ct); l.s = l.t.GetStream(); }
			else l.u = new UdpClient(ep.AddressFamily);
			return l;
		}
		public async Task<byte[]> Ask(byte[] req, CancellationToken ct)
		{
			var tx = req[8..20];
			if (t != null)
			{
				await s.WriteAsync(req, ct);
				while (true)
				{
					var hd = await read(20, ct); int n = hd[2] << 8 | hd[3]; var body = await read(n, ct);
					var m = new byte[20 + n]; hd.CopyTo(m, 0); body.CopyTo(m, 20);
					if (m.AsSpan(8, 12).SequenceEqual(tx)) return m;
				}
			}
			while (true)
			{
				await u.SendAsync(req, ep, ct);
				using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct); wait.CancelAfter(250);
				try
				{
					while (true)
					{
						var r = await u.ReceiveAsync(wait.Token);
						if (r.Buffer.Length >= 20 && r.Buffer.AsSpan(8, 12).SequenceEqual(tx)) return r.Buffer;
					}
				}
				catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
			}
		}
		async Task<byte[]> read(int n, CancellationToken ct) { var b = new byte[n]; await s.ReadExactlyAsync(b, ct); return b; }
		public void Send(byte[] m) { try { if (t != null) s.Write(m); else u.Send(m, m.Length, ep); } catch (Exception) { } }
		public void Dispose() { u?.Dispose(); t?.Dispose(); }
	}

	// "stun:1.2.3.4:19302", "turn:1.2.3.4:443?transport=tcp" -> where, and TCP or not
	static async Task<(IPEndPoint ep, bool tcp, bool turn)> where(string url)
	{
		bool turn = url.StartsWith("turn"), tcp = url.Contains("transport=tcp");
		int c = url.IndexOf(':'), q = url.IndexOf('?'); string rest = q < 0 ? url[(c + 1)..] : url[(c + 1)..q];
		int pc = rest.LastIndexOf(':'); string host = pc < 0 ? rest : rest[..pc]; int port = pc < 0 ? 3478 : int.Parse(rest[(pc + 1)..]);
		if (!IPAddress.TryParse(host, out var ip))
		{
			var ips = await Dns.GetHostAddressesAsync(host);
			ip = Array.Find(ips, a => a.AddressFamily == AddressFamily.InterNetwork) ?? ips[0];
		}
		return (new IPEndPoint(ip, port), tcp, turn);
	}

	public static async Task<bool> Check(RTCIceServer srv, int ms)
	{
		using var cts = new CancellationTokenSource(ms); var ct = cts.Token;
		try
		{
			var (ep, tcp, turn) = await where(srv.urls);
			using var l = await Link.Open(ep, tcp, ct);
			if (!turn) return type(await l.Ask(build(BIND, RandomNumberGenerator.GetBytes(12), new()), ct)) == BIND_OK;
			var udp = new List<(ushort, byte[])> { (A_TRANSPORT, new byte[] { 17, 0, 0, 0 }) };
			var m = await l.Ask(build(ALLOC, RandomNumberGenerator.GetBytes(12), udp), ct);
			if (type(m) == ALLOC_OK) return attr(m, A_RELAYED) != null;
			var realm = attr(m, A_REALM); var nonce = attr(m, A_NONCE);
			if (type(m) != ALLOC_ERR || realm == null || nonce == null) return false;
			var key = MD5.HashData(Encoding.UTF8.GetBytes($"{srv.username}:{Encoding.UTF8.GetString(realm)}:{srv.credential}"));
			var auth = new List<(ushort, byte[])> { (A_TRANSPORT, new byte[] { 17, 0, 0, 0 }), (A_USER, Encoding.UTF8.GetBytes(srv.username ?? "")), (A_REALM, realm), (A_NONCE, nonce) };
			m = await l.Ask(build(ALLOC, RandomNumberGenerator.GetBytes(12), auth, key), ct);
			if (type(m) != ALLOC_OK || attr(m, A_RELAYED) == null) return false;
			// let the server free this test relay now (no answer needed)
			l.Send(build(REFRESH, RandomNumberGenerator.GetBytes(12), new() { (A_LIFE, new byte[4]), (A_USER, Encoding.UTF8.GetBytes(srv.username ?? "")), (A_REALM, realm), (A_NONCE, nonce) }, key));
			return true;
		}
		catch (Exception) { return false; }
	}
}
