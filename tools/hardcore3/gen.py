#!/usr/bin/env python3
# Makes data/dodge_H3.json: Hardcore level 3 "Raining Teddy Bears" (Black Ops Zombies), Samantha's room. 112 BPM, beat 0 = 0.09 s.
# Song map (beats; a bar is 4): the music box alone 0-15 | the bass comes in 16-35 (0:08.7) | the chords go round in 12 bars:
# 36-83 (0:19.4) | 84-131 (0:45.1) | 132-179 (1:10.8) | 180-207 (1:36.5) | the last notes 208-212 | win 212 (1:53.7)
# Parts: 1 the room (0-83), 2 the box (84-131), 3 the nightmare (132-212). Checkpoints 0, 84, 132.
# Nothing here is random: every attack is a fixed pattern on the beat (the scissors lock on where you are: "rnd").
# An event fires when its warning starts; times are beats. Hardcore has no Easy.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_H3.json')
SONG = json.load(open(os.path.join(HERE, 'song.json')))
W, H = 1280, 720
CX, CY = W / 2, H / 2
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def stage(part, b0, b1): ev(b0, 'H3stage', part=part, to=b1, keep=True)
def fx(b, v=0.3, s=9, dur=1.2): ev(b, 'H3fx', v=v, s=s, dur=dur)

# ================= part 1: the room (0-83) =================
stage(1, 0, 84)

# 0:00 the music box: every note of the tune drops a pin down a column. The column is the note's pitch, and the cylinder turns one
# column every half beat, so a tune that stays on one note walks across the room. From 0:08.7 the tune again, mirrored
notes = [(b, m) for b, m in SONG['notes'] if b >= 1.25]
def pcol(b, m): return ((m - 66) + int(round(b * 2))) % 20
tn, col = [], []
# the first time each pin is two columns wide
for b, m in notes: tn += [b, b]; col += [pcol(b, m), (pcol(b, m) + 1) % 20]
for b, m in notes:
    if b + 16 > 33: continue
    # the second time every note hits both halves of the room (mirrored)
    tn += [b + 16, b + 16]; col += [19 - pcol(b, m), pcol(b, m)]
ev(0, 'H3pins', tn=tn, col=col, v=1350, len=190, wn=0.75, to=35)

# 0:08.7 the teddy rain: bears land on a grid, the next ones on the other grid (half a step over): hop from one to the other. First
# every two beats, from 0:11.3 on every beat. The wind pushes the grids along
def rain(b0, tl, par, s=240, R=112, vx=40, vy=-25, wn=1.5, hold=0.35):
    ev(b0, 'H3rain', tl=tl, par=par, s=s, R=R, ox=CX, oy=CY, vx=vx, vy=vy, wn=wn, hold=hold)
tl = [18, 20] + list(range(21, 36)); rain(16.5, tl, [i % 2 for i in range(len(tl))])

