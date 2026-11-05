using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSegmentIteratorTests
{
    [Test]
    public void SegmentRecord_StoresUtf16OffsetsAndLength()
    {
        UnicodeSegmentRecord segment = new UnicodeSegmentRecord(2, 5);

        Assert.That(segment.StartUtf16Index, Is.EqualTo(2));
        Assert.That(segment.EndUtf16Index, Is.EqualTo(5));
        Assert.That(segment.Utf16Length, Is.EqualTo(3));
        Assert.That(segment.Contains(2), Is.True);
        Assert.That(segment.Contains(4), Is.True);
        Assert.That(segment.Contains(5), Is.False);
    }

    [Test]
    public void SegmentRecord_RejectsInvalidOffsets()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegmentRecord(-1, 0));
        Assert.Throws<System.ArgumentException>(() => new UnicodeSegmentRecord(2, 1));
    }

    [Test]
    public void CreateSegments_ConvertsBreakOffsetsToRecords()
    {
        UnicodeSegmentRecord[] segments = UnicodeSegments.CreateSegments(new int[] { 0, 2, 5 });

        Assert.That(segments, Has.Length.EqualTo(2));
        AssertSegment(segments[0], 0, 2);
        AssertSegment(segments[1], 2, 5);
        Assert.That(UnicodeSegments.CreateSegments(new int[] { 0 }), Is.Empty);
    }

    [Test]
    public void GetGraphemeSegments_ReturnsUtf16OffsetRecords()
    {
        UnicodeSegmentRecord[] segments = new UnicodeSegments().GetGraphemeSegments("a\u0301b");

        Assert.That(segments, Has.Length.EqualTo(2));
        AssertSegment(segments[0], 0, 2);
        AssertSegment(segments[1], 2, 3);
    }

    [Test]
    public void GetWordSegments_ReturnsUtf16OffsetRecords()
    {
        UnicodeSegmentRecord[] segments = new UnicodeSegments().GetWordSegments("can't stop");

        Assert.That(segments, Has.Length.EqualTo(3));
        AssertSegment(segments[0], 0, 5);
        AssertSegment(segments[1], 5, 6);
        AssertSegment(segments[2], 6, 10);
    }

    [Test]
    public void GetSentenceSegments_ReturnsUtf16OffsetRecords()
    {
        UnicodeSegmentRecord[] segments = new UnicodeSegments().GetSentenceSegments("Hi. There");

        Assert.That(segments, Has.Length.EqualTo(2));
        AssertSegment(segments[0], 0, 4);
        AssertSegment(segments[1], 4, 9);
    }

    [Test]
    public void GetLineSegments_ReturnsUtf16OffsetRecords()
    {
        UnicodeSegmentRecord[] segments = new UnicodeSegments().GetLineSegments("A B");

        Assert.That(segments, Has.Length.EqualTo(2));
        AssertSegment(segments[0], 0, 2);
        AssertSegment(segments[1], 2, 3);
    }

    [Test]
    public void SegmentIterators_RejectNullInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetGraphemeSegments(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetWordSegments(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetSentenceSegments(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetLineSegments(null!));
        Assert.Throws<System.ArgumentNullException>(() => UnicodeSegments.CreateSegments(null!));
    }

    static void AssertSegment(UnicodeSegmentRecord segment, int start, int end)
    {
        Assert.That(segment.StartUtf16Index, Is.EqualTo(start));
        Assert.That(segment.EndUtf16Index, Is.EqualTo(end));
        Assert.That(segment.Utf16Length, Is.EqualTo(end - start));
    }
}
