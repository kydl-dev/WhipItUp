using BepInEx.Configuration;
using KydlCore.Common;

namespace WhipItUp.Ui;

/// <summary>
/// The mod's menu = tabs in the KY hub of KydlCore (F6). There is NO menu key, window or input lock in this mod: KydlCore owns them (D-040).
/// Row 1 of the hub shows this mod (named by SetModTitle), row 2 shows the tabs registered here: Viewer and Details (the weapon list, shared
/// choice of weapon), added by the Viewer feature's Start and removed by its Stop, and Settings (the player config, with the switches), always there.
/// What a tab shows is built by the views in the Unity project (WhipItUp-Unity/Assets/WhipItUp_UI/Runtime, previewed there); the game side only hands them data.
/// A mod that registers any tab loses the automatic "Settings" tab, so the settings tab is registered on purpose.
/// </summary>
internal static class HubTabs
{
    internal const string ViewerId = "viewer", DetailsId = "details", SettingsId = "settings";

    // the chosen weapon survives page switches (and hub reopenings): the list itself is read again every time a page is built
    static string _family;
    static int _rank;

    /// Once from ModInstaller.Install: the mod's title and its settings tab. Expected outcome: the settings tab is registered.
    internal static bool RegisterModPage(ConfigFile config)
    {
        KydlCore.Features.Hub.Contracts.KyHub.SetModTitle(Plugin.ModGuid, Plugin.ModName);
        var settings = KydlCore.Features.Hub.Contracts.KyHub.AddTab(Plugin.ModGuid, SettingsId, Loc.T("whipitup_tab_settings"), () => KydlCore.Features.Hub.Contracts.KyHub.ConfigEntries(config), 10);
        ModLog.Verbose("HubTabs: title + settings tab " + (settings != null ? "registered" : "FAILED"));
        return settings != null;
    }

    /// The Viewer feature's Start: the weapon list and the details page. Expected outcome: both tabs are registered.
    internal static bool AddWeaponTabs()
    {
        var viewer = KydlCore.Features.Hub.Contracts.KyHub.AddPage(Plugin.ModGuid, ViewerId, Loc.T("whipitup_tab_viewer"), WeaponKyPages.Viewer, 0, 30);
        var details = KydlCore.Features.Hub.Contracts.KyHub.AddPage(Plugin.ModGuid, DetailsId, Loc.T("whipitup_tab_details"), WeaponKyPages.Details, 1, 10);
        ModLog.Verbose("HubTabs: weapon tabs registered viewer " + (viewer != null) + ", details " + (details != null));
        return viewer != null && details != null;
    }

    /// The Viewer feature's Stop. Expected outcome: both tabs are gone (true also when they were never there).
    internal static bool RemoveWeaponTabs()
    {
        bool viewer = RemoveTab(ViewerId);       // both tabs are always removed, so no short-circuit
        bool details = RemoveTab(DetailsId);
        return viewer && details;
    }

    /// Mod unload: every tab of this mod is removed. Expected outcome: no tab of this mod is left.
    internal static bool UnregisterModPage()
    {
        bool weapons = RemoveWeaponTabs();
        bool settings = RemoveTab(SettingsId);
        return weapons && settings;
    }

    static bool RemoveTab(string id)
    {
        bool removed = KydlCore.Features.Hub.Contracts.KyHub.RemoveTab(Plugin.ModGuid, id);
        bool gone = !KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, id);
        ModLog.Verbose("HubTabs: tab '" + id + "' " + (removed ? "removed" : "was not registered") + (gone ? "" : " - STILL THERE"));
        return gone;
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
