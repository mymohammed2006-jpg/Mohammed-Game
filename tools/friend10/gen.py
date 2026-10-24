#!/usr/bin/env python3
# Makes data/dodge_F10.json: F10 "Walk" (Kwabs), Ghassan's level. 144 BPM, beat 0 = 1.902 s, a beat = 0.41667 s.
# Song map (beats): verse 1 0-100 | chorus 1 100-164 (0:43.6) | verse 2 164-244 (1:10.2) | chorus 2 244-308 (1:43.6)
# | bridge 308-324 (2:10.2) | build 324-340 (2:16.9) | the voice alone 340-374 (2:23.6) | win 374 (2:37.7, the last chorus comes in)
# Parts: 1 the first steps (0-100), 2 the long road (100-244), 3 against the wind (244-374). Checkpoints 0, 100, 244.
# Nothing here is random: every attack is a fixed pattern on the beat (the shadow follows the square, so it is "rnd" for the bot).
# An event fires at its start minus EZW times its warning (Easy shows the warning longer).
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

# ---------- the walker's steps: it turns round its front foot towards the next target ----------
def rot(x, y, cx, cy, q):
    c, s = math.cos(q), math.sin(q); dx, dy = x - cx, y - cy
    return cx + dx * c - dy * s, cy + dx * s + dy * c

def walk(n, R, hub, ang, sbs, targets, opts, inside, lean=None, alt=False, near=1.4):
    """sbs: the step beats; targets: points to walk to in turn; opts: turns (radians) to choose from;
    inside(x, y): a foot may land there; lean: a point the sweep should face (the scissors), alt: change the swept side each step"""
    hx, hy = hub; ti = 0; last = 0; lastm = None
    st = {'sb': [], 'px': [], 'py': [], 'hx': [], 'hy': [], 'ang': [], 'da': []}
    bestd, stall = 1e9, 0
    for sb in sbs:
        T = targets[ti % len(targets)]; dd = math.hypot(T[0] - hx, T[1] - hy)
        if dd < bestd - 1: bestd, stall = dd, 0
        else: stall += 1
        if dd < R * near or stall >= 2: ti += 1; T = targets[ti % len(targets)]; bestd, stall = 1e9, 0
        feet = [(hx + R * math.cos(ang + 2 * math.pi * j / n), hy + R * math.sin(ang + 2 * math.pi * j / n)) for j in range(n)]
        # the pivot: the foot most towards the target
        pj = max(range(n), key=lambda j: (feet[j][0] - hx) * (T[0] - hx) + (feet[j][1] - hy) * (T[1] - hy))
        px, py = feet[pj]
        best = None
        for q in opts + [a * D for a in (90, -90, 60, -60, 30, -30)]:
            if best is not None and q not in opts: break
            nhx, nhy = rot(hx, hy, px, py, q)
            nf = [rot(f[0], f[1], px, py, q) for f in feet]
            if not all(inside(x, y) for x, y in nf + [(nhx, nhy)]): continue
            sc = math.hypot(T[0] - nhx, T[1] - nhy)
            if lean is not None:
                mx, my = rot(hx, hy, px, py, q / 2); sc += 0.6 * math.hypot(lean[0] - mx, lean[1] - my)
            mx, my = rot(hx, hy, px, py, q / 2)
            if alt and lastm is not None and math.hypot(mx - lastm[0], my - lastm[1]) < R * 1.2: sc += R * 0.8
            if best is None or sc < best[0]: best = (sc, q, nhx, nhy)
        if best is None: raise SystemExit(f'walker stuck at beat {sb}')
        _, q, nhx, nhy = best; lastm = rot(hx, hy, px, py, q / 2)
        for k, v in (('sb', sb), ('px', px), ('py', py), ('hx', hx), ('hy', hy), ('ang', ang), ('da', q)): st[k].append(v)
        hx, hy, ang, last = nhx, nhy, ang + q, q
    return st

def inbox(m): return lambda x, y: m <= x <= W - m and m <= y <= H - m
def indisc(r): return lambda x, y: math.hypot(x - W / 2, y - H / 2) <= r
D = math.pi / 180

# ================= part 1: the first steps (0-100) =================
ev(0, 'F10stage', part=1, to=100, v=0, keep=True)

