using Kydl.Common;
using Kydl.Ui;
using UnityEngine;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Game side of the Viewer tab (T-015 S5): makes the shared WeaponViewerRig with the real item models (WeaponFamily.CreateViewerModel:
    /// the item's mesh + rank material in the rest pose, or the whip coiled) on a layer no game object uses. One rig per menu open; the viewport disposes it on close.
    /// </summary>
    internal static class WeaponViewerScene
    {
        /// Layers tried first (high, unnamed ones); the first that has no name in the game is used.
        const int FirstLayer = 31, LastLayer = 24;

        /// Expected outcome: a rig (never null; with no free layer it uses the last one tried and says so).
        internal static IWeaponScene Make(ViewerSettings settings)
        {
            int layer = PickLayer();
            var main = Camera.main;
            bool mainSees = main != null && (main.cullingMask & (1 << layer)) != 0;
            ModLog.Verbose("WeaponViewerScene: layer " + layer + " ('" + LayerMask.LayerToName(layer) + "'), main camera " + (main != null ? "'" + main.name + "' far clip " + main.farClipPlane.ToString("0") + " m, sees the layer " + mainSees + ", rendering path " + main.actualRenderingPath + " (deferred ignores light masks above 4 layers: D-077 F-1)" : "none")
                + " (the rig stands " + Vector3.Distance(main != null ? main.transform.position : Vector3.zero, WeaponViewerRig.Park).ToString("0") + " m away)");
            return new WeaponViewerRig((w, coiled) => WhipItUp.Weapons.WeaponFamily.CreateViewerModel(w.Family, w.Rank, coiled), layer, settings, UiKit.Track, s => ModLog.Verbose(s));
        }

        static int PickLayer()
        {
            for (int i = FirstLayer; i >= LastLayer; i--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
            ModLog.Warn("WeaponViewerScene: layers " + LastLayer + ".." + FirstLayer + " all have names in the game - using " + FirstLayer + " ('" + LayerMask.LayerToName(FirstLayer) + "'); the view may show other objects");
            return FirstLayer;
        }
    }
}
