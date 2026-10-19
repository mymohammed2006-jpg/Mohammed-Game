# compares the UI check's outputs: state lines, requests, pictures (mean difference per captured frame)
import sys, os, difflib
a, b = sys.argv[1], sys.argv[2]
def lines(d, f): return open(os.path.join(d, f), encoding='utf-8').read().splitlines()
for f in ('state.log', 'net.log'):
    la, lb = lines(a, f), lines(b, f)
    bad = [i for i in range(max(len(la), len(lb))) if (la[i] if i < len(la) else None) != (lb[i] if i < len(lb) else None)]
    print(f'{f}: {len(la)} vs {len(lb)} lines, {len(bad)} differ')
    for i in bad[:6]:
        print('  orig:', (la[i] if i < len(la) else '-')[:400]); print('  port:', (lb[i] if i < len(lb) else '-')[:400])
try:
    from PIL import Image, ImageChops
    import numpy as np
    rows = []
    for fn in sorted(os.listdir(a)):
        if not fn.endswith('.png') or not os.path.exists(os.path.join(b, fn)): continue
        x = np.asarray(Image.open(os.path.join(a, fn)).convert('RGB'), dtype=np.int16); y = np.asarray(Image.open(os.path.join(b, fn)).convert('RGB'), dtype=np.int16)
        d = np.abs(x - y); rows.append((d.mean(), (d.max(axis=2) > 60).mean() * 100, fn))
    rows.sort(reverse=True)
    print('pictures: %d compared; worst (mean/255, %% pixels off by >60):' % len(rows))
    for m, p, fn in rows[:15]: print('  %s  %.2f  %.2f%%' % (fn, m, p))
except ImportError as e: print('pictures: need numpy + pillow', e)
