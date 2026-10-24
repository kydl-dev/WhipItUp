using System;
using System.Collections.Generic;
using System.Globalization;

namespace WhipItUp.Weapons
{
    /// <summary>
    /// The keyed whip lash of the custom whip clips (T-013, D-084): per clip and frame (30 fps) the lash nodes in the whip's model space
    /// (Unity space of Whip.fbx, metres before the item scale), made in Blender (WhipItUp-Blender/scripts/anim_lash_frames.py) and
    /// embedded as whip_lash_frames.txt. Pure logic (no Unity / Valheim types): parsing and sampling are unit-tested.
    /// Logging goes through the injected log (null = silent).
    /// </summary>
    internal sealed class WhipLashFrames
    {
        internal sealed class Clip
        {
            internal string Name;
            internal int Frames;
            internal int Nodes;
            /// Frame the lash is fully extended: the crack sound plays there (D-086); -1 = none.
            internal int Crack = -1;
            /// Crack as normalized clip time (0..1), -1 = none.
            internal float CrackTime { get { return Crack >= 0 && Frames > 1 ? (float)Crack / (Frames - 1) : -1f; } }
            /// [frame][node * 3 + axis]
            internal float[][] Data;
        }

        readonly Dictionary<string, Clip> _clips = new Dictionary<string, Clip>(StringComparer.Ordinal);
        internal int Count { get { return _clips.Count; } }
        internal IEnumerable<string> Names { get { return _clips.Keys; } }

        /// Parses the text. Bad lines are skipped and logged; a clip with missing frames is dropped. Expected outcome: the number of clips read.
        internal static WhipLashFrames Parse(string text, Action<string> log)
        {
            var f = new WhipLashFrames();
            if (string.IsNullOrEmpty(text)) { if (log != null) log("WhipLashFrames: no text - no keyed lash"); return f; }
            Clip cur = null;
            var rows = new List<float[]>();
            int lineNo = 0, skipped = 0;
            Action close = () =>
            {
                if (cur == null) return;
                if (rows.Count != cur.Frames) { if (log != null) log("WhipLashFrames: clip " + cur.Name + " has " + rows.Count + " frame line(s), expected " + cur.Frames + " - dropped"); }
                else { cur.Data = rows.ToArray(); f._clips[cur.Name] = cur; if (log != null) log("WhipLashFrames: clip " + cur.Name + ": " + cur.Frames + " frames x " + cur.Nodes + " nodes, crack frame " + cur.Crack); }
                rows = new List<float[]>();
                cur = null;
            };
            foreach (var raw in text.Split('\n'))
            {
                lineNo++;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (line.StartsWith("clip ", StringComparison.Ordinal))
                {
                    close();
                    var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int frames, nodes;
                    if (p.Length != 4 || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out frames) || !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out nodes) || frames < 1 || nodes < 2)
                    { skipped++; if (log != null) log("WhipLashFrames: line " + lineNo + " is not 'clip <name> <frames> <nodes>': " + line); continue; }
                    cur = new Clip { Name = p[1], Frames = frames, Nodes = nodes };
                    continue;
                }
                if (line.StartsWith("crack ", StringComparison.Ordinal) && cur != null)
                {
                    int crack;
                    if (int.TryParse(line.Substring(6).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out crack) && crack >= 0 && crack < cur.Frames) cur.Crack = crack;
                    else { skipped++; if (log != null) log("WhipLashFrames: line " + lineNo + " of " + cur.Name + " is not 'crack <frame 0.." + (cur.Frames - 1) + ">': " + line); }
                    continue;
                }
                if (cur == null) { skipped++; continue; }
                var v = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (v.Length != cur.Nodes * 3) { skipped++; if (log != null) log("WhipLashFrames: line " + lineNo + " of " + cur.Name + " has " + v.Length + " values, expected " + (cur.Nodes * 3)); continue; }
                var row = new float[v.Length];
                bool ok = true;
                for (int i = 0; i < v.Length && ok; i++) ok = float.TryParse(v[i], NumberStyles.Float, CultureInfo.InvariantCulture, out row[i]);
                if (!ok) { skipped++; if (log != null) log("WhipLashFrames: line " + lineNo + " of " + cur.Name + " has a value that is not a number"); continue; }
                rows.Add(row);
            }
            close();
            if (log != null) log("WhipLashFrames: " + f._clips.Count + " clip(s) read, " + skipped + " line(s) skipped");
            return f;
        }

        internal Clip Get(string name)
        {
            Clip c;
            return name != null && _clips.TryGetValue(name, out c) ? c : null;
        }

        /// Node position at normalized clip time t (0..1, clamped), linear between frames. Expected outcome: false when the clip / node is unknown.
        internal bool Sample(string clip, float t, int node, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            var c = Get(clip);
            if (c == null || node < 0 || node >= c.Nodes) return false;
            if (float.IsNaN(t)) t = 0f;
            float fr = Math.Max(0f, Math.Min(1f, t)) * (c.Frames - 1);
            int a = (int)Math.Floor(fr);
            int b = Math.Min(c.Frames - 1, a + 1);
            float w = fr - a;
            var ra = c.Data[a];
            var rb = c.Data[b];
            int k = node * 3;
            x = ra[k] + (rb[k] - ra[k]) * w;
            y = ra[k + 1] + (rb[k + 1] - ra[k + 1]) * w;
            z = ra[k + 2] + (rb[k + 2] - ra[k + 2]) * w;
            return true;
        }
    }
}
