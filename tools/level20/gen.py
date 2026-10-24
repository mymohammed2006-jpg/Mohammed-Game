#!/usr/bin/env python3
# Makes data/dodge_L20.json: L20 "New Game" (Nitro Fun), you started a new game. 128 BPM, beat 0 = 0.59075 s, a beat = 0.46875 s.
# Song map (beats): intro, no bass 0-31 | bass 32-63 (0:15.6) | crash 64, the quiet 65-79 (0:30.6) | the build 80-111 (0:38.1, lifts 96, 104)
# | DROP 112-140 (0:53.1) | break 141-143 | 144-172 (1:08.1) | the near silence 173-175 | 176-204 (1:23.1) | break 205-207 | 208-236 (1:38.1)
# | break 237-239 | the last hit 240 (1:53.1) | win 242 (1:54.0)
# Parts: 1 switching on (0-111), 2 playing (112-175), 3 the boss (176-242). Checkpoints 0, 112, 176.
# Nothing here is random: every attack is a fixed pattern on the beat (the Pong shot and the boss's laser aim at the square: "rnd").
# "nz": Normal only, "ez": Easy only, where the two have their own patterns.
import json, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'data', 'dodge_L20.json')
W, H = 1280, 720
EV = []

def r4(v): return round(v, 4) if isinstance(v, float) else v
def ev(b_, kind_, **kw):
    d = {'b': round(b_, 4), 'k': kind_}
    for a, v in kw.items(): d[a] = [r4(x) for x in v] if isinstance(v, (list, tuple)) else r4(v)
    EV.append(d)
def fx(b, v=0.3, s=10, dur=1.5): ev(b, 'L20fx', v=v, s=s, dur=dur)
def both(fn):
    # fn(easy) -> list of (b, k, kw); adds the Normal one ("nz") and the Easy one ("ez")
    for ez in (False, True):
        for b, kd, kw in fn(ez): ev(b, kd, **({'ez': True} if ez else {'nz': True}), **kw)

ev(0, 'L20rot', rb=[0, 204, 205, 207.5, 236], ra=[0, 0, 0, 180, 720], keep=True)
ev(0, 'L20stage', to=244, keep=True)

# ================= part 1: switching on (0-111) =================
# 0:00 the start menu: the selector jumps two rows on every beat (it can jump over you).
# Every 4 beats a "select" line crosses the screen and only its gap row is safe (from 16 two lines, from both sides). The gaps are
# picked so a person can always get there
def menu(ez):
    # the cursor jumps two rows a beat (0 2 4 1 3 ...), so it can jump over you; the second one mirrors it
    sb = list(range(2, 32)); sr = [(2 * (b - 2)) % 5 for b in sb]
    qb, qr = [], []
    cb, cs, cg = [], [], []
    # where a person can be at each beat (rows): a selector's row is closed for its beat; a step is one row a beat;
    # during a scan (c .. c+cd) you stay in its gap. The gaps are picked so a path always exists, far from the last one
    def busy(b):
        out = set()
        if b in sb: out.add(sr[sb.index(b)])
        if b in qb: out.add(qr[qb.index(b)])
        return out
    reach = {2}; scans = list(range(4, 30, 8)) if ez else [4, 8, 10, 12, 14, 17, 20, 23, 26, 29]; last = 2
    DBL = set() if ez else {17, 23, 29}
    for b in range(2, 32):
        if b in scans:
            i = len(cb)
            for _ in range(2):
                cand = []
                for g in range(5):
                    if g not in reach: continue
                    dbl = b in DBL
                    if any(g in busy(x) for x in range(b, b + (3 if dbl else 2))): continue
                    cand.append(g)
                # no room for two lines here: one line
                if cand or b not in DBL: break
                print('menu: one line at', b); DBL.discard(b)
            if not cand: print('menu: no scan at', b); scans.remove(b)
            else:
                g = min(cand, key=lambda r: abs(r - last)) if i == 0 else max(cand, key=lambda r: (abs(r - last), (r * 7 + i) % 5))
                cb.append(b); cs.append(1 if i % 2 == 0 else -1); cg.append(g); last = g
                # from 16 (Normal) two lines at once, from both sides
                if b in DBL: cb.append(b); cs.append(-cs[-1]); cg.append(g)
        # the next beat's rows
        # stay in the gap while the line can still reach you (one line: stand on its starting side and it has passed you by the next beat)
        stay = [x for x in cb if x <= b < x + (2 if x in DBL else 1)]
        if stay: reach = {cg[cb.index(stay[0])]}; continue
        nxt = set()
        for a in reach:
            for d in (-1, 0, 1):
                r = a + d
                if 0 <= r < 5 and r not in busy(b) and a not in busy(b) and r not in busy(b + 1): nxt.add(r)
        reach = nxt or reach
    return [(0, 'L20menu', dict(sb=sb, sr=sr, qb=qb, qr=qr, cb=cb, cs=cs, cg=cg, cd=2.4 if ez else 1.6, tw=0.5, ta=2, te=32))]
