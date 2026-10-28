# The letters of "أجمل غرامي" as outlines: shaped with HarfBuzz from the game's own Tajawal 800 (assets/fonts/tajawal800_ar.woff2),
# flattened to points (font units, y up). Used by gen2.py (pip: fonttools brotli uharfbuzz).
import os, io, math
import uharfbuzz as hb
from fontTools.ttLib import TTFont
from fontTools.pens.basePen import BasePen
HERE = os.path.dirname(os.path.abspath(__file__))
FONT = os.path.join(HERE, '..', '..', 'assets', 'fonts', 'tajawal800_ar.woff2')
_tt = TTFont(FONT); _tt.flavor = None; _buf = io.BytesIO(); _tt.save(_buf); _data = _buf.getvalue()
_face = hb.Face(hb.Blob(_data)); _font = hb.Font(_face); _gs = _tt.getGlyphSet()

class _Flat(BasePen):
    def __init__(s, gs, step): super().__init__(gs); s.cs = []; s.c = None; s.step = step
    def _moveTo(s, p): s.c = [p]; s.cs.append(s.c)
    def _lineTo(s, p): s.c.append(p)
    def _curveToOne(s, a, b, c):
        p0 = s.c[-1]; n = max(2, int(math.dist(p0, c) / s.step))
        for i in range(1, n + 1):
            t = i / n; u = 1 - t
            s.c.append((u**3*p0[0]+3*u*u*t*a[0]+3*u*t*t*b[0]+t**3*c[0], u**3*p0[1]+3*u*u*t*a[1]+3*u*t*t*b[1]+t**3*c[1]))
    def _qCurveToOne(s, a, b):
        p0 = s.c[-1]; n = max(2, int(math.dist(p0, b) / s.step))
        for i in range(1, n + 1):
            t = i / n; u = 1 - t
            s.c.append((u*u*p0[0]+2*u*t*a[0]+t*t*b[0], u*u*p0[1]+2*u*t*a[1]+t*t*b[1]))
    def _closePath(s):
        if s.c and len(s.c) > 1 and math.dist(s.c[0], s.c[-1]) < 1e-6: s.c.pop()
    _endPath = _closePath

def area(c): return 0.5 * sum(c[i-1][0]*c[i][1] - c[i][0]*c[i-1][1] for i in range(len(c)))

def shape(text, step=28):
    """[(x advance start, [contours])] in visual order left to right, font units"""
    buf = hb.Buffer(); buf.add_str(text); buf.guess_segment_properties(); hb.shape(_font, buf)
    out = []; x = 0
    for inf, pos in zip(buf.glyph_infos, buf.glyph_positions):
        name = _tt.getGlyphName(inf.codepoint); pen = _Flat(_gs, step); _gs[name].draw(pen)
        cs = [[(x + pos.x_offset + px, pos.y_offset + py) for px, py in c] for c in pen.cs if len(c) > 2]
        out.append((x, pos.x_advance, cs)); x += pos.x_advance
    return out, x

if __name__ == '__main__':
    for t in ['غ', 'ر', 'ا', 'م', 'ي', 'أجمل', 'غرامي', 'أجمل غرامي']:
        g, w = shape(t)
        print(t, 'width', w, [(len(cs), [round(area(c)) for c in cs], (round(min(p[1] for c in cs for p in c)) if cs else None, round(max(p[1] for c in cs for p in c)) if cs else None)) for x, a, cs in g])
