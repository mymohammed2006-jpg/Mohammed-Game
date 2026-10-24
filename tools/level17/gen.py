#!/usr/bin/env python3
# Makes data/dodge_L17.json: L17 "حالف لا ما انام" (Jafar Al-Ghazal and Mohammed Jamal), the night that won't end.
# The level is the song from 0:24 to 2:44 (songs/SONG_L17.mp3 is cut from 0:23.77). 76 BPM, beat 0 = 0.205 s in the cut file, a beat = 0.7895 s.
# Each beat has hits at +0, +0.42 and +0.74 (the strongest at +0 and +0.74), and a softer one at +0.25.
# Song map (beats): verse 0-64 (0:24) | 64-96 (1:14.5) | the quiet dip 96-98 (1:39.8) | 98-120 (1:41.4) | 120-177 (1:58.8) | win 177.42 (2:44.0)
# Parts: 1 the clock (0-64), 2 the eyelids (64-120), 3 till dawn (120-177.42). Checkpoints 0, 64, 120.
# Nothing here is random: every attack is a fixed pattern on the beat (only the eye's stare aims at the square: "rnd").
# Times are beats; an event fires at its start minus EZW times its warning (Easy shows the warning longer).
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L17.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
END = 177.42
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW - 0.05, k, hit=hit, wn=wn, **kw)
def fx(b, v=0.3, s=8, dur=1.2): ev(b, 'L17fx', v=v, s=s, dur=dur)

ev(0, 'L17stage', to=END + 1, keep=True)

# ================= part 1: the clock (0-64) =================
fx(0, 0.2, 6, 1.0)
# 0:24 the minute hand ticks round on every beat and the wedge it jumps across burns: keep behind it. From 0:31 the hour hand ticks
# the other way every two beats: when they meet, slip through a hand's white window to the other side
ev(0.2, 'L17hand', hit=1.5, wn=0.6, a0=-90, st=30, tk=list(range(2, 18)), len=900, w=22, hc=200, hw=34, burn=True, to=18)
ev(7.2, 'L17hand', hit=8.5, wn=0.6, a0=90, st=-30, tk=list(range(9, 18, 2)), len=900, w=28, hc=300, hw=34, burn=True, to=18)

# 0:38 the numerals throw daggers through the middle one after another on the strong hits, round the clock; from 0:44 the other way
# round (and the minute hand ticks again, no wedge); from 0:48 two at once
fb, fk = [], []
k = 0
for b in range(18, 26):
    for o in (0, 0.74): fb.append(b + o); fk.append(k % 12); k += 1
k = 4
for b in range(26, 30):
    for o in (0, 0.74): fb.append(b + o); fk.append(k % 12); k -= 1
for b in range(30, 34):
    fb += [b, b]; fk += [k % 12, (k + 4) % 12]; k -= 1
    fb.append(b + 0.74); fk.append(k % 12); k -= 1
at(18, 0.9, 'L17chime', R=262, fb=fb, fk=fk, sp=1500, dl=96, dw=34, to=34)
ev(24.5, 'L17hand', hit=25.8, wn=0.6, a0=0, st=30, tk=list(range(26, 34)), len=900, w=22, hc=330, hw=36, burn=False, to=34)

# 0:51 the gears: the inner gear and the housing turn opposite ways; the inner teeth bite out, then the outer teeth bite in.
# Be in a gap of the teeth that bite
ib = list(range(36, 50, 2))
ob = list(range(37, 50, 2))
ev(32.4, 'L17gears', hit=34, wn=0.55, ri=96, ro=330, ni=8, no=12, hwi=8, hwo=6, wi=0.26, wo=-0.2, a0i=0, a0o=15,
   restI=26, restO=26, punI=150, punO=150, ib=ib, ob=ob, to=50)

# 1:03 the alarm clock rings: rings of sound with one gap leave it; the two hits of a pair have their gaps close together.
# From 1:10 the minute hand ticks again (with its wedge) while the alarm rings once a beat
rb, ga, ez = [], [], []
for j, b in enumerate(range(50, 57)):
    base = 90 + 55 * j
    rb += [b + 0.74, b + 1]; ga += [base, base + 14]; ez += [0, 1]
