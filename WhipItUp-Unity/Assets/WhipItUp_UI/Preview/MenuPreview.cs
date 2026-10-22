using System;
using System.Collections.Generic;
using System.Globalization;
using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WhipItUp.Ui
{
    /// <summary>
    /// Unity-only preview of the mod's menu (NOT compiled into the mod: only Runtime/ is). Add this component to any GameObject of an empty
    /// scene and press Play: the menu opens in the Game view in the same look as in game (game fonts from Assets/Kydl.Ui/Preview/GameAssets).
    /// Set the Game view to 1920x1080 (the panel scales from that like in game).
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const string FontFolder = "Assets/Kydl.Ui/Preview/GameAssets/";

        [Tooltip("Tab shown when Play starts: 0 = Viewer, 1 = Details (the preview has no click tool for the tab bar, so this checks the other tab without clicking).")]
        [SerializeField] int startTab;

        [Tooltip("Family the menu starts on: Paddle, Flogger or Whip; empty = Paddle (preview only).")]
        [SerializeField] string startFamily = "";

        [Tooltip("UI pixels the body is scrolled once the layout exists (preview only: the Unity MCP has no scroll tool). 0 = top.")]
        [SerializeField] float startScroll;

        [Tooltip("Layer the 3D viewer's camera and model use (preview only; the game picks its own in S5).")]
        [SerializeField] int viewerLayer = 31;

        [Tooltip("Preview only: input sent to the menu as real UI Toolkit events when this text changes in Play mode (the Unity MCP has no mouse). "
            + "Commands separated by ';': 'drag dx dy' (UI px on the viewport), 'wheel n' (n > 0 = toward the player = zoom out), 'click <button text>' (e.g. '+', 'Reset', a drop-down entry).")]
        [SerializeField] string inputCommand = "";
        string _lastCommand = "";

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            root.Add(MainMenuView.Build(UiText.English, StartWith(SampleWeapons(), startFamily), () => Debug.Log("[MenuPreview] close pressed"), startTab, startScroll,
                MakeScene, new ViewerSettings()));
            Debug.Log("[MenuPreview] WhipItUp menu shown, start tab " + startTab);
        }

        void OnDisable() { UiKit.Reset(); }

        void Update()
        {
            if (inputCommand == _lastCommand) return;
            _lastCommand = inputCommand;
            foreach (var c in (inputCommand ?? "").Split(';'))
                if (c.Trim().Length > 0) RunCommand(c.Trim());
        }

        // One preview input command (see inputCommand's tooltip): IMGUI events turned into UI Toolkit pointer / wheel events, sent to the element.
        static void RunCommand(string command)
        {
            var root = UiKit.Root;
            var parts = command.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string verb = parts[0].ToLowerInvariant(), arg = parts.Length > 1 ? parts[1].Trim() : "";
            var view = root != null ? root.Q("whipitup-viewport-image") : null;
            if (verb == "drag" || verb == "wheel")
            {
                if (view == null) { Debug.LogWarning("[MenuPreview] input '" + command + "': no 3D viewport (Viewer tab hidden or no scene)"); return; }
                Vector2 c = view.worldBound.center;
                var n = arg.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (verb == "wheel")
                {
                    float d = n.Length > 0 ? float.Parse(n[0], CultureInfo.InvariantCulture) : 1f;
                    Send(view, new Event { type = EventType.ScrollWheel, delta = new Vector2(0f, d), mousePosition = c });
                    Debug.Log("[MenuPreview] input: wheel " + d + " at " + c);
                    return;
                }
                float dx = n.Length > 0 ? float.Parse(n[0], CultureInfo.InvariantCulture) : 0f, dy = n.Length > 1 ? float.Parse(n[1], CultureInfo.InvariantCulture) : 0f;
                Send(view, new Event { type = EventType.MouseDown, button = 0, mousePosition = c });
                const int steps = 5;
                for (int i = 1; i <= steps; i++) Send(view, new Event { type = EventType.MouseDrag, button = 0, mousePosition = c + new Vector2(dx, dy) * i / steps });
                Send(view, new Event { type = EventType.MouseUp, button = 0, mousePosition = c + new Vector2(dx, dy) });
                Debug.Log("[MenuPreview] input: drag " + dx + ", " + dy + " UI px from " + c);
                return;
            }
            if (verb == "click")
            {
                Button hit = null;
                if (root != null) root.Query<Button>().ForEach(b => { if (hit == null && b.resolvedStyle.display != DisplayStyle.None && string.Equals((b.text ?? "").Trim(), arg, StringComparison.OrdinalIgnoreCase)) hit = b; });
                if (hit == null) { Debug.LogWarning("[MenuPreview] input '" + command + "': no button with the text '" + arg + "'"); return; }
                Vector2 c = hit.worldBound.center;
                Send(hit, new Event { type = EventType.MouseDown, button = 0, mousePosition = c });
                Send(hit, new Event { type = EventType.MouseUp, button = 0, mousePosition = c });
                Debug.Log("[MenuPreview] input: clicked '" + arg + "' at " + c);
                return;
            }
            Debug.LogWarning("[MenuPreview] input: unknown command '" + command + "' (drag dx dy / wheel n / click text)");
        }

        static void Send(VisualElement target, Event imgui)
        {
            EventBase e;
            switch (imgui.type)
            {
                case EventType.MouseDown: e = PointerDownEvent.GetPooled(imgui); break;
                case EventType.MouseUp: e = PointerUpEvent.GetPooled(imgui); break;
                case EventType.ScrollWheel: e = WheelEvent.GetPooled(imgui); break;
                default: e = PointerMoveEvent.GetPooled(imgui); break;
            }
            using (e) target.SendEvent(e);
        }

        // The Viewer tab's 3D scene: the same rig the game uses, with the Unity project's FBX + rank textures as the model.
        IWeaponScene MakeScene()
        {
            return new WeaponViewerRig(MakeModel, viewerLayer, new ViewerSettings(), UiKit.Track, s => Debug.Log("[MenuPreview] " + s));
        }

        // Preview model: Assets/WhipItUp/<Family>/<Family>.fbx with a Standard material of that rank's albedo + the shared normal / metal-gloss maps.
        static GameObject MakeModel(WeaponInfo weapon, bool coiled)
        {
            // the FBX is the rest pose: the coil pose is baked from the game's item bones, so the preview shows no difference (the switch only logs here)
            Debug.Log("[MenuPreview] model of " + weapon + " asked in the " + (coiled ? "coiled" : "rest") + " pose (the preview always shows the FBX rest pose)");
#if UNITY_EDITOR
            string folder = "Assets/WhipItUp/" + weapon.Family + "/";
            string prefix = folder + weapon.Family.ToLowerInvariant() + "_";
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(folder + weapon.Family + ".fbx");
            if (fbx == null) { Debug.LogWarning("[MenuPreview] no model " + folder + weapon.Family + ".fbx"); return null; }
            var go = Instantiate(fbx);
            go.name = "Preview_" + weapon.Family + "_V" + weapon.Rank;
            var mat = new Material(Shader.Find("Standard")) { name = go.name };
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "Albedo_V" + weapon.Rank + ".png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "Normal.png");
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "MetalGloss.png");
            mat.SetTexture("_MainTex", albedo);
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            if (metal != null) { mat.SetTexture("_MetallicGlossMap", metal); mat.EnableKeyword("_METALLICGLOSSMAP"); }
            int n = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                n++;
            }
            Debug.Log("[MenuPreview] model " + go.name + ": " + n + " renderer(s), albedo " + (albedo != null ? albedo.name : "MISSING") + ", normal " + (normal != null) + ", metal " + (metal != null));
            return go;
