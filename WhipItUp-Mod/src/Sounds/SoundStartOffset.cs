using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Sounds
{
    /// <summary>
    /// On a sound prefab: once the game starts playing the clip, jump the playback position to StartSeconds (the impact), so a sound
    /// with a lead-in lands on the hit without editing its data (the flogger slap, user: "don't cut or anything, just sync it").
    /// Checks a few frames because the game may start the AudioSource after Awake (ZSFX delay / play logic).
    /// </summary>
    internal sealed class SoundStartOffset : MonoBehaviour
    {
        public float StartSeconds;
        AudioSource[] _sources;
        int _frames;

        void OnEnable() { _sources = GetComponentsInChildren<AudioSource>(true); _frames = 0; }

        void Update()
        {
            if (StartSeconds <= 0f || _sources == null || ++_frames > 10) { enabled = false; return; }
            foreach (var s in _sources)
            {
                if (s == null || s.clip == null || !s.isPlaying || s.time >= StartSeconds) continue;
                s.time = Mathf.Min(StartSeconds, s.clip.length - 0.01f);
                ModLog.Verbose("Sound: '" + s.clip.name + "' jumped to " + s.time.ToString("0.000") + " s (frame " + _frames + ")");
                enabled = false;
            }
        }
    }
}
