namespace WhipItUp.Weapons
{
    /// <summary>Pure maths of the whip hit-stop (T-019, WhipHitStopPatch); no game types, unit-tested.</summary>
    internal static class HitStopMath
    {
        /// The pause a whip hit gets: the setting, but never longer than the game's own pause and never negative.
        /// A vanilla value of 0 or less is passed through unchanged. Expected outcome: 0..vanilla.
        internal static float Duration(float vanilla, float setting)
        {
            if (vanilla <= 0f) return vanilla;
            return setting < 0f ? 0f : (setting > vanilla ? vanilla : setting);
        }
    }
}
