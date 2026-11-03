using HarmonyLib;
using Kydl.Common;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// T-019 (D-098 / D-099): Valheim pauses the attacker's animation for 0.15 s whenever a melee hit lands
    /// (Attack.DoMeleeAttack -> Character.FreezeFrame(0.15f) -> Animator.speed ~0). With the whip's long lash that hit-stop looked like
    /// a frame drop, so for a player holding a whip the pause becomes [Whip] HitStop seconds (0 = none). Other weapons keep vanilla.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.FreezeFrame))]
    internal static class WhipHitStopPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Character __instance, ref float duration)
        {
            if (!ModSettings.On) return true;
            var h = __instance as Humanoid;
            var weapon = h != null ? h.GetCurrentWeapon() : null;
            var prefab = weapon != null ? weapon.m_dropPrefab : null;
            if (prefab == null || !prefab.name.StartsWith(WhipItems.FamilyName + "_V")) return true;
            float vanilla = duration;
            duration = HitStopMath.Duration(vanilla, ModSettings.WhipHitStop.Value);
            ModLog.Verbose("Whip: hit-stop " + vanilla.ToString("0.###") + " s -> " + duration.ToString("0.###") + " s (" + prefab.name + ")");
            return duration > 0.0005f;   // 0 = skip the game's pause (and its RPC) entirely
        }
    }
}
