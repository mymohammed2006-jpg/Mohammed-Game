#!/usr/bin/env python3
# Makes data/dodge_L17.json: L17 "حالف لا ما انام" (Jafar Al-Ghazal and Mohammed Jamal), the second design (10-28): "سهرة ما تنام",
# the party that swore not to sleep. (The first design is gen_v51.py -> data/dodge_L17_v51.json, for the replays recorded with it.)
# The level is the song from 0:24 to 2:44 (songs/SONG_L17.mp3 is cut from 0:23.77). 76 BPM, beat 0 = 0.205 s in the cut file, a beat = 0.7895 s.
# The drum: each beat hits at +0 (dum) and +0.74 (the strongest), with +0.42 between them.
# Song map (beats): intro 0-16 (lighter) | the full beat from 16 (0:36.6), a big hit every 16 (16, 32, 56, 80) | the bass drops 96-98 (1:39.8) |
#   quieter verse 98-120 | no bass 120-136 (1:58.7) | the full beat again from 136 (2:11.3), big hits 152, 176 | win 177.42 (2:44.0)
# Parts: 1 the drum and the claps (0-64), 2 the record (64-120), 3 till morning (120-177.42). Checkpoints 0, 64, 120.
# Nothing is random or aimed. Every event of a part starts after the part's restart point (checkpoint - 2), so a restart rebuilds it.
# Times are beats; an event fires at its first danger minus EZW times its warning (Easy shows the warning longer).
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L17.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
END = 177.42
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L17bfx', v=v, s=s, dur=dur)
def first(b, wn): return max(0, b - wn * EZW - 0.05)

class Drums:
    def __init__(s): s.l = []
    def add(s, b, x, y, r, ez=0): s.l.append((b, x, y, r, ez))
    def put(s, wn=1.0, hold=0.3):
        l = sorted(s.l); ev(first(l[0][0], wn), 'L17bdrums', sb=[a[0] for a in l], x=[a[1] for a in l], y=[a[2] for a in l], r=[a[3] for a in l], ez=[a[4] for a in l], hold=hold, wn=wn)
class Claps:
    def __init__(s): s.l = []
    def add(s, b, x, y, a, hw, ez=0): s.l.append((b, x, y, a, hw, ez))
    def put(s, wn=1.0):
        l = sorted(s.l); ev(first(l[0][0] - 0.34, wn), 'L17bclap', cb=[a[0] for a in l], x=[a[1] for a in l], y=[a[2] for a in l], a=[a[3] for a in l], hw=[a[4] for a in l], ez=[a[5] for a in l], wn=wn)
class Fire:
    def __init__(s): s.l = []
    def add(s, b, x, y, n, sp, a0=0, lx=None, dl=0, sg=1, ez=0): s.l.append((b, x if lx is None else lx, x, y, n, a0, sg, dl, sp, ez))
    def put(s, fu=0.8, sr=11, wn=1.0, rng=560):
        l = sorted(s.l); ks = ['fb', 'lx', 'fx', 'fy', 'n', 'a0', 'sg', 'dl', 'sp', 'ez']
        ev(first(l[0][0] - fu, wn), 'L17bfire', fu=fu, sr=sr, wn=wn, rng=rng, **{k: [a[i] for a in l] for i, k in enumerate(ks)})
def chobi(vert, sb, ps, hc, hs=75, dr=25, sp=84, lw=16, wn=1.0):
    assert len(ps) == len(sb) + 1 and len(hc) == len(sb) + 1
    ev(first(sb[0], wn), 'L17bchobi', vert=vert, sb=sb, ps=ps, hc=hc, hs=hs, dr=dr, sp=sp, lw=lw, wn=wn, to=sb[-1] + 0.3)
def laser(ox, oy, kb, ka, hit, to, bw=26, mv=0.2, wn=0.9):
    ev(first(hit, max(wn, 1)), 'L17blaser', ox=ox, oy=oy, kb=kb, ka=ka, mv=mv, bw=bw, wn=wn, hit=hit, to=to)

ev(0, 'L17bstage', to=END + 1, keep=True)

# ================= part 1: the drum and the claps (0-64) =================
fx(0, 0.2, 6, 1.0)
# 0:24 the frame drums on the skin: a column of three slams on the strong hit, then two columns, then a wave across and back
COL = [160, 400, 640, 880, 1120]; ROW = [150, 360, 570]
d = Drums()
def col(b, c, r=114, ez=0):
    for y in ROW: d.add(b, COL[c], y, r, ez)
