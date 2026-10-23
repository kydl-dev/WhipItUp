"""Textures of the handle rings and the valknut (numpy + PIL), shared by paddle_textures.py and the flogger / whip texture scripts
(D-060 / D-061 / D-064).

  band      reference/nordic_band.png (= photo-models/model.png): white strands = raised, dark = recessed; 16 repeats around a ring
  valknut   reference/valknut.png: black strands on a transparent background (alpha = the valknut); on the tall ring's two squares and on the paddle blade
  metal     dark old metal with dirt and wear, the same for every rank; the embossed strands take the rank colour with the highest contrast (D-021)

Labels written into the atlas arrays: 10 = ring metal (RING_METAL, rank independent), 11 = embossed strand (ring_colour(rank)).
"""
import math
import os

import numpy as np
from PIL import Image

from rank_palette import PALETTE, INLAY_OVERRIDE, luminance

REF = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "reference"))
BAND_PNG = os.path.join(REF, "nordic_band.png")
VALKNUT_PNG = os.path.join(REF, "valknut.png")

RING_METAL = (46, 44, 42)       # dark old metal, all ranks (the user: may be plain black metal)
WOOD = (112, 74, 42)            # grip wood (the same values as paddle_textures.WOOD / WOOD_DARK)
WOOD_DARK = (70, 44, 24)
LABEL_METAL = 10
LABEL_EMBOSS = 11
BAND_ROWS = (2, 62)             # rows of nordic_band.png that hold the band (the rest is the border of the picture)
BAND_TILES = 2                  # 8 repeats per picture, 16 around the ring (handle_layout.BAND_REPEATS)
VALKNUT_MARGIN = 0.06           # free border of the valknut inside the square field


def log(msg):
    print("[ring_textures] " + msg)


