using System.Linq;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>Item names (D-042..D-044) and descriptions (D-045): every one of the 21 items has both, nothing is left over.</summary>
    [TestFixture]
    public class WeaponNamesTests
    {
        static readonly string[] Families = { "Paddle", "Flogger", "Whip" };

        [Test]
        public void EveryItem_HasANameAndADescription()
        {
            foreach (var family in Families)
                for (int rank = 1; rank <= Ladder.Ranks; rank++)
                {
                    Assert.That(WeaponNames.En.ContainsKey(WeaponNames.Key(family, rank)), Is.True, "name of " + family + " V" + rank);
                    Assert.That(WeaponNames.Desc.ContainsKey(WeaponNames.DescKey(family, rank)), Is.True, "description of " + family + " V" + rank);
                    Assert.That(WeaponNames.En[WeaponNames.Key(family, rank)].Trim(), Is.Not.Empty);
                    Assert.That(WeaponNames.Desc[WeaponNames.DescKey(family, rank)].Trim(), Is.Not.Empty);
                }
        }

        [Test]
        public void NoLeftoverTexts_ExactlyOnePerItem()
        {
            int items = Families.Length * Ladder.Ranks;
            Assert.That(WeaponNames.En.Count, Is.EqualTo(items));
            Assert.That(WeaponNames.Desc.Count, Is.EqualTo(items));
            Assert.That(WeaponNames.En.Values.Distinct().Count(), Is.EqualTo(items), "item names are unique");
        }

        [Test]
        public void Descriptions_UseOnlyPlainAscii_SoTheGameFontDrawsThem()
        {
            // curly quotes / dashes are not known to be in the game's fonts (names may carry the user's own letters, e.g. the o-umlaut of Jotun)
            foreach (var kv in WeaponNames.Desc)
                Assert.That(kv.Value.All(c => c >= ' ' && c <= '~'), Is.True, kv.Key + ": " + kv.Value);
        }

        [Test]
        public void MergeInto_KeepsTheUiTexts_AndAddsNamesAndDescriptions()
        {
            var ui = new System.Collections.Generic.Dictionary<string, string> { { "ui_test", "x" } };
            var all = WeaponNames.MergeInto(ui);
            Assert.That(all["ui_test"], Is.EqualTo("x"));
            Assert.That(all.Count, Is.EqualTo(1 + WeaponNames.En.Count + WeaponNames.Desc.Count));
            Assert.That(WeaponNames.MergeInto(null).Count, Is.EqualTo(WeaponNames.En.Count + WeaponNames.Desc.Count));
        }
    }
}
