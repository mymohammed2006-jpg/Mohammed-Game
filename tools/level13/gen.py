#!/usr/bin/env python3
# Writes data/dodge_L13.json: main level 13 "A beat further" (Neon Beats Music), the whole song.
# The kinds are code in src/Game/Dodge/L13.cs. Beats: 140 bpm, beat 0 = 0.265 s (B(b) = 0.265 + b*60/140).
# intro 0-16 | busy 16-128 | break 128-144 (0:55) | calmer 144-192 | build-up 192-216 (1:22.5) | stop 216-224 (1:32.8)
# drop 224-280 (1:36.3) | fill 280-288 (2:00) | last part 288-344 | end 344-352, win 352 (2:31.1). Checkpoints 128, 224.
# usage: python3 tools/level13/gen.py   (from the godot-port folder)
import json, math, os
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
W, H, TAU = 1280, 720, math.pi * 2
WN = [1.8, 1.7, 1.6]            # warning (beats) of each part, as in L13T(); Easy makes it 1.3x longer
EZWN = 1.3

def mulberry32(seed):
    a = [seed & 0xFFFFFFFF]
    def f():
        a[0] = (a[0] + 0x6D2B79F5) & 0xFFFFFFFF; t = a[0]
        t = ((t ^ (t >> 15)) * (1 | t)) & 0xFFFFFFFF
        t = ((t + (((t ^ (t >> 7)) * (61 | t)) & 0xFFFFFFFF)) & 0xFFFFFFFF) ^ t
        return ((t ^ (t >> 14)) & 0xFFFFFFFF) / 4294967296.0
    return f
R = mulberry32(1313)
E = []
def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **f):
    e = {"b": r4(float(b)), "k": k}
    for key, v in f.items():
        if isinstance(v, bool): 
            if v: e[key] = True
        else: e[key] = r4(v)
    E.append(e)
def lead(T, extra=0): return WN[T] * EZWN + extra
# warned kinds fire early enough for Easy's longer warning; "r" marks the ones that depend on the run (aimed at the player)
def light(h, x, y, r, T, dur=0.7): ev(h - lead(T), "L13light", h=h, x=x, y=y, r=r, T=T, dur=dur)
def lightP(h, r, T, dur=0.7): ev(h - lead(T), "L13lightP", h=h, r=r, T=T, dur=dur, rnd=True)
def flame(h, x, w, T, bot=False, dur=0.8): ev(h - lead(T), "L13flame", h=h, x=x, w=w, T=T, bot=bot, dur=dur)
def flameP(h, w, T, bot=False, dur=0.8): ev(h - lead(T), "L13flameP", h=h, w=w, T=T, bot=bot, dur=dur, rnd=True)
def sparks(b, x, y, T, n=2): ev(b, "L13sparks", x=x, y=y, T=T, n=n)
def arm(h, left, y, hold, T): ev(h - lead(T), "L13arm", h=h, left=left, y=y, hold=hold, T=T)
def vent(h, c, vert, gap, T): ev(h - lead(T), "L13vent", h=h, c=c, vert=vert, gap=gap, T=T)
def stage(b0, h, x0, x1, y1, n, T): ev(b0, "L13stage", h=h, x0=x0, x1=x1, y1=y1, n=n, T=T, r0=R() * TAU)
def hit(b, fl, sh): ev(b, "L13hit", fl=fl, sh=sh)

# ---------- part 1: the launch (0:00 - 0:55) ----------
T = 0
ev(0, "L13pad", keep=True, lift=80, lift2=90, end=129)
for i in range(4):   # the countdown: pad lights in pairs closing in
    light(4 + i, 170 + i * 135, H - 120, 58, T); light(4 + i, W - 170 - i * 135, H - 120, 58, T)
for b in range(8, 16):   # rows of lights on the beat, a light on you on the off-beat
    y = 140 if b % 2 == 0 else H - 140; o = 0 if b % 4 < 2 else 128
    for j in range(5): light(b, 128 + 256 * j + o - 64, y, 40, T)
    if b >= 12: lightP(b + 0.5, 46, T)
