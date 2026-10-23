"""Shared layout of the flogger (D-029, handle D-064 / D-069): sizes (metres), fall chains, bones and UV atlas rects.

Imported by build_flogger.py (Blender) and flogger_textures.py. Axes: Z = length (falls point +Z), origin = middle of the grip.
Family look with the paddle: wolf-icon pommel disc, then the paddle's handle (handle_layout.py): R1 band ring, wood + spiral grip,
R2 band ring, tall valknut ring, R3 band ring; the falls leave through the top of R3.
Total ~0.86 m: pommel bottom -0.141 .. longest fall tip ~0.72 (D-064: ~0.86 m).
"""
import math

import handle_layout as HL
import paddle_layout as PL

# ---- handle (D-064: 0.26 m = R1 bottom .. R3 top; wood grip 0.196 m) ----
GRIP_BOTTOM = -0.080
GRIP_TOP = 0.116
GRIP_RADIUS = 0.0150
RINGS = HL.handle_rings(GRIP_BOTTOM, GRIP_TOP, GRIP_RADIUS)     # R1 -0.090..-0.080, R2 0.116..0.126, V 0.128..0.158, R3 0.160..0.170
POMMEL_DISC = (-0.1145, 0.0265, 0.0120)     # centre z, radius, thickness (same disc as the paddle)
POMMEL_BEVEL = 0.0016
POMMEL_FACE_RADIUS = 0.0215

# ---- falls: 12 chains (bundles), each driven by a 5-bone chain ----
CHAINS = 12
BRAIDED_CHAINS = (0, 6)       # these two chains are one braided round fall (contrast colour); the others carry 4 flat strips
CHAIN_RING_RADIUS = 0.0125    # where the chains leave the handle
FALL_START = RINGS["r3"][0]   # z where the falls start: the bottom of R3, 10 mm inside it (they leave through its top)
FALL_LENGTH = 0.54            # D-064 (was 0.46)
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


# ---- UV atlas (1024), v up: rows 0 .. 0.34 = the handle parts (handle_layout.py, same as the paddle), falls above ----
UV_STRIP = (0.00, HL.BODY_V0, 0.50, 1.00)       # flat falls: u across the strip width (all strips share it), v along the length
UV_BRAID_FALL = (0.50, HL.BODY_V0, 0.75, 1.00)  # braided falls: u around, v along
UV_WOOD, UV_WRAP, UV_METAL, UV_POMMEL_FACE = HL.UV_WOOD, HL.UV_WRAP, HL.UV_METAL, HL.UV_POMMEL_FACE
UV_RING_BAND, UV_VALK_SQ, UV_RING_METAL = HL.UV_RING_BAND, HL.UV_VALK_SQ, HL.UV_RING_METAL

TEXTURE_SIZE = 1024

# the handle rects must stay identical to the paddle atlas (the ring textures are painted into them the same way)
for _name in ("UV_WOOD", "UV_WRAP", "UV_METAL", "UV_POMMEL_FACE", "UV_RING_BAND", "UV_VALK_SQ", "UV_RING_METAL"):
    assert getattr(HL, _name) == getattr(PL, _name), f"handle atlas rect {_name} differs from paddle_layout"
