# WhipItUp-Blender

- `WhipItUp.blend`: the working scene (object `Paddle`, material `paddle_mat`). Built by scripts, not by hand.
- `reference/`: the user's reference photos (shape only; colours come from RidingCropMod, D-005/D-014).
- `scripts/paddle_layout.py`: paddle sizes + UV atlas rects (shared). `scripts/build_paddle.py`: builds `Paddle` in the open .blend (Blender MCP: exec the file).
  `scripts/paddle_textures.py`: writes `textures/paddle_Albedo_V1..V7.png`, `paddle_Normal.png`, `paddle_MetalGloss.png` (system python, numpy + PIL).
  `scripts/preview_render.py`: preview renders into `renders/` (git-ignored); `scripts/sheet.py` joins them.
- Order after a layout change: paddle_textures.py, then build_paddle.py, then export.
- `exports/`: FBX for Unity (export contract in `exports/README.md` once the first export is done).
- `scripts/medallion_knot.py`: the medallion knot (distance-field render; `python scripts/medallion_knot.py renders/knot.png` previews it). Used by paddle_textures.py.
- `reference/TrophyWolf_icon_64.png`: the vanilla TrophyWolf icon, cropped from the rip's IconAtlas (x 2088, y 1800, 64x64, Unity y-up) for the pommel inlay.
