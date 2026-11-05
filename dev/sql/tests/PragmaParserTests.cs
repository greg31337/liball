using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PragmaParserTests
{
    [Test]
    public void StatementParser_AttachesPragmaExpressionPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("pragma main.cache_size = -2000", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Pragma));
        Assert.That(statement.Pragma, Is.Not.Null);
        SqlPragmaStatement pragma = statement.Pragma!;
        Assert.That(pragma.Name.GetText(), Is.EqualTo("main.cache_size"));
        Assert.That(pragma.ValueKind, Is.EqualTo(SqlPragmaValueKind.Equals));
        Assert.That(pragma.HasValue(), Is.True);
        Assert.That(pragma.Value!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
    }

    [Test]
    public void PragmaParser_ParsesBareAndParenthesizedPragmas()
    {
        SqlPragmaStatement bare = ParsePragma("pragma foreign_keys");
        SqlPragmaStatement parenthesized = ParsePragma("pragma table_info(items)");

        Assert.That(bare.Name.GetText(), Is.EqualTo("foreign_keys"));
        Assert.That(bare.ValueKind, Is.EqualTo(SqlPragmaValueKind.None));
        Assert.That(bare.HasValue(), Is.False);
        Assert.That(parenthesized.Name.GetText(), Is.EqualTo("table_info"));
        Assert.That(parenthesized.ValueKind, Is.EqualTo(SqlPragmaValueKind.Parenthesized));
        Assert.That(parenthesized.Value!.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
    }

    [Test]
    public void PragmaParser_ReportsMissingName()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("pragma", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLPRAGMA002"));
        Assert.That(result.Script.GetStatementAt(0).Pragma, Is.TypeOf<SqlPragmaStatement>());
    }

    [Test]
    public void PragmaParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlPragmaParser(null!));
    }

    static SqlPragmaStatement ParsePragma(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Pragma!;
    }
}
