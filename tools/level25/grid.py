#!/usr/bin/env python3
# شيلة حان الوعد يا لابتي (main level 25): the beat grid (BPM and beat 0) from the kick's onsets, and where the kick drops out (the breaks).
# python3 grid.py songs/SONG_L25.mp3   -> 134.7 BPM, beat 0 = 0.38 s; the breaks (no kick on the odd beats): b88-102, b184-196, b280-294, b374-390
import numpy as np, subprocess, sys
sr = 22050; hop = 128; dt = hop / sr; win = 1024
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', sys.argv[1], '-t', '186', '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
n = len(x) // hop
X = np.lib.stride_tricks.sliding_window_view(np.pad(x, (0, win)), win)[::hop][:n]
S = np.abs(np.fft.rfft(X * np.hanning(win), axis=1)); f = np.fft.rfftfreq(win, 1 / sr)
L = np.log1p(S); D = np.maximum(0, np.diff(L, axis=0, prepend=L[:1]))
k = D[:, (f >= 30) & (f < 120)].sum(1); k = k / np.percentile(k, 99.5)
def score(bpm, off):
    P = 60 / bpm; ts = np.arange(off, 184, P); return k[(ts / dt).round().astype(int)].mean()
best = max(((score(b, o), b, o) for b in np.arange(133.5, 135.5, 0.05) for o in np.arange(0, 60 / b, 0.005)))
_, bpm, off = best; P = 60 / bpm; print(f'{bpm:.2f} BPM, beat 0 = {off:.3f} s')
def at(b): i = int(round((off + b * P) / dt)); return k[max(0, i - 1):i + 3].max()
print('no kick on beats:', [b for b in range(int((184 - off) / P)) if at(b) < 0.35])
