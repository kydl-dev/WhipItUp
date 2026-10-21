using System.Collections.Generic;
using HarmonyLib;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Drives the whip's lash (one FallChains chain) by what its holder does (D-034), on every client from the holder's animator:
    ///  * idle (held, not attacking): the lash is gathered in coils at the hand (coil targets in the whip's own space, strong pull);
    ///  * grab (attack 2 hit, attacker's client only): the lash runs to the target and wraps its neck (helix) for GrabSeconds;
    ///  * attacking (club combo, attack 2): the lash snaps straight along the swing (pull toward the straight rest shape, little gravity),
    ///    then winds back into the coil over CoilReturnSeconds;
    ///  * dropped: plain hanging physics on the ground (FallChains defaults).
    /// Tunables: ModSettings "Whip".
    /// </summary>
    internal sealed class WhipMotion : MonoBehaviour, IFallDriver
    {
        static readonly AccessTools.FieldRef<Character, Animator> AnimatorRef = AccessTools.FieldRefAccess<Character, Animator>("m_animator");
        /// Animator states that count as "attacking" (club combo + the spear-throw clip of attack 2).
        static readonly Dictionary<int, string> AttackStates = new Dictionary<int, string>();
        static WhipMotion()
        {
            foreach (var n in new[] { "swing_sword 0", "swing_sword 1", "swing_sword 2", "swing_longsword0", "swing_longsword1", "swing_longsword2", "spear_throw" })
                AttackStates[Animator.StringToHash(n)] = n;
        }

        Character _holder;
        float _sinceAttack = 99f;
        Character _grabTarget;
        float _grabTime = -99f;
        string _mode = "";

        void OnEnable() { _holder = GetComponentInParent<Character>(); _sinceAttack = 99f; _grabTarget = null; }

        /// Attack 2 hit on the attacker's client: wrap the target's neck (visual).
        public void StartGrab(Character target)
        {
            _grabTarget = target;
            _grabTime = Time.time;
            ModLog.Verbose("WhipMotion: grab " + (target != null ? target.name : "null"));
        }

        public void Drive(FallChains chains, float dt)
        {
            // the game instantiates the equipped visual first and parents it to the hand afterwards: look the holder up until found
            if (_holder == null) _holder = GetComponentInParent<Character>();
            if (_holder == null) { SetMode("free (dropped)"); return; }
            bool attacking = IsAttacking();
            _sinceAttack = attacking ? 0f : _sinceAttack + dt;
            if (_grabTarget != null && Time.time - _grabTime < ModSettings.WhipGrabSeconds.Value && !_grabTarget.IsDead())
            {
                var head = _grabTarget.GetHeadPoint();
                var start = transform.position;
                float seg = chains.ChainCount > 0 ? chains.SegmentLength(0, 1) : 0.1f;
                chains.Override = (c, i) => GrabTarget(start, head, i * seg);
                chains.Pull = ModSettings.WhipGrabPull.Value;
                chains.GravityScale = 0f;
                SetMode("grab");
                return;
            }
            _grabTarget = null;
            if (attacking)
            {
                chains.Pull = ModSettings.WhipLashStiffness.Value;
                chains.GravityScale = ModSettings.WhipLashGravity.Value;
                SetMode("lash");
                return;
            }
            // idle: wind into the coil quickly (linear blend over CoilReturnSeconds), no gravity once coiled
            float back = Mathf.Clamp01(_sinceAttack / Mathf.Max(0.01f, ModSettings.WhipCoilReturnSeconds.Value));
            if (!BuildCoil(chains)) { SetMode("free (no chain)"); return; }
            chains.Override = (c, i) => _coil[Mathf.Clamp(i, 0, _coil.Length - 1)];
            chains.Pull = Mathf.Lerp(ModSettings.WhipLashStiffness.Value, ModSettings.WhipCoilPull.Value, back);
            chains.GravityScale = 1f - back;
            chains.Snap = back * back;                 // ends fully kinematic: the settled coil does not wobble or spin
            SetMode(back < 1f ? "coiling" : "coiled");
        }

        Vector3[] _coil;

        /// The idle coil in world space, rebuilt every frame from the real chain (node spacing = the simulated bone lengths, so the
        /// length constraint never fights the targets): loops hanging under the hand in the vertical plane through the handle axis,
        /// side by side (each loop shifted along the plane normal). Expected outcome: a coil with one point per node.
        bool BuildCoil(FallChains chains)
        {
            if (chains.ChainCount == 0) return chains.ChainCount > 0;
            int n = chains.NodeCount(0);
            if (_coil == null || _coil.Length != n) _coil = new Vector3[n];
            var start = chains.RootPosition(0);
            var axis = transform.up;                                          // handle direction (model +Y)
            var down = Vector3.ProjectOnPlane(Vector3.down, axis);
            if (down.sqrMagnitude < 1e-4f) down = Vector3.ProjectOnPlane(transform.right, axis);
            down.Normalize();
            var normal = Vector3.Cross(axis, down).normalized;
            float scale = transform.lossyScale.y;
            float r = ModSettings.WhipCoilRadius.Value * scale;
            // centre: below the lash start (CoilDrop = along the handle, negative = back toward the hand)
            var centre = start + down * r + axis * (ModSettings.WhipCoilDrop.Value * scale);
            var d0 = start - centre;
            float r0 = d0.magnitude;
            float a0 = Mathf.Atan2(Vector3.Dot(d0, down), Vector3.Dot(d0, axis));
            _coil[0] = start;
            // points on the circle spaced by the CHORD of each bone (2 R sin(da/2) = bone length), so the targets keep the bone
            // lengths exactly; the first point sits on a circle of radius r0 through the lash start and the radius eases to r
            float a = a0, turned = 0f;
            for (int i = 1; i < n; i++)
            {
                float len = chains.SegmentLength(0, i);
                float rr = Mathf.Lerp(r0, r, Mathf.Clamp01(i / 3f));
                float da = 2f * Mathf.Asin(Mathf.Clamp(len / (2f * rr), -1f, 1f));
                a += da;
                turned += da;
                var p = centre + (axis * Mathf.Cos(a) + down * Mathf.Sin(a)) * rr + normal * (0.012f * scale * turned / (2f * Mathf.PI));
                // exact length from the previous point (the side shift and the radius change would stretch it a little)
                var d = p - _coil[i - 1];
                _coil[i] = d.sqrMagnitude > 1e-10f ? _coil[i - 1] + d.normalized * len : p;
            }
            return _coil.Length == n;
        }

        /// Distance s (m) along the lash: first straight to the target's head, then a helix around the neck.
        static Vector3 GrabTarget(Vector3 start, Vector3 head, float s)
        {
            var toHead = head - start;
            float r = ModSettings.WhipGrabRadius.Value;
            float reach = Mathf.Max(0.01f, toHead.magnitude - r);
            if (s <= reach) return start + toHead.normalized * s;
            float a = (s - reach) / r;
            var side = Vector3.Cross(Vector3.up, toHead.normalized).normalized;
            var back = Vector3.Cross(side, Vector3.up);
            return head + Vector3.down * 0.12f + (side * Mathf.Cos(a) - back * Mathf.Sin(a)) * r + Vector3.up * (0.015f * a);
        }

        bool IsAttacking()
        {
            var h = _holder as Humanoid;
            bool local = h != null && h.IsOwner() && h.InAttack();
            if (local) return local;
            var a = AnimatorRef(_holder);
            bool found = false;
            for (int l = 0; a != null && l < a.layerCount && !found; l++)
                found = AttackStates.ContainsKey(a.GetCurrentAnimatorStateInfo(l).shortNameHash)
                        || (a.IsInTransition(l) && AttackStates.ContainsKey(a.GetNextAnimatorStateInfo(l).shortNameHash));
            return found;
        }

        void SetMode(string mode)
        {
            if (mode == _mode) return;
            _mode = mode;
            ModLog.Verbose("WhipMotion: " + (_holder != null ? _holder.name : "dropped whip") + " -> " + mode);
        }
    }
}
