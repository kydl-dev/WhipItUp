namespace WhipItUp.Weapons;

/// <summary>Pure maths of the whip hit-stop (T-019, WhipHitStopPatch); no game types, unit-tested.</summary>
internal static class HitStopMath
{
    /// The pause a whip hit gets: the setting, but never longer than the game's own pause and never negative.
    /// A vanilla value of 0 or less is passed through unchanged. Expected outcome: 0..vanilla.
    internal static float Duration(float vanilla, float setting)
    {
        // Stryker disable once Equality : for a vanilla value of exactly 0 the clamp below also gives 0
        if (vanilla <= 0f) return vanilla;
        // Stryker disable once Equality : at the boundaries (setting == vanilla, setting == 0) both branches return the same number
        return setting < 0f ? 0f : (setting > vanilla ? vanilla : setting);
    }
}