SEQ = [(1, [2]), (1.74, [1, 3]), (2.42, [0]), (2.74, [4]), (3, [2]), (3.74, [0, 4]), (4.42, [1]), (4.74, [3]), (5, [2]), (5.42, [0]), (5.74, [4]),
       (6, [1]), (6.42, [3]), (6.74, [0, 2]), (7.42, [4]), (7.74, [1, 3])]
for j, (b, cs) in enumerate(SEQ):
    for q, c in enumerate(cs): col(b, c, ez=1 if (q == 1 or j % 3 == 2) else 0)
# 0:30 the claps come in on the dum (flat bands), the drums on the strong hit go on the diagonals
cl = Claps()
for j, y in enumerate([180, 540, 360, 200, 520, 300, 460, 360]): cl.add(9 + j, CX, y, 0, 82)
for j in range(4): cl.add(9.42 + 2 * j, [320, 960, 480, 800][j], CY, 90, 70, ez=j % 2)
for j in range(8):
    b = 8.74 + j
    pts = [(300, 170), (640, 360), (980, 550)] if j % 2 == 0 else [(300, 550), (640, 360), (980, 170)]
    if j % 4 == 3: pts = [(160, 360), (1120, 360)]
    for (x, y) in pts: d.add(b, x, y, 110, ez=1 if (x, y) == (640, 360) else 0)
d.put(wn=1.0)

# 0:36 the chobi: the dancers' line steps in from the right on every dum; get through where they let go of hands. The claps shut the
# columns in front of it on the strong hit
fx(16, 0.15, 8, 1.0)
# (two steps a beat: on the dum and on the middle hit)
sb = [b + o for b in range(16, 25) for o in (0, 0.42)][:17]
PS = [1400 - (1660 / 17) * k for k in range(18)]
HC = [360, 200, 520, 300, 560, 180, 420, 250, 500, 340, 600, 160, 440, 280, 540, 220, 400, 360]
chobi(True, sb, PS, HC, hs=62)
for j in range(7):
    xl = 1400 - (1660 / 17) * (2 * j + 2)
    x = xl - 380 if xl - 380 > 120 else xl + 380
    cl.add(16.74 + j, x, CY, 90, 70, ez=j % 2)
# 0:42 two lines: one from the left on the dum, one from the top on the strong hit
chobi(True, [b + o for b in range(24, 33) for o in (0, 0.42)][:17], [-80 + (1640 / 17) * k for k in range(18)],
      [360, 200, 520, 300, 560, 180, 460, 260, 520, 340, 600, 220, 480, 160, 420, 300, 540, 540], hs=64)
chobi(False, [24.74 + i for i in range(8)], [-60, 80, 180, 280, 380, 480, 580, 680, 800], [640, 300, 980, 520, 220, 860, 420, 1040, 1040], hs=70)
cl.put(wn=1.0)

# 0:49 the fireworks: a shell a beat bursts on the strong hit; from 0:52 a second, slower ring from the same shell
fx(32, 0.15, 8, 1.0)
f = Fire()
P5 = [(320, 230), (960, 230), (640, 190), (420, 300), (860, 300), (640, 250), (300, 210), (980, 210)]
for j, (x, y) in enumerate(P5):
    b = 32.74 + j
    f.add(b, x, y, 20, 350, a0=9 * (j % 2), lx=x + (60 if x < CX else -60))
    if j >= 2: f.add(b, x, y, 14, 220, a0=13 + 9 * (j % 2), lx=x + (60 if x < CX else -60), ez=1)
    if j % 2 == 1: f.add(b - 0.32, W - x, y + 60, 14, 330, a0=6, lx=W - x, ez=1)
# 0:55 two at a time (left and right) on the dum and on the strong hit, and spirals from the middle; the claps cross on the diagonals
for j in range(8):
    b = 40 + j
    if j in (4, 6):
        f.add(b, CX, 200, 30, 330, a0=90, dl=0.028, sg=1 if j == 4 else -1)
        continue
    f.add(b, 280, 240, 18, 360, a0=10 * (j % 2), lx=200)
    f.add(b + 0.74, 1000, 240, 18, 360, a0=10 * ((j + 1) % 2), lx=1080, ez=1)
