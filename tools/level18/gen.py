#!/usr/bin/env python3
# Makes data/dodge_L18.json: L18 "اعوف الدنيا" (Mohamed Alsalim), I would leave the world for you. The level plays the song from 0:50 to 3:40
# (songs/SONG_L18.mp3 is the song cut at 0:50). 102 BPM, beat 0 = 0.150 s (song 0:50.15), a beat = 0.58825 s; the song's three hits in a
# beat land at 0, 0.43 and 0.74 of it.
# Song map (beats): verse 0-23 (0:50) | verse b 24-47 (1:04) | chorus 48-79 (1:18) | chorus b 80-114 (1:37) | quiet break 115-127 (1:58)
# | verse 2 128-162 (2:05) | the soft part 163-198 (2:26) | the build 199-223 (2:47) | last chorus 224-255 (3:01) | its second half 256-287 (3:20)
# | the end 288 (3:39.5), win 289 (3:40.2).
# Parts: 1 the world (0-114), 2 leaving it (115-223), 3 beyond it (224-289). Checkpoints 0, 115, 224.
# Nothing here is random except the compass (it locks on the square). Times are beats; an event fires at its start minus wn * EZW (Easy's
# longest warning) and shows its warning from start - wn.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L18_v51.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
HITS = (0, 0.43, 0.74)
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)
def stage(part, b0, b1, **kw): ev(max(0, b0 - 4), 'L18stage', part=part, fr=b0, to=b1, keep=True, **kw)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L18fx', v=v, s=s, dur=dur)

# the same turning as L15Spin in L15.cs (radians a beat, changing over half a beat), to find the eclipses
class Spin:
    ST = 1.0 / 32
    def __init__(s, a0, b0, b1, sb, sw):
        s.b0, s.sb, s.sw = b0, sb, sw
        n = int(math.ceil((b1 - b0) / s.ST)) + 2; s.tab = [a0]
        for k in range(1, n):
            bb = b0 + k * s.ST; s.tab.append(s.tab[-1] + (s.W(bb - s.ST) + s.W(bb)) / 2 * s.ST)
    def W(s, b):
        v = s.sw[0]
        for i in range(1, len(s.sb)):
            if b >= s.sb[i] + 0.5: v = s.sw[i]
            else:
                if b > s.sb[i]: v = s.sw[i - 1] + (s.sw[i] - s.sw[i - 1]) * (b - s.sb[i]) / 0.5
                break
        return v
    def At(s, bt):
        f = (bt - s.b0) / s.ST
        if f <= 0: return s.tab[0]
        i = int(f)
        if i >= len(s.tab) - 1: return s.tab[-1]
        return s.tab[i] + (s.tab[i + 1] - s.tab[i]) * (f - i)

# ================= part 1: the world (0-114) =================
stage(1, 0, 115)
fx(0, 0.15, 5, 1.0)

# 0:50 the meridians: lines of longitude on a turning globe sweep across, slow and close together at the sides, fast in the middle; each has its
# gap at its own height. The globe turns right, from 1:55 (b9) back left, from 1:00 (b17) right again and faster
gy = [360, 200, 480, 290, 570, 180, 430, 550, 250, 460, 160, 340, 520, 230]
at(3, 2, 'L18merid', n=14, R=720, RY=1100, th=32, gy=gy, gh=46, a0=-12, s0=0, sb=[0, 9, 17], sw=[0.31, -0.32, 0.38], to=24)

# 1:04 night and day: the screen is 12 time zones and the night (pink) moves over them on the song's three hits (three zones in a beat, then a
# beat's rest). Stay in the day; it turns back at the end of each run. From 1:11 (b36) the day is a zone narrower and the polar nights breathe
def night(cols, ws0, steps_at, moves, ws_from, start=4, wide=(3, 9)):
    # moves: list of (beat it may start, target left zone); steps_at: the fractions of the burst beat
    st = {}  # strip -> list of (beat, dark)
    L = start; ws = ws0
    def dark_set(L, ws, b):
        lo, hi = (wide if b < 26 else (L, L + ws - 1))
        return {c for c in range(cols) if c < lo or c > hi}
    cur = dark_set(L, ws, 24.5)
    for c in cur: st.setdefault(c, []).append((25.0, True))
    b = 26
    # the first two beats the day is wide (zones 3-9), then it narrows to the window
    changes = [(26.0, L, ws)]
    mi = 0; target = moves[0][1]
    while b < 48:
        if mi + 1 < len(moves) and b >= moves[mi + 1][0] and L == target: mi += 1; target = moves[mi][1]
        nws = ws - 1 if (b >= ws_from and ws == ws0) else ws
        if b % 2 == 0:
            for f in steps_at:
                if L != target:
                    L += 1 if target > L else -1
                    L = max(0, min(cols - nws, L))
                changes.append((b + f, L, nws))
        ws = nws
        b += 1
    prev = cur
    for (bb, L_, ws_) in changes:
        now = dark_set(L_, ws_, bb)
        for c in now - prev: st.setdefault(c, []).append((bb, True))
        for c in prev - now: st.setdefault(c, []).append((bb, False))
        prev = now
    sc, sa, se = [], [], []
    for c, lst in st.items():
        lst.sort(key=lambda q: q[0]); on = None
        for (bb, d) in lst:
            if d and on is None: on = bb
            elif not d and on is not None:
                if bb > on: sc.append(c); sa.append(on); se.append(bb)
                on = None
        if on is not None: sc.append(c); sa.append(on); se.append(48.0)
    return sc, sa, se
