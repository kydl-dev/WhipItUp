using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>D-110 / D-111: how a coiled whip lies on an item stand (split out of WhipMotion to keep it under 500 lines).</summary>
    internal static class WhipStand
    {
        const float StandClearance = 0.05f;     // m between the board surface and the whip's centre line (grip rings ~0.04 m radius at scale 1.5)
        const float StandHandleTiltDeg = 30f;   // the handle crosses the board this much below the horizontal (user: like the armor stand / viewer)

        /// User 2026-10-25 (D-110, screenshots 001 / 002 of session 143136): on an item stand the coil hung edge-on (wall stand) or into the
        /// rock (ground stand) and off centre. Here the whole whip is laid INTO the stand's board: the board's thin axis is its normal
        /// (wall stand: out of the wall, ground stand: up), the coil lies in that plane, the handle crosses it StandHandleTiltDeg below the
        /// board's horizontal with the pommel to one side, and the middle between the coil centre and the grip sits on the board centre.
        /// Runs once per stand visual (the stand does not move). Expected outcome: the board normal the coil must lie in (zero = not posed).
        internal static Vector3 LayIntoBoard(Transform transform, ItemStand stand, Vector3 start)
        {
            if (stand == null) return Vector3.zero;
            MeshFilter board = null;
            float bestVol = 0f;
            foreach (var mf in stand.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || mf.transform.IsChildOf(transform)) continue;
                var sz = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
                float vol = sz.x * sz.y * sz.z;
                if (vol > bestVol) { bestVol = vol; board = mf; }
            }
            if (board == null) { ModLog.Verbose("WhipMotion: item stand " + stand.name + " has no board mesh - coil left as is"); return Vector3.zero; }
            var b = board.sharedMesh.bounds;
            var size = Vector3.Scale(b.size, board.transform.lossyScale);
            Vector3 localN = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
            var n = board.transform.TransformDirection(localN).normalized;
            var boardCentre = board.transform.TransformPoint(b.center);
            float outward = Vector3.Dot(transform.position - boardCentre, n);    // keep the item's distance in front of the board
            if (outward < 0f) { n = -n; outward = -outward; }
            // user 2026-10-25 (TEST-038): on the ground stand the game puts the item 0.027 m above the middle of a 0.05 m block, so the
            // lash and the grip rings sank into it. Keep at least half the board's thickness + StandClearance in front of its middle.
            float thin = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            float minOut = thin * 0.5f + StandClearance;
            if (outward < minOut) { ModLog.Verbose("WhipMotion: " + stand.name + ": " + outward.ToString("0.000") + " m in front of the board middle is inside the whip's thickness - lifted to " + minOut.ToString("0.000") + " m"); outward = minOut; }
            // in-plane directions: 'down' = world down on the board (a ground board: keep the handle's current heading instead)
            var axisNow = start - transform.position;
            float h = axisNow.magnitude;
            if (h < 1e-4f) { ModLog.Verbose("WhipMotion: item stand pose skipped - lash start on the grip"); return Vector3.zero; }
            var dn = Vector3.ProjectOnPlane(Vector3.down, n);
            if (dn.sqrMagnitude < 0.05f) dn = Vector3.ProjectOnPlane(-axisNow, n);
            dn.Normalize();
            var side = Vector3.Cross(n, dn).normalized;
            // grip -> collar points up-and-across; the pommel ends low on the other side
            float t = StandHandleTiltDeg * Mathf.Deg2Rad;
            var axisT = (side * Mathf.Cos(t) - dn * Mathf.Sin(t)).normalized;
            transform.rotation = Quaternion.FromToRotation(axisNow.normalized, axisT) * transform.rotation;
            float r = Mathf.Max(0.03f, ModSettings.WhipCoilRadius.Value);
            var downT = WhipMotion.CoilDown(axisT, n, transform.right);
            var coilCentre = transform.position + axisT * h + downT * r;
            var mid = (coilCentre + transform.position) * 0.5f;
            var target = boardCentre + n * outward;
            var move = target - mid;
            transform.position += move;
            ModLog.Verbose("WhipMotion: laid into " + stand.name + " board '" + board.name + "' (" + size.ToString("F2") + " m, normal " + n.ToString("F2") + ", " + outward.ToString("0.000")
                + " m in front): handle " + StandHandleTiltDeg + " deg below the board's horizontal, moved " + move.magnitude.ToString("0.000") + " m to centre coil + grip on the board");
            return n;
        }
    }
}
