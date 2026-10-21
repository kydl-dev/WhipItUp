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
    /// Optional per-frame controller on the same GameObject (the whip's WhipMotion): sets the targets / strength before each step.
    internal interface IFallDriver
    {
        /// Called at the start of every FallChains step. Set chains.Override / Pull / GravityScale / Grounded for this frame.
        void Drive(FallChains chains, float dt);
    }

    internal sealed class FallChains : MonoBehaviour
    {
        /// When set: world-space target for (chain, node) instead of the rest pose (node 0 is always the handle end).
        public System.Func<int, int, Vector3> Override;
        /// When >= 0: pull toward the targets per second for this frame instead of the Falls Stiffness.
        public float Pull = -1f;
        public float GravityScale = 1f;
        /// 0..1 for this frame: how much the nodes are placed exactly on the Override targets after the simulation step (1 = fully
        /// kinematic: no velocity, no wobble - the whip's settled coil). Targets must keep the bone lengths (the driver's job).
        public float Snap;
        /// Keep nodes above the ground under the handle (dropped items: no falls through the floor).
        public bool Grounded;
        IFallDriver _driver;
        static readonly int GroundMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small");
        sealed class Chain
        {
            public Transform[] Bones;
            public Quaternion[] RestLocal;
            public Vector3 TailLocal;           // tip of the last bone in its own space
            public Vector3[] Pos, Prev;         // nodes 0..n (node i = head of bone i, node n = tip)
            public float[] Length;
            public Vector3[] LastAxis;          // last turn axis per bone: keeps near-180-degree turns from picking a random axis
        }

        readonly List<Chain> _chains = new List<Chain>();

        /// Chain count / node count of a chain (nodes = bones + 1) / world position of a node in the last simulated frame.
        public int ChainCount { get { return _chains.Count; } }
        public int NodeCount(int chain) { return chain < _chains.Count ? _chains[chain].Bones.Length + 1 : 0; }
        public Vector3 NodePosition(int chain, int node) { return _chains[chain].Pos[node]; }
        /// World length of the segment that ends at node (node >= 1), measured from the rest pose (includes the visual's scale).
        public float SegmentLength(int chain, int node) { var c = _chains[chain]; return node >= 1 && node <= c.Length.Length ? c.Length[node - 1] : 0f; }
        /// After the last step: the largest distance (m) between a bone head and its simulated node (should stay ~0; big = the mesh
        /// does not follow the simulation), and the largest single-bone turn in degrees.
        public float LastMismatch { get; private set; }
        public float LastMaxTurn { get; private set; }
        /// World position of the first lash node (where the lash leaves the handle).
        public Vector3 RootPosition(int chain) { return _chains[chain].Bones[0].position; }
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
                c.LastAxis = new Vector3[bones.Length];
                _chains.Add(c);
            }
            _driver = GetComponent<IFallDriver>();
            ModLog.Verbose("FallChains: " + _chains.Count + " chain(s) on " + name + (_driver != null ? " (driver " + _driver.GetType().Name + ")" : "") + (_chains.Count == 0 ? " - no fall_KK_B bones found" : ", " + _chains[0].Bones.Length + " bone(s) each"));
        }

        bool _held;

        int _primes;

        /// Debug log of a (re)prime: why, held or not, and every bone's name, measured length, parent and world position.
        void LogPrime(int ci, Chain c, Vector3[] target, string why)
        {
            _primes++;
            if (!ModLog.DebugOn) return;
            var holder = GetComponentInParent<Character>();
            float total = 0f;
            foreach (var l in c.Length) total += l;
            ModLog.Verbose("FallChains: " + name + " chain " + ci + " " + why + " #" + _primes + " - held " + _held + " (holder " + (holder != null ? holder.name : "none")
                + "), parent " + (transform.parent != null ? transform.parent.name : "none") + ", lossyScale " + transform.lossyScale.ToString("F2") + ", "
                + c.Bones.Length + " bones, total length " + total.ToString("0.000") + " m");
            for (int i = 0; i < c.Bones.Length; i++)
            {
                var b = c.Bones[i];
                ModLog.Verbose("FallChains:   " + b.name + " length " + c.Length[i].ToString("0.0000") + " m, parent " + (b.parent != null ? b.parent.name : "none")
                    + ", world " + target[i].ToString("F3") + ", local " + b.localPosition.ToString("F4") + ", localScale " + b.localScale.ToString("F2"));
            }
            ModLog.Verbose("FallChains:   tip world " + target[c.Bones.Length].ToString("F3"));
        }

        /// Rotation turning 'from' onto 'to' around a stable axis. Quaternion.FromToRotation picks an arbitrary axis when the two are
        /// (nearly) opposite - in a coil that happens every frame and made the lash twist / spin; then the bone's last axis is reused.
        static Quaternion Turn(Chain c, int i, Vector3 from, Vector3 to)
        {
            var f = from.normalized;
            var t = to.normalized;
            float angle = Vector3.Angle(f, t);
            if (angle < 0.01f) return Quaternion.identity;
            var axis = Vector3.Cross(f, t);
            if (axis.sqrMagnitude < 1e-4f || angle > 170f)
            {
                var last = c.LastAxis[i];
                axis = last.sqrMagnitude > 0.5f ? Vector3.ProjectOnPlane(last, f) : Vector3.Cross(f, Vector3.up);
                if (axis.sqrMagnitude < 1e-6f) axis = Vector3.Cross(f, Vector3.right);
            }
            axis.Normalize();
            c.LastAxis[i] = axis;
            return Quaternion.AngleAxis(angle, axis);
        }

        void OnEnable() { _primed = false; _held = GetComponentInParent<Character>() != null; if (_driver == null) _driver = GetComponent<IFallDriver>(); }

        void LateUpdate()
        {
            if (_chains.Count == 0) return;
            float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 1f / 30f);
            Override = null; Pull = -1f; GravityScale = 1f; Snap = 0f; if (!_held) _held = GetComponentInParent<Character>() != null;   // parented to the hand after OnEnable
            Grounded = !_held;
            if (_driver != null) _driver.Drive(this, dt);
            var g = Vector3.down * ModSettings.FallGravity.Value * GravityScale * dt * dt;
            // shape keeping: a weak pull toward the rest shape per second (linear; at the user's 0.03 gravity wins and the falls hang limp);
            // a driver may replace it (the whip's coil / lash / grab)
            float pull = Mathf.Clamp01((Pull >= 0f ? Pull : ModSettings.FallStiffness.Value) * dt);
            float ground = float.NegativeInfinity;
            RaycastHit hit;
            if (Grounded && Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 3f, GroundMask)) ground = hit.point.y + 0.01f;
            float keep = Mathf.Clamp01(1f - ModSettings.FallDamping.Value);
            float inertia = Mathf.Clamp01(ModSettings.FallInertia.Value);
            float mismatch = 0f, maxTurn = 0f;
            for (int ci = 0; ci < _chains.Count; ci++)
            {
                var c = _chains[ci];
                // rest pose -> spring targets
                for (int i = 0; i < c.Bones.Length; i++) c.Bones[i].localRotation = c.RestLocal[i];
                int n = c.Bones.Length;
                var target = new Vector3[n + 1];
                for (int i = 0; i < n; i++) target[i] = c.Bones[i].position;
                target[n] = c.Bones[n - 1].TransformPoint(c.TailLocal);
                // prime / teleport: start from the REST pose and measure the bone lengths there (before any driver override: the whip's
                // coil is built from these lengths, so measuring the override made them 0 - lash stayed straight, TEST-007 log)
                bool teleport = _primed && (target[0] - c.Pos[0]).sqrMagnitude > TeleportDistance * TeleportDistance;
                if (!_primed || teleport)
                {
                    float jump = _primed ? (target[0] - c.Pos[0]).magnitude : 0f;
                    for (int i = 0; i <= n; i++) { c.Pos[i] = target[i]; c.Prev[i] = target[i]; }
                    for (int i = 1; i <= n; i++) c.Length[i - 1] = (target[i] - target[i - 1]).magnitude;
                    LogPrime(ci, c, target, teleport ? "re-prime (handle jumped " + jump.ToString("0.00") + " m)" : "first prime");
                    continue;
                }
                if (Override != null) for (int i = 1; i <= n; i++) target[i] = Override(ci, i);
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
                    if (c.Pos[i].y < ground) c.Pos[i].y = ground;
                }
                if (Override != null && Snap > 0f)
                    for (int i = 1; i <= n; i++)
                    {
                        c.Pos[i] = Vector3.Lerp(c.Pos[i], target[i], Snap);
                        if (Snap >= 0.999f) c.Prev[i] = c.Pos[i];     // settled: no velocity left to carry over
                    }
                // bones point at their simulated child
                for (int i = 0; i < n; i++)
                {
                    var b = c.Bones[i];
                    var restChild = i + 1 < n ? c.Bones[i + 1].position : b.TransformPoint(c.TailLocal);
                    var from = restChild - b.position;
                    var to = c.Pos[i + 1] - c.Pos[i];
                    if (from.sqrMagnitude > 1e-10f && to.sqrMagnitude > 1e-10f)
                    {
                        maxTurn = Mathf.Max(maxTurn, Vector3.Angle(from, to));
                        b.rotation = Turn(c, i, from, to) * b.rotation;
                    }
                    mismatch = Mathf.Max(mismatch, (b.position - c.Pos[i]).magnitude);
                }
            }
            LastMismatch = mismatch;
            LastMaxTurn = maxTurn;
            if (!_primed) { _primed = true; ModLog.Verbose("FallChains: " + name + " primed (" + _chains.Count + " chains)"); }
        }
    }
}
