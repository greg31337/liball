using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerNumericLiteralTests
{
    [Test]
    public void Lexer_TokenizesIntegerLiterals()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("0 123");

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "0");
        AssertToken(tokens[2], SqlTokenKind.NumericLiteral, "123");
        AssertToken(tokens[3], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesDecimalLiterals()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("1.5 1. .5 0.25");

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "1.5");
        AssertToken(tokens[2], SqlTokenKind.NumericLiteral, "1.");
        AssertToken(tokens[4], SqlTokenKind.NumericLiteral, ".5");
        AssertToken(tokens[6], SqlTokenKind.NumericLiteral, "0.25");
    }

    [Test]
    public void Lexer_TokenizesExponentLiterals()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("1e10 1E-2 .5e+3 1.2E3");

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "1e10");
        AssertToken(tokens[2], SqlTokenKind.NumericLiteral, "1E-2");
        AssertToken(tokens[4], SqlTokenKind.NumericLiteral, ".5e+3");
        AssertToken(tokens[6], SqlTokenKind.NumericLiteral, "1.2E3");
    }

    [Test]
    public void Lexer_TokenizesSqliteHexIntegerLiteralsWhenDialectAllowsThem()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("0x10 0XABC", options);

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "0x10");
        AssertToken(tokens[2], SqlTokenKind.NumericLiteral, "0XABC");
    }

    [Test]
    public void Lexer_GenericDialectDoesNotTokenizeHexIntegerAsSingleLiteral()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("0x10");

        AssertToken(tokens[0], SqlTokenKind.NumericLiteral, "0");
        AssertToken(tokens[1], SqlTokenKind.Identifier, "x10");
    }

    [Test]
    public void Lexer_DotWithoutDigitRemainsPunctuation()
    {
        SqlToken[] tokens = SqlLexer.Tokenize(".");

        AssertToken(tokens[0], SqlTokenKind.Dot, ".");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
