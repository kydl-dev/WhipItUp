"""T-013: key poses of the whip clips (PLAN T-013 S2 table). Blender world: the character faces -Y, its right is -X, up is Z;
the right shoulder is near (-0.24, 0.07, 1.65), arm reach about 0.6 m. Per key: hand = RightHand position (m), dir = whip handle
direction, roll = degrees about it (0 = palm down like the T-pose), twist = shoulders turn (deg, + = to the character's left),
bend = lean forward (deg), lean = to the left (deg), elbow = IK pole position (optional), drop = hips down (m).
"""

READY = {"hand": (-0.28, -0.28, 1.12), "dir": (-0.1, -0.7, -0.7), "roll": 0, "twist": 0, "bend": 8, "crouch": 0.08}
# D-093 (user: lean further forward, like the Frostner = the vanilla sword combo): bend / crouch per key follow the vanilla clips measured
# by the Unity menu "Valheim/Report whip clip lean" (vanilla torso lean 11-37 deg at the hit, hips 0.54-0.85 m high vs our 0.95)

CLIPS = {
    "W1_SideFlick": {"frames": 34, "speed": [(11, 1.2), (18, 1.0)], "keys": [
        # the lash needs ~6 frames to roll out after the hand sweep: the hand leads, the lash lands in front-left on the Hit frame (14)
        (0, READY),
        (3, {"hand": (-0.55, 0.30, 1.28), "dir": (-0.55, 0.6, -0.25), "roll": -20, "twist": -40, "bend": 0, "lean": -4, "crouch": 0.08}),
        (5, {"hand": (-0.50, -0.30, 1.30), "dir": (-0.65, -0.75, 0.0), "roll": 0, "twist": -12, "bend": 17, "crouch": 0.12}),
        (7, {"hand": (-0.12, -0.52, 1.30), "dir": (0.35, -0.94, 0.0), "roll": 10, "twist": 18, "bend": 42, "crouch": 0.18}),
        (11, {"hand": (0.22, -0.34, 1.24), "dir": (0.9, -0.25, -0.3), "roll": 15, "twist": 35, "bend": 49, "lean": 3, "crouch": 0.20}),
        (24, {"hand": (0.02, -0.36, 1.16), "dir": (0.1, -0.8, -0.6), "roll": 5, "twist": 15, "bend": 21, "crouch": 0.10}),
        (33, READY),
    ]},
    "W2_Backhand": {"frames": 25, "speed": [(6, 1.1), (13, 1.0)], "keys": [
        # Hit on frame 8: the backhand sweep crosses the front on frames 2-5, the lash lands front-right
        (0, READY),
        (1, {"hand": (0.30, -0.15, 1.45), "dir": (0.6, 0.5, 0.3), "roll": 180, "twist": 35, "bend": 14, "lean": 3, "crouch": 0.10}),
        (3, {"hand": (0.08, -0.48, 1.35), "dir": (0.5, -0.85, 0.1), "roll": 180, "twist": 15, "bend": 35, "crouch": 0.12}),
        (5, {"hand": (-0.35, -0.45, 1.30), "dir": (-0.6, -0.8, 0.0), "roll": 160, "twist": -15, "bend": 49, "crouch": 0.15}),
        (8, {"hand": (-0.52, -0.12, 1.25), "dir": (-0.9, 0.1, -0.2), "roll": 150, "twist": -35, "bend": 49, "lean": -3, "crouch": 0.15}),
        (16, {"hand": (-0.30, -0.32, 1.16), "dir": (-0.2, -0.8, -0.5), "roll": 60, "twist": -10, "bend": 28, "crouch": 0.10}),
        (24, READY),
    ]},
    "W3_Overhand": {"frames": 25, "speed": [(1, 0.5), (4, 1.5), (16, 1.0)], "keys": [
        # D-095 (user): hit 3 leans like hit 2 (bend 49 / crouch 0.15 at the hit), not deeper. OnAttackTrigger on frame 8: arm up and back (slow, speed 0.5), overhead forward-down snap, the lash cracks in front at chest height
        (0, READY),
        (2, {"hand": (-0.32, 0.18, 1.95), "dir": (-0.1, 0.6, 0.8), "roll": 0, "twist": -25, "bend": -10, "lean": -5, "crouch": 0.05}),
        (4, {"hand": (-0.25, -0.38, 1.72), "dir": (0.0, -0.9, 0.4), "roll": 0, "twist": -5, "bend": 35, "crouch": 0.12}),
        (5, {"hand": (-0.15, -0.55, 1.30), "dir": (0.0, -0.9, -0.4), "roll": 0, "twist": 5, "bend": 49, "crouch": 0.15}),
        (10, {"hand": (-0.12, -0.38, 1.00), "dir": (0.0, -0.5, -0.85), "roll": 0, "twist": 5, "bend": 49, "crouch": 0.15}),
        (24, READY),
    ]},
    "G_LashGrab": {"frames": 34, "speed": [(15, 1.5), (24, 1.0)],
                   # D-086: the 'wrap-around' (horizontal swing, YouTube oadsRsIZHUQ) but the lash wraps the ENEMY's body: hand a bit above
                   # the hip, drawn back to the right, swung level forward and across so the lash rolls out flat and winds round the
                   # body (centre 3 m ahead, waist / chest height), then a hard pull back (the body is pulled toward the player on 25-33)
                   "pin": {"from": 21, "center": (-0.10, -3.00, 1.05), "to": (-0.25, -1.80, 1.00), "move": (25, 33), "wrap": 24, "radius": 0.32},
                   "keys": [
        (0, {"hand": (-0.30, -0.25, 1.20), "dir": (-0.1, -0.8, -0.55), "roll": 0, "twist": 0, "bend": 11, "crouch": 0.08}),
        (6, {"hand": (-0.50, 0.15, 1.18), "dir": (-0.6, 0.75, -0.1), "roll": -10, "twist": -35, "bend": 7, "crouch": 0.08}),
        (11, {"hand": (-0.42, -0.42, 1.22), "dir": (-0.35, -0.94, 0.0), "roll": 0, "twist": -10, "bend": 21, "crouch": 0.10}),
        (14, {"hand": (-0.15, -0.60, 1.22), "dir": (0.25, -0.97, 0.0), "roll": 5, "twist": 10, "bend": 42, "crouch": 0.15}),
        (18, {"hand": (0.08, -0.52, 1.20), "dir": (0.55, -0.83, 0.0), "roll": 10, "twist": 22, "bend": 49, "crouch": 0.15}),
        (22, {"hand": (0.05, -0.50, 1.20), "dir": (0.45, -0.89, 0.0), "roll": 10, "twist": 18, "bend": 42, "crouch": 0.15}),
        (25, {"hand": (-0.15, -0.42, 1.18), "dir": (0.1, -0.99, 0.0), "roll": 5, "twist": 5, "bend": 28, "crouch": 0.12}),
        (30, {"hand": (-0.45, 0.05, 1.05), "dir": (-0.2, -0.95, 0.1), "roll": 0, "twist": -25, "bend": -5, "lean": -3, "crouch": 0.10}),
        (33, {"hand": (-0.32, -0.22, 1.15), "dir": (-0.15, -0.85, -0.3), "roll": 0, "twist": -12, "bend": 7, "crouch": 0.06}),
    ]},
}
