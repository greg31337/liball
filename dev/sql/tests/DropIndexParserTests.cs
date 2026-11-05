using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DropIndexParserTests
{
    [Test]
    public void StatementParser_AttachesDropIndexSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("drop index main.idx_items_name", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.DropIndex));
        Assert.That(statement.DropIndex, Is.Not.Null);
        SqlDropIndexDefinition drop = statement.DropIndex!;
        Assert.That(drop.IfExists, Is.False);
        Assert.That(drop.Name.GetText(), Is.EqualTo("main.idx_items_name"));
    }

    [Test]
    public void DropIndexParser_ParsesIfExists()
    {
        SqlDropIndexDefinition drop = ParseDrop("drop index if exists idx_items_name");

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("idx_items_name"));
    }

    [Test]
    public void DropIndexParser_ReportsMalformedIfExistsAndMissingName()
    {
        SqlParseResult missingExists = SqlStatementParser.ParseScript("drop index if idx_items_name", SqlParseOptions.CreateSqlite());
        SqlParseResult missingName = SqlStatementParser.ParseScript("drop index", SqlParseOptions.CreateSqlite());

        Assert.That(missingExists.IsSuccess(), Is.False);
        Assert.That(missingExists.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPIX003"));
        Assert.That(missingName.IsSuccess(), Is.False);
        Assert.That(missingName.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROPIX004"));
    }

    [Test]
    public void DropIndexParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDropIndexParser(null!));
    }

    static SqlDropIndexDefinition ParseDrop(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).DropIndex!;
    }
}
