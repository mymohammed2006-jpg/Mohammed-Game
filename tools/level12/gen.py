#!/usr/bin/env python3
# Makes data/dodge_L12.json: L12 "Hard Spike" (Neon Beats). 128 BPM, beat 0 = 0.047 s, a bar = 4 beats = 1.875 s.
# Song map (beats): intro 0-15 | drop A 16-47, hats 48-63 | drum build 64-79 | break 80-83 | drop B 84-115 | 116-119 down
# | quiet 120-147 | build 148-163 | drop A' 164-195 | build 196-211 | break 212-215 | drop B' 216-247 | music stops 248 (win)
# Phases: 1 the ground floor (0-83), 2 the crystal mine (84-163), 3 the summit and the hedgehog (164-248).
# Times written as "hit" beats; an event fires when its warning starts (hit - wn).
import json, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L12.json')
W, H = 1280, 720
R = random.Random(1212)
EV = []

def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}; d.update(kw); EV.append(d)

# the warning gets shorter as the drums speed up (beats)
WN = {'calm': 1.875, 'drop': 1.625, 'fast': 1.375, 'rush': 1.125}

def bed(hit, e='b', d=140, step=0, dir=1, gaps=(), sw=80, dur=0.75, wn=1.5, u0=None, u1=None):
    kw = dict(e=e, d=d, hit=hit, step=step, dir=dir, gaps=list(gaps), sw=sw, dur=dur, wn=wn)
    if u0 is not None: kw['u0'] = u0
    if u1 is not None: kw['u1'] = u1
    ev(hit - wn, 'bed', **kw)

def crush(hit, e, a, b, d, dur=0.75, wn=1.5, boom=False, slide=0):
    kw = dict(e=e, u0=a, u1=b, d=d, hit=hit, dur=dur, wn=wn)
    if boom: kw['boom'] = True
    if slide: kw['slide'] = slide
    ev(hit - wn, 'crush', **kw)

def lance(hit, e, c=None, aim=False, off=0, w=56, ln=420, dur=0.6, wn=1.5):
    kw = dict(e=e, hit=hit, w=w, len=ln, dur=dur, wn=wn)
    if aim: kw['aim'] = True; kw['off'] = off
    else: kw['c'] = c
    ev(hit - wn, 'lance', **kw)

def stal(hit, x=None, aim=False, off=0, n=6, sp=330, fall=0.5, wn=1.5, w=54, ln=150):
    kw = dict(hit=hit, n=n, sp=sp, fall=fall, wn=wn, w=w, len=ln)
    if aim: kw['aim'] = True; kw['off'] = off
    else: kw['x'] = x
    ev(hit - fall - wn, 'stal', **kw)

def gear(t0, t1, x0, y0, x1, y1, R_=46, hop=0, wn=1.5, hb=0):
    ev(t0 - wn, 'gear', t0=t0, t1=t1, x0=x0, y0=y0, x1=x1, y1=y1, R=R_, hop=hop, wn=wn, hb=hb)

def grow(t0, x=0, y=0, aim=False, ox=0, oy=0, Rr=90, g=1, hold=1, shrink=0.5, n=9, burst=0, sp=300, wn=1.5, life=1.0):
    kw = dict(t0=t0, R=Rr, g=g, hold=hold, shrink=shrink, n=n, wn=wn)
    if aim: kw.update(aim=True, ox=ox, oy=oy)
    else: kw.update(x=x, y=y)
    if burst: kw.update(burst=burst, sp=sp, life=life)
    ev(t0 - wn, 'grow', **kw)

def hogq(b, m, **kw): ev(b, 'hogq', m=m, **kw)
def roll(t0, t1, pts, wn=1.5, stuck=0, stay=1.5): ev(t0 - wn, 'roll', t0=t0, t1=t1, pts=[round(p, 1) for p in pts], wn=wn, stuck=stuck, stay=stay)

# =========================== phase 1: the ground floor (0:00 - 0:39) ===========================
ev(0, 'bg', ph=1, to=83, rise=80, keep=True)
w = WN['calm']
# intro: the floor wakes up. Waves of floor teeth run under the synth, the ceiling answers
bed(2, 'b', d=120, step=0.25, dir=1, wn=w, gaps=(7, 8))
bed(6, 't', d=120, step=0.25, dir=-1, wn=w, gaps=(4, 5))
for i, b in enumerate((4, 6, 8)):
    lance(b, 'b' if i % 2 == 0 else 't', aim=True, w=60, ln=430, wn=w)