moves = [(26, 7), (28, 0), (34, 6), (38, 1), (42, 7), (45, 3)]
sc, sa, se = night(12, 5, HITS, moves, 32)
esc, esa, ese = night(12, 6, (0, 0.74), moves, 36)
ev(23.5 - 1.0 * EZW, 'L18night', cols=12, sc=sc, sa=sa, se=se, esc=esc, esa=esa, ese=ese, wn=1.0, tw=23.5, to=48)
at(37, 1.5, 'L18polar', lo=60, hi=200, per=4, to=48)
fx(24, 0.12, 4, 0.8)

# 1:18 the flight routes: waves of three planes take off on the three hits, each from far off screen on a curve; their trails stay a moment.
# Every wave comes from another side (135 degrees on), so find the cell between this wave's trails and the last one's
fx(48, 0.3, 9, 1.4)
def route(hit, ang, off, bulge, w=20, wn=1.5, fl=1.25, hold=1.5, ezskip=False):
    dx, dy = math.cos(ang), math.sin(ang); nx, ny = -dy, dx
    x0, y0 = CX - dx * 900 + nx * off, CY - dy * 900 + ny * off
    x1, y1 = CX + dx * 900 + nx * off, CY + dy * 900 + ny * off
    qx, qy = CX + nx * (off + bulge), CY + ny * (off + bulge)
    at(hit, wn, 'L18route', x0=round(x0, 1), y0=round(y0, 1), qx=round(qx, 1), qy=round(qy, 1), x1=round(x1, 1), y1=round(y1, 1), w=w, fl=fl, hold=hold, ezskip=ezskip)
shifts = [0, 120, -90, 60, -140, 100, -40, 150, -110, 30, 130, -70, 80, -150, 40, -20]
for i, b in enumerate(range(48, 80, 2)):
    ang = math.radians(20 + 135 * i); sh = shifts[i % len(shifts)]; bul = 200 if i % 2 == 0 else -200
    for j, f in enumerate(HITS + tuple(1 + q for q in HITS)):
        route(b + 1 + f, ang, sh + (j - 2.5) * 140, bul, w=22, wn=1.15, hold=2.0, ezskip=(j % 2 == 1))

# 1:37 the compass: its needle follows you; every beat it locks where you are and fires along that line 0.74 beat later (the third hit).
# Keep going round it. A route still crosses every 4 beats; from 1:47 (b97) a second needle turns on its own and fires on the second hits
fx(80, 0.25, 8, 1.2)
lk = sorted([b for b in range(82, 114)] + [b + 0.43 for b in range(99, 114, 2)])
at(82, 2, 'L18compass', lk=lk, lag=0.435, w=76, hold=0.35, to=114.5, rnd=True)
for i, b in enumerate(range(83, 113, 2)):
    route(b, math.radians(70 + 110 * i), ((i * 97) % 300) - 150, 220 if i % 2 else -220, ezskip=(i % 2 == 1))
at(87, 1.5, 'L18needle', a0=30, wb=-32, fb=[b + 0.43 for b in range(87, 114)], pw=0.8, w=40, hold=0.45, to=114.5, ezskip=True)

# ================= part 2: leaving it (115-223) =================
stage(2, 115, 224)
fx(115, 0.35, 10, 1.5)

# 1:58 the map folds over: half the map folds onto the other half (pink), but its cities are holes to stand in. Run to the other half or hide in a city
folds = [
    (117, 0, 700, [(880, 180), (1080, 470), (760, 590)]),
    (120.5, 1, 700, [(170, 250), (430, 560), (300, 120)]),
    (123.5, 2, 410, [(260, 560), (640, 620), (1010, 540)]),
    (126, 3, 410, [(420, 110), (840, 160), (1150, 80)]),
]
for i, (hit, sd, d, hl) in enumerate(folds):
    to = folds[i + 1][0] - 0.75 if i + 1 < len(folds) else 128
    at(hit, 2 if i == 0 else 1.5, 'L18fold', sd=sd, d=d, hx=[p[0] for p in hl], hy=[p[1] for p in hl], hr=50, fi=0.4, to=to)

