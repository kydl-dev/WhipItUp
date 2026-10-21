using System.Collections.Generic;
using HarmonyLib;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// T-005: the paddle's secondary attack (the Flametal mace's "mace_secondary", user 2026-10-21) gets a wrist roll so the paddle face
    /// hits back-handed and upright. Sits on every Player (local and remote: it reads the animator, which plays on every client, so other
    /// players see the roll too - nothing is sent over the network). In LateUpdate (after the animator has posed the bones) it adds a
    /// rotation to the right hand bone, blended in and out over the clip's normalised time (MaceAltAttack: 2.13 s, wind-up until ~0.46,
    /// trail 0.46..0.68, hit at 0.57). Angle, axis and timing are Tunables (ModSettings, Paddle section) for live tuning in KydlTools.
    /// </summary>
    internal sealed class BackhandRoll : MonoBehaviour
    {
        public const string SecondaryState = "mace_secondary";
        static readonly int StateHash = Animator.StringToHash(SecondaryState);
        static readonly AccessTools.FieldRef<Character, Animator> AnimatorRef = AccessTools.FieldRefAccess<Character, Animator>("m_animator");
        static readonly AccessTools.FieldRef<Humanoid, VisEquipment> VisRef = AccessTools.FieldRefAccess<Humanoid, VisEquipment>("m_visEquipment");
        static readonly AccessTools.FieldRef<VisEquipment, int> RightItemRef = AccessTools.FieldRefAccess<VisEquipment, int>("m_rightItem");
        /// Stable hashes of every Paddle_V# prefab name (VisEquipment.m_rightItem holds the hash of the equipped item's prefab name).
        static readonly HashSet<int> PaddleHashes = new HashSet<int>();

        Player _player;
        string _phase = "idle";

        public static bool RegisterPaddle(string prefabName) { return PaddleHashes.Add(prefabName.GetStableHashCode()); }

        void Awake() { _player = GetComponent<Player>(); }

        void LateUpdate()
        {
            if (_player == null || !ModSettings.On) return;
            var vis = VisRef(_player);
            if (vis == null || !PaddleHashes.Contains(RightItemRef(vis))) { SetPhase("idle", 0f, 0f); return; }
            var animator = AnimatorRef(_player);
            float t;
            if (animator == null || !TryStateTime(animator, out t)) { SetPhase("idle", 0f, 0f); return; }
            float w = Weight(t);
            if (w <= 0f) { SetPhase(t < ModSettings.BackhandInFull.Value ? "wind-up" : "done", t, w); return; }
            var hand = vis.m_rightHand != null ? vis.m_rightHand.parent : null;     // m_rightHand = the attach point; its parent = the hand bone
            if (hand == null) { ModLog.Warn("BackhandRoll: no right hand bone on " + _player.GetPlayerName()); return; }
            hand.localRotation = hand.localRotation * Quaternion.AngleAxis(ModSettings.BackhandAngle.Value * w, ModSettings.BackhandAxis);
            SetPhase(w >= 1f ? "rolled" : "rolling", t, w);
        }

        /// Normalised time (0..1) of mace_secondary on any layer (current state, or the next one while blending into it).
        static bool TryStateTime(Animator a, out float t)
        {
            for (int layer = 0; layer < a.layerCount; layer++)
            {
                var cur = a.GetCurrentAnimatorStateInfo(layer);
                if (cur.shortNameHash == StateHash) { t = cur.normalizedTime; return t >= 0f; }
                if (a.IsInTransition(layer))
                {
                    var next = a.GetNextAnimatorStateInfo(layer);
                    if (next.shortNameHash == StateHash) { t = next.normalizedTime; return t >= 0f; }
                }
            }
            t = -1f;
            return t >= 0f;
        }

        /// 0 before InStart, smooth ramp to 1 at InFull, 1 until OutStart, smooth ramp to 0 at OutEnd.
        internal static float Weight(float t)
        {
            float a = ModSettings.BackhandInStart.Value, b = ModSettings.BackhandInFull.Value;
            float c = ModSettings.BackhandOutStart.Value, d = ModSettings.BackhandOutEnd.Value;
            if (t <= a || t >= d) return 0f;
            if (t < b) return Mathf.SmoothStep(0f, 1f, (t - a) / Mathf.Max(0.001f, b - a));
            if (t <= c) return 1f;
            return Mathf.SmoothStep(1f, 0f, (t - c) / Mathf.Max(0.001f, d - c));
        }

        void SetPhase(string phase, float t, float w)
        {
            if (phase == _phase) return;
            _phase = phase;
            ModLog.Verbose("BackhandRoll: " + (_player != null ? _player.GetPlayerName() : "?") + " " + phase + " at t " + t.ToString("0.00") + " (weight " + w.ToString("0.00")
                + ", angle " + ModSettings.BackhandAngle.Value + " around " + ModSettings.BackhandAxis + ")");
        }
    }

    /// Puts a BackhandRoll on every Player when it is created (local and remote).
    [HarmonyPatch(typeof(Player), "Awake")]
    internal static class BackhandRollAttachPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            if (__instance.GetComponent<BackhandRoll>() == null) __instance.gameObject.AddComponent<BackhandRoll>();
            ModLog.Verbose("BackhandRoll: added to a player object (" + __instance.name + ")");
        }
    }
}
