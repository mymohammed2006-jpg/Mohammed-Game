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
# Everything here moves on a sound of the song (tools/level20 analysis: the claps, the kicks, the tune's notes and their pitch).
# Pitch: a higher note goes right (or up), a lower one left (or down)
PR = {'C': 0, 'C#': 1, 'D': 2, 'D#': 3, 'E': 4, 'F': 5, 'F#': 6, 'G': 7, 'G#': 8, 'A': 9, 'A#': 10, 'B': 11}
def notes(s): return [(float(t[:t.index('#') - 1 if t.endswith('##') else len(t.rstrip('ABCDEFG#'))]), t[len(t.rstrip('ABCDEFG#')):]) for t in s.split()]
# the tune's notes (beat + pitch), from the song
N_INTRO = notes('0.5C# 1.75C# 3.75C# 4.25C# 5.75C# 6.25C# 6.75E 7.25E 7.75C# 8.25C# 9.25F# 9.75F# 10.25C# 11.75F# 12.25C# 13.75C# 15.5D# 16.25C# 16.5F# 17.75C# 19.75C# 20.25C# 20.5E 21.25E 21.75C# 22.25C# 22.5E 23.25E 23.75C# 24.25C# 25.75F# 26.25C# 27.75F# 28.25C# 29.75C# 31.5D#')
N_BASS = notes('33.75A 35.75A 36.25A 37.75A 38.25A 39.75A 40.25A 40.75C# 41.75A 42.25A 43.75A 44.25A 45.5G# 45.75A 47.75E 48.75C# 49.75A 51.75A 52.25A 53.25E 53.75A 54.25A 54.5E 55.25E 55.75A 56.25A 56.75C# 57.75A 58.25A 60.25A 61.75A 62.0A 63.0A 63.25A 63.5E 63.75C#')
N_STAB = notes('65.0A 65.75A 70.25C# 71.25C# 73.5G# 75.25C# 76.25C# 77.5G# 78.25C# 79.0C# 79.5G# 79.75A')
N_RUN = notes('101.0F# 101.25D 101.5G# 101.75A 103.25D# 103.5A 103.75A')
N_DROP = notes('115.5G# 117.5G# 118.25G# 118.5D# 119.5A 120.0F# 122.75G# 123.5F 124.25A 124.5A 125.25G# 125.5G# 125.75G# 126.0F# 126.25F# 126.5F# 126.75F# 127.5E 127.75E 128.0F# 130.75G# 131.5G# 132.0F 132.75G# 133.5G# 134.0E 134.25G# 134.5G# 135.0A 135.25F 135.5G# 135.75G# 136.0G# 139.0F')
N_BREAK = notes('140.0F# 140.25F# 140.5A 140.75A 141.5E 141.75F# 142.25F# 142.5F#')
N_DROP2 = notes('144.0F# 147.5G# 148.25G# 148.5G# 148.75A 149.5G# 149.75F# 150.25G# 150.5E 151.5A 152.0F# 155.5G# 156.25A 156.5A 157.5G# 158.25F# 158.5F# 158.75F# 159.5E 160.0F# 164.25G# 164.5G# 164.75A 165.5G# 165.75F# 166.0G# 166.25G# 166.5G# 166.75G# 167.0A 167.25G# 167.5G# 167.75G# 168.0G#')
def up(a, b): return (PR[b] - PR[a] + 6) % 12 - 6   # the step from note a to note b (-6 .. 5)

# 0:00 Tetris on the tune: a piece hops a column on every main note (C#, from 0:15.6 A: the way the tune goes) and turns on the other
# notes; on the clap it SLAMS down at once onto its dashed ghost. A row comes up from below on the double kicks (0:15.6-0:30, the
# next places dashed a beat before) and the laser takes the bottom rows away at the ends of the phrases. 0:30 the crash: all of it goes
SH = [[0, 1, 1, 1, 2, 1, 3, 1], [1, 1, 2, 1, 1, 2, 2, 2], [0, 1, 1, 1, 2, 1, 1, 0], [1, 0, 2, 0, 0, 1, 1, 1], [0, 0, 1, 0, 1, 1, 2, 1], [0, 0, 0, 1, 1, 1, 2, 1], [2, 0, 0, 1, 1, 1, 2, 1]]
SC = [1.5, 1.5, 1, 1, 1, 1, 1]
def turn(s, r):
    c = SC[s]; out = []
    for j in range(4):
        x, y = SH[s][2 * j], SH[s][2 * j + 1]
        for _ in range(r % 4): x, y = 2 * c - y, x
        out.append((int(round(x)), int(round(y))))
    mx = min(a for a, _ in out); my = min(b for _, b in out)
    return [(a - mx, b - my) for a, b in out]
