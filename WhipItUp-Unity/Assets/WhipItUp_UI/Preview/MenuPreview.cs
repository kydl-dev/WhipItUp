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

        [Tooltip("Tab shown when Play starts: 0 = Viewer, 1 = Details (the preview has no click tool for the tab bar, so this checks the other tab without clicking).")]
        [SerializeField] int startTab;

        [Tooltip("Family the menu starts on: Paddle, Flogger or Whip; empty = Paddle (preview only).")]
        [SerializeField] string startFamily = "";

        [Tooltip("UI pixels the body is scrolled once the layout exists (preview only: the Unity MCP has no scroll tool). 0 = top.")]
        [SerializeField] float startScroll;

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            root.Add(MainMenuView.Build(UiText.English, StartWith(SampleWeapons(), startFamily), () => Debug.Log("[MenuPreview] close pressed"), startTab, startScroll));
            Debug.Log("[MenuPreview] WhipItUp menu shown, start tab " + startTab);
        }

        void OnDisable() { UiKit.Reset(); }

        // The list with the weapons of `family` first (the menu starts on the first weapon of the list); empty or unknown family = unchanged.
        static List<WeaponInfo> StartWith(List<WeaponInfo> all, string family)
        {
            if (string.IsNullOrEmpty(family)) return all;
            var first = all.FindAll(w => w.Family == family);
            if (first.Count == 0) { Debug.LogWarning("[MenuPreview] startFamily '" + family + "' has no sample weapons, keeping the list order"); return all; }
            var list = new List<WeaponInfo>(first);
            list.AddRange(all.FindAll(w => w.Family != family));
            Debug.Log("[MenuPreview] the list starts with " + family + " (" + first.Count + " weapon(s) first)");
            return list;
        }

        // Preview-only data (the game builds the real list in src/Ui/WeaponCatalog.cs from the item names): 3 families x 7 ranks of sample text; one
        // entry carries the longest real name and description so wrapping shows.
        static List<WeaponInfo> SampleWeapons()
        {
            var list = new List<WeaponInfo>();
            foreach (var family in WeaponSelection.Families)
                for (int rank = 1; rank <= 7; rank++)
                {
                    bool longest = family == "Whip" && rank == 6;
                    var info = new WeaponInfo(family, rank,
                        longest ? "Charred and Dangerous" : family + " sample " + rank,
                        longest ? "Warning: prolonged exposure may cause singed eyebrows, colourful language, and sudden respect." : "Sample description of " + family + " V" + rank + ".");
                    WeaponDetailsBuilder.Fill(info, SampleStats(family, rank), UiText.English, line => Debug.Log("[MenuPreview] " + line));
                    list.Add(info);
                }
            return list;
        }

        // Preview-only numbers (the game reads the real ones in src/Ui/WeaponStatsReader.cs): they only show how the Details tab lays out
        // quality 1 beside quality 4, a bleed line, attack 2 with an effect line and a recipe with several ingredients. Not the real balance.
        static WeaponStats SampleStats(string family, int rank)
        {
            bool paddle = family == "Paddle";
            float baseDamage = 20f + 10f * (rank - 1);
            var s = new WeaponStats
            {
                Family = family, Rank = rank, Skill = paddle ? "Clubs" : "Whips", Weight = 2f, OneHanded = true, MaxQuality = 4,
                AttackForce = 20f + 5f * rank, BlockPower = 10f + 2f * rank, ParryBonus = 2f, DeflectionForce = 15f + rank,
                Durability = 200f + 50f * (rank - 1), DurabilityPerLevel = 40f,
                Station = rank < 3 ? "Workbench" : rank < 6 ? "Forge" : "Black forge", StationLevel = 1 + (rank - 1) % 3,
                BleedPerSecond = paddle ? 0f : 4f + rank, BleedSeconds = paddle ? 0f : 5f,
                Attack2 = new WeaponStats.AttackLine { DamageMultiplier = 2.5f, ForceMultiplier = 2f, StaggerMultiplier = 0.8f, Stamina = 18f, Range = 2.2f },
            };
            s.Damage.Add(new WeaponStats.DamageLine(paddle ? "blunt" : "slash", baseDamage, baseDamage * 0.2f));
            s.Attack1.StaggerMultiplier = 0.4f;
            s.Attack1.Stamina = 10f + rank;
            s.Attack1.Range = paddle ? 2f : family == "Whip" ? 4.8f : 2.6f;
            s.Ingredients.Add(new WeaponStats.IngredientLine("Leather scraps", 10 + 2 * rank, 5));
            s.Ingredients.Add(new WeaponStats.IngredientLine("Resin", 2 + rank, 1));
            return s;
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
