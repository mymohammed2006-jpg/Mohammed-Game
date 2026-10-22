#!/usr/bin/env python3
# Makes data/dodge_H2.json: Hardcore level 2 "Laughing Corpses" (Black Ops Zombies), the graveyard under siege. 90 BPM, beat 0 = 0.115 s.
# Song map (beats): quiet start 0-29 | drums 30-62 (0:20) | stop 63-65 (0:42) | 66-98 (0:44) | the quiet 99-113 (1:06)
# | 114-186 (1:16, loudest 170-178) | the quiet end 187-202 (2:05) | win 203 (2:15.4)
# Parts: 1 the graveyard (0-65), 2 the rounds (66-113), 3 the last round (114-203). Checkpoints 0, 66, 114.
# Nothing here is random: every fixed attack is a pattern on the beat. The horde, the hounds and the haunted copy follow the square
# ("rnd": the bot only sees them after a human's reaction time). Times are beats; an event fires at its warning's start.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_H2.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def stage(part, b0, b1): ev(b0, 'H2stage', part=part, to=b1, keep=True)
def fx(b, v=0.35, s=10, dur=1.5): ev(b, 'H2fx', v=v, s=s, dur=dur)

# ---------- helpers ----------
def arms(wins, hits, lf=1.0, wn=0.75, hold=1.0, w=96):
    lfs = lf if isinstance(lf, list) else [lf]
    ev(min(hits) - wn, 'H2arms', win=wins, hits=hits, lf=lfs, wn=wn, hold=hold, w=w)
def bars(b0, b1, step, start, d=1, every=1, lf=1.0, hold=1.0, wn=0.75, pair=True, w=116):
    # arms round the walls one window after the other (d=1 clockwise), with the opposite window too (pair): a turning fan of bars
    wins, hits = [], []; i = start; b = b0
    while b < b1 - 1e-9:
        wins.append(i % 16); hits.append(round(b, 4))
        if pair: wins.append((i + 8) % 16); hits.append(round(b, 4))
        i += d * every; b += step
    arms(wins, hits, lf, wn, hold, w)
def grasp(b0, b1, lf=0.17, w=60, wn=1.0):
    # hands reach in from every window and corner and stay: the edges of the graveyard are no place to hide
    wins = list(range(20)); arms(wins, [b0] * 20, [lf] * 16 + [lf * 1.05] * 4, wn=wn, hold=b1 - b0, w=w)
def wire(hit, to, L, a0, sb, sw, on, x=CX, y=CY, wn=1.0):
    ev(hit - wn, 'H2wire', hit=hit, to=to, L=L, a0=a0, sb=sb, sw=sw, on=on, x=x, y=y)
GRAVES = [(210, 150), (640, 120), (1070, 150), (1150, 360), (1070, 570), (640, 600), (210, 570), (130, 360)]
def horde(sb, gi, st, to, boom=0, bR=80, wn=1.5):
    ev(min(sb) - wn, 'H2horde', sb=sb, sx=[GRAVES[g][0] for g in gi], sy=[GRAVES[g][1] for g in gi], st=st, wn=wn, to=to, boom=boom, bR=bR, rnd=True)
SPAWN = [(60, 60), (1220, 660), (1220, 60), (60, 660), (640, 40), (640, 680), (1240, 360), (40, 360)]
HI = [0]
def hound(b, copy=False, wn=0.6, sp=1900):
    x, y = SPAWN[HI[0] % len(SPAWN)]; HI[0] += 1
    ev(b, 'H2hound', x=x, y=y, copy=copy, wn=wn, sp=sp, rnd=True)
def pack(b, n=3, gap=0.5, **kw):
    for i in range(n): hound(b + i * gap, **kw)
COF_X = [200, 640, 1080, 200, 640, 1080]
COF_Y = [150, 150, 150, 570, 570, 570]
def coffins(b0, to, ord_, sh, ex, R=360, hold=0.45):
    ev(b0, 'H2coffins', cx=COF_X, cy=COF_Y, ord=ord_, sh=sh, ex=ex, R=R, hold=hold, to=to)
