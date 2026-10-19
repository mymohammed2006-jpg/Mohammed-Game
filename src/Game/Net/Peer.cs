using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.Net;

namespace MG;

// One connection to the other player, straight between the two PCs (WebRTC data channels, SIPSorcery: free, no server of ours).
// Google's free STUN servers tell each PC its address on the internet; the two offers go through Firebase (rooms/{code}).
// Channel "u": fast, may drop (where the square is); channel "r": every message arrives, in order (events).
// SIPSorcery calls back on its own threads: messages wait in `inbox` until the game reads them on its frame.
public class Peer
{
	public static List<RTCIceServer> ICE = new()
	{
		new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
		new RTCIceServer { urls = "stun:stun1.l.google.com:19302" },
	};
	public static int GatherMs = 3000;   // the longest wait for the PC's addresses before the offer is sent

	RTCPeerConnection pc; RTCDataChannel u, r;
	public readonly ConcurrentQueue<(bool rel, byte[] d)> inbox = new();
	public volatile bool closed;
	public bool open => !closed && u?.readyState == RTCDataChannelState.open && r?.readyState == RTCDataChannelState.open;

	RTCPeerConnection make()
	{
		var p = new RTCPeerConnection(new RTCConfiguration { iceServers = ICE });
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
	async Task<string> gathered(RTCSessionDescriptionInit d)
	{
		var done = new TaskCompletionSource();
		pc.onicegatheringstatechange += s => { if (s == RTCIceGatheringState.complete) done.TrySetResult(); };
		await pc.setLocalDescription(d);
		await Task.WhenAny(done.Task, Task.Delay(GatherMs));
		return pc.localDescription.sdp.ToString();
	}

	// the room's maker: the offer to put in the room
	public async Task<string> Offer()
	{
		pc = make();
		u = await pc.createDataChannel("u", new RTCDataChannelInit { ordered = false, maxRetransmits = 0 }); hook(u);
		r = await pc.createDataChannel("r"); hook(r);
		return await gathered(pc.createOffer());
	}
	public bool Answer(string sdp) => pc.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = sdp }) == SetDescriptionResultEnum.OK;

	// the one who joins: the answer to the room's offer (null = a bad offer)
	public async Task<string> Join(string offer)
	{
		pc = make();
		pc.ondatachannel += ch => { if (ch.label == "u") u = ch; else r = ch; hook(ch); };
		if (pc.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer }) != SetDescriptionResultEnum.OK) return null;
		return await gathered(pc.createAnswer());
	}

	public void Send(bool rel, byte[] d) { try { if (open) (rel ? r : u).send(d); } catch (Exception) { } }
	public void Send(string msg) => Send(true, Encoding.UTF8.GetBytes(msg));
	public void Close() { closed = true; try { pc?.Close("bye"); } catch (Exception) { } }
}
