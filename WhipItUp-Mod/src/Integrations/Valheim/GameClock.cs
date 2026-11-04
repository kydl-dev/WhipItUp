using KydlCore.Core;

namespace WhipItUp.Integrations.Valheim;

/// <summary>Game-side clock for pure code: Unity's Time.time (seconds since the game started).</summary>
internal sealed class GameClock : IClock
{
    public float Now { get { return UnityEngine.Time.time; } }
}
