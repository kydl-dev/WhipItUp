using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The Paddle family (T-004..T-006): Paddle_V1..V7, clones of the vanilla Club (Clubs skill, club combo, whoosh + slap sounds).
    /// Damage (user 2026-10-21): no bleed; blunt = crop blunt + one crop bleed tick; the crop's absolute stagger.
    /// Attack 2 = MaceEldner's secondary with the back-handed wrist roll (D-026, BackhandRoll).
    /// </summary>
    internal sealed class PaddleItems : WeaponFamily
    {
        public override string Family { get { return "Paddle"; } }
        protected override string Description { get { return "A studded leather paddle with a wolf-crest pommel. Placeholder text until the names are chosen."; } }
        protected override Vector3 Rotation { get { return ModSettings.PaddleRotation; } }
        protected override float Grip { get { return ModSettings.PaddleGrip.Value; } }
        protected override float Scale { get { return ModSettings.PaddleScale.Value; } }
        protected override float MainDamage(int rank) { return Ladder.PaddleBlunt(rank); }

        protected override bool ApplyDamage(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            float clubBlunt = shared.m_damages.m_blunt;
            float blunt = Ladder.PaddleBlunt(rank);
            shared.m_damages = new HitData.DamageTypes { m_blunt = blunt };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = Ladder.BluntPerLevel };
            shared.m_attackStatusEffect = null;     // no bleed on the paddle (user)
            ModLog.Verbose("PaddleItems: " + name + " blunt " + blunt.ToString("0.#") + " (Club " + clubBlunt.ToString("0.#") + ", crop " + Ladder.CropBlunt(rank).ToString("0.#")
                + " + bleed tick " + Ladder.CropBleedPerSecond(rank).ToString("0.#") + ")");
            return Mathf.Approximately(shared.m_damages.m_blunt, blunt);
        }

        // placeholder skill icon until Whip_V1 exists (D-028)
        protected override void OnFirstIcon(Sprite icon) { WhipSkill.SetIcon(icon, "Paddle_V1", false); }
    }
}
