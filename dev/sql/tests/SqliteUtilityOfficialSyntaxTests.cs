using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteUtilityOfficialSyntaxTests
{
    [Test]
    public void SQLiteUtilitySyntax_ParsesPragmaAnalyzeVacuumAndReindexShape()
    {
        SqlParseResult result = ParseScript("pragma main.cache_size = -2000; pragma table_info(items); analyze main.items; vacuum temp into 'backup.db'; reindex main.idx_items_name");

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(5));
        SqlPragmaStatement cache = result.Script.GetStatementAt(0).Pragma!;
        SqlPragmaStatement tableInfo = result.Script.GetStatementAt(1).Pragma!;
        SqlAnalyzeStatement analyze = result.Script.GetStatementAt(2).Analyze!;
        SqlVacuumStatement vacuum = result.Script.GetStatementAt(3).Vacuum!;
        SqlReindexStatement reindex = result.Script.GetStatementAt(4).Reindex!;
        Assert.That(cache.Name.GetText(), Is.EqualTo("main.cache_size"));
        Assert.That(cache.ValueKind, Is.EqualTo(SqlPragmaValueKind.Equals));
        Assert.That(cache.Value!.Kind, Is.EqualTo(SqlExpressionKind.Unary));
        Assert.That(tableInfo.Name.GetText(), Is.EqualTo("table_info"));
        Assert.That(tableInfo.ValueKind, Is.EqualTo(SqlPragmaValueKind.Parenthesized));
        Assert.That(analyze.TargetName!.GetText(), Is.EqualTo("main.items"));
        Assert.That(vacuum.DatabaseName!.GetText(), Is.EqualTo("temp"));
        Assert.That(vacuum.IntoExpression!.Literal!.Value, Is.EqualTo("backup.db"));
        Assert.That(reindex.TargetName!.GetText(), Is.EqualTo("main.idx_items_name"));
    }

    [Test]
    public void SQLiteUtilitySyntax_ParsesAttachDetachTransactionsAndSavepointsShape()
    {
        SqlParseResult result = ParseScript("attach database 'extra.db' as aux; detach database aux; begin exclusive transaction; commit transaction; end; rollback transaction; rollback to savepoint sp1; savepoint sp2; release savepoint sp2");

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(9));
        SqlAttachStatement attach = result.Script.GetStatementAt(0).Attach!;
        SqlDetachStatement detach = result.Script.GetStatementAt(1).Detach!;
        SqlTransactionStatement begin = result.Script.GetStatementAt(2).Transaction!;
        SqlTransactionStatement commit = result.Script.GetStatementAt(3).Transaction!;
        SqlTransactionStatement end = result.Script.GetStatementAt(4).Transaction!;
        SqlTransactionStatement rollback = result.Script.GetStatementAt(5).Transaction!;
        SqlTransactionStatement rollbackTo = result.Script.GetStatementAt(6).Transaction!;
        SqlSavepointStatement savepoint = result.Script.GetStatementAt(7).Savepoint!;
        SqlReleaseStatement release = result.Script.GetStatementAt(8).Release!;
        Assert.That(attach.DatabaseExpression!.Literal!.Value, Is.EqualTo("extra.db"));
        Assert.That(attach.SchemaName!.GetText(), Is.EqualTo("aux"));
        Assert.That(detach.SchemaName!.GetText(), Is.EqualTo("aux"));
        Assert.That(begin.Mode, Is.EqualTo(SqlTransactionMode.Exclusive));
        Assert.That(begin.HasTransactionKeyword, Is.True);
        Assert.That(commit.ActionKind, Is.EqualTo(SqlTransactionActionKind.Commit));
        Assert.That(commit.HasTransactionKeyword, Is.True);
        Assert.That(end.ActionKind, Is.EqualTo(SqlTransactionActionKind.End));
        Assert.That(rollback.ActionKind, Is.EqualTo(SqlTransactionActionKind.Rollback));
        Assert.That(rollbackTo.IsToSavepoint, Is.True);
        Assert.That(rollbackTo.SavepointName!.GetText(), Is.EqualTo("sp1"));
        Assert.That(savepoint.Name!.GetText(), Is.EqualTo("sp2"));
        Assert.That(release.HasSavepointKeyword, Is.True);
        Assert.That(release.Name!.GetText(), Is.EqualTo("sp2"));
    }

    [Test]
    public void SQLiteUtilitySyntax_ParsesExplainPrefixesShape()
    {
        SqlParseResult result = ParseScript("explain query plan select id from items where id = ?; explain insert into items values (1)");

        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(2));
        SqlExplainStatement queryPlan = result.Script.GetStatementAt(0).Explain!;
        SqlExplainStatement insertExplain = result.Script.GetStatementAt(1).Explain!;
        Assert.That(queryPlan.IsQueryPlan, Is.True);
        Assert.That(queryPlan.Statement!.Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(result.Script.GetStatementAt(0).Query, Is.SameAs(queryPlan.Statement.Query));
        Assert.That(insertExplain.IsQueryPlan, Is.False);
        Assert.That(insertExplain.Statement!.Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(insertExplain.Statement.Insert, Is.TypeOf<SqlInsertStatement>());
    }

    [Test]
    public void SQLiteUtilitySyntax_ParsesPragmaTableValuedFunctionInSelect()
    {
        SqlParseResult result = ParseScript("select name from pragma_table_info('items') as info where pk = 1");
        SqlSelectCore core = result.Script.GetStatementAt(0).Query!.GetSelectCoreAt(0);
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(core.HasFrom(), Is.True);
        Assert.That(table.Kind, Is.EqualTo(SqlTableReferenceKind.Function));
        Assert.That(table.FunctionExpression, Is.Not.Null);
        Assert.That(table.FunctionExpression!.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(table.Alias!.GetText(), Is.EqualTo("info"));
        Assert.That(core.HasWhere(), Is.True);
    }

    static SqlParseResult ParseScript(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result;
    }
}
