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
            { "whipitup_tab_viewer", "Viewer" },
            { "whipitup_tab_details", "Details" },
            { "whipitup_menu_family", "Family" },
            { "whipitup_menu_weapon", "Weapon" },
            { "whipitup_family_paddle", "Paddle" },
            { "whipitup_family_flogger", "Flogger" },
            { "whipitup_family_whip", "Whip" },
        };


        /// English text (the Unity preview; the game reads the same table through Jotunn).
        internal static string English(string key)
        {
            string s;
            return En.TryGetValue(key, out s) ? s : key;
        }
    }
}
