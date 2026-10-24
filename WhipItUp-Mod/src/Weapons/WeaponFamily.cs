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
    /// optional attack 2 copied from MaceEldner (+ BackhandRoll wrist roll) and a rendered inventory icon. Names and descriptions come from WeaponNames (T-012).
    /// </summary>
    internal abstract class WeaponFamily
    {
        public abstract string Family { get; }
        protected virtual string VanillaBase { get { return "Club"; } }
        protected const string SecondarySource = "MaceEldner";   // Flametal mace: its secondary attack is attack 2 (D-026)
        protected virtual bool HasMaceSecondary { get { return true; } }
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
        /// Every registered family by name ("Paddle", "Flogger", "Whip"): the F6 viewer asks it for models.
        static readonly Dictionary<string, WeaponFamily> Registered = new Dictionary<string, WeaponFamily>();
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
            Registered[Family] = this;
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
            var ingredients = ResolveIngredients(name, i);
            var config = new ItemConfig
            {
                Name = Kydl.Common.Loc.Token(WeaponNames.Key(Family, rank)),
                Description = Kydl.Common.Loc.Token(WeaponNames.DescKey(Family, rank)),
                CraftingStation = Ladder.Station[i],
                MinStationLevel = Ladder.StationLevel[i],
                Requirements = ingredients.Select(g => new RequirementConfig(g.Item, g.Amount, g.AmountPerLevel, true)).ToArray(),
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
            ModLog.Verbose(Family + "Items: " + name + " '" + Kydl.Common.Loc.T(WeaponNames.Key(Family, rank)) + "' (" + Kydl.Common.Loc.T(WeaponNames.DescKey(Family, rank)) + ") added (" + config.CraftingStation + " " + config.MinStationLevel + ", " + string.Join(", ", ingredients.Select(g => g.ToString())) + ")");
        }

        /// The recipe of one rank with every item id resolved against the game's prefabs (Ladder.Resolve: the user's metal id first, the rip's name as fallback).
        /// An item that is in no form in the game is left out and logged as an error, so one wrong id cannot break the whole recipe.
        /// Expected outcome: the list has one entry per ingredient of the ladder that exists in the game.
        List<Ladder.Ingredient> ResolveIngredients(string name, int index)
        {
            var result = new List<Ladder.Ingredient>();
            foreach (var g in Ladder.Ingredients[index])
            {
                string id = Ladder.Resolve(g, candidate => PrefabManager.Instance.GetPrefab(candidate) != null);
                if (PrefabManager.Instance.GetPrefab(id) == null)
                {
                    ModLog.Error(Family + "Items: " + name + ": ingredient '" + g.Item + "'" + (g.Candidates != null ? " (tried " + string.Join(", ", g.Candidates) + ")" : "") + " is not in the game - the recipe is made WITHOUT it");
                    continue;
                }
                if (id != g.Item) ModLog.Verbose(Family + "Items: " + name + ": ingredient '" + g.Item + "' is not in the game, using '" + id + "'");
                result.Add(new Ladder.Ingredient(id, g.Amount, g.AmountPerLevel, null, g.FlatUpgrade));
            }
            ModLog.Verbose(Family + "Items: " + name + ": recipe " + result.Count + " of " + Ladder.Ingredients[index].Length + " ingredients: " + string.Join(", ", result.Select(g => g.ToString())));
            return result;
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

        /// A new model of that weapon for the F6 viewer: the family mesh with the rank's material, taken from the registered item prefab. The pose
        /// is the rest pose (falls / lash straight) unless `coiled` is asked AND the family has the switch (the whip, ViewerPose, D-057): then the
        /// same baked coil as its icon. The flogger's icon pose (falls hanging) is never used here. The caller owns (and destroys) it.
        /// Null when the family or item is missing (logged).
        internal static GameObject CreateViewerModel(string family, int rank, bool coiled)
        {
            WeaponFamily f;
            if (family == null || !Registered.TryGetValue(family, out f) || f.Art == null)
            {
                ModLog.Verbose("WeaponFamily: viewer model asked for unknown family '" + family + "' (registered: " + string.Join(", ", Registered.Keys.ToArray()) + ")");
                return null;
            }
            string name = Ladder.PrefabName(family, rank);
            var db = ObjectDB.instance;
            var prefab = db != null ? db.GetItemPrefab(name) : null;
            if (prefab == null) { ModLog.Verbose("WeaponFamily: viewer model of " + name + ": no item prefab (ObjectDB " + (db != null ? "has no such item" : "not ready") + ")"); return null; }
            bool coil = coiled && Ui.ViewerPose.HasSwitch(family);
            if (coiled && !coil) ModLog.Verbose("WeaponFamily: viewer model of " + name + ": coiled asked but " + family + " has no coil switch - rest pose");
            var model = f.Art.CreateIconSource(prefab, name, coil);
            ModLog.Verbose("WeaponFamily: viewer model of " + name + (model != null ? " made from the item prefab (" + (coil ? "coiled, the icon pose baked" : "rest pose") + ", skinned family " + f.Art.Skinned + ")" : " NOT made (no visual / material on the item prefab)"));
            return model;
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
                    Width = 128,
                    Height = 128,
                    Rotation = IconRotation,
                    UseCache = false,
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
