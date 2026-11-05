using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerBacktickIdentifierTests
{
    [Test]
    public void Lexer_TokenizesBacktickIdentifierWhenDialectAllowsIt()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("`table name`", options);

        AssertToken(tokens[0], SqlTokenKind.Identifier, "`table name`");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_BacktickIdentifierConsumesDoubledBacktick()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("`a``b`", options);

        AssertToken(tokens[0], SqlTokenKind.Identifier, "`a``b`");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_UnterminatedBacktickIdentifierConsumesToEnd()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("`table", options);

        AssertToken(tokens[0], SqlTokenKind.Identifier, "`table");
        AssertToken(tokens[1], SqlTokenKind.EndOfFile, "");
    }

    [Test]
    public void Lexer_TokenizesBackticksAsBadTokensWhenDialectDisallowsBacktickIdentifiers()
    {
        SqlToken[] tokens = SqlLexer.Tokenize("`abc`");

        AssertToken(tokens[0], SqlTokenKind.BadToken, "`");
        AssertToken(tokens[1], SqlTokenKind.Identifier, "abc");
        AssertToken(tokens[2], SqlTokenKind.BadToken, "`");
        AssertToken(tokens[3], SqlTokenKind.EndOfFile, "");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
