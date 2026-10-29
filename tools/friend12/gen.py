#!/usr/bin/env python3
# Makes data/dodge_F12.json: F12 "Can't Stop The Feeling" (Justin Timberlake), Coulexrl's level, verified by Mohammed.
# The song file is the song from its start (songs/SONG_F12.mp3 = the first 3:16). 113.00 BPM, beat 0 = 0.405 s, a beat = 0.53097 s.
# Song map (beats): intro guitar 0-16 | verse 1 from 16 (0:08.9) | the drums + "sunshine in my pocket" 48 (0:25.9)
# | "under the lights" 80 (0:42.9) | chorus 1 112 (0:59.9) | verse 2, quiet, 160 (1:25.4) | "sunshine" again 192 (1:42.4)
# | "under the lights" again 224 (1:59.3) | chorus 2 256 (2:16.3) | the breakdown 320 (2:50.3) | the hush 352 (3:07.3) | win 354 (3:08.4)
# Parts: 1 the electricity in the bones (0-112), 2 the sunshine in the pocket (112-256), 3 can't stop the feeling (256-354).
# Checkpoints 0, 112, 256. Every part a little harder than the one before (his rule, about 5%).
# Nothing here is random: every attack is a fixed pattern on the beat; the ones marked rnd aim at where the player is when they show.
# An event fires at its hit minus EZW times its warning (Easy shows the warning longer).
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_F12.json')
W, H = 1280, 720
BPM, OFF = 113.0, 0.405
SPB = 60 / BPM
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(max(0, b), 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW - 0.05, k, hit=hit, wn=wn, **kw)
def stage(b, to, sc, **kw): ev(b, 'F12stage', sc=sc, to=to, keep=True, **kw)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'F12fx', v=v, s=s, dur=dur)
def bolt(hit, pts, wn=1.0, w=22, dur=1.0, sd=0, **kw): at(hit, wn, 'F12bolt', pts=[p for xy in pts for p in xy], w=w, dur=dur, sd=sd, **kw)
def quad(a, q, b, n=18):
    return [((1 - u) ** 2 * a[0] + 2 * u * (1 - u) * q[0] + u * u * b[0], (1 - u) ** 2 * a[1] + 2 * u * (1 - u) * q[1] + u * u * b[1]) for u in [i / n for i in range(n + 1)]]

# ================= part 1: the electricity in the bones (0-112) =================
stage(0, 16.5, 1)
# 0:01 the guitar: on every chord a live cable jumps across the dark room as a staircase, from wall to wall (a socket at each end).
# The stairs go down and up in turn and their middle steps up and down, so the side you are on gets cut: cross between two chords.
# From 0:04 every chord brings two at once, one down and one up, crossing: wait in one of the diamonds between them.
def stairs(mid, dy):
    pts = []; y = mid - dy * 4; pts.append((-10, y))
    for s in range(8):
        x = (s + 1) * 160; pts.append((x, y))
        if s < 7: y += dy; pts.append((x, y))
    return pts
# from 0:04 the chords come quicker: a cable on every beat
for j, b in enumerate([2, 4, 6, 7, 8, 9, 10, 11, 12, 13]):
    mid = 360 + [-90, 60, 120, -60, -120, 90, 0, -100, 110, -30][j]
    bolt(b, stairs(mid, 58 if j % 2 == 0 else -58), wn=1.0, w=20, dur=1.25, sd=j)
    if b >= 6: bolt(b, stairs(720 - mid, -58 if j % 2 == 0 else 58), wn=1.0, w=20, dur=1.25, sd=j + 20, ezskip=True)

# 0:09 "inside my bones": the x-ray. Two live bands close in from the top and the bottom to a wave: you ride the corridor.
# 0:13 "electric, wavy when I turn it on": the wave grows. Sparks run along the corridor from the right: take the other half.
stage(15.5, 32.5, 2)
fx(16, 0.25, 6, 1.0)
sp = []
for b in (18, 20, 22): sp += [b, 0, 0]
for j, b in enumerate(range(24, 31)): sp += [b + (0.5 if j % 2 else 0), 1 if j % 2 == 0 else -1, 1 if j % 2 else 0]
at(16, 2.0, 'F12wave', cy=360, kb=[16, 20, 23.5, 24.5, 31], ka=[0, 100, 100, 150, 150], lam=1250, v=300, gap=200, to=31.4,
   sp=sp, sv=560, sr=16, swn=1.0)

