namespace WhipItUp.Testing
{
    /// <summary>
    /// T-023: pure verdicts of the whip animation in-game tests (no game types, unit-tested). Each returns "PASS: ..." / "FAIL: ..." /
    /// "SKIP: ..." like the GameProbe tests.
    /// </summary>
    internal static class WhipAnimChecks
    {
        /// Clips: all body clips (bundle) and all keyed lash clips (embedded whip_lash_frames.txt) are there.
        internal static string Clips(int body, int bodyExpected, int lash, int lashExpected)
        {
            string nums = body + " of " + bodyExpected + " body clip(s), " + lash + " of " + lashExpected + " lash clip(s)";
            return body == bodyExpected && lash == lashExpected ? "PASS: " + nums : "FAIL: " + nums + " - rebuild the bundle / check whip_lash_frames.txt";
        }

        /// Controller: holding a whip = our override with every slot replaced; not holding one = the game's own controller.
        internal static string Controller(bool enabled, bool holdsWhip, bool attached, bool overrideActive, int replaced, int expected)
        {
            if (!enabled) return "SKIP: [Whip] CustomAnimations is off";
            if (holdsWhip)
                return attached && overrideActive && replaced == expected
                    ? "PASS: whip in hand, whip clips active (" + replaced + " of " + expected + " slot(s) replaced)"
                    : "FAIL: whip in hand but attached " + attached + ", override " + overrideActive + ", " + replaced + " of " + expected + " slot(s) replaced";
            return !attached && !overrideActive
                ? "PASS: no whip in hand, the game's own controller (no whip slots)"
                : "FAIL: no whip in hand but attached " + attached + ", override " + overrideActive + " (" + replaced + " whip slot(s)) - the original controller did not come back";
        }

        /// Swaps: every controller swap this session put every animator layer back in its state (TEST-020: lying / sitting player).
        internal static string Swaps(int swaps, int losing, string lastLoss)
        {
            if (swaps == 0) return "SKIP: no controller swap yet - equip a whip, then another weapon, press PageDown";
            return losing == 0 ? "PASS: " + swaps + " swap(s), every layer state carried over"
                : "FAIL: " + losing + " of " + swaps + " swap(s) lost layer states (last: " + lastLoss + ")";
        }
    }
}
