using System.Linq;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The rank ladder (D-005, D-006) and the paddle's blunt rule (crop blunt + one bleed tick, user 2026-10-21).</summary>
    [TestFixture]
    public class LadderTests
    {
        [Test]
        public void SevenRanks_AllTablesHaveSevenEntries()
        {
            Assert.That(Ladder.Ranks, Is.EqualTo(7));
            foreach (var table in new[] { Ladder.Stagger, Ladder.Knockback, Ladder.Stamina, Ladder.BlockArmor, Ladder.BlockForce })
                Assert.That(table.Length, Is.EqualTo(Ladder.Ranks));
            Assert.That(Ladder.Station.Length, Is.EqualTo(Ladder.Ranks));
            Assert.That(Ladder.StationLevel.Length, Is.EqualTo(Ladder.Ranks));
            Assert.That(Ladder.Ingredients.Length, Is.EqualTo(Ladder.Ranks));
            Assert.That(Ladder.CropTexture.Length, Is.EqualTo(Ladder.Ranks));
        }

        [Test]
        public void RankFive_UsesCropV6Texture_UserExample()
        {
            // the user's rule: crop V6 is the 5th crop of the ladder -> our V5
            Assert.That(Ladder.CropTexture[Ladder.Index(5)], Is.EqualTo("crop_Albedo_V6"));
            Assert.That(Ladder.CropTexture, Is.EqualTo(new[] { "crop_Albedo", "crop_Albedo_V1", "crop_Albedo_V3", "crop_Albedo_V4", "crop_Albedo_V6", "crop_Albedo_V2", "crop_Albedo_V5" }));
        }

        [TestCase(1, 30f)]
        [TestCase(2, 40.5f)]
        [TestCase(7, 188.73f)]
        public void PaddleBlunt_IsCropBluntPlusOneBleedTick(int rank, float expected)
        {
            Assert.That(Ladder.PaddleBlunt(rank), Is.EqualTo(expected).Within(0.05f));
            Assert.That(Ladder.PaddleBlunt(rank), Is.EqualTo(Ladder.CropBlunt(rank) + Ladder.CropBleedPerSecond(rank)).Within(0.001f));
        }

        [Test]
        public void StaggerMultiplier_KeepsTheCropsAbsoluteStagger()
        {
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                float blunt = Ladder.PaddleBlunt(rank);
                Assert.That(Ladder.StaggerMultiplier(rank, blunt) * blunt, Is.EqualTo(Ladder.Stagger[rank - 1]).Within(0.001f));
            }
        }

        [Test]
        public void PrefabNames_AreFamilyUnderscoreVNumber()
        {
            Assert.That(Enumerable.Range(1, 7).Select(r => Ladder.PrefabName("Paddle", r)), Is.EqualTo(new[] { "Paddle_V1", "Paddle_V2", "Paddle_V3", "Paddle_V4", "Paddle_V5", "Paddle_V6", "Paddle_V7" }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Ladder.PrefabName("Paddle", 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Ladder.PrefabName("Paddle", 8));
        }

        [Test]
        public void Durability_And_Upgrades_MatchTheCrop()
        {
            Assert.That(Ladder.Durability(1), Is.EqualTo(200f));
            Assert.That(Ladder.Durability(7), Is.EqualTo(500f));
            Assert.That(Ladder.BluntPerLevel, Is.EqualTo(6f).Within(0.001f));
            Assert.That(Ladder.DurabilityPerLevel, Is.EqualTo(40f).Within(0.001f));
            Assert.That(Ladder.Ingredients[0][0].AmountPerLevel, Is.EqualTo(5));
            Assert.That(Ladder.Ingredients[0][1].AmountPerLevel, Is.EqualTo(1));
        }

        [Test]
        public void EveryRecipe_EndsWithItsRankMetal_6BaseAndFlat2PerUpgrade()
        {
            var expected = new[] { "Tin", "Copper", "Bronze", "Iron", "BlackMetal", "Flametal_New", "Gold" };
            for (int i = 0; i < Ladder.Ranks; i++)
            {
                var metal = Ladder.Ingredients[i].Last();
                Assert.That(metal.Item, Is.EqualTo(expected[i]), "rank " + (i + 1));
                Assert.That(metal.Amount, Is.EqualTo(6));
                Assert.That(metal.AmountPerLevel, Is.EqualTo(2));
                Assert.That(metal.FlatUpgrade, Is.True, "D-105: a flat 2 for every upgrade");
                Assert.That(Ladder.Ingredients[i].Length, Is.EqualTo(4), "the three crop ingredients stay (D-062: added, nothing removed)");
            }
        }

        [Test]
        public void TheCropIngredientsKeepTheirHalfAmountUpgradeRule()
        {
            foreach (var recipe in Ladder.Ingredients)
                foreach (var g in recipe.Take(3))
                    Assert.That(g.AmountPerLevel, Is.EqualTo(System.Math.Max(1, (g.Amount + 1) / 2)), g.Item);
        }

        [TestCase(1, 10)]
        [TestCase(2, 5)]
        [TestCase(3, 10)]
        [TestCase(4, 20)]
        public void UpgradeAmount_MirrorsTheGamesRequirementMath(int quality, int expected)
        {
            // Piece.Requirement.GetAmount in assembly_valheim.dll 1.0.20: q1 = amount, q2 = 1x, q3 = 2x, q4 = 4x amountPerLevel (D-065); V1 leather scraps 10 (+5)
            Assert.That(Ladder.UpgradeAmount(Ladder.Ingredients[0][0], quality), Is.EqualTo(expected));
        }

        [TestCase(1, 6)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        public void UpgradeAmount_RankMetalIsFlatPerUpgrade(int quality, int expected)
        {
            // D-105: the rank metal costs 6 to craft, then 2 for every upgrade (FlatUpgradePatch overrides the game's 1x / 2x / 4x)
            for (int i = 0; i < Ladder.Ranks; i++) Assert.That(Ladder.UpgradeAmount(Ladder.Ingredients[i].Last(), quality), Is.EqualTo(expected), "rank " + (i + 1));
        }

        [Test]
        public void Resolve_PicksTheFirstCandidateTheGameHas()
        {
            var flametal = Ladder.Ingredients[5].Last();
            Assert.That(Ladder.Resolve(flametal, id => id == "Flametal_New" || id == "FlametalNew"), Is.EqualTo("Flametal_New"));
            Assert.That(Ladder.Resolve(flametal, id => id == "FlametalNew"), Is.EqualTo("FlametalNew"));
            Assert.That(Ladder.Resolve(flametal, id => false), Is.EqualTo("Flametal_New"), "nothing found: the ingredient's own id (the caller logs the error)");
            Assert.That(Ladder.Resolve(Ladder.Ingredients[0][0], id => false), Is.EqualTo("LeatherScraps"), "no candidates: the ingredient's own id");
        }
    }
}
