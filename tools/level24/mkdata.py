#!/usr/bin/env python3
# يالعين قسامي (main level 24): the song's hits in beats, picked from steps.json -> src/Game/Dodge/L24data.cs
# K kick, S snare (the claps), H hats. A hit = a local peak over the threshold, at least gap beats after the last one.
import json, os
HERE = os.path.dirname(os.path.abspath(__file__))
J = json.load(open(os.path.join(HERE, 'steps.json')))
def pick(k, a, b, thr, gap=0.25):
    out = []; last = -9; v = J[k]
    for st in range(int(a * 4), int(b * 4)):
        x = v[st]
        if x >= thr and x >= v[st - 1] and x >= v[min(len(v) - 1, st + 1)] and st / 4 - last >= gap: out.append(st / 4); last = st / 4
    return out
D = {
    'QS_KICK': pick('K', 0, 342, 0.7, 0.5),     # the strong kicks
    'QS_SNR': pick('S', 0, 342, 0.75, 0.5),     # the strong claps
    'QS_HAT': pick('H', 0, 342, 0.7, 0.5),      # the hats
}
out = ['namespace MG;', '// يالعين قسامي (main level 24): the song\'s hits in beats, made by tools/level24/mkdata.py from steps.json', 'public static partial class G', '{']
for k, v in D.items(): out.append(f'\tstatic readonly double[] {k} = {{ ' + ', '.join(f'{x:g}' for x in v) + ' };')
out.append('}')
open(os.path.join(HERE, '..', '..', 'src', 'Game', 'Dodge', 'L24data.cs'), 'w').write('\n'.join(out) + '\n')
for k, v in D.items(): print(k, len(v))
