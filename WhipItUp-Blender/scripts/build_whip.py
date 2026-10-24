"""Builds the whip 'Whip' (one skinned mesh, one material, one UV atlas) + its armature 'WhipRig' in the open .blend.

Run inside Blender 5.x (exec the file). Layout: whip_layout.py. Re-running replaces both objects.
Parts: wolf-icon pommel disc, the paddle's handle (R1 band ring, wood + spiral grip + wood core, R2, tall valknut ring, R3: handle_layout.py,
grip_tools.py, ring_tools.py), thong core + modelled braid (2 flat ribbons each way, plain weave, thin edge strings, D-073), flat fall, cracker.
Bones: 'root' (handle, weight 1) + one chain 'fall_00_0'..'fall_00_37' along the lash (BONES in whip_layout.py; the name pattern FallChains looks for).
Rings (D-066): valknut rings on bones 8 / 16 / 24 and a small one at the cracker tip, each rigid and weighted 100 % to its ONE bone.
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
import handle_layout as HL  # noqa: E402
import paddle_layout as PL  # noqa: E402
importlib.reload(HL)        # reload the layouts BEFORE whip_layout imports them (Blender keeps old modules between runs)
importlib.reload(PL)
import whip_layout as W  # noqa: E402
import mesh_tools as T  # noqa: E402
import ring_tools as R  # noqa: E402
import grip_tools as G  # noqa: E402
importlib.reload(W)
importlib.reload(T)
importlib.reload(R)
importlib.reload(G)
from mesh_tools import lerp_rect, build_slab  # noqa: E402

log = T.log


def build_thong(bm, uv):
    """The core tube along +Z under the braid (radius W.core_radius, tapering). UVs are per face corner, so the core texture repeats
    every THONG_UV_REPEAT metres (k rings per repeat) without duplicated vertices."""
    n = W.THONG_SIDES
    k = max(1, round(W.THONG_UV_REPEAT * W.THONG_RINGS_PER_M))       # rings per texture repeat
    rings_total = int(round(W.THONG_LENGTH / W.THONG_UV_REPEAT * k))
    out = []
    rings = []
    for i in range(rings_total + 1):
        s = W.THONG_LENGTH * i / rings_total
        r = W.core_radius(s)
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


def _grid(bm, pts):
    """Vertices for rows of points ([[Vector]]). Returns the vertex grid."""
    return [[bm.verts.new(p) for p in row] for row in pts]


def _quads(bm, uv, grid, uv_point, uv_fn=None):
    """Quad faces between neighbouring rows / columns of a vertex grid, wound so the normals point away from the thong axis.
    UVs: one flat `uv_point`, or per face corner `uv_fn(face_row, row, col)` (face_row = lower row of the quad) when given."""
    r0, r1 = len(grid) // 2, len(grid) // 2 + 1
    a, b, d = grid[r0][0].co, grid[r0][1].co, grid[r1][0].co
    radial = Vector((a.x, a.y, 0.0))
    flip = (b - a).cross(d - a).dot(radial) < 0
    faces = []
    for i in range(len(grid) - 1):
        for j in range(len(grid[0]) - 1):
            cells = ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))
            quad = tuple(grid[r][c] for r, c in cells)
            f = bm.faces.new(tuple(reversed(quad)) if flip else quad)
            faces.append(f)
            at = {grid[r][c]: (r, c) for r, c in cells}
            for loop in f.loops:
                loop[uv].uv = uv_fn(i, *at[loop.vert]) if uv_fn else uv_point
    return faces, flip


def build_braid(bm, uv):
    """The modelled braid (D-073): BRAID_STRANDS flat ribbons each way round the core, plain weave (W.ribbon_height), plus a thin string along
    both edges of every ribbon. Returns [(vert, s)] for the bone weights."""
    n = int(math.ceil(W.THONG_LENGTH / W.BRAID_STEP))
    ss = [min(W.THONG_LENGTH, i * W.BRAID_STEP) for i in range(n + 1)]
    out = []
    ribbons = edges = 0
    for direction in (1, -1):
        for strand in range(W.BRAID_STRANDS):
            pts = []
            for s in ss:
                row = []
                for j in range(W.BRAID_COLS + 1):
                    w = j / W.BRAID_COLS
                    ang = W.ribbon_angle(direction, strand, w, s)
                    r = W.ribbon_radius(direction, w, s)
                    row.append(Vector((r * math.cos(ang), r * math.sin(ang), W.THONG_START + s)))
                pts.append(row)
            grid = _grid(bm, pts)
            K = W.BRAID_CHECK_RINGS

            def checker_uv(face_row, row, col, direction=direction):
                """Per-corner UV into the checker rect: ring index counted from the start of the colour period this face lies in."""
                return W.braid_uv(direction, col / W.BRAID_COLS, row - (face_row // K) * K)

            _quads(bm, uv, grid, None, uv_fn=checker_uv)
            out += [(v, ss[i]) for i, row in enumerate(grid) for v in row]
            ribbons += 1
            for w in (0.0, 1.0):
                epts = []
                for s in ss:
                    ang = W.ribbon_angle(direction, strand, w, s)
                    r = W.ribbon_radius(direction, w, s) + W.EDGE_LIFT
                    d = (W.EDGE_WIDTH / 2) / r
                    epts.append([Vector((r * math.cos(ang + k * d), r * math.sin(ang + k * d), W.THONG_START + s)) for k in (-1, 1)])
                egrid = _grid(bm, epts)
                _quads(bm, uv, egrid, lerp_rect(W.UV_GOLD, *W.GOLD_UV))
                out += [(v, ss[i]) for i, row in enumerate(egrid) for v in row]
                edges += 1
    log(f"braid: {ribbons} ribbons + {edges} edge strings, {len(ss)} rings of {W.BRAID_STEP * 1000:.1f} mm, turn {W.BRAID_TURN:.3f} m, "
        f"radius {W.thong_radius(0):.4f} -> {W.thong_radius(W.THONG_LENGTH):.4f} m, {len(out)} verts")
    return out


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
        spread = Vector((math.cos(a), math.sin(a), 0)) * W.CRACKER_SPREAD
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


def build_lash_rings(bm, uv):
    """The valknut rings on the lash (D-066). Returns {bone index: [verts]}. The ring squares are smaller than on the handle (W.RING_SQUARE)."""
    out = {}
    saved = HL.SQUARE
    try:
        specs = [(b, W.THONG_START + W.BONE_LENGTH * (b + 0.5) - W.RING_HEIGHT / 2, W.RING_HEIGHT, W.RING_RADIUS, W.RING_SQUARE) for b in W.RING_BONES]
        tip0 = W.THONG_START + W.TOTAL_LASH - W.TIP_RING_HEIGHT
        specs.append((W.BONES - 1, tip0, W.TIP_RING_HEIGHT, W.TIP_RING_RADIUS, W.TIP_RING_SQUARE))
        for bone, z0, h, r, sq in specs:
            HL.SQUARE = sq
            before = set(bm.verts)
            R.build_valknut_ring(bm, uv, z0, z0 + h, r, HL.RING_RECTS)
            out[bone] = [v for v in bm.verts if v not in before]
            log(f"lash ring on bone {bone + 1}: z {z0:.3f}..{z0 + h:.3f} r {r:.4f} square {sq:.3f} ({len(out[bone])} verts, thong r there {W.thong_radius(max(0.0, z0 + h / 2 - W.THONG_START)):.4f})")
    finally:
        HL.SQUARE = saved
    return out


def smooth_plait_edges(me, plait_idx):
    """The cords and gold strings are round tubes with few corners: clear the sharp-edge mark on every edge between two plait vertices so they shade smooth."""
    attr = me.attributes.get("sharp_edge")
    if attr is None:
        log("plait smoothing: no sharp_edge attribute (nothing to clear)")
        return 0
    cleared = 0
    for e in me.edges:
        a, b = e.vertices
        if a in plait_idx and b in plait_idx and attr.data[e.index].value:
            attr.data[e.index].value = False
            cleared += 1
    log(f"plait smoothing: cleared {cleared} sharp edges of {len(me.edges)}")
    return cleared


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
    seg = W.BONE_LENGTH
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
    # the paddle's handle (D-064 / D-069): R1, wood + spiral grip, wood core through R2 / V / R3, R2, tall valknut ring, R3
    grip = HL.grip_spec(W.GRIP_BOTTOM, W.GRIP_TOP, W.GRIP_RADIUS)
    G.build_grip(bm, uv, grip, HL.GRIP_RECTS)
    G.build_core(bm, uv, grip, W.RINGS["top"] - HL.CORE_UNDER_TOP, HL.GRIP_RECTS)
    R.build_handle_rings(bm, uv, W.RINGS, HL.RING_RECTS)
    handle = {v for v in bm.verts}
    lash, end_ring = build_thong(bm, uv)
    plait = build_braid(bm, uv)
    lash += plait
    lash += build_fall(bm, uv, end_ring)
    ring_verts = build_lash_rings(bm, uv)
    bm.to_mesh(me)
    handle_idx = [v.index for v in handle]
    lash_w = [(v.index, s) for v, s in lash]
    plait_idx = {v.index for v, _ in plait}
    ring_idx = {b: [v.index for v in vs] for b, vs in ring_verts.items()}
    bm.free()

    obj = bpy.data.objects.new("Whip", me)
    bpy.context.scene.collection.objects.link(obj)
    rig = build_armature()
    groups = {"root": obj.vertex_groups.new(name="root")}
    for b in range(W.BONES):
        groups["fall_00_%d" % b] = obj.vertex_groups.new(name="fall_00_%d" % b)
    groups["root"].add(handle_idx, 1.0, 'REPLACE')
    for b, idxs in ring_idx.items():
        groups["fall_00_%d" % b].add(idxs, 1.0, 'REPLACE')      # rigid: 100 % on ONE bone
    seg = W.BONE_LENGTH
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
    smooth_plait_edges(me, plait_idx)

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
