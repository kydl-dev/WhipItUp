using System.Collections.Generic;
using System.Linq;
using WhipItUp.Ui;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The menu's weapon list (T-015): 21 entries with the user's names and descriptions, in family / rank order.</summary>
    [TestFixture]
    public class WeaponCatalogTests
    {
        static string Text(string key)
        {
            string s;
            if (WeaponNames.En.TryGetValue(key, out s)) return s;
            return WeaponNames.Desc.TryGetValue(key, out s) ? s : key;
        }

        [Test]
        public void Builds_AllItems_FamilyByFamily_WeakestFirst()
        {
            var list = WeaponCatalog.Build(Text);
            Assert.That(list.Count, Is.EqualTo(WeaponSelection.Families.Length * Ladder.Ranks));
            Assert.That(list.Select(w => w.Family + w.Rank).Distinct().Count(), Is.EqualTo(list.Count), "no weapon twice");
            Assert.That(list[0].Family + list[0].Rank, Is.EqualTo("Paddle1"));
            Assert.That(list[Ladder.Ranks].Family + list[Ladder.Ranks].Rank, Is.EqualTo("Flogger1"));
            Assert.That(list.Last().Family + list.Last().Rank, Is.EqualTo("Whip" + Ladder.Ranks));
        }

        [Test]
        public void EveryWeapon_CarriesTheUsersNameAndDescription()
        {
            foreach (var w in WeaponCatalog.Build(Text))
            {
                Assert.That(w.Name, Is.EqualTo(WeaponNames.En[WeaponNames.Key(w.Family, w.Rank)]), w.ToString());
                Assert.That(w.Description, Is.EqualTo(WeaponNames.Desc[WeaponNames.DescKey(w.Family, w.Rank)]), w.ToString());
            }
        }

        [Test]
        public void ATextThatIsStillItsKey_IsLogged()
        {
            var lines = new List<string>();
            WeaponCatalog.Build(k => k.Contains("whip_v3") ? k : Text(k), lines.Add);
            Assert.That(lines.Count(l => l.Contains("no text for")), Is.EqualTo(2), string.Join(" | ", lines));
        }

        [Test]
        public void AMissingText_DoesNotThrow()
        {
            var list = WeaponCatalog.Build(k => null);
            Assert.That(list.Count, Is.EqualTo(WeaponSelection.Families.Length * Ladder.Ranks));
            Assert.That(list.All(w => w.Name == "" && w.Description == ""), Is.True);
        }

        [Test]
        public void TheMenuList_FeedsTheSelection()
        {
            var s = new WeaponSelection(WeaponCatalog.Build(Text));
            s.SetFamily("Whip");
            s.SetRank(7);
            Assert.That(s.Current.Name, Is.EqualTo(WeaponNames.En["whipitup_whip_v7"]));
        }

        static WeaponStats Stats(string family, int rank)
        {
            var s = new WeaponStats { Family = family, Rank = rank, Skill = "Clubs", MaxQuality = Ladder.MaxQuality, Durability = 200f, Station = "Workbench", StationLevel = 1 };
            s.Damage.Add(new WeaponStats.DamageLine("blunt", 30f, 6f));
            s.Attack1.Stamina = 6f;
            return s;
        }

        [Test]
        public void WithStats_EveryWeaponHasTheFiveDetailGroups()
        {
            var log = new List<string>();
            var list = WeaponCatalog.Build(Text, log.Add, Stats);
            Assert.That(list.All(w => w.Groups.Count == 5), Is.True, string.Join(", ", list.Where(w => w.Groups.Count != 5).Select(w => w.ToString()).ToArray()));
            Assert.That(log.Any(l => l.Contains(list.Count + " with details")), Is.True, string.Join(" | ", log.ToArray()));
        }

        [Test]
        public void WithoutAStatsProvider_NoWeaponHasDetails()
        {
            Assert.That(WeaponCatalog.Build(Text).All(w => w.Groups.Count == 0), Is.True);
        }

        [Test]
        public void AWeaponWithoutStats_IsLogged_AndKeepsItsNameAndDescription()
        {
            var log = new List<string>();
            var list = WeaponCatalog.Build(Text, log.Add, (f, r) => f == "Whip" && r == 3 ? null : Stats(f, r));
            var whip3 = list.Single(w => w.Family == "Whip" && w.Rank == 3);
            Assert.That(whip3.Groups, Is.Empty);
            Assert.That(whip3.Name, Is.EqualTo(WeaponNames.En["whipitup_whip_v3"]));
            Assert.That(whip3.Description, Is.EqualTo(WeaponNames.Desc["whipitup_whip_v3_desc"]));
            Assert.That(list.Count(w => w.Groups.Count == 5), Is.EqualTo(list.Count - 1));
            Assert.That(log.Any(l => l.Contains("no stats for") && l.Contains("Whip V3")), Is.True, string.Join(" | ", log.ToArray()));
        }
    }
}
