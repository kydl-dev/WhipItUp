namespace WhipItUp
{
    /// <summary>
    /// Which functionalities of WhipItUp are on (PROJECT_RULES 11, user 2026-10-30). Pure: no game types, unit tested.
    /// A functionality works only when [General] EnableAll AND its own [Section] Enable are on. Off = it is not added to the game / not shown.
    /// Functionalities: Paddle, Flogger, Whip (weapon families) and Viewer (the weapon viewer + details tabs in the KY hub).
    /// </summary>
    internal static class Functions
    {
        /// Expected outcome: true only when both switches are on.
        internal static bool IsOn(bool enableAll, bool own) { return enableAll && own; }

        /// The Whips skill is shared by the flogger and the whip: it is needed when at least one of them is on. Expected outcome: false only when both are off.
        internal static bool SkillNeeded(bool flogger, bool whip) { return flogger || whip; }

        /// Is the weapon family (Paddle / Flogger / Whip, case ignored) on? Unknown names are off. Expected outcome: the matching flag, false for an unknown name.
        internal static bool FamilyOn(string family, bool paddle, bool flogger, bool whip)
        {
            if (string.Equals(family, "Paddle", System.StringComparison.OrdinalIgnoreCase)) return paddle;
            if (string.Equals(family, "Flogger", System.StringComparison.OrdinalIgnoreCase)) return flogger;
            if (string.Equals(family, "Whip", System.StringComparison.OrdinalIgnoreCase)) return whip;
            return false;
        }

        /// The viewer / details tabs show weapons, so they are shown when the viewer is on AND at least one family is on.
        internal static bool ViewerShown(bool viewer, bool paddle, bool flogger, bool whip) { return viewer && (paddle || flogger || whip); }
    }
}
