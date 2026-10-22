using System;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Pure float math of the 3D viewer: how far the camera stands so the weapon fits, zoom / pitch limits, where the camera is for a yaw / pitch.
    /// No Unity / Valheim types (unit-tested: ViewerMathTests). Angles in degrees, lengths in metres.
    /// </summary>
    internal static class ViewerMath
    {
        const double Rad = Math.PI / 180.0;

        /// The camera never comes closer to the weapon's centre than this many bounding-sphere radii (so turning a long weapon toward the camera
        /// never puts its tip behind the camera).
        internal const float MinDistanceRadii = 1.5f;

        /// Camera distance from the weapon's centre at zoom 1 for a box of half width `halfWidth` (across the view), half height `halfHeight`
        /// (up the view) and bounding-sphere `radius`, with a vertical field of view and the viewport's aspect (width / height); `margin` = extra
        /// room (0.08 = 8 %). Expected outcome: a distance > 0 (bad input: the result for a 0.5 m weapon in a square 30 degree view).
        internal static float FitDistance(float halfWidth, float halfHeight, float radius, float verticalFovDeg, float aspect, float margin)
        {
            if (!(verticalFovDeg > 1f && verticalFovDeg < 179f)) verticalFovDeg = 30f;
            if (!(aspect > 0.01f) || float.IsInfinity(aspect)) aspect = 1f;
            if (!(margin >= 0f) || float.IsInfinity(margin)) margin = 0f;
            if (!(radius > 0f) || float.IsInfinity(radius)) radius = 0.5f;
            halfWidth = halfWidth > 0f && !float.IsInfinity(halfWidth) ? halfWidth : radius;
            halfHeight = halfHeight > 0f && !float.IsInfinity(halfHeight) ? halfHeight : radius;
            double tanV = Math.Tan(verticalFovDeg * 0.5 * Rad);
            double tanH = tanV * aspect;
            double grow = 1.0 + margin;
            double byWidth = halfWidth * grow / tanH;
            double byHeight = halfHeight * grow / tanV;
            double least = radius * MinDistanceRadii;
            return (float)Math.Max(least, Math.Max(byWidth, byHeight));
        }

        /// Pitch kept within -limit..limit (limit itself 0..89).
        internal static float ClampPitch(float pitch, float limit)
        {
            limit = Math.Max(0f, Math.Min(89f, float.IsNaN(limit) ? 0f : limit));
            if (float.IsNaN(pitch)) return 0f;
            return Math.Max(-limit, Math.Min(limit, pitch));
        }

        /// Yaw wrapped into -180..180 (a long drag never grows the number).
        internal static float WrapYaw(float yaw)
        {
            if (float.IsNaN(yaw) || float.IsInfinity(yaw)) return 0f;
            double y = yaw % 360.0;
            if (y > 180.0) y -= 360.0;
            if (y <= -180.0) y += 360.0;
            return (float)y;
        }

        /// Magnification after `steps` zoom steps (positive = closer) of `factor` each, kept within min..max (min <= max is enforced).
        internal static float StepZoom(float zoom, float steps, float factor, float min, float max)
        {
            if (!(min > 0f)) min = 0.01f;
            if (max < min) max = min;
            if (!(factor > 1f)) factor = 1f;
            if (!(zoom > 0f) || float.IsInfinity(zoom)) zoom = 1f;
            double z = zoom * Math.Pow(factor, float.IsNaN(steps) ? 0f : steps);
            return (float)Math.Max(min, Math.Min(max, z));
        }

        /// Camera position relative to the weapon's centre for a yaw (turn around the up axis) and pitch (positive = from above) at `distance`.
        /// Yaw 0, pitch 0 = the camera on the -Z side looking toward +Z (the weapon's front faces the camera).
        internal static void Orbit(float yawDeg, float pitchDeg, float distance, out float x, out float y, out float z)
        {
            double yaw = yawDeg * Rad, pitch = pitchDeg * Rad;
            double flat = distance * Math.Cos(pitch);
            x = (float)(-flat * Math.Sin(yaw));
            y = (float)(distance * Math.Sin(pitch));
            z = (float)(-flat * Math.Cos(yaw));
        }
    }
}