# 2:05 the map tears into six strips that slide left and right (each the other way to its neighbours); every strip has tears coming at you.
# Ride a gap to the edge, then cross to the next strip when its seam is not torn (the seams tear on the hits: 1 3 5 on the first, 2 4 on the second)
fx(128, 0.25, 8, 1.2)
sb, ss = [], []
for b in range(129, 162):
    for j in (1, 3, 5): sb.append(b); ss.append(j)
    for j in (2, 4): sb.append(b + 0.43); ss.append(j)
at(128.5, 2, 'L18tear', rows=6, vs=[170, -190, 160, -175, 195, -160], x0=[60, 300, 180, 420, 0, 240], pw=100, sp=360, sw=12, sb=sb, ss=ss, sd=0.36, swn=0.6, to=163)

# 2:26 the hourglass: the arena is the glass, outside it is pink. The sand pours in a stream through the neck (cross it with a dash), splashes
# up where it lands and drips from the top. Every 8 beats the stream stops and the glass turns over: be in the neck while it turns
fx(163, 0.3, 8, 1.4)
fb = [171, 179, 187, 195]
fs = [164, 172, 180, 188, 196]; fe = [170, 178, 186, 194, 199]
spb, db, dx = [], [], []
for a, e in zip(fs, fe):
    for b in range(a, e):
        spb.append(b)
        for f, side in ((0.43, 1), (0.74, -1)):
            db.append(b + f); dx.append(side * (70 + 45 * ((b * 3 + (1 if f > 0.5 else 0)) % 4)))
at(164, 2, 'L18glass', NK=80, HW=600, ex=1.5, hmin=30, hmax=150, sw=30, spv=480, gr=1100, gw=9, fs=fs, fe=fe, fb=fb, spb=spb, spa=[-35, -62, -118, -145], db=db, dx=dx, to=199)

# 2:47 the world closes in: the edge of the world closes towards you, and rings spread from the centre with a gap each (the gaps move on);
# from 2:53 (b211) a second ring follows on the third hit with its gap further on
fx(199, 0.25, 8, 1.2)
at(200, 1.5, 'L18close', r0=740, r1=360, to=223.5)
ga = 200
for i, b in enumerate(range(201, 223, 2)):
    at(b, 1.5, 'L18ripple', ga=ga, gw=58, v=390, th=18)
    if b >= 211: at(b + 0.74, 1.5, 'L18ripple', ga=ga + 55, gw=58, v=390, th=18, ezskip=True)
    ga = (ga + 130) % 360

# ================= part 3: beyond it (224-289) =================
stage(3, 224, 289.5, brk=256)
fx(224, 0.45, 12, 1.6)

# 3:01 the orbit: two bodies turn round the centre (the inner one fast), each with a ring of pebbles. Whenever they line up with the middle
# (every 4 beats, then every 3 from 3:20) the eclipse burns along that line. 3:20 the world in the middle breaks: its shards fly out along five
# spiral arms on the three hits; go round with the arms. The pebbles are gone by then
S0 = 222
sbi, swi = [S0, 256], [0.2 + math.pi / 3, 0.2 + math.pi / 2]
sbo, swo = [S0, 256], [0.2, 0.2]
ai, ao = 90, 0
si = Spin(math.radians(ai), S0, 292, sbi, swi); so = Spin(math.radians(ao), S0, 292, sbo, swo)
eb = []; prev = None; bt = 225.5
while bt < 287:
    d = math.sin(si.At(bt) - so.At(bt))
    if prev is not None and (prev < 0) != (d < 0) and math.cos(si.At(bt) - so.At(bt)) ** 2 > 0.5: eb.append(round(bt, 3))
    prev = d; bt += 1 / 64
at(225, 2, 'L18close', r0=400, r1=345, ex=1.75, rt=227, to=287.5)
at(225, 2, 'L18orbit', ri=150, ro=300, ex=1.7, rb=34, pr=140, pwi=1.2, pwo=-1.0, pb=17, np=12, pon=224, poff=256, ew=1.2, bw=0.6, w=70,
   s0=S0, ai=ai, ao=ao, sbi=sbi, swi=swi, sbo=sbo, swo=swo, eb=eb, to=287.5)
fx(256, 0.35, 10, 1.4)
shb = [round(b + f, 3) for b in range(256, 283) for f in HITS]
ev(254.5, 'L18shards', sb=shb, na=7, a0=-90, spin=20, b0=256, v=290, kb=0.0024, sr=16, tw=254.5, to=283)
fx(287.5, 0.5, 6, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
acc = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L18', 'name': 'المرحلة الثامنة عشرة', 'song': 'اعوف الدنيا', 'file': 'SONG_L18', 'bpm': 101.997, 'off': 0.150, 'end': 289, 'cps': [0, 115, 224],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezwn': 1.25}, 'energy': acc['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'eclipses', eb)
