using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The Paddle family (T-004): Paddle_V1..V7, clones of the vanilla Club (Clubs skill, club animations, whoosh + slap sounds: Sounds/WeaponSounds).
    /// Stats: the crop ladder (Ladder) except no bleed; blunt = crop blunt + one crop bleed tick; crop's absolute stagger (user 2026-10-21).
    /// Attack 2 = the Flametal mace's secondary attack with a back-handed wrist roll (SetSecondaryAttack, BackhandRoll; T-005).
    /// Names are placeholders until T-012 (the user approves the real names).
    /// </summary>
    internal static class PaddleItems
    {
        public const string Family = "Paddle";
        const string VanillaBase = "Club";
        const string SecondarySource = "MaceEldner";     // Flametal mace: its secondary attack is the paddle's attack 2
        const float IconTiltDegrees = 45f;

        static WeaponArt _art;
        static readonly List<CustomItem> Items = new List<CustomItem>();

        /// Called from Plugin.Awake once the bundle is loaded. Expected outcome: the art is loaded and the item hook is set.
        public static bool Register(AssetBundle bundle)
        {
            _art = new WeaponArt(Family);
            bool loaded = _art.Load(bundle);
            if (_art.Mesh == null) { ModLog.Error("PaddleItems: no paddle mesh - the paddles are NOT added"); return _art.Mesh != null; }
            PrefabManager.OnVanillaPrefabsAvailable += AddAll;
            ModSettings.PaddleAlignmentChanged += () => _art.ApplyAlignment(ModSettings.PaddleRotation, ModSettings.PaddleGrip.Value, ModSettings.PaddleScale.Value);
            ModLog.Verbose("PaddleItems: registered (art complete " + loaded + "), waiting for the vanilla prefabs");
            return loaded;
        }

        static void AddAll()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddAll;
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                try { Add(rank); }
                catch (Exception ex) { ModLog.Error("PaddleItems: could not add " + Ladder.PrefabName(Family, rank) + ": " + ex); }
            }
            if (Items.Count < Ladder.Ranks) ModLog.Error("PaddleItems: only " + Items.Count + " of " + Ladder.Ranks + " paddles added - see the errors above");
            else ModLog.Info("PaddleItems: " + Items.Count + " paddles added");
            foreach (var line in Ladder.Describe(Family, Ladder.PaddleBlunt)) ModLog.Verbose("PaddleItems: " + line);
        }

        static void Add(int rank)
        {
            string name = Ladder.PrefabName(Family, rank);
            if (!_art.HasRank(rank)) { ModLog.Error("PaddleItems: skipping " + name + " (its texture is not in the bundle)"); return; }
            int i = Ladder.Index(rank);
            var config = new ItemConfig
            {
                Name = "Paddle " + RankWord(rank),
                Description = "A studded leather paddle with a wolf-crest pommel. Placeholder text until the names are chosen.",
                CraftingStation = Ladder.Station[i],
                MinStationLevel = Ladder.StationLevel[i],
                Requirements = Ladder.Ingredients[i].Select(g => new RequirementConfig(g.Item, g.Amount, g.AmountPerLevel, true)).ToArray(),
            };
            var item = new CustomItem(name, VanillaBase, config);
            if (item.ItemPrefab == null) { ModLog.Error("PaddleItems: could not clone '" + VanillaBase + "' for " + name); return; }

            _art.Apply(item.ItemPrefab, rank, () => ModSettings.PaddleRotation, () => ModSettings.PaddleGrip.Value, () => ModSettings.PaddleScale.Value);
            var shared = item.ItemDrop.m_itemData.m_shared;
            ApplyStats(shared, rank, name);
            Sounds.WeaponSounds.Apply(shared, name);
            ItemManager.Instance.AddItem(item);
            Items.Add(item);
            RenderIcon(item, shared, name);
            ModLog.Verbose("PaddleItems: " + name + " added (" + config.CraftingStation + " " + config.MinStationLevel + ", " + string.Join(", ", Ladder.Ingredients[i].Select(g => g.ToString())) + ")");
        }

        /// Expected outcome: the shared data holds the ladder's values for this rank (returns that check).
        static bool ApplyStats(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            int i = Ladder.Index(rank);
            float clubBlunt = shared.m_damages.m_blunt;
            float blunt = Ladder.PaddleBlunt(rank);
            shared.m_damages = new HitData.DamageTypes { m_blunt = blunt };
            shared.m_damagesPerLevel = new HitData.DamageTypes { m_blunt = Ladder.BluntPerLevel };
            shared.m_maxQuality = Ladder.MaxQuality;
            shared.m_maxDurability = Ladder.Durability(rank);
            shared.m_durabilityPerLevel = Ladder.DurabilityPerLevel;
            shared.m_attack.m_attackStamina = Ladder.Stamina[i];
            shared.m_attack.m_staggerMultiplier = Ladder.StaggerMultiplier(rank, blunt);
            shared.m_attack.m_lastChainDamageMultiplier = Ladder.LastChainDamageMultiplier;
            shared.m_attackForce = Ladder.Knockback[i];
            shared.m_blockPower = Ladder.BlockArmor[i];
            shared.m_timedBlockBonus = Ladder.ParryBonus;
            shared.m_deflectionForce = Ladder.BlockForce[i];
            SetSecondaryAttack(shared, rank, blunt, name);
            shared.m_attackStatusEffect = null;     // no bleed on the paddle (user)
            ModLog.Verbose("PaddleItems: " + name + " blunt " + blunt.ToString("0.#") + " (Club " + clubBlunt.ToString("0.#") + ", crop " + Ladder.CropBlunt(rank).ToString("0.#")
                + " + bleed tick " + Ladder.CropBleedPerSecond(rank).ToString("0.#") + "), stagger " + Ladder.Stagger[i] + ", durability " + shared.m_maxDurability
                + ", attack 2 " + (shared.m_secondaryAttack != null && !string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation) ? shared.m_secondaryAttack.m_attackAnimation : "none"));
            return Mathf.Approximately(shared.m_damages.m_blunt, blunt) && shared.m_maxQuality == Ladder.MaxQuality;
        }

        /// Attack 2 (T-005, user 2026-10-21): a copy of the Flametal mace's (MaceEldner) secondary attack, "mace_secondary", with the mace's own
        /// damage / force / stagger multipliers on top of this rank; stamina = the rank's attack-1 stamina x BackhandStaminaFactor.
        /// The wrist roll during it is BackhandRoll. Expected outcome: the paddle has a secondary attack with that animation.
        static bool SetSecondaryAttack(ItemDrop.ItemData.SharedData shared, int rank, float blunt, string name)
        {
            var mace = PrefabManager.Instance.GetPrefab(SecondarySource);
            var maceDrop = mace != null ? mace.GetComponent<ItemDrop>() : null;
            var src = maceDrop != null ? maceDrop.m_itemData.m_shared.m_secondaryAttack : null;
            if (src == null || string.IsNullOrEmpty(src.m_attackAnimation))
            {
                ModLog.Error("PaddleItems: '" + SecondarySource + "' or its secondary attack not found - " + name + " has no attack 2");
                return shared.m_secondaryAttack != null && !string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation);
            }
            var a = src.Clone();
            int i = Ladder.Index(rank);
            float maceStagger = a.m_staggerMultiplier;
            a.m_attackStamina = Ladder.Stamina[i] * ModSettings.BackhandStaminaFactor.Value;
            a.m_staggerMultiplier = Ladder.StaggerMultiplier(rank, blunt) * maceStagger;   // keeps the mace's "x2 of attack 1" stagger
            shared.m_secondaryAttack = a;
            BackhandRoll.RegisterPaddle(name);
            ModLog.Verbose("PaddleItems: " + name + " attack 2 = " + SecondarySource + " '" + a.m_attackAnimation + "' (type " + a.m_attackType + ", damage x" + a.m_damageMultiplier
                + ", force x" + a.m_forceMultiplier + ", stagger x" + maceStagger + ", stamina " + a.m_attackStamina.ToString("0.#") + ")");
            return shared.m_secondaryAttack.m_attackAnimation == BackhandRoll.SecondaryState;
        }

        static void RenderIcon(CustomItem item, ItemDrop.ItemData.SharedData shared, string name)
        {
            GameObject source = null;
            try
            {
                var visual = item.ItemPrefab.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.gameObject.name == WeaponArt.VisualName);
                if (visual == null) { ModLog.Warn("PaddleItems: no visual on " + name + " - it keeps the Club's icon"); return; }
                source = new GameObject(name + "_IconSource");
                source.AddComponent<MeshFilter>().sharedMesh = _art.Mesh;
                source.AddComponent<MeshRenderer>().sharedMaterial = visual.sharedMaterial;
                var icon = RenderManager.Instance.Render(new RenderManager.RenderRequest(source)
                {
                    Width = 128, Height = 128, Rotation = Quaternion.Euler(0f, 0f, IconTiltDegrees), UseCache = false,
                });
                if (icon == null) { ModLog.Warn("PaddleItems: icon render returned nothing - " + name + " keeps the Club's icon"); return; }
                shared.m_icons = new[] { icon };
                ModLog.Verbose("PaddleItems: " + name + " icon rendered");
            }
            catch (NullReferenceException ex) { ModLog.Warn("PaddleItems: icon render failed (missing render setup) - " + name + " keeps the Club's icon. " + ex.Message); }
            catch (Exception ex) { ModLog.Warn("PaddleItems: icon render failed - " + name + " keeps the Club's icon. " + ex.Message); }
            finally { if (source != null) UnityEngine.Object.DestroyImmediate(source); }
        }

        static string RankWord(int rank) { return "V" + rank; }
    }
}
