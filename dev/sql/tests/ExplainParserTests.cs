using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExplainParserTests
{
    [Test]
    public void StatementParser_AttachesExplainSelectPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("explain select * from items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Explain));
        Assert.That(statement.Explain, Is.Not.Null);
        Assert.That(statement.Query, Is.Not.Null);
        SqlExplainStatement explain = statement.Explain!;
        Assert.That(explain.IsQueryPlan, Is.False);
        Assert.That(explain.HasStatement(), Is.True);
        Assert.That(explain.Statement!.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(explain.Statement.Query, Is.SameAs(statement.Query));
    }

    [Test]
    public void ExplainParser_ParsesQueryPlanPrefix()
    {
        SqlExplainStatement explain = ParseExplain("explain query plan select id from items where id = ?");

        Assert.That(explain.IsQueryPlan, Is.True);
        Assert.That(explain.Statement!.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(explain.Statement.Query, Is.Not.Null);
    }

    [Test]
    public void ExplainParser_ParsesDmlStatementPrefix()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("explain insert into items values (1)", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        SqlStatement statement = result.Script.GetStatementAt(0);
        SqlExplainStatement explain = statement.Explain!;
        Assert.That(statement.Query, Is.Null);
        Assert.That(explain.Statement!.Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(explain.Statement.Insert, Is.TypeOf<SqlInsertStatement>());
    }

    [Test]
    public void ExplainParser_ReportsMissingPlanButParsesStatement()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("explain query select 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPLAIN002"));
        SqlExplainStatement explain = result.Script.GetStatementAt(0).Explain!;
        Assert.That(explain.IsQueryPlan, Is.False);
        Assert.That(explain.Statement!.Kind, Is.EqualTo(SqlStatementKind.Select));
    }

    [Test]
    public void ExplainParser_ReportsMissingStatement()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("explain query plan", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPLAIN003"));
        Assert.That(result.Script.GetStatementAt(0).Explain, Is.TypeOf<SqlExplainStatement>());
    }

    [Test]
    public void ExplainParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlExplainParser(null!));
    }

    static SqlExplainStatement ParseExplain(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Explain!;
    }
}
