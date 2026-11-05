using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerSqliteSpecificTests
{
    [Test]
    public void Lexer_TokenizesSqliteSpecificLiteralsIdentifiersAndKeywordsTogether()
    {
        string sql = "[weird name] `bt``id` X'53514C' 0x2A $name PRAGMA STRICT RETURNING";
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlLexer lexer = SqlLexer.Create(sql, options);
        SqlToken[] tokens = lexer.LexTokens();

        AssertToken(tokens[0], SqlTokenKind.Identifier, "[weird name]");
        AssertToken(tokens[2], SqlTokenKind.Identifier, "`bt``id`");
        AssertToken(tokens[4], SqlTokenKind.BlobLiteral, "X'53514C'");
        AssertToken(tokens[6], SqlTokenKind.NumericLiteral, "0x2A");
        AssertToken(tokens[8], SqlTokenKind.Parameter, "$name");
        AssertToken(tokens[10], SqlTokenKind.Keyword, "PRAGMA");
        AssertToken(tokens[12], SqlTokenKind.Keyword, "STRICT");
        AssertToken(tokens[14], SqlTokenKind.Keyword, "RETURNING");
        AssertToken(tokens[15], SqlTokenKind.EndOfFile, "");
        Assert.That(lexer.Diagnostics.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void Lexer_TokenizesSqliteCaseInsensitiveSpecificKeywords()
    {
        SqlDialectOptions options = SqliteDialect.CreateOptions();
        SqlToken[] tokens = SqlLexer.Tokenize("pragma strict returning", options);

        AssertToken(tokens[0], SqlTokenKind.Keyword, "pragma");
        AssertToken(tokens[2], SqlTokenKind.Keyword, "strict");
        AssertToken(tokens[4], SqlTokenKind.Keyword, "returning");
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
