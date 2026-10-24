"""T-014 icon render: run inside Blender with WhipItUp.blend open (do NOT save the file afterwards: it changes ranks, poses and
placements). Renders the paddle (V2), coiled whip (V4) and flogger (V6) fanned, transparent, to renders/_icon_weapons.png;
then run make_icon.py (outside Blender) for WhipItUp-Mod/icon.png."""
import math
import os

import bpy

TEX = os.path.join(os.path.dirname(bpy.data.filepath), "textures")


def set_rank(obj, fam, v):
    m = bpy.data.objects[obj].data.materials[0]
    for nd in m.node_tree.nodes:
        if nd.type == 'TEX_IMAGE' and nd.image and 'Albedo' in nd.image.name:
            nd.image = bpy.data.images.load(os.path.join(TEX, "%s_Albedo_V%d.png" % (fam, v)), check_existing=True)


def main():
    set_rank('Paddle', 'paddle', 2)
    set_rank('Flogger', 'flogger', 6)
    set_rank('Whip', 'whip', 4)
    rig = bpy.data.objects['WhipRig']
    fall = [pb for pb in rig.pose.bones if pb.name.startswith('fall_')]
    n = len(fall)
    for i, pb in enumerate(fall):                      # same coil as the mod's icon pose (WhipItems.CoilForIcon)
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, math.radians(52 * 16 / max(1, n) * (1 - 0.35 * i / max(1, n - 1))))
    p = bpy.data.objects['Paddle']
    # user 2026-10-24: the weapons side by side (not from one point), the whip barely overlapping the others
    p.scale = (1.3, 1.3, 1.3); p.rotation_euler = (0, math.radians(-14), 0); p.location = (-0.36, 0.08, 0.16)
    f = bpy.data.objects['FloggerRig']
    f.rotation_euler = (0, math.radians(14), 0); f.location = (0.30, 0.08, -0.02)
    rig.location = (0.04, -0.12, 0.10)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in (p, bpy.data.objects['Flogger'], bpy.data.objects['Whip']):
        oe = o.evaluated_get(dg); me = oe.to_mesh(); pts += [oe.matrix_world @ v.co for v in me.vertices]; oe.to_mesh_clear()
    xs = [q.x for q in pts]; zs = [q.z for q in pts]
    sc = bpy.context.scene
    cam = bpy.data.objects['PreviewCam']
    cam.data.type = 'ORTHO'
    cam.location = ((min(xs) + max(xs)) / 2, -3, (min(zs) + max(zs)) / 2)
    cam.rotation_euler = (math.radians(90), 0, 0)
    cam.data.ortho_scale = max(max(xs) - min(xs), max(zs) - min(zs)) * 1.04
    sc.camera = cam
    key, fill = bpy.data.objects['PreviewKey'], bpy.data.objects['PreviewFill']
    key.rotation_euler = (math.radians(55), 0, math.radians(-25)); key.data.energy = 4.5
    fill.rotation_euler = (math.radians(70), 0, math.radians(150)); fill.data.energy = 1.5
    for o in bpy.data.objects:
        if o.type == 'LIGHT' and o.name not in ('PreviewKey', 'PreviewFill'):
            o.hide_render = True
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = 1024
    sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGBA'
    sc.render.filepath = os.path.join(os.path.dirname(bpy.data.filepath), "renders", "_icon_weapons.png")
    bpy.ops.render.render(write_still=True)


main()
