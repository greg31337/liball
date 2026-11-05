using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateSourceLineMapTests
{
    [Test]
    public void Constructor_CreatesSingleEmptyLineForEmptySource()
    {
        TemplateSource source = TemplateSource.FromString("");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        Assert.That(lineMap.Source, Is.SameAs(source));
        Assert.That(lineMap.LineCount, Is.EqualTo(1));
        Assert.That(lineMap.LineStarts, Is.EqualTo(new int[] { 0 }));
        Assert.That(lineMap.GetLineText(1), Is.EqualTo(""));
        Assert.That(lineMap.GetPosition(0).LineNumber, Is.EqualTo(1));
        Assert.That(lineMap.GetPosition(0).ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void GetPosition_MapsOffsetsAcrossLfCrLfAndCrLineEndings()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo\r\nthree\rfour");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        TemplateSourcePosition first = lineMap.GetPosition(0);
        TemplateSourcePosition second = lineMap.GetPosition(4);
        TemplateSourcePosition third = lineMap.GetPosition(10);
        TemplateSourcePosition fourth = lineMap.GetPosition(15);

        Assert.That(lineMap.LineCount, Is.EqualTo(4));
        Assert.That(lineMap.LineStarts, Is.EqualTo(new int[] { 0, 4, 9, 15 }));
        Assert.That(first.LineNumber, Is.EqualTo(1));
        Assert.That(first.ColumnNumber, Is.EqualTo(1));
        Assert.That(second.LineNumber, Is.EqualTo(2));
        Assert.That(second.ColumnNumber, Is.EqualTo(1));
        Assert.That(third.LineNumber, Is.EqualTo(3));
        Assert.That(third.ColumnNumber, Is.EqualTo(2));
        Assert.That(fourth.LineNumber, Is.EqualTo(4));
        Assert.That(fourth.ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void GetLine_ReturnsTextRangeWithoutLineEnding()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo\r\nthree\rfour");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        TemplateSourceLine first = lineMap.GetLine(1);
        TemplateSourceLine second = lineMap.GetLine(2);
        TemplateSourceLine third = lineMap.GetLine(3);
        TemplateSourceLine fourth = lineMap.GetLine(4);

        Assert.That(first.Start, Is.EqualTo(0));
        Assert.That(first.End, Is.EqualTo(3));
        Assert.That(first.Length, Is.EqualTo(3));
        Assert.That(first.GetText(), Is.EqualTo("one"));
        Assert.That(second.Start, Is.EqualTo(4));
        Assert.That(second.End, Is.EqualTo(7));
        Assert.That(second.GetText(), Is.EqualTo("two"));
        Assert.That(third.Start, Is.EqualTo(9));
        Assert.That(third.End, Is.EqualTo(14));
        Assert.That(third.GetText(), Is.EqualTo("three"));
        Assert.That(fourth.Start, Is.EqualTo(15));
        Assert.That(fourth.End, Is.EqualTo(19));
        Assert.That(fourth.GetText(), Is.EqualTo("four"));
    }

    [Test]
    public void GetOffset_MapsLineAndColumnBackToOffset()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo\r\nthree\rfour");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        Assert.That(lineMap.GetOffset(1, 1), Is.EqualTo(0));
        Assert.That(lineMap.GetOffset(1, 4), Is.EqualTo(3));
        Assert.That(lineMap.GetOffset(2, 2), Is.EqualTo(5));
        Assert.That(lineMap.GetOffset(3, 6), Is.EqualTo(14));
        Assert.That(lineMap.GetOffset(4, 5), Is.EqualTo(19));
    }

    [Test]
    public void TrailingNewline_CreatesEmptyFinalLine()
    {
        TemplateSource source = TemplateSource.FromString("one\n");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        TemplateSourcePosition endPosition = lineMap.GetPosition(source.Length);
        TemplateSourceLine finalLine = lineMap.GetLine(2);

        Assert.That(lineMap.LineCount, Is.EqualTo(2));
        Assert.That(endPosition.LineNumber, Is.EqualTo(2));
        Assert.That(endPosition.ColumnNumber, Is.EqualTo(1));
        Assert.That(finalLine.Start, Is.EqualTo(4));
        Assert.That(finalLine.End, Is.EqualTo(4));
        Assert.That(finalLine.GetText(), Is.EqualTo(""));
    }

    [Test]
    public void SourceConvenienceMethods_UseLineMap()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo");
        TemplateSourceSpan span = source.CreateSpan(4, 3);

        Assert.That(source.GetPosition(5).LineNumber, Is.EqualTo(2));
        Assert.That(source.GetPosition(5).ColumnNumber, Is.EqualTo(2));
        Assert.That(source.GetLine(2).GetText(), Is.EqualTo("two"));
        Assert.That(source.GetOffset(2, 3), Is.EqualTo(6));
        Assert.That(span.GetStartPosition().LineNumber, Is.EqualTo(2));
        Assert.That(span.GetStartPosition().ColumnNumber, Is.EqualTo(1));
    }

    [Test]
    public void LineMap_ReportsLineStartAndLineEndOffsets()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        Assert.That(lineMap.IsOffsetAtLineStart(0), Is.True);
        Assert.That(lineMap.IsOffsetAtLineStart(1), Is.False);
        Assert.That(lineMap.IsOffsetAtLineStart(4), Is.True);
        Assert.That(lineMap.IsOffsetAtLineEnd(3), Is.True);
        Assert.That(lineMap.IsOffsetAtLineEnd(5), Is.False);
        Assert.That(lineMap.IsOffsetAtLineEnd(7), Is.True);
    }

    [Test]
    public void LineMap_RejectsInvalidOffsetsLinesAndColumns()
    {
        TemplateSource source = TemplateSource.FromString("one\ntwo");
        TemplateSourceLineMap lineMap = source.CreateLineMap();

        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetPosition(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetPosition(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLine(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetLine(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetOffset(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetOffset(1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineMap.GetOffset(2, 2147483647));
    }
}
