using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class TypedPayloadTests
{
    [Test]
    public void StatementParser_PopulatesEveryTypedPayloadField()
    {
        Assert.That(Parse("select 1").Query, Is.Not.Null);
        Assert.That(Parse("insert into items values (1)").Insert, Is.Not.Null);
        Assert.That(Parse("update items set id = 1").Update, Is.Not.Null);
        Assert.That(Parse("delete from items").Delete, Is.Not.Null);
        Assert.That(Parse("create table items (id integer)").CreateTable, Is.Not.Null);
        Assert.That(Parse("alter table items rename to archived_items").AlterTable, Is.Not.Null);
        Assert.That(Parse("drop table items").DropTable, Is.Not.Null);
        Assert.That(Parse("create index idx_items on items (id)").CreateIndex, Is.Not.Null);
        Assert.That(Parse("drop index idx_items").DropIndex, Is.Not.Null);
        Assert.That(Parse("create view item_ids as select id from items").CreateView, Is.Not.Null);
        Assert.That(Parse("drop view item_ids").DropView, Is.Not.Null);
        Assert.That(Parse("create trigger tr_items after insert on items begin select 1; end").CreateTrigger, Is.Not.Null);
        Assert.That(Parse("drop trigger tr_items").DropTrigger, Is.Not.Null);
        Assert.That(Parse("create virtual table docs using fts5(content)").CreateVirtualTable, Is.Not.Null);
        Assert.That(Parse("pragma cache_size = 100").Pragma, Is.Not.Null);
        Assert.That(Parse("analyze items").Analyze, Is.Not.Null);
        Assert.That(Parse("vacuum main").Vacuum, Is.Not.Null);
        Assert.That(Parse("attach ':memory:' as aux").Attach, Is.Not.Null);
        Assert.That(Parse("detach aux").Detach, Is.Not.Null);
        Assert.That(Parse("reindex idx_items").Reindex, Is.Not.Null);
        Assert.That(Parse("begin immediate").Transaction, Is.Not.Null);
        Assert.That(Parse("commit").Transaction, Is.Not.Null);
        Assert.That(Parse("rollback").Transaction, Is.Not.Null);
        Assert.That(Parse("savepoint work").Savepoint, Is.Not.Null);
        Assert.That(Parse("release work").Release, Is.Not.Null);
        Assert.That(Parse("explain query plan select 1").Explain, Is.Not.Null);
    }

    [Test]
    public void QueryAndReturningClauses_ExposeTypedResultColumns()
    {
        SqlStatement select = Parse("select id as item_id from items");
        SqlStatement insert = Parse("insert into items values (1) returning id");

        SqlResultColumn selectColumn = select.Query!.SelectCores[0].ResultColumns[0];
        SqlResultColumn returningColumn = insert.Insert!.Returning!.ResultColumns[0];

        Assert.That(selectColumn.Expression!.Name!.GetText(), Is.EqualTo("id"));
        Assert.That(selectColumn.Alias!.GetText(), Is.EqualTo("item_id"));
        Assert.That(returningColumn.Expression!.Name!.GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void CastAndParameterExpressions_ExposeTypedTextPayloads()
    {
        SqlExpression cast = SqlParser.ParseExpression("cast(value as decimal(10, 2))").Expression;
        SqlExpression parameter = SqlParser.ParseExpression(":item_id").Expression;

        string? typeName = cast.TypeName;
        string? parameterText = parameter.Value;

        Assert.That(typeName, Is.EqualTo("decimal(10, 2)"));
        Assert.That(parameterText, Is.EqualTo(":item_id"));
    }

    static SqlStatement Parse(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True, text);
        return result.Script.GetStatementAt(0);
    }
}
