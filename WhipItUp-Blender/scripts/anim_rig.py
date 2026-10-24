"""T-013 S1: builds WhipAnim.blend = the Valheim player skeleton (rig 'ValheimPlayer', Mixamo bone names, T-pose) from
data/player_skeleton.json (rip of player_maleAvatar), a simple skinned body proxy ('BodyProxy') and the whip ('Whip' + 'WhipRig',
appended from WhipItUp.blend) held in RightHand exactly as in game (RightHand_Attach -> item 'model' -> WhipItUpVisual Align).

Spaces: the project's FBX export (forward -Z, up Y, bake) + Unity's import give Unity = (-bx, bz, -by) (measured: WhipAnim_Body.fbx in
Unity, WhipAnimSetup log), so Blender = K @ u with K = ((-1,0,0),(0,0,-1),(0,1,0)) and a Unity matrix U is K @ U @ K^T in Blender. Run inside Blender (Blender MCP). Prints a log; the result dict is in RESULT.
"""
import json
import math
import os

import bpy
from mathutils import Matrix, Quaternion, Vector

ROOT = globals().get("ROOT", r"E:\Projects\Valheim\WhipItUp\WhipItUp-Blender")
SOURCE_BLEND = os.path.join(ROOT, "WhipItUp.blend")
OUT_BLEND = os.path.join(ROOT, "WhipAnim.blend")
LOG = []

K = Matrix(((-1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))   # Unity -> Blender (orthogonal: inverse = transpose)
SKIP = {"player_male(Clone)", "Armature", "body"}
# proxy thickness (radius in metres) per bone name part; fingers / ends get none
RADIUS = {"Hips": 0.13, "Spine": 0.13, "Spine1": 0.14, "Spine2": 0.15, "Neck": 0.05, "Head": 0.10, "Shoulder": 0.05, "Arm": 0.055,
          "ForeArm": 0.045, "Hand": 0.04, "UpLeg": 0.075, "Leg": 0.055, "Foot": 0.045, "ToeBase": 0.035}


def log(s):
    LOG.append(s)
    print("anim_rig: " + s)


def trs(pos, rot, scale):
    q = Quaternion((rot[3], rot[0], rot[1], rot[2]))
    return Matrix.Translation(Vector(pos)) @ q.to_matrix().to_4x4() @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1))


def to_blender(u):
    return K @ u @ K.transposed()


def fmt(v):
    return "(" + ", ".join("%.4f" % x for x in v) + ")"


def unity_worlds(data):
    bones = {b["name"]: b for b in data["skeleton"]}
    world = {}

    def w(name):
        if name in world:
            return world[name]
        b = bones[name]
        local = trs(b["pos"], b["rot"], b["scale"])
        m = w(b["parent"]) @ local if b["parent"] else local
        world[name] = m
        return m
    for n in bones:
        w(n)
    return bones, world


def radius_for(name):
    key = name.replace("Left", "").replace("Right", "")
    return RADIUS.get(key, 0.0)


def mode(obj, m):
    win = bpy.context.window_manager.windows[0]
    with bpy.context.temp_override(window=win, active_object=obj, object=obj, selected_objects=[obj]):
        bpy.ops.object.mode_set(mode=m)


def build_armature(bones, world):
    arm = bpy.data.armatures.new("ValheimPlayer")
    rig = bpy.data.objects.new("ValheimPlayer", arm)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    mode(rig, 'EDIT')
    children = {}
    for b in bones.values():
        children.setdefault(b["parent"], []).append(b["name"])
    heads = {n: to_blender(world[n]).to_translation() for n in bones}
    made = {}
    for n, b in bones.items():
        if n in SKIP:
            continue
        eb = arm.edit_bones.new(n)
        eb.head = heads[n]
        kids = [k for k in children.get(n, []) if k not in SKIP]
        if n == "Hips":
            kids = ["Spine"]
        if n in ("Spine2",):
            kids = ["Neck"]
        if n.endswith("Hand"):
            kids = [k for k in kids if "Middle1" in k] or kids
        if kids:
            tail = sum((heads[k] for k in kids), Vector()) / len(kids)
        else:
            parent = heads.get(b["parent"], heads[n] - Vector((0, 0, 0.05)))
            d = heads[n] - parent
            tail = heads[n] + (d.normalized() * max(0.02, d.length * 0.5) if d.length > 1e-6 else Vector((0, 0, 0.05)))
        if (tail - eb.head).length < 1e-4:
            tail = eb.head + Vector((0, 0, 0.02))
        eb.tail = tail
        made[n] = eb
    for n, eb in made.items():
        p = bones[n]["parent"]
        if p in made:
            eb.parent = made[p]
            eb.use_connect = False
    mode(rig, 'OBJECT')
    hips = heads["Hips"]
    log("rig ValheimPlayer: %d bones, Hips at %s, Head at %s, RightHand at %s, LeftToeBase at %s" % (
        len(made), fmt(hips), fmt(heads["Head"]), fmt(heads["RightHand"]), fmt(heads["LeftToeBase"])))
    return rig, heads


