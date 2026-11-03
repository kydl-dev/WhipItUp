using System.Collections.Generic;
using WhipItUp.Ui;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The weapon the menu shows (T-015): one shared selection for both tabs; Changed fires once per real change.</summary>
    [TestFixture]
    public class WeaponSelectionTests
    {
        static List<WeaponInfo> Full()
        {
            var list = new List<WeaponInfo>();
            foreach (var family in WeaponSelection.Families)
                for (int rank = 1; rank <= 7; rank++) list.Add(new WeaponInfo(family, rank, family + " " + rank, "desc"));
            return list;
        }

        [Test]
        public void Starts_OnTheFirstWeapon()
        {
            var s = new WeaponSelection(Full());
            Assert.That(s.Family, Is.EqualTo("Paddle"));
            Assert.That(s.Rank, Is.EqualTo(1));
            Assert.That(s.Current.Name, Is.EqualTo("Paddle 1"));
        }

        [Test]
        public void Set_ChangesTheWeapon_AndFiresChangedOnce()
        {
            var s = new WeaponSelection(Full());
            int fired = 0;
            s.Changed += () => fired++;
            Assert.That(s.Set("Flogger", 4), Is.True);
            Assert.That(fired, Is.EqualTo(1));
            Assert.That(s.Current.Name, Is.EqualTo("Flogger 4"));
        }

        [Test]
        public void Set_TheSameWeaponAgain_FiresNothing()
        {
            var s = new WeaponSelection(Full());
            s.Set("Whip", 3);
            int fired = 0;
            s.Changed += () => fired++;
            Assert.That(s.Set("Whip", 3), Is.True, "already selected = the selection is that weapon");
            Assert.That(fired, Is.EqualTo(0));
        }

        [Test]
        public void Set_AnUnknownWeapon_IsIgnored_NoEvent()
        {
            var s = new WeaponSelection(Full());
            int fired = 0;
            s.Changed += () => fired++;
            Assert.That(s.Set("Spatula", 1), Is.False);
            Assert.That(s.Set("Paddle", 8), Is.False);
            Assert.That(s.Set("Paddle", 0), Is.False);
            Assert.That(fired, Is.EqualTo(0));
            Assert.That(s.Family + s.Rank, Is.EqualTo("Paddle1"));
        }

        [Test]
        public void SetFamily_KeepsTheRank()
        {
            var s = new WeaponSelection(Full());
            s.SetRank(5);
            Assert.That(s.SetFamily("Whip"), Is.True);
            Assert.That(s.Family, Is.EqualTo("Whip"));
            Assert.That(s.Rank, Is.EqualTo(5));
        }

        [Test]
        public void SetFamily_WhenTheFamilyLacksThatRank_TakesItsWeakest()
        {
            var list = Full();
            list.RemoveAll(w => w.Family == "Whip" && w.Rank > 2);
            var s = new WeaponSelection(list);
            s.SetRank(6);
            Assert.That(s.SetFamily("Whip"), Is.True);
            Assert.That(s.Rank, Is.EqualTo(1));
        }

        [Test]
        public void SetFamily_OfAnEmptyFamily_IsIgnored()
        {
            var s = new WeaponSelection(Full());
            int fired = 0;
            s.Changed += () => fired++;
            Assert.That(s.SetFamily("Spatula"), Is.False);
            Assert.That(fired, Is.EqualTo(0));
        }

        [Test]
        public void InFamily_ListsWeakestFirst()
        {
            var list = Full();
            list.Reverse();
            var ranks = new WeaponSelection(list).InFamily("Flogger").ConvertAll(w => w.Rank);
            Assert.That(ranks, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Test]
        public void AnEmptyOrNullList_HasNoCurrentWeapon_AndDoesNotThrow()
        {
            foreach (var s in new[] { new WeaponSelection(null), new WeaponSelection(new List<WeaponInfo>()) })
            {
                Assert.That(s.Current, Is.Null);
                Assert.That(s.Set("Paddle", 1), Is.True, "the start values are Paddle V1");
                Assert.That(s.Set("Whip", 2), Is.False);
                Assert.That(s.InFamily("Paddle"), Is.Empty);
            }
        }

        [Test]
        public void EveryDecision_IsLogged()
        {
            var lines = new List<string>();
            var s = new WeaponSelection(Full(), lines.Add);
            s.Set("Flogger", 2);
            s.Set("Flogger", 2);
            s.Set("Nope", 9);
            Assert.That(lines.Count, Is.GreaterThanOrEqualTo(4), string.Join(" | ", lines));
            Assert.That(lines.Exists(l => l.Contains("Paddle V1 -> Flogger V2")), Is.True);
        }
    }
}
