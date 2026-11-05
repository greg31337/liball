using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerStringLiteralTests
{
    [Test]
    public void Lexer_TokenizesSingleQuotedStringByDefault()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("'value'");

        AssertToken(tokens[0], SqlTokenKind.StringLiteral, "'value'");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesEmptySingleQuotedString()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("''");

        AssertToken(tokens[0], SqlTokenKind.StringLiteral, "''");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_SingleQuotedStringConsumesDoubledQuote()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("'a''b'");

        AssertToken(tokens[0], SqlTokenKind.StringLiteral, "'a''b'");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesUnsupportedSingleQuotedStringAsBadToken()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowSingleQuotedStrings = false;
        SqlToken[] tokens = SqlLexer.Tokenize("'value'", options);

        AssertToken(tokens[0], SqlTokenKind.BadToken, "'value'");
    }

    [Test]
    public void Lexer_UnterminatedSingleQuotedStringConsumesToEnd()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("'value");

        AssertToken(tokens[0], SqlTokenKind.StringLiteral, "'value");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
