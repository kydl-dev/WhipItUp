// Editor-only. Import settings + AssetBundle assignment for the mod's art (Assets/WhipItUp/<Item>/).
//  * Menu "Valheim/Set up WhipItUp assets": for every item folder under Assets/WhipItUp
//      - *_Albedo_V#.png  : sRGB colour, mipmaps, 1024 max
//      - *_Normal.png     : Normal map (tangent space, +Y up as written by the Blender texture scripts)
//      - *_MetalGloss.png : linear (sRGB off), alpha = smoothness
//      - *.fbx            : scale 1 (metres), no materials, no animation, read/write off, tangents calculated if missing
//      - *.ogg / *.mp3 / *.wav : Decompress On Load + preload (the mod reads the samples to trim / boost them), mono off
//    and assigns all of them to the AssetBundle "whipitup" (the mod's only bundle, PROJECT_RULES rule 13).
//  * Logs one line per asset and a summary with the imported mesh bounds (check: Paddle is about 0.56 m long).
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhipItUp.EditorTools
{
    public static class WhipItUpAssetSetup
    {
        private const string Root = "Assets/WhipItUp";
        private const string BundleName = "whipitup";

        [MenuItem("Valheim/Set up WhipItUp assets")]
        public static void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                Debug.LogWarning("[WhipItUpAssetSetup] no folder " + Root + " - nothing to set up");
                return;
            }

            var guids = AssetDatabase.FindAssets("", new[] { Root });
            // Assets/WhipItUp/Anim (T-013 whip animation clips) has its own import rules: WhipAnimSetup
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p) && !p.StartsWith(WhipAnimSetup.Folder + "/")).Distinct().ToArray();
            int textures = 0, models = 0, sounds = 0, skipped = 0;
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer is TextureImporter ti) { SetUpTexture(path, ti); textures++; }
                else if (importer is ModelImporter mi) { SetUpModel(path, mi); models++; }
                else if (importer is AudioImporter ai) { SetUpAudio(path, ai); sounds++; }
                else { skipped++; Debug.Log("[WhipItUpAssetSetup] skipped (not a texture/model): " + path); continue; }
                importer.assetBundleName = BundleName;
                importer.SaveAndReimport();
            }

            foreach (var path in paths.Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)))
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var skinned = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
                foreach (var smr in skinned)
                    Debug.Log("[WhipItUpAssetSetup] " + path + ": skinned renderer '" + smr.name + "', " + smr.bones.Length + " bones, root bone " + (smr.rootBone != null ? smr.rootBone.name : "none")
                              + ", model root rotation " + root.transform.localRotation.eulerAngles.ToString("F1") + " scale " + root.transform.localScale.ToString("F3"));
                var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
                if (mesh == null) { Debug.LogError("[WhipItUpAssetSetup] no mesh inside " + path); continue; }
                Debug.Log("[WhipItUpAssetSetup] mesh '" + mesh.name + "' from " + path + ": " + mesh.vertexCount + " verts, " +
                          (mesh.triangles.Length / 3) + " tris, bounds " + mesh.bounds.size.ToString("F3") + " centre " + mesh.bounds.center.ToString("F3") +
                          ", tangents " + (mesh.tangents.Length > 0) + ", uv " + (mesh.uv.Length > 0));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[WhipItUpAssetSetup] done: " + textures + " texture(s), " + models + " model(s), " + sounds + " sound(s) set up and assigned to bundle '" + BundleName + "', " + skipped + " other file(s) skipped");
        }

        private static void SetUpTexture(string path, TextureImporter ti)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            if (name.EndsWith("_Normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (name.EndsWith("_MetalGloss"))
            {
                ti.sRGBTexture = false;
            }
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.isReadable = false;
            Debug.Log("[WhipItUpAssetSetup] texture " + path + ": type " + ti.textureType + ", sRGB " + ti.sRGBTexture);
        }

        private static void SetUpAudio(string path, AudioImporter ai)
        {
            var settings = ai.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            // the flogger and whip slaps are used exactly as the user's files (user: "don't cut or anything"): PCM, no re-compression
            var stem = Path.GetFileNameWithoutExtension(path);
            bool asIs = stem == "sfx_flogger_slap" || stem == "sfx_whip_slap";
            settings.compressionFormat = asIs ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
            settings.quality = asIs ? 1f : 0.7f;
            settings.preloadAudioData = true;
            ai.defaultSampleSettings = settings;
            ai.forceToMono = false;
            ai.loadInBackground = false;
            Debug.Log("[WhipItUpAssetSetup] sound " + path + ": Decompress On Load, " + (asIs ? "PCM (as the file is)" : "Vorbis 70%") + ", preload");
        }

        private static void SetUpModel(string path, ModelImporter mi)
        {
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            // skinned items (an armature in the FBX, e.g. the flogger's fall bones) need a rig for their SkinnedMeshRenderer: Generic, no avatar
            bool skinned = Path.GetFileNameWithoutExtension(path) == "Flogger" || Path.GetFileNameWithoutExtension(path) == "Whip";
            mi.animationType = skinned ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            if (skinned) mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            mi.optimizeGameObjects = false;      // the mod drives the bone transforms by name
            mi.isReadable = false;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.importNormals = ModelImporterNormals.Import;
            Debug.Log("[WhipItUpAssetSetup] model " + path + ": scale 1 (file scale), no materials, no animation, rig " + mi.animationType + ", tangents Mikk");
        }
    }
}
