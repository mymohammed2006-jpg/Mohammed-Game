#!/usr/bin/env python3
# Makes data/dodge_L15.json: L15 "Abra Macabre" (Black Ops Zombies), the summoning in the crypt. 120 BPM, beat 0 = 0.075 s, a beat = 0.5 s.
# Song map (beats): quiet start 0-11 | build 12-15 | riff 16-69 (0:08) | break 70-77 (0:35) | heavy 78-143 (0:39)
# | heavy + high lead 144-205 (1:12) | the quiet 206-237 (1:43) | heavy 238-303 (1:59) | the last part 304-365 (2:32) | win 366 (3:03.1)
# Parts: 1 the crypt (0-77), 2 the ritual (78-205), 3 the beast (206-366). Checkpoints 0, 78, 206.
# Nothing here is random: every attack is a fixed pattern on the beat. Times are beats; an event fires at its start minus
# wn * EZW (Easy's longest warning) and shows its warning from start - wn.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L15.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)
def stage(part, b0, b1): ev(b0, 'L15stage', part=part, to=b1, keep=True)
def fx(b, v=0.35, s=10, dur=1.5): ev(b, 'L15fx', v=v, s=s, dur=dur)
def e(a, r): return (math.cos(a) * r, math.sin(a) * r)

# the same smooth path as L15Path in L15.cs, to check how fast a light moves (px a second)
def herm(b, v, bt):
    n = len(b)
    def M(i): return (v[1] - v[0]) / (b[1] - b[0]) if i <= 0 else (v[i] - v[i - 1]) / (b[i] - b[i - 1]) if i >= n - 1 else (v[i + 1] - v[i - 1]) / (b[i + 1] - b[i - 1])
    if bt <= b[0]: return v[0]
    if bt >= b[-1]: return v[-1]
    i = 0
    while i < n - 2 and bt >= b[i + 1]: i += 1
    d = b[i + 1] - b[i]; u = (bt - b[i]) / d; u2 = u * u; u3 = u2 * u
    return (2 * u3 - 3 * u2 + 1) * v[i] + (u3 - 2 * u2 + u) * d * M(i) + (-2 * u3 + 3 * u2) * v[i + 1] + (u3 - u2) * d * M(i + 1)
def topspeed(kb, kx, ky):
    m = 0; st = 1 / 64
    bt = kb[0]
    while bt < kb[-1]:
        dx = herm(kb, kx, bt + st) - herm(kb, kx, bt); dy = herm(kb, ky, bt + st) - herm(kb, ky, bt)
        m = max(m, math.hypot(dx, dy) / (st * 0.5)); bt += st
    return m
SPEEDS = []

# ================= part 1: the crypt (0-77) =================
stage(1, 0, 78)

# 0:00 the lantern: the fog comes in grey from beat 1 and kills from beat 4; stay in the light while it walks round the crypt
kb = [4, 6, 8, 10, 12, 14, 16]
kx = [640, 640, 820, 960, 820, 600, 470]
ky = [360, 360, 280, 420, 560, 520, 380]
kr = [150, 130, 115, 105, 100, 100, 100]
at(4, 3, 'L15lantern', kb=kb, kx=kx, ky=ky, kr=kr, to=16.25)
SPEEDS.append(('lantern 0:00', topspeed(kb, kx, ky)))

