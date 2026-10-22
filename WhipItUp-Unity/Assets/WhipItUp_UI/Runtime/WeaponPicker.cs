using System;
using System.Collections.Generic;
using System.Globalization;
using Kydl.Ui;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The two drop-downs both tabs start with: Family (Paddle / Flogger / Whip) and Weapon ("V1 - Boar'd Game"). They read and write the shared
    /// WeaponSelection and rebuild themselves when it changes (the shared UiControls.Dropdown keeps its current value inside, so a new value
    /// means a new drop-down; it closes before the choice is reported, so rebuilding from inside the choice is safe).
    /// </summary>
    internal static class WeaponPicker
    {
        internal static VisualElement Build(Func<string, string> text, WeaponSelection selection)
        {
            var box = new VisualElement { name = "whipitup-picker" };
            box.style.flexShrink = 0;
            Action fill = () => Fill(box, text, selection);
            selection.Changed += fill;
            Fill(box, text, selection);
            return box;
        }

        static void Fill(VisualElement box, Func<string, string> text, WeaponSelection selection)
        {
            box.Clear();
            var families = new List<string>(WeaponSelection.Families);
            var ranks = new List<string>();
            foreach (var w in selection.InFamily(selection.Family)) ranks.Add(w.Rank.ToString(CultureInfo.InvariantCulture));
            UiLog.Verbose("WeaponPicker: filled for " + selection.Family + " V" + selection.Rank + ", " + ranks.Count + " weapon(s) in the family");

            box.Add(UiControls.Row(text("whipitup_menu_family"), UiControls.Dropdown(
                families, selection.Family,
                f => text("whipitup_family_" + f.ToLowerInvariant()),
                f => { selection.SetFamily(f); })));
            box.Add(UiControls.Row(text("whipitup_menu_weapon"), UiControls.Dropdown(
                ranks, selection.Rank.ToString(CultureInfo.InvariantCulture),
                r => WeaponLabel(selection, r),
                r =>
                {
                    int rank;
                    if (int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out rank)) selection.SetRank(rank);
                    else UiLog.Warn("WeaponPicker: drop-down returned '" + r + "', not a rank number - selection unchanged");
                })));
        }

        /// "V3 - Bjorn to Spank" for the rank option `rank` of the selected family.
        static string WeaponLabel(WeaponSelection selection, string rank)
        {
            int n;
            if (!int.TryParse(rank, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return rank;
            var info = selection.Find(selection.Family, n);
            return "V" + n + " - " + (info != null ? info.Name : "?");
        }
    }
}