# 0:17 "all through my city, all through my home": the city at night. The electricity jumps from roof to roof in arcs and hangs down
# from the top as power lines in turn, so the sky and the street are cut one after the other.
BX = [0, 120, 150, 130, 100, 230, 240, 140, 120, 390, 110, 260, 510, 150, 170, 670, 100, 300, 780, 140, 140, 930, 120, 250, 1060, 110, 160, 1180, 100, 280]
stage(31.5, 48.5, 3, bx=BX)
def roof(i): return (BX[3 * i] + BX[3 * i + 1] / 2, H - BX[3 * i + 2])
ARCS = [(0, 4, 'up'), (None, (180, 760, 470), 'sag'), (3, 8, 'up'), (None, (560, 1270, 440), 'sag'), (1, 5, 'up'), (None, (0, 600, 500), 'sag'), (6, 9, 'up')]
for j, (a, bb, kind) in enumerate(ARCS):
    b = 33 + j
    if kind == 'up':
        A, B_ = roof(a), roof(bb); q = ((A[0] + B_[0]) / 2, min(A[1], B_[1]) - 520)
        bolt(b, quad(A, q, B_), wn=0.8, w=36, dur=1.6, sd=10 + j)
    else:
        x0, x1, sag = bb; q = ((x0 + x1) / 2, sag * 2 - 0)
        bolt(b, quad((x0, -6), q, (x1, -6)), wn=0.8, w=36, dur=1.6, sd=10 + j)
# 0:21 "flying up, no ceiling": the ceiling breaks and its pieces fly up from the roofs in fans, left and right in turn
for j, b in enumerate(range(40, 48)):
    i = [1, 8, 3, 6, 0, 9, 4, 5][j]; x0, y0 = roof(i); n = 10
    for q in range(n):
        an = math.radians(-90 + (-70 + 140 * q / (n - 1)) + (8 if j % 2 else -8))
        L = 1100; at(b, 1.0, 'F12proj', sh=0, x0=x0, y0=y0, x1=x0 + math.cos(an) * L, y1=y0 + math.sin(an) * L, r=21, spin=1.4 if q % 2 else -1.4, dur=1.0, sd=j * 10 + q,
                     ezskip=q % 2 == 1)

# 0:26 the drums come in. "Got that good soul in my feet": giant soles stamp on every beat, walking a dance across the floor
stage(47.5, 80.5, 4)
fx(48, 0.3, 8, 1.2)
def walker(b0, b1, x0, y0, x1, y1, side_amp, ang, step_out, hold=2.0, L=262, ez_every=0):
    n = b1 - b0
    for i in range(n):
        u = i / max(1, n - 1); x = x0 + (x1 - x0) * u; y = y0 + (y1 - y0) * u
        right = i % 2 == 1; nx, ny = -math.sin(math.radians(ang)), math.cos(math.radians(ang))
        o = side_amp * (1 if right else -1)
        at(b0 + i, 1.0, 'F12sole', x=x + nx * o, y=y + ny * o, a=ang + (step_out if right else -step_out), L=L, right=right, hold=hold,
           ezskip=bool(ez_every and i % ez_every == ez_every - 1))
# a walk from the left to the right, then back along another line ("I got that sunshine in my pocket")
walker(49, 57, 120, 520, 1160, 300, 70, -12, 8)
# and a second one the other way, higher
walker(49, 57, 1160, 560, 120, 260, 64, 192, 8, ez_every=2)
# "good soul in my feet": two dancers walk at once, crossing
walker(57, 65, 1160, 140, 140, 600, 64, 155, 8, ez_every=2)
walker(57, 65, 140, 140, 1160, 600, 64, 25, 8)
# every bar the stamp shakes the floor: a ring from the stamping foot
for j, b in enumerate([52, 56, 60, 64]):
    x, y = [(470, 450), (850, 380), (640, 370), (640, 370)][j]
    at(b, 1.0, 'F12ring', x=x, y=y, r0=20, r1=900, w=22, gaps=[j * 70 + 30, j * 70 + 210], gw=11, dur=3.0, ezskip=j == 1)
# 0:34 "I feel that hot blood in my body when it drops": hot drops fall in a staircase of columns
for j, b in enumerate([65, 66, 67, 68]):
    for q in range(5):
        x = 60 + q * 260 + j * 65
        at(b, 0.8, 'F12proj', sh=2, x0=x, y0=-40, x1=x, y1=H + 60, r=20, dur=0.9, sd=200 + j * 5 + q, ezskip=q % 2 == 1)
