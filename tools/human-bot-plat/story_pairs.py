#!/usr/bin/env python3
# Puts the before/after clips (pair.sh) into the story video (story.sh): each pair goes in at its frame, and the part of
# the story the pair already shows is left out.   python3 story_pairs.py <storyDir> <pairsDir> <out.mp4>
import subprocess, sys, os
S, P, OUT = sys.argv[1:4]
clips = [tuple(map(int, c.split('-'))) for c in open(f'{S}/clips.txt').read().split(',')]
# (frame where the pair goes in, story frames left out after it (first,last) or None, the pair's files)
PAIRS = [(6380, (6380, 6760), ['page_old', 'page_new']),
         (7311, (7311, 7629), ['stack_old', 'stack_new']),
         (37860, None, ['fall_old', 'fall_new']),
         (38935, (38935, 39060), ['door_old', 'door_new']),
         (44440, (44440, 44800), ['win_old', 'win_new'])]
tmp = f'{P}/seg'; os.makedirs(tmp, exist_ok=True); parts = []; k = 0
def cut(n, a, x, y):   # frames x..y of story clip n (which starts at frame a), with short fades
    global k; k += 1; o = f'{tmp}/{k}.mp4'; d = (y - x + 1) / 60; e = max(0, d - 0.25)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-ss', f'{(x - a) / 60}', '-t', f'{d}', '-i', f'{S}/c/{n}.mp4',
                    '-vf', f'fade=in:st=0:d=0.2,fade=out:st={e}:d=0.25', '-af', f'afade=in:st=0:d=0.15,afade=out:st={e}:d=0.25',
                    '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', '-c:a', 'aac', '-b:a', '192k', '-ar', '44100', o], check=True)
    parts.append(o)
for n, (a, b) in enumerate(clips, 1):
    here = [p for p in PAIRS if a <= p[0] <= b]
    if not here: parts.append(f'{S}/c/{n}.mp4'); continue
    x = a
    for at, skip, files in here:
        if at > x: cut(n, a, x, at - 1)
        parts += [f'{P}/{f}.mp4' for f in files]
        x = skip[1] + 1 if skip else at
    if x <= b: cut(n, a, x, b)
open(f'{tmp}/list.txt', 'w').write(''.join(f"file '{p}'\n" for p in parts))
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', f'{tmp}/list.txt', '-c', 'copy', OUT], check=True)
print(subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', OUT], capture_output=True, text=True).stdout)
