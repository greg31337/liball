using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LexerCommonTokenTests
{
    [Test]
    public void Lexer_TokenizesCommonSqlSequenceWithTriviaTokens()
    {
        string sql = "-- lead\nSELECT col_1, 'x''y' FROM table1 WHERE id = ?1;\n/* done */";
        SqlLexer lexer = SqlLexer.Create(sql);
        SqlToken[] tokens = lexer.LexTokens();

        AssertToken(tokens[0], SqlTokenKind.LineComment, 0, 7, "-- lead");
        AssertToken(tokens[1], SqlTokenKind.Whitespace, 7, 1, "\n");
        AssertToken(tokens[2], SqlTokenKind.Keyword, 8, 6, "SELECT");
        AssertToken(tokens[3], SqlTokenKind.Whitespace, 14, 1, " ");
        AssertToken(tokens[4], SqlTokenKind.Identifier, 15, 5, "col_1");
        AssertToken(tokens[5], SqlTokenKind.Comma, 20, 1, ",");
        AssertToken(tokens[6], SqlTokenKind.Whitespace, 21, 1, " ");
        AssertToken(tokens[7], SqlTokenKind.StringLiteral, 22, 6, "'x''y'");
        AssertToken(tokens[8], SqlTokenKind.Whitespace, 28, 1, " ");
        AssertToken(tokens[9], SqlTokenKind.Keyword, 29, 4, "FROM");
        AssertToken(tokens[10], SqlTokenKind.Whitespace, 33, 1, " ");
        AssertToken(tokens[11], SqlTokenKind.Identifier, 34, 6, "table1");
        AssertToken(tokens[12], SqlTokenKind.Whitespace, 40, 1, " ");
        AssertToken(tokens[13], SqlTokenKind.Keyword, 41, 5, "WHERE");
        AssertToken(tokens[14], SqlTokenKind.Whitespace, 46, 1, " ");
        AssertToken(tokens[15], SqlTokenKind.Identifier, 47, 2, "id");
        AssertToken(tokens[16], SqlTokenKind.Whitespace, 49, 1, " ");
        AssertToken(tokens[17], SqlTokenKind.Equal, 50, 1, "=");
        AssertToken(tokens[18], SqlTokenKind.Whitespace, 51, 1, " ");
        AssertToken(tokens[19], SqlTokenKind.Parameter, 52, 2, "?1");
        AssertToken(tokens[20], SqlTokenKind.Semicolon, 54, 1, ";");
        AssertToken(tokens[21], SqlTokenKind.Whitespace, 55, 1, "\n");
        AssertToken(tokens[22], SqlTokenKind.BlockComment, 56, 10, "/* done */");
        AssertToken(tokens[23], SqlTokenKind.EndOfFile, 66, 0, "");
        Assert.That(lexer.Diagnostics.GetCount(), Is.EqualTo(0));
    }

    static void AssertToken(SqlToken token, SqlTokenKind kind, int start, int length, string text)
    {
        Assert.That(token.Kind, Is.EqualTo(kind));
        Assert.That(token.Span.Start, Is.EqualTo(start));
        Assert.That(token.Span.Length, Is.EqualTo(length));
        Assert.That(token.Text, Is.EqualTo(text));
    }
}