def noise(h, w, scale, octaves=4, seed=0):
    """Smooth value noise 0..1, h x w."""
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for _ in range(octaves):
        gh, gw = max(2, int(h / scale) + 2), max(2, int(w / scale) + 2)
        g = rng.random((gh, gw)).astype(np.float32)
        img = Image.fromarray((g * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += amp * (np.asarray(img, np.float32) / 255.0)
        tot += amp
        amp *= 0.5
        scale = max(1.0, scale / 2)
    return out / tot


def ring_colour(rank):
    """Rank colour (leather, fittings, accent, override) with the highest WCAG contrast against the ring metal."""
    crop, main, fit, acc = PALETTE[rank]
    cands = [main, fit, acc] + ([INLAY_OVERRIDE[rank]] if rank in INLAY_OVERRIDE else [])
    lm = luminance(RING_METAL) + 0.05

    def ratio(c):
        lc = luminance(c) + 0.05
        return max(lc, lm) / min(lc, lm)

    return max(cands, key=ratio)


def band_strip(width, height):
    """Float 0..1 strand mask of the band for a region of width x height px: the picture tiled BAND_TILES times, rows BAND_ROWS."""
    im = Image.open(BAND_PNG).convert("RGBA")
    a = np.asarray(im, np.float32) / 255.0
    lum = a[..., :3].mean(-1)
    m = np.clip((lum - 0.40) / 0.30, 0.0, 1.0) * (a[..., 3] > 0.5)
    m[:, 0] = m[:, 1]                       # the outermost columns are transparent: copy the neighbour so the tiles join
    m[:, -1] = m[:, -2]
    m = m[BAND_ROWS[0]:BAND_ROWS[1], :]
    m = np.tile(m, (1, BAND_TILES))
    out = Image.fromarray((m * 255).astype(np.uint8)).resize((width, height), Image.LANCZOS)
    return np.asarray(out, np.float32) / 255.0


_VALKNUT = None


def valknut_alpha(size=512):
    """Float 0..1 valknut (black strands = 1) from valknut.png, size x size, row 0 = top."""
    global _VALKNUT
    if _VALKNUT is None or _VALKNUT.shape[0] != size:
        im = Image.open(VALKNUT_PNG).convert("RGBA")
        a = np.asarray(im, np.float32) / 255.0
        m = a[..., 3] * (1.0 - a[..., :3].mean(-1))          # opaque AND dark
        _VALKNUT = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((size, size), Image.LANCZOS), np.float32) / 255.0
    return _VALKNUT


def _sample(img, u, v):
    """Bilinear sample of img (row 0 = top) at u, v in 0..1 (v up); outside -> 0."""
    n = img.shape[0]
    x = np.clip(u * n - 0.5, 0, n - 1.001)
    y = np.clip((1 - v) * n - 0.5, 0, n - 1.001)
    x0, y0 = x.astype(int), y.astype(int)
    fx, fy = x - x0, y - y0
    s = (img[y0, x0] * (1 - fx) + img[y0, x0 + 1] * fx) * (1 - fy) + (img[y0 + 1, x0] * (1 - fx) + img[y0 + 1, x0 + 1] * fx) * fy
    return s * ((u >= 0) & (u <= 1) & (v >= 0) & (v <= 1))


def _dilate(mask, r):
    out = mask.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r:
                out |= np.roll(np.roll(mask, dy, 0), dx, 1)
    return out


def valknut_blade(px, pz, size):
    """valknut.png on a blade face. px, pz = metres from the valknut centre (arrays). Returns fill, groove, profile, area like the old drawn valknut."""
    img = valknut_alpha(256)
    prof = _sample(img, px / size + 0.5, pz / size + 0.5)
    fill = prof > 0.5
    groove = _dilate(fill, 2) & ~fill
    area = np.hypot(px, pz) < size * 0.62
    soft = _dilate(fill, 1)
    return fill & area, groove & area, np.clip(prof, 0, 1) * (soft & area), area


def paint_rings(rects, rect_box, arrays):
    """Writes the band, valknut-square and ring-metal regions into the atlas arrays (H, shade, label, metal, smooth; all N x N)."""
    H, shade, label, metal, smooth = (arrays[k] for k in ("H", "shade", "label", "metal", "smooth"))

    # ring metal: lips, bodies and frames
    x0, y0, x1, y1 = rect_box(rects["ring_metal"])
    h, w = y1 - y0, x1 - x0
    dirt = noise(h, w, 6, seed=21)
    wear = noise(h, w, 3, seed=22)
    label[y0:y1, x0:x1] = LABEL_METAL
    shade[y0:y1, x0:x1] = 0.70 + 0.55 * dirt
    H[y0:y1, x0:x1] = 0.5 + 0.04 * wear
    metal[y0:y1, x0:x1] = 0.85
    smooth[y0:y1, x0:x1] = 0.25 + 0.3 * wear

    # band: strands raised and burnished, the ground darker with dirt
    x0, y0, x1, y1 = rect_box(rects["band"])
    h, w = y1 - y0, x1 - x0
    strand = band_strip(w, h)
    dirt = noise(h, w, 5, seed=23)
    wear = noise(h, w, 3, seed=24)
    on = strand > 0.5
    label[y0:y1, x0:x1] = np.where(on, LABEL_EMBOSS, LABEL_METAL)
    shade[y0:y1, x0:x1] = np.where(on, 0.80 + 0.35 * wear, (0.45 + 0.35 * dirt))
    H[y0:y1, x0:x1] = 0.25 + 0.5 * strand
    metal[y0:y1, x0:x1] = 0.85
    smooth[y0:y1, x0:x1] = np.where(on, 0.35 + 0.3 * wear, 0.18)

    # valknut field (both squares share it): the valknut raised, the ground imprinted
    x0, y0, x1, y1 = rect_box(rects["valk"])
    h, w = y1 - y0, x1 - x0
    uu = (np.arange(w) + 0.5) / w
    vv = 1.0 - (np.arange(h) + 0.5) / h
    U, V = np.meshgrid(uu, vv)
    k = 1.0 / (1.0 - 2 * VALKNUT_MARGIN)
    prof = _sample(valknut_alpha(256), (U - 0.5) * k + 0.5, (V - 0.5) * k + 0.5)
    on = prof > 0.5
    dirt = noise(h, w, 5, seed=25)
    wear = noise(h, w, 3, seed=26)
    label[y0:y1, x0:x1] = np.where(on, LABEL_EMBOSS, LABEL_METAL)
    shade[y0:y1, x0:x1] = np.where(on, 0.80 + 0.35 * wear, 0.45 + 0.35 * dirt)
    H[y0:y1, x0:x1] = 0.25 + 0.5 * np.clip(prof, 0, 1)
    metal[y0:y1, x0:x1] = 0.85
    smooth[y0:y1, x0:x1] = np.where(on, 0.35 + 0.3 * wear, 0.18)
    painted = int((label >= LABEL_METAL).sum())
    log(f"rings painted: {painted} px, strand colours " + ", ".join(f"V{r}={ring_colour(r)}" for r in PALETTE))
    return painted


def _grid(rect, rect_box):
    """Per-pixel (u, v) inside a UV rect, both 0..1, v up. Returns the pixel box, U, V."""
    x0, y0, x1, y1 = rect_box(rect)
    w, h = x1 - x0, y1 - y0
    uu = (np.arange(w) + 0.5) / w
    vv = 1.0 - (np.arange(h) + 0.5) / h
    U, V = np.meshgrid(uu, vv)
    return (x0, y0, x1, y1), U, V


def paint_grip(rects, rect_box, arrays, wood_label, wrap_label):
    """The paddle's wood grain + spiral wrap (same code as paddle_textures.build, D-014 / D-069) into rects['wood'] / rects['wrap'] of the
    atlas arrays (H, shade, label, smooth). The flogger and the whip call this so their grip is the paddle's grip."""
    H, shade, label, smooth = (arrays[k] for k in ("H", "shade", "label", "smooth"))
    (x0, y0, x1, y1), U, V = _grid(rects["wood"], rect_box)
    g = noise(*U.shape, scale=3, seed=7)
    grain = 0.5 + 0.5 * np.sin(U * 2 * math.pi * 9 + g * 6.0)
    label[y0:y1, x0:x1] = wood_label
    shade[y0:y1, x0:x1] = grain                      # 0 = dark grain, 1 = light wood (mixed in the albedo)
    H[y0:y1, x0:x1] = 0.5 + 0.08 * grain
    smooth[y0:y1, x0:x1] = 0.28

    (x0, y0, x1, y1), U, V = _grid(rects["wrap"], rect_box)
    turns = 14
    f = (U + V * turns) % 1.0                        # diagonal leather strips
    prof = np.sin(f * math.pi)
    label[y0:y1, x0:x1] = wrap_label
    shade[y0:y1, x0:x1] = 0.55 + 0.45 * prof * (0.9 + 0.2 * noise(*U.shape, scale=5, seed=9))
    H[y0:y1, x0:x1] = prof
    smooth[y0:y1, x0:x1] = 0.33
    log(f"grip painted: wood label {wood_label}, wrap label {wrap_label}")
    return int((label == wood_label).sum() + (label == wrap_label).sum())


def grip_albedo(img, label, shade, accent, wood_label, wrap_label):
    """Wood (dark..light by the grain shade) and wrap (rank accent colour) into an albedo image (N x N x 3 float)."""
    s = shade[..., None]
    wood = np.array(WOOD_DARK, np.float32) + (np.array(WOOD, np.float32) - np.array(WOOD_DARK, np.float32)) * s
    img = np.where((label == wood_label)[..., None], wood, img)
    return np.where((label == wrap_label)[..., None], np.array(accent, np.float32) * s, img)


def ring_albedo(rank, label, shade):
    """(mask_metal, colour_metal, mask_emboss, colour_emboss) for the albedo of one rank."""
    return (label == LABEL_METAL), np.array(RING_METAL, np.float32), (label == LABEL_EMBOSS), np.array(ring_colour(rank), np.float32)
