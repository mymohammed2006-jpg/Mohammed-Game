#!/usr/bin/env python3
# Makes data/dodge_L20.json: L20 "New Game" (Nitro Fun), the design of 10-28. 128 BPM, beat 0 = 0.59075 s, a beat = 0.46875 s.
# (The old design, public v49-v51, is gen_v51.py -> data/dodge_L20_v51.json, for the replays saved before.)
# Song map (beats): intro 0-31 (a kick every beat, the tune's C# notes) | bass 32-63 (0:15.6, kicks on the even beats, the bass on the odd)
# | crash 64, the broken beat 65-79 (0:30.6) | the build 80-111 (0:38.1, lifts 96, 104) | DROP 112-140 (0:53.1) | break 140-143
# | 144-172 (1:08.1) | the near silence 173-175 | 176-204 (1:23.1) | break 205-207 | 208-236 (1:38.1) | break 237-239 | the last hit 240 | win 242
# Parts: 1 press start (0-111), 2 playing (112-175), 3 the boss: the controller itself (176-242). Checkpoints 0, 112, 176.
# Parts 1 and 2 are the controller: its four buttons, the D-pad and the two sticks, each on one sound of the song (L20n.cs, kinds "L20b_*").
# Nothing here is random. "nz": Normal only, "ez": Easy only, where the two have their own patterns.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L20.json')
W, H = 1280, 720
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b_, kind_, **kw):
    d = {'b': round(b_, 4), 'k': kind_}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def fx(b, v=0.3, s=10, dur=1.5): ev(b, 'L20fx', v=v, s=s, dur=dur)
def both(fn):
    # fn(easy) -> list of (b, k, kw); adds the Normal one ("nz") and the Easy one ("ez")
    for ez in (False, True):
        for b, kd, kw in fn(ez): ev(b, kd, **({'ez': True} if ez else {'nz': True}), **kw)

ev(0, 'L20rot', rb=[0, 204, 205, 207.5, 236], ra=[0, 0, 0, 180, 720], keep=True)
ev(0, 'L20stage', to=244, keep=True)

# The song's sounds (my own map of 10-24, checked again 10-28 on the kicks, the bass and the claps): the tune's notes with their pitch
PR = {'C': 0, 'C#': 1, 'D': 2, 'D#': 3, 'E': 4, 'F': 5, 'F#': 6, 'G': 7, 'G#': 8, 'A': 9, 'A#': 10, 'B': 11}
def notes(s): return [(float(t[:t.index('#') - 1 if t.endswith('##') else len(t.rstrip('ABCDEFG#'))]), t[len(t.rstrip('ABCDEFG#')):]) for t in s.split()]
# the tune's notes (beat + pitch), from the song
N_INTRO = notes('0.5C# 1.75C# 3.75C# 4.25C# 5.75C# 6.25C# 6.75E 7.25E 7.75C# 8.25C# 9.25F# 9.75F# 10.25C# 11.75F# 12.25C# 13.75C# 15.5D# 16.25C# 16.5F# 17.75C# 19.75C# 20.25C# 20.5E 21.25E 21.75C# 22.25C# 22.5E 23.25E 23.75C# 24.25C# 25.75F# 26.25C# 27.75F# 28.25C# 29.75C# 31.5D#')
N_BASS = notes('33.75A 35.75A 36.25A 37.75A 38.25A 39.75A 40.25A 40.75C# 41.75A 42.25A 43.75A 44.25A 45.5G# 45.75A 47.75E 48.75C# 49.75A 51.75A 52.25A 53.25E 53.75A 54.25A 54.5E 55.25E 55.75A 56.25A 56.75C# 57.75A 58.25A 60.25A 61.75A 62.0A 63.0A 63.25A 63.5E 63.75C#')
N_STAB = notes('65.0A 65.75A 70.25C# 71.25C# 73.5G# 75.25C# 76.25C# 77.5G# 78.25C# 79.0C# 79.5G# 79.75A')
N_RUN = notes('101.0F# 101.25D 101.5G# 101.75A 103.25D# 103.5A 103.75A')
N_DROP = notes('115.5G# 117.5G# 118.25G# 118.5D# 119.5A 120.0F# 122.75G# 123.5F 124.25A 124.5A 125.25G# 125.5G# 125.75G# 126.0F# 126.25F# 126.5F# 126.75F# 127.5E 127.75E 128.0F# 130.75G# 131.5G# 132.0F 132.75G# 133.5G# 134.0E 134.25G# 134.5G# 135.0A 135.25F 135.5G# 135.75G# 136.0G# 139.0F')
N_BREAK = notes('140.0F# 140.25F# 140.5A 140.75A 141.5E 141.75F# 142.25F# 142.5F#')
N_DROP2 = notes('144.0F# 147.5G# 148.25G# 148.5G# 148.75A 149.5G# 149.75F# 150.25G# 150.5E 151.5A 152.0F# 155.5G# 156.25A 156.5A 157.5G# 158.25F# 158.5F# 158.75F# 159.5E 160.0F# 164.25G# 164.5G# 164.75A 165.5G# 165.75F# 166.0G# 166.25G# 166.5G# 166.75G# 167.0A 167.25G# 167.5G# 167.75G# 168.0G#')
def up(a, b): return (PR[b] - PR[a] + 6) % 12 - 6   # the step from note a to note b (-6 .. 5)

