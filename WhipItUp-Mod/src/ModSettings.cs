using BepInEx.Configuration;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp
{
    /// <summary>
    /// All settings in one place (user rule, every mod):
    ///  * PLAYER CONFIG (BepInEx entries below, bound once from Plugin.Awake): only what a player should change. Clear descriptions, ranges,
    ///    drop-downs (enums). Descriptions never mention older versions, old names or old values.
    ///  * DEV-ONLY values (Tunable fields below): fixed values chosen by the user, NOT in the player's file. The user's KydlTools mod (F7) finds
    ///    every static Tunable here by itself and lets the user change it in game - nothing to register.
    /// When the user settles a value in game, it becomes the new fixed value / default here.
    /// </summary>
    internal static class ModSettings
    {
        // [General]
        public static ConfigEntry<bool> EnableAll;
        public static ConfigEntry<KeyCode> MenuKey;
        // [DebugLog]
        public static ConfigEntry<bool> DebugLog;

        /// Is the mod switched on ([General] EnableAll)? Off = inert: no effects, no menus; nothing built is removed.
        public static bool On { get { return EnableAll == null || EnableAll.Value; } }

        // --- dev-only fixed values (Tunables): not in the config file, KydlTools changes them for testing -----------------------------------
        public static readonly Tunable<float> ExampleSpeed = new Tunable<float>("Example", "ExampleSpeed", 1f,
            "Example dev-only value: replace or remove. Read it with ModSettings.ExampleSpeed.Value.");

        // Weapon sounds (whoosh = swing, slap = hit). Live: a change applies to the next sound played.
        public static readonly Tunable<float> WhooshVolume = new Tunable<float>("Sound", "WhooshVolume", 1f, "Swing (whoosh) volume 0..1.");
        public static readonly Tunable<float> SlapVolume = new Tunable<float>("Sound", "SlapVolume", 2.2f, "Hit (slap) volume (user setting 2.2; the game plays anything above 1 at full volume - SlapGain makes it louder still).");
        public static readonly Tunable<float> WhooshGain = new Tunable<float>("Sound", "WhooshGain", 1.5f, "Loudness multiplier on the whoosh data (above ~0.7 peaks are soft-limited).");
        public static readonly Tunable<float> SlapGain = new Tunable<float>("Sound", "SlapGain", 1f, "Loudness multiplier on the slap data (1 = the file as it is; more squashes the peaks).");
        public static readonly Tunable<float> FloggerSlapVolume = new Tunable<float>("Sound", "FloggerSlapVolume", 1f, "Flogger hit volume (the file is played as it is).");
        public static readonly Tunable<bool> FloggerSlapSync = new Tunable<bool>("Sound", "FloggerSlapSync", true, "Start the flogger hit sound at its impact (skips the lead-in at playback, the file is not changed).");
        public static readonly Tunable<float> WhipSlapVolume = new Tunable<float>("Sound", "WhipSlapVolume", 1f, "Whip hit volume (the file is played as it is).");
        public static readonly Tunable<bool> WhipSlapSync = new Tunable<bool>("Sound", "WhipSlapSync", true, "Start the whip hit sound at its impact (skips the lead-in at playback, the file is not changed).");
        public static readonly Tunable<bool> SlapTrim = new Tunable<bool>("Sound", "SlapTrim", true, "Cut the quiet 50 ms lead-in of the slap so it lands on the hit (also normalises it).");

        // Paddle in the hand (degrees around the model's own axes; grip in metres along the handle, + = hand toward the blade; scale 1 = as modelled, 0.56 m)
        public static readonly Tunable<float> PaddleRotX = new Tunable<float>("Paddle", "RotationX", 270f, "Paddle model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotY = new Tunable<float>("Paddle", "RotationY", 180f, "Paddle model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotZ = new Tunable<float>("Paddle", "RotationZ", 0f, "Paddle model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> PaddleGrip = new Tunable<float>("Paddle", "GripOffset", 0.15f, "Metres the hand sits from the middle of the grip (+ = toward the blade).");
        public static readonly Tunable<float> PaddleScale = new Tunable<float>("Paddle", "Scale", 1f, "Paddle model scale (1 = 0.56 m as modelled).");
        // Paddle attack 2 (mace_secondary + wrist roll, T-005). Times = normalised clip time 0..1. Live.
        public static readonly Tunable<float> BackhandAngle = new Tunable<float>("Paddle", "BackhandAngle", 90f, "Wrist roll in degrees during attack 2 (0 = no roll, negative = the other way).");
        public static readonly Tunable<float> BackhandAxisX = new Tunable<float>("Paddle", "BackhandAxisX", 0f, "Roll axis X in the hand bone's own space.");
        public static readonly Tunable<float> BackhandAxisY = new Tunable<float>("Paddle", "BackhandAxisY", 1f, "Roll axis Y in the hand bone's own space (Y = along the forearm on Valheim's rig).");
        public static readonly Tunable<float> BackhandAxisZ = new Tunable<float>("Paddle", "BackhandAxisZ", 0f, "Roll axis Z in the hand bone's own space.");
        public static readonly Tunable<float> BackhandInStart = new Tunable<float>("Paddle", "BackhandInStart", 0.30f, "Clip time where the roll starts.");
        public static readonly Tunable<float> BackhandInFull = new Tunable<float>("Paddle", "BackhandInFull", 0.45f, "Clip time where the roll is complete (just before the swing).");
        public static readonly Tunable<float> BackhandOutStart = new Tunable<float>("Paddle", "BackhandOutStart", 0.68f, "Clip time where the roll starts to unwind (after the hit).");
        public static readonly Tunable<float> BackhandOutEnd = new Tunable<float>("Paddle", "BackhandOutEnd", 0.85f, "Clip time where the hand is back to normal.");
        public static readonly Tunable<float> BackhandStaminaFactor = new Tunable<float>("Paddle", "BackhandStaminaFactor", 1.75f, "Attack 2 stamina = rank attack-1 stamina x this (the Flametal mace: 28 / 16). Game restart.");
        public static Vector3 BackhandAxis { get { var v = new Vector3(BackhandAxisX.Value, BackhandAxisY.Value, BackhandAxisZ.Value); return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.up; } }

        public static Vector3 PaddleRotation { get { return new Vector3(PaddleRotX.Value, PaddleRotY.Value, PaddleRotZ.Value); } }
        /// Raised when any paddle alignment Tunable changes (KydlTools): the visuals are re-aligned live.
        public static event System.Action PaddleAlignmentChanged;

        // Flogger in the hand (same meaning as the paddle values; falls point along the model's +Y)
        public static readonly Tunable<float> FloggerRotX = new Tunable<float>("Flogger", "RotationX", 270f, "Flogger model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> FloggerRotY = new Tunable<float>("Flogger", "RotationY", 180f, "Flogger model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> FloggerRotZ = new Tunable<float>("Flogger", "RotationZ", 0f, "Flogger model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> FloggerGrip = new Tunable<float>("Flogger", "GripOffset", 0.15f, "Metres the hand sits from the middle of the grip (+ = toward the falls).");
        public static readonly Tunable<float> FloggerScale = new Tunable<float>("Flogger", "Scale", 1f, "Flogger model scale (1 = 0.76 m as modelled).");
        public static Vector3 FloggerRotation { get { return new Vector3(FloggerRotX.Value, FloggerRotY.Value, FloggerRotZ.Value); } }
        public static event System.Action FloggerAlignmentChanged;

        // Whip in the hand + lash behaviour (WhipMotion). Live unless marked "game restart".
        public static readonly Tunable<float> WhipRotX = new Tunable<float>("Whip", "RotationX", 270f, "Whip model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> WhipRotY = new Tunable<float>("Whip", "RotationY", 180f, "Whip model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> WhipRotZ = new Tunable<float>("Whip", "RotationZ", 0f, "Whip model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> WhipGrip = new Tunable<float>("Whip", "GripOffset", 0.11f, "Metres the hand sits from the middle of the grip (+ = toward the lash).");
        public static readonly Tunable<float> WhipScale = new Tunable<float>("Whip", "Scale", 1.5f, "Whip model scale (user default 1.5 = 2.7 m lash; 1 = 1.8 m as modelled).");
        public static Vector3 WhipRotation { get { return new Vector3(WhipRotX.Value, WhipRotY.Value, WhipRotZ.Value); } }
        public static event System.Action WhipAlignmentChanged;
        public static readonly Tunable<float> WhipReach = new Tunable<float>("Whip", "Reach", 4.8f, "Attack 1 range in metres (club: 2.4). Game restart.");
        public static readonly Tunable<float> WhipGrabRange = new Tunable<float>("Whip", "GrabRange", 5f, "Attack 2 range in metres: an enemy within it gets the lash around the neck and is pulled in. Game restart.");
        public static readonly Tunable<float> WhipGrabAngle = new Tunable<float>("Whip", "GrabAngle", 25f, "Attack 2 cone in degrees. Game restart.");
        public static readonly Tunable<float> WhipPullForce = new Tunable<float>("Whip", "PullForce", 80f, "How hard attack 2 pulls the enemy toward you (push force; heavy enemies move less).");
        public static readonly Tunable<float> WhipGrabSeconds = new Tunable<float>("Whip", "GrabSeconds", 0.9f, "How long the lash stays around the neck (visual).");
        public static readonly Tunable<float> WhipGrabPull = new Tunable<float>("Whip", "GrabPull", 25f, "How fast the lash snaps to the neck (1/s).");
        public static readonly Tunable<float> WhipGrabRadius = new Tunable<float>("Whip", "GrabRadius", 0.14f, "Radius of the wrap around the neck (m).");
        public static readonly Tunable<float> WhipLashStiffness = new Tunable<float>("Whip", "LashStiffness", 6f, "While attacking: pull toward the straight lash (1/s; higher = snappier).");
        public static readonly Tunable<float> WhipLashGravity = new Tunable<float>("Whip", "LashGravity", 2f, "While attacking: gravity on the lash (x Falls Gravity).");
        public static readonly Tunable<float> WhipCoilPull = new Tunable<float>("Whip", "CoilPull", 30f, "While idle: pull into the coil (1/s).");
        public static readonly Tunable<float> WhipCoilReturnSeconds = new Tunable<float>("Whip", "CoilReturnSeconds", 0.3f, "Time after an attack until the lash is fully coiled again.");
        public static readonly Tunable<float> WhipCoilRadius = new Tunable<float>("Whip", "CoilRadius", 0.12f, "Coil loop radius (m).");
        public static readonly Tunable<float> WhipCoilDrop = new Tunable<float>("Whip", "CoilDrop", -2f, "Coil centre height relative to the collar along the handle (m; negative = toward the pommel).");
        public static readonly Tunable<float> WhipCollarY = new Tunable<float>("Whip", "CollarY", 0.45f, "Start of the idle lash curve (m, model space; user value - the model's collar is at 0.15).");
        public static readonly Tunable<float> WhipLashLength = new Tunable<float>("Whip", "LashLength", 3.8f, "Length used for the idle curve (m, model space; user value - the modelled lash is 1.8).");
        /// The modelled lash (whip_layout.py TOTAL_LASH / BONES): fixed facts of the model, used for the neck wrap.
        public const float WhipModelLashLength = 1.8f;
        public const int WhipModelBones = 16;
        public static readonly Tunable<int> WhipBones = new Tunable<int>("Whip", "Bones", 36, "Point count of the idle curve (user value; the model has 16 lash bones).");

        // Fall physics (FallChains: springs + gravity on the fall bones). Live.
        public static readonly Tunable<float> FallGravity = new Tunable<float>("Falls", "Gravity", 9.8f, "Downward pull on the falls (m/s2; the world's is 9.81).");
        public static readonly Tunable<float> FallStiffness = new Tunable<float>("Falls", "Stiffness", 0.03f, "Pull back toward the straight rest shape per second (0 = fully limp; above ~1 the falls stop hanging).");
        public static readonly Tunable<float> FallDamping = new Tunable<float>("Falls", "Damping", 0.08f, "Velocity lost per frame (0..1; higher = calmer falls).");
        public static readonly Tunable<float> FallInertia = new Tunable<float>("Falls", "Inertia", 0.6f, "How much of the hand's motion the falls lag behind (0 = rigid, 1 = full).");

        static ModSettings()
        {
            foreach (var t in new[] { PaddleRotX, PaddleRotY, PaddleRotZ, PaddleGrip, PaddleScale })
                t.SettingChanged += (s, e) => { ModLog.Verbose("Config: paddle alignment " + ((Tunable<float>)s)); PaddleAlignmentChanged?.Invoke(); };
            foreach (var t in new[] { WhipRotX, WhipRotY, WhipRotZ, WhipGrip, WhipScale })
                t.SettingChanged += (s, e) => { ModLog.Verbose("Config: whip alignment " + ((Tunable<float>)s)); WhipAlignmentChanged?.Invoke(); };
            foreach (var t in new[] { FloggerRotX, FloggerRotY, FloggerRotZ, FloggerGrip, FloggerScale })
                t.SettingChanged += (s, e) => { ModLog.Verbose("Config: flogger alignment " + ((Tunable<float>)s)); FloggerAlignmentChanged?.Invoke(); };
        }

        /// Expected outcome: every player entry is bound (returns that check).
        public static bool Bind(ConfigFile c)
        {
            var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

            EnableAll = c.Bind("General", "EnableAll", true,
                "Switches the whole mod on / off while playing. Off: no effects and no menus from this mod; things already built stay in the world.");
            MenuKey = c.Bind("General", "MenuKey", KeyCode.F6,
                "Opens / closes the WhipItUp menu. None = no menu.");
            DebugLog = c.Bind("DebugLog", "Enable", false,
                "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

            ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
            ConfigTools.DropRemoved(c);

            EnableAll.SettingChanged += (s, e) => ModLog.Info("Config: EnableAll = " + EnableAll.Value + (EnableAll.Value ? ", the mod is on" : ", the mod is inert (no effects or menus)"));
            ModLog.Verbose("Config: bound (EnableAll " + EnableAll.Value + ", menu key " + MenuKey.Value + ", debug log " + DebugLog.Value + ")");
            return EnableAll != null && MenuKey != null && DebugLog != null;
        }
    }
}
