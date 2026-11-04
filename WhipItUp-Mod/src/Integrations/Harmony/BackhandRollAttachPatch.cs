using HarmonyLib;
using KydlCore.Common;
namespace WhipItUp.Weapons;

/// Puts a BackhandRoll on every Player when it is created (local and remote).
[HarmonyPatch(typeof(Player), "Awake")]
internal static class BackhandRollAttachPatch
{
    [HarmonyPostfix]
    static void Postfix(Player __instance)
    {
        if (__instance.GetComponent<BackhandRoll>() == null) __instance.gameObject.AddComponent<BackhandRoll>();
        ModLog.Verbose("BackhandRoll: added to a player object (" + __instance.name + ")");
    }
}
