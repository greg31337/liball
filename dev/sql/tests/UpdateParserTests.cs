using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class UpdateParserTests
{
    [Test]
    public void StatementParser_AttachesUpdateTargetWithAssignmentsAndWhere()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update or ignore items as i set name = 'a', updated_at = current_timestamp where id = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Update));
        Assert.That(statement.Update, Is.Not.Null);
        SqlUpdateStatement update = statement.Update!;
        Assert.That(update.ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
        Assert.That(update.TableName.GetText(), Is.EqualTo("items"));
        Assert.That(update.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(update.GetAssignmentCount(), Is.EqualTo(2));
        Assert.That(update.GetAssignmentColumnAt(0).GetText(), Is.EqualTo("name"));
        Assert.That(update.GetAssignmentExpressionAt(0).Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(update.GetAssignmentColumnAt(1).GetText(), Is.EqualTo("updated_at"));
        Assert.That(update.GetAssignmentExpressionAt(1).Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(update.HasWhere(), Is.True);
        Assert.That(update.Where!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [TestCase("rollback", SqlConflictActionKind.Rollback)]
    [TestCase("abort", SqlConflictActionKind.Abort)]
    [TestCase("fail", SqlConflictActionKind.Fail)]
    [TestCase("ignore", SqlConflictActionKind.Ignore)]
    [TestCase("replace", SqlConflictActionKind.Replace)]
    public void UpdateParser_ParsesSqliteOrConflictActions(string action, SqlConflictActionKind expected)
    {
        SqlUpdateStatement update = ParseUpdate("update or " + action + " items set name = 'a'");

        Assert.That(update.HasConflictAction(), Is.True);
        Assert.That(update.ConflictAction, Is.EqualTo(expected));
    }

    [Test]
    public void UpdateParser_ParsesReturningClause()
    {
        SqlUpdateStatement update = ParseUpdate("update items set name = 'a' returning id, name as item_name");
        SqlReturningClause returning = update.Returning!;
        SqlResultColumn second = (SqlResultColumn)returning.GetResultColumnAt(1);

        Assert.That(update.HasReturning(), Is.True);
        Assert.That(returning.GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(second.Alias!.GetText(), Is.EqualTo("item_name"));
    }

    [Test]
    public void UpdateParser_ParsesFromClause()
    {
        SqlUpdateStatement update = ParseUpdate("update items set name = source.name from source where source.id = items.id returning *");

        Assert.That(update.HasFrom(), Is.True);
        Assert.That(update.GetFromCount(), Is.EqualTo(1));
        Assert.That(update.GetFromAt(0).Kind, Is.EqualTo(SqlTableReferenceKind.Named));
        Assert.That(update.GetFromAt(0).Name!.GetText(), Is.EqualTo("source"));
        Assert.That(update.HasWhere(), Is.True);
        Assert.That(update.HasReturning(), Is.True);
    }

    [Test]
    public void UpdateParser_ParsesIndexedByTarget()
    {
        SqlUpdateStatement update = ParseUpdate("update items as i indexed by idx_items_name set name = 'a'");

        Assert.That(update.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(update.HasIndexedBy(), Is.True);
        Assert.That(update.IndexedBy!.GetText(), Is.EqualTo("idx_items_name"));
        Assert.That(update.IsNotIndexed, Is.False);
    }

    [Test]
    public void UpdateParser_ParsesNotIndexedTarget()
    {
        SqlUpdateStatement update = ParseUpdate("update items not indexed set name = 'a'");

        Assert.That(update.HasIndexedBy(), Is.False);
        Assert.That(update.IsNotIndexed, Is.True);
    }

    [Test]
    public void UpdateParser_ParsesOrderByAndLimit()
    {
        SqlUpdateStatement update = ParseUpdate("update items set name = 'a' where active = 1 order by id desc limit 5 offset 2");

        Assert.That(update.HasOrderBy(), Is.True);
        Assert.That(update.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(update.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(update.HasLimit(), Is.True);
        Assert.That(update.Limit!.HasOffset(), Is.True);
    }

    [Test]
    public void UpdateParser_ParsesReturningBeforeOrderByAndLimit()
    {
        SqlUpdateStatement update = ParseUpdate("update items set name = 'a' returning id order by id limit 1");

        Assert.That(update.HasReturning(), Is.True);
        Assert.That(update.HasOrderBy(), Is.True);
        Assert.That(update.HasLimit(), Is.True);
    }

    [Test]
    public void UpdateParser_ParsesJoinedFromClause()
    {
        SqlUpdateStatement update = ParseUpdate("update items set name = s.name from source as s join lookup as l on l.id = s.lookup_id where s.id = items.id");
        SqlTableReference source = update.GetFromAt(0);

        Assert.That(source.Name!.GetText(), Is.EqualTo("source"));
        Assert.That(source.Alias!.GetText(), Is.EqualTo("s"));
        Assert.That(source.GetJoinCount(), Is.EqualTo(1));
        Assert.That(source.GetJoinAt(0).Right.Name!.GetText(), Is.EqualTo("lookup"));
    }

    [Test]
    public void UpdateParser_ReportsMissingConflictActionAfterOr()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update or items set name = 'a'", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLUPDATE002"));
        SqlUpdateStatement update = result.Script.GetStatementAt(0).Update!;
        Assert.That(update.ConflictAction, Is.EqualTo(SqlConflictActionKind.None));
        Assert.That(update.TableName.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void UpdateParser_ReportsMissingAssignment()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update items set where id = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLUPDATE006"));
        SqlUpdateStatement update = result.Script.GetStatementAt(0).Update!;
        Assert.That(update.HasAssignments(), Is.False);
        Assert.That(update.HasWhere(), Is.True);
    }

    [Test]
    public void UpdateParser_ReportsMissingByAfterIndexed()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update items indexed idx_items_name set name = 'a'", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLUPDATE011"));
        SqlUpdateStatement update = result.Script.GetStatementAt(0).Update!;
        Assert.That(update.IndexedBy!.GetText(), Is.EqualTo("idx_items_name"));
    }

    [Test]
    public void AstSearch_FindsUpdateTargetTable()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("update items set name = source.name from source", SqlParseOptions.CreateSqlite());

        SqlName[] tables = SqlAstSearch.FindTables(result.Script.GetStatementAt(0));

        Assert.That(tables, Has.Length.EqualTo(2));
        Assert.That(Array.Exists(tables, table => table.GetText() == "items"), Is.True);
        Assert.That(Array.Exists(tables, table => table.GetText() == "source"), Is.True);
    }

    [Test]
    public void AstWalker_WalksUpdatePayloads()
    {
        SqlUpdateStatement update = ParseUpdate("update items indexed by idx_items_name set name = source.name from source where id = 1 returning id order by id limit 2");
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkUpdateStatement(update);

        Assert.That(walker.UpdateStatements, Has.Count.EqualTo(1));
        Assert.That(walker.TableReferences, Has.Count.EqualTo(1));
        Assert.That(walker.ReturningClauses, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Has.Count.GreaterThanOrEqualTo(6));
        Assert.That(walker.Names.Exists(name => name.GetText() == "items"), Is.True);
        Assert.That(walker.Names.Exists(name => name.GetText() == "idx_items_name"), Is.True);
        Assert.That(walker.Names.Exists(name => name.GetText() == "source"), Is.True);
    }

    [Test]
    public void UpdateParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlUpdateParser(null!));
    }

    static SqlUpdateStatement ParseUpdate(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Update!;
    }
}
