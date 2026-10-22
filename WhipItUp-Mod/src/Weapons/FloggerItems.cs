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
        protected override Vector3 Rotation { get { return ModSettings.FloggerRotation; } }
        protected override float Grip { get { return ModSettings.FloggerGrip.Value; } }
        protected override float Scale { get { return ModSettings.FloggerScale.Value; } }
        protected override Sounds.HitSound HitSound { get { return Sounds.HitSound.FloggerSlap; } }
        /// Icon (user 2026-10-21: "like the floggers hang in game"): handle almost level (pommel left, slightly higher), the falls bend
        /// at the collar and hang straight down in a tight bundle. Jotunn's camera: screen-right = world -X, up = +Y.
        const float IconHandleAngle = 110f;      // turns the model's +Y (pommel -> collar) to screen-right, 20 degrees below level
        protected override Quaternion IconRotation { get { return Quaternion.Euler(0f, 0f, IconHandleAngle); } }
        protected override System.Action<Transform, Transform[]> PoseForIcon { get { return HangForIcon; } }

        /// Turns the first bone of every fall chain so the chain points straight down in the icon (screen-down = -Y after IconRotation);
        /// the rest of each chain follows, a small per-chain spread keeps it from looking like one plank.
        static void HangForIcon(Transform root, Transform[] bones)
        {
            var down = Quaternion.Inverse(Quaternion.Euler(0f, 0f, IconHandleAngle)) * Vector3.down;   // screen-down in the model's space
            int bent = 0;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                if (!b.name.EndsWith("_0")) continue;           // fall_KK_0 = first bone of chain KK; the rest follow it
                var child = b.childCount > 0 ? b.GetChild(0) : null;
                if (child == null) continue;
                var dir = child.position - b.position;
                var spread = Quaternion.AngleAxis((bent % 5 - 2) * 2.5f, root.forward) * down;
                b.rotation = Quaternion.FromToRotation(dir, spread) * b.rotation;
                bent++;
            }
            ModLog.Verbose("FloggerItems: icon pose - " + bent + " fall chain(s) hanging straight down from the collar");
        }

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
