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

SQUARE = 0.025            # valknut square (outer size, curved around the ring); was 0.020, user 2026-10-23: bigger / as wide as the handle (D-068).
                          # 0.0258 is the most that fits the 0.030 m ring between the lips, square_geometry() raises an error above that
SQUARE_FRAME = 0.0015     # raised frame width
SQUARE_RAISE = 0.0008     # frame height over the ring body (the field inside is imprinted: lower than the frame)
SQUARE_WALL = 0.0004      # slope between body and frame
SQUARE_CENTRES = (math.pi / 2, 3 * math.pi / 2)   # +Y and -Y side: the faces of the weapon (front / back)

BAND_REPEATS = 16         # model.png repeats around one band ring (8 repeats per image, tiled twice)
RING_SEGMENTS = 20

# ---- the paddle's grip, reused by flogger and whip (user 2026-10-23: "flogger and whip handle should look like the current model of the paddle") ----
GRIP_SWELL = 0.0018       # extra radius in the middle of the wood grip (paddle value)
WRAP_EXTRA = 0.0016       # wrap radius over the wood (paddle value)
WRAP_FROM_BOTTOM = 0.022  # the spiral wrap starts this far above the grip bottom (paddle: -0.078 -> -0.056)
WRAP_FROM_TOP = 0.040     # ... and ends this far below the grip top (paddle: 0.108 -> 0.068)
CORE_UNDER_TOP = 0.002    # the wood core stops this far below the top of R3 (grip_tools.build_core)

# ---- UV atlas rows v 0 .. 0.34 of EVERY weapon: the handle parts use the same rects as the paddle atlas (paddle_layout.py, checked there);
#      the weapon's own body (falls / thong / blade) uses v BODY_V0 .. 1 ----
BODY_V0 = 0.34
UV_WOOD = (0.00, 0.05, 0.25, 0.20)
UV_WRAP = (0.25, 0.00, 0.45, 0.20)
UV_METAL = (0.45, 0.00, 0.75, 0.08)
UV_POMMEL_FACE = (0.78, 0.00, 0.98, 0.20)
UV_RING_BAND = (0.00, 0.20, 0.70, 0.26)
UV_VALK_SQ = (0.72, 0.20, 0.86, 0.34)
UV_RING_METAL = (0.87, 0.20, 0.99, 0.34)

# rect dicts as the build scripts (grip_tools / ring_tools) want them
GRIP_RECTS = {"wood": UV_WOOD, "wrap": UV_WRAP}
RING_RECTS = {"band": UV_RING_BAND, "ring_metal": UV_RING_METAL, "valk": UV_VALK_SQ}


def grip_spec(bottom, top, radius):
    """The dict grip_tools.build_grip / build_core want, with the paddle's swell and wrap proportions."""
    return dict(bottom=bottom, top=top, radius=radius, swell=GRIP_SWELL,
                wrap_bottom=bottom + WRAP_FROM_BOTTOM, wrap_top=top - WRAP_FROM_TOP, wrap_extra=WRAP_EXTRA)


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
    room = (zs0 - SQUARE_WALL) - (z0 + LIP_H + WALL)       # body left between the lip step and the square's slope, each side
    if room <= 0:
        raise ValueError(f"valknut square {SQUARE:.4f} m does not fit the ring {z1 - z0:.4f} m between the lips (room {room:.5f} m)")
    return {
        "half_angle": (SQUARE / 2) / r,
        "frame_angle": SQUARE_FRAME / r,
        "wall_angle": SQUARE_WALL / r,
        "zs0": zs0, "zs1": zs1,
        "fz0": zs0 + SQUARE_FRAME, "fz1": zs1 - SQUARE_FRAME,
    }
