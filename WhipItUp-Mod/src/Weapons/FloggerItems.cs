using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The Flogger family (T-008, T-009): Flogger_V1..V7, clones of the vanilla Club, Whips skill (D-007), club combo + MaceEldner attack 2
    /// (user: "for now club combo + mace 2nd"), skinned falls driven by FallChains. Damage (user 2026-10-21): the crop numbers as
    /// SLASH + the crop's bleed (Bleed status effect, one per rank, never stacking).
    /// </summary>
    internal sealed class FloggerItems : WeaponFamily
    {
        public override string Family { get { return "Flogger"; } }
        protected override string Description { get { return "A wolf-crested flogger of braided leather falls. Placeholder text until the names are chosen."; } }
        protected override Vector3 Rotation { get { return ModSettings.FloggerRotation; } }
        protected override float Grip { get { return ModSettings.FloggerGrip.Value; } }
        protected override float Scale { get { return ModSettings.FloggerScale.Value; } }
        protected override Sounds.HitSound HitSound { get { return Sounds.HitSound.FloggerSlap; } }
        protected override float MainDamage(int rank) { return Ladder.CropBlunt(rank); }

        protected override bool ApplyDamage(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            float slash = Ladder.CropBlunt(rank);
            float bleed = Ladder.CropBleedPerSecond(rank);
            shared.m_damages = new HitData.DamageTypes { m_slash = slash };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = Ladder.BluntPerLevel };
            var se = Bleed.Create(name, bleed, Ladder.BleedSeconds);
            shared.m_attackStatusEffect = se;
            shared.m_attackStatusEffectChance = se != null ? 1f : 0f;
            ModLog.Verbose("FloggerItems: " + name + " slash " + slash.ToString("0.#") + " + bleed " + bleed.ToString("0.#") + "/s for " + Ladder.BleedSeconds + " s (effect " + (se != null ? se.name : "none") + ")");
            return Mathf.Approximately(shared.m_damages.m_slash, slash) && se != null;
        }

        protected override void ApplyExtras(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            shared.m_skillType = WhipSkill.ForItems;
            ModLog.Verbose("FloggerItems: " + name + " skill " + shared.m_skillType);
        }
    }
}
