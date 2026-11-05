using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfLayoutClusterTests
{
    [Test]
    public void GetLayoutClusters_ReturnsGraphemeClusterUtf16Ranges()
    {
        string text = "a\u0301b\U0001F600";

        UnicodeSegmentRecord[] clusters = new UnicodePdfText().GetLayoutClusters(text);

        Assert.That(clusters, Has.Length.EqualTo(3));
        AssertSegment(clusters[0], 0, 2);
        AssertSegment(clusters[1], 2, 3);
        AssertSegment(clusters[2], 3, 5);
        Assert.That(text.Substring(clusters[0].StartUtf16Index, clusters[0].Utf16Length), Is.EqualTo("a\u0301"));
    }

    [Test]
    public void GetLayoutClusters_KeepsEmojiZwjSequencesTogether()
    {
        string text = "\U0001F600\u200D\U0001F600A";

        UnicodeSegmentRecord[] clusters = new UnicodePdfText().GetLayoutClusters(text);

        Assert.That(clusters, Has.Length.EqualTo(2));
        AssertSegment(clusters[0], 0, 5);
        AssertSegment(clusters[1], 5, 6);
    }

    [Test]
    public void GetLayoutClusters_PreservesInvalidUtf16OffsetsAsReplacementClusters()
    {
        string text = "\uD800A";

        UnicodeSegmentRecord[] clusters = new UnicodePdfText().GetLayoutClusters(text);

        Assert.That(clusters, Has.Length.EqualTo(2));
        AssertSegment(clusters[0], 0, 1);
        AssertSegment(clusters[1], 1, 2);
    }

    [Test]
    public void GetLayoutClusters_ReturnsNoSegmentsForEmptyText()
    {
        Assert.That(new UnicodePdfText().GetLayoutClusters(""), Is.Empty);
    }

    [Test]
    public void GetLayoutClusters_MatchesCoreGraphemeSegmentation()
    {
        string text = "\r\nA\u0301";

        Assert.That(
            ToRangeText(new UnicodePdfText().GetLayoutClusters(text)),
            Is.EqualTo(ToRangeText(new UnicodeSegments().GetGraphemeSegments(text))));
    }

    [Test]
    public void GetLayoutClusters_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().GetLayoutClusters(null!));
    }

    static string ToRangeText(UnicodeSegmentRecord[] segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < segments.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(segments[index].StartUtf16Index);
            builder.Append("-");
            builder.Append(segments[index].EndUtf16Index);
            index = index + 1;
        }

        return builder.ToString();
    }

    static void AssertSegment(UnicodeSegmentRecord segment, int startUtf16Index, int endUtf16Index)
    {
        Assert.That(segment.StartUtf16Index, Is.EqualTo(startUtf16Index));
        Assert.That(segment.EndUtf16Index, Is.EqualTo(endUtf16Index));
        Assert.That(segment.Utf16Length, Is.EqualTo(endUtf16Index - startUtf16Index));
    }
}