# 0:08 the ribcage: a wave of ribs snaps shut column by column; stand in the gap of your column when it reaches you.
# From 0:12 two waves at once from both sides: find the place where both gaps meet (or move between them)
N = 16
def curve(f): return [round(max(70, min(H - 70, f(c))), 1) for c in range(N)]
def ribs(hit, d, gy, wn=1.5): at(hit, wn, 'L15ribs', dir=d, gy=gy, gh=60, dur=1.0, hold=0.3)
fx(16, 0.2, 8, 1.0)
ribs(17, 1, curve(lambda c: 360 + 180 * math.sin(2 * math.pi * c / 16)))
ribs(19, -1, curve(lambda c: 360 - 180 * math.sin(2 * math.pi * c / 16 + 0.8)))
ribs(21, 1, curve(lambda c: 200 + 25 * c))
ribs(23, -1, curve(lambda c: 560 - 25 * c))
ribs(25, 1, curve(lambda c: 360 + 200 * math.sin(2 * math.pi * c / 16))); ribs(25, -1, curve(lambda c: 360 - 200 * math.sin(2 * math.pi * c / 16)))
ribs(27, 1, curve(lambda c: 120 + 30 * c)); ribs(27, -1, curve(lambda c: 600 - 20 * c))
ribs(29, 1, curve(lambda c: 360 + 210 * math.sin(2 * math.pi * c / 8))); ribs(29, -1, curve(lambda c: 360 + 210 * math.cos(2 * math.pi * c / 8)))
ribs(31, 1, curve(lambda c: 600 - 30 * c)); ribs(31, -1, curve(lambda c: 120 + 30 * c))

# 0:16 the floor falls in a spiral from the edge in (each slab cracks 1.5 beats before); the slabs come back 5.5 beats later.
# Follow the ring that comes back, or go in and jump (dash) over a pit at the end
def spiral(cols, rows):
    out = []; top, bot, l, r = 0, rows - 1, 0, cols - 1
    while top <= bot and l <= r:
        for c in range(l, r + 1): out.append(top * cols + c)
        for rr in range(top + 1, bot + 1): out.append(rr * cols + r)
        if top < bot:
            for c in range(r - 1, l - 1, -1): out.append(bot * cols + c)
        if l < r:
            for rr in range(bot - 1, top, -1): out.append(rr * cols + l)
        top += 1; bot -= 1; l += 1; r -= 1
    return out
at(33, 1.5, 'L15floor', cols=8, rows=5, ord=spiral(8, 5), rate=0.3, dn=5.5, to=48)

# 0:24 the chains: every beat all cells burn but one; the next safe cell shows before.
# From 0:28 the echo: each burn is followed a beat later by its mirror (across the middle, the dashed line shows which way)
COLS, ROWS = 6, 4
def cell(r, c): return r * COLS + c
plain = [(1, 1), (1, 2), (0, 2), (0, 3), (1, 3), (1, 4), (2, 4), (2, 3)]
pt, ps, pe = [], [], []
for i, (r, c) in enumerate(plain): pt.append(49 + i); ps.append(cell(r, c)); pe.append(0)
# echo pairs: (first cell, mirror: 'h' left-right, 'v' up-down)
pairs = [((1, 2), 'h'), ((2, 3), 'h'), ((2, 1), 'v'), ((1, 2), 'v'), ((2, 3), 'h'), ((1, 2), 'v')]
b = 57
for (r, c), m in pairs:
    pt.append(b); ps.append(cell(r, c)); pe.append(0)
    r2, c2 = (r, COLS - 1 - c) if m == 'h' else (ROWS - 1 - r, c)
    pt.append(b + 1); ps.append(cell(r2, c2)); pe.append(1); b += 2
pt.append(69); ps.append(cell(2, 3)); pe.append(0)
at(49, 1.5, 'L15cells', cols=COLS, rows=ROWS, pt=pt, ps=ps, pe=pe, hold=0.25, to=69.6)

# 0:35 the light comes back and closes in to a point
kb = [70, 72, 74, 76, 77, 77.75]
kx = [746, 640, 520, 590, 660, 670]
ky = [450, 520, 420, 280, 300, 310]
kr = [120, 95, 75, 55, 42, 40]
at(70, 0.6, 'L15lantern', kb=kb, kx=kx, ky=ky, kr=kr, to=77.75)
SPEEDS.append(('lantern 0:35', topspeed(kb, kx, ky)))

