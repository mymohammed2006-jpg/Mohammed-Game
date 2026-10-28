# Boss 2 "رقصة الموت" (La Danse Macabre): the song's notes in beats -> src/Game/Dodge/B2data.cs
# hits.json comes from hits.py (numpy only): ffmpeg -i song.mp3 -ac 1 -ar 22050 -f f32le song.f32; python3 hits.py song.f32 hits.json
import json, os
HERE = os.path.dirname(os.path.abspath(__file__)); H = json.load(open(os.path.join(HERE, 'hits.json')))
lead = list(zip(H['lead'], H['leadPitch']))
def pick(a, b, gap):
    out = []
    for q, p in lead:
        if a <= q < b and (not out or q - out[-1][0] >= gap - 1e-9): out.append((q, p))
    return out
def ranks(xs, n):
    # pitch rank -> 0..n-1 (ties broken by order), then no lane twice in a row
    o = sorted(range(len(xs)), key=lambda i: (xs[i][1], i)); r = [0] * len(xs)
    for k, i in enumerate(o): r[i] = min(n - 1, k * n // len(xs))
    for i in range(1, len(r)):
        if r[i] == r[i - 1]: r[i] = (r[i] + (1 if i % 2 else n - 1)) % n
    return r
intro = pick(2, 40, 0.5)
plk = pick(40, 64, 0.5); plkS = ranks(plk, 4)
pin = pick(208, 248, 0.5); pinL = ranks(pin, 8)
def arr(v): return ', '.join(('%g' % x) for x in v)
src = f'''namespace MG;
// Boss 2 "رقصة الموت" (Shirobon - La Danse Macabre): the song's notes in beats (onsets measured with numpy), made by tools/boss2/mkdata.py
public static partial class G
{{
	// the intro's notes (b2-40): a note leaves the clock on each
	static readonly double[] B2_INTRO = {{ {arr(q for q, _ in intro)} }};
	// the violin's notes (b40-64) and which string each plucks (by the note's pitch: 0 low .. 3 high)
	static readonly double[] B2_PLK = {{ {arr(q for q, _ in plk)} }};
	static readonly int[] B2_PLKS = {{ {arr(plkS)} }};
	// the music box (b208-248): a pin on each note, its lane by the pitch (0 low .. 7 high)
	static readonly double[] B2_PIN = {{ {arr(q for q, _ in pin)} }};
	static readonly int[] B2_PINL = {{ {arr(pinL)} }};
}}
'''
open(os.path.join(HERE, '..', '..', 'src', 'Game', 'Dodge', 'B2data.cs'), 'w').write(src.replace('\n', '\r\n') if False else src)
print('intro', len(intro), 'plk', len(plk), 'pin', len(pin))
