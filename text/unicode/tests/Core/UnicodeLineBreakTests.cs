using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeLineBreakTests
{
    [Test]
    public void GetLineBreaks_ReturnsStartAndEndForEmptyAndPlainText()
    {
        Assert.That(new UnicodeSegments().GetLineBreaks(""), Is.EqualTo(new int[] { 0 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("ABC"), Is.EqualTo(new int[] { 0, 3 }));
    }

    [Test]
    public void GetLineBreaks_BreaksAfterSpaceRuns()
    {
        Assert.That(new UnicodeSegments().GetLineBreaks("A B"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("A  B"), Is.EqualTo(new int[] { 0, 3, 4 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("A! B"), Is.EqualTo(new int[] { 0, 3, 4 }));
    }

    [Test]
    public void GetLineBreaks_BreaksAfterMandatoryLineEndings()
    {
        Assert.That(new UnicodeSegments().GetLineBreaks("A\nB"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("\r\nA"), Is.EqualTo(new int[] { 0, 2, 3 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("A\u0001B"), Is.EqualTo(new int[] { 0, 3 }));
    }

    [Test]
    public void GetLineBreaks_AllowsIdeographicAndEmojiBaseBreaks()
    {
        Assert.That(new UnicodeSegments().GetLineBreaks("\u3400\u3401A"), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("\U0001F600\U0001F600A"), Is.EqualTo(new int[] { 0, 2, 4, 5 }));
    }

    [Test]
    public void GetLineBreakOpportunities_DistinguishesOptionalAndMandatoryBreaks()
    {
        UnicodeLineBreakRecord[] results = new UnicodeSegments().GetLineBreakOpportunities("A B\r\nC");

        Assert.That(results.Select(result => result.Utf16Index), Is.EqualTo(new int[] { 2, 5, 6 }));
        Assert.That(results.Select(result => result.Status), Is.EqualTo(new UnicodeLineBreakStatus[]
        {
            UnicodeLineBreakStatus.Optional,
            UnicodeLineBreakStatus.Mandatory,
            UnicodeLineBreakStatus.Mandatory,
        }));
        Assert.That(new UnicodeSegments().GetLineBreakOpportunities("")[0].Status, Is.EqualTo(UnicodeLineBreakStatus.Mandatory));
    }

    [Test]
    public void GetLineBreaks_AppliesNumericHangulAndRegionalIndicatorRules()
    {
        Assert.That(new UnicodeSegments().GetLineBreaks("$12,345.00"), Is.EqualTo(new int[] { 0, 10 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("\u1100\u1161\u11A8"), Is.EqualTo(new int[] { 0, 3 }));
        Assert.That(new UnicodeSegments().GetLineBreaks("\U0001F1E6\U0001F1E7\U0001F1E8"), Is.EqualTo(new int[] { 0, 4, 6 }));
    }

    [Test]
    public void GetLineBreakClass_UsesBoundaryTablesAndFallbacks()
    {
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x0020), Is.EqualTo(UnicodeLineBreakClass.Space));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x0021), Is.EqualTo(UnicodeLineBreakClass.Exclamation));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x3400), Is.EqualTo(UnicodeLineBreakClass.Ideographic));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x1F600), Is.EqualTo(UnicodeLineBreakClass.Ideographic));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x000D), Is.EqualTo(UnicodeLineBreakClass.CarriageReturn));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x000A), Is.EqualTo(UnicodeLineBreakClass.LineFeed));
        Assert.That(new UnicodeSegments().GetLineBreakClass(0x0041), Is.EqualTo(UnicodeLineBreakClass.Alphabetic));
    }

    [Test]
    public void LineHelpers_ExposeRepresentativeRulePredicates()
    {
        Assert.That(UnicodeSegments.LineBreakClassFromName("SP"), Is.EqualTo(UnicodeLineBreakClass.Space));
        Assert.That(UnicodeSegments.LineBreakClassFromName("ID"), Is.EqualTo(UnicodeLineBreakClass.Ideographic));
        Assert.That(UnicodeSegments.LineBreakClassFromName("EB"), Is.EqualTo(UnicodeLineBreakClass.EmojiBase));
        Assert.That(UnicodeSegments.IsLineMandatoryBreakBefore(UnicodeLineBreakClass.LineFeed), Is.True);
        Assert.That(UnicodeSegments.IsLineMandatoryBreakAfter(UnicodeLineBreakClass.CarriageReturn), Is.True);
        Assert.That(UnicodeSegments.IsLineBreakForbiddenBefore(UnicodeLineBreakClass.ClosingPunctuation), Is.True);
        Assert.That(UnicodeSegments.IsLineBreakForbiddenBefore(UnicodeLineBreakClass.ClosingParenthesis), Is.True);
        Assert.That(UnicodeSegments.IsLineBreakForbiddenBefore(UnicodeLineBreakClass.Exclamation), Is.True);
        Assert.That(UnicodeSegments.IsLineBreakForbiddenBefore(UnicodeLineBreakClass.WordJoiner), Is.True);
        Assert.That(UnicodeSegments.IsLineIdeographicLike(UnicodeLineBreakClass.EmojiBase), Is.True);
    }

    [Test]
    public void GetLineBreaks_RejectsNullText()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetLineBreaks(null!));
        Assert.Throws<System.ArgumentNullException>(() => new UnicodeSegments().GetLineBreakOpportunities(null!));
    }

    [Test]
    public void UnicodeLineBreakRecord_StoresValidatedResult()
    {
        UnicodeLineBreakRecord result = new UnicodeLineBreakRecord(4, UnicodeLineBreakStatus.Optional);

        Assert.That(result.Utf16Index, Is.EqualTo(4));
        Assert.That(result.Status, Is.EqualTo(UnicodeLineBreakStatus.Optional));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new UnicodeLineBreakRecord(-1, UnicodeLineBreakStatus.Mandatory));
    }
}
