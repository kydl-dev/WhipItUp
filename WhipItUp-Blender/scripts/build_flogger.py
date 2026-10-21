"""Builds the flogger 'Flogger' (one skinned mesh, one material, one UV atlas) + its armature 'FloggerRig' in the open .blend.

Run inside Blender 5.x (Blender MCP: exec the file). Layout: flogger_layout.py. Re-running replaces both objects.
Parts: wolf-icon pommel disc, ferrule, two-tone braided grip, knotwork collar, 12 fall chains (10 x 4 flat strips, 2 braided round falls).
Bones: 'root' (handle, weight 1 for every non-fall vertex) + per chain 'fall_KK_B' (B = 0..4 along the fall), parented in a chain under root.
The mod's FallChains component simulates those bones (springs + gravity) in game.
"""
import importlib
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.path.join(os.path.dirname(bpy.data.filepath), "scripts")
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)
import flogger_layout as F  # noqa: E402
import mesh_tools as T  # noqa: E402
importlib.reload(F)
importlib.reload(T)
from mesh_tools import lerp_rect, build_slab, build_lathe  # noqa: E402

log = T.log


def chain_frame(k):
    """Start point and unit direction of chain k at rest."""
    a = F.chain_angle(k)
    radial = Vector((math.cos(a), math.sin(a), 0.0))
    start = Vector((0, 0, F.FALL_START)) + radial * F.CHAIN_RING_RADIUS
    tilt = math.radians(F.FALL_SPREAD_DEG)
    direction = (Vector((0, 0, 1)) * math.cos(tilt) + radial * math.sin(tilt)).normalized()
    return start, direction, radial


