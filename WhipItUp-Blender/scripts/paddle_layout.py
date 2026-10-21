"""Shared layout of the paddle: geometry sizes (metres) and the UV atlas regions.

Imported by build_paddle.py (Blender) and paddle_textures.py (numpy/PIL). Change a value here and rerun BOTH scripts.
Axes (Blender): X = blade width, Y = thickness, Z = length (up). Origin (0,0,0) = the middle of the grip.
UV rects are (u0, v0, u1, v1) in 0..1, v up (Blender convention; the texture script flips rows for the PNG).
"""

# ---- geometry (metres). Total length = POMMEL_BOTTOM .. BLADE_TOP = 0.56 m (D-015: ~0.55 m) ----
BLADE_BOTTOM = 0.135
BLADE_TOP = 0.420
BLADE_THICK = 0.012
BLADE_HALF_NECK = 0.035      # half width where the blade enters the guard
BLADE_HALF_WIDE = 0.082      # half width after the flare
BLADE_HALF_TOP = 0.088       # half width just before the rounded top
BLADE_FLARE_END = 0.25       # fraction of the blade length where the flare is complete
BLADE_ROUND_START = 0.80     # fraction where the rounded top starts
BLADE_ROUND_POWER = 2.5      # superellipse power of the rounded top
BLADE_BEVEL = 0.0018

HANDLE_BOTTOM = -0.078
HANDLE_TOP = 0.102
HANDLE_RADIUS = 0.0145
HANDLE_SWELL = 0.0018        # extra radius in the middle of the grip
WRAP_BOTTOM = -0.056
WRAP_TOP = 0.062
WRAP_EXTRA = 0.0016          # wrap radius over the wood

FERRULE_TOP = (0.094, 0.104, 0.0180)      # z0, z1, radius (handle -> guard)
FERRULE_BOTTOM = (-0.088, -0.076, 0.0172) # z0, z1, radius (handle -> pommel)

GUARD = (0.097, 0.148, 0.043, 0.0105)     # z0, z1, half width x, half thickness y
GUARD_RIVET_Z = 0.1225
GUARD_RIVET_X = (-0.026, 0.0, 0.026)
RIVET_RADIUS = 0.0038

STUD_RADIUS = 0.0042
STUD_INSET = 0.0085          # stud centre distance from the blade edge
STUD_SPACING = 0.019
STUD_FROM_T = 0.14           # first stud at this fraction of the blade length (above the neck)

POMMEL_DISC = (-0.1135, 0.0265, 0.0120)  # centre z, radius, thickness (disc in the XZ plane, faces +-Y like the blade)
POMMEL_BEVEL = 0.0016
POMMEL_FACE_RADIUS = 0.0215              # radius of the wolf inlay on each face (inside the raised metal rim)
POMMEL_BOTTOM = POMMEL_DISC[0] - POMMEL_DISC[1]

# ---- tooling on the blade (both faces, back face mirrored so it reads correctly) ----
MEDALLION_Z = 0.300
MEDALLION_RADIUS = 0.052
VALKNUT_Z = 0.190
VALKNUT_SIZE = 0.030         # height of the valknut

# ---- UV atlas (one 1024x1024 texture per rank) ----
BLADE_X_RANGE = (-0.090, 0.090)           # x mapped onto the blade rects
UV_BLADE_FRONT = (0.00, 0.20, 0.50, 1.00)
UV_BLADE_BACK = (0.50, 0.20, 1.00, 1.00)
UV_BLADE_RIM = (0.00, 0.00, 0.25, 0.05)
UV_WOOD = (0.00, 0.05, 0.25, 0.20)
UV_WRAP = (0.25, 0.00, 0.45, 0.20)
UV_GUARD_FACE = (0.45, 0.08, 0.75, 0.20)
UV_METAL = (0.45, 0.00, 0.75, 0.08)
UV_POMMEL_FACE = (0.78, 0.00, 0.98, 0.20)   # square: both disc faces (back mirrored so the wolf reads correctly)

TEXTURE_SIZE = 1024


def _smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def blade_half_width(t):
    """Half width (m) of the blade at fraction t (0 = neck, 1 = top) of its length."""
    w = BLADE_HALF_NECK + (BLADE_HALF_WIDE - BLADE_HALF_NECK) * _smoothstep(0.0, BLADE_FLARE_END, t)
    w += (BLADE_HALF_TOP - BLADE_HALF_WIDE) * _smoothstep(BLADE_FLARE_END, BLADE_ROUND_START, t)
    if t > BLADE_ROUND_START:
        s = (t - BLADE_ROUND_START) / (1.0 - BLADE_ROUND_START)
        w *= max(0.0, 1.0 - s ** BLADE_ROUND_POWER) ** (1.0 / BLADE_ROUND_POWER)
    return w


def blade_outline(samples=48):
    """Closed outline of the blade in XZ (m): right edge bottom -> top, the top point, left edge top -> bottom."""
    length = BLADE_TOP - BLADE_BOTTOM
    ts = [i / samples for i in range(samples + 1)]
    ts = sorted(set(ts + [BLADE_ROUND_START + (1 - BLADE_ROUND_START) * (1 - (1 - k / 16) ** 2) for k in range(17)]))
    right = [(blade_half_width(t), BLADE_BOTTOM + t * length) for t in ts if t < 1.0]
    left = [(-x, z) for (x, z) in reversed(right)]
    return right + [(0.0, BLADE_TOP)] + left
