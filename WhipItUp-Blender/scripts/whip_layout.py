"""Shared layout of the whip (D-034, handle D-064 / D-069): sizes (metres), the thong bone chain and UV atlas rects.

Imported by build_whip.py (Blender) and whip_textures.py. Axes: Z = length (thong points +Z at rest), origin = middle of the grip.
Family look: wolf-icon pommel disc, then the paddle's handle (handle_layout.py: R1 band ring, wood + spiral grip, R2 band ring, tall
valknut ring, R3 band ring), then a 1.5 m tapering two-tone braided thong, a flat leather fall and a frayed cracker. One bone chain
(fall_00_0..N-1) so the mod's FallChains drives it; in game it is coiled in the hand while idle and lashes out while attacking (WhipMotion).
Lash 1.90 m = 38 bones of 0.050 m (D-066): rigid valknut rings on bones 8 / 16 / 24 and a small one at the tip, plaited thong (whip_textures.plait).
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
THONG_LENGTH = 1.60            # braided plait part (D-064 / D-066: 32 bones of 0.050 m)
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
TOTAL_LASH = THONG_LENGTH + FALL_LENGTH + CRACKER_LENGTH    # 1.90 m (D-064)
BONE_LENGTH = 0.050           # D-066: 32 bones over the plait + 6 over fall and cracker
BONES = round(TOTAL_LASH / BONE_LENGTH)    # 38

# ---- valknut rings on the lash (D-066): rigid, each weighted 100 % to ONE bone ----
RING_BONES = (7, 15, 23)      # 0-based = bones 8 / 16 / 24, the ring is centred on the middle of the bone (0.375 / 0.775 / 1.175 m)
RING_RADIUS = 0.011
RING_HEIGHT = 0.030
RING_SQUARE = 0.016           # valknut square on these smaller rings (handle ring: 0.025 at radius 0.019, D-070): same arc angle about 75 deg
TIP_RING_RADIUS = 0.0055      # 50 %: the last thing seen at the tip end, flush with the cracker tip
TIP_RING_HEIGHT = 0.015
TIP_RING_SQUARE = 0.008
CRACKER_SPREAD = 0.0025       # the cracker strands end inside the tip ring (body radius 0.0047 m; was 0.012)


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
