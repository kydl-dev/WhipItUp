"""T-013: simulates the whip lash from the keyed hand motion of a body clip and bakes it into the action 'Lash_<clip>' on WhipRig
(D-084: the lash is keyed in Blender; this is the keying tool, the result is plain keys that can be hand-tuned).

Model: verlet chain through the fall_ bone heads + the last tail (real bone lengths x whip scale), the first point fixed to the
handle end, gravity, air damping, distance constraints, a bending stiffness that falls off toward the tip (the thong taper),
ground at z = 0.02. Optional PIN = (from_frame, (x, y, z)): the tip is held at that point (attack 2: around the neck).
Per frame: bone rotations solved in armature space from the points (no twist added), keyed as quaternions.
Logs: settle result, per-frame tip speed (m/s) and the frame of the fastest tip (the crack), the max stretch error.
Globals: CLIP, SUB (substeps per frame), STIFF (bend stiffness at the handle), DAMP, PIN, PREROLL (frames to settle at frame 0).
"""
import math

import bpy
from mathutils import Matrix, Quaternion, Vector

CLIP = globals().get("CLIP", "W1_SideFlick")
SUB = globals().get("SUB", 12)
STIFF = globals().get("STIFF", 0.15)
TIP_STIFF = globals().get("TIP_STIFF", 0.02)
DRAG = globals().get("DRAG", 0.005)
DAMP = globals().get("DAMP", 0.0015)
GRAVITY = globals().get("GRAVITY", 9.8)
ITER = globals().get("ITER", 12)
PIN = globals().get("PIN", None)          # dict from anim_clips: from, center, to, move (f0, f1), wrap (points), radius
SPEED = globals().get("SPEED", [])         # [(frame, animator speed)] from the clip's Speed events: the lash runs in game time
PREROLL = globals().get("PREROLL", 45)
FPS = 30.0
LOG = []


def log(s):
    LOG.append(s)
    print("anim_lash: " + s)


def fmt(v):
    return "(" + ", ".join("%.3f" % x for x in v) + ")"


def chain(whip):
    bones = [b for b in whip.data.bones if b.name.startswith("fall_")]
    bones.sort(key=lambda b: (int(b.name.split("_")[1]), int(b.name.split("_")[2])))
    return bones


def handle(whip, root_bone):
    """World position of the lash start (root bone tail) and the handle direction, from the evaluated scene."""
    m = whip.matrix_world @ root_bone.matrix_local
    start = whip.matrix_world @ root_bone.tail_local
    d = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    return start, d


def step(p, prev, start, hdir, lengths, dt, pin):
    n = len(p)
    g = Vector((0, 0, -GRAVITY)) * dt * dt
    for i in range(1, n):
        v = (p[i] - prev[i]) * (1.0 - DAMP)
        prev[i] = p[i].copy()
        p[i] = p[i] + v + g
    # bending (once per substep): every point is pulled toward the straight continuation of the segment before it, stiff at the
    # handle and soft at the tip (the thong tapers); the first segment follows the handle direction
    p[0] = start.copy()
    p[1] = p[1].lerp(p[0] + hdir * lengths[0], STIFF)
    for i in range(1, n - 1):
        a = i / (n - 2)
        k = STIFF * (1 - a) ** 1.5 + TIP_STIFF
        straight = p[i] + (p[i] - p[i - 1]).normalized() * lengths[i]
        p[i + 1] = p[i + 1].lerp(straight, k)
    # air drag on the fall-off part (the last 30 %): stops the tip from curling into pigtails
    for i in range(int(n * 0.7), n):
        v = p[i] - prev[i]
        prev[i] = prev[i] + v * DRAG
    for _ in range(ITER):
        p[0] = start.copy()
        for i in range(n - 1):
            d = p[i + 1] - p[i]
            l = d.length
            if l < 1e-9:
                continue
            corr = d * ((l - lengths[i]) / l)
            if i == 0:
                p[i + 1] -= corr
            else:
                p[i] += corr * 0.5
                p[i + 1] -= corr * 0.5
        if pin is not None:
            w = pin.get("weight", 1.0)
            for i, q in pin.items():
                if i != "weight":
                    p[i] = p[i].lerp(q, w)
        for i in range(1, n):
            if p[i].z < 0.02:
                p[i].z = 0.02
    p[0] = start.copy()


def solve_rotations(whip, bones, pts):
    """Armature-space pose matrices: bone i from pts[i] toward pts[i+1]; returns local basis quaternions."""
    inv = whip.matrix_world.inverted()
    out = []
    parent_pose = {}
    for i, b in enumerate(bones):
        head = inv @ pts[i]
        tail = inv @ pts[i + 1]
        want = (tail - head).normalized()
        rest = b.matrix_local.to_3x3()
        rest_y = (rest @ Vector((0, 1, 0))).normalized()
        q = rest_y.rotation_difference(want)
        pose = Matrix.Translation(head) @ (q.to_matrix() @ rest).to_4x4()
        if b.parent is not None and b.parent.name in parent_pose:
            pp = parent_pose[b.parent.name]
            rel = b.parent.matrix_local.inverted() @ b.matrix_local
            basis = (pp @ rel).inverted() @ pose
        elif b.parent is not None:
            rel = b.parent.matrix_local.inverted() @ b.matrix_local
            basis = (b.parent.matrix_local @ rel).inverted() @ pose
        else:
            basis = b.matrix_local.inverted() @ pose
        parent_pose[b.name] = pose
        out.append(basis.to_quaternion().normalized())
    return out


