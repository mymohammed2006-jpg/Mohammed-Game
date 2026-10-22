#!/usr/bin/env python3
# Makes data/dodge_H1.json: Hardcore level 1 "Clockwork Squares" (Black Ops Zombies), the cursed clock of iron squares.
# 100 BPM, beat 0 = 0.001 s, a beat = 0.6 s, a bar = 4 beats. Song map (beats): intro 0-11 | hats 12-43 | main 44-83 (0:26.4)
# | bigger 84-111 (0:50.4) | 112-147 (1:07.2) | 148-175 (1:28.8) | last 176-188 (1:45.6) | win 189 (1:53.4).
# Parts: 1 the machine starts (0-83), 2 the clock strikes (84-147), 3 midnight (148-189). Checkpoints 0, 84, 148.
# Hardcore: Normal only (no Easy), 3 hearts. Nothing here is random: every attack is a fixed pattern on the beat, except the lines
# marked aim (the square's own row or column) which have "rnd" so the bot only sees them when they fire.
import json, math, os, random

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_H1.json')
W, H = 1280, 720
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b, k, **kw):
    d = {'b': round(max(0.0, b), 4), 'k': k}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
# an event that starts showing at hit - wn fires a quarter beat before that
def at(hit, wn, k, **kw): ev(hit - wn - 0.25, k, hit=hit, wn=wn, **kw)
# the backdrop goes in first (before the part's first attacks, so it is drawn under them) and shows from b0
def stage(part, b0, b1): ev(b0 - 3, 'H1stage', part=part, at=b0, to=b1, keep=True)
def fx(b, v=0.35, s=10, dur=1.5): ev(b, 'H1fx', v=v, s=s, dur=dur)

# the grids: the screen in 8 x 5 cells of 160 x 144; the clock face 5 x 5 cells of 144 in the middle; the box 16 x 9 cells of 80
G8 = dict()
FACE = dict(gx=280, gy=0, gw=144, gh=144, gc=5, gr=5)
G16 = dict(gx=0, gy=0, gw=80, gh=80, gc=16, gr=9)
def cx(c): return 80 + 160 * c
def cy(r): return 72 + 144 * r

# ================= part 1: the machine starts (0-83) =================
stage(1, 0, 84)

# 0:00 the squares that jump a cell every beat (their next place is a ghost). Four go round the edge, three round the inner ring the
# other way, one runs up and down the middle row; from 0:03 they all jump every half beat
ring = [(c, 0) for c in range(8)] + [(7, r) for r in range(1, 5)] + [(c, 4) for c in range(6, -1, -1)] + [(0, r) for r in range(3, 0, -1)]
inner = [(c, 1) for c in range(1, 7)] + [(6, 2), (6, 3)] + [(c, 3) for c in range(5, 0, -1)] + [(1, 2)]
B0, NB = 1, 22   # a jump every beat to beat 5, then every half beat (steps: beats 1..5 then 5.5, 6 .. 12)
def jump(cells):
    # the half-beat steps: the data is per half beat, a slow beat is the same place twice
    at(2, 2, 'H1jump', b0=B0, st=0.5, xs=[cx(c) for c, r in cells], ys=[cy(r) for c, r in cells], sz=136, to=12.6)
def steps(nxt):
    out = []; p = None
    for k in range(NB):
        b = B0 + 0.5 * k
        if p is None or b >= 5 or k % 2 == 0: p = nxt()
        out.append(p)
    return out
for s0 in (0, 6, 11, 17):
    it = iter([ring[(s0 + j) % len(ring)] for j in range(99)]); jump(steps(lambda: next(it)))
for s0 in (2, 6, 10):
    it = iter([inner[(s0 - j) % len(inner)] for j in range(99)]); jump(steps(lambda: next(it)))
mid = [2, 3, 4, 5, 4, 3]
it = iter([(mid[j % 6], 2) for j in range(99)]); jump(steps(lambda: next(it)))

