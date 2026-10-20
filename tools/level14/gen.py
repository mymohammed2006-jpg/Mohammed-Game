#!/usr/bin/env python3
# Makes data/dodge_L14.json: L14 "Jump again" (Neon Beats). 120 BPM, beat 0 = 0.315 s, a bar = 4 beats = 2 s.
# The rhythm is 3-2-3: in every two beats the hits are at +0.25, +1 and +1.5 (acc()).
# Song map (beats): bass 0-15 | hats 16-31 | drums 32-47 | build 48-63 | lead 64-95 | bigger lead 96-127
# | the lights go out 128-143 (1:04) | plunger 144-147 (1:12) | drop 148-179 (1:14) | drain 180-195 (1:30) | win 197 (1:38.8)
# Parts: 1 the top of the machine (0-63), 2 the play field (64-147), 3 the jackpot (148-197): ramps, magnet, ball locks, drain.
# Times are written as "hit" beats; an event fires at hit - wn * EZW (Easy's longest warning), its warning shows from hit - wn.
import json, math, os, random

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L14.json')
W, H = 1280, 720
EZW = 1.3
R = random.Random(1414)
EV = []

def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    # aimed at the square, or random each run: the human bot only knows it 0.32-0.40 s after it fires
    if kw.get('aim') or k in ('L14bumperP', 'L14ballP', 'L14flip'): d['rnd'] = True
    for a, v in kw.items(): d[a] = round(v, 4) if isinstance(v, float) else v
    EV.append(d)
def acc(g, k): return g * 2 + (0.25, 1.0, 1.5)[k]
def rx(a=150, b=W - 150): return a + R.random() * (b - a)

# the speeds (px/s) and warnings (beats) of each part of the song
T = {'bass': (280, 1.1), 'hats': (320, 1.0), 'drums': (340, 1.0), 'build': (360, 0.9), 'lead': (390, 0.9),
     'lead2': (410, 0.85), 'dark': (290, 1.0), 'riser': (440, 0.8), 'drop': (400, 1.0), 'magnet': (400, 1.0), 'lock': (340, 0.9), 'drain': (360, 0.9)}

def bumper(b0, b1, x, y, R0, pops, popR, t): ev(b0 - t[1] * EZW, 'L14bumper', x=x, y=y, R=R0, hit=b0, to=b1, pops=pops, popR=popR, wn=t[1])
def bumperP(b0, b1, R0, popR, t, off): ev(b0 - t[1] * EZW, 'L14bumperP', a=round(R.random() * 2 * math.pi, 4), off=off, R=R0, hit=b0, to=b1, popR=popR, wn=t[1])
def ballP(b, x, y, t, nb, split=False, jit=0.35):
    kw = dict(x=x, y=y, hit=b, wn=t[1] * 0.8, sp=t[0], nb=nb, j=round((R.random() - 0.5) * jit, 4))
    if split: kw['split'] = True
    ev(b - t[1] * 0.8 * EZW, 'L14ballP', **kw)
def lane(b, b1, i, n, t): ev(b - t[1] * EZW, 'L14lane', i=i, n=n, hit=b, to=b1, wn=t[1])
def laneP(b, ln, n, t, d=0): ev(b - t[1] * EZW, 'L14lane', aim=True, d=d, n=n, hit=b, to=b + ln, wn=t[1])
# a wall over the whole screen needs time to walk to its gap: 3 beats of warning
BW = 3
def bank(b, n, gap, step, d, t, gw=2): ev(b - BW * EZW, 'L14bank', n=n, gap=gap, gw=gw, hit=b, step=step, dir=d, wn=BW)
def bankP(b, n, step, t, gw=2): ev(b - BW * EZW, 'L14bank', aim=True, n=n, gw=gw, hit=b, step=step, wn=BW)
def flipper(b0, b1, px, py, ln, a0, a1, flips, t, nb, split=False):
    kw = dict(px=px, py=py, len=ln, a0=round(a0, 4), a1=round(a1, 4), b0=b0, b1=b1, flips=flips, wn=t[1], sp=t[0], nb=nb)
    if split: kw['split'] = True
    ev(b0 - 2, 'L14flip', **kw)
def deco(part, b0, b1): ev(b0, 'L14deco', part=part, to=b1, keep=True)
def fall(b, x, t, dur, r): ev(b - t[1] * EZW, 'L14fall', x=round(x, 1), hit=b, dur=dur, r=r, wn=t[1])
def fallP(b, off, t, dur, r): ev(b - t[1] * EZW, 'L14fall', aim=True, off=off, hit=b, dur=dur, r=r, wn=t[1])

