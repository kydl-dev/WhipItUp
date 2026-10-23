"""Shared layout of the whip (D-034, handle D-064 / D-069): sizes (metres), the thong bone chain and UV atlas rects.

Imported by build_whip.py (Blender) and whip_textures.py. Axes: Z = length (thong points +Z at rest), origin = middle of the grip.
Family look: wolf-icon pommel disc, then the paddle's handle (handle_layout.py: R1 band ring, wood + spiral grip, R2 band ring, tall
valknut ring, R3 band ring), then a 1.5 m tapering two-tone braided thong, a flat leather fall and a frayed cracker. One bone chain
(fall_00_0..N-1) so the mod's FallChains drives it; in game it is coiled in the hand while idle and lashes out while attacking (WhipMotion).
NOT done yet (T-016 S3 body, D-064 / D-066): lash 1.90 m with 38 bones, valknut rings on bones 8 / 16 / 24 and at the tip, plait.
"""
import math

import handle_layout as HL
import paddle_layout as PL

# ---- handle (D-064: 0.27 m = R1 bottom .. R3 top; wood grip 0.206 m) ----
GRIP_BOTTOM = -0.080
GRIP_TOP = 0.126
GRIP_RADIUS = 0.0150
RINGS = HL.handle_rings(GRIP_BOTTOM, GRIP_TOP, GRIP_RADIUS)     # R1 -0.090..-0.080, R2 0.126..0.136, V 0.138..0.168, R3 0.170..0.180
POMMEL_DISC = (-0.1145, 0.0265, 0.0120)
POMMEL_BEVEL = 0.0016
POMMEL_FACE_RADIUS = 0.0215

THONG_START = RINGS["r3"][0]   # bottom of R3, 10 mm inside it (D-064: 0.150 -> 0.170); the thong leaves through the top of R3
THONG_LENGTH = 1.50            # braided part
THONG_R0 = 0.0110             # radius at the handle
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
BONES = 32


def thong_radius(s):
    """Radius at s metres along the braided thong."""
    t = max(0.0, min(1.0, s / THONG_LENGTH))
    return THONG_R0 + (THONG_R1 - THONG_R0) * (t ** 0.8)


# ---- UV atlas (1024), v up: rows 0 .. 0.34 = the handle parts (handle_layout.py, same as the paddle), thong + fall above ----
UV_THONG = (0.00, HL.BODY_V0, 0.50, 1.00)       # braid: u around, v along (repeats every THONG_UV_REPEAT m)
UV_FALL = (0.50, HL.BODY_V0, 0.625, 1.00)       # flat fall + cracker strands
UV_WOOD, UV_WRAP, UV_METAL, UV_POMMEL_FACE = HL.UV_WOOD, HL.UV_WRAP, HL.UV_METAL, HL.UV_POMMEL_FACE
UV_RING_BAND, UV_VALK_SQ, UV_RING_METAL = HL.UV_RING_BAND, HL.UV_VALK_SQ, HL.UV_RING_METAL

TEXTURE_SIZE = 1024

# the handle rects must stay identical to the paddle atlas (the ring textures are painted into them the same way)
for _name in ("UV_WOOD", "UV_WRAP", "UV_METAL", "UV_POMMEL_FACE", "UV_RING_BAND", "UV_VALK_SQ", "UV_RING_METAL"):
    assert getattr(HL, _name) == getattr(PL, _name), f"handle atlas rect {_name} differs from paddle_layout"
