using System.Collections.Generic;
using System.IO;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit
{
    /// <summary>The keyed whip lash data of the custom whip clips (T-013): parsing, sampling, and the real embedded file.</summary>
    [TestFixture]
    public class WhipLashFramesTests
    {
        const string Small = "# comment\nclip A 2 2\n0 0 0 1 0 0\n0 0 0 3 0 0\nclip B 1 2\n0 0 0 0 1 0\n";

        [Test]
        public void Parse_ReadsClipsFramesAndNodes()
        {
            var log = new List<string>();
            var f = WhipLashFrames.Parse(Small, log.Add);
            Assert.That(f.Count, Is.EqualTo(2));
            Assert.That(f.Get("A").Frames, Is.EqualTo(2));
            Assert.That(f.Get("A").Nodes, Is.EqualTo(2));
            Assert.That(log.Exists(l => l.Contains("2 clip(s) read")), Is.True);
        }

        [Test]
        public void Sample_InterpolatesBetweenFrames_AndClampsTime()
        {
            var f = WhipLashFrames.Parse(Small, null);
            Assert.That(f.Sample("A", 0.5f, 1, out float x, out _, out _), Is.True);
            Assert.That(x, Is.EqualTo(2f).Within(1e-5));
            f.Sample("A", 7f, 1, out x, out _, out _);
            Assert.That(x, Is.EqualTo(3f).Within(1e-5));
            f.Sample("A", -1f, 1, out x, out _, out _);
            Assert.That(x, Is.EqualTo(1f).Within(1e-5));
        }

        [Test]
        public void Sample_UnknownClipOrNode_IsFalse()
        {
            var f = WhipLashFrames.Parse(Small, null);
            Assert.That(f.Sample("X", 0f, 0, out _, out _, out _), Is.False);
            Assert.That(f.Sample("A", 0f, 5, out _, out _, out _), Is.False);
        }

        [Test]
        public void Parse_ClipWithMissingFrames_IsDropped_AndBadLinesAreLogged()
        {
            var log = new List<string>();
            var f = WhipLashFrames.Parse("clip A 3 2\n0 0 0 0 0 0\n0 x 0 0 0 0\nclip B 1 2\n1 2\n", log.Add);
            Assert.That(f.Count, Is.EqualTo(0));
            Assert.That(log.Exists(l => l.Contains("not a number")), Is.True, string.Join(" | ", log));
            Assert.That(log.Exists(l => l.Contains("expected 3")), Is.True);
        }

        [Test]
        public void Parse_ReadsTheCrackFrame_AndItsNormalizedTime()
        {
            var f = WhipLashFrames.Parse("clip A 3 2\ncrack 2\n0 0 0 0 0 0\n0 0 0 0 0 0\n0 0 0 0 0 0\nclip B 2 2\n0 0 0 0 0 0\n0 0 0 0 0 0\n", null);
            Assert.That(f.Get("A").Crack, Is.EqualTo(2));
            Assert.That(f.Get("A").CrackTime, Is.EqualTo(1f).Within(1e-5));
            Assert.That(f.Get("B").Crack, Is.EqualTo(-1));
            Assert.That(f.Get("B").CrackTime, Is.LessThan(0f));
        }

        [Test]
        public void EmbeddedFile_HasTheFourWhipClips_With39Nodes()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "WhipItUp-Mod", "src", "Weapons", "whip_lash_frames.txt");
            Assert.That(File.Exists(path), Is.True, "missing " + path);
            var f = WhipLashFrames.Parse(File.ReadAllText(path), null);
            foreach (var (name, frames) in new[] { ("W1_SideFlick", 34), ("W2_Backhand", 25), ("W3_Overhand", 25), ("G_LashGrab", 34) })
            {
                var c = f.Get(name);
                Assert.That(c, Is.Not.Null, name);
                Assert.That(c.Frames, Is.EqualTo(frames), name);
                Assert.That(c.Nodes, Is.EqualTo(39), name);
                Assert.That(c.Crack, Is.InRange(0, frames - 1), name + " crack frame");
            }
        }
    }
}
