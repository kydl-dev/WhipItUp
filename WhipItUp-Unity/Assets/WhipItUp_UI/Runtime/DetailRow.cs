namespace WhipItUp.Ui
{
    /// <summary>
    /// One line of the Details tab: a label and its value at quality 1; MaxValue is the value at the weapon's max quality and is empty when the
    /// value does not change with quality (D-048 Q4). Plain data, no Unity / Valheim types.
    /// </summary>
    internal sealed class DetailRow
    {
        internal readonly string Label;
        internal readonly string Value;
        internal readonly string MaxValue;

        internal bool HasMax { get { return MaxValue.Length > 0; } }

        internal DetailRow(string label, string value, string maxValue = "")
        {
            Label = label ?? "";
            Value = value ?? "";
            MaxValue = maxValue ?? "";
        }

        public override string ToString() { return Label + " = " + Value + (HasMax ? " -> " + MaxValue : ""); }
    }
}
