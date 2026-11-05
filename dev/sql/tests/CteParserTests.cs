using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CteParserTests
{
    [Test]
    public void QueryParser_ParsesCommonTableExpression()
    {
        SqlQuery query = ParseQuery("with cte as (select 1) select * from cte");

        Assert.That(query.HasCommonTableExpressions(), Is.True);
        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(1));
        SqlCommonTableExpression cte = query.GetCommonTableExpressionAt(0);
        Assert.That(cte.Name.GetText(), Is.EqualTo("cte"));
        Assert.That(cte.Query.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(query.GetSelectCoreAt(0).GetFromAt(0).Name!.GetText(), Is.EqualTo("cte"));
    }

    [Test]
    public void QueryParser_ParsesRecursiveCommonTableExpressionWithColumns()
    {
        SqlQuery query = ParseQuery("with recursive nums(n) as (select 1 union all select n + 1 from nums limit 10) select n from nums");

        Assert.That(query.IsRecursive, Is.True);
        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(1));
        SqlCommonTableExpression cte = query.GetCommonTableExpressionAt(0);
        Assert.That(cte.GetColumnNameCount(), Is.EqualTo(1));
        Assert.That(cte.GetColumnNameAt(0).GetText(), Is.EqualTo("n"));
        Assert.That(cte.Query.IsCompound(), Is.True);
        Assert.That(cte.Query.GetCompoundOperatorAt(0), Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(cte.Query.HasLimit(), Is.True);
    }

    [Test]
    public void QueryParser_ParsesCteMaterializationHints()
    {
        SqlQuery query = ParseQuery("with a as materialized (select 1), b as not materialized (select 2) select * from a, b");

        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(2));
        Assert.That(query.GetCommonTableExpressionAt(0).IsMaterialized(), Is.True);
        Assert.That(query.GetCommonTableExpressionAt(1).IsNotMaterialized(), Is.True);
    }

    [Test]
    public void StatementParser_AttachesCteQuery()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("with cte as (select 1) select * from cte", SqlParseOptions.CreateSqlite());
        SqlQuery query = result.Script.GetStatementAt(0).Query!;

        Assert.That(query.GetCommonTableExpressionCount(), Is.EqualTo(1));
        Assert.That(query.Span.Length, Is.EqualTo("with cte as (select 1) select * from cte".Length));
    }

    [Test]
    public void QueryParser_ReportsCteDiagnostics()
    {
        SqlParserContext missingAsContext = Context("with cte select 1");
        SqlQueryParser missingAsParser = new SqlQueryParser(missingAsContext);
        SqlParserContext missingNameContext = Context("with");
        SqlQueryParser missingNameParser = new SqlQueryParser(missingNameContext);

        SqlQuery missingAs = missingAsParser.ParseQuery();
        SqlQuery missingName = missingNameParser.ParseQuery();

        Assert.That(missingAs.GetCommonTableExpressionCount(), Is.EqualTo(1));
        Assert.That(missingAsContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY023"));
        Assert.That(missingName.GetCommonTableExpressionCount(), Is.EqualTo(0));
        Assert.That(missingNameContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY022"));
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
