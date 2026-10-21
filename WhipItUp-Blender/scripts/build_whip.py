"""Builds the whip 'Whip' (one skinned mesh, one material, one UV atlas) + its armature 'WhipRig' in the open .blend.

Run inside Blender 5.x (exec the file). Layout: whip_layout.py. Re-running replaces both objects.
Bones: 'root' (handle, weight 1) + one chain 'fall_00_0'..'fall_00_15' along the lash (the name pattern FallChains looks for).
"""
import importlib
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.path.join(os.path.dirname(bpy.data.filepath), "scripts")
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)
import whip_layout as W  # noqa: E402
import mesh_tools as T  # noqa: E402
importlib.reload(W)
importlib.reload(T)
from mesh_tools import lerp_rect, build_slab, build_lathe  # noqa: E402

log = T.log


def build_thong(bm, uv):
    """Tapering braided tube along +Z. UVs are per face corner, so the braid texture repeats every THONG_UV_REPEAT metres
    (k rings per repeat) without duplicated vertices."""
    n = W.THONG_SIDES
    k = max(1, round(W.THONG_UV_REPEAT * W.THONG_RINGS_PER_M))       # rings per texture repeat
    rings_total = int(round(W.THONG_LENGTH / W.THONG_UV_REPEAT * k))
    out = []
    rings = []
    for i in range(rings_total + 1):
        s = W.THONG_LENGTH * i / rings_total
        r = W.thong_radius(s)
        ring = [bm.verts.new((r * math.cos(2 * math.pi * j / n), r * math.sin(2 * math.pi * j / n), W.THONG_START + s)) for j in range(n)]
        out += [(v, s) for v in ring]
        rings.append((s, ring))
    for i in range(rings_total):
        pring, ring = rings[i][1], rings[i + 1][1]
        v0 = (i % k) / k
        v1 = v0 + 1.0 / k
        for j in range(n):
            a, b, c, d = pring[j], pring[(j + 1) % n], ring[(j + 1) % n], ring[j]
            f = bm.faces.new((a, b, c, d))
            for loop in f.loops:
                u = j / n if loop.vert in (a, d) else (j + 1) / n
                loop[uv].uv = lerp_rect(W.UV_THONG, u, v0 if loop.vert in (a, b) else v1)
    bm.faces.new(list(reversed(rings[0][1])))
    return out, rings[-1]


