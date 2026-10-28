#!/usr/bin/env python3
# Makes data/dodge_F10.json: F10 "Walk" (Kwabs), Ghassan's level, the second design (10-28). 144 BPM, beat 0 = 1.902 s, a beat = 0.41667 s.
# Song map (beats): intro 0-36 | verse 1 36-100 | chorus 1 100-164 (0:43.6) | verse 2 164-244 (1:10.2) | chorus 2 244-308 (1:43.6)
# | bridge 308-324 (2:10.2) | build 324-340 (2:16.9) | the voice alone 340-374 (2:23.6) | win 374 (2:37.7, the last chorus comes in)
# The groove (my own onset count per 16th, tools none): kick on 1 and on the "and" of 2 (beat x.5 after the clap), claps on beats 2 and 4
# (b = 1 and 3 in a bar of 4), so the steps land on the claps, every 2 beats (the song walks at half time).
# Parts: 1 the empty street (0-100), 2 the crowd (100-244), 3 the way home (244-374). Checkpoints 0, 100, 244 (as the first design).
# Nothing here is random: every attack is a fixed pattern on the beat.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_F10.json')
W, H = 1280, 720
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)

# ---------- a walk: prints along a path of straight legs ----------
def walk(pts, beats, stride, lat, start_side=-1):
    """pts: the path's corners; beats: the step beats; prints stride apart along the path, lat to the side (left/right in turn)"""
    segs = []; tot = 0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        L = math.hypot(x1 - x0, y1 - y0); segs.append((x0, y0, x1, y1, tot, L)); tot += L
    out = {'x': [], 'y': [], 'a': [], 'sd': [], 'bl': []}; sd = start_side
    for i, b in enumerate(beats):
        s = min(i * stride, tot - 1e-6)
        for (x0, y0, x1, y1, s0, L) in segs:
            if s <= s0 + L: break
        u = (s - s0) / L; a = math.atan2(y1 - y0, x1 - x0); px, py = x0 + (x1 - x0) * u, y0 + (y1 - y0) * u
        # the side: left foot to the left of the walk (screen y down: left of the way = (sin a, -cos a))
        out['x'].append(px + sd * -lat * math.sin(a)); out['y'].append(py + sd * lat * math.cos(a)); out['a'].append(a); out['sd'].append(sd); out['bl'].append(b)
        sd = -sd
    return out

def trail(b, st, S, wn, stay, rr=0, rw=10, rd=1.2, rg=0, ck=None, cko=0.5, cw=14, cd=0.7, cwn=1.5, **kw):
    n = len(st['x']); ckl = ck if ck is not None else [0] * n
    first = min(st['bl'])
    ev(first - wn * EZW - 0.05 if b is None else b, 'F10btrail', S=S, wn=wn, st=stay, rr=rr, rw=rw, rd=rd, rg=rg, ck=ckl, cko=cko, cw=cw, cd=cd, cwn=cwn, **st, **kw)

# ================= part 1: the empty street (0-100) =================
ev(0, 'F10bstage', part=1, to=100, keep=True)

# 0:02 the first footsteps: a walker comes in along the top and turns down, another along the bottom and turns up, a step every 2 beats
# (the second on the other beat), each print stamps a ring out
P1 = walk([(-60, 230), (1060, 230), (1060, 500), (-200, 500)], [2 + 2 * i for i in range(17)], 110, 34)
P2 = walk([(1340, 490), (220, 490), (220, 220), (1480, 220)], [3 + 2 * i for i in range(17)], 110, 34)
trail(None, P1, 112, 1.5, 5, rr=420, rw=12, rd=1.3, rg=0.9)
trail(None, P2, 112, 1.5, 5, rr=420, rw=12, rd=1.3, rg=0.9)
# 0:06 two more cross the middle on the diagonals, and back (Easy: not these)
P3 = walk([(-80, -60), (1360, 780), (1360, 60)], [10 + 2 * i for i in range(13)], 150, 34)
P4 = walk([(1360, -60), (-80, 780), (-80, 60)], [11 + 2 * i for i in range(13)], 150, 34)
trail(None, P3, 112, 1.5, 3, rr=420, rw=12, rd=1.3, rg=0.9, ezskip=True)
trail(None, P4, 112, 1.5, 3, rr=420, rw=12, rd=1.3, rg=0.9, ezskip=True)

