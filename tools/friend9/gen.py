#!/usr/bin/env python3
# Makes data/dodge_F9.json: F9 "I'm Still Here" (Sia), azm511's level. 85 BPM, beat 0 = 0.405 s, a beat = 0.7059 s.
# Song map (beats, from the timed lyrics): piano 0-8 | verse lines every 8 beats from 9 (0:06.5) | louder from 41 (0:29)
# | chorus 69-104 (0:49, lines every 4 beats; "still here" 101 = 1:11.9) | quiet 104-112 (1:14) | verse 2 112-165 (1:20; tears 120 = 1:25.4,
# light over shadow 145 = 1:42.6) | chorus 2 165-212 (1:56.9; "still here" 197.6 = 2:19.9) | win 212 (2:30.0)
# Parts: 1 the city (0-104), 2 the sea at sunset (104-165), 3 the coffin (165-212). Checkpoints 0, 104, 165.
# Nothing here is random: every attack is a fixed pattern on the beat. An event fires at its start minus EZW times its warning
# (Easy shows the warning longer).
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_F9.json')
W, H = 1280, 720
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW - 0.05, k, hit=hit, wn=wn, **kw)

# ================= part 1: the city at night (0-104) =================
ev(0, 'F9stage', part=1, to=104, keep=True)

# 0:01 the street lamps come on in a wave (left to right, then back), on the piano
LX = [100 + 135 * i for i in range(9)]
lamp = {x: [] for x in LX}
for i in range(9): lamp[LX[i]].append(1.5 + 0.375 * i)
for i in range(9): lamp[LX[8 - i]].append(4.0 + 0.375 * i)
for i in range(0, 9, 2): lamp[LX[i]].append(6.75)
for i in range(1, 9, 2): lamp[LX[i]].append(7.75)
for i in range(0, 9, 2): lamp[LX[i]].append(8.75)
for x in LX: ev(0, 'F9lamp', x=x, hb=sorted(lamp[x]), wn=1.0, on=1.1, t0=0, to=10)

# 0:06 the tower: the camera rises with it (58 px a beat from beat 9); the shadow walks through its floors and every window it passes lights pink
FB0, FV, FPY = 9, 58, 116
def scr(bt): return max(0, bt - FB0) * FV
def cell(srow, col, bt):
    # the window on screen row srow (0 = the top one fully on screen) at beat bt
    r = int(math.floor((scr(bt) - 20) / FPY)) + 1 + srow
    return r * 9 + col
cells, tb = [], []
def win(srow, col, bt): cells.append(cell(srow, col, bt)); tb.append(round(bt, 4))
# line 1 (0:06.5): one shadow walks the floors in a zigzag, 4 windows a beat
b = 9.0
for s in range(6):
    cols = range(9) if s % 2 == 0 else range(8, -1, -1)
    for c in cols:
        if b < 17: win(s, c, b); win(5 - s, 8 - c, b)
        b += 0.18
# line 2 (0:12.4): two shadows from the two sides, crossing on each floor
b = 17.0
for s in [5, 4, 3, 2]:
    for k in range(9):
        win(s, k, b + 0.18 * k); win(s - 2 if s > 1 else 3, 8 - k, b + 0.18 * k)
    b += 2.0
# line 3 (0:17.6): whole floors light from the top down, one a beat; one window stays dark (go through it)
DARK = [4, 6, 2, 7, 1, 5, 3, 8, 0, 4, 7, 2, 6, 1, 5, 3]
for j in range(16):
    bt = 25.0 + j * 0.5
    srow = j % 6
    for c in range(9):
        if c != DARK[j]: win(srow, c, bt)
# line 4 (0:23.5): columns from the sides in, then from the middle out
order = [0, 8, 1, 7, 2, 6, 3, 5, 4, 3, 5, 2, 6, 1, 7]
for j, c in enumerate(order):
    bt = 33.0 + j * 0.5
    for s in range(6): win(s, c, bt)