both(menu)

# 0:15 Tetris (the bass comes in): pieces fall in steps and turn a quarter every beat, stack up and the full rows clear (a laser
# across a beat before). Faster from 48. At 64 (the crash) the whole stack goes at once
SH = [[0, 1, 1, 1, 2, 1, 3, 1], [1, 1, 2, 1, 1, 2, 2, 2], [0, 1, 1, 1, 2, 1, 1, 0], [1, 0, 2, 0, 0, 1, 1, 1], [0, 0, 1, 0, 1, 1, 2, 1], [0, 0, 0, 1, 1, 1, 2, 1], [2, 0, 0, 1, 1, 1, 2, 1]]
SC = [1.5, 1.5, 1, 1, 1, 1, 1]
def turn(s, r):
    c = SC[s]; out = []
    for j in range(4):
        x, y = SH[s][2 * j], SH[s][2 * j + 1]
        for _ in range(r % 4): x, y = 2 * c - y, x
        out.append((int(round(x)), int(round(y))))
    return out
COLS, ROWS = 16, 9

def norm(cs):
    mx = min(c for c, r in cs); my = min(r for c, r in cs); return sorted((c - mx, r - my) for c, r in cs)
def match(target):
    # the shape, turn and box place that make exactly these cells
    for s in range(7):
        for r in range(4):
            cs = turn(s, r)
            if norm(cs) == norm(target):
                dx = min(c for c, _ in target) - min(c for c, _ in cs); dy = min(r2 for _, r2 in target) - min(r2 for _, r2 in cs)
                return s, r, dx, dy
    raise Exception('no shape for %s' % target)
LAYOUTS = [[4, 2, 4, 2, 4], [2, 4, 4, 4, 2], [4, 4, 2, 2, 4], [2, 2, 4, 4, 4], [4, 2, 2, 4, 4], [4, 4, 4, 2, 2]]
def band_pieces(li):
    # a band (2 rows, base row b): the pieces as cell sets relative to the band's bottom row (0) and the row above (-1), in landing order
    blocks = []; c = 0
    for j, w in enumerate(LAYOUTS[li % len(LAYOUTS)]):
        if w == 2: blocks.append([[(c, -1), (c + 1, -1), (c, 0), (c + 1, 0)]])
        elif (j + li) % 2 == 0: blocks.append([[(c + i, 0) for i in range(4)], [(c + i, -1) for i in range(4)]])
        else: blocks.append([[(c + 1, 0), (c + 2, 0), (c + 3, 0), (c + 3, -1)], [(c, -1), (c + 1, -1), (c + 2, -1), (c, 0)]])
        c += w
    # first the bottom pieces of all blocks, then the tops, far apart one after the other
    order = sorted(range(len(blocks)), key=lambda j: (j % 2, j if j % 2 == 0 else -j))
    out = [blocks[j][0] for j in order] + [blocks[j][1] for j in order if len(blocks[j]) > 1]
    return out
