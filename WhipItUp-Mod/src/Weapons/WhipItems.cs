using HarmonyLib;
using Jotunn.Managers;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The Whip family (T-010, T-011, D-034): Whip_V1..V7, clones of the Club (club combo), Whips skill, slash + bleed like the flogger,
    /// longer reach. Lash coiled in the hand while idle / snapping out while attacking (WhipMotion). Attack 2 = lash wrapped around the body of a near enemy + pull (D-036, body wrap D-086).
    /// Hit sound = the user's whip_slap (as is, synced). Whip_V1's icon becomes the Whips skill icon (D-028).
    /// </summary>
    internal sealed class WhipItems : WeaponFamily
    {
        public const string FamilyName = "Whip";
        const string GrabSource = "SpearBronze";      // its secondary 'spear_throw' clip is the throw of attack 2

        public override string Family { get { return FamilyName; } }
        protected override Vector3 Rotation { get { return ModSettings.WhipRotation; } }
        protected override float Grip { get { return ModSettings.WhipGrip.Value; } }
        protected override float Scale { get { return ModSettings.WhipScale.Value; } }
        protected override Sounds.HitSound HitSound { get { return Sounds.HitSound.WhipSlap; } }
        /// Icon (user 2026-10-21): the whip with the lash coiled (also the Whips skill icon, from Whip_V1).
        protected override float IconTiltDegrees { get { return 30f; } }
        protected override System.Action<Transform, Transform[]> PoseForIcon { get { return CoilForIcon; } }

        /// Bends every lash bone around the model's Z axis (the icon camera looks along Z) so the lash winds into an outward spiral.
        static void CoilForIcon(Transform root, Transform[] bones)
        {
            int n = bones.Length;
            for (int i = 0; i < n; i++)
            {
                float angle = 52f * 16f / Mathf.Max(1, n) * (1f - 0.35f * i / Mathf.Max(1, n - 1));   // ~2 loops whatever the bone count; tighter first
                bones[i].rotation = Quaternion.AngleAxis(angle, root.forward) * bones[i].rotation;
            }
        }

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

        /// Attack 2 (user 2026-10-21, back with whip scale 1.5): SpearBronze's secondary (spear_throw clip) turned into a long narrow melee hit;
        /// an enemy it hits gets the lash wrapped around its body and is pulled toward the player (GrabPullPatch).
        protected override bool ConfigureSecondary(ItemDrop.ItemData.SharedData shared, int rank, float main, string name)
        {
            var spear = PrefabManager.Instance.GetPrefab(GrabSource);
            var drop = spear != null ? spear.GetComponent<ItemDrop>() : null;
            var src = drop != null ? drop.m_itemData.m_shared.m_secondaryAttack : null;
            if (src == null || string.IsNullOrEmpty(src.m_attackAnimation)) { ModLog.Error("WhipItems: '" + GrabSource + "' secondary not found - " + name + " has no attack 2"); return src != null; }
            var a = src.Clone();
            a.m_attackType = Attack.AttackType.Horizontal;     // melee hit check instead of throwing the item
            a.m_attackProjectile = null;
            a.m_attackRange = ModSettings.WhipGrabRange.Value;
            a.m_attackAngle = ModSettings.WhipGrabAngle.Value;
            a.m_attackStamina = Ladder.Stamina[Ladder.Index(rank)] * ModSettings.BackhandStaminaFactor.Value;
            a.m_damageMultiplier = ModSettings.WhipGrabDamage.Value;   // T-017 / D-061: 2x attack 1, so the risk of pulling the enemy close pays
            a.m_staggerMultiplier = Ladder.StaggerMultiplier(rank, main);
            a.m_consumeItem = false;
            shared.m_secondaryAttack = a;
            ModLog.Verbose("WhipItems: " + name + " attack 2 = " + GrabSource + " '" + a.m_attackAnimation + "' as melee grab, range " + a.m_attackRange + ", angle " + a.m_attackAngle
                + ", damage x" + a.m_damageMultiplier.ToString("0.##") + ", stamina " + a.m_attackStamina.ToString("0.#"));
            return shared.m_secondaryAttack.m_attackType == Attack.AttackType.Horizontal
                && Mathf.Approximately(shared.m_secondaryAttack.m_damageMultiplier, ModSettings.WhipGrabDamage.Value);
        }
    }

    /// <summary>
    /// Attack 2 of a whip: before the hit is sent to the target's owner, turn its push into a pull toward the attacker and start the
    /// body wrap on the attacker's whip visual. Runs where the attack happens (the attacker's client); the pull travels with the hit.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class GrabPullPatch
    {
        static readonly AccessTools.FieldRef<Humanoid, bool> SecondaryRef = AccessTools.FieldRefAccess<Humanoid, bool>("m_currentAttackIsSecondary");

        [HarmonyPrefix]
        static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null) return;
            var attacker = hit.GetAttacker() as Humanoid;
            if (attacker == null || attacker == __instance || !SecondaryRef(attacker)) return;
            var weapon = attacker.GetCurrentWeapon();
            var prefab = weapon != null ? weapon.m_dropPrefab : null;
            if (prefab == null || !prefab.name.StartsWith(WhipItems.FamilyName + "_V")) return;
            var dir = attacker.transform.position - __instance.transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            hit.m_dir = (dir.normalized + Vector3.up * 0.15f).normalized;
            hit.m_pushForce = ModSettings.WhipPullForce.Value;
            int wraps = 0;
            foreach (var m in attacker.GetComponentsInChildren<WhipMotion>()) { m.StartGrab(__instance); wraps++; }
            ModLog.Verbose("Whip: attack 2 grabbed " + __instance.name + " at " + dist.ToString("0.0") + " m, pull " + hit.m_pushForce + " toward " + attacker.name + " (" + wraps + " lash visual(s))");
        }
    }
}
