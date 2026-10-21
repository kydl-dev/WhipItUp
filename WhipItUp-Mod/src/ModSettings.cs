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

        // Fall physics (FallChains: springs + gravity on the fall bones). Live.
        public static readonly Tunable<float> FallGravity = new Tunable<float>("Falls", "Gravity", 6f, "Downward pull on the falls (m/s2; the world's is 9.81).");
        public static readonly Tunable<float> FallStiffness = new Tunable<float>("Falls", "Stiffness", 1.5f, "How fast the falls return to their rest shape (1/s; 0 = limp).");
        public static readonly Tunable<float> FallDamping = new Tunable<float>("Falls", "Damping", 0.12f, "Velocity lost per frame (0..1; higher = calmer falls).");
        public static readonly Tunable<float> FallInertia = new Tunable<float>("Falls", "Inertia", 0.8f, "How much of the hand's motion the falls lag behind (0 = rigid, 1 = full).");

        static ModSettings()
        {
            foreach (var t in new[] { PaddleRotX, PaddleRotY, PaddleRotZ, PaddleGrip, PaddleScale })
                t.SettingChanged += (s, e) => { ModLog.Verbose("Config: paddle alignment " + ((Tunable<float>)s)); PaddleAlignmentChanged?.Invoke(); };
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
