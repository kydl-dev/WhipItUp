# Export contract (Blender -> Unity)

Script: `scripts/export_fbx.py` (exec in Blender with OBJECT = '<name>').
- FBX, selected object only, mesh only, no embedded textures/materials.
- Units: metres (1 BU = 1 m), "FBX Units Scale", Apply Transform (bake_space_transform) on.
- Axes: forward -Z, up Y. In Blender the item is built Z-up, origin = middle of the grip.
- Names: Blender object `Paddle` -> `Paddle.fbx` -> Unity mesh `Paddle`. Textures from `textures/` (PascalCase prefix + `_Albedo_V#`, `_Normal`, `_MetalGloss`).
- Unity copies live in `WhipItUp-Unity/Assets/WhipItUp/<Item>/` (GUIDs in their .meta: overwrite in place, never delete/re-add).
