using System.Collections.Generic;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Item names (English only, PROJECT_RULES 15), keys "whipitup_&lt;family&gt;_v&lt;rank&gt;". Paddle + Flogger names approved by the user
    /// (2026-10-22, T-012); Whip names still being worked on with the user - placeholders until then.
    /// </summary>
    internal static class WeaponNames
    {
        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "whipitup_paddle_v1", "Boar'd Game" },
            { "whipitup_paddle_v2", "Deerly Beloved" },
            { "whipitup_paddle_v3", "Bjorn to Spank" },
            { "whipitup_paddle_v4", "Howl Monitor" },
            { "whipitup_paddle_v5", "Lox and Load" },
            { "whipitup_paddle_v6", "Ask Nicely" },
            { "whipitup_paddle_v7", "Seal of Approval" },
            { "whipitup_flogger_v1", "Neck & Tails" },
            { "whipitup_flogger_v2", "Greydwarf Fringe" },
            { "whipitup_flogger_v3", "Bear Necessities" },
            { "whipitup_flogger_v4", "Fenring Fringe" },
            { "whipitup_flogger_v5", "Fuling Around" },
            { "whipitup_flogger_v6", "Charred Tails" },
            { "whipitup_flogger_v7", "Moose Tassels" },
            { "whipitup_whip_v1", "Whip V1" },
            { "whipitup_whip_v2", "Whip V2" },
            { "whipitup_whip_v3", "Whip V3" },
            { "whipitup_whip_v4", "Whip V4" },
            { "whipitup_whip_v5", "Whip V5" },
            { "whipitup_whip_v6", "Whip V6" },
            { "whipitup_whip_v7", "Whip V7" },
        };

        public static string Key(string family, int rank) { return "whipitup_" + family.ToLowerInvariant() + "_v" + rank; }

        /// The UI text table + the item names, for Loc.Register. Expected outcome: every name key is in the result.
        public static Dictionary<string, string> MergeInto(Dictionary<string, string> ui)
        {
            var all = new Dictionary<string, string>(ui ?? new Dictionary<string, string>());
            foreach (var kv in En) all[kv.Key] = kv.Value;
            return all;
        }
    }
}
