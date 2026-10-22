using System;
using System.Globalization;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Where the viewer camera looks from: yaw, pitch and zoom (1 = the whole weapon fits), changed by drag / wheel / buttons and put back by Reset.
    /// Pure logic, no Unity / Valheim types (unit-tested: ViewerOrbitTests). Every change method returns whether the view changed and logs through
    /// the injected `log` (the viewport passes UiLog.Verbose; drags are logged by the scene's throttled render line, not here, to keep the log short).
    /// </summary>
    internal sealed class ViewerOrbit
    {
        readonly ViewerSettings _settings;
        readonly Action<string> _log;

        internal float Yaw { get; private set; }
        internal float Pitch { get; private set; }
        internal float Zoom { get; private set; }

        internal ViewerOrbit(ViewerSettings settings, Action<string> log = null)
        {
            _settings = settings ?? new ViewerSettings();
            _log = log;
            Reset();
        }

        /// Back to the default view (start yaw / pitch, zoom 1). Expected outcome: returns whether anything changed.
        internal bool Reset()
        {
            float yaw = ViewerMath.WrapYaw(_settings.StartYaw);
            float pitch = ViewerMath.ClampPitch(_settings.StartPitch, _settings.PitchLimit);
            bool changed = yaw != Yaw || pitch != Pitch || Zoom != 1f;
            Yaw = yaw; Pitch = pitch; Zoom = 1f;
            Log("ViewerOrbit: reset to " + this + (changed ? "" : " (already there)"));
            return changed;
        }

        /// A drag of dx / dy UI pixels (dx > 0 = right, dy > 0 = down): right turns the weapon to the right, down shows it more from above.
        internal bool Drag(float dx, float dy)
        {
            if (float.IsNaN(dx) || float.IsNaN(dy)) { Log("ViewerOrbit: drag with NaN ignored"); return false; }
            float yaw = ViewerMath.WrapYaw(Yaw - dx * _settings.RotateSpeed);
            float pitch = ViewerMath.ClampPitch(Pitch + dy * _settings.RotateSpeed, _settings.PitchLimit);
            bool changed = yaw != Yaw || pitch != Pitch;
            Yaw = yaw; Pitch = pitch;
            return changed;
        }

        /// Zoom by whole steps (+1 = one step closer, -1 = one step away).
        internal bool StepZoom(float steps)
        {
            float z = ViewerMath.StepZoom(Zoom, steps, _settings.ZoomStep, _settings.ZoomMin, _settings.ZoomMax);
            bool changed = z != Zoom;
            Log("ViewerOrbit: zoom " + F(Zoom) + " -> " + F(z) + " (" + F(steps) + " step(s), range " + F(_settings.ZoomMin) + ".." + F(_settings.ZoomMax) + ")" + (changed ? "" : " - at the limit"));
            Zoom = z;
            return changed;
        }

        /// A mouse wheel turn: UI Toolkit's delta.y is positive when the wheel turns toward the player (= zoom out); one notch = one step.
        internal bool Wheel(float deltaY)
        {
            if (deltaY == 0f || float.IsNaN(deltaY)) return false;
            return StepZoom(deltaY > 0f ? -1f : 1f);
        }

        static string F(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        public override string ToString() { return "yaw " + F(Yaw) + " deg, pitch " + F(Pitch) + " deg, zoom x" + F(Zoom); }

        void Log(string s) { if (_log != null) _log(s); }
    }
}
