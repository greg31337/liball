using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeGraphemeSegmentationTests
{
    [Test]
    public void GetGraphemeBreaks_ReturnsStartAndEndForEmptyAndAsciiText()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("ab"), Is.EqualTo(new int[] { 0, 1, 2 }));
    }

    [Test]
    public void GetGraphemeBreaks_KeepsExtendAndZwjWithPreviousCluster()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("a\u0301b"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("a\u200Db"), Is.EqualTo(new int[] { 0, 2, 3 }));
    }

    [Test]
    public void GetGraphemeBreaks_HandlesCrLfAndControls()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("\r\nA"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("A\rB"), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("A\u0001B"), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void GetGraphemeBreaks_KeepsHangulSyllableSequencesTogether()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("\u1100\u1161\u11A8A"), Is.EqualTo(new int[] { 0, 3, 4 }));
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("\uAC00\u11A8A"), Is.EqualTo(new int[] { 0, 2, 3 }));
    }

    [Test]
    public void GetGraphemeBreaks_KeepsIndicConjunctsTogether()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreaks("\u0915\u094D\u0915A"), Is.EqualTo(new int[] { 0, 3, 4 }));
        Assert.That(new UnicodeSegments().GetGraphemeClusterCount("\u0915\u094D\u200D\u0915"), Is.EqualTo(1));
    }

    [Test]
    public void GetGraphemeBreaks_GroupsRegionalIndicatorsInPairs()
    {
        string text = "\U0001F1E6\U0001F1E7\U0001F1E8";

        Assert.That(new UnicodeSegments().GetGraphemeBreaks(text), Is.EqualTo(new int[] { 0, 4, 6 }));
    }

    [Test]
    public void GetGraphemeBreaks_KeepsEmojiZwjSequencesTogether()
    {
        string text = "\U0001F600\u200D\U0001F600A";

        Assert.That(new UnicodeSegments().GetGraphemeBreaks(text), Is.EqualTo(new int[] { 0, 5, 6 }));
    }

    [Test]
    public void GetGraphemeBreakClass_UsesBoundaryTablesAndFallbacks()
    {
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0x000D), Is.EqualTo(UnicodeGraphemeBreakClass.Cr));
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0x0300), Is.EqualTo(UnicodeGraphemeBreakClass.Extend));
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0x1100), Is.EqualTo(UnicodeGraphemeBreakClass.L));
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0xAC01), Is.EqualTo(UnicodeGraphemeBreakClass.Lvt));
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0x1F600), Is.EqualTo(UnicodeGraphemeBreakClass.ExtendedPictographic));
        Assert.That(new UnicodeSegments().GetGraphemeBreakClass(0x0041), Is.EqualTo(UnicodeGraphemeBreakClass.Other));
    }

    [Test]
    public void GraphemeHelpers_ExposeRepresentativeRulePredicates()
    {
        Assert.That(UnicodeSegments.GraphemeBreakClassFromName("Regional_Indicator"), Is.EqualTo(UnicodeGraphemeBreakClass.RegionalIndicator));
        Assert.That(UnicodeSegments.IsGraphemeBreakControl(UnicodeGraphemeBreakClass.Control), Is.True);
        Assert.That(UnicodeSegments.IsGraphemeControl(0x0001), Is.True);
        Assert.That(UnicodeSegments.IsGraphemeControl(0x0041), Is.False);
        Assert.That(UnicodeSegments.IsHangulAfterLeading(UnicodeGraphemeBreakClass.Lv), Is.True);
        Assert.That(UnicodeSegments.IsHangulAfterLvOrV(UnicodeGraphemeBreakClass.T), Is.True);
    }

    [Test]
    public void GetGraphemeBreaks_RejectsNullText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetGraphemeBreaks(null!));
    }
}
