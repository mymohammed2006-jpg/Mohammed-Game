# Looks for flashes in a run's pictures: a frame that jumps away from the one before and back in the next (a blink of 1-2
# frames), anywhere on the screen, e.g. the clouds between rooms. python3 flash.py <dir with f*.png> [threshold=10]
import sys, glob
from PIL import Image, ImageChops, ImageStat
fs = sorted(glob.glob(sys.argv[1] + '/f*.png')); th = float(sys.argv[2]) if len(sys.argv) > 2 else 10
ims = [Image.open(f).convert('L').resize((320, 180)) for f in fs]
def d(a, b): return ImageStat.Stat(ImageChops.difference(ims[a], ims[b])).mean[0]
n = 0
for i in range(1, len(ims) - 2):
    for w in (1, 2):
        out, back, across = d(i - 1, i), d(i + w - 1, i + w), d(i - 1, i + w)
        if out > th and back > th and across < min(out, back) / 3:
            n += 1; print(f'blink at {fs[i].split("/")[-1]} ({w} frame): in {out:.1f}, out {back:.1f}, before vs after {across:.1f}')
print(f'{len(ims)} frames, {n} blinks')
