#!/usr/bin/env python3
# Makes data/dodge_L16.json: L16 "عين بعين" (Hussam Al-Rassam), the third design (v52): the garden that dies, the mirror that breaks, the bride.
# The song file is the song from 0:40 (songs/SONG_L16.mp3). 106.1 BPM, a beat = 0.56549 s, beat 0 = 0.749 s in the file (0:40.75 in the song).
# A bar is 4 beats from beat 0; the drum hits on its beats 0 and 2, and on some bars a hit at 3.25 (87.25, 91.25, 103.25, 135.25, 247.25, 263.25...).
# The lines (beats, song time; the second verse is the first one's tune 160 beats later, a line is 8 beats):
#   0 (0:40.7) "مو حبيبي.. انت ماظن نفسك انت" | 12 (0:47.5) "وكدامي جنتي تموت خوفك يكتلك" | 20-36 (0:52) "وهسه تكلي انساك واكدر ابدلك" x2
#   36 (1:01) "وينه كلامك" | 44-60 (1:05.6) "هسه احنه عين بعين تحجي عليه" x2 | 60-76 (1:14.7) "مدري شنو النساك.. / كل عمري ما انساه"
#   76 (1:23.7) "ويلي ويلي / خوفك يكتلك" | 92 "اي وهسه تكلي انساك واكدر ابدلك" | 100 music
#   108 (1:41.8) "خنت الكضه وياك / هو النطاك الروح هيجي جزاته", 128 (1:53.1) the same, stronger
#   152 (2:06.7) "يلكتلي غيري / ماغزرت الحضنات" x2 | 176 (2:20.3) "وانت طلعت غدار حارس وبكت الدار / روح وبعد للموت تحرم عليه"
#   188 (2:27) "اي هسه تكلي انساك" | 204 "عين بعين" x2 | 220 "مدري شنو النساك" | 236 "ويلي ويلي" | 252 "اي هسه تكلي" | 260 music
#   268 (3:12.3) "رحت انت بس والله تالي تجيني / ترجع ذليل", 288 (3:23.6) stronger | 312 (3:37.2) "الي يبيع الناس" | 320 (3:41.7) "يخلص العمر حسرات"
#   the end 329.4 (3:47.0)
# Parts: 1 the garden (0-106), 2 the mirror (108-186), 3 the bride (188-329.4). Checkpoints 0, 108, 188 (his word 10-25: two saves);
# every part ends 2 beats before the next checkpoint (a restart starts there).
# Fixed attacks fire before their warning starts (b = hit - wn * EZW, Easy's longest warning); aimed ones ("rnd") lock on the square at b.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L16.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
SPB = 60 / 106.1027
OFF = 0.749
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items():
        if v is False: continue
        d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L16bfx', v=v, s=s, dur=dur)
def fixed(hit, k, wn=1, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)
def flat(*rows): return [x for r in rows for x in r]
def rint(sp, b0, b):
    # the rate program (beat, rate...) integrated from b0 to b (as L16bInt)
    a = 0
    for i in range(0, len(sp) - 1, 2):
        s0 = max(sp[i], b0); s1 = sp[i + 2] if i + 2 < len(sp) else 1e9
        if b <= s0: break
        a += sp[i + 1] * (min(b, s1) - s0)
    return a

def rose(hit, to, wn=1, ez=False, **kw): fixed(hit, 'L16brose', wn=wn, to=to, ezskip=ez, **kw)
def vine(hit, to, x, y, a, ez=False, wn=1, **kw): fixed(hit, 'L16bvine', wn=wn, to=to, x=x, y=y, a=a, ezskip=ez, **kw)
def vine_aim(b, a, wn=0.8, dur=2, ez=False, **kw):
    # a vine along your row (a 0 / 180) or column (a 90 / -90), from that edge
    x = -20 if a == 0 else W + 20 if a == 180 else 0; y = -20 if a == 90 else H + 20 if a == -90 else 0
    ev(b, 'L16bvine', rnd=True, wn=wn, dur=dur, x=x, y=y, a=a, L=1340 if a in (0, 180) else 760, ezskip=ez, **kw)
