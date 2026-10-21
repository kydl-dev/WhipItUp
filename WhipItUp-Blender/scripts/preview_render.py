"""Renders preview images of one object (default 'Paddle') from several angles into WhipItUp-Blender/renders/.

Run inside Blender. Creates/reuses a camera 'PreviewCam' and lights 'PreviewKey'/'PreviewFill' (in collection 'Preview').
Globals you can set before exec: TARGET (object name), VIEWS (list of (name, azimuth_deg, elevation_deg)), RES (pixels),
CENTER + SIZE (close-up on a world point, frame size in m).
"""
import math
import os

import bpy
from mathutils import Vector

TARGET = globals().get("TARGET", "Paddle")
VIEWS = globals().get("VIEWS", [("front", 0, 5), ("side", 90, 5), ("threequarter", 35, 20), ("back", 180, 5)])
RES = globals().get("RES", 900)
OUT = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "renders"))


def ensure(name, maker):
    o = bpy.data.objects.get(name)
    if o is None:
        o = maker()
        o.name = name
    return o


def main():
    os.makedirs(OUT, exist_ok=True)
    obj = bpy.data.objects[TARGET]
    col = bpy.data.collections.get("Preview") or bpy.data.collections.new("Preview")
    if col.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(col)

    def mk_cam():
        c = bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam"))
        col.objects.link(c)
        return c

    def mk_light(n, e):
        def f():
            l = bpy.data.objects.new(n, bpy.data.lights.new(n, 'SUN'))
            l.data.energy = e
            col.objects.link(l)
            return l
        return f

    cam = ensure("PreviewCam", mk_cam)
    key = ensure("PreviewKey", mk_light("PreviewKey", 4.0))
    fill = ensure("PreviewFill", mk_light("PreviewFill", 1.5))
    key.rotation_euler = (math.radians(50), 0, math.radians(30))
    fill.rotation_euler = (math.radians(70), 0, math.radians(200))
    sc = bpy.context.scene
    sc.camera = cam
    sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    sc.render.resolution_x = RES
    sc.render.resolution_y = RES
    sc.render.film_transparent = False
    if sc.world is None:
        sc.world = bpy.data.worlds.new("World")
    bg = sc.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.35, 0.37, 0.40, 1)
        bg.inputs[1].default_value = 0.8
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    center = sum(bb, Vector()) / 8
    size = max((max(v[i] for v in bb) - min(v[i] for v in bb)) for i in range(3))
    if globals().get("CENTER") is not None:      # optional close-up: world point + frame size (m)
        center, size = Vector(globals()["CENTER"]), globals().get("SIZE", size)
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = size * 1.15
    hidden = [o for o in sc.objects if o.type == 'MESH' and o is not obj and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    files = []
    for name, az, el in VIEWS:
        a, e = math.radians(az), math.radians(el)
        d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
        cam.location = center + d * (size * 3)
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        path = os.path.join(OUT, f"{TARGET}_{name}.png")
        sc.render.filepath = path
        bpy.ops.render.render(write_still=True)
        files.append(path)
    for o in hidden:
        o.hide_render = False
    return {"files": files, "hidden_meshes": [o.name for o in hidden]}


RESULT = main()
