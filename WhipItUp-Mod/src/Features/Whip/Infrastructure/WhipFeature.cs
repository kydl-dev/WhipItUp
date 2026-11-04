using KydlCore.Common;
using KydlCore.Core;
using WhipItUp.Weapons;

namespace WhipItUp.Features.Whip.Infrastructure;

/// <summary>The seven Whip weapons with their own animations and lash (Whips skill). Gate: ModSettings.WhipOn. The items are added to the game once, at startup (a game restart is needed to change a family), so Stop leaves them in place.</summary>
internal sealed class WhipFeature : IFeature
{
    internal const string FeatureId = "whip";
    static bool _registered;
    internal static bool Running { get; private set; }
    public string Id { get { return FeatureId; } }
    public bool IsEnabled() { return ModSettings.WhipOn; }
    public bool Start()
    {
        if (!_registered)
        {
            new WhipItems().Register(Plugin.Bundle, a => ModSettings.WhipAlignmentChanged += a);
            WhipAnimations.Load(Plugin.Bundle);   // T-013 custom whip clips + keyed lash
            _registered = true;
        }
        Running = true;
        ModLog.Verbose("WhipFeature: started");
        return Running;
    }
    public bool Stop() { Running = false; ModLog.Verbose("WhipFeature: stopped (items stay until the next game start)"); return !Running; }
}
