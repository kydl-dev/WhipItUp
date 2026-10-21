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

        // Weapon sounds (whoosh = swing, slap = hit). Read once when the items are made (game start).
        public static readonly Tunable<float> WhooshVolume = new Tunable<float>("Sound", "WhooshVolume", 1f, "Swing (whoosh) volume 0..1. Game restart.");
        public static readonly Tunable<float> SlapVolume = new Tunable<float>("Sound", "SlapVolume", 1f, "Hit (slap) volume 0..1. Game restart.");
        public static readonly Tunable<float> SoundGain = new Tunable<float>("Sound", "Gain", 1.5f, "Loudness multiplier on the sound data (soft-limited). Game restart.");
        public static readonly Tunable<bool> SlapTrim = new Tunable<bool>("Sound", "SlapTrim", true, "Cut the quiet lead-in of the slap and normalise it. Game restart.");

        // Paddle in the hand (degrees around the model's own axes; grip in metres along the handle, + = hand toward the blade; scale 1 = as modelled, 0.56 m)
        public static readonly Tunable<float> PaddleRotX = new Tunable<float>("Paddle", "RotationX", 270f, "Paddle model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotY = new Tunable<float>("Paddle", "RotationY", 180f, "Paddle model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotZ = new Tunable<float>("Paddle", "RotationZ", 0f, "Paddle model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> PaddleGrip = new Tunable<float>("Paddle", "GripOffset", 0f, "Metres the hand sits from the middle of the grip (+ = toward the blade).");
        public static readonly Tunable<float> PaddleScale = new Tunable<float>("Paddle", "Scale", 1f, "Paddle model scale (1 = 0.56 m as modelled).");
        public static Vector3 PaddleRotation { get { return new Vector3(PaddleRotX.Value, PaddleRotY.Value, PaddleRotZ.Value); } }
        /// Raised when any paddle alignment Tunable changes (KydlTools): the visuals are re-aligned live.
        public static event System.Action PaddleAlignmentChanged;

        static ModSettings()
        {
            foreach (var t in new[] { PaddleRotX, PaddleRotY, PaddleRotZ, PaddleGrip, PaddleScale })
                t.SettingChanged += (s, e) => { ModLog.Verbose("Config: paddle alignment " + ((Tunable<float>)s)); PaddleAlignmentChanged?.Invoke(); };
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
