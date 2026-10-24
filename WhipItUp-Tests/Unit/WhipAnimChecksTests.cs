using WhipItUp.Testing;

namespace WhipItUp.Tests.Unit
{
    /// <summary>T-023: verdicts of the whip animation in-game tests.</summary>
    [TestFixture]
    public class WhipAnimChecksTests
    {
        [Test] public void Clips_AllThere_Pass() { Assert.That(WhipAnimChecks.Clips(4, 4, 4, 4), Does.StartWith("PASS")); }
        [Test] public void Clips_BodyMissing_Fail() { Assert.That(WhipAnimChecks.Clips(3, 4, 4, 4), Does.StartWith("FAIL")); }
        [Test] public void Clips_LashMissing_Fail() { Assert.That(WhipAnimChecks.Clips(4, 4, 0, 4), Does.StartWith("FAIL")); }

        [Test] public void Controller_Disabled_Skip() { Assert.That(WhipAnimChecks.Controller(false, true, false, false, 0, 4), Does.StartWith("SKIP")); }
        [Test] public void Controller_WhipAllSlots_Pass() { Assert.That(WhipAnimChecks.Controller(true, true, true, true, 4, 4), Does.StartWith("PASS")); }
        [Test] public void Controller_WhipMissingSlot_Fail() { Assert.That(WhipAnimChecks.Controller(true, true, true, true, 3, 4), Does.StartWith("FAIL")); }
        [Test] public void Controller_WhipNotAttached_Fail() { Assert.That(WhipAnimChecks.Controller(true, true, false, false, 0, 4), Does.StartWith("FAIL")); }
        [Test] public void Controller_NoWhipVanilla_Pass() { Assert.That(WhipAnimChecks.Controller(true, false, false, false, 0, 4), Does.StartWith("PASS")); }
        [Test] public void Controller_NoWhipStillOverride_Fail() { Assert.That(WhipAnimChecks.Controller(true, false, false, true, 4, 4), Does.StartWith("FAIL")); }

        [Test] public void Swaps_None_Skip() { Assert.That(WhipAnimChecks.Swaps(0, 0, null), Does.StartWith("SKIP")); }
        [Test] public void Swaps_AllKept_Pass() { Assert.That(WhipAnimChecks.Swaps(6, 0, null), Does.StartWith("PASS")); }
        [Test] public void Swaps_OneLost_Fail() { Assert.That(WhipAnimChecks.Swaps(6, 1, "Player -> 'x': 1 of 2"), Does.StartWith("FAIL").And.Contains("1 of 2")); }
    }
}
