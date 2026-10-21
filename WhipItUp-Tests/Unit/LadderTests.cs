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
    }
}
