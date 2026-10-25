#!/usr/bin/env python3
# Makes data/dodge_L16.json: L16 "عين بعين" (Hussam Al-Rassam), the song's story line by line.
# The song file is the song from 0:40 (songs/SONG_L16.mp3). 106.1 BPM, a beat = 0.56549 s, beat 0 = 0.749 s in the file (0:40.75 in the song).
# The lines (beats, song time; found from the song's shape: the second verse is the first one's tune 160 beats later, a line is 8 beats):
#   0 (0:40.7) "انت ماظن نفسك انت" | 12 (0:47.5) "وكدامي جنتي تموت خوفك يكتلك" | 20-36 (0:52) "وهسه تكلي انساك واكدر ابدلك" x2
#   36 (1:01) "وينه كلامك ذاك حجيك اليه" | 44-60 (1:05.6) "هسه احنه عين بعين تحجي عليه" x2 | 60-76 (1:14.7) "مدري شنو النساك.. / كل عمري ما انساه"
#   76 (1:23.7) "ويلي ويلي" | 84 "خوفك يكتلك" | 92 "اي وهسه تكلي انساك واكدر ابدلك" | 100 music
#   108 (1:41.8) "خنت الكضه وياك عمره وحياته / هو النطاك الروح هيجي جزاته", 128 (1:53.1) the same, stronger
#   152 (2:06.7) "يلكتلي غيري لكيت / ماغزرت الحضنات" x2 | 176 (2:20.3) "وانت طلعت غدار حارس وبكت الدار / روح وبعد للموت تحرم عليه"
#   188 (2:27) "اي هسه تكلي انساك" | 196 "وينه كلامك" | 204 "عين بعين" x2 | 220 "مدري شنو النساك" | 236 "ويلي ويلي" | 244 "خوفك يكتلك"
#   252 "اي هسه تكلي" | 260 music | 268 (3:12.3) "رحت انت بس والله تالي تجيني / ترجع ذليل تصير وتبوس اديني", 288 (3:23.6) stronger
#   312 (3:37.2) "الي يبيع الناس مايلكى راحه" | 320 (3:41.7) "يخلص العمر حسرات وهم ومناحه" | the end 329.4 (3:47.0)
# Parts: 1 the night by the river (0-107), 2 the memories (108-187), 3 she appears (188-329.4). Checkpoints 0, 108, 188.
# Fixed attacks fire before their warning starts (b = hit - wn * EZW, Easy's longest warning); aimed ones ("rnd") lock on the square at b.
import json, math, os, random

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
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def aim(b, k, **kw): ev(b, k, rnd=True, **kw)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L16fx', v=v, s=s, dur=dur)
def fixed(hit, k, wn=1, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)

def rain(hit, lean, gx, x0=0, sp=105, v=950, gw=150, ez=False):
    kw = dict(x0=x0, sp=sp, lean=lean, gx=gx, gw=gw, len=190, w=15, v=v)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16rain', **kw)
TGA = [0]
def tear(b, n=2, ez=False):
    TGA[0] += 67
    kw = dict(wn=1.3, sr=80, ga=TGA[0] % 180, gw=44, v=420, R=520, th=18)
    if ez: kw['ezskip'] = True
    aim(b, 'L16tear', **kw)
def bolt(b, x=None, seed=1, ez=False):
    kw = dict(seed=seed, th=26, wn=1, hold=0.35)
    if x is None: kw['rnd'] = True
    else: kw['x'] = x
    if ez: kw['ezskip'] = True
    ev(b, 'L16bolt', **kw)
def frond(hit, left, py, down, dur=1.4, ez=False):
    px = -30 if left else W + 30
    a0, a1 = (-62, 62) if left else (242, 118)
    if not down: a0, a1 = a1, a0
    kw = dict(px=px, py=py, a0=a0, a1=a1, L=760, dur=dur)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16frond', **kw)
def tide(hit, top, lvl=320, dur=1.8, ez=False):
    kw = dict(top=top, lvl=lvl, dur=dur)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16tide', **kw)
def fan(hit, ga, ez=False, n=28, v=540):
    kw = dict(ga=ga, gw=44, n=n, v=v, r=13)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16fan', wn=0.75, **kw)
def slash(b, a, ez=False):
    kw = dict(a=a, w=110, wn=1.3, hold=0.4)
    if ez: kw['ezskip'] = True
    aim(b, 'L16slash', **kw)
def dash(b, vert, d, ez=False):
    kw = dict(vert=vert, dir=d, wn=1.15, dur=0.5)
    if ez: kw['ezskip'] = True
    aim(b, 'L16dash', **kw)
def pillar(b, ez=False):
    kw = dict(w=170, wn=1.2, hold=0.45)
    if ez: kw['ezskip'] = True
    aim(b, 'L16pillar', **kw)
def close(hit, ga, gs, ez=False):
    kw = dict(R0=820, v=330, th=30, ga=ga, gs=gs, gw=50)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16close', **kw)

