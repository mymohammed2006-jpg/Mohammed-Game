#!/usr/bin/env python3
# شيلة حان الوعد يا لابتي (main level 25): the pulse, one number a bar (4 beats from beat 0), from songs/SONG_L25.mp3 -> data/dodge_L25.json "energy"
import numpy as np, json, subprocess, os
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, '..', '..')
sr = 22050; P = 60 / 134.7
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', os.path.join(R, 'songs', 'SONG_L25.mp3'), '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
e = []
for bar in range(103):
    a = int((0.38 + bar * 4 * P) * sr); b = int((0.38 + (bar + 1) * 4 * P) * sr); s = x[a:b]
    e.append(float(np.sqrt((s ** 2).mean())) if len(s) else 0)
m = max(e); e = [round(v / m, 3) for v in e]
p = os.path.join(R, 'data', 'dodge_L25.json'); d = json.load(open(p)); d['energy'] = e
open(p, 'w').write(json.dumps(d, ensure_ascii=False, separators=(',', ':'))); print(e)
