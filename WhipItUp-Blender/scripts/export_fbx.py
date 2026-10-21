"""Exports one object as FBX for Unity into WhipItUp-Blender/exports/<Name>.fbx (export contract: exports/README.md).

Run inside Blender. Global OBJECT (default 'Paddle'). Selected object (+ RIG armature) only, metres, forward -Z, up Y, apply transform,
mesh only, no materials embedded (Unity builds the material in the mod from the vanilla item's material).
"""
import os

import bpy

OBJECT = globals().get("OBJECT", "Paddle")
# skinned items: the armature is exported too (globals RIG = armature object name); bones keep their names (fall_KK_B)
RIG = globals().get("RIG")


def main():
    obj = bpy.data.objects[OBJECT]
    out_dir = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "exports"))
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, OBJECT + ".fbx")
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    rig = bpy.data.objects[RIG] if RIG else None
    if rig is not None:
        rig.select_set(True)
    bpy.context.view_layer.objects.active = obj
    # n-gons (blade / disc faces) block tangent export: triangulate them for the export only (temporary modifier)
    tri = obj.modifiers.new("ExportTriangulate", 'TRIANGULATE')
    tri.min_vertices = 5
    tri.quad_method = 'BEAUTY'
    tri.ngon_method = 'BEAUTY'
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'ARMATURE'} if rig is not None else {'MESH'}, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='FACE',
                             use_tspace=True, add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=True, bake_anim=False, path_mode='STRIP',
                             embed_textures=False)
    obj.modifiers.remove(tri)
    return {"path": path, "bytes": os.path.getsize(path)}


RESULT = main()