# (Easy: no middle one and no edge ones)
# 0:02 the compasses: three stand in the three thirds of the street on one foot and turn over, all together every 4 beats, to the left
# then to the right (each sweeps half a circle); then two walk across, along the top and the bottom, a turn every 2 beats round the
# front foot, the swept side changing each step
OPT = [180 * D, -180 * D, 150 * D, -150 * D, 120 * D, -120 * D]
def wiper(x0, R, sbs):
    st = {'sb': [], 'px': [], 'py': [], 'hx': [], 'hy': [], 'ang': [], 'da': []}; hx, hy, ang = x0, 360 + R, math.pi / 2
    for sb in sbs:
        for k, v in (('sb', sb), ('px', x0), ('py', 360), ('hx', hx), ('hy', hy), ('ang', ang), ('da', math.pi)): st[k].append(v)
        hx, hy = rot(hx, hy, x0, 360, math.pi); ang += math.pi
    return st
for x0 in (213, 640, 1067):
    ev(1.0, 'F10walk', n=2, R=150, w=14, sw=0.8, wn=2.0, tin=2.5, to=20.5, ezskip=(x0 == 640), **wiper(x0, 150, [4, 8, 12, 16, 20]))
# and two small ones walk along the top and bottom edges, sweeping only the edge side
for y0, b0, xa, lean in ((90, 6, 120, (640, -900)), (630, 7, 1160, (640, 1620))):
    st = walk(2, 110, (xa, y0), 0.0, [b0 + 2 * i for i in range(7)], [(1280 - xa, y0), (xa, y0)], [180 * D, -180 * D], lambda x, y: -60 <= x <= W + 60, lean=lean, near=0.7)
    ev(b0 - 2.4, 'F10walk', n=2, R=110, w=14, sw=0.6, wn=1.0, tin=b0 - 1, to=20.5, ezskip=True, **st)
for y0, b0, xa in ((140, 20, 180), (580, 21, 1100)):
    st = walk(2, 130, (xa, y0), 0.0, [b0 + 2 * i for i in range(8)], [(1280 - xa, y0), (xa, y0)], OPT, inbox(10), alt=True, near=0.7)
    ev(b0 - 2.4, 'F10walk', n=2, R=130, w=16, sw=0.6, wn=1.0, tin=b0 - 1, to=36, **st)

# 0:16.9 the crossing: walk on the beat. All the stripes but the safe ones are pink on each beat.
def mask(n, safe): return sum(1 << i for i in range(n) if i not in safe)
def zebra(b0, horiz, n, beats, safes, ezsafes, on=0.45, wn=1.0, to=None):
    ev(b0 - 1.5, 'F10zebra', horiz=horiz, n=n, hb=beats, mk=[mask(n, s) for s in safes], ezmk=[mask(n, s) for s in ezsafes], on=on, wn=wn, to=to or beats[-1] + 1)
def walkpos(start, n, steps, w, flip=4):
    # a safe group w wide moving one stripe a beat, back the other way every flip beats
    p, d, out = start, 1, []
    for i in range(steps):
        out.append(p)
        if (i + 1) % flip == 0: d = -d
        if not (0 <= p + d <= n - w): d = -d
        p += d
    return out
def grp(p, w): return set(range(p, p + w))
def ezgrp(p, w, n): s = max(0, min(n - w - 2, p - 1)); return set(range(s, s + w + 2))
# 0:16.9 one safe pair, walking
P = walkpos(1, 10, 8, 2, 4)
zebra(36, False, 10, [36 + i for i in range(8)], [grp(p, 2) for p in P], [ezgrp(p, 2, 10) for p in P])
# 0:20.2 two pairs from the sides in, crossing and out again
A = [0, 1, 2, 3, 4, 5, 6, 7]; Bq = [8, 7, 6, 5, 4, 3, 2, 1]
zebra(44, False, 10, [44 + i for i in range(8)], [grp(a, 2) | grp(b, 2) for a, b in zip(A, Bq)], [grp(max(0, a - 1), 4) | grp(min(6, b - 1), 4) for a, b in zip(A, Bq)])
# 0:23.6 the stripes lie down: rows, a safe pair walking up and down
P = walkpos(4, 6, 8, 2, 3)
zebra(52, True, 6, [52 + i for i in range(8)], [grp(p, 2) for p in P], [ezgrp(p, 2, 6) for p in P])
# 0:26.9 both ways at once, one each beat: the safe square walks on the diagonal
PV = walkpos(0, 10, 4, 3, 99); PH = walkpos(0, 6, 4, 2, 99)
zebra(60, False, 10, [60 + 2 * i for i in range(4)], [grp(p, 3) for p in PV], [grp(max(0, p - 1), 5) for p in PV], wn=1.5, to=68)
zebra(61, True, 6, [61 + 2 * i for i in range(4)], [grp(p, 2) for p in PH], [grp(max(0, min(2, p - 1)), 4) for p in PH], wn=1.5, to=68)
# 0:30.2 the compass walks over the crossing: every other stripe every 4 beats, the compass on the even beats
C3 = walk(2, 125, (640, 360), math.pi / 4, [70 + 2 * i for i in range(15)], [(1000, 250), (980, 500), (300, 500), (280, 240)],
          [180 * D, -180 * D, 150 * D, -150 * D, 120 * D, -120 * D], inbox(40), alt=True)
