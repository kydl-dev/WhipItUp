using System;
using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The Details tab: the two drop-downs, then the selected weapon: name, description and the detail blocks (general, attacks, defence,
    /// durability, crafting) that WeaponDetailsBuilder made. Where a value changes with quality, a small header over the block names the two
    /// columns (quality 1, max quality).
    /// </summary>
    internal static class WeaponDetailsView
    {
        const float LabelWidth = 210f;      // px at the 1080p reference of the shared panel
        const float ValueWidth = 140f;

        internal static VisualElement Build(Func<string, string> text, WeaponSelection selection)
        {
            var tab = new VisualElement { name = "whipitup-details-tab" };
            tab.style.flexShrink = 0;
            tab.Add(WeaponPicker.Build(text, selection));
            var info = new VisualElement { name = "whipitup-details" };
            info.style.flexShrink = 0;
            tab.Add(info);

            Action show = () => Show(info, text, selection.Current);
            selection.Changed += show;
            show();
            return tab;
        }

        static void Show(VisualElement info, Func<string, string> text, WeaponInfo w)
        {
            info.Clear();
            if (w == null) { UiLog.Verbose("WeaponDetailsView: no weapon selected, nothing to show"); return; }
            info.Add(UiControls.Heading(w.Name));
            info.Add(UiControls.Note(w.Description));
            if (w.Groups.Count == 0)
            {
                UiLog.Verbose("WeaponDetailsView: " + w + " has no detail groups, showing the 'no item data' note");
                info.Add(UiControls.Note(text("whipitup_details_unavailable")));
                return;
            }
            int rows = 0;
            foreach (var g in w.Groups)
            {
                info.Add(UiControls.Heading(g.Title));
                bool hasMax = false;
                foreach (var r in g.Rows) if (r.HasMax) { hasMax = true; break; }
                if (hasMax) info.Add(ColumnHeader(w));
                foreach (var r in g.Rows) { info.Add(StatRow(r, hasMax)); rows++; }
            }
            UiLog.Verbose("WeaponDetailsView: showing " + w + ": " + w.Groups.Count + " group(s), " + rows + " row(s)");
        }

        /// "Quality 1 | Quality 4" over the two value columns of a block.
        static VisualElement ColumnHeader(WeaponInfo w)
        {
            var r = RowBox();
            r.Add(Cell("", LabelWidth, UiKit.TextDim, UiKit.SmallSize));
            r.Add(Cell(w.QualityHeader, ValueWidth, UiKit.TextDim, UiKit.SmallSize));
            r.Add(Cell(w.MaxQualityHeader, ValueWidth, UiKit.TextDim, UiKit.SmallSize));
            return r;
        }

        /// A row: label | value | (max-quality value). Only a row with a max value gets a fixed-width value cell (columns line up); any other row's value
        /// takes the rest of the line, so a longer text (bleed, an ingredient, the attack 2 effect) wraps instead of being cut.
        static VisualElement StatRow(DetailRow row, bool blockHasMax)
        {
            var r = RowBox();
            r.Add(Cell(row.Label, LabelWidth, UiKit.TextDim, UiKit.FontSize));
            var value = Cell(row.Value, blockHasMax && row.HasMax ? ValueWidth : -1f, UiKit.Text, UiKit.FontSize);
            r.Add(value);
            if (blockHasMax && row.HasMax) r.Add(Cell(row.MaxValue, ValueWidth, UiKit.Gold, UiKit.FontSize));
            return r;
        }

        static VisualElement RowBox()
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.FlexStart; r.style.flexShrink = 0;
            r.style.marginTop = 2; r.style.marginBottom = 2;
            return r;
        }

        /// A text cell: fixed width, or (width < 0) the rest of the line.
        static Label Cell(string content, float width, Color color, float size)
        {
            var l = UiKit.Label(content, size, color);
            l.style.whiteSpace = WhiteSpace.Normal;
            if (width >= 0f) { l.style.width = width; l.style.flexShrink = 0; }
            else { l.style.flexGrow = 1; l.style.flexShrink = 1; }
            return l;
        }
    }
}
