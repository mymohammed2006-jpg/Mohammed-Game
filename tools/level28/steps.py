#!/usr/bin/env python3
# Ya Tayeb El Galb (main level 28): onset strength of the level's song per quarter beat, per band -> steps.json (read by mkdata.py).
# python3 steps.py   (reads songs/SONG_L28.mp3 = the song from 3:20.000; beat 0 = 0.345 s of the file, 132.06 BPM)
import numpy as np, json, subprocess, os
HERE = os.path.dirname(os.path.abspath(__file__)); R = os.path.join(HERE, '..', '..')
sr = 22050; hop = 256; dt = hop / sr; win = 1024
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', os.path.join(R, 'songs', 'SONG_L28.mp3'), '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
n = len(x) // hop
X = np.lib.stride_tricks.sliding_window_view(np.pad(x, (0, win)), win)[::hop][:n]
S = np.abs(np.fft.rfft(X * np.hanning(win), axis=1)); f = np.fft.rfftfreq(win, 1 / sr)
L = np.log1p(S); D = np.maximum(0, np.diff(L, axis=0, prepend=L[:1]))
bands = {'K': (30, 120), 'B': (120, 350), 'S': (350, 1500), 'V': (1500, 4000), 'H': (6000, 11000)}
O = {k: D[:, (f >= a) & (f < b)].sum(1) for k, (a, b) in bands.items()}
for k in O: O[k] = O[k] / np.percentile(O[k][:int(122 / dt)], 99.5)
P = 60 / 132.06; OFF = 0.345
J = {k: [round(float(O[k][max(0, int(round((OFF + st / 4 * P) / dt)) - 2):int(round((OFF + st / 4 * P) / dt)) + 4].max()), 3) for st in range(270 * 4)] for k in O}
# the voice: energy 1500-4000 Hz per quarter beat (dB, relative), for where the singing is
E = (S[:, (f >= 1500) & (f < 4000)] ** 2).sum(1)
J['VE'] = [round(float(10 * np.log10(E[int((OFF + st / 4 * P) / dt):int((OFF + (st + 1) / 4 * P) / dt)].mean() + 1e-9)), 1) for st in range(270 * 4)]
json.dump(J, open(os.path.join(HERE, 'steps.json'), 'w'))
