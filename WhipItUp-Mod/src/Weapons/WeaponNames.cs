using System.Collections.Generic;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Item names (English only, PROJECT_RULES 15), keys "whipitup_&lt;family&gt;_v&lt;rank&gt;". Paddle + Flogger names approved by the user
    /// (2026-10-22, T-012); Whip names chosen by the user (2026-10-22).
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
            { "whipitup_flogger_v2", "Brutalizer" },
            { "whipitup_flogger_v3", "Bear Necessities" },
            { "whipitup_flogger_v4", "Fenring Fringe" },
            { "whipitup_flogger_v5", "Fuling Around" },
            { "whipitup_flogger_v6", "Charred Tails" },
            { "whipitup_flogger_v7", "Moose Tassels" },
            { "whipitup_whip_v1", "Boar to be Wild" },
            { "whipitup_whip_v2", "Troll Toll" },
            { "whipitup_whip_v3", "Bear Witness" },
            { "whipitup_whip_v4", "Moder of Discipline" },
            { "whipitup_whip_v5", "Barley Legal" },
            { "whipitup_whip_v6", "Charred and Dangerous" },
            { "whipitup_whip_v7", "Jötun Tamer" },
        };

        /// Item descriptions written by the user (2026-10-22, D-045), keys "whipitup_&lt;family&gt;_v&lt;rank&gt;_desc". Straight apostrophes only: the
        /// game's fonts are not known to draw the curly one.
        public static readonly Dictionary<string, string> Desc = new Dictionary<string, string>
        {
            { "whipitup_paddle_v1_desc", "A beginner's favourite. Easy to handle, difficult to explain to the village healer." },
            { "whipitup_paddle_v2_desc", "A heartfelt tribute to the deer. Its hide lives on, and someone's backside will remember it" },
            { "whipitup_paddle_v3_desc", "Some Vikings are born to raid. Others are Bjorn to make you regret talking back." },
            { "whipitup_paddle_v4_desc", "Keeps the pack in line, one loud clap at a time." },
            { "whipitup_paddle_v5_desc", "When life gives you lox, make the Plains regret it." },
            { "whipitup_paddle_v6_desc", "Manners maketh the Viking. Repeated requests maketh the mark." },
            { "whipitup_paddle_v7_desc", "Five stars. One very sore Viking." },
            { "whipitup_flogger_v1_desc", "Comes with a complimentary shiver down the spine." },
            { "whipitup_flogger_v2_desc", "The diplomatic option for Vikings who have run out of diplomatic options." },
            { "whipitup_flogger_v3_desc", "Barely civilized. Bearly merciful." },
            { "whipitup_flogger_v4_desc", "The latest in Fenring couture. One size fits ow." },
            { "whipitup_flogger_v5_desc", "The Fulings called it unfair. They found out." },
            { "whipitup_flogger_v6_desc", "Smoked, stroked, and thoroughly lashed." },
            { "whipitup_flogger_v7_desc", "Fashionably ferocious. Tragically effective." },
            { "whipitup_whip_v1_desc", "Let the beast out. Then put it back in line." },
            { "whipitup_whip_v2_desc", "Pay the toll or hear the crack." },
            { "whipitup_whip_v3_desc", "For making an impression that even the bears will remember." },
            { "whipitup_whip_v4_desc", "The mother of all discipline, with dragon-grade patience." },
            { "whipitup_whip_v5_desc", "Drink responsibly. Swing irresponsibly." },
            { "whipitup_whip_v6_desc", "Warning: prolonged exposure may cause singed eyebrows, colourful language, and sudden respect." },
            { "whipitup_whip_v7_desc", "Size doesn't matter. Technique does." },
        };

        public static string Key(string family, int rank) { return "whipitup_" + family.ToLowerInvariant() + "_v" + rank; }

        /// The localization key of a rank's description.
        public static string DescKey(string family, int rank) { return Key(family, rank) + "_desc"; }

        /// The UI text table + the item names + the item descriptions, for Loc.Register. Expected outcome: every name and description key is in the result.
        public static Dictionary<string, string> MergeInto(Dictionary<string, string> ui)
        {
            var all = new Dictionary<string, string>(ui ?? new Dictionary<string, string>());
            foreach (var kv in En) all[kv.Key] = kv.Value;
            foreach (var kv in Desc) all[kv.Key] = kv.Value;
            return all;
        }
    }
}
