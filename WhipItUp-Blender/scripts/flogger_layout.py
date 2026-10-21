"""Shared layout of the flogger (D-029): sizes (metres), fall chains, bones and UV atlas rects.

Imported by build_flogger.py (Blender) and flogger_textures.py. Axes: Z = length (falls point +Z), origin = middle of the grip.
Family look with the paddle: wolf-icon pommel disc, knotwork metal collar where the falls start, two-tone braided grip.
Total ~0.76 m: pommel bottom -0.141 .. longest fall tip ~0.62 (D-029: ~0.75 m).
"""
import math

# ---- handle ----
GRIP_BOTTOM = -0.080
GRIP_TOP = 0.110
GRIP_RADIUS = 0.0150
GRIP_SWELL = 0.0012
FERRULE_BOTTOM = (-0.090, -0.078, 0.0172)   # z0, z1, radius (grip -> pommel)
COLLAR = (0.108, 0.150, 0.0205)             # z0, z1, radius (knotwork band, the falls start inside it)
POMMEL_DISC = (-0.1145, 0.0265, 0.0120)     # centre z, radius, thickness (same disc as the paddle)
POMMEL_BEVEL = 0.0016
POMMEL_FACE_RADIUS = 0.0215

# ---- falls: 12 chains (bundles), each driven by a 5-bone chain ----
CHAINS = 12
BRAIDED_CHAINS = (0, 6)       # these two chains are one braided round fall (contrast colour); the others carry 4 flat strips
CHAIN_RING_RADIUS = 0.0125    # where the chains leave the collar
FALL_START = 0.140            # z where the falls leave the collar
FALL_LENGTH = 0.46
FALL_LENGTH_JITTER = 0.025    # +- per strip
FALL_SPREAD_DEG = 9.0         # outward tilt of a chain at rest
BONES_PER_CHAIN = 5
STRIPS_PER_CHAIN = 4
STRIP_WIDTH = 0.0100
STRIP_THICK = 0.0012
STRIP_FAN = 0.0060            # sideways offset between the strips of one bundle
STRIP_SEGMENTS = 12
BRAID_RADIUS = 0.0042
BRAID_SIDES = 6


def chain_angle(k):
    return 2 * math.pi * k / CHAINS


# ---- UV atlas (1024), v up ----
UV_STRIP = (0.00, 0.30, 0.50, 1.00)      # flat falls: u across the strip width (all strips share it), v along the length
UV_BRAID_FALL = (0.50, 0.30, 0.75, 1.00)  # braided falls: u around, v along
UV_GRIP = (0.75, 0.30, 1.00, 1.00)       # braided grip: u around, v along
UV_COLLAR = (0.00, 0.15, 0.50, 0.30)     # knotwork band: u around, v height
UV_METAL = (0.50, 0.15, 0.75, 0.30)
UV_POMMEL_FACE = (0.78, 0.00, 0.98, 0.20)

TEXTURE_SIZE = 1024