# ================= part 2: the ritual (78-205) =================
stage(2, 76, 206)
fx(78, 0.45, 12, 1.5)
def arms(hit, to, n, sb, sw, a0=0, r0=0, ln=760, th=22, wn=1.5):
    at(hit, wn, 'L15arms', n=n, r0=r0, len=ln, th=th, a0=a0, sb=sb, sw=sw, to=to)
# 0:39 three bone arms turn round the skull and turn back on the heavy beats (the arrows flip a beat before)
arms(78, 94, 3, [78, 82, 86, 88, 90, 92], [0.45, -0.45, 0.5, -0.5, 0.55, -0.55])
# 0:47 four arms, and two short ones inside turning the other way
arms(94, 110, 4, [94, 98, 100, 102, 104, 106, 108], [0.42, -0.42, 0.48, -0.48, 0.5, -0.5, 0.5], a0=20)
arms(94, 110, 2, [94, 102], [-0.75, 0.75], a0=90, r0=70, ln=330, th=20)
# 0:55 the arms and the echo: a blast of spikes, and a beat later its echo on the other side of the skull
arms(110, 126, 3, [110, 114, 118, 122], [0.4, -0.4, 0.45, -0.45], a0=40)
ang = [0.3, 2.2, 4.0, 1.1, 3.0, 5.2, 0.6, 2.6]
for i in range(8):
    b0 = 111 + 2 * i; x, y = CX + math.cos(ang[i]) * 250, CY + math.sin(ang[i]) * 230
    at(b0, 1.0, 'L15blast', x=round(x, 1), y=round(y, 1), R=100, hold=0.3)
    at(b0 + 1, 1.0, 'L15blast', x=round(W - x, 1), y=round(H - y, 1), R=100, hold=0.3, echo=True)
# 1:03 the curse: your own path turns to spikes 0.75 s after you pass (for 1.6 s). Two arms turn and turn back: don't walk back on your path
ev(125.5, 'L15curse', hit=126, to=144, dl=0.75, lf=1.6, R=24)
arms(126, 144, 2, [126, 130, 134, 137, 140, 142], [0.35, -0.35, 0.4, -0.4, 0.45, -0.45], a0=0)

# 1:12 the door rings: the fog closes in ring by ring (go in through the doors when they line up), then the fog lifts and the middle
# blasts (get out). The doors of the 3 rings meet each round at the same place
P = 2 * math.pi / 10
w = 0.12; WS = [w + P, w - P, w]          # inner, middle, outer: they line up again every 10 beats
fb, fr, cb = [], [], []
for c in (145, 155, 165):
    fb += [c + 1.5, c + 3.0, c + 4.5, c + 5.5]; fr += [255, 175, 95, 9999]; cb += [c + 8]
# the doors line up for the way in: outer at c + 1, middle at c + 2.5, inner at c + 4 (all at angle th0 in the first round)
th0 = math.radians(200)
a0 = [math.degrees(th0 - WS[0] * (145 + 4 - 144)), math.degrees(th0 - WS[1] * (145 + 2.5 - 144)), math.degrees(th0 - WS[2] * (145 + 1 - 144))]
at(144, 2, 'L15rings', rr=[110, 190, 270], rw=16, dw=62, ws=WS, a0=a0, fb=fb, fr=fr, cb=cb, cR=180, ch=0.75, to=175.5)
fx(144, 0.25, 8, 1.0)

# 1:28 the star: drawn line by line while it turns; at the end all burns but the marked point. Be in it before it closes (or dash in)
def star(c, sv, sb, sw, a0):
    at(c + 1, 1, 'L15star', R=330, th=12, a0=a0, sb=sb, sw=sw, lt=[c + 1, c + 2, c + 3, c + 4, c + 5], sv=sv, det=c + 7, dh=1)
star(176, 0, [176], [0.12], 0)
star(186, 2, [186], [-0.15], 30)
star(196, 4, [196, 200], [0.16, -0.2], 10)

