# Changelog

## 2.0.0
- Breaking: WhipItUp now needs the KydlCore mod (hard dependency). Install KydlCore as well.
- The menu is now tabs (Viewer, Details, Settings) in the KY hub window of KydlCore (F6); the own menu window and the `[General] MenuKey` setting are gone.
- New config switches: `[General] EnableAll`, `[Paddle] Enable`, `[Flogger] Enable`, `[Whip] Enable`, `[Viewer] Enable`. A switched-off weapon family is not registered at all (restart the game after changing it); the viewer tabs are hidden when the viewer is off.
- `[DebugLog] Enable` writes detailed lines to the BepInEx log.

## 1.0.1
- Whips placed on an item stand or an armor stand now hang coiled instead of as a loose lash.
- On item stands the coiled whip lies flat on the stand, centred, and clear of the ground stand's block.

## 1.0.0
- First release.
- Paddle, Flogger and Whip in seven ranks each (21 weapons), crafted at the workbench, forge and black forge; the rank metal
  costs 6 to craft and a flat 2 for every upgrade.
- New Whips skill (flogger and whip).
- Whip: own attack animations (side flick, backhand, overhand finisher), keyed lash, coiled in the hand while idle, crack sound
  at full extension, one hit sound per swing, attack 2 wraps the enemy's body and pulls it in (x2 damage), shorter hit pause.
- F6 menu: 3D weapon viewer and full weapon details.

## 0.1.0
- Development version (2026-10-21 to 2026-10-24), not released.
