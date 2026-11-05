using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SourceLocatorTests
{
    [Test]
    public void Locator_UsesHalfOpenContainmentAndOverlapRules()
    {
        ISqlSourceLocator locator = Locator("select value", null);
        SqlTextSpan span = new SqlTextSpan(7, 5);

        Assert.That(locator.GetExclusiveEnd(span), Is.EqualTo(12));
        Assert.That(locator.ContainsPosition(span, 7), Is.True);
        Assert.That(locator.ContainsPosition(span, 11), Is.True);
        Assert.That(locator.ContainsPosition(span, 12), Is.False);
        Assert.That(locator.ContainsSpan(span, new SqlTextSpan(8, 2)), Is.True);
        Assert.That(locator.ContainsSpan(span, new SqlTextSpan(12, 0)), Is.True);
        Assert.That(locator.ContainsSpan(span, new SqlTextSpan(6, 2)), Is.False);
        Assert.That(locator.Overlaps(span, new SqlTextSpan(11, 2)), Is.True);
        Assert.That(locator.Overlaps(span, new SqlTextSpan(12, 2)), Is.False);
        Assert.That(locator.Overlaps(span, new SqlTextSpan(8, 0)), Is.False);
    }

    [Test]
    public void Locator_ReturnsAllocatedAndSpanSourceSlices()
    {
        SqlSourceText source = new SqlSourceText("select value");
        ISqlSourceLocator locator = new SqlSourceLocator(source);
        SqlTextSpan span = new SqlTextSpan(7, 5);
        source.Text = "changed after construction";

        Assert.That(locator.GetSourceSlice(span), Is.EqualTo("value"));
        Assert.That(locator.GetSourceSliceSpan(span).ToString(), Is.EqualTo("value"));
        Assert.That(locator.GetSourceSlice(new SqlTextSpan(12, 0)), Is.EqualTo(""));
    }

    [Test]
    public void Locator_MapsPositionsAndFileLocations()
    {
        ISqlSourceLocator locator = Locator("select\n  value", "queries/items.sql");
        SqlTextSpan span = new SqlTextSpan(9, 5);

        Assert.That(locator.GetLineNumber(9), Is.EqualTo(2));
        Assert.That(locator.GetColumnNumber(9), Is.EqualTo(3));
        Assert.That(locator.GetLineNumber(14), Is.EqualTo(2));
        Assert.That(locator.GetColumnNumber(14), Is.EqualTo(8));

        SqlFileLocation location = locator.GetFileLocation(span);
        Assert.That(location.FileName, Is.EqualTo("queries/items.sql"));
        Assert.That(location.Span, Is.SameAs(span));
        Assert.That(location.StartLine, Is.EqualTo(2));
        Assert.That(location.StartColumn, Is.EqualTo(3));
        Assert.That(location.EndLine, Is.EqualTo(2));
        Assert.That(location.EndColumn, Is.EqualTo(8));
    }

    [Test]
    public void Locator_MapsMultilineAndEndOfFileLocations()
    {
        ISqlSourceLocator locator = Locator("one\r\ntwo\n", null);

        SqlFileLocation multiline = locator.GetFileLocation(new SqlTextSpan(3, 5));
        SqlFileLocation endOfFile = locator.GetFileLocation(new SqlTextSpan(9, 0));

        Assert.That(multiline.StartLine, Is.EqualTo(1));
        Assert.That(multiline.StartColumn, Is.EqualTo(4));
        Assert.That(multiline.EndLine, Is.EqualTo(2));
        Assert.That(multiline.EndColumn, Is.EqualTo(4));
        Assert.That(endOfFile.StartLine, Is.EqualTo(3));
        Assert.That(endOfFile.StartColumn, Is.EqualTo(1));
        Assert.That(endOfFile.EndLine, Is.EqualTo(3));
        Assert.That(endOfFile.EndColumn, Is.EqualTo(1));
    }

    [Test]
    public void Locator_RejectsNullAndOutOfRangeInputs()
    {
        ISqlSourceLocator locator = Locator("select", null);
        SqlTextSpan span = new SqlTextSpan(0, 1);

        Assert.Throws<ArgumentNullException>(() => new SqlSourceLocator(null!));
        Assert.Throws<ArgumentNullException>(() => locator.GetExclusiveEnd(null!));
        Assert.Throws<ArgumentNullException>(() => locator.ContainsPosition(null!, 0));
        Assert.Throws<ArgumentNullException>(() => locator.ContainsSpan(null!, span));
        Assert.Throws<ArgumentNullException>(() => locator.ContainsSpan(span, null!));
        Assert.Throws<ArgumentNullException>(() => locator.Overlaps(null!, span));
        Assert.Throws<ArgumentNullException>(() => locator.Overlaps(span, null!));
        Assert.Throws<ArgumentNullException>(() => locator.GetSourceSlice(null!));
        Assert.Throws<ArgumentNullException>(() => locator.GetFileLocation(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => locator.GetSourceSlice(new SqlTextSpan(6, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => locator.GetSourceSliceSpan(new SqlTextSpan(7, 0)).ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => locator.GetLineNumber(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => locator.GetColumnNumber(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => locator.GetFileLocation(new SqlTextSpan(5, 2)));
    }

    [Test]
    public void FileLocation_RejectsInvalidCoordinates()
    {
        SqlTextSpan span = new SqlTextSpan(0, 0);

        Assert.Throws<ArgumentNullException>(() => new SqlFileLocation(null, null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlFileLocation(null, span, 0, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlFileLocation(null, span, 1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlFileLocation(null, span, 1, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlFileLocation(null, span, 1, 1, 1, 0));
    }

    static ISqlSourceLocator Locator(string text, string? fileName)
    {
        SqlSourceText source = new SqlSourceText(text);
        source.FileName = fileName;
        return new SqlSourceLocator(source);
    }
}