for i, b in enumerate((10, 11, 12, 13)):
    lance(b, 'l' if i % 2 == 0 else 'r', aim=True, w=60, ln=520, wn=w)
bed(14, 'b', d=110, sw=160, step=0, wn=w, gaps=(1, 4, 6))

# drop A (0:07.5): the crushers come down with the kick
w = WN['drop']
for bar in range(4, 6):
    b0 = bar * 4
    for k in range(4):
        side = (k + bar * 2) % 3   # one crusher a beat, a third of the ceiling, moving along
        crush(b0 + k, 't', side * W / 3, (side + 1) * W / 3, 220, dur=0.5, wn=w, boom=k == 0)
        if k % 2: bed(b0 + k + 0.5, 'b', d=150, sw=160, wn=w, gaps=(R.randrange(6),), dur=0.4)
        if k % 2 == 0: lance(b0 + k + 0.5, 'r', aim=True, off=R.choice((-40, 40)), ln=900, w=50, wn=w)
for bar in range(6, 8):
    b0 = bar * 4
    g0 = 260 if bar == 6 else 600   # the walls close in, the gap is somewhere else each bar
    crush(b0, 'l', 0, H, g0, dur=3, wn=w, boom=True)
    crush(b0, 'r', 0, H, W - g0 - 420, dur=3, wn=w)
    for k in (0, 2):
        if k % 2 == 0: lance(b0 + k + 0.5, 'b' if k % 4 == 0 else 't', aim=True, ln=380, wn=w)
for bar in range(8, 10):
    b0 = bar * 4
    bed(b0, 'b', d=200, step=0.25, dir=1 if bar == 8 else -1, wn=w, gaps=(R.randrange(3, 13),))
    bed(b0 + 1, 't', d=200, step=0.25, dir=-1 if bar == 8 else 1, wn=w, gaps=(R.randrange(3, 13),))
    for k in (0, 1, 2, 3): lance(b0 + k + 0.5, 'lrlr'[k], aim=True, off=(0, 50, -50, 0)[k], ln=600, wn=w)
# the jaws: top and bottom close with a gap that walks
for bar in range(10, 12):
    b0 = bar * 4
    gc = 460 if bar == 10 else 820
    for e in ('t', 'b'):
        crush(b0, e, 0, gc - 120, 300, dur=2.5, wn=w, boom=e == 't')
        crush(b0, e, gc + 120, W, 300, dur=2.5, wn=w)
    lance(b0 + 3, 'l' if bar == 10 else 'r', aim=True, ln=900, wn=w)