COLS, ROWS = 16, 9
def tetris(ez):
    # the pieces: (spawn beat, slam beat, base column, the half it keeps to, mirrored) and the notes they dance on
    P = []
    claps = list(range(2, 32, 4 if ez else 2))
    t0 = 0.5
    bases = [3, 11, 6, 13, 1, 9, 4, 12, 7, 2, 10, 5, 14, 8, 0]
    if ez:
        for i, c in enumerate(claps): P.append(dict(sp=t0, sl=c, base=bases[i % len(bases)], lo=0, hi=15, mir=False, notes=N_INTRO, main='C#')); t0 = c
    else:
        # Normal: two pieces, the left one slams on a clap, its mirror on the right on the next; each lives 4 beats
        for i, c in enumerate(claps):
            right = i % 2 == 1
            P.append(dict(sp=max(0.5, c - 4), sl=c, base=([3, 1, 5, 2, 6, 0, 4, 2] if not right else [12, 14, 10, 13, 9, 15, 11, 13])[(i // 2) % 8],
                          lo=8 if right else 0, hi=15 if right else 7, mir=right, notes=N_INTRO, main='C#'))
    if ez:
        for i, c in enumerate(range(34, 63, 4)): P.append(dict(sp=c - 4 if i else 32, sl=c, base=[3, 11, 5, 13, 2, 10, 6, 12][i], lo=0, hi=15, mir=False, notes=N_BASS, main='A'))
    else:
        # from the bass: both halves slam together on every clap, mirrored
        for i, c in enumerate(range(34, 63, 2)): P.append(dict(sp=c - 2 if i else 32, sl=c, base=[2, 4, 1, 5, 3, 6, 2, 4][i % 8], lo=0, hi=7, mir=False, notes=N_BASS, main='A'))
        for i, c in enumerate(range(34, 63, 2)): P.append(dict(sp=c - 2 if i else 32, sl=c, base=[13, 11, 14, 10, 12, 9, 13, 11][i % 8], lo=8, hi=15, mir=True, notes=N_BASS, main='A'))
    garbage = [] if False else ([37, 45, 53, 61] if ez else [33, 37, 43, 45, 49, 53, 59, 61])
    clears = [7.75, 15.75, 23.75, 31.75, 39.75, 47.75, 55.75] if ez else [b + 3.75 for b in range(0, 32, 4)] + [b + 1.75 for b in range(32, 62, 2)]
    # play it out on a 1/4 beat clock
    grid = {}            # (col,row) -> cell
    cells = []; pz_states = []; cl, clr = [], []
    shape_i = 0
    for p in P: p['s'] = shape_i % 7; shape_i += 3
    ev = sorted({round(x * 4) / 4 for x in [p['sl'] for p in P] + garbage + clears})
    def height(): return ROWS - min([r for (c, r) in grid] or [ROWS])
    # the pieces' flights (they don't touch the stack until they slam)
    flights = {}
    for k, p in enumerate(P):
        s = p['s']; r = 0; x = p['base']; dirn = -1 if p['mir'] else 1
        def cl_x(x, r):
            w = max(a for a, _ in turn(s, r)); return max(p['lo'], min(p['hi'] - w, x))
        x = cl_x(x, r); st = [(p['sp'], [(x + a, b) for a, b in turn(s, r)])]
        prev = None
        for (nb, pc) in p['notes']:
            if nb <= p['sp'] + 0.5 - 1e-9 or nb >= p['sl'] - 1e-9: prev = pc; continue
            if pc == p['main']:
                d = 1 if prev is None else (1 if up(prev, pc) > 0 else -1 if up(prev, pc) < 0 else dirn)
                if p['mir']: d = -d
                nx = cl_x(x + d, r)
                if nx == x: d = -d; nx = cl_x(x + d, r)
                x = nx; dirn = d if not p['mir'] else -d
            else:
                r = (r + (1 if up(p['main'], pc) > 0 else 3)) % 4; x = cl_x(x, r)
            st.append((nb, [(x + a, b) for a, b in turn(s, r)])); prev = pc
        flights[k] = st
    order = sorted(range(len(P)), key=lambda k: P[k]['sl'])
    # gravity: a flying piece also falls a row on every half beat (Easy every beat), so it comes down into your space; never into the stack
    gstep = 1 if ez else 0.5
    base = {k: flights[k] for k in range(len(P))}
    flights = {k: [] for k in range(len(P))}
    dyk = {k: 0 for k in range(len(P))}
    def at(k, t):
        cs = None
        for b, c in base[k]:
            if b <= t + 1e-9: cs = c
        return cs
    def put(k, t, cs):
        if not flights[k] or flights[k][-1][1] != cs:
            if flights[k] and abs(flights[k][-1][0] - t) < 1e-9: flights[k][-1] = (t, cs)
            else: flights[k].append((t, cs))
    def fits(cs): return all(0 <= r < ROWS and (c, r) not in grid for c, r in cs)
    times = sorted({x / 4 for x in range(0, 65 * 4)} | {b for k in base for b, _ in base[k] if b < 65})
    gi = 0
    for t in times:
        # garbage rows (everything up a row, a new row at the bottom with one hole)
        if any(abs(t - g) < 1e-9 for g in garbage):
            if height() <= 5:
                for c in cells:
                    if c['end'] > t: c['kf'].append((t, c['kf'][-1][1] - 1))
                grid = {(c, r - 1): v for (c, r), v in grid.items()}
                hole = [5, 12, 2, 9, 14, 6, 0, 11][gi % 8]; gi += 1
                for col in range(COLS):
                    if col == hole: continue
                    cell = dict(col=col, kf=[(t, ROWS - 1)], end=999); cells.append(cell); grid[(col, ROWS - 1)] = cell
            else: print('tetris: no garbage at', t)
        # the laser takes the bottom rows (keep it low)
        if any(abs(t - c) < 1e-9 for c in clears):
            keep = (2 if t < 32 else 3) if ez else (3 if t < 32 else 4)
            n = 0
            while height() > keep or (n == 0 and height() > 0):
                n += 1
                row = ROWS - n
                for (c, r), v in list(grid.items()):
                    if r == row: v['end'] = t; del grid[(c, r)]
                cl.append(t); clr.append(row)
                if n >= 4: break
            if n:
                moved = {}
                for (c, r), v in grid.items():
                    v['kf'].append((t, r + n)); moved[(c, r + n)] = v
                grid = moved
        # the flying pieces: their dance, their fall, kept out of the stack
        for k in range(len(P)):
            p = P[k]
            if t < p['sp'] - 1e-9 or t >= p['sl'] - 1e-9: continue
            cs0 = at(k, t)
            sh = lambda d: [(c, r + d) for c, r in cs0]
            if t < p['sl'] - 1e-9 and t >= p['sp'] + 1 - 1e-9 and abs((t - p['sp']) / gstep - round((t - p['sp']) / gstep)) < 1e-9 and fits(sh(dyk[k] + 1)) and fits(sh(dyk[k] + 2)):
                dyk[k] += 1
            while dyk[k] > 0 and not fits(sh(dyk[k])): dyk[k] -= 1
            put(k, t, sh(dyk[k]))
        # the slams
        for k in order:
            p = P[k]
            if abs(p['sl'] - t) > 1e-9: continue
            cs = flights[k][-1][1]
            dy = 0
            while all(r + dy + 1 < ROWS and (c, r + dy + 1) not in grid for c, r in cs): dy += 1
            land = [(c, r + dy) for c, r in cs]
            flights[k].append((t, land))
            for c, r in land:
                cell = dict(col=c, kf=[(t, r)], end=999); cells.append(cell); grid[(c, r)] = cell
    # the crash at 64: all of it goes at 64.5
    rows = sorted({r for (c, r) in grid})
    for r in rows: cl.append(64.5); clr.append(r)
    for c in cells:
        if c['end'] > 64.5: c['end'] = 64.5
    pz = []
    for k in range(len(P)):
        st = flights[k]; pz.append(len(st))
        for b, cs in st:
            pz.append(b)
            for c, r in cs: pz += [c, r]
    sk = []
    for c in cells:
        sk += [c['col'], len(c['kf'])]
        for b, r in c['kf']: sk += [b, r]
        sk.append(c['end'])
    print(('Easy' if ez else 'Normal') + f' tetris: {len(P)} pieces, {len(cells)} cells, clears {len(cl)}')
    return [(0, 'L20tetris', dict(pz=pz, sk=sk, cl=cl, clr=clr, tw=0, te=64.6))]
both(tetris)
fx(32, 0.12, 8, 1.0)

# 0:30.6 the loading screen on the broken beat (hits at 0, 0.5, 1.25, 2, 2.75, 3.5 of every 4 beats): two loading bars step on every hit
# (from the left from 64.5, from the right from 72) and only their holes are safe; the loading ring breathes on the beat and its gap
# JUMPS to a new place on every stab of the tune (higher note: it jumps on clockwise), the next place dashed
BROKEN = [b + o for b in range(64, 80, 4) for o in (0, 0.5, 1.25, 2, 2.75, 3.5)]
def loading(ez):
    sa = [b for b in BROKEN if b >= 64.5]; sbb = [b for b in BROKEN if b >= 72]
    ha = [round(360 + 210 * math.sin(0.55 * k + 0.3)) for k in range(len(sa))]
    hb = [round(360 - 200 * math.sin(0.6 * k + 1.1)) for k in range(len(sbb))]
    out = [(62.5, 'L20front', dict(x0=40, dir=1, sx=52, hy=ha, hh=70 if ez else 54, tw=63.5, ta=64.5, sb=sa))]
    if not ez: out.append((62.5, 'L20front', dict(x0=1240, dir=-1, sx=52, hy=hb, hh=54, tw=70.5, ta=72, sb=sbb)))
    jb, ja = [], []; a = -90; prev = 'A'
    for b, pc in N_STAB:
        if b < 65: prev = pc; continue
        d = 1 if up(prev, pc) >= 0 else -1; a += d * (70 if ez else 95); jb.append(b); ja.append(a); prev = pc
    out.append((63, 'L20ring', dict(rmin=60, rmax=780, per=2, th=30, gw=50 if ez else 42, w=0, rv=4, a0=-90, core=80, tw=63, ta=65, te=80, jb=jb, ja=ja)))
    return out
both(loading)
fx(64, 0.18, 10, 1.2)

# 0:38.1 Pong on the build: every new long note (they climb) brings in a ball, from the left paddle at the note's height; the balls hit
# the walls and paddles on the kicks (from 0:45.6 the kicks come every half beat); on the quick tune at 0:47.5 the left paddle fires a
# small ball on every note. 0:49.3 on the climbing notes the balls stop one by one and aim at you, and on the drop (0:53) they all fly
ADD = [(80, 'E'), (81.75, 'F#'), (85.75, 'G#'), (89.75, 'A#'), (93.75, 'B')]
KICKS = sorted(set([80, 82, 84, 85, 86, 87, 87.5, 88, 90, 92, 93, 94, 95, 95.5] + [96 + 0.5 * i for i in range(16)] + [104 + 0.5 * i for i in range(12)]))
FREEZE = [104, 105.75, 107.5, 108.75, 110]
def pong(ez):
    R = 24 if ez else 36; XL, XR = 60, W - 60; v = 720 if ez else 1150
    adds = ADD[:3] if ez else ADD; frz = FREEZE[:3] if ez else FREEZE
    nb = len(adds)
    balls = []
    ytab = {'E': 600, 'F#': 520, 'G#': 420, 'A#': 300, 'B': 200}
    for i, (b, pc) in enumerate(adds): balls.append(dict(kb=[b], kx=[XL], ky=[ytab[pc]], side='L', last=b, fr=frz[i]))
    def side_of(x, y): return 'L' if x <= XL + 1 else 'R' if x >= XR - 1 else 'T' if y <= R + 1 else 'B'
    def cands(sd):
        out = []
        if sd != 'L': out += [('L', XL, y) for y in range(140, H - 139, 40)]
        if sd != 'R': out += [('R', XR, y) for y in range(140, H - 139, 40)]
        if sd != 'T': out += [('T', x, R) for x in range(140, W - 139, 40)]
        if sd != 'B': out += [('B', x, H - R) for x in range(140, W - 139, 40)]
        return out
    j = 0
    for kb in KICKS:
        # the kick goes to the ball that waited longest (alive, not stopped, at least a beat since its last hit)
        live = [bl for bl in balls if bl['kb'][0] < kb - 1e-9 and kb < bl['fr'] - 1e-9 and kb - bl['last'] >= 1 - 1e-9]
        if not live: continue
        bl = min(live, key=lambda q: q['last'])
        px, py = bl['kx'][-1], bl['ky'][-1]; D = v * (kb - bl['last']) * 0.46875
        cs = cands(bl['side'])
        sc = sorted(cs, key=lambda c: abs(math.hypot(c[1] - px, c[2] - py) - D) + (0 if c[0] in 'LR' else 50))
        good = [c for c in sc if abs(math.hypot(c[1] - px, c[2] - py) - D) < abs(math.hypot(sc[0][1] - px, sc[0][2] - py) - D) + 60]
        c = good[(j * 7) % len(good)]; j += 1
        bl['kb'].append(kb); bl['kx'].append(c[1]); bl['ky'].append(c[2]); bl['side'] = c[0]; bl['last'] = kb
    out = []; nk, KB, KX, KY = [], [], [], []
    lpad, rpad = [], []
    for bl in balls:
        # where it is when it stops: on its way to the next hit (or still, if none came)
        fr = bl['fr']; kb, kx, ky = bl['kb'], bl['kx'], bl['ky']
        D2 = v * 1.0 * 0.46875; px, py = kx[-1], ky[-1]
        # it carries on for one more line after its last hit, and stops on it at fr
        cs = cands(bl['side']); c = min(cs, key=lambda c: abs(math.hypot(c[1] - px, c[2] - py) - v * (fr - kb[-1] + 1) * 0.46875))
        u = (fr - kb[-1]) / (fr - kb[-1] + 1); sx = px + (c[1] - px) * u; sy = py + (c[2] - py) * u
        kb.append(fr); kx.append(sx); ky.append(sy)
        for i in range(len(kb)):
            if kx[i] <= XL + 1: lpad.append((kb[i], ky[i]))
            if kx[i] >= XR - 1: rpad.append((kb[i], ky[i]))
        nk.append(len(kb)); KB += kb; KX += kx; KY += ky
        out.append((fr - 0.5, 'L20shot', dict(bx=[sx], by=[sy], R=R, sp=1050 if ez else 1300, f0=fr, f1=112, fa=111.5, rnd=True)))
    lpad.sort(); rpad.sort()
    out.insert(0, (78.5, 'L20pong', dict(nk=nk, kb=KB, kx=KX, ky=KY, R=R, pad=True, lb=[b for b, y in lpad], ly=[y for b, y in lpad], rb=[b for b, y in rpad], ry=[y for b, y in rpad], tw=79, ta=80, te=111)))
    # the small balls on the quick tune: from the left paddle, straight across
    def padY(pts, b):
        if not pts: return H / 2
        if b <= pts[0][0]: return pts[0][1]
        for i in range(len(pts) - 1):
            if b < pts[i + 1][0]:
                u = (b - pts[i][0]) / (pts[i + 1][0] - pts[i][0]); e = u * u * (3 - 2 * u); return pts[i][1] + (pts[i + 1][1] - pts[i][1]) * e
        return pts[-1][1]
    sn, sb_, sx_, sy_ = [], [], [], []
    run = N_RUN[::2] if ez else N_RUN
    for b, pc in run:
        y = max(80, min(H - 80, padY(lpad, b) + (PR[pc] - 6) * 14)); dur = (W - 2 * XL) / (700 if ez else 1300) / 0.46875
        sn.append(2); sb_ += [b, b + dur]; sx_ += [XL, XR + 40]; sy_ += [y, y]
    out.append((99, 'L20pong', dict(nk=sn, kb=sb_, kx=sx_, ky=sy_, R=14, pad=False, lb=[], ly=[], rb=[], ry=[], tw=100, ta=100, te=106)))
    return out
both(pong)
fx(80, 0.1, 8, 1.0); fx(96, 0.08, 6, 0.8); fx(104, 0.1, 8, 0.8)

# ================= part 2: playing (112-175) =================
# 0:53.1 the drop - Space Invaders: the army steps on every kick. Every note of the tune makes the invader in ITS column fire (the column
# by the note's pitch, so the lasers play the tune across the screen; the laser comes out of the invader's own body, as wide as it). On
# the claps the bottom row drops bombs; the saucer crosses on the fill (1:02.5) and bombs on its hits; on the fill at 1:04.3 the
# invaders dive at you, one on every hit
PCOL = {'D#': 0, 'E': 1, 'F': 2, 'F#': 3, 'G': 4, 'G#': 5, 'A': 6, 'A#': 7, 'B': 8, 'C': 9, 'C#': 9, 'D': 0}
def invaders(ez):
    cols, rows, dx, dy = 10, 4, 116, 58
    ox0 = W / 2 - dx * (cols - 1) / 2
    sb, ox, oy = [], [], []
    x, y, d, n = ox0 - 45, 70.0, 1, 0
    for b in range(112, 136):
        sb.append(b); ox.append(x); oy.append(y); n += 1
        if n % 4 == 0: y += 22; d = -d
        else: x += d * 30
    fb, fc = [], []
    for i, (b, pc) in enumerate(N_DROP):
        if b >= 136: continue
        if ez and i % 2: continue
        fb.append(b); fc.append(PCOL[pc])
        if not ez: fb.append(b); fc.append(9 - PCOL[pc])   # Normal: its mirror too
    claps = [112, 116, 118, 120, 122, 124, 126, 128, 130, 132]
    bb, bc = [], []
    for k, b in enumerate(claps):
        for c in ([(k * 3) % 10] if ez else [(k * 3) % 10, (k * 3 + 5) % 10]): bb.append(b); bc.append(c)
    fill = [136, 136.75, 137, 137.25, 137.5, 137.75, 138, 138.25, 138.75, 139, 139.25, 139.5]
    if ez: fill = fill[::2]
    who = [(c, 3) for c in [0, 9, 1, 8, 2, 7, 3, 6, 4, 5]] + [(c, 2) for c in [0, 9]]
    db, dc, dr, dx2 = [], [], [], []
    for i, b in enumerate(fill):
        c, r = who[i]; db.append(b); dc.append(c); dr.append(r); dx2.append(W - (ox0 + c * dx))
    out = [(110, 'L20inv2', dict(cols=cols, rows=rows, dx=dx, dy=dy, sb=sb, ox=ox, oy=oy, fb=fb, fc=fc, lw=60, lh=0.35, wn=1.2 if ez else 1, bb=bb, bc=bc, vb=640 if ez else 820,
                                    db=db, dc=dc, dr=dr, dx2=dx2, ddur=2 if ez else 1.6, tw=110.5, ta=112, te=140.25))]
    bd = [134, 134.75, 135.5] if ez else [134, 134.25, 134.5, 134.75, 135, 135.25, 135.5]
    out.append((131, 'L20ufo', dict(y=34, dir=1, bd=bd, vb=750 if ez else 950, wn=0.9 if ez else 0.75, ta=132, te=136)))
    return out
both(invaders)
fx(112, 0.22, 14, 1.6); fx(128, 0.08, 6, 0.6)

# 1:06.2 the break, the tune alone - the glitch: the screen in 6 pink slices with a hole each; on every note a hole jumps (two in Normal),
# right for a higher note, left for a lower one; its next place is dashed before
def glitch(ez):
    n = 6; hw = 240 if ez else 170; x0 = [300, 980, 420, 860, 540, 740]; lo, hi = hw / 2 + 10, W - hw / 2 - 10
    keys = [[(139.5, x)] for x in x0]
    prev = 'F#'; turnk = 0
    for i, (b, pc) in enumerate(N_BREAK):
        d = 1 if up(prev, pc) > 0 else -1 if up(prev, pc) < 0 else (1 if i % 2 else -1); prev = pc
        for k in ([turnk % n] if ez else [turnk % n, (turnk + 3) % n]):
            x = keys[k][-1][1] + d * (220 if ez else 260)
            if x > hi: x = 2 * hi - x
            if x < lo: x = 2 * lo - x
            keys[k].append((b, round(x)))
        turnk += 1
    nk, kb, kx = [], [], []
    for k in range(n): nk.append(len(keys[k])); kb += [b for b, x in keys[k]]; kx += [x for b, x in keys[k]]
    return [(138.5, 'L20glitch2', dict(n=n, hw=hw, nk=nk, kb=kb, kx=kx, tw=139, ta=140, te=143.75))]
both(glitch)

# 1:08.1 the rhythm game: the notes ARE the tune's notes, each in the lane of its pitch (5 lanes, low to high); a long note is a long
# wall; every kick puts a note in the mirror lane. 1:15.6 the highway leans; 1:17.5 the quick notes pour down; 1:18 lanes swap
LANE = {'D': 0, 'D#': 0, 'E': 0, 'F': 0, 'F#': 1, 'G': 1, 'G#': 2, 'A': 3, 'A#': 4, 'B': 4, 'C': 4, 'C#': 4}
def lanes(ez):
    nb, nl, nlen = [], [], []
    mel = N_DROP2 if not ez else [x for i, x in enumerate(N_DROP2) if i % 3 != 2]
    for i, (b, pc) in enumerate(mel):
        nxt = mel[i + 1][0] if i + 1 < len(mel) else 172
        ln = min(1.5, nxt - b - 0.75) if nxt - b >= 1.5 else 0
        nb.append(b); nl.append(LANE[pc]); nlen.append(round(ln, 2))
    last = 2
    for b in range(144, 172):
        if ez and b % 2: continue
        cur = [LANE[pc] for t, pc in mel if t <= b]
        if cur: last = cur[-1]
        l = 4 - last
        if any(abs(t - b) < 1e-9 and LANE[pc] == l for t, pc in mel): continue
        nb.append(b); nl.append(l); nlen.append(0)
        if not ez and b % 2 == 0:
            # on the claps a second note, two lanes off the tune's
            l2 = (last + 2) % 5
            if l2 != l: nb.append(b); nl.append(l2); nlen.append(0)
        if not ez:
            # the hats: half a beat later a note next to the kick's, so the free lanes keep moving
            l3 = (l + (1 if b % 2 else 4)) % 5
            if not any(abs(t - b - 0.5) < 0.3 and LANE[pc] == l3 for t, pc in mel): nb.append(b + 0.5); nl.append(l3); nlen.append(0)
    return [(141, 'L20lanes', dict(n=5, lw=256, cx0=128, hitY=600, vpb=480 if ez else 680, nb=nb, nl=nl, nlen=nlen, tilt=12 if ez else 22, tb0=156, tb1=164,
             sw=[166, 168] if ez else [166, 167.5, 169], sa=[1, 0] if ez else [1, 0, 2], sbb=[3, 4] if ez else [3, 4, 3], swd=1, tw=142, ta=143.75, te=172, line=True))]
both(lanes)
fx(144, 0.12, 8, 1.0)

# 1:21.2 the near silence: the screen closes in a step on each of its last hits, then bursts open on the boss (176)
both(lambda ez: [(169, 'L20close', dict(bw=320 if ez else 280, bh=260 if ez else 220, tw=169.5, t0=170, t1=175.75, t2=176, cs=[171, 171.75, 172, 174, 174.5, 175, 175.75]))])

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
