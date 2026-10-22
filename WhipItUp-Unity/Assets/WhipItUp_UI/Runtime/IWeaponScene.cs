using System;
using UnityEngine;

namespace WhipItUp.Ui
{
    /// <summary>
    /// What the Viewer tab's viewport talks to: something that puts one weapon in front of a camera and renders it into a texture.
    /// The Unity preview and the game both use WeaponViewerRig; they differ only in how the weapon's model is made (preview: the FBX + rank
    /// textures from the Unity project, game: a copy of the item's visual, T-015 S5). Dispose removes the camera, light, texture and model.
    /// </summary>
    internal interface IWeaponScene : IDisposable
    {
        /// The picture the viewport shows (null until Resize made it).
        Texture Texture { get; }

        /// Shows that weapon (removes the one before); `coiled` asks for the coil pose (only the whip has one, ViewerPose.ShowsCoiled, D-057; the
        /// rest pose otherwise). Expected outcome: returns whether a model is shown.
        bool Show(WeaponInfo weapon, bool coiled);

        /// The viewport's size in screen pixels; makes a new texture when it changed. Expected outcome: returns whether a texture exists.
        bool Resize(int widthPx, int heightPx);

        /// Renders once from that view. `pan` = where the camera looks, as an offset from the weapon's centre in world axes, in view heights at zoom 1
        /// (ViewerOrbit.PanX / Y / Z). Expected outcome: returns whether something was rendered.
        bool Render(float yawDeg, float pitchDeg, float zoom, float panX, float panY, float panZ);
    }
}
