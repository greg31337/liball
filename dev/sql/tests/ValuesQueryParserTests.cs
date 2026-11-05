using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ValuesQueryParserTests
{
    [Test]
    public void QueryParser_ParsesValuesCore()
    {
        SqlQuery query = ParseQuery("values (1, 2), (3, 4)");
        SqlSelectCore core = query.GetSelectCoreAt(0);

        Assert.That(core.IsValues, Is.True);
        Assert.That(core.HasValues(), Is.True);
        Assert.That(core.GetValueRowCount(), Is.EqualTo(2));
        Assert.That(core.GetValueRowAt(0).Length, Is.EqualTo(2));
        Assert.That(core.GetValueRowAt(0)[0].Literal!.SourceText, Is.EqualTo("1"));
        Assert.That(core.GetValueRowAt(1)[1].Literal!.SourceText, Is.EqualTo("4"));
    }

    [Test]
    public void StatementParser_AttachesValuesQuery()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("values (1)", SqlParseOptions.CreateSqlite());
        SqlQuery query = result.Script.GetStatementAt(0).Query!;

        Assert.That(result.Script.GetStatementAt(0).Kind, Is.EqualTo(SqlStatementKind.Select));
        Assert.That(query.GetSelectCoreAt(0).IsValues, Is.True);
    }

    [Test]
    public void QueryParser_ParsesValuesInCompoundQuery()
    {
        SqlQuery query = ParseQuery("values (1) union all values (2)");

        Assert.That(query.IsCompound(), Is.True);
        Assert.That(query.GetSelectCoreCount(), Is.EqualTo(2));
        Assert.That(query.GetSelectCoreAt(0).IsValues, Is.True);
        Assert.That(query.GetSelectCoreAt(1).IsValues, Is.True);
        Assert.That(query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
    }

    [Test]
    public void QueryParser_ParsesValuesInCommonTableExpression()
    {
        SqlQuery query = ParseQuery("with cte as (values (1)) select * from cte");
        SqlCommonTableExpression cte = query.GetCommonTableExpressionAt(0);

        Assert.That(cte.Query.GetSelectCoreAt(0).IsValues, Is.True);
        Assert.That(cte.Query.GetSelectCoreAt(0).GetValueRowCount(), Is.EqualTo(1));
    }

    [Test]
    public void QueryParser_ReportsValuesDiagnostics()
    {
        SqlParserContext missingRowContext = Context("values");
        SqlQueryParser missingRowParser = new SqlQueryParser(missingRowContext);
        SqlParserContext missingCloseContext = Context("values (1");
        SqlQueryParser missingCloseParser = new SqlQueryParser(missingCloseContext);

        SqlQuery missingRow = missingRowParser.ParseQuery();
        SqlQuery missingClose = missingCloseParser.ParseQuery();

        Assert.That(missingRow.GetSelectCoreAt(0).IsValues, Is.True);
        Assert.That(missingRowContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY026"));
        Assert.That(missingClose.GetSelectCoreAt(0).IsValues, Is.True);
        Assert.That(missingCloseContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY027"));
    }

    [Test]
    public void Walker_WalksValuesRows()
    {
        SqlExpression value = SqlExpression.Create(SqlExpressionKind.Literal, new SqlTextSpan(8, 1));
        SqlSelectCore core = SqlSelectCore.Create(new SqlResultColumn[0], new SqlTextSpan(0, 9));
        core.IsValues = true;
        core.Values = new SqlExpression[][] { new SqlExpression[] { value } };
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkSelectCore(core);

        Assert.That(walker.Expressions, Does.Contain(value));
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