for j, b in enumerate(range(58, 64)):
    rb.append(b + 0.42); ga.append(200 + 47 * j); ez.append(0)
ev(48.8, 'L17alarm', hit=50, wn=0.8, kb=[49, 53, 57, 60, 64], kx=[640, 420, 860, 640, 640], ky=[360, 250, 470, 360, 360],
   rb=rb, ga=ga, ez=ez, gw=66, sp=410, th=18, to=64)
ev(56.6, 'L17hand', hit=57.8, wn=0.6, a0=180, st=-30, tk=list(range(58, 64)), len=900, w=22, hc=150, hw=36, burn=True, to=64)

# ================= part 2: the eyelids (64-120) =================
fx(64, 0.12, 8, 1.2)
# 1:14 the blink: the lids close and leave one slit; be in it. From 1:20 the slit slides while the lids hold (follow it)
def blink(tb, x, y, sw, sh, dx=0, hold=0.5, wn=1.4):
    at(tb, wn, 'L17blink', x=x, y=y, sw=sw, sh=sh, dx=dx, cl=0.35, hold=hold, op=0.2)
blink(66, 640, 360, 260, 64, wn=1.9); blink(68, 420, 250, 230, 56); blink(70, 640, 480, 230, 56); blink(72, 860, 330, 220, 54)
blink(74, 1000, 520, 220, 54, -260, 1.1); blink(76.5, 640, 340, 210, 52, -300, 1.1); blink(79, 260, 230, 210, 52, 0, 0.5)

# 1:17 the window blinds: rows of slats turn shut. Odd and even rows in turn, then all but two rows every two beats; from 1:24 the
# columns too, a moment after the rows: be where an open row and an open column cross
ROWS, COLS = 9, 16
fb, fs, fz = [], [], []
for b in range(80, 84):
    for s in range(ROWS):
        if s % 2 == b % 2: fb.append(b); fs.append(s); fz.append(0)
OPEN = [(84, (3, 4)), (86, (6, 7)), (88, (3, 4)), (90, (1, 2)), (92, (4, 5)), (94, (6, 7))]
for b, op in OPEN:
    for s in range(ROWS):
        if s not in op: fb.append(b); fs.append(s); fz.append(0)
at(80, 0.7, 'L17blinds', vert=False, n=ROWS, fb=fb, fs=fs, ez=fz, hold=0.35, fl=0.12, to=95.2)
fb, fs, fz = [], [], []
OPC = [(88.42, (9, 10, 11, 12)), (90.42, (3, 4, 5, 6)), (92.42, (10, 11, 12, 13)), (94.42, (5, 6, 7, 8))]
for j, (b, op) in enumerate(OPC):
    for s in range(COLS):
        if s not in op: fb.append(b); fs.append(s); fz.append(j % 2)
at(88.42, 1.6, 'L17blinds', vert=True, n=COLS, fb=fb, fs=fs, ez=fz, hold=0.3, fl=0.12, to=95.2)

# 1:39 the doze: the music drops, all goes dark but a circle of light. Stay in it; at 1:41 the eye snaps open and the lashes fly out
at(96, 1.5, 'L17doze', kb=[96, 96.75, 97.5, 98], kx=[640, 540, 720, 640], ky=[360, 300, 400, 360], kr=[150, 130, 115, 110], to=98)
fx(98, 0.15, 10, 1.0)

# 1:41 the eye's stare: three beams on the three hits of a beat, each where the square was half a beat before. Keep moving.
# The eye moves to the left (1:47), then to the right (1:50), where it fires once in the gaps between the blinks
ev(97.2, 'L17stare', hit=98, wn=0.9, x=640, y=64, fb=[99, 99.42, 99.74, 101, 101.42, 101.74, 103, 103.42, 103.74], ez=[0, 1, 0, 0, 1, 0, 0, 1, 0], lk=0.5, bw=40, hold=0.22, to=104.4, rnd=True)
ev(102.6, 'L17stare', hit=104, wn=0.9, x=64, y=360, fb=[105, 105.42, 105.74, 107, 107.42, 107.74], ez=[0, 1, 0, 0, 1, 0], lk=0.5, bw=40, hold=0.22, to=108.4, rnd=True)
ev(106.4, 'L17stare', hit=108, wn=0.9, x=1216, y=360, fb=[109, 109.42, 111.42, 113.42, 115.42, 117.42], ez=[0, 1, 0, 0, 0, 0], lk=0.5, bw=40, hold=0.22, to=118.2, rnd=True)
blink(110, 500, 300, 200, 50, 200, 0.8); blink(112, 760, 450, 200, 50, -200, 0.8); blink(114, 480, 330, 190, 50, 200, 0.8)
blink(116, 760, 460, 190, 48, -180, 0.8); blink(118, 620, 330, 190, 48, 0, 0.5)

