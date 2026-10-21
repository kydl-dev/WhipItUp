using HarmonyLib;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Sounds
{
    /// <summary>
    /// The game creates a weapon's hit effect once per object a swing hits, so hitting two targets (or a target + the ground) would play
    /// the slap twice at once. Before EffectList.Create runs a list holding our slap, the slap entry is switched off when a slap played
    /// within SameSwingSeconds. Logs whoosh / slap requests with the debug log on. (Ported from RidingCropMod.)
    /// </summary>
    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    internal static class SlapOncePerSwingPatch
    {
        const float SameSwingSeconds = 0.25f;
        static float _lastSlap = -100f;

        [HarmonyPrefix]
        static void Prefix(EffectList __instance)
        {
            var entries = __instance != null ? __instance.m_effectPrefabs : null;
            var slap = WeaponSounds.SlapPrefab;
            var whoosh = WeaponSounds.WhooshPrefab;
            if (entries == null || (slap == null && whoosh == null)) return;
            foreach (var e in entries)
            {
                if (e == null || e.m_prefab == null) continue;
                if (e.m_prefab == whoosh) { ModLog.Verbose("Sound: frame " + Time.frameCount + " whoosh requested"); continue; }
                if (e.m_prefab != slap) continue;
                float now = Time.time;
                bool duplicate = now >= _lastSlap && now - _lastSlap < SameSwingSeconds;
                e.m_enabled = !duplicate;                    // EffectList.Create skips disabled entries
                if (!duplicate) _lastSlap = now;
                ModLog.Verbose("Sound: frame " + Time.frameCount + (duplicate ? " duplicate slap of the same swing dropped" : " slap requested"));
            }
        }
    }
}
