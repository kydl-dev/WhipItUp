"""Shared Blender mesh helpers for the WhipItUp items (paddle, flogger, whip). Imported by the build_*.py scripts.

All parts are built Z-up in metres; UVs go into atlas rects (u0, v0, u1, v1), v up.
"""
import math

import bmesh
import bpy
from mathutils import Vector

LOG = []


def log(msg):
    LOG.append(msg)
    print("[mesh_tools] " + msg)


def lerp_rect(rect, u, v):
    u0, v0, u1, v1 = rect
    return (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)


def build_slab(bm, uv_layer, outline, thick, bevel, front_rect, back_rect, rim_rect, x_range, z_range, name):
    """Flat part in the XZ plane, faces +-Y: an extruded closed outline with bevelled edges and planar UVs
    (front -> front_rect, back -> back_rect mirrored so its image reads correctly from behind, sides -> rim_rect)."""
    hy = thick / 2
    before = set(bm.faces)
    front = [bm.verts.new((x, hy, z)) for (x, z) in outline]
    back = [bm.verts.new((x, -hy, z)) for (x, z) in outline]
    n = len(outline)
    f_front = bm.faces.new(front)
    f_back = bm.faces.new(list(reversed(back)))
    sides = [bm.faces.new((front[(i + 1) % n], front[i], back[i], back[(i + 1) % n])) for i in range(n)]
    bm.normal_update()
    if f_front.normal.y < 0:
        for f in [f_front, f_back] + sides:
            f.normal_flip()
    edges = list(f_front.edges) + list(f_back.edges)
    bmesh.ops.bevel(bm, geom=edges, offset=bevel, segments=2, affect='EDGES', profile=0.5)
    bm.normal_update()
    faces = [f for f in bm.faces if f not in before]
    perim = [0.0]
    for i in range(1, n + 1):
        p0, p1 = outline[i - 1], outline[i % n]
        perim.append(perim[-1] + math.hypot(p1[0] - p0[0], p1[1] - p0[1]))
    total = perim[-1]

    def perim_at(x, z):
        best, bi = 1e9, 0
        for i, (px, pz) in enumerate(outline):
            d = (px - x) ** 2 + (pz - z) ** 2
            if d < best:
                best, bi = d, i
        return perim[bi] / total

    x0, x1 = x_range
    z0, z1 = z_range
    counts = {"front": 0, "back": 0, "rim": 0}
    for f in faces:
        ny = f.normal.y
        kind = "front" if ny > 0.95 else "back" if ny < -0.95 else "rim"
        counts[kind] += 1
        for loop in f.loops:
            co = loop.vert.co
            u = (co.x - x0) / (x1 - x0)
            v = (co.z - z0) / (z1 - z0)
            if kind == "front":
                loop[uv_layer].uv = lerp_rect(front_rect, u, v)
            elif kind == "back":
                loop[uv_layer].uv = lerp_rect(back_rect, 1.0 - u, v)
            else:
                loop[uv_layer].uv = lerp_rect(rim_rect, perim_at(co.x, co.z), co.y / thick + 0.5)
    log(f"{name}: outline {n} points, thickness {thick} m, UV faces {counts}")
    return faces


def build_lathe(bm, uv_layer, profile, rect, segments=20):
    """profile = [(z, r), ...] bottom to top. Cylindrical UV into rect (u = angle, v = profile index)."""
    rings = []
    for (z, r) in profile:
        ring = []
        for s in range(segments + 1):          # seam duplicated for a clean UV
            a = 2 * math.pi * s / segments
            ring.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)) if s < segments else None)
        ring[segments] = ring[0]
        rings.append(ring)
    faces = []
    nz = len(profile)
    for k in range(nz - 1):
        for s in range(segments):
            f = bm.faces.new((rings[k][s], rings[k][s + 1], rings[k + 1][s + 1], rings[k + 1][s]))
            faces.append((f, k, s))
    for f, k, s in faces:
        for loop in f.loops:
            vk = k if loop.vert in (rings[k][s], rings[k][s + 1]) else k + 1
            vs = s if loop.vert in (rings[k][s], rings[k + 1][s]) else s + 1
            if rings[k][s] is rings[k][s + 1]:
                vs = s
            loop[uv_layer].uv = lerp_rect(rect, vs / segments, vk / (nz - 1))
    # caps
    for idx in (0, nz - 1):
        ring = rings[idx][:segments]
        cap = bm.faces.new(ring if idx == nz - 1 else list(reversed(ring)))
        for loop in cap.loops:
            co = loop.vert.co
            r = max(profile[idx][1], 1e-6)
            loop[uv_layer].uv = lerp_rect(rect, 0.5 + 0.5 * co.x / r * 0.1, 0.5 + 0.5 * co.y / r * 0.1)
    return [f for f, _, _ in faces]


# ---------------------------------------------------------------------------------------------------- generic parts
def add_primitive_mesh(maker, rect, **kw):
    """Creates a temp object with a bpy.ops primitive, maps its UV box into rect, returns its mesh data copy."""
    maker(**kw)
    obj = bpy.context.active_object
    me = obj.data
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    us = [d.uv.x for d in uv]
    vs = [d.uv.y for d in uv]
    u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
    for d in uv:
        d.uv = lerp_rect(rect, (d.uv.x - u0) / max(u1 - u0, 1e-6), (d.uv.y - v0) / max(v1 - v0, 1e-6))
    return obj
