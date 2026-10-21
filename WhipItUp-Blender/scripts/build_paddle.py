"""Builds the paddle mesh 'Paddle' (one object, one material, one UV atlas) in the open .blend.

Run inside Blender 5.x (Blender MCP, or: blender -b source/Paddle.blend --python scripts/build_paddle.py).
Layout and UV regions: paddle_layout.py (shared with paddle_textures.py). Re-running replaces the old 'Paddle' object.
Parts: round metal pommel disc (TrophyWolf inlay), ferrules, wood handle, grip wrap, knotwork guard + rivets, blade with studded rim.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else None
if SCRIPT_DIR is None:
    SCRIPT_DIR = os.path.join(os.path.dirname(bpy.data.filepath), "..", "scripts")
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)
import importlib
import paddle_layout as L
importlib.reload(L)

LOG = []


def log(msg):
    LOG.append(msg)
    print("[build_paddle] " + msg)


def lerp_rect(rect, u, v):
    u0, v0, u1, v1 = rect
    return (u0 + (u1 - u0) * u, v0 + (v1 - v0) * v)


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------------------------------- blade
blade_half_width = L.blade_half_width
blade_outline = L.blade_outline


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


def build_blade(bm, uv_layer):
    return build_slab(bm, uv_layer, blade_outline(), L.BLADE_THICK, L.BLADE_BEVEL, L.UV_BLADE_FRONT, L.UV_BLADE_BACK,
                      L.UV_BLADE_RIM, L.BLADE_X_RANGE, (L.BLADE_BOTTOM, L.BLADE_TOP), "blade")


def build_pommel(bm, uv_layer):
    """Round metal disc at the end of the handle (D-020). Faces carry the vanilla TrophyWolf icon (texture)."""
    cz, r, t = L.POMMEL_DISC
    outline = [(r * math.cos(2 * math.pi * k / 40), cz + r * math.sin(2 * math.pi * k / 40)) for k in range(40)]
    # the face rect covers the whole disc diameter; paddle_textures.py draws the wolf inside POMMEL_FACE_RADIUS and the metal rim outside
    return build_slab(bm, uv_layer, outline, t, L.POMMEL_BEVEL, L.UV_POMMEL_FACE, L.UV_POMMEL_FACE, L.UV_METAL,
                      (-r, r), (cz - r, cz + r), "pommel disc")


# ---------------------------------------------------------------------------------------------------- lathe
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


def build_guard(bm, uv_layer):
    z0, z1, hx, hy = L.GUARD
    ret = bmesh.ops.create_cube(bm, size=1.0)
    verts = ret["verts"]
    for v in verts:
        v.co = Vector((v.co.x * 2 * hx, v.co.y * 2 * hy, z0 + (v.co.z + 0.5) * (z1 - z0)))
    faces = list({f for v in verts for f in v.link_faces})
    edges = list({e for f in faces for e in f.edges})
    geom = bmesh.ops.bevel(bm, geom=edges, offset=0.0035, segments=2, affect='EDGES', profile=0.5)
    faces = list({f for v in verts if v.is_valid for f in v.link_faces} | set(geom.get("faces", [])))
    bm.normal_update()
    for f in faces:
        for loop in f.loops:
            co = loop.vert.co
            u = (co.x + hx) / (2 * hx)
            v = (co.z - z0) / (z1 - z0)
            if f.normal.y > 0.9:
                loop[uv_layer].uv = lerp_rect(L.UV_GUARD_FACE, u, v)
            elif f.normal.y < -0.9:
                loop[uv_layer].uv = lerp_rect(L.UV_GUARD_FACE, 1 - u, v)
            else:
                loop[uv_layer].uv = lerp_rect(L.UV_METAL, u, v)
    return faces


def build_studs(objs):
    length = L.BLADE_TOP - L.BLADE_BOTTOM
    # walk the right edge from STUD_FROM_T to the top and back down the left edge, inset by STUD_INSET
    pts = []
    n = 400
    path = []
    for i in range(n + 1):
        t = L.STUD_FROM_T + (1 - L.STUD_FROM_T) * i / n
        path.append((blade_half_width(t), L.BLADE_BOTTOM + t * length))
    path = path + [(-x, z) for (x, z) in reversed(path)]
    # inset along the 2D normal
    inset = []
    for i, (x, z) in enumerate(path):
        a = path[max(i - 1, 0)]
        b = path[min(i + 1, len(path) - 1)]
        tx, tz = b[0] - a[0], b[1] - a[1]
        ln = math.hypot(tx, tz) or 1.0
        nx, nz = tz / ln, -tx / ln          # outward for the right edge going up
        inset.append((x - nx * L.STUD_INSET, z - nz * L.STUD_INSET))
    acc, last = 0.0, None
    for p in inset:
        if last is None:
            pts.append(p)
        else:
            acc += math.hypot(p[0] - last[0], p[1] - last[1])
            if acc >= L.STUD_SPACING:
                pts.append(p)
                acc = 0.0
        last = p
    # drop a stud that sits too close to the first one (closing the loop at the bottom is open: neck side)
    sy = (L.BLADE_THICK / 2 + 0.0022) / L.STUD_RADIUS
    for (x, z) in pts:
        o = add_primitive_mesh(bpy.ops.mesh.primitive_uv_sphere_add, L.UV_METAL, segments=8, ring_count=5,
                               radius=L.STUD_RADIUS, location=(x, 0, z))
        o.scale = (1, sy, 1)
        objs.append(o)
    log(f"studs: {len(pts)}")
    for x in L.GUARD_RIVET_X:
        o = add_primitive_mesh(bpy.ops.mesh.primitive_uv_sphere_add, L.UV_METAL, segments=10, ring_count=6,
                               radius=L.RIVET_RADIUS, location=(x, 0, L.GUARD_RIVET_Z))
        o.scale = (1, (L.GUARD[3] + 0.0018) / L.RIVET_RADIUS, 1)
        objs.append(o)


# ---------------------------------------------------------------------------------------------------- main
def main():
    old = bpy.data.objects.get("Paddle")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    for m in [m for m in bpy.data.meshes if m.users == 0]:
        bpy.data.meshes.remove(m)
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.scene.cursor.location = (0, 0, 0)

    # 1) bmesh parts (blade, handle, wrap, ferrules, guard)
    me = bpy.data.meshes.new("Paddle")
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    build_blade(bm, uv_layer)
    build_pommel(bm, uv_layer)

    zs = [L.HANDLE_BOTTOM + (L.HANDLE_TOP - L.HANDLE_BOTTOM) * i / 10 for i in range(11)]
    mid = (L.HANDLE_BOTTOM + L.HANDLE_TOP) / 2
    half = (L.HANDLE_TOP - L.HANDLE_BOTTOM) / 2
    wood = [(z, L.HANDLE_RADIUS + L.HANDLE_SWELL * (1 - ((z - mid) / half) ** 2)) for z in zs]
    build_lathe(bm, uv_layer, wood, L.UV_WOOD, segments=18)
    wz = [L.WRAP_BOTTOM + (L.WRAP_TOP - L.WRAP_BOTTOM) * i / 8 for i in range(9)]
    wrap = [(z, L.HANDLE_RADIUS + L.WRAP_EXTRA + L.HANDLE_SWELL * (1 - ((z - mid) / half) ** 2)) for z in wz]
    wrap = [(wz[0] - 0.001, wrap[0][1] - 0.001)] + wrap + [(wz[-1] + 0.001, wrap[-1][1] - 0.001)]
    build_lathe(bm, uv_layer, wrap, L.UV_WRAP, segments=18)
    for (z0, z1, r) in (L.FERRULE_TOP, L.FERRULE_BOTTOM):
        prof = [(z0, r - 0.0015), (z0 + 0.0015, r), (z1 - 0.0015, r), (z1, r - 0.0015)]
        build_lathe(bm, uv_layer, prof, L.UV_METAL, segments=18)
    build_guard(bm, uv_layer)
    bm.to_mesh(me)
    bm.free()
    main_obj = bpy.data.objects.new("Paddle", me)
    bpy.context.scene.collection.objects.link(main_obj)

    # 2) primitive parts (studs, rivets) -> joined into Paddle
    parts = []
    build_studs(parts)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:
        o.select_set(True)
    main_obj.select_set(True)
    bpy.context.view_layer.objects.active = main_obj
    bpy.ops.object.join()
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj = bpy.context.active_object
    obj.name = "Paddle"
    obj.data.name = "Paddle"
    # one UV layer only
    while len(obj.data.uv_layers) > 1:
        obj.data.uv_layers.remove(obj.data.uv_layers[-1])
    obj.data.uv_layers[0].name = "UVMap"
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))

    # 3) material (preview in Blender; Unity builds its own from the vanilla Club material)
    mat = bpy.data.materials.get("paddle_mat") or bpy.data.materials.new("paddle_mat")
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    tex_dir = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "textures"))
    albedo = os.path.join(tex_dir, "paddle_Albedo_V1.png")
    if os.path.exists(albedo):
        mat.use_nodes = True
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        img = nt.nodes.get("albedo") or nt.nodes.new("ShaderNodeTexImage")
        img.name = "albedo"
        img.image = bpy.data.images.load(albedo, check_existing=True)
        img.image.reload()
        nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
        nrm_path = os.path.join(tex_dir, "paddle_Normal.png")
        if os.path.exists(nrm_path):
            nimg = nt.nodes.get("normal") or nt.nodes.new("ShaderNodeTexImage")
            nimg.name = "normal"
            nimg.image = bpy.data.images.load(nrm_path, check_existing=True)
            nimg.image.reload()
            nimg.image.colorspace_settings.name = "Non-Color"
            nmap = nt.nodes.get("nmap") or nt.nodes.new("ShaderNodeNormalMap")
            nmap.name = "nmap"
            nt.links.new(nimg.outputs["Color"], nmap.inputs["Color"])
            nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
        log("material: textures linked from " + tex_dir)
    else:
        log("material: no textures yet (run paddle_textures.py)")

    me = obj.data
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    log(f"Paddle: {len(me.vertices)} verts, {tris} tris, bounds x {min(xs):.3f}..{max(xs):.3f} "
        f"y {min(ys):.3f}..{max(ys):.3f} z {min(zs):.3f}..{max(zs):.3f} (length {max(zs) - min(zs):.3f} m)")
    return {"log": LOG, "verts": len(me.vertices), "tris": tris, "length": max(zs) - min(zs)}


if __name__ == "__main__" or True:
    RESULT = main()
