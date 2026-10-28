#!/usr/bin/env python3
# Makes data/dodge_L16.json: L16 "عين بعين" (Hussam Al-Rassam), the third design (v52): the shanasheel of an old Baghdad house at night.
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
# Parts: 1 the window (0-107), 2 the house (108-187), 3 the dome (188-329.4). Checkpoints 0, 108, 188 (his word 10-25: two saves).
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
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L16bfx', v=v, s=s, dur=dur)
def fixed(hit, k, wn=1, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)
def flat(*rows): return [x for r in rows for x in r]

def lat(hit, to, nw=False, **kw):
    if nw: ev(hit, 'L16blat', hit=hit, to=to, nw=True, **kw)
    else: fixed(hit, 'L16blat', to=to, **kw)
def star(hit, gi, v=430, R0=30, rot=0, gn=2, th=26, ez=False, wn=1):
    kw = dict(gi=gi, v=v, R0=R0, rot=rot, gn=gn, th=th)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16bstar', wn=wn, **kw)
def shot(hit, n, off, v=380, back=0, Rs=520, ez=False):
    kw = dict(n=n, off=off, v=v, back=back, Rs=Rs)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16bshot', wn=0.75, **kw)
def arch(hit, d, o, v=300, P=320, ow=150, T=80, ez=False):
    kw = dict(dir=d, o=o, v=v, P=P, ow=ow, T=T)
    if ez: kw['ezskip'] = True
    fixed(hit, 'L16barch', **kw)
def dagger(b, a, ez=False):
    kw = dict(a=a, w=70, wn=1, hold=0.35, rnd=True)
    if ez: kw['ezskip'] = True
    ev(b, 'L16bdagger', **kw)
