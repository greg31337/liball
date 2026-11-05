using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SelectCoreClauseParserTests
{
    [Test]
    public void QueryParser_ParsesWhereClause()
    {
        SqlSelectCore core = ParseCore("select * from items where id = 1");

        Assert.That(core.HasWhere(), Is.True);
        Assert.That(core.Where!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(core.Where.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Equal));
    }

    [Test]
    public void QueryParser_ParsesGroupByAndHavingClauses()
    {
        SqlSelectCore core = ParseCore("select category, count(*) from items group by category, region having count(*) > 1");

        Assert.That(core.HasGroupBy(), Is.True);
        Assert.That(core.GetGroupByCount(), Is.EqualTo(2));
        Assert.That(core.GetGroupByAt(0).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(core.GetGroupByAt(1).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(core.HasHaving(), Is.True);
        Assert.That(core.Having!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(core.Having.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Greater));
    }

    [Test]
    public void QueryParser_ParsesNamedWindowClause()
    {
        SqlSelectCore core = ParseCore("select sum(amount) over win from items window win as (partition by category order by created)");

        Assert.That(core.HasWindows(), Is.True);
        Assert.That(core.GetWindowCount(), Is.EqualTo(1));
        SqlWindowDefinition window = core.GetWindowAt(0);
        Assert.That(window.Name, Is.Not.Null);
        Assert.That(window.Name!.GetText(), Is.EqualTo("win"));
        Assert.That(window.GetPartitionByCount(), Is.EqualTo(1));
        Assert.That(window.GetOrderByCount(), Is.EqualTo(1));
    }

    [Test]
    public void QueryParser_ParsesMultipleWindowDefinitions()
    {
        SqlSelectCore core = ParseCore("select sum(x) over w1 from t window w1 as (partition by a), w2 as (order by b)");

        Assert.That(core.GetWindowCount(), Is.EqualTo(2));
        Assert.That(core.GetWindowAt(0).Name!.GetText(), Is.EqualTo("w1"));
        Assert.That(core.GetWindowAt(1).Name!.GetText(), Is.EqualTo("w2"));
        Assert.That(core.GetWindowAt(1).GetOrderByCount(), Is.EqualTo(1));
    }

    [Test]
    public void QueryParser_ReportsClauseDiagnostics()
    {
        SqlParserContext groupContext = Context("select * from items group category");
        SqlQueryParser groupParser = new SqlQueryParser(groupContext);
        SqlParserContext windowContext = Context("select * from items window");
        SqlQueryParser windowParser = new SqlQueryParser(windowContext);

        SqlQuery groupQuery = groupParser.ParseQuery();
        SqlQuery windowQuery = windowParser.ParseQuery();

        Assert.That(groupQuery.GetSelectCoreAt(0).GetGroupByCount(), Is.EqualTo(1));
        Assert.That(groupContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY012"));
        Assert.That(windowQuery.GetSelectCoreAt(0).GetWindowCount(), Is.EqualTo(0));
        Assert.That(windowContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY014"));
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
