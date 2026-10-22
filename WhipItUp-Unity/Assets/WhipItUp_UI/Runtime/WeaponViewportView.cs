using System;
using Kydl.Ui;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The Viewer tab: the two drop-downs and the viewport. S2 shell: the viewport is an empty framed box showing the weapon's name; the 3D view
    /// (render texture, rotate / zoom / clip / reset) replaces its content in T-015 S4 / S5.
    /// </summary>
    internal static class WeaponViewportView
    {
        /// Height of the viewport frame in UI pixels (S5 turns it into a Tunable with a range).
        internal const float FrameHeight = 420f;

        internal static VisualElement Build(Func<string, string> text, WeaponSelection selection)
        {
            var tab = new VisualElement { name = "whipitup-viewer-tab" };
            tab.style.flexShrink = 0;
            tab.Add(WeaponPicker.Build(text, selection));

            var frame = new VisualElement { name = "whipitup-viewport" };
            frame.style.flexShrink = 0; frame.style.height = FrameHeight; frame.style.marginTop = 10;
            frame.style.backgroundColor = UiKit.Track;
            UiKit.Borders(frame, 1, UiKit.Border); UiKit.Radius(frame, 4);
            frame.style.overflow = Overflow.Hidden;                    // what is zoomed past the frame is never shown
            frame.style.justifyContent = Justify.Center; frame.style.alignItems = Align.Center;
            var name = UiKit.Label("", UiKit.SectionSize, UiKit.TextDim, true);
            frame.Add(name);
            tab.Add(frame);

            Action show = () =>
            {
                var w = selection.Current;
                name.text = w != null ? w.Name : "";
                UiLog.Verbose("WeaponViewportView: showing " + (w != null ? w.ToString() : "no weapon") + " (frame " + FrameHeight + " px high)");
            };
            selection.Changed += show;
            show();
            return tab;
        }
    }
}
