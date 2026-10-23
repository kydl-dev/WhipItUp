"""Blender (bmesh): the handle rings shared by paddle, flogger and whip (D-061 / D-062 / D-064): three band rings and the tall valknut ring.

Geometry numbers: handle_layout.py. Every ring is a lathe grid with its own UVs:
  band ring    lips at both ends (metal), the recessed middle shows the model.png band (rects['band'], 16 repeats around)
  valknut ring body + lips (metal), two curved squares (front / back, +Y / -Y) with a raised frame and an imprinted field
               that shows valknut.png (rects['valk'], read the right way from outside on both sides)
rects = {'band': rect, 'ring_metal': rect, 'valk': rect}, (u0, v0, u1, v1), v up.
"""
import math

import handle_layout as H
from mesh_tools import lerp_rect, log

TAU = 2 * math.pi


def _lathe_grid(bm, uv_layer, rows, angles, radius_fn, mapper_fn, cap_uv):
    """Quad grid around Z. rows: sorted z; angles: sorted in [0, TAU); radius_fn(z, a) -> r; mapper_fn(zc, ac) -> f(z, a) -> (u, v)."""
    nz, na = len(rows), len(angles)
    verts = []
    for z in rows:
        verts.append([bm.verts.new((radius_fn(z, a) * math.cos(a), radius_fn(z, a) * math.sin(a), z)) for a in angles])
    faces = []
    for i in range(nz - 1):
        for j in range(na):
            j2 = (j + 1) % na
            f = bm.faces.new((verts[i][j], verts[i][j2], verts[i + 1][j2], verts[i + 1][j]))
            a0 = angles[j]
            a1 = angles[j2] if j2 != 0 else TAU
            mapper = mapper_fn((rows[i] + rows[i + 1]) / 2, (a0 + a1) / 2)
            corners = ((rows[i], a0), (rows[i], a1), (rows[i + 1], a1), (rows[i + 1], a0))
            for loop, (z, a) in zip(f.loops, corners):
                loop[uv_layer].uv = mapper(z, a)
            faces.append(f)
    for idx, bottom in ((0, True), (nz - 1, False)):
        ring = verts[idx]
        cap = bm.faces.new(list(reversed(ring)) if bottom else ring)
        for loop in cap.loops:
            loop[uv_layer].uv = cap_uv
        faces.append(cap)
    return faces


def build_band_ring(bm, uv_layer, z0, z1, r, rects):
    """Ring with a lip at each end and the recessed band between the lips. r = lip radius."""
    rows = [z0, z0 + H.LIP_FOOT, z0 + H.LIP_H, z0 + H.LIP_H + H.WALL, z1 - H.LIP_H - H.WALL, z1 - H.LIP_H, z1 - H.LIP_FOOT, z1]
    radii = [r - 0.001, r, r, r - H.BAND_RECESS, r - H.BAND_RECESS, r, r, r - 0.001]
    by_row = dict(zip(rows, radii))
    angles = [TAU * k / H.RING_SEGMENTS for k in range(H.RING_SEGMENTS)]
    b0, b1 = rows[3], rows[4]

    def mapper_fn(zc, ac):
        if b0 <= zc <= b1:
            return lambda z, a: lerp_rect(rects["band"], a / TAU, (z - b0) / (b1 - b0))
        return lambda z, a: lerp_rect(rects["ring_metal"], a / TAU, (z - z0) / (z1 - z0))

    faces = _lathe_grid(bm, uv_layer, rows, angles, lambda z, a: by_row[z], mapper_fn, lerp_rect(rects["ring_metal"], 0.5, 0.5))
    log(f"band ring z {z0:.3f}..{z1:.3f} r {r:.4f}: {len(faces)} faces")
    return faces


def _angle_distance(a, c):
    return abs((a - c + math.pi) % TAU - math.pi)


