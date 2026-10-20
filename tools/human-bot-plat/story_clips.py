#!/usr/bin/env python3
# The story video's cut: from a STORY run's events.log, the frames worth showing (the failed tries up to the fall and the
# restart, the demo while waiting, each solving try to its room's end, the win screen); replays and long waits are left out.
#   python3 story_clips.py <events.log>  ->  prints "a-b,c-d,..." (60 fps frames)
import re, sys

ev = []
for l in open(sys.argv[1], encoding='utf-8'):
    m = re.match(r'@(\d+) \[[^\]]*\] (.*)', l.strip())
    if m: ev.append((int(m[1]), m[2]))
end = ev[-1][0]

tries = []
for f, s in ev:
    m = re.match(r'(RESTART -> |ROOM )(\d+) try (\d+).*STORY (\w+)', s)
    if m:
        tries.append(dict(start=f, room=int(m[2]), kind=m[4], restart=m[1].startswith('RESTART'), go=None, downs=[], ups=[], back=[], next=None, won=None, demo=[], nosafe=[]))
        continue
    if not tries: continue
    t = tries[-1]
    if s.startswith('go ') and t['go'] is None: t['go'] = f
    elif s.startswith('DOWN'): t['downs'].append(f)
    elif s.startswith('UP'): t['ups'].append(f)
    elif s.startswith('back up'): t['back'].append(f)
    elif s.startswith('NEXT'): t['next'] = f
    elif s.startswith('WON'): t['won'] = f
    elif s.startswith('DEMO'): t['demo'].append(f)
    elif s.startswith('no safe way'): t['nosafe'].append(f)

clips = []
shown_wait = False
for i, t in enumerate(tries):
    nxt = tries[i + 1]['start'] if i + 1 < len(tries) else end
    go = t['go'] or t['start']
    if t['restart'] and tries[i - 1]['kind'] == 'fail':
        clips.append((t['start'] - 40, t['start'] + 200))          # the restart and its line
    if i == 0:
        clips.append((1, go))                                       # the opening lines while they wait for the beat
    if t['demo']:
        clips.append((t['demo'][0] - 10, t['demo'][-1] + 20))
    if t['kind'] == 'fail':
        a = go - 30
        if t['next'] is not None:                                   # the try went on to the room's end (a fall and a help)
            clips.append((a, t['next'] + 200))
        elif t['downs']:
            b = t['downs'][-1] + 240
            if t['ups']: b = max(b, t['ups'][-1] + 150)
            if t['nosafe'] and not shown_wait:                      # once: the whole 6 s wait until the page restarts
                shown_wait = True; b = nxt
            clips.append((a, min(b, nxt)))
    elif t['kind'] == 'solve':
        b = t['next'] + 200 if t['next'] is not None else end
        if t['won'] is not None: b = end
        clips.append((go - 60, min(b, end)))

clips = sorted((max(1, a), min(end, b)) for a, b in clips if b > a)
merged = []
for a, b in clips:
    if merged and a <= merged[-1][1] + 30: merged[-1] = (merged[-1][0], max(merged[-1][1], b))
    else: merged.append((a, b))
print(','.join(f'{a}-{b}' for a, b in merged))
print(f'{len(merged)} clips, {sum(b - a + 1 for a, b in merged) / 60:.1f} s', file=sys.stderr)
