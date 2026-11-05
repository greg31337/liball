using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class JoinParserTests
{
    [Test]
    public void QueryParser_ParsesInnerJoinWithOnConstraint()
    {
        SqlSelectCore core = ParseCore("select * from items join orders on items.id = orders.item_id");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(core.GetFromCount(), Is.EqualTo(1));
        Assert.That(table.GetJoinCount(), Is.EqualTo(1));
        SqlJoin join = table.GetJoinAt(0);
        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Inner));
        Assert.That(join.Right.Name!.GetText(), Is.EqualTo("orders"));
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.On));
        Assert.That(join.OnExpression, Is.Not.Null);
        Assert.That(join.OnExpression!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [Test]
    public void QueryParser_ParsesLeftOuterJoinWithUsingConstraint()
    {
        SqlSelectCore core = ParseCore("select * from items left outer join orders using (id, item_id)");
        SqlJoin join = core.GetFromAt(0).GetJoinAt(0);

        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Left));
        Assert.That(join.IsOuter, Is.True);
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.Using));
        Assert.That(join.GetUsingColumnCount(), Is.EqualTo(2));
        Assert.That(join.GetUsingColumnAt(0).GetText(), Is.EqualTo("id"));
        Assert.That(join.GetUsingColumnAt(1).GetText(), Is.EqualTo("item_id"));
    }

    [Test]
    public void QueryParser_ParsesNaturalLeftOuterJoinCombination()
    {
        SqlSelectCore core = ParseCore("select * from items natural left outer join orders");
        SqlJoin join = core.GetFromAt(0).GetJoinAt(0);

        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Left));
        Assert.That(join.IsNatural, Is.True);
        Assert.That(join.IsOuter, Is.True);
        Assert.That(join.IsNaturalJoin(), Is.True);
    }

    [Test]
    public void QueryParser_ParsesCrossJoinWithoutConstraint()
    {
        SqlSelectCore core = ParseCore("select * from items cross join orders");
        SqlJoin join = core.GetFromAt(0).GetJoinAt(0);

        Assert.That(join.Kind, Is.EqualTo(SqlJoinKind.Cross));
        Assert.That(join.ConstraintKind, Is.EqualTo(SqlJoinConstraintKind.None));
        Assert.That(join.HasOnExpression(), Is.False);
        Assert.That(join.HasUsingColumns(), Is.False);
    }

    [Test]
    public void QueryParser_ParsesChainedJoinsOnLeftTable()
    {
        SqlSelectCore core = ParseCore("select * from a join b join c");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.GetJoinCount(), Is.EqualTo(2));
        Assert.That(table.GetJoinAt(0).Right.Name!.GetText(), Is.EqualTo("b"));
        Assert.That(table.GetJoinAt(1).Right.Name!.GetText(), Is.EqualTo("c"));
    }

    [Test]
    public void QueryParser_DoesNotTreatJoinKeywordAsAlias()
    {
        SqlSelectCore core = ParseCore("select * from items join orders");
        SqlTableReference table = core.GetFromAt(0);

        Assert.That(table.HasAlias(), Is.False);
        Assert.That(table.GetJoinCount(), Is.EqualTo(1));
    }

    [Test]
    public void QueryParser_ReportsMalformedJoinDiagnostics()
    {
        SqlParserContext missingJoinContext = Context("select * from items left orders");
        SqlQueryParser missingJoinParser = new SqlQueryParser(missingJoinContext);
        SqlParserContext missingUsingCloseContext = Context("select * from items join orders using (id");
        SqlQueryParser missingUsingCloseParser = new SqlQueryParser(missingUsingCloseContext);

        SqlQuery missingJoin = missingJoinParser.ParseQuery();
        SqlQuery missingUsingClose = missingUsingCloseParser.ParseQuery();

        Assert.That(missingJoin.GetSelectCoreAt(0).GetFromAt(0).GetJoinCount(), Is.EqualTo(1));
        Assert.That(missingJoinContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY009"));
        Assert.That(missingUsingClose.GetSelectCoreAt(0).GetFromAt(0).GetJoinCount(), Is.EqualTo(1));
        Assert.That(missingUsingCloseContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY011"));
    }

    static SqlSelectCore ParseCore(string text)
    {
        SqlQueryParser parser = new SqlQueryParser(Context(text));
        return parser.ParseQuery().GetSelectCoreAt(0);
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
