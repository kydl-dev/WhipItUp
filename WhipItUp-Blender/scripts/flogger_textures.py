"""Writes the flogger textures into WhipItUp-Blender/textures/ (numpy + PIL):
  flogger_Albedo_V1..V7.png (sRGB, per rank), flogger_Normal.png (+Y up), flogger_MetalGloss.png (R metallic, A smoothness).
Colours (D-029): falls = rank leather; the 2 braided falls and the grip braid = two-tone leather + rank contrast colour
(rank_palette.second_colour, the paddle medallion colour); collar / ferrule / pommel rim = fitting metal with a knotwork band;
pommel faces = the vanilla TrophyWolf icon (as on the paddle). Layout: flogger_layout.py.
Usage: python flogger_textures.py [out_dir]
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import flogger_layout as F  # noqa: E402
from rank_palette import PALETTE, second_colour  # noqa: E402
import paddle_textures as P  # noqa: E402  (shared helpers: rect_box/rect_grid/noise/interlace, the wolf icon path)

N = F.TEXTURE_SIZE
P.N = N


def log(msg):
    print("[flogger_textures] " + msg)


def braid(U, V, around, along):
    """Two-family diagonal braid. Returns (family A mask, height 0..1)."""
    a = U * around + V * along
    b = U * around - V * along
    fa, fb = a % 1.0, b % 1.0
    top_a = ((np.floor(a) + np.floor(b)) % 2) == 0
    ha = np.sin(math.pi * fa)
    hb = np.sin(math.pi * fb)
    h = np.where(top_a, ha * (0.55 + 0.45 * hb), hb * (0.55 + 0.45 * ha))
    gap = np.where(top_a, ha, hb) < 0.18
    return top_a, np.where(gap, 0.0, h)


def spiral_braid(U, V, around, along, cut):
    """Two-tone spiral braid like the photo's handle: slanted bands (alternating colours) cut into overlapping 'bricks' by
    the crossing direction, each brick rounded. Returns (colour A mask, height 0..1)."""
    a = U * around + V * along
    b = U * around - V * along * cut
    band = np.floor(a)
    fa = a % 1.0
    fb = (b + 0.5 * (band % 2)) % 1.0          # bricks of neighbouring bands offset by half
    h = np.sin(math.pi * fa) * (0.35 + 0.65 * np.sin(math.pi * fb))
    h = np.where((fa < 0.08) | (fa > 0.92) | (fb < 0.05), 0.0, h)
    return (band % 2) == 0, h


def build(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    H = np.full((N, N), 0.5, np.float32)
    metal = np.zeros((N, N), np.float32)
    smooth = np.full((N, N), 0.3, np.float32)
    label = np.zeros((N, N), np.uint8)      # 1 leather 2 two-tone A (leather) 3 two-tone B (contrast) 4 metal
    shade = np.ones((N, N), np.float32)
    icon_rgb = np.zeros((N, N, 3), np.float32)
    icon_a = np.zeros((N, N), np.float32)

    # flat falls: leather grain along the strip, darker burnished edges and tips
    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_STRIP)
    grain = P.noise(*U.shape, scale=5, seed=21)
    streak = P.noise(*U.shape, scale=2, seed=22)
    edge = np.clip(1 - np.minimum(U, 1 - U) / 0.12, 0, 1)
    label[y0:y1, x0:x1] = 1
    shade[y0:y1, x0:x1] = (0.9 + 0.15 * grain + 0.05 * streak) * (1 - 0.35 * edge) * (1 - 0.25 * np.clip((V - 0.85) / 0.15, 0, 1))
    H[y0:y1, x0:x1] = 0.5 + 0.05 * grain - 0.1 * edge
    smooth[y0:y1, x0:x1] = 0.34 + 0.06 * grain

    # braided falls (round): small 6-around braid, two-tone
    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_BRAID_FALL)
    fam, h = braid(U, V, 3, 40)
    label[y0:y1, x0:x1] = np.where(fam, 2, 3)
    shade[y0:y1, x0:x1] = 0.55 + 0.5 * h
    H[y0:y1, x0:x1] = h
    smooth[y0:y1, x0:x1] = 0.4

    # grip: wider braid, two-tone
    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_GRIP)
    fam, h = spiral_braid(U, V, 6, 7, 2.5)
    n = P.noise(*U.shape, scale=6, seed=23)
    label[y0:y1, x0:x1] = np.where(fam, 2, 3)
    shade[y0:y1, x0:x1] = (0.5 + 0.55 * h) * (0.95 + 0.1 * n)
    H[y0:y1, x0:x1] = h
    smooth[y0:y1, x0:x1] = 0.38

    # collar: metal band with a woven knot (same 4-strand wave as the paddle guard)
    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_COLLAR)
    GX = U * 2 * math.pi * F.COLLAR[2]                 # metres around
    GZ = V * (F.COLLAR[1] - F.COLLAR[0])
    zc = GZ.max() / 2
    amp, k = (F.COLLAR[1] - F.COLLAR[0]) * 0.18, 2 * math.pi / ((2 * math.pi * F.COLLAR[2]) / 6)   # 6 repeats around
    s1 = (np.abs(GZ - (zc + amp * np.sin(k * GX))), np.cos(k * GX))
    s2 = (np.abs(GZ - (zc - amp * np.sin(k * GX))), -np.cos(k * GX))
    s3 = (np.abs(GZ - (zc + amp * 1.9)), np.sin(k * GX * 2))
    s4 = (np.abs(GZ - (zc - amp * 1.9)), np.sin(k * GX * 2 + 1))
    span = F.COLLAR[1] - F.COLLAR[0]
    kf, kg, kp = P.interlace([s1, s2, s3, s4], GX, GZ, span * 0.09, span * 0.035)
    n = P.noise(*U.shape, scale=8, seed=24)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = np.where(kf, 1.05 + 0.1 * kp, np.where(kg, 0.45, 0.7 + 0.1 * n))
    H[y0:y1, x0:x1] = np.where(kf, 0.55 + 0.35 * kp, np.where(kg, 0.1, 0.35))
    metal[y0:y1, x0:x1] = 0.9
    smooth[y0:y1, x0:x1] = np.where(kf, 0.65, 0.45)

    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_METAL)
    n = P.noise(*U.shape, scale=8, seed=25)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = 0.85 + 0.25 * n
    metal[y0:y1, x0:x1] = 0.9
    smooth[y0:y1, x0:x1] = 0.5 + 0.15 * n

    # pommel faces: the TrophyWolf icon in a raised metal rim (as on the paddle)
    (x0, y0, x1, y1), U, V = P.rect_grid(F.UV_POMMEL_FACE)
    rr = np.hypot(U - 0.5, V - 0.5) * 2 * F.POMMEL_DISC[1]
    rim = rr >= F.POMMEL_FACE_RADIUS
    side = int(round(F.POMMEL_FACE_RADIUS / F.POMMEL_DISC[1] * (x1 - x0)))
    icon = Image.open(P.WOLF_ICON).convert("RGBA").resize((side, side), Image.LANCZOS)
    canvas = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
    canvas.paste(icon, ((x1 - x0 - side) // 2, (y1 - y0 - side) // 2))
    ic = np.asarray(canvas, np.float32) / 255.0
    alpha = ic[..., 3] * ~rim
    n = P.noise(*U.shape, scale=6, seed=26)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = np.where(rim, 1.0 + 0.1 * n, 0.55 + 0.1 * n)
    icon_rgb[y0:y1, x0:x1] = ic[..., :3] * 255
    icon_a[y0:y1, x0:x1] = alpha
    H[y0:y1, x0:x1] = np.where(rim, 0.75, 0.3 + alpha * (0.3 + 0.2 * ic[..., :3].mean(-1)))
    metal[y0:y1, x0:x1] = np.where(alpha > 0.5, 0.2, 0.9)
    smooth[y0:y1, x0:x1] = np.where(rim, 0.6, 0.4)

    # normal (+Y up) + metal/gloss, shared
    gx = np.zeros_like(H); gy = np.zeros_like(H)
    gx[:, 1:-1] = (H[:, 2:] - H[:, :-2]) / 2
    gy[1:-1, :] = (H[2:, :] - H[:-2, :]) / 2
    nx, ny, nz = -gx * 6.0, gy * 6.0, np.ones_like(H)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    Image.fromarray(((np.stack([nx / ln, ny / ln, nz / ln], -1) * 0.5 + 0.5) * 255).astype(np.uint8), "RGB").save(os.path.join(out_dir, "flogger_Normal.png"))
    mg = np.zeros((N, N, 4), np.uint8)
    mg[..., 0] = mg[..., 1] = mg[..., 2] = (metal * 255).astype(np.uint8)
    mg[..., 3] = (np.clip(smooth, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(mg, "RGBA").save(os.path.join(out_dir, "flogger_MetalGloss.png"))

    written = 0
    for rank, (crop, main, fit, acc) in PALETTE.items():
        two = second_colour(rank)
        main_c, fit_c, two_c = (np.array(c, np.float32) for c in (main, fit, two))
        s = shade[..., None]
        img = np.zeros((N, N, 3), np.float32)
        img = np.where((label == 1)[..., None] | (label == 2)[..., None], main_c * s, img)
        img = np.where((label == 3)[..., None], two_c * s, img)
        img = np.where((label == 4)[..., None], fit_c * s, img)
        a = icon_a[..., None]
        img = img * (1 - a) + icon_rgb * a
        Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(os.path.join(out_dir, f"flogger_Albedo_V{rank}.png"))
        written += 1
        log(f"V{rank}: leather {main}, braid 2nd colour {two}, fittings {fit} ({crop})")
    log(f"normal + metalgloss + {written} albedos written to {out_dir}")
    return written == len(PALETTE)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "textures"))
    sys.exit(0 if build(out) else 1)