def dg(x, y): return math.degrees(math.atan2(y, x))
def lerpk(kb, kv, bt):
    # the same straight-line keyframes as L20bKey(.., ed 0) in L20n.cs
    if bt <= kb[0]: return kv[0]
    j = max(i for i in range(len(kb)) if bt >= kb[i])
    if j + 1 >= len(kb): return kv[j]
    return kv[j] + (kv[j + 1] - kv[j]) * (bt - kb[j]) / (kb[j + 1] - kb[j])
def ring(b, x, y, sides, ga, gw, v, th=22, rot=0, r0=20, wn=0.6):
    return (b - wn - 0.05, 'L20b_ring', dict(x=round(x, 1), y=round(y, 1), ta=b, sides=sides, rot=round(rot, 2), ga=round(ga, 2), gw=gw, v=v, th=th, r0=r0, wn=wn))
def sgn(a, b, last=1):
    s = up(a, b); return 1 if s > 0 else -1 if s < 0 else last

# ================= part 1: press start (0-111) =================
ev(0, 'L20b_title', b0=0, b1=2)
# 0:01.5 the cross on every kick (Easy every other): it crosses the screen at two places in turn, turning 15 degrees a time;
# from 0:08 it stands in the middle and turns 30 degrees a kick, so the four free corners go round. On the tune's notes a ring of the
# circle button comes out of the cross, open towards one corner: a higher note opens it a corner on clockwise, a lower one back
XC = [(320, 220), (960, 500), (960, 220), (320, 500)]
TQ = [(190, 130), (1090, 130), (1090, 590), (190, 590)]
def intro(ez):
    out = []; xb, xx, xy, xa = [], [], [], []
    for k, b in enumerate(range(2, 31)):
        if ez and b % 2: continue
        if b < 16: (x, y), a = XC[k % 4], 45 + 15 * k
        else: (x, y), a = (640, 360), 45 + 30 * (b - 16)
        xb.append(b); xx.append(x); xy.append(y); xa.append(a)
    out.append((1, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=34 if ez else 40, wn=1.25 if ez else 1.0, lh=0.4)))
    # every ring is open towards one of the four corners (the same one for 2 beats at least, so it can be reached): it moves on a corner
    # with the tune, clockwise for a higher note, back for a lower one
    q = 0; prev = 'C#'; qb = -9
    mel = [n for n in N_INTRO if n[0] >= 2.5]
    for i, (b, pc) in enumerate(mel):
        if b - qb >= 2 and pc != prev: q = (q + sgn(prev, pc, 1)) % 4; qb = b
        prev = pc
        if ez and i % 2: continue
        j = max(i2 for i2 in range(len(xb)) if xb[i2] <= b); tx, ty = TQ[q]
        out.append(ring(b, xx[j], xy[j], 0, dg(tx - xx[j], ty - xy[j]), 85 if ez else 66, 470 if ez else 600))
    return out
