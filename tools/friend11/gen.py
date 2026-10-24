#!/usr/bin/env python3
# Makes data/dodge_F11.json: F11 "أجمل غرامي" (Abdullah Al Farwan), miracle's level. The song file starts at the song's 0:20.
# 100.91 BPM, beat 0 = 0.5405 s into the file (0:20.54 in the song), a beat = 0.59459 s. Each beat has three hits (0, 0.3, 0.6: dum, tak, tak).
# Song map (beats): intro 0-4 | singing from 4 (0:22.9) | change 28 (0:37.2) | quieter break 108 (1:25.2) | back up 140 (1:43.8)
# | dip 212-216 (2:26.6) | win 258 (2:54.0)
# Parts: 1 the eyes (0-108), 2 the night and the road (108-212), 3 the heart and the hands (212-258). Checkpoints 0, 108, 212.
# Nothing here is random: every attack is a fixed pattern on the beat (the gazes aim where the player is). An event fires at its start
# minus EZW times its warning (Easy shows the warning longer).
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_F11.json')
W, H = 1280, 720
BPM, OFF = 100.91, 0.5405
SPB = 60 / BPM
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW - 0.05, k, hit=hit, wn=wn, **kw)
def tri(u): u = u % 1; return 4 * u - 1 if u < 0.5 else 3 - 4 * u   # -1..1..-1

# ================= part 1: the eyes (0-108) =================
ev(0, 'F11stage', part=1, to=108, keep=True)

