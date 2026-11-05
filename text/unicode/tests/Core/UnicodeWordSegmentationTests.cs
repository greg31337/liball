using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeWordSegmentationTests
{
    [Test]
    public void GetWordBreaks_ReturnsStartAndEndForEmptyAndAsciiWords()
    {
        Assert.That(new UnicodeSegments().GetWordBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("abc"), Is.EqualTo(new int[] { 0, 3 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("abc def"), Is.EqualTo(new int[] { 0, 3, 4, 7 }));
    }

    [Test]
    public void GetWordBreaks_KeepsLettersNumbersAndInternalApostrophesTogether()
    {
        Assert.That(new UnicodeSegments().GetWordBreaks("abc123"), Is.EqualTo(new int[] { 0, 6 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("can't stop"), Is.EqualTo(new int[] { 0, 5, 6, 10 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("1'2"), Is.EqualTo(new int[] { 0, 3 }));
    }

    [Test]
    public void GetWordBreaks_HandlesMidPunctuationHebrewKanaAndExtendNumLet()
    {
        Assert.That(new UnicodeSegments().GetWordBreaks("foo.bar"), Is.EqualTo(new int[] { 0, 7 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("3,456"), Is.EqualTo(new int[] { 0, 5 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("\u05D0\"\u05D1"), Is.EqualTo(new int[] { 0, 3 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("\u30AB\u30BF_\u30AB\u30CA"), Is.EqualTo(new int[] { 0, 5 }));
    }

    [Test]
    public void GetWordBreaks_IgnoresExtendAndZwjForWordDecisions()
    {
        Assert.That(new UnicodeSegments().GetWordBreaks("a\u0301b"), Is.EqualTo(new int[] { 0, 3 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("a\u200Db"), Is.EqualTo(new int[] { 0, 3 }));
    }

    [Test]
    public void GetWordBreaks_HandlesCrLfAndControls()
    {
        Assert.That(new UnicodeSegments().GetWordBreaks("\r\nA"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("A\rB"), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(new UnicodeSegments().GetWordBreaks("A\u0001B"), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void GetWordBreaks_GroupsRegionalIndicatorsInPairs()
    {
        string text = "\U0001F1E6\U0001F1E7\U0001F1E8";

        Assert.That(new UnicodeSegments().GetWordBreaks(text), Is.EqualTo(new int[] { 0, 4, 6 }));
    }

    [Test]
    public void GetWordBreaks_KeepsEmojiZwjSequencesTogether()
    {
        string text = "\U0001F600\u200D\U0001F600A";

        Assert.That(new UnicodeSegments().GetWordBreaks(text), Is.EqualTo(new int[] { 0, 5, 6 }));
    }

    [Test]
    public void GetWordBreakClass_UsesBoundaryTablesAndFallbacks()
    {
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x0041), Is.EqualTo(UnicodeWordBreakClass.ALetter));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x0030), Is.EqualTo(UnicodeWordBreakClass.Numeric));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x0027), Is.EqualTo(UnicodeWordBreakClass.SingleQuote));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x0300), Is.EqualTo(UnicodeWordBreakClass.Extend));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x200D), Is.EqualTo(UnicodeWordBreakClass.Zwj));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x1F1E6), Is.EqualTo(UnicodeWordBreakClass.RegionalIndicator));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x1F600), Is.EqualTo(UnicodeWordBreakClass.ExtendedPictographic));
        Assert.That(new UnicodeSegments().GetWordBreakClass(0x0020), Is.EqualTo(UnicodeWordBreakClass.WSegSpace));
    }

    [Test]
    public void WordHelpers_ExposeRepresentativeRulePredicates()
    {
        Assert.That(UnicodeSegments.WordBreakClassFromName("ALetter"), Is.EqualTo(UnicodeWordBreakClass.ALetter));
        Assert.That(UnicodeSegments.IsWordBreakControl(UnicodeWordBreakClass.Newline), Is.True);
        Assert.That(UnicodeSegments.IsWordIgnored(UnicodeWordBreakClass.Extend), Is.True);
        Assert.That(UnicodeSegments.IsWordIgnored(UnicodeWordBreakClass.ALetter), Is.False);
        Assert.That(UnicodeSegments.IsWordLetter(UnicodeWordBreakClass.ALetter), Is.True);
        Assert.That(UnicodeSegments.IsWordLetter(UnicodeWordBreakClass.Numeric), Is.False);
    }

    [Test]
    public void GetWordBreaks_RejectsNullText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetWordBreaks(null!));
    }
}
