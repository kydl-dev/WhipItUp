"""Blender (bmesh): the wood + spiral grip shared by paddle, flogger and whip (D-014, D-061 rule 3).

The grip is a wooden lathe (slight swell in the middle) with a wrapped leather band (spiral strips in the texture, UV_WRAP) over it.
g = dict(bottom, top, radius, swell, wrap_bottom, wrap_top, wrap_extra) in metres; rects = {'wood': rect, 'wrap': rect}.
"""
from mesh_tools import build_lathe, log


def build_grip(bm, uv_layer, g, rects, segments=18):
    zs = [g["bottom"] + (g["top"] - g["bottom"]) * i / 10 for i in range(11)]
    mid = (g["bottom"] + g["top"]) / 2
    half = (g["top"] - g["bottom"]) / 2

    def swell(z):
        return g["swell"] * (1 - ((z - mid) / half) ** 2)

    wood = [(z, g["radius"] + swell(z)) for z in zs]
    build_lathe(bm, uv_layer, wood, rects["wood"], segments=segments)
    wz = [g["wrap_bottom"] + (g["wrap_top"] - g["wrap_bottom"]) * i / 8 for i in range(9)]
    wrap = [(z, g["radius"] + g["wrap_extra"] + swell(z)) for z in wz]
    wrap = [(wz[0] - 0.001, wrap[0][1] - 0.001)] + wrap + [(wz[-1] + 0.001, wrap[-1][1] - 0.001)]
    build_lathe(bm, uv_layer, wrap, rects["wrap"], segments=segments)
    log(f"grip: wood z {g['bottom']:.3f}..{g['top']:.3f} r {g['radius']:.4f}, wrap z {g['wrap_bottom']:.3f}..{g['wrap_top']:.3f}")
    return g["top"] - g["bottom"]


def build_core(bm, uv_layer, g, z_top, rects, segments=18):
    """Wooden shaft that runs on from the top of the grip through R2, the tall valknut ring and R3 (user 2026-10-23: the rings must not
    look like they hang in the air; it shows in the 2 mm gaps between the rings). z_top = a little below the top of R3 so it never meets the cap."""
    z0 = g["top"]
    if z_top <= z0:
        raise ValueError(f"core top {z_top:.4f} is not above the grip top {z0:.4f}")
    build_lathe(bm, uv_layer, [(z0, g["radius"]), (z_top, g["radius"])], rects["wood"], segments=segments)
    log(f"core: wood shaft z {z0:.3f}..{z_top:.3f} r {g['radius']:.4f} (through the rings)")
    return z_top - z0
