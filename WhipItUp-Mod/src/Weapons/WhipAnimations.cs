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
        sealed class Holder { public RuntimeAnimatorController Original; public int Count; public int ReleaseFrame = -1; }
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
            if (Attached.TryGetValue(animator, out h))
            {
                bool wasReleasing = h.ReleaseFrame >= 0;
                h.Count++;
                h.ReleaseFrame = -1;
                ModLog.Verbose("WhipAnimations: " + who + (wasReleasing ? " got a new whip visual before the old one's release ran - controller kept, no swap" : " already uses the whip clips") + " (holders " + h.Count + ")");
                return true;
            }
            var original = animator.runtimeAnimatorController;
            if (original == null) { ModLog.Verbose("WhipAnimations: " + who + " has no controller - not attached"); return false; }
            var aoc = OverrideFor(original);
            if (aoc == null) return false;
            Swap(animator, aoc, who);
            Attached[animator] = new Holder { Original = original, Count = 1 };
            ModLog.Verbose("WhipAnimations: " + who + " now plays the whip clips (controller '" + original.name + "' -> override, " + Attached.Count + " character(s) attached)");
            return true;
        }

        /// The whip visual of this character went away. The original controller comes back on the NEXT frame (Tick) unless a new whip
        /// visual attaches first: the game rebuilds the visual now and then (fights, re-equips), and a controller swap restarts the animator
        /// (TEST-020: the player lay on the floor / sat). Expected outcome: true when a release is scheduled (or nothing was attached).
        internal static bool Detach(Animator animator, string who)
        {
            Holder h;
            if (animator == null || !Attached.TryGetValue(animator, out h)) { CleanDead(); return true; }
            if (--h.Count > 0) { ModLog.Verbose("WhipAnimations: " + who + " still holds a whip (holders " + h.Count + ")"); return false; }
            h.Count = 0;
            h.ReleaseFrame = Time.frameCount;
            ModLog.Verbose("WhipAnimations: " + who + " let go of the whip visual (frame " + h.ReleaseFrame + ") - original controller back next frame unless a new whip visual attaches");
            return true;
        }

        /// Every frame (Plugin.Update): puts the original controller back on characters whose whip release is at least one frame old.
        /// Expected outcome: the number of characters restored this frame.
        internal static int Tick()
        {
            if (Attached.Count == 0) return 0;
            List<Animator> done = null;
            foreach (var kv in Attached)
            {
                var h = kv.Value;
                if (h.ReleaseFrame < 0 || Time.frameCount <= h.ReleaseFrame) continue;
                if (done == null) done = new List<Animator>();
                done.Add(kv.Key);
            }
            if (done == null) return 0;
            foreach (var a in done)
            {
                var h = Attached[a];
                Attached.Remove(a);
                if (a == null) { ModLog.Verbose("WhipAnimations: released animator was destroyed - nothing to put back"); continue; }
                string who = a.transform.root != null ? a.transform.root.name : a.name;
                if (a.runtimeAnimatorController is AnimatorOverrideController) Swap(a, h.Original, who);
                ModLog.Verbose("WhipAnimations: " + who + " back to '" + (h.Original != null ? h.Original.name : "null") + "' (" + Attached.Count + " character(s) still attached)");
            }
            return done.Count;
        }

        static readonly Dictionary<int, float> FloatParams = new Dictionary<int, float>();
        static readonly Dictionary<int, int> IntParams = new Dictionary<int, int>();
        static readonly Dictionary<int, bool> BoolParams = new Dictionary<int, bool>();

        /// Sets another controller WITHOUT restarting the animator in its default state: the state of every layer (with its time) and
        /// every float / int / bool parameter are carried over. Expected outcome: true when the controller was set.
        static bool Swap(Animator a, RuntimeAnimatorController controller, string who)
        {
            if (a == null || controller == null) return false;
            int layers = a.isInitialized ? a.layerCount : 0;
            var hashes = new int[layers];
            var times = new float[layers];
            for (int l = 0; l < layers; l++)
            {
                var st = a.IsInTransition(l) ? a.GetNextAnimatorStateInfo(l) : a.GetCurrentAnimatorStateInfo(l);
                hashes[l] = st.fullPathHash;
                times[l] = st.normalizedTime;
            }
            FloatParams.Clear(); IntParams.Clear(); BoolParams.Clear();
            if (a.isInitialized)
                foreach (var p in a.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Float) FloatParams[p.nameHash] = a.GetFloat(p.nameHash);
                    else if (p.type == AnimatorControllerParameterType.Int) IntParams[p.nameHash] = a.GetInteger(p.nameHash);
                    else if (p.type == AnimatorControllerParameterType.Bool) BoolParams[p.nameHash] = a.GetBool(p.nameHash);
                }
            float speed = a.speed;
            a.runtimeAnimatorController = controller;
            foreach (var kv in FloatParams) a.SetFloat(kv.Key, kv.Value);
            foreach (var kv in IntParams) a.SetInteger(kv.Key, kv.Value);
            foreach (var kv in BoolParams) a.SetBool(kv.Key, kv.Value);
            int restored = 0;
            for (int l = 0; l < layers && l < a.layerCount; l++)
                if (a.HasState(l, hashes[l])) { a.Play(hashes[l], l, times[l] - (float)System.Math.Floor(times[l])); restored++; }
            a.speed = speed;
            a.Update(0f);
            ModLog.Verbose("WhipAnimations: " + who + " controller -> '" + controller.name + "', " + restored + " of " + layers + " layer state(s) and "
                + (FloatParams.Count + IntParams.Count + BoolParams.Count) + " parameter(s) carried over (layer 0 state " + (layers > 0 ? hashes[0].ToString() : "-") + ")");
            return a.runtimeAnimatorController == controller;
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