def tetris(ez):
    if ez: spawns = [(32 + 2 * i, 0.5) for i in range(8)] + [(48 + i, 0.5) for i in range(14)]
    else: spawns = [(32 + i, 0.25) for i in range(8)] + [(40 + i, 0.125) for i in range(8)] + [(48 + 0.5 * i, 0.125) for i in range(27)]
    # give the pieces to bands in turn
    plan = []; li = 0
    while len(plan) < len(spawns):
        for cs in band_pieces(li): plan.append((li, cs))
        li += 1
    nbands = li; bandsize = {}
    for bi, cs in plan[:len(spawns)]: bandsize[bi] = bandsize.get(bi, 0) + 1
    full = {bi: len(band_pieces(bi)) for bi in range(nbands)}
    cleared = {}            # band -> clear beat
    locked = {}             # band -> pieces locked
    def base(bi, t):
        # the band's bottom row at beat t: 8, minus 2 for every band below it not cleared yet
        return 8 - 2 * sum(1 for bj in range(bi) if not (bj in cleared and cleared[bj] <= t + 1e-9))
    pieces = []; cells = []; clears, clrows = [], []
    active = []
    t = 32.0; si = 0
    while t < 64.75:
        # clears due now: the cells above drop two rows
        for bi, cb in list(cleared.items()):
            if abs(cb - t) < 1e-9:
                for cell in cells:
                    if cell['band'] > bi and cell['end'] > t: cell['kf'].append((t, cell['kf'][-1][1] + 2))
                    elif cell['band'] == bi: cell['end'] = t
        if abs(t - 64.0) < 1e-9:
            rows = sorted({cell['kf'][-1][1] for cell in cells if cell['end'] > 64})
            for rr in rows: clears.append(64.5); clrows.append(rr)
            for cell in cells:
                if cell['end'] > 64: cell['end'] = 64.5
        while si < len(spawns) and abs(spawns[si][0] - t) < 1e-9:
            bi, cs = plan[si]; s, rf, dx, dy = match(cs); step = spawns[si][1]
            # the box starts so its lowest cell is just above the screen; it turns every beat, so it starts rf - turns
            top = -1 - max(r2 for _, r2 in turn(s, rf))
            active.append(dict(bi=bi, cs=cs, s=s, rf=rf, dx=dx, dy=dy, y=top, sb=t, step=step, next=t + step, states=None))
            si += 1
        for p in list(active):
            if abs(p['next'] - t) > 1e-9: continue
            p['y'] += 1; p['next'] = t + p['step']
            if p['y'] >= p['dy'] + base(p['bi'], t): p['y'] = p['dy'] + base(p['bi'], t); p['lock'] = t; active.remove(p); pieces.append(p)
        t = round(t + 0.125, 4)
        # a band is full: it clears a beat after its last piece locked
        for p in pieces:
            if 'counted' in p: continue
            p['counted'] = True; locked[p['bi']] = locked.get(p['bi'], 0) + 1
            row0 = base(p['bi'], p['lock'])
            for c, r2 in p['cs']: cells.append(dict(col=c, band=p['bi'], kf=[(p['lock'], row0 + r2)], end=999))
            if locked[p['bi']] == full[p['bi']] and p['lock'] < 63:
                cleared[p['bi']] = p['lock'] + 1; clears += [p['lock'] + 1, p['lock'] + 1]; clrows += [row0 - 1, row0]
    for p in active: p['lock'] = 64.5; pieces.append(p)
    # the falling states: the piece turns every beat (its last turn is rf), steps down every step
    pz = []
    for p in pieces:
        nt = int(math.floor(p['lock'] - p['sb'] + 1e-9)); r0 = p['rf'] - nt
        y = -1 - max(r2 for _, r2 in turn(p['s'], p['rf'])); st = []
        tt = p['sb']; yy = y
        times = sorted({round(p['sb'] + k * p['step'], 4) for k in range(int((p['lock'] - p['sb']) / p['step']) + 2) if p['sb'] + k * p['step'] <= p['lock'] + 1e-9} | {float(b) for b in range(int(math.ceil(p['sb'])), int(math.floor(p['lock'])) + 1) if b > p['sb']})
        for tt in times:
            yy = y + int(math.floor((tt - p['sb']) / p['step'] + 1e-9)); rr = r0 + int(math.floor(tt - p['sb'] + 1e-9))
            if tt >= p['lock'] - 1e-9: break
            st.append((tt, [(p['dx'] + a, yy + b) for a, b in turn(p['s'], rr)]))
        # the lock state: where it really landed
        row0 = base(p['bi'], p['lock']) if p['lock'] < 64.5 else None
        last = [(c, row0 + r2) for c, r2 in p['cs']] if row0 is not None else st[-1][1]
        st.append((p['lock'], last))
        pz.append(len(st))
        for b, cs in st:
            pz.append(b)
            for c in cs: pz += [c[0], c[1]]
    sk = []
    for cell in cells:
        sk += [cell['col'], len(cell['kf'])]
        for b, r in cell['kf']: sk += [b, r]
        sk.append(min(cell['end'], 64.5))
    print(('Easy' if ez else 'Normal') + f' tetris: {len(pieces)} pieces, bands cleared {len(cleared)}, cells {len(cells)}')
    return [(30.5, 'L20tetris', dict(pz=pz, sk=sk, cl=clears, clr=clrows, tw=31, te=64.6))]
