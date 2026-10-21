"""Joins the preview renders of one object into <name>_sheet.png (run with system python: numpy/PIL)."""
import sys
from PIL import Image
name = sys.argv[1] if len(sys.argv) > 1 else "Paddle"
views = sys.argv[2:] or ["front", "side", "threequarter", "back"]
ims = [Image.open(f"renders/{name}_{v}.png").convert("RGB") for v in views]
s = Image.new("RGB", (sum(i.width for i in ims), ims[0].height))
x = 0
for i in ims:
    s.paste(i, (x, 0)); x += i.width
s.save(f"renders/{name}_sheet.png")
print("sheet", s.size)