# 0:07.2 the sliding grid: a row or column slides a cell on every beat (all of it kills while it slides, 0.35 beat).
# Rows top to bottom, then the columns run left to right every half beat (cross behind the one that just slid),
# then your own row and column (aimed), then two lines at once
ev(11, 'H1board', to=30, **G8)
def slide(hit, axis, idx, d, wn=0.55, aim=False, dur=0.45, g=G8):
    kw = dict(axis=axis, idx=idx, dir=d, dur=dur, **g)
    if aim: kw['aim'] = True; kw['rnd'] = True
    at(hit, wn, 'H1slide', **kw)
fx(12, 0.15, 6, 1.0)
for i, r in enumerate([0, 1, 2, 3, 4, 3, 2, 1, 0, 2]): slide(13 + 0.5 * i, 0, r, 1 if i % 2 == 0 else -1)
for i in range(8): slide(18 + 0.5 * i, 1, i, 1 if i % 2 == 0 else -1)
for i in range(8): slide(22.25 + 0.5 * i, 1, 7 - i, -1 if i % 2 == 0 else 1)
for k in range(26, 30):
    slide(k, 0, 0, 1 if k % 2 else -1, wn=0.75, aim=True)
    slide(k + 0.5, 1, 0, 1 if k % 2 else -1, wn=0.75, aim=True)
for k, (a, b2) in enumerate([(0, 3), (1, 4), (0, 2)]): slide(26.25 + k, 0, a, 1, dur=0.3); slide(26.25 + k, 0, b2, -1, dur=0.3)

# 0:16.8 the countdown bombs: a bomb lands every beat and blasts its row and column 2 beats later (from 0:20.4 one every half beat)
bomb_cells = [(1, 1), (6, 3), (3, 0), (4, 4), (0, 2), (7, 1), (2, 3), (5, 0), (3, 4), (6, 2), (1, 0), (4, 1), (7, 4), (2, 2),
              (5, 3), (0, 4), (6, 0), (3, 2), (1, 3), (4, 0), (7, 2), (2, 4), (5, 1), (0, 0)]
def bomb(land, hit, c, r, g=G8, hold=0.4):
    ev(land - 0.75, 'H1bomb', land=land, hit=hit, c=c, r=r, hold=hold, **g)
k = 0
for b in range(30, 34): c, r = bomb_cells[k]; bomb(b, b + 2, c, r, hold=0.45); k += 1
for i in range(20): c, r = bomb_cells[k % len(bomb_cells)]; b = 34 + 0.5 * i; bomb(b, b + 2, c, r, hold=0.45); k += 1

# 0:26.4 the press: the walls slam (every 1.25 beats, then every beat) and leave only a gap (its next place is outlined). First from the sides (a column),
# then from all four sides (a square hole) on every beat. The gaps are 200 x 180 and 200-260 px apart: a dash nearly every beat
gxs = [640, 400, 640, 900, 700, 500, 640, 860, 1000, 800, 600, 400, 300, 480, 680, 880, 740]
gys = [360, 360, 360, 360, 220, 340, 500, 420, 260, 180, 300, 220, 420, 540, 440, 300, 500]
mds = [0, 0, 0, 0] + [1] * 13
hs = [46 + 1.25 * i for i in range(4)] + [51 + i for i in range(13)]
fx(44, 0.2, 8, 1.0)
ev(43.5, 'H1press', hs=hs, gx=gxs, gy=gys, md=mds, gw=200, gh=180, cl=0.2, hold=0.45, tw=44)
# between the slams the squares jump round the edge every half beat (four of them, two each way)
def jumpline(b0, cells, hit, to): ev(hit - 1.25, 'H1jump', hit=hit, wn=1, b0=b0, st=0.5, xs=[cx(c) for c, r in cells], ys=[cy(r) for c, r in cells], sz=136, to=to)
for s0, d in ((3, 1), (14, -1), (9, 1), (20, -1)):
    jumpline(47, [ring[(s0 + d * k) % len(ring)] for k in range(38)], 48, 65)

# 0:38.4 the square wave: a channel between two stepped walls runs across the screen; each step moves the channel up or down 160 px
# (the old and the new channel share 28 px: be there when the step comes). A step every half beat; it turns round every 3-4 beats
SEGW = 320
cent = [600, 440, 280, 120, 280, 440]
top = [c - 94 for c in cent]; bot = [c + 94 for c in cent]
V = SEGW / 0.5
fx(64, 0.25, 8, 1.0)
sb = [0, 69, 73, 77, 80, 83]
at(65, 1.5, 'H1wave', top=top, bot=bot, segw=SEGW, x0=0, sb=sb, sv=[(-V if i % 2 == 0 else V) for i in range(len(sb))], to=84)

