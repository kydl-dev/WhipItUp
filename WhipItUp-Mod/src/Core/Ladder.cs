using System;
using System.Collections.Generic;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The rank ladder shared by every weapon family (D-005, D-006): 7 ranks, weakest first, rank number = position (V1..V7).
    /// Copied from RidingCropMod (RidingCrop.cs `Tiers` + balance constants; its crops in ladder order are base, V1, V3, V4, V6, V2, V5).
    /// Pure logic, no Unity / Valheim types: unit-tested in WhipItUp-Tests/Unit (LadderTests).
    /// </summary>
    internal static class Ladder
    {
        public const int Ranks = 7;

        // Crop balance (RidingCrop.cs): blunt and bleed compound per rank.
        public const float CropBaseBlunt = 20f;
        public const float CropBaseBleedPerSecond = 10f;
        public const float BluntGrowthPerRank = 0.40f;
        public const float BleedGrowthPerRank = 0.25f;
        public const float BleedSeconds = 5f;
        public const float BaseDurability = 200f;
        public const float DurabilityStepPerRank = 50f;
        public const float UpgradeBluntFractionOfBase = 0.30f;       // +6 blunt per upgrade level, every rank
        public const float UpgradeDurabilityFractionOfBase = 0.20f;  // +40 durability per upgrade level, every rank
        public const int MaxQuality = 4;
        public const float ParryBonus = 2f;
        public const float LastChainDamageMultiplier = 2f;

        // Absolute per-rank combat stats (quality 1, skill 0), same as the crop ladder.
        public static readonly float[] Stagger = { 12f, 25f, 35f, 55f, 95f, 125f, 150f };
        public static readonly float[] Knockback = { 30f, 60f, 80f, 100f, 100f, 100f, 100f };
        public static readonly float[] Stamina = { 6f, 8f, 10f, 12f, 14f, 16f, 18f };
        public static readonly float[] BlockArmor = { 3f, 12f, 21f, 30f, 39f, 57f, 66f };
        public static readonly float[] BlockForce = { 20f, 30f, 30f, 30f, 30f, 30f, 30f };

        /// Crafting station prefab names (Jotunn CraftingStations.* values) + level needed for quality 1, per rank.
        public static readonly string[] Station = { "piece_workbench", "piece_workbench", "forge", "forge", "forge", "blackforge", "blackforge" };
        public static readonly int[] StationLevel = { 1, 2, 1, 2, 3, 1, 2 };

        // Rank metal for the rings (D-062 / D-064; user 2026-10-24 D-105): 6 at quality 1, then a FLAT 2 for every upgrade (q2, q3, q4). The game would
        // scale the upgrade cost (1x / 2x / 4x amountPerLevel, UpgradeAmount), so FlatUpgradePatch makes the metal cost exactly amountPerLevel per upgrade.
        public const int MetalBaseAmount = 6;
        public const int MetalAmountPerLevel = 2;

        /// The rank metal's item prefab ids per rank, V1..V7 (user: Tin, Copper, Bronze, Iron, BlackMetal, Flametal_New, Gold). The first id is the user's;
        /// the next ones are the names the AssetRipper rip uses, tried only when the first is not in the game (D-062: ids are read from the live ObjectDB, not guessed).
        public static readonly string[][] MetalIds =
        {
            new[] { "Tin" },
            new[] { "Copper" },
            new[] { "Bronze" },
            new[] { "Iron" },
            new[] { "BlackMetal" },
            new[] { "Flametal_New", "FlametalNew" },
            new[] { "Gold" },
        };

        /// Ingredients per rank (item prefab id, amount at quality 1): the crop ladder's, plus the rank metal as the last entry (D-062 / D-063: added, nothing removed).
        public static readonly Ingredient[][] Ingredients =
        {
            new[] { new Ingredient("LeatherScraps", 10), new Ingredient("Resin", 2), new Ingredient("Wood", 5), Metal(0) },
            new[] { new Ingredient("DeerHide", 10), new Ingredient("Bronze", 2), new Ingredient("FineWood", 5), Metal(1) },
            new[] { new Ingredient("BjornHide", 10), new Ingredient("SurtlingCore", 2), new Ingredient("RoundLog", 5), Metal(2) },
            new[] { new Ingredient("WolfPelt", 10), new Ingredient("Obsidian", 2), new Ingredient("FineWood", 5), Metal(3) },
            new[] { new Ingredient("LoxPelt", 10), new Ingredient("Tar", 2), new Ingredient("ElderBark", 5), Metal(4) },
            new[] { new Ingredient("AskHide", 10), new Ingredient("CharredBone", 2), new Ingredient("Blackwood", 5), Metal(5) },
            new[] { new Ingredient("MooseHide", 10), new Ingredient("SealHide", 2), new Ingredient("Frostwood", 5), Metal(6) },
        };

        static Ingredient Metal(int index) { return new Ingredient(MetalIds[index][0], MetalBaseAmount, MetalAmountPerLevel, MetalIds[index], true); }

        /// The crop texture each rank's colours come from (documentation / logs only; the colours are baked by the Blender scripts).
        public static readonly string[] CropTexture = { "crop_Albedo", "crop_Albedo_V1", "crop_Albedo_V3", "crop_Albedo_V4", "crop_Albedo_V6", "crop_Albedo_V2", "crop_Albedo_V5" };

        public struct Ingredient
        {
            public readonly string Item;
            public readonly int Amount;
            /// Item ids to try in this order when the first one is not in the game (null = just Item).
            public readonly string[] Candidates;
            readonly int _perLevel;
            /// True = every upgrade costs exactly AmountPerLevel (FlatUpgradePatch), not the game's growing 1x / 2x / 4x.
            public readonly bool FlatUpgrade;
            /// Upgrade cost per level: the explicit value when one was given, else half the base amount, rounded up, at least 1 (crop rule).
            public int AmountPerLevel { get { return _perLevel > 0 ? _perLevel : Math.Max(1, (Amount + 1) / 2); } }
            public Ingredient(string item, int amount) { Item = item; Amount = amount; _perLevel = 0; Candidates = null; FlatUpgrade = false; }
            public Ingredient(string item, int amount, int amountPerLevel, string[] candidates, bool flatUpgrade = false) { Item = item; Amount = amount; _perLevel = amountPerLevel; Candidates = candidates; FlatUpgrade = flatUpgrade; }
            public override string ToString() { return Amount + " " + Item + " (+" + AmountPerLevel + (FlatUpgrade ? " every upgrade)" : "/upgrade)"); }
        }

        /// The item id to put into the recipe: the first of the ingredient's candidates that the game has (exists), else the ingredient's own id.
        /// Expected outcome: a non-empty id.
        public static string Resolve(Ingredient ingredient, Func<string, bool> exists)
        {
            if (ingredient.Candidates != null)
            {
                foreach (var id in ingredient.Candidates)
                {
                    if (exists(id)) return id;
                }
            }
            return ingredient.Item;
        }

        /// How much of an ingredient the game charges at a quality level: a mirror of the game's `Piece.Requirement.GetAmount`
        /// (read from assembly_valheim.dll 1.0.20): quality 1 = amount, then amountPerLevel x (quality - 1) up to quality 3, x (4 + (quality - 4) / 2) from quality 4.
        /// Jotunn's RequirementConfig does not set `m_upgraderResource`, so the base amount is not added on upgrades.
        public static int UpgradeAmount(Ingredient ingredient, int quality)
        {
            if (quality <= 1) return ingredient.Amount;
            if (ingredient.FlatUpgrade) return ingredient.AmountPerLevel;     // FlatUpgradePatch (D-105)
            float factor = quality < 4 ? quality - 1 : 4f + (quality - 4) / 2f;
            return (int)Math.Floor(factor * ingredient.AmountPerLevel);
        }

        /// Rank number 1..7 -> index 0..6. Throws for anything else.
        public static int Index(int rank)
        {
            if (rank < 1 || rank > Ranks) throw new ArgumentOutOfRangeException(nameof(rank), rank, "rank is 1.." + Ranks);
            return rank - 1;
        }

        public static string PrefabName(string family, int rank) { Index(rank); return family + "_V" + rank; }

        public static float CropBlunt(int rank) { return CropBaseBlunt * (float)Math.Pow(1f + BluntGrowthPerRank, Index(rank)); }
        public static float CropBleedPerSecond(int rank) { return CropBaseBleedPerSecond * (float)Math.Pow(1f + BleedGrowthPerRank, Index(rank)); }

        /// Paddle blunt (user 2026-10-21): no bleed; the crop's blunt + ONE bleed tick (damage per second, not the 5 s total).
        public static float PaddleBlunt(int rank) { return CropBlunt(rank) + CropBleedPerSecond(rank); }

        public static float Durability(int rank) { return BaseDurability + DurabilityStepPerRank * Index(rank); }
        public static float BluntPerLevel { get { return CropBaseBlunt * UpgradeBluntFractionOfBase; } }
        public static float DurabilityPerLevel { get { return BaseDurability * UpgradeDurabilityFractionOfBase; } }

        /// Valheim stores stagger as a multiplier on the hit's damage: keep the crop's ABSOLUTE stagger for the given blunt.
        public static float StaggerMultiplier(int rank, float blunt) { return blunt > 0f ? Stagger[Index(rank)] / blunt : 0f; }

        /// One line per rank for logs / the debug command.
        public static IEnumerable<string> Describe(string family, Func<int, float> blunt)
        {
            for (int rank = 1; rank <= Ranks; rank++)
            {
                int i = rank - 1;
                yield return PrefabName(family, rank) + ": blunt " + blunt(rank).ToString("0.#") + " (+" + BluntPerLevel.ToString("0.#") + "/level, max q" + MaxQuality + " "
                    + (blunt(rank) + BluntPerLevel * (MaxQuality - 1)).ToString("0.#") + "), stagger " + Stagger[i] + ", knockback " + Knockback[i] + ", stamina " + Stamina[i]
                    + ", block " + BlockArmor[i] + " (parry x" + ParryBonus + "), durability " + Durability(rank) + " (+" + DurabilityPerLevel + "/level), "
                    + Station[i] + " " + StationLevel[i] + ", " + string.Join(", ", Array.ConvertAll(Ingredients[i], g => g.ToString())) + ", colours of " + CropTexture[i];
            }
        }
    }
}