# ================= part 3: the beast (206-366) =================
stage(3, 204, 368)
fx(206, 0.3, 6, 1.0)
# 1:43 the quiet: three pendulums, and the fog creeps in from the left, then from the right: cross under the blades in time
for i, (x, ph) in enumerate([(320, 0), (640, 1.33), (960, 2.67)]):
    at(207, 2, 'L15blade', x=x, y=0, len=680, amp=32, per=4, ph=ph, R=46, to=238)
at(208, 2, 'L15creep', side=0, kb=[208, 222, 230, 236, 238], kx=[0, 520, 520, 300, 0], to=238)
at(222, 2, 'L15creep', side=1, kb=[222, 230, 236, 238], kx=[W, 820, 900, W], to=238)

# 1:59 the beast's jaws bite round the turning crypt: be in the strip between the fangs
fx(238, 0.35, 14, 1.5)
bb = [239, 241, 243, 245] + list(range(246, 254))
gx = [-150, 120, 0, 220, -60, 60, -180, -40, 140, 0, -200, 100]
at(239, 1.5, 'L15maw', a0=0, sb=[238, 246, 250], sw=[0.15, -0.2, 0.25], bb=bb, gx=gx, dw=55, hold=0.3, to=254)

# 2:08 the spine whips: they lash on the beat where their shadow showed
def whip(hit, x, y, angd, ln=1500, w_=44, wn=1.0, hold=0.4, ezskip=False):
    at(hit, wn, 'L15whip', x=x, y=y, ang=angd, len=ln, w=w_, hold=hold, **({'ezskip': True} if ezskip else {}))
for i, b0 in enumerate(range(255, 266)):
    k = i % 4
    if k == 0: whip(b0, 0, 120, 18); whip(b0 + 0.5, W, 600, 198, ezskip=True)
    elif k == 1: whip(b0, 0, H, -35); whip(b0 + 0.5, W, H, 215, ezskip=True)
    elif k == 2: whip(b0, 300, 0, 90, ln=740); whip(b0, 980, H, 270, ln=740); whip(b0 + 0.5, 640, 0, 90, ln=740, ezskip=True)
    else: whip(b0, 0, 360, 0); whip(b0 + 0.5, 0, 0, 29, ezskip=True); whip(b0 + 0.5, 0, H, -29, ezskip=True)

# 2:15 the false light: two lights split and cross; at the end of each round the false one burns. The true one beats on the beat
R0, RL, RV = 268, 4, 3.5
real = [0, 1, 1, 0, 1, 0, 0, 1, 0]
S = (640.0, 360.0); KB, AX, AY, BX, BY = [], [], [], [], []
def cl(p, m=110): return (max(m, min(W - m, p[0])), max(m, min(H - m, p[1])))
phis = [0.0, 1.2, 2.5, 0.6, 3.6, 1.9, 4.4, 0.2, 2.9]
for k in range(9):
    f = phis[k]; S = cl(S, 260)
    def pt_(a, r): return (S[0] + e(a, r)[0], S[1] + e(a, r)[1])
    A = [S, pt_(f, 110), pt_(f + math.pi / 2, 40), pt_(f + math.pi, 110), pt_(f + math.pi, 150)]
    Bp = [S, pt_(f + math.pi, 110), pt_(f - math.pi / 2, 40), pt_(f, 110), pt_(f, 150)]
    for j in range(5): KB.append(R0 + RL * k + j); AX.append(round(A[j][0], 1)); AY.append(round(A[j][1], 1)); BX.append(round(Bp[j][0], 1)); BY.append(round(Bp[j][1], 1))
    S = A[4] if real[k] == 0 else Bp[4]
    SPEEDS.append((f'light round {k}', max(topspeed(KB[-5:], AX[-5:], AY[-5:]), topspeed(KB[-5:], BX[-5:], BY[-5:]))))
at(268, 1.0, 'L15lights', kb=KB, ax=AX, ay=AY, bx=BX, by=BY, real=real, r=85, rv=RV, rl=RL, r0=R0, to=304)
END_LIGHT = S

