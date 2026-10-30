#!/usr/bin/env python3
# Trouble (main level 29): the song's hits, from songs/SONG_L29.mp3 (own onset analysis, numpy only).
# Grid: 154 BPM, beat 0 = 0.371 s of the file. For each quarter beat: the onset strength of the low beatbox (30-150 Hz, kick),
# the high noise (2-8 kHz, snare) and the voice (300-2000 Hz), each against the 90th percentile of the 16 beats around it.
# Prints per section the quarter beats that reach 8.5/9 (at least 0.5 beat apart); src/Game/Dodge/L29data.cs holds the ones the level uses
# (TB_S1: snares 36-79, TB_KB: kicks of the breakdown 104-136 (+ the voice at 131.5), TB_SX: snares 232-264, TB_K3: kicks 288-328).
import numpy as np, subprocess, os
R = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
sr, hop, n = 22050, 256, 2048; fps = sr / hop
x = np.frombuffer(subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', os.path.join(R, 'songs', 'SONG_L29.mp3'), '-ac', '1', '-ar', str(sr), '-f', 'f32le', '-'], capture_output=True).stdout, dtype=np.float32)
win = np.hanning(n); M = np.array([np.abs(np.fft.rfft(x[i:i + n] * win)) for i in range(0, len(x) - n, hop)])
fq = np.fft.rfftfreq(n, 1 / sr)
def bandflux(a, b):
    X = np.log1p(M[:, (fq >= a) & (fq < b)] * 20); return np.maximum(0, np.diff(X, axis=0, prepend=X[:1])).sum(1)
P = 60 / 154; off = 0.371; NB = 360
G = []
for f in (bandflux(30, 150), bandflux(2000, 8000), bandflux(300, 2000)):
    v = np.array([f[max(0, int(round((off + k / 4 * P) * fps)) - 2):int(round((off + k / 4 * P) * fps)) + 3].max() for k in range(NB * 4)])
    G.append(np.array([v[k] / (np.percentile(v[max(0, k - 32):k + 32], 90) + 1e-9) for k in range(len(v))]))
for nm, row in (('K', 0), ('S', 1), ('V', 2)):
    for a, b in ((0, 8), (8, 36), (36, 64), (64, 104), (104, 136), (136, 164), (164, 200), (200, 232), (232, 264), (264, 288), (288, 328), (328, 360)):
        out, last = [], -9
        for k in range(a * 4, b * 4):
            if G[row][k] * 9 >= 8.5 and k / 4 - last >= 0.5 - 1e-9: out.append(k / 4); last = k / 4
        print(nm, a, b, ' '.join(f'{v:g}' for v in out))
