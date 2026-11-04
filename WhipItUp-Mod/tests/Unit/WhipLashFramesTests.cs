using System.Collections.Generic;
using System.IO;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit;

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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Get("A").Frames, Is.EqualTo(2));
            Assert.That(f.Get("A").Nodes, Is.EqualTo(2));
            Assert.That(log.Exists(l => l.Contains("2 clip(s) read")), Is.True);
        }
    }

    [Test]
    public void Sample_InterpolatesBetweenFrames_AndClampsTime()
    {
        var f = WhipLashFrames.Parse(Small, null);
        float x;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Sample("A", 0.5f, 1, out x, out _, out _), Is.True);
            Assert.That(x, Is.EqualTo(2f).Within(1e-5));
        }
        f.Sample("A", 7f, 1, out x, out _, out _);
        Assert.That(x, Is.EqualTo(3f).Within(1e-5));
        f.Sample("A", -1f, 1, out x, out _, out _);
        Assert.That(x, Is.EqualTo(1f).Within(1e-5));
    }

    const string Three = "clip C 3 2\n0 0 0 1 2 3\n0 0 0 5 6 7\n0 0 0 9 10 11\n";

    [TestCase(0f, 1f, 2f, 3f)]
    [TestCase(0.25f, 3f, 4f, 5f)]
    [TestCase(0.5f, 5f, 6f, 7f)]
    [TestCase(0.75f, 7f, 8f, 9f)]
    [TestCase(1f, 9f, 10f, 11f)]
    public void Sample_AllThreeAxes_InterpolateOverSeveralFrames(float t, float ex, float ey, float ez)
    {
        var f = WhipLashFrames.Parse(Three, null);
        float x, y, z;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Sample("C", t, 1, out x, out y, out z), Is.True);
            Assert.That(x, Is.EqualTo(ex).Within(1e-4));
            Assert.That(y, Is.EqualTo(ey).Within(1e-4));
            Assert.That(z, Is.EqualTo(ez).Within(1e-4));
        }
    }

    [Test]
    public void Sample_FirstNodeIsValid_AndTheNodeCountIsOutOfRange()
    {
        var f = WhipLashFrames.Parse(Three, null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Sample("C", 0f, 0, out _, out _, out _), Is.True);
            Assert.That(f.Sample("C", 0f, 2, out _, out _, out _), Is.False, "nodes are 0..Nodes-1");
            Assert.That(f.Sample("C", 0f, -1, out _, out _, out _), Is.False);
            Assert.That(f.Sample(null, 0f, 0, out _, out _, out _), Is.False);
        }
    }

    [Test]
    public void Sample_NaNTime_IsTheFirstFrame()
    {
        var f = WhipLashFrames.Parse(Three, null);
        float x;
        f.Sample("C", float.NaN, 1, out x, out _, out _);
        Assert.That(x, Is.EqualTo(1f).Within(1e-5));
    }

    [Test]
    public void Names_ListsEveryClip()
    {
        var f = WhipLashFrames.Parse(Small, null);
        Assert.That(f.Names, Is.EquivalentTo(new[] { "A", "B" }));
    }

    [TestCase(0, 3, 0f)]
    [TestCase(1, 3, 0.5f)]
    [TestCase(2, 3, 1f)]
    [TestCase(-1, 3, -1f)]
    [TestCase(0, 1, -1f)]
    [TestCase(1, 1, -1f)]
    public void CrackTime_IsTheCrackFrameOverTheLastFrame_OrMinusOne(int crack, int frames, float want)
    {
        var c = new WhipLashFrames.Clip { Crack = crack, Frames = frames };
        Assert.That(c.CrackTime, Is.EqualTo(want).Within(1e-5));
    }

    [Test]
    public void Sample_UnknownClipOrNode_IsFalse()
    {
        var f = WhipLashFrames.Parse(Small, null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Sample("X", 0f, 0, out _, out _, out _), Is.False);
            Assert.That(f.Sample("A", 0f, 5, out _, out _, out _), Is.False);
        }
    }

    [Test]
    public void Parse_ClipWithMissingFrames_IsDropped_AndBadLinesAreLogged()
    {
        var log = new List<string>();
        var f = WhipLashFrames.Parse("clip A 3 2\n0 0 0 0 0 0\n0 x 0 0 0 0\nclip B 1 2\n1 2\n", log.Add);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Count, Is.EqualTo(0));
            Assert.That(log.Exists(l => l.Contains("not a number")), Is.True, string.Join(" | ", log));
            Assert.That(log.Exists(l => l.Contains("expected 3")), Is.True);
        }
    }

    [Test]
    public void Parse_ReadsTheCrackFrame_AndItsNormalizedTime()
    {
        var f = WhipLashFrames.Parse("clip A 3 2\ncrack 2\n0 0 0 0 0 0\n0 0 0 0 0 0\n0 0 0 0 0 0\nclip B 2 2\n0 0 0 0 0 0\n0 0 0 0 0 0\n", null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Get("A").Crack, Is.EqualTo(2));
            Assert.That(f.Get("A").CrackTime, Is.EqualTo(1f).Within(1e-5));
            Assert.That(f.Get("B").Crack, Is.EqualTo(-1));
            Assert.That(f.Get("B").CrackTime, Is.LessThan(0f));
        }
    }

    [Test]
    public void EmbeddedFile_HasTheFourWhipClips_With39Nodes()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "src", "Features", "Whip", "Infrastructure", "whip_lash_frames.txt");
        Assert.That(File.Exists(path), Is.True, "missing " + path);
        var f = WhipLashFrames.Parse(File.ReadAllText(path), null);
        foreach (var (name, frames) in new[] { ("W1_SideFlick", 34), ("W2_Backhand", 25), ("W3_Overhand", 25), ("G_LashGrab", 34) })
        {
            var c = f.Get(name);
            Assert.That(c, Is.Not.Null, name);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(c.Frames, Is.EqualTo(frames), name);
                Assert.That(c.Nodes, Is.EqualTo(39), name);
                Assert.That(c.Crack, Is.InRange(0, frames - 1), name + " crack frame");
            }
        }
    }
}
