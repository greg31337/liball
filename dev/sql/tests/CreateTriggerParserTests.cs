using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CreateTriggerParserTests
{
    [Test]
    public void StatementParser_AttachesCreateTriggerSchemaAndKeepsBodySemicolonsInOneStatement()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create temp trigger if not exists tr_items after update of name, price on items for each row when new.price > old.price begin select 1; update audit set value = 1; insert into audit values (1); delete from audit where value = 0; end;", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.CreateTrigger));
        Assert.That(statement.CreateTrigger, Is.Not.Null);
        SqlTriggerDefinition trigger = statement.CreateTrigger!;
        Assert.That(trigger.IsTemporary, Is.True);
        Assert.That(trigger.IfNotExists, Is.True);
        Assert.That(trigger.Name.GetText(), Is.EqualTo("tr_items"));
        Assert.That(trigger.Timing, Is.EqualTo(SqlTriggerTiming.After));
        Assert.That(trigger.Event, Is.EqualTo(SqlTriggerEvent.Update));
        Assert.That(trigger.GetUpdateColumnCount(), Is.EqualTo(2));
        Assert.That(trigger.GetUpdateColumnAt(0).GetText(), Is.EqualTo("name"));
        Assert.That(trigger.GetUpdateColumnAt(1).GetText(), Is.EqualTo("price"));
        Assert.That(trigger.TargetName.GetText(), Is.EqualTo("items"));
        Assert.That(trigger.ForEachRow, Is.True);
        Assert.That(trigger.HasWhenExpression(), Is.True);
        Assert.That(trigger.WhenExpression!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(trigger.HasBody(), Is.True);
        Assert.That(trigger.GetBodyStatementCount(), Is.EqualTo(4));
        Assert.That(trigger.GetBodyStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(trigger.GetBodyStatementAt(1).Kind, Is.EqualTo(SqlStatementKind.Update));
        Assert.That(trigger.GetBodyStatementAt(2).Kind, Is.EqualTo(SqlStatementKind.Insert));
        Assert.That(trigger.GetBodyStatementAt(3).Kind, Is.EqualTo(SqlStatementKind.Delete));
    }

    [Test]
    public void CreateTriggerParser_ParsesInsteadOfAndDefaultTimingForms()
    {
        SqlTriggerDefinition insteadOf = ParseTrigger("create trigger tr_view instead of insert on v_items begin select 1; end");
        SqlTriggerDefinition defaultTiming = ParseTrigger("create trigger tr_delete delete on main.items begin select 1; end");

        Assert.That(insteadOf.Timing, Is.EqualTo(SqlTriggerTiming.InsteadOf));
        Assert.That(insteadOf.Event, Is.EqualTo(SqlTriggerEvent.Insert));
        Assert.That(insteadOf.TargetName.GetText(), Is.EqualTo("v_items"));
        Assert.That(insteadOf.GetBodyStatementCount(), Is.EqualTo(1));
        Assert.That(insteadOf.GetBodyStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(defaultTiming.Timing, Is.EqualTo(SqlTriggerTiming.Unknown));
        Assert.That(defaultTiming.Event, Is.EqualTo(SqlTriggerEvent.Delete));
        Assert.That(defaultTiming.TargetName.GetText(), Is.EqualTo("main.items"));
        Assert.That(defaultTiming.GetBodyStatementCount(), Is.EqualTo(1));
    }

    [Test]
    public void CreateTriggerParser_ReportsMalformedHeaders()
    {
        SqlParseResult missingOn = SqlStatementParser.ParseScript("create trigger tr_items before insert items begin select 1; end;", SqlParseOptions.CreateSqlite());
        SqlParseResult missingBegin = SqlStatementParser.ParseScript("create trigger tr_items after delete on items select 1; end;", SqlParseOptions.CreateSqlite());
        SqlParseResult malformedIf = SqlStatementParser.ParseScript("create trigger if tr_items after delete on items begin select 1; end;", SqlParseOptions.CreateSqlite());

        Assert.That(missingOn.IsSuccess(), Is.False);
        Assert.That(missingOn.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLTRIGGER011"));
        Assert.That(missingBegin.IsSuccess(), Is.False);
        Assert.That(missingBegin.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLTRIGGER016"));
        Assert.That(malformedIf.IsSuccess(), Is.False);
        Assert.That(malformedIf.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLTRIGGER003"));
    }

    [Test]
    public void CreateTriggerParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCreateTriggerParser(null!));
    }

    static SqlTriggerDefinition ParseTrigger(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateTrigger!;
    }
}