both(intro)

# 0:15.6 the bass: the square tiles slam on its hits (the odd beats), diagonal stripes that move on a cell each time; the cross keeps the
# kicks (every 2 beats, Easy 4) at the four quarters' middles, and before each cross a square ring comes out of its place (Normal)
QC = [(320, 180), (960, 540), (960, 180), (320, 540)]
def bass(ez):
    out = []; xb, xx, xy, xa = [], [], [], []
    for k, b in enumerate(range(34, 63, 4 if ez else 2)):
        (x, y) = QC[k % 4]; xb.append(b); xx.append(x); xy.append(y); xa.append(45 + 10 * k)
        if not ez: out.append(ring(b - 0.25, x, y, 4, dg(640 - x, 360 - y) + (35 if k % 2 else -35), 62, 560, rot=45 + 10 * k, wn=0.75))
    out.append((32, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=34 if ez else 42, wn=1.25 if ez else 1.0, lh=0.4)))
    tb, tn, tc = [], [], []
    for k, b in enumerate(range(33, 64, 4 if ez else 2)):
        cells = [c + 8 * r for r in range(5) for c in range(8) if (c + r + k) % (5 if ez else 3) == 0]
        tb.append(b); tn.append(len(cells)); tc += cells
    out.append((31.5, 'L20b_tiles', dict(cols=8, cw=160, ch=144, tb=tb, tn=tn, tc=tc, wn=1.25 if ez else 1.0, lh=0.75)))
    return out
both(bass)
fx(32, 0.12, 8, 1.0)

# 0:30.6 the crash, then the broken beat (hits at 0, 0.5, 1.25, 2, 2.75, 3.5 of every 4 beats): you are round the triangle button.
# Its sides are open in the middle; on the hits it shrinks to the middle and grows past the screen again (its sides sweep over you:
# be on a way through) and turns 40 degrees (the way of the tune's stabs). Normal: on each turn its corners throw shards out
def tri(ez):
    kb, kr, ka = [63], [700], [0]; a = 0; d = 1; si = 0
    stabs = [n for n in N_STAB]; prev = 'A'
    sb, sx, sy, sa = [], [], [], []
    for bar in (64, 68, 72, 76):
        for j, o in enumerate((0, 0.5, 1.25, 2, 2.75, 3.5)):
            b = bar + o
            if b < 65.25: continue
            while si < len(stabs) and stabs[si][0] <= b + 1e-9: d = sgn(prev, stabs[si][1], d); prev = stabs[si][1]; si += 1
            if j in (1, 3):
                if ez and j == 3: continue
                a += 40 * d
                kb.append(b); kr.append(kr[-1]); ka.append(a)
            else:
                kb.append(b); kr.append(170 if kr[-1] > 400 else 700); ka.append(a)
            # the shards: out of the three corners of the small one, along the corners' ways (Easy on the turns only)
            if ez and j not in (1, 3): continue
            for c in range(3):
                q = math.radians(a) - math.pi / 2 + c * 2 * math.pi / 3
                sb.append(b); sx.append(round(640 + math.cos(q) * 120, 1)); sy.append(round(360 + math.sin(q) * 120, 1)); sa.append(round(math.degrees(q), 2))
    out = [(63, 'L20b_tri', dict(kb=kb, kr=kr, ka=ka, ed=0.15, hw=170 if ez else 125, th=26 if ez else 32, core=52, tw=63, ta=65, te=80))]
    out.append((64.5, 'L20b_shards', dict(sb=sb, sx=sx, sy=sy, sa=sa, v=560 if ez else 640, R=24, wn=0.75)))
    return out
both(tri)
fx(64, 0.18, 10, 1.2)