# =========================== part 1: the top of the machine (0:00 - 0:32) ===========================
deco(1, 0, 64)
# the bass: a bumper lands beside you on each bass hit and pops a ring
t = T['bass']
for g in range(2, 8):
    bumperP(acc(g, 0), acc(g, 0) + 3, 50, 100, t, 210)
    if g % 4 == 1: bumperP(acc(g, 2), acc(g, 2) + 2.5, 44, 110, t, 320)
# the hats: three bumpers in a triangle pop in turn on the 3-2-3 and shoot balls at you
t = T['hats']
bx = [W / 2, W / 2 - 250, W / 2 + 250]; by = [H / 2 - 150, H / 2 + 120, H / 2 + 120]
pops = [[], [], []]; c = 0
for g in range(8, 16):
    for k in range(3):
        if k == 0 or (k == 2 and g % 2 == 1): pops[c % 3].append(acc(g, k))
        if k == 0 and g % 2 == 0: ballP(acc(g, k), bx[c % 3], by[c % 3], t, 2)
        c += 1
for i in range(3): bumper(16, 32, bx[i], by[i], 58, pops[i], 110, t)
# the drums: banks of drop targets stand up over the whole screen with a gap, then fall one by one
t = T['drums']
for k in range(2):
    b = 32 + k * 8
    bankP(b, 10, 0.25, t, 3)
    bumperP(b + 4, b + 6, 46, 150, t, 160); bumperP(b + 5.5, b + 7.5, 46, 150, t, 220)
# the build: rollover lanes light up beside you, faster and faster (beats, then halves, then quarters)
t = T['build']
for i, d in enumerate((0, 2, -2, 2)): laneP(48 + i * 2, 0.6, 12, t, d)
for i, d in enumerate((2, -2, 2, -2)): laneP(56 + i * 0.75, 0.45, 12, t, d)
# then from both sides to the middle in quarters (twice), and the middle lights last
for r in range(2):
    for i in range(4):
        b = 60 + r * 1.5 + i * 0.25; lane(b, b + 0.4, i, 10, t); lane(b, b + 0.4, 9 - i, 10, t)
lane(63, 64, 4, 10, t); lane(63, 64, 5, 10, t)
ev(64, 'L14fx', v=0.3, s=12)

# =========================== part 2: the play field (0:32 - 1:14) ===========================
deco(2, 64, 128)
t = T['lead']
# the lead: two flippers at the bottom flip on the 3-2-3 and shoot balls up
fl, fr = [], []
for g in range(32, 48):
    fl.append(acc(g, 0)); fr.append(acc(g, 1))
    if g % 2 == 1: fl.append(acc(g, 2))
flipper(64, 96, 330, 690, 300, 0.45, -0.5, fl, t, 2)
flipper(64, 96, W - 330, 690, 300, math.pi - 0.45, math.pi + 0.5, fr, t, 2)
# the two slingshots kick on the snare (beats 1 and 3)
for sd in (-1, 1): ev(62, 'L14sling', x=W / 2 + sd * 420, sd=sd, b0=64, b1=96, kicks=[64 + i * 4.0 + (2 if sd < 0 else 0) + 1 for i in range(8)])
# two bumpers at the top pop every two bars, and kick balls at you between the flippers' shots
bumper(64, 96, W / 2 - 300, 170, 52, [64 + i * 8.0 for i in range(4)], 140, t)
bumper(64, 96, W / 2 + 300, 170, 52, [68 + i * 8.0 for i in range(4)], 140, t)
for k in range(8): ballP(64 + k * 4 + 3, W / 2 - 300 if k % 2 == 0 else W / 2 + 300, 170, t, 1, False, 0.2)
# the bigger lead: multiball. Flippers on the side walls, and every ball breaks in two at its first rebound
t = T['lead2']
fl, fr = [], []
for g in range(48, 64):
    (fl if g % 2 == 0 else fr).append(acc(g, 0))
    if g % 4 == 1: fl.append(acc(g, 2))
    if g % 4 == 3: fr.append(acc(g, 2))