def drop_aim(b, S=80, rot=0, seed=1, ns=4, vs=440, hold=0.8, ez=False, wn=1): ev(b, 'L16bdrop', rnd=True, wn=wn, S=S, rot=rot, seed=seed, ns=ns, vs=vs, hold=hold, ezskip=ez)
def petal(hit, an, x=CX, y=CY, ez=False, v=400, **kw): fixed(hit, 'L16bpetal', wn=0.6, an=an, x=x, y=y, v=v * 1.3, ezskip=ez, **kw)
def crack(hit, x, y, n, a0, hold=2, seed=1, ez=False, **kw): fixed(hit, 'L16bcrack', x=x, y=y, n=n, a0=a0, hold=hold, seed=seed, ezskip=ez, **kw)
def crack_aim(b, n, a0=0, hold=2, seed=1, mir=False, ez=False, wn=0.8, th=18): ev(b, 'L16bcrack', rnd=True, wn=wn, n=n, a0=a0, hold=hold, seed=seed, mir=mir, th=th, ezskip=ez)
def drop(hit, x, y, S=80, rot=0, seed=1, ns=4, vs=420, hold=1, ez=False): fixed(hit, 'L16bdrop', x=x, y=y, S=S, rot=rot, seed=seed, ns=ns, vs=vs, hold=hold, ezskip=ez)
def cut(hit, a, x=CX, y=CY, w=80, op=0.5, hold=1, seed=1, ez=False): fixed(hit, 'L16bcut', x=x, y=y, a=a, w=w, op=op, hold=hold, seed=seed, ezskip=ez)
def cut_aim(b, a, w=80, op=0.5, hold=1, seed=1, ez=False, wn=0.8): ev(b, 'L16bcut', rnd=True, wn=wn, a=a, w=w, op=op, hold=hold, seed=seed, ezskip=ez)
def orbit(hit, to, x, y, orad, oa0, osp, dor=0, pl=(), R=46, th=13, spin=0.5, ez=False):
    fixed(hit, 'L16bring', x=x, y=y, orad=orad, oa0=oa0, osp=osp, dor=dor, pl=list(pl), R=R, th=th, spin=spin, to=to, ezskip=ez)
def throw(hit, x, y, x1, y1, v=800, back=0, R=44, th=12, spin=0.5, ez=False, wn=1):
    fixed(hit, 'L16bring', wn=wn, x=x, y=y, x1=x1, y1=y1, v=v, back=back, R=R, th=th, spin=spin, ezskip=ez)
def ring_aim(b, x, y, v=720, R=44, th=12, spin=0.5, ez=False, wn=0.8): ev(b, 'L16bring', rnd=True, wn=wn, x=x, y=y, v=v * 1.1, R=R, th=th, spin=spin, ezskip=ez)
def lace(hit, d, p0, v, hs, bw=70, hr=58, ez=False, wn=1): fixed(hit, 'L16blace', wn=wn, dir=d, p0=p0, v=v * 1.15, hs=hs, bw=bw, hr=hr, ezskip=ez)
def coin(hit, x, y, an, vs, gr=900, r=13, ez=False, wn=1): fixed(hit, 'L16bcoin', wn=wn, x=x, y=y, an=an, vs=[v * 1.15 for v in vs], gr=gr * 1.32, r=r, ezskip=ez)
def bq(b, x, y, Rb=170, n=5, spin=0.6, hold=1.2, ez=False, wn=1.0): ev(b, 'L16bbq', rnd=True, wn=wn, x=x, y=y, Rb=Rb, n=n, spin=spin, hold=hold, ezskip=ez)
def ring_of(n, off=0): return [off + 360 * i / n for i in range(n)]

# the bride: (beat, x, y, beats to get there)...; her face is at (x, y), her hands (and the bouquet) 145 px below it
BP = [188, CX, 120, 0, 258, CX, 200, 2, 268, CX, 120, 1.5, 310, CX, 200, 2, 320, CX, 110, 2]
MC = []   # the beats the mirror gets a crack (it shows them)