def build_proxy(rig):
    verts, faces, groups = [], [], []
    for pb in rig.data.bones:
        r = radius_for(pb.name)
        if r <= 0:
            continue
        head, tail = rig.matrix_world @ pb.head_local, rig.matrix_world @ pb.tail_local
        axis = tail - head
        if axis.length < 1e-4:
            continue
        rot = axis.to_track_quat('Z', 'Y').to_matrix()
        seg, start = 10, len(verts)
        for ring, (c, rr) in enumerate(((head, r), (tail, r * 0.85))):
            for i in range(seg):
                a = 2 * math.pi * i / seg
                verts.append(c + rot @ Vector((math.cos(a) * rr, math.sin(a) * rr, 0)))
                groups.append(pb.name)
        for i in range(seg):
            j = (i + 1) % seg
            faces.append((start + i, start + j, start + seg + j, start + seg + i))
        faces.append(tuple(start + i for i in reversed(range(seg))))
        faces.append(tuple(start + seg + i for i in range(seg)))
    me = bpy.data.meshes.new("BodyProxy")
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    body = bpy.data.objects.new("BodyProxy", me)
    bpy.context.collection.objects.link(body)
    for i, g in enumerate(groups):
        vg = body.vertex_groups.get(g) or body.vertex_groups.new(name=g)
        vg.add([i], 1.0, 'REPLACE')
    body.parent = rig
    mod = body.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    mat = bpy.data.materials.new("BodyProxyMat")
    mat.diffuse_color = (0.55, 0.45, 0.38, 1)
    me.materials.append(mat)
    log("body proxy: %d verts, %d faces, %d bone groups" % (len(verts), len(faces), len(body.vertex_groups)))
    return body


def append_whip():
    with bpy.data.libraries.load(SOURCE_BLEND, link=False) as (src, dst):
        dst.objects = [n for n in src.objects if n in ("Whip", "WhipRig")]
    objs = {o.name: o for o in dst.objects if o is not None}
    for o in objs.values():
        bpy.context.collection.objects.link(o)
    whip, wrig = objs.get("Whip"), objs.get("WhipRig")
    if whip is None or wrig is None:
        raise RuntimeError("Whip / WhipRig not found in " + SOURCE_BLEND)
    fall = [b for b in wrig.data.bones if b.name.startswith("fall_")]
    first, last = wrig.matrix_world @ fall[0].head_local, wrig.matrix_world @ fall[-1].tail_local
    log("whip appended: mesh parent %s, rig at %s, %d bones (%d fall_), lash from %s to %s (%.3f m), mesh bbox z %.3f..%.3f" % (
        whip.parent.name if whip.parent else "none", fmt(wrig.matrix_world.to_translation()), len(wrig.data.bones), len(fall),
        fmt(first), fmt(last), (last - first).length,
        min((whip.matrix_world @ Vector(c)).z for c in whip.bound_box), max((whip.matrix_world @ Vector(c)).z for c in whip.bound_box)))
    return whip, wrig


def place_whip(data, world, rig, whip, wrig):
    att = data["rightHandAttach"]
    model = data["clubModel"]
    al = data["whipAlign"]
    q = Quaternion((1, 0, 0, 0))
    e = [math.radians(a) for a in al["eulerDeg"]]
    # Unity Quaternion.Euler(x, y, z) = rotate z, then x, then y (ZXY order, applied as Y @ X @ Z)
    qu = (Matrix.Rotation(e[1], 4, 'Y') @ Matrix.Rotation(e[0], 4, 'X') @ Matrix.Rotation(e[2], 4, 'Z'))
    s = al["scale"]
    pos = qu.to_3x3() @ Vector((0, -al["grip"] * s, 0))
    visual = Matrix.Translation(pos) @ qu @ Matrix.Diagonal((s, s, s, 1))
    u = world["RightHand"] @ trs(att["pos"], att["rot"], att["scale"]) @ trs(model["pos"], model["rot"], [1, 1, 1]) @ visual
    t = to_blender(u)
    roots = [o for o in (wrig, whip) if o.parent is None]
    for o in roots:
        o.matrix_world = t @ o.matrix_world
    bpy.context.view_layer.update()
    # parent the whip root(s) to the hand bone, keeping the world transform
    for o in roots:
        mw = o.matrix_world.copy()
        o.parent = rig
        o.parent_type = 'BONE'
        o.parent_bone = "RightHand"
        bpy.context.view_layer.update()
        o.matrix_world = mw
    fall = [b for b in wrig.data.bones if b.name.startswith("fall_")]
    tip = wrig.matrix_world @ fall[-1].tail_local
    log("whip in RightHand: visual Unity pos %s, Blender grip root %s, lash tip %s, scale %.2f (roots moved: %s)" % (
        fmt(u.to_translation()), fmt(wrig.matrix_world.to_translation()), fmt(tip), u.to_scale().x, [o.name for o in roots]))


def main():
    data = json.load(open(os.path.join(ROOT, "data", "player_skeleton.json")))
    bpy.ops.wm.read_homefile(use_empty=True)
    bones, world = unity_worlds(data)
    rig, heads = build_armature(bones, world)
    build_proxy(rig)
    whip, wrig = append_whip()
    place_whip(data, world, rig, whip, wrig)
    scn = bpy.context.scene
    scn.render.fps = 30
    scn.frame_start, scn.frame_end = 0, 34
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    log("saved " + OUT_BLEND)
    return {"log": LOG}


RESULT = main()
