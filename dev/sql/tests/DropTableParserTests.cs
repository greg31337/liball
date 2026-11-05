using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DropTableParserTests
{
    [Test]
    public void StatementParser_AttachesDropTableSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("drop table main.items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.DropTable));
        Assert.That(statement.DropTable, Is.Not.Null);
        SqlDropTableDefinition drop = statement.DropTable!;
        Assert.That(drop.IfExists, Is.False);
        Assert.That(drop.Name.GetText(), Is.EqualTo("main.items"));
    }

    [Test]
    public void DropTableParser_ParsesIfExists()
    {
        SqlDropTableDefinition drop = ParseDrop("drop table if exists items");

        Assert.That(drop.IfExists, Is.True);
        Assert.That(drop.Name.GetText(), Is.EqualTo("items"));
    }

    [Test]
    public void DropTableParser_ReportsMalformedIfExistsAndMissingName()
    {
        SqlParseResult missingExists = SqlStatementParser.ParseScript("drop table if items", SqlParseOptions.CreateSqlite());
        SqlParseResult missingName = SqlStatementParser.ParseScript("drop table", SqlParseOptions.CreateSqlite());

        Assert.That(missingExists.IsSuccess(), Is.False);
        Assert.That(missingExists.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROP003"));
        Assert.That(missingName.IsSuccess(), Is.False);
        Assert.That(missingName.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDROP004"));
    }

    [Test]
    public void DropTableParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDropTableParser(null!));
    }

    static SqlDropTableDefinition ParseDrop(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).DropTable!;
    }
}
