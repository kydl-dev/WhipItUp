using System.Collections.Generic;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Spring + gravity simulation of the fall bones of a skinned weapon (flogger, later the whip thong). Bones named fall_KK_B (chain KK,
    /// bone B from the handle out; made by build_flogger.py). Every LateUpdate (after the hand pose): each chain's bones are put back in
    /// their rest pose, those rest positions are the spring targets, verlet points follow with gravity / damping / inertia, keep their
    /// bone lengths, and each bone is turned to point at its simulated child. Purely visual and local (each client simulates its own copy,
    /// nothing is sent over the network). Tunables: ModSettings "Falls" (live).
    /// </summary>
    internal sealed class FallChains : MonoBehaviour
    {
        sealed class Chain
        {
            public Transform[] Bones;
            public Quaternion[] RestLocal;
            public Vector3 TailLocal;           // tip of the last bone in its own space
            public Vector3[] Pos, Prev;         // nodes 0..n (node i = head of bone i, node n = tip)
            public float[] Length;
        }

        readonly List<Chain> _chains = new List<Chain>();
        bool _primed;
        const float TeleportDistance = 2f;

        void Awake()
        {
            var byChain = new SortedDictionary<string, SortedDictionary<int, Transform>>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var n = t.name;
                if (!n.StartsWith("fall_") || n.Length < 9) continue;
                var parts = n.Split('_');
                int b;
                if (parts.Length != 3 || !int.TryParse(parts[2], out b)) continue;
                if (!byChain.ContainsKey(parts[1])) byChain[parts[1]] = new SortedDictionary<int, Transform>();
                byChain[parts[1]][b] = t;
            }
            foreach (var kv in byChain)
            {
                var bones = new List<Transform>(kv.Value.Values).ToArray();
                if (bones.Length == 0) continue;
                var c = new Chain { Bones = bones, RestLocal = new Quaternion[bones.Length], Length = new float[bones.Length] };
                for (int i = 0; i < bones.Length; i++) c.RestLocal[i] = bones[i].localRotation;
                // tip: same local offset as the last bone's head is from its parent bone (chains are evenly split)
                c.TailLocal = bones.Length > 1 ? bones[bones.Length - 1].localPosition : Vector3.up * 0.1f;
                c.Pos = new Vector3[bones.Length + 1];
                c.Prev = new Vector3[bones.Length + 1];
                _chains.Add(c);
            }
            ModLog.Verbose("FallChains: " + _chains.Count + " chain(s) on " + name + (_chains.Count == 0 ? " - no fall_KK_B bones found" : ", " + _chains[0].Bones.Length + " bone(s) each"));
        }

        void OnEnable() { _primed = false; }

        void LateUpdate()
        {
            if (_chains.Count == 0) return;
            float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 1f / 30f);
            var g = Vector3.down * ModSettings.FallGravity.Value * dt * dt;
            float pull = 1f - Mathf.Exp(-ModSettings.FallStiffness.Value * dt);
            float keep = Mathf.Clamp01(1f - ModSettings.FallDamping.Value);
            float inertia = Mathf.Clamp01(ModSettings.FallInertia.Value);
            foreach (var c in _chains)
            {
                // rest pose -> spring targets
                for (int i = 0; i < c.Bones.Length; i++) c.Bones[i].localRotation = c.RestLocal[i];
                int n = c.Bones.Length;
                var target = new Vector3[n + 1];
                for (int i = 0; i < n; i++) target[i] = c.Bones[i].position;
                target[n] = c.Bones[n - 1].TransformPoint(c.TailLocal);
                if (!_primed || (target[0] - c.Pos[0]).sqrMagnitude > TeleportDistance * TeleportDistance)
                {
                    for (int i = 0; i <= n; i++) { c.Pos[i] = target[i]; c.Prev[i] = target[i]; }
                    for (int i = 1; i <= n; i++) c.Length[i - 1] = (target[i] - target[i - 1]).magnitude;
                    continue;
                }
                // root follows the handle; inertia < 1 lets the falls partly move with it
                var rootMove = target[0] - c.Pos[0];
                c.Pos[0] = target[0];
                c.Prev[0] = target[0];
                for (int i = 1; i <= n; i++)
                {
                    var v = (c.Pos[i] - c.Prev[i]) * keep;          // verlet velocity of the last frame
                    var p = c.Pos[i] + rootMove * (1f - inertia);   // part of the hand's motion is carried along
                    c.Prev[i] = p;
                    p += v + g;
                    p += (target[i] - p) * pull;
                    var d = p - c.Pos[i - 1];
                    float len = d.magnitude;
                    c.Pos[i] = len > 1e-6f ? c.Pos[i - 1] + d * (c.Length[i - 1] / len) : target[i];
                }
                // bones point at their simulated child
                for (int i = 0; i < n; i++)
                {
                    var b = c.Bones[i];
                    var restChild = i + 1 < n ? c.Bones[i + 1].position : b.TransformPoint(c.TailLocal);
                    var from = restChild - b.position;
                    var to = c.Pos[i + 1] - b.position;
                    if (from.sqrMagnitude > 1e-10f && to.sqrMagnitude > 1e-10f) b.rotation = Quaternion.FromToRotation(from, to) * b.rotation;
                }
            }
            if (!_primed) { _primed = true; ModLog.Verbose("FallChains: " + name + " primed (" + _chains.Count + " chains)"); }
        }
    }
}
