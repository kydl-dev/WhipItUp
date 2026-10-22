using System;
using System.Collections.Generic;
using WhipItUp.Ui;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The 3D viewer's pure math, orbit state and settings ranges (T-015 S4).</summary>
    [TestFixture]
    public class ViewerMathTests
    {
        const float Eps = 1e-3f;

        [Test]
        public void FitDistance_TallViewport_IsLimitedByWidth()
        {
            // 1 m half width in a square 30 degree view: d = 1 / tan(15) = 3.732
            float d = ViewerMath.FitDistance(1f, 0.1f, 1f, 30f, 1f, 0f);
            Assert.That(d, Is.EqualTo(1f / (float)Math.Tan(15 * Math.PI / 180)).Within(Eps));
        }

        [Test]
        public void FitDistance_WideViewport_NeedsLessDistanceForTheWidth()
        {
            float square = ViewerMath.FitDistance(2f, 0.05f, 0.1f, 30f, 1f, 0f);
            float wide = ViewerMath.FitDistance(2f, 0.05f, 0.1f, 30f, 2.5f, 0f);
            Assert.That(wide, Is.LessThan(square));
            Assert.That(wide, Is.EqualTo(2f / (2.5f * (float)Math.Tan(15 * Math.PI / 180))).Within(Eps));
        }

        [Test]
        public void FitDistance_NeverCloserThanTheMinimumRadii()
        {
            float d = ViewerMath.FitDistance(0.01f, 0.01f, 1f, 90f, 4f, 0f);
            Assert.That(d, Is.EqualTo(ViewerMath.MinDistanceRadii).Within(Eps));
        }

        [Test]
        public void FitDistance_MarginGrowsTheDistance()
        {
            float plain = ViewerMath.FitDistance(1f, 1f, 0.1f, 30f, 1f, 0f);
            float room = ViewerMath.FitDistance(1f, 1f, 0.1f, 30f, 1f, 0.1f);
            Assert.That(room, Is.EqualTo(plain * 1.1f).Within(Eps));
        }

        [Test]
        public void FitDistance_BadInput_GivesAPositiveDistance()
        {
            Assert.That(ViewerMath.FitDistance(float.NaN, -1f, 0f, 0f, float.NaN, float.NaN), Is.GreaterThan(0f));
            Assert.That(ViewerMath.FitDistance(1f, 1f, 1f, 30f, 0f, 0f), Is.GreaterThan(0f));
        }

        [TestCase(10f, 80f, 10f)]
        [TestCase(95f, 80f, 80f)]
        [TestCase(-95f, 80f, -80f)]
        [TestCase(50f, 120f, 50f)]
        [TestCase(95f, 120f, 89f)]
        public void ClampPitch_StaysWithinTheLimit(float pitch, float limit, float expected)
        {
            Assert.That(ViewerMath.ClampPitch(pitch, limit), Is.EqualTo(expected).Within(Eps));
        }

        [TestCase(190f, -170f)]
        [TestCase(-190f, 170f)]
        [TestCase(720f, 0f)]
        [TestCase(180f, 180f)]
        [TestCase(45f, 45f)]
        public void WrapYaw_IsWithinMinus180To180(float yaw, float expected)
        {
            Assert.That(ViewerMath.WrapYaw(yaw), Is.EqualTo(expected).Within(Eps));
        }

        [Test]
        public void StepZoom_MultipliesAndClamps()
        {
            Assert.That(ViewerMath.StepZoom(1f, 1f, 1.2f, 0.5f, 8f), Is.EqualTo(1.2f).Within(Eps));
            Assert.That(ViewerMath.StepZoom(1f, -1f, 1.2f, 0.5f, 8f), Is.EqualTo(1f / 1.2f).Within(Eps));
            Assert.That(ViewerMath.StepZoom(7f, 3f, 1.2f, 0.5f, 8f), Is.EqualTo(8f).Within(Eps));
            Assert.That(ViewerMath.StepZoom(0.6f, -5f, 1.2f, 0.5f, 8f), Is.EqualTo(0.5f).Within(Eps));
        }

        [Test]
        public void Orbit_YawZeroPitchZero_IsInFrontOnMinusZ()
        {
            ViewerMath.Orbit(0f, 0f, 2f, out float x, out float y, out float z);
            Assert.That(x, Is.EqualTo(0f).Within(Eps));
            Assert.That(y, Is.EqualTo(0f).Within(Eps));
            Assert.That(z, Is.EqualTo(-2f).Within(Eps));
        }

        [Test]
        public void Orbit_KeepsTheDistance_AndPitchUpIsAbove()
        {
            ViewerMath.Orbit(37f, 25f, 3f, out float x, out float y, out float z);
            Assert.That(Math.Sqrt(x * x + y * y + z * z), Is.EqualTo(3.0).Within(Eps));
            Assert.That(y, Is.GreaterThan(0f));
        }

        [Test]
        public void Orbit_Reset_IsTheStartView()
        {
            var s = new ViewerSettings { StartYaw = 20f, StartPitch = 12f };
            var o = new ViewerOrbit(s);
            o.Drag(100f, 50f);
            o.StepZoom(2f);
            Assert.That(o.Reset(), Is.True);
            Assert.That(o.Yaw, Is.EqualTo(20f).Within(Eps));
            Assert.That(o.Pitch, Is.EqualTo(12f).Within(Eps));
            Assert.That(o.Zoom, Is.EqualTo(1f).Within(Eps));
            Assert.That(o.Reset(), Is.False, "a second reset changes nothing");
        }

        [Test]
        public void Orbit_DragRight_TurnsYaw_DragDown_RaisesPitch_WithinTheLimit()
        {
            var s = new ViewerSettings { StartYaw = 0f, StartPitch = 0f, RotateSpeed = 0.5f, PitchLimit = 30f };
            var o = new ViewerOrbit(s);
            Assert.That(o.Drag(20f, 0f), Is.True);
            Assert.That(o.Yaw, Is.EqualTo(-10f).Within(Eps));
            o.Drag(0f, 1000f);
            Assert.That(o.Pitch, Is.EqualTo(30f).Within(Eps));
            Assert.That(o.Drag(0f, 10f), Is.False, "at the pitch limit a further drag down changes nothing");
        }

        [Test]
        public void Orbit_Wheel_TowardThePlayer_ZoomsOut()
        {
            var log = new List<string>();
            var o = new ViewerOrbit(new ViewerSettings(), log.Add);
            o.Wheel(1f);
            Assert.That(o.Zoom, Is.LessThan(1f));
            o.Wheel(-1f);
            o.Wheel(-1f);
            Assert.That(o.Zoom, Is.GreaterThan(1f));
            Assert.That(o.Wheel(0f), Is.False);
            Assert.That(log.Exists(l => l.Contains("ViewerOrbit: zoom")), Is.True, "zoom changes are logged");
        }

        [Test]
        public void Settings_Clamp_PutsEveryValueIntoItsRange()
        {
            var s = new ViewerSettings { RotateSpeed = 99f, ZoomStep = 0f, ZoomMax = float.NaN, FrameHeight = 5f, PitchLimit = 10f, StartPitch = 45f };
            var log = new List<string>();
            int changed = s.Clamp(log.Add);
            Assert.That(changed, Is.EqualTo(5));
            Assert.That(s.RotateSpeed, Is.EqualTo(ViewerSettings.RotateSpeedMax));
            Assert.That(s.ZoomStep, Is.EqualTo(ViewerSettings.ZoomStepMin));
            Assert.That(s.ZoomMax, Is.EqualTo(ViewerSettings.ZoomMaxDefault));
            Assert.That(s.FrameHeight, Is.EqualTo(ViewerSettings.FrameHeightMin));
            Assert.That(s.StartPitch, Is.EqualTo(10f));
            Assert.That(log.Count, Is.EqualTo(6), "one line per fixed value + the summary");
        }

        [Test]
        public void Settings_Defaults_AreInsideTheirRanges()
        {
            var s = new ViewerSettings();
            Assert.That(s.Clamp(), Is.EqualTo(0));
        }
    }
}
