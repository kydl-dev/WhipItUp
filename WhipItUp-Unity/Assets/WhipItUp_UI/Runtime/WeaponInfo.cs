using System.Collections.Generic;

namespace WhipItUp.Ui
{
    /// <summary>
    /// What the menu knows about one weapon: plain data, no game types (the game side builds it: src/Ui/WeaponCatalog.cs; the Unity preview
    /// fills it with sample values). The Details tab rows (general, attacks, defence, durability, crafting) are filled in by
    /// WeaponDetailsBuilder.Fill; a weapon without stats has no groups.
    /// </summary>
    internal sealed class WeaponInfo
    {
        /// "Paddle", "Flogger" or "Whip" (one of WeaponSelection.Families).
        internal readonly string Family;
        /// 1..7 (V1 = weakest).
        internal readonly int Rank;
        /// The shown item name, e.g. "Boar'd Game".
        internal readonly string Name;
        /// The shown item description (the user's one-liner).
        internal readonly string Description;

        /// The Details tab blocks, in display order (empty until WeaponDetailsBuilder.Fill ran).
        internal readonly List<DetailGroup> Groups = new List<DetailGroup>();
        /// Column titles over the values: "Quality 1" and "Quality 4" (empty without groups).
        internal string QualityHeader = "";
        internal string MaxQualityHeader = "";

        internal WeaponInfo(string family, int rank, string name, string description)
        {
            Family = family;
            Rank = rank;
            Name = name ?? "";
            Description = description ?? "";
        }

        /// True when at least one row has a max-quality value, so the view needs the second column.
        internal bool HasMaxColumn
        {
            get
            {
                foreach (var g in Groups)
                    foreach (var r in g.Rows)
                        if (r.HasMax) return true;
                return false;
            }
        }

        public override string ToString() { return Family + " V" + Rank + " '" + Name + "'"; }
    }
}
