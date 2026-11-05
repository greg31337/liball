using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CompoundSelectParserTests
{
    [Test]
    public void QueryParser_ParsesUnionCompoundSelect()
    {
        SqlQuery query = ParseQuery("select 1 union select 2");

        Assert.That(query.IsCompound(), Is.True);
        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorCount(), Is.EqualTo(1));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.Union));
    }

    [Test]
    public void QueryParser_ParsesUnionAllCompoundSelect()
    {
        SqlQuery query = ParseQuery("select 1 union all select 2");

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
    }

    [Test]
    public void QueryParser_ParsesIntersectAndExceptCompoundSelects()
    {
        SqlQuery query = ParseQuery("select 1 intersect select 2 except select 3");

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(3));
        Assert.That(query.GetCompoundOperatorCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.Intersect));
        Assert.That(query.GetCompoundOperatorAt(1), Is.EqualTo(SqlCompoundOperatorKind.Except));
    }

    [Test]
    public void QueryParser_ParsesOrderAndLimitAfterCompoundSelect()
    {
        SqlQuery query = ParseQuery("select 1 union all select 2 order by 1 limit 5");

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(query.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Span.Length, Is.EqualTo("select 1 union all select 2 order by 1 limit 5".Length));
    }

    [Test]
    public void StatementParser_AttachesCompoundSelectQuery()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select 1 union select 2", SqlParseOptions.CreateSqlite());
        SqlQuery query = result.Script.GetStatementAt(0).Query!;

        Assert.That(query.IsCompound(), Is.True);
        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.Union));
    }

    [Test]
    public void QueryParser_ReportsMissingRightSelectAfterCompoundOperator()
    {
        SqlParserContext context = Context("select 1 union order by 1");
        SqlQueryParser parser = new SqlQueryParser(context);

        SqlQuery query = parser.ParseQuery();

        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.Union));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY001"));
    }

    static SqlQuery ParseQuery(string text)
    {
        SqlQueryParser parser = new SqlQueryParser(Context(text));
        return parser.ParseQuery();
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