ev(8, 'F9facade', cells=cells, tb=tb, on=1.8, wn=1.0, b0=FB0, v=FV, to=41.5)

# 0:29 the traffic: six lanes, cars on every lane (even lanes to the right, odd to the left). Cross between them.
ev(39, 'F9roads', to=69, keep=True)
def cars(b0, b1, spd, gap, ph, hbs=()):
    for ln in range(6):
        d = 1 if ln % 2 == 0 else -1
        b = b0 + ph[ln]
        while b < b1:
            hb = [x for x in hbs if b - 0.5 < x < b + 6]
            at(b, 1.0, 'F9car', ln=ln, dir=d, sp=spd[ln], hb=hb, hl=0.5, len=260, cw=170)
            b += gap[ln]
cars(41, 49, [280, 320, 280, 320, 280, 320], [2.5] * 6, [0, 1.25, 0.5, 1.75, 1, 2.25])
cars(49, 57, [320, 360, 320, 360, 320, 360], [2] * 6, [0.5, 1.5, 1, 0, 1.25, 0.25], hbs=[x + 0.0 for x in range(50, 58, 2)])
cars(57, 64, [360, 400, 360, 400, 360, 400], [2] * 6, [0, 1, 0.5, 1.5, 0.25, 1.25], hbs=[x + 0.0 for x in range(57, 66, 1)])

# 0:49 the chorus: a car drives far below, the towers' shadows turn like the hands of a clock (light through their windows leaves gaps)
TX = [160, 480, 800, 1120]; TH = [300, 380, 340, 280]
# the shadows turn from lying right (8 degrees) to lying left (172) and back, once a line, then faster
bs = [69, 73, 77, 81, 85, 88, 91, 93.5, 95.5, 97.5]
ka = [8 if j % 2 == 0 else 172 for j in range(len(bs))]
at(69, 2.0, 'F9shadows', xs=TX, th=TH, kb=bs, ka=ka, bw=22, gaps=[285, 345, 565, 625], to=97)
# on each "wants me dead" a floor of windows lights across the screen (the walls between are safe)
for b, y, ox in [(73, 200, 20), (75, 470, 90), (77, 330, 60), (81, 140, 0), (83, 520, 40), (85, 260, 110), (89, 380, 70), (91, 160, 30), (93, 560, 100), (95, 300, 10)]:
    at(b, 1.0, 'F9row', y=y, ox=ox, on=0.7)

# 1:09 "lost the war... still here": the towers fall one after another
for b, x, d in [(97.5, 150, 1), (98.25, 1130, -1), (99.0, 420, 1), (99.75, 860, -1), (100.5, 640, 1), (101.2, 980, -1)]:
    ev(b - 0.6 * EZW - 1.2 * EZW - 0.05, 'F9topple', x=x, dir=d, len=600, w=100, hit=b, fall=0.6, wn=1.2, hold=0.6)

# ================= part 2: the sea at sunset (104-165) =================
ev(103.5, 'F9stage', part=2, to=165, keep=True)
ev(104, 'F9fx', v=0.35, s=8, dur=1.4)
# 1:14 the waves come in from the horizon; go through the break. Every 4 beats the backwash goes back up the whole screen
GX = [640, 420, 860, 560, 300, 700, 980, 640, 380, 820, 560, 300, 520, 900, 240, 660, 1000, 440, 760, 340, 600, 900, 300, 640]
for j, gx in enumerate(GX):
    b = 106 + 0.8 * j
    if b > 120.5: break
    ev(b - 2.75 * 1.2 - 0.05, 'F9wave', hit=b, dur=2.75, gx=gx, gw=210)
for b, gx in [(110.3, 600), (112.8, 900), (114.8, 200), (116.8, 300), (118.8, 1000), (120.8, 760)]:
    ev(b - 2 * 1.2 - 0.05, 'F9wave', hit=b, dur=2, gx=gx, gw=200, up=True)

