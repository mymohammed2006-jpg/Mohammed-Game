#!/usr/bin/env python3
# Makes data/dodge_L18.json: L18 "اعوف الدنيا" (Mohamed Alsalim), the second design (v52; the first is data/dodge_L18_v51.json from gen.py,
# kept for old replays). Same song, start, end, checkpoints and ids as before. Kinds in src/Game/Dodge/L18b.cs.
# A sad Iraqi song: the level is SADNESS and MUSIC, and every time the song changes its sound, the attack and the look change (Mohammed 10-28).
# 102 BPM, beat 0 = song 0:50.15, a beat = 0.58825 s; the song's three hits in a beat land at 0, 0.43 and 0.74 of it.
# Where the sound changes (beats, found from the song's own sound bands, beat by beat):
#   0 calm (0:50) | 12 the beat comes in (0:57.2) | 49 the chorus, loud (1:19.0) | 81 its second half (1:37.8) | 114 quiet (1:57.2)
#   | 130 the groove (2:06.6) | 162 soft (2:25.4) | 187 rising (2:40.2) | 225 the last chorus (3:02.5) | 256 its second half (3:20.7) | win 289 (3:40.2)
# Checkpoints 0, 115, 224. Nothing is random or aimed.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L18.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3          # Easy's longest warning stretch: an event fires this much earlier than its Normal warning
HITS = (0, 0.43, 0.74)
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def scene(sc, b0, b1, **kw): ev(max(0, b0 - 4), 'L18bscene', sc=sc, fr=b0, to=b1, keep=True, **kw)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L18bfx', v=v, s=s, dur=dur)

# ---------- 0:50 calm: the dark room, the candle, the oud. The tears fall, three on the three hits of each beat, walking across ----------
scene(1, 0, 12)
fx(0, 0.15, 4, 1.0)
xs, tb = [], []
for b in range(2, 12):
    for j, f in enumerate(HITS):
        xs.append(110 + 132 * ((b * 2 + j * 3) % 9)); tb.append(b + f)
        if b >= 3 and j != 1: xs.append(W - (110 + 132 * ((b * 2 + j * 3) % 9)) + 66); tb.append(b + f)
ev(0.2, 'L18btears', x=xs, tb=tb, ft=1.4, r=17, wn=1.0, to=13.8)

# ---------- 0:57 the beat comes in: the sad song's notes run along the staff from the right, on the hits; a melody that goes up and down,
# from 1:04 (b24) chords on the first hit, from 1:11 (b36) a second voice ----------
scene(2, 12, 49)
fx(12, 0.12, 4, 0.8)
mel = [2, 4, 6, 8, 7, 5, 3, 5, 9, 7, 5, 3, 1, 3, 5, 4]
pl, tb, fl = [], [], []
k = 0
for b in range(13, 49):
    for j, f in enumerate(HITS):
        q = mel[k % len(mel)]; k += 1
        pl.append(q); tb.append(b + f); fl.append(1 if j else 0)
        if b >= 18 and j == 0:
            for c in (4, 7) if b % 2 == 0 else (-4, 3):
                if 0 <= q + c <= 10: pl.append(q + c); tb.append(b + f); fl.append(0)
        if b >= 30 and j == 2: pl.append(10 - q); tb.append(b + f); fl.append(1)
ev(10.5, 'L18bstaff', sp=55, pl=pl, tb=tb, fl=fl, v=650, r=16, wn=1.0, tw=11.5, to=50.5)

# ---------- 1:19 the chorus: the violin cries. Its bow sweeps on every beat (the grey line shows where it goes), its sound goes out in rings
# (with a gap) on the other two hits; every 8 beats the violin moves ----------
scene(3, 49, 81)
fx(49, 0.3, 9, 1.4)
mb = [47, 55.5, 63.5, 71.5]; mx = [900, 380, 760, 470]; my = [380, 330, 300, 420]
sb, ba = [47], [200]
a = 200
for i, b in enumerate(range(49, 81, 2)):
    a += 95 if i % 2 == 0 else -95
    if i % 4 == 3: a += 50
    sb.append(b); ba.append(a)
