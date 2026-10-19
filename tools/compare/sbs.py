# side by side: original | port | difference x4.  python3 sbs.py <a.png> <b.png> <out.png>
import sys, numpy as np
from PIL import Image
a = np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(int); b = np.asarray(Image.open(sys.argv[2]).convert('RGB')).astype(int)
d = np.minimum(np.abs(a - b).max(axis=2) * 4, 255)
im = np.concatenate([a, b, np.stack([d] * 3, 2)], 0).astype(np.uint8)
Image.fromarray(im).resize((640, 1080)).save(sys.argv[3])
