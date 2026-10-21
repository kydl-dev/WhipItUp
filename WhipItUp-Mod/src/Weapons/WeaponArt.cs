using System;
using System.Collections.Generic;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// One weapon family's art from the mod's AssetBundle (built by WhipItUp-Unity, assets under Assets/WhipItUp/&lt;Family&gt;/):
    /// the mesh (the FBX model named &lt;Family&gt;), one albedo per rank (&lt;family&gt;_Albedo_V#), a shared normal map and metal/gloss map.
    /// Builds each rank's material from the vanilla item's own material so it keeps Valheim's item shader (a plain Standard shader
    /// can render wrong in Valheim), and puts the mesh on a child of every vanilla mesh holder of the cloned prefab.
    /// </summary>
    internal sealed class WeaponArt
    {
        public readonly string Family;
        public Mesh Mesh { get; private set; }
        /// True when the model is a skinned mesh with bones (flogger, whip): the whole model is instanced and FallChains drives its bones.
        public bool Skinned { get; private set; }
        GameObject _model;
        readonly Dictionary<int, Texture2D> _albedo = new Dictionary<int, Texture2D>();
        Texture2D _normal, _metalGloss;
        /// The visual children made so far (all ranks): ApplyAlignment re-positions them when a Tunable changes.
        readonly List<Transform> _visuals = new List<Transform>();
        public const string VisualName = "WhipItUpVisual";

        /// Extra components for each skinned visual (the whip adds WhipMotion). Set by the family before Apply.
        public Action<GameObject> DecorateVisual;

        public WeaponArt(string family) { Family = family; }

        /// Loads everything; logs what is missing. Expected outcome: a mesh and all 7 albedos (returns that check).
        public bool Load(AssetBundle bundle)
        {
            if (bundle == null) { ModLog.Error("WeaponArt: no asset bundle - " + Family + " cannot be added (rebuild the bundle in Unity: Valheim > Build AssetBundles)"); return Mesh != null; }
            var model = bundle.LoadAsset<GameObject>(Family);
            _model = model;
            var filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            var skin = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            Skinned = skin != null;
            Mesh = filter != null ? filter.sharedMesh : skin != null ? skin.sharedMesh : null;
            if (Mesh == null) ModLog.Error("WeaponArt: mesh '" + Family + "' is not in the bundle");
            else ModLog.Verbose("WeaponArt: mesh '" + Mesh.name + "' " + Mesh.vertexCount + " verts, bounds " + Mesh.bounds.size.ToString("F3"));
            string prefix = Family.ToLowerInvariant();
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                var tex = bundle.LoadAsset<Texture2D>(prefix + "_Albedo_V" + rank);
                if (tex == null) { ModLog.Error("WeaponArt: texture '" + prefix + "_Albedo_V" + rank + "' is not in the bundle - " + Ladder.PrefabName(Family, rank) + " will be skipped"); continue; }
                _albedo[rank] = tex;
            }
            _normal = bundle.LoadAsset<Texture2D>(prefix + "_Normal");
            _metalGloss = bundle.LoadAsset<Texture2D>(prefix + "_MetalGloss");
            if (_normal == null) ModLog.Warn("WeaponArt: '" + prefix + "_Normal' missing - " + Family + " will look flat");
            if (_metalGloss == null) ModLog.Warn("WeaponArt: '" + prefix + "_MetalGloss' missing - " + Family + " will have no metal shine");
            ModLog.Verbose("WeaponArt: " + Family + " loaded, " + _albedo.Count + " of " + Ladder.Ranks + " albedo(s), normal " + (_normal != null) + ", metal/gloss " + (_metalGloss != null));
            return Mesh != null && _albedo.Count == Ladder.Ranks;
        }

        public bool HasRank(int rank) { return _albedo.ContainsKey(rank); }

        /// Replaces every mesh of the cloned prefab with this family's mesh + the rank's material.
        /// Expected outcome: at least one visual made (returns that check).
        public bool Apply(GameObject prefab, int rank, Func<Vector3> rotation, Func<float> grip, Func<float> scale)
        {
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (filters.Length == 0 || renderers.Length == 0) { ModLog.Error("WeaponArt: '" + prefab.name + "' has no MeshFilter/MeshRenderer to replace"); return filters.Length > 0 && renderers.Length > 0; }
            Texture2D albedo;
            if (!_albedo.TryGetValue(rank, out albedo)) { ModLog.Error("WeaponArt: no albedo for rank " + rank + " of " + Family); return albedo != null; }

            var material = new Material(renderers[0].sharedMaterial) { name = Ladder.PrefabName(Family, rank) + "_mat" };
            material.mainTexture = albedo;
            material.color = Color.white;
            SetTexture(material, "_BumpMap", _normal, "_NORMALMAP");
            SetTexture(material, "_MetallicGlossMap", _metalGloss, "_METALLICGLOSSMAP");

            int made = 0;
            if (Skinned) return ApplySkinned(prefab, filters, material, rotation, grip, scale);
            foreach (var f in filters)
            {
                var old = f.GetComponent<MeshRenderer>();
                // A CHILD carries the mesh: Valheim resets the rotation of the "attach" node on equip, so rotating our child is reliable.
                var visual = new GameObject(VisualName);
                visual.layer = f.gameObject.layer;
                visual.transform.SetParent(f.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = Mesh;
                var r = visual.AddComponent<MeshRenderer>();
                r.sharedMaterials = new[] { material };
                if (old != null) { r.shadowCastingMode = old.shadowCastingMode; r.receiveShadows = old.receiveShadows; UnityEngine.Object.DestroyImmediate(old); }
                UnityEngine.Object.DestroyImmediate(f);
                _visuals.Add(visual.transform);
                Align(visual.transform, rotation(), grip(), scale());
                made++;
            }
            ModLog.Verbose("WeaponArt: " + prefab.name + ": " + made + " mesh(es) replaced, albedo " + albedo.name + ", shader " + material.shader.name);
            return made > 0;
        }

        /// Skinned model (flogger / whip): an instance of the whole bundle model (mesh + bones) under each vanilla mesh holder, with
        /// FallChains on it. Expected outcome: at least one visual made (returns that check).
        bool ApplySkinned(GameObject prefab, MeshFilter[] filters, Material material, Func<Vector3> rotation, Func<float> grip, Func<float> scale)
        {
            int made = 0;
            foreach (var f in filters)
            {
                var old = f.GetComponent<MeshRenderer>();
                var visual = UnityEngine.Object.Instantiate(_model, f.transform, false);
                visual.name = VisualName;
                foreach (var t in visual.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = f.gameObject.layer;
                foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.sharedMaterials = new[] { material };
                    smr.updateWhenOffscreen = true;           // falls swing outside the rest bounds
                    if (old != null) { smr.shadowCastingMode = old.shadowCastingMode; smr.receiveShadows = old.receiveShadows; }
                }
                var anim = visual.GetComponent<Animator>();
                if (anim != null) UnityEngine.Object.DestroyImmediate(anim);   // no animation: FallChains moves the bones
                if (DecorateVisual != null) DecorateVisual(visual);   // before FallChains so it finds its driver
                visual.AddComponent<FallChains>();
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
                UnityEngine.Object.DestroyImmediate(f);
                _visuals.Add(visual.transform);
                Align(visual.transform, rotation(), grip(), scale());
                made++;
            }
            ModLog.Verbose("WeaponArt: " + prefab.name + ": " + made + " skinned visual(s) with FallChains, material " + material.name + ", shader " + material.shader.name);
            return made > 0;
        }

        /// A bare object with the family mesh (rest pose) + the item's material for the icon render. Null if the item has no visual.
        public GameObject CreateIconSource(GameObject itemPrefab, string name)
        {
            Material mat = null;
            foreach (var r in itemPrefab.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.name != VisualName && (r.transform.parent == null || r.transform.parent.name != VisualName) && !(r is SkinnedMeshRenderer)) continue;
                mat = r.sharedMaterial;
                break;
            }
            if (mat == null || Mesh == null) return null;
            var go = new GameObject(name + "_IconSource");
            go.AddComponent<MeshFilter>().sharedMesh = Mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// Re-applies the hand alignment to every visual made so far (called when a Tunable changes in KydlTools).
        /// Expected outcome: every live visual aligned (returns that check).
        public bool ApplyAlignment(Vector3 rotation, float grip, float scale)
        {
            int done = 0, gone = 0;
            foreach (var t in _visuals)
            {
                if (t == null) { gone++; continue; }
                Align(t, rotation, grip, scale);
                done++;
            }
            _visuals.RemoveAll(t => t == null);
            ModLog.Verbose("WeaponArt: " + Family + " alignment rot " + rotation + " grip " + grip.ToString("0.###") + " scale " + scale.ToString("0.###") + " -> " + done + " visual(s), " + gone + " destroyed one(s) dropped");
            return _visuals.Count == done;
        }

        static void Align(Transform t, Vector3 rotation, float grip, float scale)
        {
            var q = Quaternion.Euler(rotation);
            t.localRotation = q;
            t.localScale = Vector3.one * scale;
            // slide along the model's long axis (+Y in Unity): bigger grip = hand further up the handle (toward the blade)
            t.localPosition = q * new Vector3(0f, -grip * scale, 0f);
        }

        /// Assigns a map and switches its shader keyword (Standard-style shaders only sample a map when its keyword is on).
        static void SetTexture(Material m, string property, Texture2D tex, string keyword)
        {
            if (!m.HasProperty(property)) { ModLog.Verbose("WeaponArt: shader '" + m.shader.name + "' has no " + property + " slot"); return; }
            m.SetTexture(property, tex);
            if (tex != null) m.EnableKeyword(keyword); else m.DisableKeyword(keyword);
        }
    }
}
