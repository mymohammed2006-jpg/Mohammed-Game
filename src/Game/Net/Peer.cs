using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.Net;

namespace MG;

// One connection to the other player, straight between the two PCs (WebRTC data channels, SIPSorcery: free, no server of ours).
// Google's free STUN servers tell each PC its address on the internet; the two offers go through Firebase (rooms/{code}).
// When a network blocks the direct way, the packets go through a free relay (TURN): Open Relay (metered.ca) with its public
// shared secret, and the project's own free metered.ca key when METERED_URL is set.
// Channel "u": fast, may drop (where the square is); channel "r": every message arrives, in order (events).
// SIPSorcery calls back on its own threads: messages wait in `inbox` until the game reads them on its frame.
public class Peer
{
	public static List<RTCIceServer> ICE = new()
	{
		new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
		new RTCIceServer { urls = "stun:stun1.l.google.com:19302" },
	};
	public static bool Relay = true;      // tests: false (no internet relay here)
	public static bool RelayOnly;          // tests: only the relay (to check the relay path)
	public static string METERED_URL = "";   // "https://<app>.metered.live/api/v1/turn/credentials?apiKey=<key>" (free plan, 20 GB a month)
	public static int GatherMs = 5000;   // the longest wait for the PC's addresses (and the relay's) before the offer is sent
	static readonly System.Net.Http.HttpClient http = new() { Timeout = TimeSpan.FromSeconds(4) };
	// TURN REST credentials: user = expiry time, password = base64(HMAC-SHA1(secret, user))
	static RTCIceServer openRelay(string url)
	{
		string user = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 6 * 3600) + ":mg";
		using var h = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes("openrelayprojectsecret"));
		return new RTCIceServer { urls = url, username = user, credential = Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(user))), credentialType = RTCIceCredentialType.password };
	}
	// "stun:host:port" -> "stun:1.2.3.4:port" (our own lookup, the library's can fail on some PCs)
	static async Task<string> byIp(string url)
	{
		try
		{
			int c = url.IndexOf(':'), q = url.IndexOf('?'); string rest = q < 0 ? url[(c + 1)..] : url[(c + 1)..q], tail = q < 0 ? "" : url[q..];
			int pc = rest.LastIndexOf(':'); string host = pc < 0 ? rest : rest[..pc], port = pc < 0 ? "" : rest[pc..];
			if (System.Net.IPAddress.TryParse(host, out _)) return url;
			var ips = await System.Net.Dns.GetHostAddressesAsync(host);
			var ip = Array.Find(ips, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) ?? (ips.Length > 0 ? ips[0] : null);
			return ip == null ? url : url[..(c + 1)] + ip + port + tail;
		}
		catch (Exception) { return url; }
	}
	static async Task<List<RTCIceServer>> servers()
	{
		var l = new List<RTCIceServer>();
		foreach (var s in ICE) l.Add(new RTCIceServer { urls = await byIp(s.urls), username = s.username, credential = s.credential, credentialType = s.credentialType });
		if (!Relay) return l;
		if (METERED_URL != "")
			try
			{
				var a = System.Text.Json.Nodes.JsonNode.Parse(await http.GetStringAsync(METERED_URL)) as System.Text.Json.Nodes.JsonArray;
				foreach (var s in a ?? new()) { string u = (string)s["urls"]; if (u != null && u.StartsWith("turn")) l.Add(new RTCIceServer { urls = u, username = (string)s["username"], credential = (string)s["credential"], credentialType = RTCIceCredentialType.password }); }
			}
			catch (Exception) { }
		l.Add(openRelay(await byIp("turn:staticauth.openrelay.metered.ca:80")));
		l.Add(openRelay(await byIp("turn:staticauth.openrelay.metered.ca:443?transport=tcp")));
		return l;
	}

	RTCPeerConnection pc; RTCDataChannel u, r;
	public readonly ConcurrentQueue<(bool rel, byte[] d)> inbox = new();
	public volatile bool closed;
	public bool open => !closed && u?.readyState == RTCDataChannelState.open && r?.readyState == RTCDataChannelState.open;

	// the library starts looking for addresses as soon as the connection is made; an offer or answer only carries
	// the addresses found by then, so it is written after the search ends (or after GatherMs)
	TaskCompletionSource found;
	async Task<RTCPeerConnection> make()
	{
		var p = new RTCPeerConnection(new RTCConfiguration { iceServers = await servers(), iceTransportPolicy = RelayOnly ? RTCIceTransportPolicy.relay : RTCIceTransportPolicy.all });
		var f = found = new TaskCompletionSource();
		p.onicegatheringstatechange += s => { if (s == RTCIceGatheringState.complete) f.TrySetResult(); };
		if (p.iceGatheringState == RTCIceGatheringState.complete) f.TrySetResult();
		p.onconnectionstatechange += s => { if (s is RTCPeerConnectionState.failed or RTCPeerConnectionState.closed) closed = true; };
		return p;
	}
	void hook(RTCDataChannel ch)
	{
		bool rel = ch.label == "r";
		ch.onmessage += (c, proto, d) => inbox.Enqueue((rel, d));
		ch.onclose += () => closed = true;
	}
	// the SDP once every address is known (or after GatherMs), so one write to Firebase carries all of them
	async Task<string> gathered(Func<RTCSessionDescriptionInit> d)
	{
		await Task.WhenAny(found.Task, Task.Delay(GatherMs));
		await pc.setLocalDescription(d());
		return mine = pc.localDescription.sdp.ToString();
	}
	// what each side found (for the "can't connect" line): direct addresses, internet address, relay
	string mine = "", theirs = "";
	static string kinds(string sdp) { int c(string t) => sdp.Split("typ " + t).Length - 1; return $"{c("host")}/{c("srflx")}/{c("relay")}"; }
	public string Diag() => $"أنت {kinds(mine)} · صاحبك {kinds(theirs)}";

	// the room's maker: the offer to put in the room
	public async Task<string> Offer()
	{
		pc = await make();
		u = await pc.createDataChannel("u", new RTCDataChannelInit { ordered = false, maxRetransmits = 0 }); hook(u);
		r = await pc.createDataChannel("r"); hook(r);
		return await gathered(() => pc.createOffer());
	}
	public bool Answer(string sdp) => (theirs = sdp) != null && pc.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = sdp }) == SetDescriptionResultEnum.OK;

	// the one who joins: the answer to the room's offer (null = a bad offer)
	public async Task<string> Join(string offer)
	{
		pc = await make();
		pc.ondatachannel += ch => { if (ch.label == "u") u = ch; else r = ch; hook(ch); };
		theirs = offer;
		await Task.WhenAny(found.Task, Task.Delay(GatherMs));
		if (pc.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer }) != SetDescriptionResultEnum.OK) return null;
		return await gathered(() => pc.createAnswer());
	}

	public void Send(bool rel, byte[] d) { try { if (open) (rel ? r : u).send(d); } catch (Exception) { } }
	public void Send(string msg) => Send(true, Encoding.UTF8.GetBytes(msg));
	public void Close() { closed = true; try { pc?.Close("bye"); } catch (Exception) { } }
}
