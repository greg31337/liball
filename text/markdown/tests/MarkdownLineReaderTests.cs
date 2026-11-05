using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownLineReaderTests
{
    [Test]
    public void ReadLines_ReturnsNoLinesForEmptySource()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("");

        Assert.That(lines, Has.Length.EqualTo(0));
    }

    [Test]
    public void ReadLines_PreservesLfTerminatedLines()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("alpha\nbeta\n");

        Assert.That(lines, Has.Length.EqualTo(2));

        AssertLine(lines[0], 1, "alpha", "\n", 0, 5, 6, 2);
        AssertLine(lines[1], 2, "beta", "\n", 6, 10, 11, 3);
    }

    [Test]
    public void ReadLines_PreservesCrLfCrAndFinalUnterminatedLine()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("a\r\nb\rc");

        Assert.That(lines, Has.Length.EqualTo(3));

        AssertLine(lines[0], 1, "a", "\r\n", 0, 1, 3, 2);
        AssertLine(lines[1], 2, "b", "\r", 3, 4, 5, 3);
        AssertLine(lines[2], 3, "c", "", 5, 6, 6, 3);
        Assert.That(lines[2].LineSpan.End.Column, Is.EqualTo(2));
    }

    [Test]
    public void ReadLines_PreservesBlankTerminatedLines()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("\r\n\n\r");

        Assert.That(lines, Has.Length.EqualTo(3));

        AssertLine(lines[0], 1, "", "\r\n", 0, 0, 2, 2);
        AssertLine(lines[1], 2, "", "\n", 2, 2, 3, 3);
        AssertLine(lines[2], 3, "", "\r", 3, 3, 4, 4);
    }

    [Test]
    public void ReadLines_PreservesSingleUnterminatedLine()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("final");

        Assert.That(lines, Has.Length.EqualTo(1));

        AssertLine(lines[0], 1, "final", "", 0, 5, 5, 1);
        Assert.That(lines[0].LineSpan.End.Column, Is.EqualTo(6));
    }

    [Test]
    public void ReadLines_ReplacesNullCharactersWithoutChangingOffsets()
    {
        MarkdownLine[] lines = MarkdownLineReader.ReadLines("a\0\n\0b");
        string firstLine = "a" + MarkdownInputNormalizer.NullReplacementCharacter.ToString();
        string secondLine = MarkdownInputNormalizer.NullReplacementCharacter.ToString() + "b";

        Assert.That(lines, Has.Length.EqualTo(2));

        AssertLine(lines[0], 1, firstLine, "\n", 0, 2, 3, 2);
        AssertLine(lines[1], 2, secondLine, "", 3, 5, 5, 2);
        Assert.That(lines[1].LineSpan.End.Column, Is.EqualTo(3));
    }

    [Test]
    public void ReadLines_RejectsNullSource()
    {
        Assert.That(() => MarkdownLineReader.ReadLines(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static void AssertLine(
        MarkdownLine line,
        int lineNumber,
        string text,
        string lineEnding,
        int startOffset,
        int contentEndOffset,
        int lineEndOffset,
        int lineEndLine)
    {
        Assert.That(line.LineNumber, Is.EqualTo(lineNumber));
        Assert.That(line.Text, Is.EqualTo(text));
        Assert.That(line.LineEnding, Is.EqualTo(lineEnding));
        Assert.That(line.HasLineEnding(), Is.EqualTo(lineEnding.Length > 0));
        Assert.That(line.GetContentLength(), Is.EqualTo(text.Length));
        Assert.That(line.GetLineLength(), Is.EqualTo(text.Length + lineEnding.Length));

        Assert.That(line.ContentSpan.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(line.ContentSpan.Start.Line, Is.EqualTo(lineNumber));
        Assert.That(line.ContentSpan.Start.Column, Is.EqualTo(1));
        Assert.That(line.ContentSpan.End.Offset, Is.EqualTo(contentEndOffset));
        Assert.That(line.ContentSpan.End.Line, Is.EqualTo(lineNumber));
        Assert.That(line.ContentSpan.End.Column, Is.EqualTo(text.Length + 1));

        Assert.That(line.LineSpan.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(line.LineSpan.Start.Line, Is.EqualTo(lineNumber));
        Assert.That(line.LineSpan.Start.Column, Is.EqualTo(1));
        Assert.That(line.LineSpan.End.Offset, Is.EqualTo(lineEndOffset));
        Assert.That(line.LineSpan.End.Line, Is.EqualTo(lineEndLine));
    }
}