f.put()
cl = Claps()
for j in range(8): cl.add(40.42 + j, CX, CY, 45 if j % 2 == 0 else -45, 66, ez=j % 2)

# 1:01 the party lights from the four corners step on the dum and the strong hit and cross each other
fx(48, 0.12, 6, 1.0)
def steps(b0, b1, offs):
    out = []
    for b in range(b0, b1):
        for o in offs: out.append(b + o)
    return out
# (the corners opposite each other move together: one pair on the dum, the other on the strong hit, every beat)
TL = [22, 64, 30, 72, 40, 18, 66, 34, 76]
TR = [158, 112, 150, 104, 140, 162, 116, 146, 100]
laser(0, 0, [47] + [48 + i for i in range(8)], TL, 48, 56.2, mv=0.22)
laser(W, 720, [47] + [48 + i for i in range(8)], [180 + a for a in TL], 48, 56.2, mv=0.22)
laser(W, 0, [47] + [48.74 + i for i in range(8)], TR, 48.42, 56.2, mv=0.22)
laser(0, 720, [47] + [48.74 + i for i in range(8)], [180 + a for a in TR], 48.42, 56.2, mv=0.22)
for j in range(8): cl.add(48.42 + j, CX, CY, 0 if j % 2 == 0 else 90, 56, ez=j % 2)
# 1:08 five spotlights from the top swing like scissors on the dum; from 1:11 the drums slam under them on the strong hit
LX = [128, 384, 640, 896, 1152]
for i, x in enumerate(LX):
    sg = 1 if i % 2 == 0 else -1
    ka = [90] + [90 + sg * (34 if j % 2 == 0 else -34) for j in range(16)]
    laser(x, 0, [55.5] + [56 + j * 0.5 for j in range(16)], ka, 56.0, 61.9, bw=22, mv=0.26)
d = Drums()
for j in range(5):
    b = 56.74 + j
    xs = [256, 768] if j % 2 == 0 else [512, 1024]
    for x in xs: d.add(b, x, 600, 110)
d.put()
cl.add(57.42, CX, 360, 0, 66); cl.add(59.42, CX, 360, 0, 66, ez=1); cl.add(61.42, CX, 360, 0, 66)
cl.put()

# ================= part 2: the record (64-120) =================
fx(64, 0.12, 8, 1.2)
# 1:15 the record: four rings and the spindle turn (each its own way), with openings to stand in; the arrows show the way they go and blink
# before they all turn back. The claps cross the record: change rings before your opening goes under them
ev(first(65, 1.5), 'L17brecord', re=[60, 165, 270, 380, 820], w=[0.68, -0.56, 0.45, -0.34], no=[2, 3, 4, 6], hh=[42, 28, 21, 14], a0=[0, 30, 10, 45],
   kb=[65, 68, 71, 73, 75, 76.74, 78.42], km=[1, -1, 1.5, -1, 1, -1.5, 1], wn=1.5, hit=65, to=80)
cl = Claps()
for j in range(13): cl.add(66.74 + j, CX, CY, [90, 0, 45, -45][j % 4], 46, ez=j % 2)
cl.put(wn=1.2)

# 1:27 the equalizer: the bars leave a tunnel that jumps on every dum (bar after bar, like the sound), and the bars bump in on the two
# other hits. From 1:33 the bars come from the walls
fx(80, 0.15, 8, 1.0)
# (the tunnel moves at most ~110 px a step, so the old and the new tunnel always overlap)
sb = [80.5] + [b + o for b in range(81, 89) for o in (0, 0.74)]
C = [(360 + 190 * math.sin(k * 0.55), 360 + 190 * math.sin(k * 0.55 + 2.2)) for k in range(len(sb))]
ev(first(80.5, 1.0), 'L17beq', side=False, nc=20, sb=sb, c0=[c[0] for c in C], c1=[c[1] for c in C], jag=28, gh=100,
   bb=[b + 0.42 for b in range(81, 89)], bump=34, stg=0.03, wn=0.7, hit=80.5, to=88.7)
