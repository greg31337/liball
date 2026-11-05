using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerWhitespaceCommentTests
{
    [Test]
    public void Lexer_TokenizesWhitespaceAndEndOfFile()
    {
        SqlToken[] tokens = SqlLexer.Tokenize(" \t\r\n");

        Assert.That(tokens.Length, Is.EqualTo(2));
        AssertToken(tokens[0], SqlTokenKind.Whitespace, 0, 4, " \t\r\n");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, 4, 0, "");
    }

    [Test]
    public void Lexer_TokenizesLineCommentBeforeLineBreak()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("-- x\n  ");

        Assert.That(tokens.Length, Is.EqualTo(3));
        AssertToken(tokens[0], SqlTokenKind.LineComment, 0, 4, "-- x");
        AssertToken(tokens[1], SqlTokenKind.Whitespace, 4, 3, "\n  ");
        AssertToken(tokens[2], SqlTokenKind.EndOfFile, 7, 0, "");
    }

    [Test]
    public void Lexer_TokenizesBlockComment()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("/* a */");

        Assert.That(tokens.Length, Is.EqualTo(2));
        AssertToken(tokens[0], SqlTokenKind.BlockComment, 0, 7, "/* a */");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, 7, 0, "");
    }

    [Test]
    public void Lexer_UnterminatedBlockCommentConsumesToEnd()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("/* a");

        Assert.That(tokens.Length, Is.EqualTo(2));
        AssertToken(tokens[0], SqlTokenKind.BlockComment, 0, 4, "/* a");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, 4, 0, "");
    }

    [Test]
    public void Lexer_EmitsBadTokensForUnknownCharactersBeforeMoreScannersExist()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("# ");

        Assert.That(tokens.Length, Is.EqualTo(3));
        AssertToken(tokens[0], SqlTokenKind.BadToken, 0, 1, "#");
        AssertToken(tokens[1], SqlTokenKind.Whitespace, 1, 1, " ");
        AssertToken(tokens[2], SqlTokenKind.EndOfFile, 2, 0, "");
    }

    [Test]
    public void Lexer_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Tokenize(null!));
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Create((string)null!));
        Assert.Throws<ArgumentNullException>(() => SqlLexer.Create((SqlSourceText)null!));
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, int start, int length, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Span.Start, Is.EqualTo(start));
        Assert.That(token.Span.Length, Is.EqualTo(length));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
