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
        // Weapon sounds (whoosh = swing, slap = hit). Live: a change applies to the next sound played.
        public static readonly Tunable<float> WhooshVolume = new Tunable<float>("Sound", "WhooshVolume", 1f, 0f, 3f, "Swing (whoosh) volume 0..1.");
        public static readonly Tunable<float> SlapVolume = new Tunable<float>("Sound", "SlapVolume", 2.2f, 0f, 3f, "Hit (slap) volume (user setting 2.2; the game plays anything above 1 at full volume - SlapGain makes it louder still).");
        public static readonly Tunable<float> WhooshGain = new Tunable<float>("Sound", "WhooshGain", 1.5f, 0.5f, 3f, "Loudness multiplier on the whoosh data (above ~0.7 peaks are soft-limited).");
        public static readonly Tunable<float> SlapGain = new Tunable<float>("Sound", "SlapGain", 1f, 0.5f, 3f, "Loudness multiplier on the slap data (1 = the file as it is; more squashes the peaks).");
        public static readonly Tunable<float> FloggerSlapVolume = new Tunable<float>("Sound", "FloggerSlapVolume", 1f, 0f, 3f, "Flogger hit volume (the file is played as it is).");
        public static readonly Tunable<bool> FloggerSlapSync = new Tunable<bool>("Sound", "FloggerSlapSync", true, "Start the flogger hit sound at its impact (skips the lead-in at playback, the file is not changed).");
        public static readonly Tunable<float> WhipSlapVolume = new Tunable<float>("Sound", "WhipSlapVolume", 1f, 0f, 3f, "Whip hit volume (the file is played as it is).");
        public static readonly Tunable<bool> WhipSlapSync = new Tunable<bool>("Sound", "WhipSlapSync", true, "Start the whip hit sound at its impact (skips the lead-in at playback, the file is not changed).");
        public static readonly Tunable<bool> SlapTrim = new Tunable<bool>("Sound", "SlapTrim", true, "Cut the quiet 50 ms lead-in of the slap so it lands on the hit (also normalises it).");

        // Paddle in the hand (degrees around the model's own axes; grip in metres along the handle, + = hand toward the blade; scale 1 = as modelled, 0.65 m)
        public static readonly Tunable<float> PaddleRotX = new Tunable<float>("Paddle", "RotationX", 270f, -360f, 360f, "Paddle model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotY = new Tunable<float>("Paddle", "RotationY", 180f, -360f, 360f, "Paddle model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> PaddleRotZ = new Tunable<float>("Paddle", "RotationZ", 0f, -360f, 360f, "Paddle model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> PaddleGrip = new Tunable<float>("Paddle", "GripOffset", 0.15f, -0.3f, 0.3f, "Metres the hand sits from the middle of the grip (+ = toward the blade).");
        public static readonly Tunable<float> PaddleScale = new Tunable<float>("Paddle", "Scale", 1f, 0.5f, 2f, "Paddle model scale (1 = 0.65 m as modelled).");
        // Paddle attack 2 (mace_secondary + wrist roll, T-005). Times = normalised clip time 0..1. Live.
        public static readonly Tunable<float> BackhandAngle = new Tunable<float>("Paddle", "BackhandAngle", 90f, -360f, 360f, "Wrist roll in degrees during attack 2 (0 = no roll, negative = the other way).");
        public static readonly Tunable<float> BackhandAxisX = new Tunable<float>("Paddle", "BackhandAxisX", 0f, -1f, 1f, "Roll axis X in the hand bone's own space.");
        public static readonly Tunable<float> BackhandAxisY = new Tunable<float>("Paddle", "BackhandAxisY", 1f, -1f, 1f, "Roll axis Y in the hand bone's own space (Y = along the forearm on Valheim's rig).");
        public static readonly Tunable<float> BackhandAxisZ = new Tunable<float>("Paddle", "BackhandAxisZ", 0f, -1f, 1f, "Roll axis Z in the hand bone's own space.");
        public static readonly Tunable<float> BackhandInStart = new Tunable<float>("Paddle", "BackhandInStart", 0.30f, 0f, 1f, "Clip time where the roll starts.");
        public static readonly Tunable<float> BackhandInFull = new Tunable<float>("Paddle", "BackhandInFull", 0.45f, 0f, 1f, "Clip time where the roll is complete (just before the swing).");
        public static readonly Tunable<float> BackhandOutStart = new Tunable<float>("Paddle", "BackhandOutStart", 0.68f, 0f, 1f, "Clip time where the roll starts to unwind (after the hit).");
        public static readonly Tunable<float> BackhandOutEnd = new Tunable<float>("Paddle", "BackhandOutEnd", 0.85f, 0f, 1f, "Clip time where the hand is back to normal.");
        public static readonly Tunable<float> BackhandStaminaFactor = new Tunable<float>("Paddle", "BackhandStaminaFactor", 1.75f, 0.5f, 3f, "Attack 2 stamina = rank attack-1 stamina x this (the Flametal mace: 28 / 16). Game restart.");
        public static Vector3 BackhandAxis { get { var v = new Vector3(BackhandAxisX.Value, BackhandAxisY.Value, BackhandAxisZ.Value); return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.up; } }

        public static Vector3 PaddleRotation { get { return new Vector3(PaddleRotX.Value, PaddleRotY.Value, PaddleRotZ.Value); } }
        /// Raised when any paddle alignment Tunable changes (KydlTools): the visuals are re-aligned live.
        public static event System.Action PaddleAlignmentChanged;

        // Flogger in the hand (same meaning as the paddle values; falls point along the model's +Y)
        public static readonly Tunable<float> FloggerRotX = new Tunable<float>("Flogger", "RotationX", 270f, -360f, 360f, "Flogger model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> FloggerRotY = new Tunable<float>("Flogger", "RotationY", 180f, -360f, 360f, "Flogger model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> FloggerRotZ = new Tunable<float>("Flogger", "RotationZ", 0f, -360f, 360f, "Flogger model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> FloggerGrip = new Tunable<float>("Flogger", "GripOffset", 0.15f, -0.3f, 0.3f, "Metres the hand sits from the middle of the grip (+ = toward the falls).");
        public static readonly Tunable<float> FloggerScale = new Tunable<float>("Flogger", "Scale", 1f, 0.5f, 2f, "Flogger model scale (1 = 0.76 m as modelled).");
        public static Vector3 FloggerRotation { get { return new Vector3(FloggerRotX.Value, FloggerRotY.Value, FloggerRotZ.Value); } }
        public static event System.Action FloggerAlignmentChanged;

        // Whip in the hand + lash behaviour (WhipMotion). Live unless marked "game restart".
        public static readonly Tunable<float> WhipRotX = new Tunable<float>("Whip", "RotationX", 270f, -360f, 360f, "Whip model rotation X (degrees) in the hand.");
        public static readonly Tunable<float> WhipRotY = new Tunable<float>("Whip", "RotationY", 180f, -360f, 360f, "Whip model rotation Y (degrees) in the hand.");
        public static readonly Tunable<float> WhipRotZ = new Tunable<float>("Whip", "RotationZ", 0f, -360f, 360f, "Whip model rotation Z (degrees) in the hand.");
        public static readonly Tunable<float> WhipGrip = new Tunable<float>("Whip", "GripOffset", 0.11f, -0.3f, 0.3f, "Metres the hand sits from the middle of the grip (+ = toward the lash).");
        public static readonly Tunable<float> WhipScale = new Tunable<float>("Whip", "Scale", 1.5f, 0.5f, 2.5f, "Whip model scale (user default 1.5 = 2.7 m lash; 1 = 1.8 m as modelled).");
        public static Vector3 WhipRotation { get { return new Vector3(WhipRotX.Value, WhipRotY.Value, WhipRotZ.Value); } }
        public static event System.Action WhipAlignmentChanged;
        public static readonly Tunable<float> WhipReach = new Tunable<float>("Whip", "Reach", 4.8f, 1f, 8f, "Attack 1 range in metres (club: 2.4). Game restart.");
        public static readonly Tunable<float> WhipGrabRange = new Tunable<float>("Whip", "GrabRange", 5f, 1f, 10f, "Attack 2 range in metres: an enemy within it gets the lash around the neck and is pulled in. Game restart.");
        public static readonly Tunable<float> WhipGrabAngle = new Tunable<float>("Whip", "GrabAngle", 25f, 5f, 90f, "Attack 2 cone in degrees. Game restart.");
        public static readonly Tunable<float> WhipPullForce = new Tunable<float>("Whip", "PullForce", 80f, 0f, 300f, "How hard attack 2 pulls the enemy toward you (push force; heavy enemies move less).");
        public static readonly Tunable<float> WhipGrabSeconds = new Tunable<float>("Whip", "GrabSeconds", 0.9f, 0.1f, 3f, "How long the lash stays around the neck (visual).");
        public static readonly Tunable<float> WhipGrabPull = new Tunable<float>("Whip", "GrabPull", 25f, 1f, 60f, "How fast the lash snaps to the neck (1/s).");
        public static readonly Tunable<float> WhipGrabRadius = new Tunable<float>("Whip", "GrabRadius", 0.14f, 0.05f, 0.5f, "Radius of the wrap around the neck (m).");
        public static readonly Tunable<float> WhipLashStiffness = new Tunable<float>("Whip", "LashStiffness", 6f, 0f, 20f, "While attacking: pull toward the straight lash (1/s; higher = snappier).");
        public static readonly Tunable<float> WhipLashGravity = new Tunable<float>("Whip", "LashGravity", 2f, 0f, 5f, "While attacking: gravity on the lash (x Falls Gravity).");
        public static readonly Tunable<float> WhipCoilPull = new Tunable<float>("Whip", "CoilPull", 40f, 1f, 80f, "While idle: pull into the coil (1/s; 40+ = it snaps into the coil).");
        public static readonly Tunable<float> WhipCoilReturnSeconds = new Tunable<float>("Whip", "CoilReturnSeconds", 0.3f, 0.05f, 2f, "Time after an attack until the lash is fully coiled again.");
        public static readonly Tunable<float> WhipCoilRadius = new Tunable<float>("Whip", "CoilRadius", 0.18f, 0.08f, 0.5f, "Coil loop radius in metres (world size).");
        public static readonly Tunable<float> WhipCoilDrop = new Tunable<float>("Whip", "CoilDrop", 0f, -0.5f, 0.5f, "Slides the coil along the handle in metres (negative = back toward the hand; the first bones stay at the collar).");

        // Fall physics (FallChains: springs + gravity on the fall bones). Live.
        public static readonly Tunable<float> FallGravity = new Tunable<float>("Falls", "Gravity", 9.8f, 0f, 20f, "Downward pull on the falls (m/s2; the world's is 9.81).");
        public static readonly Tunable<float> FallStiffness = new Tunable<float>("Falls", "Stiffness", 0.03f, 0f, 2f, "Pull back toward the straight rest shape per second (0 = fully limp; above ~1 the falls stop hanging).");
        public static readonly Tunable<float> FallDamping = new Tunable<float>("Falls", "Damping", 0.08f, 0f, 0.5f, "Velocity lost per frame (0..1; higher = calmer falls).");
        public static readonly Tunable<float> FallInertia = new Tunable<float>("Falls", "Inertia", 0.6f, 0f, 1f, "How much of the hand's motion the falls lag behind (0 = rigid, 1 = full).");

        // Viewer tab of the F6 menu (T-015 S5): ranges = ViewerSettings (shared with the Unity preview). Read when the menu opens.
        public static readonly Tunable<float> ViewerRotateSpeed = new Tunable<float>("Viewer", "RotateSpeed", Ui.ViewerSettings.RotateSpeedDefault, Ui.ViewerSettings.RotateSpeedMin, Ui.ViewerSettings.RotateSpeedMax, "Degrees the weapon turns per UI pixel you drag.");
        public static readonly Tunable<float> ViewerZoomStep = new Tunable<float>("Viewer", "ZoomStep", Ui.ViewerSettings.ZoomStepDefault, Ui.ViewerSettings.ZoomStepMin, Ui.ViewerSettings.ZoomStepMax, "Magnification factor per mouse-wheel notch or +/- press (1.2 = 20 % closer).");
        public static readonly Tunable<float> ViewerZoomMax = new Tunable<float>("Viewer", "ZoomMax", Ui.ViewerSettings.ZoomMaxDefault, Ui.ViewerSettings.ZoomMaxMin, Ui.ViewerSettings.ZoomMaxMax, "Most magnification (x times; 1 = the whole weapon fits the frame).");
        public static readonly Tunable<float> ViewerZoomMin = new Tunable<float>("Viewer", "ZoomMin", Ui.ViewerSettings.ZoomMinDefault, Ui.ViewerSettings.ZoomMinMin, Ui.ViewerSettings.ZoomMinMax, "Least magnification (x times; below 1 the weapon is smaller than the frame).");
        public static readonly Tunable<float> ViewerPitchLimit = new Tunable<float>("Viewer", "PitchLimit", Ui.ViewerSettings.PitchLimitDefault, Ui.ViewerSettings.PitchLimitMin, Ui.ViewerSettings.PitchLimitMax, "Degrees the view may tilt up or down.");
        public static readonly Tunable<float> ViewerFrameHeight = new Tunable<float>("Viewer", "FrameHeight", Ui.ViewerSettings.FrameHeightDefault, Ui.ViewerSettings.FrameHeightMin, Ui.ViewerSettings.FrameHeightMax, "Height of the 3D view in UI pixels (1080p reference; reopen the menu to apply).");
        public static readonly Tunable<float> ViewerMargin = new Tunable<float>("Viewer", "Margin", Ui.ViewerSettings.MarginDefault, Ui.ViewerSettings.MarginMin, Ui.ViewerSettings.MarginMax, "Room around the weapon at zoom 1 (0.08 = 8 %).");
        public static readonly Tunable<float> ViewerStartYaw = new Tunable<float>("Viewer", "StartYaw", Ui.ViewerSettings.StartYawDefault, Ui.ViewerSettings.StartYawMin, Ui.ViewerSettings.StartYawMax, "Degrees the default view (and Reset) is turned.");
        public static readonly Tunable<float> ViewerStartPitch = new Tunable<float>("Viewer", "StartPitch", Ui.ViewerSettings.StartPitchDefault, Ui.ViewerSettings.StartPitchMin, Ui.ViewerSettings.StartPitchMax, "Degrees the default view (and Reset) looks down on the weapon.");
        public static readonly Tunable<float> ViewerPanSpeed = new Tunable<float>("Viewer", "PanSpeed", Ui.ViewerSettings.PanSpeedDefault, Ui.ViewerSettings.PanSpeedMin, Ui.ViewerSettings.PanSpeedMax, "How far the weapon moves per pixel you right-drag (x times; 1 = it follows the mouse exactly).");
        public static readonly Tunable<float> ViewerPanLimit = new Tunable<float>("Viewer", "PanLimit", Ui.ViewerSettings.PanLimitDefault, Ui.ViewerSettings.PanLimitMin, Ui.ViewerSettings.PanLimitMax, "Farthest the weapon may be moved from the middle, in view heights at zoom 1 (0.8 = about half a weapon length).");

        /// The viewer values as the menu uses them (read fresh at every menu open, so KydlTools changes apply on the next open).
        internal static Ui.ViewerSettings Viewer()
        {
            var v = new Ui.ViewerSettings
            {
                RotateSpeed = ViewerRotateSpeed.Value,
                ZoomStep = ViewerZoomStep.Value,
                ZoomMax = ViewerZoomMax.Value,
                ZoomMin = ViewerZoomMin.Value,
                PitchLimit = ViewerPitchLimit.Value,
                FrameHeight = ViewerFrameHeight.Value,
                Margin = ViewerMargin.Value,
                StartYaw = ViewerStartYaw.Value,
                StartPitch = ViewerStartPitch.Value,
                PanSpeed = ViewerPanSpeed.Value,
                PanLimit = ViewerPanLimit.Value,
            };
            v.Clamp(ModLog.Verbose);
            return v;
        }

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
