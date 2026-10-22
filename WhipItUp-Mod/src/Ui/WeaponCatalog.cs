using System;
using System.Collections.Generic;
using WhipItUp.Weapons;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The menu's list of weapons: all 21 items (3 families x 7 ranks) with the shown name and description. Pure logic (the text lookup is passed
    /// in), unit-tested. S2 only carries name + description; the detail values read from the live items are added in T-015 S3.
    /// </summary>
    internal static class WeaponCatalog
    {
        /// `text` turns a localization key into the shown text (game: Loc.T). `log` gets the debug lines (null = silent).
        /// Expected outcome: Families x Ladder.Ranks entries, weakest rank first within each family.
        internal static List<WeaponInfo> Build(Func<string, string> text, Action<string> log = null)
        {
            var list = new List<WeaponInfo>();
            // `Weapons.Ladder`, not `Ladder`: the game has its own global class named Ladder, which wins over the using-imported one in this namespace
            foreach (var family in WeaponSelection.Families)
            {
                for (int rank = 1; rank <= Weapons.Ladder.Ranks; rank++)
                {
                    string nameKey = WeaponNames.Key(family, rank), descKey = WeaponNames.DescKey(family, rank);
                    string name = text(nameKey) ?? "", desc = text(descKey) ?? "";
                    // an unknown key comes back as the key itself (or "$key"): a name that is still a token means the text table is missing it
                    if (log != null && (name == nameKey || name.StartsWith("$"))) log("WeaponCatalog: no text for '" + nameKey + "' (got '" + name + "')");
                    if (log != null && (desc == descKey || desc.StartsWith("$"))) log("WeaponCatalog: no text for '" + descKey + "' (got '" + desc + "')");
                    list.Add(new WeaponInfo(family, rank, name, desc));
                }
            }
            if (log != null) log("WeaponCatalog: " + list.Count + " weapon(s) built (" + WeaponSelection.Families.Length + " families x " + Weapons.Ladder.Ranks + " ranks)");
            return list;
        }
    }
}