# the hats come in (0:22.5): floor and ceiling stab in turns, a zigzag you have to follow
for bar in range(12, 14):
    b0 = bar * 4
    for k in (0, 2):
        odd = (k // 2 + bar) % 2
        bed(b0 + k, 'b', d=470, sw=160, wn=w, dur=1.0, gaps=[i for i in range(8) if i % 2 != odd])
        bed(b0 + k, 't', d=470, sw=160, wn=w, dur=1.0, gaps=[i for i in range(8) if i % 2 == odd])
# lances from all four walls on the eighths
for bar in range(14, 16):
    b0 = bar * 4
    for k in range(4):
        lance(b0 + k, 'lrbt'[k % 4], aim=True, off=(-60 if k % 3 == 0 else 60), ln=560 if k % 4 < 2 else 380, wn=w)
    crush(b0 + 2, 't' if bar == 14 else 'b', 0, W, 110, dur=1.5, wn=w)

# the drum build (0:30): spike columns sweep across, faster and faster, one column stays down
GAP = [4]
def sweep(b0, step, dir, wn, cols=8):
    gap = GAP[0] = max(1, min(cols - 2, GAP[0] + R.choice((-1, 0, 1))))   # the column that stays down moves at most one over
    for i in range(cols):
        c = i if dir > 0 else cols - 1 - i
        if c in (gap,): continue
        x0 = c * (W / cols)
        crush(b0 + i * step, 'b', x0 + 4, x0 + W / cols - 4, H - 24, dur=max(0.35, step * 1.5), wn=wn)
sweep(64, 0.5, 1, WN['drop'])
sweep(68, 0.5, -1, WN['fast'])
sweep(72, 0.5, 1, WN['fast'])
sweep(76, 0.375, -1, WN['rush'])
# the break (0:37.5): everything goes back, the tower rises
ev(80, 'flash', v=0.35, s=10)

# =========================== phase 2: the crystal mine (0:39 - 1:17) ===========================
ev(82, 'bg', ph=2, to=163, rise=162, keep=True)
w = WN['drop']
# the strong hits of drop B on the 16ths (from the song): X = big
PAT = {21: 'X...X.xXx.x.Xxxx', 22: 'X..xX.xxXxXxXxx.', 23: 'XxxXXxXXXxxxX.xx', 24: 'XxXXX.xxxxxxxxx.',
       25: 'X...XxxXx.xxXx..', 26: 'X...X.xxxXXxx.xx', 27: 'XxXXX.XXXxXxXxxx', 28: 'XxXXX.xxxXxxx.x.'}
# bars 21-22: crystals rain on the big hits, walking across; a gear rolls the floor
for bar in (21, 22):
    b0 = bar * 4; x = 120 if bar == 21 else W - 120
    for i, c in enumerate(PAT[bar]):
        if c != 'X': continue
        stal(b0 + i * 0.25, x=x, n=5, wn=w); x += (150 if bar == 21 else -150)
        if x > W - 80 or x < 80: x = 160 if bar == 21 else W - 160
    gear(b0, b0 + 4, -60 if bar == 21 else W + 60, H - 50, W + 60 if bar == 21 else -60, H - 50, R_=50, hop=110, wn=w, hb=0)
    for k in (1, 3): lance(b0 + k, 'l' if (k + bar) % 2 else 'r', aim=True, ln=640, wn=w)
# bars 23-24: crystals grow near you and burst on the big hits; a gear on the ceiling
for bar in (23, 24):
    b0 = bar * 4; n = 0
    for i, c in enumerate(PAT[bar]):
        if c != 'X' or n >= 4: continue
        if i % 2: continue
        n += 1; grow(b0 + i * 0.25 - 0.5, aim=True, ox=R.choice((-150, 150)), oy=R.choice((-110, 110)), Rr=80, g=0.5, hold=0.0, burst=8, sp=260, wn=w)
    gear(b0, b0 + 4, W + 60 if bar == 23 else -60, 50, -60 if bar == 23 else W + 60, 50, R_=50, hop=0, wn=w)
    lance(b0 + 2, 'l' if bar == 23 else 'r', aim=True, ln=700, wn=w)
# bars 25-26: crystals fall right above you, the walls stab back
for bar in (25, 26):
    b0 = bar * 4
    for i, c in enumerate(PAT[bar]):
        if c == 'X': stal(b0 + i * 0.25, aim=True, off=R.choice((-40, 40)), n=5, wn=w)
    bed(b0 + 1, 'l', d=180, sw=90, step=0.25, dir=1, wn=w, gaps=(R.randrange(2, 6),))
    bed(b0 + 3, 'r', d=180, sw=90, step=0.25, dir=-1, wn=w, gaps=(R.randrange(2, 6),))
    gear(b0, b0 + 4, -60 if bar == 25 else W + 60, 60, W + 60 if bar == 25 else -60, H - 60, R_=46, wn=w)
# bars 27-28: two gears cross the room diagonally, rows of crystals with a hole
for bar in (27, 28):
    b0 = bar * 4
    gear(b0, b0 + 4, -60, 80 if bar == 27 else H - 80, W + 60, H - 80 if bar == 27 else 80, R_=56, wn=w)
    gear(b0 + 2, b0 + 6, W + 60, 120, -60, H - 120, R_=44, wn=w)
    for k in (0, 1, 2, 3):
        hole = R.randrange(1, 7)
        for j in range(8):
            if j in (hole, hole + 1): continue
            stal(b0 + k + 1, x=80 + j * 160, n=3, sp=260, wn=w, w=60)
# 0:54: the bass goes, one big crystal in the middle bursts and the lights go out
grow(116, x=W / 2, y=H / 2, Rr=170, g=1.5, hold=0.5, burst=20, sp=280, wn=w, life=1.6)
ev(119, 'dark', v=0.88)
# the quiet (0:56 - 1:09): in the dark the walls creep in and crystals grow slowly around you
w = WN['calm']
crush(122, 'l', 0, H, 330, dur=10, wn=2, slide=8)
crush(122, 'r', 0, H, 330, dur=10, wn=2, slide=8)
spots = [(470, 180), (810, 540), (640, 360), (470, 540), (810, 180), (560, 300), (720, 420), (640, 150), (640, 570)]
for i in range(9):
    x, y = spots[i]
    grow(120 + i * 2.5, x=x, y=y, Rr=110, g=2, hold=3, shrink=1, n=11, wn=2)
for b in (121, 125, 129, 133): grow(b, aim=True, Rr=85, g=1.5, hold=1, shrink=1, n=10, wn=2)
crush(136, 't', 0, W, 160, dur=8, wn=2, slide=6)
crush(136, 'b', 0, W, 160, dur=8, wn=2, slide=6)
for i in range(4):
    grow(137 + i * 2.5, aim=True, ox=0, oy=0, Rr=95, g=1.5, hold=1, shrink=1, n=10, wn=2)
gear(126, 134, W / 2, -60, W / 2, H + 60, R_=40, wn=2)
gear(130, 138, -60, H / 2, W + 60, H / 2, R_=40, wn=2)
gear(140, 147, W + 60, 200, -60, 520, R_=40, wn=2)
ev(146, 'dark', v=0)
# the build (1:09): a fuse of crystals runs across the room, each one bursts into a small ring, faster with the drums
def fuse(b0, step, n, pts, wn, ring=6, sp=280):
    for i in range(n):
        x, y = pts[i % len(pts)]
        grow(b0 + i * step - 0.5, x=x, y=y, Rr=64, g=0.5, hold=0, burst=ring, sp=sp, wn=wn, life=0.75)
zig = [(160 + i * 120, 200 if i % 2 == 0 else 520) for i in range(9)]
fuse(148, 1, 4, zig[:4], WN['drop'])
fuse(152, 0.5, 8, zig[1:9][::-1], WN['fast'])
ring_pts = [(640 + 300 * __import__('math').cos(i * 0.785), 360 + 220 * __import__('math').sin(i * 0.785)) for i in range(8)]
fuse(156, 0.5, 8, ring_pts, WN['fast'], ring=5, sp=260)
for i in range(4): stal(160 + i * 0.5, x=80 + ((i * 3) % 8) * 160, n=4, sp=240, wn=WN['rush'])
ev(162, 'flash', v=0.3, s=8)

# =========================== phase 3: the summit and the hedgehog (1:17 - 1:56) ===========================
ev(162, 'bg', ph=3, to=250, keep=True)
ev(162, 'hog', x=248, keep=True)
w = WN['drop']
HX, HY = W / 2, 120
# drop A': fans on 1 and 3, aimed volleys on 2 and 4, the floor stabs where it stomps
for bar in (41, 42):
    b0 = bar * 4
    for k in range(4):
        if k % 2 == 0: hogq(b0 + k, 'fan', n=11, a=90, spr=14, sp=280)
        else: hogq(b0 + k, 'aim', n=5, spr=10, sp=340)
    lance(b0 + 0.5, 'l', aim=True, ln=700, wn=w)
    lance(b0 + 2.5, 'r', aim=True, ln=700, wn=w)
    bed(b0 + 1, 'b', d=150, sw=160, wn=w, gaps=(R.randrange(8),), dur=0.5)
    bed(b0 + 3, 'b', d=150, sw=160, wn=w, gaps=(R.randrange(8),), dur=0.5)
# it curls up and rolls along the floor and back, leaving quills
roll(172, 176, [HX, HY, 80, H - 70, W - 80, H - 70, HX, HY], wn=w, stuck=120, stay=2)
for k in (0, 2): bed(172.5 + k, 't', d=170, sw=160, wn=w, gaps=(R.randrange(1, 7),), dur=0.5)
hogq(176, 'ring', n=18, sp=260)
roll(178, 180, [HX, HY, W - 120, 380, HX, HY], wn=w, stuck=0)
hogq(180, 'ring', n=18, sp=260, rot=10)
# rings and side crushers
for bar in (45, 46):
    b0 = bar * 4
    hogq(b0, 'ring', n=20, sp=250, rot=bar * 9)
    hogq(b0 + 2, 'aim', n=5, spr=12, sp=360)
    crush(b0 + 1, 'l' if bar == 45 else 'r', 0, H, 380, dur=1, wn=w, boom=True)
    hogq(b0 + 3, 'fan', n=7, a=60 if bar == 45 else 120, spr=12, sp=320)
    bed(b0 + 0.5, 'b', d=160, sw=160, step=0.25, dir=1 if bar == 45 else -1, wn=w, gaps=(R.randrange(1, 7),), dur=0.5)
    bed(b0 + 2.5, 't' if bar == 45 else 'b', d=160, sw=160, step=0.25, dir=-1 if bar == 45 else 1, wn=w, gaps=(R.randrange(1, 7),), dur=0.5)
# around the whole room along the walls
roll(188, 194, [HX, HY, 80, 120, 80, H - 70, W - 80, H - 70, W - 80, 120, HX, HY], wn=w, stuck=140, stay=2)
hogq(194, 'fan', n=11, a=90, spr=15, sp=300)
for b in (189, 190, 191, 192, 193): stal(b, aim=True, off=R.choice((-30, 30)), n=4, sp=260, wn=w)
# the build (1:32): it spins and throws a spiral, faster with the drums
hogq(196, 'spiral', arms=3, t0=196, t1=200, every=1, turn=30, sp=220)
hogq(200, 'spiral', arms=3, t0=200, t1=204, every=0.5, turn=-26, sp=230, rot=40)
hogq(204, 'spiral', arms=2, t0=204, t1=208, every=0.25, turn=22, sp=250)
hogq(208, 'spiral', arms=2, t0=208, t1=211.5, every=0.25, turn=-30, sp=250, rot=90)
for b in (207, 211):
    hole = R.randrange(1, 7)
    for j in range(8):
        if j not in (hole, hole + 1): stal(b, x=80 + j * 160, n=0, wn=WN['fast'], w=50)
ev(212, 'flash', v=0.3, s=8)
# drop B' (1:41): everything from the tower at once, changing every two bars
w = WN['fast']
for bar in (54, 55):
    b0 = bar * 4
    roll(b0, b0 + 2.5, [HX, HY, 100, H - 80, W - 100, 160, HX, HY] if bar == 54 else [HX, HY, W - 100, H - 80, 100, 160, HX, HY], wn=w, stuck=160, stay=1.5)
    hogq(b0 + 3, 'ring', n=16, sp=280, rot=bar * 11)
    for i, c in enumerate(PAT[bar - 33]):
        if c == 'X' and i % 2 == 0 and i % 4 != 0: stal(b0 + i * 0.25, aim=i % 8 == 6, x=R.randrange(100, W - 100), n=4, sp=250, wn=w)
for bar in (56, 57):
    b0 = bar * 4
    for k in range(4):
        gc = 200 + R.randrange(0, 880)
        crush(b0 + k, 'b', 0, gc - 100, 220, dur=0.5, wn=w)
        crush(b0 + k, 'b', gc + 100, W, 220, dur=0.5, wn=w)
        hogq(b0 + k + 0.5, 'aim' if k % 2 else 'fan', n=5 if k % 2 else 9, spr=12 if k % 2 else 16, a=90, sp=300)
for bar in (58, 59):
    b0 = bar * 4
    gear(b0, b0 + 3, -60, H - 50, W + 60, H - 50, R_=52, hop=140, wn=w)
    gear(b0 + 1, b0 + 4, W + 60, 260, -60, 260, R_=44, hop=0, wn=w)
    hogq(b0, 'ring', n=14, sp=260, rot=bar * 7)
    hogq(b0 + 2, 'aim', n=3, spr=8, sp=320)
    lance(b0 + 3, 'l' if bar == 58 else 'r', aim=True, ln=760, wn=w)
# the last two bars: one last roll around the room, then it charges up and bursts with the music (1:56.3)
roll(240, 244, [HX, HY, 80, H - 70, W - 80, H - 70, W - 80, 120, 80, 120, HX, HY], wn=w, stuck=150, stay=1.5)
hogq(244, 'fan', n=13, a=90, spr=14, sp=320)
hogq(245, 'aim', n=5, spr=10, sp=380)

EV.sort(key=lambda d: d['b'])
# "rnd": the event depends on the run (aimed at the square, or random shards): the human bot only sees it 0.32-0.40 s after it starts
for d in EV:
    k = d['k']
    if d.get('aim') or (k == 'stal' and d.get('n', 6) > 0) or (k == 'grow' and d.get('burst', 0) > 0) \
            or (k == 'roll' and d.get('stuck', 0) > 0) or (k == 'hogq' and d.get('m') == 'aim'):
        d['rnd'] = True
print(sum(1 for d in EV if d.get('rnd')), 'rnd events')
energy = json.load(open(os.path.join(HERE, 'energy.json')))
energy = [e if e > 0.12 else 0.25 for e in energy]
doc = dict(id='L12', name='المرحلة الثانية عشرة', song='Hard Spike', file='SONG_L12', bpm=128, off=0.047, end=248, cps=[0, 84, 164],
           ez=dict(ezg=1, ezc=1, ez=0.5), energy=energy, events=EV)
json.dump(doc, open(OUT, 'w'), ensure_ascii=False, indent=0)
print(len(EV), 'events ->', os.path.normpath(OUT))