both(tetris)
fx(32, 0.12, 8, 1.0)

# 0:30.6 the quiet: the loading ring breathes round the middle (only its gap is safe, and the gap turns back every 4 beats), the icon in the
# middle burns, and two loading bars cross the screen (from the left from 66, from the right from 70): go through their holes
HOLES_A = [360, 250, 170, 260, 400, 530, 600, 480, 340, 210, 150, 280, 430, 560]
HOLES_B = [200, 330, 470, 580, 470, 330, 280, 380, 420, 330]
def loading(ez):
    out = [(62.5, 'L20front', dict(x0=40, dir=1, sx=80, hy=HOLES_A, hh=64 if ez else 52, tw=65, ta=66))]
    out.append((62.5, 'L20front', dict(x0=1240, dir=-1, sx=80, hy=HOLES_B, hh=64 if ez else 52, tw=69 if ez else 67, ta=70 if ez else 68)))
    out.append((63, 'L20ring', dict(rmin=60, rmax=780, per=2, th=30, gw=46 if ez else 40, w=1.2, rv=4, a0=-90, core=80, tw=63, ta=65, te=80)))
    return out
both(loading)
fx(64, 0.18, 10, 1.2)

# 0:38.1 Pong: a ball between two paddles, its next lines dashed. It splits in two at 96 and in four at 104 (both on the left paddle),
# and at 110 they all stop... then fly at you at 111
def bounce(x0, y0, x1, y1, m, R):
    # the line from (x0,y0) to (x1,y1) off the top and bottom walls m times (down first from the upper half): its corners
    lo, hi = R, H - R; span = hi - lo; down = y0 < H / 2
    def fold(y):
        u = (y - lo) % (2 * span); return lo + (u if u < span else 2 * span - u)
    Ye = y1 if m == 0 else ((2 * hi - y1) if down else (2 * lo - y1)) if m == 1 else (y1 + 2 * span if down else y1 - 2 * span)
    pts = [(x0, y0)]
    j0, j1 = sorted([(y0 - lo) / span, (Ye - lo) / span])
    bs = [lo + j * span for j in range(math.floor(j0) + 1, math.ceil(j1)) ]
    bs.sort(key=lambda Yb: abs(Yb - y0))
    for Yb in bs: u = (Yb - y0) / (Ye - y0); pts.append((x0 + (x1 - x0) * u, fold(Yb)))
    pts.append((x1, y1)); return pts
