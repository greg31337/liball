using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectFeatureDiagnosticsTests
{
    [Test]
    public void StatementParser_ReportsUnsupportedReturningForGenericDialect()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items values (1) returning id");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(HasDiagnostic(result, "SQLDIALECT001"), Is.True);
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.HasReturning(), Is.True);
        Assert.That(insert.Returning!.GetResultColumnCount(), Is.EqualTo(1));
    }

    [Test]
    public void StatementParser_ReportsUnsupportedReturningOnUpdateAndDeleteForGenericDialect()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update items set name = 'a' returning id; delete from items returning id");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(CountDiagnostics(result, "SQLDIALECT001"), Is.EqualTo(2));
        SqlUpdateStatement update = result.Script.GetStatementAt(0).Update!;
        SqlDeleteStatement delete = result.Script.GetStatementAt(1).Delete!;
        Assert.That(update.HasReturning(), Is.True);
        Assert.That(delete.HasReturning(), Is.True);
    }

    [Test]
    public void StatementParser_ReportsUnsupportedUpsertForGenericDialect()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items(id) values (1) on conflict(id) do nothing");

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(HasDiagnostic(result, "SQLDIALECT001"), Is.True);
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.HasUpsertClauses(), Is.True);
        Assert.That(insert.GetUpsertClauseAt(0).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
    }

    [Test]
    public void StatementParser_DoesNotReportUnsupportedSqliteFeaturesForSqliteDialect()
    {
        SqlParseResult result = SqlStatementParser.ParseScript(
            "insert into items(id) values (1) on conflict(id) do nothing returning id",
            SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(HasDiagnostic(result, "SQLDIALECT001"), Is.False);
    }

    [Test]
    public void QueryParser_ReportsUnsupportedCommonTableExpressionWhenDisabled()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.Dialect.AllowCommonTableExpressions = false;

        SqlParseResult result = SqlStatementParser.ParseScript("with cte as (select 1) select * from cte", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(HasDiagnostic(result, "SQLDIALECT001"), Is.True);
        SqlQuery query = result.Script.GetStatementAt(0).Query!;
        Assert.That(query.HasCommonTableExpressions(), Is.True);
    }

    [Test]
    public void QueryParser_ReportsUnsupportedWindowFunctionsWhenDisabled()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.Dialect.AllowWindowFunctions = false;

        SqlParseResult result = SqlStatementParser.ParseScript("select sum(amount) over win from items window win as (partition by category)", options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(CountDiagnostics(result, "SQLDIALECT001"), Is.EqualTo(2));
        SqlQuery query = result.Script.GetStatementAt(0).Query!;
        SqlSelectCore core = query.GetSelectCoreAt(0);
        SqlResultColumn column = (SqlResultColumn)core.GetResultColumnAt(0);
        Assert.That(column.Expression, Is.Not.Null);
        Assert.That(column.Expression!.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(core.HasWindows(), Is.True);
    }

    static bool HasDiagnostic(SqlParseResult result, string code)
    {
        return CountDiagnostics(result, code) > 0;
    }

    static int CountDiagnostics(SqlParseResult result, string code)
    {
        int count = 0;
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
