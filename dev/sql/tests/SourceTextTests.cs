using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SourceTextTests
{
    [Test]
    public void SourceText_StoresTextAndBuildsFullSpan()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select\n1");

        Assert.That(sourceText.Text, Is.EqualTo("select\n1"));
        Assert.That(sourceText.FileName, Is.Null);
        Assert.That(sourceText.HasFileName(), Is.False);
        Assert.That(sourceText.GetLength(), Is.EqualTo(8));
        Assert.That(sourceText.IsEmpty(), Is.False);
        Assert.That(sourceText.FullSpan.Start, Is.EqualTo(0));
        Assert.That(sourceText.FullSpan.Length, Is.EqualTo(8));
    }

    [Test]
    public void SourceText_StoresOptionalFileName()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select 1", "queries/sample.sql");

        Assert.That(sourceText.FileName, Is.EqualTo("queries/sample.sql"));
        Assert.That(sourceText.HasFileName(), Is.True);
    }

    [Test]
    public void SourceText_DelegatesPositionLookupToLineMap()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select\n1");

        Assert.That(sourceText.GetLineCount(), Is.EqualTo(2));
        Assert.That(sourceText.GetLineStartPosition(2), Is.EqualTo(7));
        Assert.That(sourceText.GetLineNumber(7), Is.EqualTo(2));
        Assert.That(sourceText.GetColumnNumber(7), Is.EqualTo(1));
        Assert.That(sourceText.GetLineNumberForSpan(new SqlTextSpan(7, 1)), Is.EqualTo(2));
        Assert.That(sourceText.GetColumnNumberForSpan(new SqlTextSpan(7, 1)), Is.EqualTo(1));
    }

    [Test]
    public void SourceText_ReturnsSubstringForValidSpan()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select 1");

        Assert.That(sourceText.GetText(new SqlTextSpan(0, 6)), Is.EqualTo("select"));
        Assert.That(sourceText.GetText(SqlTextSpan.FromBounds(7, 8)), Is.EqualTo("1"));
    }

    [Test]
    public void SourceText_RejectsNullTextAndInvalidSpans()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select");

        Assert.Throws<ArgumentNullException>(() => SqlSourceText.Create(null!));
        Assert.Throws<ArgumentNullException>(() => sourceText.GetText(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => sourceText.GetText(new SqlTextSpan(6, 1)));
    }

    [Test]
    public void SourceText_HandlesEmptyTextAndBoundarySpans()
    {
        SqlSourceText empty = new SqlSourceText("");
        SqlSourceText sourceText = SqlSourceText.Create("abc", "");

        Assert.That(empty.IsEmpty(), Is.True);
        Assert.That(empty.GetLineCount(), Is.EqualTo(1));
        Assert.That(empty.GetText(new SqlTextSpan(0, 0)), Is.Empty);
        Assert.That(sourceText.HasFileName(), Is.False);
        Assert.That(sourceText.GetText(new SqlTextSpan(3, 0)), Is.Empty);
        Assert.Throws<ArgumentOutOfRangeException>(() => sourceText.GetText(new SqlTextSpan(4, 0)));
    }
}
