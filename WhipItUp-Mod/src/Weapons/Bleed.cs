using Jotunn.Entities;
using Jotunn.Managers;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// Bleed for the flogger / whip ranks (ported from RidingCropMod's SE_RidingCropBleed): a tick of true damage every second for the
    /// duration, credited to the attacker. Bleeds never stack, not even between ranks or families: while a target still bleeds, further
    /// hits neither start a second bleed (CanAdd) nor restart the running one (ResetTime). One effect per rank (its own damage/s).
    /// </summary>
    public class SE_WhipItUpBleed : StatusEffect
    {
        // public on purpose: the game clones status effects when it applies them, public fields are copied along
        public float m_damagePerTick = 10f;
        public float m_tickInterval = 1f;
        public int m_ticksTotal = 5;

        float _tickTimer;
        int _ticksDone;
        Character _attacker;

        bool IsBleeding { get { return _ticksDone < m_ticksTotal; } }

        public void Configure(float damagePerSecond, float seconds)
        {
            m_tickInterval = 1f;
            m_damagePerTick = damagePerSecond * m_tickInterval;
            m_ticksTotal = Mathf.Max(1, Mathf.RoundToInt(seconds / m_tickInterval));
            m_ttl = m_ticksTotal * m_tickInterval + 0.5f * m_tickInterval;   // a bit longer than the last tick
        }

        // without the attacker a kill by bleeding gives the player no credit
        public override void SetAttacker(Character attacker) { base.SetAttacker(attacker); _attacker = attacker; }

        public override void Setup(Character character)
        {
            base.Setup(character);
            _tickTimer = m_tickInterval;
            _ticksDone = 0;
            ModLog.Verbose("Bleed: '" + name + "' started on '" + (character != null ? character.name : "null") + "': " + m_damagePerTick.ToString("0.#") + " x " + m_ticksTotal);
        }

        public override void ResetTime()
        {
            if (IsBleeding) { ModLog.Verbose("Bleed: '" + name + "' hit again while bleeding - ignored"); return; }
            base.ResetTime();
            _ticksDone = 0;
            _tickTimer = m_tickInterval;
        }

        public override bool CanAdd(Character character)
        {
            var seMan = character != null ? character.GetSEMan() : null;
            var active = seMan != null ? seMan.GetStatusEffects() : null;
            SE_WhipItUpBleed running = null;
            if (active != null)
                foreach (var e in active) { var b = e as SE_WhipItUpBleed; if (b != null && b.IsBleeding) { running = b; break; } }
            if (running != null) ModLog.Verbose("Bleed: '" + name + "' not applied to '" + character.name + "': '" + running.name + "' is still bleeding");
            return running == null && base.CanAdd(character);
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (m_character == null || !IsBleeding) return;
            _tickTimer -= dt;
            if (_tickTimer > 0f) return;
            _tickTimer += m_tickInterval;
            _ticksDone++;
            ModLog.Verbose("Bleed: '" + name + "' tick " + _ticksDone + "/" + m_ticksTotal + " on '" + m_character.name + "' for " + m_damagePerTick.ToString("0.#"));
            var hit = new HitData();
            hit.m_point = m_character.GetCenterPoint();
            hit.m_damage.m_damage = m_damagePerTick;
            hit.m_hitType = HitData.HitType.Undefined;
            if (_attacker != null) hit.SetAttacker(_attacker);
            m_character.ApplyDamage(hit, true, false, HitData.DamageModifier.Normal);
        }
    }

    internal static class Bleed
    {
        /// One registered effect per item (unique name = unique hash). Returns null (and logs) if Jotunn refuses it.
        public static SE_WhipItUpBleed Create(string itemName, float damagePerSecond, float seconds)
        {
            var se = ScriptableObject.CreateInstance<SE_WhipItUpBleed>();
            se.name = "SE_" + itemName + "_Bleed";
            se.Configure(damagePerSecond, seconds);
            se.m_name = "Bleeding";
            se.m_tooltip = "Bleeding: " + damagePerSecond.ToString("0.#") + " damage per second for " + seconds.ToString("0.#") + " seconds.";
            if (!ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(se, false)))
            {
                ModLog.Warn("Bleed: Jotunn did not accept '" + se.name + "' - " + itemName + " will not make targets bleed");
                return null;
            }
            ModLog.Verbose("Bleed: '" + se.name + "' registered (" + damagePerSecond.ToString("0.#") + "/s x " + seconds + " s)");
            return se;
        }
    }
}