# ======== part 1: the garden "جنتي" ========
# 0:41 "مو حبيبي.. انت ماظن نفسك انت": a pen draws a big five-petal rose over the garden (its line hurts as it is drawn; you live in a
# petal or outside it). Then it turns and swells on every beat, and small petals fly out between its petals
SPA = [2, 0, 6, 22]
DROPS = [12, 14, 16, 18]
rose(2, 20.2, wn=1.5, n=5, R=300, a0=-90, tr=4, sp=SPA, th=22, pl=list(range(6, 20)), dp=80, drop=flat(*[[b, k] for k, b in enumerate(DROPS)]))
for i in range(26):
    b = 6.5 + 0.5 * i; off = -90 + rint(SPA, 2, b) + 36 + (0 if i % 2 == 0 else 18)
    petal(b, ring_of(5, off), aa=24, bb=13, v=500, d0=30, ez=(i % 2 == 1))
# 0:47.5 "وكدامي جنتي تموت": the rose dies: a petal falls on each drum (flying out from where it was), the last one goes at 0:52
for k, b in enumerate(DROPS):
    ax = -90 + rint(SPA, 2, b) + 72 * k
    petal(b, [ax], aa=56, bb=30, v=620, d0=140)
for i, b in enumerate(range(7, 19, 2)): vine_aim(b, [0, 90, 180, -90][i % 4], A=18, lam=260, v=3000, th=18, ez=(i % 2 == 1))
fx(20, 0.3, 8, 1.0)
# 0:52 "وهسه تكلي انساك واكدر ابدلك" x2: three roses round the middle, each turning its own way; every two beats they change places
# ("ابدلك", a third of a turn round, twice one way then twice back); on every beat one of them throws its petals out, in turn
OST = flat(*[[b, 120 if (i // 2) % 2 == 0 else -120] for i, b in enumerate(range(21, 36, 2))])
ROSES3 = [(3, 45), (4, -45), (5, 45)]
for j, (n, s) in enumerate(ROSES3):
    rose(20, 36, x=CX, y=CY, orad=250, oa0=-90 + 120 * j, ost=OST, od=0.6, n=n, R=180, a0=0, sp=[20, s], th=20)
for i in range(31):
    b = 20 + 0.5 * i; j = i % 3; n, s = ROSES3[j]
    oa = -90 + 120 * j + sum(OST[q + 1] for q in range(0, len(OST), 2) if OST[q] + 1 <= b)
    x = CX + 250 * math.cos(math.radians(oa)); y = CY + 250 * math.sin(math.radians(oa))
    petal(b + 0.5, ring_of(n, s * (b + 0.5 - 20)), x=x, y=y, aa=26, bb=14, v=520, d0=60, ez=(i % 2 == 1))
for i, b in enumerate(range(20, 35)): vine_aim(b, [0, 90, 180, -90][i % 4], A=18, lam=260, v=3000, th=18, ez=(i % 2 == 1))
fx(36, 0.3, 8, 1.0)
# 1:01 "وينه كلامك": thorn vines grow across the garden one a beat, from the left and the right in turn, then two down; you keep to the
# lanes between them, and petals run along the lanes, one way then the other
VL = [(36, -20, 110, 0), (37, W + 20, 610, 180), (38, -20, 235, 0), (39, W + 20, 485, 180), (40, 420, -20, 90), (41, 860, H + 20, -90), (42, -20, 360, 0), (43, W + 20, 360, 180)]
for i, (b, x, y, a) in enumerate(VL):
    vine(b, 44, x, y, a, L=1340 if a in (0, 180) else 760, A=24, lam=260, ph=i, v=2200, th=18, ez=(i >= 6 and i % 2 == 1))
for i, b in enumerate([38.5, 39, 39.5, 40, 40.5, 41, 41.5, 42, 42.5, 43]):
    ly = [172, 547, 297, 422][i % 4]; left = i % 2 == 0
    petal(b, [0 if left else 180], x=-40 if left else W + 40, y=ly, aa=30, bb=15, v=900, ez=(i % 2 == 1))
fx(44, 0.3, 8, 1.0)
# 1:05.6 "هسه احنه عين بعين" x2: two roses face to face, turning against each other (the other way round at the second "عين بعين"),
# swelling every two beats; they throw their petals at each other; vines along the top and the bottom close the room
for s, x in [(1, 360), (-1, 920)]:
    sp = [44, 45 * s, 52, -45 * s]
    rose(44, 60, x=x, y=CY, n=4, R=250, a0=0 if s > 0 else 180, sp=sp, th=22, pl=list(range(44, 60, 2)), dp=70)
    for i, b in enumerate(range(45, 60)):
        petal(b, ring_of(4, (0 if s > 0 else 180) + rint(sp, 44, b) + (45 if i % 2 else 0)), x=x, y=CY, aa=30, bb=16, v=520, d0=40, ez=(i % 2 == 1))
for i, b in enumerate(range(46, 60, 2)):
    top = 70 if i % 2 == 0 else 100
    vine(b, b + 2.6, -20 if i % 2 == 0 else W + 20, top, 0 if i % 2 == 0 else 180, L=1340, A=20, lam=300, v=2200, th=18)
    vine(b, b + 2.6, W + 20 if i % 2 == 0 else -20, H - top, 180 if i % 2 == 0 else 0, L=1340, A=20, lam=300, v=2200, th=18, ez=True)
for i, b in enumerate(range(44, 59)): vine_aim(b, [90, -90][i % 2], A=18, lam=260, v=3000, th=18, ez=(i % 2 == 1))
fx(60, 0.35, 8, 1.0)
# 1:14.7 "مدري شنو النساك / كل عمري ما انساه": the great seven-petal rose, swelling on every beat, turning one way then back every two
# beats; between its petals small petals fly out to the sides on every beat, and vines grow down the two sides
SPE = flat(*[[b, 36 if i % 2 == 0 else -36] for i, b in enumerate(range(60, 76, 2))])
rose(60, 76, n=7, R=360, a0=-90, sp=SPE, th=22, pl=list(range(60, 76)), dp=60)
for i, b in enumerate(range(60, 76)):
    petal(b + 0.5, ring_of(7, -90 + 360 / 14 + rint(SPE, 60, b + 0.5)), aa=24, bb=13, v=540, d0=30, ez=(i % 2 == 1))
for i, b in enumerate(range(62, 76, 2)):
    vine(b, b + 2.6, 110, -20 if i % 2 == 0 else H + 20, 90 if i % 2 == 0 else -90, L=760, A=26, lam=200, v=2000, th=18)
    vine(b, b + 2.6, W - 110, H + 20 if i % 2 == 0 else -20, -90 if i % 2 == 0 else 90, L=760, A=26, lam=200, v=2000, th=18, ez=True)
for i, b in enumerate(range(60, 75)): vine_aim(b, [0, 90, 180, -90][i % 4], A=18, lam=260, v=3000, th=18, ez=(i % 2 == 1))
fx(76, 0.35, 10, 1.2)
# 1:23.7 "ويلي ويلي / خوفك يكتلك": vines on every half beat going round the four sides (top, right, bottom, left), each a step further
# along; on every second beat a ring of petals from the middle
for i in range(32):
    b = 76 + i * 0.5; side = i % 4; f = [0.25, 0.5, 0.75, 0.4, 0.6, 0.35, 0.65, 0.5][(i // 4) % 8]
    if side == 0: x, y, a, L = W * f, -20, 90, 760
    elif side == 1: x, y, a, L = W + 20, H * (1 - f), 180, 1340
    elif side == 2: x, y, a, L = W * (1 - f), H + 20, -90, 760
    else: x, y, a, L = -20, H * f, 0, 1340
    vine(b + 0.5, b + 2.5, x, y, a, L=L, A=22, lam=240, ph=i, v=2800, th=18, ez=(i % 2 == 1))
for i, b in enumerate(range(77, 92)):
    petal(b, ring_of(8, 22.5 * i), aa=28, bb=15, v=520, d0=20, ez=(i % 2 == 1))
for i, b in enumerate(range(77, 90, 2)): vine_aim(b, [0, 90, 180, -90][i % 4], A=18, lam=260, v=3000, th=18, ez=True)
fx(92, 0.3, 8, 1.0)
# 1:32.7 "اي وهسه تكلي انساك واكدر ابدلك": the three roses again, changing places on every beat, and throwing their petals
OSG = flat(*[[b, 120 if (i // 2) % 2 == 0 else -120] for i, b in enumerate(range(93, 100))])
for j, (n, s) in enumerate([(3, -50), (4, 50), (5, -50)]):
    rose(92, 100, x=CX, y=CY, orad=250, oa0=-90 + 120 * j, ost=OSG, od=0.5, n=n, R=180, a0=0, sp=[92, s], th=20)
for i, b in enumerate(range(92, 100, 2)): petal(b + 0.5, ring_of(9, 20 * i), aa=26, bb=14, v=480, d0=20)
for i, b in enumerate(range(92, 98)): vine_aim(b, [0, 90, 180, -90][i % 4], A=18, lam=260, v=3000, th=18, ez=(i % 2 == 1))
# 1:37 the music: the last rose opens wide from the middle; ride a gap between its petals as it turns (back the other way halfway)
rose(100, 106, wn=2, n=6, R=820, grow=3, a0=-90, sp=[100, 30, 103, -30], th=22)
fx(106, 0.4, 10, 1.4)

# ======== part 2: the mirror ========
# 1:41.8 "خنت الكضه وياك / هو النطاك الروح": the mirror is hit on every beat, at the points of a star round the middle; five cracks run
# out from each to the edges and stay a while; on "هيجي جزاته" it is hit where you stand (ten cracks)
for q, base in enumerate([-90, 90]):
    bs = list(range(108, 117)) if q == 0 else list(range(120, 125))
    for i, b in enumerate(bs):
        th = math.radians(base + 144 * i); rr = 280 if i % 2 == 0 else 170
        x = CX + rr * math.cos(th); y = CY + rr * 0.8 * math.sin(th)
        crack(b, x, y, 7, base + 144 * i + 36, hold=3, seed=10 + q * 20 + i, th=24); MC.append(b)
        if i % 2 == 1: petal(b + 0.5, ring_of(6, 30 * i), x=x, y=y, shape=1, aa=20, bb=11, sp=5, v=480, d0=20, ez=True)
    bb = 118 if q == 0 else 126
    crack_aim(bb - 1, 10, a0=18, hold=1.6, seed=60 + q, wn=1); MC.append(bb)
fx(118, 0.35, 10, 1.0); fx(126, 0.35, 10, 1.0)
# and pieces of it fall where you stand, between the hits
for i, b in enumerate([109, 111, 113, 115, 121, 123, 125]): drop_aim(b - 0.5, S=78, rot=40 * i, seed=700 + i, ez=(i % 2 == 1))
# 1:53.1 the same, stronger: pieces of the mirror fall on you (their shadows show where), two each beat marching across in turn from the
# left and the right, and break into splinters; every two beats a cut opens across the whole mirror, turning a step each time
for i in range(48):
    b = 128 + i * 0.5; col = (i // 2) % 8; x = 180 + 131 * col if (i // 16) % 2 == 0 else W - 180 - 131 * col
    y = 180 if i % 2 == 0 else 540
    drop(b + 0.5, x, y, S=80, rot=37 * i, seed=100 + i, ns=4, vs=480, hold=0.8, ez=(i % 2 == 1))
for i, b in enumerate(range(128, 152, 2)):
    cut(b, 30 * i + 15, w=90, op=0.5, hold=1.2, seed=200 + i, ez=(i % 2 == 1)); MC.append(b)
for i, b in enumerate(range(129, 151)): drop_aim(b - 0.5, S=78, rot=50 * i, seed=720 + i, ez=(i % 2 == 1))
fx(152, 0.35, 10, 1.0)
# 2:06.7 "يلكتلي غيري / ماغزرت الحضنات" x2: the mirror shows someone else: on every beat it is hit at your place in the mirror (the other
# side), and one of its cracks runs straight at you; between them pieces fall on the four quarters in turn
for i, b in enumerate(range(152, 176)):
    crack_aim(b - 1, 10, a0=0, hold=1.6, seed=300 + i, mir=True, wn=1, th=22, ez=(i % 2 == 1)); MC.append(b)
for i, b in enumerate(range(152, 176)):
    q = i % 4; x = [320, 960, 960, 320][q]; y = [190, 190, 530, 530][q]
    drop(b + 0.5, x, y, S=90, rot=53 * i, seed=400 + i, ns=5, vs=460, hold=0.7, ez=(i % 2 == 1))
fx(176, 0.35, 10, 1.0)
# 2:20.3 "وانت طلعت غدار حارس وبكت الدار / روح وبعد للموت تحرم عليه": forbidden: crosses cut through where you stand on every beat (an X,
# then a +), and at 2:24 the mirror breaks: cracks all round from the middle and its splinters fly out
for i, b in enumerate(range(176, 184)):
    for a in ([45, -45] if i % 2 == 0 else [0, 90]):
        cut_aim(b - 1, a, w=86, op=0.4, hold=0.6, seed=500 + i * 2 + (a > 0), wn=1, ez=(i % 2 == 1))
    MC.append(b)
crack(184, CX, CY, 16, 0, hold=1.4, seed=600, th=18); MC.append(184)
petal(184.5, ring_of(16, 11.25), shape=1, aa=24, bb=13, sp=6, v=560, d0=40)
fx(184, 0.5, 14, 1.6)

# ======== part 3: the bride ========
def hand(b):
    # where her hands are at beat b (as L16bBrideAt, with her size 1.15)
    x, y = BP[1], BP[2]
    for i in range(4, len(BP), 4):
        k = min(1, max(0, (b - BP[i]) / max(0.01, BP[i + 3])))
        if k <= 0: break
        k = k * k * (3 - 2 * k); x += (BP[i + 1] - x) * k; y += (BP[i + 2] - y) * k
    return x, y + 145
def fan(side, n, a0, a1, v0, v1):
    # coins thrown up to one side: angles a0..a1 (degrees, up is -90), speeds v0..v1
    an = [a0 + (a1 - a0) * i / (n - 1) for i in range(n)]; vs = [v0 + (v1 - v0) * i / (n - 1) for i in range(n)]
    if side > 0: an = [-180 - a for a in an]
    return an, vs
def coins(b, side, n=7, a0=-150, a1=-104, v0=560, v1=780, ez=False):
    x, y = hand(b); an, vs = fan(side, n, a0, a1, v0, v1); coin(b, x, y, an, vs, ez=ez)
fx(188, 0.4, 8, 1.6)
# 2:27 "اي هسه تكلي انساك واكدر ابدلك" x2: she comes in her wedding dress. The coins thrown over a bride rain from her hands on every half
# beat, a fan to the left, then to the right (shifted in the second line); every two beats she throws a ring at you ("ابدلك")
for i in range(28):
    b = 190 + i * 0.5; sh = 0 if b < 196 else 4
    coins(b, -1 if i % 2 == 0 else 1, 7, -150 + sh, -104 + sh, ez=(i % 2 == 1))
for i, b in enumerate(range(191, 204, 2)):
    x, y = hand(b); ring_aim(b - 1, x, y, v=950, ez=(i % 2 == 1))
# 2:36 "عين بعين" x2: her wedding rings turn round her, spinning like coins (edge on they are thin), flying out on every beat and back;
# at the second "عين بعين" they turn the other way. Coins between
for k in range(3):
    orbit(204, 220, CX, 300, 220, 120 * k, [204, 90, 212, -90], dor=220, pl=list(range(204, 220)), R=50, th=14, spin=0.4)
for i, b in enumerate(range(204, 220)):
    coins(b + 0.5, -1 if i % 2 == 0 else 1, 6, -140, -108, 640, 780, ez=(i % 2 == 1))
for i, b in enumerate(range(205, 219, 2)):
    x, y = hand(b); ring_aim(b - 1, x, y, v=950, ez=(i % 2 == 1))
fx(220, 0.3, 8, 1.0)
# 2:45 "مدري شنو النساك / كل عمري ما انساه": her veil's lace sweeps the room on every beat, across, back, down and up in turn; you go
# through its holes
LC = [(0, -40, 720, [150, 400, 640]), (0, W + 40, -720, [270, 520]), (1, -40, 520, [200, 520, 840, 1160]), (1, H + 40, -520, [360, 680, 1000]),
      (0, -40, 760, [100, 350, 600]), (0, W + 40, -760, [220, 470, 690]), (1, -40, 540, [120, 440, 760, 1080]), (1, H + 40, -540, [280, 600, 920, 1240])]
for i, b in enumerate(range(220, 236)):
    d, p0, v, hs = LC[i % 8]; lace(b, d, p0, v, hs, ez=(i % 2 == 1))
fx(236, 0.35, 8, 1.0)
# 2:54 "ويلي ويلي / خوفك يكتلك": on every beat she throws her bouquet where you are; it opens into a rose; coins between
for i, b in enumerate(range(236, 252)):
    x, y = hand(b); bq(b - 1.0, x, y, Rb=205, n=5, spin=0.8 * (1 if i % 2 == 0 else -1), hold=1.0, ez=(i % 2 == 1))
for i, b in enumerate(range(236, 252)):
    coins(b + 0.5, -1 if i % 2 == 0 else 1, 6, -150, -105, 560, 760, ez=(i % 2 == 1))
# 3:03 "اي هسه تكلي": rings at you on every half beat, from one hand then the other
for i in range(16):
    b = 252 + i * 0.5; x, y = hand(b); ring_aim(b - 0.8, x + (-40 if i % 2 == 0 else 40), y, v=1050, wn=0.8, ez=(i % 2 == 1))
fx(260, 0.35, 8, 1.2)
# 3:07.8 the music: she dances in the middle, and her skirt turns round her as a great eight-petal rose, swelling on every half beat,
# turning back on every beat; coins fly from her hands
rose(260, 268, wn=1.5, x=CX, y=340, n=8, R=340, a0=0, sp=flat(*[[b, 60 if i % 2 == 0 else -60] for i, b in enumerate(range(260, 268))]), th=22,
     pl=[260 + 0.5 * i for i in range(16)], dp=60)
for i, b in enumerate(range(260, 268)):
    coins(b + 0.5, -1 if i % 2 == 0 else 1, 6, -150, -105, 600, 780, ez=(i % 2 == 1))
for i, b in enumerate(range(261, 268)):
    x, y = hand(b); ring_aim(b - 1, x + (-40 if i % 2 == 0 else 40), y, v=950, ez=(i % 2 == 1))
fx(268, 0.35, 8, 1.0)
# 3:12.3 "رحت انت بس والله تالي تجيني / ترجع ذليل": on every beat she throws rings in a fan that go out and come back to her ("تالي
# تجيني"); every two beats her lace sweeps across
for i, b in enumerate(range(270, 288)):
    x, y = hand(b); sh = 12 if i % 2 == 0 else -12
    for a in [30 + sh, 72 + sh, 108 + sh, 150 + sh]:
        r = math.radians(a); throw(b, x, y, x + 560 * math.cos(r), y + 560 * math.sin(r), v=1250, back=0.5, R=44, th=12, ez=(i % 2 == 1))
for i, b in enumerate(range(271, 288, 2)):
    d = i % 2; lace(b, 0, -40 if d == 0 else W + 40, 620 if d == 0 else -620, [[180, 430, 660], [300, 550], [120, 370, 620], [240, 490, 700]][i % 4], ez=(i % 2 == 1))
fx(288, 0.4, 10, 1.2)
# 3:23.6 the same, stronger: rings out and back on every beat with coins, then the lace both ways on every beat with her bouquet, then
# her rings round her again with rings at you
for i, b in enumerate(range(288, 296)):
    x, y = hand(b); sh = [0, 20, -20, 10][i % 4]
    for a in ([25 + sh, 75 + sh, 125 + sh] if i % 2 == 0 else [55 + sh, 105 + sh, 155 + sh]):
        r = math.radians(a); throw(b, x, y, x + 520 * math.cos(r), y + 520 * math.sin(r), v=1000, back=0.5, R=42, th=12, ez=(i % 2 == 1))
    coins(b + 0.5, -1 if i % 2 == 0 else 1, 6, -150, -110, 600, 760, ez=True)
for i, b in enumerate(range(296, 304)):
    d = i % 2; lace(b, 0, -40 if d == 0 else W + 40, 600 if d == 0 else -600, [[140, 390, 640], [260, 510], [200, 450, 690], [80, 330, 580]][i % 4], ez=(i % 2 == 1))
for b in [297, 301]:
    x, y = hand(b); bq(b - 1.0, x, y, Rb=160, n=5, spin=0.7, hold=1.0)
for k in range(3):
    orbit(304, 311, CX, 300, 240, 90 + 120 * k, [304, 80, 308, -80], dor=180, pl=list(range(304, 311)), R=48, th=14, spin=0.5)
for i in range(12):
    b = 305 + i * 0.5; x, y = hand(b); ring_aim(b - 1, x, y, v=820, ez=(i % 2 == 1))
fx(312, 0.35, 8, 1.0)
# 3:37.2 "الي يبيع الناس مايلكى راحه": no rest: she dances in the middle again, her skirt's rose faster, coins from both sides every half beat
rose(312, 320, wn=1.5, x=CX, y=340, n=8, R=360, a0=22.5, sp=flat(*[[b, -70 if i % 2 == 0 else 70] for i, b in enumerate(range(312, 320))]), th=22,
     pl=[312 + 0.5 * i for i in range(16)], dp=60)
for i in range(15):
    b = 312.5 + i * 0.5; coins(b, -1 if i % 2 == 0 else 1, 6, -150, -105, 600, 780, ez=(i % 2 == 1))
for i, b in enumerate(range(313, 320)):
    x, y = hand(b); ring_aim(b - 1, x + (-40 if i % 2 == 0 else 40), y, v=980, ez=(i % 2 == 1))
fx(320, 0.35, 10, 1.0)
# 3:41.7 "يخلص العمر حسرات وهم ومناحه": she goes up and fades away; her lace sweeps across on every half beat, the holes stepping down,
# then up
for i in range(8):
    b = 320 + i; d = i % 2; base = [100, 160, 220, 280, 220, 160, 100, 160][i] + (0 if d == 0 else 125)
    hs = [y for y in (base, base + 250, base + 500) if y < 700]
    lace(b + 0.5, 0, -40 if d == 0 else W + 40, 640 if d == 0 else -640, hs, ez=(i % 2 == 1))
for i in range(8): coins(320.5 + i, -1 if i % 2 == 0 else 1, 5, -150, -110, 600, 760, ez=True)
fx(329.4, 0.4, 12, 1.8)

ev(0, 'L16bstage', pb=[0, 108, 188], to=331, bp=BP, mc=sorted(MC), keep=True)

# ---------------- out ----------------
def song(b):
    t = 40 + OFF + b * SPB
    return f'{int(t // 60)}:{t % 60:04.1f}'
EV.sort(key=lambda d: (d['b'], 0 if d['k'] == 'L16bstage' else 1))
ENERGY = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L16', 'name': 'المرحلة السادسة عشرة', 'song': 'عين بعين', 'file': 'SONG_L16', 'bpm': 106.1027, 'off': OFF, 'end': 329.4, 'cps': [0, 108, 188],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0}, 'energy': ENERGY, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'end', song(329.4))
