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
    /// One weapon family = 7 rank items &lt;Family&gt;_V1..V7 (D-005) cloned from a vanilla weapon, with the family's art from the bundle
    /// (WeaponArt), the crop-ladder stats (Ladder; each family decides its damage in ApplyDamage), the mod's whoosh/slap sounds, an
    /// optional attack 2 copied from MaceEldner (+ BackhandRoll wrist roll) and a rendered inventory icon. Names are placeholders until T-012.
    /// </summary>
    internal abstract class WeaponFamily
    {
        public abstract string Family { get; }
        protected virtual string VanillaBase { get { return "Club"; } }
        protected const string SecondarySource = "MaceEldner";   // Flametal mace: its secondary attack is attack 2 (D-026)
        protected virtual bool HasMaceSecondary { get { return true; } }
        protected abstract string Description { get; }
        protected abstract Vector3 Rotation { get; }
        protected abstract float Grip { get; }
        protected abstract float Scale { get; }
        protected virtual Sounds.HitSound HitSound { get { return Sounds.HitSound.Slap; } }
        protected virtual float IconTiltDegrees { get { return 45f; } }
        /// Rotation of the model in the icon render (Jotunn's camera looks along -Z, screen-right = world -X; the model's long axis is +Y).
        protected virtual Quaternion IconRotation { get { return Quaternion.Euler(0f, 0f, IconTiltDegrees); } }
        /// Bone pose for the icon (skinned families). Null = rest pose.
        protected virtual Action<Transform, Transform[]> PoseForIcon { get { return null; } }

        protected WeaponArt Art;
        protected readonly List<CustomItem> Items = new List<CustomItem>();

        /// The family's damage for a rank (blunt / slash / bleed...). Expected outcome: shared damage set (returns that check).
        protected abstract bool ApplyDamage(ItemDrop.ItemData.SharedData shared, int rank, string name);
        /// The damage the stagger table is measured against (per rank).
        protected abstract float MainDamage(int rank);
        /// Hook for skill etc. Default: keep the vanilla base's skill.
        protected virtual void ApplyExtras(ItemDrop.ItemData.SharedData shared, int rank, string name) { }
        /// Extra components on each skinned visual (default none).
        protected virtual void DecorateVisual(GameObject visual) { }
        /// Attack 2. Default: the mace secondary (+ wrist roll). Expected outcome: the item has the attack 2 the family wants.
        protected virtual bool ConfigureSecondary(ItemDrop.ItemData.SharedData shared, int rank, float main, string name)
        {
            return !HasMaceSecondary || SetSecondaryAttack(shared, rank, main, name);
        }
        /// Called with the rank-1 icon once it rendered.
        protected virtual void OnFirstIcon(Sprite icon) { }

        /// Called from Plugin.Awake once the bundle is loaded. Expected outcome: the art is loaded and the item hook is set.
        public bool Register(AssetBundle bundle, Action<Action> subscribeAlignmentChanged)
        {
            Art = new WeaponArt(Family) { DecorateVisual = DecorateVisual, PoseForIcon = PoseForIcon };
            bool loaded = Art.Load(bundle);
            if (Art.Mesh == null) { ModLog.Error(Family + "Items: no " + Family + " mesh - the " + Family + " items are NOT added"); return Art.Mesh != null; }
            PrefabManager.OnVanillaPrefabsAvailable += AddAll;
            subscribeAlignmentChanged(() => Art.ApplyAlignment(Rotation, Grip, Scale));
            ModLog.Verbose(Family + "Items: registered (art complete " + loaded + ", skinned " + Art.Skinned + "), waiting for the vanilla prefabs");
            return loaded;
        }

        void AddAll()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddAll;
            for (int rank = 1; rank <= Ladder.Ranks; rank++)
            {
                try { Add(rank); }
                catch (Exception ex) { ModLog.Error(Family + "Items: could not add " + Ladder.PrefabName(Family, rank) + ": " + ex); }
            }
            if (Items.Count < Ladder.Ranks) ModLog.Error(Family + "Items: only " + Items.Count + " of " + Ladder.Ranks + " added - see the errors above");
            else ModLog.Info(Family + "Items: " + Items.Count + " items added");
            foreach (var line in Ladder.Describe(Family, MainDamage)) ModLog.Verbose(Family + "Items: " + line);
        }

        void Add(int rank)
        {
            string name = Ladder.PrefabName(Family, rank);
            if (!Art.HasRank(rank)) { ModLog.Error(Family + "Items: skipping " + name + " (its texture is not in the bundle)"); return; }
            int i = Ladder.Index(rank);
            var config = new ItemConfig
            {
                Name = Family + " V" + rank,
                Description = Description,
                CraftingStation = Ladder.Station[i],
                MinStationLevel = Ladder.StationLevel[i],
                Requirements = Ladder.Ingredients[i].Select(g => new RequirementConfig(g.Item, g.Amount, g.AmountPerLevel, true)).ToArray(),
            };
            var item = new CustomItem(name, VanillaBase, config);
            if (item.ItemPrefab == null) { ModLog.Error(Family + "Items: could not clone '" + VanillaBase + "' for " + name); return; }

            Art.Apply(item.ItemPrefab, rank, () => Rotation, () => Grip, () => Scale);
            var shared = item.ItemDrop.m_itemData.m_shared;
            ApplyStats(shared, rank, name);
            ApplyExtras(shared, rank, name);
            Sounds.WeaponSounds.Apply(shared, name, HitSound);
            ItemManager.Instance.AddItem(item);
            Items.Add(item);
            RenderIcon(item, shared, name, rank);
            ModLog.Verbose(Family + "Items: " + name + " added (" + config.CraftingStation + " " + config.MinStationLevel + ", " + string.Join(", ", Ladder.Ingredients[i].Select(g => g.ToString())) + ")");
        }

        /// Expected outcome: the shared data holds the ladder's values for this rank (returns that check).
        bool ApplyStats(ItemDrop.ItemData.SharedData shared, int rank, string name)
        {
            int i = Ladder.Index(rank);
            bool damaged = ApplyDamage(shared, rank, name);
            float main = MainDamage(rank);
            shared.m_maxQuality = Ladder.MaxQuality;
            shared.m_maxDurability = Ladder.Durability(rank);
            shared.m_durabilityPerLevel = Ladder.DurabilityPerLevel;
            shared.m_attack.m_attackStamina = Ladder.Stamina[i];
            shared.m_attack.m_staggerMultiplier = Ladder.StaggerMultiplier(rank, main);
            shared.m_attack.m_lastChainDamageMultiplier = Ladder.LastChainDamageMultiplier;
            shared.m_attackForce = Ladder.Knockback[i];
            shared.m_blockPower = Ladder.BlockArmor[i];
            shared.m_timedBlockBonus = Ladder.ParryBonus;
            shared.m_deflectionForce = Ladder.BlockForce[i];
            ConfigureSecondary(shared, rank, main, name);
            ModLog.Verbose(Family + "Items: " + name + " main damage " + main.ToString("0.#") + ", stagger " + Ladder.Stagger[i] + ", durability " + shared.m_maxDurability
                + ", attack 2 " + (shared.m_secondaryAttack != null && !string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation) ? shared.m_secondaryAttack.m_attackAnimation : "none"));
            return damaged && shared.m_maxQuality == Ladder.MaxQuality;
        }

        /// Attack 2 (D-026): a copy of MaceEldner's secondary attack, mace multipliers on top of the rank; stamina = rank stamina x factor.
        /// Expected outcome: the item has a secondary attack with that animation.
        protected bool SetSecondaryAttack(ItemDrop.ItemData.SharedData shared, int rank, float main, string name)
        {
            var mace = PrefabManager.Instance.GetPrefab(SecondarySource);
            var maceDrop = mace != null ? mace.GetComponent<ItemDrop>() : null;
            var src = maceDrop != null ? maceDrop.m_itemData.m_shared.m_secondaryAttack : null;
            if (src == null || string.IsNullOrEmpty(src.m_attackAnimation))
            {
                ModLog.Error(Family + "Items: '" + SecondarySource + "' or its secondary attack not found - " + name + " has no attack 2");
                return shared.m_secondaryAttack != null && !string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation);
            }
            var a = src.Clone();
            int i = Ladder.Index(rank);
            float maceStagger = a.m_staggerMultiplier;
            a.m_attackStamina = Ladder.Stamina[i] * ModSettings.BackhandStaminaFactor.Value;
            a.m_staggerMultiplier = Ladder.StaggerMultiplier(rank, main) * maceStagger;   // keeps the mace's "x2 of attack 1" stagger
            shared.m_secondaryAttack = a;
            BackhandRoll.RegisterItem(name);
            ModLog.Verbose(Family + "Items: " + name + " attack 2 = " + SecondarySource + " '" + a.m_attackAnimation + "' (type " + a.m_attackType + ", damage x" + a.m_damageMultiplier
                + ", force x" + a.m_forceMultiplier + ", stagger x" + maceStagger + ", stamina " + a.m_attackStamina.ToString("0.#") + ")");
            return shared.m_secondaryAttack.m_attackAnimation == BackhandRoll.SecondaryState;
        }

        void RenderIcon(CustomItem item, ItemDrop.ItemData.SharedData shared, string name, int rank)
        {
            GameObject source = null;
            try
            {
                source = Art.CreateIconSource(item.ItemPrefab, name);
                if (source == null) { ModLog.Warn(Family + "Items: no visual on " + name + " - it keeps the " + VanillaBase + "'s icon"); return; }
                var icon = RenderManager.Instance.Render(new RenderManager.RenderRequest(source)
                {
                    Width = 128, Height = 128, Rotation = IconRotation, UseCache = false,
                });
                if (icon == null) { ModLog.Warn(Family + "Items: icon render returned nothing - " + name + " keeps the " + VanillaBase + "'s icon"); return; }
                shared.m_icons = new[] { icon };
                if (rank == 1) OnFirstIcon(icon);
                ModLog.Verbose(Family + "Items: " + name + " icon rendered");
            }
            catch (NullReferenceException ex) { ModLog.Warn(Family + "Items: icon render failed (missing render setup) - " + name + " keeps the vanilla icon. " + ex.Message); }
            catch (Exception ex) { ModLog.Warn(Family + "Items: icon render failed - " + name + " keeps the vanilla icon. " + ex.Message); }
            finally { if (source != null) UnityEngine.Object.DestroyImmediate(source); }
        }
    }
}
