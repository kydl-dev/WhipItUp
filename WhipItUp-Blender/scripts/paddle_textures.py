"""Writes the paddle textures (numpy + PIL only) into WhipItUp-Blender/textures/:

  paddle_Albedo_V1..V7.png  sRGB colour, one per rank (D-005 rank -> crop colour mapping, D-014 part colours)
  paddle_Normal.png         tangent-space normal, +Y up (Unity / OpenGL), shared by all ranks
  paddle_MetalGloss.png     Unity Standard packing: R = metallic, A = smoothness, shared by all ranks

Usage: python paddle_textures.py [out_dir]. Layout + UV rects: paddle_layout.py (shared with build_paddle.py).
Tooling (D-017, D-019): knotwork medallion + Valknut (valknut.png, D-060), darker shade of the rank's main leather colour, raised in the normal map.
Handle rings (D-064): band + valknut squares + dark metal, ring_textures.py (rank-independent metal, embossed strands in the highest-contrast rank colour).
"""
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import paddle_layout as L  # noqa: E402
import medallion_knot  # noqa: E402
import ring_textures as RT  # noqa: E402
from ring_textures import noise  # noqa: E402,F401
from rank_palette import PALETTE, INLAY_OVERRIDE, luminance, contrast_colour  # noqa: E402,F401

N = L.TEXTURE_SIZE
RNG = np.random.default_rng(1234)
WOLF_ICON = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "reference", "TrophyWolf_icon_64.png"))
ICON_RGB = np.zeros((N, N, 3), np.float32)
ICON_A = np.zeros((N, N), np.float32)
MED_BG = np.zeros((N, N), bool)                 # recessed background inside the medallion / valknut

WOOD = (112, 74, 42)
WOOD_DARK = (70, 44, 24)
TOOL_BG = 0.45      # recessed background inside the medallion = main x this
TOOL_GROOVE = 0.30  # cut lines = main x this
BURNISH = 0.60      # edge burnish = main x this
MED_PITCH = 0.25    # plait strand spacing as a fraction of the medallion radius
MED_SWIRL = 1.1     # radians of extra rotation at the centre (curves the plait)
MED_CORE = 0.36     # half size of the square core frame / medallion radius


def log(msg):
    print("[paddle_textures] " + msg)


# ------------------------------------------------------------------------------------------------ helpers
def rect_box(rect):
    """UV rect (v up) -> pixel box (x0, y0, x1, y1), y down."""
    u0, v0, u1, v1 = rect
    return int(round(u0 * N)), int(round((1 - v1) * N)), int(round(u1 * N)), int(round((1 - v0) * N))


def rect_grid(rect):
    """Per-pixel (u, v) inside rect, both 0..1, v up. Returns box, uu, vv."""
    x0, y0, x1, y1 = rect_box(rect)
    w, h = x1 - x0, y1 - y0
    uu = (np.arange(w) + 0.5) / w
    vv = 1.0 - (np.arange(h) + 0.5) / h
    U, V = np.meshgrid(uu, vv)
    return (x0, y0, x1, y1), U, V


def seg_dist(px, pz, a, b):
    ax, az = a
    bx, bz = b
    dx, dz = bx - ax, bz - az
    t = np.clip(((px - ax) * dx + (pz - az) * dz) / (dx * dx + dz * dz), 0, 1)
    return np.hypot(px - (ax + t * dx), pz - (az + t * dz)), t


def interlace(strands, px, pz, width, border):
    """strands = list of (dist_array, priority_array). Returns (fill_mask, groove_mask, profile) of the woven knot:
    the strand with the highest priority on a pixel is on top; its border cuts the strands below it."""
    d = np.stack([s[0] for s in strands])
    pr = np.stack([np.where(s[0] < width / 2 + border, s[1], -9) for s in strands])
    top = np.argmax(pr, axis=0)
    dtop = np.take_along_axis(d, top[None], 0)[0]
    covered = (d < width / 2 + border).any(axis=0)
    fill = covered & (dtop < width / 2)
    groove = covered & ~fill
    profile = np.clip(1 - (dtop / (width / 2)) ** 2, 0, 1)
    return fill, groove, profile


# ------------------------------------------------------------------------------------------------ blade
def blade_coords(rect, back):
    box, U, V = rect_grid(rect)
    x0, x1 = L.BLADE_X_RANGE
    # back rect is mirrored in the UVs (build_paddle.py), so drawing in viewer coords reads correctly on both faces
    X = x0 + U * (x1 - x0)
    Z = L.BLADE_BOTTOM + V * (L.BLADE_TOP - L.BLADE_BOTTOM)
    return box, X, Z


