#!/usr/bin/env python3
# راضي بحبك (main level 23): the pulse, one number a bar (4 beats from beat 0), from songs/SONG_L23.mp3 -> data/dodge_L23.json "energy"
# 96 BPM, beat 0 = 0.065 s (own beat search on the song's onsets)
import numpy as np, json, subprocess, os
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, '..', '..')
sr = 22050
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', os.path.join(R, 'songs', 'SONG_L23.mp3'), '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
e = []
for bar in range(66):
    a = int((0.065 + bar * 2.5) * sr); b = int((0.065 + (bar + 1) * 2.5) * sr); s = x[a:b]
    e.append(float(np.sqrt((s ** 2).mean())) if len(s) else 0)
m = max(e); e = [round(v / m, 3) for v in e]
p = os.path.join(R, 'data', 'dodge_L23.json')
d = json.load(open(p)) if os.path.exists(p) else {}
d.update({"id": "L23", "name": "المرحلة الثالثة والعشرون", "song": "راضي بحبك", "file": "SONG_L23", "bpm": 96, "off": 0.065, "end": 261, "cps": [0, 84, 168],
          "ez": {"ezg": 0, "ezc": 0, "ez": 1.0, "ezsp": 0.9, "ezwn": 1.15}, "energy": e})
open(p, 'w').write(json.dumps(d, ensure_ascii=False, separators=(',', ':'))); print(e)
