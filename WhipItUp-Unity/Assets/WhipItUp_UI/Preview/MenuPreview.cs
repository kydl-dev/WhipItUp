using System.Collections.Generic;
using Kydl.Ui;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WhipItUp.Ui
{
    /// <summary>
    /// Unity-only preview of the mod's menu (NOT compiled into the mod: only Runtime/ is). Add this component to any GameObject of an empty
    /// scene and press Play: the menu opens in the Game view in the same look as in game (game fonts from Assets/Kydl.Ui/Preview/GameAssets).
    /// Set the Game view to 1920x1080 (the panel scales from that like in game).
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        const string FontFolder = "Assets/Kydl.Ui/Preview/GameAssets/";

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            root.Add(MainMenuView.Build(UiText.English, SampleWeapons(), () => Debug.Log("[MenuPreview] close pressed")));
            Debug.Log("[MenuPreview] WhipItUp menu shown");
        }

        void OnDisable() { UiKit.Reset(); }

        // Preview-only data (the game builds the real list in src/Ui/WeaponCatalog.cs from the item names): 3 families x 7 ranks of sample text; one
        // entry carries the longest real name and description so wrapping shows.
        static List<WeaponInfo> SampleWeapons()
        {
            var list = new List<WeaponInfo>();
            foreach (var family in WeaponSelection.Families)
                for (int rank = 1; rank <= 7; rank++)
                {
                    bool longest = family == "Whip" && rank == 6;
                    list.Add(new WeaponInfo(family, rank,
                        longest ? "Charred and Dangerous" : family + " sample " + rank,
                        longest ? "Warning: prolonged exposure may cause singed eyebrows, colourful language, and sudden respect." : "Sample description of " + family + " V" + rank + "."));
                }
            return list;
        }

        // UiKit finds the game fonts by name among the loaded fonts (Norse, Norsebold, NotoEmoji-Regular): load them first.
        static void LoadGameFonts()
        {
#if UNITY_EDITOR
            foreach (var f in new[] { "Norse.otf", "Norsebold.otf", "NotoEmoji-Regular.ttf" })
                if (AssetDatabase.LoadAssetAtPath<Font>(FontFolder + f) == null) Debug.LogWarning("[MenuPreview] font missing: " + FontFolder + f + " (Kydl > Sync shared UI kit)");
#endif
        }
    }
}
