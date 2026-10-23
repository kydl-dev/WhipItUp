"""Shared layout of the whip (D-034, handle D-064 / D-069): sizes (metres), the thong bone chain and UV atlas rects.

Imported by build_whip.py (Blender) and whip_textures.py. Axes: Z = length (thong points +Z at rest), origin = middle of the grip.
Family look: wolf-icon pommel disc, then the paddle's handle (handle_layout.py: R1 band ring, wood + spiral grip, R2 band ring, tall
valknut ring, R3 band ring), then a 1.6 m tapering thong (dark core + 2 modelled cords crossing over / under + a thin gold string beside each,
D-071 / D-072), a flat leather fall and a frayed cracker. One bone chain
(fall_00_0..N-1) so the mod's FallChains drives it; in game it is coiled in the hand while idle and lashes out while attacking (WhipMotion).
Lash 1.90 m = 38 bones of 0.050 m (D-066): rigid valknut rings on bones 8 / 16 / 24 and a small one at the tip, modelled plait on the thong (build_whip.build_plait).
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


# ---- modelled braid (D-073): the plain weave of the user's photo (renders/Whip_W_ring8.png) as real geometry ----
# BRAID_STRANDS flat ribbons wind one way round the core and BRAID_STRANDS the other way; each ribbon is 1 / BRAID_STRANDS of the circumference wide,
# so every direction covers the whole thong. Every ribbon goes over / under at every crossing (plain weave, the cells are the diamonds of the photo).
# In each direction one ribbon has colour A (rank leather) and one colour B (2nd colour); the colours swap between the directions (as in the photo).
# A thin string (the medallion brown) runs along both edges of every ribbon.
BRAID_STRANDS = 2             # ribbons per direction
BRAID_TURN = 0.060            # metres of thong per full turn of a ribbon (the photo: 0.06 m, constant along the thong)
BRAID_STEP = 0.003            # metres between two sample rings along a ribbon (5 per diamond cell)
BRAID_COLS = 5                # quads across one ribbon
BRAID_MID = 0.85              # ribbon surface radius / thong radius half way between over and under
BRAID_LIFT = 0.15             # over = MID + LIFT (the thong radius itself), under = MID - LIFT
BRAID_SHARP = 4.0             # steepness of the over / under swap (tanh of sin)
CORE_FRAC = 0.58              # core radius / thong radius (the under ribbons ride at 0.70 of the thong radius)
EDGE_WIDTH = 0.0009           # width of the thin string on a ribbon edge (m)
EDGE_LIFT = 0.0002            # the thin string sits this far above the ribbon surface (m)


def core_radius(s):
    """Radius of the core tube under the braid at s metres along the thong."""
    return CORE_FRAC * thong_radius(s)


def braid_rate():
    """Ribbon bands per metre of thong (the photo's 'along' / repeat): a ribbon turns 2 pi / BRAID_TURN * ... radians per metre."""
    return BRAID_STRANDS / BRAID_TURN


def ribbon_angle(direction, strand, w, s):
    """Angle (rad) round the thong of the point at fraction w (0..1) across ribbon `strand` of the given direction (+1 / -1) at s metres.
    The two directions wind opposite ways."""
    return 2 * math.pi * (strand + w - direction * braid_rate() * s) / BRAID_STRANDS


def ribbon_height(direction, w, s):
    """-1 (under) .. +1 (over) of a ribbon at (w, s). The plain weave of the photo: direction +1 is over where sin(pi (w - 2 q s)) > 0,
    direction -1 is over exactly where +1 is under (they swap at every diagonal cell boundary)."""
    q = braid_rate()
    if direction > 0:
        return math.tanh(BRAID_SHARP * math.sin(math.pi * (w - 2 * q * s)))
    return -math.tanh(BRAID_SHARP * math.sin(math.pi * (w + 2 * q * s)))


def ribbon_radius(direction, w, s):
    """Radius of the outer surface of a ribbon at (w, s)."""
    return thong_radius(s) * (BRAID_MID + BRAID_LIFT * ribbon_height(direction, w, s))


def ribbon_colour(direction, strand):
    """0 = colour A (rank leather), 1 = colour B (2nd colour): direction +1 has A on the even strands, direction -1 on the odd ones."""
    return 0 if (strand % 2 == 0) == (direction > 0) else 1


# ---- UV atlas (1024), v up: rows 0 .. 0.34 = the handle parts (handle_layout.py, same as the paddle), thong + fall above ----
UV_THONG = (0.00, HL.BODY_V0, 0.50, 1.00)       # dark core tube: u around, v along (repeats every THONG_UV_REPEAT m)
UV_FALL = (0.50, HL.BODY_V0, 0.625, 1.00)       # flat fall + cracker strands; v 0.30 = rank leather (cord A), v 0.88 = 2nd colour (cord B)
UV_GOLD = (0.625, HL.BODY_V0, 0.75, 1.00)       # the thin edge strings (one flat colour per rank)
CORD_UV = {0: (0.5, 0.30), 1: (0.5, 0.88)}      # point inside UV_FALL per ribbon colour (0 = A rank leather, 1 = B 2nd colour)
GOLD_UV = (0.5, 0.5)                            # point inside UV_GOLD (the thin edge strings, painted in the medallion brown)
UV_WOOD, UV_WRAP, UV_METAL, UV_POMMEL_FACE = HL.UV_WOOD, HL.UV_WRAP, HL.UV_METAL, HL.UV_POMMEL_FACE
UV_RING_BAND, UV_VALK_SQ, UV_RING_METAL = HL.UV_RING_BAND, HL.UV_VALK_SQ, HL.UV_RING_METAL

TEXTURE_SIZE = 1024

# the handle rects must stay identical to the paddle atlas (the ring textures are painted into them the same way)
for _name in ("UV_WOOD", "UV_WRAP", "UV_METAL", "UV_POMMEL_FACE", "UV_RING_BAND", "UV_VALK_SQ", "UV_RING_METAL"):
    assert getattr(HL, _name) == getattr(PL, _name), f"handle atlas rect {_name} differs from paddle_layout"
