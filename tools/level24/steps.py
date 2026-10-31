#!/usr/bin/env python3
# يالعين قسامي (main level 24): onset strength of the song per quarter beat, per band -> steps.json (read by mkdata.py).
# python3 steps.py <song.mp3>   (the whole original song; the level is 2:34.0-5:27.6 of it, its beat 0 = 2:34.1975, 120 BPM)
import numpy as np, json, subprocess, sys
sr = 22050; hop = 256; dt = hop / sr; win = 1024
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', sys.argv[1], '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
n = len(x) // hop
X = np.lib.stride_tricks.sliding_window_view(np.pad(x, (0, win)), win)[::hop][:n]
S = np.abs(np.fft.rfft(X * np.hanning(win), axis=1)); f = np.fft.rfftfreq(win, 1 / sr)
L = np.log1p(10 * S); D = np.maximum(0, np.diff(L, axis=0, prepend=L[:1]))
bands = {'K': (30, 110), 'B': (110, 300), 'S': (300, 1200), 'M': (1200, 4000), 'H': (7000, 11000)}
O = {k: D[:, (f >= a) & (f < b)].sum(1) for k, (a, b) in bands.items()}
for k in O: O[k] = O[k] / np.percentile(O[k][int(150 / dt):int(330 / dt)], 99.5)
P = 0.5; OFF = 154.1975
J = {k: [round(float(O[k][max(0, int(round((OFF + st / 4 * P) / dt)) - 2):int(round((OFF + st / 4 * P) / dt)) + 3].max()), 3) for st in range(346 * 4)] for k in O}
json.dump(J, open('steps.json', 'w'))
