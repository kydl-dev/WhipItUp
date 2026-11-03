using System;
using System.Collections.Generic;
using WhipItUp.Weapons;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The menu's list of weapons: all 21 items (3 families x 7 ranks) with the shown name and description and, when a stats provider is given,
    /// the detail blocks of the Details tab (WeaponDetailsBuilder). Pure logic (the text lookup and the stats are passed in), unit-tested.
    /// </summary>
    internal static class WeaponCatalog
    {
        /// `text` turns a localization key into the shown text (game: Loc.T). `log` gets the debug lines (null = silent). `stats` returns the numbers
        /// of a (family, rank) weapon, null for none (game: WeaponStatsReader.Read; null provider = no details at all).
        /// Expected outcome: Families x Ladder.Ranks entries, weakest rank first within each family.
        internal static List<WeaponInfo> Build(Func<string, string> text, Action<string> log = null, Func<string, int, WeaponStats> stats = null, Func<string, bool> familyOn = null)
        {
            var list = new List<WeaponInfo>();
            int withDetails = 0;
            // `Weapons.Ladder`, not `Ladder`: the game has its own global class named Ladder, which wins over the using-imported one in this namespace
            foreach (var family in WeaponSelection.Families)
            {
                if (familyOn != null && !familyOn(family)) { if (log != null) log("WeaponCatalog: " + family + " is off, its weapons are not listed"); continue; }   // [Paddle] / [Flogger] / [Whip] Enable
                for (int rank = 1; rank <= Weapons.Ladder.Ranks; rank++)
                {
                    string nameKey = WeaponNames.Key(family, rank), descKey = WeaponNames.DescKey(family, rank);
                    string name = text(nameKey) ?? "", desc = text(descKey) ?? "";
                    // an unknown key comes back as the key itself (or "$key"): a name that is still a token means the text table is missing it
                    if (log != null && (name == nameKey || name.StartsWith("$"))) log("WeaponCatalog: no text for '" + nameKey + "' (got '" + name + "')");
                    if (log != null && (desc == descKey || desc.StartsWith("$"))) log("WeaponCatalog: no text for '" + descKey + "' (got '" + desc + "')");
                    var info = new WeaponInfo(family, rank, name, desc);
                    var s = stats != null ? stats(family, rank) : null;
                    if (stats != null && s == null && log != null) log("WeaponCatalog: no stats for " + info + " - its Details tab shows no numbers");
                    if (s != null)
                    {
                        WeaponDetailsBuilder.Fill(info, s, text, log);
                        withDetails++;
                    }
                    list.Add(info);
                }
            }
            if (log != null) log("WeaponCatalog: " + list.Count + " weapon(s) built (" + WeaponSelection.Families.Length + " families x " + Weapons.Ladder.Ranks + " ranks, off families left out), " + withDetails + " with details");
            return list;
        }
    }
}
