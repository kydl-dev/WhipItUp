"""Writes the whip textures into WhipItUp-Blender/textures/ (numpy + PIL):
  whip_Albedo_V1..V7.png, whip_Normal.png (+Y up), whip_MetalGloss.png (R metallic, A smoothness).
Colours (D-034): thong = two-tone spiral braid (rank leather + rank 2nd colour), fall = rank leather, cracker = 2nd colour,
pommel rim = fitting metal, pommel faces = TrophyWolf icon. Handle (D-064 / D-069) = the paddle's: wood + spiral wrap in the rank accent
colour (ring_textures.paint_grip), band rings / valknut squares / dark old metal (ring_textures.paint_rings).
Usage: python whip_textures.py [out_dir]
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import whip_layout as W  # noqa: E402
import ring_textures as RT  # noqa: E402
import flogger_textures as FT  # noqa: E402  (spiral_braid)
import paddle_textures as P  # noqa: E402  (rect_grid, rect_box, noise, interlace, WOLF_ICON)
from rank_palette import PALETTE, second_colour  # noqa: E402

N = W.TEXTURE_SIZE
LABEL_WOOD = FT.LABEL_WOOD
LABEL_WRAP = FT.LABEL_WRAP


def log(msg):
    print("[whip_textures] " + msg)


PLAIT_STRINGS = 2     # strings per layer around the thong (D-061 Q15 / D-066: 2 layers x 2 strings)
PLAIT_ALONG = 10      # string bands per texture repeat along the thong (integer: the plait tiles)


def plait(U, V, around, along):
    """Two layers of strings crossing each other (D-066): layer A winds one way (a), layer B the other way (b); the two colours
    alternate inside each layer and the layers swap colours, and at every crossing the visible layer alternates (over / under).
    Returns (colour A mask, height 0..1). Integer around / along make it tile in u and v."""
    a = U * around + V * along
    b = U * around - V * along
    ia, ib = np.floor(a), np.floor(b)
    top_a = ((ia + ib) % 2) == 0
    colour_a = np.where(top_a, (ia % 2) == 0, (ib % 2) == 1)
    f = np.where(top_a, a % 1.0, b % 1.0)
    h = np.sin(math.pi * f) ** 0.7
    h = np.where((f < 0.06) | (f > 0.94), 0.0, h)
    return colour_a, h


def build(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    H = np.full((N, N), 0.5, np.float32)
    metal = np.zeros((N, N), np.float32)
    smooth = np.full((N, N), 0.3, np.float32)
    label = np.zeros((N, N), np.uint8)      # 1 leather / braid A, 3 braid B (2nd colour), 4 metal
    shade = np.ones((N, N), np.float32)
    icon_rgb = np.zeros((N, N, 3), np.float32)
    icon_a = np.zeros((N, N), np.float32)

    # thong: one rect = THONG_UV_REPEAT m of thong; the braid must tile vertically (integer 'along')
    (x0, y0, x1, y1), U, V = P.rect_grid(W.UV_THONG)
    fam, h = plait(U, V, PLAIT_STRINGS, PLAIT_ALONG)
    n = P.noise(*U.shape, scale=6, seed=31)
    label[y0:y1, x0:x1] = np.where(fam, 1, 3)
    shade[y0:y1, x0:x1] = (0.5 + 0.55 * h) * (0.95 + 0.1 * n)
    H[y0:y1, x0:x1] = h
    smooth[y0:y1, x0:x1] = 0.4

    # fall (flat leather, v 0..0.7) + cracker strands (v 0.75..1, 2nd colour)
    (x0, y0, x1, y1), U, V = P.rect_grid(W.UV_FALL)
    g = P.noise(*U.shape, scale=4, seed=32)
    edge = np.clip(1 - np.minimum(U, 1 - U) / 0.15, 0, 1)
    label[y0:y1, x0:x1] = np.where(V > 0.72, 3, 1)
    shade[y0:y1, x0:x1] = (0.9 + 0.15 * g) * (1 - 0.3 * edge)
    H[y0:y1, x0:x1] = 0.5 + 0.05 * g
    smooth[y0:y1, x0:x1] = 0.35

    # grip = the paddle's wood + spiral wrap (D-069), rings = the paddle's band / valknut / dark metal (D-064)
    arrays = {"H": H, "shade": shade, "label": label, "metal": metal, "smooth": smooth}
    RT.paint_grip({"wood": W.UV_WOOD, "wrap": W.UV_WRAP}, P.rect_box, arrays, LABEL_WOOD, LABEL_WRAP)
    RT.paint_rings({"band": W.UV_RING_BAND, "valk": W.UV_VALK_SQ, "ring_metal": W.UV_RING_METAL}, P.rect_box, arrays)

    (x0, y0, x1, y1), U, V = P.rect_grid(W.UV_METAL)
    n = P.noise(*U.shape, scale=8, seed=35)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = 0.85 + 0.25 * n
    metal[y0:y1, x0:x1] = 0.9
    smooth[y0:y1, x0:x1] = 0.5 + 0.15 * n

    (x0, y0, x1, y1), U, V = P.rect_grid(W.UV_POMMEL_FACE)
    rr = np.hypot(U - 0.5, V - 0.5) * 2 * W.POMMEL_DISC[1]
    rim = rr >= W.POMMEL_FACE_RADIUS
    side = int(round(W.POMMEL_FACE_RADIUS / W.POMMEL_DISC[1] * (x1 - x0)))
    icon = Image.open(P.WOLF_ICON).convert("RGBA").resize((side, side), Image.LANCZOS)
    canvas = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
    canvas.paste(icon, ((x1 - x0 - side) // 2, (y1 - y0 - side) // 2))
    ic = np.asarray(canvas, np.float32) / 255.0
    alpha = ic[..., 3] * ~rim
    n = P.noise(*U.shape, scale=6, seed=36)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = np.where(rim, 1.0 + 0.1 * n, 0.55 + 0.1 * n)
    icon_rgb[y0:y1, x0:x1] = ic[..., :3] * 255
    icon_a[y0:y1, x0:x1] = alpha
    H[y0:y1, x0:x1] = np.where(rim, 0.75, 0.3 + alpha * (0.3 + 0.2 * ic[..., :3].mean(-1)))
    metal[y0:y1, x0:x1] = np.where(alpha > 0.5, 0.2, 0.9)
    smooth[y0:y1, x0:x1] = np.where(rim, 0.6, 0.4)

    gx = np.zeros_like(H); gy = np.zeros_like(H)
    gx[:, 1:-1] = (H[:, 2:] - H[:, :-2]) / 2
    gy[1:-1, :] = (H[2:, :] - H[:-2, :]) / 2
    nx, ny, nz = -gx * 6.0, gy * 6.0, np.ones_like(H)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    Image.fromarray(((np.stack([nx / ln, ny / ln, nz / ln], -1) * 0.5 + 0.5) * 255).astype(np.uint8), "RGB").save(os.path.join(out_dir, "whip_Normal.png"))
    mg = np.zeros((N, N, 4), np.uint8)
    mg[..., 0] = mg[..., 1] = mg[..., 2] = (metal * 255).astype(np.uint8)
    mg[..., 3] = (np.clip(smooth, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(mg, "RGBA").save(os.path.join(out_dir, "whip_MetalGloss.png"))

    written = 0
    for rank, (crop, main, fit, acc) in PALETTE.items():
        two = second_colour(rank)
        main_c, fit_c, two_c = (np.array(c, np.float32) for c in (main, fit, two))
        s = shade[..., None]
        img = np.zeros((N, N, 3), np.float32)
        img = np.where((label == 1)[..., None], main_c * s, img)
        img = np.where((label == 3)[..., None], two_c * s, img)
        img = np.where((label == 4)[..., None], fit_c * s, img)
        img = RT.grip_albedo(img, label, shade, acc, LABEL_WOOD, LABEL_WRAP)
        m_metal, c_metal, m_emboss, c_emboss = RT.ring_albedo(rank, label, shade)
        img = np.where(m_metal[..., None], c_metal * s, img)
        img = np.where(m_emboss[..., None], c_emboss * s, img)
        a = icon_a[..., None]
        img = img * (1 - a) + icon_rgb * a
        Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(os.path.join(out_dir, f"whip_Albedo_V{rank}.png"))
        written += 1
        log(f"V{rank}: leather {main}, braid 2nd colour {two}, fittings {fit} ({crop})")
    log(f"normal + metalgloss + {written} albedos written to {out_dir}")
    return written == len(PALETTE)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "textures"))
    sys.exit(0 if build(out) else 1)
