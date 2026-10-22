#!/usr/bin/env python3
# Reads the song (songs/SONG_H3.mp3) and writes song.json for gen.py: the music box notes of the opening (beat, MIDI note, on a quarter
# beat) and the energy of every bar (0-1). 112 BPM, beat 0 = 0.09 s. Needs librosa (pip install librosa); run once.
import json, os, numpy as np, librosa
HERE = os.path.dirname(os.path.abspath(__file__))
P, O = 60 / 112, 0.09
y, sr = librosa.load(os.path.join(HERE, '..', '..', 'songs', 'SONG_H3.mp3'), sr=22050)
seg = y[:int(20 * sr)]; hm = librosa.effects.harmonic(seg)
on = librosa.onset.onset_detect(y=hm, sr=sr, units='time', delta=0.05)
C = np.abs(librosa.cqt(hm, sr=sr, fmin=librosa.note_to_hz('C3'), n_bins=48)); tt = librosa.frames_to_time(np.arange(C.shape[1]), sr=sr)
notes = {}
for t in on:
    b = round((t - O) / P * 4) / 4
    if b > 14.5: continue
    f = np.searchsorted(tt, t + 0.04); k = int(np.argmax(C[:, f:f + 3].mean(1)))
    notes.setdefault(b, 48 + k)
S = np.abs(librosa.stft(y, hop_length=512)); f = librosa.fft_frequencies(sr=sr); ft = librosa.frames_to_time(np.arange(S.shape[1]), sr=sr, hop_length=512)
lo = S[f < 400].sum(0); en = []
for bar in range(int((len(y) / sr - O) / (4 * P)) + 1):
    m = (ft >= O + bar * 4 * P) & (ft < O + (bar + 1) * 4 * P); en.append(float(lo[m].mean()) if m.any() else 0)
mx = max(en); en = [round(min(1, 0.15 + 0.85 * v / mx), 3) for v in en]
json.dump({'notes': sorted([[b, m] for b, m in notes.items()]), 'energy': en}, open(os.path.join(HERE, 'song.json'), 'w'))
print(len(notes), 'notes', len(en), 'bars')