# the big drop falls in the middle and splashes on "drops" and on the "ooh"
at(69, 1.2, 'F12proj', sh=2, x0=640, y0=-80, x1=640, y1=380, r=58, dur=1.0, sd=300)
fx(70, 0.2, 10, 1.0)
at(70, 1.0, 'F12ring', x=640, y=380, r0=40, r1=950, w=34, gaps=[0, 180], gw=15, dur=2.6)
at(71, 1.0, 'F12ring', x=640, y=380, r0=40, r1=950, w=34, gaps=[90, 270], gw=20, dur=2.6)
# 0:37 "I can't take my eyes up off it, moving so phenomenally": the feet again, quicker, stamping round the middle
for i, b in enumerate(range(72, 80)):
    an = i * 90 + 45; rr = 250
    at(b, 1.0, 'F12sole', x=640 + math.cos(math.radians(an)) * rr, y=360 + math.sin(math.radians(an)) * rr * 0.75, a=an + 90, L=250, right=i % 2 == 1, hold=1.4)
# "room on lock": the room locks: two live bars close the sides
bolt(76, [(250, -10), (250, H + 10)], wn=1.0, w=26, dur=3.6, sd=40)
bolt(76, [(1030, -10), (1030, H + 10)], wn=1.0, w=26, dur=3.6, sd=41)
for i, b in enumerate([77, 78, 79]):
    at(b, 1.0, 'F12sole', x=[520, 760, 640][i], y=[260, 470, 360][i], a=[90, -90, 0][i], L=270, right=i % 2 == 0, hold=0.9)

# 0:43 "under the lights when everything goes": the stage. Searchlights on the truss swing like pendulums
stage(79.5, 112.5, 5)
fx(80, 0.25, 6, 1.0)
LAMP = [(200, 22), (520, 22), (760, 22), (1080, 22)]
def beam(hit, lamp, kb, ka, to, ha=9, wn=1.0, **kw):
    ox, oy = lamp; at(hit, wn, 'F12beam', ox=ox, oy=oy, kb=kb, ka=ka, ha=ha, len=1100, to=to, **kw)
def swing(b0, b1, mid, amp, per, ph):
    kb, ka = [], []; n = int((b1 - b0) / (per / 2)) + 1
    for i in range(n): kb.append(b0 + i * per / 2); ka.append(mid + amp * (1 if (i + ph) % 2 == 0 else -1))
    return kb, ka
kb, ka = swing(81, 97, 90, 66, 5, 0); beam(81, LAMP[0], kb, ka, 96)
kb, ka = swing(81, 97, 90, 66, 5, 1); beam(81, LAMP[3], kb, ka, 96)
# "nowhere to hide when I'm getting you close": the two middle lamps find you, twice a bar
kb, ka = swing(88, 104, 90, 36, 4, 0); beam(88, LAMP[1], kb, ka, 104, ha=8)
for j, b in enumerate(range(90, 104, 2)):
    lamp = LAMP[2] if j % 2 == 0 else LAMP[1]
    beam(b, lamp, [b, b + 0.6, b + 1.6], [90, 90, 90], b + 1.9, ha=7, wn=1.5, aim=True, rnd=True, ezskip=j % 2 == 1)
# "when we move, well, you already know": floor lamps in the corners cross the stage
beam(96, (40, H - 20), [96, 101.5, 107], [-75, -40, -75], 107, ha=5.5)
beam(100, (W - 40, H - 20), [100, 105.5], [-105, -145], 107, ha=6.5, ezskip=True)
# 0:53 "so just imagine, just imagine, just imagine": three big shapes draw themselves, then burn
def star(r, n=5, k=0.45): return [c for i in range(2 * n) for c in ((r if i % 2 == 0 else r * k) * math.cos(math.pi * i / n - math.pi / 2), (r if i % 2 == 0 else r * k) * math.sin(math.pi * i / n - math.pi / 2))]
def ngon(r, n): return [c for i in range(n) for c in (r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n))]
at(102, 1.6, 'F12shape', x=380, y=330, pts=star(290), rot=0, hold=1.0)
at(104, 1.6, 'F12shape', x=900, y=390, pts=ngon(270, 8), rot=22.5, hold=1.0)
at(106, 1.6, 'F12shape', x=640, y=360, pts=[-430, 300, 0, -330, 430, 300], rot=0, hold=1.0)
# "nothing I can see but you": the lights come together on you, then all go dark for the chorus
for j, lamp in enumerate(LAMP):
    beam(107.5, lamp, [107.5, 108.3, 109.3], [90, 90, 90], 109.6, ha=5, wn=1.5, aim=True, rnd=True, ezskip=j % 2 == 1)

