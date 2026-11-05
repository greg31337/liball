using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserFunctionCallTests
{
    [Test]
    public void ExpressionParser_ParsesSimpleAndQualifiedFunctionCalls()
    {
        SqlExpression simple = Parse("max(amount)");
        SqlExpression qualified = Parse("main.stats.sum(amount, tax + 1)");

        Assert.That(simple.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(simple.Name, Is.Not.Null);
        Assert.That(simple.Name!.GetText(), Is.EqualTo("max"));
        Assert.That(simple.GetArgumentCount(), Is.EqualTo(1));
        Assert.That(simple.GetArgumentAt(0).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(qualified.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(qualified.Name, Is.Not.Null);
        Assert.That(qualified.Name!.GetText(), Is.EqualTo("main.stats.sum"));
        Assert.That(qualified.GetArgumentCount(), Is.EqualTo(2));
        Assert.That(qualified.GetArgumentAt(1).Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [Test]
    public void ExpressionParser_ParsesZeroArgumentAndWildcardFunctionCalls()
    {
        SqlExpression zero = Parse("random()");
        SqlExpression wildcard = Parse("count(*)");

        Assert.That(zero.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(zero.GetArgumentCount(), Is.EqualTo(0));
        Assert.That(zero.IsDistinct, Is.False);
        Assert.That(wildcard.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(wildcard.GetArgumentCount(), Is.EqualTo(1));
        Assert.That(wildcard.GetArgumentAt(0).Kind, Is.EqualTo(SqlExpressionKind.Wildcard));
    }

    [Test]
    public void ExpressionParser_ParsesDistinctAggregateArguments()
    {
        SqlExpression expression = Parse("sum(distinct amount)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.IsDistinct, Is.True);
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(1));
        Assert.That(expression.GetTokenCount(), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void ExpressionParser_ParsesFunctionOrderByTerms()
    {
        SqlExpression expression = Parse("group_concat(name order by created desc nulls last, id asc)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(1));
        Assert.That(expression.OrderBy.Length, Is.EqualTo(2));
        Assert.That(expression.OrderBy[0].Expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.OrderBy[0].Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(expression.OrderBy[0].NullOrdering, Is.EqualTo(SqlNullOrdering.Last));
        Assert.That(expression.OrderBy[1].Direction, Is.EqualTo(SqlOrderingDirection.Asc));
    }

    [Test]
    public void ExpressionParser_ParsesFunctionOrderByCollation()
    {
        SqlExpression expression = Parse("group_concat(name order by name collate nocase desc)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.OrderBy.Length, Is.EqualTo(1));
        Assert.That(expression.OrderBy[0].HasCollation(), Is.True);
        Assert.That(expression.OrderBy[0].Collation!.GetText(), Is.EqualTo("nocase"));
        Assert.That(expression.OrderBy[0].Direction, Is.EqualTo(SqlOrderingDirection.Desc));
    }

    [Test]
    public void ExpressionParser_ParsesAggregateFilterClause()
    {
        SqlExpression expression = Parse("sum(amount) filter (where amount > 0)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.Filter, Is.Not.Null);
        Assert.That(expression.Filter!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.Filter.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Greater));
        Assert.That(expression.Span.Length, Is.EqualTo("sum(amount) filter (where amount > 0)".Length));
    }

    [Test]
    public void ExpressionParser_ParsesCombinedAggregateOptions()
    {
        SqlExpression expression = Parse("sum(distinct amount order by created desc) filter (where active = 1)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.IsDistinct, Is.True);
        Assert.That(expression.GetArgumentCount(), Is.EqualTo(1));
        Assert.That(expression.OrderBy.Length, Is.EqualTo(1));
        Assert.That(expression.OrderBy[0].Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(expression.Filter, Is.Not.Null);
        Assert.That(expression.Filter!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Equal));
    }

    [Test]
    public void ExpressionParser_FunctionCallsParticipateInBinaryExpressions()
    {
        SqlExpression expression = Parse("abs(x) + 1");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
    }

    [Test]
    public void ExpressionParser_ReportsFunctionCallDiagnostics()
    {
        SqlParserContext missingCloseContext = Context("max(amount");
        SqlExpressionParser missingCloseParser = new SqlExpressionParser(missingCloseContext);
        SqlParserContext missingByContext = Context("group_concat(x order x)");
        SqlExpressionParser missingByParser = new SqlExpressionParser(missingByContext);
        SqlParserContext malformedFilterContext = Context("sum(x) filter where x");
        SqlExpressionParser malformedFilterParser = new SqlExpressionParser(malformedFilterContext);

        SqlExpression missingClose = missingCloseParser.ParseExpression();
        SqlExpression missingBy = missingByParser.ParseExpression();
        SqlExpression malformedFilter = malformedFilterParser.ParseExpression();

        Assert.That(missingClose.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(missingCloseContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR006"));
        Assert.That(missingBy.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(missingByContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR007"));
        Assert.That(malformedFilter.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(malformedFilterContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR010"));
    }

    static SqlExpression Parse(string text)
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context(text));
        return parser.ParseExpression();
    }

    static SqlParserContext Context(string text)
    {
        SqlParseOptions options = SqlParseOptions.CreateSqlite();
        SqlToken[] tokens = SqlLexer.Tokenize(text, options.Dialect);
        return SqlParserContext.Create(tokens, options, SqlSourceText.Create(text));
    }
}
