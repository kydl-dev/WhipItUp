using System.Collections.Generic;
using HarmonyLib;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Drives the whip's lash (one FallChains chain) by what its holder does (D-034), on every client from the holder's animator:
    ///  * idle (held, not attacking): the lash is gathered in coils at the hand (coil targets in the whip's own space, strong pull);
    ///  * grab (attack 2 hit, attacker's client only): the lash runs to the target and wraps its body (helix, D-086) for GrabSeconds;
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

        void OnEnable() { _holder = GetComponentInParent<Character>(); _sinceAttack = 99f; _grabTarget = null; _attachTried = false; _standChecked = -1; _dropSeen = -1f; Live.Add(this); }
        void OnDisable()
        {
            Live.Remove(this);
            if (_attachTried) WhipAnimations.Detach(_attached, _holder != null ? _holder.name : "a whip holder");
            _attachTried = false;
            _attached = null;
        }

        // ---- custom whip clips (T-013): attached to the holder's animator while this visual exists ---------------------------------
        Animator _attached;
        bool _attachTried;
        string _keyedClip;
        float _nextKeyedLog;

        float _lastKeyedT = -1f;
        float _swingStart = -100f;
        string _lastKeyedClip;

        /// D-086: the crack sound when the keyed lash reaches its fully extended frame (whips crack on every swing, not only on a hit),
        /// at the tip. Shares the hit slap's once-per-swing gate, so a hit right after the crack does not play a second slap.
        /// Expected outcome: true when the crack played this frame.
        bool Crack(WhipLashFrames lash, string clip, float t)
        {
            var c = lash.Get(clip);
            bool newSwing = clip != _lastKeyedClip || t < _lastKeyedT;
            if (newSwing) _swingStart = Time.time;
            float prev = newSwing ? -1f : _lastKeyedT;
            _lastKeyedClip = clip;
            _lastKeyedT = t;
            if (c == null || c.CrackTime < 0f || !(prev < c.CrackTime && t >= c.CrackTime)) return false;
            // one crack per swing: a hit of THIS swing already played its slap (TEST-021: hit 2's slap + its crack 0.26 s later = a double sound)
            float slap = Sounds.SlapOncePerSwingPatch.LastSlapTime;
            if (slap >= _swingStart)
            {
                ModLog.Verbose("WhipMotion: crack " + clip + " frame " + c.Crack + " skipped - this swing's hit played its slap " + (Time.time - slap).ToString("0.00") + " s ago");
                return false;
            }
            var prefab = Sounds.WeaponSounds.WhipSlapPrefab;
            float x, y, z;
            lash.Sample(clip, c.CrackTime, c.Nodes - 1, out x, out y, out z);
            var tip = transform.TransformPoint(new Vector3(x, y, z));
            if (prefab == null) { ModLog.Verbose("WhipMotion: crack of " + clip + " at frame " + c.Crack + " - no whip slap sound loaded"); return false; }
            if (!Sounds.SlapOncePerSwingPatch.TryClaim("whip crack " + clip + " frame " + c.Crack + " (t " + prev.ToString("0.00") + " -> " + t.ToString("0.00") + ")")) return false;
            Instantiate(prefab, tip, Quaternion.identity);
            ModLog.Verbose("WhipMotion: crack " + clip + " at the tip " + tip.ToString("F2") + " (" + (_holder != null ? _holder.name : "?") + ")");
            return true;
        }

        /// The keyed lash of the custom clip that plays now (WhipAnimations / whip_lash_frames.txt): every node on its keyed position in this
        /// visual's space (the model data is in Whip.fbx model units, the visual's scale makes it game size). Expected outcome: true when it drives.
        bool DriveKeyed(FallChains chains)
        {
            string clip;
            float t;
            if (!WhipAnimations.CurrentLash(_attached, out clip, out t)) { _keyedClip = null; _lastKeyedClip = null; return false; }
            var lash = WhipAnimations.Lash;
            var self = transform;
            chains.Override = (c, i) =>
            {
                float x, y, z;
                return lash.Sample(clip, t, i, out x, out y, out z) ? self.TransformPoint(new Vector3(x, y, z)) : chains.NodePosition(c, i);
            };
            chains.Pull = 60f;
            chains.GravityScale = 0f;
            chains.Snap = 1f;                          // exactly on the keys: the lash shape is the animation, not physics
            Crack(lash, clip, t);
            SetMode("keyed lash " + clip);
            if (clip != _keyedClip || Time.time >= _nextKeyedLog)
            {
                float x, y, z;
                lash.Sample(clip, t, 0, out x, out y, out z);
                var start = self.TransformPoint(new Vector3(x, y, z));
                if (clip != _keyedClip && chains.ChainCount > 0)
                    ModLog.Check("WhipMotion: keyed lash start vs the real lash start (" + clip + ")", (start - chains.RootPosition(0)).magnitude, 0.0, 0.03, "m");
                lash.Sample(clip, t, chains.ChainCount > 0 ? chains.NodeCount(0) - 1 : 0, out x, out y, out z);
                ModLog.Verbose("WhipMotion: keyed lash " + clip + " t " + t.ToString("0.00") + ", tip " + self.TransformPoint(new Vector3(x, y, z)).ToString("F2") + ", lash start " + start.ToString("F2")
                    + ", visual scale " + self.lossyScale.x.ToString("0.00"));
                _nextKeyedLog = Time.time + 0.25f;
                _keyedClip = clip;
            }
            return true;
        }

        // ---- read by the in-game tests (Testing/InGameTests, run by the user's GameProbe) ----------------------------------------------
        /// Every enabled whip visual (held, dropped, in the viewer).
        internal static readonly List<WhipMotion> Live = new List<WhipMotion>();
        internal string Mode { get { return _mode; } }
        internal Character Holder { get { return _holder; } }
        /// Result of the last coil health check (every 3 s while coiled); null = not checked yet.
        internal bool? LastCoilOk { get; private set; }
        internal string LastCoilDetail { get; private set; }
        internal float LastCoilAt { get; private set; } = -99f;

        /// Attack 2 hit on the attacker's client: wrap the target's body (visual).
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
            if (_holder == null)
            {
                // user bug report 2026-10-25 (D-108): a whip on an item stand or an armor stand must hang coiled, not as a loose rope.
                // Those stands show the item's visual without a Character; a dropped whip (ItemDrop) stays free.
                string stand = StandName();
                if (stand != null) { DriveCoil(chains, 1f, "coiled (" + stand + ")"); return; }
                SetMode("free (dropped)");
                ProbeDropped();
                return;
            }
            if (!_attachTried)
            {
                _attachTried = true;
                _attached = AnimatorRef(_holder);
                WhipAnimations.Attach(_attached, _holder.name);
            }
            bool attacking = IsAttacking();
            _sinceAttack = attacking ? 0f : _sinceAttack + dt;
            if (_grabTarget != null && Time.time - _grabTime < ModSettings.WhipGrabSeconds.Value && !_grabTarget.IsDead())
            {
                // D-086: the lash wraps the target's BODY (centre point, its collider radius); GetHeadPoint threw for a Seeker (no head)
                var centre = _grabTarget.GetCenterPoint();
                float radius = Mathf.Max(ModSettings.WhipGrabRadius.Value, _grabTarget.GetRadius() * 1.05f);
                var start = transform.position;
                float seg = chains.ChainCount > 0 ? chains.SegmentLength(0, 1) : 0.1f;
                if (_grabLogged != _grabTarget)
                {
                    _grabLogged = _grabTarget;
                    ModLog.Verbose("WhipMotion: grab wraps " + _grabTarget.name + " at its centre " + centre.ToString("F2") + ", radius " + radius.ToString("0.00") + " m (collider "
                        + _grabTarget.GetRadius().ToString("0.00") + " m, setting " + ModSettings.WhipGrabRadius.Value + " m), " + (centre - start).magnitude.ToString("0.00") + " m away");
                }
                chains.Override = (c, i) => GrabTarget(start, centre, i * seg, radius);
                chains.Pull = ModSettings.WhipGrabPull.Value;
                chains.GravityScale = 0f;
                SetMode("grab");
                return;
            }
            _grabTarget = null;
            if (DriveKeyed(chains)) { _sinceAttack = 0f; return; }
            if (attacking)
            {
                chains.Pull = ModSettings.WhipLashStiffness.Value;
                chains.GravityScale = ModSettings.WhipLashGravity.Value;
                SetMode("lash");
                return;
            }
            // idle: wind into the coil quickly (linear blend over CoilReturnSeconds), no gravity once coiled
            DriveCoil(chains, Mathf.Clamp01(_sinceAttack / Mathf.Max(0.01f, ModSettings.WhipCoilReturnSeconds.Value)), null);
        }

        float _dropSeen = -1f, _dropNextLog;
        static readonly HarmonyLib.AccessTools.FieldRef<Player, float> AutoPickupRangeRef = HarmonyLib.AccessTools.FieldRefAccess<Player, float>("m_autoPickupRange");
        static readonly HarmonyLib.AccessTools.FieldRef<Player, int> AutoPickupMaskRef = HarmonyLib.AccessTools.FieldRefAccess<Player, int>("m_autoPickupMask");
        /// D-109 diagnosis (debug log only): for 8 s after a whip lands as a dropped item, once a second: where it is, how far from the
        /// local player's pickup point, and every condition the game's Player.AutoPickup checks (auto pickup flag, CanPickup, rigidbody /
        /// collider on the pickup layers, inventory room). Tells why a whip taken off a stand is not picked up.
        void ProbeDropped()
        {
            if (!ModSettings.DebugLog.Value) return;
            if (_dropSeen < 0f) { _dropSeen = Time.time; _dropNextLog = 0f; }
            if (Time.time - _dropSeen > 8f || Time.time < _dropNextLog) return;
            _dropNextLog = Time.time + 1f;
            var drop = GetComponentInParent<ItemDrop>();
            var p = Player.m_localPlayer;
            if (drop == null) { ModLog.Verbose("WhipDrop: no ItemDrop above the dropped whip visual (" + transform.root.name + ")"); return; }
            var rb = drop.GetComponent<Rigidbody>();
            int mask = p != null ? AutoPickupMaskRef(p) : 0;
            var cols = drop.GetComponentsInChildren<Collider>(true);
            var sb = new System.Text.StringBuilder();
            foreach (var c in cols)
                sb.Append(c.GetType().Name).Append('@').Append(c.gameObject.name).Append(" layer ").Append(LayerMask.LayerToName(c.gameObject.layer))
                  .Append(((mask >> c.gameObject.layer) & 1) == 1 ? " (pickup layer)" : "").Append(c.enabled ? "" : " DISABLED").Append(c.isTrigger ? " trigger" : "")
                  .Append(c.attachedRigidbody == rb ? "" : " other-rb").Append("; ");
            string who = "no local player";
            if (p != null)
            {
                var from = p.transform.position + Vector3.up;     // AutoPickup's sphere centre
                float d = Vector3.Distance(from, drop.transform.position);
                who = "pickup point " + d.ToString("0.00") + " m away (range " + AutoPickupRangeRef(p).ToString("0.0") + " m), can add " + p.GetInventory().CanAddItem(drop.m_itemData, -1);
            }
            ModLog.Verbose("WhipDrop: " + drop.name + " t+" + (Time.time - _dropSeen).ToString("0.0") + " s at " + drop.transform.position.ToString("F2") + ", autoPickup " + drop.m_autoPickup
                + ", CanPickup(delay) " + drop.CanPickup(true) + ", rb " + (rb == null ? "none" : (rb.isKinematic ? "kinematic" : "dynamic") + " v " + rb.linearVelocity.magnitude.ToString("0.00"))
                + ", " + who + ", colliders: " + (cols.Length == 0 ? "NONE" : sb.ToString()));
        }

        int _standChecked = -1;
        float _standNextCheck;
        string _stand;
        /// "item stand" / "armor stand" when this visual sits on one, else null. A found stand is kept (until the next enable); "none" is
        /// looked up again every 0.5 s, because the game may parent the visual to the stand after creating it. Logged when it changes.
        string StandName()
        {
            if (_standChecked == 1 && (_stand != null || Time.time < _standNextCheck)) return _stand;
            string was = _standChecked == 1 ? _stand ?? "none" : "unknown";
            _standChecked = 1;
            _standNextCheck = Time.time + 0.5f;
            _stand = GetComponentInParent<ItemStand>() != null ? "item stand" : GetComponentInParent<ArmorStand>() != null ? "armor stand" : null;
            if ((_stand ?? "none") != was)
                ModLog.Verbose("WhipMotion: no character holds this whip - " + (_stand != null ? "on an " + _stand + ": coiled" : "not on a stand (dropped): free") + " (" + transform.root.name + ")");
            return _stand;
        }

        /// The idle coil. back = 0..1 blend from the lash into the coil (1 = settled, kinematic). standMode = the mode name for a stand, null in a hand.
        void DriveCoil(FallChains chains, float back, string standMode)
        {
            if (!BuildCoil(chains)) { SetMode("free (no chain)"); return; }
            chains.Override = (c, i) => _coil[Mathf.Clamp(i, 0, _coil.Length - 1)];
            chains.Pull = Mathf.Lerp(ModSettings.WhipLashStiffness.Value, ModSettings.WhipCoilPull.Value, back);
            chains.GravityScale = 1f - back;
            chains.Snap = back * back;                 // ends fully kinematic: the settled coil does not wobble or spin
            // first health check 1 s after the coil settled: the very first coiled frame still has the un-primed chain (false CHECK OFF, TEST-020)
            if (back >= 1f && _mode != "coiled" && _mode != standMode) _nextDiag = Time.time + 1f;
            SetMode(standMode ?? (back < 1f ? "coiling" : "coiled"));
            if (back >= 1f && _coil != null && !_coilLogged)
            {
                _coilLogged = true;
                var sb = new System.Text.StringBuilder("WhipMotion: coil points (world): ");
                for (int i = 0; i < _coil.Length; i += 4) sb.Append(i).Append('=').Append(_coil[i].ToString("F2")).Append(' ');
                ModLog.Verbose(sb.ToString());
            }
            if (back < 1f) _coilLogged = false;
            if (back >= 1f && Time.time >= _nextDiag)
            {
                _nextDiag = Time.time + 3f;     // coil health every 3 s while coiled
                LogCoilHealth(chains);
            }
        }

        /// Coil health with the values the code expects (user, 2026-10-24: a value that is not what the code expects is a WARNING, not info).
        /// mismatch: bones vs simulated nodes, expected 0 (the targets keep the bone lengths). max bone turn: the bend between two bones on a
        /// circle of radius r is the chord angle 2 asin(L / 2r) of the longest bone L. A CHECK OFF warning is written by ModLog when either is off.
        /// lossyScale is the item's attach-chain scale: only informative (the coil is sized in world metres, not by it).
        /// Expected outcome: returns whether both checks were inside their tolerance.
        bool LogCoilHealth(FallChains chains)
        {
            float expectedTurn = _coilLongest > 1e-5f && _coilRadiusUsed > 1e-5f
                ? 2f * Mathf.Asin(Mathf.Clamp01(_coilLongest / (2f * _coilRadiusUsed))) * Mathf.Rad2Deg : 0f;
            bool mismatchOk = ModLog.Check("WhipMotion: coil bone/node mismatch", chains.LastMismatch, 0.0, CoilMismatchTolerance, "m");
            bool turnOk = ModLog.Check("WhipMotion: coil max bone turn (radius " + _coilRadiusUsed.ToString("0.000") + " m, longest bone "
                + _coilLongest.ToString("0.000") + " m)", chains.LastMaxTurn, expectedTurn, CoilTurnToleranceDeg, "deg");
            ModLog.Verbose("WhipMotion: coil - radius setting " + ModSettings.WhipCoilRadius.Value + " m, used " + _coilRadiusUsed.ToString("0.000")
                + " m, bone 0-1 " + chains.SegmentLength(0, 1).ToString("0.000") + " m, lossyScale " + transform.lossyScale.ToString("F2") + " (info only, not used for sizing)");
            LastCoilOk = mismatchOk && turnOk;
            LastCoilAt = Time.time;
            LastCoilDetail = "mismatch " + chains.LastMismatch.ToString("0.000") + " m (expected 0 +- " + CoilMismatchTolerance + "), max bone turn "
                + chains.LastMaxTurn.ToString("0.0") + " deg (expected " + expectedTurn.ToString("0.0") + " +- " + CoilTurnToleranceDeg + "), radius " + _coilRadiusUsed.ToString("0.000") + " m";
            return mismatchOk && turnOk;
        }

        const double CoilMismatchTolerance = 0.01;    // m: more than 1 cm between a bone and its node = the bones do not follow the coil
        const double CoilTurnToleranceDeg = 3.0;      // deg: the side shift and the radius ease bend a bone a little more than the pure chord angle
        float _coilRadiusUsed, _coilLongest;

        Vector3[] _coil;
        float _nextDiag;
        bool _radiusWarned;
        bool _coilLogged;

        /// The idle coil in world space, rebuilt every frame from the real chain (node spacing = the simulated bone lengths, so the
        /// length constraint never fights the targets): loops hanging under the hand in the vertical plane through the handle axis,
        /// side by side (each loop shifted along the plane normal). Expected outcome: a coil with one point per node.
        bool BuildCoil(FallChains chains)
        {
            if (chains.ChainCount == 0 || chains.SegmentLength(0, 1) <= 1e-5f) return chains.ChainCount > 0 && chains.SegmentLength(0, 1) > 1e-5f;   // not primed yet
            int n = chains.NodeCount(0);
            if (_coil == null || _coil.Length != n) _coil = new Vector3[n];
            var start = chains.RootPosition(0);
            // handle axis from the model origin (middle of the grip) to where the lash starts - no reliance on transform axes or
            // lossyScale (the item's attach chain carries scales that made the coil metres wide: TEST-007 log, mismatch 150 m)
            var axis = start - transform.position;
            if (axis.sqrMagnitude < 1e-8f) axis = transform.up;
            axis.Normalize();
            var down = Vector3.ProjectOnPlane(Vector3.down, axis);
            if (down.sqrMagnitude < 1e-4f) down = Vector3.ProjectOnPlane(transform.right, axis);
            down.Normalize();
            var normal = Vector3.Cross(axis, down).normalized;
            float r = Mathf.Max(0.03f, ModSettings.WhipCoilRadius.Value);     // world metres
            // centre straight below the lash start: the lash leaves the handle going forward (tangent = handle axis), loops down,
            // back and up - no sharp turn at the collar. CoilDrop slides the whole coil along the handle afterwards.
            var shift = axis * ModSettings.WhipCoilDrop.Value;
            var centre = start + down * r;
            var d0 = start - centre;
            float r0 = d0.magnitude;
            float a0 = Mathf.Atan2(Vector3.Dot(d0, down), Vector3.Dot(d0, axis));
            _coil[0] = start;
            // points on the circle spaced by the CHORD of each bone (2 R sin(da/2) = bone length), so the targets keep the bone
            // lengths exactly; the first point sits on a circle of radius r0 through the lash start and the radius eases to r
            // a bone longer than the circle's diameter cannot be a chord of it: grow the radius instead of clamping the angle
            float longest = 0f;
            for (int i = 1; i < n; i++) longest = Mathf.Max(longest, chains.SegmentLength(0, i));
            float minR = longest * 0.5f * 1.05f;
            if (r < minR)
            {
                if (!_radiusWarned) ModLog.Warn("WhipMotion: CoilRadius " + r.ToString("0.000") + " m is smaller than half the longest bone (" + longest.ToString("0.000") + " m) - using " + minR.ToString("0.000") + " m");
                _radiusWarned = true;
                r = minR;
                centre = start + down * r;
                d0 = start - centre;
                r0 = d0.magnitude;
                a0 = Mathf.Atan2(Vector3.Dot(d0, down), Vector3.Dot(d0, axis));
            }
            _coilRadiusUsed = r;
            _coilLongest = longest;
            float a = a0, turned = 0f;
            for (int i = 1; i < n; i++)
            {
                float len = chains.SegmentLength(0, i);
                float rr = Mathf.Lerp(r0, r, Mathf.Clamp01(i / 3f));
                float da = 2f * Mathf.Asin(len / (2f * rr));     // len <= 2 rr guaranteed above (r0 = r here)
                a += da;
                turned += da;
                var p = centre + (axis * Mathf.Cos(a) + down * Mathf.Sin(a)) * rr + normal * (0.02f * turned / (2f * Mathf.PI));
                // exact length from the previous point (the side shift and the radius change would stretch it a little)
                var d = p - _coil[i - 1];
                _coil[i] = d.sqrMagnitude > 1e-10f ? _coil[i - 1] + d.normalized * len : p;
            }
            for (int i = 1; i < n; i++) _coil[i] += shift * Mathf.Clamp01(i / 4f);     // ease the shift in so the first bones stay attached
            return _coil.Length == n;
        }

        Character _grabLogged;

        /// Distance s (m) along the lash: first straight to the target's body, then a helix around it (radius r, slowly going down).
        static Vector3 GrabTarget(Vector3 start, Vector3 head, float s, float r)
        {
            var toHead = head - start;
            float reach = Mathf.Max(0.01f, toHead.magnitude - r);
            if (s <= reach) return start + toHead.normalized * s;
            float a = (s - reach) / r;
            var side = Vector3.Cross(Vector3.up, toHead.normalized).normalized;
            var back = Vector3.Cross(side, Vector3.up);
            return head + (side * Mathf.Cos(a) - back * Mathf.Sin(a)) * r + Vector3.down * (0.02f * a);
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
