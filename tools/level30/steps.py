#!/usr/bin/env python3
# C'est la vie (main level 30): onset strength of the song per quarter beat, per band -> steps.json (read by mkdata.py).
# python3 steps.py <song.mp3>   (the whole original song; the level starts at 0:07.000, its beat 0 = 0:07.4905, 130 BPM)
import numpy as np, json, subprocess, sys
sr = 22050; hop = 64; win = 1024; dt = hop / sr
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', sys.argv[1], '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
n = len(x) // hop
X = np.lib.stride_tricks.sliding_window_view(np.pad(x, (win // 2, win)), win)[::hop][:n]
S = np.abs(np.fft.rfft(X * np.hanning(win), axis=1)); f = np.fft.rfftfreq(win, 1 / sr)
L = np.log1p(10 * S); D = np.maximum(0, np.diff(L, axis=0, prepend=L[:1]))
bands = {'K': (30, 120), 'B': (120, 400), 'V': (400, 1500), 'S': (1500, 5000), 'H': (6000, 11000)}
O = {k: D[:, (f >= a) & (f < b)].sum(1) for k, (a, b) in bands.items()}
P = 60 / 130; OFF = 7.4905; NB = 392
J = {}
for k, o in O.items():
    v = np.array([o[max(0, int(round((OFF + st / 4 * P) / dt)) - 2):int(round((OFF + st / 4 * P) / dt)) + 3].max() for st in range(NB * 4)])
    J[k] = [round(float(q), 3) for q in v / np.percentile(v[144:], 99.5)]
json.dump(J, open('steps.json', 'w'))
