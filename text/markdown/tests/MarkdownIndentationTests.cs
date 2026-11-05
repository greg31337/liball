using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownIndentationTests
{
    [Test]
    public void GetColumnsToNextTabStop_UsesFourColumnStops()
    {
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(0), Is.EqualTo(4));
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(1), Is.EqualTo(3));
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(2), Is.EqualTo(2));
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(3), Is.EqualTo(1));
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(4), Is.EqualTo(4));
        Assert.That(MarkdownIndentation.GetColumnsToNextTabStop(7), Is.EqualTo(1));
    }

    [Test]
    public void AdvanceColumn_ExpandsTabsToNextStop()
    {
        Assert.That(MarkdownIndentation.AdvanceColumn(0, 'x'), Is.EqualTo(1));
        Assert.That(MarkdownIndentation.AdvanceColumn(0, ' '), Is.EqualTo(1));
        Assert.That(MarkdownIndentation.AdvanceColumn(0, '\t'), Is.EqualTo(4));
        Assert.That(MarkdownIndentation.AdvanceColumn(1, '\t'), Is.EqualTo(4));
        Assert.That(MarkdownIndentation.AdvanceColumn(4, '\t'), Is.EqualTo(8));
    }

    [Test]
    public void ScanLeading_CountsSpacesAndTabsInVisualColumns()
    {
        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading("  \ttext");

        Assert.That(scan.CharacterIndex, Is.EqualTo(3));
        Assert.That(scan.Column, Is.EqualTo(4));
        Assert.That(scan.IsBlank, Is.False);
    }

    [Test]
    public void ScanLeading_ExpandsTabsFromCurrentColumn()
    {
        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading(" \t text");

        Assert.That(scan.CharacterIndex, Is.EqualTo(3));
        Assert.That(scan.Column, Is.EqualTo(5));
        Assert.That(scan.IsBlank, Is.False);
    }

    [Test]
    public void ScanLeading_ReportsBlankWhitespaceOnlyLines()
    {
        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading("\t  ");

        Assert.That(scan.CharacterIndex, Is.EqualTo(3));
        Assert.That(scan.Column, Is.EqualTo(6));
        Assert.That(scan.IsBlank, Is.True);
    }

    [Test]
    public void ScanLeading_ReportsEmptyLineAsBlank()
    {
        MarkdownIndentationScan scan = MarkdownIndentation.ScanLeading("");

        Assert.That(scan.CharacterIndex, Is.EqualTo(0));
        Assert.That(scan.Column, Is.EqualTo(0));
        Assert.That(scan.IsBlank, Is.True);
    }

    [Test]
    public void CountAndFindConvenienceMethods_UseLeadingScan()
    {
        Assert.That(MarkdownIndentation.CountLeadingColumns("\t text"), Is.EqualTo(5));
        Assert.That(MarkdownIndentation.FindFirstNonSpaceOrTab("\t text"), Is.EqualTo(2));
    }

    [Test]
    public void IndentationHelpers_RejectInvalidArguments()
    {
        Assert.That(() => MarkdownIndentation.GetColumnsToNextTabStop(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownIndentation.AdvanceColumn(-1, '\t'), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownIndentation.ScanLeading(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownIndentationScan.Create(-1, 0, true), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownIndentationScan.Create(0, -1, true), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