# ================= part 2: the sunshine in the pocket (112-256) =================
# The giant neon dancer: its dance is the attack. A pose: x, y, rot, size, then the limbs (deg; 0 right, 90 down):
# l upper arm, l forearm, r upper arm, r forearm, l thigh, l shin, r thigh, r shin
P = {
    'idle':  [100, 95, 80, 85, 100, 95, 80, 85],
    'T':     [180, 180, 0, 0, 100, 95, 80, 85],
    'Y':     [225, 230, -45, -50, 100, 95, 80, 85],
    'disR':  [140, 130, -55, -60, 100, 95, 75, 80],     # Saturday night: right hand to the sky
    'disL':  [235, 240, 40, 50, 105, 100, 80, 85],
    'pL':    [180, 180, 100, 95, 100, 95, 80, 85],     # pointing left
    'pR':    [100, 95, 0, 0, 100, 95, 80, 85],
    'up':    [250, 260, -70, -80, 100, 95, 80, 85],
    'kR':    [200, 210, -20, -30, 100, 95, 15, 5],     # kick right
    'kL':    [200, 210, -20, -30, 165, 175, 80, 85],
    'split': [200, 200, -20, -20, 160, 170, 20, 10],
    'wind':  [150, 200, 30, -20, 110, 90, 70, 90],
    'low':   [160, 160, 20, 20, 150, 60, 30, 120],
}
def hips(keys, b):
    k = [q for q in keys if q[0] <= b]; q = k[-1] if k else keys[0]; return q[1], q[2]
def dance3(keys, beats, gw=27, ez=True):
    # "dance, dance, dance": a ring of light from the dancer's hips on each word, the gaps turning
    for j, b in enumerate(beats):
        x, y = hips(keys, b); at(b, 1.0, 'F12ring', x=x, y=y, r0=40, r1=1000, w=24, gaps=[(j % 3) * 60 + 30, (j % 3) * 60 + 210], gw=gw, dur=2.6, ezskip=ez and j % 3 == 1)
def dancer(t0, to, keys, style=0, mv=0.36, wn=1.0, lw=34):
    kt, kp = [], []
    for (b, x, y, rot, s, name) in keys: kt.append(b); kp += [x, y, rot, s] + P[name]
    at(t0, wn, 'F12dancer', style=style, kt=kt, kp=kp, mv=mv, lw=lw, to=to)

stage(111.5, 160.5, 6)
fx(112, 0.35, 10, 1.4)
S1 = 1.15
K = []
def key(b, x, y, rot, name, s=S1): K.append((b, x, y, rot, s, name))
# "nothing I can see but you when you dance, dance, dance"
key(112, 640, 400, 0, 'idle'); key(114, 640, 400, 0, 'T'); key(116, 600, 400, 0, 'Y'); key(117, 600, 400, 0, 'pL'); key(118, 680, 400, 0, 'pR'); key(119, 640, 400, 0, 'up')
# "feel a good, good creepin' up on you" (the light creeps in from the edges): it slides from side to side pointing
key(121, 470, 400, 0, 'disR'); key(123, 820, 400, 0, 'disL'); key(125, 470, 400, 0, 'disR'); key(127, 640, 400, 0, 'T')
# "so just dance, dance, dance, come on": it spins with its arms out, then kicks
key(130, 640, 400, 180, 'T'); key(132, 640, 400, 360, 'T'); key(133, 640, 420, 360, 'kR'); key(134, 640, 420, 360, 'kL'); key(135, 640, 470, 360, 'split')
# "all those things I shouldn't do": it goes to the left, its arms wind round
key(136.5, 460, 400, 360, 'wind'); key(138, 430, 400, 330, 'T'); key(139.5, 430, 400, 390, 'T')
# "but you dance, dance, dance": three to the right
key(141, 480, 400, 360, 'pR'); key(142, 520, 400, 360, 'kR'); key(143, 560, 400, 360, 'disR')
# "and ain't nobody leavin' soon, so keep dancing": a long turn across the stage
key(145, 640, 400, 450, 'T'); key(147, 800, 400, 540, 'T'); key(149, 860, 400, 630, 'Y'); key(151, 860, 400, 720, 'T')
# "I can't stop the feeling, so just dance, dance, dance"
key(152.5, 760, 400, 720, 'disL'); key(154, 560, 400, 720, 'disR'); key(155.5, 640, 470, 720, 'low'); key(157, 600, 400, 720, 'pL'); key(158, 680, 400, 720, 'pR'); key(159, 640, 400, 720, 'up')
dancer(112, 159.6, K, mv=0.33)
dance3(K, [117, 118, 119, 133, 134, 135, 141, 142, 143, 157, 158, 159])
at(120.5, 1.0, 'F12creep', kb=[120.5, 122.5, 126.5, 128.5], kd=[0, 130, 130, 0], to=128.6)
# "so keep dancing": one more ring from its hips in the long turn
x, y = hips(K, 149); at(149, 1.0, 'F12ring', x=x, y=y, r0=40, r1=1000, w=24, gaps=[120, 300], gw=30, dur=2.6, ezskip=True)

