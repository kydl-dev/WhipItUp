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
    internal enum HitSound { Slap, FloggerSlap, WhipSlap }

    internal static class WeaponSounds
    {
        public const string WhooshAsset = "sfx_whoosh";
        public const string SlapAsset = "sfx_slap";
        /// The flogger's hit (user 2026-10-21): played exactly as the file is ("don't cut or anything, just sync it"): the sample data is
        /// not changed; playback starts at the impact (SoundStartOffset) so the hit lands on time.
        public const string FloggerSlapAsset = "sfx_flogger_slap";
        /// The whip's hit (user 2026-10-21): handled like the flogger slap (file as it is, playback synced to the impact).
        public const string WhipSlapAsset = "sfx_whip_slap";
        static AudioClip _whooshClip, _slapClip, _floggerClip, _whipClip;
        static bool _prepared;
        public static GameObject WhooshPrefab { get; private set; }
        public static GameObject SlapPrefab { get; private set; }
        public static GameObject FloggerSlapPrefab { get; private set; }
        public static GameObject WhipSlapPrefab { get; private set; }
        public static bool IsHitPrefab(GameObject p) { return p != null && (p == SlapPrefab || p == FloggerSlapPrefab || p == WhipSlapPrefab); }

        /// Expected outcome: both clips found in the bundle (returns that check).
        public static bool Load(AssetBundle bundle)
        {
            if (bundle == null) { ModLog.Warn("WeaponSounds: no bundle - weapons keep the vanilla sounds"); return _whooshClip != null && _slapClip != null; }
            _whooshClip = bundle.LoadAsset<AudioClip>(WhooshAsset);
            _slapClip = bundle.LoadAsset<AudioClip>(SlapAsset);
            _floggerClip = bundle.LoadAsset<AudioClip>(FloggerSlapAsset);
            _whipClip = bundle.LoadAsset<AudioClip>(WhipSlapAsset);
            if (_whipClip == null) ModLog.Warn("WeaponSounds: '" + WhipSlapAsset + "' is not in the bundle - the whip uses the paddle slap");
            if (_floggerClip == null) ModLog.Warn("WeaponSounds: '" + FloggerSlapAsset + "' is not in the bundle - the flogger uses the paddle slap");
            if (_whooshClip == null) ModLog.Warn("WeaponSounds: '" + WhooshAsset + "' is not in the bundle - the swing keeps the vanilla sound");
            if (_slapClip == null) ModLog.Warn("WeaponSounds: '" + SlapAsset + "' is not in the bundle - the hit keeps the vanilla sound");
            ModLog.Verbose("WeaponSounds: clips loaded - whoosh " + Describe(_whooshClip) + ", slap " + Describe(_slapClip) + ", flogger slap " + Describe(_floggerClip));
            return _whooshClip != null && _slapClip != null;
        }

        static string Describe(AudioClip c) { return c == null ? "none" : "'" + c.name + "' " + c.length.ToString("0.00") + " s, " + c.frequency + " Hz, " + c.channels + " ch, " + c.loadType; }

        /// Swaps the weapon's swing and hit sounds. Call after the vanilla prefabs exist. Expected outcome: our prefabs are in the lists.
        public static bool Apply(ItemDrop.ItemData.SharedData shared, string itemName, HitSound hit)
        {
            Prepare(shared);
            var hitPrefab = hit == HitSound.FloggerSlap && FloggerSlapPrefab != null ? FloggerSlapPrefab
                          : hit == HitSound.WhipSlap && WhipSlapPrefab != null ? WhipSlapPrefab : SlapPrefab;
            if (WhooshPrefab != null)
            {
                bool startHad = HasSound(shared.m_startEffect), triggerHad = HasSound(shared.m_triggerEffect);
                shared.m_startEffect = WithoutSounds(shared.m_startEffect);
                shared.m_triggerEffect = WithoutSounds(shared.m_triggerEffect);
                if (startHad) shared.m_startEffect = With(shared.m_startEffect, WhooshPrefab);
                if (triggerHad || !startHad) shared.m_triggerEffect = With(shared.m_triggerEffect, WhooshPrefab);
            }
            if (hitPrefab != null)
            {
                shared.m_hitEffect = With(WithoutSounds(shared.m_hitEffect), hitPrefab);
                shared.m_hitTerrainEffect = With(WithoutSounds(shared.m_hitTerrainEffect), hitPrefab);
            }
            bool ok = (WhooshPrefab == null || Contains(shared.m_startEffect, WhooshPrefab) || Contains(shared.m_triggerEffect, WhooshPrefab))
                      && (hitPrefab == null || Contains(shared.m_hitEffect, hitPrefab));
            ModLog.Verbose("WeaponSounds: " + itemName + " sounds set (whoosh " + (WhooshPrefab != null) + ", hit " + (hitPrefab != null ? hitPrefab.name : "vanilla") + ", ok " + ok + ")");
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
            if (_whooshClip != null) WhooshPrefab = Create("sfx_whipitup_whoosh", template, false);
            if (_slapClip != null) SlapPrefab = Create("sfx_whipitup_slap", template, true);
            if (_floggerClip != null)
            {
                FloggerSlapPrefab = Create("sfx_whipitup_flogger_slap", template, true);
                if (FloggerSlapPrefab != null) FloggerSlapPrefab.AddComponent<SoundStartOffset>();
            }
            if (_whipClip != null)
            {
                WhipSlapPrefab = Create("sfx_whipitup_whip_slap", template, true);
                if (WhipSlapPrefab != null) WhipSlapPrefab.AddComponent<SoundStartOffset>();
            }
            Rebuild();
            // live: volume / gain / trim changes in KydlTools apply to the next sound played (no restart)
            foreach (var t in new[] { ModSettings.WhooshVolume, ModSettings.SlapVolume, ModSettings.WhooshGain, ModSettings.SlapGain, ModSettings.FloggerSlapVolume, ModSettings.WhipSlapVolume })
                t.SettingChanged += (o, e) => Rebuild();
            ModSettings.SlapTrim.SettingChanged += (o, e) => Rebuild();
            ModSettings.FloggerSlapSync.SettingChanged += (o, e) => Rebuild();
            ModSettings.WhipSlapSync.SettingChanged += (o, e) => Rebuild();
        }

        static AudioClip _whooshMade, _slapMade;     // the processed copies in use (destroyed when replaced)

        /// (Re)makes the processed clips from the original bundle clips with the current Tunables and puts them + the volumes on the
        /// sound prefabs. Expected outcome: every existing sound prefab plays the new clip (returns that check).
        public static bool Rebuild()
        {
            bool ok = true;
            if (WhooshPrefab != null)
            {
                var clip = ClipTools.Boost(_whooshClip, ModSettings.WhooshGain.Value, "whoosh");
                ok &= SetClip(WhooshPrefab, clip, ModSettings.WhooshVolume.Value);
                Replace(ref _whooshMade, clip, _whooshClip);
            }
            if (SlapPrefab != null)
            {
                var trimmed = ModSettings.SlapTrim.Value ? ClipTools.TrimLeadIn(_slapClip, "slap") : _slapClip;
                var clip = ClipTools.Boost(trimmed, ModSettings.SlapGain.Value, "slap");
                if (trimmed != _slapClip && clip != trimmed) Object.Destroy(trimmed);    // intermediate copy no longer needed
                ok &= SetClip(SlapPrefab, clip, ModSettings.SlapVolume.Value);
                Replace(ref _slapMade, clip, _slapClip);
            }
            if (FloggerSlapPrefab != null)
            {
                // the file as it is (no trim, no gain); only the playback start moves to the impact
                ok &= SetClip(FloggerSlapPrefab, _floggerClip, ModSettings.FloggerSlapVolume.Value);
                float start = ModSettings.FloggerSlapSync.Value ? ClipTools.OnsetSeconds(_floggerClip, "flogger slap") : 0f;
                foreach (var o in FloggerSlapPrefab.GetComponentsInChildren<SoundStartOffset>(true)) o.StartSeconds = start;
                ModLog.Verbose("WeaponSounds: flogger slap plays from " + start.ToString("0.000") + " s (sync " + ModSettings.FloggerSlapSync.Value + ")");
            }
            ok &= AsIs(WhipSlapPrefab, _whipClip, ModSettings.WhipSlapVolume.Value, ModSettings.WhipSlapSync.Value, "whip slap");
            ModLog.Verbose("WeaponSounds: clips (re)built - whoosh gain " + ModSettings.WhooshGain.Value + " vol " + ModSettings.WhooshVolume.Value
                + ", slap trim " + ModSettings.SlapTrim.Value + " gain " + ModSettings.SlapGain.Value + " vol " + ModSettings.SlapVolume.Value + ", ok " + ok);
            return ok;
        }

        /// A hit sound played as the file is; sync = playback starts at the impact. Expected outcome: clip + start set (or no prefab).
        static bool AsIs(GameObject prefab, AudioClip clip, float volume, bool sync, string label)
        {
            if (prefab == null) return clip == null;
            bool ok = SetClip(prefab, clip, volume);
            float start = sync ? ClipTools.OnsetSeconds(clip, label) : 0f;
            foreach (var o in prefab.GetComponentsInChildren<SoundStartOffset>(true)) o.StartSeconds = start;
            ModLog.Verbose("WeaponSounds: " + label + " plays from " + start.ToString("0.000") + " s (sync " + sync + ")");
            return ok;
        }

        static void Replace(ref AudioClip made, AudioClip next, AudioClip original)
        {
            if (made != null && made != next && made != original) Object.Destroy(made);
            made = next != original ? next : null;
        }

        static bool SetClip(GameObject prefab, AudioClip clip, float volume)
        {
            int n = 0;
            foreach (var sfx in prefab.GetComponentsInChildren<ZSFX>(true)) { sfx.m_audioClips = new[] { clip }; sfx.m_minVol = volume; sfx.m_maxVol = volume; n++; }
            foreach (var src in prefab.GetComponentsInChildren<AudioSource>(true)) src.clip = clip;
            ModLog.Verbose("WeaponSounds: " + prefab.name + " <- '" + (clip != null ? clip.name : "none") + "' volume " + volume.ToString("0.##") + " (" + n + " ZSFX)");
            return n > 0 && clip != null;
        }

        static GameObject Create(string name, GameObject template, bool critical)
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(name, template);
            if (prefab == null) { ModLog.Warn("WeaponSounds: could not clone '" + template.name + "' as " + name); return null; }
            foreach (var sfx in prefab.GetComponentsInChildren<ZSFX>(true))
            {
                sfx.m_hash = name.GetStableHashCode();      // own hash: the clone inherited the template's
                if (!critical) continue;
                sfx.m_maxConcurrentSources = 0;              // never refused (duplicates: SlapOncePerSwingPatch)
                sfx.m_minDelay = 0f; sfx.m_maxDelay = 0f; sfx.m_fadeInDuration = 0f; sfx.m_fadeOutOnAwake = false;
            }
            foreach (var src in prefab.GetComponentsInChildren<AudioSource>(true))
            {
                if (!critical) continue;
                src.priority = 0;          // dropped last when Unity runs out of voices
                src.spatialBlend = 0f;     // camera sits metres behind the player: a 3D slap at the hit point is quieted by that distance
                src.loop = false;
            }
            PrefabManager.Instance.AddPrefab(prefab);
            ModLog.Verbose("WeaponSounds: prefab " + name + " made from '" + template.name + "' (critical " + critical + ")");
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
