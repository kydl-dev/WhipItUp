"""T-013: exports the whip animation FBX files from WhipAnim.blend into exports/ (same axis contract as export_fbx.py: metres,
forward -Z, up Y, bake_space_transform, primary bone axis Y, no leaf bones).

Global PART:
- 'body' -> WhipAnim_Body.fbx: rig ValheimPlayer + BodyProxy, every action whose name starts with 'Body_' as one take (Unity: Humanoid).
- 'lash' -> WhipAnim_Lash.fbx: rig WhipRig + Whip, every action whose name starts with 'Lash_' as one take (Unity: Generic; the bone
  paths equal Whip.fbx, so the clips sample onto the in-game whip visual).
Without actions (S1) the file holds the rest pose only. Returns RESULT with path, size and the exported take names.
"""
import os

import bpy

PART = globals().get("PART", "body")
SETS = {"body": ("ValheimPlayer", "BodyProxy", "Body_", "WhipAnim_Body.fbx"),
        "lash": ("WhipRig", "Whip", "Lash_", "WhipAnim_Lash.fbx")}


def main():
    rig_name, mesh_name, prefix, file_name = SETS[PART]
    rig, mesh = bpy.data.objects[rig_name], bpy.data.objects[mesh_name]
    out_dir = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "exports"))
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, file_name)
    takes = sorted(a.name for a in bpy.data.actions if a.name.startswith(prefix))
    # only this part's actions may be baked: hide the others with a fake user kept, restore after
    others = [a for a in bpy.data.actions if not a.name.startswith(prefix)]
    saved_parent = (mesh.parent, mesh.parent_type, mesh.parent_bone) if PART == "lash" else None
    old_action = rig.animation_data.action if rig.animation_data else None
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    win = bpy.context.window_manager.windows[0]
    with bpy.context.temp_override(window=win, active_object=rig, object=rig, selected_objects=[rig, mesh]):
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'ARMATURE'}, apply_unit_scale=True,
                                 apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                                 use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, primary_bone_axis='Y',
                                 secondary_bone_axis='X', use_armature_deform_only=False, bake_anim=bool(takes),
                                 bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=bool(takes),
                                 bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, path_mode='STRIP', embed_textures=False)
    if rig.animation_data:
        rig.animation_data.action = old_action
    print("anim_export: %s -> %s (%d bytes), takes %s, other actions present %d" % (PART, path, os.path.getsize(path), takes, len(others)))
    return {"path": path, "bytes": os.path.getsize(path), "takes": takes}


RESULT = main()
