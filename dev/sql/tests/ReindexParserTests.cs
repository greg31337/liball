using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ReindexParserTests
{
    [Test]
    public void StatementParser_AttachesReindexTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("reindex main.idx_items_name", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Reindex));
        Assert.That(statement.Reindex, Is.Not.Null);
        SqlReindexStatement reindex = statement.Reindex!;
        Assert.That(reindex.HasTargetName(), Is.True);
        Assert.That(reindex.TargetName!.GetText(), Is.EqualTo("main.idx_items_name"));
    }

    [Test]
    public void ReindexParser_ParsesBareAndQuotedTargets()
    {
        SqlReindexStatement bare = ParseReindex("reindex");
        SqlReindexStatement quoted = ParseReindex("reindex \"unicode nocase\"");

        Assert.That(bare.HasTargetName(), Is.False);
        Assert.That(bare.TargetName, Is.Null);
        Assert.That(quoted.HasTargetName(), Is.True);
        Assert.That(quoted.TargetName!.GetText(), Is.EqualTo("unicode nocase"));
        Assert.That(quoted.TargetName.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void ReindexParser_ReportsMalformedTarget()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("reindex .", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLREINDEX002"));
        Assert.That(result.Script.GetStatementAt(0).Reindex, Is.TypeOf<SqlReindexStatement>());
    }

    [Test]
    public void ReindexParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlReindexParser(null!));
    }

    static SqlReindexStatement ParseReindex(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Reindex!;
    }
}