ev(67.5, 'F10walk', n=2, R=125, w=16, sw=0.6, wn=1.0, tin=69, to=100, **C3)
odd = [i for i in range(10) if i % 2]; even = [i for i in range(10) if i % 2 == 0]
B1 = [69 + 4 * i for i in range(6)]
zebra(69, False, 10, B1, [set(odd) if i % 2 == 0 else set(even) for i in range(6)], [set(range(10)) - {1, 5} if i % 2 == 0 else set(range(10)) - {3, 7} for i in range(6)], wn=1.5, to=93)
# 0:40.2 the walk again, every beat, to the chorus
P = walkpos(3, 10, 8, 3, 4)
zebra(92, False, 10, [92 + i for i in range(8)], [grp(p, 3) for p in P], [ezgrp(p, 3, 10) for p in P])

# ================= part 2: the long road (100-244) =================
ev(100, 'F10stage', part=2, to=164, v=170, keep=True)
ev(100, 'F10fx', v=0.35, s=9, dur=1.2)
# 0:43.6 the barriers come down the road; their gaps walk on the slant
V = 170
def row(b0, gx, gw=200, th=26, wn=1.0, **kw): ev(b0 - wn * EZW - 0.05, 'F10row', b0=b0, v=V, th=th, gx=gx, gw=gw, wn=wn, **kw)
x, d = 640, 1
for i in range(32):
    row(101 + i, [x], ezskip=(i % 2 == 1))
    if (i + 1) % 6 == 0: d = -d
    if not (300 <= x + 100 * d <= 980): d = -d
    x += 100 * d
# 0:57 two gaps: they part to the sides and come back together
for i in range(16):
    k = i if i < 8 else 15 - i
    row(133 + i, [640 - 60 - 55 * k, 640 + 60 + 55 * k], gw=200, ezskip=(i % 2 == 1))
# 1:03.6 the barriers every beat and a half, the gap jumping across
for i in range(10):
    row(149 + 1.5 * i, [420 if i % 2 == 0 else 860], gw=210)
# the compasses at the sides walk down and up the road and cut in like scissors on the bars
def side(x0, lean):
    return walk(2, 120, (x0, 150), math.pi / 2, [108 + 4 * i for i in range(10)] + [148 + 2 * i for i in range(8)],
                [(x0, 600), (x0, 130)], [180 * D, -180 * D], inbox(20), lean=lean, near=1.2)
SL, SR = side(150, (640, 360)), side(W - 150, (640, 360))
ev(105.5, 'F10walk', n=2, R=120, w=14, sw=0.5, wn=1.2, tin=107, to=164, **SL)
ev(105.5, 'F10walk', n=2, R=120, w=14, sw=0.5, wn=1.2, tin=107, to=164, **SR)

# 1:10.2 the turning walls: a bar on every other crossing of a 160 px grid; every 2 beats each turns a quarter, column after column
ev(164, 'F10stage', part=2, to=244, v=0, keep=True)
PX, PY = [], []
for j in range(5):
    for i in range(9):
        if (i + j) % 2 == 0: PX.append(160 * i); PY.append(40 + 160 * j)
