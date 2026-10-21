"""Makes WhipItUp-Unity/Assets/WhipItUp/Sounds/sfx_slap.ogg from the user's reference/sounds/slap3_original.ogg (user 2026-10-21:
"if there is any reverb, reduce it"). Needs ffmpeg + numpy (run where both exist; the agent ran it in its cloud workspace).

The original: impact at ~50 ms, body until ~125 ms, then a slow room tail (-10 dB -> -50 dB over ~300 ms) = the reverb.
Processing: hold the hit for 75 ms after the onset, then an exponential fade (tau 35 ms) and a cut at hold + 5 tau (0.30 s total);
the stereo side signal (mostly room) is cut to 60% in the hit and 25% in the tail; normalised to 0.98 peak; Vorbis q7, 32 kHz stereo.
The mod still trims the 50 ms lead-in at runtime (ClipTools.TrimLeadIn).
Usage: python slap_dereverb.py <in.ogg> <out.ogg>
"""
import subprocess
import sys

import numpy as np

SR, ONSET, HOLD, TAU = 32000, 0.050, 0.075, 0.035


def main(src, dst):
    raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", src, "-f", "f32le", "-ac", "2", "-ar", str(SR), "-"], capture_output=True, check=True).stdout
    a = np.frombuffer(raw, np.float32).reshape(-1, 2)
    m, s = (a[:, 0] + a[:, 1]) / 2, (a[:, 0] - a[:, 1]) / 2
    t = np.arange(len(a)) / SR
    hold = ONSET + HOLD
    g = np.where(t < hold, 1.0, np.exp(-(t - hold) / TAU))
    sg = np.where(t < ONSET + 0.02, 0.6, 0.25)
    out = np.stack([m * g + s * g * sg, m * g - s * g * sg], 1)[: int((hold + TAU * 5) * SR)]
    fade = int(0.01 * SR)
    out[-fade:] *= np.linspace(1, 0, fade)[:, None]
    out /= np.abs(out).max() / 0.98
    subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "2", "-i", "-", "-c:a", "libvorbis", "-q:a", "7", dst],
                   input=out.astype(np.float32).tobytes(), check=True)
    print(f"{dst}: {len(out) / SR:.2f} s")
    return len(out) > 0


if __name__ == "__main__":
    sys.exit(0 if main(sys.argv[1], sys.argv[2]) else 1)