# ================= part 2: the clock strikes (84-147) =================
stage(2, 84, 148)
fx(84, 0.35, 12, 1.5)

# the clock face: find a board whose safe cells before and after each turn are few and far from the last ones
N = 5
def rot(c, r, q):
    for _ in range(q): c, r = N - 1 - r, c
    return c, r
def deadly(lay, q):
    out = set()
    for i in lay:
        c, r = rot(i % N, i // N, q); out.add(r * N + c)
    return out
def conn(free, a, b):
    seen = set(a); st = list(a)
    while st:
        i = st.pop()
        if i in b: return True
        c, r = i % N, i // N
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nc, nr = c + dc, r + dr
            j = nr * N + nc
            if 0 <= nc < N and 0 <= nr < N and j in free and j not in seen: seen.add(j); st.append(j)
    return False
ALL = set(range(25))
# how bad a board is: no safe cell at a turn, too many, the same ones as the turn before, too close, or no way there
def face_pen(lay, qs, hi):
    qp = 0; S = []; p = 0
    for q in qs:
        both = (ALL - deadly(lay, qp)) & (ALL - deadly(lay, q))
        if not both: p += 100
        p += max(0, len(both) - hi) * 5
        if S:
            p += len(both & S[-1]) * 6
            if both and S[-1] and min(abs(a % N - b % N) + abs(a // N - b // N) for a in both for b in S[-1]) < 2: p += 2
            if both and S[-1] and not conn(ALL - deadly(lay, qp), S[-1], both): p += 10
        S.append(both); qp = q
    return p, S
# the middle cell never moves when the board turns, so it is always deadly
def face_board(qs, hi, lo_n, hi_n, seed, iters=200000):
    rng = random.Random(seed); lay = {12} | set(rng.sample(range(25), (lo_n + hi_n) // 2 - 1)); cur, _ = face_pen(lay, qs, hi); best = (cur, sorted(lay)); T = 5.0
    for it in range(iters):
        nl = set(lay); i = rng.choice([x for x in range(25) if x != 12])
        if i in nl: nl.remove(i)
        else: nl.add(i)
        if not (lo_n <= len(nl) <= hi_n): continue
        p, _ = face_pen(nl, qs, hi)
        if p <= cur or rng.random() < math.exp((cur - p) / T): lay, cur = nl, p
        if cur < best[0]: best = (cur, sorted(lay))
        T = max(0.05, T * 0.99997)
        if cur == 0: break
    return best[1], face_pen(set(best[1]), qs, hi)
qs1 = [1, 3, 0, 1, 2, 0, 3, 1, 2, 0]
lay1, (P1, S1) = face_board(qs1, 4, 9, 14, 1)
tt1 = [88 + k for k in range(10)]
# the board as it will stand after the next turn shows 0.7 beat before it
ev(83.5, 'H1face', hit=86, wi=2, wn=0.7, lay=lay1, tt=tt1, qs=qs1, q0=0, walls=True, to=98.5, **FACE)
print('face 1', lay1, 'penalty', P1, [sorted(s) for s in S1])
# the face turns, and the dominoes stand up round it

# 0:58 the dominoes: waves of falling tiles (each kills only while it falls, 0.3 beat; the next tile falls half a beat later).
# Running ahead of a wave does not save you: go into it and cross behind the falling line
waves = [  # (start beat, sources, beats per cell)
    (99.5, [(0, 2)], 0.4),
    (101.5, [(7, 0), (7, 4)], 0.4),
    (103.5, [(3, 2), (4, 2)], 0.4),
    (105, [(c, 0) for c in range(8)], 0.4),
    (106.5, [(0, 4), (7, 4)], 0.4),
    (108, [(7, r) for r in range(5)], 0.4),
    (109.5, [(0, 0), (7, 0)], 0.4),
    (111, [(0, r) for r in range(5)], 0.4),
]
ft = []
for i in range(40):
    c, r = i % 8, i // 8
    ft.extend(b0 + rate * min(abs(c - sc) + abs(r - sr) for sc, sr in src) for b0, src, rate in waves)
ev(99, 'H1domino', ft=ft, nk=len(waves), hold=0.3, wn=0.75, to=112.5)

# 1:07.2 the knights: five of them leap in L shapes; where one lands, the 3 x 3 round it blasts. A landing every quarter beat
def knight_paths(starts, n, seed):
    moves = [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)]
    pos = [list(s) for s in starts]; paths = [[tuple(s)] for s in starts]
    rng = random.Random(seed)
    for k in range(n):
        for i in range(len(pos)):
            c, r = pos[i]
            best = None
            for j, (dc, dr) in enumerate(moves):
                nc, nr = c + dc, r + dr
                if not (0 <= nc < 8 and 0 <= nr < 5): continue
                others = [tuple(p) for o, p in enumerate(pos) if o != i]
                d = min(max(abs(nc - oc), abs(nr - orr)) for oc, orr in others)
                # spread out, and do not go back where it just was
                back = len(paths[i]) > 1 and paths[i][-2] == (nc, nr)
                sc = d * 10 - back * 30 + rng.random()
                if best is None or sc > best[0]: best = (sc, nc, nr)
            pos[i] = [best[1], best[2]]; paths[i].append((best[1], best[2]))
    return paths
kp = knight_paths([(1, 1), (6, 3), (6, 1), (1, 3), (3, 2), (4, 0)], 10, 3)
for i, path in enumerate(kp):
    tj = [113 + i / 4 + 1.5 * k for k in range(len(path))]
    ev(111.5, 'H1knight', cs=[p[0] for p in path], rs=[p[1] for p in path], tj=tj, hold=0.4, jd=0.6, tw=112)

# 1:16.8 the conveyor lanes: five belts of iron blocks, slow and fast in turn; on every chime (4 beats) each belt turns round and the
# slow ones become fast. Ride a slow belt's gap, cross to the next slow one when the chime comes
fx(128, 0.2, 8, 1.0)
lanes = [(+250, 380, 190), (-470, 400, 190), (+280, 390, 195), (-490, 410, 190), (+260, 385, 190)]
chimes = [128, 132, 136, 140, 144]
for r, (v0, per, bw) in enumerate(lanes):
    sv = []
    for k, cb in enumerate(chimes):
        slow = (r % 2 == 0) == (k % 2 == 0)
        mag = (340 + 15 * k) if slow else (600 + 20 * k)
        sgn = (1 if v0 > 0 else -1) * (1 if k % 2 == 0 else -1)
        sv.append(sgn * mag)
    at(129, 1.5, 'H1lane', y0=144 * r, lh=144, bw=bw, per=per, ph=97 * r, sb=chimes, sv=sv, to=147.6)

# ================= part 3: midnight (148-189) =================
stage(3, 148, 190)
fx(148, 0.4, 12, 1.5)

# 1:28.8 the 12 chimes: on each beat one side of the box closes in by 80 px (top, right, bottom, left, round and round) and stays.
# Inside, the box's lines slide every half beat (80 px wide). At the 12th the box bursts open
ct = [149 + k for k in range(12)]; cs = [k % 4 for k in range(12)]
ev(148, 'H1shrink', ct=ct, cs=cs, wn=1.0, tx=161, **G16)
# the box after k chimes: cells [L, 16-R) x [T, 9-B)
def box(k):
    T = sum(1 for j in range(k) if cs[j] == 0); R = sum(1 for j in range(k) if cs[j] == 1); Bm = sum(1 for j in range(k) if cs[j] == 2); L = sum(1 for j in range(k) if cs[j] == 3)
    return L, 16 - R, T, 9 - Bm
for i in range(20):
    hit = 150 + 0.5 * i; k = sum(1 for c in ct if c <= hit); L, R, T, Bm = box(k)
    if i >= 10: slide(hit, i % 2, 0, 1 if i % 4 < 2 else -1, wn=0.9, aim=True, dur=0.45, g=G16); continue
    if i % 2 == 0:
        idx = T + (i // 2) % max(1, Bm - T); slide(hit, 0, idx, 1 if i % 4 == 0 else -1, wn=0.6, dur=0.45, g=G16)
        slide(hit, 1, L + (i * 5 + 3) % max(1, R - L), -1, wn=0.6, dur=0.45, g=G16)
    else: idx = L + (i * 3) % max(1, R - L); slide(hit, 1, idx, 1 if i % 4 == 1 else -1, wn=0.6, dur=0.45, g=G16)
fx(161, 0.5, 14, 1.2)

# 1:36.6 the stack: iron blocks fall into the columns every half beat and pile up; a full row blasts (with the row over it) and the
# blocks above drop a row
order = [0, 0, 7, 7, 1, 1, 6, 6, 0, 7, 2, 5, 2, 5, 1, 6, 3, 4, 3, 4, 2, 5, 3, 4]
FALL, HOLD, CLR = 0.35, 0.35, 1.0
height = [0] * 8; blocks = []; clears = []
cells = {}   # (col, row) -> block index
t = 162.0
for j, c in enumerate(order):
    bs = t; land = t + FALL; row = 4 - height[c]
    blocks.append({'c': c, 'bs': bs, 'keys': [(land, row)], 'bd': 999.0}); cells[(c, row)] = j; height[c] += 1
    # a full row?
    for rr in range(5):
        if all((cc, rr) in cells for cc in range(8)):
            tc = land + CLR; clears.append((rr, tc))
            for cc in range(8): blocks[cells.pop((cc, rr))]['bd'] = tc
            # the blocks over it drop a row after the blast
            moved = {}
            for (cc, r2), bj in sorted(cells.items(), key=lambda kv: -kv[0][1]):
                if r2 < rr: blocks[bj]['keys'].append((tc + HOLD * 0.6, r2 + 1)); moved[(cc, r2 + 1)] = bj
                else: moved[(cc, r2)] = bj
            cells = moved
            for cc in range(8): height[cc] = sum(1 for (c3, r3) in cells if c3 == cc)
    t += 0.4
for b in blocks: b['bd'] = min(b['bd'], 176.5)
ko = [0]; kt = []; kr = []
for b in blocks:
    for tt_, rr in b['keys']: kt.append(tt_); kr.append(rr)
    ko.append(len(kt))
ev(161, 'H1stack', bc=[b['c'] for b in blocks], bs=[b['bs'] for b in blocks], ko=ko, kt=kt, kr=kr, bd=[b['bd'] for b in blocks],
   cr=[c[0] for c in clears], ctm=[c[1] for c in clears], hold=HOLD, wn=1.0, to=176.5)
print('stack clears', clears, 'rows used', sorted({rr for b in blocks for _, rr in b['keys']}))

# 1:45.6 the whole machine: the clock face again (lighter board) turning every 2 beats, countdown bombs on it every beat (2 beats
# each), and its lines sliding at you every beat
qs2 = [3, 1, 2, 0, 1]
lay2, (P2, S2) = face_board(qs2, 6, 8, 10, 2)
tt2 = [180, 182, 184, 186, 188]
ev(175.5, 'H1face', hit=178, wi=2, wn=1.25, lay=lay2, tt=tt2, qs=qs2, q0=0, walls=True, to=188.6, **FACE)
print('face 2', lay2, 'penalty', P2, [sorted(s) for s in S2])
fb = [(1, 1), (3, 3), (0, 4), (4, 0), (2, 2), (1, 3), (3, 1), (0, 0)]
for i, (c, r) in enumerate(fb): bomb(179 + i, 181 + i, c, r, g=FACE, hold=0.35)
for i in range(6): slide(181.5 + i, i % 2, 0, 1 if i % 2 else -1, wn=1.1, aim=True, dur=0.35, g=FACE)
ev(188, 'H1burst', t1=188.6)
fx(189, 0.6, 16, 2.0)

# ---------------- out ----------------
EV.sort(key=lambda d: d['b'])
en = json.load(open(os.path.join(HERE, 'energy.json')))['energy']
D = {'id': 'H1', 'name': 'هاردكور: المرحلة الأولى', 'song': 'Clockwork Squares', 'file': 'SONG_H1', 'bpm': 100, 'off': 0.001, 'end': 189, 'cps': [0, 84, 148],
     'ez': {}, 'energy': en, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), 'rnd', sum(1 for d in EV if d.get('rnd')))
