#!/usr/bin/env python3
# Makes data/dodge_F11.json: F11 "أجمل غرامي" (Abdullah Al Farwan), miracle's level, the new design (v52; the first design's data is
# data/dodge_F11_v51.json, made by gen.py, kept for the old replays). The song file starts at the song's 0:20.
# 100.675 BPM, beat 0 = 0.548 s into the file (0:20.55 in the song), a beat = 0.59598 s. Each beat has its two hits at 0 and 0.58 (dum, tak).
# (The first design's grid, 100.91 / 0.5405, was half a beat late by the end; the hits were measured again on the song itself.)
# Song map (beats): intro 0-4 | singing from 4 (0:22.9) | it opens up 28 (0:37.2) | break, no high drums 108 (1:24.9) | back 140 (1:44.0)
# | dip 216-220 (2:29.3) | win 257.5 (2:54.0)
# Parts: 1 the strings (0-108), 2 the letter (108-212), 3 the melody (212-257.5). Checkpoints 0, 108, 212.
# Nothing here is random: every attack is a fixed pattern on the beat. An event fires at its start minus EZW times its warning.
import json, math, os
from glyphs import shape, area

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_F11.json')
W, H = 1280, 720
BPM, OFF = 100.675, 0.548
SPB = 60 / BPM
TK = 0.58   # the tak, after each beat
EZW = 1.3
EV = []

def r4(v): return round(v, 3) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW - 0.05, k, hit=hit, wn=wn, **kw)
def ease(u): u = min(1, max(0, u)); return u * u * (3 - 2 * u)
def kf(kb, ka, b):
    if b <= kb[0]: return ka[0]
    if b >= kb[-1]: return ka[-1]
    i = max(j for j in range(len(kb) - 1) if kb[j] <= b)
    return ka[i] + (ka[i + 1] - ka[i]) * ease((b - kb[i]) / (kb[i + 1] - kb[i]))
def turning(kb, ka, b0, b1):
    """the angle changes somewhere in b0..b1"""
    return any(abs(kf(kb, ka, b0 + (b1 - b0) * q / 12) - kf(kb, ka, b0)) > 0.5 for q in range(13))

HOLD, DEC = 0.6, 0.5   # a pluck holds 0.6 beat, then dies out in 0.5
class Str:
    def __init__(s, d, a0=0, L=700, thr=0.25): s.d = d; s.a0 = a0; s.L = L; s.thr = thr; s.plk = []
    def pluck(s, b, n, A, ez=0): s.plk.append((b, n, A, ez))
    def busy(s, b0, b1): return any(p[0] < b1 and p[0] + HOLD + DEC > b0 for p in s.plk)
def strings(ss, b0, b1, kb=(0,), ka=(0,), wn=1.0):
    for s in ss:
        s.plk.sort()
        ev(b0, 'F11bstr', px=640, py=360, d=s.d, a0=s.a0, L=s.L, kb=list(kb), ka=list(ka), thr=s.thr, hold=HOLD, dec=DEC, wn=wn, to=b1,
           plk=[v for p in s.plk for v in p])

# ================= part 1: the strings (0-108) =================
# 0:20 the oud: three strings across; at 0:22 the singing starts and they are plucked on the dum, from 0:25 on the tak too
KB1 = [0, 31.4, 32.0, 39.4, 40.0, 47.4, 48.0, 55.4, 56.0, 63.4, 64.0, 70.0, 71.6, 98.4, 99.6]
KA1 = [0, 0, 22, 22, -22, -22, 40, 40, -32, -32, 12, 12, 90, 90, 180]
ev(0, 'F11bstage', part=1, to=109.4, kb=KB1, ka=KA1, keep=True)
TH = 0.22
A3 = [Str(-180, thr=TH), Str(0, thr=TH), Str(180, thr=TH)]
NS = [2, 3, 2, 4, 3, 2, 4, 3]
seq = [1, 0, 2, 1, 2, 0, 1, 2, 0, 2, 1, 0]
k = 0
for b in range(4, 28):
    for h in ((b,) if b < 6 else (b, b + TK)):
        cand = [seq[(k + q) % 12] for q in range(3)]; k += 1
        i = next((c for c in cand if not A3[c].busy(h, h + 0.3)), None)
        if i is None: continue
        A3[i].pluck(h, NS[k % 8], 70 if h == b else 60, ez=1 if h != b else 0)