def edge_distance(X, Z):
    """Distance (m) from each point to the blade outline (positive inside)."""
    pts = np.array(L.blade_outline(120) + [L.blade_outline(120)[0]])
    best = np.full(X.shape, 1e9, np.float32)
    for i in range(len(pts) - 1):
        d, _ = seg_dist(X, Z, pts[i], pts[i + 1])
        best = np.minimum(best, d)
    # inside test: |x| <= half width at that z (and above the neck)
    t = np.clip((Z - L.BLADE_BOTTOM) / (L.BLADE_TOP - L.BLADE_BOTTOM), 0, 1)
    hw = np.vectorize(L.blade_half_width)(t)
    inside = np.abs(X) <= hw
    return np.where(inside, best, -best)


def medallion(X, Z):
    """Celtic knot medallion (medallion_knot.py) sampled at blade points. Returns fill, groove, profile, area masks."""
    R = L.MEDALLION_RADIUS
    mx, mz = X / R, (Z - L.MEDALLION_Z) / R
    f, g, p = medallion_knot.sample(mx, mz)
    r = np.hypot(mx, mz)
    area = r < 1.04
    return f & area, g & area, p, area


def valknut(X, Z):
    """The user's valknut.png (D-060) on the blade at VALKNUT_Z, VALKNUT_SIZE high. Returns fill, groove, profile, area."""
    return RT.valknut_blade(X, Z - L.VALKNUT_Z, L.VALKNUT_SIZE)


def stitches(X, Z, de):
    """Dashed saddle stitch 3.5 mm inside the edge."""
    inset = 0.0035
    near = np.abs(de - inset) < 0.0007
    # dash pattern along the edge: use the angle around the blade centre as a cheap arc parameter
    ang = np.arctan2(Z - (L.BLADE_BOTTOM + L.BLADE_TOP) / 2, X)
    dash = (np.sin(ang * 110) > -0.2)
    return near & dash & (Z > L.GUARD[1] + 0.002)


