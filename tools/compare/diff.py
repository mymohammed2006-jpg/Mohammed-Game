# Compares the original's run (A) with the port's run (B): state logs line by line, then each captured frame.
# python3 diff.py <dirA> <dirB> [report.png]
import sys, os
import numpy as np
from PIL import Image
A, B = sys.argv[1], sys.argv[2]
la = open(os.path.join(A, 'state.log')).read().split('\n')
lb = open(os.path.join(B, 'state.log')).read().split('\n')
names = 'frame songT x y hp hazards particles rngS checkpoint sumX sumY hits deaths state'.split()
bad = 0
for i, (a, b) in enumerate(zip(la, lb)):
    if a == b: continue
    fa, fb = a.split(), b.split()
    diffs = []
    for n, x, y in zip(names, fa, fb):
        if x == y: continue
        try:
            if abs(float(x) - float(y)) <= 1e-6 * max(1, abs(float(x))): continue
        except ValueError: pass
        diffs.append(f'{n}: {x} vs {y}')
    if diffs:
        bad += 1
        if bad <= 5: print('STATE line', i, '; '.join(diffs))
if len(la) != len(lb): print('STATE length', len(la), 'vs', len(lb))
print('state: %d lines differ of %d' % (bad, min(len(la), len(lb))))
worst = []
for f in sorted(os.listdir(A)):
    if not f.endswith('.png') or not os.path.exists(os.path.join(B, f)): continue
    a = np.asarray(Image.open(os.path.join(A, f)).convert('RGB')).astype(int)
    b = np.asarray(Image.open(os.path.join(B, f)).convert('RGB')).astype(int)
    d = np.abs(a - b).max(axis=2)
    worst.append((d.mean(), (d > 40).sum(), f))
worst.sort(reverse=True)
print('frames: %d compared; mean diff avg %.2f/255' % (len(worst), np.mean([w[0] for w in worst]) if worst else 0))
for m, n, f in worst[:8]: print('  %s mean %.2f, %d px off by >40' % (f, m, n))
