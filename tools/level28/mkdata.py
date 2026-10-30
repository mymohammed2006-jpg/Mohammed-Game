#!/usr/bin/env python3
# Ya Tayeb El Galb (main level 28): the song's hits in beats, picked from steps.json -> src/Game/Dodge/L28data.cs
# K kick, B the low drum/bass, S the snare/clap and the oud (mids), V the voice band, H hats. A hit = a local peak over the threshold, at least gap beats after the last one.
import json, os
HERE = os.path.dirname(os.path.abspath(__file__))
J = json.load(open(os.path.join(HERE, 'steps.json')))
def pick(k, a, b, thr, gap=0.25):
    out = []; last = -9; v = J[k]
    for st in range(int(a * 4), int(b * 4)):
        x = v[st]
        if x >= thr and x >= v[st - 1] and x >= v[min(len(v) - 1, st + 1)] and st / 4 - last >= gap: out.append(st / 4); last = st / 4
    return out
def u(*ls, gap=0.5):
    out = []; last = -9
    for x in sorted(set(x for l in ls for x in l)):
        if x - last >= gap: out.append(x); last = x
    return out
D = {
    'TQ_PULSE': u(pick('K', 0, 266, 0.6), gap=1),                              # the heart beats on the kick (looks only)
    'TQ_ECG': u(pick('K', 1, 32, 0.6), pick('B', 1, 32, 0.7), gap=1.5),        # 3:20.3 the monitor's spikes (kick)
    'TQ_WAVE': u(pick('K', 32, 48, 0.6), pick('B', 32, 48, 0.7), gap=1.5),     # 3:34.9 the heart's waves (kick)
    'TQ_BURST': pick('S', 48, 56, 0.6, 1),                                     # 3:42.2 the heart bursts (claps)
    'TQ_WAVE2': u(pick('K', 56, 64, 0.6), pick('B', 56, 64, 0.7), gap=1.5),    # 3:45.8 waves again
    'TQ_WALL': u(pick('K', 64, 96, 0.6), gap=2),                               # 3:49.4 the pulse walls (kick)
    'TQ_BITE': pick('S', 96, 128, 0.6, 2),                                     # 4:04.0 the hearts with a bite (claps)
    'TQ_KICK2': u(pick('K', 128, 160, 0.6), gap=1.5),                          # 4:18.5 the rose's petals (kick)
    'TQ_RAIN': u(pick('K', 160, 184, 0.6), pick('H', 160, 184, 0.7), gap=1),   # 4:33.0 the petal rain
    'TQ_BEAM': u(pick('K', 184, 216, 0.6), gap=1.5),                           # 4:43.9 the light beams (kick)
    'TQ_HAT3': pick('H', 216, 248, 0.5, 0.5),                                  # 4:58.5 the spiral (hats)
    'TQ_KICK3': u(pick('K', 216, 248, 0.6), gap=2),                            # 4:58.5 the waves in the spiral (kick)
}
s = 'namespace MG;\n// Ya Tayeb El Galb (main level 28): the song\'s hits in beats, made by tools/level28/mkdata.py from steps.json\npublic static partial class G\n{\n'
for k, v in D.items(): s += f'\tstatic readonly double[] {k} = {{ {", ".join("%g" % x for x in v)} }};\n'
s += '}\n'
open(os.path.join(HERE, '..', '..', 'src', 'Game', 'Dodge', 'L28data.cs'), 'w').write(s); print(s)
