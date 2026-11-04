using KydlCore.Common;
using KydlCore.Core;
using WhipItUp.Weapons;

namespace WhipItUp.Features.Flogger.Infrastructure;

/// <summary>The seven Flogger weapons (Whips skill). Gate: ModSettings.FloggerOn. The items are added to the game once, at startup (a game restart is needed to change a family), so Stop leaves them in place.</summary>
internal sealed class FloggerFeature : IFeature
{
    internal const string FeatureId = "flogger";
    static bool _registered;
    internal static bool Running { get; private set; }
    public string Id { get { return FeatureId; } }
    public bool IsEnabled() { return ModSettings.FloggerOn; }
    public bool Start()
    {
        if (!_registered)
        {
            new FloggerItems().Register(Plugin.Bundle, a => ModSettings.FloggerAlignmentChanged += a);

            _registered = true;
        }
        Running = true;
        ModLog.Verbose("FloggerFeature: started");
        return Running;
    }
    public bool Stop() { Running = false; ModLog.Verbose("FloggerFeature: stopped (items stay until the next game start)"); return !Running; }
}
