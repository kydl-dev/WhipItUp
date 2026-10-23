"""Builds the paddle mesh 'Paddle' (one object, one material, one UV atlas) in the open .blend.

Run inside Blender 5.x (Blender MCP, or: blender -b source/Paddle.blend --python scripts/build_paddle.py).
Layout and UV regions: paddle_layout.py (shared with paddle_textures.py). Re-running replaces the old 'Paddle' object.
Parts: round metal pommel disc (TrophyWolf inlay), handle (D-064: R1 band ring, wood + spiral grip, R2 band ring, tall valknut ring, R3 band ring),
knotwork guard + rivets, blade with studded rim. Handle parts: handle_layout.py, grip_tools.py, ring_tools.py (shared with flogger / whip).
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
import handle_layout as HL
import paddle_layout as L
import mesh_tools as T
import ring_tools as R
import grip_tools as G
importlib.reload(HL)
importlib.reload(L)
importlib.reload(T)
importlib.reload(R)
importlib.reload(G)
from mesh_tools import lerp_rect, build_slab, build_lathe, add_primitive_mesh  # noqa: E402

LOG = T.LOG


def log(msg):
    T.log(msg)


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------------------------------- blade
blade_half_width = L.blade_half_width
blade_outline = L.blade_outline


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

    grip = dict(bottom=L.HANDLE_BOTTOM, top=L.HANDLE_TOP, radius=L.HANDLE_RADIUS, swell=L.HANDLE_SWELL,
                wrap_bottom=L.WRAP_BOTTOM, wrap_top=L.WRAP_TOP, wrap_extra=L.WRAP_EXTRA)
    G.build_grip(bm, uv_layer, grip, {"wood": L.UV_WOOD, "wrap": L.UV_WRAP})
    G.build_core(bm, uv_layer, grip, L.RINGS["top"] - 0.002, {"wood": L.UV_WOOD})
    R.build_handle_rings(bm, uv_layer, L.RINGS, {"band": L.UV_RING_BAND, "ring_metal": L.UV_RING_METAL, "valk": L.UV_VALK_SQ})
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
