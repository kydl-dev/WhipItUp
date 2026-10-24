"""Joins previews/anim/<clip>/{q,t}_fNN.png into previews/anim/<clip>_sheet.png: one column per frame (3/4 view above, top view
below), frame number written on each. Usage: python anim_sheet.py <previews/anim dir> <clip>"""
import os
import re
import sys

from PIL import Image, ImageDraw

base, clip = sys.argv[1], sys.argv[2]
d = os.path.join(base, clip)
frames = sorted({int(m.group(1)) for f in os.listdir(d) for m in [re.match(r"q_f(\d+)\.png", f)] if m})
ims = {(v, f): Image.open(os.path.join(d, "%s_f%02d.png" % (v, f))).convert("RGB") for v in "qt" for f in frames}
w, h = ims[("q", frames[0])].size
sheet = Image.new("RGB", (w * len(frames), h * 2), "white")
dr = ImageDraw.Draw(sheet)
for i, f in enumerate(frames):
    for j, v in enumerate("qt"):
        sheet.paste(ims[(v, f)], (i * w, j * h))
    dr.text((i * w + 8, 6), "f%d" % f, fill=(200, 0, 0))
out = os.path.join(base, clip + "_sheet.png")
sheet.save(out)
print("sheet", out, sheet.size)
