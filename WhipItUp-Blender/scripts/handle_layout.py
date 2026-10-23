"""Shared handle layout of all three weapons (D-061 / D-062 / D-064). Pure python: imported by the Blender build scripts
(ring_tools.py) and by the numpy texture scripts (ring_textures.py).

Order along the handle, pommel -> body: wolf medallion (stays) -> R1 band ring -> wood + spiral grip -> R2 band ring
-> V tall valknut ring -> R3 band ring (= end of the handle). Axes: Z up, metres, origin = middle of the grip.
The user set the ring heights (band 0.010 m, valknut ring 0.030 m) and the handle lengths; the rest is the agent's calculation (D-064).
"""
import math

BAND_H = 0.010            # band ring height (user)
V_H = 0.030               # valknut ring height (user)
GAP = 0.002               # gap between R2 / V / R3 so every ring is individually distinguishable
BAND_EXTRA_R = 0.003      # band ring lip radius = grip radius + this
V_EXTRA_R = 0.004         # valknut ring frame radius = grip radius + this

LIP_H = 0.0015            # raised lip at both ends of every ring (height along z)
LIP_FOOT = 0.0005         # chamfer height under a lip
BAND_RECESS = 0.0006      # the embossed band sits this much below the lips
WALL = 0.0002             # width of the little step between lip and band

SQUARE = 0.020            # valknut square (outer size, curved around the ring)
SQUARE_FRAME = 0.0015     # raised frame width
SQUARE_RAISE = 0.0008     # frame height over the ring body (the field inside is imprinted: lower than the frame)
SQUARE_WALL = 0.0004      # slope between body and frame
SQUARE_CENTRES = (math.pi / 2, 3 * math.pi / 2)   # +Y and -Y side: the faces of the weapon (front / back)

BAND_REPEATS = 16         # model.png repeats around one band ring (8 repeats per image, tiled twice)
RING_SEGMENTS = 20


def handle_rings(grip_bottom, grip_top, grip_radius):
    """z / radius of the five rings for a wood grip from grip_bottom to grip_top.

    Returns dict: r1, r2, r3 = (z0, z1, lip radius); v = (z0, z1, frame radius); bottom / top = the whole handle.
    """
    band_r = grip_radius + BAND_EXTRA_R
    r1 = (grip_bottom - BAND_H, grip_bottom, band_r)
    r2 = (grip_top, grip_top + BAND_H, band_r)
    v0 = r2[1] + GAP
    v = (v0, v0 + V_H, grip_radius + V_EXTRA_R)
    r3_0 = v[1] + GAP
    r3 = (r3_0, r3_0 + BAND_H, band_r)
    return {"r1": r1, "r2": r2, "v": v, "r3": r3, "bottom": r1[0], "top": r3[1]}


def square_geometry(z0, z1, r):
    """Valknut square on the ring (z0, z1, frame radius r): half angle, frame angle, z bounds of the outer square and of the field."""
    zm = (z0 + z1) / 2
    zs0, zs1 = zm - SQUARE / 2, zm + SQUARE / 2
    return {
        "half_angle": (SQUARE / 2) / r,
        "frame_angle": SQUARE_FRAME / r,
        "wall_angle": SQUARE_WALL / r,
        "zs0": zs0, "zs1": zs1,
        "fz0": zs0 + SQUARE_FRAME, "fz1": zs1 - SQUARE_FRAME,
    }
