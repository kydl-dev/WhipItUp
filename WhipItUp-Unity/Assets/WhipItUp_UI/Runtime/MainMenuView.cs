using System;
using System.Collections.Generic;
using Kydl.Ui;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// WHAT the mod's menu shows (UI Toolkit, shared Kydl.Ui look): a window with two tabs, Viewer and Details, that share one weapon selection.
    /// Compiled into the mod AND shown by the Unity preview (Assets/WhipItUp_UI/Preview/MenuPreview.cs: enter Play mode in the Preview scene).
    /// Keep game types (Player, ZNet, ...) OUT of this folder: the Unity project does not have them. The game side (keys, input lock,
    /// game data) is src/Ui/MainMenu.cs + WeaponCatalog.cs in the mod; pass data in through Build's parameters.
    /// </summary>
    internal static class MainMenuView
    {
        /// Builds the window. `text` turns a text key into the shown text (game: the mod's localization, Unity: English); `weapons` is the list
        /// of all weapons (game: WeaponCatalog.Build, Unity: the preview's sample list).
        internal static VisualElement Build(Func<string, string> text, IList<WeaponInfo> weapons, Action onClose)
        {
            var w = UiKit.Window(text("whipitup_menu_title"), text("whipitup_menu_footer"), onClose, out UiScroll body);
            var selection = new WeaponSelection(weapons, UiLog.Verbose);

            var tabs = new[] { WeaponViewportView.Build(text, selection), WeaponDetailsView.Build(text, selection) };
            foreach (var t in tabs) body.Content.Add(t);

            var bar = new TabBar(new[] { text("whipitup_tab_viewer"), text("whipitup_tab_details") }, 0, index => ShowTab(tabs, index));
            ShowTab(tabs, bar.Selected);
            w.Insert(1, bar.Root);               // between the title bar and the scrolling body: the tabs stay put while the content scrolls
            UiLog.Verbose("MainMenuView: built (" + (weapons != null ? weapons.Count : 0) + " weapon(s), " + tabs.Length + " tabs, start " + selection.Family + " V" + selection.Rank + ")");
            return w;
        }

        static void ShowTab(VisualElement[] tabs, int index)
        {
            for (int i = 0; i < tabs.Length; i++) tabs[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
            UiLog.Verbose("MainMenuView: tab " + index + " shown");
        }
    }
}