# 0:19.4 the jump rope: it sweeps the whole room every two beats. No gap: dash through it. The teddy rain lands on the beats between:
# dash, hop, dash, hop.
# From 0:25.8 a second rope stands up and sweeps across, taking turns with the first
def rope(axis, ts, dirs, sd=0.35, th=14, wn=0.6): ev(ts[0] - wn - 0.1, 'H3rope', axis=axis, ts=ts, dir=dirs, sd=sd, th=th, wn=wn)
ts = list(range(37, 48, 2)); rope(0, ts, [1 if (b // 4) % 2 == 0 else -1 for b in ts])
tl = [38 + 2 * i for i in range(5)]; rain(36.4, tl, [2 + i % 2 for i in range(len(tl))], vx=-30, vy=30)
ts = list(range(49, 60, 4)); rope(0, ts, [1 if (b // 4) % 2 == 0 else -1 for b in ts])
ts = list(range(51, 60, 4)); rope(1, ts, [1 if (b // 4) % 2 == 0 else -1 for b in ts])
tl = [50 + 2 * i for i in range(5)]; rain(48.4, tl, [i % 2 for i in range(len(tl))], vx=35, vy=35)

# 0:32 the needle sews the room shut: three walls every three beats; the button shows the room that stays, every other room fills with
# stuffing. Run to the button before the walls close the way (the last pattern's walls are still up when the next starts)
# (made with tools/hardcore3/needle_search2.py: from the last button to the next one is 240-400 px, the room that stays is 4-14% of the
# screen, and the way crosses only walls the needle reaches at least 0.1 s after you can be there)
NEEDLE = [
    ([(640, 0, 640, 720), (320, 720, 320, 0), (0, 480, 1280, 480)], (560, 600)),
    ([(0, 480, 1280, 480), (0, 0, 1280, 720), (640, 720, 640, 0)], (880, 600)),
    ([(0, 360, 1280, 360), (1280, 180, 0, 180), (960, 720, 960, 0)], (1120, 600)),
    ([(1280, 0, 0, 720), (640, 0, 640, 720), (0, 0, 1280, 720)], (800, 540)),
    ([(0, 720, 1280, 0), (320, 720, 320, 0), (640, 0, 640, 720)], (560, 540)),
    ([(1280, 180, 0, 180), (850, 0, 850, 720), (1280, 720, 0, 0)], (720, 300)),
    ([(320, 720, 320, 0), (1280, 0, 0, 720), (0, 0, 1280, 720)], (480, 360)),
    ([(850, 0, 850, 720), (0, 360, 1280, 360), (640, 720, 640, 0)], (720, 240)),
]
for i, (walls, (sx, sy)) in enumerate(NEEDLE):
    pb = 60 + 3 * i
    ev(pb - 1, 'H3needle', x0=[w[0] for w in walls], y0=[w[1] for w in walls], x1=[w[2] for w in walls], y1=[w[3] for w in walls],
       ws=[pb, pb + 0.7, pb + 1.4], wd=0.5, sx=sx, sy=sy, tf=pb + 2.1, tu=pb + 3)

# ================= part 2: the box (84-131) =================
stage(2, 84, 132); fx(84)

# 0:45 the mystery box: the pointer runs round 12 slices and slows down (2 slices a step, then 1, then it stops); every slice but the
# last one blasts. Read where it stops
def wheel(sb, s0, steps):
    tk, sk, s = [], [], s0
    for i, st in enumerate([0] + steps): s = (s + st) % 12; tk.append(sb + 0.25 * i); sk.append(s)
    ev(sb - 0.4, 'H3wheel', n=12, bR=66, tk=tk, sk=sk, tb=sb + 2.45, hold=0.45)
wheel(85, 0, [2, 2, 2, 2, 1, 1, 1, 1])
wheel(88, 5, [2, 2, 2, 2, 1, 1, 1, 1, 1])
wheel(91, 11, [2, 2, 2, 2, 2, 1, 1, 1])
wheel(94, 5, [2, 2, 2, 2, 2, 1, 1, 1, 1])

# 0:51.5 the bear tears: it runs in and tears in two every beat, four times; the pieces fan out
def split(hit, x, y, a, v=420, dt=0.75, sp=50, dep=4, r=52, wn=1.5): ev(hit - wn, 'H3split', hit=hit, x=x, y=y, a=a, v=v, dt=dt, sp=sp, dep=dep, r=r)
split(97, -40, CY, 0); split(98, W + 40, CY - 200, 180)
split(99, W + 40, CY, 180); split(100, -40, CY + 200, 0)
split(101, -40, -40, 30); split(101, W + 40, H + 40, 210); split(102, CX, -40, 90, sp=55)
split(103, W + 40, -40, 150); split(103, -40, H + 40, -30); split(104.5, -40, CY, 0, sp=55)
split(104, CX, -40, 90, sp=60); split(104, CX, H + 40, -90, sp=60)
split(105, -40, CY, 0, sp=40); split(105, W + 40, CY, 180, sp=40)
split(106, -40, -40, 35, sp=45); split(106, W + 40, -40, 145, sp=45); split(107, -40, H + 40, -35, sp=45); split(107, W + 40, H + 40, 215, sp=45)

def snip(tl_, a_, tc=0.65): ev(tl_ - 1, 'H3snip', rnd=True, ang=a_, D=270, op=28, L=760, w=16, tl=tl_, tc=tl_ + tc, cd=0.2, hold=0.25)
# 0:58 the scissors: each one follows you, locks on where you are, then shuts 0.65 beat later. A new pair on every beat, from a new side:
# keep moving across the blades
for k in range(12):
    tl_ = 108.5 + k
    a_ = (k * 135) % 360
    ev(tl_ - 1, 'H3snip', rnd=True, ang=a_, D=270, op=28, L=760, w=16, tl=tl_, tc=tl_ + 0.65, cd=0.2, hold=0.25)
    # the last six come in pairs, the second from the other side
    if k >= 6: ev(tl_ - 1, 'H3snip', rnd=True, ang=(a_ + 180) % 360, D=270, op=28, L=760, w=16, tl=tl_, tc=tl_ + 0.65, cd=0.2, hold=0.25)

# 1:04.4 the carousel: three rings of horses turning (the middle one the other way). On every half beat one set of rings drops its
# horses. The hub grows and the fence closes in: change rings
ev(119, 'H3carousel', hit=120, to=132, rr=[120, 210, 300], np=[7, 10, 13], R=42, dn=0.445,
   r0a0=0, r0sb=[119, 122, 125, 128], r0sw=[0.55, -0.6, 0.65, -0.7],
   r1a0=20, r1sb=[119, 122, 125, 128], r1sw=[-0.45, 0.5, -0.55, 0.6],
   r2a0=0, r2sb=[119, 122, 125, 128], r2sw=[0.36, -0.4, 0.44, -0.48],
   hb=[119, 122, 125, 128, 130], hr=[60, 150, 60, 150, 60], rb=[119, 123.5, 126.5, 129], rm=[420, 262, 420, 262])

# ================= part 3: the nightmare (132-212) =================
stage(3, 132, 212); fx(132, v=0.35, s=10)

# 1:10.8 the mirror: the room turns over left to right (and back) on these beats. A beat before, it cracks and the box chimes
ev(132, 'H3flip', fb=[133.5, 140, 144, 148, 150, 152])

# 1:10.8 the spinning tops: they bounce off the walls, faster after every bounce; their next two runs show
def top(hit, x, y, a, v=540, nb=6, R=28, wn=1.5): ev(hit - wn, 'H3top', hit=hit, x=x, y=y, a=a, v=v, nb=nb, ac=1.12, R=R)
for (x, y, a) in [(90, 90, 22), (W - 90, 90, 158), (W - 90, H - 90, 202), (90, H - 90, -22)]: top(133.5, x, y, a)
for (x, y, a) in [(CX, 60, 60), (CX, H - 60, -120)]: top(137, x, y, a)
for (x, y, a) in [(60, CY, -40), (W - 60, CY, 140), (CX, 60, 120), (CX, H - 60, -60)]: top(141, x, y, a)
for (x, y, a) in [(90, 90, 40), (W - 90, H - 90, 220), (W - 90, 90, 130), (90, H - 90, -50)]: top(145, x, y, a)
for (x, y, a) in [(CX, 60, 75), (CX, H - 60, -105), (60, CY, 20), (W - 60, CY, 200)]: top(148, x, y, a)
# the scissors come back while the room is mirrored
for k, b in enumerate(range(141, 152, 2)): snip(b + 0.5, (k * 135 + 45) % 360)
tl = [135, 137, 139] + list(range(140, 152)); rain(133, tl, [i % 2 for i in range(len(tl))], s=250, R=108, vx=-30, vy=20)

# 1:21.5 the cat's cradle: seven strings between two hands. Every beat some strings jump to other fingers
# (the new ones show a beat before); the whole cradle turns, and turns back
FY = [-300, -200, -100, 0, 100, 200, 300]
# each change swaps strings in a fixed order (two swaps, from 1:26 on every beat)
SW = [(0, 2), (3, 6), (1, 4), (2, 5), (0, 6), (1, 3), (4, 5), (0, 3), (2, 6), (1, 5), (0, 4), (3, 5)]
PM = [list(range(7))]
for i in range(27):
    p_ = PM[-1][:]; a_, b_ = SW[(2 * i) % len(SW)]; c_, d_ = SW[(2 * i + 1) % len(SW)]
    p_[a_], p_[b_] = p_[b_], p_[a_]; p_[c_], p_[d_] = p_[d_], p_[c_]; PM.append(p_)
cb = [151] + [153 + i for i in range(len(PM) - 1)]
ev(150, 'H3cradle', hit=152, to=180, fy=FY, d=430, pm=[v for p in PM for v in p], cb=cb, th=12,
   a0=0, sb=[150, 156, 162, 168, 174], sw=[0.26, -0.38, 0.48, -0.56, 0.62])
# and the scissors through the strings
for k, b in enumerate(range(154, 179, 3)): snip(b + 0.5, (k * 135 + 30) % 360)
for k, b in enumerate([156, 160, 164, 168, 172, 176]):
    for (x, y, a) in ([(90, 90, 30), (W - 90, H - 90, 210)] if k % 2 == 0 else [(W - 90, 90, 150), (90, H - 90, -30)]): top(b, x, y, a, v=480, nb=5)

# 1:36.5 the end: all of it, in order. The rope every two beats and the rain between; the mirror; the bear that tears; then everything
fx(180, v=0.3, s=10)
ts = list(range(181, 188, 2)); rope(0, ts, [1] * len(ts))
tl = [182 + 2 * i for i in range(4)]; rain(180.4, tl, [i % 2 for i in range(len(tl))], vx=45, vy=-30)
ev(182, 'H3flip', fb=[184, 188, 192, 196, 200, 204])
ts = list(range(189, 196, 2)); rope(1, ts, [1 if i % 2 == 0 else -1 for i in range(len(ts))])
split(189, -40, CY, 0, sp=45, v=470); split(191, W + 40, CY, 180, sp=45, v=470)
split(193, CX, -40, 90, sp=55, v=470); split(195, CX, H + 40, -90, sp=55, v=470)
ts = list(range(197, 208, 2)); rope(0, ts, [1 if (b // 4) % 2 == 0 else -1 for b in ts])
tl = [198 + 2 * i for i in range(5)]; rain(196.4, tl, [2 + i % 2 for i in range(len(tl))], vx=-40, vy=-30)
split(199, -40, -40, 30, sp=40); split(203, W + 40, H + 40, 210, sp=40)
for (x, y, a) in [(90, 90, 35), (W - 90, H - 90, 215)]: top(198, x, y, a)
for (x, y, a) in [(W - 90, 90, 145), (90, H - 90, -35)]: top(202, x, y, a)

# the scissors again, on the half beats between the rope and the rain
for k, b in enumerate(range(188, 207, 2)): snip(b + 0.5, (k * 135 + 90) % 360)

# 1:51.5 bye bye
ev(208, 'H3bye', to=211.5)

EV.sort(key=lambda d: d['b'])
D = {'id': 'H3', 'name': 'هاردكور: المرحلة الثالثة', 'song': 'Raining Teddy Bears', 'file': 'SONG_H3', 'bpm': 112, 'off': 0.09, 'end': 212, 'cps': [0, 84, 132],
     'ez': {}, 'energy': SONG['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'pins', len(tn))
