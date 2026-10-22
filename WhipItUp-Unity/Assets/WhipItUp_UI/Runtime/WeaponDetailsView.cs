using System;
using Kydl.Ui;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The Details tab: the two drop-downs, then the selected weapon. S2 shell: only the name and the description; the detail groups (general,
    /// attacks, defence, durability, crafting; quality 1 beside max quality) come in T-015 S3.
    /// </summary>
    internal static class WeaponDetailsView
    {
        internal static VisualElement Build(Func<string, string> text, WeaponSelection selection)
        {
            var tab = new VisualElement { name = "whipitup-details-tab" };
            tab.style.flexShrink = 0;
            tab.Add(WeaponPicker.Build(text, selection));
            var info = new VisualElement { name = "whipitup-details" };
            info.style.flexShrink = 0;
            tab.Add(info);

            Action show = () =>
            {
                info.Clear();
                var w = selection.Current;
                UiLog.Verbose("WeaponDetailsView: showing " + (w != null ? w.ToString() : "no weapon"));
                if (w == null) return;
                info.Add(UiControls.Heading(w.Name));
                info.Add(UiControls.Note(w.Description));
            };
            selection.Changed += show;
            show();
            return tab;
        }
    }
}