# 0:27 the strum sweeps a lane on every second beat: get out of it, through a string at rest or through a plucked one's node
SW_A = [(12, 1, 1), (14, 2, -1), (16, 0, 1), (18, 3, -1), (20, 1, -1), (22, 2, 1), (24, 0, -1), (26, 3, 1)]
LANES3 = [(-372, -183), (-177, -3), (3, 177), (183, 372)]   # lane v ranges (between the strings, 3 px off them)
for j, (b, ln, wy) in enumerate(SW_A):
    v0, v1 = LANES3[ln]
    at(b, 1.5, 'F11bsweep', an=0, v0=v0, v1=v1, u0=-720 * wy, u1=720 * wy, bl=70, dur=1.0)
strings(A3, 0, 28.5)

# 0:37 the music opens up: four strings, two plucked on the dum and one on the tak, and the oud turns on every bar
B4 = [Str(-216, L=800, thr=TH), Str(-72, L=800, thr=TH), Str(72, L=800, thr=TH), Str(216, L=800, thr=TH)]
LANES4 = [(-380, -219), (-213, -75), (-69, 69), (75, 213), (219, 380)]
bnd4 = {0: [0], 1: [0, 1], 2: [1, 2], 3: [2, 3], 4: [3]}
SW_B = [(30, 2, 1), (32.6, 1, -1), (34, 3, 1), (36, 2, -1), (38, 0, 1), (40.6, 4, -1), (42, 1, 1), (44, 3, -1), (46, 2, 1), (48.6, 1, -1),
        (50, 3, 1), (52, 0, -1), (54, 4, 1), (56.6, 2, -1), (58, 1, 1), (60, 3, -1), (62, 2, 1), (64.6, 1, -1), (66, 3, 1), (68, 2, -1)]
seqB = [(1, 2), (0, 3), (1, 3), (0, 2), (2, 3), (0, 1)]
def span2(sw): return SW_B.index(sw) % 2 == 0 and sw[1] <= 3
k = 0
for b in range(28, 71):
    for h in (b, b + TK):
        if turning(KB1, KA1, h - 0.1, h + HOLD + DEC + 0.1): continue
        sw = next((x for x in SW_B if x[0] - 1.0 <= h < x[0]), None)
        if sw:
            # just before a strum the strings round it shake: the way out is through a node
            bs = sorted(set(bnd4[sw[1]] + (bnd4[sw[1] + 1] if span2(sw) else [])))
            bs = [bs[0], bs[-1]] if len(bs) > 1 else bs
            for i in bs:
                if not B4[i].busy(h, h + 0.3): B4[i].pluck(h, [2, 3][k % 2], 56, ez=1 if len(bnd4[sw[1]]) == 2 and i == bnd4[sw[1]][1] else 0)
            k += 1; continue
        pair = seqB[k % 6]; k += 1
        for q, i in enumerate(pair if h == b else pair[:1]):
            if B4[i].busy(h, h + 0.3): continue
            B4[i].pluck(h, NS[(k + q) % 8], 56 if h == b else 48, ez=1 if (h != b or q == 1) else 0)
for j, (b, ln, wy) in enumerate(SW_B):
    v0, v1 = LANES4[ln]; an = kf(KB1, KA1, b)
    if span2((b, ln, wy)): v1 = LANES4[ln + 1][1]   # two lanes at once, over the string between them
    at(b, 1.2, 'F11bsweep', an=an, v0=v0, v1=v1, u0=-820 * wy, u1=820 * wy, bl=70, dur=1.2, ezskip=j % 4 == 3)

# 1:02 the chord: the oud stands up, on every beat the pick runs across all four strings and each one shakes as it passes;
# the harmonic changes each time, so the still points jump between the middle and the top and bottom
def chord(b, y, wy, n, hor=False, dur=0.55, A=46, ez=0):
    if not hor:
        a, z = (380, 900) if wy > 0 else (900, 380)
        at(b, 1.0, 'F11bpick', x0=a, y0=y, x1=z, y1=y, r=26, dur=dur, ezskip=ez)
        for s in B4:
            x = 640 - s.d; s.pluck(b + dur * (x - a) / (z - a), n, A, ez)
    else:
        a, z = (60, 660) if wy > 0 else (660, 60)
        at(b, 1.0, 'F11bpick', x0=y, y0=a, x1=y, y1=z, r=26, dur=dur, ezskip=ez)
        for s in B4:
            yy = 360 - s.d; s.pluck(b + dur * (yy - a) / (z - a), n, A, ez)
