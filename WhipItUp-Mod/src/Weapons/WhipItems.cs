using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The Whip family (T-010, T-011, D-034): Whip_V1..V7, clones of the Club (club combo), Whips skill, slash + bleed like the flogger,
    /// longer reach. Lash coiled in the hand while idle / snapping out while attacking (WhipMotion). No attack 2 (user, D-035).
    /// Hit sound = the user's whip_slap (as is, synced). Whip_V1's icon becomes the Whips skill icon (D-028).
    /// </summary>
    internal sealed class WhipItems : WeaponFamily
    {
        public const string FamilyName = "Whip";

        public override string Family { get { return FamilyName; } }
        protected override string Description { get { return "A wolf-crested bullwhip with a long braided lash. Placeholder text until the names are chosen."; } }
        protected override Vector3 Rotation { get { return ModSettings.WhipRotation; } }
        protected override float Grip { get { return ModSettings.WhipGrip.Value; } }
        protected override float Scale { get { return ModSettings.WhipScale.Value; } }
        protected override Sounds.HitSound HitSound { get { return Sounds.HitSound.WhipSlap; } }
        protected override float MainDamage(int rank) { return Ladder.CropBlunt(rank); }
        protected override void DecorateVisual(GameObject visual) { visual.AddComponent<WhipMotion>(); }
        protected override void OnFirstIcon(Sprite icon) { WhipSkill.SetIcon(icon, "Whip_V1", true); }

        protected override bool ApplyDamage(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            float slash = Ladder.CropBlunt(rank);
            float bleed = Ladder.CropBleedPerSecond(rank);
            shared.m_damages = new HitData.DamageTypes { m_slash = slash };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = Ladder.BluntPerLevel };
            var se = Bleed.Create(name, bleed, Ladder.BleedSeconds);
            shared.m_attackStatusEffect = se;
            shared.m_attackStatusEffectChance = se != null ? 1f : 0f;
            shared.m_attack.m_attackRange = ModSettings.WhipReach.Value;
            ModLog.Verbose("WhipItems: " + name + " slash " + slash.ToString("0.#") + " + bleed " + bleed.ToString("0.#") + "/s, reach " + shared.m_attack.m_attackRange);
            return Mathf.Approximately(shared.m_damages.m_slash, slash) && se != null;
        }

        protected override void ApplyExtras(ItemDrop.ItemData.SharedData shared, int rank, string name) { shared.m_skillType = WhipSkill.ForItems; }

        /// No attack 2 (user 2026-10-21: "remove 2nd attack, the whip seems too short for that"). Expected outcome: no secondary.
        protected override bool ConfigureSecondary(ItemDrop.ItemData.SharedData shared, int rank, float main, string name)
        {
            shared.m_secondaryAttack = new Attack();             // an Attack without an animation = no secondary attack
            ModLog.Verbose("WhipItems: " + name + " has no attack 2");
            return string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation);
        }
    }
}