# 1:25 "ooh, it's something magical, it's in the air, it's in my blood, it's rushing on": quiet. Rivers of light rush up the screen;
# a gold bubble floats along each one: get into the bubble to let the river pass over you
stage(159.5, 192.5, 7)
fx(160, 0.2, 4, 1.0)
for j, b in enumerate(range(161, 189, 4)):
    fast = b >= 184
    at(b, 1.5, 'F12river', y0=H + 50, vy=-180 if fast else -135, A=[60, 90, 70, 100, 80, 90, 90][j], lam=[900, 760, 1000, 820, 940, 700, 860][j], ph=j * 1.3,
       th=42, bx=[300, 980, 640, 200, 1080, 420, 860][j], bv=[110, -110, 125, -100, 125, -125, 135][j], br=84, fl=-300 if j % 2 else 300)

# 1:42 "'cause I got that sunshine in my pocket": the sun rises, its rays turn like a fan
stage(191.5, 256.5, 8)
fx(192, 0.3, 6, 1.4)
SB = [192, 196, 204, 212, 220, 224]; SX = [640, 640, 470, 810, 640, 640]; SY = [860, 380, 360, 360, 360, 360]
RB = [192, 196, 200, 200.6, 204, 208, 208.6, 212, 216, 216.6, 220, 223.5]
RA = [0, 30, 70, 70, 40, 0, 0, 50, 95, 95, 65, 65]
at(196, 2.0, 'F12sun', kb=SB, kx=SX, ky=SY, rb=RB, ra=RA, n=6, hw=5, rd=70, len=1500, to=223.6)
# "got that good soul in my feet": the soles come back, walking under the sun
walker(201, 208, 160, 600, 1120, 600, 60, 0, 8, hold=1.3, ez_every=2)
# "I feel that hot blood in my body when it drops": the sun drips: hot drops fall from its disc to both sides
def sunpos(b):
    import bisect
    if b <= SB[0]: return SX[0], SY[0]
    for i in range(len(SB) - 1):
        if SB[i] <= b <= SB[i + 1]:
            u = (b - SB[i]) / (SB[i + 1] - SB[i]); u = u * u * (3 - 2 * u); return SX[i] + (SX[i + 1] - SX[i]) * u, SY[i] + (SY[i + 1] - SY[i]) * u
    return SX[-1], SY[-1]
for j, b in enumerate([209, 210, 211, 212, 213, 214, 215]):
    x0, y0 = sunpos(b)
    for s in (-1, 1):
        an = math.radians(90 + s * (25 + 12 * (j % 3)))
        at(b, 0.9, 'F12proj', sh=2, x0=x0, y0=y0, x1=x0 + math.cos(an) * 900, y1=y0 + math.sin(an) * 900, r=19, dur=1.4, sd=400 + j * 2 + (s > 0), ezskip=s > 0 and j % 2 == 1)
