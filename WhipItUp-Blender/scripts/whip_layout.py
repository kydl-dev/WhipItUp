"""Shared layout of the whip (D-034): sizes (metres), the thong bone chain and UV atlas rects.

Imported by build_whip.py (Blender) and whip_textures.py. Axes: Z = length (thong points +Z at rest), origin = middle of the grip.
Family look: wolf-icon pommel disc, two-tone braided grip, knotwork metal collar, then a 1.8 m tapering two-tone braided thong,
a flat leather fall and a frayed cracker. One bone chain (fall_00_0..N-1) so the mod's FallChains drives it; in game it is coiled
in the hand while idle and lashes out while attacking (WhipMotion).
"""
import math

GRIP_BOTTOM = -0.080
GRIP_TOP = 0.120
GRIP_RADIUS = 0.0150
GRIP_SWELL = 0.0010
FERRULE_BOTTOM = (-0.090, -0.078, 0.0172)
COLLAR = (0.118, 0.152, 0.0185)
POMMEL_DISC = (-0.1145, 0.0265, 0.0120)
POMMEL_BEVEL = 0.0016
POMMEL_FACE_RADIUS = 0.0215

THONG_START = 0.150
THONG_LENGTH = 1.50            # braided part
THONG_R0 = 0.0110             # radius at the collar
THONG_R1 = 0.0032             # radius at the end of the braid
THONG_SIDES = 8
THONG_RINGS_PER_M = 40
THONG_UV_REPEAT = 0.30        # metres of thong per full UV rect height (the braid texture repeats)
FALL_LENGTH = 0.22            # flat leather fall after the braid
FALL_WIDTH = 0.0060
FALL_THICK = 0.0015
CRACKER_LENGTH = 0.08         # frayed tip
CRACKER_STRANDS = 5
CRACKER_RADIUS = 0.0009
TOTAL_LASH = THONG_LENGTH + FALL_LENGTH + CRACKER_LENGTH    # 1.80 m (D-034)
BONES = 16


def thong_radius(s):
    """Radius at s metres along the braided thong."""
    t = max(0.0, min(1.0, s / THONG_LENGTH))
    return THONG_R0 + (THONG_R1 - THONG_R0) * (t ** 0.8)


UV_THONG = (0.00, 0.30, 0.50, 1.00)      # braid: u around, v along (repeats every THONG_UV_REPEAT m)
UV_FALL = (0.50, 0.30, 0.625, 1.00)      # flat fall + cracker strands
UV_GRIP = (0.75, 0.30, 1.00, 1.00)
UV_COLLAR = (0.00, 0.15, 0.50, 0.30)
UV_METAL = (0.50, 0.15, 0.75, 0.30)
UV_POMMEL_FACE = (0.78, 0.00, 0.98, 0.20)

TEXTURE_SIZE = 1024
