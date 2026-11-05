using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeGraphemeSliceTests
{
    [Test]
    public void GetGraphemeClusterCount_CountsUserVisibleClusters()
    {
        Assert.That(new UnicodeSegments().GetGraphemeClusterCount(""), Is.EqualTo(0));
        Assert.That(new UnicodeSegments().GetGraphemeClusterCount("a\u0301b\U0001F600"), Is.EqualTo(3));
        Assert.That(new UnicodeSegments().GetGraphemeClusterCount("\U0001F600\u200D\U0001F600"), Is.EqualTo(1));
    }

    [Test]
    public void TruncateGraphemeClusters_TruncatesByClusterCount()
    {
        string text = "a\u0301b\U0001F600";

        Assert.That(new UnicodeSegments().TruncateGraphemeClusters(text, 0), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().TruncateGraphemeClusters(text, 1), Is.EqualTo("a\u0301"));
        Assert.That(new UnicodeSegments().TruncateGraphemeClusters(text, 2), Is.EqualTo("a\u0301b"));
        Assert.That(new UnicodeSegments().TruncateGraphemeClusters(text, 10), Is.EqualTo(text));
    }

    [Test]
    public void SliceGraphemeClusters_ReturnsWholeClusterRanges()
    {
        string text = "a\u0301b\U0001F600";

        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 0, 1), Is.EqualTo("a\u0301"));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 1, 2), Is.EqualTo("b\U0001F600"));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 2, 10), Is.EqualTo("\U0001F600"));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 1, int.MaxValue), Is.EqualTo("b\U0001F600"));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 3, 1), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 1, 0), Is.EqualTo(""));
    }

    [Test]
    public void TruncateGraphemeUtf16_DoesNotSplitClusters()
    {
        string text = "a\u0301b\U0001F600";

        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 0), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 1), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 2), Is.EqualTo("a\u0301"));
        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 4), Is.EqualTo("a\u0301b"));
        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 5), Is.EqualTo(text));
    }

    [Test]
    public void TruncateGraphemeUtf16_DoesNotSplitEmojiZwjSequences()
    {
        string text = "\U0001F600\u200D\U0001F600A";

        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 4), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().TruncateGraphemeUtf16(text, 5), Is.EqualTo("\U0001F600\u200D\U0001F600"));
        Assert.That(new UnicodeSegments().SliceGraphemeClusters(text, 0, 1), Is.EqualTo("\U0001F600\u200D\U0001F600"));
    }

    [Test]
    public void SuffixGraphemeClusters_ReturnsWholeTrailingClusters()
    {
        string text = "a\u0301b\U0001F600";

        Assert.That(new UnicodeSegments().SuffixGraphemeClusters(text, 0), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().SuffixGraphemeClusters(text, 2), Is.EqualTo("b\U0001F600"));
        Assert.That(new UnicodeSegments().SuffixGraphemeClusters(text, 20), Is.EqualTo(text));
    }

    [Test]
    public void DisplayWidthAndTruncation_RespectWideAndEmojiClusters()
    {
        string text = "a\u0301\u754C\U0001F600";

        Assert.That(new UnicodeSegments().GetGraphemeDisplayWidth(text), Is.EqualTo(5));
        Assert.That(new UnicodeSegments().GetGraphemeDisplayWidth("\u0301"), Is.EqualTo(0));
        Assert.That(new UnicodeSegments().TruncateGraphemeDisplayWidth(text, 0), Is.EqualTo(""));
        Assert.That(new UnicodeSegments().TruncateGraphemeDisplayWidth(text, 3), Is.EqualTo("a\u0301\u754C"));
        Assert.That(new UnicodeSegments().TruncateGraphemeDisplayWidth(text, 4), Is.EqualTo("a\u0301\u754C"));
        Assert.That(new UnicodeSegments().TruncateGraphemeDisplayWidth(text, 5), Is.EqualTo(text));
    }

    [Test]
    public void GraphemeSliceHelpers_RejectNullAndInvalidArguments()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetGraphemeClusterCount(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().TruncateGraphemeClusters(null!, 1));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().SliceGraphemeClusters(null!, 0, 1));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().TruncateGraphemeUtf16(null!, 1));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().SuffixGraphemeClusters(null!, 1));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetGraphemeDisplayWidth(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().TruncateGraphemeDisplayWidth(null!, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().TruncateGraphemeClusters("a", -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().SliceGraphemeClusters("a", -1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().SliceGraphemeClusters("a", 0, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().TruncateGraphemeUtf16("a", -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().SuffixGraphemeClusters("a", -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeSegments().TruncateGraphemeDisplayWidth("a", -1));
    }
}
