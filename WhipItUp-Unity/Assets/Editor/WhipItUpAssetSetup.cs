// Editor-only. Import settings + AssetBundle assignment for the mod's art (Assets/WhipItUp/<Item>/).
//  * Menu "Valheim/Set up WhipItUp assets": for every item folder under Assets/WhipItUp
//      - *_Albedo_V#.png  : sRGB colour, mipmaps, 1024 max
//      - *_Normal.png     : Normal map (tangent space, +Y up as written by the Blender texture scripts)
//      - *_MetalGloss.png : linear (sRGB off), alpha = smoothness
//      - *.fbx            : scale 1 (metres), no materials, no animation, read/write off, tangents calculated if missing
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
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().ToArray();
            int textures = 0, models = 0, skipped = 0;
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer is TextureImporter ti) { SetUpTexture(path, ti); textures++; }
                else if (importer is ModelImporter mi) { SetUpModel(path, mi); models++; }
                else { skipped++; Debug.Log("[WhipItUpAssetSetup] skipped (not a texture/model): " + path); continue; }
                importer.assetBundleName = BundleName;
                importer.SaveAndReimport();
            }

            foreach (var path in paths.Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)))
            {
                var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
                if (mesh == null) { Debug.LogError("[WhipItUpAssetSetup] no mesh inside " + path); continue; }
                Debug.Log("[WhipItUpAssetSetup] mesh '" + mesh.name + "' from " + path + ": " + mesh.vertexCount + " verts, " +
                          (mesh.triangles.Length / 3) + " tris, bounds " + mesh.bounds.size.ToString("F3") + " centre " + mesh.bounds.center.ToString("F3") +
                          ", tangents " + (mesh.tangents.Length > 0) + ", uv " + (mesh.uv.Length > 0));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[WhipItUpAssetSetup] done: " + textures + " texture(s), " + models + " model(s) set up and assigned to bundle '" + BundleName + "', " + skipped + " other file(s) skipped");
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

        private static void SetUpModel(string path, ModelImporter mi)
        {
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = false;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.importNormals = ModelImporterNormals.Import;
            Debug.Log("[WhipItUpAssetSetup] model " + path + ": scale 1 (file scale), no materials, no animation, tangents Mikk");
        }
    }
}
