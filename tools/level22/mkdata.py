#!/usr/bin/env python3
# Shiawase VIP (main level 22): the song's hits in beats, picked from steps.json -> src/Game/Dodge/L22data.cs
# K kick, B bass (the wobble), S snare, M the chops (mids), H hats. A hit = a local peak over the threshold, at least gap beats after the last one.
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
def inbars(l, bars): return [x for x in l if any(b <= x < b + 4 for b in bars)]
D = {
    'SW_GROW': u(pick('B', 1, 17, 0.55), pick('K', 1, 17, 0.6), gap=0.5),          # 0:30.5 the cracks grow on the bass
    'SW_CHOP1': pick('M', 10, 17, 0.5, 0.5),                                          # 0:34.1 the tiles fall on the chops
    'SW_RAD': u(pick('B', 17, 85, 0.6), pick('K', 17, 85, 0.75), gap=1),              # 0:36.9 the plate's cracks
    'SW_SNR1': pick('S', 17, 85, 0.75, 1),                                            # 0:36.9 the plate's rim chips (snare)
    'SW_RAIN': inbars(u(pick('B', 117, 149, 0.6), pick('K', 117, 149, 0.75), gap=0.5), [117, 125, 133, 141, 145]),  # 1:16.9 bass bars
    'SW_CHOP2': inbars(pick('M', 121, 149, 0.45, 0.5), [121, 129, 137]),             # 1:18.5 chop bars
    'SW_DOWN': u(pick('B', 149, 177, 0.6), pick('K', 149, 177, 0.75), gap=1),         # 1:29.7 rows from the top (bass)
    'SW_UP': pick('S', 149, 177, 0.6, 1),                                             # 1:29.7 rows from below (snare)
    'SW_KICK3': pick('K', 182, 213, 0.75, 1),                                         # 1:42.5 the eyes burn on the kicks
    'SW_SNR3': pick('S', 182, 213, 0.75, 1),                                          # 1:42.5 the chin's fans (snare)
    'SW_HAT': pick('H', 245, 285, 0.6, 0.5),                                          # 2:08.1 shards on the gold rails (hats)
    'SW_WOB': u(pick('B', 285, 341, 0.6), pick('K', 285, 341, 0.7), gap=0.5),         # 2:24.1 the corridor bends on the wobble
    'SW_SNR4': pick('S', 285, 341, 0.75, 1),                                          # 2:24.1 the corridor's spikes (snare)
    'SW_KICK5': pick('K', 341, 371, 0.55, 1),                                         # 2:46.5 the seams fire (kick)
}
s = 'namespace MG;\n// Shiawase VIP (main level 22): the song\'s hits in beats, made by tools/level22/mkdata.py from steps.json\npublic static partial class G\n{\n'
for k, v in D.items(): s += f'\tstatic readonly double[] {k} = {{ {", ".join("%g" % x for x in v)} }};\n'
s += '}\n'
open(os.path.join(HERE, '..', '..', 'src', 'Game', 'Dodge', 'L22data.cs'), 'w').write(s); print(s)
