using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerBlobLiteralTests
{
    [Test]
    public void Lexer_TokenizesUppercaseSqliteBlobLiteral()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("X'53514C'", options);

        AssertToken(tokens[0], SqlTokenKind.BlobLiteral, "X'53514C'");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesLowercaseSqliteBlobLiteral()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("x'00ff'", options);

        AssertToken(tokens[0], SqlTokenKind.BlobLiteral, "x'00ff'");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_UnterminatedBlobLiteralConsumesToEnd()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("X'53514C", options);

        AssertToken(tokens[0], SqlTokenKind.BlobLiteral, "X'53514C");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_GenericDialectTokenizesBlobSyntaxAsIdentifierAndString()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("X'53514C'");

        AssertToken(tokens[0], SqlTokenKind.Identifier, "X");
        AssertToken(tokens[1], SqlTokenKind.StringLiteral, "'53514C'");
        AssertToken(tokens[2], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_BlobLiteralConsumesDoubledQuoteAsPartOfToken()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("X'ab''cd'", options);

        AssertToken(tokens[0], SqlTokenKind.BlobLiteral, "X'ab''cd'");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