# 0:38.1 the build: the four buttons come in on the long climbing notes and go round you, faster and faster; on every kick the next one
# fires its own attack from where it is: the cross its beams, the circle, the square and the triangle their rings (each open on a side).
# 0:49 they wind in to the middle and meet there on the drop
KICKS = sorted(set([80, 82, 84, 85, 86, 87, 87.5, 88, 90, 92, 93, 94, 95, 95.5] + [96 + 0.5 * i for i in range(16)] + [104 + 0.5 * i for i in range(12)]))
def build(ez):
    cx, cy, rx, ry = 640, 360, 470, 250
    eb = [80, 81.75, 85.75, 89.75]; ph = [180, 0, 270, 90]
    sp = (28, 45, 70) if ez else (40, 62, 95)
    kb = [79, 96, 104, 111.5]; ka = [0, 17 * sp[0], 17 * sp[0] + 8 * sp[1], 17 * sp[0] + 8 * sp[1] + 7.5 * sp[2]]; ks = [1, 1, 1, 0.0]
    def at(i, b):
        a = math.radians(lerpk(kb, ka, b) + ph[i]); s = lerpk(kb, ks, b); return cx + math.cos(a) * rx * s, cy + math.sin(a) * ry * s
    out = [(78.5, 'L20b_orbit', dict(rx=rx, ry=ry, kb=kb, ka=ka, ks=ks, eb=eb, ph=ph, R=46, te=111.75))]
    xb, xx, xy, xa = [], [], [], []
    turn = 0
    kicks = [k for k in KICKS if k < 109.5]
    if ez: kicks = [k for i, k in enumerate(kicks) if k < 96 or i % 2 == 0]
    for n, k in enumerate(kicks):
        live = [i for i in range(4) if eb[i] <= k - 0.5]
        if not live: continue
        i = live[turn % len(live)]; turn += 1
        x, y = at(i, k); toc = dg(cx - x, cy - y); off = [40, -40, 0, 75][(turn // 4) % 4]
        if i == 0:
            xb.append(k); xx.append(round(x, 1)); xy.append(round(y, 1)); xa.append(round(lerpk(kb, ka, k) + ph[0] + 45, 2))
        else:
            out.append(ring(k, x, y, [0, 0, 4, 3][i], toc + off, 80 if ez else 56, 500 if ez else 640, rot=lerpk(kb, ka, k) + ph[i], r0=40, wn=0.75 if ez else 0.6))
    out.append((79, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=30 if ez else 40, wn=1.0 if ez else 0.75, lh=0.25)))
    return out
both(build)
fx(80, 0.1, 8, 1.0); fx(96, 0.08, 6, 0.8); fx(104, 0.1, 8, 0.8)

# ================= part 2: playing (112-175) =================
# 0:53.1 the drop - the D-pad: it turns a notch (22.5 degrees) on every stab of the tune, clockwise for a higher note, back for a lower;
# on the big hits it is PRESSED and its arms reach the screen's edges, so be in a quarter's middle. Every 2 beats (Easy 4) a ring comes
# out of its hub, open in one quarter; the open quarter goes on a quarter every 4 beats (Easy at each press). 1:04.3 the fill: it spins round half a turn
def dpad(ez):
    a = 0; kb, ka = [110.5], [0]; prev = 'F#'; d = 1
    for i, (b, pc) in enumerate(N_DROP):
        if b >= 136: break
        d = sgn(prev, pc, d); prev = pc
        if ez and i % 2: continue
        a += 22.5 * d; kb.append(b); ka.append(a)
    for b in [136, 136.75, 137.5, 138.25, 139]: a += 45; kb.append(b); ka.append(a)
    pb = [115, 123, 131] if ez else [115, 119, 123, 127, 131]
    out = [(110, 'L20b_pad', dict(hb=64, aw=48 if ez else 70, al=320, kb=kb, ka=ka, ed=0.08, pb=pb, pl=1500, pw=90 if ez else 110, pd=0.6, tw=110.5, ta=112, te=140))]
    q = 0
    for k, b in enumerate(range(114, 136, 4 if ez else 2)):
        q = k // 2 if not ez else sum(1 for p in pb if p <= b)
        pa = ka[max(i for i in range(len(kb)) if kb[i] <= b)]
        out.append(ring(b, 640, 360, 0, pa + 45 + 90 * q + (20 if k % 2 else -20), 85 if ez else 44, 500 if ez else 720, r0=60))
    return out
both(dpad)
fx(112, 0.22, 14, 1.6); fx(128, 0.08, 6, 0.6)

# 1:05.6 the break, the tune alone: the square tiles in diagonal stripes on its notes, a cell on each time
def brk(ez):
    tb, tn, tc = [], [], []
    for k, b in enumerate([140, 141.5, 142.5] if ez else [140, 140.75, 141.5, 142.25, 143]):
        cells = [c + 8 * r for r in range(5) for c in range(8) if (c + r + k) % (4 if ez else 3) == 0]
        tb.append(b); tn.append(len(cells)); tc += cells
    return [(139, 'L20b_tiles', dict(cols=8, cw=160, ch=144, tb=tb, tn=tn, tc=tc, wn=1.0 if ez else 0.75, lh=0.45))]
both(brk)

# 1:08.1 the two sticks: each note of the tune is a beam out of the sticks (the second stick opposite through the middle, aiming the
# other way); a higher note aims 45 degrees clockwise, a lower one back; a long note's beam sweeps on 90 degrees. The bass hits click the
# sticks (Normal every 2 beats): a ring out of each, both open towards the same place (top, bottom, left, right; Easy top, bottom). 1:15.6 the sticks turn round the middle a quarter
def sticks(ez):
    kb, kx, ky = [142.5, 156], [380, 380], [360, 360]
    for i in range(1, 9):
        q = math.pi + i / 8 * math.pi / 2; kb.append(156 + i); kx.append(round(640 + math.cos(q) * 260, 1)); ky.append(round(360 + math.sin(q) * 260, 1))
    kb.append(169); kx.append(kx[-1]); ky.append(ky[-1])
    mel = list(N_DROP2)
    if ez:
        keep = []; lastb = -9
        for i, (b, pc) in enumerate(mel):
            if b - lastb >= 0.7: keep.append((b, pc)); lastb = b
        mel = keep
    fb, fa0, fa1, fl = [], [], [], []; a = 0; prev = 'F#'; d = 1
    for i, (b, pc) in enumerate(mel):
        d = sgn(prev, pc, d); prev = pc; a += 45 * d
        nxt = mel[i + 1][0] if i + 1 < len(mel) else 169
        if nxt - b >= 1.5: fb.append(b); fa0.append(a); a += 90 * d; fa1.append(a); fl.append(round(min(1.25, nxt - b - 0.6), 3))
        else: fb.append(b); fa0.append(a); fa1.append(a + (0 if ez else 25 * d)); fl.append(0.4 if ez else 0.45); a = fa1[-1]
    out = [(142, 'L20b_sticks', dict(kb=kb, kx=kx, ky=ky, fb=fb, fa0=fa0, fa1=fa1, fl=fl, bw=40 if ez else 56, wn=0.9 if ez else 0.65, tw=142.5, ta=144, te=169))]
    clicks = [147, 155, 163] if ez else [147, 149, 151, 153, 155, 157, 159, 161, 163, 165, 167]
    for k, b in enumerate(clicks):
        tx, ty = [(640, 30), (640, 690), (60, 360), (1220, 360)][k % 4] if not ez else [(640, 30), (640, 690)][k % 2]
        x0, y0 = lerpk(kb, kx, b), lerpk(kb, ky, b)
        for x, y in ((x0, y0), (W - x0, H - y0)):
            out.append(ring(b, x, y, 0, dg(tx - x, ty - y), 80 if ez else 44, 500 if ez else 640, r0=60, wn=0.75))
    return out
both(sticks)
fx(144, 0.12, 8, 1.0)

# 1:20.2 the near silence: the screen's four quarters are four buttons pressed on its last hits; only one stays free each time (Easy two)
def corners(ez):
    cs = [171, 172, 173, 174, 175]
    free = [0, 1, 3, 2, 0]
    tb, tn, tc = [], [], []
    for b, f in zip(cs, free):
        cells = [c for c in range(4) if c != f and not (ez and c == [1, 3, 2, 0][f])]
        tb.append(b); tn.append(len(cells)); tc += cells
    return [(169, 'L20b_tiles', dict(cols=2, cw=640, ch=360, tb=tb, tn=tn, tc=tc, wn=1.0 if ez else 0.75, lh=0.45))]
both(corners)

# ================= part 3: the boss (176-242) =================
# The controller itself comes down: the same plan as the old boss (its sprays and aimed laser, the tube, the flip, then everything at you
# while it circles the arena, its end and NEW GAME), but every attack is one of the controller's own: the buttons, the D-pad, the tiles
ev(174, 'L20b_boss', **{'in': 174.5, 'on': 176, 'ex': 237})
fx(176, 0.2, 14, 1.6)
# 1:23.1 the grips spray the four buttons in mirrored fans; its laser aims at you, fires a beat later and sweeps; on the bass hits (every
# 4 beats from 179) a circle ring comes out of it, open to the left or the right in turn
def boss1(ez):
    out = [(176, 'L20b_spray', dict(ex=[440, 840], ey=[120, 120], ta=178, te=192, ev=0.75 if ez else 0.375, fan=2 if ez else 3, spread=16 if ez else 11, amp=60, per=8, sp=320 if ez else 420, R=13 if ez else 17))]
    for i, ba in enumerate([180, 184, 188]):
        out.append((ba - 1.5, 'L20beam', dict(x=640, y=86, ba=ba, bf=ba + (1.0 if ez else 0.75), be=ba + 2, w=64 if ez else 84, swp=(25 if ez else 40) * (1 if i % 2 == 0 else -1), rnd=True)))
    for k, b in enumerate([179, 183, 187, 191] if not ez else [179, 187]):
        out.append(ring(b, 640, 90, 0, 90 + (55 if k % 2 else -55), 80 if ez else 60, 480 if ez else 560, r0=60, wn=0.75))
    return out
both(boss1)
# 1:30.6 out of the middle (where the old tube was), the same three steps: crosses (the cross turning a notch every half beat), two spiral
# arms of triangle shards, then a wall: circle rings every half beat, open in one place that moves a notch a beat (against the turn, then with it)
def middle(ez):
    out = []; xb, xx, xy, xa = [], [], [], []
    for i in range(8):
        b = 192 + 0.5 * i
        if ez and i % 2: continue
        xb.append(b); xx.append(640); xy.append(400); xa.append(22.5 * i)
    out.append((191, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=38 if ez else 52, wn=1.0 if ez else 0.75, lh=0.4)))
    sb, sx, sy, sa = [], [], [], []
    step = 0.5 if ez else 0.25
    for i in range(int(4 / step)):
        b = 196 + step * i
        for arm in ([0] if ez else [0, 180]):
            a = arm + 28 * i; sb.append(b); sx.append(round(640 + math.cos(math.radians(a)) * 50, 1)); sy.append(round(400 + math.sin(math.radians(a)) * 50, 1)); sa.append(a)
    out.append((195, 'L20b_shards', dict(sb=sb, sx=sx, sy=sy, sa=sa, v=420 if ez else 520, R=24, wn=0.6)))
    for i in range(10):
        b = 200 + 0.5 * i
        if ez and i % 2: continue
        ga = -90 - 30 * (i // 2) if b < 202.5 else -90 - 60 + 30 * ((i - 5) // 2)
        out.append(ring(b, 640, 400, 0, ga, 84 if ez else 70, 500 if ez else 600, th=26, r0=50, wn=0.6))
    return out
both(middle)
# 1:36.2 the flip: the controller turns the arena over (it goes round to the bottom); a square ring and a triangle ring come out of the middle
def flip(ez):
    return [ring(205.5, 640, 360, 4, 90, 80 if ez else 60, 560 if ez else 650, rot=45, r0=40, wn=1.5),
            ring(206.5, 640, 360, 3, -60, 80 if ez else 60, 560 if ez else 650, rot=-90, r0=40, wn=1.5)]
both(flip)
fx(208, 0.2, 12, 1.4)
# 1:38.1 the controller in a rage circles the arena and throws every attack of the level at you, in order: the crosses, the button rings,
# the tiles, the D-pad, then a mix. All in its frame (u across, v away from it), so "down" is always away from the controller
def rage(ez):
    out = []; F = dict(frame=True)
    # 208-213.75 crosses in front of it, left and right in turn, turning
    xb, xx, xy, xa = [], [], [], []
    for i in range(12):
        b = 208.5 + 0.5 * i
        if ez and i % 2: continue
        xb.append(b); xx.append(-260 if i % 2 == 0 else 260); xy.append(300 + 60 * (i % 3)); xa.append(45 + 15 * i)
    out.append((207.5, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=36 if ez else 48, wn=1.0 if ez else 0.75, lh=0.35, **F)))
    # 214-219.75 the button rings out of it (circle, square, triangle), open to one side then the other
    for i in range(12):
        b = 214 + 0.5 * i
        if ez and i % 2: continue
        b0, k, kw = ring(b, 0, 40, [0, 4, 3][i % 3], 90 + (40 if (i // 2) % 2 else -40), 80 if ez else 56, 480 if ez else 560, rot=90 if i % 3 == 2 else 45, r0=60, wn=0.6)
        out.append((b0, k, dict(kw, **F)))
    # 220-225.75 the tiles in front of it, diagonal stripes that move on a cell every beat
    tb, tn, tc = [], [], []
    for k, b in enumerate(range(220, 226, 2 if ez else 1)):
        cells = [c + 8 * r for r in range(5) for c in range(8) if (c + r + k) % (4 if ez else 3) == 0]
        tb.append(b); tn.append(len(cells)); tc += cells
    out.append((219, 'L20b_tiles', dict(cols=8, cw=160, ch=144, tb=tb, tn=tn, tc=tc, wn=1.0 if ez else 0.75, lh=0.45, ox=-640, oy=60, **F)))
    # 226-231.75 its D-pad out in front: a notch a beat (the way swaps every 2), pressed on 228 and 230
    kb, ka = [225.5], [0]; a = 0
    for i, b in enumerate(range(226, 232)): a += 30 * (1 if (i // 2) % 2 == 0 else -1); kb.append(b); ka.append(a)
    out.append((225, 'L20b_pad', dict(x=0, y=360, hb=56, aw=44 if ez else 56, al=280, kb=kb, ka=ka, ed=0.1, pb=[229] if ez else [228, 230], pl=1500, pw=90 if ez else 104, pd=0.6, tw=225.5, ta=226, te=232, **F)))
    # 232-236 the mix: crosses and button rings in turn
    xb, xx, xy, xa = [], [], [], []
    for i in range(8):
        b = 232 + 0.5 * i
        if ez and i % 2: continue
        if i % 2 == 0: xb.append(b); xx.append([-200, 200, 0, -300][(i // 2) % 4]); xy.append(320); xa.append(30 * i)
        else:
            b0, k, kw = ring(b, 0, 40, [4, 3, 0, 4][(i // 2) % 4], 90 + (45 if (i // 2) % 2 else -45), 56, 560, rot=45, r0=60, wn=0.6)
            out.append((b0, k, dict(kw, **F)))
    out.append((231, 'L20b_cross', dict(xb=xb, xx=xx, xy=xy, xa=xa, w=36 if ez else 48, wn=1.0 if ez else 0.75, lh=0.35, **F)))
    return out
both(rage)
# 1:51.2 its end: it cracks, bursts at 238, and three rings come out of it, a circle, a square and a triangle, open down-left, down-right, down
def boom(ez):
    return [ring(b, 640, 80, sides, ga, 75 if ez else 52, 1050 if ez else 1150, th=24, rot=rot, r0=30, wn=0.75)
            for b, ga, sides, rot in [(238, 115, 0, 0), (238.5, 70, 4, 45), (239, 95, 3, -90)]]
both(boom)
fx(238, 0.25, 14, 1.4)
fx(240, 0.4, 12, 1.5); ev(240, 'L20end', ta=240, te=242)

EV.sort(key=lambda e: e['b'])
energy = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L20', 'name': 'المرحلة العشرون', 'song': 'New Game', 'file': 'SONG_L20', 'bpm': 128, 'off': 0.59075, 'end': 242, 'cps': [0, 112, 176],
     'ez': {'ezg': 0, 'ezc': 0, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.15}, 'energy': energy, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), '->', OUT)