HY = [160, 560, 360, 220, 500, 160, 560, 300]
for j, b in enumerate(range(72, 98)):
    chord(b, HY[j % 8], 1 if j % 2 == 0 else -1, [2, 3][j % 2], ez=1 if j % 4 == 3 else 0)
# between the chords, the wide sides are swept down and up
for j, b in enumerate([73 + TK, 75 + TK, 77 + TK, 79 + TK, 81 + TK, 83 + TK, 85 + TK, 87 + TK, 89 + TK, 91 + TK, 93 + TK, 95 + TK]):
    ln = 0 if j % 2 == 0 else 4; wy = 1 if (j // 2) % 2 == 0 else -1
    v0, v1 = (-640, -219) if ln == 0 else (219, 640)
    at(b, 1.2, 'F11bsweep', an=90, v0=v0, v1=v1, u0=-420 * wy, u1=420 * wy, bl=70, dur=1.2, ezskip=j % 2 == 1)
# 1:20 the oud turns flat (upside down) and the chord comes from the top and the bottom, on the dum and the tak
for j, h in enumerate([100, 100 + TK, 101, 101 + TK, 102, 102 + TK, 103, 103 + TK, 104, 104 + TK, 105, 105 + TK, 106]):
    chord(h, [300, 900, 520, 760, 400, 980, 640, 220, 1060, 460, 820, 340, 640][j], 1 if j % 2 == 0 else -1, [2, 3][(j // 2) % 2], hor=True, dur=0.45, ez=1 if j % 2 else 0)
strings(B4, 27, 108, kb=KB1, ka=KA1)

# ================= part 2: the letter (108-212) =================
ev(106.6, 'F11bstage', part=2, to=213.4, keep=True)   # each stage fades in over the last one
ev(108, 'F11bfx', v=0.35, s=6, dur=1.2)

def glyphs_of(text):
    """the glyphs (main outlines) and their dots, font units"""
    gl, wd = shape(text); out = []
    for x, adv, cs in gl:
        if not cs: continue
        main = [c for c in cs if abs(area(c)) >= 20000 or area(c) > 0]
        dots = [c for c in cs if not (abs(area(c)) >= 20000 or area(c) > 0)]
        out.append(dict(x=x, adv=adv, main=main, dots=[(sum(p[0] for p in c) / len(c), sum(p[1] for p in c) / len(c)) for c in dots]))
    return out, wd
def pen(glist, times, fills, cx=0, cy=0, kb=(0,), ka=(0,), life=2.0, w=12, wn=1.0, to=None, ezfill=False, still=False):
    """glist: [[contour in screen/local px]...] per glyph; times: (b0, b1) per glyph; fills: (fill beat, length) or None"""
    pts, ci, cg, gt = [], [0], [], []
    for j, cs in enumerate(glist):
        for c in cs:
            for x, y in c: pts += [round(x, 1), round(y, 1)]
            ci.append(len(pts) // 2); cg.append(j)
        f = fills[j]; gt += [times[j][0], times[j][1], f[0] if f else -1, f[1] if f else 0]
    first = min([f[0] for f in fills if f] + ([] if still else [times[0][0]]))
    at(first, wn, 'F11bpen', pts=pts, ci=ci, cg=cg, gt=gt, cx=cx, cy=cy, kb=list(kb), ka=list(ka), life=life, w=w, to=to, ezfill=ezfill, still=still)

# 1:25 the break: the pen writes the word "غرامي" one giant letter at a time, from the right; each letter fills with ink when it is done,
# and its dots fall into place as diamonds
S1 = 0.47; BASE1 = 430
LET = [('غ', 1050, 108.6, 113.4), ('ر', 800, 114.6, 117.4), ('ا', 640, 118.6, 120.4), ('م', 440, 121.6, 125.4), ('ي', 190, 126.6, 131.4)]
gl, tm, fl, flick = [], [], [], []
dots = []
for ch, xc, b0, b1 in LET:
    g, wd = glyphs_of(ch); g = g[0]
    xs = [p[0] for c in g['main'] for p in c]; mx = (min(xs) + max(xs)) / 2
    conv = lambda px, py: (xc + (px - mx) * S1, BASE1 - py * S1)
    gl.append([[conv(*p) for p in c] for c in g['main']]); tm.append((b0, b1)); fl.append((b1 + 0.6, 2.0))
    flick.append((conv(*g['main'][0][0]), b1 + 0.6 + TK))
    for (dx, dy) in g['dots']: dots.append((conv(dx, dy), b1 + 0.6 + TK))
pen(gl, tm, fl, life=3.0, w=17, to=136)
# the lines of the letter: a chain of dots runs down (or up) the page on every second beat, with one space in it, the space walking across
for j, b in enumerate(range(111, 139, 2)):
    gx = [200, 420, 640, 860, 1080, 860, 640, 420][j % 8]; dn = j % 2 == 0
    at(b, 1.2, 'F11brow', vert=True, p0=-20 if dn else H + 20, p1=H + 20 if dn else -20, gc=gx, gw=130, r=14, dur=2.4, ezskip=j % 4 == 3)
# the pen flicks a drop of ink at you when it lifts off a letter
for (x, y), b in flick:
    at(b, 0.8, 'F11bdot', x0=x, y0=y, x1=x, y1=y, len=1500, r=15, dur=2.6, hold=0, aim=True, rnd=True)
for (x, y), b in dots:
    at(b - 0.9, 1.0, 'F11bdot', x0=x, y0=-30, x1=x, y1=y, r=17, dur=0.9, hold=0.6, stay=True, to=136)
# the rain between the letters: one diamond on each tak, its column walking across the screen
for j, b in enumerate([x + d for x in range(110, 140) for d in (0, TK)]):
    q = j % 10; x = 110 + 118 * (q if q < 6 else 10 - q)
    if j % 2: x = W - x
    if j % 2 and j % 4 != 1: continue
    at(b, 1.0, 'F11bdot', x0=x, y0=-30 if j % 2 == 0 else H + 30, x1=x, y1=H + 30 if j % 2 == 0 else -30, r=15, dur=1.25, hold=0, ezskip=j % 2 == 1)

# 1:44 the music is back: two pens write the same words in mirror writing (the right half as it reads, the left half its mirror):
# "أجمل" on the top line, then "غرامي" on the bottom line
def word_px(text, s, x0, base):
    g, wd = glyphs_of(text); out = []
    for q in g:
        out.append(dict(main=[[(x0 + p[0] * s, base - p[1] * s) for p in c] for c in q['main']], dots=[(x0 + d[0] * s, base - d[1] * s) for d in q['dots']], x=x0 + (q['x'] + q['adv'] / 2) * s))
    return out, wd * s
def mirror_write(text, s, base, b0, b1, life, fill_len):
    g, wd = word_px(text, s, 652, base)
    g.sort(key=lambda q: -q['x'])   # written from the right (the right half), from the middle out (its mirror)
    n = len(g); per = (b1 - b0) / n
    tms = [(b0 + per * j, b0 + per * (j + 1) - 0.15) for j in range(n)]
    fls = [(round(b0 + per * (j + 1)) + TK, fill_len) for j in range(n)]
    for side in (1, -1):
        gg = [[[(p[0] if side > 0 else W - p[0], p[1]) for p in c] for c in q['main']] for q in g]
        if side < 0: gg = [[c[::-1] for c in cs] for cs in gg]
        pen(gg, tms, fls, life=life, w=15, to=176)
        if side > 0:
            for j in range(n): at(tms[j][1] + 0.3, 0.8, 'F11bdot', x0=gg[j][0][0][0], y0=gg[j][0][0][1], x1=0, y1=0, len=1500, r=14, dur=2.6, hold=0, aim=True, rnd=True)
        for j, q in enumerate(g):
            for (x, y) in q['dots']:
                xx = x if side > 0 else W - x
                at(math.ceil(tms[j][1]) + TK - 0.8, 1.0, 'F11bdot', x0=xx, y0=-30 if base < 360 else H + 30, x1=xx, y1=y, r=13, dur=0.8, hold=0.6, stay=True, to=176, ezskip=side < 0)
    return g
# through the middle line, between the two words: a diamond across on every tak
for j, b in enumerate([x + TK for x in range(140, 158)]):
    y = [440, 400, 470, 380][j % 4] if b < 149 else [300, 340, 260, 360][j % 4]; wy = 1 if j % 2 == 0 else -1
    at(b, 1.0, 'F11bdot', x0=640 - 680 * wy, y0=y, x1=640 + 680 * wy, y1=y, r=15, dur=1.4, hold=0, ezskip=j % 2 == 1)
# and from the middle out, a column of dots and its mirror, on every second beat
for j, b in enumerate(range(141, 158, 2)):
    gy = [200, 520, 360, 120, 600, 280, 440, 360, 200][j]
    for sd in (1, -1):
        at(b, 1.2, 'F11brow', vert=False, p0=640 + 10 * sd, p1=640 + 680 * sd, gc=gy, gw=130, r=14, dur=2.2, ezskip=j % 2 == 1 and sd < 0)
mirror_write('أجمل', 0.25, 262, 140.0, 148.6, 2.5, 1.3)
mirror_write('غرامي', 0.225, 610, 149.0, 157.6, 2.5, 1.3)
# 1:51 the ink sets: from the middle out the letters fill again in pairs (dum and tak), the top line then the bottom
# (the same outlines once more as a pen with no time to write: written in a moment, then only the fills)
for (text, s, base, b0) in [('أجمل', 0.25, 262, 158.0), ('غرامي', 0.225, 610, 162.0), ('أجمل', 0.25, 262, 166.0), ('غرامي', 0.225, 610, 170.0)]:
    g, wd = word_px(text, s, 652, base); g.sort(key=lambda q: q['x'])   # from the middle out
    for side in (1, -1):
        gg = [[[(p[0] if side > 0 else W - p[0], p[1]) for p in c] for c in q['main']] for q in g]
        tms = [(b0 - 0.6, b0 - 0.5) for _ in g]
        fls = [(b0 + [0, TK, 1, 1 + TK, 2][j], 1.0) for j in range(len(g))]
        pen(gg, tms, fls, life=0.0, w=1, to=b0 + 4, ezfill=side < 0, still=True)
# lines of dots close in from the top and the bottom to the middle line (mirrored), on every second beat
for j, b in enumerate(range(159, 176, 2)):
    gx = [640, 300, 980, 460, 820, 180, 1100, 640, 400][j]
    for sd in (1, -1):
        at(b + TK, 1.2, 'F11brow', vert=True, p0=360 - 380 * sd, p1=360 - 10 * sd, gc=gx if sd > 0 else W - gx, gw=130, r=14, dur=2.0, ezskip=j % 2 == 1 and sd < 0)
# and the diamonds across, the line between the two words, on every dum
for j, b in enumerate([x + d for x in range(159, 176) for d in (0, TK)]):
    y = [380, 330, 410, 300][j % 4]; wy = 1 if j % 2 == 0 else -1
    at(b, 1.0, 'F11bdot', x0=640 - 680 * wy, y0=y, x1=640 + 680 * wy, y1=y, r=15, dur=1.6, hold=0, ezskip=j % 2 == 1)

# 1:58 the ring: the pen writes "أجمل غرامي" round the middle, the ring turns; then the ink runs round it, letter by letter,
# and diamonds burst from the middle through the ring
RS, RR = 0.18, 215
KBR = [176, 188, 192, 196, 200, 204, 208, 212]
KAR = [0, 40, -10, 50, 0, 70, 20, 40]
g, wd = glyphs_of('أجمل غرامي')
def ring(px, py):
    th = -math.pi / 2 + (px * RS - wd * RS / 2) / RR
    r = RR + py * RS
    return (r * math.cos(th), r * math.sin(th))
def dense(c, step=6):
    out = []
    for i in range(len(c)):
        a = c[i]; b = c[(i + 1) % len(c)]; n = max(1, int(math.dist(a, b) * RS / step))
        out += [(a[0] + (b[0] - a[0]) * q / n, a[1] + (b[1] - a[1]) * q / n) for q in range(n)]
    return out
gg = [[[ring(*p) for p in dense(c)] for c in q['main']] for q in g[::-1]]   # written from the right of the word
n = len(gg); per = 12.0 / n
tms = [(176 + per * j, 176 + per * (j + 1) - 0.1) for j in range(n)]
# the ink running round: glyph j fills at 188 + j (dum), again from 199 + (n-1-j) going back, and once more from 206
fls = [None] * n
pen(gg, tms, fls, cx=640, cy=360, kb=KBR, ka=KAR, life=2.5, w=11, to=213)
for rnd, (b0, order) in enumerate([(188.0, range(n)), (198.0, range(n - 1, -1, -1)), (207.0, range(n))]):
    tm2 = [(b0 - 0.6, b0 - 0.55)] * n
    fl2 = [None] * n
    for k2, j in enumerate(order): fl2[j] = (b0 + k2 * 0.58 if rnd == 2 else b0 + k2 * 1.0, 2.0)
    pen(gg, tm2, fl2, cx=640, cy=360, kb=KBR, ka=KAR, life=0.0, w=1, to=b0 + n + 1.5, ezfill=rnd == 2, still=True)
for j in range(n): at(tms[j][1] + 0.2, 0.8, 'F11bdot', x0=640 + gg[j][0][0][0], y0=360 + gg[j][0][0][1], x1=0, y1=0, len=1500, r=14, dur=2.6, hold=0, aim=True, rnd=True, ezskip=j % 2 == 1)
# the bursts: eight diamonds out from the middle, on every second dum, each burst turned half a step
for j, b in enumerate(list(range(178, 188, 2)) + [x + d for x in range(189, 211) for d in (-TK, 0)]):
    for q in range(14):
        a = (q + 0.5 * (j % 2)) * math.pi / 7
        at(b + TK, 1.0, 'F11bdot', x0=640, y0=360, x1=640 + 800 * math.cos(a), y1=360 + 800 * math.sin(a), r=14, dur=2.1, hold=0, ezskip=q % 2 == 1)

# ================= part 3: the melody (212-257.5) =================
# 2:27 the strings come back through the middle, three of them like the rays of the oud's rose, and they turn
KB3 = [212, 220, 228, 236, 244, 250, 257.5]
KA3 = [0, 30, -30, 60, 0, 90, 60]
ev(210.6, 'F11bstage', part=3, to=260, kb=KB3, ka=KA3, keep=True)
ev(212, 'F11bfx', v=0.4, s=8, dur=1.4)
R3 = [Str(0, a0=0, L=760, thr=0.2), Str(0, a0=60, L=760, thr=0.2), Str(0, a0=120, L=760, thr=0.2)]
k = 0
for b in range(220, 236):
    for h in (b, b + TK):
        i = k % 3; k += 1
        R3[i].pluck(h, [2, 3, 4, 3][(k // 3) % 4], 72 if h == b else 62, ez=1 if h != b else 0)
        R3[(i + 1) % 3].pluck(h, [4, 2, 3][(k // 3) % 3], 64 if h == b else 56, ez=1)
for b in range(236, 250):
    pair = [(0, 1), (1, 2), (2, 0)][b % 3]
    for i in pair: R3[i].pluck(b, [2, 4][b % 2], 64, ez=1 if i == pair[1] else 0)
    R3[3 - sum(pair)].pluck(b + TK, 3, 60, ez=1)
for b in range(250, 257):
    for i in range(3): R3[i].pluck(b, 2 if b % 2 == 0 else 4, 64, ez=1 if i == 2 else 0)
# the last diamonds, in from the corners on the tak, between the rays
for j, b in enumerate(range(222, 256)):
    cx = [(0, 0), (W, 0), (W, H), (0, H)][j % 4]
    at(b, 0.8, 'F11bdot', x0=cx[0], y0=cx[1], x1=0, y1=0, len=1600, r=14, dur=2.6, hold=0, aim=True, rnd=True, ezskip=j % 2 == 1)
    at(b + TK, 1.0, 'F11bdot', x0=cx[0], y0=cx[1], x1=640 + (640 - cx[0]) * 0.2, y1=360 + (360 - cx[1]) * 0.2, r=15, dur=1.3, hold=0, ezskip=j % 2 == 1)
strings(R3, 212, 257.5, kb=KB3, ka=KA3)
ev(257.5, 'F11bend')

EV.sort(key=lambda d: d['b'])
en = json.load(open(os.path.join(HERE, 'energy2.json')))['energy']
D = {'id': 'F11', 'name': 'أجمل غرامي', 'by': 'miracle', 'ver': 'Mohammed', 'file': 'SONG_F11', 'bpm': BPM, 'off': OFF, 'end': 257.5, 'cps': [0, 108, 212],
     'ez': {'ezg': 0, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': en, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print(len(EV), 'events', os.path.getsize(OUT) // 1024, 'KB')
