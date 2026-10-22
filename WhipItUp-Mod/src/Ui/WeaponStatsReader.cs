
using System;
using System.Collections.Generic;
using System.Globalization;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Game side of the Details tab: reads the numbers of one weapon from the item the game really uses (ObjectDB item prefab -> ItemDrop shared
    /// data, its recipe) into a plain WeaponStats. Reading them live means values the user changes (KydlTools, config) show up the next time
    /// the menu opens. Not unit-testable (game types); every value read is logged when debug is on. WeaponDetailsBuilder turns the result into rows.
    /// </summary>
    internal static class WeaponStatsReader
    {
        /// The numbers of `family` V`rank`. Null (and a log line saying why) when the game's item database or the item is not there yet, e.g. in the main menu.
        /// Expected outcome: a WeaponStats that describes the same family and rank, or null.
        internal static WeaponStats Read(string family, int rank)
        {
            string name = Weapons.Ladder.PrefabName(family, rank);
            var db = ObjectDB.instance;
            if (db == null) { ModLog.Verbose("WeaponStatsReader: no ObjectDB yet (not in a world?), no stats for " + name); return null; }
            var prefab = db.GetItemPrefab(name);
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null) { ModLog.Verbose("WeaponStatsReader: item '" + name + "' is not in the ObjectDB (prefab " + (prefab != null) + "), no stats"); return null; }
            try
            {
                var sh = drop.m_itemData.m_shared;
                var s = new WeaponStats
                {
                    Family = family,
                    Rank = rank,
                    Skill = SkillName(sh.m_skillType),
                    Weight = sh.m_weight,
                    OneHanded = sh.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon,
                    MaxQuality = sh.m_maxQuality,
                    AttackForce = sh.m_attackForce,
                    Attack1 = Line(sh.m_attack),
                    BlockPower = sh.m_blockPower,
                    BlockPowerPerLevel = sh.m_blockPowerPerLevel,
                    ParryBonus = sh.m_timedBlockBonus,
                    DeflectionForce = sh.m_deflectionForce,
                    DeflectionForcePerLevel = sh.m_deflectionForcePerLevel,
                    Durability = sh.m_maxDurability,
                    DurabilityPerLevel = sh.m_durabilityPerLevel,
                };
                var d = sh.m_damages;
                var p = sh.m_damagesPerLevel;
                AddDamage(s, "blunt", d.m_blunt, p.m_blunt);
                AddDamage(s, "slash", d.m_slash, p.m_slash);
                AddDamage(s, "pierce", d.m_pierce, p.m_pierce);
                AddDamage(s, "chop", d.m_chop, p.m_chop);
                AddDamage(s, "pickaxe", d.m_pickaxe, p.m_pickaxe);
                AddDamage(s, "fire", d.m_fire, p.m_fire);
                AddDamage(s, "frost", d.m_frost, p.m_frost);
                AddDamage(s, "lightning", d.m_lightning, p.m_lightning);
                AddDamage(s, "poison", d.m_poison, p.m_poison);
                AddDamage(s, "spirit", d.m_spirit, p.m_spirit);

                var bleed = sh.m_attackStatusEffect as Weapons.SE_WhipItUpBleed;
                if (bleed != null && bleed.m_tickInterval > 0f)
                {
                    s.BleedPerSecond = bleed.m_damagePerTick / bleed.m_tickInterval;
                    s.BleedSeconds = bleed.m_ticksTotal * bleed.m_tickInterval;
                }
                if (sh.m_secondaryAttack != null && !string.IsNullOrEmpty(sh.m_secondaryAttack.m_attackAnimation)) s.Attack2 = Line(sh.m_secondaryAttack);
                ReadRecipe(db, drop, s);

                ModLog.Verbose("WeaponStatsReader: " + name + ": skill " + s.Skill + ", weight " + N(s.Weight) + " kg, max quality " + s.MaxQuality + ", damage [" + DamageText(s) + "], bleed "
                    + (s.BleedPerSecond > 0f ? N(s.BleedPerSecond) + "/s for " + N(s.BleedSeconds) + " s" : "none") + ", force " + N(s.AttackForce) + ", attack 1 " + LineText(s.Attack1)
                    + ", attack 2 " + (s.Attack2 != null ? LineText(s.Attack2) : "none") + ", block " + N(s.BlockPower) + " (+" + N(s.BlockPowerPerLevel) + "/level), parry x" + N(s.ParryBonus)
                    + ", deflection " + N(s.DeflectionForce) + " (+" + N(s.DeflectionForcePerLevel) + "/level), durability " + N(s.Durability) + " (+" + N(s.DurabilityPerLevel) + "/level), station '"
                    + s.Station + "' " + s.StationLevel + ", " + s.Ingredients.Count + " ingredient(s)");
                return s;
            }
            catch (NullReferenceException ex)
            {
                ModLog.Warn("WeaponStatsReader: " + name + " has missing item data (" + ex.Message + ") - its Details tab shows no numbers");
                return null;
            }
            catch (Exception ex)
            {
                ModLog.Warn("WeaponStatsReader: could not read " + name + " (" + ex.GetType().Name + ": " + ex.Message + ") - its Details tab shows no numbers");
                return null;
            }
        }

        static WeaponStats.AttackLine Line(Attack a)
        {
            return new WeaponStats.AttackLine
            {
                DamageMultiplier = a.m_damageMultiplier,
                ForceMultiplier = a.m_forceMultiplier,
                StaggerMultiplier = a.m_staggerMultiplier,
                Stamina = a.m_attackStamina,
                Range = a.m_attackRange,
            };
        }

        /// Only damage types the weapon really has (a type with base 0 and no per-level gain is left out).
        static void AddDamage(WeaponStats s, string type, float baseValue, float perLevel)
        {
            if (baseValue > 0f || perLevel > 0f) s.Damage.Add(new WeaponStats.DamageLine(type, baseValue, perLevel));
        }

        static void ReadRecipe(ObjectDB db, ItemDrop drop, WeaponStats s)
        {
            var recipe = db.GetRecipe(drop.m_itemData);
            if (recipe == null) { ModLog.Verbose("WeaponStatsReader: no recipe for " + drop.name); return; }
            s.Station = recipe.m_craftingStation != null ? Localize(recipe.m_craftingStation.m_name) : "";
            s.StationLevel = recipe.m_minStationLevel;
            foreach (var req in recipe.m_resources)
            {
                if (req == null || req.m_resItem == null) continue;
                s.Ingredients.Add(new WeaponStats.IngredientLine(Localize(req.m_resItem.m_itemData.m_shared.m_name), req.m_amount, req.m_amountPerLevel));
            }
        }

        /// The shown name of the skill an item trains: our Whips skill, else the game's own text.
        static string SkillName(Skills.SkillType type)
        {
            if (type != Skills.SkillType.None && type == Weapons.WhipSkill.Type) return Weapons.WhipSkill.Name;
            return Localize("$skill_" + type.ToString().ToLowerInvariant());
        }

        static string Localize(string token) { return Localization.instance != null ? Localization.instance.Localize(token) : token; }

        static string DamageText(WeaponStats s)
        {
            var parts = new List<string>();
            foreach (var d in s.Damage) parts.Add(d.Type + " " + N(d.Base) + " (+" + N(d.PerLevel) + "/level)");
            return string.Join(", ", parts.ToArray());
        }

        static string LineText(WeaponStats.AttackLine a)
        {
            return "(damage x" + N(a.DamageMultiplier) + ", force x" + N(a.ForceMultiplier) + ", stagger x" + N(a.StaggerMultiplier) + ", stamina " + N(a.Stamina) + ", reach " + N(a.Range) + " m)";
        }

        static string N(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }
    }
}
