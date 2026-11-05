using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TextSpanAndLineMapTests
{
    [Test]
    public void TextSpan_FromBoundsComputesLengthAndEnd()
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(4, 9);

        Assert.That(span.Start, Is.EqualTo(4));
        Assert.That(span.Length, Is.EqualTo(5));
        Assert.That(span.GetEnd(), Is.EqualTo(9));
        Assert.That(span.IsEmpty(), Is.False);
    }

    [Test]
    public void TextSpan_ContainsAndOverlapsUseExclusiveEnd()
    {
        SqlTextSpan span = new SqlTextSpan(10, 5);
        SqlTextSpan touchingSpan = new SqlTextSpan(15, 2);
        SqlTextSpan overlappingSpan = new SqlTextSpan(14, 4);

        Assert.That(span.Contains(10), Is.True);
        Assert.That(span.Contains(14), Is.True);
        Assert.That(span.Contains(15), Is.False);
        Assert.That(span.Contains(9), Is.False);
        Assert.That(span.ContainsInclusiveEnd(15), Is.True);
        Assert.That(span.ContainsInclusiveEnd(9), Is.False);
        Assert.That(span.OverlapsWith(touchingSpan), Is.False);
        Assert.That(span.OverlapsWith(overlappingSpan), Is.True);
    }

    [Test]
    public void TextSpan_ContainsSpansIncludingEmptyBoundaries()
    {
        SqlTextSpan span = new SqlTextSpan(4, 5);

        Assert.That(span.ContainsSpan(new SqlTextSpan(4, 5)), Is.True);
        Assert.That(span.ContainsSpan(new SqlTextSpan(9, 0)), Is.True);
        Assert.That(span.ContainsSpan(new SqlTextSpan(3, 1)), Is.False);
        Assert.That(span.ContainsSpan(new SqlTextSpan(8, 2)), Is.False);
        Assert.That(new SqlTextSpan(4, 0).OverlapsWith(span), Is.False);
        Assert.Throws<ArgumentNullException>(() => span.ContainsSpan(null!));
        Assert.Throws<ArgumentNullException>(() => span.OverlapsWith(null!));
    }

    [Test]
    public void TextSpan_RejectsInvalidBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTextSpan(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTextSpan(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTextSpan.FromBounds(5, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTextSpan.FromBounds(-1, int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlTextSpan.FromBounds(int.MinValue, int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlTextSpan(int.MaxValue, 1));
    }

    [Test]
    public void LineMap_ResolvesLfCrLfAndCrLineStarts()
    {
        string text = "one\r\ntwo\nthree\rfour";
        SqlLineMap lineMap = new SqlLineMap(text);

        Assert.That(lineMap.GetLineCount(), Is.EqualTo(4));
        Assert.That(lineMap.GetLineStartPosition(1), Is.EqualTo(0));
        Assert.That(lineMap.GetLineStartPosition(2), Is.EqualTo(5));
        Assert.That(lineMap.GetLineStartPosition(3), Is.EqualTo(9));
        Assert.That(lineMap.GetLineStartPosition(4), Is.EqualTo(15));
        Assert.That(lineMap.GetLineNumber(0), Is.EqualTo(1));
        Assert.That(lineMap.GetColumnNumber(0), Is.EqualTo(1));
        Assert.That(lineMap.GetLineNumber(5), Is.EqualTo(2));
        Assert.That(lineMap.GetColumnNumber(5), Is.EqualTo(1));
        Assert.That(lineMap.GetLineNumber(8), Is.EqualTo(2));
        Assert.That(lineMap.GetColumnNumber(8), Is.EqualTo(4));
        Assert.That(lineMap.GetLineNumberForSpan(new SqlTextSpan(9, 5)), Is.EqualTo(3));
        Assert.That(lineMap.GetColumnNumberForSpan(new SqlTextSpan(9, 5)), Is.EqualTo(1));
        Assert.That(lineMap.GetLineNumber(text.Length), Is.EqualTo(4));
        Assert.That(lineMap.GetColumnNumber(text.Length), Is.EqualTo(5));
    }

    [Test]
    public void LineMap_TreatsFinalLineBreakAsEmptyTrailingLine()
    {
        SqlLineMap lineMap = new SqlLineMap("a\n");

        Assert.That(lineMap.GetLineCount(), Is.EqualTo(2));
        Assert.That(lineMap.GetLineNumber(2), Is.EqualTo(2));
        Assert.That(lineMap.GetColumnNumber(2), Is.EqualTo(1));
    }

    [Test]
    public void LineMap_RejectsInvalidPositionsAndLines()
    {
        SqlLineMap lineMap = new SqlLineMap("select");

        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLineNumber(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLineNumber(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLineStartPosition(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLineStartPosition(2));
        Assert.Throws<ArgumentNullException>(() => new SqlLineMap(null!));
        Assert.Throws<ArgumentNullException>(() => lineMap.GetLineNumberForSpan(null!));
        Assert.Throws<ArgumentNullException>(() => lineMap.GetColumnNumberForSpan(null!));
        Assert.That(lineMap.IsValidPosition(0), Is.True);
        Assert.That(lineMap.IsValidPosition(6), Is.True);
        Assert.That(lineMap.IsValidPosition(7), Is.False);
    }

    [Test]
    public void LineMap_EmptyTextHasOneAddressableLine()
    {
        SqlLineMap lineMap = new SqlLineMap("");

        Assert.That(lineMap.LineStarts, Is.EqualTo(new int[] { 0 }));
        Assert.That(lineMap.GetLineNumber(0), Is.EqualTo(1));
        Assert.That(lineMap.GetColumnNumber(0), Is.EqualTo(1));

        SqlLineMap trailingCr = new SqlLineMap("a\r");
        Assert.That(trailingCr.LineStarts, Is.EqualTo(new int[] { 0, 2 }));
    }
}