flipper(96, 128, 20, 250, 280, 0.6, -0.4, sorted(fl), t, 2, True)
flipper(96, 128, W - 20, 470, 280, math.pi - 0.6, math.pi + 0.4, sorted(fr), t, 2, True)
bankP(100, 10, 0.25, t); bankP(116, 10, 0.25, t)
# the lights go out: TILT, and the dot display runs JUMP AGAIN (to the left) and AGAIN JUMP (to the right)
t = T['dark']
ev(128, 'L14gloom', v=0.55); ev(128, 'L14fx', v=0.35, s=10)
ev(127, 'L14word', s='TILT', t0=128, t1=131, c=24, y=30)
C = 22
ev(126, 'L14matrix', s='JUMP AGAIN', c=C, y0=12, dir=-1, sp=t[0] * 0.85, ta=128, te=144, amp=28, **{'from': 128})
ev(129, 'L14matrix', s='AGAIN JUMP', c=C, y0=H - 12 - 7 * C, dir=1, sp=t[0] * 0.85, ta=131, te=144, amp=28, **{'from': 128})
ev(144, 'L14gloom', v=0)
# the plunger: the spring pulls back, the lanes run after you
t = T['riser']
ev(143, 'L14plunger', t0=144, t1=148, R=70)
for i, d in enumerate((0, 2, -2, 1, -1, 2, -2, 0)): laneP(144 + i * 0.5, 0.35, 12, t, d)

# =========================== part 3: the jackpot (1:14 - 1:38) ===========================
deco(3, 148, 180)
ev(148, 'L14fx', v=0.5, s=18)
# the ramps (1:14): curved rails light up and trains of three balls race along them on the 3-2-3
t = T['drop']
RAMPS = [(W / 2, 820, 520, 3.75, 5.67), (330, 330, 190, 0.3, 5.3), (W - 330, 390, 190, 3.4, -1.6), (W / 2, -100, 620, 2.45, 0.69)]
for g in range(74, 78):
    cx, cy, rr, a0, a1 = RAMPS[g - 74]; tr = [acc(g, k) for k in range(3)]
    ev(tr[0] - t[1] * EZW, 'L14ramp', cx=cx, cy=cy, rr=rr, a0=a0, a1=a1, trains=tr, wn=t[1], sp=t[0], nb=3, gap=0.1, r=15)
# the magnet (1:18): balls come in, spin around it and shoot straight out, four times
t = T['magnet']
ev(154.5, 'L14magnet', cs=[156, 160, 164, 168], tin=1.5, tout=3, n=12, r0=520, ro=170, w=1.8, sp=t[0], R=34, te=172, r=15)
# the ball locks (1:26): three locks at the top fill up on the 3-2-3, then all let go in fans downwards
t = T['lock']
REL = [174.25, 176.25, 178.25, 180.25]
ev(171, 'L14locks', t0=171.5, t1=179, lights=[r - d for r in REL for d in (1.75, 1.25, 0.75)], rel=REL)
FANS = [(30, 60, 90, 120, 150), (45, 75, 105, 135, 165), (15, 45, 75, 105, 135), (35, 65, 95, 125, 155)]
for c, rb in enumerate(REL):
    for x in (W / 4, W / 2, W * 3 / 4):
        for a in FANS[c]: ev(rb - t[1] * EZW, 'L14shot', x=x, y=46, ang=a, r=15, nb=1, hit=rb, wn=t[1], sp=t[0])
# the drain (1:30): the balls pour down into the drain on the 3-2-3
t = T['drain']
deco(4, 180, 197)
for g in range(90, 97):
    for k in range(3):
        b = acc(g, k)
        if k == 1: fallP(b, 60 if g % 2 == 0 else -60, t, 1.1, 30)
        else: fall(b, rx(120, W - 120), t, 1.1, 30)
        if g < 96: fallP(b + 0.5, -150 if g % 2 == 0 else 150, t, 1.0, 30)
ev(192, 'L14word', s='REPLAY', t0=192, t1=199, c=26, y=H / 2 - 91, cyan=True)

# energy per bar (the beat pulse), from the mp3's loudness
ENERGY = [0.43, 0.43, 0.46, 0.43, 0.39, 0.39, 0.42, 0.57, 0.61, 0.58, 0.58, 0.58, 0.60, 0.63, 0.66, 0.60, 0.65, 0.63, 0.67, 0.65, 0.66, 0.67, 0.67,
          0.67, 0.70, 0.72, 0.72, 0.71, 0.69, 0.68, 0.72, 0.57, 0.17, 0.20, 0.15, 0.16, 0.49, 0.87, 1.00, 0.89, 0.89, 0.87, 1.00, 0.88, 0.84, 0.29, 0.27,
          0.27, 0.27, 0.12]

EV.sort(key=lambda e: e['b'])
doc = {'id': 'L14', 'name': 'المرحلة الرابعة عشرة', 'song': 'Jump again', 'file': 'SONG_L14', 'bpm': 120, 'off': 0.315, 'end': 197,
       'cps': [0, 64, 148], 'ez': {'ezg': 1, 'ezc': 1, 'ez': 0.5}, 'energy': ENERGY, 'events': EV}
with open(OUT, 'w', encoding='utf-8') as f_:
    json.dump(doc, f_, ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), '->', os.path.normpath(OUT))
