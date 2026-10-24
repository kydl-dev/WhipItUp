"""T-013: renders frames of a baked clip (actions Body_<clip> on ValheimPlayer, Lash_<clip> on WhipRig when present) with the
IK / rotation constraints muted, i.e. exactly what the FBX exports. Workbench, 2 views per frame into previews/anim/<clip>/.
Then run anim_sheet.py (system python) to join them. Globals: CLIP, FRAMES (list), SIZE."""
import math
import os

import bpy
from mathutils import Vector

CLIP = globals().get("CLIP", "W1_SideFlick")
FRAMES = globals().get("FRAMES", [0, 8, 12, 14, 18, 26])
SIZE = globals().get("SIZE", (420, 420))
VIEWS = {"q": ((-3.6, -4.2, 2.3), (-0.2, -0.6, 1.1)), "t": ((-0.3, -0.9, 7.0), (-0.3, -0.9, 1.0))}


def main():
    scn = bpy.context.scene
    rig, whip = bpy.data.objects["ValheimPlayer"], bpy.data.objects["WhipRig"]
    body, lash = bpy.data.actions.get("Body_" + CLIP), bpy.data.actions.get("Lash_" + CLIP)
    if body is None:
        raise RuntimeError("no action Body_" + CLIP)
    muted = [c for pb in rig.pose.bones for c in pb.constraints if not c.mute]
    for c in muted:
        c.mute = True
    rig.animation_data.action = body
    if whip.animation_data is None:
        whip.animation_data_create()
    old_lash = whip.animation_data.action
    whip.animation_data.action = lash
    scn.render.engine = 'BLENDER_WORKBENCH'
    scn.display.shading.light = 'STUDIO'
    scn.display.shading.color_type = 'MATERIAL'
    scn.render.resolution_x, scn.render.resolution_y = SIZE
    scn.render.film_transparent = False
    cam = bpy.data.objects.get("AnimCam") or bpy.data.objects.new("AnimCam", bpy.data.cameras.new("AnimCam"))
    if cam.name not in scn.collection.objects:
        scn.collection.objects.link(cam)
    cam.data.lens = 35
    scn.camera = cam
    out = os.path.join(os.path.dirname(bpy.data.filepath), "previews", "anim", CLIP)
    os.makedirs(out, exist_ok=True)
    files = []
    for f in FRAMES:
        scn.frame_set(f)
        for v, (pos, tgt) in VIEWS.items():
            cam.location = Vector(pos)
            cam.rotation_euler = (Vector(tgt) - cam.location).to_track_quat('-Z', 'Y').to_euler()
            scn.render.filepath = os.path.join(out, "%s_f%02d.png" % (v, f))
            bpy.ops.render.render(write_still=True)
            files.append(scn.render.filepath)
    for c in muted:
        c.mute = False
    whip.animation_data.action = old_lash
    print("anim_render: %s %d image(s) in %s (lash action %s)" % (CLIP, len(files), out, lash.name if lash else "none"))
    return {"dir": out, "files": len(files)}


RESULT = main()