def main():
    scn = bpy.context.scene
    rig, whip = bpy.data.objects["ValheimPlayer"], bpy.data.objects["WhipRig"]
    body = bpy.data.actions.get("Body_" + CLIP)
    if body is None:
        raise RuntimeError("no Body_" + CLIP)
    muted = [c for pb in rig.pose.bones for c in pb.constraints if not c.mute]
    for c in muted:
        c.mute = True
    rig.animation_data.action = body
    if whip.animation_data is None:
        whip.animation_data_create()
    whip.animation_data.action = None
    for pb in whip.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = Quaternion()
    frames = int(round(body.frame_range[1])) + 1
    bones = chain(whip)
    root_bone = whip.data.bones["root"]
    scale = whip.matrix_world.to_scale().x
    lengths = [b.length * scale for b in bones]
    scn.frame_set(0)
    start, hdir = handle(whip, root_bone)
    # initial: straight along the handle, then settle (pre-roll) under gravity with the hand still
    pts = [start.copy()]
    for L in lengths:
        pts.append(pts[-1] + hdir * L)
    prev = [p.copy() for p in pts]
    dt = 1.0 / (FPS * SUB)
    for _ in range(PREROLL * SUB):
        step(pts, prev, start, hdir, lengths, dt, None)
    log("%s: %d bones, lash %.3f m (scale %.2f), settled %d frames: tip %s" % (CLIP, len(bones), sum(lengths), scale, PREROLL, fmt(pts[-1])))
    act = bpy.data.actions.get("Lash_" + CLIP)
    if act is not None:
        bpy.data.actions.remove(act)
    act = bpy.data.actions.new("Lash_" + CLIP)
    act.use_fake_user = True
    whip.animation_data.action = act
    def speed_at(f):
        v = 1.0
        for fr, sp in SPEED:
            if f >= fr:
                v = sp
        return v

    def pins_at(t, n):
        """Tip points held around the neck: the last `wrap` points on a turning circle, more of them as the wrap goes on."""
        if not PIN or t < PIN["from"]:
            return None
        c0, c1 = Vector(PIN["center"]), Vector(PIN["to"])
        m0, m1 = PIN["move"]
        a = min(1.0, max(0.0, (t - m0) / max(1e-6, m1 - m0)))
        c = c0.lerp(c1, a)
        k = min(PIN["wrap"], 1 + int((t - PIN["from"]) * 4))
        out = {"weight": min(1.0, 0.15 + (t - PIN["from"]) / 3.0)}   # the tip is drawn to the neck over 3 frames, not snapped
        for j in range(k):
            ang = j * lengths[-1] / PIN["radius"]        # one bone length of arc per point: the wound part keeps its length
            out[n - 1 - j] = c + Vector((math.cos(ang) * PIN["radius"], math.sin(ang) * PIN["radius"], -0.02 * j))
        return out
    last_q = [None] * len(bones)
    speeds = []
    curls = []
    prev_tip = pts[-1].copy()
    max_err = 0.0
    for f in range(frames):
        dt = 1.0 / (FPS * SUB * speed_at(f))       # one clip frame lasts 1 / (30 x speed) s in game
        for s in range(SUB):
            t = f + (s + 1) / SUB - 1 if f > 0 else 0
            if f > 0:
                scn.frame_set(int(math.floor(t)), subframe=t - math.floor(t))
                start, hdir = handle(whip, root_bone)
            step(pts, prev, start, hdir, lengths, dt, pins_at(f + (s + 1) / SUB, len(pts)))
        scn.frame_set(f)
        start, hdir = handle(whip, root_bone)
        speed = (pts[-1] - prev_tip).length * FPS
        prev_tip = pts[-1].copy()
        speeds.append(speed)
        err = max(abs((pts[i + 1] - pts[i]).length - lengths[i]) for i in range(len(lengths)))
        max_err = max(max_err, err)
        turn = sum(math.degrees((pts[i] - pts[i - 1]).angle(pts[i + 1] - pts[i], 0.0)) for i in range(1, len(pts) - 1))
        reach = (pts[-1] - pts[0]).length / sum(lengths)
        tip = pts[-1] - pts[0]
        az = math.degrees(math.atan2(tip.x, -tip.y))      # 0 = in front of the character (-Y), +90 = to its left (+X)
        curls.append((turn, reach, az, tip.z))
        qs = solve_rotations(whip, bones, pts)
        for i, (b, q) in enumerate(zip(bones, qs)):
            if last_q[i] is not None and last_q[i].dot(q) < 0:
                q.negate()
            last_q[i] = q
            pb = whip.pose.bones[b.name]
            pb.rotation_quaternion = q
            pb.keyframe_insert("rotation_quaternion", frame=f)
    peak = max(range(frames), key=lambda i: speeds[i])
    log("%s: baked Lash_%s, %d frames; tip speed m/s per frame: %s" % (CLIP, CLIP, frames, " ".join("%d:%.0f" % (i, v) for i, v in enumerate(speeds))))
    log("%s: lash shape per frame (total bend deg / reach of the length / tip azimuth deg, 0 = front, +90 = left / tip height over the handle m): %s" % (CLIP, " ".join("%d:%.0f/%.2f/az%.0f/dz%.1f" % (i, c[0], c[1], c[2], c[3]) for i, c in enumerate(curls))))
    log("%s: fastest tip at frame %d (%.1f m/s), max stretch error %.4f m, final tip %s" % (CLIP, peak, speeds[peak], max_err, fmt(pts[-1])))
    for c in muted:
        c.mute = False
    return {"log": LOG, "peak": peak, "maxturn": max(c[0] for c in curls)}


RESULT = main()
