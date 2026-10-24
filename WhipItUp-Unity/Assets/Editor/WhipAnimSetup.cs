// Editor-only (T-013). Import settings for the whip animation FBX files in Assets/WhipItUp/Anim, made by
// WhipItUp-Blender/scripts/anim_export.py:
//  * WhipAnim_Body.fbx: the Valheim player skeleton (Mixamo names) + body proxy + the body clips -> Humanoid, avatar from this model,
//    every take imported as a clip (S5 adds the game's animation events).
//  * WhipAnim_Lash.fbx: the whip rig + the lash clips -> Generic (bone paths equal Whip.fbx).
// Menu "Valheim/Set up WhipItUp animations" sets them up, assigns them to the "whipitup" bundle and logs the avatar check
// (valid, human, mapped bones) and every clip (length, frame rate, events).
// Body clips (PLAN T-013 S2 table): take "<rig>|Body_<name>" -> clip "Body_<name>", no loop, root motion baked into the pose (the game
// moves the player, not the clip), and the game's animation events (CharacterAnimEvent: Speed, TrailOn, Hit / OnAttackTrigger, TrailOff,
// Chain) at the frames of the vanilla clip each one replaces.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhipItUp.EditorTools
{
    public static class WhipAnimSetup
    {
        public const string Folder = "Assets/WhipItUp/Anim";
        private const string BundleName = "whipitup";

        [MenuItem("Valheim/Set up WhipItUp animations")]
        public static void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) { Debug.LogWarning("[WhipAnimSetup] no folder " + Folder + " - nothing to set up"); return; }
            // the T-pose rig first: the body clips copy its avatar
            var paths = AssetDatabase.FindAssets("t:Model", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .OrderBy(p => p.EndsWith("_Rig.fbx") ? 0 : 1).ToArray();
            Debug.Log("[WhipAnimSetup] " + paths.Length + " model(s) in " + Folder + ": " + string.Join(", ", paths));
            foreach (var path in paths)
            {
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) { Debug.LogWarning("[WhipAnimSetup] not a model importer: " + path); continue; }
                string stem = Path.GetFileNameWithoutExtension(path);
                bool rig = stem.EndsWith("_Rig");
                bool body = stem.EndsWith("_Body");
                mi.globalScale = 1f;
                mi.useFileScale = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = !rig;
                mi.optimizeGameObjects = false;
                mi.isReadable = false;
                mi.animationType = rig || body ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                mi.avatarSetup = rig ? ModelImporterAvatarSetup.CreateFromThisModel : body ? ModelImporterAvatarSetup.CopyFromOther : ModelImporterAvatarSetup.NoAvatar;
                if (body)
                {
                    var src = AssetDatabase.LoadAllAssetsAtPath(Folder + "/WhipAnim_Rig.fbx").OfType<Avatar>().FirstOrDefault();
                    if (src == null) Debug.LogWarning("[WhipAnimSetup] no avatar in " + Folder + "/WhipAnim_Rig.fbx (export it: anim_export.py PART rig) - " + path + " keeps its own");
                    else mi.sourceAvatar = src;
                    Debug.Log("[WhipAnimSetup] " + path + ": avatar copied from " + (src != null ? src.name : "nothing"));
                }
                mi.assetBundleName = BundleName;
                if (body) SetUpBodyClips(path, mi);
                mi.SaveAndReimport();
                Debug.Log("[WhipAnimSetup] " + path + ": rig " + mi.animationType + ", avatar " + mi.avatarSetup + ", animation on, " + mi.defaultClipAnimations.Length + " take(s) in the file");
                Report(path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WhipAnimSetup] done");
        }

        // frame (30 fps) -> event; the frames come from the vanilla clips (rip): Attack1_2, Attack2, Attack3, throw_spear
        private static readonly System.Collections.Generic.Dictionary<string, (int frames, (int f, string fn, float p)[] ev)> Events =
            new System.Collections.Generic.Dictionary<string, (int, (int, string, float)[])>
            {
                { "Body_W1_SideFlick", (34, new[] { (11, "Speed", 1.2f), (12, "TrailOn", 0f), (14, "Hit", 0f), (18, "Speed", 1f), (21, "TrailOff", 0f), (22, "Chain", 0f) }) },
                { "Body_W2_Backhand", (25, new[] { (6, "TrailOn", 0f), (6, "Speed", 1.1f), (8, "Hit", 0f), (12, "TrailOff", 0f), (13, "Speed", 1f), (17, "Chain", 0f) }) },
                { "Body_W3_Overhand", (25, new[] { (1, "Speed", 0.5f), (4, "Speed", 1.5f), (5, "TrailOn", 0f), (8, "OnAttackTrigger", 0f), (11, "TrailOff", 0f), (16, "Speed", 1f) }) },
                { "Body_G_LashGrab", (34, new[] { (0, "Speed", 1f), (15, "Speed", 1.5f), (15, "TrailOn", 1f), (22, "OnAttackTrigger", 0f), (24, "Speed", 1f) }) },
            };

        private static void SetUpBodyClips(string path, ModelImporter mi)
        {
            var takes = mi.defaultClipAnimations;
            var clips = new System.Collections.Generic.List<ModelImporterClipAnimation>();
            foreach (var t in takes)
            {
                string name = t.takeName.Contains("|") ? t.takeName.Substring(t.takeName.LastIndexOf('|') + 1) : t.takeName;
                if (!Events.TryGetValue(name, out var spec)) { Debug.LogWarning("[WhipAnimSetup] take '" + t.takeName + "' is not a known body clip - skipped (known: " + string.Join(", ", Events.Keys) + ")"); continue; }
                t.name = name;
                t.loopTime = false;
                t.lockRootRotation = true; t.keepOriginalOrientation = true;
                t.lockRootHeightY = true; t.keepOriginalPositionY = true;
                t.lockRootPositionXZ = true; t.keepOriginalPositionXZ = true;
                float last = Mathf.Max(1, spec.frames - 1);
                t.events = spec.ev.Select(e => new AnimationEvent { functionName = e.fn, floatParameter = e.p, time = e.f / last }).ToArray();
                if (Mathf.Abs((t.lastFrame - t.firstFrame) - last) > 0.5f)
                    Debug.LogWarning("[WhipAnimSetup] " + name + ": take is " + (t.lastFrame - t.firstFrame) + " frames long, expected " + last + " (events are placed by normalized time " + "frame / " + last + ")");
                clips.Add(t);
                Debug.Log("[WhipAnimSetup] body clip " + name + " (take '" + t.takeName + "', frames " + t.firstFrame + ".." + t.lastFrame + "): no loop, root baked, events "
                          + string.Join(", ", spec.ev.Select(e => e.fn + (e.fn == "Speed" ? "(" + e.p + ")" : "") + "@f" + e.f)));
            }
            mi.clipAnimations = clips.ToArray();
            Debug.Log("[WhipAnimSetup] " + path + ": " + clips.Count + " of " + takes.Length + " take(s) set up as body clips");
            if (clips.Count != Events.Count) Debug.LogWarning("[WhipAnimSetup] expected " + Events.Count + " body clips, got " + clips.Count);
        }

        private static void Report(string path)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var avatar in all.OfType<Avatar>())
            {
                var hd = avatar.humanDescription;
                int mapped = hd.human != null ? hd.human.Length : 0;
                Debug.Log("[WhipAnimSetup] " + path + ": avatar '" + avatar.name + "' valid " + avatar.isValid + ", human " + avatar.isHuman + ", " + mapped + " human bone(s) mapped"
                          + (mapped > 0 ? " (e.g. " + string.Join(", ", hd.human.Take(6).Select(h => h.humanName + "=" + h.boneName)) + ")" : ""));
                if (avatar.isHuman && mapped < 15) Debug.LogWarning("[WhipAnimSetup] expected at least 15 mapped human bones (Unity's required set), got " + mapped);
            }
            foreach (var clip in all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                var events = AnimationUtility.GetAnimationEvents(clip);
                Debug.Log("[WhipAnimSetup] clip '" + clip.name + "' in " + Path.GetFileName(path) + ": " + clip.length.ToString("0.###") + " s at " + clip.frameRate + " fps, humanMotion " + clip.humanMotion
                          + ", " + events.Length + " event(s)" + (events.Length > 0 ? " [" + string.Join(", ", events.Select(e => e.functionName + "@" + e.time.ToString("0.###"))) + "]" : ""));
            }
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root != null)
            {
                var hips = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Hips");
                var hand = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "RightHand");
                Debug.Log("[WhipAnimSetup] " + path + ": Hips at " + (hips != null ? hips.position.ToString("F3") : "missing") + ", RightHand at " + (hand != null ? hand.position.ToString("F3") : "missing")
                          + " (game avatar: Hips (0.010, 0.987, -0.014), RightHand (0.847, 1.629, -0.068))");
            }
        }
    }
}
