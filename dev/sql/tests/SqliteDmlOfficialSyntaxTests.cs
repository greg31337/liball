using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteDmlOfficialSyntaxTests
{
    [Test]
    public void SQLiteDmlSyntax_ParsesInsertSelectUpsertAndReturningShape()
    {
        SqlInsertStatement insert = ParseInsert("insert or replace into main.items as target (id, name) select id, name from staging where true on conflict(id) do update set name = excluded.name where target.name <> excluded.name returning id, name");
        SqlUpsertClause upsert = insert.GetUpsertClauseAt(0);

        Assert.That(insert.ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(insert.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(insert.Alias!.GetText(), Is.EqualTo("target"));
        Assert.That(insert.GetColumnNameCount(), Is.EqualTo(2));
        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Select));
        Assert.That(insert.SourceQuery!.GetSelectCoreAt(0).HasWhere(), Is.True);
        Assert.That(upsert.ActionKind, Is.EqualTo(SqlUpsertActionKind.DoUpdate));
        Assert.That(upsert.GetConflictTargetCount(), Is.EqualTo(1));
        Assert.That(upsert.GetUpdateAssignmentCount(), Is.EqualTo(1));
        Assert.That(upsert.HasUpdateWhere(), Is.True);
        Assert.That(insert.Returning!.GetResultColumnCount(), Is.EqualTo(2));
    }

    [Test]
    public void SQLiteDmlSyntax_ParsesDefaultValuesReturningShape()
    {
        SqlInsertStatement insert = ParseInsert("insert into items default values returning *");
        SqlResultColumn column = (SqlResultColumn)insert.Returning!.GetResultColumnAt(0);

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.DefaultValues));
        Assert.That(insert.HasUpsertClauses(), Is.False);
        Assert.That(column.IsWildcard, Is.True);
    }

    [Test]
    public void SQLiteDmlSyntax_ParsesUpdateFromReturningLimitShape()
    {
        SqlUpdateStatement update = ParseUpdate("update or fail items indexed by idx_items_name set name = s.name from staging as s where s.id = items.id returning id order by id limit 3");

        Assert.That(update.ConflictAction, Is.EqualTo(SqlConflictActionKind.Fail));
        Assert.That(update.IndexedBy!.GetText(), Is.EqualTo("idx_items_name"));
        Assert.That(update.GetAssignmentCount(), Is.EqualTo(1));
        Assert.That(update.GetFromCount(), Is.EqualTo(1));
        Assert.That(update.GetFromAt(0).Alias!.GetText(), Is.EqualTo("s"));
        Assert.That(update.HasWhere(), Is.True);
        Assert.That(update.HasReturning(), Is.True);
        Assert.That(update.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(update.HasLimit(), Is.True);
    }

    [Test]
    public void SQLiteDmlSyntax_ParsesDeleteReturningLimitShape()
    {
        SqlDeleteStatement delete = ParseDelete("delete from main.items not indexed where active = 0 returning id order by id desc limit 2");

        Assert.That(delete.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(delete.IsNotIndexed, Is.True);
        Assert.That(delete.HasWhere(), Is.True);
        Assert.That(delete.Returning!.GetResultColumnCount(), Is.EqualTo(1));
        Assert.That(delete.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(delete.HasLimit(), Is.True);
    }

    [Test]
    public void SQLiteDmlSyntax_ParsesInsertUpdateDeleteScript()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items values (1) returning id; update items set name = 'a' where id = 1; delete from items where id = 2", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(3));
        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(result.Script.GetStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Update));
        Assert.That(result.Script.GetStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Delete));
        Assert.That(result.Script.GetStatementAt(0).Insert, Is.TypeOf<SqlInsertStatement>());
        Assert.That(result.Script.GetStatementAt(1).Update, Is.TypeOf<SqlUpdateStatement>());
        Assert.That(result.Script.GetStatementAt(2).Delete, Is.TypeOf<SqlDeleteStatement>());
    }

    static SqlInsertStatement ParseInsert(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Insert!;
    }

    static SqlUpdateStatement ParseUpdate(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Update!;
    }

    static SqlDeleteStatement ParseDelete(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Delete!;
    }
}
