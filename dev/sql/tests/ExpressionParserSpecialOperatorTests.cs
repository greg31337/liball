using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ExpressionParserSpecialOperatorTests
{
    [TestCase("a is null", "is")]
    [TestCase("a is not null", "is not")]
    public void ExpressionParser_ParsesIsExpressions(string text, string operatorText)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Is));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.OperatorText, Is.EqualTo(operatorText));
        Assert.That(expression.OperatorToken, Is.Not.Null);
        Assert.That(expression.GetTokenCount(), Is.EqualTo(operatorText == "is" ? 1 : 2));
    }

    [TestCase("name like 'a%'", "like")]
    [TestCase("name glob 'a*'", "glob")]
    [TestCase("name regexp '^a'", "regexp")]
    [TestCase("name match 'a'", "match")]
    [TestCase("name not like 'a%'", "not like")]
    [TestCase("name not glob 'a*'", "not glob")]
    [TestCase("name not regexp '^a'", "not regexp")]
    [TestCase("name not match 'a'", "not match")]
    public void ExpressionParser_ParsesPatternMatchExpressions(string text, string operatorText)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Like));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Alternate, Is.Null);
        Assert.That(expression.OperatorText, Is.EqualTo(operatorText));
    }

    [Test]
    public void ExpressionParser_ParsesLikeEscapeExpression()
    {
        SqlExpression expression = Parse("name like 'a!%' escape '!'");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Like));
        Assert.That(expression.OperatorText, Is.EqualTo("like escape"));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Alternate, Is.Not.Null);
        Assert.That(expression.Alternate!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [TestCase("age between 10 and 20", "between")]
    [TestCase("age not between 10 and 20", "not between")]
    public void ExpressionParser_ParsesBetweenExpressions(string text, string operatorText)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Between));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.OperatorText, Is.EqualTo(operatorText + " and"));
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
    }

    [TestCase("id in (1, 2, 3)", "in")]
    [TestCase("id not in (1, 2)", "not in")]
    public void ExpressionParser_ParsesInExpressionLists(string text, string operatorText)
    {
        SqlExpression expression = Parse(text);

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(expression.Operand, Is.Not.Null);
        Assert.That(expression.OperatorText, Is.EqualTo(operatorText));
        Assert.That(expression.Right, Is.Not.Null);
        Assert.That(expression.Right!.Kind, Is.EqualTo(SqlExpressionKind.Unknown));
        Assert.That(expression.Arguments.Length, Is.GreaterThan(0));
    }

    [Test]
    public void ExpressionParser_ParsesEmptyInExpressionList()
    {
        SqlExpression expression = Parse("id in ()");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(expression.Arguments.Length, Is.EqualTo(0));
        Assert.That(expression.Span.Length, Is.EqualTo("id in ()".Length));
    }

    [Test]
    public void ExpressionParser_SpecialOperatorsUseComparisonPrecedence()
    {
        SqlExpression expression = Parse("a + b in (c) or d");

        Assert.That(expression.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(expression.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Or));
        Assert.That(expression.Left, Is.Not.Null);
        Assert.That(expression.Left!.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(expression.Left.Operand, Is.Not.Null);
        Assert.That(expression.Left.Operand!.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.Add));
    }

    [Test]
    public void ExpressionParser_ReportsDiagnosticsForMalformedSpecialOperators()
    {
        SqlParserContext betweenContext = Context("a between 1");
        SqlExpressionParser betweenParser = new SqlExpressionParser(betweenContext);
        SqlParserContext inContext = Context("a in ;");
        SqlExpressionParser inParser = new SqlExpressionParser(inContext);

        SqlExpression between = betweenParser.ParseExpression();
        SqlExpression inExpression = inParser.ParseExpression();

        Assert.That(between.Kind, Is.EqualTo(SqlExpressionKind.Between));
        Assert.That(betweenContext.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(betweenContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR003"));
        Assert.That(inExpression.Kind, Is.EqualTo(SqlExpressionKind.In));
        Assert.That(inContext.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(inContext.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLEXPR005"));
    }

    [Test]
    public void ExpressionParser_SpecialOperatorHelpersRejectInvalidInputs()
    {
        SqlExpressionParser parser = new SqlExpressionParser(Context("a"));
        SqlExpression left = Parse("a");

        Assert.Throws<ArgumentNullException>(() => parser.TryParseSpecialExpression(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.TryParseSpecialExpression(left, -1));
        Assert.Throws<ArgumentNullException>(() => parser.GetSpecialOperatorPrecedence(null!));
        Assert.Throws<ArgumentNullException>(() => parser.IsPatternMatchOperator(null!));
        Assert.Throws<ArgumentNullException>(() => parser.ParseIsExpression(null!, 40));
        Assert.Throws<ArgumentNullException>(() => parser.ParseLikeExpression(null!, 40, false));
        Assert.Throws<ArgumentNullException>(() => parser.ParseBetweenExpression(null!, 40, false));
        Assert.Throws<ArgumentNullException>(() => parser.ParseInExpression(null!, 40, false));
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
