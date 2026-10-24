using System.Collections.Generic;
using HarmonyLib;
using Kydl.Common;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// D-105 (user 2026-10-24): the rank metal of every WhipItUp weapon costs a FLAT Ladder.MetalAmountPerLevel (2) for each upgrade
    /// (quality 2, 3 and 4). The game's Piece.Requirement.GetAmount would charge 1x / 2x / 4x amountPerLevel, so for the metal
    /// requirement of our recipes the result becomes amountPerLevel. The other ingredients keep the game's rule.
    /// Our requirements are found by scanning ObjectDB's recipes once per ObjectDB (the game makes new Requirement objects per world).
    /// </summary>
    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    internal static class FlatUpgradePatch
    {
        static readonly HashSet<Piece.Requirement> Flat = new HashSet<Piece.Requirement>();
        static ObjectDB _scanned;
        static int _scannedRecipes = -1;

        [HarmonyPostfix]
        static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (qualityLevel <= 1 || !ModSettings.On) return;
            Scan();
            if (!Flat.Contains(__instance)) return;
            int game = __result;
            __result = __instance.m_amountPerLevel;
            if (game != __result) ModLog.Verbose("FlatUpgrade: " + (__instance.m_resItem != null ? __instance.m_resItem.name : "?") + " at quality " + qualityLevel + ": " + game + " -> " + __result + " (flat per upgrade)");
        }

        /// Collects the metal requirement of every WhipItUp recipe. Expected outcome: the number of requirements found (21 with all weapons).
        internal static int Scan()
        {
            var db = ObjectDB.instance;
            if (db == null || db.m_recipes == null) return Flat.Count;
            if (db == _scanned && db.m_recipes.Count == _scannedRecipes) return Flat.Count;
            Flat.Clear();
            foreach (var r in db.m_recipes)
            {
                if (r == null || r.m_item == null || r.m_resources == null) continue;
                int rank = RankOf(r.m_item.name);
                if (rank < 1) continue;
                var metals = Ladder.MetalIds[Ladder.Index(rank)];
                foreach (var req in r.m_resources)
                    if (req != null && req.m_resItem != null && System.Array.IndexOf(metals, req.m_resItem.name) >= 0) Flat.Add(req);
            }
            _scanned = db;
            _scannedRecipes = db.m_recipes.Count;
            ModLog.Verbose("FlatUpgrade: " + Flat.Count + " rank-metal requirement(s) of WhipItUp recipes cost a flat " + Ladder.MetalAmountPerLevel + " per upgrade (" + db.m_recipes.Count + " recipes scanned)");
            return Flat.Count;
        }

        /// "Whip_V3" -> 3 for our three families, else 0.
        static int RankOf(string prefab)
        {
            foreach (var fam in new[] { "Paddle", "Flogger", WhipItems.FamilyName })
            {
                string pre = fam + "_V";
                int n;
                if (prefab.StartsWith(pre) && int.TryParse(prefab.Substring(pre.Length), out n) && n >= 1 && n <= Ladder.Ranks) return n;
            }
            return 0;
        }
    }
}