# 1:25 "tears falling": tears drop into the sea and their rings spread (go through the break, or dash)
TP = [(320, 300, 90), (960, 480, -90), (640, 560, 180), (420, 220, 0), (880, 260, 120), (220, 520, -45), (1060, 360, 200), (640, 300, 60),
      (300, 420, 0), (980, 220, 150)]
for j, (x, y, ga) in enumerate((TP + TP[::-1][1:])[:17]):
    at(121 + j * 0.45, 1.5, 'F9ripple', x=x if j < len(TP) else W - x, y=y, sp=240, th=12, ga=ga, gw=60, rmax=740)

# 1:31 the lighthouse: two beams turn round (once in 8 beats, then back) and the tide comes and goes
at(129, 2.0, 'F9light', x=640, y=H - 120, n=3, hw=4.6, kb=[129, 137, 145], ka=[200, 560, 200], to=145)
tkb = [129 + 2 * i for i in range(9)]; tky = [H - 80 if i % 2 == 0 else H - 260 for i in range(9)]
at(129, 2.0, 'F9tide', kb=tkb, ky=tky, to=145)

# 1:42 "light over shadow": the moon's reflection breaks into rows that come down at you; the moon pulses rings
for j in range(24):
    b = 145 + (j if j < 8 else 8 + (j - 8) * 0.5)
    if j % 2 == 0: xs = [70 + 180 * k for k in range(8)]
    else: xs = [160 + 180 * k for k in range(7)]
    ev(b - 1.0, 'F9glint', xs=[x + (40 if (j // 2) % 2 else 0) for x in xs], hw=59, hit=b, sp=238, ezskip=(j >= 8 and j % 2 == 1))
for b, ga in [(146, 90), (148, 40), (150, 140), (152, 70), (154, 110), (156, 50), (158, 130), (160, 90)]:
    at(b, 1.5, 'F9ripple', x=640, y=60, sp=260, th=12, ga=ga, gw=45, rmax=900, nodrop=True)

# ================= part 3: the coffin (165-212) =================
ev(163.5, 'F9stage', part=3, to=212.5, keep=True)
ev(165, 'F9fx', v=0.4, s=10, dur=1.5)
at(165.5, 1.2, 'F9coffin', kb=[165, 169, 173, 177, 181, 184, 197.5, 201, 205, 209, 212], ka=[0, 22, -22, 24, -18, 0, 0, 14, -14, 12, 0], to=212)

# 1:57 the nails: on the lines they come through the walls (three hits each) and stay; the coffin turns
def nail(lx, ly, ang, hb0, to, ln=340): at(hb0, 1.5, 'F9nail', lx=lx, ly=ly, ang=ang, len=ln, hb=[hb0, hb0 + 1, hb0 + 2], to=to)
def topy(lx): return -300 + (lx + 330) * 85 / 905   # the top wall from (-330,-300) to (575,-215)
for lx in [-150, 330]: nail(lx, topy(lx), 90, 165.5, 173)
for lx in [80, 520]: nail(lx, -topy(lx), -90, 166.5, 173)
for lx in [-50, 420]: nail(lx, topy(lx), 90, 169.5, 177)
for lx in [-260, 200]: nail(lx, -topy(lx), -90, 169.5, 177)
nail(-575, 0, 0, 172.5, 177, ln=260)
for lx in [100, 500]: nail(lx, topy(lx), 90, 173.5, 181)
for lx in [-250, 350]: nail(lx, topy(lx), 90, 166.5, 173)
for lx in [-120, 280]: nail(lx, -topy(lx), -90, 167.5, 173)
for lx in [180]: nail(lx, topy(lx), 90, 171.5, 177)
nail(575, 0, 180, 168.5, 173, ln=300)
for lx in [-150, 300]: nail(lx, -topy(lx), -90, 174.5, 181)
nail(575, -60, 180, 176.5, 181, ln=300); nail(575, 90, 180, 177.5, 181, ln=200)
for lx in [-200, 220]: nail(lx, topy(lx), 90, 177.5, 181)

# 2:08 the lid comes down and the earth is thrown on it: lumps fall and lie on the floor of the coffin
at(181, 1.5, 'F9lid', kb=[181, 184, 187, 190], ky=[0, 140, 220, 270], to=193)
FLOOR = H / 2 + 255
for j, x in enumerate([320, 960, 640, 180, 1100, 480, 800, 260, 1020, 640, 400, 880, 140, 560, 1160, 720, 300, 1000, 460, 820, 600, 220, 1060, 380, 900, 140, 700, 1140]):
    at(182 + j * 0.36, 1.2, 'F9dirt', x=x, y=FLOOR - 78 - (44 if j >= 10 else 0), R=78, fall=0.6, to=193)

# 2:16 the skull looks in: its eyes are two beams that close like scissors
ev(190, 'F9skull', x=640, y=78, r=72, tc=197.6, tb=211.6)
EYE = dict(lx=612, ly=88, rx=668, ry=88, bw=22)
for b, la0, la1, ra0, ra1 in [(193.2, 150, 70, 30, 110), (194.2, 100, 40, 80, 140), (195.2, 160, 100, 20, 80), (196.2, 120, 60, 60, 120), (197.0, 150, 90, 30, 90)]:
    at(b, 1.0, 'F9eyes', la0=la0, la1=la1, ra0=ra0, ra1=ra1, sw=0.6, hold=0.3, **EYE)

# 2:19 "still here": the skull cracks and the cracks run down the screen, the eyes keep closing between them
def crack(x1, seed, y1=H + 10):
    pts = [(640, 130)]; n = 7
    dx, dy = x1 - 640, y1 - 130; l = math.hypot(dx, dy); nx, ny = -dy / l, dx / l
    for i in range(1, n + 1):
        u = i / n; o = (60 if (i + seed) % 2 else -60) * (1 - u * 0.6)
        pts.append((640 + dx * u + nx * o, 130 + dy * u + ny * o))
    return [p[0] for p in pts], [p[1] for p in pts]
# to the sides too: x1 < 0 or > W ends on the side walls at y1
SIDE = {180: (-10, 300), 1100: (W + 10, 420), 140: (-10, 560), 1140: (W + 10, 220), 120: (-10, 160), 1160: (W + 10, 600)}
for j, (b, x1) in enumerate([(198.5, 180), (199.25, 1100), (200, 420), (200.75, 860), (201.5, 640), (202.5, 260), (203.25, 1020), (204, 520), (204.75, 760), (205.5, 140), (206.5, 1140), (207.25, 340), (208, 940), (208.75, 600), (209.5, 120), (210.25, 1160)]):
    xs, ys = crack(*SIDE.get(x1, (x1, H + 10)), j)
    at(b, 0.9, 'F9crack', xs=xs, ys=ys, w=30, hold=0.8)
    if j % 2 == 1: xs, ys = crack(*SIDE.get(W - x1, (W - x1, H + 10)), j + 1); at(b, 0.9, 'F9crack', xs=xs, ys=ys, w=30, hold=0.8)
for b, la0, la1, ra0, ra1 in [(199.6, 140, 80, 40, 100), (201.9, 100, 40, 80, 140), (203.6, 150, 90, 30, 90), (205.9, 120, 60, 60, 120), (207.6, 160, 100, 20, 80), (209.9, 110, 50, 70, 130)]:
    at(b, 1.0, 'F9eyes', la0=la0, la1=la1, ra0=ra0, ra1=ra1, sw=0.6, hold=0.3, **EYE)
ev(211.8, 'F9open')

EV.sort(key=lambda d: d['b'])
en = json.load(open(os.path.join(HERE, 'energy.json')))['energy']
D = {'id': 'F9', 'name': "I'm Still Here", 'by': 'azm511', 'ver': 'Mohammed', 'file': 'SONG_F9', 'bpm': 85, 'off': 0.405, 'end': 212, 'cps': [0, 104, 165],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': en, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print(len(EV), 'events')
