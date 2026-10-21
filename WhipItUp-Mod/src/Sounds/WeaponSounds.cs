using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Sounds
{
    /// <summary>
    /// The mod's weapon sounds (user 2026-10-21): swing = the RidingCrop whoosh (bundle asset sfx_whoosh), hit = the user's slap (sfx_slap).
    /// A sound is a cloned vanilla sound prefab (keeps the game's mixer, 3D settings and networking) with our clip; it is registered with
    /// Jotunn so other players hear it too. Lessons kept from the crop: own sound hash per prefab (AudioMan limits same-hash sounds),
    /// the slap is never refused, starts at once and is not positional; duplicate slaps of one swing are dropped (SlapOncePerSwingPatch).
    /// </summary>
    internal static class WeaponSounds
    {
        public const string WhooshAsset = "sfx_whoosh";
        public const string SlapAsset = "sfx_slap";
        static AudioClip _whooshClip, _slapClip;
        static bool _prepared;
        public static GameObject WhooshPrefab { get; private set; }
        public static GameObject SlapPrefab { get; private set; }

        /// Expected outcome: both clips found in the bundle (returns that check).
        public static bool Load(AssetBundle bundle)
        {
            if (bundle == null) { ModLog.Warn("WeaponSounds: no bundle - weapons keep the vanilla sounds"); return _whooshClip != null && _slapClip != null; }
            _whooshClip = bundle.LoadAsset<AudioClip>(WhooshAsset);
            _slapClip = bundle.LoadAsset<AudioClip>(SlapAsset);
            if (_whooshClip == null) ModLog.Warn("WeaponSounds: '" + WhooshAsset + "' is not in the bundle - the swing keeps the vanilla sound");
            if (_slapClip == null) ModLog.Warn("WeaponSounds: '" + SlapAsset + "' is not in the bundle - the hit keeps the vanilla sound");
            ModLog.Verbose("WeaponSounds: clips loaded - whoosh " + Describe(_whooshClip) + ", slap " + Describe(_slapClip));
            return _whooshClip != null && _slapClip != null;
        }

        static string Describe(AudioClip c) { return c == null ? "none" : "'" + c.name + "' " + c.length.ToString("0.00") + " s, " + c.frequency + " Hz, " + c.channels + " ch, " + c.loadType; }

        /// Swaps the weapon's swing and hit sounds. Call after the vanilla prefabs exist. Expected outcome: our prefabs are in the lists.
        public static bool Apply(ItemDrop.ItemData.SharedData shared, string itemName)
        {
            Prepare(shared);
            if (WhooshPrefab != null)
            {
                bool startHad = HasSound(shared.m_startEffect), triggerHad = HasSound(shared.m_triggerEffect);
                shared.m_startEffect = WithoutSounds(shared.m_startEffect);
                shared.m_triggerEffect = WithoutSounds(shared.m_triggerEffect);
                if (startHad) shared.m_startEffect = With(shared.m_startEffect, WhooshPrefab);
                if (triggerHad || !startHad) shared.m_triggerEffect = With(shared.m_triggerEffect, WhooshPrefab);
            }
            if (SlapPrefab != null)
            {
                shared.m_hitEffect = With(WithoutSounds(shared.m_hitEffect), SlapPrefab);
                shared.m_hitTerrainEffect = With(WithoutSounds(shared.m_hitTerrainEffect), SlapPrefab);
            }
            bool ok = (WhooshPrefab == null || Contains(shared.m_startEffect, WhooshPrefab) || Contains(shared.m_triggerEffect, WhooshPrefab))
                      && (SlapPrefab == null || Contains(shared.m_hitEffect, SlapPrefab));
            ModLog.Verbose("WeaponSounds: " + itemName + " sounds set (whoosh " + (WhooshPrefab != null) + ", slap " + (SlapPrefab != null) + ", ok " + ok + ")");
            return ok;
        }

        static void Prepare(ItemDrop.ItemData.SharedData shared)
        {
            if (_prepared) return;
            _prepared = true;
            Log("start", shared.m_startEffect); Log("trigger", shared.m_triggerEffect); Log("hit", shared.m_hitEffect); Log("hitTerrain", shared.m_hitTerrainEffect);
            var template = FindTemplate(shared);
            if (template == null) { ModLog.Warn("WeaponSounds: no vanilla sound prefab to copy - weapons keep the vanilla sounds"); return; }
            ModLog.Verbose("WeaponSounds: template sound prefab '" + template.name + "'");
            if (_whooshClip != null) WhooshPrefab = Create("sfx_whipitup_whoosh", template, ClipTools.Boost(_whooshClip, ModSettings.SoundGain.Value, "whoosh"), ModSettings.WhooshVolume.Value, false);
            if (_slapClip != null)
            {
                var clip = ModSettings.SlapTrim.Value ? ClipTools.TrimLeadIn(_slapClip, "slap") : _slapClip;
                var boosted = ClipTools.Boost(clip, ModSettings.SoundGain.Value, "slap");
                if (clip != _slapClip && boosted != clip) Object.Destroy(clip);     // intermediate copy no longer needed
                SlapPrefab = Create("sfx_whipitup_slap", template, boosted, ModSettings.SlapVolume.Value, true);
            }
        }

        static GameObject Create(string name, GameObject template, AudioClip clip, float volume, bool critical)
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(name, template);
            if (prefab == null) { ModLog.Warn("WeaponSounds: could not clone '" + template.name + "' as " + name); return null; }
            foreach (var sfx in prefab.GetComponentsInChildren<ZSFX>(true))
            {
                sfx.m_audioClips = new[] { clip };
                sfx.m_minVol = volume;
                sfx.m_maxVol = volume;
                sfx.m_hash = name.GetStableHashCode();      // own hash: the clone inherited the template's
                if (!critical) continue;
                sfx.m_maxConcurrentSources = 0;              // never refused (duplicates: SlapOncePerSwingPatch)
                sfx.m_minDelay = 0f; sfx.m_maxDelay = 0f; sfx.m_fadeInDuration = 0f; sfx.m_fadeOutOnAwake = false;
            }
            foreach (var src in prefab.GetComponentsInChildren<AudioSource>(true))
            {
                src.clip = clip;
                if (!critical) continue;
                src.priority = 0;          // dropped last when Unity runs out of voices
                src.spatialBlend = 0f;     // camera sits metres behind the player: a 3D slap at the hit point is quieted by that distance
                src.loop = false;
            }
            PrefabManager.Instance.AddPrefab(prefab);
            ModLog.Verbose("WeaponSounds: prefab " + name + " made (clip '" + clip.name + "', " + clip.length.ToString("0.00") + " s, volume " + volume.ToString("0.##") + ", critical " + critical + ")");
            return prefab;
        }

        static GameObject FindTemplate(ItemDrop.ItemData.SharedData shared)
        {
            var found = new List<GameObject>();
            foreach (var list in new[] { shared.m_triggerEffect, shared.m_startEffect, shared.m_hitEffect, shared.m_hitTerrainEffect })
                if (list != null && list.m_effectPrefabs != null)
                    found.AddRange(list.m_effectPrefabs.Where(e => e != null && IsSound(e.m_prefab)).Select(e => e.m_prefab));
            var best = found.FirstOrDefault(p => p.name.StartsWith("sfx_", System.StringComparison.OrdinalIgnoreCase));
            if (best != null) return best;
            foreach (var n in new[] { "sfx_club_hit", "sfx_sword_hit", "sfx_club_swing" })
            {
                var p = PrefabManager.Instance.GetPrefab(n);
                if (IsSound(p)) return p;
            }
            return found.FirstOrDefault();
        }

        static bool IsSound(GameObject p) { return p != null && p.GetComponent<ZSFX>() != null; }
        static bool HasSound(EffectList l) { return l != null && l.m_effectPrefabs != null && l.m_effectPrefabs.Any(e => e != null && IsSound(e.m_prefab)); }
        static bool Contains(EffectList l, GameObject p) { return l != null && l.m_effectPrefabs != null && l.m_effectPrefabs.Any(e => e != null && e.m_prefab == p); }

        static EffectList WithoutSounds(EffectList l)
        {
            var kept = l == null || l.m_effectPrefabs == null ? new List<EffectList.EffectData>() : l.m_effectPrefabs.Where(e => e != null && !IsSound(e.m_prefab)).ToList();
            return new EffectList { m_effectPrefabs = kept.ToArray() };
        }

        static EffectList With(EffectList l, GameObject p)
        {
            var e = l == null || l.m_effectPrefabs == null ? new List<EffectList.EffectData>() : l.m_effectPrefabs.Where(x => x != null).ToList();
            e.Add(new EffectList.EffectData { m_prefab = p, m_enabled = true, m_variant = -1 });
            return new EffectList { m_effectPrefabs = e.ToArray() };
        }

        static void Log(string label, EffectList l)
        {
            var names = l == null || l.m_effectPrefabs == null ? "(none)" : string.Join(", ", l.m_effectPrefabs.Where(e => e != null).Select(e => (e.m_prefab != null ? e.m_prefab.name : "null") + (IsSound(e.m_prefab) ? "[sound]" : "")));
            ModLog.Verbose("WeaponSounds: vanilla " + label + " effects: " + names);
        }
    }
}