def pong(ez):
    R = 22 if ez else 30; XL, XR = 60, W - 60
    # ball A's paddle hits: (beat, side, y, bounces on the way there)
    A = [(80, 'L', 360, 0), (84, 'R', 200, 1), (88, 'L', 520, 1), (92, 'R', 560, 0), (96, 'L', 180, 2), (100, 'R', 440, 1), (104, 'L', 300, 1), (107, 'R', 620, 1), (110, 'L', 260, 0)]
    def path(hits):
        kb, kx, ky = [], [], []
        for i, (b, sd, y, m) in enumerate(hits):
            x = XL if sd == 'L' else XR
            if i == 0: kb.append(b); kx.append(x); ky.append(y); continue
            pb, psd, py, _ = hits[i - 1]; px = XL if psd == 'L' else XR
            pts = bounce(px, py, x, y, m, R)
            L = [0]
            for j in range(1, len(pts)): L.append(L[-1] + math.hypot(pts[j][0] - pts[j - 1][0], pts[j][1] - pts[j - 1][1]))
            for j in range(1, len(pts)): kb.append(pb + (b - pb) * L[j] / L[-1]); kx.append(pts[j][0]); ky.append(pts[j][1])
        return kb, kx, ky
    balls = [A]
    # B splits from A at 96: the same beats, mirrored up and down
    B_ = [(96, 'L', 180, 0)] + [(b, s, H - y, m) for (b, s, y, m) in A if b > 96]
    balls.append(B_)
    if not ez:
        # C and D split at 104: the same beats, other heights
        C = [(104, 'L', 300, 0), (107, 'R', 140, 0), (110, 'L', 520, 1)]
        D = [(104, 'L', H - 300, 0), (107, 'R', H - 140, 0), (110, 'L', 200, 1)]  # from where B is at 104
        balls += [C, D]
    nk, KB, KX, KY = [], [], [], []
    for hb in balls:
        kb, kx, ky = path(hb); nk.append(len(kb)); KB += kb; KX += kx; KY += ky
    lb = [b for (b, s, y, m) in A if s == 'L']; ly = [y for (b, s, y, m) in A if s == 'L']
    rb = [b for (b, s, y, m) in A if s == 'R']; ry = [y for (b, s, y, m) in A if s == 'R']
    # the shot: the balls where they stopped
    bx = [XL] * len(balls); by = [hb[-1][2] for hb in balls]
    return [(78.5, 'L20pong', dict(nk=nk, kb=KB, kx=KX, ky=KY, R=R, pad=True, lb=lb, ly=ly, rb=rb, ry=ry, tw=79, ta=80, te=110.5)),
            (109, 'L20shot', dict(bx=bx, by=by, R=R, sp=850 if ez else 1300, f0=110, f1=111, rnd=True))]
both(pong)
fx(80, 0.1, 8, 1.0); fx(96, 0.08, 6, 0.8); fx(104, 0.1, 8, 0.8)

