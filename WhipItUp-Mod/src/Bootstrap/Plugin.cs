using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Utils;
using KydlCore.Common;
using UnityEngine;
using WhipItUp.Bootstrap;
using WhipItUp.Ui;

namespace WhipItUp;

/// <summary>
/// WhipItUp: Adds three craftable weapon families: Paddle, Flogger and Whip, seven colour ranks each, with a new Whips skill.
/// Entry point. Order in Awake matters: log first, then the config (player settings), then text, then everything that uses them.
/// </summary>
[BepInPlugin(ModGuid, ModName, ModVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(KydlCore.Bootstrap.Plugin.ModGuid, "0.1.0")]     // the UI (hub, windows, input lock): manifest KydlDev-KydlCore-0.1.0, same version
// Weapons, recipes and skill levels exist in the shared world: everybody needs the same version (MIGRATION_DECISIONS "Network compatibility").
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
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
    ModInstaller _installer;

    void Awake()
    {
        Instance = this;
        ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
        ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

        ModSettings.Bind(Config);
        _cfgWatcher = new ConfigReloader(Config);
        Loc.Register(Weapons.WeaponNames.MergeInto(UiText.En));   // English only (PROJECT_RULES 15)

        Bundle = LoadBundle();
        Sounds.WeaponSounds.Load(Bundle);
        // Harmony patches live in src/Integrations/Harmony; each one asks ModSettings whether the mod is on
        PatchInstaller.Apply(ModGuid, Assembly.GetExecutingAssembly());
        // the features (Whips skill, paddle, flogger, whip, viewer) and the mod's tabs in the KY hub of KydlCore (it owns the menu key, window and input lock).
        // A weapon family that is off ([Paddle] / [Flogger] / [Whip] Enable, or EnableAll) is not added to the game (needs a restart to change).
        _installer = new ModInstaller(Config);
        bool installed = _installer.Install();
        Config.SettingChanged += OnSettingChanged;
        ModLog.Verbose("Plugin: Awake finished (architecture " + Core.Architecture.Tag + ", installer " + (installed ? "ok" : "had failures") + "; paddle " + ModSettings.PaddleOn + ", flogger " + ModSettings.FloggerOn + ", whip " + ModSettings.WhipOn + ", viewer " + ModSettings.ViewerOn + ")");
    }

    void OnSettingChanged(object sender, SettingChangedEventArgs e)
    {
        ModLog.Verbose("Plugin: config [" + e.ChangedSetting.Definition.Section + "] " + e.ChangedSetting.Definition.Key + " changed - refreshing the features");
        if (_installer != null) _installer.Refresh();
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
        Config.SettingChanged -= OnSettingChanged;
        if (_installer != null) { _installer.Uninstall(); _installer = null; }
        if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
        ModLog.Verbose("Plugin: destroyed");
    }
}
