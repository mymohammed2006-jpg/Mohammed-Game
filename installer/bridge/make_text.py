# Draws the bridge window's three Arabic messages into PNGs (white, Tajawal 800, twice the size they show at).
# Pictures, because Windows' own text drawing (GDI+) placed the right-to-left line off centre.
# Needs Pillow with raqm (Arabic shaping).  Run: python3 make_text.py
from PIL import Image, ImageDraw, ImageFont
MSGS = {'msg_update.png': 'جاري التحديث', 'msg_done.png': 'خلص التحديث، اللعبة تفتح من جديد', 'msg_fail.png': 'ما كمل التحديث، اللعبة تفتح الحين'}
font = ImageFont.truetype('../../assets/fonts/tajawal800_ar.woff2', 42, layout_engine=ImageFont.Layout.RAQM)
for name, text in MSGS.items():
    l, t, r, b = font.getbbox(text, direction='rtl', language='ar')
    img = Image.new('RGBA', (r - l + 8, 64), (255, 255, 255, 0))
    ImageDraw.Draw(img).text((4 - l, 32), text, font=font, fill='white', anchor='lm', direction='rtl', language='ar')
    img.save(name); print(name, img.size)
