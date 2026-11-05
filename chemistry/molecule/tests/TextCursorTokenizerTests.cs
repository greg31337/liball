using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class TextCursorTokenizerTests
{
    [Test]
    public void TextCursor_TracksIndexLineAndColumnAcrossLfAndCrLf()
    {
        TextCursor cursor = new TextCursor("A\r\nBC\nD", "sample.txt");

        Assert.That(cursor.CurrentChar(), Is.EqualTo('A'));
        Assert.That(cursor.Index, Is.EqualTo(0));
        Assert.That(cursor.LineNumber, Is.EqualTo(1));
        Assert.That(cursor.ColumnNumber, Is.EqualTo(1));
        Assert.That(cursor.GetLocation().SourceName, Is.EqualTo("sample.txt"));

        cursor.Advance();
        Assert.That(cursor.Index, Is.EqualTo(1));
        Assert.That(cursor.LineNumber, Is.EqualTo(1));
        Assert.That(cursor.ColumnNumber, Is.EqualTo(2));

        cursor.Advance();
        Assert.That(cursor.Index, Is.EqualTo(3));
        Assert.That(cursor.LineNumber, Is.EqualTo(2));
        Assert.That(cursor.ColumnNumber, Is.EqualTo(1));
        Assert.That(cursor.CurrentChar(), Is.EqualTo('B'));

        cursor.AdvanceCount(2);
        Assert.That(cursor.CurrentChar(), Is.EqualTo('\n'));
        Assert.That(cursor.LineNumber, Is.EqualTo(2));
        Assert.That(cursor.ColumnNumber, Is.EqualTo(3));

        Assert.That(cursor.ConsumeIf('\n'), Is.True);
        Assert.That(cursor.CurrentChar(), Is.EqualTo('D'));
        Assert.That(cursor.LineNumber, Is.EqualTo(3));
        Assert.That(cursor.ColumnNumber, Is.EqualTo(1));
        Assert.That(cursor.ReadToEnd(), Is.EqualTo("D"));
        Assert.That(cursor.IsEnd(), Is.True);
    }

    [Test]
    public void TextCursor_MatchesAndPeeksWithoutAdvancing()
    {
        TextCursor cursor = TextCursor.Create("C1=CC");

        Assert.That(cursor.Match("C1"), Is.True);
        Assert.That(cursor.Match("C2"), Is.False);
        Assert.That(cursor.PeekChar(0), Is.EqualTo('C'));
        Assert.That(cursor.PeekChar(2), Is.EqualTo('='));
        Assert.That(cursor.PeekChar(99), Is.EqualTo('\0'));
        Assert.That(cursor.Index, Is.EqualTo(0));
    }

    [Test]
    public void Tokenizer_SkipsWhitespaceAndTracksTokenLocations()
    {
        Tokenizer tokenizer = new Tokenizer("C1=CC\n -12.5e+2 [O-]", "sample.smi");

        TextToken token0 = tokenizer.NextToken();
        TextToken token1 = tokenizer.NextToken();
        TextToken token2 = tokenizer.NextToken();
        TextToken token3 = tokenizer.NextToken();
        TextToken token4 = tokenizer.NextToken();
        TextToken token5 = tokenizer.NextToken();
        TextToken token6 = tokenizer.NextToken();
        TextToken token7 = tokenizer.NextToken();
        TextToken token8 = tokenizer.NextToken();
        TextToken token9 = tokenizer.NextToken();

        Assert.That(token0.Kind, Is.EqualTo(TextTokenKind.Word));
        Assert.That(token0.Text, Is.EqualTo("C1"));
        Assert.That(token0.LineNumber, Is.EqualTo(1));
        Assert.That(token0.ColumnNumber, Is.EqualTo(1));
        Assert.That(token0.Location.SourceName, Is.EqualTo("sample.smi"));

        Assert.That(token1.Kind, Is.EqualTo(TextTokenKind.Symbol));
        Assert.That(token1.Text, Is.EqualTo("="));
        Assert.That(token1.ColumnNumber, Is.EqualTo(3));

        Assert.That(token2.Kind, Is.EqualTo(TextTokenKind.Word));
        Assert.That(token2.Text, Is.EqualTo("CC"));

        Assert.That(token3.Kind, Is.EqualTo(TextTokenKind.Number));
        Assert.That(token3.Text, Is.EqualTo("-12.5e+2"));
        Assert.That(token3.LineNumber, Is.EqualTo(2));
        Assert.That(token3.ColumnNumber, Is.EqualTo(2));

        Assert.That(token4.Text, Is.EqualTo("["));
        Assert.That(token5.Text, Is.EqualTo("O"));
        Assert.That(token6.Text, Is.EqualTo("-"));
        Assert.That(token7.Text, Is.EqualTo("]"));
        Assert.That(token8.Kind, Is.EqualTo(TextTokenKind.End));
        Assert.That(token8.Text, Is.EqualTo(""));
        Assert.That(token9.Kind, Is.EqualTo(TextTokenKind.End));
    }

    [Test]
    public void Tokenizer_CanReturnWhitespaceTokensWhenConfigured()
    {
        Tokenizer tokenizer = Tokenizer.Create("A \nB");
        tokenizer.SkipWhitespace = false;

        TextToken wordA = tokenizer.NextToken();
        TextToken whitespace = tokenizer.NextToken();
        TextToken wordB = tokenizer.NextToken();

        Assert.That(wordA.Kind, Is.EqualTo(TextTokenKind.Word));
        Assert.That(wordA.Text, Is.EqualTo("A"));
        Assert.That(whitespace.Kind, Is.EqualTo(TextTokenKind.Whitespace));
        Assert.That(whitespace.Text, Is.EqualTo(" \n"));
        Assert.That(whitespace.LineNumber, Is.EqualTo(1));
        Assert.That(whitespace.ColumnNumber, Is.EqualTo(2));
        Assert.That(wordB.Kind, Is.EqualTo(TextTokenKind.Word));
        Assert.That(wordB.Text, Is.EqualTo("B"));
        Assert.That(wordB.LineNumber, Is.EqualTo(2));
        Assert.That(wordB.ColumnNumber, Is.EqualTo(1));
    }
}
