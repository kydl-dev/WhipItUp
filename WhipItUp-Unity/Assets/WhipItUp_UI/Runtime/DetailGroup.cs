using System.Collections.Generic;

namespace WhipItUp.Ui
{
    /// <summary>A titled block of DetailRows on the Details tab (General, Attack 1, ...). Plain data, no Unity / Valheim types.</summary>
    internal sealed class DetailGroup
    {
        internal readonly string Title;
        internal readonly List<DetailRow> Rows = new List<DetailRow>();

        internal DetailGroup(string title) { Title = title ?? ""; }

        /// Adds a row and returns it.
        internal DetailRow Add(string label, string value, string maxValue = "")
        {
            var row = new DetailRow(label, value, maxValue);
            Rows.Add(row);
            return row;
        }

        public override string ToString() { return Title + " (" + Rows.Count + " rows)"; }
    }
}
