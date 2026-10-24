// Editor-only (T-013 S9, D-093): how far forward the body leans in the whip clips compared with the vanilla clips the Frostner
// (MaceSilver, attack 'swing_longsword' = the same states the whip replaces) plays. Menu "Valheim/Report whip clip lean".
// Samples every clip on the Valheim player rig (WhipAnim_Rig.fbx, Humanoid) and logs per frame: the torso lean (angle of
// Hips -> Neck from vertical, + = forward toward +Z), the head's forward offset over the hips (m) and the hips height (m).
// Vanilla copies (research only, never in the bundle, git-ignored): Assets/_VanillaReference/Vanilla_<clip>.anim from the rip.
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhipItUp.EditorTools
{
    public static class WhipAnimLeanReport
    {
        static readonly (string ours, string vanilla, int hit)[] Pairs =
        {
            ("Body_W1_SideFlick", "Vanilla_Attack1_2", 14), ("Body_W2_Backhand", "Vanilla_Attack2", 8),
            ("Body_W3_Overhand", "Vanilla_Attack3", 8), ("Body_G_LashGrab", "Vanilla_throw_spear", 22),
        };

        [MenuItem("Valheim/Report whip clip lean")]
        public static void Report()
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WhipAnimSetup.Folder + "/WhipAnim_Rig.fbx");
            if (rigPrefab == null) { Debug.LogError("[WhipAnimLean] no " + WhipAnimSetup.Folder + "/WhipAnim_Rig.fbx"); return; }
            var ours = AssetDatabase.LoadAllAssetsAtPath(WhipAnimSetup.Folder + "/WhipAnim_Body.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            var go = Object.Instantiate(rigPrefab);
            try
            {
                var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                anim.applyRootMotion = false;
                var t = go.GetComponentsInChildren<Transform>(true).ToDictionary(x => x.name);
                foreach (var p in Pairs)
                {
                    AnimationClip mine;
                    ours.TryGetValue(p.ours, out mine);
                    var van = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_VanillaReference/" + p.vanilla + ".anim");
                    Debug.Log("[WhipAnimLean] " + p.ours + " (hit frame " + p.hit + ") vs " + p.vanilla + ":\n  ours:    " + Sample(go, t, mine, p.hit) + "\n  vanilla: " + Sample(go, t, van, -1));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        // lean of the torso in degrees (+ = forward), head forward offset and hips height at 0 / 25 / 50 / 75 / 100 % and at the hit frame
        static string Sample(GameObject go, System.Collections.Generic.Dictionary<string, Transform> t, AnimationClip clip, int hitFrame)
        {
            if (clip == null) return "(clip missing)";
            var sb = new System.Text.StringBuilder(clip.length.ToString("0.00") + " s, humanMotion " + clip.humanMotion + ": ");
            float maxLean = -999f, sum = 0f;
            int n = 0;
            for (float f = 0f; f <= 1.0001f; f += 0.05f)
            {
                float lean, head, hips;
                Measure(go, t, clip, f * clip.length, out lean, out head, out hips);
                maxLean = Mathf.Max(maxLean, lean); sum += lean; n++;
                if (Mathf.Abs(f * 4f - Mathf.Round(f * 4f)) < 0.01f) sb.Append(" " + (f * 100f).ToString("0") + "%: lean " + lean.ToString("0") + " deg, head +" + head.ToString("0.00") + " m, hips " + hips.ToString("0.00") + " m |");
            }
            if (hitFrame >= 0) { float l, h, hp; Measure(go, t, clip, hitFrame / 30f, out l, out h, out hp); sb.Append(" HIT f" + hitFrame + ": lean " + l.ToString("0") + " deg, head +" + h.ToString("0.00") + " m |"); }
            sb.Append(" max lean " + maxLean.ToString("0") + " deg, mean " + (sum / n).ToString("0") + " deg");
            return sb.ToString();
        }

        static void Measure(GameObject go, System.Collections.Generic.Dictionary<string, Transform> t, AnimationClip clip, float time, out float lean, out float head, out float hips)
        {
            clip.SampleAnimation(go, time);
            var h = t["Hips"].position; var neck = t["Neck"].position; var hd = t["Head"].position;
            var up = neck - h;
            var fwd = go.transform.forward;
            lean = Mathf.Atan2(Vector3.Dot(up, fwd), Vector3.Dot(up, Vector3.up)) * Mathf.Rad2Deg;
            head = Vector3.Dot(hd - h, fwd);
            hips = h.y - go.transform.position.y;
        }
    }
}