# ================= part 2: playing (112-175) =================
# 0:53.1 the drop - Space Invaders: 10 x 4 invaders step side to side on the beat and come down a row at each turn (twice as fast from 128).
# The lowest one of a column fires down on the beat (its column shows a beat before). The saucer crosses at 128 and drops bombs.
# At 136 the lower two rows dive out in curves, at 138 the upper two
def invaders(ez):
    cols, rows, dx, dy = 10, 4, 116, 58
    ox0 = W / 2 - dx * (cols - 1) / 2
    sb, ox, oy = [], [], []
    x, y, d, n = ox0, 70.0, 1, 0
    b = 112.0
    while b < 136:
        sb.append(b); ox.append(x); oy.append(y)
        n += 1
        if n % 4 == 0: y += 22; d = -d
        else: x += d * 30
        b += 1 if b < 128 else 0.5
    lb, lc = [], []
    for i, b in enumerate(range(112, 120)):
        pr = [(4, 5), (3, 6), (2, 7), (1, 8), (0, 9), (1, 8), (2, 7), (3, 6)][i]
        cs = list(pr) + ([(pr[0] + 5) % 10] if i % 2 == 1 else []) if not ez else list(pr)
        for c in sorted(set(cs)): lb.append(b + 0.0); lc.append(c)
    for i, b in enumerate(range(120, 128)):
        k = i % 3; cs = [c for c in range(k, 10, 3)]
        if ez: cs = cs[:2]
        else: cs = sorted(set(cs + [(k + 1) % 10, (k + 5) % 10]))
        for c in cs: lb.append(b); lc.append(c)
    for i in range(16):
        b = 128 + 0.5 * i; j = i % 10 if (i // 10) % 2 == 0 else 9 - i % 10
        cs = [j % 10, 9 - j % 10] if not ez else [j % 10]
        if not ez: cs.append((j + 5) % 10 if i % 2 == 1 else (j + 3) % 10)
        for c in sorted(set(cs)): lb.append(b); lc.append(c)
    out = [(110, 'L20inv', dict(cols=cols, rows=rows, dx=dx, dy=dy, sb=sb, ox=ox, oy=oy, lb=lb, lc=lc, lw=40 if ez else 62, lh=0.35, wn=1.3 if ez else 1, dv=[136, 138], ddur=3 if ez else 1.6, tw=110.5, ta=112, te=141.5))]
    bd = [128.5 + 0.25 * i for i in range(13)] if not ez else [128.5 + i for i in range(4)]
    out.append((127, 'L20ufo', dict(y=34, dir=1, bd=bd, vb=750 if ez else 1050, wn=0.9 if ez else 0.75, ta=128, te=132)))
    return out
both(invaders)
fx(112, 0.22, 14, 1.6); fx(128, 0.08, 6, 0.6)

# 1:06.7 the break - the glitch: the screen in 6 slices, all pink but for a hole each that slides (the slices slide against each other)
def glitch(ez):
    return [(139.5, 'L20glitch', dict(n=6, hw=230 if ez else 150, x0=[300, 980, 420, 860, 540, 740], v=[s * (300 if ez else 460) for s in [1, -1, 1, -1, 1, -1]], tw=140, ta=140.75, te=143.75))]
both(glitch)

# 1:08.1 the rhythm game: 5 lanes, the notes come down with the song (a kick on every beat, the pumping chords between, the riffs) and
# one lane is always free (it moves a lane a beat at most). Long notes are walls. 160: the highway leans left and right; 166-170 lanes swap
def lanes(ez):
    nb, nl, nlen = [], [], []
    F = 2   # the free lane
    free = {}
    path = [2, 2, 3, 3, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 4, 3, 2, 2, 1, 0, 1, 2, 3, 2, 1, 1, 2, 3] if ez else [2, 3, 4, 3, 2, 1, 0, 1, 2, 3, 4, 4, 3, 2, 1, 2, 3, 2, 1, 0, 0, 1, 2, 3, 4, 3, 2, 1]
    for i, b in enumerate(range(144, 171)):
        F = path[i % len(path)]; free[b] = F
        others = [l for l in range(5) if l != F]
        kick = others[(i * 2) % 4]
        nb.append(b); nl.append(kick); nlen.append(0)
        if ez and i % 2 == 1: continue
        # the chord between the beats: two lanes, never the free one or the one it goes to next
        Fn = path[(i + 1) % len(path)]
        ch = [l for l in range(5) if l not in (F, Fn)]
        pick = ch if not ez else [ch[i % len(ch)], ch[(i + 1) % len(ch)]]
        for l in sorted(set(pick)): nb.append(b + 0.5); nl.append(l); nlen.append(0)
        # long notes every 4 beats: a wall on the far side of the free lane
        if i % 4 == 2 and not ez:
            far = 0 if F >= 3 else 4
            nb.append(b + 0.25); nl.append(far); nlen.append(1.25)
    # the riffs (from the song at 148-151): quick notes on the far lanes
    if not ez:
        for b in [148.25, 148.5, 148.75, 149.5, 149.75, 150.0, 150.5, 151.5]:
            F = free[int(b)]; Fn = free.get(int(b) + 1, F); l = 0 if min(F, Fn) >= 2 else 4
            nb.append(b); nl.append(l); nlen.append(0)
    return [(141, 'L20lanes', dict(n=5, lw=256, cx0=128, hitY=600, vpb=380 if ez else 450, nb=nb, nl=nl, nlen=nlen, tilt=12 if ez else 26, tb0=160 if ez else 154, tb1=166,
             sw=[166, 168] if ez else [166, 167.5, 169], sa=[1, 0] if ez else [1, 0, 2], sbb=[3, 4] if ez else [3, 4, 3], swd=1, tw=142, ta=142.5, te=173, line=True))]
both(lanes)
fx(144, 0.12, 8, 1.0)

# 1:21.2 the near silence: the screen closes in to a box in the middle, then bursts open on the boss (176)
both(lambda ez: [(170, 'L20close', dict(bw=320 if ez else 280, bh=260 if ez else 220, tw=170.5, t0=171, t1=175.5, t2=176))])

# ================= part 3: the boss (176-242) =================
ev(174, 'L20boss', **{'in': 174.5, 'on': 176, 'ex': 237})
fx(176, 0.2, 14, 1.6)
# 1:23.1 the mirrored sprays from its wings, and its laser: it aims at you, fires a beat later and sweeps
def boss1(ez):
    out = [(176, 'L20spray', dict(ex=[440, 840], ey=[102, 102], ta=178, te=192, ev=0.75 if ez else 0.375, fan=2 if ez else 3, spread=16 if ez else 11, amp=60, per=8, sp=320 if ez else 420, R=11 if ez else 14))]
    for i, ba in enumerate([180, 184, 188]):
        out.append((ba - 1.5, 'L20beam', dict(x=640, y=86, ba=ba, bf=ba + (1.0 if ez else 0.75), be=ba + 2, w=64 if ez else 84, swp=(25 if ez else 40) * (1 if i % 2 == 0 else -1), rnd=True)))
    return out
both(boss1)
# 1:30.6 the Tempest tube: spikes climb out of the middle along 12 turning lanes: crosses, then two spiral arms, then a wall with a free pair
def tube(ez):
    sb, sl = [], []
    for i in range(8):   # 192-195.5: crosses
        b = 192 + 0.5 * i
        for l in ([i % 12, (i + 4) % 12, (i + 8) % 12] if ez else [i % 12, (i + 3) % 12, (i + 6) % 12, (i + 9) % 12]): sb.append(b); sl.append(l)
    for i in range(16 if not ez else 8):   # 196-199.75: spiral arms
        b = 196 + (0.25 if not ez else 0.5) * i
        for l in ([i % 12, (i + 6) % 12] if not ez else [i % 12]): sb.append(b); sl.append(l)
    F = 0
    for i in range(10):  # 200-204.5: a wall with a free pair that moves a lane a beat (back after 202.5)
        b = 200 + 0.5 * i; F = (-(i // 2)) % 12 if b < 202.5 else (-2 + (i - 5) // 4) % 12   # against the turn, then with it every 2 beats
        free = {F % 12, (F + 1) % 12}
        for l in range(12):
            if l not in free: sb.append(b); sl.append(l)
    return [(190, 'L20tube', dict(x=640, y=400, n=12, w=0.3 if ez else 0.35, a0=0, sb=sb, sl=sl, v=470 if ez else 520, len=100 if ez else 110, r0=50, eR=48, tw=190.5, ta=192, te=205))]
both(tube)
# 1:36.2 the flip: the boss turns the arena over (it goes round to the bottom) and two shock rings come out of the middle
def flip(ez):
    return [(204, 'L20shock', dict(x=640, y=360, ta=205.5, v=560 if ez else 650, th=24, ga=90, gw=80 if ez else 60, r0=40, wn=1)),
            (205, 'L20shock', dict(x=640, y=360, ta=206.5, v=560 if ez else 650, th=24, ga=-60, gw=80 if ez else 60, r0=40, wn=1))]
both(flip)
fx(208, 0.2, 12, 1.4)
# 1:38.1 the boss in a rage circles the arena and throws every game of the level at you, in order: Tetris, the invaders, the rhythm game,
# Pong, then a mix. All in its frame (u across, v away from it), so "down" is always away from the boss
def rage(ez):
    out = []
    # 208-213.5 Tetris rain
    cols = [-6, 2, -2, 6, -8, 0, 4, -4, 8, -1, 3, -5, 7, -7, 1]
    pb = [208 + (0.375 if not ez else 1) * i for i in range(15 if not ez else 6)]
    out.append((207, 'L20rain', dict(pb=pb, pc=cols[:len(pb)], ps=[(i * 3) % 7 for i in range(len(pb))], pr=[0] * len(pb), pst=0.15 if not ez else 0.35, pw=0.75, v0=80, frame=True)))
    # 214-219.5 a row of invaders and their lasers
    lb, lc = [], []
    for i in range(12):
        b = 214.5 + 0.5 * i; k = i % 3
        cs = [c for c in range(k, 10, 3)] if not ez else [c for c in range(k, 10, 4)]
        for c in cs: lb.append(b); lc.append(c)
    out.append((212.5, 'L20row', dict(n=10, sp=140, y0=110, sw=50, lb=lb, lc=lc, lw=40 if ez else 56, lh=0.35, wn=1.2 if ez else 1, ta=214, te=220, frame=True)))
    # 220-225.5 the rhythm game out of the boss: 7 lanes, a free lane that moves a lane a beat
    nb, nl, nlen = [], [], []
    path = [3, 2, 1, 1, 2, 3, 4, 5, 5, 4, 3, 3]
    for i in range(12):
        b = 219.5 + 0.5 * i; F = path[i]
        ls = [l for l in range(7) if abs(l - F) > (1 if ez else 0)]
        pick = ls[i % 2::2] if not ez else ls[i % 3::3]
        for l in pick: nb.append(b + 2); nl.append(l); nlen.append(0)
    out.append((218, 'L20lanes', dict(n=7, lw=220, cx0=-660, hitY=60 + 2 * 300, vpb=300, clipY=60, nb=nb, nl=nl, nlen=nlen, tilt=0, tb0=0, tb1=0, sw=[], sa=[], sbb=[], tw=219, ta=219.5, te=231, line=False, frame=True)))
    return out
both(rage)
# 226-231.5 Pong without paddles: balls off the screen's edges (the screen, not the boss's frame)
def pong2(ez):
    R = 16 if ez else 22
    rt = [[(226, 200, 120), (227.5, 1200, 360), (229, 700, 690), (230.5, 60, 300), (232, 640, -60)],
          [(226, 1080, 600), (227.5, 80, 420), (229, 580, 30), (230.5, 1220, 380), (232, 640, 800)],
          [(226, 640, 40), (227, 1240, 520), (228.5, 300, 690), (230, 40, 200), (231.5, 900, 30), (232, 1100, -60)]]
    if ez: rt = rt[:2]
    else: rt.append([(226, 40, 600), (227.5, 900, 690), (228.5, 1240, 250), (230, 520, 30), (231.5, 100, 500), (232, -60, 560)])
    nk, kb, kx, ky = [], [], [], []
    for r in rt:
        nk.append(len(r)); kb += [p[0] for p in r]; kx += [p[1] for p in r]; ky += [p[2] for p in r]
    return [(224.5, 'L20pong', dict(nk=nk, kb=kb, kx=kx, ky=ky, R=R, pad=False, lb=[], ly=[], rb=[], ry=[], tw=225, ta=226, te=232))]
both(pong2)
# 232-236 the mix: rain and lasers together
def mix(ez):
    pb = [232 + (0.5 if not ez else 1) * i for i in range(8 if not ez else 4)]
    lb, lc = [], []
    for i in range(8):
        b = 232.5 + 0.5 * i
        for c in ([(i * 3) % 10, (i * 3 + 5) % 10] if not ez else [(i * 3) % 10]): lb.append(b); lc.append(c)
    return [(231, 'L20rain', dict(pb=pb, pc=[5, -5, 1, -7, 7, -3, 3, -1][:len(pb)], ps=[(i * 2 + 1) % 7 for i in range(len(pb))], pr=[1] * len(pb), pst=0.25 if not ez else 0.35, pw=0.75, v0=80, frame=True)),
            (230.5, 'L20row', dict(n=10, sp=140, y0=110, sw=60, lb=lb, lc=lc, lw=40 if ez else 56, lh=0.35, wn=1.2 if ez else 1, ta=232, te=236.5, frame=True))]
both(mix)
# 1:51.2 its end: it cracks, bursts at 238, and three shock rings come out of it with their gaps down-left, down-right, down
def boom(ez):
    out = []
    for b, ga in [(238, 115), (238.5, 70), (239, 95)]:
        out.append((b - 1, 'L20shock', dict(x=640, y=80, ta=b, v=1050 if ez else 1150, th=24, ga=ga, gw=75 if ez else 52, r0=30, wn=0.75)))
    return out
both(boom)
fx(238, 0.25, 14, 1.4)
fx(240, 0.4, 12, 1.5); ev(240, 'L20end', ta=240, te=242)

EV.sort(key=lambda e: e['b'])
energy = json.load(open(os.path.join(HERE, 'energy.json')))
D = {'id': 'L20', 'name': 'المرحلة العشرون', 'song': 'New Game', 'file': 'SONG_L20', 'bpm': 128, 'off': 0.59075, 'end': 242, 'cps': [0, 112, 176],
     'ez': {'ezg': 0, 'ezc': 0, 'ez': 1.0, 'ezsp': 0.9, 'ezwn': 1.15}, 'energy': energy, 'events': EV}
json.dump(D, open(OUT, 'w'), ensure_ascii=False, separators=(',', ':'))
print('events', len(EV), '->', OUT)
