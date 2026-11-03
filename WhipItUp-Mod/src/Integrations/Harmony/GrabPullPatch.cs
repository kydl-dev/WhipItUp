using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
namespace WhipItUp.Weapons
{
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
