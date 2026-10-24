// Editor-only (T-013). Import settings for the whip animation FBX files in Assets/WhipItUp/Anim, made by
// WhipItUp-Blender/scripts/anim_export.py:
//  * WhipAnim_Body.fbx: the Valheim player skeleton (Mixamo names) + body proxy + the body clips -> Humanoid, avatar from this model,
//    every take imported as a clip (S5 adds the game's animation events).
//  * WhipAnim_Lash.fbx: the whip rig + the lash clips -> Generic (bone paths equal Whip.fbx).
// Menu "Valheim/Set up WhipItUp animations" sets them up, assigns them to the "whipitup" bundle and logs the avatar check
// (valid, human, mapped bones) and every clip (length, frame rate, events).
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
            var paths = AssetDatabase.FindAssets("t:Model", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
            Debug.Log("[WhipAnimSetup] " + paths.Length + " model(s) in " + Folder + ": " + string.Join(", ", paths));
            foreach (var path in paths)
            {
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) { Debug.LogWarning("[WhipAnimSetup] not a model importer: " + path); continue; }
                bool body = Path.GetFileNameWithoutExtension(path).EndsWith("_Body");
                mi.globalScale = 1f;
                mi.useFileScale = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = true;
                mi.optimizeGameObjects = false;
                mi.isReadable = false;
                mi.animationType = body ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
                mi.avatarSetup = body ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar;
                mi.assetBundleName = BundleName;
                mi.SaveAndReimport();
                Debug.Log("[WhipAnimSetup] " + path + ": rig " + mi.animationType + ", avatar " + mi.avatarSetup + ", animation on, " + mi.defaultClipAnimations.Length + " take(s) in the file");
                Report(path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[WhipAnimSetup] done");
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