# 0:16.9 the giant: big prints on the claps, and on the kick after every step the street cracks across his way, edge to edge
G1 = walk([(400, 840), (400, -120)], [37 + 2 * i for i in range(8)], 120, 62)
G1b = walk([(880, -120), (880, 840)], [53 + 2 * i for i in range(8)], 120, 62)
G1c = walk([(640, 840), (640, -120)], [69 + 2 * i for i in range(8)], 120, 62)
G1d = walk([(260, -120), (260, 840)], [85 + 2 * i for i in range(8)], 120, 62)
for G in (G1, G1b, G1c, G1d):
    trail(None, G, 190, 1.6, 2.5, rr=520, rw=14, rd=1.4, rg=0.8, ck=[1] * len(G['x']), cko=0.5, cw=16, cd=0.7, cwn=1.2)
# 0:23.6 a second giant across the street, on the other beats: his cracks go up and down (Easy: without them)
G2 = walk([(-120, 180), (1400, 180)], [54 + 2 * i for i in range(8)], 140, 62)
G2b = walk([(1400, 540), (-120, 540)], [70 + 2 * i for i in range(8)], 140, 62)
G2c = walk([(-120, 360), (1400, 360)], [86 + 2 * i for i in range(7)], 140, 62)
for G in (G2, G2b, G2c):
    trail(None, G, 190, 1.6, 2.5, rr=520, rw=14, rd=1.4, rg=0.8, ck=[1] * len(G['x']), cko=0.5, cw=16, cd=0.7, cwn=1.2, ezck=True)
# and the small walkers go on round the edges, between the giant's steps (Easy: not these)
P5 = walk([(-80, 640), (1180, 640), (1180, 80), (100, 80), (100, 640), (1400, 640)], [38 + 2 * i for i in range(31)], 120, 30)
P6 = walk([(1360, 80), (100, 80), (100, 640), (1180, 640), (1180, 80), (-120, 80)], [39 + 2 * i for i in range(31)], 120, 30)
trail(None, P5, 112, 1.5, 3, rr=420, rw=12, rd=1.3, rg=0.9, ezskip=True)
trail(None, P6, 112, 1.5, 3, rr=420, rw=12, rd=1.3, rg=0.9, ezskip=True)

# ================= part 2: the crowd (100-244) =================
ev(100, 'F10bstage', part=2, to=164, keep=True)
ev(100, 'F10bfx', v=0.35, s=9, dur=1.2)
# 0:43.6 the crowd: seven lanes of people on the road, every other lane the other way, all of them take a step on every beat;
# halfway (and again at 1:03.6) they all turn round
LC = [180 + 60 * k for k in range(7)]
LP = [300, 340, 280, 360, 320, 300, 340]
OF0 = [0, 120, 60, 200, 30, 170, 90]
lo, ln, ofs = [], [], []
for k in range(7):
    lo.append(len(ofs)); P = LP[k]; o = OF0[k]
    pts = [o, o + P * 0.5 + (20 if k % 2 else -20)]
    ofs += pts; ln.append(len(pts))
ev(100, 'F10bcrowd', ring=False, lc=LC, ld=[1 if k % 2 == 0 else -1 for k in range(7)], lp=LP, lo=lo, ln=ln, ofs=ofs,
   sb0=101, ev=1, sn=63, st=50, md=0.35, fb=[132, 148], fw=2, rb=28, tin=101, to=164)
