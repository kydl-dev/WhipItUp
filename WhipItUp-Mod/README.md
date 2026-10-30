#Note

Mod requested by MissBeaHive from twitch.tv

# WhipItUp

Three new weapon families for Valheim, the **Paddle**, the **Flogger** and the **Whip**, each in seven colour ranks from the
Meadows to the Mountains, plus a new **Whips** skill. The whip has its own animations, a lash that coils in your hand and snaps
out when you attack, and a second attack that wraps the lash around an enemy and pulls it toward you.

## Features
- **21 craftable weapons**: Paddle, Flogger and Whip in seven ranks (V1 to V7), each rank with its own colours and name.
- **Paddle** (Clubs skill): blunt damage. Attack 2 is a heavy back-handed swing with a wrist roll (x2.5 damage).
- **Flogger** (Whips skill): slash damage plus bleeding (5 s). Attack 2 is a heavy overhead strike (x2.5 damage).
- **Whip** (Whips skill): slash damage plus bleeding, and almost twice the reach of a club (4.8 m).
  - Attack 1 is a three-hit combo with its own animations: side flick, backhand and an overhand finisher (x2 damage).
  - Attack 2 wraps the lash around the enemy's body and pulls it toward you (x2 damage, 5 m).
  - The lash coils in your hand while idle, cracks at full extension, and hit sounds play once per swing.
- **Whips skill**: a new skill that levels with the flogger and the whip.
- **Weapon viewer (KY hub, F6)**: look at every weapon in 3D (rotate, pan, zoom; the whip coiled or straight) and read its stats,
  attacks and recipe for every quality level.

## Weapons
All weapons: max quality 4, +6 damage per level, durability 200 to 500 (+40 per level).

| Rank | Station | Paddle (blunt) | Flogger / Whip (slash + bleed per s) | Materials to craft (upgrade to quality 2 / 3 / 4) |
|------|---------|----------------|--------------------------------------|--------------------------|
| V1 | Workbench 1 | 30 | 20 + 10 | 10 Leather scraps (5/10/20), 2 Resin (1/2/4), 5 Wood (3/6/12), 6 Tin (2/2/2) |
| V2 | Workbench 2 | 40.5 | 28 + 12.5 | 10 Deer hide (5/10/20), 2 Bronze (1/2/4), 5 Fine wood (3/6/12), 6 Copper (2/2/2) |
| V3 | Forge 1 | 54.8 | 39.2 + 15.6 | 10 Bear hide (5/10/20), 2 Surtling core (1/2/4), 5 Core wood (3/6/12), 6 Bronze (2/2/2) |
| V4 | Forge 2 | 74.4 | 54.9 + 19.5 | 10 Wolf pelt (5/10/20), 2 Obsidian (1/2/4), 5 Fine wood (3/6/12), 6 Iron (2/2/2) |
| V5 | Forge 3 | 101.3 | 76.8 + 24.4 | 10 Lox pelt (5/10/20), 2 Tar (1/2/4), 5 Ancient bark (3/6/12), 6 Black metal (2/2/2) |
| V6 | Black forge 1 | 138.1 | 107.6 + 30.5 | 10 Asksvin hide (5/10/20), 2 Charred bone (1/2/4), 5 Blackwood (3/6/12), 6 Flametal (2/2/2) |
| V7 | Black forge 2 | 188.7 | 150.6 + 38.2 | 10 Moose hide (5/10/20), 2 Seal hide (1/2/4), 5 Frostwood (3/6/12), 6 Gold (2/2/2) |

The F6 menu shows the exact numbers of every weapon at every quality level.

## How to use
- Craft the weapons at the workbench, forge or black forge (see the table); they show up once you know the materials.
- Press **F6** to open the KY hub (KydlCore); the WhipItUp tabs hold the weapon viewer, the details and the settings.

## Configuration
`BepInEx/config/com.kydldev.WhipItUp.cfg` (or the in-game configuration manager):

| Section | Setting | Default | What it does |
|---------|---------|---------|--------------|
| General | EnableAll | true | Switches the whole mod on / off while playing |
| Paddle | Enable | true | Paddle family (restart the game after changing) |
| Flogger | Enable | true | Flogger family and the Whips skill (restart the game after changing) |
| Whip | Enable | true | Whip family and the Whips skill (restart the game after changing) |
| Viewer | Enable | true | Weapon viewer and details tabs in the KY hub |
| DebugLog | Enable | false | Detailed lines in the BepInEx log for bug reports |

Edits to the file are picked up while the game runs.

## Multiplayer
Every player needs the mod (same version).

## Bug reports
Turn on `[DebugLog] Enable`, reproduce the problem, and attach `BepInEx/LogOutput.log`.
