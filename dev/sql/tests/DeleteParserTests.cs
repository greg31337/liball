using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DeleteParserTests
{
    [Test]
    public void StatementParser_AttachesDeleteTargetWithWhereAndReturning()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("delete from items as i where id = 1 returning id, name as item_name", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Delete));
        Assert.That(statement.Delete, Is.Not.Null);
        SqlDeleteStatement delete = statement.Delete!;
        Assert.That(delete.TableName.GetText(), Is.EqualTo("items"));
        Assert.That(delete.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(delete.HasWhere(), Is.True);
        Assert.That(delete.Where!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(delete.HasReturning(), Is.True);
        Assert.That(delete.Returning!.GetResultColumnCount(), Is.EqualTo(2));
        SqlResultColumn second = (SqlResultColumn)delete.Returning.GetResultColumnAt(1);
        Assert.That(second.Alias!.GetText(), Is.EqualTo("item_name"));
    }

    [Test]
    public void DeleteParser_ParsesQualifiedTableName()
    {
        SqlDeleteStatement delete = ParseDelete("delete from main.items where active = 0");

        Assert.That(delete.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(delete.HasWhere(), Is.True);
    }

    [Test]
    public void DeleteParser_ParsesReturningWildcard()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items returning *");
        SqlResultColumn column = (SqlResultColumn)delete.Returning!.GetResultColumnAt(0);

        Assert.That(column.IsWildcard, Is.True);
    }

    [Test]
    public void DeleteParser_ParsesIndexedByTarget()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items as i indexed by idx_items_id where id = 1");

        Assert.That(delete.Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(delete.HasIndexedBy(), Is.True);
        Assert.That(delete.IndexedBy!.GetText(), Is.EqualTo("idx_items_id"));
        Assert.That(delete.IsNotIndexed, Is.False);
        Assert.That(delete.HasWhere(), Is.True);
    }

    [Test]
    public void DeleteParser_ParsesNotIndexedTarget()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items not indexed where id = 1");

        Assert.That(delete.HasIndexedBy(), Is.False);
        Assert.That(delete.IsNotIndexed, Is.True);
    }

    [Test]
    public void DeleteParser_ParsesOrderByAndLimit()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items where active = 0 order by created_at desc limit 10 offset 5");

        Assert.That(delete.HasOrderBy(), Is.True);
        Assert.That(delete.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(delete.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(delete.HasLimit(), Is.True);
        Assert.That(delete.Limit!.HasOffset(), Is.True);
    }

    [Test]
    public void DeleteParser_ParsesReturningBeforeOrderByAndLimit()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items returning id order by id limit 1");

        Assert.That(delete.HasReturning(), Is.True);
        Assert.That(delete.HasOrderBy(), Is.True);
        Assert.That(delete.HasLimit(), Is.True);
    }

    [Test]
    public void DeleteParser_ReportsMissingFrom()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("delete items where id = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDELETE002"));
        SqlDeleteStatement delete = result.Script.GetStatementAt(0).Delete!;
        Assert.That(delete.TableName.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void DeleteParser_ReportsMissingReturningExpression()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("delete from items returning", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDELETE006"));
        SqlDeleteStatement delete = result.Script.GetStatementAt(0).Delete!;
        Assert.That(delete.HasReturning(), Is.True);
        Assert.That(delete.Returning!.HasResultColumns(), Is.False);
    }

    [Test]
    public void DeleteParser_ReportsMissingByAfterIndexed()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("delete from items indexed idx_items_id where id = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDELETE007"));
        SqlDeleteStatement delete = result.Script.GetStatementAt(0).Delete!;
        Assert.That(delete.IndexedBy!.GetText(), Is.EqualTo("idx_items_id"));
    }

    [Test]
    public void AstSearch_FindsDeleteTargetTable()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("delete from items where id = 1", SqlParseOptions.CreateSqlite());

        SqlName[] tables = SqlAstSearch.FindTables(result.Script.GetStatementAt(0));

        Assert.That(tables, Has.Length.EqualTo(1));
        Assert.That(tables[0].GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void AstWalker_WalksDeletePayloads()
    {
        SqlDeleteStatement delete = ParseDelete("delete from items where id = 1 returning id order by id limit 2");
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkDeleteStatement(delete);

        Assert.That(walker.DeleteStatements, Has.Count.EqualTo(1));
        Assert.That(walker.ReturningClauses, Has.Count.EqualTo(1));
        Assert.That(walker.Expressions, Has.Count.GreaterThanOrEqualTo(4));
        Assert.That(walker.Names.Exists(name => name.GetText() == "items"), Is.True);
    }

    [Test]
    public void DeleteParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDeleteParser(null!));
    }

    static SqlDeleteStatement ParseDelete(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Delete!;
    }
}