#else
            return null;
#endif
        }

        // The list with the weapons of `family` first (the menu starts on the first weapon of the list); empty or unknown family = unchanged.
        static List<WeaponInfo> StartWith(List<WeaponInfo> all, string family)
        {
            if (string.IsNullOrEmpty(family)) return all;
            var first = all.FindAll(w => w.Family == family);
            if (first.Count == 0) { Debug.LogWarning("[MenuPreview] startFamily '" + family + "' has no sample weapons, keeping the list order"); return all; }
            var list = new List<WeaponInfo>(first);
            list.AddRange(all.FindAll(w => w.Family != family));
            Debug.Log("[MenuPreview] the list starts with " + family + " (" + first.Count + " weapon(s) first)");
            return list;
        }

        // Preview-only data (the game builds the real list in src/Ui/WeaponCatalog.cs from the item names): 3 families x 7 ranks of sample text; one
        // entry carries the longest real name and description so wrapping shows.
        static List<WeaponInfo> SampleWeapons()
        {
            var list = new List<WeaponInfo>();
            foreach (var family in WeaponSelection.Families)
                for (int rank = 1; rank <= 7; rank++)
                {
                    bool longest = family == "Whip" && rank == 6;
                    var info = new WeaponInfo(family, rank,
                        longest ? "Charred and Dangerous" : family + " sample " + rank,
                        longest ? "Warning: prolonged exposure may cause singed eyebrows, colourful language, and sudden respect." : "Sample description of " + family + " V" + rank + ".");
                    WeaponDetailsBuilder.Fill(info, SampleStats(family, rank), UiText.English, line => Debug.Log("[MenuPreview] " + line));
                    list.Add(info);
                }
            return list;
        }

        // Preview-only numbers (the game reads the real ones in src/Ui/WeaponStatsReader.cs): they only show how the Details tab lays out
        // quality 1 beside quality 4, a bleed line, attack 2 with an effect line and a recipe with several ingredients. Not the real balance.
        static WeaponStats SampleStats(string family, int rank)
        {
            bool paddle = family == "Paddle";
            float baseDamage = 20f + 10f * (rank - 1);
            var s = new WeaponStats
            {
                Family = family, Rank = rank, Skill = paddle ? "Clubs" : "Whips", Weight = 2f, OneHanded = true, MaxQuality = 4,
                AttackForce = 20f + 5f * rank, BlockPower = 10f + 2f * rank, ParryBonus = 2f, DeflectionForce = 15f + rank,
                Durability = 200f + 50f * (rank - 1), DurabilityPerLevel = 40f,
                Station = rank < 3 ? "Workbench" : rank < 6 ? "Forge" : "Black forge", StationLevel = 1 + (rank - 1) % 3,
                BleedPerSecond = paddle ? 0f : 4f + rank, BleedSeconds = paddle ? 0f : 5f,
                Attack2 = new WeaponStats.AttackLine { DamageMultiplier = 2.5f, ForceMultiplier = 2f, StaggerMultiplier = 0.8f, Stamina = 18f, Range = 2.2f },
            };
            s.Damage.Add(new WeaponStats.DamageLine(paddle ? "blunt" : "slash", baseDamage, baseDamage * 0.2f));
            s.Attack1.StaggerMultiplier = 0.4f;
            s.Attack1.Stamina = 10f + rank;
            s.Attack1.Range = paddle ? 2f : family == "Whip" ? 4.8f : 2.6f;
            s.Ingredients.Add(new WeaponStats.IngredientLine("Leather scraps", 10 + 2 * rank, 5));
            s.Ingredients.Add(new WeaponStats.IngredientLine("Resin", 2 + rank, 1));
            return s;
        }

        // UiKit finds the game fonts by name among the loaded fonts (Norse, Norsebold, NotoEmoji-Regular): load them first.
        static void LoadGameFonts()
        {
#if UNITY_EDITOR
            foreach (var f in new[] { "Norse.otf", "Norsebold.otf", "NotoEmoji-Regular.ttf" })
                if (AssetDatabase.LoadAssetAtPath<Font>(FontFolder + f) == null) Debug.LogWarning("[MenuPreview] font missing: " + FontFolder + f + " (Kydl > Sync shared UI kit)");
#endif
        }
    }
}
