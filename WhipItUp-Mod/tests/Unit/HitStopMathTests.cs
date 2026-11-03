using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>T-019: the whip hit-stop length (WhipHitStopPatch uses HitStopMath.Duration).</summary>
    [TestFixture]
    public class HitStopMathTests
    {
        [TestCase(0.15f, 0.05f, 0.05f)]
        [TestCase(0.15f, 0f, 0f)]
        [TestCase(0.15f, 0.15f, 0.15f)]
        [TestCase(0.15f, 0.5f, 0.15f)]
        [TestCase(0.15f, -1f, 0f)]
        [TestCase(0f, 0.05f, 0f)]
        public void Duration_IsTheSettingCappedByTheGamePause(float vanilla, float setting, float expected)
        {
            Assert.That(HitStopMath.Duration(vanilla, setting), Is.EqualTo(expected).Within(1e-6f));
        }
    }
}
