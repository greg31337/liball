using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserWindowTests
{
    [Test]
    public void ExpressionParser_ParsesNamedWindowOverClause()
    {
        SqlExpression expression = Parse("row_number() over win");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Operand!.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.BaseWindowName, Is.Not.Null);
        Assert.That(expression.WindowDefinition.BaseWindowName!.GetText(), Is.EqualTo("win"));
    }

    [Test]
    public void ExpressionParser_ParsesPartitionAndOrderWindowDefinition()
    {
        SqlExpression expression = Parse("sum(amount) over (partition by category order by created desc)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.GetPartitionByCount(), Is.EqualTo(1));
        Assert.That(expression.WindowDefinition.GetOrderByCount(), Is.EqualTo(1));
        Assert.That(expression.WindowDefinition.GetPartitionByAt(0).Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(expression.WindowDefinition.GetOrderByAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
    }

    [Test]
    public void ExpressionParser_ParsesBaseWindowNameInsideDefinition()
    {
        SqlExpression expression = Parse("sum(amount) over (base partition by category)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.BaseWindowName, Is.Not.Null);
        Assert.That(expression.WindowDefinition.BaseWindowName!.GetText(), Is.EqualTo("base"));
        Assert.That(expression.WindowDefinition.GetPartitionByCount(), Is.EqualTo(1));
    }

    [Test]
    public void ExpressionParser_ParsesBetweenWindowFrame()
    {
        SqlExpression expression = Parse("sum(amount) over (order by created rows between unbounded preceding and current row)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.Frame, Is.Not.Null);
        Assert.That(expression.WindowDefinition.Frame!.Kind, Is.EqualTo(SqlWindowFrameKind.Rows));
        Assert.That(expression.WindowDefinition.Frame.Start.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.UnboundedPreceding));
        Assert.That(expression.WindowDefinition.Frame.End, Is.Not.Null);
        Assert.That(expression.WindowDefinition.Frame.End!.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.CurrentRow));
    }

    [Test]
    public void ExpressionParser_ParsesExpressionWindowFrameAndExclusion()
    {
        SqlExpression expression = Parse("sum(amount) over (order by created range between 1 preceding and 1 following exclude ties)");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(expression.WindowDefinition, Is.Not.Null);
        Assert.That(expression.WindowDefinition!.Frame, Is.Not.Null);
        Assert.That(expression.WindowDefinition.Frame!.Kind, Is.EqualTo(SqlWindowFrameKind.Range));
        Assert.That(expression.WindowDefinition.Frame.Start.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.Preceding));
        Assert.That(expression.WindowDefinition.Frame.Start.Expression, Is.Not.Null);
        Assert.That(expression.WindowDefinition.Frame.End, Is.Not.Null);
        Assert.That(expression.WindowDefinition.Frame.End!.Kind, Is.EqualTo(SqlWindowFrameBoundaryKind.Following));
        Assert.That(expression.WindowDefinition.Frame.Exclusion, Is.EqualTo(SqlWindowFrameExclusionKind.Ties));
    }

    [Test]
    public void ExpressionParser_WindowExpressionParticipatesInBinaryExpressions()
    {
        SqlExpression expression = Parse("sum(amount) over win + 1");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Window));
    }

    [Test]
    public void ExpressionParser_ReportsWindowDiagnostics()
    {
        SqlParserContext missingTargetContext = Context("sum(amount) over");
        SqlExpressionParser missingTargetParser = new SqlExpressionParser(missingTargetContext);
        SqlParserContext missingByContext = Context("sum(amount) over (partition category)");
        SqlExpressionParser missingByParser = new SqlExpressionParser(missingByContext);

        SqlExpression missingTarget = missingTargetParser.ParseExpression();
        SqlExpression missingBy = missingByParser.ParseExpression();

        Assert.That(missingTarget.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(missingTargetContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR022"));
        Assert.That(missingBy.Kind, Is.EqualTo(SqlExpressionKind.Window));
        Assert.That(missingByContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR024"));
    }

    [Test]
    public void ExpressionParser_WindowHelperRejectsInvalidInputs()
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context("x"));
        SqlExpression left = Parse("x");

        Assert.Throws<ArgumentNullException>(() => parser.TryParseWindowExpression(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.TryParseWindowExpression(left, -1));
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