def tally(strokes, to):
    # strokes: (x0, y0, x1, y1, start, dd, w, wn)
    b0 = min(s[4] - s[7] for s in strokes)
    ev(b0, 'H2tally', x0=[s[0] for s in strokes], y0=[s[1] for s in strokes], x1=[s[2] for s in strokes], y1=[s[3] for s in strokes],
       sb=[s[4] for s in strokes], dd=[s[5] for s in strokes], w=[s[6] for s in strokes], wn=[s[7] for s in strokes], to=to)
def copy(b0, hit, to, dl=0.3): ev(b0, 'H2copy', hit=hit, to=to, dl=dl, rnd=True)
def nuke(b0, ticks, hit, x, y, r, hold=0.75): ev(b0, 'H2nuke', tick=ticks, hit=hit, x=x, y=y, r=r, hold=hold)

# ================= part 1: the graveyard (0-65) =================
stage(1, 0, 66)
# 0:00 the boards: arms burst from one window after the other right across to the far wall, a fan of bars turning round the middle.
# Stay in the open wedge and turn with it
bars(1, 8, 0.5, 0, d=1, lf=2.0, hold=3.5, wn=0.6, w=120)
for b in range(3, 15): hound(b)
# 0:05 two fans, one each way: they close the space between them, cross, and open it again
bars(8, 15, 0.5, 0, d=1, lf=2.0, hold=3.0, pair=False, wn=0.6, w=120)
bars(8, 15, 0.5, 8, d=-1, lf=2.0, hold=3.0, pair=False, wn=0.6, w=120)
# 0:10 three electric wires turn (left, middle, right), live one after the other; hands reach in from all the windows
grasp(15, 30)
bars(16, 30, 0.5, 2, d=-1, lf=2.0, pair=False, hold=1.5, wn=0.6)
wire(16, 30, 380, 0, [15], [60], [16, 17.5, 19, 20.5, 22, 23.5, 25, 26.5, 28, 29.5], x=320, wn=1.0)
wire(16, 30, 380, 60, [15], [-60], [16.75, 18.25, 19.75, 21.25, 22.75, 24.25, 25.75, 27.25, 28.75, 30], x=640)
wire(16, 30, 380, 120, [15], [60], [17.5, 19, 20.5, 22, 23.5, 25, 26.5, 28, 29.5, 30], x=960)
# 0:20 the horde: zombies climb out of the graves and lurch at you on every beat. Lead them into each other or into the wire;
# every one that dies stays as a corpse on the ground, so choose where they fall
fx(30, 0.3, 10, 1.2)
grasp(30, 62.5)
sb, gi = [], []
for k in range(30):
    sb += [31 + k, 31.33 + k, 31.67 + k]; gi += [k % 8, (k + 4) % 8, (k + 2) % 8]
horde(sb, gi, 290, 63)
wire(32, 62, 300, 45, [30, 46], [30, -34], [32, 40, 41.5, 50, 51.5, 61])
# 0:27 hellhounds in threes, each one aimed at you when its lightning strikes
for b in range(33, 62, 2): pack(b)
bars(34, 62, 1.0, 0, d=1, lf=2.0, pair=False, hold=1.5, wn=0.6)
# 0:42 the stop: every corpse blows up, then all the windows break one after the other
fx(63, 0.4, 12, 1.0)
bars(63.5, 65.75, 0.125, 0, d=1, lf=1.2, pair=False, hold=0.75, wn=0.6)

# ================= part 2: the rounds (66-113) =================
stage(2, 66, 114)
fx(66, 0.35, 10, 1.5)
# 0:44 the tally marks: four chalk walls, then the fifth cuts across them all. Pick the lane where the cut leaves you room;
# hands punch down the lanes and hounds come for you
tally([(300, 40, 320, 680, 66.5, 0.3, 34, 1.0), (500, 680, 480, 40, 67, 0.3, 34, 1.0), (700, 40, 720, 680, 67.5, 0.3, 34, 1.0),
       (900, 680, 880, 40, 68, 0.3, 34, 1.0), (180, 610, 1050, 120, 70.5, 0.5, 110, 2.5)], 73)
