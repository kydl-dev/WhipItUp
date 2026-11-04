using System;
using System.Collections.Generic;
using WhipItUp.Weapons;

namespace WhipItUp.Tests.Unit;

/// <summary>The keyed lash text parser, pinned down line by line: which lines are skipped, what is logged (exact text and line numbers), and that a null log stays silent.</summary>
[TestFixture]
public class WhipLashFramesParseTests
{
    const string Row = "0 0 0 0 0 0\n";

    static List<string> Run(string text, out WhipLashFrames f)
    {
        var log = new List<string>();
        f = WhipLashFrames.Parse(text, log.Add);
        return log;
    }

    [TestCase(null)]
    [TestCase("")]
    public void Parse_NoText_LogsOnce_AndReadsNothing(string text)
    {
        WhipLashFrames f;
        var log = Run(text, out f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Count, Is.EqualTo(0));
            Assert.That(log, Is.EqualTo(new[] { "WhipLashFrames: no text - no keyed lash" }));
        }
    }

    [Test]
    public void Parse_BadClipHeaders_AreSkipped_WithTheirLineNumber()
    {
        WhipLashFrames f;
        var log = Run("# c\n\nclip A 0 2\nclip B 2 1\nclip C x 2\nclip D 2 y\nclip E 2 2 9\nclip F 1 2\n" + Row, out f);
        const string tail = " is not 'clip <name> <frames> <nodes>': ";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Count, Is.EqualTo(1));
            Assert.That(f.Names, Is.EquivalentTo(new[] { "F" }));
            Assert.That(log, Is.EqualTo(new[]
            {
                "WhipLashFrames: line 3" + tail + "clip A 0 2",
                "WhipLashFrames: line 4" + tail + "clip B 2 1",
                "WhipLashFrames: line 5" + tail + "clip C x 2",
                "WhipLashFrames: line 6" + tail + "clip D 2 y",
                "WhipLashFrames: line 7" + tail + "clip E 2 2 9",
                "WhipLashFrames: clip F: 1 frames x 2 nodes, crack frame -1",
                "WhipLashFrames: 1 clip(s) read, 5 line(s) skipped",
            }));
        }
    }

    [Test]
    public void Parse_CrackLines_AcceptZeroAndTheLastFrame_RejectEverythingElse()
    {
        WhipLashFrames f;
        var log = Run("clip A 3 2\ncrack 0\ncrack 3\ncrack -1\ncrack x\ncrack 2\n" + Row + Row + Row, out f);
        const string tail = " of A is not 'crack <frame 0..2>': ";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Get("A").Crack, Is.EqualTo(2));
            Assert.That(log, Is.EqualTo(new[]
            {
                "WhipLashFrames: line 3" + tail + "crack 3",
                "WhipLashFrames: line 4" + tail + "crack -1",
                "WhipLashFrames: line 5" + tail + "crack x",
                "WhipLashFrames: clip A: 3 frames x 2 nodes, crack frame 2",
                "WhipLashFrames: 1 clip(s) read, 3 line(s) skipped",
            }));
        }
    }

    [Test]
    public void Parse_CrackFrameZero_IsKept()
    {
        WhipLashFrames f;
        Run("clip B 2 2\ncrack 0\n" + Row + Row, out f);
        Assert.That(f.Get("B").Crack, Is.EqualTo(0));
    }

    [Test]
    public void Parse_CrackBeforeAnyClip_IsAStrayLine()
    {
        WhipLashFrames f;
        var log = Run("crack 1\nclip A 1 2\n" + Row, out f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Get("A").Crack, Is.EqualTo(-1));
            Assert.That(log[log.Count - 1], Is.EqualTo("WhipLashFrames: 1 clip(s) read, 1 line(s) skipped"));
        }
    }

    [Test]
    public void Parse_BadDataLines_AreSkipped_CommentsAndBlanksAreIgnored()
    {
        WhipLashFrames f;
        var log = Run("0 0 0 0 0 0\nclip A 2 2\n0 0 0\n0 0 0 0 0 z\n# note\n\n1 2 3 4 5 6\n7 8 9 10 11 12\n", out f);
        float x, y, z;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(log, Is.EqualTo(new[]
            {
                "WhipLashFrames: line 3 of A has 3 values, expected 6",
                "WhipLashFrames: line 4 of A has a value that is not a number",
                "WhipLashFrames: clip A: 2 frames x 2 nodes, crack frame -1",
                "WhipLashFrames: 1 clip(s) read, 3 line(s) skipped",
            }));
            Assert.That(f.Sample("A", 0f, 1, out x, out y, out z), Is.True);
            Assert.That(new[] { x, y, z }, Is.EqualTo(new[] { 4f, 5f, 6f }));
            f.Sample("A", 1f, 1, out x, out y, out z);
            Assert.That(new[] { x, y, z }, Is.EqualTo(new[] { 10f, 11f, 12f }));
        }
    }

    [Test]
    public void Parse_ClipWithTooFewFrames_IsDropped_WithTheCounts()
    {
        WhipLashFrames f;
        var log = Run("clip A 3 2\n" + Row, out f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Count, Is.EqualTo(0));
            Assert.That(f.Get("A"), Is.Null);
            Assert.That(log, Is.EqualTo(new[]
            {
                "WhipLashFrames: clip A has 1 frame line(s), expected 3 - dropped",
                "WhipLashFrames: 0 clip(s) read, 0 line(s) skipped",
            }));
        }
    }

    [Test]
    public void Parse_ClipWithTooManyFrames_IsDropped()
    {
        WhipLashFrames f;
        Run("clip A 1 2\n" + Row + Row, out f);
        Assert.That(f.Count, Is.EqualTo(0));
    }

    [TestCase("crack 1\nclip A 0 2\nclip B 2 1\nclip C 3 2\ncrack 9\ncrack x\n0 0 0\n0 0 0 0 0 z\n# n\n\n")]
    [TestCase("clip A 3 2\n" + Row)]
    [TestCase("0 0 0 0 0 0\nclip A 1 2\n" + Row)]
    [TestCase("")]
    public void Parse_WithoutALog_IsSilentAndDoesNotThrow(string text)
    {
        WhipLashFrames f = null;
        Assert.That(() => f = WhipLashFrames.Parse(text, null), Throws.Nothing);
        Assert.That(f, Is.Not.Null);
    }

    [Test]
    public void Get_UnknownOrNullName_IsNull()
    {
        WhipLashFrames f;
        Run("clip A 1 2\n" + Row, out f);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(f.Get("A"), Is.Not.Null);
            Assert.That(f.Get("a"), Is.Null, "names are case-sensitive");
            Assert.That(f.Get(null), Is.Null);
        }
    }
}
