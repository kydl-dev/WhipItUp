using WhipItUp;

namespace WhipItUp.Tests.Unit
{
    /// <summary>Which functionalities are on (PROJECT_RULES 11): the pure rules in Functions.</summary>
    [TestFixture]
    public class FunctionsTests
    {
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void IsOn_NeedsEnableAllAndTheOwnSwitch(bool all, bool own, bool want)
        {
            Assert.That(Functions.IsOn(all, own), Is.EqualTo(want));
        }

        [TestCase(true, true, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void SkillNeeded_WhenFloggerOrWhipIsOn(bool flogger, bool whip, bool want)
        {
            Assert.That(Functions.SkillNeeded(flogger, whip), Is.EqualTo(want));
        }

        [TestCase("Paddle", true, false, false, true)]
        [TestCase("paddle", true, false, false, true)]
        [TestCase("Flogger", false, true, false, true)]
        [TestCase("Whip", false, false, true, true)]
        [TestCase("Paddle", false, true, true, false)]
        [TestCase("Flogger", true, false, true, false)]
        [TestCase("Whip", true, true, false, false)]
        [TestCase("Sword", true, true, true, false)]
        [TestCase(null, true, true, true, false)]
        public void FamilyOn_ReturnsTheFlagOfThatFamily(string family, bool paddle, bool flogger, bool whip, bool want)
        {
            Assert.That(Functions.FamilyOn(family, paddle, flogger, whip), Is.EqualTo(want));
        }

        [TestCase(true, true, false, false, true)]
        [TestCase(true, false, false, true, true)]
        [TestCase(true, false, false, false, false)]
        [TestCase(false, true, true, true, false)]
        public void ViewerShown_NeedsTheViewerAndAWeaponFamily(bool viewer, bool paddle, bool flogger, bool whip, bool want)
        {
            Assert.That(Functions.ViewerShown(viewer, paddle, flogger, whip), Is.EqualTo(want));
        }
    }
}
