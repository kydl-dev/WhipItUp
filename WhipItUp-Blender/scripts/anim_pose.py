"""T-013 S3/S4: keys the whip attack clips on the ValheimPlayer rig in WhipAnim.blend.

Controls (made once): empty 'HandTarget' (IK target of RightForeArm, chain 2, and Copy Rotation source of RightHand) and empty
'ElbowPole'. A key pose (see CLIPS in anim_clips.py) gives: hand position (world, Blender: the character faces -Y, its right is -X,
up Z), the whip handle direction + roll, the elbow pole, spine / hips twist + bend, the left arm and the leg stance.
For each clip: keys every pose at its frame on the controls and the FK bones, then bakes the visual pose of every bone into the action
'Body_<clip>' (what the FBX exports). Logs every key (hand position, handle direction, achieved hand position error) and the bake.
Global CLIP = one clip name or None (all). Run inside Blender (Blender MCP); RESULT holds the log.
"""
import math

import bpy
from mathutils import Matrix, Quaternion, Vector

LOG = []
RIG = "ValheimPlayer"
FK_BONES = ("Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "RightUpLeg", "RightLeg", "RightFoot")


def log(s):
    LOG.append(s)
    print("anim_pose: " + s)


def fmt(v):
    return "(" + ", ".join("%.3f" % x for x in v) + ")"


def win_override(obj):
    win = bpy.context.window_manager.windows[0]
    return bpy.context.temp_override(window=win, active_object=obj, object=obj, selected_objects=[obj])


def empty(name, size=0.08, shape='SPHERE'):
    o = bpy.data.objects.get(name)
    if o is None:
        o = bpy.data.objects.new(name, None)
        o.empty_display_type = shape
        o.empty_display_size = size
        bpy.context.scene.collection.objects.link(o)
    o.rotation_mode = 'QUATERNION'
    return o


def reset_pose(rig):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = Quaternion()
        pb.location = Vector()
        pb.scale = Vector((1, 1, 1))
    bpy.context.view_layer.update()


def rest_frames(rig):
    """Hand rest rotation (world) and the whip handle direction / side axis at rest, with no constraints active."""
    muted = [c for pb in rig.pose.bones for c in pb.constraints if not c.mute]
    for c in muted:
        c.mute = True
    reset_pose(rig)
    whip = bpy.data.objects["WhipRig"]
    hand = rig.matrix_world @ rig.pose.bones["RightHand"].matrix
    wm = whip.matrix_world
    d0 = (wm.to_3x3() @ Vector((0, 0, 1))).normalized()
    s0 = (wm.to_3x3() @ Vector((1, 0, 0))).normalized()
    for c in muted:
        c.mute = False
    log("rest: RightHand at %s, whip handle dir %s, side %s" % (fmt(hand.to_translation()), fmt(d0), fmt(s0)))
    return hand.to_quaternion(), d0, s0


def frame_of(d, s):
    d = d.normalized()
    s = (s - d * s.dot(d)).normalized()
    u = d.cross(s)
    return Matrix((s, u, d)).transposed()


def side_for(d, roll_deg):
    s = d.cross(Vector((0, 0, 1)))
    if s.length < 1e-4:
        s = Vector((-1, 0, 0))
    s.normalize()
    return Quaternion(d.normalized(), math.radians(roll_deg)) @ s


def setup_controls(rig):
    tgt, pole = empty("HandTarget", 0.06, 'ARROWS'), empty("ElbowPole", 0.05)
    fore, hand = rig.pose.bones["RightForeArm"], rig.pose.bones["RightHand"]
    ik = fore.constraints.get("WhipIK") or fore.constraints.new('IK')
    ik.name, ik.target, ik.pole_target, ik.chain_count = "WhipIK", tgt, pole, 2
    cr = hand.constraints.get("WhipRot") or hand.constraints.new('COPY_ROTATION')
    cr.name, cr.target, cr.target_space, cr.owner_space = "WhipRot", tgt, 'WORLD', 'WORLD'
    return tgt, pole, ik


def calibrate_pole(rig, tgt, pole, ik):
    """Picks the IK pole angle that puts the elbow nearest the pole (the bones' roll is arbitrary)."""
    reset_pose(rig)
    tgt.location = Vector((-0.45, -0.35, 1.2))
    pole.location = Vector((-0.8, 0.4, 1.0))
    best = None
    for a in range(-180, 181, 5):
        ik.pole_angle = math.radians(a)
        bpy.context.view_layer.update()
        elbow = rig.matrix_world @ rig.pose.bones["RightForeArm"].head
        mid = (rig.matrix_world @ rig.pose.bones["RightArm"].head + tgt.location) * 0.5
        want = (pole.location - mid).normalized()
        score = (elbow - mid).normalized().dot(want)
        if best is None or score > best[0]:
            best = (score, a)
    ik.pole_angle = math.radians(best[1])
    log("IK pole angle %d deg (elbow toward pole: dot %.3f)" % (best[1], best[0]))


def rotate_world(rig, name, axis, deg):
    if abs(deg) < 1e-6:
        return
    pb = rig.pose.bones[name]
    head = pb.matrix.to_translation()
    r = Matrix.Rotation(math.radians(deg), 4, Vector(axis))
    pb.matrix = Matrix.Translation(head) @ r @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()


def apply_body(rig, p):
    twist, bend, lean = p.get("twist", 0.0), p.get("bend", 0.0), p.get("lean", 0.0)
    hips = rig.pose.bones["Hips"]
    hips.location = Vector((0, 0, 0))
    bpy.context.view_layer.update()
    # crouch (D-093: lean into the attack like the Frostner / vanilla sword combo, whose hips sink 0.1-0.4 m): the hips go down by
    # 'crouch' m and both legs fold so the feet stay at their height: thigh forward phi, knee 2 phi, foot back phi, 2 L (1 - cos phi) = crouch
    crouch = p.get("crouch", 0.0)
    drop = p.get("drop", 0.04) + crouch
    hips.matrix = Matrix.Translation((0, 0, -drop)) @ hips.matrix
    bpy.context.view_layer.update()
    rotate_world(rig, "Hips", (0, 0, 1), p.get("hips", twist * 0.35))
    for n, f in (("Spine", 0.3), ("Spine1", 0.35), ("Spine2", 0.35)):
        rotate_world(rig, n, (0, 0, 1), twist * 0.65 * f / 1.0)
        rotate_world(rig, n, (1, 0, 0), bend * f)           # + bend = lean forward (toward -Y): +X rotation tips +Z toward -Y (the
                                                             # first drafts had -bend: every 'forward' lean leaned BACK, user D-086)
        rotate_world(rig, n, (0, 1, 0), lean * f)           # + lean = toward the character's left (+X)
    rotate_world(rig, "Head", (0, 0, 1), -twist * 0.4)        # the head keeps looking ahead
    rotate_world(rig, "Head", (1, 0, 0), -bend * 0.25)        # and half level: looks ahead, not down (not arched back either: D-086)
    # left arm relaxed: down from the T-pose, a little forward, elbow bent
    la = p.get("larm", (68, 15, 25))
    rotate_world(rig, "LeftArm", (0, 1, 0), la[0])
    rotate_world(rig, "LeftArm", (0, 0, 1), -la[1])
    rotate_world(rig, "LeftForeArm", (0, 1, 0), la[2] * 0.3)
    rotate_world(rig, "LeftForeArm", (0, 0, 1), -la[2])
    # stance: left leg forward, right leg back, knees bent
    st = p.get("stance", 1.0)
    rotate_world(rig, "LeftUpLeg", (1, 0, 0), -16 * st)
    rotate_world(rig, "LeftLeg", (1, 0, 0), 22 * st)
    rotate_world(rig, "LeftFoot", (1, 0, 0), -6 * st)
    rotate_world(rig, "RightUpLeg", (1, 0, 0), 12 * st)
    rotate_world(rig, "RightLeg", (1, 0, 0), 14 * st)
    rotate_world(rig, "RightFoot", (1, 0, 0), -20 * st)
    if crouch > 0:
        leg = (rig.data.bones["LeftUpLeg"].length + rig.data.bones["LeftLeg"].length)
        phi = math.degrees(math.acos(max(-1.0, min(1.0, 1.0 - crouch / (2 * leg)))))
        for side in ("Left", "Right"):
            rotate_world(rig, side + "UpLeg", (1, 0, 0), -phi)
            rotate_world(rig, side + "Leg", (1, 0, 0), 2 * phi)
            rotate_world(rig, side + "Foot", (1, 0, 0), -phi)


def key_pose(rig, tgt, pole, rest, frame, p):
    h0, d0, s0 = rest
    # the hand target is written for an upright body: when the body leans / crouches (D-093) the shoulder moves, and the hand moves with
    # it (same offset), so the arm keeps its shape instead of being pulled back by a fixed target
    upright = dict(p, bend=0.0, crouch=0.0, lean=0.0)
    reset_pose(rig)
    apply_body(rig, upright)
    sh0 = rig.matrix_world @ rig.pose.bones["RightArm"].head
    reset_pose(rig)
    apply_body(rig, p)
    shift = (rig.matrix_world @ rig.pose.bones["RightArm"].head) - sh0
    d = Vector(p["dir"]).normalized()
    s = side_for(d, p.get("roll", 0.0))
    q = (frame_of(d, s) @ frame_of(d0, s0).inverted()).to_quaternion()
    tgt.location = Vector(p["hand"]) + shift
    tgt.rotation_quaternion = q @ h0
    pole.location = Vector(p.get("elbow", (p["hand"][0] - 0.35, p["hand"][1] + 0.45, p["hand"][2] - 0.35))) + shift
    bpy.context.view_layer.update()
    for o in (tgt, pole):
        o.keyframe_insert("location", frame=frame)
    tgt.keyframe_insert("rotation_quaternion", frame=frame)
    for n in FK_BONES:
        pb = rig.pose.bones[n]
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if n == "Hips":
            pb.keyframe_insert("location", frame=frame)
    got = rig.matrix_world @ rig.pose.bones["RightHand"].matrix
    whip = bpy.data.objects["WhipRig"].matrix_world
    wd = (whip.to_3x3() @ Vector((0, 0, 1))).normalized()
    want = Vector(p["hand"]) + shift
    log("key f%d: shoulder shift %s by bend %.0f / crouch %.2f; hand %s (got %s, error %.3f m), handle dir %s (got %s, %.1f deg off), roll %.0f, twist %.0f, bend %.0f" % (
        frame, fmt(shift), p.get("bend", 0.0), p.get("crouch", 0.0), fmt(want), fmt(got.to_translation()), (got.to_translation() - want).length, fmt(d), fmt(wd),
        math.degrees(wd.angle(d)) if wd.length > 0 else -1, p.get("roll", 0.0), p.get("twist", 0.0), p.get("bend", 0.0)))


def new_actions(rig, tgt, pole, name):
    for o in (rig, tgt, pole):
        if o.animation_data is None:
            o.animation_data_create()
        act = bpy.data.actions.get("Key_" + name + "_" + o.name)
        if act is not None:
            bpy.data.actions.remove(act)
        o.animation_data.action = bpy.data.actions.new("Key_" + name + "_" + o.name)


def bake(rig, name, frames):
    out = "Body_" + name
    old = bpy.data.actions.get(out)
    if old is not None:
        bpy.data.actions.remove(old)
    with win_override(rig):
        bpy.ops.object.mode_set(mode='POSE')
        bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=0, frame_end=frames - 1, only_selected=True, visual_keying=True, clear_constraints=False,
                         use_current_action=False, bake_types={'POSE'})
        bpy.ops.object.mode_set(mode='OBJECT')
    act = rig.animation_data.action
    act.name = out
    act.use_fake_user = True
    log("baked %s: frames 0..%d, %d fcurve(s)" % (out, frames - 1, len(getattr(act, "fcurves", [])) or -1))
    return act


def make_clip(name, spec):
    rig = bpy.data.objects[RIG]
    tgt, pole, ik = setup_controls(rig)
    rest = rest_frames(rig)
    calibrate_pole(rig, tgt, pole, ik)
    new_actions(rig, tgt, pole, name)
    scn = bpy.context.scene
    scn.frame_start, scn.frame_end = 0, spec["frames"] - 1
    for frame, p in spec["keys"]:
        scn.frame_set(frame)
        key_pose(rig, tgt, pole, rest, frame, p)
    act = bake(rig, name, spec["frames"])
    return act