# the buses on the pavements: top, then bottom, every 8 beats (so you cross the crowd); from 0:57 every 4
def bus(b0, top, dur=2.0, len_=420):
    y0, y1 = (14, 136) if top else (H - 136, H - 14)
    rgt = (int(b0) // 8) % 2 == 0
    xs, xe = (-20, W + len_ + 20) if rgt else (W + 20, -len_ - 20)
    ev(b0 - 2 * EZW - 0.05, 'F10bbus', y0=y0, y1=y1, xs=xs, xe=xe, len=len_, b0=b0, b1=b0 + dur, wn=2.0)
for i, b in enumerate(range(104, 132, 8)): bus(b, i % 2 == 0)
for i, b in enumerate(range(132, 162, 4)): bus(b, i % 2 == 0, dur=1.8)

# 1:10.2 the march: squares in rows with corridors between them, a step on every beat (80 px); in each corridor one square stands in the way
# and two holes in every wall let you through to the next corridor. Every 7 steps the whole block steps sideways once.
ev(164, 'F10bstage', part=3, to=244, keep=True)
ev(164, 'F10bfx', v=0.25, s=6, dur=1.0)
C = 80
def block(ncol, nrow, blk, con):
    """corridors on odd rows, walls on even rows; blk: {corridor row: blocker col}, con: {wall row: [hole cols]}"""
    rows = []
    for r in range(nrow):
        if r % 2 == 1: rows.append(''.join('#' if blk.get(r) == c else '.' for c in range(ncol)))
        else: rows.append(''.join('.' if c in con.get(r, []) else '#' for c in range(ncol)))
    return rows
def flip(rows): return [r[::-1] for r in rows]
def turn(rows): return [''.join(rows[r][c] for r in range(len(rows))) for c in range(len(rows[0]))]   # rows <-> columns
def moves(seq): return [m for k, n in seq for m in [k] * n]
def mv(seq): return [1 if m == 'R' else -1 if m == 'L' else 0 for m in seq], [1 if m == 'D' else -1 if m == 'U' else 0 for m in seq]
# 13 rows from 160 px above the screen: the screen's top and bottom rows are walls, so a step up or down never takes a corridor off it.
# Moving right the higher columns reach you first, so the way out of a corridor is a hole at a column above its blocker.
A = block(12, 13, {1: 8, 3: 2, 5: 8, 7: 2, 9: 8, 11: 2}, {2: [5, 10], 4: [5, 10], 6: [5, 10], 8: [5, 10], 10: [5, 10]})
MA = moves([('R', 7), ('D', 1), ('R', 7), ('U', 1), ('R', 7), ('D', 1), ('R', 7)])
mx, my = mv(MA)
ev(164, 'F10bmarch', mask='/'.join(A), c=C, x0=-12 * C, y0=-2 * C, mb=[165 + i for i in range(len(MA))], mx=mx, my=my, md=0.4, wn=1.4, sz=64, tin=164.5, to=197)
# 1:23.6 back from the right (the lower columns reach you first), the blockers in other corridors
Bm = flip(block(12, 13, {1: 2, 3: 8, 5: 2, 7: 8, 9: 2, 11: 8}, {2: [5, 10], 4: [5, 10], 6: [5, 10], 8: [5, 10], 10: [5, 10]}))
MB = moves([('L', 7), ('U', 1), ('L', 7), ('D', 1), ('L', 7), ('U', 1), ('L', 7)])
mx, my = mv(MB)
ev(196, 'F10bmarch', mask='/'.join(Bm), c=C, x0=W, y0=-2 * C, mb=[197 + i for i in range(len(MB))], mx=mx, my=my, md=0.4, wn=1.4, sz=64, tin=196.5, to=229)
# 1:37 from above: the corridors stand up (columns), the block steps down, once right and once back left
# (19 columns from 80 px left of the screen: the right edge is a wall before the step right, the left edge after it)
Cm = turn(block(7, 19, {1: 4, 3: 1, 5: 4, 7: 1, 9: 4, 11: 1, 13: 4, 15: 1, 17: 4}, {2: [3, 6], 4: [3, 6], 6: [3, 6], 8: [3, 6], 10: [3, 6], 12: [3, 6], 14: [3, 6], 16: [3, 6]}))
MC = moves([('D', 4), ('R', 1), ('D', 5), ('L', 1), ('D', 4)])
mx, my = mv(MC)
ev(228, 'F10bmarch', mask='/'.join(Cm), c=C, x0=-C, y0=-7 * C, mb=[229 + i for i in range(len(MC))], mx=mx, my=my, md=0.4, wn=1.4, sz=64, tin=228.5, to=244)

# ================= part 3: the way home (244-374) =================
ev(244, 'F10bstage', part=4, to=340, keep=True)
ev(244, 'F10bfx', v=0.35, s=10, dur=1.4)
# 1:43.6 the roundabout: rings of people round the middle, every other ring the other way, a step on every beat; at 2:00.3 they turn round
RR = [120 + 60 * k for k in range(11)]
lo, ln, ofs = [], [], []
for k, R in enumerate(RR):
    circ = 2 * math.pi * R; m = max(2, round(circ / 320)); sp = circ / m
    lo.append(len(ofs)); ln.append(m)
    ofs += [j * sp + (k * 47) % sp for j in range(m)]
ev(244, 'F10bcrowd', ring=True, lc=RR, ld=[1 if k % 2 == 0 else -1 for k in range(len(RR))], lp=[2 * math.pi * R for R in RR], lo=lo, ln=ln, ofs=ofs,
   sb0=246, ev=1, sn=62, st=40, md=0.35, fb=[276], fw=2, rb=28, tin=245, to=308)
# and from the middle a wave every 8 beats, with two holes that turn round
k = 0
for b in list(range(252, 308, 8)):
    base = (k * 50) * math.pi / 180; k += 1
    ev(b - 1.6 * EZW - 0.05, 'F10bwave', b0=b, v=200, w=14, ga=[base, base + math.pi], gw=0.85, wn=1.6)

# 2:10.2 the footsteps go round you into the middle (two walkers, one each side); the street cracks under every other step.
# At the build (2:16.9) they step on every beat.
def spiral(a0, beats, r0, r1, stride):
    out = {'x': [], 'y': [], 'a': [], 'sd': [], 'bl': []}; a = a0; n = len(beats); sd = -1
    for i, b in enumerate(beats):
        R = r0 + (r1 - r0) * i / max(1, n - 1); x, y = W / 2 + R * math.cos(a), H / 2 + R * math.sin(a); wa = a + math.pi / 2   # walking round, clockwise on screen
        out['x'].append(x + sd * -28 * math.sin(wa)); out['y'].append(y + sd * 28 * math.cos(wa)); out['a'].append(wa); out['sd'].append(sd); out['bl'].append(b)
        sd = -sd; a += stride / R
    return out
SB = [309 + 2 * i for i in range(8)] + [325 + i for i in range(13)]
for j in range(2):
    S1 = spiral(j * math.pi, SB, 470, 150, 125)
    trail(None, S1, 116, 1.4, 2.2, rr=170, rw=10, rd=1.0, ck=[1 if (i + j) % 2 == 0 else 0 for i in range(len(SB))], cko=0.5, cw=14, cd=0.6, cwn=1.4, ezck=(j == 1))
ev(308, 'F10bfx', v=0.2, s=5, dur=0.8)

# 2:23.6 the voice alone: the street turns pink from the edges; only the middle (closing) and the footsteps are safe, and from 2:25.7
# only one walk from the middle to the win, two steps wide. A step of the walk is safe from 3 beats before its own beat to 6 after.
ev(340, 'F10bstage', part=5, to=376, keep=True)
FX, FY, FA, FS, BS, BE = [], [], [], [], [], []
# the steps of four walks that come in from the corners (safe until the middle closes)
for (sx, sy) in ((150, 110), (1130, 110), (1130, 610), (150, 610)):
    d = math.hypot(W / 2 - sx, H / 2 - sy)
    w = walk([(sx, sy), (W / 2, H / 2)], [338.5, 339.5, 340.5, 341.5, 342.5], (d - 160) / 4, 24)
    for i in range(len(w['x'])):
        FX.append(w['x'][i]); FY.append(w['y'][i]); FA.append(w['a'][i]); FS.append(w['sd'][i]); BS.append(w['bl'][i]); BE.append(344.5 + 0.25 * i)
# then one way from the middle: up, round to the right, down and to the left, and the last step on the drop
WAY = [(W / 2 - 52, H / 2), (W / 2 - 52, 150), (980, 150), (980, 560), (240, 560), (240, 300)]
w = walk(WAY, [346 + 2 * i for i in range(14)], 85, 12, start_side=1)
# and you don't walk it alone: a friend's steps beside yours (on the right), so the way is two steps wide
for i in range(len(w['x'])):
    for twin in (0, 1):
        a = w['a'][i]; ox, oy = (-105 * math.sin(a), 105 * math.cos(a)) if twin else (0, 0)
        FX.append(w['x'][i] + ox); FY.append(w['y'][i] + oy); FA.append(a); FS.append(-w['sd'][i] if twin else w['sd'][i]); BS.append(w['bl'][i] - 3); BE.append(w['bl'][i] + 6)
BE[-1] = 380
ev(336, 'F10bfloor', x=FX, y=FY, a=FA, sd=FS, bs=BS, be=BE, S=270, wn=2.0, fon=342, fw=3, cr0=420, cb1=346.5, to=376)

# ================= the file =================
EV.sort(key=lambda e: e['b'])
EN = json.load(open(os.path.join(HERE, 'energy.json')))
doc = {'id': 'F10', 'name': 'Walk', 'by': 'Ghassan', 'ver': 'Mohammed', 'file': 'SONG_F10', 'bpm': 144, 'off': 1.902, 'end': 374,
       'cps': [0, 100, 244], 'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': EN, 'events': EV}
json.dump(doc, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'bytes', os.path.getsize(OUT))
