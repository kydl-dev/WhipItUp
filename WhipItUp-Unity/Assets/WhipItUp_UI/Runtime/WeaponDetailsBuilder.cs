using System;
using System.Collections.Generic;
using System.Globalization;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Turns the numbers of one weapon (WeaponStats) into the labelled blocks of the Details tab: General, Attack 1, Attack 2 (when the weapon
    /// has one), Defence, Durability, Crafting. Values that change with quality show quality 1 beside the max quality (D-048 Q4). Pure logic, no
    /// Unity / Valheim types, unit-tested (WeaponDetailsBuilderTests); the Unity preview uses it with sample stats. Numbers are written with
    /// the invariant culture and one decimal at most, whatever the player's system language.
    /// </summary>
    internal static class WeaponDetailsBuilder
    {
        const string NumberFormat = "0.#";
        const float SameWithin = 0.05f;     // values closer than this look equal at the shown precision
        const string Whip = "Whip";

        /// Fills `info` (groups + column headers) from `s`; `text` turns a text key into the shown text, `log` gets the debug lines (null = silent).
        /// Expected outcome: 5 groups, 6 when the weapon has attack 2; no stats = no groups. Returns that check.
        internal static bool Fill(WeaponInfo info, WeaponStats s, Func<string, string> text, Action<string> log = null)
        {
            info.Groups.Clear();
            info.QualityHeader = "";
            info.MaxQualityHeader = "";
            if (s == null)
            {
                Log(log, "WeaponDetailsBuilder: " + info + " has no stats, no detail groups");
                return info.Groups.Count == 0;
            }

            int q = Math.Max(1, s.MaxQuality);
            info.Groups.Add(General(s, text));
            info.Groups.Add(Attack(text("whipitup_group_attack1"), s, s.Attack1, q, null, text));
            if (s.Attack2 != null)
                info.Groups.Add(Attack(text("whipitup_group_attack2"), s, s.Attack2, q, text(s.Family == Whip ? "whipitup_attack2_grab" : "whipitup_attack2_backhand"), text));
            info.Groups.Add(Defence(s, q, text));
            info.Groups.Add(Durability(s, q, text));
            info.Groups.Add(Crafting(s, text));
            info.QualityHeader = text("whipitup_details_quality") + " 1";
            info.MaxQualityHeader = q > 1 ? text("whipitup_details_quality") + " " + q : "";

            int rows = 0;
            var parts = new List<string>();
            foreach (var g in info.Groups) { rows += g.Rows.Count; parts.Add(g.Title + " " + g.Rows.Count); }
            Log(log, "WeaponDetailsBuilder: " + info + ": " + info.Groups.Count + " group(s) [" + string.Join(", ", parts.ToArray()) + "], " + rows + " row(s), max quality " + q
                + ", max column " + (info.HasMaxColumn ? "shown" : "not needed"));
            return info.Groups.Count == (s.Attack2 != null ? 6 : 5);
        }

        static DetailGroup General(WeaponStats s, Func<string, string> text)
        {
            var g = new DetailGroup(text("whipitup_group_general"));
            g.Add(text("whipitup_row_family"), text("whipitup_family_" + s.Family.ToLowerInvariant()));
            g.Add(text("whipitup_row_rank"), "V" + s.Rank.ToString(CultureInfo.InvariantCulture));
            g.Add(text("whipitup_row_skill"), s.Skill);
            g.Add(text("whipitup_row_weight"), Num(s.Weight) + " kg");
            g.Add(text("whipitup_row_hands"), text(s.OneHanded ? "whipitup_hands_one" : "whipitup_hands_two"));
            return g;
        }

        /// One attack: damage per type (item damage x the attack's multiplier), bleed, knockback, stagger, stamina, reach.
        static DetailGroup Attack(string title, WeaponStats s, WeaponStats.AttackLine a, int q, string effect, Func<string, string> text)
        {
            var g = new DetailGroup(title);
            if (!string.IsNullOrEmpty(effect)) g.Add(text("whipitup_row_effect"), effect);

            float total1 = 0f, totalQ = 0f;
            foreach (var d in s.Damage)
            {
                float v1 = d.Base * a.DamageMultiplier;
                float vq = At(d.Base, d.PerLevel, q) * a.DamageMultiplier;
                if (v1 <= 0f && vq <= 0f) continue;
                g.Add(text("whipitup_dmg_" + d.Type), Num(v1), MaxText(v1, vq, q));
                total1 += v1;
                totalQ += vq;
            }
            if (s.BleedPerSecond > 0f && s.BleedSeconds > 0f)
                g.Add(text("whipitup_row_bleed"), Format(text("whipitup_bleed_value"), Num(s.BleedPerSecond), Num(s.BleedSeconds)));
            g.Add(text("whipitup_row_knockback"), Num(s.AttackForce * a.ForceMultiplier));
            g.Add(text("whipitup_row_stagger"), Num(total1 * a.StaggerMultiplier), MaxText(total1 * a.StaggerMultiplier, totalQ * a.StaggerMultiplier, q));
            g.Add(text("whipitup_row_stamina"), Num(a.Stamina));
            g.Add(text("whipitup_row_reach"), Num(a.Range) + " m");
            return g;
        }

        static DetailGroup Defence(WeaponStats s, int q, Func<string, string> text)
        {
            var g = new DetailGroup(text("whipitup_group_defence"));
            g.Add(text("whipitup_row_block"), Num(s.BlockPower), MaxText(s.BlockPower, At(s.BlockPower, s.BlockPowerPerLevel, q), q));
            g.Add(text("whipitup_row_parry"), "x" + Num(s.ParryBonus));
            g.Add(text("whipitup_row_deflection"), Num(s.DeflectionForce), MaxText(s.DeflectionForce, At(s.DeflectionForce, s.DeflectionForcePerLevel, q), q));
            return g;
        }

        static DetailGroup Durability(WeaponStats s, int q, Func<string, string> text)
        {
            var g = new DetailGroup(text("whipitup_group_durability"));
            g.Add(text("whipitup_row_durability"), Num(s.Durability), MaxText(s.Durability, At(s.Durability, s.DurabilityPerLevel, q), q));
            g.Add(text("whipitup_row_maxquality"), q.ToString(CultureInfo.InvariantCulture));
            return g;
        }

        static DetailGroup Crafting(WeaponStats s, Func<string, string> text)
        {
            var g = new DetailGroup(text("whipitup_group_crafting"));
            if (string.IsNullOrEmpty(s.Station) && s.Ingredients.Count == 0)
            {
                g.Add(text("whipitup_row_station"), text("whipitup_recipe_missing"));
                return g;
            }
            g.Add(text("whipitup_row_station"), Format(text("whipitup_station_value"), s.Station, s.StationLevel.ToString(CultureInfo.InvariantCulture)));
            foreach (var i in s.Ingredients)
                g.Add(i.Name, Format(text("whipitup_ingredient_value"), i.Amount.ToString(CultureInfo.InvariantCulture), i.PerUpgrade.ToString(CultureInfo.InvariantCulture)));
            return g;
        }

        /// The value at quality `level` (1 = base).
        static float At(float baseValue, float perLevel, int level) { return baseValue + perLevel * (level - 1); }

        /// The max-quality column text: empty when there is no higher quality or the value does not change (at the shown precision).
        static string MaxText(float atOne, float atMax, int q)
        {
            return q > 1 && Math.Abs(atMax - atOne) >= SameWithin ? Num(atMax) : "";
        }

        static string Num(float v) { return v.ToString(NumberFormat, CultureInfo.InvariantCulture); }

        /// A text template with {0} / {1} placeholders; a template that is missing (the key came back) is used as it is.
        static string Format(string template, string a, string b)
        {
            return string.Format(CultureInfo.InvariantCulture, template ?? "", a, b);
        }

        static void Log(Action<string> log, string line) { if (log != null) log(line); }
    }
}
