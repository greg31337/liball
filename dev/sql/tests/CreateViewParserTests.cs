using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CreateViewParserTests
{
    [Test]
    public void StatementParser_AttachesCreateViewSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create temp view if not exists main.v_items (item_id, item_name) as select id, name from items where active = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.CreateView));
        Assert.That(statement.CreateView, Is.Not.Null);
        SqlViewDefinition view = statement.CreateView!;
        Assert.That(view.IsTemporary, Is.True);
        Assert.That(view.IfNotExists, Is.True);
        Assert.That(view.Name.GetText(), Is.EqualTo("main.v_items"));
        Assert.That(view.GetColumnNameCount(), Is.EqualTo(2));
        Assert.That(view.GetColumnNameAt(0).GetText(), Is.EqualTo("item_id"));
        Assert.That(view.GetColumnNameAt(1).GetText(), Is.EqualTo("item_name"));
        Assert.That(view.Query.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(view.Query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(2));
    }

    [Test]
    public void CreateViewParser_ParsesViewWithoutColumnList()
    {
        SqlViewDefinition view = ParseView("create view v_values as values (1), (2)");

        Assert.That(view.IsTemporary, Is.False);
        Assert.That(view.IfNotExists, Is.False);
        Assert.That(view.Name.GetText(), Is.EqualTo("v_values"));
        Assert.That(view.HasColumnNames(), Is.False);
        Assert.That(view.Query.GetSelectCoreAt(0).IsValues, Is.True);
    }

    [Test]
    public void CreateViewParser_ReportsMalformedInputs()
    {
        SqlParseResult missingAs = SqlStatementParser.ParseScript("create view v_items select 1", SqlParseOptions.CreateSqlite());
        SqlParseResult emptyColumnList = SqlStatementParser.ParseScript("create view v_items () as select 1", SqlParseOptions.CreateSqlite());
        SqlParseResult malformedIf = SqlStatementParser.ParseScript("create view if v_items as select 1", SqlParseOptions.CreateSqlite());

        Assert.That(missingAs.IsSuccess(), Is.False);
        Assert.That(missingAs.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIEW009"));
        Assert.That(emptyColumnList.IsSuccess(), Is.False);
        Assert.That(emptyColumnList.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIEW008"));
        Assert.That(malformedIf.IsSuccess(), Is.False);
        Assert.That(malformedIf.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIEW003"));
    }

    [Test]
    public void CreateViewParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCreateViewParser(null!));
    }

    static SqlViewDefinition ParseView(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateView!;
    }
}
