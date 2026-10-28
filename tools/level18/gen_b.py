#!/usr/bin/env python3
# Makes data/dodge_L18.json: L18 "اعوف الدنيا" (Mohamed Alsalim), the second design (v52; the first is data/dodge_L18_v51.json from gen.py,
# kept for old replays). Same song, start, end, checkpoints and ids as before. Kinds in src/Game/Dodge/L18b.cs.
# The strings that hold me to the world: part 1 they sound, part 2 I cut them, part 3 only the red thread to you is left.
# 102 BPM, beat 0 = song 0:50.15, a beat = 0.58825 s; the song's three hits in a beat land at 0, 0.43 and 0.74 of it.
# Song map (beats): verse 0-23 (0:50) | verse b 24-47 (1:04) | chorus 48-79 (1:18) | chorus b 80-114 (1:37) | quiet break 115-127 (1:58)
# | verse 2 128-162 (2:05) | the soft part 163-198 (2:26) | the build 199-223 (2:47) | last chorus 224-255 (3:01) | its second half 256-287 (3:20)
# | the end 288 (3:39.5), win 289 (3:40.2). Checkpoints 0, 115, 224. Nothing is random or aimed.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L18.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3          # Easy's longest warning stretch: an event fires this much earlier than its Normal warning
HITS = (0, 0.43, 0.74)
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def stage(part, b0, b1): ev(max(0, b0 - 4), 'L18bstage', part=part, fr=b0, to=b1, keep=True)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L18bfx', v=v, s=s, dur=dur)
def strings(tw, to, wn, **kw): ev(tw - 0.5, 'L18bstr', tw=tw, to=to, wn=wn, **kw)
def song(b): s = 50.15 + 0.58825 * b; return '%d:%04.1f' % (s // 60, s % 60)

# ================= part 1: the strings sound (0-114) =================
stage(1, 0, 115)
fx(0, 0.15, 5, 1.0)

# 0:50 eleven strings stand across the screen (grey, harmless). On the three hits of a beat three of them are plucked, far apart (a wave
# across the screen); a plucked string shakes inside a pink lens that fills most of the way to its neighbours and dies away over a beat.
# Every beat the wave moves one string on; every 4 beats it turns back. Walk with the gap
V = [-580 + 116 * i for i in range(11)]
pi, pb, pa = [], [], []
pos, d = 0, 1
for b in range(3, 24):
    for j, f in enumerate(HITS):
        i = pos + 4 * j
        if i > 10: i -= 11
        pi.append(i); pb.append(b + f); pa.append(90 if b < 12 else 96)
    if b >= 8 and b % 2 == 0:   # 0:54.9 the low note: the middle of the gap shakes too, wide
        g = (pos + 2) % 11; pi.append(g); pb.append(b + 0.74); pa.append(110)
    if (b - 1) % 4 == 0: d = -d
    pos = (pos + d) % 11
strings(0.5, 24, 1.0, a0=90, v=V, lh=560, pi=pi, pb=pb, pa=pa, pd=1.4)

# 1:04 the loom: the strings stay and seven strings cross them; the hits alternate (0 and 0.74 the standing strings, 0.43 the lying ones).
# From 1:11 (b36) the whole loom tilts one way and back
Vh = [-312 + 104 * i for i in range(7)]
pv, pvb, pva, ph, phb, pha = [], [], [], [], [], []
for b in range(24, 48):
    k = b - 24
    pv.append((k * 3) % 11); pvb.append(b); pva.append(100)
    pv.append((k * 3 + 6) % 11); pvb.append(b + 0.74); pva.append(100)
    ph.append((k * 2) % 7); phb.append(b + 0.43); pha.append(88)
    if b >= 30 and b % 2 == 1: ph.append((k * 2 + 4) % 7); phb.append(b + 0.74); pha.append(88)
tilt = dict(sb=[24, 36, 40, 44], sw=[0, 0.05, -0.05, 0.05])
strings(23.0, 48, 1.0, a0=90, v=V, lh=560, pi=pv, pb=pvb, pa=pva, pd=1.1, **tilt)
strings(23.0, 48, 1.0, a0=0, v=Vh, lh=800, pi=ph, pb=phb, pa=pha, pd=1.1, **tilt)
fx(24, 0.12, 4, 0.8)

# 1:18 the string art: 20 pegs on a ring round the screen, peg i tied to peg k*i (k = 2 the heart curve, 3, 4 ...). The ring turns, and on every
# second beat the threads are tied again another way (shown in grey a beat before): find a hole in the next pattern
fx(48, 0.3, 9, 1.4)
pbs = list(range(48, 64, 2)) + list(range(64, 80))
muls = [2, 3, 4, 5, 7, 9, 2, 6, 3, 8, 4, 11, 5, 7, 9, 13, 2, 6, 4, 3, 7, 5, 9, 8]
offs = [0, 1, 3, 0, 2, 5, 7, 1, 4, 0, 6, 2, 9, 3, 1, 5, 8, 2, 6, 0, 4, 7, 3, 1]
ev(46.0 - 1.0 * EZW, 'L18bart', tw=46.0, to=80, wn=1.0, M=20, ezM=16, R=760, a0=0, sb=[46, 56, 64, 72], sw=[0.12, -0.12, 0.14, -0.14],
   pb=pbs, mul=muls, off=offs, w=9)

# 1:37 the star: 16 pegs on a ring, each tied to the peg o further on, so the threads make a turning star with an empty circle in its middle
# (the bigger o, the smaller the circle; o = 8 is threads through the middle). Every second beat it is tied again with another o (grey a beat
# before): go into the circle when it opens, out between the points when it closes. From 1:47 (b97) it turns the other way, faster
fx(80, 0.25, 8, 1.2)
sb_ = list(range(80, 115, 2)); so_ = [5, 7, 4, 6, 8, 5, 7, 6, 4, 7, 5, 8, 6, 4, 7, 5, 8, 6]
ev(78.5 - 1.0 * EZW, 'L18bart', tw=78.5, to=114.5, wn=1.0, M=16, ezM=12, R=820, a0=0, sb=[78, 97], sw=[0.26, -0.34],
   pb=sb_, mul=[1] * len(sb_), off=so_[:len(sb_)], w=9)

# ================= part 2: I cut them (115-223) =================
stage(2, 115, 224)
fx(115, 0.35, 10, 1.5)

# 1:58 the strings snap one by one: each breaks in the middle and both halves whip away (the top half one way, the bottom half the other)
def whip(hit, x, y, L, a0, da, wn=1.25, w=10, curl=0.5, dur=0.32, hold=0.35, ezskip=False):
    ev(hit - wn * EZW, 'L18bwhip', hit=hit, wn=wn, x=round(x, 1), y=round(y, 1), L=L, a0=a0, da=da, w=w, curl=curl, dur=dur, hold=hold, ezskip=ezskip)
order = [5, 1, 9, 3, 7, 0, 10, 4, 6, 2, 8]
for k, b in enumerate(range(116, 128)):
    x = CX + V[order[k % 11]]; sg = 1 if k % 2 == 0 else -1
    whip(b, x, -10, 370, 90, 125 * sg, wn=1.5, dur=0.4)
    whip(b, x, H + 10, 370, -90, 125 * sg, wn=1.5, dur=0.4)
    if b >= 122 and b % 2 == 0:
        x2 = CX + V[order[(k + 5) % 11]]
        whip(b + 0.74, x2, -10, 370, 90, -125 * sg, wn=1.5, dur=0.4, ezskip=True)
        whip(b + 0.74, x2, H + 10, 370, -90, -125 * sg, wn=1.5, dur=0.4, ezskip=True)

# 2:05 the cut ends whip in from the edges: two a beat (on its first and last hit), half way round the frame from each other, lying along the wall and sweeping in;
# every beat the three move on round the frame and every other beat they sweep the other way
P = 2 * (W + H)
def perim(s):
    s %= P
    if s < W: return s, 0, 0          # top edge, going right
    s -= W
    if s < H: return W, s, 90         # right edge, going down
    s -= H
    if s < W: return W - s, H, 180    # bottom, going left
    s -= W
    return 0, H - s, 270              # left, going up
s0 = 0
for k, b in enumerate(range(129, 163)):
    for j, f in enumerate((0, 0.74)):
        x, y, tang = perim(s0 + j * P / 2 + (P / 4 if k % 2 else 0))
        sg = 1 if (k // 2) % 2 == 0 else -1
        a0 = tang if sg > 0 else tang + 180
        whip(b + f, x, y, 440, a0, 110 * sg, wn=1.25, w=10, dur=0.4, hold=0.25, ezskip=(j == 1))
    s0 += P / 14

# 2:26 the needle sews the world shut round you: two needles, one along rows and one down columns (a third from 2:32, b174, a fourth from 2:39, b186), lunge a stitch on the hits (and on the
# 0.74 hits too from 2:29, b168). The thread is stitched over and under the cloth: slip through the gaps between the stitches
def rows(ys, x0, x1, start_left=True):
    pts = []; left = start_left
    for i, y in enumerate(ys):
        a, b = (x0, x1) if left else (x1, x0)
        if i == 0: pts.append((a - 260 if left else a + 260, y))
        pts.append((a, y)); pts.append((b, y)); left = not left
    return pts
def cols(xs, y0, y1):
    pts = []; top = True
    for i, x in enumerate(xs):
        a, b = (y0, y1) if top else (y1, y0)
        if i == 0: pts.append((x, a - 260))
        pts.append((x, a)); pts.append((x, b)); top = not top
    return pts
def steps(b0, b1, fast_from):
    sb = []
    for b in range(b0, b1):
        sb += [b, b + 0.43] + ([b + 0.74] if b >= fast_from else [])
    return sb
na = rows([110, 290, 470, 630, 200, 560], 60, 1220)
nb = cols([1180, 930, 680, 430, 180, 1060, 560], 50, 670)
nc = [(-260, 380), (60, 380), (1220, 380), (1220, 30), (300, 690), (300, 30), (980, 690), (980, 30), (640, 690)]
nd = [(1540, 200), (1220, 200), (60, 520), (1220, 520), (60, 200), (640, 690)]
for pts, sb, tw in ((na, steps(164, 199, 168), 163), (nb, steps(165, 199, 170), 163), (nc, steps(174, 199, 174), 172), (nd, steps(186, 199, 186), 184)):
    ev(tw - 0.5, 'L18bneedle', tw=tw, to=199, px=[round(p[0], 1) for p in pts], py=[round(p[1], 1) for p in pts], sb=sb, st=78, lunge=0.16,
       so=46, su=27, w=9, nl=170)
fx(163, 0.3, 8, 1.4)

# 2:47 the harp: six strings through the middle turn like spokes, faster and faster with the build; on every hit one is plucked (round the
# harp). 3:01 they all snap
fx(199, 0.25, 8, 1.2)
pi, pb, pa = [], [], []
for k, b in enumerate(range(200, 224)):
    for j, f in enumerate(HITS):
        pi.append((k * 2 + j * 2) % 6); pb.append(b + f); pa.append(90 + 30 * (b - 200) / 23)
strings(198.0, 223.6, 1.0, mode='rad', n=6, da=30, r0=-820, r1=820, a0=90, sb=[198, 207, 215, 219], sw=[0.12, 0.22, 0.32, 0.42], pi=pi, pb=pb, pa=pa, pd=1.35)

# ================= part 3: only the red thread to you (224-289) =================
stage(3, 224, 289.5)
fx(224, 0.45, 12, 1.6)

# 3:01 two hearts on their orbits (the near one fast one way, the far one, mostly past the screen's edge, slow the other) with the red thread between them; it always hurts
# and it sounds on the hits. 3:06.6 (b232) a second pair joins, turning the other way. 3:20 (b256) a third pair, and the heart drawn in strings beats in the middle
# and turns a step on every beat
pi, pb, pa = [], [], []
for b in range(226, 288):
    for j, f in enumerate(HITS):
        th = 0 if b < 232 else (j + b) % 2 if b < 256 else (j + b) % 3
        pi.append(th); pb.append(b + f); pa.append(84 if b < 256 else 90)
ev(222.0, 'L18bthread', tw=222.5, ta=224, to=288.5, wn=1.0, rx=[260, 900, 330, 900, 200, 900], ry=[170, 560, 210, 560, 140, 560], ha0=[0, 200, 90, 300, 45, 160],
   sb=[222, 256], hsw=[0.48, -0.25, -0.44, 0.28, 0.6, -0.2, 0.54, -0.3, -0.5, 0.33, 0.66, -0.22], on=[224, 232, 256], off=[288.5, 288.5, 288.5], core=6, hr=20,
   pi=pi, pb=pb, pa=pa, pd=1.0)
hb = list(range(256, 288)); ho = [(i * 4) % 30 for i in range(len(hb))]
ev(254.0, 'L18bart', tw=254.5, to=288.5, wn=1.0, M=30, ezM=22, R=370, a0=0, sb=[254], sw=[0.25], pb=hb, mul=[2] * len(hb), off=ho, w=6)
fx(256, 0.35, 10, 1.4)
fx(288, 0.5, 6, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
acc = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L18', 'name': 'المرحلة الثامنة عشرة', 'song': 'اعوف الدنيا', 'file': 'SONG_L18', 'bpm': 101.997, 'off': 0.150, 'end': 289, 'cps': [0, 115, 224],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezwn': 1.25}, 'energy': acc['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
