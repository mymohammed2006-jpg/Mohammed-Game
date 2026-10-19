import sys, os
# COLS=3,4,5,6,7: compare only these (0 x,1 y,2 hp,3 hazards,4 rngS,5 cp,6 sumx,7 sumy,8 hits,9 deaths)
COLS = [int(c) for c in os.environ.get('COLS', '').split(',') if c]
# state.log line: frame songT x y hp hazards particles rngS cp sumx sumy hits deaths state
def load(p):
    d = {}; run = 0; prev = False
    for ln in open(p):
        f = ln.split()
        if len(f) < 14: continue
        st = f[13]
        pl = st in ('play', 'replay')
        if pl and not prev: run += 1
        prev = pl
        if not pl: continue
        k = (run, round(float(f[1]) * 240))      # the step this frame ended on
        v = (f[2], f[3], f[4], f[5], f[7], f[8], f[9], f[10], f[11], f[12])
        if COLS: v = tuple(v[c] for c in COLS)
        d[k] = v
    return d
import os
TOL = float(os.environ.get('TOL', '0'))   # TOL=0.01: numbers may differ that much (old variable-frame code has float noise)
def same(a, b):
    if a == b: return True
    if not TOL: return False
    for x, y in zip(a, b):
        try:
            if abs(float(x) - float(y)) > TOL * max(1, abs(float(x))): return False
        except ValueError:
            if x != y: return False
    return True
logs = [load(p) for p in sys.argv[1:]]
base = logs[0]; bad = 0
for p, L in zip(sys.argv[2:], logs[1:]):
    common = sorted(set(base) & set(L)); diff = [k for k in common if not same(base[k], L[k])]
    print(f"{p}: {len(common)} shared moments, {len(diff)} differ, runs {max(k[0] for k in L)} vs {max(k[0] for k in base)}")
    for k in diff[:3]: print("  ", k, base[k], L[k])
    bad += len(diff) + (0 if common else 1)
print("RESULT:", "SAME" if bad == 0 else "DIFFERENT")
