using System.Collections.Generic;
using System.Linq;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  run when the main menu is reached;   World_*  run 5 s after a world is loaded, and again whenever the user presses
    ///   GameProbe's RetestKey (PageDown) - e.g. after equipping a whip for the coil test.
    /// Each returns "PASS: ..." / "FAIL: ..." / "SKIP: ..." with the numbers (units) that decided it. Read-only: nothing is changed.
    /// Note: outside namespace WhipItUp.Weapons write `Weapons.Ladder` (the game has its own global `Ladder`, MISTAKES 2026-10-22).
    /// </summary>
    internal static class InGameTests
    {
        static readonly string[] Families = { "Paddle", "Flogger", Weapons.WhipItems.FamilyName };

        static string Startup_ConfigBound()
        {
            if (ModSettings.EnableAll == null || ModSettings.DebugLog == null) return "FAIL: ModSettings.Bind did not run (EnableAll / DebugLog null)";
            string r = ModSettings.On ? "PASS: EnableAll on, menu key " + ModSettings.MenuKey.Value + ", debug log " + ModSettings.DebugLog.Value : "SKIP: EnableAll is off in the config";
            ModLog.Verbose("InGameTests: Startup_ConfigBound -> " + r);
            return r;
        }

        static string Startup_BundleLoaded()
        {
            var b = Plugin.Bundle;
            string r = b != null ? "PASS: bundle '" + Plugin.BundleName + "' with " + b.GetAllAssetNames().Length + " asset(s)" : "FAIL: Plugin.Bundle is null - no weapon models / sounds";
            ModLog.Verbose("InGameTests: Startup_BundleLoaded -> " + r);
            return r;
        }

        static string Startup_WhipSkillRegistered()
        {
            var t = Weapons.WhipSkill.Type;
            string r = t != Skills.SkillType.None ? "PASS: skill type " + (int)t : "FAIL: the Whips skill is not registered (items fall back to Clubs)";
            ModLog.Verbose("InGameTests: Startup_WhipSkillRegistered -> " + r);
            return r;
        }

        /// Every family x rank prefab (3 x 7 = 21) is in ObjectDB.
        static string World_AllWeaponsInObjectDB()
        {
            if (ObjectDB.instance == null) return "SKIP: ObjectDB not ready";
            var missing = Expected().Where(n => ObjectDB.instance.GetItemPrefab(n) == null).ToList();
            string r = Result("ObjectDB", missing);
            ModLog.Verbose("InGameTests: World_AllWeaponsInObjectDB -> " + r);
            return r;
        }

        /// Every family x rank prefab is in ZNetScene (needed to drop / sync it).
        static string World_AllWeaponsInZNetScene()
        {
            if (ZNetScene.instance == null) return "SKIP: ZNetScene not ready";
            var missing = Expected().Where(n => ZNetScene.instance.GetPrefab(n) == null).ToList();
            string r = Result("ZNetScene", missing);
            ModLog.Verbose("InGameTests: World_AllWeaponsInZNetScene -> " + r);
            return r;
        }

        /// The whip in the local player's hand, coiled: bones follow the coil (mismatch ~0) and bend by the chord angle (WhipMotion.LogCoilHealth).
        static string World_WhipCoilHealth()
        {
            var player = Player.m_localPlayer;
            if (player == null) return "SKIP: no local player";
            var held = Weapons.WhipMotion.Live.FirstOrDefault(w => w != null && w.Holder == player);
            string r;
            if (held == null) r = "SKIP: no whip in the hand - equip one, stand still 2 s, press PageDown (GameProbe re-runs the tests)";
            else if (held.Mode != "coiled") r = "SKIP: whip not coiled (mode '" + held.Mode + "') - stand still and press PageDown";
            else if (!held.LastCoilOk.HasValue || Time.time - held.LastCoilAt > 6f) r = "SKIP: no coil check in the last 6 s (it runs every 3 s while coiled) - press PageDown again";
            else r = (held.LastCoilOk.Value ? "PASS: " : "FAIL: ") + held.LastCoilDetail;
            ModLog.Verbose("InGameTests: World_WhipCoilHealth -> " + r);
            return r;
        }

        static IEnumerable<string> Expected()
        {
            foreach (var f in Families)
                for (int rank = 1; rank <= Weapons.Ladder.Ranks; rank++) yield return Weapons.Ladder.PrefabName(f, rank);
        }

        static string Result(string where, List<string> missing)
        {
            int expected = Families.Length * Weapons.Ladder.Ranks;
            return missing.Count == 0 ? "PASS: all " + expected + " weapons (" + string.Join(", ", Families) + " V1-V" + Weapons.Ladder.Ranks + ") in " + where
                : "FAIL: " + missing.Count + " of " + expected + " missing in " + where + ": " + string.Join(", ", missing.ToArray());
        }
    }
}