# 1:59 "under the lights" again: a ring of spotlights closes round you; the sun keeps turning inside it
ZB = [224, 228, 232, 236, 240, 244, 248, 252, 254.5]
ZX = [640, 640, 500, 780, 640, 640, 640, 640, 640]
ZY = [360, 360, 360, 360, 320, 360, 360, 360, 360]
ZR = [880, 400, 385, 385, 375, 375, 355, 355, 900]
at(224, 1.5, 'F12zone', kb=ZB, kx=ZX, ky=ZY, kr=ZR, to=254.6)
at(226, 1.5, 'F12sun', kb=ZB[:6], kx=ZX[:6], ky=ZY[:6], rb=[226, 230, 234, 238, 242, 243.5], ra=[30, 75, 120, 75, 30, 30], n=4, hw=5, rd=56, len=900, to=243.6)
# "just imagine, just imagine, just imagine": inside the ring, half of it burns, then another half, then the middle
def half(r, a0):
    return [c for i in range(13) for c in (r * math.cos(math.radians(a0 + 180 * i / 12)), r * math.sin(math.radians(a0 + 180 * i / 12)))]
at(246, 1.5, 'F12shape', x=640, y=360, pts=half(360, 0), rot=0, hold=0.9)
at(248, 1.5, 'F12shape', x=640, y=360, pts=half(360, 180), rot=0, hold=0.9)
at(250, 1.5, 'F12shape', x=640, y=360, pts=ngon(150, 10), rot=0, hold=0.9)
# "nothing I can see but you": four lights on you, then the ring opens
for j, lamp in enumerate([(0, 0), (W, 0), (0, H), (W, H)]):
    beam(251.5, lamp, [251.5, 252.2, 253.2], [0, 0, 0], 253.5, ha=5, wn=1.5, aim=True, rnd=True, ezskip=j % 2 == 1)

# ================= part 3: can't stop the feeling (256-354) =================
# The dancer again, bigger and quicker, on a floor that turns; then the endless line of light runs round a figure 8 and never stops.
FB = [256, 288, 320]; FA = [0, 120, 240]
stage(255.5, 320.5, 9, kb=FB, ka=FA)
fx(256, 0.35, 12, 1.5)
S3 = 1.2
K = []
def key3(b, x, y, rot, name, s=S3): K.append((b, x, y, rot, s, name))
# "nothing I can see but you when you dance, dance, dance"
key3(256, 640, 390, 0, 'T'); key3(257.5, 640, 390, 45, 'T'); key3(259, 640, 390, 90, 'Y'); key3(260, 600, 390, 90, 'disR'); key3(261, 680, 390, 90, 'disL')
key3(262, 640, 390, 90, 'pL'); key3(262.75, 640, 390, 90, 'pR'); key3(263.5, 640, 390, 90, 'up')
# "feel a good, good creepin' up on you": it slides round you on the turning floor
key3(265, 470, 330, 135, 'T'); key3(266.5, 470, 330, 180, 'kR'); key3(268, 810, 430, 225, 'T'); key3(269.5, 810, 430, 270, 'kL'); key3(271, 640, 390, 360, 'T')
dancer(256, 271.6, K, mv=0.24)
dance3(K, [261, 262, 263], gw=32)
K = []
# "so just dance, dance, dance": the endless line comes in, running round the 8
at(272, 1.5, 'F12snake', cx=640, cy=360, ax=580, ay=270, rb=[272, 288, 304, 320], ra=[0, 30, 90, 120], s0=0, om=1.95, len=3.2, nseg=4, gap=0.31, w=26, to=319.7)
# "dance, dance, dance": three rings from the middle of the 8, their gaps turning
for j, b in enumerate([277, 278, 279, 317, 318, 319]):
    at(b, 1.0, 'F12ring', x=640, y=360, r0=30, r1=900, w=26, gaps=[(j % 3) * 60 + 20, (j % 3) * 60 + 200], gw=28, dur=2.4, ezskip=j % 3 == 1)