def build_strip(bm, uv_layer, start, direction, side, length, fan):
    """Flat leather strip (thin box) along direction; side = its width axis. Returns [(vert, s along fall 0..1)]."""
    normal = direction.cross(side).normalized()
    hw, ht = F.STRIP_WIDTH / 2, F.STRIP_THICK / 2
    base = start + side * fan
    rings = []
    for i in range(F.STRIP_SEGMENTS + 1):
        s = i / F.STRIP_SEGMENTS
        c = base + direction * (length * s)
        taper = 1.0 - 0.25 * s
        rings.append([bm.verts.new(c + side * (sx * hw * taper) + normal * (sy * ht)) for (sx, sy) in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    out = []
    for i in range(F.STRIP_SEGMENTS):
        for j in range(4):
            a, b = rings[i][j], rings[i][(j + 1) % 4]
            c, d = rings[i + 1][(j + 1) % 4], rings[i + 1][j]
            f = bm.faces.new((a, b, c, d))
            for loop in f.loops:
                vi = loop.vert
                ii = i if vi in (a, b) else i + 1
                jj = j if vi in (a, d) else (j + 1) % 4
                u = 1.0 if jj in (1, 2) else 0.0        # ring corners: 0 (-1,-1) 1 (1,-1) 2 (1,1) 3 (-1,1): u across the width
                loop[uv_layer].uv = lerp_rect(F.UV_STRIP, u, ii / F.STRIP_SEGMENTS * (length / (F.FALL_LENGTH + F.FALL_LENGTH_JITTER)))
    # end caps
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    for i, ring in enumerate(rings):
        for v in ring:
            out.append((v, i / F.STRIP_SEGMENTS * length / F.FALL_LENGTH))
    return out


def build_braid_fall(bm, uv_layer, start, direction, length):
    side = direction.orthogonal().normalized()
    other = direction.cross(side).normalized()
    n = F.BRAID_SIDES
    rings = []
    for i in range(F.STRIP_SEGMENTS + 1):
        s = i / F.STRIP_SEGMENTS
        c = start + direction * (length * s)
        r = F.BRAID_RADIUS * (1.0 - 0.3 * s)
        ring = [bm.verts.new(c + (side * math.cos(2 * math.pi * j / n) + other * math.sin(2 * math.pi * j / n)) * r) for j in range(n)]
        rings.append(ring)
    for i in range(F.STRIP_SEGMENTS):
        for j in range(n):
            a, b = rings[i][j], rings[i][(j + 1) % n]
            c, d = rings[i + 1][(j + 1) % n], rings[i + 1][j]
            f = bm.faces.new((a, b, c, d))
            for loop in f.loops:
                vi = loop.vert
                ii = i if vi in (a, b) else i + 1
                jj = j + (0 if vi in (a, d) else 1)
                loop[uv_layer].uv = lerp_rect(F.UV_BRAID_FALL, jj / n, ii / F.STRIP_SEGMENTS)
    bm.faces.new(list(reversed(rings[0])))
    tip = bm.verts.new(start + direction * (length + 0.006))
    for j in range(n):
        f = bm.faces.new((rings[-1][j], rings[-1][(j + 1) % n], tip))
        for loop in f.loops:
            loop[uv_layer].uv = lerp_rect(F.UV_BRAID_FALL, 0.5, 1.0)
    return [(v, i / F.STRIP_SEGMENTS * length / F.FALL_LENGTH) for i, ring in enumerate(rings) for v in ring] + [(tip, (length + 0.006) / F.FALL_LENGTH)]


def build_armature():
    old = bpy.data.objects.get("FloggerRig")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    arm = bpy.data.armatures.new("FloggerRig")
    rig = bpy.data.objects.new("FloggerRig", arm)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new("root")
    root.head = (0, 0, 0)
    root.tail = (0, 0, F.FALL_START)
    seg = F.FALL_LENGTH / F.BONES_PER_CHAIN
    for k in range(F.CHAINS):
        start, d, _ = chain_frame(k)
        parent = root
        for b in range(F.BONES_PER_CHAIN):
            eb = arm.edit_bones.new("fall_%02d_%d" % (k, b))
            eb.head = start + d * (seg * b)
            eb.tail = start + d * (seg * (b + 1))
            eb.parent = parent
            eb.use_connect = b > 0
            parent = eb
    bpy.ops.object.mode_set(mode='OBJECT')
    log(f"armature: root + {F.CHAINS} chains x {F.BONES_PER_CHAIN} bones")
    return rig


def main():
    random.seed(7)
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    old = bpy.data.objects.get("Flogger")
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)

    me = bpy.data.meshes.new("Flogger")
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    # handle parts (all weight root)
    cz, r, t = F.POMMEL_DISC
    outline = [(r * math.cos(2 * math.pi * k / 40), cz + r * math.sin(2 * math.pi * k / 40)) for k in range(40)]
    build_slab(bm, uv, outline, t, F.POMMEL_BEVEL, F.UV_POMMEL_FACE, F.UV_POMMEL_FACE, F.UV_METAL, (-r, r), (cz - r, cz + r), "pommel disc")
    z0, z1, fr = F.FERRULE_BOTTOM
    build_lathe(bm, uv, [(z0, fr - 0.0015), (z0 + 0.0015, fr), (z1 - 0.0015, fr), (z1, fr - 0.0015)], F.UV_METAL, segments=18)
    mid, half = (F.GRIP_BOTTOM + F.GRIP_TOP) / 2, (F.GRIP_TOP - F.GRIP_BOTTOM) / 2
    zs = [F.GRIP_BOTTOM + (F.GRIP_TOP - F.GRIP_BOTTOM) * i / 12 for i in range(13)]
    build_lathe(bm, uv, [(z, F.GRIP_RADIUS + F.GRIP_SWELL * (1 - ((z - mid) / half) ** 2)) for z in zs], F.UV_GRIP, segments=20)
    c0, c1, cr = F.COLLAR
    build_lathe(bm, uv, [(c0, cr - 0.002), (c0 + 0.002, cr), (c1 - 0.002, cr), (c1, cr - 0.002)], F.UV_COLLAR, segments=24)
    handle_verts = set(bm.verts)

    # falls
    fall_verts = []      # (vert, chain, s)
    for k in range(F.CHAINS):
        start, d, radial = chain_frame(k)
        if k in F.BRAIDED_CHAINS:
            L = F.FALL_LENGTH + random.uniform(-F.FALL_LENGTH_JITTER, F.FALL_LENGTH_JITTER)
            fall_verts += [(v, k, s) for (v, s) in build_braid_fall(bm, uv, start, d, L)]
            continue
        side = d.cross(radial).normalized()        # strips lie tangentially around the ring
        for j in range(F.STRIPS_PER_CHAIN):
            L = F.FALL_LENGTH + random.uniform(-F.FALL_LENGTH_JITTER, F.FALL_LENGTH_JITTER)
            fan = (j - (F.STRIPS_PER_CHAIN - 1) / 2) * F.STRIP_FAN
            fall_verts += [(v, k, s) for (v, s) in build_strip(bm, uv, start + radial * (0.0008 * j), d, side, L, fan)]
    bm.to_mesh(me)
    handle_idx = {v.index for v in handle_verts}
    weights = [(v.index, k, s) for (v, k, s) in fall_verts]
    bm.free()

    obj = bpy.data.objects.new("Flogger", me)
    bpy.context.scene.collection.objects.link(obj)
    rig = build_armature()

    # skin: root for the handle, two neighbouring bones of the chain for the falls (linear blend along the fall)
    groups = {"root": obj.vertex_groups.new(name="root")}
    for k in range(F.CHAINS):
        for b in range(F.BONES_PER_CHAIN):
            n = "fall_%02d_%d" % (k, b)
            groups[n] = obj.vertex_groups.new(name=n)
    groups["root"].add(list(handle_idx), 1.0, 'REPLACE')
    for idx, k, s in weights:
        x = max(0.0, s * F.BONES_PER_CHAIN - 0.5)            # bone centres at 0.5, 1.5, ...
        b0 = min(F.BONES_PER_CHAIN - 1, int(x))
        b1 = min(F.BONES_PER_CHAIN - 1, b0 + 1)
        w1 = min(1.0, x - b0) if b1 != b0 else 0.0
        if s < 0.04:                                        # the first millimetres stay with the handle (no tearing at the collar)
            groups["root"].add([idx], 1.0 - s / 0.04, 'REPLACE')
            groups["fall_%02d_0" % k].add([idx], s / 0.04, 'REPLACE')
            continue
        groups["fall_%02d_%d" % (k, b0)].add([idx], 1.0 - w1, 'REPLACE')
        if w1 > 0:
            groups["fall_%02d_%d" % (k, b1)].add([idx], w1, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))

    mat = bpy.data.materials.get("flogger_mat") or bpy.data.materials.new("flogger_mat")
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    tex = os.path.normpath(os.path.join(SCRIPT_DIR, "..", "textures", "flogger_Albedo_V1.png"))
    if os.path.exists(tex):
        mat.use_nodes = True
        nt = mat.node_tree
        img = nt.nodes.get("albedo") or nt.nodes.new("ShaderNodeTexImage")
        img.name = "albedo"
        img.image = bpy.data.images.load(tex, check_existing=True)
        img.image.reload()
        nt.links.new(img.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
        nrm = os.path.join(os.path.dirname(tex), "flogger_Normal.png")
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
        log("material: no textures yet (run flogger_textures.py)")

    zs = [v.co.z for v in me.vertices]
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    log(f"Flogger: {len(me.vertices)} verts, {tris} tris, z {min(zs):.3f}..{max(zs):.3f} (length {max(zs) - min(zs):.3f} m), {len(groups)} vertex groups")
    return {"log": list(T.LOG), "verts": len(me.vertices), "tris": tris, "length": max(zs) - min(zs)}


RESULT = main()
