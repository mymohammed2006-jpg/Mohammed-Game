#!/usr/bin/env python3
# Makes data/dodge_L19.json: L19 "عافني وراح" (Ahmed Jawad), what he left behind: his letter, his oud, your photo.
# The song file starts at 0:18 of the original (songs/SONG_L19.mp3). 99.915 BPM, beat 0 = 0.344 s, a beat = 0.6005 s.
# Song map (beats; song time = 18.344 + 0.6005 b): band 0-6 (0:18) | soft, no bass 6-50 (0:22) | full band 50-134 (0:48.4)
# | dip 134-142 (1:38.8) | full 142-166 (1:43.6) | the quiet 166-206 (1:58) | the last part 206-239 (2:22) | win 239.25 (2:42.0)
# Parts: 1 the letter (0-50), 2 the oud (50-166), 3 the photo (166-239). Checkpoints 0, 50, 166.
# Nothing here is random: every attack is a fixed pattern on the beat. Times are beats; an event fires at its start minus
# wn * EZW (Easy's longest warning) and shows its warning from start - wn.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L19.json')
W, H = 1280, 720
CX, CY = W / 2, H / 2
EZW = 1.3
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(b, 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def at(hit, wn, k, **kw): ev(hit - wn * EZW, k, hit=hit, wn=wn, **kw)
def fx(b, v=0.3, s=10, dur=1.5): ev(b, 'L19fx', v=v, s=s, dur=dur)
def drop(hit, x, y, R=78, ang=0, hold=1.2, wn=1.5): at(hit, wn, 'L19drop', x=x, y=y, R=R, ang=ang, hold=hold)

ev(0, 'L19stage', pb=[0, 50, 166], keep=True)

# ================= part 1: the letter (0-50) =================
# 0:18 ink drops land on the beat, one after another across the page (a blot and its splashes)
for i, (b, x, y) in enumerate([(2, 1000, 200), (3, 820, 520), (4, 620, 200), (5, 440, 520), (5.5, 1060, 560), (6, 250, 210)]):
    drop(b, x, y, ang=0.4 * i)

# 0:22 the pen writes the page row by row from the right; the wet ink hurts for 3 beats. Cross the wet rows through the spaces,
# which make two stairs (one from the left, one from the right)
ROWS = 8; RH = 90
gx = []
for j in range(ROWS): gx += [260 + 55 * j, 1020 - 55 * j]
at(7, 1.5, 'L19pen', y0=0, rh=RH, ord=list(range(ROWS)), gx=gx, ng=2, gw=110, dr=1.5, wr=1.5, wet=3, to=22.5)

# 0:31 the second page, from the bottom up and from the left, in wide rows
gx = []
for j in range(4): gx += [200 + 60 * j, 760 + 60 * j]
at(21.5, 1.5, 'L19pen', y0=0, rh=180, ord=[3, 2, 1, 0], gx=gx, ng=2, gw=130, dr=1.6, wr=1.5, wet=2.5, ltr=True, to=31)

# the last drops on the dry page
drop(27.5, 300, 600, ang=0.2); drop(28.5, 980, 600, ang=1.0); drop(29.5, 640, 560, ang=0.6)

# 0:37 the letter folds: a flap of the page turns over its line and lands on the other side, then turns back.
# Get out of the flap and out of where it lands (the line and the landing show 2 beats before)
def fold(hit, fx_, fy_, na, fl=0.8, hold=1.0, wn=2): at(hit, wn, 'L19fold', fx=fx_, fy=fy_, na=na, fl=fl, hold=hold)
FOLDS = [
    (31.0, 320, 360, 180),      # the left part over the middle: stay right
    (33.5, 640, 240, 270),      # the top over the middle: go down
    (36.0, 960, 360, 0),        # the right part over the middle: go left
    (38.5, 640, 480, 90),       # the bottom over the middle: go up
    (41.0, 200, 200, 225),      # the top left corner (the envelope): go right
    (43.5, 800, 800, 45),       # the bottom right corner
    (43.5, 1080, 200, 315),     # and the top right corner at the same time
    (45.5, 280, 360, 180), (45.5, 1000, 360, 0),   # both sides at once: the middle strip
    (47.4, 640, 200, 270),      # the top: go down in the strip
]
for f in FOLDS: fold(*f)

# ================= part 2: the oud (50-166) =================
fx(50, 0.35, 12, 1.5)
# 0:48 the strings shake in standing waves: the safe places are the still points (the nodes, white dots). Every 2 beats the note
# changes (the next nodes show as grey rings) and the safe places move. Then plucks run along the strings and bounce at the ends:
# cross a still string (grey, harmless) to get out of their way. Then the plectrum strokes across, and every string it crosses
# shakes for 1.5 beats. At 1:39 the strings snap one by one and their halves whip round (towards the edge they are nearest)
YS = [-270, -90, 90, 270]; LH = 760
hs, hn = [], []
SEQ = [(50 + 2 * i, ns) for i, ns in enumerate([  # beat: loops per string (0 = still); every lane keeps a safe place across each change
    [0, 1, 1, 0], [0, 4, 4, 0], [0, 3, 3, 0], [0, 2, 2, 0], [0, 2, 3, 0], [3, 5, 5, 2], [2, 3, 2, 5], [3, 3, 5, 2], [5, 2, 5, 3], [3, 2, 5, 2],
    [2, 5, 3, 3], [3, 5, 5, 2], [5, 3, 2, 3], [3, 3, 5, 2], [2, 5, 2, 5], [3, 2, 3, 2]])] + [(80, [0, 0, 0, 0])]
for b, ns in SEQ: hs.append(b); hn += ns
# plucks (beat, string, frame x)
pb, pi_, pxs = [], [], []
def pluck(b, i, x): pb.append(b); pi_.append(i); pxs.append(x)
for k in range(4): pluck(81 + 2 * k, 1 + (k % 2), 0)                       # 1:06 the middle strings from the middle
for k in range(8): pluck(89 + k, 0 if k % 2 == 0 else 3, 450 if k % 4 < 2 else -450)   # the outer strings near the ends
for k in range(8):                                                         # two at once, mirror places
    b = 97 + k; x = [-500, -200, 200, 500][k % 4]
    pluck(b, 0 if k % 2 == 0 else 1, x); pluck(b, 2 if k % 2 == 0 else 3, -x)
for k in range(7): pluck(105 + k, [1, 2, 0, 3][k % 4], [-300, 300, 0, -600, 600, 0, -300][k])
# the plectrum: strokes up and down across the strings while it moves right, then one long stroke back
KT = [112, 114, 116, 118, 120, 122, 124, 126, 128.5, 131, 133]
KX = [100, 260, 420, 580, 740, 900, 1060, 1220, 700, 180, 180]
KY = [-40, 760, -40, 760, -40, 760, -40, 760, -40, 760, 760]
eb, ei, en, ed = [], [], [], []
for s in range(len(KT) - 1):
    for i, yf in enumerate(YS):
        y = CY + yf
        if (KY[s] - y) * (KY[s + 1] - y) < 0:
            u = (y - KY[s]) / (KY[s + 1] - KY[s]); eb.append(round(KT[s] + u * (KT[s + 1] - KT[s]), 3)); ei.append(i); en.append(2 if s % 2 == 0 else 3); ed.append(1.5)
# the snaps
snb, sni, snx, sna = [136, 137.5, 139, 140.5], [0, 3, 1, 2], [-100, 150, 0, -60], [-90, 90, -90, 90]
at(50, 1.5, 'L19strings', lh=LH, A=100, th=6, ys=YS, on=[58, 50, 50, 58], off=[142] * 4, hs=hs, hn=hn, md=0.5, pwn=1.5,
   pb=pb, pi=pi_, pxs=pxs, pv=560, pw=70, pa=112, pl=3, pfw=1,
   eb=eb, ei=ei, en=en, ed=ed, kt=KT, kx=KX, ky=KY, kr=30, kwn=2,
   snb=snb, sni=sni, snx=snx, sna=sna, snd=0.6, snh=0.5, snw=1.5, snp=600, to=142)

# 1:43 new strings in a frame that turns slowly one way then the other: the nodes turn with it
fx(142, 0.3, 10, 1.2)
YS2 = [-500, -300, -100, 100, 300, 500]
SEQ2 = [(142 + 2 * i, ns) for i, ns in enumerate([
    [2, 3, 2, 3, 3, 2], [3, 2, 5, 5, 2, 3], [2, 3, 3, 3, 3, 2], [3, 5, 2, 2, 5, 3], [2, 2, 3, 3, 2, 2], [5, 3, 2, 2, 3, 5],
    [2, 5, 3, 3, 5, 2], [3, 2, 2, 2, 2, 3], [2, 3, 5, 5, 3, 2], [3, 2, 3, 3, 2, 3], [5, 3, 2, 2, 3, 5], [2, 2, 2, 2, 2, 2]])]
hs2, hn2 = [], []
for b, ns in SEQ2: hs2.append(b); hn2 += ns
at(142, 1.5, 'L19strings', lh=900, A=110, th=6, ys=YS2, on=[142] * 6, off=[166] * 6, hs=hs2, hn=hn2, md=0.5, pwn=1.5,
   a0=0, sb0=[142, 148, 154, 160], sw0=[0.04, -0.04, 0.04, -0.04], to=166)

# ================= part 3: the photo (166-239) =================
fx(166, 0.25, 6, 1.0)

def zig(a, b, n, amp, vertical, seed):
    # a zigzag line from a to b (n teeth), across x (vertical line: points (x, y)) or across y
    pts = []
    for i in range(n + 1):
        u = i / n; o = 0 if i in (0, n) else amp * (1 if (i + seed) % 2 else -1) * (0.6 + 0.4 * math.sin(i * 1.7 + seed))
        pts.append((a + o, u) if vertical else (u, a + o))
    return pts
def vline(x0, n, amp, seed, y0=-20, y1=H + 20):
    return [(x0 + (0 if i in (0, n) else amp * (1 if (i + seed) % 2 else -1) * (0.6 + 0.4 * math.sin(i * 1.7 + seed))), y0 + (y1 - y0) * i / n) for i in range(n + 1)]
def hline(y0, n, amp, seed, x0=-20, x1=W + 20):
    return [(x0 + (x1 - x0) * i / n, y0 + (0 if i in (0, n) else amp * (1 if (i + seed) % 2 else -1) * (0.6 + 0.4 * math.sin(i * 1.3 + seed)))) for i in range(n + 1)]
def fx_of(line, y):
    # x of a vertical polyline at y
    for (x0, y0), (x1, y1) in zip(line, line[1:]):
        if y0 <= y <= y1: return x0 + (x1 - x0) * (y - y0) / (y1 - y0) if y1 != y0 else x0
    return line[0][0] if y < line[0][1] else line[-1][0]
def fy_of(line, x):
    for (x0, y0), (x1, y1) in zip(line, line[1:]):
        if x0 <= x <= x1: return y0 + (y1 - y0) * (x - x0) / (x1 - x0) if x1 != x0 else y0
    return line[0][1] if x < line[0][0] else line[-1][1]

def photo(hit, to, vlines, hl, tears, flips, gk=None, gv=None, fires=None, wn=1.0, rot=None, fez=None, extra=None):
    # pieces: columns between the vertical lines (and the edges), rows above / below the horizontal line
    nc = len(vlines) + 1; nr = 2 if hl else 1
    def L(c): return None if c == 0 else vlines[c - 1]
    def R(c): return None if c == nc - 1 else vlines[c]
    vx, vy, vc = [], [], []
    for r in range(nr):
        for c in range(nc):
            pts = []
            # sample the boundary: top, right, bottom, left
            def top(x): return 0 if (r == 0 or not hl) else fy_of(hl, x)
            def bot(x): return H if (r == nr - 1 or not hl) else fy_of(hl, x)
            def lx(y): return 0 if L(c) is None else fx_of(L(c), y)
            def rx(y): return W if R(c) is None else fx_of(R(c), y)
            # corners (solve a few rounds)
            def corner(fxl, fyl, y):
                for _ in range(8): x = fxl(y); y = fyl(x)
                return x, y
            tl = corner(lx, top, 0 if r == 0 else H / 2); tr = corner(rx, top, 0 if r == 0 else H / 2)
            bl = corner(lx, bot, H if r == nr - 1 else H / 2); br = corner(rx, bot, H if r == nr - 1 else H / 2)
            N = 14
            for i in range(N + 1): x = tl[0] + (tr[0] - tl[0]) * i / N; pts.append((x, top(x)))
            for i in range(1, N): y = tr[1] + (br[1] - tr[1]) * i / N; pts.append((rx(y), y))
            for i in range(N + 1): x = br[0] + (bl[0] - br[0]) * i / N; pts.append((x, bot(x)))
            for i in range(1, N): y = bl[1] + (tl[1] - bl[1]) * i / N; pts.append((lx(y), y))
            vc.append(len(pts)); vx += [round(p[0], 1) for p in pts]; vy += [round(p[1], 1) for p in pts]
    npc = nc * nr
    tt, td, tnx, tny, dd, tlx, tly, tlc, psd = [], [], [], [], [], [], [], [], []
    for (b, dur, line, nxy, d, side) in tears:
        tt.append(b); td.append(dur); tnx.append(nxy[0]); tny.append(nxy[1]); dd.append(d)
        tlc.append(len(line)); tlx += [round(p[0], 1) for p in line]; tly += [round(p[1], 1) for p in line]
    for k in range(npc):
        r, c = divmod(k, nc)
        for (b, dur, line, nxy, d, side) in tears: psd.append(side(r, c))
    fc, ft, fz = [], [], []
    for k in range(npc):
        ts = flips(k // nc, k % nc); fc.append(len(ts)); ft += ts
        fz += fez(k // nc, k % nc, ts) if fez else [1] * len(ts)
    kw = dict(vx=vx, vy=vy, vc=vc, tt=tt, td=td, tnx=tnx, tny=tny, dd=dd, tlx=tlx, tly=tly, tlc=tlc, psd=psd,
              rot=rot or [0] * npc, dr=2, fc=fc, ft=ft, fez=fz, fd=0.3, fwn=1.5, twn=2, to=to, fw=30, bwn=1.5)
    if gk: kw.update(gk=gk, gv=gv, gd=1)
    if fires: kw.update(bx=[f[1] for f in fires], by=[f[2] for f in fires], bt=[f[0] for f in fires], bv=[f[3] for f in fires])
    at(hit, wn, 'L19photo', **kw)

# 1:58 your photo tears: down the middle, across, then each half again. The pieces drift apart (the table between them is safe)
V1, V2, V3 = vline(330, 9, 22, 1), vline(640, 9, 28, 2), vline(950, 9, 22, 3)
HL = hline(360, 12, 24, 4)
def shift(line, dx, dy_top, dy_bot, ycut=360):
    return [(x + dx, y + (dy_top if y < ycut else dy_bot)) for (x, y) in line]
def part(line, top):
    return [p for p in line if (p[1] < HL[0][1] + 10) == top] if False else line
D1, D2, D3 = 60, 45, 35
def v_in_row(line, r, dx):
    # the part of a vertical line inside one row, moved where that row's pieces are when it tears
    out = []
    for (x, y) in line:
        hy = fy_of(HL, x)
        if (r == 0 and y <= hy) or (r == 1 and y >= hy): out.append((x + dx, y + (-D2 if r == 0 else D2)))
    return out
tears = [
    (168, 1.0, V2, (1, 0), D1, lambda r, c: -1 if c < 2 else 1),
    (171, 1.0, [(x, y) for (x, y) in HL], (0, 1), D2, lambda r, c: -1 if r == 0 else 1),
    (174, 0.75, v_in_row(V1, 0, -D1), (1, 0), D3, lambda r, c: 0 if (c >= 2 or r != 0) else (-1 if c == 0 else 1)),
    (174, 0.75, v_in_row(V1, 1, -D1), (1, 0), D3, lambda r, c: 0 if (c >= 2 or r != 1) else (-1 if c == 0 else 1)),
    (175.5, 0.75, v_in_row(V3, 0, D1), (1, 0), D3, lambda r, c: 0 if (c < 2 or r != 0) else (-1 if c == 2 else 1)),
    (175.5, 0.75, v_in_row(V3, 1, D1), (1, 0), D3, lambda r, c: 0 if (c < 2 or r != 1) else (-1 if c == 2 else 1)),
]
# the horizontal tear runs across the gap of the first one: draw it where the halves are then (the left half moved left)
tears[1] = (171, 1.0, [(x + (-D1 if x < 640 else D1), y) for (x, y) in HL], (0, 1), D2, tears[1][5])
# which pieces show their back (pink) in each 2-beat window from 178: checker, the other checker, rows, columns, all but one...
def S(*ks): return set(ks)
ALLP = set(range(8))
WIN = [
    (178, S(0, 2, 5, 7)), (180, S(1, 3, 4, 6)), (182, ALLP - S(5)), (184, S(0, 1, 2, 3)), (186, S(4, 5, 6, 7)), (188, ALLP - S(2)),
    (190, S(0, 3, 5, 6)), (192, S(1, 2, 4, 7)), (194, S(0, 1, 6, 7)), (196, ALLP - S(4)), (198, S(2, 3, 4, 5)), (200, S(0, 2, 5, 7)), (202, S()),
]
def flips(r, c):
    k = r * 4 + c; ts = []; back = False
    for b, st in WIN:
        nb = k in st
        if nb != back: ts.append(b); back = nb
    return ts
# Easy: skip the "all but one" windows (each skipped window removes a pair of flips, so the piece ends the same)
def fez(r, c, ts):
    k = r * 4 + c; out = []
    for b in ts:
        skip = any(abs(b - wb) < 1e-6 or abs(b - (wb + 2)) < 1e-6 for wb, st in WIN if len(st) == 7 and k in st)
        out.append(0 if skip else 1)
    return out
# the pieces come back together (no table between them) and go apart again
GK = [180, 183, 188, 191, 196, 199]; GV = [0, 1, 0, 1, 0, 1]
# fires from two corners at 2:12 and 2:14: the front burns the pieces, behind it is ash. Cross it through the table when the
# pieces are apart, or dash through it
FIRES = [(190, 1300, -30, 230), (194, -30, 750, 230)]
photo(167, 206, [V1, V2, V3], HL, tears, flips, fez=fez, gk=GK, gv=GV, fires=FIRES, rot=[0.05, -0.04, 0.04, -0.06, -0.05, 0.03, -0.04, 0.06])
# the tears fall on the ash
for i, (b, x, y) in enumerate([(200.5, 400, 250), (201.5, 880, 470), (202.5, 640, 200), (203.5, 300, 520), (204.5, 980, 220), (205.5, 640, 520)]):
    drop(b, x, y, R=72, ang=0.5 * i)

# ================= the last part (206-239) =================
fx(206, 0.4, 14, 1.5)
# 2:22 a new page, written faster
gx = []
for j in range(ROWS): gx += [1040 - 60 * j, 220 + 50 * j]
at(206.5, 1.5, 'L19pen', y0=0, rh=RH, ord=list(range(ROWS)), gx=gx, ng=2, gw=110, dr=1.25, wr=1.25, wet=3, to=220)
for i, (b, x, y) in enumerate([(212, 1080, 600), (213, 200, 600), (214, 640, 620)]):
    drop(b, x, y, R=70, ang=0.3 * i)
# 2:28 the strings again, the note changing faster, and plucks
SEQ3 = [(216 + 1.5 * i, ns) for i, ns in enumerate([[2, 2, 2, 2], [3, 3, 5, 2], [2, 5, 2, 5], [3, 2, 3, 2], [5, 3, 2, 3], [2, 3, 2, 5], [3, 5, 5, 2], [2, 2, 2, 2]])] + [(228, [0, 0, 0, 0])]
hs3, hn3 = [], []
for b, ns in SEQ3: hs3.append(b); hn3 += ns
at(216, 1.5, 'L19strings', lh=LH, A=100, th=6, ys=YS, on=[216] * 4, off=[228] * 4, hs=hs3, hn=hn3, md=0.5, pwn=1.5, to=228)
# 2:35 the last photo: it tears in four, the pieces flip, then it burns from its four corners while ink drops fall
V = vline(640, 8, 24, 5); HH = hline(360, 10, 20, 6)
tears4 = [(229.5, 0.75, V, (1, 0), 55, lambda r, c: -1 if c == 0 else 1), (230.5, 0.75, [(x + (-55 if x < 640 else 55), y) for (x, y) in HH], (0, 1), 45, lambda r, c: -1 if r == 0 else 1)]
WIN4 = [(232, {0, 3}), (233.5, {1, 2}), (235, {0, 1, 2}), (236.5, {1, 2, 3}), (238, set())]
def flips4(r, c):
    k = r * 2 + c; ts = []; back = False
    for b, st in WIN4:
        nb = k in st
        if nb != back: ts.append(b); back = nb
    return ts
FIRES4 = [(234.5, -20, -20, 300), (234.5, W + 20, -20, 300), (234.5, -20, H + 20, 300), (234.5, W + 20, H + 20, 300)]
photo(229, 239.25, [V], HH, tears4, flips4, fires=FIRES4, rot=[0.03, -0.03, -0.03, 0.03])
for i, (b, x, y) in enumerate([(235.5, 640, 360), (236.5, 520, 300), (237.5, 760, 420), (238.5, 640, 360)]):
    drop(b, x, y, R=64, ang=0.7 * i, hold=0.9)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
en_ = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L19', 'name': 'المرحلة التاسعة عشرة', 'song': 'عافني وراح', 'file': 'SONG_L19', 'bpm': 99.915, 'off': 0.344, 'end': 239.25, 'cps': [0, 50, 166],
     'ez': {'ezg': 1, 'ezc': 1, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.3}, 'energy': en_['energy'], 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV))
