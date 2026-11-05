using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class UtilityCompletionTests
{
    [Test]
    public void AttachAndDetach_RetainOptionalDatabaseKeyword()
    {
        SqlParseResult result = Parse("attach 'one.db' as one; attach database 'two.db' as two; detach one; detach database two");
        SqlAttachStatement shortAttach = result.Script.GetStatementAt(0).Attach!;
        SqlAttachStatement databaseAttach = result.Script.GetStatementAt(1).Attach!;
        SqlDetachStatement shortDetach = result.Script.GetStatementAt(2).Detach!;
        SqlDetachStatement databaseDetach = result.Script.GetStatementAt(3).Detach!;

        Assert.That(shortAttach.HasDatabaseKeyword, Is.False);
        Assert.That(databaseAttach.HasDatabaseKeyword, Is.True);
        Assert.That(shortDetach.HasDatabaseKeyword, Is.False);
        Assert.That(databaseDetach.HasDatabaseKeyword, Is.True);
        Assert.That(Generate(result, 0), Is.EqualTo("ATTACH 'one.db' AS one"));
        Assert.That(Generate(result, 1), Is.EqualTo("ATTACH DATABASE 'two.db' AS two"));
        Assert.That(Generate(result, 2), Is.EqualTo("DETACH one"));
        Assert.That(Generate(result, 3), Is.EqualTo("DETACH DATABASE two"));
    }

    [Test]
    public void End_HasDistinctStatementAndTransactionKinds()
    {
        SqlParseResult result = Parse("commit transaction; end transaction");
        SqlStatement commit = result.Script.GetStatementAt(0);
        SqlStatement end = result.Script.GetStatementAt(1);

        Assert.That(commit.Kind, Is.EqualTo(SqlStatementKind.Commit));
        Assert.That(commit.Transaction!.ActionKind, Is.EqualTo(SqlTransactionActionKind.Commit));
        Assert.That(end.Kind, Is.EqualTo(SqlStatementKind.End));
        Assert.That(end.Transaction!.ActionKind, Is.EqualTo(SqlTransactionActionKind.End));
        Assert.That(Generate(result, 1), Is.EqualTo("END TRANSACTION"));
    }

    [Test]
    public void Rollback_RetainsOptionalSavepointKeywordInTypedTarget()
    {
        SqlParseResult result = Parse("rollback to first_point; rollback transaction to savepoint second_point");
        SqlTransactionStatement shortRollback = result.Script.GetStatementAt(0).Transaction!;
        SqlTransactionStatement savepointRollback = result.Script.GetStatementAt(1).Transaction!;

        Assert.That(shortRollback.RollbackTarget, Is.Not.Null);
        Assert.That(shortRollback.RollbackTarget!.HasSavepointKeyword, Is.False);
        Assert.That(shortRollback.RollbackTarget.Name.GetText(), Is.EqualTo("first_point"));
        Assert.That(savepointRollback.RollbackTarget, Is.Not.Null);
        Assert.That(savepointRollback.RollbackTarget!.HasSavepointKeyword, Is.True);
        Assert.That(savepointRollback.RollbackTarget.Name.GetText(), Is.EqualTo("second_point"));
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkTransactionStatement(shortRollback);
        Assert.That(walker.Names, Does.Contain(shortRollback.RollbackTarget.Name));
        Assert.That(Generate(result, 0), Is.EqualTo("ROLLBACK TO first_point"));
        Assert.That(Generate(result, 1), Is.EqualTo("ROLLBACK TRANSACTION TO SAVEPOINT second_point"));
    }

    [Test]
    public void Explain_ExposesOrdinaryAndQueryPlanKinds()
    {
        SqlParseResult result = Parse("explain select 1; explain query plan select id from items");
        SqlExplainStatement explain = result.Script.GetStatementAt(0).Explain!;
        SqlExplainStatement queryPlan = result.Script.GetStatementAt(1).Explain!;

        Assert.That(explain.Kind, Is.EqualTo(SqlExplainKind.Explain));
        Assert.That(explain.IsQueryPlan, Is.False);
        Assert.That(queryPlan.Kind, Is.EqualTo(SqlExplainKind.QueryPlan));
        Assert.That(queryPlan.IsQueryPlan, Is.True);
        Assert.That(Generate(result, 0), Is.EqualTo("EXPLAIN SELECT 1"));
        Assert.That(Generate(result, 1), Is.EqualTo("EXPLAIN QUERY PLAN SELECT id FROM items"));
    }

    static SqlParseResult Parse(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result;
    }

    static string Generate(SqlParseResult result, int index)
    {
        return SqlGenerator.GenerateStatement(result.Script.GetStatementAt(index));
    }
}
