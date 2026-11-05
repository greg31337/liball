using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DropTriggerParserTests
{
    [Test]
    public void StatementParser_AttachesDropTriggerSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("drop trigger main.tr_items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.DropTrigger));
        Assert.That(statement.DropTrigger, Is.Not.Null);
        SqlDropTriggerDefinition drop = statement.DropTrigger!;
        Assert.That(drop.IfExists, Is.False);
        Assert.That(drop.Name.GetText(), Is.EqualTo("main.tr_items"));
    }

    [Test]
    public void DropTriggerParser_ParsesIfExists()
    {
        SqlDropTriggerDefinition drop = ParseDrop("drop trigger if exists tr_items");

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("tr_items"));
    }

    [Test]
    public void DropTriggerParser_ReportsMalformedIfExistsAndMissingName()
    {
        SqlParseResult missingExists = SqlStatementParser.ParseScript("drop trigger if tr_items", SqlParseOptions.CreateSqlite());
        SqlParseResult missingName = SqlStatementParser.ParseScript("drop trigger", SqlParseOptions.CreateSqlite());

        Assert.That(missingExists.IsSuccess(), Is.False);
        Assert.That(missingExists.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPTRIGGER003"));
        Assert.That(missingName.IsSuccess(), Is.False);
        Assert.That(missingName.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPTRIGGER004"));
    }

    [Test]
    public void DropTriggerParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDropTriggerParser(null!));
    }

    static SqlDropTriggerDefinition ParseDrop(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).DropTrigger!;
    }
}
