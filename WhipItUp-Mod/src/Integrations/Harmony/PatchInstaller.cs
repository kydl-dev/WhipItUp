using System;
using System.Reflection;
using HarmonyLib;
using KydlCore.Common;

namespace WhipItUp
{
    /// <summary>Applies and removes the mod's Harmony patches (patching lives in Integrations/Harmony).</summary>
    internal static class PatchInstaller
    {
        /// <summary>Applies every [HarmonyPatch] class of this assembly. Expected outcome: true when the patching ran without an exception.</summary>
        internal static bool Apply(string harmonyId, Assembly assembly)
        {
            try
            {
                var harmony = new Harmony(harmonyId);
                harmony.PatchAll(assembly);
                int count = 0;
                foreach (var m in harmony.GetPatchedMethods())
                {
                    count++;
                    ModLog.Verbose("PatchInstaller: patched " + m.DeclaringType + "." + m.Name);
                }
                ModLog.Verbose("PatchInstaller: Harmony patched " + count + " method(s)");
                return true;
            }
            catch (HarmonyException e)
            {
                ModLog.Error("PatchInstaller: Harmony PatchAll failed - sounds, flat upgrade cost, whip hit-stop and whip grab may not work. " + e.GetType().Name + ": " + e.Message);
                return false;
            }
        }
    }
}