# 0:22 the sadu bands: woven strips cross the screen in steps on the dum and the second tak; the opening (the diamond of the weave) zigzags
BW, ST, GH = 70, 80, 78
def band(b0, vert, fwd, ph, amp=160, per=8, ez=False):
    L = W if vert else H; n = int(math.ceil((L + BW) / ST)) + 1
    ts = [b0 + (i // 2) + (0.6 if i % 2 else 0) for i in range(n)]
    c = H / 2 if vert else W / 2
    gs = [c + amp * tri(i / per + ph) for i in range(n)]
    st = ST if fwd else -ST
    p0 = BW / 2 - ST if fwd else L - BW / 2 + ST
    at(b0, 1.5, 'F11band', vert=vert, p0=p0, st=st, bw=BW, ts=ts, gs=gs, gh=GH, ezskip=ez)
# 0:22 columns from the sides in turn
for j, b in enumerate([4, 8, 12, 16]): band(b, True, j % 2 == 0, 0.25 * j)
# 0:32 rows from the top and the bottom
for j, b in enumerate([20, 22, 24, 26]): band(b, False, j % 2 == 0, 0.5 + 0.25 * j, amp=360, ez=j % 2 == 1)
# 0:37 the weave: columns and rows together
for j, (b, vert, fwd) in enumerate([(28, True, True), (29, False, True), (31, True, False), (32, False, False), (34, True, True)]):
    band(b, vert, fwd, 0.3 * j, amp=160 if vert else 360, ez=j == 4)

# 0:40 the kohl eyes open over the dunes: they look only at you
ev(33, 'F11eyes', to=108)
# 0:42 one gaze a beat, the eyes in turn (aimed where you are when the line shows)
for j, b in enumerate(range(37, 53)): at(b, 1.0, 'F11gaze', rnd=True, eye=j % 2, bw=28, hold=0.4, ezskip=j % 2 == 1)
# 0:52 the blinks: an eye blinks and its lashes fly out in a fan, the other eye keeps looking at you
for j, b in enumerate(range(53, 72, 2)):
    eye = j % 2; base = 90 + (12 if j % 4 < 2 else -12)
    at(b, 1.0, 'F11lash', eye=eye, angs=[base + d for d in (-54, -36, -18, 0, 18, 36, 54)], sp=330, len=44, lw=9)
    at(b + 1, 1.0, 'F11gaze', rnd=True, eye=1 - eye, bw=28, hold=0.4, ezskip=True)

# 1:04 the circumstances: walls of sand close on you from both sides; where they meet there is one hole
# (one every 4 beats; each hole near the last one, so the way from one to the next is short)
WALLS = [(True, 640, 360), (False, 360, 520), (True, 420, 300), (False, 260, 560), (True, 680, 220),
         (False, 380, 820), (True, 860, 460), (False, 560, 700), (True, 560, 420)]
for j, (vert, seam, hc) in enumerate(WALLS):
    T = 74 + 4 * j
    at(T, 1.0, 'F11close', vert=vert, seam=seam, hc=hc, hh=88, dep=60, mv=1.5, hold=0.4)
    if j % 2 == 1: at(T + 2, 1.0, 'F11gaze', rnd=True, eye=j // 2 % 2, bw=28, hold=0.4, ezskip=True)

# ================= part 2: the night and the road (108-212) =================
ev(108, 'F11stage', part=2, to=212, keep=True)
ev(108, 'F11fx', v=0.35, s=6, dur=1.2)

# 1:25 the road: it winds down the screen; outside it the sand turns pink
RD = dict(v=200, a1=200, l1=300, p1=0.0, a2=60, l2=150, p2=1.3, ramp=900, hw=120)
ROAD_HIT, ROAD_TO = 111, 133
at(ROAD_HIT, 3.0, 'F11road', to=ROAD_TO, **RD)
def road_x(y, b):
    s = RD['v'] * (b - ROAD_HIT) * SPB + (H - y); k = min(1, max(0, s / RD['ramp']))
    return W / 2 + k * (RD['a1'] * math.sin(s / RD['l1'] + RD['p1']) + RD['a2'] * math.sin(s / RD['l2'] + RD['p2']))
# 1:30 shooting stars fall across the road, one on each second tak, on one side of it then the other
DUR = 1.4
for j, b in enumerate([x + 0.6 for x in range(117, 132)]):
    yt = 430 + 90 * tri(j / 6); fr = (yt + 30) / (H + 60); bc = b + DUR * fr
    xt = road_x(yt, bc) + (1 if j % 2 == 0 else -1) * RD['hw'] * 0.45
    sl = 260 if j % 2 == 0 else -260
    x0 = xt - sl * fr
    at(b, 1.0, 'F11star', x0=x0, y0=-30, x1=x0 + sl, y1=H + 30, r=13, dur=DUR, ezskip=j % 2 == 1)

# 1:36 the coffee pot in the middle: it pours a curved stream of coffee that sweeps round
ev(128, 'F11dallah', x=640, y=360, a0=-90, to=164)
for j, b in enumerate(range(131, 164, 2)):
    s0 = -90 + 45 * j; d = 1 if j % 2 == 0 else -1
    at(b, 1.0, 'F11pour', x=640, y=360, a0=s0, a1=s0 + d * 130, sw=1.4, len=620, lw=22, cv=0.18)
# 1:43 the music comes back: a star on every second tak, sweeping across the sky
for j, b in enumerate([x + 0.6 for x in range(140, 164)]):
    q = j % 16; x0 = 120 + 130 * (q if q < 8 else 15 - q); sl = 220 if (j // 4) % 2 == 0 else -220
    at(b, 1.0, 'F11star', x0=x0, y0=-30, x1=x0 + sl, y1=H + 30, r=13, dur=1.3, ezskip=j % 2 == 1)

# 1:58 the incense: rings of smoke grow from the burner, each one's two openings turned a little more; the coffee pot pours from the top
ev(164, 'F11burner', x=640, y=400, to=212)
ev(164, 'F11dallah', x=640, y=78, a0=90, to=212)
ga = 90.0
for j, b in enumerate(range(166, 207)):
    ga += 20 if (j // 8) % 2 == 0 else -20
    at(b, 1.0, 'F11smoke', x=640, y=400, ga=[ga, ga + 180], gw=75, sp=150, th=16, rmax=900, ezskip=j % 2 == 1 and j >= 8)
for j, b in enumerate(range(168, 210, 4)):
    a0, a1 = (25, 155) if j % 2 == 0 else (155, 25)
    at(b, 1.0, 'F11pour', x=640, y=78, a0=a0, a1=a1, sw=2.0, len=700, lw=22, cv=0.1)

# ================= part 3: the heart and the hands (212-258) =================
ev(212, 'F11stage', part=3, to=260, keep=True)
ev(212, 'F11fx', v=0.4, s=8, dur=1.4)
ev(212, 'F11heart', x=640, y=360, to=257.5)

# 2:28 the hands come in from the sides, one step on every dum; their fingers between each other's
FL, FH = 230, 58
kb, kl = [214], [-FL]
for b in range(215, 257):
    kb.append(b)
    if b <= 232: kl.append(-FL + (30 + FL) * (b - 214) / 18)
    elif b <= 250: kl.append(30 + 200 * (b - 232) / 18)
    else: kl.append(230 + 70 * (b - 250) / 6)
kr = [W - x for x in kl]
S0 = ([120, 360, 600], [240, 480, 720]); S1 = ([180, 420, 660], [60, 300, 540])
fb, ly, ry = [], [], []
b = 214
while b < 257:
    s = S0 if len(fb) % 2 == 0 else S1; fb.append(b); ly += s[0]; ry += s[1]
    b += 4
ev(214, 'F11hands', to=257.5, kb=kb, kl=kl, kr=kr, fb=fb, ly=ly, ry=ry, nf=3, fl=FL, fh=FH)

# 2:29 the heart beats: two waves a time (dum, then the second tak; from 2:34 one), with two openings that turn round the heart
for j, b in enumerate(range(216, 232, 2)):
    c = 0.25 + 0.06 * j
    for k, dt in enumerate((0, 0.6) if b < 224 else (0,)):
        cc = c + 0.035 * k
        gp = [(cc - 0.08) % 1, (cc + 0.08) % 1, (cc + 0.5 - 0.08) % 1, (cc + 0.5 + 0.08) % 1]
        at(b + dt, 1.0, 'F11hwave', x=640, y=360, gp=gp, sp=150, th=14, rmax=900, ezskip=k == 1)
# 2:36 the henna turns round the heart, 30 degrees on each dum, turning back every 4 beats
hb, ha, a = [232], [0.0], 0.0
for b in range(233, 250):
    a += 30 if ((b - 233) // 4) % 2 == 0 else -30; hb.append(b); ha.append(a)
at(232, 2.0, 'F11henna', x=640, y=360, n=6, r0=95, r1=250, cr=16, kb=hb, ka=ha, to=250)
for j, b in enumerate(range(236, 250, 4)):
    c = 0.1 + 0.25 * j
    at(b, 1.0, 'F11hwave', x=640, y=360, gp=[(c - 0.08) % 1, (c + 0.08) % 1, (c + 0.42) % 1, (c + 0.58) % 1], sp=150, th=14, rmax=900, ezskip=True)
# 2:47 the last heartbeats while the hands close in
for j, b in enumerate([250, 251.6, 253, 254.6, 256]):
    c = 0.75 + 0.05 * j
    at(b, 1.0, 'F11hwave', x=640, y=360, gp=[(c - 0.09) % 1, (c + 0.09) % 1, (c + 0.41) % 1, (c + 0.59) % 1], sp=150, th=14, rmax=900, ezskip=j % 2 == 1)
ev(257.5, 'F11clasp')

EV.sort(key=lambda d: d['b'])
en = json.load(open(os.path.join(HERE, 'energy.json')))['energy']
D = {'id': 'F11', 'name': 'أجمل غرامي', 'by': 'miracle', 'ver': 'Mohammed', 'file': 'SONG_F11', 'bpm': BPM, 'off': OFF, 'end': 258, 'cps': [0, 108, 212],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': en, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print(len(EV), 'events')