# 2:32 all of it: the lantern with the curse, then the jaws with whips, then the arms with the echo and the curse, then the light closes
fx(304, 0.35, 12, 1.5)
kb = [304, 306, 308, 310, 312, 314, 316, 318, 320]
pts = [END_LIGHT, (760, 260), (930, 360), (860, 540), (620, 560), (420, 470), (380, 280), (560, 200), (640, 360)]
at(304, 1.0, 'L15lantern', kb=kb, kx=[round(p[0], 1) for p in pts], ky=[round(p[1], 1) for p in pts], kr=[140] * 9, to=320)
SPEEDS.append(('lantern 2:32', topspeed(kb, [p[0] for p in pts], [p[1] for p in pts])))
ev(303.5, 'L15curse', hit=304, to=319.5, dl=0.75, lf=1.4, R=22)
bb = list(range(321, 336))
gx = [0, 160, -60, 200, 40, -180, -20, 140, -120, 60, 220, 0, -160, 80, -40]
at(321, 1.5, 'L15maw', a0=40, sb=[320, 326, 330, 334], sw=[-0.2, 0.25, -0.3, 0.3], bb=bb, gx=gx, dw=58, hold=0.3, to=336)
for i, b0 in enumerate(range(322, 336, 2)):
    if i % 2 == 0: whip(b0 + 0.5, 0, 180 + 60 * i, 8)
    else: whip(b0 + 0.5, W, 640 - 60 * i, 188)
arms(336, 352, 3, [336, 340, 344, 348], [0.45, -0.45, 0.5, -0.5], a0=60)
ev(335.5, 'L15curse', hit=336, to=351.5, dl=0.75, lf=1.2, R=22)
ang = [1.0, 3.3, 5.0, 2.0, 4.1, 0.2, 2.8, 5.6]
for i in range(8):
    b0 = 337 + 2 * i; x, y = CX + math.cos(ang[i]) * 250, CY + math.sin(ang[i]) * 230
    at(b0, 1.0, 'L15blast', x=round(x, 1), y=round(y, 1), R=100, hold=0.3)
    at(b0 + 1, 1.0, 'L15blast', x=round(W - x, 1), y=round(H - y, 1), R=100, hold=0.3, echo=True)
# the light spirals in and closes; whips lash across it
kb = [352, 354, 356, 358, 360, 362, 363.5]
pts = [(640, 360), (800, 300), (760, 470), (580, 470), (560, 320), (660, 320), (640, 360)]
kr = [130, 110, 90, 75, 62, 50, 45]
at(352, 0.8, 'L15lantern', kb=kb, kx=[p[0] for p in pts], ky=[p[1] for p in pts], kr=kr, to=364)
SPEEDS.append(('lantern 2:56', topspeed(kb, [p[0] for p in pts], [p[1] for p in pts])))
for b0 in range(353, 363):
    x = herm(kb, [p[0] for p in pts], b0); y = herm(kb, [p[1] for p in pts], b0)
    a = (b0 * 67) % 180; off = 70 if b0 % 2 == 0 else -70
    nx, ny = -math.sin(math.radians(a)), math.cos(math.radians(a)); dx, dy = math.cos(math.radians(a)), math.sin(math.radians(a))
    px, py = x + nx * off, y + ny * off
    whip(b0, round(px - dx * 2000, 1), round(py - dy * 2000, 1), a, ln=4000, w_=36, ezskip=(b0 % 2 == 1))
ev(361.5, 'L15shatter', t0=362, t1=364)
fx(364, 0.6, 16, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
acc = json.load(open(os.path.join(HERE, 'accents.json')))
D = {'id': 'L15', 'name': 'المرحلة الخامسة عشرة', 'song': 'Abra Macabre', 'file': 'SONG_L15', 'bpm': 120, 'off': 0.075, 'end': 366, 'cps': [0, 78, 206],
     'ez': {'ezg': 1, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.15}, 'energy': acc['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
for n, s in SPEEDS: print(f'  {n}: top speed {s:.0f} px/s (the square: 370)')
