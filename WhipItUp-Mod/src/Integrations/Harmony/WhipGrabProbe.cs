using HarmonyLib;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// T-020 (D-101) diagnosis, debug log only: when the local player's whip attack 2 runs its melee hit check, log the nearest
    /// creature in front (distance, angle off the aim, its height) and the attack's ray shape. A 'grabbed' line (GrabPullPatch)
    /// right after = hit; none = miss, and this line says where the creature was.
    /// </summary>
    [HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
    internal static class WhipGrabProbe
    {
        static readonly AccessTools.FieldRef<Attack, Humanoid> CharacterRef = AccessTools.FieldRefAccess<Attack, Humanoid>("m_character");
        static readonly AccessTools.FieldRef<Humanoid, bool> SecondaryRef = AccessTools.FieldRefAccess<Humanoid, bool>("m_currentAttackIsSecondary");

        [HarmonyPrefix]
        static void Prefix(Attack __instance)
        {
            if (!ModSettings.On || !ModSettings.DebugLog.Value || __instance == null) return;
            var who = CharacterRef(__instance);
            if (who == null || who != Player.m_localPlayer || !SecondaryRef(who)) return;
            var w = who.GetCurrentWeapon();
            if (w == null || w.m_dropPrefab == null || !w.m_dropPrefab.name.StartsWith(WhipItems.FamilyName + "_V")) return;
            Character best = null;
            float bestDist = float.MaxValue, bestAngle = 0f;
            Vector3 me = who.transform.position, fwd = who.transform.forward;
            foreach (var c in Character.GetAllCharacters())
            {
                if (c == null || c == who || c.IsDead() || c.IsPlayer()) continue;
                var d = c.transform.position - me;
                d.y = 0f;
                float dist = d.magnitude, angle = Vector3.Angle(fwd, d);
                if (dist > __instance.m_attackRange + 3f || angle > 60f || dist >= bestDist) continue;
                best = c; bestDist = dist; bestAngle = angle;
            }
            string shape = "range " + __instance.m_attackRange + ", angle " + __instance.m_attackAngle + ", height " + __instance.m_attackHeight + ", width " + __instance.m_attackRayWidth
                + ", char heights " + __instance.m_attackHeightChar1 + " / " + __instance.m_attackHeightChar2;
            if (best == null) ModLog.Verbose("Whip: attack 2 hit check (" + shape + ") - no creature within " + (__instance.m_attackRange + 3f) + " m / 60 deg in front");
            else ModLog.Verbose("Whip: attack 2 hit check (" + shape + ") - nearest in front: " + best.name + " at " + bestDist.ToString("0.0") + " m, " + bestAngle.ToString("0") + " deg off the aim, height "
                + best.GetHeight().ToString("0.0") + " m, centre " + (best.GetCenterPoint().y - me.y).ToString("0.0") + " m above your feet"
                + (bestDist <= __instance.m_attackRange && bestAngle <= __instance.m_attackAngle * 0.5f ? " (inside range + cone)" : " (OUTSIDE range or cone)"));
        }
    }
}