# ------------------------------------------------------------------------------------------------ build
def build(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    H = np.zeros((N, N), np.float32)               # height 0..1
    metal = np.zeros((N, N), np.float32)
    smooth = np.full((N, N), 0.3, np.float32)
    # region label + per-pixel colour factors; albedo is built per rank from these
    label = np.zeros((N, N), np.uint8)             # 0 empty 1 leather 2 wood 3 wrap 4 metal 7 rim 8 wolf icon 9 tooling inlay (2nd colour)
    shade = np.ones((N, N), np.float32)            # multiplier on the region colour
    stitch = np.zeros((N, N), bool)
    tooled = np.zeros((N, N), bool)                # pixel belongs to tooling (keeps the leather hue, darker)

    for rect, back in ((L.UV_BLADE_FRONT, False), (L.UV_BLADE_BACK, True)):
        (x0, y0, x1, y1), X, Z = blade_coords(rect, back)
        de = edge_distance(X, Z)
        grain = noise(*X.shape, scale=6, seed=3 if back else 2)
        mf, mg, mp, marea = medallion(X, Z)
        vf, vg, vp, varea = valknut(X, Z)
        st = stitches(X, Z, de)
        sh = 0.92 + 0.16 * grain
        burn = np.clip(1 - de / 0.014, 0, 1) ** 1.5
        sh *= 1 - (1 - BURNISH) * burn
        bg = (marea | varea) & ~(mf | vf)
        MED_BG[y0:y1, x0:x1] = bg
        sh = np.where(mg | vg, TOOL_GROOVE, sh)
        h = 0.55 + 0.05 * grain
        h = np.where(bg, 0.25, h)
        h = np.where(mf | vf, 0.55 + 0.35 * np.maximum(mp * mf, vp * vf), h)
        h = np.where(mg | vg, 0.05, h)
        h = np.where(st, 0.62, np.where(np.abs(de - 0.0035) < 0.0012, h - 0.12, h))
        sm = np.where(mg | vg | bg, 0.22, 0.38 + 0.06 * grain)
        H[y0:y1, x0:x1] = h
        label[y0:y1, x0:x1] = 1
        shade[y0:y1, x0:x1] = sh
        stitch[y0:y1, x0:x1] = st
        tooled[y0:y1, x0:x1] = bg | mg | vg | mf | vf
        label[y0:y1, x0:x1] = np.where(mf | vf, 9, 1)
        smooth[y0:y1, x0:x1] = sm
        log(f"blade {'back' if back else 'front'}: medallion {int(mf.sum())} px raised, valknut {int(vf.sum())} px, stitches {int(st.sum())} px")

    (x0, y0, x1, y1), U, V = rect_grid(L.UV_BLADE_RIM)
    n = noise(*U.shape, scale=4, seed=5)
    label[y0:y1, x0:x1] = 7
    shade[y0:y1, x0:x1] = BURNISH * (0.9 + 0.2 * n)
    H[y0:y1, x0:x1] = 0.5 + 0.05 * n
    smooth[y0:y1, x0:x1] = 0.45

    (x0, y0, x1, y1), U, V = rect_grid(L.UV_WOOD)
    g = noise(*U.shape, scale=3, seed=7)
    grain = 0.5 + 0.5 * np.sin(U * 2 * math.pi * 9 + g * 6.0)
    label[y0:y1, x0:x1] = 2
    shade[y0:y1, x0:x1] = grain                      # 0 = dark grain, 1 = light wood (mixed in albedo)
    H[y0:y1, x0:x1] = 0.5 + 0.08 * grain
    smooth[y0:y1, x0:x1] = 0.28

    (x0, y0, x1, y1), U, V = rect_grid(L.UV_WRAP)
    turns = 14
    f = (U + V * turns) % 1.0                        # diagonal leather strips
    prof = np.sin(f * math.pi)
    label[y0:y1, x0:x1] = 3
    shade[y0:y1, x0:x1] = 0.55 + 0.45 * prof * (0.9 + 0.2 * noise(*U.shape, scale=5, seed=9))
    H[y0:y1, x0:x1] = prof
    smooth[y0:y1, x0:x1] = 0.33

    (x0, y0, x1, y1), U, V = rect_grid(L.UV_METAL)
    n = noise(*U.shape, scale=8, seed=11)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = 0.85 + 0.25 * n
    H[y0:y1, x0:x1] = 0.5
    metal[y0:y1, x0:x1] = 0.9
    smooth[y0:y1, x0:x1] = 0.5 + 0.15 * n

    # guard faces: knotwork band (two interlaced waves) inside a raised frame
    (x0, y0, x1, y1), U, V = rect_grid(L.UV_GUARD_FACE)
    gz0, gz1, ghx, ghy = L.GUARD
    GX = -ghx + U * 2 * ghx
    GZ = gz0 + V * (gz1 - gz0)
    zc = (gz0 + gz1) / 2
    amp, k = (gz1 - gz0) * 0.18, 2 * math.pi / (ghx * 0.55)
    s1 = (np.abs(GZ - (zc + amp * np.sin(k * GX))), np.cos(k * GX))
    s2 = (np.abs(GZ - (zc - amp * np.sin(k * GX))), -np.cos(k * GX))
    s3 = (np.abs(GZ - (zc + amp * 1.9)), np.sin(k * GX * 2))
    s4 = (np.abs(GZ - (zc - amp * 1.9)), np.sin(k * GX * 2 + 1))
    kf, kg, kp = interlace([s1, s2, s3, s4], GX, GZ, (gz1 - gz0) * 0.09, (gz1 - gz0) * 0.035)
    n = noise(*U.shape, scale=8, seed=13)
    label[y0:y1, x0:x1] = 4
    sh = np.where(kf, 1.05 + 0.1 * kp, np.where(kg, 0.45, 0.7 + 0.1 * n))
    shade[y0:y1, x0:x1] = sh
    H[y0:y1, x0:x1] = np.where(kf, 0.55 + 0.35 * kp, np.where(kg, 0.1, 0.35))
    metal[y0:y1, x0:x1] = 0.9
    smooth[y0:y1, x0:x1] = np.where(kf, 0.65, 0.45)

    # pommel disc faces: vanilla TrophyWolf icon inlaid in a raised metal rim (D-020, the user's choice)
    (x0, y0, x1, y1), U, V = rect_grid(L.UV_POMMEL_FACE)
    rr = np.hypot(U - 0.5, V - 0.5) * 2 * L.POMMEL_DISC[1]          # metres from the disc centre
    fr = L.POMMEL_FACE_RADIUS
    rim = rr >= fr
    icon = Image.open(WOLF_ICON).convert("RGBA")
    side = int(round(2 * fr / (2 * L.POMMEL_DISC[1]) * (x1 - x0)))
    icon = icon.resize((side, side), Image.LANCZOS)
    canvas = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
    canvas.paste(icon, ((x1 - x0 - side) // 2, (y1 - y0 - side) // 2))
    ic = np.asarray(canvas, np.float32) / 255.0
    alpha = ic[..., 3] * ~rim
    n = noise(*U.shape, scale=6, seed=17)
    label[y0:y1, x0:x1] = 4
    shade[y0:y1, x0:x1] = np.where(rim, 1.0 + 0.1 * n, 0.55 + 0.1 * n)    # rim bright, recessed field darker
    ICON_RGB[y0:y1, x0:x1] = ic[..., :3] * 255
    ICON_A[y0:y1, x0:x1] = alpha
    lum = ic[..., :3].mean(-1)
    H[y0:y1, x0:x1] = np.where(rim, 0.75, 0.3 + alpha * (0.3 + 0.2 * lum))
    metal[y0:y1, x0:x1] = np.where(alpha > 0.5, 0.2, 0.9)
    smooth[y0:y1, x0:x1] = np.where(rim, 0.6, np.where(alpha > 0.5, 0.4, 0.35))

    # handle rings (D-064): band, valknut squares, dark metal; labels 10 / 11 are coloured per rank below
    RT.paint_rings({"band": L.UV_RING_BAND, "valk": L.UV_VALK_SQ, "ring_metal": L.UV_RING_METAL}, rect_box,
                   {"H": H, "shade": shade, "label": label, "metal": metal, "smooth": smooth})

    # ---- normal map (shared). Image rows go down: dH/dv (v up) = -(row gradient).
    strength = 6.0
    gx = np.zeros_like(H)
    gy = np.zeros_like(H)
    gx[:, 1:-1] = (H[:, 2:] - H[:, :-2]) / 2
    gy[1:-1, :] = (H[2:, :] - H[:-2, :]) / 2
    nx = -gx * strength
    ny = gy * strength                                # = -dH/dv
    nz = np.ones_like(H)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nrm = np.stack([nx / ln, ny / ln, nz / ln], -1) * 0.5 + 0.5
    Image.fromarray((nrm * 255).astype(np.uint8), "RGB").save(os.path.join(out_dir, "paddle_Normal.png"))

    mg = np.zeros((N, N, 4), np.uint8)
    mg[..., 0] = (metal * 255).astype(np.uint8)
    mg[..., 1] = mg[..., 0]
    mg[..., 2] = mg[..., 0]
    mg[..., 3] = (np.clip(smooth, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(mg, "RGBA").save(os.path.join(out_dir, "paddle_MetalGloss.png"))

    # ---- albedo per rank
    written = []
    for rank, (crop, main, fit, acc) in PALETTE.items():
        main_c = np.array(main, np.float32)
        fit_c = np.array(fit, np.float32)
        acc_c = np.array(acc, np.float32)
        img = np.zeros((N, N, 3), np.float32)
        s = shade[..., None]
        img = np.where((label == 1)[..., None] | (label == 7)[..., None], main_c * s, img)
        img = np.where(stitch[..., None], acc_c * 0.95, img)
        wood = np.array(WOOD_DARK, np.float32) + (np.array(WOOD, np.float32) - np.array(WOOD_DARK, np.float32)) * s
        img = np.where((label == 2)[..., None], wood, img)
        img = np.where((label == 3)[..., None], acc_c * s, img)
        img = np.where((label == 4)[..., None], fit_c * s, img)
        inlay_c = INLAY_OVERRIDE.get(rank) or contrast_colour(main, fit, acc)
        img = np.where((label == 9)[..., None], np.array(inlay_c, np.float32) * s, img)
        # background: darker leather behind light strands; plain leather behind dark strands (keeps the contrast)
        bg_f = TOOL_BG if luminance(inlay_c) > luminance(main) else 1.0
        img = np.where(MED_BG[..., None], img * bg_f, img)
        m_metal, c_metal, m_emboss, c_emboss = RT.ring_albedo(rank, label, shade)
        img = np.where(m_metal[..., None], c_metal * s, img)
        img = np.where(m_emboss[..., None], c_emboss * s, img)
        a = ICON_A[..., None]
        img = img * (1 - a) + ICON_RGB * a
        path = os.path.join(out_dir, f"paddle_Albedo_V{rank}.png")
        Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(path)
        written.append(path)
        log(f"V{rank}: {crop} colours main {main} fittings {fit} accent {acc}, medallion strands {inlay_c} -> {os.path.basename(path)}")
    log(f"normal + metalgloss + {len(written)} albedos written to {out_dir}")
    return len(written) == len(PALETTE)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "textures"))
    ok = build(out)
    sys.exit(0 if ok else 1)
