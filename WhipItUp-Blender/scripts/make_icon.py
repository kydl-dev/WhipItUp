"""Builds the Thunderstore icon (256x256) WhipItUp-Mod/icon.png from renders/_icon_weapons.png (Blender render of the three
weapons fanned, transparent background; made by the T-014 icon scene) + the mod name under it in the game's Norse font.
Run: python3 make_icon.py  (from anywhere; paths are relative to this file). Pillow needed."""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(HERE, "..", "renders", "_icon_weapons.png")
FONT = os.path.join(ROOT, "WhipItUp-Unity", "Assets", "Kydl.Ui", "Preview", "GameAssets", "AveriaSerifLibre-Bold.ttf")   # the game's UI font; Norse has no lower case (user: only the W capital)
OUT = os.path.join(ROOT, "WhipItUp-Mod", "icon.png")
S = 4 * 256                      # draw at 4x, scale down at the end (smooth edges)
BG_TOP, BG_BOTTOM = (38, 30, 24), (14, 11, 9)
GOLD, GOLD_DARK = (232, 186, 96), (120, 84, 34)
NAME = "Whip it up"          # user 2026-10-24: written like this, on a concave (smile) arch of 30 deg
ARCH_DEG = 30.0


def draw_arched(img, text, font, bottom):
    """The whole name bent as ONE piece onto a concave arch (smile: ends up, middle down) spanning ARCH_DEG degrees.
    The text is drawn straight on its own layer, then every pixel is mapped onto a circle whose centre is above the text
    (column = angle along the arc, row = distance from the centre), so the letters, spaces and shadow all follow one curve.
    'bottom' = lowest pixel of the middle of the name."""
    import numpy as np
    pad = 20
    l, t, r, b = font.getbbox(text, stroke_width=4)
    tw, th = r - l + 2 * pad, b - t + 2 * pad
    flat = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    fd = ImageDraw.Draw(flat)
    fd.text((pad - l + 5, pad - t + 6), text, font=font, fill=(0, 0, 0, 200))                  # drop shadow
    fd.text((pad - l, pad - t), text, font=font, fill=GOLD, stroke_width=4, stroke_fill=GOLD_DARK)
    src = np.asarray(flat).astype(np.float32)
    radius = tw / math.radians(ARCH_DEG)               # radius of the text's top edge: the full width spans ARCH_DEG
    cx, cy = S / 2.0, bottom - th - radius             # circle centre above the name
    ys, xs = np.mgrid[0:S, 0:S].astype(np.float32)
    dx, dy = xs - cx, ys - cy
    rr = np.sqrt(dx * dx + dy * dy)
    theta = np.arctan2(dx, dy)                         # 0 = straight down from the centre, positive = to the right
    u = theta * radius + tw / 2.0                      # column in the flat text
    v = rr - radius                                    # row in the flat text (0 = top edge)
    ok = (u >= 0) & (u < tw - 1) & (v >= 0) & (v < th - 1) & (dy > 0)
    u0, v0 = np.floor(u).astype(int), np.floor(v).astype(int)
    fu, fv = (u - u0)[..., None], (v - v0)[..., None]
    u0, v0 = np.clip(u0, 0, tw - 2), np.clip(v0, 0, th - 2)
    out = (src[v0, u0] * (1 - fu) * (1 - fv) + src[v0, u0 + 1] * fu * (1 - fv)
           + src[v0 + 1, u0] * (1 - fu) * fv + src[v0 + 1, u0 + 1] * fu * fv)      # bilinear
    out[~ok] = 0
    img.alpha_composite(Image.fromarray(out.clip(0, 255).astype(np.uint8), "RGBA"))

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

    font = ImageFont.truetype(FONT, int(S * 0.17))
    draw_arched(img, NAME, font, bottom=int(S * 0.955))
    d = ImageDraw.Draw(img)
    d.rectangle([6, 6, S - 7, S - 7], outline=GOLD_DARK + (255,), width=10)   # thin frame
    img = img.resize((256, 256), Image.LANCZOS).convert("RGB")
    img.save(OUT)
    print("icon written:", OUT, img.size)


if __name__ == "__main__":
    main()
