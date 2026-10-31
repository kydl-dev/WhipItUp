using System.Collections.Generic;
using System.Linq;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Testing
{
    /// <summary>
    /// KydlCore D-084: hooks for DrakesTestLab (WhipItUp-Tests/TestLab/suites/WhipItUp). They read the REAL ObjectDB items, recipes and the Whips skill.
    /// </summary>
    internal static class TestHooks
    {
        static ItemDrop.ItemData.SharedData Shared(string prefab)
        {
            var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = go != null ? go.GetComponent<ItemDrop>() : null;
            if (drop == null) throw new System.Exception("item " + prefab + " not found");
            return drop.m_itemData.m_shared;
        }

        [TestHook("whipitup.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// <summary>The weapon's blunt + slash damage at quality 1 (sum of the damage types).</summary>
        [TestHook("whipitup.damage")]
        static float Damage(string prefab) { var d = Shared(prefab).m_damages; return d.m_blunt + d.m_slash + d.m_pierce; }

        [TestHook("whipitup.slash")]
        static float Slash(string prefab) { return Shared(prefab).m_damages.m_slash; }

        [TestHook("whipitup.blunt")]
        static float Blunt(string prefab) { return Shared(prefab).m_damages.m_blunt; }

        /// <summary>The weapon's skill as text (Clubs / WhipItUp skill id ...).</summary>
        [TestHook("whipitup.skill")]
        static string Skill(string prefab) { return Shared(prefab).m_skillType.ToString(); }

        [TestHook("whipitup.maxQuality")]
        static int MaxQuality(string prefab) { return Shared(prefab).m_maxQuality; }

        /// <summary>True when the ObjectDB has a recipe that crafts the item.</summary>
        [TestHook("whipitup.hasRecipe")]
        static bool HasRecipe(string prefab)
        {
            return ObjectDB.instance.m_recipes.Any(r => r != null && r.m_item != null && r.m_item.name == prefab);
        }

        /// <summary>Crafting station of the recipe (text) and the minimum station level.</summary>
        [TestHook("whipitup.station")]
        static string Station(string prefab)
        {
            var r = ObjectDB.instance.m_recipes.FirstOrDefault(x => x != null && x.m_item != null && x.m_item.name == prefab);
            if (r == null) throw new System.Exception("no recipe for " + prefab);
            return (r.m_craftingStation != null ? r.m_craftingStation.name : "none") + " " + r.m_minStationLevel;
        }

        /// <summary>The recipe's cost to craft, "name:amount,..." at quality 1 (what the game charges).</summary>
        [TestHook("whipitup.cost")]
        static string Cost(string prefab, int quality = 1)
        {
            var r = ObjectDB.instance.m_recipes.FirstOrDefault(x => x != null && x.m_item != null && x.m_item.name == prefab);
            if (r == null) throw new System.Exception("no recipe for " + prefab);
            return string.Join(",", r.m_resources.Where(q => q != null && q.m_resItem != null).Select(q => q.m_resItem.name + ":" + q.GetAmount(quality)).ToArray());
        }

        /// <summary>True when the player's inventory holds the item (any quality).</summary>
        [TestHook("whipitup.owned")]
        static bool Owned(string prefab)
        {
            var inv = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;
            return inv != null && inv.GetAllItems().Any(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefab);
        }

        /// <summary>True when the item in the player's hand is the prefab.</summary>
        [TestHook("whipitup.inHand")]
        static bool InHand(string prefab)
        {
            var p = Player.m_localPlayer;
            if (p == null) return false;
            var f = typeof(Humanoid).GetField("m_rightItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var item = f != null ? f.GetValue(p) as ItemDrop.ItemData : null;
            return item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == prefab;
        }

        /// <summary>Skill level of the weapon's skill for the local player (the Whips skill is registered, level 0 at start).</summary>
        [TestHook("whipitup.skillLevel")]
        static float SkillLevel(string prefab)
        {
            var p = Player.m_localPlayer;
            if (p == null) throw new System.Exception("no player");
            return p.GetSkills().GetSkillLevel(Shared(prefab).m_skillType);
        }

        /// <summary>All prefab names of the families that are on (for a log line).</summary>
        [TestHook("whipitup.weapons")]
        static string Weapons()
        {
            var names = new List<string>();
            foreach (var f in new[] { "Paddle", "Flogger", "Whip" })
                for (int r = 1; r <= 7; r++) { string n = f + "_V" + r; if (ObjectDB.instance.GetItemPrefab(n) != null) names.Add(n); }
            return names.Count + ": " + string.Join(",", names.ToArray());
        }
    }
}