sb = [88.9] + [b + o for b in range(89, 96) for o in (0, 0.74)]
C = [(640 - 300 * math.sin(k * 0.5), 640 - 300 * math.sin(k * 0.5 + 2.6)) for k in range(len(sb))]
ev(first(88.9, 1.0), 'L17beq', side=True, nc=12, sb=sb, c0=[c[0] for c in C], c1=[c[1] for c in C], jag=32, gh=112,
   bb=[b + 0.42 for b in range(89, 96)], bump=34, stg=0.04, wn=0.7, hit=88.9, to=96)
# 1:39 the bass drops: one clap flat, one standing, across the middle
cl = Claps()
cl.add(97, CX, CY, 0, 110); cl.add(97.74, CX, CY, 90, 150)
cl.put(wn=1.0)
fx(98, 0.15, 10, 1.0)

# 1:41 the dance rings: two rings of dancers, one opening out and one closing in, step by step on the dum and the strong hit, turning
# opposite ways; go through their holes as they pass you
RA = [90, 230, 370, 510, 650, 790, 650, 510, 370, 230, 90]
RB = [790, 650, 510, 370, 230, 90, 230, 370, 510, 650, 790]
ev(first(98.5, 1.0), 'L17bdance', sb=[99 + i for i in range(10)], rr=RA, w=0.62, a0=0, nh=3, hh=16, nd=24, dr=22, lw=14, wn=1.0, hit=98.5, to=109.3)
ev(first(98.5, 1.0), 'L17bdance', sb=[99.74 + i for i in range(10)], rr=RB, w=-0.52, a0=30, nh=3, hh=16, nd=24, dr=22, lw=14, wn=1.0, hit=98.5, to=110)
# 1:49 again faster (two holes each, turning faster), and two lights from the top corners sweep slowly
RA = [790, 650, 510, 370, 230, 90, 230, 370, 510, 650, 790]
RB = [90, 230, 370, 510, 650, 790, 650, 510, 370, 230, 90]
ev(first(109.5, 1.0), 'L17bdance', sb=[110 + i for i in range(8)] + [117.5], rr=RA[:10], w=0.85, a0=15, nh=2, hh=20, nd=22, dr=22, lw=14, wn=1.0, hit=109.5, to=117.9)
ev(first(109.5, 1.0), 'L17bdance', sb=[110.74 + i for i in range(8)] + [118], rr=RB[:10], w=-0.75, a0=60, nh=2, hh=20, nd=22, dr=22, lw=14, wn=1.0, hit=109.5, to=117.9)
laser(0, 0, [109.6] + [111 + j for j in range(7)], [10, 60, 25, 75, 30, 68, 20, 55], 110, 117.9, bw=24, mv=0.45, wn=1.0)
laser(W, 0, [109.6] + [111.5 + j for j in range(6)], [170, 120, 155, 105, 150, 112, 160], 110, 117.9, bw=24, mv=0.45, wn=1.0)

# ================= part 3: till morning (120-177.42) =================
fx(120, 0.12, 8, 1.2)
# 1:58 the fireworks show (no bass): a shell on every dum, sweeping over the roofs; from 2:05 a second one on the strong hit; then spirals
f = Fire()
for j in range(15):
    b = 121 + j
    x = [220, 450, 680, 910, 1060, 830, 600, 370][j % 8]; y = [220, 270, 200, 260, 220, 290, 210, 260][j % 8]
    if b in (132, 134): continue
    f.add(b, x, y, 22, 320, a0=8 * (j % 2))
    if b >= 124 and b not in (131, 133, 135): f.add(b + 0.74, W - x, y + 30, 14, 380, a0=13, ez=1 if b < 128 else 0)
f.add(132, 320, 250, 34, 330, a0=0, dl=0.026, sg=1)
f.add(134, 960, 250, 34, 330, a0=180, dl=0.026, sg=-1)
f.add(135.74, CX, 220, 20, 330, a0=9, ez=1)
f.put()