rb, ga = [], []
g0 = 0
for i, b in enumerate(range(50, 81)):
    rb.append(b + 0.43); ga.append(g0); g0 = (g0 + (40 if (i // 4) % 2 == 0 else -40)) % 360
ev(46.0, 'L18bviolin', mb=mb, mx=mx, my=my, sb=sb, ba=ba, rb=rb, ga=ga, bl=1150, bw=16, sdur=0.45, rv=330, rth=14, gw=95, ezk=2, wn=1.0, tw=47.2, to=81.3)

# ---------- 1:37 its second half: the old record turns. Its grooves burn (a new set every 2 beats, grey the beat before), its arm swings over it,
# notes jump off its rim in fans to the sides on the second and third hits; every 8 beats it slides ----------
scene(4, 81, 114)
fx(81, 0.25, 8, 1.2)
rings = [100, 150, 200, 250, 300]
ar, aa, aw, on, off = [], [], [], [], []
for i, b in enumerate(range(82, 114, 2)):
    for k, r in enumerate(rings):
        start = (i * 67 + k * 71) % 360
        span = 150 + 40 * ((i + k) % 3)
        ar.append(r); aa.append(start); aw.append(span); on.append(b); off.append(b + 2)
qb, qa = [80], [150]
for i, b in enumerate(range(82, 114, 2)):
    qb.append(b + 1); qa.append([152, 128, 140, 118, 160, 132][i % 6])
nb, na = [], []
for i, b in enumerate(range(83, 114)):
    # on the second hit a fan of 8 notes jumps off the rim to the right, on the third to the left; the fan swings up and down and its notes
    # step half a place every beat, so the sides are never quiet
    c = 22 * math.sin(i * 0.9)
    for f, side in ((0.43, 0), (0.74, 180)):
        for j in range(8): nb.append(b + f); na.append(round((side + c * (1 if side == 0 else -1) + (j - 3.5 + (0.5 if i % 2 else 0)) * 15) % 360, 2))
# every 8 beats the record slides to another place (its new place shows in grey 2 warnings before), so no corner stays safe
ev(79.5, 'L18brecord', RR=325, th=12, nv=380, nr=13, armL=560, armW=16, qx=1100, qy=60, a0=0, ssb=[80, 97], ssw=[0.45, -0.55],
   mb=[80, 89, 97, 105], mx=[640, 440, 800, 520],
   ar=ar, aa=aa, aw=aw, on=on, off=off, qb=qb, qa=qa, nb=nb, na=na, wn=1.0, tw=80.5, to=114.3)

# ---------- 1:57 quiet: the rain on the window, slow drops that sway down the glass ----------
scene(5, 114, 130)
fx(114, 0.2, 5, 1.2)
xs, tb, wa = [], [], []
for b in range(115, 130):
    for j, f in enumerate(HITS):
        xs.append(30 + 122 * ((b * 4 + j * 5) % 11)); tb.append(b + f); wa.append(40 + 10 * ((b + j) % 3))
        xs.append(30 + 122 * ((b * 4 + 7 + j) % 10) + 61); tb.append(b + f); wa.append(55)
        xs.append(30 + 122 * ((b * 4 + 3 + j * 2) % 10) + 30); tb.append(b + f + 0.2); wa.append(48)
ev(113.3, 'L18brain', x=xs, tb=tb, wa=wa, vy=480, wf=0.55, r=13, wn=1.0, to=130.5)

# ---------- 2:06 the groove: the radio's sound bars; their opening is a melody that moves on one bar every beat (from 2:13, b142, twice a beat),
# the bars close in a little on the last hit ----------
scene(6, 130, 162)
fx(130, 0.25, 8, 1.2)
pc = []
for i in range(80):
    pc.append(round(CY + 200 * math.sin(i * 0.42) * (0.6 + 0.4 * math.sin(i * 0.11 + 1)), 1))
sb = []
for b in range(132, 162):
    sb.append(b)
    if b >= 142: sb.append(b + 0.43)
pb = [b + 0.74 for b in range(132, 162)]
ev(128.5, 'L18beq', nc=16, pc=pc, sb=sb, pb=pb, gap=165, pg=34, tw=129.3, ta=130.6, to=162.2)

# ---------- 2:25 soft: the tears fall on the river under the moon; each makes two rings that spread (a gap in them) ----------
scene(7, 162, 187)
fx(162, 0.2, 5, 1.2)
xs, ys, tb, gs = [], [], [], []
for i, b in enumerate(range(163, 187)):
    for j, f in enumerate(HITS):
        u = i * 3 + j
        xs.append(round(CX + 540 * math.sin(u * 0.83), 1)); ys.append(round(360 + 265 * math.sin(u * 1.31 + 0.5), 1)); tb.append(b + f); gs.append((u * 97) % 360)
ev(161.0, 'L18bripple', x=xs, y=ys, tb=tb, ga=gs, rv=260, rmax=360, th=11, gw=70, wn=1.0, to=188)

# ---------- 2:40 rising: the heart beats faster and faster (every beat, then twice, then on every hit); each beat sends a heart-shaped wave out,
# its arteries burn on the last hit ----------
scene(8, 187, 225)
fx(187, 0.25, 7, 1.2)
hb = [b for b in range(188, 206)] + [b + f for b in range(206, 216) for f in (0, 0.5)] + [b + f for b in range(216, 225) for f in HITS]
wb = [b for b in range(189, 206, 2)] + [b for b in range(206, 224)]
wg = []
g = 90
for i in range(len(wb)):
    wg.append(g % 360); dg = 24 if wb[i] < 206 else 13          # every beat from b206: the gap moves less, so far from the heart you can still follow it
    g += dg if (i // 5) % 2 == 0 else -dg
vb = [b + 0.74 for b in range(197, 224, 2)]
ev(185.5, 'L18bbeat', hs=62, wv=240, wth=14, gw=100, vw=12, vd=0.25, na=6, a0=0, ssb=[186, 206, 216], ssw=[0.15, -0.2, 0.22],
   hb=hb, wb=wb, wg=wg, vb=vb, wn=1.0, tw=186.5, ta=187.5, to=225)

# ---------- 3:02 the last chorus: the heart breaks in two; the halves go round, apart and back, and shards fly out of the crack on the hits ----------
scene(9, 225, 256)
fx(225, 0.45, 12, 1.6)
sb, sa = [], []
a = 0
for b in range(226, 256):
    for f in HITS:
        for m in range(3): sb.append(b + f); sa.append((a + m * 120) % 360)
        a = (a + 47) % 360
ev(222.5, 'L18bbroke', hs=150, d0=320, d1=130, sv=500, sr=12, fw=50, ns=7, a0=0, ssb=[222, 240], ssw=[0.7, -0.8], rsw=[0.9, -1.0],
   sb=sb, sa=sa, wn=1.0, tw=223, ta=225, to=256.3)

# ---------- 3:20 its second half: the last song. The staff bent into rings, the notes run round them (each ring the other way); the tears fall
# again; at 3:39.6 (b288) it all stops and the candle goes out ----------
scene(10, 256, 289.5, out=289)
fx(256, 0.35, 10, 1.4)
rr = [150, 245, 340, 435, 530, 625, 720]
ri, a0, tb, te, w = [], [], [], [], []
for i, b in enumerate(range(257, 288)):
    for j, f in enumerate(HITS):
        for m in range(3):
            k = (i + j * 2 + m * 3) % 7
            ri.append(k); a0.append((i * 71 + j * 120 + m * 120) % 360); tb.append(b + f); te.append(min(b + f + 4.5, 288.3)); w.append(2.5 if k % 2 == 0 else -2.5)
ev(254.5, 'L18bround', rr=rr, ri=ri, a0=a0, tb=tb, te=te, w=w, r=15, wn=1.0, tw=255.2, to=288.5)
xs, tb = [], []
for b in range(258, 288, 1):
    for f in HITS:
        xs.append(80 + 140 * ((b * 3 + int(f * 10)) % 9)); tb.append(b + f)
ev(256.0, 'L18btears', x=xs, tb=tb, ft=1.6, r=16, wn=1.0, to=289.5)
fx(288, 0.5, 6, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
acc = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L18', 'name': 'المرحلة الثامنة عشرة', 'song': 'اعوف الدنيا', 'file': 'SONG_L18', 'bpm': 101.997, 'off': 0.150, 'end': 289, 'cps': [0, 115, 224],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezwn': 1.25}, 'energy': acc['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
