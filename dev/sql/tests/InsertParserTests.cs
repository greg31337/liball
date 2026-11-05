using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class InsertParserTests
{
    [Test]
    public void StatementParser_AttachesInsertTargetWithConflictAction()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert or ignore into main.items as i (id, name) values (1, 'a')", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(statement.Insert, Is.Not.Null);
        SqlInsertStatement insert = statement.Insert!;
        Assert.That(insert.IsReplaceKeyword, Is.False);
        Assert.That(insert.ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
        Assert.That(insert.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(insert.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(insert.GetColumnNameCount(), Is.EqualTo(2));
        Assert.That(insert.GetColumnNameAt(0).GetText(), Is.EqualTo("id"));
        Assert.That(insert.GetColumnNameAt(1).GetText(), Is.EqualTo("name"));
    }

    [TestCase("rollback", SqlConflictActionKind.Rollback)]
    [TestCase("abort", SqlConflictActionKind.Abort)]
    [TestCase("fail", SqlConflictActionKind.Fail)]
    [TestCase("ignore", SqlConflictActionKind.Ignore)]
    [TestCase("replace", SqlConflictActionKind.Replace)]
    public void InsertParser_ParsesSqliteOrConflictActions(string action, SqlConflictActionKind expected)
    {
        SqlInsertStatement insert = ParseInsert("insert or " + action + " into items values (1)");

        Assert.That(insert.HasConflictAction(), Is.True);
        Assert.That(insert.ConflictAction, Is.EqualTo(expected));
    }

    [Test]
    public void InsertParser_MapsReplaceKeywordToReplaceConflictAction()
    {
        SqlInsertStatement insert = ParseInsert("replace into items values (1)");

        Assert.That(insert.IsReplaceKeyword, Is.True);
        Assert.That(insert.ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(insert.TableName.GetText(), Is.EqualTo("items"));
        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Values));
    }

    [Test]
    public void InsertParser_ParsesValuesSourceRows()
    {
        SqlInsertStatement insert = ParseInsert("insert into items (id, name) values (1, 'a'), (2, 'b')");

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Values));
        Assert.That(insert.GetValueRowCount(), Is.EqualTo(2));
        Assert.That(insert.GetValueRowAt(0), Has.Length.EqualTo(2));
        Assert.That(insert.GetValueRowAt(0)[0].Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(insert.GetValueRowAt(1)[1].Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(insert.HasSourceQuery(), Is.False);
    }

    [Test]
    public void InsertParser_ParsesSelectSource()
    {
        SqlInsertStatement insert = ParseInsert("insert into archive select id, name from items where active = 1");

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Select));
        Assert.That(insert.HasValues(), Is.False);
        Assert.That(insert.HasSourceQuery(), Is.True);
        Assert.That(insert.SourceQuery!.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(insert.SourceQuery.GetSelectCoreAt(0).HasFrom(), Is.True);
        Assert.That(insert.SourceQuery.GetSelectCoreAt(0).HasWhere(), Is.True);
    }

    [Test]
    public void InsertParser_ParsesWithSelectSource()
    {
        SqlInsertStatement insert = ParseInsert("insert into archive with cte as (select 1 as id) select id from cte");

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Select));
        Assert.That(insert.SourceQuery, Is.Not.Null);
        Assert.That(insert.SourceQuery!.HasCommonTableExpressions(), Is.True);
        Assert.That(insert.SourceQuery.GetCommonTableExpressionAt(0).Name.GetText(), Is.EqualTo("cte"));
    }

    [Test]
    public void InsertParser_ParsesDefaultValuesSource()
    {
        SqlInsertStatement insert = ParseInsert("insert into items default values");

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.DefaultValues));
        Assert.That(insert.HasValues(), Is.False);
        Assert.That(insert.HasSourceQuery(), Is.False);
    }

    [Test]
    public void InsertParser_ParsesUpsertDoNothing()
    {
        SqlInsertStatement insert = ParseInsert("insert into items (id, name) values (1, 'a') on conflict(id) do nothing");
        SqlUpsertClause upsert = insert.GetUpsertClauseAt(0);

        Assert.That(insert.HasUpsertClauses(), Is.True);
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(upsert.ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
        Assert.That(upsert.GetConflictTargetCount(), Is.EqualTo(1));
        Assert.That(upsert.GetConflictTargetAt(0).Expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(upsert.HasUpdateAssignments(), Is.False);
    }

    [Test]
    public void InsertParser_ParsesUpsertDoUpdateWithWhereClauses()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1, 'a') on conflict(id) where active = 1 do update set name = excluded.name, updated_at = current_timestamp where items.name <> excluded.name");
        SqlUpsertClause upsert = insert.GetUpsertClauseAt(0);

        Assert.That(upsert.ActionKind, Is.EqualTo(SqlUpsertActionKind.DoUpdate));
        Assert.That(upsert.HasConflictWhere(), Is.True);
        Assert.That(upsert.ConflictWhere!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(upsert.GetUpdateAssignmentCount(), Is.EqualTo(2));
        Assert.That(upsert.GetUpdateColumnAt(0).GetText(), Is.EqualTo("name"));
        Assert.That(upsert.GetUpdateExpressionAt(0).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(upsert.GetUpdateColumnAt(1).GetText(), Is.EqualTo("updated_at"));
        Assert.That(upsert.GetUpdateExpressionAt(1).Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(upsert.HasUpdateWhere(), Is.True);
        Assert.That(upsert.UpdateWhere!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [Test]
    public void InsertParser_ParsesMultipleUpsertClauses()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1, 'a') on conflict(id) do update set name = excluded.name on conflict do nothing");

        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(2));
        Assert.That(insert.GetUpsertClauseAt(0).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoUpdate));
        Assert.That(insert.GetUpsertClauseAt(0).GetUpdateAssignmentCount(), Is.EqualTo(1));
        Assert.That(insert.GetUpsertClauseAt(1).HasConflictTargets(), Is.False);
        Assert.That(insert.GetUpsertClauseAt(1).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
    }

    [Test]
    public void InsertParser_ParsesUpsertAfterSelectSource()
    {
        SqlInsertStatement insert = ParseInsert("insert into archive select id, name from items on conflict(id) do nothing");

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Select));
        Assert.That(insert.SourceQuery, Is.Not.Null);
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(insert.GetUpsertClauseAt(0).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
    }

    [Test]
    public void InsertParser_ParsesReturningResultColumns()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1) returning id, name as item_name, items.*");
        SqlReturningClause returning = insert.Returning!;
        SqlResultColumn first = (SqlResultColumn)returning.GetResultColumnAt(0);
        SqlResultColumn second = (SqlResultColumn)returning.GetResultColumnAt(1);
        SqlResultColumn third = (SqlResultColumn)returning.GetResultColumnAt(2);

        Assert.That(insert.HasReturning(), Is.True);
        Assert.That(returning.GetResultColumnCount(), Is.EqualTo(3));
        Assert.That(first.Expression!.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(second.Alias!.GetText(), Is.EqualTo("item_name"));
        Assert.That(third.IsWildcard, Is.True);
        Assert.That(third.Qualifier!.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void InsertParser_ParsesReturningAfterUpsert()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1, 'a') on conflict(id) do update set name = excluded.name returning id, name");

        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(insert.HasReturning(), Is.True);
        Assert.That(insert.Returning!.GetResultColumnCount(), Is.EqualTo(2));
    }

    [Test]
    public void InsertParser_ParsesReturningAfterDefaultValues()
    {
        SqlInsertStatement insert = ParseInsert("insert into items default values returning *");
        SqlResultColumn column = (SqlResultColumn)insert.Returning!.GetResultColumnAt(0);

        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.DefaultValues));
        Assert.That(column.IsWildcard, Is.True);
    }

    [Test]
    public void InsertParser_ReportsMissingConflictActionAfterOr()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert or into items values (1)", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINSERT003"));
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.ConflictAction, Is.EqualTo(SqlConflictActionKind.None));
        Assert.That(insert.TableName.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void InsertParser_ReportsMissingSource()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINSERT008"));
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.SourceKind, Is.EqualTo(SqlInsertSourceKind.Unknown));
    }

    [Test]
    public void InsertParser_ReportsMissingDoInUpsert()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items values (1) on conflict(id) nothing", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINSERT012"));
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.GetUpsertClauseCount(), Is.EqualTo(1));
        Assert.That(insert.GetUpsertClauseAt(0).ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
    }

    [Test]
    public void InsertParser_ReportsMissingReturningExpression()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items values (1) returning", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINSERT020"));
        SqlInsertStatement insert = result.Script.GetStatementAt(0).Insert!;
        Assert.That(insert.HasReturning(), Is.True);
        Assert.That(insert.Returning!.HasResultColumns(), Is.False);
    }

    [Test]
    public void AstSearch_FindsInsertTargetTable()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("insert into items values (1)", SqlParseOptions.CreateSqlite());

        SqlName[] tables = SqlAstSearch.FindTables(result.Script.GetStatementAt(0));

        Assert.That(tables, Has.Length.EqualTo(1));
        Assert.That(tables[0].GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void InsertParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlInsertParser(null!));
    }

    [Test]
    public void AstWalker_WalksInsertValuesAndSourceQuery()
    {
        SqlInsertStatement valuesInsert = ParseInsert("insert into items values (1)");
        SqlInsertStatement selectInsert = ParseInsert("insert into archive select id from items");
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkInsertStatement(valuesInsert);
        walker.WalkInsertStatement(selectInsert);

        Assert.That(walker.InsertStatements, Has.Count.EqualTo(2));
        Assert.That(walker.Expressions, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(walker.Queries, Has.Count.EqualTo(1));
        Assert.That(walker.Names.Exists(name => name.GetText() == "archive"), Is.True);
    }

    [Test]
    public void AstWalker_WalksUpsertPayloads()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1) on conflict(id) where active = 1 do update set name = excluded.name where id > 0");
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkInsertStatement(insert);

        Assert.That(walker.UpsertClauses, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Has.Count.GreaterThanOrEqualTo(5));
        Assert.That(walker.Names.Exists(name => name.GetText() == "name"), Is.True);
    }

    [Test]
    public void AstWalker_WalksReturningPayloads()
    {
        SqlInsertStatement insert = ParseInsert("insert into items values (1) returning id, name as item_name");
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkInsertStatement(insert);

        Assert.That(walker.ReturningClauses, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(walker.Names.Exists(name => name.GetText() == "item_name"), Is.True);
    }

    static SqlInsertStatement ParseInsert(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Insert!;
    }
}
