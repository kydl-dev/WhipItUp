using KydlCore.Common;
using KydlCore.Core;
using WhipItUp.Ui;

namespace WhipItUp.Features.Viewer.Infrastructure;

/// <summary>The weapon viewer and details tabs in the KY hub. Gate: ModSettings.ViewerShown (viewer on and at least one family on). Live: Start adds the tabs, Stop removes them.</summary>
internal sealed class ViewerFeature : IFeature
{
    internal const string FeatureId = "viewer";
    internal static bool Running { get; private set; }
    public string Id { get { return FeatureId; } }
    public bool IsEnabled() { return ModSettings.ViewerShown; }
    public bool Start() { Running = HubTabs.AddWeaponTabs(); ModLog.Verbose("ViewerFeature: started " + Running); return Running; }
    public bool Stop() { bool gone = HubTabs.RemoveWeaponTabs(); Running = !gone; ModLog.Verbose("ViewerFeature: stopped " + gone); return gone; }
}
