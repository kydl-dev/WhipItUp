using System;
using System.Reflection;
using Kydl.Common;
using UnityEngine;

namespace WhipItUp.Sounds
{
    /// <summary>
    /// Runtime edits of sound data (ported from RidingCropMod, where they fixed a late and quiet slap):
    ///  * TrimLeadIn: cut the quiet lead-in before the impact and normalise to full scale, so the hit is heard the moment it lands.
    ///  * Boost: multiply the samples (AudioSource volume cannot go above 1), peaks above the knee are soft-limited (tanh), no clipping.
    /// Both need the clip's samples (import setting "Decompress On Load", set by WhipItUpAssetSetup.cs). Any problem returns the clip unchanged.
    /// </summary>
    internal static class ClipTools
    {
        const float OnsetFraction = 0.10f;    // impact = first sample at 10% of the peak (-20 dB)
        const float KeepBeforeSeconds = 0.005f;
        const float FadeInSeconds = 0.003f;
        const float LimiterKnee = 0.7f;

        // AudioClip.GetData / SetData have float[] and Span<float> overloads in the game's UnityEngine; with LangVersion latest the
        // compiler binds to the Span one, which the game's Mono mscorlib does not have (CS7069). The float[] overloads are called
        // through reflection instead (looked up once).
        static readonly MethodInfo GetDataArray = typeof(AudioClip).GetMethod("GetData", new[] { typeof(float[]), typeof(int) });
        static readonly MethodInfo SetDataArray = typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });

        static bool CallData(MethodInfo m, AudioClip clip, float[] data)
        {
            if (m == null) { ModLog.Warn("ClipTools: AudioClip float[] Get/SetData not found in this Unity version"); return m != null; }
            return (bool)m.Invoke(clip, new object[] { data, 0 });
        }

        static bool Read(AudioClip clip, out float[] data, string label)
        {
            data = null;
            if (clip == null) { ModLog.Warn("ClipTools: " + label + ": no clip"); return data != null; }
            clip.LoadAudioData();
            data = new float[clip.samples * clip.channels];
            if (clip.samples == 0 || !CallData(GetDataArray, clip, data))
            {
                ModLog.Warn("ClipTools: " + label + ": the samples of '" + clip.name + "' cannot be read (is it 'Decompress On Load' in Unity?) - used unchanged");
                data = null;
            }
            return data != null;
        }

        static AudioClip Make(string name, float[] data, int channels, int frequency)
        {
            var clip = AudioClip.Create(name, data.Length / channels, channels, frequency, false);
            CallData(SetDataArray, clip, data);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;   // runtime-made clip: Unity must not clean it up
            return clip;
        }

        public static AudioClip TrimLeadIn(AudioClip source, string label)
        {
            try
            {
                float[] data;
                if (!Read(source, out data, label)) return source;
                int ch = source.channels;
                float peak = 0f;
                for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
                if (peak < 0.0001f) { ModLog.Warn("ClipTools: " + label + ": '" + source.name + "' is silent - used unchanged"); return source; }
                int firstLoud = 0;
                for (int i = 0; i < data.Length; i++) if (Mathf.Abs(data[i]) >= OnsetFraction * peak) { firstLoud = i / ch; break; }
                int start = Mathf.Max(0, firstLoud - (int)(source.frequency * KeepBeforeSeconds));
                int fade = Mathf.Max(1, (int)(source.frequency * FadeInSeconds));
                float gain = Mathf.Max(1f, 0.98f / peak);
                int frames = data.Length / ch - start;
                var outData = new float[frames * ch];
                for (int f = 0; f < frames; f++)
                {
                    float k = f < fade ? (float)f / fade : 1f;
                    for (int c = 0; c < ch; c++) outData[f * ch + c] = Mathf.Clamp(data[(start + f) * ch + c] * gain * k, -1f, 1f);
                }
                var trimmed = Make(source.name + "_trimmed", outData, ch, source.frequency);
                ModLog.Verbose("ClipTools: " + label + ": '" + source.name + "' peak " + peak.ToString("0.000") + ", x" + gain.ToString("0.00") + ", "
                    + (start * 1000f / source.frequency).ToString("0") + " ms lead-in removed (" + source.length.ToString("0.000") + " s -> " + trimmed.length.ToString("0.000") + " s)");
                return trimmed;
            }
            catch (TargetInvocationException ex) { ModLog.Warn("ClipTools: " + label + ": Get/SetData failed (" + (ex.InnerException ?? ex).Message + ") - used unchanged"); return source; }
            catch (UnityException ex) { ModLog.Warn("ClipTools: " + label + ": Unity refused the clip data (" + ex.Message + ") - used unchanged"); return source; }
            catch (Exception ex) { ModLog.Warn("ClipTools: " + label + ": trim failed (" + ex.Message + ") - used unchanged"); return source; }
        }

        public static AudioClip Boost(AudioClip source, float gain, string label)
        {
            if (source == null || Mathf.Abs(gain - 1f) < 0.001f) { ModLog.Verbose("ClipTools: " + label + ": gain x" + gain.ToString("0.##") + " - nothing to do"); return source; }
            try
            {
                float[] data;
                if (!Read(source, out data, label)) return source;
                float before = 0f, after = 0f;
                int limited = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    float a = Mathf.Abs(data[i]);
                    before = Mathf.Max(before, a);
                    float amp = a * gain;
                    if (amp > LimiterKnee)
                    {
                        limited++;
                        float head = 1f - LimiterKnee;
                        amp = LimiterKnee + head * (float)Math.Tanh((amp - LimiterKnee) / head);
                    }
                    after = Mathf.Max(after, amp);
                    data[i] = data[i] < 0f ? -amp : amp;
                }
                var boosted = Make(source.name + "_gain", data, source.channels, source.frequency);
                ModLog.Verbose("ClipTools: " + label + ": x" + gain.ToString("0.##") + " on '" + source.name + "', peak " + before.ToString("0.000") + " -> " + after.ToString("0.000")
                    + (limited > 0 ? ", soft limiter on " + limited + " of " + data.Length + " samples" : ", no limiting"));
                return boosted;
            }
            catch (TargetInvocationException ex) { ModLog.Warn("ClipTools: " + label + ": Get/SetData failed (" + (ex.InnerException ?? ex).Message + ") - used unchanged"); return source; }
            catch (UnityException ex) { ModLog.Warn("ClipTools: " + label + ": Unity refused the clip data (" + ex.Message + ") - used unchanged"); return source; }
            catch (Exception ex) { ModLog.Warn("ClipTools: " + label + ": gain failed (" + ex.Message + ") - used unchanged"); return source; }
        }
    }
}
