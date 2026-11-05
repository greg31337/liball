using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSegmentationEdgeTests
{
    [Test]
    public void EmptyText_HasOnlyItsSharedBoundaryAndMandatoryEnd()
    {
        UnicodeSegments segments = new UnicodeSegments();

        Assert.That(segments.GetGraphemeBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(segments.GetWordBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(segments.GetSentenceBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(segments.GetLineBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(segments.GetGraphemeSegments(""), Is.Empty);
        Assert.That(segments.GetWordSegments(""), Is.Empty);
        Assert.That(segments.GetSentenceSegments(""), Is.Empty);
        Assert.That(segments.GetLineSegments(""), Is.Empty);

        UnicodeLineBreakRecord[] opportunities = segments.GetLineBreakOpportunities("");
        Assert.That(opportunities.Select(result => result.Utf16Index), Is.EqualTo(new int[] { 0 }));
        Assert.That(opportunities[0].Status, Is.EqualTo(UnicodeLineBreakStatus.Mandatory));
    }

    [Test]
    public void MalformedUtf16_IsReplacedWithoutLosingOriginalOffsets()
    {
        string text = "A\uD800\u0301B\uDC00";
        UnicodeSegments segments = new UnicodeSegments();

        AssertBreakOffsets(segments.GetGraphemeBreaks(text), text);
        AssertBreakOffsets(segments.GetWordBreaks(text), text);
        AssertBreakOffsets(segments.GetSentenceBreaks(text), text);
        AssertBreakOffsets(segments.GetLineBreaks(text), text);
        AssertSegmentsCover(segments.GetGraphemeSegments(text), text.Length);
        AssertSegmentsCover(segments.GetWordSegments(text), text.Length);
        AssertSegmentsCover(segments.GetSentenceSegments(text), text.Length);
        AssertSegmentsCover(segments.GetLineSegments(text), text.Length);
        Assert.That(segments.GetGraphemeBreaks(text), Is.EqualTo(new int[] { 0, 1, 3, 4, 5 }));
        Assert.That(segments.GetLineBreakOpportunities(text)[^1].Utf16Index, Is.EqualTo(text.Length));
    }

    [Test]
    public void EveryBoundaryPreservesUtf16ScalarIntegrity()
    {
        string text = "A\U0001F600\u0301B\r\n\u754C";
        UnicodeSegments segments = new UnicodeSegments();

        AssertBreakOffsets(segments.GetGraphemeBreaks(text), text);
        AssertBreakOffsets(segments.GetWordBreaks(text), text);
        AssertBreakOffsets(segments.GetSentenceBreaks(text), text);
        AssertBreakOffsets(segments.GetLineBreaks(text), text);
    }

    [Test]
    public void PrefixSuffixSliceAndUtf16Cuts_AlwaysUseGraphemeBoundaries()
    {
        string text = "a\u0301\U0001F468\u200D\U0001F469\u754Cb";
        UnicodeSegments segments = new UnicodeSegments();
        int[] boundaries = segments.GetGraphemeBreaks(text);
        int clusterCount = boundaries.Length - 1;

        int count = 0;
        while (count <= clusterCount + 1)
        {
            string prefix = segments.TruncateGraphemeClusters(text, count);
            string suffix = segments.SuffixGraphemeClusters(text, count);
            Assert.That(boundaries, Does.Contain(prefix.Length));
            Assert.That(boundaries, Does.Contain(text.Length - suffix.Length));
            count = count + 1;
        }

        int limit = 0;
        while (limit <= text.Length)
        {
            string prefix = segments.TruncateGraphemeUtf16(text, limit);
            Assert.That(prefix.Length, Is.LessThanOrEqualTo(limit));
            Assert.That(boundaries, Does.Contain(prefix.Length));
            limit = limit + 1;
        }

        int start = 0;
        while (start <= clusterCount)
        {
            count = 0;
            while (count <= clusterCount + 1)
            {
                string slice = segments.SliceGraphemeClusters(text, start, count);
                int expectedStart = boundaries[start];
                int expectedEndIndex = start + count;
                if (expectedEndIndex > clusterCount)
                {
                    expectedEndIndex = clusterCount;
                }

                Assert.That(slice, Is.EqualTo(text.Substring(expectedStart, boundaries[expectedEndIndex] - expectedStart)));
                count = count + 1;
            }

            start = start + 1;
        }
    }

    [Test]
    public void WidthLimitedCuts_AreClusterSafeWithinLimitAndMaximal()
    {
        string text = "a\u0301\u754C\U0001F600b";
        UnicodeSegments segments = new UnicodeSegments();
        int[] boundaries = segments.GetGraphemeBreaks(text);
        int totalWidth = segments.GetGraphemeDisplayWidth(text);
        int limit = 0;

        while (limit <= totalWidth + 1)
        {
            string result = segments.TruncateGraphemeDisplayWidth(text, limit);
            Assert.That(boundaries, Does.Contain(result.Length));
            Assert.That(segments.GetGraphemeDisplayWidth(result), Is.LessThanOrEqualTo(limit));

            if (result.Length < text.Length)
            {
                int boundaryIndex = System.Array.IndexOf(boundaries, result.Length);
                string throughNextCluster = text.Substring(0, boundaries[boundaryIndex + 1]);
                Assert.That(segments.GetGraphemeDisplayWidth(throughNextCluster), Is.GreaterThan(limit));
            }

            limit = limit + 1;
        }
    }

    static void AssertBreakOffsets(int[] breaks, string text)
    {
        Assert.That(breaks[0], Is.EqualTo(0));
        Assert.That(breaks[^1], Is.EqualTo(text.Length));

        int index = 0;
        while (index < breaks.Length)
        {
            int offset = breaks[index];
            Assert.That(offset, Is.InRange(0, text.Length));
            if (index > 0)
            {
                Assert.That(offset, Is.GreaterThan(breaks[index - 1]));
            }

            if (offset > 0 && offset < text.Length)
            {
                bool splitsSurrogatePair = char.IsHighSurrogate(text[offset - 1]) && char.IsLowSurrogate(text[offset]);
                Assert.That(splitsSurrogatePair, Is.False, "Boundary split a surrogate pair at UTF-16 offset " + offset.ToString());
            }

            index = index + 1;
        }
    }

    static void AssertSegmentsCover(UnicodeSegmentRecord[] segments, int textLength)
    {
        int offset = 0;
        int index = 0;
        while (index < segments.Length)
        {
            Assert.That(segments[index].StartUtf16Index, Is.EqualTo(offset));
            Assert.That(segments[index].EndUtf16Index, Is.GreaterThan(offset));
            offset = segments[index].EndUtf16Index;
            index = index + 1;
        }

        Assert.That(offset, Is.EqualTo(textLength));
    }
}
