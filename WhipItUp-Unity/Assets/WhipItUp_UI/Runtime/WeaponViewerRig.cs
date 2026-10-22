using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The 3D viewer's scene (IWeaponScene): one camera that only sees its own layer, two lights that only light that layer, and the weapon model,
    /// parked far outside the world. The camera is never enabled; it renders on demand into a RenderTexture the viewport shows, so nothing is drawn
    /// while the view does not change. The model comes from `makeModel` (preview: FBX + rank textures; game: a copy of the item visual, S5).
    /// The model's longest side is laid across the view (the viewport is wider than high), then the whole weapon is fitted (ViewerMath.FitDistance).
    /// Unity types only, no Valheim types (compiled into the mod and used by the Unity preview).
    /// </summary>
    internal sealed class WeaponViewerRig : IWeaponScene
    {
        /// Where the rig stands: outside the playable world (radius 10500 m), so no world light or object is near it.
        internal static readonly Vector3 Park = new Vector3(-15000f, 300f, -15000f);
        internal const float FieldOfView = 30f;
        const float LogEverySeconds = 0.5f;
        static readonly Color Ambient = new Color(0.38f, 0.36f, 0.34f);

        readonly Func<WeaponInfo, bool, GameObject> _makeModel;
        readonly ViewerSettings _settings;
        readonly Action<string> _log;
        readonly int _layer;
        GameObject _root, _pivot, _model;
        Camera _camera;
        RenderTexture _texture;
        WeaponInfo _weapon;
        float _halfWidth, _halfHeight, _radius;
        float _lastLog = -100f;
        int _renders;

        public Texture Texture { get { return _texture; } }

        /// `layer` 0..31: the layer only this rig uses (the game's cameras must not draw it, S5). `background` = the clear colour of the view.
        internal WeaponViewerRig(Func<WeaponInfo, bool, GameObject> makeModel, int layer, ViewerSettings settings, Color background, Action<string> log)
        {
            _makeModel = makeModel;
            _settings = settings ?? new ViewerSettings();
            _log = log;
            _layer = Mathf.Clamp(layer, 0, 31);
            if (_layer != layer) Log("WeaponViewerRig: layer " + layer + " is outside 0..31, using " + _layer);

            _root = new GameObject("WhipItUp_ViewerRig");
            _root.transform.position = Park;
            _pivot = new GameObject("Pivot");
            _pivot.transform.SetParent(_root.transform, false);

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(_root.transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.enabled = false;                                  // rendered on demand (Render)
            _camera.cullingMask = 1 << _layer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = background;
            _camera.fieldOfView = FieldOfView;
            _camera.nearClipPlane = 0.02f;
            _camera.farClipPlane = 200f;
            _camera.allowHDR = false;
            _camera.useOcclusionCulling = false;
            _camera.renderingPath = RenderingPath.Forward;              // light culling masks + MSAA only work in forward (the game may run deferred)

            AddLight(camGo.transform, "KeyLight", new Vector3(25f, 30f, 0f), 1.1f);     // from up-left behind the camera
            AddLight(camGo.transform, "FillLight", new Vector3(-10f, -45f, 0f), 0.45f); // softer from the right
            Log("WeaponViewerRig: made at " + V(Park) + ", layer " + _layer + ", fov " + FieldOfView + " deg, background " + background + ", " + _settings);
        }

        void AddLight(Transform parent, string name, Vector3 euler, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << _layer;                          // lights the weapon only, never the world
            light.renderMode = LightRenderMode.ForcePixel;
        }

        public bool Show(WeaponInfo weapon, bool coiled)
        {
            if (_root == null) { Log("WeaponViewerRig: Show(" + weapon + ", " + (coiled ? "coiled" : "rest pose") + ") after Dispose - ignored"); return false; }
            if (_model != null) { _model.SetActive(false); UnityEngine.Object.Destroy(_model); _model = null; }   // inactive now: Destroy only happens at the frame's end
            _weapon = weapon;
            if (weapon == null) { Log("WeaponViewerRig: no weapon to show"); return false; }

            GameObject model = null;
            Log("WeaponViewerRig: making the model of " + weapon + " in the " + (coiled ? "coiled" : "rest") + " pose");
            try { model = _makeModel != null ? _makeModel(weapon, coiled) : null; }
            catch (Exception e) { Log("WeaponViewerRig: making the model of " + weapon + " failed: " + e.GetType().Name + ": " + e.Message); }
            if (model == null) { Log("WeaponViewerRig: no model for " + weapon + " (model maker gave null)"); return false; }

            _model = model;
            _pivot.transform.localRotation = Quaternion.identity;
            _model.transform.SetParent(_pivot.transform, false);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            int renderers = Prepare(_model);
            if (renderers == 0) { Log("WeaponViewerRig: " + weapon + " model has no renderers - nothing to see"); return false; }

            Bounds raw = WorldBounds(_model);
            Vector3 local = raw.size;
            // the longest side across the view (world X): Y-long models turn -90 deg around Z (top to the right), Z-long ones 90 deg around Y
            Quaternion lay = Quaternion.identity;
            string axis = "X";
            if (local.y >= local.x && local.y >= local.z) { lay = Quaternion.Euler(0f, 0f, -90f); axis = "Y"; }
            else if (local.z >= local.x && local.z >= local.y) { lay = Quaternion.Euler(0f, 90f, 0f); axis = "Z"; }
            _model.transform.localRotation = lay;
            Bounds laid = WorldBounds(_model);
            _model.transform.position -= laid.center - _pivot.transform.position;   // the weapon's centre on the pivot (the orbit's centre)
            Bounds placed = WorldBounds(_model);
            _halfWidth = placed.extents.x;
            _halfHeight = Mathf.Max(placed.extents.y, placed.extents.z);    // y or z can face the camera after turning: keep the bigger
            _radius = placed.extents.magnitude;
            Log("WeaponViewerRig: showing " + weapon + " (" + (coiled ? "coiled" : "rest pose") + "): " + renderers + " renderer(s), size " + V(raw.size) + " m, longest axis " + axis
                + " laid across, centre offset " + V(placed.center - _pivot.transform.position) + " m, half width " + F(_halfWidth) + " / height " + F(_halfHeight)
                + " / radius " + F(_radius) + " m");
            return true;
        }

        // Puts every part on the viewer layer, without shadows; physics and scripts of a game copy must not run (S5 makes the copy inert too).
        int Prepare(GameObject model)
        {
            int renderers = 0, colliders = 0;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = _layer;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var skinned = r as SkinnedMeshRenderer;
                if (skinned != null) skinned.updateWhenOffscreen = true;       // bounds follow the bones, the rig is never on the game's screen
                renderers++;
            }
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) { c.enabled = false; colliders++; }
            Log("WeaponViewerRig: prepared model '" + model.name + "': " + renderers + " renderer(s) on layer " + _layer + ", " + colliders + " collider(s) off");
            return renderers;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public bool Resize(int widthPx, int heightPx)
        {
            if (_root == null) { Log("WeaponViewerRig: Resize after Dispose - ignored"); return false; }
            int w = Mathf.Clamp(widthPx, 16, 4096), h = Mathf.Clamp(heightPx, 16, 4096);
            if (_texture != null && _texture.width == w && _texture.height == h) return true;
            if (_texture != null) { _camera.targetTexture = null; _texture.Release(); UnityEngine.Object.Destroy(_texture); }
            _texture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "WhipItUp_Viewer", antiAliasing = 4 };
            _texture.Create();
            _camera.targetTexture = _texture;
            Log("WeaponViewerRig: texture " + w + "x" + h + " px (asked " + widthPx + "x" + heightPx + ")");
            return true;
        }

        public bool Render(float yawDeg, float pitchDeg, float zoom, float panX, float panY, float panZ)
        {
            if (_root == null || _texture == null) { Log("WeaponViewerRig: Render skipped (" + (_root == null ? "disposed" : "no texture yet") + ")"); return false; }
            float aspect = (float)_texture.width / _texture.height;
            float fit = ViewerMath.FitDistance(_halfWidth, _halfHeight, _radius, FieldOfView, aspect, _settings.Margin);
            float distance = fit / Mathf.Max(0.01f, zoom);
            // pan is in view heights at zoom 1: one of them = the height the camera sees at the weapon's centre when it stands `fit` metres away
            float viewHeight = 2f * fit * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 pan = new Vector3(panX, panY, panZ) * viewHeight;
            float x, y, z;
            ViewerMath.Orbit(yawDeg, pitchDeg, distance, out x, out y, out z);
            Vector3 target = _pivot.transform.position + pan;          // the point the camera looks at (the weapon's centre when not panned)
            _camera.transform.position = target + new Vector3(x, y, z);
            _camera.transform.LookAt(target, Vector3.up);
            // the nearest weapon point is at least this far from the camera; the near plane stays just in front of it
            _camera.nearClipPlane = Mathf.Clamp(distance - pan.magnitude - _radius * 1.2f, 0.01f, 0.5f);
            RenderLit();
            _renders++;
            float now = Time.realtimeSinceStartup;
            if (now - _lastLog >= LogEverySeconds)
            {
                _lastLog = now;
                Log("WeaponViewerRig: render #" + _renders + " of " + (_weapon != null ? _weapon.ToString() : "nothing") + ": yaw " + F(yawDeg) + " / pitch " + F(pitchDeg)
                    + " deg, zoom x" + F(zoom) + ", fit " + F(fit) + " m, distance " + F(distance) + " m, aspect " + F(aspect) + ", pan " + V(pan) + " m (" + V(new Vector3(panX, panY, panZ))
                    + " view heights of " + F(viewHeight) + " m), near " + F(_camera.nearClipPlane) + " m, model " + (_model != null ? "shown" : "none"));
            }
            return true;
        }

        // The scene's ambient light and fog belong to the world (night = dark, mist = grey): for this one render they are set to a fixed
        // studio look and put back right after, so the weapon looks the same at any time of day.
        void RenderLit()
        {
            var mode = RenderSettings.ambientMode;
            var ambient = RenderSettings.ambientLight;
            float intensity = RenderSettings.ambientIntensity;
            bool fog = RenderSettings.fog;
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Ambient;
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.fog = false;
                _camera.Render();
            }
            finally
            {
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientLight = ambient;
                RenderSettings.ambientIntensity = intensity;
                RenderSettings.fog = fog;
            }
        }

        public void Dispose()
        {
            if (_root == null) return;
            if (_camera != null) _camera.targetTexture = null;
            if (_texture != null) { _texture.Release(); UnityEngine.Object.Destroy(_texture); _texture = null; }
            UnityEngine.Object.Destroy(_root);                       // camera, lights and model are its children
            _root = null; _model = null; _camera = null;
            Log("WeaponViewerRig: disposed after " + _renders + " render(s)");
        }

        static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }
        static string V(Vector3 v) { return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")"; }
        void Log(string s) { if (_log != null) _log(s); }
    }
}
