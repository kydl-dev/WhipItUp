using Kydl.Common;
using Kydl.Core.Features;
using WhipItUp.Weapons;

namespace WhipItUp.Features.Skill.Infrastructure
{
    /// <summary>The Whips skill, shared by the flogger and the whip. Gate: ModSettings.SkillOn (flogger or whip on). Registered once, before the game creates the player's Skills.</summary>
    internal sealed class SkillFeature : IFeature
    {
        internal const string FeatureId = "skill";
        static bool _registered;
        internal static bool Running { get; private set; }
        public string Id { get { return FeatureId; } }
        public bool IsEnabled() { return ModSettings.SkillOn; }
        public bool Start()
        {
            if (!_registered) { WhipSkill.Register(); _registered = true; }
            Running = true;
            ModLog.Verbose("SkillFeature: started");
            return Running;
        }
        public bool Stop() { Running = false; ModLog.Verbose("SkillFeature: stopped (the skill stays until the next game start)"); return !Running; }
    }
}