def door(hit, gx, sw=120, hold=1.0): fixed(hit, 'L16bdoor', wn=1.3, gx=gx, sw=sw, cl=0.35, hold=hold)
def rock(b0, b1, sign=1):
    # slats rocking: half a gap each beat, two forward, two back
    st = []
    for i, b in enumerate(range(b0, b1)): st += [b, sign * (110 if (i // 2) % 2 == 0 else -110), 0]
    return st

ev(0, 'L16bstage', pb=[0, 108, 188], to=331, keep=True)

# ======== part 1: the window ========
# 0:40.7 "مو حبيبي": the window's slats, rocking with the song (half a gap each beat), then the other way across
lat(2, 8.05, fam=1, s=220, a0=45, th=28, th1=40, pl=[4, 6], st=rock(3, 8))
lat(8.05, 12, fam=1, s=220, a0=-45, th=28, th1=40, pl=[8.05, 10], st=rock(9, 12, -1))
fx(12, 0.3, 8, 1.0)
# 0:47.5 "وكدامي جنتي تموت / تكلي انساك واكدر ابدلك" x2: the whole window over the room; you live in its holes. It drifts, its wood swells
# on the drum, and on the third beat of each bar it jumps half a hole (down, then across), so you jump with it
SW = flat(*[[b, 115, 115 if i % 2 == 0 else -115] for i, b in enumerate(range(14, 36, 4))])
# (from "هسه تكلي" at 0:52 it turns, and from 0:56.6 it turns back and drifts the other way)
lat(12, 36, fam=2, s=230, a0=45, th=24, th1=34, pl=list(range(12, 36, 4)), st=SW, sp=[12, 0, 20, 3, 28, -3], dr=[12, 14, 0, 20, 0, 0, 28, -12, 6])
fx(36, 0.3, 8, 1.0)
# 1:01 "وينه كلامك": eight-pointed stars ring out from the middle every beat, the gap walking round
for i, b in enumerate(range(36, 44)): star(b + 0.5, [2, 3, 2, 1, 2, 3, 4, 3][i], v=400, rot=0 if i % 2 == 0 else 22.5, ez=(i % 2 == 1))
# 1:05.6 "عين بعين" x2: two round windows face to face, turning around each other, each lattice turning its own way; at the second
# "عين بعين" they turn back
for q in range(2):
    oa = 180 * q
    lat(44, 52, fam=2, s=165, a0=0, spin=6, th=24, th1=36, pl=[44, 46, 48, 50], R=330, orad=250, oa0=oa, ospin=7, cx=CX, cy=CY)
    lat(52, 60, nw=True, fam=2, s=165, a0=6 * 8, spin=-6, th=24, th1=36, pl=[52, 54, 56, 58], R=330, orad=250, oa0=oa + 7 * 8, ospin=-7, cx=CX, cy=CY)
fx(60, 0.35, 8, 1.0)
# 1:14.7 "مدري شنو النساك / كل عمري ما انساه": the rosette: 8 petals flick round a little on each beat (back the other way from 1:19),
# reach out on the drums, and stars fly out between them on each bar
RS = flat(*[[b, 11.25 if b < 68 else -11.25] for b in range(61, 76)])
fixed(60, 'L16brose', n=8, r0=44, rl=430, rh=600, pw=120, a0=22.5, st=RS, pl=list(range(60, 76, 2)), to=76)
for i, b in enumerate(range(60, 76, 4)): shot(b + 1, 16, 22.5 + 11.25 * (i % 2), v=360, ez=(i % 2 == 1))
fx(76, 0.35, 10, 1.2)
# 1:23.7 "ويلي ويلي / خوفك يكتلك": the window again, square, turning slowly; it jumps on the third beat of each bar and on the hits
SW2 = flat(*[[b, 105 if i % 2 == 0 else 0, 0 if i % 2 == 0 else 105] for i, b in enumerate([78, 82, 86, 90])])
lat(76, 92, fam=2, s=210, a0=0, spin=3.5, th=26, th1=46, pl=[76, 80, 84, 87.25, 88, 91.25], st=SW2)
# 1:32.8 "اي وهسه تكلي انساك واكدر ابدلك": star rings every beat, the gap stepping round; a big one on "ابدلك"
for i, b in enumerate(range(92, 100, 2)): star(b + 0.5, (i * 1 + 5) % 8, rot=22.5 * (i % 2), ez=(i % 2 == 1))
star(99, 1, v=520, rot=0, gn=4, th=34)
# 1:37.3 the music: the slats rocking, slower, till the save
lat(100, 106, fam=1, s=240, a0=-45, th=26, th1=36, pl=[100, 103.25, 104], st=[101, 120, 0, 103, -120, 0, 105, 120, 0])
fx(108, 0.3, 8, 1.2)

# ======== part 2: the house ========
# 1:41.8 "خنت الكضه وياك": the courtyard floor: the stars burn, then the crosses between them, you step from one to the other;
# on "هيجي جزاته" only one star is left safe (gold)
def fl(b, kind, dur): return [b, kind, dur, 0, 0]
FL = dict(c=300, e=16)
def one(hit, fb, wn=2.0, dur=1.2, **kw): ev(hit - wn, 'L16bone', rnd=True, wn=wn, dur=dur, fb=fb, ezskip=True, **FL, **kw)
PR = flat(fl(108, 0, 1.4), fl(110, 1, 1.4), fl(112, 0, 1.4), fl(114, 1, 1.4),
          fl(118, 0, 0.5), fl(119, 1, 0.5), fl(120, 0, 0.5), fl(121, 1, 0.5), fl(122, 0, 1.4), fl(124, 1, 1.4))
fixed(108, 'L16bfloor', pr=PR, pwn=1, to=127.3, **FL)
one(116.5, 108)
one(126.2, 108, dur=1.0)
fx(128, 0.3, 8, 1.0)
# 1:53.1 the same, stronger: the arcades of the house fall through the room, one every 2 beats, the arches half a step apart each time;
# from 2:00 others cross from the left between them
for i, b in enumerate(range(128, 147, 2)): arch(b, 0, (i * 160) % 320, v=280, ow=160, ez=(i % 2 == 1))
for i, b in enumerate(range(137, 142, 4)): arch(b, 2, 40 + (i * 150) % 300, v=280, P=300, ow=160, ez=True)
fx(135.25, 0.25, 6, 0.6)
fx(152, 0.3, 8, 1.0)
# 2:06.7 "يلكتلي غيري / ماغزرت الحضنات" x2: the floor again, turning; the second time slower steps with the dagger on the beats between
PR2 = flat(*[fl(152 + 2 * k, k % 2, 1.4) for k in range(4)], *[fl(162 + 2 * k, (k + 1) % 2, 1.4) for k in range(6)])
fixed(152, 'L16bfloor', spin=2.2, pr=PR2, pwn=1, to=174, **FL)
one(160.4, 152, spin=2.2)
for i, b in enumerate(range(163, 174, 4)): dagger(b, [0, 90, 45][i % 3], ez=(i % 2 == 1))
fx(176, 0.35, 10, 1.2)
# 2:20.3 "وانت طلعت غدار حارس وبكت الدار": the traitor's dagger at you every beat; "روح وبعد للموت تحرم عليه": the doors close on you,
# one slit left
for i, b in enumerate(range(175, 182)): dagger(b, [0, 45, 90, 135][i % 4], ez=(i % 2 == 1))
door(182.5, 860)
door(185, 420, hold=0.8)

# ======== part 3: the dome ========
fx(188, 0.5, 10, 1.6)
# 2:27 "اي هسه تكلي انساك / وينه كلامك": the dome's ribs turn, the way they turn changes on every bar; star rings ring out between them
fixed(188.5, 'L16bribs', n=6, tw=0.0016, th=30, a0=0, sp=[188.5, 10, 192, -10, 196, 12, 200, -12], to=204)
for i, b in enumerate(range(190, 204, 2)): star(b + 0.5, (i * 3) % 8, v=400, rot=22.5 * (i % 2), ez=(i % 2 == 1))
fx(204, 0.3, 8, 1.0)
# 2:36 "عين بعين" x2: the round windows again, diamond holes, faster round each other
for q in range(2):
    oa = 90 + 180 * q
    lat(204, 212, fam=2, s=170, a0=45, spin=-8, th=24, th1=36, pl=[204, 206, 208, 210], R=330, orad=250, oa0=oa, ospin=10, cx=CX, cy=CY)
    lat(212, 220, nw=True, fam=2, s=170, a0=45 - 8 * 8, spin=8, th=24, th1=36, pl=[212, 214, 216, 218], R=330, orad=250, oa0=oa + 10 * 8, ospin=-10, cx=CX, cy=CY)
fx(220, 0.3, 8, 1.0)
# 2:45 "مدري شنو النساك": the rosette under the dome, its stars flying out every 2 beats
RS2 = flat(*[[b, -11.25 if b < 228 else 11.25] for b in range(221, 236)])
fixed(220, 'L16brose', n=8, r0=44, rl=460, rh=640, pw=130, a0=0, st=RS2, pl=list(range(220, 236, 2)), to=236)
for i, b in enumerate(range(221, 236, 2)): shot(b, 16, 11.25 * (i % 2), v=380, ez=(i % 2 == 1))
fx(236, 0.35, 10, 1.2)
# 2:54 "ويلي ويلي / خوفك يكتلك": the floor, every beat, drifting up
PR3 = flat(*[fl(236 + 2 * k, k % 2, 1.4) for k in range(4)])
fixed(236, 'L16bfloor', vy=-22, pr=PR3, pwn=1, to=243.6, **FL)
# 2:58.7 "اي هسه تكلي": the window, turning, jumping every 2 beats
SW3 = flat(*[[b, 110, 110 if i % 2 == 0 else -110] for i, b in enumerate([245, 249])])
lat(244, 252, fam=2, s=220, a0=45, spin=-3, th=24, th1=38, pl=[244, 247.25, 248], st=SW3)
# 3:03.3 the arcades from every side in turn; the music (3:07.8): the dagger every beat and the doors on the hit
for i, b in enumerate(range(252, 260, 2)): arch(b, [0, 2, 1, 3][i], (i * 110) % 320, v=320)
for i, b in enumerate(range(259, 263)): dagger(b, [45, 135, 0, 90][i % 4])
door(263.25, 640, hold=0.9)
for i, b in enumerate(range(265, 267)): dagger(b, [90, 0][i % 2])
fx(268, 0.35, 10, 1.2)
# 3:12.3 "رحت انت بس والله تالي تجيني / ترجع ذليل": stars shot out that stop, slide half a step and come back to the middle
for i, b in enumerate(range(268, 288, 2)): shot(b, 18, 10 * i, v=420, back=0.5, Rs=520, ez=(i % 2 == 1))
for i, b in enumerate(range(279, 288, 2)): dagger(b, [0, 90, 45, 135][i % 4], ez=True)
fx(288, 0.35, 10, 1.2)
# 3:23.6 the same, stronger: a volley every beat, and the ribs again from 3:30
for i, b in enumerate(range(288, 300)): shot(b, 14, 7 * i, v=440, back=0.5, Rs=480, ez=(i % 2 == 1))
fixed(300, 'L16bribs', n=4, tw=-0.0018, th=30, a0=45, sp=[300, 9, 304, -9, 308, 11], to=312)
for i, b in enumerate(range(301, 312, 2)): shot(b, 14, 13 * i, v=440, back=0.5, Rs=480, ez=True)
fx(312, 0.3, 8, 1.0)
# 3:37.2 "الي يبيع الناس مايلكى راحه": the window with no rest: drifting, turning, jumping every 2 beats, swelling on every beat
SW4 = flat(*[[b, 105 if i % 2 == 0 else -105, 105] for i, b in enumerate([313, 315, 317, 319])])
lat(312, 320, fam=2, s=210, a0=45, spin=3, th=24, th1=38, pl=list(range(312, 320, 2)), st=SW4, dx=-12, dy=8)
# 3:41.7 "يخلص العمر حسرات وهم ومناحه": star rings closing in on the middle, one each beat, the gap stepping round; at 3:47 it is over
for i, b in enumerate(range(320, 328)): star(b + 1.5, (i * 1) % 8, v=-470, R0=1000, rot=22.5 * (i % 2), gn=2, th=28, ez=(i % 2 == 1))
fx(329.4, 0.4, 12, 1.8)

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
