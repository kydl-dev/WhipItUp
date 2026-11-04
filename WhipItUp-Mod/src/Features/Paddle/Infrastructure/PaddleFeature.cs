using KydlCore.Common;
using KydlCore.Core;
using WhipItUp.Weapons;

namespace WhipItUp.Features.Paddle.Infrastructure;

/// <summary>The seven Paddle weapons (Clubs skill). Gate: ModSettings.PaddleOn. The items are added to the game once, at startup (a game restart is needed to change a family), so Stop leaves them in place.</summary>
internal sealed class PaddleFeature : IFeature
{
    internal const string FeatureId = "paddle";
    static bool _registered;
    internal static bool Running { get; private set; }
    public string Id { get { return FeatureId; } }
    public bool IsEnabled() { return ModSettings.PaddleOn; }
    public bool Start()
    {
        if (!_registered)
        {
            new PaddleItems().Register(Plugin.Bundle, a => ModSettings.PaddleAlignmentChanged += a);

            _registered = true;
        }
        Running = true;
        ModLog.Verbose("PaddleFeature: started");
        return Running;
    }
    public bool Stop() { Running = false; ModLog.Verbose("PaddleFeature: stopped (items stay until the next game start)"); return !Running; }
}