def build_valknut_ring(bm, uv_layer, z0, z1, r, rects):
    """Tall ring (r = frame radius) with two curved valknut squares on the +Y and -Y side."""
    g = H.square_geometry(z0, z1, r)
    th, tf, tw = g["half_angle"], g["frame_angle"], g["wall_angle"]
    zs0, zs1, fz0, fz1 = g["zs0"], g["zs1"], g["fz0"], g["fz1"]
    r_body = r - H.SQUARE_RAISE
    kinds = [(z0, "foot"), (z0 + H.LIP_FOOT, "lip"), (z0 + H.LIP_H, "lip"), (z0 + H.LIP_H + H.WALL, "body"),
             (zs0 - H.SQUARE_WALL, "body"), (zs0, "sq"), (fz0, "sq"), (fz0 + H.SQUARE_WALL, "sq"),
             (fz1 - H.SQUARE_WALL, "sq"), (fz1, "sq"), (zs1, "sq"), (zs1 + H.SQUARE_WALL, "body"),
             (z1 - H.LIP_H - H.WALL, "body"), (z1 - H.LIP_H, "lip"), (z1 - H.LIP_FOOT, "lip"), (z1, "foot")]
    kind_of = dict(kinds)
    rows = [z for z, _ in kinds]
    eps = 1e-9

    def in_square(z, a):
        return any(_angle_distance(a, c) <= th + eps for c in H.SQUARE_CENTRES) and zs0 - eps <= z <= zs1 + eps

    def in_field(z, a):
        return any(_angle_distance(a, c) < th - tf - eps for c in H.SQUARE_CENTRES) and fz0 + eps < z < fz1 - eps

    def radius_fn(z, a):
        kind = kind_of[z]
        if kind == "foot":
            return r - 0.001
        if kind == "lip":
            return r
        if kind == "sq" and in_square(z, a) and not in_field(z, a):
            return r
        return r_body

    marks = []
    for c in H.SQUARE_CENTRES:
        marks += [c - th - tw, c - th, c - th + tf, c - th + tf + tw, c + th - tf - tw, c + th - tf, c + th, c + th + tw]
    marks = [m % TAU for m in marks]
    uniform = [TAU * k / H.RING_SEGMENTS for k in range(H.RING_SEGMENTS)]
    angles = sorted(marks + [u for u in uniform if all(_angle_distance(u, m) > 0.12 for m in marks)])
    f_th = th - tf - tw                                  # field half angle (the body-radius vertices of the field)
    f_z0, f_z1 = fz0 + H.SQUARE_WALL, fz1 - H.SQUARE_WALL

    def mapper_fn(zc, ac):
        for c in H.SQUARE_CENTRES:
            if _angle_distance(ac, c) < f_th and f_z0 < zc < f_z1:
                return lambda z, a, c=c: lerp_rect(rects["valk"], 0.5 + (a - c) / (2 * f_th), (z - f_z0) / (f_z1 - f_z0))
        return lambda z, a: lerp_rect(rects["ring_metal"], a / TAU, (z - z0) / (z1 - z0))

    faces = _lathe_grid(bm, uv_layer, rows, angles, radius_fn, mapper_fn, lerp_rect(rects["ring_metal"], 0.5, 0.5))
    log(f"valknut ring z {z0:.3f}..{z1:.3f} r {r:.4f}: {len(faces)} faces, {len(angles)} columns x {len(rows) - 1} rows")
    return faces


def build_handle_rings(bm, uv_layer, rings, rects):
    """All five rings of a handle (handle_layout.handle_rings) into bm; returns the number of faces added."""
    n = 0
    for key in ("r1", "r2", "r3"):
        z0, z1, r = rings[key]
        n += len(build_band_ring(bm, uv_layer, z0, z1, r, rects))
    z0, z1, r = rings["v"]
    n += len(build_valknut_ring(bm, uv_layer, z0, z1, r, rects))
    log(f"handle rings: {n} faces, handle {rings['bottom']:.3f}..{rings['top']:.3f} m = {rings['top'] - rings['bottom']:.3f} m")
    return n
