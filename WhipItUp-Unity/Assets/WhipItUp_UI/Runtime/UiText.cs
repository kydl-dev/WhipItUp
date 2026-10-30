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
            { "whipitup_tab_settings", "Settings" },
            { "whipitup_menu_family", "Family" },
            { "whipitup_menu_weapon", "Weapon" },
            { "whipitup_family_paddle", "Paddle" },
            { "whipitup_family_flogger", "Flogger" },
            { "whipitup_family_whip", "Whip" },

            // Viewer tab (the 3D view)
            { "whipitup_viewer_hint", "Left-drag: turn   Right-drag: move   Wheel: zoom" },
            { "whipitup_viewer_reset", "Reset" },
            { "whipitup_viewer_straight", "Straight" },
            { "whipitup_viewer_coiled", "Coiled" },
            { "whipitup_viewer_unavailable", "No 3D view here." },

            // Details tab: blocks, row labels, damage types, templates ({0} / {1} are filled in by WeaponDetailsBuilder)
            { "whipitup_details_quality", "Quality" },
            { "whipitup_details_unavailable", "No item data yet: open this menu in a world to see the numbers." },
            { "whipitup_group_general", "General" },
            { "whipitup_group_attack1", "Attack 1" },
            { "whipitup_group_attack2", "Attack 2" },
            { "whipitup_group_defence", "Defence" },
            { "whipitup_group_durability", "Durability" },
            { "whipitup_group_crafting", "Crafting" },
            { "whipitup_row_family", "Family" },
            { "whipitup_row_rank", "Rank" },
            { "whipitup_row_skill", "Skill" },
            { "whipitup_row_weight", "Weight" },
            { "whipitup_row_hands", "Hands" },
            { "whipitup_hands_one", "One-handed" },
            { "whipitup_hands_two", "Two-handed" },
            { "whipitup_row_effect", "Effect" },
            { "whipitup_row_bleed", "Bleed" },
            { "whipitup_row_knockback", "Knockback" },
            { "whipitup_row_stagger", "Stagger" },
            { "whipitup_row_stamina", "Stamina" },
            { "whipitup_row_reach", "Reach" },
            { "whipitup_row_block", "Block power" },
            { "whipitup_row_parry", "Parry bonus" },
            { "whipitup_row_deflection", "Deflection force" },
            { "whipitup_row_durability", "Durability" },
            { "whipitup_row_maxquality", "Max quality" },
            { "whipitup_row_station", "Station" },
            { "whipitup_dmg_blunt", "Blunt" },
            { "whipitup_dmg_slash", "Slash" },
            { "whipitup_dmg_pierce", "Pierce" },
            { "whipitup_dmg_chop", "Chop" },
            { "whipitup_dmg_pickaxe", "Pickaxe" },
            { "whipitup_dmg_fire", "Fire" },
            { "whipitup_dmg_frost", "Frost" },
            { "whipitup_dmg_lightning", "Lightning" },
            { "whipitup_dmg_poison", "Poison" },
            { "whipitup_dmg_spirit", "Spirit" },
            { "whipitup_bleed_value", "{0} per second for {1} s" },
            { "whipitup_station_value", "{0}, level {1}" },
            { "whipitup_ingredient_value", "{0} (+{1} per upgrade)" },
            { "whipitup_recipe_missing", "No recipe found" },
            { "whipitup_attack2_backhand", "Back-handed swing" },
            { "whipitup_attack2_grab", "Lash grab: pulls the target toward you" },
        };


        /// English text (the Unity preview; the game reads the same table through Jotunn).
        internal static string English(string key)
        {
            string s;
            return En.TryGetValue(key, out s) ? s : key;
        }
    }
}
