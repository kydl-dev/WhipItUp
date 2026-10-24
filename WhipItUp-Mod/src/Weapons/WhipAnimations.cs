using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The custom whip clips (T-013, D-084). Body: four Humanoid clips from the bundle (WhipAnim_Body.fbx: Body_W1_SideFlick, Body_W2_Backhand,
    /// Body_W3_Overhand, Body_G_LashGrab) replace the vanilla clips the whip's attacks play (Attack1_2 / Attack2 / Attack3 of the sword combo
    /// 'swing_longsword', throw_spear of attack 2) through an AnimatorOverrideController - ONLY on a character that holds a whip: the whip
    /// visual (WhipMotion) attaches it while it exists and puts the original controller back when it goes, on every client, so swords,
    /// clubs and spears keep their attacks. Lash: whip_lash_frames.txt (embedded) = the keyed lash, sampled by WhipMotion by the state's time.
    /// Tunable Whip CustomAnimations switches both off (applies the next time a whip is equipped).
    /// </summary>
    internal static class WhipAnimations
    {
        /// vanilla clip name -> our clip name
        static readonly Dictionary<string, string> ClipMap = new Dictionary<string, string>
        {
            { "Attack1_2", "Body_W1_SideFlick" }, { "Attack2", "Body_W2_Backhand" }, { "Attack3", "Body_W3_Overhand" }, { "throw_spear", "Body_G_LashGrab" },
        };
        /// animator state (short name hash) -> lash clip name in whip_lash_frames.txt
        static readonly Dictionary<int, string> StateToLash = new Dictionary<int, string>
        {
            { Animator.StringToHash("swing_sword 0"), "W1_SideFlick" }, { Animator.StringToHash("swing_sword 1"), "W2_Backhand" },
            { Animator.StringToHash("swing_sword 2"), "W3_Overhand" }, { Animator.StringToHash("spear_throw"), "G_LashGrab" },
        };
        const string LashResource = "whip_lash_frames.txt";

        static readonly Dictionary<string, AnimationClip> Clips = new Dictionary<string, AnimationClip>();
        static readonly Dictionary<RuntimeAnimatorController, AnimatorOverrideController> Overrides = new Dictionary<RuntimeAnimatorController, AnimatorOverrideController>();
        sealed class Holder { public RuntimeAnimatorController Original; public int Count; }
        static readonly Dictionary<Animator, Holder> Attached = new Dictionary<Animator, Holder>();

        internal static WhipLashFrames Lash { get; private set; }

        /// Plugin.Awake. Expected outcome: all 4 body clips and all 4 lash clips are loaded (returns that check).
        internal static bool Load(AssetBundle bundle)
        {
            Clips.Clear();
            if (bundle != null)
                foreach (var c in bundle.LoadAllAssets<AnimationClip>())
                    if (c != null && c.name.StartsWith("Body_")) Clips[c.name] = c;
            foreach (var kv in ClipMap)
            {
                AnimationClip c;
                if (Clips.TryGetValue(kv.Value, out c))
                    ModLog.Verbose("WhipAnimations: clip " + kv.Value + " (replaces " + kv.Key + "): " + c.length.ToString("0.000") + " s, " + c.frameRate + " fps, humanMotion " + c.humanMotion
                        + ", " + c.events.Length + " event(s) [" + string.Join(", ", System.Array.ConvertAll(c.events, e => e.functionName + "@" + e.time.ToString("0.000"))) + "]");
                else ModLog.Warn("WhipAnimations: clip " + kv.Value + " is not in the bundle (" + (bundle != null ? "rebuild it in Unity: Valheim > Set up WhipItUp animations, then Build AssetBundles" : "no bundle") + ") - " + kv.Key + " stays vanilla for the whip");
            }
            Lash = WhipLashFrames.Parse(ReadResource(LashResource), s => ModLog.Verbose(s));
            ModLog.Verbose("WhipAnimations: loaded " + Clips.Count + " body clip(s) of " + ClipMap.Count + ", " + Lash.Count + " lash clip(s) of " + StateToLash.Count + ", enabled " + ModSettings.WhipCustomAnimations.Value);
            return Clips.Count == ClipMap.Count && Lash.Count == StateToLash.Count;
        }

        static string ReadResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res != name && !res.EndsWith("." + name)) continue;
                using (var s = asm.GetManifestResourceStream(res))
                using (var r = new StreamReader(s))
                {
                    var text = r.ReadToEnd();
                    ModLog.Verbose("WhipAnimations: resource " + res + " read, " + text.Length + " chars");
                    return text;
                }
            }
            ModLog.Warn("WhipAnimations: embedded resource " + name + " not found - the lash uses the physics swing (no keyed lash)");
            return null;
        }

        /// A whip visual appeared in this character's hand. Expected outcome: true when the whip clips are active on its animator.
        internal static bool Attach(Animator animator, string who)
        {
            if (animator == null) { ModLog.Verbose("WhipAnimations: attach for " + who + " - no animator"); return false; }
            if (!ModSettings.WhipCustomAnimations.Value || Clips.Count == 0) { ModLog.Verbose("WhipAnimations: attach for " + who + " skipped (enabled " + ModSettings.WhipCustomAnimations.Value + ", clips " + Clips.Count + ")"); return false; }
            Holder h;
            if (Attached.TryGetValue(animator, out h)) { h.Count++; ModLog.Verbose("WhipAnimations: " + who + " already uses the whip clips (holders " + h.Count + ")"); return true; }
            var original = animator.runtimeAnimatorController;
            if (original == null) { ModLog.Verbose("WhipAnimations: " + who + " has no controller - not attached"); return false; }
            var aoc = OverrideFor(original);
            if (aoc == null) return false;
            animator.runtimeAnimatorController = aoc;
            Attached[animator] = new Holder { Original = original, Count = 1 };
            ModLog.Verbose("WhipAnimations: " + who + " now plays the whip clips (controller '" + original.name + "' -> override, " + Attached.Count + " character(s) attached)");
            return true;
        }

        /// The whip visual of this character went away. Expected outcome: true when the original controller is back (or nothing was attached).
        internal static bool Detach(Animator animator, string who)
        {
            Holder h;
            if (animator == null || !Attached.TryGetValue(animator, out h)) { CleanDead(); return true; }
            if (--h.Count > 0) { ModLog.Verbose("WhipAnimations: " + who + " still holds a whip (holders " + h.Count + ")"); return false; }
            Attached.Remove(animator);
            if (animator.runtimeAnimatorController is AnimatorOverrideController) animator.runtimeAnimatorController = h.Original;
            ModLog.Verbose("WhipAnimations: " + who + " back to '" + (h.Original != null ? h.Original.name : "null") + "' (" + Attached.Count + " character(s) still attached)");
            return animator.runtimeAnimatorController == h.Original;
        }

        static void CleanDead()
        {
            var dead = new List<Animator>();
            foreach (var a in Attached.Keys) if (a == null) dead.Add(a);
            foreach (var a in dead) Attached.Remove(a);
            if (dead.Count > 0) ModLog.Verbose("WhipAnimations: dropped " + dead.Count + " destroyed animator(s)");
        }

        static AnimatorOverrideController OverrideFor(RuntimeAnimatorController original)
        {
            AnimatorOverrideController aoc;
            if (Overrides.TryGetValue(original, out aoc)) return aoc;
            aoc = new AnimatorOverrideController(original) { name = original.name + "_WhipItUp" };
            var list = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
            aoc.GetOverrides(list);
            int replaced = 0;
            var found = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var key = list[i].Key;
                string ours;
                AnimationClip clip;
                if (key == null || !ClipMap.TryGetValue(key.name, out ours) || !Clips.TryGetValue(ours, out clip)) continue;
                list[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, clip);
                found.Add(key.name);
                replaced++;
            }
            aoc.ApplyOverrides(list);
            foreach (var k in ClipMap.Keys)
                if (!found.Contains(k)) ModLog.Warn("WhipAnimations: the game's controller '" + original.name + "' has no clip '" + k + "' - its whip clip is not used (game update?)");
            ModLog.Verbose("WhipAnimations: override of '" + original.name + "' made: " + replaced + " clip slot(s) replaced of " + list.Count);
            Overrides[original] = aoc;
            return aoc;
        }

        /// Keyed lash clip for the animator's current attack state: the lash clip name and the state's normalized time (0..1).
        /// Prefers the next state during a transition. Expected outcome: false when no whip clip state plays (or the clips are not attached).
        internal static bool CurrentLash(Animator animator, out string clip, out float t)
        {
            clip = null;
            t = 0f;
            if (animator == null || Lash == null || !Attached.ContainsKey(animator)) return false;
            for (int l = 0; l < animator.layerCount; l++)
            {
                if (animator.IsInTransition(l))
                {
                    var next = animator.GetNextAnimatorStateInfo(l);
                    if (StateToLash.TryGetValue(next.shortNameHash, out clip) && Lash.Get(clip) != null) { t = next.normalizedTime; return true; }
                }
                var cur = animator.GetCurrentAnimatorStateInfo(l);
                if (StateToLash.TryGetValue(cur.shortNameHash, out clip) && Lash.Get(clip) != null) { t = cur.normalizedTime; return true; }
            }
            clip = null;
            return false;
        }
    }
}