def build_fall(bm, uv, end_ring):
    """Flat fall strip from the braid end, then a few thin cracker strands."""
    s0, ring = end_ring
    z0 = W.THONG_START + s0
    hw, ht = W.FALL_WIDTH / 2, W.FALL_THICK / 2
    segs = 8
    out = []
    rows = []
    for i in range(segs + 1):
        t = i / segs
        z = z0 + W.FALL_LENGTH * t
        w = hw * (1.0 - 0.3 * t)
        rows.append([bm.verts.new((x * w, y * ht, z)) for (x, y) in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
        out += [(v, s0 + W.FALL_LENGTH * t) for v in rows[-1]]
    for i in range(segs):
        for j in range(4):
            a, b = rows[i][j], rows[i][(j + 1) % 4]
            c, d = rows[i + 1][(j + 1) % 4], rows[i + 1][j]
            f = bm.faces.new((a, b, c, d))
            for loop in f.loops:
                jj = [a, b, c, d].index(loop.vert)
                corner = j if jj in (0, 3) else (j + 1) % 4
                u = 1.0 if corner in (1, 2) else 0.0
                v = (i if jj in (0, 1) else i + 1) / segs * 0.7
                loop[uv].uv = lerp_rect(W.UV_FALL, u, v)
    # cracker strands
    zc = z0 + W.FALL_LENGTH
    for k in range(W.CRACKER_STRANDS):
        a = 2 * math.pi * k / W.CRACKER_STRANDS
        off = Vector((math.cos(a), math.sin(a), 0)) * 0.0012
        spread = Vector((math.cos(a), math.sin(a), 0)) * 0.012
        n = 4
        rr = []
        for i in range(5):
            t = i / 4
            c = Vector((0, 0, zc + W.CRACKER_LENGTH * t)) + off + spread * (t * t)
            r = W.CRACKER_RADIUS * (1 - 0.6 * t)
            rr.append([bm.verts.new(c + Vector((r * math.cos(2 * math.pi * j / n), r * math.sin(2 * math.pi * j / n), 0))) for j in range(n)])
            out += [(v, s0 + W.FALL_LENGTH + W.CRACKER_LENGTH * t) for v in rr[-1]]
        for i in range(4):
            for j in range(n):
                f = bm.faces.new((rr[i][j], rr[i][(j + 1) % n], rr[i + 1][(j + 1) % n], rr[i + 1][j]))
                for loop in f.loops:
                    loop[uv].uv = lerp_rect(W.UV_FALL, 0.5, 0.75 + 0.25 * i / 4)
    return out


def build_armature():
    old = bpy.data.objects.get("WhipRig")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    for a in [a for a in bpy.data.armatures if a.users == 0]:
        bpy.data.armatures.remove(a)
    arm = bpy.data.armatures.new("WhipRig")
    rig = bpy.data.objects.new("WhipRig", arm)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new("root")
    root.head = (0, 0, 0)
    root.tail = (0, 0, W.THONG_START)
    seg = W.TOTAL_LASH / W.BONES
    parent = root
    for b in range(W.BONES):
        eb = arm.edit_bones.new("fall_00_%d" % b)
        eb.head = (0, 0, W.THONG_START + seg * b)
        eb.tail = (0, 0, W.THONG_START + seg * (b + 1))
        eb.parent = parent
        eb.use_connect = b > 0
        parent = eb
    bpy.ops.object.mode_set(mode='OBJECT')
    log(f"armature: root + {W.BONES} lash bones ({seg:.3f} m each)")
    return rig


def main():
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    old = bpy.data.objects.get("Whip")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    me = bpy.data.meshes.new("Whip")
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    cz, r, t = W.POMMEL_DISC
    outline = [(r * math.cos(2 * math.pi * k / 40), cz + r * math.sin(2 * math.pi * k / 40)) for k in range(40)]
    build_slab(bm, uv, outline, t, W.POMMEL_BEVEL, W.UV_POMMEL_FACE, W.UV_POMMEL_FACE, W.UV_METAL, (-r, r), (cz - r, cz + r), "pommel disc")
    z0, z1, fr = W.FERRULE_BOTTOM
    build_lathe(bm, uv, [(z0, fr - 0.0015), (z0 + 0.0015, fr), (z1 - 0.0015, fr), (z1, fr - 0.0015)], W.UV_METAL, segments=18)
    mid, half = (W.GRIP_BOTTOM + W.GRIP_TOP) / 2, (W.GRIP_TOP - W.GRIP_BOTTOM) / 2
    zs = [W.GRIP_BOTTOM + (W.GRIP_TOP - W.GRIP_BOTTOM) * i / 12 for i in range(13)]
    build_lathe(bm, uv, [(z, W.GRIP_RADIUS + W.GRIP_SWELL * (1 - ((z - mid) / half) ** 2)) for z in zs], W.UV_GRIP, segments=20)
    c0, c1, cr = W.COLLAR
    build_lathe(bm, uv, [(c0, cr - 0.002), (c0 + 0.002, cr), (c1 - 0.002, cr), (c1, cr - 0.002)], W.UV_COLLAR, segments=24)
    handle = {v for v in bm.verts}
    lash, end_ring = build_thong(bm, uv)
    lash += build_fall(bm, uv, end_ring)
    bm.to_mesh(me)
    handle_idx = [v.index for v in handle]
    lash_w = [(v.index, s) for v, s in lash]
    bm.free()

    obj = bpy.data.objects.new("Whip", me)
    bpy.context.scene.collection.objects.link(obj)
    rig = build_armature()
    groups = {"root": obj.vertex_groups.new(name="root")}
    for b in range(W.BONES):
        groups["fall_00_%d" % b] = obj.vertex_groups.new(name="fall_00_%d" % b)
    groups["root"].add(handle_idx, 1.0, 'REPLACE')
    seg = W.TOTAL_LASH / W.BONES
    for idx, s in lash_w:
        x = max(0.0, s / seg - 0.5)
        b0 = min(W.BONES - 1, int(x))
        b1 = min(W.BONES - 1, b0 + 1)
        w1 = min(1.0, x - b0) if b1 != b0 else 0.0
        if s < 0.03:
            groups["root"].add([idx], 1.0 - s / 0.03, 'REPLACE')
            groups["fall_00_0"].add([idx], s / 0.03, 'REPLACE')
            continue
        groups["fall_00_%d" % b0].add([idx], 1.0 - w1, 'REPLACE')
        if w1 > 0:
            groups["fall_00_%d" % b1].add([idx], w1, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))

    mat = bpy.data.materials.get("whip_mat") or bpy.data.materials.new("whip_mat")
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    tex = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "textures", "whip_Albedo_V1.png"))
    if os.path.exists(tex):
        mat.use_nodes = True
        nt = mat.node_tree
        img = nt.nodes.get("albedo") or nt.nodes.new("ShaderNodeTexImage")
        img.name = "albedo"
        img.image = bpy.data.images.load(tex, check_existing=True)
        img.image.reload()
        nt.links.new(img.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
        nrm = os.path.join(os.path.dirname(tex), "whip_Normal.png")
        if os.path.exists(nrm):
            ni = nt.nodes.get("normal") or nt.nodes.new("ShaderNodeTexImage")
            ni.name = "normal"
            ni.image = bpy.data.images.load(nrm, check_existing=True)
            ni.image.reload()
            ni.image.colorspace_settings.name = "Non-Color"
            nm = nt.nodes.get("nmap") or nt.nodes.new("ShaderNodeNormalMap")
            nm.name = "nmap"
            nt.links.new(ni.outputs["Color"], nm.inputs["Color"])
            nt.links.new(nm.outputs["Normal"], nt.nodes["Principled BSDF"].inputs["Normal"])
        log("material: textures linked")
    else:
        log("material: no textures yet (run whip_textures.py)")
    zs = [v.co.z for v in me.vertices]
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    log(f"Whip: {len(me.vertices)} verts, {tris} tris, z {min(zs):.3f}..{max(zs):.3f} (length {max(zs) - min(zs):.3f} m)")
    return {"log": list(T.LOG), "verts": len(me.vertices), "tris": tris, "length": max(zs) - min(zs)}


RESULT = main()
