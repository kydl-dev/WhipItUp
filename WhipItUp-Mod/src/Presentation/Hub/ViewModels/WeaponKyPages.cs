using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kydl.Common;
using KydlCore;

namespace WhipItUp.Ui
{
    /// <summary>
    /// T-098: the Viewer and Details tabs as described pages (family + weapon choices, then either the hosted 3D view or the detail rows). KydlCore draws them and sets the text size.
    /// The State holds the chosen weapon, so a pick (and nothing else) rebuilds the page.
    /// </summary>
    internal static class WeaponKyPages
    {
        static void Pickers(KySection s, WeaponSelection selection, Func<string, string> text, StringBuilder state)
        {
            var families = new List<KyChoice>();
            foreach (var f in WeaponSelection.Families) families.Add(new KyChoice(f, text("whipitup_family_" + f.ToLowerInvariant())));
            s.Choice(text("whipitup_menu_family"), families, selection.Family, f => selection.SetFamily(f));
            var ranks = new List<KyChoice>();
            foreach (var w in selection.InFamily(selection.Family))
                ranks.Add(new KyChoice(w.Rank.ToString(CultureInfo.InvariantCulture), "V" + w.Rank + " - " + w.Name));
            s.Choice(text("whipitup_menu_weapon"), ranks, selection.Rank.ToString(CultureInfo.InvariantCulture), r =>
            {
                int rank;
                if (int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out rank)) selection.SetRank(rank);
            });
            state.Append(selection.Family).Append(selection.Rank).Append('|').Append(ranks.Count).Append('|');
        }

        /// <summary>Expected outcome: the two choices and the hosted 3D view of the chosen weapon.</summary>
        internal static KyPage Viewer()
        {
            Func<string, string> text = k => Loc.T(k);
            var selection = HubTabs.NewSelection();
            var viewer = ModSettings.Viewer();
            var page = new KyPage();
            var state = new StringBuilder("viewer|");
            var s = page.Section("");
            Pickers(s, selection, text, state);
            s.Hosted(() => WeaponViewportView.Build(text, selection, () => WeaponViewerScene.Make(viewer), viewer, false));
            page.State = state.ToString();
            return page;
        }

        /// <summary>Expected outcome: the two choices, then the chosen weapon's name, description and detail blocks (a note when it has none).</summary>
        internal static KyPage Details()
        {
            Func<string, string> text = k => Loc.T(k);
            var selection = HubTabs.NewSelection();
            var page = new KyPage();
            var state = new StringBuilder("details|");
            Pickers(page.Section(""), selection, text, state);
            var w = selection.Current;
            if (w == null) { page.State = state.ToString(); return page; }
            var head = page.Section(w.Name);
            head.Note(w.Description);
            state.Append(w.Name).Append('|');
            if (w.Groups.Count == 0) { head.Note(text("whipitup_details_unavailable")); page.State = state.ToString(); return page; }
            foreach (var g in w.Groups)
            {
                var s = page.Section(g.Title);
                bool hasMax = false;
                foreach (var r in g.Rows) if (r.HasMax) { hasMax = true; break; }
                if (hasMax) s.Columns(w.QualityHeader, w.MaxQualityHeader);
                foreach (var r in g.Rows)
                {
                    s.Value(r.Label, r.Value, hasMax && r.HasMax ? r.MaxValue : "");
                    state.Append(r.Label).Append('=').Append(r.Value).Append('/').Append(r.MaxValue).Append('|');
                }
            }
            page.State = state.ToString();
            return page;
        }
    }
}