# the dancer, smaller, dances in the middle of the 8 (its loops go round it)
key3(280, 640, 390, 0, 'idle', 1.0); key3(282, 640, 390, 0, 'T', 1.0); key3(284, 640, 390, 90, 'T', 1.0); key3(285, 640, 390, 90, 'pL', 1.0); key3(286, 640, 390, 90, 'pR', 1.0); key3(287, 640, 390, 90, 'up', 1.0)
key3(289, 640, 390, 180, 'T', 1.05); key3(291, 640, 390, 270, 'Y', 1.05); key3(293, 640, 390, 360, 'T', 1.05); key3(294, 640, 410, 360, 'kR', 1.05); key3(295, 640, 410, 360, 'kL', 0.95)
key3(297, 640, 390, 450, 'T', 1.0); key3(299, 640, 390, 540, 'disR', 1.0); key3(301, 640, 390, 630, 'T', 1.0); key3(302, 640, 390, 630, 'pL', 1.0); key3(303, 640, 390, 630, 'pR', 1.0)
dancer(280, 303.6, K, mv=0.32)
dance3(K, [285, 286, 287, 301, 302, 303], gw=28)
K = []
# "I can't stop the feeling, so just dance, dance, dance": a second line runs the other way round
at(304, 1.5, 'F12snake', cx=640, cy=360, ax=620, ay=300, rb=[304, 320], ra=[45, 0], s0=math.pi, om=-1.7, len=2.6, nseg=3, gap=0.32, w=24, to=319.7, ezskip=True)
# the breakdown, 2:50 "I can't stop the, I can't stop the feeling... got this feeling in my body": the x-ray skeleton from part 1 dances
# and throws its bones like boomerangs at you; they come back to its hands
stage(319.5, 356, 10)
fx(320, 0.3, 10, 1.4)
key3(321, 640, 380, 0, 'idle', 1.0); key3(323, 640, 380, 0, 'T', 1.0); key3(325, 640, 380, 0, 'Y', 1.0); key3(327, 640, 380, 0, 'disR', 1.0)
key3(329, 640, 380, 0, 'disL', 1.0); key3(331, 640, 380, 0, 'T', 1.0); key3(333, 640, 380, 90, 'T', 1.0); key3(335, 640, 380, 180, 'T', 1.0)
key3(337, 520, 380, 180, 'kR', 1.0); key3(339, 760, 380, 180, 'kL', 1.0); key3(341, 640, 380, 180, 'split', 1.0); key3(343, 640, 380, 180, 'up', 1.0)
key3(345, 640, 380, 270, 'T', 1.05); key3(347, 640, 380, 360, 'T', 1.05); key3(348.5, 640, 380, 540, 'T', 1.1); key3(350, 640, 380, 720, 'T', 1.1); key3(351.5, 640, 380, 720, 'disR', 1.1)
dancer(320.5, 352.0, K, style=1, mv=0.3, lw=34)
# the bones: one a bar at you, then two at once in a fan (and one at you between them)
for j, b in enumerate([338, 342, 346]):
    x0 = 640; at(b, 1.0, 'F12proj', sh=1, x0=x0, y0=260, x1=x0 + 1, y1=260, aim=True, len=760, back=True, r=36, spin=2.2, dur=1.1, sd=700 + j, rnd=True, ezskip=j == 1)
for j, b in enumerate([322, 324, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335]):
    x0, y0 = (420 if j % 2 == 0 else 860), 230
    at(b, 1.0, 'F12proj', sh=1, x0=x0, y0=y0, x1=x0 + 1, y1=y0, aim=True, len=760, back=True, r=36, spin=2.2, dur=1.2, sd=500 + j, rnd=True)
for j, b in enumerate(range(336, 348, 2)):
    for s in (-1, 1):
        x0 = 640 + s * 220; an = math.radians(90 + s * (30 + 18 * (j % 3)) + (180 if j % 2 else 0))
        at(b, 1.0, 'F12proj', sh=1, x0=x0, y0=330, x1=x0 + math.cos(an) * 560, y1=330 + math.sin(an) * 560, back=True, r=36, spin=2.4 * s, dur=1.1, sd=600 + j * 2 + (s > 0),
           ezskip=s > 0 and j % 2 == 1)
# 3:07 the hush: everything stops; 3:08 the gold light
ev(354, 'F12end')

d = {
    'id': 'F12', 'name': "Can't Stop The Feeling", 'by': 'Coulexrl', 'ver': 'Mohammed',
    'file': 'SONG_F12', 'bpm': BPM, 'off': OFF, 'end': 354, 'cps': [0, 112, 256],
    'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3},
    'energy': json.load(open(os.path.join(HERE, 'energy.json'))) if os.path.exists(os.path.join(HERE, 'energy.json')) else None,
    'events': sorted(EV, key=lambda e: e['b']),
}
if d['energy'] is None: del d['energy']
open(OUT, 'w').write(json.dumps(d, ensure_ascii=False, separators=(',', ':')))
print(len(EV), 'events')
