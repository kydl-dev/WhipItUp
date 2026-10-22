namespace WhipItUp.Ui
{
    /// <summary>
    /// What the menu knows about one weapon: plain data, no game types (the game side builds it: src/Ui/WeaponCatalog.cs; the Unity preview
    /// fills it with sample values). The details tab rows (damage, defence, durability, crafting) are added to it in T-015 S3.
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

        internal WeaponInfo(string family, int rank, string name, string description)
        {
            Family = family;
            Rank = rank;
            Name = name ?? "";
            Description = description ?? "";
        }

        public override string ToString() { return Family + " V" + Rank + " '" + Name + "'"; }
    }
}
