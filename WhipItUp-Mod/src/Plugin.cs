using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Kydl.Common;
using UnityEngine;
using WhipItUp.Ui;

namespace WhipItUp
{
    /// <summary>
    /// WhipItUp: Adds three craftable weapon families: Paddle, Flogger and Whip, seven colour ranks each, with a new Whips skill.
    /// Entry point. Order in Awake matters: log first, then the config (player settings), then text, then everything that uses them.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(KydlCore.Plugin.ModGuid, "0.1.0")]     // the UI (hub, windows, input lock): manifest KydlDev-KydlCore-0.1.0, same version
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        // ModGuid never changes after the first release. ModVersion = manifest.json version_number = newest CHANGELOG heading (tests check it).
        public const string ModGuid = "com.kydldev.WhipItUp";
        public const string ModName = "WhipItUp";
        public const string ModVersion = "2.0.0";
        public const string BundleName = "whipitup";

        internal static Plugin Instance;
        /// This mod's AssetBundle (null while the mod has none yet: Unity has not built one).
        internal static AssetBundle Bundle;
        ConfigReloader _cfgWatcher;

        void Awake()
        {
            Instance = this;
            ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
            ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

            ModSettings.Bind(Config);
            _cfgWatcher = new ConfigReloader(Config);
            Loc.Register(Weapons.WeaponNames.MergeInto(UiText.En));   // English only (PROJECT_RULES 15)

            Bundle = LoadBundle();
            // a weapon family that is off ([Paddle] / [Flogger] / [Whip] Enable, or EnableAll) is not added to the game (needs a restart to change)
            if (ModSettings.SkillOn) Weapons.WhipSkill.Register();          // before the game creates the player's Skills
            else ModLog.Verbose("Plugin: flogger and whip are off, the Whips skill is not registered");
            Sounds.WeaponSounds.Load(Bundle);
            if (ModSettings.PaddleOn) new Weapons.PaddleItems().Register(Bundle, a => ModSettings.PaddleAlignmentChanged += a);
            else ModLog.Verbose("Plugin: paddle is off, its items are not registered");
            if (ModSettings.FloggerOn) new Weapons.FloggerItems().Register(Bundle, a => ModSettings.FloggerAlignmentChanged += a);
            else ModLog.Verbose("Plugin: flogger is off, its items are not registered");
            if (ModSettings.WhipOn)
            {
                new Weapons.WhipItems().Register(Bundle, a => ModSettings.WhipAlignmentChanged += a);
                Weapons.WhipAnimations.Load(Bundle);   // T-013 custom whip clips + keyed lash
            }
            else ModLog.Verbose("Plugin: whip is off, its items and animations are not registered");
            HubTabs.Register(Config);               // the weapon viewer / details / settings tabs in the KY hub of KydlCore (it owns the menu key, window and input lock)
            new Harmony(ModGuid).PatchAll(Assembly.GetExecutingAssembly());
            ModLog.Verbose("Plugin: Awake finished (Harmony patches applied; paddle " + ModSettings.PaddleOn + ", flogger " + ModSettings.FloggerOn + ", whip " + ModSettings.WhipOn + ", viewer " + ModSettings.ViewerOn + ")");
        }

        // 1) embedded in the DLL (Kydl.Mod.targets embeds Assets\<bundle>; read fully into memory: LoadFromStream breaks once the stream
        //    is disposed), 2) the file next to the DLL (the Gale test profile gets both). No bundle at all is normal for a new mod.
        static AssetBundle LoadBundle()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res != BundleName && !res.EndsWith("." + BundleName)) continue;
                using (var stream = asm.GetManifestResourceStream(res))
                using (var ms = new MemoryStream())
                {
                    if (stream != null) stream.CopyTo(ms);
                    var b = AssetBundle.LoadFromMemory(ms.ToArray());
                    if (b != null) { ModLog.Verbose("Plugin: bundle '" + BundleName + "' loaded from the DLL (" + b.GetAllAssetNames().Length + " asset(s))"); return b; }
                    ModLog.Warn("Plugin: the embedded bundle '" + res + "' could not be loaded");
                }
            }
            var path = Path.Combine(Path.GetDirectoryName(asm.Location) ?? "", BundleName);
            if (!File.Exists(path)) { ModLog.Verbose("Plugin: no bundle '" + BundleName + "' (not embedded, not at " + path + ") - fine until the mod has assets"); return null; }
            var fromFile = AssetBundle.LoadFromFile(path);
            if (fromFile == null) ModLog.Warn("Plugin: bundle file " + path + " could not be loaded");
            else ModLog.Verbose("Plugin: bundle loaded from " + path);
            return fromFile;
        }

        void Update()
        {
            if (_cfgWatcher != null) _cfgWatcher.Tick();
            Weapons.WhipAnimations.Tick();     // T-013: put the game's controller back one frame after a whip visual went away (also when off)
        }

        void OnDestroy()
        {
            if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
            ModLog.Verbose("Plugin: destroyed");
        }
    }
}
