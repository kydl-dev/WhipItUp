using System;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The pose the Viewer tab shows the weapon in (T-015 S6b, D-057). The whip is shown COILED by default (like its icon, D-037) and has a
    /// Straight / Coiled switch button; the other families have no switch and always show their rest pose. Pure logic (no Unity / Valheim types):
    /// the viewport owns one instance per menu open, so the chosen pose survives a weapon change but a new menu open starts coiled again.
    /// </summary>
    internal sealed class ViewerPose
    {
        /// The only family with the switch (D-057: "for the whip only").
        internal const string SwitchFamily = "Whip";

        internal const string StraightKey = "whipitup_viewer_straight";
        internal const string CoiledKey = "whipitup_viewer_coiled";

        const bool DefaultCoiled = true;

        readonly Action<string> _log;
        bool _coiled = DefaultCoiled;

        internal ViewerPose(Action<string> log = null) { _log = log; }

        /// What the switch is set to (the default is coiled; only a family with the switch uses it).
        internal bool Coiled { get { return _coiled; } }

        /// Whether the Viewer tab offers the Straight / Coiled switch for that family.
        internal static bool HasSwitch(string family) { return string.Equals(family, SwitchFamily, StringComparison.Ordinal); }

        /// Whether the model of that family is made in the coil pose now (a family without the switch is never coiled).
        internal bool ShowsCoiled(string family) { return HasSwitch(family) && _coiled; }

        /// The text key of the switch button: it names what a click gives, so a coiled whip offers "Straight" and a straight one "Coiled".
        internal string ButtonKey { get { return _coiled ? StraightKey : CoiledKey; } }

        /// Flips the pose. Expected outcome: the pose is the opposite of before (returns that check).
        internal bool Toggle()
        {
            bool before = _coiled;
            _coiled = !_coiled;
            if (_log != null) _log("ViewerPose: " + (before ? "coiled" : "straight") + " -> " + (_coiled ? "coiled" : "straight") + " (switch button, default " + (DefaultCoiled ? "coiled" : "straight") + ")");
            return _coiled != before;
        }

        public override string ToString() { return _coiled ? "coiled" : "straight"; }
    }
}
