using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeBoundaryTableTests
{
    [Test]
    public void GraphemeBreakTables_ContainProductionBoundaryProperties()
    {
        Assert.That(UnicodeBoundaryTables.GraphemeBreaks(), Has.Length.GreaterThan(1000));
        AssertRangeName(0x000D, UnicodeBoundaryTables.GraphemeBreaks(), UnicodeBoundaryTables.GraphemeBreakNames(), "CR");
        AssertRangeName(0x0300, UnicodeBoundaryTables.GraphemeBreaks(), UnicodeBoundaryTables.GraphemeBreakNames(), "Extend");
        AssertRangeName(0x1F1E6, UnicodeBoundaryTables.GraphemeBreaks(), UnicodeBoundaryTables.GraphemeBreakNames(), "Regional_Indicator");
    }

    [Test]
    public void WordAndSentenceBreakTables_ContainProductionBoundaryProperties()
    {
        Assert.That(UnicodeBoundaryTables.WordBreakNames(), Does.Contain("ALetter"));
        Assert.That(UnicodeBoundaryTables.WordBreakNames(), Does.Contain("Hebrew_Letter"));
        AssertRangeName(0x0041, UnicodeBoundaryTables.WordBreaks(), UnicodeBoundaryTables.WordBreakNames(), "ALetter");
        Assert.That(UnicodeBoundaryTables.SentenceBreakNames(), Does.Contain("ATerm"));
        Assert.That(UnicodeBoundaryTables.SentenceBreakNames(), Does.Contain("SContinue"));
        AssertRangeName(0x002E, UnicodeBoundaryTables.SentenceBreaks(), UnicodeBoundaryTables.SentenceBreakNames(), "ATerm");
    }

    [Test]
    public void LineIndicHangulAndEmojiTables_ContainProductionBoundaryProperties()
    {
        Assert.That(UnicodeBoundaryTables.LineBreakNames(), Does.Contain("ID"));
        Assert.That(UnicodeBoundaryTables.LineBreakNames(), Does.Contain("EB"));
        AssertRangeName(0x3400, UnicodeBoundaryTables.LineBreaks(), UnicodeBoundaryTables.LineBreakNames(), "ID");
        AssertRangeName(0x094D, UnicodeBoundaryTables.IndicConjunctBreaks(), UnicodeBoundaryTables.IndicConjunctBreakNames(), "Linker");
        AssertRangeName(0xAC01, UnicodeBoundaryTables.HangulSyllableTypes(), UnicodeBoundaryTables.HangulSyllableTypeNames(), "LVT");
        AssertRangeName(0x1F600, UnicodeBoundaryTables.EmojiBoundaryProperties(), UnicodeBoundaryTables.EmojiBoundaryPropertyNames(), "Extended_Pictographic");
    }

    static void AssertRangeName(int codePoint, UnicodeRangeValue[] ranges, string[] names, string expected)
    {
        Assert.That(
            ranges.Any(range => range.Contains(codePoint) && names[range.Value] == expected),
            Is.True,
            "U+" + codePoint.ToString("X4") + " " + expected);
    }
}
