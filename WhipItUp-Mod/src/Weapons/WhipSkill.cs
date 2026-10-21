using System;
using Jotunn.Configs;
using Jotunn.Managers;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// T-007: the custom "Whips" skill used by the Flogger and Whip families (D-007). Same progression as Clubs (IncreaseStep 1, the game's
    /// usual damage-by-skill curve). Registered in Plugin.Awake: Jotunn needs skills before the game creates the player's Skills object.
    /// Icon: a placeholder until Whip_V1 exists (user 2026-10-21: "use the V1 whip icon when we make whips, for now any icon") - for now
    /// Paddle_V1's inventory icon (SetIcon, called once that icon has rendered). The game reads the icon when it creates a player's Skills.
    /// </summary>
    internal static class WhipSkill
    {
        public const string Identifier = "com.kydldev.WhipItUp.Whips";   // never changes after release: saved in player files
        public const string Name = "Whips";
        static SkillConfig _config;
        public static Skills.SkillType Type { get; private set; } = Skills.SkillType.None;

        /// Expected outcome: Jotunn gave the skill a type (returns that check).
        public static bool Register()
        {
            try
            {
                _config = new SkillConfig
                {
                    Identifier = Identifier,
                    Name = Name,
                    Description = "Skill with whips and floggers. The higher it is, the harder they hit.",
                    IncreaseStep = 1f,
                };
                Type = SkillManager.Instance.AddSkill(_config);
                ModLog.Verbose("WhipSkill: '" + Name + "' registered as skill type " + (int)Type);
            }
            catch (ArgumentException ex) { ModLog.Error("WhipSkill: Jotunn refused the skill config (" + ex.Message + ") - whips will use Clubs"); Type = Skills.SkillType.None; }
            catch (Exception ex) { ModLog.Error("WhipSkill: could not register '" + Name + "' (" + ex.Message + ") - whips will use Clubs"); Type = Skills.SkillType.None; }
            return Type != Skills.SkillType.None;
        }

        /// Sets the skill icon if it has none yet, or when force is true (Whip_V1's icon replaces the placeholder).
        /// Expected outcome: the skill has an icon (returns that check).
        public static bool SetIcon(Sprite icon, string from, bool force)
        {
            if (_config == null || icon == null) { ModLog.Verbose("WhipSkill: icon from " + from + " not set (skill registered " + (_config != null) + ", icon " + (icon != null) + ")"); return _config != null && _config.Icon != null; }
            if (_config.Icon != null && !force) return _config.Icon != null;
            _config.Icon = icon;
            ModLog.Verbose("WhipSkill: icon = " + from + (force ? "" : " (placeholder until Whip_V1)"));
            return _config.Icon == icon;
        }

        /// The skill a whip-family item should train: Whips, or Clubs when the skill could not be registered.
        public static Skills.SkillType ForItems { get { return Type != Skills.SkillType.None ? Type : Skills.SkillType.Clubs; } }
    }
}
