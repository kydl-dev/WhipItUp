"""Builds the Thunderstore icon (256x256) WhipItUp-Mod/icon.png from renders/_icon_weapons.png (Blender render of the three
weapons fanned, transparent background; made by the T-014 icon scene) + the mod name under it in the game's Norse font.
Run: python3 make_icon.py  (from anywhere; paths are relative to this file). Pillow needed."""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(HERE, "..", "renders", "_icon_weapons.png")
FONT = os.path.join(ROOT, "WhipItUp-Unity", "Assets", "Kydl.Ui", "Preview", "GameAssets", "Norsebold.otf")
OUT = os.path.join(ROOT, "WhipItUp-Mod", "icon.png")
S = 4 * 256                      # draw at 4x, scale down at the end (smooth edges)
BG_TOP, BG_BOTTOM = (38, 30, 24), (14, 11, 9)
GOLD, GOLD_DARK = (232, 186, 96), (120, 84, 34)
NAME = "Whip it up"          # user 2026-10-24: written like this, on a concave (smile) arch of 30 deg
ARCH_DEG = 30.0


def draw_arched(img, text, font, bottom):
    """Text on a concave arch (ends up, middle down) spanning ARCH_DEG degrees; every letter turned to follow the curve.
    The letters sit on a circle whose centre is above the text; 'bottom' = lowest pixel of the middle letters."""
    d = ImageDraw.Draw(img)
    widths = [d.textlength(c, font=font) for c in text]
    total = sum(widths)
    radius = total / math.radians(ARCH_DEG)
    asc = font.getbbox("W")[3]
    cx, cy = S / 2, bottom - asc - radius            # circle centre above the text
    pos = -total / 2
    for c, w in zip(text, widths):
        mid = pos + w / 2
        pos += w
        if c == " ":
            continue
        theta = mid / radius                           # radians, 0 = straight down from the centre
        size = int(max(w, asc) * 2.2)
        layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        ox, oy = (size - w) / 2, size / 2 - asc / 2  # letter centred on the layer (rotation centre)
        ld.text((ox + 5, oy + 6), c, font=font, fill=(0, 0, 0, 200))
        ld.text((ox, oy), c, font=font, fill=GOLD, stroke_width=4, stroke_fill=GOLD_DARK)
        layer = layer.rotate(-math.degrees(theta), resample=Image.BICUBIC)   # PIL turns counter-clockwise on screen; left letters lean right
        bx = cx + (radius + asc / 2) * math.sin(theta)    # centre of the letter (half a cap height inside the circle)
        by = cy + (radius + asc / 2) * math.cos(theta)
        img.alpha_composite(layer, (int(bx - size / 2), int(by - size / 2)))


def main():
    img = Image.new("RGBA", (S, S))
    d = ImageDraw.Draw(img)
    for y in range(S):                                   # vertical gradient, warm dark leather tones
        t = y / (S - 1)
        d.line([(0, y), (S, y)], fill=tuple(int(a + (b - a) * t) for a, b in zip(BG_TOP, BG_BOTTOM)) + (255,))
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))          # soft light behind the weapons
    ImageDraw.Draw(glow).ellipse([S * 0.12, S * 0.05, S * 0.88, S * 0.75], fill=(255, 200, 120, 46))
    img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(S * 0.08)))

    w = Image.open(SRC).convert("RGBA")
    w = w.crop(w.getbbox())
    box = int(S * 0.88)                                  # weapons area (width-limited: the weapons stand side by side)
    k = min(box / w.width, int(S * 0.70) / w.height)
    w = w.resize((int(w.width * k), int(w.height * k)), Image.LANCZOS)
    shadow = Image.new("RGBA", w.size, (0, 0, 0, 0))
    shadow.putalpha(w.getchannel("A").point(lambda a: int(a * 0.6)))
    x, y = (S - w.width) // 2, int(S * 0.025)
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(10)), (x + 10, y + 14))
    img.alpha_composite(w, (x, y))

    font = ImageFont.truetype(FONT, int(S * 0.185))
    draw_arched(img, NAME, font, bottom=int(S * 0.955))
    d = ImageDraw.Draw(img)
    d.rectangle([6, 6, S - 7, S - 7], outline=GOLD_DARK + (255,), width=10)   # thin frame
    img = img.resize((256, 256), Image.LANCZOS).convert("RGB")
    img.save(OUT)
    print("icon written:", OUT, img.size)


if __name__ == "__main__":
    main()
