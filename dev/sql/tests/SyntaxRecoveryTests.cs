using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SyntaxRecoveryTests
{
    [Test]
    public void QueryParser_RecoversMissingCommaBetweenResultColumns()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select 1 2, 3", SqlParseOptions.CreateSqlite());
        SqlSelectCore core = result.Script.GetStatementAt(0).Query!.GetSelectCoreAt(0);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY030"));
        Assert.That(core.GetResultColumnCount(), Is.EqualTo(3));
    }

    [Test]
    public void QueryParser_RecoversMissingCommaBetweenValuesExpressions()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("values (1 2, 3)", SqlParseOptions.CreateSqlite());
        SqlSelectCore core = result.Script.GetStatementAt(0).Query!.GetSelectCoreAt(0);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY031"));
        Assert.That(core.GetValueRowAt(0), Has.Length.EqualTo(3));
    }

    [Test]
    public void QueryParser_RecoversMissingCommaBetweenTableReferences()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select * from items i orders o where i.id = o.item_id", SqlParseOptions.CreateSqlite());
        SqlSelectCore core = result.Script.GetStatementAt(0).Query!.GetSelectCoreAt(0);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY032"));
        Assert.That(core.GetFromCount(), Is.EqualTo(2));
        Assert.That(core.GetFromAt(0).Alias!.GetText(), Is.EqualTo("i"));
        Assert.That(core.GetFromAt(1).Alias!.GetText(), Is.EqualTo("o"));
        Assert.That(core.HasWhere(), Is.True);
    }

    [Test]
    public void QueryParser_DoesNotRecoverMissingCommaWhenRecoveryDisabled()
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        options.EnableRecovery = false;
        SqlParseResult result = SqlStatementParser.ParseScript("values (1 2)", options);
        SqlSelectCore core = result.Script.GetStatementAt(0).Query!.GetSelectCoreAt(0);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY027"));
        Assert.That(core.GetValueRowAt(0), Has.Length.EqualTo(1));
    }
}
