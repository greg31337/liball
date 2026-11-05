using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DropViewParserTests
{
    [Test]
    public void StatementParser_AttachesDropViewSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("drop view main.v_items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.DropView));
        Assert.That(statement.DropView, Is.Not.Null);
        SqlDropViewDefinition drop = statement.DropView!;
        Assert.That(drop.IfExists, Is.False);
        Assert.That(drop.Name.GetText(), Is.EqualTo("main.v_items"));
    }

    [Test]
    public void DropViewParser_ParsesIfExists()
    {
        SqlDropViewDefinition drop = ParseDrop("drop view if exists v_items");

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("v_items"));
    }

    [Test]
    public void DropViewParser_ReportsMalformedIfExistsAndMissingName()
    {
        SqlParseResult missingExists = SqlStatementParser.ParseScript("drop view if v_items", SqlParseOptions.CreateSqlite());
        SqlParseResult missingName = SqlStatementParser.ParseScript("drop view", SqlParseOptions.CreateSqlite());

        Assert.That(missingExists.IsSuccess(), Is.False);
        Assert.That(missingExists.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPVIEW003"));
        Assert.That(missingName.IsSuccess(), Is.False);
        Assert.That(missingName.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPVIEW004"));
    }

    [Test]
    public void DropViewParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDropViewParser(null!));
    }

    static SqlDropViewDefinition ParseDrop(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).DropView!;
    }
}