arms([1, 11, 2, 10, 3, 9, 1, 11, 2, 10, 3, 9], [68, 68.5, 69, 69.5, 70, 70.5, 71, 71.5, 72, 72.5, 73, 73.5], 1.6, wn=0.6, hold=0.8)
for b in (68.5, 69, 70, 70.5, 71.5, 72, 72.5, 73): hound(b)
# 0:49 the second round: a grid of walls, then the cut. Get into a cell the cut misses before the walls close you in; hounds come for you
tally([(260, 30, 260, 690, 73.5, 0.3, 34, 1.0), (520, 690, 520, 30, 74, 0.3, 34, 1.0), (780, 30, 780, 690, 74.5, 0.3, 34, 1.0),
       (1040, 690, 1040, 30, 75, 0.3, 34, 1.0), (20, 240, 1260, 240, 75.5, 0.4, 34, 1.0), (1260, 480, 20, 480, 76, 0.4, 34, 1.0),
       (100, 100, 1180, 640, 78, 0.5, 110, 2.8)], 80)
for b in (76.5, 77, 77.5, 78.5, 79, 79.5): hound(b)
# 0:52 the coffins: they light up one by one, then burst in the same order (only the lid shakes before). Remember the order;
# the next round lights up while the last one is still bursting
coffins(77, 100.5, [0, 5, 2, 3, 1, 4, 5, 0, 4, 1, 3, 2, 1, 3, 0, 5, 2, 4],
        [78, 79, 80, 81, 82, 83, 86, 87, 88, 89, 90, 91, 94, 94.5, 95, 95.5, 96, 96.5],
        [85, 85.5, 86, 86.5, 87, 87.5, 92.5, 93, 93.5, 94, 94.5, 95, 97.5, 98, 98.5, 99, 99.5, 100], hold=0.8)
grasp(81, 100)
bars(84, 100, 1.0, 0, d=1, lf=2.0, pair=False, hold=1.0, wn=0.6)
for b in (82, 83.5, 85, 86.5, 88, 89.5, 91, 92.5, 94, 95.5, 97, 98.5): pack(b, 2)
# 1:06 the dark: only the lightning on the beat shows what is there. The horde comes again; hounds strike with the lightning
ev(99, 'H2dark', to=113.5, fl=[b for b in range(99, 114)])
grasp(100, 113.5)
sb, gi = [], []
for k in range(12):
    sb += [100 + k, 100.33 + k, 100.67 + k]; gi += [(2 * k) % 8, (2 * k + 5) % 8, (2 * k + 3) % 8]
horde(sb, gi, 290, 113.5)
for b in range(101, 113): hound(b, wn=0.75); hound(b + 0.5, wn=0.75)

# ================= part 3: the last round (114-203) =================
stage(3, 114, 204)
fx(114, 0.45, 14, 1.5)
# 1:16 your haunted copy: it does what you did a moment ago, mirrored left to right. Cross the middle straight and you meet it.
# Wires turn on both sides; arms burst right across
copy(114, 116, 130)
wire(116, 130, 300, 0, [114, 122], [50, -50], [116, 117.5, 119, 120.5, 122, 123.5, 125, 126.5, 128, 129.5], x=330)
wire(116, 130, 300, 90, [114, 122], [-50, 50], [116.75, 118.25, 119.75, 121.25, 122.75, 124.25, 125.75, 127.25, 128.75, 130], x=950)
bars(118, 130, 0.5, 2, d=-1, lf=2.0, pair=False, hold=2.0, wn=0.6)
for b in range(118, 130, 2): hound(b + 0.5)
# 1:27 pack-a-punch: the horde again, faster, and now a corpse blows up 3 beats after it falls. Bars turn round the walls
fx(130, 0.3, 10, 1.2)
grasp(130, 146)
sb, gi = [], []
for k in range(13):
    sb += [131 + k, 131.33 + k, 131.67 + k]; gi += [(3 * k) % 8, (3 * k + 4) % 8, (3 * k + 6) % 8]
horde(sb, gi, 270, 146, boom=3, bR=100)
bars(132, 146, 0.625, 0, d=1, lf=2.0, hold=1.5, pair=False, wn=0.6)
# 1:38 the tally with your copy: walls cut the screen into cells, and the hounds now go for your copy, not for you
copy(146, 147, 162)
tally([(320, 30, 320, 690, 147, 0.3, 34, 1.0), (640, 690, 640, 30, 147.5, 0.3, 34, 1.0), (960, 30, 960, 690, 148, 0.3, 34, 1.0),
       (20, 360, 1260, 360, 148.5, 0.5, 34, 1.0), (100, 650, 1180, 80, 151, 0.5, 110, 3.0)], 152)
