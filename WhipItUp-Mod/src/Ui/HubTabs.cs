using BepInEx.Configuration;
using Kydl.Common;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The mod's menu = tabs in the KY hub of KydlCore (F6). There is NO menu key, window or input lock in this mod: KydlCore owns them (D-040).
    /// Row 1 of the hub shows this mod (named by SetModTitle), row 2 shows the tabs registered here: Viewer and Details (the weapon list, shared
    /// choice of weapon) when [Viewer] Enable is on and at least one weapon family is on, and Settings (the player config, with the switches).
    /// What a tab shows is built by the views in the Unity project (WhipItUp-Unity/Assets/WhipItUp_UI/Runtime, previewed there); the game side only hands them data.
    /// A mod that registers any tab loses the automatic "Settings" tab, so the settings tab is registered on purpose.
    /// </summary>
    internal static class HubTabs
    {
        internal const string ViewerId = "viewer", DetailsId = "details", SettingsId = "settings";

        // the chosen weapon survives page switches (and hub reopenings): the list itself is read again every time a page is built
        static string _family;
        static int _rank;

        /// Called once from Plugin.Awake (after the config is bound and the text is registered). Expected outcome: the settings tab is registered, and the two weapon tabs when ModSettings.ViewerShown.
        internal static void Register(ConfigFile config)
        {
            KydlCore.KyHub.SetModTitle(Plugin.ModGuid, Plugin.ModName);
            bool shown = ModSettings.ViewerShown;
            var viewer = shown ? KydlCore.KyHub.AddPage(Plugin.ModGuid, ViewerId, Loc.T("whipitup_tab_viewer"), WeaponKyPages.Viewer, 0, 30) : null;
            var details = shown ? KydlCore.KyHub.AddPage(Plugin.ModGuid, DetailsId, Loc.T("whipitup_tab_details"), WeaponKyPages.Details, 1, 10) : null;
            var settings = KydlCore.KyHub.AddTab(Plugin.ModGuid, SettingsId, Loc.T("whipitup_tab_settings"), () => KydlCore.KyHub.ConfigEntries(config), 10);
            ModLog.Verbose("HubTabs: weapon tabs " + (shown ? "on" : "off (viewer " + ModSettings.ViewerOn + ", paddle " + ModSettings.PaddleOn + ", flogger " + ModSettings.FloggerOn + ", whip " + ModSettings.WhipOn + ")")
                + "; registered viewer " + (viewer != null) + ", details " + (details != null) + ", settings " + (settings != null));
        }

        /// A fresh weapon list (stats are read from the game now) with the weapon chosen last time selected again. Expected outcome: a selection that remembers its changes.
        internal static WeaponSelection NewSelection()
        {
            var weapons = WeaponCatalog.Build(k => Loc.T(k), s => ModLog.Verbose(s), WeaponStatsReader.Read, ModSettings.FamilyOn);
            var selection = new WeaponSelection(weapons, s => ModLog.Verbose(s));
            if (_family != null) selection.Set(_family, _rank);
            selection.Changed += () => { _family = selection.Family; _rank = selection.Rank; };
            return selection;
        }
    }
}
