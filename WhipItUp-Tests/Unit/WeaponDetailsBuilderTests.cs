
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WhipItUp.Ui;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The Details tab rows (T-015 S3): quality 1 beside max quality, the ladder's numbers, groups, texts, culture independence.</summary>
    [TestFixture]
    public class WeaponDetailsBuilderTests
    {
        static string Text(string key) { return UiText.English(key); }

        /// Stats the way the reader would produce them for a paddle (blunt only, no bleed), with the ladder's numbers.
        static WeaponStats PaddleStats(int rank, bool attack2 = false)
        {
            int i = Ladder.Index(rank);
            float blunt = Ladder.PaddleBlunt(rank);
            var s = new WeaponStats
            {
                Family = "Paddle", Rank = rank, Skill = "Clubs", Weight = 2f, OneHanded = true, MaxQuality = Ladder.MaxQuality,
                AttackForce = Ladder.Knockback[i], BlockPower = Ladder.BlockArmor[i], ParryBonus = Ladder.ParryBonus, DeflectionForce = Ladder.BlockForce[i],
                Durability = Ladder.Durability(rank), DurabilityPerLevel = Ladder.DurabilityPerLevel, Station = "Workbench", StationLevel = Ladder.StationLevel[i],
            };
            s.Damage.Add(new WeaponStats.DamageLine("blunt", blunt, Ladder.BluntPerLevel));
            s.Attack1.StaggerMultiplier = Ladder.StaggerMultiplier(rank, blunt);
            s.Attack1.Stamina = Ladder.Stamina[i];
            s.Attack1.Range = 2f;
            if (attack2) s.Attack2 = new WeaponStats.AttackLine { DamageMultiplier = 2.5f, ForceMultiplier = 2f, StaggerMultiplier = s.Attack1.StaggerMultiplier * 2f, Stamina = 10f, Range = 2.2f };
            foreach (var g in Ladder.Ingredients[i]) s.Ingredients.Add(new WeaponStats.IngredientLine(g.Item, g.Amount, g.AmountPerLevel));
            return s;
        }

        static WeaponInfo Filled(WeaponStats s, List<string> log = null)
        {
            var info = new WeaponInfo(s.Family, s.Rank, "Name", "Desc");
            WeaponDetailsBuilder.Fill(info, s, Text, log == null ? (System.Action<string>)null : log.Add);
            return info;
        }

        static DetailRow Row(WeaponInfo info, string group, string label)
        {
            var g = info.Groups.FirstOrDefault(x => x.Title == group);
            Assert.That(g, Is.Not.Null, "group '" + group + "' in " + string.Join(", ", info.Groups.Select(x => x.Title).ToArray()));
            var r = g.Rows.FirstOrDefault(x => x.Label == label);
            Assert.That(r, Is.Not.Null, "row '" + label + "' in group '" + group + "': " + string.Join(", ", g.Rows.Select(x => x.Label).ToArray()));
            return r;
        }

        [Test]
        public void ANormalWeapon_HasFiveGroups_InDisplayOrder()
        {
            var info = new WeaponInfo("Paddle", 1, "N", "D");
            bool ok = WeaponDetailsBuilder.Fill(info, PaddleStats(1), Text);
            Assert.That(ok, Is.True);
            Assert.That(info.Groups.Select(g => g.Title).ToArray(), Is.EqualTo(new[] { "General", "Attack 1", "Defence", "Durability", "Crafting" }));
        }

        [Test]
        public void AWeaponWithAttack2_HasSixGroups_AndAnEffectLinePerFamily()
        {
            var paddle = Filled(PaddleStats(3, true));
            Assert.That(paddle.Groups.Select(g => g.Title).ToArray(), Is.EqualTo(new[] { "General", "Attack 1", "Attack 2", "Defence", "Durability", "Crafting" }));
            Assert.That(Row(paddle, "Attack 2", "Effect").Value, Is.EqualTo("Back-handed swing"));

            var whipStats = PaddleStats(3, true);
            whipStats.Family = "Whip";
            Assert.That(Row(Filled(whipStats), "Attack 2", "Effect").Value, Does.StartWith("Lash grab"));
        }

        [Test]
        public void Damage_ShowsQuality1BesideMaxQuality()
        {
            var blunt = Row(Filled(PaddleStats(1)), "Attack 1", "Blunt");
            Assert.That(blunt.Value, Is.EqualTo("30"), "crop blunt 20 + one bleed tick 10");
            Assert.That(blunt.MaxValue, Is.EqualTo("48"), "30 + 3 upgrade levels x 6");
        }

        [Test]
        public void Attack2_DamageIsItemDamageTimesItsMultiplier()
        {
            var blunt = Row(Filled(PaddleStats(1, true)), "Attack 2", "Blunt");
            Assert.That(blunt.Value, Is.EqualTo("75"), "30 x 2.5");
            Assert.That(blunt.MaxValue, Is.EqualTo("120"), "48 x 2.5");
        }

        [Test]
        public void Stagger_IsTheMultiplierTimesTheDamage_SoItMatchesTheLadder()
        {
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                var stagger = Row(Filled(PaddleStats(rank)), "Attack 1", "Stagger");
                Assert.That(stagger.Value, Is.EqualTo(Ladder.Stagger[rank - 1].ToString("0.#", CultureInfo.InvariantCulture)), "rank " + rank);
            }
            Assert.That(Row(Filled(PaddleStats(1)), "Attack 1", "Stagger").MaxValue, Is.EqualTo("19.2"), "12 x 48 / 30");
        }

        [Test]
        public void TheLadderNumbers_ShowUpForEveryRank()
        {
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                int i = rank - 1;
                var info = Filled(PaddleStats(rank));
                Assert.That(Row(info, "Attack 1", "Blunt").Value, Is.EqualTo(Ladder.PaddleBlunt(rank).ToString("0.#", CultureInfo.InvariantCulture)), "blunt, rank " + rank);
                Assert.That(Row(info, "Attack 1", "Knockback").Value, Is.EqualTo(Ladder.Knockback[i].ToString("0.#", CultureInfo.InvariantCulture)), "knockback, rank " + rank);
                Assert.That(Row(info, "Attack 1", "Stamina").Value, Is.EqualTo(Ladder.Stamina[i].ToString("0.#", CultureInfo.InvariantCulture)), "stamina, rank " + rank);
                Assert.That(Row(info, "Defence", "Block power").Value, Is.EqualTo(Ladder.BlockArmor[i].ToString("0.#", CultureInfo.InvariantCulture)), "block, rank " + rank);
                Assert.That(Row(info, "Durability", "Durability").Value, Is.EqualTo(Ladder.Durability(rank).ToString("0.#", CultureInfo.InvariantCulture)), "durability, rank " + rank);
            }
        }

        [Test]
        public void Durability_ShowsTheMaxQualityValue_AndMaxQualityAsOneColumn()
        {
            var info = Filled(PaddleStats(1));
            var dur = Row(info, "Durability", "Durability");
            Assert.That(dur.Value, Is.EqualTo("200"));
            Assert.That(dur.MaxValue, Is.EqualTo("320"), "200 + 3 x 40");
            var maxQ = Row(info, "Durability", "Max quality");
            Assert.That(maxQ.Value, Is.EqualTo("4"));
            Assert.That(maxQ.HasMax, Is.False);
        }

        [Test]
        public void AValueThatDoesNotGrowWithQuality_HasNoMaxColumnText()
        {
            var info = Filled(PaddleStats(1));
            Assert.That(Row(info, "Defence", "Block power").HasMax, Is.False, "no block power per level");
            Assert.That(Row(info, "Attack 1", "Knockback").HasMax, Is.False);
            Assert.That(Row(info, "Defence", "Parry bonus").Value, Is.EqualTo("x2"));
        }

        [Test]
        public void Bleed_IsOnlyShownWhenTheWeaponBleeds()
        {
            var s = PaddleStats(2);
            Assert.That(Filled(s).Groups[1].Rows.Any(r => r.Label == "Bleed"), Is.False);
            s.BleedPerSecond = 12.5f;
            s.BleedSeconds = 5f;
            Assert.That(Row(Filled(s), "Attack 1", "Bleed").Value, Is.EqualTo("12.5 per second for 5 s"));
        }

        [Test]
        public void ZeroDamageTypes_AreLeftOut()
        {
            var s = PaddleStats(1);
            s.Damage.Add(new WeaponStats.DamageLine("slash", 0f, 0f));
            var rows = Filled(s).Groups[1].Rows.Select(r => r.Label).ToArray();
            Assert.That(rows, Does.Contain("Blunt"));
            Assert.That(rows, Does.Not.Contain("Slash"));
        }

        [Test]
        public void TheRecipe_ShowsStationAndEveryIngredientWithItsUpgradeCost()
        {
            var info = Filled(PaddleStats(1));
            Assert.That(Row(info, "Crafting", "Station").Value, Is.EqualTo("Workbench, level 1"));
            Assert.That(Row(info, "Crafting", "LeatherScraps").Value, Is.EqualTo("10 (+5 per upgrade)"));
            Assert.That(Row(info, "Crafting", "Resin").Value, Is.EqualTo("2 (+1 per upgrade)"));
            Assert.That(info.Groups.Last().Rows.Count, Is.EqualTo(1 + Ladder.Ingredients[0].Length));
        }

        [Test]
        public void NoRecipe_SaysSo()
        {
            var s = PaddleStats(1);
            s.Station = "";
            s.Ingredients.Clear();
            Assert.That(Row(Filled(s), "Crafting", "Station").Value, Is.EqualTo("No recipe found"));
        }

        [Test]
        public void TheColumnHeaders_NameQuality1AndTheMaxQuality()
        {
            var info = Filled(PaddleStats(1));
            Assert.That(info.QualityHeader, Is.EqualTo("Quality 1"));
            Assert.That(info.MaxQualityHeader, Is.EqualTo("Quality 4"));
            Assert.That(info.HasMaxColumn, Is.True);
        }

        [Test]
        public void MaxQuality1_HasNoMaxColumnAtAll()
        {
            var s = PaddleStats(1);
            s.MaxQuality = 1;
            var info = Filled(s);
            Assert.That(info.MaxQualityHeader, Is.EqualTo(""));
            Assert.That(info.HasMaxColumn, Is.False);
        }

        [Test]
        public void NoStats_GiveNoGroups_AndALogLine()
        {
            var log = new List<string>();
            var info = new WeaponInfo("Whip", 2, "N", "D");
            bool ok = WeaponDetailsBuilder.Fill(info, null, Text, log.Add);
            Assert.That(ok, Is.True, "no stats = no groups is the expected state");
            Assert.That(info.Groups, Is.Empty);
            Assert.That(log.Any(l => l.Contains("no stats")), Is.True, string.Join(" | ", log.ToArray()));
        }

        [Test]
        public void FillingTwice_ReplacesTheGroups_NotAddsToThem()
        {
            var info = new WeaponInfo("Paddle", 1, "N", "D");
            WeaponDetailsBuilder.Fill(info, PaddleStats(1), Text);
            WeaponDetailsBuilder.Fill(info, PaddleStats(1), Text);
            Assert.That(info.Groups.Count, Is.EqualTo(5));
        }

        [Test]
        public void Numbers_UseThePointWhateverTheSystemLanguage()
        {
            var old = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(Row(Filled(PaddleStats(1)), "Attack 1", "Stagger").MaxValue, Is.EqualTo("19.2"));
            }
            finally { Thread.CurrentThread.CurrentCulture = old; }
        }

        [Test]
        public void EveryTextKeyTheBuilderUses_IsInTheEnglishTable()
        {
            var used = new HashSet<string>();
            System.Func<string, string> recording = k => { used.Add(k); return UiText.English(k); };
            foreach (var family in WeaponSelection.Families)
            {
                var s = PaddleStats(1, true);
                s.Family = family;
                s.BleedPerSecond = 10f;
                s.BleedSeconds = 5f;
                foreach (var type in new[] { "blunt", "slash", "pierce", "chop", "pickaxe", "fire", "frost", "lightning", "poison", "spirit" })
                    s.Damage.Add(new WeaponStats.DamageLine(type, 1f, 1f));
                WeaponDetailsBuilder.Fill(new WeaponInfo(family, 1, "N", "D"), s, recording);
                s.OneHanded = false;
                s.Station = "";
                s.Ingredients.Clear();
                WeaponDetailsBuilder.Fill(new WeaponInfo(family, 1, "N", "D"), s, recording);
            }
            var missing = used.Where(k => !UiText.En.ContainsKey(k)).ToArray();
            Assert.That(missing, Is.Empty, "keys used but not in UiText.En: " + string.Join(", ", missing));
        }
    }
}