COL = [x // 160 for x in PX]
def turns(b0, b1, sbs, das, rtl, tin=None):
    # a quarter turn takes half a beat (shown 1.1 beats before), a half turn a whole beat (shown 1.6 before)
    dl = [round(0.1 * ((8 - c) if rtl else c), 3) for c in COL]
    sw = [0.5 if abs(d) <= 90 else 1.0 for d in das]; wn = [1.1 if abs(d) <= 90 else 1.6 for d in das]
    ev(b0 - 2.2, 'F10turns', x=PX, y=PY, a0=[0] * len(PX), dl=dl, L=148, w=12, sb=sbs, da=das, sw=sw, wn=wn, tin=tin or b0, to=b1)
# all of them turn together in one direction for 4 turns, then back for 4 (a whole round), the wave from the left then from the right
S1 = [168 + 2 * i for i in range(16)]
D1 = [90 if (i // 4) % 2 == 0 else -90 for i in range(16)]
turns(167, 201, S1, D1, False)
# 1:25.4 the second half: the wave from the right, a whole round the other way, back, and half again
S2 = [204 + 2 * i for i in range(10)]
D2 = [-90] * 4 + [90] * 4 + [-90] * 2
# 1:33.7 then half turns every 4 beats (each bar sweeps a half circle)
S3 = [224 + 4 * i for i in range(5)]
D3 = [-180] * 5
turns(203, 244, S2 + S3, D2 + D3, True)

# ================= part 3: against the wind (244-374) =================
ev(244, 'F10stage', part=3, to=308, v=0, keep=True)
ev(244, 'F10fx', v=0.35, s=10, dur=1.4)
# the room turns: 160 degrees one way, then back
ev(244, 'F10spin', kb=[244, 276, 280, 308, 309], ka=[0, 160, 150, 30, 30], keep=True)
# 1:43.6 the tripod rolls over its feet in the turning room
T1 = walk(3, 150, (640, 360), 0.0, [246 + 2 * i for i in range(31)], [(400, 250), (880, 250), (880, 470), (400, 470)],
          [120 * D, -120 * D], indisc(330), near=1.0)
ev(244.2, 'F10walk', n=3, R=150, w=16, sw=0.7, wn=1.1, tin=245, to=308, rot=True, **T1)
# with it at first the compass, the other way round
C4 = walk(2, 120, (640, 200), 0.0, [247 + 2 * i for i in range(7)], [(820, 500), (460, 500), (460, 220)],
          [180 * D, -180 * D, 150 * D, -150 * D], indisc(330), alt=True)
ev(245.3, 'F10walk', n=2, R=120, w=14, sw=0.6, wn=1.0, tin=246.5, to=261, rot=True, **C4)
# 1:47 the rain in lanes along the wind; the wind turns at 1:53.7 and 2:00.3
# (the wind also sways a little every 4 beats, so the lanes never stand still)
RB = [246, 250, 254, 258, 262, 266, 270, 274, 276, 277, 280, 284, 288, 292, 293, 296, 300, 304, 308]
RA = [90, 83, 97, 83, 97, 83, 97, 87, 90, 45, 52, 38, 52, 45, 135, 128, 142, 128, 135]
ev(244.2, 'F10rain', kb=RB, ka=RA, sp=135, sl=48, per=118, v=420, sw=5, wn=1.5, tin=246, to=308)

# 2:10.2 your shadow walks your path 1.5 s behind you, while the crossing makes you walk on the beat
ev(305, 'F10stage', part=1, to=340, v=0, keep=True)
ev(306, 'F10shadow', tw=306, ta=308, to=366, kb=[308, 324, 340, 366], kd=[1.5, 1.5, 0.9, 1.0], s=26, rnd=True)
P = walkpos(0, 10, 16, 3, 6)
zebra(308, False, 10, [308 + i for i in range(16)], [grp(p, 3) for p in P], [ezgrp(p, 3, 10) for p in P])
# 2:16.9 the build: the shadow comes closer (0.9 s), the compass steps every beat
C5 = walk(2, 125, (640, 360), 0.0, [325 + i for i in range(15)], [(1000, 200), (1000, 520), (280, 520), (280, 200)],
          [180 * D, -180 * D, 150 * D, -150 * D, 120 * D, -120 * D], inbox(40), alt=True)
ev(323, 'F10walk', n=2, R=125, w=16, sw=0.45, wn=0.8, tin=324, to=340, **C5)

# 2:23.6 the voice alone: the room closes in to a narrow winding road (it comes down 120 px a beat); the shadow still behind you
ev(340, 'F10stage', part=4, to=376, v=0, keep=True)
ev(338, 'F10road', kb=[340, 348, 360, 368, 373, 374], kw=[1500, 320, 280, 260, 700, 1500], ka=[0, 140, 180, 170, 60, 0], v=120, lam=1500, tin=340, to=374)

# ================= the file =================
EV.sort(key=lambda e: e['b'])
EN = json.load(open(os.path.join(HERE, 'energy.json')))
doc = {'id': 'F10', 'name': 'Walk', 'by': 'Ghassan', 'ver': 'Mohammed', 'file': 'SONG_F10', 'bpm': 144, 'off': 1.902, 'end': 374,
       'cps': [0, 100, 244], 'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': EN, 'events': EV}
json.dump(doc, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'bytes', os.path.getsize(OUT))