# ================= part 3: till dawn (120-177.42) =================
fx(120, 0.12, 8, 1.2)
# 1:58 the star trails: rings of stars close in on the moon, each turning the other way from the one before, with one gap.
sb, ga, w = [], [], []
a = 200
times = [117.5 + i for i in range(18)]
for i, b in enumerate(times):
    sb.append(b); a = (a + (70 if i % 2 == 0 else -110)) % 360; ga.append(a); w.append(0.35 if i % 2 == 0 else -0.35)
ev(117.4, 'L17trails', sb=sb, ga=ga, w=w, r0=760, v=170, gw=68, th=18, to=136)

# 2:11 the messages: bubbles slide in from both sides in lanes ("typing..." shows where), three to a beat on its hits, the lanes
# stepping down and up the screen. From 2:18 two at a time
fb, fl, fs, fL, fz = [], [], [], [], []
for j, b in enumerate(range(135, 152)):
    base = (j * 3) % 6 if j % 4 < 2 else (5 - (j * 3) % 6)
    for q, o in enumerate((0, 0.42, 0.74)):
        lane = (base + (q if j % 2 == 0 else -q)) % 6
        fb.append(b + o); fl.append(lane); fs.append((j + q) % 2); fL.append(380 + 60 * ((j + q) % 3)); fz.append(0)
        if b >= 143 and q != 1:
            fb.append(b + o); fl.append((lane + 3) % 6); fs.append(1 - (j + q) % 2); fL.append(340); fz.append(1)
order = sorted(range(len(fb)), key=lambda i: fb[i])
at(135, 1.0, 'L17msgs', n=6, bh=92, fb=[fb[i] for i in order], fl=[fl[i] for i in order], fs=[fs[i] for i in order], fL=[fL[i] for i in order],
   ez=[fz[i] for i in order], sp=1050, to=152.5)

# 2:12 .. 2:30 time racing: the hands come back and spin, the long one turning back on the strong beats (the arrows flash a beat
# before). Slip through a window to cross
SB = [152, 155, 157, 159.74, 161.74, 163, 164.74, 166]
ev(150.6, 'L17race', hit=152, wn=1.0, len=[900, 330], wd=[22, 34], hc=[230, 140], hw=[38, 36], a0=[-90, 120], sb=SB,
   sw0=[0.85, -0.9, 0.95, -1.0, 1.0, -1.05, 1.1, -1.1], sw1=[-0.32, 0.32, -0.36, 0.36, -0.4, 0.4, -0.42, 0.42], to=167.5)

# 2:31 the dawn: the horizon rises and burns under it; from 2:33 the sun's rays sweep the sky. At 2:44 the sun is up: the win
at(167.5, 1.2, 'L17dawn', hb=[167.5, 176.5], hy=[720, 410], nr=5, spread=30, amp=24, per=4, ph=169, rw=24, rays=169, to=END)

EV.sort(key=lambda e: e['b'])
for e in EV:
    if e.pop('rnd', False): e['rnd'] = True

# the pulse: one number a bar (4 beats), from the song's loudness
energy = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L17', 'name': 'المرحلة السابعة عشرة', 'song': 'حالف لا ما انام', 'file': 'SONG_L17', 'bpm': 76.0, 'off': 0.205, 'end': END, 'cps': [0, 64, 120],
     'ez': {'ezg': 1, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.85, 'ezwn': 1.3}, 'energy': energy, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
