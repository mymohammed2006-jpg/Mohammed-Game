#!/usr/bin/env python3
# "لا دندنت" (main level 26): onset strength of the song per quarter beat, per band -> steps.json (read by mkdata.py).
# python3 steps.py <song.mp3>   (the whole original song; the level starts at 0:30.000, its beat 0 = 0:30.445, 116 BPM; the drums come in at beat 13)
import numpy as np, json, subprocess, sys
sr = 22050; hop = 256; dt = hop / sr; win = 1024
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', sys.argv[1], '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
n = len(x) // hop
X = np.lib.stride_tricks.sliding_window_view(np.pad(x, (0, win)), win)[::hop][:n]
S = np.abs(np.fft.rfft(X * np.hanning(win), axis=1)); f = np.fft.rfftfreq(win, 1 / sr)
L = np.log1p(S); D = np.maximum(0, np.diff(L, axis=0, prepend=L[:1]))
bands = {'K': (30, 110), 'B': (110, 300), 'S': (300, 1200), 'V': (500, 2500), 'H': (5000, 11000)}
O = {k: D[:, (f >= a) & (f < b)].sum(1) for k, (a, b) in bands.items()}
for k in O: O[k] = O[k] / np.percentile(O[k][int(30 / dt):int(158 / dt)], 99.5)
P = 60 / 116; OFF = 30.445
J = {k: [round(float(O[k][max(0, int(round((OFF + st / 4 * P) / dt)) - 2):int(round((OFF + st / 4 * P) / dt)) + 3].max()), 3) for st in range(250 * 4)] for k in O}
json.dump(J, open('steps.json', 'w'))