hit(16, 0.35, 12)   # 0:07 ignition: the flames march across, sparks bounce off the ground
for b in range(16, 32):
    i = b - 16 if b < 24 else 31 - b; x = 80 + i * 160; flame(b, x, 120, T)
    if b % 2 == 1: sparks(b + 0.5, x, H - 12, T)
    if b >= 24 and b % 4 == 2: flameP(b + 0.5, 140, T, dur=0.6)
for k in range(16):   # flames in pairs from the edges to the middle and back
    b = 32 + k; i = k % 4 if k % 8 < 4 else 3 - k % 4
    flame(b, 100 + i * 150, 110, T); flame(b, W - 100 - i * 150, 110, T)
    if k % 4 == 2: lightP(b + 0.5, 52, T)
    if k % 4 == 0: sparks(b + 0.5, W / 2, H - 12, T, 4)
ys = [130, 360, 590]   # 0:20 the tower arms swing out; their tips clamp a beat later
for k in range(16):
    b = 48 + k; left = k % 2 == 0; y = ys[(k // 2 + (0 if left else 1)) % 3]
    if k % 4 < 2 or (k >= 8 and k % 4 == 2): arm(b, left, y, 1.2, T)
    else: light(b, 780 if left else W - 780, y, 44, T, 0.5)
for k in range(8):   # 0:27 the fuel pipes: steam from every vent but two
    b = 64 + 2 * k
    if k % 2 == 0: vent(b, 210 if k % 4 == 0 else H - 210, False, 1 + int(R() * 13), T)
    else: vent(b, 340 if k % 4 == 1 else W - 340, True, 1 + int(R() * 6), T)
    if k % 2 == 1: lightP(b + 1, 50, T, 0.5)
    if k >= 4: flame(b + 1, 120 if k % 2 == 0 else W - 120, 130, T, bot=(k % 4 >= 2), dur=0.6)
hit(80, 0.4, 16)   # 0:34 lift-off: the pad sinks away, the empty stages fall and break
for k in range(4):
    b = 80 + 4 * k; x1 = 330 + R() * 120 if k % 2 == 0 else W - 330 - R() * 120; y1 = 250 + R() * 220
    stage(b, b + 3, x1 + (-260 if k % 2 == 0 else 260), x1, y1, 8, T)
    vent(b + 1, W - 280 if k % 2 == 0 else 280, True, 1 + int(R() * 6), T); lightP(b + 2.5, 50, T)
for k in range(4):
    b = 96 + 4 * k
    for s in range(2):
        x1 = 280 + R() * 200 if s == 0 else W - 280 - R() * 200; y1 = 220 + R() * 280
        stage(b + s, b + 3 + s * 0.5, x1 + 300 if s == 0 else x1 - 300, x1, y1, 7, T)
    lightP(b + 1, 48, T); lightP(b + 2, 48, T)
for k in range(8):   # 0:48 max Q: flames from the top and the bottom at once
    b = 112 + k; i = k % 4; flame(b, 140 + i * 330, 130, T); flame(b, W - 140 - i * 330, 130, T, bot=True)
    if k % 2 == 1: lightP(b + 0.5, 50, T, 0.5)
for (x1, y1) in [(W * 0.22, H * 0.55), (W * 0.5, H * 0.35), (W * 0.78, H * 0.55)]: stage(120, 124, x1, x1, y1, 7, T)
hit(124, 0.3, 14); lightP(125, 56, T); lightP(126, 56, T)

# ---------- part 2: the asteroid belt (0:55 - 1:36) ----------
T = 1
ev(128, "L13earth", keep=True, end=226)
for (b0, b1, x0, y0, x1, y1, w) in [(128, 146, -170, 190, W + 170, 300, 0.9), (130, 148, W + 170, 560, -170, 440, -0.8), (134, 150, W * 0.55, -170, W * 0.45, H + 170, 1.1)]:
    # satellites float through the break. The first one is out before the 0:55 save's restart point (2 beats before it): keepNew
    # brings it back after a death there (v51, his ask 10-26: it vanished)
    ev(b0 - 3, "L13sat", h=b0, end=b1, x0=x0, y0=y0, x1=x1, y1=y1, w=w, keepNew=b0 == 128)
an = 0.3
for b in [128.5, 133, 134, 134.5, 135, 139, 139.5, 141, 142, 143]:   # beacon lights on the notes
    light(b, W / 2 + math.cos(an) * 270, H / 2 + math.sin(an) * 210, 50, T, 0.5); an += 2.4
for k in range(16):   # 1:02 asteroids that split in two, then in two again
    b = 144 + 2 * k; left = k % 2 == 0; vy = (R() * 2 - 1) * 60; sd = int(R() * 1e6)
    if k % 2 == 0: y = 140 + R() * 440; ev(b - lead(T), "L13rock", h=b, T=T, left=left, y=y, vy=vy, r=58, seed=sd)
    else: ev(b - lead(T), "L13rockP", h=b, T=T, left=left, vy=vy, r=54, seed=sd, rnd=True)
for k, d in enumerate(["R", "D", "L", "U"]):   # walls of solar panels with one panel missing
    b = 149 + 8 * k; nT = 6 if d in "LR" else 8; ev(b - lead(T, 1), "L13panel", h=b, T=T, dir=d, nT=nT, gap=int(R() * nT))
for k in range(8):   # 1:15 comets on curves
    b = 176 + 2 * k; fl = k % 2 == 0; ya = 80 + R() * 200; yb = H - 80 - R() * 200; xc = W * (0.3 + R() * 0.4); yc = H + 120 if k % 4 < 2 else -120
    ev(b - lead(T, 1), "L13comet", h=b, T=T, x0=-60 if fl else W + 60, x1=W + 60 if fl else -60, ya=ya, yb=yb, xc=xc, yc=yc)
    if k % 4 == 1:
        sd = int(R() * 1e6); ev(b + 1 - lead(T), "L13rockP", h=b + 1, T=T, left=(k // 2) % 2 == 0, vy=0, r=54, vk=0.9, s2=5, sep=90, seed=sd, rnd=True)
dx, dy = -0.5 / math.sqrt(1.25), 1 / math.sqrt(1.25)   # 1:22 the meteor shower, thicker and faster; a lane of sky stays clear and moves
b = 192.0
while b < 216 - 1e-6:
    per = 3 if b < 200 else 4 if b < 208 else 5 if b < 212 else 7; q = round((b - 192) * 4)
    skip = (per == 3 and q % 4 == 3) or (per == 5 and q % 8 == 7)
    if not skip:
        spk = 1.1 + 0.4 * (b - 192) / 24; lane = 690 + 380 * math.sin((b - 192) * 0.26)
        u = R(); x = u * (W + 520) - 60
        if abs(x - lane) < 150: x = lane + (-150 - u * 120 if x < lane else 150 + u * 120)
        x0, y0 = x, -20
        if x0 > W + 20: y0 = (x0 - W - 20) * 2; x0 = W + 20
        ev(b - lead(T), "L13meteor", h=b, T=T, x0=x0, y0=y0, dx=dx, dy=dy, spk=spk, tf=216, tx=222)
    b += 0.25
def wall(h, end, side, d, amp=20, wv=1.6): ev(h - lead(T), "L13wall", h=h, end=end, T=T, side=side, d=d, amp=amp, wv=wv)
wall(196, 216, "D", 64, 18, 1.2)   # the air below starts to burn as the rocket dives through the shower
hit(216, 0.3, 6); ev(218, "L13charge", end=224)   # 1:32 everything freezes; the warp charges up

# ---------- part 3: light speed and the black hole (1:36 - 2:31) ----------
T = 2
def warp(b, n, a0, g0, gn, v0=80, ak=1.0): ev(b - lead(T), "L13warp", h=b, T=T, n=n, a0=a0, g0=g0, gn=gn, v0=v0, ak=ak)   # the path shows a warning ahead
def frame(h, side): ev(h - 1, "L13frame", h=h, T=T, side=side)
hit(224, 0.5, 16); ev(224, "L13warpbg", keep=True, end=283)
for k in range(8): warp(224 + k, 14, 0.1, k, 2); warp(224 + k + 0.5, 5, 0.3 + k * 0.7, 0, 0, 160, 1.3)   # rings, the gap turning
for k in range(16): warp(232 + k * 0.5, 3, k * 0.5, 0, 0, 120, 1.1)   # three-armed spiral
warp(232, 14, 0, 2, 2); warp(236, 14, 0.5, 9, 2)
for k in range(8):   # tunnel frames
    if k % 2 == 0: frame(240 + k, (k // 2) % 4)
    else: warp(240 + k, 14, 0.2 * k, int(R() * 14), 3)
for k in range(8):   # the vanishing point swings left and right
    warp(248 + k, 15, 0.13 * k, int(R() * 15), 2)
    if k % 2 == 0: warp(248 + k + 0.5, 6, 0.5 * k, 0, 0, 160, 1.3)
for k in range(16): b = 256 + k * 0.5; warp(b, 3, k * 0.42, 0, 0, 120, 1.1) if k % 2 == 0 else warp(b, 3, -k * 0.42 + 0.5, 0, 0, 120, 1.1)   # two spirals
warp(260, 15, 0.2, 5, 2)
for k in range(8):   # frames and rays aimed at you
    if k % 4 == 0: frame(264 + k, (k // 4 * 3 + 1) % 4)
    elif k % 2 == 1: ev(264 + k - lead(T), "L13bundle", h=264 + k, T=T, rnd=True)
    else: warp(264 + k, 12, 0.3 * k, int(R() * 12), 2)
for (h, e, sides, d) in [(228, 236, "UD", 78), (240, 248, "LR", 120), (252, 260, "UD", 84), (264, 272, "LR", 110), (273, 280, "UD", 70)]:   # the tunnel walls close in, top and bottom, then the sides
    for sd in sides: wall(h, e, sd, d, 24, 2.2 if sd in "UL" else -2.2)
for k in range(8):   # everything at once
    b = 272 + k
    if k % 4 == 0: frame(b, (k // 4 + 2) % 4)
    else: warp(b, 16, 0.1 * k, int(R() * 16), 2)
    warp(b + 0.5, 3, k * 0.6 * (1 if k % 2 == 0 else -1), 0, 0, 120, 1.1)
hit(280, 0.5, 14)
# 2:00 out of the warp: the black hole (arms from 290, falls in from 344, explodes 351)
ev(281, "L13hole", arms=290, fall=344, end=351)
def pull(h, gap, gs=0.9): ev(h - lead(T), "L13pull", h=h, T=T, gap=gap, gs=gs)
def debris(h, a0, w): ev(h - lead(T), "L13debris", h=h, T=T, a0=a0, w=w)
pull(285, 0.5)
for k in range(8):
    b = 288 + 2 * k
    if k % 4 == 0: pull(b, 0.4 + k * 1.3, 1.2)
    debris(b + 1, R() * TAU, 0.5)
for k in range(16):
    b = 304 + k
    if k % 8 == 0: pull(b, R() * TAU, 1.2)
    if k % 2 == 0: debris(b, R() * TAU, 0.6 if k % 4 == 0 else -0.6)
for k in range(4):   # the polar jets while the hole circles
    b = 320 + 4 * k; a0 = R() * math.pi; ev(b - lead(T), "L13jet", h=b, T=T, a0=a0, w=0.55 if k % 2 == 0 else -0.55)
    if k % 2 == 1: pull(b + 2, R() * TAU, 1.2)
    debris(b + 1, R() * TAU, 0.5); debris(b + 3, R() * TAU, -0.5)
for k in range(8):
    b = 336 + k
    if k % 4 == 0: pull(b, R() * TAU, 1.1)
    debris(b + 0.5, R() * TAU, 0.6)
    if k in (0, 4): a0 = R() * math.pi; ev(b - lead(T), "L13jet", h=b, T=T, a0=a0, w=0.7)

SKIP = set(os.environ.get('SKIP', '').split(','))
E = [e for e in E if e['k'] not in SKIP]
E.sort(key=lambda e: e["b"])   # stable: same-beat events keep their order
energy = json.load(open(os.path.join(HERE, "energy.json")))
doc = {"id": "L13", "name": "المرحلة الثالثة عشرة", "song": "A beat further", "file": "SONG_L13", "bpm": 140, "off": 0.265, "end": 352, "cps": [0, 128, 224],
       "ez": {"ezg": 1, "ezc": 1, "ez": 0.42}, "energy": energy, "events": E}
out = os.path.join(ROOT, "data", "dodge_L13.json")
json.dump(doc, open(out, "w"), ensure_ascii=False, separators=(",", ":"))
print("events", len(E), "->", out)
