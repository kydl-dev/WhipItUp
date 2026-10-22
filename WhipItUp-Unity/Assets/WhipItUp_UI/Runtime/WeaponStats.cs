
using System.Collections.Generic;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The numbers of one weapon as the game uses them, read from the live item (src/Ui/WeaponStatsReader.cs) and turned into labelled rows by
    /// WeaponDetailsBuilder. Plain data, no Unity / Valheim types, so the builder is unit-tested and the Unity preview can fill it with samples.
    /// Everything is quality 1 / skill 0; the per-level fields say how much each quality level above 1 adds.
    /// </summary>
    internal sealed class WeaponStats
    {
        /// One damage type: `Type` is the key suffix of the text "whipitup_dmg_&lt;type&gt;" (blunt, slash, pierce, chop, pickaxe, fire, frost, lightning, poison, spirit).
        internal sealed class DamageLine
        {
            internal readonly string Type;
            internal readonly float Base;
            internal readonly float PerLevel;
            internal DamageLine(string type, float baseValue, float perLevel) { Type = type; Base = baseValue; PerLevel = perLevel; }
        }

        /// One attack (primary or secondary): the multipliers are applied on top of the item's damage and attack force.
        internal sealed class AttackLine
        {
            internal float DamageMultiplier = 1f;
            internal float ForceMultiplier = 1f;
            /// The game's stagger multiplier of this attack (stagger = multiplier x the damage that hits).
            internal float StaggerMultiplier;
            internal float Stamina;
            /// Reach in metres.
            internal float Range;
        }

        /// One crafting ingredient: the shown item name, the amount to craft (quality 1) and the amount each upgrade level costs.
        internal sealed class IngredientLine
        {
            internal readonly string Name;
            internal readonly int Amount;
            internal readonly int PerUpgrade;
            internal IngredientLine(string name, int amount, int perUpgrade) { Name = name; Amount = amount; PerUpgrade = perUpgrade; }
        }

        internal string Family = "";
        internal int Rank;
        /// The shown skill name (e.g. "Clubs", "Whips").
        internal string Skill = "";
        internal float Weight;
        internal bool OneHanded = true;
        internal int MaxQuality = 1;

        internal readonly List<DamageLine> Damage = new List<DamageLine>();
        /// Bleed from the weapon's status effect: damage per second and for how many seconds (0 = no bleed).
        internal float BleedPerSecond;
        internal float BleedSeconds;

        /// The item's attack force (knockback); each attack multiplies it with its ForceMultiplier.
        internal float AttackForce;
        internal AttackLine Attack1 = new AttackLine();
        /// Null when the weapon has no attack 2.
        internal AttackLine Attack2;

        internal float BlockPower, BlockPowerPerLevel;
        internal float ParryBonus;
        internal float DeflectionForce, DeflectionForcePerLevel;

        internal float Durability, DurabilityPerLevel;

        /// The shown crafting station name and the station level needed for quality 1 (empty name = no recipe found).
        internal string Station = "";
        internal int StationLevel;
        internal readonly List<IngredientLine> Ingredients = new List<IngredientLine>();

        public override string ToString() { return Family + " V" + Rank + " stats"; }
    }
}
