using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class QueryTailParserTests
{
    [Test]
    public void QueryParser_ParsesOrderByClause()
    {
        SqlQuery query = ParseQuery("select * from items order by name collate nocase desc nulls first, id asc");

        Assert.That(query.HasOrderBy(), Is.True);
        Assert.That(query.GetOrderByCount(), Is.EqualTo(2));
        Assert.That(query.GetOrderByAt(0).Expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(query.GetOrderByAt(0).HasCollation(), Is.True);
        Assert.That(query.GetOrderByAt(0).Collation!.GetText(), Is.EqualTo("nocase"));
        Assert.That(query.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(query.GetOrderByAt(0).NullOrdering, Is.EqualTo(SqlNullOrdering.First));
        Assert.That(query.GetOrderByAt(1).Direction, Is.EqualTo(SqlOrderingDirection.Asc));
    }

    [Test]
    public void QueryParser_ParsesLimitClause()
    {
        SqlQuery query = ParseQuery("select * from items limit 10");

        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Limit!.LimitExpression.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(query.Limit.HasOffset(), Is.False);
    }

    [Test]
    public void QueryParser_ParsesLimitOffsetClause()
    {
        SqlQuery query = ParseQuery("select * from items limit 10 offset 20");

        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Limit!.LimitExpression.Literal!.SourceText, Is.EqualTo("10"));
        Assert.That(query.Limit.HasOffset(), Is.True);
        Assert.That(query.Limit.OffsetExpression!.Literal!.SourceText, Is.EqualTo("20"));
    }

    [Test]
    public void QueryParser_ParsesSqliteCommaLimitClause()
    {
        SqlQuery query = ParseQuery("select * from items limit 20, 10");

        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Limit!.LimitExpression.Literal!.SourceText, Is.EqualTo("10"));
        Assert.That(query.Limit.OffsetExpression, Is.Not.Null);
        Assert.That(query.Limit.OffsetExpression!.Literal!.SourceText, Is.EqualTo("20"));
    }

    [Test]
    public void StatementParser_AttachesQueryTailPayloads()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("select * from items order by id limit 5", SqlParseOptions.CreateSqlite());
        SqlQuery query = result.Script.GetStatementAt(0).Query!;

        Assert.That(query.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(query.HasLimit(), Is.True);
        Assert.That(query.Span.Length, Is.EqualTo("select * from items order by id limit 5".Length));
    }

    [Test]
    public void QueryParser_ReportsOrderByDiagnostic()
    {
        SqlParserContext context = Context("select * order name");
        SqlQueryParser parser = new SqlQueryParser(context);

        SqlQuery query = parser.ParseQuery();

        Assert.That(query.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(context.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY017"));
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
