using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerDoubleQuotedTests
{
    [Test]
    public void Lexer_TokenizesDoubleQuotedIdentifierByDefault()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("\"table name\"");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "\"table name\"");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_DoubleQuotedScannerConsumesDoubledQuotes()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("\"a\"\"b\"");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "\"a\"\"b\"");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesDoubleQuotedStringWhenIdentifierFlagIsDisabled()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowDoubleQuotedStrings = true;
        SqlToken[] tokens = SqlLexer.Tokenize("\"value\"", options);

        AssertToken(tokens[0], SqlTokenKind.StringLiteral, "\"value\"");
    }

    [Test]
    public void Lexer_DoubleQuotedIdentifierFlagWinsWhenBothFlagsAreEnabled()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDoubleQuotedIdentifiers = true;
        options.AllowDoubleQuotedStrings = true;
        SqlToken[] tokens = SqlLexer.Tokenize("\"value\"", options);

        AssertToken(tokens[0], SqlTokenKind.Identifier, "\"value\"");
    }

    [Test]
    public void Lexer_TokenizesUnsupportedDoubleQuotedTextAsBadToken()
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();

        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowDoubleQuotedStrings = false;
        SqlToken[] tokens = SqlLexer.Tokenize("\"value\"", options);

        AssertToken(tokens[0], SqlTokenKind.BadToken, "\"value\"");
    }

    [Test]
    public void Lexer_UnterminatedDoubleQuotedTextConsumesToEnd()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("\"value");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "\"value");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
