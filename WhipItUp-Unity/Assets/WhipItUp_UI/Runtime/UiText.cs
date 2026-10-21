using System.Collections.Generic;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Player-facing text of the mod, English only (user rule: no other languages). Shared by the game (registered with Jotunn in Plugin.Awake through Kydl.Common.Loc)
    /// and the Unity preview (English). Every key starts with "whipitup_" so it never clashes with another mod.
    /// </summary>
    internal static class UiText
    {
        internal static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "whipitup_menu_title", "WhipItUp" },
            { "whipitup_menu_footer", "Esc to close" },
            { "whipitup_menu_section", "Example" },
            { "whipitup_menu_note", "This is the starting menu of the mod. Replace its content in MainMenuView." },
            { "whipitup_menu_example_switch", "Example switch" },
            { "whipitup_menu_example_slider", "Example value" },
            { "whipitup_menu_close", "Close" },
        };


        /// English text (the Unity preview; the game reads the same table through Jotunn).
        internal static string English(string key)
        {
            string s;
            return En.TryGetValue(key, out s) ? s : key;
        }
    }
}