for b in (149, 149.5, 150, 150.5, 151, 151.5): hound(b, copy=True)
for b in (149.25, 150.25, 151.25): hound(b)
bars(149, 161, 0.5, 0, d=1, lf=2.0, pair=False, hold=1.0, wn=0.6)
tally([(20, 180, 1260, 180, 152.5, 0.4, 34, 1.0), (1260, 540, 20, 540, 153, 0.4, 34, 1.0), (427, 30, 427, 690, 153.5, 0.3, 34, 1.0),
       (853, 690, 853, 30, 154, 0.3, 34, 1.0), (100, 100, 1180, 640, 157, 0.5, 110, 3.0)], 161)
for b in range(155, 161): hound(b, copy=True); hound(b + 0.5, copy=True); hound(b + 0.25)
# 1:48 the coffins with hounds in fives
coffins(161.5, 171.5, [2, 3, 0, 5, 1, 4], [162, 163, 164, 165, 166, 167], [168, 168.5, 169, 169.5, 170, 170.5], hold=0.8)
grasp(162, 171)
pack(163, 5); pack(165.75, 5); pack(168.5, 5)
bars(163, 171, 0.5, 4, d=-1, lf=2.0, pair=False, hold=1.0, wn=0.6)
# 1:54 the loudest part: your copy, the turning wires and the hands; then the horde with hounds aimed at your copy
fx(171, 0.4, 12, 1.5)
copy(171, 172, 187)
wire(172, 179, 300, 30, [171, 175], [60, -60], [172, 173.5, 174, 175.5, 176, 177.5, 178, 179], x=330)
wire(172, 179, 300, 120, [171, 175], [-60, 60], [173, 174.5, 175, 176.5, 177, 178.5], x=950)
bars(172, 179, 0.5, 0, d=1, lf=2.0, hold=2.0, wn=0.6)
grasp(179, 187)
sb, gi = [], []
for k in range(7):
    sb += [179 + k, 179.33 + k, 179.67 + k]; gi += [(5 * k) % 8, (5 * k + 4) % 8, (5 * k + 2) % 8]
horde(sb, gi, 270, 187, boom=3, bR=100)
for b in range(180, 187): hound(b, copy=True); hound(b + 0.5, copy=True); hound(b + 0.25)
bars(180, 187, 1.0, 8, d=-1, lf=2.0, pair=False, hold=1.0, wn=0.6)
# 2:05 the nukes: each counts down; get into the shelter before it goes off. Each shelter is smaller and the count shorter
nuke(188, [189, 190, 191], 192, 790, 300, 110)
for b in (188.5, 189, 189.5, 190, 190.5, 191, 191.5): hound(b, wn=0.5, sp=2200)
nuke(192.75, [193.5, 194.5], 195.5, 490, 430, 90)
for b in (193, 193.5, 194, 194.5, 195): hound(b, wn=0.5, sp=2200)
nuke(196.25, [197, 198], 199, 720, 400, 75, hold=1.0)
arms([13, 14, 12, 15, 11], [196.5, 197, 197.5, 198, 198.5], 0.55, wn=0.6, hold=1.0)
for b in (196.25, 196.75, 197.25, 197.75, 198.25, 198.75): hound(b, wn=0.5, sp=2200)
nuke(199.75, [200.5, 201.25], 202, 580, 320, 62, hold=0.6)
for b in (200, 200.5, 201, 201.5): hound(b, wn=0.5, sp=2200)
fx(203, 0.6, 16, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
en = json.load(open(os.path.join(HERE, 'energy.json')))['energy']
D = {'id': 'H2', 'name': 'هاردكور: المرحلة الثانية', 'song': 'Laughing Corpses', 'file': 'SONG_H2', 'bpm': 90, 'off': 0.115, 'end': 203, 'cps': [0, 66, 114],
     'ez': {}, 'energy': en, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
