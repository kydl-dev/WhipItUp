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
        Vector3[] _coilLocal;

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
            if (_holder == null) { SetMode("free (dropped)"); return; }
            bool attacking = IsAttacking();
            _sinceAttack = attacking ? 0f : _sinceAttack + dt;
            if (_grabTarget != null && Time.time - _grabTime < ModSettings.WhipGrabSeconds.Value && !_grabTarget.IsDead())
            {
                var head = _grabTarget.GetHeadPoint();
                var start = transform.position;
                float seg = ModSettings.WhipModelLashLength * transform.lossyScale.y / ModSettings.WhipModelBones;
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
            float back = Mathf.Clamp01(_sinceAttack / Mathf.Max(0.01f, ModSettings.WhipCoilReturnSeconds.Value));
            EnsureCoil(chains);
            chains.Override = (c, i) => transform.TransformPoint(_coilLocal[Mathf.Min(i, _coilLocal.Length - 1)]);
            chains.Pull = ModSettings.WhipCoilPull.Value * back * back;
            chains.GravityScale = 1f - back;
            SetMode(back < 1f ? "coiling" : "coiled");
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

        /// Coil points in the whip's own space (long axis +Y, collar at CollarY): loops in the X-Y plane next to the hand.
        void EnsureCoil(FallChains chains)
        {
            int n = ModSettings.WhipBones.Value + 1;
            if (_coilLocal != null && _coilLocal.Length == n) return;
            _coilLocal = new Vector3[n];
            float seg = ModSettings.WhipLashLength.Value / Mathf.Max(1, ModSettings.WhipBones.Value);
            float r = ModSettings.WhipCoilRadius.Value;
            var start = new Vector3(0f, ModSettings.WhipCollarY.Value, 0f);
            var centre = start + new Vector3(-r, ModSettings.WhipCoilDrop.Value, 0f);
            var d0 = start - centre;
            float a0 = Mathf.Atan2(d0.y, d0.x);
            float r0 = d0.magnitude;
            for (int i = 0; i < n; i++)
            {
                float s = i * seg;
                float a = a0 - s / r;                                   // clockwise, down the outside first
                float rr = Mathf.Lerp(r0, r, Mathf.Clamp01(s / (r * 1.5f)));
                _coilLocal[i] = centre + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0.012f * (s / (2f * Mathf.PI * r)));
            }
            ModLog.Verbose("WhipMotion: coil of " + n + " points, radius " + r + ", " + (ModSettings.WhipLashLength.Value / (2f * Mathf.PI * r)).ToString("0.0") + " loops");
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