ev(0, 'L16stage', pb=[0, 108, 188, 268], to=331, keep=True)
random.seed(16)
GX = [300, 820, 520, 1040, 220, 700, 980, 420, 860, 300, 620, 1060, 380, 760, 240, 940]

# ======== part 1: the night by the river (she is not seen) ========
# 0:40.7 the opening: rain curtains every 2 beats, the wind turns each time
for i, b in enumerate(range(2, 12, 2)): rain(b, 0.25 if i % 2 == 0 else -0.25, GX[i], x0=(i * 37) % 105, v=850)
fx(12, 0.3, 8, 1.0)
# 0:47.5 "وكدامي جنتي تموت.. / تكلي انساك": a curtain every beat, the wind turns every 2 beats; from 0:52 her tears fall on you
for i, b in enumerate(range(12, 36)): rain(b, 0.35 if (b // 2) % 2 == 0 else -0.35, GX[(i + 5) % 16], x0=(i * 52) % 105)
for b in range(20, 36, 2): tear(b + 0.5, ez=(b % 4 == 2))
# 1:01 "وينه كلامك": the tears, one each beat, and a light rain
for b in range(36, 44): tear(b, ez=(b % 2 == 1))
for i, b in enumerate(range(37, 44, 2)): rain(b, 0.3 if i % 2 == 0 else -0.3, GX[(i * 3) % 16], sp=210, x0=40)
# 1:05.6 "عين بعين" x2: the Tigris, three heavy waves; the second time faster, and tears on top
fixed(44, 'L16waves', n=3, y0=170, dy=190, lift=70, wl=1.6, A=60, L=170, ws=2.4, th=40, to=52)
fixed(52, 'L16waves', n=3, y0=170, dy=190, lift=90, wl=2.2, A=75, L=150, ws=3.2, th=40, to=60)
for b in range(53, 60, 2): tear(b, n=1, ez=True)
fx(60, 0.3, 8, 1.0)
# 1:14.7 "مدري شنو النساك / كل عمري ما انساه": the storm, lightning on you every beat, and from the sides
for i, b in enumerate(range(59, 75)): bolt(b, seed=i + 1)
for i, b in enumerate(range(60, 76, 2)): bolt(b + 0.5, x=[160, 1120, 400, 880][i % 4], seed=40 + i, ez=True)
for i, b in enumerate(range(61, 76, 2)): rain(b, 0.3 if i % 2 == 0 else -0.3, GX[(i * 5) % 16], sp=210)
fx(76, 0.35, 10, 1.2)
# 1:23.7 "ويلي ويلي / خوفك يكتلك": the rain and two waves together, the lightning every 2 beats
fixed(76, 'L16waves', n=2, y0=230, dy=260, lift=60, wl=1.4, A=55, L=180, ws=2.2, th=34, to=92)
for i, b in enumerate(range(76, 92)): rain(b, 0.35 if (b // 2) % 2 == 0 else -0.35, GX[(i * 7) % 16], sp=140, ez=(b % 2 == 1))
for i, b in enumerate(range(83, 99, 2)): bolt(b, seed=60 + i)
# 1:32.8 "اي وهسه تكلي": the tears, every beat, two rings
for b in range(92, 100): tear(b, ez=(b % 2 == 1))
for i, b in enumerate(range(92, 100, 2)): rain(b + 1, 0.3 if i % 2 == 0 else -0.3, GX[(i * 9) % 16], sp=140)
# 1:37.3 the music: tears and two last bolts
for b in range(100, 106): tear(b, n=1, ez=(b % 2 == 1))
bolt(103.5, seed=90); bolt(105.5, seed=91)
fx(108, 0.3, 8, 1.2)

# ======== part 2: the memories (still unseen) ========
# 1:41.8 "خنت الكضه.. / هو النطاك الروح": palm fronds swing like slow pendulums, one each beat from the sides in turn
for i, b in enumerate(range(109, 128)):
    left = i % 2 == 0
    frond(b, left, [130, 590][(i // 2) % 2] if left else [360, 200, 520][(i // 2) % 3], (i // 4) % 2 == 0, ez=(i % 4 == 3))
fx(128, 0.3, 8, 1.0)
# 1:53.1 the lift: the river rises from below, then from above, every 2 beats, with a light rain; from 2:00 the fronds come back
for i, b in enumerate(range(128, 152, 2)): tide(b, i % 2 == 1, lvl=310)
for i, b in enumerate(range(129, 140)): rain(b, 0.3 if i % 2 == 0 else -0.3, GX[(i * 3) % 16], sp=210, ez=(i % 2 == 1))
for i, b in enumerate(range(141, 152, 2)): frond(b, i % 2 == 0, 360, i % 2 == 0, ez=(i % 2 == 1))
fx(152, 0.3, 8, 1.0)
# 2:06.7 "يلكتلي غيري لكيت / ماغزرت الحضنات": incense smoke twists up in 5 columns; the second time the columns drift, and tears fall
fixed(152, 'L16smoke', wn=1.5, cx=[128 + 256 * k for k in range(5)], A=100, r=32, dr=0, to=164)
fixed(164, 'L16smoke', wn=1.5, cx=[128 + 256 * k for k in range(5)], A=105, r=30, dr=170, to=176)
for b in range(165, 176, 2): tear(b, n=1, ez=True)
fx(176, 0.35, 10, 1.2)
# 2:20.3 "وانت طلعت غدار حارس وبكت الدار / روح وبعد للموت": the storm, lightning on you every beat, the river from both sides
for i, b in enumerate(range(175, 186)): bolt(b, seed=120 + i)
tide(178, False, lvl=230); tide(181, True, lvl=230, ez=True); tide(184, False, lvl=230)
for i, b in enumerate(range(176, 186, 2)): rain(b + 1, 0.3 if i % 2 == 0 else -0.3, GX[(i * 11) % 16], sp=210)

# ======== part 3: she appears ========
fx(188, 0.6, 10, 1.6)
# 2:27 "اي هسه تكلي انساك" / "وينه كلامك": her hair, 5 strands turning, the way they turn changes every 4 beats
fixed(189, 'L16hair', n=5, ws=[0.45, -0.55, 0.5, -0.6], len=1500, th=38, to=204)
# 2:36 "عين بعين" x2: she walks from side to side and sends rings of light, the gap somewhere else each time
for i, b in enumerate([205 + 0.5 * k for k in range(30)]):
    if b >= 220: break
    fan(b, (i * 71) % 360, ez=(i % 2 == 1))
fx(220, 0.3, 8, 1.0)
# 2:45 "مدري شنو النساك": X slashes on you every beat, and a ring every 2 beats
for i, b in enumerate(range(220, 236)): slash(b, 45 if i % 2 == 0 else 0)
for i, b in enumerate(range(221, 236, 2)): fan(b + 0.5, (i * 97) % 360, ez=True, n=24)
fx(236, 0.35, 10, 1.2)
# 2:54 "ويلي ويلي / خوفك يكتلك": the hair again, 6 strands, and rings
fixed(237, 'L16hair', n=6, ws=[-0.5, 0.6, -0.55, 0.65], len=1500, th=36, to=251.5)
for i, b in enumerate(range(239, 251, 2)): fan(b, (i * 83 + 40) % 360, n=24, ez=(i % 2 == 1))
# 2:59 "اي هسه تكلي": she is gone, her pink shadow dashes across your line every beat
for i, b in enumerate(range(251, 259)): dash(b, i % 2 == 0, 1 if (i // 2) % 2 == 0 else -1)
# 3:07.8 the music: the spiral of her dance
ev(259.9, 'L16spiral', arms=6, pt=0.125, v=360, sp=0.9, r=13, t0=260, t1=287.5)
fx(268, 0.35, 10, 1.2)
# 3:12.3 "رحت انت بس.. / ترجع ذليل": the spiral goes on, and pillars of light fall on you
for i, b in enumerate(range(267, 287, 2)): pillar(b, ez=(i % 2 == 1))
fx(288, 0.35, 10, 1.2)
# 3:23.6 the same, stronger: her shadow dashes every beat, and from 3:30 two at once (down and across)
for i, b in enumerate(range(287, 311)):
    dash(b, i % 2 == 0, 1 if (i // 2) % 2 == 0 else -1)
    if b >= 299 and i % 2 == 0: dash(b + 0.02, False, -1 if (i // 2) % 2 == 0 else 1, ez=True)
fx(312, 0.3, 8, 1.0)
# 3:37.2 "الي يبيع الناس مايلكى راحه": she is back: slashes every beat, a ring between them
for i, b in enumerate(range(311, 319)): slash(b, 0 if i % 2 == 0 else 45)
for i, b in enumerate(range(312, 320)): fan(b + 0.5, (i * 113) % 360, n=24, ez=True)
# 3:41.7 "يخلص العمر حسرات وهم ومناحه": the rings of her dress close in on her, one each beat, each gap turning; she fades at 3:47
for i, b in enumerate(range(320, 328)): close(b, (i * 137) % 360, 0.6 if i % 2 == 0 else -0.6, ez=(i % 2 == 1))
fx(329.4, 0.4, 12, 1.8)

# ---------------- out ----------------
def song(b):
    t = 40 + OFF + b * SPB
    return f'{int(t // 60)}:{t % 60:04.1f}'
EV.sort(key=lambda d: (d['b'], 0 if d['k'] == 'L16stage' else 1 if d['k'] == 'L16face' else 2))
ENERGY = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L16', 'name': 'المرحلة السادسة عشرة', 'song': 'عين بعين', 'file': 'SONG_L16', 'bpm': 106.1027, 'off': OFF, 'end': 329.4, 'cps': [0, 108, 188],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0}, 'energy': ENERGY, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'end', song(329.4))
