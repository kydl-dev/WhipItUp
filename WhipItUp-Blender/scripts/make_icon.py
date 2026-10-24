"""Builds the Thunderstore icon (256x256) WhipItUp-Mod/icon.png from renders/_icon_weapons.png (Blender render of the three
weapons fanned, transparent background; made by the T-014 icon scene) + the mod name under it in the game's Norse font.
Run: python3 make_icon.py  (from anywhere; paths are relative to this file). Pillow needed."""
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
NAME = "WhipItUp"


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
    box = int(S * 0.80)                                  # weapons area: top 80 % of the icon
    k = min(box / w.width, box / w.height)
    w = w.resize((int(w.width * k), int(w.height * k)), Image.LANCZOS)
    shadow = Image.new("RGBA", w.size, (0, 0, 0, 0))
    shadow.putalpha(w.getchannel("A").point(lambda a: int(a * 0.6)))
    x, y = (S - w.width) // 2, int(S * 0.02)
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(10)), (x + 10, y + 14))
    img.alpha_composite(w, (x, y))

    font = ImageFont.truetype(FONT, int(S * 0.15))
    d = ImageDraw.Draw(img)
    tw = d.textlength(NAME, font=font)
    ty = int(S * 0.815)
    d.text(((S - tw) / 2 + 5, ty + 6), NAME, font=font, fill=(0, 0, 0, 200))      # drop shadow
    d.text(((S - tw) / 2, ty), NAME, font=font, fill=GOLD, stroke_width=4, stroke_fill=GOLD_DARK)
    d.rectangle([6, 6, S - 7, S - 7], outline=GOLD_DARK + (255,), width=10)   # thin frame
    img = img.resize((256, 256), Image.LANCZOS).convert("RGB")
    img.save(OUT)
    print("icon written:", OUT, img.size)


if __name__ == "__main__":
    main()