# 2:11 the dancers from four sides: two standing lines cross the screen on the dum, two flat lines on the strong hit
fx(136, 0.18, 10, 1.0)
chobi(True, [136 + i for i in range(8)], [-60, 120, 300, 480, 660, 840, 1020, 1200, 1380], [360, 200, 520, 280, 560, 180, 440, 300, 300], hs=75)
chobi(True, [136.42 + i for i in range(8)], [1340, 1160, 980, 800, 620, 440, 260, 80, -100], [360, 540, 220, 480, 160, 520, 260, 420, 420], hs=75)
chobi(False, [136.74 + i for i in range(7)], [-60, 70, 180, 290, 400, 510, 620, 790], [640, 300, 940, 520, 200, 860, 420, 420], hs=85)
# 2:17 the record again, faster and turning back more often
ev(first(145, 1.5), 'L17brecord', re=[55, 150, 260, 380, 820], w=[-0.8, 0.66, -0.5, 0.38], no=[2, 3, 4, 6], hh=[42, 27, 20, 14], a0=[90, 0, 60, 20],
   kb=[145, 147, 148.74, 150], km=[1, -1, 1.3, -1.3], wn=1.5, hit=145, to=152)

# 2:24 six lights from the floor fan out and close on the dum and the strong hit; the drums slam along the top between them
fx(152, 0.18, 10, 1.0)
FL = [107, 320, 533, 747, 960, 1173]
st = steps(152, 160, (0, 0.74))
for i, x in enumerate(FL):
    u = (i - 2.5) / 2.5
    ka = [-90]
    for j in range(len(st)):
        ph = j % 4
        ka.append(-90 + [u * 32, -u * 32, 26 * (1 if i % 2 else -1), -26 * (1 if i % 2 else -1)][ph])
    laser(x, 720, [151.5] + st, ka, 152, 160.2, bw=20, mv=0.18)
d = Drums()
for j in range(8):
    b = 152.42 + j
    xs = [213, 640, 1067] if j % 2 == 0 else [427, 853]
    for x in xs: d.add(b, x, 150, 100, ez=1 if (j % 2 == 0 and x == 640) else 0)
d.put()
# 2:30 the claps cross on the diagonals on the dum and the strong hit, fireworks from the corners between them
cl = Claps()
for j in range(8):
    b = 160 + j
    cl.add(b, CX, CY, 34 if j % 2 == 0 else -34, 64)
    if j % 2 == 1: cl.add(b + 0.74, CX, CY, 90 if j % 4 == 1 else 0, 50, ez=1)
cl.put(wn=1.0)
f = Fire()
for j in range(8):
    b = 160.42 + j
    x = 190 if j % 2 == 0 else 1090
    f.add(b, x, 200 if j % 4 < 2 else 520, 18, 350, a0=10 * (j % 3), lx=x)
f.put(fu=0.7)

# 2:36 the last fireworks: spirals from three shells, rings between, the drums slam round the middle on the dum; the last at 2:43
fx(168, 0.2, 10, 1.2)
f = Fire()
f.add(168, 320, 220, 28, 280, dl=0.03, sg=1); f.add(168.74, 960, 220, 28, 280, a0=180, dl=0.03, sg=-1, ez=1)
f.add(170, CX, 240, 22, 330, a0=8); f.add(170.74, 300, 300, 16, 300, ez=1); f.add(171.42, 980, 300, 16, 300, a0=11)
f.add(172, 960, 220, 28, 280, a0=180, dl=0.03, sg=-1); f.add(172.74, 320, 220, 28, 280, dl=0.03, sg=1, ez=1)
f.add(174, CX, 220, 26, 300, a0=7); f.add(174.74, 400, 280, 14, 330, ez=1); f.add(175.42, 880, 280, 14, 330, a0=12)
f.add(176, CX, 230, 30, 300, a0=6)
f.put()
d = Drums()
for j in range(7):
    b = 169 + j
    for q in range(3):
        a = math.radians(-90 + 120 * q + (60 if j % 2 else 0)); d.add(b, CX + math.cos(a) * 250, CY + 40 + math.sin(a) * 210, 105, ez=1 if q == 2 else 0)
d.put()

EV.sort(key=lambda e: e['b'])
# a part's events must start after the part's restart point (checkpoint - 2), and the part before must be over by the checkpoint
for e in EV:
    if e['k'] == 'L17bstage': continue
    to = e.get('to')
    for cp in (64, 120):
        if e['b'] < cp - 2 and to is not None and to > cp - 2 + 0.01: print('WARNING crosses', cp, e['k'], e['b'], to)

energy = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L17', 'name': 'المرحلة السابعة عشرة', 'song': 'حالف لا ما انام', 'file': 'SONG_L17', 'bpm': 76.0, 'off': 0.205, 'end': END, 'cps': [0, 64, 120],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.85, 'ezwn': 1.3}, 'energy': energy, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
