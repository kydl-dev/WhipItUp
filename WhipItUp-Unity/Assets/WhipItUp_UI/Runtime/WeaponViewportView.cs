using System;
using System.Globalization;
using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The Viewer tab: the two drop-downs, then a fixed-size viewport with the selected weapon in 3D (IWeaponScene renders it into a texture).
    /// Left-drag turns it, right-drag moves it inside the view (D-056), the mouse wheel and [+] / [-] zoom, [Reset] returns to the default view; a new weapon also starts at the default view.
    /// The viewport keeps its size: zoomed in, the weapon simply runs past the edges (the texture is the viewport, nothing outside it is drawn).
    /// Without a scene (null) the frame only shows the weapon's name and a note.
    /// </summary>
    internal static class WeaponViewportView
    {
        internal static VisualElement Build(Func<string, string> text, WeaponSelection selection, Func<IWeaponScene> makeScene = null, ViewerSettings settings = null)
        {
            settings = settings ?? new ViewerSettings();
            settings.Clamp(UiLog.Verbose);
            var tab = new VisualElement { name = "whipitup-viewer-tab" };
            tab.style.flexShrink = 0;
            tab.Add(WeaponPicker.Build(text, selection));

            var frame = new VisualElement { name = "whipitup-viewport" };
            frame.style.flexShrink = 0; frame.style.height = settings.FrameHeight; frame.style.marginTop = 10;
            frame.style.backgroundColor = UiKit.Track;
            UiKit.Borders(frame, 1, UiKit.Border); UiKit.Radius(frame, 4);
            frame.style.overflow = Overflow.Hidden;                    // what is zoomed past the frame is never shown
            tab.Add(frame);

            IWeaponScene scene = null;
            try { scene = makeScene != null ? makeScene() : null; }
            catch (Exception e) { UiLog.Error("WeaponViewportView: making the 3D scene failed: " + e.GetType().Name + ": " + e.Message); }
            if (scene == null)
            {
                BuildPlaceholder(text, selection, frame);
                return tab;
            }
            new Viewport(text, selection, scene, settings, frame);
            return tab;
        }

        // No 3D scene: the weapon's name in the middle of the frame and a note why there is no model.
        static void BuildPlaceholder(Func<string, string> text, WeaponSelection selection, VisualElement frame)
        {
            frame.style.justifyContent = Justify.Center; frame.style.alignItems = Align.Center;
            var name = UiKit.Label("", UiKit.SectionSize, UiKit.TextDim, true);
            frame.Add(name);
            frame.Add(UiKit.Label(text("whipitup_viewer_unavailable"), UiKit.FontSize, UiKit.TextDim));
            Action show = () =>
            {
                var w = selection.Current;
                name.text = w != null ? w.Name : "";
                UiLog.Verbose("WeaponViewportView: no 3D scene, showing the name of " + (w != null ? w.ToString() : "no weapon"));
            };
            selection.Changed += show;
            show();
        }

        /// The live viewport: owns the scene (disposed when the window goes away), the orbit and the input.
        sealed class Viewport
        {
            readonly WeaponSelection _selection;
            readonly IWeaponScene _scene;
            readonly ViewerOrbit _orbit;
            readonly Image _image;
            readonly Label _name;
            readonly VisualElement _frame;
            bool _dragging;
            bool _panning;                                              // the drag in progress: right button = pan, left button = rotate
            int _dragButton = -1;
            Vector2 _last;
            int _pointer = -1;
            readonly ViewerPose _pose = new ViewerPose(UiLog.Verbose);   // coiled by default, only the whip has the switch (D-057)
            readonly Func<string, string> _text;
            Button _poseButton;

            internal Viewport(Func<string, string> text, WeaponSelection selection, IWeaponScene scene, ViewerSettings settings, VisualElement frame)
            {
                _selection = selection;
                _text = text;
                _scene = scene;
                _frame = frame;
                _orbit = new ViewerOrbit(settings, UiLog.Verbose);

                _image = new Image { name = "whipitup-viewport-image", scaleMode = ScaleMode.StretchToFill };
                _image.style.position = Position.Absolute;
                _image.style.left = 0; _image.style.top = 0; _image.style.right = 0; _image.style.bottom = 0;
                frame.Add(_image);

                _name = UiKit.Label("", UiKit.SectionSize, UiKit.Gold, true);
                _name.style.position = Position.Absolute; _name.style.left = 10; _name.style.top = 6;
                _name.pickingMode = PickingMode.Ignore;
                frame.Add(_name);

                var hint = UiKit.Label(text("whipitup_viewer_hint"), UiKit.SmallSize, UiKit.TextDim);
                hint.style.position = Position.Absolute; hint.style.left = 10; hint.style.bottom = 8;
                hint.pickingMode = PickingMode.Ignore;
                frame.Add(hint);

                var buttons = new VisualElement { name = "whipitup-viewport-buttons" };
                buttons.style.position = Position.Absolute; buttons.style.right = 6; buttons.style.bottom = 4;
                buttons.style.flexDirection = FlexDirection.Row;
                _poseButton = UiControls.Button(text(_pose.ButtonKey), TogglePose);
                buttons.Add(_poseButton);
                buttons.Add(UiControls.Button("+", () => Changed(_orbit.StepZoom(1f), "zoom in button")));
                buttons.Add(UiControls.Button("-", () => Changed(_orbit.StepZoom(-1f), "zoom out button")));
                buttons.Add(UiControls.Button(text("whipitup_viewer_reset"), () => Changed(_orbit.Reset(), "reset button")));
                frame.Add(buttons);

                _image.RegisterCallback<PointerDownEvent>(OnDown);
                _image.RegisterCallback<PointerMoveEvent>(OnMove);
                _image.RegisterCallback<PointerUpEvent>(OnUp);
                _image.RegisterCallback<PointerCaptureOutEvent>(e => EndDrag("capture lost"));
                frame.RegisterCallback<WheelEvent>(OnWheel);
                frame.RegisterCallback<GeometryChangedEvent>(e => OnResized(frame));
                frame.RegisterCallback<DetachFromPanelEvent>(e => Dispose());

                _selection.Changed += ShowSelected;
                ShowSelected();
                UiLog.Verbose("WeaponViewportView: 3D viewport built (" + settings + ")");
            }

            void ShowSelected()
            {
                var w = _selection.Current;
                _name.text = w != null ? w.Name : "";
                string family = w != null ? w.Family : null;
                bool coiled = _pose.ShowsCoiled(family);
                bool shown = _scene.Show(w, coiled);
                _orbit.Reset();
                bool hasSwitch = ViewerPose.HasSwitch(family);
                _poseButton.style.display = hasSwitch ? DisplayStyle.Flex : DisplayStyle.None;
                UiLog.Verbose("WeaponViewportView: selected " + (w != null ? w.ToString() : "no weapon") + ", pose " + (coiled ? "coiled" : "rest") + " (switch " + (hasSwitch ? "shown, set to " + _pose : "hidden: " + family + " has none") + "), model "
                    + (shown ? "shown" : "NOT shown (see the WeaponViewerRig lines above)"));
                Draw("weapon changed");
            }

            // The Straight / Coiled button: only the model is made again - the view (rotation, zoom, pan) stays as it is (D-057).
            void TogglePose()
            {
                var w = _selection.Current;
                string family = w != null ? w.Family : null;
                if (!ViewerPose.HasSwitch(family)) { UiLog.Verbose("WeaponViewportView: pose button pressed on " + (w != null ? w.ToString() : "no weapon") + ", which has no switch - ignored"); return; }
                bool flipped = _pose.Toggle();
                _poseButton.text = _text(_pose.ButtonKey);
                bool shown = _scene.Show(w, _pose.ShowsCoiled(family));
                UiLog.Verbose("WeaponViewportView: pose switch of " + w + " -> " + _pose + " (flipped " + flipped + "), model " + (shown ? "shown" : "NOT shown (see the WeaponViewerRig lines above)")
                    + ", view kept: yaw " + _orbit.Yaw.ToString("0.#", CultureInfo.InvariantCulture) + ", pitch " + _orbit.Pitch.ToString("0.#", CultureInfo.InvariantCulture) + ", zoom " + _orbit.Zoom.ToString("0.##", CultureInfo.InvariantCulture));
                Draw("pose changed");
            }

            void OnDown(PointerDownEvent e)
            {
                if (e.button != 0 && e.button != 1) return;                // left = rotate, right = pan, other buttons are not ours
                if (_dragging)
                {
                    UiLog.Verbose("WeaponViewportView: button " + e.button + " pressed during a " + (_panning ? "pan" : "rotate") + " drag (button " + _dragButton + ") - ignored");
                    return;
                }
                _dragging = true;
                _panning = e.button == 1;
                _dragButton = e.button;
                _pointer = e.pointerId;
                _last = new Vector2(e.position.x, e.position.y);
                _image.CapturePointer(e.pointerId);
                e.StopPropagation();
                UiLog.Verbose("WeaponViewportView: " + (_panning ? "pan" : "rotate") + " drag started at " + P(_last) + " (button " + e.button + ", pointer " + e.pointerId + ", frame " + FrameHeightPx().ToString("0", CultureInfo.InvariantCulture) + " UI px high)");
            }

            void OnMove(PointerMoveEvent e)
            {
                if (!_dragging || e.pointerId != _pointer) return;
                var now = new Vector2(e.position.x, e.position.y);
                var d = now - _last;
                _last = now;
                if (d.sqrMagnitude <= 0f) return;
                bool changed = _panning ? _orbit.Pan(d.x, d.y, FrameHeightPx()) : _orbit.Drag(d.x, d.y);
                if (changed) Draw(null);
                e.StopPropagation();
            }

            void OnUp(PointerUpEvent e)
            {
                if (e.button != _dragButton || e.pointerId != _pointer) return;
                if (_image.HasPointerCapture(e.pointerId)) _image.ReleasePointer(e.pointerId);
                EndDrag("button " + e.button + " released");
                e.StopPropagation();
            }

            void EndDrag(string why)
            {
                if (!_dragging) return;
                _dragging = false;
                _pointer = -1;
                UiLog.Verbose("WeaponViewportView: " + (_panning ? "pan" : "rotate") + " drag ended (" + why + "), view " + _orbit);
                _panning = false;
                _dragButton = -1;
            }

            // The frame's height in UI pixels: what a dragged pixel is measured against, so the weapon follows the mouse.
            float FrameHeightPx() { return _frame.contentRect.height; }

            void OnWheel(WheelEvent e)
            {
                Changed(_orbit.Wheel(e.delta.y), "wheel " + e.delta.y.ToString("0.##", CultureInfo.InvariantCulture));
                e.StopPropagation();                                      // the window's scroll view must not scroll while zooming
            }

            void OnResized(VisualElement frame)
            {
                var r = frame.contentRect;
                float scale = PixelsPerPoint();
                int w = Mathf.RoundToInt(r.width * scale), h = Mathf.RoundToInt(r.height * scale);
                if (w < 16 || h < 16) { UiLog.Verbose("WeaponViewportView: frame " + r.width + "x" + r.height + " UI px is too small (hidden tab?), no texture"); return; }
                UiLog.Verbose("WeaponViewportView: frame " + r.width.ToString("0", CultureInfo.InvariantCulture) + "x" + r.height.ToString("0", CultureInfo.InvariantCulture)
                    + " UI px x " + scale.ToString("0.###", CultureInfo.InvariantCulture) + " = " + w + "x" + h + " screen px");
                if (_scene.Resize(w, h)) _image.image = _scene.Texture;
                Draw("resized");
            }

            // UI points -> screen pixels of the shared panel (scaled from 1080 p by height, Kydl.Ui UiKit).
            static float PixelsPerPoint()
            {
                var s = UiKit.Settings;
                if (s == null || s.scaleMode != PanelScaleMode.ScaleWithScreenSize || s.referenceResolution.y <= 0) return 1f;
                float height = s.targetTexture != null ? s.targetTexture.height : Screen.height;
                return Mathf.Clamp(height / s.referenceResolution.y, 0.25f, 8f);
            }

            void Changed(bool changed, string why)
            {
                if (changed) Draw(why);
                else UiLog.Verbose("WeaponViewportView: " + why + " - view unchanged (" + _orbit + ")");
            }

            // `why` null = a drag step (the scene logs those throttled).
            void Draw(string why)
            {
                bool done = _scene.Render(_orbit.Yaw, _orbit.Pitch, _orbit.Zoom, _orbit.PanX, _orbit.PanY, _orbit.PanZ);
                _image.MarkDirtyRepaint();
                if (why != null) UiLog.Verbose("WeaponViewportView: drawn after " + why + " (" + _orbit + ")" + (done ? "" : " - scene did not render"));
            }

            void Dispose()
            {
                _selection.Changed -= ShowSelected;
                _image.image = null;
                _scene.Dispose();
                UiLog.Verbose("WeaponViewportView: window closed, 3D scene disposed");
            }

            static string P(Vector2 v) { return "(" + v.x.ToString("0", CultureInfo.InvariantCulture) + ", " + v.y.ToString("0", CultureInfo.InvariantCulture) + ")"; }
        }
    }
}
