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

        /// T-017 (D-061): every whip's attack 2 does Whip.GrabDamage (default 2) x the damage of attack 1 (attack 1 multiplier 1).
        static string World_WhipAttack2Damage()
        {
            if (ObjectDB.instance == null) return "SKIP: ObjectDB not ready";
            float want = ModSettings.WhipGrabDamage.Value;
            var bad = new List<string>();
            int n = 0;
            for (int rank = 1; rank <= 7; rank++)
            {
                string name = Weapons.WhipItems.FamilyName + "_V" + rank;
                var go = ObjectDB.instance.GetItemPrefab(name);
                var sh = go != null && go.GetComponent<ItemDrop>() != null ? go.GetComponent<ItemDrop>().m_itemData.m_shared : null;
                if (sh == null || sh.m_attack == null || sh.m_secondaryAttack == null) { bad.Add(name + " missing"); continue; }
                n++;
                float ratio = sh.m_secondaryAttack.m_damageMultiplier / Mathf.Max(0.0001f, sh.m_attack.m_damageMultiplier);
                if (Mathf.Abs(ratio - want) > 0.01f) bad.Add(name + " x" + ratio.ToString("0.##"));
            }
            string r = bad.Count == 0 ? "PASS: " + n + " whip(s), attack 2 = x" + want.ToString("0.##") + " of attack 1 (Whip.GrabDamage)"
                : "FAIL: attack 2 / attack 1 damage should be x" + want.ToString("0.##") + ": " + string.Join(", ", bad.ToArray());
            ModLog.Verbose("InGameTests: World_WhipAttack2Damage -> " + r);
            return r;
        }

        static readonly HarmonyLib.AccessTools.FieldRef<Character, Animator> AnimatorRef = HarmonyLib.AccessTools.FieldRefAccess<Character, Animator>("m_animator");

        /// T-023: the 4 body clips (bundle) and the 4 keyed lash clips are loaded.
        static string World_WhipClipsLoaded()
        {
            var lash = Weapons.WhipAnimations.Lash;
            string r = WhipAnimChecks.Clips(Weapons.WhipAnimations.BodyClipsLoaded, Weapons.WhipAnimations.BodyClipsExpected, lash != null ? lash.Count : 0, Weapons.WhipAnimations.LashClipsExpected);
            ModLog.Verbose("InGameTests: World_WhipClipsLoaded -> " + r);
            return r;
        }

        /// T-023: with a whip in hand the local player's animator plays the whip clips (all slots); without one it has the game's controller back.
        static string World_WhipControllerState()
        {
            var player = Player.m_localPlayer;
            string r;
            if (player == null) r = "SKIP: no local player";
            else
            {
                var a = AnimatorRef(player);
                var w = player.GetCurrentWeapon();
                bool holds = w != null && w.m_dropPrefab != null && w.m_dropPrefab.name.StartsWith(Weapons.WhipItems.FamilyName + "_V");
                r = a == null ? "SKIP: the player has no animator yet"
                    : WhipAnimChecks.Controller(ModSettings.WhipCustomAnimations.Value, holds, Weapons.WhipAnimations.IsAttached(a), a.runtimeAnimatorController is AnimatorOverrideController,
                        Weapons.WhipAnimations.ReplacedSlots(a), Weapons.WhipAnimations.BodyClipsExpected);
            }
            ModLog.Verbose("InGameTests: World_WhipControllerState -> " + r);
            return r;
        }

        /// T-023: no controller swap this session lost an animator layer state (TEST-020: lying on the floor after a weapon switch).
        static string World_WhipSwapsKeptState()
        {
            string r = WhipAnimChecks.Swaps(Weapons.WhipAnimations.Swaps, Weapons.WhipAnimations.SwapsLosingState, Weapons.WhipAnimations.LastSwapLoss);
            ModLog.Verbose("InGameTests: World_WhipSwapsKeptState -> " + r);
            return r;
        }

        /// D-105: every weapon's rank metal costs Ladder.MetalBaseAmount to craft and a flat Ladder.MetalAmountPerLevel for each upgrade
        /// (asked through the game's own Piece.Requirement.GetAmount, so FlatUpgradePatch is part of the check).
        static string World_MetalCostFlat()
        {
            if (ObjectDB.instance == null) return "SKIP: ObjectDB not ready";
            int found = Weapons.FlatUpgradePatch.Scan(), checkedN = 0;
            var bad = new List<string>();
            foreach (var r in ObjectDB.instance.m_recipes)
            {
                if (r == null || r.m_item == null || !Expected().Contains(r.m_item.name)) continue;
                foreach (var req in r.m_resources)
                {
                    if (req == null || req.m_resItem == null || !Weapons.Ladder.MetalIds.Any(ids => ids.Contains(req.m_resItem.name))) continue;
                    checkedN++;
                    string got = req.GetAmount(1) + "/" + req.GetAmount(2) + "/" + req.GetAmount(3) + "/" + req.GetAmount(4);
                    string want = Weapons.Ladder.MetalBaseAmount + "/" + Weapons.Ladder.MetalAmountPerLevel + "/" + Weapons.Ladder.MetalAmountPerLevel + "/" + Weapons.Ladder.MetalAmountPerLevel;
                    if (got != want) bad.Add(r.m_item.name + " " + req.m_resItem.name + " " + got);
                }
            }
            string r2 = checkedN == 0 ? "FAIL: no rank metal found in the WhipItUp recipes"
                : bad.Count == 0 ? "PASS: " + checkedN + " recipe(s), metal q1..q4 = " + Weapons.Ladder.MetalBaseAmount + "/" + Weapons.Ladder.MetalAmountPerLevel + "/" + Weapons.Ladder.MetalAmountPerLevel + "/" + Weapons.Ladder.MetalAmountPerLevel + " (" + found + " flat requirement(s))"
                : "FAIL: " + bad.Count + " of " + checkedN + " wrong (q1/q2/q3/q4): " + string.Join(", ", bad.Take(5).ToArray());
            ModLog.Verbose("InGameTests: World_MetalCostFlat -> " + r2);
            return r2;
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
